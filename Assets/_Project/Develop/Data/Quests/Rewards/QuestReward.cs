using System;
using UnityEngine;

namespace _Project.Develop.Data.Quests.Rewards
{
    [Serializable]
    public abstract class QuestReward
    {
        public abstract void Grant();
    }
}