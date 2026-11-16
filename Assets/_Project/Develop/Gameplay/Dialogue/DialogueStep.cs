using UnityEngine;

namespace _Project.Develop.Gameplay.Dialogue
{
    public enum DialogueStepKind
    {
        End,
        Wait,
        GoTo
    }

    public readonly struct DialogueStep
    {
        public readonly DialogueStepKind kind;
        public readonly Hash128 nextNodeId;

        private DialogueStep(DialogueStepKind kind, Hash128 nextNodeId = default)
        {
            this.kind = kind;
            this.nextNodeId = nextNodeId;
        }

        public static DialogueStep Wait(Hash128 nextNodeId)
        {
            return new DialogueStep(DialogueStepKind.Wait, nextNodeId);
        }

        public static DialogueStep GoTo(Hash128 nextNodeId)
        {
            return new DialogueStep(DialogueStepKind.GoTo, nextNodeId);
        }

        public static DialogueStep End()
        {
            return new DialogueStep(DialogueStepKind.End);
        }
    }
}