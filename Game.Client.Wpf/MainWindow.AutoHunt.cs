using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Game.Shared;

namespace Game.Client.Wpf;

// Auto-hunt / idle-farming config window (docs/AutoHunt.md). Partial of MainWindow — shares its
// private fields. The server is authoritative; this is only a config editor + MP/s HUD.
public partial class MainWindow
{
    private AutoHuntConfigDto? _autoConfig;
    // Live row controls so Apply can read them back (skill id, enable box, reuse-seconds box).
    private readonly List<(string SkillId, CheckBox Enabled, TextBox Reuse)> _autoRows = new();

    private void AutoHuntButton_Click(object sender, RoutedEventArgs e)
    {
        if (AutoHuntPanel.Visibility == Visibility.Visible)
        {
            AutoHuntPanel.Visibility = Visibility.Collapsed;
            return;
        }
        BuildAutoHuntWindow();
        AutoHuntPanel.Visibility = Visibility.Visible;
    }

    private void AutoHuntClose_Click(object sender, RoutedEventArgs e) =>
        AutoHuntPanel.Visibility = Visibility.Collapsed;

    /// <summary>The server echoes the stored config on login + on change — cache it and, if the
    /// window is open, rebuild so it reflects the authoritative settings.</summary>
    private void OnAutoConfig(AutoHuntConfigDto cfg)
    {
        _autoConfig = cfg;
        AutoHuntEnabledCheck.IsChecked = cfg.Enabled;
        // Don't rebuild an open window here — it would discard unsaved edits. The window is
        // (re)built from _autoConfig when it's next opened.
    }

    /// <summary>Live HUD: total MP/s + per-skill reuse. Also keeps the Enabled box in sync.</summary>
    private void OnAutoHuntStatus(AutoHuntStatus status)
    {
        AutoHuntEnabledCheck.IsChecked = status.Enabled;
        string detail = status.Skills.Length == 0
            ? ""
            : "  (" + string.Join(", ", status.Skills.Select(s =>
                $"{s.Name} {s.ReuseSeconds:0.#}s")) + ")";
        AutoMpsText.Text = $"Mana: {status.MpPerSec:0.0} /s{detail}";
    }

    private async void AutoHuntEnabled_Click(object sender, RoutedEventArgs e) =>
        await _net.ToggleAutoHuntAsync(AutoHuntEnabledCheck.IsChecked == true);

    private CheckBox? _autoBasicCheck;   // "Basic Attack" opt-in (a pseudo auto-skill)

    private void BuildAutoHuntWindow()
    {
        AutoHpPctBox.Text = (_autoConfig?.HpPotionPct ?? 0).ToString(CultureInfo.InvariantCulture);
        AutoMpPctBox.Text = (_autoConfig?.MpPotionPct ?? 0).ToString(CultureInfo.InvariantCulture);
        AutoBuffPotionsCheck.IsChecked = _autoConfig?.AutoBuffPotions ?? false;
        AutoHuntEnabledCheck.IsChecked = _autoConfig?.Enabled ?? false;

        AutoFarmRangeBox.Text = (_autoConfig?.FarmRange ?? 1000).ToString(CultureInfo.InvariantCulture);
        AutoStaticCheck.IsChecked = _autoConfig?.StaticSpot ?? false;
        AutoRankNormal.IsChecked = _autoConfig?.AttackNormal ?? true;
        AutoRankElite.IsChecked = _autoConfig?.AttackElite ?? false;
        AutoRankBoss.IsChecked = _autoConfig?.AttackBoss ?? false;

        _autoRows.Clear();
        AutoSkillsList.Items.Clear();

        var cfg = _autoConfig?.Skills ?? Array.Empty<AutoSkillDto>();
        var cfgById = cfg.ToDictionary(s => s.SkillId, s => s);

        // "Basic Attack" pseudo-row first: melee when no skill is ready (fighters on, mages off).
        _autoBasicCheck = new CheckBox
        {
            Content = "Basic Attack (melee when no skill is ready)",
            Foreground = Brushes.White, FontSize = 12, Margin = new Thickness(0, 2, 0, 4),
            IsChecked = cfgById.TryGetValue(AutoHuntIds.BasicAttack, out var ba) && ba.Enabled
        };
        AutoSkillsList.Items.Add(_autoBasicCheck);

        // Then real skills: configured order first (priority preserved), then any other learned.
        var ordered = new List<string>();
        foreach (var s in cfg)
            if (_learnedSkills.Contains(s.SkillId) && !ordered.Contains(s.SkillId))
                ordered.Add(s.SkillId);
        foreach (var id in _learnedSkills)
            if (!ordered.Contains(id))
                ordered.Add(id);

        foreach (var id in ordered)
        {
            if (SkillCatalog.Get(id) is not SkillDef def) continue;
            if (AutoTypeLabel(def) is not string type) continue;   // passive / not auto-usable

            cfgById.TryGetValue(id, out var entry);
            AutoSkillsList.Items.Add(BuildAutoRow(def, type, entry));
        }
    }

    private FrameworkElement BuildAutoRow(SkillDef def, string type, AutoSkillDto? entry)
    {
        var enabled = new CheckBox
        {
            IsChecked = entry?.Enabled ?? false,
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0)
        };
        var name = new TextBlock
        {
            Text = SkillDisplayName(def.Id, def.Name), Foreground = Brushes.White,
            FontSize = 12, Width = 180, VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var tag = new TextBlock
        {
            Text = type, Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0xA8, 0x88)),
            FontSize = 11, Width = 54, VerticalAlignment = VerticalAlignment.Center
        };
        // Reuse (seconds) = base cooldown + the user's extra delay; can only be raised (min = default).
        float baseSec = def.CooldownTicks / (float)GameConstants.TickRate;
        float shownSec = (def.CooldownTicks + (entry?.ExtraDelayTicks ?? 0)) / (float)GameConstants.TickRate;
        var reuse = new TextBox
        {
            Text = shownSec.ToString("0.#", CultureInfo.InvariantCulture),
            Width = 44, Height = 22, TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 2, 0),
            ToolTip = $"Reuse in seconds (min {baseSec:0.#}s = the skill's own cooldown)."
        };

        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
        row.Children.Add(enabled);
        row.Children.Add(name);
        row.Children.Add(tag);
        row.Children.Add(new TextBlock
        {
            Text = "reuse", Foreground = new SolidColorBrush(Color.FromRgb(0xCF, 0xC8, 0xB0)),
            FontSize = 11, VerticalAlignment = VerticalAlignment.Center
        });
        row.Children.Add(reuse);
        row.Children.Add(new TextBlock
        {
            Text = "s", Foreground = new SolidColorBrush(Color.FromRgb(0xCF, 0xC8, 0xB0)),
            FontSize = 11, VerticalAlignment = VerticalAlignment.Center
        });

        _autoRows.Add((def.Id, enabled, reuse));
        return row;
    }

    private async void AutoHuntApply_Click(object sender, RoutedEventArgs e)
    {
        var skills = new List<AutoSkillDto>();
        // Basic Attack pseudo-skill (opt-in melee).
        if (_autoBasicCheck is not null)
            skills.Add(new AutoSkillDto(AutoHuntIds.BasicAttack, _autoBasicCheck.IsChecked == true, 0));
        foreach (var (skillId, enabledBox, reuseBox) in _autoRows)
        {
            // Convert the desired total reuse (s) back to an EXTRA delay over the skill's default.
            int baseCd = SkillCatalog.Get(skillId)?.CooldownTicks ?? 0;
            int totalTicks = SecondsToTicks(reuseBox.Text);
            int extra = Math.Max(0, totalTicks - baseCd);
            skills.Add(new AutoSkillDto(skillId, enabledBox.IsChecked == true, extra));
        }

        var cfg = new AutoHuntConfigDto(
            AutoHuntEnabledCheck.IsChecked == true,
            ParsePct(AutoHpPctBox.Text),
            ParsePct(AutoMpPctBox.Text),
            AutoBuffPotionsCheck.IsChecked == true,
            skills.ToArray(),
            Array.Empty<string>(),   // empty = keep ALL buff potions up (server convenience)
            FarmRange: ParseRange(AutoFarmRangeBox.Text),
            StaticSpot: AutoStaticCheck.IsChecked == true,
            AttackNormal: AutoRankNormal.IsChecked == true,
            AttackElite: AutoRankElite.IsChecked == true,
            AttackBoss: AutoRankBoss.IsChecked == true);

        _autoConfig = cfg;
        await _net.SetAutoHuntConfigAsync(cfg);
    }

    /// <summary>attack/buff/debuff/heal label for the row, or null for passive/not-auto-usable.</summary>
    private static string? AutoTypeLabel(SkillDef def)
    {
        if (def.Category == SkillCategory.Passive) return null;
        var e = def.Effect;
        if ((e & (SkillEffect.PhysicalDamage | SkillEffect.MagicDamage)) != 0) return "attack";
        if ((e & SkillEffect.Heal) != 0) return "heal";
        if (def.Category == SkillCategory.Buff || (e & SkillEffect.AnyBuff) != 0) return "buff";
        if ((e & SkillEffect.ContestCc) != 0 || def.DebuffSchool != DebuffSchool.None) return "debuff";
        return null;
    }

    private static int ParsePct(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
            ? Math.Clamp(v, 0, 100) : 0;

    private static int ParseRange(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
            ? Math.Clamp(v, 200, 2000) : 1000;

    private static int SecondsToTicks(string text) =>
        float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float s)
            ? Math.Max(0, (int)Math.Round(s * GameConstants.TickRate)) : 0;
}
