using _Project.Develop.EntryPoints;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.DI
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