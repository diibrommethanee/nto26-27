using System.Collections.Generic;
using _Project.Develop.Data.Questing.Rewards;
using _Project.Develop.Data.Surfaces;
using UnityEngine;

namespace _Project.Develop.Data.Questing.Quests
{
    [CreateAssetMenu(fileName = "Quest", menuName = "Configs/Questing/Quest")]
    public class Quest : ScriptableObject
    {
        public string questName;
        
        [TextArea] public string description;
        public List<QuestStep> steps;
        
        [SerializeReference] public List<QuestReward> rewards = new();

        [Space(5)] public SurfaceStyle surfaceStyle;
    }
}
