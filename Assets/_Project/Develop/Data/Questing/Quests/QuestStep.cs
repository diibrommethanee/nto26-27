using System;

namespace _Project.Develop.Data.Questing.Quests
{
    [Serializable]
    public class QuestStep
    {
        public string id;
        public string title;

        public QuestTarget target;
        public QuestEventType eventType;

        public int requiredAmount = 1;
    }
}