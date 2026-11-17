using _Project.Develop.Data.Dialogue;

using System.Collections.Generic;

namespace _Project.Develop.Gameplay.Dialogue
{
    public readonly struct DialogueShown
    {
        public readonly DialogueNodeData dialogue;

        public DialogueShown(DialogueNodeData dialogue)
        {
            this.dialogue = dialogue;
        }
    }

    public readonly struct DialogueChoicesShown
    {
        public readonly IReadOnlyList<ChoiceOptionData> choices;

        public DialogueChoicesShown(IReadOnlyList<ChoiceOptionData> choices)
        {
            this.choices = choices;
        }
    }

    public readonly struct DialogueEnded
    {
        
    }
}
