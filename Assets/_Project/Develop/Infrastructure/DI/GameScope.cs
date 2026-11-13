using _Project.Develop.Gameplay.Input;
using _Project.Develop.Infrastructure.EntryPoints;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Infrastructure.DI
{
    public class GameScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<InputController>().AsSelf();
            
            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
