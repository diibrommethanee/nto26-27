using System;

namespace _Project.Develop.Data.Questing.Rewards
{
    [Serializable]
    public abstract class QuestReward
    {
        public abstract void Grant();
    }
}