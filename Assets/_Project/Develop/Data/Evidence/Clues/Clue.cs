using _Project.Develop.Data.Questing.Quests;
using _Project.Develop.Data.Questing.Rewards;
using UnityEngine;

namespace _Project.Develop.Data.Evidence
{
    public enum ClueType { Outline, Collectible }
    public enum ClueSticker { Red, Blue, Green }

    [CreateAssetMenu(menuName = "Configs/Evidence/Clue")]
    public class Clue : QuestTarget
    {
        public string title;
        [TextArea] public string description;
        public Sprite icon;
        public ClueType type;
        public ClueSticker sticker;
        public Hypothesis hypothesis;
        public QuestReward reward = new();

        [Header("Hypothesis Text")]
        public string fillWord;
    }
}
