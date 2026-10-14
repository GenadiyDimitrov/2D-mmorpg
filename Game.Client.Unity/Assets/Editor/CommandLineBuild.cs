using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
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

        private const string AppIconPath = "Assets/Branding/AppIcon.png";
        private const string SplashLogoPath = "Assets/Branding/SplashLogo.png";

        /// <summary>
        /// The launcher icon, the name under it, and the logo on Unity's startup screen (owner, 2026-10-14:
        /// the cracked-star logo and the full logo from `docs/design/Assets`). All placeholders — swap a file
        /// at the same path and the next build picks it up; the startup logo becomes a company logo later.
        /// The icon is the DEFAULT one, which Android uses for every density it is not given separately. The
        /// label was "Game.Client.Unity" — Unity's project name — until now.
        /// </summary>
        private static void StampBranding()
        {
            PlayerSettings.productName = "Vardelar";

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon == null) Debug.LogWarning("[build] no app icon at " + AppIconPath);
            else
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
                Debug.Log("[build] app icon " + AppIconPath);
            }

            // The startup screen only takes a SPRITE, and a dropped-in PNG imports as a plain texture, so the
            // importer is set here rather than trusting whatever .meta a swapped file arrives with.
            if (AssetImporter.GetAtPath(SplashLogoPath) is TextureImporter importer)
            {
                if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }
                var logo = AssetDatabase.LoadAssetAtPath<Sprite>(SplashLogoPath);
                if (logo != null)
                {
                    PlayerSettings.SplashScreen.show = true;
                    PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2.5f, logo) };
                    Debug.Log("[build] splash logo " + SplashLogoPath);
                }
            }
            else Debug.LogWarning("[build] no splash logo at " + SplashLogoPath);
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
            StampBranding();

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
