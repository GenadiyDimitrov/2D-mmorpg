using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Client.Editor
{
    /// <summary>
    /// Headless Android build, driven from the command line so the Unity Editor GUI never has to be
    /// open. Close the Editor (freeing its RAM), then from a shell:
    ///
    ///   Unity.exe -quit -batchmode -nographics \
    ///     -projectPath "…/Game.Client.Unity" -buildTarget Android \
    ///     -executeMethod Game.Client.Editor.CommandLineBuild.BuildAndroid \
    ///     -logFile "…/unitybuild.log"
    ///
    /// Produces builds/L2Clone.apk. Install and launch it with adb afterwards; this method only
    /// builds (batch-mode auto-deploy is unreliable). Exit code is 0 on success, 1 on failure, so a
    /// caller can tell whether an APK was actually produced.
    /// </summary>
    public static class CommandLineBuild
    {
        private const string OutputApk = "builds/L2Clone.apk";

        /// <summary>
        /// Stamp the APK's version from <see cref="Game.Shared.GameConstants.GameVersion"/> — the ONE
        /// source of truth the server and the login handshake already use.
        ///
        /// Android's app info showed 0.1.0 while the game said 0.28.x, because Unity's bundleVersion
        /// is a separate field nobody was maintaining. Two version numbers where one is edited by hand
        /// is a number that will be wrong; deriving it at build time means it simply cannot drift.
        ///
        /// bundleVersionCode must be an ever-increasing INTEGER (Android refuses to install an older
        /// code over a newer one), so MAJOR.MINOR.BUILD is packed as M*10000 + m*100 + b.
        /// </summary>
        private static void StampVersion()
        {
            string version = Game.Shared.GameConstants.GameVersion;
            PlayerSettings.bundleVersion = version;

            var parts = version.Split('.');
            int major = parts.Length > 0 && int.TryParse(parts[0], out var a) ? a : 0;
            int minor = parts.Length > 1 && int.TryParse(parts[1], out var b) ? b : 0;
            int build = parts.Length > 2 && int.TryParse(parts[2], out var c) ? c : 0;

            PlayerSettings.Android.bundleVersionCode = major * 10000 + minor * 100 + build;
            Debug.Log($"[build] version {version} (code {PlayerSettings.Android.bundleVersionCode})");
        }

        [MenuItem("Build/Android APK")]
        public static void BuildAndroid()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                // Build Settings had no scene enabled — fall back to the one scene we ship.
                scenes = new[] { "Assets/Scenes/SampleScene.unity" };
            }

            StampVersion();

            Debug.Log("[build] Android build starting. Scenes: " + string.Join(", ", scenes));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputApk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[build] SUCCEEDED → {OutputApk}  ({summary.totalSize / (1024 * 1024)} MB, " +
                          $"{summary.totalTime.TotalSeconds:0}s)");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[build] FAILED: {summary.result} — {summary.totalErrors} error(s).");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
