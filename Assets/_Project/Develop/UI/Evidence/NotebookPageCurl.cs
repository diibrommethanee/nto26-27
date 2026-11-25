using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.UI.Evidence
{
    internal class NotebookPageCurl : BaseMeshEffect
    {
        public const float LiftHeight = 18f;
        public const float VerticalStretch = .055f;
        private const int Strips = 12;
        private float _bend;
        private float _hinge;
        private float _gripHeight;

        public void SetBend(float bend, float hinge, float gripHeight = 0f)
        {
            if (_bend == bend && _hinge == hinge && _gripHeight == gripHeight)
                return;

            _bend = bend;
            _hinge = hinge;
            _gripHeight = gripHeight;
            graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || _bend == 0f || mesh.currentVertCount != 4)
                return;

            var bottomLeft = UIVertex.simpleVert;
            var topLeft = bottomLeft;
            var topRight = bottomLeft;
            var bottomRight = bottomLeft;
            mesh.PopulateUIVertex(ref bottomLeft, 0);
            mesh.PopulateUIVertex(ref topLeft, 1);
            mesh.PopulateUIVertex(ref topRight, 2);
            mesh.PopulateUIVertex(ref bottomRight, 3);
            mesh.Clear();

            for (var strip = 0; strip <= Strips; strip++)
            {
                var u = (float)strip / Strips;
                var distance = Mathf.Clamp01(Mathf.Abs(u - _hinge) * (_hinge == .5f ? 2f : 1f));
                var lift = GetLift(distance, _bend);
                mesh.AddVert(Bend(bottomLeft, bottomRight, u, lift * (1f - _gripHeight * .6f)));
                mesh.AddVert(Bend(topLeft, topRight, u, lift * (1f + _gripHeight * .6f)));
                if (strip == 0)
                    continue;

                var vertex = strip * 2;
                mesh.AddTriangle(vertex - 2, vertex - 1, vertex + 1);
                mesh.AddTriangle(vertex - 2, vertex + 1, vertex);
            }
        }

        private static UIVertex Bend(UIVertex left, UIVertex right, float u, float lift)
        {
            var vertex = left;
            vertex.position = Vector3.LerpUnclamped(left.position, right.position, u);
            vertex.position.y += lift * LiftHeight + vertex.position.y * lift * VerticalStretch;
            vertex.uv0 = Vector4.LerpUnclamped(left.uv0, right.uv0, u);
            var color = Color.LerpUnclamped(left.color, right.color, u);
            var light = 1f - lift * .16f;
            vertex.color = new Color(color.r * light, color.g * light, color.b * light, color.a);
            return vertex;
        }

        public Vector3 BendPoint(Vector3 point)
        {
            var rect = graphic.rectTransform.rect;
            var u = Mathf.InverseLerp(rect.xMin, rect.xMax, point.x);
            var height = Mathf.InverseLerp(rect.yMin, rect.yMax, point.y) * 2f - 1f;
            var lift = GetLift(Mathf.Abs(u - _hinge), _bend) * (1f + height * _gripHeight * .6f);
            point.y += lift * LiftHeight + point.y * lift * VerticalStretch;
            return point;
        }

        public static float GetLift(float distance, float bend) => Mathf.Sin(Mathf.Clamp01(distance) * Mathf.PI * .8f) * bend;
    }
}
