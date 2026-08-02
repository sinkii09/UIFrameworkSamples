using Coffee.UIEffects;
using Coffee.UIExtensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ColorStackSort.Editor
{
    /// <summary>
    /// Builds the Phase 6 juice parts — sweep, particle burst, press punch — onto prefabs the other
    /// two builders assemble. Split out to keep both of those under the file-size limit, and because
    /// this is the only file that needs to know the UIEffect / UIParticle packages exist.
    /// </summary>
    internal static class ColorStackSortJuicePrefabParts
    {
        /// <summary>
        /// Ships inside the UIEffect package. Shiny with a null texture flashes the whole graphic
        /// instead of sweeping a band across it, so this must resolve — hence the explicit error.
        /// </summary>
        private const string TransitionTexturePath =
            "Packages/com.coffee.ui-effect/UIEffectPresets/Textures/Transition-Horizontal.png";

        private const float SweepDuration = 0.55f;

        /// <summary>
        /// Adds the completion sweep and the rejected-tap flash to a tube. The UIEffect must live on
        /// the same GameObject as the Graphic it filters, which is the tube root.
        /// </summary>
        internal static TubeFeedback AddTubeFeedback(RectTransform tubeRoot, Image body)
        {
            var effect = tubeRoot.gameObject.AddComponent<UIEffect>();
            effect.transitionFilter = TransitionFilter.Shiny;
            effect.transitionTexture = AssetDatabase.LoadAssetAtPath<Texture>(TransitionTexturePath);
            effect.transitionWidth = 0.25f;
            effect.transitionSoftness = 0.35f;
            effect.transitionColorGlow = true;

            // Zero, NOT the default. transitionRate is the sweep's own position; leaving it at its
            // default parks the band mid-tube and shows it permanently, which reads as a rendering
            // bug rather than an effect. The tweener drives this value from 0.
            effect.transitionRate = 0f;

            if (effect.transitionTexture == null)
                Debug.LogError($"[ColorStackSort] Transition texture missing at {TransitionTexturePath} — " +
                               "the tube sweep will flash instead of sweeping.");

            var tweener = tubeRoot.gameObject.AddComponent<UIEffectTweener>();
            tweener.direction = UIEffectTweener.Direction.Forward;
            tweener.duration = SweepDuration;
            tweener.wrapMode = UIEffectTweener.WrapMode.Once;

            // None: the sweep marks a tube being COMPLETED. Playing on enable would fire it on every
            // tube the moment the board is built.
            tweener.playOnEnable = UIEffectTweener.PlayOnEnable.None;

            var feedback = tubeRoot.gameObject.AddComponent<TubeFeedback>();
            UiPrefabFactory.SetReference(feedback, "_body", body);
            UiPrefabFactory.SetReference(feedback, "_sweep", tweener);

            return feedback;
        }

        /// <summary>
        /// Creates a burst emitter. One per board is enough — see <see cref="JuiceBurstEmitter"/>.
        /// </summary>
        internal static JuiceBurstEmitter CreateBurstEmitter(RectTransform parent, string name)
        {
            var rect = UiPrefabFactory.CreatePoint(name, parent, new Vector2(8f, 8f));
            var particles = rect.gameObject.AddComponent<ParticleSystem>();

            var main = particles.main;
            // World, so overlapping bursts stay where they were emitted when the emitter is moved to
            // the next tube. Local space would drag every live particle along with it.
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.startLifetime = 0.9f;
            main.startSpeed = 320f;
            main.startSize = 18f;
            main.gravityModifier = 1.6f;
            main.maxParticles = 400;

            // Emission is driven entirely by explicit Emit() calls.
            var emission = particles.emission;
            emission.rateOverTime = 0f;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 12f;

            var uiParticle = rect.gameObject.AddComponent<UIParticle>();

            // Absolute = "emitted from the world position". In Relative mode UIParticle offsets the
            // baked mesh by (emitter position - parent position) * (scale - 1), so moving the shared
            // emitter to the next tube would DRAG particles still alive from the previous burst.
            // That term is zero while scale is exactly 1, which makes Relative look fine right up
            // until someone tunes the scale below — so it is pinned here rather than left to luck.
            uiParticle.positionMode = UIParticle.PositionMode.Absolute;

            // UI units are far larger than world units; this is a starting point, not a tuned value.
            uiParticle.scale = 1f;
            uiParticle.RefreshParticles();

            var emitter = rect.gameObject.AddComponent<JuiceBurstEmitter>();
            UiPrefabFactory.SetReference(emitter, "_uiParticle", uiParticle);
            UiPrefabFactory.SetReference(emitter, "_particles", particles);

            return emitter;
        }

        /// <summary>Adds a press punch to a button whose transition is None.</summary>
        internal static void AddPressPunch(Selectable selectable)
        {
            if (selectable == null) return;

            var punch = selectable.gameObject.AddComponent<ButtonPressPunch>();
            UiPrefabFactory.SetReference(punch, "_selectable", selectable);
            UiPrefabFactory.SetReference(punch, "_target", selectable.transform as RectTransform);
        }
    }
}
