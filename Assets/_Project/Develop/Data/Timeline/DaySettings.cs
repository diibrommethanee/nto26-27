using UnityEngine;

namespace _Project.Develop.Data.Timeline
{
    [CreateAssetMenu(menuName = "Configs/Timeline/Day Settings")]
    public class DaySettings : ScriptableObject
    {
        [Min(1)] public int startingMinutes = 480;
        [Min(1)] public int minutesPerPhase = 120;
        [Min(1)] public int dialogueCostMinutes = 30;
        [Min(1)] public int abilityCostMinutes = 15;
    }
}
