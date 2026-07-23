using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the party window and the invite prompt.
    ///
    /// The window shows itself whenever you are in a party and hides when you are not — an empty
    /// roster is exactly how the server says "you left". It is not in the window stack for that
    /// reason: there is nothing to close, only a party to leave.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _partyPanel, _partyContent;
        private TextMeshProUGUI _partyTitle;
        private int _partyStamp = -1;

        private RectTransform _invitePanel;
        private TextMeshProUGUI _inviteText;

        private void BuildPartyWindow()
        {
            _partyPanel = UiKit.PanelBox(_worldRoot, "Party");
            UiKit.Place(_partyPanel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -230f), new Vector2(300f, 270f));
            var inner = _partyPanel.GetChild(0);

            _partyTitle = UiKit.Label(inner, "", 16f, UiKit.Accent, TextAlignmentOptions.Left);
            UiKit.Place(UiKit.Rect(_partyTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -8f), new Vector2(240f, 22f));

            var leave = UiKit.TextButton(inner, "Leave", () => Boot.PartyLeave(), 14f);
            UiKit.Place(UiKit.Rect(leave.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-10f, -6f), new Vector2(72f, 26f));

            ScrollRect scroll;
            _partyContent = UiKit.ScrollArea(inner, out scroll, 2f);
            UiKit.Stretch((RectTransform)scroll.transform, 10f, 34f, 10f, 10f);

            // Movable: drag anywhere on the panel BACKGROUND (the member rows and the scroll list
            // catch their own taps/drags first, so this only fires on empty space and the title strip).
            inner.gameObject.AddComponent<DragMove>().Target = _partyPanel;

            _partyPanel.gameObject.SetActive(false);

            BuildInvitePrompt();
        }

        private void BuildInvitePrompt()
        {
            _invitePanel = UiKit.PanelBox(_root, "PartyInvite");
            UiKit.Place(_invitePanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -60f), new Vector2(500f, 180f));
            var inner = _invitePanel.GetChild(0);

            _inviteText = UiKit.Label(inner, "", 17f, UiKit.Text, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_inviteText.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(20f, -18f), new Vector2(450f, 66f));

            var accept = UiKit.TextButton(inner, "Join", () => Boot.AnswerPartyInvite(true));
            UiKit.Place(UiKit.Rect(accept.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(20f, 18f), new Vector2(210f, 48f));

            var decline = UiKit.TextButton(inner, "Decline", () => Boot.AnswerPartyInvite(false));
            UiKit.Place(UiKit.Rect(decline.gameObject), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-20f, 18f), new Vector2(210f, 48f));

            _invitePanel.gameObject.SetActive(false);
        }

        private void RefreshPartyWindow()
        {
            var invite = Boot.PendingInvite;
            _invitePanel.gameObject.SetActive(invite != null);
            if (invite != null)
                _inviteText.text = invite.InviterName + " invites you to a party.\n\n"
                                 + "Loot rule: " + LootName(invite.LootMode)
                                 + "\nGold is always split evenly.";

            var party = Boot.Party;
            bool inParty = party != null && party.Length > 0;
            _partyPanel.gameObject.SetActive(inParty);
            if (!inParty) { _partyStamp = -1; return; }

            // HP/MP move constantly, so the stamp covers them: the party window IS a health display,
            // and a roster that only rebuilds on membership change would show stale bars.
            int stamp = party.Length * 31 + (int)Boot.PartyLoot;
            foreach (var m in party) stamp = stamp * 31 + m.Hp + m.Mp * 7 + (int)m.Status;
            if (stamp == _partyStamp) return;
            _partyStamp = stamp;

            _partyTitle.text = "Party " + party.Length + "   ·   " + LootName(Boot.PartyLoot);

            for (int i = _partyContent.childCount - 1; i >= 0; i--)
                Destroy(_partyContent.GetChild(i).gameObject);

            bool iLead = false;
            foreach (var m in party) if (m.IsLeader && m.Id == Boot.SelfId) iLead = true;

            foreach (var member in party)
            {
                var row = UiKit.Box(_partyContent, "Member", UiKit.PanelLight);
                row.gameObject.AddComponent<LayoutElement>().minHeight = 48f;

                // Tapping a member TARGETS them — that is how you heal someone without hunting for
                // their marker in a fight.
                var select = row.gameObject.AddComponent<Button>();
                select.targetGraphic = row;
                var id = member.Id;
                select.onClick.AddListener(() => Boot.TargetId = id);

                string title = (member.IsLeader ? "* " : "") + member.Name + "   Lv " + member.Level;
                if (member.Status != PartyMemberStatus.Online) title += "   (" + member.Status + ")";

                var label = UiKit.Label(row.transform, title, 15f,
                                        member.Status == PartyMemberStatus.Online ? UiKit.Text : UiKit.TextDim,
                                        TextAlignmentOptions.Left);
                UiKit.Place(UiKit.Rect(label.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(10f, -4f), new Vector2(230f, 20f));

                var hp = UiKit.ValueBar(row.transform, UiKit.Hp);
                UiKit.Place(UiKit.Rect(hp.transform.parent.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(10f, -26f), new Vector2(220f, 10f));
                UiKit.SetBar(hp, member.Hp, member.MaxHp);

                var mp = UiKit.ValueBar(row.transform, UiKit.Mp);
                UiKit.Place(UiKit.Rect(mp.transform.parent.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(10f, -38f), new Vector2(220f, 8f));
                UiKit.SetBar(mp, member.Mp, member.MaxMp);

                // Debuffs on a party member matter to a healer more than almost anything else.
                if (member.Debuffs != null && member.Debuffs.Length > 0)
                {
                    var debuffs = UiKit.Label(row.transform, string.Join(" ", member.Debuffs), 12f,
                                              new Color(1f, 0.55f, 0.55f), TextAlignmentOptions.Left);
                    UiKit.Place(UiKit.Rect(debuffs.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                                new Vector2(10f, 2f), new Vector2(230f, 14f));
                }

                if (iLead && member.Id != Boot.SelfId)
                {
                    var kick = UiKit.TextButton(row.transform, "Kick", () => Boot.PartyKick(id), 13f);
                    UiKit.Place(UiKit.Rect(kick.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                new Vector2(-8f, 0f), new Vector2(60f, 30f));
                }
            }

            // Loot mode is the LEADER's to change, and the server requires a unanimous vote — so this
            // starts a vote rather than setting anything.
            if (iLead)
            {
                var cycle = UiKit.TextButton(_partyContent, "Propose loot: " + LootName(NextLoot(Boot.PartyLoot)),
                                             () => Boot.PartySetLoot(NextLoot(Boot.PartyLoot)), 14f);
                cycle.gameObject.AddComponent<LayoutElement>().minHeight = 36f;
            }
        }

        private static LootMode NextLoot(LootMode mode)
        {
            switch (mode)
            {
                case LootMode.FindersKeepers: return LootMode.Random;
                case LootMode.Random:         return LootMode.RoundRobin;
                case LootMode.RoundRobin:     return LootMode.LeaderOnly;
                default:                      return LootMode.FindersKeepers;
            }
        }

        private static string LootName(LootMode mode)
        {
            switch (mode)
            {
                case LootMode.FindersKeepers: return "finders keepers";
                case LootMode.Random:         return "random";
                case LootMode.RoundRobin:     return "round robin";
                case LootMode.LeaderOnly:     return "leader only";
                default:                      return mode.ToString();
            }
        }
    }
}
