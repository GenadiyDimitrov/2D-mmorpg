using System.Collections.Generic;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the things that tell you what is happening RIGHT NOW — the cast bar,
    /// floating damage numbers, level-coloured names and the zone readout.
    ///
    /// Without these the game is silent: you press a skill and nothing visibly happens until an HP bar
    /// twitches, and you cannot tell a level-3 rat from a level-70 one until it kills you.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        // cast bar
        private RectTransform _castPanel;
        private TextMeshProUGUI _castLabel;
        private Image _castFill;

        // zone readout
        private TextMeshProUGUI _zoneLabel;

        // floating damage
        private class FloatingNumber
        {
            public RectTransform Root;
            public TextMeshProUGUI Label;
            public Vector3 World;
            public float BornAt;
        }

        private readonly List<FloatingNumber> _floaters = new List<FloatingNumber>();
        private const float FloatSeconds = 1.1f;
        private const float FloatRisePixels = 70f;

        private void BuildFeedback()
        {
            // Cast bar: centred low, where a phone's thumbs are not covering it.
            _castPanel = UiKit.PanelBox(_worldRoot, "CastBar");
            UiKit.Place(_castPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 140f), new Vector2(420f, 44f));
            var inner = _castPanel.GetChild(0);

            _castFill = UiKit.ValueBar(inner, UiKit.Accent);
            UiKit.Stretch(UiKit.Rect(_castFill.transform.parent.gameObject), 8f, 22f, 8f, 8f);

            _castLabel = UiKit.Label(inner, "", 16f, UiKit.Text, TextAlignmentOptions.Center);
            UiKit.Place(UiKit.Rect(_castLabel.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -3f), new Vector2(400f, 20f));

            // THE WHOLE BAR IS THE CANCEL BUTTON.
            //
            // Android has no ESC key, and a small "Stop" button next to a 44px bar is a poor target
            // for a thumb during the two seconds it exists. Mobile MMOs overwhelmingly make the cast
            // bar itself the cancel target: it is large, it is centred where a thumb already rests,
            // and it only exists while there is something to cancel. The label stays so it is
            // obviously pressable rather than a mystery.
            var cancel = _castPanel.gameObject.AddComponent<Button>();
            cancel.targetGraphic = _castPanel.GetComponent<Image>();
            cancel.onClick.AddListener(() => Boot.CancelCast());

            var hint = UiKit.Label(inner, "tap to cancel", 13f, UiKit.TextDim, TextAlignmentOptions.Right);
            UiKit.Place(UiKit.Rect(hint.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-10f, -3f), new Vector2(140f, 18f));

            _castPanel.gameObject.SetActive(false);

            // "You entered X" — a banner across the middle of the screen that FADES, not a permanent
            // readout.
            //
            // It used to be a label pinned under the self panel, which cost a line of the HUD forever
            // to answer a question you ask when you cross a border and never again. The owner's rule
            // for the top-left corner is vitals only; everything else is either transient or lives in
            // a window.
            _zoneLabel = UiKit.Label(_worldRoot, "", 26f, UiKit.TextDim, TextAlignmentOptions.Center);
            UiKit.Place(UiKit.Rect(_zoneLabel.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -110f), new Vector2(900f, 40f));
            _zoneLabel.outlineColor = new Color32(0, 0, 0, 220);
            _zoneLabel.outlineWidth = 0.2f;

            BuildBuffBar();
        }

        /// <summary>
        /// Subscribed on the first frame that HAS a Boot, not while building.
        ///
        /// Awake runs the instant AddComponent is called — before GameBoot assigns itself to Boot — so
        /// `Boot.CombatHappened += …` during the build threw a NullReferenceException, which aborted
        /// Awake halfway and left the UI PARTLY CONSTRUCTED: panels that had already been created were
        /// on screen, everything after the throw did not exist, and the phase switching then had
        /// nothing coherent to show or hide. On the device that reads as "the game froze".
        /// </summary>
        private void HookFeedback()
        {
            Boot.CombatHappened -= SpawnDamageNumber;   // idempotent: no double numbers on a re-hook
            Boot.CombatHappened += SpawnDamageNumber;
        }

        private void UnhookFeedback()
        {
            if (Boot != null) Boot.CombatHappened -= SpawnDamageNumber;
        }

        private void RefreshFeedback()
        {
            RefreshCastBar();
            RefreshZoneLabel();
            RefreshFloaters();
            RefreshBuffBar();
        }

        // ----- buff bar ---------------------------------------------------------------------------

        private RectTransform _buffBar;
        private Button _buffCollapse;

        /// <summary>
        /// Hide has THREE stages, not two (owner, 2026-07-22):
        ///   0 Shown     — every group, every row.
        ///   1 OneRow    — one row per group, the rest counted on the button ("+3").
        ///   2 Hidden    — no squares at all, just the total ("9+").
        /// Debuffs are exempt from ALL of it and always draw in full.
        /// </summary>
        private int _buffStage;

        private class BuffSquare
        {
            public RectTransform Root;
            public Image Box;
            public TextMeshProUGUI Label, Time;
            public string Key;          // the buff this square currently SHOWS
            public bool IsDebuff;
        }

        private readonly List<BuffSquare> _buffSquares = new List<BuffSquare>();

        private const int BuffsPerRow = 6;
        private const float BuffSize = 44f, BuffStep = 47f, BuffRowStep = 50f;

        private void BuildBuffBar()
        {
            // Docked directly under the self panel (which ends at -164) instead of floating in the
            // middle of the world, where it covered the map and belonged to nothing.
            _buffBar = UiKit.Rect(UiKit.Box(_worldRoot, "BuffBar", new Color(0, 0, 0, 0),
                                            blocksInput: false).gameObject);
            UiKit.Place(_buffBar, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -170f), new Vector2(360f, 220f));

            _buffCollapse = UiKit.TextButton(_worldRoot, "", () => _buffStage = (_buffStage + 1) % 3, 13f);
            UiKit.Place(UiKit.Rect(_buffCollapse.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(300f, -170f), new Vector2(58f, 24f));
        }

        /// <summary>
        /// Buffs as SQUARES with the remaining time under them. Debuffs are tinted red so a poison is
        /// not mistaken for a blessing at a glance, and a buff blinks under 60s so you have warning
        /// before it drops mid-fight. Tapping a BUFF cancels it; tapping a debuff does nothing,
        /// because being able to click away a poison would make debuffs pointless.
        ///
        /// Layout, per the owner: SIX per row, wrapping, packed tight. Buffs collapse to a single row
        /// (a level-90 character carries nine and they ran the width of the screen at three letters
        /// each). **Debuffs wrap the same way but never collapse** — a hidden debuff is a fight you
        /// lose without knowing why, so they are always fully visible, and they are drawn FIRST so
        /// they hold the same place whether the buffs below are open or shut.
        ///
        /// Each square remembers the buff KEY it is showing. The old version captured a list INDEX in
        /// its click handler, which stops being the same buff the moment one expires — cancelling one
        /// buff could remove a different one.
        /// </summary>
        private void RefreshBuffBar()
        {
            var all = Boot.Buffs ?? new BuffDto[0];

            // FOUR groups, from the BuffRow the server has been sending all along — the client was
            // splitting on IsDebuff alone and lumping everything else together, which is why a health
            // potion's effect disappeared under the buff Hide button. A potion is not a buff you cast;
            // hiding one to tidy the other is wrong.
            var debuffs = new List<BuffDto>();
            var buffs = new List<BuffDto>();
            var items = new List<BuffDto>();
            var consumables = new List<BuffDto>();

            foreach (var b in all)
            {
                if (b.IsDebuff || b.Row == BuffRow.Debuff) debuffs.Add(b);
                else if (b.Row == BuffRow.Item) items.Add(b);
                else if (b.Row == BuffRow.Consumable) consumables.Add(b);
                else buffs.Add(b);
            }

            int used = 0;
            float y = 0f;

            // Debuffs FIRST and never hidden. First because they then hold the same place whether the
            // groups below are open or shut; never hidden because a debuff you cannot see is a fight
            // you lose without knowing why.
            y = LayoutBuffRow(debuffs, ref used, y, collapsible: false);
            int debuffRows = debuffs.Count == 0 ? 0 : ((debuffs.Count - 1) / BuffsPerRow) + 1;

            y = LayoutBuffRow(buffs, ref used, y, collapsible: true);
            y = LayoutBuffRow(items, ref used, y, collapsible: true);
            y = LayoutBuffRow(consumables, ref used, y, collapsible: true);

            int hideable = buffs.Count + items.Count + consumables.Count;
            int visible = _buffStage == 0 ? hideable
                        : _buffStage == 1 ? Mathf.Min(buffs.Count, BuffsPerRow)
                                          + Mathf.Min(items.Count, BuffsPerRow)
                                          + Mathf.Min(consumables.Count, BuffsPerRow)
                        : 0;

            _buffCollapse.gameObject.SetActive(hideable > 0);
            UiKit.SetButtonText(_buffCollapse,
                _buffStage == 0 ? "hide" : "+" + (hideable - visible));

            // WHERE the button sits (owner, 2026-07-22): normally at the END of the first buff row,
            // where it started — it belongs to the thing it hides, and chasing the bottom of a list
            // that changes height meant it moved every time a buff expired. When EVERYTHING is hidden
            // there is no row to sit at the end of, so it takes the FIRST buff's place instead: the
            // squares are gone, and the button is exactly where you last saw them.
            float firstBuffRowY = -170f - debuffRows * BuffRowStep;
            UiKit.Rect(_buffCollapse.gameObject).anchoredPosition = _buffStage == 2
                ? new Vector2(12f, firstBuffRowY)
                : new Vector2(12f + BuffsPerRow * BuffStep, firstBuffRowY);

            for (int i = used; i < _buffSquares.Count; i++)
                _buffSquares[i].Root.gameObject.SetActive(false);
        }

        /// <summary>Lay one group out six-per-row from <paramref name="y"/> downward, and return the y
        /// the next group starts at.</summary>
        private float LayoutBuffRow(List<BuffDto> group, ref int used, float y, bool collapsible)
        {
            int shown = !collapsible ? group.Count
                      : _buffStage == 0 ? group.Count
                      : _buffStage == 1 ? Mathf.Min(group.Count, BuffsPerRow)
                                        : 0;

            for (int i = 0; i < shown; i++)
            {
                var square = SquareAt(used++);
                var buff = group[i];

                square.Root.anchoredPosition = new Vector2((i % BuffsPerRow) * BuffStep,
                                                           y - (i / BuffsPerRow) * BuffRowStep);
                square.Root.gameObject.SetActive(true);
                square.Key = buff.Key;
                square.IsDebuff = buff.IsDebuff;

                square.Label.text = Abbreviations.For(buff.Name) + (buff.Stacks > 1 ? " x" + buff.Stacks : "");
                square.Time.text = ShortTime(buff.SecondsLeft);

                var tint = buff.IsDebuff ? new Color(0.45f, 0.18f, 0.18f, 0.95f) : UiKit.PanelLight;

                // Under a minute, blink — a buff that expires mid-fight should not be a surprise.
                if (!buff.IsDebuff && buff.SecondsLeft > 0f && buff.SecondsLeft <= 60f
                    && Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f)
                    tint = new Color(0.50f, 0.42f, 0.15f, 0.95f);

                square.Box.color = tint;
            }

            if (shown == 0) return y;
            return y - (((shown - 1) / BuffsPerRow) + 1) * BuffRowStep;
        }

        private BuffSquare SquareAt(int index)
        {
            while (_buffSquares.Count <= index)
            {
                var box = UiKit.Box(_buffBar, "Buff", UiKit.PanelLight);
                UiKit.Place(UiKit.Rect(box.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            Vector2.zero, new Vector2(BuffSize, BuffSize));

                var square = new BuffSquare { Root = UiKit.Rect(box.gameObject), Box = box };

                var button = box.gameObject.AddComponent<Button>();
                button.targetGraphic = box;
                // Closes over the SQUARE, not over a position in a list that reshuffles every time a
                // buff expires.
                button.onClick.AddListener(() =>
                {
                    if (square.IsDebuff || string.IsNullOrEmpty(square.Key)) return;
                    Boot.RemoveBuff(square.Key);
                });

                square.Label = UiKit.Label(box.transform, "", 14f, UiKit.Text, TextAlignmentOptions.Center);
                UiKit.Place(UiKit.Rect(square.Label.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(0f, -3f), new Vector2(42f, 20f));

                square.Time = UiKit.Label(box.transform, "", 11f, UiKit.TextDim, TextAlignmentOptions.Center);
                UiKit.Place(UiKit.Rect(square.Time.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                            new Vector2(0f, 3f), new Vector2(42f, 14f));

                _buffSquares.Add(square);
            }
            return _buffSquares[index];
        }

        /// <summary>Compact durations: "1h02", "12m", "45s". A buff bar with "3600.0 seconds" on it is
        /// unreadable at a glance, and a glance is all it ever gets.</summary>
        private static string ShortTime(float seconds)
        {
            if (seconds <= 0f) return "";
            if (seconds >= 3600f) return Mathf.FloorToInt(seconds / 3600f) + "h"
                                       + Mathf.FloorToInt(seconds % 3600f / 60f).ToString("00");
            if (seconds >= 60f) return Mathf.FloorToInt(seconds / 60f) + "m";
            return Mathf.CeilToInt(seconds) + "s";
        }

        private void RefreshCastBar()
        {
            bool casting = !string.IsNullOrEmpty(Boot.CastingSkill)
                           && Time.realtimeSinceStartup < Boot.CastEndsAt;
            _castPanel.gameObject.SetActive(casting);
            if (!casting) return;

            float total = Mathf.Max(0.01f, Boot.CastEndsAt - Boot.CastStartedAt);
            float done = Time.realtimeSinceStartup - Boot.CastStartedAt;
            UiKit.SetBar(_castFill, done, total);
            _castLabel.text = Boot.CastingSkill + "   " + Mathf.Max(0f, total - done).ToString("0.0") + "s";
        }

        private string _zoneShown = "";
        private float _zoneShownAt = -99f;
        private const float ZoneBannerSeconds = 3.5f;

        /// <summary>
        /// Announce the ground you just walked onto, once, then fade.
        ///
        /// The name is worked out from WorldMap — the same data the server spawns from, so the client
        /// cannot claim you are somewhere the server disagrees with. Only a CHANGE is announced; the
        /// banner then fades over <see cref="ZoneBannerSeconds"/> and the screen goes back to being
        /// the game.
        ///
        /// 🔗 When Regions ship on both clients this becomes RegionMap.At() for the name and
        /// RegionMap.LevelBand() for the "Lv 20-80" — both already exist server-side. That is why the
        /// band is composed here rather than hand-written into a string per zone.
        /// </summary>
        private void RefreshZoneLabel()
        {
            EntityDto self = null;
            if (Boot.Entities != null) Boot.Entities.TryGetState(Boot.SelfId, out self);
            if (self == null) return;

            string name;
            Color colour;

            var town = WorldMap.SafeZoneAt(self.X, self.Y);
            if (town != null)
            {
                name = town.Name + "   (safe zone)";
                colour = new Color(0.55f, 0.75f, 1f);
            }
            else
            {
                name = null;
                colour = UiKit.TextDim;
                foreach (var zone in WorldMap.SpawnZones)
                {
                    float dx = self.X - zone.X, dy = self.Y - zone.Y;
                    if (dx * dx + dy * dy > zone.Radius * zone.Radius) continue;

                    name = "Hunting ground   Lv " + zone.MinLevel + "-" + zone.MaxLevel;
                    colour = LevelColour(zone.MaxLevel, self.Level);
                    break;
                }
                if (name == null) name = "Wilds";
            }

            if (name != _zoneShown)
            {
                _zoneShown = name;
                _zoneShownAt = Time.unscaledTime;
                _zoneLabel.text = "You entered " + name;
                _zoneLabel.color = colour;
            }

            // Hold at full strength for the first half, then fade — long enough to read while walking,
            // short enough that it is gone before it becomes furniture.
            float age = (Time.unscaledTime - _zoneShownAt) / ZoneBannerSeconds;
            var faded = _zoneLabel.color;
            faded.a = age >= 1f ? 0f : Mathf.Clamp01((1f - age) * 2f);
            _zoneLabel.color = faded;
        }

        /// <summary>
        /// How dangerous something is RELATIVE to you, L2-style: far above → red, above → orange,
        /// even → white, below → green, trivial → grey. Absolute level means little; the gap is what
        /// decides whether you can take it.
        /// </summary>
        internal static Color LevelColour(int targetLevel, int myLevel)
        {
            if (myLevel <= 0 || targetLevel <= 0) return UiKit.Text;
            int gap = targetLevel - myLevel;
            if (gap >= 9)  return new Color(1.00f, 0.30f, 0.30f);   // red — will kill you
            if (gap >= 5)  return new Color(1.00f, 0.55f, 0.25f);   // orange
            if (gap >= 2)  return new Color(1.00f, 0.85f, 0.35f);   // yellow
            if (gap >= -2) return new Color(0.92f, 0.94f, 0.96f);   // white — an even fight
            if (gap >= -8) return new Color(0.55f, 0.90f, 0.55f);   // green
            return new Color(0.60f, 0.63f, 0.68f);                  // grey — not worth the walk
        }

        // ----- floating damage --------------------------------------------------------------------

        private void SpawnDamageNumber(CombatEvent e)
        {
            if (Boot.Entities == null || !_showDamageNumbers) return;

            // Anchor on the entity that was HIT — damage belongs where it landed. Misses still show,
            // because "nothing happened" and "I missed" look identical otherwise.
            var view = Boot.Entities.Find(e.TargetId);
            if (view == null) return;

            bool incoming = e.TargetId == Boot.SelfId;
            string text;
            Color colour;
            float size = 20f;

            switch (e.Outcome)
            {
                case CombatOutcome.Miss:                 // physical evade
                    text = "miss"; colour = UiKit.TextDim; break;
                case CombatOutcome.Crit:
                    text = e.Damage.ToString() + "!"; colour = new Color(1f, 0.85f, 0.30f); size = 26f; break;
                case CombatOutcome.Heal:                 // +N green, over whoever was healed (incl. party)
                    if (e.Damage <= 0) return;
                    text = "+" + e.Damage; colour = UiKit.Good; break;
                case CombatOutcome.ManaHeal:
                    if (e.Damage <= 0) return;
                    text = "+" + e.Damage + " MP"; colour = UiKit.Mp; break;
                case CombatOutcome.Buff:                 // a buff/debuff landed — show its NAME so you know
                    text = string.IsNullOrEmpty(e.Skill) ? "buff" : e.Skill;
                    colour = new Color(0.55f, 0.80f, 1f); size = 16f; break;
                case CombatOutcome.Fail:                 // spell partially resisted
                    text = e.Damage > 0 ? e.Damage.ToString() : "resist";
                    colour = new Color(0.72f, 0.62f, 0.95f); break;
                case CombatOutcome.Block:
                    text = e.Damage > 0 ? e.Damage + " blk" : "block"; colour = UiKit.TextDim; break;
                default:                                  // Hit
                    if (e.Damage <= 0) return;
                    text = e.Damage.ToString();
                    colour = incoming ? new Color(1f, 0.45f, 0.45f) : UiKit.Text;
                    break;
            }

            var floater = FreeFloater();
            floater.Label.text = text;
            floater.Label.color = colour;
            floater.Label.fontSize = size;
            floater.World = view.transform.position + Vector3.up * NameplateHeight;
            floater.BornAt = Time.unscaledTime;
            floater.Root.gameObject.SetActive(true);
        }

        private FloatingNumber FreeFloater()
        {
            foreach (var floater in _floaters)
                if (!floater.Root.gameObject.activeSelf) return floater;

            var root = UiKit.Rect(UiKit.Box(_nameplateLayer, "Damage",
                                            new Color(0, 0, 0, 0), blocksInput: false).gameObject);
            root.sizeDelta = new Vector2(160f, 34f);
            var label = UiKit.Label(root, "", 20f, UiKit.Text, TextAlignmentOptions.Center);
            UiKit.Stretch(UiKit.Rect(label.gameObject), 0f, 0f, 0f, 0f);

            var created = new FloatingNumber { Root = root, Label = label };
            _floaters.Add(created);
            return created;
        }

        private void RefreshFloaters()
        {
            var cam = Camera.main;
            if (cam == null) return;

            foreach (var floater in _floaters)
            {
                if (!floater.Root.gameObject.activeSelf) continue;

                float age = (Time.unscaledTime - floater.BornAt) / FloatSeconds;
                if (age >= 1f) { floater.Root.gameObject.SetActive(false); continue; }

                var screen = cam.WorldToScreenPoint(floater.World);
                if (screen.z <= 0f) { floater.Root.gameObject.SetActive(false); continue; }

                // Rise and fade. The number stays pinned to the WORLD point it was born at rather than
                // following the entity — a mob that walks off should not drag its damage with it.
                floater.Root.position = screen + new Vector3(0f, age * FloatRisePixels, 0f);
                var colour = floater.Label.color;
                colour.a = 1f - age * age;
                floater.Label.color = colour;
            }
        }
    }
}
