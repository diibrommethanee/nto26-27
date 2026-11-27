using System.IO;
using _Project.Develop.Data.Questing.Quests;
using System.Collections.Generic;
using System.Linq;

namespace _Project.Develop.Gameplay.Quests
{
    public class QuestProgress
    {
        public Quest Data { get; }
        public IReadOnlyList<QuestStepProgress> Steps { get; }

        public int CurrentStepIndex { get; private set; }

        public bool IsCompleted => CurrentStepIndex >= Steps.Count;

        public QuestStepProgress CurrentStep => IsCompleted ? null : Steps[CurrentStepIndex];

        public QuestProgress(Quest data)
        {
            Data = data;
            Steps = data.steps
                .Where(step => step.eventType != QuestEventType.Empty)
                .Select(step => new QuestStepProgress(step))
                .ToList().AsReadOnly();
        }

        internal void Restore(int[] saved)
        {
            if (saved == null || saved.Length != Steps.Count + 1 || saved[0] < 0 || saved[0] > Steps.Count)
                throw new InvalidDataException($"Invalid saved step index for quest '{Data.name}'.");

            for (var i = 0; i < Steps.Count; i++)
            {
                var amount = saved[i + 1];
                if (amount < 0)
                    throw new InvalidDataException($"Negative progress for quest '{Data.name}', step {i}.");
                Steps[i].AddProgress(amount);
            }
            CurrentStepIndex = saved[0];
        }

        internal void AdvanceStep()
        {
            CurrentStepIndex++;
        }
    }
}
