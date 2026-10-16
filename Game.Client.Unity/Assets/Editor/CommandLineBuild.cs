using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Client.Editor
{
    /// <summary>
    /// Headless Android (and Windows test-client) build, driven from the command line so the Unity Editor GUI never has to be
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

        /// <summary>`BL-345` (0.268.0) — the app's permanent identity. Android knows an app by this id AND the key that
        /// signed it: change either and every phone must uninstall before it can install the new one. Was Unity's
        /// template id (`com.UnityTechnologies.com.unity.template.urpblank`) on this PC's throwaway debug key.</summary>
        public const string AndroidPackageId = "com.gdimitrov.mmo";

        /// <summary>The release key lives OUTSIDE the repo, beside its password: <c>%USERPROFILE%\.vardelar-signing\</c>
        /// (<c>release.keystore</c>, alias <c>vardelar</c>, and <c>password.txt</c>). Never commit either.</summary>
        private static readonly string SigningDir =
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vardelar-signing");

        /// <summary>
        /// `BL-345` — the package id, and the release key. ⚠ NO KEY = NO BUILD, deliberately: falling back to the debug
        /// key would make an APK no friend's phone can install over the one it has (a silent "uninstall first").
        /// Lose the key and the same holds for every future build — keep a copy of the folder somewhere safe.
        /// </summary>
        private static void StampSigning()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackageId);

            string keystore = System.IO.Path.Combine(SigningDir, "release.keystore");
            string passFile = System.IO.Path.Combine(SigningDir, "password.txt");
            if (!System.IO.File.Exists(keystore) || !System.IO.File.Exists(passFile))
                throw new BuildFailedException($"[build] no release key: {keystore} + password.txt are needed (BL-345)");
            string pass = System.IO.File.ReadAllText(passFile).Trim();

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = pass;
            PlayerSettings.Android.keyaliasName = "vardelar";
            PlayerSettings.Android.keyaliasPass = pass;
            Debug.Log($"[build] package {AndroidPackageId}, signed with {keystore}");
        }

        [MenuItem("Build/Android APK")]
        public static void BuildAndroid()
        {
            StampVersion();
            StampBranding();
            StampSigning();
            Build(BuildTarget.Android, BuildTargetGroup.Android, OutputApk);
        }

        private const string OutputWindowsExe = "builds/windows/Vardelar.exe";

        /// <summary>
        /// `BL-360` — the PC test client: the same game, built for 64-bit Windows. The owner, 2026-10-14: *"if
        /// possible not forced full screen, but to be a bordered window -> options for fullscreen later"*. So it
        /// opens as an ordinary bordered, RESIZABLE window at 1600×900; a fullscreen on/off option is owed.
        ///
        /// ⚠ Unity remembers the last window size/mode per machine (registry, Screenmanager keys) and reapplies
        /// it on the next launch — these settings decide the FIRST launch only.
        ///
        /// `runInBackground` is on: a windowed game loses focus whenever you click another window, and a paused
        /// player stops pumping the connection. Android ignores the flag. Mouse input needs no work — the Editor
        /// already plays by mouse (left click = tap, right drag = turn, wheel = zoom).
        /// </summary>
        [MenuItem("Build/Windows (test client)")]
        public static void BuildWindows()
        {
            StampVersion();
            StampBranding();

            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, OutputWindowsExe);
        }

        private static void Build(BuildTarget target, BuildTargetGroup group, string output)
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

            Debug.Log($"[build] {target} build starting. Scenes: " + string.Join(", ", scenes));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                targetGroup = group,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[build] SUCCEEDED → {output}  ({summary.totalSize / (1024 * 1024)} MB, " +
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
