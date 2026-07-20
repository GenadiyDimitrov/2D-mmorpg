using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Game.Shared;

namespace Game.Client.Wpf;

/// <summary>
/// The staff-only client surface: the on-screen role badge, and the two windows behind
/// <c>/bag &lt;name&gt;</c> and <c>/give &lt;name&gt;</c>.
///
/// None of this grants any power. Every button here sends a command the server re-authorizes against
/// the CHARACTER's role — the client just decides what is worth drawing.
/// </summary>
public partial class MainWindow
{
    /// <summary>The player /give is currently aiming at (the picker lists MY items, not theirs).</summary>
    private string _adminGiveTarget = "";

    /// <summary>The player whose bag /bag is showing.</summary>
    private string _adminBagTarget = "";

    private AdminStateDto? _adminState;

    private void OnAdminState(AdminStateDto state)
    {
        _adminState = state;
        _role = state.Role;
        UpdateAdminIndicator();
    }

    /// <summary>Draw the staff badge: role, plus anything currently ON that would otherwise be
    /// invisible (god mode, forced speeds). The owner could only tell whether god mode was on by
    /// typing /god again and reading which way it toggled.</summary>
    private void UpdateAdminIndicator()
    {
        if (AdminBadge is null) return;   // called before the XAML is loaded

        if (!_inGame || _role == AccountRole.Player)
        {
            AdminBadge.Visibility = Visibility.Collapsed;
            return;
        }

        var parts = new System.Collections.Generic.List<string> { _role.ToString().ToUpperInvariant() };
        if (_adminState is AdminStateDto s)
        {
            if (s.GodMode) parts.Add("GOD");
            if (s.CastSpeed is float c) parts.Add($"cast {c:0.##}");
            if (s.AttackSpeed is float a) parts.Add($"atk {a:0.##}");
            if (s.MoveSpeed is float m) parts.Add($"move {m:0.##}");
        }

        AdminBadgeText.Text = string.Join("  ·  ", parts);
        // God mode is the one worth shouting about — it silently changes every fight you're in.
        bool god = _adminState?.GodMode == true;
        AdminBadge.Background = new SolidColorBrush(god
            ? Color.FromArgb(0xC0, 0x6A, 0x18, 0x18)
            : Color.FromArgb(0xA0, 0x40, 0x20, 0x00));
        AdminBadge.Visibility = Visibility.Visible;
    }

    /// <summary>/bag &lt;name&gt; — another player's inventory, with a Remove button per row.</summary>
    private void ShowAdminBagWindow(AdminBagDto bag)
    {
        _adminBagTarget = bag.OwnerName;
        _adminGiveTarget = "";
        AdminBagTitle.Text = $"{bag.OwnerName}'s bag";
        AdminBagSubtitle.Text = $"{bag.Gold:#,##0} gold · {bag.Items.Length} items · click Remove to destroy an item";
        BuildAdminBagRows(bag, giveMode: false);
        Panel.SetZIndex(AdminBagPanel, ++_panelZ);   // above whatever else is open
        AdminBagPanel.Visibility = Visibility.Visible;
    }

    /// <summary>/give &lt;name&gt; — MY inventory, with a Give button per row.</summary>
    private void ShowAdminGiveWindow(AdminBagDto picker)
    {
        _adminGiveTarget = picker.OwnerName;
        _adminBagTarget = "";
        AdminBagTitle.Text = $"Give to {picker.OwnerName}";
        AdminBagSubtitle.Text = "Your inventory. Tradability is ignored — anything here can be handed over.";
        BuildAdminBagRows(picker, giveMode: true);
        Panel.SetZIndex(AdminBagPanel, ++_panelZ);   // above whatever else is open
        AdminBagPanel.Visibility = Visibility.Visible;
    }

    private void BuildAdminBagRows(AdminBagDto bag, bool giveMode)
    {
        AdminBagList.Children.Clear();
        if (bag.Items.Length == 0)
        {
            AdminBagList.Children.Add(new TextBlock
            {
                Text = "(empty)",
                Foreground = new SolidColorBrush(Color.FromRgb(0x8F, 0x9A, 0xA4)),
                FontSize = 12,
                Margin = new Thickness(2, 6, 0, 0),
            });
            return;
        }

        foreach (var item in bag.Items)
        {
            var def = ItemCatalog.Get(item.DefId);
            string label = def?.Name ?? item.DefId;
            if (item.Enchant > 0) label = $"+{item.Enchant} {label}";
            if (item.Quantity > 1) label += $"  x{item.Quantity}";
            if (item.Equipped) label += "  (equipped)";

            var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var text = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(Color.FromRgb(0xD8, 0xE0, 0xE6)),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            Grid.SetColumn(text, 0);
            row.Children.Add(text);

            var button = new Button
            {
                Content = giveMode ? "Give" : "Remove",
                Width = 74,
                Height = 22,
                FontSize = 11,
                Margin = new Thickness(6, 0, 0, 0),
            };
            var instanceId = item.InstanceId;
            int quantity = item.Quantity;
            button.Click += async (_, _) =>
            {
                if (giveMode)
                {
                    if (_adminGiveTarget.Length == 0) return;
                    await _net.AdminGiveItemAsync(_adminGiveTarget, instanceId, quantity);
                    AdminBagPanel.Visibility = Visibility.Collapsed;   // one gift per /give
                }
                else
                {
                    if (_adminBagTarget.Length == 0) return;
                    await _net.AdminRemoveItemAsync(_adminBagTarget, instanceId);
                    // The server answers with a refreshed AdminBag, so no local mutation here.
                }
            };
            Grid.SetColumn(button, 1);
            row.Children.Add(button);

            AdminBagList.Children.Add(row);
        }
    }
}
