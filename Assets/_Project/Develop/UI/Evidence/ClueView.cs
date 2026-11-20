using System;
using _Project.Develop.Data.Evidence;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project.Develop.UI.Evidence
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ClueView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private GameObject selectedVisual;
        [SerializeField] private float spawnDuration = 0.3f;
        [SerializeField] private float selectScale = 1.05f;
        [SerializeField] private float hoverScale = 1.03f;
        [SerializeField] private float dragScale = 1.08f;

        private RectTransform _board;
        private Action<ClueView> _onSelected;
        private Vector2 _dragOffset;
        private int? _pointer;
        private bool _dragging;
        private bool _hovered;
        private bool _selected;
        private Vector3 _baseScale;
        private Vector3 _selectionScale;
        private Tween _scaleTween;
        private Tween _selectionTween;
        private Tween _spawnMove;

        public Clue Data { get; private set; }
        public RectTransform Rect => (RectTransform)transform;

        public void Initialize(Clue data, RectTransform board, Vector2 position, Action<ClueView> onSelected)
        {
            Data = data;
            _board = board;
            _onSelected = onSelected;
            icon.sprite = data.icon;
            GetComponentInChildren<TMP_Text>(true).text = data.title;
            _baseScale = Rect.localScale;
            _selectionScale = selectedVisual.transform.localScale;
            selectedVisual.SetActive(false);

            Rect.anchoredPosition = position + Vector2.down * 24f;
            Rect.localScale = _baseScale * 0.8f;
            _scaleTween = Tween.Scale(Rect, _baseScale, spawnDuration, Ease.OutBack);
            _spawnMove = Tween.UIAnchoredPositionY(Rect, position.y, spawnDuration, Ease.OutCubic);
            Tween.Alpha(GetComponent<CanvasGroup>(), 0f, 1f, spawnDuration * 0.8f);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            UpdateScale();
            _selectionTween.Stop();
            var visual = selectedVisual.transform;
            if (selected)
            {
                selectedVisual.SetActive(true);
                visual.localScale = Vector3.zero;
                _selectionTween = Tween.Scale(visual, _selectionScale, 0.2f, Ease.OutBack);
            }
            else
            {
                _selectionTween = Tween.Scale(visual, Vector3.zero, 0.12f, Ease.InBack)
                    .OnComplete(selectedVisual, target => target.SetActive(false));
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHovered(true);
        public void OnPointerExit(PointerEventData eventData) => SetHovered(false);

        private void SetHovered(bool hovered)
        {
            _hovered = hovered;
            UpdateScale();
        }

        private void UpdateScale()
        {
            var idleScale = _selected ? selectScale : 1f;
            var multiplier = _dragging ? dragScale : Mathf.Max(idleScale, _hovered ? hoverScale : 1f);
            _scaleTween.Stop();
            _scaleTween = Tween.Scale(Rect, _baseScale * multiplier, 0.14f, Ease.OutBack);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || _pointer.HasValue ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _board, eventData.position, eventData.pressEventCamera, out var point))
                return;

            _pointer = eventData.pointerId;
            _dragOffset = (Vector2)_board.InverseTransformPoint(Rect.position) - point;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_pointer != eventData.pointerId || eventData.button != PointerEventData.InputButton.Left ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _board, eventData.position, eventData.pressEventCamera, out var point))
                return;

            if (!_dragging)
            {
                _dragging = true;
                _spawnMove.Stop();
                UpdateScale();
            }

            var position = point + _dragOffset;
            var bounds = _board.rect;
            var halfWidth = Rect.rect.width * Rect.lossyScale.x / _board.lossyScale.x * 0.5f;
            var halfHeight = Rect.rect.height * Rect.lossyScale.y / _board.lossyScale.y * 0.5f;
            position.x = Mathf.Clamp(position.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth);
            position.y = Mathf.Clamp(position.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight);
            Rect.position = _board.TransformPoint(position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_pointer != eventData.pointerId || eventData.button != PointerEventData.InputButton.Left)
                return;

            _pointer = null;
            if (_dragging)
            {
                _dragging = false;
                UpdateScale();
                return;
            }

            _onSelected(this);
        }

        private void OnDisable()
        {
            _pointer = null;
            _dragging = false;
            _hovered = false;
        }
    }
}
