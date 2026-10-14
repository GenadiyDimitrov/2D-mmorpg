using System;
using System.Collections.Generic;
using Game.Shared;
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

            // The second half of `87b`, and the other reason the SYSTEM tab is the expensive one: every
            // Info/Good/Warn line goes through Debug.Log (see AddTagged), and on a device Unity captures
            // a managed STACK TRACE for each one — chat lines deliberately do not, which is why no other
            // tab pays this. Nothing reads the trace of an informational line, so turn it off for those
            // two levels and keep it where it is the whole point: errors, exceptions and asserts, i.e.
            // the crash trail this buffer exists for.
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);

            Application.logMessageReceived += OnUnityLog;
        }

        /// 🔑 ONLY ERRORS (owner, 2026-10-14): *"[buildings] 106 placed … [UnlitMaterials] … those
        /// messages are not needed"*. Plain Debug.Log and warnings stay in logcat, where a developer reads
        /// them; the chat keeps only what a player could otherwise be left staring at a broken screen
        /// without — an exception, an error, a failed assert. That also retires `BL-313`'s per-glyph
        /// filter: TMP's missing-glyph line is a WARNING, so it no longer reaches the console at all.
        private static void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            // Our own Add() already routes through Debug.Log, so only pick up messages that did NOT
            // come from us (they are tagged) to avoid duplicating every line.
            if (condition != null && condition.StartsWith("[hud] ")) return;
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            Append(condition, new Color(1f, 0.45f, 0.45f));
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

        /// 🔴🔑 EVERY LINE IS FOLDED TO ASCII HERE, AND THIS IS WHERE THE FEEDBACK LOOP WAS CUT
        /// (§100, 2026-09-16). His report: *"the unicode character with value [] cannot be found in
        /// [liberation sans srf] … replaced with unicode character [] in text object [lable]"*, with an
        /// FPS drop, **and only in the System tab**.
        ///
        /// <para>The TMP atlas is STATIC, so a character outside it logs a warning ONCE PER FRAME PER
        /// LABEL. The System tab is special because this class hooks
        /// <c>Application.logMessageReceived</c>: TMP's own missing-glyph warning — which CONTAINS the
        /// offending character — was appended here, rendered by the console label, and logged again.
        /// One em dash in one system line became a self-feeding loop that grew a warning per frame.
        /// That is the FPS drop, and folding at this single sink stops it whatever the source: server
        /// prose, our own Info/Warn lines, or Unity's engine log.</para>
        ///
        /// <para>🔑 A FOLD, NOT A STRIP. <see cref="AsciiText.Fold"/> maps only the typographic set
        /// (em dash, U+2212, ×, …) and passes everything else through untouched, because the owner
        /// types BULGARIAN in chat and this sink carries chat. The server folds its own prose too, so
        /// this is the belt to that braces — and it is the half that also covers the client's own
        /// 120-odd em dashes and Unity's log.</para></summary>
        private static void Append(string text, Color color, Tab tab = Tab.System)
        {
            text = Game.Shared.AsciiText.Fold(text);
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
                // Only a GENERATION bump makes an append-only view throw
                // its drawn rows away, so a Revision bump alone would leave them on screen.
                Revision++; ClearGeneration++;
            }
        }

        // ===================== A FRESH CHAT ON EVERY ENTRY (owner, 2026-10-14) =====================
        //
        // *"after a server is started clean no chat log exists .. its cleared for every player … this must
        // be the chat of every player after each server restart or login -> otherwise the Shutdown flood
        // and any other stuff stays after restart"*. This REVERSES playtest 28 (*"chat again is saved
        // between logins. Don't reset"*), which filed each character's chat to disk and restored it on
        // entry: the newest ruling wins, and those per-character files are no longer read or written.
        //
        // Entering the world — a login, a character switch, the silent re-entry after a server restart —
        // clears EVERY tab, System included, and opens on these four lines. The server's own arrival
        // lines (your role, its privileges, the anti-phishing warning) follow them.

        /// <summary>Wipe the whole buffer and write the opening lines every session starts with.</summary>
        public static void StartSession()
        {
            Clear();
            Append(GameConstants.GameTitle, new Color(1f, 0.82f, 0.35f));
            Append("Welcome to the world of " + GameConstants.GameName + ".", new Color(0.85f, 0.9f, 1f));
            Append("Client version: " + GameConstants.GameVersion, new Color(0.85f, 0.9f, 1f));
            Append("Connected.", new Color(0.6f, 1f, 0.6f));
        }
    }
}
