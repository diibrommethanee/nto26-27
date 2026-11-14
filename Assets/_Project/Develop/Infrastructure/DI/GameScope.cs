using _Project.Develop.Data.Train;
using _Project.Develop.Gameplay;
using _Project.Develop.Gameplay.Events;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Train;
using _Project.Develop.Gameplay.NPC;
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
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<InputController>().AsSelf();
            
            var options = new MessagePipeOptions();
            builder.RegisterMessageBroker<NpcInteractedEvent>(options);
            builder.RegisterMessageBroker<QuestCompletedEvent>(options);
            builder.RegisterMessageBroker<QuestStepCompletedEvent>(options);
            builder.RegisterMessageBroker<StartQuestEvent>(options);
            
            builder.RegisterEntryPoint<GameEntryPoint>();
            
            builder.RegisterMessageBroker<TrainStopped>(options);
            builder.RegisterMessageBroker<TrainDeparted>(options);
            
            builder.RegisterInstance(trainSettings);
            builder.RegisterEntryPoint<TrainMovement>().AsSelf();
            builder.RegisterEntryPoint<QuestService>();
            
            builder.RegisterComponentInHierarchy<TestQuestSystem>();
            builder.RegisterComponentInHierarchy<TestNPC>();
        }
    }
}
