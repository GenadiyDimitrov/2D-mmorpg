using Game.Shared;

/// <summary>
/// THE DROP LOOKUP — *"i say what im looking for and it shows me all mob_name / [mob_lvl - elite|boss|
/// normal] / location / drop_rate"* (owner, 2026-09-16, `BL-253`).
///
/// <para>`dotnet run --project tools/BalanceMatrix -- --drops &lt;text&gt;` answers "where does this come
/// from" for any item, by name or by id, substring, case-insensitive.</para>
///
/// ── Why it walks SPAWNS and not the catalogue ──────────────────────────────────────────────────
/// 🔑 RANK IS A PROPERTY OF THE SPAWN, NOT OF THE TEMPLATE. Half the top-end faucets in the game —
/// every Greater/Safe enchant scroll, every Epic+ crafting material, every recipe book — are emitted
/// only for an Elite or a Boss kill, and the only thing that creates one of those is a ZONE. A tool
/// that read `MobType.Drops` would have reported, correctly and uselessly, that nothing drops
/// `scroll_greater_a`. So the unit of the answer is a (zone × template) pair: this is the same list
/// the kill roll builds in `GameLoopService.RollDrop`, assembled the same way, in the same order.
///
/// ⚠ EVERY CHANCE HERE GOES THROUGH <see cref="MobCatalog.EffectiveChance"/>, which is the rule the
/// whole drop system runs on: the kill roll, the target-inspect list and this tool must read the same
/// number or the number on screen stops being the number you get. Rates shown are therefore LIVE —
/// run it after a `/droprate` change and the table moves with it.
///
/// ⚠ THE RECIPE BOOKS ARE NOT `DropEntry`s. They are rolled by hand inside `RollBossBonus`, so their
/// rows below are reconstructed from that code and are the one place this file can silently drift
/// from the server. If you change that roll, change <see cref="RecipeRows"/> with it.
/// </summary>
internal static class DropFinder
{
    /// <summary>One place an item comes from.</summary>
    private readonly record struct Source(
        string ItemId, string ItemName, string MobName, string Level, MobRank Rank,
        string Location, float Chance, string Note);

    public static void Run(string[] args)
    {
        string query = args.Length > 1 ? string.Join(' ', args[1..]).Trim() : "";
        if (query.Length == 0)
        {
            Console.WriteLine("usage: --drops <item name or id>   e.g. --drops \"greater scroll\", --drops epic_wood");
            return;
        }

        var hits = new List<Source>();
        foreach (var zone in WorldMap.SpawnZones)
            foreach (string id in zone.MobTypes)
                Collect(zone, id, query, hits);

        if (hits.Count == 0)
        {
            Console.WriteLine($"\nNothing in the world drops anything matching \"{query}\".");
            Console.WriteLine("(Check the spelling, then check whether it is craft-only or vendor-only —");
            Console.WriteLine(" this tool reports MOB sources and nothing else.)");
            return;
        }

        Console.WriteLine($"\n═══ WHAT DROPS \"{query}\" ═══");
        Console.WriteLine("Chance is PER KILL, with every live rate knob applied (global × group × item).\n");

        foreach (var group in hits.GroupBy(h => h.ItemId).OrderBy(g => g.Key))
        {
            var rows = group.OrderByDescending(r => r.Chance).ToList();
            Console.WriteLine($"── {rows[0].ItemName}  [{group.Key}]  — {rows.Count} source(s)");
            Console.WriteLine($"   {"creature",-28} {"lvl",-7} {"rank",-6} {"where",-26} {"per kill",10}  note");
            foreach (var r in rows)
                Console.WriteLine($"   {Trim(r.MobName, 28),-28} {r.Level,-7} {Rank(r.Rank),-6} "
                                + $"{Trim(r.Location, 26),-26} {Pct(r.Chance),10}  {r.Note}");
            Console.WriteLine();
        }
    }

    /// <summary>Every row one (zone × template) pair pays, assembled exactly as `RollDrop` assembles it.</summary>
    private static void Collect(SpawnZone zone, string mobId, string query, List<Source> hits)
    {
        var type = MobCatalog.Get(mobId);
        if (type.Dummy) return;

        // The LEVELS this spawner actually produces. A template with a natural level brings its own and
        // the zone's band is only a label — unless ForceZoneLevel, where the zone wins. Same rule as
        // WorldPlan.DedicatedFor and GameLoopService's spawn path; getting it wrong here would report a
        // band's drops off a creature that never spawns in it.
        int lo = type.Level > 0 && !zone.ForceZoneLevel ? type.Level : zone.MinLevel;
        int hi = type.Level > 0 && !zone.ForceZoneLevel ? type.Level : zone.MaxLevel;
        string levelLabel = lo == hi ? lo.ToString() : $"{lo}-{hi}";
        string where = LocationOf(zone);

        // The drop rows are level-gated (DropEntry.MinLevel/MaxLevel), so a band is walked level by
        // level and the best chance in the band is what the row reports — a band that pays a thing at
        // only one of its levels is still a source, and saying "76-79" next to the 79-only rate would
        // be the lie this whole tool exists to avoid. The note says so when it happens.
        var best = new Dictionary<string, (float Chance, int At)>();

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
            {
                if (!Matches(e.ItemId, query)) continue;
                float c = MobCatalog.EffectiveChance(e);
                if (!best.TryGetValue(e.ItemId, out var had) || c > had.Chance)
                    best[e.ItemId] = (c, lvl);
            }

            foreach (var (itemId, chance) in RecipeRows(lvl, zone.Rank))
            {
                if (!Matches(itemId, query)) continue;
                if (!best.TryGetValue(itemId, out var had) || chance > had.Chance)
                    best[itemId] = (chance, lvl);
            }
        }

        foreach (var (itemId, (chance, at)) in best)
            hits.Add(new Source(itemId, NameOf(itemId), type.Name, levelLabel, zone.Rank, where, chance,
                                lo == hi || at == lo ? "" : $"only from level {at}"));
    }

    /// <summary>The RECIPE BOOK roll from `GameLoopService.RollBossBonus`, mirrored. ⚠ These are not
    /// DropEntries and there is no shared function to call — see the ⚠ in the file header. Since
    /// `BL-247` the roll takes the rate knobs like everything else, so the numbers here are the
    /// authored chance × the live "other" group rate ÷ its own ×3 baseline, i.e. the global rate.</summary>
    private static IEnumerable<(string ItemId, float Chance)> RecipeRows(int level, MobRank rank)
    {
        if (rank == MobRank.Normal || level < 76) yield break;
        float rate = MobCatalog.EffectiveRate(0) / 3f;   // the "other" group's ×3 is baked into the author

        IEnumerable<(string, float)> Roll(float chance, params string[] keys)
        {
            foreach (string k in keys)
                yield return (ItemCatalog.RecipeBookId($"craft_{k}_t76"), chance * rate / keys.Length);
        }

        if (rank == MobRank.Boss)
        {
            foreach (var r in Roll(0.50f, "heavy", "light", "robe", "helm", "gloves", "boots", "shield")) yield return r;
            foreach (var r in Roll(0.40f, "sword1h", "sword2h", "blunt1h", "blunt2h", "duals", "bow", "wand", "staff")) yield return r;
            foreach (var r in Roll(0.60f, "necklace", "ring", "earring")) yield return r;
        }
        else
        {
            foreach (var r in Roll(0.001f, "heavy", "light", "robe", "sword1h", "sword2h", "bow", "wand",
                                   "necklace", "ring", "earring")) yield return r;
        }
    }

    /// <summary>The FIELD whose polygon holds this spawner, which is the name a player would say. Every
    /// spawner is inside one — `RegionMap.ValidateSpawnersInFields` fails the boot otherwise — so the
    /// nearest-town fallback is for a tool run against a half-edited world, not for normal use.</summary>
    private static string LocationOf(SpawnZone z)
    {
        foreach (var f in RegionMap.Fields)
            if (f.Contains(z.X, z.Y))
                return f.Name;
        return $"near {WorldMap.NearestSafeZone(z.X, z.Y).Name}";
    }

    private static bool Matches(string itemId, string query) =>
        itemId.Contains(query, StringComparison.OrdinalIgnoreCase)
        || NameOf(itemId).Contains(query, StringComparison.OrdinalIgnoreCase);

    private static string NameOf(string itemId) =>
        ItemCatalog.Get(itemId) is ItemDef d ? d.Name : itemId;

    private static string Rank(MobRank r) => r switch
    {
        MobRank.Boss => "BOSS",
        MobRank.Elite => "elite",
        _ => "normal",
    };

    /// <summary>A per-kill chance in the unit a player reads it in. Above 100% it is COPIES per kill
    /// (MobCatalog.DropCopies), which is what a high server rate means everywhere else.</summary>
    private static string Pct(float c) =>
        c >= 1f ? $"×{c:0.##}"
        : c >= 0.01f ? $"{c * 100f:0.##}%"
        : c >= 0.0001f ? $"{c * 100f:0.####}%"
        : $"{c * 100f:0.000000}%";

    private static string Trim(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}
