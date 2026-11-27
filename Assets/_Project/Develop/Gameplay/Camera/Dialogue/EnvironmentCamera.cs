using UnityEngine;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Camera
{
    [DefaultExecutionOrder(30000)]
    public class EnvironmentCamera : MonoBehaviour
    {
        [SerializeField] private UnityCamera source;
        [SerializeField] private bool foreground;
        [SerializeField, Range(0f, 6f)] private float blurPixelsAt1080p = 2.4f;
        [SerializeField, Range(0.1f, 1f)] private float falloffRadius = 0.52f;
        private Transform playerAnchor;
        private Transform npcAnchor;
        private UnityCamera capture;
        private MeshRenderer screen;
        private RenderTexture target;
        private Material material;
        private int captureMask;
        private float strength;

        public void SetStrength(float value, Transform anchor, Transform interlocutorAnchor)
        {
            strength = value;
            playerAnchor = anchor;
            if (interlocutorAnchor) npcAnchor = interlocutorAnchor;
        }

        private void Awake()
        {
            capture = GetComponent<UnityCamera>();
            var layer = foreground ? "Foreground" : "Background";
            captureMask = LayerMask.GetMask(layer);
            var surface = new GameObject(layer + " Screen");
            surface.transform.SetParent(source.transform, false);
            surface.transform.localPosition = Vector3.forward * (source.nearClipPlane + 1f);
            surface.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            screen = surface.AddComponent<MeshRenderer>();
            if (foreground) screen.sortingLayerName = layer;
            screen.sortingOrder = short.MinValue;
            material = new Material(Resources.Load<Shader>("Environment"));
            if (foreground) material.SetInt("_BubbleStencilTest", (int)UnityEngine.Rendering.CompareFunction.NotEqual);
            screen.sharedMaterial = material;
            screen.enabled = false;
        }

        private void LateUpdate()
        {
            if (strength <= 0f)
            {
                if (capture.enabled) StopCapture();
                return;
            }
            if (!capture.enabled)
            {
                capture.CopyFrom(source);
                capture.depth = source.depth - 1;
                capture.cullingMask = captureMask | LayerMask.GetMask("Lighting");
                capture.clearFlags = CameraClearFlags.SolidColor;
                if (foreground) capture.backgroundColor = Color.clear;
                source.cullingMask &= ~captureMask;
                capture.enabled = true;
                screen.enabled = true;
            }
            int width = Mathf.Max(1, source.pixelWidth);
            int height = Mathf.Max(1, source.pixelHeight);
            if (!target || target.width != width || target.height != height)
            {
                ReleaseTarget();
                target = new RenderTexture(width, height, 24, foreground ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.DefaultHDR);
                target.filterMode = FilterMode.Bilinear;
                target.wrapMode = TextureWrapMode.Clamp;
                capture.targetTexture = target;
                material.mainTexture = target;
            }
            transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            capture.projectionMatrix = source.projectionMatrix;
            var focusOffset = Vector3.up * 2.3f;
            var focus = playerAnchor.parent.position + focusOffset;
            Vector3 viewport = source.WorldToViewportPoint(focus);
            material.SetVector("_Focus", new Vector4(viewport.x, viewport.y, source.aspect, falloffRadius));
            var npcFocus = npcAnchor
                ? npcAnchor.parent.position + focusOffset
                : focus;
            Vector3 npcViewport = source.WorldToViewportPoint(npcFocus);
            material.SetVector("_NpcFocus", new Vector4(npcViewport.x, npcViewport.y, 0f, 0f));
            material.SetFloat("_Radius", strength * blurPixelsAt1080p * height / 1080f);
        }

        private void StopCapture()
        {
            source.cullingMask |= captureMask;
            capture.enabled = false;
            screen.enabled = false;
            ReleaseTarget();
        }

        private void ReleaseTarget()
        {
            capture.targetTexture = null;
            if (target) Destroy(target);
            target = null;
        }

        private void OnDisable()
        {
            if (capture && capture.enabled) StopCapture();
        }

        private void OnDestroy()
        {
            Destroy(material);
        }
    }
}
