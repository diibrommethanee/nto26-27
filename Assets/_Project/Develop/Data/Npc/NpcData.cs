using _Project.Develop.Data.Dialogue;
using _Project.Develop.Data.Questing.Quests;
using NaughtyAttributes;
using UnityEngine;

namespace _Project.Develop.Data.Npc
{
    [CreateAssetMenu(fileName = "NpcData", menuName = "Configs/Npc Data")]
    public class NpcData : QuestTarget
    {
        [BoxGroup] public string displayName;
        [BoxGroup] public DialogueDefinition dialogue;
    }
}
