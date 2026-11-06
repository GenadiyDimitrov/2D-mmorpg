using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Client
{
    /// <summary>
    /// `BL-371` stage 1 — THE ONE DOOR every non-core asset comes through: models, buildings, weapons, props, music,
    /// sounds, icons, the login art. Callback-shaped on purpose: an asset arrives LATER than it was asked for, and every
    /// caller is written to cope with that, so the switch to Addressables bundles (stage 2) and to download-on-demand
    /// (stage 3) changes the body of this class and nothing that calls it.
    ///
    /// <para>🔑 <b>The contract a caller must honour:</b> the callback may run AT ONCE (the asset is already cached) or
    /// some frames later, and it may hand over <c>null</c> (there is no such asset — the callers' fallbacks are the
    /// design, see <see cref="ModelLibrary"/>). Never assume either. A callback that touches a MonoBehaviour checks it
    /// is still alive: the window or body that asked may have been destroyed while the load ran.</para>
    ///
    /// <para>🔑 <b>The cache holds the MISSES too.</b> Until art lands most model keys miss, and a miss that is not
    /// remembered is a lookup per spawn, forever. Two requests for one key while it loads share the one load.</para>
    ///
    /// <para><b>Today's body is still <c>Resources</c></b>: a single asset through <c>Resources.LoadAsync</c> (genuinely
    /// later), a folder through <c>Resources.LoadAll</c> handed over on the next frame, so every playtest already runs
    /// the "arrives later" path the bundles will. Outside play mode (the editor's headless preview tools) everything is
    /// synchronous, since no player loop runs to finish an async load there.</para>
    ///
    /// <para>⚠ <b>Core stays OUT of here</b>, on <c>Resources</c>: the shaders (a missing shader is a pink world, and
    /// they are tiny) and <c>server.txt</c> (needed before anything else can happen). Everything else goes through this.</para>
    /// </summary>
    public static class GameAssets
    {
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

        /// <summary>One asset by its path (<c>"Models/humanoid"</c>, no extension). <paramref name="done"/> gets it, or
        /// null when there is none.</summary>
        public static void Load<T>(string path, Action<T> done) where T : Object
        {
            if (done == null) return;
            if (string.IsNullOrEmpty(path)) { done(null); return; }
            string key = "1|" + typeof(T).Name + "|" + path;
            Request(key, o => done(o as T), () =>
            {
                if (!Application.isPlaying) { Finish(key, Resources.Load<T>(path)); return; }
                var op = Resources.LoadAsync<T>(path);
                op.completed += _ => Finish(key, op.asset as T);
            });
        }

        /// <summary>Every asset of type <typeparamref name="T"/> in a folder (<c>"Sounds/crit"</c>). Never null: an empty or
        /// missing folder is an empty array.</summary>
        public static void LoadAll<T>(string folder, Action<T[]> done) where T : Object
        {
            if (done == null) return;
            string key = "*|" + typeof(T).Name + "|" + folder;
            Request(key, o => done(o as T[] ?? new T[0]), () =>
            {
                var all = Resources.LoadAll<T>(folder) ?? new T[0];
                if (!Application.isPlaying) { Finish(key, all); return; }
                Pump.Later(() => Finish(key, all));
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

        /// <summary>Runs a folder's hand-over on the next frame. A hidden object, made the first time it is needed.</summary>
        private sealed class Pump : MonoBehaviour
        {
            private static Pump _instance;
            private readonly List<Action> _queue = new List<Action>();
            private readonly List<Action> _running = new List<Action>();

            public static void Later(Action a)
            {
                if (_instance == null)
                {
                    var go = new GameObject("GameAssets") { hideFlags = HideFlags.HideAndDontSave };
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Pump>();
                }
                _instance._queue.Add(a);
            }

            private void Update()
            {
                if (_queue.Count == 0) return;
                _running.AddRange(_queue);
                _queue.Clear();
                foreach (var a in _running) a();
                _running.Clear();
            }
        }
    }
}
