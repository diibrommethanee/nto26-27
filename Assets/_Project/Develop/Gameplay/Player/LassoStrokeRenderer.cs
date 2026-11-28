using System;
using System.Collections.Generic;
using System.Threading;
using _Project.Develop.Data.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Develop.Gameplay.Player
{
    internal class LassoStrokeRenderer : IDisposable
    {
        private readonly PlayerStats _settings;
        private readonly CancellationToken _destroyToken;
        private readonly List<Vector2> _smoothedStroke = new();
        private readonly List<Vector3> _stroke = new();
        private readonly LineRenderer _line;
        private readonly Material _material;
        private readonly Texture2D _strokeTexture;

        public LassoStrokeRenderer(GameObject owner, PlayerStats settings, CancellationToken destroyToken)
        {
            _settings = settings;
            _destroyToken = destroyToken;
            _strokeTexture = CreateStrokeTexture();
            _material = new Material(Shader.Find("Sprites/Default")) { mainTexture = _strokeTexture };
            _line = CreateLine(owner);
            _line.colorGradient = _settings.strokeGradient;
            _line.enabled = false;
        }

        public void Hide()
        {
            _line.positionCount = 0;
            _line.enabled = false;
        }

        public async UniTask FadeAsync(IReadOnlyList<Vector2> points)
        {
            var line = CreateLine(new GameObject("Fading lasso"));
            SetStrokePositions(line, points);

            var gradient = _line.colorGradient;
            var originalAlpha = gradient.alphaKeys;
            var fadingAlpha = gradient.alphaKeys;
            line.colorGradient = gradient;

            try
            {
                var startTime = Time.unscaledTime;
                var progress = 0f;
                while (progress < 1f)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, _destroyToken);
                    progress = Mathf.Clamp01((Time.unscaledTime - startTime) / _settings.fadeDuration);
                    var opacity = 1f - Mathf.SmoothStep(0f, 1f, progress);
                    for (var i = 0; i < fadingAlpha.Length; i++)
                        fadingAlpha[i].alpha = originalAlpha[i].alpha * opacity;

                    gradient.alphaKeys = fadingAlpha;
                    line.colorGradient = gradient;
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(line.gameObject);
            }
        }

        private LineRenderer CreateLine(GameObject owner)
        {
            var line = owner.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = _material;
            line.sortingOrder = 1000;
            line.widthCurve = _settings.widthProfile;
            line.widthMultiplier = _settings.strokeWidth;
            line.numCornerVertices = 4;
            line.numCapVertices = 2;
            return line;
        }

        private Texture2D CreateStrokeTexture()
        {
            const int resolution = 128;
            var texture = new Texture2D(1, resolution, TextureFormat.RGBA32, false)
            {
                name = "Lasso brush stroke",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[resolution];
            for (var y = 0; y < resolution; y++)
            {
                var crossSection = 2f * y / (resolution - 1f) - 1f;
                var distance = Mathf.Abs(crossSection);
                var shade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.95f, 0.12f, crossSection));
                var color = Color.Lerp(_settings.fillColor, _settings.shadeColor, 0.16f + 0.34f * shade);
                color.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.68f, 1f, distance));
                pixels[y] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        public void Draw(IReadOnlyList<Vector2> points)
        {
            SetStrokePositions(_line, points);
            _line.enabled = true;
        }

        private void SetStrokePositions(LineRenderer line, IReadOnlyList<Vector2> points)
        {
            BuildStroke(points);
            line.positionCount = _stroke.Count;
            for (var i = 0; i < _stroke.Count; i++)
                line.SetPosition(i, _stroke[i]);
        }

        private void BuildStroke(IReadOnlyList<Vector2> points)
        {
            _smoothedStroke.Clear();
            _smoothedStroke.Add(points[0]);
            var last = points.Count - 1;
            for (var i = 1; i < last; i++)
            {
                var point = points[i];
                var neighborsMidpoint = (points[i - 1] + points[i + 1]) * 0.5f;
                var correction = Vector2.ClampMagnitude((neighborsMidpoint - point) * 0.35f, _settings.maxStrokeCorrection);
                _smoothedStroke.Add(point + correction);
            }
            _smoothedStroke.Add(points[last]);

            _stroke.Clear();
            _stroke.Add(_smoothedStroke[0]);
            for (var i = 1; i < _smoothedStroke.Count - 1; i++)
            {
                var point = _smoothedStroke[i];
                var incoming = _smoothedStroke[i - 1] - point;
                var outgoing = _smoothedStroke[i + 1] - point;
                var entry = point + Vector2.ClampMagnitude(incoming * 0.45f, _settings.cornerRadius);
                var exit = point + Vector2.ClampMagnitude(outgoing * 0.45f, _settings.cornerRadius);
                if (Vector2.Dot(incoming.normalized, outgoing.normalized) > 0.65f)
                {
                    // Fold back gently on sharp reversals instead of drawing a pointed hook.
                    AppendStrokePoint(Vector2.Lerp(entry, exit, 0.5f));
                    continue;
                }

                for (var sample = 0; sample <= 4; sample++)
                {
                    var t = sample * 0.25f;
                    var start = Vector2.Lerp(entry, point, t);
                    var end = Vector2.Lerp(point, exit, t);
                    AppendStrokePoint(Vector2.Lerp(start, end, t));
                }
            }

            _stroke.Add(_smoothedStroke[_smoothedStroke.Count - 1]);
        }

        private void AppendStrokePoint(Vector2 point)
        {
            if (((Vector3)point - _stroke[_stroke.Count - 1]).sqrMagnitude >= 0.015f * 0.015f)
                _stroke.Add(point);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(_line);
            UnityEngine.Object.Destroy(_material);
            UnityEngine.Object.Destroy(_strokeTexture);
        }
    }
}
