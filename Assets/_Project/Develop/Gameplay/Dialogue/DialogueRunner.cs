using System;
using System.Collections.Generic;
using _Project.Develop.Data.Dialogue;
using _Project.Develop.Data.Npc;
using _Project.Develop.Gameplay.Npc;
using MessagePipe;
using UnityEngine;

namespace _Project.Develop.Gameplay.Dialogue
{
    public class DialogueRunner
    {
        private const int MaxAutomaticSteps = 128;

        private readonly Dictionary<Hash128, NodeData> _nodes = new();
        private readonly Dictionary<Type, IDialogueNodeHandler> _handlers = new();
        private readonly Dictionary<NpcData, (DialogueDefinition Dialogue, Hash128 NodeId,
            DialogueNodeData PreviousDialogue, bool TalkedPublished, bool ReturnPending)> _suspended = new();
        private readonly IPublisher<DialogueEnded> _endedPublisher;
        private readonly IPublisher<NpcTalkedEvent> _talkedPublisher;
        private readonly IPublisher<DialogueChoicesShown> _choicesPublisher;
        private IReadOnlyList<ChoiceOptionData> _choices;
        private NpcData _npc;
        private DialogueDefinition _dialogue;
        private bool _talkedPublished;
        private bool _playingReturnDialogue;

        private Hash128 _nextNodeId;
        private NodeData _waitingNode;
        private bool _waitingForContinue;
        private bool _processing;

        public NpcData Interlocutor => _npc;
        public bool IsRunning { get; private set; }
        public bool IsChoosing => _choices != null;

        public DialogueRunner(IEnumerable<IDialogueNodeHandler> handlers, IPublisher<DialogueEnded> endedPublisher,
            IPublisher<NpcTalkedEvent> talkedPublisher, IPublisher<DialogueChoicesShown> choicesPublisher)
        {
            _endedPublisher = endedPublisher;
            _talkedPublisher = talkedPublisher;
            _choicesPublisher = choicesPublisher;

            foreach (var handler in handlers)
            {
                if (!_handlers.TryAdd(handler.NodeType, handler))
                {
                    throw new InvalidOperationException($"Duplicate dialogue handler for {handler.NodeType.Name}.");
                }
            }
        }

        public void Start(DialogueDefinition dialogue, NpcData npc = null)
        {
            if (_processing)
            {
                Debug.LogWarning("Cannot start dialogue while processing a node.");
                return;
            }
            
            if (IsRunning)
            {
                Debug.LogWarning("Cannot start dialogue: another dialogue is active.");
                return;
            }

            _nodes.Clear();

            if (dialogue == null || dialogue.nodes == null)
            {
                Debug.LogError("Cannot start dialogue: invalid definition.");
                return;
            }

            var hasCheckpoint = npc != null && _suspended.TryGetValue(npc, out _);
            var checkpoint = hasCheckpoint ? _suspended[npc] : default;
            var sameDialogue = hasCheckpoint && checkpoint.Dialogue == dialogue;
            var returnDialogue = dialogue.returnDialogue;
            var playReturn = sameDialogue && checkpoint.ReturnPending &&
                returnDialogue != null && returnDialogue != dialogue;
            if (playReturn)
                dialogue = returnDialogue;

            foreach (var node in dialogue.nodes)
            {
                if (node == null || !_nodes.TryAdd(node.id, node))
                {
                    Debug.LogError("Cannot start dialogue: null node or duplicate node ID.");
                    _nodes.Clear();
                    return;
                }
            }

            if (!_nodes.ContainsKey(dialogue.entryNodeId))
            {
                Debug.LogError("Cannot start dialogue: entry node was not found.");
                _nodes.Clear();
                return;
            }

            _nextNodeId = dialogue.entryNodeId;
            _dialogue = dialogue;
            _npc = npc;
            _playingReturnDialogue = playReturn;
            _talkedPublished = playReturn;
            if (hasCheckpoint && !playReturn)
                _suspended.Remove(npc);

            if (!playReturn && sameDialogue && dialogue.resumeAfterExit &&
                _nodes.TryGetValue(checkpoint.NodeId, out var resumeNode) && resumeNode is not EndNodeData)
            {
                _nextNodeId = checkpoint.NodeId;
                _talkedPublished = checkpoint.TalkedPublished;
                if (_handlers[typeof(DialogueNodeData)] is DialogueNodeHandler dialogueHandler)
                    dialogueHandler.PreviousDialogue = checkpoint.PreviousDialogue;
            }
            _waitingForContinue = false;
            IsRunning = true;

            ProcessNodes();
        }

        public void Continue()
        {
            if (!IsRunning || !_waitingForContinue || _processing)
                return;

            _waitingForContinue = false;
            var completedNode = _waitingNode;
            _waitingNode = null;

            _processing = true;
            try
            {
                if (IsRunning && completedNode != null &&
                    _handlers[completedNode.GetType()] is IDialogueContinuationHandler handler)
                {
                    handler.Complete(completedNode);
                }
            }
            finally
            {
                _processing = false;
            }

            if (IsRunning)
                ProcessNodes();
        }

        public void Choose(int index)
        {
            if (!IsChoosing || _processing)
                return;

            if (index < 0 || index >= _choices.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _nextNodeId = _choices[index].nextNodeId;
            _choices = null;
            ProcessNodes();
        }

        public void Interrupt()
        {
            if (!IsRunning || _processing)
                return;

            if (_waitingForContinue && _nodes.TryGetValue(_nextNodeId, out var next) && next is EndNodeData)
            {
                Continue();
                return;
            }

            if (!_playingReturnDialogue && _npc != null && (_waitingNode != null || IsChoosing) &&
                (_dialogue.resumeAfterExit || _dialogue.returnDialogue != null))
            {
                var previousDialogue = (_handlers[typeof(DialogueNodeData)] as DialogueNodeHandler)?.PreviousDialogue;
                _suspended[_npc] = (_dialogue, _waitingNode?.id ?? _nextNodeId, previousDialogue, _talkedPublished,
                    _dialogue.returnDialogue != null);
            }

            Stop();
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            IsRunning = false;
            _waitingForContinue = false;
            _waitingNode = null;
            _choices = null;
            _nextNodeId = default;
            _npc = null;
            _dialogue = null;
            _playingReturnDialogue = false;
            _nodes.Clear();

            _endedPublisher.Publish(new DialogueEnded());
        }

        private void ProcessNodes()
        {
            _processing = true;

            try
            {
                for (var i = 0; i < MaxAutomaticSteps; i++)
                {
                    if (!IsRunning)
                        return;

                    if (!_nodes.TryGetValue(_nextNodeId, out var node))
                    {
                        var missingNodeId = _nextNodeId;
                        Stop();
                        throw new InvalidOperationException($"Dialogue node '{missingNodeId}' was not found.");
                    }

                    if (!_handlers.TryGetValue(node.GetType(), out var handler))
                    {
                        Stop();
                        throw new InvalidOperationException($"No dialogue handler registered for {node.GetType().Name}.");
                    }

                    var step = handler.Handle(node);
                    
                    if (!IsRunning)
                        return;

                    switch (step.kind)
                    {
                        case DialogueStepKind.Wait:
                            _waitingNode = node;
                            _nextNodeId = step.nextNodeId;
                            _waitingForContinue = true;
                            if (step.publishTalked)
                                PublishTalked();
                            return;

                        case DialogueStepKind.Choose:
                            _choices = step.choices;
                            _waitingForContinue = false;
                            _waitingNode = null;
                            _choicesPublisher.Publish(new DialogueChoicesShown(_choices));
                            return;

                        case DialogueStepKind.GoTo:
                            _nextNodeId = step.nextNodeId;
                            break;

                        case DialogueStepKind.End:
                            if (_npc != null && step.setDialogue != null)
                            {
                                _npc.state.dialogue = step.setDialogue;
                                _suspended.Remove(_npc);
                            }
                            else if (_playingReturnDialogue)
                            {
                                var checkpoint = _suspended[_npc];
                                checkpoint.ReturnPending = false;
                                _suspended[_npc] = checkpoint;
                            }
                            PublishTalked();
                            Stop();
                            return;

                        default:
                            Stop();
                            throw new InvalidOperationException($"Unsupported dialogue step: {step.kind}.");
                    }
                }

                Stop();
                throw new InvalidOperationException("Dialogue exceeded the automatic step limit. Check for a loop.");
            }
            catch
            {
                Stop();
                throw;
            }
            finally
            {
                _processing = false;
            }
        }

        private void PublishTalked()
        {
            if (_talkedPublished || _npc == null)
                return;
            
            _talkedPublished = true;
            _talkedPublisher.Publish(new NpcTalkedEvent(_npc));
        }
    }
}
