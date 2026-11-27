using _Project.Develop.Gameplay.Input;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.UI
{
    [RequireComponent(typeof(Image))]
    public class CheckpointIndicator : MonoBehaviour
    {
        [SerializeField, Min(.01f)] private float fadeDuration = .24f;
        [SerializeField, Min(0f)] private float minimumVisibleTime = .66f;

        [Inject] private IBufferedPublisher<SavingVisibilityChanged> _visibility;
        private Image _image;
        private Vector3 _originalScale;
        private Vector2 _restPosition;
        private Quaternion _restRotation;
        private float _startedAt;
        private float _progress;
        private float _until;
        private bool _saving;
        private bool _showing;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _originalScale = transform.localScale;
            _restPosition = _image.rectTransform.anchoredPosition;
            _restRotation = transform.localRotation;
            _image.enabled = false;
        }

        public void SetSaving(bool saving)
        {
            _saving = saving;
            if (!saving) return;
            gameObject.SetActive(true);
            _until = Time.unscaledTime + Mathf.Max(minimumVisibleTime, fadeDuration);
            if (_showing) return;
            _showing = true;
            _startedAt = Time.unscaledTime;
            _image.enabled = true;
            ApplyAppearance();
            _visibility.Publish(new SavingVisibilityChanged(true));
        }

        private void Update()
        {
            if (!_showing) return;
            bool visible = _saving || Time.unscaledTime < _until;
            _progress = Mathf.MoveTowards(_progress, visible ? 1f : 0f, Time.unscaledDeltaTime / fadeDuration);
            ApplyAppearance();
            if (visible || _progress > 0f) return;
            _showing = false;
            _image.enabled = false;
            transform.localScale = _originalScale;
            _image.rectTransform.anchoredPosition = _restPosition;
            transform.localRotation = _restRotation;
            gameObject.SetActive(false);
            _visibility.Publish(new SavingVisibilityChanged(false));
        }

        private void ApplyAppearance()
        {
            float alpha = Mathf.SmoothStep(0f, 1f, _progress);
            _image.canvasRenderer.SetAlpha(alpha);
            float elapsed = Time.unscaledTime - _startedAt;
            float stroke = Mathf.Clamp01((elapsed - fadeDuration * .55f) / .42f);
            float arc = Mathf.Sin(stroke * Mathf.PI);
            float flick = Mathf.Sin(stroke * 2f * Mathf.PI) * arc;
            float landing = 1f - Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration);
            float lift = (1f - alpha) * (1f - landing);
            float unit = _image.rectTransform.rect.height;
            var offset = new Vector2(-.12f * landing + .10f * arc + .07f * lift,
                .14f * landing - .055f * flick + .10f * lift);
            _image.rectTransform.anchoredPosition = _restPosition + offset * unit;
            transform.localRotation = _restRotation * Quaternion.Euler(0f, 0f,
                -9f * landing + 4f * flick + 5f * lift);
            transform.localScale = _originalScale * (1f + .21f * landing - .025f * arc);
        }
    }
}
