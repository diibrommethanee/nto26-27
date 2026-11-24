using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.UI.Evidence
{
    [AddComponentMenu("Layout/Hypothesis Sentence Layout")]
    public class HypothesisSentenceLayout : LayoutGroup
    {
        [SerializeField, Min(0)] private float columnSpacing = 8f;
        [SerializeField, Min(0)] private float rowSpacing = 4f;

        private readonly List<Rect> _positions = new();

        public override void SetLayoutHorizontal()
        {
            MeasureRows();
            for (var i = 0; i < rectChildren.Count; i++)
                SetChildAlongAxis(rectChildren[i], 0, _positions[i].x, _positions[i].width);
        }

        public override void CalculateLayoutInputVertical()
        {
            var height = MeasureRows();
            SetLayoutInputForAxis(height, height, -1, -1, 1);
        }

        public override void SetLayoutVertical()
        {
            for (var i = 0; i < rectChildren.Count; i++)
                SetChildAlongAxis(rectChildren[i], 1, _positions[i].y, _positions[i].height);
        }

        private float MeasureRows()
        {
            _positions.Clear();
            var availableWidth = Mathf.Max(1f, rectTransform.rect.width - padding.horizontal);
            var rowStart = 0;
            var x = 0f;
            var y = (float)padding.top;
            var rowHeight = 0f;

            foreach (var child in rectChildren)
            {
                var width = Mathf.Clamp(LayoutUtility.GetPreferredWidth(child), 1f, availableWidth);
                var height = LayoutUtility.GetPreferredHeight(child);
                if (child.TryGetComponent<TMP_Text>(out var text))
                    height = text.GetPreferredValues(text.text, width, 0).y;

                if (x > 0 && x + width > availableWidth)
                {
                    CentreRow(rowStart, rowHeight);
                    y += rowHeight + rowSpacing;
                    rowStart = _positions.Count;
                    x = 0;
                    rowHeight = 0;
                }

                _positions.Add(new Rect(padding.left + x, y, width, height));
                x += width + columnSpacing;
                rowHeight = Mathf.Max(rowHeight, height);
            }

            CentreRow(rowStart, rowHeight);
            return y + rowHeight + padding.bottom;
        }

        private void CentreRow(int start, float height)
        {
            for (var i = start; i < _positions.Count; i++)
            {
                var bounds = _positions[i];
                bounds.y += (height - bounds.height) * 0.5f;
                _positions[i] = bounds;
            }
        }
    }
}
