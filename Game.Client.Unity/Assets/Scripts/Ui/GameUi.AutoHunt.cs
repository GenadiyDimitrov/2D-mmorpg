using System;
using Game.Shared;
using TMPro;
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
        // skill chains (playtest-15 design #1)
        private Button _autoCyclicToggle, _autoAssistToggle, _autoHealToggle;
        private Slider _autoHealSlider;
        private bool _autoCyclic, _autoAssist, _autoHealOn;

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

            // BOTH switches, not just the toggle (32r): the ring describes where the autopilot will
            // hunt, so with auto-farm off it is drawing a rule that nothing is enforcing. The toggle
            // stays a remembered preference — turn farming on and the ring is simply there again.
            var self = (_showFarmRange && Boot.AutoHunting
                        && Boot.Phase == ClientPhase.InWorld && Boot.Entities != null)
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
                        Vector2.zero, new Vector2(760f, 520f));
            var inner = _autoPotionsPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_autoPotionsPanel, "Auto Potions",
                                              () => CloseWindow(_autoPotionsPanel));

            // Two tabs, same window: heal potions answer "how do I stay alive", buff potions and
            // scrolls answer "what do I keep up". Both are things you set once and forget, both are
            // spent by the same per-tick loop, and neither needs auto-farm to be on.
            _autoPotTabButtons = new Button[2];
            string[] tabNames = { "Potions", "Buffs" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int tab = i;
                var button = UiKit.TextButton(inner, tabNames[i], () => { _autoPotTab = tab; ShowAutoPotTab(); }, 17f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(18f, -chrome - 6f), new Vector2(144f, 38f));
                UiKit.Rect(button.gameObject).anchoredPosition = new Vector2(18f + i * 150f, -chrome - 6f);
                _autoPotTabButtons[i] = button;
            }

            float top = chrome + 50f;
            _potionsTabRoot = UiKit.Rect(UiKit.Box(inner, "PotionsTab", new Color(0f, 0f, 0f, 0f), false).gameObject);
            UiKit.Stretch(_potionsTabRoot, 0f, top, 0f, 76f);
            _buffsTabRoot = UiKit.Rect(UiKit.Box(inner, "BuffsTab", new Color(0f, 0f, 0f, 0f), false).gameObject);
            UiKit.Stretch(_buffsTabRoot, 0f, top, 0f, 76f);

            BuildPotionsTab();
            BuildBuffsTab();

            BottomButtons(inner, ResetAutoPotions, SaveAutoPotions, () => CloseWindow(_autoPotionsPanel));

            _autoPotionsPanel.gameObject.SetActive(false);
        }

        private void BuildPotionsTab()
        {
            float y = -12f;
            UiKit.Place(UiKit.Rect(UiKit.Label(_potionsTabRoot, "Heal potions — drink when HP drops below the %:", 13f, UiKit.Accent).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, y), new Vector2(620f, 20f));
            y -= 26f;

            // One row per tier: an on/off toggle + a threshold slider whose title is the tier name. Armed
            // from the highest threshold down, so common@80 / uncommon@70 / rare@50 fall back for one
            // another (the server drinks the first ready one whose line is crossed).
            for (int i = 0; i < HealPotRows.Length; i++)
            {
                int idx = i;
                _potToggles[i] = ToggleButton(_potionsTabRoot, new Vector2(18f, y),
                    () => { _potOn[idx] = !_potOn[idx]; RefreshAutoLabels(); }, 84f);
                _potSliders[i] = UiKit.SliderRow(_potionsTabRoot, HealPotRows[i].Name, 5f, 95f, DefaultPotThreshold[i], "0", null);
                UiKit.Place(UiKit.Rect(_potSliders[i].transform.parent.gameObject),
                            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, y - 6f), new Vector2(620f, 26f));
                y -= 46f;
            }

            y -= 6f;
            UiKit.Place(UiKit.Rect(UiKit.Label(_potionsTabRoot, "Mana potion (MP potions arrive with the 3rd-class kits):", 13f, UiKit.TextDim).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, y), new Vector2(620f, 20f));
            y -= 26f;
            _autoMpToggle = ToggleButton(_potionsTabRoot, new Vector2(18f, y), () => { _autoMpOn = !_autoMpOn; RefreshAutoLabels(); }, 84f);
            _autoMpSlider = UiKit.SliderRow(_potionsTabRoot, "MP", 5f, 95f, 40f, "0", null);
            UiKit.Place(UiKit.Rect(_autoMpSlider.transform.parent.gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, y - 6f), new Vector2(620f, 26f));
        }

        // ----- the BUFFS tab (BL-04) ---------------------------------------------------------------

        private RectTransform _potionsTabRoot, _buffsTabRoot;
        private Button[] _autoPotTabButtons;
        private int _autoPotTab;

        private string[] _buffFamilies = Array.Empty<string>();
        private bool[] _buffPotOn = Array.Empty<bool>();
        private bool[] _buffScrOn = Array.Empty<bool>();
        private ItemRarity[] _buffMax = Array.Empty<ItemRarity>();
        private Button[] _buffPotBtn, _buffScrBtn, _buffRarBtn;

        /// <summary>
        /// One row per buff FAMILY, exactly as he drew it: <c>Bulwark [potion x][scroll ][max rarity:
        /// rare]</c>. A family is the unit because a family is what can be UP — every rung of Bulwark,
        /// potion or scroll or a cleric's blessing, is the same buff under one key, so "keep Bulwark
        /// up" is one question. The old per-ITEM list could not ask it, and could not express the cap.
        ///
        /// <para>The rows are built ONCE: the family list comes from the catalog and cannot change while
        /// the game runs. Only the labels and colours refresh.</para>
        /// </summary>
        private void BuildBuffsTab()
        {
            UiKit.Place(UiKit.Rect(UiKit.Label(_buffsTabRoot,
                    "Keep these up automatically. At equal rarity a SCROLL is spent before a potion; "
                    + "the cap is the dearest one it may open.", 13f, UiKit.Accent).gameObject),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -8f), new Vector2(700f, 36f));

            ScrollRect scroll;
            var content = UiKit.ScrollArea(_buffsTabRoot, out scroll, 3f);
            UiKit.Stretch((RectTransform)scroll.transform, 14f, 46f, 14f, 4f);

            var families = BuffConsumables.Families;
            int n = families.Count;
            _buffFamilies = new string[n];
            _buffPotOn = new bool[n];
            _buffScrOn = new bool[n];
            _buffMax = new ItemRarity[n];
            _buffPotBtn = new Button[n];
            _buffScrBtn = new Button[n];
            _buffRarBtn = new Button[n];

            for (int i = 0; i < n; i++)
            {
                int idx = i;
                string family = families[i].Family;
                _buffFamilies[i] = family;

                var rarities = BuffConsumables.RaritiesOf(family);
                _buffMax[i] = rarities.Count > 0 ? rarities[rarities.Count - 1] : ItemRarity.Common;

                bool hasPot = BuffConsumables.HasForm(family, BuffForm.Potion);
                bool hasScr = BuffConsumables.HasForm(family, BuffForm.Scroll);

                var row = UiKit.Box(content, "BuffRow", UiKit.PanelLight);
                row.gameObject.AddComponent<LayoutElement>().minHeight = 46f;

                var name = UiKit.Label(row.transform, families[i].Name, 16f, UiKit.Text, TextAlignmentOptions.Left);
                UiKit.Stretch(UiKit.Rect(name.gameObject), 12f, 0f, 468f, 0f);

                // A family with no potion (the scroll-only eight) gets a DEAD button, not a missing one:
                // the column has to line up down the list, and an absent control reads as a bug.
                _buffPotBtn[i] = UiKit.TextButton(row.transform, "",
                    hasPot ? (Action)(() => { _buffPotOn[idx] = !_buffPotOn[idx]; RefreshBuffRows(); }) : null, 14f);
                _buffPotBtn[i].interactable = hasPot;
                UiKit.Place(UiKit.Rect(_buffPotBtn[i].gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                            new Vector2(-334f, 0f), new Vector2(118f, 36f));

                _buffScrBtn[i] = UiKit.TextButton(row.transform, "",
                    hasScr ? (Action)(() => { _buffScrOn[idx] = !_buffScrOn[idx]; RefreshBuffRows(); }) : null, 14f);
                _buffScrBtn[i].interactable = hasScr;
                UiKit.Place(UiKit.Rect(_buffScrBtn[i].gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                            new Vector2(-210f, 0f), new Vector2(118f, 36f));

                _buffRarBtn[i] = UiKit.TextButton(row.transform, "",
                    rarities.Count > 1 ? (Action)(() => CycleBuffRarity(idx)) : null, 14f);
                _buffRarBtn[i].interactable = rarities.Count > 1;
                UiKit.Place(UiKit.Rect(_buffRarBtn[i].gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                            new Vector2(-8f, 0f), new Vector2(196f, 36f));
            }

            RefreshBuffRows();
        }

        /// <summary>Step the cap to the next rarity this family actually sells, wrapping. Only the
        /// rarities that EXIST are offered — a cap of Epic on a family whose best is Rare would be a
        /// setting that cannot be told from any other.</summary>
        private void CycleBuffRarity(int idx)
        {
            var rarities = BuffConsumables.RaritiesOf(_buffFamilies[idx]);
            if (rarities.Count == 0) return;
            int at = rarities.IndexOf(_buffMax[idx]);
            _buffMax[idx] = rarities[(at + 1) % rarities.Count];
            RefreshBuffRows();
        }

        private void RefreshBuffRows()
        {
            if (_buffPotBtn == null) return;
            for (int i = 0; i < _buffPotBtn.Length; i++)
            {
                bool hasPot = _buffPotBtn[i].interactable;
                bool hasScr = _buffScrBtn[i].interactable;
                UiKit.SetButtonText(_buffPotBtn[i], hasPot ? (_buffPotOn[i] ? "Potion  ON" : "Potion  off") : "no potion");
                UiKit.SetButtonText(_buffScrBtn[i], hasScr ? (_buffScrOn[i] ? "Scroll  ON" : "Scroll  off") : "no scroll");
                UiKit.SetButtonText(_buffRarBtn[i], "max rarity:  " + _buffMax[i]);
                if (_buffPotBtn[i].targetGraphic != null)
                    _buffPotBtn[i].targetGraphic.color = hasPot && _buffPotOn[i] ? AutoOnCol : AutoOffCol;
                if (_buffScrBtn[i].targetGraphic != null)
                    _buffScrBtn[i].targetGraphic.color = hasScr && _buffScrOn[i] ? AutoOnCol : AutoOffCol;
            }
        }

        private void ShowAutoPotTab()
        {
            if (_potionsTabRoot != null) _potionsTabRoot.gameObject.SetActive(_autoPotTab == 0);
            if (_buffsTabRoot != null) _buffsTabRoot.gameObject.SetActive(_autoPotTab == 1);
            if (_autoPotTabButtons != null)
                for (int i = 0; i < _autoPotTabButtons.Length; i++)
                    _autoPotTabButtons[i].targetGraphic.color = i == _autoPotTab ? UiKit.TabActive : UiKit.PanelLight;
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

            // The buff rows fill from the saved lines; a family the config has never mentioned stays
            // off with its cap at the best rarity that family sells — the cap is there to be lowered
            // deliberately, so its default is "no cap".
            for (int i = 0; i < _buffFamilies.Length; i++)
            {
                var rarities = BuffConsumables.RaritiesOf(_buffFamilies[i]);
                var line = FindBuffLine(c.Buffs, _buffFamilies[i]);
                _buffPotOn[i] = line != null && line.Potion;
                _buffScrOn[i] = line != null && line.Scroll;
                _buffMax[i] = line != null && rarities.Contains(line.MaxRarity)
                    ? line.MaxRarity
                    : rarities.Count > 0 ? rarities[rarities.Count - 1] : ItemRarity.Common;
            }

            RefreshAutoLabels();
            RefreshBuffRows();
            ShowAutoPotTab();
            OpenWindow(_autoPotionsPanel);
        }

        private static AutoBuffDto FindBuffLine(AutoBuffDto[] lines, string family)
        {
            if (lines != null)
                foreach (var l in lines)
                    if (l.Family == family) return l;
            return null;
        }

        /// <summary>Save BOTH tabs — one window, one Save. They are two halves of one config record,
        /// and a Save that only wrote the tab you happened to be looking at would silently drop the
        /// other half of the same edit.</summary>
        private void SaveAutoPotions()
        {
            var lines = new AutoPotionDto[HealPotRows.Length];
            for (int i = 0; i < HealPotRows.Length; i++)
                lines[i] = new AutoPotionDto(HealPotRows[i].Id, _potOn[i], Mathf.RoundToInt(_potSliders[i].value));
            int mp = _autoMpOn ? Mathf.RoundToInt(_autoMpSlider.value) : 0;

            // EVERY family goes, armed or not. A non-empty array is what tells the server the tab is in
            // charge now (it replaces the old keep-every-buff-potion-up switch), so sending only the
            // armed ones would make "I turned them all off" indistinguishable from "I never opened it",
            // and the old behaviour would quietly come back.
            var buffs = new AutoBuffDto[_buffFamilies.Length];
            for (int i = 0; i < _buffFamilies.Length; i++)
                buffs[i] = new AutoBuffDto(_buffFamilies[i], _buffPotOn[i], _buffScrOn[i], _buffMax[i]);

            // HpPotionPct 0 → the server uses the per-potion HealPotions list, not the old single line.
            Boot.PushAutoConfig(Boot.AutoConfig with
            {
                HealPotions = lines, HpPotionPct = 0, MpPotionPct = mp, Buffs = buffs,
            });
            ClientLog.Info("Auto-potions saved.");
        }

        /// <summary>Reset the tab you are LOOKING at. Wiping the other one — which you cannot see, and
        /// may have just spent a minute setting up — is not what a Reset button means.</summary>
        private void ResetAutoPotions()
        {
            if (_autoPotTab == 1)
            {
                for (int i = 0; i < _buffFamilies.Length; i++)
                {
                    var rarities = BuffConsumables.RaritiesOf(_buffFamilies[i]);
                    _buffPotOn[i] = false;
                    _buffScrOn[i] = false;
                    _buffMax[i] = rarities.Count > 0 ? rarities[rarities.Count - 1] : ItemRarity.Common;
                }
                RefreshBuffRows();
                return;
            }

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
                        Vector2.zero, new Vector2(660f, 600f));
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

            // ----- skill chain (playtest-15 design #1) -----
            // Shares its row with the Show-range toggle at x=330, so this label stops short of it.
            // ASCII arrows/dots on purpose: no visible UI string in this client uses "→" or "…" yet, and
            // a glyph the TMP atlas doesn't carry renders as a box on the phone.
            var chain = UiKit.Label(inner, "Skill chain — heals, then buffs, then attacks:", 14f, UiKit.Accent);
            UiKit.Place(UiKit.Rect(chain.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, y), new Vector2(300f, 22f));
            y -= 34f;

            _autoCyclicToggle = ToggleButton(inner, new Vector2(18f, y), () =>
            {
                _autoCyclic = !_autoCyclic;
                RefreshAutoLabels();
            }, 300f);
            var cyclicNote = UiKit.Label(inner, "on = 1-2-3-4-1...  off = back to the top each time (1-2-1-3)",
                                         12f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(cyclicNote.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(330f, y - 8f), new Vector2(310f, 30f));
            y -= 48f;

            // The heal chain's OWN threshold — deliberately not the auto-potion one: a potion and a heal
            // are used at different moments (and a healer parks this at 100 to heal on cooldown).
            _autoHealToggle = ToggleButton(inner, new Vector2(18f, y), () => { _autoHealOn = !_autoHealOn; RefreshAutoLabels(); }, 84f);
            _autoHealSlider = UiKit.SliderRow(inner, "heal below HP%", 10f, 100f, 70f, "0", null);
            UiKit.Place(UiKit.Rect(_autoHealSlider.transform.parent.gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, y - 6f), new Vector2(526f, 26f));
            y -= 48f;

            _autoAssistToggle = ToggleButton(inner, new Vector2(18f, y), () => { _autoAssist = !_autoAssist; RefreshAutoLabels(); }, 300f);
            var assistNote = UiKit.Label(inner, "only attack the party leader's target; idle if he has none",
                                         12f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(assistNote.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(330f, y - 8f), new Vector2(310f, 30f));
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
            _autoCyclic = c.CyclicOrder;
            _autoAssist = c.AssistPartyLeader;
            _autoHealOn = c.HealThresholdPct > 0;             // 0 = the heal chain is off
            _autoHealSlider.value = _autoHealOn ? Mathf.Clamp(c.HealThresholdPct, 10, 100) : 70f;
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
                CyclicOrder  = _autoCyclic,
                HealThresholdPct = _autoHealOn ? Mathf.RoundToInt(_autoHealSlider.value) : 0,
                AssistPartyLeader = _autoAssist,
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
            _autoCyclic = false;
            _autoAssist = false;
            _autoHealOn = true;
            _autoHealSlider.value = 70f;
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
            SetToggle(_autoCyclicToggle, _autoCyclic, "Cyclic order");
            SetToggle(_autoAssistToggle, _autoAssist, "Assist party leader");
            SetToggleOnOff(_autoHealToggle, _autoHealOn);
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
