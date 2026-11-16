using _Project.Develop.Data.Npc;
using _Project.Develop.Data.Questing.Quests;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace _Project.Develop.Editor.Dialogue
{
    [DataTypeStyleMapper(typeof(DialogueGraph))]
    public class DialogueTypeStyleMapper : DataTypeStyleMapper
    {
        public DialogueTypeStyleMapper()
        {
            var icon = EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D;
            Register(typeof(NpcData), icon, Color.white);
            Register(typeof(Quest), icon, Color.white);
        }
    }
}