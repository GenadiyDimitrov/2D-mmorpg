using System;
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
    /// afford when buying — so Max can never order a refusal). A non-stacking item skips the numpad. In
    /// both cases the last step is a plain-text confirm, so nothing leaves your purse by a single tap.
    ///
    /// The server owns the transaction and every price: the buy price rides the shop DTO, the sell
    /// price is the shared ItemCatalog formula, and the server re-checks gold, stock and sellability —
    /// this window only gathers "which item, how many" and asks for confirmation.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _vendorPanel, _vendorList;
        private TextMeshProUGUI _vendorTitle;
        private Button _vendorBuyTab, _vendorSellTab, _vendorViewTab;
        private bool _vendorSell;
        /// <summary>Detail view = two lines per row (name+price, then what it IS). Remembered across
        /// vendors because it is a preference, not a per-shop mode.</summary>
        private bool _vendorDetailed = true;
        private int _vendorRevision = -1;

        // numpad
        private RectTransform _numpadPanel;
        private TextMeshProUGUI _numpadTitle;
        private TMP_InputField _numpadInput;
        private int _numpadMax = 1;
        private Action<int> _numpadOk;

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

            // COMPACT vs DETAIL. Compact is one line per item for scrolling a long ladder; detail adds
            // a second line naming the type, grade, quality and the stat that matters (owner asked for
            // a button that switches between "list-rows" and rows carrying their description).
            _vendorViewTab = UiKit.TextButton(inner, "Detail", ToggleVendorView, 15f);
            UiKit.Place(UiKit.Rect(_vendorViewTab.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-18f, -chrome - 32f), new Vector2(120f, 30f));

            ScrollRect scroll;
            _vendorList = UiKit.ScrollArea(inner, out scroll, 3f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 68f, 16f, 16f);

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
                         + (int)(Boot.Gold % 1_000_000);
            revision = revision * 31 + (Boot.Dialog?.Shop?.Items?.Length ?? 0);
            foreach (var it in items) revision = revision * 31 + it.Quantity + (it.Equipped ? 7 : 0);
            if (revision == _vendorRevision) return;
            _vendorRevision = revision;

            _vendorTitle.text = _vendorSell ? "Sell — pick an item from your bag"
                                            : "Buy — you have " + Boot.Gold.ToString("N0") + " " + GameConstants.CurrencyName;
            _vendorBuyTab.targetGraphic.color = _vendorSell ? UiKit.PanelLight : UiKit.TabActive;
            _vendorSellTab.targetGraphic.color = _vendorSell ? UiKit.TabActive : UiKit.PanelLight;
            UiKit.SetButtonText(_vendorViewTab, _vendorDetailed ? "Compact" : "Detail");
            _vendorViewTab.targetGraphic.color = _vendorDetailed ? UiKit.TabActive : UiKit.PanelLight;

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

            foreach (var ware in shop.Items)
            {
                var def = ItemCatalog.Get(ware.DefId);
                if (def == null) continue;
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
        }

        private void BuildSellList(InventoryItemDto[] items)
        {
            bool any = false;
            foreach (var item in items)
            {
                var def = ItemCatalog.Get(item.DefId);
                if (def == null || item.Equipped || !ItemCatalog.IsSellable(def)) continue;
                any = true;

                long unit = ItemCatalog.SellPrice(def);
                string label = Coloured(def.Name, def.Rarity) + (item.Quantity > 1 ? "   x" + item.Quantity : "")
                             + "   " + unit.ToString("N0") + " " + GameConstants.CurrencyName + " ea";
                var captured = item;
                VendorRow(label, UiKit.Text, () => SellTap(captured, def, unit));
            }
            if (!any) VendorNote("Nothing here can be sold (equipped or bound items can't).");
        }

        private void BuyTap(string defId, string name, ItemDef def, long unit)
        {
            if (IsStackable(def))
            {
                // Max = the most you can AFFORD, clamped to the server's 999 cap — never a refusal.
                int affordable = unit > 0 ? (int)Math.Min(999, Boot.Gold / unit) : 999;
                OpenNumpad("Buy " + name, Mathf.Max(1, affordable),
                           qty => ConfirmBuy(defId, name, unit, qty));
            }
            else ConfirmBuy(defId, name, unit, 1);
        }

        private void SellTap(InventoryItemDto item, ItemDef def, long unit)
        {
            var id = item.InstanceId;
            if (IsStackable(def) && item.Quantity > 1)
                OpenNumpad("Sell " + def.Name, item.Quantity, qty => ConfirmSell(id, def.Name, unit, qty));
            else
                ConfirmSell(id, def.Name, unit, 1);
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
            if (def != null)
            {
                t.AppendLine().AppendLine();
                t.AppendLine(ItemStatsText(def, new InventoryItemDto(Guid.Empty, defId, false, 0, 1, null)).TrimEnd());
                if (!string.IsNullOrWhiteSpace(def.Description))
                    t.AppendLine().Append("<size=13><color=#9AA3AD>").Append(def.Description).Append("</color></size>");
            }
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

        /// <summary>The one-line "what IS this" a shopper needs before tapping: type, grade, quality and
        /// the stat that matters for the slot. The full sheet is in the confirm dialog.</summary>
        private static string WareSummary(ItemDef def)
        {
            var t = new StringBuilder();
            t.Append(def.Rarity).Append("  ")
             .Append(def.ItemLevel > 0 ? ItemCatalog.TierLetter(def.ItemLevel) : def.Grade.ToString())
             .Append("-grade  ").Append(TypeLine(def));
            if (def.AtkBonus > 0)  t.Append("   Atk ").Append(def.AtkBonus);
            if (def.MAtkBonus > 0) t.Append("   M.Atk ").Append(def.MAtkBonus);
            if (def.DefBonus > 0)  t.Append("   Def ").Append(def.DefBonus);
            if (def.MDefBonus > 0) t.Append("   M.Def ").Append(def.MDefBonus);
            if (def.HpBonus > 0)   t.Append("   HP +").Append(def.HpBonus);
            if (def.MpBonus > 0)   t.Append("   MP +").Append(def.MpBonus);
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

            _numpadTitle = UiKit.Label(inner, "", 16f, UiKit.Text, TextAlignmentOptions.Center);
            UiKit.Place(UiKit.Rect(_numpadTitle.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -chrome - 8f), new Vector2(320f, 24f));

            _numpadInput = UiKit.InputField(inner, "1", false, 22f);
            _numpadInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _numpadInput.onValueChanged.AddListener(OnNumpadTyped);
            UiKit.Place(UiKit.Rect(_numpadInput.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -chrome - 40f), new Vector2(320f, 46f));

            // 3-column pad: 1-9, then C 0 <.
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "<" };
            const float bw = 96f, bh = 54f, gap = 8f;
            float x0 = -(bw + gap), y0 = -chrome - 100f;
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

            var ok = UiKit.TextButton(inner, "OK", NumpadConfirm, 17f);
            UiKit.Place(UiKit.Rect(ok.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 16f), new Vector2(110f, 46f));

            // X closes the numpad for THIS item (back to the list) — NOT the whole vendor.
            var close = UiKit.TextButton(inner, "X", CloseNumpad, 17f);
            UiKit.Place(UiKit.Rect(close.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-18f, 16f), new Vector2(100f, 46f));

            _numpadPanel.gameObject.SetActive(false);
        }

        private void OpenNumpad(string title, int max, Action<int> onOk)
        {
            _numpadMax = Mathf.Max(1, max);
            _numpadOk = onOk;
            _numpadTitle.text = title + "   (max " + _numpadMax + ")";
            _numpadInput.SetTextWithoutNotify("1");
            OpenWindow(_numpadPanel);
        }

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
        }

        private void NumpadConfirm()
        {
            int qty = int.TryParse(_numpadInput.text, out int v) ? Mathf.Clamp(v, 1, _numpadMax) : 1;
            _numpadOk?.Invoke(qty);
        }
    }
}
