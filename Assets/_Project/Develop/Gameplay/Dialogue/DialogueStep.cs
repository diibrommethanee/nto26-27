using UnityEngine;

using System.Collections.Generic;
using _Project.Develop.Data.Dialogue;

namespace _Project.Develop.Gameplay.Dialogue
{
    public enum DialogueStepKind
    {
        End,
        Wait,
        GoTo,
        Choose
    }

    public readonly struct DialogueStep
    {
        public readonly DialogueStepKind kind;
        public readonly Hash128 nextNodeId;
        public readonly bool publishTalked;
        public readonly IReadOnlyList<ChoiceOptionData> choices;
        public readonly _Project.Develop.Data.Dialogue.DialogueDefinition setDialogue;

        private DialogueStep(DialogueStepKind kind, Hash128 nextNodeId = default, bool publishTalked = false,
            _Project.Develop.Data.Dialogue.DialogueDefinition setDialogue = null,
            IReadOnlyList<ChoiceOptionData> choices = null)
        {
            this.kind = kind;
            this.nextNodeId = nextNodeId;
            this.publishTalked = publishTalked;
            this.setDialogue = setDialogue;
            this.choices = choices;
        }

        public static DialogueStep Wait(Hash128 nextNodeId, bool publishTalked = false)
        {
            return new DialogueStep(DialogueStepKind.Wait, nextNodeId, publishTalked);
        }

        public static DialogueStep Choose(IReadOnlyList<ChoiceOptionData> choices)
        {
            return new DialogueStep(DialogueStepKind.Choose, choices: choices);
        }

        public static DialogueStep GoTo(Hash128 nextNodeId)
        {
            return new DialogueStep(DialogueStepKind.GoTo, nextNodeId);
        }

        public static DialogueStep End(_Project.Develop.Data.Dialogue.DialogueDefinition setDialogue = null)
        {
            return new DialogueStep(DialogueStepKind.End, setDialogue: setDialogue);
        }
    }
}
