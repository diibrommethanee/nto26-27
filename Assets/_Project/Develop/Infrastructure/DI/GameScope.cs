using _Project.Develop.Data.Train;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Train;
using _Project.Develop.Infrastructure.EntryPoints;
using MessagePipe;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Infrastructure.DI
{
    public class GameScope : LifetimeScope
    {
        [FormerlySerializedAs("trainMovementSettings")] [FormerlySerializedAs("trainStopSettings")] [SerializeField, Space(5)] private TrainSettings trainSettings;
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<InputController>().AsSelf();
            
            builder.RegisterEntryPoint<GameEntryPoint>();
            
            var options = new MessagePipeOptions();
            
            builder.RegisterMessageBroker<TrainStopped>(options);
            builder.RegisterMessageBroker<TrainDeparted>(options);
            
            builder.RegisterInstance(trainSettings);
            builder.RegisterEntryPoint<TrainMovement>().AsSelf();
        }
    }
}
