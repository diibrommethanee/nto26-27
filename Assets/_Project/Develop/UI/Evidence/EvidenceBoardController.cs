using PrimeTween;
using System.Collections.Generic;
using _Project.Develop.Data.Evidence;
using UnityEngine;
using VContainer;

namespace _Project.Develop.UI.Evidence
{
    public class EvidenceBoardController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform board;
        [SerializeField] private Transform evidenceContainer;
        [SerializeField] private Transform threadContainer;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Prefabs")]
        [SerializeField] private EvidenceView evidencePrefab;
        [SerializeField] private EvidenceThreadView threadPrefab;
        [SerializeField] private HypothesisData currentHypothesis;
        
        [Header("Animations")]
        [SerializeField] private float openDuration = 0.32f;
        [SerializeField] private float closeDuration = 0.18f;
        [SerializeField] private float slideDistance = 30f;
        [SerializeField] private float closedScale = 0.94f;

        private Vector3 _boardOpenScale;
        private float _boardOpenY;
        private bool _isOpen;

        private EvidenceDatabase _database;
        private EvidenceView _selectedEvidence;

        private EvidenceThreadView _previewThread;
        private HypothesisBuilderView _hypothesisView;
        private readonly List<EvidenceData> _collectedEvidence = new();

        private readonly List<(EvidenceView first, EvidenceView second)> _combinedPairs = new();
        private readonly List<EvidenceView> _evidenceViews = new();

        public RectTransform Board => board;
        public RectTransform EvidenceContainer => (RectTransform)evidenceContainer;

        [Inject]
        private void Construct(EvidenceDatabase database)
        {
            _database = database;
        }
        
        private void Awake()
        {
            _boardOpenScale = board.localScale;
            _boardOpenY = board.anchoredPosition.y;

            _isOpen = canvasGroup.alpha > 0.99f &&
                      canvasGroup.interactable &&
                      canvasGroup.blocksRaycasts;

            if (!_isOpen)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                board.localScale = _boardOpenScale * closedScale;
                board.anchoredPosition += Vector2.down * slideDistance;
            }

            _hypothesisView = HypothesisBuilderView.Create(board);
            if (currentHypothesis != null)
                _hypothesisView.Show(currentHypothesis, _collectedEvidence, OnHypothesisSolved);
        }

        public void SelectEvidence(EvidenceView evidence)
        {
            if (evidence == null)
                return;

            if (_selectedEvidence == evidence)
            {
                SetSelectedEvidence(null);
                return;
            }

            if (_selectedEvidence == null)
            {
                SetSelectedEvidence(evidence);
                return;
            }

            var first = _selectedEvidence;

            if (IsAlreadyCombined(first, evidence))
            {
                SetSelectedEvidence(null);
                return;
            }

            if (!_database.TryGetResult(first.Data, evidence.Data, out var result) || result == null)
            {
                SetSelectedEvidence(null);
                return;
            }

            _combinedPairs.Add((first, evidence));

            CreateThread(first, evidence);

            var resultPosition = (first.Rect.anchoredPosition + evidence.Rect.anchoredPosition) / 2f
                                 + Vector2.up * 100f;
            SpawnEvidence(result, resultPosition);

            SetSelectedEvidence(null);

        }

        private void RegisterEvidence(EvidenceData data)
        {
            if (data == null)
                return;

            if (!_collectedEvidence.Contains(data))
                _collectedEvidence.Add(data);

            // Also read the cards currently on the board. This covers evidence
            // views instantiated by scene tools that do not use SpawnEvidence.
            foreach (var view in evidenceContainer.GetComponentsInChildren<EvidenceView>(true))
            {
                if (view.Data != null && !_collectedEvidence.Contains(view.Data))
                    _collectedEvidence.Add(view.Data);
            }

            // Refresh even when the asset was already registered. The same evidence
            // can be spawned again after the hypothesis UI has been created.
            _hypothesisView?.RefreshEvidence(_collectedEvidence);
        }

        public void SetCurrentHypothesis(HypothesisData hypothesis)
        {
            currentHypothesis = hypothesis;
            _hypothesisView?.Show(currentHypothesis, _collectedEvidence, OnHypothesisSolved);
        }

        private void OnHypothesisSolved(HypothesisData hypothesis)
        {
            // Story progression can subscribe to this event or use the reward id
            // as the quest/story key.
            HypothesisSolved?.Invoke(hypothesis);
        }

        public event System.Action<HypothesisData> HypothesisSolved;

        public void RegisterCollectedEvidence(EvidenceData data) => RegisterEvidence(data);

        private void SetSelectedEvidence(EvidenceView evidence)
        {
            if (_selectedEvidence != null)
                _selectedEvidence.SetSelected(false);

            _selectedEvidence = evidence;

            if (_selectedEvidence == null)
            {
                if (_previewThread != null)
                    _previewThread.Hide();

                return;
            }

            _selectedEvidence.SetSelected(true);

            GetPreviewThread()?.FollowMouse(_selectedEvidence.Rect);
        }

        private bool IsAlreadyCombined(
            EvidenceView first,
            EvidenceView second)
        {
            foreach (var pair in _combinedPairs)
            {
                if ((pair.first == first && pair.second == second) ||
                    (pair.first == second && pair.second == first))
                    return true;
            }

            return false;
        }

        private EvidenceThreadView GetPreviewThread()
        {
            if (_previewThread != null)
                return _previewThread;

            if (threadPrefab == null || threadContainer == null)
            {
                return null;
            }

            _previewThread = Instantiate(
                threadPrefab,
                threadContainer,
                false
            );

            _previewThread.Initialize(board);

            return _previewThread;
        }

        private void CreateThread(EvidenceView first, EvidenceView second)
        {
            var thread = GetPreviewThread();

            if (thread == null)
                return;

            thread.Connect(first.Rect, second.Rect);

            _previewThread = null;
        }

        public void SpawnEvidence(EvidenceData data, Vector2 position)
        {
            var view = Instantiate(
                evidencePrefab,
                evidenceContainer,
                false
            );

            view.Initialize(data, this);
            view.Rect.anchoredPosition = position;

            _evidenceViews.Add(view);
            view.SetSelected(false);
            RegisterEvidence(data);
        }

        public void Open()
        {
            if (_isOpen)
                return;

            _isOpen = true;

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            Sequence.Create()
                .Group(Tween.Scale(
                    board,
                    _boardOpenScale,
                    openDuration,
                    Ease.OutBack
                ))
                .Group(Tween.UIAnchoredPositionY(
                    board,
                    _boardOpenY,
                    openDuration,
                    Ease.OutCubic
                ))
                .Group(Tween.Custom(
                    canvasGroup,
                    canvasGroup.alpha,
                    1f,
                    openDuration,
                    (target, alpha) => target.alpha = alpha
                ));
        }

        public void Close()
        {
            SetSelectedEvidence(null);

            if (!_isOpen)
                return;

            _isOpen = false;

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Sequence.Create()
                .Group(Tween.Scale(
                    board,
                    _boardOpenScale * closedScale,
                    closeDuration,
                    Ease.InBack
                ))
                .Group(Tween.UIAnchoredPositionY(
                    board,
                    _boardOpenY - slideDistance,
                    closeDuration,
                    Ease.InCubic
                ))
                .Group(Tween.Custom(
                    canvasGroup,
                    canvasGroup.alpha,
                    0f,
                    closeDuration,
                    (target, alpha) => target.alpha = alpha
                ));
        }
    }
}
