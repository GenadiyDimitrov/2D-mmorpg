using System;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the two ADMIN item windows, one panel in two modes. They existed only in the
    /// WPF harness and were never ported when it was deleted (0.42.8) — the server kept answering
    /// `/bag` and `/give` and nothing on the phone listened, so both commands "did nothing".
    ///
    /// <para><b>/bag &lt;name&gt;</b> — that player's inventory; tap a row to DESTROY the item (asks
    /// first). The server re-sends the bag after every removal, so the list stays current.</para>
    /// <para><b>/give &lt;name&gt;</b> — YOUR inventory; tap a row to hand it over (a stack asks how
    /// many). The server does not re-send the picker, so this mode reads your live bag instead.</para>
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _adminBagPanel, _adminBagList;
        private TextMeshProUGUI _adminBagTitle;
        private AdminBagDto _adminBag;
        private bool _adminGive;
        private object _adminBagSeen;   // the array last drawn — both sources are replaced whole on a push

        private void BuildAdminBagWindow()
        {
            _adminBagPanel = UiKit.PanelBox(_worldRoot, "AdminBag");
            UiKit.Place(_adminBagPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(660f, 560f));
            var inner = _adminBagPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_adminBagPanel, "Admin", () => CloseWindow(_adminBagPanel));

            _adminBagTitle = UiKit.Label(inner, "", 17f, UiKit.TextDim, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_adminBagTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 6f), new Vector2(600f, 22f));

            ScrollRect scroll;
            _adminBagList = UiKit.ScrollArea(inner, out scroll, 3f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 40f, 16f, 16f);

            _adminBagPanel.gameObject.SetActive(false);
        }

        /// <summary>Open (or refresh) the admin item window — the server's answer to `/bag` or `/give`.</summary>
        public void ShowAdminBag(AdminBagDto dto, bool give)
        {
            _adminBag = dto;
            _adminGive = give;
            _adminBagSeen = null;   // force a rebuild
            if (!_adminBagPanel.gameObject.activeSelf) OpenWindow(_adminBagPanel);
        }

        private void RefreshAdminBagWindow()
        {
            if (!_adminBagPanel.gameObject.activeSelf || _adminBag == null) return;

            var items = (_adminGive ? Boot.Inventory : _adminBag.Items) ?? Array.Empty<InventoryItemDto>();
            if (ReferenceEquals(items, _adminBagSeen)) return;
            _adminBagSeen = items;

            string who = _adminBag.OwnerName;
            _adminBagTitle.text = _adminGive
                ? "Give to " + who + " — tap one of YOUR items"
                : who + "'s bag — " + _adminBag.Gold.ToString("N0") + " " + GameConstants.CurrencyName + ". Tap to remove.";

            for (int i = _adminBagList.childCount - 1; i >= 0; i--)
                Destroy(_adminBagList.GetChild(i).gameObject);

            if (items.Length == 0)
            {
                var note = UiKit.Label(_adminBagList, _adminGive ? "Your bag is empty." : "Their bag is empty.", 16f, UiKit.TextDim);
                note.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
                return;
            }

            foreach (var item in ByName(items))
            {
                var def = ItemCatalog.Get(item.DefId);
                string name = def != null ? ItemTag.RowName(def, item) : item.DefId;
                string label = (def != null ? Coloured(name, def.Rarity) : name)
                             + (item.Quantity > 1 ? "   x" + item.Quantity : "")
                             + (item.Equipped ? "   <color=#9AA3AD>(equipped)</color>" : "");
                var id = item.InstanceId;
                int stack = item.Quantity;

                Action onTap;
                if (!_adminGive)
                    onTap = () => Ask("Remove " + name + (stack > 1 ? " x" + stack : "") + " from " + who + "?",
                                      "Remove", () => Boot.AdminRemoveItem(who, id));
                else if (stack > 1)
                    onTap = () => OpenNumpad("Give " + name + " to " + who, stack, "Give", qty =>
                    {
                        Boot.AdminGiveItem(who, id, qty);
                        CloseNumpad();
                    }, qty => qty >= stack ? "the whole stack" : qty + " of " + stack);
                else
                    onTap = () => Ask("Give " + name + " to " + who + "?", "Give", () => Boot.AdminGiveItem(who, id, 1));

                var button = UiKit.TextButton(_adminBagList, label, onTap, 16f);
                var lbl = button.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) { lbl.alignment = TextAlignmentOptions.Left; lbl.color = UiKit.Text; }
                button.gameObject.AddComponent<LayoutElement>().minHeight = 44f;
            }
        }
    }
}
