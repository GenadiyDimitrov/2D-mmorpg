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
    // PRIMARY-stat deltas (Str/Agi/Con/…) are STORED but NOT yet applied — they need a derivation
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
    ArmorSlot[]? RequiredSlots = null,
    // ADDITIONAL bonus granted only when the set's own SHIELD is also equipped (a shield whose
    // SetId == this set's Id). Per the gear CSV: "Set bonus active only with all 4 of group, and
    // shield for additional bonus if available" — so the shield is a BONUS, never a requirement.
    StatMods ShieldBonus = default);

public static class ArmorSetCatalog
{
    // ----- Stable set ids -----
    public const string DarkDominion = "set_dark_dominion";
    // F grade's sets. The ids MUST match what the tiered generator emits for ItemLevel 1
    // (ItemCatalog.TieredArmor: "set_{weight}_t{level}" and "set_acc_t{level}") — a set is joined to
    // its pieces by nothing but this string, so a mismatch is a bonus that silently never applies.
    public const string FLightSet     = "set_light_t1";
    public const string FRobeSet      = "set_robe_t1";
    public const string FHeavySet     = "set_heavy_t1";
    public const string FAccessorySet = "set_acc_t1";

    // Retired ids — the newbie kit is the F-grade top now, so nothing carries these. Kept only so an
    // old save referencing one still resolves to a name rather than throwing.
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
        // ===== F GRADE ("Ferrite") — the old Newbie sets, now the F tier's own =====
        // These already existed as the Newbie sets; when the newbie kit became the F-grade top
        // (0.31.2) their IDS had to follow it onto the tiered pieces, or the bonus would have been
        // orphaned — defined, but attached to items that no longer exist. Same bonuses, same shape:
        // light is the defensive line, robe buys cast speed.
        //
        new ArmorSetDef(FLightSet, "Ferrite Leathers",
            new ClassFlatBonus(MaxHp: 42), DefencePct: 0.02f, AccessorySetId: FAccessorySet),
        new ArmorSetDef(FRobeSet, "Ferrite Robe",
            new ClassFlatBonus(), CastSpeedPct: 0.15f, AccessorySetId: FAccessorySet),
        // HEAVY at F had no newbie equivalent (the starter kit was fighter-light / mage-robe), but the
        // F tier does have a heavy body, so a tank in F would otherwise complete nothing. It mirrors
        // light — the defensive line, same numbers. One line to change if heavy should differ.
        new ArmorSetDef(FHeavySet, "Ferrite Bulwark",
            new ClassFlatBonus(MaxHp: 42), DefencePct: 0.02f, AccessorySetId: FAccessorySet),
    }.Concat(TieredSets()).SelectMany(QualityVariants).ToDictionary(s => s.Id);

    /// <summary>A set at every quality that can wear it — the authored one (MYTHIC) plus derived
    /// EPIC and LEGENDARY copies at 70% / 85%.
    ///
    /// THE POINT: a set must be completed by four pieces of the SAME quality (owner, 2026-07-30).
    /// Before this, every Epic/Legendary/Mythic copy of a piece carried the SAME set id, so a Mythic
    /// body + Epic helm + Legendary gloves + Epic boots completed the set — and paid the FULL Mythic
    /// bonus. Mixing was strictly better than matching, and the quality of an accessory did not matter
    /// at all as long as it was Epic or above. Now each quality has its own set id and its own scaled
    /// bonus, so four Mythics earn the Mythic set and four Epics earn the Epic one.
    ///
    /// Below Epic there is no set at all (those copies carry no set id) — see ItemCatalog.HasIdentity.</summary>
    private static IEnumerable<ArmorSetDef> QualityVariants(ArmorSetDef authored)
    {
        yield return authored;   // the authored numbers ARE the Mythic set
        foreach (var q in new[] { ItemRarity.Epic, ItemRarity.Legendary })
        {
            float f = ItemCatalog.RarityScale(q);
            string suffix = "_" + q.ToString().ToLowerInvariant();
            yield return authored with
            {
                Id = authored.Id + suffix,
                Name = $"{authored.Name} ({q})",
                Bonus = authored.Bonus.Scaled(f),
                Mods = authored.Mods.Scaled(f),
                DefencePct = authored.DefencePct * f,
                CastSpeedPct = authored.CastSpeedPct * f,
                ShieldBonus = authored.ShieldBonus.Scaled(f),
                // The accessory line must be quality-matched too, or a Mythic body would still be
                // completed by Epic accessories through the shared accessory id.
                AccessorySetId = string.IsNullOrEmpty(authored.AccessorySetId)
                    ? "" : authored.AccessorySetId + suffix,
            };
        }
    }

    // ----- Tiered gear sets (docs/data/gear/gear_sets.csv, BASE variant per weight/tier). Each body of a
    // tier + that tier's shared accessory line (set_acc_t{lv}) completes it. Bonuses are the FULL
    // StatMods; SECONDARY stats apply now, PRIMARY-stat deltas (Con/Str/…) are stored for the pre-pass.
    // The SHIELD-conditional extras (the CSV's "Shield: <bonus>" clause on the def-oriented heavy
    // sets) are live: see ShieldBonus + Entity.RecomputeDerived. -----
    private static ArmorSetDef GearSet(string weightKey, int level, string family, StatMods mods,
        StatMods shieldBonus = default) =>
        new($"set_{weightKey}_t{level}", $"{family} {ItemCatalog.TierLetter(level)}",
            new ClassFlatBonus(), Mods: mods, AccessorySetId: $"set_acc_t{level}",
            ShieldBonus: shieldBonus);

    private static ArmorSetDef GearVariant(string id, int level, string name, StatMods mods) =>
        new(id, name, new ClassFlatBonus(), Mods: mods, AccessorySetId: $"set_acc_t{level}");

    private static IEnumerable<ArmorSetDef> TieredSets() => new[]
    {
        // Heavy — "Ironforge". These are the sets the CSV gives a SHIELD-conditional extra to
        // (the def-oriented line: 20/40/52/61/76 — the *_dmg variants get none).
        GearSet("heavy", 20, "Ironforge", new StatMods(PDefPct: 0.05f, MaxHp: 135),
            shieldBonus: new StatMods(ShieldDefPct: 0.10f)),                     // shield.p.def x1.1
        GearSet("heavy", 40, "Ironforge", new StatMods(MaxHp: 270),
            shieldBonus: new StatMods(PDefPct: 0.05f)),                          // p.def x1.05
        GearSet("heavy", 52, "Ironforge", new StatMods(Con: 3, Str: 3),
            shieldBonus: new StatMods(ShieldDefPct: 0.25f)),                     // shield.p.def x1.25
        GearSet("heavy", 61, "Ironforge", new StatMods(PAtkPct: 0.04f, Con: 2, Agi: -2, CcResist: 0.4f),
            shieldBonus: new StatMods(Reflect: 0.05f)),                          // reflect 5% of melee basic
        GearSet("heavy", 76, "Ironforge", new StatMods(MaxHp: 455, Str: 2, Con: 2, Agi: -2, CcResist: 0.4f),
            shieldBonus: new StatMods(PDefPct: 0.05f, MDefPct: 0.05f, ShieldDefPct: 0.25f, Reflect: 0.05f)),
        // ===== S (level 80) — AUTHORED BY HIM 2026-08-11. The S row was "set bonus NOT authored yet"
        // in the CSV until now, so all three of these sets are NEW: an S body + S accessories completed
        // nothing at all before, which made the top grade the only one with no set identity. =====
        //
        // Heavy S is the tank line taken to its endgame shape: it is the FIRST set to carry crit rate
        // (+100 = +10 points flat, outside every multiplier), magic damage reduction, melee vamp and a
        // PvP damage-taken cut. Its shield clause repeats the PvP cut, so shield-up is ×0.95 × ×0.95.
        GearSet("heavy", 80, "Ironforge", new StatMods(Str: 3, Con: 2, Agi: -2, MaxHp: 550,
            CritRateFlat: 0.10f, CcResist: 0.4f, MagicResist: 0.02f, MeleeVamp: 0.02f,
            PvpDamageTakenPct: -0.05f),
            shieldBonus: new StatMods(PDefPct: 0.06f, MDefPct: 0.06f, ShieldDefPct: 0.30f, Reflect: 0.05f,
                PvpDamageTakenPct: -0.05f)),
        // Light — "Nightleaf"
        GearSet("light", 20, "Nightleaf", new StatMods(Evasion: 2, MaxMp: 92)),
        GearSet("light", 40, "Nightleaf", new StatMods(Evasion: 4)),
        GearSet("light", 52, "Nightleaf", new StatMods(PAtkPct: 0.02f, Agi: 3, Con: -2, Str: -1)),
        GearSet("light", 61, "Nightleaf", new StatMods(MeleeVamp: 0.05f, Agi: 1, Con: -1, CcResist: 0.4f)),
        GearSet("light", 76, "Nightleaf", new StatMods(PAtkPct: 0.04f, AtkSpeedPct: 0.04f, MaxMp: 220,
            Spt: 1, Agi: 1, Str: 1, Con: -2, CcResist: 0.4f)),
        // Light S — the A set's shape with everything nudged up, plus four channels light never had:
        // move speed ×1.03, flat crit rate +30 (= +3 points), flat crit DAMAGE +200 and evasion +3.
        // ⚠ The +200 crit damage is the largest single source in the game — 2H Weapon Mastery at level 5
        // is +106 — so a rogue in S Nightleaf roughly triples his flat crit-damage term. It is what he
        // authored; it is also the one number here most worth a BalanceMatrix pass before it ships.
        GearSet("light", 80, "Nightleaf", new StatMods(PAtkPct: 0.05f, AtkSpeedPct: 0.05f, MaxMp: 300,
            Spt: 1, Agi: 2, Str: 2, Con: -2, CcResist: 0.4f, MoveSpeedPct: 0.03f,
            CritRateFlat: 0.03f, CritDamageFlat: 200f, Evasion: 3, PvpDamageTakenPct: -0.05f)),
        // Robe — "Arcanum"
        GearSet("robe", 20, "Arcanum", new StatMods(Wit: 1, MoveSpeed: 7)),
        GearSet("robe", 40, "Arcanum", new StatMods(CastSpeedPct: 0.15f)),
        GearSet("robe", 52, "Arcanum", new StatMods(CastSpeedPct: 0.15f, PDefPct: 0.05f, MDefPct: 0.05f)),
        GearSet("robe", 61, "Arcanum", new StatMods(Wit: 2, MoveSpeed: 7, CastSpeedPct: 0.15f,
            PDefPct: 0.08f, Spt: -1, CcResist: 0.4f)),
        // ⚠ A (76) was RE-AUTHORED 2026-08-11: M.Atk ×1.17 -> ×1.10, and SPT −1 added. Both pull the
        // nuke set back — less magic damage AND less MP/regen — which is consistent with the F-rung
        // caster re-author from the same pass rather than a slip.
        GearSet("robe", 76, "Arcanum", new StatMods(Wit: 2, Int: 1, Spt: -1, MAtkPct: 0.10f, MoveSpeed: 7,
            CastSpeedPct: 0.15f, CcResist: 0.4f)),
        // Robe S — the A set with the ×1.17 M.Atk restored and INT +2 (A is +1). So the caster's magic
        // damage step from A to S is in the SET, not just the staff.
        GearSet("robe", 80, "Arcanum", new StatMods(Wit: 2, Int: 2, Spt: -1, MAtkPct: 0.17f, MoveSpeed: 7,
            CastSpeedPct: 0.15f, CcResist: 0.4f)),

        // Body VARIANTS (dmg/support/nuke lines). Same accessory line as the tier's base set.
        // Stun/Fear-resist lines on the *_dmg variants are deferred (need the CC-resist mechanic).
        GearVariant("set_heavy_t52_dmg", 52, "Ironforge C (Assault)",
            new StatMods(MoveSpeed: 7, HpRegenPct: 0.05f, Str: 3, Con: -2, Agi: -1)),
        GearVariant("set_heavy_t61_dmg", 61, "Ironforge B (Assault)",
            new StatMods(PAtkPct: 0.04f, Accuracy: 4, Str: 2, Con: -2, CcResist: 0.4f)),
        GearVariant("set_light_t40_pdef", 40, "Nightleaf D (Bulwark)", new StatMods(PDefPct: 0.05f)),
        GearVariant("set_light_t40_mdef", 40, "Nightleaf D (Warded)", new StatMods(MDefPct: 0.05f)),
        GearVariant("set_light_t40_str", 40, "Nightleaf D (Brawler)", new StatMods(Str: 4, Con: -1)),
        GearVariant("set_light_t52_sup", 52, "Nightleaf C (Sage)",
            new StatMods(CastSpeedPct: 0.15f, MaxMp: 120, Spt: 1, Wit: -1, Int: -1)),
        GearVariant("set_light_t61_dmg", 61, "Nightleaf B (Assault)",
            new StatMods(PAtkPct: 0.08f, Agi: 1, Con: -1, CcResist: 0.4f)),
        GearVariant("set_robe_t40_sup", 40, "Arcanum D (Warden)", new StatMods(PDefPct: 0.05f, Wit: 1)),
        GearVariant("set_robe_t40_nuke", 40, "Arcanum D (Destroyer)", new StatMods(Int: 4, Wit: -1)),
    };

    /// <summary>The classic four-slot requirement used when a set doesn't override it.</summary>
    public static readonly ArmorSlot[] DefaultSlots =
        { ArmorSlot.Head, ArmorSlot.Body, ArmorSlot.Gloves, ArmorSlot.Boots };

    public static ArmorSetDef? Get(string id) =>
        string.IsNullOrEmpty(id) ? null : _byId.GetValueOrDefault(id);

    public static IEnumerable<ArmorSetDef> All => _byId.Values;
}
