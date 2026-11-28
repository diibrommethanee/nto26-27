using System.Collections.Generic;
using System.Linq;
using _Project.Develop.Data;
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

    public class NpcStates : ISavable
    {
        private Dictionary<NpcData, NpcState> _states = new();

        private struct SavedNpc
        {
            public int Mood;
            public string DialogueKey;
            public bool TeaServed;
        }

        [Save] private Dictionary<string, SavedNpc> Characters
        {
            get
            {
                var catalog = CheckpointCatalog.Instance;
                return _states.ToDictionary(pair => catalog.Key(pair.Key), pair => new SavedNpc
                {
                    Mood = pair.Value.Mood,
                    DialogueKey = pair.Value.Dialogue == null ? null : catalog.Key(pair.Value.Dialogue),
                    TeaServed = pair.Value.TeaServed
                });
            }
            set
            {
                var catalog = CheckpointCatalog.Instance;
                var restored = new Dictionary<NpcData, NpcState>();
                foreach (var pair in value)
                {
                    var npc = catalog.Resolve<NpcData>(pair.Key);
                    var data = pair.Value;
                    restored.Add(npc, new NpcState(npc)
                    {
                        Mood = data.Mood,
                        Dialogue = data.DialogueKey == null ? null : catalog.Resolve<DialogueDefinition>(data.DialogueKey),
                        TeaServed = data.TeaServed
                    });
                }
                _states = restored;
            }
        }

        internal void StartNewDay()
        {
            foreach (var state in _states.Values) state.TeaServed = false;
        }

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
