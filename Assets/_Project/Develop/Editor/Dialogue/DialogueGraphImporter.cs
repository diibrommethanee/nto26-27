using _Project.Develop.Data.Dialogue;
using System;
using System.Collections.Generic;
using System.IO;
using _Project.Develop.Data.Evidence;
using _Project.Develop.Data.Npc;
using _Project.Develop.Data.Questing.Quests;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace _Project.Develop.Editor.Dialogue
{
    [ScriptedImporter(13, DialogueGraph.AssetExtension)]
    public class DialogueGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var graph = GraphDatabase.LoadGraphForImporter<DialogueGraph>(ctx.assetPath);

            if (graph == null)
            {
                ctx.LogImportError("Failed to load the dialogue graph.");
                return;
            }

            try
            {
                var sourceNodes = graph.GetNodes().ToArray();
                var starts = sourceNodes.OfType<StartNode>().ToArray();

                if (starts.Length != 1)
                {
                    throw new InvalidOperationException("The dialogue must contain exactly one Start node.");
                }

                var entryNodeId = ReadNextNodeId(starts[0]);
                if (sourceNodes.Any(node => node.ID == entryNodeId && node is NextDialogueNode))
                    throw new InvalidOperationException("NextDialogue requires a preceding Dialogue node.");
                var nodes = new List<NodeData>();

                foreach (var node in sourceNodes)
                {
                    switch (node)
                    {
                        case StartNode:
                            break;

                        case DialogueNodeBase:
                            nodes.Add(new DialogueNodeData
                            {
                                id = node.ID,
                                character = node is DialogueNode ? ReadValue<NpcData>(node, "Character") : null,
                                overrideName = node is DialogueNode ? ReadText(node, "OverrideName") : string.Empty,
                                inheritCharacter = node is NextDialogueNode,
                                text = ReadText(node, "Text"),
                                quest = ReadValue<Quest>(node, "Quest"),
                                evidence = ReadValue<EvidenceData>(node, "Evidence"),
                                publishTalked = ReadValue<bool>(node, "PublishTalked"),
                                moodChange = ReadValue<int>(node, "MoodChange"),
                                nextNodeId = ReadNextNodeId(node)
                            });
                            break;

                        case EndNode:
                            nodes.Add(new EndNodeData
                            {
                                id = node.ID,
                                setDialogue = ReadValue<DialogueDefinition>(node, "SetDialogue")
                            });
                            break;

                        case ChoiceNode:
                            nodes.Add(ReadChoice(node));
                            break;

                        default:
                            throw new InvalidOperationException($"Unsupported node: " + $"{node.GetType().Name}.");
                    }
                }

                var runtime = ScriptableObject.CreateInstance<DialogueDefinition>();

                runtime.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

                runtime.graphId = graph.ID;
                runtime.entryNodeId = entryNodeId;
                runtime.nodes = nodes;

                ctx.AddObjectToAsset("Runtime", runtime);
                ctx.SetMainObject(runtime);
            }
            catch (InvalidOperationException exception)
            {
                ctx.LogImportError(exception.Message);
            }
        }

        private static ChoiceNodeData ReadChoice(INode node)
        {
            var choice = new ChoiceNodeData { id = node.ID };

            for (var i = 1; i <= node.OutputPortCount; i++)
            {
                node.GetNodeOptionByName($"Condition{i}")
                    .TryGetValue<ChoiceConditionType>(out var condition);

                var option = new ChoiceOptionData
                {
                    text = ReadText(node, $"Text{i}"),
                    condition = condition,
                    nextNodeId = ReadNextNodeId(node, $"Out{i}")
                };

                if (condition == ChoiceConditionType.RequiredMood)
                {
                    option.character = ReadValue<NpcData>(node, $"Character{i}");
                    option.moodThreshold = ReadValue<int>(node, $"MoodThreshold{i}");
                }
                else if (condition == ChoiceConditionType.HasEvidence)
                {
                    option.requiredEvidence = ReadValue<EvidenceData>(node, $"Evidence{i}");
                }
                else if (condition == ChoiceConditionType.HasHypothesis)
                {
                    option.requiredHypothesis = ReadValue<HypothesisData>(node, $"Hypothesis{i}");
                }

                choice.options.Add(option);
            }

            return choice;
        }

        private static Hash128 ReadNextNodeId(INode node, string portName = "Out")
        {
            var port = node.GetOutputPortByName(portName);
            var connection = port?.FirstConnectedPort;

            if (connection == null)
            {
                throw new InvalidOperationException($"Node {node.GetType().Name} ({node.ID}) has no connection on its {portName} port.");
            }

            var target = connection.GetNode();

            if (connection.Name != "In" || (target is not DialogueNodeBase && target is not ChoiceNode && target is not EndNode))
            {
                throw new InvalidOperationException("Flow outputs must connect to the In port of a Dialogue, NextDialogue, Choice or End node.");
            }

            return target.ID;
        }

        private static string ReadText(INode node, string portName)
        {
            return ReadValue<string>(node, portName) ?? string.Empty;
        }

        private static T ReadValue<T>(INode node, string portName)
        {
            var port = node.GetInputPortByName(portName);

            if (port == null)
            {
                throw new InvalidOperationException(
                    $"Port {portName} was not found.");
            }

            if (port.IsConnected)
            {
                throw new InvalidOperationException($"Set {portName} directly on the node. " + "Connected value sources are not supported yet.");
            }

            if (!port.TryGetValue<T>(out var value))
            {
                throw new InvalidOperationException($"Failed to read {portName}.");
            }

            return value;
        }
    }
}
