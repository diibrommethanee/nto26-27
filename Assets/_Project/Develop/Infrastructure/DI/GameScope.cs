using _Project.Develop.Gameplay;
using _Project.Develop.Gameplay.Events;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.NPC;
using _Project.Develop.Gameplay.Quests;
using _Project.Develop.Infrastructure.EntryPoints;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Infrastructure.DI
{
    public class GameScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<InputController>(Lifetime.Singleton);
            
            var options = new MessagePipeOptions();
            builder.RegisterMessageBroker<NpcInteractedEvent>(options);
            builder.RegisterMessageBroker<QuestCompletedEvent>(options);
            builder.RegisterMessageBroker<QuestStepCompletedEvent>(options);
            builder.RegisterMessageBroker<StartQuestEvent>(options);
            
            builder.RegisterEntryPoint<GameEntryPoint>();
            builder.RegisterEntryPoint<QuestService>();
            
            builder.RegisterComponentInHierarchy<TestQuestSystem>();
            builder.RegisterComponentInHierarchy<TestNPC>();
        }
    }
}
