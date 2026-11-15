using _Project.Develop.Data.Questing.Quests;

namespace _Project.Develop.Gameplay.Quests
{
    public readonly struct StartQuestEvent
    {
        public Quest Quest { get; }

        public StartQuestEvent(Quest quest)
        {
            Quest = quest;
        }
    }
}