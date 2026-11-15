namespace _Project.Develop.Gameplay.Quests
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