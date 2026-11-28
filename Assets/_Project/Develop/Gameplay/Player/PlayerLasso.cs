using _Project.Develop.Data.Player;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Player.Abilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Player
{
    internal class PlayerLasso : IDisposable
    {
        private readonly PlayerStats _settings;
        private readonly InputController _input;
        private readonly EvidenceService _evidence;
        private readonly UnityCamera _camera;
        private readonly PlayerAbility _ability;
        private readonly Transform _owner;
        private readonly CancellationToken _destroyToken;
        private readonly List<Vector2> _points = new();
        private readonly List<float> _crossings = new();
        private readonly List<Vector2> _smoothedStroke = new();
        private readonly List<Vector3> _stroke = new();
        private readonly LineRenderer _line;
        private readonly Material _material;
        private readonly Texture2D _strokeTexture;
        private Vector2 _startPosition;
        private Vector2 _drawCenter;
        private float _ropeLength;
        private Vector2 _min;
        private Vector2 _max;
        private bool _held;
        private bool _drawing;

        public PlayerLasso(InputController input, EvidenceService evidence, UnityCamera camera,
            PlayerAbility ability, GameObject owner, PlayerStats settings)
        {
            _settings = settings;
            _input = input;
            _evidence = evidence;
            _camera = camera;
            _ability = ability;
            _owner = owner.transform;
            _destroyToken = owner.GetCancellationTokenOnDestroy();

            _strokeTexture = CreateStrokeTexture();
            _material = new Material(Shader.Find("Sprites/Default")) { mainTexture = _strokeTexture };
            _line = CreateLine(owner);
            _line.colorGradient = _settings.strokeGradient;
            _line.enabled = false;
        }

        public void Tick()
        {
            if (Mouse.current == null) return;
            var button = Mouse.current.leftButton;
            var position = Mouse.current.position.ReadValue();
            if (button.wasPressedThisFrame) Begin(position);
            if (_held && (button.isPressed || button.wasReleasedThisFrame))
                ExtendStroke(position, button.wasReleasedThisFrame);

            if (button.wasReleasedThisFrame) End();
        }

        private void ExtendStroke(Vector2 position, bool released)
        {
            if (!CanDrawAt(position))
            {
                Cancel();
                return;
            }

            var point = ToWorld(position);
            if (!IsWithinDrawRadius(point))
            {
                FadeAndCancel();
                return;
            }

            _drawing |= Vector2.Distance(_startPosition, position) >= _settings.startMovePixels;
            var previous = _points[_points.Count - 1];
            var segmentLength = Vector2.Distance(previous, point);
            if (segmentLength <= (released ? 0f : _settings.pointSpacing)) return;

            var remainingLength = _settings.maxRopeLength - _ropeLength;
            if (segmentLength > remainingLength)
            {
                if (remainingLength > 0f)
                    AddPoint(Vector2.Lerp(previous, point, remainingLength / segmentLength));

                FadeAndCancel();
                return;
            }

            _ropeLength += segmentLength;
            AddPoint(point);
            if (_drawing) DrawLine();
        }

        private void Begin(Vector2 position)
        {
            BeginPressAsync(position).Forget();
        }

        private async UniTask BeginPressAsync(Vector2 position)
        {
            await UniTask.Yield(PlayerLoopTiming.LastUpdate, _destroyToken);

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            _drawCenter = _owner.position;
            var point = ToWorld(position);
            if (!CanDrawAt(position) || !IsWithinDrawRadius(point))
                return;

            _held = true;
            _drawing = false;
            _points.Clear();
            _stroke.Clear();
            _startPosition = position;
            _ropeLength = 0f;
            _min = _max = point;
            _points.Add(point);

        }

        private void End()
        {
            FinishPressAsync().Forget();
        }

        private async UniTask FinishPressAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.LastUpdate, _destroyToken);

            if (!_held)
                return;

            if (_drawing && !_input.CutsceneActive && !_input.IsZoomMode)
                CollectEnclosedClue();

            Cancel();
        }

        public void Cancel()
        {
            _held = false;
            _drawing = false;
            _points.Clear();
            _stroke.Clear();
            _line.positionCount = 0;
            _line.enabled = false;
        }

        private void CollectEnclosedClue()
        {
            if (_points.Count < 3)
                return;

            var allowEdgeTolerance = ExceedsArea(_settings.edgeTolerance * _settings.edgeTolerance * 0.5f);
            var padding = Vector2.one * _settings.edgeTolerance;
            foreach (var hit in Physics2D.OverlapAreaAll(_min - padding, _max + padding))
            {
                var clue = hit != null ? hit.GetComponentInParent<WorldClue>() : null;
                if (clue == null || clue.IsPickup || !clue.CanCollect(_ability.ActiveSkill))
                    continue;

                var position = (Vector2)hit.bounds.center;
                if (!IsWithinDrawRadius(position) || !Contains(position, allowEdgeTolerance))
                    continue;

                clue.Collect(_evidence);
                return;
            }
        }

        private bool Contains(Vector2 point, bool allowEdgeTolerance)
        {
            var inside = false;
            var nearEdge = false;

            for (int i = 0, j = _points.Count - 1; i < _points.Count; j = i++)
            {
                var a = _points[i];
                var b = _points[j];


                var edge = b - a;
                if (allowEdgeTolerance && !nearEdge && edge.sqrMagnitude > 0f)
                {
                    var t = Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
                    nearEdge = (point - (a + edge * t)).sqrMagnitude <= _settings.edgeTolerance * _settings.edgeTolerance;
                }

                if (TryGetCrossing(a, b, point.y, out var x) && point.x < x)
                    inside = !inside;
            }

            return inside || nearEdge;
        }

        private bool CanDrawAt(Vector2 screenPosition)
        {
            return !_input.CutsceneActive && !_input.IsZoomMode &&
                   _camera.pixelRect.Contains(screenPosition);
        }

        private bool IsWithinDrawRadius(Vector2 worldPosition)
        {
            return (worldPosition - _drawCenter).sqrMagnitude <= _settings.drawRadius * _settings.drawRadius;
        }

        private void AddPoint(Vector2 point)
        {
            _min = Vector2.Min(_min, point);
            _max = Vector2.Max(_max, point);
            _points.Add(point);
        }

        private bool ExceedsArea(float limit)
        {
            var size = _max - _min;
            if (_points.Count < 3 || size.x * size.y <= limit)
                return false;

            // Pair crossings on each row to measure only the spans inside the contour.
            var rowCount = Mathf.CeilToInt(size.y / _settings.areaSampleSpacing);
            var rowHeight = size.y / rowCount;
            var area = 0f;
            for (var row = 0; row < rowCount; row++)
            {
                var y = _min.y + (row + 0.5f) * rowHeight;
                _crossings.Clear();
                for (int i = 0, j = _points.Count - 1; i < _points.Count; j = i++)
                {
                    if (TryGetCrossing(_points[i], _points[j], y, out var x))
                        _crossings.Add(x);
                }

                _crossings.Sort();
                for (var i = 0; i < _crossings.Count; i += 2)
                    area += (_crossings[i + 1] - _crossings[i]) * rowHeight;

                if (area > limit)
                    return true;
            }

            return false;
        }

        private static bool TryGetCrossing(Vector2 a, Vector2 b, float y, out float x)
        {
            if ((a.y > y) == (b.y > y))
            {
                x = 0f;
                return false;
            }

            x = a.x + (b.x - a.x) * (y - a.y) / (b.y - a.y);
            return true;
        }

        private void FadeAndCancel()
        {
            if (_drawing && _points.Count >= 2)
                FadeStrokeAsync().Forget();

            Cancel();
        }

        private async UniTask FadeStrokeAsync()
        {
            BuildStroke();
            var line = CreateLine(new GameObject("Fading lasso"));
            line.positionCount = _stroke.Count;
            for (var i = 0; i < _stroke.Count; i++)
                line.SetPosition(i, _stroke[i]);

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

        private void DrawLine()
        {
            BuildStroke();
            _line.positionCount = _stroke.Count;
            for (var i = 0; i < _stroke.Count; i++)
                _line.SetPosition(i, _stroke[i]);

            _line.enabled = true;
        }

        private void BuildStroke()
        {
            _smoothedStroke.Clear();
            _smoothedStroke.Add(_points[0]);
            var last = _points.Count - 1;
            for (var i = 1; i < last; i++)
            {
                var point = _points[i];
                var neighborsMidpoint = (_points[i - 1] + _points[i + 1]) * 0.5f;
                var correction = Vector2.ClampMagnitude((neighborsMidpoint - point) * 0.35f, _settings.maxStrokeCorrection);
                _smoothedStroke.Add(point + correction);
            }
            _smoothedStroke.Add(_points[last]);

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

        private Vector2 ToWorld(Vector2 position)
        {
            return _camera.ScreenToWorldPoint(new Vector3(position.x, position.y, -_camera.transform.position.z));
        }

        public void Dispose()
        {
            Cancel();
            UnityEngine.Object.Destroy(_line);
            UnityEngine.Object.Destroy(_material);
            UnityEngine.Object.Destroy(_strokeTexture);
        }
    }
}
