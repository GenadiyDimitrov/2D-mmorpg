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

    private CheckBox? _autoBasicCheck;          // "Basic Attack" opt-in (a pseudo auto-skill)
    private readonly List<string> _autoOrder = new();   // real-skill display order = priority

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

        // Priority order: configured real skills first (preserved), then any other learned actives.
        var cfg = _autoConfig?.Skills ?? Array.Empty<AutoSkillDto>();
        _autoOrder.Clear();
        foreach (var s in cfg)
            if (s.SkillId != AutoHuntIds.BasicAttack && !_autoOrder.Contains(s.SkillId) &&
                _learnedSkills.Contains(s.SkillId) && IsAutoUsable(s.SkillId))
                _autoOrder.Add(s.SkillId);
        foreach (var id in _learnedSkills)
            if (!_autoOrder.Contains(id) && IsAutoUsable(id))
                _autoOrder.Add(id);

        var (state, basic) = StateFromConfig();
        RenderAutoSkills(state, basic);
    }

    private static bool IsAutoUsable(string id) =>
        SkillCatalog.Get(id) is SkillDef d && AutoTypeLabel(d) is not null;

    /// <summary>Snapshot the current row edits (enabled + reuse) so a reorder doesn't lose them.</summary>
    private (Dictionary<string, (bool Enabled, int Extra)> State, bool Basic) CaptureAuto()
    {
        var d = new Dictionary<string, (bool, int)>();
        foreach (var (id, en, reuse) in _autoRows)
        {
            int baseCd = SkillCatalog.Get(id)?.CooldownTicks ?? 0;
            d[id] = (en.IsChecked == true, Math.Max(0, SecondsToTicks(reuse.Text) - baseCd));
        }
        return (d, _autoBasicCheck?.IsChecked == true);
    }

    private (Dictionary<string, (bool Enabled, int Extra)> State, bool Basic) StateFromConfig()
    {
        var d = new Dictionary<string, (bool, int)>();
        bool basic = false;
        foreach (var s in _autoConfig?.Skills ?? Array.Empty<AutoSkillDto>())
        {
            if (s.SkillId == AutoHuntIds.BasicAttack) { basic = s.Enabled; continue; }
            d[s.SkillId] = (s.Enabled, s.ExtraDelayTicks);
        }
        return (d, basic);
    }

    private void RenderAutoSkills(Dictionary<string, (bool Enabled, int Extra)> state, bool basic)
    {
        _autoRows.Clear();
        AutoSkillsList.Items.Clear();

        _autoBasicCheck = new CheckBox
        {
            Content = "Basic Attack (melee when no skill is ready)",
            Foreground = Brushes.White, FontSize = 12, Margin = new Thickness(0, 2, 0, 4),
            IsChecked = basic
        };
        AutoSkillsList.Items.Add(_autoBasicCheck);

        foreach (var id in _autoOrder)
        {
            if (SkillCatalog.Get(id) is not SkillDef def) continue;
            if (AutoTypeLabel(def) is not string type) continue;
            state.TryGetValue(id, out var st);
            AutoSkillsList.Items.Add(BuildAutoRow(def, type, st.Enabled, st.Extra));
        }
    }

    /// <summary>Move a skill up (-1) / down (+1) in the priority order, preserving row edits.</summary>
    private void AutoMove(string id, int dir)
    {
        int i = _autoOrder.IndexOf(id), j = i + dir;
        if (i < 0 || j < 0 || j >= _autoOrder.Count) return;
        var (state, basic) = CaptureAuto();
        (_autoOrder[i], _autoOrder[j]) = (_autoOrder[j], _autoOrder[i]);
        RenderAutoSkills(state, basic);
    }

    private FrameworkElement BuildAutoRow(SkillDef def, string type, bool enabledInit, int extraTicks)
    {
        var enabled = new CheckBox
        {
            IsChecked = enabledInit,
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0)
        };
        var name = new TextBlock
        {
            Text = SkillDisplayName(def.Id, def.Name), Foreground = Brushes.White,
            FontSize = 12, Width = 150, VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var tag = new TextBlock
        {
            Text = type, Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0xA8, 0x88)),
            FontSize = 11, Width = 50, VerticalAlignment = VerticalAlignment.Center
        };
        // Reuse (seconds) = base cooldown + the user's extra delay; can only be raised (min = default).
        float baseSec = def.CooldownTicks / (float)GameConstants.TickRate;
        float shownSec = (def.CooldownTicks + extraTicks) / (float)GameConstants.TickRate;
        var reuse = new TextBox
        {
            Text = shownSec.ToString("0.#", CultureInfo.InvariantCulture),
            Width = 40, Height = 22, TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 2, 0),
            ToolTip = $"Reuse in seconds (min {baseSec:0.#}s = the skill's own cooldown)."
        };
        var up = new Button
        {
            Content = "▲", Width = 20, Height = 20, Padding = new Thickness(0), FontSize = 9,
            Margin = new Thickness(6, 0, 0, 0), Tag = def.Id, ToolTip = "Higher priority"
        };
        up.Click += (_, _) => AutoMove((string)up.Tag, -1);
        var down = new Button
        {
            Content = "▼", Width = 20, Height = 20, Padding = new Thickness(0), FontSize = 9,
            Margin = new Thickness(2, 0, 0, 0), Tag = def.Id, ToolTip = "Lower priority"
        };
        down.Click += (_, _) => AutoMove((string)down.Tag, +1);

        var mut = new SolidColorBrush(Color.FromRgb(0xCF, 0xC8, 0xB0));
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
        row.Children.Add(enabled);
        row.Children.Add(name);
        row.Children.Add(tag);
        row.Children.Add(new TextBlock { Text = "reuse", Foreground = mut, FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
        row.Children.Add(reuse);
        row.Children.Add(new TextBlock { Text = "s", Foreground = mut, FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
        row.Children.Add(up);
        row.Children.Add(down);

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
