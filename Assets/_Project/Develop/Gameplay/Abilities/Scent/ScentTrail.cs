using System;
using _Project.Develop.Gameplay.Player.Abilities;
using _Project.Develop.Gameplay.Tea;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Gameplay.Abilities.Scent
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ScentTrail : MonoBehaviour
    {
        [SerializeField] private Data.Scents.Scent scent;
        [SerializeField] private Transform start;
        [SerializeField] private Transform end;
        [SerializeField, Min(0.1f)] private float detectionRadius = 7f;

        private const int Segments = 16;

        private static readonly int StartId = Shader.PropertyToID("_Start");
        private static readonly int EndId = Shader.PropertyToID("_End");
        private static readonly int ColorId = Shader.PropertyToID("_ScentColor");
        private static readonly int TextureId = Shader.PropertyToID("_FlowTexture");
        private static readonly int SpeedId = Shader.PropertyToID("_FlowSpeed");
        private static readonly int WaveId = Shader.PropertyToID("_WaveAmplitude");
        private static readonly int WidthId = Shader.PropertyToID("_RibbonWidth");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        private Mesh _mesh;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _properties;
        private bool _isVisible;
        private IBufferedSubscriber<SmellVisibilityChanged> _smell;
        private NextDayBuffService _buffService;
        private PlayerAbility _player;
        private IDisposable _subscription;

        private void Awake()
        {
            var filter = GetComponent<MeshFilter>();
            _renderer = GetComponent<MeshRenderer>();

            if (scent == null || start == null || end == null || _renderer.sharedMaterial == null)
            {
                _renderer.enabled = false;
                enabled = false;
                return;
            }

            _renderer.enabled = _isVisible;

            var vertices = new Vector3[(Segments + 1) * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[Segments * 6];

            for (var i = 0; i <= Segments; i++)
            {
                var vertex = i * 2;
                var t = i / (float)Segments;
                uv[vertex] = new Vector2(t, 0f);
                uv[vertex + 1] = new Vector2(t, 1f);

                if (i == Segments)
                    continue;

                var triangle = i * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }

            _mesh = new Mesh { name = "Scent Ribbon", vertices = vertices, uv = uv, triangles = triangles };
            filter.sharedMesh = _mesh;
            _properties = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            bool withinRange = _player != null && IsWithinRange();
            if (_renderer != null)
                _renderer.enabled = _isVisible && withinRange;

            if (!_isVisible || !withinRange)
                return;

            var startPosition = transform.InverseTransformPoint(start.position);
            var endPosition = transform.InverseTransformPoint(end.position);

            _properties.SetVector(StartId, startPosition);
            _properties.SetVector(EndId, endPosition);
            _properties.SetColor(ColorId, scent.color);
            if (scent.flowTexture != null)
                _properties.SetTexture(TextureId, scent.flowTexture);
            _properties.SetFloat(SpeedId, scent.flowSpeed);
            _properties.SetFloat(WaveId, scent.waveAmplitude);
            _properties.SetFloat(WidthId, scent.ribbonWidth);
            _properties.SetFloat(IntensityId, scent.intensity);
            _renderer.SetPropertyBlock(_properties);

            var bounds = new Bounds((startPosition + endPosition) * 0.5f, Vector3.zero);
            bounds.Encapsulate(startPosition);
            bounds.Encapsulate(endPosition);
            var padding = Vector3.Distance(startPosition, endPosition) * 0.12f
                          + scent.waveAmplitude + scent.ribbonWidth;
            bounds.Expand(new Vector3(padding * 2f, padding * 2f, 0.1f));
            _mesh.bounds = bounds;
        }

        private void Start()
        {
            // Scene-placed trails resolve their own scope once. The buffered event covers late enables.
            var scope = GetComponentInParent<LifetimeScope>() ?? LifetimeScope.Find<LifetimeScope>(gameObject.scene);
            _smell = scope.Container.Resolve<IBufferedSubscriber<SmellVisibilityChanged>>();
            _buffService = scope.Container.Resolve<NextDayBuffService>();
            _player = FindAnyObjectByType<PlayerAbility>();
            Subscribe();
        }

        private bool IsWithinRange()
        {
            Vector2 from = start.position;
            Vector2 to = end.position;
            Vector2 closestPoint = from;
            Vector2 segment = to - from;
            if (segment.sqrMagnitude > 0f)
            {
                float projection = Mathf.Clamp01(Vector2.Dot((Vector2)_player.transform.position - from, segment)
                                                 / segment.sqrMagnitude);
                closestPoint = from + segment * projection;
            }

            float effectiveRadius = detectionRadius
                                    * (_buffService.ThymeActiveThisDay ? NextDayBuffService.AbilityRadiusMultiplier : 1f);
            return ((Vector2)_player.transform.position - closestPoint).sqrMagnitude
                   <= effectiveRadius * effectiveRadius;
        }

        private void OnEnable()
        {
            if (_smell != null) Subscribe();
        }

        private void Subscribe() =>
            _subscription = _smell.Subscribe(message => SetVisible(message.IsActive));

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;

            if (_renderer != null)
                _renderer.enabled = isVisible && _player != null && IsWithinRange();
        }

        private void OnDestroy()
        {
            if (_mesh == null)
                return;

            if (Application.isPlaying)
                Destroy(_mesh);
            else
                DestroyImmediate(_mesh);
        }
    }
}
