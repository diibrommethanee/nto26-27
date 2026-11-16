using _Project.Develop.Data.Questing.Quests;

namespace _Project.Develop.Gameplay.Quests
{
    public readonly struct QuestCompletedEvent
    {
        public Quest Quest { get; }

        public QuestCompletedEvent(Quest quest)
        {
            Quest = quest;
        }
    }
}
