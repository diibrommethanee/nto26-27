using _Project.Develop.Data.Questing.Quests;
using System.Collections.Generic;
using System.Linq;

namespace _Project.Develop.Gameplay.Quests
{
    public class QuestProgress
    {
        public Quest Data { get; }
        public List<QuestStepProgress> Steps { get; }

        public int CurrentStepIndex { get; private set; }

        public bool IsCompleted => CurrentStepIndex >= Steps.Count;

        public QuestStepProgress CurrentStep => IsCompleted ? null : Steps[CurrentStepIndex];

        public QuestProgress(Quest data)
        {
            Data = data;
            Steps = data.steps
                .Where(step => step.eventType != QuestEventType.Empty)
                .Select(step => new QuestStepProgress(step))
                .ToList();
        }

        public void AdvanceStep()
        {
            CurrentStepIndex++;
        }
    }
}
