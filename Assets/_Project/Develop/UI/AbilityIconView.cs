using System;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Player.Abilities;
using MessagePipe;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.UI
{
    public class AbilityIconView : MonoBehaviour
    {
        [SerializeField] private Transform hearingIcon;
        [SerializeField] private Transform smellIcon;

        private Icon _hearing;
        private Icon _smell;
        private IDisposable _skillSubscription;
        private Tween _transition;
        private Tween _hudFade;
        private IDisposable _hudVisibilitySubscription;
        private float _visibility;
        private Vector2 _weights;
        private bool _initialized;

        [Inject]
        private void Construct(IBufferedSubscriber<SelectedSkillChanged> skills,
            ISubscriber<ZoomModeChanged> zoom, InputController input, IBufferedSubscriber<SavingVisibilityChanged> saving)
        {
            _hearing = new Icon(hearingIcon);
            _smell = new Icon(smellIcon);
            SetVisibility(input.IsZoomMode ? 0f : 1f);
            _hudVisibilitySubscription = new HudVisibilitySubscription(zoom, saving, input.IsZoomMode, SetHidden);
            _skillSubscription = skills.Subscribe(OnSelectedSkillChanged);
        }

        private void SetHidden(bool hidden, bool saveTransition)
        {
            _hudFade.Stop();
            _hudFade = Tween.Custom(this, _visibility, hidden ? 0f : 1f,
                saveTransition ? .075f : hidden ? .18f : .3f, (view, alpha) => view.SetVisibility(alpha),
                Ease.InOutSine, useUnscaledTime: true);
        }

        private void SetVisibility(float alpha)
        {
            _visibility = alpha;
            _hearing.SetVisibility(alpha);
            _smell.SetVisibility(alpha);
        }

        private void OnSelectedSkillChanged(SelectedSkillChanged selected)
        {
            float strength = selected.IsActive ? 1f : .45f;
            Vector2 target = (selected.Skill == SkillType.Hearing ? Vector2.right : Vector2.up) * strength;
            _transition.Stop();
            if (!_initialized)
            {
                Apply(target);
                _initialized = true;
                return;
            }

            _transition = Tween.Custom(this, _weights, target, 0.22f,
                (view, value) => view.Apply(value), Ease.InOutSine, useUnscaledTime: true);
        }

        private void Apply(Vector2 weights)
        {
            _weights = weights;
            _hearing.Apply(weights.x);
            _smell.Apply(weights.y);
        }

        private void OnDestroy()
        {
            _transition.Stop();
            _skillSubscription?.Dispose();
            _hudFade.Stop();
            _hudVisibilitySubscription?.Dispose();
        }

        private class Icon
        {
            private readonly Image _image;
            private readonly Outline _outline;
            private readonly Color _tint;

            public Icon(Transform root)
            {
                _image = root.GetComponent<Image>();
                _tint = _image.color;
                _outline = root.gameObject.AddComponent<Outline>();
                _outline.effectDistance = new Vector2(1.3f, -1.3f);
            }

            public void SetVisibility(float alpha) => _image.canvasRenderer.SetAlpha(alpha);

            public void Apply(float weight)
            {
                var tint = _tint;
                tint.a *= Mathf.Lerp(.42f, 1f, weight);
                _image.color = tint;

                _outline.effectColor = new Color(1f, 1f, 1f, Mathf.Lerp(.24f, .68f, weight));
            }
        }
    }
}
