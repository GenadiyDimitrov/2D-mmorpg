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
            // `BL-42` — everything the skill does that rides as a FIELD rather than an effect flag:
            // resurrection, buff preservation, lifesteal, traps, stealth, threat, reward rates, the
            // reagent it eats, the weapon it needs. None of it reached this card before, because the
            // card reads flags and magnitudes and the SkillEffect enum has been full for years. One
            // line each, on its own row, because several of them are whole sentences rather than
            // "Label +12%" and they read badly comma-joined.
            foreach (var mech in SkillText.Mechanics(def, level))
                text.AppendLine(Line("", mech));

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

                // The owner's rule is "if a skill is not described as Can Crit or Can Double it doesn't
                // do it" (playtest-19 M8) — but "described" meant the authored PROSE, which drifted:
                // skills carrying the flag never said so, and Piercing Stab's line still described
                // behaviour it no longer had (playtest-20 `52b`). Read the FLAGS, like every other row
                // on this card, so what you are told and what the resolver does cannot disagree.
                string roll = CritLine(def);
                if (roll != null) text.AppendLine(Line("On hit", roll));
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

        /// <summary>How this skill's damage ROLLS, straight from the flags — the `52b` line. Null for
        /// anything that deals no damage (a buff has nothing to crit).
        ///
        /// <para>The four cases are the resolver's, in its order: a BLOW is gated on the crit roll, a
        /// [Double] is a flat ×2 and never crits, Can Crit rolls the caster's rate, and a physical
        /// skill with none of them lands flat. Magic is separate — it always rolls magic crit, which is
        /// why no magic skill carries these flags and why saying nothing there would be a lie of
        /// omission rather than a saved line.</para></summary>
        private static string CritLine(SkillDef def)
        {
            bool physical = (def.Effect & SkillEffect.PhysicalDamage) != 0;
            bool magic = (def.Effect & SkillEffect.MagicDamage) != 0;
            if (!physical && !magic) return null;

            string rate = def.CritRateMod == 1f ? "" : "  (crit rate x" + def.CritRateMod.ToString("0.#") + ")";
            if (def.BlowOnCrit)
                return "Blow — full power only on a crit, otherwise "
                       + Mathf.RoundToInt(def.BlowFailFraction * 100f) + "%" + rate;
            if (def.CanDouble) return "Can Double — x2 when it doubles" + rate;
            if (def.CanCrit) return "Can Crit" + rate;
            if (magic) return "Magic crit";
            return "Lands flat — no crit, no double";
        }

        private static string Line(string label, string value) => label.PadRight(11) + value;

        private static string Seconds(int ticks) =>
            ticks <= 0 ? "instant" : (ticks / (float)GameConstants.TickRate).ToString("0.#") + "s";
    }
}
