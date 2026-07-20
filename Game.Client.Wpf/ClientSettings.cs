using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Game.Shared;

namespace Game.Client.Wpf;

/// <summary>A 2D point/size for the settings file.</summary>
public class Vec2
{
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>Main-window geometry.</summary>
public class WindowGeom
{
    public Vec2 Position { get; set; } = new() { X = 100, Y = 100 };
    public Vec2 Size { get; set; } = new() { X = 1280, Y = 800 };
}

/// <summary>Persistent CLIENT preferences — window geometry and popup positions, nothing else. Lives
/// NEXT TO THE EXE (the Debug/publish output), like an options.ini. It is NOT a build item, so an
/// update never overwrites it; if it's missing the app writes a default. Best-effort — a bad file just
/// yields defaults.
///
/// This file is for things that belong to THIS MACHINE. Anything that belongs to the CHARACTER lives in
/// the DB and comes down from the server (skill bar, auto-hunt). Don't put character state back here.
///
/// Shape:
///   { "Window": { "Position": {x,y}, "Size": {x,y} },
///     "Panels": { "InventoryPanel": {x,y}, "SkillsPanel": {x,y}, ... } }
/// "Panels" holds each popup's DRAG OFFSET from its authored home position (0,0 = untouched, i.e. the
/// default layout). Saved on close, not on every move.</summary>
public class ClientSettings
{
    public WindowGeom Window { get; set; } = new();

    /// <summary>Popup drag offsets, keyed by the panel's x:Name. Absent / (0,0) = default position.
    /// The skill bar's whole-stack move offset lives here too, under "SkillBar".</summary>
    public Dictionary<string, Vec2> Panels { get; set; } = new();

    /// <summary>How many skill-bar rows the player chose to show (1-5). 0 = auto-fit to the highest
    /// occupied slot. The bar data itself is always 60 slots; this is display only.</summary>
    public int SkillBarRows { get; set; }

    /// <summary>Search radius for the "target closest" action, in world units. A PREFERENCE, not a game
    /// rule — it only decides how far the client looks when picking a target, so it belongs on this
    /// machine rather than in the character row. Clamped to
    /// [<see cref="GameConstants.TargetSearchRangeMin"/>, <see cref="GameConstants.TargetSearchRangeMax"/>]
    /// on read, so a hand-edited file can't set it to something silly.</summary>
    public double TargetSearchRange
    {
        get => Math.Clamp(_targetSearchRange, GameConstants.TargetSearchRangeMin,
                                              GameConstants.TargetSearchRangeMax);
        set => _targetSearchRange = value;
    }
    private double _targetSearchRange = GameConstants.TargetSearchRangeDefault;

    private static string FilePath =>
        Path.Combine(AppContext.BaseDirectory, "client-settings.json");

    public static ClientSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<ClientSettings>(File.ReadAllText(FilePath)) ?? new ClientSettings();
            var fresh = new ClientSettings();
            fresh.Save();   // create the default so it's there to edit
            return fresh;
        }
        catch { return new ClientSettings(); }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* best-effort */ }
    }
}
