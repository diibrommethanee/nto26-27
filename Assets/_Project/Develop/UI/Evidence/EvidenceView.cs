using System;
using System.Collections.Generic;
using TMPro;
using _Project.Develop.Data.Evidence;
using _Project.Develop.Gameplay.Evidence;
using Cysharp.Threading.Tasks;
using PrimeTween;
using _Project.Develop.Gameplay.Input;
using MessagePipe;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.UI.Evidence
{
    [RequireComponent(typeof(CanvasGroup))]
    public class EvidenceView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const string DialogueClueLinkPrefix = _Project.Develop.UI.Dialogue.DialogueMarkup.ClueLinkPrefix;

        private Clue _dialogueClue;
        private TMP_Text _dialogueLabel;

        [Header("References")]
        [SerializeField] private RectTransform board;
        [SerializeField] private RectTransform notebookButton;
        [SerializeField] private Image clueFlightImage;

        [Header("Prefabs")]
        [SerializeField] private ClueView cluePrefab;
        [SerializeField] private ClueView wordCluePrefab;
        [SerializeField] private ClueThreadView threadPrefab;
        [SerializeField] private Sprite redSticker;
        [SerializeField] private Sprite blueSticker;
        [SerializeField] private Sprite greenSticker;
        [Header("Notebook Pages")]
        [SerializeField] private Image matLeft;
        [SerializeField] private Image matRight;
        [SerializeField] private Image surfaceLeft;
        [SerializeField] private Image surfaceRight;
        [SerializeField] private List<Hypothesis> hypotheses = new();
        [SerializeField, HideInInspector] private Hypothesis currentHypothesis;

        [Header("Animations")]
        [SerializeField] private float openDuration = 0.32f;
        [SerializeField] private float closeDuration = 0.18f;
        [SerializeField] private float slideDistance = 30f;
        [SerializeField] private float closedScale = 0.94f;
        [SerializeField, Min(0.1f)] private float clueFlyDuration = 0.55f;
        [SerializeField, Min(0f)] private float clueFlyArc = 90f;
        [SerializeField, Min(8f)] private float clueFlyIconSize = 48f;

        private readonly Dictionary<Clue, ClueView> _cards = new();
        private EvidenceService _evidence;
        private CanvasGroup _canvasGroup;
        private CanvasGroup _notebookButtonGroup;
        private InputController _input;
        private ISubscriber<ZoomModeChanged> _zoomMode;
        private IBufferedSubscriber<SavingVisibilityChanged> _saving;
        private IDisposable _zoomSubscription;
        private Tween _notebookFade;
        private NotebookPages _pages;
        private ClueConnections _connections;
        private ClueFlightAnimation _flight;
        private bool _isOpen;
        private Sequence _transition;
        private Vector3 _openScale;
        private float _openY;

        [Inject]
        private void Construct(EvidenceService evidence, InputController input, ISubscriber<ZoomModeChanged> zoomMode,
            IBufferedSubscriber<SavingVisibilityChanged> saving)
        {
            _evidence = evidence;
            _input = input;
            _zoomMode = zoomMode;
            _saving = saving;
        }

        private void Start()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (notebookButton)
            {
                _flight = new ClueFlightAnimation(notebookButton, clueFlightImage, clueFlyDuration,
                    clueFlyArc, clueFlyIconSize, destroyCancellationToken);
                _notebookButtonGroup = notebookButton.GetComponent<CanvasGroup>();
                if (!_notebookButtonGroup)
                    _notebookButtonGroup = notebookButton.gameObject.AddComponent<CanvasGroup>();
                SetNotebookButtonVisible(!_input.IsZoomMode);
                _zoomSubscription = new HudVisibilitySubscription(_zoomMode, _saving, _input.IsZoomMode, SetHudHidden);
            }
            _openScale = board.localScale;
            _openY = board.anchoredPosition.y;
            _isOpen = _canvasGroup.interactable && _canvasGroup.blocksRaycasts && _canvasGroup.alpha > 0.99f;
            _input.SetNotebookOpen(_isOpen);
            _canvasGroup.alpha = _isOpen ? 1f : 0f;
            _canvasGroup.SetInteractive(_isOpen);
            if (!_isOpen)
            {
                board.localScale = _openScale * closedScale;
                board.anchoredPosition -= Vector2.up * slideDistance;
            }

            _connections = new ClueConnections(board, threadPrefab, _evidence);
            _pages = new NotebookPages(board, matLeft, matRight,
                surfaceLeft, surfaceRight, _evidence, _connections.ClearSelection);
            if (hypotheses.Count == 0 && currentHypothesis != null)
                _pages.Add(currentHypothesis);
            foreach (var hypothesis in hypotheses)
                _pages.Add(hypothesis);
            foreach (var hypothesis in _evidence.UnlockedHypotheses)
                _pages.Add(hypothesis);
            _pages.ShowFirst();
            _evidence.HypothesisUnlocked += OnHypothesisUnlocked;
            _evidence.ClueCollected += OnClueCollected;
            _evidence.Restored += RefreshRestored;
            foreach (var clue in _evidence.Collected)
                AddClueCard(clue, null);
        }

        private void RefreshRestored()
        {
            foreach (var hypothesis in _evidence.UnlockedHypotheses) OnHypothesisUnlocked(hypothesis);
            foreach (var clue in _evidence.Collected)
                AddClueCard(clue, null);
        }

        private void OnHypothesisUnlocked(Hypothesis hypothesis)
        {
            _connections.ClearSelection();
            _pages.Add(hypothesis);
            _pages.Stop();
            var destination = _pages.ClueArea(hypothesis);
            foreach (var clue in _evidence.Collected)
            {
                if (clue.hypothesis == hypothesis && _cards.TryGetValue(clue, out var card) && card.Rect.parent != destination)
                    card.MoveToPage(destination, GetAutomaticSpawnPosition(destination));
            }
            _connections.RefreshPages(_pages);
            _pages.Stop();
        }

        private void SetHudHidden(bool hidden, bool saveTransition)
        {
            _notebookFade.Stop();
            _notebookButtonGroup.SetInteractive(false);
            var visible = !hidden;
            _notebookFade = Tween.Alpha(_notebookButtonGroup, visible ? 1f : 0f, saveTransition ? .075f : visible ? .28f : .18f,
                Ease.OutCubic, useUnscaledTime: true);
            if (visible)
                _notebookFade.OnComplete(this, view => view.SetNotebookButtonVisible(true));
        }

        private void SetNotebookButtonVisible(bool visible)
        {
            _notebookButtonGroup.alpha = visible ? 1f : 0f;
            _notebookButtonGroup.SetInteractive(visible);
        }

        private void OnDestroy()
        {
            _zoomSubscription?.Dispose();
            _notebookFade.Stop();
            _transition.Stop();
            _pages?.CancelAnimation();
            if (_evidence != null)
            {
                _evidence.HypothesisUnlocked -= OnHypothesisUnlocked;
                _evidence.ClueCollected -= OnClueCollected;
                _evidence.Restored -= RefreshRestored;
            }
        }

        private void SelectClue(ClueView card)
        {
            _connections.Select(card, _pages.ThreadArea((RectTransform)card.Rect.parent));
        }

        public void SpawnClue(Clue clue, Vector2 position)
        {
            _evidence.Collect(clue, boardPosition: position);
        }

        private void OnClueCollected(Clue clue, Vector3? sourceWorldPosition, Vector2? boardPosition)
        {
            _pages.RefreshClues();
            var label = !sourceWorldPosition.HasValue && clue == _dialogueClue ? _dialogueLabel : null;
            PresentClueAsync(clue, sourceWorldPosition, boardPosition, label).Forget();
        }

        public void ContinueWithClueText(Clue clue, TMP_Text label, Action continueDialogue)
        {
            _dialogueClue = clue;
            _dialogueLabel = label;
            try
            {
                continueDialogue();
            }
            finally
            {
                _dialogueClue = null;
                _dialogueLabel = null;
            }
        }

        private async UniTask PresentClueAsync(Clue clue, Vector3? sourceWorldPosition, Vector2? boardPosition,
            TMP_Text dialogueLabel)
        {
            if (dialogueLabel && notebookButton)
                await _flight.FlyDialogueClueAsync(dialogueLabel);
            else if (sourceWorldPosition is Vector3 worldPosition && notebookButton != null)
            {
                var sourceSprite = _evidence.GetClueSprite(clue);
                if (sourceSprite == null) sourceSprite = clue.icon;
                var isOutlineSticker = clue.type == ClueType.Outline && sourceSprite == null;
                await _flight.FlyClueToNotebookAsync(sourceSprite != null ? sourceSprite : GetSticker(clue.sticker),
                    worldPosition, isOutlineSticker);
            }

            AddClueCard(clue, boardPosition);
        }

        private void AddClueCard(Clue clue, Vector2? position)
        {
            if (_cards.ContainsKey(clue))
                return;

            _connections.ClearSelection();
            var area = _pages.ClueArea(clue.hypothesis);
            var icon = clue.icon;
            if (icon == null && clue.type == ClueType.Collectible)
                icon = _evidence.GetClueSprite(clue);

            var hasIcon = icon != null;
            var prefab = hasIcon ? cluePrefab : wordCluePrefab;
            var spawnPosition = position ?? GetAutomaticSpawnPosition(area);
            var card = Instantiate(prefab, area);
            _cards.Add(clue, card);
            _pages.ShowCluePage(clue.hypothesis);
            card.Initialize(clue, area, spawnPosition, SelectClue, hasIcon ? icon : GetSticker(clue.sticker), hasIcon);
        }

        private Sprite GetSticker(ClueSticker sticker)
        {
            return sticker switch
            {
                ClueSticker.Blue => blueSticker,
                ClueSticker.Green => greenSticker,
                _ => redSticker
            };
        }

        private Vector2 GetAutomaticSpawnPosition(RectTransform area)
        {
            var bounds = area.rect;
            return new Vector2(
                Mathf.Lerp(bounds.xMin, bounds.xMax, UnityEngine.Random.Range(.16f, .84f)),
                Mathf.Lerp(bounds.yMin, bounds.yMax, UnityEngine.Random.Range(.16f, .68f)));
        }
        public void SetCurrentHypothesis(Hypothesis hypothesis)
        {
            if (hypothesis != null)
                _evidence.UnlockHypothesis(hypothesis);
            _pages.Show(hypothesis);
        }

        public bool IsNotebookOpen => _isOpen;

        public void Open() => SetOpen(true);

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isOpen && _canvasGroup.interactable)
                _pages.BeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData) => _pages.Drag(eventData);

        public void OnEndDrag(PointerEventData eventData) => _pages.EndDrag(eventData);

        public void Close()
        {
            _connections.ClearSelection();
            _pages.Stop();
            SetOpen(false);
        }

        private void SetOpen(bool open)
        {
            if (_isOpen == open)
                return;

            _isOpen = open;
            _input.SetNotebookOpen(open);
            _transition.Stop();
            _canvasGroup.SetInteractive(false);
            var duration = open ? openDuration : closeDuration;
            _transition = Sequence.Create()
                .Group(Tween.Scale(board, open ? _openScale : _openScale * closedScale,
                    duration, open ? Ease.OutBack : Ease.InBack))
                .Group(Tween.UIAnchoredPositionY(board, open ? _openY : _openY - slideDistance,
                    duration, open ? Ease.OutCubic : Ease.InCubic))
                .Group(Tween.Alpha(_canvasGroup, open ? 1f : 0f, duration, Ease.Linear));
            if (open)
                _transition.OnComplete(this, view => view._canvasGroup.SetInteractive(true));
        }
    }
}
