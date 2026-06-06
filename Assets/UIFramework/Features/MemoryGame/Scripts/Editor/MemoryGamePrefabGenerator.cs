using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryGame.Editor
{
    /// <summary>
    /// Generates Card, GameplayView, and WinView prefabs for the Memory Flip Card Game.
    /// Run via Tools > UIFramework > Create Memory Game Prefabs.
    /// After creation: assign 8 card face sprites to GameplayView._cardFaceSprites in the Inspector.
    /// </summary>
    public static class MemoryGamePrefabGenerator
    {
        private const string PrefabsPath  = "Assets/UIFramework/Features/MemoryGame/Prefabs";
        private const string ResourcePath = "Assets/Resources";
        private const string CardPath     = PrefabsPath + "/Card.prefab";
        private const string GameplayPath = ResourcePath + "/GameplayView.prefab";
        private const string WinPath      = ResourcePath + "/WinView.prefab";

        [MenuItem("Tools/UIFramework/Create Memory Game Prefabs")]
        public static void CreateAll()
        {
            EnsureFolder("Assets/UIFramework/Features/MemoryGame", "Prefabs");
            EnsureFolder("Assets", "Resources");

            // Card must be saved first; returned CardView is wired into GameplayView._cardPrefab
            // using the in-memory asset reference rather than a re-load — avoids import-pipeline races.
            var cardViewRef = CreateCardPrefab();
            CreateGameplayViewPrefab(cardViewRef);
            CreateWinViewPrefab();

            AssetDatabase.Refresh();
            Debug.Log("[MemoryGame] Prefabs ready. " +
                      "Assign 8 sprites to GameplayView._cardFaceSprites in the Inspector.");
        }

        // ── Card ──────────────────────────────────────────────────────────────

        private static CardView CreateCardPrefab()
        {
            if (Exists(CardPath)) return AssetDatabase.LoadAssetAtPath<GameObject>(CardPath)?.GetComponent<CardView>();

            var root = new GameObject("Card");
            StretchRect(root);

            // CardView carries [RequireComponent(typeof(Button))]; Button is auto-added.
            var cardView = root.AddComponent<CardView>();
            var btn      = root.GetComponent<Button>();

            var back  = CreateImage(root, "Back",  new Color(0.18f, 0.28f, 0.55f));
            var front = CreateImage(root, "Front", new Color(0.95f, 0.82f, 0.40f));
            front.gameObject.SetActive(false);

            btn.targetGraphic = back;

            WireFields(cardView,
                ("_button",     btn),
                ("_backImage",  back),
                ("_frontImage", front));

            var saved = PrefabUtility.SaveAsPrefabAsset(root, CardPath);
            Object.DestroyImmediate(root);
            return saved.GetComponent<CardView>();
        }

        // ── GameplayView ──────────────────────────────────────────────────────

        private static void CreateGameplayViewPrefab(CardView cardPrefabRef)
        {
            if (Exists(GameplayPath)) return;

            var root = CreateViewRoot("GameplayView");
            root.AddComponent<GameplayView>();

            CreateImage(root, "Background", new Color(0.08f, 0.10f, 0.18f, 0.98f));

            // Header — horizontal strip at top with evenly distributed TMP children
            var header = CreateChild(root, "Header",
                new Vector2(0f, 0.88f), new Vector2(1f, 1f));
            var hlg = header.AddComponent<HorizontalLayoutGroup>();
            hlg.childForceExpandWidth  = true;
            hlg.childControlWidth      = true;   // required: distributes width across children
            hlg.childForceExpandHeight = true;
            hlg.childControlHeight     = true;

            var movesText   = CreateTMP(header, "MovesText",   "Moves: 0",    22);
            var matchesText = CreateTMP(header, "MatchesText", "Matches: 0/8", 22);
            var timerText   = CreateTMP(header, "TimerText",   "0:00",         22);

            // Grid — fills remaining area, 4-column fixed layout
            var gridGo = CreateChild(root, "Grid",
                new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.86f));
            var glg = gridGo.AddComponent<GridLayoutGroup>();
            glg.cellSize        = new Vector2(100, 140);
            glg.spacing         = new Vector2(8, 8);
            glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;
            glg.childAlignment  = TextAnchor.MiddleCenter;

            WireFields(root.GetComponent<GameplayView>(),
                ("_gridParent",  gridGo.GetComponent<RectTransform>()),
                ("_movesText",   movesText),
                ("_matchesText", matchesText),
                ("_timerText",   timerText),
                ("_cardPrefab",  cardPrefabRef));

            SaveAndDestroy(root, GameplayPath);
        }

        // ── WinView ───────────────────────────────────────────────────────────

        private static void CreateWinViewPrefab()
        {
            if (Exists(WinPath)) return;

            var root = CreateViewRoot("WinView");
            root.AddComponent<WinView>();

            CreateImage(root, "Overlay", new Color(0f, 0f, 0f, 0.65f));

            var panel = CreateChild(root, "Panel",
                new Vector2(0.25f, 0.20f), new Vector2(0.75f, 0.82f));
            panel.AddComponent<Image>().color = new Color(0.10f, 0.12f, 0.22f, 0.97f);

            var title     = CreateTMP(panel, "Title",     "You Win!",   52, FontStyles.Bold);
            SetAnchors(title.gameObject,    new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.96f));
            var movesText = CreateTMP(panel, "MovesText", "Moves: 0",   28);
            SetAnchors(movesText.gameObject, new Vector2(0.10f, 0.60f), new Vector2(0.90f, 0.72f));
            var timeText  = CreateTMP(panel, "TimeText",  "Time: 0:00", 28);
            SetAnchors(timeText.gameObject,  new Vector2(0.10f, 0.46f), new Vector2(0.90f, 0.58f));

            var playBtn = CreateButton(panel, "PlayAgainButton", "Play Again",
                new Vector2(0.08f, 0.12f), new Vector2(0.47f, 0.26f));
            var menuBtn = CreateButton(panel, "MainMenuButton", "Main Menu",
                new Vector2(0.53f, 0.12f), new Vector2(0.92f, 0.26f),
                new Color(0.45f, 0.18f, 0.18f));

            WireFields(root.GetComponent<WinView>(),
                ("_movesText",       movesText),
                ("_timeText",        timeText),
                ("_playAgainButton", playBtn.GetComponent<Button>()),
                ("_mainMenuButton",  menuBtn.GetComponent<Button>()));

            SaveAndDestroy(root, WinPath);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static GameObject CreateViewRoot(string name)
        {
            var go = new GameObject(name);
            StretchRect(go);
            go.AddComponent<CanvasGroup>();
            return go;
        }

        private static void StretchRect(GameObject go)
        {
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Image CreateImage(GameObject parent, string name, Color color)
        {
            var go = CreateChild(parent, name, Vector2.zero, Vector2.one);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static GameObject CreateChild(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        private static TMP_Text CreateTMP(GameObject parent, string name, string text,
            int size, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
            return tmp;
        }

        private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static GameObject CreateButton(GameObject parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color? color = null)
        {
            var go  = CreateChild(parent, name, anchorMin, anchorMax);
            var img = go.AddComponent<Image>();
            img.color = color ?? new Color(0.18f, 0.42f, 0.72f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGo   = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;

            return go;
        }

        private static void WireFields(Component target, params (string field, Object value)[] pairs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in pairs)
            {
                var prop = so.FindProperty(field);
                if (prop == null)
                {
                    Debug.LogWarning($"[MemoryGamePrefabGenerator] Field '{field}' not found on {target.GetType().Name}");
                    continue;
                }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SaveAndDestroy(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static bool Exists(string path)
        {
            if (!System.IO.File.Exists(path)) return false;
            Debug.Log($"[MemoryGamePrefabGenerator] {System.IO.Path.GetFileName(path)} already exists — skipping.");
            return true;
        }

        private static void EnsureFolder(string parent, string child)
        {
            var full = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
