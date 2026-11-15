using System;

namespace _Project.Develop.Data.Questing.Quests
{
    [Serializable]
    public class QuestStep
    {
        public string id;
        public string title;

        public QuestEventType eventType;
        public string targetId;

        public int requiredAmount = 1;
    }
}