namespace _Project.Develop.Gameplay.Quests
{
    public readonly struct QuestStepCompletedEvent
    {
        public string QuestId { get; }
        public string StepId { get; }

        public QuestStepCompletedEvent(string questId, string stepId)
        {
            QuestId = questId;
            StepId = stepId;
        }
    }
}