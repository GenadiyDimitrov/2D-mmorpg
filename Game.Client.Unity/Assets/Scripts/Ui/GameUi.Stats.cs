using System.Text;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the character sheet.
    ///
    /// ===== `BL-246`: TWO TABS =====================================================================
    ///
    /// Owner, 2026-09-16, with the layout drawn row for row. The sheet had grown into one long column
    /// that mixed "what am I" with "why did that hit for that", and the split is his answer to it.
    ///
    /// 🔑 <b>BASIC SHOWS THE LAST CLASS ONLY</b> — *"Class: Shadowblade (Directly Shadowblade, not
    /// ElfRogue,Descipiline etc ... just last class)"* — and DETAILS shows the full chain. That
    /// distinction IS the point of the split: BASIC answers "who am I and can I fight that", DETAILS
    /// answers "what exactly is my sheet made of", and the class line is the smallest example of the
    /// same rule the rest of the layout follows.
    ///
    /// ⚠ It was a PROTOCOL change as well as a layout. Four of his rows had no source in
    /// <see cref="StatsUpdate"/> at all and arrived with protocol 43: MP Receive
    /// (<c>RestoreMpMod</c>), Stab Rate (<c>BlowRate</c>), the magic crit RESIST, and M.Fail — which
    /// is COMPUTED server-side at parity, because a fizzle chance needs an attacker and "an attacker
    /// of my level" is the only reading of one on a sheet that has no attacker in it. Everything else
    /// he listed was already on the wire and simply had nowhere to be drawn: the three masteries came
    /// with `BL-190`, the crit-resist pair and the heal stats earlier still.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _statsPanel;
        private TextMeshProUGUI _statsBody;
        private int _statsStamp = -1;

        /// <summary>Which tab is showing. 0 = BASIC, 1 = DETAILS. Not persisted: the sheet is opened
        /// for one of two reasons and BASIC is the one you want nine times out of ten.</summary>
        private int _statsTab;

        private Button[] _statsTabButtons;

        private static readonly string[] StatsTabNames = { "Basic", "Details" };

        private void BuildStatsWindow()
        {
            _statsPanel = UiKit.PanelBox(_worldRoot, "Stats");
            UiKit.Place(_statsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(620f, 460f));
            var inner = _statsPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_statsPanel, "Character", () => CloseWindow(_statsPanel));

            // The tab strip. Two buttons, hand-rolled rather than through BuildCategoryTabs — that one
            // is keyed on ItemCategory, and these are not categories of item.
            _statsTabButtons = new Button[StatsTabNames.Length];
            for (int i = 0; i < StatsTabNames.Length; i++)
            {
                int tab = i;
                var button = UiKit.TextButton(inner, StatsTabNames[i], () =>
                {
                    if (_statsTab == tab) return;
                    _statsTab = tab;
                    _statsStamp = -1;          // the body is keyed on the STATS, not the tab — force it
                }, 15f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(16f + i * 106f, -chrome - 6f), new Vector2(100f, 32f));
                _statsTabButtons[i] = button;
            }

            ScrollRect scroll;
            var content = UiKit.ScrollArea(inner, out scroll, 2f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 44f, 16f, 16f);

            _statsBody = UiKit.Label(content, "", 16f, UiKit.Text, TextAlignmentOptions.TopLeft);
            var fitter = _statsBody.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _statsPanel.gameObject.SetActive(false);
        }

        private void RefreshStatsWindow()
        {
            if (!_statsPanel.gameObject.activeSelf) return;

            var s = Boot.Stats;
            if (s == null) { _statsBody.text = "Waiting for stats …"; return; }

            // Rebuild only when a number actually moved. Regen ticks every 3s and HP/MP change
            // constantly, so a naive per-frame rebuild would re-lay out a long text block forever.
            // Karma and the kill counts are in the stamp too — they arrive on their own push, so a
            // sheet keyed only on StatsUpdate would keep showing yesterday's karma. The TAB is NOT in
            // the stamp: switching it resets the stamp directly, which is cheaper and unambiguous.
            int stamp = s.GetHashCode() ^ (Boot.Progress != null ? Boot.Progress.Level * 7919 : 0)
                      ^ (Boot.Karma * 31 + Boot.PkCount * 7 + Boot.PvpCount + (Boot.PvpEnabled ? 1 : 0))
                      ^ (Boot.Favor != null ? Boot.Favor.GetHashCode() * 17 : 0);   // `BL-277`, its own push
            if (stamp == _statsStamp) return;
            _statsStamp = stamp;

            for (int i = 0; i < _statsTabButtons.Length; i++)
                if (_statsTabButtons[i] != null)
                    _statsTabButtons[i].targetGraphic.color =
                        i == _statsTab ? UiKit.TabActive : UiKit.PanelLight;

            _statsBody.text = (_statsTab == 0 ? BuildBasicSheet(s) : BuildDetailsSheet(s)).TrimEnd();
        }

        // ----- BASIC ---------------------------------------------------------------------------
        //
        // His table: Class (race/level, then the LAST class), Primary, Basic, PVP. Nothing derived and
        // nothing conditional — this tab is the same length on every character, which is what makes it
        // readable at a glance.

        private string BuildBasicSheet(StatsUpdate s)
        {
            var t = new StringBuilder();

            var active = Boot.ActiveClass;
            t.AppendLine(Head("Class"));
            if (active != null)
            {
                t.AppendLine(Row2("Race", active.Race.ToString(), "Level", active.Level.ToString()));
                // 🔑 THE LAST CLASS AND NOTHING ELSE (owner: *"just last class"*). The chain lives on
                // the DETAILS tab; repeating it here would make the two tabs the same tab.
                t.AppendLine(Row2("Class", LastClassName(active), "", ""));
            }
            t.AppendLine();

            t.AppendLine(Head("Primary"));
            t.AppendLine(Row3("ATK", s.Atk.ToString(), "CON", s.Con.ToString(), "SPT", s.Spt.ToString()));
            t.AppendLine(Row2("WIT", s.Wit.ToString(), "AGI", s.Agi.ToString()));
            t.AppendLine();

            t.AppendLine(Head("Basic"));
            t.AppendLine(Row2("HP", s.MaxHp.ToString(), "MP", s.MaxMp.ToString()));
            t.AppendLine(Row2("P.Atk", s.AttackPower.ToString(), "M.Atk", s.MagicAttack.ToString()));
            t.AppendLine(Row2("P.Def", s.Defence.ToString(), "M.Def", s.MagicDefence.ToString()));
            // His "Atk Speed: 1/1500" — the RAW stat over its cap, which is the form that tells you how
            // much room is left. The multiplier form (x3.70) is on the DETAILS tab instead.
            t.AppendLine(Row2("Atk Speed", SpeedOverCap(s.AttackSpeedMult, StatCaps.AttackSpeed),
                              "Cast Speed", SpeedOverCap(s.CastSpeedMult, StatCaps.CastSpeed)));
            t.AppendLine(Row3("Acc", s.Accuracy.ToString(), "Eva", s.Evasion.ToString(),
                              "Speed", s.MoveSpeed.ToString("0")));
            t.AppendLine();

            // PvP / reputation. Karma is what turns guards hostile, makes you drop gear on death and
            // takes the safety out of towns, so a player carrying it must be able to see it.
            t.AppendLine(Head("PVP"));
            t.AppendLine(Row2("PVP", Boot.PvpCount.ToString(), "PK", Boot.PkCount.ToString()));
            t.AppendLine(Row2("Karma", Boot.Karma > 0
                                  ? "<color=#FF6060>" + Boot.Karma.ToString("N0") + "</color>" : "0",
                              "Flag", Boot.PvpEnabled ? "ON" : "off"));

            return t.ToString();
        }

        // ----- DETAILS -------------------------------------------------------------------------
        //
        // His table: the class CHAIN, Vitals, Offence, Defence. Every row here is a derived or
        // conditional number — the things you open a sheet to check when something did not behave the
        // way you expected it to.

        private string BuildDetailsSheet(StatsUpdate s)
        {
            var t = new StringBuilder();

            var active = Boot.ActiveClass;
            if (active != null)
            {
                t.AppendLine(Head("Class"));
                t.AppendLine(ClassChain(active));
                t.AppendLine();
            }

            t.AppendLine(Head("Vitals"));
            t.AppendLine(Row2("HP/s", s.HpRegen.ToString("0.#"), "MP/s", s.MpRegen.ToString("0.#")));
            // RECEIVED, not given: how much of an incoming heal / mana restore actually lands on you.
            // Both are multipliers on the wire (1 = neutral), shown as the bonus they represent.
            t.AppendLine(Row2("HP Receive", Signed(s.HealReceivedMod - 1f)
                                          + (s.HealReceivedFlat != 0 ? "  +" + s.HealReceivedFlat : ""),
                              "MP Receive", Signed(s.RestoreMpMod - 1f)));
            // GIVEN: what your own heals are worth. Output = (HealPowerFlat + skillPower)·HealPowerMod,
            // so the two halves are a multiplier and a flat, and the row shows both in that order.
            t.AppendLine(Row2("Restore power",
                              "x" + s.HealPowerMod.ToString("0.##") + " + " + s.HealPowerFlat, "", ""));
            t.AppendLine();

            t.AppendLine(Head("Offence"));
            t.AppendLine(Row2("Acc", s.Accuracy.ToString(), "", ""));
            // ⚠ Crit dmg is the FINISHED physical multiplier (StatCalculator.PhysicalCritMult = 2.0 +
            // your bonus, capped), not the bonus on its own — the same reading as the M.Crit dmg row
            // below it, which has always been the finished x2 / x2.6 / x3.38. Showing one as a total
            // and the other as a bonus is how a sheet teaches you a wrong number. The "+N" after it is
            // the FLAT crit damage: attack added inside the ratio on a crit, never a multiplier.
            t.AppendLine(Row2("Crit", Pct(s.CritChance),
                              "Crit dmg", "x" + StatCalculator.PhysicalCritMult(s.CritDamage).ToString("0.##")
                                        + " +" + s.CritDamageFlat.ToString("0")));
            t.AppendLine(Row2("M.Crit", Pct(s.MagicCritChance),
                              "M.Crit dmg", "x" + s.MagicCritDamage.ToString("0.##")));
            // The three skill MASTERIES (`BL-190`) — sent, never derived here: the rate comes from
            // whichever passives the character happens to hold, which is server knowledge.
            t.AppendLine(Row2("x2 Dmg", Pct(s.DoubleDamageRate), "Reuse rst", Pct(s.CooldownResetRate)));
            // Stab Rate is the BLOW rate (`BL-188`), already clamped. ⚠ It is YOUR side of the contest —
            // the defender's BlowResist is applied on top of it at the point of use.
            t.AppendLine(Row2("x2 Duration", Pct(s.DoubleDurationRate), "Stab Rate", Pct(s.BlowRate)));
            // Here the speeds read as the MULTIPLIER against the 333 = 1.0x baseline; the raw stat over
            // its cap is on the BASIC tab. ⚠ The DTO field is a TIME multiplier where lower = faster,
            // so the speed the player expects is its reciprocal.
            t.AppendLine(Row2("Atk.Speed", SpeedTimes(s.AttackSpeedMult),
                              "Cast.Speed", SpeedTimes(s.CastSpeedMult)));
            t.AppendLine();

            t.AppendLine(Head("Defence"));
            t.AppendLine(Row3("Eva", s.Evasion.ToString(), "Speed", s.MoveSpeed.ToString("0"),
                              "State", s.MoveState.ToString()));
            // M.Fail is the chance a caster of YOUR OWN LEVEL fizzles against you (computed server-side
            // — the formula needs an attacker). M.Resist is DAMAGE reduction, not a landing stat: two
            // different defences against magic, which is why they share a line and not a name.
            t.AppendLine(Row2("M.Fail", Pct(s.MagicFailAtParity), "M.Resist", Pct(s.MagicResist)));
            // 🔑 THESE TWO ARE RESISTS, not your own crit. They cut an ATTACKER's crit rate and crit
            // damage against you (`BL-211`). Same word, opposite side — hence the group they sit in.
            t.AppendLine(Row2("Crit", Pct(s.CritRateResist), "Crit dmg", Pct(s.CritDmgResist)));
            t.AppendLine(Row2("M.Crit", Pct(s.MagicCritRateResist), "", ""));
            // Block is conditional on a shield, so its row is drawn only with one — a permanent
            // "Block Rate: 0%" on every mage is noise, which is the same reasoning that dropped the old
            // Shield-def row (his, 2026-08-12).
            if (s.HasShield)
                t.AppendLine(Row2("Block Rate", Pct(s.BlockChance), "Block Red", Pct(s.BlockReduction)));
            t.AppendLine();

            // `BL-277` — his "Other" block: the Wayfarer's Favor gauge and the FINISHED rates (server
            // rate × runes × Favor), sent by the server so the sheet never re-derives them.
            var f = Boot.Favor;
            if (f != null)
            {
                t.AppendLine(Head("Other"));
                // `BL-283` — his "Charisma: 12345678 (1000)" = lifetime (current). Current is the last 30
                // days, capped at 1000; it is what speeds the Blessing fill below.
                t.AppendLine(Row2("Charisma", f.CharismaLifetime.ToString("N0") + " (" + f.CharismaCurrent + ")", "", ""));
                t.AppendLine(Row2("Favor", f.Points.ToString("N0") + " / " + WayfarerFavor.MaxPoints.ToString("N0"),
                                  "Stage", f.Stage + "  (+" + (WayfarerFavor.BonusPerStage * f.Stage * 100f).ToString("0") + "%)"));
                // `BL-277` part 2 — his "Our_Blessing: 98/100 (x4)": current progress and the fill rate.
                // The time left of a running one is on its buff square, so the row just says it is on.
                t.AppendLine(Row2("Blessing", f.BlessingActive ? "ACTIVE" : f.BlessingPercent + " / 100",
                                  "Kill fill", Rate(f.BlessingFillRate)));   // `BL-295`: kills only
                t.AppendLine(Row2("Exp rate", Rate(f.ExpRate), "SP rate", Rate(f.SpRate)));
                t.AppendLine(Row2("Gold rate", Rate(f.GoldRate), "Drop rate", Rate(f.DropRate)));
                t.AppendLine();
            }

            // Not on his layout, and kept because nothing else shows them and they cost two lines: the
            // gear summary and the wallet. They sit at the BOTTOM of DETAILS, which is where a row
            // nobody asked for belongs.
            t.AppendLine(Head("Gear"));
            t.AppendLine(Row2("Armour", string.IsNullOrEmpty(s.ArmorMastery) ? "—" : s.ArmorMastery,
                              "Set", string.IsNullOrEmpty(s.ActiveSet) ? "—" : s.ActiveSet));
            t.AppendLine(Row2("Gold", Boot.Gold.ToString("N0"), "SP", s.SkillPoints.ToString("N0")));
            // `BL-257` — platinum is the ACCOUNT's, so the row says so. Hidden at zero: a premium line
            // reading 0 on every character in the game is noise until he has any.
            if (Boot.Platinum > 0)
                t.AppendLine(Row2("Platinum (account)", Boot.Platinum.ToString("N0"), "", ""));

            return t.ToString();
        }

        // ----- the class line, both readings ----------------------------------------------------

        /// <summary>BASIC's `Class:` — the LAST class this character took and nothing else (owner:
        /// *"Directly Shadowblade, not ElfRogue,Descipiline etc"*). Falls back down the ladder, so a
        /// character who has not reached the 3rd yet still reads as whatever it last became.</summary>
        private static string LastClassName(SubclassDto active)
        {
            string fourth = FourthClassCatalog.Get(active.FourthClass)?.Name;
            if (!string.IsNullOrEmpty(fourth)) return fourth;
            string third = ThirdClassCatalog.Get(active.ThirdClass)?.Name;
            if (!string.IsNullOrEmpty(third)) return third;
            string second = ClassCatalog.Get(active.SecondClass)?.Name;
            return !string.IsNullOrEmpty(second) ? second : active.BaseClass.ToString();
        }

        /// <summary>DETAILS' class line — the whole chain, his `Elf Rogue -> Phantom -> Shadowblade`.
        /// The head is the 2nd class (that IS "Elf Rogue" — see docs/design/ClassRenames.md); a
        /// character below it shows its race and base class, which is the only name it has.</summary>
        private static string ClassChain(SubclassDto active)
        {
            string second = ClassCatalog.Get(active.SecondClass)?.Name;
            var chain = new StringBuilder(string.IsNullOrEmpty(second)
                ? active.Race + " " + active.BaseClass : second);
            string third = ThirdClassCatalog.Get(active.ThirdClass)?.Name;
            if (!string.IsNullOrEmpty(third)) chain.Append(" -> ").Append(third);
            string fourth = FourthClassCatalog.Get(active.FourthClass)?.Name;
            if (!string.IsNullOrEmpty(fourth)) chain.Append(" -> ").Append(fourth);
            return chain.ToString();
        }

        // ----- formatting -----------------------------------------------------------------------

        private static string Head(string title) => "<b>" + title + "</b>";

        /// <summary>Two label/value pairs per line — a phone is wide in landscape and a single column
        /// would need twice the scrolling.</summary>
        private static string Row2(string a, string av, string b, string bv)
        {
            string left = (a + ":").PadRight(14) + av;
            if (string.IsNullOrEmpty(b)) return left;
            return left.PadRight(32) + (b + ":").PadRight(14) + bv;
        }

        /// <summary>Three pairs, for his `ATK CON SPT` and `Acc Eva Speed` rows. Narrower columns than
        /// <see cref="Row2"/>, which is why it is a separate helper rather than a parameter — those
        /// rows carry short numbers and would waste half the line at Row2's widths.</summary>
        private static string Row3(string a, string av, string b, string bv, string c, string cv)
        {
            return ((a + ":").PadRight(10) + av).PadRight(22)
                 + ((b + ":").PadRight(10) + bv).PadRight(22)
                 + (c + ":").PadRight(10) + cv;
        }

        private static string Pct(float value) => (value * 100f).ToString("0.#") + "%";

        /// <summary>A rate as his "x2.5" — up to two decimals, trailing zeros dropped.</summary>
        private static string Rate(float value) => "x" + value.ToString("0.##");

        /// <summary>A bonus as a signed percent — "0%", "+12%", "-30%". Used where the wire carries a
        /// MULTIPLIER (1 = neutral) and the row wants the bonus it represents.</summary>
        private static string Signed(float bonus)
        {
            float pct = bonus * 100f;
            return (pct > 0f ? "+" : "") + pct.ToString("0.#") + "%";
        }

        /// <summary>Attack/cast speed as the RAW STAT over its cap — "702 / 1999".
        ///
        /// ⚠ The DTO field is a cast/attack-TIME multiplier where LOWER = FASTER: the server returns
        /// `SpeedBaseline / stat`, so a fast caster's field is SMALL. The raw stat is therefore
        /// `SpeedBaseline / mult` (NOT mult × baseline — that inverts it, which is the bug that made a
        /// fully-buffed caster read "158" when the real stat was ~702).</summary>
        private static string SpeedOverCap(float mult, int cap)
        {
            float m = Mathf.Max(0.001f, mult);
            int raw = Mathf.RoundToInt(StatCalculator.SpeedBaseline / m);
            return raw.ToString("N0") + " / " + cap.ToString("N0");
        }

        /// <summary>The same number as a multiplier against the 333 = 1.0x baseline — "x2.11".</summary>
        private static string SpeedTimes(float mult) =>
            "x" + (1f / Mathf.Max(0.001f, mult)).ToString("0.00");

        /// <summary>Both forms at once — "702 / 1999  (x2.11)". The two-tab sheet splits them (raw over
        /// cap on BASIC, the multiplier on DETAILS) because it has the room for two lines; the TARGET
        /// window has one line per stat and still wants the whole story, so it keeps this.</summary>
        private static string SpeedStat(float mult, int cap) =>
            SpeedOverCap(mult, cap) + "  (" + SpeedTimes(mult) + ")";
    }
}
