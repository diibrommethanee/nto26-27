using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.UI.Evidence
{
    internal class NotebookTurnShadow : MaskableGraphic
    {
        private const int Columns = 8;

        public void SetPose(RectTransform leaf, float width, float opacity)
        {
            var rect = rectTransform;
            rect.anchorMin = rect.anchorMax = leaf.anchorMin;
            rect.pivot = leaf.pivot;
            rect.anchoredPosition = leaf.anchoredPosition;
            rect.sizeDelta = new Vector2(width, leaf.rect.height);
            color = new Color(.23f, .13f, .20f, opacity);
            gameObject.SetActive(opacity > .001f);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var area = rectTransform.rect;
            var onLeft = rectTransform.pivot.x > .5f;
            for (var column = 0; column <= Columns; column++)
            {
                var u = (float)column / Columns;
                var distance = onLeft ? 1f - u : u;
                var strength = 1f - distance;
                var ink = color;
                ink.a *= strength * strength;
                var x = Mathf.Lerp(area.xMin, area.xMax, u);
                mesh.AddVert(new Vector3(x, area.yMin), ink, Vector2.zero);
                mesh.AddVert(new Vector3(x, area.yMax), ink, Vector2.zero);
                if (column == 0)
                    continue;

                var index = column * 2;
                mesh.AddTriangle(index - 2, index - 1, index + 1);
                mesh.AddTriangle(index - 2, index + 1, index);
            }
        }
    }
}
