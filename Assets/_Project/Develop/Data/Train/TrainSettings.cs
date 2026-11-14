using NaughtyAttributes;
using UnityEngine;

namespace _Project.Develop.Data.Train
{
    [CreateAssetMenu(fileName = "TrainSettings", menuName = "Train Settings")]
    public class TrainSettings : ScriptableObject
    {
        [BoxGroup, Min(0.1f)] public float minTravelTime = 60f;
        [BoxGroup, Min(0.1f)] public float maxTravelTime = 120f;
        [BoxGroup, Min(0.1f)] public float brakingDuration = 3f;
        [BoxGroup, Min(0.1f)] public float stopDuration = 15f;
        [BoxGroup, Min(0.1f)] public float accelerationDuration = 4f;
        [BoxGroup, Min(0f)] public float travelSpeed = 2f;
    }
}