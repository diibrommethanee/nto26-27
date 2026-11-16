using _Project.Develop.Data.Questing;
using _Project.Develop.Data.Train;
using _Project.Develop.Gameplay.Dialogue;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Npc;
using _Project.Develop.Gameplay.Train;
using _Project.Develop.Gameplay.Quests;
using _Project.Develop.Infrastructure.EntryPoints;
using MessagePipe;
using NaughtyAttributes;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Infrastructure.DI
{
    public class GameScope : LifetimeScope
    {
        [SerializeField, Expandable, Space(5)] private TrainSettings trainSettings;
        [SerializeField] private QuestDatabase questDatabase;
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<InputController>().AsSelf();
            
            var options = new MessagePipeOptions();
            
            builder.RegisterMessageBroker<DialogueStatementShown>(options);
            builder.RegisterMessageBroker<DialogueEnded>(options);

            builder.Register<StatementNodeHandler>(Lifetime.Scoped).As<IDialogueNodeHandler>();
            builder.Register<EndNodeHandler>(Lifetime.Scoped).As<IDialogueNodeHandler>();
            builder.Register<DialogueRunner>(Lifetime.Scoped);
            
            builder.RegisterMessageBroker<NpcInteractedEvent>(options);
            builder.RegisterMessageBroker<QuestCompletedEvent>(options);
            builder.RegisterMessageBroker<QuestStepCompletedEvent>(options);
            builder.RegisterMessageBroker<StartQuestEvent>(options);
            
            builder.RegisterInstance(questDatabase);
            builder.Register<IQuestRegistry, QuestRegistry>(Lifetime.Singleton);
            
            builder.RegisterEntryPoint<GameEntryPoint>();
            
            builder.RegisterMessageBroker<TrainStopped>(options);
            builder.RegisterMessageBroker<TrainDeparted>(options);
            
            builder.RegisterInstance(trainSettings);
            builder.RegisterEntryPoint<TrainMovement>().AsSelf();
            builder.RegisterEntryPoint<QuestService>();
        }
    }
}
