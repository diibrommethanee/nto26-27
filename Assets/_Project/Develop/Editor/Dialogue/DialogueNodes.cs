using System;
using _Project.Develop.Data.Npc;
using _Project.Develop.Data.Questing.Quests;
using Unity.GraphToolkit.Editor;

namespace _Project.Develop.Editor.Dialogue
{
    [Serializable, Node("Dialogue", "Assets/_Project/Develop/Editor/Dialogue/Icons/Start.png", "Start")]
    public class StartNode : Node
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddOutputPort("Out").WithDisplayName(string.Empty).WithCapacity(PortCapacity.Single).Build();
        }
    }

    [Serializable, Node("Dialogue", "Assets/_Project/Develop/Editor/Dialogue/Icons/Dialogue.png", "Dialogue")]
    public class DialogueNode : Node
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort("In").WithDisplayName(string.Empty).WithCapacity(PortCapacity.Multi).Build();
            
            context.AddInputPort<NpcData>("Character").WithDisplayName("Character").Build();

            context.AddInputPort<string>("OverrideName").WithDisplayName("Override Name").Build();
            
            context.AddInputPort<string>("Text").AsTextArea(5, 8).Build();
            
            context.AddInputPort<Quest>("Quest").WithDisplayName("Quest").Build();
            
            context.AddOutputPort("Out").WithDisplayName(string.Empty).WithCapacity(PortCapacity.Single).Build();
        }
    }

    [Serializable, Node("Dialogue", "Assets/_Project/Develop/Editor/Dialogue/Icons/End.png", "End")]
    public class EndNode : Node
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort("In").WithDisplayName(string.Empty).WithCapacity(PortCapacity.Multi).Build();
        }
    }
}
