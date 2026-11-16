using _Project.Develop.Data.Dialogue;
using System;
using System.Collections.Generic;
using System.IO;
using _Project.Develop.Data.Npc;
using _Project.Develop.Data.Questing.Quests;
using System.Linq;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace _Project.Develop.Editor.Dialogue
{
    [ScriptedImporter(3, DialogueGraph.AssetExtension)]
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
                var nodes = new List<DialogueNodeData>();

                foreach (var node in sourceNodes)
                {
                    switch (node)
                    {
                        case StartNode:
                            break;

                        case DialogueNode:
                            nodes.Add(new StatementNodeData
                            {
                                id = node.ID,
                                character = ReadValue<NpcData>(node, "Character"),
                                overrideName = ReadText(node, "OverrideName"),
                                text = ReadText(node, "Text"),
                                quest = ReadValue<Quest>(node, "Quest"),
                                nextNodeId = ReadNextNodeId(node)
                            });
                            break;

                        case EndNode:
                            nodes.Add(new EndNodeData
                            {
                                id = node.ID
                            });
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

        private static Hash128 ReadNextNodeId(INode node)
        {
            var port = node.GetOutputPortByName("Out");
            var connection = port?.FirstConnectedPort;

            if (connection == null)
            {
                throw new InvalidOperationException($"Node {node.GetType().Name} " + $"({node.ID}) has no connection on its Out port.");
            }

            var target = connection.GetNode();

            if (connection.Name != "In" || (target is not DialogueNode && target is not EndNode))
            {
                throw new InvalidOperationException("Out must connect to the In port " + "of a Dialogue or End node.");
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
