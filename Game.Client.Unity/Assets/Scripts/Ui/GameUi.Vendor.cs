using System;
using System.Collections.Generic;
using System.Text;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: batch C — the VENDOR (buy/sell), on the owner's spec.
    ///
    /// A vendor asks first: buy or sell? Buy shows the vendor's wares; sell shows MY sellable
    /// inventory. Picking an item that stacks opens a NUMPAD (digits, clear, backspace, a keyboard-
    /// capable number box, and a Max/All that fills the whole stack when selling or the most you can
    /// afford when buying — so Max can never order a refusal).
    ///
    /// ONE confirmation, never two (playtest-16). Every row already describes its item, so a stackable
    /// goes straight to the pad and the PAD is the confirmation: it shows the running total and its
    /// button says "Buy"/"Sell". A non-stacking item has no pad, so it gets the plain confirm dialog
    /// with the full stat sheet. Either way nothing leaves your purse on a single unlabelled tap.
    ///
    /// The server owns the transaction and every price: the buy price rides the shop DTO, the sell
    /// price is the shared ItemCatalog formula, and the server re-checks gold, stock and sellability —
    /// this window only gathers "which item, how many" and asks for confirmation.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _vendorPanel, _vendorList;
        private TextMeshProUGUI _vendorTitle;
        private Button _vendorBuyTab, _vendorSellTab, _vendorViewTab, _vendorQSellTab;
        /// <summary>`BL-240` — the instant-sale button. Sell side only.</summary>
        private Button _vendorInstantSell;
        private bool _vendorSell;
        /// <summary>V1 (playtest-18): quick-sell, the bin's twin. With it ON a row sells the WHOLE
        /// stack on one tap — no numpad, no confirm — which is what emptying a bag of trash actually
        /// wants. Off by default and re-armed per session for the same reason the bin is: it removes
        /// the only step between a mis-tap and a sold item. Sell is at least undoable (the buy-back
        /// list), which is why this one may skip the confirm where the bin still cannot.</summary>
        private bool _vendorQuickSell;
        /// <summary>Detail view = two lines per row (name+price, then what it IS). Remembered across
        /// vendors because it is a preference, not a per-shop mode.</summary>
        private bool _vendorDetailed = true;
        private int _vendorRevision = -1;
        /// <summary>C8's category filter. It sits on BOTH lists, not just the sell side he named: the
        /// two share one window and one strip of tabs, and a filter row that went dead every time you
        /// tapped Buy would read as a bug. A vendor stocks gear, potions and mats together, so the
        /// buy side wants it for the same reason the bag did.</summary>
        private ItemCategory _vendorTab = ItemCategory.All;
        private Button[] _vendorTabButtons;
        private static readonly ItemCategory[] VendorTabs =
            { ItemCategory.All, ItemCategory.Gear, ItemCategory.Use, ItemCategory.Mats };

        // `BL-290` — A VENDOR'S TABS ARE ITS OWN, on the BUY side: the strip is rebuilt from the shop being
        // shown (`ShopDef.Tabs`), with "All" in front. The sell side and a shop with no tabs of its own keep
        // the generic four above. The strip is rebuilt only when WHICH strip it is changes.
        private Transform _vendorInner;
        private float _vendorChrome;
        private Button _vendorOrderButton;
        private string _vendorStripKey;
        /// <summary>The shop tabs the strip currently shows; null = the generic ItemCategory tabs.</summary>
        private ShopTab[] _vendorShopTabs;
        /// <summary>0 = All, i = <c>_vendorShopTabs[i - 1]</c>. Kept across a Buy/Sell flip, reset for a new shop.</summary>
        private int _vendorShopTab;
        private string _vendorShopTabOf = "";
        private const float VendorRowWidth = 624f, VendorOrderWidth = 86f;

        // numpad
        private RectTransform _numpadPanel;
        private TextMeshProUGUI _numpadTitle;
        private TMP_InputField _numpadInput;
        private Button _numpadOkButton;
        // `BL-279` — the bottom-left button is "Max" for a quantity and a MODE switch (exact/added) for a
        // delay. Kept so OpenNumpad can repaint it per use.
        private Button _numpadMaxButton;
        private Func<string> _numpadModeLabel;
        private Action _numpadModeTap;
        private int _numpadMax = 1;
        private Action<int> _numpadOk;
        private string _numpadHead = "";
        /// <summary>Renders the running total under the title — the pad is the confirmation now, so
        /// the price has to live on it.</summary>
        private Func<int, string> _numpadSummary;

        private void BuildVendorWindow()
        {
            _vendorPanel = UiKit.PanelBox(_worldRoot, "Vendor");
            UiKit.Place(_vendorPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(660f, 500f));
            var inner = _vendorPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_vendorPanel, "Vendor", () => CloseWindow(_vendorPanel));

            _vendorTitle = UiKit.Label(inner, "", 17f, UiKit.TextDim, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_vendorTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 6f), new Vector2(400f, 22f));

            _vendorBuyTab = UiKit.TextButton(inner, "Buy", () => SetVendorMode(false), 15f);
            UiKit.Place(UiKit.Rect(_vendorBuyTab.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 32f), new Vector2(120f, 30f));
            _vendorSellTab = UiKit.TextButton(inner, "Sell", () => SetVendorMode(true), 15f);
            UiKit.Place(UiKit.Rect(_vendorSellTab.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(144f, -chrome - 32f), new Vector2(120f, 30f));

            // QSell sits beside the Sell tab it modifies, and is HIDDEN on the buy side — a toggle that
            // goes dead when you tap Buy reads as a bug (the same reasoning that put the category
            // filter on both lists instead of blanking it).
            _vendorQSellTab = UiKit.TextButton(inner, "QSell: off",
                () => { _vendorQuickSell = !_vendorQuickSell; _vendorRevision = -1; }, 14f);
            UiKit.Place(UiKit.Rect(_vendorQSellTab.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(270f, -chrome - 32f), new Vector2(122f, 30f));

            // COMPACT vs DETAIL. Compact is one line per item for scrolling a long ladder; detail adds
            // a second line naming the type, grade, quality and the stat that matters (owner asked for
            // a button that switches between "list-rows" and rows carrying their description).
            _vendorViewTab = UiKit.TextButton(inner, "Detail", ToggleVendorView, 15f);
            UiKit.Place(UiKit.Rect(_vendorViewTab.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-18f, -chrome - 32f), new Vector2(120f, 30f));

            // `BL-240` — INSTANT SALE. It sits on the CATEGORY row, hard right, because the tab is half
            // of what it means: *"it sells everything of that rarity depending on the tab you are on"*.
            // Hidden on the buy side, like QSell, for the same reason.
            _vendorInstantSell = UiKit.TextButton(inner, "Instant sale", BeginInstantSell, 14f);
            UiKit.Place(UiKit.Rect(_vendorInstantSell.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-18f, -chrome - 66f), new Vector2(152f, 30f));

            // `BL-290`: the tab strip itself is built by EnsureVendorTabs, because it depends on the shop.
            _vendorInner = inner;
            _vendorChrome = chrome;
            // `BL-117` — same button, same shared order, at the end of this window's tabs (moved there by
            // EnsureVendorTabs, since the number of tabs now varies by shop).
            _vendorOrderButton = BuildOrderButton(inner, new Vector2(18f + VendorTabs.Length * 90f, -chrome - 66f), VendorOrderWidth,
                             // Both lists this window owns: the sell shelf AND the buyback shelf,
                             // which is its own panel with its own revision and would otherwise keep
                             // the previous order until the next sale moved its hash.
                             () => { _vendorRevision = -1; _buyBackRevision = -1; });

            ScrollRect scroll;
            _vendorList = UiKit.ScrollArea(inner, out scroll, 3f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 104f, 16f, 16f);

            _vendorPanel.gameObject.SetActive(false);

            BuildNumpad();
        }

        /// <summary>Open the vendor from the NPC dialog's Buy/Sell buttons.</summary>
        public void OpenVendor(bool sell)
        {
            _vendorSell = sell;
            _vendorRevision = -1;   // force a rebuild
            OpenWindow(_vendorPanel);
        }

        private void SetVendorMode(bool sell)
        {
            _vendorSell = sell;
            _vendorRevision = -1;
        }

        private void ToggleVendorView()
        {
            _vendorDetailed = !_vendorDetailed;
            _vendorRevision = -1;
        }

        /// <summary>Rebuild the list when the mode, gold, or inventory changed — so a sell removes the
        /// row it sold and a buy re-checks what you can still afford, both driven by the server's push.</summary>
        private void RefreshVendorWindow()
        {
            if (!_vendorPanel.gameObject.activeSelf) return;

            EnsureVendorTabs();
            var items = Boot.Inventory ?? Array.Empty<InventoryItemDto>();
            int revision = (_vendorSell ? 1 : 0) * 92821 + (_vendorDetailed ? 7919 : 0)
                         + _vendorShopTab * 1299709 + (_vendorShopTabs == null ? 0 : 3571)   // `BL-290`
                         + (_vendorQuickSell ? 15485863 : 0)
                         + (int)_vendorTab * 104729 + (int)(Boot.Gold % 1_000_000)
                         + (int)(Boot.Platinum % 1_000_000) * 7    // `BL-257` — the wallet has two halves now
                         + Boot.LockRevision * 1013;               // `BL-239` — a lock adds/removes rows
            revision = revision * 31 + (Boot.Dialog?.Shop?.Items?.Length ?? 0);
            // Identity, not just quantity — same reason as the bag stamp: an item swapped for another
            // of the same count would otherwise leave the sell list showing what you no longer own.
            foreach (var it in items)
                revision = revision * 31 + it.InstanceId.GetHashCode() + it.Quantity + (it.Equipped ? 7 : 0);
            if (revision == _vendorRevision) return;
            _vendorRevision = revision;

            _vendorTitle.text = _vendorSell
                ? _vendorQuickSell ? "Sell — one tap sells the WHOLE stack"
                                   : "Sell — pick an item from your bag"
                : "Buy — you have " + Wallet();
            _vendorBuyTab.targetGraphic.color = _vendorSell ? UiKit.PanelLight : UiKit.TabActive;
            _vendorSellTab.targetGraphic.color = _vendorSell ? UiKit.TabActive : UiKit.PanelLight;
            UiKit.SetButtonText(_vendorViewTab, _vendorDetailed ? "Compact" : "Detail");
            _vendorViewTab.targetGraphic.color = _vendorDetailed ? UiKit.TabActive : UiKit.PanelLight;
            _vendorQSellTab.gameObject.SetActive(_vendorSell);
            _vendorInstantSell.gameObject.SetActive(_vendorSell);   // `BL-240`
            UiKit.SetButtonText(_vendorQSellTab, _vendorQuickSell ? "QSell: ON" : "QSell: off");
            _vendorQSellTab.targetGraphic.color = _vendorQuickSell
                ? new Color(0.42f, 0.20f, 0.20f, 0.95f)   // the bin's armed red — it skips the confirm too
                : UiKit.PanelLight;
            if (_vendorShopTabs == null) PaintCategoryTabs(_vendorTabButtons, VendorTabs, _vendorTab);
            else
                for (int i = 0; i < _vendorTabButtons.Length; i++)
                    _vendorTabButtons[i].targetGraphic.color = i == _vendorShopTab ? UiKit.TabActive : UiKit.PanelLight;

            for (int i = _vendorList.childCount - 1; i >= 0; i--)
                Destroy(_vendorList.GetChild(i).gameObject);

            if (_vendorSell) BuildSellList(items);
            else BuildBuyList();
        }

        // ═══ `BL-257` — TWO CURRENCIES ON ONE SHELF ═══════════════════════════════════════════════
        //
        // *"any item that have a platinum or/and gold must be bought with the value."* So a row prices
        // whichever halves it has, an item you cannot afford is dim because of EITHER half, and the
        // numpad's maximum is the smaller of what the two wallets allow. All three read the same two
        // helpers rather than each spelling out "if plat > 0" — the shop and `HandleBuy` already agree
        // on the rule and the UI must not invent a third version of it.

        /// <summary>What the player is carrying, both halves. Platinum is only named when there is some
        /// — a wallet line reading "0 Platinum" on every vendor in the game is noise until he has any.</summary>
        private string Wallet() =>
            Boot.Gold.ToString("N0") + " " + GameConstants.CurrencyName
            + (Boot.Platinum > 0 ? "  ·  " + Boot.Platinum.ToString("N0") + " " + GameConstants.PlatinumName : "");

        /// <summary>A price tag: gold, platinum, or both. Unlike the wallet above, a zero half here is
        /// simply absent — the item genuinely does not cost it.</summary>
        private static string Price(long gold, long plat, int qty = 1) =>
            gold > 0 && plat > 0 ? (gold * qty).ToString("N0") + " " + GameConstants.CurrencyName
                                   + " + " + (plat * qty).ToString("N0") + " " + GameConstants.PlatinumName
          : plat > 0 ? (plat * qty).ToString("N0") + " " + GameConstants.PlatinumName
          : (gold * qty).ToString("N0") + " " + GameConstants.CurrencyName;

        /// <summary>`BL-272` part 2 — an essence price tag: "750 Cobalt Essence + 6,750 Darksteel Essence".</summary>
        private static string EssencePrice(ItemCostDto[] cost) =>
            string.Join(" + ", Array.ConvertAll(cost, c => c.Qty.ToString("N0") + " " + c.Name));

        /// <summary>How many of an item the BAG holds, all rows — the reach of the server's CountItem.</summary>
        private int HeldCount(string defId)
        {
            int n = 0;
            foreach (var it in Boot.Inventory ?? Array.Empty<InventoryItemDto>())
                if (it.DefId == defId) n += it.Quantity;
            return n;
        }

        /// <summary>Can this many be paid for out of BOTH wallets?</summary>
        private bool CanAfford(long gold, long plat, int qty = 1) =>
            Boot.Gold >= gold * qty && Boot.Platinum >= plat * qty;

        /// <summary>`BL-290` — build the tab strip this view needs, if it is not the one already built: the shop's
        /// own tabs (with "All" first) on the buy side of a shop that has them, the generic four otherwise.
        /// The width shrinks to fit a long strip in the row, which the buy side has to itself (Instant sale
        /// is hidden there).</summary>
        private void EnsureVendorTabs()
        {
            string shopId = Boot.Dialog?.Shop?.ShopId ?? "";
            var tabs = _vendorSell ? null : ShopCatalog.Get(shopId)?.Tabs;
            if (shopId != _vendorShopTabOf) { _vendorShopTabOf = shopId; _vendorShopTab = 0; }
            string key = tabs == null ? "generic" : "shop:" + shopId;
            if (key == _vendorStripKey) return;
            _vendorStripKey = key;

            if (_vendorTabButtons != null)
                foreach (var b in _vendorTabButtons)
                    if (b != null) Destroy(b.gameObject);
            _vendorShopTabs = tabs;
            var at = new Vector2(18f, -_vendorChrome - 66f);
            float width;
            int count;
            if (tabs == null)
            {
                width = 88f;
                count = VendorTabs.Length;
                _vendorTabButtons = BuildCategoryTabs(_vendorInner, VendorTabs, at, width,
                                                      cat => { _vendorTab = cat; _vendorRevision = -1; });
            }
            else
            {
                count = tabs.Length + 1;
                width = Mathf.Min(88f, (VendorRowWidth - VendorOrderWidth - 4f) / count - 2f);
                float font = width < 70f ? 12f : 14f;
                _vendorTabButtons = new Button[count];
                for (int i = 0; i < count; i++)
                {
                    int tab = i;
                    var b = UiKit.TextButton(_vendorInner, i == 0 ? "All" : tabs[i - 1].Name,
                        () => { _vendorShopTab = tab; _vendorRevision = -1; }, font);
                    UiKit.Place(UiKit.Rect(b.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                                new Vector2(at.x + i * (width + 2f), at.y), new Vector2(width, 32f));
                    _vendorTabButtons[i] = b;
                }
            }
            if (_vendorShopTab >= count) _vendorShopTab = 0;
            UiKit.Place(UiKit.Rect(_vendorOrderButton.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(at.x + count * (width + 2f), at.y), new Vector2(VendorOrderWidth, 32f));
        }

        /// <summary>Is this ware under the buy tab that is showing? (`BL-290`)</summary>
        private bool InBuyTab(ItemDef def) =>
            _vendorShopTabs == null ? InCategory(_vendorTab, def)
            : _vendorShopTab == 0 || ShopCatalog.InTab(_vendorShopTabs[_vendorShopTab - 1], def);

        private void BuildBuyList()
        {
            var shop = Boot.Dialog?.Shop;
            if (shop?.Items == null || shop.Items.Length == 0)
            {
                VendorNote("This vendor has nothing to sell.");
                return;
            }

            bool anyInTab = false;
            foreach (var ware in ByOrder(shop.Items))   // `BL-117`: the same cycle as the bag's
            {
                var def = ItemCatalog.Get(ware.DefId);
                if (def == null || !InBuyTab(def)) continue;
                anyInTab = true;
                // `BL-272` part 2 — an ESSENCE row (the T52 essence shop): no gold, one piece per purchase.
                if (ware.Essence is { Length: > 0 } essence)
                {
                    string esPrice = EssencePrice(essence);
                    bool esAfford = Array.TrueForAll(essence, c => HeldCount(c.DefId) >= c.Qty);
                    string esDefId = ware.DefId, esName = ware.Name;
                    string esHead = (esAfford ? Coloured(esName, def.Rarity) : esName) + "   " + esPrice;
                    VendorRow(_vendorDetailed ? esHead + "\n<size=12><color=#9AA3AD>" + WareSummary(def) + "</color></size>"
                                              : esHead,
                              esAfford ? UiKit.Text : UiKit.TextDim,
                              () => OpenWareDetails(esDefId, "Buy  " + esPrice, () => Boot.BuyItem(esDefId, 1)),
                              _vendorDetailed ? 56f : 38f);
                    continue;
                }
                long unit = Math.Max(0, ware.BuyPrice);   // -1 = no GOLD price, not "unbuyable"
                long unitPlat = ware.PlatinumPrice;
                bool afford = CanAfford(unit, unitPlat);
                string defId = ware.DefId;
                string name = ware.Name;

                // Quality reads off the COLOUR now, not the name — the shop stocks the same piece at
                // Common/Uncommon/Rare, so without it three identical-looking rows differ only in price.
                // ⚠ Only colour it when you can AFFORD it. TMP's <color> markup overrides the label's
                // own colour for that span, so a coloured name ignored the dimming that says "you can't
                // buy this" — the quality cue was quietly cancelling the affordability cue.
                string head = (afford ? Coloured(name, def.Rarity) : name)
                              + "   " + Price(unit, unitPlat);
                // DETAIL view adds a second line saying WHAT the thing is (owner: "i hve no idea which
                // is which"). Compact view is the old one-line row, for scrolling a long ladder fast.
                string label = _vendorDetailed ? head + "\n<size=12><color=#9AA3AD>" + WareSummary(def) + "</color></size>"
                                               : head;
                VendorRow(label, afford ? UiKit.Text : UiKit.TextDim,
                          () => BuyTap(defId, name, def, unit, unitPlat), _vendorDetailed ? 56f : 38f);
            }
            if (!anyInTab) VendorNote("Nothing in this category here — try All.");
        }

        private void BuildSellList(InventoryItemDto[] items)
        {
            bool any = false;
            foreach (var item in ByName(items))          // C8: name order and the category tabs
            {
                var def = ItemCatalog.Get(item.DefId);
                if (def == null || item.Equipped) continue;
                // ⚠ ASK THE INSTANCE, not the def. `HandleSell` gates on `item.Sellable(def)` and pays
                // `item.SellPrice(def)` — both of which read the per-instance overrides — so a list
                // built off `ItemCatalog` alone could offer a row the server refuses, or quote a price
                // it will not pay. `ItemTag` is the one implementation both sides share.
                if (!ItemTag.Sellable(def, item.SellPriceOverride, item.TradableOverride)) continue;
                // `BL-239` — *"a lock on items NOT TO SHOW IN SELL WINDOW"*. Gone entirely rather than
                // greyed: the sell list is a list of what you are offering, and a row you cannot pick
                // is only a row to scroll past.
                if (Boot.IsLocked(def, item)) continue;
                if (!InCategory(_vendorTab, def)) continue;
                any = true;

                long unit = ItemTag.SellPrice(def, item.SellPriceOverride);
                // `BL-242` — THE ENCHANT IS ON THE ROW. *"sale list don't show enchant value"*: a +6
                // and a +0 of the same piece were two identical lines in the one window where you part
                // with them for good. The bag has shown "+N " forever; the sell list simply never did.
                string shownName = (item.Enchant > 0 ? "+" + item.Enchant + " " : "") + ItemTag.RowName(def, item);
                string head = Coloured(shownName, def.Rarity) + (item.Quantity > 1 ? "   x" + item.Quantity : "")
                            + "   " + unit.ToString("N0") + " " + GameConstants.CurrencyName + " ea";
                // The SELL side gets the same second line the buy side has — "the details on the row"
                // is what replaced the details dialog, so it has to be on both or selling still needs it.
                //
                // `BL-242`, second half: *"in the description of the sell item row should show the
                // attributes if any"*. They are appended to that line rather than given one of their
                // own — a row that grows a third line only for enchanted gear makes the list ragged,
                // and an attribute is a property of the piece exactly like its summary is.
                string label = _vendorDetailed
                    ? head + "\n<size=12><color=#9AA3AD>" + WareSummary(def) + AttributeSuffix(item) + "</color></size>"
                    : head;
                var captured = item;
                VendorRow(label, UiKit.Text, () => SellTap(captured, def, unit),
                          _vendorDetailed ? 56f : 44f);
            }
            if (!any) VendorNote(_vendorTab == ItemCategory.All
                ? "Nothing here can be sold (equipped or bound items can't)."
                : "Nothing sellable in this category — try All.");
        }

        /// <summary>`BL-242` — the attributes an INSTANCE carries, as a short tail for its summary
        /// line: <c>  ·  Crit rate +12%, P.Atk +40</c>. Empty when the piece has none, so an ordinary
        /// row is unchanged.
        ///
        /// <para>It reads <see cref="AttributeSystem"/> for the name and the percent flag rather than
        /// formatting them here — the item-details window and the target inspector already do, and
        /// three spellings of the same attribute is exactly the drift the shared catalog exists to
        /// prevent.</para></summary>
        private static string AttributeSuffix(InventoryItemDto item)
        {
            if (item.Attributes == null || item.Attributes.Length == 0) return "";
            var t = new StringBuilder("  ·  ");
            for (int i = 0; i < item.Attributes.Length; i++)
            {
                var a = item.Attributes[i];
                if (i > 0) t.Append(", ");
                t.Append(AttributeSystem.DisplayName(a.Type)).Append(" +").Append(a.Value);
                if (AttributeSystem.IsPercent(a.Type)) t.Append('%');
            }
            return t.ToString();
        }

        // ═══ `BL-291` — A ROW OPENS THE ITEM'S DETAILS FIRST (owner, 2026-09-25) ═══════════════════
        //
        // *"the mythic body armors in vendors need to show the set effect .. before buy/sell (if not quick
        // is enabled) open a details panel for that item .. then there should be a buy button .. for
        // potions will open it details pannel then a buy button will open the numpad"*.
        //
        // The panel is the bag's own item window (OpenVendorItemDetails), so a ware reads exactly as
        // it will once it is yours: stats, description, the SET and what it does. The vendor's old
        // confirm dialog printed only the stat block, which is why a Mythic body armour never said.
        //
        // 🔑 STILL ONE CONFIRMATION PER PURCHASE (playtest-16: a details dialog in front of the numpad
        //    and a confirm behind it was *"like a double confirmation"*). The PANEL is now the
        //    confirmation: Buy on a single piece buys it, and for a stack Buy opens the numpad, which
        //    names the quantity and the total. Nothing is asked twice. QSell still skips all of it.

        /// <summary>Open a SHOP WARE in the item window with Buy + Cancel. It has no instance, so it is
        /// drawn from a stand-in DTO (quantity 1, no enchant, no attributes).</summary>
        private void OpenWareDetails(string defId, string buyLabel, Action buy)
        {
            var ware = new InventoryItemDto(Guid.Empty, defId, false, 0, 1, null);
            OpenVendorItemDetails(ware, track: false, _ => new List<(string Label, Action Click)>
            {
                (buyLabel, () => { CloseAllItemViews(); buy(); }),
                ("Cancel", CloseAllItemViews),
            });
        }

        private void BuyTap(string defId, string name, ItemDef def, long unit, long unitPlat) =>
            OpenWareDetails(defId, "Buy  " + Price(unit, unitPlat),
                IsStackable(def) ? () => BuyQuantity(defId, name, def, unit, unitPlat)
                                 : () => Boot.BuyItem(defId, 1));

        /// <summary>The numpad half of buying a STACKABLE — reached from the details panel's Buy.</summary>
        private void BuyQuantity(string defId, string name, ItemDef def, long unit, long unitPlat)
        {

            // Max = the most you can AFFORD, clamped to ONE STACK — the server's own rule since 0.93.0
            // (*"max shop buy = 1 stack"*), so the cap here is the item's, not a hard-coded 999: mana
            // potions still buy 999 at a time and buff scrolls buy 9. Reading `def.MaxStack` rather
            // than repeating a number is what keeps the numpad and `HandleBuy` from drifting — the
            // clamp is never a refusal, so a mismatch would silently truncate an order instead of
            // erroring.
            int cap = def.MaxStack;
            // `BL-257` — the SMALLER of what each wallet allows. A half that is not charged does not
            // limit anything, which is why each side only narrows the cap when its price is real.
            int affordable = cap;
            if (unit > 0) affordable = (int)Math.Min(affordable, Boot.Gold / unit);
            if (unitPlat > 0) affordable = (int)Math.Min(affordable, Boot.Platinum / unitPlat);
            OpenNumpad("Buy " + name, Mathf.Max(1, affordable), "Buy",
                       qty => { Boot.BuyItem(defId, qty); CloseNumpad(); },
                       qty => qty + " x " + Price(unit, unitPlat) + " = " + Price(unit, unitPlat, qty)
                              + "   (you have " + Wallet() + ")");
        }

        // ═══ `BL-240` — INSTANT SALE ═══════════════════════════════════════════════════════════════
        //
        // *"u click on button inside the vendor sell tab and it shows rarity to instant sell -> it sells
        // everitying of that rarity depending on the tab you are on"*. Two taps: pick the rarity, then
        // confirm. The rarity list is BUILT FROM THE BAG, so a rung you own nothing of is not offered —
        // and each row already names what it will take and what it pays, which is what makes the
        // confirmation a check rather than a guess.
        //
        // ⚠ THE COUNT AND THE GOLD HERE ARE THE CLIENT'S ARITHMETIC over the same `ItemTag` predicates
        // the server uses, so they agree — but the server re-derives both, and its number is the one
        // that lands. A stack looted between the popup and the tap makes the sale slightly larger than
        // the quote, never smaller in a way that surprises you.

        private void BeginInstantSell()
        {
            var options = new List<(string Label, Action OnPick)>();

            for (int r = 0; r <= (int)ItemRarity.Mythic; r++)
            {
                var rarity = (ItemRarity)r;
                int rows = 0, units = 0;
                long gold = 0;
                foreach (var item in Boot.Inventory ?? Array.Empty<InventoryItemDto>())
                {
                    if (item.Equipped) continue;
                    var def = ItemCatalog.Get(item.DefId);
                    if (def == null || def.Rarity != rarity) continue;
                    if (!ItemCatalog.InCategory(_vendorTab, def)) continue;
                    if (Boot.IsLocked(def, item)) continue;                       // `BL-239`
                    if (!ItemTag.Sellable(def, item.SellPriceOverride, item.TradableOverride)) continue;
                    int qty = Mathf.Max(1, item.Quantity);
                    rows++;
                    units += qty;
                    gold += ItemTag.SellPrice(def, item.SellPriceOverride) * qty;
                }
                if (rows == 0) continue;

                var picked = rarity;
                long quoted = gold;
                int pickedRows = rows, pickedUnits = units;
                options.Add((Coloured(rarity.ToString(), rarity)
                             + "   " + pickedRows + (pickedUnits > pickedRows ? " stacks" : " items")
                             + "   " + quoted.ToString("N0") + " " + GameConstants.CurrencyName,
                    () =>
                    {
                        CloseWindow(_selectPopup);
                        Ask($"Sell {pickedRows} {picked} {InstantSellTabWord(_vendorTab)}"
                            + (pickedUnits > pickedRows ? $" ({pickedUnits} items)" : "")
                            + $" for {quoted:N0} {GameConstants.CurrencyName}?"
                            + "\n\n<size=15>Buy-back holds the last "
                            + GameConstants.BuyBackSlots + " sales, so a long sweep can push the "
                            + "earliest ones off the shelf.</size>",
                            "Sell all",
                            () => Boot.InstantSell(_vendorTab, picked));
                    }));
            }

            if (options.Count == 0)
            {
                ClientLog.Info("Nothing sellable on this tab. (Locked and equipped items are never swept.)");
                return;
            }

            ShowSelection("Instant sale — " + CategoryLabel(_vendorTab), options.ToArray());
        }

        /// <summary>The word a tab goes by in the confirmation. Deliberately the same four words the
        /// server's own message uses, so the question and the answer read as one sentence.</summary>
        private static string InstantSellTabWord(ItemCategory c) => c switch
        {
            ItemCategory.Gear => "gear",
            ItemCategory.Use  => "consumables",
            ItemCategory.Mats => "materials",
            _                 => "items",
        };

        private void SellTap(InventoryItemDto item, ItemDef def, long unit)
        {
            var id = item.InstanceId;

            // QSell (V1): the whole stack, straight out, no question asked. Max is the point — asking
            // "how many?" for the twelfth pile of trash is the friction he named. It deliberately
            // covers the non-stacking case too: a single sword is one tap either way, and a toggle
            // that still popped a dialog for half the rows would not be a quick-sell.
            if (_vendorQuickSell)
            {
                int qty = Mathf.Max(1, item.Quantity);
                Boot.SellItem(id, qty);
                return;
            }

            // `BL-242` — the enchant rides into the CONFIRM too. The row is where you notice it, but
            // the dialog is where you commit, and a confirm that says plain "Electrum Blade" after a
            // row that said "+6 Electrum Blade" is the one place the warning could still be missed.
            string named = (item.Enchant > 0 ? "+" + item.Enchant + " " : "") + def.Name;

            // `BL-291` — the real instance goes in, so its enchant and ATTRIBUTE ROLLS show, and the
            // window follows it (tracked) and closes itself once it has been sold. Each redraw is handed
            // the fresh DTO, so a stack that shrank prices the numpad off what is actually left.
            OpenVendorItemDetails(item, track: true, cur =>
            {
                string sellLabel = "Sell  " + unit.ToString("N0") + " " + GameConstants.CurrencyName
                                 + (cur.Quantity > 1 ? " ea" : "");
                Action sell = !IsStackable(def) || cur.Quantity <= 1
                    ? () => Boot.SellItem(id, 1)
                    : () => OpenNumpad("Sell " + named, cur.Quantity, "Sell",
                                       qty => { Boot.SellItem(id, qty); CloseNumpad(); },
                                       qty => qty + " of " + cur.Quantity + "   you get "
                                              + (unit * qty).ToString("N0") + " " + GameConstants.CurrencyName);
                return new List<(string Label, Action Click)>
                {
                    (sellLabel, () => { CloseAllItemViews(); sell(); }),
                    ("Cancel", CloseAllItemViews),
                };
            });
        }

        // Stackable = the server's rule for what a quantity even means (Consumable / Scroll).
        // Asks the SHARED def (ItemDef.IsStackable) rather than re-listing the slots here. The local
        // copy used to omit Material, so crafting mats had no quantity numpad and sold one at a time
        // while the server happily stacked them.
        private static bool IsStackable(ItemDef def) => def.IsStackable;

        private void VendorNote(string text)
        {
            var label = UiKit.Label(_vendorList, text, 16f, UiKit.TextDim);
            label.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
        }

        private void VendorRow(string text, Color colour, Action onTap, float height = 44f)
        {
            var button = UiKit.TextButton(_vendorList, text, onTap, 16f);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) { label.alignment = TextAlignmentOptions.Left; label.color = colour; }
            button.gameObject.AddComponent<LayoutElement>().minHeight = height;
        }

        /// <summary>The one-line "what IS this" a shopper needs before tapping: quality, grade, type,
        /// then the stats that matter — and for a consumable, what USING it does.
        ///
        /// This row is now the ONLY description before the numpad (the details dialog in front of it
        /// was the double confirmation, playtest-16), so it carries the consumable's effect too: a
        /// potion whose row says "Rare F-grade Potion" and nothing else is the case that sent people
        /// looking for a second window in the first place. Stats read "Atk 12, M.Atk 3" rather than
        /// column-spaced, so the eye can take the line in one pass.</summary>
        private static string WareSummary(ItemDef def)
        {
            var t = new StringBuilder();
            if (ItemCatalog.RarityLabel(def) is { Length: > 0 } rw) t.Append(rw).Append(' ');   // Mythic gear is plain (`BL-272`)
            // `BL-289`: a grade only where one exists (gear, or a box of gear); the rest says so plainly.
            string grade = ItemCatalog.GradeLabel(def);
            if (grade != ItemCatalog.NoGrade) t.Append(grade).Append("-grade ");
            t.Append(TypeLine(def));
            if (grade == ItemCatalog.NoGrade) t.Append(" (grade -)");

            var stats = new List<string>();
            if (def.AtkBonus > 0)  stats.Add("Atk " + def.AtkBonus);
            if (def.MAtkBonus > 0) stats.Add("M.Atk " + def.MAtkBonus);
            if (def.DefBonus > 0)  stats.Add("Def " + def.DefBonus);
            if (def.MDefBonus > 0) stats.Add("M.Def " + def.MDefBonus);
            if (def.HpBonus > 0)   stats.Add("HP +" + def.HpBonus);
            if (def.MpBonus > 0)   stats.Add("MP +" + def.MpBonus);
            if (stats.Count > 0) t.Append(" — ").Append(string.Join(", ", stats));

            // A consumable's whole value is its skill, so say what it does right here.
            if (SkillCatalog.Get(def.UseSkillId) is SkillDef use && !string.IsNullOrWhiteSpace(use.Description))
                t.Append(" — ").Append(use.Description);
            return t.ToString();
        }

        // ----- numpad ----------------------------------------------------------------------------

        private void BuildNumpad()
        {
            _numpadPanel = UiKit.PanelBox(_worldRoot, "Numpad");
            UiKit.Place(_numpadPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(360f, 480f));
            var inner = _numpadPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_numpadPanel, "Quantity", CloseNumpad);

            // Two lines: what you are ordering, and what it will cost at the quantity showing.
            _numpadTitle = UiKit.Label(inner, "", 16f, UiKit.Text, TextAlignmentOptions.Center);
            UiKit.Place(UiKit.Rect(_numpadTitle.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -chrome - 4f), new Vector2(320f, 44f));

            _numpadInput = UiKit.InputField(inner, "1", false, 22f);
            _numpadInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _numpadInput.onValueChanged.AddListener(OnNumpadTyped);
            UiKit.Place(UiKit.Rect(_numpadInput.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -chrome - 56f), new Vector2(320f, 46f));

            // 3-column pad: 1-9, then C 0 <.
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "<" };
            const float bw = 96f, bh = 54f, gap = 8f;
            float x0 = -(bw + gap), y0 = -chrome - 114f;
            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                int col = i % 3, rowIdx = i / 3;
                var button = UiKit.TextButton(inner, k, () => NumpadKey(k), 20f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(x0 + col * (bw + gap), y0 - rowIdx * (bh + gap)), new Vector2(bw, bh));
            }

            var max = UiKit.TextButton(inner, "Max", NumpadMaxOrMode, 16f);
            _numpadMaxButton = max;
            UiKit.Place(UiKit.Rect(max.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(18f, 16f), new Vector2(100f, 46f));

            _numpadOkButton = UiKit.TextButton(inner, "OK", NumpadConfirm, 17f);
            UiKit.Place(UiKit.Rect(_numpadOkButton.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 16f), new Vector2(110f, 46f));

            // X closes the numpad for THIS item (back to the list) — NOT the whole vendor.
            var close = UiKit.TextButton(inner, "X", CloseNumpad, 17f);
            UiKit.Place(UiKit.Rect(close.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-18f, 16f), new Vector2(100f, 46f));

            _numpadPanel.gameObject.SetActive(false);
        }

        /// <summary>Open the quantity pad. <paramref name="okLabel"/> names what the button will DO
        /// ("Buy"/"Sell"), because this pad is now the confirmation itself — an "OK" that silently
        /// spends gold is exactly the ambiguity the removed confirm dialog used to cover.
        /// <paramref name="summary"/> re-renders under the title on every keystroke, so the total is
        /// on screen at the moment you press the button.</summary>
        /// <param name="initial">What the box opens showing (a delay opens on the one already set).</param>
        /// <param name="modeLabel">`BL-279` — when given, the Max button becomes a MODE switch showing this
        /// label and calling <paramref name="modeTap"/>; null keeps it "Max".</param>
        private void OpenNumpad(string title, int max, string okLabel, Action<int> onOk,
                                Func<int, string> summary = null, int initial = 1,
                                Func<string> modeLabel = null, Action modeTap = null)
        {
            _numpadModeLabel = modeTap != null ? modeLabel : null;
            _numpadModeTap = modeTap;
            UiKit.SetButtonText(_numpadMaxButton, _numpadModeLabel != null ? _numpadModeLabel() : "Max");
            _numpadMax = Mathf.Max(1, max);
            _numpadOk = onOk;
            _numpadHead = title + "   (max " + _numpadMax + ")";
            _numpadSummary = summary;
            UiKit.SetButtonText(_numpadOkButton, okLabel);
            _numpadInput.SetTextWithoutNotify(Mathf.Clamp(initial, 1, _numpadMax).ToString());
            RefreshNumpadTitle();
            OpenWindow(_numpadPanel);
        }

        private void NumpadMaxOrMode()
        {
            if (_numpadModeTap == null) { _numpadInput.text = _numpadMax.ToString(); return; }
            _numpadModeTap();
            if (_numpadModeLabel != null) UiKit.SetButtonText(_numpadMaxButton, _numpadModeLabel());
            RefreshNumpadTitle();   // the summary line reads the mode too
        }

        private int NumpadQty() =>
            int.TryParse(_numpadInput.text, out int v) ? Mathf.Clamp(v, 1, _numpadMax) : 1;

        private void RefreshNumpadTitle() =>
            _numpadTitle.text = _numpadSummary == null
                ? _numpadHead
                : _numpadHead + "\n<size=14><color=#9AA3AD>" + _numpadSummary(NumpadQty()) + "</color></size>";

        private void CloseNumpad() => CloseWindow(_numpadPanel);

        private void NumpadKey(string k)
        {
            if (k == "C") { _numpadInput.text = "0"; return; }
            if (k == "<")
            {
                var t = _numpadInput.text;
                _numpadInput.text = t.Length > 1 ? t.Substring(0, t.Length - 1) : "0";
                return;
            }
            // digit
            _numpadInput.text = _numpadInput.text == "0" ? k : _numpadInput.text + k;
        }

        /// <summary>Keep the box within the max as it is typed (buttons OR the phone keyboard), so the
        /// confirm can only ever offer a legal quantity. Min is enforced at OK, not here, so the field
        /// can be empty mid-edit.</summary>
        private void OnNumpadTyped(string s)
        {
            if (int.TryParse(s, out int v) && v > _numpadMax)
                _numpadInput.SetTextWithoutNotify(_numpadMax.ToString());
            RefreshNumpadTitle();   // the total is part of the question, so it follows every keystroke
        }

        private void NumpadConfirm() => _numpadOk?.Invoke(NumpadQty());
    }
}
