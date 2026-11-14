using System.Collections.Generic;
using UnityEngine;

namespace _Project.Develop.Data.Quests
{
    [CreateAssetMenu(fileName = "QuestDatabase")]
    public sealed class QuestDatabase : ScriptableObject
    {
        public List<QuestData> quests = new();
    }
}