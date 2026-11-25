using _Project.Develop.Data.Npc;
using UnityEngine;

namespace _Project.Develop.Gameplay.Npc
{
    public readonly struct NpcInteractedEvent
    {
        public NpcData Npc { get; }
        public Transform DialogueAnchor { get; }

        public NpcInteractedEvent(NpcData npc, Transform dialogueAnchor)
        {
            Npc = npc;
            DialogueAnchor = dialogueAnchor;
        }
    }
}
