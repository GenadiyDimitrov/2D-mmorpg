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

        private const string PrefPitch      = "cam.pitch";
        private const string PrefYaw        = "cam.yaw";
        private const string PrefOrtho      = "cam.ortho";
        private const string PrefOrthoSize  = "cam.orthoSize";
        private const string PrefEntity     = "ui.entityScale";
        private const string PrefPlate      = "ui.nameplateHeight";
        private const string PrefUiScale    = "ui.referenceHeight";
        private const string PrefDamage     = "ui.damageNumbers";
        private const string PrefZones      = "ui.zoneOverlay";

        private bool _showDamageNumbers = true;

        /// <summary>Load saved look settings BEFORE anything is built, so the UI is created at the
        /// player's chosen scale rather than built at the default and resized a frame later.</summary>
        private void LoadLookPrefs()
        {
            UiKit.Reference = new Vector2(UiKit.Reference.x,
                PlayerPrefs.GetFloat(PrefUiScale, UiKit.Reference.y));

            EntityManager.EntityScale = PlayerPrefs.GetFloat(PrefEntity, EntityManager.EntityScale);
            NameplateHeight = PlayerPrefs.GetFloat(PrefPlate, NameplateHeight);
            _showDamageNumbers = PlayerPrefs.GetInt(PrefDamage, 1) == 1;

            // Applied here rather than only written on tap: the preference was being saved and then
            // ignored on the next launch, so "zone colours off" never survived a restart.
            if (Boot != null && Boot.Zones != null)
                Boot.Zones.gameObject.SetActive(PlayerPrefs.GetInt(PrefZones, 1) == 1);

            if (Boot != null && Boot.CameraRig != null)
            {
                var rig = Boot.CameraRig;
                rig.Pitch = PlayerPrefs.GetFloat(PrefPitch, rig.Pitch);
                rig.Yaw = PlayerPrefs.GetFloat(PrefYaw, rig.Yaw);
                rig.Orthographic = PlayerPrefs.GetInt(PrefOrtho, 0) == 1;
                rig.OrthoSize = PlayerPrefs.GetFloat(PrefOrthoSize, rig.OrthoSize);
            }
        }

        private void BuildSettingsWindow()
        {
            _settingsPanel = UiKit.PanelBox(_worldRoot, "Settings");
            UiKit.Place(_settingsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(640f, 560f));   // grown for the camera rows
            var inner = _settingsPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_settingsPanel, "Settings", () => CloseWindow(_settingsPanel));

            float y = -chrome - 14f;

            Row(inner, ref y, UiKit.SliderRow(inner, "Camera angle", 45f, 90f,
                Boot.CameraRig != null ? Boot.CameraRig.Pitch : 90f, "0", v =>
                {
                    if (Boot.CameraRig != null) Boot.CameraRig.Pitch = v;
                    PlayerPrefs.SetFloat(PrefPitch, v);
                }));

            Row(inner, ref y, UiKit.SliderRow(inner, "Camera height", 10f, 90f,
                Boot.CameraRig != null ? Boot.CameraRig.Distance : 38f, "0", v =>
                {
                    if (Boot.CameraRig != null) Boot.CameraRig.Distance = v;
                    // The rig owns its own persistence for distance (pinch writes it too).
                }));

            // Yaw exists on the rig and was hardcoded to 0. As a SLIDER rather than a twist gesture:
            // a two-finger twist is hard to tell from pinch, and until the world has occluders there
            // is nothing to rotate around — this is enough to judge whether it is worth a gesture.
            Row(inner, ref y, UiKit.SliderRow(inner, "Camera rotation", 0f, 360f,
                Boot.CameraRig != null ? Boot.CameraRig.Yaw : 0f, "0", v =>
                {
                    if (Boot.CameraRig != null) Boot.CameraRig.Yaw = v;
                    PlayerPrefs.SetFloat(PrefYaw, v);
                }));

            Row(inner, ref y, UiKit.SliderRow(inner, "Ortho zoom", 6f, 60f,
                Boot.CameraRig != null ? Boot.CameraRig.OrthoSize : 22f, "0", v =>
                {
                    if (Boot.CameraRig != null) Boot.CameraRig.OrthoSize = v;
                    PlayerPrefs.SetFloat(PrefOrthoSize, v);
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

            // UI scale needs a REBUILD, not a live update: the canvas scaler reads the reference
            // resolution when it lays out, and every panel here was positioned against it. Saying so
            // is better than silently doing half of it.
            Row(inner, ref y, UiKit.SliderRow(inner, "UI size (restart)", 480f, 1100f,
                UiKit.Reference.y, "0", v => PlayerPrefs.SetFloat(PrefUiScale, v)));

            var projection = UiKit.TextButton(inner, "", () =>
            {
                if (Boot.CameraRig == null) return;
                Boot.CameraRig.Orthographic = !Boot.CameraRig.Orthographic;
                PlayerPrefs.SetInt(PrefOrtho, Boot.CameraRig.Orthographic ? 1 : 0);
                RefreshSettingsLabels();
            }, 15f);
            UiKit.Place(UiKit.Rect(projection.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(260f, 38f));
            _projectionToggle = projection;
            y -= 48f;

            var damage = UiKit.TextButton(inner, "", () =>
            {
                _showDamageNumbers = !_showDamageNumbers;
                PlayerPrefs.SetInt(PrefDamage, _showDamageNumbers ? 1 : 0);
                RefreshSettingsLabels();
            }, 15f);
            UiKit.Place(UiKit.Rect(damage.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(260f, 38f));
            _damageToggle = damage;

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

            var save = UiKit.TextButton(inner, "Save", () => PlayerPrefs.Save(), 16f);
            UiKit.Place(UiKit.Rect(save.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(18f, 16f), new Vector2(140f, 40f));

            var reset = UiKit.TextButton(inner, "Reset to defaults", () =>
            {
                foreach (var key in new[] { PrefPitch, PrefYaw, PrefOrtho, PrefOrthoSize,
                                            PrefEntity, PrefPlate, PrefUiScale, PrefDamage, PrefZones })
                    PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                ClientLog.Info("Look settings reset — restart the app to apply.");
            }, 16f);
            UiKit.Place(UiKit.Rect(reset.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(170f, 16f), new Vector2(220f, 40f));

            RefreshSettingsLabels();
            _settingsPanel.gameObject.SetActive(false);
        }

        private Button _damageToggle, _zoneToggle, _projectionToggle;

        private void RefreshSettingsLabels()
        {
            bool ortho = Boot.CameraRig != null && Boot.CameraRig.Orthographic;
            UiKit.SetButtonText(_projectionToggle, ortho ? "View: ORTHO (even sight)" : "View: perspective");

            UiKit.SetButtonText(_damageToggle, _showDamageNumbers ? "Damage numbers: ON" : "Damage numbers: off");
            bool zonesOn = Boot.Zones != null && Boot.Zones.gameObject.activeSelf;
            UiKit.SetButtonText(_zoneToggle, zonesOn ? "Zone colours: ON" : "Zone colours: off");
        }

        private static void Row(Transform parent, ref float y, Slider slider)
        {
            UiKit.Place(UiKit.Rect(slider.transform.parent.gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(580f, 26f));
            y -= 40f;
        }
    }
}
