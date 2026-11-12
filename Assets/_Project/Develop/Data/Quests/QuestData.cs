using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Develop.Data.Quests
{
    [CreateAssetMenu(fileName = "QuestData", menuName = "Quest Data")]
    public class QuestData : ScriptableObject
    {
        public string questId;
        public string questName;
        [TextArea] public string description;
        public List<QuestStepData> steps;
    }
}
