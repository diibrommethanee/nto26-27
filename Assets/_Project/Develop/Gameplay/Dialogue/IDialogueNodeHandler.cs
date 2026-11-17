using System;
using _Project.Develop.Data.Dialogue;

namespace _Project.Develop.Gameplay.Dialogue
{
    public interface IDialogueContinuationHandler
    {
        void Complete(NodeData node);
    }

    public interface IDialogueNodeHandler
    {
        Type NodeType { get; }
        
        DialogueStep Handle(NodeData node);
    }
}
