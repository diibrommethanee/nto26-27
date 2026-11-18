using _Project.Develop.Data.Questing.Quests;

namespace _Project.Develop.Gameplay.Quests
{
    public readonly struct QuestStepCompletedEvent
    {
        public Quest Quest { get; }
        public string StepId { get; }

        public QuestStepCompletedEvent(Quest quest, string stepId)
        {
            Quest = quest;
            StepId = stepId;
        }
    }
}
