using _Project.Develop.Data.Surfaces;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.UI.Surface
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Surface Graphic")]
    public class SurfaceGraphic : MaskableGraphic
    {
        public enum FillSource { Fill, Edge, Accent, PatternTint }

        public FillSource FillFrom { get; set; }
        public bool ShowTextureAndPattern { get; set; } = true;
        public SurfaceStyle style;
        public RectTransform[] parts = {};
        private readonly Vector4[] _axisX = new Vector4[16];
        private readonly Vector4[] _axisY = new Vector4[16];
        private Material _instance;

        public override Material defaultMaterial => _instance ? _instance :
            _instance = new Material(Resources.Load<Shader>("SurfaceGraphic")) { hideFlags = HideFlags.HideAndDontSave };

        protected override void OnEnable()
        {
            base.OnEnable();
            Canvas.willRenderCanvases += ApplySurface;
        }

        protected override void OnDisable()
        {
            Canvas.willRenderCanvases -= ApplySurface;
            base.OnDisable();
        }

        private void ApplySurface()
        {
            if (!style || canvasRenderer.materialCount == 0) return;
            var mat = canvasRenderer.GetMaterial();
            if (!mat) return;
            var count = 0;
            foreach (var part in parts)
            {
                if (!part || !part.gameObject.activeInHierarchy) continue;
                var m = part.worldToLocalMatrix * rectTransform.localToWorldMatrix;
                var rect = part.rect;
                _axisX[count] = new Vector4(m.m00, m.m01, m.m03 - rect.center.x, rect.width * 0.5f);
                _axisY[count] = new Vector4(m.m10, m.m11, m.m13 - rect.center.y, rect.height * 0.5f);
                if (++count == _axisX.Length) break;
            }
            var bounds = GetPixelAdjustedRect();
            mat.SetVector("_Rect", new Vector4(bounds.xMin, bounds.yMin, bounds.width, bounds.height));
            mat.SetInt("_Count", count);
            mat.SetVectorArray("_AxisX", _axisX);
            mat.SetVectorArray("_AxisY", _axisY);
            mat.SetVector("_Shape", new Vector4(style.rounding, style.fusion, style.border, style.ripple));
            mat.SetFloat("_Shimmer", style.shimmer);
            mat.SetColor("_Fill", FillFrom switch
            {
                FillSource.Edge => style.edge,
                FillSource.Accent => style.accent,
                FillSource.PatternTint => style.patternTint,
                _ => style.fill
            });
            mat.SetColor("_Edge", style.edge);
            mat.SetColor("_Accent", style.accent);
            mat.SetTexture("_SurfaceTexture", style.customTexture);
            mat.SetColor("_TextureTint", style.textureTint);
            mat.SetVector("_TextureSettings", new Vector4((float)(style.texture == SurfaceTexture.Custom && !style.customTexture ? SurfaceTexture.None : style.texture), ShowTextureAndPattern ? style.textureOpacity : 0f,
                Mathf.Max(1, style.textureTileSize), (float)style.textureMapping));
            mat.SetTexture("_PatternTexture", style.customPattern);
            mat.SetColor("_PatternTint", style.patternTint);
            mat.SetVector("_PatternSettings", new Vector4((float)(style.pattern == SurfacePattern.Custom && !style.customPattern ? SurfacePattern.None : style.pattern), ShowTextureAndPattern ? style.patternOpacity : 0f,
                Mathf.Max(1, style.patternSpacing), style.patternSize));
            mat.SetFloat("_StaggerPattern", style.staggerPattern ? 1 : 0);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Application.isPlaying) Destroy(_instance);
            else DestroyImmediate(_instance);
        }
    }
}
