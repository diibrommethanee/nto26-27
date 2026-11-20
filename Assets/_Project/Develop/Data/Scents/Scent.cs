using UnityEngine;

namespace _Project.Develop.Data.Scents
{
    [CreateAssetMenu(menuName = "Scent")]
    public class Scent : ScriptableObject
    {
        public Color color = Color.white;
        public Texture2D flowTexture;
        public float flowSpeed = 0.5f;
        public float waveAmplitude = 0.1f;
        public float ribbonWidth = 0.3f;
        [Range(0f, 1f)] public float intensity = 1f;
    }
}