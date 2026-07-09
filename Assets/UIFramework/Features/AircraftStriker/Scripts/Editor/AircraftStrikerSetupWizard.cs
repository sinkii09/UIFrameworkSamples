#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AircraftStriker.Editor
{
    // Run via menu: AircraftStriker > Setup Wizard > Run Full Setup
    // Creates all ScriptableObjects, gameplay prefabs, and view prefabs.
    // Safe to re-run: existing assets are overwritten.
    // CAUTION: view/row prefabs are Addressable (see AddressableAssetsData/AssetGroups/UI_Prefab.asset).
    // SavePrefab deletes-then-recreates at ViewsRoot, which can change the asset GUID — re-running
    // after the Addressables migration can silently break the group's m_GUID bindings. Verify the
    // Addressables Groups window after any re-run.
    public static class AircraftStrikerSetupWizard
    {
        const string SOBullets   = "Assets/UIFramework/Features/AircraftStriker/ScriptableObjects/Bullets";
        const string SOPatterns  = "Assets/UIFramework/Features/AircraftStriker/ScriptableObjects/Patterns";
        const string SOEnemies   = "Assets/UIFramework/Features/AircraftStriker/ScriptableObjects/Enemies";
        const string SOWaves     = "Assets/UIFramework/Features/AircraftStriker/ScriptableObjects/Waves";
        const string SOShop      = "Assets/UIFramework/Features/AircraftStriker/ScriptableObjects/Shop";
        const string PrefPlayer  = "Assets/UIFramework/Features/AircraftStriker/Prefabs/Player";
        const string PrefEnemies = "Assets/UIFramework/Features/AircraftStriker/Prefabs/Enemies";
        const string PrefBullets = "Assets/UIFramework/Features/AircraftStriker/Prefabs/Projectiles";
        const string PrefPickups = "Assets/UIFramework/Features/AircraftStriker/Prefabs/Pickups";
        const string PrefVFX     = "Assets/UIFramework/Features/AircraftStriker/Prefabs/VFX";
        const string PrefSystems = "Assets/UIFramework/Features/AircraftStriker/Prefabs/Systems";
        const string ViewsRoot   = "Assets/UIFramework/Features/AircraftStriker/AssetBundles/AircraftStriker";

        // ── ENTRY POINT ────────────────────────────────────────────────────────────

        [MenuItem("AircraftStriker/Setup Wizard/Run Full Setup")]
        public static void RunSetup()
        {
            CreateFolders();
            EnsureTag("PlayerHitbox");

            var bullets  = CreateBulletConfigs();
            var patterns = CreateBulletPatternConfigs(bullets);
            var enemies  = CreateEnemyConfigs(patterns);
            var waves    = CreateWaveConfigs(enemies, patterns);
            CreateWaveDatabase(waves);
            var items    = CreateShopItems();
            CreateShopCatalog(items);

            var bulletPrefabs = CreateBulletPrefabs(bullets);
            var (basic, speed, heavy, boss) = CreateEnemyPrefabs();
            var (health, shield, score)     = CreatePickupPrefabs();
            var (smallVFX, largeVFX)        = CreateVFXPrefabs();
            CreateSystemPrefabs(bulletPrefabs, basic, speed, heavy, boss,
                                health, shield, score, smallVFX, largeVFX);

            var (shopRow, skinRow) = CreateRowPrefabs();
            CreateViewPrefabs(shopRow, skinRow);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AircraftSetup] Done. Assets in:\n" +
                      "  SOs   → Assets/UIFramework/Features/AircraftStriker/ScriptableObjects/\n" +
                      "  Prefs → Assets/UIFramework/Features/AircraftStriker/Prefabs/\n" +
                      "  Views → Assets/UIFramework/Features/AircraftStriker/AssetBundles/AircraftStriker/\n" +
                      "Next steps:\n" +
                      "  1. AircraftStriker > Setup Wizard > Rebuild Bullet VFX  (adds particle layers)\n" +
                      "  2. Open AircraftGame scene, assign Inspector refs on AircraftLifetimeScope\n" +
                      "  3. Assign Bullet-Additive.mat to particle system renderers for glow\n" +
                      "  4. Add art sprites to player/enemy prefabs");
        }

        // ── FOLDERS ────────────────────────────────────────────────────────────────

        static void CreateFolders()
        {
            foreach (var path in new[]
            {
                SOBullets, SOPatterns, SOEnemies, SOWaves, SOShop,
                PrefPlayer, PrefEnemies, PrefBullets, PrefPickups, PrefVFX, PrefSystems,
                ViewsRoot
            })
                EnsureFolder(path);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts   = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        static void EnsureTag(string tag)
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            var tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
            tagsProp.arraySize++;
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
            // Force Unity to reload the tag registry so GO.tag assignment works in this same run.
            AssetDatabase.ImportAsset("ProjectSettings/TagManager.asset", ImportAssetOptions.ForceUpdate);
            Debug.Log($"[AircraftSetup] Added tag: {tag}");
        }

        // ── BULLET CONFIGS ─────────────────────────────────────────────────────────
        // Returns array indexed by BulletType enum int value [0..5].

        static BulletConfig[] CreateBulletConfigs()
        {
            // (type, fileName, baseSpeed, maxSpeed, accel, angVel, lifetime, damage)
            var defs = new (BulletType type, string name, float spd, float max, float acc, float av, float life, int dmg)[]
            {
                (BulletType.PlayerBasic, "PlayerBasic",  12f, 12f, 0f,   0f,  5f,  1),
                (BulletType.EnemyRed,   "EnemyRed",      5f, 12f, 2f,   0f,  8f,  1),
                (BulletType.EnemyBlue,  "EnemyBlue",     3f,  3f, 0f,  25f, 12f,  1),
                (BulletType.EnemyOrb,   "EnemyOrb",      2f,  2f, 0f,   0f, 15f,  1),
                (BulletType.EnemyLaser, "EnemyLaser",   18f, 18f, 0f,   0f,  4f,  2),
                (BulletType.BossGold,   "BossGold",      4f,  9f, 0.8f, 0f, 10f,  2),
            };

            var configs = new BulletConfig[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                var so = MakeSO<BulletConfig>($"{SOBullets}/{d.name}.asset");
                so.BulletType      = d.type;
                so.BaseSpeed       = d.spd;
                so.MaxSpeed        = d.max;
                so.Acceleration    = d.acc;
                so.AngularVelocity = d.av;
                so.Lifetime        = d.life;
                so.Damage          = d.dmg;
                EditorUtility.SetDirty(so);
                configs[i] = so;
            }
            return configs;
        }

        // ── BULLET PATTERN CONFIGS ─────────────────────────────────────────────────

        static Dictionary<string, BulletPatternConfig> CreateBulletPatternConfigs(BulletConfig[] b)
        {
            // b[0]=PlayerBasic [1]=EnemyRed [2]=EnemyBlue [3]=EnemyOrb [4]=EnemyLaser [5]=BossGold
            var result = new Dictionary<string, BulletPatternConfig>();

            void Pat(string name, BulletPatternType type, BulletConfig bullet,
                     int count, float spread, float interval,
                     bool aim = false, int bursts = 1, float delay = 0.15f, float spiral = 15f)
            {
                var so               = MakeSO<BulletPatternConfig>($"{SOPatterns}/{name}.asset");
                so.PatternType       = type;
                so.BulletConfig      = bullet;
                so.Count             = count;
                so.SpreadAngle       = spread;
                so.PatternInterval   = interval;
                so.AimAtPlayer       = aim;
                so.BurstCount        = bursts;
                so.BurstDelay        = delay;
                so.SpiralStepDegrees = spiral;
                EditorUtility.SetDirty(so);
                result[name] = so;
            }

            Pat("Ring8",         BulletPatternType.Ring,       b[1], 8,  360f, 2.0f);
            Pat("AimedFan5",     BulletPatternType.AimedFan,   b[2], 5,  60f,  2.5f, aim: true);
            Pat("SpiralCW4",     BulletPatternType.SpiralCW,   b[1], 4,  360f, 1.5f, spiral: 30f);
            Pat("Wall7",         BulletPatternType.Wall,        b[3], 7,  180f, 3.0f);
            Pat("BurstFan3",     BulletPatternType.BurstFan,    b[2], 3,  40f,  2.0f, aim: true, bursts: 3, delay: 0.1f);
            Pat("DualSpiral6",   BulletPatternType.DualSpiral,  b[1], 6,  360f, 1.5f, spiral: 20f);
            Pat("BossRing16",    BulletPatternType.Ring,        b[5], 16, 360f, 1.5f);
            Pat("BossSpiralCW8", BulletPatternType.SpiralCW,   b[5], 8,  360f, 1.2f, spiral: 20f);
            Pat("BossDual8",     BulletPatternType.DualSpiral,  b[5], 8,  360f, 1.0f, spiral: 15f);

            return result;
        }

        // ── ENEMY CONFIGS ──────────────────────────────────────────────────────────
        // Returns [0]=Basic [1]=Speed [2]=Heavy [3]=Boss

        static EnemyConfig[] CreateEnemyConfigs(Dictionary<string, BulletPatternConfig> patterns)
        {
            var defs = new (EnemyType type, string name, int hp, float spd, int score, float drop, string pat)[]
            {
                (EnemyType.Basic, "BasicEnemy",  3,   2f,   10,  0.15f, "Ring8"),
                (EnemyType.Speed, "SpeedEnemy",  2,   4.5f, 20,  0.10f, "AimedFan5"),
                (EnemyType.Heavy, "HeavyEnemy",  10,  1f,   35,  0.30f, "Wall7"),
                (EnemyType.Boss,  "BossEnemy",   200, 1f,   1000, 0f,   ""),
            };

            var configs = new EnemyConfig[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                var so           = MakeSO<EnemyConfig>($"{SOEnemies}/{d.name}.asset");
                so.Type          = d.type;
                so.MaxHealth     = d.hp;
                so.MoveSpeed     = d.spd;
                so.ScoreValue    = d.score;
                so.PickupDropChance = d.drop;
                so.FirePattern   = string.IsNullOrEmpty(d.pat) ? null : patterns.GetValueOrDefault(d.pat);
                EditorUtility.SetDirty(so);
                configs[i] = so;
            }
            return configs;
        }

        // ── WAVE CONFIGS ───────────────────────────────────────────────────────────

        static WaveConfig[] CreateWaveConfigs(EnemyConfig[] enemies, Dictionary<string, BulletPatternConfig> patterns)
        {
            var basic = enemies[0];
            var speed = enemies[1];
            var heavy = enemies[2];
            var boss  = enemies[3];
            var p1 = patterns.GetValueOrDefault("BossRing16");
            var p2 = patterns.GetValueOrDefault("BossSpiralCW8");
            var p3 = patterns.GetValueOrDefault("BossDual8");

            var waves = new WaveConfig[10];

            WaveConfig Wave(string name, float delay, SpawnEntry[] entries,
                            bool isBoss = false, EnemyConfig bossConfig = null,
                            BulletPatternConfig[] ph1 = null,
                            BulletPatternConfig[] ph2 = null,
                            BulletPatternConfig[] ph3 = null)
            {
                var so               = MakeSO<WaveConfig>($"{SOWaves}/{name}.asset");
                so.PreWaveDelay      = delay;
                so.IsBossWave        = isBoss;
                so.BossConfig        = bossConfig;
                so.Enemies           = entries;
                so.BossPhase1Patterns = ph1;
                so.BossPhase2Patterns = ph2;
                so.BossPhase3Patterns = ph3;
                EditorUtility.SetDirty(so);
                return so;
            }

            SpawnEntry E(EnemyConfig cfg, int count, float interval,
                         FormationPatternType formation, float hx, float hy)
                => new SpawnEntry { EnemyConfig = cfg, Count = count, SpawnInterval = interval,
                                    Formation = formation, HoldPositionViewport = new Vector2(hx, hy) };

            waves[0] = Wave("Wave01", 1.0f, new[] { E(basic, 6,  1.0f, FormationPatternType.LineH,    0.5f, 0.80f) });
            waves[1] = Wave("Wave02", 1.5f, new[] { E(basic, 8,  0.8f, FormationPatternType.LineH,    0.5f, 0.82f) });
            waves[2] = Wave("Wave03", 1.5f, new[] { E(basic, 5,  1.0f, FormationPatternType.LineH,    0.5f, 0.80f),
                                                     E(speed, 3,  1.2f, FormationPatternType.Arrow,    0.5f, 0.68f) });
            waves[3] = Wave("Wave04", 2.0f, new[] { E(speed, 4,  1.2f, FormationPatternType.VShape,   0.5f, 0.82f),
                                                     E(heavy, 2,  2.0f, FormationPatternType.LineH,    0.5f, 0.65f) });
            waves[4] = Wave("Wave05", 2.0f, new SpawnEntry[0],
                            isBoss: true, bossConfig: boss,
                            ph1: new[] { p1 }, ph2: new[] { p1, p2 }, ph3: new[] { p2, p3 });
            waves[5] = Wave("Wave06", 1.5f, new[] { E(basic, 10, 0.6f, FormationPatternType.LineH,    0.5f, 0.82f),
                                                     E(heavy, 2,  2.0f, FormationPatternType.Diamond,  0.5f, 0.65f) });
            waves[6] = Wave("Wave07", 2.0f, new[] { E(speed, 6,  1.0f, FormationPatternType.Arrow,    0.5f, 0.82f),
                                                     E(heavy, 3,  2.0f, FormationPatternType.LineV,    0.2f, 0.70f) });
            waves[7] = Wave("Wave08", 2.0f, new[] { E(speed, 4,  1.0f, FormationPatternType.VShape,   0.3f, 0.82f),
                                                     E(heavy, 4,  1.5f, FormationPatternType.Diamond,  0.7f, 0.75f) });
            waves[8] = Wave("Wave09", 2.5f, new[] { E(heavy, 6,  1.5f, FormationPatternType.LineH,    0.5f, 0.82f),
                                                     E(speed, 6,  1.0f, FormationPatternType.Arrow,    0.5f, 0.65f),
                                                     E(basic, 4,  0.8f, FormationPatternType.LineH,    0.5f, 0.52f) });
            waves[9] = Wave("Wave10", 2.5f, new SpawnEntry[0],
                            isBoss: true, bossConfig: boss,
                            ph1: new[] { p1, p2 }, ph2: new[] { p1, p2, p3 }, ph3: new[] { p2, p3, p3 });

            return waves;
        }

        static void CreateWaveDatabase(WaveConfig[] waves)
        {
            var so            = MakeSO<WaveDatabase>($"{SOWaves}/WaveDatabase.asset");
            so.Waves          = waves;
            so.LoopAfterFinal = true;
            EditorUtility.SetDirty(so);
        }

        // ── SHOP ───────────────────────────────────────────────────────────────────

        static ShopItemConfig[] CreateShopItems()
        {
            var defs = new (string id, string display, string desc, int cost, ShopItemType type)[]
            {
                ("weapon_lv2", "Weapon Lv2",  "Doubles shot spread.",      100, ShopItemType.StartingWeaponLevel),
                ("weapon_lv3", "Weapon Lv3",  "Triple shot with laser.",   250, ShopItemType.StartingWeaponLevel),
                ("bonus_hp",   "Extra Life",  "Start with +1 max life.",   150, ShopItemType.BonusLives),
                ("skin_2",     "Steel Wing",  "Sleek silver hull.",        200, ShopItemType.SkinUnlock),
                ("skin_3",     "Crimson Ace", "Red-hot flame livery.",     350, ShopItemType.SkinUnlock),
            };

            var items = new ShopItemConfig[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                var so         = MakeSO<ShopItemConfig>($"{SOShop}/{d.id}.asset");
                so.Id          = d.id;
                so.DisplayName = d.display;
                so.Description = d.desc;
                so.CoinCost    = d.cost;
                so.ItemType    = d.type;
                EditorUtility.SetDirty(so);
                items[i] = so;
            }
            return items;
        }

        static void CreateShopCatalog(ShopItemConfig[] items)
        {
            var so   = MakeSO<ShopCatalog>($"{SOShop}/ShopCatalog.asset");
            so.Items = items;
            EditorUtility.SetDirty(so);
        }

        // ── BULLET PREFABS ─────────────────────────────────────────────────────────
        // Index must match BulletType enum int order [0..5].

        static BulletController[] CreateBulletPrefabs(BulletConfig[] configs)
        {
            var names  = new[] { "BulletPlayerBasic", "BulletEnemyRed", "BulletEnemyBlue",
                                  "BulletEnemyOrb",    "BulletEnemyLaser", "BulletBossGold" };
            var colors = new[]
            {
                Color.cyan, Color.red, new Color(0.3f,0.4f,1f),
                new Color(0.8f,0.3f,1f), Color.white, new Color(1f,0.8f,0f)
            };
            var sizes = new[] { 0.10f, 0.12f, 0.14f, 0.18f, 0.05f, 0.16f };

            var prefabs = new BulletController[6];
            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject(names[i]);
                var rb       = go.AddComponent<Rigidbody2D>();
                rb.bodyType  = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                var sr       = go.AddComponent<SpriteRenderer>();
                sr.color     = colors[i];
                go.transform.localScale = Vector3.one * sizes[i];
                var col      = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius    = 0.5f;
                var bc       = go.AddComponent<BulletController>();

                var cso      = new SerializedObject(bc);
                cso.FindProperty("_config").objectReferenceValue = configs[i];
                cso.ApplyModifiedProperties();

                var saved  = SavePrefab(go, PrefBullets, names[i]);
                prefabs[i] = saved.GetComponent<BulletController>();
                Object.DestroyImmediate(go);
            }
            return prefabs;
        }

        // ── ENEMY PREFABS ──────────────────────────────────────────────────────────

        static (EnemyController basic, EnemyController speed, EnemyController heavy, BossController boss) CreateEnemyPrefabs()
        {
            EnemyController MakeEnemy(string name, Color color, Vector3 scale)
            {
                var go    = new GameObject(name);
                var rb    = go.AddComponent<Rigidbody2D>();
                rb.bodyType  = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                var sr    = go.AddComponent<SpriteRenderer>();
                sr.color  = color;
                go.transform.localScale = scale;
                go.AddComponent<PolygonCollider2D>();
                go.AddComponent<EnemyController>();
                var saved = SavePrefab(go, PrefEnemies, name);
                Object.DestroyImmediate(go);
                return saved.GetComponent<EnemyController>();
            }

            var basic = MakeEnemy("EnemyBasic", new Color(0.6f, 0.8f, 1f), new Vector3(0.6f, 0.6f, 1f));
            var speed = MakeEnemy("EnemySpeed", new Color(0.3f, 1f,   0.4f), new Vector3(0.4f, 0.7f, 1f));
            var heavy = MakeEnemy("EnemyHeavy", new Color(1f,   0.5f, 0.2f), new Vector3(0.9f, 0.9f, 1f));

            var bossGo = new GameObject("Boss");
            var bossRb = bossGo.AddComponent<Rigidbody2D>();
            bossRb.bodyType  = RigidbodyType2D.Kinematic;
            bossRb.gravityScale = 0f;
            var bossSr = bossGo.AddComponent<SpriteRenderer>();
            bossSr.color = new Color(0.7f, 0.2f, 1f);
            bossGo.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            bossGo.AddComponent<PolygonCollider2D>();
            bossGo.AddComponent<BossController>();
            var bossSaved = SavePrefab(bossGo, PrefEnemies, "Boss");
            Object.DestroyImmediate(bossGo);

            return (basic, speed, heavy, bossSaved.GetComponent<BossController>());
        }

        // ── PICKUP PREFABS ─────────────────────────────────────────────────────────

        static (PickupController health, PickupController shield, PickupController score) CreatePickupPrefabs()
        {
            PickupController Make(string name, Color color)
            {
                var go    = new GameObject(name);
                var sr    = go.AddComponent<SpriteRenderer>();
                sr.color  = color;
                go.transform.localScale = Vector3.one * 0.3f;
                var col   = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius    = 0.5f;
                go.AddComponent<PickupController>();
                var saved = SavePrefab(go, PrefPickups, name);
                Object.DestroyImmediate(go);
                return saved.GetComponent<PickupController>();
            }

            return (Make("PickupHealth",     Color.red),
                    Make("PickupShield",     Color.cyan),
                    Make("PickupScoreBoost", Color.yellow));
        }

        // ── VFX PREFABS ────────────────────────────────────────────────────────────

        static (ParticleSystem small, ParticleSystem large) CreateVFXPrefabs()
        {
            ParticleSystem Make(string name, float sizeScale)
            {
                var go   = new GameObject(name);
                var ps   = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime  = 0.5f;
                main.startSpeed     = 3f * sizeScale;
                main.startSize      = 0.15f * sizeScale;
                main.maxParticles   = 20;
                main.stopAction     = ParticleSystemStopAction.Disable;
                main.loop           = false;
                var em  = ps.emission;
                em.rateOverTime = 0;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });
                var saved = SavePrefab(go, PrefVFX, name);
                Object.DestroyImmediate(go);
                return saved.GetComponent<ParticleSystem>();
            }

            return (Make("VFXSmallExplosion", 1f), Make("VFXLargeExplosion", 2.5f));
        }

        // ── SYSTEM PREFABS ─────────────────────────────────────────────────────────

        static void CreateSystemPrefabs(
            BulletController[] bulletPrefabs,
            EnemyController basic, EnemyController speed,
            EnemyController heavy, BossController boss,
            PickupController health, PickupController shield, PickupController scoreBoost,
            ParticleSystem smallVFX, ParticleSystem largeVFX)
        {
            // AircraftPoolManager — wire all prefab refs
            {
                var go  = new GameObject("AircraftPoolManager");
                var mgr = go.AddComponent<AircraftPoolManager>();
                var so  = new SerializedObject(mgr);

                var bulletArr = so.FindProperty("_bulletPrefabs");
                bulletArr.arraySize = 6;
                for (int i = 0; i < 6; i++)
                    bulletArr.GetArrayElementAtIndex(i).objectReferenceValue = bulletPrefabs[i];

                var capArr = so.FindProperty("_bulletCapacities");
                capArr.arraySize = 6;
                int[] caps = { 30, 80, 80, 50, 20, 60 };
                for (int i = 0; i < 6; i++)
                    capArr.GetArrayElementAtIndex(i).intValue = caps[i];

                so.FindProperty("_basicEnemyPrefab").objectReferenceValue        = basic;
                so.FindProperty("_speedEnemyPrefab").objectReferenceValue        = speed;
                so.FindProperty("_heavyEnemyPrefab").objectReferenceValue        = heavy;
                so.FindProperty("_bossPrefab").objectReferenceValue              = boss;
                so.FindProperty("_healthPickupPrefab").objectReferenceValue      = health;
                so.FindProperty("_shieldPickupPrefab").objectReferenceValue      = shield;
                so.FindProperty("_scoreBoostPickupPrefab").objectReferenceValue  = scoreBoost;
                so.FindProperty("_smallExplosionPrefab").objectReferenceValue    = smallVFX;
                so.FindProperty("_largeExplosionPrefab").objectReferenceValue    = largeVFX;
                so.ApplyModifiedProperties();

                SavePrefab(go, PrefSystems, "AircraftPoolManager");
                Object.DestroyImmediate(go);
            }

            // WaveManager
            {
                var go = new GameObject("WaveManager");
                go.AddComponent<WaveManager>();
                SavePrefab(go, PrefSystems, "WaveManager");
                Object.DestroyImmediate(go);
            }

            // BackgroundScrollController
            {
                var go  = new GameObject("BackgroundScroll");
                var bsc = go.AddComponent<BackgroundScrollController>();
                // TileA and TileB must be assigned manually (need actual sprite GOs)
                SavePrefab(go, PrefSystems, "BackgroundScroll");
                Object.DestroyImmediate(go);
            }

            // AircraftSoundManager — 1 music AudioSource + 3 sfx pool
            {
                var go      = new GameObject("AircraftSoundManager");
                var musicSrc = go.AddComponent<AudioSource>();
                musicSrc.loop   = true;
                musicSrc.volume = 0.6f;
                var sfx1 = go.AddComponent<AudioSource>();
                var sfx2 = go.AddComponent<AudioSource>();
                var sfx3 = go.AddComponent<AudioSource>();
                var mgr  = go.AddComponent<AircraftSoundManager>();
                var so   = new SerializedObject(mgr);
                so.FindProperty("_musicSource").objectReferenceValue = musicSrc;
                var poolProp = so.FindProperty("_sfxPool");
                poolProp.arraySize = 3;
                poolProp.GetArrayElementAtIndex(0).objectReferenceValue = sfx1;
                poolProp.GetArrayElementAtIndex(1).objectReferenceValue = sfx2;
                poolProp.GetArrayElementAtIndex(2).objectReferenceValue = sfx3;
                so.ApplyModifiedProperties();
                SavePrefab(go, PrefSystems, "AircraftSoundManager");
                Object.DestroyImmediate(go);
            }

            // PlayerShip — graze trigger on root + inner hitbox child
            {
                var go  = new GameObject("PlayerShip");
                var rb  = go.AddComponent<Rigidbody2D>();
                rb.bodyType  = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                var sr  = go.AddComponent<SpriteRenderer>();
                sr.color = new Color(0.3f, 0.8f, 1f);
                go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
                var graze     = go.AddComponent<CircleCollider2D>();
                graze.isTrigger = true;
                graze.radius    = 0.8f;
                go.AddComponent<PlayerController>();

                // Inner kill hitbox child — tag "PlayerHitbox"
                var hitbox = new GameObject("PlayerHitbox");
                // Tag may not be in Unity's registry yet if it was just added above.
                // ImportAsset in EnsureTag usually forces it, but guard defensively.
                try { hitbox.tag = "PlayerHitbox"; }
                catch (Exception) { Debug.LogWarning("[AircraftSetup] 'PlayerHitbox' tag not active yet. Assign tag manually on the PlayerHitbox child GO after re-opening the prefab."); }
                hitbox.transform.SetParent(go.transform, false);
                var hCol = hitbox.AddComponent<CircleCollider2D>();
                hCol.isTrigger = true;
                hCol.radius    = 0.15f;

                SavePrefab(go, PrefPlayer, "PlayerShip");
                Object.DestroyImmediate(go);
            }

            // AircraftInputHandler — full-screen transparent UI panel
            {
                var go   = new GameObject("AircraftInputHandler");
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var img  = go.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0.01f); // near-invisible but raycast-able
                go.AddComponent<AircraftInputHandler>();
                SavePrefab(go, PrefSystems, "AircraftInputHandler");
                Object.DestroyImmediate(go);
            }
        }

        // ── ROW PREFABS ────────────────────────────────────────────────────────────

        static (ShopItemRow shopRow, SkinItemRow skinRow) CreateRowPrefabs()
        {
            // ShopItemRow
            ShopItemRow shopRowPrefab;
            {
                var go   = UIRoot("ShopItemRow");
                var comp = go.AddComponent<ShopItemRow>();
                var bg   = go.AddComponent<Image>();
                bg.color = new Color(0.15f, 0.15f, 0.15f);

                var nameL  = MakeTMP(go, "NameLabel",      "Item Name",    new Vector2(0f, 0.6f), new Vector2(0.7f, 1f));
                var descL  = MakeTMP(go, "DescLabel",      "Description",  new Vector2(0f, 0.2f), new Vector2(0.7f, 0.6f));
                var costL  = MakeTMP(go, "CostLabel",      "0 coins",      new Vector2(0f, 0f),   new Vector2(0.5f, 0.2f));
                var buyBtn = MakeBtn(go, "BuyButton",       "Buy",          new Vector2(0.7f, 0f), new Vector2(1f, 1f));
                var badge  = MakeEmpty(go, "UnlockedBadge");

                var so = new SerializedObject(comp);
                so.FindProperty("_nameLabel").objectReferenceValue    = nameL;
                so.FindProperty("_descLabel").objectReferenceValue    = descL;
                so.FindProperty("_costLabel").objectReferenceValue    = costL;
                so.FindProperty("_buyButton").objectReferenceValue    = buyBtn;
                so.FindProperty("_unlockedBadge").objectReferenceValue = badge;
                so.ApplyModifiedProperties();

                var saved     = SavePrefab(go, ViewsRoot, "ShopItemRow");
                shopRowPrefab = saved.GetComponent<ShopItemRow>();
                Object.DestroyImmediate(go);
            }

            // SkinItemRow
            SkinItemRow skinRowPrefab;
            {
                var go   = UIRoot("SkinItemRow");
                var comp = go.AddComponent<SkinItemRow>();
                var bg   = go.AddComponent<Image>();
                bg.color = new Color(0.15f, 0.15f, 0.15f);

                var nameL    = MakeTMP(go,  "NameLabel",        "Skin Name", new Vector2(0f,   0.7f), new Vector2(0.7f, 1f));
                var preview  = MakeImg(go,  "PreviewImage",     new Vector2(0f,   0f),   new Vector2(0.3f, 0.7f));
                var selBtn   = MakeBtn(go,  "SelectButton",     "Select",    new Vector2(0.7f, 0.2f), new Vector2(1f,   0.8f));
                var selInd   = MakeEmpty(go, "SelectedIndicator");
                var locked   = MakeEmpty(go, "LockedOverlay");

                var so = new SerializedObject(comp);
                so.FindProperty("_nameLabel").objectReferenceValue         = nameL;
                so.FindProperty("_previewImage").objectReferenceValue      = preview;
                so.FindProperty("_selectButton").objectReferenceValue      = selBtn;
                so.FindProperty("_selectedIndicator").objectReferenceValue = selInd;
                so.FindProperty("_lockedOverlay").objectReferenceValue     = locked;
                so.ApplyModifiedProperties();

                var saved    = SavePrefab(go, ViewsRoot, "SkinItemRow");
                skinRowPrefab = saved.GetComponent<SkinItemRow>();
                Object.DestroyImmediate(go);
            }

            return (shopRowPrefab, skinRowPrefab);
        }

        // ── VIEW PREFABS ───────────────────────────────────────────────────────────

        static void CreateViewPrefabs(ShopItemRow shopRow, SkinItemRow skinRow)
        {
            // ── AircraftMainMenuView ────────────────────────────────────────────
            {
                var root = UIRoot("AircraftMainMenuView");
                root.AddComponent<AircraftMainMenuView>();
                var title   = UIChild(root, "Title");
                var best    = MakeTMP(root, "BestScoreLabel", "Best: 0",  new Vector2(0.1f, 0.75f), new Vector2(0.9f, 0.85f));
                var playBtn = MakeBtn(root, "PlayButton",      "PLAY",     new Vector2(0.2f, 0.45f), new Vector2(0.8f, 0.58f));
                var shopBtn = MakeBtn(root, "ShopButton",      "SHOP",     new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.41f));

                var so = new SerializedObject(root.GetComponent<AircraftMainMenuView>());
                so.FindProperty("_bestScoreLabel").objectReferenceValue = best;
                so.FindProperty("_playButton").objectReferenceValue     = playBtn;
                so.FindProperty("_shopButton").objectReferenceValue     = shopBtn;
                so.FindProperty("_titleTransform").objectReferenceValue = title.GetComponent<RectTransform>();
                so.ApplyModifiedProperties();

                SavePrefab(root, ViewsRoot, "AircraftMainMenuView");
                Object.DestroyImmediate(root);
            }

            // ── AircraftHUDView ─────────────────────────────────────────────────
            {
                var root = UIRoot("AircraftHUDView");
                root.AddComponent<AircraftHUDView>();
                var scoreL    = MakeTMP(root, "ScoreLabel",      "0",       new Vector2(0.02f, 0.92f), new Vector2(0.5f,  0.99f));
                var comboL    = MakeTMP(root, "ComboLabel",       "",        new Vector2(0.5f,  0.92f), new Vector2(0.98f, 0.99f));
                var waveL     = MakeTMP(root, "WaveLabel",        "Wave 1",  new Vector2(0.35f, 0.85f), new Vector2(0.65f, 0.92f));
                var grazeFill = MakeImg(root, "GrazeMeterFill",   new Vector2(0.02f, 0.05f), new Vector2(0.3f,  0.09f));
                var grazeL    = MakeTMP(root, "GrazeCountLabel",  "Graze: 0",new Vector2(0.02f, 0.09f), new Vector2(0.3f,  0.14f));
                var pauseBtn  = MakeBtn(root, "PauseButton",       "II",      new Vector2(0.85f, 0.88f), new Vector2(0.98f, 0.99f));
                var bossWarn  = MakeEmpty(root, "BossWarning");

                // Health icons (3)
                var hIcons = new Image[3];
                for (int i = 0; i < 3; i++)
                    hIcons[i] = MakeImg(root, $"HealthIcon{i+1}",
                                        new Vector2(0.02f + i * 0.065f, 0.14f),
                                        new Vector2(0.07f + i * 0.065f, 0.20f));
                hIcons[0].color = Color.red;
                hIcons[1].color = Color.red;
                hIcons[2].color = Color.red;

                // Shield icons (2)
                var sIcons = new Image[2];
                for (int i = 0; i < 2; i++)
                    sIcons[i] = MakeImg(root, $"ShieldIcon{i+1}",
                                        new Vector2(0.02f + i * 0.065f, 0.20f),
                                        new Vector2(0.07f + i * 0.065f, 0.26f));
                sIcons[0].color = Color.cyan;
                sIcons[1].color = Color.cyan;

                var so = new SerializedObject(root.GetComponent<AircraftHUDView>());
                so.FindProperty("_scoreLabel").objectReferenceValue     = scoreL;
                so.FindProperty("_comboLabel").objectReferenceValue     = comboL;
                so.FindProperty("_waveLabel").objectReferenceValue      = waveL;
                so.FindProperty("_grazeMeterFill").objectReferenceValue  = grazeFill;
                so.FindProperty("_grazeCountLabel").objectReferenceValue = grazeL;
                so.FindProperty("_pauseButton").objectReferenceValue    = pauseBtn;
                so.FindProperty("_bossWarning").objectReferenceValue    = bossWarn;

                var hp = so.FindProperty("_healthIcons");
                hp.arraySize = 3;
                for (int i = 0; i < 3; i++) hp.GetArrayElementAtIndex(i).objectReferenceValue = hIcons[i];

                var sp = so.FindProperty("_shieldIcons");
                sp.arraySize = 2;
                for (int i = 0; i < 2; i++) sp.GetArrayElementAtIndex(i).objectReferenceValue = sIcons[i];

                so.ApplyModifiedProperties();

                SavePrefab(root, ViewsRoot, "AircraftHUDView");
                Object.DestroyImmediate(root);
            }

            // ── AircraftPauseView ───────────────────────────────────────────────
            {
                var root    = UIRoot("AircraftPauseView");
                root.AddComponent<AircraftPauseView>();
                var resume  = MakeBtn(root, "ResumeButton",  "RESUME",  new Vector2(0.2f, 0.52f), new Vector2(0.8f, 0.62f));
                var restart = MakeBtn(root, "RestartButton", "RESTART", new Vector2(0.2f, 0.39f), new Vector2(0.8f, 0.49f));
                var menu    = MakeBtn(root, "MenuButton",    "MENU",    new Vector2(0.2f, 0.26f), new Vector2(0.8f, 0.36f));

                var so = new SerializedObject(root.GetComponent<AircraftPauseView>());
                so.FindProperty("_resumeButton").objectReferenceValue  = resume;
                so.FindProperty("_restartButton").objectReferenceValue = restart;
                so.FindProperty("_menuButton").objectReferenceValue    = menu;
                so.ApplyModifiedProperties();

                SavePrefab(root, ViewsRoot, "AircraftPauseView");
                Object.DestroyImmediate(root);
            }

            // ── AircraftGameOverView ────────────────────────────────────────────
            {
                var root  = UIRoot("AircraftGameOverView");
                root.AddComponent<AircraftGameOverView>();
                var panel = UIChild(root, "Panel");
                var panelImg = panel.AddComponent<Image>();
                panelImg.color = new Color(0.05f, 0.05f, 0.15f, 0.95f);
                SetAnchors(panel, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.95f));

                var title      = MakeTMP(panel, "TitleText",      "GAME OVER",  new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.99f));
                var finalScore = MakeTMP(panel, "FinalScoreLabel","0",           new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.88f));
                var bestScore  = MakeTMP(panel, "BestScoreLabel", "Best: 0",    new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.76f));
                var waves      = MakeTMP(panel, "WavesLabel",     "Wave 1",     new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.68f));
                var coins      = MakeTMP(panel, "CoinsLabel",     "+0 coins",   new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.60f));
                var graze      = MakeTMP(panel, "GrazeLabel",     "Graze: 0",   new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.52f));
                var maxCombo   = MakeTMP(panel, "MaxComboLabel",  "Max Combo: x1", new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.44f));
                var badge      = MakeEmpty(panel, "NewHighScoreBadge");
                var retry      = MakeBtn(panel,  "RetryButton",   "RETRY",      new Vector2(0.05f, 0.08f), new Vector2(0.48f, 0.20f));
                var menuBtn    = MakeBtn(panel,  "MenuButton",    "MENU",       new Vector2(0.52f, 0.08f), new Vector2(0.95f, 0.20f));

                var so = new SerializedObject(root.GetComponent<AircraftGameOverView>());
                so.FindProperty("_panel").objectReferenceValue           = panel.GetComponent<RectTransform>();
                so.FindProperty("_titleText").objectReferenceValue       = title;
                so.FindProperty("_finalScoreLabel").objectReferenceValue  = finalScore;
                so.FindProperty("_bestScoreLabel").objectReferenceValue   = bestScore;
                so.FindProperty("_wavesLabel").objectReferenceValue       = waves;
                so.FindProperty("_coinsLabel").objectReferenceValue       = coins;
                so.FindProperty("_grazeLabel").objectReferenceValue       = graze;
                so.FindProperty("_maxComboLabel").objectReferenceValue    = maxCombo;
                so.FindProperty("_newHighScoreBadge").objectReferenceValue = badge;
                so.FindProperty("_retryButton").objectReferenceValue      = retry;
                so.FindProperty("_menuButton").objectReferenceValue       = menuBtn;
                so.ApplyModifiedProperties();

                SavePrefab(root, ViewsRoot, "AircraftGameOverView");
                Object.DestroyImmediate(root);
            }

            // ── AircraftVictoryView ─────────────────────────────────────────────
            {
                var root  = UIRoot("AircraftVictoryView");
                root.AddComponent<AircraftVictoryView>();
                var panel = UIChild(root, "Panel");
                var panelImg = panel.AddComponent<Image>();
                panelImg.color = new Color(0.05f, 0.15f, 0.05f, 0.95f);
                SetAnchors(panel, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.95f));

                var title      = MakeTMP(panel, "TitleText",       "VICTORY!",   new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.99f));
                var finalScore = MakeTMP(panel, "FinalScoreLabel", "0",          new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.88f));
                var bestScore  = MakeTMP(panel, "BestScoreLabel",  "Best: 0",   new Vector2(0.05f, 0.64f), new Vector2(0.95f, 0.74f));
                var coins      = MakeTMP(panel, "CoinsLabel",      "+0 coins",   new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.64f));
                var badge      = MakeEmpty(panel, "NewHighScoreBadge");
                var playAgain  = MakeBtn(panel,  "PlayAgainButton","PLAY AGAIN", new Vector2(0.05f, 0.08f), new Vector2(0.48f, 0.20f));
                var menuBtn    = MakeBtn(panel,  "MenuButton",     "MENU",       new Vector2(0.52f, 0.08f), new Vector2(0.95f, 0.20f));

                var so = new SerializedObject(root.GetComponent<AircraftVictoryView>());
                so.FindProperty("_panel").objectReferenceValue            = panel.GetComponent<RectTransform>();
                so.FindProperty("_titleText").objectReferenceValue        = title;
                so.FindProperty("_finalScoreLabel").objectReferenceValue  = finalScore;
                so.FindProperty("_bestScoreLabel").objectReferenceValue   = bestScore;
                so.FindProperty("_coinsLabel").objectReferenceValue       = coins;
                so.FindProperty("_newHighScoreBadge").objectReferenceValue = badge;
                so.FindProperty("_playAgainButton").objectReferenceValue  = playAgain;
                so.FindProperty("_menuButton").objectReferenceValue       = menuBtn;
                so.ApplyModifiedProperties();

                SavePrefab(root, ViewsRoot, "AircraftVictoryView");
                Object.DestroyImmediate(root);
            }

            // ── ShopView ────────────────────────────────────────────────────────
            {
                var root      = UIRoot("ShopView");
                root.AddComponent<ShopView>();
                var coinL     = MakeTMP(root, "CoinBalanceLabel",  "0 coins",  new Vector2(0.02f, 0.92f), new Vector2(0.98f, 0.99f));
                var container = UIChild(root, "ItemListContainer");
                SetAnchors(container, new Vector2(0.02f, 0.12f), new Vector2(0.98f, 0.88f));
                var skinsBtn  = MakeBtn(root, "SkinsButton", "SKINS", new Vector2(0.55f, 0.02f), new Vector2(0.85f, 0.10f));
                var backBtn   = MakeBtn(root, "BackButton",  "BACK",  new Vector2(0.02f, 0.02f), new Vector2(0.32f, 0.10f));

                var so = new SerializedObject(root.GetComponent<ShopView>());
                so.FindProperty("_coinBalanceLabel").objectReferenceValue = coinL;
                so.FindProperty("_itemListContainer").objectReferenceValue = container.transform;
                so.FindProperty("_itemRowPrefab").objectReferenceValue    = shopRow;
                so.FindProperty("_skinsButton").objectReferenceValue      = skinsBtn;
                so.FindProperty("_backButton").objectReferenceValue       = backBtn;
                so.ApplyModifiedProperties();

                SavePrefab(root, ViewsRoot, "ShopView");
                Object.DestroyImmediate(root);
            }

            // ── SkinSelectionView ───────────────────────────────────────────────
            {
                var root      = UIRoot("SkinSelectionView");
                root.AddComponent<SkinSelectionView>();
                var container = UIChild(root, "SkinListContainer");
                SetAnchors(container, new Vector2(0.02f, 0.12f), new Vector2(0.98f, 0.99f));
                var backBtn   = MakeBtn(root, "BackButton", "BACK", new Vector2(0.02f, 0.02f), new Vector2(0.32f, 0.10f));

                var so = new SerializedObject(root.GetComponent<SkinSelectionView>());
                so.FindProperty("_skinListContainer").objectReferenceValue = container.transform;
                so.FindProperty("_skinRowPrefab").objectReferenceValue     = skinRow;
                so.FindProperty("_backButton").objectReferenceValue        = backBtn;
                so.ApplyModifiedProperties();

                SavePrefab(root, ViewsRoot, "SkinSelectionView");
                Object.DestroyImmediate(root);
            }
        }

        // ── HELPERS ────────────────────────────────────────────────────────────────

        // Creates a ScriptableObject asset at path; overwrites existing.
        static T MakeSO<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                // Wipe and reuse the existing asset so references stay valid
                var fields = typeof(T).GetFields(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var f in fields)
                    f.SetValue(existing, f.GetValue(ScriptableObject.CreateInstance<T>()));
                return existing;
            }
            var so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        // Saves go as prefab, overwrites if exists; returns the saved prefab asset.
        static GameObject SavePrefab(GameObject go, string folder, string name)
        {
            var path = $"{folder}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                AssetDatabase.DeleteAsset(path);
            return PrefabUtility.SaveAsPrefabAsset(go, path);
        }

        // Full-screen RectTransform root (no view component — caller adds it).
        static GameObject UIRoot(string name)
        {
            var go   = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<CanvasGroup>();
            return go;
        }

        // Empty child with RectTransform, stretch to parent by default.
        static GameObject UIChild(GameObject parent, string name)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        // Sets anchor-min/max on a GO's RectTransform.
        static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
        {
            var rect    = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // TMP_Text child positioned via anchor fractions.
        static TextMeshProUGUI MakeTMP(GameObject parent, string name, string text,
                                       Vector2 anchorMin, Vector2 anchorMax)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var tmp  = go.AddComponent<TextMeshProUGUI>();
            tmp.text       = text;
            tmp.fontSize   = 22;
            tmp.alignment  = TextAlignmentOptions.Center;
            tmp.color      = Color.white;
            return tmp;
        }

        // Button child with background Image + TMP label.
        static Button MakeBtn(GameObject parent, string name, string label,
                              Vector2 anchorMin, Vector2 anchorMax)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img  = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.35f, 0.7f);
            var btn  = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var labelGo   = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var lRect     = labelGo.AddComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.offsetMin = Vector2.zero;
            lRect.offsetMax = Vector2.zero;
            var tmp       = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
            return btn;
        }

        // Image child.
        static Image MakeImg(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.AddComponent<Image>();
        }

        // Inactive empty child — used for badges, warnings, indicators.
        static GameObject MakeEmpty(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            go.SetActive(false);
            return go;
        }
    }
}
#endif
