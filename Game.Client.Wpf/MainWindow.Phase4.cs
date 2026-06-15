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

    private async void UsePotionHotkey(int index)
    {
        // Q -> first potion square (Minor), E -> second (Healing).
        if (index < 0 || index >= PotionSquares.Length)
            return;
        int defId = PotionSquares[index].DefId;
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

        bool hasCurrent = current.Item is not null;
        int curEnch = current.Item?.Enchant ?? 0;
        int newEnch = item.Enchant;

        // Enchant-aware bonuses (a +5 sword really is stronger than a +0).
        int CurB(int b) => EnchantRules.BonusAt(b, curEnch);
        int NewB(int b) => item.Equipped ? 0 : EnchantRules.BonusAt(b, newEnch);

        EquipCompareList.Items.Clear();
        AddCompareRow("Attack", CurB(current.Def?.AtkBonus ?? 0), NewB(def.AtkBonus), hasCurrent);
        AddCompareRow("Defence", CurB(current.Def?.DefBonus ?? 0), NewB(def.DefBonus), hasCurrent);
        AddCompareRow("Max HP", CurB(current.Def?.HpBonus ?? 0), NewB(def.HpBonus), hasCurrent);
        AddCompareRow("Max MP", CurB(current.Def?.MpBonus ?? 0), NewB(def.MpBonus), hasCurrent);
        AddCompareRow("Evasion", CurB(current.Def?.EvaBonus ?? 0), NewB(def.EvaBonus), hasCurrent);
        if (def.WeaponRange > 0 || (current.Def?.WeaponRange ?? 0) > 0)
            AddCompareRow("Range", (int)(current.Def?.WeaponRange ?? 0),
                item.Equipped ? 0 : (int)def.WeaponRange, hasCurrent);

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


    // =======================================================================
    // Character creation — class tree
    // =======================================================================

    private void BuildCreationTree()
    {
        CreationTree.Children.Clear();
        AddTreeHeader("1. Choose a Race");

        foreach (var race in Enum.GetValues<Race>())
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
        foreach (var r in Enum.GetValues<Race>())
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
        var skills = ClassProgression.UsableSkills(race, baseClass, null, 1).ToList();
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
        var skills = ClassProgression.UsableSkills(sc.Race, sc.Base, sc.Archetype, 20).ToList();
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

    private void RefreshSkillsWindow()
    {
        SkillsList.Items.Clear();
        Archetype? archetype = _mySecondClass > 0 ? ClassCatalog.Get(_mySecondClass)?.Archetype : null;

        foreach (var def in ClassProgression.UsableSkills(_myRace, _myBaseClass, archetype, _level))
        {
            bool onBar = _skillBar.Any(x => x == def.Id);

            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            var header = new DockPanel();
            var name = new TextBlock
            {
                Text = def.Name, Foreground = Brushes.White, FontSize = 13,
                FontWeight = FontWeights.Bold
            };
            var assign = new Button
            {
                Content = onBar ? "On Bar" : "To Bar",
                Height = 22, Width = 70, FontSize = 10, IsEnabled = !onBar
            };
            int id = def.Id;
            assign.Click += (_, _) => AssignSkillToBar(id);
            DockPanel.SetDock(assign, Dock.Right);
            header.Children.Add(assign);
            header.Children.Add(name);
            panel.Children.Add(header);

            panel.Children.Add(new TextBlock
            {
                Text = SkillCatalog.DescriptionOf(def.Id),
                Foreground = Brushes.Gainsboro, FontSize = 11, TextWrapping = TextWrapping.Wrap
            });

            string duration = def.DurationTicks > 0
                ? $"  Duration {def.DurationTicks * GameConstants.TickSeconds:0}s" : "";
            panel.Children.Add(new TextBlock
            {
                Text = $"MP {def.MpCost}   Cast {def.CastTicks * GameConstants.TickSeconds:0.0}s   " +
                       $"Cooldown {def.CooldownTicks * GameConstants.TickSeconds:0}s{duration}",
                Foreground = Brushes.SkyBlue, FontSize = 10, Margin = new Thickness(0, 2, 0, 0)
            });

            SkillsList.Items.Add(panel);
        }
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

    private static readonly (int DefId, Color Color)[] PotionSquares =
    {
        (30, Color.FromRgb(120, 200, 120)),  // Minor — green
        (31, Color.FromRgb(90, 150, 230)),   // Healing — blue
        (32, Color.FromRgb(220, 170, 70)),   // Greater — gold
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

            var badge = new TextBlock
            {
                Text = CountBadge(count),
                Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 3, 1)
            };

            var label = new TextBlock
            {
                Text = def.Rarity.ToString()[..1],  // M/U/R-ish initial of rarity
                Foreground = Brushes.White, FontSize = 16, FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var grid = new Grid();
            grid.Children.Add(label);
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

            int id = defId;
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

        foreach (var scrollDefId in new[] { 40, 41, 42 })
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

        AddDebugHeader("Scrolls");
        DebugList.Children.Add(DebugGiveButton(40, "Common Scroll"));
        DebugList.Children.Add(DebugGiveButton(41, "Uncommon Scroll"));
        DebugList.Children.Add(DebugGiveButton(42, "Rare Scroll"));

        AddDebugHeader("Potions");
        DebugList.Children.Add(DebugGiveButton(30, "Minor Potion"));
        DebugList.Children.Add(DebugGiveButton(31, "Healing Potion"));
        DebugList.Children.Add(DebugGiveButton(32, "Greater Potion"));

        AddDebugHeader("Gear (F)");
        DebugList.Children.Add(DebugGiveButton(7, "Knight's Blade (rare)"));
        DebugList.Children.Add(DebugGiveButton(11, "Plate Armor"));
        DebugList.Children.Add(DebugGiveButton(13, "Mystic Robe"));

        AddDebugHeader("Gear (E)");
        DebugList.Children.Add(DebugGiveButton(17, "Crusader Blade"));
        DebugList.Children.Add(DebugGiveButton(18, "Full Plate"));
        DebugList.Children.Add(DebugGiveButton(20, "Arcane Robe"));
    }

    private void AddDebugHeader(string text) =>
        DebugList.Children.Add(new TextBlock
        {
            Text = text, Foreground = Brushes.Gray, FontSize = 10,
            FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 2)
        });

    private System.Windows.Controls.Button DebugGiveButton(int defId, string label)
    {
        return DebugAction(label, async () => await _net.DebugGiveAsync(defId));
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

}
