using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// SignalR events fire on a background thread; UnityEngine APIs may only be touched on the
    /// main thread. Enqueue work here from any thread; it runs in Update() on the main thread.
    /// Put ONE of these in the scene (or it self-creates on first use).
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> _queue = new Queue<Action>();
        private static UnityMainThreadDispatcher _instance;

        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MainThreadDispatcher");
                    _instance = go.AddComponent<UnityMainThreadDispatcher>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null) _instance = this;
        }

        public void Enqueue(Action action)
        {
            if (action == null) return;
            lock (_queue) { _queue.Enqueue(action); }
        }

        private void Update()
        {
            // Drain OUTSIDE the lock so an enqueued action can safely enqueue more.
            while (true)
            {
                Action action = null;
                lock (_queue)
                {
                    if (_queue.Count > 0) action = _queue.Dequeue();
                }
                if (action == null) break;
                try { action(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }
    }
}
