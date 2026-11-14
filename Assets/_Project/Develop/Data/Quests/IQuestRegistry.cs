using System.Collections.Generic;

namespace _Project.Develop.Data.Quests
{
    public interface IQuestRegistry
    {
        bool TryGetQuest(string questId, out QuestData quest);
        bool TryGetStep(string questId, string stepId, out QuestStepData step);
        bool TryGetStepIndex(string questId, string stepId, out int index);
        bool TryGetNextStep(string questId, string stepId, out QuestStepData nextStep);
        IReadOnlyCollection<QuestData> AllQuests { get; }
    }
}