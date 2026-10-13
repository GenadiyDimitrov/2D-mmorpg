using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the Sound window (`BL-45`) — Mute all, then Master / Music / Effects.
    ///
    /// A window of its own rather than rows in Settings, because Settings already fills the 800-unit screen height at
    /// UI size 1. Opened from Menu → Sound, and from a button in Settings (which the login screen reaches too). Like
    /// Settings it is LOOK: PlayerPrefs on the phone, nothing sent to the server. The values live on <see cref="Sfx"/>,
    /// which both the effects and <see cref="Music"/> read every frame, so a slider is heard as it moves.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _soundPanel;
        private Button _muteToggle;

        private void BuildSoundWindow()
        {
            // On the setup canvas, beside Settings, so the login screen can reach it.
            _soundPanel = UiKit.PanelBox(_setupRoot, "Sound");
            UiKit.Place(_soundPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(640f, 300f));
            var inner = _soundPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_soundPanel, "Sound", () => CloseWindow(_soundPanel));
            float y = -chrome - 14f;

            _muteToggle = UiKit.TextButton(inner, "", () =>
            {
                Sfx.Muted = !Sfx.Muted;
                PlayerPrefs.SetInt(Sfx.MutePref, Sfx.Muted ? 1 : 0);
                RefreshSoundLabels();
            }, 16f);
            UiKit.Place(UiKit.Rect(_muteToggle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(260f, 38f));
            y -= 52f;

            Row(inner, ref y, UiKit.SliderRow(inner, "Master", 0f, 1f, Sfx.Master, "0.00", v =>
            {
                Sfx.Master = v;
                PlayerPrefs.SetFloat(Sfx.MasterPref, v);
            }));
            Row(inner, ref y, UiKit.SliderRow(inner, "Music", 0f, 1f, Sfx.MusicVolume, "0.00", v =>
            {
                Sfx.MusicVolume = v;
                PlayerPrefs.SetFloat(Sfx.MusicPref, v);
            }));
            Row(inner, ref y, UiKit.SliderRow(inner, "Effects", 0f, 1f, Sfx.Effects, "0.00", v =>
            {
                Sfx.Effects = v;
                PlayerPrefs.SetFloat(Sfx.EffectsPref, v);
            }));

            var save = UiKit.TextButton(inner, "Save", () => PlayerPrefs.Save(), 16f);
            UiKit.Place(UiKit.Rect(save.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(18f, 16f), new Vector2(140f, 40f));

            RefreshSoundLabels();
            _soundPanel.gameObject.SetActive(false);
        }

        /// <summary>A checkbox drawn in text: the TMP atlas is static, so a ☑ glyph may not exist in it.</summary>
        private void RefreshSoundLabels() =>
            UiKit.SetButtonText(_muteToggle, Sfx.Muted ? "[X]  Mute all" : "[  ]  Mute all");

        private void OpenSoundWindow() => ToggleWindow(_soundPanel);
    }
}
