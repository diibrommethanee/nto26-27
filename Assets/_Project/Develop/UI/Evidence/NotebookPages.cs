using System;
using System.Collections.Generic;
using _Project.Develop.Data.Evidence;
using _Project.Develop.Gameplay.Evidence;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.Develop.UI.Evidence
{
    internal class NotebookPages
    {
        private const float PageTurnDuration = 0.66f;

        private readonly List<(Hypothesis Data, HypothesisView View, CanvasGroup Group, RectTransform Clues)> _pages = new();
        private readonly RectTransform _board;
        private readonly Vector3[] _clueAreaCorners = new Vector3[4];
        private readonly Dictionary<RectTransform, RectTransform> _threadAreas = new();
        private readonly RectTransform _pendingClues;
        private readonly Image _matLeft;
        private readonly Image _matRight;
        private readonly Image _surfaceLeft;
        private readonly Image _surfaceRight;
        private readonly Image _turningFace;
        private readonly NotebookTurnShadow _shadow;
        private readonly NotebookPageCurl _faceCurl;
        private readonly NotebookPrintedText _printedText;
        private readonly HypothesisView _template;
        private readonly EvidenceService _evidence;
        private readonly Action _clearSelection;

        private Tween _turn;
        private int _position;
        private int? _dragPointer;
        private readonly PageTurnGesture _gesture = new();
        private int _turnOrigin;
        private int _turnDestination;
        private Action<float> _paintTurn;

        private bool HasPendingClues => _pendingClues.GetComponentInChildren<ClueView>(true) != null;
        private int PageCount => _pages.Count + (HasPendingClues ? 1 : 0);
        private int SpreadCount => Mathf.Max(1, (PageCount + 1) / 2);

        public NotebookPages(RectTransform board,
            Image matLeft, Image matRight, Image surfaceLeft, Image surfaceRight,
            EvidenceService evidence, Action clearSelection)
        {
            _board = board;

            _matLeft = matLeft;
            _matRight = matRight;
            _surfaceLeft = surfaceLeft;
            _surfaceRight = surfaceRight;
            _pendingClues = CreateClueArea(0, "Pending");
            _evidence = evidence;
            _clearSelection = clearSelection;
            _template = board.GetComponentInChildren<HypothesisView>(true);
            _template.gameObject.SetActive(false);

            _shadow = new GameObject("Page Turn Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(NotebookTurnShadow))
                .GetComponent<NotebookTurnShadow>();
            _shadow.gameObject.layer = _board.gameObject.layer;
            _shadow.transform.SetParent(_board, false);
            _shadow.raycastTarget = false;
            _shadow.gameObject.SetActive(false);
            _turningFace = CreateImage("Turning Page");
            _faceCurl = _turningFace.gameObject.AddComponent<NotebookPageCurl>();
            _printedText = new NotebookPrintedText(_turningFace.rectTransform, _faceCurl);
            _turningFace.raycastTarget = false;
            _turningFace.gameObject.SetActive(false);
        }

        public void Add(Hypothesis hypothesis)
        {
            if (hypothesis == null || IndexOf(hypothesis) >= 0)
                return;

            var view = UnityEngine.Object.Instantiate(_template, _board);
            view.name = hypothesis.name;
            var rect = (RectTransform)view.transform;
            var left = _pages.Count % 2 == 0;
            rect.anchorMin = new Vector2(left ? .04f : .54f, .5f);
            rect.anchorMax = new Vector2(left ? .46f : .96f, .95f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            view.Initialize(_evidence);
            view.Show(hypothesis);
            view.gameObject.SetActive(false);
            var pageName = $"{_pages.Count + 1}_{hypothesis.name.Replace(' ', '_')}";
            var clues = CreateClueArea(_pages.Count, pageName);
            _pages.Add((hypothesis, view, GetGroup(rect), clues));
        }

        public RectTransform ClueArea(Hypothesis hypothesis)
        {
            var index = IndexOf(hypothesis);
            return index < 0 ? _pendingClues : _pages[index].Clues;
        }

        public void ShowCluePage(Hypothesis hypothesis)
        {
            Stop();
            var index = IndexOf(hypothesis);
            _position = (index < 0 ? _pages.Count : index) / 2;
            Render();
        }

        private RectTransform CreateClueArea(int index, string pageName)
        {
            var surface = index % 2 == 0 ? _surfaceLeft : _surfaceRight;
            var template = (RectTransform)surface.transform.Find("ClueContainer");
            var rect = UnityEngine.Object.Instantiate(template, surface.transform, false);
            rect.name = $"ClueContainer_{pageName}";
            var threads = UnityEngine.Object.Instantiate((RectTransform)surface.transform.Find("ThreadContainer"), surface.transform, false);
            threads.name = $"ThreadContainer_{pageName}";
            threads.gameObject.SetActive(false);
            _threadAreas.Add(rect, threads);
            rect.gameObject.AddComponent<CanvasGroup>();
            var mask = rect.gameObject.AddComponent<RectMask2D>();
            mask.padding = new Vector4(-12f, -12f, -12f, -12f);
            rect.gameObject.SetActive(false);
            return rect;
        }
        public RectTransform ThreadArea(RectTransform clueArea) => _threadAreas[clueArea];

        public void ShowFirst() => Render();

        public void Show(Hypothesis hypothesis)
        {
            Stop();
            Add(hypothesis);
            _position = hypothesis == null ? 0 : IndexOf(hypothesis) / 2;
            Render();
        }

        public void RefreshClues()
        {
            foreach (var page in _pages)
                page.View.RefreshClues();
        }

        public void Stop()
        {
            CancelAnimation();
            CloseChoices();
            Render();
        }

        public void CancelAnimation()
        {
            _turn.Stop();
            _printedText.Detach();
            _dragPointer = null;
            if (_paintTurn != null)
                _position = _turnOrigin;
            _paintTurn = null;
        }

        private int IndexOf(Hypothesis hypothesis) => _pages.FindIndex(page => page.Data == hypothesis);

        private void CloseChoices()
        {
            foreach (var page in _pages)
                page.View.CloseChoices();
        }

        private void Render()
        {
            _printedText.Detach();
            _matLeft.gameObject.SetActive(true);
            _matRight.gameObject.SetActive(true);
            _surfaceLeft.gameObject.SetActive(true);
            _surfaceRight.gameObject.SetActive(true);
            _position = Mathf.Clamp(_position, 0, SpreadCount - 1);
            for (var index = 0; index < _pages.Count; index++)
            {
                var view = _pages[index].View;
                var rect = PlaceFlatPage(index);
                rect.SetParent(index % 2 == 0 ? _surfaceLeft.transform : _surfaceRight.transform, true);
                view.gameObject.SetActive(index / 2 == _position);
            }
            PlaceClueAreas(_position * 2, _position * 2 + 1, -1);
            SetContentInteractive(true);

            _turningFace.gameObject.SetActive(false);
            _faceCurl.SetBend(0f, .5f);
            _shadow.gameObject.SetActive(false);
        }

        public void BeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || _dragPointer.HasValue || _turn.isAlive)
                return;

            var direction = TurnDirectionFor(eventData.pointerPressRaycast.gameObject);
            var destination = _position + direction;
            if (direction == 0 || destination < 0 || destination >= SpreadCount)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_board, eventData.pressPosition,
                    eventData.pressEventCamera, out var start))
                return;

            _dragPointer = eventData.pointerId;
            _gesture.Begin(direction, start, PageFace(direction < 0).rectTransform);
        }

        private int TurnDirectionFor(GameObject pressed)
        {
            if (pressed == _surfaceLeft.gameObject || pressed == _matLeft.gameObject) return -1;
            if (pressed == _surfaceRight.gameObject || pressed == _matRight.gameObject) return 1;
            return 0;
        }

        public void Drag(PointerEventData eventData)
        {
            if (_dragPointer != eventData.pointerId ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(_board, eventData.position,
                    eventData.pressEventCamera, out var cursor))
                return;

            var passedDeadZone = _gesture.Move(cursor.x);
            if (passedDeadZone && _paintTurn == null)
                StartTurn(_gesture.Direction);

            _paintTurn?.Invoke(_gesture.Progress);
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (_dragPointer != eventData.pointerId)
                return;

            Drag(eventData);
            _dragPointer = null;
            if (_paintTurn == null)
                return;

            var flick = _gesture.IsFlick;
            var commit = _gesture.Progress >= .5f || flick;
            var goal = commit ? 1f : 0f;
            var remainder = Mathf.Abs(goal - _gesture.Progress);
            var duration = Mathf.Clamp(PageTurnDuration * remainder / (flick ? 1.2f : 1f), .22f, .49f);
            _turn = Tween.Custom(_gesture.Progress, goal, duration, _paintTurn, flick ? Ease.OutSine : Ease.InOutSine,
                    useUnscaledTime: true)
                .OnComplete(() =>
                {
                    _position = commit ? _turnDestination : _turnOrigin;
                    _paintTurn = null;
                    Render();
                });
        }

        private void StartTurn(int direction)
        {
            _clearSelection();
            CloseChoices();
            var origin = _position;
            var destination = origin + direction;
            _turnOrigin = origin;
            _turnDestination = destination;
            var outgoing = PageFace(direction < 0);
            var incoming = PageFace(direction > 0);
            SetTurnFace(outgoing, 1f);
            SetContentInteractive(false);
            _turningFace.gameObject.SetActive(true);
            PlaceTurnLayers(origin, destination, direction, true);

            _paintTurn = progress =>
            {
                var firstHalf = progress < .5f;
                var position = firstHalf ? origin : destination;
                var turningLeaf = firstHalf ? outgoing : incoming;
                if (_position != position)
                {
                    _position = position;
                    SetTurnFace(turningLeaf, width: 0f);
                    PlaceTurnLayers(origin, destination, direction, firstHalf);
                }

                var angle = Mathf.PI * progress;
                var width = Mathf.Abs(Mathf.Cos(angle));
                SetTurnFace(turningLeaf, width);
                var lift = Mathf.Sin(angle);
                var receiver = incoming.rectTransform;
                _shadow.SetPose(receiver, receiver.rect.width * Mathf.Lerp(.08f, .39f, lift), lift * .32f);
            };
        }

        private Image PageFace(bool left) => left ? _surfaceLeft : _surfaceRight;

        private RectTransform PlaceFlatPage(int index)
        {
            var rect = (RectTransform)_pages[index].View.transform;
            var left = index % 2 == 0;
            rect.SetParent(_board, false);
            rect.anchorMin = new Vector2(left ? .04f : .54f, .5f);
            rect.anchorMax = new Vector2(left ? .46f : .96f, .95f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();
            return rect;
        }

        private void PlaceTurnLayers(int origin, int destination, int direction, bool front)
        {
            _printedText.Detach();
            var turningSide = direction > 0 ? 1 : 0;
            var originRest = origin * 2 + 1 - turningSide;
            var destinationRest = destination * 2 + turningSide;
            var moving = front ? origin * 2 + turningSide : destination * 2 + 1 - turningSide;
            var leaf = _turningFace.rectTransform;

            for (var index = 0; index < _pages.Count; index++)
            {
                var rect = PlaceFlatPage(index);
                var stationary = index == originRest || index == destinationRest;
                rect.gameObject.SetActive(stationary || index == moving);
                if (stationary)
                    rect.SetSiblingIndex(leaf.GetSiblingIndex());
                if (index != moving)
                    continue;

                AttachToTurningPage(rect, leaf);
            }
            PlaceClueAreas(originRest, destinationRest, moving);
            _shadow.transform.SetAsLastSibling();
            leaf.SetAsLastSibling();
        }

        private void PlaceClueAreas(int first, int second, int moving)
        {
            for (var index = 0; index <= _pages.Count; index++)
            {
                var pending = index == _pages.Count;
                var rect = pending ? _pendingClues : _pages[index].Clues;
                var surface = index % 2 == 0 ? _surfaceLeft : _surfaceRight;
                AlignClueArea(rect, surface);
                rect.gameObject.SetActive((!pending || HasPendingClues) &&
                    (index == first || index == second || index == moving));
                var threads = _threadAreas[rect];
                threads.SetParent(moving == index ? _turningFace.transform : surface.transform, false);
                threads.anchorMin = Vector2.zero;
                threads.anchorMax = Vector2.one;
                threads.offsetMin = threads.offsetMax = Vector2.zero;
                threads.gameObject.SetActive(rect.gameObject.activeSelf);
                if (moving == index)
                    _printedText.Attach(threads);
                if (moving < 0)
                {
                    rect.SetParent(surface.transform, true);
                    rect.SetAsLastSibling();
                    continue;
                }

                var leaf = _turningFace.rectTransform;
                if (index != moving)
                {
                    rect.SetSiblingIndex(leaf.GetSiblingIndex());
                    continue;
                }

                AttachToTurningPage(rect, leaf);
            }
        }

        private void AttachToTurningPage(RectTransform rect, RectTransform leaf)
        {
            var size = rect.rect.size;
            var offset = rect.localPosition - leaf.localPosition;
            rect.SetParent(leaf, false);
            rect.anchorMin = rect.anchorMax = leaf.pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
            _printedText.Attach(rect);
        }

        private void AlignClueArea(RectTransform rect, Image surface)
        {
            var template = (RectTransform)surface.transform.Find("ClueContainer");
            template.GetWorldCorners(_clueAreaCorners);
            var minimum = (Vector2)_board.InverseTransformPoint(_clueAreaCorners[0]);
            var maximum = (Vector2)_board.InverseTransformPoint(_clueAreaCorners[2]);
            rect.SetParent(_board, false);
            rect.anchorMin = rect.anchorMax = _board.pivot;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = maximum - minimum;
            rect.anchoredPosition = (minimum + maximum) * .5f;
            rect.localScale = Vector3.one;
        }

        private void SetTurnFace(Image face, float width)
        {
            var rect = _turningFace.rectTransform;
            var source = face.rectTransform;
            _turningFace.sprite = face.sprite;
            var shade = Mathf.Lerp(.79f, 1f, width);
            _turningFace.color = new Color(shade, shade, shade);
            rect.anchorMin = rect.anchorMax = source.anchorMin;
            rect.sizeDelta = source.sizeDelta;
            rect.pivot = source.pivot;
            rect.anchoredPosition = source.anchoredPosition;
            rect.localScale = new Vector3(width, 1f, 1f);
            _faceCurl.SetBend(.36f * Mathf.Sqrt(Mathf.Max(0f, 1f - width * width)), rect.pivot.x, _gesture.GripHeight);
            _printedText.Refresh();
        }

        private void SetContentInteractive(bool interactive)
        {
            GetGroup(_pendingClues).SetInteractive(interactive);
            foreach (var page in _pages)
            {
                page.Group.SetInteractive(interactive);
                GetGroup(page.Clues).SetInteractive(interactive);
            }
        }

        private static CanvasGroup GetGroup(RectTransform rect) => rect.TryGetComponent<CanvasGroup>(out var group)
            ? group
            : rect.gameObject.AddComponent<CanvasGroup>();

        private Image CreateImage(string name)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.gameObject.layer = _board.gameObject.layer;
            image.transform.SetParent(_board, false);
            return image;
        }

    }
}
