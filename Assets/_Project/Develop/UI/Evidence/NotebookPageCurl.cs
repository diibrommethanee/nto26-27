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
            var strip = Mathf.InverseLerp(rect.xMin, rect.xMax, point.x) * Strips;
            var column = Mathf.Min(Mathf.FloorToInt(strip), Strips - 1);
            var x = strip - column;
            var y = Mathf.InverseLerp(rect.yMin, rect.yMax, point.y);
            var left = GetLift(Mathf.Abs((float)column / Strips - _hinge), _bend);
            var right = GetLift(Mathf.Abs((float)(column + 1) / Strips - _hinge), _bend);
            var bottomLeft = EdgeOffset(rect.yMin, left, -1f);
            var bottomRight = EdgeOffset(rect.yMin, right, -1f);
            var topLeft = EdgeOffset(rect.yMax, left, 1f);
            var topRight = EdgeOffset(rect.yMax, right, 1f);
            point.y += x <= y
                ? bottomLeft * (1f - y) + topLeft * (y - x) + topRight * x
                : bottomLeft * (1f - x) + topRight * y + bottomRight * (x - y);
            return point;
        }

        private float EdgeOffset(float y, float lift, float side) =>
            lift * (1f + side * _gripHeight * .6f) * (LiftHeight + y * VerticalStretch);

        public static float GetLift(float distance, float bend) => Mathf.Sin(Mathf.Clamp01(distance) * Mathf.PI * .8f) * bend;
    }
}
