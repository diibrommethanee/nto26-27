using System;
using _Project.Develop.Data.Dialogue;
using _Project.Develop.Data.Npc;
using UnityEngine;

namespace _Project.Develop.Data.Questing.Rewards
{
    [Serializable]
    public class QuestReward
    {
        public NpcData dialogueNpc;
        public DialogueDefinition dialogue;

        public NpcData moodNpc;
        [Min(0)] public int moodIncrease;
    }
}