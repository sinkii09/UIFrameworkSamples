using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ColorStackSort
{
    /// <summary>
    /// Root scope — subclasses the framework scope instead of parenting a child scope to it.
    /// <para>
    /// ColorStackSort registers no scene MonoBehaviours (the entire board lives inside the
    /// BoardView prefab), so the child-scope pattern's <c>SetScopeContainer</c> /
    /// <c>ResetScopeContainer</c> lifecycle would be overhead with nothing to show for it.
    /// Anything registered here is visible to the per-view child scopes UIViewFactory creates,
    /// which is what ViewModels need.
    /// </para>
    /// </summary>
    public sealed class ColorStackSortLifetimeScope : UIFrameworkLifetimeScope
    {
        [SerializeField] private ColorStackSortSettings _settings;

        protected override void Configure(IContainerBuilder builder)
        {
            // Must run first: registers IUINavigator, IUIViewFactory, IUIStateMachine and
            // GameLifecycleManager, and runs UIViewRegistry.AutoRegister.
            base.Configure(builder);

            if (_settings != null)
            {
                builder.RegisterInstance(_settings);
            }
            else
            {
                Debug.LogError("[ColorStackSortLifetimeScope] Settings not assigned — " +
                               "ColorStackSortGameplayState will fail to resolve.", this);
            }

            // Singleton, never Scoped. UIViewFactory gives every view its own child container, so a
            // scoped registration would hand BoardViewModel and the win ViewModel two different
            // services — progression would advance in one and never be read by the other.
            builder.Register<LevelProgressService>(Lifetime.Singleton);

            builder.Register<ColorStackSortGameplayState>(Lifetime.Singleton);
            builder.RegisterEntryPoint<ColorStackSortBootstrap>();
        }
    }
}
