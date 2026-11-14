using _Project.Develop.Data.Quests;

namespace _Project.Develop.Gameplay.Events
{
    public readonly struct StartQuestEvent
    {
        public QuestData QuestData { get; }

        public StartQuestEvent(QuestData questData)
        {
            this.QuestData = questData;
        }
    }
}