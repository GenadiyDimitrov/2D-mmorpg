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
        private Button _partyViewButton;
        private int _partyStamp = -1;
        // Member buff/debuff view: 0 buffs only, 1 debuffs only, 2 all, 3 none (owner's 4-stage toggle).
        private int _partyView = 2;
        private static readonly string[] PartyViewNames = { "buffs", "debuffs", "all", "none" };

        private RectTransform _invitePanel;
        private TextMeshProUGUI _inviteText;

        private void BuildPartyWindow()
        {
            _partyPanel = UiKit.PanelBox(_worldRoot, "Party");
            // 380 wide, not 300: the effect SQUARES sit to the right of each member's bars (owner), and
            // the leader's Kick/Lead buttons need to clear them.
            UiKit.Place(_partyPanel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -230f), new Vector2(380f, 270f));
            var inner = _partyPanel.GetChild(0);

            _partyTitle = UiKit.Label(inner, "", 16f, UiKit.Accent, TextAlignmentOptions.Left);
            UiKit.Place(UiKit.Rect(_partyTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -8f), new Vector2(240f, 22f));

            var leave = UiKit.TextButton(inner, "Leave", () => Boot.PartyLeave(), 14f);
            UiKit.Place(UiKit.Rect(leave.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-10f, -6f), new Vector2(66f, 26f));

            // 4-stage view of members' effects, like the buff-bar Hide: buffs / debuffs / all / none.
            _partyViewButton = UiKit.TextButton(inner, "", () =>
            {
                _partyView = (_partyView + 1) % 4;
                _partyStamp = -1;   // force a rebuild
            }, 13f);
            UiKit.Place(UiKit.Rect(_partyViewButton.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-82f, -6f), new Vector2(96f, 26f));

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
            // IsLeader and the member IDs are part of the stamp on purpose. Without them, passing
            // leadership changed NOTHING the stamp could see — same roster, same HP, same buffs — so
            // the window never rebuilt: the crown stayed on the old leader and the Lead button stayed
            // put, which read as "[lead] doesn't work". Swapping one member for another with identical
            // HP had the same blind spot, hence the IDs.
            int stamp = party.Length * 31 + (int)Boot.PartyLoot + _partyView * 131 + (_lootMenuOpen ? 7717 : 0);
            foreach (var m in party) stamp = stamp * 31 + m.Hp + m.Mp * 7 + (int)m.Status
                + (m.Buffs?.Length ?? 0) * 17 + (m.Debuffs?.Length ?? 0) * 13
                + (m.IsLeader ? 1000003 : 0) + m.Id.GetHashCode();
            if (stamp == _partyStamp) return;
            _partyStamp = stamp;

            UiKit.SetButtonText(_partyViewButton, "fx: " + PartyViewNames[_partyView]);
            _partyTitle.text = "Party " + party.Length + "   ·   " + LootName(Boot.PartyLoot);

            for (int i = _partyContent.childCount - 1; i >= 0; i--)
                Destroy(_partyContent.GetChild(i).gameObject);

            bool iLead = false;
            foreach (var m in party) if (m.IsLeader && m.Id == Boot.SelfId) iLead = true;

            foreach (var member in party)
            {
                // Effects are SQUARES beside the member now, not a wrapping text line under them — so a
                // row is a fixed 46px whether or not anything is up, and the window stops growing taller
                // every time someone gets buffed (owner: "that way the party window will decrease in
                // height").
                var row = UiKit.Box(_partyContent, "Member", UiKit.PanelLight);
                row.gameObject.AddComponent<LayoutElement>().minHeight = 46f;

                // Tapping a member TARGETS them — that is how you heal someone without hunting for
                // their marker in a fight.
                var select = row.gameObject.AddComponent<Button>();
                select.targetGraphic = row;
                var id = member.Id;
                select.onClick.AddListener(() => Boot.TargetId = id);

                // Leader badge: a gold star instead of the old bare "*" (owner asked for a star/crown).
                // ⚠ FONT RISK — the bundled LiberationSans has no glyph for "→" or "✕" and TMP draws a
                // missing glyph as a hollow box (that is where the "[]" close buttons came from). U+2605
                // BLACK STAR is usually present, but VERIFY ON DEVICE: if this shows a box, fall back to
                // "<color=#FFD24A>*</color>" — the colour is what makes it read as a badge either way.
                string crown = member.IsLeader ? "<color=#FFD24A>★</color> " : "";
                string title = crown + member.Name + "   Lv " + member.Level;
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

                // Effect SQUARES to the right of the bars — the same shape as your own buff bar, minus
                // the countdown (owner). A healer reads who needs a cleanse; a buffer reads who is
                // missing a chant, both at a glance rather than by parsing a run-on line of names.
                //
                // ⚠ NO <60s flashing here, unlike the personal buff bar: PartyMemberDto carries effect
                // NAMES only (string[]), no remaining time, so the client has nothing to count down.
                // Adding it means putting durations on the wire — a DTO change, hence a protocol bump.
                BuildMemberEffects(row.transform, member);

                if (iLead && member.Id != Boot.SelfId)
                {
                    var kick = UiKit.TextButton(row.transform, "Kick", () => Boot.PartyKick(id), 13f);
                    UiKit.Place(UiKit.Rect(kick.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                new Vector2(-8f, 0f), new Vector2(58f, 30f));
                    // Pass leadership — only the leader can, and only to someone else (owner).
                    var lead = UiKit.TextButton(row.transform, "Lead", () => Boot.PartyChangeLeader(id), 13f);
                    UiKit.Place(UiKit.Rect(lead.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                new Vector2(-70f, 0f), new Vector2(58f, 30f));
                }
            }

            // Loot mode is the LEADER's to change, and the server requires a unanimous vote — so this
            // starts a vote rather than setting anything.
            if (iLead)
            {
                // A DROP-DOWN, not a cycle button (owner). Cycling meant tapping through modes you did
                // not want — and since each tap STARTS A VOTE the party has to answer, an unwanted mode
                // in passing was not free. Tapping opens the list; picking a row proposes that mode
                // directly. Tapping the header again closes it without proposing anything.
                var header = UiKit.TextButton(_partyContent, "Loot: " + LootName(Boot.PartyLoot)
                                                             + (_lootMenuOpen ? "   ^" : "   v"),
                                              () => { _lootMenuOpen = !_lootMenuOpen; _partyStamp = -1; }, 14f);
                header.gameObject.AddComponent<LayoutElement>().minHeight = 36f;

                if (_lootMenuOpen)
                    foreach (LootMode mode in System.Enum.GetValues(typeof(LootMode)))
                    {
                        if (mode == Boot.PartyLoot) continue;   // already active — nothing to propose
                        var pick = mode;
                        var row = UiKit.TextButton(_partyContent, "   propose " + LootName(pick),
                                                   () =>
                                                   {
                                                       Boot.PartySetLoot(pick);
                                                       _lootMenuOpen = false;
                                                       _partyStamp = -1;
                                                   }, 13f);
                        row.gameObject.AddComponent<LayoutElement>().minHeight = 30f;
                    }
            }
        }

        /// <summary>Is the leader's loot-mode drop-down expanded? Part of the party window's rebuild
        /// stamp, so opening it repaints.</summary>
        private bool _lootMenuOpen;

        // Effect-square geometry. Small enough that a fully-buffed member still fits two rows beside
        // their bars without the row growing.
        private const float FxSize = 20f, FxStep = 22f, FxLeft = 238f, FxTop = -4f;
        private const int FxPerRow = 3;

        /// <summary>Draw a member's buffs/debuffs as coloured squares to the right of their bars, per the
        /// 4-stage view toggle (buffs / debuffs / all / none). Green = buff, red = debuff, matching the
        /// personal buff bar; the label is the same abbreviation that bar uses so the two read alike.</summary>
        private void BuildMemberEffects(Transform row, PartyMemberDto member)
        {
            if (_partyView == 3) return;   // "none" stage

            bool showBuffs = _partyView == 0 || _partyView == 2;
            bool showDebuffs = _partyView == 1 || _partyView == 2;

            int i = 0;
            void Square(string name, bool debuff)
            {
                if (i >= FxPerRow * 2) return;   // two rows is all that fits; the rest are dropped
                var box = UiKit.Box(row, "Fx", debuff ? new Color(0.45f, 0.18f, 0.18f, 0.95f)
                                                      : new Color(0.16f, 0.36f, 0.20f, 0.95f));
                UiKit.Place(UiKit.Rect(box.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(FxLeft + (i % FxPerRow) * FxStep, FxTop - (i / FxPerRow) * FxStep),
                            new Vector2(FxSize, FxSize));
                var text = UiKit.Label(box.transform, Abbreviations.For(name), 10f,
                                       debuff ? new Color(1f, 0.72f, 0.72f) : new Color(0.72f, 0.95f, 0.76f),
                                       TextAlignmentOptions.Center);
                UiKit.Stretch(UiKit.Rect(text.gameObject), 1f, 1f, 1f, 1f);
                i++;
            }

            if (showBuffs && member.Buffs != null)
                foreach (var b in member.Buffs) Square(b, false);
            if (showDebuffs && member.Debuffs != null)
                foreach (var d in member.Debuffs) Square(d, true);
        }

        // (NextLoot is gone with the cycle button — the drop-down offers every mode directly, so there is
        // no "next" to compute and no way to tap past the one you wanted.)

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
