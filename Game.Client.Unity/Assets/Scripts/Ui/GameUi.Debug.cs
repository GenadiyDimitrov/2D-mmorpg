using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// THE ADMIN WINDOW (the former debug window), at parity with the WPF harness (batch F).
    ///
    /// It was called "Debug" while its commands were compiled out with `#if DEBUG`. That made the RELEASE
    /// server published to the phone accept every one of them and do nothing — the window opened, the
    /// buttons worked, and pressing them was silence (owner, 2026-07-30). The commands ship in every build
    /// now and are authorised server-side on the account ROLE, so this is an admin toolbox, not a debug
    /// one, and the menu entry that opens it collapses away entirely for a normal player.
    ///
    /// The owner uses this constantly — it is how a build gets into the state worth testing without
    /// playing thirty levels to get there. It was explicitly ruled "FULL functionality wanted, do NOT
    /// trim it", so this mirrors WPF's five tabs rather than picking a convenient subset:
    ///
    ///   Equip       — the real tiered gear, drilled down by category then level, plus boxes.
    ///   Consumables — scrolls, potions, reagents.
    ///   Functions   — buffs, gold/SP, level, karma.
    ///   Class       — profession, subclass swap/add, character reset.
    ///   Teleport    — NPCs, spawn zones, cities.
    ///   Tuning      — the live server knobs (rates/karma/caps/regen), admin-only.
    ///
    /// Nothing here is a hardcoded list where a catalog exists: gear, towns, zones, NPCs and classes
    /// are all read out of Game.Shared, so content added to the CSVs appears here for free. That was
    /// a real bug in the old WPF menu — hand-listed items meant whole gear tiers looked missing.
    ///
    /// The one deliberate omission is karma +/-, which the owner asked to drop; karma CLEAR stays,
    /// because an accidental kill has to be undoable.
    /// </summary>
    public partial class GameUi
    {
        private RectTransform _debugContent;
        private TextMeshProUGUI _debugTitle;
        private readonly List<Button> _debugTabButtons = new();

        private int _debugTab;             // 0 equip, 1 consumables, 2 functions, 3 teleport, 4 class, 5 tuning
        private string _debugEquipCat = "";  // "" = root, else armor/weapon/jewel
        private int _debugEquipLevel;      // 0 = still choosing
        private int _debugTpView;          // 0 root, 1 npcs, 2 zones, 3 cities
        private bool _debugAddDiscView;

        private readonly Dictionary<string, TMP_InputField> _tuneFields = new();

        // key, label, isFloat — the order matches the DebugConfigDto round-trip.
        private static readonly (string Key, string Label, bool Float)[] TuneSpec =
        {
            ("exp", "Exp rate x", true),
            ("sp", "SP rate x", true),
            ("dropChance", "Drop chance x", true),
            ("dropAmount", "Drop amount x", true),
            ("gold", "Gold rate x", true),
            ("karmaBase", "Karma base", false),
            ("karmaConsec", "Karma x/consec kill", true),
            ("karmaLevel", "Karma x/level diff", true),
            ("karmaDeath", "Karma -/death", false),
            ("karmaMob", "Karma -/mob kill", false),
            ("idleCap", "Idle cap sec (0=inf)", false),
            ("offlineCap", "Offline cap sec (0=inf)", false),
            ("grace", "Disconnect grace (sec)", false),
            ("testHealPower", "Test heal power", false),
            ("testSkillPower", "Test skill Flat", false),
            ("testSkillMod", "Test skill Mod x", true),
            ("regenInterval", "Regen tick (sec)", true),
            ("conRegen", "CON regen x/point", true),
            ("mobRegen", "Mob regen in combat (frac/s)", true),
            ("mobRegenIdle", "Mob regen idle (frac/s)", true),
        };

        private void BuildDebugPanel()
        {
            _debugPanel = UiKit.PanelBox(_worldRoot, "Debug");
            UiKit.Place(_debugPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(720f, 560f));
            var inner = _debugPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_debugPanel, "Admin", () => CloseWindow(_debugPanel));

            // Tabs across the top. Short labels: five of them have to fit a phone in landscape.
            string[] tabs = { "Equip", "Items", "Func", "TP", "Class", "Tune" };
            const float tabW = 112f, tabGap = 4f;
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                var button = UiKit.TextButton(inner, tabs[i], () => ShowDebugTab(index), 15f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(14f + i * (tabW + tabGap), -chrome - 8f), new Vector2(tabW, 38f));
                _debugTabButtons.Add(button);
            }

            _debugTitle = UiKit.Label(inner, "", 14f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(_debugTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -chrome - 50f), new Vector2(660f, 24f));

            _debugContent = UiKit.ScrollArea(inner, out var scroll, 4f);
            UiKit.Stretch((RectTransform)scroll.transform, 14f, chrome + 78f, 14f, 14f);

            _debugPanel.gameObject.SetActive(false);
        }

        /// <summary>Switching tabs resets that tab's drill-down. Coming back to a tab three levels deep
        /// into a menu you left ten minutes ago is disorienting, and "Back" from there leads somewhere
        /// you never chose.</summary>
        private void ShowDebugTab(int tab)
        {
            _debugTab = tab;
            _debugEquipCat = "";
            _debugEquipLevel = 0;
            _debugTpView = 0;
            _debugAddDiscView = false;
            if (tab == 5) Boot.Debug(n => n.RequestDebugConfigAsync(), "tuning");
            RefreshDebugPanel();
        }

        /// <summary>Rebuild the visible list. Called on every navigation AND when the server pushes a
        /// new class list, so the subclass rows can never show a swap that already happened.</summary>
        public void RefreshDebugPanel()
        {
            if (_debugContent == null || _debugPanel == null || !_debugPanel.gameObject.activeSelf) return;

            for (int i = _debugContent.childCount - 1; i >= 0; i--)
                Destroy(_debugContent.GetChild(i).gameObject);
            _tuneFields.Clear();

            for (int i = 0; i < _debugTabButtons.Count; i++)
                UiKit.SetButtonText(_debugTabButtons[i], _debugTabButtons[i] != null
                    ? (i == _debugTab ? "[" + TabName(i) + "]" : TabName(i)) : "");

            switch (_debugTab)
            {
                case 1: BuildDebugConsumables(); break;
                case 2: BuildDebugFunctions(); break;
                case 3: BuildDebugTeleport(); break;
                case 4: BuildDebugClass(); break;
                case 5: BuildDebugTuning(); break;
                default: BuildDebugEquip(); break;
            }
        }

        private static string TabName(int i) => i switch
        {
            1 => "Items", 2 => "Func", 3 => "TP", 4 => "Class", 5 => "Tune", _ => "Equip"
        };

        // ---- row helpers ------------------------------------------------------------------------

        private void DebugHeader(string text)
        {
            var label = UiKit.Label(_debugContent, text, 14f, UiKit.Accent);
            var le = label.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 26f;
        }

        private Button DebugAction(string text, Action click)
        {
            var button = UiKit.TextButton(_debugContent, text, click, 15f);
            var le = button.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 38f;
            return button;
        }

        private void DebugGive(string defId, string label, int quantity = 1)
        {
            if (defId == null) return;
            DebugAction(label, () => Boot.Debug(n => n.DebugGiveAsync(defId, quantity), "give"));
        }

        private void DebugNote(string text)
        {
            var label = UiKit.Label(_debugContent, text, 13f, UiKit.Good);
            var le = label.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 24f;
        }

        // ---- Equip -------------------------------------------------------------------------------

        /// <summary>The gear LEVELS present in the catalog — discovered, never hardcoded, so new tiers
        /// appear here the day they are added to the CSV.</summary>
        private static IEnumerable<int> GearLevels() => ItemCatalog.AllItems
            .Where(d => d.ItemLevel > 0 && d.Rarity == ItemRarity.Epic)
            .Select(d => d.ItemLevel).Distinct().OrderBy(l => l);

        private static IEnumerable<ItemDef> GearAt(string cat, int level) => ItemCatalog.AllItems
            .Where(d => d.ItemLevel == level && d.Rarity == ItemRarity.Epic)
            .Where(d => cat switch
            {
                "armor" => d.Slot is EquipSlot.Armor or EquipSlot.Shield,
                "weapon" => d.Slot == EquipSlot.Weapon,
                "jewel" => d.Slot == EquipSlot.Jewel,
                _ => false
            })
            .OrderBy(d => d.Slot).ThenBy(d => d.Name, StringComparer.Ordinal);

        private void BuildDebugEquip()
        {
            if (_debugEquipCat == "")
            {
                _debugTitle.text = "Gear, boxes and legendary items";
                DebugHeader("Tiered gear (pick a level, then a piece)");
                DebugAction("Armor & Shields >", () => { _debugEquipCat = "armor"; _debugEquipLevel = 0; RefreshDebugPanel(); });
                DebugAction("Weapons >", () => { _debugEquipCat = "weapon"; _debugEquipLevel = 0; RefreshDebugPanel(); });
                DebugAction("Jewels >", () => { _debugEquipCat = "jewel"; _debugEquipLevel = 0; RefreshDebugPanel(); });

                DebugHeader("Boxes");
                DebugGive(ItemCatalog.BoxNewbie, "Newbie Box");
                DebugGive(ItemCatalog.BoxTreasure, "Treasure Chest");
                DebugGive(ItemCatalog.BoxNewbieArmorLight, "Newbie Light Armor Box");
                DebugGive(ItemCatalog.BoxNewbieArmorRobe, "Newbie Robe Armor Box");
                DebugGive(ItemCatalog.BoxNewbieJewels, "Newbie Jewels Box");
                DebugGive(ItemCatalog.BoxNewbieWeapons, "Newbie Weapons Box (select)");

                DebugHeader("Legendary");
                DebugGive(ItemCatalog.GodWeapon, "God's Judgment");
                DebugGive(ItemCatalog.GodArmor, "God's Robes");
                return;
            }

            string catLabel = _debugEquipCat switch
            {
                "armor" => "Armor & Shields", "weapon" => "Weapons", _ => "Jewels"
            };

            if (_debugEquipLevel == 0)
            {
                _debugTitle.text = catLabel + " — pick a level";
                DebugAction("< Back", () => { _debugEquipCat = ""; RefreshDebugPanel(); });
                foreach (int lv in GearLevels())
                {
                    int level = lv;
                    DebugAction($"Level {level}  ({ItemCatalog.TierLetter(level)}-Grade) >",
                                () => { _debugEquipLevel = level; RefreshDebugPanel(); });
                }
                return;
            }

            _debugTitle.text = $"{catLabel} — Level {_debugEquipLevel} " +
                               $"({ItemCatalog.TierLetter(_debugEquipLevel)}-Grade)";
            DebugAction("< Back", () => { _debugEquipLevel = 0; RefreshDebugPanel(); });

            // A set bonus needs all four pieces, so one click has to be able to produce a whole set —
            // otherwise testing a set bonus is four taps of hunting through a list.
            if (_debugEquipCat == "armor")
            {
                int lvl = _debugEquipLevel;
                foreach (var (key, label) in new[] { ("heavy", "Heavy"), ("light", "Light"), ("robe", "Robe") })
                {
                    string bodyId = $"{key}_t{lvl}";
                    if (ItemCatalog.Get(bodyId) is null) continue;
                    DebugAction($"* Full {label} Set (body + helm + gloves + boots)", () =>
                    {
                        foreach (var id in new[] { bodyId, $"helm_t{lvl}", $"gloves_t{lvl}", $"boots_t{lvl}" })
                            if (ItemCatalog.Get(id) is not null)
                            {
                                string give = id;
                                Boot.Debug(n => n.DebugGiveAsync(give, 1), "give");
                            }
                    });
                }
                DebugHeader("Individual pieces");
            }

            foreach (var def in GearAt(_debugEquipCat, _debugEquipLevel))
                DebugGive(def.Id, def.Name);
        }

        // ---- Consumables -------------------------------------------------------------------------

        private void BuildDebugConsumables()
        {
            _debugTitle.text = "Scrolls, potions and reagents";

            // Every one of the 18 (D2: "every scroll in the admin menu"). Generated from the same table
            // the catalog is built from, so a scroll can never be authored and left unreachable here.
            // One header per grade — 18 flat rows would be an unreadable wall on a phone.
            foreach (var (grade, _, _, level, _) in ItemCatalog.EnchantScrollBands)
            {
                DebugHeader($"Enchant scrolls — {EnchantRules.GradeName(grade)} grade (item level {level}+)");
                foreach (var (kind, prefix, _) in ItemCatalog.EnchantScrollTypes)
                {
                    string id = ItemCatalog.EnchantScrollKey(kind, grade);
                    DebugGive(id, prefix + " x10", 10);
                }
            }

            DebugHeader("Attribute scrolls (x10)");
            DebugGive(ItemCatalog.AttrScrollCommon, "Attr Scroll (Common) x10", 10);
            DebugGive(ItemCatalog.AttrScrollUncommon, "Attr Scroll (Uncommon) x10", 10);
            DebugGive(ItemCatalog.AttrScrollRare, "Attr Scroll (Rare) x10", 10);
            DebugGive(ItemCatalog.AttrScrollEpic, "Attr Scroll (Epic) x10", 10);
            DebugGive(ItemCatalog.AttrScrollLegendary, "Attr Scroll (Legendary) x10", 10);
            DebugGive(ItemCatalog.AttrScrollMythic, "Attr Scroll (Mythic) x10", 10);

            DebugHeader("Buff potions & scrolls (x5)");
            DebugGive(ItemCatalog.SpeedPotionC, "Swift Potion (Lesser) x5", 5);
            DebugGive(ItemCatalog.SpeedPotionU, "Swift Potion x5", 5);
            DebugGive(ItemCatalog.CastPotionU, "Alacrity Potion x5", 5);
            DebugGive(ItemCatalog.AtkPotionU, "Haste Potion x5", 5);
            DebugGive(ItemCatalog.EvaPotionU, "Agility Potion x5", 5);
            DebugGive(ItemCatalog.SpeedScrollR, "Scroll of Swift x5", 5);
            DebugGive(ItemCatalog.CastScrollR, "Scroll of Alacrity x5", 5);
            DebugGive(ItemCatalog.DashPotionM, "Dash Potion (Supreme) x5", 5);
            // The Blessing Box is the only source of a buff scroll in play, so it is the one thing here
            // that has to be reachable to test the pick-10 chooser at all.
            DebugGive(ItemCatalog.BoxBuffScrolls, "Blessing Box x2", 2);

            DebugHeader("Potions (x10)");
            DebugGive(ItemCatalog.MinorPotion, "Minor Potion x10", 10);
            DebugGive(ItemCatalog.HealingPotion, "Healing Potion x10", 10);
            DebugGive(ItemCatalog.GreaterPotion, "Greater Potion x10", 10);

            DebugHeader("Reagents");
            DebugGive(ItemCatalog.ElementalStone, "Elemental Stone x10", 10);
            DebugGive(ItemCatalog.SkillStone, "Skill Stone x10", 10);

            // The ULTIMATE scrolls are deliberately not vendor-stocked, so debug is the only way to get
            // hold of them — and therefore the only way to test them.
            DebugHeader("Utility scrolls (x5)");
            DebugGive(ItemCatalog.ScrollReturn, "Scroll of Return x5", 5);
            DebugGive(ItemCatalog.ScrollReturnUltimate, "ULT Scroll of Return x5", 5);
            DebugGive(ItemCatalog.ScrollResurrect, "Scroll of Resurrection x5", 5);
            DebugGive(ItemCatalog.ScrollResurrectUltimate, "ULT Scroll of Resurrection x5", 5);
        }

        // ---- Functions ---------------------------------------------------------------------------

        private void BuildDebugFunctions()
        {
            _debugTitle.text = "The frequently-used levers";

            // The buffer NPC refuses above 75 (a game rule), so this is the only way to be buffed past
            // that — which matters because the balance numbers we sign off on are BUFFED numbers.
            DebugHeader("Full buffer");
            DebugAction("Full Buffs (1h)", () => Boot.Debug(n => n.DebugBuffAsync(), "buff"));

            // 10kk, not 100k: the level-40 stat swaps cost 1kk-5kk per level, so a smaller button could
            // not fund a single meaningful purchase to test with.
            DebugHeader("Gold & SP");
            DebugAction("+10,000,000 Gold", () => Boot.Debug(n => n.DebugGoldAsync(10_000_000), "gold"));
            DebugAction("+1,000,000 SP", () => Boot.Debug(n => n.DebugSpAsync(1_000_000), "sp"));

            // One round trip per click. DELEVELLING KEEPS YOUR LEARNED SKILLS — drop to 40, feel it,
            // climb back, without re-learning the whole kit.
            DebugHeader("Level");
            DebugAction("Level +1", () => Boot.Debug(n => n.DebugLevelAsync(1), "level"));
            DebugAction("Level +10", () => Boot.Debug(n => n.DebugLevelAsync(10), "level"));
            DebugAction("Level -1", () => Boot.Debug(n => n.DebugLevelAsync(-1), "level"));
            DebugAction("Level -10", () => Boot.Debug(n => n.DebugLevelAsync(-10), "level"));

            // Karma +/- was dropped at the owner's request; CLEAR stays, because an accidental kill has
            // to be undoable. The server clamps karma to [0, 1M] and clears the PK streak and the red
            // name when it reaches 0, so a delta past the floor IS the clear — no new command needed.
            DebugHeader("Karma");
            DebugAction("Karma CLEAR (all)", () => Boot.Debug(n => n.DebugKarmaAsync(-1_000_000), "karma"));
        }

        // ---- Teleport ----------------------------------------------------------------------------

        private void BuildDebugTeleport()
        {
            if (_debugTpView == 0)
            {
                _debugTitle.text = "Teleport to...";
                DebugAction("NPCs >", () => { _debugTpView = 1; RefreshDebugPanel(); });
                DebugAction("Spawn zones >", () => { _debugTpView = 2; RefreshDebugPanel(); });
                DebugAction("Cities >", () => { _debugTpView = 3; RefreshDebugPanel(); });
                return;
            }

            DebugAction("< Back", () => { _debugTpView = 0; RefreshDebugPanel(); });

            if (_debugTpView == 1)
            {
                _debugTitle.text = "NPCs";
                foreach (var npc in WorldMap.Npcs.OrderBy(n => n.Name))
                {
                    float x = npc.X, y = npc.Y;
                    DebugAction(npc.Name, () => Boot.Debug(n => n.DebugTeleportAsync(x, y), "teleport"));
                }
            }
            else if (_debugTpView == 2)
            {
                // Land ~400 units OUTSIDE the spawn ring so you arrive NEXT to the mobs, not on top of
                // a pack of them at whatever level the zone is.
                _debugTitle.text = "Spawn zones (level range)";
                foreach (var z in WorldMap.SpawnZones.OrderBy(z => z.MinLevel))
                {
                    string mob = z.MobTypes.Length > 0
                        ? (MobCatalog.Get(z.MobTypes[0])?.Name ?? z.MobTypes[0]) : "";
                    float tx = z.X + z.Radius + 400f, ty = z.Y;
                    DebugAction($"Lv {z.MinLevel}-{z.MaxLevel}  {mob}",
                                () => Boot.Debug(n => n.DebugTeleportAsync(tx, ty), "teleport"));
                }
            }
            else
            {
                _debugTitle.text = "Cities";
                foreach (var c in WorldMap.SafeZones.OrderBy(c => c.Name))
                {
                    float x = c.X, y = c.Y;
                    DebugAction(c.Name, () => Boot.Debug(n => n.DebugTeleportAsync(x, y), "teleport"));
                }
            }
        }

        // ---- Class -------------------------------------------------------------------------------

        private void BuildDebugClass()
        {
            if (_debugAddDiscView) { BuildDebugAddDiscipline(); return; }

            _debugTitle.text = "Profession, subclasses and reset";

            DebugHeader("Profession & skills");
            DebugAction("Give all skills (to my level)",
                        () => Boot.Debug(n => n.DebugLearnAllAsync(), "learn all"));

            // The owner didn't recognise a dedicated class panel and asked for class change to live
            // HERE instead (playtest 9, §10): NPCs change class via quest, debug changes it directly.
            //
            // These rows used to call DebugSetProfession — the CRAFTING profession — passing a class id
            // (1-18) into a 5-value enum, so every class from id 5 up silently became ScrollScribe
            // (playtest-13). Only classes of your own race + base class are offered; the rest need a
            // Reset first, and listing them would just be 15 rows that refuse.
            // ONE list for BOTH tiers (owner, playtest-16): the panel only ever offered the 2nd class,
            // so a 3rd class was reachable only through its quest — items and kills included — which is
            // exactly what an admin is trying to skip. Picking a DISCIPLINE row grants the 2nd class with
            // it (the server forces the parent 2nd class in HandleDebugThirdClass), so one tap is the
            // whole change; the plain 2nd-class row above each pair stays for below level 40, where the
            // server refuses a 3rd class on purpose.
            var mine = Boot.ActiveClass;
            DebugHeader("Change class — pick a discipline to get 2nd + 3rd at once");
            if (mine != null && mine.Level < ThirdClassCatalog.ChangeLevel)
                DebugNote($"Lv {mine.Level} — disciplines need level {ThirdClassCatalog.ChangeLevel} " +
                          "(the Func tab levels you).");

            // A discipline may be walked only ONCE across the classes one character owns, so anything a
            // SIBLING class already holds is shown but not offered — the server would refuse it anyway.
            var takenElsewhere = new HashSet<Discipline>();
            foreach (var s in Boot.Subclasses)
                if (!s.Active && s.ThirdClass > 0 && ThirdClassCatalog.Get(s.ThirdClass) is { } t)
                    takenElsewhere.Add(t.Discipline);

            foreach (var sc in ClassCatalog.Playable.OrderBy(s => s.Race).ThenBy(s => s.Name))
            {
                if (mine != null && (sc.Race != mine.Race || sc.Base != mine.BaseClass)) continue;
                int id = sc.Id;
                bool current2nd = mine != null && mine.SecondClass == sc.Id;
                DebugAction($"{sc.Race} {sc.Name}  (2nd only){(current2nd ? "   <- current" : "")}",
                            () => Boot.Debug(n => n.DebugSecondClassAsync(id), "2nd class"));

                foreach (var tcd in ThirdClassCatalog.ForParent(sc.Id))
                {
                    if (takenElsewhere.Contains(tcd.Discipline))
                    {
                        DebugNote($"     {tcd.Discipline} — another of your classes walks this");
                        continue;
                    }
                    int tid = tcd.Id;
                    bool current3rd = mine != null && mine.ThirdClass == tcd.Id;
                    DebugAction($"     -> {tcd.Discipline}{(current3rd ? "   <- current" : "")}",
                                () => Boot.Debug(n => n.DebugThirdClassAsync(tid), "3rd class"));
                }
            }

            // The CRAFTING profession is a separate axis (it gates which recipes you can craft) and now
            // has its own rows, instead of being the accidental target of the class list above.
            DebugHeader("Crafting profession");
            foreach (Profession prof in Enum.GetValues(typeof(Profession)))
            {
                Profession p = prof;
                DebugAction(p.ToString(),
                            () => Boot.Debug(n => n.DebugSetProfessionAsync((int)p), "crafting profession"));
            }

            // The owner's real test loop: swap class on the spot to compare two builds in the SAME
            // gear, instead of relogging onto another character. Each class keeps its own level, XP,
            // skills and skill BAR; inventory, gold and auto-hunt are shared and survive the swap.
            DebugHeader("Classes (subclass)");
            foreach (var sc in Boot.Subclasses)
            {
                string label = $"#{sc.Slot} {SubclassLabel(sc)} Lv{sc.Level}";
                if (sc.Active) DebugNote(label + "   <- playing");
                else
                {
                    int slot = sc.Slot;
                    DebugAction($"Switch to {label}",
                                () => Boot.Debug(n => n.SwitchSubclassAsync(slot), "subclass"));
                }
            }
            DebugAction("+ Add a class (discipline) >", () => { _debugAddDiscView = true; RefreshDebugPanel(); });

            DebugHeader("Reset character (re-roll, same char)");
            foreach (var race in Enum.GetValues(typeof(Race)).Cast<Race>())
            {
                if (race == Race.God) continue;   // not playable — see the god-not-playable rule
                foreach (var bc in Enum.GetValues(typeof(BaseClass)).Cast<BaseClass>())
                {
                    Race r = race; BaseClass b = bc;
                    DebugAction($"Reset -> {r} {b}", () => Boot.Debug(n => n.DebugResetAsync(r, b), "reset"));
                }
            }
        }

        private static string SubclassLabel(SubclassDto sc)
        {
            if (sc.ThirdClass > 0 && ThirdClassCatalog.Get(sc.ThirdClass) is { } tcd)
                return $"{tcd.Race} {tcd.Discipline}";
            return $"{sc.Race} {sc.BaseClass}";
        }

        /// <summary>You may not walk the same DISCIPLINE twice across the classes one character owns
        /// (archetypes are NOT restricted — several Nukers are fine as long as their disciplines
        /// differ). This only hides the barred options; the server enforces it for real.</summary>
        private void BuildDebugAddDiscipline()
        {
            _debugTitle.text = $"Add a class — each must be Lv {ThirdClassCatalog.SubclassLevel}+ " +
                               "with its 3rd class (admins exempt)";
            DebugAction("< Back", () => { _debugAddDiscView = false; RefreshDebugPanel(); });

            var owned = new HashSet<Discipline>();
            foreach (var s in Boot.Subclasses)
                if (s.ThirdClass > 0 && ThirdClassCatalog.Get(s.ThirdClass) is { } t)
                    owned.Add(t.Discipline);

            foreach (var tcd in ThirdClassCatalog.Playable
                         .Where(t => !owned.Contains(t.Discipline))
                         .OrderBy(t => t.Discipline).ThenBy(t => t.Race))
            {
                int id = tcd.Id;
                DebugAction($"{tcd.Race} {tcd.Discipline}",
                            () => Boot.Debug(n => n.DebugAddSubclassAsync(id), "add class"));
            }
        }

        // ---- Live tuning -------------------------------------------------------------------------

        private void BuildDebugTuning()
        {
            _debugTitle.text = "Runtime server knobs — applied live, persisted between runs";

            foreach (var (key, label, _) in TuneSpec)
            {
                var row = UiKit.Box(_debugContent, "TuneRow", new Color(0, 0, 0, 0), blocksInput: false);
                var le = row.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 36f;

                var text = UiKit.Label(row.transform, label, 14f, UiKit.TextDim);
                UiKit.Place(UiKit.Rect(text.gameObject), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                            new Vector2(6f, 0f), new Vector2(360f, 28f));

                var field = UiKit.InputField(row.transform, "", false, 14f);
                UiKit.Place(UiKit.Rect(field.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                            new Vector2(-6f, 0f), new Vector2(150f, 30f));
                _tuneFields[key] = field;
            }

            DebugAction("Apply", ApplyTuning);
            DebugAction("Refresh from server", () => Boot.Debug(n => n.RequestDebugConfigAsync(), "tuning"));

            if (Boot.DebugConfig != null) FillTuning(Boot.DebugConfig);
        }

        /// <summary>Populate from the server's values. Always driven by the server's echo, never by
        /// what we last sent: the server CLAMPS, and the echo is how we find out what it accepted.</summary>
        public void FillTuning(DebugConfigDto d)
        {
            if (d == null || _tuneFields.Count == 0) return;
            void Set(string key, string value) { if (_tuneFields.TryGetValue(key, out var f)) f.text = value; }
            string S(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
            string I(int v) => v.ToString(CultureInfo.InvariantCulture);

            Set("exp", S(d.ExpRate));
            Set("sp", S(d.SpRate));
            Set("dropChance", S(d.DropChanceRate));
            Set("dropAmount", S(d.DropAmountRate));
            Set("gold", S(d.GoldRate));
            Set("karmaBase", I(d.KarmaBase));
            Set("karmaConsec", S(d.KarmaConsecGrowth));
            Set("karmaLevel", S(d.KarmaLevelGrowth));
            Set("karmaDeath", I(d.KarmaLossPerDeath));
            Set("karmaMob", I(d.KarmaLossPerMob));
            Set("idleCap", I(d.IdleCapSeconds));
            Set("offlineCap", I(d.OfflineCapSeconds));
            Set("grace", I(d.GraceSeconds));
            Set("testHealPower", I(d.TestHealPower));
            Set("testSkillPower", I(d.TestSkillPower));
            Set("testSkillMod", S(d.TestSkillMod));
            Set("regenInterval", S(d.RegenIntervalSeconds));
            Set("conRegen", S(d.ConRegenBase));
            Set("mobRegen", d.MobHpRegenPctCombat.ToString("0.####", CultureInfo.InvariantCulture));
            Set("mobRegenIdle", d.MobRegenPctIdle.ToString("0.####", CultureInfo.InvariantCulture));
        }

        private void ApplyTuning()
        {
            float F(string key) =>
                _tuneFields.TryGetValue(key, out var f) &&
                float.TryParse(f.text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0f;
            int I(string key) =>
                _tuneFields.TryGetValue(key, out var f) &&
                int.TryParse(f.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

            var dto = new DebugConfigDto(
                F("exp"), F("sp"), F("dropChance"), F("dropAmount"), F("gold"),
                I("karmaBase"), F("karmaConsec"), F("karmaLevel"), I("karmaDeath"), I("karmaMob"),
                I("idleCap"), I("offlineCap"), I("grace"),
                I("testHealPower"), I("testSkillPower"), F("testSkillMod"),
                F("regenInterval"), F("conRegen"), F("mobRegen"), F("mobRegenIdle"));

            Boot.Debug(n => n.SetDebugConfigAsync(dto), "tuning");
        }
    }
}
