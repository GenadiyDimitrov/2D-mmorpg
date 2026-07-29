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
        // auto-potions — the Potions tab: the 4 heal tiers + a reserved MP row.
        private RectTransform _autoPotionsPanel;
        private static readonly (string Id, string Name)[] HealPotRows =
        {
            (ItemCatalog.MinorPotion,   "Common"),
            (ItemCatalog.HealingPotion, "Uncommon"),
            (ItemCatalog.GreaterPotion, "Rare"),
            (ItemCatalog.InstantPotion, "Instant"),
        };
        private static readonly int[] DefaultPotThreshold = { 80, 70, 50, 30 };
        private readonly Button[] _potToggles = new Button[4];
        private readonly Slider[] _potSliders = new Slider[4];
        private readonly bool[] _potOn = new bool[4];
        private Button _autoMpToggle;
        private Slider _autoMpSlider;
        private bool _autoMpOn;

        // auto-farm
        private RectTransform _autoFarmPanel;
        private Button _autoStaticToggle, _autoNormalToggle, _autoEliteToggle, _autoBossToggle;
        private Button _farmShowRangeToggle;
        private Slider _autoRangeSlider;
        private bool _autoStatic, _autoNormal, _autoElite, _autoBoss;

        // farming-range ring drawn in the world (a border-only circle showing the farm radius)
        private LineRenderer _farmRing;
        private bool _showFarmRange;
        private const string PrefFarmRing = "autofarm.showRange";

        private static readonly Color AutoOnCol  = new Color(0.20f, 0.42f, 0.24f, 0.95f);
        private static readonly Color AutoOffCol = new Color(0.42f, 0.20f, 0.20f, 0.95f);

        private void BuildAutoHuntWindows()
        {
            BuildAutoPotionsWindow();
            BuildAutoFarmWindow();
            BuildFarmRing();
        }

        // ----- farming-range ring ----------------------------------------------------------------

        private void BuildFarmRing()
        {
            _showFarmRange = PlayerPrefs.GetInt(PrefFarmRing, 0) == 1;

            var go = new GameObject("FarmRangeRing");
            _farmRing = go.AddComponent<LineRenderer>();
            _farmRing.useWorldSpace = true;
            _farmRing.loop = true;
            _farmRing.widthMultiplier = 0.35f;
            // The IL2CPP-safe shader (a raw Shader.Find would render magenta on the phone).
            _farmRing.material = new Material(UnlitMaterials.Shader);
            _farmRing.startColor = _farmRing.endColor = new Color(1f, 0.9f, 0.2f, 0.9f);
            _farmRing.positionCount = 0;
            go.SetActive(false);
        }

        /// <summary>Draw the farm-radius ring around the player while the toggle is on. Called each frame
        /// from RefreshWorld. Centred on the STATIC anchor when "keep position" is on, otherwise on the
        /// player (roaming really does follow the character).</summary>
        private void RefreshFarmRing()
        {
            if (_farmRing == null) return;

            var self = (_showFarmRange && Boot.Phase == ClientPhase.InWorld && Boot.Entities != null)
                ? Boot.Entities.Find(Boot.SelfId) : null;
            if (self == null)
            {
                if (_farmRing.gameObject.activeSelf) _farmRing.gameObject.SetActive(false);
                return;
            }

            if (!_farmRing.gameObject.activeSelf) _farmRing.gameObject.SetActive(true);
            float radius = Boot.AutoConfig.FarmRange * WorldMapper.Scale;

            // "Keep position" means the circle is a PLACE, not a leash drawn around you: the server
            // holds you to FarmCenter, so the ring belongs there. Drawing it around the character made
            // the one setting whose whole point is standing still look like it was following you
            // (playtest-13). Roaming mode has no anchor — the scan really does follow the character.
            Vector3 c = Boot.AutoConfig != null && Boot.AutoConfig.StaticSpot
                ? WorldMapper.ToUnity(Boot.FarmCenter.x, Boot.FarmCenter.y)
                : self.transform.position;
            c.y = 0.05f;

            const int segs = 64;
            if (_farmRing.positionCount != segs) _farmRing.positionCount = segs;
            for (int i = 0; i < segs; i++)
            {
                float a = i / (float)segs * Mathf.PI * 2f;
                _farmRing.SetPosition(i, c + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        // ----- auto-potions ----------------------------------------------------------------------

        private void BuildAutoPotionsWindow()
        {
            _autoPotionsPanel = UiKit.PanelBox(_worldRoot, "AutoPotions");
            UiKit.Place(_autoPotionsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(680f, 430f));
            var inner = _autoPotionsPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_autoPotionsPanel, "Auto Potions",
                                              () => CloseWindow(_autoPotionsPanel));

            float y = -chrome - 12f;
            UiKit.Place(UiKit.Rect(UiKit.Label(inner, "Heal potions — drink when HP drops below the %:", 13f, UiKit.Accent).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, y), new Vector2(620f, 20f));
            y -= 26f;

            // One row per tier: an on/off toggle + a threshold slider whose title is the tier name. Armed
            // from the highest threshold down, so common@80 / uncommon@70 / rare@50 fall back for one
            // another (the server drinks the first ready one whose line is crossed).
            for (int i = 0; i < HealPotRows.Length; i++)
            {
                int idx = i;
                _potToggles[i] = ToggleButton(inner, new Vector2(18f, y),
                    () => { _potOn[idx] = !_potOn[idx]; RefreshAutoLabels(); }, 84f);
                _potSliders[i] = UiKit.SliderRow(inner, HealPotRows[i].Name, 5f, 95f, DefaultPotThreshold[i], "0", null);
                UiKit.Place(UiKit.Rect(_potSliders[i].transform.parent.gameObject),
                            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, y - 6f), new Vector2(556f, 26f));
                y -= 46f;
            }

            y -= 6f;
            UiKit.Place(UiKit.Rect(UiKit.Label(inner, "Mana potion (MP potions arrive with the 3rd-class kits):", 13f, UiKit.TextDim).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, y), new Vector2(620f, 20f));
            y -= 26f;
            _autoMpToggle = ToggleButton(inner, new Vector2(18f, y), () => { _autoMpOn = !_autoMpOn; RefreshAutoLabels(); }, 84f);
            _autoMpSlider = UiKit.SliderRow(inner, "MP", 5f, 95f, 40f, "0", null);
            UiKit.Place(UiKit.Rect(_autoMpSlider.transform.parent.gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, y - 6f), new Vector2(556f, 26f));

            BottomButtons(inner, ResetAutoPotions, SaveAutoPotions, () => CloseWindow(_autoPotionsPanel));

            _autoPotionsPanel.gameObject.SetActive(false);
        }

        /// <summary>Find the saved line for a potion id in the config (null if none/unset).</summary>
        private static AutoPotionDto FindPotLine(AutoPotionDto[] lines, string id)
        {
            if (lines != null)
                foreach (var l in lines)
                    if (l.ItemId == id) return l;
            return null;
        }

        private void OpenAutoPotions()
        {
            var c = Boot.AutoConfig;
            for (int i = 0; i < HealPotRows.Length; i++)
            {
                var line = FindPotLine(c.HealPotions, HealPotRows[i].Id);
                _potOn[i] = line != null && line.Enabled;
                _potSliders[i].value = line != null && line.ThresholdPct > 0
                    ? Mathf.Clamp(line.ThresholdPct, 5, 95) : DefaultPotThreshold[i];
            }
            _autoMpOn = c.MpPotionPct > 0;
            _autoMpSlider.value = _autoMpOn ? Mathf.Clamp(c.MpPotionPct, 5, 95) : 40f;
            RefreshAutoLabels();
            OpenWindow(_autoPotionsPanel);
        }

        private void SaveAutoPotions()
        {
            var lines = new AutoPotionDto[HealPotRows.Length];
            for (int i = 0; i < HealPotRows.Length; i++)
                lines[i] = new AutoPotionDto(HealPotRows[i].Id, _potOn[i], Mathf.RoundToInt(_potSliders[i].value));
            int mp = _autoMpOn ? Mathf.RoundToInt(_autoMpSlider.value) : 0;
            // HpPotionPct 0 → the server uses the per-potion HealPotions list, not the old single line.
            Boot.PushAutoConfig(Boot.AutoConfig with { HealPotions = lines, HpPotionPct = 0, MpPotionPct = mp });
            ClientLog.Info("Auto-potions saved.");
        }

        private void ResetAutoPotions()
        {
            for (int i = 0; i < HealPotRows.Length; i++)
            {
                _potOn[i] = i == 0;   // Common armed by default
                _potSliders[i].value = DefaultPotThreshold[i];
            }
            _autoMpOn = false;
            _autoMpSlider.value = 40f;
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

            // Show/hide the farm-range ring in the world (client-only, persisted).
            _farmShowRangeToggle = ToggleButton(inner, new Vector2(330f, y), () =>
            {
                _showFarmRange = !_showFarmRange;
                PlayerPrefs.SetInt(PrefFarmRing, _showFarmRange ? 1 : 0);
                RefreshAutoLabels();
            }, 290f);

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
            for (int i = 0; i < _potToggles.Length; i++) SetToggleOnOff(_potToggles[i], _potOn[i]);
            SetToggleOnOff(_autoMpToggle, _autoMpOn);
            SetToggle(_autoStaticToggle, _autoStatic, "Keep position");
            SetToggle(_autoNormalToggle, _autoNormal, "Normal mobs");
            SetToggle(_autoEliteToggle,  _autoElite,  "Elite mobs");
            SetToggle(_autoBossToggle,   _autoBoss,   "Boss mobs");
            SetToggle(_farmShowRangeToggle, _showFarmRange, "Show range");
        }

        private static void SetToggle(Button button, bool on, string name)
        {
            if (button == null) return;
            UiKit.SetButtonText(button, name + ":  " + (on ? "ON" : "off"));
            if (button.targetGraphic != null) button.targetGraphic.color = on ? AutoOnCol : AutoOffCol;
        }

        /// <summary>A name-less on/off toggle (the name is carried by the adjacent slider row).</summary>
        private static void SetToggleOnOff(Button button, bool on)
        {
            if (button == null) return;
            UiKit.SetButtonText(button, on ? "ON" : "off");
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
