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
        // Item bar slots show a live count and grey out at zero, so re-render the bar when the bag
        // changes if any slot holds an item.
        if (_skillBarLoaded && _skillBar.Any(GameConstants.IsItemSlot))
            RenderSkillBar();
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

    /// <summary>Inventory tabs. EQUIPPED is its own pane so what you're wearing is not buried in a bag
    /// of 40 other things — the bag was clogged and swapping gear was painful. The BAG tab therefore
    /// shows only what is NOT equipped: an item lives in exactly one tab, never both.</summary>
    private enum InvTab { Equipped, Bag, Quest }

    private InvTab _invTab = InvTab.Bag;

    private static bool IsGearItem(InventoryItemDto item)
    {
        var d = ItemCatalog.Get(item.DefId);
        return d is not null && !ItemCatalog.IsQuestItem(d);
    }

    private void InvTabEquipped_Click(object sender, RoutedEventArgs e)
    {
        _invTab = InvTab.Equipped;
        RefreshInventoryPanel();
    }

    private void InvTabGear_Click(object sender, RoutedEventArgs e)
    {
        _invTab = InvTab.Bag;
        RefreshInventoryPanel();
    }

    private void InvTabQuest_Click(object sender, RoutedEventArgs e)
    {
        _invTab = InvTab.Quest;
        RefreshInventoryPanel();
    }

    private void RefreshInventoryPanel()
    {
        InventoryList.Items.Clear();
        InvTabEquipped.FontWeight = _invTab == InvTab.Equipped ? FontWeights.Bold : FontWeights.Normal;
        InvTabGear.FontWeight     = _invTab == InvTab.Bag      ? FontWeights.Bold : FontWeights.Normal;
        InvTabQuest.FontWeight    = _invTab == InvTab.Quest    ? FontWeights.Bold : FontWeights.Normal;

        int worn = _inventory.Count(i => i.Equipped && IsGearItem(i));
        RefreshInventoryGold();
        InventoryHint.Text = _invTab switch
        {
            InvTab.Quest    => "Quest items — cannot be dropped or traded.",
            InvTab.Equipped => $"{worn} equipped. Click a piece to take it off.",
            _               => $"{_inventory.Count(IsGearItem) - worn} in bag " +
                               $"({_inventory.Count(IsGearItem)}/{GameConstants.InventorySize} slots used). " +
                               "Click an item to equip it.",
        };

        // The EQUIPPED pane reads like a character sheet, so order it by body slot rather than by
        // whatever order the item happens to sit at in the bag.
        var items = _invTab == InvTab.Equipped
            ? _inventory.OrderBy(i => (int)(ItemCatalog.Get(i.DefId)?.Slot ?? 0))
                        .ThenBy(i => (int)(ItemCatalog.Get(i.DefId)?.ArmorSlot ?? 0))
                        .ToList()
            : _inventory;

        foreach (var item in items)
        {
            var def = ItemCatalog.Get(item.DefId);
            if (def is null)
                continue;

            // Tab filter. An item lives in exactly ONE tab — in particular the BAG hides what you are
            // wearing, which is the whole point of giving Equipped its own pane.
            bool isQuest = ItemCatalog.IsQuestItem(def);
            bool show = _invTab switch
            {
                InvTab.Quest    => isQuest,
                InvTab.Equipped => !isQuest && item.Equipped,
                _               => !isQuest && !item.Equipped,
            };
            if (!show)
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

            // On the EQUIPPED pane this button UNEQUIPS (orange [U]); on the BAG it DESTROYS (red [X]).
            // A red delete sitting on the gear you are WEARING is a footgun — the destructive action and
            // the everyday one looked identical and lived in the same spot (owner, 2026-07-17).
            Button remove;
            if (_invTab == InvTab.Equipped)
            {
                remove = new Button
                {
                    Content = "U", Width = 24, Height = 28, FontSize = 11,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromArgb(140, 200, 120, 40)),   // orange: take off
                    ToolTip = "Unequip this item"
                };
                remove.Click += async (_, _) => await _net.EquipItemAsync(dto.InstanceId);   // equip toggles
            }
            else
            {
                remove = new Button
                {
                    Content = "X", Width = 24, Height = 28, FontSize = 11,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromArgb(120, 160, 60, 60)),
                    ToolTip = "Destroy this item"
                };
                // A SINGLE item deletes instantly (a confirm there would only annoy). A STACK asks whether
                // to drop ALL or just ONE — so you don't nuke 40 potions to shed one.
                remove.Click += async (_, _) => await RemoveItemPrompt(dto);
            }
            DockPanel.SetDock(remove, Dock.Right);
            row.Children.Add(remove);

            // BAG tab: a "Bar" button on a CONSUMABLE puts it on the skill bar, so you can then USE it
            // from the bar (click to drink/cast) instead of digging through the bag each time.
            if (_invTab == InvTab.Bag && def.Slot == EquipSlot.Consumable
                && SkillCatalog.Get(def.UseSkillId) is not null)
            {
                string defId = def.Id;
                var toBar = new Button
                {
                    Content = "Bar", Width = 34, Height = 28, FontSize = 10,
                    Foreground = Brushes.White, Margin = new Thickness(0, 0, 3, 0),
                    Background = new SolidColorBrush(Color.FromArgb(140, 60, 110, 160)),   // blue: to bar
                    ToolTip = "Put on the skill bar (use it from there)"
                };
                toBar.Click += (_, _) => AssignItemToBar(defId);
                DockPanel.SetDock(toBar, Dock.Right);
                row.Children.Add(toBar);
            }

            // BAG tab: an orange [E] to equip straight from the bag, without going through the compare
            // popup — the mirror of the Equipped tab's [U]. Sits BEFORE the red [X] so the everyday
            // action is never adjacent-and-identical to the destructive one.
            if (_invTab == InvTab.Bag && ItemCatalog.IsEquippable(def))
            {
                var equip = new Button
                {
                    Content = "E", Width = 24, Height = 28, FontSize = 11,
                    Foreground = Brushes.White, Margin = new Thickness(0, 0, 3, 0),
                    Background = new SolidColorBrush(Color.FromArgb(140, 200, 120, 40)),   // orange: put on
                    ToolTip = "Equip this item"
                };
                equip.Click += async (_, _) => await _net.EquipItemAsync(dto.InstanceId);
                DockPanel.SetDock(equip, Dock.Right);
                row.Children.Add(equip);
            }

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

            // Main item button — opens equip/compare popup (potions drink). On the EQUIPPED pane the
            // "[E]" marker is noise (everything there is equipped), so name the body SLOT instead —
            // that's what makes the pane read as a character sheet you can swap gear from.
            var button = new Button
            {
                Content = _invTab == InvTab.Equipped
                    ? $"{EquipSlotLabel(def),-9} {ItemLabel(def, false, item.Enchant, item.Quantity)}"
                    : ItemLabel(def, item.Equipped, item.Enchant, item.Quantity),
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

    /// <summary>Which body slot a piece occupies, for the Equipped pane. Armor reports its ArmorSlot
    /// (Head/Body/Gloves/Boots), everything else its EquipSlot (Weapon/Shield/Jewel).</summary>
    private static string EquipSlotLabel(ItemDef def) => def.Slot switch
    {
        EquipSlot.Armor => def.ArmorSlot.ToString(),
        EquipSlot.Jewel => def.JewelType.ToString(),
        _ => def.Slot.ToString(),
    };

    private static string ItemLabel(ItemDef def, bool equipped, int enchant, int quantity)
    {
        string tag = equipped ? "[E] " : "";
        string ench = enchant > 0 ? $"+{enchant} " : "";
        string qty = quantity > 1 ? $"  x{(quantity >= 100 ? "99+" : quantity.ToString())}" : "";
        string req = ItemCatalog.RequiredLevel(def) > 0
            ? $" (Lv{ItemCatalog.RequiredLevel(def)})" : "";
        // Grade letter comes from the TIER ladder (F/E/D/C/B/A), not the pricing-only ItemGrade enum.
        return $"{tag}{ench}{def.Name}  {GradePenalty.GradeNameOf(def)}/{def.Rarity}{req}{qty}";
    }

    /// <summary>The item tooltip. Returns a rich element (not a string) so the SET section can
    /// colour each piece: green = you're wearing it, grey = you're missing it.
    ///
    /// It builds an explicit ToolTip with a DARK background. WPF's default tooltip chrome is LIGHT,
    /// and the content here is white/gainsboro — so all of this was white-on-white and effectively
    /// invisible, which is a large part of why the set info "wasn't there".</summary>
    private object BuildItemTooltip(ItemDef def, InventoryItemDto item)
    {
        var panel = new StackPanel { MaxWidth = 320 };
        panel.Children.Add(new TextBlock
        {
            Text = BuildItemTooltipText(def, item),
            Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap
        });
        if (BuildSetSection(def) is StackPanel setPanel)
            panel.Children.Add(setPanel);

        return new ToolTip
        {
            Content = panel,
            Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x22, 0x30)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x5A, 0x70)),
            Foreground = Brushes.White,
            Padding = new Thickness(8),
        };
    }

    /// <summary>The SET section: which set this piece belongs to, what the bonus gives, how many of
    /// the required pieces you're actually wearing, and a green/grey line per piece. Null if the
    /// item isn't a set piece.</summary>
    private StackPanel? BuildSetSection(ItemDef def)
    {
        if (string.IsNullOrEmpty(def.SetId)) return null;

        // Show the set section ONLY on the set-defining BODY armor (owner). Accessories (boots / gloves /
        // helm) carry a SetId too — they're shared across a tier's bodies — but a boot is "just a boot",
        // not the piece that grants the set bonus, and showing "3/4 heavy set" on it was confusing (it
        // even lingered after the body was swapped to a robe). The body is the set's identity; ask it.
        if (!(def.Slot == EquipSlot.Armor && def.ArmorSlot == ArmorSlot.Body)) return null;

        // The bonus is BODY-driven, so find the set whose body line this piece belongs to — an
        // accessory is shared across every body of its tier, so it can complete several sets.
        var sets = ArmorSetCatalog.All
            .Where(s => s.Id == def.SetId
                     || (string.IsNullOrEmpty(s.AccessorySetId) ? s.Id : s.AccessorySetId) == def.SetId)
            .ToList();
        if (sets.Count == 0) return null;

        var equippedBody = _inventory.FirstOrDefault(i => i.Equipped
            && ItemCatalog.Get(i.DefId) is { Slot: EquipSlot.Armor, ArmorSlot: ArmorSlot.Body });
        string wornBodySet = equippedBody is null ? "" : (ItemCatalog.Get(equippedBody.DefId)?.SetId ?? "");
        // Show the set matching the body you're actually wearing, else this item's own set.
        var set = sets.FirstOrDefault(s => s.Id == wornBodySet) ?? sets[0];

        var accId = string.IsNullOrEmpty(set.AccessorySetId) ? set.Id : set.AccessorySetId;
        var slots = set.RequiredSlots
            ?? new[] { ArmorSlot.Body, ArmorSlot.Head, ArmorSlot.Gloves, ArmorSlot.Boots };

        // Per slot: the set's piece for it, and whether it's worn.
        var pieces = new List<(string Name, bool Worn)>();
        foreach (var slot in slots)
        {
            string needSet = slot == ArmorSlot.Body ? set.Id : accId;
            var piece = ItemCatalog.AllItems.FirstOrDefault(d =>
                d.Slot == EquipSlot.Armor && d.ArmorSlot == slot && d.SetId == needSet);
            bool worn = _inventory.Any(i => i.Equipped
                && ItemCatalog.Get(i.DefId) is { } wd
                && wd.Slot == EquipSlot.Armor && wd.ArmorSlot == slot && wd.SetId == needSet);
            pieces.Add((piece?.Name ?? slot.ToString(), worn));
        }

        int have = pieces.Count(p => p.Worn);
        bool active = have == pieces.Count;
        var green = new SolidColorBrush(Color.FromRgb(120, 220, 130));
        var grey = new SolidColorBrush(Color.FromRgb(150, 150, 150));

        var panel = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        panel.Children.Add(new TextBlock { Text = $"— {set.Name} set —", Foreground = Brushes.Gainsboro });
        panel.Children.Add(new TextBlock
        {
            Text = $"Bonus: {SetBonusText(set)}",
            Foreground = active ? green : grey,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Items {have}/{pieces.Count}",
            Foreground = active ? green : grey
        });
        foreach (var (name, worn) in pieces)
            panel.Children.Add(new TextBlock
            {
                Text = $"   {(worn ? "✔" : "✖")} {name}",
                Foreground = worn ? green : grey
            });

        // ---- The set's SHIELD, if it has one. It is NOT one of the required pieces — wearing it
        // just adds an EXTRA bonus on top (and only the def-oriented heavy sets define one). ----
        var shield = ItemCatalog.AllItems.FirstOrDefault(d => d.Slot == EquipSlot.Shield && d.SetId == set.Id);
        if (shield is not null)
        {
            bool shieldWorn = _inventory.Any(i => i.Equipped
                && ItemCatalog.Get(i.DefId) is { Slot: EquipSlot.Shield } wd && wd.SetId == set.Id);
            // The extra only actually applies when the 4-piece set is ALSO complete.
            bool shieldActive = shieldWorn && active;
            panel.Children.Add(new TextBlock
            {
                Text = $"Shield Bonus: {ShieldBonusText(set)}",
                Foreground = shieldActive ? green : grey,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"   {(shieldWorn ? "✔" : "✖")} {shield.Name}",
                Foreground = shieldWorn ? green : grey
            });
        }
        return panel;
    }

    /// <summary>What a set's SHIELD-conditional extra gives (only while its own shield is worn).</summary>
    private static string ShieldBonusText(ArmorSetDef set)
    {
        var s = set.ShieldBonus;
        var parts = new List<string>();
        if (s.MaxHp != 0) parts.Add($"+{s.MaxHp} HP");
        if (s.PDef != 0) parts.Add($"+{s.PDef:0} P.Def");
        if (s.MDef != 0) parts.Add($"+{s.MDef:0} M.Def");
        if (s.PDefPct != 0f) parts.Add($"+{s.PDefPct * 100:0}% P.Def");
        if (s.MDefPct != 0f) parts.Add($"+{s.MDefPct * 100:0}% M.Def");
        if (s.PAtkPct != 0f) parts.Add($"+{s.PAtkPct * 100:0}% P.Atk");
        if (s.ShieldDefPct != 0f) parts.Add($"+{s.ShieldDefPct * 100:0}% Shield Def");
        if (s.Reflect != 0f) parts.Add($"reflect {s.Reflect * 100:0}% of melee damage");
        if (s.CcResist != 0f) parts.Add($"+{s.CcResist * 100:0}% CC Resist");
        return parts.Count > 0 ? string.Join(", ", parts) : "—";
    }

    /// <summary>One-line summary of what a set's bonus actually gives.</summary>
    private static string SetBonusText(ArmorSetDef set)
    {
        var parts = new List<string>();
        var b = set.Bonus;
        if (b.MaxHp != 0) parts.Add($"+{b.MaxHp} HP");
        if (b.MaxMp != 0) parts.Add($"+{b.MaxMp} MP");
        if (b.Defence != 0) parts.Add($"+{b.Defence} Def");
        if (b.Attack != 0) parts.Add($"+{b.Attack} Atk");
        if (b.Accuracy != 0) parts.Add($"+{b.Accuracy} Acc");
        if (b.Evasion != 0) parts.Add($"+{b.Evasion} Eva");
        if (set.DefencePct != 0f) parts.Add($"+{set.DefencePct * 100:0}% P.Def");
        if (set.CastSpeedPct != 0f) parts.Add($"+{set.CastSpeedPct * 100:0}% Cast Speed");

        var m = set.Mods;
        if (m.MaxHp != 0) parts.Add($"+{m.MaxHp} HP");
        if (m.MaxMp != 0) parts.Add($"+{m.MaxMp} MP");
        if (m.PDef != 0) parts.Add($"+{m.PDef:0} P.Def");
        if (m.MDef != 0) parts.Add($"+{m.MDef:0} M.Def");
        if (m.PAtk != 0) parts.Add($"+{m.PAtk:0} P.Atk");
        if (m.MAtk != 0) parts.Add($"+{m.MAtk:0} M.Atk");
        if (m.PDefPct != 0f) parts.Add($"+{m.PDefPct * 100:0}% P.Def");
        if (m.MDefPct != 0f) parts.Add($"+{m.MDefPct * 100:0}% M.Def");
        if (m.CastSpeedPct != 0f) parts.Add($"+{m.CastSpeedPct * 100:0}% Cast Speed");
        if (m.AtkSpeedPct != 0f) parts.Add($"+{m.AtkSpeedPct * 100:0}% Atk Speed");
        if (m.MoveSpeed != 0) parts.Add($"+{m.MoveSpeed:0} Speed");
        if (m.CcResist != 0f) parts.Add($"+{m.CcResist * 100:0}% CC Resist");

        return parts.Count > 0 ? string.Join(", ", parts) : "—";
    }

    private static string BuildItemTooltipText(ItemDef def, InventoryItemDto item)
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
        // Every consumable describes itself through the SKILL it grants.
        if (SkillCatalog.Get(def.UseSkillId) is SkillDef useDef)
            lines.Add($"Use: {useDef.Description}");
        // The SET section is rendered separately (BuildSetSection) so each piece can be coloured.
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
                bool taken = DisciplineTakenElsewhere(tc.Discipline);
                var button = new Button
                {
                    Content = taken
                        ? $"{tc.Name}  ({tc.Discipline}) — another class already walks this"
                        : $"{tc.Name}  ({tc.Discipline})",
                    Height = 32, Margin = new Thickness(0, 0, 0, 6), FontSize = 12,
                    IsEnabled = !taken,
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
            // No stat bonus to advertise: a class change no longer raises main stats. No archetype
            // restriction either — you may own several classes of the same 2nd class (they just can't
            // reach the same 3rd-class discipline).
            var button = new Button
            {
                Content = $"{def.Name}  ({def.Archetype})",
                Height = 32,
                Margin = new Thickness(0, 0, 0, 6),
                FontSize = 12,
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
    private int _myPkCount;
    private int _myPvpCount;

    private async void PvpButton_Click(object sender, RoutedEventArgs e) =>
        await _net.TogglePvpAsync(!_pvpEnabled);

    private async void CounterButton_Click(object sender, RoutedEventArgs e) =>
        await _net.ToggleCounterAttackAsync(!_counterEnabled);

    private void OnPvpState(PvpState s)
    {
        _pvpEnabled = s.Pvp;
        _counterEnabled = s.CounterAttack;
        _myKarma = s.Karma;
        _myPkCount = s.PkCount;
        _myPvpCount = s.PvpCount;
        if (StatsPanel.Visibility == Visibility.Visible)
            RefreshStatsPanel();   // karma/PK/PvP live-refresh in the character window
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

    // =======================================================================
    // Resurrection offer (a fallen player accepts/declines being revived)
    // =======================================================================

    /// <summary>An offer arrived: show it INSIDE the death overlay, above the Respawn button. It is not
    /// a separate window — as one it was drawn underneath the death overlay and could not be clicked
    /// until you had already respawned, which is exactly when the offer no longer matters.</summary>
    private void OnResurrectOffer(ResurrectOffer offer)
    {
        string exp = offer.ExpRestored > 0
            ? $"\nRestores {offer.ExpPct * 100:0}% of lost experience ({offer.ExpRestored:N0})."
            : "\nRestores none of your lost experience.";
        ResurrectText.Text = $"{offer.FromName} offers to resurrect you.{exp}";
        ResurrectOfferPanel.Visibility = Visibility.Visible;
    }

    /// <summary>Take the offer off screen WITHOUT touching the death overlay: declining (or letting the
    /// offer expire) must leave you dead, looking at the Respawn button, free to wait for a better
    /// resurrection than the one you just refused.</summary>
    private void HideResurrectPrompt() => ResurrectOfferPanel.Visibility = Visibility.Collapsed;

    private async void ResurrectAccept_Click(object sender, RoutedEventArgs e)
    {
        HideResurrectPrompt();
        await _net.ResurrectResponseAsync(true);
    }

    private async void ResurrectDecline_Click(object sender, RoutedEventArgs e)
    {
        HideResurrectPrompt();
        await _net.ResurrectResponseAsync(false);
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

        // Clicking anywhere on the row TARGETS that member (so a healer can select + heal an ally).
        // Background=Transparent makes the whole row hit-testable; it targets only, never attacks.
        var row = new StackPanel
        {
            Margin = new Thickness(0, 3, 0, 3),
            Background = Brushes.Transparent,
            Cursor = System.Windows.Input.Cursors.Hand,
            Tag = m.Id
        };
        row.Children.Add(titleRow);
        row.Children.Add(hpBar);
        row.Children.Add(mpBar);

        // Debuffs on this member, so a healer spots who to cleanse straight from the roster.
        //
        // ICONS/letters, not full names (owner, 2026-07-20): spelled out, three debuffs wrapped the row
        // to three lines and pushed the rest of the party off the panel. Each tile carries its own
        // tooltip, so the full name is one hover away and the roster stays a roster.
        if (m.Debuffs is { Length: > 0 })
        {
            var debuffRow = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };
            var debuffRed = new SolidColorBrush(Color.FromRgb(0xE0, 0x6C, 0x6C));
            foreach (var debuff in m.Debuffs)
            {
                string icon = SkillIcons.ForName(debuff);
                debuffRow.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0x50, 0x80, 0x20, 0x20)),
                    BorderBrush = debuffRed,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Margin = new Thickness(0, 0, 3, 0),
                    Padding = new Thickness(3, 0, 3, 0),
                    ToolTip = debuff,
                    Child = new TextBlock
                    {
                        Text = icon.Length > 0 ? icon : Abbreviations.For(debuff),
                        Foreground = debuffRed,
                        FontSize = 10,
                        FontWeight = FontWeights.SemiBold,
                    },
                });
            }
            row.Children.Add(debuffRow);
        }

        row.MouseLeftButtonDown += PartyRow_Target;
        return row;
    }

    private async void PartyKick_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Guid id })
            await _net.PartyKickAsync(id);
    }

    /// <summary>Click a party member's roster row → target them (targets only, never attacks a party
    /// member). Lets a healer select an ally in the party window and cast a heal on them.</summary>
    private void PartyRow_Target(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id })
        {
            _targetId = id;
            UpdateTargetFrame();
        }
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

        // Gold. Only overwrite MY box when it isn't focused, so it doesn't fight what I'm typing (the
        // server clamps to what I own and echoes the accepted value back).
        if (!TradeGoldBox.IsFocused)
            TradeGoldBox.Text = state.MyGold.ToString();
        TheirGoldText.Text = state.TheirGold.ToString("N0");

        TradeReadyButton.Content = state.MyReady ? "Ready ✓" : "Ready";
        TradeReadyButton.Background = state.MyReady
            ? new SolidColorBrush(Color.FromArgb(80, 80, 220, 120))
            : Brushes.LightGray;
    }

    private void TradeGoldBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter) TradeGoldBox_Commit(sender, e);
    }

    /// <summary>Send my gold offer to the server (which clamps it to what I actually have and resets
    /// both ready flags — same anti-bait-and-switch as changing items).</summary>
    private void TradeGoldBox_Commit(object sender, RoutedEventArgs e)
    {
        long gold = long.TryParse(TradeGoldBox.Text.Trim(), out var g) ? Math.Max(0, g) : 0;
        _ = _net.TradeGoldAsync(gold);
    }

    /// <summary>Destroy an item. Single → instant. Stack (qty&gt;1) → ask Yes = ALL, No = one, Cancel.</summary>
    private async System.Threading.Tasks.Task RemoveItemPrompt(InventoryItemDto dto)
    {
        if (dto.Quantity <= 1)
        {
            await _net.RemoveItemAsync(dto.InstanceId, all: false);
            return;
        }
        string name = ItemCatalog.Get(dto.DefId)?.Name ?? "this item";
        var r = MessageBox.Show(
            $"You have {dto.Quantity} {name}.\n\nYes = destroy ALL {dto.Quantity}\nNo = destroy just ONE",
            "Destroy items", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
        if (r == MessageBoxResult.Yes) await _net.RemoveItemAsync(dto.InstanceId, all: true);
        else if (r == MessageBoxResult.No) await _net.RemoveItemAsync(dto.InstanceId, all: false);
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
                // Dark background so the (often light) rarity-coloured text reads — the default WPF
                // button chrome is light grey and washed white/common items out (the skill-bar problem).
                // Use IsHitTestVisible (not IsEnabled) for the partner's offer: a DISABLED button is
                // greyed by WPF regardless of our colours, which is why "their offer" stayed unreadable.
                Background = new SolidColorBrush(Color.FromArgb(0xC0, 0x1A, 0x22, 0x30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x5A, 0x70)),
                Foreground = RarityBrush(def.Rarity),
                IsHitTestVisible = removable,
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
                // Dark background so the rarity-coloured text reads (matches the offer lists).
                Background = new SolidColorBrush(Color.FromArgb(0xC0, 0x1A, 0x22, 0x30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x5A, 0x70)),
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
        StatsList.Items.Add(MakeStatRow("CON / ATK / WIT / DEX / SPT",
            $"{st.Con} / {st.Atk} / {st.Wit} / {st.Dex} / {st.Spt}",
            "What each stat buys you:\n\n" +
            "CON  — Max HP and HP regeneration.\n" +
            "ATK  — the single power stat: P.Atk for fighters, M.Atk for mages\n" +
            "         (your WEAPON decides the split).\n" +
            "WIT  — cast speed and magic critical rate. NOT magic damage.\n" +
            "DEX  — accuracy, evasion, physical critical rate and attack speed.\n" +
            "SPT  — Spirit: Max MP, MP regeneration and M.Def.\n\n" +
            "The level-40 swap passives trade one of these for another."));
        StatsList.Items.Add(MakeStatRow("Max HP / MP", $"{st.MaxHp} / {st.MaxMp}",
            "Max HP comes from CON; Max MP from SPT. Both also scale with level and class."));
        StatsList.Items.Add(MakeStatRow("P.Atk / M.Atk", $"{st.AttackPower} / {st.MagicAttack}"));
        StatsList.Items.Add(MakeStatRow("M.Atk (internal / L2-ref)", $"{st.MagicAttackInternal:N0}"));
        StatsList.Items.Add(MakeStatRow("Defence (Phys / Magic)", $"{st.Defence} / {st.MagicDefence}"));
        StatsList.Items.Add(MakeStatRow("Heal power (flat / mod)", $"{st.HealPowerFlat} / ×{st.HealPowerMod:0.##}"));
        StatsList.Items.Add(MakeStatRow("Heal received (flat / mod)", $"{st.HealReceivedFlat} / ×{st.HealReceivedMod:0.##}"));
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

        // ----- Reputation (karma / PK / PvP), from the PvpState push, not the StatsUpdate -----
        StatsList.Items.Add(MakeStatRowColored("Karma", _myKarma.ToString("N0"), KarmaBrush(_myKarma)));
        StatsList.Items.Add(MakeStatRow("PK / PvP kills", $"{_myPkCount} / {_myPvpCount}"));

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

    /// <summary>Karma colour (owner): karma &gt;1000 is a fixed DARK red ("over the line"); from 1000
    /// down to 0 the red channel FADES from ~255 to ~50, so you can watch your bad value cool off as you
    /// grind it down. 0 karma = neutral (not a PK).</summary>
    private static Brush KarmaBrush(int karma)
    {
        if (karma <= 0) return Brushes.Gainsboro;
        if (karma > 1000) return new SolidColorBrush(Color.FromRgb(0x8B, 0, 0));   // DarkRed
        int r = 50 + (int)(205 * Math.Clamp(karma, 0, 1000) / 1000f);              // 255 @1000 → 50 @~0
        return new SolidColorBrush(Color.FromRgb((byte)r, 0, 0));
    }

    private static Grid MakeStatRowColored(string label, string value, Brush valueBrush)
    {
        var grid = MakeStatRow(label, value);
        if (grid.Children[1] is TextBlock v) v.Foreground = valueBrush;
        return grid;
    }

    private static Grid MakeStatRow(string label, string value, string? tooltip = null)
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

        // A stat's NAME says nothing about what it buys you (owner, 2026-07-20). The tooltip goes on
        // the whole row + both texts, because hovering the label or the number should both work.
        if (tooltip is not null)
        {
            grid.ToolTip = tooltip;
            grid.Background = Brushes.Transparent;   // a null background is not hit-testable
            l.ToolTip = tooltip;
            v.ToolTip = tooltip;
        }
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

    /// <summary>Use one consumable, from WHEREVER it was clicked — the bag or a skill-bar slot.
    ///
    /// It lives in one place because the two call sites disagreed and the bar was wrong: a resurrection
    /// scroll has to travel with the TARGET it is being used on (like the healer's res), and the bar sent
    /// the plain no-target message, so the server correctly answered "pick a friendly dead target" and
    /// the scroll appeared broken on the bar while working perfectly from the bag. Potions and Return
    /// have no target, which is why only the res scroll showed the split.</summary>
    private void UseConsumable(Guid instanceId, ItemDef def)
    {
        if (SkillCatalog.Get(def.UseSkillId) is { Resurrect: true })
        {
            if (_targetId is Guid rezTarget)
                _ = _net.UsePotionOnAsync(instanceId, rezTarget);
            else
                AppendChat(new ChatMessage("SYSTEM",
                    "Select a fallen ally first, then use the scroll.", ChatChannel.System));
            return;
        }
        if (ItemCatalog.IsBuffPotion(def))
            _ = _net.UsePotionAsync(instanceId);
        else
            _ = DrinkPotion(instanceId);
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
            UseConsumable(item.InstanceId, def);
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
            $"{GradePenalty.GradeNameOf(def)}-grade/{def.Rarity}" +
            (ItemCatalog.RequiredLevel(def) > 0 ? $"  •  full power at Lv{ItemCatalog.RequiredLevel(def)}" : "");

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

        // SET INFO — what the set needs, what it gives, and which pieces you're actually wearing.
        // This existed but was only ever attached to the hover TOOLTIP, never to this window — which
        // is the window you open when you're deciding what to wear, so it read as "set info missing".
        if (BuildSetSection(def) is StackPanel setSection)
            EquipCompareList.Items.Add(setSection);

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

    /// <summary>Which tab of the skills window is showing.</summary>
    private enum SkillTab { Learned, Learn, Actions }
    private SkillTab _skillTab = SkillTab.Learned;
    private bool _skillTabLearn => _skillTab == SkillTab.Learn;
    private string? _pendingLearnId;

    private void SkillTabLearned_Click(object sender, RoutedEventArgs e)
    {
        _skillTab = SkillTab.Learned;
        RefreshSkillsWindow();
    }

    private void SkillTabLearn_Click(object sender, RoutedEventArgs e)
    {
        _skillTab = SkillTab.Learn;
        RefreshSkillsWindow();
    }

    private void SkillTabActions_Click(object sender, RoutedEventArgs e)
    {
        _skillTab = SkillTab.Actions;
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
        TabLearned.FontWeight = _skillTab == SkillTab.Learned ? FontWeights.Bold : FontWeights.Normal;
        TabLearn.FontWeight = _skillTab == SkillTab.Learn ? FontWeights.Bold : FontWeights.Normal;
        TabActions.FontWeight = _skillTab == SkillTab.Actions ? FontWeights.Bold : FontWeights.Normal;

        switch (_skillTab)
        {
            case SkillTab.Learn: BuildLearnTab(); break;
            case SkillTab.Actions: BuildActionsTab(); break;
            default: BuildLearnedTab(); break;
        }
    }

    /// <summary>Tab 3: the built-in ACTIONS — the things you do constantly that aren't skills (attack,
    /// target closest, sit/stand, run/walk, trade, party invite, follow, assist).
    ///
    /// They get the same "To Bar" gesture as a skill because to the player they ARE bar entries; the
    /// only difference is that they're always available and never learned.</summary>
    private void BuildActionsTab()
    {
        AddSkillGroupHeader("Actions");
        foreach (var action in ActionCatalog.All)
        {
            string token = GameConstants.ActionSlotToken(action.Id);
            bool onBar = _skillBar.Any(x => x == token);

            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };

            var assign = new Button
            {
                Content = onBar ? "On Bar" : "To Bar",
                Height = 24, Width = 70, FontSize = 10, IsEnabled = !onBar,
            };
            assign.Click += (_, _) => AssignTokenToBar(token);
            DockPanel.SetDock(assign, Dock.Right);
            row.Children.Add(assign);

            var name = new TextBlock
            {
                Text = $"{action.Icon}  {action.Name}",
                Foreground = Brushes.White, FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = action.Description,
            };
            row.Children.Add(name);
            SkillsList.Items.Add(row);
        }

        SkillsList.Items.Add(new TextBlock
        {
            Text = "Actions are always available — no MP, no cooldown, nothing to learn.",
            Foreground = Brushes.Gray, FontSize = 11, TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0),
        });
    }

    /// <summary>Tab 1: skills you've learned, grouped by category, usable/bar-able.</summary>
    private void BuildLearnedTab()
    {
        _skillWindowCooldowns.Clear();   // rows are rebuilt below; drop the old TextBlock refs

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

                    // Live cooldown, so you can see what's ready without hunting along the bar.
                    // Ticked by UpdateSkillCooldowns; the row itself is not rebuilt each frame.
                    var cd = new TextBlock
                    {
                        Width = 44, FontSize = 11, FontWeight = FontWeights.Bold,
                        // Gold here (NOT DarkGoldenrod): this window is dark-backed, so it needs the
                        // opposite of the skill BAR, whose buttons are light grey.
                        Foreground = Brushes.Gold,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Right,
                        Visibility = Visibility.Collapsed
                    };
                    DockPanel.SetDock(cd, Dock.Right);
                    row.Children.Add(cd);
                    _skillWindowCooldowns.Add((id, cd));
                }

                // An explicit DETAILS button rather than "the name is secretly clickable" — an
                // invisible hit-target is not discoverable, and it sat on top of the row swallowing
                // mouse events.
                string detailId = def.Id;
                var details = new Button
                {
                    Content = "Details", Height = 24, Width = 60, FontSize = 10,
                    Margin = new Thickness(0, 0, 4, 0)
                };
                details.Click += (_, _) => OpenSkillDetail(detailId);
                DockPanel.SetDock(details, Dock.Right);
                row.Children.Add(details);

                string icon = SkillIcon(def);
                var name = new TextBlock
                {
                    Text = (icon.Length > 0 ? icon + "  " : "") + SkillDisplayName(def.Id, def.Name),
                    Foreground = Brushes.White, FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center
                };
                row.Children.Add(name);
                SkillsList.Items.Add(row);
            }
        }
    }

    /// <summary>Tab 2: learnable skills grouped by required level, with Learn
    /// buttons enabled when level + SP (+ previous rank) allow.</summary>
    /// <summary>True if this skill is no longer a legal pick — it vanishes from the learn list. Two
    /// things can lock it, and the server enforces both (this is only so the UI doesn't offer a pick
    /// that would be refused):
    /// <list type="bullet">
    ///   <item>a rival in the same mutually-exclusive GROUP is already learned (take +CON−DEX and
    ///         +CON−ATK is gone for good), or</item>
    ///   <item>the stat-swap DIRECTION rule — you may not raise a stat you have sold, nor sell a
    ///         stat you have bought. (See SkillCatalog.StatSwapConflict.)</item>
    /// </list>
    /// The skill you PICKED still shows, so you can keep levelling it.</summary>
    private bool LockedByExclusiveGroup(string skillId)
    {
        if (SkillCatalog.Get(skillId) is not SkillDef def) return false;
        if (_learnedSkills.Contains(skillId)) return false;   // it's the one you took

        if (!string.IsNullOrEmpty(def.ExclusiveGroup)
            && _learnedSkills.Any(id => SkillCatalog.Get(id) is { } other
                                        && other.ExclusiveGroup == def.ExclusiveGroup))
            return true;

        return SkillCatalog.StatSwapConflict(skillId, _learnedSkills) is not null;
    }

    private void BuildLearnTab()
    {
        var all = ClassSkills.LearnableAt(_myRace, _myBaseClass, CurrentArchetype, int.MaxValue, CurrentDiscipline);

        // Show only each skill's NEXT learnable level (the entry whose SkillLevel ==
        // current+1), and hide skills replaced by something you already know (Flame
        // Bolt → Magic Bolt). Grouped by the character level that unlocks it.
        var groups = all
            .Where(cs => cs.SkillLevel == _learnedLevels.GetValueOrDefault(cs.SkillId) + 1
                         && !SupersededByLearned(cs.SkillId)
                         && !LockedByExclusiveGroup(cs.SkillId))
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
                int gold = def.GoldCostAt(cs.SkillLevel);   // the stat-swap passives are bought with GOLD
                bool canLearn = levelMet && _skillPoints >= cost && (gold == 0 || _gold >= gold);
                string levelTag = def.MaxLevel > 1 ? $" Lv.{cs.SkillLevel}" : "";
                string priceTag = gold > 0
                    ? $"({gold:N0} {GameConstants.CurrencyName})"
                    : $"(SP {cost})";

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

                string icon = SkillIcon(def);
                var name = new TextBlock
                {
                    Text = $"{(icon.Length > 0 ? icon + "  " : "")}{SkillDisplayName(def.Id, def.Name)}{levelTag}  {priceTag}",
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
        int gold = def.GoldCostAt(target);   // the stat-swap passives are paid for in GOLD, not SP
        _pendingLearnId = skillId;
        LearnTitle.Text = def.MaxLevel > 1 ? $"{def.Name} (Lv.{target})" : def.Name;

        string body = SkillDetail(def);
        // A permanent, exclusive choice deserves a warning before you spend millions on it.
        if (!string.IsNullOrEmpty(def.ExclusiveGroup) && !_learnedSkills.Contains(skillId))
            body += "\n\nThis choice is PERMANENT: taking it locks out the other options in its group.";
        LearnBody.Text = body;

        bool enough = gold > 0 ? _gold >= gold : _skillPoints >= cost;
        LearnCost.Text = gold > 0
            ? $"Cost: {gold:N0} {GameConstants.CurrencyName}   (you have {_gold:N0})"
            : $"Cost: {cost} SP   (you have {_skillPoints})";
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
        // Report the LEVEL YOU ACTUALLY KNOW, not the SkillDef's level-1 defaults. Elemental Bolt now
        // runs to 13 levels (power 37 → 116), so showing the base row told you almost nothing.
        int lvl = Math.Max(1, _learnedLevels.GetValueOrDefault(def.Id));

        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(def.Description))
            lines.Add(def.Description);
        if (def.MaxLevel > 1)
            lines.Add($"Level {lvl} of {def.MaxLevel}");

        if (IsPassive(def))
        {
            lines.Add("Passive — always active once learned (no MP, not cast).");
            var ps = PassiveSummary(def);
            if (ps.Count > 0) lines.Add(string.Join(",  ", ps));
            return string.Join("\n", lines);
        }

        // POWER — the number that decides the damage/heal, and it was simply never shown.
        int power = def.PowerAt(lvl);
        if (power > 0)
            lines.Add($"Power: {power}");

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

        lines.Add($"Cooldown: {def.CooldownTicks * GameConstants.TickSeconds:0}s    MP: {def.MpCostAt(lvl)}");
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

        // No auto-placement here any more — the SERVER parks newly-learned skills and pushes the
        // resulting bar just before this message (see GameLoopService.SendLearned). Doing it here meant
        // the client wrote a bar it didn't author, which corrupted the real one. Just repaint.
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

    /// <summary>The buff bar is grouped by SUBTYPE, one row each: buffs a buffer gave you,
    /// debuffs on you, always-on item effects (sets / weapon abilities), and temporary things you
    /// consumed. A row hides itself when empty, so the "item" row simply doesn't appear until gear
    /// effects exist as buffs.</summary>
    private void OnBuffs(BuffUpdate update)
    {
        var rows = new Dictionary<BuffRow, ItemsControl>
        {
            [BuffRow.Buff] = BuffRowBuffs,
            [BuffRow.Debuff] = BuffRowDebuffs,
            [BuffRow.Item] = BuffRowItems,
            [BuffRow.Consumable] = BuffRowConsumables,
        };
        foreach (var ctrl in rows.Values) ctrl.Items.Clear();

        foreach (var buff in update.Buffs)
            (rows.GetValueOrDefault(buff.Row) ?? BuffRowBuffs).Items.Add(BuildBuffSquare(buff));

        foreach (var ctrl in rows.Values)
            ctrl.Visibility = ctrl.Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>One buff/debuff as a compact SQUARE (like a skill-bar slot) instead of a wide pill —
    /// pills wrapped into several rows and ate the screen. The icon/abbrev sits centre, the time reads
    /// small at the bottom, a stack count top-right; the whole square blinks when it's about to expire.</summary>
    private Border BuildBuffSquare(BuffDto buff)
    {
        bool toggle = buff.SecondsLeft < 0f;   // an indefinite toggle/stance or a gear-driven debuff
        var tint = buff.Row switch
        {
            BuffRow.Debuff     => Color.FromArgb(230, 120, 40, 40),    // red
            BuffRow.Consumable => Color.FromArgb(230, 40, 110, 70),    // green — from your bag
            BuffRow.Item       => Color.FromArgb(230, 95, 80, 35),     // bronze — from your gear
            _                  => Color.FromArgb(230, 40, 80, 120),    // blue — an ordinary buff
        };

        var grid = new Grid { Width = 38, Height = 38 };
        // Face: the emoji icon if the server sent one, else the buff name's initials.
        bool isIcon = !string.IsNullOrWhiteSpace(buff.Icon);
        grid.Children.Add(new TextBlock
        {
            Text = isIcon ? buff.Icon : BuffAbbrev(buff.Name),
            Foreground = Brushes.White, FontSize = isIcon ? 20 : 13,
            FontWeight = isIcon ? FontWeights.Normal : FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false
        });
        // Time (or the toggle glyph) along the bottom edge.
        grid.Children.Add(new TextBlock
        {
            Text = toggle ? "⟳" : ShortTime(buff.SecondsLeft),
            Foreground = Brushes.White, FontSize = 9,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 1), IsHitTestVisible = false
        });
        if (buff.Stacks > 1)
            grid.Children.Add(new TextBlock
            {
                Text = buff.Stacks.ToString(),
                Foreground = Brushes.Gold, FontSize = 10, FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 1, 2, 0), IsHitTestVisible = false
            });

        string time = toggle ? "active (toggle)" : $"{ShortTime(buff.SecondsLeft)} remaining";
        var square = new Border
        {
            Background = new SolidColorBrush(tint),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 0, 4, 4),
            Cursor = buff.IsDebuff ? null : System.Windows.Input.Cursors.Hand,
            Child = grid,
            ToolTip = buff.IsDebuff
                ? $"{buff.Name}\n{buff.Description}\n{time}"
                : $"{buff.Name}\n{buff.Description}\n{time}\n(double-click to remove)"
        };

        // About to expire (≤60s, not a toggle) → blink the square so it can't be missed. Applies to
        // debuffs too — they're already red, and a blink makes "you are debuffed" unmissable.
        if (!toggle && buff.SecondsLeft <= 60f)
        {
            var blink = new System.Windows.Media.Animation.DoubleAnimation(1.0, 0.4,
                new Duration(TimeSpan.FromSeconds(0.6)))
            {
                AutoReverse = true,
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
            };
            square.BeginAnimation(OpacityProperty, blink);
        }

        // Double-click a (beneficial) buff to drop it early, like a timeout.
        if (!buff.IsDebuff && !string.IsNullOrEmpty(buff.Key))
        {
            string key = buff.Key;
            square.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ClickCount == 2) { _ = _net.RemoveBuffAsync(key); e.Handled = true; }
            };
        }
        return square;
    }

    /// <summary>Label for a buff square when it has no emoji icon. Buffs aren't SkillDefs, so this works
    /// off the display name — but it goes through the same catalog-wide resolver as the skill bar, which
    /// is the whole point: the three heal-over-time buffs used to render an identical "HOT" square.</summary>
    private static string BuffAbbrev(string name) => Abbreviations.For(name);

    /// <summary>A duration as at most TWO digits + ONE unit, floored to the biggest unit that reaches 1
    /// (owner): ≥1d → Nd, ≥1h → Nh, ≥60s → Nm, else Ns. So 3600→1h, 3540→59m, 180→3m, 119→1m, 59→59s,
    /// 90000→1d. Keeps every square the same width no matter the timer.</summary>
    private static string ShortTime(float seconds)
    {
        int s = (int)seconds;
        if (s >= 86400) return $"{s / 86400}d";
        if (s >= 3600)  return $"{s / 3600}h";
        if (s >= 60)    return $"{s / 60}m";
        return $"{Math.Max(0, s)}s";
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

    /// <summary>A consumable's tooltip is just its SKILL's description (+ the shared drink
    /// cooldown, if it has one). The item no longer carries heal numbers of its own.</summary>
    private static string PotionTooltip(ItemDef def)
    {
        string text = SkillCatalog.Get(def.UseSkillId)?.Description ?? "";
        if (def.PotionCooldownTicks > 0)
            text += $" CD {def.PotionCooldownTicks / GameConstants.TickRate}s.";
        return text.Trim();
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

    private void DebugTabEquip_Click(object sender, RoutedEventArgs e)
    {
        _debugTab = 0;
        _debugEquipCat = "";   // always land on the category root, not wherever you drilled to last
        _debugEquipLevel = 0;
        BuildDebugMenu();
    }
    private void DebugTabConsum_Click(object sender, RoutedEventArgs e) { _debugTab = 1; BuildDebugMenu(); }
    private void DebugTabFunc_Click(object sender, RoutedEventArgs e) { _debugTab = 2; BuildDebugMenu(); }
    private void DebugTabClass_Click(object sender, RoutedEventArgs e) { _debugTab = 4; _debugAddDiscView = false; BuildDebugMenu(); }

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
            case 4: BuildDebugClass(); break;
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

    // ---- Equip tab: a LEVEL-driven drill-down over the real tiered gear ------------------
    // Root → category (Armor / Weapons / Jewels) → level (20/40/52/61/76) → the individual
    // pieces, one give-button each. The old menu hardcoded a handful of E-grade "rare" items
    // and the named sets, and never exposed the tiered gear at all — which is why the level
    // 20-40 sets appeared to be missing. Nothing is hardcoded now: the lists are read straight
    // out of ItemCatalog, so gear added to the CSV shows up here for free.
    private string _debugEquipCat = "";   // "" = root, else "armor" / "weapon" / "jewel"
    private int _debugEquipLevel;         // 0 = still choosing a level

    /// <summary>The gear LEVELS present in the catalog (20/40/52/61/76), discovered, not hardcoded.</summary>
    private static IEnumerable<int> GearLevels() => ItemCatalog.AllItems
        .Where(d => d.ItemLevel > 0 && d.Rarity == ItemRarity.Epic)
        .Select(d => d.ItemLevel).Distinct().OrderBy(l => l);

    /// <summary>The tier pieces of one category at one level. Epic only — that's the SET tier;
    /// the Common/Uncommon/Rare drop copies are excluded so the list stays short.</summary>
    private static IEnumerable<ItemDef> GearAt(string cat, int level) => ItemCatalog.AllItems
        .Where(d => d.ItemLevel == level && d.Rarity == ItemRarity.Epic)
        .Where(d => cat switch
        {
            "armor"  => d.Slot is EquipSlot.Armor or EquipSlot.Shield,
            "weapon" => d.Slot == EquipSlot.Weapon,
            "jewel"  => d.Slot == EquipSlot.Jewel,
            _ => false
        })
        .OrderBy(d => d.Slot).ThenBy(d => d.Name, StringComparer.Ordinal);

    private void BuildDebugEquip()
    {
        // ---- Root ----
        if (_debugEquipCat == "")
        {
            AddDebugHeader("Tiered gear (pick a level, then a piece)");
            DebugList.Children.Add(DebugAction("Armor & Shields ▸", () => { _debugEquipCat = "armor"; _debugEquipLevel = 0; BuildDebugMenu(); return Task.CompletedTask; }));
            DebugList.Children.Add(DebugAction("Weapons ▸", () => { _debugEquipCat = "weapon"; _debugEquipLevel = 0; BuildDebugMenu(); return Task.CompletedTask; }));
            DebugList.Children.Add(DebugAction("Jewels ▸", () => { _debugEquipCat = "jewel"; _debugEquipLevel = 0; BuildDebugMenu(); return Task.CompletedTask; }));

            AddDebugHeader("Boxes");
            DebugList.Children.Add(DebugGiveButton(ItemCatalog.BoxNewbie, "Newbie Box"));
            DebugList.Children.Add(DebugGiveButton(ItemCatalog.BoxTreasure, "Treasure Chest"));
            DebugList.Children.Add(DebugGiveButton(ItemCatalog.BoxNewbieArmorLight, "Newbie Light Armor Box"));
            DebugList.Children.Add(DebugGiveButton(ItemCatalog.BoxNewbieArmorRobe, "Newbie Robe Armor Box"));
            DebugList.Children.Add(DebugGiveButton(ItemCatalog.BoxNewbieJewels, "Newbie Jewels Box"));
            DebugList.Children.Add(DebugGiveButton(ItemCatalog.BoxNewbieWeapons, "Newbie Weapons Box (select)"));

            AddDebugHeader("Legendary");
            DebugList.Children.Add(DebugGiveButton(ItemCatalog.GodWeapon, "God's Judgment"));
            DebugList.Children.Add(DebugGiveButton(ItemCatalog.GodArmor, "God's Robes"));
            return;
        }

        string catLabel = _debugEquipCat switch { "armor" => "Armor & Shields", "weapon" => "Weapons", _ => "Jewels" };

        // ---- Level picker ----
        if (_debugEquipLevel == 0)
        {
            DebugList.Children.Add(DebugAction("◂ Back", () => { _debugEquipCat = ""; BuildDebugMenu(); return Task.CompletedTask; }));
            AddDebugHeader($"{catLabel} — pick a level");
            foreach (int lv in GearLevels())
            {
                int level = lv;
                string grade = ItemCatalog.TierLetter(level);
                DebugList.Children.Add(DebugAction($"Level {level}  ({grade}-Grade) ▸",
                    () => { _debugEquipLevel = level; BuildDebugMenu(); return Task.CompletedTask; }));
            }
            return;
        }

        // ---- The pieces at this level ----
        DebugList.Children.Add(DebugAction("◂ Back", () => { _debugEquipLevel = 0; BuildDebugMenu(); return Task.CompletedTask; }));
        AddDebugHeader($"{catLabel} — Level {_debugEquipLevel} ({ItemCatalog.TierLetter(_debugEquipLevel)}-Grade)");

        // Armor: a one-click "full set" per body weight (body + helm + gloves + boots), since a
        // set bonus needs all four. The individual pieces are still listed below it.
        if (_debugEquipCat == "armor")
        {
            int lvl = _debugEquipLevel;
            foreach (var (key, label) in new[] { ("heavy", "Heavy"), ("light", "Light"), ("robe", "Robe") })
            {
                string bodyId = $"{key}_t{lvl}";
                if (ItemCatalog.Get(bodyId) is null) continue;
                DebugList.Children.Add(DebugAction($"★ Full {label} Set (body + helm + gloves + boots)", async () =>
                {
                    foreach (var id in new[] { bodyId, $"helm_t{lvl}", $"gloves_t{lvl}", $"boots_t{lvl}" })
                        if (ItemCatalog.Get(id) is not null)
                            await _net.DebugGiveAsync(id);
                }));
            }
            AddDebugHeader("Individual pieces");
        }

        foreach (var def in GearAt(_debugEquipCat, _debugEquipLevel))
            DebugList.Children.Add(DebugGiveButton(def.Id, def.Name));
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
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.SkillStone, "Skill Stone +10", 10));

        // The ULTIMATE scrolls are deliberately not vendor-stocked, so debug is the only way to get
        // hold of them — and therefore the only way to test them.
        AddDebugHeader("Scrolls (x5)");
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.ScrollReturn, "Scroll of Return x5", 5));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.ScrollReturnUltimate, "ULT Scroll of Return x5", 5));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.ScrollResurrect, "Scroll of Resurrection x5", 5));
        DebugList.Children.Add(DebugGiveButton(ItemCatalog.ScrollResurrectUltimate, "ULT Scroll of Resurrection x5", 5));
    }

    /// <summary>Every class this character owns (server-pushed). Drives the subclass section below.</summary>
    private SubclassDto[] _subclasses = Array.Empty<SubclassDto>();

    // ---- CLASS UNIQUENESS (mirrors the server; see Entity) --------------------------------------
    //
    // You may not walk the same DISCIPLINE twice across the classes one character owns (archetypes are
    // NOT restricted — several Nukers are fine as long as their disciplines differ). This only greys the
    // barred options OUT; the server enforces it for real. Matched on the discipline, NOT the class id.

    private bool DisciplineTakenElsewhere(Discipline d) =>
        _subclasses.Any(s => !s.Active && s.ThirdClass > 0
                             && ThirdClassCatalog.Get(s.ThirdClass)?.Discipline == d);

    private void OnSubclasses(SubclassListDto dto)
    {
        _subclasses = dto.Classes;
        if (DebugPanel.Visibility == Visibility.Visible)
            BuildDebugMenu();   // refresh the swap buttons
    }

    /// <summary>The SUBCLASS section: one row per class you own, plus buttons to add another.
    ///
    /// This is the owner's test loop — swap class on the spot to compare two builds in the SAME gear,
    /// instead of relogging onto another character. Each class keeps its own level, XP, skills and
    /// skill BAR; the inventory, gold and auto-hunt settings are shared and survive the swap.</summary>
    // -------------------------------------------------------------------------------------------
    // FUNCTIONS tab: the frequently-used levers, grouped top-to-bottom as
    //   full buffer  →  gold + SP  →  level  →  karma.
    // (Class-management moved to its own "Class" tab.)
    // -------------------------------------------------------------------------------------------
    private void BuildDebugFunctions()
    {
        // Full buff set on yourself, at ANY level, without walking to the NPC. Note the NPC itself
        // still refuses above 75 (a game rule) — this is the only way to get buffed past that, which
        // matters because the balance numbers we sign off on are BUFFED numbers.
        AddDebugHeader("Full buffer");
        DebugList.Children.Add(DebugAction("Full Buffs (1h)", async () => await _net.DebugBuffAsync()));

        AddDebugHeader("Gold & SP");
        // 10kk, not 100k: the level-40 stat swaps cost 1kk-5kk per level (15kk to max one), so the
        // old button could not fund a single meaningful purchase to test with.
        DebugList.Children.Add(DebugAction("+10,000,000 Gold", async () => await _net.DebugGoldAsync(10_000_000)));
        DebugList.Children.Add(DebugAction("+1kk SP", async () => await _net.DebugSpAsync(1_000_000)));

        AddDebugHeader("Level");
        // One round-trip per click now (+10 used to fire ten separate commands, each with its own
        // level-up broadcast and character save). DELEVEL KEEPS YOUR LEARNED SKILLS — drop to 40,
        // feel it, climb back, without re-learning the whole kit.
        DebugList.Children.Add(DebugAction("Level +1",  async () => await _net.DebugLevelAsync(+1)));
        DebugList.Children.Add(DebugAction("Level +10", async () => await _net.DebugLevelAsync(+10)));
        DebugList.Children.Add(DebugAction("Level -1",  async () => await _net.DebugLevelAsync(-1)));
        DebugList.Children.Add(DebugAction("Level -10", async () => await _net.DebugLevelAsync(-10)));

        // Karma nudges. ±1000 to cross the PK line fast; ±20 = one mob kill's worth, to feel the grind.
        AddDebugHeader("Karma");
        DebugList.Children.Add(DebugAction("Karma +1000", async () => await _net.DebugKarmaAsync(+1000)));
        DebugList.Children.Add(DebugAction("Karma -1000", async () => await _net.DebugKarmaAsync(-1000)));
        DebugList.Children.Add(DebugAction("Karma +1 mob (+20)", async () => await _net.DebugKarmaAsync(+20)));
        DebugList.Children.Add(DebugAction("Karma -1 mob (-20)", async () => await _net.DebugKarmaAsync(-20)));
        // Clear all — the server clamps karma to [0, 1M] and clears the PK streak + red name when it
        // reaches 0, so a delta past the ceiling IS the clear. No new command needed.
        DebugList.Children.Add(DebugAction("Karma CLEAR (all)", async () => await _net.DebugKarmaAsync(-1_000_000)));
    }

    // -------------------------------------------------------------------------------------------
    // CLASS tab: everything class-management, grouped
    //   profession & skills  →  subclasses (swap + add a discipline)  →  reset.
    // -------------------------------------------------------------------------------------------
    private bool _debugAddDiscView;   // true = showing the "add a discipline" picker

    private void BuildDebugClass()
    {
        if (_debugAddDiscView) { BuildDebugAddDiscipline(); return; }

        AddDebugHeader("Profession & skills");
        DebugList.Children.Add(DebugAction("Change Profession (class change)", () =>
        {
            OpenClassChangePanel();
            return Task.CompletedTask;
        }));
        DebugList.Children.Add(DebugAction("Give all skills (to my level)", async () => await _net.DebugLearnAllAsync()));

        AddDebugHeader("Classes (subclass)");
        foreach (var sc in _subclasses)
        {
            string label = $"#{sc.Slot} {SubclassName(sc)} Lv{sc.Level}" + (sc.Active ? "  ← playing" : "");
            if (sc.Active)
            {
                DebugList.Children.Add(new TextBlock
                {
                    Text = label, Foreground = Brushes.LightGreen,
                    FontSize = 11, Margin = new Thickness(2, 0, 0, 4),
                });
            }
            else
            {
                int slot = sc.Slot;
                DebugList.Children.Add(DebugAction($"Switch to {label}",
                    async () => await _net.SwitchSubclassAsync(slot)));
            }
        }
        DebugList.Children.Add(DebugAction("＋ Add a class (discipline) ▸",
            () => { _debugAddDiscView = true; BuildDebugMenu(); return Task.CompletedTask; }));

        AddDebugHeader("Reset Character (re-roll, same char)");
        foreach (var race in SelectableRaces())
            foreach (var bc in Enum.GetValues<BaseClass>())
            {
                var r = race; var b = bc;
                DebugList.Children.Add(DebugAction($"Reset → {r} {b}",
                    async () => await _net.DebugResetAsync(r, b)));
            }
    }

    /// <summary>A friendly name for a subclass row: its 3rd-class discipline + race if it has one,
    /// else just the base class (the slot-0 class you were created as, pre-3rd-class).</summary>
    private static string SubclassName(SubclassDto sc)
    {
        if (sc.ThirdClass > 0 && ThirdClassCatalog.Get(sc.ThirdClass) is { } tcd)
            return $"{tcd.Race} {tcd.Discipline}";
        return $"{sc.Race} {sc.BaseClass}";
    }

    /// <summary>The "add a class" picker: every 3rd-class discipline you don't already own, across all
    /// races (a discipline is unique across the character — one Tempest bars every Tempest). Level 76+
    /// only; normal accounts cap at 4 classes, admins are unlimited (the discipline filter still holds).</summary>
    private void BuildDebugAddDiscipline()
    {
        DebugList.Children.Add(DebugAction("◂ Back", () =>
        {
            _debugAddDiscView = false; BuildDebugMenu(); return Task.CompletedTask;
        }));
        AddDebugHeader($"Add a class — every class must be lvl {ThirdClassCatalog.SubclassLevel}+ with its 3rd class (admins exempt)");

        var owned = _subclasses
            .Where(s => s.ThirdClass > 0 && ThirdClassCatalog.Get(s.ThirdClass) is not null)
            .Select(s => ThirdClassCatalog.Get(s.ThirdClass)!.Discipline)
            .ToHashSet();

        // Belt-and-suspenders: _subclasses can go stale after a debug profession change (that path
        // re-sends Stats but not the class list), so the ACTIVE class may still look like a base class
        // with ThirdClass=0. _myThirdClass is always current — fold its discipline in so the main is
        // never offered back to itself.
        if (_myThirdClass > 0 && ThirdClassCatalog.Get(_myThirdClass) is { } activeTcd)
            owned.Add(activeTcd.Discipline);

        foreach (var tcd in ThirdClassCatalog.Playable
                     .Where(t => !owned.Contains(t.Discipline))
                     .OrderBy(t => t.Discipline).ThenBy(t => t.Race))
        {
            int id = tcd.Id;
            DebugList.Children.Add(DebugAction($"{tcd.Race} {tcd.Discipline}",
                async () => await _net.DebugAddSubclassAsync(id)));
        }
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

    /// <summary>The three buffer options. "cost 0" shows as "free". The server re-checks gold + range
    /// and re-sends the dialog after each action (so the restore cost drops to 0, gold updates, etc.).</summary>
    private void BuildBufferSection(BufferInfo buffer)
    {
        AddDialogHeader("Blessings");
        if (!buffer.CanBuff)
        {
            AddDialogText(buffer.Message);
            return;
        }
        if (!string.IsNullOrEmpty(buffer.Message))
            AddDialogText(buffer.Message);

        string Price(long c) => c <= 0 ? "free" : $"{c:N0} {GameConstants.CurrencyName}";

        Button Row(string text, bool enabled, Func<System.Threading.Tasks.Task> act)
        {
            var b = new Button
            {
                Content = text, Height = 28, HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 0, 4), Padding = new Thickness(8, 0, 8, 0), IsEnabled = enabled,
            };
            b.Click += async (_, _) => await act();
            DialogContent.Children.Add(b);
            return b;
        }

        Row($"Full buff set  —  {Price(buffer.FullBuffCost)}", _gold >= buffer.FullBuffCost,
            () => _net.BufferActionAsync(_dialogNpcId, "full", ""));

        Row($"Restore HP / MP to full  —  {Price(buffer.RestoreCost)}", _gold >= buffer.RestoreCost,
            () => _net.BufferActionAsync(_dialogNpcId, "restore", ""));

        AddDialogText("— or a single buff —");
        foreach (var buff in buffer.Buffs)
        {
            string id = buff.SkillId;
            Row($"{buff.Name}  —  {Price(buff.Cost)}", _gold >= buff.Cost,
                () => _net.BufferActionAsync(_dialogNpcId, "single", id));
        }
    }

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
            "SkillReset" => "Mindwright",
            "Buffer" => "Spirit Helper",
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

        // Skill reset: un-learn a PERMANENT, mutually-exclusive pick (a level-40 stat swap) so its
        // group is open again. Free to forget — the gold is NOT refunded, which is the whole point:
        // you may change your mind, you may not undo the price of being wrong.
        if (dialog.SkillReset is SkillResetInfo reset)
        {
            AddDialogHeader("Forget a permanent choice");
            if (reset.Skills.Length == 0)
            {
                AddDialogText("You have not committed to any permanent skill yet.");
            }
            else
            {
                AddDialogText("Forgetting frees its group so you may commit again. "
                            + "The gold you spent is NOT refunded.");
                foreach (var s in reset.Skills)
                {
                    var btn = new Button
                    {
                        Content = $"Forget {s.Name} (Lv.{s.Level})  —  {s.GoldSpent:N0} "
                                + $"{GameConstants.CurrencyName} written off",
                        Height = 28, HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(0, 2, 0, 4), Padding = new Thickness(8, 0, 8, 0)
                    };
                    string skillId = s.SkillId;
                    btn.Click += async (_, _) => await _net.ForgetSkillAsync(_dialogNpcId, skillId);
                    DialogContent.Children.Add(btn);
                }
            }
        }

        // Buffer: full-buff / single buff / restore. Free ≤40, priced above (server-authoritative).
        if (dialog.Buffer is BufferInfo buffer)
            BuildBufferSection(buffer);

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

    private void ShopClose_Click(object sender, RoutedEventArgs e)
    {
        ShopPanel.Visibility = Visibility.Collapsed;
        CloseBuyQty();   // never leave the quantity prompt floating over the world with no shop behind it
    }

    // ----- Buy-quantity prompt (consumables only) -------------------------------------------------

    private string? _buyQtyDefId;
    private int _buyQtyPrice;

    /// <summary>Ask how many to buy. Deliberately does NOT close on a purchase — only Cancel closes it —
    /// so stocking up is repeated clicks on one button rather than re-opening the prompt each time.</summary>
    private void OpenBuyQty(string defId, string name, int price)
    {
        _buyQtyDefId = defId;
        _buyQtyPrice = price;
        BuyQtyText.Text = $"Buy {name}\n{price:N0} {GameConstants.CurrencyName} each. How many?";
        RefreshBuyQty();
        BuyQtyPanel.Visibility = Visibility.Visible;
        Panel.SetZIndex(BuyQtyPanel, ++_panelZ);
    }

    private void CloseBuyQty()
    {
        BuyQtyPanel.Visibility = Visibility.Collapsed;
        _buyQtyDefId = null;
    }

    /// <summary>Grey out the amounts you can't afford. Re-run on every gold change, since the prompt
    /// stays open across purchases and your gold drops as you buy.</summary>
    private void RefreshBuyQty()
    {
        BuyQty1.IsEnabled = _gold >= (long)_buyQtyPrice;
        BuyQty10.IsEnabled = _gold >= (long)_buyQtyPrice * 10;
        BuyQty100.IsEnabled = _gold >= (long)_buyQtyPrice * 100;
        BuyQty1000.IsEnabled = _gold >= (long)_buyQtyPrice * 1000;
    }

    private async void BuyQty_Click(object sender, RoutedEventArgs e)
    {
        if (_buyQtyDefId is not string defId) return;
        if (sender is not Button b || b.Tag is not string tag || !int.TryParse(tag, out int qty)) return;
        await _net.BuyItemAsync(_dialogNpcId, defId, qty);   // server re-checks gold and clamps
    }

    private void BuyQtyCancel_Click(object sender, RoutedEventArgs e) => CloseBuyQty();

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
                string name = entry.Name;
                // Consumables are bought in BULK (potions, scrolls, reagents — Angel's Protection alone
                // eats 5 Skill Stones a cast), so Buy opens a quantity prompt instead of trickling one
                // per click. Gear stays one-click: you buy exactly one sword.
                bool bulk = ItemCatalog.Get(defId)?.Slot == EquipSlot.Consumable;
                ShopList.Children.Add(ShopRow(name, $"{price:N0} {GameConstants.CurrencyName}",
                    "Buy", _gold >= price,
                    bulk
                        ? () => { OpenBuyQty(defId, name, price); return Task.CompletedTask; }
                        : async () => await _net.BuyItemAsync(_dialogNpcId, defId, 1)));
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
