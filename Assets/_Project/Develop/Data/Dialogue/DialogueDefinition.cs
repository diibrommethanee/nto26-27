using System;
using System.Collections.Generic;
using _Project.Develop.Data.Npc;
using _Project.Develop.Data.Questing.Quests;
using UnityEngine;

namespace _Project.Develop.Data.Dialogue
{
    public class DialogueDefinition : ScriptableObject
    {
        public Hash128 graphId;
        public Hash128 entryNodeId;

        [SerializeReference] public List<NodeData> nodes = new();
    }

    [Serializable]
    public abstract class NodeData
    {
        public Hash128 id;
    }

    [Serializable]
    public class DialogueNodeData : NodeData
    {
        public NpcData character;
        public string overrideName;
        public bool inheritCharacter;
        public string text;
        public Quest quest;
        public bool publishTalked;
        public int moodChange;
        public Hash128 nextNodeId;
    }
    
    [Serializable]
    public class ChoiceNodeData : NodeData
    {
        public List<ChoiceOptionData> options = new();
    }

    [Serializable]
    public class ChoiceOptionData
    {
        public string text;

        public ChoiceConditionType condition;
        public NpcData character;
        public int moodThreshold;

        public Hash128 nextNodeId;
    }
    
    public enum ChoiceConditionType
    {
        Always = 0,
        RequiredMood = 1
    }

    [Serializable]
    public class EndNodeData : NodeData
    {
        public DialogueDefinition setDialogue;
    }
}
