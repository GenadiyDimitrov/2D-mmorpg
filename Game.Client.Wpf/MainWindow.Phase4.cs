using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

    // =======================================================================
    // Inventory
    // =======================================================================

    private void OnInventory(InventoryUpdate update)
    {
        _inventory.Clear();
        _inventory.AddRange(update.Items);

        RefreshInventoryPanel();
        RefreshPotionBar();
        if (_tradeActive)
            RefreshTradeBag();
    }

    private void InventoryButton_Click(object sender, RoutedEventArgs e) => ToggleInventory();

    private void ToggleInventory()
    {
        InventoryPanel.Visibility = InventoryPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
        if (InventoryPanel.Visibility == Visibility.Visible)
            RefreshInventoryPanel();
    }

    private void RefreshInventoryPanel()
    {
        InventoryList.Items.Clear();
        InventoryHint.Text = $"{_inventory.Count}/{GameConstants.InventorySize} slots. " +
                             "Click an item to equip/unequip.";

        foreach (var item in _inventory)
        {
            var def = ItemCatalog.Get(item.DefId);
            if (def is null)
                continue;

            var button = new Button
            {
                Content = ItemLabel(def, item.Equipped),
                Height = 30,
                Margin = new Thickness(0, 0, 0, 4),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 0, 0, 0),
                Foreground = item.Equipped ? Brushes.LightGreen : RarityBrush(def.Rarity),
                Background = item.Equipped
                    ? new SolidColorBrush(Color.FromArgb(60, 80, 220, 120))
                    : new SolidColorBrush(Color.FromArgb(40, 80, 100, 130))
            };
            var dto = item;
            button.Click += (_, _) => ShowEquipPopup(dto);
            InventoryList.Items.Add(button);
        }
    }

    private static string ItemLabel(ItemDef def, bool equipped)
    {
        string tag = equipped ? "[E] " : "";
        string req = ItemCatalog.RequiredLevel(def.Grade) > 0
            ? $" (Lv{ItemCatalog.RequiredLevel(def.Grade)})" : "";
        return $"{tag}{def.Name}  {def.Grade}/{def.Rarity}{req}";
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

    private void ClassButton_Click(object sender, RoutedEventArgs e)
    {
        if (_mySecondClass > 0)
        {
            AppendChat(new ChatMessage("SYSTEM",
                $"You are already a {ClassCatalog.Get(_mySecondClass)?.Name}.", ChatChannel.System));
            return;
        }

        if (_level < GameConstants.ClassChangeLevel)
        {
            AppendChat(new ChatMessage("SYSTEM",
                $"Class change unlocks at level {GameConstants.ClassChangeLevel} (you are {_level}).",
                ChatChannel.System));
            return;
        }

        ClassHint.Text = $"As a {_myRace} {_myBaseClass}, choose your path. This is permanent.";
        ClassOptions.Children.Clear();

        foreach (var def in ClassCatalog.OptionsFor(_myRace, _myBaseClass))
        {
            var (con, atk, wit, dex) = ClassCatalog.StatBonus(def.Archetype);
            var button = new Button
            {
                Content = $"{def.Name}  ({def.Archetype})  +{con}CON +{atk}ATK +{wit}WIT +{dex}DEX",
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
                Content = $"{def.Name}  {def.Grade}/{def.Rarity}",
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
                Content = $"{def.Name}  {def.Grade}/{def.Rarity}",
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
        if (StatsPanel.Visibility == Visibility.Visible)
            RefreshStatsPanel();
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

        string cls = st.SecondClass > 0
            ? ClassCatalog.Get(st.SecondClass)?.Name ?? "-"
            : $"{_myBaseClass} (base)";

        StatsList.Items.Add(MakeStatRow("Class", cls));
        StatsList.Items.Add(MakeStatRow("CON / ATK / WIT / DEX",
            $"{st.Con} / {st.Atk} / {st.Wit} / {st.Dex}"));
        StatsList.Items.Add(MakeStatRow("Max HP / MP", $"{st.MaxHp} / {st.MaxMp}"));
        StatsList.Items.Add(MakeStatRow("Attack Power", st.AttackPower.ToString()));
        StatsList.Items.Add(MakeStatRow("Defence", st.Defence.ToString()));
        StatsList.Items.Add(MakeStatRow("Accuracy / Evasion", $"{st.Accuracy} / {st.Evasion}"));
        StatsList.Items.Add(MakeStatRow("Crit Chance", $"{st.CritChance * 100:0.#}%"));
        StatsList.Items.Add(MakeStatRow("Attack Range", $"{st.BasicAttackRange:0}"));
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

    /// <summary>Rebuilds the potion action bar from inventory: one slot per
    /// distinct potion type, showing the count.</summary>
    private void RefreshPotionBar()
    {
        PotionBar.Children.Clear();
        _potionSlots.Clear();

        var potionGroups = _inventory
            .Select(i => (Item: i, Def: ItemCatalog.Get(i.DefId)))
            .Where(t => t.Def is not null && ItemCatalog.IsPotion(t.Def!))
            .GroupBy(t => t.Def!.Id)
            .OrderBy(g => g.Key);

        foreach (var group in potionGroups)
        {
            var def = ItemCatalog.Get(group.Key)!;
            var first = group.First().Item;
            int count = group.Count();

            var button = new Button
            {
                Width = 110, Height = 30, Margin = new Thickness(3, 0, 3, 0),
                FontSize = 10, Foreground = RarityBrush(def.Rarity),
                Content = $"{def.Name} x{count}"
            };
            var instanceId = first.InstanceId;
            button.Click += async (_, _) => await DrinkPotion(instanceId);
            _potionSlots.Add(new PotionSlot { Button = button, InstanceId = instanceId });
            PotionBar.Children.Add(button);
        }

        PotionBar.Visibility = _potionSlots.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void UsePotionHotkey(int index)
    {
        if (index >= 0 && index < _potionSlots.Count)
            await DrinkPotion(_potionSlots[index].InstanceId);
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

        // Potions don't compare — drink directly from inventory click.
        if (ItemCatalog.IsPotion(def))
        {
            _ = DrinkPotion(item.InstanceId);
            return;
        }

        _equipPopupInstanceId = item.InstanceId;
        EquipPopupTitle.Text = item.Equipped ? $"Unequip {def.Name}" : def.Name;
        EquipPopupSubtitle.Text =
            $"{def.Grade}/{def.Rarity}" +
            (ItemCatalog.RequiredLevel(def.Grade) > 0 ? $"  •  requires Lv{ItemCatalog.RequiredLevel(def.Grade)}" : "");

        // Find the currently equipped item in the same slot to diff against.
        var current = _inventory
            .Select(i => (Item: i, Def: ItemCatalog.Get(i.DefId)))
            .FirstOrDefault(t => t.Item.Equipped && t.Def is not null &&
                                 t.Def!.Slot == def.Slot && t.Item.InstanceId != item.InstanceId);

        EquipCompareList.Items.Clear();
        AddCompareRow("Attack", current.Def?.AtkBonus ?? 0, item.Equipped ? 0 : def.AtkBonus, current.Item is not null);
        AddCompareRow("Defence", current.Def?.DefBonus ?? 0, item.Equipped ? 0 : def.DefBonus, current.Item is not null);
        AddCompareRow("Max HP", current.Def?.HpBonus ?? 0, item.Equipped ? 0 : def.HpBonus, current.Item is not null);
        AddCompareRow("Max MP", current.Def?.MpBonus ?? 0, item.Equipped ? 0 : def.MpBonus, current.Item is not null);
        AddCompareRow("Evasion", current.Def?.EvaBonus ?? 0, item.Equipped ? 0 : def.EvaBonus, current.Item is not null);
        if (def.WeaponRange > 0 || (current.Def?.WeaponRange ?? 0) > 0)
            AddCompareRow("Range", (int)(current.Def?.WeaponRange ?? 0),
                item.Equipped ? 0 : (int)def.WeaponRange, current.Item is not null);

        EquipConfirmButton.Content = item.Equipped ? "Unequip" : "Equip";
        EquipPopup.Visibility = Visibility.Visible;
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

}
