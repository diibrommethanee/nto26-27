using System.Collections.Generic;
using _Project.Develop.Data.Dialogue;
using _Project.Develop.Data.Npc;

namespace _Project.Develop.Gameplay.Npc
{
    public class NpcState
    {
        public int Mood { get; internal set; }
        public DialogueDefinition Dialogue { get; internal set; }
        public bool TeaServed { get; internal set; }

        internal NpcState(NpcData data)
        {
            Mood = data.initialMood;
            Dialogue = data.dialogue;
        }
    }

    public class NpcStates
    {
        private readonly Dictionary<NpcData, NpcState> _states = new();

        public NpcState this[NpcData npc]
        {
            get
            {
                if (!_states.TryGetValue(npc, out var state))
                {
                    state = new NpcState(npc);
                    _states.Add(npc, state);
                }
                return state;
            }
        }
    }
}
