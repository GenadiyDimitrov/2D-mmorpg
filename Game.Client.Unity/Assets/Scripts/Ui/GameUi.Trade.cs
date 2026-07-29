using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// PLAYER-TO-PLAYER TRADE (batch E), ported from the WPF layout the owner specified.
    ///
    /// The whole thing is server-side already — offers, gold, the two-sided ready flag and the atomic
    /// swap all live in GameLoopService. This is the client half, and it is deliberately a THIN view:
    /// every change is sent to the server, and the window redraws only from the `Trade` push that
    /// comes back. Nothing is applied optimistically.
    ///
    /// That matters more here than anywhere else in the client. A trade window that shows what you
    /// THINK you offered, rather than what the server recorded, is how players get robbed — the
    /// classic exploit is swapping an item out in the instant between the other side reading the
    /// window and pressing confirm. Since the server re-sends the full state on every change (and
    /// clears both ready flags when anything moves), drawing only what it sent makes that impossible
    /// to express.
    ///
    /// Layout, per the owner's spec: left column = what each side offers with the gold under it, and
    /// my gold entry below that; right column = my inventory to pick from; Confirm/Cancel at the
    /// bottom. Movable, with an X.
    /// </summary>
    public partial class GameUi
    {
        private RectTransform _tradePanel;
        private RectTransform _tradeMine, _tradeTheirs, _tradeBag;
        private TextMeshProUGUI _tradeTitle, _tradeMyGoldLabel, _tradeTheirGoldLabel, _tradeStatus;
        private TMP_InputField _tradeGoldField;
        private Button _tradeConfirmButton;

        /// <summary>What I have put up, by item instance. The server owns the real list; this is only
        /// what we send when it changes, and it is re-synced from every push.</summary>
        private readonly List<Guid> _tradeOffer = new();

        private Guid? _tradeRequestFrom;

        private void BuildTradePanel()
        {
            _tradePanel = UiKit.PanelBox(_worldRoot, "Trade");
            UiKit.Place(_tradePanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(880f, 560f));
            var inner = _tradePanel.GetChild(0);

            // Cancelling on close is not a courtesy — a trade window you can dismiss while the server
            // still has you in a live trade would leave you unable to start another one.
            float chrome = UiKit.WindowChrome(_tradePanel, "Trade", () => CancelTrade());

            _tradeTitle = UiKit.Label(inner, "", 15f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(_tradeTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -chrome - 6f), new Vector2(500f, 24f));

            const float colW = 400f, top = 96f;

            // ---- left column: the two offers, then my gold entry ----
            UiKit.Place(UiKit.Rect(UiKit.Label(inner, "You offer", 14f, UiKit.Accent).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -top + 26f),
                        new Vector2(colW, 22f));
            _tradeMine = UiKit.ScrollArea(inner, out var mineScroll, 2f);
            UiKit.Place((RectTransform)mineScroll.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -top), new Vector2(colW, 150f));

            _tradeMyGoldLabel = UiKit.Label(inner, "gold: 0", 13f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(_tradeMyGoldLabel.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(20f, -top - 152f), new Vector2(colW, 20f));

            UiKit.Place(UiKit.Rect(UiKit.Label(inner, "They offer", 14f, UiKit.Accent).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -top - 178f),
                        new Vector2(colW, 22f));
            _tradeTheirs = UiKit.ScrollArea(inner, out var theirsScroll, 2f);
            UiKit.Place((RectTransform)theirsScroll.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -top - 204f), new Vector2(colW, 150f));

            _tradeTheirGoldLabel = UiKit.Label(inner, "gold: 0", 13f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(_tradeTheirGoldLabel.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(20f, -top - 356f), new Vector2(colW, 20f));

            _tradeGoldField = UiKit.InputField(inner, "gold to offer", false, 15f);
            UiKit.Place(UiKit.Rect(_tradeGoldField.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(16f, -top - 382f), new Vector2(250f, 34f));

            var setGold = UiKit.TextButton(inner, "Set", SendTradeGold, 15f);
            UiKit.Place(UiKit.Rect(setGold.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(274f, -top - 382f), new Vector2(90f, 34f));

            // ---- right column: my inventory ----
            UiKit.Place(UiKit.Rect(UiKit.Label(inner, "Your bag — tap to add or remove", 14f, UiKit.Accent).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(colW + 40f, -top + 26f),
                        new Vector2(colW, 22f));
            _tradeBag = UiKit.ScrollArea(inner, out var bagScroll, 2f);
            UiKit.Place((RectTransform)bagScroll.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(colW + 40f, -top), new Vector2(colW, 356f));

            // ---- bottom ----
            _tradeStatus = UiKit.Label(inner, "", 14f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(_tradeStatus.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(colW + 44f, -top - 356f), new Vector2(colW, 22f));

            _tradeConfirmButton = UiKit.TextButton(inner, "Confirm", () =>
                Boot.Trade(n => n.TradeReadyAsync(), "ready"), 16f);
            UiKit.Place(UiKit.Rect(_tradeConfirmButton.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(colW + 40f, -top - 382f), new Vector2(180f, 38f));

            var cancel = UiKit.TextButton(inner, "Cancel", () => CancelTrade(), 16f);
            UiKit.Place(UiKit.Rect(cancel.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(colW + 230f, -top - 382f), new Vector2(180f, 38f));

            var drag = _tradePanel.gameObject.AddComponent<DragMove>();
            drag.Target = _tradePanel;

            _tradePanel.gameObject.SetActive(false);
        }

        private void CancelTrade()
        {
            Boot.Trade(n => n.TradeCancelAsync(), "cancel");
            CloseWindow(_tradePanel);
        }

        private void SendTradeGold()
        {
            long gold = long.TryParse(_tradeGoldField.text, NumberStyles.Integer,
                                      CultureInfo.InvariantCulture, out var g) ? g : 0;
            Boot.Trade(n => n.TradeGoldAsync(gold), "gold");
        }

        /// <summary>Someone asked to trade. A prompt, never an auto-accept: a trade window opening by
        /// itself mid-fight is both an annoyance and the first half of a scam.</summary>
        public void OnTradeRequest(TradeRequestNotice notice)
        {
            if (notice == null) return;
            _tradeRequestFrom = notice.FromId;
            Ask($"{notice.FromName} wants to trade.", "Accept", () =>
                Boot.Trade(n => n.TradeRespondAsync(true), "accept"));
        }

        /// <summary>Redraw from the server's state. This is the ONLY thing that writes the window.</summary>
        public void OnTradeState(TradeStateUpdate state)
        {
            if (state == null || _tradePanel == null) return;

            if (!state.Active)
            {
                CloseWindow(_tradePanel);
                _tradeOffer.Clear();
                return;
            }

            if (!_tradePanel.gameObject.activeSelf) OpenWindow(_tradePanel);

            _tradeTitle.text = "Trade with " + state.PartnerName;
            _tradeMyGoldLabel.text = "gold: " + state.MyGold.ToString("N0", CultureInfo.InvariantCulture);
            _tradeTheirGoldLabel.text = "gold: " + state.TheirGold.ToString("N0", CultureInfo.InvariantCulture);

            // Re-sync from the server rather than trusting what we sent — if it rejected an item
            // (untradable, already gone) the bag must stop showing it as offered.
            _tradeOffer.Clear();
            foreach (var item in state.MyOffer) _tradeOffer.Add(item.InstanceId);

            _tradeStatus.text = (state.MyReady ? "you: READY" : "you: not ready") + "    " +
                                (state.TheirReady ? "them: READY" : "them: not ready");
            _tradeStatus.color = state.MyReady && state.TheirReady ? UiKit.Good : UiKit.TextDim;
            UiKit.SetButtonText(_tradeConfirmButton, state.MyReady ? "Ready (waiting)" : "Confirm");

            FillTradeList(_tradeMine, state.MyOffer, null);
            FillTradeList(_tradeTheirs, state.TheirOffer, null);
            FillTradeBag();
        }

        private void FillTradeList(RectTransform content, InventoryItemDto[] items, Action<Guid> onTap)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            foreach (var item in items)
            {
                string label = TradeItemLabel(item);
                if (onTap == null)
                {
                    var text = UiKit.Label(content, label, 14f, UiKit.Text);
                    text.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
                }
                else
                {
                    var id = item.InstanceId;
                    var button = UiKit.TextButton(content, label, () => onTap(id), 14f);
                    button.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
                }
            }
        }

        private static string TradeItemLabel(InventoryItemDto item)
        {
            var def = ItemCatalog.Get(item.DefId);
            string name = def?.Name ?? item.DefId;
            if (item.Enchant > 0) name += " +" + item.Enchant;
            if (item.Quantity > 1) name += "  x" + item.Quantity;
            // Quality colour matters MOST here: a trade is the one place you commit to an item you
            // cannot inspect, and the same piece exists at six qualities under one name.
            return def != null ? Coloured(name, def.Rarity) : name;
        }

        private void FillTradeBag()
        {
            for (int i = _tradeBag.childCount - 1; i >= 0; i--)
                Destroy(_tradeBag.GetChild(i).gameObject);

            foreach (var item in Boot.Inventory)
            {
                // Equipped gear is not offerable — the server refuses it, and listing it would only
                // produce a tap that silently does nothing.
                if (item.Equipped) continue;

                bool offered = _tradeOffer.Contains(item.InstanceId);
                var id = item.InstanceId;
                var button = UiKit.TextButton(_tradeBag,
                    (offered ? "- " : "+ ") + TradeItemLabel(item), () => ToggleTradeItem(id), 14f);
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
            }
        }

        /// <summary>Add or remove one item and send the WHOLE offer. The server's command takes the
        /// complete set, which makes the message idempotent: a lost or duplicated tap cannot leave the
        /// two sides disagreeing about what is on the table.</summary>
        private void ToggleTradeItem(Guid instanceId)
        {
            if (!_tradeOffer.Remove(instanceId)) _tradeOffer.Add(instanceId);
            var ids = _tradeOffer.ToArray();
            Boot.Trade(n => n.TradeOfferAsync(ids), "offer");
            FillTradeBag();   // immediate feedback; the server's push is what settles it
        }
    }
}
