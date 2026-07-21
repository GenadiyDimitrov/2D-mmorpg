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
        private TextMeshProUGUI _selfName, _selfDetail, _targetName, _targetDetail;
        private Image _selfHp, _selfMp, _selfXp, _targetHp;
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
        private RectTransform _slotMenu;
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

        private int _townIndex;
        private TextMeshProUGUI _townLabel;
        private Button _debugButton, _pvpButton, _autoButton;

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
            BuildFeedback();
            BuildSkillDetail();
            BuildStatsWindow();
            BuildTargetWindow();
            BuildSettingsWindow();
            BuildQuestWindow();
            BuildSlotMenu();
        }

        private void BuildSelfPanel()
        {
            var panel = UiKit.PanelBox(_worldRoot, "SelfPanel");
            UiKit.Place(panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -48f), new Vector2(330f, 116f));
            var inner = panel.GetChild(0);

            _selfName = UiKit.Label(inner, "waiting for your entity ...", 19f);
            UiKit.Place(UiKit.Rect(_selfName.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -8f), new Vector2(300f, 24f));

            _selfHp = UiKit.ValueBar(inner, UiKit.Hp);
            UiKit.Place(UiKit.Rect(_selfHp.transform.parent.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -36f), new Vector2(306f, 18f));
            _selfMp = UiKit.ValueBar(inner, UiKit.Mp);
            UiKit.Place(UiKit.Rect(_selfMp.transform.parent.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -58f), new Vector2(306f, 18f));
            _selfXp = UiKit.ValueBar(inner, UiKit.Xp);
            UiKit.Place(UiKit.Rect(_selfXp.transform.parent.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -80f), new Vector2(306f, 10f));

            _selfDetail = UiKit.Label(inner, "", 14f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(_selfDetail.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -94f), new Vector2(306f, 20f));
        }

        private void BuildTargetPanel()
        {
            _targetPanel = UiKit.PanelBox(_worldRoot, "TargetPanel");
            UiKit.Place(_targetPanel, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-12f, -48f), new Vector2(300f, 84f));
            var inner = _targetPanel.GetChild(0);

            _targetName = UiKit.Label(inner, "", 18f);
            UiKit.Place(UiKit.Rect(_targetName.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -8f), new Vector2(272f, 24f));

            _targetHp = UiKit.ValueBar(inner, UiKit.Hp);
            UiKit.Place(UiKit.Rect(_targetHp.transform.parent.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -36f), new Vector2(276f, 18f));

            _targetDetail = UiKit.Label(inner, "", 14f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(_targetDetail.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -58f), new Vector2(190f, 20f));

            // Details are PULLED — the server only sends them when asked, so there has to be an ask.
            var info = UiKit.TextButton(inner, "Info", () =>
            {
                if (!Boot.TargetId.HasValue) return;
                Boot.InspectTarget(Boot.TargetId.Value, _wantDrops);
                OpenWindow(_detailsPanel);
            }, 14f);
            UiKit.Place(UiKit.Rect(info.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-10f, 8f), new Vector2(76f, 28f));
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
                        new Vector2(-12f, 78f), new Vector2(w, h));
            var inner = _skillBarPanel.GetChild(0);

            for (int i = 0; i < SlotsPerPage; i++)
            {
                int index = i;   // captured; the loop variable is shared
                var at = new Vector2(pad + (i % BarColumns) * (slot + pad),
                                     -(pad + (i / BarColumns) * (slot + pad)));

                // The auto-use marker: a green frame drawn BEHIND the slot and peeking out a couple of
                // pixels. Added first so it renders under the button — a border you have to look for
                // is useless, one that covers the icon is worse.
                var border = UiKit.Box(inner, "AutoBorder", new Color(0.35f, 0.85f, 0.40f), blocksInput: false);
                UiKit.Place(UiKit.Rect(border.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            at + new Vector2(-3f, 3f), new Vector2(slot + 6f, slot + 6f));
                border.enabled = false;
                _slotBorders[i] = border;

                // No onClick: PressAndHold owns both gestures, so a hold can open the menu without
                // the button also casting what was in it on release.
                var button = UiKit.TextButton(inner, "", null, 20f);
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
            _commandField = UiKit.InputField(_worldRoot, "say  /  !world  /  /w name msg");
            UiKit.Place(UiKit.Rect(_commandField.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(12f, 78f), new Vector2(700f, 46f));
            // Submit on the keyboard's enter/done key — the mobile keyboard has no Send button of ours.
            _commandField.onSubmit.AddListener(_ => SubmitCommand());

            var send = UiKit.TextButton(_worldRoot, "Send", SubmitCommand, 17f);
            UiKit.Place(UiKit.Rect(send.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(720f, 78f), new Vector2(96f, 46f));

            var log = UiKit.TextButton(_worldRoot, "Log", () => ToggleWindow(_consolePanel), 17f);
            UiKit.Place(UiKit.Rect(log.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(824f, 78f), new Vector2(90f, 46f));
        }

        /// <summary>
        /// The action bar SIZES ITSELF to fit. Buttons used to be a fixed 126 wide stepping by 132,
        /// which silently ran off the right edge once there were ten of them — "Leave" was already
        /// past the screen before this window was added. Dividing the available width means adding a
        /// button shrinks the row instead of pushing one into the void.
        /// </summary>
        private void BuildActionBar()
        {
            var actions = new List<(string Label, Action Click)>
            {
                ("Attack",    () => { if (Boot.TargetId.HasValue) Boot.Attack(Boot.TargetId.Value); }),
                ("Sit/Stand", () => Boot.SetMoveState(
                                  Boot.Stats != null && Boot.Stats.MoveState == MoveState.Sitting
                                      ? MoveState.Running : MoveState.Sitting)),
                ("Walk/Run",  () => Boot.SetMoveState(
                                  Boot.Stats != null && Boot.Stats.MoveState == MoveState.Walking
                                      ? MoveState.Running : MoveState.Walking)),
                ("Respawn",   () => Boot.Respawn()),
                ("PvP: off",  () => Boot.TogglePvp()),
                ("Auto: off", () => Boot.ToggleAutoHunt()),
                ("Bag",       () => ToggleWindow(_bagPanel)),
                ("Skills",    () => ToggleWindow(_skillsPanel)),
                ("Char",      () => ToggleWindow(_statsPanel)),
                ("Setup",     () => ToggleWindow(_settingsPanel)),
                ("Quests",    () => ToggleWindow(_questPanel)),
                ("Debug",     () => ToggleWindow(_debugPanel)),
                ("Leave",     () => Boot.LeaveWorld()),
            };

            const float margin = 12f, gap = 4f;
            float width = (UiKit.Reference.x - margin * 2f - gap * (actions.Count - 1)) / actions.Count;
            float x = margin;

            foreach (var action in actions)
            {
                var button = UiKit.TextButton(_worldRoot, action.Label, action.Click, 15f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                            new Vector2(x, 12f), new Vector2(width, 52f));
                x += width + gap;

                if (action.Label == "PvP: off") _pvpButton = button;
                else if (action.Label == "Auto: off") _autoButton = button;
                else if (action.Label == "Debug") _debugButton = button;
            }
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
                        Vector2.zero, new Vector2(620f, 460f));
            var inner = _bagPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_bagPanel, "Bag", () => CloseWindow(_bagPanel));

            ScrollRect scroll;
            _bagContent = UiKit.ScrollArea(inner, out scroll, 3f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 10f, 16f, 16f);

            _bagPanel.gameObject.SetActive(false);
        }

        private void BuildDebugPanel()
        {
            _debugPanel = UiKit.PanelBox(_worldRoot, "Debug");
            UiKit.Place(_debugPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(560f, 380f));
            var inner = _debugPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_debugPanel, "Debug", () => CloseWindow(_debugPanel));

            float y = -chrome - 14f;
            DebugRow(inner, ref y, "Level",
                ("-10", () => Boot.Debug(n => n.DebugLevelAsync(-10), "level")),
                ("-1",  () => Boot.Debug(n => n.DebugLevelAsync(-1), "level")),
                ("+1",  () => Boot.Debug(n => n.DebugLevelAsync(1), "level")),
                ("+10", () => Boot.Debug(n => n.DebugLevelAsync(10), "level")));

            DebugRow(inner, ref y, "Grant",
                ("Learn all", () => Boot.Debug(n => n.DebugLearnAllAsync(), "learn all")),
                ("Buffs",     () => Boot.Debug(n => n.DebugBuffAsync(), "buff")));

            DebugRow(inner, ref y, "Wealth",
                ("+100k gold", () => Boot.Debug(n => n.DebugGoldAsync(100000), "gold")),
                ("+100k SP",   () => Boot.Debug(n => n.DebugSpAsync(100000), "sp")));

            // Teleport: the town list is shared data, so it can never drift from the real map.
            UiKit.Place(UiKit.Rect(UiKit.Label(inner, "Teleport", 15f, UiKit.TextDim).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, y),
                        new Vector2(120f, 28f));

            var prev = UiKit.TextButton(inner, "<", () => CycleTown(-1), 18f);
            UiKit.Place(UiKit.Rect(prev.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(120f, y), new Vector2(44f, 36f));

            _townLabel = UiKit.Label(inner, "", 17f, UiKit.Text, TextAlignmentOptions.Center);
            UiKit.Place(UiKit.Rect(_townLabel.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(170f, y - 4f), new Vector2(220f, 28f));

            var next = UiKit.TextButton(inner, ">", () => CycleTown(1), 18f);
            UiKit.Place(UiKit.Rect(next.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(396f, y), new Vector2(44f, 36f));

            var go = UiKit.TextButton(inner, "Go", () =>
            {
                var town = WorldMap.SafeZones[_townIndex];
                Boot.Debug(n => n.DebugTeleportAsync(town.X, town.Y), "teleport");
            }, 18f);
            UiKit.Place(UiKit.Rect(go.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(446f, y), new Vector2(80f, 36f));

            CycleTown(0);
            _debugPanel.gameObject.SetActive(false);
        }

        private void DebugRow(Transform parent, ref float y, string title,
                              params (string Text, Action Click)[] buttons)
        {
            UiKit.Place(UiKit.Rect(UiKit.Label(parent, title, 15f, UiKit.TextDim).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, y),
                        new Vector2(100f, 28f));

            float x = 120f;
            foreach (var entry in buttons)
            {
                var button = UiKit.TextButton(parent, entry.Text, entry.Click, 17f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(x, y), new Vector2(100f, 36f));
                x += 106f;
            }
            y -= 48f;
        }

        private void CycleTown(int delta)
        {
            var towns = WorldMap.SafeZones;
            _townIndex = (_townIndex + delta + towns.Length) % towns.Length;
            if (_townLabel != null) _townLabel.text = towns[_townIndex].Name;
        }

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
            RefreshNameplates();

            RefreshFeedback();

            _debugButton.gameObject.SetActive(Boot.IsAdmin);
            UiKit.SetButtonText(_pvpButton, Boot.PvpEnabled ? "PvP: ON" : "PvP: off");
            _pvpButton.targetGraphic.color = Boot.PvpEnabled
                ? new Color(0.55f, 0.20f, 0.20f, 0.95f) : UiKit.PanelLight;

            UiKit.SetButtonText(_autoButton, Boot.AutoHunting ? "Auto: ON" : "Auto: off");
            _autoButton.targetGraphic.color = Boot.AutoHunting
                ? new Color(0.20f, 0.45f, 0.25f, 0.95f) : UiKit.PanelLight;

            UpdateSkillBarSwipe();
        }

        private void RefreshSelf()
        {
            EntityDto self = null;
            if (Boot.Entities != null) Boot.Entities.TryGetState(Boot.SelfId, out self);

            if (self == null)
            {
                _selfName.text = "waiting for your entity ...";
                _selfDetail.text = "";
                return;
            }

            int level = Boot.Progress != null ? Boot.Progress.Level : self.Level;
            _selfName.text = self.Name + "    Lv " + level;
            UiKit.SetBar(_selfHp, self.Hp, self.MaxHp);
            UiKit.SetBar(_selfMp, self.Mp, self.MaxMp);
            if (Boot.Progress != null && Boot.Progress.ExpToNext > 0)
                UiKit.SetBar(_selfXp, Boot.Progress.Exp, Boot.Progress.ExpToNext);

            // Raw server coordinates: the fastest way to tell real movement from camera drift.
            _selfDetail.text = "HP " + self.Hp + "/" + self.MaxHp + "   MP " + self.Mp + "/" + self.MaxMp
                             + "   pos " + Mathf.RoundToInt(self.X) + "," + Mathf.RoundToInt(self.Y)
                             + "   gold " + Boot.Gold;
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
            UiKit.SetBar(_targetHp, target.Hp, target.MaxHp);
            _targetDetail.text = target.Kind + (target.Aggressive ? "   aggressive" : "");
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

                // Green frame = the auto-hunt will use this one.
                _slotBorders[i].enabled = !string.IsNullOrEmpty(token) && Boot.AutoSkills.Contains(token);

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

            if (GameConstants.IsItemSlot(token)) return "[i]";

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
                var token = TokenAt(_menuSlot);
                if (!string.IsNullOrEmpty(token)) Boot.ToggleAutoSkill(token);
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

            // Auto is only offered for a real SKILL the autopilot could cast. Actions and items are
            // not part of the auto-hunt contract, and a passive has nothing to fire.
            bool isSkill = SkillCatalog.Get(token) != null;
            bool autoable = isSkill && !IsPassive(token);

            _slotMenuAuto.gameObject.SetActive(autoable);
            if (autoable)
                UiKit.SetButtonText(_slotMenuAuto, Boot.AutoSkills.Contains(token) ? "Auto: ON" : "Auto: off");

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

            OpenWindow(_slotMenu);
        }

        private void CloseSlotMenu() => CloseWindow(_slotMenu);

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
            int revision = items.Length;
            foreach (var item in items)
                revision = revision * 31 + (item.Equipped ? 1 : 0) + item.Quantity * 7 + item.Enchant;
            if (revision == _bagRevision) return;
            _bagRevision = revision;

            for (int i = _bagContent.childCount - 1; i >= 0; i--)
                Destroy(_bagContent.GetChild(i).gameObject);

            if (items.Length == 0)
            {
                var empty = UiKit.Label(_bagContent, "Empty.", 17f, UiKit.TextDim);
                empty.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
                return;
            }

            foreach (var item in items)
            {
                var def = ItemCatalog.Get(item.DefId);
                var row = UiKit.Box(_bagContent, "Item", UiKit.PanelLight);
                row.gameObject.AddComponent<LayoutElement>().minHeight = 46f;

                string name = def != null ? def.Name : item.DefId;
                if (item.Enchant > 0) name = "+" + item.Enchant + " " + name;
                if (item.Quantity > 1) name += "   x" + item.Quantity;
                if (item.Equipped) name = "* " + name;

                var label = UiKit.Label(row.transform, name, 17f,
                                        item.Equipped ? UiKit.Good : UiKit.Text,
                                        TextAlignmentOptions.Left);
                UiKit.Stretch(UiKit.Rect(label.gameObject), 12f, 0f, 130f, 0f);

                // EquipSlot has no "None" — it classifies EVERY item — so the four WEARABLE slots are
                // tested explicitly. Consumables get Use; anything else gets no button rather than one
                // the server would refuse.
                var id = item.InstanceId;
                bool wearable = def != null &&
                    (def.Slot == EquipSlot.Weapon || def.Slot == EquipSlot.Armor ||
                     def.Slot == EquipSlot.Shield || def.Slot == EquipSlot.Jewel);

                if (wearable)
                {
                    var button = UiKit.TextButton(row.transform, item.Equipped ? "Unequip" : "Equip",
                                                  () => Boot.EquipItem(id), 15f);
                    UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                new Vector2(-8f, 0f), new Vector2(112f, 38f));
                }
                else if (def != null && def.Slot == EquipSlot.Consumable)
                {
                    var button = UiKit.TextButton(row.transform, "Use", () => Boot.UsePotion(id), 15f);
                    UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                new Vector2(-8f, 0f), new Vector2(112f, 38f));
                }
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
                plate.Label.color = e.Id == Boot.SelfId ? UiKit.Good : NameColour(e, myLevel);

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
