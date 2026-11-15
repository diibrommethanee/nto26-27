using NaughtyAttributes;
using UnityEngine;

namespace _Project.Develop.Data.Camera.Parallax
{
    [CreateAssetMenu(fileName = "ParallaxFactor", menuName = "Configs/Camera/Parallax Factor")]
    public class ParallaxFactor : ScriptableObject
    {
        [BoxGroup] public float value;
    }
}
