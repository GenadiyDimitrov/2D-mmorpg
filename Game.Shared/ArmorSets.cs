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
public record ArmorSetDef(string Id, string Name, ClassFlatBonus Bonus,
    // Optional PERCENT set bonuses (the flat ClassFlatBonus can't express these):
    float DefencePct = 0f,    // +% physical defence
    float CastSpeedPct = 0f,  // +% cast speed (faster)
    // Completion: a worn BODY whose SetId == this Id, PLUS the required accessory slots
    // whose SetId == AccessorySetId. "" => accessories must match this set's own Id (the
    // classic single-id set). Lets several bodies (e.g. light + robe newbie) SHARE one
    // accessory line while each body grants its own bonus.
    string AccessorySetId = "",
    // Which armor slots must be worn to complete the set. null = the classic four
    // (Head/Body/Gloves/Boots). Set e.g. [Body, Gloves] for a body+gloves-only set.
    // The bonus always comes from the BODY, so Body should be included.
    ArmorSlot[]? RequiredSlots = null);

public static class ArmorSetCatalog
{
    // ----- Stable set ids -----
    public const string DarkDominion = "set_dark_dominion";
    public const string NewbieLight = "set_newbie_light";
    public const string NewbieRobe  = "set_newbie_robe";
    // SHARED newbie accessory line (boots/gloves/helm) used by BOTH newbie body sets.
    // Just a SetId marker on those accessories — it has no ArmorSetDef of its own.
    public const string NewbieAccessories = "set_newbie_acc";

    private static readonly Dictionary<string, ArmorSetDef> _byId = new[]
    {
        new ArmorSetDef(DarkDominion, "Dark Dominion",
            // "+con/atk + max hp/mp" expressed as flat secondary deltas (tune later).
            new ClassFlatBonus(MaxHp: 150, MaxMp: 80, Defence: 25, Attack: 18,
                               Evasion: 6, Accuracy: 6)),
        // Newbie sets — the light/robe BODY grants the bonus; both share the same
        // newbie accessory line (boots/gloves/helm). Full set = body + 3 accessories.
        new ArmorSetDef(NewbieLight, "Newbie Light",
            new ClassFlatBonus(MaxHp: 42), DefencePct: 0.02f, AccessorySetId: NewbieAccessories),
        new ArmorSetDef(NewbieRobe, "Newbie Robe",
            new ClassFlatBonus(), CastSpeedPct: 0.15f, AccessorySetId: NewbieAccessories),
    }.ToDictionary(s => s.Id);

    /// <summary>The classic four-slot requirement used when a set doesn't override it.</summary>
    public static readonly ArmorSlot[] DefaultSlots =
        { ArmorSlot.Head, ArmorSlot.Body, ArmorSlot.Gloves, ArmorSlot.Boots };

    public static ArmorSetDef? Get(string id) =>
        string.IsNullOrEmpty(id) ? null : _byId.GetValueOrDefault(id);

    public static IEnumerable<ArmorSetDef> All => _byId.Values;
}
