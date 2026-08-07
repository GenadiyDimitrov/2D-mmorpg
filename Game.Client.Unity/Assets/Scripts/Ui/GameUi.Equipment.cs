using System.Collections.Generic;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the EQUIPMENT paper-doll — squares laid out roughly like a body, each showing
    /// the worn item's abbreviation (empty squares name their slot), plus the three PRESETS (A/B/C).
    ///
    /// It is no longer its own window: it's a COLLAPSIBLE COLUMN inside the Bag, revealed by the bag's
    /// "Equip" toggle (owner: fold it into the bag, expand the window to fit it). Tapping a filled square
    /// opens that item's details (where Unequip lives). A preset row saves the worn set, re-equips a
    /// saved one (server refuses in combat), or drops a preset:x token on the bar.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _equipColumn;
        private readonly List<(string Key, string Name, TextMeshProUGUI Face, Button Btn)> _equipSquares = new();
        private int _equipRevision = -1;

        // Body-slot squares within the ~320-wide column: key + label + top-left offset. Jewels have
        // DESIGNATED slots now (owner, playtest-15 §14) — necklace, two earrings, two rings — so an
        // empty square names what belongs there and a worn pair never shuffles between refreshes.
        private const float EquipSq = 54f;
        private static readonly (string Key, string Name, float X, float Y)[] EquipSlots =
        {
            ("head",   "Head",   133f,  -6f),
            ("weapon", "Weap",    48f, -66f),
            ("body",   "Body",   133f, -66f),
            ("shield", "Shld",   218f, -66f),
            ("gloves", "Glov",   133f, -126f),
            ("boots",  "Boot",   133f, -186f),
            ("neck",   "Neck",     8f, -262f),
            ("ear1",   "Ear",     68f, -262f),
            ("ear2",   "Ear",    128f, -262f),
            ("ring1",  "Ring",   188f, -262f),
            ("ring2",  "Ring",   248f, -262f),
        };

        // The two-slot families, in paper-doll order. Slot 1 holds the STRONGER of the pair, matching
        // the server's displace rule (ItemCatalog.JewelSlotOrder), so what the square shows and what a
        // new jewel would replace can never disagree.
        private static readonly (JewelType Type, string[] Keys)[] JewelSlotKeys =
        {
            (JewelType.Necklace, new[] { "neck" }),
            (JewelType.Earring,  new[] { "ear1", "ear2" }),
            (JewelType.Ring,     new[] { "ring1", "ring2" }),
        };

        /// <summary>Build the paper-doll + presets as a column inside <paramref name="parent"/>, anchored
        /// at <paramref name="topLeft"/> (top-left of the bag inner). Hidden until the bag's Equip toggle.</summary>
        private void BuildEquipColumn(Transform parent, Vector2 topLeft)
        {
            var col = UiKit.Box(parent, "EquipColumn", new Color(0, 0, 0, 0), blocksInput: false);
            _equipColumn = UiKit.Rect(col.gameObject);
            UiKit.Place(_equipColumn, new Vector2(0f, 1f), new Vector2(0f, 1f), topLeft, new Vector2(320f, 452f));

            foreach (var slot in EquipSlots)
            {
                var box = UiKit.Box(_equipColumn, "Slot", UiKit.PanelLight);
                UiKit.Place(UiKit.Rect(box.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(slot.X, slot.Y), new Vector2(EquipSq, EquipSq));
                var btn = box.gameObject.AddComponent<Button>();
                btn.targetGraphic = box;
                var face = UiKit.Label(box.transform, slot.Name, 13f, UiKit.TextDim, TextAlignmentOptions.Center);
                UiKit.Stretch(UiKit.Rect(face.gameObject), 2f, 2f, 2f, 2f);
                _equipSquares.Add((slot.Key, slot.Name, face, btn));
            }

            UiKit.Place(UiKit.Rect(UiKit.Label(_equipColumn, "Jewels", 12f, UiKit.TextDim).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -244f), new Vector2(120f, 14f));

            // ----- presets A / B / C -----
            UiKit.Place(UiKit.Rect(UiKit.Label(_equipColumn, "Presets", 13f, UiKit.Accent).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -324f), new Vector2(200f, 18f));
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                float y = -346f - i * 32f;
                UiKit.Place(UiKit.Rect(UiKit.Label(_equipColumn, "ABC"[i].ToString(), 15f, UiKit.Text).gameObject),
                            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, y), new Vector2(18f, 26f));

                var save = UiKit.TextButton(_equipColumn, "Save", () => Boot.SaveEquipPreset(idx), 13f);
                UiKit.Place(UiKit.Rect(save.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(30f, y), new Vector2(82f, 28f));
                var equip = UiKit.TextButton(_equipColumn, "Equip", () => Boot.ApplyEquipPreset(idx), 13f);
                UiKit.Place(UiKit.Rect(equip.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(116f, y), new Vector2(82f, 28f));
                var toBar = UiKit.TextButton(_equipColumn, "To bar", () =>
                {
                    BeginAssign(GameConstants.PresetSlotToken(idx));
                    ClientLog.Info("Tap a skill-bar slot to place preset " + "ABC"[idx] + ".");
                }, 13f);
                UiKit.Place(UiKit.Rect(toBar.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(202f, y), new Vector2(82f, 28f));
            }

            _equipColumn.gameObject.SetActive(false);
        }

        private void RefreshEquipmentWindow()
        {
            if (_equipColumn == null || !_equipColumn.gameObject.activeSelf) return;

            var items = Boot.Inventory ?? System.Array.Empty<InventoryItemDto>();
            int rev = 17;
            foreach (var it in items) if (it.Equipped) rev = rev * 31 + it.InstanceId.GetHashCode() + it.Enchant;
            if (rev == _equipRevision) return;
            _equipRevision = rev;

            // Map each worn item to a slot key. Jewels are placed per sub-type, strongest first, so
            // the ring you'd displace next is always the one in the higher-numbered square.
            var byKey = new Dictionary<string, InventoryItemDto>();
            foreach (var it in items)
            {
                if (!it.Equipped) continue;
                var def = ItemCatalog.Get(it.DefId);
                if (def == null || def.Slot == EquipSlot.Jewel) continue;
                string key = EquipKeyOf(def);
                if (key != null && !byKey.ContainsKey(key)) byKey[key] = it;
            }

            foreach (var family in JewelSlotKeys)
            {
                var worn = new List<(InventoryItemDto Item, ItemDef Def)>();
                foreach (var it in items)
                {
                    if (!it.Equipped) continue;
                    var def = ItemCatalog.Get(it.DefId);
                    if (def != null && def.Slot == EquipSlot.Jewel && def.JewelType == family.Type)
                        worn.Add((it, def));
                }
                worn.Sort((a, b) => ItemCatalog.JewelSlotOrder(a.Def, a.Item.Enchant)
                            .CompareTo(ItemCatalog.JewelSlotOrder(b.Def, b.Item.Enchant)));
                for (int i = 0; i < worn.Count && i < family.Keys.Length; i++)
                    byKey[family.Keys[i]] = worn[i].Item;
            }

            // C14: which weapon, if any, is eating the off hand. `OccupiesOffHand` is the SHARED
            // predicate the server equips by (ItemDef), so the square can never disagree with the rule
            // that actually refused your shield.
            ItemDef twoHander = null;
            if (byKey.TryGetValue("weapon", out var wep)
                && ItemCatalog.Get(wep.DefId) is ItemDef wepDef && wepDef.OccupiesOffHand)
                twoHander = wepDef;

            foreach (var (key, name, face, btn) in _equipSquares)
            {
                btn.onClick.RemoveAllListeners();
                btn.interactable = true;
                if (byKey.TryGetValue(key, out var item))
                {
                    var def = ItemCatalog.Get(item.DefId);
                    face.text = (item.Enchant > 0 ? "+" + item.Enchant + " " : "")
                              + Abbreviations.For(def != null ? def.Name : item.DefId);
                    // Quality colour, so a worn square says what grade of the piece you have on. Falls
                    // back to the old green when the def is missing (an item id from a newer build).
                    face.color = def != null ? RarityColour(def.Rarity) : UiKit.Good;
                    var captured = item;
                    btn.onClick.AddListener(() => OpenItemDetails(captured));
                }
                else if (key == "shield" && twoHander != null)
                {
                    // The two-hander's own abbreviation, dimmed, and the square goes DEAD (owner, C14
                    // — "it should show something, the same icon greyed"). An empty "Shld" square here
                    // reads as a free slot, which is the one thing it is not: the weapon is holding it.
                    // Not clickable, because there is no second item to open — it is the same weapon
                    // already sitting in the square next to it.
                    face.text = Abbreviations.For(twoHander.Name);
                    face.color = Color.Lerp(RarityColour(twoHander.Rarity), UiKit.TextDim, 0.6f);
                    btn.interactable = false;
                }
                else
                {
                    face.text = name;               // empty square names its slot
                    face.color = UiKit.TextDim;
                }
            }
        }

        /// <summary>Slot key for everything that is one-per-slot. Jewels are NOT handled here — they
        /// need the whole worn set to order a pair, so RefreshEquipmentWindow places them.</summary>
        private static string EquipKeyOf(ItemDef def)
        {
            switch (def.Slot)
            {
                case EquipSlot.Weapon: return "weapon";
                case EquipSlot.Shield: return "shield";
                case EquipSlot.Armor:
                    switch (def.ArmorSlot)
                    {
                        case ArmorSlot.Head:   return "head";
                        case ArmorSlot.Body:   return "body";
                        case ArmorSlot.Gloves: return "gloves";
                        case ArmorSlot.Boots:  return "boots";
                        default:               return null;
                    }
                default: return null;
            }
        }
    }
}
