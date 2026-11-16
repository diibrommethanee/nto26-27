using System;
using System.Collections.Generic;
using _Project.Develop.Data.Dialogue;
using MessagePipe;
using UnityEngine;

namespace _Project.Develop.Gameplay.Dialogue
{
    public sealed class DialogueRunner
    {
        private const int MaxAutomaticSteps = 128;

        private readonly Dictionary<Hash128, DialogueNodeData> _nodes = new();
        private readonly Dictionary<Type, IDialogueNodeHandler> _handlers = new();
        private readonly IPublisher<DialogueEnded> _endedPublisher;

        private Hash128 _nextNodeId;
        private DialogueNodeData _waitingNode;
        private bool _waitingForContinue;
        private bool _processing;

        public bool IsRunning { get; private set; }

        public DialogueRunner(IEnumerable<IDialogueNodeHandler> handlers, IPublisher<DialogueEnded> endedPublisher)
        {
            _endedPublisher = endedPublisher;

            foreach (var handler in handlers)
            {
                if (!_handlers.TryAdd(handler.NodeType, handler))
                {
                    throw new InvalidOperationException($"Duplicate dialogue handler for {handler.NodeType.Name}.");
                }
            }
        }

        public void Start(DialogueDefinition dialogue)
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
                if (completedNode != null &&
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

        public void Stop()
        {
            if (!IsRunning)
                return;

            IsRunning = false;
            _waitingForContinue = false;
            _waitingNode = null;
            _nextNodeId = default;
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
                        Fail($"Dialogue node '{_nextNodeId}' was not found.");
                        return;
                    }

                    if (!_handlers.TryGetValue(node.GetType(), out var handler))
                    {
                        Fail($"No dialogue handler registered for {node.GetType().Name}.");
                        return;
                    }

                    var step = handler.Handle(node);

                    // A message subscriber may have stopped the dialogue.
                    if (!IsRunning)
                        return;

                    switch (step.kind)
                    {
                        case DialogueStepKind.Wait:
                            _waitingNode = node;
                            _nextNodeId = step.nextNodeId;
                            _waitingForContinue = true;
                            return;

                        case DialogueStepKind.GoTo:
                            _nextNodeId = step.nextNodeId;
                            break;

                        case DialogueStepKind.End:
                            Stop();
                            return;

                        default:
                            Fail($"Unsupported dialogue step: {step.kind}.");
                            return;
                    }
                }

                Fail("Dialogue exceeded the automatic step limit. Check for a loop.");
            }
            finally
            {
                _processing = false;
            }
        }

        private void Fail(string message)
        {
            Debug.LogError(message);
            Stop();
        }
    }
}
