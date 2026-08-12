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
        private Button _selectCancel, _selectConfirm;

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

        /// <summary>Grow/shrink the window's compare column: the panel widens, the compare column
        /// appears on the LEFT and the selected one slides right by its width, so the piece you tapped
        /// stays where your eye already is and the worn piece appears beside it.
        ///
        /// ⚠ The panel's pivot is its CENTRE, so growing sizeDelta pushes BOTH edges outward — half
        /// the new width each way. Sliding the selected column right by a full column while its parent
        /// also moved right by half of one left it a quarter-screen from where you tapped, taking the
        /// Equip/Bin buttons out from under your thumb. Shifting the PANEL left by half the column
        /// cancels that exactly, which is what makes the sentence above true rather than aspirational.</summary>
        private void SetItemCompare(bool open)
        {
            _itemCompareOpen = open;
            _cmpView.Column.gameObject.SetActive(open);
            _itemPanel.sizeDelta = new Vector2(open ? ItemPanelExpanded : ItemPanelCollapsed, ItemPanelHeight);
            _itemPanel.anchoredPosition = new Vector2(open ? -ItemColumnWidth / 2f : 0f, 0f);

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

            // Cancel is always there; Confirm only appears for a PICK-MANY box, where tapping a row can
            // no longer mean "I have decided". Both are built once and repositioned per mode rather
            // than rebuilt, so the popup keeps one layout to reason about.
            _selectCancel = UiKit.TextButton(inner, "Cancel", () => CloseWindow(_selectPopup), 16f);
            UiKit.Place(UiKit.Rect(_selectCancel.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 16f), new Vector2(200f, 44f));

            _selectConfirm = UiKit.TextButton(inner, "Confirm", null, 16f);
            UiKit.Place(UiKit.Rect(_selectConfirm.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(106f, 16f), new Vector2(188f, 44f));
            _selectConfirm.gameObject.SetActive(false);

            _selectPopup.gameObject.SetActive(false);
        }

        /// <summary>Show a titled list of choices. Reusable: bin-delete uses it for one/all, and vendors
        /// and item-boxes reuse it rather than each rolling their own modal.</summary>
        private void ShowSelection(string title, params (string Label, Action OnPick)[] options)
        {
            _selectTitle.text = title;
            _selectConfirm.gameObject.SetActive(false);
            UiKit.Rect(_selectCancel.gameObject).anchoredPosition = new Vector2(0f, 16f);
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

        /// <summary>The same popup, but each row is a QUANTITY and one Confirm sends them all — what a
        /// pick-many box (the Blessing Box: 10 of 17) needs and a pick-one box must not have.
        ///
        /// <para>The rows used to be ticks, so ten picks meant ten DIFFERENT scrolls and "five of this
        /// one" could not be said at all — the owner hit the refusal that came out of that (playtest-20
        /// `53a`) and named the shape he wants: 5 + 3 + 2. Picks are a budget now. Tap a row to spend one
        /// on it; the row's own `-` gives one back.</para>
        ///
        /// ⚠ The counter is `[2]` and the minus is `-`, not a stepper glyph: the TMP atlas is baked with
        /// ~250 characters and anything outside it draws as a hollow box (the same trap that killed the
        /// `●` target marker).</summary>
        private void ShowMultiSelection(string title, int pickCount,
                                        (string Label, string Id)[] options, Action<List<string>> onConfirm)
        {
            var counts = new Dictionary<string, int>();
            var rows = new List<(Button Add, Button Minus, string Id, string Label)>();

            // The box is spent whole, so a PARTIAL pick forfeits the rest (playtest-19 48g: 7 of 10
            // from a 250k box). Confirm stays dead until every pick is spent; the server enforces the
            // same count, this end only makes the rule visible before the tap.
            int required = pickCount;
            int Spent() { int n = 0; foreach (var c in counts.Values) n += c; return n; }

            void Redraw()
            {
                int spent = Spent();
                _selectTitle.text = $"{title} — {spent} / {required}";
                foreach (var (add, minus, id, label) in rows)
                {
                    counts.TryGetValue(id, out int n);
                    UiKit.SetButtonText(add, (n > 0 ? $"[{n}] " : "[  ] ") + label);
                    minus.gameObject.SetActive(n > 0);
                }
                UiKit.SetButtonText(_selectConfirm,
                    spent == required ? "Confirm" : $"Choose {required - spent} more");
                _selectConfirm.interactable = spent == required;
            }

            for (int i = _selectOptions.childCount - 1; i >= 0; i--)
                Destroy(_selectOptions.GetChild(i).gameObject);
            rows.Clear();

            foreach (var (label, id) in options)
            {
                string optId = id, optLabel = label;

                // One row = the option (takes the width) + a minus that appears once you've spent on it.
                var row = new GameObject("Option", typeof(RectTransform));
                row.transform.SetParent(_selectOptions, false);
                var group = row.AddComponent<HorizontalLayoutGroup>();
                group.spacing = 6f;
                group.childForceExpandWidth = false;
                group.childForceExpandHeight = true;
                group.childControlWidth = true;
                group.childControlHeight = true;
                row.AddComponent<LayoutElement>().minHeight = 46f;

                var add = UiKit.TextButton(row.transform, label, () =>
                {
                    // Refuse the 11th rather than silently dropping it: the server takes exactly
                    // PickCount, so a quietly-ignored tap would spend a 250k box on a set the player
                    // did not choose.
                    if (Spent() >= required) { ShowToast($"Only {required} may be chosen."); return; }
                    counts.TryGetValue(optId, out int n);
                    counts[optId] = n + 1;
                    Redraw();
                }, 16f);
                var addLayout = add.gameObject.AddComponent<LayoutElement>();
                addLayout.flexibleWidth = 1f;
                addLayout.minHeight = 46f;

                var minus = UiKit.TextButton(row.transform, "-", () =>
                {
                    if (!counts.TryGetValue(optId, out int n) || n <= 0) return;
                    if (n == 1) counts.Remove(optId); else counts[optId] = n - 1;
                    Redraw();
                }, 16f);
                var minusLayout = minus.gameObject.AddComponent<LayoutElement>();
                minusLayout.minWidth = minusLayout.preferredWidth = 52f;
                minusLayout.minHeight = 46f;

                rows.Add((add, minus, optId, optLabel));
            }

            _selectConfirm.onClick.RemoveAllListeners();
            _selectConfirm.onClick.AddListener(() =>
            {
                if (Spent() != required) return;
                // Repeats ARE the quantity — the server counts them (HandleSelectBoxItems).
                var picks = new List<string>();
                foreach (var (_, _, id, _) in rows)
                    if (counts.TryGetValue(id, out int n))
                        for (int k = 0; k < n; k++) picks.Add(id);
                CloseWindow(_selectPopup);
                onConfirm?.Invoke(picks);
            });
            _selectConfirm.gameObject.SetActive(true);
            UiKit.Rect(_selectCancel.gameObject).anchoredPosition = new Vector2(-106f, 16f);
            Redraw();
            OpenWindow(_selectPopup);
        }

        /// <summary>A Rune of Tincture was used — the colour list it opens (owner, playtest-20 `59r`).
        /// Reuses the pick-ONE popup: choosing sends the ordinary SetTitleColor command, which is where
        /// the server spends the rune, so closing this list costs nothing.</summary>
        public void ShowTitleColorPicker(TitleColorOffer offer)
        {
            if (offer?.Colors == null || offer.Colors.Length == 0) return;
            var opts = new (string, Action)[offer.Colors.Length];
            for (int i = 0; i < offer.Colors.Length; i++)
            {
                string colour = offer.Colors[i];
                opts[i] = (colour, () => Boot.SetTitleColor(colour));
            }
            ShowSelection("Rune of Tincture", opts);
        }

        /// <summary>A SELECTION box was opened — turn its options into the chooser. Pick-ONE confirms on
        /// the tap; pick-MANY (PickCount &gt; 1) toggles rows and confirms once, since with ten picks in
        /// one box "I tapped it" and "I am done" stopped being the same gesture.</summary>
        public void ShowBoxSelection(SelectionOffer offer)
        {
            if (offer?.Options == null || offer.Options.Length == 0) return;
            var boxId = offer.BoxInstanceId;

            string Name(SelectionOption o)
            {
                // Quality colour here too: a selection box can offer the same piece at different rungs,
                // and this is a one-shot irreversible choice.
                var optDef = ItemCatalog.Get(o.ItemId);
                return optDef != null ? Coloured(o.Name, optDef.Rarity) : o.Name;
            }

            if (offer.PickCount > 1)
            {
                var many = new (string, string)[offer.Options.Length];
                for (int i = 0; i < offer.Options.Length; i++)
                    many[i] = (Name(offer.Options[i]), offer.Options[i].ItemId);
                ShowMultiSelection(offer.BoxName, offer.PickCount, many,
                                   picks => Boot.SelectBoxItems(boxId, picks.ToArray()));
                return;
            }

            var opts = new (string, Action)[offer.Options.Length];
            for (int i = 0; i < offer.Options.Length; i++)
            {
                string itemId = offer.Options[i].ItemId;
                opts[i] = (Name(offer.Options[i]), () => Boot.SelectBoxItems(boxId, new[] { itemId }));
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
                    : "Nothing to use it on: this scroll takes "
                      + EnchantRules.GradeName(scrollDef.ScrollGrade)
                      + " grade gear that is not already at max enchant.");
                return;
            }

            ShowSelection(scrollDef.Name, options.ToArray());
        }

        /// <summary>ADMIN `/enchant &lt;value&gt;` (D2): pick a piece of gear and set it to that enchant
        /// outright. Unlike the scroll flow this offers EVERY equippable item — no grade band, no max —
        /// because the command exists precisely to reach what a scroll cannot.</summary>
        public void BeginAdminEnchant(int value)
        {
            var options = new List<(string Label, Action OnPick)>();
            foreach (var it in Boot.Inventory ?? Array.Empty<InventoryItemDto>())
            {
                var d = ItemCatalog.Get(it.DefId);
                if (d == null || !ItemCatalog.IsEquippable(d)) continue;

                var target = it;
                string label = Coloured((it.Enchant > 0 ? "+" + it.Enchant + " " : "") + d.Name, d.Rarity)
                             + (it.Equipped ? "  (worn)" : "")
                             + "\n<size=13>" + EnchantRules.GradeName(EnchantRules.GradeOf(d))
                             + " grade  →  +" + value + "</size>";
                options.Add((label, () =>
                {
                    Boot.AdminEnchant(target.InstanceId, value);
                    CloseAllItemViews();
                }));
            }

            if (options.Count == 0) { ClientLog.Warn("No weapons, armor or jewels in your bag."); return; }
            ShowSelection("Set enchant to +" + value, options.ToArray());
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
            // The GRADE BAND is checked from EnchantRules — the same code the server validates with, so
            // the list can never offer a target the server will then refuse.
            return ItemCatalog.IsEquippable(def)
                   && item.Enchant < EnchantRules.MaxEnchant
                   && EnchantRules.Accepts(scrollDef, def);
        }

        /// <summary>A target row: the item, plus the one number that decides whether to spend the
        /// scroll on it — its current attribute, or the odds this enchant survives.</summary>
        private static string ScrollTargetLabel(ItemDef def, InventoryItemDto item, bool attribute)
        {
            string name = Coloured((item.Enchant > 0 ? "+" + item.Enchant + " " : "") + ItemTag.Name(def, item), def.Rarity);
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

        /// <summary>What a failure costs, coloured. The WORDS come from EnchantRules so the popup and
        /// the server's own outcome line can never drift apart; only the colour is the client's.</summary>
        private static string EnchantFailureText(ScrollKind kind) => kind switch
        {
            ScrollKind.Normal => "<color=#FF8080>" + EnchantRules.FailureText(kind) + "</color>.",
            ScrollKind.Safe => "<color=#80FF80>" + EnchantRules.FailureText(kind) + "</color>.",
            _ => EnchantRules.FailureText(kind) + ".",
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

        /// <summary>The worn item that shares this one's slot (and, for armor, its body part; for a
        /// jewel, its JewelType) — the piece Compare stacks against. Null when nothing of that slot is
        /// equipped.
        /// ⚠ `EquipSlot.Jewel` covers rings, earrings AND necklaces, so matching on the slot alone
        /// compared a pendant against whichever jewel sat first in the bag — a stud (playtest-19 46m).
        /// Rings and earrings are worn in PAIRS, and equipping into a full pair displaces the WEAKER
        /// one, so that is the piece the comparison has to be against.</summary>
        private InventoryItemDto FindEquippedCounterpart(ItemDef def)
        {
            InventoryItemDto weakestJewel = null;
            long weakestStrength = long.MaxValue;

            foreach (var it in Boot.Inventory ?? Array.Empty<InventoryItemDto>())
            {
                if (!it.Equipped) continue;
                var d = ItemCatalog.Get(it.DefId);
                if (d == null || d.Slot != def.Slot) continue;
                if (def.Slot == EquipSlot.Armor && d.ArmorSlot != def.ArmorSlot) continue;
                if (def.Slot == EquipSlot.Jewel)
                {
                    if (d.JewelType != def.JewelType) continue;
                    long strength = ItemCatalog.JewelStrength(d, it.Enchant);
                    if (strength < weakestStrength) { weakestStrength = strength; weakestJewel = it; }
                    continue;
                }
                return it;
            }
            return weakestJewel;
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
            // (ItemRarity.God's green was deleted 2026-08-07 with the God layer.)
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
            string baseName = ItemTag.Name(def, item);   // the instance's own name if it was given one
            string name = item.Enchant > 0 ? "+" + item.Enchant + " " + baseName : baseName;
            string tag = ItemTag.For(def, item);
            return Coloured(name, def.Rarity)            // grade/rarity moved into the description block below
                 + (tag.Length > 0 ? " <size=13><color=#9090A0>" + tag + "</color></size>" : "");
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

        /// <summary>
        /// "Expires in 29d 4h", colour-graded (C3): green over 7d, white over 1d, yellow over 1h, red
        /// under it — so a loaner kit or a rune tells you how much of it is left without doing date
        /// arithmetic, and goes loud only when it is actually about to go.
        ///
        /// Driven by <see cref="InventoryItemDto.ExpiresAtUtc"/>, which is stamped at ACQUIRE time and
        /// runs on the WALL clock (it keeps ticking while you are offline), so this is a plain
        /// UtcNow difference and not something the client counts down itself. Empty for the
        /// overwhelming majority of items, which carry no clock at all.
        /// </summary>
        private static string TimedLine(InventoryItemDto item)
        {
            if (item.ExpiresAtUtc == null) return "";

            var span = item.ExpiresAtUtc.Value - System.DateTime.UtcNow;
            // Already past, but the server has not swept it yet (the purge runs on its own cadence).
            // Say so rather than printing a negative — it is gone on the next reconcile either way.
            if (span.TotalSeconds <= 0) return "<color=#FF6060>Expired</color>";

            string hex = span.TotalDays  > 7f ? "#66E066"    // green  — plenty of time
                       : span.TotalDays  > 1f ? "#E6E6E6"    // white  — days left
                       : span.TotalHours > 1f ? "#FFD44D"    // yellow — hours left
                       :                        "#FF6060";   // red    — going within the hour

            string left = span.TotalDays  >= 1f ? (int)span.TotalDays + "d " + span.Hours + "h"
                        : span.TotalHours >= 1f ? span.Hours + "h " + span.Minutes + "m"
                        : span.Minutes    >= 1  ? span.Minutes + "m " + span.Seconds + "s"
                        :                         span.Seconds + "s";

            return "<color=" + hex + ">Expires in " + left + "</color>";
        }

        /// <summary>The item's stats, enchant-scaled where enchant applies — the same lines the WPF
        /// tooltip shows, so the two clients describe an item identically.</summary>
        private static string ItemStatsText(ItemDef def, InventoryItemDto item)
        {
            var t = new StringBuilder();
            void Line(string s) => t.AppendLine(s);

            // The identity block the owner asked for: Name / Grade / Rarity / Type, then the stats.
            // Quality lives HERE and in the name's colour — never in the name itself.
            // The INSTANCE's name and tag (`58d`) — a renamed or bound copy must not read as an
            // ordinary one, which is the whole point of handing it out with tags.
            string tag58d = ItemTag.For(def, item);
            Line("Name:  " + ItemTag.Name(def, item) + (tag58d.Length > 0 ? "  " + tag58d : ""));
            Line("Grade:  " + (def.ItemLevel > 0 ? ItemCatalog.TierLetter(def.ItemLevel) : def.Grade.ToString()));
            Line("Rarity:  " + Coloured(def.Rarity.ToString(), def.Rarity)
                 + "   (" + ItemCatalog.RarityPercent(def.Rarity) + "% power)");
            Line("Type:  " + TypeLine(def));
            if (!ItemTag.Tradable(def, item.TradableOverride)) Line("<color=#FF8080>Untradable</color>");
            if (item.CanStorePrivate == false) Line("<color=#FF8080>The keeper will not accept this</color>");
            else if (item.CanStoreAccount == false) Line("<color=#FF8080>Cannot go in the account warehouse</color>");
            string timed = TimedLine(item);
            if (timed.Length > 0) Line(timed);
            Line("");

            // ⚠ The TOTAL decides whether a line is printed, not the authored base. Since 0.60.0 an
            // enchant adds a FLAT amount to stats a piece may not carry at all — every tiered armour
            // has HpBonus 0, so a +16 S body owes +480 Max HP that the old `def.HpBonus > 0` test
            // would have hidden completely. Same server math (EnchantRules), same numbers on the card.
            int cAtk  = def.AtkBonus   + EnchantRules.AtkDelta(def, item.Enchant);
            int cMAtk = def.MAtkBonus  + EnchantRules.MAtkDelta(def, item.Enchant);
            int cDef  = def.DefBonus   + EnchantRules.DefDelta(def, item.Enchant);
            int cMDef = def.MDefBonus  + EnchantRules.MDefDelta(def, item.Enchant);
            int cHp   = def.HpBonus    + EnchantRules.HpDelta(def, item.Enchant);
            int cMp   = def.MpBonus    + EnchantRules.MpDelta(def, item.Enchant);
            if (cAtk > 0)  Line("Attack  +" + cAtk);
            if (cMAtk > 0) Line("M.Atk  +" + cMAtk);
            if (cDef > 0)  Line("Defence  +" + cDef);
            if (cMDef > 0) Line("M.Def  +" + cMDef);
            if (cHp > 0)   Line("Max HP  +" + cHp);
            if (cMp > 0)   Line("Max MP  +" + cMp);
            if (def.EvaBonus > 0) Line("Evasion  +" + def.EvaBonus);   // evasion does not enchant
            if (def.WeaponRange > 0) Line("Range  " + def.WeaponRange.ToString("0"));

            // F GRADE HAS NO BAND, so no scroll can ever touch it (EnchantRules.GradeOf → None) — and
            // printing "Per enchant +N" on a piece you cannot enchant advertises a purchase that does
            // not exist (owner, playtest-21 `68h`: *"F grade should say unenchantable or atleast remove
            // the + bonus - u cannot get it"*). Say so once, on the gear slots where the question even
            // arises; a potion or a material simply prints nothing, as before.
            bool enchantSlot = def.Slot is EquipSlot.Weapon or EquipSlot.Armor
                                        or EquipSlot.Shield or EquipSlot.Jewel;
            if (enchantSlot && EnchantRules.GradeOf(def) == EnchantGrade.None)
            {
                Line("<color=#9090A0>Unenchantable</color>");
            }
            // What one more enchant would buy, on the pieces where it buys anything. This is the
            // number the player is deciding to spend a scroll on, so it belongs on the card.
            else if (item.Enchant < EnchantRules.MaxEnchant)
            {
                string per = def.Slot switch
                {
                    EquipSlot.Weapon => "+" + EnchantRules.AtkPerEnchant(def) + " Atk, +"
                                        + EnchantRules.WeaponMAtkPerEnchant + " M.Atk",
                    EquipSlot.Armor  => "+" + EnchantRules.ArmorDefPerEnchant + " Def"
                                        + (EnchantRules.HpDelta(def, 1) > 0
                                           ? ", +" + EnchantRules.HpDelta(def, 1) + " HP" : ""),
                    EquipSlot.Shield => "+" + EnchantRules.ShieldDefPerEnchant + " shield defence"
                                        + (EnchantRules.HpDelta(def, 1) > 0
                                           ? ", +" + EnchantRules.HpDelta(def, 1) + " HP" : ""),
                    EquipSlot.Jewel  => "+" + EnchantRules.JewelMDefPerEnchant + " M.Def"
                                        + (EnchantRules.MpDelta(def, 1) > 0
                                           ? ", +" + EnchantRules.MpDelta(def, 1) + " MP" : ""),
                    _ => ""
                };
                if (per.Length > 0) Line("<color=#9090A0>Per enchant  " + per + "</color>");
            }

            // Shield block stats.
            if (def.Slot == EquipSlot.Shield)
            {
                if (def.BlockChance > 0f)    Line("Block chance  " + (def.BlockChance * 100f).ToString("0.#") + "%");
                if (def.BlockReduction > 0f) Line("Block reduction  " + (def.BlockReduction * 100f).ToString("0.#") + "%");
                if (def.ShieldDefense > 0)
                    Line("Shield defence  +" + (def.ShieldDefense + EnchantRules.ShieldDefDelta(def, item.Enchant)));
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
