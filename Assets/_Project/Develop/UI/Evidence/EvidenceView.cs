using System;
using System.Collections.Generic;
using TMPro;
using _Project.Develop.Data.Evidence;
using _Project.Develop.Gameplay.Evidence;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.UI.Evidence
{
    [RequireComponent(typeof(CanvasGroup))]
    public class EvidenceView : MonoBehaviour
    {
        public const string DialogueClueLinkPrefix = "dialogue-clue-";

        private Clue _dialogueClue;
        private TMP_Text _dialogueLabel;

        [Header("References")]
        [SerializeField] private RectTransform board;
        [SerializeField] private RectTransform clueContainer;
        [SerializeField] private RectTransform threadContainer;
        [SerializeField] private RectTransform notebookButton;

        [Header("Prefabs")]
        [SerializeField] private ClueView cluePrefab;
        [SerializeField] private ClueThreadView threadPrefab;
        [SerializeField] private Hypothesis currentHypothesis;

        [Header("Animations")]
        [SerializeField] private float openDuration = 0.32f;
        [SerializeField] private float closeDuration = 0.18f;
        [SerializeField] private float slideDistance = 30f;
        [SerializeField] private float closedScale = 0.94f;
        [SerializeField, Min(0.1f)] private float clueFlyDuration = 0.55f;
        [SerializeField, Min(0f)] private float clueFlyArc = 90f;
        [SerializeField, Min(8f)] private float clueFlyIconSize = 48f;

        private readonly HashSet<(ClueView, ClueView)> _combinedPairs = new();
        private int _cardCount;
        private EvidenceService _evidence;
        private CanvasGroup _canvasGroup;
        private HypothesisView _hypothesisView;
        private ClueView _selected;
        private ClueThreadView _preview;
        private bool _isOpen;
        private Sequence _transition;
        private Vector3 _openScale;
        private float _openY;

        [Inject]
        private void Construct(EvidenceService evidence)
        {
            _evidence = evidence;
        }

        private void Start()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _openScale = board.localScale;
            _openY = board.anchoredPosition.y;
            _isOpen = _canvasGroup.interactable && _canvasGroup.blocksRaycasts && _canvasGroup.alpha > 0.99f;
            _canvasGroup.alpha = _isOpen ? 1f : 0f;
            SetInteractive(_isOpen);
            if (!_isOpen)
            {
                board.localScale = _openScale * closedScale;
                board.anchoredPosition -= Vector2.up * slideDistance;
            }

            _hypothesisView = board.GetComponentInChildren<HypothesisView>(true);
            _hypothesisView.Initialize(_evidence);
            _evidence.ClueCollected += OnClueCollected;
            _hypothesisView.Show(currentHypothesis);
            foreach (var clue in _evidence.Collected)
                AddClueCard(clue, GetAutomaticSpawnPosition());
        }

        private void OnDestroy()
        {
            _transition.Stop();
            if (_evidence != null)
                _evidence.ClueCollected -= OnClueCollected;
        }

        private void SelectClue(ClueView card)
        {
            if (_selected == null)
            {
                _selected = card;
                card.SetSelected(true);
                if (_preview == null)
                    _preview = Instantiate(threadPrefab, threadContainer);

                _preview.FollowMouse(card.Rect);
                return;
            }

            ConnectClues(_selected, card);
            ClearSelection();
        }

        private void ConnectClues(ClueView first, ClueView second)
        {
            if (first == second ||
                _combinedPairs.Contains((first, second)) ||
                _combinedPairs.Contains((second, first)) ||
                !_evidence.TryCombine(first.Data, second.Data, out var result))
                return;

            _combinedPairs.Add((first, second));
            _preview.Connect(first.Rect, second.Rect);
            _preview = null;

            var midpoint = (first.Rect.anchoredPosition + second.Rect.anchoredPosition) * 0.5f;
            SpawnClue(result, midpoint + Vector2.up * 100f);
        }

        private void ClearSelection()
        {
            if (_selected == null)
                return;

            _selected.SetSelected(false);
            _selected = null;
            if (_preview != null)
                _preview.Hide();
        }

        public void SpawnClue(Clue clue, Vector2 position)
        {
            _evidence.Collect(clue, boardPosition: position);
        }

        private void OnClueCollected(Clue clue, Vector3? sourceWorldPosition, Vector2? boardPosition)
        {
            _hypothesisView.RefreshClues();
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
                await FlyDialogueClueAsync(dialogueLabel);
            else if (sourceWorldPosition is Vector3 worldPosition && notebookButton != null)
                await FlyClueToNotebookAsync(clue.icon, worldPosition);

            AddClueCard(clue, boardPosition ?? GetAutomaticSpawnPosition());
        }

        private UniTask FlyDialogueClueAsync(TMP_Text source)
        {
            var targetCanvas = notebookButton.GetComponentInParent<Canvas>().rootCanvas;
            var canvasRect = (RectTransform)targetCanvas.transform;
            var sourceCanvas = source.canvas.rootCanvas;
            var sourceCamera = sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : sourceCanvas.worldCamera;
            var targetCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
            var jobs = new List<UniTask>();
            var info = source.textInfo;
            for (var i = 0; i < info.linkCount; i++)
            {
                var link = info.linkInfo[i];
                if (!link.GetLinkID().StartsWith(DialogueClueLinkPrefix, StringComparison.Ordinal))
                    continue;

                var bounds = new Bounds();
                var hasGlyph = false;
                for (var j = link.linkTextfirstCharacterIndex;
                     j < link.linkTextfirstCharacterIndex + link.linkTextLength && j < info.characterCount; j++)
                {
                    var glyph = info.characterInfo[j];
                    if (!glyph.isVisible) continue;
                    if (!hasGlyph) bounds = new Bounds(glyph.bottomLeft, Vector3.zero);
                    bounds.Encapsulate(glyph.bottomLeft);
                    bounds.Encapsulate(glyph.topRight);
                    hasGlyph = true;
                }

                if (!hasGlyph) continue;
                var screen = RectTransformUtility.WorldToScreenPoint(sourceCamera,
                    source.rectTransform.TransformPoint(bounds.center));
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, targetCamera,
                        out var position))
                    jobs.Add(FlyPhraseAsync(link.GetLinkText(), source, canvasRect, position));
            }

            return UniTask.WhenAll(jobs);
        }

        private async UniTask FlyPhraseAsync(string phrase, TMP_Text source, RectTransform canvas, Vector2 position)
        {
            var flight = new GameObject("Dialogue Clue Flight", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)flight.transform;
            rect.SetParent(canvas, false);
            rect.localPosition = position;
            var label = flight.GetComponent<TextMeshProUGUI>();
            label.font = source.font;
            label.fontSize = source.fontSize * source.rectTransform.lossyScale.y / canvas.lossyScale.y;
            label.color = new Color32(255, 213, 79, 255);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.richText = false;
            label.text = phrase;
            rect.sizeDelta = label.GetPreferredValues(phrase) + new Vector2(8, 4);

            try
            {
                var target = canvas.InverseTransformPoint(notebookButton.position);
                Tween.Scale(rect, 0.35f, clueFlyDuration, Ease.InCubic);
                await Tween.LocalPosition(rect, target, clueFlyDuration, Ease.InOutCubic)
                    .SetCancellationToken(destroyCancellationToken);
            }
            finally
            {
                Destroy(flight);
            }
        }

        private async UniTask FlyClueToNotebookAsync(Sprite icon, Vector3 worldPosition)
        {
            var canvas = notebookButton.GetComponentInParent<Canvas>().rootCanvas;
            var canvasRect = (RectTransform)canvas.transform;
            var worldCamera = Camera.main;
            if (worldCamera == null)
                return;

            var screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
            var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (screenPosition.z <= 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPosition, eventCamera, out var startPosition))
                return;

            var flightObject = new GameObject("Collected Clue Flight", typeof(Image));
            var flightRect = (RectTransform)flightObject.transform;
            flightRect.SetParent(canvasRect, false);
            flightRect.anchorMin = flightRect.anchorMax = new Vector2(0.5f, 0.5f);
            flightRect.localPosition = startPosition;
            flightRect.sizeDelta = new Vector2(clueFlyIconSize, clueFlyIconSize);

            var flightImage = flightObject.GetComponent<Image>();
            flightImage.sprite = icon;
            flightImage.preserveAspect = true;
            flightImage.raycastTarget = false;

            try
            {
                var targetPosition = (Vector2)canvasRect.InverseTransformPoint(notebookButton.position);
                await Tween.Custom(flightRect, 0f, 1f, clueFlyDuration, (rect, progress) =>
                {
                    var position = Vector2.LerpUnclamped(startPosition, targetPosition, progress);
                    position.y += Mathf.Sin(progress * Mathf.PI) * clueFlyArc;
                    rect.localPosition = position;
                    rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.45f, progress);
                }, Ease.InOutCubic).SetCancellationToken(destroyCancellationToken);
            }
            finally
            {
                Destroy(flightObject);
            }
        }

        private void AddClueCard(Clue clue, Vector2 position)
        {
            Instantiate(cluePrefab, clueContainer).Initialize(clue, board, position, SelectClue);
            _cardCount++;
        }

        private Vector2 GetAutomaticSpawnPosition()
        {
            var index = _cardCount;
            return new Vector2((index % 3) * 24f, -(index / 3) * 24f);
        }

        public void SetCurrentHypothesis(Hypothesis hypothesis)
        {
            currentHypothesis = hypothesis;
            _hypothesisView.Show(hypothesis);
        }

        public void Open() => SetOpen(true);

        public void Close()
        {
            ClearSelection();
            _hypothesisView.CloseChoices();
            SetOpen(false);
        }

        private void SetOpen(bool open)
        {
            if (_isOpen == open)
                return;

            _isOpen = open;
            _transition.Stop();
            SetInteractive(false);
            var duration = open ? openDuration : closeDuration;
            _transition = Sequence.Create()
                .Group(Tween.Scale(board, open ? _openScale : _openScale * closedScale,
                    duration, open ? Ease.OutBack : Ease.InBack))
                .Group(Tween.UIAnchoredPositionY(board, open ? _openY : _openY - slideDistance,
                    duration, open ? Ease.OutCubic : Ease.InCubic))
                .Group(Tween.Alpha(_canvasGroup, open ? 1f : 0f, duration, Ease.Linear));
            if (open)
                _transition.OnComplete(this, view => view.SetInteractive(true));
        }

        private void SetInteractive(bool interactive)
        {
            _canvasGroup.interactable = interactive;
            _canvasGroup.blocksRaycasts = interactive;
        }
    }
}
