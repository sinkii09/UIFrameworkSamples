using System;
using System.IO;
using Sinkii09.UIFramework;
using UnityEditor;
using UnityEngine;

namespace ColorStackSort.Editor
{
    /// <summary>
    /// Creates the two ScriptableObject assets the scene needs to run.
    /// <para>
    /// <b>Create-if-missing.</b> Existing assets are returned untouched — tuning done in the
    /// inspector must survive a re-run. Delete an asset to regenerate it at defaults.
    /// </para>
    /// </summary>
    internal static class ColorStackSortAssetBuilder
    {
        private const string ConfigFolder = "Assets/UIFramework/Features/ColorStackSort/Configs";

        /// <summary>
        /// Deliberately NOT inside a Resources folder. The framework falls back to
        /// <c>Resources.Load("UIFrameworkConfig")</c> when a scope has no config assigned, so an
        /// asset of this name under Resources would become a silent project-wide default and could
        /// shadow another feature's. This one is wired to the scope's field explicitly.
        /// </summary>
        private const string ConfigPath = ConfigFolder + "/UIFrameworkConfig.asset";

        private const string SettingsPath = ConfigFolder + "/ColorStackSortSettings.asset";

        [MenuItem("Tools/ColorStackSort/Build Assets")]
        internal static void BuildAssetsMenuItem()
        {
            var (config, settings) = BuildAssets();
            Debug.Log($"[ColorStackSort] Assets ready (config={config != null}, settings={settings != null}) " +
                      $"in {ConfigFolder}.");
        }

        internal static (UIFrameworkConfig Config, ColorStackSortSettings Settings) BuildAssets()
        {
            Directory.CreateDirectory(ConfigFolder);
            AssetDatabase.Refresh(); // CreateAsset needs the folder known to the AssetDatabase.

            var config = LoadOrCreate<UIFrameworkConfig>(ConfigPath, asset =>
            {
                // Resources, not Addressables: one prefab does not justify a catalog build step,
                // and Addressables is only a transitive dependency in this project.
                asset.LoaderMode = LoaderMode.Resources;
            });

            var settings = LoadOrCreate<ColorStackSortSettings>(SettingsPath, null);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return LoadAssets();
        }

        /// <summary>
        /// Loads both assets fresh from disk.
        /// <para>
        /// <b>Call this again after anything that can unload assets</b> — in particular
        /// <c>EditorSceneManager.NewScene(..., NewSceneMode.Single)</c>, which unloads assets the
        /// new scene does not yet reference. References taken before it become destroyed native
        /// objects that still look like live C# references, and assigning one to a
        /// SerializedProperty writes <c>{fileID: 0}</c> — a silently null field that surfaces only
        /// as a runtime failure. This is not hypothetical; it is exactly what the first two runs of
        /// this builder produced.
        /// </para>
        /// </summary>
        internal static (UIFrameworkConfig Config, ColorStackSortSettings Settings) LoadAssets()
        {
            var config = AssetDatabase.LoadAssetAtPath<UIFrameworkConfig>(ConfigPath);
            var settings = AssetDatabase.LoadAssetAtPath<ColorStackSortSettings>(SettingsPath);

            if (config == null) Debug.LogError($"[ColorStackSort] Failed to load config at {ConfigPath}.");
            if (settings == null) Debug.LogError($"[ColorStackSort] Failed to load settings at {SettingsPath}.");

            return (config, settings);
        }

        private static T LoadOrCreate<T>(string path, Action<T> configure) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                Debug.Log($"[ColorStackSort] {Path.GetFileName(path)} already exists — left as-is.");
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            configure?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);

            return asset;
        }
    }
}
