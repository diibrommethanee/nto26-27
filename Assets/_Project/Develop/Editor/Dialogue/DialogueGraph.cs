using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace _Project.Develop.Editor.Dialogue
{
    [Serializable, Graph(AssetExtension)]
    public class DialogueGraph : Graph
    {
        public const string AssetExtension = "dg";

        [MenuItem("Assets/Create/Dialogue/Dialogue Graph")]
        private static void CreateGraph()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<DialogueGraph>();
        }
    }
}