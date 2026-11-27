using _Project.Develop.Infrastructure.EntryPoints;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Infrastructure.DI
{
    public class RootScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterMessagePipe();
            
            builder.RegisterEntryPoint<BootstrapEntryPoint>();
        }
    }
}