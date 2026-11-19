using System;
using System.Collections.Generic;
using _Project.Develop.Data.Evidence;
using _Project.Develop.Gameplay.Evidence;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.UI.Evidence
{
    public class EvidenceBoardController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform board;
        [SerializeField] private RectTransform evidenceContainer;
        [SerializeField] private RectTransform threadContainer;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private HypothesisBuilderView hypothesisView;
        [SerializeField] private RectTransform notebookButton;

        [Header("Prefabs")]
        [SerializeField] private EvidenceView evidencePrefab;
        [SerializeField] private EvidenceThreadView threadPrefab;
        [SerializeField] private HypothesisData currentHypothesis;

        [Header("Animations")]
        [SerializeField] private float openDuration = 0.32f;
        [SerializeField] private float closeDuration = 0.18f;
        [SerializeField] private float slideDistance = 30f;
        [SerializeField] private float closedScale = 0.94f;
        [SerializeField, Min(0.1f)] private float evidenceFlyDuration = 0.55f;
        [SerializeField, Min(0f)] private float evidenceFlyArc = 90f;
        [SerializeField, Min(8f)] private float evidenceFlyIconSize = 48f;

        private readonly HashSet<(EvidenceView, EvidenceView)> _combinedPairs = new();
        private readonly HashSet<EvidenceData> _displayedEvidence = new();
        private EvidenceService _evidence;
        private EvidenceView _selected;
        private EvidenceThreadView _preview;
        private Vector3 _openScale;
        private float _openY;
        private bool _isOpen;
        private Tween _transition;
        private int _transitionId;
        private Vector2? _preferredSpawnPosition;

        public event Action<HypothesisData> HypothesisSolved;

        [Inject]
        private void Construct(EvidenceService evidence)
        {
            _evidence = evidence;
        }

        private void Start()
        {
            _openScale = board.localScale;
            _openY = board.anchoredPosition.y;
            _isOpen = canvasGroup.interactable && canvasGroup.blocksRaycasts && canvasGroup.alpha > 0.99f;

            if (!_isOpen)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                board.localScale = _openScale * closedScale;
                board.anchoredPosition -= Vector2.up * slideDistance;
            }

            hypothesisView.Initialize(_evidence);
            hypothesisView.Solved += OnHypothesisSolved;
            _evidence.EvidenceCollected += OnEvidenceCollected;
            hypothesisView.Show(currentHypothesis);
            SyncCollectedEvidence();
        }

        private void OnDestroy()
        {
            hypothesisView.Solved -= OnHypothesisSolved;
            if (_evidence != null)
                _evidence.EvidenceCollected -= OnEvidenceCollected;
        }

        public void SelectEvidence(EvidenceView card)
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

            if (_selected != card &&
                !_combinedPairs.Contains((_selected, card)) &&
                !_combinedPairs.Contains((card, _selected)) &&
                _evidence.TryCombine(_selected.Data, card.Data, out var result))
            {
                _combinedPairs.Add((_selected, card));
                _preview.Connect(_selected.Rect, card.Rect);
                _preview = null;

                var center = (_selected.Rect.anchoredPosition + card.Rect.anchoredPosition) * 0.5f;
                SpawnEvidence(result, center + Vector2.up * 100f);
            }

            ClearSelection();
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

        public void SpawnEvidence(EvidenceData data, Vector2 position)
        {
            _preferredSpawnPosition = position;
            RegisterCollectedEvidence(data);

            if (!_displayedEvidence.Contains(data))
                AddEvidenceCard(data, position);

            _preferredSpawnPosition = null;
        }

        public void RegisterCollectedEvidence(EvidenceData data)
        {
            _evidence.Collect(data);
        }

        private void OnEvidenceCollected(EvidenceData data, Vector3? sourceWorldPosition)
        {
            hypothesisView.RefreshEvidence();

            if (sourceWorldPosition.HasValue && notebookButton != null &&
                TryGetFlightCanvas(out var canvas, out var startPosition, sourceWorldPosition.Value))
            {
                FlyEvidenceToNotebookAsync(data, canvas, startPosition).Forget();
                return;
            }

            var position = _preferredSpawnPosition ?? GetAutomaticSpawnPosition();
            AddEvidenceCard(data, position);
        }

        private bool TryGetFlightCanvas(out Canvas canvas, out Vector2 startPosition, Vector3 sourceWorldPosition)
        {
            canvas = notebookButton.GetComponentInParent<Canvas>()?.rootCanvas;
            startPosition = default;
            var worldCamera = Camera.main;
            if (canvas == null || worldCamera == null)
                return false;

            var canvasRect = canvas.transform as RectTransform;
            var screenPosition = worldCamera.WorldToScreenPoint(sourceWorldPosition);
            if (screenPosition.z <= 0f)
                return false;

            var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosition, eventCamera, out startPosition);
        }

        private async UniTaskVoid FlyEvidenceToNotebookAsync(EvidenceData data, Canvas canvas, Vector2 startPosition)
        {
            var flightObject = new GameObject("Collected Evidence Flight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var flightRect = (RectTransform)flightObject.transform;
            flightRect.SetParent(canvas.transform, false);
            flightRect.SetAsLastSibling();
            flightRect.anchorMin = flightRect.anchorMax = new Vector2(0.5f, 0.5f);
            flightRect.pivot = new Vector2(0.5f, 0.5f);
            flightRect.anchoredPosition = startPosition;
            flightRect.sizeDelta = new Vector2(evidenceFlyIconSize, evidenceFlyIconSize);

            var flightImage = flightObject.GetComponent<Image>();
            flightImage.sprite = data.icon;
            flightImage.preserveAspect = true;
            flightImage.raycastTarget = false;

            var canvasRect = (RectTransform)canvas.transform;
            var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var buttonScreenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, notebookButton.position);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, buttonScreenPosition, eventCamera, out var targetPosition))
            {
                Destroy(flightObject);
                AddEvidenceCard(data, GetAutomaticSpawnPosition());
                return;
            }

            await Tween.Custom(flightRect, 0f, 1f, evidenceFlyDuration, (rect, progress) =>
            {
                var position = Vector2.LerpUnclamped(startPosition, targetPosition, progress);
                position.y += Mathf.Sin(progress * Mathf.PI) * evidenceFlyArc;
                rect.anchoredPosition = position;
                rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.45f, progress);
            }, Ease.InOutCubic).SetCancellationToken(destroyCancellationToken);

            Destroy(flightObject);
            AddEvidenceCard(data, GetAutomaticSpawnPosition());
        }

        private void AddEvidenceCard(EvidenceData data, Vector2 position)
        {
            if (data == null || _displayedEvidence.Contains(data))
                return;

            if (evidencePrefab == null)
            {
                Debug.LogError("Evidence Board is missing its Evidence Prefab reference.", this);
                return;
            }

            _displayedEvidence.Add(data);
            Instantiate(evidencePrefab, evidenceContainer).Initialize(data, board, position, SelectEvidence);
        }

        private void SyncCollectedEvidence()
        {
            foreach (var collected in _evidence.Collected)
                AddEvidenceCard(collected, GetAutomaticSpawnPosition());
        }

        private Vector2 GetAutomaticSpawnPosition()
        {
            var index = _displayedEvidence.Count;
            return new Vector2((index % 3) * 24f, -(index / 3) * 24f);
        }

        public void SetCurrentHypothesis(HypothesisData hypothesis)
        {
            currentHypothesis = hypothesis;
            hypothesisView.Show(hypothesis);
        }

        private void OnHypothesisSolved(HypothesisData hypothesis)
        {
            HypothesisSolved?.Invoke(hypothesis);
        }

        public void Open()
        {
            SyncCollectedEvidence();
            SetOpenAsync(true).Forget();
        }

        public void Close()
        {
            ClearSelection();
            hypothesisView.CloseChoices();
            SetOpenAsync(false).Forget();
        }

        private async UniTask SetOpenAsync(bool open)
        {
            if (_isOpen == open)
                return;

            _isOpen = open;
            var transitionId = ++_transitionId;
            _transition.Stop();

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var duration = open ? openDuration : closeDuration;
            var startScale = board.localScale;
            var startPosition = board.anchoredPosition;
            var startAlpha = canvasGroup.alpha;
            var targetScale = open ? _openScale : _openScale * closedScale;
            var targetY = open ? _openY : _openY - slideDistance;
            var targetAlpha = open ? 1f : 0f;
            _transition = Tween.Custom(0f, 1f, duration, progress =>
            {
                var scale = Easing.Evaluate(progress, open ? Ease.OutBack : Ease.InBack);
                var slide = Easing.Evaluate(progress, open ? Ease.OutCubic : Ease.InCubic);
                board.localScale = Vector3.LerpUnclamped(startScale, targetScale, scale);
                board.anchoredPosition = new Vector2(startPosition.x, Mathf.Lerp(startPosition.y, targetY, slide));
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            }, Ease.Linear).SetCancellationToken(destroyCancellationToken);

            await _transition;
            if (transitionId == _transitionId && open)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
    }
}
