using _Project.Develop.Data.Scent;
using UnityEngine;

namespace _Project.Develop.Gameplay.Player
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ScentTrail : MonoBehaviour
    {
        [SerializeField] private ScentData scentData;
        [SerializeField] private Transform start;
        [SerializeField] private Transform end;

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

        private void Awake()
        {
            var filter = GetComponent<MeshFilter>();
            _renderer = GetComponent<MeshRenderer>();

            if (scentData == null || start == null || end == null || _renderer.sharedMaterial == null)
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
            if (!_isVisible)
                return;

            var startPosition = transform.InverseTransformPoint(start.position);
            var endPosition = transform.InverseTransformPoint(end.position);

            _properties.SetVector(StartId, startPosition);
            _properties.SetVector(EndId, endPosition);
            _properties.SetColor(ColorId, scentData.color);
            if (scentData.flowTexture != null)
                _properties.SetTexture(TextureId, scentData.flowTexture);
            _properties.SetFloat(SpeedId, scentData.flowSpeed);
            _properties.SetFloat(WaveId, scentData.waveAmplitude);
            _properties.SetFloat(WidthId, scentData.ribbonWidth);
            _properties.SetFloat(IntensityId, scentData.intensity);
            _renderer.SetPropertyBlock(_properties);

            var bounds = new Bounds((startPosition + endPosition) * 0.5f, Vector3.zero);
            bounds.Encapsulate(startPosition);
            bounds.Encapsulate(endPosition);
            var padding = Vector3.Distance(startPosition, endPosition) * 0.12f
                          + scentData.waveAmplitude + scentData.ribbonWidth;
            bounds.Expand(new Vector3(padding * 2f, padding * 2f, 0.1f));
            _mesh.bounds = bounds;
        }

        private void OnEnable()
        {
            SetVisible(SmellAbility.IsActive);
        }

        public void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;

            if (_renderer != null)
                _renderer.enabled = isVisible;
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
