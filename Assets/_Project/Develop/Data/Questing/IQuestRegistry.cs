using System.Collections.Generic;
using _Project.Develop.Data.Questing.Quests;

namespace _Project.Develop.Data.Questing
{
    public interface IQuestRegistry
    {
        bool TryGetQuest(string questId, out Quest quest);
        bool TryGetStep(string questId, string stepId, out QuestStep step);
        bool TryGetStepIndex(string questId, string stepId, out int index);
        bool TryGetNextStep(string questId, string stepId, out QuestStep nextStep);
        
        IReadOnlyCollection<Quest> AllQuests { get; }
    }
}