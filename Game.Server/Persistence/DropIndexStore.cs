using System.Diagnostics;
using System.Globalization;
using System.Text;
using Game.Shared;

namespace Game.Server.Persistence;

/// <summary>THE DROP INDEX ON DISK — *"a drop db should be build once and only once when server starts …
/// it should remember it every restart until something tuches drops/mobs … on start if its missing its
/// build with drops x1 … same as server&lt;&gt;apk protocol -> a version that says (rebuild even when u
/// have the mob database) otherwise it only build if missing"* (owner, 2026-09-17, `BL-253`).
///
/// <para>The file is <c>dropindex.txt</c> beside <c>game.db</c>, in the CONTENT ROOT — i.e.
/// <c>Game.Server/</c>, not <c>bin/Debug/net8.0/</c>. Same trap as the database: a stale copy under bin
/// will happily be found by a tool run from there and make you believe you reset something you did not.</para>
///
/// <para>🔑 TWO STAMPS, BOTH ON THE HEADER LINE, AND THEY GUARD DIFFERENT THINGS.
/// <see cref="DropIndex.Version"/> is his *"rebuild even when u have the mob database"* lever — bumped by
/// hand when the rank-layer CODE changes, which no amount of hashing can see. The CONTENT HASH covers his
/// *"until something touches drops/mobs"* automatically, off the templates, their drop rows and the spawn
/// zones. Either stamp disagreeing rebuilds; both agreeing loads.</para>
///
/// <para>⚠ A PLAIN TEXT FORMAT, NOT JSON, AND THE REASON IS MEASURED. The whole index is ~20k rows and
/// builds from scratch in about 50 ms, so the cache is only worth having if reading it is FASTER than
/// that — and a JSON document of 20k objects is not. One separator-delimited line per row parses in a few
/// milliseconds and has the side benefit of being greppable by row. The boot log prints which path ran and how
/// long it took, so the claim stays checkable rather than remembered.</para>
///
/// <para>⚠ CHANCES ARE STORED AT x1. The live rate knobs are applied when a row is READ
/// (<see cref="DropIndex.ChanceFor"/>), which is what lets a <c>/droprate</c> change move every number in
/// the window without touching the cache — see the 🔑 on <see cref="DropIndex"/>.</para></summary>
public static class DropIndexStore
{
    public const string FileName = "dropindex.txt";

    /// <summary>The field separator: ASCII UNIT SEPARATOR, not a pipe. A creature name or a field name
    /// is free text and a pipe in one would silently shift every column after it; U+001F cannot occur in
    /// either. The file stays line-oriented, so it is still greppable by row.</summary>
    private const char Sep = (char)0x1f;

    /// <summary>What the last <see cref="LoadOrBuild"/> did, for the boot log and for `/dropindex`.</summary>
    public static string LastAction { get; private set; } = "not loaded";

    /// <summary>THE LIVE INDEX. Immutable once built, so a plain static is safe to read from the tick
    /// loop — it is content, not world state, and nothing writes it after boot except an explicit
    /// <see cref="Rebuild"/>, which the single-writer loop is the only caller of.
    ///
    /// <para>⚠ Lazily builds (without caching to disk) if something reads it before boot wired it up —
    /// a unit test, a tool. A lookup that silently returned nothing would read as "this item drops from
    /// nowhere", which is the one wrong answer this whole feature exists to prevent.</para></summary>
    public static DropIndexData Current
    {
        get => _current ??= DropIndex.Build();
        private set => _current = value;
    }

    private static DropIndexData? _current;

    /// <summary>The CONTENT ROOT the index was loaded from, remembered so an admin rebuild lands on the
    /// same file. Every other server-side file resolves against `AppContext.BaseDirectory` (the bin
    /// folder) — this one deliberately does not, because it belongs beside `game.db`.</summary>
    public static string Root { get; private set; } = ".";

    /// <summary>Load the cached index if both stamps still match, otherwise build it and write it.
    /// Never throws: a corrupt or unwritable file degrades to an in-memory build, because a drop lookup
    /// failing to cache is not a reason to refuse to run a game server.</summary>
    public static DropIndexData LoadOrBuild(string contentRoot, Action<string>? log = null)
    {
        Root = contentRoot;
        string path = Path.Combine(contentRoot, FileName);
        string wantHash = DropIndex.ContentHash();
        var sw = Stopwatch.StartNew();

        if (TryLoad(path, wantHash, out var loaded, out string why))
        {
            sw.Stop();
            Current = loaded;
            LastAction = $"loaded {loaded!.Sources.Count} rows from {FileName} in {sw.ElapsedMilliseconds} ms "
                       + $"(v{loaded.Version} #{loaded.ContentHash})";
            log?.Invoke(LastAction);
            return loaded;
        }

        var built = DropIndex.Build();
        long buildMs = sw.ElapsedMilliseconds;
        Current = built;
        string saved = TrySave(path, built) ? "saved" : "NOT saved (path not writable)";
        sw.Stop();
        LastAction = $"built {built.Sources.Count} rows in {buildMs} ms — {why}; {saved} to {FileName} "
                   + $"(v{built.Version} #{built.ContentHash})";
        log?.Invoke(LastAction);
        return built;
    }

    /// <summary>Force a rebuild and rewrite, whatever the stamps say — the admin half of his version
    /// lever, for when the layer code changed and nobody bumped <see cref="DropIndex.Version"/>.</summary>
    public static DropIndexData Rebuild(string? contentRoot = null)
    {
        var sw = Stopwatch.StartNew();
        var built = DropIndex.Build();
        Current = built;
        bool ok = TrySave(Path.Combine(contentRoot ?? Root, FileName), built);
        sw.Stop();
        LastAction = $"REBUILT {built.Sources.Count} rows in {sw.ElapsedMilliseconds} ms; "
                   + (ok ? "saved" : "NOT saved") + $" (v{built.Version} #{built.ContentHash})";
        return built;
    }

    private static bool TryLoad(string path, string wantHash, out DropIndexData? data, out string why)
    {
        data = null;
        if (!File.Exists(path)) { why = "no cached index"; return false; }

        try
        {
            using var reader = new StreamReader(path, Encoding.UTF8);
            string? header = reader.ReadLine();
            var h = header?.Split(Sep);
            if (h is null || h.Length < 3 || h[0] != "dropindex")
            { why = "cached index unreadable"; return false; }

            if (!int.TryParse(h[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int ver)
                || ver != DropIndex.Version)
            { why = $"index version {h[1]} != {DropIndex.Version}"; return false; }

            if (h[2] != wantHash)
            { why = "drops or mobs changed"; return false; }

            var rows = new List<DropSource>(24000);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (line.Length == 0) continue;
                var f = line.Split(Sep);
                if (f.Length != 12) { why = "cached index malformed"; return false; }
                rows.Add(new DropSource(
                    ItemId: f[0], MobId: f[1], MobName: f[2],
                    MinLevel: int.Parse(f[3], CultureInfo.InvariantCulture),
                    MaxLevel: int.Parse(f[4], CultureInfo.InvariantCulture),
                    BestLevel: int.Parse(f[5], CultureInfo.InvariantCulture),
                    Rank: (MobRank)int.Parse(f[6], CultureInfo.InvariantCulture),
                    Location: f[7],
                    BaseChance: float.Parse(f[8], NumberStyles.Float, CultureInfo.InvariantCulture),
                    MinQty: int.Parse(f[9], CultureInfo.InvariantCulture),
                    MaxQty: int.Parse(f[10], CultureInfo.InvariantCulture),
                    GroupId: int.Parse(f[11].TrimEnd('*'), CultureInfo.InvariantCulture),
                    IgnoresRates: f[11].EndsWith("*", StringComparison.Ordinal)));
            }
            data = new DropIndexData(ver, h[2], rows);
            why = "";
            return true;
        }
        catch (Exception ex)
        {
            why = $"cached index unreadable ({ex.GetType().Name})";
            return false;
        }
    }

    private static bool TrySave(string path, DropIndexData data)
    {
        try
        {
            var sb = new StringBuilder(data.Sources.Count * 80);
            sb.Append("dropindex").Append(Sep).Append(data.Version).Append(Sep).Append(data.ContentHash)
              .Append(Sep).Append("rows=").Append(data.Sources.Count).Append('\n');
            foreach (var s in data.Sources)
                sb.Append(s.ItemId).Append(Sep).Append(s.MobId).Append(Sep).Append(s.MobName).Append(Sep)
                  .Append(s.MinLevel).Append(Sep).Append(s.MaxLevel).Append(Sep).Append(s.BestLevel).Append(Sep)
                  .Append((int)s.Rank).Append(Sep).Append(s.Location).Append(Sep)
                  .Append(s.BaseChance.ToString("R", CultureInfo.InvariantCulture)).Append(Sep)
                  .Append(s.MinQty).Append(Sep).Append(s.MaxQty).Append(Sep)
                  .Append(s.GroupId).Append(s.IgnoresRates ? "*" : "").Append('\n');
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
