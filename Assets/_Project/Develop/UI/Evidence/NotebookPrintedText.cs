using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.UI.Evidence
{
    internal class NotebookPrintedText
    {
        private const float CurlTopClearance = 24f;
        private readonly RectTransform _leaf;
        private readonly NotebookPageCurl _curl;
        private readonly List<(TMP_Text Text, Action<TMP_TextInfo> Warp)> _bindings = new();
        private readonly List<NotebookPrintedGraphic> _graphics = new();
        private readonly List<(RectMask2D Mask, Vector4 Padding)> _masks = new();

        public NotebookPrintedText(RectTransform leaf, NotebookPageCurl curl)
        {
            _leaf = leaf;
            _curl = curl;
        }

        public void Attach(RectTransform page)
        {
            foreach (var mask in page.GetComponentsInChildren<RectMask2D>())
            {
                var padding = mask.padding;
                _masks.Add((mask, padding));
                // The vertices can lift above the flat viewport during a turn.
                padding.w -= CurlTopClearance;
                mask.padding = padding;
            }

            foreach (var graphic in page.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic is TMP_Text || !graphic.isActiveAndEnabled)
                    continue;

                if (!graphic.TryGetComponent<NotebookPrintedGraphic>(out var effect))
                    effect = graphic.gameObject.AddComponent<NotebookPrintedGraphic>();
                effect.Attach(_leaf, _curl);
                _graphics.Add(effect);
            }
            foreach (var text in page.GetComponentsInChildren<TMP_Text>(true))
            {
                Action<TMP_TextInfo> warp = info => WarpText(text.transform, info);
                text.OnPreRenderText += warp;
                _bindings.Add((text, warp));
            }
        }

        public void Detach()
        {
            foreach (var (mask, padding) in _masks)
            {
                if (mask)
                    mask.padding = padding;
            }
            _masks.Clear();

            foreach (var effect in _graphics)
            {
                if (effect)
                    effect.enabled = false;
            }
            _graphics.Clear();
            foreach (var (text, warp) in _bindings)
            {
                if (!text)
                    continue;
                text.OnPreRenderText -= warp;
                text.SetVerticesDirty();
            }
            _bindings.Clear();
        }

        public void Refresh()
        {
            foreach (var effect in _graphics)
                effect.Refresh();
            foreach (var (text, _) in _bindings)
            {
                if (text.isActiveAndEnabled)
                    text.ForceMeshUpdate();
            }
        }

        private void WarpText(Transform text, TMP_TextInfo info)
        {
            if (_leaf.localScale.x < .002f)
                return;

            var toLeaf = _leaf.worldToLocalMatrix * text.localToWorldMatrix;
            var toText = toLeaf.inverse;
            for (var index = 0; index < info.materialCount; index++)
            {
                var mesh = info.meshInfo[index];
                for (var vertex = 0; vertex < mesh.vertexCount; vertex++)
                {
                    var point = toLeaf.MultiplyPoint3x4(mesh.vertices[vertex]);
                    mesh.vertices[vertex] = toText.MultiplyPoint3x4(_curl.BendPoint(point));
                }
            }
        }
    }
}
