using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>What the ground LOOKS like somewhere. Cosmetic only: the server never reads it.</summary>
public enum GroundBiome { Meadow = 0, Moor = 1, Ash = 2, Marsh = 3, Highland = 4, Snow = 5, Lava = 6 }

/// <summary>
/// Which biome each part of the overworld wears (`BL-344` step 3, 2026-10-12).
///
/// A biome belongs to a city's TERRITORY: the town and every field it manages, with a per-field override
/// where a field's own name says otherwise (the Ashen / Emberdust / Cinderflat Barrens are ash; the owner
/// asked for one lava field, and Redhorn Highlands is it). The client blends neighbouring territories by
/// distance, so two biomes that touch fade into each other instead of meeting at a seam.
///
/// <para>🔑 Kept here, not in the client, because it is WORLD data. When the map becomes the owner's grid
/// of zone cells (`BL-346`), the biome moves onto the cell and <see cref="Sites"/> is rebuilt from that
/// grid; the client's ground keeps calling the same thing.</para>
/// </summary>
public static class Biomes
{
    /// <summary>Each managing city's own biome. A town missing here is paved but is not a blend site.</summary>
    private static readonly Dictionary<string, GroundBiome> CityBiome = new()
    {
        ["town_brackenford"] = GroundBiome.Meadow,
        ["town_stonewatch"] = GroundBiome.Moor,
        ["outpost_training"] = GroundBiome.Moor,     // Stonewatch's doorstep
        ["town_greymarsh"] = GroundBiome.Marsh,
        ["castle_ironreach"] = GroundBiome.Highland,
        ["town_frostmere"] = GroundBiome.Snow,
    };

    /// <summary>Fields whose name says they are not their city's biome.</summary>
    private static readonly Dictionary<string, GroundBiome> FieldBiome = new()
    {
        ["field_stone_barrens"] = GroundBiome.Ash,
        ["field_stone_barrens_2"] = GroundBiome.Ash,
        ["field_stone_barrens_3"] = GroundBiome.Ash,
        ["field_stone_barrens_4"] = GroundBiome.Ash,
        ["field_iron_highlands"] = GroundBiome.Lava,
    };

    /// <summary>One point of biome influence: a town centre or a field's centroid.</summary>
    public readonly record struct Site(float X, float Y, GroundBiome Biome);

    /// <summary>Every blend site in the overworld. The ground colour at a point is a distance-weighted mix
    /// of the nearest few, so a territory reaches about halfway to its neighbour and then fades.</summary>
    public static readonly Site[] Sites = BuildSites();

    private static Site[] BuildSites()
    {
        var sites = new List<Site>();
        foreach (var town in Towns.All)
            if (CityBiome.TryGetValue(town.Id, out var b))
                sites.Add(new Site(town.X, town.Y, b));

        foreach (var field in RegionMap.Fields)
        {
            if (field.Outline.Length < 3 || field.MaxX < 0 || field.MaxY < 0) continue;   // dungeons have their own floor
            GroundBiome biome;
            if (!FieldBiome.TryGetValue(field.Id, out biome) && !CityBiome.TryGetValue(field.CityId, out biome))
                continue;   // a hand-authored area with no city borrows whatever surrounds it
            sites.Add(new Site(field.Outline.Average(p => p.X), field.Outline.Average(p => p.Y), biome));
        }
        return sites.ToArray();
    }
}
