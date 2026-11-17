using System;
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
        [BoxGroup, Range(-100, 100)] public int initialMood;
        
        [BoxGroup, Space(5)] public DialogueDefinition dialogue;

        [NonSerialized] public NpcState state;
    }
    
    [Serializable]
    public class NpcState
    {
        public int mood;
        public DialogueDefinition dialogue;
    }
}
