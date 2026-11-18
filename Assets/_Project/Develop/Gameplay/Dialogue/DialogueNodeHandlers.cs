using System;
using _Project.Develop.Data.Dialogue;
using _Project.Develop.Gameplay.Quests;
using MessagePipe;
using System.Collections.Generic;

namespace _Project.Develop.Gameplay.Dialogue
{
    public class DialogueNodeHandler : IDialogueNodeHandler, IDialogueContinuationHandler, IDisposable
    {
        private readonly IPublisher<DialogueShown> _publisher;
        private readonly IPublisher<StartQuestEvent> _questPublisher;
        private readonly IDisposable _endedSubscription;
        private DialogueNodeData _previousDialogue;

        public Type NodeType => typeof(DialogueNodeData);

        public DialogueNodeHandler(IPublisher<DialogueShown> publisher, IPublisher<StartQuestEvent> questPublisher, ISubscriber<DialogueEnded> endedSubscriber)
        {
            _publisher = publisher;
            _questPublisher = questPublisher;
            _endedSubscription = endedSubscriber.Subscribe(_ => _previousDialogue = null);
        }

        public DialogueStep Handle(NodeData node)
        {
            var dialogue = (DialogueNodeData)node;
            var resolved = new DialogueNodeData
            {
                id = dialogue.id,
                character = dialogue.inheritCharacter ? _previousDialogue?.character : dialogue.character,
                overrideName = dialogue.inheritCharacter ? _previousDialogue?.overrideName : dialogue.overrideName,
                text = dialogue.text,
                quest = dialogue.quest,
                publishTalked = dialogue.publishTalked,
                moodChange = dialogue.moodChange,
                nextNodeId = dialogue.nextNodeId
            };
            _previousDialogue = resolved;
            _publisher.Publish(new DialogueShown(resolved));

            return DialogueStep.Wait(dialogue.nextNodeId, dialogue.publishTalked);
        }

        public void Complete(NodeData node)
        {
            var dialogue = (DialogueNodeData)node;

            if (dialogue.moodChange != 0)
            {
                var character = _previousDialogue.character;
                character.state.mood += dialogue.moodChange;
            }

            if (dialogue.quest != null)
                _questPublisher.Publish(new StartQuestEvent(dialogue.quest));
        }

        public void Dispose()
        {
            _endedSubscription.Dispose();
            _previousDialogue = null;
        }
    }

    public class ChoiceNodeHandler : IDialogueNodeHandler
    {
        public Type NodeType => typeof(ChoiceNodeData);

        public DialogueStep Handle(NodeData node)
        {
            var choice = (ChoiceNodeData)node;
            var available = new List<ChoiceOptionData>();

            foreach (var option in choice.options)
            {
                var visible = option.condition switch
                {
                    ChoiceConditionType.Always => true,
                    ChoiceConditionType.RequiredMood =>
                        option.character.state.mood >= option.moodThreshold,
                    _ => throw new InvalidOperationException($"Unsupported choice condition: {option.condition}.")
                };

                if (visible)
                    available.Add(option);
            }

            if (available.Count == 0)
                throw new InvalidOperationException($"Choice node {node.id} has no available answers.");

            return DialogueStep.Choose(available);
        }
    }

    public class EndNodeHandler : IDialogueNodeHandler
    {
        public Type NodeType => typeof(EndNodeData);

        public DialogueStep Handle(NodeData node)
        {
            return DialogueStep.End(((EndNodeData)node).setDialogue);
        }
    }
}
