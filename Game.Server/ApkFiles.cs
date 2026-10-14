namespace Game.Server;

/// <summary>
/// The Android client this server hands out at <c>/apk</c>, and its version.
///
/// `BL-345` — WHERE it is comes from `apk-path.txt` beside the server (first non-#, non-blank line: an
/// .apk file, or a folder whose HIGHEST-VERSION .apk is served; relative = relative to that file). No file =
/// the `apk` folder beside the server, then the repo's Unity builds folder for the dev PC.
///
/// The version is the one publish.ps1 writes into the name (L2Clone-0.258.3.apk). A folder is ranked by it, not by
/// the file's date, so copying an older APK in later never puts it in front (0.259.0); a name without a version
/// ranks below any with one, then by date.
/// </summary>
internal static class ApkFiles
{
    /// <summary>The host's content root (Game.Server/ in dev) — set once by Program at startup.</summary>
    internal static string ContentRoot = Directory.GetCurrentDirectory();

    internal static string? VersionOf(string fileName) =>
        System.Text.RegularExpressions.Regex.Match(fileName, @"-(\d+(?:\.\d+)+)\.(?:apk|zip)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase) is { Success: true } m ? m.Groups[1].Value : null;

    /// <summary>The highest-version file matching <paramref name="pattern"/> in <paramref name="dir"/> (ranked by the
    /// version in the name, then by date), or null.</summary>
    private static string? Newest(string dir, string pattern) =>
        !Directory.Exists(dir) ? null
        : new DirectoryInfo(dir).GetFiles(pattern)
            .OrderByDescending(f => Version.TryParse(VersionOf(f.Name), out var v) ? v : new Version(0, 0))
            .ThenByDescending(f => f.LastWriteTimeUtc).FirstOrDefault()?.FullName;

    /// <summary>`BL-360` — the Windows test client served at <c>/pc</c>: the newest zip in the <c>pc</c> folder beside
    /// the server (tools/deploy.ps1 fills it, as it fills <c>apk</c>), then the repo's <c>builds/</c> on the dev PC.</summary>
    internal static string? FindPc() =>
        Newest(Path.Combine(AppContext.BaseDirectory, "pc"), "*.zip")
        ?? Newest(Path.GetFullPath(Path.Combine(ContentRoot, "..", "builds")), "Vardelar-Windows-*.zip");

    internal static string? Find()
    {
        string? Resolve(string path) => File.Exists(path) ? path : Newest(path, "*.apk");
        string baseDir = AppContext.BaseDirectory;
        string cfg = Path.Combine(baseDir, "apk-path.txt");
        if (File.Exists(cfg)
            && File.ReadLines(cfg).Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0 && !l.StartsWith("#"))
               is string line)
            return Resolve(Path.GetFullPath(Path.Combine(baseDir, line)));
        return Resolve(Path.Combine(baseDir, "apk"))
            ?? Resolve(Path.GetFullPath(Path.Combine(ContentRoot, "..", "Game.Client.Unity", "builds", "L2Clone.apk")));
    }

    // ─── "A NEW VERSION IS AVAILABLE" (owner, 2026-10-14) ──────────────────────────────────────────
    // Judged against the APK this server actually OFFERS, never against the server's own version: a
    // server-only release (0.259.2 shipped as a zip with no APK) must not tell everyone to update to a
    // client that does not exist. Read on the game loop at every world entry, so the folder listing is
    // cached for a minute — a `deploy -NoServer` drops a new APK in without a restart, and is noticed
    // within that minute.
    private static string? _offered;
    private static DateTime _offeredReadUtc = DateTime.MinValue;

    /// <summary>The version of the APK on offer, or null when none is hosted or its name has none.</summary>
    internal static string? OfferedVersion()
    {
        if (DateTime.UtcNow - _offeredReadUtc < TimeSpan.FromMinutes(1)) return _offered;
        try { _offered = Find() is string apk ? VersionOf(Path.GetFileName(apk)) : null; }
        catch { _offered = null; }   // an unreadable folder offers nothing; entering the world must not fail
        _offeredReadUtc = DateTime.UtcNow;
        return _offered;
    }

    /// <summary>The newer version a client of <paramref name="clientVersion"/> should install, or null when
    /// it is current (or either version cannot be read — an empty label is local tooling).</summary>
    internal static string? NewerThan(string? clientVersion) =>
        OfferedVersion() is string offered
        && Version.TryParse(offered, out var o)
        && Version.TryParse(clientVersion, out var c)
        && o > c ? offered : null;
}
