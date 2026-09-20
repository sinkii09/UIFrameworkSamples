using System;
using System.Collections.Generic;
using System.Threading;
using AstralChorus.Content;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Sinkii09.UIFramework;
using UnityEditor;
using UnityEngine;

namespace AstralChorus.Tests.Content
{
    // Concrete ContentDefinitions to put in manifests. Three of them, because All<T>() has to prove
    // it matches subclasses and skips unrelated types.
    public class TestDefinition : ContentDefinition
    {
    }

    public sealed class DerivedDefinition : TestDefinition
    {
    }

    public sealed class OtherDefinition : ContentDefinition
    {
    }

    // Stands in for the framework's loaders.
    //
    // Two properties are deliberate and neither is incidental:
    //
    // 1. A MISSING KEY THROWS, exactly as ResourcesUILoader does. A fake that returned null instead
    //    would leave ContentPackLoader's catch block unexecuted while its test still passed -- the
    //    shape that already cost this repo a sprint (a fake implementing a capability it did not
    //    have, so the fallback branch never ran once).
    //
    // 2. IT COMPLETES SYNCHRONOUSLY. UniTask's PlayerLoop does not tick in EditMode, so a fake that
    //    really awaited would HANG the test runner rather than fail it.
    public sealed class FakeAssetLoader : IAssetLoader
    {
        private readonly Dictionary<string, UnityEngine.Object> _assets =
            new Dictionary<string, UnityEngine.Object>();

        // Runs before the cancellation check, so a test can cancel mid-build or re-enter the
        // registry while the loader is still between awaits.
        public Action<string> OnLoad;

        public int LoadCount { get; private set; }

        public void Add(string key, UnityEngine.Object asset) => _assets[key] = asset;

        public UniTask<T> LoadAssetAsync<T>(string key, CancellationToken ct = default)
            where T : UnityEngine.Object
        {
            LoadCount++;
            OnLoad?.Invoke(key);

            ct.ThrowIfCancellationRequested();

            if (!_assets.TryGetValue(key, out var asset))
            {
                throw new InvalidOperationException($"[FakeAssetLoader] nothing at key '{key}'.");
            }

            var typed = asset as T;
            if (typed == null)
            {
                throw new InvalidOperationException(
                    $"[FakeAssetLoader] asset at '{key}' is {asset.GetType().Name}, not {typeof(T).Name}.");
            }

            return UniTask.FromResult(typed);
        }

        public UniTask UnloadAsync(string key, CancellationToken ct = default)
        {
            UnloadCount++;
            ct.ThrowIfCancellationRequested();

            // Mirrors Addressables, which throws when asked to release a key it never handed out.
            // A fake that quietly succeeded would leave ContentAssetLoader's failure branch unrun.
            if (!_assets.ContainsKey(key))
            {
                throw new InvalidOperationException($"[FakeAssetLoader] nothing loaded at key '{key}'.");
            }

            return UniTask.CompletedTask;
        }

        public int UnloadCount { get; private set; }
    }

    // Builds ScriptableObjects in memory and tears them down. SerializedObject is UnityEditor-only,
    // which is one of the reasons every one of these tests is EditMode.
    public sealed class ContentObjects : IDisposable
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        public TestDefinition Definition(string id) => Definition<TestDefinition>(id);

        public T Definition<T>(string id) where T : ContentDefinition
        {
            var definition = Track(ScriptableObject.CreateInstance<T>());
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            definition.name = id ?? "unnamed";
            return definition;
        }

        public ContentPackManifest Manifest(string packId, params ContentDefinition[] entries)
        {
            var manifest = Track(ScriptableObject.CreateInstance<ContentPackManifest>());
            var serialized = new SerializedObject(manifest);
            serialized.FindProperty("_packId").stringValue = packId;

            var array = serialized.FindProperty("_entries");
            array.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return manifest;
        }

        public ContentBuildProfile Profile(string alwaysLoadedPackId, params string[] enabledPackIds)
        {
            var profile = Track(ScriptableObject.CreateInstance<ContentBuildProfile>());
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("_alwaysLoadedPackId").stringValue = alwaysLoadedPackId;
            serialized.FindProperty("_manifestKeyPrefix").stringValue = "ContentPacks/";

            var array = serialized.FindProperty("_enabledPackIds");
            array.arraySize = enabledPackIds.Length;
            for (int i = 0; i < enabledPackIds.Length; i++)
            {
                array.GetArrayElementAtIndex(i).stringValue = enabledPackIds[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private T Track<T>(T obj) where T : UnityEngine.Object
        {
            _created.Add(obj);
            return obj;
        }

        public void Dispose()
        {
            foreach (var obj in _created)
            {
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            }
            _created.Clear();
        }
    }

    // Counts what was actually logged. LogAssert stops a test from failing on an EXPECTED error, but
    // it is not a reliable way to assert "exactly one warning and no more" -- and the throttle's
    // whole point is the count.
    public sealed class LogRecorder : IDisposable
    {
        private readonly List<string> _warnings = new List<string>();

        public LogRecorder() => Application.logMessageReceived += Record;

        public IReadOnlyList<string> Warnings => _warnings;

        private void Record(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Warning) _warnings.Add(message);
        }

        public void Dispose() => Application.logMessageReceived -= Record;
    }

    public static class SyncTask
    {
        // Asserts the task finished without ever yielding, THEN surfaces its result or exception.
        // Without the first half, a fake that becomes genuinely asynchronous some day would freeze
        // the test runner instead of failing a test.
        public static void Run(UniTask task)
        {
            Assert.That(task.Status.IsCompleted(), Is.True,
                "Task did not complete synchronously. UniTask's PlayerLoop does not run in EditMode, " +
                "so an awaiting fake hangs the runner instead of failing.");
            task.GetAwaiter().GetResult();
        }

        public static T Run<T>(UniTask<T> task)
        {
            Assert.That(task.Status.IsCompleted(), Is.True,
                "Task did not complete synchronously. UniTask's PlayerLoop does not run in EditMode, " +
                "so an awaiting fake hangs the runner instead of failing.");
            return task.GetAwaiter().GetResult();
        }
    }
}
