using _Project.Develop.Gameplay.Camera;
using UnityEngine;

namespace _Project.Develop.UI
{
    // Runtime-only preview: no scene or prefab wiring required.
    public sealed class CameraFeedbackView : MonoBehaviour
    {
        private static readonly Color Gold = new(1f, 0.8f, 0.48f);
        private static readonly Color Ivory = new(1f, 0.94f, 0.78f);
        private CameraController _camera;
        private CanvasGroup _group;
        private RectTransform _frame;
        private readonly UnityEngine.UI.Image[] _lines = new UnityEngine.UI.Image[16];
        private readonly UnityEngine.UI.Image[] _details = new UnityEngine.UI.Image[12];
        private readonly UnityEngine.UI.Image[] _shade = new UnityEngine.UI.Image[4];
        private readonly UnityEngine.UI.Image[] _trails = new UnityEngine.UI.Image[12];
        private Texture2D _softTexture;
        private Sprite _softSprite;
        private float _visibility;
        private float _pulse;
        private float _pulseVelocity;
        private float _nextSearch;
        private float _flow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var root = new GameObject("CameraFeedback", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(CanvasGroup));
            DontDestroyOnLoad(root);
            root.AddComponent<CameraFeedbackView>();
        }

        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _group = GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;

            // One shared soft mask for glows and trails; no imported assets or materials.
            const int size = 64;
            _softTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _softTexture.wrapMode = TextureWrapMode.Clamp;
            _softTexture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x, y) / (size - 1f) * 2f - Vector2.one;
                var alpha = Mathf.Pow(Mathf.Clamp01(1f - point.sqrMagnitude), 3f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            _softTexture.SetPixels(pixels);
            _softTexture.Apply(false, true);
            _softSprite = Sprite.Create(_softTexture, new Rect(0, 0, size, size), Vector2.one * 0.5f);

            for (var edge = 0; edge < 4; edge++)
            {
                var horizontal = edge < 2;
                var anchor = horizontal ? new Vector2(0.5f, edge) : new Vector2(edge - 2, 0.5f);
                var shade = MakeImage("EdgeShade", transform, anchor, Vector2.zero,
                    horizontal ? new Vector2(2600, 220) : new Vector2(220, 1600), true);
                _shade[edge] = shade;
            }

            _frame = new GameObject("FocusFrame", typeof(RectTransform)).GetComponent<RectTransform>();
            _frame.SetParent(transform, false);
            _frame.anchorMin = Vector2.zero;
            _frame.anchorMax = Vector2.one;
            for (var corner = 0; corner < 4; corner++)
            {
                var anchor = new Vector2(corner % 2, corner / 2);
                var inward = new Vector2(anchor.x == 0 ? 1 : -1, anchor.y == 0 ? 1 : -1);
                for (var glow = 0; glow < 2; glow++)
                for (var axis = 0; axis < 2; axis++)
                {
                    var index = corner * 4 + glow * 2 + axis;
                    var thickness = glow == 0 ? 16f : 1.5f;
                    var length = axis == 0 ? 100f : 66f;
                    var dimensions = axis == 0 ? new Vector2(length, thickness) : new Vector2(thickness, length);
                    var position = Vector2.Scale(inward, axis == 0
                        ? new Vector2(length * 0.5f, 0) : new Vector2(0, length * 0.5f));
                    _lines[index] = MakeImage("Corner", _frame, anchor, position, dimensions, glow == 0);
                }
                _details[corner * 3] = MakeImage("CornerGem", _frame, anchor,
                    -inward * 7f, Vector2.one * 4f, false);
                _details[corner * 3].rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                _details[corner * 3 + 1] = MakeImage("FineMark", _frame, anchor,
                    Vector2.Scale(inward, new Vector2(114, 0)), new Vector2(8, 1), false);
                _details[corner * 3 + 2] = MakeImage("FineMark", _frame, anchor,
                    Vector2.Scale(inward, new Vector2(0, 80)), new Vector2(1, 8), false);
            }
            for (var i = 0; i < _trails.Length; i++)
                _trails[i] = MakeImage("ScrollWisp", transform, new Vector2(i < 6 ? 0 : 1, 0.5f),
                    Vector2.zero, new Vector2(180, 4), true);
        }

        private UnityEngine.UI.Image MakeImage(string label, Transform parent, Vector2 anchor,
            Vector2 position, Vector2 size, bool soft)
        {
            var image = new GameObject(label, typeof(RectTransform), typeof(UnityEngine.UI.Image))
                .GetComponent<UnityEngine.UI.Image>();
            image.raycastTarget = false;
            if (soft) image.sprite = _softSprite;
            var rect = image.rectTransform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return image;
        }

        private void LateUpdate()
        {
            if (_camera == null && Time.unscaledTime >= _nextSearch)
            {
                _camera = FindFirstObjectByType<CameraController>();
                _nextSearch = Time.unscaledTime + 0.5f;
            }
            var active = _camera != null && _camera.isActiveAndEnabled;
            var zoom = active && _camera.IsZoomMode;
            var pulse = active ? _camera.ScrollPulse : 0f;
            _pulse = Mathf.SmoothDamp(_pulse, pulse, ref _pulseVelocity, 0.045f,
                Mathf.Infinity, Time.unscaledDeltaTime);
            _visibility = Mathf.MoveTowards(_visibility, zoom ? 1f : 0f,
                Time.unscaledDeltaTime * 5f);
            _group.alpha = Mathf.Max(_visibility, _pulse);
            var ease = 1f - Mathf.Pow(1f - _visibility, 3f);
            var inset = Mathf.Lerp(20f, 44f, ease) - _pulse * 3f;
            _frame.offsetMin = Vector2.one * inset;
            _frame.offsetMax = -Vector2.one * inset;
            var breath = 0.78f + Mathf.Sin(Time.unscaledTime * 1.6f) * 0.07f;
            for (var i = 0; i < _lines.Length; i++)
                Tint(_lines[i], i % 4 < 2 ? Gold : Ivory,
                    _visibility * (i % 4 < 2 ? 0.26f + _pulse * 0.12f : breath));
            for (var i = 0; i < _details.Length; i++)
                Tint(_details[i], Gold, _visibility * (i % 3 == 0 ? 0.75f : 0.4f));
            for (var i = 0; i < _shade.Length; i++)
                Tint(_shade[i], new Color(0.06f, 0.04f, 0.025f), _visibility * 0.32f);

            var direction = active ? _camera.ScrollDirection : 1f;
            _flow += Time.unscaledDeltaTime * (0.4f + _pulse * 2.2f);
            for (var i = 0; i < _trails.Length; i++)
            {
                var lane = i % 6;
                var phase = Mathf.Repeat(_flow + lane * 0.173f, 1f);
                var side = i < 6 ? 1f : -1f;
                var travel = (phase - 0.5f) * 130f * direction;
                var rect = _trails[i].rectTransform;
                rect.anchoredPosition = new Vector2(side * (38f + lane % 3 * 24f) + travel,
                    (lane - 2.5f) * 108f);
                rect.sizeDelta = new Vector2(95f + lane % 3 * 48f, lane % 2 == 0 ? 3f : 6f);
                var envelope = Mathf.Sin(phase * Mathf.PI);
                Tint(_trails[i], Color.Lerp(Ivory, Gold, ease),
                    _pulse * envelope * (zoom ? 0.42f : 0.62f));
            }
        }

        private static void Tint(UnityEngine.UI.Image image, Color color, float alpha)
        {
            color.a = alpha;
            image.color = color;
        }

        private void OnDestroy()
        {
            if (_softSprite != null) Destroy(_softSprite);
            if (_softTexture != null) Destroy(_softTexture);
        }
    }
}