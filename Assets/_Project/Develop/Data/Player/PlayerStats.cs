using NaughtyAttributes;
using UnityEngine;

namespace _Project.Develop.Data.Player
{
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Player Stats")]
    public class PlayerStats : ScriptableObject
    {
        [BoxGroup("Movement"), Min(0f)] public float speed = 3f;
        [BoxGroup("Movement"), Min(0.01f)] public float acceleration = 8f;
        [BoxGroup("Movement"), Min(0f)] public float stoppingDistance = 0.05f;
    }
}