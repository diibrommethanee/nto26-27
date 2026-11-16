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

        [SerializeReference]
        public List<DialogueNodeData> nodes = new();
    }

    [Serializable]
    public abstract class DialogueNodeData
    {
        public Hash128 id;
    }

    [Serializable]
    public class StatementNodeData : DialogueNodeData
    {
        public NpcData character;
        public string overrideName;
        public string text;
        public Quest quest;
        public Hash128 nextNodeId;
    }

    [Serializable]
    public sealed class EndNodeData : DialogueNodeData
    {
        
    }
}
