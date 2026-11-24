using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace _Project.Develop.Data.Surfaces
{
    public enum SurfaceTexture
    {
        None,
        Paper,
        Fabric,
        Grain,
        Custom
    }

    public enum SurfacePattern
    {
        None,
        Circles,
        Rings,
        Stars,
        Diamonds,
        Crosses,
        Custom
    }

    public enum SurfaceTextureMapping
    {
        Tile,
        Stretch
    }

    [MovedFrom(true, sourceNamespace: "_Project.Develop.UI.Surface", sourceAssembly: "UI")]
    [CreateAssetMenu(menuName = "UI/Surface Style")]
    public class SurfaceStyle : ScriptableObject
    {
        public Color fill = new Color32(53, 35, 65, 255);
        public Color edge = new Color32(178, 142, 195, 255);
        public Color accent = new Color32(255, 225, 214, 255);
        [Min(0)] public float rounding = 22;
        [Min(0)] public float fusion = 48;
        [Min(0)] public float border = 2;
        [Range(0, 6)] public float ripple = 1;
        [Range(0, 1)] public float shimmer = 0.8f;
        [Header("Texture")]
        public SurfaceTexture texture;
        public Texture2D customTexture;
        public SurfaceTextureMapping textureMapping;
        [Range(0, 1)] public float textureOpacity = .22f;
        [Min(1)] public float textureTileSize = 96;
        public Color textureTint = Color.white;

        [Header("Pattern")]
        public SurfacePattern pattern;
        public Texture2D customPattern;
        public Color patternTint = new Color32(255, 235, 207, 255);
        [Range(0, 1)] public float patternOpacity = .16f;
        [Min(1)] public float patternSpacing = 42;
        [Range(.05f, 1)] public float patternSize = .42f;
        public bool staggerPattern = true;
    }
}
