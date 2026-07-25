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

            int stamp = d.GetHashCode();
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

            foreach (var quest in d.Offered ?? new QuestSummary[0])
            {
                anything = true;
                string id = quest.Id;
                DialogRow(quest.Name + "\n" + quest.Description, "Accept",
                          () => Boot.QuestAction("accept", id), UiKit.Text);
            }

            foreach (var quest in d.InProgress ?? new QuestSummary[0])
            {
                anything = true;
                string progress = quest.CounterNeeded > 0
                    ? "   " + quest.Counter + " / " + quest.CounterNeeded : "";
                DialogRow(quest.Name + progress + "\n" + quest.CurrentStepText, null, null, UiKit.TextDim);
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
                DialogRow("Warehouse — deposit / withdraw (town)", "Bank",
                          () => { Boot.OpenWarehouse(); OpenWarehouseWindow(); }, UiKit.Text);
            }

            // ----- gatekeeper ---------------------------------------------------------------------
            if (d.Teleport != null && d.Teleport.Destinations != null)
            {
                anything = true;
                Header("Travel");
                foreach (var dest in d.Teleport.Destinations)
                {
                    string zone = dest.ZoneId;
                    string band = dest.MaxLevel > 0 ? "   (Lv " + dest.MinLevel + "-" + dest.MaxLevel + ")" : "";
                    DialogRow(dest.Name + band + "   " + dest.Fee.ToString("N0") + " " + GameConstants.CurrencyName,
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
