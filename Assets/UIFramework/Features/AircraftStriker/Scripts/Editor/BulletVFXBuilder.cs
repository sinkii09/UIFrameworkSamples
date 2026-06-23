#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AircraftStriker.Editor
{
    // Menu: AircraftStriker → Setup Wizard → Rebuild Bullet VFX
    // Replaces SpriteRenderer on each bullet prefab with a multi-layer
    // particle system. Safe to re-run; [BulletVFX] child is regenerated.
    // Also called automatically from AircraftStrikerSetupWizard.RunSetup().
    public static class BulletVFXBuilder
    {
        const string k_Dir     = "Assets/UIFramework/Features/AircraftStriker/Prefabs/Projectiles";
        const string k_MatDir  = "Assets/UIFramework/Features/AircraftStriker/Materials/VFX";
        const string k_MatPath = k_MatDir + "/Bullet-Additive.mat";

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = path[..path.LastIndexOf('/')];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path[(path.LastIndexOf('/') + 1)..]);
        }

        const string k_TexPath = k_MatDir + "/particle-circle.png";

        // Generates a 64x64 gaussian soft-circle texture as a project asset.
        // Avoids dependency on Unity built-in resource paths that changed in 2022+.
        static Texture2D GetOrCreateParticleTex()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(k_TexPath);
            if (existing != null) return existing;

            EnsureFolder(k_MatDir);
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float dist2 = dx * dx + dy * dy;
                    // Gaussian falloff — sharp centre, smooth fade to edge
                    byte a = (byte)(Mathf.Clamp01(Mathf.Exp(-4f * dist2)) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var absPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath), k_TexPath);
            System.IO.File.WriteAllBytes(absPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(k_TexPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(k_TexPath);
        }

        static Material GetOrCreateBulletMat()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(k_MatPath);
            if (mat == null)
            {
                EnsureFolder(k_MatDir);
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                mat = new Material(shader) { name = "Bullet-Additive" };
                // Additive blending: src*srcAlpha + dst*1 → layers brighten without occluding
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend",   2f);
                mat.SetInt("_SrcBlend",  (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend",  (int)BlendMode.One);
                mat.SetInt("_ZWrite",    0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetOverrideTag("RenderType", "Transparent");
                AssetDatabase.CreateAsset(mat, k_MatPath);
            }
            var tex = GetOrCreateParticleTex();
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                else                             mat.SetTexture("_MainTex", tex);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        [MenuItem("AircraftStriker/Setup Wizard/Rebuild Bullet VFX")]
        public static void RebuildAll()
        {
            Rebuild("BulletPlayerBasic", AddPlayerBasicVFX);
            Rebuild("BulletEnemyRed",    AddEnemyRedVFX);
            Rebuild("BulletEnemyBlue",   AddEnemyBlueVFX);
            Rebuild("BulletEnemyOrb",    AddEnemyOrbVFX);
            Rebuild("BulletEnemyLaser",  AddEnemyLaserVFX);
            Rebuild("BulletBossGold",    AddBossGoldVFX);
            AssetDatabase.SaveAssets();
            Debug.Log("[BulletVFX] Rebuilt 6 bullet prefabs.");
        }

        // ── PREFAB MODIFIER ────────────────────────────────────────────────────────

        static void Rebuild(string prefabName, System.Action<GameObject> factory)
        {
            var path = $"{k_Dir}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"[BulletVFX] {prefabName}.prefab not found — run Full Setup first.");
                return;
            }
            var go = PrefabUtility.LoadPrefabContents(path);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) Object.DestroyImmediate(sr);
            var old = go.transform.Find("[BulletVFX]");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            factory(go);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            PrefabUtility.UnloadPrefabContents(go);
        }

        // ── SHARED HELPERS ─────────────────────────────────────────────────────────

        static (GameObject vfxGo, BulletVFX vfx) MakeRoot(GameObject root)
        {
            var go  = new GameObject("[BulletVFX]");
            go.transform.SetParent(root.transform, false);
            var inv = 1f / Mathf.Max(0.001f, root.transform.localScale.x);
            go.transform.localScale = Vector3.one * inv;
            var vfx = go.AddComponent<BulletVFX>();
            var bc  = root.GetComponent<BulletController>();
            if (bc != null)
            {
                var so = new SerializedObject(bc);
                so.FindProperty("_vfx").objectReferenceValue = vfx;
                so.ApplyModifiedProperties();
            }
            return (go, vfx);
        }

        // Pool-safe particle layer. ±15% lifetime/size variation for organic feel.
        static ParticleSystem Layer(GameObject parent, string name,
                                     bool local, float life, float size,
                                     Color color, float rate, int maxP)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var ps   = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop            = true;
            main.playOnAwake     = false;
            main.stopAction      = ParticleSystemStopAction.None;
            main.simulationSpace = local
                ? ParticleSystemSimulationSpace.Local
                : ParticleSystemSimulationSpace.World;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(life * 0.85f, life * 1.15f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0f);
            main.startSize       = new ParticleSystem.MinMaxCurve(size * 0.85f, size * 1.15f);
            main.startColor      = new ParticleSystem.MinMaxGradient(color);
            main.maxParticles    = maxP;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0f);
            main.duration        = 1f;
            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
            var sh = ps.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.01f;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetOrCreateBulletMat();
            return ps;
        }

        static void Col3(ParticleSystem ps,
                          Color c0, float a0, float t1, Color c1, float a1, Color c2, float a2)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c0, 0f),
                              new GradientColorKey(c1, t1),
                              new GradientColorKey(c2, 1f) },
                      new[] { new GradientAlphaKey(a0, 0f),
                              new GradientAlphaKey(a1, t1),
                              new GradientAlphaKey(a2, 1f) });
            var m = ps.colorOverLifetime; m.enabled = true;
            m.color = new ParticleSystem.MinMaxGradient(g);
        }

        // 4-stop gradient — enables white-hot core → color → dark fade pattern
        static void Col4(ParticleSystem ps,
                          Color c0, float a0, float t1, Color c1, float a1,
                          float t2, Color c2, float a2, Color c3, float a3)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c0, 0f), new GradientColorKey(c1, t1),
                              new GradientColorKey(c2, t2), new GradientColorKey(c3, 1f) },
                      new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(a1, t1),
                              new GradientAlphaKey(a2, t2), new GradientAlphaKey(a3, 1f) });
            var m = ps.colorOverLifetime; m.enabled = true;
            m.color = new ParticleSystem.MinMaxGradient(g);
        }

        static void SzFade(ParticleSystem ps)
        {
            var m = ps.sizeOverLifetime; m.enabled = true;
            m.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        }

        static void SzPulse(ParticleSystem ps)
        {
            var m = ps.sizeOverLifetime; m.enabled = true;
            m.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.8f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.8f)));
        }

        // Glow halo: persistent size, white-hot birth flash → bullet color → transparent
        static ParticleSystem GlowLayer(GameObject parent, float size, Color color, int order)
        {
            var ps = Layer(parent, "Glow", true, 0.09f, size, color, 50f, 15);
            Col3(ps, Color.white, 0.65f, 0.3f, color, 0.42f, color, 0f);
            SzFade(ps);
            Ord(ps, order);
            return ps;
        }

        // Perlin noise turbulence for organic drift. strength < 0.02 = subtle, > 0.05 = chaotic.
        static void AddNoise(ParticleSystem ps, float strength, float frequency)
        {
            var n = ps.noise;
            n.enabled     = true;
            n.strengthX   = new ParticleSystem.MinMaxCurve(strength);
            n.strengthY   = new ParticleSystem.MinMaxCurve(strength);
            n.frequency   = frequency;
            n.scrollSpeed = new ParticleSystem.MinMaxCurve(0.5f);
            n.damping     = true;
        }

        // Decelerates particles that have startSpeed > 0 (sparks, debris).
        // endFraction = fraction of original speed by end of lifetime.
        static void AddSpeedDamping(ParticleSystem ps, float endFraction)
        {
            var vol = ps.velocityOverLifetime;
            vol.enabled       = true;
            vol.speedModifier = new ParticleSystem.MinMaxCurve(
                1f, AnimationCurve.EaseInOut(0f, 1f, 1f, endFraction));
            vol.space = ParticleSystemSimulationSpace.Local;
        }

        // Spinning rotation per particle — makes needle/shard shapes feel energetic.
        static void AddRotation(ParticleSystem ps, float minDegPerSec, float maxDegPerSec)
        {
            var rol = ps.rotationOverLifetime;
            rol.enabled      = true;
            rol.separateAxes = false;
            rol.z = new ParticleSystem.MinMaxCurve(
                minDegPerSec * Mathf.Deg2Rad, maxDegPerSec * Mathf.Deg2Rad);
        }

        static void Wire(BulletVFX vfx, ParticleSystem[] layers)
        {
            var so = new SerializedObject(vfx);
            var p  = so.FindProperty("_layers");
            p.arraySize = layers.Length;
            for (int i = 0; i < layers.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];
            so.ApplyModifiedProperties();
        }

        static void Ord(ParticleSystem ps, int order) =>
            ps.GetComponent<ParticleSystemRenderer>().sortingOrder = order;

        // ── PER-TYPE FACTORIES ─────────────────────────────────────────────────────

        static void AddPlayerBasicVFX(GameObject root)
        {
            var (vfxGo, vfx) = MakeRoot(root);

            var glow  = GlowLayer(vfxGo, 0.30f, new Color(0f, 0.8f, 1f), 8);

            // Core: white-hot flash → bright cyan → cool blue
            var core  = Layer(vfxGo, "Core", true, 0.06f, 0.10f, Color.white, 90f, 24);
            Col4(core, Color.white, 1f, 0.2f, new Color(0.8f, 1f, 1f), 1f,
                        0.65f, new Color(0.27f, 0.87f, 1f), 0.7f, new Color(0f, 0.4f, 0.9f), 0f);
            SzFade(core); AddNoise(core, 0.015f, 2.5f); Ord(core, 10);

            // Trail: bright teal start → deep blue fade
            var trail = Layer(vfxGo, "Trail", false, 0.20f, 0.09f, new Color(0f, 1f, 1f), 100f, 120);
            Col3(trail, new Color(0.7f, 1f, 1f), 0.92f, 0.5f,
                        new Color(0.27f, 0.93f, 0.93f), 0.55f, new Color(0f, 0.3f, 0.7f), 0f);
            SzFade(trail); Ord(trail, 9);

            Wire(vfx, new[] { glow, core, trail });
        }

        static void AddEnemyRedVFX(GameObject root)
        {
            var (vfxGo, vfx) = MakeRoot(root);

            var glow  = GlowLayer(vfxGo, 0.32f, new Color(1f, 0.2f, 0f), 8);

            // Core: white-hot → yellow → orange-red (combustion feel)
            var core  = Layer(vfxGo, "Core", true, 0.08f, 0.13f, new Color(1f, 0.53f, 0f), 80f, 24);
            Col4(core, Color.white, 1f, 0.15f, new Color(1f, 0.9f, 0.3f), 1f,
                        0.55f, new Color(1f, 0.27f, 0f), 0.78f, new Color(0.6f, 0f, 0f), 0f);
            SzFade(core); AddNoise(core, 0.012f, 2.0f); Ord(core, 10);

            var trail = Layer(vfxGo, "Trail", false, 0.22f, 0.11f, new Color(1f, 0.2f, 0f), 80f, 100);
            Col3(trail, new Color(1f, 0.7f, 0.3f), 0.92f, 0.4f,
                        new Color(1f, 0.27f, 0f), 0.7f, new Color(0.2f, 0f, 0f), 0f);
            { var s = trail.sizeOverLifetime; s.enabled = true;
              s.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f)); }
            Ord(trail, 9);

            Wire(vfx, new[] { glow, core, trail });
        }

        static void AddEnemyBlueVFX(GameObject root)
        {
            var (vfxGo, vfx) = MakeRoot(root);

            var glow  = GlowLayer(vfxGo, 0.26f, new Color(0f, 0.4f, 1f), 8);

            // Core: white → ice-blue → electric blue (crisp, cold energy)
            var core  = Layer(vfxGo, "Core", true, 0.05f, 0.09f, Color.white, 100f, 22);
            Col4(core, Color.white, 1f, 0.25f, new Color(0.7f, 0.85f, 1f), 1f,
                        0.65f, new Color(0.27f, 0.53f, 1f), 0.78f, new Color(0f, 0f, 0.8f), 0f);
            SzFade(core); AddNoise(core, 0.012f, 2.2f); Ord(core, 10);

            var trail = Layer(vfxGo, "Trail", false, 0.16f, 0.07f, new Color(0.13f, 0.4f, 1f), 110f, 120);
            Col3(trail, new Color(0.8f, 0.9f, 1f), 0.92f, 0.45f,
                        new Color(0.53f, 0.67f, 1f), 0.6f, new Color(0f, 0f, 0.4f), 0f);
            SzFade(trail); Ord(trail, 9);

            Wire(vfx, new[] { glow, core, trail });
        }

        static void AddEnemyOrbVFX(GameObject root)
        {
            var (vfxGo, vfx) = MakeRoot(root);

            var glow  = GlowLayer(vfxGo, 0.38f, new Color(0.5f, 0f, 1f), 7);

            // Core: white flash → violet → deep purple, pulsing and drifting
            var core  = Layer(vfxGo, "Core", true, 0.10f, 0.18f, new Color(0.6f, 0f, 1f), 50f, 22);
            { var sh = core.shape; sh.radius = 0.04f; }
            Col4(core, Color.white, 0.9f, 0.2f, new Color(0.9f, 0.6f, 1f), 1f,
                        0.6f, new Color(0.6f, 0.1f, 1f), 0.85f, new Color(0.2f, 0f, 0.5f), 0f);
            SzPulse(core); AddNoise(core, 0.04f, 1.5f); Ord(core, 10);

            // Orbit: motes circling at fixed radius
            var orbit = Layer(vfxGo, "Orbit", true, 0.40f, 0.04f, new Color(1f, 0.53f, 1f), 25f, 18);
            { var sh = orbit.shape; sh.radius = 0.12f; sh.radiusThickness = 0f; }
            { var v = orbit.velocityOverLifetime;
              v.enabled  = true; v.space = ParticleSystemSimulationSpace.Local;
              v.orbitalZ = new ParticleSystem.MinMaxCurve(360f); }
            Col3(orbit, new Color(1f, 0.8f, 1f), 1f, 0.5f,
                        new Color(0.8f, 0.4f, 1f), 0.7f, new Color(0.6f, 0f, 1f), 0f);
            SzFade(orbit); Ord(orbit, 9);

            var trail = Layer(vfxGo, "Trail", false, 0.28f, 0.10f, new Color(0.47f, 0f, 0.8f), 40f, 70);
            Col3(trail, new Color(0.85f, 0.6f, 1f), 0.9f, 0.4f,
                        new Color(0.67f, 0.33f, 1f), 0.5f, new Color(0.2f, 0f, 0.4f), 0f);
            SzFade(trail); Ord(trail, 8);

            Wire(vfx, new[] { glow, core, orbit, trail });
        }

        static void AddEnemyLaserVFX(GameObject root)
        {
            var (vfxGo, vfx) = MakeRoot(root);

            var glow  = GlowLayer(vfxGo, 0.22f, new Color(1f, 0.1f, 0f), 8);

            // Core: tall needle shards, varied size, spin randomly — gives laser a shredding look
            var core  = Layer(vfxGo, "Core", true, 0.05f, 0.05f, Color.white, 70f, 14);
            { var m = core.main;
              m.startSize3D   = true;
              m.startSizeX    = new ParticleSystem.MinMaxCurve(0.04f, 0.06f);
              m.startSizeY    = new ParticleSystem.MinMaxCurve(0.25f, 0.35f);
              m.startSizeZ    = new ParticleSystem.MinMaxCurve(0.05f);
              m.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI); }
            Col3(core, Color.white, 1f, 0.3f, new Color(1f, 0.27f, 0.27f), 0.9f, new Color(1f, 0f, 0f), 0f);
            AddRotation(core, -240f, 240f);
            SzFade(core); Ord(core, 10);

            // Dense red-orange ember trail
            var trail = Layer(vfxGo, "Trail", false, 0.12f, 0.05f, new Color(1f, 0.13f, 0f), 150f, 120);
            Col3(trail, new Color(1f, 0.7f, 0.5f), 0.92f, 0.35f,
                        new Color(1f, 0.27f, 0f), 0.6f, new Color(0.5f, 0f, 0f), 0f);
            SzFade(trail); Ord(trail, 9);

            Wire(vfx, new[] { glow, core, trail });
        }

        static void AddBossGoldVFX(GameObject root)
        {
            var (vfxGo, vfx) = MakeRoot(root);

            var glow   = GlowLayer(vfxGo, 0.45f, new Color(1f, 0.8f, 0f), 7);

            // Core: searing white → gold → ember, pulsing with noise drift
            var core   = Layer(vfxGo, "Core", true, 0.12f, 0.22f, new Color(1f, 0.84f, 0f), 60f, 22);
            { var sh = core.shape; sh.radius = 0.06f; }
            Col4(core, Color.white, 1f, 0.2f, new Color(1f, 0.97f, 0.7f), 1f,
                        0.5f, new Color(1f, 0.87f, 0f), 0.94f, new Color(1f, 0.3f, 0f), 0f);
            SzPulse(core); AddNoise(core, 0.05f, 1.2f); Ord(core, 10);

            // Sparks: radiate outward, decelerate to a stop naturally
            var sparks = Layer(vfxGo, "Sparks", true, 0.30f, 0.035f, new Color(1f, 0.87f, 0.27f), 60f, 50);
            { var m = sparks.main; m.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.1f); }
            { var sh = sparks.shape; sh.radius = 0.08f; sh.radiusThickness = 0f; }
            Col4(sparks, Color.white, 1f, 0.2f, new Color(1f, 0.93f, 0.53f), 1f,
                          0.65f, new Color(1f, 0.27f, 0f), 0.8f, Color.clear, 0f);
            AddSpeedDamping(sparks, 0.05f);
            SzFade(sparks); Ord(sparks, 9);

            // Trail: white flash → gold → orange ember glow
            var trail  = Layer(vfxGo, "Trail", false, 0.30f, 0.13f, new Color(1f, 0.67f, 0f), 80f, 90);
            Col4(trail, new Color(1f, 1f, 0.8f), 0.92f, 0.3f, new Color(1f, 0.8f, 0f), 0.8f,
                        0.7f, new Color(1f, 0.4f, 0f), 0.4f, new Color(0.3f, 0f, 0f), 0f);
            SzFade(trail); Ord(trail, 8);

            Wire(vfx, new[] { glow, core, sparks, trail });
        }
    }
}
#endif
