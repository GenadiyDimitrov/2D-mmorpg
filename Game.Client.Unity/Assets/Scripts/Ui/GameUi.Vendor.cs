using System;
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
        private Button _vendorBuyTab, _vendorSellTab;
        private bool _vendorSell;
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

        /// <summary>Rebuild the list when the mode, gold, or inventory changed — so a sell removes the
        /// row it sold and a buy re-checks what you can still afford, both driven by the server's push.</summary>
        private void RefreshVendorWindow()
        {
            if (!_vendorPanel.gameObject.activeSelf) return;

            var items = Boot.Inventory ?? Array.Empty<InventoryItemDto>();
            int revision = (_vendorSell ? 1 : 0) * 92821 + (int)(Boot.Gold % 1_000_000);
            revision = revision * 31 + (Boot.Dialog?.Shop?.Items?.Length ?? 0);
            foreach (var it in items) revision = revision * 31 + it.Quantity + (it.Equipped ? 7 : 0);
            if (revision == _vendorRevision) return;
            _vendorRevision = revision;

            _vendorTitle.text = _vendorSell ? "Sell — pick an item from your bag"
                                            : "Buy — you have " + Boot.Gold.ToString("N0") + " " + GameConstants.CurrencyName;
            _vendorBuyTab.targetGraphic.color = _vendorSell ? UiKit.PanelLight : UiKit.TabActive;
            _vendorSellTab.targetGraphic.color = _vendorSell ? UiKit.TabActive : UiKit.PanelLight;

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

                VendorRow(name + "   " + unit.ToString("N0") + " " + GameConstants.CurrencyName,
                          afford ? UiKit.Text : UiKit.TextDim,
                          () => BuyTap(defId, name, def, unit));
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
                string label = def.Name + (item.Quantity > 1 ? "   x" + item.Quantity : "")
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
            Ask("Buy " + qty + " x " + name + " for " + (unit * qty).ToString("N0") + " " + GameConstants.CurrencyName + "?",
                "Confirm", () => { Boot.BuyItem(defId, qty); CloseNumpad(); });
        }

        private void ConfirmSell(Guid instanceId, string name, long unit, int qty)
        {
            Ask("Sell " + qty + " x " + name + " for " + (unit * qty).ToString("N0") + " " + GameConstants.CurrencyName + "?",
                "Confirm", () => { Boot.SellItem(instanceId, qty); CloseNumpad(); });
        }

        // Stackable = the server's rule for what a quantity even means (Consumable / Scroll).
        private static bool IsStackable(ItemDef def) =>
            def.Slot == EquipSlot.Consumable || def.Slot == EquipSlot.Scroll;

        private void VendorNote(string text)
        {
            var label = UiKit.Label(_vendorList, text, 16f, UiKit.TextDim);
            label.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
        }

        private void VendorRow(string text, Color colour, Action onTap)
        {
            var button = UiKit.TextButton(_vendorList, text, onTap, 16f);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) { label.alignment = TextAlignmentOptions.Left; label.color = colour; }
            button.gameObject.AddComponent<LayoutElement>().minHeight = 44f;
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
