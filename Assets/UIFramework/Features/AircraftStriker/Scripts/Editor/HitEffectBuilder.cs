#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AircraftStriker.Editor
{
    // Menu: AircraftStriker → Setup Wizard → Build Hit Effect Prefab
    // Creates HitEffect.prefab in Prefabs/VFX/ with a pooling-safe burst particle system.
    // Re-run to regenerate. Assign the prefab to AircraftPoolManager._hitEffectPrefab.
    public static class HitEffectBuilder
    {
        const string k_PrefabPath = "Assets/UIFramework/Features/AircraftStriker/Prefabs/VFX/HitEffect.prefab";
        const string k_MatDir  = "Assets/UIFramework/Features/AircraftStriker/Materials/VFX";
        const string k_MatPath = k_MatDir + "/HitEffect-Unlit.mat";

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = path[..path.LastIndexOf('/')];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path[(path.LastIndexOf('/') + 1)..]);
        }

        static Material GetOrCreateHitMat()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(k_MatPath);
            if (mat != null) return mat;
            EnsureFolder(k_MatDir);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            mat = new Material(shader) { name = "HitEffect-Unlit" };
            // Alpha blending: burst dots fade out naturally via colorOverLifetime
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend",   0f);
            mat.SetInt("_SrcBlend",  (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",  (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",    0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            AssetDatabase.CreateAsset(mat, k_MatPath);
            return mat;
        }

        [MenuItem("AircraftStriker/Setup Wizard/Build Hit Effect Prefab")]
        public static void Build()
        {
            var root = new GameObject("HitEffect");
            var hitEffect = root.AddComponent<HitEffect>();

            var burstGo = new GameObject("Burst");
            burstGo.transform.SetParent(root.transform, false);
            var ps = burstGo.AddComponent<ParticleSystem>();

            ConfigureParticles(ps);

            // Wire HitEffect._ps via SerializedObject so private field is set properly.
            var so = new SerializedObject(hitEffect);
            so.FindProperty("_ps").objectReferenceValue = ps;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, k_PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[HitEffectBuilder] HitEffect.prefab created at " + k_PrefabPath +
                      ". Assign it to AircraftPoolManager._hitEffectPrefab in the Inspector.");
        }

        static void ConfigureParticles(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop            = false;
            main.playOnAwake     = false;
            main.stopAction      = ParticleSystemStopAction.None;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.duration        = 0.1f;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor      = new ParticleSystem.MinMaxGradient(Color.white);
            main.maxParticles    = 20;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0f);

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            var sh = ps.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.05f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = GetOrCreateHitMat();
            rend.sortingOrder = 15;
        }
    }
}
#endif
