using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// A tiny ring buffer of log lines, mirrored into the on-screen console.
    ///
    /// This exists because on a phone there IS no console: an exception in a SignalR callback, a
    /// refused connection, a version-mismatch rejection — all of it used to vanish into a logcat
    /// nobody was reading, which is exactly how you end up staring at a black screen with no idea
    /// whether the client even reached the server. It hooks Application.logMessageReceived, so
    /// plain Debug.Log from anywhere in the client shows up here too.
    /// </summary>
    public static class ClientLog
    {
        public struct Line
        {
            public string Text;
            public Color Color;
            /// <summary>Monotonic id, never reused. Lets the console APPEND only the lines it hasn't
            /// drawn yet instead of rebuilding the whole buffer every time one arrives.</summary>
            public long Seq;
        }

        private static long _seq;

        private const int Capacity = 200;
        private static readonly List<Line> _lines = new List<Line>(Capacity);
        private static bool _hooked;

        /// <summary>Bumped on every append so the console can auto-scroll only when something changed.</summary>
        public static int Revision { get; private set; }

        /// <summary>Bumped ONLY when the buffer is cleared. The console watches it to know when to throw
        /// away its rows and rebuild, versus just appending the new lines onto what it already drew.</summary>
        public static int ClearGeneration { get; private set; }

        public static IReadOnlyList<Line> Lines => _lines;

        public static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            Application.logMessageReceived += OnUnityLog;
        }

        private static void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            // Our own Add() already routes through Debug.Log, so only pick up messages that did NOT
            // come from us (they are tagged) to avoid duplicating every line.
            if (condition != null && condition.StartsWith("[hud] ")) return;

            Color c;
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:  c = new Color(1f, 0.45f, 0.45f); break;
                case LogType.Warning: c = new Color(1f, 0.85f, 0.4f); break;
                default:              c = new Color(0.85f, 0.85f, 0.85f); break;
            }
            Append(condition, c);
        }

        public static void Info(string text) => AddTagged(text, new Color(0.85f, 0.9f, 1f));
        public static void Good(string text) => AddTagged(text, new Color(0.6f, 1f, 0.6f));
        public static void Warn(string text) => AddTagged(text, new Color(1f, 0.85f, 0.4f));
        public static void Error(string text) => AddTagged(text, new Color(1f, 0.45f, 0.45f));

        /// <summary>Log to BOTH the on-screen console and Unity's log (so logcat / the Editor still
        /// have the full story), without the hook echoing it back as a second line.</summary>
        private static void AddTagged(string text, Color color)
        {
            Debug.Log("[hud] " + text);
            Append(text, color);
        }

        private static void Append(string text, Color color)
        {
            lock (_lines)
            {
                if (_lines.Count >= Capacity) _lines.RemoveAt(0);
                _lines.Add(new Line { Text = DateTime.Now.ToString("HH:mm:ss") + "  " + text, Color = color, Seq = _seq++ });
                Revision++;
            }
        }

        public static void Clear()
        {
            lock (_lines) { _lines.Clear(); Revision++; ClearGeneration++; }
        }
    }
}
