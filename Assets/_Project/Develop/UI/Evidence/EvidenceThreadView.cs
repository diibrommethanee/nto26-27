
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _Project.Develop.UI.Evidence
{
    public class EvidenceThreadView : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private Graphic lineGraphic;

        private RectTransform _board;
        private RectTransform _first;
        private RectTransform _second;

        private Camera _uiCamera;

        private bool _isVisible;
        private bool _followMouse;

        private void Awake()
        {
            if (rect == null)
                rect = GetComponent<RectTransform>();

            if (lineGraphic == null)
                lineGraphic = rect.GetComponent<Graphic>();

            if (lineGraphic != null)
            {
                lineGraphic.enabled = false;
                lineGraphic.raycastTarget = false;
            }
        }

        public void Initialize(RectTransform board)
        {
            _board = board;

            var canvas = board.GetComponentInParent<Canvas>()?.rootCanvas;

            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                _uiCamera = canvas.worldCamera;
        }

        public void FollowMouse(RectTransform first)
        {
            _first = first;
            _second = null;

            _followMouse = true;
            SetVisible(true);
        }

        public void Connect(RectTransform first, RectTransform second)
        {
            _first = first;
            _second = second;

            _followMouse = false;
            SetVisible(true);
        }

        public void Hide()
        {
            _first = null;
            _second = null;

            _followMouse = false;
            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (!_isVisible || _first == null || _board == null)
                return;

            Vector2 firstPosition = _board.InverseTransformPoint(_first.position);
            Vector2 secondPosition;

            if (_followMouse)
            {
                if (Mouse.current == null)
                    return;

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _board,
                        Mouse.current.position.ReadValue(),
                        _uiCamera,
                        out secondPosition))
                    return;
            }
            else
            {
                if (_second == null)
                {
                    Hide();
                    return;
                }

                secondPosition = _board.InverseTransformPoint(_second.position);
            }

            Draw(firstPosition, secondPosition);
        }

        private void Draw(Vector2 first, Vector2 second)
        {
            var direction = second - first;

            rect.localPosition = (first + second) / 2f;

            rect.sizeDelta = new Vector2(
                direction.magnitude,
                rect.sizeDelta.y
            );

            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            rect.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;

            if (lineGraphic != null)
                lineGraphic.enabled = visible;
        }
    }
}
