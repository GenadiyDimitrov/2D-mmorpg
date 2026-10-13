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
            _uiScale = Mathf.Clamp(PlayerPrefs.GetFloat(PrefUiScale, 1f), UiScaleMin, UiScaleMax);
            UiKit.Reference = new Vector2(UiKit.Reference.x, UiKit.ReferenceHeightAtX1 / _uiScale);
            _barScale = Mathf.Clamp(PlayerPrefs.GetFloat(PrefBarScale, BarScaleDefault), BarScaleMin, BarScaleMax);

            EntityManager.EntityScale = PlayerPrefs.GetFloat(PrefEntity, EntityManager.EntityScale);
            NameplateHeight = PlayerPrefs.GetFloat(PrefPlate, NameplateHeight);
            _showDamageNumbers = PlayerPrefs.GetInt(PrefDamage, 1) == 1;
            _showTickLines = PlayerPrefs.GetInt(PrefTicks, 0) == 1;
            PropScatter.LoadQuality();   // so the Settings label is right before the ground exists
            DayNight.LoadPrefs();
            Sfx.LoadPrefs();

            // Applied here rather than only written on tap: the preference was being saved and then
            // ignored on the next launch, so "zone colours off" never survived a restart.
            if (Boot != null && Boot.Zones != null)
                Boot.Zones.gameObject.SetActive(PlayerPrefs.GetInt(PrefZones, 1) == 1);

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
                        Vector2.zero, new Vector2(640f, 690f));   // +40 for the sound volume (BL-45); grown for the camera rows, the bar-shape row, the bar size, then shadows + night; -88 for the ortho rows (0.252.0)
            var inner = _settingsPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_settingsPanel, "Settings", () => CloseWindow(_settingsPanel));

            float y = -chrome - 14f;

            Row(inner, ref y, _pitchSlider = UiKit.SliderRow(inner, "Camera angle", CameraRig.MinPitch, CameraRig.MaxPitch,
                Boot.CameraRig != null ? Boot.CameraRig.Pitch : 90f, "0", v =>
                {
                    if (Boot.CameraRig != null) Boot.CameraRig.Pitch = v;
                    PlayerPrefs.SetFloat(PrefPitch, v);
                }));

            // The slider holds the LOGARITHMIC position (0..1), the label the distance: equal slides are equal-looking
            // zoom steps, the same as a pinch (CameraRig.ZoomToSlider).
            Row(inner, ref y, _zoomSlider = UiKit.SliderRow(inner, "Camera zoom", 0f, 1f,
                CameraRig.ZoomToSlider(Boot.CameraRig != null ? Boot.CameraRig.Distance : 20f), "0", v =>
                {
                    if (Boot.CameraRig != null) Boot.CameraRig.SetZoom(CameraRig.SliderToZoom(v));
                }, v => CameraRig.SliderToZoom(v).ToString(CameraRig.SliderToZoom(v) < 1f ? "0.0" : "0")));

            // Yaw: this slider, or (`BL-344` step 8) one finger dragged sideways across the world.
            Row(inner, ref y, _yawSlider = UiKit.SliderRow(inner, "Camera rotation", 0f, 360f,
                Boot.CameraRig != null ? Boot.CameraRig.Yaw : 0f, "0", v =>
                {
                    if (Boot.CameraRig != null) Boot.CameraRig.Yaw = v;
                    PlayerPrefs.SetFloat(PrefYaw, v);
                }));

            Row(inner, ref y, UiKit.SliderRow(inner, "Entity size", 0.3f, 3f,
                EntityManager.EntityScale, "0.00", v =>
                {
                    EntityManager.EntityScale = v;
                    if (Boot.Entities != null) Boot.Entities.ApplyEntityScale();
                    PlayerPrefs.SetFloat(PrefEntity, v);
                }));

            Row(inner, ref y, UiKit.SliderRow(inner, "Nameplate height", 0f, 3f,
                NameplateHeight, "0.00", v =>
                {
                    NameplateHeight = v;
                    PlayerPrefs.SetFloat(PrefPlate, v);
                }));

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
                        new Vector2(292f, y - 48f), new Vector2(260f, 38f));
            _tickToggle = ticks;

            // Boot.Zones is a HELD reference. This used to call FindAnyObjectByType<ZoneOverlay>(),
            // which does not return INACTIVE objects — so the first tap hid the overlay and every tap
            // after it found nothing to un-hide. The colours could be turned off exactly once, for the
            // lifetime of the app.
            var zones = UiKit.TextButton(inner, "", () =>
            {
                var overlay = Boot.Zones;
                if (overlay == null) return;
                overlay.gameObject.SetActive(!overlay.gameObject.activeSelf);
                PlayerPrefs.SetInt(PrefZones, overlay.gameObject.activeSelf ? 1 : 0);
                RefreshSettingsLabels();
            }, 15f);
            UiKit.Place(UiKit.Rect(zones.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(292f, y), new Vector2(260f, 38f));
            _zoneToggle = zones;
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
            y -= 48f;

            // `BL-344` step 5 — the owner asked for "a bar or gamma as other games": how bright night is, per player.
            Row(inner, ref y, UiKit.SliderRow(inner, "Night brightness", DayNight.NightMin, DayNight.NightMax,
                DayNight.NightLevel, "0.00", v =>
                {
                    DayNight.NightLevel = v;
                    PlayerPrefs.SetFloat(DayNight.NightPref, v);
                }));

            // `BL-45` — one volume for every sound effect; 0 = silent.
            Row(inner, ref y, UiKit.SliderRow(inner, "Sound volume", 0f, 1f, Sfx.Volume, "0.00", v =>
                {
                    Sfx.Volume = v;
                    PlayerPrefs.SetFloat(Sfx.VolumePref, v);
                }));

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

            var save = UiKit.TextButton(inner, "Save", () => PlayerPrefs.Save(), 16f);
            UiKit.Place(UiKit.Rect(save.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(18f, 16f), new Vector2(140f, 40f));

            var reset = UiKit.TextButton(inner, "Reset to defaults", () =>
            {
                foreach (var key in new[] { PrefPitch, PrefYaw,
                                            PrefEntity, PrefPlate, PrefUiScale, PrefBarScale, PrefDamage,
                                            PrefZones, PrefTicks, PrefBarShape, PrefExtraBars, PropScatter.Pref, DayNight.ShadowPref, DayNight.NightPref, Sfx.VolumePref })
                    PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                // The two sizes come back at once — they are the ones that can make the game unusable.
                // The rest (camera, shape...) still waits for a restart, as before.
                // (setting the slider runs its handler, so the label, the live apply and the pref follow)
                _uiScaleSlider.value = 1f;
                _barScaleSlider.value = BarScaleDefault / BarScaleShown;
                ClientLog.Info("Look settings reset — UI and skill bar size now, the rest after a restart.");
            }, 16f);
            UiKit.Place(UiKit.Rect(reset.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(170f, 16f), new Vector2(220f, 40f));

            RefreshSettingsLabels();
            _settingsPanel.gameObject.SetActive(false);
        }

        private Button _damageToggle, _zoneToggle, _modelToggle, _tickToggle, _sceneryToggle, _shadowToggle;
        private Button _extraSlotsToggle, _barShapeToggle;   // `BL-269`, `BL-299`
        private Slider _uiScaleSlider, _barScaleSlider;
        private Slider _pitchSlider, _yawSlider, _zoomSlider;   // follow the drag gesture while Settings is open (SyncCameraSliders)

        /// <summary>`BL-344` step 8: the drag and the compass move the camera behind the sliders' back, so
        /// an open Settings window pulls the numbers back off the rig. Setting <c>value</c> runs the row's
        /// handler, which only writes the same numbers back — harmless.</summary>
        private void SyncCameraSliders()
        {
            var rig = Boot != null ? Boot.CameraRig : null;
            if (rig == null || _pitchSlider == null || !_settingsPanel.gameObject.activeSelf) return;
            if (Mathf.Abs(_pitchSlider.value - rig.Pitch) > 0.5f) _pitchSlider.value = rig.Pitch;
            if (Mathf.Abs(Mathf.DeltaAngle(_yawSlider.value, rig.Yaw)) > 0.5f) _yawSlider.value = rig.Yaw;
            float zoomAt = CameraRig.ZoomToSlider(rig.Distance);   // a pinch moves it too
            if (Mathf.Abs(_zoomSlider.value - zoomAt) > 0.005f) _zoomSlider.value = zoomAt;
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
            bool zonesOn = Boot.Zones != null && Boot.Zones.gameObject.activeSelf;
            UiKit.SetButtonText(_zoneToggle, zonesOn ? "Zone colours: ON" : "Zone colours: off");
            UiKit.SetButtonText(_modelToggle,
                EntityManager.ModelsEnabled ? "3D models: ON" : "3D models: off (faster)");
            UiKit.SetButtonText(_extraSlotsToggle, ExtraSlotsLabel);
            UiKit.SetButtonText(_sceneryToggle, "Scenery: " + PropScatter.Quality);
            UiKit.SetButtonText(_shadowToggle, DayNight.ShadowLabel);
            UiKit.SetButtonText(_barShapeToggle, BarShapeLabel);
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
            _optionsPanel = UiKit.PanelBox(_worldRoot, "Options");
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
