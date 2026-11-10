using _Project.Develop.EntryPoints;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.DI
{
    public class GameScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
