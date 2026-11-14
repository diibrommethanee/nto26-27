namespace _Project.Develop.Gameplay.Events
{
    public readonly struct QuestCompletedEvent
    {
        public string QuestId { get; }

        public QuestCompletedEvent(string questId)
        {
            QuestId = questId;
        }
    }
}