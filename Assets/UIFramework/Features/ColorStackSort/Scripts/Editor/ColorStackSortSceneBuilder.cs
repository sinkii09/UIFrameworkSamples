using System.IO;
using Sinkii09.UIFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ColorStackSort.Editor
{
    /// <summary>
    /// Builds the playable scene from code, so the framework's scene requirements are enforced
    /// rather than remembered.
    /// <para>
    /// <b>Create-if-missing.</b> Refuses to overwrite an existing scene — the same rule as
    /// <see cref="ColorStackSortPrefabBuilder"/>, and for the same reason: a builder that rebuilds
    /// from hardcoded values silently reverts every manual edit, which has already cost this
    /// project once (AircraftStrikerSetupWizard).
    /// </para>
    /// <para>
    /// The UIRoot recipe mirrors the package's own <c>Step7_CreateUIRootPrefab</c>
    /// (Editor/Installer/UIFrameworkInstallerWizardSteps.cs) — treat that method as the source of
    /// truth if the two ever disagree. It is not called directly because it writes a prefab to the
    /// project-global path Assets/UIFramework/UIRoot.prefab, adds components reflectively so it
    /// cannot wire the serialized fields, and emits no EventSystem.
    /// </para>
    /// </summary>
    internal static class ColorStackSortSceneBuilder
    {
        private const string SceneFolder = "Assets/UIFramework/Features/ColorStackSort/Scenes";
        private const string ScenePath = SceneFolder + "/ColorStackSort.unity";

        // Portrait phone — matches the locked mobile-portrait target.
        private static readonly Vector2 ReferenceResolution = new(1080f, 1920f);

        private static readonly string[] LayerNames = { "HUD", "Screen", "Popup", "Overlay", "Debug" };
        private static readonly int[] LayerOrders = { 0, 100, 200, 300, 400 };

        [MenuItem("Tools/ColorStackSort/Build Scene")]
        internal static void BuildScene()
        {
            if (File.Exists(ScenePath))
            {
                Debug.Log($"[ColorStackSort] {ScenePath} already exists — skipped, not rebuilt. " +
                          "Delete it to regenerate.");
                return;
            }

            // NewScene below closes whatever is open without saving, and the File.Exists check
            // above only protects the target scene, not the user's current work.
            //
            // Refuse rather than prompt. SaveCurrentModifiedScenesIfUserWantsTo() opens a modal
            // dialog, which blocks Unity's main thread indefinitely when this runs from automation
            // (a menu item invoked over the MCP bridge) — it deadlocked the Editor exactly once
            // here. Refusing is non-blocking, loses nothing, and matches this builder's
            // never-clobber convention.
            var active = SceneManager.GetActiveScene();
            if (active.isDirty)
            {
                Debug.LogError($"[ColorStackSort] Scene '{active.name}' has unsaved changes — " +
                               "save or discard it, then run this again. Nothing was modified.");
                return;
            }

            Directory.CreateDirectory(SceneFolder);

            // Both are create-if-missing, so this stays cheap on re-runs. Prefabs are built here
            // too: a scene whose BoardView prefab is missing from Resources cannot show anything.
            ColorStackSortPrefabBuilder.BuildPrefabs();
            ColorStackSortAssetBuilder.BuildAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Loaded AFTER NewScene, deliberately. Creating a scene in Single mode unloads assets
            // the new scene does not reference yet, so anything grabbed before this line comes back
            // as a destroyed object and silently serializes as null. See LoadAssets().
            var (config, settings) = ColorStackSortAssetBuilder.LoadAssets();
            if (config == null || settings == null)
            {
                // LoadAssets already logged which one. Abort rather than save a scene whose
                // serialized fields are {fileID: 0} — that looks built but cannot run.
                Debug.LogError("[ColorStackSort] Aborting scene build — required assets missing.");
                return;
            }

            CreateCamera();

            // Must be a scene root: UIFrameworkLifetimeScope.Awake calls DontDestroyOnLoad, which
            // fails on a non-root GameObject.
            var root = CreateUIRoot();
            var layers = CreateLayers(root);
            CreateEventSystem(root);
            WireScope(root, config, settings, layers);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[ColorStackSort] Scene created at {ScenePath}.");
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.10f, 0.15f);
            camera.orthographic = true;

            // The UI is ScreenSpaceOverlay and needs no camera, but a scene without one logs
            // "Display 1: No cameras rendering" every frame.
            go.AddComponent<AudioListener>();
        }

        private static GameObject CreateUIRoot()
        {
            var root = new GameObject("UIRoot", typeof(RectTransform));

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // The scope finds this with GetComponentInChildren, so it must be on the UIRoot or
            // below it — anywhere else in the scene is not found.
            root.AddComponent<SafeAreaProvider>();

            return root;
        }

        private static Transform[] CreateLayers(GameObject root)
        {
            var layers = new Transform[LayerNames.Length];

            for (var i = 0; i < LayerNames.Length; i++)
            {
                var go = new GameObject(LayerNames[i], typeof(RectTransform));
                go.transform.SetParent(root.transform, false);

                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var canvas = go.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = LayerOrders[i];

                // Required: UIRootLayerRefs silently skips interactability changes on any layer
                // without a GraphicRaycaster.
                go.AddComponent<GraphicRaycaster>();
                go.AddComponent<CanvasGroup>();

                if (LayerNames[i] == "Debug") go.SetActive(false);

                layers[i] = rect;
            }

            return layers;
        }

        private static void CreateEventSystem(GameObject root)
        {
            var go = new GameObject("EventSystem");
            go.transform.SetParent(root.transform, false);
            go.AddComponent<EventSystem>();

            // The framework's back-button source is the new Input System, so the legacy
            // StandaloneInputModule would throw at runtime.
            go.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>
        /// Wires the serialized fields. Uses <see cref="SerializedObject"/> because
        /// <c>_config</c> and <c>_layers</c> are private fields on the base class
        /// <see cref="UIFrameworkLifetimeScope"/> and are unreachable through the public API.
        /// </summary>
        private static void WireScope(
            GameObject root, UIFrameworkConfig config, ColorStackSortSettings settings, Transform[] layers)
        {
            var scope = root.AddComponent<ColorStackSortLifetimeScope>();
            var serialized = new SerializedObject(scope);

            serialized.FindProperty("_config").objectReferenceValue = config;
            serialized.FindProperty("_settings").objectReferenceValue = settings;

            // Unassigned _layers is fatal: the scope logs an error and skips the registration, and
            // every IUIViewFactory resolution then fails.
            var layersProperty = serialized.FindProperty("_layers");
            for (var i = 0; i < LayerNames.Length; i++)
                layersProperty.FindPropertyRelative(LayerNames[i]).objectReferenceValue = layers[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
