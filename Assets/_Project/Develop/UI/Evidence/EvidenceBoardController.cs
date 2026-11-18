using System;
using System.Collections.Generic;
using _Project.Develop.Data.Evidence;
using _Project.Develop.Gameplay.Evidence;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
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

        [Header("Prefabs")]
        [SerializeField] private EvidenceView evidencePrefab;
        [SerializeField] private EvidenceThreadView threadPrefab;
        [SerializeField] private HypothesisData currentHypothesis;

        [Header("Animations")]
        [SerializeField] private float openDuration = 0.32f;
        [SerializeField] private float closeDuration = 0.18f;
        [SerializeField] private float slideDistance = 30f;
        [SerializeField] private float closedScale = 0.94f;

        private readonly HashSet<(EvidenceView, EvidenceView)> _combinedPairs = new();
        private EvidenceService _evidence;
        private EvidenceView _selected;
        private EvidenceThreadView _preview;
        private Vector3 _openScale;
        private float _openY;
        private bool _isOpen;
        private Tween _transition;
        private int _transitionId;

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
            hypothesisView.Show(currentHypothesis);
        }

        private void OnDestroy()
        {
            hypothesisView.Solved -= OnHypothesisSolved;
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
            RegisterCollectedEvidence(data);
            Instantiate(evidencePrefab, evidenceContainer).Initialize(data, board, position, SelectEvidence);
        }

        public void RegisterCollectedEvidence(EvidenceData data)
        {
            if (_evidence.Collect(data))
                hypothesisView.RefreshEvidence();
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
