using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.UI.Evidence
{
    internal class ClueFlightAnimation
    {
        private readonly RectTransform _notebookButton;
        private readonly Image _clueFlightImage;
        private readonly float _clueFlyDuration;
        private readonly float _clueFlyArc;
        private readonly float _clueFlyIconSize;
        private readonly CancellationToken _destroyCancellationToken;

        public ClueFlightAnimation(RectTransform notebookButton, Image clueFlightImage,
            float clueFlyDuration, float clueFlyArc, float clueFlyIconSize, CancellationToken destroyCancellationToken)
        {
            _notebookButton = notebookButton;
            _clueFlightImage = clueFlightImage;
            _clueFlyDuration = clueFlyDuration;
            _clueFlyArc = clueFlyArc;
            _clueFlyIconSize = clueFlyIconSize;
            _destroyCancellationToken = destroyCancellationToken;
        }

        public UniTask FlyDialogueClueAsync(TMP_Text source)
        {
            var targetCanvas = _notebookButton.GetComponentInParent<Canvas>().rootCanvas;
            var canvasRect = (RectTransform)targetCanvas.transform;
            var sourceCanvas = source.canvas.rootCanvas;
            var sourceCamera = EventCamera(sourceCanvas);
            var targetCamera = EventCamera(targetCanvas);
            var jobs = new List<UniTask>();
            var info = source.textInfo;
            for (var i = 0; i < info.linkCount; i++)
            {
                var link = info.linkInfo[i];
                if (!link.GetLinkID().StartsWith(EvidenceView.DialogueClueLinkPrefix, StringComparison.Ordinal))
                    continue;

                if (!TryGetVisibleBounds(info, link, out var bounds))
                    continue;

                var screen = RectTransformUtility.WorldToScreenPoint(sourceCamera,
                    source.rectTransform.TransformPoint(bounds.center));
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, targetCamera,
                        out var position))
                    jobs.Add(FlyPhraseAsync(link.GetLinkText(), source, canvasRect, position));
            }

            return UniTask.WhenAll(jobs);
        }

        private static bool TryGetVisibleBounds(TMP_TextInfo info, TMP_LinkInfo link, out Bounds bounds)
        {
            bounds = new Bounds();
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
            return hasGlyph;
        }

        private static Camera EventCamera(Canvas canvas) =>
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

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

            var scaleTween = Tween.Scale(rect, 0.35f, _clueFlyDuration, Ease.InCubic);
            try
            {
                var target = canvas.InverseTransformPoint(_notebookButton.position);
                await Tween.LocalPosition(rect, target, _clueFlyDuration, Ease.InOutCubic)
                    .SetCancellationToken(_destroyCancellationToken);
            }
            finally
            {
                scaleTween.Stop();
                UnityEngine.Object.Destroy(flight);
            }
        }

        public async UniTask FlyClueToNotebookAsync(Sprite icon, Vector3 worldPosition, bool isOutlineSticker)
        {
            var canvas = _notebookButton.GetComponentInParent<Canvas>().rootCanvas;
            var flightParent = (RectTransform)_clueFlightImage.transform.parent;
            var worldCamera = Camera.main;
            if (worldCamera == null)
                return;

            var screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
            var eventCamera = EventCamera(canvas);
            if (screenPosition.z <= 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    flightParent, screenPosition, eventCamera, out var startPosition))
                return;

            await UniTask.WaitWhile(() => _clueFlightImage.enabled, cancellationToken: _destroyCancellationToken);
            var flightRect = _clueFlightImage.rectTransform;
            flightRect.localPosition = startPosition;
            flightRect.sizeDelta = new Vector2(_clueFlyIconSize, _clueFlyIconSize);
            var startScale = isOutlineSticker ? 0.65f : 1f;
            var endScale = isOutlineSticker ? 0.75f : 0.45f;
            flightRect.localScale = Vector3.one * startScale;

            _clueFlightImage.sprite = icon;
            _clueFlightImage.enabled = true;

            try
            {
                var targetPosition = (Vector2)flightParent.InverseTransformPoint(_notebookButton.position);
                await Tween.Custom(flightRect, 0f, 1f, _clueFlyDuration, (rect, progress) =>
                {
                    var position = Vector2.LerpUnclamped(startPosition, targetPosition, progress);
                    position.y += Mathf.Sin(progress * Mathf.PI) * _clueFlyArc;
                    rect.localPosition = position;
                    rect.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, progress);
                }, Ease.InOutCubic).SetCancellationToken(_destroyCancellationToken);
            }
            finally
            {
                if (_clueFlightImage != null)
                    _clueFlightImage.enabled = false;
            }
        }
    }
}
