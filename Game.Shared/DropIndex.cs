using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Shared;

/// <summary>ONE PLACE AN ITEM COMES FROM — a (zone × template) pair and the chance it pays.</summary>
/// <param name="BaseChance">The chance at **x1**, with no rate knob applied. Every knob is live and
/// admin-editable, so storing a multiplied number would freeze whatever the rate was on the day the
/// index was built. Read it through <see cref="DropIndex.ChanceFor"/>, never directly.</param>
/// <param name="IgnoresRates">True for the boss/elite MAT PILE, which is a flat give inside the kill
/// path that no multiplier reaches (<see cref="MobCatalog.BossPile"/>). For those rows
/// <see cref="DropIndex.ChanceFor"/> returns <paramref name="BaseChance"/> unchanged — showing a player
/// a rate-scaled number for a drop that ignores rates is the exact lie this system exists to avoid.</param>
/// <param name="BestLevel">The level inside the band at which <paramref name="BaseChance"/> is paid. A
/// `DropEntry` can be level-gated inside a spawner's band, so "76-79" beside a 79-only rate would be
/// wrong; when this is not the band's floor the UI says "from level N".</param>
public sealed record DropSource(
    string ItemId,
    string MobId, string MobName,
    int MinLevel, int MaxLevel, int BestLevel,
    MobRank Rank, string Location,
    float BaseChance, int MinQty, int MaxQty, int GroupId,
    bool IgnoresRates);

/// <summary>A BUILT index plus the two stamps that say whether it is still true.</summary>
/// <param name="Version">The hand-bumped <see cref="DropIndex.Version"/> the index was built by. It
/// covers the things a content hash CANNOT see — the CODE of the rank layers.</param>
/// <param name="ContentHash">A hash of the data the walk reads (templates, their drop rows, the spawn
/// zones). It covers *"until something touches drops/mobs"* automatically.</param>
public sealed record DropIndexData(int Version, string ContentHash, IReadOnlyList<DropSource> Sources);

/// <summary>THE DROP DATABASE — *"i say what im looking for and it shows me all mob_name/[mob_lvl-elite|
/// boss|normal]/location/drop_rate"* (owner, 2026-09-16, `BL-253`).
///
/// <para>🔑 IT WALKS SPAWNS, NOT TEMPLATES. Rank is a property of the SPAWN — the zone assigns it — and
/// half the top-end faucets in the game (every Greater/Safe enchant scroll, every Epic+ material, every
/// recipe book, the whole boss mat pile) exist only for an Elite or a Boss kill. A lookup written against
/// <c>MobType.Drops</c> would answer "nothing drops this" — correctly, and uselessly. So the unit of an
/// answer is a (zone × template) pair, assembled the same way and in the same order as
/// <c>GameLoopService.RollDrop</c> assembles it.</para>
///
/// <para>🔑 EVERY CHANCE IS STORED AT x1 AND MULTIPLIED WHEN IT IS READ. That is what lets the index be
/// built once and cached for good: the rate knobs (<c>/droprate</c>, a Rune of Drop, the group rates) are
/// live and change under the cache, and a stored *effective* number would go stale the first time he
/// typed a command. His words for the build step were *"if its missing its build with drops x1"* — this
/// is that, and it is also why a rate change is not a reason to rebuild.</para>
///
/// <para>🔑 WHEN IT REBUILDS — his *"a version that says (rebuild even when u have the mob database),
/// otherwise it only build if missing"*. There are TWO stamps and they cover different failures:</para>
/// <list type="bullet">
///   <item><b><see cref="ContentHash"/></b> — hashed off the templates, their drop rows and the spawn
///   zones. It catches *"something touches drops/mobs"* with nobody having to remember anything, which
///   is the half that would otherwise rot silently.</item>
///   <item><b><see cref="Version"/></b> — bumped by hand. It catches what a data hash cannot see: the
///   rank LAYERS are code (<see cref="MobCatalog.GearDrops"/>, <see cref="MobCatalog.EnchantScrollDrops"/>,
///   <see cref="MobCatalog.EliteMatDrops"/>, <see cref="MobCatalog.UtilityScrollDrops"/>,
///   <see cref="MobCatalog.RecipeRolls"/>, <see cref="MobCatalog.BossPile"/>), and editing a number
///   inside one of those methods moves no data at all. ⚠ EDIT ANY OF THEM, BUMP THIS.</item>
/// </list>
///
/// <para>⚠ A stale index is worse than a slow one: it tells a player to farm a creature that does not pay.
/// When in doubt, bump.</para></summary>
public static class DropIndex
{
    /// <summary>Bump when the rank-LAYER code changes — see the ⚠ in the class summary. Data changes are
    /// caught by <see cref="ContentHash"/> and need no bump.
    /// <list type="bullet"><item>1 — first build (`BL-253`, 0.168.0).</item></list></summary>
    public const int Version = 1;

    /// <summary>Build the whole index. A few hundred milliseconds; the server does it once and caches the
    /// result to disk, so this runs on a fresh checkout and after a content change and never otherwise.</summary>
    public static DropIndexData Build()
    {
        var sources = new List<DropSource>();
        foreach (var zone in WorldMap.SpawnZones)
            foreach (string mobId in zone.MobTypes)
                Collect(zone, mobId, sources);
        return new DropIndexData(Version, ContentHash(), sources);
    }

    /// <summary>Every row one (zone × template) pair pays, assembled exactly as `RollDrop` assembles it.</summary>
    private static void Collect(SpawnZone zone, string mobId, List<DropSource> into)
    {
        var type = MobCatalog.Get(mobId);
        if (type.Dummy) return;

        // The LEVELS this spawner actually produces. A template with a natural level brings its own and
        // the zone's band is only a label — unless ForceZoneLevel, where the zone wins. Same rule as
        // WorldPlan.DedicatedFor and GameLoopService's spawn path; getting it wrong here would report a
        // band's drops off a creature that never spawns in it.
        int lo = type.Level > 0 && !zone.ForceZoneLevel ? type.Level : zone.MinLevel;
        int hi = type.Level > 0 && !zone.ForceZoneLevel ? type.Level : zone.MaxLevel;
        string where = LocationOf(zone);

        // A drop row is level-gated inside the band (DropEntry.MinLevel/MaxLevel), so the band is walked
        // level by level and the BEST chance in it is what the row reports — a band that pays a thing at
        // only one of its levels is still a source, and quoting the one-level rate against "76-79" is the
        // lie this whole thing exists to avoid. BestLevel carries which level it was.
        var best = new Dictionary<string, DropSource>();

        void Offer(DropSource s)
        {
            if (!best.TryGetValue(s.ItemId, out var had) || s.BaseChance > had.BaseChance)
                best[s.ItemId] = s;
        }

        for (int lvl = lo; lvl <= hi; lvl++)
        {
            var rows = new List<DropEntry>();
            if (type.Drops is not null)
                rows.AddRange(type.Drops.Where(e => e.AppliesAtLevel(lvl)));

            if (zone.Rank != MobRank.Normal)
            {
                rows.RemoveAll(e => MobCatalog.IsGearGroup(e.GroupId));
                rows.AddRange(MobCatalog.GearDrops(lvl, zone.Rank));
                rows.AddRange(MobCatalog.EnchantScrollDrops(lvl, zone.Rank));
                rows.AddRange(MobCatalog.UtilityScrollDrops(lvl, zone.Rank));
                rows.AddRange(MobCatalog.EliteMatDrops(lvl, zone.Rank, type.Category));
            }

            foreach (var e in rows)
                Offer(new DropSource(e.ItemId, mobId, type.Name, lo, hi, lvl, zone.Rank, where,
                                     e.Chance, e.MinQty, e.MaxQty, e.GroupId, IgnoresRates: false));

            // The two faucets that are NOT DropEntries — both read from the same tables the kill path
            // reads, which is why they moved into MobCatalog (`BL-253`).
            foreach (var roll in MobCatalog.RecipeRolls(lvl, zone.Rank))
            {
                float each = roll.Delivered / MobCatalog.RecipeOtherGroupRate / roll.BookIds.Length;
                foreach (string bookId in roll.BookIds)
                    Offer(new DropSource(bookId, mobId, type.Name, lo, hi, lvl, zone.Rank, where,
                                         each, 1, 1, GroupId: 0, IgnoresRates: false));
            }

            foreach (var row in MobCatalog.BossPile(lvl, zone.Rank, type.Category))
                Offer(new DropSource(Crafting.MaterialId(row.Type, row.Rarity), mobId, type.Name, lo, hi,
                                     lvl, zone.Rank, where, row.Chance, row.MinQty, row.MaxQty,
                                     GroupId: 0, IgnoresRates: true));
        }

        into.AddRange(best.Values);
    }

    /// <summary>THE chance this source pays a given player, with every live knob applied — the same
    /// <see cref="MobCatalog.EffectiveChance"/> the kill roll and the target-inspect list use, so the
    /// number in the window is the number he gets. The mat pile is the one exception and says so.</summary>
    public static float ChanceFor(DropSource s, float playerMult = 1f) =>
        s.IgnoresRates
            ? s.BaseChance
            : MobCatalog.EffectiveChance(new DropEntry(s.ItemId, s.BaseChance, s.MinQty, s.MaxQty,
                                                       GroupId: s.GroupId), playerMult);

    /// <summary>Everything matching a free-text query, by item id or item NAME, substring, case
    /// insensitive — richest source first, then by creature level. Grouped by item by the caller.</summary>
    public static List<DropSource> Find(DropIndexData data, string query, float playerMult = 1f)
    {
        query = (query ?? "").Trim();
        if (query.Length == 0) return new List<DropSource>();

        var hits = data.Sources.Where(s => Matches(s.ItemId, query)).ToList();
        hits.Sort((a, b) =>
        {
            int byItem = string.CompareOrdinal(a.ItemId, b.ItemId);
            if (byItem != 0) return byItem;
            int byChance = ChanceFor(b, playerMult).CompareTo(ChanceFor(a, playerMult));
            return byChance != 0 ? byChance : a.MinLevel.CompareTo(b.MinLevel);
        });
        return hits;
    }

    private static bool Matches(string itemId, string query) =>
        itemId.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
        || NameOf(itemId).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

    public static string NameOf(string itemId) =>
        ItemCatalog.Get(itemId) is ItemDef d ? d.Name : itemId;

    /// <summary>The FIELD whose polygon holds this spawner, which is the name a player would say. Every
    /// spawner is inside one — `RegionMap.ValidateSpawnersInFields` fails the boot otherwise — so the
    /// nearest-town fallback is for a half-edited world, not for normal use.</summary>
    private static string LocationOf(SpawnZone z)
    {
        foreach (var f in RegionMap.Fields)
            if (f.Contains(z.X, z.Y))
                return f.Name;
        return $"near {WorldMap.NearestSafeZone(z.X, z.Y).Name}";
    }

    /// <summary>A stable hash of everything the walk READS as data: the templates, their authored drop
    /// rows, and the spawn zones' rosters and bands.
    ///
    /// <para>⚠ FNV-1a by hand, NOT <c>string.GetHashCode</c>. .NET randomises string hashing per PROCESS,
    /// so a built-in hash would differ on every restart and the cache would rebuild every single boot
    /// while looking as though it were working.</para></summary>
    public static string ContentHash()
    {
        var sb = new StringBuilder();
        foreach (var t in MobCatalog.Templates.OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            sb.Append(t.Id).Append('|').Append(t.Level).Append('|').Append((int)t.Category)
              .Append('|').Append(t.Dummy ? 1 : 0).Append(';');
            if (t.Drops is not null)
                foreach (var d in t.Drops)
                    sb.Append(d.ItemId).Append(',').Append(d.Chance.ToString("R")).Append(',')
                      .Append(d.MinQty).Append(',').Append(d.MaxQty).Append(',')
                      .Append(d.MinLevel).Append(',').Append(d.MaxLevel).Append(',')
                      .Append(d.GroupId).Append(';');
            sb.Append('\n');
        }
        foreach (var z in WorldMap.SpawnZones)
        {
            sb.Append(z.X).Append(',').Append(z.Y).Append(',').Append(z.MinLevel).Append(',')
              .Append(z.MaxLevel).Append(',').Append((int)z.Rank).Append(',')
              .Append(z.ForceZoneLevel ? 1 : 0).Append(':');
            foreach (string m in z.MobTypes) sb.Append(m).Append(',');
            sb.Append('\n');
        }

        ulong h = 14695981039346656037UL;
        string s = sb.ToString();
        for (int i = 0; i < s.Length; i++)
        {
            h ^= s[i];
            h *= 1099511628211UL;
        }
        return h.ToString("x16");
    }
}
