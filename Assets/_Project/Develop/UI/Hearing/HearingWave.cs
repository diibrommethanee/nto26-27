using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.UI.Hearing
{
    public class HearingWave : MaskableGraphic
    {
        private const int PointCount = 48;
        private const int ArcCount = 3;
        private const float MaxTiltDegrees = 9f;
        private static readonly Color InkColor = new Color32(107, 76, 136, 255);
        private static readonly Color LockColor = new Color32(132, 101, 174, 255);
        private static readonly Color EdgeColor = new Color32(245, 226, 241, 255);
        private static readonly Color FlashColor = new Color32(239, 218, 255, 255);
        private static readonly float[] StrokeOffsets = { -5.5f, -3.5f, -2.4f, 0f, 2.4f, 3.5f, 5.5f };

        private float _time;
        private float _amplitude = 1f;
        private float _targetAmplitude = 1f;
        private float _progress;
        private float _flash;
        private float _tilt;

        public void ResetSignal()
        {
            _amplitude = _targetAmplitude = 1f;
            _progress = _flash = 0f;
        }

        public void Tune(float distance, float tolerance)
        {
            float noise = Mathf.InverseLerp(tolerance, tolerance + 0.23f, distance);
            _targetAmplitude = Mathf.Lerp(0.065f, 1f, noise);
        }

        public void FacePlayer(Vector2 direction)
        {
            _tilt = MaxTiltDegrees * direction.normalized.x;
        }

        public void SetProgress(float progress) => _progress = progress;

        public void Flash() => _flash = 1f;

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            _time += delta * 2f;
            _amplitude = Mathf.MoveTowards(_amplitude, _targetAmplitude, delta * 4f);
            _flash = Mathf.MoveTowards(_flash, 0f, delta * 2.8f);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            Quaternion tilt = Quaternion.Euler(0f, 0f, _tilt);

            for (int arc = 0; arc < ArcCount; arc++)
                DrawArc(mesh, rect, tilt, arc);
        }

        private void DrawArc(VertexHelper mesh, Rect rect, Quaternion tilt, int arc)
        {
            int firstVertex = mesh.currentVertCount;
            int stride = StrokeOffsets.Length;
            float travel = Mathf.Repeat(_time * 0.26f + arc / (float)ArcCount, 1f);
            float opacity = Mathf.SmoothStep(0f, 1f, travel / 0.14f)
                * (1f - Mathf.SmoothStep(0f, 1f, (travel - 0.72f) / 0.28f));
            float arcWidth = rect.width * Mathf.Lerp(0.18f, 0.80f, travel);
            float baseline = rect.center.y + rect.height * (0.58f - travel * 1.05f);
            float strokeWidth = Mathf.Lerp(0.68f, 1.12f, travel);
            float lockBlend = Mathf.SmoothStep(0f, 1f, _progress);
            Color ink = Color.Lerp(Color.Lerp(InkColor, color, 0.15f), LockColor, lockBlend);
            Color highlight = Color.Lerp(EdgeColor, FlashColor, _flash);

            for (int i = 0; i < PointCount; i++)
            {
                float along = i / (float)(PointCount - 1);
                Vector2 point = Sample(rect, arc, arcWidth, baseline, along);
                Vector2 tangent = Sample(rect, arc, arcWidth, baseline, along + 0.004f)
                    - Sample(rect, arc, arcWidth, baseline, along - 0.004f);
                Vector2 normal = Vector2.Perpendicular(tangent).normalized;
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.Min(along, 1f - along) * 8f);
                float width = (0.66f + fade * 0.34f) * strokeWidth * (1f + _flash * 0.18f);
                foreach (float offset in StrokeOffsets)
                {
                    float edge = Mathf.Abs(offset);
                    float rim = Mathf.InverseLerp(2.4f, 3.5f, edge);
                    Color vertexColor = Color.Lerp(ink, highlight, rim);
                    vertexColor.a *= opacity * fade * (1f - Mathf.InverseLerp(3.5f, 5.5f, edge));
                    Vector2 vertex = point + normal * (offset * width);
                    Vector2 tiltedVertex = rect.center + (Vector2)(tilt * (vertex - rect.center));
                    mesh.AddVert(tiltedVertex, vertexColor, Vector2.zero);
                }

                if (i == 0) continue;
                int previous = firstVertex + (i - 1) * stride;
                int current = previous + stride;
                for (int band = 0; band < stride - 1; band++)
                {
                    mesh.AddTriangle(previous + band, previous + band + 1, current + band + 1);
                    mesh.AddTriangle(previous + band, current + band + 1, current + band);
                }
            }
        }

        private Vector2 Sample(Rect rect, int arc, float width, float baseline, float along)
        {
            float horizontal = along * 2f - 1f;
            float bend = width * 0.105f;
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(along));
            float drift = Mathf.PerlinNoise(along * 7f + arc * 4f, _time * 0.33f) - 0.5f;
            float detail = Mathf.PerlinNoise(along * 24f + arc * 3f, _time * 0.57f + 7f) - 0.5f;
            float ripple = (drift * 0.095f + detail * 0.035f) * rect.height * envelope * _amplitude;
            float y = baseline + horizontal * horizontal * bend + ripple;
            return new Vector2(rect.center.x + horizontal * width * 0.4f, y);
        }
    }
}
