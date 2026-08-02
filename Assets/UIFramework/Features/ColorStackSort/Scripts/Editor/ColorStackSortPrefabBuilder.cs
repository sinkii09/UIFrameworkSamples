using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ColorStackSort.Editor
{
    /// <summary>
    /// Builds the Ball / Tube / BoardView prefabs from code, so the feature has no binary art
    /// dependency and the hierarchy constraints below are enforced rather than remembered.
    /// <para>
    /// <b>Create-if-missing.</b> Existing prefabs are skipped, never rebuilt. This is deliberate:
    /// <c>AircraftStrikerSetupWizard</c> rebuilds from hardcoded values and silently reverts any
    /// manual edit, which has bitten this project before. Delete a prefab to regenerate it.
    /// </para>
    /// </summary>
    internal static class ColorStackSortPrefabBuilder
    {
        private const string PrefabFolder = "Assets/UIFramework/Features/ColorStackSort/Prefabs";

        /// <summary>
        /// View prefabs must live under a Resources folder: ResourcesUILoader passes the view key
        /// verbatim to <c>Resources.LoadAsync</c>, so this path below "Resources/" has to match
        /// <c>[UIViewKey("ColorStackSort/BoardView")]</c> exactly.
        /// <para>
        /// Only views go here. Ball and Tube are referenced through serialized fields, so forcing
        /// them into a folder that ships in every build would buy nothing.
        /// </para>
        /// </summary>
        private const string ViewFolder =
            "Assets/UIFramework/Features/ColorStackSort/Resources/ColorStackSort";

        private const float BallSize = 76f;
        private const float TubeWidth = 118f;
        private const float TubeHeight = 400f;

        [MenuItem("Tools/ColorStackSort/Build Prefabs")]
        internal static void BuildPrefabs()
        {
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(ViewFolder);

            // SaveAsPrefabAsset needs the folders registered in the AssetDatabase, not just present
            // on disk — matters on a clean regenerate where Resources/ColorStackSort is brand new.
            AssetDatabase.Refresh();

            var ball = BuildIfMissing($"{PrefabFolder}/Ball.prefab", CreateBall);
            var tube = BuildIfMissing($"{PrefabFolder}/Tube.prefab", CreateTube);
            BuildIfMissing($"{ViewFolder}/BoardView.prefab",
                () => CreateBoard(
                    AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Tube.prefab"),
                    AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Ball.prefab")));
            BuildIfMissing($"{ViewFolder}/ColorStackSortWinView.prefab",
                ColorStackSortPanelPrefabBuilder.CreateWinView);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ColorStackSort] Prefabs ready in {PrefabFolder} + {ViewFolder} " +
                      $"(ball={ball}, tube={tube}).");
        }

        private static bool BuildIfMissing(string path, System.Func<GameObject> create)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log($"[ColorStackSort] {Path.GetFileName(path)} already exists — skipped, not rebuilt.");
                return false;
            }

            var root = create();
            if (root == null) return false;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            return true;
        }

        private static GameObject CreateBall()
        {
            var rect = UiPrefabFactory.CreatePoint("Ball", null, new Vector2(BallSize, BallSize));
            var image = UiPrefabFactory.AddImage(
                rect, UiPrefabFactory.BuiltIn(UiPrefabFactory.KnobSprite), Color.white, false);

            var view = rect.gameObject.AddComponent<BallView>();
            UiPrefabFactory.SetReference(view, "_image", image);

            return rect.gameObject;
        }

        private static GameObject CreateTube()
        {
            var rect = UiPrefabFactory.CreatePoint("Tube", null, new Vector2(TubeWidth, TubeHeight));
            var body = UiPrefabFactory.AddImage(
                rect, UiPrefabFactory.BuiltIn(UiPrefabFactory.PanelSprite),
                new Color(1f, 1f, 1f, 0.13f), true);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = body;
            button.transition = Selectable.Transition.None;

            // Balls live here, NOT on the tube root: Shake() animates this child so a LayoutGroup
            // on the tube row cannot overwrite it. No LayoutGroup on this object, ever.
            var slots = UiPrefabFactory.CreatePoint("Slots", rect, new Vector2(TubeWidth, TubeHeight));
            slots.anchorMin = slots.anchorMax = new Vector2(0.5f, 0f);
            slots.pivot = new Vector2(0.5f, 0f);
            slots.anchoredPosition = Vector2.zero;

            var view = rect.gameObject.AddComponent<TubeView>();
            UiPrefabFactory.SetReference(view, "_slotRoot", slots);
            UiPrefabFactory.SetReference(view, "_button", button);
            UiPrefabFactory.SetReference(
                view, "_feedback", ColorStackSortJuicePrefabParts.AddTubeFeedback(rect, body));

            return rect.gameObject;
        }

        private static GameObject CreateBoard(GameObject tubePrefab, GameObject ballPrefab)
        {
            var root = UiPrefabFactory.CreateStretch("BoardView", null);
            root.gameObject.AddComponent<CanvasGroup>();

            var tubeRow = UiPrefabFactory.CreateStretch("TubeRow", root);
            var grid = tubeRow.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(TubeWidth, TubeHeight);
            grid.spacing = new Vector2(26f, 40f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            // MUST be last — uGUI draws in hierarchy order, so anything after this would cover
            // travelling balls. BoardView also calls SetAsLastSibling at runtime as a belt-and-braces.
            // No Mask or RectMask2D anywhere above it, or the overlay would clip travelling balls.
            var overlay = UiPrefabFactory.CreateStretch("TravelOverlay", root);
            overlay.SetAsLastSibling();

            // Built before the overlay's SetAsLastSibling below would matter, but parented under
            // root either way — the bar sits at the bottom of the screen, clear of the tube row.
            var controlBar = ColorStackSortPanelPrefabBuilder.CreateControlBar(root);

            // Under root, NOT under the overlay: BoardRenderer.Clear() destroys every child of the
            // overlay to sweep up balls stranded by a cancelled move, and would take the emitter
            // with them. Sitting below the overlay in draw order is fine — the burst fires once the
            // balls have already landed.
            var burstEmitter = ColorStackSortJuicePrefabParts.CreateBurstEmitter(root, "BurstEmitter");

            var view = root.gameObject.AddComponent<BoardView>();
            UiPrefabFactory.SetReference(view, "_tubeRow", tubeRow);
            UiPrefabFactory.SetReference(view, "_travelOverlay", overlay);
            UiPrefabFactory.SetReference(view, "_controlBar", controlBar);
            UiPrefabFactory.SetReference(view, "_burstEmitter", burstEmitter);
            // Saving a BoardView with either reference unset produces a prefab that builds fine and
            // then renders an empty board at runtime — say so rather than skipping in silence.
            if (tubePrefab != null) UiPrefabFactory.SetReference(view, "_tubePrefab", tubePrefab.GetComponent<TubeView>());
            else Debug.LogError("[ColorStackSort] Tube.prefab missing — BoardView._tubePrefab left unset.");

            if (ballPrefab != null) UiPrefabFactory.SetReference(view, "_ballPrefab", ballPrefab.GetComponent<BallView>());
            else Debug.LogError("[ColorStackSort] Ball.prefab missing — BoardView._ballPrefab left unset.");

            return root.gameObject;
        }

    }
}
