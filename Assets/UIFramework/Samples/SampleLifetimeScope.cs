using VContainer;
using VContainer.Unity;

namespace UIFrameworkSamples
{
    // Child scope of UIFrameworkLifetimeScope.
    // In the Inspector, set the Parent field to the UIRoot GameObject that has UIFrameworkLifetimeScope.
    // Add this component to any GameObject in your game scene.
    public class SampleLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SampleGameplayState>(Lifetime.Singleton);
            builder.RegisterEntryPoint<SampleGameBootstrap>();
        }
    }
}
