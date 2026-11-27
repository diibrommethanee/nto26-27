using _Project.Develop.Data.Questing.Quests;
using System;

namespace _Project.Develop.Gameplay.Quests
{
    public class QuestStepProgress
    {
        public QuestStep Data { get; }
        public int CurrentAmount { get; private set; }

        public bool IsCompleted => CurrentAmount >= Data.requiredAmount;

        public QuestStepProgress(QuestStep data)
        {
            Data = data;
        }

        internal void AddProgress(int amount)
        {
            CurrentAmount = Math.Min(CurrentAmount + amount, Data.requiredAmount);
        }
    }
}