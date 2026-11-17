using _Project.Develop.Data.Npc;

namespace _Project.Develop.Gameplay.Npc
{
    public readonly struct NpcTalkedEvent
    {
        public NpcData Npc { get; }

        public NpcTalkedEvent(NpcData npc)
        {
            Npc = npc;
        }
    }
}