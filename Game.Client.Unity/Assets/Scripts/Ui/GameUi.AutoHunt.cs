using System;
using Game.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the two SETUP windows for idle farming.
    ///
    /// These are deliberately split into two panels rather than one WPF-style config window, on the
    /// owner's call: auto-potions are useful even with auto-farm off (survival while you fight by
    /// hand), so they live apart and can grow apart. Both open from the Menu — they are setup, not
    /// something you press mid-fight — while the on/off itself stays on the top-right Auto button.
    ///
    /// Neither window owns the WHOLE config: each edits only its own fields and pushes
    /// <see cref="GameBoot.AutoConfig"/> with those changed (a record `with`), so saving potions can
    /// never wipe the farm settings and vice-versa. The server is authoritative and echoes the clamped
    /// result back, which is what the windows fill from the next time they open.
    ///
    /// Per-skill selection is NOT here: a skill is marked for auto-use by long-pressing its slot on the
    /// skill bar (Auto: on/off), which is the phone's answer to WPF's priority list. Priority is the
    /// bar order; per-skill reuse and the cyclic/priority choice are a later pass.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        // auto-potions
        private RectTransform _autoPotionsPanel;
        private Button _autoHpToggle, _autoMpToggle;
        private Slider _autoHpSlider, _autoMpSlider;
        private bool _autoHpOn, _autoMpOn;

        // auto-farm
        private RectTransform _autoFarmPanel;
        private Button _autoStaticToggle, _autoNormalToggle, _autoEliteToggle, _autoBossToggle;
        private Slider _autoRangeSlider;
        private bool _autoStatic, _autoNormal, _autoElite, _autoBoss;

        private static readonly Color AutoOnCol  = new Color(0.20f, 0.42f, 0.24f, 0.95f);
        private static readonly Color AutoOffCol = new Color(0.42f, 0.20f, 0.20f, 0.95f);

        private void BuildAutoHuntWindows()
        {
            BuildAutoPotionsWindow();
            BuildAutoFarmWindow();
        }

        // ----- auto-potions ----------------------------------------------------------------------

        private void BuildAutoPotionsWindow()
        {
            _autoPotionsPanel = UiKit.PanelBox(_worldRoot, "AutoPotions");
            UiKit.Place(_autoPotionsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(640f, 360f));
            var inner = _autoPotionsPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_autoPotionsPanel, "Auto Potions",
                                              () => CloseWindow(_autoPotionsPanel));

            float y = -chrome - 16f;

            _autoHpToggle = ToggleButton(inner, new Vector2(18f, y), () =>
            {
                _autoHpOn = !_autoHpOn;
                RefreshAutoLabels();
            });
            y -= 46f;
            _autoHpSlider = UiKit.SliderRow(inner, "drink HP below", 5f, 95f, 60f, "0", null);
            PlaceRow(inner, _autoHpSlider, y);
            y -= 52f;

            _autoMpToggle = ToggleButton(inner, new Vector2(18f, y), () =>
            {
                _autoMpOn = !_autoMpOn;
                RefreshAutoLabels();
            });
            y -= 46f;
            _autoMpSlider = UiKit.SliderRow(inner, "drink MP below", 5f, 95f, 40f, "0", null);
            PlaceRow(inner, _autoMpSlider, y);
            y -= 52f;

            var note = UiKit.Label(inner,
                "Drinks the best matching potion in your bag when a bar drops below its %.", 13f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(note.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(600f, 34f));

            BottomButtons(inner, ResetAutoPotions, SaveAutoPotions, () => CloseWindow(_autoPotionsPanel));

            _autoPotionsPanel.gameObject.SetActive(false);
        }

        private void OpenAutoPotions()
        {
            var c = Boot.AutoConfig;
            _autoHpOn = c.HpPotionPct > 0;
            _autoMpOn = c.MpPotionPct > 0;
            _autoHpSlider.value = _autoHpOn ? Mathf.Clamp(c.HpPotionPct, 5, 95) : 60f;
            _autoMpSlider.value = _autoMpOn ? Mathf.Clamp(c.MpPotionPct, 5, 95) : 40f;
            RefreshAutoLabels();
            OpenWindow(_autoPotionsPanel);
        }

        private void SaveAutoPotions()
        {
            int hp = _autoHpOn ? Mathf.RoundToInt(_autoHpSlider.value) : 0;
            int mp = _autoMpOn ? Mathf.RoundToInt(_autoMpSlider.value) : 0;
            Boot.PushAutoConfig(Boot.AutoConfig with { HpPotionPct = hp, MpPotionPct = mp });
            ClientLog.Info("Auto-potions saved.");
        }

        private void ResetAutoPotions()
        {
            _autoHpOn = true;  _autoHpSlider.value = 60f;
            _autoMpOn = false; _autoMpSlider.value = 40f;
            RefreshAutoLabels();
        }

        // ----- auto-farm -------------------------------------------------------------------------

        private void BuildAutoFarmWindow()
        {
            _autoFarmPanel = UiKit.PanelBox(_worldRoot, "AutoFarm");
            UiKit.Place(_autoFarmPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(640f, 420f));
            var inner = _autoFarmPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_autoFarmPanel, "Auto Farm",
                                              () => CloseWindow(_autoFarmPanel));

            float y = -chrome - 16f;

            _autoRangeSlider = UiKit.SliderRow(inner, "search range", 200f, 2000f, 1000f, "0", null);
            PlaceRow(inner, _autoRangeSlider, y);
            y -= 52f;

            _autoStaticToggle = ToggleButton(inner, new Vector2(18f, y), () =>
            {
                _autoStatic = !_autoStatic;
                RefreshAutoLabels();
            }, 300f);
            var staticNote = UiKit.Label(inner, "on = a fixed circle where you started; off = it follows you",
                                         12f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(staticNote.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(330f, y - 8f), new Vector2(300f, 30f));
            y -= 54f;

            var engage = UiKit.Label(inner, "Engage which ranks:", 14f, UiKit.Accent);
            UiKit.Place(UiKit.Rect(engage.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(400f, 22f));
            y -= 30f;

            _autoNormalToggle = ToggleButton(inner, new Vector2(18f, y),  () => { _autoNormal = !_autoNormal; RefreshAutoLabels(); }, 300f);
            y -= 46f;
            _autoEliteToggle  = ToggleButton(inner, new Vector2(18f, y),  () => { _autoElite  = !_autoElite;  RefreshAutoLabels(); }, 300f);
            y -= 46f;
            _autoBossToggle   = ToggleButton(inner, new Vector2(18f, y),  () => { _autoBoss   = !_autoBoss;   RefreshAutoLabels(); }, 300f);

            BottomButtons(inner, ResetAutoFarm, SaveAutoFarm, () => CloseWindow(_autoFarmPanel));

            _autoFarmPanel.gameObject.SetActive(false);
        }

        private void OpenAutoFarm()
        {
            var c = Boot.AutoConfig;
            _autoRangeSlider.value = Mathf.Clamp(c.FarmRange, 200, 2000);
            _autoStatic = c.StaticSpot;
            _autoNormal = c.AttackNormal;
            _autoElite  = c.AttackElite;
            _autoBoss   = c.AttackBoss;
            RefreshAutoLabels();
            OpenWindow(_autoFarmPanel);
        }

        private void SaveAutoFarm()
        {
            Boot.PushAutoConfig(Boot.AutoConfig with
            {
                FarmRange    = Mathf.RoundToInt(_autoRangeSlider.value),
                StaticSpot   = _autoStatic,
                AttackNormal = _autoNormal,
                AttackElite  = _autoElite,
                AttackBoss   = _autoBoss,
            });
            ClientLog.Info("Auto-farm settings saved.");
        }

        private void ResetAutoFarm()
        {
            _autoRangeSlider.value = 1000f;
            _autoStatic = false;
            _autoNormal = true;
            _autoElite  = false;
            _autoBoss   = false;
            RefreshAutoLabels();
        }

        // ----- shared ----------------------------------------------------------------------------

        /// <summary>Repaint every on/off toggle in both windows from its backing bool. One method so a
        /// toggle, an open, or a reset all land the same green/red state.</summary>
        private void RefreshAutoLabels()
        {
            SetToggle(_autoHpToggle,     _autoHpOn,  "HP potion");
            SetToggle(_autoMpToggle,     _autoMpOn,  "MP potion");
            SetToggle(_autoStaticToggle, _autoStatic, "Keep position");
            SetToggle(_autoNormalToggle, _autoNormal, "Normal mobs");
            SetToggle(_autoEliteToggle,  _autoElite,  "Elite mobs");
            SetToggle(_autoBossToggle,   _autoBoss,   "Boss mobs");
        }

        private static void SetToggle(Button button, bool on, string name)
        {
            if (button == null) return;
            UiKit.SetButtonText(button, name + ":  " + (on ? "ON" : "off"));
            if (button.targetGraphic != null) button.targetGraphic.color = on ? AutoOnCol : AutoOffCol;
        }

        private Button ToggleButton(Transform parent, Vector2 offset, Action onClick, float width = 220f)
        {
            var button = UiKit.TextButton(parent, "", onClick, 15f);
            UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        offset, new Vector2(width, 38f));
            return button;
        }

        private static void PlaceRow(Transform parent, Slider slider, float y)
        {
            UiKit.Place(UiKit.Rect(slider.transform.parent.gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(580f, 26f));
        }

        /// <summary>The [Reset] [Save] [Close] strip every setup window carries, on the owner's spec.</summary>
        private void BottomButtons(Transform inner, Action reset, Action save, Action close)
        {
            var resetButton = UiKit.TextButton(inner, "Reset", () => reset(), 16f);
            UiKit.Place(UiKit.Rect(resetButton.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(18f, 16f), new Vector2(180f, 46f));

            var saveButton = UiKit.TextButton(inner, "Save", () => save(), 16f);
            UiKit.Place(UiKit.Rect(saveButton.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 16f), new Vector2(180f, 46f));

            var closeButton = UiKit.TextButton(inner, "Close", () => close(), 16f);
            UiKit.Place(UiKit.Rect(closeButton.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-18f, 16f), new Vector2(180f, 46f));
        }
    }
}
