using System;
using _Project.Develop.Data.Dialogue;
using _Project.Develop.Data.Evidence;
using _Project.Develop.Data.Npc;
using _Project.Develop.Data.Questing.Quests;
using Unity.GraphToolkit.Editor;
using UnityEngine;

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

    [Serializable]
    public abstract class DialogueNodeBase : Node
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort("In").WithDisplayName(string.Empty).WithCapacity(PortCapacity.Multi).Build();

            DefineCharacterPorts(context);

            context.AddInputPort<string>("Text").AsTextArea(5, 8).Build();

            context.AddInputPort<Quest>("Quest").WithDisplayName("Quest").Build();
            
            context.AddInputPort<EvidenceData>("Evidence").WithDisplayName("Evidence").Build();

            context.AddInputPort<bool>("PublishTalked").WithDisplayName("Publish Talked").WithDefaultValue(false).Build();
            
            context.AddInputPort<int>("MoodChange").WithDisplayName("Mood Change").WithDefaultValue(0).Build();

            context.AddOutputPort("Out").WithDisplayName(string.Empty).WithCapacity(PortCapacity.Single).Build();
        }

        protected virtual void DefineCharacterPorts(IPortDefinitionContext context)
        {
            
        }
    }

    [Serializable, Node("Dialogue", "Assets/_Project/Develop/Editor/Dialogue/Icons/Dialogue.png", "Dialogue")]
    public class DialogueNode : DialogueNodeBase
    {
        protected override void DefineCharacterPorts(IPortDefinitionContext context)
        {
            context.AddInputPort<NpcData>("Character").WithDisplayName("Character").Build();

            context.AddInputPort<string>("OverrideName").WithDisplayName("Override Name").Build();
        }
    }

    [Serializable, Node("Dialogue", "Assets/_Project/Develop/Editor/Dialogue/Icons/Dialogue.png", "NextDialogue")]
    public class NextDialogueNode : DialogueNodeBase
    {
        
    }

    [Serializable, Node("Dialogue", "Assets/_Project/Develop/Editor/Dialogue/Icons/Choice.png", "Choice")]
    public class ChoiceNode : Node
    {
        private const int MaxOptions = 5;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            var countOption = context.AddOption<int>("OptionsCount").WithDisplayName(string.Empty).WithDefaultValue(2).Delayed().Build();

            countOption.TryGetValue<int>(out var count);
            count = Mathf.Clamp(count, 1, MaxOptions);

            for (var i = 1; i <= count; i++)
                context.AddOption<ChoiceConditionType>($"Condition{i}").WithDisplayName(string.Empty).WithDefaultValue(ChoiceConditionType.Always).Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort("In").WithDisplayName(string.Empty).WithCapacity(PortCapacity.Multi).Build();

            GetNodeOptionByName("OptionsCount").TryGetValue<int>(out var count);
            count = Mathf.Clamp(count, 1, MaxOptions);

            for (var i = 1; i <= count; i++)
            {
                context.AddInputPort<string>($"Text{i}").WithDisplayName($"Text {i}").AsTextArea(2, 4).Build();

                GetNodeOptionByName($"Condition{i}").TryGetValue<ChoiceConditionType>(out var condition);

                if (condition == ChoiceConditionType.RequiredMood)
                {
                    context.AddInputPort<NpcData>($"Character{i}").WithDisplayName("Character").Build();
                    context.AddInputPort<int>($"MoodThreshold{i}").WithDisplayName("Threshold").WithDefaultValue(0).Build();
                }
                else if (condition == ChoiceConditionType.HasEvidence)
                {
                    context.AddInputPort<EvidenceData>($"Evidence{i}").WithDisplayName("Evidence").Build();
                }
                else if (condition == ChoiceConditionType.HasHypothesis)
                {
                    context.AddInputPort<HypothesisData>($"Hypothesis{i}").WithDisplayName("Hypothesis").Build();
                }

                context.AddOutputPort($"Out{i}").WithDisplayName(string.Empty).WithCapacity(PortCapacity.Single).Build();
            }
        }
    }

    [Serializable, Node("Dialogue", "Assets/_Project/Develop/Editor/Dialogue/Icons/End.png", "End")]
    public class EndNode : Node
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort("In").WithDisplayName(string.Empty).WithCapacity(PortCapacity.Multi).Build();
            
            context.AddInputPort<DialogueDefinition>("SetDialogue").WithDisplayName("Set Dialogue").Build();
        }
    }
}
