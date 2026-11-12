using System;

namespace _Project.Develop.Data.Quests
{
    [Serializable]
    public class QuestStepData
    {
        public string id;
        public string title;

        public QuestEventType eventType;
        public string targetId;

        public int requiredAmount = 1;
    }
}