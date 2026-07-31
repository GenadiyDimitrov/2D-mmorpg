using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: batch B — the inventory rework.
    ///
    /// The old bag was "30 buttons": every row carried its one action inline, and there was nowhere to
    /// read what an item actually did. This replaces that with the owner's shape — a row is just
    /// <c>name (qty) [details] [e|u]</c> — plus a proper item-details window that carries the full
    /// stats, set info, a bin-delete, and an equipment COMPARE against the piece you already wear.
    ///
    /// It also builds the reusable SELECTION POPUP (a titled list of choices), which the details
    /// window uses for "delete one / delete all" and which vendors and item-boxes will reuse rather
    /// than each inventing their own modal.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        // A details window and its controls, so the main window and the compare window share one builder.
        private class DetailView
        {
            public RectTransform Panel;
            public TextMeshProUGUI Emark, Title, Body;
            public RectTransform Buttons;
            /// <summary>Kept so the body can be scrolled back to the TOP and its layout rebuilt when
            /// new text is put in it — see ShowItem.</summary>
            public ScrollRect Scroll;
        }

        private DetailView _itemView, _cmpView;

        // reusable selection popup
        private RectTransform _selectPopup, _selectOptions;
        private TextMeshProUGUI _selectTitle;

        private void BuildItemWindows()
        {
            _itemView = BuildDetailView("ItemDetails", new Vector2(0.5f, 0.5f), new Vector2(0f, 0f));
            // The compare window opens offset to the LEFT so both are on screen at once.
            _cmpView = BuildDetailView("ItemCompare", new Vector2(0.5f, 0.5f), new Vector2(-360f, 0f));
            BuildSelectionPopup();
        }

        private DetailView BuildDetailView(string name, Vector2 anchor, Vector2 offset)
        {
            var v = new DetailView();
            v.Panel = UiKit.PanelBox(_worldRoot, name);
            UiKit.Place(v.Panel, anchor, new Vector2(0.5f, 0.5f), offset, new Vector2(560f, 470f));
            var inner = v.Panel.GetChild(0);
            float chrome = UiKit.WindowChrome(v.Panel, "Item", () => CloseWindow(v.Panel));

            // Orange "E" top-left = "this is the piece you are wearing", per the owner's compare spec.
            v.Emark = UiKit.Label(inner, "E", 22f, new Color(1f, 0.6f, 0.15f), TextAlignmentOptions.Center);
            UiKit.Place(UiKit.Rect(v.Emark.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -chrome - 4f), new Vector2(26f, 26f));

            v.Title = UiKit.Label(inner, "", 18f, UiKit.Accent, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(v.Title.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(44f, -chrome - 6f), new Vector2(490f, 26f));

            // Body starts well BELOW the title (was chrome+38, only ~8px under it — the first stat line
            // "Attack +0" crammed under the name). chrome+66 gives a clear gap.
            ScrollRect scroll;
            var content = UiKit.ScrollArea(inner, out scroll, 2f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 66f, 16f, 70f);
            v.Scroll = scroll;
            v.Body = UiKit.Label(content, "", 15f, UiKit.Text, TextAlignmentOptions.TopLeft);
            v.Body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // The action row is rebuilt per open (a consumable's buttons differ from a weapon's).
            v.Buttons = UiKit.Rect(UiKit.Box(inner, "Buttons", new Color(0, 0, 0, 0), blocksInput: false).gameObject);
            v.Buttons.anchorMin = new Vector2(0f, 0f);
            v.Buttons.anchorMax = new Vector2(1f, 0f);
            v.Buttons.pivot = new Vector2(0.5f, 0f);
            v.Buttons.offsetMin = new Vector2(12f, 12f);
            v.Buttons.offsetMax = new Vector2(-12f, 60f);

            v.Panel.gameObject.SetActive(false);
            return v;
        }

        private void BuildSelectionPopup()
        {
            _selectPopup = UiKit.PanelBox(_worldRoot, "Selection");
            UiKit.Place(_selectPopup, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(440f, 360f));
            var inner = _selectPopup.GetChild(0);

            _selectTitle = UiKit.Label(inner, "", 18f, UiKit.Text, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_selectTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(20f, -18f), new Vector2(400f, 26f));

            ScrollRect scroll;
            _selectOptions = UiKit.ScrollArea(inner, out scroll, 6f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, 56f, 16f, 66f);

            var cancel = UiKit.TextButton(inner, "Cancel", () => CloseWindow(_selectPopup), 16f);
            UiKit.Place(UiKit.Rect(cancel.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 16f), new Vector2(200f, 44f));

            _selectPopup.gameObject.SetActive(false);
        }

        /// <summary>Show a titled list of choices. Reusable: bin-delete uses it for one/all, and vendors
        /// and item-boxes reuse it rather than each rolling their own modal.</summary>
        private void ShowSelection(string title, params (string Label, Action OnPick)[] options)
        {
            _selectTitle.text = title;
            for (int i = _selectOptions.childCount - 1; i >= 0; i--)
                Destroy(_selectOptions.GetChild(i).gameObject);

            foreach (var (label, onPick) in options)
            {
                var pick = onPick;
                var button = UiKit.TextButton(_selectOptions, label, () =>
                {
                    CloseWindow(_selectPopup);
                    pick?.Invoke();
                }, 16f);
                button.gameObject.AddComponent<LayoutElement>().minHeight = 46f;
            }
            OpenWindow(_selectPopup);
        }

        /// <summary>A SELECTION box was opened — turn its options into the chooser. Picking one confirms
        /// via SelectBoxItems. (All current selection boxes are pick-ONE; a pick-many box would need
        /// accumulating selection, not built.)</summary>
        public void ShowBoxSelection(SelectionOffer offer)
        {
            if (offer?.Options == null || offer.Options.Length == 0) return;
            var boxId = offer.BoxInstanceId;
            var opts = new (string, Action)[offer.Options.Length];
            for (int i = 0; i < offer.Options.Length; i++)
            {
                string itemId = offer.Options[i].ItemId;
                // Quality colour here too: a selection box can offer the same piece at different rungs,
                // and this is a one-shot irreversible choice.
                var optDef = ItemCatalog.Get(itemId);
                string optName = optDef != null
                    ? Coloured(offer.Options[i].Name, optDef.Rarity) : offer.Options[i].Name;
                opts[i] = (optName, () => Boot.SelectBoxItems(boxId, new[] { itemId }));
            }
            ShowSelection(offer.BoxName, opts);
        }

        // ----- item details ----------------------------------------------------------------------

        public void OpenItemDetails(InventoryItemDto item) => ShowItem(_itemView, item, allowCompare: true);

        /// <summary>Force the body's layout to reflect the text just assigned, and scroll to the top.
        /// Both halves matter: without the rebuild the ContentSizeFitter has not resized the label yet,
        /// and without the reset the ScrollRect keeps the offset from whatever was shown last.</summary>
        private static void ResetDetailScroll(DetailView v)
        {
            if (v.Body != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)v.Body.transform);
            if (v.Scroll != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)v.Scroll.transform);
                v.Scroll.verticalNormalizedPosition = 1f;
            }
        }

        private void ShowItem(DetailView v, InventoryItemDto item, bool allowCompare)
        {
            var def = ItemCatalog.Get(item.DefId);
            if (def == null) return;

            v.Emark.gameObject.SetActive(item.Equipped);
            v.Title.text = DetailTitle(def, item);
            v.Body.text = ItemStatsText(def, item) + SetInfoText(def);
            // The ContentSizeFitter resizes the body on the NEXT layout pass, and the ScrollRect keeps
            // whatever scroll offset it had. On the first open both were stale, so the content sat too
            // high and the first stat row ("Attack …") hid under the title bar — then looked fine on
            // every reopen, because by then the layout had caught up (playtest-13). Rebuild now and
            // pin the view to the top, so the first open renders like every other one.
            ResetDetailScroll(v);

            for (int i = v.Buttons.childCount - 1; i >= 0; i--)
                Destroy(v.Buttons.GetChild(i).gameObject);

            var id = item.InstanceId;
            string name = def.Name;
            var actions = new List<(string Label, Action Click)>();

            if (def.Slot == EquipSlot.Consumable)
            {
                actions.Add(("Use", () => { Boot.UsePotion(id); CloseWindow(v.Panel); }));
                // Quick-use bar: put "item:<defId>" on the skill bar (any stack of it satisfies the slot).
                // Reuses the skill-assign flow — tap a slot next to place it.
                actions.Add(("To bar", () =>
                {
                    BeginAssign(GameConstants.ItemSlotToken(def.Id));
                    ClientLog.Info("Tap a skill-bar slot to place " + def.Name + ".");
                    CloseWindow(v.Panel);
                }));
            }
            else if (IsWearable(def))
            {
                actions.Add((item.Equipped ? "Unequip" : "Equip",
                             () => { Boot.EquipItem(id); CloseWindow(v.Panel); }));
                // Compare only makes sense for an inventory piece with a worn counterpart of its slot.
                if (allowCompare && !item.Equipped && FindEquippedCounterpart(def) is InventoryItemDto worn)
                    actions.Add(("Compare", () => ShowItem(_cmpView, worn, allowCompare: false)));
            }
            else if (def.Slot == EquipSlot.Box)
            {
                // Open a box: a random box grants its loot; a selection box replies with a chooser
                // (ShowBoxSelection). This is how shot boxes → runes on the phone.
                actions.Add(("Open", () => { Boot.OpenBox(id); CloseWindow(v.Panel); }));
            }

            // Runes can't be binned (server refuses too) — don't offer it.
            if (!def.IsRune)
                actions.Add(("Bin", () => ConfirmBin(item, def)));

            LayoutButtons(v.Buttons, actions);
            OpenWindow(v.Panel);
        }

        /// <summary>Bin-delete: a stack asks one-vs-all through the reusable popup; a single item just
        /// confirms. Either way the details window closes once the item is gone.</summary>
        private void ConfirmBin(InventoryItemDto item, ItemDef def)
        {
            var id = item.InstanceId;
            int stack = item.Quantity;
            if (stack > 1)
            {
                // Quantity item → pick HOW MANY to bin on the numpad (owner's ask — the plain
                // one/all/cancel was "not as fancy"). Max is the whole stack.
                OpenNumpad("Delete " + def.Name, stack, qty =>
                {
                    Boot.RemoveItem(id, all: qty >= stack, quantity: qty);
                    CloseNumpad();
                    CloseAllItemViews();
                });
            }
            else
            {
                Ask("Delete " + def.Name + "?", "Delete", () => { Boot.RemoveItem(id, true); CloseAllItemViews(); });
            }
        }

        private void CloseAllItemViews()
        {
            CloseWindow(_cmpView.Panel);
            CloseWindow(_itemView.Panel);
        }

        private static bool IsWearable(ItemDef def) =>
            def.Slot == EquipSlot.Weapon || def.Slot == EquipSlot.Armor ||
            def.Slot == EquipSlot.Shield || def.Slot == EquipSlot.Jewel;

        /// <summary>The worn item that shares this one's slot (and, for armor, its body part) — the
        /// piece Compare stacks against. Null when nothing of that slot is equipped.</summary>
        private InventoryItemDto FindEquippedCounterpart(ItemDef def)
        {
            foreach (var it in Boot.Inventory ?? Array.Empty<InventoryItemDto>())
            {
                if (!it.Equipped) continue;
                var d = ItemCatalog.Get(it.DefId);
                if (d == null || d.Slot != def.Slot) continue;
                if (def.Slot == EquipSlot.Armor && d.ArmorSlot != def.ArmorSlot) continue;
                return it;
            }
            return null;
        }

        private void LayoutButtons(RectTransform row, List<(string Label, Action Click)> actions)
        {
            // Fixed usable width (panel 560 − chrome insets), not row.rect.width: the rect can still be
            // 0 the frame the panel is activated, which would collapse every button.
            const float gap = 8f, height = 44f, usable = 536f;
            float width = Mathf.Min(170f, (usable - gap * (actions.Count - 1)) / Mathf.Max(1, actions.Count));
            float x = 0f;
            foreach (var (label, click) in actions)
            {
                var button = UiKit.TextButton(row, label, click, 16f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                            new Vector2(x, 0f), new Vector2(width, height));
                // "Bin" reads as destructive.
                if (label == "Bin") button.targetGraphic.color = new Color(0.42f, 0.20f, 0.20f, 0.95f);
                x += width + gap;
            }
        }

        /// <summary>The colour a quality is drawn in. This is now the ONLY place quality shows on a
        /// name — the word used to be baked into it ("Common Electrum Longbow"), which read as a
        /// different item rather than the same bow at a lower grade (owner, playtest-13). The WPF
        /// harness only ever defined three of these; the ladder has six.</summary>
        public static Color RarityColour(ItemRarity r) => r switch
        {
            ItemRarity.Uncommon  => new Color(0.45f, 0.78f, 1.00f),   // pale blue
            ItemRarity.Rare      => new Color(1.00f, 0.84f, 0.20f),   // gold
            ItemRarity.Epic      => new Color(0.72f, 0.45f, 1.00f),   // violet — the 70% split: identity starts here
            ItemRarity.Legendary => new Color(1.00f, 0.50f, 0.15f),   // orange
            ItemRarity.Mythic    => new Color(1.00f, 0.30f, 0.35f),   // red
            ItemRarity.God       => new Color(0.60f, 1.00f, 0.60f),   // debug tier
            _                    => new Color(0.85f, 0.85f, 0.85f),   // Common — plain
        };

        /// <summary>Wrap a name in its quality colour for a TextMeshPro label.</summary>
        public static string Coloured(string text, ItemRarity r)
        {
            var c = RarityColour(r);
            return "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + text + "</color>";
        }

        private static string DetailTitle(ItemDef def, InventoryItemDto item)
        {
            string name = item.Enchant > 0 ? "+" + item.Enchant + " " + def.Name : def.Name;
            return Coloured(name, def.Rarity);   // grade/rarity moved into the description block below
        }

        /// <summary>Human-readable "what IS this" line: weapon type + hands, armour weight + slot,
        /// jewel kind, or the plain slot for everything else.</summary>
        private static string TypeLine(ItemDef def)
        {
            if (def.Slot == EquipSlot.Weapon)
                return def.WeaponType.Base() + (def.WeaponType.IsTwoHanded() ? " (2H)" : " (1H)")
                       + (def.IsMagicWeapon ? " — caster" : "");
            if (def.Slot == EquipSlot.Armor)
                return (def.Weight == ArmorWeight.None ? "" : def.Weight + " ") + def.ArmorSlot;
            if (def.Slot == EquipSlot.Jewel) return def.JewelType.ToString();
            return def.Slot.ToString();
        }

        /// <summary>The item's stats, enchant-scaled where enchant applies — the same lines the WPF
        /// tooltip shows, so the two clients describe an item identically.</summary>
        private static string ItemStatsText(ItemDef def, InventoryItemDto item)
        {
            var t = new StringBuilder();
            void Line(string s) => t.AppendLine(s);

            // The identity block the owner asked for: Name / Grade / Rarity / Type, then the stats.
            // Quality lives HERE and in the name's colour — never in the name itself.
            Line("Name:  " + def.Name);
            Line("Grade:  " + (def.ItemLevel > 0 ? ItemCatalog.TierLetter(def.ItemLevel) : def.Grade.ToString()));
            Line("Rarity:  " + Coloured(def.Rarity.ToString(), def.Rarity)
                 + "   (" + ItemCatalog.RarityPercent(def.Rarity) + "% power)");
            Line("Type:  " + TypeLine(def));
            if (!def.Tradable) Line("<color=#FF8080>Untradable</color>");
            Line("");

            if (def.AtkBonus > 0)  Line("Attack  +" + EnchantRules.BonusAt(def.AtkBonus, item.Enchant));
            if (def.MAtkBonus > 0) Line("M.Atk  +" + EnchantRules.BonusAt(def.MAtkBonus, item.Enchant));
            if (def.DefBonus > 0)  Line("Defence  +" + EnchantRules.BonusAt(def.DefBonus, item.Enchant));
            if (def.MDefBonus > 0) Line("M.Def  +" + def.MDefBonus);
            if (def.HpBonus > 0)   Line("Max HP  +" + EnchantRules.BonusAt(def.HpBonus, item.Enchant));
            if (def.MpBonus > 0)   Line("Max MP  +" + EnchantRules.BonusAt(def.MpBonus, item.Enchant));
            if (def.EvaBonus > 0)  Line("Evasion  +" + EnchantRules.BonusAt(def.EvaBonus, item.Enchant));
            if (def.WeaponRange > 0) Line("Range  " + def.WeaponRange.ToString("0"));

            // Shield block stats.
            if (def.Slot == EquipSlot.Shield)
            {
                if (def.BlockChance > 0f)    Line("Block chance  " + (def.BlockChance * 100f).ToString("0.#") + "%");
                if (def.BlockReduction > 0f) Line("Block reduction  " + (def.BlockReduction * 100f).ToString("0.#") + "%");
                if (def.ShieldDefense > 0)   Line("Shield defence  +" + def.ShieldDefense);
            }

            if (item.Attributes != null && item.Attributes.Length > 0)
            {
                t.AppendLine();
                t.AppendLine("<b>Attributes</b>");
                foreach (var a in item.Attributes)
                    Line("  " + AttributeSystem.DisplayName(a.Type) + "  +" + a.Value
                         + (AttributeSystem.IsPercent(a.Type) ? "%" : ""));
            }

            // Every consumable describes itself through the SKILL it grants.
            if (SkillCatalog.Get(def.UseSkillId) is SkillDef useDef)
            {
                t.AppendLine();
                t.AppendLine("<b>Use</b>");
                Line("  " + useDef.Description);
            }

            return t.ToString().TrimEnd();
        }

        // NOT static: the piece list says which slots you have FILLED, which needs the live inventory.
        private string SetInfoText(ItemDef def)
        {
            if (string.IsNullOrEmpty(def.SetId) || ArmorSetCatalog.Get(def.SetId) is not ArmorSetDef set)
                return "";

            var t = new StringBuilder();
            t.AppendLine();
            t.AppendLine();
            t.AppendLine("<b>Set: " + set.Name + "</b>");
            AppendSetPieces(t, set);

            var b = set.Bonus;
            var parts = new List<string>();
            if (b.MaxHp != 0)    parts.Add("HP +" + b.MaxHp);
            if (b.MaxMp != 0)    parts.Add("MP +" + b.MaxMp);
            if (b.Defence != 0)  parts.Add("Def +" + b.Defence);
            if (b.Attack != 0)   parts.Add("Atk +" + b.Attack);
            if (b.Evasion != 0)  parts.Add("Eva +" + b.Evasion);
            if (b.Accuracy != 0) parts.Add("Acc +" + b.Accuracy);
            if (set.DefencePct > 0f)   parts.Add("Def +" + (set.DefencePct * 100f).ToString("0.#") + "%");
            if (set.CastSpeedPct > 0f) parts.Add("Cast +" + (set.CastSpeedPct * 100f).ToString("0.#") + "%");
            if (parts.Count > 0) t.AppendLine("  Bonus: " + string.Join(", ", parts));

            return t.ToString().TrimEnd('\r', '\n');
        }

        /// <summary>Which pieces the set wants, and which of them you are actually wearing (32c).
        ///
        /// The window used to say only "wear the full set for a bonus" — with no way to learn WHICH
        /// four items that meant, or how close you were, short of tapping every piece in the shop.
        /// The completion rule mirrors the server's DetectActiveSet exactly: the BODY carries the
        /// set's own id, the other slots carry the shared ACCESSORY line (which is what lets a light
        /// and a robe body share one set of gloves/boots/helm).
        ///
        /// [x]/[ ] rather than a tick or a cross: the font TMP ships with has no glyph outside ASCII
        /// and draws a hollow box for one.</summary>
        private void AppendSetPieces(StringBuilder t, ArmorSetDef set)
        {
            string accId = string.IsNullOrEmpty(set.AccessorySetId) ? set.Id : set.AccessorySetId;
            var required = set.RequiredSlots ?? ArmorSetCatalog.DefaultSlots;

            // What is worn in each armor slot right now, by set id (missing = that slot is empty).
            var wornSet = new Dictionary<ArmorSlot, string>();
            var wornName = new Dictionary<ArmorSlot, string>();
            if (Boot.Inventory != null)
                foreach (var it in Boot.Inventory)
                {
                    if (!it.Equipped || ItemCatalog.Get(it.DefId) is not ItemDef sd
                        || sd.Slot != EquipSlot.Armor) continue;
                    wornSet[sd.ArmorSlot] = sd.SetId ?? "";
                    wornName[sd.ArmorSlot] = sd.Name;
                }

            int have = 0;
            var rows = new List<string>();
            foreach (var slot in required)
            {
                string needId = slot == ArmorSlot.Body ? set.Id : accId;
                bool filled = wornSet.TryGetValue(slot, out var ws) && ws == needId;
                if (filled) have++;

                // The piece(s) in the catalog that satisfy this slot. A set can offer more than one
                // body variant, so join them rather than picking the first arbitrarily.
                string names = string.Join(" / ", ItemCatalog.AllItems
                    .Where(i => i.Slot == EquipSlot.Armor && i.ArmorSlot == slot && i.SetId == needId)
                    .Select(i => i.Name).Distinct());
                if (names.Length == 0) names = "(no piece authored yet)";

                string row = "  " + (filled ? "[x] " : "[ ] ") + slot + ":  " + names;
                if (!filled && wornName.TryGetValue(slot, out var wn)) row += "   (wearing " + wn + ")";
                rows.Add(row);
            }

            t.AppendLine("  Pieces worn: " + have + " / " + required.Length
                         + (have == required.Length ? "  — bonus ACTIVE" : ""));
            foreach (var row in rows) t.AppendLine(row);
        }
    }
}
