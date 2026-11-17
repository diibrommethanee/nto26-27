
using _Project.Develop.Data.Evidence;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.Develop.UI.Evidence
{
    public class EvidenceView : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IDragHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private GameObject selectedVisual;

        [Header("Animations")]
        [SerializeField] private float spawnDuration = 0.3f;
        [SerializeField] private float selectScale = 1.05f;
        [SerializeField] private float hoverScale = 1.03f;
        [SerializeField] private float dragScale = 1.08f;

        private EvidenceBoardController _controller;
        private CanvasGroup _canvasGroup;

        public EvidenceData Data { get; private set; }
        public RectTransform Rect { get; private set; }

        private Vector2 _dragOffset;
        private int _pointerId;

        private Vector3 _baseScale;
        private Vector3 _selectedVisualScale;

        private bool _pointerHeld;
        private bool _isDragging;
        private bool _canDrag;
        private bool _isSelected;
        private bool _isHovered;

        private void Awake()
        {
            Rect = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            if (Rect == null)
            {
                return;
            }

            _baseScale = Rect.localScale;

            if (selectedVisual != null)
            {
                var visualTransform = selectedVisual.transform;

                _selectedVisualScale = visualTransform.localScale;

                visualTransform.localScale = Vector3.zero;
                selectedVisual.SetActive(false);

                foreach (var graphic in selectedVisual.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
            }
        }

        public void Initialize(EvidenceData data, EvidenceBoardController controller)
        {
            Data = data;
            _controller = controller;

            icon.sprite = data.icon;
            title.text = data.title;

            SetSelected(false);

            PlaySpawnAnimation();
        }

        private void PlaySpawnAnimation()
        {
            var targetPosition = Rect.anchoredPosition;

            Rect.localScale = _baseScale * 0.8f;
            Rect.anchoredPosition += Vector2.down * 24f;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            Tween.Scale(
                Rect,
                _baseScale,
                spawnDuration,
                Ease.OutBack
            );

            Tween.UIAnchoredPositionY(
                Rect,
                targetPosition.y,
                spawnDuration,
                Ease.OutCubic
            );

            if (_canvasGroup != null)
            {
                Tween.Custom(
                    _canvasGroup,
                    0f,
                    1f,
                    spawnDuration * 0.8f,
                    (target, alpha) => target.alpha = alpha
                );
            }
        }

        public void SetSelected(bool selected)
        {
            if (_isSelected == selected)
                return;

            _isSelected = selected;

            AnimateScale();

            if (selectedVisual == null)
                return;

            var visualTransform = selectedVisual.transform;

            Tween.StopAll(onTarget: visualTransform);

            if (selected)
            {
                selectedVisual.SetActive(true);
                visualTransform.localScale = Vector3.zero;

                Tween.Scale(
                    visualTransform,
                    _selectedVisualScale,
                    0.2f,
                    Ease.OutBack
                );
            }
            else
            {
                Tween.Scale(
                    visualTransform,
                    Vector3.zero,
                    0.12f,
                    Ease.InBack
                ).OnComplete(
                    target: this,
                    target => target.HideSelectedVisual()
                );
            }
        }

        private void HideSelectedVisual()
        {
            if (!_isSelected && selectedVisual != null)
                selectedVisual.SetActive(false);
        }

        private void AnimateScale()
        {
            var multiplier = 1f;

            if (_isSelected)
                multiplier = selectScale;

            if (_isHovered)
                multiplier = Mathf.Max(multiplier, hoverScale);

            if (_isDragging)
                multiplier = dragScale;

            Tween.Scale(
                Rect,
                _baseScale * multiplier,
                0.14f,
                Ease.OutBack
            );
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            AnimateScale();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            AnimateScale();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _pointerHeld = true;
            _isDragging = false;
            _pointerId = eventData.pointerId;

            var board = _controller.Board;

            _canDrag = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                board,
                eventData.position,
                eventData.pressEventCamera,
                out var pointerPosition
            );

            if (!_canDrag)
                return;

            var cardPosition = board.InverseTransformPoint(Rect.position);
            _dragOffset = (Vector2)cardPosition - pointerPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_pointerHeld ||
                eventData.pointerId != _pointerId ||
                eventData.button != PointerEventData.InputButton.Left ||
                !_canDrag)
                return;

            if (!_isDragging)
            {
                _isDragging = true;
                AnimateScale();
            }

            var board = _controller.Board;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    board,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var position))
                return;

            position += _dragOffset;

            var bounds = board.rect;

            var halfWidth = Rect.rect.width *
                            (Rect.lossyScale.x / board.lossyScale.x) * 0.5f;

            var halfHeight = Rect.rect.height *
                             (Rect.lossyScale.y / board.lossyScale.y) * 0.5f;

            position.x = Mathf.Clamp(
                position.x,
                bounds.xMin + halfWidth,
                bounds.xMax - halfWidth
            );

            position.y = Mathf.Clamp(
                position.y,
                bounds.yMin + halfHeight,
                bounds.yMax - halfHeight
            );

            Rect.position = board.TransformPoint(position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pointerHeld || eventData.pointerId != _pointerId)
                return;

            var wasDragging = _isDragging;

            if (eventData.button == PointerEventData.InputButton.Left && !wasDragging)
                _controller.SelectEvidence(this);

            _pointerHeld = false;
            _isDragging = false;
            _canDrag = false;

            if (wasDragging)
                AnimateScale();
        }

        private void OnDisable()
        {
            _pointerHeld = false;
            _isDragging = false;
            _canDrag = false;
            _isHovered = false;
        }
    }
}
