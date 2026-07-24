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
        private RectTransform _skillBarPanel;
        private readonly Button[] _slotButtons = new Button[SlotsPerPage];
        private readonly TextMeshProUGUI[] _slotFaces = new TextMeshProUGUI[SlotsPerPage];
        private readonly Image[] _slotBorders = new Image[SlotsPerPage];
        private readonly TextMeshProUGUI[] _slotCancel = new TextMeshProUGUI[SlotsPerPage];
        private readonly TextMeshProUGUI[] _slotAutoMarks = new TextMeshProUGUI[SlotsPerPage];

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

        // console
        private RectTransform _consolePanel, _consoleContent;
        private ScrollRect _consoleScroll;
        private int _seenLogRevision = -1;

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
        private const float BagWidthCollapsed = 460f, BagWidthExpanded = 792f, BagHeight = 500f;
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

        private Button _debugButton, _pvpButton, _autoButton, _respawnButton;
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
                if (Boot.TargetId.HasValue) Boot.Attack(Boot.TargetId.Value);
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
            const float slot = 78f, pad = 6f;
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

            var log = UiKit.TextButton(_worldRoot, "Log", () => ToggleWindow(_consolePanel), 17f);
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

        /// <summary>The overflow menu: everything you press once a session rather than once a fight.</summary>
        private void BuildMenuPanel()
        {
            _menuPanel = UiKit.PanelBox(_worldRoot, "Menu");
            UiKit.Place(_menuPanel, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-12f, -100f), new Vector2(200f, 392f));
            var inner = _menuPanel.GetChild(0);

            var entries = new List<(string Label, Action Click)>
            {
                ("Auto Pots", () => { CloseWindow(_menuPanel); OpenAutoPotions(); }),
                ("Auto Farm", () => { CloseWindow(_menuPanel); OpenAutoFarm(); }),
                ("Quests", () => { CloseWindow(_menuPanel); ToggleWindow(_questPanel); }),
                ("Rank",   () => { CloseWindow(_menuPanel); OpenRank(); }),
                ("Setup",  () => { CloseWindow(_menuPanel); ToggleWindow(_settingsPanel); }),
                ("Debug",  () => { CloseWindow(_menuPanel); ToggleWindow(_debugPanel); }),
                ("Leave",  () => { CloseWindow(_menuPanel); Boot.LeaveWorld(); }),
            };

            float y = -10f;
            foreach (var entry in entries)
            {
                var button = UiKit.TextButton(inner, entry.Label, entry.Click, 16f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(0f, y), new Vector2(180f, 46f));
                y -= 52f;

                if (entry.Label == "Debug") _debugButton = button;
            }

            _menuPanel.gameObject.SetActive(false);
        }

        private void BuildConsole()
        {
            _consolePanel = UiKit.PanelBox(_worldRoot, "Console");
            UiKit.Place(_consolePanel, new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(12f, 132f), new Vector2(760f, 320f));
            var inner = _consolePanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_consolePanel, "Log", () => CloseWindow(_consolePanel));

            ScrollRect scroll;
            _consoleContent = UiKit.ScrollArea(inner, out scroll, 1f);
            _consoleScroll = scroll;
            UiKit.Stretch((RectTransform)scroll.transform, 10f, chrome + 6f, 10f, 46f);

            var clear = UiKit.TextButton(inner, "Clear", () => ClientLog.Clear(), 16f);
            UiKit.Place(UiKit.Rect(clear.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-10f, 8f), new Vector2(100f, 34f));

            _consolePanel.gameObject.SetActive(false);
        }

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
            _bagTabButtons = new Button[2];
            _bagTabFilters = new[] { 1, 2 };
            string[] tabs = { "Items", "Quest" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int filter = _bagTabFilters[i];
                var button = UiKit.TextButton(inner, tabs[i], () => { _bagTab = filter; _bagRevision = -1; }, 15f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(16f + i * 96f, -chrome - 36f), new Vector2(92f, 32f));
                _bagTabButtons[i] = button;
            }

            _bagEquipToggle = UiKit.TextButton(inner, "Equip", ToggleBagEquip, 14f);
            UiKit.Place(UiKit.Rect(_bagEquipToggle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(208f, -chrome - 36f), new Vector2(92f, 32f));

            _bagDelToggle = UiKit.TextButton(inner, "Del: off",
                () => { _bagFastDel = !_bagFastDel; _bagRevision = -1; }, 14f);
            UiKit.Place(UiKit.Rect(_bagDelToggle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(304f, -chrome - 36f), new Vector2(92f, 32f));

            // The item list is a FIXED-width column on the left, so widening the window for the equip
            // column never stretches it.
            ScrollRect scroll;
            _bagContent = UiKit.ScrollArea(inner, out scroll, 3f);
            UiKit.Place(UiKit.Rect(scroll.transform.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -chrome - 74f), new Vector2(418f, BagHeight - chrome - 90f));

            // The paper-doll column (hidden until the Equip toggle) sits to the right of the list.
            BuildEquipColumn(inner, new Vector2(446f, -chrome - 8f));

            _bagPanel.gameObject.SetActive(false);
        }

        /// <summary>Expand/collapse the bag to show the worn-gear paper-doll column beside the item list.</summary>
        private void ToggleBagEquip()
        {
            _bagEquipOpen = !_bagEquipOpen;
            if (_equipColumn != null) _equipColumn.gameObject.SetActive(_bagEquipOpen);
            _bagPanel.sizeDelta = new Vector2(_bagEquipOpen ? BagWidthExpanded : BagWidthCollapsed, BagHeight);
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
            RefreshBag();
            RefreshSkillsWindow();
            RefreshStatsWindow();
            RefreshTargetWindow();
            RefreshQuestWindow();
            RefreshDialogWindow();
            RefreshPartyWindow();
            RefreshVendorWindow();
            RefreshEquipmentWindow();
            RefreshRegionUi();
            RefreshFarmRing();
            RefreshNameplates();

            RefreshFeedback();

            _debugButton.gameObject.SetActive(Boot.IsAdmin);

            // Respawn only while dead — the rest of the time it is a button that can do nothing.
            EntityDto self = null;
            if (Boot.Entities != null) Boot.Entities.TryGetState(Boot.SelfId, out self);
            _respawnButton.gameObject.SetActive(self != null && self.Dead);
            UiKit.SetButtonText(_pvpButton, Boot.PvpEnabled ? "PvP: ON" : "PvP: off");
            _pvpButton.targetGraphic.color = Boot.PvpEnabled
                ? new Color(0.55f, 0.20f, 0.20f, 0.95f) : UiKit.PanelLight;

            UiKit.SetButtonText(_autoButton, Boot.AutoHunting ? "Auto: ON" : "Auto: off");
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

                // An EMPTY slot is a disabled button — which made it impossible to place anything,
                // because the only target for a pending skill is an empty slot. While an assignment or
                // a move is waiting, every slot has to be pressable.
                _slotButtons[i].interactable = usable || _pendingAssign != null || _pendingMoveFrom >= 0;

                // Thin green frame + a corner "A" = the auto-hunt will use this one.
                bool auto = !string.IsNullOrEmpty(token) && Boot.AutoSkills.Contains(AutoIdFor(token));
                _slotBorders[i].enabled = auto;
                _slotAutoMarks[i].gameObject.SetActive(auto);

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
            if (bar != null && index < bar.Length) Boot.UseSlot(bar[index]);
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
        private static string AutoIdFor(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            var action = ActionCatalog.FromToken(token);
            if (action != null)
                return action.Id == GameConstants.ActionBasicAttack ? AutoHuntIds.BasicAttack : null;

            return SkillCatalog.Get(token) != null && !IsPassive(token) ? token : null;
        }

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

        private void RefreshConsole()
        {
            if (!_consolePanel.gameObject.activeSelf || _seenLogRevision == ClientLog.Revision) return;
            _seenLogRevision = ClientLog.Revision;

            for (int i = _consoleContent.childCount - 1; i >= 0; i--)
                Destroy(_consoleContent.GetChild(i).gameObject);

            var lines = ClientLog.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                var label = UiKit.Label(_consoleContent, lines[i].Text, 15f, lines[i].Color);
                // Rows GROW with wrapped text instead of a fixed height. A fixed row is what made
                // long messages draw over each other in the IMGUI console.
                var fitter = label.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            Canvas.ForceUpdateCanvases();
            _consoleScroll.verticalNormalizedPosition = 0f;   // newest line
        }

        private void RefreshBag()
        {
            if (!_bagPanel.gameObject.activeSelf) return;

            // Cheap change stamp: the server pushes the WHOLE bag on any change, so length plus the
            // equipped/quantity state is enough to know when the rows need rebuilding.
            var items = Boot.Inventory ?? Array.Empty<InventoryItemDto>();
            int used = 0;
            foreach (var it in items) if (!it.Equipped) used++;   // worn gear doesn't take a slot

            int revision = items.Length * 17 + _bagTab * 7919 + (_bagFastDel ? 104729 : 0) + (int)(Boot.Gold % 1_000_000);
            foreach (var item in items)
                revision = revision * 31 + (item.Equipped ? 1 : 0) + item.Quantity * 7 + item.Enchant;
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

                var label = UiKit.Label(row.transform, name, 17f,
                                        item.Equipped ? UiKit.Good : UiKit.Text,
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
        }

        private void RefreshNameplates()
        {
            var cam = Camera.main;
            if (cam == null || Boot.Entities == null) return;

            // Your own level, for the mob level-gap colours. Progress is authoritative for yourself;
            // the entity's own Level is the fallback before the first progress push.
            int myLevel = Boot.Progress != null ? Boot.Progress.Level : 0;
            if (myLevel <= 0 && Boot.Entities.TryGetState(Boot.SelfId, out var me)) myLevel = me.Level;

            int used = 0;
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
                plate.Label.text = title;

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

                bool bar = e.MaxHp > 0 && e.Kind != EntityKind.Npc;
                plate.BarBg.gameObject.SetActive(bar);
                if (bar) UiKit.SetBar(plate.BarFill, e.Hp, e.MaxHp);
            }

            for (int i = used; i < _nameplates.Count; i++)
                _nameplates[i].Root.gameObject.SetActive(false);
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

                _nameplates.Add(new Nameplate
                {
                    Root = root,
                    Label = label,
                    BarBg = bg.GetComponent<Image>(),
                    BarFill = fill,
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

        private void Ask(string message, string okLabel, Action action)
        {
            _confirmText.text = message;
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
