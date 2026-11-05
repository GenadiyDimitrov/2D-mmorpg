using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Client
{
    /// <summary>One key plus the modifiers that must be held with it (Shift+1 is a different key from 1).</summary>
    public struct KeyBind : IEquatable<KeyBind>
    {
        public KeyCode Key;
        public bool Shift, Ctrl, Alt;

        public KeyBind(KeyCode key, bool shift = false, bool ctrl = false, bool alt = false)
        { Key = key; Shift = shift; Ctrl = ctrl; Alt = alt; }

        public bool IsNone => Key == KeyCode.None;
        public static readonly KeyBind None = new KeyBind(KeyCode.None);

        public bool Equals(KeyBind o) => Key == o.Key && Shift == o.Shift && Ctrl == o.Ctrl && Alt == o.Alt;
        public override bool Equals(object obj) => obj is KeyBind o && Equals(o);
        public override int GetHashCode() => ((int)Key << 3) | (Shift ? 1 : 0) | (Ctrl ? 2 : 0) | (Alt ? 4 : 0);

        /// <summary>Stored form, "Ctrl+Shift+Alpha1" — KeyCode names, so a saved file survives a reordering.</summary>
        public string Save() => IsNone ? "" : (Ctrl ? "Ctrl+" : "") + (Shift ? "Shift+" : "") + (Alt ? "Alt+" : "") + Key;

        public static KeyBind Load(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return None;
            var b = new KeyBind();
            foreach (var part in text.Split('+'))
            {
                if (part == "Ctrl") b.Ctrl = true;
                else if (part == "Shift") b.Shift = true;
                else if (part == "Alt") b.Alt = true;
                else if (Enum.TryParse(part, out KeyCode k)) b.Key = k;
            }
            return b;
        }

        /// <summary>What the key box shows: "Shift+1", "Ctrl+PgDn", "W", "Up".</summary>
        public string Display()
        {
            if (IsNone) return "—";
            return (Ctrl ? "Ctrl+" : "") + (Shift ? "Shift+" : "") + (Alt ? "Alt+" : "") + KeyName(Key);
        }

        private static string KeyName(KeyCode k)
        {
            if (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9) return ((int)(k - KeyCode.Alpha0)).ToString();
            if (k >= KeyCode.Keypad0 && k <= KeyCode.Keypad9) return "Num" + (int)(k - KeyCode.Keypad0);
            switch (k)
            {
                case KeyCode.UpArrow: return "Up";
                case KeyCode.DownArrow: return "Down";
                case KeyCode.LeftArrow: return "Left";
                case KeyCode.RightArrow: return "Right";
                case KeyCode.PageUp: return "PgUp";
                case KeyCode.PageDown: return "PgDn";
                case KeyCode.Return: return "Enter";
                case KeyCode.KeypadEnter: return "NumEnter";
                case KeyCode.BackQuote: return "`";
                case KeyCode.Minus: return "-";
                case KeyCode.Equals: return "=";
                case KeyCode.LeftBracket: return "[";
                case KeyCode.RightBracket: return "]";
                case KeyCode.Semicolon: return ";";
                case KeyCode.Quote: return "'";
                case KeyCode.Comma: return ",";
                case KeyCode.Period: return ".";
                case KeyCode.Slash: return "/";
                case KeyCode.Backslash: return "\\";
                case KeyCode.Mouse2: return "Mouse3";
                case KeyCode.Mouse3: return "Mouse4";
                case KeyCode.Mouse4: return "Mouse5";
                default: return k.ToString();
            }
        }

        public static bool ShiftHeld => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        public static bool CtrlHeld => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        public static bool AltHeld => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        /// <summary>Pressed this frame with EXACTLY these modifiers — so 1 does not fire under Shift+1.</summary>
        public bool Down() => !IsNone && Input.GetKeyDown(Key)
                              && Shift == ShiftHeld && Ctrl == CtrlHeld && Alt == AltHeld;

        /// <summary>Held now, with at least its own modifiers (a held move key keeps walking if you also press Shift).</summary>
        public bool Held() => !IsNone && Input.GetKey(Key)
                              && (!Shift || ShiftHeld) && (!Ctrl || CtrlHeld) && (!Alt || AltHeld);
    }

    /// <summary>
    /// `BL-364` item 1 — EVERY desktop key, rebindable (owner: *"I want to be able to change the shortcuts of hotbar (main +
    /// additional) - as well as arrow/wasd keys for move"*). One action = two key slots (so WASD and the arrows both
    /// move). Saved per device in PlayerPrefs under <c>keys.&lt;id&gt;</c>; an action never saved reads its default, so
    /// a new action added later arrives bound. Esc is NOT here: it is the phone's Back button and must always work.
    /// </summary>
    public static class KeyBindings
    {
        public sealed class KeyAction
        {
            public string Id, Name, Group;
            public KeyBind[] Defaults;
            public KeyBind[] Keys = new KeyBind[2];
        }

        public const string PrefPrefix = "keys.";
        public const int BarSlots = 10;

        /// <summary>Every action, in the order the Keys window lists them.</summary>
        public static readonly List<KeyAction> All = new List<KeyAction>();
        private static readonly Dictionary<string, KeyAction> ById = new Dictionary<string, KeyAction>();

        public const string MoveForward = "move.fwd", MoveBack = "move.back", MoveLeft = "move.left", MoveRight = "move.right";
        public const string PageNext = "bar.page.next", PagePrev = "bar.page.prev";
        public const string Bag = "win.bag", Character = "win.char", Skills = "win.skills", Quests = "win.quests",
                            Menu = "win.menu", Chat = "chat";

        public static string BarSlot(int bar, int index) => "bar" + bar + "." + index;

        static KeyBindings()
        {
            Add(MoveForward, "Move forward", "Movement", new KeyBind(KeyCode.W), new KeyBind(KeyCode.UpArrow));
            Add(MoveBack, "Move back", "Movement", new KeyBind(KeyCode.S), new KeyBind(KeyCode.DownArrow));
            Add(MoveLeft, "Move left", "Movement", new KeyBind(KeyCode.A), new KeyBind(KeyCode.LeftArrow));
            Add(MoveRight, "Move right", "Movement", new KeyBind(KeyCode.D), new KeyBind(KeyCode.RightArrow));

            // A browser takes Ctrl+1…9 for its own tabs before the page ever sees them, so there the third bar defaults
            // to Alt+digit. Windows keeps 0.265.0's Ctrl+digit.
            bool web = Application.platform == RuntimePlatform.WebGLPlayer;
            string[] barNames = { "Main bar", "Extra bar 1", "Extra bar 2" };
            for (int bar = 0; bar < 3; bar++)
                for (int n = 0; n < BarSlots; n++)
                {
                    var key = n == 9 ? KeyCode.Alpha0 : KeyCode.Alpha1 + n;
                    var bind = bar == 0 ? new KeyBind(key)
                             : bar == 1 ? new KeyBind(key, shift: true)
                             : web ? new KeyBind(key, alt: true) : new KeyBind(key, ctrl: true);
                    Add(BarSlot(bar, n), barNames[bar] + " — square " + (n + 1), barNames[bar], bind);
                }
            Add(PageNext, "Next bar page", "Skill bar", new KeyBind(KeyCode.PageDown));
            Add(PagePrev, "Previous bar page", "Skill bar", new KeyBind(KeyCode.PageUp));

            Add(Bag, "Bag", "Windows", new KeyBind(KeyCode.B));
            Add(Character, "Character", "Windows", new KeyBind(KeyCode.C));
            Add(Skills, "Skills", "Windows", new KeyBind(KeyCode.K));
            Add(Quests, "Quests", "Windows", new KeyBind(KeyCode.L));
            Add(Menu, "Menu", "Windows", new KeyBind(KeyCode.M));
            Add(Chat, "Type in chat", "Windows", new KeyBind(KeyCode.Return), new KeyBind(KeyCode.KeypadEnter));

            foreach (var a in All) LoadOne(a);
        }

        private static void Add(string id, string name, string group, KeyBind first, KeyBind second = default)
        {
            var a = new KeyAction { Id = id, Name = name, Group = group, Defaults = new[] { first, second } };
            All.Add(a);
            ById[id] = a;
        }

        private static void LoadOne(KeyAction a)
        {
            string saved = PlayerPrefs.GetString(PrefPrefix + a.Id, null);
            if (saved == null) { a.Keys[0] = a.Defaults[0]; a.Keys[1] = a.Defaults[1]; return; }
            var parts = saved.Split('|');
            a.Keys[0] = KeyBind.Load(parts.Length > 0 ? parts[0] : "");
            a.Keys[1] = KeyBind.Load(parts.Length > 1 ? parts[1] : "");
        }

        private static void SaveOne(KeyAction a) =>
            PlayerPrefs.SetString(PrefPrefix + a.Id, a.Keys[0].Save() + "|" + a.Keys[1].Save());

        public static KeyAction Get(string id) => ById.TryGetValue(id, out var a) ? a : null;

        public static bool Down(string id)
        {
            var a = Get(id);
            return a != null && (a.Keys[0].Down() || a.Keys[1].Down());
        }

        public static bool Held(string id)
        {
            var a = Get(id);
            return a != null && (a.Keys[0].Held() || a.Keys[1].Held());
        }

        /// <summary>Bind <paramref name="bind"/> to slot <paramref name="slot"/> of <paramref name="action"/>. A key already
        /// used elsewhere is taken from there (owner's list: *"a key already bound elsewhere is unbound from there, and
        /// you are told"*) — the return value names what lost it, or null.</summary>
        public static string Bind(KeyAction action, int slot, KeyBind bind)
        {
            string taken = null;
            if (!bind.IsNone)
                foreach (var other in All)
                    for (int s = 0; s < 2; s++)
                    {
                        if (other == action && s == slot) continue;
                        if (!other.Keys[s].Equals(bind)) continue;
                        other.Keys[s] = KeyBind.None;
                        SaveOne(other);
                        taken = other.Name;
                    }
            action.Keys[slot] = bind;
            SaveOne(action);
            return taken;
        }

        public static void ResetAll()
        {
            foreach (var a in All)
            {
                PlayerPrefs.DeleteKey(PrefPrefix + a.Id);
                a.Keys[0] = a.Defaults[0];
                a.Keys[1] = a.Defaults[1];
            }
            PlayerPrefs.Save();
        }

        /// <summary>The key pressed THIS frame, for the rebind box — any keyboard key or a side mouse button; null if none.
        /// Modifiers alone are not keys (they are read as part of the bind), and the left/right mouse buttons are
        /// excluded because they are how you click the box.</summary>
        public static KeyCode? PressedThisFrame()
        {
            if (!Input.anyKeyDown) return null;
            foreach (var k in Capturable)
                if (Input.GetKeyDown(k)) return k;
            return null;
        }

        private static readonly KeyCode[] Capturable = BuildCapturable();

        private static KeyCode[] BuildCapturable()
        {
            var list = new List<KeyCode>();
            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
            {
                if (k == KeyCode.None || k == KeyCode.Mouse0 || k == KeyCode.Mouse1) continue;
                if (k >= KeyCode.JoystickButton0) continue;   // every joystick code sits above this one
                if (k == KeyCode.LeftShift || k == KeyCode.RightShift || k == KeyCode.LeftControl
                    || k == KeyCode.RightControl || k == KeyCode.LeftAlt || k == KeyCode.RightAlt
                    || k == KeyCode.AltGr || k == KeyCode.LeftCommand || k == KeyCode.RightCommand
                    || k == KeyCode.LeftWindows || k == KeyCode.RightWindows) continue;
                if (!list.Contains(k)) list.Add(k);   // aliased enum values (LeftApple = LeftCommand) appear twice
            }
            return list.ToArray();
        }
    }
}
