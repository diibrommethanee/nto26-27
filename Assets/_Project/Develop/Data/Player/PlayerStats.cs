using NaughtyAttributes;
using UnityEngine;

namespace _Project.Develop.Data.Player
{
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Configs/Player Stats")]
    public class PlayerStats : ScriptableObject
    {
        [BoxGroup("Movement"), Min(0f)] public float speed = 3f;
        [BoxGroup("Movement"), Min(0.01f)] public float acceleration = 8f;
        [BoxGroup("Movement"), Min(0f)] public float stoppingDistance = 0.05f;

        [BoxGroup("Lasso"), Min(0.01f)] public float drawRadius = 4f;
        [BoxGroup("Lasso"), Min(0.01f)] public float maxRopeLength = 7f;
        [BoxGroup("Lasso"), Min(0.01f)] public float startMovePixels = 12f;
        [BoxGroup("Lasso"), Min(0.001f)] public float pointSpacing = 0.02f;
        [BoxGroup("Lasso"), Min(0f)] public float edgeTolerance = 0.15f;
        [BoxGroup("Lasso"), Min(0.001f)] public float areaSampleSpacing = 0.05f;

        [BoxGroup("Lasso"), Min(0.001f)] public float strokeWidth = 0.075f;
        [BoxGroup("Lasso"), Min(0f)] public float maxStrokeCorrection = 0.025f;
        [BoxGroup("Lasso"), Min(0f)] public float cornerRadius = 0.085f;
        [BoxGroup("Lasso"), Min(0.01f)] public float fadeDuration = 0.28f;
        [BoxGroup("Lasso")] public Color fillColor = new Color32(211, 198, 226, 255);
        [BoxGroup("Lasso")] public Color shadeColor = new Color32(124, 98, 125, 255);
        [BoxGroup("Lasso")] public AnimationCurve widthProfile = new(new Keyframe(0f, 0.3f),
            new Keyframe(0.12f, 0.94f), new Keyframe(0.76f, 0.94f), new Keyframe(1f, 0.08f));
        [BoxGroup("Lasso")] public Gradient strokeGradient = new()
        {
            alphaKeys = new[]
            {
                new GradientAlphaKey(0.42f, 0f),
                new GradientAlphaKey(1f, 0.18f),
                new GradientAlphaKey(1f, 1f)
            }
        };
    }
}
