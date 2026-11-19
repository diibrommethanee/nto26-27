using System.Collections.Generic;
using _Project.Develop.Data.Questing.Quests;
using UnityEngine;

namespace _Project.Develop.Data.Questing
{
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "Configs/Questing/Quest Database")]
    public class QuestDatabase : ScriptableObject
    {
        public List<Quest> quests = new();
    }
}
