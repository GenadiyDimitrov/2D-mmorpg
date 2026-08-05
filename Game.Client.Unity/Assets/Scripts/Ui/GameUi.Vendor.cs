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

        // numpad
        private RectTransform _numpadPanel;
        private TextMeshProUGUI _numpadTitle;
        private TMP_InputField _numpadInput;
        private Button _numpadOkButton;
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

            _vendorTabButtons = BuildCategoryTabs(inner, VendorTabs, new Vector2(18f, -chrome - 66f), 88f,
                                                  cat => { _vendorTab = cat; _vendorRevision = -1; });

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

            var items = Boot.Inventory ?? Array.Empty<InventoryItemDto>();
            int revision = (_vendorSell ? 1 : 0) * 92821 + (_vendorDetailed ? 7919 : 0)
                         + (_vendorQuickSell ? 15485863 : 0)
                         + (int)_vendorTab * 104729 + (int)(Boot.Gold % 1_000_000);
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
                : "Buy — you have " + Boot.Gold.ToString("N0") + " " + GameConstants.CurrencyName;
            _vendorBuyTab.targetGraphic.color = _vendorSell ? UiKit.PanelLight : UiKit.TabActive;
            _vendorSellTab.targetGraphic.color = _vendorSell ? UiKit.TabActive : UiKit.PanelLight;
            UiKit.SetButtonText(_vendorViewTab, _vendorDetailed ? "Compact" : "Detail");
            _vendorViewTab.targetGraphic.color = _vendorDetailed ? UiKit.TabActive : UiKit.PanelLight;
            _vendorQSellTab.gameObject.SetActive(_vendorSell);
            UiKit.SetButtonText(_vendorQSellTab, _vendorQuickSell ? "QSell: ON" : "QSell: off");
            _vendorQSellTab.targetGraphic.color = _vendorQuickSell
                ? new Color(0.42f, 0.20f, 0.20f, 0.95f)   // the bin's armed red — it skips the confirm too
                : UiKit.PanelLight;
            PaintCategoryTabs(_vendorTabButtons, VendorTabs, _vendorTab);

            for (int i = _vendorList.childCount - 1; i >= 0; i--)
                Destroy(_vendorList.GetChild(i).gameObject);

            if (_vendorSell) BuildSellList(items);
            else BuildBuyList();
        }

        private void BuildBuyList()
        {
            var shop = Boot.Dialog?.Shop;
            if (shop?.Items == null || shop.Items.Length == 0)
            {
                VendorNote("This vendor has nothing to sell.");
                return;
            }

            bool anyInTab = false;
            foreach (var ware in shop.Items)
            {
                var def = ItemCatalog.Get(ware.DefId);
                if (def == null || !InCategory(_vendorTab, def)) continue;
                anyInTab = true;
                long unit = ware.BuyPrice;
                bool afford = Boot.Gold >= unit;
                string defId = ware.DefId;
                string name = ware.Name;

                // Quality reads off the COLOUR now, not the name — the shop stocks the same piece at
                // Common/Uncommon/Rare, so without it three identical-looking rows differ only in price.
                // ⚠ Only colour it when you can AFFORD it. TMP's <color> markup overrides the label's
                // own colour for that span, so a coloured name ignored the dimming that says "you can't
                // buy this" — the quality cue was quietly cancelling the affordability cue.
                string head = (afford ? Coloured(name, def.Rarity) : name)
                              + "   " + unit.ToString("N0") + " " + GameConstants.CurrencyName;
                // DETAIL view adds a second line saying WHAT the thing is (owner: "i hve no idea which
                // is which"). Compact view is the old one-line row, for scrolling a long ladder fast.
                string label = _vendorDetailed ? head + "\n<size=12><color=#9AA3AD>" + WareSummary(def) + "</color></size>"
                                               : head;
                VendorRow(label, afford ? UiKit.Text : UiKit.TextDim,
                          () => BuyTap(defId, name, def, unit), _vendorDetailed ? 56f : 38f);
            }
            if (!anyInTab) VendorNote("Nothing in this category here — try All.");
        }

        private void BuildSellList(InventoryItemDto[] items)
        {
            bool any = false;
            foreach (var item in ByName(items))          // C8: name order and the category tabs
            {
                var def = ItemCatalog.Get(item.DefId);
                if (def == null || item.Equipped || !ItemCatalog.IsSellable(def)) continue;
                if (!InCategory(_vendorTab, def)) continue;
                any = true;

                long unit = ItemCatalog.SellPrice(def);
                string head = Coloured(def.Name, def.Rarity) + (item.Quantity > 1 ? "   x" + item.Quantity : "")
                            + "   " + unit.ToString("N0") + " " + GameConstants.CurrencyName + " ea";
                // The SELL side gets the same second line the buy side has — "the details on the row"
                // is what replaced the details dialog, so it has to be on both or selling still needs it.
                string label = _vendorDetailed
                    ? head + "\n<size=12><color=#9AA3AD>" + WareSummary(def) + "</color></size>"
                    : head;
                var captured = item;
                VendorRow(label, UiKit.Text, () => SellTap(captured, def, unit),
                          _vendorDetailed ? 56f : 44f);
            }
            if (!any) VendorNote(_vendorTab == ItemCategory.All
                ? "Nothing here can be sold (equipped or bound items can't)."
                : "Nothing sellable in this category — try All.");
        }

        // ONE confirmation per purchase (owner, playtest-16). 32d put a details dialog in FRONT of the
        // numpad, and with the confirm behind it a stack cost three taps through three windows — he
        // called it "like a double confirmation… the details can be on the row, just better worded".
        //
        // So the details moved onto the ROW (detail view is on by default, buy AND sell), and for a
        // stackable **the numpad IS the confirmation**: it names the item, prices it, and its button
        // says what it will do. Nothing is asked twice. A NON-stackable still gets the one confirm
        // dialog — it has no numpad to carry the question, and it is a single step, not a second one.
        private void BuyTap(string defId, string name, ItemDef def, long unit)
        {
            if (!IsStackable(def)) { ConfirmBuy(defId, name, unit, 1); return; }

            // Max = the most you can AFFORD, clamped to the server's 999 cap — never a refusal.
            int affordable = unit > 0 ? (int)Math.Min(999, Boot.Gold / unit) : 999;
            OpenNumpad("Buy " + name, Mathf.Max(1, affordable), "Buy",
                       qty => { Boot.BuyItem(defId, qty); CloseNumpad(); },
                       qty => qty + " x " + unit.ToString("N0") + " = " + (unit * qty).ToString("N0")
                              + " " + GameConstants.CurrencyName
                              + "   (you have " + Boot.Gold.ToString("N0") + ")");
        }

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

            if (!IsStackable(def) || item.Quantity <= 1) { ConfirmSell(id, def.Name, unit, 1); return; }

            OpenNumpad("Sell " + def.Name, item.Quantity, "Sell",
                       qty => { Boot.SellItem(id, qty); CloseNumpad(); },
                       qty => qty + " of " + item.Quantity + "   you get "
                              + (unit * qty).ToString("N0") + " " + GameConstants.CurrencyName);
        }

        /// <summary>Append an item's stat block and description to a vendor message. Shared by the
        /// details step and the final confirm so the two can never describe the same item differently.
        /// The "Name:" row is dropped — the name is already in the line above it.</summary>
        private void AppendItemDetails(StringBuilder t, ItemDef def, string defId)
        {
            if (def == null) return;
            // Smaller than the question: the question is the decision, the stats are its evidence, and
            // at the dialog's 19px they ran the panel past the bottom of the screen.
            t.AppendLine().AppendLine().Append("<size=15>");
            string stats = ItemStatsText(def, new InventoryItemDto(Guid.Empty, defId, false, 0, 1, null));
            foreach (var line in stats.Split('\n'))
                if (!line.StartsWith("Name:")) t.AppendLine(line.TrimEnd());
            if (!string.IsNullOrWhiteSpace(def.Description))
                t.Append("<color=#9AA3AD>").Append(def.Description).Append("</color>");
            t.Append("</size>");
        }

        private void ConfirmBuy(string defId, string name, long unit, int qty)
        {
            // The confirm dialog is where the item DESCRIPTION belongs (owner, playtest-13: "clicking on
            // the item opens confirmation dialog with the items description"). It is the last moment
            // before the gold leaves, and the only place there is room to say what you are actually
            // buying — which matters far more now that a piece exists at three qualities and the name
            // no longer tells you which one you tapped.
            var def = ItemCatalog.Get(defId);
            var t = new StringBuilder();
            t.Append("Buy ").Append(qty).Append(" x ").Append(name)
             .Append(" for ").Append((unit * qty).ToString("N0")).Append(' ').Append(GameConstants.CurrencyName).Append('?');
            AppendItemDetails(t, def, defId);
            Ask(t.ToString(), "Confirm", () => { Boot.BuyItem(defId, qty); CloseNumpad(); });
        }

        private void ConfirmSell(Guid instanceId, string name, long unit, int qty)
        {
            Ask("Sell " + qty + " x " + name + " for " + (unit * qty).ToString("N0") + " " + GameConstants.CurrencyName + "?",
                "Confirm", () => { Boot.SellItem(instanceId, qty); CloseNumpad(); });
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
            t.Append(def.Rarity).Append(' ')
             .Append(def.ItemLevel > 0 ? ItemCatalog.TierLetter(def.ItemLevel) : def.Grade.ToString())
             .Append("-grade ").Append(TypeLine(def));

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

            var max = UiKit.TextButton(inner, "Max", () => _numpadInput.text = _numpadMax.ToString(), 16f);
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
        private void OpenNumpad(string title, int max, string okLabel, Action<int> onOk,
                                Func<int, string> summary = null)
        {
            _numpadMax = Mathf.Max(1, max);
            _numpadOk = onOk;
            _numpadHead = title + "   (max " + _numpadMax + ")";
            _numpadSummary = summary;
            UiKit.SetButtonText(_numpadOkButton, okLabel);
            _numpadInput.SetTextWithoutNotify("1");
            RefreshNumpadTitle();
            OpenWindow(_numpadPanel);
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
