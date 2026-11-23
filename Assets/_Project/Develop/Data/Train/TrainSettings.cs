using UnityEngine;

namespace _Project.Develop.Data.Train
{
    [CreateAssetMenu(fileName = "TrainSettings", menuName = "Configs/Train Settings")]
    public class TrainSettings : ScriptableObject
    {
        [Min(0.1f)] public float minTravelTime = 60f;
        [Min(0.1f)] public float maxTravelTime = 120f;
        [Min(0.1f)] public float brakingDuration = 3f;
        [Min(0.1f)] public float stopDuration = 15f;
        [Min(0.1f)] public float accelerationDuration = 4f;
        [Min(0f)] public float travelSpeed = 2f;
    }
}
