using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Spikes.WebGlPersistenceProbe.Editor
{
    /// <summary>
    /// Builds the WebGL persistence probe (plans/260919-1554, Phase 1).
    /// <para>
    /// Lives in a real Editor assembly rather than being driven by <c>script-execute</c>: a callback
    /// registered from a Roslyn-compiled dynamic assembly does not survive that assembly going away,
    /// so an <c>EditorApplication.delayCall</c> subscribed from there never fires. A persistent
    /// assembly keeps the delegate alive.
    /// </para>
    /// <para>
    /// <see cref="Schedule"/> returns immediately and lets the build run on a later editor tick, so
    /// the caller (an MCP request) is not held open for the whole build.
    /// </para>
    /// </summary>
    public static class SpikeWebGlBuilder
    {
        private const string Scene = "Assets/_Spikes/WebGlPersistenceProbe/WebGlPersistenceProbeScene.unity";
        private const string OutDir = @"E:\Hoc_2025\1_1_2025\TheEnd\Build\WebGLSpike";
        private const string Status = @"E:\Hoc_2025\1_1_2025\TheEnd\Build\WebGLSpike.status.txt";

        private static bool _queued;

        [MenuItem("Spikes/Build WebGL Persistence Probe")]
        public static void Schedule()
        {
            if (_queued)
            {
                Write("ALREADY QUEUED");
                return;
            }

            _queued = true;
            EditorApplication.update += Tick;
            Write("QUEUED " + DateTime.Now.ToString("HH:mm:ss"));
        }

        private static void Tick()
        {
            EditorApplication.update -= Tick;
            _queued = false;
            BuildNow();
        }

        /// <summary>Synchronous. Safe to invoke directly from a menu item.</summary>
        public static void BuildNow()
        {
            try
            {
                if (!File.Exists(Scene))
                {
                    Write("ABORT scene missing: " + Scene);
                    return;
                }

                Write("BUILDING started " + DateTime.Now.ToString("HH:mm:ss"));

                // Scenes are passed explicitly, so EditorBuildSettings is never touched and needs no
                // restore afterwards — the project's committed scene list stays exactly as it is.
                var opts = new BuildPlayerOptions
                {
                    scenes = new[] { Scene },
                    locationPathName = OutDir,
                    target = BuildTarget.WebGL,
                    targetGroup = BuildTargetGroup.WebGL,
                    options = BuildOptions.Development
                };

                var summary = BuildPipeline.BuildPlayer(opts).summary;
                Write($"DONE result={summary.result} sizeBytes={summary.totalSize} time={summary.totalTime} " +
                      $"errors={summary.totalErrors} warnings={summary.totalWarnings} finished={DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Write("EXCEPTION " + ex);
            }
        }

        private static void Write(string text)
        {
            try
            {
                File.WriteAllText(Status, text);
            }
            catch (Exception ex)
            {
                Debug.LogError("[SpikeWebGlBuilder] could not write status: " + ex.Message);
            }

            Debug.Log("[SpikeWebGlBuilder] " + text);
        }
    }
}
