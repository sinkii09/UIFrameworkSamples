using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MemoryGame
{
    // Single root scope for the game. Extends UIFrameworkLifetimeScope so base.Configure()
    // wires all UIFramework services, then this class adds game-level registrations.
    // Place this component on the UIRoot prefab instead of UIFrameworkLifetimeScope.
    // SampleBootstrap registers game states and shows the first screen.
    public class GameLifetimeScope : UIFrameworkLifetimeScope
    {
        [SerializeField] private SoundManager _soundManager;
        [SerializeField] private SoundConfig _soundConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            // --- Audio ---
            if (_soundManager != null)
                builder.RegisterInstance(_soundManager).As<ISoundService>();
            else
                Debug.LogError("[GameLifetimeScope] SoundManager not assigned.", this);

            if (_soundConfig != null)
                builder.RegisterInstance(_soundConfig);
            else
                Debug.LogError("[GameLifetimeScope] SoundConfig not assigned.", this);

            // --- Game states & bootstrap (previously in SampleLifetimeScope) ---
            builder.Register<SampleGameplayState>(Lifetime.Singleton);
            builder.Register<MemoryGameState>(Lifetime.Singleton);
            builder.RegisterEntryPoint<SampleGameBootstrap>();
        }
    }
}
