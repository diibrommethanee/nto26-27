using System.Collections.Generic;
using _Project.Develop.Data.Questing.Rewards;
using NaughtyAttributes;
using UnityEngine;

namespace _Project.Develop.Data.Questing.Quests
{
    [CreateAssetMenu(fileName = "Quest", menuName = "Configs/Questing/Quest")]
    public class Quest : ScriptableObject
    {
        [BoxGroup] public string questName;
        
        [BoxGroup, TextArea] public string description;
        public List<QuestStep> steps;
        
        [SerializeReference] public List<QuestReward> rewards = new();
    }
}
