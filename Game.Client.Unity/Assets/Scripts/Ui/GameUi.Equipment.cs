using System.Collections.Generic;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the EQUIPMENT window — a "paper doll" of what you're wearing (squares laid
    /// out roughly like a body, each showing the item's abbreviation), plus the three equipment
    /// PRESETS (A/B/C) at the bottom. Owner's ask: not a plain list.
    ///
    /// Tapping a filled square opens that item's details (where Unequip lives). A preset row saves the
    /// worn set, re-equips a saved one (server refuses in combat), or drops a preset:x token on the bar.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _equipPanel;
        private readonly List<(string Key, TextMeshProUGUI Face, Button Btn)> _equipSquares = new();
        private int _equipRevision = -1;

        // Body-slot squares: key + label + top-left offset (from the panel's top-left). Jewels are five
        // slots filled in the order worn (necklace / earrings / rings — the server enforces the caps).
        private static readonly (string Key, string Name, float X, float Y)[] EquipSlots =
        {
            ("head",   "Head",   241f, -74f),
            ("weapon", "Weapon",  44f, -168f),
            ("body",   "Body",   241f, -168f),
            ("shield", "Shield", 438f, -168f),
            ("gloves", "Gloves", 241f, -262f),
            ("boots",  "Boots",  241f, -356f),
            ("jewel0", "",        30f, -462f),
            ("jewel1", "",       132f, -462f),
            ("jewel2", "",       234f, -462f),
            ("jewel3", "",       336f, -462f),
            ("jewel4", "",       438f, -462f),
        };

        private void BuildEquipmentWindow()
        {
            _equipPanel = UiKit.PanelBox(_worldRoot, "Equipment");
            UiKit.Place(_equipPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(560f, 700f));
            var inner = _equipPanel.GetChild(0);
            UiKit.WindowChrome(_equipPanel, "Equipment", () => CloseWindow(_equipPanel));

            const float sq = 88f;
            foreach (var slot in EquipSlots)
            {
                if (slot.Name.Length > 0)
                {
                    var lbl = UiKit.Label(inner, slot.Name, 12f, UiKit.TextDim, TextAlignmentOptions.Center);
                    UiKit.Place(UiKit.Rect(lbl.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                                new Vector2(slot.X, slot.Y + 16f), new Vector2(sq, 14f));
                }
                var box = UiKit.Box(inner, "Slot", UiKit.PanelLight);
                UiKit.Place(UiKit.Rect(box.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(slot.X, slot.Y), new Vector2(sq, sq));
                var btn = box.gameObject.AddComponent<Button>();
                btn.targetGraphic = box;
                var face = UiKit.Label(box.transform, "—", 15f, UiKit.TextDim, TextAlignmentOptions.Center);
                UiKit.Stretch(UiKit.Rect(face.gameObject), 2f, 2f, 2f, 2f);
                _equipSquares.Add((slot.Key, face, btn));
            }

            UiKit.Place(UiKit.Rect(UiKit.Label(inner, "Jewels", 12f, UiKit.TextDim).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -444f), new Vector2(120f, 14f));

            // ----- presets A / B / C -----
            UiKit.Place(UiKit.Rect(UiKit.Label(inner, "Equipment presets", 14f, UiKit.Accent).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -566f), new Vector2(400f, 20f));
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                float y = -594f - i * 34f;
                UiKit.Place(UiKit.Rect(UiKit.Label(inner, "ABC"[i].ToString(), 16f, UiKit.Text).gameObject),
                            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, y), new Vector2(24f, 26f));

                var save = UiKit.TextButton(inner, "Save", () => Boot.SaveEquipPreset(idx), 14f);
                UiKit.Place(UiKit.Rect(save.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(66f, y), new Vector2(120f, 28f));
                var equip = UiKit.TextButton(inner, "Equip", () => Boot.ApplyEquipPreset(idx), 14f);
                UiKit.Place(UiKit.Rect(equip.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(194f, y), new Vector2(120f, 28f));
                var toBar = UiKit.TextButton(inner, "To bar", () =>
                {
                    BeginAssign(GameConstants.PresetSlotToken(idx));
                    ClientLog.Info("Tap a skill-bar slot to place preset " + "ABC"[idx] + ".");
                }, 14f);
                UiKit.Place(UiKit.Rect(toBar.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(322f, y), new Vector2(120f, 28f));
            }

            _equipPanel.gameObject.SetActive(false);
        }

        private void RefreshEquipmentWindow()
        {
            if (_equipPanel == null || !_equipPanel.gameObject.activeSelf) return;

            var items = Boot.Inventory ?? System.Array.Empty<InventoryItemDto>();
            int rev = 17;
            foreach (var it in items) if (it.Equipped) rev = rev * 31 + it.InstanceId.GetHashCode() + it.Enchant;
            if (rev == _equipRevision) return;
            _equipRevision = rev;

            // Map each worn item to a slot key; jewels fill jewel0..4 in the order they're found.
            var byKey = new Dictionary<string, InventoryItemDto>();
            int jewel = 0;
            foreach (var it in items)
            {
                if (!it.Equipped) continue;
                var def = ItemCatalog.Get(it.DefId);
                if (def == null) continue;
                string key = EquipKeyOf(def, ref jewel);
                if (key != null && !byKey.ContainsKey(key)) byKey[key] = it;
            }

            foreach (var (key, face, btn) in _equipSquares)
            {
                btn.onClick.RemoveAllListeners();
                if (byKey.TryGetValue(key, out var item))
                {
                    var def = ItemCatalog.Get(item.DefId);
                    face.text = (item.Enchant > 0 ? "+" + item.Enchant + " " : "")
                              + Abbreviations.For(def != null ? def.Name : item.DefId);
                    face.color = UiKit.Good;
                    var captured = item;
                    btn.onClick.AddListener(() => OpenItemDetails(captured));
                }
                else
                {
                    face.text = "—";
                    face.color = UiKit.TextDim;
                }
            }
        }

        private static string EquipKeyOf(ItemDef def, ref int jewel)
        {
            switch (def.Slot)
            {
                case EquipSlot.Weapon: return "weapon";
                case EquipSlot.Shield: return "shield";
                case EquipSlot.Jewel:  return jewel < 5 ? "jewel" + (jewel++) : null;
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
