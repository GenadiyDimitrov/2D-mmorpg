using System.Text;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the expanded TARGET window (the WPF target-details / mob-info panel) and the
    /// resurrect offer.
    ///
    /// The target details are pulled, not pushed: the server only sends them when asked, so a HUD that
    /// never asks shows nothing. That is why the small target panel gets an "Info" button rather than
    /// the details simply appearing.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _detailsPanel;
        private TextMeshProUGUI _detailsBody, _detailsTitle;
        private Button _statsTabButton, _dropsTabButton, _skillsTabButton;

        private enum TargetTab { Stats, Drops, Skills }

        /// <summary>Which tab is showing, and whether the drop table has already been ASKED for during
        /// THIS window-open. Both are the owner's rule: stats come on the Info tap; the drop table is a
        /// separate, heavier ask that fires the first time the Drops tab is opened and NOT AGAIN until
        /// the window is reopened — switching between tabs re-requests nothing. The Skills tab needs no
        /// ask at all: the kit rides along with the stats response.</summary>
        private TargetTab _targetTab;
        private bool _dropsRequested;

        /// <summary>THE ENTITY THIS WINDOW IS PINNED TO — set when Info is tapped, and NOT changed by
        /// retargeting. The window used to render `Boot.Details` only while it still matched
        /// `Boot.TargetId`, so the moment auto-farm picked a new mob the sheet he was reading blanked
        /// to "Select a target and tap Info" (his playtest-20 ask). A bestiary page you cannot finish
        /// reading is not a bestiary page. Tapping Info again re-pins to whatever is targeted then.</summary>
        private System.Guid? _detailsPinned;

        private ScrollRect _detailsScroll;

        private RectTransform _resPanel;
        private TextMeshProUGUI _resText;

        private void BuildTargetWindow()
        {
            _detailsPanel = UiKit.PanelBox(_worldRoot, "TargetDetails");
            UiKit.Place(_detailsPanel, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-12f, -140f), new Vector2(540f, 470f));
            var inner = _detailsPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_detailsPanel, "Target", () => CloseWindow(_detailsPanel));

            _detailsTitle = UiKit.Label(inner, "", 19f, UiKit.Accent, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_detailsTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -chrome - 6f), new Vector2(500f, 24f));

            // Three tabs. Stats and Skills are re-renders of data we already hold; Drops is a lazy ask
            // (below). Skills and Drops are both mob-only — a player target shows Stats alone.
            _statsTabButton = UiKit.TextButton(inner, "Stats", () => _targetTab = TargetTab.Stats, 15f);
            UiKit.Place(UiKit.Rect(_statsTabButton.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -chrome - 34f), new Vector2(110f, 30f));

            _skillsTabButton = UiKit.TextButton(inner, "Skills", () => _targetTab = TargetTab.Skills, 15f);
            UiKit.Place(UiKit.Rect(_skillsTabButton.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(132f, -chrome - 34f), new Vector2(110f, 30f));

            _dropsTabButton = UiKit.TextButton(inner, "Drops", ShowDropsTab, 15f);
            UiKit.Place(UiKit.Rect(_dropsTabButton.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(248f, -chrome - 34f), new Vector2(110f, 30f));

            ScrollRect scroll;
            var content = UiKit.ScrollArea(inner, out scroll, 2f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 70f, 16f, 16f);
            _detailsScroll = scroll;

            _detailsBody = UiKit.Label(content, "", 15f, UiKit.Text, TextAlignmentOptions.TopLeft);
            var fitter = _detailsBody.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _detailsPanel.gameObject.SetActive(false);

            BuildResurrectPrompt();
        }

        /// <summary>Info tap on the target panel: open the window on the STATS tab and ask the server
        /// for stats only (WithDrops:false). Resets the once-per-open drop-request latch.</summary>
        public void OpenTargetDetails()
        {
            if (!Boot.TargetId.HasValue) return;
            _targetTab = TargetTab.Stats;
            _dropsRequested = false;
            _detailsPinned = Boot.TargetId.Value;   // pin HERE — retargeting must not move it
            Boot.InspectTarget(Boot.TargetId.Value, false);
            OpenWindow(_detailsPanel);
        }

        // Just switch tabs — the actual drop-table request is driven from the refresh loop, so it still
        // fires (once) even if the tab was tapped before the stats response had arrived.
        private void ShowDropsTab() => _targetTab = TargetTab.Drops;

        /// <summary>
        /// The resurrect prompt sits on the ROOT canvas, not inside the world layer.
        ///
        /// It has to be visible while you are dead — which in the WPF client was the bug that took two
        /// playtests to pin down, because the prompt was drawn UNDER the death overlay. Anything that
        /// must be reachable at the worst moment goes above everything else.
        /// </summary>
        private void BuildResurrectPrompt()
        {
            _resPanel = UiKit.PanelBox(_root, "Resurrect");
            UiKit.Place(_resPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, 60f), new Vector2(520f, 200f));
            var inner = _resPanel.GetChild(0);

            _resText = UiKit.Label(inner, "", 18f, UiKit.Text, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_resText.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(20f, -20f), new Vector2(470f, 80f));

            var accept = UiKit.TextButton(inner, "Get up", () => Boot.AnswerResurrect(true));
            UiKit.Place(UiKit.Rect(accept.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(20f, 20f), new Vector2(220f, 52f));

            var decline = UiKit.TextButton(inner, "Stay down", () => Boot.AnswerResurrect(false));
            UiKit.Place(UiKit.Rect(decline.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-20f, 20f), new Vector2(220f, 52f));

            _resPanel.gameObject.SetActive(false);
        }

        private void RefreshTargetWindow()
        {
            // Resurrect offer — checked every frame because it can arrive at any moment, and it is not
            // a "window" in the stack sense: back must not dismiss a decision this important.
            var offer = Boot.PendingResurrect;
            _resPanel.gameObject.SetActive(offer != null);
            if (offer != null)
            {
                // A SELF-res reads as your own doing, not as a stranger's offer — the preservation
                // skills (Undying Will / Rite of Preservation) send this prompt with the fallen
                // player as their own rescuer, and "Gena offers to resurrect you" over your own
                // corpse reads like a bug. Its window also never expires, which is worth saying:
                // the whole point of the re-spec is that the decision waits for you.
                bool self = offer.SelfRes;
                _resText.text = (self ? "Your own preservation holds you together.\n"
                                      : offer.FromName + " offers to resurrect you.\n")
                              + "You would recover " + (int)(offer.ExpPct * 100f) + "% of the exp you lost"
                              + (offer.ExpRestored > 0 ? "  (" + offer.ExpRestored.ToString("N0") + ")" : "")
                              + ".\n\nAccepting revives you WHERE YOU FELL."
                              + (self ? "\nThis offer does not expire." : "");
            }

            if (!_detailsPanel.gameObject.activeSelf) return;

            // Keyed on the PINNED entity, never on the live target — see _detailsPinned.
            var d = Boot.Details;
            if (d == null || _detailsPinned == null || d.Id != _detailsPinned.Value)
            {
                _detailsBody.text = "Select a target and tap Info.";
                _detailsTitle.text = "";
                _statsTabButton.gameObject.SetActive(false);
                _dropsTabButton.gameObject.SetActive(false);
                _skillsTabButton.gameObject.SetActive(false);
                return;
            }

            // Rank only reads on a mob (players send ""); Normal is left unmarked to keep the common
            // case clean, so the tag only appears when it means something. The [pinned] tag appears
            // once the sheet stops describing what you are actually fighting — without it, holding the
            // window open through a retarget silently shows the wrong creature's numbers.
            string rank = !string.IsNullOrEmpty(d.Rank) && d.Rank != "Normal" ? "   [" + d.Rank + "]" : "";
            bool stale = Boot.TargetId.HasValue && Boot.TargetId.Value != d.Id;
            _detailsTitle.text = d.Name + "   Lv " + d.Level + rank
                               + (stale ? "   <color=#8A8F98><size=15>[pinned]</size></color>" : "");

            // Only mobs have Drops and Skills; a player target forces Stats.
            _statsTabButton.gameObject.SetActive(true);
            _dropsTabButton.gameObject.SetActive(d.IsMob);
            _skillsTabButton.gameObject.SetActive(d.IsMob);
            if (!d.IsMob) _targetTab = TargetTab.Stats;

            // The one drop-table ask, per window-open: fire the first frame the Drops tab is showing a
            // mob we haven't yet fetched drops for. Driven here (not on the button) so it survives the
            // tab being tapped before the stats response landed.
            if (_targetTab == TargetTab.Drops && d.IsMob && !_dropsRequested)
            {
                Boot.InspectTarget(d.Id, true);
                _dropsRequested = true;
            }

            // Active tab gets the accent; the others stay neutral.
            _statsTabButton.targetGraphic.color = _targetTab == TargetTab.Stats ? UiKit.TabActive : UiKit.PanelLight;
            _skillsTabButton.targetGraphic.color = _targetTab == TargetTab.Skills ? UiKit.TabActive : UiKit.PanelLight;
            _dropsTabButton.targetGraphic.color = _targetTab == TargetTab.Drops ? UiKit.TabActive : UiKit.PanelLight;

            // Only touch the label when the text actually changes: this runs every frame the window is
            // open, and a forced layout rebuild per frame would be wasteful. The rebuild + scroll reset
            // is what stops the body being laid out against its PREVIOUS size — which showed as the mob
            // sheet rendering with its top rows above the visible area (playtest-13).
            string body = _targetTab switch
            {
                TargetTab.Drops  => DropsText(d),
                TargetTab.Skills => SkillsText(d),
                _                => StatsText(d),
            };
            if (_detailsBody.text != body)
            {
                _detailsBody.text = body;
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_detailsBody.transform);
                if (_detailsScroll != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_detailsScroll.transform);
                    _detailsScroll.verticalNormalizedPosition = 1f;
                }
            }
        }

        /// <summary>The FULL stat sheet — same depth as the character window, on the owner's rule that
        /// it is better to carry the info and not need it. Grouped so a wall of numbers still reads.</summary>
        private static string StatsText(TargetDetails d)
        {
            var t = new StringBuilder();

            // 🔴 BEHAVIOUR FIRST, for a mob (playtest 23): *"add info like -> agro:true/false, social:
            // true/false, social clan: clanName, info that will be helpful to a player."* It goes at the
            // TOP because it is the only part of this sheet you need BEFORE you pull — everything below
            // it tells you how the fight goes, this tells you whether there is one and how many.
            if (d.IsMob)
            {
                t.AppendLine("<b>Behaviour</b>");
                t.AppendLine(Pair("Aggressive", d.Aggressive ? "yes — attacks on sight" : "no — leaves you alone",
                                  "", ""));
                t.AppendLine(Pair("Social", string.IsNullOrEmpty(d.SocialClan)
                                      ? "no — fights alone"
                                      : "yes — " + d.SocialClan + " answer its cry", "", ""));
                if (!string.IsNullOrEmpty(d.Rank) && d.Rank != "Normal")
                    t.AppendLine(Pair("Rank", d.Rank, "", ""));
                t.AppendLine();
            }

            // A mob's MANA is deliberately not shown — it is one of the "unused info statuses" he asked
            // to be rid of. Nothing you can do changes it and no mob is ever stopped by it, so a full
            // MP bar on every creature was a column of noise. Players keep both halves: another player's
            // MP is exactly what tells a healer whether they can still cast.
            t.AppendLine(d.IsMob
                ? Pair("HP", d.Hp + " / " + d.MaxHp, "HP regen", d.HpRegen.ToString("0.#") + "/s")
                : Pair("HP", d.Hp + " / " + d.MaxHp, "MP", d.Mp + " / " + d.MaxMp));
            if (!d.IsMob)
                t.AppendLine(Pair("HP regen", d.HpRegen.ToString("0.#") + "/s", "MP regen", d.MpRegen.ToString("0.#") + "/s"));

            t.AppendLine();
            t.AppendLine("<b>Attributes</b>");
            // A MOB's ATK, CON and SPT are shown to NOBODY, because a mob reads none of them: its HP, MP,
            // P.Atk, M.Atk, P.Def and M.Def all come from the MobBaseStats curve, and StatCalculator.MobStats
            // says so itself for SPT ("mobs don't use it … it's here so the record is complete"). They were
            // the numbers that looked "over inflated" on a level-80 creature — CON 175, ATK 168 — and they
            // were inflated, but only as text: nothing downstream ever read them. Printing a stat that
            // drives nothing is the same "unused info statuses" problem as the mob MP bar above and the
            // all-zero Utility block below. AGI and WIT stay: those two are live on a mob.
            if (!d.IsMob)
            {
                t.AppendLine(Pair("Power (ATK)", d.Atk.ToString(), "CON", d.Con.ToString()));
                t.AppendLine(Pair("AGI", d.Agi.ToString(), "WIT", d.Wit.ToString()));
                t.AppendLine(Pair("SPT", d.Spt.ToString(), "", ""));
            }
            else
            {
                t.AppendLine(Pair("AGI", d.Agi.ToString(), "WIT", d.Wit.ToString()));
            }

            t.AppendLine();
            t.AppendLine("<b>Offense</b>");
            t.AppendLine(Pair("P.Atk", d.PAtk.ToString(), "M.Atk", d.MAtk.ToString()));
            t.AppendLine(Pair("Accuracy", d.Accuracy.ToString(), "Atk range", d.AttackRange.ToString("0")));
            t.AppendLine(Pair("Crit", Pct(d.CritChance), "M.Crit", Pct(d.MagicCritChance)));
            t.AppendLine(Pair("Crit dmg", "x" + (1f + d.CritDamage).ToString("0.##"),
                              "Atk speed", SpeedStat(d.AttackSpeedMult, StatCaps.AttackSpeed)));
            t.AppendLine(Pair("Cast speed", SpeedStat(d.CastSpeedMult, StatCaps.CastSpeed),
                              "Move speed", d.MoveSpeed.ToString("0")));

            t.AppendLine();
            t.AppendLine("<b>Defense</b>");
            t.AppendLine(Pair("P.Def", d.PDef.ToString(), "M.Def", d.MDef.ToString()));
            t.AppendLine(Pair("Evasion", d.Evasion.ToString(), "Interrupt res", d.InterruptResist.ToString()));
            t.AppendLine(Pair("Crit res", Pct(d.CritResist), "Crit dmg res", Pct(d.CritDmgResist)));
            t.AppendLine(Pair("Bow res", Pct(d.BowResist), "Magic res", Pct(d.MagicResist)));

            // The Utility block is dropped entirely when every number in it is zero, which for a mob is
            // essentially always — three rows of "0%" is the clearest example of the "unused info
            // statuses" he asked to be rid of. It reappears the moment a mob actually carries one.
            if (d.CooldownReduction != 0f || d.MeleeVamp != 0f || d.SpellVamp != 0f)
            {
                t.AppendLine();
                t.AppendLine("<b>Utility</b>");
                t.AppendLine(Pair("Cooldown", Pct(d.CooldownReduction), "Melee vamp", Pct(d.MeleeVamp)));
                t.AppendLine(Pair("Spell vamp", Pct(d.SpellVamp), "", ""));
            }

            if (d.Effects != null && d.Effects.Length > 0)
            {
                t.AppendLine();
                t.AppendLine("<b>Effects</b>");
                foreach (var effect in d.Effects) t.AppendLine("  " + effect);
            }

            // Traits/passives are NOT here any more — they moved to the Skills tab, which is where he
            // asked for "actives and passives" together. Effects stay: they are what is on the target
            // right now, which belongs with its current numbers.
            return t.ToString().TrimEnd();
        }

        /// <summary>The bestiary page for a creature's KIT: what it can cast, and what it permanently
        /// is. Both halves are always titled, including when empty — "this one only swings" is a real
        /// answer about a mob, and a blank section reads as a bug instead.</summary>
        private static string SkillsText(TargetDetails d)
        {
            var t = new StringBuilder();

            t.AppendLine("<b>Skills</b>");
            if (d.Skills == null || d.Skills.Length == 0)
                t.AppendLine("  None — this creature only attacks.");
            else
                foreach (var line in d.Skills) t.AppendLine("  " + line);

            t.AppendLine();
            t.AppendLine("<b>Passives</b>");
            if (d.Passives == null || d.Passives.Length == 0)
                t.AppendLine("  None.");
            else
                foreach (var passive in d.Passives) t.AppendLine("  " + passive);

            return t.ToString().TrimEnd();
        }

        /// <summary>The drop table. Null = the ask is still in flight (or never made); an empty array =
        /// the server answered and this creature drops nothing at its level.</summary>
        private string DropsText(TargetDetails d)
        {
            if (d.Drops == null)
                return _dropsRequested ? "Loading drop table…" : "Open the Drops tab to load the table.";
            if (d.Drops.Length == 0)
                return "This creature has no drops at its level.";

            var t = new StringBuilder();
            t.AppendLine("<b>Drops</b>   (chance already includes the server's drop rate)");
            t.AppendLine();
            foreach (var drop in d.Drops) t.AppendLine("  " + drop);
            return t.ToString().TrimEnd();
        }

        // Pct(float) is shared from GameUi.Stats.cs (same partial class).

        private static string Pair(string a, string av, string b, string bv) =>
            b.Length == 0
                ? (a + ":").PadRight(13) + av
                : ((a + ":").PadRight(13) + av).PadRight(28) + (b + ":").PadRight(13) + bv;
    }
}
