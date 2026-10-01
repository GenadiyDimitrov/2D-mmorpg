using System.Collections.Generic;
using System.Text;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the SKILL TREE window — `BL-330` step 2, the in-game twin of docs/design/SkillTree.html.
    ///
    /// Owner, 2026-10-01: *"we just can make a full skill tree at any class master. When opening each time it preselects
    /// whatever u have (u can change and compare with other classes)"* — and the same at character creation, preselected on
    /// the race and class being picked. So it is NEVER locked: it opens on you and lets you look anywhere.
    ///
    /// Read-only, and built LOCALLY from <see cref="SkillTreeData"/> in Game.Shared — the builder the page uses — the same
    /// way the Learn tab builds from the compiled class tables. No server round trip, and the page and the game cannot
    /// disagree. A class-table change therefore needs a new APK, like the Learn tab.
    ///
    /// It hangs off the ROOT canvas, not the world root, because character select has no world yet.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _treePanel, _treeContent;
        private ScrollRect _treeScroll;
        /// <summary>The pick: race index (== Races.Length is the Swaps &amp; Sigils tab), Fighter/Mage, 2nd, 3rd.</summary>
        private int _treeRace, _treeBase, _treeSecond, _treeThird;

        private const string TreeExtras = "Swaps & Sigils";

        private void BuildSkillTree()
        {
            _treePanel = UiKit.PanelBox(_root, "SkillTree");
            UiKit.Place(_treePanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(760f, 560f));
            var inner = _treePanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_treePanel, "Skill Tree", () => CloseWindow(_treePanel));

            _treeContent = UiKit.ScrollArea(inner, out _treeScroll, 4f);
            UiKit.Stretch((RectTransform)_treeScroll.transform, 16f, chrome + 8f, 16f, 16f);

            _treePanel.gameObject.SetActive(false);
        }

        /// <summary>Open the tree on a class: race and Fighter/Mage always, the 2nd and 3rd when known (null = the first
        /// path, which is just where the reader starts looking).</summary>
        private void OpenSkillTree(Race race, BaseClass baseClass, Archetype? archetype = null, Discipline? discipline = null)
        {
            _treeRace = System.Math.Max(0, System.Array.IndexOf(SkillTreeData.Races, race));
            var bases = SkillTreeData.For(race);
            _treeBase = System.Math.Max(0, System.Array.FindIndex(bases, b => b.BaseClass == baseClass));
            var seconds = bases[_treeBase].Seconds;
            _treeSecond = archetype is Archetype a ? System.Math.Max(0, System.Array.FindIndex(seconds, s => s.Archetype == a)) : 0;
            var thirds = seconds.Length > 0 ? seconds[_treeSecond].Thirds : new SkillTreeThird[0];
            _treeThird = discipline is Discipline d ? System.Math.Max(0, System.Array.FindIndex(thirds, t => t.Discipline == d)) : 0;
            RenderSkillTree();
            OpenWindow(_treePanel);
            _treeScroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>The class master's button: the tree on THIS character's class, every step it already took.</summary>
        private void OpenSkillTreeForMe()
        {
            var a = Boot != null ? Boot.ActiveClass : null;
            if (a == null) { OpenSkillTree(Race.Human, BaseClass.Fighter); return; }
            OpenSkillTree(a.Race, a.BaseClass,
                          a.SecondClass > 0 ? ClassCatalog.Get(a.SecondClass)?.Archetype : null,
                          a.ThirdClass > 0 ? ThirdClassCatalog.Get(a.ThirdClass)?.Discipline : null);
        }

        private void RenderSkillTree()
        {
            for (int i = _treeContent.childCount - 1; i >= 0; i--)
                Destroy(_treeContent.GetChild(i).gameObject);

            var raceNames = new List<string>();
            foreach (var r in SkillTreeData.Races) raceNames.Add(r.ToString());
            raceNames.Add(TreeExtras);
            TreePicker("Race", raceNames, _treeRace, i => { _treeRace = i; _treeBase = _treeSecond = _treeThird = 0; });

            if (_treeRace >= SkillTreeData.Races.Length) { RenderTreeExtras(); return; }

            var race = SkillTreeData.Races[_treeRace];
            var bases = SkillTreeData.For(race);
            var bc = bases[Mathf.Clamp(_treeBase, 0, bases.Length - 1)];
            var names = new List<string>();
            foreach (var b in bases) names.Add(b.BaseClass.ToString());
            TreePicker("Start as", names, _treeBase, i => { _treeBase = i; _treeSecond = _treeThird = 0; });

            if (bc.Seconds.Length == 0) return;
            var second = bc.Seconds[Mathf.Clamp(_treeSecond, 0, bc.Seconds.Length - 1)];
            names.Clear();
            foreach (var s in bc.Seconds) names.Add(s.Name);
            TreePicker("2nd class", names, _treeSecond, i => { _treeSecond = i; _treeThird = 0; });

            SkillTreeThird third = null;
            if (second.Thirds.Length > 0)
            {
                third = second.Thirds[Mathf.Clamp(_treeThird, 0, second.Thirds.Length - 1)];
                names.Clear();
                foreach (var t in second.Thirds) names.Add(t.ThirdName + " > " + t.FourthName);
                TreePicker("3rd class", names, _treeThird, i => _treeThird = i);
            }

            if (!string.IsNullOrEmpty(second.Blurb)) TreeText(second.Blurb, 15f, UiKit.TextDim);

            TreeSection(race + " " + bc.BaseClass, "1st class  1-19", null, bc.First);
            TreeSection("Kept for life", "every " + race + " " + bc.BaseClass.ToString().ToLowerInvariant(),
                        "The race skills and grade passives stay through every class change.", bc.Life);
            TreeSection(second.Name, "2nd class  20-39", null, second.Second);
            if (third != null)
            {
                TreeSection(third.ThirdName, "3rd class  40-75", null, third.Third);
                TreeSection(third.FourthName, "4th class  76+", "After the Rite of Ascension.", third.Fourth);
            }
            TreeSection("Every 4th class", "shared",
                        "Learned by every class once ascended. The sigils are on the " + TreeExtras + " tab.",
                        SkillTreeData.Shared4th());
        }

        private void RenderTreeExtras()
        {
            TreeText("In no path: both are bought on their own shelf, by every race.", 15f, UiKit.TextDim);
            TreeSection("Stat swaps", "40+  3rd class",
                        "Each rank moves one point from one stat to another: at most +5 on any stat and 9 ranks in all, "
                        + "paid in gold, the price rising with the ranks you already own.", SkillTreeData.Swaps());
            foreach (var g in SkillTreeData.Sigils())
                TreeSection(g.Name + " sigils", "76+  ascended",
                            "You take one Attack, one Defence and one Support sigil, from any flavour.", g.Skills);
        }

        /// <summary>One picker row: a caption, then the choices as buttons sharing the width, four to a line. The chosen one
        /// is lit. Picking re-renders the window from the top of the tree.</summary>
        private void TreePicker(string caption, List<string> choices, int selected, System.Action<int> pick)
        {
            const int perLine = 4;
            const float captionW = 104f, lineH = 42f;
            for (int start = 0; start < choices.Count; start += perLine)
            {
                var line = TreeBlock("Picker", lineH);
                if (start == 0)
                {
                    var label = UiKit.Label(line, caption, 14f, UiKit.TextDim, TextAlignmentOptions.Left);
                    UiKit.Place(UiKit.Rect(label.gameObject), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                new Vector2(4f, 0f), new Vector2(captionW - 8f, lineH));
                }
                int count = System.Math.Min(perLine, choices.Count - start);
                for (int k = 0; k < count; k++)
                {
                    int index = start + k;
                    var button = UiKit.TextButton(line, choices[index], () => { pick(index); RenderSkillTree(); }, 15f);
                    button.targetGraphic.color = index == selected ? UiKit.TabActive : UiKit.PanelLight;
                    var rt = UiKit.Rect(button.gameObject);
                    // Fractions of the width after the caption, so the row fits whatever width the window has.
                    rt.anchorMin = new Vector2((float)k / perLine, 0f);
                    rt.anchorMax = new Vector2((float)(k + 1) / perLine, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.offsetMin = new Vector2(captionW * (1f - (float)k / perLine) + 3f, 3f);
                    rt.offsetMax = new Vector2(captionW * (1f - (float)(k + 1) / perLine) - 3f, -3f);
                }
            }
        }

        /// <summary>A section: heading, its level band, an optional note, then one row per skill.</summary>
        private void TreeSection(string title, string band, string note, SkillTreeSkill[] skills)
        {
            TreeBlock("Gap", 8f);
            TreeText("<b>" + title + "</b>   <size=14><color=#" + ColorUtility.ToHtmlStringRGB(UiKit.Accent) + ">" + band
                     + "</color></size>", 20f, UiKit.Text);
            if (!string.IsNullOrEmpty(note)) TreeText(note, 14f, UiKit.TextDim);
            if (skills.Length == 0) { TreeText("Nothing authored here yet.", 15f, UiKit.TextDim); return; }
            foreach (var sk in skills) TreeSkillRow(sk);
        }

        /// <summary>A skill: icon, first learn level, name, kind. Tapping it shows (or hides) one line per learn level with
        /// that rung's own text, right under the row — his §120a: *"each row to show its own descirpion -> to compare
        /// powers"*.</summary>
        private void TreeSkillRow(SkillTreeSkill sk)
        {
            var row = UiKit.Box(_treeContent, "Row", UiKit.PanelLight);
            SetHeight(UiKit.Rect(row.gameObject), 44f);

            bool passive = sk.Category == SkillCategory.Passive;
            float textLeft = RowIcon(row.transform, SkillSprite(sk.Id), passive);
            var name = UiKit.Label(row.transform,
                "<color=#" + ColorUtility.ToHtmlStringRGB(UiKit.Accent) + ">Lv " + sk.FirstLevel + "</color>   " + sk.Name,
                16f, UiKit.Text, TextAlignmentOptions.Left);
            UiKit.Stretch(UiKit.Rect(name.gameObject), textLeft, 0f, 110f, 0f);
            var kind = UiKit.Label(row.transform, sk.Category.ToString().ToUpperInvariant(), 12f, KindColour(sk.Category),
                                   TextAlignmentOptions.Right);
            UiKit.Place(UiKit.Rect(kind.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-10f, 0f), new Vector2(96f, 30f));

            TextMeshProUGUI detail = null;
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row;
            button.onClick.AddListener(() =>
            {
                if (detail == null)
                {
                    detail = TreeText(RungText(sk), 15f, UiKit.Text);
                    detail.margin = new Vector4(52f, 2f, 8f, 6f);
                    detail.transform.SetSiblingIndex(row.transform.GetSiblingIndex() + 1);
                }
                else detail.gameObject.SetActive(!detail.gameObject.activeSelf);
            });
        }

        /// <summary>One line per rung. Rungs sharing a learn level (the swaps' five at 40) read by rank.</summary>
        private static string RungText(SkillTreeSkill sk)
        {
            var levels = new HashSet<int>();
            foreach (var r in sk.Rungs) levels.Add(r.Level);
            bool byRank = levels.Count < sk.Rungs.Length;
            string accent = ColorUtility.ToHtmlStringRGB(UiKit.Accent);
            var text = new StringBuilder();
            if (!string.IsNullOrEmpty(sk.Tag)) text.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(UiKit.TextDim))
                                                   .Append('>').Append(sk.Tag).Append("</color>\n");
            foreach (var r in sk.Rungs)
            {
                text.Append("<color=#").Append(accent).Append('>').Append(byRank ? "Rank " + r.Rung : "Level " + r.Level)
                    .Append("</color><indent=86>");
                if (!string.IsNullOrEmpty(r.Name)) text.Append("<b>").Append(r.Name).Append("</b>  ");
                text.Append(string.IsNullOrEmpty(r.Text) ? "-" : r.Text).Append("</indent>\n");
            }
            return text.ToString().TrimEnd('\n');
        }

        private static Color KindColour(SkillCategory c)
        {
            switch (c)
            {
                case SkillCategory.Physical: return new Color(0.88f, 0.52f, 0.45f, 1f);
                case SkillCategory.Magic:    return new Color(0.52f, 0.68f, 0.91f, 1f);
                case SkillCategory.Buff:     return new Color(0.49f, 0.80f, 0.59f, 1f);
                case SkillCategory.Debuff:   return new Color(0.83f, 0.54f, 0.81f, 1f);
                case SkillCategory.Heal:     return new Color(0.45f, 0.81f, 0.79f, 1f);
                default:                     return UiKit.TextDim;
            }
        }

        /// <summary>A wrapped text block in the tree's column that sizes itself to its text.</summary>
        private TextMeshProUGUI TreeText(string text, float size, Color colour)
        {
            var label = UiKit.Label(_treeContent, text, size, colour, TextAlignmentOptions.TopLeft);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.margin = new Vector4(4f, 2f, 4f, 2f);
            label.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return label;
        }

        /// <summary>An empty fixed-height block in the tree's column (a picker line, a gap).</summary>
        private Transform TreeBlock(string name, float height)
        {
            var box = UiKit.Box(_treeContent, name, new Color(0f, 0f, 0f, 0f), blocksInput: false);
            SetHeight(UiKit.Rect(box.gameObject), height);
            return box.transform;
        }

        /// <summary>The scroll column does not control its children's heights, so each one carries its own.</summary>
        private static void SetHeight(RectTransform rt, float height)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = height;
        }
    }
}
