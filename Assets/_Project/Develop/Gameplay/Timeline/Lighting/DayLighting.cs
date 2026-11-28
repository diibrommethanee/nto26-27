using System;
using _Project.Develop.Data.Timeline.Lighting;
using MessagePipe;
using NaughtyAttributes;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VContainer;

namespace _Project.Develop.Gameplay.Timeline.Lighting
{
    public class DayLighting : MonoBehaviour
    {
        [SerializeField] private DayLightingSettings settings;
        [SerializeField] private Light2D globalLight;
        [SerializeField] private Volume morningVolume;
        [SerializeField] private Volume daytimeVolume;
        [SerializeField] private Volume eveningVolume;

        private IBufferedSubscriber<DayPhaseChanged> _phases;
        private DayService _dayService;
        private NextDayTransition _nextDay;
        private IDisposable _subscription;
        private Tween _transition;
        private Vector3 _blend;
        private bool _initialized;

        [Inject]
        private void Construct(IBufferedSubscriber<DayPhaseChanged> phases, DayService dayService, NextDayTransition nextDay)
        {
            _phases = phases;
            _dayService = dayService;
            _nextDay = nextDay;
            if (isActiveAndEnabled) Subscribe();
        }

        [Button("Force Phase", EButtonEnableMode.Playmode)]
        private void ForcePhase() => _dayService.TryAdvancePhase();

        [Button("End Day", EButtonEnableMode.Playmode)]
        private void EndDay() => _dayService.TryEndDay();

        [Button("Start Next Day", EButtonEnableMode.Playmode)]
        private async void StartNextDay()
        {
            try { await _nextDay.StartAsync(); }
            catch (Exception error) { Debug.LogException(error, this); }
        }
        
#if UNITY_EDITOR
        [Inject] private _Project.Develop.Data.ICheckpointService _checkpoints;

        private async void ForceSave()
        {
            try { await _checkpoints.SaveAsync(); }
            catch (Exception error) { Debug.LogException(error, this); }
        }
#endif

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.digit1Key.wasPressedThisFrame)
                ForcePhase();
            if (keyboard.digit2Key.wasPressedThisFrame)
                EndDay();
            if (keyboard.digit3Key.wasPressedThisFrame)
                StartNextDay();
#if UNITY_EDITOR
            if (keyboard.digit9Key.wasPressedThisFrame)
                ForceSave();
#endif
        }

        private void OnEnable()
        {
            if (_phases != null) Subscribe();
        }

        private void Subscribe()
        {
            morningVolume.sharedProfile = settings.morning.profile;
            daytimeVolume.sharedProfile = settings.daytime.profile;
            eveningVolume.sharedProfile = settings.evening.profile;
            _initialized = false;
            _subscription = _phases.Subscribe(ApplyPhase);
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
            _transition.Stop();
        }

        private void ApplyPhase(DayPhaseChanged message)
        {
            var target = message.Phase switch
            {
                DayPhase.Morning => Vector3.right,
                DayPhase.Daytime => Vector3.up,
                _ => Vector3.forward
            };
            _transition.Stop();
            if (!_initialized)
            {
                _initialized = true;
                ApplyBlend(target);
                return;
            }

            _transition = Tween.Custom(this, _blend, target, settings.transitionDuration,
                (lighting, blend) => lighting.ApplyBlend(blend),
                Ease.InOutSine, useUnscaledTime: true);
        }

        private void ApplyBlend(Vector3 blend)
        {
            _blend = blend;
            globalLight.color = settings.morning.color * blend.x + settings.daytime.color * blend.y + settings.evening.color * blend.z;
            globalLight.intensity = settings.morning.intensity * blend.x + settings.daytime.intensity * blend.y + settings.evening.intensity * blend.z;

            // URP applies volumes in priority order. Normalize the first two before the evening overlay.
            var morningAndDay = blend.x + blend.y;
            morningVolume.weight = 1f;
            daytimeVolume.weight = morningAndDay > 0f ? blend.y / morningAndDay : 0f;
            eveningVolume.weight = blend.z;
        }
    }
}
