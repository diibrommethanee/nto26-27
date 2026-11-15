using _Project.Develop.Data.Questing.Quests;
using NaughtyAttributes;
using UnityEngine;

namespace _Project.Develop.Data.Npc
{
    [CreateAssetMenu(fileName = "NpcData", menuName = "Configs/Npc Data")]
    public class NpcData : ScriptableObject
    {
        [BoxGroup] public string id;
        [BoxGroup] public string displayName;
        [BoxGroup, Space(5)] public Quest quest;
    }
}