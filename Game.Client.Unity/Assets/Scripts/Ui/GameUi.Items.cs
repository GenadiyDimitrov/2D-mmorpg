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
        // One COLUMN of the item window — heading, scrollable stat body, and (on the selected side
        // only) an action row. The window owns two of these; see BuildItemWindows.
        private class DetailView
        {
            public RectTransform Column;
            public TextMeshProUGUI Emark, Title, Body;
            public RectTransform Buttons;
            /// <summary>Kept so the body can be scrolled back to the TOP and its layout rebuilt when
            /// new text is put in it — see ShowItem.</summary>
            public ScrollRect Scroll;
        }

        // C11: compare and details are ONE window, not two. It used to be two independent panels, the
        // compare one hard-offset 360px left — which meant two title bars, two ✕s, two things to drag
        // apart when they overlapped, and no relationship on screen between the piece you are holding
        // and the piece you are wearing. Now the window GROWS a second column to the left, the same
        // shape the bag uses for its paper-doll (ToggleBagEquip). The left column is comparison only:
        // it carries no Bin, no Equip/Unequip — acting on the worn piece is not what Compare is for.
        private RectTransform _itemPanel;
        private DetailView _itemView, _cmpView;
        private bool _itemCompareOpen;

        private const float ItemColumnWidth = 528f;
        private const float ItemPanelCollapsed = ItemColumnWidth + 32f;      // one column + margins
        private const float ItemPanelExpanded = ItemPanelCollapsed + ItemColumnWidth;
        private const float ItemPanelHeight = 470f;
        private const float ItemColumnX = 16f;

        // ----- C8: one item FILTER, shared by every list of your own bag ---------------------------
        //
        // The bag, the sell vendor and the warehouse keeper all show the same inventory and all three
        // were an unordered dump of it. They now share these categories AND the name ordering, so the
        // piece you are hunting sits in the same place whichever window you opened — the point of the
        // ask was navigability, and three windows filtering three different ways would not deliver it.
        //
        // Gear = anything you can WEAR (runes included: you hold them). Use = anything you consume,
        // scrolls and boxes with it — a box is a tap-to-spend, not a material. Mats = the rest, which
        // is what "everything else" honestly is. Quest is a category so the bag can keep its own tab
        // for it; the vendor and the keeper never show it at all (B4).
        internal enum ItemCategory { All = 0, Gear = 1, Use = 2, Mats = 3, Quest = 4 }

        private static ItemCategory CategoryOf(ItemDef def) => def == null ? ItemCategory.Mats : def.Slot switch
        {
            EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield
                or EquipSlot.Jewel or EquipSlot.Rune          => ItemCategory.Gear,
            EquipSlot.Consumable or EquipSlot.Scroll
                or EquipSlot.Box                              => ItemCategory.Use,
            EquipSlot.QuestItem                               => ItemCategory.Quest,
            _                                                 => ItemCategory.Mats,
        };

        /// <summary>Does this item belong under the given tab? <see cref="ItemCategory.All"/> means
        /// everything EXCEPT quest tokens, which are only ever reachable through their own tab.</summary>
        private static bool InCategory(ItemCategory tab, ItemDef def)
        {
            var c = CategoryOf(def);
            return tab == ItemCategory.All ? c != ItemCategory.Quest : c == tab;
        }

        /// <summary>The C8 ordering: by NAME, with a stable tie-break so two rows of the same piece at
        /// different enchants never swap places between refreshes.</summary>
        private static List<InventoryItemDto> ByName(IEnumerable<InventoryItemDto> items)
        {
            var list = new List<InventoryItemDto>(items);
            list.Sort((a, b) =>
            {
                var da = ItemCatalog.Get(a.DefId);
                var db = ItemCatalog.Get(b.DefId);
                int c = string.Compare(da != null ? da.Name : a.DefId,
                                       db != null ? db.Name : b.DefId, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                c = b.Enchant.CompareTo(a.Enchant);            // stronger first
                return c != 0 ? c : a.InstanceId.CompareTo(b.InstanceId);
            });
            return list;
        }

        /// <summary>Lay out a row of category tabs and hand back the buttons, so the bag, the vendor and
        /// the keeper build an identical filter strip instead of each rolling its own.</summary>
        private Button[] BuildCategoryTabs(Transform parent, ItemCategory[] cats, Vector2 topLeft,
                                           float width, Action<ItemCategory> pick)
        {
            var buttons = new Button[cats.Length];
            for (int i = 0; i < cats.Length; i++)
            {
                var cat = cats[i];
                var b = UiKit.TextButton(parent, CategoryLabel(cat), () => pick(cat), 14f);
                UiKit.Place(UiKit.Rect(b.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(topLeft.x + i * (width + 2f), topLeft.y), new Vector2(width, 32f));
                buttons[i] = b;
            }
            return buttons;
        }

        private static string CategoryLabel(ItemCategory c) => c switch
        {
            ItemCategory.All => "All",
            ItemCategory.Gear => "Gear",
            ItemCategory.Use => "Use",
            ItemCategory.Mats => "Mats",
            _ => "Quest",
        };

        private static void PaintCategoryTabs(Button[] buttons, ItemCategory[] cats, ItemCategory active)
        {
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].targetGraphic.color = cats[i] == active ? UiKit.TabActive : UiKit.PanelLight;
        }

        // reusable selection popup
        private RectTransform _selectPopup, _selectOptions;
        private TextMeshProUGUI _selectTitle;

        private void BuildItemWindows()
        {
            _itemPanel = UiKit.PanelBox(_worldRoot, "ItemDetails");
            UiKit.Place(_itemPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(ItemPanelCollapsed, ItemPanelHeight));
            var inner = _itemPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_itemPanel, "Item", CloseAllItemViews);

            // Both columns are built at the SAME x. The selected column slides right by exactly one
            // column width when compare opens, so the window grows into the space on the left rather
            // than the list jumping under your thumb.
            _cmpView = BuildDetailColumn(inner, "Compare", chrome);
            _itemView = BuildDetailColumn(inner, "Selected", chrome);
            _cmpView.Column.gameObject.SetActive(false);

            BuildSelectionPopup();
            _itemPanel.gameObject.SetActive(false);
        }

        private DetailView BuildDetailColumn(Transform inner, string name, float chrome)
        {
            var v = new DetailView();
            var box = UiKit.Box(inner, name, new Color(0, 0, 0, 0), blocksInput: false);
            v.Column = UiKit.Rect(box.gameObject);
            UiKit.Place(v.Column, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(ItemColumnX, -chrome - 6f),
                        new Vector2(ItemColumnWidth, ItemPanelHeight - chrome - 18f));

            // Orange "E" top-left = "this is the piece you are wearing", per the owner's compare spec.
            v.Emark = UiKit.Label(v.Column, "E", 22f, new Color(1f, 0.6f, 0.15f), TextAlignmentOptions.Center);
            UiKit.Place(UiKit.Rect(v.Emark.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(0f, -2f), new Vector2(26f, 26f));

            v.Title = UiKit.Label(v.Column, "", 18f, UiKit.Accent, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(v.Title.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(32f, -4f), new Vector2(ItemColumnWidth - 40f, 26f));

            // Body starts well BELOW the title (it used to sit ~8px under it, which crammed the first
            // stat line under the name).
            ScrollRect scroll;
            var content = UiKit.ScrollArea(v.Column, out scroll, 2f);
            UiKit.Stretch((RectTransform)scroll.transform, 0f, 40f, 0f, 56f);
            v.Scroll = scroll;
            v.Body = UiKit.Label(content, "", 15f, UiKit.Text, TextAlignmentOptions.TopLeft);
            v.Body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // The action row is rebuilt per open (a consumable's buttons differ from a weapon's), and
            // stays EMPTY on the compare column.
            v.Buttons = UiKit.Rect(UiKit.Box(v.Column, "Buttons", new Color(0, 0, 0, 0), blocksInput: false).gameObject);
            v.Buttons.anchorMin = new Vector2(0f, 0f);
            v.Buttons.anchorMax = new Vector2(1f, 0f);
            v.Buttons.pivot = new Vector2(0.5f, 0f);
            v.Buttons.offsetMin = new Vector2(0f, 4f);
            v.Buttons.offsetMax = new Vector2(0f, 48f);

            return v;
        }

        /// <summary>Grow/shrink the window's compare column. Mirrors ToggleBagEquip: the panel widens
        /// and the SELECTED column slides right by the column width, so the piece you tapped stays
        /// where your eye already is and the worn piece appears beside it.</summary>
        private void SetItemCompare(bool open)
        {
            _itemCompareOpen = open;
            _cmpView.Column.gameObject.SetActive(open);
            _itemPanel.sizeDelta = new Vector2(open ? ItemPanelExpanded : ItemPanelCollapsed, ItemPanelHeight);

            var p = _itemView.Column.anchoredPosition;
            p.x = open ? ItemColumnX + ItemColumnWidth : ItemColumnX;
            _itemView.Column.anchoredPosition = p;
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

        /// <summary>Open the item window on a bag/paper-doll row. Always opens COLLAPSED — compare is a
        /// deliberate second tap, not a shape the window remembers from the last item you looked at.</summary>
        public void OpenItemDetails(InventoryItemDto item)
        {
            SetItemCompare(false);
            ShowItem(_itemView, item, actions: true);
            OpenWindow(_itemPanel);
        }

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

        /// <summary><paramref name="actions"/> false = the COMPARE column: stats only, no buttons at
        /// all (owner, C11 — "the equiped item part dont have the bin/unequip buttons").</summary>
        private void ShowItem(DetailView v, InventoryItemDto item, bool actions)
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

            // The compare column is READ-ONLY: it shows the worn piece and stops there.
            if (!actions) return;

            var id = item.InstanceId;
            var buttons = new List<(string Label, Action Click)>();

            if (def.Slot == EquipSlot.Consumable)
            {
                buttons.Add(("Use", () => { Boot.UsePotion(id); CloseAllItemViews(); }));
                // Quick-use bar: put "item:<defId>" on the skill bar (any stack of it satisfies the slot).
                // Reuses the skill-assign flow — tap a slot next to place it.
                buttons.Add(("To bar", () =>
                {
                    BeginAssign(GameConstants.ItemSlotToken(def.Id));
                    ClientLog.Info("Tap a skill-bar slot to place " + def.Name + ".");
                    CloseAllItemViews();
                }));
            }
            else if (IsWearable(def))
            {
                buttons.Add((item.Equipped ? "Unequip" : "Equip",
                             () => { Boot.EquipItem(id); CloseAllItemViews(); }));
                // Compare only makes sense for an inventory piece with a worn counterpart of its slot.
                // It TOGGLES the second column: re-showing the same item relabels this button, so the
                // window that grew has a way back that isn't "close it and find the row again".
                if (!item.Equipped && FindEquippedCounterpart(def) is InventoryItemDto worn)
                    buttons.Add((_itemCompareOpen ? "Hide" : "Compare", () =>
                    {
                        SetItemCompare(!_itemCompareOpen);
                        if (_itemCompareOpen) ShowItem(_cmpView, worn, actions: false);
                        ShowItem(v, item, actions: true);   // relabel Compare/Hide
                    }));
            }
            else if (def.Slot == EquipSlot.Box)
            {
                // Open a box: a random box grants its loot; a selection box replies with a chooser
                // (ShowBoxSelection). This is how rune boxes → runes on the phone.
                buttons.Add(("Open", () => { Boot.OpenBox(id); CloseAllItemViews(); }));
            }
            else if (ItemCatalog.IsEnchantScroll(def) || ItemCatalog.IsAttributeScroll(def))
            {
                // Enchant and attribute scrolls both work the same way from the player's side:
                // tap Use, pick which of your items to burn it on, confirm. Until 0.45.0 neither
                // had ANY phone UI at all — the commands existed on the server and nothing sent
                // them, so scrolls were dead weight in the bag.
                buttons.Add(("Use", () => BeginScrollUse(item, def)));
            }

            // Runes and QUEST ITEMS can't be binned (the server refuses both) — don't offer it. B4:
            // every disposal path refuses a token, so none of them may show the button that starts it.
            if (!def.IsRune && !ItemCatalog.IsQuestItem(def))
                buttons.Add(("Bin", () => ConfirmBin(item, def)));

            LayoutButtons(v.Buttons, buttons);
        }

        // ----- scrolls: enchant + attribute -------------------------------------------------------
        //
        // One flow for both. The eligibility rules are NOT re-implemented here — an attribute
        // scroll's grade band and "needs an existing attribute" rule come from AttributeSystem,
        // the same code the server validates with, so the list can never offer a target the
        // server will refuse. The server is still the authority; this only saves the player taps.

        /// <summary>Tapping Use on a scroll: offer the items it can legally be spent on.</summary>
        private void BeginScrollUse(InventoryItemDto scroll, ItemDef scrollDef)
        {
            bool attribute = ItemCatalog.IsAttributeScroll(scrollDef);
            var options = new List<(string Label, Action OnPick)>();

            foreach (var it in Boot.Inventory ?? Array.Empty<InventoryItemDto>())
            {
                var d = ItemCatalog.Get(it.DefId);
                if (d == null || !ScrollCanTarget(scrollDef, d, it, attribute)) continue;

                var target = it;
                var targetDef = d;
                options.Add((ScrollTargetLabel(targetDef, target, attribute),
                             () => ConfirmScrollUse(scroll, scrollDef, target, targetDef, attribute)));
            }

            if (options.Count == 0)
            {
                // Say WHY there is nothing, not just "nothing" — for an attribute scroll the reason
                // is almost always the grade band or a bare item, and both are fixable.
                ClientLog.Info(attribute
                    ? "Nothing to use it on: this scroll takes " + AttributeSystem.AcceptedGrades(scrollDef.AttrScroll)
                      + " grade weapons and jewels"
                      + (AttributeSystem.NeedsExisting(scrollDef.AttrScroll)
                            ? ", and only ones that already have an attribute." : ".")
                    : "Nothing to enchant — every piece you own is already at max enchant.");
                return;
            }

            ShowSelection(scrollDef.Name, options.ToArray());
        }

        private static bool ScrollCanTarget(ItemDef scrollDef, ItemDef def, InventoryItemDto item, bool attribute)
        {
            if (attribute)
            {
                if (!AttributeSystem.CanHoldAttribute(def)) return false;
                if (!AttributeSystem.Accepts(scrollDef.AttrScroll, AttributeSystem.TierOf(def.ItemLevel)))
                    return false;
                // A value re-roll needs something to re-roll.
                if (AttributeSystem.NeedsExisting(scrollDef.AttrScroll)
                    && (item.Attributes == null || item.Attributes.Length == 0)) return false;
                return true;
            }
            return ItemCatalog.IsEquippable(def) && item.Enchant < EnchantRules.MaxEnchant;
        }

        /// <summary>A target row: the item, plus the one number that decides whether to spend the
        /// scroll on it — its current attribute, or the odds this enchant survives.</summary>
        private static string ScrollTargetLabel(ItemDef def, InventoryItemDto item, bool attribute)
        {
            string name = Coloured((item.Enchant > 0 ? "+" + item.Enchant + " " : "") + def.Name, def.Rarity);
            if (item.Equipped) name += "  (worn)";

            if (attribute)
            {
                if (item.Attributes != null && item.Attributes.Length > 0)
                {
                    var a = item.Attributes[0];
                    return name + "\n<size=13>" + AttributeSystem.DisplayName(a.Type) + " +" + a.Value
                           + (AttributeSystem.IsPercent(a.Type) ? "%" : "") + "</size>";
                }
                return name + "\n<size=13>no attribute</size>";
            }
            int pct = Mathf.RoundToInt(EnchantRules.SuccessChance(item.Enchant) * 100f);
            return name + "\n<size=13>+" + (item.Enchant + 1) + " at " + pct + "%</size>";
        }

        /// <summary>Last stop before the scroll is gone. Enchant spells out what a FAILURE costs,
        /// because that is the part that loses the item — a Common scroll shatters it.</summary>
        private void ConfirmScrollUse(InventoryItemDto scroll, ItemDef scrollDef,
                                      InventoryItemDto target, ItemDef targetDef, bool attribute)
        {
            string question;
            if (attribute)
            {
                question = "Use " + scrollDef.Name + " on " + targetDef.Name + "?";
                if (AttributeSystem.ActionOf(scrollDef.AttrScroll) == AttrScrollAction.RollType
                    && target.Attributes != null && target.Attributes.Length > 0)
                    question += "\n\nThis REPLACES its current attribute with a random one.";
            }
            else
            {
                int pct = Mathf.RoundToInt(EnchantRules.SuccessChance(target.Enchant) * 100f);
                question = "Enchant " + targetDef.Name + " to +" + (target.Enchant + 1) + "?"
                         + "\n\nSuccess " + pct + "%.  On failure: " + EnchantFailureText(scrollDef.ScrollKind);
            }

            var scrollId = scroll.InstanceId;
            var targetId = target.InstanceId;
            Ask(question, "Use", () =>
            {
                if (attribute) Boot.RerollAttributes(scrollId, targetId);
                else Boot.Enchant(scrollId, targetId);
                CloseAllItemViews();
            });
        }

        private static string EnchantFailureText(ScrollKind kind) => kind switch
        {
            ScrollKind.Common => "<color=#FF8080>the item is DESTROYED</color>.",
            ScrollKind.Uncommon => "the enchant resets to +0.",
            ScrollKind.Rare => "the enchant drops by 1.",
            _ => "nothing happens."
        };

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
                OpenNumpad("Delete " + def.Name, stack, "Delete", qty =>
                {
                    Boot.RemoveItem(id, all: qty >= stack, quantity: qty);
                    CloseNumpad();
                    CloseAllItemViews();
                },
                qty => qty >= stack ? "the whole stack" : qty + " of " + stack + ", keeping " + (stack - qty));
            }
            else
            {
                Ask("Delete " + def.Name + "?", "Delete", () => { Boot.RemoveItem(id, true); CloseAllItemViews(); });
            }
        }

        /// <summary>One window now, so closing is one call — but the compare column is collapsed on the
        /// way out so the next item opens at the narrow shape.</summary>
        private void CloseAllItemViews()
        {
            SetItemCompare(false);
            CloseWindow(_itemPanel);
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
            // M.Def IS enchant-scaled on the server (RecomputeDerived runs it through BonusAt like
            // every other bonus); this line was the one that printed the raw base, so an enchanted
            // pendant under-reported itself — and C10 now RANKS jewels by this number.
            if (def.MDefBonus > 0) Line("M.Def  +" + EnchantRules.BonusAt(def.MDefBonus, item.Enchant));
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
                t.AppendLine("<b>Attribute</b>");
                foreach (var a in item.Attributes)
                    Line("  " + AttributeSystem.DisplayName(a.Type) + "  +" + a.Value
                         + (AttributeSystem.IsPercent(a.Type) ? "%" : ""));
            }

            // What this base COULD carry. Shown whether or not it already has one, because the
            // whole point of attributes-by-scroll is deciding if a base is worth a scroll BEFORE
            // you spend it — you can see a D-grade dagger tops out at 30% crit rate without
            // burning anything to find out.
            if (AttributeSystem.CanHoldAttribute(def))
            {
                var rolls = AttributeSystem.PossibleRolls(def).ToList();
                if (rolls.Count > 0)
                {
                    t.AppendLine();
                    t.AppendLine("<b>Can roll</b>  (attribute scroll, "
                                 + AttributeSystem.TierName(AttributeSystem.TierOf(def.ItemLevel)) + " grade)");
                    foreach (var r in rolls) Line("  " + r);
                }
            }

            // Scrolls (and anything else with authored flavour) explain themselves.
            if (!string.IsNullOrEmpty(def.Description))
            {
                t.AppendLine();
                Line(def.Description);
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

            // WHAT THE SET DOES (owner, playtest-16: "the effect is not shown — what does that set
            // do?"). This listed only the ClassFlatBonus, and every tiered set leaves that empty and
            // carries its bonus in Mods — so the answer for nearly every set in the game was a blank.
            var parts = new List<string>();
            var b = set.Bonus;
            if (b.MaxHp != 0)    parts.Add("Max HP +" + b.MaxHp);
            if (b.MaxMp != 0)    parts.Add("Max MP +" + b.MaxMp);
            if (b.Defence != 0)  parts.Add("P.Def +" + b.Defence);
            if (b.Attack != 0)   parts.Add("P.Atk +" + b.Attack);
            if (b.Evasion != 0)  parts.Add("Evasion +" + b.Evasion);
            if (b.Accuracy != 0) parts.Add("Accuracy +" + b.Accuracy);
            if (set.DefencePct > 0f)   parts.Add("P.Def +" + (set.DefencePct * 100f).ToString("0.#") + "%");
            if (set.CastSpeedPct > 0f) parts.Add("Cast speed +" + (set.CastSpeedPct * 100f).ToString("0.#") + "%");
            parts.AddRange(SkillText.Mods(set.Mods));   // the shared formatter both clients read from
            if (parts.Count > 0) t.AppendLine("  Grants: " + string.Join(", ", parts));

            // The shield extra is never required to complete the set, so it reads as a separate line
            // rather than being mixed into the numbers you get for wearing four pieces.
            var shield = SkillText.Mods(set.ShieldBonus);
            if (shield.Count > 0)
                t.AppendLine("  With the set's shield: " + string.Join(", ", shield));

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
