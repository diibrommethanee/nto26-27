namespace _Project.Develop.Gameplay.Events
{
    public readonly struct NpcInteractedEvent
    {
        public string NpcId { get;  }
        public string NpcName { get;  }

        public NpcInteractedEvent(string npcId, string npcName)
        {
            this.NpcId = npcId;
            this.NpcName = npcName;
        }
    }
}