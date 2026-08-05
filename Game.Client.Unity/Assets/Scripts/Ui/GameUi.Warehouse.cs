using System;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the WAREHOUSE (bank). Two tabs — DEPOSIT lists your bag (tap to stash),
    /// WITHDRAW lists the bank (tap to take back). Whole-item moves; the SERVER owns the transfer, gates it
    /// to town, and pushes the contents (Boot.Warehouse). Opened from a town NPC's dialog. Mirrors the
    /// vendor window's tab+list shape, without prices or a quantity numpad (a move is reversible).
    ///
    /// A second row of buttons picks WHICH bank: **Private** (this character, free) or **Account**
    /// (shared with your other characters, tradable items only, 10k per slot). They are one window
    /// rather than two because the question a player has at the keeper is "where do I put this",
    /// which is a choice inside one task, not two separate errands.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _warehousePanel, _warehouseList;
        private TextMeshProUGUI _warehouseTitle, _warehouseSlotsLabel;
        private Button _warehouseDepositTab, _warehouseWithdrawTab;
        private Button _warehousePrivateTab, _warehouseAccountTab;
        private bool _warehouseWithdraw;
        private bool _warehouseAccount;
        private int _warehouseRevision = -1;
        /// <summary>C8's category filter, the third window it lands on. Quest is not a tab here —
        /// a token can't be banked at all (B4), so the category would only ever be empty.</summary>
        private ItemCategory _warehouseTab = ItemCategory.All;
        private Button[] _warehouseTabButtons;
        private static readonly ItemCategory[] WarehouseTabs =
            { ItemCategory.All, ItemCategory.Gear, ItemCategory.Use, ItemCategory.Mats };

        private void BuildWarehouseWindow()
        {
            _warehousePanel = UiKit.PanelBox(_worldRoot, "Warehouse");
            UiKit.Place(_warehousePanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(660f, 500f));
            var inner = _warehousePanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_warehousePanel, "Warehouse", () => CloseWindow(_warehousePanel));

            _warehouseTitle = UiKit.Label(inner, "", 17f, UiKit.TextDim, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_warehouseTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 6f), new Vector2(470f, 22f));

            // Slot usage, top-RIGHT, beside the mode line (G6).
            _warehouseSlotsLabel = UiKit.Label(inner, "", 15f, UiKit.TextDim, TextAlignmentOptions.Right);
            UiKit.Place(UiKit.Rect(_warehouseSlotsLabel.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-18f, -chrome - 6f), new Vector2(150f, 22f));

            _warehouseDepositTab = UiKit.TextButton(inner, "Deposit", () => SetWarehouseMode(false), 15f);
            UiKit.Place(UiKit.Rect(_warehouseDepositTab.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 32f), new Vector2(130f, 30f));
            _warehouseWithdrawTab = UiKit.TextButton(inner, "Withdraw", () => SetWarehouseMode(true), 15f);
            UiKit.Place(UiKit.Rect(_warehouseWithdrawTab.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(154f, -chrome - 32f), new Vector2(130f, 30f));

            _warehousePrivateTab = UiKit.TextButton(inner, "Private", () => SetWarehouseBank(false), 15f);
            UiKit.Place(UiKit.Rect(_warehousePrivateTab.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 66f), new Vector2(130f, 30f));
            _warehouseAccountTab = UiKit.TextButton(inner, "Account", () => SetWarehouseBank(true), 15f);
            UiKit.Place(UiKit.Rect(_warehouseAccountTab.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(154f, -chrome - 66f), new Vector2(130f, 30f));

            // The category strip shares the Private/Account row rather than taking a fourth one — the
            // window is 660 wide and those two buttons use only its left half, so a row of its own would
            // cost list height for nothing.
            _warehouseTabButtons = BuildCategoryTabs(inner, WarehouseTabs, new Vector2(296f, -chrome - 66f), 80f,
                                                     cat => { _warehouseTab = cat; _warehouseRevision = -1; });

            ScrollRect scroll;
            _warehouseList = UiKit.ScrollArea(inner, out scroll, 3f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 102f, 16f, 16f);

            _warehousePanel.gameObject.SetActive(false);
        }

        /// <summary>Open the window; its contents arrive via Boot.OpenWarehouse → the "Warehouse" push.</summary>
        public void OpenWarehouseWindow()
        {
            _warehouseWithdraw = false;
            _warehouseAccount = false;
            _warehouseRevision = -1;   // force a rebuild
            OpenWindow(_warehousePanel);
        }

        private void SetWarehouseMode(bool withdraw)
        {
            _warehouseWithdraw = withdraw;
            _warehouseRevision = -1;
        }

        private void SetWarehouseBank(bool account)
        {
            _warehouseAccount = account;
            _warehouseRevision = -1;
            // Ask for THAT bank's contents. The server gates both to town, so a refusal arrives as a
            // system line rather than as a window that silently shows nothing.
            if (account) Boot.OpenAccountWarehouse();
            else Boot.OpenWarehouse();
        }

        /// <summary>Rebuild the list when the tab, bag or bank changed (revision-gated, like the vendor).</summary>
        private void RefreshWarehouseWindow()
        {
            if (!_warehousePanel.gameObject.activeSelf) return;

            var bag = Boot.Inventory ?? Array.Empty<InventoryItemDto>();
            var bank = (_warehouseAccount ? Boot.AccountWarehouse : Boot.Warehouse)
                       ?? Array.Empty<InventoryItemDto>();
            int revision = (_warehouseWithdraw ? 1 : 0) * 92821 + (_warehouseAccount ? 1 : 0) * 51787
                         + (int)_warehouseTab * 104729;
            foreach (var it in bag) revision = revision * 31 + it.InstanceId.GetHashCode() + it.Quantity + (it.Equipped ? 7 : 0);
            foreach (var it in bank) revision = revision * 31 + it.InstanceId.GetHashCode() * 7 + it.Quantity;
            if (revision == _warehouseRevision) return;
            _warehouseRevision = revision;

            _warehouseTitle.text = _warehouseWithdraw
                ? (_warehouseAccount ? "Withdraw — tap an item to take it out (free)"
                                     : "Withdraw — tap an item to take it out")
                : (_warehouseAccount
                    ? "Deposit — tradable items only, " + GameConstants.AccountWarehouseSlotFee.ToString("N0")
                      + " " + GameConstants.CurrencyName + " per new slot"
                    : "Deposit — tap a bag item to store it (town only)");

            // G6: how full the bank is, on the window itself. Until now a full warehouse announced
            // itself only as a refusal in the chat log — you tapped, nothing visibly happened, and the
            // reason was in a window you weren't looking at. RED once there is no room left.
            int slotCap = _warehouseAccount ? GameConstants.AccountWarehouseSize : GameConstants.WarehouseSize;
            bool full = bank.Length >= slotCap;
            _warehouseSlotsLabel.text = "Slots " + bank.Length + " / " + slotCap;
            _warehouseSlotsLabel.color = full ? UiKit.Bad : UiKit.TextDim;
            _warehouseDepositTab.targetGraphic.color = _warehouseWithdraw ? UiKit.PanelLight : UiKit.TabActive;
            _warehouseWithdrawTab.targetGraphic.color = _warehouseWithdraw ? UiKit.TabActive : UiKit.PanelLight;
            _warehousePrivateTab.targetGraphic.color = _warehouseAccount ? UiKit.PanelLight : UiKit.TabActive;
            _warehouseAccountTab.targetGraphic.color = _warehouseAccount ? UiKit.TabActive : UiKit.PanelLight;
            PaintCategoryTabs(_warehouseTabButtons, WarehouseTabs, _warehouseTab);

            for (int i = _warehouseList.childCount - 1; i >= 0; i--)
                Destroy(_warehouseList.GetChild(i).gameObject);

            BuildWarehouseList(_warehouseWithdraw ? bank : bag, _warehouseWithdraw, _warehouseAccount);
        }

        private void BuildWarehouseList(InventoryItemDto[] items, bool withdraw, bool account)
        {
            bool any = false;
            foreach (var item in ByName(items))          // C8: name order, same as the bag and the vendor
            {
                if (!withdraw && item.Equipped) continue;   // worn gear isn't stashable (unequip it first)
                var def = ItemCatalog.Get(item.DefId);
                if (def == null || !InCategory(_warehouseTab, def)) continue;

                // The account bank takes tradable items only, so a bound item is dropped from the
                // DEPOSIT list rather than listed and then refused — a row you can tap and be told no
                // is worse than a row that was never offered.
                if (!withdraw && account && !def.Tradable) continue;
                // Quest items are refused by BOTH banks (B4): banking a token stalled its quest step
                // while Complete still took it. Same rule as above — never offer the row.
                if (!withdraw && ItemCatalog.IsQuestItem(def)) continue;
                any = true;

                string label = Coloured(def.Name, def.Rarity) + (item.Quantity > 1 ? "   x" + item.Quantity : "");
                var id = item.InstanceId;
                WarehouseRow(label, () =>
                {
                    if (withdraw) { if (account) Boot.AccountWarehouseWithdraw(id); else Boot.WarehouseWithdraw(id); }
                    else { if (account) Boot.AccountWarehouseDeposit(id); else Boot.WarehouseDeposit(id); }
                });
            }
            if (!any)
                WarehouseNote(_warehouseTab != ItemCategory.All
                    ? "Nothing in this category — try All."
                    : withdraw
                        ? (account ? "Your account warehouse is empty." : "Your warehouse is empty.")
                        : (account ? "Nothing tradable in your bag to store." : "Nothing in your bag to store."));
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
