using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.UI.Evidence
{
    internal class NotebookPrintedGraphic : BaseMeshEffect
    {
        private RectTransform _leaf;
        private NotebookPageCurl _curl;

        public void Attach(RectTransform leaf, NotebookPageCurl curl)
        {
            _leaf = leaf;
            _curl = curl;
            enabled = true;
        }

        public void Refresh() => graphic.SetVerticesDirty();

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || !_leaf || _leaf.localScale.x < .002f)
                return;

            var toLeaf = _leaf.worldToLocalMatrix * transform.localToWorldMatrix;
            var fromLeaf = toLeaf.inverse;
            var vertex = UIVertex.simpleVert;
            for (var index = 0; index < mesh.currentVertCount; index++)
            {
                mesh.PopulateUIVertex(ref vertex, index);
                vertex.position = fromLeaf.MultiplyPoint3x4(_curl.BendPoint(toLeaf.MultiplyPoint3x4(vertex.position)));
                mesh.SetUIVertex(vertex, index);
            }
        }
    }
}
