using System;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the BUY-BACK window. Lists items you recently SOLD (Boot.BuyBack, which the
    /// server pushes when a vendor opens); tap a row to re-buy it for the same gold you got. The server
    /// owns the price + gold re-check. Opened from a vendor's dialog. Mirrors the warehouse window's shape.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _buyBackPanel, _buyBackList;
        private TextMeshProUGUI _buyBackTitle;
        private int _buyBackRevision = -1;

        private void BuildBuyBackWindow()
        {
            _buyBackPanel = UiKit.PanelBox(_worldRoot, "BuyBack");
            UiKit.Place(_buyBackPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(660f, 500f));
            var inner = _buyBackPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_buyBackPanel, "Buy Back", () => CloseWindow(_buyBackPanel));

            _buyBackTitle = UiKit.Label(inner, "", 17f, UiKit.TextDim, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_buyBackTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 6f), new Vector2(500f, 22f));

            ScrollRect scroll;
            _buyBackList = UiKit.ScrollArea(inner, out scroll, 3f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 40f, 16f, 16f);

            _buyBackPanel.gameObject.SetActive(false);
        }

        /// <summary>Open the buy-back window (its list rides the shop's "BuyBack" push).</summary>
        public void OpenBuyBackWindow()
        {
            _buyBackRevision = -1;   // force a rebuild
            OpenWindow(_buyBackPanel);
        }

        private void RefreshBuyBackWindow()
        {
            if (!_buyBackPanel.gameObject.activeSelf) return;

            var list = Boot.BuyBack ?? Array.Empty<BuyBackEntryDto>();
            int revision = (int)(Boot.Gold % 1_000_000);
            foreach (var e in list) revision = revision * 31 + e.Index * 7 + e.Quantity + (int)(e.UnitPrice % 1000);
            if (revision == _buyBackRevision) return;
            _buyBackRevision = revision;

            _buyBackTitle.text = "Buy back — you have " + Boot.Gold.ToString("N0") + " " + GameConstants.CurrencyName;

            for (int i = _buyBackList.childCount - 1; i >= 0; i--)
                Destroy(_buyBackList.GetChild(i).gameObject);

            if (list.Length == 0)
            {
                var note = UiKit.Label(_buyBackList, "Nothing to buy back — sell something first.", 16f, UiKit.TextDim);
                note.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
                return;
            }

            foreach (var e in list)
            {
                long cost = e.UnitPrice * e.Quantity;
                bool afford = Boot.Gold >= cost;
                // Quality colour, like every other item list — the name alone no longer says which
                // rung of the ladder you sold.
                // Colour only when affordable — TMP's <color> markup wins over the label colour, so a
                // coloured name would cancel the dimming that marks a row you can't pay for.
                var def = ItemCatalog.Get(e.DefId);
                string shown = def != null && afford ? Coloured(e.Name, def.Rarity) : e.Name;
                string label = shown + (e.Quantity > 1 ? "   x" + e.Quantity : "")
                             + "   " + cost.ToString("N0") + " " + GameConstants.CurrencyName;
                int idx = e.Index;
                var button = UiKit.TextButton(_buyBackList, label, () => Boot.BuyBackItem(idx), 16f);
                var lbl = button.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) { lbl.alignment = TextAlignmentOptions.Left; lbl.color = afford ? UiKit.Text : UiKit.TextDim; }
                button.gameObject.AddComponent<LayoutElement>().minHeight = 44f;
            }
        }
    }
}
