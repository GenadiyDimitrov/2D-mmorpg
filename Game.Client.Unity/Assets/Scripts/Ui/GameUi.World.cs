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
        private TextMeshProUGUI _selfName, _targetName, _targetDetail;
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
        private RectTransform _consolePanel, _consoleContent;
        private ScrollRect _consoleScroll;
        private int _seenLogRevision = -1;
        private long _renderedLogSeq = -1;   // highest ClientLog.Line.Seq already drawn as a row
        private int _seenClearGen = -1;      // last ClientLog.ClearGeneration we rebuilt for

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
        /// <summary>Cap on console ROWS kept alive. Plenty of scrollback, but bounded so the window can
        /// never accumulate hundreds of live labels — see RefreshConsole for why that mattered.</summary>
        private const int ConsoleDisplayRows = 120;

        // bag / debug
        private RectTransform _bagPanel, _bagContent, _debugPanel;
        private int _bagRevision = -1;
        private int _bagTab = 1;             // FILTER value shown: 1 Items (everything unequipped), 2 Quest
        private Button[] _bagTabButtons;
        private int[] _bagTabFilters;        // the BagTabOf value each tab button shows
        private Button _bagDelToggle;
        private bool _bagFastDel;            // when on, each row shows a no-confirm Del button
        private Button _bagEquipToggle;      // expands the paper-doll column (worn gear) beside the list
        private bool _bagEquipOpen;
        private const float BagWidthCollapsed = 460f, BagWidthExpanded = 792f, BagHeight = 560f;
        /// <summary>Left inset of the bag's item list when the equip column is CLOSED.</summary>
        private const float BagListX = 16f;
        /// <summary>How far the list slides right when the equip column opens on its left — exactly the
        /// width the window gains, so the list keeps its position relative to the RIGHT edge.</summary>
        private const float BagEquipColumnWidth = BagWidthExpanded - BagWidthCollapsed;
        private RectTransform _bagListRect;
        private TextMeshProUGUI _bagGoldLabel, _bagSlotsLabel;
        private static readonly Color GoldColour = new Color(0.95f, 0.82f, 0.35f);

        /// <summary>
        /// Open windows, oldest first. The back button pops the LAST one opened, so closing walks back
        /// through the panels in the order you opened them; only when nothing is left does it offer to
        /// quit. Every future window (skills, character sheet, shops, party …) joins this by calling
        /// <see cref="OpenWindow"/> — nothing else needs to know about the back button.
        /// </summary>
        private readonly List<RectTransform> _windows = new List<RectTransform>();

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
        private Button _targetPartyButton, _targetTradeButton, _targetInfoButton;
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
            BuildQuestWindow();
            BuildDialogWindow();
            BuildPartyWindow();
            BuildAutoHuntWindows();
            BuildItemWindows();
            BuildVendorWindow();
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

            // TAPPING YOUR OWN PANEL OPENS THE CHARACTER SHEET. That is what the sheet is about, and
            // it is where you are already looking when you wonder — so "Char" does not need a
            // permanent button competing with the ones you press in a fight. The button is on the
            // BORDER object, so the whole panel is the target rather than a strip of it.
            var open = panel.gameObject.AddComponent<Button>();
            open.targetGraphic = panel.GetComponent<Image>();
            open.onClick.AddListener(() => ToggleWindow(_statsPanel));

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
            UiKit.Place(_targetPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -48f), new Vector2(300f, 176f));
            var inner = _targetPanel.GetChild(0);

            // Deliberately NOT CloseWindow: this panel is not in the stack, and hiding it while the
            // target still existed would only make it reappear on the next frame.
            float chrome = UiKit.WindowChrome(_targetPanel, "Target", () => Boot.TargetId = null);

            _targetName = UiKit.Label(inner, "", 18f);
            UiKit.Place(UiKit.Rect(_targetName.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -chrome - 6f), new Vector2(272f, 24f));

            _targetHp = UiKit.ValueBar(inner, UiKit.Hp);
            UiKit.Place(UiKit.Rect(_targetHp.transform.parent.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -chrome - 34f), new Vector2(276f, 22f));
            _targetHpText = UiKit.BarLabel(_targetHp, 13f);

            // MP bar — shown for PLAYER targets only (owner). A mob's mana tells you nothing you can act
            // on; another player's is what tells a healer whether they can still cast.
            _targetMp = UiKit.ValueBar(inner, UiKit.Mp);
            _targetMpRow = UiKit.Rect(_targetMp.transform.parent.gameObject);
            UiKit.Place(_targetMpRow, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -chrome - 56f), new Vector2(276f, 18f));
            _targetMpText = UiKit.BarLabel(_targetMp, 12f);

            _targetDetail = UiKit.Label(inner, "", 14f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(_targetDetail.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -chrome - 76f), new Vector2(190f, 20f));

            // Two rows of contextual action buttons, so every target command is one tap — no slash typing.
            // The server refuses anything invalid, but RefreshTarget only SHOWS the ones that apply to the
            // current target (enemy vs player) so the frame stays honest. x-slots for three-across.
            float bx0 = 10f, bx1 = 104f, bx2 = 198f, bw = 88f;

            // Top row (y=44): Attack / Follow / Assist.
            _targetAttackButton = UiKit.TextButton(inner, "Attack", () =>
            {
                // The same verb as the second tap and the bar's Attack action — never its own logic.
                if (Boot.TargetId.HasValue) Boot.AttackOrFollow(Boot.TargetId.Value);
            }, 14f);
            UiKit.Place(UiKit.Rect(_targetAttackButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(bx0, 44f), new Vector2(bw, 28f));

            _targetFollowButton = UiKit.TextButton(inner, "Follow", () =>
            {
                if (Boot.TargetId.HasValue) Boot.Follow(Boot.TargetId.Value);
            }, 14f);
            UiKit.Place(UiKit.Rect(_targetFollowButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(bx1, 44f), new Vector2(bw, 28f));

            _targetAssistButton = UiKit.TextButton(inner, "Assist", () =>
            {
                if (Boot.TargetId.HasValue) Boot.Assist(Boot.TargetId.Value);
            }, 14f);
            UiKit.Place(UiKit.Rect(_targetAssistButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(bx2, 44f), new Vector2(bw, 28f));

            // Bottom row (y=8): Party / Trade / Info.
            _targetPartyButton = UiKit.TextButton(inner, "Party", () =>
            {
                if (Boot.TargetId.HasValue) Boot.PartyInvite(Boot.TargetId.Value);
            }, 14f);
            UiKit.Place(UiKit.Rect(_targetPartyButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(bx0, 8f), new Vector2(bw, 28f));

            _targetTradeButton = UiKit.TextButton(inner, "Trade", () =>
            {
                if (Boot.TargetId.HasValue)
                {
                    var id = Boot.TargetId.Value;
                    Boot.Trade(n => n.TradeRequestAsync(id), "request");
                }
            }, 14f);
            UiKit.Place(UiKit.Rect(_targetTradeButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(bx1, 8f), new Vector2(bw, 28f));

            _targetInfoButton = UiKit.TextButton(inner, "Info", OpenTargetDetails, 14f);
            UiKit.Place(UiKit.Rect(_targetInfoButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(bx2, 8f), new Vector2(bw, 28f));
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
            var log = UiKit.TextButton(_worldRoot, "Chat", () => ToggleWindow(_consolePanel), 17f);
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
            }

            if (Mathf.Approximately(lift, _keyboardLift)) return;
            _keyboardLift = lift;

            for (int i = 0; i < _cmdBarRects.Length; i++)
                _cmdBarRects[i].anchoredPosition = _cmdBarHome[i] + new Vector2(0f, lift);
        }

        /// <summary>
        /// The action bar: five buttons TOP-RIGHT in two rows, with the rarely-pressed ones behind a
        /// Menu. Row one is [PvP][Auto][Menu], row two [Bag][Skills].
        ///
        /// It used to be ten buttons across the whole bottom edge, which is the worst place for them
        /// on a phone: that strip is where the thumbs rest and where the chat and skill bar want to
        /// live, and ten equal buttons gave the same prominence to "Bag" (constantly) and "Leave"
        /// (once). Top-right is reachable, out of the way of the thumbs, and the split is by FREQUENCY:
        /// what you press mid-fight stays out, what you press once a session goes in the Menu.
        ///
        /// Char is not here at all — you open it by tapping your own HP panel, which is both a
        /// shortcut and where you were already looking.
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
                // Restore lives on the MENU, not at a vendor (C18): binning is a field accident, and an
                // undo you can only reach in town is no undo at all.
                ("Restore", () => { CloseWindow(_menuPanel); OpenRestoreWindow(); }, false),
                ("Rank",   () => { CloseWindow(_menuPanel); OpenRank(); }, false),
                ("Setup",  () => { CloseWindow(_menuPanel); ToggleWindow(_settingsPanel); }, false),
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
            _consolePanel = UiKit.PanelBox(_worldRoot, "Console");
            UiKit.Place(_consolePanel, new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(12f, 132f), new Vector2(760f, 320f));
            var inner = _consolePanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_consolePanel, "Chat", () => CloseWindow(_consolePanel));

            // Tabs. "PM" rather than "Whisper": the row has five buttons across 760px on a phone, and
            // every MMO player already reads PM.
            string[] names = { "All", "Local", "World", "PM", "System" };
            _chatTabButtons = new Button[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                int value = _chatTabValues[i];
                var button = UiKit.TextButton(inner, names[i], () => SetChatTab(value), 15f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(12f + i * 100f, -chrome - 4f), new Vector2(96f, 32f));
                _chatTabButtons[i] = button;
            }

            ScrollRect scroll;
            _consoleContent = UiKit.ScrollArea(inner, out scroll, 1f);
            _consoleScroll = scroll;
            UiKit.Stretch((RectTransform)scroll.transform, 10f, chrome + 40f, 10f, 46f);

            var clear = UiKit.TextButton(inner, "Clear", () => ClientLog.Clear(), 16f);
            UiKit.Place(UiKit.Rect(clear.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-10f, 8f), new Vector2(100f, 34f));

            // Reply: fills the command box with "/w <last whisperer> ". Answering a whisper otherwise
            // means retyping a name you can see on screen but cannot copy.
            _chatReplyButton = UiKit.TextButton(inner, "Reply",
                                                () => Boot.ComposeWhisper(Boot.LastWhisperName), 16f);
            UiKit.Place(UiKit.Rect(_chatReplyButton.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-118f, 8f), new Vector2(100f, 34f));

            HighlightChatTabs();
            _consolePanel.gameObject.SetActive(false);
        }

        /// <summary>Switch tab: the rows are a FILTERED projection of one buffer, so changing the filter
        /// throws the drawn rows away and redraws from the buffer. That is the expensive path the append
        /// rewrite removed from the per-LINE case — here it happens once per tap, not per message.</summary>
        private void SetChatTab(int tab)
        {
            if (_chatTab == tab) return;
            _chatTab = tab;
            RebuildConsoleRows();
            HighlightChatTabs();
        }

        private void HighlightChatTabs()
        {
            if (_chatTabButtons == null) return;
            for (int i = 0; i < _chatTabButtons.Length; i++)
                _chatTabButtons[i].targetGraphic.color =
                    _chatTabValues[i] == _chatTab ? UiKit.TabActive : UiKit.PanelLight;
        }

        /// <summary>Drop every drawn row and let the next refresh redraw the ones the filter accepts.</summary>
        private void RebuildConsoleRows()
        {
            for (int i = _consoleContent.childCount - 1; i >= 0; i--)
                Destroy(_consoleContent.GetChild(i).gameObject);
            _renderedLogSeq = -1;
            _seenLogRevision = -1;   // force RefreshConsole to run even if no new line has arrived
        }

        /// <summary>True if a line belongs in the tab being shown. All (-1) accepts everything.</summary>
        private bool ChatTabAccepts(ClientLog.Tab where) => _chatTab < 0 || (int)where == _chatTab;

        private void BuildBag()
        {
            _bagPanel = UiKit.PanelBox(_worldRoot, "Bag");
            UiKit.Place(_bagPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(BagWidthCollapsed, BagHeight));
            var inner = _bagPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_bagPanel, "Bag", () => CloseWindow(_bagPanel));

            // Header line over the LEFT (list) region: gold left, slot usage beside it. Left-anchored so
            // they stay put when the window widens to reveal the equip column.
            _bagGoldLabel = UiKit.Label(inner, "", 15f, GoldColour, TextAlignmentOptions.Left);
            UiKit.Place(UiKit.Rect(_bagGoldLabel.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 8f), new Vector2(230f, 22f));
            _bagSlotsLabel = UiKit.Label(inner, "", 14f, UiKit.TextDim, TextAlignmentOptions.Right);
            UiKit.Place(UiKit.Rect(_bagSlotsLabel.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(252f, -chrome - 8f), new Vector2(182f, 22f));

            // Row: Items / Quest tabs, then the Equip TOGGLE (expands the paper-doll column), then the
            // Fast-Del toggle. Del's per-row buttons are hidden until it's on (owner) so a stray tap can't
            // bin an item. Tabs hold FILTER values (BagTabOf), not indices — there is no "Equip" list tab
            // any more; worn gear lives on the paper-doll.
            // EQUIP goes FIRST (owner) — it is the one that changes the window's shape, and the thing you
            // reach for most, so it leads the row rather than sitting third.
            _bagEquipToggle = UiKit.TextButton(inner, "Equip", ToggleBagEquip, 14f);
            UiKit.Place(UiKit.Rect(_bagEquipToggle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -chrome - 36f), new Vector2(92f, 32f));

            _bagTabButtons = new Button[2];
            _bagTabFilters = new[] { 1, 2 };
            string[] tabs = { "Items", "Quest" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int filter = _bagTabFilters[i];
                var button = UiKit.TextButton(inner, tabs[i], () => { _bagTab = filter; _bagRevision = -1; }, 15f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(112f + i * 96f, -chrome - 36f), new Vector2(92f, 32f));
                _bagTabButtons[i] = button;
            }

            _bagDelToggle = UiKit.TextButton(inner, "Del: off",
                () => { _bagFastDel = !_bagFastDel; _bagRevision = -1; }, 14f);
            UiKit.Place(UiKit.Rect(_bagDelToggle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(304f, -chrome - 36f), new Vector2(92f, 32f));

            // The item list is a FIXED-width column, so widening the window for the equip column never
            // stretches it — it just slides.
            ScrollRect scroll;
            _bagContent = UiKit.ScrollArea(inner, out scroll, 3f);
            _bagListRect = UiKit.Rect(scroll.transform.gameObject);
            UiKit.Place(_bagListRect, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(BagListX, -chrome - 74f), new Vector2(418f, BagHeight - chrome - 90f));

            // The paper-doll column (hidden until the Equip toggle) opens on the LEFT (owner), so it
            // appears where the window grows rather than pushing the list off toward the far edge. The
            // list slides right by exactly the column's width when it opens; see ToggleBagEquip.
            BuildEquipColumn(inner, new Vector2(BagListX, -chrome - 74f));   // below the header row, aligned with the list (was -8, which put the Head slot over the tabs)

            _bagPanel.gameObject.SetActive(false);
        }

        /// <summary>Expand/collapse the bag to show the worn-gear paper-doll column beside the item list.</summary>
        private void ToggleBagEquip()
        {
            _bagEquipOpen = !_bagEquipOpen;
            if (_equipColumn != null) _equipColumn.gameObject.SetActive(_bagEquipOpen);
            _bagPanel.sizeDelta = new Vector2(_bagEquipOpen ? BagWidthExpanded : BagWidthCollapsed, BagHeight);
            // Slide the list right so the paper-doll has the left side to itself.
            if (_bagListRect != null)
            {
                var p = _bagListRect.anchoredPosition;
                p.x = _bagEquipOpen ? BagListX + BagEquipColumnWidth : BagListX;
                _bagListRect.anchoredPosition = p;
            }
            _bagEquipToggle.targetGraphic.color = _bagEquipOpen ? UiKit.TabActive : UiKit.PanelLight;
            _equipRevision = -1;   // force the paper-doll to repaint on next refresh
        }

        /// <summary>Which bag tab an item belongs to: 0 Equip = what you're WEARING; 2 Quest; 1 Items =
        /// everything else, INCLUDING unequipped gear (owner: an unequipped item lives in the Items bag,
        /// not the Equipment bag).</summary>
        private static int BagTabOf(InventoryItemDto item, ItemDef def)
        {
            if (item.Equipped) return 0;
            if (def != null && def.Slot == EquipSlot.QuestItem) return 2;
            return 1;
        }

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

            string level = target.Kind == EntityKind.Player && target.Level <= 0 ? "" : "  Lv " + target.Level;
            _targetName.text = target.Name + level + (target.Dead ? "   (dead)" : "");
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

            _targetDetail.text = target.Kind + (target.Aggressive ? "   aggressive" : "");

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
        }

        /// <summary>The target frame for a party member the world snapshot no longer carries (they
        /// walked out of interest range). Everything here comes from the roster push, which keeps
        /// running at any distance — so the frame stays up and the actions that name a target by id
        /// (assist, heal, buff, kick, change leader) remain reachable. The fast buttons are all
        /// mob-only anyway, so none of them appear.</summary>
        private void DrawDistantPartyTarget(PartyMemberDto m)
        {
            _targetPanel.gameObject.SetActive(true);
            _targetName.text = m.Name + "  Lv " + m.Level;

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
            _targetDetail.text = (string.IsNullOrEmpty(m.ClassName) ? "Party" : m.ClassName)
                               + "   (out of sight)";

            if (_targetPartyButton != null) _targetPartyButton.gameObject.SetActive(false);
            if (_targetTradeButton != null) _targetTradeButton.gameObject.SetActive(false);
            if (_targetFollowButton != null) _targetFollowButton.gameObject.SetActive(false);
            if (_targetAssistButton != null) _targetAssistButton.gameObject.SetActive(false);
            if (_targetAttackButton != null) _targetAttackButton.gameObject.SetActive(false);
            if (_targetInfoButton != null) _targetInfoButton.gameObject.SetActive(false);
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
            if (!_consolePanel.gameObject.activeSelf || _seenLogRevision == ClientLog.Revision) return;
            _seenLogRevision = ClientLog.Revision;

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
            if (_seenClearGen != ClientLog.ClearGeneration)
            {
                _seenClearGen = ClientLog.ClearGeneration;
                RebuildConsoleRows();
            }

            int appended = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Seq <= _renderedLogSeq) continue;   // already drawn
                // A line the current tab does not want is SKIPPED, not remembered: _renderedLogSeq only
                // advances past lines that were actually drawn, so switching tabs (which resets it to
                // -1) redraws the buffer from the start with the new filter.
                if (!ChatTabAccepts(lines[i].Where)) continue;
                var label = UiKit.Label(_consoleContent, lines[i].Text, 15f, lines[i].Color);
                // Rows GROW with wrapped text instead of a fixed height. A fixed row is what made
                // long messages draw over each other in the IMGUI console.
                var fitter = label.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                _renderedLogSeq = lines[i].Seq;
                appended++;
            }

            // Trim the oldest rows so the window never holds more than the cap.
            // ⚠ Count the excess ONCE and destroy that many by index. A `while (childCount > cap)` loop
            // FREEZES: Unity's Destroy is deferred to end-of-frame, so childCount does NOT drop inside
            // the loop — the condition stays true and GetChild(0) keeps returning the same (already
            // marked) object forever. That infinite loop is what locked the phone in 0.28.77; this is
            // the real fix on top of the append rewrite.
            int excess = _consoleContent.childCount - ConsoleDisplayRows;
            for (int i = 0; i < excess; i++)
                Destroy(_consoleContent.GetChild(i).gameObject);

            if (appended > 0)
            {
                Canvas.ForceUpdateCanvases();
                _consoleScroll.verticalNormalizedPosition = 0f;   // newest line
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

            int revision = items.Length * 17 + _bagTab * 7919 + (_bagFastDel ? 104729 : 0) + (int)(Boot.Gold % 1_000_000);
            foreach (var item in items)
                revision = revision * 31 + item.InstanceId.GetHashCode()
                         + (item.Equipped ? 1 : 0) + item.Quantity * 7 + item.Enchant;
            if (revision == _bagRevision) return;
            _bagRevision = revision;

            _bagGoldLabel.text = "Gold: " + Boot.Gold.ToString("N0");
            _bagSlotsLabel.text = "Slots " + used + " / " + GameConstants.InventorySize;
            for (int i = 0; i < _bagTabButtons.Length; i++)
                _bagTabButtons[i].targetGraphic.color = _bagTabFilters[i] == _bagTab ? UiKit.TabActive : UiKit.PanelLight;
            UiKit.SetButtonText(_bagDelToggle, _bagFastDel ? "Del: ON" : "Del: off");
            _bagDelToggle.targetGraphic.color = _bagFastDel ? new Color(0.42f, 0.20f, 0.20f, 0.95f) : UiKit.PanelLight;

            for (int i = _bagContent.childCount - 1; i >= 0; i--)
                Destroy(_bagContent.GetChild(i).gameObject);

            bool anyInTab = false;
            foreach (var item in items)
            {
                var def = ItemCatalog.Get(item.DefId);
                if (BagTabOf(item, def) != _bagTab) continue;
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
                var empty = UiKit.Label(_bagContent,
                    _bagTab == 0 ? "No equipment." : _bagTab == 2 ? "No quest items." : "No items.",
                    17f, UiKit.TextDim);
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

        /// <summary>A worn title is gold and smaller than the name — it must read as a decoration on the
        /// character, not as part of who they are. The name keeps its own colour, which already means
        /// something (level gap, PvP flag), so the title cannot borrow it.</summary>
        private static readonly Color TitleColour = new Color(0.95f, 0.83f, 0.45f, 1f);

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

                // THE WORN TITLE, on its own gold line above the name. The server has already decided
                // whether it is still held — an unheld one arrives as "" — so there is nothing to check
                // here beyond "is there text".
                bool hasTitle = !string.IsNullOrEmpty(e.Title);
                plate.TitleLabel.gameObject.SetActive(hasTitle);
                if (hasTitle) plate.TitleLabel.text = e.Title;

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
