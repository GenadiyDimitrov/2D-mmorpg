using System.Collections.Generic;
using System.Linq;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the Skills window — the WPF client's Skills panel brought over.
    ///
    /// Three tabs:
    ///   Known    — what you have; tap one to put it on the bar.
    ///   Learn    — the next learnable level of every class skill, grouped by unlock level, bought
    ///              with SP (or gold, for the stat-swap passives).
    ///   Actions  — the built-in non-skill actions (attack, sit, target closest …) that also live on
    ///              the bar as "action:" tokens.
    ///
    /// Assigning is a two-step TAP, not a drag: pick a skill here, then tap a bar slot. Drag-and-drop
    /// on a phone fights the swipe that pages the bar, and the WPF drag saga is a warning about how
    /// much can go wrong with drag on a skill bar even with a mouse.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _skillsPanel, _skillsContent;
        private TextMeshProUGUI _skillsHeader, _assignHint;
        private int _skillsTab;                 // 0 known, 1 learn, 2 actions
        private Button[] _skillsTabButtons;
        private int _skillsRevision = -1;

        /// <summary>The token waiting for a bar slot, or null. While set, tapping a slot ASSIGNS
        /// instead of casting — the hint bar says so, because a mode you cannot see is a trap.</summary>
        private string _pendingAssign;

        private void BuildSkillsWindow()
        {
            _skillsPanel = UiKit.PanelBox(_worldRoot, "Skills");
            UiKit.Place(_skillsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(720f, 500f));
            var inner = _skillsPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_skillsPanel, "Skills", () => CloseWindow(_skillsPanel));

            _skillsHeader = UiKit.Label(inner, "", 16f, UiKit.TextDim, TextAlignmentOptions.Right);
            UiKit.Place(UiKit.Rect(_skillsHeader.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-70f, -14f), new Vector2(300f, 28f));

            _skillsTabButtons = new Button[3];
            string[] names = { "Known", "Learn", "Actions" };
            for (int i = 0; i < names.Length; i++)
            {
                int tab = i;
                var button = UiKit.TextButton(inner, names[i], () =>
                {
                    _skillsTab = tab;
                    _skillsRevision = -1;      // force a rebuild
                }, 17f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(18f + i * 150f, -chrome - 6f), new Vector2(144f, 38f));
                _skillsTabButtons[i] = button;
            }

            ScrollRect scroll;
            _skillsContent = UiKit.ScrollArea(inner, out scroll, 3f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 50f, 16f, 44f);

            _assignHint = UiKit.Label(inner, "", 15f, UiKit.Accent);
            UiKit.Place(UiKit.Rect(_assignHint.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(18f, 12f), new Vector2(660f, 26f));

            _skillsPanel.gameObject.SetActive(false);
        }

        private void RefreshSkillsWindow()
        {
            _assignHint.text = _pendingAssign != null
                ? "Tap a bar slot to place it.  (tap Cancel to stop)"
                : "Tap 'To bar', then tap a slot.  Press and HOLD a slot for Move / Remove / Auto.";

            if (!_skillsPanel.gameObject.activeSelf) return;

            _skillsHeader.text = "SP " + Boot.SkillPoints + "     " + GameConstants.CurrencyName + " " + Boot.Gold;
            for (int i = 0; i < _skillsTabButtons.Length; i++)
                _skillsTabButtons[i].targetGraphic.color = i == _skillsTab ? UiKit.Accent : UiKit.PanelLight;

            // Rebuild only when something that changes the LIST changed. Rows carry captured ids and
            // registered listeners, so a per-frame rebuild would leak both.
            int revision = _skillsTab * 7919 + Boot.Learned.Count * 31 + Boot.SkillPoints
                         + (Boot.ActiveClass != null ? Boot.ActiveClass.Level * 13 : 0)
                         + (_pendingAssign != null ? _pendingAssign.GetHashCode() : 0)
                         + BarStamp();
            if (revision == _skillsRevision) return;
            _skillsRevision = revision;

            for (int i = _skillsContent.childCount - 1; i >= 0; i--)
                Destroy(_skillsContent.GetChild(i).gameObject);

            if (_skillsTab == 0) BuildKnownTab();
            else if (_skillsTab == 1) BuildLearnTab();
            else BuildActionsTab();
        }

        private void BuildKnownTab()
        {
            if (Boot.Learned.Count == 0)
            {
                Note("No skills learned yet — see the Learn tab.");
                return;
            }

            // Grouped by CATEGORY and sorted by name inside each, like the WPF panel — a flat
            // alphabetical list of forty skills is unreadable on a phone.
            var known = Boot.Learned.Keys
                .Select(id => SkillCatalog.Get(id))
                .Where(d => d != null)
                .OrderBy(d => d.Category).ThenBy(d => d.Name);

            SkillCategory? current = null;
            foreach (var def in known)
            {
                if (current == null || current.Value != def.Category)
                {
                    current = def.Category;
                    Note(CategoryName(def.Category));
                }

                string level = def.MaxLevel > 1 ? "  Lv." + Boot.Learned[def.Id] : "";
                string token = def.Id;

                // Two ways to be passive, and BOTH have to be checked: a PassiveEffect (Passive is a
                // nullable effect, not a flag) or the Passive category. Testing only the effect let
                // category-only passives onto the bar, where they sit as buttons that can never do
                // anything.
                bool passive = def.Passive != null || def.Category == SkillCategory.Passive;
                bool onBar = Boot.SkillBar != null && System.Array.IndexOf(Boot.SkillBar, def.Id) >= 0;

                if (passive)
                {
                    // A passive has nothing to press and nowhere to be placed.
                    Row(SkillLetters(def) + "  " + def.Name + level, null, null, UiKit.TextDim,
                        def.Id, Boot.Learned[def.Id]);
                    continue;
                }

                // "Use" here as well as on the Actions tab: casting from the list is the natural thing
                // to try, and requiring a bar slot first is a detour.
                //
                // "To bar" goes DISABLED once the skill is on the bar, replacing the old "* on bar"
                // text. The state belongs to the control that acts on it — a greyed button says "no,
                // and here is why" in the place you were about to press.
                Row2Buttons(SkillLetters(def) + "  " + def.Name + level,
                            "Use", () => Boot.UseSlot(token),
                            _pendingAssign == token ? "Cancel" : "To bar",
                            onBar && _pendingAssign != token ? null : (System.Action)(() => BeginAssign(token)),
                            def.Id, Boot.Learned[def.Id]);
            }
        }

        /// <summary>
        /// The next learnable LEVEL of each class skill, grouped by the character level that unlocks
        /// it — mirroring the WPF panel. Hidden: skills something you already know supersedes, and
        /// skills locked out by an exclusive group (the stat-swap passives are a permanent choice).
        /// </summary>
        private void BuildLearnTab()
        {
            var active = Boot.ActiveClass;
            if (active == null) { Note("Waiting for your class ..."); return; }

            var archetype = active.SecondClass > 0 ? ClassCatalog.Get(active.SecondClass)?.Archetype : null;
            var discipline = active.ThirdClass > 0 ? ThirdClassCatalog.Get(active.ThirdClass)?.Discipline : null;

            var all = ClassSkills.LearnableAt(active.Race, active.BaseClass, archetype, int.MaxValue, discipline);

            var groups = all
                .Where(cs => cs.SkillLevel == Boot.Learned.GetValueOrDefault(cs.SkillId) + 1
                             && !Superseded(cs.SkillId)
                             && !LockedByExclusiveGroup(cs.SkillId))
                .GroupBy(cs => cs.LearnLevel)
                .OrderBy(g => g.Key);

            bool any = false;
            foreach (var group in groups)
            {
                any = true;
                bool levelMet = active.Level >= group.Key;
                Note("Level " + group.Key + (levelMet ? "" : "   (locked)"));

                foreach (var cs in group)
                {
                    var def = SkillCatalog.Get(cs.SkillId);
                    if (def == null) continue;

                    int sp = def.SpCostAt(cs.SkillLevel);
                    int gold = def.GoldCostAt(cs.SkillLevel);   // stat-swap passives are bought with GOLD
                    bool canLearn = levelMet && Boot.SkillPoints >= sp && (gold == 0 || Boot.Gold >= gold);

                    string levelTag = def.MaxLevel > 1 ? "  Lv." + cs.SkillLevel : "";
                    string price = gold > 0 ? "(" + gold.ToString("N0") + " " + GameConstants.CurrencyName + ")"
                                            : "(SP " + sp + ")";
                    string id = def.Id;

                    // Detail shows the level you would GET, not the one you have — the numbers should
                    // match the purchase being considered.
                    Row(SkillLetters(def) + "  " + def.Name + levelTag + "   " + price,
                        "Learn",
                        canLearn ? (System.Action)(() => { Boot.LearnSkill(id); _skillsRevision = -1; }) : null,
                        canLearn ? UiKit.Text : UiKit.TextDim,
                        def.Id, cs.SkillLevel);
                }
            }

            if (!any) Note("Nothing left to learn for this class right now.");
        }

        /// <summary>
        /// The built-in actions. Each row offers BOTH "Use" and "To bar": an action is a thing you do
        /// (sit, follow, target closest), and requiring a trip through the bar to do it once is a
        /// detour — you cannot try one without first spending a slot on it.
        /// </summary>
        private void BuildActionsTab()
        {
            foreach (var action in ActionCatalog.All)
            {
                string token = GameConstants.ActionSlotToken(action.Id);
                Row2Buttons(Abbreviations.For(action.Name) + "  " + action.Name,
                            "Use", () => Boot.UseSlot(token),
                            "To bar", () => BeginAssign(token));
            }
        }

        /// <summary>True when a skill you already know REPLACES this one (Magic Bolt → Flame Bolt), so
        /// it should not be offered again.</summary>
        private bool Superseded(string skillId)
        {
            foreach (var known in Boot.Learned.Keys)
            {
                var def = SkillCatalog.Get(known);
                if (def?.Replaces == null) continue;
                foreach (var replaced in def.Replaces)
                    if (replaced == skillId) return true;
            }
            return false;
        }

        /// <summary>Exclusive groups are permanent build choices — one of the set, ever. Offering a
        /// second one would just get a refusal from the server.</summary>
        private bool LockedByExclusiveGroup(string skillId)
        {
            var def = SkillCatalog.Get(skillId);
            if (def == null) return false;

            if (!string.IsNullOrEmpty(def.ExclusiveGroup) && !Boot.Learned.ContainsKey(skillId))
            {
                foreach (var known in Boot.Learned.Keys)
                {
                    var other = SkillCatalog.Get(known);
                    if (other != null && other.ExclusiveGroup == def.ExclusiveGroup) return true;
                }
            }
            return SkillCatalog.StatSwapConflict(skillId, new List<string>(Boot.Learned.Keys)) != null;
        }

        /// <summary>Cheap stamp of what is ON the bar, so the "• on bar" marks refresh the moment a
        /// skill is placed rather than at the next unrelated change.</summary>
        private int BarStamp()
        {
            var bar = Boot.SkillBar;
            if (bar == null) return 0;
            int stamp = 17;
            for (int i = 0; i < bar.Length; i++)
                if (!string.IsNullOrEmpty(bar[i])) stamp = stamp * 31 + i + bar[i].Length;
            return stamp;
        }

        private static string CategoryName(SkillCategory category)
        {
            switch (category)
            {
                case SkillCategory.Physical: return "Physical";
                case SkillCategory.Magic:    return "Magic";
                case SkillCategory.Buff:     return "Buffs";
                case SkillCategory.Debuff:   return "Debuffs";
                case SkillCategory.Heal:     return "Heals";
                case SkillCategory.Passive:  return "Passives";
                default:                     return category.ToString();
            }
        }

        /// <summary>
        /// The LETTERS for a skill square. Deliberately NOT <c>def.Icon</c>: those icons are emoji,
        /// and the font TMP ships with (LiberationSans) has no emoji glyphs — every one of them would
        /// draw as the hollow "missing glyph" box, which is what the ✕ close buttons were.
        ///
        /// Emoji need a TMP font asset with an emoji fallback, which has to be generated in the
        /// Editor. Until that exists, letters are honest; boxes are not.
        /// </summary>
        internal static string SkillLetters(SkillDef def)
        {
            return string.IsNullOrWhiteSpace(def.Abbrev) ? Abbreviations.For(def.Name) : def.Abbrev;
        }

        private void Note(string text)
        {
            var label = UiKit.Label(_skillsContent, text, 16f, UiKit.TextDim);
            label.gameObject.AddComponent<LayoutElement>().minHeight = 30f;
        }

        /// <param name="detailSkill">Skill id whose details the ROW opens when tapped, or null for a
        /// row with nothing to explain (an action). The row itself is the target rather than a small
        /// "?" button — the name is what someone reaches for when they want to know more.</param>
        private void Row(string text, string buttonText, System.Action onClick, Color colour,
                         string detailSkill = null, int detailLevel = 1)
        {
            var row = UiKit.Box(_skillsContent, "Row", UiKit.PanelLight);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 44f;

            if (detailSkill != null)
            {
                var open = row.gameObject.AddComponent<Button>();
                open.targetGraphic = row;
                string id = detailSkill;
                int level = detailLevel;
                open.onClick.AddListener(() => ShowSkillDetail(id, level));
            }

            var label = UiKit.Label(row.transform, text, 16f, colour, TextAlignmentOptions.Left);
            UiKit.Stretch(UiKit.Rect(label.gameObject), 12f, 0f, buttonText != null ? 120f : 12f, 0f);

            if (buttonText == null) return;

            var button = UiKit.TextButton(row.transform, buttonText, onClick, 15f);
            button.interactable = onClick != null;
            UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-8f, 0f), new Vector2(104f, 36f));
        }

        /// <summary>A row with TWO buttons on the right. Same shape as <see cref="Row"/>, which keeps
        /// its single-button form rather than growing an optional-second-button parameter that every
        /// other caller would have to pass null for. A null handler renders its button DISABLED, which
        /// is how "already on the bar" is shown.</summary>
        private void Row2Buttons(string text, string leftText, System.Action onLeft,
                                 string rightText, System.Action onRight,
                                 string detailSkill = null, int detailLevel = 1)
        {
            var row = UiKit.Box(_skillsContent, "Row", UiKit.PanelLight);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 44f;

            if (detailSkill != null)
            {
                var open = row.gameObject.AddComponent<Button>();
                open.targetGraphic = row;
                string id = detailSkill;
                int level = detailLevel;
                open.onClick.AddListener(() => ShowSkillDetail(id, level));
            }

            var label = UiKit.Label(row.transform, text, 16f, UiKit.Text, TextAlignmentOptions.Left);
            UiKit.Stretch(UiKit.Rect(label.gameObject), 12f, 0f, 220f, 0f);

            var right = UiKit.TextButton(row.transform, rightText, onRight, 15f);
            right.interactable = onRight != null;
            UiKit.Place(UiKit.Rect(right.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-8f, 0f), new Vector2(104f, 36f));

            var left = UiKit.TextButton(row.transform, leftText, onLeft, 15f);
            left.interactable = onLeft != null;
            UiKit.Place(UiKit.Rect(left.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-118f, 0f), new Vector2(84f, 36f));
        }

        // ----- assigning to the bar --------------------------------------------------------------

        /// <summary>True for a skill that cannot usefully sit on the bar. Checked again at assign
        /// time, not just when drawing the row — the server stores whatever token it is sent, so the
        /// client is the only thing that can keep a passive off the bar.</summary>
        internal static bool IsPassive(string skillId)
        {
            var def = SkillCatalog.Get(skillId);
            return def != null && (def.Passive != null || def.Category == SkillCategory.Passive);
        }

        private void BeginAssign(string token)
        {
            if (IsPassive(token))
            {
                ClientLog.Warn("Passive skills are always on — they can't go on the bar.");
                return;
            }

            // Tapping the same entry twice cancels, so there is always a way out of the mode without
            // committing to a slot.
            _pendingAssign = _pendingAssign == token ? null : token;
        }

        /// <summary>Called when a bar slot is tapped. Returns true when it consumed the tap to place a
        /// pending skill, so the caller knows not to also CAST the slot.</summary>
        private bool TryPlacePending(int barIndex)
        {
            if (_pendingAssign == null) return false;
            Boot.AssignSlot(barIndex, _pendingAssign);
            _pendingAssign = null;
            _skillsRevision = -1;
            return true;
        }
    }
}
