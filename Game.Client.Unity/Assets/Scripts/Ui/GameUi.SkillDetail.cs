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
            // ⚠ DescriptionAt, not Description — the card claimed to describe THIS level (see the
            // summary above) while printing level 1's prose. Restore Spirit is where it showed:
            // its generic line is "Burns HP to restore MP", and every per-level line carries the
            // real pair ("Burns 200 HP to restore 120 MP"), so the price was invisible at every
            // level (playtest-20 `55b`). The same slip ran through MP / Power below.
            string desc = def.DescriptionAt(level);
            if (!string.IsNullOrWhiteSpace(desc)) text.AppendLine(desc).AppendLine();

            bool passive = def.Passive != null || def.Category == SkillCategory.Passive;
            text.AppendLine(Line("Type", def.Category + (passive ? "  (passive)" : "")));

            // ----- WHAT IT ACTUALLY DOES ------------------------------------------------------
            // The authored Description is prose, and prose was all a passive ever showed: you could
            // read "toughens your hide" and still not know whether it was worth an SP. The numbers
            // were on the def the whole time; SkillText formats them, at THIS level, for both clients.
            AppendEffects(text, "Effect", SkillText.Passive(def.PassiveAt(level) ?? default));
            if (def.ArmorMasteryAt(level) is ArmorMasteryProfile armor)
                AppendEffects(text, "Armor", SkillText.ArmorMastery(armor));
            if (def.WeaponMasteryAt(level) is WeaponMasteryProfile weapon)
                AppendEffects(text, "Weapon", SkillText.WeaponMastery(weapon));
            AppendEffects(text, def.Category == SkillCategory.Debuff ? "Applies" : "Grants",
                          SkillText.Buff(def, level));

            if (def.Passive == null)
            {
                // Ticks are the server's unit (10/sec); seconds are the player's. Convert here rather
                // than showing "CastTicks 20", which means nothing to anyone reading it.
                int mp = def.MpCostAt(level), hp = def.HpCostAt(level), power = def.PowerAt(level);
                if (mp > 0) text.AppendLine(Line("MP", mp.ToString()));
                // An HP price is a cost like any other and was never printed at all — the other half
                // of `55b`. It is also refused now when you cannot afford it (`55c`), so the number
                // has to be visible or the refusal looks arbitrary.
                if (hp > 0) text.AppendLine(Line("HP", hp.ToString()));
                text.AppendLine(Line("Cast", Seconds(def.CastTicks)));
                text.AppendLine(Line("Cooldown", Seconds(def.CooldownTicks)));

                if (def.Range > 0) text.AppendLine(Line("Range", ((int)def.Range).ToString()));
                if (def.AreaRadius > 0) text.AppendLine(Line("Area", ((int)def.AreaRadius).ToString()));
                if (power > 0) text.AppendLine(Line("Power", power.ToString()));
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

        /// <summary>One "label: a, b, c" block, or nothing at all when the list is empty — a heading
        /// with no numbers under it is exactly the emptiness this was meant to remove.</summary>
        private static void AppendEffects(StringBuilder text, string label, System.Collections.Generic.List<string> lines)
        {
            if (lines == null || lines.Count == 0) return;
            text.AppendLine(Line(label, string.Join(", ", lines)));
        }

        private static string Line(string label, string value) => label.PadRight(11) + value;

        private static string Seconds(int ticks) =>
            ticks <= 0 ? "instant" : (ticks / (float)GameConstants.TickRate).ToString("0.#") + "s";
    }
}
