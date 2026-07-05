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
    // Full StatMods bonus (the tiered gear sets use this). SECONDARY stats (pDef%/mDef%/pAtk%/
    // cast%/as%/HP/MP/eva/regen/vamp/reflect) are applied in RecomputeDerived's set block.
    // PRIMARY-stat deltas (Str/Dex/Con/…) are STORED but NOT yet applied — they need a derivation
    // pre-pass (so "CON +3" raises HP); wire that as a separate, tested pass.
    StatMods Mods = default,
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
    }.Concat(TieredSets()).ToDictionary(s => s.Id);

    // ----- Tiered gear sets (docs/gear/gear_sets.csv, BASE variant per weight/tier). Each body of a
    // tier + that tier's shared accessory line (set_acc_t{lv}) completes it. Bonuses are the FULL
    // StatMods; SECONDARY stats apply now, PRIMARY-stat deltas (Con/Str/…) are stored for the pre-pass.
    // CC-resist / shield-conditional / reflect(heavy) bonus lines are DEFERRED (mechanics pending). -----
    private static ArmorSetDef GearSet(string weightKey, int level, string family, StatMods mods) =>
        new($"set_{weightKey}_t{level}", $"{family} {ItemCatalog.TierLetter(level)}",
            new ClassFlatBonus(), Mods: mods, AccessorySetId: $"set_acc_t{level}");

    private static IEnumerable<ArmorSetDef> TieredSets() => new[]
    {
        // Heavy — "Ironforge"
        GearSet("heavy", 20, "Ironforge", new StatMods(PDefPct: 0.05f, MaxHp: 135)),
        GearSet("heavy", 40, "Ironforge", new StatMods(MaxHp: 270)),
        GearSet("heavy", 52, "Ironforge", new StatMods(Con: 3, Str: 3)),
        GearSet("heavy", 61, "Ironforge", new StatMods(PAtkPct: 0.04f, Con: 2, Dex: -2)),
        GearSet("heavy", 76, "Ironforge", new StatMods(MaxHp: 455, Str: 2, Con: 2, Dex: -2)),
        // Light — "Nightleaf"
        GearSet("light", 20, "Nightleaf", new StatMods(Evasion: 2, MaxMp: 92)),
        GearSet("light", 40, "Nightleaf", new StatMods(Evasion: 4)),
        GearSet("light", 52, "Nightleaf", new StatMods(PAtkPct: 0.02f, Dex: 3, Con: -2, Str: -1)),
        GearSet("light", 61, "Nightleaf", new StatMods(MeleeVamp: 0.05f, Dex: 1, Con: -1)),
        GearSet("light", 76, "Nightleaf", new StatMods(PAtkPct: 0.04f, AtkSpeedPct: 0.04f, MaxMp: 220,
            MpRegenPct: 0.05f, MDefPct: 0.05f, Dex: 1, Str: 1, Con: -2)),
        // Robe — "Arcanum"
        GearSet("robe", 20, "Arcanum", new StatMods(Wit: 1, MoveSpeed: 7)),
        GearSet("robe", 40, "Arcanum", new StatMods(CastSpeedPct: 0.15f)),
        GearSet("robe", 52, "Arcanum", new StatMods(CastSpeedPct: 0.15f, PDefPct: 0.05f, MDefPct: 0.05f)),
        GearSet("robe", 61, "Arcanum", new StatMods(Wit: 2, MoveSpeed: 7, CastSpeedPct: 0.15f,
            PDefPct: 0.08f, MDefPct: -0.05f, MpRegenPct: -0.05f)),
        GearSet("robe", 76, "Arcanum", new StatMods(Wit: 2, Int: 1, MAtkPct: 0.17f, MoveSpeed: 7,
            CastSpeedPct: 0.15f)),
    };

    /// <summary>The classic four-slot requirement used when a set doesn't override it.</summary>
    public static readonly ArmorSlot[] DefaultSlots =
        { ArmorSlot.Head, ArmorSlot.Body, ArmorSlot.Gloves, ArmorSlot.Boots };

    public static ArmorSetDef? Get(string id) =>
        string.IsNullOrEmpty(id) ? null : _byId.GetValueOrDefault(id);

    public static IEnumerable<ArmorSetDef> All => _byId.Values;
}
