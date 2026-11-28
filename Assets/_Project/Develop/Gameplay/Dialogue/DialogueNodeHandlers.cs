using _Project.Develop.Gameplay.Npc;
using System;
using _Project.Develop.Data.Dialogue;
using _Project.Develop.Gameplay.Quests;
using MessagePipe;
using System.Collections.Generic;
using _Project.Develop.Gameplay.Evidence;

namespace _Project.Develop.Gameplay.Dialogue
{
    public class DialogueNodeHandler : IDialogueNodeHandler, IDialogueContinuationHandler, IDisposable
    {
        private readonly IPublisher<DialogueShown> _publisher;
        private readonly IPublisher<StartQuestEvent> _questPublisher;
        private readonly NpcStates _states;
        private readonly EvidenceService _evidenceService;
        private readonly IDisposable _endedSubscription;
        internal DialogueNodeData PreviousDialogue { get; set; }

        public Type NodeType => typeof(DialogueNodeData);

        public DialogueNodeHandler(
            IPublisher<DialogueShown> publisher,
            IPublisher<StartQuestEvent> questPublisher,
            ISubscriber<DialogueEnded> endedSubscriber,
            EvidenceService evidenceService, NpcStates states)
        {
            _publisher = publisher;
            _questPublisher = questPublisher;
            _states = states;
            _evidenceService = evidenceService;
            _endedSubscription = endedSubscriber.Subscribe(_ => PreviousDialogue = null);
        }

        public DialogueStep Handle(NodeData node)
        {
            var dialogue = (DialogueNodeData)node;
            var resolved = dialogue.ResolveSpeaker(PreviousDialogue);
            PreviousDialogue = resolved;
            _publisher.Publish(new DialogueShown(resolved));

            return DialogueStep.Wait(dialogue.nextNodeId, dialogue.publishTalked);
        }

        public void Complete(NodeData node)
        {
            var dialogue = (DialogueNodeData)node;

            if (dialogue.moodChange != 0)
            {
                var character = PreviousDialogue.character;
                _states[character].Mood += dialogue.moodChange;
            }

            if (dialogue.quest != null)
                _questPublisher.Publish(new StartQuestEvent(dialogue.quest));

            if (dialogue.clue != null)
                _evidenceService.Collect(dialogue.clue);

            if (dialogue.hypothesis != null)
                _evidenceService.UnlockHypothesis(dialogue.hypothesis);
        }

        public void Dispose()
        {
            _endedSubscription.Dispose();
            PreviousDialogue = null;
        }
    }

    public class ChoiceNodeHandler : IDialogueNodeHandler
    {
        private readonly NpcStates _states;
        private readonly EvidenceService _evidenceService;

        public Type NodeType => typeof(ChoiceNodeData);

        public ChoiceNodeHandler(EvidenceService evidenceService, NpcStates states)
        {
            _states = states;
            _evidenceService = evidenceService;
        }

        public DialogueStep Handle(NodeData node)
        {
            var choice = (ChoiceNodeData)node;
            var available = new List<ChoiceOptionData>();

            foreach (var option in choice.options)
            {
                if (IsAvailable(option))
                    available.Add(option);
            }

            if (available.Count == 0)
                throw new InvalidOperationException($"Choice node {node.id} has no available answers.");

            return DialogueStep.Choose(available);
        }

        private bool IsAvailable(ChoiceOptionData option) =>
            option.condition switch
            {
                ChoiceConditionType.Always => true,
                ChoiceConditionType.RequiredMood =>
                    _states[option.character].Mood >= option.moodThreshold,
                ChoiceConditionType.HasClue =>
                    _evidenceService.HasClue(option.requiredClue),
                ChoiceConditionType.HasHypothesis =>
                    _evidenceService.HasSolvedHypothesis(option.requiredHypothesis),
                _ => throw new InvalidOperationException($"Unsupported choice condition: {option.condition}.")
            };
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
