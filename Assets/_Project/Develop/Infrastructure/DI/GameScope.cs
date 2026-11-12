using _Project.Develop.Gameplay.Events;
using _Project.Develop.Gameplay.Input;
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
            builder.RegisterMessagePipe();
            builder.Register<InputController>(Lifetime.Singleton);
            
            var options = new MessagePipeOptions();
            builder.RegisterMessageBroker<NpcInteractedEvent>(new MessagePipeOptions());
            builder.RegisterMessageBroker<QuestCompletedEvent>(new MessagePipeOptions());
            builder.RegisterMessageBroker<QuestStepCompletedEvent>(new MessagePipeOptions());
            
            builder.RegisterEntryPoint<GameEntryPoint>();
            builder.RegisterEntryPoint<QuestService>();
        }
    }
}
