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

        // ===================== CHAT THAT SURVIVES A RELOG (playtest 28) =====================
        //
        // 🔑 HIS TWO RULINGS ARE NOT IN CONFLICT, THEY ARE THE SAME RULE. C1 (playtest 17) said *"chat
        // must reset on exit"* because a newly created character opened onto a DELETED character's
        // conversation. Playtest 28 says *"chat again is saved between logins. Don't reset"*. What he
        // wanted both times is that the chat belongs to the CHARACTER — the first report was it leaking
        // ACROSS characters, the second is it being thrown away WITHIN one.
        //
        // So the buffer is now filed per character instead of wiped: leaving the world stores the chat
        // under whoever was talking, entering it restores that character's own and nobody else's. A
        // character that never spoke restores nothing, which is what a fresh character sees.
        //
        // It goes to DISK, not just memory. "Between logins" on a phone includes the app being killed
        // in the background, and an in-memory stash would quietly not survive the one case he is most
        // likely to hit.
        //
        // The System tab is still never stored and never restored: it is the diagnostics trail, it is
        // not per-character, and writing every logcat line of a session to disk on exit is a cost with
        // no reader.

        /// <summary>Which character's chat is currently in the buffer (empty = none / the login
        /// screen). Kept so <see cref="SwitchCharacter"/> knows what file to write on the way out.</summary>
        private static string _chatOwner = "";

        /// <summary>How many chat lines are carried across a relog. Well under <see cref="Capacity"/>
        /// on purpose: this is "what were we just talking about", not an archive, and the file is read
        /// and written on the main thread at two moments where a stall would be felt.</summary>
        private const int PersistedChatLines = 300;

        /// <summary>Hand the chat buffer to a different character (or to nobody — pass an empty key
        /// when leaving for the character screen).
        ///
        /// Saves the outgoing character's chat, clears the buffer, then loads the incoming one's.
        /// Calling it twice with the same key is a no-op, so the login path can call it freely.</summary>
        public static void SwitchCharacter(string characterKey)
        {
            characterKey = characterKey ?? "";
            if (characterKey == _chatOwner) return;

            SaveChat(_chatOwner);
            ClearChat();
            _chatOwner = characterKey;
            LoadChat(characterKey);
        }

        /// <summary>Write the current character's chat lines out. Safe to call with no owner.</summary>
        public static void SaveChat(string characterKey)
        {
            if (string.IsNullOrEmpty(characterKey)) return;
            try
            {
                var sb = new System.Text.StringBuilder();
                lock (_lines)
                {
                    int from = 0;
                    int chat = 0;
                    for (int i = _lines.Count - 1; i >= 0; i--)
                        if (_lines[i].Where != Tab.System && ++chat >= PersistedChatLines) { from = i; break; }
                    for (int i = from; i < _lines.Count; i++)
                    {
                        var l = _lines[i];
                        if (l.Where == Tab.System) continue;
                        // tab|r|g|b|text — the text is last, so it may contain anything but a newline
                        // (and it cannot: every line is one Append).
                        sb.Append((int)l.Where).Append('|')
                          .Append(l.Color.r.ToString("0.###")).Append('|')
                          .Append(l.Color.g.ToString("0.###")).Append('|')
                          .Append(l.Color.b.ToString("0.###")).Append('|')
                          .Append(l.Text.Replace('\n', ' ')).Append('\n');
                    }
                }
                System.IO.File.WriteAllText(ChatFilePath(characterKey), sb.ToString());
            }
            catch (Exception e)
            {
                // Never let a storage problem take the client down — the chat log is a convenience.
                Debug.LogWarning("[hud] chat save failed: " + e.Message);
            }
        }

        private static void LoadChat(string characterKey)
        {
            if (string.IsNullOrEmpty(characterKey)) return;
            try
            {
                string path = ChatFilePath(characterKey);
                if (!System.IO.File.Exists(path)) return;
                var restored = new List<Line>();
                foreach (var raw in System.IO.File.ReadAllLines(path))
                {
                    if (string.IsNullOrEmpty(raw)) continue;
                    var parts = raw.Split(new[] { '|' }, 5);
                    if (parts.Length < 5) continue;
                    int tab; float r, g, b;
                    if (!int.TryParse(parts[0], out tab)) continue;
                    float.TryParse(parts[1], out r);
                    float.TryParse(parts[2], out g);
                    float.TryParse(parts[3], out b);
                    restored.Add(new Line { Text = parts[4], Color = new Color(r, g, b), Where = (Tab)tab });
                }
                if (restored.Count == 0) return;

                lock (_lines)
                {
                    // The restored lines go in FRONT of whatever the current session already logged
                    // (the connect/handshake System lines), because they are older. Seq is re-stamped
                    // in order so the console's append-only draw still sees a monotonic sequence.
                    var merged = new List<Line>(restored.Count + _lines.Count);
                    merged.AddRange(restored);
                    merged.AddRange(_lines);
                    _lines.Clear();
                    _seq = 0;
                    for (int i = 0; i < merged.Count; i++)
                    {
                        var l = merged[i];
                        l.Seq = _seq++;
                        _lines.Add(l);
                    }
                    while (_lines.Count > Capacity) _lines.RemoveAt(0);
                    Revision++; ClearGeneration++;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[hud] chat load failed: " + e.Message);
            }
        }

        /// <summary>One file per character, named by a sanitised key so a character name can never
        /// walk out of the folder or collide with another file the client keeps.</summary>
        private static string ChatFilePath(string characterKey)
        {
            var safe = new System.Text.StringBuilder(characterKey.Length);
            foreach (char c in characterKey)
                safe.Append(char.IsLetterOrDigit(c) ? c : '_');
            return System.IO.Path.Combine(Application.persistentDataPath, "chat_" + safe + ".log");
        }
    }
}
