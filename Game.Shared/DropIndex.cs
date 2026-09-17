using System;
using System.Collections.Generic;
using System.Linq;

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

/// <summary>A BUILT index.</summary>
public sealed record DropIndexData(IReadOnlyList<DropSource> Sources);

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
/// <para>🔑 EVERY CHANCE IS HELD AT x1 AND MULTIPLIED WHEN IT IS READ. The rate knobs (<c>/droprate</c>,
/// a Rune of Drop, the group rates) are live and admin-editable, so a stored *effective* number would be
/// wrong the first time one moved. The index holds the authored chance; <see cref="ChanceFor"/> applies
/// the knobs. That is his *"build with drops x1"*.</para>
///
/// <para>🔴 <b>IT IS BUILT ONCE PER SERVER START AND NEVER CACHED TO DISK — his ruling, 2026-09-17:</b>
/// *"If drop indexes are build even after x10 more mobs still faster than reading a file, build each
/// restart. (that way no drop version needed)"*. It is, and so there is no file, no content hash and no
/// version stamp. <b>MEASURED:</b> building 19,842 rows takes <b>13 ms</b> in-process; reading the same
/// rows back from a 1.8 MB file took <b>28 ms</b>. Both are linear in rows — 0.65 µs/row to build against
/// 1.4 µs/row to parse — so the build stays roughly twice as fast at any world size, and a ten-fold world
/// is ~130 ms against ~280 ms.</para>
///
/// <para>🔑 <b>AND THE STALENESS PROBLEM WENT WITH THE FILE</b>, which is the real win rather than the
/// milliseconds. A cache needed two stamps to know when it had gone wrong: a content hash for the DATA,
/// and a hand-bumped version for the rank-LAYER CODE (<see cref="MobCatalog.GearDrops"/>,
/// <see cref="MobCatalog.EnchantScrollDrops"/>, <see cref="MobCatalog.EliteMatDrops"/>,
/// <see cref="MobCatalog.UtilityScrollDrops"/>, <see cref="MobCatalog.RecipeRolls"/>,
/// <see cref="MobCatalog.BossPile"/>) — because editing a number inside one of those methods moves no
/// data at all and no hash could see it. That second stamp was a thing a person had to remember, forever,
/// or the window would quietly tell a player to farm a creature that does not pay. Nothing to remember
/// now: a restart is the invalidation.</para></summary>
public static class DropIndex
{
    /// <summary>Build the whole index — ~13 ms for ~20k rows. The server does this once at boot and holds
    /// the result; nothing writes it to disk (see the 🔴 in the class summary).</summary>
    public static DropIndexData Build()
    {
        var sources = new List<DropSource>();
        foreach (var zone in WorldMap.SpawnZones)
            foreach (string mobId in zone.MobTypes)
                Collect(zone, mobId, sources);
        return new DropIndexData(sources);
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

        // 🔑 AN EXACT ITEM ID MEANS EXACTLY THAT ITEM, and this is what makes the client's prediction
        // list work: you pick "Common Wood" and you get Common Wood, not Common Wood beside every id
        // that happens to contain it. A substring search is the fallback for free text he typed himself.
        // ⚠ Checked against the CATALOGUE, not against the index — an id that is real but drops from
        // nothing must answer "nothing drops this", never fall through to a substring sweep.
        bool exact = ItemCatalog.Get(query) is not null;

        var hits = data.Sources.Where(s => exact
                                           ? string.Equals(s.ItemId, query, StringComparison.Ordinal)
                                           : Matches(s.ItemId, query)).ToList();
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

}
