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

        string cls = st.SecondClass > 0
            ? ClassCatalog.Get(st.SecondClass)?.Name ?? "-"
            : $"{_myBaseClass} (base)";

        StatsList.Items.Add(MakeStatRow("Class", cls));
        StatsList.Items.Add(MakeStatRow("CON / ATK / WIT / DEX",
            $"{st.Con} / {st.Atk} / {st.Wit} / {st.Dex}"));
        StatsList.Items.Add(MakeStatRow("Max HP / MP", $"{st.MaxHp} / {st.MaxMp}"));
        StatsList.Items.Add(MakeStatRow("P.Atk / M.Atk", $"{st.AttackPower} / {st.MagicAttack}"));
        StatsList.Items.Add(MakeStatRow("Defence (Phys / Magic)", $"{st.Defence} / {st.MagicDefence}"));
        StatsList.Items.Add(MakeStatRow("Accuracy / Evasion", $"{st.Accuracy} / {st.Evasion}"));
        StatsList.Items.Add(MakeStatRow("Crit (Phys / Magic)",
            $"{st.CritChance * 100:0.#}% / {st.MagicCritChance * 100:0.#}%"));
        if (st.HasShield)
            StatsList.Items.Add(MakeStatRow("Block (chance / reduce / def)",
                $"{st.BlockChance * 100:0.#}% / {st.BlockReduction * 100:0.#}% / +{st.ShieldDefense}"));
        StatsList.Items.Add(MakeStatRow("Attack Range", $"{st.BasicAttackRange:0}"));
        StatsList.Items.Add(MakeStatRow("Move Speed", $"{st.MoveSpeed:0}"));

        // Cast reduction: WIT vs baseline 25 (negative modifier = faster),
        // combined with item Cast Speed attributes.
        float witPct = -st.CastModifier * 100f;            // +12 = 12% faster from WIT
        float itemPct = (1f - st.CastSpeedMult) * 100f;    // item cast-speed attribute
        float totalFaster = witPct + itemPct;
        string castLabel = totalFaster >= 0
            ? $"{totalFaster:0.#}% faster"
            : $"{-totalFaster:0.#}% slower";
        StatsList.Items.Add(MakeStatRow("Cast Speed", castLabel));
        StatsList.Items.Add(MakeStatRow("  from WIT", $"{(witPct>=0?"+":"")}{witPct:0.#}% (base 25 WIT)"));
        if (itemPct > 0)
            StatsList.Items.Add(MakeStatRow("  from items", $"+{itemPct:0.#}%"));

        float atkSpeedPct = (1f - st.AttackSpeedMult) * 100f;
        if (atkSpeedPct > 0)
            StatsList.Items.Add(MakeStatRow("Attack Speed", $"+{atkSpeedPct:0.#}%"));
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
        var (con, atk, wit, dex) = ClassCatalog.StatBonus(sc.Archetype);
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
            role + $"\n\nClass-change bonus: +{con} CON, +{atk} ATK, +{wit} WIT, +{dex} DEX.\n\n" +
            "Skills (base + signature):", skills);
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

                var assign = new Button
                {
                    Content = onBar ? "On Bar" : "To Bar",
                    Height = 24, Width = 70, FontSize = 10, IsEnabled = !onBar
                };
                string id = def.Id;
                assign.Click += (_, _) => AssignSkillToBar(id);
                DockPanel.SetDock(assign, Dock.Right);
                row.Children.Add(assign);

                var name = new TextBlock
                {
                    Text = SkillDisplayName(def.Id, def.Name),
                    Foreground = Brushes.White, FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = SkillTooltip(def)
                };
                row.Children.Add(name);
                SkillsList.Items.Add(row);
            }
        }
    }

    /// <summary>Tab 2: learnable skills grouped by required level, with Learn
    /// buttons enabled when level + SP (+ previous rank) allow.</summary>
    private void BuildLearnTab()
    {
        var all = ClassSkills.ForClass(_myRace, _myBaseClass, CurrentArchetype);

        // Unlearned only, grouped by learn level.
        var groups = all
            .Where(cs => !_learnedSkills.Contains(cs.SkillId))
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

                bool prevRankOk = def.Rank <= 1 || HasPreviousRankLearned(def);
                bool canLearn = levelMet && _skillPoints >= def.SpCost && prevRankOk;

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
                    Text = $"{SkillDisplayName(def.Id, def.Name)}  (SP {def.SpCost})",
                    Foreground = canLearn ? Brushes.White : Brushes.Gray,
                    FontSize = 13, VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = SkillTooltip(def)
                };
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

    private bool HasPreviousRankLearned(SkillDef def)
    {
        foreach (var id in _learnedSkills)
        {
            var l = SkillCatalog.Get(id);
            if (l is not null && l.BuffKey == def.BuffKey && l.Rank == def.Rank - 1)
                return true;
        }
        return false;
    }

    private void OpenLearnPopup(string skillId)
    {
        var def = SkillCatalog.Get(skillId);
        if (def is null) return;

        _pendingLearnId = skillId;
        LearnTitle.Text = def.Name;
        LearnBody.Text = SkillTooltip(def);
        bool enough = _skillPoints >= def.SpCost;
        LearnCost.Text = $"Cost: {def.SpCost} SP   (you have {_skillPoints})";
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

    private static string CategoryName(SkillCategory c) => c switch
    {
        SkillCategory.Physical => "Physical Skills",
        SkillCategory.Magic => "Magic Skills",
        SkillCategory.Buff => "Buffs",
        SkillCategory.Debuff => "Debuffs",
        SkillCategory.Heal => "Heals",
        _ => "Other"
    };

    private static string SkillTooltip(SkillDef def)
    {
        string duration = def.DurationTicks > 0
            ? $"  Duration {def.DurationTicks * GameConstants.TickSeconds:0}s" : "";
        return $"{def.Description}\n" +
               $"MP {def.MpCost}   Cast {def.CastTicks * GameConstants.TickSeconds:0.0}s   " +
               $"Cooldown {def.CooldownTicks * GameConstants.TickSeconds:0}s{duration}";
    }

    /// <summary>Server -> client: learned skill ids + SP. Refresh bar + window.</summary>
    private void OnLearned(LearnedSkills learned)
    {
        _learnedSkills.Clear();
        foreach (var id in learned.SkillIds)
            _learnedSkills.Add(id);
        _skillPoints = learned.SkillPoints;

        AutoPlaceNewSkills();
        RenderSkillBar();
        if (SkillsPanel.Visibility == Visibility.Visible)
            RefreshSkillsWindow();
    }

    // =======================================================================
    // Buff bar
    // =======================================================================

    private void OnBuffs(BuffUpdate update)
    {
        BuffBar.Items.Clear();
        foreach (var buff in update.Buffs)
        {
            var pill = new Border
            {
                Background = buff.IsDebuff
                    ? new SolidColorBrush(Color.FromArgb(200, 120, 40, 40))
                    : new SolidColorBrush(Color.FromArgb(200, 40, 80, 120)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 4, 4),
                Child = new TextBlock
                {
                    Text = $"{buff.Name}  {buff.SecondsLeft:0}s",
                    Foreground = Brushes.White, FontSize = 11
                },
                ToolTip = $"{buff.Name}\n{buff.Description}\n{buff.SecondsLeft:0}s remaining"
            };
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

    private void DebugButton_Click(object sender, RoutedEventArgs e)
    {
        BuildDebugMenu();
        DebugPanel.Visibility = DebugPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private void DebugClose_Click(object sender, RoutedEventArgs e) =>
        DebugPanel.Visibility = Visibility.Collapsed;

    private void BuildDebugMenu()
    {
        DebugList.Children.Clear();

        DebugList.Children.Add(DebugAction("Level +1", async () => await _net.DebugLevelAsync()));

        AddDebugHeader("Legendary");
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.GodWeapon, "God's Judgment"));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.GodArmor, "God's Robes"));

        AddDebugHeader("Rare Weapons (E)");
        DebugList.Children.Add(DebugGiveButton(
            ItemCatalog.WeaponKey(WeaponType.Sword, ItemGrade.E, ItemRarity.Rare), "Rare Sword"));
        DebugList.Children.Add(DebugGiveButton(
            ItemCatalog.WeaponKey(WeaponType.Dual, ItemGrade.E, ItemRarity.Rare), "Rare Daggers"));
        DebugList.Children.Add(DebugGiveButton(
            ItemCatalog.WeaponKey(WeaponType.Bow, ItemGrade.E, ItemRarity.Rare), "Rare Bow"));
        DebugList.Children.Add(DebugGiveButton(
            ItemCatalog.WeaponKey(WeaponType.Blunt, ItemGrade.E, ItemRarity.Rare), "Rare Staff"));

        AddDebugHeader("Rare Armor (E)");
        DebugList.Children.Add(DebugGiveButton(
            ItemCatalog.ArmorKey(ArmorWeight.Heavy, ItemGrade.E, ItemRarity.Rare), "Rare Heavy"));
        DebugList.Children.Add(DebugGiveButton(
            ItemCatalog.ArmorKey(ArmorWeight.Light, ItemGrade.E, ItemRarity.Rare), "Rare Light"));
        DebugList.Children.Add(DebugGiveButton(
            ItemCatalog.ArmorKey(ArmorWeight.Robe, ItemGrade.E, ItemRarity.Rare), "Rare Robe"));

        AddDebugHeader("Scrolls (x10)");
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.ScrollCommon, "Common Scroll x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.ScrollUncommon, "Uncommon Scroll x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.ScrollRare, "Rare Scroll x10", 10));

        AddDebugHeader("Attribute Scrolls (x10)");
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.AttrScrollCommon, "Attr Scroll (Common) x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.AttrScrollUncommon, "Attr Scroll (Uncommon) x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.AttrScrollRare, "Attr Scroll (Rare) x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.AttrScrollLegendary, "Attr Scroll (Legendary) x10", 10));

        AddDebugHeader("Potions (x10)");
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.MinorPotion, "Minor Potion x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.HealingPotion, "Healing Potion x10", 10));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.GreaterPotion, "Greater Potion x10", 10));
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
        DialogNpcRole.Text = dialog.NpcRole == "ClassChange" ? "Class Master" : "Quest Giver";
        DialogContent.Children.Clear();

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
        }

        // Class-change options.
        foreach (var c in dialog.ClassChanges)
        {
            AddDialogHeader($"Class Change: {c.ClassName}");
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
