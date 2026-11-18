using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Develop.UI.Evidence
{
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class EvidenceThreadView : MonoBehaviour
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
            var direction = end - start;
            _rect.localPosition = (start + end) * 0.5f;
            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, direction.magnitude);
            _rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            _line.enabled = true;
        }
    }
}
