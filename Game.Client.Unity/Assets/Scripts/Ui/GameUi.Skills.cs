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
        private int _skillsTab;                 // 0 known, 1 learn, 2 actions, 3 stats
        private Button[] _skillsTabButtons;
        private int _skillsRevision = -1;

        /// <summary>Rungs STAGED on the Stats tab but not yet paid for: pair skill id -> how many more.
        /// The tab is a planning pad — nothing here has cost anything until Confirm, which is the point
        /// of it ("before u confirm a selection to show what u are changing").</summary>
        private readonly Dictionary<string, int> _swapStage = new Dictionary<string, int>();

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

            _skillsTabButtons = new Button[4];
            string[] names = { "Known", "Learn", "Actions", "Stats" };
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

            BuildLearnConfirm();
        }

        // ----- learn confirmation ----------------------------------------------------------------

        private RectTransform _learnPanel;
        private TextMeshProUGUI _learnTitle, _learnBody;
        private System.Action _learnAction;

        private void BuildLearnConfirm()
        {
            _learnPanel = UiKit.PanelBox(_worldRoot, "LearnConfirm");
            UiKit.Place(_learnPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -10f), new Vector2(540f, 360f));
            var inner = _learnPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_learnPanel, "Learn skill", () => CloseWindow(_learnPanel));

            _learnTitle = UiKit.Label(inner, "", 19f, UiKit.Accent, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_learnTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 8f), new Vector2(490f, 26f));

            ScrollRect scroll;
            var content = UiKit.ScrollArea(inner, out scroll, 2f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 42f, 16f, 70f);
            _learnBody = UiKit.Label(content, "", 16f, UiKit.Text, TextAlignmentOptions.TopLeft);
            _learnBody.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var cancel = UiKit.TextButton(inner, "Cancel", () => CloseWindow(_learnPanel), 16f);
            UiKit.Place(UiKit.Rect(cancel.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(18f, 14f), new Vector2(230f, 46f));

            var confirm = UiKit.TextButton(inner, "Confirm", () =>
            {
                var act = _learnAction;
                CloseWindow(_learnPanel);
                act?.Invoke();
            }, 16f);
            UiKit.Place(UiKit.Rect(confirm.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-18f, 14f), new Vector2(230f, 46f));

            _learnPanel.gameObject.SetActive(false);
        }

        /// <summary>Why this row can't be bought yet, in the player's terms. Checked in the same order
        /// the server checks them, so the message matches what a Learn would actually be refused for.</summary>
        private string LearnBlockedReason(SkillDef def, int levelGate, bool levelMet, int sp, long gold)
        {
            if (!levelMet)
                return def.Name + " requires level " + levelGate + " — you are " + (Boot.ActiveClass?.Level ?? 0) + ".";
            if (Boot.SkillPoints < sp)
                return def.Name + " costs " + sp + " SP — you have " + Boot.SkillPoints + ".";
            if (gold > 0 && Boot.Gold < gold)
                return def.Name + " costs " + gold.ToString("N0") + " " + GameConstants.CurrencyName
                     + " — you have " + Boot.Gold.ToString("N0") + ".";
            return def.Name + " can't be learned right now.";
        }

        /// <summary>The owner's §7: never spend SP blind. Show what the purchase changes — for an
        /// UPGRADE the before→after of the numbers that move (power, MP), for a brand-new skill what it
        /// does — plus the cost, behind a Confirm.</summary>
        private void ConfirmLearn(SkillDef def, int newLevel, int sp, long gold)
        {
            _learnTitle.text = def.Name + (def.MaxLevel > 1 ? "   Lv." + newLevel : "");

            var t = new System.Text.StringBuilder();
            string desc = def.DescriptionAt(newLevel);
            if (!string.IsNullOrWhiteSpace(desc)) t.AppendLine(desc).AppendLine();

            int cur = newLevel - 1;
            if (cur >= 1)   // an upgrade — show the deltas
            {
                LearnChange(t, "Power", def.PowerAt(cur), def.PowerAt(newLevel));
                LearnChange(t, "MP cost", def.MpCostAt(cur), def.MpCostAt(newLevel));
            }
            else            // brand-new skill — show the level-1 numbers
            {
                if (def.PowerAt(newLevel) > 0)  t.AppendLine("Power   " + def.PowerAt(newLevel));
                if (def.MpCostAt(newLevel) > 0) t.AppendLine("MP cost   " + def.MpCostAt(newLevel));
            }

            // A PASSIVE has no Power and no MP cost, so everything above skips it and the page you were
            // spending SP on said nothing but its prose. Its numbers live in the PassiveEffect/mastery
            // tables — state them, and for an upgrade state the level you have next to the one you'd buy.
            LearnEffects(t, "Now", def, cur);
            LearnEffects(t, cur >= 1 ? "After" : "Effect", def, newLevel);

            t.AppendLine();
            t.AppendLine(gold > 0 ? "Cost:  " + gold.ToString("N0") + " " + GameConstants.CurrencyName
                                  : "Cost:  " + sp + " SP");

            _learnBody.text = t.ToString().TrimEnd();
            string id = def.Id;
            _learnAction = () => { Boot.LearnSkill(id); _skillsRevision = -1; };
            OpenWindow(_learnPanel);
        }

        /// <summary>Every numeric effect a skill has at one LEVEL, on one line per source. Silent when
        /// the level doesn't exist (level 0 = "you don't have it yet") or carries no numbers.</summary>
        private static void LearnEffects(System.Text.StringBuilder t, string label, SkillDef def, int level)
        {
            if (level < 1) return;
            var lines = SkillText.Passive(def.PassiveAt(level) ?? default);
            if (def.ArmorMasteryAt(level) is ArmorMasteryProfile armor) lines.AddRange(SkillText.ArmorMastery(armor));
            if (def.WeaponMasteryAt(level) is WeaponMasteryProfile weapon) lines.AddRange(SkillText.WeaponMastery(weapon));
            lines.AddRange(SkillText.Buff(def, level));
            if (lines.Count == 0) return;
            t.AppendLine(label + "   " + string.Join(", ", lines));
        }

        // "->" not "→": the bundled LiberationSans has no arrow glyph (same reason the close button is X).
        private static void LearnChange(System.Text.StringBuilder t, string label, int from, int to)
        {
            if (from == to) { if (to != 0) t.AppendLine(label + "   " + to); }
            else t.AppendLine(label + "   " + from + "  ->  " + to);
        }

        private void RefreshSkillsWindow()
        {
            _assignHint.text = _pendingAssign != null
                ? "Tap a bar slot to place it.  (tap Cancel to stop)"
                : "Tap 'To bar', then tap a slot.  Press and HOLD a slot for Move / Remove / Auto.";

            if (!_skillsPanel.gameObject.activeSelf) return;

            _skillsHeader.text = "SP " + Boot.SkillPoints + "     " + GameConstants.CurrencyName + " " + Boot.Gold;
            for (int i = 0; i < _skillsTabButtons.Length; i++)
                _skillsTabButtons[i].targetGraphic.color = i == _skillsTab ? UiKit.TabActive : UiKit.PanelLight;

            // Rebuild only when something that changes the LIST changed. Rows carry captured ids and
            // registered listeners, so a per-frame rebuild would leak both.
            int revision = _skillsTab * 7919 + Boot.Learned.Count * 31 + Boot.SkillPoints
                         + (Boot.ActiveClass != null ? Boot.ActiveClass.Level * 13 : 0)
                         + (_pendingAssign != null ? _pendingAssign.GetHashCode() : 0)
                         + BarStamp() + SwapStageStamp();
            if (revision == _skillsRevision) return;
            _skillsRevision = revision;

            for (int i = _skillsContent.childCount - 1; i >= 0; i--)
                Destroy(_skillsContent.GetChild(i).gameObject);

            if (_skillsTab == 0) BuildKnownTab();
            else if (_skillsTab == 1) BuildLearnTab();
            else if (_skillsTab == 2) BuildActionsTab();
            else BuildStatsTab();
        }

        /// <summary>Cheap stamp of the staged basket, so a [+] redraws the row, the price and the
        /// "Added:" line on the same frame it is pressed. Levels alone would not do it — the LEARNED
        /// levels are untouched until Confirm.</summary>
        private int SwapStageStamp()
        {
            int stamp = 23;
            foreach (var kv in _swapStage) stamp += kv.Key.Length * 131 + kv.Value * 7919;
            return stamp;
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
                    // Stat swaps are bought with GOLD, and priced by how many rungs you already own
                    // rather than per level — the same computation the server charges, so the shelf
                    // price and the bill agree. Everything else uses its own authored per-level cost.
                    long gold = SkillCatalog.StatSwapOf(cs.SkillId) is not null
                        ? SkillCatalog.StatSwapPriceRange(
                              SkillCatalog.StatSwapRungsOwned(Boot.Learned),
                              SkillCatalog.StatSwapRungsOwned(Boot.Learned)
                                  + (cs.SkillLevel - (Boot.Learned.TryGetValue(cs.SkillId, out int have) ? have : 0)))
                        : def.GoldCostAt(cs.SkillLevel);
                    bool canLearn = levelMet && Boot.SkillPoints >= sp && (gold == 0 || Boot.Gold >= gold);

                    string levelTag = def.MaxLevel > 1 ? "  Lv." + cs.SkillLevel : "";
                    string price = gold > 0 ? "(" + gold.ToString("N0") + " " + GameConstants.CurrencyName + ")"
                                            : "(SP " + sp + ")";

                    // Detail shows the level you would GET, not the one you have — the numbers should
                    // match the purchase being considered.
                    var learnDef = def;
                    int learnLevel = cs.SkillLevel;

                    // The button is ALWAYS wired. It used to be `canLearn ? action : null`, which made an
                    // unaffordable row a dead button: tapping Learn did nothing at all, with no message,
                    // which is indistinguishable from the feature being broken (and was reported as
                    // exactly that). When you can't buy it yet, say WHY.
                    int lvlGate = group.Key;
                    System.Action act = canLearn
                        ? () => ConfirmLearn(learnDef, learnLevel, sp, gold)
                        : () => ClientLog.Warn(LearnBlockedReason(learnDef, lvlGate, levelMet, sp, gold));

                    Row(SkillLetters(def) + "  " + def.Name + levelTag + "   " + price,
                        "Learn", act,
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

        // ----- the STATS tab (BL-03) ---------------------------------------------------------------

        /// <summary>How a swap stat is spelled on screen. MEN is the one that does not spell itself:
        /// the id still says "men" because it is persisted and append-only, but Spirit replaced it and
        /// SPT is what the player's stat sheet calls it (his own line reads "SPT -8").</summary>
        private static string SwapLabel(SkillCatalog.SwapStat s) =>
            s == SkillCatalog.SwapStat.Men ? "SPT" : s.ToString().ToUpperInvariant();

        /// <summary>
        /// The Stats tab: buy the level-40 stat swaps as a BASKET you can see before you pay for it.
        ///
        /// <para>His complaint was that the Learn tab made this "a bit chaotic" — twelve pair-shaped
        /// skill rows, each priced separately, with the thing you actually care about (where your stats
        /// end up) written nowhere. So this tab is built round the two numbers that matter: the running
        /// "Added:" line, and the total the basket will cost.</para>
        ///
        /// <para>Staging is FREE. [+] and [-] only ever move the plan, and the only limits on them are
        /// the two rung caps — gold is asked once, at Confirm. That is why [-] can never take back a
        /// rung you have already paid for: un-committing is the Mindwriter's job, it is free there, and
        /// it drops a whole pair at once, which is not something a [-] button should imply.</para>
        /// </summary>
        private void BuildStatsTab()
        {
            var active = Boot.ActiveClass;
            if (active == null) { Note("Waiting for your class ..."); return; }

            var discipline = active.ThirdClass > 0 ? ThirdClassCatalog.Get(active.ThirdClass)?.Discipline : null;
            var shelf = SkillCatalog.StatSwapsFor(active.BaseClass, discipline).ToList();

            if (active.Level < SkillCatalog.StatSwapLearnLevel)
            {
                Note("Stat swaps unlock at level " + SkillCatalog.StatSwapLearnLevel
                     + " — you are " + active.Level + ".");
                Note("Each rung moves +1 into one stat and -1 out of another. "
                     + SkillCatalog.StatSwapMaxTotal + " rungs, at most +"
                     + SkillCatalog.StatSwapMaxPerStat + " on any one stat.");
                return;
            }

            // What the character WOULD have if the basket were bought. Every cap question below is
            // asked of this, not of Boot.Learned — a staged rung has to count against the budget or the
            // tab would happily plan a build the server must then refuse.
            var projected = new Dictionary<string, int>(Boot.Learned);
            foreach (var kv in _swapStage)
                projected[kv.Key] = Boot.Learned.GetValueOrDefault(kv.Key) + kv.Value;

            int owned = SkillCatalog.StatSwapRungsOwned(Boot.Learned);
            int staged = 0;
            foreach (var kv in _swapStage) staged += kv.Value;
            long total = SkillCatalog.StatSwapPriceRange(owned, owned + staged);

            Note("Rungs   " + owned + " / " + SkillCatalog.StatSwapMaxTotal + " committed"
                 + (staged > 0 ? "     + " + staged + " selected" : ""));
            Note(owned + staged >= SkillCatalog.StatSwapMaxTotal
                ? "Next price   -   all " + SkillCatalog.StatSwapMaxTotal + " rungs are spoken for."
                : "Next price   " + SkillCatalog.StatSwapRungPrice(owned + staged).ToString("N0")
                  + " " + GameConstants.CurrencyName);

            foreach (var id in shelf)
            {
                var def = SkillCatalog.Get(id);
                if (def == null || SkillCatalog.StatSwapOf(id) is not { } pair) continue;

                int paid = Boot.Learned.GetValueOrDefault(id);
                int plan = _swapStage.GetValueOrDefault(id);
                int at = paid + plan;

                // The pair, spelled as the two numbers it moves — which is what the row is FOR. A pair
                // sitting at zero still shows "+1 / -1" so the shelf reads as a menu of trades.
                string moves = at > 0
                    ? SwapLabel(pair.Up) + " +" + at + "   " + SwapLabel(pair.Down) + " -" + at
                    : SwapLabel(pair.Up) + " +1   " + SwapLabel(pair.Down) + " -1";

                bool canAdd = SkillCatalog.StatSwapConflict(id, at + 1, projected) == null;
                string capturedId = id;

                SwapRow(def.Name + "      " + moves,
                        plan > 0 ? UiKit.Accent : at > 0 ? UiKit.Text : UiKit.TextDim,
                        id, Mathf.Max(1, at),
                        paid, plan,
                        plan > 0 ? (System.Action)(() => StageSwap(capturedId, -1)) : null,
                        canAdd ? (System.Action)(() => StageSwap(capturedId, +1)) : null);
            }

            // The running line he asked for by name: "Added: WIT +5 | ATK +3 | SPT -8". Raises first,
            // then the sacrifices — read top to bottom it is the build, not the receipt.
            var net = SkillCatalog.StatSwapNet(projected);
            var moved = net.Where(kv => kv.Value != 0).OrderByDescending(kv => kv.Value).ToList();
            Note(moved.Count == 0
                ? "Added:   nothing yet."
                : "Added:   " + string.Join("  |  ",
                      moved.Select(kv => SwapLabel(kv.Key) + " " + (kv.Value > 0 ? "+" : "") + kv.Value)));

            if (staged == 0)
            {
                Note("Pick rungs with [+]. Nothing is charged until you confirm.");
                return;
            }

            bool affordable = Boot.Gold >= total;
            Row2Buttons("Confirm " + staged + (staged == 1 ? " rung" : " rungs") + "   "
                        + total.ToString("N0") + " " + GameConstants.CurrencyName,
                        "Clear", () => { _swapStage.Clear(); _skillsRevision = -1; },
                        "Confirm",
                        affordable
                            ? (System.Action)ConfirmSwapBasket
                            : () => ClientLog.Warn("That costs " + total.ToString("N0") + " "
                                    + GameConstants.CurrencyName + " — you have " + Boot.Gold.ToString("N0") + "."));

            Note("[-] only takes back a selection. A rung you have PAID for is undone at the "
                 + "Mindwriter, free, a whole pair at a time.");
        }

        private void StageSwap(string skillId, int delta)
        {
            int now = _swapStage.GetValueOrDefault(skillId) + delta;
            if (now <= 0) _swapStage.Remove(skillId);
            else _swapStage[skillId] = now;
            _skillsRevision = -1;
        }

        /// <summary>Send the basket. Cleared optimistically: the affordability and both caps have
        /// already been checked against the same shared helpers the server uses, so a refusal here means
        /// the world moved under us — and the server says so in chat, with nothing spent.</summary>
        private void ConfirmSwapBasket()
        {
            var picks = _swapStage.Select(kv => new StatSwapPurchaseDto(kv.Key, kv.Value)).ToArray();
            if (picks.Length == 0) return;
            Boot.BuyStatSwaps(picks);
            _swapStage.Clear();
            _skillsRevision = -1;
        }

        /// <summary>A row with a [-] COUNT [+] stepper on the right. Its own helper rather than a third
        /// parameter set on <see cref="Row2Buttons"/>: the middle is a LABEL, not a button, and a
        /// stepper that looked like two more buttons in a row of buttons is exactly the "chaotic"
        /// reading this tab exists to fix.</summary>
        private void SwapRow(string text, Color colour, string detailSkill, int detailLevel,
                             int paid, int planned, System.Action onMinus, System.Action onPlus)
        {
            var row = UiKit.Box(_skillsContent, "SwapRow", UiKit.PanelLight);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 46f;

            if (detailSkill != null)
            {
                var open = row.gameObject.AddComponent<Button>();
                open.targetGraphic = row;
                string id = detailSkill;
                int level = detailLevel;
                open.onClick.AddListener(() => ShowSkillDetail(id, level));
            }

            var label = UiKit.Label(row.transform, text, 16f, colour, TextAlignmentOptions.Left);
            UiKit.Stretch(UiKit.Rect(label.gameObject), 12f, 0f, 210f, 0f);

            var plus = UiKit.TextButton(row.transform, "+", onPlus, 18f);
            plus.interactable = onPlus != null;
            UiKit.Place(UiKit.Rect(plus.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-8f, 0f), new Vector2(56f, 36f));

            // "2 (+1)" — what you own, and what you are about to add, kept apart. One merged number
            // would hide which part of the build is already paid for and which is still a plan.
            var count = UiKit.Label(row.transform,
                planned > 0 ? paid + " (+" + planned + ")" : paid.ToString(),
                15f, planned > 0 ? UiKit.Accent : UiKit.TextDim, TextAlignmentOptions.Center);
            UiKit.Place(UiKit.Rect(count.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-68f, 0f), new Vector2(76f, 30f));

            var minus = UiKit.TextButton(row.transform, "-", onMinus, 18f);
            minus.interactable = onMinus != null;
            UiKit.Place(UiKit.Rect(minus.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-146f, 0f), new Vector2(56f, 36f));
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

            // Stat swaps are exempt from the exclusive-group block — their limits are numeric now
            // (+5 per stat, 9 rungs), and a stat may legitimately be raised by two different pairs.
            bool isSwap = SkillCatalog.StatSwapOf(skillId) is not null;
            if (!isSwap && !string.IsNullOrEmpty(def.ExclusiveGroup) && !Boot.Learned.ContainsKey(skillId))
            {
                foreach (var known in Boot.Learned.Keys)
                {
                    var other = SkillCatalog.Get(known);
                    if (other != null && other.ExclusiveGroup == def.ExclusiveGroup) return true;
                }
            }
            int want = (Boot.Learned.TryGetValue(skillId, out int lvl) ? lvl : 0) + 1;
            return SkillCatalog.StatSwapConflict(skillId, want, Boot.Learned) != null;
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
