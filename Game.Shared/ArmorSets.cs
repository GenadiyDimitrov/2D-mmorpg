namespace Game.Shared;

/// <summary>
/// A named armor set. Wearing all FOUR armor slots (Head/Body/Gloves/Boots) that
/// share this set's <see cref="Id"/> grants <see cref="Bonus"/> on top of each
/// piece's own stats + rolled attributes. A set can offer several BODY weight
/// variants (e.g. a heavy and a robe "DarkDominion") that share the same
/// accessories — wearing any one body variant + the three accessories completes it.
/// The bonus reuses <see cref="ClassFlatBonus"/> (flat secondary deltas), applied
/// exactly like a class identity bonus.
/// </summary>
public record ArmorSetDef(string Id, string Name, ClassFlatBonus Bonus);

public static class ArmorSetCatalog
{
    // ----- Stable set ids -----
    public const string DarkDominion = "set_dark_dominion";

    private static readonly Dictionary<string, ArmorSetDef> _byId = new[]
    {
        new ArmorSetDef(DarkDominion, "Dark Dominion",
            // "+con/atk + max hp/mp" expressed as flat secondary deltas (tune later).
            new ClassFlatBonus(MaxHp: 150, MaxMp: 80, Defence: 25, Attack: 18,
                               Evasion: 6, Accuracy: 6)),
    }.ToDictionary(s => s.Id);

    public static ArmorSetDef? Get(string id) =>
        string.IsNullOrEmpty(id) ? null : _byId.GetValueOrDefault(id);

    public static IEnumerable<ArmorSetDef> All => _byId.Values;
}
