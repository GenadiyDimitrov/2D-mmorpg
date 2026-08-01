using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Game.Shared;

namespace Game.Client.Wpf;

// Debug live-tuning panel (admin only). Edits runtime server knobs (rates / karma / caps) so they
// can be dialed in during a playtest; the final values get baked back into the code defaults.
public partial class MainWindow
{
    private readonly Dictionary<string, TextBox> _debugFields = new();

    // key, label, isFloat — order matches the DebugConfigDto round-trip.
    private static readonly (string Key, string Label, bool Float)[] DebugSpec =
    {
        ("exp", "Exp rate ×", true),
        ("sp", "SP rate ×", true),
        ("dropChance", "Drop chance ×", true),
        ("dropAmount", "Drop amount ×", true),
        ("gold", "Gold rate ×", true),
        ("karmaBase", "Karma base", false),
        ("karmaConsec", "Karma ×/consec kill", true),
        ("karmaLevel", "Karma ×/level diff", true),
        ("karmaDeath", "Karma −/death", false),
        ("karmaMob", "Karma −/mob kill", false),
        ("idleCap", "Idle cap sec (0=∞)", false),
        ("offlineCap", "Offline cap sec (0=∞)", false),
        ("grace", "Disconnect grace (sec)", false),
        ("testHealPower", "Test heal power", false),
        ("testSkillPower", "Test skill Flat", false),
        ("testSkillMod", "Test skill Mod ×", true),
        ("regenInterval", "Regen tick (sec)", true),
        ("conRegen", "CON regen ×/point", true),
        ("mobRegen", "Mob regen in combat (frac/s)", true),
        ("mobRegenIdle", "Mob regen idle (frac/s)", true),
    };

    private void TuneOpen_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = Visibility.Collapsed;
        EnsureDebugRows();
        TunePanel.Visibility = Visibility.Visible;
        _ = _net.RequestDebugConfigAsync();   // populate from the authoritative current values
    }

    private void TuneRefresh_Click(object sender, RoutedEventArgs e) => _ = _net.RequestDebugConfigAsync();
    private void TuneClose_Click(object sender, RoutedEventArgs e) => TunePanel.Visibility = Visibility.Collapsed;

    private void EnsureDebugRows()
    {
        if (_debugFields.Count > 0) return;
        var mut = new SolidColorBrush(Color.FromRgb(0xCF, 0xC8, 0xB0));
        foreach (var (key, label, _) in DebugSpec)
        {
            var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            var lbl = new TextBlock
            {
                Text = label, Foreground = mut, FontSize = 12, VerticalAlignment = VerticalAlignment.Center
            };
            var box = new TextBox
            {
                Width = 80, Height = 22, HorizontalAlignment = HorizontalAlignment.Right,
                TextAlignment = TextAlignment.Center
            };
            row.Children.Add(lbl);
            row.Children.Add(box);
            TuneFields.Children.Add(row);
            _debugFields[key] = box;
        }
    }

    private void OnDebugConfig(DebugConfigDto d)
    {
        EnsureDebugRows();
        _debugFields["exp"].Text = Str(d.ExpRate);
        _debugFields["sp"].Text = Str(d.SpRate);
        _debugFields["dropChance"].Text = Str(d.DropChanceRate);
        _debugFields["dropAmount"].Text = Str(d.DropAmountRate);
        _debugFields["gold"].Text = Str(d.GoldRate);
        _debugFields["karmaBase"].Text = d.KarmaBase.ToString(CultureInfo.InvariantCulture);
        _debugFields["karmaConsec"].Text = Str(d.KarmaConsecGrowth);
        _debugFields["karmaLevel"].Text = Str(d.KarmaLevelGrowth);
        _debugFields["karmaDeath"].Text = d.KarmaLossPerDeath.ToString(CultureInfo.InvariantCulture);
        _debugFields["karmaMob"].Text = d.KarmaLossPerMob.ToString(CultureInfo.InvariantCulture);
        _debugFields["idleCap"].Text = d.IdleCapSeconds.ToString(CultureInfo.InvariantCulture);
        _debugFields["offlineCap"].Text = d.OfflineCapSeconds.ToString(CultureInfo.InvariantCulture);
        _debugFields["grace"].Text = d.GraceSeconds.ToString(CultureInfo.InvariantCulture);
        _debugFields["testHealPower"].Text = d.TestHealPower.ToString(CultureInfo.InvariantCulture);
        _debugFields["testSkillPower"].Text = d.TestSkillPower.ToString(CultureInfo.InvariantCulture);
        _debugFields["testSkillMod"].Text = Str(d.TestSkillMod);
        _debugFields["regenInterval"].Text = Str(d.RegenIntervalSeconds);
        _debugFields["conRegen"].Text = Str(d.ConRegenBase);
        _debugFields["mobRegen"].Text = d.MobHpRegenPctCombat.ToString("0.####", CultureInfo.InvariantCulture);
        _debugFields["mobRegenIdle"].Text = d.MobRegenPctIdle.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private async void TuneApply_Click(object sender, RoutedEventArgs e)
    {
        var d = new DebugConfigDto(
            F("exp"), F("sp"), F("dropChance"), F("dropAmount"), F("gold"),
            I("karmaBase"), F("karmaConsec"), F("karmaLevel"), I("karmaDeath"), I("karmaMob"),
            I("idleCap"), I("offlineCap"), I("grace"),
            I("testHealPower"), I("testSkillPower"), F("testSkillMod"),
            F("regenInterval"), F("conRegen"), F("mobRegen"), F("mobRegenIdle"));
        await _net.SetDebugConfigAsync(d);
    }

    private static string Str(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private float F(string key) =>
        float.TryParse(_debugFields[key].Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0f;

    private int I(string key) =>
        int.TryParse(_debugFields[key].Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
}
