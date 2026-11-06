using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace Game.Client
{
    /// <summary>
    /// `BL-371` — THE ONE DOOR every non-core asset comes through: models, buildings, weapons, props, music, sounds,
    /// icons, the login art. Callback-shaped on purpose: an asset arrives LATER than it was asked for, and every caller
    /// is written to cope with that, so stage 3 (download on demand) changes the body of this class and nothing that
    /// calls it.
    ///
    /// <para>🔑 <b>The contract a caller must honour:</b> the callback may run AT ONCE (the asset is already cached) or
    /// some frames later, and it may hand over <c>null</c> (there is no such asset — the callers' fallbacks are the
    /// design, see <see cref="ModelLibrary"/>). Never assume either. A callback that touches a MonoBehaviour checks it
    /// is still alive: the window or body that asked may have been destroyed while the load ran.</para>
    ///
    /// <para>🔑 <b>The cache holds the MISSES too.</b> Until art lands most model keys miss, and a miss that is not
    /// remembered is a lookup per spawn, forever. Two requests for one key while it loads share the one load.</para>
    ///
    /// <para><b>The body is ADDRESSABLES</b> (stage 2, 0.277.0): the assets live in <c>Assets/Content/&lt;Folder&gt;/</c>,
    /// one group per folder, address = the path under <c>Content/</c> without its extension (the same keys the
    /// <c>Resources</c> era used), each entry labelled with its folder (<c>AddressablesSetup</c>, editor). The bundles
    /// ship INSIDE the build. 🔴 <b>A key is looked up as a LOCATION first</b>, never loaded blind: Addressables throws
    /// on an unknown key, and <see cref="ModelLibrary"/>'s fallback chain misses on purpose, every spawn. A folder is
    /// its label's locations filtered by path prefix, which is <c>Resources.LoadAll</c>'s recursive meaning.</para>
    ///
    /// <para>Outside play mode (the editor's headless preview tools) everything is SYNCHRONOUS, read straight from
    /// <c>Assets/Content</c> through the AssetDatabase: no player loop runs there to finish an async load, and a
    /// preview must not depend on the bundles having been built.</para>
    ///
    /// <para>⚠ <b>Core stays OUT of here</b>, on <c>Resources</c>: the shaders (a missing shader is a pink world, and
    /// they are tiny) and <c>server.txt</c> (needed before anything else can happen). Everything else goes through this.</para>
    /// </summary>
    public static class GameAssets
    {
        /// <summary>Where the addressable assets sit in the project (editor meaning; the runtime knows addresses).</summary>
        public const string ContentRoot = "Assets/Content";

        private sealed class Entry
        {
            public bool Done;
            public object Value;                       // T (single) or T[] (folder); null = a miss
            public List<Action<object>> Waiting;       // callers who asked before it was Done
        }

        private static readonly Dictionary<string, Entry> Cache = new Dictionary<string, Entry>();

        /// <summary>Loads still running. A caller that redraws when assets land can wait for this to reach 0 rather than
        /// redrawing once per arrival (the skill windows do).</summary>
        public static int Pending { get; private set; }

        /// <summary>Bumped every time any load finishes. Cheap to poll for "has anything new arrived".</summary>
        public static int Revision { get; private set; }

        /// <summary>One asset by its address (<c>"Models/humanoid"</c>, no extension). <paramref name="done"/> gets it,
        /// or null when there is none.</summary>
        public static void Load<T>(string path, Action<T> done) where T : Object
        {
            if (done == null) return;
            if (string.IsNullOrEmpty(path)) { done(null); return; }
            string key = "1|" + typeof(T).Name + "|" + path;
            Request(key, o => done(o as T), () =>
            {
                if (!Application.isPlaying) { Finish(key, EditorLoad<T>(path)); return; }
                Locate(path, typeof(T), locations =>
                {
                    if (locations == null || locations.Count == 0) { Finish(key, null); return; }
                    var op = Addressables.LoadAssetAsync<T>(locations[0]);
                    op.Completed += h => Finish(key, Succeeded(h, path) ? h.Result : null);
                });
            });
        }

        /// <summary>Every asset of type <typeparamref name="T"/> in a folder and its subfolders (<c>"Sounds/crit"</c>).
        /// Never null: an empty or missing folder is an empty array.</summary>
        public static void LoadAll<T>(string folder, Action<T[]> done) where T : Object
        {
            if (done == null) return;
            folder = (folder ?? "").TrimEnd('/');
            string key = "*|" + typeof(T).Name + "|" + folder;
            Request(key, o => done(o as T[] ?? new T[0]), () =>
            {
                if (!Application.isPlaying) { Finish(key, EditorLoadAll<T>(folder)); return; }
                int slash = folder.IndexOf('/');
                string label = slash < 0 ? folder : folder.Substring(0, slash);   // the top folder = the group's label
                string prefix = folder + "/";
                Locate(label, typeof(T), locations =>
                {
                    var inFolder = new List<IResourceLocation>();
                    var seen = new HashSet<string>();
                    if (locations != null)
                        foreach (var loc in locations)
                            if (loc.PrimaryKey.StartsWith(prefix, StringComparison.Ordinal) && seen.Add(loc.InternalId))
                                inFolder.Add(loc);
                    if (inFolder.Count == 0) { Finish(key, new T[0]); return; }
                    var op = Addressables.LoadAssetsAsync<T>(inFolder, null);
                    op.Completed += h =>
                    {
                        var list = Succeeded(h, folder) ? h.Result : null;
                        var arr = new T[list?.Count ?? 0];
                        for (int i = 0; i < arr.Length; i++) arr[i] = list[i];
                        Finish(key, arr);
                    };
                });
            });
        }

        /// <summary>The asset if it has ALREADY arrived (true, with null meaning "there is none"), without asking for it.
        /// For a per-frame caller that only wants to know whether it can draw yet.</summary>
        public static bool TryGet<T>(string path, out T asset) where T : Object
        {
            asset = null;
            if (!Cache.TryGetValue("1|" + typeof(T).Name + "|" + path, out var e) || !e.Done) return false;
            asset = e.Value as T;
            return true;
        }

        /// <summary>The locations of a key filtered to a type — an empty list for an unknown key, never an exception.
        /// The handle is released at once: the locations are plain data that outlive it.</summary>
        private static void Locate(string key, Type type, Action<IList<IResourceLocation>> then)
        {
            AsyncOperationHandle<IList<IResourceLocation>> op;
            try { op = Addressables.LoadResourceLocationsAsync(key, type); }
            catch (Exception ex) { Debug.LogException(ex); then(null); return; }
            op.Completed += h =>
            {
                var found = h.Status == AsyncOperationStatus.Succeeded && h.Result != null
                    ? new List<IResourceLocation>(h.Result) : null;
                Addressables.Release(h);
                then(found);
            };
        }

        private static bool Succeeded<TResult>(AsyncOperationHandle<TResult> h, string what)
        {
            if (h.Status == AsyncOperationStatus.Succeeded) return true;
            Debug.LogWarning($"[assets] {what}: load failed — {h.OperationException?.Message}");
            return false;
        }

        private static void Request(string key, Action<object> deliver, Action start)
        {
            if (Cache.TryGetValue(key, out var e))
            {
                if (e.Done) Deliver(deliver, e.Value);
                else e.Waiting.Add(deliver);
                return;
            }
            Cache[key] = new Entry { Waiting = new List<Action<object>> { deliver } };
            Pending++;
            start();
        }

        private static void Finish(string key, object value)
        {
            if (!Cache.TryGetValue(key, out var e) || e.Done) return;
            e.Done = true;
            e.Value = value;
            var waiting = e.Waiting;
            e.Waiting = null;
            Pending--;
            Revision++;
            foreach (var w in waiting) Deliver(w, value);
        }

        /// <summary>One caller's mistake must not starve the others waiting on the same asset.</summary>
        private static void Deliver(Action<object> deliver, object value)
        {
            try { deliver(value); }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        // ----- edit mode: straight from the project, synchronously ------------------------------------------------

        private static T EditorLoad<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            string full = ContentRoot + "/" + path;
            string dir = System.IO.Path.GetDirectoryName(full)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir)) return null;
            foreach (var file in System.IO.Directory.GetFiles(dir, System.IO.Path.GetFileName(full) + ".*"))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(file.Replace('\\', '/'));
                if (asset != null) return asset;
            }
#endif
            return null;
        }

        private static T[] EditorLoadAll<T>(string folder) where T : Object
        {
            var found = new List<T>();
#if UNITY_EDITOR
            string dir = ContentRoot + "/" + folder;
            if (!UnityEditor.AssetDatabase.IsValidFolder(dir)) return found.ToArray();
            var seen = new HashSet<string>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("", new[] { dir }))
            {
                if (!seen.Add(guid)) continue;
                string p = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (UnityEditor.AssetDatabase.IsValidFolder(p)) continue;
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(p);
                if (asset != null) found.Add(asset);
            }
#endif
            return found.ToArray();
        }
    }
}
