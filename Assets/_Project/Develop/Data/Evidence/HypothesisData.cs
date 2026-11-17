using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Develop.Data.Evidence
{
    public enum HypothesisNodeType { Text, Fill }

    [Serializable]
    public class HypothesisNode
    {
        public HypothesisNodeType type;

        [TextArea] public string text;
        public EvidenceData correctEvidence;
        public string correctAnswerFill;
    }

    [CreateAssetMenu(menuName = "Configs/Hypothesis")]
    public class HypothesisData : ScriptableObject
    {
        public string id;
        public string title;
        public List<HypothesisNode> nodes = new();

        [Header("Reward")]
        public string rewardId;
    }
}
