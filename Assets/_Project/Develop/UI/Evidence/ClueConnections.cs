using System.Collections.Generic;
using _Project.Develop.Gameplay.Evidence;
using UnityEngine;

namespace _Project.Develop.UI.Evidence
{
    internal class ClueConnections
    {
        private readonly RectTransform _board;
        private readonly ClueThreadView _threadPrefab;
        private readonly EvidenceService _evidence;
        private readonly HashSet<(ClueView, ClueView)> _combinedPairs = new();
        private readonly List<ClueThreadView> _threads = new();
        private ClueView _selected;
        private ClueThreadView _preview;

        public ClueConnections(RectTransform board, ClueThreadView threadPrefab, EvidenceService evidence)
        {
            _board = board;
            _threadPrefab = threadPrefab;
            _evidence = evidence;
        }

        public void RefreshPages(NotebookPages pages)
        {
            foreach (var thread in _threads)
                thread.RefreshPage(_board, pages.ThreadArea);
        }

        public void Select(ClueView card, RectTransform threadArea)
        {
            if (_selected == null)
            {
                _selected = card;
                card.SetSelected(true);
                _preview = Object.Instantiate(_threadPrefab, threadArea);
                _preview.transform.SetAsFirstSibling();

                _preview.FollowMouse(card.Rect);
                return;
            }

            ConnectClues(_selected, card);
            ClearSelection();
        }

        private void ConnectClues(ClueView first, ClueView second)
        {
            if (first == second || first.Rect.parent != second.Rect.parent ||
                _combinedPairs.Contains((first, second)) ||
                _combinedPairs.Contains((second, first)) ||
                !_evidence.TryCombine(first.Data, second.Data, out var result))
                return;

            _combinedPairs.Add((first, second));
            _preview.Connect(first.Rect, second.Rect);
            _threads.Add(_preview);
            _preview = null;

            var midpoint = (first.Rect.anchoredPosition + second.Rect.anchoredPosition) * 0.5f;
            _evidence.Collect(result, boardPosition: midpoint + Vector2.up * 100f);
        }

        public void ClearSelection()
        {
            if (_selected == null)
                return;

            _selected.SetSelected(false);
            _selected = null;
            if (_preview != null)
            {
                _preview.Hide();
                _preview.transform.SetParent(_board, false);
                Object.Destroy(_preview.gameObject);
                _preview = null;
            }
        }
    }
}
