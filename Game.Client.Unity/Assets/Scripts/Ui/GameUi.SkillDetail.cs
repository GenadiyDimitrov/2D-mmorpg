using System.Text;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the skill DETAIL popup — the WPF client's SkillDetailPopup brought over.
    ///
    /// Tapping a skill's name anywhere (the Known list, the Learn list) opens it. Without this the
    /// Learn tab asks you to spend SP on a name and a price, which is a decision made blind: nothing
    /// tells you what the skill does, what it costs to cast, how long it takes or how far it reaches.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _detailPanel;
        private TextMeshProUGUI _detailTitle, _detailBody;

        private void BuildSkillDetail()
        {
            _detailPanel = UiKit.PanelBox(_worldRoot, "SkillDetail");
            UiKit.Place(_detailPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -20f), new Vector2(560f, 380f));
            var inner = _detailPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_detailPanel, "Skill", () => CloseWindow(_detailPanel));

            _detailTitle = UiKit.Label(inner, "", 20f, UiKit.Accent, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_detailTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 8f), new Vector2(510f, 26f));

            ScrollRect scroll;
            var content = UiKit.ScrollArea(inner, out scroll, 2f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 42f, 16f, 16f);

            _detailBody = UiKit.Label(content, "", 16f, UiKit.Text, TextAlignmentOptions.TopLeft);
            var fitter = _detailBody.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _detailPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Show what a skill actually does. Levels matter: a skill you know at Lv3 should describe Lv3,
        /// and one you are about to buy should describe the level you would GET, so the numbers match
        /// the decision in front of you.
        /// </summary>
        private void ShowSkillDetail(string skillId, int level)
        {
            var def = SkillCatalog.Get(skillId);
            if (def == null) return;

            _detailTitle.text = def.Name + (def.MaxLevel > 1 ? "   Lv." + level : "");

            var text = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(def.Description)) text.AppendLine(def.Description).AppendLine();

            text.AppendLine(Line("Type", def.Category + (def.Passive != null ? "  (passive)" : "")));

            if (def.Passive == null)
            {
                // Ticks are the server's unit (10/sec); seconds are the player's. Convert here rather
                // than showing "CastTicks 20", which means nothing to anyone reading it.
                if (def.MpCost > 0) text.AppendLine(Line("MP", def.MpCost.ToString()));
                text.AppendLine(Line("Cast", Seconds(def.CastTicks)));
                text.AppendLine(Line("Cooldown", Seconds(def.CooldownTicks)));

                if (def.Range > 0) text.AppendLine(Line("Range", ((int)def.Range).ToString()));
                if (def.AreaRadius > 0) text.AppendLine(Line("Area", ((int)def.AreaRadius).ToString()));
                if (def.Power > 0) text.AppendLine(Line("Power", def.Power.ToString()));
                if (def.DurationTicks > 0) text.AppendLine(Line("Duration", Seconds(def.DurationTicks)));
                text.AppendLine(Line("Target", def.TargetMode.ToString()));
            }

            if (!string.IsNullOrEmpty(def.BuffKey))
                text.AppendLine(Line("Stacks as", def.BuffKey + (def.Rank > 0 ? "  rank " + def.Rank : "")));

            // The two things that silently stop a skill being learnable — worth stating on the page
            // where someone is deciding whether to buy it.
            if (!string.IsNullOrEmpty(def.ExclusiveGroup))
                text.AppendLine(Line("Exclusive", def.ExclusiveGroup + " — only one of this group, ever"));
            if (def.Replaces != null && def.Replaces.Length > 0)
                text.AppendLine(Line("Replaces", string.Join(", ", def.Replaces)));

            _detailBody.text = text.ToString().TrimEnd();
            OpenWindow(_detailPanel);
        }

        private static string Line(string label, string value) => label.PadRight(11) + value;

        private static string Seconds(int ticks) =>
            ticks <= 0 ? "instant" : (ticks / (float)GameConstants.TickRate).ToString("0.#") + "s";
    }
}
