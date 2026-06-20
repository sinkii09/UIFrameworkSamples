using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AircraftStriker
{
    // UIFramework services (IUINavigator, GameLifecycleManager, etc.) come from the parent scope.
    // This scope adds aircraft-specific registrations only.
    // SetScopeContainer / ResetScopeContainer lifecycle is owned by AircraftGameBootstrap (IInitializable +
    // IDisposable) — that fires after all scopes are fully built, avoiding the Awake() parent-not-ready race.
    public class AircraftLifetimeScope : LifetimeScope
    {
        [Header("Scene MonoBehaviours — must be assigned in Inspector")]
        [SerializeField] private AircraftPoolManager  _poolManager;
        [SerializeField] private AircraftInputHandler _inputHandler;
        [SerializeField] private WaveManager          _waveManager;
        [SerializeField] private PlayerController     _playerController;
        [SerializeField] private AircraftSoundManager _soundManager;

        [Header("ScriptableObjects")]
        [SerializeField] private WaveDatabase _waveDatabase;
        [SerializeField] private ShopCatalog  _shopCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            // no [Inject] — no callback needed
            if (_poolManager      != null) builder.RegisterInstance(_poolManager);
            else Debug.LogError("[AircraftLifetimeScope] PoolManager not assigned.",      this);

            // no [Inject] — no callback needed
            if (_inputHandler     != null) builder.RegisterInstance(_inputHandler);
            else Debug.LogError("[AircraftLifetimeScope] InputHandler not assigned.",     this);

            if (_waveManager      != null)
            {
                // Pin to local so the closure captures the value, not the field reference.
                var wm = _waveManager;
                builder.RegisterInstance(wm);
                // RegisterInstance skips [Inject] method injection — must call explicitly after build.
                // RegisterBuildCallback fires after all registrations are resolved, before IInitializable.Initialize().
                builder.RegisterBuildCallback(c => c.Inject(wm));
            }
            else Debug.LogError("[AircraftLifetimeScope] WaveManager not assigned.", this);

            // Prefab template — AircraftGameplayState instantiates it at runtime (OnEnterAsync) and
            // destroys it on exit. Registered here so VContainer can inject it as a spawn source.
            if (_playerController != null) builder.RegisterInstance(_playerController);
            else Debug.LogError("[AircraftLifetimeScope] PlayerController prefab not assigned.", this);

            // ScriptableObjects — no [Inject] — no callback needed
            if (_waveDatabase     != null) builder.RegisterInstance(_waveDatabase);
            else Debug.LogError("[AircraftLifetimeScope] WaveDatabase not assigned.",     this);

            // no [Inject] — no callback needed
            if (_shopCatalog      != null) builder.RegisterInstance(_shopCatalog);
            else Debug.LogError("[AircraftLifetimeScope] ShopCatalog not assigned.",      this);

            // no [Inject] — no callback needed
            if (_soundManager     != null) builder.RegisterInstance<IAircraftSoundService>(_soundManager);
            else Debug.LogError("[AircraftLifetimeScope] SoundManager not assigned.",     this);

            // Pure-C# singletons (no scene dependency)
            builder.Register<IProgressionService, PlayerPrefsProgressionService>(Lifetime.Singleton);
            builder.Register<AircraftHUDChannel>(Lifetime.Singleton);
            builder.Register<BulletPatternExecutor>(Lifetime.Singleton);
            builder.Register<CheckpointManager>(Lifetime.Singleton);
            builder.Register<GameplayController>(Lifetime.Singleton);
            builder.Register<ShopService>(Lifetime.Singleton);

            // UIFramework game states + entry-point bootstrap
            builder.Register<AircraftGameplayState>(Lifetime.Singleton);
            builder.Register<AircraftMainMenuState>(Lifetime.Singleton);
            builder.RegisterEntryPoint<AircraftGameBootstrap>();
        }
    }
}
