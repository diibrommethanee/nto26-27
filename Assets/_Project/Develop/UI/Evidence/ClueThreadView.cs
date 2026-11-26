using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Develop.UI.Evidence
{
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class ClueThreadView : MonoBehaviour
    {
        private RectTransform _rect;
        private RectTransform _container;
        private RectTransform _first;
        private RectTransform _second;
        private UnityEngine.UI.Image _line;
        private Camera _uiCamera;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _container = (RectTransform)_rect.parent;
            _line = GetComponent<UnityEngine.UI.Image>();
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            _uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Hide();
        }

        public void FollowMouse(RectTransform first)
        {
            Connect(first, null);
        }

        public void Connect(RectTransform first, RectTransform second)
        {
            _first = first;
            _second = second;
            enabled = true;
            UpdateLine();
        }

        public void Hide()
        {
            _line.enabled = false;
            enabled = false;
        }

        public void RefreshPage(RectTransform board, System.Func<RectTransform, RectTransform> threadArea)
        {
            var together = _first.parent == _second.parent;
            _container = together ? threadArea((RectTransform)_first.parent) : board;
            _rect.SetParent(_container, false);
            _rect.SetAsFirstSibling();
            if (together)
                Connect(_first, _second);
            else
                Hide();
        }

        private void LateUpdate()
        {
            UpdateLine();
        }

        private void UpdateLine()
        {
            Vector2 end;
            if (_second != null)
            {
                end = _container.InverseTransformPoint(_second.position);
            }
            else if (Mouse.current == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                         _container, Mouse.current.position.ReadValue(), _uiCamera, out end))
            {
                _line.enabled = false;
                return;
            }

            var start = (Vector2)_container.InverseTransformPoint(_first.position);
            var mask = _container.GetComponent<UnityEngine.UI.RectMask2D>();
            if (mask != null)
            {
                var bounds = _container.rect;
                var padding = mask.padding;
                var clearance = _rect.rect.height * 0.5f;
                var min = bounds.min + new Vector2(padding.x + clearance, padding.y + clearance);
                var max = bounds.max - new Vector2(padding.z + clearance, padding.w + clearance);
                start = Vector2.Max(min, Vector2.Min(max, start));

                // Shorten along the thread so the end keeps its original angle.
                var delta = end - start;
                var fraction = 1f;
                if (end.x < min.x || end.x > max.x)
                    fraction = Mathf.Min(fraction, ((delta.x < 0f ? min.x : max.x) - start.x) / delta.x);
                if (end.y < min.y || end.y > max.y)
                    fraction = Mathf.Min(fraction, ((delta.y < 0f ? min.y : max.y) - start.y) / delta.y);
                end = start + delta * fraction;
            }

            var direction = end - start;
            _rect.localPosition = (start + end) * 0.5f;
            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, direction.magnitude);
            _rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            _line.enabled = true;
        }
    }
}
