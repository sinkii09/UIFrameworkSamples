using AstralChorus.Content;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;

namespace AstralChorus.Bootstrap
{
    /// <summary>
    /// Root scope — subclasses the framework scope, the same way ColorStackSort and MemoryGame do.
    /// <para>
    /// Not modelled on <c>AircraftLifetimeScope</c>: that one derives from VContainer's plain
    /// <c>LifetimeScope</c> and owns its wiring through a separate bootstrap, so copying its shape
    /// while overriding <c>Configure</c> would drop every framework registration — including
    /// <c>IAssetLoader</c>, without which <see cref="ContentPackLoader"/> cannot even be constructed.
    /// </para>
    /// <para>
    /// No <c>SetScopeContainer</c> / <c>ResetScopeContainer</c> here. There are no views yet, and the
    /// hook for it would be <c>OnAwake()</c> — never "after base.Awake()", because the base
    /// <c>Awake</c> destroys the duplicate and returns before a container exists.
    /// </para>
    /// </summary>
    public sealed class AstralChorusLifetimeScope : UIFrameworkLifetimeScope
    {
        [Tooltip("Which content packs load. Leave empty to run with the content system switched off.")]
        [SerializeField] private ContentBuildProfile _contentProfile;

        protected override void Configure(IContainerBuilder builder)
        {
            // Must run first: registers IUILoader/IAssetLoader, IUINavigator, IUIViewFactory,
            // persistence and the lifecycle signals.
            base.Configure(builder);

            if (_contentProfile == null)
            {
                // Not silent: an unassigned Inspector field and a deliberately disabled content
                // system look identical from the outside otherwise. VContainer resolves lazily, so
                // without this line the first symptom would be a resolve failure on some screen far
                // from the cause.
                Debug.LogWarning("[AstralChorus] No ContentBuildProfile assigned — content system " +
                                 "disabled. Anything resolving IContentRegistry will throw.", this);
                return;
            }

            builder.RegisterInstance(_contentProfile);

            // One instance behind two faces: the loader needs the concrete type to write into,
            // everything else only ever sees the read-only interface.
            builder.Register<ContentRegistry>(Lifetime.Singleton)
                   .AsSelf()
                   .As<IContentRegistry>();

            builder.Register<ContentAssetLoader>(Lifetime.Singleton).As<IContentAssetLoader>();

            builder.Register<ContentPackLoader>(Lifetime.Singleton).AsSelf();
        }
    }
}
