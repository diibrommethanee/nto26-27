using _Project.Develop.Data.Dialogue;

namespace _Project.Develop.Gameplay.Dialogue
{
    public readonly struct DialogueStatementShown
    {
        public readonly StatementNodeData statement;

        public DialogueStatementShown(StatementNodeData statement)
        {
            this.statement = statement;
        }
    }

    public readonly struct DialogueEnded
    {
        
    }
}