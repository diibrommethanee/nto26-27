using _Project.Develop.Data.Questing.Quests;
using UnityEngine;

namespace _Project.Develop.Data.Evidence
{
    [CreateAssetMenu(menuName = "Configs/Evidence/Clue")]
    public class Clue : QuestTarget
    {
        public string title;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Hypothesis Text")]
        public string fillWord;
    }
}
