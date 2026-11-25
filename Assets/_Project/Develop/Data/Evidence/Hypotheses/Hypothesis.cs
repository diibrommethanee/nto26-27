using System;
using System.Collections.Generic;
using _Project.Develop.Data.Questing.Quests;
using _Project.Develop.Data.Questing.Rewards;
using UnityEngine;

namespace _Project.Develop.Data.Evidence
{
    public enum HypothesisNodeType { Text, Fill }

    [Serializable]
    public class HypothesisNode
    {
        public HypothesisNodeType type;

        [TextArea] public string text;
        public Clue correctClue;
        public string correctAnswerFill;
    }

    [CreateAssetMenu(menuName = "Configs/Hypothesis")]
    public class Hypothesis : QuestTarget
    {
        public string title;
        public List<HypothesisNode> nodes = new();

        public QuestReward reward = new();
    }
}
