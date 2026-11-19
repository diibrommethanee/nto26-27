using UnityEngine;

namespace _Project.Develop.UI.Surface
{
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
    }
}
