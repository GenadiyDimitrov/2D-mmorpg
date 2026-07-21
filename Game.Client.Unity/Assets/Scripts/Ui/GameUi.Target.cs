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
        private Button _dropsButton;
        private bool _wantDrops;

        private RectTransform _resPanel;
        private TextMeshProUGUI _resText;

        private void BuildTargetWindow()
        {
            _detailsPanel = UiKit.PanelBox(_worldRoot, "TargetDetails");
            UiKit.Place(_detailsPanel, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-12f, -140f), new Vector2(520f, 430f));
            var inner = _detailsPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_detailsPanel, "Target", () => CloseWindow(_detailsPanel));

            _detailsTitle = UiKit.Label(inner, "", 19f, UiKit.Accent, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_detailsTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -chrome - 6f), new Vector2(400f, 24f));

            // Drops are a separate ASK, not a toggle on existing data — the server only includes them
            // when the request says so, so flipping this has to re-inspect.
            _dropsButton = UiKit.TextButton(inner, "Drops", () =>
            {
                _wantDrops = !_wantDrops;
                if (Boot.TargetId.HasValue) Boot.InspectTarget(Boot.TargetId.Value, _wantDrops);
            }, 15f);
            UiKit.Place(UiKit.Rect(_dropsButton.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-16f, -chrome - 4f), new Vector2(90f, 30f));

            ScrollRect scroll;
            var content = UiKit.ScrollArea(inner, out scroll, 2f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 40f, 16f, 16f);

            _detailsBody = UiKit.Label(content, "", 15f, UiKit.Text, TextAlignmentOptions.TopLeft);
            var fitter = _detailsBody.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _detailsPanel.gameObject.SetActive(false);

            BuildResurrectPrompt();
        }

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
                return;
            }

            _detailsTitle.text = d.Name + "   Lv " + d.Level;
            _dropsButton.gameObject.SetActive(d.IsMob);

            var t = new StringBuilder();
            t.AppendLine(Pair("HP", d.Hp + " / " + d.MaxHp, "MP", d.Mp + " / " + d.MaxMp));
            t.AppendLine(Pair("P.Atk", d.PAtk.ToString(), "M.Atk", d.MAtk.ToString()));
            t.AppendLine(Pair("P.Def", d.PDef.ToString(), "M.Def", d.MDef.ToString()));
            t.AppendLine(Pair("Accuracy", d.Accuracy.ToString(), "Evasion", d.Evasion.ToString()));
            t.AppendLine(Pair("Crit", (d.CritChance * 100f).ToString("0.#") + "%",
                              "Bow resist", (d.BowResist * 100f).ToString("0.#") + "%"));

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

            if (d.Drops != null && d.Drops.Length > 0)
            {
                t.AppendLine();
                t.AppendLine("<b>Drops</b>   (chance already includes the server's drop rate)");
                foreach (var drop in d.Drops) t.AppendLine("  " + drop);
            }

            _detailsBody.text = t.ToString().TrimEnd();
        }

        private static string Pair(string a, string av, string b, string bv) =>
            ((a + ":").PadRight(11) + av).PadRight(26) + (b + ":").PadRight(11) + bv;
    }
}
