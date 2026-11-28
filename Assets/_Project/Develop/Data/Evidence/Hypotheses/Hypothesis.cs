using System;
using System.Collections.Generic;
using _Project.Develop.Data.Questing.Quests;
using _Project.Develop.Data.Questing.Rewards;
using NaughtyAttributes;
using UnityEngine;

namespace _Project.Develop.Data.Evidence
{
    public enum HypothesisNodeType { Text, Fill }

    [Serializable]
    public class HypothesisNode
    {
        public HypothesisNodeType type;

        [AllowNesting, ShowIf(nameof(IsText)), TextArea] public string text;
        [AllowNesting, ShowIf(nameof(IsFill))] public Clue correctClue;

        private bool IsText => type == HypothesisNodeType.Text;
        private bool IsFill => type == HypothesisNodeType.Fill;
    }

    [CreateAssetMenu(menuName = "Configs/Hypothesis")]
    public class Hypothesis : QuestTarget
    {
        public string title;
        public List<HypothesisNode> nodes = new();

        public QuestReward reward = new();

        public bool MatchesAnswers(IReadOnlyList<Clue> answers)
        {
            var slot = 0;
            foreach (var node in nodes)
            {
                if (node.type != HypothesisNodeType.Fill)
                    continue;

                if (slot >= answers.Count)
                    return false;

                var answer = answers[slot];
                if (answer == null || answer != node.correctClue || !answer.FitsSlot(this, slot))
                    return false;

                slot++;
            }

            return slot > 0 && slot == answers.Count;
        }
    }
}
