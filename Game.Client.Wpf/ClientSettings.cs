using System;
using System.IO;
using System.Text.Json;

namespace Game.Client.Wpf;

/// <summary>Persistent CLIENT preferences — window geometry, and nothing else. Lives NEXT TO THE EXE
/// (the Debug/publish output folder), like an options.ini. It is NOT a build item, so an update or
/// rebuild never overwrites it; if it's missing the app writes a default. Best-effort — a bad file
/// just yields defaults.
///
/// This file is for things that belong to THIS MACHINE. Anything that belongs to the CHARACTER lives
/// in the DB and comes down from the server: the skill bar (SkillBarDto) and the auto-hunt config
/// (AutoHuntConfigDto) both used to be here, which meant they didn't follow the account to another
/// machine. Don't put character state back in here.</summary>
public class ClientSettings
{
    // Startup position (virtual-screen coords — negative Left/Top puts it on a monitor left/above
    // the primary, e.g. to send clients to your 1st monitor). Edit here or just drag+close a window.
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 100;
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 800;

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
        try { File.WriteAllText(FilePath, JsonSerializer.Serialize(this)); }
        catch { /* best-effort */ }
    }
}
