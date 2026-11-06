using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.Client.Editor
{
    /// <summary>
    /// `BL-371` stage 2 — the Addressables setup, made and kept by CODE so the Editor never has to be opened.
    ///
    /// <para>🔑 <b>A FOLDER IS A GROUP, and dropping a file in is enough.</b> Every file under
    /// <c>Assets/Content/&lt;Folder&gt;/</c> becomes an entry of the group <c>&lt;Folder&gt;</c>, with the address
    /// <c>GameAssets</c> asks for: its path under <c>Content/</c> with no extension (<c>"Models/humanoid"</c>,
    /// <c>"Sounds/crit/hit1"</c>), the same key the <c>Resources</c> era used. Each entry also carries its folder as a
    /// LABEL, which is how <c>GameAssets.LoadAll</c> finds a subfolder's assets. <see cref="Sync"/> runs on every import
    /// under <c>Content/</c> (the postprocessor below) AND at the start of every build, so a file copied in by a tool or
    /// by hand is never left out.</para>
    ///
    /// <para><b>Local only</b> (stage 2): the bundles build into the player's StreamingAssets and load from there. Stage
    /// 3 (download on demand) changes the groups' load path, not this layout.</para>
    ///
    /// <para>Headless: <c>Unity.exe -batchmode -quit -projectPath … -executeMethod
    /// Game.Client.Editor.AddressablesSetup.Setup</c>. The player builds call <see cref="BuildContent"/> themselves
    /// (<see cref="CommandLineBuild"/>).</para>
    /// </summary>
    public static class AddressablesSetup
    {
        public const string ContentRoot = "Assets/Content";

        /// <summary>The groups, one per top folder. Music packs SEPARATELY (one bundle per track), so a new track never
        /// changes the bundle of the others; everything else is small enough to pack one bundle per folder.</summary>
        public static readonly string[] Folders =
            { "Models", "Buildings", "Weapons", "Props", "Music", "Sounds", "SkillIcons", "ActionIcons", "ItemIcons", "Ui" };

        private static readonly HashSet<string> PackSeparately = new HashSet<string> { "Music" };

        [MenuItem("Build/Addressables: sync groups")]
        public static void Setup()
        {
            Sync(createSettings: true);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Creates the settings when asked (and missing), the groups, and one entry per file; removes entries
        /// whose file is gone or moved out. Idempotent — a run with nothing new changes nothing.</summary>
        public static AddressableAssetSettings Sync(bool createSettings)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(createSettings);
            if (settings == null) return null;

            // The player builds build the content FIRST, explicitly, and fail on its error (CommandLineBuild). A second,
            // silent build inside BuildPlayer would only cost time.
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;

            if (!AssetDatabase.IsValidFolder(ContentRoot)) AssetDatabase.CreateFolder("Assets", "Content");

            int added = 0, removed = 0;
            foreach (var folder in Folders)
            {
                string dir = ContentRoot + "/" + folder;
                if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(ContentRoot, folder);

                var group = settings.FindGroup(folder) ?? settings.CreateGroup(folder, false, false, false, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                ConfigureLocal(settings, group, PackSeparately.Contains(folder));
                settings.AddLabel(folder, false);

                var wanted = new HashSet<string>();
                foreach (var guid in AssetDatabase.FindAssets("", new[] { dir }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) continue;
                    if (!wanted.Add(guid)) continue;

                    string address = AddressOf(path);
                    var entry = settings.FindAssetEntry(guid);
                    if (entry == null || entry.parentGroup != group)
                    {
                        entry = settings.CreateOrMoveEntry(guid, group, false, false);
                        added++;
                    }
                    if (entry.address != address) entry.SetAddress(address, false);
                    // Exactly ONE label, its own folder: a file moved between folders must not keep the old one.
                    foreach (var l in entry.labels.ToArray())
                        if (l != folder) entry.SetLabel(l, false, false, false);
                    entry.SetLabel(folder, true, false, false);
                }

                foreach (var entry in group.entries.ToArray())
                    if (!wanted.Contains(entry.guid)) { group.RemoveAssetEntry(entry, false); removed++; }
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"[addressables] synced {Folders.Length} groups: +{added} / -{removed} entries");
            return settings;
        }

        /// <summary><c>Assets/Content/Sounds/crit/hit1.ogg</c> → <c>Sounds/crit/hit1</c>.</summary>
        public static string AddressOf(string assetPath)
        {
            string rel = assetPath.Substring(ContentRoot.Length + 1);
            int dot = rel.LastIndexOf('.');
            return dot > rel.LastIndexOf('/') ? rel.Substring(0, dot) : rel;
        }

        private static void ConfigureLocal(AddressableAssetSettings settings, AddressableAssetGroup group, bool separately)
        {
            var bundled = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
            bundled.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            bundled.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            bundled.BundleMode = separately
                ? BundledAssetGroupSchema.BundlePackingMode.PackSeparately
                : BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            bundled.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            bundled.IncludeInBuild = true;

            var update = group.GetSchema<ContentUpdateGroupSchema>() ?? group.AddSchema<ContentUpdateGroupSchema>();
            update.StaticContent = false;
        }

        /// <summary>Syncs, then builds the bundles for the ACTIVE build target (the <c>-buildTarget</c> of the headless
        /// run). Returns the error, or null when it built. The player build then copies them into StreamingAssets.</summary>
        public static string BuildContent()
        {
            var settings = Sync(createSettings: true);
            if (settings == null) return "no Addressables settings";
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error)) return result.Error;
            Debug.Log($"[addressables] content built for {EditorUserBuildSettings.activeBuildTarget} " +
                      $"in {result.Duration:0.0}s → {result.OutputPath}");
            return null;
        }

        /// <summary>Keeps the groups in step with the folder while working: a file imported, moved or deleted under
        /// <c>Content/</c> re-syncs once, after the import finishes. Only once the settings exist — creating them is
        /// <see cref="Setup"/>'s job, never a side effect of an import.</summary>
        private sealed class Watcher : AssetPostprocessor
        {
            private static bool _queued;

            private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                if (_queued || AddressableAssetSettingsDefaultObject.SettingsExists == false) return;
                bool touches = imported.Concat(deleted).Concat(moved).Concat(movedFrom)
                    .Any(p => p.StartsWith(ContentRoot + "/") && !p.EndsWith(".meta"));
                if (!touches) return;
                _queued = true;
                EditorApplication.delayCall += () => { _queued = false; Sync(createSettings: false); };
            }
        }
    }
}
