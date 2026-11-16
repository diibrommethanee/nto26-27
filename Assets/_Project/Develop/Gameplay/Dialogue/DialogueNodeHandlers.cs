using System;
using _Project.Develop.Data.Dialogue;
using _Project.Develop.Gameplay.Quests;
using MessagePipe;

namespace _Project.Develop.Gameplay.Dialogue
{
    public sealed class StatementNodeHandler : IDialogueNodeHandler, IDialogueContinuationHandler
    {
        private readonly IPublisher<DialogueStatementShown> _publisher;
        private readonly IPublisher<StartQuestEvent> _questPublisher;

        public Type NodeType => typeof(StatementNodeData);

        public StatementNodeHandler(IPublisher<DialogueStatementShown> publisher, IPublisher<StartQuestEvent> questPublisher)
        {
            _publisher = publisher;
            _questPublisher = questPublisher;
        }

        public DialogueStep Handle(DialogueNodeData node)
        {
            var statement = (StatementNodeData)node;
            _publisher.Publish(new DialogueStatementShown(statement));

            return DialogueStep.Wait(statement.nextNodeId);
        }

        public void Complete(DialogueNodeData node)
        {
            var statement = (StatementNodeData)node;

            if (statement.quest != null)
                _questPublisher.Publish(new StartQuestEvent(statement.quest));
        }
    }

    public sealed class EndNodeHandler : IDialogueNodeHandler
    {
        public Type NodeType => typeof(EndNodeData);

        public DialogueStep Handle(DialogueNodeData node)
        {
            return DialogueStep.End();
        }
    }
}
