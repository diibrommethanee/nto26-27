using System.Collections.Generic;
using _Project.Develop.Data.Questing.Quests;

namespace _Project.Develop.Data.Questing
{
    public interface IQuestRegistry
    {
        bool Contains(Quest quest);
        bool TryGetStep(Quest quest, string stepId, out QuestStep step);
        bool TryGetStepIndex(Quest quest, string stepId, out int index);
        bool TryGetNextStep(Quest quest, string stepId, out QuestStep nextStep);
        
        IReadOnlyCollection<Quest> AllQuests { get; }
    }
}
