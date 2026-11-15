namespace _Project.Develop.Gameplay.Npc
{
    public readonly struct NpcInteractedEvent
    {
        public string NpcId { get; }
        public string NpcName { get; }

        public NpcInteractedEvent(string npcId, string npcName)
        {
            NpcId = npcId;
            NpcName = npcName;
        }
    }
}