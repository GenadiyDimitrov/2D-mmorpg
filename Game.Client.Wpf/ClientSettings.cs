using System;
using System.IO;
using System.Text.Json;

namespace Game.Client.Wpf;

/// <summary>Persistent client preferences (window size, …). Stored in %LocalAppData%\L2Clone so it's
/// NOT in the repo and NOT copied to the build/Debug folder. Best-effort — a bad/missing file just
/// yields defaults.</summary>
public class ClientSettings
{
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 800;

    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "L2Clone", "client-settings.json");

    public static ClientSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<ClientSettings>(File.ReadAllText(FilePath)) ?? new ClientSettings();
        }
        catch { /* ignore */ }
        return new ClientSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch { /* best-effort */ }
    }
}
