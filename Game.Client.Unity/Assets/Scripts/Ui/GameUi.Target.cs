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
        private Button _statsTabButton, _dropsTabButton;

        /// <summary>Which tab is showing, and whether the drop table has already been ASKED for during
        /// THIS window-open. Both are the owner's rule: stats come on the Info tap; the drop table is a
        /// separate, heavier ask that fires the first time the Drops tab is opened and NOT AGAIN until
        /// the window is reopened — switching back and forth between the two tabs re-requests nothing.</summary>
        private bool _dropsTab;
        private bool _dropsRequested;

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

            // Two tabs. Stats is just a re-render of data we already hold; Drops is a lazy ask (below).
            _statsTabButton = UiKit.TextButton(inner, "Stats", () => { _dropsTab = false; }, 15f);
            UiKit.Place(UiKit.Rect(_statsTabButton.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -chrome - 34f), new Vector2(110f, 30f));

            _dropsTabButton = UiKit.TextButton(inner, "Drops", ShowDropsTab, 15f);
            UiKit.Place(UiKit.Rect(_dropsTabButton.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(132f, -chrome - 34f), new Vector2(110f, 30f));

            ScrollRect scroll;
            var content = UiKit.ScrollArea(inner, out scroll, 2f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 70f, 16f, 16f);

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
            _dropsTab = false;
            _dropsRequested = false;
            Boot.InspectTarget(Boot.TargetId.Value, false);
            OpenWindow(_detailsPanel);
        }

        // Just switch tabs — the actual drop-table request is driven from the refresh loop, so it still
        // fires (once) even if the tab was tapped before the stats response had arrived.
        private void ShowDropsTab() => _dropsTab = true;

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
                _resText.text = offer.FromName + " offers to resurrect you.\n"
                              + "You would recover " + (int)(offer.ExpPct * 100f) + "% of the exp you lost"
                              + (offer.ExpRestored > 0 ? "  (" + offer.ExpRestored.ToString("N0") + ")" : "")
                              + ".\n\nAccepting revives you WHERE YOU FELL.";

            if (!_detailsPanel.gameObject.activeSelf) return;

            var d = Boot.Details;
            if (d == null || (Boot.TargetId.HasValue && d.Id != Boot.TargetId.Value))
            {
                _detailsBody.text = "Select a target and tap Info.";
                _detailsTitle.text = "";
                _statsTabButton.gameObject.SetActive(false);
                _dropsTabButton.gameObject.SetActive(false);
                return;
            }

            // Rank only reads on a mob (players send ""); Normal is left unmarked to keep the common
            // case clean, so the tag only appears when it means something.
            string rank = !string.IsNullOrEmpty(d.Rank) && d.Rank != "Normal" ? "   [" + d.Rank + "]" : "";
            _detailsTitle.text = d.Name + "   Lv " + d.Level + rank;

            // Only mobs have a Drops tab; a player target forces Stats.
            _statsTabButton.gameObject.SetActive(true);
            _dropsTabButton.gameObject.SetActive(d.IsMob);
            if (_dropsTab && !d.IsMob) _dropsTab = false;

            // The one drop-table ask, per window-open: fire the first frame the Drops tab is showing a
            // mob we haven't yet fetched drops for. Driven here (not on the button) so it survives the
            // tab being tapped before the stats response landed.
            if (_dropsTab && d.IsMob && !_dropsRequested)
            {
                Boot.InspectTarget(d.Id, true);
                _dropsRequested = true;
            }

            // Active tab gets the accent; the other stays neutral.
            _statsTabButton.targetGraphic.color = _dropsTab ? UiKit.PanelLight : UiKit.Accent;
            _dropsTabButton.targetGraphic.color = _dropsTab ? UiKit.Accent : UiKit.PanelLight;

            _detailsBody.text = _dropsTab ? DropsText(d) : StatsText(d);
        }

        /// <summary>The FULL stat sheet — same depth as the character window, on the owner's rule that
        /// it is better to carry the info and not need it. Grouped so a wall of numbers still reads.</summary>
        private static string StatsText(TargetDetails d)
        {
            var t = new StringBuilder();
            t.AppendLine(Pair("HP", d.Hp + " / " + d.MaxHp, "MP", d.Mp + " / " + d.MaxMp));
            t.AppendLine(Pair("HP regen", d.HpRegen.ToString("0.#") + "/s", "MP regen", d.MpRegen.ToString("0.#") + "/s"));

            t.AppendLine();
            t.AppendLine("<b>Attributes</b>");
            t.AppendLine(Pair("Power (ATK)", d.Atk.ToString(), "CON", d.Con.ToString()));
            t.AppendLine(Pair("DEX", d.Dex.ToString(), "WIT", d.Wit.ToString()));
            t.AppendLine(Pair("SPT", d.Spt.ToString(), "", ""));

            t.AppendLine();
            t.AppendLine("<b>Offense</b>");
            t.AppendLine(Pair("P.Atk", d.PAtk.ToString(), "M.Atk", d.MAtk.ToString()));
            t.AppendLine(Pair("Accuracy", d.Accuracy.ToString(), "Atk range", d.AttackRange.ToString("0")));
            t.AppendLine(Pair("Crit", Pct(d.CritChance), "M.Crit", Pct(d.MagicCritChance)));
            t.AppendLine(Pair("Crit dmg", "x" + (1f + d.CritDamage).ToString("0.##"),
                              "Atk speed", "x" + d.AttackSpeedMult.ToString("0.##")));
            t.AppendLine(Pair("Cast speed", "x" + d.CastSpeedMult.ToString("0.##"),
                              "Move speed", d.MoveSpeed.ToString("0")));

            t.AppendLine();
            t.AppendLine("<b>Defense</b>");
            t.AppendLine(Pair("P.Def", d.PDef.ToString(), "M.Def", d.MDef.ToString()));
            t.AppendLine(Pair("Evasion", d.Evasion.ToString(), "Interrupt res", d.InterruptResist.ToString()));
            t.AppendLine(Pair("Crit res", Pct(d.CritResist), "Crit dmg res", Pct(d.CritDmgResist)));
            t.AppendLine(Pair("Bow res", Pct(d.BowResist), "Magic-fail res", Pct(d.MagicFailResist)));

            t.AppendLine();
            t.AppendLine("<b>Utility</b>");
            t.AppendLine(Pair("Cooldown", Pct(d.CooldownReduction), "Melee vamp", Pct(d.MeleeVamp)));
            t.AppendLine(Pair("Spell vamp", Pct(d.SpellVamp), "", ""));

            if (d.Effects != null && d.Effects.Length > 0)
            {
                t.AppendLine();
                t.AppendLine("<b>Effects</b>");
                foreach (var effect in d.Effects) t.AppendLine("  " + effect);
            }

            if (d.Passives != null && d.Passives.Length > 0)
            {
                t.AppendLine();
                t.AppendLine("<b>Traits</b>");
                foreach (var passive in d.Passives) t.AppendLine("  " + passive);
            }

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
