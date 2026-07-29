using System;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the private WAREHOUSE (bank). Two tabs — DEPOSIT lists your bag (tap to stash),
    /// WITHDRAW lists the bank (tap to take back). Whole-item moves; the SERVER owns the transfer, gates it
    /// to town, and pushes the contents (Boot.Warehouse). Opened from a town NPC's dialog. Mirrors the
    /// vendor window's tab+list shape, without prices or a quantity numpad (a move is reversible).
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _warehousePanel, _warehouseList;
        private TextMeshProUGUI _warehouseTitle;
        private Button _warehouseDepositTab, _warehouseWithdrawTab;
        private bool _warehouseWithdraw;
        private int _warehouseRevision = -1;

        private void BuildWarehouseWindow()
        {
            _warehousePanel = UiKit.PanelBox(_worldRoot, "Warehouse");
            UiKit.Place(_warehousePanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(660f, 500f));
            var inner = _warehousePanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_warehousePanel, "Warehouse", () => CloseWindow(_warehousePanel));

            _warehouseTitle = UiKit.Label(inner, "", 17f, UiKit.TextDim, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_warehouseTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 6f), new Vector2(500f, 22f));

            _warehouseDepositTab = UiKit.TextButton(inner, "Deposit", () => SetWarehouseMode(false), 15f);
            UiKit.Place(UiKit.Rect(_warehouseDepositTab.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 32f), new Vector2(130f, 30f));
            _warehouseWithdrawTab = UiKit.TextButton(inner, "Withdraw", () => SetWarehouseMode(true), 15f);
            UiKit.Place(UiKit.Rect(_warehouseWithdrawTab.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(154f, -chrome - 32f), new Vector2(130f, 30f));

            ScrollRect scroll;
            _warehouseList = UiKit.ScrollArea(inner, out scroll, 3f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 68f, 16f, 16f);

            _warehousePanel.gameObject.SetActive(false);
        }

        /// <summary>Open the window; its contents arrive via Boot.OpenWarehouse → the "Warehouse" push.</summary>
        public void OpenWarehouseWindow()
        {
            _warehouseWithdraw = false;
            _warehouseRevision = -1;   // force a rebuild
            OpenWindow(_warehousePanel);
        }

        private void SetWarehouseMode(bool withdraw)
        {
            _warehouseWithdraw = withdraw;
            _warehouseRevision = -1;
        }

        /// <summary>Rebuild the list when the tab, bag or bank changed (revision-gated, like the vendor).</summary>
        private void RefreshWarehouseWindow()
        {
            if (!_warehousePanel.gameObject.activeSelf) return;

            var bag = Boot.Inventory ?? Array.Empty<InventoryItemDto>();
            var bank = Boot.Warehouse ?? Array.Empty<InventoryItemDto>();
            int revision = (_warehouseWithdraw ? 1 : 0) * 92821;
            foreach (var it in bag) revision = revision * 31 + it.InstanceId.GetHashCode() + it.Quantity + (it.Equipped ? 7 : 0);
            foreach (var it in bank) revision = revision * 31 + it.InstanceId.GetHashCode() * 7 + it.Quantity;
            if (revision == _warehouseRevision) return;
            _warehouseRevision = revision;

            _warehouseTitle.text = _warehouseWithdraw
                ? "Withdraw — tap an item to take it out"
                : "Deposit — tap a bag item to store it (town only)";
            _warehouseDepositTab.targetGraphic.color = _warehouseWithdraw ? UiKit.PanelLight : UiKit.TabActive;
            _warehouseWithdrawTab.targetGraphic.color = _warehouseWithdraw ? UiKit.TabActive : UiKit.PanelLight;

            for (int i = _warehouseList.childCount - 1; i >= 0; i--)
                Destroy(_warehouseList.GetChild(i).gameObject);

            BuildWarehouseList(_warehouseWithdraw ? bank : bag, _warehouseWithdraw);
        }

        private void BuildWarehouseList(InventoryItemDto[] items, bool withdraw)
        {
            bool any = false;
            foreach (var item in items)
            {
                if (!withdraw && item.Equipped) continue;   // worn gear isn't stashable (unequip it first)
                var def = ItemCatalog.Get(item.DefId);
                if (def == null) continue;
                any = true;

                string label = Coloured(def.Name, def.Rarity) + (item.Quantity > 1 ? "   x" + item.Quantity : "");
                var id = item.InstanceId;
                WarehouseRow(label, () =>
                {
                    if (withdraw) Boot.WarehouseWithdraw(id);
                    else Boot.WarehouseDeposit(id);
                });
            }
            if (!any)
                WarehouseNote(withdraw ? "Your warehouse is empty." : "Nothing in your bag to store.");
        }

        private void WarehouseNote(string text)
        {
            var label = UiKit.Label(_warehouseList, text, 16f, UiKit.TextDim);
            label.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
        }

        private void WarehouseRow(string text, Action onTap)
        {
            var button = UiKit.TextButton(_warehouseList, text, onTap, 16f);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) { label.alignment = TextAlignmentOptions.Left; label.color = UiKit.Text; }
            button.gameObject.AddComponent<LayoutElement>().minHeight = 44f;
        }
    }
}
