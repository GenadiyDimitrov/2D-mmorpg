using System;
using System.Collections.Generic;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: everything shown while in world — nameplates, the self and target panels,
    /// the action bar, the command line, the skill bar, the log console, the bag, the debug panel and
    /// the back-button confirm dialog.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _worldRoot;

        // nameplates
        private RectTransform _nameplateLayer;
        private readonly List<Nameplate> _nameplates = new List<Nameplate>();

        /// <summary>
        /// How far ABOVE an entity's origin the nameplate is anchored, in world units.
        ///
        /// Straight down at 90° this barely matters — the plate lands on the marker either way and the
        /// pivot does the work. It matters the moment the camera tilts: at 2.5D a plate anchored at
        /// the feet drifts down over the body, so this is the knob that puts it back on the head.
        /// </summary>
        public float NameplateHeight = 0.9f;

        /// <summary>Screen-space gap between the entity and the bottom of its plate, so neither the
        /// name nor the HP bar is drawn across the marker itself.</summary>
        private const float PlateGap = 12f;

        // self / target
        // _targetName is gone (playtest 23) — the target frame's title bar carries the name now, so the
        // row it used to sit in was 28px of duplication.
        private TextMeshProUGUI _selfName, _targetDetail;
        private TextMeshProUGUI _selfHpText, _selfMpText, _selfXpText, _targetHpText, _targetMpText;
        private Image _selfHp, _selfMp, _selfXp, _targetHp, _targetMp;
        private RectTransform _targetMpRow;
        private RectTransform _targetPanel;

        // command bar
        private TMP_InputField _commandField;

        // skill bar
        private const int BarColumns = 6, BarRows = 2;
        private const int SlotsPerPage = BarColumns * BarRows;
        private const int BarPages = GameConstants.SkillBarSlots / SlotsPerPage;
        /// <summary>Side of one square. A field, not a local, because the reuse overlay resizes itself
        /// against it every frame and the two must not drift apart.</summary>
        private const float SlotSize = 78f;
        private RectTransform _skillBarPanel;
        private readonly Button[] _slotButtons = new Button[SlotsPerPage];
        private readonly TextMeshProUGUI[] _slotFaces = new TextMeshProUGUI[SlotsPerPage];
        private readonly Image[] _slotBorders = new Image[SlotsPerPage];
        /// <summary>The "this stance is ON" ring — one per square, drawn OUTSIDE the green auto ring so
        /// a slot can carry both marks at once (owner, playtest 28: *"toggle skill on the skill bar
        /// should be marked with aqua border or different color (not so bright just different form the
        /// rest) when 'on'"*). It answers a question the buff bar technically already answered and the
        /// thumb could not: the toggle is where you press it, the buff square is somewhere else.</summary>
        private readonly Image[] _slotToggleBorders = new Image[SlotsPerPage];
        private readonly TextMeshProUGUI[] _slotCancel = new TextMeshProUGUI[SlotsPerPage];
        private readonly Image[] _slotReuse = new Image[SlotsPerPage];
        private readonly RectTransform[] _slotReuseRects = new RectTransform[SlotsPerPage];
        private readonly TextMeshProUGUI[] _slotReuseText = new TextMeshProUGUI[SlotsPerPage];
        private readonly TextMeshProUGUI[] _slotAutoMarks = new TextMeshProUGUI[SlotsPerPage];
        /// <summary>How many of a consumable slot's item are in the bag (32n). Bottom-LEFT, because the
        /// top-left is the slot number and the bottom-right is the auto "A".</summary>
        private readonly TextMeshProUGUI[] _slotCounts = new TextMeshProUGUI[SlotsPerPage];

        /// <summary>
        /// How long after a cast STARTS before its slot will accept a cancel tap.
        ///
        /// The button that started the cast is under the finger that is still coming up, and a double
        /// tap on a skill is a natural thing to do — without this, the second tap of a double tap
        /// would start the cast and immediately cancel it, costing the initial MP and the full
        /// cooldown for nothing. The X is drawn from the start (so it is visible) but inert until this
        /// elapses.
        /// </summary>
        private const float CastCancelGrace = 0.35f;

        // slot context menu (press and hold)
        private RectTransform _slotMenu, _slotMenuScrim;
        private Button _slotMenuAuto, _slotMenuDetail;
        private int _menuSlot = -1;        // page-relative slot the menu belongs to
        private int _pendingMoveFrom = -1; // absolute bar index being moved, or -1
        private TextMeshProUGUI _pageLabel;
        private int _barPage;
        private int _swipeFinger = -1;
        private float _swipeStartX;

        // console / chat
        /// <summary>
        /// One append-only VIEW of the <see cref="ClientLog"/> buffer: a scroll area plus the
        /// bookkeeping that lets it draw only the lines it has not drawn yet.
        ///
        /// There are two of them since D5 — the Chat window and the Combat window — over the ONE
        /// buffer, which is what keeps every line's arrival order intact and costs one list. It is a
        /// type rather than two sets of fields because the append/trim/clear-generation dance below is
        /// subtle (see <see cref="RefreshLogView"/>: a `while (childCount > cap)` loop froze the
        /// phone), and a copy-pasted second one would eventually drift back into the slow path.
        /// </summary>
        private sealed class LogView
        {
            public RectTransform Panel, Content;
            public ScrollRect Scroll;
            /// <summary>Which lines this view draws.</summary>
            public Func<ClientLog.Tab, bool> Accepts;
            public int SeenRevision = -1;
            public long RenderedSeq = -1;   // highest ClientLog.Line.Seq already drawn as a row
            public int SeenClearGen = -1;   // last ClientLog.ClearGeneration we rebuilt for
        }

        private LogView _chatView, _combatView;

        /// <summary>Which chat tab is showing. -1 is ALL (no filter); otherwise the
        /// <see cref="ClientLog.Tab"/> being shown on its own.</summary>
        private int _chatTab = -1;
        private Button[] _chatTabButtons;
        private readonly int[] _chatTabValues =
        {
            -1, (int)ClientLog.Tab.Local, (int)ClientLog.Tab.World,
            (int)ClientLog.Tab.Whisper, (int)ClientLog.Tab.System,
        };
        private Button _chatReplyButton;
        /// <summary>The Chat window's 6th button. It is NOT a tab — it toggles the Combat window (D5),
        /// and lights up while that window is open.</summary>
        private Button _combatTabButton;
        /// <summary>Cap on console ROWS kept alive. Plenty of scrollback, but bounded so the window can
        /// never accumulate hundreds of live labels — see RefreshConsole for why that mattered.</summary>
        private const int ConsoleDisplayRows = 120;

        // bag / debug
        private RectTransform _bagPanel, _bagContent, _debugPanel;
        private int _bagRevision = -1;
        private ItemCategory _bagTab = ItemCategory.All;   // which C8 category the list is filtered to
        private Button[] _bagTabButtons;
        private static readonly ItemCategory[] BagTabs =
            { ItemCategory.All, ItemCategory.Gear, ItemCategory.Use, ItemCategory.Mats, ItemCategory.Quest };
        private Button _bagDelToggle;
        private bool _bagFastDel;            // when on, each row shows a no-confirm Del button
        private Button _bagEquipToggle;      // expands the paper-doll column (worn gear) beside the list
        private bool _bagEquipOpen;
        private const float BagWidthCollapsed = 460f, BagWidthExpanded = 792f, BagHeight = 560f;
        /// <summary>The bag is TALLER with the paper-doll open. C8's second tab row pushed the column
        /// down 36px, and the column's own content (body squares → jewel row → the three preset rows)
        /// ends 438px below its top — 50px more than the 388 the window had left under the tabs. With
        /// no mask on a PanelBox that is not clipping: preset C simply drew outside the window, over
        /// the world, un-tappable where you expected it. The list keeps its own height either way,
        /// so only the paper-doll side uses the extra room.</summary>
        private const float BagHeightExpanded = 620f;
        /// <summary>Left inset of the bag's item list when the equip column is CLOSED.</summary>
        private const float BagListX = 16f;
        /// <summary>How far the list slides right when the equip column opens on its left — exactly the
        /// width the window gains, so the list keeps its position relative to the RIGHT edge.</summary>
        private const float BagEquipColumnWidth = BagWidthExpanded - BagWidthCollapsed;
        private RectTransform _bagListRect;
        /// <summary>The window chrome height, kept so the toggle can re-derive the list's height when
        /// the window grows for the paper-doll instead of leaving 60px of dead space under the list.</summary>
        private float _bagChrome;
        private TextMeshProUGUI _bagGoldLabel, _bagSlotsLabel;
        private static readonly Color GoldColour = new Color(0.95f, 0.82f, 0.35f);

        /// <summary>
        /// Open windows, oldest first. The back button pops the LAST one opened, so closing walks back
        /// through the panels in the order you opened them; only when nothing is left does it offer to
        /// quit. Every future window (skills, character sheet, shops, party …) joins this by calling
        /// <see cref="OpenWindow"/> — nothing else needs to know about the back button.
        /// </summary>
        private readonly List<RectTransform> _windows = new List<RectTransform>();

        /// <summary>Is a window that belongs to an NPC CONVERSATION open? Movement is locked while one
        /// is (owner, playtest-19 M13): a ground tap queued before opening the gatekeeper walked you out
        /// of range, and the teleport you then chose answered "Too far" — the server re-checks the
        /// distance on every dialog action, so any of these windows is only valid where you stand.
        ///
        /// Deliberately NOT keyed on `Boot.DialogNpcId`: that stays set until CloseDialog, and the
        /// vendor/keeper/gatekeeper panels close by their own ✕ without clearing it — which would leave
        /// the player unable to move with nothing on screen to explain it.</summary>
        public bool NpcWindowOpen =>
            IsOpen(_dialogPanel) || IsOpen(_vendorPanel) || IsOpen(_buyBackPanel)
            || IsOpen(_warehousePanel) || IsOpen(_learnPanel);

        private static bool IsOpen(RectTransform panel) => panel != null && panel.gameObject.activeSelf;

        private void OpenWindow(RectTransform panel)
        {
            if (panel == null) return;
            _windows.Remove(panel);        // re-opening moves it back to the top of the stack
            _windows.Add(panel);
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();      // and draws above the ones opened before it

            // The debug window is BUILT once and its contents are rebuilt per tab, so opening it after
            // levelling/swapping class would otherwise show whatever was true when it was last closed.
            if (panel == _debugPanel) RefreshDebugPanel();
        }

        private void CloseWindow(RectTransform panel)
        {
            if (panel == null) return;
            _windows.Remove(panel);
            panel.gameObject.SetActive(false);
        }

        private void ToggleWindow(RectTransform panel)
        {
            if (panel == null) return;
            if (panel.gameObject.activeSelf) CloseWindow(panel);
            else OpenWindow(panel);
        }

        /// <summary>Close the most recently opened window. False when there was none.</summary>
        private bool CloseTopWindow()
        {
            // Walk from the top: a panel closed by its own ✕ leaves a stale entry behind, and the back
            // button must not appear to do nothing while it eats those.
            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                var panel = _windows[i];
                _windows.RemoveAt(i);
                if (panel != null && panel.gameObject.activeSelf)
                {
                    panel.gameObject.SetActive(false);
                    return true;
                }
            }
            return false;
        }

        private Button _pvpButton, _autoButton, _respawnButton;
        private Button _targetPartyButton, _targetTradeButton, _targetInfoButton, _targetTalkButton;
        private Button _targetAttackButton, _targetFollowButton, _targetAssistButton;
        private RectTransform _menuPanel;

        // confirm dialog
        private RectTransform _confirmPanel;
        private TextMeshProUGUI _confirmText;
        private Button _confirmOkButton;
        private Action _confirmAction;
        private int _lastBackFrame = -1;
        private bool _quitConfirmed;

        // ----- build -----------------------------------------------------------------------------

        private void BuildWorld()
        {
            // Both of these are full-screen INVISIBLE containers, so neither may absorb raycasts —
            // otherwise every tap would land on "the UI" and the character would never walk anywhere.
            _worldRoot = UiKit.Rect(UiKit.Box(_root, "World", new Color(0, 0, 0, 0), blocksInput: false).gameObject);
            UiKit.Stretch(_worldRoot, 0f, 0f, 0f, 0f);
            // First child = drawn first = BEHIND the panels. Nameplates belong under the UI, not over it.
            _nameplateLayer = UiKit.Rect(UiKit.Box(_worldRoot, "Nameplates", new Color(0, 0, 0, 0), blocksInput: false).gameObject);
            UiKit.Stretch(_nameplateLayer, 0f, 0f, 0f, 0f);

            BuildSelfPanel();
            BuildTargetPanel();
            BuildSkillBar();
            BuildCommandBar();
            BuildActionBar();
            BuildConsole();
            BuildBag();
            BuildSkillsWindow();
            BuildDebugPanel();
            BuildTradePanel();
            BuildFeedback();
            BuildSkillDetail();
            BuildStatsWindow();
            BuildTargetWindow();
            BuildSettingsWindow();
            BuildOptionsWindow();
            BuildQuestWindow();
            BuildDialogWindow();
            BuildPartyWindow();
            BuildAutoHuntWindows();
            BuildItemWindows();
            BuildVendorWindow();
            BuildCraftingWindow();
            BuildWarehouseWindow();
            BuildBuyBackWindow();
            BuildRestoreWindow();
            BuildRankWindow();
            BuildRegionUi();
            BuildSlotMenu();
        }

        /// <summary>
        /// Name, level, and three bars with their numbers ON them — the ordinary shape of a game HUD.
        ///
        /// The numbers used to be a separate line under the bars, which meant reading a value took two
        /// glances at two places and cost a row of the panel. Bars are also taller now: 18px was fine
        /// for a plain fill but not with text in it, and the EXP bar's 10px could not hold a digit at
        /// all. Nothing else lives here — the panel is VITALS ONLY.
        /// </summary>
        private void BuildSelfPanel()
        {
            var panel = UiKit.PanelBox(_worldRoot, "SelfPanel");
            UiKit.Place(panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -48f), new Vector2(330f, 114f));
            var inner = panel.GetChild(0);

            // TAPPING YOUR OWN PANEL TARGETS YOU.
            //
            // 🔑 It opened the character sheet until playtest 28, and the sheet was the wrong thing to
            // put there: *"clicking on myself (name upper left) should targets me … as a healer in a
            // party it's hard to target urself fast from the window, and now outside party u cannot
            // target ursel at all"*. He is right that it was impossible — TouchInput refuses a world
            // tap on your own body (`!view.IsSelf`, so your own collider can never steal a tap meant
            // for the ground under your feet), and the party window only exists when you are in a
            // party. A solo healer had NO way to select himself, which on a bar full of ally-targeted
            // skills is the difference between a heal landing and a cast being thrown away.
            //
            // This panel is the obvious door: it is your name, it is always on screen, and it is
            // nowhere near the skill bar. The character sheet moved to a [Char] button of its own —
            // first inside the bag beside [Equip], and since 2026-08-28 on the ACTION BAR between
            // [Bag] and [Skills], because the bag version cost two taps every time.
            //
            // The button is on the BORDER object, so the whole panel is the target rather than a
            // strip of it.
            var open = panel.gameObject.AddComponent<Button>();
            open.targetGraphic = panel.GetComponent<Image>();
            open.onClick.AddListener(() =>
            {
                if (Boot.SelfId != Guid.Empty) Boot.TargetId = Boot.SelfId;
            });

            _selfName = UiKit.Label(inner, "waiting for your entity ...", 19f);
            UiKit.Place(UiKit.Rect(_selfName.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -6f), new Vector2(300f, 24f));

            _selfHp = UiKit.ValueBar(inner, UiKit.Hp);
            UiKit.Place(UiKit.Rect(_selfHp.transform.parent.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -34f), new Vector2(306f, 22f));
            _selfHpText = UiKit.BarLabel(_selfHp, 13f);

            _selfMp = UiKit.ValueBar(inner, UiKit.Mp);
            UiKit.Place(UiKit.Rect(_selfMp.transform.parent.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -60f), new Vector2(306f, 22f));
            _selfMpText = UiKit.BarLabel(_selfMp, 13f);

            _selfXp = UiKit.ValueBar(inner, UiKit.Xp);
            UiKit.Place(UiKit.Rect(_selfXp.transform.parent.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -86f), new Vector2(306f, 18f));
            _selfXpText = UiKit.BarLabel(_selfXp, 11f);
        }

        /// <summary>
        /// The target frame: movable, with the standard chrome, and its ✕ is what DESELECTS.
        ///
        /// The two jobs WPF's ESC was doing are split here, at the owner's call:
        ///   back / ESC  → walks the WINDOW STACK only, and never touches the target. Closing the bag
        ///                 mid-fight must not drop the mob you are hitting.
        ///   this X      → clears the target, which also hides this panel (it is shown whenever there
        ///                 IS a target). Deselecting is deliberate, so it costs a deliberate tap.
        /// </summary>
        private void BuildTargetPanel()
        {
            // TOP-CENTRE by default (owner). The target is what you are looking at, so it belongs
            // where your eyes already are — above your character — rather than in a corner. It is
            // movable, so this is only the starting position.
            _targetPanel = UiKit.PanelBox(_worldRoot, "TargetPanel");
            // 🔴 28px SHORTER since playtest 23, because the name row is gone: *"the current name text
            // can be removed so the title window be smaller in size."* The title bar carries the name now.
            //
            // 🔴 …AND 12px TALLER AGAIN (playtest 24, `87d`): *"the text 'mob:..' is hidden ... the 1st
            // text is half hidden."* He read it as the title row, but the arithmetic says it was the
            // BOTTOM: the detail line ran 94→114 from the top while the first button row, being
            // bottom-anchored, ran 76→104 in a 148-tall panel — so Attack covered the top half of
            // "Mob: 44, Aggressive". That collision was the other half of the same 28px shrink: the top
            // rows moved up with the deleted name row, and the bottom-anchored buttons moved up with the
            // panel's floor, straight into them. Fixed twice over — one BUTTON ROW instead of two (five
            // of the seven buttons have been permanently hidden since playtest 23, so the second row
            // held one button and a gap), and 12px of room so the gap is real rather than a tie.
            UiKit.Place(_targetPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -48f), new Vector2(300f, 160f));
            var inner = _targetPanel.GetChild(0);

            // Deliberately NOT CloseWindow: this panel is not in the stack, and hiding it while the
            // target still existed would only make it reappear on the next frame.
            // 🔑 The title is a PLACEHOLDER — RefreshTarget overwrites it with the target's own name
            // every frame (*"put the name in place of `Target`. The title of the window to be the
            // targets name"*). It is only ever seen for the split second before the first refresh.
            float chrome = UiKit.WindowChrome(_targetPanel, "Target", () => Boot.TargetId = null);

            _targetHp = UiKit.ValueBar(inner, UiKit.Hp);
            UiKit.Place(UiKit.Rect(_targetHp.transform.parent.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -chrome - 6f), new Vector2(276f, 22f));
            _targetHpText = UiKit.BarLabel(_targetHp, 13f);

            // MP bar — shown for PLAYER targets only (owner). A mob's mana tells you nothing you can act
            // on; another player's is what tells a healer whether they can still cast.
            _targetMp = UiKit.ValueBar(inner, UiKit.Mp);
            _targetMpRow = UiKit.Rect(_targetMp.transform.parent.gameObject);
            UiKit.Place(_targetMpRow, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -chrome - 28f), new Vector2(276f, 18f));
            _targetMpText = UiKit.BarLabel(_targetMp, 12f);

            // 🔴 THE DETAIL LINE IS FULL-WIDTH NOW. It was 190px against a 300px panel, which is the
            // *"the type mob/player is half visible"* he reported — and it is about to hold a great deal
            // more than a kind: `Mob: 44, Aggressive, Social (wolf)` / `Player: Vagabond`.
            _targetDetail = UiKit.Label(inner, "", 14f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(_targetDetail.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -chrome - 48f), new Vector2(276f, 20f));

            // ONE row of contextual action buttons (`87d`), so every target command is one tap — no slash
            // typing. The server refuses anything invalid, but RefreshTarget only SHOWS the ones that
            // apply to the current target (enemy vs player) so the frame stays honest. Three across.
            //
            // ⚠ It was two rows until playtest 24, and the SECOND row is what overlapped the detail line.
            // It can afford to be one because RefreshTarget hides five of these seven outright: since
            // playtest 23 a mob shows Attack + Info, an NPC shows Talk, and a player shows nothing at all.
            // Follow / Assist / Party / Trade are kept as dead fields rather than deleted (the Actions tab
            // of the Skills window owns those verbs now) — ⚠ if one is ever un-hidden it needs a slot in
            // THIS row and 94px more panel width, not the old y=44 row, which would land on the text again.
            float bx0 = 10f, bx1 = 104f, bx2 = 198f, bw = 88f;
            const float rowY = 8f;

            _targetAttackButton = UiKit.TextButton(inner, "Attack", () =>
            {
                // The same verb as the second tap and the bar's Attack action — never its own logic.
                if (Boot.TargetId.HasValue) Boot.AttackOrFollow(Boot.TargetId.Value);
            }, 14f);
            UiKit.Place(UiKit.Rect(_targetAttackButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(bx0, rowY), new Vector2(bw, 28f));

            _targetFollowButton = UiKit.TextButton(inner, "Follow", () =>
            {
                if (Boot.TargetId.HasValue) Boot.Follow(Boot.TargetId.Value);
            }, 14f);
            UiKit.Place(UiKit.Rect(_targetFollowButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(bx1, rowY), new Vector2(bw, 28f));

            _targetAssistButton = UiKit.TextButton(inner, "Assist", () =>
            {
                if (Boot.TargetId.HasValue) Boot.Assist(Boot.TargetId.Value);
            }, 14f);
            UiKit.Place(UiKit.Rect(_targetAssistButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(bx2, rowY), new Vector2(bw, 28f));

            // The four hidden ones share slots with the three live ones on purpose — they are never
            // active at the same time, so overlapping rects cost nothing and the row stays three wide.
            _targetPartyButton = UiKit.TextButton(inner, "Party", () =>
            {
                if (Boot.TargetId.HasValue) Boot.PartyInvite(Boot.TargetId.Value);
            }, 14f);
            UiKit.Place(UiKit.Rect(_targetPartyButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(bx0, rowY), new Vector2(bw, 28f));

            _targetTradeButton = UiKit.TextButton(inner, "Trade", () =>
            {
                if (Boot.TargetId.HasValue)
                {
                    var id = Boot.TargetId.Value;
                    Boot.Trade(n => n.TradeRequestAsync(id), "request");
                }
            }, 14f);
            UiKit.Place(UiKit.Rect(_targetTradeButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(bx1, rowY), new Vector2(bw, 28f));

            _targetInfoButton = UiKit.TextButton(inner, "Info", OpenTargetDetails, 14f);
            UiKit.Place(UiKit.Rect(_targetInfoButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(bx1, rowY), new Vector2(bw, 28f));

            // NPCs only (C9 / M13). It shares the Info slot — the two never show together, since Info is
            // a mob's stats-and-drops window and an NPC has neither.
            _targetTalkButton = UiKit.TextButton(inner, "Talk", () =>
            {
                if (Boot.TargetId.HasValue) Boot.ApproachAndTalk(Boot.TargetId.Value);
            }, 14f);
            UiKit.Place(UiKit.Rect(_targetTalkButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(bx1, rowY), new Vector2(bw, 28f));
            _targetTalkButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Two rows of six on the right: one PAGE of the server's 60-slot bar, swipe or the ‹ › arrows
        /// to move between the five pages. The bar is rendered VERBATIM and never written back — see
        /// GameBoot.SkillBar for why that rule is absolute.
        /// </summary>
        private void BuildSkillBar()
        {
            const float slot = SlotSize, pad = 6f;
            float w = BarColumns * slot + (BarColumns + 1) * pad;
            float h = BarRows * slot + (BarRows + 1) * pad + 26f;

            _skillBarPanel = UiKit.PanelBox(_worldRoot, "SkillBar");
            UiKit.Place(_skillBarPanel, new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-12f, 14f), new Vector2(w, h));
            var inner = _skillBarPanel.GetChild(0);

            for (int i = 0; i < SlotsPerPage; i++)
            {
                int index = i;   // captured; the loop variable is shared
                var at = new Vector2(pad + (i % BarColumns) * (slot + pad),
                                     -(pad + (i / BarColumns) * (slot + pad)));

                // The auto-use marker: a THIN green frame drawn behind the slot, peeking out 2px.
                //
                // It used to peek 3px, and because the slot's own fill is slightly transparent the
                // green also washed through the whole face — so an auto slot read as a green BUTTON
                // rather than a marked one. The frame is thinner now, the slot face below is opaque so
                // nothing bleeds through, and the actual "this repeats" signal is the small green A in
                // the corner (see below) rather than colour over the whole square.
                // The TOGGLE-ON ring, OUTSIDE the auto ring (peeks 5px to the auto ring's 2px), so a
                // stance that is both on and auto-marked shows an aqua edge around a green one instead
                // of one hiding the other. Added first = drawn first = furthest back.
                //
                // The colour is deliberately muted (his words: "not so bright just different form the
                // rest"). A saturated cyan on a dark bar reads as an ALERT, and a stance being on is
                // the opposite of an alert — it is the state you meant to be in.
                var onBorder = UiKit.Box(inner, "ToggleBorder", new Color(0.30f, 0.68f, 0.72f), blocksInput: false);
                UiKit.Place(UiKit.Rect(onBorder.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            at + new Vector2(-5f, 5f), new Vector2(slot + 10f, slot + 10f));
                onBorder.enabled = false;
                _slotToggleBorders[i] = onBorder;

                var border = UiKit.Box(inner, "AutoBorder", new Color(0.35f, 0.85f, 0.40f), blocksInput: false);
                UiKit.Place(UiKit.Rect(border.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            at + new Vector2(-2f, 2f), new Vector2(slot + 4f, slot + 4f));
                border.enabled = false;
                _slotBorders[i] = border;

                // No onClick: PressAndHold owns both gestures, so a hold can open the menu without
                // the button also casting what was in it on release.
                var button = UiKit.TextButton(inner, "", null, 20f);
                button.targetGraphic.color = new Color(0.17f, 0.20f, 0.24f, 1f);   // opaque: see above
                var press = button.gameObject.AddComponent<PressAndHold>();
                press.OnTap = () => FireSlot(index);
                press.OnHold = () => OpenSlotMenu(index);
                press.Enabled = () => button.interactable;
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            at, new Vector2(slot, slot));
                _slotButtons[i] = button;
                _slotFaces[i] = button.GetComponentInChildren<TextMeshProUGUI>();

                // Slot number, top-left, like the WPF squares.
                var hotkey = UiKit.Label(button.transform, (i + 1).ToString(), 12f, UiKit.TextDim);
                UiKit.Place(UiKit.Rect(hotkey.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(4f, -2f), new Vector2(20f, 16f));

                // "A" = the auto-hunt repeats this one. Bottom-RIGHT, because the top-left corner is
                // the slot number and the middle is the face. The font TMP ships with has no recycle
                // glyph (nor any other symbol outside ASCII — that is what drew the hollow boxes), so
                // a letter is the honest version of the icon the owner asked for.
                var auto = UiKit.Label(button.transform, "A", 13f, new Color(0.40f, 0.95f, 0.45f),
                                       TextAlignmentOptions.BottomRight);
                UiKit.Place(UiKit.Rect(auto.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                            new Vector2(-4f, 2f), new Vector2(20f, 16f));
                auto.gameObject.SetActive(false);
                _slotAutoMarks[i] = auto;

                // How many are left, bottom-LEFT. Only ever shown for an item slot: a potion bar you
                // cannot count is a bar you have to open the bag to trust (32n).
                var count = UiKit.Label(button.transform, "", 13f, new Color(0.95f, 0.88f, 0.55f),
                                        TextAlignmentOptions.BottomLeft);
                UiKit.Place(UiKit.Rect(count.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                            new Vector2(4f, 2f), new Vector2(34f, 16f));
                count.gameObject.SetActive(false);
                _slotCounts[i] = count;

                // ----- REUSE (cooldown) -----------------------------------------------------------
                // A dark sheet over the slot that DRAINS from the top as the reuse runs out, plus the
                // seconds left in the middle. Two signals on purpose: the sheet is the one you read
                // without looking (how much of the bar is dark), the number is the one you read when
                // you are deciding whether to wait. Both are drawn UNDER the cancel X — a cast in
                // progress is the more urgent thing to say about the same square.
                //
                // The sheet shrinks by RESIZING (anchored to the slot's top edge) rather than with a
                // filled Image: a filled Image needs a sprite, and every box in this UI is spriteless.
                var shade = UiKit.Box(button.transform, "Reuse", new Color(0.04f, 0.05f, 0.07f, 0.78f),
                                      blocksInput: false);
                var shadeRect = UiKit.Rect(shade.gameObject);
                shadeRect.anchorMin = new Vector2(0f, 1f);
                shadeRect.anchorMax = new Vector2(1f, 1f);
                shadeRect.pivot = new Vector2(0.5f, 1f);
                shadeRect.anchoredPosition = Vector2.zero;
                shadeRect.sizeDelta = new Vector2(0f, slot);
                shade.gameObject.SetActive(false);
                _slotReuse[i] = shade;
                _slotReuseRects[i] = shadeRect;

                var reuseText = UiKit.Label(button.transform, "", 22f, new Color(1f, 0.86f, 0.55f),
                                            TextAlignmentOptions.Center);
                UiKit.Stretch(UiKit.Rect(reuseText.gameObject), 0f, 0f, 0f, 0f);
                reuseText.gameObject.SetActive(false);
                _slotReuseText[i] = reuseText;

                // The cancel X, drawn over the slot only while THIS skill is the one being cast.
                var cancel = UiKit.Label(button.transform, "X", 34f, new Color(1f, 0.35f, 0.35f),
                                         TextAlignmentOptions.Center);
                UiKit.Stretch(UiKit.Rect(cancel.gameObject), 0f, 0f, 0f, 0f);
                cancel.gameObject.SetActive(false);
                _slotCancel[i] = cancel;
            }

            var prev = UiKit.TextButton(inner, "<", () => PageBy(-1), 18f);
            UiKit.Place(UiKit.Rect(prev.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(pad, 4f), new Vector2(40f, 20f));

            _pageLabel = UiKit.Label(inner, "", 14f, UiKit.TextDim, TextAlignmentOptions.Center);
            UiKit.Place(UiKit.Rect(_pageLabel.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 4f), new Vector2(120f, 20f));

            var next = UiKit.TextButton(inner, ">", () => PageBy(1), 18f);
            UiKit.Place(UiKit.Rect(next.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-pad, 4f), new Vector2(40f, 20f));
        }

        private void BuildCommandBar()
        {
            // Bottom edge now that the action bar has left it, with a little padding off the edge —
            // flush against it is where a phone's own gesture bar lives.
            const float bottom = 14f;

            _commandField = UiKit.InputField(_worldRoot, "say  /  !world  /  /w name msg");
            UiKit.Place(UiKit.Rect(_commandField.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(12f, bottom), new Vector2(620f, 46f));
            // Submit on the keyboard's enter/done key — the mobile keyboard has no Send button of ours.
            _commandField.onSubmit.AddListener(_ => SubmitCommand());

            var send = UiKit.TextButton(_worldRoot, "Send", SubmitCommand, 17f);
            UiKit.Place(UiKit.Rect(send.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(640f, bottom), new Vector2(96f, 46f));

            // "Chat", not "Log": it is the chat window now — the diagnostics live on its System tab.
            var log = UiKit.TextButton(_worldRoot, "Chat", () => ToggleWindow(_chatView.Panel), 17f);
            UiKit.Place(UiKit.Rect(log.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(744f, bottom), new Vector2(90f, 46f));

            // Remember where the row sits so the keyboard lift can be applied as an offset from it.
            _cmdBarRects = new[]
            {
                UiKit.Rect(_commandField.gameObject), UiKit.Rect(send.gameObject), UiKit.Rect(log.gameObject),
            };
            _cmdBarHome = new Vector2[_cmdBarRects.Length];
            for (int i = 0; i < _cmdBarRects.Length; i++)
                _cmdBarHome[i] = _cmdBarRects[i].anchoredPosition;
        }

        private RectTransform[] _cmdBarRects;
        private Vector2[] _cmdBarHome;
        private float _keyboardLift = -1f;

        /// <summary>Extra canvas units the command row clears the soft keyboard by — his 10-20px, taken
        /// at the top of that range because a punch-hole is round and clipping its lower arc is still
        /// clipping. Canvas units, not screen pixels: the canvas scales, so this stays the same visual
        /// gap on every device.</summary>
        private const float KeyboardClearance = 20f;

        /// <summary>Lift the command row above the soft keyboard while it is open.
        ///
        /// Android's keyboard is an OVERLAY — it does not resize the game view and Unity reports no
        /// layout change — so anything pinned to the bottom edge is simply swallowed by it. Nothing was
        /// handling that, which is why the checklist item "the soft keyboard lifts the command bar
        /// instead of covering it" failed: the lift had never been written.
        ///
        /// `TouchScreenKeyboard.area` is in SCREEN pixels while the canvas is scaled to a reference
        /// HEIGHT (matchWidthOrHeight = 1), so the height converts by Reference.y / Screen.height. Some
        /// devices report an empty area for a frame or two after the keyboard opens; rather than let the
        /// bar sit under it, fall back to a conservative 45% of the screen until a real value arrives.</summary>
        private void UpdateKeyboardLift()
        {
            if (_cmdBarRects == null) return;

            float lift = 0f;
            if (TouchScreenKeyboard.visible && Screen.height > 0)
            {
                float px = TouchScreenKeyboard.area.height;
                if (px <= 0f || px >= Screen.height) px = Screen.height * 0.45f;
                lift = px * (UiKit.Reference.y / Screen.height);
                // 🔴 +20 ON TOP OF THE KEYBOARD (playtest 23): *"Also move the chat text box with 10-20
                // pixels more higher. Now it's the middle of the screen and it's under my front camera
                // circle. And I cannot see the first few letters of what I'm typing."*
                // 🔑 It reads as "the middle of the screen" because that is only true WHILE TYPING — the
                // row lives at the bottom edge and this lift is what puts it mid-screen, level with a
                // landscape phone's punch-hole camera. So the clearance belongs here and not in
                // BuildCommandBar, or the row would float 20px off the bottom edge for the rest of the
                // time to fix a problem that only exists with the keyboard up.
                lift += KeyboardClearance;
            }

            if (Mathf.Approximately(lift, _keyboardLift)) return;
            _keyboardLift = lift;

            for (int i = 0; i < _cmdBarRects.Length; i++)
                _cmdBarRects[i].anchoredPosition = _cmdBarHome[i] + new Vector2(0f, lift);
        }

        /// <summary>
        /// The action bar: six buttons TOP-RIGHT in two rows, with the rarely-pressed ones behind a
        /// Menu. Row one is [PvP][Auto][Menu], row two [Bag][Char][Skills].
        ///
        /// It used to be ten buttons across the whole bottom edge, which is the worst place for them
        /// on a phone: that strip is where the thumbs rest and where the chat and skill bar want to
        /// live, and ten equal buttons gave the same prominence to "Bag" (constantly) and "Leave"
        /// (once). Top-right is reachable, out of the way of the thumbs, and the split is by FREQUENCY:
        /// what you press mid-fight stays out, what you press once a session goes in the Menu.
        ///
        /// Char CAME BACK to this bar on 2026-08-28. It was here originally, moved onto the vitals
        /// panel, then into the BAG when the vitals tap became "target myself" — and every move bought
        /// a tap somewhere else. His verdict on the bag version: *"now it's very annoying each time to
        /// open bag, open stats"*. It is a top-level window like the other two, so it gets a top-level
        /// button, and it goes in the MIDDLE so neither Bag nor Skills moves out from under the thumb.
        /// </summary>
        private void BuildActionBar()
        {
            // Attack / Sit-Stand / Walk-Run are not here: they exist in ActionCatalog and belong on the
            // SKILL BAR, placed from the Skills window's Actions tab.
            //
            // Respawn is the exception to the top-right grouping — it appears only while you are DEAD
            // (see RefreshWorld) and needs to be impossible to miss, so it sits in the middle instead
            // of hiding behind a menu at the worst possible moment.
            // TWO ROWS rather than one row of five. Five buttons in a line reached most of the way
            // across the top edge and collided with the target frame in the middle; stacked, they stay
            // in the corner the thumb owns.
            var rows = new List<List<(string Label, Action Click)>>
            {
                new()
                {
                    ("PvP: off",  () => Boot.TogglePvp()),
                    ("Auto: off", () => Boot.ToggleAutoHunt()),
                    ("Menu",      () => ToggleWindow(_menuPanel)),
                },
                new()
                {
                    ("Bag",       () => ToggleWindow(_bagPanel)),
                    ("Char",      () => ToggleWindow(_statsPanel)),
                    ("Skills",    () => ToggleWindow(_skillsPanel)),
                },
            };

            const float gap = 6f, width = 132f, height = 46f;
            float y = -48f;

            foreach (var row in rows)
            {
                float x = -12f;

                // Right to left, so each row grows leftward from the screen edge and the LAST entry
                // ends up nearest the corner — the thumb's easiest reach.
                for (int i = row.Count - 1; i >= 0; i--)
                {
                    var button = UiKit.TextButton(_worldRoot, row[i].Label, row[i].Click, 15f);
                    UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                                new Vector2(x, y), new Vector2(width, height));
                    x -= width + gap;

                    if (row[i].Label == "PvP: off") _pvpButton = button;
                    else if (row[i].Label == "Auto: off") _autoButton = button;
                }

                y -= height + gap;
            }

            _respawnButton = UiKit.TextButton(_worldRoot, "Respawn", () => Boot.Respawn(), 17f);
            UiKit.Place(UiKit.Rect(_respawnButton.gameObject), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -120f), new Vector2(200f, 54f));

            BuildMenuPanel();
        }

        /// <summary>The overflow menu: everything you press once a session rather than once a fight.
        ///
        /// Laid out by <see cref="LayoutMenuPanel"/> rather than at build time, because one entry (Admin)
        /// is only there for staff. Hiding it with fixed offsets left a 52px HOLE between Setup and Leave
        /// and an over-tall panel — the owner's *"don't leave a gap between the buttons, collapse it"*.</summary>
        private readonly List<(Button Button, bool AdminOnly)> _menuButtons = new();
        private bool _menuLaidOutForAdmin;

        private void BuildMenuPanel()
        {
            _menuPanel = UiKit.PanelBox(_worldRoot, "Menu");
            UiKit.Place(_menuPanel, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-12f, -100f), new Vector2(200f, 392f));
            var inner = _menuPanel.GetChild(0);

            var entries = new List<(string Label, Action Click, bool AdminOnly)>
            {
                ("Auto Pots", () => { CloseWindow(_menuPanel); OpenAutoPotions(); }, false),
                ("Auto Farm", () => { CloseWindow(_menuPanel); OpenAutoFarm(); }, false),
                ("Quests", () => { CloseWindow(_menuPanel); ToggleWindow(_questPanel); }, false),
                // Craft is on the MENU rather than at an NPC: a profession is a property of the
                // character, every rarity of material drops in the field, and the one thing you do
                // after a farm run is refine what you picked up — none of which wants a trip to town.
                ("Craft",  () => { CloseWindow(_menuPanel); OpenCraftingWindow(); }, false),
                // Restore lives on the MENU, not at a vendor (C18): binning is a field accident, and an
                // undo you can only reach in town is no undo at all.
                ("Restore", () => { CloseWindow(_menuPanel); OpenRestoreWindow(); }, false),
                ("Rank",   () => { CloseWindow(_menuPanel); OpenRank(); }, false),
                ("Setup",  () => { CloseWindow(_menuPanel); ToggleWindow(_settingsPanel); }, false),
                // Options ≠ Setup: Setup is how the game LOOKS (local, PlayerPrefs), Options is what
                // other players may do to you (server-side, per character) — M2/B11.
                ("Options", () => { CloseWindow(_menuPanel); ToggleWindow(_optionsPanel); }, false),
                // The admin toolbox. It was labelled "Debug" while its commands were compiled out of
                // release builds; they ship now and are gated on the account role, server-side.
                ("Admin",  () => { CloseWindow(_menuPanel); ToggleWindow(_debugPanel); }, true),
                // Offline sits directly above Leave because they are the same decision — "I'm done for
                // now" — with the character either staying in the world hunting or coming out of it.
                ("Offline", () => { CloseWindow(_menuPanel);
                                    Ask("Keep hunting offline?\n\n<size=15>Your character stays in the "
                                        + "world under the autopilot until its offline time runs out. "
                                        + "Set up Auto Farm first.</size>",
                                        "Offline", () => Boot.StartOfflineFarm()); }, false),
                ("Leave",  () => { CloseWindow(_menuPanel); Boot.LeaveWorld(); }, false),
            };

            foreach (var entry in entries)
            {
                var button = UiKit.TextButton(inner, entry.Label, entry.Click, 16f);
                _menuButtons.Add((button, entry.AdminOnly));
                // (the admin entry is found by its AdminOnly flag in _menuButtons — no field needed)
            }

            _menuLaidOutForAdmin = !Boot.CanUseAdminTools;   // force the first layout
            LayoutMenuPanel();
            _menuPanel.gameObject.SetActive(false);
        }

        /// <summary>Stack the VISIBLE menu buttons with no holes, and shrink the panel to fit them. Only
        /// re-runs when admin-ness actually changes (it is known at login, and again on a role change), so
        /// this is not per-frame work.</summary>
        private void LayoutMenuPanel()
        {
            bool admin = Boot.CanUseAdminTools;
            // Mirror it for the STATIC card builders (ItemStatsText and friends), which have no Boot.
            // Set before the early-out: the flag has to be right even on the frames where the menu
            // layout has nothing to redo.
            StaffTools = admin;
            if (admin == _menuLaidOutForAdmin) return;
            _menuLaidOutForAdmin = admin;

            const float rowH = 46f, rowStep = 52f, padTop = 10f, padBottom = 14f;
            float y = -padTop;
            int shown = 0;
            foreach (var (button, adminOnly) in _menuButtons)
            {
                bool visible = admin || !adminOnly;
                button.gameObject.SetActive(visible);
                if (!visible) continue;

                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(0f, y), new Vector2(180f, rowH));
                y -= rowStep;
                shown++;
            }
            _menuPanel.sizeDelta = new Vector2(200f, padTop + shown * rowStep + padBottom);
        }

        /// <summary>
        /// The chat window: five tabs over the one log buffer, plus the command box's Reply shortcut.
        ///
        /// It is the old debug console grown up. Chat and diagnostics shared a single undifferentiated
        /// list, so a whisper was one uncoloured line among a hundred warnings — the WPF harness had
        /// colours and tabs and the phone never got them (the oldest open item in the roadmap). System
        /// is now just one of the five tabs, so nothing that used to be visible has been hidden.
        /// </summary>
        private void BuildConsole()
        {
            var panel = UiKit.PanelBox(_worldRoot, "Console");
            UiKit.Place(panel, new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(12f, 132f), new Vector2(760f, 320f));
            var inner = panel.GetChild(0);
            float chrome = UiKit.WindowChrome(panel, "Chat", () => CloseWindow(panel));

            // Tabs. "PM" rather than "Whisper": the row has five buttons across 760px on a phone, and
            // every MMO player already reads PM. The SIXTH, Combat, is not one of them — it opens the
            // combat window (D5) and sits in the row because that is where you would look for it.
            //
            // 🔴 BL-88 — THE ROW MUST FIT AT THE WINDOW'S MINIMUM WIDTH. Playtest 25: *"decreasing the
            // width of the chat leaves the [combat] button floating in the air - make the buttons smaller
            // or like the icons on the top"*. It was six 96px buttons on a 100px step — 608px of row
            // inside a window you are allowed to shrink to 520, so the last button hung outside the
            // frame with nothing behind it. 76 on an 80 step is 488, which fits at the minimum with room
            // to spare and stops the row being a thing you can break by dragging a corner.
            // ⚠ The step and the count are what decide this, so a SEVENTH tab is not free: 12 + n·80 + 76
            // must stay under the MakeAdjustable minimum below.
            const float tabW = 76f, tabStep = 80f, tabFont = 14f;
            string[] names = { "All", "Local", "World", "PM", "System" };
            _chatTabButtons = new Button[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                int value = _chatTabValues[i];
                var button = UiKit.TextButton(inner, names[i], () => SetChatTab(value), tabFont);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(12f + i * tabStep, -chrome - 4f), new Vector2(tabW, 32f));
                _chatTabButtons[i] = button;
            }

            _combatTabButton = UiKit.TextButton(inner, "Combat",
                                                () => { ToggleWindow(_combatView.Panel); HighlightChatTabs(); }, tabFont);
            UiKit.Place(UiKit.Rect(_combatTabButton.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f + names.Length * tabStep, -chrome - 4f), new Vector2(tabW, 32f));

            // 🔴 THE FEED NOW REACHES THE BOTTOM OF THE WINDOW (`87e`(c), playtest 24): *"remove the row
            // with 'clear' and 'replay' — it's now an empty space and the text never gets to the
            // bottom."* The bottom inset was 46 to clear a button row that is gone; it is 10 now, which
            // is the same margin as every other edge. The two buttons moved into the TITLE BAR beside the
            // padlock, as icons — see below.
            ScrollRect scroll;
            var content = UiKit.ScrollArea(inner, out scroll, 1f);
            UiKit.Stretch((RectTransform)scroll.transform, 10f, chrome + 40f, 10f, 10f);

            // 🔴 MOVABLE, RESIZABLE, LOCKABLE — and remembered on the device (playtest 23). The chat and
            // combat windows are the two he named, and they are the right two: they are the only windows
            // that stay open WHILE you play, so they are the only ones whose size is a trade against the
            // view of the world rather than a fit to their own content.
            // ⚠ The minimum is not arbitrary — it is what the six tab buttons need on one row. They are
            // 488px wide since BL-88, so 520 now clears them instead of being 88px short of them.
            UiKit.MakeAdjustable(panel, "chat", new Vector2(520f, 200f));

            // The two ex-buttons, now icons in the title bar (`87e`(c)) — *"move them up beside L/U,
            // clear as a bin, replay as a speech bubble."* Slot 0 is the padlock MakeAdjustable just
            // added, so these take 1 and 2, counting inwards from the close button.
            // ⚠ "Replay" is his word for REPLY: it fills the command box with "/w <last whisperer> ",
            // which is why the icon is a speech bubble and not a rewind.
            UiKit.TitleBarIcon(panel, 1, UiKit.Icon.Bin, () => ClientLog.Clear());
            _chatReplyButton = UiKit.TitleBarIcon(panel, 2, UiKit.Icon.Bubble,
                                                  () => Boot.ComposeWhisper(Boot.LastWhisperName));

            _chatView = new LogView
            {
                Panel = panel, Content = content, Scroll = scroll, Accepts = ChatTabAccepts,
            };
            panel.gameObject.SetActive(false);

            BuildCombatWindow();
            HighlightChatTabs();
        }

        /// <summary>
        /// The COMBAT window (D5): the damage / loot / exp feed, pulled out of the System tab.
        ///
        /// A window of its own rather than a sixth tab, because the point is to read it AT THE SAME
        /// TIME as chat — one fight writes a line per swing plus a loot line plus a reward line, and
        /// as a tab of the same window it would only have moved the problem: you would still have to
        /// choose between watching your damage and seeing that someone whispered you.
        ///
        /// It sits bottom-RIGHT so it and the chat window (bottom-left, 760 wide) can both be open
        /// without overlapping on a 1280-wide reference canvas. Both are draggable from there.
        /// </summary>
        private void BuildCombatWindow()
        {
            var panel = UiKit.PanelBox(_worldRoot, "CombatLog");
            UiKit.Place(panel, new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-12f, 132f), new Vector2(480f, 320f));
            var inner = panel.GetChild(0);
            float chrome = UiKit.WindowChrome(panel, "Combat",
                                              () => { CloseWindow(panel); HighlightChatTabs(); });

            // Same as the chat window (`87e`(c)): the feed runs to the bottom now and Clear is an icon
            // in the title bar. This one has no Reply — there is nobody to answer in a damage feed.
            ScrollRect scroll;
            var content = UiKit.ScrollArea(inner, out scroll, 1f);
            UiKit.Stretch((RectTransform)scroll.transform, 10f, chrome + 6f, 10f, 10f);

            // The second of the two windows he named. Its minimum is smaller than chat's — it has no tab
            // row to fit, just a feed — so it can be shrunk to a genuinely thin ticker down one side,
            // which is most of the point of *"without they obscure my view"*.
            UiKit.MakeAdjustable(panel, "combat", new Vector2(300f, 160f));

            // ClearTab, not Clear: this window's bin must not take the conversation with it.
            UiKit.TitleBarIcon(panel, 1, UiKit.Icon.Bin, () => ClientLog.ClearTab(ClientLog.Tab.Combat));

            _combatView = new LogView
            {
                Panel = panel, Content = content, Scroll = scroll,
                Accepts = where => where == ClientLog.Tab.Combat,
            };
            panel.gameObject.SetActive(false);
        }

        /// <summary>Switch tab: the rows are a FILTERED projection of one buffer, so changing the filter
        /// throws the drawn rows away and redraws from the buffer. That is the expensive path the append
        /// rewrite removed from the per-LINE case — here it happens once per tap, not per message.</summary>
        private void SetChatTab(int tab)
        {
            if (_chatTab == tab) return;
            _chatTab = tab;
            RebuildLogRows(_chatView);
            HighlightChatTabs();
        }

        private void HighlightChatTabs()
        {
            if (_chatTabButtons == null) return;
            for (int i = 0; i < _chatTabButtons.Length; i++)
                _chatTabButtons[i].targetGraphic.color =
                    _chatTabValues[i] == _chatTab ? UiKit.TabActive : UiKit.PanelLight;
            // The Combat button is a toggle, so it reports the WINDOW's state, not a selected tab.
            if (_combatTabButton != null)
                _combatTabButton.targetGraphic.color =
                    IsOpen(_combatView?.Panel) ? UiKit.TabActive : UiKit.PanelLight;
        }

        /// <summary>Drop every drawn row and let the next refresh redraw the ones the filter accepts.</summary>
        private void RebuildLogRows(LogView view)
        {
            for (int i = view.Content.childCount - 1; i >= 0; i--)
                DropRow(view.Content.GetChild(i));
            view.RenderedSeq = -1;
            view.SeenRevision = -1;   // force RefreshLogView to run even if no new line has arrived
        }

        /// <summary>Remove a console row NOW and destroy it whenever Unity gets round to it.
        ///
        /// <para>⚠ The <c>SetParent(null)</c> is the whole point and it is not tidiness: <c>Destroy</c>
        /// is deferred to the end of the frame, so a row that has only been destroyed is still a CHILD
        /// for the rest of this frame — it still counts in <c>childCount</c> and it is still laid out.
        /// Every piece of arithmetic in <see cref="RefreshLogView"/> is over <c>childCount</c>, so
        /// leaving the corpses in place made the trim measure a number that included rows already on
        /// their way out. Detaching first makes the count honest inside the frame that changed it.</para></summary>
        private static void DropRow(Transform row)
        {
            row.SetParent(null, false);
            Destroy(row.gameObject);
        }

        /// <summary>True if a line belongs in the chat tab being shown. All (-1) accepts everything
        /// EXCEPT the combat feed — that one has its own window, and letting it back into All would
        /// undo the separation the window was built for.</summary>
        private bool ChatTabAccepts(ClientLog.Tab where) =>
            _chatTab < 0 ? where != ClientLog.Tab.Combat : (int)where == _chatTab;

        private void BuildBag()
        {
            _bagPanel = UiKit.PanelBox(_worldRoot, "Bag");
            UiKit.Place(_bagPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(BagWidthCollapsed, BagHeight));
            var inner = _bagPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_bagPanel, "Bag", () => CloseWindow(_bagPanel));
            _bagChrome = chrome;

            // Header line over the LEFT (list) region: gold left, slot usage beside it. Left-anchored so
            // they stay put when the window widens to reveal the equip column.
            _bagGoldLabel = UiKit.Label(inner, "", 15f, GoldColour, TextAlignmentOptions.Left);
            UiKit.Place(UiKit.Rect(_bagGoldLabel.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 8f), new Vector2(230f, 22f));
            _bagSlotsLabel = UiKit.Label(inner, "", 14f, UiKit.TextDim, TextAlignmentOptions.Right);
            UiKit.Place(UiKit.Rect(_bagSlotsLabel.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(252f, -chrome - 8f), new Vector2(182f, 22f));

            // TWO rows now (C8). Row 1 keeps the two TOGGLES — Equip (expands the paper-doll column) and
            // Fast-Del, whose per-row buttons stay hidden until it's on (owner) so a stray tap can't bin
            // an item. EQUIP goes first: it is the one that changes the window's shape and the thing you
            // reach for most. Row 2 is the category FILTER, five tabs wide, which is why it needed a row
            // of its own — "Items | Quest" fitted beside the toggles, "All | Gear | Use | Mats | Quest"
            // does not. Worn gear is still not a tab: it lives on the paper-doll.
            _bagEquipToggle = UiKit.TextButton(inner, "Equip", ToggleBagEquip, 14f);
            UiKit.Place(UiKit.Rect(_bagEquipToggle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -chrome - 36f), new Vector2(92f, 32f));

            // CHAR IS NO LONGER HERE (owner, 2026-08-28). It sat beside [Equip] for one playtest — the
            // door the sheet needed once the vitals tap became "target myself" — and his verdict is that
            // the door was in the wrong wall: *"now it's very annoying each time to open bag, open
            // stats"*. Two taps and a window to close, for a number you check between pulls. It is on the
            // ACTION BAR now, between [Bag] and [Skills] (BuildActionBar), one tap from anywhere.

            _bagDelToggle = UiKit.TextButton(inner, "Del: off",
                () => { _bagFastDel = !_bagFastDel; _bagRevision = -1; }, 14f);
            UiKit.Place(UiKit.Rect(_bagDelToggle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(112f, -chrome - 36f), new Vector2(92f, 32f));

            _bagTabButtons = BuildCategoryTabs(inner, BagTabs, new Vector2(16f, -chrome - 72f), 80f,
                                               cat => { _bagTab = cat; _bagRevision = -1; });

            // The item list is a FIXED-width column, so widening the window for the equip column never
            // stretches it — it just slides.
            ScrollRect scroll;
            _bagContent = UiKit.ScrollArea(inner, out scroll, 3f);
            _bagListRect = UiKit.Rect(scroll.transform.gameObject);
            UiKit.Place(_bagListRect, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(BagListX, -chrome - 110f), new Vector2(418f, BagHeight - chrome - 126f));

            // The paper-doll column (hidden until the Equip toggle) opens on the LEFT (owner), so it
            // appears where the window grows rather than pushing the list off toward the far edge. The
            // list slides right by exactly the column's width when it opens; see ToggleBagEquip.
            BuildEquipColumn(inner, new Vector2(BagListX, -chrome - 110f));   // below the header rows, aligned with the list (was -8, which put the Head slot over the tabs)

            _bagPanel.gameObject.SetActive(false);
        }

        /// <summary>Expand/collapse the bag to show the worn-gear paper-doll column beside the item list.</summary>
        private void ToggleBagEquip()
        {
            _bagEquipOpen = !_bagEquipOpen;
            if (_equipColumn != null) _equipColumn.gameObject.SetActive(_bagEquipOpen);
            _bagPanel.sizeDelta = new Vector2(_bagEquipOpen ? BagWidthExpanded : BagWidthCollapsed,
                                              _bagEquipOpen ? BagHeightExpanded : BagHeight);
            // Slide the list right so the paper-doll has the left side to itself, and let it use the
            // height the window just gained — it is anchored to the top, so without this the extra
            // 60px would be dead space under a list that could have shown two more rows.
            if (_bagListRect != null)
            {
                var p = _bagListRect.anchoredPosition;
                p.x = _bagEquipOpen ? BagListX + BagEquipColumnWidth : BagListX;
                _bagListRect.anchoredPosition = p;
                _bagListRect.sizeDelta = new Vector2(_bagListRect.sizeDelta.x,
                    (_bagEquipOpen ? BagHeightExpanded : BagHeight) - _bagChrome - 126f);
            }
            _bagEquipToggle.targetGraphic.color = _bagEquipOpen ? UiKit.TabActive : UiKit.PanelLight;
            _equipRevision = -1;   // force the paper-doll to repaint on next refresh
        }

        /// <summary>Does this bag row belong under the current tab? WORN gear is in no tab at all —
        /// it lives on the paper-doll column (owner: an unequipped item lives in the Items bag, not the
        /// Equipment bag), which is why this is not simply <see cref="InCategory"/>.</summary>
        private static bool InBagTab(ItemCategory tab, InventoryItemDto item, ItemDef def)
            => !item.Equipped && InCategory(tab, def);

        // BuildDebugPanel and everything it needs now live in GameUi.Debug.cs — it grew from four
        // buttons to the full WPF-parity tool (five tabs + live tuning) and had no business sharing a
        // file with the bag and the action bar.

        private void BuildOverlays()
        {
            _confirmPanel = UiKit.PanelBox(_root, "Confirm");
            UiKit.Place(_confirmPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(520f, 200f));
            var inner = _confirmPanel.GetChild(0);

            _confirmText = UiKit.Label(inner, "", 19f, UiKit.Text, TextAlignmentOptions.TopLeft);
            // Belt and braces with the sizing in Ask(): if a message somehow exceeds the height cap,
            // TRUNCATE it inside its own rect rather than letting it draw over the buttons.
            _confirmText.overflowMode = TextOverflowModes.Truncate;
            UiKit.Place(UiKit.Rect(_confirmText.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(22f, -22f), new Vector2(470f, 80f));

            var cancel = UiKit.TextButton(inner, "Cancel", Dismiss);
            UiKit.Place(UiKit.Rect(cancel.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(22f, 22f), new Vector2(220f, 52f));

            _confirmOkButton = UiKit.TextButton(inner, "OK", () =>
            {
                var action = _confirmAction;
                Dismiss();
                if (action != null) action();
            });
            UiKit.Place(UiKit.Rect(_confirmOkButton.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-22f, 22f), new Vector2(220f, 52f));

            _confirmPanel.gameObject.SetActive(false);
        }

        // ----- refresh ---------------------------------------------------------------------------

        private void RefreshWorld()
        {
            RefreshSelf();
            RefreshTarget();
            RefreshSkillBar();
            RefreshConsole();
            RefreshTitlesTab();
            RefreshBag();
            RefreshSkillsWindow();
            RefreshStatsWindow();
            RefreshTargetWindow();
            RefreshQuestWindow();
            RefreshQuestTracker();   // on screen even when the log is closed — that is its whole point
            RefreshQuestDetail();
            RefreshDialogWindow();
            RefreshPartyWindow();
            RefreshVendorWindow();
            RefreshCraftingList();
            RefreshWarehouseWindow();
            RefreshBuyBackWindow();
            RefreshRestoreWindow();
            RefreshEquipmentWindow();
            RefreshRegionUi();
            RefreshFarmRing();
            RefreshNameplates();

            RefreshFeedback();

            // The admin entry appears/disappears with the account role, and the menu RE-STACKS so there is
            // no hole where it was (owner). No-ops unless admin-ness changed.
            LayoutMenuPanel();

            // Respawn only while dead — the rest of the time it is a button that can do nothing.
            EntityDto self = null;
            if (Boot.Entities != null) Boot.Entities.TryGetState(Boot.SelfId, out self);
            _respawnButton.gameObject.SetActive(self != null && self.Dead);
            UiKit.SetButtonText(_pvpButton, Boot.PvpEnabled ? "PvP: ON" : "PvP: off");
            _pvpButton.targetGraphic.color = Boot.PvpEnabled
                ? new Color(0.55f, 0.20f, 0.20f, 0.95f) : UiKit.PanelLight;

            // While it runs, the button IS the timer (32q): the idle budget is spent silently and the
            // session used to just stop one day with a chat line you may have scrolled past.
            int autoLeft = Boot.AutoIdleSecondsLeftNow;
            UiKit.SetButtonText(_autoButton, !Boot.AutoHunting ? "Auto: off"
                                : autoLeft <= 0 ? "Auto: ON" : "Auto " + ShortTime(autoLeft));
            _autoButton.targetGraphic.color = Boot.AutoHunting
                ? new Color(0.20f, 0.45f, 0.25f, 0.95f) : UiKit.PanelLight;

            // The scrim follows the menu wherever the menu was closed FROM — the back button pops it
            // off the window stack directly, and a scrim left behind would silently eat every tap on
            // the game with nothing on screen to explain why.
            _slotMenuScrim.gameObject.SetActive(_slotMenu.gameObject.activeSelf);

            UpdateSkillBarSwipe();
        }

        private void RefreshSelf()
        {
            EntityDto self = null;
            if (Boot.Entities != null) Boot.Entities.TryGetState(Boot.SelfId, out self);

            if (self == null)
            {
                _selfName.text = "waiting for your entity ...";
                _selfHpText.text = _selfMpText.text = _selfXpText.text = "";
                return;
            }

            int level = Boot.Progress != null ? Boot.Progress.Level : self.Level;
            _selfName.text = self.Name + "    Lv " + level;

            // VITALS ONLY, and the numbers ride ON the bars. Gold and raw coordinates used to live
            // under them and overflowed the panel onto the world behind it. The rule the owner set is
            // general: the always-on panel carries name, level, HP/MP/EXP and nothing else — gold
            // belongs to the bag, position to the debug panel, the rest to whichever window owns it.
            UiKit.SetBar(_selfHp, self.Hp, self.MaxHp);
            _selfHpText.text = ValueAndPct(self.Hp, self.MaxHp);

            UiKit.SetBar(_selfMp, self.Mp, self.MaxMp);
            _selfMpText.text = ValueAndPct(self.Mp, self.MaxMp);

            // The EXP bar was BLANK, and the reason is worth keeping: the text was only written when
            // ExpToNext > 0, which is false at MAX LEVEL — so the one character most likely to be
            // looked at (a level 90 admin) showed nothing at all, and it read as a bug in the bar
            // rather than as "there is no next level". Say so instead.
            if (Boot.Progress == null)
            {
                _selfXpText.text = "";
            }
            else if (Boot.Progress.ExpToNext > 0)
            {
                UiKit.SetBar(_selfXp, Boot.Progress.Exp, Boot.Progress.ExpToNext);
                _selfXpText.text = ValueAndPct(Boot.Progress.Exp, Boot.Progress.ExpToNext);
            }
            else
            {
                UiKit.SetBar(_selfXp, 1, 1);
                _selfXpText.text = "MAX LEVEL";
            }
        }

        /// <summary>"1000 / 2000   50%" — the pair AND the percentage, on every bar. The numbers say
        /// how much is left in absolute terms (can I survive that hit), the percentage says how far
        /// through I am; people read one or the other depending on what they are doing.</summary>
        private static string ValueAndPct(long value, long max)
        {
            if (max <= 0) return value.ToString("N0");
            return value.ToString("N0") + " / " + max.ToString("N0")
                 + "   " + (100f * value / max).ToString("0.#") + "%";
        }

        private void RefreshTarget()
        {
            EntityDto target = null;
            if (Boot.TargetId.HasValue && Boot.Entities != null)
                Boot.Entities.TryGetState(Boot.TargetId.Value, out target);

            // 🔴 playtest-17 B7: an ally out of interest range is not in the world snapshot at all, so
            // the frame had nothing to draw. The ROSTER still knows them — name, level, class and both
            // bars — so draw from that instead of showing an empty screen where assist/heal/kick live.
            if (target == null && Boot.TargetId.HasValue &&
                Boot.FindPartyMember(Boot.TargetId.Value) is PartyMemberDto away)
            {
                DrawDistantPartyTarget(away);
                return;
            }

            _targetPanel.gameObject.SetActive(target != null);
            if (target == null) return;

            // 🔴 BL-88 — THE TITLE BAR IS THE NAME AND NOTHING ELSE. Playtest 25: *"only the name of the
            // target. No lvl no target.title, now the [title + name + lvl] overflows"*. The level had
            // already moved to the detail line in playtest 23; the WORN TITLE goes down there with it,
            // which is the last thing that could make this one row longer than the frame. A phone frame
            // is a fixed width and three variable-length things could always be made to overflow it —
            // one is the only count that cannot.
            string worn = string.IsNullOrEmpty(target.Title)
                        ? ""
                        : "<color=#" + (string.IsNullOrEmpty(target.TitleColor)
                                        ? TitleCatalog.DefaultHex : target.TitleColor) + ">"
                          + target.Title + "</color>";
            UiKit.SetWindowTitle(_targetPanel, target.Name + (target.Dead ? "   (dead)" : ""));
            bool self = Boot.Entities != null && Boot.TargetId == Boot.Entities.SelfId;
            bool player = target.Kind == EntityKind.Player && !self;
            bool mob = target.Kind == EntityKind.Mob;

            UiKit.SetBar(_targetHp, target.Hp, target.MaxHp);
            // CURRENT/MAX digits, not a percentage (owner, 2026-07-24). This reverses the older "another
            // player's exact HP is information you should not have" rule — he asked for the raw pair on
            // both mobs and players. Level stays private; only HP/MP opened up.
            _targetHpText.text = target.MaxHp > 0 ? target.Hp.ToString("N0") + " / " + target.MaxHp.ToString("N0") : "";

            // MP: players only. A mob's mana is not something you can act on.
            if (_targetMpRow != null) _targetMpRow.gameObject.SetActive(player && target.MaxMp > 0);
            if (player && target.MaxMp > 0)
            {
                UiKit.SetBar(_targetMp, target.Mp, target.MaxMp);
                _targetMpText.text = target.Mp.ToString("N0") + " / " + target.MaxMp.ToString("N0");
            }

            // 🔴 THE DETAIL LINE, rewritten to his spec (playtest 23): *"the type mob/player ... there u
            // can put agro,social for mob (for player will be his clan rank - king/soldier etc) now each
            // player can have there a 'vagabond' or some other word for clanless"* — his own examples
            // were `Mob: 44, Aggressive, Social` and `Player: Vagabond`.
            //
            // 🔑 The LEVEL rides here because the title bar took the name: `Mob: 44` is his format read
            // literally, and it is also the only place a mob's level can now appear. A player's level
            // stays private (the server sends 0), so a player row has no number in it, which is correct
            // rather than a gap.
            //
            // ⚠ "Vagabond" is not a placeholder waiting on a lookup — there are NO player clans in the
            // game yet, so every player is genuinely clanless and every row reads the same. When clans
            // exist this becomes the rank; nothing else about the line changes.
            //
            // 🔑 BL-88 — AND THE WORN TITLE NOW LANDS HERE TOO, first in the list, keeping its colour:
            // *"the mob title moves down into the Mob: row"*. `Mob: 44, Field Boss, Aggressive` — the
            // rank word an elite or a boss wears is a FACT about the creature, which is exactly what
            // this line is for, and it costs the title bar nothing to say it here instead.
            var bits = new List<string>();
            if (!string.IsNullOrEmpty(worn)) bits.Add(worn);
            if (mob)
            {
                // ⚠ Counted from HERE, not from zero: a titled mob's list is already non-empty, and
                // testing the whole list would have silently dropped "Passive" from every elite.
                int behaviour = bits.Count;
                if (target.Aggressive) bits.Add("Aggressive");
                // Social is OFF game-wide right now (`BL-73`) — the server sends "" for every mob while
                // the switch is down, so this simply prints nothing rather than claiming a camp answers.
                if (!string.IsNullOrEmpty(target.SocialClan)) bits.Add("Social (" + target.SocialClan + ")");
                if (bits.Count == behaviour) bits.Add("Passive");
                _targetDetail.text = "Mob: " + target.Level + ", " + string.Join(", ", bits);
            }
            else if (target.Kind == EntityKind.Npc)
            {
                _targetDetail.text = bits.Count > 0 ? "NPC: " + string.Join(", ", bits) : "NPC";
            }
            else
            {
                bits.Insert(0, "Vagabond");
                _targetDetail.text = "Player: " + string.Join(", ", bits);
            }

            // A targeted PLAYER carries NO fast buttons at all (owner, 2026-07-24): attack, follow,
            // assist, party and trade all come off the frame. They are not lost — they belong in the
            // Skills window's ACTIONS tab, placeable on the skill bar like a skill, which is what
            // "every command as a button" actually meant. The frame keeps ONE button, Info, and only
            // for mobs (their stats and drop table are the thing you genuinely need looked up).
            if (_targetPartyButton != null) _targetPartyButton.gameObject.SetActive(false);
            if (_targetTradeButton != null) _targetTradeButton.gameObject.SetActive(false);
            if (_targetFollowButton != null) _targetFollowButton.gameObject.SetActive(false);
            if (_targetAssistButton != null) _targetAssistButton.gameObject.SetActive(false);
            // Attack stays for MOBS — killing things is the core loop and one tap for it is not clutter.
            if (_targetAttackButton != null) _targetAttackButton.gameObject.SetActive(mob && !target.Dead);
            if (_targetInfoButton != null) _targetInfoButton.gameObject.SetActive(mob);
            // …and Talk for NPCs (M13), which is the whole reason an NPC gets a frame at all now that
            // the first tap no longer opens the conversation.
            if (_targetTalkButton != null)
                _targetTalkButton.gameObject.SetActive(target.Kind == EntityKind.Npc);
        }

        /// <summary>The target frame for a party member the world snapshot no longer carries (they
        /// walked out of interest range). Everything here comes from the roster push, which keeps
        /// running at any distance — so the frame stays up and the actions that name a target by id
        /// (assist, heal, buff, kick, change leader) remain reachable. The fast buttons are all
        /// mob-only anyway, so none of them appear.</summary>
        private void DrawDistantPartyTarget(PartyMemberDto m)
        {
            _targetPanel.gameObject.SetActive(true);
            // BL-88 — the name and NOTHING else, the same rule as the live frame above. A party
            // member's level is not private, so it moves to the detail line rather than disappearing:
            // this bar has exactly one job on every kind of target, or it can be made to overflow again.
            UiKit.SetWindowTitle(_targetPanel, m.Name);

            UiKit.SetBar(_targetHp, m.Hp, m.MaxHp);
            _targetHpText.text = m.MaxHp > 0
                ? m.Hp.ToString("N0") + " / " + m.MaxHp.ToString("N0") : "";

            if (_targetMpRow != null) _targetMpRow.gameObject.SetActive(m.MaxMp > 0);
            if (m.MaxMp > 0)
            {
                UiKit.SetBar(_targetMp, m.Mp, m.MaxMp);
                _targetMpText.text = m.Mp.ToString("N0") + " / " + m.MaxMp.ToString("N0");
            }

            // Say WHY the frame looks different, or an out-of-sight ally reads as a rendering fault.
            // The level rides here now (BL-88), beside the class, where a mob's level also sits.
            _targetDetail.text = (string.IsNullOrEmpty(m.ClassName) ? "Party" : m.ClassName)
                               + ": " + m.Level + "   (out of sight)";

            if (_targetPartyButton != null) _targetPartyButton.gameObject.SetActive(false);
            if (_targetTradeButton != null) _targetTradeButton.gameObject.SetActive(false);
            if (_targetFollowButton != null) _targetFollowButton.gameObject.SetActive(false);
            if (_targetAssistButton != null) _targetAssistButton.gameObject.SetActive(false);
            if (_targetAttackButton != null) _targetAttackButton.gameObject.SetActive(false);
            if (_targetInfoButton != null) _targetInfoButton.gameObject.SetActive(false);
            if (_targetTalkButton != null) _targetTalkButton.gameObject.SetActive(false);
        }

        private void RefreshSkillBar()
        {
            _pageLabel.text = (_barPage + 1) + " / " + BarPages;
            var bar = Boot.SkillBar;
            int first = _barPage * SlotsPerPage;

            for (int i = 0; i < SlotsPerPage; i++)
            {
                int index = first + i;
                string token = bar != null && index < bar.Length ? bar[index] : null;

                bool usable;
                _slotFaces[i].text = SlotFace(token, out usable);

                // How many of this consumable remain (32n) — 1…99 then "99+", so a full stack of 300
                // potions cannot push the digits across the face of the square.
                bool isItem = !string.IsNullOrEmpty(token) && GameConstants.IsItemSlot(token);
                int have = isItem ? Boot.BagCount(token.Substring(GameConstants.SkillBarItemPrefix.Length)) : 0;
                _slotCounts[i].gameObject.SetActive(isItem);
                if (isItem) _slotCounts[i].text = have > 99 ? "99+" : have.ToString();

                // 🔴 playtest-18 G7: a consumable slot you have run OUT of stays PRESSABLE. It used to
                // go non-interactable, and PressAndHold is gated on `interactable` — so the very gesture
                // that opens the slot menu died with it and the empty slot could never be taken off the
                // bar. It is drawn as a permanent FULL cooldown instead (his call): that says "not now"
                // without disabling the only way to remove it. The TAP is inert (see FireSlot).
                bool outOfStock = isItem && have <= 0;

                // An EMPTY slot is a disabled button — which made it impossible to place anything,
                // because the only target for a pending skill is an empty slot. While an assignment or
                // a move is waiting, every slot has to be pressable.
                _slotButtons[i].interactable =
                    usable || outOfStock || _pendingAssign != null || _pendingMoveFrom >= 0;

                // Thin green frame + a corner "A" = the auto-hunt will use this one.
                bool auto = !string.IsNullOrEmpty(token) && Boot.AutoSkills.Contains(AutoIdFor(token));
                _slotBorders[i].enabled = auto;
                _slotAutoMarks[i].gameObject.SetActive(auto);

                // Aqua frame OUTSIDE that one = this slot holds a TOGGLE and the toggle is on.
                _slotToggleBorders[i].enabled = IsToggleOn(token);

                // The reuse sheet + its countdown. Driven from the client's own clock (the server sends
                // one message when the timer starts, not one per tick), so this animates at frame rate.
                float left, fraction;
                bool cooling = Boot.ReuseOf(token, out left, out fraction);
                _slotReuse[i].gameObject.SetActive(cooling || outOfStock);
                _slotReuseText[i].gameObject.SetActive(cooling);
                if (cooling)
                {
                    var size = _slotReuseRects[i].sizeDelta;
                    size.y = SlotSize * fraction;
                    _slotReuseRects[i].sizeDelta = size;
                    // Tenths under 10s — the difference between "now" and "still a while" is the whole
                    // reason to look; a bare "1" for anything under two seconds hides it.
                    _slotReuseText[i].text = left >= 10f ? Mathf.CeilToInt(left).ToString()
                                                         : left.ToString("0.0");
                }
                else if (outOfStock)
                {
                    // No countdown text — there is no timer to run out, only an empty bag.
                    var size = _slotReuseRects[i].sizeDelta;
                    size.y = SlotSize;
                    _slotReuseRects[i].sizeDelta = size;
                }

                // X over the slot whose skill is being cast right now, so cancelling is where your
                // finger already is rather than somewhere else on screen.
                bool casting = IsCastingSlot(token);
                _slotCancel[i].gameObject.SetActive(casting);
                if (casting) _slotButtons[i].interactable = true;
            }
        }

        /// <summary>Is this bar token a TOGGLE skill that is currently switched on?
        ///
        /// The server does not push a "toggles that are on" list and does not need to: a live toggle is
        /// a buff on the bar under the skill's own <c>BuffKey</c>, with no timer (SecondsLeft &lt; 0,
        /// because a stance runs until you or your MP stop it). So the answer is already in the buff
        /// push — it just had nothing reading it from the SLOT's side.
        ///
        /// A non-toggle token answers false without touching the buff list, which is most of the bar.
        /// </summary>
        private bool IsToggleOn(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            if (SkillCatalog.Get(token) is not { Toggle: true } def) return false;
            string key = string.IsNullOrEmpty(def.BuffKey) ? def.Id : def.BuffKey;
            var buffs = Boot.Buffs;
            if (buffs == null) return false;
            for (int i = 0; i < buffs.Length; i++)
                if (buffs[i].Key == key) return true;
            return false;
        }

        /// <summary>What to print on a slot. Skills resolve icon → authored Abbrev →
        /// <see cref="Abbreviations"/>, the same order as WPF: that helper resolves the WHOLE catalog
        /// at once, which is what stops two skills sharing a label.</summary>
        private string SlotFace(string token, out bool usable)
        {
            usable = false;
            if (string.IsNullOrEmpty(token)) return "";

            usable = true;
            if (ActionCatalog.FromToken(token) is ActionDef action)
                return Abbreviations.For(action.Name);

            if (GameConstants.IsItemSlot(token))
            {
                // Show WHICH item (abbreviated), and grey the slot when you have none in the bag.
                string defId = token.Substring(GameConstants.SkillBarItemPrefix.Length);
                var idef = ItemCatalog.Get(defId);
                usable = Boot.FindBagItem(defId) != null;
                return idef != null ? Abbreviations.For(idef.Name) : "[i]";
            }

            if (GameConstants.IsPresetSlot(token))
            {
                usable = true;
                string s = token.Substring(GameConstants.SkillBarPresetPrefix.Length);
                return int.TryParse(s, out int p) && p >= 0 && p < 3 ? "P-" + "ABC"[p] : "P?";
            }

            var def = SkillCatalog.Get(token);
            if (def == null) { usable = false; return "?"; }

            // A bar is per-class, and a subclass can legitimately hold skills it has not learned.
            usable = Boot.Learned.Count == 0 || Boot.Learned.ContainsKey(token);

            return SkillLetters(def);
        }

        /// <summary>Is this token the skill currently being cast? Matched by NAME, because the cast
        /// push (CastInfo) carries a display name rather than the skill id.</summary>
        private bool IsCastingSlot(string token)
        {
            if (!Boot.IsCasting || string.IsNullOrEmpty(token)) return false;
            var def = SkillCatalog.Get(token);
            return def != null && def.Name == Boot.CastingSkill;
        }

        private void FireSlot(int slotOnPage)
        {
            int index = _barPage * SlotsPerPage + slotOnPage;

            // Tapping the slot that is mid-cast CANCELS it — but not instantly. See CastCancelGrace:
            // a double tap would otherwise start the cast and kill it in the same gesture, paying the
            // initial MP and the whole cooldown for nothing.
            if (IsCastingSlot(TokenAt(slotOnPage)))
            {
                if (Time.realtimeSinceStartup - Boot.CastStartedAt >= CastCancelGrace) Boot.CancelCast();
                return;
            }

            // A slot tap means whichever pending gesture is armed, and only casts when none is. Move
            // is checked first because it was started from this very bar.
            if (_pendingMoveFrom >= 0)
            {
                int from = _pendingMoveFrom;
                _pendingMoveFrom = -1;
                if (from != index) Boot.SwapSlots(from, index);
                _skillsRevision = -1;
                return;
            }

            if (TryPlacePending(index)) return;

            var bar = Boot.SkillBar;
            if (bar == null || index >= bar.Length) return;

            // A consumable slot at 0 is drawn as a permanent full cooldown and stays pressable so it can
            // still be held and removed (playtest-18 G7) — but the TAP does nothing. Firing it would only
            // earn a refusal from the server for something the slot already says you cannot do.
            string fired = bar[index];
            if (!string.IsNullOrEmpty(fired) && GameConstants.IsItemSlot(fired) &&
                Boot.BagCount(fired.Substring(GameConstants.SkillBarItemPrefix.Length)) <= 0)
                return;

            // ConfirmOnUse (owner, 2026-08-26): the bar is ONE TAP, which is exactly where an expensive
            // consumable gets drunk by accident — so the same prompt the details window shows goes here
            // too. Everything without the flag fires straight through, unchanged.
            if (!string.IsNullOrEmpty(fired) && GameConstants.IsItemSlot(fired)
                && ItemCatalog.Get(fired.Substring(GameConstants.SkillBarItemPrefix.Length)) is ItemDef bdef
                && bdef.ConfirmOnUse)
            {
                ConfirmUse(bdef, () => Boot.UseSlot(fired));
                return;
            }

            Boot.UseSlot(fired);
        }

        /// <summary>
        /// Press and hold a slot → Move / Bin / Auto, the phone's stand-in for a right-click menu.
        /// Auto only appears for things the auto-hunt can actually use; an occupied slot holding an
        /// action or an item gets Move and Bin only, and an empty slot has no menu at all.
        /// </summary>
        private void BuildSlotMenu()
        {
            // A full-screen scrim UNDER the menu, opened and closed with it.
            //
            // Without one, the menu was the only thing catching taps: everywhere else on screen the
            // tap fell through to the world and WALKED the character, while a menu asking you to pick
            // Move / Remove / Auto was still open. It is invisible but must absorb raycasts (so
            // TouchInput's UiKit.OverUi check sees it), and tapping it simply dismisses the menu —
            // which is what a tap outside a popup means everywhere else.
            _slotMenuScrim = UiKit.Rect(UiKit.Box(_worldRoot, "SlotMenuScrim",
                                                  new Color(0f, 0f, 0f, 0.001f)).gameObject);
            UiKit.Stretch(_slotMenuScrim, 0f, 0f, 0f, 0f);
            var dismiss = _slotMenuScrim.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = _slotMenuScrim.GetComponent<Image>();
            dismiss.onClick.AddListener(CloseSlotMenu);
            _slotMenuScrim.gameObject.SetActive(false);

            _slotMenu = UiKit.PanelBox(_worldRoot, "SlotMenu");
            UiKit.Place(_slotMenu, new Vector2(0f, 0f), new Vector2(0.5f, 0f),
                        Vector2.zero, new Vector2(150f, 150f));
            var inner = _slotMenu.GetChild(0);

            var move = UiKit.TextButton(inner, "Move", () =>
            {
                _pendingMoveFrom = _barPage * SlotsPerPage + _menuSlot;
                _pendingAssign = null;     // the two modes would fight over the next tap
                CloseSlotMenu();
            }, 16f);
            UiKit.Place(UiKit.Rect(move.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -8f), new Vector2(130f, 40f));

            var bin = UiKit.TextButton(inner, "Remove", () =>
            {
                int index = _barPage * SlotsPerPage + _menuSlot;
                Boot.AssignSlot(index, null);
                _skillsRevision = -1;      // the "* on bar" marks are now stale
                CloseSlotMenu();
            }, 16f);
            UiKit.Place(UiKit.Rect(bin.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -52f), new Vector2(130f, 40f));

            _slotMenuAuto = UiKit.TextButton(inner, "Auto", () =>
            {
                var id = AutoIdFor(TokenAt(_menuSlot));
                if (!string.IsNullOrEmpty(id)) Boot.ToggleAutoSkill(id);
                CloseSlotMenu();
            }, 16f);
            UiKit.Place(UiKit.Rect(_slotMenuAuto.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -96f), new Vector2(130f, 40f));

            // Details last, because it is the one option that does not change anything.
            _slotMenuDetail = UiKit.TextButton(inner, "Details", () =>
            {
                var token = TokenAt(_menuSlot);
                var def = SkillCatalog.Get(token);
                CloseSlotMenu();
                if (def != null)
                    ShowSkillDetail(def.Id, Boot.Learned.TryGetValue(def.Id, out var lv) ? lv : 1);
            }, 16f);
            UiKit.Place(UiKit.Rect(_slotMenuDetail.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -140f), new Vector2(130f, 40f));

            _slotMenu.gameObject.SetActive(false);
        }

        /// <summary>
        /// The auto-hunt id a bar token maps to, or null when the autopilot cannot repeat it.
        ///
        /// A skill is its own id. The BASIC ATTACK is the exception: it is an action token on the bar
        /// but the server knows it as the pseudo-skill <see cref="AutoHuntIds.BasicAttack"/>, which is
        /// the entry that decides whether the autopilot melees at all. Not mapping it is why the
        /// attack slot was the one thing on the bar with no Auto toggle — the owner asked why, and the
        /// answer was simply that the client never joined the two names up.
        /// </summary>
        /// <remarks>The implementation moved to <see cref="GameBoot.AutoIdFor"/> — the BAR owns the
        /// mark, so <c>AssignSlot</c> needs the same mapping to clear it when a token leaves.</remarks>
        private static string AutoIdFor(string token) => GameBoot.AutoIdFor(token);

        private string TokenAt(int slotOnPage)
        {
            int index = _barPage * SlotsPerPage + slotOnPage;
            var bar = Boot.SkillBar;
            return bar != null && index >= 0 && index < bar.Length ? bar[index] : null;
        }

        private void OpenSlotMenu(int slotOnPage)
        {
            string token = TokenAt(slotOnPage);
            if (string.IsNullOrEmpty(token)) return;   // nothing to move, bin or automate

            _menuSlot = slotOnPage;

            // Auto is offered for anything the autopilot can actually repeat: a castable skill, or the
            // basic attack (which reaches the server as a pseudo-skill — see AutoIdFor). Items, the
            // other actions and passives have nothing for it to fire.
            bool isSkill = SkillCatalog.Get(token) != null;
            var autoId = AutoIdFor(token);
            bool autoable = autoId != null;

            _slotMenuAuto.gameObject.SetActive(autoable);
            if (autoable)
                UiKit.SetButtonText(_slotMenuAuto, Boot.AutoSkills.Contains(autoId) ? "Auto: ON" : "Auto: off");

            // Details only for a real skill — an action or an item has no SkillDef to describe.
            _slotMenuDetail.gameObject.SetActive(isSkill);
            UiKit.Place(UiKit.Rect(_slotMenuDetail.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, autoable ? -140f : -96f), new Vector2(130f, 40f));

            // Sit the menu above the bar. It is pinned to the right rather than to the held slot:
            // anchoring per-slot pushes it off screen for the edge columns on a phone.
            int rows = 2 + (autoable ? 1 : 0) + (isSkill ? 1 : 0);
            var rt = _slotMenu;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-12f, 300f);
            rt.sizeDelta = new Vector2(150f, 18f + rows * 44f);

            // Scrim first, then the menu — OpenWindow raises each to the top as it goes, so this
            // order is what puts the menu ABOVE the thing that blocks taps around it.
            _slotMenuScrim.gameObject.SetActive(true);
            _slotMenuScrim.SetAsLastSibling();
            OpenWindow(_slotMenu);
        }

        private void CloseSlotMenu()
        {
            _slotMenuScrim.gameObject.SetActive(false);
            CloseWindow(_slotMenu);
        }

        private void PageBy(int delta) => _barPage = Mathf.Clamp(_barPage + delta, 0, BarPages - 1);

        /// <summary>Swipe across the bar to change page. Read from raw touches because uGUI has no
        /// gesture concept and the slot buttons underneath would otherwise swallow the drag.</summary>
        private void UpdateSkillBarSwipe()
        {
            const float swipePixels = 90f;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);

                if (touch.phase == TouchPhase.Began &&
                    RectTransformUtility.RectangleContainsScreenPoint(_skillBarPanel, touch.position, null))
                {
                    _swipeFinger = touch.fingerId;
                    _swipeStartX = touch.position.x;
                }
                else if (touch.fingerId == _swipeFinger &&
                         (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
                {
                    float dx = touch.position.x - _swipeStartX;
                    if (Mathf.Abs(dx) >= swipePixels) PageBy(dx < 0 ? 1 : -1);
                    _swipeFinger = -1;
                }
            }
        }

        private void SubmitCommand()
        {
            if (!string.IsNullOrWhiteSpace(_commandField.text)) Boot.Say(_commandField.text);
            _commandField.text = "";
            // Drop focus so the keyboard closes and the next tap on the ground WALKS instead of
            // being eaten by the text field.
            _commandField.DeactivateInputField();
        }

        /// <summary>Put text in the command box and open the keyboard with the caret after it — the
        /// "/w Name " half of a whisper that no button can finish. Used by the Whisper action and the
        /// chat window's Reply.</summary>
        public void ComposeCommand(string text)
        {
            if (_commandField == null) return;
            _commandField.text = text ?? "";
            _commandField.Select();
            _commandField.ActivateInputField();
            _commandField.caretPosition = _commandField.text.Length;
            _commandField.selectionAnchorPosition = _commandField.caretPosition;
            _commandField.selectionFocusPosition = _commandField.caretPosition;
        }

        private void RefreshConsole()
        {
            RefreshLogView(_chatView);
            RefreshLogView(_combatView);
        }

        private void RefreshLogView(LogView view)
        {
            if (view == null || !view.Panel.gameObject.activeSelf
                || view.SeenRevision == ClientLog.Revision) return;
            view.SeenRevision = ClientLog.Revision;

            var lines = ClientLog.Lines;

            // APPEND ONLY. This used to Destroy every child and rebuild all up-to-200 labels — each with
            // a ContentSizeFitter — plus a Canvas.ForceUpdateCanvases(), EVERY time a single line arrived
            // while the window was open. During combat or debug spam that is many full teardown/rebuilds
            // a second, and the cost grows with the accumulated line count — which is why the phone
            // "lagged a lot until I cleared it" (owner, playtest 0.28.76): clearing dropped the buffer
            // back to ~0 rows, so the rebuild went cheap again. Now each refresh only draws the lines it
            // has not drawn before, and trims the oldest rows past a cap. Reported 2026-07-24; fixed same.

            // A Clear wipes the rows once; everything else just appends. ClearGeneration is bumped only
            // by ClientLog.Clear, so this is unambiguous — no guessing from buffer indices.
            if (view.SeenClearGen != ClientLog.ClearGeneration)
            {
                view.SeenClearGen = ClientLog.ClearGeneration;
                RebuildLogRows(view);
            }

            // 🔴 NEVER BUILD A ROW THIS SAME CALL IS ABOUT TO THROW AWAY (playtest 24, `87b`:
            // *"System chat lagging the game ... Other tabs don't, just system (respectively and
            // 'all')"* · *"after a game restart it works"*).
            //
            // The append rewrite fixed the per-LINE case and left the per-BATCH one: a batch was
            // whatever had not been drawn yet, and three routes make that the ENTIRE 1000-line buffer —
            // switching tab (RebuildLogRows resets RenderedSeq to -1), reopening the window (the
            // refresh early-outs while it is closed, so the backlog is waiting when it opens), and a
            // Clear generation. Every one of those built up to 1000 labels, each with a
            // ContentSizeFitter, in ONE frame, and then the trim below destroyed ~880 of them
            // immediately. That is the whole report: it is System and All because those are the only
            // tabs the buffer actually fills — Local/World/PM hold a handful of lines and their batch
            // is a handful of rows — and a restart cures it because a restart empties the buffer.
            //
            // So find the OLDEST line still inside the display cap and start there. The cap is the
            // number of rows the window can hold at all, so nothing visible is lost: the lines older
            // than that were never going to survive the trim.
            int first = lines.Count;
            int budget = ConsoleDisplayRows;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].Seq <= view.RenderedSeq) break;   // everything older is already drawn
                if (view.Accepts(lines[i].Where) && --budget < 0) break;
                first = i;
            }

            int appended = 0;
            for (int i = first; i < lines.Count; i++)
            {
                if (lines[i].Seq <= view.RenderedSeq) continue;   // already drawn
                // A line this view does not want is SKIPPED, not remembered: RenderedSeq only advances
                // past lines that were actually drawn, so switching tabs (which resets it to -1)
                // redraws the buffer from the start with the new filter.
                if (!view.Accepts(lines[i].Where)) continue;
                var label = UiKit.Label(view.Content, lines[i].Text, 15f, lines[i].Color);
                // Rows GROW with wrapped text instead of a fixed height. A fixed row is what made
                // long messages draw over each other in the IMGUI console.
                var fitter = label.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                view.RenderedSeq = lines[i].Seq;
                appended++;
            }

            // Trim the oldest rows so the window never holds more than the cap.
            // ⚠ Count the excess ONCE and destroy that many by index. A `while (childCount > cap)` loop
            // FREEZES: Unity's Destroy is deferred to end-of-frame, so childCount does NOT drop inside
            // the loop — the condition stays true and GetChild(0) keeps returning the same (already
            // marked) object forever. That infinite loop is what locked the phone in 0.28.77; this is
            // the real fix on top of the append rewrite. DropRow detaches as well as destroying, so the
            // count is honest even when a rebuild ran earlier in this same frame (`87b`).
            int excess = view.Content.childCount - ConsoleDisplayRows;
            for (int i = 0; i < excess; i++)
                DropRow(view.Content.GetChild(0));

            if (appended > 0)
            {
                Canvas.ForceUpdateCanvases();
                view.Scroll.verticalNormalizedPosition = 0f;   // newest line
            }
        }

        private void RefreshBag()
        {
            if (!_bagPanel.gameObject.activeSelf) return;

            // Cheap change stamp: the server pushes the WHOLE bag on any change, so this only has to
            // notice that the push differs from what is on screen.
            //
            // It MUST include each item's IDENTITY (owner, playtest-16: "the newbie/rune box opening
            // doesn't refresh the inventory with the out-of-box items"). The stamp used to hash only
            // length + equipped + quantity + enchant, and opening a box is a SWAP: one box leaves, one
            // item arrives. Same length, same quantities, same everything it looked at — an identical
            // stamp, so the rows were never rebuilt and the bag still showed the box you just opened.
            // Any other loot changed the length and hid the bug. InstanceId is unique per item, so a
            // swap can no longer be invisible (the warehouse and paper-doll stamps already did this).
            var items = Boot.Inventory ?? Array.Empty<InventoryItemDto>();
            int used = 0;
            foreach (var it in items) if (!it.Equipped) used++;   // worn gear doesn't take a slot

            int revision = items.Length * 17 + (int)_bagTab * 7919 + (_bagFastDel ? 104729 : 0) + (int)(Boot.Gold % 1_000_000);
            foreach (var item in items)
                revision = revision * 31 + item.InstanceId.GetHashCode()
                         + (item.Equipped ? 1 : 0) + item.Quantity * 7 + item.Enchant;
            if (revision == _bagRevision) return;
            _bagRevision = revision;

            _bagGoldLabel.text = "Gold: " + Boot.Gold.ToString("N0");
            _bagSlotsLabel.text = "Slots " + used + " / " + GameConstants.InventorySize;
            PaintCategoryTabs(_bagTabButtons, BagTabs, _bagTab);
            UiKit.SetButtonText(_bagDelToggle, _bagFastDel ? "Del: ON" : "Del: off");
            _bagDelToggle.targetGraphic.color = _bagFastDel ? new Color(0.42f, 0.20f, 0.20f, 0.95f) : UiKit.PanelLight;

            for (int i = _bagContent.childCount - 1; i >= 0; i--)
                Destroy(_bagContent.GetChild(i).gameObject);

            bool anyInTab = false;
            foreach (var item in ByName(items))          // C8: name order, same as the vendor and the keeper
            {
                var def = ItemCatalog.Get(item.DefId);
                if (!InBagTab(_bagTab, item, def)) continue;
                anyInTab = true;
                var row = UiKit.Box(_bagContent, "Item", UiKit.PanelLight);
                row.gameObject.AddComponent<LayoutElement>().minHeight = 46f;

                // The row is now name (qty) + [details] + a fast action — NOT the action inline. Reading
                // an item and acting on it are two different intents, and cramming both into every row is
                // what the owner called "30 buttons". Details is where the stats, set info, compare and
                // bin-delete live (GameUi.Items.cs); the row keeps only the fast path.
                string name = def != null ? def.Name : item.DefId;
                if (item.Enchant > 0) name = "+" + item.Enchant + " " + name;
                if (item.Quantity > 1) name += "   x" + item.Quantity;
                if (item.Equipped) name = "* " + name;

                // QUALITY COLOUR on the bag row. The vendor, warehouse, item details and worn squares
                // all colour by rarity; the bag — the list you look at most — was the one place still
                // painting everything the same grey. That matters more now than it used to: the same
                // piece exists at six qualities under ONE name, so without the colour two rows of
                // "Electrum Blade" are indistinguishable.
                //
                // An EQUIPPED row stays green: "this is what you are wearing" is the more urgent fact
                // while scanning a bag, and the "*" prefix alone is easy to miss.
                var label = UiKit.Label(row.transform, name, 17f,
                                        item.Equipped ? UiKit.Good
                                                      : def != null ? RarityColour(def.Rarity) : UiKit.Text,
                                        TextAlignmentOptions.Left);
                UiKit.Stretch(UiKit.Rect(label.gameObject), 12f, 0f, 224f, 0f);

                var id = item.InstanceId;
                var shown = item;
                float rightX = -8f;   // buttons grow leftward from the row's right edge

                // Fast-delete: HIDDEN unless the Del toggle is on; bins the WHOLE stack with NO
                // confirmation (owner). Never for quest items, nor WORN gear (unequip it first).
                if (_bagFastDel && !item.Equipped && (def == null || def.Slot != EquipSlot.QuestItem))
                {
                    var bin = UiKit.TextButton(row.transform, "Del", () => Boot.RemoveItem(id, true), 14f);
                    bin.targetGraphic.color = new Color(0.42f, 0.20f, 0.20f, 0.95f);
                    UiKit.Place(UiKit.Rect(bin.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                new Vector2(rightX, 0f), new Vector2(52f, 38f));
                    rightX -= 56f;
                }

                var details = UiKit.TextButton(row.transform, "Details", () => OpenItemDetails(shown), 14f);
                UiKit.Place(UiKit.Rect(details.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                            new Vector2(rightX, 0f), new Vector2(94f, 38f));
                rightX -= 98f;

                // Fast path: [e] equips/unequips gear, [u] uses a consumable — both skip the details
                // window. EquipSlot has no "None" (it classifies EVERY item), so the wearable slots are
                // tested explicitly; anything else gets no fast button rather than one the server refuses.
                bool wearable = def != null &&
                    (def.Slot == EquipSlot.Weapon || def.Slot == EquipSlot.Armor ||
                     def.Slot == EquipSlot.Shield || def.Slot == EquipSlot.Jewel);

                if (wearable)
                {
                    var fast = UiKit.TextButton(row.transform, "e", () => Boot.EquipItem(id), 15f);
                    UiKit.Place(UiKit.Rect(fast.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                new Vector2(rightX, 0f), new Vector2(48f, 38f));
                }
                else if (def != null && def.Slot == EquipSlot.Consumable)
                {
                    var fast = UiKit.TextButton(row.transform, "u", () => Boot.UsePotion(id), 15f);
                    UiKit.Place(UiKit.Rect(fast.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                new Vector2(rightX, 0f), new Vector2(48f, 38f));
                }
            }

            if (!anyInTab)
            {
                var empty = UiKit.Label(_bagContent, _bagTab switch
                {
                    ItemCategory.Gear  => "No gear in the bag.",
                    ItemCategory.Use   => "No potions, scrolls or boxes.",
                    ItemCategory.Mats  => "No materials.",
                    ItemCategory.Quest => "No quest items.",
                    _                  => "No items.",
                }, 17f, UiKit.TextDim);
                empty.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
            }
        }

        // ----- nameplates ------------------------------------------------------------------------

        private class Nameplate
        {
            public RectTransform Root;
            public TextMeshProUGUI Label;
            public Image BarBg, BarFill;
            /// <summary>The MOB cast bar and the name of what it is casting — see RefreshNameplates.</summary>
            public RectTransform Cast;
            public Image CastFill;
            public TextMeshProUGUI CastLabel;
            /// <summary>The worn leaderboard title, on its own line above the name (players only).</summary>
            public TextMeshProUGUI TitleLabel;
        }

        /// <summary>What a title with no colour of its own is drawn in, and the fallback for a hex this
        /// build cannot parse.</summary>
        private static readonly Color TitleColour = new Color(0.95f, 0.83f, 0.45f, 1f);

        /// <summary>
        /// Turn the RRGGBB the server sent into a colour. The hex is chosen server-side — by the board
        /// a title came from, by the palette its owner picked, or by the NPC role line — so this is a
        /// parse, not a lookup: the client is deliberately not a second place that decides what colour
        /// a title is.
        /// </summary>
        private static Color TitleColourOf(string hex) =>
            !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString("#" + hex, out var c)
                ? c : TitleColour;

        /// <summary>Height of the title line, and how far the cast bar is pushed up when one is showing.</summary>
        private const float TitleLineHeight = 17f;

        /// <summary>A telegraphed spell is amber — deliberately neither the HP red under it nor the
        /// blue of your own cast bar, so "something is about to hit me" has its own colour.</summary>
        private static readonly Color CastColour = new Color(0.95f, 0.72f, 0.25f, 1f);

        private void RefreshNameplates()
        {
            var cam = Camera.main;
            if (cam == null || Boot.Entities == null) return;

            // Your own level, for the mob level-gap colours. Progress is authoritative for yourself;
            // the entity's own Level is the fallback before the first progress push.
            int myLevel = Boot.Progress != null ? Boot.Progress.Level : 0;
            if (myLevel <= 0 && Boot.Entities.TryGetState(Boot.SelfId, out var me)) myLevel = me.Level;

            int used = 0;
            bool targetDrawn = false;
            foreach (var kv in Boot.Entities.States)
            {
                var e = kv.Value;
                var view = Boot.Entities.Find(e.Id);
                if (view == null) continue;

                var screen = cam.WorldToScreenPoint(view.transform.position + Vector3.up * NameplateHeight);
                if (screen.z <= 0f) continue;   // behind the camera

                var plate = PlateAt(used++);
                plate.Root.gameObject.SetActive(true);
                plate.Root.position = screen;

                // The "*" marks an aggressive mob — what to tiptoe around BEFORE it decides for you.
                string title = e.Name + (e.Aggressive ? "*" : "");
                if (e.Disconnected) title += "  (disconnected)";
                // Quest marker over an NPC's head, so you can SEE who has something for you instead of
                // walking the town tapping everyone (owner, playtest-13). Prefixed rather than suffixed
                // so a row of NPCs lines its markers up.
                string mark = QuestMarkGlyph(e.Id);
                if (mark.Length > 0) title = mark + " " + title;
                plate.Label.text = title;

                // YOUR TARGET, marked with a circle on each side of the name (owner, 2026-08-01).
                // Positioned here because this is the one place that knows both where the plate landed
                // this frame and how wide the name turned out to be.
                if (Boot.TargetId.HasValue && Boot.TargetId.Value == e.Id)
                {
                    PlaceTargetDots(plate, title);
                    targetDrawn = true;
                }

                // YOUR OWN flag colour wins over "you are green".
                //
                // Self used to be painted green unconditionally, which meant the one player who most
                // needs to know he is a PK — the one carrying the karma — was the only player on
                // screen not shown as one. Karma turns guards hostile and makes you drop gear when you
                // die; forgetting you have it is exactly how that costs you something. Green now means
                // "you, and clean"; red means "you, and hunted".
                plate.Label.color = e.Id == Boot.SelfId && e.Flag == PvpFlag.Innocent
                    ? UiKit.Good
                    : NameColour(e, myLevel);

                // THE TITLE LINE, above the name. It arrives as TEXT plus a COLOUR and this draws both
                // — deliberately without knowing where either came from. A board title, a staff title,
                // one the player wrote with `/title`, and an NPC's role ("Elder" over "Marius") are all
                // the same two fields by the time they reach here, which is why free titles needed no
                // new drawing code. The server has already decided whether a granted one is still held;
                // an unheld one arrives as "".
                bool hasTitle = !string.IsNullOrEmpty(e.Title);
                plate.TitleLabel.gameObject.SetActive(hasTitle);
                if (hasTitle)
                {
                    plate.TitleLabel.text = e.Title;
                    plate.TitleLabel.color = TitleColourOf(e.TitleColor);
                }

                // The cast bar shares the space above the plate, so it steps up over a title rather than
                // drawing through it.
                plate.Cast.anchoredPosition = new Vector2(0f, hasTitle ? 2f + TitleLineHeight : 2f);

                bool bar = e.MaxHp > 0 && e.Kind != EntityKind.Npc;
                plate.BarBg.gameObject.SetActive(bar);
                if (bar) UiKit.SetBar(plate.BarFill, e.Hp, e.MaxHp);

                // THE MOB CAST BAR. The server has broadcast a mob's casts since bosses shipped and
                // nothing drew them, which is why a boss's telegraphed slam has always landed out of
                // nowhere. A named, filling bar over its head is the whole point of telegraphing: it is
                // the window in which you walk out of it, interrupt it, or decide to eat it.
                //
                // It fills on the CLIENT's clock from the duration the server sent — the server pushes
                // once at the start, not per tick — and a finished bar simply stops being reported.
                GameBoot.MobCast cast = null;
                bool casting = !e.Dead && Boot.TryGetMobCast(e.Id, out cast);
                plate.Cast.gameObject.SetActive(casting);
                if (casting)
                {
                    float total = Mathf.Max(0.01f, cast.EndsAt - cast.StartedAt);
                    UiKit.SetBar(plate.CastFill, Time.realtimeSinceStartup - cast.StartedAt, total);
                    plate.CastLabel.text = cast.SkillName;
                }
            }

            // Nothing targeted, or the target is off screen / behind the camera — the dots are DESTROYED
            // rather than hidden. There is only ever one target, so they are cheap to make and there is
            // nothing to pool: keeping two dead objects around to avoid two allocations per re-target
            // would be the more complicated of the two.
            if (!targetDrawn) DestroyTargetDots();

            for (int i = used; i < _nameplates.Count; i++)
                _nameplates[i].Root.gameObject.SetActive(false);
        }

        // ----- the target dots --------------------------------------------------------------------

        /// <summary>The two circles flanking your target's name. Created when something is targeted,
        /// destroyed when it is not — there is exactly one target, so there is exactly one pair.</summary>
        private Image _targetDotLeft, _targetDotRight;

        /// <summary>Diameter of a dot and its gap from the name, in UI units.</summary>
        private const float TargetDotSize = 11f, TargetDotGap = 7f;

        /// <summary>
        /// How far above the entity's screen point the dots sit — the NAME's vertical middle.
        ///
        /// Derived from the plate's own layout rather than eyeballed: the label box starts at
        /// <see cref="PlateGap"/> + 10 (see PlateAt) and the 15pt line is bottom-aligned inside it, so
        /// the glyphs' middle lands about a third of a line above that edge. Change the plate's layout
        /// and this has to move with it.
        /// </summary>
        private const float TargetDotY = PlateGap + 10f + 5f;

        /// <summary>
        /// Put the two dots either side of the name that was just drawn on <paramref name="plate"/>.
        ///
        /// The x offset is the RENDERED width of the title, asked of TMP directly — the plate is a
        /// fixed 200 wide with the name centred and free to overflow, so the box says nothing about
        /// where the text actually ends. Rich text (the quest "!" at 200%) is included in that measure,
        /// which is what keeps the dots outside the marker rather than on top of it.
        /// </summary>
        private void PlaceTargetDots(Nameplate plate, string title)
        {
            if (_targetDotLeft == null) CreateTargetDots();

            float half = plate.Label.GetPreferredValues(title).x * 0.5f + TargetDotGap + TargetDotSize * 0.5f;

            // The plate's position is in SCREEN pixels; everything above is in UI units. lossyScale is
            // the canvas scaler's factor between them — without it the dots would drift away from the
            // name on any device whose resolution is not the 1280x720 the UI is authored at.
            float k = _nameplateLayer.lossyScale.x;
            var at = plate.Root.position + new Vector3(0f, TargetDotY * k, 0f);

            _targetDotLeft.rectTransform.position = at + new Vector3(-half * k, 0f, 0f);
            _targetDotRight.rectTransform.position = at + new Vector3(half * k, 0f, 0f);
        }

        private void CreateTargetDots()
        {
            _targetDotLeft = NewTargetDot();
            _targetDotRight = NewTargetDot();
        }

        private Image NewTargetDot()
        {
            var dot = UiKit.Box(_nameplateLayer, "TargetDot", UiKit.Accent, blocksInput: false);
            dot.sprite = CircleSprite();
            UiKit.Place(UiKit.Rect(dot.gameObject), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(TargetDotSize, TargetDotSize));
            return dot;
        }

        private void DestroyTargetDots()
        {
            if (_targetDotLeft != null) Destroy(_targetDotLeft.gameObject);
            if (_targetDotRight != null) Destroy(_targetDotRight.gameObject);
            _targetDotLeft = null;
            _targetDotRight = null;
        }

        /// <summary>
        /// A filled white circle, GENERATED at runtime and shared by everything that wants one.
        ///
        /// Built rather than imported because this UI is authored entirely in code — the owner does not
        /// open the Editor, so an imported .png would be a file only the Editor can add and nobody can
        /// review in a diff (see UiKit's header). A uGUI Image tints whatever sprite it is given, so one
        /// white circle serves any colour.
        ///
        /// The edge fades over the last pixel instead of stopping dead, which is what stops an 11px dot
        /// from reading as a tiny staircase. Alpha works here where it does not in the world: uGUI's
        /// shader is alpha-blended, unlike the opaque unlit one the 3D markers use.
        /// </summary>
        private static Sprite _circleSprite;

        private static Sprite CircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Circle" };
            texture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            float centre = (size - 1) * 0.5f, radius = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centre, dy = y - centre;
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            texture.SetPixels32(pixels);
            texture.Apply();

            _circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            _circleSprite.name = "Circle";
            return _circleSprite;
        }

        /// <summary>
        /// What a name's colour MEANS:
        ///   you            green
        ///   player         white / purple (flagged for PvP) / red (a PK) — the wire's PvpFlag
        ///   mob            level gap to you: red down to grey (see LevelColour)
        ///   NPC            yellow — a service, and now unkillable
        ///
        /// Absolute mob level is nearly useless; the GAP is what decides whether you can take it, so
        /// that is what the colour encodes.
        /// </summary>
        private static Color NameColour(EntityDto e, int myLevel)
        {
            if (e.Id == Guid.Empty) return UiKit.Text;

            switch (e.Kind)
            {
                case EntityKind.Npc:
                    return new Color(1f, 0.93f, 0.55f);

                case EntityKind.Player:
                    switch (e.Flag)
                    {
                        case PvpFlag.Pk:      return new Color(1.00f, 0.30f, 0.30f);
                        case PvpFlag.Flagged: return new Color(0.80f, 0.50f, 1.00f);
                        default:              return new Color(0.92f, 0.94f, 0.96f);
                    }

                case EntityKind.Mob:
                    return LevelColour(e.Level, myLevel);

                default:
                    return UiKit.Text;
            }
        }

        /// <summary>Nameplates are POOLED. Entities come and go every few seconds as they wander in and
        /// out of view; creating and destroying uGUI objects at that rate would churn the layout system
        /// for no reason.</summary>
        private Nameplate PlateAt(int index)
        {
            while (_nameplates.Count <= index)
            {
                var root = UiKit.Rect(UiKit.Box(_nameplateLayer, "Plate",
                                                new Color(0, 0, 0, 0), blocksInput: false).gameObject);
                root.sizeDelta = new Vector2(200f, 34f + PlateGap);
                // Pivot at the BOTTOM edge, so the plate grows upward from the screen point and sits
                // above the character instead of being centred on him. With a centred pivot the name
                // lands straight on top of the marker.
                root.pivot = new Vector2(0.5f, 0f);

                var label = UiKit.Label(root, "", 15f, UiKit.Text, TextAlignmentOptions.Bottom);
                UiKit.Stretch(UiKit.Rect(label.gameObject), 0f, 0f, 0f, PlateGap + 10f);

                // Outlined, because a nameplate is drawn over WHATEVER is under it and the name
                // colour is already spoken for — it encodes the level gap and the PvP flag, so it
                // cannot also be chosen for legibility. A dark outline makes every one of those
                // colours readable against the ground without changing what the colour MEANS.
                label.outlineColor = new Color32(0, 0, 0, 210);
                label.outlineWidth = 0.22f;

                // No wrapping on a nameplate. The plate is 200 wide, and the doubled quest glyph costs
                // ~30 of that — wrap it and a long-named NPC's "!" ends up on a line of its own above
                // the name, which is exactly the overlap we were fixing. Overflowing sideways is fine:
                // the plate is transparent and centred on the NPC.
                label.enableWordWrapping = false;

                // The bar clears the marker by PlateGap as well. A bottom pivot alone only guarantees
                // the plate grows upward FROM the anchor — its lowest element still sits exactly on
                // the entity, which is why the HP bar was still drawn across the character.
                var fill = UiKit.ValueBar(root, UiKit.Hp);
                var bg = (RectTransform)fill.transform.parent;
                UiKit.Place(bg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                            new Vector2(0f, PlateGap), new Vector2(70f, 6f));

                // A nameplate floats over the world, so it must not eat the tap meant for the entity
                // underneath it — which is precisely where you aim when you want to attack something.
                fill.raycastTarget = false;
                bg.GetComponent<Image>().raycastTarget = false;

                // The cast bar goes ABOVE the name, not below it, and hangs off the TOP of the plate
                // rect (anchor 1, pivot 0) rather than being fitted inside it: the plate's height is
                // sized for the name and the HP bar, and a mob is casting for two seconds in a hundred.
                // Growing every plate permanently to hold a row that is almost always empty would push
                // every name in the world further off its owner's head.
                var castRoot = UiKit.Rect(UiKit.Box(root, "Cast", new Color(0, 0, 0, 0),
                                                    blocksInput: false).gameObject);
                UiKit.Place(castRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                            new Vector2(0f, 2f), new Vector2(120f, 30f));

                var castFill = UiKit.ValueBar(castRoot, CastColour);
                var castBg = (RectTransform)castFill.transform.parent;
                UiKit.Place(castBg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                            Vector2.zero, new Vector2(80f, 6f));
                castFill.raycastTarget = false;
                castBg.GetComponent<Image>().raycastTarget = false;

                // Named, because "it is casting SOMETHING" is only half the warning — a boss's slam and
                // its self-heal want opposite reactions, and you have one and a half seconds to pick.
                var castLabel = UiKit.Label(castRoot, "", 13f, CastColour, TextAlignmentOptions.Bottom);
                UiKit.Place(UiKit.Rect(castLabel.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                            new Vector2(0f, 7f), new Vector2(200f, 18f));
                castLabel.outlineColor = new Color32(0, 0, 0, 210);
                castLabel.outlineWidth = 0.22f;
                castLabel.enableWordWrapping = false;

                castRoot.gameObject.SetActive(false);

                // The TITLE sits directly above the name, hanging off the top of the plate for the same
                // reason the cast bar does: almost nobody is wearing one, and growing every plate in the
                // world to reserve a line for it would lift every name off its owner's head.
                var titleLabel = UiKit.Label(root, "", 12f, TitleColour, TextAlignmentOptions.Bottom);
                UiKit.Place(UiKit.Rect(titleLabel.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                            new Vector2(0f, 0f), new Vector2(200f, TitleLineHeight));
                // "A different font from the name" (owner, C16). The client ships ONE TMP font asset with
                // a STATIC atlas, so a second typeface is not available without baking one — italic +
                // small caps is TMP's synthesised styling on the font we already have, and it is the
                // difference he was after: the title stops looking like a second name and starts looking
                // like an inscription. Letter spacing widens it a touch so the caps do not crowd.
                titleLabel.fontStyle = FontStyles.Italic | FontStyles.SmallCaps;
                titleLabel.characterSpacing = 4f;
                titleLabel.outlineColor = new Color32(0, 0, 0, 210);
                titleLabel.outlineWidth = 0.22f;
                titleLabel.enableWordWrapping = false;
                titleLabel.raycastTarget = false;
                titleLabel.gameObject.SetActive(false);

                _nameplates.Add(new Nameplate
                {
                    TitleLabel = titleLabel,
                    Root = root,
                    Label = label,
                    BarBg = bg.GetComponent<Image>(),
                    BarFill = fill,
                    Cast = castRoot,
                    CastFill = castFill,
                    CastLabel = castLabel,
                });
            }
            return _nameplates[index];
        }

        // ----- back button -----------------------------------------------------------------------

        // OnDisable lives in GameUi.cs — a partial class gets ONE of each Unity message, and a second
        // definition is a compile error rather than a second callback.
        private void OnEnable() { Application.wantsToQuit += OnWantsToQuit; }

        private bool OnWantsToQuit()
        {
            if (_quitConfirmed) return true;
            AskBack();
            return false;
        }

        /// <summary>The phone's back button walks OUT one step at a time instead of killing the app:
        /// open panel → in world → character select → log out → quit, asking at each session step.
        /// Android delivers back as Escape AND as a quit request, hence the frame guard and the
        /// wantsToQuit veto.</summary>
        private void UpdateBackButton()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) AskBack();
        }

        private void AskBack()
        {
            if (Boot == null) return;
            if (_lastBackFrame == Time.frameCount) return;
            _lastBackFrame = Time.frameCount;

            if (_confirmPanel.gameObject.activeSelf) { Dismiss(); return; }

            // Back = "undo what I am doing", and the most immediate thing is a cast in progress. This
            // is the phone's ESC: the desktop client cancels a cast with Escape, and Android delivers
            // its back button AS Escape, so the two end up meaning the same thing without inventing a
            // second convention.
            if (Boot.IsCasting) { Boot.CancelCast(); return; }

            // One window per press, newest first. Only when the screen is clear does back mean "I
            // want out" — the old ladder made you confirm your way through leaving the world and
            // logging out, which is three prompts to do the one thing you asked for.
            if (CloseTopWindow()) return;

            Ask("Quit the game?", "Quit", QuitGracefully);
        }

        // Drawn at DOUBLE the name's size and bold (owner: "double the size of the quest ! and ?, they
        // are just too small"). At name size a "!" is a couple of phone pixels wide — the whole point of
        // the marker is to be readable while running past, without reading the name. `line-height=100%`
        // pins the plate's line box to the NAME's height, so the bigger glyph does not shove the name
        // down or push the row into the one above it.
        private const string QuestMarkOpen  = "<line-height=100%><size=200%><b>";
        private const string QuestMarkClose = "</b></size></line-height>";

        /// <summary>The glyph over an NPC's head: gold "!" = a quest you can take, grey "?" = one you
        /// are on, gold "?" = one you can hand in NOW. The MMO shorthand, so it needs no explaining.</summary>
        private string QuestMarkGlyph(Guid entityId)
        {
            if (Boot.QuestMarks == null) return "";
            for (int i = 0; i < Boot.QuestMarks.Length; i++)
            {
                if (Boot.QuestMarks[i].NpcEntityId != entityId) continue;
                switch (Boot.QuestMarks[i].State)
                {
                    case QuestMarkState.Available:     return Mark("!", "#FFD23C");
                    case QuestMarkState.ReadyToHandIn: return Mark("?", "#FFD23C");
                    case QuestMarkState.InProgress:    return Mark("?", "#9AA3AD");
                }
                return "";
            }
            return "";

            static string Mark(string glyph, string hex) =>
                "<color=" + hex + ">" + QuestMarkOpen + glyph + QuestMarkClose + "</color>";
        }

        /// <summary>The confirm dialog, GROWN to fit its message.
        ///
        /// It used to be a fixed 520x200 panel with an 80px text box, which was fine for one-line
        /// questions ("Sell 3 x Potion?") and broke the moment the vendor confirmation started carrying
        /// the item's full stat block — the text simply ran out through the bottom of the panel, past
        /// the buttons (owner: "the vendor details are good, just coming out of the confirm dialogue").
        ///
        /// Measuring with TMP's own GetPreferredValues means the dialog fits whatever it is given rather
        /// than every caller having to guess a height. The clamp keeps it on screen on a phone; the
        /// bottom padding always reserves room for the buttons, so text can never overlap them.</summary>
        private void Ask(string message, string okLabel, Action action)
        {
            _confirmText.text = message;

            var textRect = (RectTransform)_confirmText.transform;
            const float pad = 22f, buttonRow = 52f, gap = 18f;
            float wrapWidth = textRect.sizeDelta.x;
            // Cap the TEXT, not the panel, so the buttons keep their room whatever the message is.
            float textH = Mathf.Clamp(_confirmText.GetPreferredValues(message, wrapWidth, 0f).y,
                                      56f, 460f);
            textRect.sizeDelta = new Vector2(wrapWidth, textH);
            _confirmPanel.sizeDelta = new Vector2(_confirmPanel.sizeDelta.x,
                                                  pad + textH + gap + buttonRow + pad);

            UiKit.SetButtonText(_confirmOkButton, okLabel);
            _confirmAction = action;
            _confirmPanel.gameObject.SetActive(true);
        }

        private void Dismiss()
        {
            _confirmAction = null;
            _confirmPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Leave the world and log out BEFORE the process dies, then quit.
        ///
        /// "Gracefully" is not politeness: the server keys a session to the connection, and a client
        /// that vanishes is a link-dead player — it holds the character in world through a grace
        /// period, and the save happens on logout. Killing the process instead of logging out means
        /// the last minutes of play can be lost and the character lingers as a target.
        ///
        /// The quit still happens if the network calls fail or hang; a shutdown that can be blocked by
        /// a dead socket is worse than an ungraceful one.
        /// </summary>
        private void QuitGracefully()
        {
            StartCoroutine(QuitRoutine());
        }

        private System.Collections.IEnumerator QuitRoutine()
        {
            _confirmText.text = "Logging out ...";
            _confirmPanel.gameObject.SetActive(true);

            if (Boot.Phase == ClientPhase.InWorld)
            {
                Boot.LeaveWorld();
                float until = Time.realtimeSinceStartup + 1.5f;
                while (Boot.Phase == ClientPhase.InWorld && Time.realtimeSinceStartup < until)
                    yield return null;
            }

            Boot.Logout();
            yield return new WaitForSecondsRealtime(0.6f);

            _quitConfirmed = true;
            Application.Quit();
        }
    }
}
