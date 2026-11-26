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
        [SerializeField] protected UnityEngine.UI.Image icon;
        [SerializeField] protected TMP_Text title;
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

        public virtual void Initialize(Clue data, RectTransform board, Vector2 position, Action<ClueView> onSelected,
            Sprite artwork = null)
        {
            Data = data;
            _board = board;
            _onSelected = onSelected;
            var word = data as WordClue;
            var pickup = data.icon != null;
            icon.sprite = artwork != null ? artwork : word != null && word.noteSprite != null ? word.noteSprite : data.icon;
            icon.preserveAspect = pickup;
            title.gameObject.SetActive(!pickup);
            title.text = word != null ? word.text : data.title;
            if (!pickup)
            {
                Rect.sizeDelta = word != null ? new Vector2(172f, 174f) : new Vector2(132f, 150f);
                icon.rectTransform.anchorMin = Vector2.zero;
                icon.rectTransform.anchorMax = Vector2.one;
                icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
                title.rectTransform.anchorMin = Vector2.zero;
                title.rectTransform.anchorMax = Vector2.one;
                title.rectTransform.offsetMin = new Vector2(19f, 28f);
                title.rectTransform.offsetMax = new Vector2(-18f, -20f);
                title.enableAutoSizing = true;
                title.fontSizeMin = 11f;
                title.fontSizeMax = word != null ? 24f : 22f;
                title.alignment = TextAlignmentOptions.Center;
            }
            _baseScale = Rect.localScale;
            _selectionScale = selectedVisual.transform.localScale;
            selectedVisual.SetActive(false);

            position = ClampToBoard(position);
            Rect.anchoredPosition = ClampToBoard(position + Vector2.down * 24f);
            Rect.localScale = _baseScale * 0.8f;
            _scaleTween = Tween.Scale(Rect, _baseScale, spawnDuration, Ease.OutBack);
            _spawnMove = Tween.UIAnchoredPositionY(Rect, position.y, spawnDuration, Ease.OutCubic);
            Tween.Alpha(GetComponent<CanvasGroup>(), 0f, 1f, spawnDuration * 0.8f);
        }

        public void MoveToPage(RectTransform area, Vector2 position)
        {
            _spawnMove.Stop();
            _board = area;
            Rect.SetParent(area, false);
            Rect.anchoredPosition = ClampToBoard(position);
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
            var targetScale = _baseScale * multiplier;
            _scaleTween.Stop();
            if (Rect.localScale != targetScale)
                _scaleTween = Tween.Scale(Rect, targetScale, 0.14f, Ease.OutBack);
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

            var position = ClampToBoard(point + _dragOffset);
            Rect.position = _board.TransformPoint(position);
        }

        private Vector2 ClampToBoard(Vector2 position)
        {
            var bounds = _board.rect;
            var maxScale = Mathf.Max(1f, Mathf.Max(dragScale, Mathf.Max(selectScale, hoverScale)));
            var halfWidth = Rect.rect.width * Mathf.Abs(_baseScale.x) * maxScale * 0.5f;
            var halfHeight = Rect.rect.height * Mathf.Abs(_baseScale.y) * maxScale * 0.5f;

            var minX = bounds.xMin + halfWidth;
            var maxX = bounds.xMax - halfWidth;
            var minY = bounds.yMin + halfHeight;
            var maxY = bounds.yMax - halfHeight;
            position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : bounds.center.x;
            position.y = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : bounds.center.y;
            return position;
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
