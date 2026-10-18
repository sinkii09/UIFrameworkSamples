using System;
using Sinkii09.UIFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RecyclerShowcase.Editor
{
    /// <summary>
    /// Tools/Recycler Showcase/Build Scene — generates the cell prefabs and the showcase scene.
    ///
    /// <para>Everything under Prefabs/ and Scenes/ is GENERATED and overwritten on every build
    /// (unlike ColorStackSort's create-if-missing builder): prefabs and scene are always rebuilt
    /// together, so no stale reference can survive. Change this code, not the generated assets.</para>
    /// </summary>
    internal static class RecyclerShowcaseSceneBuilder
    {
        private const string Root = "Assets/UIFramework/Features/RecyclerShowcase";
        private const string ScenePath = Root + "/Scenes/RecyclerShowcase.unity";
        private const int TabCount = 5;

        [MenuItem("Tools/Recycler Showcase/Build Scene")]
        internal static void Build()
        {
            if (!CanReplaceOpenScene()) return;

            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root, "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                Populate();
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException($"[RecyclerShowcase] Could not save '{ScenePath}'.");
                Debug.Log($"[RecyclerShowcase] Built {ScenePath}. Press Play and click through the tabs.");
            }
            catch (Exception exception)
            {
                // Logged, not swallowed. The half-built untitled scene is replaced with an empty one:
                // left dirty, it would make every later Build refuse with "unsaved changes".
                Debug.LogException(exception);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void Populate()
        {
            ShowcasePrefabBuilder.BuildAll();

            // Loaded AFTER NewScene: Single mode unloads assets the new scene does not reference yet,
            // and a reference taken before it is a fake null that serializes as {fileID: 0} silently.
            RecyclerCell row = Load(ShowcasePrefabBuilder.RowPath);
            RecyclerCell header = Load(ShowcasePrefabBuilder.HeaderPath);
            RecyclerCell picture = Load(ShowcasePrefabBuilder.PicturePath);

            CreateCamera();
            RectTransform canvas = CreateCanvas();
            CreateEventSystem();

            Button template = CreateButtonTemplate(canvas);
            Button[] tabs = CreateTabBar(canvas);
            ShowcasePanel[] panels = ShowcasePanelBuilder.BuildAll(canvas, template, row, header, picture);
            foreach (ShowcasePanel panel in panels) panel.gameObject.SetActive(false);

            var controller = new GameObject("RecyclerShowcase").AddComponent<RecyclerShowcaseController>();
            ShowcaseWiring.Refs(controller, "_panels", panels);
            ShowcaseWiring.Refs(controller, "_tabs", tabs);
        }

        [MenuItem("Tools/Recycler Showcase/Open Scene")]
        internal static void Open()
        {
            if (!CanReplaceOpenScene()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // Refuse rather than prompt: SaveCurrentModifiedScenesIfUserWantsTo opens a modal dialog that
        // blocks the main thread and deadlocks any MCP/automation caller.
        private static bool CanReplaceOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[RecyclerShowcase] Exit Play Mode first.");
                return false;
            }

            // Every loaded scene, not just the active one: Single mode closes additive scenes too,
            // without asking.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isDirty) continue;

                Debug.LogError($"[RecyclerShowcase] '{scene.name}' has unsaved changes — save or discard them first.");
                return false;
            }

            return true;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }

        private static RecyclerCell Load(string path)
        {
            var cell = AssetDatabase.LoadAssetAtPath<RecyclerCell>(path);
            if (cell == null) throw new InvalidOperationException($"[RecyclerShowcase] Prefab missing at '{path}'.");
            return cell;
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ShowcaseUiFactory.Ink;
            // The UI is ScreenSpaceOverlay; the camera only stops "No cameras rendering" spam.
            go.AddComponent<AudioListener>();
        }

        private static RectTransform CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Portrait, matching this project's Game view; fraction anchors keep landscape usable.
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        // The project runs the new Input System only — StandaloneInputModule would throw every frame.
        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static Button CreateButtonTemplate(Transform canvas)
        {
            Button template = ShowcaseUiFactory.Button(ShowcaseUiFactory.Create("ButtonTemplate", canvas), "Button", 20f);
            // Sized by the toolbar's GridLayoutGroup — no LayoutElement needed.
            template.gameObject.SetActive(false);
            return template;
        }

        private static Button[] CreateTabBar(Transform canvas)
        {
            RectTransform bar = ShowcaseUiFactory.TopBand("Tabs", canvas, 0f, ShowcasePanelLayout.TabBarHeight, 0f);
            ShowcaseUiFactory.Image(bar, ShowcaseUiFactory.Panel);

            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 16, 16);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;   // equal tabs across whatever width there is
            layout.childForceExpandHeight = true;

            var tabs = new Button[TabCount];
            for (int i = 0; i < TabCount; i++)
            {
                // Captions are filled at runtime from each panel's Title, so they live in one place.
                // 22 px: five tabs leave ~190 px each on a 1080-wide portrait screen.
                tabs[i] = ShowcaseUiFactory.Button(ShowcaseUiFactory.Create($"Tab {i + 1}", bar), $"Tab {i + 1}", 22f);
            }

            return tabs;
        }
    }
}
