using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Game.Shared;

namespace Game.Client.Wpf;

/// <summary>
/// Phase 4 UI: inventory/equip, second-class change, and the trade window.
/// Kept in a partial so MainWindow.xaml.cs stays focused on the world view.
/// </summary>
public partial class MainWindow
{
    private StatsUpdate? _stats;
    private float _potionCooldownEndsAt;     // _clock seconds
    private string _potionEffect = "";
    private readonly List<PotionSlot> _potionSlots = new();
    private Guid? _equipPopupInstanceId;
    private Guid? _enchantTargetId;
    private Guid? _rerollTargetId;
    private readonly HashSet<int> _rerollLocks = new();
    private bool _creationReady;

    // =======================================================================
    // Inventory
    // =======================================================================

    private void OnInventory(InventoryUpdate update)
    {
        _inventory.Clear();
        _inventory.AddRange(update.Items);

        RefreshInventoryPanel();
        RebuildPotionBar();
        if (_tradeActive)
            RefreshTradeBag();
        if (ShopPanel.Visibility == Visibility.Visible)
            RenderShop();
    }

    private void InventoryButton_Click(object sender, RoutedEventArgs e) => ToggleInventory();

    private void ToggleInventory()
    {
        InventoryPanel.Visibility = InventoryPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
        if (InventoryPanel.Visibility == Visibility.Visible)
            RefreshInventoryPanel();
    }

    private bool _invShowQuest;

    private static bool IsGearItem(InventoryItemDto item)
    {
        var d = ItemCatalog.Get(item.DefId);
        return d is not null && !ItemCatalog.IsQuestItem(d);
    }


    private void InvTabGear_Click(object sender, RoutedEventArgs e)
    {
        _invShowQuest = false;
        RefreshInventoryPanel();
    }

    private void InvTabQuest_Click(object sender, RoutedEventArgs e)
    {
        _invShowQuest = true;
        RefreshInventoryPanel();
    }

    private void RefreshInventoryPanel()
    {
        InventoryList.Items.Clear();
        InvTabGear.FontWeight = _invShowQuest ? FontWeights.Normal : FontWeights.Bold;
        InvTabQuest.FontWeight = _invShowQuest ? FontWeights.Bold : FontWeights.Normal;

        InventoryHint.Text = _invShowQuest
            ? "Quest items — cannot be dropped or traded."
            : $"{_inventory.Count(IsGearItem)}/{GameConstants.InventorySize} slots. " +
              "Click an item to equip/unequip.";

        foreach (var item in _inventory)
        {
            var def = ItemCatalog.Get(item.DefId);
            if (def is null)
                continue;

            // Tab filter: Quest tab shows quest items only; Gear tab shows the rest.
            bool isQuest = ItemCatalog.IsQuestItem(def);
            if (isQuest != _invShowQuest)
                continue;

            // Quest items: simple labelled row, no equip/enchant/remove.
            if (isQuest)
            {
                InventoryList.Items.Add(new TextBlock
                {
                    Text = $"\u2756 {def.Name}",
                    Foreground = RarityBrush(def.Rarity),
                    FontSize = 13, Margin = new Thickness(2, 0, 0, 6),
                    ToolTip = def.Name
                });
                continue;
            }

            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            var dto = item;

            // Remove (X) button — destroy item.
            var remove = new Button
            {
                Content = "X", Width = 24, Height = 28, FontSize = 11,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(120, 160, 60, 60)),
                ToolTip = "Destroy this item"
            };
            remove.Click += async (_, _) => await _net.RemoveItemAsync(dto.InstanceId);
            DockPanel.SetDock(remove, Dock.Right);
            row.Children.Add(remove);

            // Enchant (+) button — only for equippable gear.
            if (ItemCatalog.IsEquippable(def))
            {
                var enchant = new Button
                {
                    Content = "+", Width = 24, Height = 28, FontSize = 13, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White, Margin = new Thickness(0, 0, 3, 0),
                    Background = new SolidColorBrush(Color.FromArgb(120, 90, 110, 160)),
                    ToolTip = "Enchant this item"
                };
                enchant.Click += (_, _) => OpenEnchantPopup(dto);
                DockPanel.SetDock(enchant, Dock.Right);
                row.Children.Add(enchant);

                // Reroll (⟳) button — only for gear that actually has rolled attributes.
                if (dto.Attributes.Length > 0)
                {
                    var reroll = new Button
                    {
                        Content = "⟳", Width = 24, Height = 28, FontSize = 13, FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White, Margin = new Thickness(0, 0, 3, 0),
                        Background = new SolidColorBrush(Color.FromArgb(120, 150, 110, 70)),
                        ToolTip = "Reroll this item's attributes"
                    };
                    reroll.Click += (_, _) => OpenRerollPopup(dto);
                    DockPanel.SetDock(reroll, Dock.Right);
                    row.Children.Add(reroll);
                }
            }

            // Main item button — opens equip/compare popup (potions drink).
            var button = new Button
            {
                Content = ItemLabel(def, item.Equipped, item.Enchant, item.Quantity),
                Height = 28,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 0, 0, 0),
                Foreground = item.Equipped ? Brushes.LightGreen : RarityBrush(def.Rarity),
                Background = item.Equipped
                    ? new SolidColorBrush(Color.FromArgb(60, 80, 220, 120))
                    : new SolidColorBrush(Color.FromArgb(40, 80, 100, 130))
            };
            button.ToolTip = BuildItemTooltip(def, dto);
            button.Click += (_, _) => ShowEquipPopup(dto);
            row.Children.Add(button);

            InventoryList.Items.Add(row);
        }
    }

    private static string ItemLabel(ItemDef def, bool equipped, int enchant, int quantity)
    {
        string tag = equipped ? "[E] " : "";
        string ench = enchant > 0 ? $"+{enchant} " : "";
        string qty = quantity > 1 ? $"  x{(quantity >= 100 ? "99+" : quantity.ToString())}" : "";
        string req = ItemCatalog.RequiredLevel(def.Grade) > 0
            ? $" (Lv{ItemCatalog.RequiredLevel(def.Grade)})" : "";
        return $"{tag}{ench}{def.Name}  {def.Grade}/{def.Rarity}{req}{qty}";
    }

    private static string BuildItemTooltip(ItemDef def, InventoryItemDto item)
    {
        var lines = new List<string> { $"{def.Name}  {def.Grade}/{def.Rarity}" };
        if (item.Enchant > 0) lines.Add($"Enchant: +{item.Enchant}");
        if (def.AtkBonus > 0) lines.Add($"Attack +{EnchantRules.BonusAt(def.AtkBonus, item.Enchant)}");
        if (def.DefBonus > 0) lines.Add($"Defence +{EnchantRules.BonusAt(def.DefBonus, item.Enchant)}");
        if (def.HpBonus > 0) lines.Add($"Max HP +{EnchantRules.BonusAt(def.HpBonus, item.Enchant)}");
        if (def.MpBonus > 0) lines.Add($"Max MP +{EnchantRules.BonusAt(def.MpBonus, item.Enchant)}");
        if (def.EvaBonus > 0) lines.Add($"Evasion +{EnchantRules.BonusAt(def.EvaBonus, item.Enchant)}");
        if (def.WeaponRange > 0) lines.Add($"Range {def.WeaponRange:0}");
        if (item.Attributes.Length > 0)
        {
            lines.Add("— Attributes —");
            foreach (var a in item.Attributes)
                lines.Add($"{AttributeSystem.DisplayName(a.Type)} +{a.Value}{(AttributeSystem.IsPercent(a.Type) ? "%" : "")}");
        }
        if (ItemCatalog.IsBuffPotion(def) && SkillCatalog.Get(def.BuffSkillId) is SkillDef buffDef)
            lines.Add($"Use: {buffDef.Description}");
        if (!string.IsNullOrEmpty(def.SetId) && ArmorSetCatalog.Get(def.SetId) is ArmorSetDef set)
        {
            var b = set.Bonus;
            var parts = new List<string>();
            if (b.MaxHp != 0) parts.Add($"+{b.MaxHp} HP");
            if (b.MaxMp != 0) parts.Add($"+{b.MaxMp} MP");
            if (b.Defence != 0) parts.Add($"+{b.Defence} Def");
            if (b.Attack != 0) parts.Add($"+{b.Attack} Atk");
            if (b.Accuracy != 0) parts.Add($"+{b.Accuracy} Acc");
            if (b.Evasion != 0) parts.Add($"+{b.Evasion} Eva");
            lines.Add($"— {set.Name} set (full: {string.Join(", ", parts)}) —");
        }
        return string.Join("\n", lines);
    }

    private static Brush RarityBrush(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Uncommon => Brushes.LightSkyBlue,
        ItemRarity.Rare => Brushes.Gold,
        _ => Brushes.Gainsboro
    };

    // =======================================================================
    // Class change
    // =======================================================================

    // Class change is meant to happen through a quest (not yet built). For testing,
    // the debug Functions tab opens this panel directly to pick a second class.
    private void OpenClassChangePanel()
    {
        ClassOptions.Children.Clear();

        // Already have a 3rd class — nothing further (4th class isn't built yet).
        if (_myThirdClass > 0)
        {
            AppendChat(new ChatMessage("SYSTEM",
                $"You are already a {ThirdClassCatalog.Get(_myThirdClass)?.Name} (no 4th class yet).",
                ChatChannel.System));
            return;
        }

        // Have a 2nd class → offer the (debug) 3rd-class disciplines for it.
        if (_mySecondClass > 0)
        {
            ClassHint.Text = $"[DEBUG] Choose a 3rd-class discipline (skills unlock at level 40).";
            foreach (var tc in ThirdClassCatalog.ForParent(_mySecondClass))
            {
                var button = new Button
                {
                    Content = $"{tc.Name}  ({tc.Discipline})",
                    Height = 32, Margin = new Thickness(0, 0, 0, 6), FontSize = 12
                };
                int id = tc.Id;
                button.Click += async (_, _) =>
                {
                    await _net.DebugThirdClassAsync(id);
                    ClassPanel.Visibility = Visibility.Collapsed;
                };
                ClassOptions.Children.Add(button);
            }
            ClassPanel.Visibility = Visibility.Visible;
            return;
        }

        // No class yet → 2nd-class options (the real change; requires level 20).
        ClassHint.Text = $"As a {_myRace} {_myBaseClass}, choose your path. This is permanent.";
        foreach (var def in ClassCatalog.OptionsFor(_myRace, _myBaseClass))
        {
            // No stat bonus to advertise: a class change no longer raises main stats.
            var button = new Button
            {
                Content = $"{def.Name}  ({def.Archetype})",
                Height = 32,
                Margin = new Thickness(0, 0, 0, 6),
                FontSize = 12
            };
            int classId = def.Id;
            button.Click += async (_, _) =>
            {
                await _net.ChangeClassAsync(classId);
                ClassPanel.Visibility = Visibility.Collapsed;
            };
            ClassOptions.Children.Add(button);
        }

        ClassPanel.Visibility = Visibility.Visible;
    }

    private void ClassClose_Click(object sender, RoutedEventArgs e) =>
        ClassPanel.Visibility = Visibility.Collapsed;

    // One-time reminder, on reaching the class-change level, pointing the player at
    // the class-change quest chain (Elder Marius → High Priest Oren → Class Master Vael).
    private void MaybeShowClassChangeNotice(int level, int secondClass)
    {
        if (_classQuestNoticeShown || secondClass > 0 || level < GameConstants.ClassChangeLevel)
            return;
        _classQuestNoticeShown = true;
        MessageBox.Show(
            $"You have reached level {level}. Your second class awaits! Begin the trial with " +
            "Elder Marius, then seek High Priest Oren, and change class at Class Master Vael.",
            "Class Change");
    }

    // One-time reminder, on reaching the 3rd-class level, pointing a 2nd-class
    // character at Grandmaster Thorne's discipline chain.
    private void MaybeShowThirdClassNotice(int level, int secondClass, int thirdClass)
    {
        if (_thirdClassNoticeShown || secondClass == 0 || thirdClass > 0
            || level < ThirdClassCatalog.ChangeLevel)
            return;
        _thirdClassNoticeShown = true;
        MessageBox.Show(
            $"You have reached level {level}. Your discipline awaits! Seek Grandmaster Thorne " +
            "to undertake the ordeal and choose your 3rd class.",
            "Discipline");
    }

    // =======================================================================
    // Settings menu
    // =======================================================================

    private void SettingsButton_Click(object sender, RoutedEventArgs e) =>
        SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;

    private void SettingsClose_Click(object sender, RoutedEventArgs e) =>
        SettingsPanel.Visibility = Visibility.Collapsed;

    private async void SettingsCharSelect_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = Visibility.Collapsed;
        await ReturnToCharacterSelectAsync();
    }

    // Exit goes through the server (blocked in combat); the app closes only on an OK result.
    private async void SettingsExit_Click(object sender, RoutedEventArgs e) =>
        await _net.LogoutAsync();

    private async void SettingsOffline_Click(object sender, RoutedEventArgs e) =>
        await _net.StartOfflineFarmAsync();

    private void OnLogoutResult(LogoutResult r)
    {
        if (r.Ok)
            Application.Current.Shutdown();
        else
            MessageBox.Show(r.Reason, "Can't exit");
    }

    // ----- PvP toggles + reputation -----
    private bool _pvpEnabled;
    private bool _counterEnabled;
    private int _myKarma;

    private async void PvpButton_Click(object sender, RoutedEventArgs e) =>
        await _net.TogglePvpAsync(!_pvpEnabled);

    private async void CounterButton_Click(object sender, RoutedEventArgs e) =>
        await _net.ToggleCounterAttackAsync(!_counterEnabled);

    private void OnPvpState(PvpState s)
    {
        _pvpEnabled = s.Pvp;
        _counterEnabled = s.CounterAttack;
        _myKarma = s.Karma;
        PvpButton.Content = _pvpEnabled ? "PvP: On" : "PvP: Off";
        PvpButton.Background = _pvpEnabled
            ? new SolidColorBrush(Color.FromRgb(0xA0, 0x40, 0x40)) : null;
        CounterButton.Content = _counterEnabled ? "Counter: On" : "Counter: Off";
        CounterButton.Background = _counterEnabled
            ? new SolidColorBrush(Color.FromRgb(0x40, 0x70, 0xA0)) : null;
    }

    // =======================================================================
    // Trade
    // =======================================================================

    private async void TradeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_targetId is Guid id)
            await _net.TradeRequestAsync(id);
    }

    private void OnTradeRequest(TradeRequestNotice notice)
    {
        _pendingTradeFrom = notice.FromId;
        TradePromptText.Text = $"{notice.FromName} wants to trade with you.";
        TradePrompt.Visibility = Visibility.Visible;
    }

    private async void TradeAccept_Click(object sender, RoutedEventArgs e)
    {
        TradePrompt.Visibility = Visibility.Collapsed;
        _pendingTradeFrom = null;
        await _net.TradeRespondAsync(true);
    }

    private async void TradeDecline_Click(object sender, RoutedEventArgs e)
    {
        TradePrompt.Visibility = Visibility.Collapsed;
        _pendingTradeFrom = null;
        await _net.TradeRespondAsync(false);
    }

    // =======================================================================
    // Party
    // =======================================================================

    private async void PartyInviteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_targetId is Guid id)
            await _net.PartyInviteAsync(id);
    }

    private static string LootModeLabel(LootMode mode) => mode switch
    {
        LootMode.FindersKeepers => "Finders Keepers",
        LootMode.Random         => "Random",
        LootMode.RoundRobin     => "Round Robin",
        LootMode.LeaderOnly     => "Leader Only",
        _                       => mode.ToString(),
    };

    private DispatcherTimer? _partyInviteTimer;

    private void OnPartyInvite(PartyInviteDto invite)
    {
        _pendingPartyFrom = invite.InviterId;
        PartyInviteText.Text =
            $"{invite.InviterName} invites you to a party.\nLoot rule: {LootModeLabel(invite.LootMode)}";
        PartyInvitePrompt.Visibility = Visibility.Visible;

        // Auto-dismiss after the server-side invite timeout (~30s) so a stale prompt doesn't linger.
        _partyInviteTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _partyInviteTimer.Stop();
        _partyInviteTimer.Tick -= PartyInviteTimedOut;
        _partyInviteTimer.Tick += PartyInviteTimedOut;
        _partyInviteTimer.Start();
    }

    private void PartyInviteTimedOut(object? sender, EventArgs e)
    {
        _partyInviteTimer?.Stop();
        PartyInvitePrompt.Visibility = Visibility.Collapsed;
        _pendingPartyFrom = null;
    }

    /// <summary>Leader proposed a loot-rule change and needs my agreement (Open), or the vote just
    /// resolved and the prompt should close.</summary>
    private void OnPartyLootVote(PartyLootVoteDto vote)
    {
        if (!vote.Open)
        {
            LootVotePrompt.Visibility = Visibility.Collapsed;
            return;
        }
        LootVoteText.Text =
            $"{vote.RequestedBy} wants to change the loot rule to {LootModeLabel(vote.Mode)}.";
        LootVotePrompt.Visibility = Visibility.Visible;
    }

    private async void LootVoteAccept_Click(object sender, RoutedEventArgs e)
    {
        LootVotePrompt.Visibility = Visibility.Collapsed;
        await _net.PartyLootVoteAsync(true);
    }

    private async void LootVoteDecline_Click(object sender, RoutedEventArgs e)
    {
        LootVotePrompt.Visibility = Visibility.Collapsed;
        await _net.PartyLootVoteAsync(false);
    }

    private async void PartyAccept_Click(object sender, RoutedEventArgs e)
    {
        _partyInviteTimer?.Stop();
        PartyInvitePrompt.Visibility = Visibility.Collapsed;
        _pendingPartyFrom = null;
        await _net.PartyRespondAsync(true);
    }

    private async void PartyDecline_Click(object sender, RoutedEventArgs e)
    {
        _partyInviteTimer?.Stop();
        PartyInvitePrompt.Visibility = Visibility.Collapsed;
        _pendingPartyFrom = null;
        await _net.PartyRespondAsync(false);
    }

    private async void PartyLeaveButton_Click(object sender, RoutedEventArgs e) =>
        await _net.PartyLeaveAsync();

    /// <summary>Rebuild the party roster from the server's authoritative snapshot. An empty
    /// roster means I left / the party disbanded, so the window hides.</summary>
    private void OnParty(PartyUpdate update)
    {
        _partyMemberIds.Clear();
        _partyIsLeader = false;
        PartyMembers.Children.Clear();

        if (update.Members.Length == 0)
        {
            PartyPanel.Visibility = Visibility.Collapsed;
            LootVotePrompt.Visibility = Visibility.Collapsed;   // any open vote is moot
            UpdateTargetFrame();   // invite button may become available again
            return;
        }

        foreach (var m in update.Members)
        {
            _partyMemberIds.Add(m.Id);
            if (m.Id == _myId && m.IsLeader)
                _partyIsLeader = true;
        }

        // I can only kick if I'm the leader; never show a kick button on myself.
        foreach (var m in update.Members)
            PartyMembers.Children.Add(BuildPartyRow(m, _partyIsLeader && m.Id != _myId));

        // Reflect the server's loot rule; only the leader may change it.
        _suppressLootCombo = true;
        PartyLootCombo.SelectedIndex = (int)update.LootMode;
        PartyLootCombo.IsEnabled = _partyIsLeader;
        _suppressLootCombo = false;

        PartyPanel.Visibility = Visibility.Visible;
        UpdateTargetFrame();       // hide the invite button for people already grouped
    }

    private FrameworkElement BuildPartyRow(PartyMemberDto m, bool showKick)
    {
        double hp = m.MaxHp > 0 ? Math.Clamp((double)m.Hp / m.MaxHp, 0, 1) : 0;
        double mp = m.MaxMp > 0 ? Math.Clamp((double)m.Mp / m.MaxMp, 0, 1) : 0;
        const double barWidth = 204;

        // AFK indicator: a small tag + tint for auto-hunting (idle) or offline-farming members, so
        // the party can tell an AFK player from a network drop and decide whether to kick.
        string statusTag = m.Status switch
        {
            PartyMemberStatus.Auto    => "  • AFK",
            PartyMemberStatus.Offline => "  • OFFLINE",
            _                         => ""
        };
        var statusBrush = m.Status switch
        {
            PartyMemberStatus.Auto    => new SolidColorBrush(Color.FromRgb(0xE0, 0xD0, 0x60)),
            PartyMemberStatus.Offline => new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)),
            _                         => (m.Id == _myId ? Brushes.Gold : Brushes.White)
        };
        var header = new TextBlock
        {
            Text = $"{(m.IsLeader ? "★ " : "")}{m.Name}  Lv{m.Level} {m.ClassName}{statusTag}",
            Foreground = statusBrush,
            FontSize = 11, VerticalAlignment = VerticalAlignment.Center,
            ToolTip = m.Status switch
            {
                PartyMemberStatus.Auto    => "Auto-hunting (AFK)",
                PartyMemberStatus.Offline => "Offline — auto-farming while disconnected",
                _                         => null
            }
        };

        var kick = new Button
        {
            Content = "✕", Width = 16, Height = 16, Padding = new Thickness(0),
            FontSize = 9, HorizontalAlignment = HorizontalAlignment.Right,
            ToolTip = $"Remove {m.Name} from the party",
            Visibility = showKick ? Visibility.Visible : Visibility.Collapsed,
            Tag = m.Id
        };
        kick.Click += PartyKick_Click;

        var titleRow = new Grid();
        titleRow.Children.Add(header);
        titleRow.Children.Add(kick);

        var hpFill = new Rectangle
        {
            Fill = new SolidColorBrush(Color.FromRgb(0xC9, 0x3C, 0x3C)),
            Height = 6, Width = barWidth * hp,
            HorizontalAlignment = HorizontalAlignment.Left, RadiusX = 2, RadiusY = 2
        };
        var mpFill = new Rectangle
        {
            Fill = new SolidColorBrush(Color.FromRgb(0x3F, 0x7F, 0xD8)),
            Height = 5, Width = barWidth * mp,
            HorizontalAlignment = HorizontalAlignment.Left, RadiusX = 2, RadiusY = 2
        };
        var hpBar = new Border
        {
            Height = 6, Margin = new Thickness(0, 2, 0, 1), CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), Child = hpFill
        };
        var mpBar = new Border
        {
            Height = 5, CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), Child = mpFill
        };

        var row = new StackPanel { Margin = new Thickness(0, 3, 0, 3) };
        row.Children.Add(titleRow);
        row.Children.Add(hpBar);
        row.Children.Add(mpBar);
        return row;
    }

    private async void PartyKick_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Guid id })
            await _net.PartyKickAsync(id);
    }

    private async void PartyLootCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressLootCombo || !_partyIsLeader)
            return;
        await _net.PartySetLootModeAsync((LootMode)PartyLootCombo.SelectedIndex);
    }

    private void OnTradeState(TradeStateUpdate state)
    {
        _tradeActive = state.Active;

        if (!state.Active)
        {
            TradeWindow.Visibility = Visibility.Collapsed;
            _myTradeOffer.Clear();
            return;
        }

        TradeWindow.Visibility = Visibility.Visible;
        TradeTitle.Text = $"Trading with {state.PartnerName}";
        TheirOfferLabel.Text = $"{state.PartnerName}'s offer" +
            (state.TheirReady ? "  (READY)" : "");

        // Mirror the server's authoritative view of my offer.
        _myTradeOffer.Clear();
        foreach (var item in state.MyOffer)
            _myTradeOffer.Add(item.InstanceId);

        FillOfferList(MyOfferList, state.MyOffer, removable: true);
        FillOfferList(TheirOfferList, state.TheirOffer, removable: false);
        RefreshTradeBag();

        TradeReadyButton.Content = state.MyReady ? "Ready ✓" : "Ready";
        TradeReadyButton.Background = state.MyReady
            ? new SolidColorBrush(Color.FromArgb(80, 80, 220, 120))
            : Brushes.LightGray;
    }

    private void FillOfferList(ItemsControl list, InventoryItemDto[] items, bool removable)
    {
        list.Items.Clear();
        foreach (var item in items)
        {
            var def = ItemCatalog.Get(item.DefId);
            if (def is null)
                continue;

            var button = new Button
            {
                Content = $"{def.Name}{(item.Quantity > 1 ? $" x{item.Quantity}" : "")}  {def.Grade}/{def.Rarity}",
                Height = 26,
                Margin = new Thickness(0, 0, 0, 3),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(6, 0, 0, 0),
                Foreground = RarityBrush(def.Rarity),
                IsEnabled = removable
            };
            if (removable)
            {
                var instanceId = item.InstanceId;
                button.Click += async (_, _) =>
                {
                    _myTradeOffer.Remove(instanceId);
                    await _net.TradeOfferAsync(_myTradeOffer.ToArray());
                };
            }
            list.Items.Add(button);
        }
    }

    private void RefreshTradeBag()
    {
        TradeBagList.Items.Clear();
        foreach (var item in _inventory)
        {
            if (item.Equipped || _myTradeOffer.Contains(item.InstanceId))
                continue;

            var def = ItemCatalog.Get(item.DefId);
            if (def is null)
                continue;

            var button = new Button
            {
                Content = $"{def.Name}{(item.Quantity > 1 ? $" x{item.Quantity}" : "")}  {def.Grade}/{def.Rarity}",
                Height = 26,
                Margin = new Thickness(0, 0, 0, 3),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(6, 0, 0, 0),
                Foreground = RarityBrush(def.Rarity)
            };
            var instanceId = item.InstanceId;
            button.Click += async (_, _) =>
            {
                if (_myTradeOffer.Count >= GameConstants.TradeMaxOfferSlots)
                    return;
                _myTradeOffer.Add(instanceId);
                await _net.TradeOfferAsync(_myTradeOffer.ToArray());
            };
            TradeBagList.Items.Add(button);
        }
    }

    private async void TradeReady_Click(object sender, RoutedEventArgs e) =>
        await _net.TradeReadyAsync();

    private async void TradeWindowCancel_Click(object sender, RoutedEventArgs e) =>
        await _net.TradeCancelAsync();

    // =======================================================================
    // Stats window
    // =======================================================================

    private void OnStats(StatsUpdate stats)
    {
        _stats = stats;
        _skillPoints = stats.SkillPoints;
        _moveState = stats.MoveState;
        if (StatsPanel.Visibility == Visibility.Visible)
            RefreshStatsPanel();
        if (SkillsPanel.Visibility == Visibility.Visible)
            SkillPointsText.Text = $"SP: {_skillPoints}";
        UpdateMoveStateIndicator();
    }

    private void UpdateMoveStateIndicator()
    {
        string label = _moveState switch
        {
            MoveState.Walking => "Walking",
            MoveState.Sitting => "Sitting",
            _ => "Running"
        };
        MoveStateText.Text = $"{label}  (Z sit / X walk)";
    }

    private void StatsButton_Click(object sender, RoutedEventArgs e) => ToggleStats();

    private void ToggleStats()
    {
        StatsPanel.Visibility = StatsPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
        if (StatsPanel.Visibility == Visibility.Visible)
            RefreshStatsPanel();
    }

    private void StatsClose_Click(object sender, RoutedEventArgs e) =>
        StatsPanel.Visibility = Visibility.Collapsed;

    private void RefreshStatsPanel()
    {
        StatsList.Items.Clear();
        if (_stats is not StatsUpdate st)
        {
            StatsList.Items.Add(MakeStatRow("Stats not loaded yet", ""));
            return;
        }

        string cls = _myThirdClass > 0
            ? ThirdClassCatalog.Get(_myThirdClass)?.Name ?? "-"
            : st.SecondClass > 0
                ? ClassCatalog.Get(st.SecondClass)?.Name ?? "-"
                : $"{_myBaseClass} (base)";

        StatsList.Items.Add(MakeStatRow("Class", cls));
        StatsList.Items.Add(MakeStatRow("CON / ATK / WIT / DEX",
            $"{st.Con} / {st.Atk} / {st.Wit} / {st.Dex}"));
        StatsList.Items.Add(MakeStatRow("Max HP / MP", $"{st.MaxHp} / {st.MaxMp}"));
        StatsList.Items.Add(MakeStatRow("P.Atk / M.Atk", $"{st.AttackPower} / {st.MagicAttack}"));
        StatsList.Items.Add(MakeStatRow("Defence (Phys / Magic)", $"{st.Defence} / {st.MagicDefence}"));
        if (!string.IsNullOrEmpty(st.ActiveSet))
            StatsList.Items.Add(MakeStatRow("Set Bonus", $"{st.ActiveSet} (complete)"));
        if (!string.IsNullOrEmpty(st.ArmorMastery))
            StatsList.Items.Add(MakeStatRow("Armor", st.ArmorMastery));
        StatsList.Items.Add(MakeStatRow("Accuracy / Evasion", $"{st.Accuracy} / {st.Evasion}"));
        StatsList.Items.Add(MakeStatRow("Crit (Phys / Magic)",
            $"{st.CritChance * 100:0.#}% / {st.MagicCritChance * 100:0.#}%"));
        if (st.HasShield)
            StatsList.Items.Add(MakeStatRow("Block (chance / reduce / def)",
                $"{st.BlockChance * 100:0.#}% / {st.BlockReduction * 100:0.#}% / +{st.ShieldDefense}"));
        StatsList.Items.Add(MakeStatRow("Attack Range", $"{st.BasicAttackRange:0}"));
        StatsList.Items.Add(MakeStatRow("Move Speed", $"{st.MoveSpeed:0}"));

        // CastSpeedMult / AttackSpeedMult are the EFFECTIVE multipliers (WIT/DEX +
        // gear + masteries + buffs/potions all folded in; lower = faster). Show as
        // the L2-style "speed stat / cap" (333 = 1.0x), with % faster for context.
        StatsList.Items.Add(MakeStatRow("Cast Speed",
            SpeedStatLabel(st.CastSpeedMult, StatCaps.CastSpeed)));
        StatsList.Items.Add(MakeStatRow("Attack Speed",
            SpeedStatLabel(st.AttackSpeedMult, StatCaps.AttackSpeed)));

        // ----- Extended / debug stats (regens + the buff-effect layer) -----
        StatsList.Items.Add(MakeStatRow("HP / MP Regen", $"{st.HpRegen:0.#} / {st.MpRegen:0.#} per s"));
        StatsList.Items.Add(MakeStatRow("Crit Damage", $"x{2f + st.CritDamage:0.##}"));
        if (st.MeleeVamp > 0 || st.SpellVamp > 0)
            StatsList.Items.Add(MakeStatRow("Vampiric (melee / spell)",
                $"{st.MeleeVamp * 100:0.#}% / {st.SpellVamp * 100:0.#}%"));
        if (st.CooldownReduction > 0)
            StatsList.Items.Add(MakeStatRow("Reuse Reduction", $"{st.CooldownReduction * 100:0.#}%"));
        StatsList.Items.Add(MakeStatRow("Interrupt Resist", $"{st.InterruptResist}"));
        if (st.MagicFailResist > 0 || st.MagicFailFloor > 0)
            StatsList.Items.Add(MakeStatRow("Spell Fail (resist / vs you)",
                $"{st.MagicFailResist * 100:0.#}% / {st.MagicFailFloor * 100:0.#}%"));
        if (st.CritRateResist > 0 || st.CritDmgResist > 0 || st.BowResist > 0)
            StatsList.Items.Add(MakeStatRow("Resists (critRate/critDmg/bow)",
                $"{st.CritRateResist * 100:0.#}% / {st.CritDmgResist * 100:0.#}% / {st.BowResist * 100:0.#}%"));
    }

    /// <summary>Format an effective time-multiplier (lower = faster) as the L2-style
    /// "speed stat / cap" pair (333 = 1.0x), e.g. "1206 / 1999  (+72% )".</summary>
    private static string SpeedStatLabel(float mult, int cap)
    {
        mult = Math.Max(0.0001f, mult);
        int stat = (int)Math.Round(StatCalculator.SpeedBaseline / mult);
        float faster = (1f - mult) * 100f;
        string pct = faster >= 0 ? $"+{faster:0.#}% faster" : $"{faster:0.#}% slower";
        return $"{stat} / {cap}   ({pct})";
    }

    private static Grid MakeStatRow(string label, string value)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        var l = new TextBlock { Text = label, Foreground = Brushes.Gainsboro, FontSize = 12 };
        var v = new TextBlock { Text = value, Foreground = Brushes.White, FontSize = 12,
            FontWeight = FontWeights.SemiBold };
        Grid.SetColumn(v, 1);
        grid.Children.Add(l);
        grid.Children.Add(v);
        return grid;
    }

    // =======================================================================
    // Potions (action bar + inventory + hotkeys Q/E)
    // =======================================================================

    private void OnPotion(PotionStatus status)
    {
        _potionCooldownEndsAt = (float)_clock.Elapsed.TotalSeconds + status.CooldownSeconds;
        _potionEffect = status.ActiveEffect;
    }

    private async void UsePotionHotkey(int index)
    {
        // Q -> first potion square (Minor), E -> second (Healing).
        if (index < 0 || index >= PotionSquares.Length)
            return;
        string defId = PotionSquares[index].DefId;
        var item = _inventory.FirstOrDefault(i => i.DefId == defId);
        if (item is not null)
            await DrinkPotion(item.InstanceId);
    }

    private async Task DrinkPotion(Guid instanceId)
    {
        if (_clock.Elapsed.TotalSeconds < _potionCooldownEndsAt)
            return;
        await _net.UsePotionAsync(instanceId);
    }

    // =======================================================================
    // Equip comparison popup
    // =======================================================================

    private void ShowEquipPopup(InventoryItemDto item)
    {
        if (ItemCatalog.Get(item.DefId) is not ItemDef def)
            return;

        // Potions don't compare — drink directly from inventory click. Buff potions
        // ignore the heal-potion cooldown (they apply a timed buff instead).
        if (ItemCatalog.IsPotion(def))
        {
            if (ItemCatalog.IsBuffPotion(def))
                _ = _net.UsePotionAsync(item.InstanceId);
            else
                _ = DrinkPotion(item.InstanceId);
            return;
        }

        // Boxes/chests open on click (roll their loot table server-side).
        if (ItemCatalog.IsBox(def))
        {
            _ = _net.OpenBoxAsync(item.InstanceId);
            return;
        }

        _equipPopupInstanceId = item.InstanceId;
        EquipPopupTitle.Text = item.Equipped ? $"Unequip {def.Name}" : def.Name;
        EquipPopupSubtitle.Text =
            $"{def.Grade}/{def.Rarity}" +
            (ItemCatalog.RequiredLevel(def.Grade) > 0 ? $"  •  requires Lv{ItemCatalog.RequiredLevel(def.Grade)}" : "");

        // Find the currently equipped item in the same slot to diff against. For
        // armor the body-part slot must match too (compare a helmet vs a helmet).
        var current = _inventory
            .Select(i => (Item: i, Def: ItemCatalog.Get(i.DefId)))
            .FirstOrDefault(t => t.Item.Equipped && t.Def is not null &&
                                 t.Def!.Slot == def.Slot && t.Item.InstanceId != item.InstanceId &&
                                 (def.Slot != EquipSlot.Armor || t.Def!.ArmorSlot == def.ArmorSlot));

        // The clicked item is the SUBJECT — always show ITS real stats. The
        // delta column compares against whatever is equipped in that slot
        // (empty if nothing, or if you clicked the equipped item itself).
        bool isEquippedItem = item.Equipped;
        bool hasOther = current.Item is not null;
        int subjectEnch = item.Enchant;
        int otherEnch = current.Item?.Enchant ?? 0;

        // "Other" = the equipped item we diff against (none when clicking the
        // equipped item, so the delta column simply hides).
        int Subj(int b) => EnchantRules.BonusAt(b, subjectEnch);
        int Other(int b) => EnchantRules.BonusAt(b, otherEnch);
        bool showDelta = hasOther && !isEquippedItem;

        EquipCompareList.Items.Clear();
        AddStatRow2("Attack", Subj(def.AtkBonus), showDelta ? Subj(def.AtkBonus) - Other(current.Def!.AtkBonus) : (int?)null);
        AddStatRow2("Defence", Subj(def.DefBonus), showDelta ? Subj(def.DefBonus) - Other(current.Def!.DefBonus) : (int?)null);
        AddStatRow2("M.Def", Subj(def.MDefBonus), showDelta ? Subj(def.MDefBonus) - Other(current.Def!.MDefBonus) : (int?)null);
        AddStatRow2("Max HP", Subj(def.HpBonus), showDelta ? Subj(def.HpBonus) - Other(current.Def!.HpBonus) : (int?)null);
        AddStatRow2("Max MP", Subj(def.MpBonus), showDelta ? Subj(def.MpBonus) - Other(current.Def!.MpBonus) : (int?)null);
        AddStatRow2("Evasion", Subj(def.EvaBonus), showDelta ? Subj(def.EvaBonus) - Other(current.Def!.EvaBonus) : (int?)null);
        if (def.WeaponRange > 0)
            AddStatRow2("Range", (int)def.WeaponRange,
                showDelta ? (int)def.WeaponRange - (int)(current.Def?.WeaponRange ?? 0) : (int?)null);

        // Rolled attributes on THIS item instance.
        if (item.Attributes.Length > 0)
        {
            EquipCompareList.Items.Add(new TextBlock
            {
                Text = "Attributes:", Foreground = Brushes.Gold, FontSize = 12,
                FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 2)
            });
            foreach (var attr in item.Attributes)
                AddStatRow2($"  {AttributeSystem.DisplayName(attr.Type)}", attr.Value, null,
                    suffix: AttributeSystem.IsPercent(attr.Type) ? "%" : "");
        }

        EquipConfirmButton.Content = item.Equipped ? "Unequip" : "Equip";
        EquipPopup.Visibility = Visibility.Visible;
    }

    /// <summary>Row showing an item's own stat value plus an optional delta
    /// (vs the equipped item). Delta null = no comparison column.</summary>
    private void AddStatRow2(string label, int value, int? delta, string suffix = "")
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        var l = new TextBlock { Text = label, Foreground = Brushes.Gainsboro, FontSize = 12 };
        var v = new TextBlock
        {
            Text = $"{value}{suffix}", Foreground = Brushes.White, FontSize = 12,
            FontWeight = FontWeights.SemiBold
        };
        Grid.SetColumn(v, 1);
        grid.Children.Add(l);
        grid.Children.Add(v);

        if (delta is int d && d != 0)
        {
            var deltaText = new TextBlock
            {
                Text = d > 0 ? $"(+{d})" : $"({d})",
                Foreground = d > 0 ? Brushes.LightGreen : Brushes.IndianRed,
                FontSize = 12, FontWeight = FontWeights.SemiBold
            };
            Grid.SetColumn(deltaText, 2);
            grid.Children.Add(deltaText);
        }

        EquipCompareList.Items.Add(grid);
    }

    /// <summary>Row showing current -> new with a colored delta.</summary>
    private void AddCompareRow(string label, int current, int candidate, bool hasCurrent)
    {
        int delta = candidate - current;

        var grid = new Grid { Margin = new Thickness(0, 0, 0, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        var l = new TextBlock { Text = label, Foreground = Brushes.Gainsboro, FontSize = 12 };
        var mid = new TextBlock
        {
            Text = hasCurrent ? $"{current}  ->  {candidate}" : $"{candidate}",
            Foreground = Brushes.White, FontSize = 12
        };
        Grid.SetColumn(mid, 1);

        var deltaText = new TextBlock
        {
            Text = delta == 0 ? "" : (delta > 0 ? $"+{delta}" : delta.ToString()),
            Foreground = delta > 0 ? Brushes.LightGreen : delta < 0 ? Brushes.IndianRed : Brushes.Gray,
            FontSize = 12, FontWeight = FontWeights.SemiBold
        };
        Grid.SetColumn(deltaText, 2);

        grid.Children.Add(l);
        grid.Children.Add(mid);
        grid.Children.Add(deltaText);
        EquipCompareList.Items.Add(grid);
    }

    private async void EquipConfirm_Click(object sender, RoutedEventArgs e)
    {
        if (_equipPopupInstanceId is Guid id)
            await _net.EquipItemAsync(id);
        EquipPopup.Visibility = Visibility.Collapsed;
        _equipPopupInstanceId = null;
    }

    private void EquipPopupClose_Click(object sender, RoutedEventArgs e)
    {
        EquipPopup.Visibility = Visibility.Collapsed;
        _equipPopupInstanceId = null;
    }

    private class PotionSlot
    {
        public required Button Button { get; init; }
        public required Guid InstanceId { get; init; }
    }


    // =======================================================================
    // Character creation — class tree
    // =======================================================================

    private static IEnumerable<Race> SelectableRaces()
    {
        foreach (var race in Enum.GetValues<Race>())
        {
#if !DEBUG
            if (race == Race.God) continue;   // God race only creatable in DEBUG
#endif
            yield return race;
        }
    }

    private void BuildCreationTree()
    {
        CreationTree.Children.Clear();
        AddTreeHeader("1. Choose a Race");

        foreach (var race in SelectableRaces())
        {
            var btn = TreeButton(race.ToString(), 0);
            btn.Click += (_, _) => SelectRace(race);
            CreationTree.Children.Add(btn);
        }

        ShowCreationInfo("Welcome",
            "Pick a race, then a base class, then preview a second class. " +
            "Races set your starting stats; base class sets your playstyle.\n\n" +
            "Ork/Demon: high CON/ATK, low WIT/DEX — brawlers.\n" +
            "Elf/Angel: high DEX/WIT, low CON/ATK — finesse & magic.\n" +
            "Human: balanced.", null);
    }

    private void SelectRace(Race race)
    {
        _myRace = race;
        _creationReady = false;
        ConnectButton.IsEnabled = false;

        BuildCreationTree(); // reset, then expand under the chosen race
        // Re-render with race expanded.
        CreationTree.Children.Clear();
        AddTreeHeader("1. Race");
        foreach (var r in SelectableRaces())
        {
            var btn = TreeButton(r.ToString(), 0, selected: r == race);
            btn.Click += (_, _) => SelectRace(r);
            CreationTree.Children.Add(btn);
        }

        AddTreeHeader("2. Choose Base Class");
        foreach (var bc in Enum.GetValues<BaseClass>())
        {
            var btn = TreeButton(bc.ToString(), 1);
            btn.Click += (_, _) => SelectBaseClass(race, bc);
            CreationTree.Children.Add(btn);
        }

        var stats = StatCalculator.GetBaseStats(race, BaseClass.Fighter);
        var mstats = StatCalculator.GetBaseStats(race, BaseClass.Mage);
        ShowCreationInfo($"{race}",
            $"Fighter base stats: CON {stats.Con}, ATK {stats.Atk}, WIT {stats.Wit}, DEX {stats.Dex}\n" +
            $"Mage base stats:    CON {mstats.Con}, ATK {mstats.Atk}, WIT {mstats.Wit}, DEX {mstats.Dex}\n\n" +
            "Now choose Fighter or Mage to see its second-class paths.", null);
    }

    private void SelectBaseClass(Race race, BaseClass baseClass)
    {
        _myRace = race;
        _myBaseClass = baseClass;
        _creationReady = true;
        ConnectButton.IsEnabled = true; // you can create now; 2nd class is preview only

        CreationTree.Children.Clear();
        AddTreeHeader("1. Race");
        var rb = TreeButton(race.ToString(), 0, selected: true);
        rb.Click += (_, _) => SelectRace(race);
        CreationTree.Children.Add(rb);

        AddTreeHeader("2. Base Class");
        foreach (var bc in Enum.GetValues<BaseClass>())
        {
            var btn = TreeButton(bc.ToString(), 1, selected: bc == baseClass);
            btn.Click += (_, _) => SelectBaseClass(race, bc);
            CreationTree.Children.Add(btn);
        }

        AddTreeHeader("3. Preview Second Class (at Lv20)");
        foreach (var sc in ClassCatalog.OptionsFor(race, baseClass))
        {
            var btn = TreeButton($"{sc.Name}  ({sc.Archetype})", 2);
            btn.Click += (_, _) => PreviewSecondClass(sc);
            CreationTree.Children.Add(btn);
        }

        var stats = StatCalculator.GetBaseStats(race, baseClass);
        string passive = baseClass == BaseClass.Fighter
            ? "Fighters: lower HP, more attack/defence focus; can use all armor."
            : "Mages: spell-casters; WIT shortens cast time; robe specialists.";
        var skills = ClassSkills.ForClass(race, baseClass, null)
            .Select(cs => SkillCatalog.Get(cs.SkillId))
            .Where(d => d is not null).Select(d => d!).ToList();
        ShowCreationInfo($"{race} {baseClass}",
            $"Base stats: CON {stats.Con}, ATK {stats.Atk}, WIT {stats.Wit}, DEX {stats.Dex}\n\n" +
            passive + "\n\nStarting skills:", skills);
    }

    private void PreviewSecondClass(SecondClassDef sc)
    {
        string role = sc.Archetype switch
        {
            Archetype.Tank => "Fortress: heavy armor + shield, soaks damage.",
            Archetype.Warrior => "Heavy 2-hander: huge hits, less defence than a tank.",
            Archetype.Rogue => "Fast dual-wield melee: evasion, crits, DoTs.",
            Archetype.Archer => "Ranged bow: +500 attack range (cap 1100), heavy hits.",
            Archetype.Healer => "Robe support: heals/buffs, +500 spell range (cap 900).",
            Archetype.Nuker => "Robe caster: big damage spells, +500 spell range.",
            _ => ""
        };
        var skills = ClassSkills.ForClass(sc.Race, sc.Base, sc.Archetype)
            .Select(cs => SkillCatalog.Get(cs.SkillId))
            .Where(d => d is not null).Select(d => d!).ToList();
        ShowCreationInfo($"{sc.Name}  ({sc.Archetype})",
            role + "\n\nYour main stats do not change — this class is defined by its skills "
                 + "and the gear it can use.\n\nSkills (base + signature):", skills);
    }

    private void ShowCreationInfo(string title, string body, List<SkillDef>? skills)
    {
        CreationTitle.Text = title;
        CreationBody.Text = body;
        CreationSkills.Items.Clear();
        if (skills is null)
            return;

        foreach (var def in skills)
        {
            var tb = new TextBlock
            {
                Foreground = Brushes.LightSkyBlue, FontSize = 12,
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0),
                Text = $"• {def.Name} — {SkillCatalog.DescriptionOf(def.Id)}"
            };
            CreationSkills.Items.Add(tb);
        }
    }

    private void AddTreeHeader(string text) =>
        CreationTree.Children.Add(new TextBlock
        {
            Text = text, Foreground = Brushes.Gray, FontSize = 11,
            FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 4)
        });

    private static System.Windows.Controls.Button TreeButton(string text, int indent, bool selected = false)
    {
        return new System.Windows.Controls.Button
        {
            Content = text,
            Height = 30,
            Margin = new Thickness(indent * 16, 0, 0, 4),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(8, 0, 0, 0),
            FontSize = 12,
            Background = selected
                ? new SolidColorBrush(Color.FromArgb(110, 90, 150, 220))
                : new SolidColorBrush(Color.FromArgb(40, 80, 100, 130)),
            Foreground = Brushes.White
        };
    }

    // =======================================================================
    // Skills window
    // =======================================================================

    private void SkillsButton_Click(object sender, RoutedEventArgs e) => ToggleSkills();
    private void SkillsClose_Click(object sender, RoutedEventArgs e) =>
        SkillsPanel.Visibility = Visibility.Collapsed;

    private void ToggleSkills()
    {
        SkillsPanel.Visibility = SkillsPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
        if (SkillsPanel.Visibility == Visibility.Visible)
            RefreshSkillsWindow();
    }

    private bool _skillTabLearn; // false = Learned, true = Learn
    private string? _pendingLearnId;

    private void SkillTabLearned_Click(object sender, RoutedEventArgs e)
    {
        _skillTabLearn = false;
        RefreshSkillsWindow();
    }

    private void SkillTabLearn_Click(object sender, RoutedEventArgs e)
    {
        _skillTabLearn = true;
        RefreshSkillsWindow();
    }

    private Archetype? CurrentArchetype =>
        _mySecondClass > 0 ? ClassCatalog.Get(_mySecondClass)?.Archetype : null;

    private Discipline? CurrentDiscipline =>
        _myThirdClass > 0 ? ThirdClassCatalog.Get(_myThirdClass)?.Discipline : null;

    private void RefreshSkillsWindow()
    {
        SkillsList.Items.Clear();
        SkillPointsText.Text = $"SP: {_skillPoints}";

        // Tab highlight.
        TabLearned.FontWeight = _skillTabLearn ? FontWeights.Normal : FontWeights.Bold;
        TabLearn.FontWeight = _skillTabLearn ? FontWeights.Bold : FontWeights.Normal;

        if (_skillTabLearn)
            BuildLearnTab();
        else
            BuildLearnedTab();
    }

    /// <summary>Tab 1: skills you've learned, grouped by category, usable/bar-able.</summary>
    private void BuildLearnedTab()
    {
        var learned = _learnedSkills
            .Select(id => SkillCatalog.Get(id))
            .Where(d => d is not null).Select(d => d!)
            .OrderBy(d => d.Category).ThenBy(d => d.Name)
            .ToList();

        if (learned.Count == 0)
        {
            SkillsList.Items.Add(new TextBlock
            {
                Text = "No skills learned yet. Check the 'Skills to Learn' tab.",
                Foreground = Brushes.Gray, Margin = new Thickness(0, 6, 0, 0)
            });
            return;
        }

        foreach (var group in learned.GroupBy(d => d.Category))
        {
            AddSkillGroupHeader(CategoryName(group.Key));
            foreach (var def in group)
            {
                bool onBar = _skillBar.Any(x => x == def.Id);
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };

                if (IsPassive(def))
                {
                    // Passives are always-on; no action-bar slot.
                    var tag = new TextBlock
                    {
                        Text = "Passive", Width = 70, FontSize = 10,
                        Foreground = Brushes.MediumAquamarine,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    };
                    DockPanel.SetDock(tag, Dock.Right);
                    row.Children.Add(tag);
                }
                else
                {
                    var assign = new Button
                    {
                        Content = onBar ? "On Bar" : "To Bar",
                        Height = 24, Width = 70, FontSize = 10, IsEnabled = !onBar
                    };
                    string id = def.Id;
                    assign.Click += (_, _) => AssignSkillToBar(id);
                    DockPanel.SetDock(assign, Dock.Right);
                    row.Children.Add(assign);
                }

                var name = new TextBlock
                {
                    Text = SkillDisplayName(def.Id, def.Name),
                    Foreground = Brushes.White, FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = "Click for details"
                };
                string detailId = def.Id;
                name.MouseLeftButtonUp += (_, _) => OpenSkillDetail(detailId);
                row.Children.Add(name);
                SkillsList.Items.Add(row);
            }
        }
    }

    /// <summary>Tab 2: learnable skills grouped by required level, with Learn
    /// buttons enabled when level + SP (+ previous rank) allow.</summary>
    private void BuildLearnTab()
    {
        var all = ClassSkills.LearnableAt(_myRace, _myBaseClass, CurrentArchetype, int.MaxValue, CurrentDiscipline);

        // Show only each skill's NEXT learnable level (the entry whose SkillLevel ==
        // current+1), and hide skills replaced by something you already know (Flame
        // Bolt → Magic Bolt). Grouped by the character level that unlocks it.
        var groups = all
            .Where(cs => cs.SkillLevel == _learnedLevels.GetValueOrDefault(cs.SkillId) + 1
                         && !SupersededByLearned(cs.SkillId))
            .GroupBy(cs => cs.LearnLevel)
            .OrderBy(g => g.Key);

        bool any = false;
        foreach (var group in groups)
        {
            any = true;
            bool levelMet = _level >= group.Key;
            AddSkillGroupHeader($"Level {group.Key}" + (levelMet ? "" : "  (locked)"));

            foreach (var cs in group)
            {
                var def = SkillCatalog.Get(cs.SkillId);
                if (def is null) continue;

                int cost = def.SpCostAt(cs.SkillLevel);
                bool canLearn = levelMet && _skillPoints >= cost;
                string levelTag = def.MaxLevel > 1 ? $" Lv.{cs.SkillLevel}" : "";

                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
                var learn = new Button
                {
                    Content = "Learn", Height = 24, Width = 70, FontSize = 10,
                    IsEnabled = canLearn
                };
                string id = def.Id;
                learn.Click += (_, _) => OpenLearnPopup(id);
                DockPanel.SetDock(learn, Dock.Right);
                row.Children.Add(learn);

                var name = new TextBlock
                {
                    Text = $"{SkillDisplayName(def.Id, def.Name)}{levelTag}  (SP {cost})",
                    Foreground = canLearn ? Brushes.White : Brushes.Gray,
                    FontSize = 13, VerticalAlignment = VerticalAlignment.Center,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = "Click for details"
                };
                string detailId = def.Id;
                name.MouseLeftButtonUp += (_, _) => OpenSkillDetail(detailId);
                row.Children.Add(name);
                SkillsList.Items.Add(row);
            }
        }

        if (!any)
            SkillsList.Items.Add(new TextBlock
            {
                Text = "Nothing left to learn for this class right now.",
                Foreground = Brushes.Gray, Margin = new Thickness(0, 6, 0, 0)
            });
    }

    /// <summary>True if a learned skill REPLACES this one (cross-skill upgrade, e.g.
    /// Flame Bolt → Magic Bolt) — so it shouldn't appear as learnable.</summary>
    private bool SupersededByLearned(string skillId)
    {
        foreach (var id in _learnedSkills)
            if (SkillCatalog.Get(id)?.Replaces is { } rep && Array.IndexOf(rep, skillId) >= 0)
                return true;
        return false;
    }

    private void OpenLearnPopup(string skillId)
    {
        var def = SkillCatalog.Get(skillId);
        if (def is null) return;

        int target = _learnedLevels.GetValueOrDefault(skillId) + 1;
        int cost = def.SpCostAt(target);
        _pendingLearnId = skillId;
        LearnTitle.Text = def.MaxLevel > 1 ? $"{def.Name} (Lv.{target})" : def.Name;
        LearnBody.Text = SkillDetail(def);
        bool enough = _skillPoints >= cost;
        LearnCost.Text = $"Cost: {cost} SP   (you have {_skillPoints})";
        LearnCost.Foreground = enough ? Brushes.LightGreen : Brushes.IndianRed;
        LearnConfirm.IsEnabled = enough;
        LearnPopup.Visibility = Visibility.Visible;
    }

    private async void LearnConfirm_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingLearnId is string id)
            await _net.LearnSkillAsync(id);
        LearnPopup.Visibility = Visibility.Collapsed;
        _pendingLearnId = null;
    }

    private void LearnCancel_Click(object sender, RoutedEventArgs e)
    {
        LearnPopup.Visibility = Visibility.Collapsed;
        _pendingLearnId = null;
    }

    private void AddSkillGroupHeader(string text) =>
        SkillsList.Items.Add(new TextBlock
        {
            Text = text, Foreground = Brushes.Gray, FontSize = 11,
            FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 4)
        });

    private static bool IsPassive(SkillDef def) => def.Category == SkillCategory.Passive;

    private static string CategoryName(SkillCategory c) => c switch
    {
        SkillCategory.Physical => "Physical Skills",
        SkillCategory.Magic => "Magic Skills",
        SkillCategory.Buff => "Buffs",
        SkillCategory.Debuff => "Debuffs",
        SkillCategory.Heal => "Heals",
        SkillCategory.Passive => "Passives",
        _ => "Other"
    };

    /// <summary>Full skill detail for the click-popup: description, real cast time
    /// (base folds in the current cast-speed multiplier), cooldown, MP, range,
    /// duration, plus a human-readable buff/passive bonus summary.</summary>
    private string SkillDetail(SkillDef def)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(def.Description))
            lines.Add(def.Description);

        if (IsPassive(def))
        {
            lines.Add("Passive — always active once learned (no MP, not cast).");
            var ps = PassiveSummary(def);
            if (ps.Count > 0) lines.Add(string.Join(",  ", ps));
            return string.Join("\n", lines);
        }

        // Cast: base (real). "real" folds in WIT/gear/masteries/buffs from the
        // latest stats update; a moving multiplier so it tracks your current speed.
        if (def.CastTicks > 0)
        {
            float baseCast = def.CastTicks * GameConstants.TickSeconds;
            float mult = _stats?.CastSpeedMult ?? 1f;
            lines.Add($"Cast: {baseCast:0.0}s  ({baseCast * mult:0.0}s now)");
        }
        else
        {
            lines.Add("Cast: instant");
        }

        lines.Add($"Cooldown: {def.CooldownTicks * GameConstants.TickSeconds:0}s    MP: {def.MpCost}");
        if (def.Range > 0) lines.Add($"Range: {def.Range:0}");
        if (def.DurationTicks > 0)
            lines.Add($"Duration: {def.DurationTicks * GameConstants.TickSeconds:0}s");

        var bs = BuffSummary(def);
        if (bs.Count > 0) lines.Add(string.Join(",  ", bs));
        return string.Join("\n", lines);
    }

    /// <summary>Always-on bonuses carried by a passive skill (PassiveEffect).</summary>
    private static List<string> PassiveSummary(SkillDef def)
    {
        var outp = new List<string>();
        if (def.Passive is not PassiveEffect p) return outp;
        void Pct(string label, float v) { if (Math.Abs(v) > 0.0001f) outp.Add($"{label} {(v >= 0 ? "+" : "")}{v * 100:0.#}%"); }
        void Flat(string label, int v) { if (v != 0) outp.Add($"{label} {(v >= 0 ? "+" : "")}{v}"); }
        Pct("Max HP", p.MaxHpPct); Pct("Max MP", p.MaxMpPct);
        Flat("Defence", p.Defence); Flat("Magic Def", p.MagicDefence);
        Flat("Attack", p.Attack); Pct("Attack", p.AttackPct);
        Flat("Evasion", p.Evasion); Flat("Accuracy", p.Accuracy);
        Pct("Crit Rate", p.CritRate); Pct("Crit Dmg", p.CritDamage); Pct("Magic Crit", p.MagicCritRate);
        Pct("HP Regen", p.HpRegen); Pct("MP Regen", p.MpRegen);
        Pct("Atk Speed", p.AtkSpeedPct); Pct("Cast Speed", p.CastSpeedPct); Pct("Move Speed", p.MoveSpeedPct);
        return outp;
    }

    /// <summary>Per-effect magnitudes of a buff/debuff/HoT, e.g. "Attack +20%".</summary>
    private static List<string> BuffSummary(SkillDef def)
    {
        var outp = new List<string>();
        if (def.Magnitudes is null) return outp;
        foreach (var m in def.Magnitudes)
        {
            string label = EffectLabel(m.Effect);
            if (label.Length == 0) continue;
            outp.Add(m.Mode == ModifierMode.Flat
                ? $"{label} {(m.Value >= 0 ? "+" : "")}{m.Value:0.#}"
                : $"{label} {(m.Value >= 0 ? "+" : "")}{m.Value * 100:0.#}%");
        }
        return outp;
    }

    private static string EffectLabel(SkillEffect e) => e switch
    {
        SkillEffect.BuffAtk => "Attack",
        SkillEffect.BuffDef => "Defence",
        SkillEffect.BuffMagicDef => "Magic Def",
        SkillEffect.BuffCastSpeed => "Cast Speed",
        SkillEffect.BuffAtkSpeed => "Atk Speed",
        SkillEffect.BuffMoveSpeed => "Move Speed",
        SkillEffect.BuffEvasion => "Evasion",
        SkillEffect.BuffHp => "Max HP",
        SkillEffect.BuffMp => "Max MP",
        SkillEffect.BuffHpRegen => "HP Regen",
        SkillEffect.BuffMpRegen => "MP Regen",
        SkillEffect.BuffBlockChance => "Block Chance",
        SkillEffect.BuffShieldDef => "Shield Def",
        SkillEffect.DebuffDef => "Def Down",
        SkillEffect.DebuffHealRecv => "Healing Down",
        SkillEffect.HealOverTime => "Heal/sec",
        SkillEffect.BuffPhysAtk => "P.Atk",
        SkillEffect.BuffMagAtk => "M.Atk",
        SkillEffect.BuffAccuracy => "Accuracy",
        SkillEffect.BuffCritRate => "Crit Rate",
        SkillEffect.BuffMagicCritRate => "M.Crit Rate",
        SkillEffect.BuffCritDamage => "Crit Dmg",
        SkillEffect.BuffCritDmgResist => "Crit Dmg Resist",
        SkillEffect.BuffCritRateResist => "Crit Rate Resist",
        SkillEffect.BuffBowResist => "Bow Resist",
        SkillEffect.BuffMagicFailFloor => "Anti-Magic",
        SkillEffect.BuffMagicFailResist => "Spell Focus",
        SkillEffect.BuffInterruptPower => "Cancel Power",
        SkillEffect.BuffInterruptResist => "Cancel Resist",
        SkillEffect.BuffMeleeVamp => "Vampiric",
        SkillEffect.BuffSpellVamp => "Spell Vamp",
        SkillEffect.BuffCooldown => "Reuse",
        _ => ""
    };

    private void OpenSkillDetail(string skillId)
    {
        var def = SkillCatalog.Get(skillId);
        if (def is null) return;
        SkillDetailTitle.Text = SkillDisplayName(def.Id, def.Name);
        SkillDetailBody.Text = SkillDetail(def);
        SkillDetailPopup.Visibility = Visibility.Visible;
    }

    private void SkillDetailClose_Click(object sender, RoutedEventArgs e) =>
        SkillDetailPopup.Visibility = Visibility.Collapsed;

    /// <summary>Server -> client: learned skill ids + SP. Refresh bar + window.</summary>
    private void OnLearned(LearnedSkills learned)
    {
        _learnedSkills.Clear();
        _learnedLevels.Clear();
        foreach (var s in learned.Skills)
        {
            _learnedSkills.Add(s.Id);
            _learnedLevels[s.Id] = s.Level;
        }
        _skillPoints = learned.SkillPoints;

        AutoPlaceNewSkills();
        RenderSkillBar();
        if (SkillsPanel.Visibility == Visibility.Visible)
            RefreshSkillsWindow();
    }

    // =======================================================================
    // Buff bar
    // =======================================================================

    // ----- Selection box chooser -----
    private Guid _selectionBoxId;
    private int _selectionPick;
    private readonly List<CheckBox> _selectionChecks = new();

    private void OnSelection(SelectionOffer offer)
    {
        _selectionBoxId = offer.BoxInstanceId;
        _selectionPick = offer.PickCount;
        _selectionChecks.Clear();
        SelectionList.Children.Clear();
        SelectionTitle.Text = offer.BoxName;

        foreach (var opt in offer.Options)
        {
            var cb = new CheckBox
            {
                Content = opt.Name,
                Tag = opt.ItemId,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 3, 0, 3)
            };
            cb.Checked += SelectionCheckChanged;
            cb.Unchecked += SelectionCheckChanged;
            _selectionChecks.Add(cb);
            SelectionList.Children.Add(cb);
        }
        UpdateSelectionSubtitle();
        SelectionPopup.Visibility = Visibility.Visible;
    }

    private void SelectionCheckChanged(object sender, RoutedEventArgs e)
    {
        // Enforce the pick limit: bounce a check that would exceed it.
        int checkedCount = _selectionChecks.Count(c => c.IsChecked == true);
        if (sender is CheckBox box && box.IsChecked == true && checkedCount > _selectionPick)
        {
            box.IsChecked = false;
            return;
        }
        UpdateSelectionSubtitle();
    }

    private void UpdateSelectionSubtitle()
    {
        int n = _selectionChecks.Count(c => c.IsChecked == true);
        SelectionSubtitle.Text = $"Pick {_selectionPick} — selected {n}/{_selectionPick}";
        SelectionConfirm.IsEnabled = n >= 1 && n <= _selectionPick;
    }

    private void SelectionConfirm_Click(object sender, RoutedEventArgs e)
    {
        var chosen = _selectionChecks
            .Where(c => c.IsChecked == true)
            .Select(c => (string)c.Tag)
            .ToArray();
        if (chosen.Length == 0) return;
        _ = _net.SelectBoxItemsAsync(_selectionBoxId, chosen);
        SelectionPopup.Visibility = Visibility.Collapsed;
    }

    private void SelectionCancel_Click(object sender, RoutedEventArgs e) =>
        SelectionPopup.Visibility = Visibility.Collapsed;

    private void OnBuffs(BuffUpdate update)
    {
        BuffBar.Items.Clear();
        foreach (var buff in update.Buffs)
        {
            // SecondsLeft < 0 = an indefinite TOGGLE/stance (no countdown).
            bool toggle = buff.SecondsLeft < 0f;
            string stacks = buff.Stacks > 1 ? $" x{buff.Stacks}" : "";
            string time = toggle ? "active (toggle)" : $"{buff.SecondsLeft:0}s remaining";
            string tip = buff.IsDebuff
                ? $"{buff.Name}\n{buff.Description}\n{time}"
                : $"{buff.Name}\n{buff.Description}\n{time}\n(double-click to remove)";
            var pill = new Border
            {
                Background = buff.IsDebuff
                    ? new SolidColorBrush(Color.FromArgb(200, 120, 40, 40))
                    : new SolidColorBrush(Color.FromArgb(200, 40, 80, 120)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 4, 4),
                Cursor = buff.IsDebuff ? null : System.Windows.Input.Cursors.Hand,
                Child = new TextBlock
                {
                    Text = toggle ? $"{buff.Name}{stacks}  ⟳" : $"{buff.Name}{stacks}  {buff.SecondsLeft:0}s",
                    Foreground = Brushes.White, FontSize = 11
                },
                ToolTip = tip
            };
            // Double-click a (beneficial) buff to drop it early, like a timeout.
            if (!buff.IsDebuff && !string.IsNullOrEmpty(buff.Key))
            {
                string key = buff.Key;
                pill.MouseLeftButtonDown += (_, e) =>
                {
                    if (e.ClickCount == 2) { _ = _net.RemoveBuffAsync(key); e.Handled = true; }
                };
            }
            BuffBar.Items.Add(pill);
        }
    }

    // =======================================================================
    // Fixed potion squares (always visible, color-coded, count badge)
    // =======================================================================

    private static readonly (string DefId, Color Color)[] PotionSquares =
    {
        (ItemCatalog.MinorPotion, Color.FromRgb(120, 200, 120)),  // green
        (ItemCatalog.HealingPotion, Color.FromRgb(90, 150, 230)), // blue
        (ItemCatalog.GreaterPotion, Color.FromRgb(220, 170, 70)), // gold
    };

    private void RebuildPotionBar()
    {
        PotionBar.Children.Clear();
        _potionSlots.Clear();

        foreach (var (defId, color) in PotionSquares)
        {
            var def = ItemCatalog.Get(defId);
            if (def is null)
                continue;

            var stack = _inventory.FirstOrDefault(i => i.DefId == defId);
            int count = stack?.Quantity ?? 0;

            // Rarity letter top-left, count bottom-right — clearly separated.
            var letter = new TextBlock
            {
                Text = RarityLetter(def.Rarity),
                Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(4, 1, 0, 0)
            };

            var badge = new TextBlock
            {
                Text = CountBadge(count),
                Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 3, 1)
            };

            var grid = new Grid();
            grid.Children.Add(letter);
            grid.Children.Add(badge);

            var button = new Button
            {
                Width = 46, Height = 46, Margin = new Thickness(4, 0, 0, 0),
                Background = new SolidColorBrush(color),
                Content = grid,
                IsEnabled = count > 0,
                ToolTip = $"{def.Name}\n{PotionTooltip(def)}"
            };
            if (count == 0)
                button.Opacity = 0.4;

            string id = defId;
            button.Click += async (_, _) =>
            {
                var firstStack = _inventory.FirstOrDefault(i => i.DefId == id);
                if (firstStack is not null)
                    await DrinkPotion(firstStack.InstanceId);
            };

            _potionSlots.Add(new PotionSlot { Button = button, InstanceId = Guid.Empty });
            PotionBar.Children.Add(button);
        }

        PotionBar.Visibility = Visibility.Visible;
    }

    private static string CountBadge(int count) =>
        count >= 100 ? "99+" : count.ToString();

    private static string RarityLetter(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => "C",
        ItemRarity.Uncommon => "U",
        ItemRarity.Rare => "R",
        _ => "?"
    };

    private static string PotionTooltip(ItemDef def)
    {
        if (def.InstantHealPercent > 0)
            return $"Instant heal {def.InstantHealPercent * 100:0}% HP. CD {def.PotionCooldownTicks / GameConstants.TickRate}s.";
        return $"Heal {def.HealPercentPerSecond * 100:0}%/s for " +
               $"{def.PotionDurationTicks / GameConstants.TickRate}s. CD {def.PotionCooldownTicks / GameConstants.TickRate}s.";
    }

    // =======================================================================
    // Enchant popup
    // =======================================================================

    private void OpenEnchantPopup(InventoryItemDto item)
    {
        if (ItemCatalog.Get(item.DefId) is not ItemDef def || !ItemCatalog.IsEquippable(def))
            return;

        _enchantTargetId = item.InstanceId;
        EnchantTitle.Text = $"Enchant {def.Name} +{item.Enchant}";

        float chance = EnchantRules.SuccessChance(item.Enchant) * 100;
        EnchantInfo.Text = item.Enchant >= EnchantRules.MaxEnchant
            ? "This item is at maximum enchant (+16)."
            : $"Next: +{item.Enchant} -> +{item.Enchant + 1}   Success: {chance:0}%\n" +
              "Common scroll: item BREAKS on fail.\n" +
              "Uncommon scroll: enchant RESETS to +0 on fail.\n" +
              "Rare scroll: enchant drops by 1 on fail.";

        EnchantScrollList.Items.Clear();
        bool maxed = item.Enchant >= EnchantRules.MaxEnchant;

        foreach (var scrollDefId in new[] { ItemCatalog.ScrollCommon, ItemCatalog.ScrollUncommon, ItemCatalog.ScrollRare })
        {
            var scrollDef = ItemCatalog.Get(scrollDefId)!;
            int count = _inventory.FirstOrDefault(i => i.DefId == scrollDefId)?.Quantity ?? 0;

            var button = new Button
            {
                Content = $"{scrollDef.Name}  x{count}",
                Height = 30, Margin = new Thickness(0, 0, 0, 4),
                Foreground = RarityBrush(scrollDef.Rarity),
                IsEnabled = count > 0 && !maxed,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 0, 0, 0)
            };
            button.Click += async (_, _) =>
            {
                var scroll = _inventory.FirstOrDefault(i => i.DefId == scrollDefId);
                if (scroll is not null && _enchantTargetId is Guid tid)
                    await _net.EnchantAsync(scroll.InstanceId, tid);
            };
            EnchantScrollList.Items.Add(button);
        }

        EquipPopup.Visibility = Visibility.Collapsed;
        EnchantPopup.Visibility = Visibility.Visible;
    }

    private void OnEnchant(EnchantResultDto result)
    {
        // Refresh the popup if still open (inventory update drives the list).
        if (EnchantPopup.Visibility == Visibility.Visible && _enchantTargetId is Guid tid)
        {
            var item = _inventory.FirstOrDefault(i => i.InstanceId == tid);
            if (item is null || result.Destroyed)
            {
                EnchantPopup.Visibility = Visibility.Collapsed;
                _enchantTargetId = null;
            }
            else
            {
                OpenEnchantPopup(item);
            }
        }
    }

    private void EnchantClose_Click(object sender, RoutedEventArgs e)
    {
        EnchantPopup.Visibility = Visibility.Collapsed;
        _enchantTargetId = null;
    }

    // =======================================================================
    // Attribute reroll popup
    // =======================================================================

    private void OpenRerollPopup(InventoryItemDto item)
    {
        if (ItemCatalog.Get(item.DefId) is not ItemDef def || item.Attributes.Length == 0)
            return;

        _rerollTargetId = item.InstanceId;
        _rerollLocks.Clear();
        RerollTitle.Text = $"Reroll {def.Name}";
        RerollInfo.Text =
            "Lock the attributes you want to keep, then pick a scroll. Each reroll " +
            "re-randomises the unlocked slots (type + value). Common locks 0, Uncommon 1, " +
            "Rare 2. Legendary rerolls ALL to their MAX value.";

        // One lock checkbox per current attribute.
        RerollAttrList.Items.Clear();
        for (int i = 0; i < item.Attributes.Length; i++)
        {
            int index = i;
            var a = item.Attributes[i];
            var cb = new CheckBox
            {
                Content = $"{AttributeSystem.DisplayName(a.Type)} +{a.Value}" +
                          (AttributeSystem.IsPercent(a.Type) ? "%" : ""),
                Foreground = Brushes.White,
                Margin = new Thickness(0, 1, 0, 1)
            };
            cb.Checked += (_, _) => _rerollLocks.Add(index);
            cb.Unchecked += (_, _) => _rerollLocks.Remove(index);
            RerollAttrList.Items.Add(cb);
        }

        // Scroll buttons, labelled with their lock capacity + how many you own.
        RerollScrollList.Items.Clear();
        var scrolls = new (string DefId, int Locks)[]
        {
            (ItemCatalog.AttrScrollCommon, 0),
            (ItemCatalog.AttrScrollUncommon, 1),
            (ItemCatalog.AttrScrollRare, 2),
            (ItemCatalog.AttrScrollLegendary, -1),   // -1 = maxes all
        };
        foreach (var (scrollDefId, locks) in scrolls)
        {
            var scrollDef = ItemCatalog.Get(scrollDefId)!;
            int count = _inventory.FirstOrDefault(i => i.DefId == scrollDefId)?.Quantity ?? 0;
            string lockLabel = locks < 0 ? "max all" : $"lock {locks}";
            var button = new Button
            {
                Content = $"{scrollDef.Name}  ({lockLabel})  x{count}",
                Height = 30, Margin = new Thickness(0, 0, 0, 4),
                Foreground = RarityBrush(scrollDef.Rarity),
                IsEnabled = count > 0,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 0, 0, 0)
            };
            button.Click += async (_, _) =>
            {
                var scroll = _inventory.FirstOrDefault(i => i.DefId == scrollDefId);
                if (scroll is not null && _rerollTargetId is Guid tid)
                    await _net.RerollAttributesAsync(scroll.InstanceId, tid, _rerollLocks.ToArray());
            };
            RerollScrollList.Items.Add(button);
        }

        EquipPopup.Visibility = Visibility.Collapsed;
        EnchantPopup.Visibility = Visibility.Collapsed;
        RerollPopup.Visibility = Visibility.Visible;
    }

    private void OnReroll(RerollResultDto result)
    {
        // Refresh the popup (inventory update carries the new attributes).
        if (RerollPopup.Visibility == Visibility.Visible && _rerollTargetId is Guid tid)
        {
            var item = _inventory.FirstOrDefault(i => i.InstanceId == tid);
            if (item is null)
            {
                RerollPopup.Visibility = Visibility.Collapsed;
                _rerollTargetId = null;
            }
            else
            {
                OpenRerollPopup(item);
            }
        }
    }

    private void RerollClose_Click(object sender, RoutedEventArgs e)
    {
        RerollPopup.Visibility = Visibility.Collapsed;
        _rerollTargetId = null;
        _rerollLocks.Clear();
    }

    // =======================================================================
    // Debug menu
    // =======================================================================

    // Debug menu is split into three tabs: 0 = Equip, 1 = Consumables, 2 = Functions.
    private int _debugTab;

    private void DebugButton_Click(object sender, RoutedEventArgs e)
    {
        BuildDebugMenu();
        DebugPanel.Visibility = DebugPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private void DebugClose_Click(object sender, RoutedEventArgs e) =>
        DebugPanel.Visibility = Visibility.Collapsed;

    private void DebugTabEquip_Click(object sender, RoutedEventArgs e) { _debugTab = 0; BuildDebugMenu(); }
    private void DebugTabConsum_Click(object sender, RoutedEventArgs e) { _debugTab = 1; BuildDebugMenu(); }
    private void DebugTabFunc_Click(object sender, RoutedEventArgs e) { _debugTab = 2; BuildDebugMenu(); }

    private int _debugTpView;   // 0 = categories, 1 = NPCs, 2 = Zones, 3 = Cities
    private void DebugTabTp_Click(object sender, RoutedEventArgs e) { _debugTab = 3; _debugTpView = 0; BuildDebugMenu(); }

    private void BuildDebugMenu()
    {
        DebugList.Children.Clear();
        switch (_debugTab)
        {
            case 1: BuildDebugConsumables(); break;
            case 2: BuildDebugFunctions(); break;
            case 3: BuildDebugTeleport(); break;
            default: BuildDebugEquip(); break;
        }
    }

    private void BuildDebugTeleport()
    {
        if (_debugTpView == 0)
        {
            AddDebugHeader("Teleport to…");
            DebugList.Children.Add(DebugAction("NPCs ▸", () => { _debugTpView = 1; BuildDebugMenu(); return Task.CompletedTask; }));
            DebugList.Children.Add(DebugAction("Zones (spawn) ▸", () => { _debugTpView = 2; BuildDebugMenu(); return Task.CompletedTask; }));
            DebugList.Children.Add(DebugAction("Cities ▸", () => { _debugTpView = 3; BuildDebugMenu(); return Task.CompletedTask; }));
            return;
        }

        DebugList.Children.Add(DebugAction("◂ Back", () => { _debugTpView = 0; BuildDebugMenu(); return Task.CompletedTask; }));

        if (_debugTpView == 1)
        {
            AddDebugHeader("NPCs");
            foreach (var npc in WorldMap.Npcs.OrderBy(n => n.Name))
            {
                float x = npc.X, y = npc.Y;
                DebugList.Children.Add(DebugAction(npc.Name, async () => await _net.DebugTeleportAsync(x, y)));
            }
        }
        else if (_debugTpView == 2)
        {
            // Land ~400 units OUTSIDE the spawn ring (centre + radius + 400) so you arrive
            // next to the mobs, not on top of them.
            AddDebugHeader("Spawn zones (Lv range)");
            foreach (var z in WorldMap.SpawnZones.OrderBy(z => z.MinLevel))
            {
                string mob = z.MobTypes.Length > 0 ? (MobCatalog.Get(z.MobTypes[0])?.Name ?? z.MobTypes[0]) : "";
                float tx = z.X + z.Radius + 400f, ty = z.Y;
                DebugList.Children.Add(DebugAction($"Lv {z.MinLevel}-{z.MaxLevel}  {mob}",
                    async () => await _net.DebugTeleportAsync(tx, ty)));
            }
        }
        else
        {
            AddDebugHeader("Cities");
            foreach (var c in WorldMap.SafeZones.OrderBy(c => c.Name))
            {
                float x = c.X, y = c.Y;
                DebugList.Children.Add(DebugAction(c.Name, async () => await _net.DebugTeleportAsync(x, y)));
            }
        }
    }

    private void BuildDebugEquip()
    {
        AddDebugHeader("Legendary");
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.GodWeapon, "God's Judgment"));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.GodArmor, "God's Robes"));

        AddDebugHeader("Boxes");
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.BoxNewbie, "Newbie Box"));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.BoxTreasure, "Treasure Chest"));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.BoxNewbieArmorLight, "Newbie Light Armor Box"));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.BoxNewbieArmorRobe, "Newbie Robe Armor Box"));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.BoxNewbieJewels, "Newbie Jewels Box"));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.BoxNewbieWeapons, "Newbie Weapons Box (select)"));

        AddDebugHeader("Rare Weapons (E)");
        DebugList.Children.Add(DebugGiveButton(
            ItemCatalog.WeaponKey(WeaponType.Sword, ItemGrade.E, ItemRarity.Rare), "Rare Sword"));
        DebugList.Children.Add(DebugGiveButton(
            ItemCatalog.WeaponKey(WeaponType.Dual, ItemGrade.E, ItemRarity.Rare), "Rare Daggers"));
        DebugList.Children.Add(DebugGiveButton(
            ItemCatalog.WeaponKey(WeaponType.Bow, ItemGrade.E, ItemRarity.Rare), "Rare Bow"));
        DebugList.Children.Add(DebugGiveButton(
            ItemCatalog.WeaponKey(WeaponType.TwoHandedBlunt, ItemGrade.E, ItemRarity.Rare), "Rare Staff"));

        AddDebugHeader("Rare Armor Sets (E: body + accessories)");
        foreach (var (w, label) in new[] { (ArmorWeight.Heavy, "Heavy"), (ArmorWeight.Light, "Light"), (ArmorWeight.Robe, "Robe") })
        {
            var weight = w;
            DebugList.Children.Add(DebugAction($"Rare {label} Set", async () =>
            {
                await _net.DebugGiveAsync(ItemCatalog.ArmorKey(weight, ArmorSlot.Body, ItemGrade.E, ItemRarity.Rare));
                foreach (var slot in new[] { ArmorSlot.Head, ArmorSlot.Gloves, ArmorSlot.Boots })
                    await _net.DebugGiveAsync(ItemCatalog.ArmorKey(ArmorWeight.None, slot, ItemGrade.E, ItemRarity.Rare));
            }));
        }

        AddDebugHeader("Named Sets");
        DebugList.Children.Add(DebugAction("Dark Dominion (heavy body)", async () =>
        {
            await _net.DebugGiveAsync(ItemCatalog.DarkDominionHeavyBody);
            await _net.DebugGiveAsync(ItemCatalog.DarkDominionHead);
            await _net.DebugGiveAsync(ItemCatalog.DarkDominionGloves);
            await _net.DebugGiveAsync(ItemCatalog.DarkDominionBoots);
        }));
        DebugList.Children.Add(DebugAction("Dark Dominion (light body)", async () =>
        {
            await _net.DebugGiveAsync(ItemCatalog.DarkDominionLightBody);
            await _net.DebugGiveAsync(ItemCatalog.DarkDominionHead);
            await _net.DebugGiveAsync(ItemCatalog.DarkDominionGloves);
            await _net.DebugGiveAsync(ItemCatalog.DarkDominionBoots);
        }));
        DebugList.Children.Add(DebugAction("Dark Dominion (robe body)", async () =>
        {
            await _net.DebugGiveAsync(ItemCatalog.DarkDominionRobeBody);
            await _net.DebugGiveAsync(ItemCatalog.DarkDominionHead);
            await _net.DebugGiveAsync(ItemCatalog.DarkDominionGloves);
            await _net.DebugGiveAsync(ItemCatalog.DarkDominionBoots);
        }));
    }

    private void BuildDebugConsumables()
    {
        AddDebugHeader("Scrolls (x10)");
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.ScrollCommon, "Common Scroll x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.ScrollUncommon, "Uncommon Scroll x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.ScrollRare, "Rare Scroll x10", 10));

        AddDebugHeader("Attribute Scrolls (x10)");
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.AttrScrollCommon, "Attr Scroll (Common) x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.AttrScrollUncommon, "Attr Scroll (Uncommon) x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.AttrScrollRare, "Attr Scroll (Rare) x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.AttrScrollLegendary, "Attr Scroll (Legendary) x10", 10));

        AddDebugHeader("Buff Potions (x5)");
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.SpeedPotionC, "Swiftness (Lesser) x5", 5));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.SpeedPotionR, "Swiftness (Greater) x5", 5));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.CastPotionR, "Focus (Greater) x5", 5));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.AtkPotionR, "Haste (Greater) x5", 5));

        AddDebugHeader("Potions (x10)");
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.MinorPotion, "Minor Potion x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.HealingPotion, "Healing Potion x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.GreaterPotion, "Greater Potion x10", 10));

        AddDebugHeader("Reagents");
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.ElementalStone, "Elemental Stone +10", 10));
    }

    private void BuildDebugFunctions()
    {
        AddDebugHeader("Character");
        DebugList.Children.Add(DebugAction("Level +1", async () => await _net.DebugLevelAsync()));
        DebugList.Children.Add(DebugAction("Level +10", async () => { for (int i = 0; i < 10; i++) await _net.DebugLevelAsync(); }));
        DebugList.Children.Add(DebugAction("Learn all skills (to my level)", async () => await _net.DebugLearnAllAsync()));
        DebugList.Children.Add(DebugAction("+1kk SP", async () => await _net.DebugSpAsync(1_000_000)));
        DebugList.Children.Add(DebugAction("+100,000 Gold", async () => await _net.DebugGoldAsync(100_000)));

        // Re-roll the SAME character: pick race + base class; resets to level 1 with
        // the starter kit (classes/skills/quests/inventory cleared). No relog needed.
        AddDebugHeader("Reset Character (re-roll, same char)");
        foreach (var race in SelectableRaces())
            foreach (var bc in Enum.GetValues<BaseClass>())
            {
                var r = race; var b = bc;
                DebugList.Children.Add(DebugAction($"Reset → {r} {b}",
                    async () => await _net.DebugResetAsync(r, b)));
            }
        DebugList.Children.Add(DebugAction("Class Change (test)", () =>
        {
            OpenClassChangePanel();
            return Task.CompletedTask;
        }));
    }

    private void AddDebugHeader(string text) =>
        DebugList.Children.Add(new TextBlock
        {
            Text = text, Foreground = Brushes.Gray, FontSize = 10,
            FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 2)
        });

    private System.Windows.Controls.Button DebugGiveButton(string defId, string label, int qty = 1)
    {
        return DebugAction(label, async () => await _net.DebugGiveAsync(defId, qty));
    }

    private System.Windows.Controls.Button DebugAction(string label, Func<Task> action)
    {
        var button = new System.Windows.Controls.Button
        {
            Content = label, Height = 26, Margin = new Thickness(0, 0, 0, 3),
            FontSize = 11, HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(6, 0, 0, 0)
        };
        button.Click += async (_, _) => await action();
        return button;
    }

    // =======================================================================
    // NPC dialog + quest log
    // =======================================================================

    private Guid _dialogNpcId;

    private void OnDialog(NpcDialog dialog)
    {
        // The NPC entity id is whatever we last clicked to talk; capture from
        // the currently-hovered talk target via _lastTalkNpcId.
        DialogNpcName.Text = dialog.NpcName;
        DialogNpcRole.Text = dialog.NpcRole switch
        {
            "ClassChange" => "Class Master",
            "Vendor" => "Merchant",
            "Teleporter" => "Gatekeeper",
            _ => "Quest Giver"
        };
        DialogContent.Children.Clear();

        // Vendor: offer a button to open the shop window.
        if (dialog.Shop is ShopInfo shop)
        {
            _shop = shop;
            AddDialogHeader(shop.Title);
            var browse = new Button { Content = "Browse Wares", Width = 140, Height = 28,
                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 2, 0, 8) };
            browse.Click += (_, _) => OpenShop();
            DialogContent.Children.Add(browse);
        }

        // Gatekeeper: a button per destination ("Travel to X — N gold").
        if (dialog.Teleport is TeleportInfo teleport)
        {
            AddDialogHeader("Travel");
            foreach (var dest in teleport.Destinations)
            {
                string band = dest.MaxLevel > 0 ? $"  (Lv {dest.MinLevel}-{dest.MaxLevel})" : "";
                var btn = new Button
                {
                    Content = $"Travel to {dest.Name}{band}  —  {dest.Fee:N0} {GameConstants.CurrencyName}",
                    Height = 28, HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 2, 0, 4), Padding = new Thickness(8, 0, 8, 0),
                    IsEnabled = _gold >= dest.Fee
                };
                string zoneId = dest.ZoneId;
                btn.Click += async (_, _) =>
                {
                    await _net.TeleportAsync(_dialogNpcId, zoneId);
                    DialogPanel.Visibility = Visibility.Collapsed;
                };
                DialogContent.Children.Add(btn);
            }
        }

        // Offered quests.
        foreach (var q in dialog.Offered)
        {
            AddDialogHeader($"Available: {q.Name}");
            AddDialogText(q.Description);
            var accept = new Button { Content = "Accept", Width = 90, Height = 26,
                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0,2,0,8) };
            string id = q.Id;
            accept.Click += async (_, _) => { await _net.QuestActionAsync("accept", id, _dialogNpcId); };
            DialogContent.Children.Add(accept);
        }

        // Turn-in (complete here).
        foreach (var q in dialog.Turnable)
        {
            AddDialogHeader($"Ready to complete: {q.Name}");
            AddDialogText(q.CurrentStepText);
            var complete = new Button { Content = "Complete", Width = 100, Height = 26,
                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0,2,0,8) };
            string id = q.Id;
            complete.Click += async (_, _) => { await _net.QuestActionAsync("complete", id, _dialogNpcId); };
            DialogContent.Children.Add(complete);
        }

        // In progress (status only).
        foreach (var q in dialog.InProgress)
        {
            AddDialogHeader($"In progress: {q.Name}");
            string prog = q.StepCount > 0 ? $"Step {q.StepIndex + 1}/{q.StepCount}: {q.CurrentStepText}" : q.CurrentStepText;
            if (q.CounterNeeded > 1)
                prog += $"  ({q.Counter}/{q.CounterNeeded})";
            AddDialogText(prog);
            if (!string.IsNullOrEmpty(q.Location))
                AddDialogText($"➜ {q.Location}");
        }

        // Class-change options.
        foreach (var c in dialog.ClassChanges)
        {
            AddDialogHeader($"Class Change: {c.ClassName}");
            if (!string.IsNullOrEmpty(c.Description))
                AddDialogText(c.Description);
            var sb = new System.Text.StringBuilder("Requires: ");
            for (int i = 0; i < c.RequiredItemNames.Length; i++)
                sb.Append($"{c.RequiredItemNames[i]} {(c.HasItem[i] ? "\u2713" : "\u2717")}  ");
            AddDialogText(sb.ToString());

            var change = new Button { Content = $"Become {c.ClassName}", Width = 160, Height = 28,
                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0,2,0,8),
                IsEnabled = c.Meets };
            int classId = c.SecondClassId;
            change.Click += async (_, _) => { await _net.QuestActionAsync("changeclass", classId.ToString(), _dialogNpcId); };
            DialogContent.Children.Add(change);
        }

        if (DialogContent.Children.Count == 0)
            AddDialogText("They have nothing for you right now.");

        DialogPanel.Visibility = Visibility.Visible;
    }

    private void AddDialogHeader(string text) =>
        DialogContent.Children.Add(new TextBlock
        {
            Text = text, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold,
            FontSize = 13, Margin = new Thickness(0, 8, 0, 2), TextWrapping = TextWrapping.Wrap
        });

    private void AddDialogText(string text) =>
        DialogContent.Children.Add(new TextBlock
        {
            Text = text, Foreground = new SolidColorBrush(Color.FromRgb(0xB9, 0xC4, 0xCC)),
            FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 2)
        });

    private void DialogClose_Click(object sender, RoutedEventArgs e) =>
        DialogPanel.Visibility = Visibility.Collapsed;

    // =======================================================================
    // Vendor shop
    // =======================================================================

    private ShopInfo? _shop;
    private bool _shopSellTab;

    private void OpenShop()
    {
        if (_shop is null) return;
        DialogPanel.Visibility = Visibility.Collapsed;
        ShopTitle.Text = _shop.Title;
        _shopSellTab = false;
        RenderShop();
        ShopPanel.Visibility = Visibility.Visible;
    }

    private void ShopTabBuy_Click(object sender, RoutedEventArgs e) { _shopSellTab = false; RenderShop(); }
    private void ShopTabSell_Click(object sender, RoutedEventArgs e) { _shopSellTab = true; RenderShop(); }
    private void ShopClose_Click(object sender, RoutedEventArgs e) => ShopPanel.Visibility = Visibility.Collapsed;

    private void UpdateShopGold() =>
        ShopGoldText.Text = $"Your {GameConstants.CurrencyName}: {_gold:N0}";

    private void RenderShop()
    {
        ShopList.Children.Clear();
        UpdateShopGold();

        if (!_shopSellTab)
        {
            // BUY: the vendor's fixed wares.
            if (_shop is null || _shop.Items.Length == 0)
            {
                ShopList.Children.Add(MakeShopHint("Nothing for sale."));
                return;
            }
            foreach (var entry in _shop.Items)
            {
                string defId = entry.DefId;
                int price = entry.BuyPrice;
                ShopList.Children.Add(ShopRow(entry.Name, $"{price:N0} {GameConstants.CurrencyName}",
                    "Buy", _gold >= price,
                    async () => await _net.BuyItemAsync(_dialogNpcId, defId, 1)));
            }
            return;
        }

        // SELL: the player's sellable inventory.
        bool any = false;
        foreach (var item in _inventory)
        {
            if (item.Equipped) continue;
            if (ItemCatalog.Get(item.DefId) is not ItemDef def || !ItemCatalog.IsSellable(def)) continue;
            any = true;
            int unit = ItemCatalog.SellPrice(def);
            string label = item.Quantity > 1 ? $"{def.Name}  x{item.Quantity}" : def.Name;
            Guid instanceId = item.InstanceId;
            ShopList.Children.Add(ShopRow(label, $"{unit:N0} {GameConstants.CurrencyName} ea", "Sell", true,
                async () => await _net.SellItemAsync(_dialogNpcId, instanceId, 1)));
        }
        if (!any)
            ShopList.Children.Add(MakeShopHint("Nothing here you can sell."));
    }

    private TextBlock MakeShopHint(string text) => new()
    {
        Text = text, Foreground = Brushes.Gray, FontSize = 12, Margin = new Thickness(0, 6, 0, 0)
    };

    private FrameworkElement ShopRow(string label, string priceLabel, string action, bool enabled, Func<Task> onClick)
    {
        var dock = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };

        var button = new Button
        {
            Content = action, Width = 64, Height = 26, IsEnabled = enabled,
            VerticalAlignment = VerticalAlignment.Center
        };
        button.Click += async (_, _) => await onClick();
        DockPanel.SetDock(button, Dock.Right);
        dock.Children.Add(button);

        var price = new TextBlock
        {
            Text = priceLabel, Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE2, 0xC8)),
            FontSize = 11, Width = 130, TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0)
        };
        DockPanel.SetDock(price, Dock.Right);
        dock.Children.Add(price);

        var name = new TextBlock
        {
            Text = label, Foreground = Brushes.White, FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap
        };
        dock.Children.Add(name);   // fills remaining space
        return dock;
    }

    private void OnQuestLog(QuestLog log)
    {
        QuestLogContent.Children.Clear();
        if (log.Active.Length == 0)
            QuestLogContent.Children.Add(new TextBlock
            {
                Text = "No active quests.", Foreground = Brushes.Gray
            });

        foreach (var q in log.Active)
        {
            QuestLogContent.Children.Add(new TextBlock
            {
                Text = q.Name, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold,
                FontSize = 13, Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap
            });
            string prog = $"Step {q.StepIndex + 1}/{q.StepCount}: {q.CurrentStepText}";
            if (q.CounterNeeded > 1) prog += $"  ({q.Counter}/{q.CounterNeeded})";
            QuestLogContent.Children.Add(new TextBlock
            {
                Text = prog, Foreground = new SolidColorBrush(Color.FromRgb(0x9F, 0xB0, 0xBE)),
                FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 1)
            });
            if (!string.IsNullOrEmpty(q.Location))
                QuestLogContent.Children.Add(new TextBlock
                {
                    Text = $"➜ {q.Location}", Foreground = new SolidColorBrush(Color.FromRgb(0x7C, 0xB3, 0x42)),
                    FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4)
                });
        }

        if (log.Completed.Length > 0)
            QuestLogContent.Children.Add(new TextBlock
            {
                Text = $"Completed: {log.Completed.Length}", Foreground = Brushes.Gray,
                FontSize = 11, Margin = new Thickness(0, 8, 0, 0)
            });
    }

    private void ToggleQuestLog() =>
        QuestLogPanel.Visibility = QuestLogPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;

    private void QuestLogClose_Click(object sender, RoutedEventArgs e) =>
        QuestLogPanel.Visibility = Visibility.Collapsed;

}
