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
        /// <summary>
        /// Which tab of the chat window a line belongs to.
        ///
        /// The tabs are a FILTER over one buffer, not four buffers: a line is written once and every
        /// tab decides whether to draw it. That is what makes "All" free, and it keeps the ordering
        /// between channels intact — two buffers merged for display would have to be re-sorted, and
        /// the Seq that lets the console append instead of rebuild would stop being monotonic per tab.
        ///
        /// Everything that is not player chat (errors, warnings, server system lines) is
        /// <see cref="Tab.System"/> — the old, single-list console, now one tab of five.
        ///
        /// <see cref="Tab.Combat"/> (D5) is the exception that is NOT a tab of the chat window: the
        /// damage / loot / exp feed drowns everything else during a fight, which is the whole reason
        /// it was pulled out of System. It gets a window of its own and is excluded from "All", so
        /// the chat window can stay open next to it and still be readable.
        /// </summary>
        public enum Tab { System = 0, Local = 1, World = 2, Whisper = 3, Combat = 4 }

        public struct Line
        {
            public string Text;
            public Color Color;
            /// <summary>Monotonic id, never reused. Lets the console APPEND only the lines it hasn't
            /// drawn yet instead of rebuilding the whole buffer every time one arrives.</summary>
            public long Seq;
            public Tab Where;
        }

        private static long _seq;

        /// <summary>How many lines the buffer keeps. 1000 (owner, C1) rather than the old 200: the
        /// console only ever DRAWS <c>ConsoleDisplayRows</c> of them, so the cost of a deeper buffer is
        /// a few hundred strings, not rows — and 200 lines is under a minute of combat, which meant a
        /// death you wanted to read back was already gone by the time you opened the window.</summary>
        private const int Capacity = 1000;
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

            // Unity's own log — always System; nothing here is chat.
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

        /// <summary>A line of PLAYER chat, tabbed by channel. Not routed through Debug.Log: chat is not
        /// diagnostics, and mirroring every whisper into logcat is both noise and a small privacy leak
        /// on a shared phone.</summary>
        public static void Chat(string text, Color color, Tab tab) => Append(text, color, tab);

        /// <summary>Log to BOTH the on-screen console and Unity's log (so logcat / the Editor still
        /// have the full story), without the hook echoing it back as a second line.</summary>
        private static void AddTagged(string text, Color color)
        {
            Debug.Log("[hud] " + text);
            Append(text, color, Tab.System);
        }

        private static void Append(string text, Color color, Tab tab = Tab.System)
        {
            lock (_lines)
            {
                if (_lines.Count >= Capacity) _lines.RemoveAt(0);
                _lines.Add(new Line
                {
                    Text = DateTime.Now.ToString("HH:mm:ss") + "  " + text,
                    Color = color, Seq = _seq++, Where = tab,
                });
                Revision++;
            }
        }

        public static void Clear()
        {
            lock (_lines) { _lines.Clear(); Revision++; ClearGeneration++; }
        }

        /// <summary>Drop one tab's lines and leave the rest alone — the Combat window's Clear (D5).
        /// Its own Clear must not take the chat with it, and the chat window's Clear is still the
        /// full <see cref="Clear"/>.</summary>
        public static void ClearTab(Tab tab)
        {
            lock (_lines)
            {
                if (_lines.RemoveAll(l => l.Where == tab) == 0) return;
                // Same reasoning as ClearChat: only a GENERATION bump makes an append-only view throw
                // its drawn rows away, so a Revision bump alone would leave them on screen.
                Revision++; ClearGeneration++;
            }
        }

        /// <summary>
        /// Drop the PLAYER-CHAT lines and keep the System tab (C1).
        ///
        /// Called when you leave the world, so the next character never opens onto the last one's
        /// conversation — a deleted character's chat log showing up under a freshly created one is
        /// what got this reported. The System tab is deliberately spared: it is the crash trail this
        /// whole buffer exists for (an exception in a SignalR callback, a refused connection), it is
        /// not per-character, and wiping it on every relog would throw away the diagnostics for the
        /// relog itself — which is exactly when they are wanted.
        /// </summary>
        public static void ClearChat()
        {
            lock (_lines)
            {
                int removed = _lines.RemoveAll(l => l.Where != Tab.System);
                if (removed == 0) return;
                // ClearGeneration, not just Revision: the console draws APPEND-ONLY and only a
                // generation bump makes it throw its rows away and redraw from the buffer. Bumping
                // Revision alone would leave the removed lines on screen until the next Clear.
                Revision++; ClearGeneration++;
            }
        }
    }
}
