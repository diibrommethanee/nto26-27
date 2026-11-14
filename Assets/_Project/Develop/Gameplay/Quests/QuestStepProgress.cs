using System;
using _Project.Develop.Data.Quests;

namespace _Project.Develop.Gameplay.Quests
{
    public class QuestStepProgress
    {
        public QuestStepData Data { get; }

        public int CurrentAmount { get; private set; }

        public bool IsCompleted => CurrentAmount >= Data.requiredAmount;

        public QuestStepProgress(QuestStepData data)
        {
            Data = data;
        }

        public void AddProgress(int amount)
        {
            CurrentAmount = Math.Min(
                CurrentAmount + amount,
                Data.requiredAmount
            );
        }
    }
}