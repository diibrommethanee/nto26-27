using System;
using _Project.Develop.Gameplay.Abilities.Hearing;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Player.Abilities;
using _Project.Develop.Gameplay.Tea;
using MessagePipe;
using PrimeTween;
using TMPEffects.Components;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.Develop.UI.Hearing
{
    public class HearingView : MonoBehaviour
    {
        private const float NoSignalRadius = 2f;
        private const float EnterSeconds = 0.30f;
        private const float ExitSeconds = 0.24f;
        private const float TranscriptReadSeconds = 1.2f;
        private const float FadeSeconds = 0.75f;

        [SerializeField] private GameObject panel;
        [SerializeField] private Sprite hearingIcon;
        [SerializeField] private HearingSource[] sources;
        [SerializeField] private PlayerAbility player;
        [SerializeField] private HearingWave wave;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Vector2 offsetAbovePlayer = new(0f, 3.65f);

        private IDisposable _subscription;
        private EvidenceService _evidence;
        private InputController _input;
        private NextDayBuffService _buffService;
        private TMPWriter _writer;
        private Canvas _canvas;
        private CanvasGroup _visibility;
        private Tween _visibilityTween;
        private float _statusAlpha;
        private HearingSource _source;
        private Vector3? _lastNoSignalOrigin;
        private Vector3 _lastNoSignalPosition;
        private enum ListeningStage { Tuning, WritingTranscript, ReadingTranscript }

        private HearingTuning _tuning;
        private ListeningStage _stage;
        private float _readingElapsed;

        [Inject]
        private void Construct(IBufferedSubscriber<HearingChanged> hearing, EvidenceService evidence,
            InputController input, NextDayBuffService buffService)
        {
            _evidence = evidence;
            _input = input;
            _buffService = buffService;
            _writer = status.GetComponent<TMPWriter>();
            _writer.OnFinishWriter.AddListener(OnTranscriptFinished);
            _canvas = GetComponent<Canvas>();
            if (!panel.TryGetComponent(out _visibility))
                _visibility = panel.AddComponent<CanvasGroup>();
            _visibility.alpha = 0f;
            _statusAlpha = status.alpha;
            wave.raycastTarget = false;
            _subscription = hearing.Subscribe(OnHearingChanged);
        }

        private void OnHearingChanged(HearingChanged message)
        {
            _visibilityTween.Stop();
            _source = null;
            _tuning = null;
            _stage = ListeningStage.Tuning;
            if (!message.IsActive)
            {
                FadeOut();
                return;
            }

            _writer.enabled = false;
            Vector3 playerPosition = player.transform.position;

            _source = FindNearestSource(playerPosition);

            bool repeatNoSignal = false;
            transform.position = _source != null
                ? _source.transform.position
                : GetNoSignalPosition(playerPosition, out repeatNoSignal);
            wave.gameObject.SetActive(_source != null);
            _canvas.enabled = !_input.IsZoomMode;
            status.text = string.Empty;
            status.alpha = _statusAlpha;
            panel.SetActive(true);
            if (_source == null)
            {
                ShowNoSignal(repeatNoSignal);
                return;
            }

            FadeIn();
            wave.ResetSignal();
            wave.FacePlayer(playerPosition - _source.transform.position);
            _tuning = new HearingTuning(_source);
            RefreshTuningHint();
        }

        private void ShowNoSignal(bool repeated)
        {
            const string messageText = "Поблизости ничего не слышно";
            if (repeated)
            {
                status.text = messageText;
                FadeIn();
            }
            else
            {
                _visibility.alpha = 1f;
                StartWriting(messageText);
            }
        }

        private HearingSource FindNearestSource(Vector3 playerPosition)
        {
            HearingSource nearestSource = null;
            float nearest = float.PositiveInfinity;
            foreach (var source in sources)
            {
                float distance = Vector2.Distance(playerPosition, source.transform.position);
                if (!source.isActiveAndEnabled || distance > _buffService.AbilityRadius(source.radius) || distance >= nearest
                    || _evidence.HasClue(source.clue)) continue;
                nearestSource = source;
                nearest = distance;
            }

            return nearestSource;
        }

        private void FadeOut()
        {
            if (!panel.activeSelf || _visibility.alpha <= 0f)
            {
                HidePanel();
                return;
            }

            if (_writer.isActiveAndEnabled) _writer.StopWriter();
            _visibilityTween = Tween.Alpha(_visibility, 0f, ExitSeconds, Ease.InOutSine, useUnscaledTime: true);
            _visibilityTween.OnComplete(this, view => view.HidePanel());
        }

        private void FadeIn()
        {
            _visibilityTween = Tween.Alpha(_visibility, 1f, EnterSeconds,
                Ease.InOutSine, useUnscaledTime: true);
        }

        private Vector3 GetNoSignalPosition(Vector3 playerPosition, out bool repeat)
        {
            repeat = _lastNoSignalOrigin is Vector3 origin
                && ((Vector2)(playerPosition - origin)).sqrMagnitude < 0.0025f;
            if (repeat)
                return _lastNoSignalPosition;

            _lastNoSignalOrigin = playerPosition;
            _lastNoSignalPosition = playerPosition + (Vector3)offsetAbovePlayer
                + new Vector3(UnityEngine.Random.Range(-0.9f, 0.9f), UnityEngine.Random.Range(0f, 0.35f), 0f);
            return _lastNoSignalPosition;
        }

        private void RefreshTuningHint()
        {
            float difference = _tuning.FrequencyDifference;
            float distance = Mathf.Abs(difference);
            wave.Tune(distance, _source.tolerance);
            if (_tuning.IsInTune) return;

            status.alpha = _statusAlpha;
            wave.SetProgress(0f);
            status.text = difference > 0 ? "Крути колёсико вверх" : "Крути колёсико вниз";
        }

        private void Update()
        {
            if (player.ActiveSkill != SkillType.Hearing) return;

            bool zooming = _input.IsZoomMode;
            _canvas.enabled = !zooming;
            if (_source == null)
            {
                Vector2 displacement = player.transform.position - _lastNoSignalOrigin.Value;
                if (displacement.sqrMagnitude > NoSignalRadius * NoSignalRadius)
                    player.StopHearing();
                return;
            }
            Vector2 directionToPlayer = player.transform.position - _source.transform.position;
            float effectiveRadius = _buffService.AbilityRadius(_source.radius);
            if (directionToPlayer.sqrMagnitude > effectiveRadius * effectiveRadius)
            {
                player.StopHearing();
                return;
            }

            wave.FacePlayer(directionToPlayer);
            if (zooming) return;
            if (_stage == ListeningStage.ReadingTranscript)
                UpdateTranscriptFade();
            else if (_stage == ListeningStage.Tuning)
                UpdateTuning();
        }

        private void UpdateTuning()
        {
            float scroll = _input.Actions.UI.ScrollWheel.ReadValue<Vector2>().y;
            if (scroll != 0f)
            {
                _tuning.Adjust(scroll);
                RefreshTuningHint();
            }

            if (!_tuning.IsInTune) return;

            float progress = _tuning.AdvanceHold(Time.unscaledDeltaTime);
            wave.SetProgress(progress);
            status.alpha = _statusAlpha * (1f - Mathf.SmoothStep(0f, 1f, progress));
            if (!_tuning.IsDecoded) return;

            _stage = ListeningStage.WritingTranscript;
            wave.Flash();
            wave.SetProgress(1f);
            StartWriting(_source.transcript);
        }

        private void StartWriting(string text)
        {
            _writer.enabled = true;
            _writer.SetText(text);
            status.alpha = _statusAlpha;
            _writer.StartWriter();
        }

        private void OnTranscriptFinished(TMPWriter writer)
        {
            if (_stage != ListeningStage.WritingTranscript || _source == null) return;

            _stage = ListeningStage.ReadingTranscript;
            _readingElapsed = 0f;
            _evidence.Collect(_source.clue, _source.transform.position, sourceSprite: hearingIcon);
        }

        private void UpdateTranscriptFade()
        {
            _readingElapsed += Time.unscaledDeltaTime;
            if (_readingElapsed < TranscriptReadSeconds) return;

            float fade = (_readingElapsed - TranscriptReadSeconds) / FadeSeconds;
            _visibility.alpha = 1f - Mathf.SmoothStep(0f, 1f, fade);
            if (fade >= 1f)
                player.StopHearing();
        }

        private void HidePanel()
        {
            _visibility.alpha = 0f;
            panel.SetActive(false);
            _writer.enabled = false;
        }

        private void OnDestroy()
        {
            _visibilityTween.Stop();
            _writer?.OnFinishWriter.RemoveListener(OnTranscriptFinished);
            _subscription?.Dispose();
        }
    }
}
