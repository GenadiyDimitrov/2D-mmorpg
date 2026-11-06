using System.Linq;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: Settings.
    ///
    /// This is deliberately NOT a port of the WPF settings panel. It exists to answer the questions
    /// that have cost the most time on this client — how big should entity markers be, how high should
    /// the camera sit, where does a nameplate belong, is the UI too large — none of which can be
    /// answered from a desk. Every one of those was previously a guess followed by a four-minute
    /// rebuild and a reinstall.
    ///
    /// Everything here is LOOK, not rules: nothing sent to the server, nothing that changes the game.
    /// Values persist in PlayerPrefs so a rebuild does not reset the player's taste.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _settingsPanel;
        /// <summary>The canvas Setup is drawn on — fixed at ×1 and above the game UI (see EnsureBuilt).</summary>
        private Canvas _setupCanvas;
        private RectTransform _setupRoot;

        private const string PrefPitch      = CameraRig.PitchKey;   // the drag gesture writes it too
        private const string PrefYaw        = CameraRig.YawKey;
        private const string PrefEntity     = "ui.entityScale";
        private const string PrefPlate      = "ui.nameplateHeight";
        /// <summary>UI size as a MULTIPLIER, ×1 = an 800-high design (UiKit.ReferenceHeightAtX1). A new
        /// key rather than the old "ui.referenceHeight": that one stored the raw height, and a phone still
        /// holding the 480 that broke the menu should simply start over at ×1.</summary>
        private const string PrefUiScale    = "ui.scale";
        /// <summary>The skill bar's own size, on top of the UI size. ×1 is a THIRD of the old bar (owner,
        /// 2026-09-26: *"decrease the size of the skill bar 3 times ... range from 1/5 to 5 times the new
        /// size"*).
        /// `BL-320` (2026-09-28): *"make the dafualt of skill bar current x2.5 .. and to range from 1.5 to 4.5
        /// (current numbers)"*. ×1 stays a third of the old bar, so the default is 2.5/3 of it.
        /// `BL-320` again (2026-09-28): *"make the x2.5 to show as x1 ... from 0.5 to 2 default x1 (old numbers
        /// x1.25~5 default 2.5)"*. The stored value keeps its old units, so a phone keeps its size; only the
        /// slider shows it divided by <see cref="BarScaleShown"/>.</summary>
        private const string PrefBarScale   = "ui.skillBarSize";   // new key: an old ×1 would clamp to 1.5, not start at 2.5
        private const float UiScaleMin = 0.6f, UiScaleMax = 1.6f;
        private const float BarScaleMin = 1.25f, BarScaleMax = 5f, BarScaleDefault = 2.5f;
        /// <summary>The stored bar scale the slider calls ×1.</summary>
        private const float BarScaleShown = 2.5f;

        /// <summary>`BL-364` item 9 — a DESKTOP starts smaller: the same 800-unit layout on a 1080-tall monitor is a phone
        /// layout blown up across a desk. 0.8 UI and a 0.8 bar until the player moves the sliders; the phone keeps 1.</summary>
        private static float UiScaleFirst => HoverTip.Wanted ? 0.8f : 1f;
        private static float BarScaleFirst => HoverTip.Wanted ? 2f : BarScaleDefault;
        /// <summary>What skill-bar ×1 is in the bar's own build units (SlotSize 78 etc.).</summary>
        private const float BarScaleBase = 1f / 3f;
        private float _uiScale = 1f;
        private float _barScale = BarScaleDefault;
        private const string PrefDamage     = "ui.damageNumbers";
        private const string PrefZones      = "ui.zoneOverlay";
        /// <summary>`BL-220` — whether the COMBAT TAB prints per-second tick lines (DoT, HoT, the
        /// mana-over-time tick). Owner 2026-09-12: *"Remove dot/hot from combat chat (or make it
        /// option for the client) it's to much flood and miss the dmg"*.</summary>
        private const string PrefTicks      = "ui.combatTickLines";

        private bool _showDamageNumbers = true;
        /// <summary>Default OFF, because he asked for them REMOVED and offered the toggle only as an
        /// alternative. A DoT on a 30-second timer writes thirty lines into the one tab he is reading
        /// to see what his stab hit for — the flood is the whole complaint, so the quiet state is the
        /// one a fresh install gets. The floating numbers over the target are NOT affected: this is a
        /// chat filter, and watching a poison tick on the mob is still how you know it landed.</summary>
        private static bool _showTickLines
        {
            get => GameBoot.ShowCombatTickLines;
            set => GameBoot.ShowCombatTickLines = value;
        }

        /// <summary>Load saved look settings BEFORE anything is built, so the UI is created at the
        /// player's chosen scale rather than built at the default and resized a frame later.</summary>
        private void LoadLookPrefs()
        {
            _uiScale = Mathf.Clamp(PlayerPrefs.GetFloat(PrefUiScale, UiScaleFirst), UiScaleMin, UiScaleMax);
            UiKit.Reference = new Vector2(UiKit.Reference.x, UiKit.ReferenceHeightAtX1 / _uiScale);
            _barScale = Mathf.Clamp(PlayerPrefs.GetFloat(PrefBarScale, BarScaleFirst), BarScaleMin, BarScaleMax);

            // 0.273.4 (owner, 2026-11-06): entity size, nameplate height and zone colours are no longer settings — fixed at
            // 0.9 / 0.85 / off. The old keys are DELETED, not just ignored, so a phone that once moved them starts clean.
            foreach (var key in new[] { PrefEntity, PrefPlate, PrefZones }) PlayerPrefs.DeleteKey(key);
            _showDamageNumbers = PlayerPrefs.GetInt(PrefDamage, 1) == 1;
            _showTickLines = PlayerPrefs.GetInt(PrefTicks, 0) == 1;
            PropScatter.LoadQuality();   // so the Settings label is right before the ground exists
            DayNight.LoadPrefs();
            Sfx.LoadPrefs();

            // Zone colours: always off (0.259.0 made off the default; 0.273.4 took the toggle away).
            if (Boot != null && Boot.Zones != null) Boot.Zones.gameObject.SetActive(false);

            if (Boot != null && Boot.CameraRig != null)
            {
                var rig = Boot.CameraRig;
                rig.Pitch = PlayerPrefs.GetFloat(PrefPitch, rig.Pitch);
                rig.Yaw = PlayerPrefs.GetFloat(PrefYaw, rig.Yaw);
            }
        }

        private void BuildSettingsWindow()
        {
            // On the SETUP canvas, not the world root: it is reachable from the login screen too, and it
            // must never be scaled by the setting it controls.
            _settingsPanel = UiKit.PanelBox(_setupRoot, "Settings");
            UiKit.Place(_settingsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(640f, 690f));   // grown for the camera rows, the bar-shape row, the bar size, then shadows + night; -88 for the ortho rows (0.252.0); +40 view distance
            var frame = _settingsPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_settingsPanel, "Settings", () => CloseWindow(_settingsPanel));

            // THE ROWS SCROLL (owner, 0.262.0: *"setup start to become larger (make it scrolable..)"*): between the title
            // bar and the Save / Reset / Sound buttons, which stay put. Every row below is placed at an absolute y down
            // a content rect, whose height is set to the last row's once they are all built.
            var view = UiKit.Box(frame, "Viewport", new Color(0, 0, 0, 0.001f));   // near-invisible, but catches the drag
            var viewRect = UiKit.Stretch(UiKit.Rect(view.gameObject), 0f, chrome, 16f, SetupFooter);   // 16 = the scrollbar
            view.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewRect, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            scroll.viewport = viewRect;
            scroll.content = content;
            UiKit.VerticalScrollbar(frame, scroll, 640f - 2f - 14f - 4f, chrome + 4f, 4f, SetupFooter + 4f);
            Transform inner = content;

            float y = -14f;

            // 0.273.4 (owner, 2026-11-06) — the other windows open from HERE, as rows of the window, not from the footer:
            // Sound and Options here; Keys in the Desktop group below. Options shows in the world only (its switches are
            // the character's, kept on the server), so the login screen's Settings leaves that square empty.
            SettingsLink(inner, 18f, y, "Sound", () => OpenSoundWindow());
            _settingsOptionsLink = SettingsLink(inner, 292f, y, "Options", () => ToggleWindow(_optionsPanel));
            y -= 48f;
            // Camera angle / zoom / rotation, entity size, nameplate height and zone colours were removed the same day:
            // the camera is the drag, the pinch and the wheel; the other three are fixed (EntityScale 0.9, 0.85, off).

            // Both sizes apply LIVE. That is safe only because this window sits on its own fixed canvas:
            // the game UI rescales behind the slider while the slider itself stays under your finger.
            Row(inner, ref y, _uiScaleSlider = UiKit.SliderRow(inner, "UI size (x)", UiScaleMin, UiScaleMax,
                _uiScale, "0.00", v =>
                {
                    ApplyUiScale(v);
                    PlayerPrefs.SetFloat(PrefUiScale, v);
                }));

            Row(inner, ref y, _barScaleSlider = UiKit.SliderRow(inner, "Skill bar size (x)", BarScaleMin / BarScaleShown,
                BarScaleMax / BarScaleShown, _barScale / BarScaleShown, "0.00", v =>
                {
                    _barScale = v * BarScaleShown;
                    ApplyBarLayout();
                    PlayerPrefs.SetFloat(PrefBarScale, _barScale);
                }));

            var damage = UiKit.TextButton(inner, "", () =>
            {
                _showDamageNumbers = !_showDamageNumbers;
                PlayerPrefs.SetInt(PrefDamage, _showDamageNumbers ? 1 : 0);
                RefreshSettingsLabels();
            }, 15f);
            UiKit.Place(UiKit.Rect(damage.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(260f, 38f));
            _damageToggle = damage;

            // `BL-220` — the tick lines. It sits beside "Damage numbers" on purpose: the two together
            // are the whole of "how loud is combat", and a player who turns one down usually wants the
            // other left alone (the floaters are readable, the chat flood is not).
            var ticks = UiKit.TextButton(inner, "", () =>
            {
                _showTickLines = !_showTickLines;
                PlayerPrefs.SetInt(PrefTicks, _showTickLines ? 1 : 0);
                RefreshSettingsLabels();
            }, 15f);
            UiKit.Place(UiKit.Rect(ticks.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(292f, y), new Vector2(260f, 38f));
            _tickToggle = ticks;
            y -= 48f;

            // `BL-93` — 3D models on/off. This is the low-end quality preset, not a debug switch: OFF
            // is the exact client that shipped before models existed (flat unlit spheres, one draw
            // call, no skinning), so a device that cannot carry rigged meshes still has a game. It
            // lives here rather than behind a Debug panel for that reason — the player whose phone is
            // struggling is the one who needs to find it.
            var models = UiKit.TextButton(inner, "", () =>
            {
                if (Boot.Entities == null) return;
                Boot.Entities.SetModelsEnabled(!EntityManager.ModelsEnabled);
                RefreshSettingsLabels();
            }, 15f);
            UiKit.Place(UiKit.Rect(models.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(260f, 38f));
            _modelToggle = models;
            // `BL-344` step 4 — trees, rocks, grass. A quality setting beside the models one, for the
            // same reason: Low thins them and shortens the draw range, Off is the bare ground.
            var scenery = UiKit.TextButton(inner, "", () =>
            {
                PropScatter.SetQuality((PropScatter.Level)(((int)PropScatter.Quality + 3) % 4));
                RefreshSettingsLabels();
            }, 15f);
            UiKit.Place(UiKit.Rect(scenery.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(292f, y), new Vector2(260f, 38f));
            _sceneryToggle = scenery;
            y -= 48f;

            // `BL-344` step 5 — shadows: off / characters / characters + trees. Characters are one cheap shadow map;
            // trees double the scenery's vertex work, so they are the top step and not the default.
            var shadows = UiKit.TextButton(inner, "", () =>
            {
                DayNight.SetShadows((DayNight.Shadows)(((int)DayNight.ShadowQuality + 1) % 3));
                RefreshSettingsLabels();
            }, 15f);
            UiKit.Place(UiKit.Rect(shadows.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(260f, 38f));
            _shadowToggle = shadows;
            // 0.262.0 — the old top status strip, now a movable popup of its own (see BuildStatusStrip).
            var gameInfo = UiKit.TextButton(inner, "", () => { ToggleGameInfo(); RefreshSettingsLabels(); }, 15f);
            UiKit.Place(UiKit.Rect(gameInfo.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(292f, y), new Vector2(260f, 38f));
            _gameInfoToggle = gameInfo;
            y -= 48f;

            // `BL-344` step 5 — the owner asked for "a bar or gamma as other games": how bright night is, per player.
            Row(inner, ref y, UiKit.SliderRow(inner, "Night brightness", DayNight.NightMin, DayNight.NightMax,
                DayNight.NightLevel, "0.00", v =>
                {
                    DayNight.NightLevel = v;
                    PlayerPrefs.SetFloat(DayNight.NightPref, v);
                }));

            // Owner, 2026-10-14: how far you see. Lower = less to draw on a weak phone.
            // 0.264.0 — split in two (owner: *"entites to have their own slider 1500~3000 and world(trees/houses)
            // 1500~5000"*). The fog belongs to the WORLD one: it hides where the scenery stops.
            // 0.266.0 — typed boxes for his distance test; 0.270.0 (`BL-358`) — his ranges, as a CYCLE button each: one tap
            // moves to the next step and wraps, and the button reads the step. World 3k/6k/10k/15k/20k, Entities
            // 1.5k/2.75k/4k/5k. Entities never reach past the world (*"no point enemy to load at 5k if your world is at
            // 3k"*): the server is sent DayNight.EntityReach, and the button says when the world is holding it back.
            var world = UiKit.TextButton(inner, "", () =>
            {
                var steps = GameConstants.WorldDistanceSteps;
                DayNight.ViewDistance = steps[(DayNight.StepIndex(steps, DayNight.ViewDistance) + 1) % steps.Length];
                PlayerPrefs.SetFloat(DayNight.ViewPref, DayNight.ViewDistance);
                Boot.SendViewDistance();   // entities never reach past it, so the server's range moves with it
                RefreshSettingsLabels();
            }, 15f);
            UiKit.Place(UiKit.Rect(world.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(260f, 38f));
            _worldDistToggle = world;
            var entities = UiKit.TextButton(inner, "", () =>
            {
                var steps = GameConstants.EntityDistanceSteps;
                DayNight.EntityDistance = steps[(DayNight.StepIndex(steps, DayNight.EntityDistance) + 1) % steps.Length];
                PlayerPrefs.SetFloat(DayNight.EntityPref, DayNight.EntityDistance);
                Boot.SendViewDistance();   // the server sends creatures out to it
                RefreshSettingsLabels();
            }, 15f);
            UiKit.Place(UiKit.Rect(entities.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(292f, y), new Vector2(260f, 38f));
            _entityDistToggle = entities;
            y -= 48f;

            // `BL-269` — extra skill squares, 0/6/12/18/24: *"need option to add more 6/12/18/24 skill slots
            // (half of or full the 2nd and 3rd skill bars)"*. A LOOK setting like the rest of this column,
            // so it lives on the phone; what the server stores is the same 60-slot bar either way.
            // `BL-299` — and the SHAPE beside it: 2x5 / 1x10 / 5x2. `BL-321`: the extras are now 0/1/2 whole bars in that shape.
            var barShape = UiKit.TextButton(inner, "", () => { CycleBarShape(); RefreshSettingsLabels(); }, 15f);
            UiKit.Place(UiKit.Rect(barShape.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(260f, 38f));
            _barShapeToggle = barShape;
            var extraSlots = UiKit.TextButton(inner, "", () => { CycleExtraSlots(); RefreshSettingsLabels(); }, 15f);
            UiKit.Place(UiKit.Rect(extraSlots.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(292f, y), new Vector2(260f, 38f));
            _extraSlotsToggle = extraSlots;
            y -= 48f;

            // `BL-364` items 5-6 — render scale and the frame-rate cap, on every platform (the phone too).
            Row(inner, ref y, UiKit.SliderRow(inner, "Render scale (x)", DisplaySettings.RenderScaleMin,
                DisplaySettings.RenderScaleMax, DisplaySettings.RenderScale, "0.00", v =>
                {
                    DisplaySettings.SetRenderScale(v);
                    PlayerPrefs.SetFloat(DisplaySettings.RenderScalePref, v);
                }));
            _fpsToggle = SettingsButton(inner, 18f, y, () =>
            {
                var steps = DisplaySettings.FpsSteps;
                int next = steps[(System.Array.IndexOf(steps, DisplaySettings.FpsCap) + 1) % steps.Length];
                DisplaySettings.SetFpsCap(next);
                PlayerPrefs.SetInt(DisplaySettings.FpsPref, next);
            });
            if (DisplaySettings.IsWindows)
                _vsyncToggle = SettingsButton(inner, 292f, y, () =>
                {
                    DisplaySettings.SetVSync(!DisplaySettings.VSync);
                    PlayerPrefs.SetInt(DisplaySettings.VSyncPref, DisplaySettings.VSync ? 1 : 0);
                });
            y -= 48f;

            // `BL-364` — the DESKTOP section: shown only where there is a mouse and keyboard (Windows, a desktop browser).
            if (HoverTip.Wanted)
            {
                var head = UiKit.Label(inner, "Desktop", 16f, UiKit.Accent, TextAlignmentOptions.BottomLeft);
                UiKit.Place(UiKit.Rect(head.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(18f, y), new Vector2(400f, 30f));
                y -= 36f;
                SettingsLink(inner, 18f, y, "Keys", () => OpenKeysWindow());
                // Item 7. The state flips a frame later (a browser asks the page first), so the label follows in
                // RefreshDisplayLabels, which watches the screen rather than trusting the click.
                _fullscreenToggle = SettingsButton(inner, 292f, y, DisplaySettings.ToggleFullscreen);
                y -= 48f;
                if (DisplaySettings.IsWindows)
                {
                    _resolutionToggle = SettingsButton(inner, 18f, y, DisplaySettings.NextResolution);
                    y -= 48f;
                }

                // Item 8 — the mouse.
                Row(inner, ref y, UiKit.SliderRow(inner, "Mouse turn speed (x)", DisplaySettings.MouseScaleMin,
                    DisplaySettings.MouseScaleMax, DisplaySettings.TurnSpeed, "0.00", v =>
                    {
                        DisplaySettings.TurnSpeed = v;
                        PlayerPrefs.SetFloat(DisplaySettings.TurnPref, v);
                    }));
                Row(inner, ref y, UiKit.SliderRow(inner, "Wheel zoom speed (x)", DisplaySettings.MouseScaleMin,
                    DisplaySettings.MouseScaleMax, DisplaySettings.WheelSpeed, "0.00", v =>
                    {
                        DisplaySettings.WheelSpeed = v;
                        PlayerPrefs.SetFloat(DisplaySettings.WheelPref, v);
                    }));
                _invertToggle = SettingsButton(inner, 18f, y, () =>
                {
                    DisplaySettings.InvertY = !DisplaySettings.InvertY;
                    PlayerPrefs.SetInt(DisplaySettings.InvertPref, DisplaySettings.InvertY ? 1 : 0);
                });
                y -= 48f;
            }
            content.sizeDelta = new Vector2(0f, -y);   // the scroll's whole height: down to the last row

            inner = frame;   // the footer buttons stay below the scrolling rows
            var save = UiKit.TextButton(inner, "Save", () => PlayerPrefs.Save(), 16f);
            UiKit.Place(UiKit.Rect(save.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(18f, 16f), new Vector2(120f, 40f));

            var reset = UiKit.TextButton(inner, "Reset to defaults", () =>
            {
                foreach (var key in new[] { PrefPitch, PrefYaw,
                                            PrefUiScale, PrefBarScale, PrefDamage,
                                            PrefTicks, PrefBarShape, PrefExtraBars, PropScatter.Pref, DayNight.ShadowPref, DayNight.NightPref,
                                            DayNight.ViewPref, DayNight.EntityPref, PrefGameInfo, PrefGameInfoX, PrefGameInfoY }
                                            .Concat(DisplaySettings.AllPrefs))
                    PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                // The two sizes come back at once — they are the ones that can make the game unusable.
                // The rest (camera, shape...) still waits for a restart, as before.
                // (setting the slider runs its handler, so the label, the live apply and the pref follow)
                _uiScaleSlider.value = UiScaleFirst;
                _barScaleSlider.value = BarScaleFirst / BarScaleShown;
                ClientLog.Info("Look settings reset — UI and skill bar size now, the rest after a restart.");
            }, 16f);
            UiKit.Place(UiKit.Rect(reset.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(150f, 16f), new Vector2(200f, 40f));

            BuildServerRow();
            RefreshSettingsLabels();
            _settingsPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// The server address (owner, 0.259.0: out of the sign-in panel, into Setup — but only the Setup opened from
        /// the login screen; in the game it is fixed and has no business on screen). The window itself is full, so
        /// it is a strip of its own sitting just above it: a child of the window, so it opens, closes and scales
        /// with it, and is switched on by the login screen's Setup button only. The field is the same
        /// <c>_urlField</c> the Login button reads.
        /// </summary>
        private void BuildServerRow()
        {
            _serverRow = UiKit.PanelBox(_settingsPanel, "ServerRow");
            UiKit.Place(_serverRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(640f, 58f));
            var inner = _serverRow.GetChild(0);
            UiKit.Place(UiKit.Rect(UiKit.Label(inner, "Server", 18f).gameObject), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(18f, 0f), new Vector2(90f, 40f));
            _urlField = UiKit.InputField(inner, "https://vardelar.duckdns.org/game");
            UiKit.Place(UiKit.Rect(_urlField.gameObject), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(110f, 0f), new Vector2(510f, 42f));
            _serverRow.gameObject.SetActive(false);
        }

        private RectTransform _serverRow;
        private Button _settingsOptionsLink;   // shown only while in the world (OpenSettings)
        private Button _damageToggle, _modelToggle, _tickToggle, _sceneryToggle, _shadowToggle, _gameInfoToggle;
        /// <summary>Room under Setup's scrolling rows for its Save / Reset buttons (y 16, 40 tall).</summary>
        private const float SetupFooter = 66f;
        private Button _extraSlotsToggle, _barShapeToggle;   // `BL-269`, `BL-299`
        private Button _worldDistToggle, _entityDistToggle;   // `BL-358`
        private Slider _uiScaleSlider, _barScaleSlider;

        /// <summary>Open (or close) Settings. <paramref name="fromLogin"/>: the login screen's copy shows the server
        /// address and hides Options, which needs a character in the world.</summary>
        private void OpenSettings(bool fromLogin)
        {
            _serverRow.gameObject.SetActive(fromLogin);
            _settingsOptionsLink.gameObject.SetActive(!fromLogin);
            ToggleWindow(_settingsPanel);
        }

        /// <summary>A 260-wide button in the Settings rows that opens another window.</summary>
        private Button SettingsLink(Transform parent, float x, float y, string label, System.Action onClick)
        {
            var b = UiKit.TextButton(parent, label, () => onClick(), 15f);
            UiKit.Place(UiKit.Rect(b.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(x, y), new Vector2(260f, 38f));
            return b;
        }

        /// <summary>Resize the GAME canvas now. Every panel is anchored, so changing the scaler's design
        /// height is the whole job — bar the overflow menu, whose column count depends on how tall the
        /// screen is in UI units.</summary>
        private void ApplyUiScale(float scale)
        {
            _uiScale = Mathf.Clamp(scale, UiScaleMin, UiScaleMax);
            UiKit.Reference = new Vector2(UiKit.Reference.x, UiKit.ReferenceHeightAtX1 / _uiScale);
            if (_canvas != null) _canvas.GetComponent<CanvasScaler>().referenceResolution = UiKit.Reference;
            if (_menuPanel != null) LayoutMenuPanel(force: true);
        }

        private void RefreshSettingsLabels()
        {

            UiKit.SetButtonText(_damageToggle, _showDamageNumbers ? "Damage numbers: ON" : "Damage numbers: off");
            UiKit.SetButtonText(_tickToggle, _showTickLines ? "DoT/HoT in chat: ON" : "DoT/HoT in chat: off");
            UiKit.SetButtonText(_modelToggle,
                EntityManager.ModelsEnabled ? "3D models: ON" : "3D models: off (faster)");
            UiKit.SetButtonText(_extraSlotsToggle, ExtraSlotsLabel);
            UiKit.SetButtonText(_sceneryToggle, "Scenery: " + PropScatter.Quality);
            UiKit.SetButtonText(_shadowToggle, DayNight.ShadowLabel);
            UiKit.SetButtonText(_gameInfoToggle, _infoPanel != null && _infoPanel.gameObject.activeSelf ? "Game info: ON" : "Game info: off");
            UiKit.SetButtonText(_barShapeToggle, BarShapeLabel);
            UiKit.SetButtonText(_worldDistToggle, "World: " + DistanceLabel(GameConstants.WorldDistanceSteps,
                DayNight.ViewStepNames, DayNight.ViewDistance));
            string ent = "Entities: " + DistanceLabel(GameConstants.EntityDistanceSteps, DayNight.EntityStepNames,
                DayNight.EntityDistance);
            if (DayNight.EntityReach < DayNight.EntityDistance) ent += " (world " + Km(DayNight.EntityReach) + ")";
            UiKit.SetButtonText(_entityDistToggle, ent);
            RefreshDisplayLabels();
        }

        /// <summary>0.270.0 — a distance step as the button reads it: "Normal 10k".</summary>
        private static string DistanceLabel(float[] steps, string[] names, float v) =>
            names[DayNight.StepIndex(steps, v)] + " " + Km(v);

        /// <summary>3000 → "3k", 2750 → "2.75k".</summary>
        private static string Km(float v) => (v / 1000f).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "k";

        // ----- `BL-364` display + mouse rows ----------------------------------------------------

        private Button _fpsToggle, _vsyncToggle, _fullscreenToggle, _resolutionToggle, _invertToggle;
        private Vector3Int _displaySeen = new Vector3Int(-1, -1, -1);   // width, height, fullscreen the labels were drawn for

        /// <summary>A 260-wide toggle/cycle button at (x, y) whose click is followed by a label refresh.</summary>
        private Button SettingsButton(Transform parent, float x, float y, System.Action onClick)
        {
            var b = UiKit.TextButton(parent, "", () => { onClick(); RefreshSettingsLabels(); }, 15f);
            UiKit.Place(UiKit.Rect(b.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(x, y), new Vector2(260f, 38f));
            return b;
        }

        private void RefreshDisplayLabels()
        {
            _displaySeen = new Vector3Int(Screen.width, Screen.height, Screen.fullScreen ? 1 : 0);
            if (_fpsToggle != null)
                UiKit.SetButtonText(_fpsToggle, "Frame rate: " + DisplaySettings.FpsLabel
                                    + (DisplaySettings.VSync ? " (vsync)" : ""));
            if (_vsyncToggle != null) UiKit.SetButtonText(_vsyncToggle, DisplaySettings.VSync ? "VSync: ON" : "VSync: off");
            if (_fullscreenToggle != null)
                UiKit.SetButtonText(_fullscreenToggle, Screen.fullScreen ? "Fullscreen: ON" : "Fullscreen: off");
            if (_resolutionToggle != null) UiKit.SetButtonText(_resolutionToggle, "Size: " + DisplaySettings.ResolutionLabel);
            if (_invertToggle != null) UiKit.SetButtonText(_invertToggle, DisplaySettings.InvertY ? "Invert mouse Y: ON" : "Invert mouse Y: off");
        }

        /// <summary>Fullscreen and the window size change a frame (or a browser prompt) after the click, and Esc / the
        /// window's own buttons change them without one — so an open Settings redraws them when the screen changes.</summary>
        private void WatchDisplayLabels()
        {
            if (_settingsPanel == null || !_settingsPanel.gameObject.activeSelf) return;
            if (_displaySeen != new Vector3Int(Screen.width, Screen.height, Screen.fullScreen ? 1 : 0)) RefreshDisplayLabels();
        }

        private static void Row(Transform parent, ref float y, Slider slider)
        {
            UiKit.Place(UiKit.Rect(slider.transform.parent.gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(580f, 26f));
            y -= 40f;
        }

        // ----- OPTIONS (playtest-19 M2 / B11) --------------------------------------------------
        //
        // A SEPARATE window from Setup on purpose: everything above is LOOK and lives in PlayerPrefs,
        // while these are RULES — they travel to the server, persist on the character, and decide what
        // other players can do to you. Mixing them would put "camera angle" and "refuse all trades" in
        // one list.
        //
        // It shares this FILE only because a brand-new .cs is invisible to Unity's generated csproj
        // until the Editor regenerates it, which would take this window out of the headless type-check.

        private RectTransform _optionsPanel;
        private Button _optAllChat, _optWhispers, _optGlobal, _optTrades, _optParty;

        private void BuildOptionsWindow()
        {
            // On the setup canvas since 0.273.4: it opens from Settings, which sits above the game canvas.
            _optionsPanel = UiKit.PanelBox(_setupRoot, "Options");
            UiKit.Place(_optionsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(560f, 400f));
            var inner = _optionsPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_optionsPanel, "Options", () => CloseWindow(_optionsPanel));

            float y = -chrome - 10f;

            var note = UiKit.Label(inner,
                "What you refuse to receive. Staff can always reach you.", 14f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(note.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(500f, 22f));
            y -= 34f;

            // Each button sends the SAME command as its slash twin, so there is exactly one code path
            // per toggle and the window can never drift from `/block-w`.
            _optAllChat  = OptionRow(inner, ref y, "all",      "All player chat");
            _optWhispers = OptionRow(inner, ref y, "whispers", "Whispers");
            _optGlobal   = OptionRow(inner, ref y, "global",   "World chat");
            _optTrades   = OptionRow(inner, ref y, "trades",   "Trade requests");
            _optParty    = OptionRow(inner, ref y, "party",    "Party invitations");

            var list = UiKit.TextButton(inner, "Block list", () => Boot.BlockCommand("list", ""), 15f);
            UiKit.Place(UiKit.Rect(list.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(18f, 16f), new Vector2(200f, 40f));

            RefreshOptionsWindow();
            _optionsPanel.gameObject.SetActive(false);
        }

        private Button OptionRow(Transform inner, ref float y, string action, string label)
        {
            var button = UiKit.TextButton(inner, label, () => Boot.BlockCommand(action, ""), 15f);
            UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(500f, 40f));
            y -= 50f;
            return button;
        }

        /// <summary>Redraw the switches from <see cref="GameBoot.Social"/> — i.e. from the server's
        /// last word, never from what the player just tapped. Called on every push.</summary>
        public void RefreshOptionsWindow()
        {
            if (_optAllChat == null || Boot == null) return;
            var s = Boot.Social;
            Switch(_optAllChat,  "All player chat",    (s & SocialOptions.BlockAllChat) != 0);
            Switch(_optWhispers, "Whispers",           (s & SocialOptions.BlockWhispers) != 0);
            Switch(_optGlobal,   "World chat",         (s & SocialOptions.BlockGlobal) != 0);
            Switch(_optTrades,   "Trade requests",     (s & SocialOptions.DeclineTrades) != 0);
            Switch(_optParty,    "Party invitations",  (s & SocialOptions.DeclineParty) != 0);
        }

        /// <summary>"&lt;label&gt;: BLOCKED / allowed". Words, not a tick glyph — the TMP atlas is baked
        /// with ~250 characters and anything outside it draws as a hollow box.</summary>
        private static void Switch(Button button, string label, bool blocked) =>
            UiKit.SetButtonText(button, label + (blocked ? ":  BLOCKED" : ":  allowed"));
    }
}
