using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UIFrameworkSamples.Editor
{
    /// <summary>
    /// Creates sample prefabs for MainMenuView, SettingsView, and HUDView.
    /// Run via Tools > UIFramework > Create Sample Prefabs.
    /// After creation, assign UITransition ScriptableObjects in the Inspector if desired.
    /// </summary>
    public static class SamplePrefabCreator
    {
        private const string PrefabsPath = "Assets/UIFramework/Samples/Prefabs";

        [MenuItem("Tools/UIFramework/Create Sample Prefabs")]
        public static void CreateAll()
        {
            EnsureFolder("Assets/UIFramework/Samples", "Prefabs");
            CreateMainMenuPrefab();
            CreateSettingsPrefab();
            CreateHUDPrefab();
            AssetDatabase.Refresh();
            Debug.Log("[UIFramework Samples] Sample prefabs ready at " + PrefabsPath);
        }

        // ── Main Menu ─────────────────────────────────────────────────────────

        private static void CreateMainMenuPrefab()
        {
            const string path = PrefabsPath + "/MainMenuView.prefab";
            if (System.IO.File.Exists(path)) { Debug.Log("MainMenuView.prefab already exists — skipping."); return; }

            var root = CreateViewRoot("MainMenuView");
            root.AddComponent<MainMenuView>();

            var bg = CreatePanel(root, "Background", new Color(0.08f, 0.08f, 0.15f, 0.95f), Vector2.zero, Vector2.one);

            var title = CreateText(bg, "TitleText", "Main Menu", 48, FontStyles.Bold);
            SetAnchors(title, new Vector2(0.2f, 0.72f), new Vector2(0.8f, 0.90f));

            var playBtn = CreateButton(bg, "PlayButton", "PLAY",
                new Vector2(0.30f, 0.54f), new Vector2(0.70f, 0.66f));
            var settBtn = CreateButton(bg, "SettingsButton", "SETTINGS",
                new Vector2(0.30f, 0.38f), new Vector2(0.70f, 0.50f));
            var quitBtn = CreateButton(bg, "QuitButton", "QUIT",
                new Vector2(0.30f, 0.22f), new Vector2(0.70f, 0.34f));

            WireFields(root.GetComponent<MainMenuView>(),
                ("_titleText",      title.GetComponent<TMP_Text>()),
                ("_playButton",     playBtn.GetComponent<Button>()),
                ("_settingsButton", settBtn.GetComponent<Button>()),
                ("_quitButton",     quitBtn.GetComponent<Button>()));

            SaveAndDestroy(root, path);
        }

        // ── Settings ──────────────────────────────────────────────────────────

        private static void CreateSettingsPrefab()
        {
            const string path = PrefabsPath + "/SettingsView.prefab";
            if (System.IO.File.Exists(path)) { Debug.Log("SettingsView.prefab already exists — skipping."); return; }

            var root = CreateViewRoot("SettingsView");
            root.AddComponent<SettingsView>();

            // Centred card — popup style
            var bg = CreatePanel(root, "Background", new Color(0.05f, 0.05f, 0.12f, 0.96f),
                new Vector2(0.25f, 0.20f), new Vector2(0.75f, 0.80f));

            var header = CreateText(bg, "HeaderText", "Settings", 36, FontStyles.Bold);
            SetAnchors(header, new Vector2(0.10f, 0.82f), new Vector2(0.90f, 0.95f));

            // SFX row
            var sfxLabel = CreateText(bg, "SFXLabel", "Sound FX", 24, FontStyles.Normal);
            SetAnchors(sfxLabel, new Vector2(0.10f, 0.64f), new Vector2(0.72f, 0.74f));
            var sfxToggle = CreateToggle(bg, "SFXToggle",
                new Vector2(0.76f, 0.64f), new Vector2(0.90f, 0.74f));

            // Music row
            var musicLabel = CreateText(bg, "MusicLabel", "Music", 24, FontStyles.Normal);
            SetAnchors(musicLabel, new Vector2(0.10f, 0.50f), new Vector2(0.72f, 0.60f));
            var musicToggle = CreateToggle(bg, "MusicToggle",
                new Vector2(0.76f, 0.50f), new Vector2(0.90f, 0.60f));

            var saveBtn  = CreateButton(bg, "SaveButton",  "Save",
                new Vector2(0.10f, 0.10f), new Vector2(0.44f, 0.22f));
            var closeBtn = CreateButton(bg, "CloseButton", "Close",
                new Vector2(0.56f, 0.10f), new Vector2(0.90f, 0.22f), new Color(0.5f, 0.15f, 0.15f));

            WireFields(root.GetComponent<SettingsView>(),
                ("_sfxToggle",   sfxToggle.GetComponent<Toggle>()),
                ("_musicToggle", musicToggle.GetComponent<Toggle>()),
                ("_saveButton",  saveBtn.GetComponent<Button>()),
                ("_closeButton", closeBtn.GetComponent<Button>()));

            SaveAndDestroy(root, path);
        }

        // ── HUD ───────────────────────────────────────────────────────────────

        private static void CreateHUDPrefab()
        {
            const string path = PrefabsPath + "/HUDView.prefab";
            if (System.IO.File.Exists(path)) { Debug.Log("HUDView.prefab already exists — skipping."); return; }

            var root = CreateViewRoot("HUDView");
            root.AddComponent<HUDView>();

            // Score — top-left
            var scoreText = CreateText(root, "ScoreText", "Score: 0", 28, FontStyles.Bold);
            SetAnchors(scoreText, new Vector2(0.02f, 0.91f), new Vector2(0.30f, 0.99f));
            scoreText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Left;

            // Health bar — bottom-left: dark background + green fill
            var healthBg = CreatePanel(root, "HealthBarBG", new Color(0.25f, 0.04f, 0.04f, 0.85f),
                new Vector2(0.02f, 0.02f), new Vector2(0.36f, 0.07f));
            var healthFill = CreatePanel(healthBg, "HealthBarFill", new Color(0.18f, 0.78f, 0.18f, 0.9f),
                Vector2.zero, Vector2.one);
            var fillImg = healthFill.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;

            // Test buttons — bottom-right
            var addBtn = CreateButton(root, "AddScoreButton", "+10 Score",
                new Vector2(0.63f, 0.02f), new Vector2(0.81f, 0.08f), new Color(0.15f, 0.45f, 0.15f));
            var dmgBtn = CreateButton(root, "DamageButton", "-10% HP",
                new Vector2(0.82f, 0.02f), new Vector2(0.99f, 0.08f), new Color(0.50f, 0.12f, 0.12f));

            WireFields(root.GetComponent<HUDView>(),
                ("_scoreText",      scoreText.GetComponent<TMP_Text>()),
                ("_healthBar",      fillImg),
                ("_addScoreButton", addBtn.GetComponent<Button>()),
                ("_damageButton",   dmgBtn.GetComponent<Button>()));

            SaveAndDestroy(root, path);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static GameObject CreateViewRoot(string name)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.AddComponent<CanvasGroup>();
            return go;
        }

        private static GameObject CreatePanel(GameObject parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static GameObject CreateText(GameObject parent, string name, string text,
            int size, FontStyles style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return go;
        }

        private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateButton(GameObject parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax,
            Color? color = null)
        {
            var go = CreatePanel(parent, name, color ?? new Color(0.18f, 0.38f, 0.72f), anchorMin, anchorMax);
            var img = go.GetComponent<Image>();
            var btn = go.AddComponent<Button>();

            btn.targetGraphic = img;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go;
        }

        private static GameObject CreateToggle(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var toggle = go.AddComponent<Toggle>();

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.25f, 0.25f, 0.25f);

            var checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkRect = checkGo.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.offsetMin = checkRect.offsetMax = Vector2.zero;
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = new Color(0.18f, 0.78f, 0.18f);

            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.isOn = true;

            return go;
        }

        // Sets [SerializeField] private fields via SerializedObject — avoids reflection hacks.
        private static void WireFields(Component component, params (string field, Object value)[] pairs)
        {
            var so = new SerializedObject(component);
            foreach (var (field, value) in pairs)
            {
                var prop = so.FindProperty(field);
                if (prop == null) { Debug.LogWarning($"[SamplePrefabCreator] Field '{field}' not found on {component.GetType().Name}"); continue; }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SaveAndDestroy(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void EnsureFolder(string parent, string child)
        {
            var full = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
