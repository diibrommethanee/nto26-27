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

        internal bool TryComplete(QuestEventType eventType, QuestTarget target)
        {
            if (Data.eventType != eventType || Data.target != target) return false;

            if (CurrentAmount < Data.requiredAmount)
                CurrentAmount++;
            return IsCompleted;
        }

        internal void RestoreAmount(int amount)
        {
            CurrentAmount = Math.Min(amount, Data.requiredAmount);
        }
    }
}