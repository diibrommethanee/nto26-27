using System.Collections.Generic;
using System.Linq;
using _Project.Develop.Data.Quests;
using UnityEngine;

namespace _Project.Develop.Gameplay.Quests
{
    public class QuestProgress
    {
        public QuestData Data { get; }
        public List<QuestStepProgress> Steps { get; }

        public int CurrentStepIndex { get; private set; }

        public bool IsCompleted => CurrentStepIndex >= Steps.Count;

        public QuestStepProgress CurrentStep =>
            IsCompleted ? null : Steps[CurrentStepIndex];

        public QuestProgress(QuestData data)
        {
            Data = data;
            Steps = data.steps.Select(x => new QuestStepProgress(x)).ToList();
        }

        public void AdvanceStep()
        {
            CurrentStepIndex++;
        }
    }
}
