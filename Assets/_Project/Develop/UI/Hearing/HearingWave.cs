using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.UI.Hearing
{
    public class HearingWave : MaskableGraphic
    {
        private float _current, _target;

        public void Tune(float current, float target)
        {
            _current = current;
            _target = target;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Draw(mesh, _target, new Color32(226, 180, 108, 255), 4);
            Draw(mesh, _current, color, 2);
        }

        private void Draw(VertexHelper mesh, float frequency, Color tint, float width)
        {
            var rect = GetPixelAdjustedRect();
            var last = new Vector2(rect.xMin, rect.center.y);
            for (int i = 1; i <= 192; i++)
            {
                float x = i / 192f;
                float y = Mathf.Sin(x * (2 + frequency * 6) * Mathf.PI * 2);
                var next = new Vector2(rect.xMin + x * rect.width, rect.center.y + y * rect.height * 0.43f);
                var offset = Vector2.Perpendicular(next - last).normalized * width * 0.5f;
                int start = mesh.currentVertCount;
                mesh.AddVert(last - offset, tint, Vector2.zero);
                mesh.AddVert(last + offset, tint, Vector2.zero);
                mesh.AddVert(next + offset, tint, Vector2.zero);
                mesh.AddVert(next - offset, tint, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start, start + 2, start + 3);
                last = next;
            }
        }
    }
}
