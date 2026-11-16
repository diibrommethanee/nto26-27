using _Project.Develop.Data.Npc;

namespace _Project.Develop.Gameplay.Npc
{
    public readonly struct NpcInteractedEvent
    {
        public NpcData Npc { get; }

        public NpcInteractedEvent(NpcData npc)
        {
            Npc = npc;
        }
    }
}
