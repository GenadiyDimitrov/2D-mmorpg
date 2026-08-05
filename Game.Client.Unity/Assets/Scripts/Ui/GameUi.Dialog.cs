using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the NPC conversation — quests, class change, vendor, gatekeeper, buffer and
    /// skill reset, all in one window.
    ///
    /// This is the panel that makes the phone a place you can PLAY rather than just move around in:
    /// every one of those services is gated behind talking to someone, and until now the Unity client
    /// had no way to start a conversation at all.
    ///
    /// One window rather than six, because the server already sends one <see cref="NpcDialog"/> with
    /// whatever that NPC happens to offer — the sections below simply appear when their part is
    /// non-null, so a vendor shows wares and a gatekeeper shows destinations without the client
    /// knowing anything about NPC types.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _dialogPanel, _dialogContent;
        private TextMeshProUGUI _dialogTitle;
        private int _dialogStamp = -1;
        /// <summary>C7: which half of a gatekeeper's list is showing — false = the local hunting-field
        /// gates, true = the roads to other cities. Not reset per NPC on purpose: whichever errand you
        /// were on last is usually the one you are still on.</summary>
        private bool _gateCities;

        private void BuildDialogWindow()
        {
            _dialogPanel = UiKit.PanelBox(_worldRoot, "Dialog");
            UiKit.Place(_dialogPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(700f, 460f));
            var inner = _dialogPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_dialogPanel, "Talk", () =>
            {
                Boot.CloseDialog();
                CloseWindow(_dialogPanel);
            });

            _dialogTitle = UiKit.Label(inner, "", 19f, UiKit.Accent, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_dialogTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 6f), new Vector2(600f, 24f));

            ScrollRect scroll;
            _dialogContent = UiKit.ScrollArea(inner, out scroll, 4f);
            UiKit.Stretch((RectTransform)scroll.transform, 16f, chrome + 38f, 16f, 16f);

            _dialogPanel.gameObject.SetActive(false);
        }

        private void RefreshDialogWindow()
        {
            var d = Boot.Dialog;

            // The dialog OPENS ITSELF when the server answers — you tapped an NPC, the reply is the
            // conversation, and making you tap again to see it would be pointless ceremony.
            if (d != null && !_dialogPanel.gameObject.activeSelf) OpenWindow(_dialogPanel);
            if (d == null || !_dialogPanel.gameObject.activeSelf) return;

            // The gatekeeper tab is part of what is on screen, so it belongs in the stamp — otherwise
            // tapping Zones/Cities changes nothing until the server happens to push a new dialog.
            int stamp = d.GetHashCode() * 31 + (_gateCities ? 1 : 0);
            if (stamp == _dialogStamp) return;
            _dialogStamp = stamp;

            _dialogTitle.text = d.NpcName + (string.IsNullOrEmpty(d.NpcRole) ? "" : "   (" + d.NpcRole + ")");

            for (int i = _dialogContent.childCount - 1; i >= 0; i--)
                Destroy(_dialogContent.GetChild(i).gameObject);

            bool anything = false;

            // ----- quests -------------------------------------------------------------------------
            foreach (var quest in d.Turnable ?? new QuestSummary[0])
            {
                anything = true;
                string id = quest.Id;
                DialogRow("[Hand in]  " + quest.Name, "Complete",
                          () => Boot.QuestAction("complete", id), UiKit.Good);
            }

            // An OFFER opens the detail window rather than accepting from a wall of text (owner: *"per-
            // quest detail window with accept/decline instead of one wall of text"*). The row says what
            // it is and where it leads; the decision — with every step, the level band and the reward on
            // one page — is made in the window that Accept/Decline lives in.
            foreach (var quest in d.Offered ?? new QuestSummary[0])
            {
                anything = true;
                string id = quest.Id;
                // C6: the NAME, nothing else. The location line went with the step text — the detail
                // window carries every word of it, and this row exists to get you there.
                DialogRow("[New]  " + quest.Name, "Details", () => ShowQuestDetail(id), UiKit.Text);
            }

            foreach (var quest in d.InProgress ?? new QuestSummary[0])
            {
                anything = true;
                // C6 again: name and the COUNT (a number, not quest text). The step text lived here and
                // now lives only in Details — which the row gained a button for, since a row that says
                // less has to lead somewhere.
                string id2 = quest.Id;
                string progress = quest.CounterNeeded > 0
                    ? "   " + quest.Counter + " / " + quest.CounterNeeded : "";
                DialogRow(quest.Name + progress, "Details", () => ShowQuestDetail(id2), UiKit.TextDim);
            }

            // ----- class change -------------------------------------------------------------------
            foreach (var option in d.ClassChanges ?? new ClassChangeOption[0])
            {
                anything = true;
                int classId = option.SecondClassId;

                // Say WHICH item is missing rather than just greying the button: "you can't" without
                // "because" sends the player back to a wiki.
                string needs = "";
                if (!option.Meets && option.RequiredItemNames != null)
                    for (int i = 0; i < option.RequiredItemNames.Length; i++)
                        if (option.HasItem == null || i >= option.HasItem.Length || !option.HasItem[i])
                            needs += (needs.Length > 0 ? ", " : "") + option.RequiredItemNames[i];

                DialogRow("Become a " + option.ClassName + "\n" + option.Description
                          + (needs.Length > 0 ? "\nNeeds: " + needs : ""),
                          "Change",
                          option.Meets ? (System.Action)(() => Boot.QuestAction("changeclass", classId.ToString())) : null,
                          option.Meets ? UiKit.Text : UiKit.TextDim);
            }

            // ----- vendor -------------------------------------------------------------------------
            // The vendor now ASKS buy-or-sell and hands off to the dedicated window (numpad, Max, a
            // confirm) rather than a one-tap Buy-x1 per row — see GameUi.Vendor.cs. Selling was
            // impossible from the phone before this.
            if (d.Shop != null && d.Shop.Items != null)
            {
                anything = true;
                Header(string.IsNullOrEmpty(d.Shop.Title) ? "Trade" : d.Shop.Title);
                DialogRow("Buy — browse the vendor's wares", "Buy",
                          () => OpenVendor(false), UiKit.Text);
                DialogRow("Sell — items from your bag", "Sell",
                          () => OpenVendor(true), UiKit.Text);
                DialogRow("Buy back — items you recently sold", "Back",
                          () => OpenBuyBackWindow(), UiKit.Text);
            }

            // ----- warehouse keeper ---------------------------------------------------------------
            if (d.Warehouse)
            {
                anything = true;
                Header("Warehouse");
                DialogRow("Open your warehouse — deposit / withdraw", "Open",
                          () => { Boot.OpenWarehouse(); OpenWarehouseWindow(); }, UiKit.Text);
            }

            // ----- gatekeeper ---------------------------------------------------------------------
            if (d.Teleport != null && d.Teleport.Destinations != null)
            {
                anything = true;
                // A gatekeeper offers this city's own hunting-field GATES (named, one per camp) and the
                // roads to the other cities. Those are two different errands, so they are two TABS now
                // (C7) rather than one list you scroll past half of: the server marks a city with an
                // empty Group and a field gate with the field's name.
                //
                // Within Zones the gates stay grouped under the field they belong to — fifteen flat rows
                // of "<place> <compass>" is unreadable on a phone, and the field is the thing you are
                // actually choosing.
                bool anyZones = false;
                foreach (var dest in d.Teleport.Destinations)
                    if (!string.IsNullOrEmpty(dest.Group)) { anyZones = true; break; }
                // A gatekeeper with no local fields must not open on an empty tab.
                bool cities = _gateCities || !anyZones;

                Header("Travel");
                GateTabs(cities, anyZones);

                string group = null;
                foreach (var dest in d.Teleport.Destinations)
                {
                    if (string.IsNullOrEmpty(dest.Group) != cities) continue;
                    if (!cities && dest.Group != group)
                    {
                        group = dest.Group;
                        Header(group);
                    }

                    string zone = dest.DestId;
                    string band = dest.MaxLevel > 0 ? "   (Lv " + dest.MinLevel + "-" + dest.MaxLevel + ")" : "";
                    // The description is what the band actually CONTAINS ("Lv 8-12 · Goblin Scout, Ashen
                    // Wolf, Werewolf") — the whole reason to name a gate rather than dump you in a polygon.
                    string what = string.IsNullOrEmpty(dest.Description) || dest.Description == "City"
                                ? "" : "\n" + dest.Description;
                    // A free ride (under the free-travel level) says so — "0 gold" reads like a bug.
                    string price = dest.Fee <= 0 ? "Free" : dest.Fee.ToString("N0") + " " + GameConstants.CurrencyName;
                    DialogRow(dest.Name + band + "   " + price + what,
                              "Go", () => Boot.TeleportTo(zone),
                              Boot.Gold >= dest.Fee ? UiKit.Text : UiKit.TextDim);
                }
            }

            // ----- buffer -------------------------------------------------------------------------
            if (d.Buffer != null)
            {
                anything = true;
                Header("Blessings");
                if (!d.Buffer.CanBuff)
                {
                    Note2(string.IsNullOrEmpty(d.Buffer.Message) ? "Not available to you." : d.Buffer.Message);
                }
                else
                {
                    DialogRow("Full buff   " + Cost(d.Buffer.FullBuffCost), "Buff",
                              () => Boot.BufferAction("full", ""), UiKit.Text);
                    DialogRow("Restore HP/MP   " + Cost(d.Buffer.RestoreCost), "Restore",
                              () => Boot.BufferAction("restore", ""), UiKit.Text);

                    foreach (var buff in d.Buffer.Buffs ?? new BufferBuff[0])
                    {
                        string skillId = buff.SkillId;
                        DialogRow(buff.Name + "   " + Cost(buff.Cost), "Cast",
                                  () => Boot.BufferAction("single", skillId), UiKit.Text);
                    }
                }
            }

            // ----- skill reset --------------------------------------------------------------------
            if (d.SkillReset != null && d.SkillReset.Skills != null)
            {
                anything = true;
                Header("Unlearn   (free — but the gold you spent is NOT refunded)");
                foreach (var skill in d.SkillReset.Skills)
                {
                    string skillId = skill.SkillId;
                    DialogRow(skill.Name + (skill.Level > 1 ? "  Lv." + skill.Level : "")
                              + (skill.GoldSpent > 0 ? "   (cost " + skill.GoldSpent.ToString("N0") + ")" : ""),
                              "Forget", () => Boot.ForgetSkill(skillId), UiKit.Text);
                }
            }

            if (!anything) Note2("Nothing to discuss right now.");
        }

        private string Cost(long gold) =>
            gold <= 0 ? "free" : gold.ToString("N0") + " " + GameConstants.CurrencyName;

        private void Header(string text)
        {
            var label = UiKit.Label(_dialogContent, "<b>" + text + "</b>", 16f, UiKit.Accent);
            label.gameObject.AddComponent<LayoutElement>().minHeight = 30f;
        }

        private void Note2(string text)
        {
            var label = UiKit.Label(_dialogContent, text, 16f, UiKit.TextDim);
            label.gameObject.AddComponent<LayoutElement>().minHeight = 30f;
        }

        /// <summary>The gatekeeper's Zones / Cities switch, drawn as a row in the dialog's own list so it
        /// scrolls with the destinations it filters. <paramref name="anyZones"/> false greys Zones out
        /// rather than hiding it — a missing tab reads as a broken window, a dead one reads as "this
        /// gatekeeper has no fields of its own".</summary>
        private void GateTabs(bool cities, bool anyZones)
        {
            var row = UiKit.Box(_dialogContent, "GateTabs", new Color(0, 0, 0, 0), blocksInput: false);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 40f;

            var zones = UiKit.TextButton(row.transform, "Zones",
                                         () => { _gateCities = false; _dialogStamp = -1; }, 15f);
            zones.interactable = anyZones;
            UiKit.Place(UiKit.Rect(zones.gameObject), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(12f, 0f), new Vector2(140f, 34f));
            zones.targetGraphic.color = !cities ? UiKit.TabActive : UiKit.PanelLight;

            var towns = UiKit.TextButton(row.transform, "Cities",
                                         () => { _gateCities = true; _dialogStamp = -1; }, 15f);
            UiKit.Place(UiKit.Rect(towns.gameObject), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(158f, 0f), new Vector2(140f, 34f));
            towns.targetGraphic.color = cities ? UiKit.TabActive : UiKit.PanelLight;
        }

        private void DialogRow(string text, string buttonText, System.Action onClick, Color colour)
        {
            var row = UiKit.Box(_dialogContent, "Row", UiKit.PanelLight);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 56f;

            var label = UiKit.Label(row.transform, text, 15f, colour, TextAlignmentOptions.TopLeft);
            UiKit.Stretch(UiKit.Rect(label.gameObject), 12f, 8f, buttonText != null ? 130f : 12f, 8f);
            var fitter = label.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (buttonText == null) return;

            var button = UiKit.TextButton(row.transform, buttonText, onClick, 15f);
            button.interactable = onClick != null;
            UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-10f, 0f), new Vector2(112f, 40f));
        }
    }
}
