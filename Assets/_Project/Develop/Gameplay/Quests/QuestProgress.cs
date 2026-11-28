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

        internal int[] Capture()
        {
            var saved = new int[Steps.Count + 1];
            saved[0] = CurrentStepIndex;
            for (var i = 0; i < Steps.Count; i++)
                saved[i + 1] = Steps[i].CurrentAmount;
            return saved;
        }

        internal static QuestProgress FromCheckpoint(Quest quest, int[] saved)
        {
            var progress = new QuestProgress(quest);
            var steps = progress.Steps;
            if (saved == null || saved.Length != steps.Count + 1 || saved[0] < 0 || saved[0] > steps.Count)
                throw new InvalidDataException($"Invalid saved step index for quest '{quest.name}'.");

            for (var i = 0; i < steps.Count; i++)
                if (saved[i + 1] < 0)
                    throw new InvalidDataException($"Negative progress for quest '{quest.name}', step {i}.");

            for (var i = 0; i < steps.Count; i++)
                steps[i].RestoreAmount(saved[i + 1]);
            progress.CurrentStepIndex = saved[0];
            return progress;
        }

        internal void AdvanceStep()
        {
            CurrentStepIndex++;
        }
    }
}
