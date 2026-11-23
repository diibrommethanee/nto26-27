using System;
using _Project.Develop.Data.Dialogue;
using _Project.Develop.Data.Questing.Quests;
using _Project.Develop.Data.Surfaces;
using UnityEngine;

namespace _Project.Develop.Data.Npc
{
    [CreateAssetMenu(fileName = "NpcData", menuName = "Configs/Npc Data")]
    public class NpcData : QuestTarget
    {
        public string displayName;
        [Range(-100, 100)] public int initialMood;
        
        [Space(5)] public DialogueDefinition dialogue;

        public SurfaceStyle surfaceStyle;

        [NonSerialized] public NpcState state;
    }
    
    [Serializable]
    public class NpcState
    {
        public int mood;
        public DialogueDefinition dialogue;
    }
}
