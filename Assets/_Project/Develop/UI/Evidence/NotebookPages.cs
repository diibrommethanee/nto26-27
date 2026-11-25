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
        private const float DragDeadZone = 4f;
        private const float FlickMemory = .1f;
        private const float FlickSpeed = 1.8f;

        private readonly List<(Hypothesis Data, HypothesisView View, CanvasGroup Group)> _pages = new();
        private readonly RectTransform _board;
        private readonly List<RectTransform> _clueAreas = new();
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
        private int _gripDirection;
        private float _dragStartX;
        private float _gripHeight;
        private float _dragSpan;
        private float _lastDragX;
        private float _lastMotionTime;
        private float _dragVelocity;
        private float _dragProgress;
        private int _turnOrigin;
        private int _turnDestination;
        private Action<float> _paintTurn;

        private bool HasPendingClues => _pendingClues.childCount > 0;
        private int PageCount => _pages.Count + (HasPendingClues ? 1 : 0);
        private int SpreadCount => Mathf.Max(1, (PageCount + 1) / 2);

        public NotebookPages(RectTransform board,
            Image matLeft, Image matRight, Image surfaceLeft, Image surfaceRight,
            EvidenceService evidence, Action clearSelection)
        {
            _board = board;
            _pendingClues = CreateClueArea("Unassigned Clues");
            _matLeft = matLeft;
            _matRight = matRight;
            _surfaceLeft = surfaceLeft;
            _surfaceRight = surfaceRight;
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
            _pages.Add((hypothesis, view, GetGroup(rect)));
            _clueAreas.Add(CreateClueArea(hypothesis.name + " Clues"));
        }

        public RectTransform ClueArea(Hypothesis hypothesis)
        {
            var index = IndexOf(hypothesis);
            return index < 0 ? _pendingClues : _clueAreas[index];
        }

        public void ShowCluePage(Hypothesis hypothesis)
        {
            Stop();
            var index = IndexOf(hypothesis);
            _position = (index < 0 ? _pages.Count : index) / 2;
            Render();
        }

        private RectTransform CreateClueArea(string label)
        {
            var area = new GameObject(label, typeof(RectTransform), typeof(CanvasGroup), typeof(RectMask2D));
            area.layer = _board.gameObject.layer;
            var rect = (RectTransform)area.transform;
            rect.SetParent(_board, false);
            rect.gameObject.SetActive(false);
            return rect;
        }

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
                PlaceFlatPage(index);
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
            var pressed = eventData.pointerPressRaycast.gameObject;
            if (eventData.button != PointerEventData.InputButton.Left || _dragPointer.HasValue || _turn.isAlive ||
                (pressed != _surfaceLeft.gameObject && pressed != _surfaceRight.gameObject &&
                 pressed != _matLeft.gameObject && pressed != _matRight.gameObject) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(_board, eventData.pressPosition,
                    eventData.pressEventCamera, out var start))
                return;

            var direction = pressed == _surfaceLeft.gameObject || pressed == _matLeft.gameObject ? -1 : 1;
            var target = _position + direction;
            if (target < 0 || target >= SpreadCount)
                return;
            _dragPointer = eventData.pointerId;
            _dragStartX = _lastDragX = start.x;
            _lastMotionTime = Time.unscaledTime;
            _dragVelocity = 0f;
            _dragProgress = 0f;
            _gripDirection = direction;
            var leaf = PageFace(direction < 0).rectTransform;
            _gripHeight = Mathf.Clamp((start.y - leaf.localPosition.y) / (leaf.rect.height * .5f), -1f, 1f);
            var distanceFromSpine = Mathf.Abs(start.x - leaf.localPosition.x);
            var gripPosition = distanceFromSpine / leaf.rect.width;
            var grip = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.2f, .95f, gripPosition));
            var resistedTravel = leaf.rect.width * Mathf.Lerp(1.45f, .72f, grip);
            var edgeGrip = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.65f, .95f, gripPosition));
            // A projected point moves twice its distance from the spine in a complete turn.
            _dragSpan = Mathf.Lerp(resistedTravel, 2f * distanceFromSpine, edgeGrip);
        }

        public void Drag(PointerEventData eventData)
        {
            if (_dragPointer != eventData.pointerId ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(_board, eventData.position,
                    eventData.pressEventCamera, out var cursor))
                return;

            var delta = _dragStartX - cursor.x;
            SampleDragVelocity(cursor.x);
            var direction = delta > 0f ? 1 : -1;
            if (Mathf.Abs(delta) <= DragDeadZone || direction != _gripDirection)
            {
                _dragProgress = 0f;
                _paintTurn?.Invoke(0f);
                return;
            }

            if (_paintTurn == null)
                StartTurn(direction);

            var travel = Mathf.Clamp01((Mathf.Abs(delta) - DragDeadZone) / _dragSpan);
            // Convert the horizontal travel to the leaf's rotation angle.
            _dragProgress = Mathf.Acos(1f - 2f * travel) / Mathf.PI;
            _paintTurn(_dragProgress);
        }

        private void SampleDragVelocity(float x)
        {
            var movement = _lastDragX - x;
            if (Mathf.Abs(movement) < .5f)
                return;

            var now = Time.unscaledTime;
            var elapsed = now - _lastMotionTime;
            var speed = movement / (_dragSpan * Mathf.Max(elapsed, 1f / 120f));
            _dragVelocity = Mathf.Lerp(_dragVelocity, speed, elapsed < FlickMemory ? .55f : 1f);
            _lastDragX = x;
            _lastMotionTime = now;
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (_dragPointer != eventData.pointerId)
                return;

            Drag(eventData);
            _dragPointer = null;
            if (_paintTurn == null)
                return;

            var velocity = Time.unscaledTime - _lastMotionTime <= FlickMemory ? _dragVelocity * _gripDirection : 0f;
            var flick = _dragProgress >= .2f && velocity >= FlickSpeed;
            var commit = _dragProgress >= .5f || flick;
            var goal = commit ? 1f : 0f;
            var remainder = Mathf.Abs(goal - _dragProgress);
            var duration = Mathf.Clamp(PageTurnDuration * remainder / (flick ? 1.2f : 1f), .22f, .49f);
            _turn = Tween.Custom(_dragProgress, goal, duration, _paintTurn, flick ? Ease.OutSine : Ease.InOutSine,
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
            PlaceClueAreas(originRest, destinationRest, moving);
            for (var index = 0; index < _pages.Count; index++)
            {
                var rect = PlaceFlatPage(index);
                var stationary = index == originRest || index == destinationRest;
                rect.gameObject.SetActive(stationary || index == moving);
                if (stationary)
                    rect.SetSiblingIndex(leaf.GetSiblingIndex());
                if (index != moving)
                    continue;

                var size = rect.rect.size;
                var offset = rect.localPosition - leaf.localPosition;
                rect.SetParent(leaf, false);
                rect.anchorMin = rect.anchorMax = leaf.pivot;
                rect.sizeDelta = size;
                rect.anchoredPosition = offset;
                _printedText.Attach(rect);
            }
            _shadow.transform.SetAsLastSibling();
            leaf.SetAsLastSibling();
        }

        private void PlaceClueAreas(int first, int second, int moving)
        {
            for (var index = 0; index <= _pages.Count; index++)
            {
                var pending = index == _pages.Count;
                var rect = pending ? _pendingClues : _clueAreas[index];
                rect.SetParent(_board, false);
                var left = index % 2 == 0;
                rect.anchorMin = new Vector2(left ? .045f : .545f, .055f);
                rect.anchorMax = new Vector2(left ? .455f : .955f, pending ? .94f : .48f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.gameObject.SetActive((!pending || HasPendingClues) &&
                    (index == first || index == second || index == moving));
                if (moving < 0)
                    continue;

                var leaf = _turningFace.rectTransform;
                if (index != moving)
                {
                    rect.SetSiblingIndex(leaf.GetSiblingIndex());
                    continue;
                }

                var size = rect.rect.size;
                var offset = rect.localPosition - leaf.localPosition;
                rect.SetParent(leaf, false);
                rect.anchorMin = rect.anchorMax = leaf.pivot;
                rect.sizeDelta = size;
                rect.anchoredPosition = offset;
                _printedText.Attach(rect);
            }
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
            _faceCurl.SetBend(.36f * Mathf.Sqrt(Mathf.Max(0f, 1f - width * width)), rect.pivot.x, _gripHeight);
            _printedText.Refresh();
        }

        private void SetContentInteractive(bool interactive)
        {
            foreach (var area in _clueAreas)
                SetGroup(GetGroup(area), interactive);
            SetGroup(GetGroup(_pendingClues), interactive);
            foreach (var page in _pages)
                SetGroup(page.Group, interactive);
        }

        private static CanvasGroup GetGroup(RectTransform rect) => rect.TryGetComponent<CanvasGroup>(out var group)
            ? group
            : rect.gameObject.AddComponent<CanvasGroup>();

        private static void SetGroup(CanvasGroup group, bool interactive)
        {
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }

        private Image CreateImage(string name)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.gameObject.layer = _board.gameObject.layer;
            image.transform.SetParent(_board, false);
            return image;
        }

    }
}
