using System;
using System.Collections.Generic;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: the party window, its options window, the per-member hold menu and the invite
    /// prompt.
    ///
    /// The window shows itself whenever you are in a party and hides when you are not — an empty
    /// roster is exactly how the server says "you left". It is not in the window stack for that
    /// reason: there is nothing to close, only a party to leave.
    ///
    /// <para>🔑 THE WINDOW HAS NO WINDOW (owner, 2026-10-07): no panel, no border, no background — only
    /// each member's plate (name + HP + MP) with that member's effects joined under it, and the gaps between
    /// them are click-through to the world. It grows by one member's height per member, never scrolls. The
    /// plate is the only thing that SELECTS (tap) and the only thing with a menu (hold: Leave on yourself;
    /// Lead / Kick on others when you lead; nothing otherwise). A tap on an effect opens THAT member's buff
    /// card. Everything that is a setting — the effects view, the loot rule, small size — lives in the
    /// options window, opened from the one header button, which never shrinks so it stays a thumb target
    /// at either size. Drag a plate, an effect or the header to move the whole thing.</para>
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _partyPanel, _partyContent;
        private Button _partyOptionsButton;
        private readonly List<PartyRow> _partyRows = new List<PartyRow>();

        /// <summary>What the rows were last laid out from. The roster is a NEW array on every push (once a
        /// second), so a reference compare is the whole change test — and the rows are pooled and
        /// updated in place, never destroyed, so a finger mid-press on a plate keeps its plate.</summary>
        private PartyMemberDto[] _partyDrawn;
        private Guid? _partyDrawnTarget;
        private bool _partyDirty = true;

        // Member effects view: 0 buffs only, 1 debuffs only, 2 all, 3 none (owner's 4-stage toggle).
        private int _partyView = 2;
        private static readonly string[] PartyViewNames = { "Buffs", "Debuffs", "All", "None" };
        /// <summary>Half size (owner: *"decrease the whole size twice"*). The header button is exempt.</summary>
        private bool _partySmall;
        private const string PrefPartyView = "party.view", PrefPartySmall = "party.small";

        // Geometry, at full size. Six effects per row exactly span the plate, so a member's block is one
        // clean column whatever is up on them.
        private const float PartyPlateW = 240f, PartyPlateH = 46f, PartyMemberGap = 6f;
        private const float PartyHeaderW = 150f, PartyHeaderH = 30f, PartyHeaderGap = 4f;
        private const int PartyFxPerRow = 6;
        private const float PartyFxStep = PartyPlateW / PartyFxPerRow, PartyFxSize = PartyFxStep - 2f;

        private static readonly Color PartyClear = new Color(0f, 0f, 0f, 0f);
        private static readonly Color PartyDebuffTint = new Color(0.45f, 0.18f, 0.18f, 0.95f);
        private static readonly Color PartyGatedTint = new Color(0.16f, 0.16f, 0.18f, 0.95f);

        private class PartyRow
        {
            public RectTransform Root, Plate;
            public Image PlateImage, Hp, Mp;
            public TextMeshProUGUI Title;
            public readonly List<PartyFx> Fx = new List<PartyFx>();
            /// <summary>Who this row shows right now — the gestures read it at the moment they fire, so a
            /// roster that reorders between press and release acts on the member under the finger.</summary>
            public PartyMemberDto Member;
        }

        private class PartyFx
        {
            public RectTransform Root;
            public Image Box, Icon, TimeBack;
            public TextMeshProUGUI Label, Time;
            public BuffView View;
        }

        private RectTransform _invitePanel;
        private TextMeshProUGUI _inviteText;

        private void BuildPartyWindow()
        {
            _partySmall = PlayerPrefs.GetInt(PrefPartySmall, 0) == 1;
            _partyView = Mathf.Clamp(PlayerPrefs.GetInt(PrefPartyView, 2), 0, 3);

            // An invisible, NON-BLOCKING holder: only the plates, the effects and the header catch input.
            _partyPanel = UiKit.Rect(UiKit.Box(_worldRoot, "Party", PartyClear, blocksInput: false).gameObject);
            UiKit.Place(_partyPanel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(12f, -230f), new Vector2(PartyPlateW, PartyHeaderH));

            // The header: the member count AND the way into the options. Never scaled.
            _partyOptionsButton = UiKit.TextButton(_partyPanel, "", OpenPartyOptions, 14f);
            UiKit.Place(UiKit.Rect(_partyOptionsButton.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        Vector2.zero, new Vector2(PartyHeaderW, PartyHeaderH));
            _partyOptionsButton.gameObject.AddComponent<DragMove>().Target = _partyPanel;

            // The rows, scaled as one (pivot top-left, so small mode shrinks toward the header).
            _partyContent = UiKit.Rect(UiKit.Box(_partyPanel, "Rows", PartyClear, blocksInput: false).gameObject);
            UiKit.Place(_partyContent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(0f, -(PartyHeaderH + PartyHeaderGap)), new Vector2(PartyPlateW, 0f));

            _partyPanel.gameObject.SetActive(false);

            BuildPartyOptions();
            BuildPartyMenu();
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
            if (!inParty)
            {
                _partyDrawn = null;
                ClosePartyMenu();
                if (IsOpen(_partyOptionsPanel)) CloseWindow(_partyOptionsPanel);
                return;
            }

            // The SELECTED member's plate is lit, so a target change repaints too.
            if (ReferenceEquals(party, _partyDrawn) && Boot.TargetId == _partyDrawnTarget && !_partyDirty) return;
            _partyDrawn = party;
            _partyDrawnTarget = Boot.TargetId;
            _partyDirty = false;

            LayoutParty(party);
            if (IsOpen(_partyOptionsPanel)) RefreshPartyOptions(false);
        }

        private void LayoutParty(PartyMemberDto[] party)
        {
            UiKit.SetButtonText(_partyOptionsButton, "Party " + party.Length + "   options");

            float y = 0f;
            int i = 0;
            for (; i < party.Length; i++)
            {
                var member = party[i];
                var row = PartyRowAt(i);
                row.Member = member;
                row.Root.gameObject.SetActive(true);

                // Leader badge: a GOLD asterisk. ★ (U+2605) drew as a hollow "[]" box on device (playtest
                // 0.28.78) — the bundled LiberationSans has no glyph for it — so the colour carries it.
                string crown = member.IsLeader ? "<color=#FFD24A>*</color> " : "";
                string title = crown + member.Name + "   Lv " + member.Level;
                if (member.Status != PartyMemberStatus.Online) title += "   (" + member.Status + ")";
                row.Title.text = title;
                row.Title.color = member.Status == PartyMemberStatus.Online ? UiKit.Text : UiKit.TextDim;
                UiKit.SetBar(row.Hp, member.Hp, member.MaxHp);
                UiKit.SetBar(row.Mp, member.Mp, member.MaxMp);
                row.PlateImage.color = Boot.TargetId == member.Id ? UiKit.TabActive : UiKit.PanelLight;

                int fx = LayoutMemberEffects(row, member);
                float height = PartyPlateH + (fx > 0 ? 2f + ((fx - 1) / PartyFxPerRow + 1) * PartyFxStep : 0f);
                UiKit.Place(row.Root, new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(0f, -y), new Vector2(PartyPlateW, height));
                y += height + PartyMemberGap;
            }
            for (; i < _partyRows.Count; i++)
            {
                _partyRows[i].Member = null;
                _partyRows[i].Root.gameObject.SetActive(false);
            }

            float contentH = Mathf.Max(0f, y - PartyMemberGap);
            float scale = _partySmall ? 0.5f : 1f;
            _partyContent.sizeDelta = new Vector2(PartyPlateW, contentH);
            _partyContent.localScale = new Vector3(scale, scale, 1f);
            // The holder's rect is what DragMove keeps on screen, so it is sized to what is DRAWN.
            _partyPanel.sizeDelta = new Vector2(Mathf.Max(PartyHeaderW, PartyPlateW * scale),
                                                PartyHeaderH + PartyHeaderGap + contentH * scale);
        }

        private PartyRow PartyRowAt(int index)
        {
            while (_partyRows.Count <= index)
            {
                var row = new PartyRow();
                row.Root = UiKit.Rect(UiKit.Box(_partyContent, "Member", PartyClear, blocksInput: false).gameObject);

                row.PlateImage = UiKit.Box(row.Root, "Plate", UiKit.PanelLight);
                row.Plate = UiKit.Place(UiKit.Rect(row.PlateImage.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                                        Vector2.zero, new Vector2(PartyPlateW, PartyPlateH));
                // TAP selects (how you heal someone without hunting for their marker in a fight); HOLD is
                // the member's menu. PressAndHold owns both, so a hold never also selects.
                var press = row.PlateImage.gameObject.AddComponent<PressAndHold>();
                press.OnTap = () => { if (row.Member != null) Boot.TargetId = row.Member.Id; };
                press.OnHold = () => { if (row.Member != null) OpenPartyMenu(row); };
                row.PlateImage.gameObject.AddComponent<DragMove>().Target = _partyPanel;

                row.Title = UiKit.Label(row.Plate, "", 15f, UiKit.Text, TextAlignmentOptions.Left);
                UiKit.Place(UiKit.Rect(row.Title.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(8f, -4f), new Vector2(PartyPlateW - 16f, 20f));

                row.Hp = UiKit.ValueBar(row.Plate, UiKit.Hp);
                UiKit.Place(UiKit.Rect(row.Hp.transform.parent.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(8f, -26f), new Vector2(PartyPlateW - 16f, 10f));
                row.Mp = UiKit.ValueBar(row.Plate, UiKit.Mp);
                UiKit.Place(UiKit.Rect(row.Mp.transform.parent.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(8f, -38f), new Vector2(PartyPlateW - 16f, 6f));

                _partyRows.Add(row);
            }
            return _partyRows[index];
        }

        /// <summary>Lay a member's effects out six per row under their plate, per the view setting, and
        /// return how many are shown. Debuffs first — the same order as your own bar, and the ones a
        /// healer is reading for. Groups collapse exactly as they do on your bar (<see cref="BuildBuffViews"/>).</summary>
        private int LayoutMemberEffects(PartyRow row, PartyMemberDto member)
        {
            int n = 0;
            if (_partyView != 3 && member.Effects != null)
            {
                bool showBuffs = _partyView == 0 || _partyView == 2;
                bool showDebuffs = _partyView == 1 || _partyView == 2;
                var views = BuildBuffViews(member.Effects);
                for (int pass = 0; pass < 2; pass++)
                    foreach (var view in views)
                    {
                        bool debuff = view.IsDebuff || view.Row == BuffRow.Debuff;
                        if ((pass == 0) != debuff) continue;
                        if (debuff ? !showDebuffs : !showBuffs) continue;
                        DrawPartyFx(PartyFxAt(row, n), view, n);
                        n++;
                    }
            }
            for (int i = n; i < row.Fx.Count; i++)
            {
                row.Fx[i].View = null;
                row.Fx[i].Root.gameObject.SetActive(false);
            }
            return n;
        }

        private void DrawPartyFx(PartyFx fx, BuffView view, int index)
        {
            fx.View = view;
            fx.Root.gameObject.SetActive(true);
            fx.Root.anchoredPosition = new Vector2((index % PartyFxPerRow) * PartyFxStep + 1f,
                                                   -(PartyPlateH + 2f + (index / PartyFxPerRow) * PartyFxStep));

            bool debuff = view.IsDebuff || view.Row == BuffRow.Debuff;
            fx.Box.color = debuff ? PartyDebuffTint : view.Suppressed ? PartyGatedTint : UiKit.PanelLight;
            fx.Time.text = ShortTime(view.Seconds);

            // The picture when the skill has one, the initials when it does not — as on your own bar.
            var sprite = SkillSprite(view.IconId);
            fx.Icon.enabled = sprite != null;
            fx.TimeBack.enabled = sprite != null && fx.Time.text.Length > 0;
            if (sprite != null)
            {
                fx.Icon.sprite = sprite;
                fx.Icon.color = view.Suppressed ? new Color(0.5f, 0.5f, 0.5f, 0.6f) : Color.white;
                fx.Label.text = view.Stacks > 1 ? "x" + view.Stacks : "";
            }
            else
                fx.Label.text = Abbreviations.For(view.Name) + (view.Stacks > 1 ? " x" + view.Stacks : "");
            fx.Label.color = view.Suppressed ? new Color(1f, 1f, 1f, 0.40f) : Color.white;
        }

        private PartyFx PartyFxAt(PartyRow row, int index)
        {
            while (row.Fx.Count <= index)
            {
                var box = UiKit.Box(row.Root, "Fx", UiKit.PanelLight);
                var fx = new PartyFx { Root = UiKit.Rect(box.gameObject), Box = box };
                UiKit.Place(fx.Root, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero,
                            new Vector2(PartyFxSize, PartyFxSize));

                // Tap or hold: the card. A member's buff is not yours to cancel, so hold has nothing else to do.
                var press = box.gameObject.AddComponent<PressAndHold>();
                press.OnTap = () => ShowPartyBuff(row, fx);
                press.OnHold = () => ShowPartyBuff(row, fx);
                box.gameObject.AddComponent<DragMove>().Target = _partyPanel;

                fx.Icon = UiKit.Box(box.transform, "Icon", Color.white, blocksInput: false);
                UiKit.Stretch(UiKit.Rect(fx.Icon.gameObject), 2f, 2f, 2f, 2f);
                fx.Icon.preserveAspect = true;
                fx.Icon.enabled = false;

                fx.Label = UiKit.Label(box.transform, "", 12f, UiKit.Text, TextAlignmentOptions.Center);
                UiKit.Place(UiKit.Rect(fx.Label.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(0f, -2f), new Vector2(PartyFxSize, 18f));

                fx.TimeBack = UiKit.Box(box.transform, "TimeBack", new Color(0f, 0f, 0f, 0.62f), blocksInput: false);
                UiKit.Place(UiKit.Rect(fx.TimeBack.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                            new Vector2(0f, 2f), new Vector2(PartyFxSize - 4f, 12f));
                fx.TimeBack.enabled = false;

                fx.Time = UiKit.Label(box.transform, "", 10f, UiKit.TextDim, TextAlignmentOptions.Center);
                UiKit.Place(UiKit.Rect(fx.Time.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                            new Vector2(0f, 2f), new Vector2(PartyFxSize, 12f));

                row.Fx.Add(fx);
            }
            return row.Fx[index];
        }

        // ----- a member's buff card --------------------------------------------------------------

        /// <summary>The party card waiting for its description: the roster carries none (it goes to every
        /// member every second), so a tap opens the card at once and asks the server for the text.
        /// <see cref="_partyPopupKey"/> null = the open card is not a party member's.</summary>
        private Guid _partyPopupMember;
        private string _partyPopupKey;
        private BuffView _partyPopupView;
        private string _partyPopupFooter;

        private void ShowPartyBuff(PartyRow row, PartyFx fx)
        {
            var member = row.Member;
            var view = fx.View;
            if (member == null || view == null || view.Keys.Count == 0) return;

            string footer = "On " + member.Name + ".";
            ShowBuffPopup(view.Name, view.Level, view.Stacks, "", view.Seconds, footer);

            // Armed AFTER the show: ShowBuffPopup disarms, so any other card that opens later cannot be
            // overwritten by a reply that was meant for this one.
            _partyPopupMember = member.Id;
            _partyPopupKey = view.Keys[0];
            _partyPopupView = view;
            _partyPopupFooter = footer;
            Boot.RequestPartyBuffInfo(member.Id, _partyPopupKey);
        }

        /// <summary>The server's answer to a tapped member buff — fill the card if it is still the one open.</summary>
        public void ShowPartyBuffInfo(PartyBuffInfo info)
        {
            if (info == null || _partyPopupKey == null || _partyPopupView == null) return;
            if (info.MemberId != _partyPopupMember || info.Key != _partyPopupKey) return;
            if (_buffPopup == null || !_buffPopup.gameObject.activeSelf) return;

            var view = _partyPopupView;
            string desc = info.Description ?? "";
            // A split group's part list (BuildBuffViews) rides on the view, since the roster had no text.
            if (!string.IsNullOrWhiteSpace(view.Description))
                desc = (desc.Length > 0 ? desc + "\n\n" : "") + view.Description;
            if (view.Suppressed)
                desc = "INACTIVE — their weapon does not meet this skill's requirement, so it is granting "
                     + "nothing right now." + (desc.Length > 0 ? "\n\n" + desc : "");
            ShowBuffPopup(view.Name, view.Level, view.Stacks, desc, info.SecondsLeft, _partyPopupFooter);
        }

        // ----- the hold menu ---------------------------------------------------------------------

        private RectTransform _partyMenu, _partyMenuScrim, _partyMenuButtons;
        private TextMeshProUGUI _partyMenuTitle;
        private const float PartyMenuW = 200f, PartyMenuButtonH = 44f, PartyMenuTop = 34f;

        private void BuildPartyMenu()
        {
            // The same invisible scrim as the skill-slot menu: a tap anywhere else dismisses, and does
            // not fall through to walk the character while a menu is up.
            _partyMenuScrim = UiKit.Rect(UiKit.Box(_worldRoot, "PartyMenuScrim", new Color(0f, 0f, 0f, 0.001f)).gameObject);
            UiKit.Stretch(_partyMenuScrim, 0f, 0f, 0f, 0f);
            var dismiss = _partyMenuScrim.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = _partyMenuScrim.GetComponent<Image>();
            dismiss.onClick.AddListener(ClosePartyMenu);
            _partyMenuScrim.gameObject.SetActive(false);

            _partyMenu = UiKit.PanelBox(_worldRoot, "PartyMenu");
            UiKit.Place(_partyMenu, new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), Vector2.zero,
                        new Vector2(PartyMenuW, 100f));
            var inner = _partyMenu.GetChild(0);
            _partyMenuTitle = UiKit.Label(inner, "", 15f, UiKit.Accent, TextAlignmentOptions.Left);
            UiKit.Place(UiKit.Rect(_partyMenuTitle.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(10f, -6f), new Vector2(PartyMenuW - 20f, 24f));
            _partyMenuButtons = UiKit.Rect(UiKit.Box(inner, "Buttons", PartyClear, blocksInput: false).gameObject);
            UiKit.Stretch(_partyMenuButtons, 0f, PartyMenuTop, 0f, 0f);
            _partyMenu.gameObject.SetActive(false);
        }

        /// <summary>Hold on a plate. YOURSELF → Leave. Someone else, and you LEAD → Lead / Kick. Someone
        /// else, and you do not → no menu at all (owner: *"otherwise none"*).</summary>
        private void OpenPartyMenu(PartyRow row)
        {
            var member = row.Member;
            var party = Boot.Party;
            if (member == null || party == null) return;
            bool self = member.Id == Boot.SelfId;
            bool iLead = Array.Exists(party, m => m.IsLeader && m.Id == Boot.SelfId);

            var actions = new List<KeyValuePair<string, Action>>();
            var id = member.Id;
            if (self)
                actions.Add(new KeyValuePair<string, Action>("Leave party", () => Boot.PartyLeave()));
            else if (iLead)
            {
                // Pass leadership — only the leader can, and only to someone else (owner).
                actions.Add(new KeyValuePair<string, Action>("Make leader", () => Boot.PartyChangeLeader(id)));
                actions.Add(new KeyValuePair<string, Action>("Kick", () => Boot.PartyKick(id)));
            }
            if (actions.Count == 0) return;

            for (int i = _partyMenuButtons.childCount - 1; i >= 0; i--)
                Destroy(_partyMenuButtons.GetChild(i).gameObject);
            _partyMenuTitle.text = member.Name;
            for (int i = 0; i < actions.Count; i++)
            {
                var act = actions[i].Value;
                var button = UiKit.TextButton(_partyMenuButtons, actions[i].Key, () =>
                {
                    ClosePartyMenu();
                    act();
                }, 16f);
                UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(0f, -i * (PartyMenuButtonH + 6f)), new Vector2(PartyMenuW - 20f, PartyMenuButtonH));
            }
            float h = PartyMenuTop + actions.Count * (PartyMenuButtonH + 6f) + 4f;
            _partyMenu.sizeDelta = new Vector2(PartyMenuW, h);

            // Beside the plate it came from (its top-right corner), pushed back on screen if that edge is near.
            var corners = new Vector3[4];
            row.Plate.GetWorldCorners(corners);
            Vector2 at = _worldRoot.InverseTransformPoint(corners[2]);
            var area = _worldRoot.rect;
            float left = Mathf.Min(at.x + 6f, area.xMax - 4f - PartyMenuW);
            float top = Mathf.Max(at.y, area.yMin + 4f + h);
            _partyMenu.anchoredPosition = new Vector2(left, top) - area.center;

            _partyMenuScrim.gameObject.SetActive(true);
            _partyMenu.gameObject.SetActive(true);
            _partyMenuScrim.SetAsLastSibling();
            _partyMenu.SetAsLastSibling();
        }

        private void ClosePartyMenu()
        {
            if (_partyMenu == null) return;
            _partyMenu.gameObject.SetActive(false);
            _partyMenuScrim.gameObject.SetActive(false);
        }

        // ----- the options window ----------------------------------------------------------------

        private RectTransform _partyOptionsPanel, _partyOptionsBody;
        private float _partyOptionsTop;
        /// <summary>What the options were last built from — rebuilt only when one of these moves, never on
        /// the once-a-second HP push, which would destroy a button under the finger.</summary>
        private int _partyOptionsStamp = int.MinValue;
        private const float PartyOptionsW = 420f;

        private void BuildPartyOptions()
        {
            _partyOptionsPanel = UiKit.PanelBox(_worldRoot, "PartyOptions");
            UiKit.Place(_partyOptionsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(PartyOptionsW, 300f));
            _partyOptionsTop = UiKit.WindowChrome(_partyOptionsPanel, "Party options",
                                                  () => CloseWindow(_partyOptionsPanel));
            var inner = _partyOptionsPanel.GetChild(0);
            _partyOptionsBody = UiKit.Rect(UiKit.Box(inner, "Body", PartyClear, blocksInput: false).gameObject);
            UiKit.Stretch(_partyOptionsBody, 0f, _partyOptionsTop, 0f, 0f);
            _partyOptionsPanel.gameObject.SetActive(false);
        }

        private void OpenPartyOptions()
        {
            RefreshPartyOptions(true);
            OpenWindow(_partyOptionsPanel);
        }

        private void RefreshPartyOptions(bool force)
        {
            var party = Boot.Party;
            if (party == null || party.Length == 0) return;
            bool iLead = Array.Exists(party, m => m.IsLeader && m.Id == Boot.SelfId);

            int stamp = _partyView + (_partySmall ? 10 : 0) + (iLead ? 100 : 0) + (int)Boot.PartyLoot * 1000;
            if (!force && stamp == _partyOptionsStamp) return;
            _partyOptionsStamp = stamp;

            for (int i = _partyOptionsBody.childCount - 1; i >= 0; i--)
                Destroy(_partyOptionsBody.GetChild(i).gameObject);

            const float pad = 16f, rowH = 40f, gap = 6f;
            float y = 12f;
            void Heading(string text)
            {
                var label = UiKit.Label(_partyOptionsBody, text, 16f, UiKit.Accent, TextAlignmentOptions.Left);
                UiKit.Place(UiKit.Rect(label.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(pad, -y), new Vector2(PartyOptionsW - 2f * pad, 22f));
                y += 26f;
            }
            void Choices(string[] names, int active, Action<int> pick)
            {
                float w = (PartyOptionsW - 2f * pad - (names.Length - 1) * gap) / names.Length;
                for (int i = 0; i < names.Length; i++)
                {
                    int choice = i;
                    var button = UiKit.TextButton(_partyOptionsBody, names[i], () => pick(choice), 15f);
                    if (i == active) button.targetGraphic.color = UiKit.TabActive;
                    UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                                new Vector2(pad + i * (w + gap), -y), new Vector2(w, rowH));
                }
                y += rowH + 14f;
            }

            // Which of each member's effects are drawn under their plate.
            Heading("Member effects");
            Choices(PartyViewNames, _partyView, v =>
            {
                _partyView = v;
                PlayerPrefs.SetInt(PrefPartyView, v);
                PlayerPrefs.Save();
                _partyDirty = true;
                RefreshPartyOptions(true);
            });

            // Small = the whole roster at half size. The header button stays full size at both.
            Heading("Window size");
            Choices(new[] { "Normal", "Small" }, _partySmall ? 1 : 0, v =>
            {
                _partySmall = v == 1;
                PlayerPrefs.SetInt(PrefPartySmall, v);
                PlayerPrefs.Save();
                _partyDirty = true;
                RefreshPartyOptions(true);
            });

            // Loot is the LEADER's to change, and the server requires a unanimous vote — so a pick here
            // starts a vote rather than setting anything. Every mode is offered directly: a cycle button
            // would start a vote for each mode tapped past on the way.
            Heading("Loot: <color=" + LootColour(Boot.PartyLoot) + ">" + LootName(Boot.PartyLoot) + "</color>");
            if (iLead)
            {
                var modes = new List<LootMode>();
                foreach (LootMode mode in Enum.GetValues(typeof(LootMode)))
                    if (mode != Boot.PartyLoot) modes.Add(mode);   // already active — nothing to propose
                float w = (PartyOptionsW - 2f * pad - gap) / 2f;
                for (int i = 0; i < modes.Count; i++)
                {
                    var pick = modes[i];
                    var button = UiKit.TextButton(_partyOptionsBody,
                        "propose <color=" + LootColour(pick) + ">" + LootName(pick) + "</color>",
                        () => Boot.PartySetLoot(pick), 14f);
                    UiKit.Place(UiKit.Rect(button.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                                new Vector2(pad + (i % 2) * (w + gap), -(y + (i / 2) * (rowH + gap))),
                                new Vector2(w, rowH));
                }
                y += ((modes.Count + 1) / 2) * (rowH + gap) + 8f;
            }
            else
            {
                var note = UiKit.Label(_partyOptionsBody, "Only the leader can propose a change. Gold is always split evenly.",
                                       14f, UiKit.TextDim, TextAlignmentOptions.TopLeft);
                UiKit.Place(UiKit.Rect(note.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(pad, -y), new Vector2(PartyOptionsW - 2f * pad, 40f));
                y += 44f;
            }

            _partyOptionsPanel.sizeDelta = new Vector2(PartyOptionsW, _partyOptionsTop + y + 6f);
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

        /// <summary>Colour per loot mode — "random" is blue (owner referred to it as "the blue random").</summary>
        private static string LootColour(LootMode mode)
        {
            switch (mode)
            {
                case LootMode.Random:         return "#5AA0FF";   // blue
                case LootMode.RoundRobin:     return "#6BD97B";   // green
                case LootMode.LeaderOnly:     return "#FFD24A";   // gold
                default:                      return "#CFCFCF";   // finders keepers — neutral
            }
        }
    }
}
