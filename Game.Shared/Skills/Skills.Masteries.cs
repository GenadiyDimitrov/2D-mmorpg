namespace Game.Shared;

/// <summary>DATA-DRIVEN armor-mastery skills per 2nd-class archetype (the healer's lives
/// in Skills.Healer.cs). Each carries a per-worn-weight <see cref="ArmorMasteryProfile"/>:
/// a BONUS for the trained weight(s), a PENALTY for off-weights (robe never penalises;
/// tank/warrior are immune so their off-weights are inert = default). These REPLACE the base
/// fighter/mage masteries; every stat is explicit per skill level — no character-level or
/// class formula (per [[stats-via-skills-not-hardcoded]]).</summary>
public static partial class SkillCatalog
{
    public const string TankArmorMastery    = "tank_armor_mastery";
    public const string WarriorArmorMastery = "warrior_armor_mastery";
    public const string RogueArmorMastery   = "rogue_armor_mastery";
    // (`archer_armor_mastery` — deleted 2026-08-07, playtest-19 `0a`/G1. Orphaned by the
    //  archer→rogue merge; a bow character wears the ROGUE light mastery. Don't re-add it.)
    public const string NukerArmorMastery   = "nuker_armor_mastery";

    /// <summary>Rogue armor level. The CSV splits in two, exactly like the warrior's:
    /// <c>with all</c> (MP regen + flat P.Def, and HP regen on the last rung) applies in EVERY
    /// weight, and <c>with light</c> (evasion, crit-rate resist, speed) only in LIGHT. Off-weights
    /// keep the "with all" half and simply miss the light half — still no active penalty for a
    /// fighter (owner ruling 2026-07-01). Everything used to be gated on light, which left a
    /// rogue in robe/heavy with no MP regen, no HP regen and no P.Def at all.</summary>
    /// ⚠ <paramref name="lightSpeed"/> is FLAT run speed, not a percentage — the CSV reads
    /// "speed +7" and he corrected it explicitly in playtest-20 ("Also speed is +7 flat not
    /// x1.07"). It was authored as MoveSpeedPct 0.06, which is a different number at every base
    /// speed and drifts as the SpeedTable changes.
    private static ArmorMasteryProfile RogueArmor(StatMods all, int lightEva, float lightSpeed = 0f) =>
        new(Robe: all, Heavy: all,
            Light: all with { Evasion = lightEva, CritRateResist = 0.15f, MoveSpeed = lightSpeed });

    /// <summary>Tank Heavy Armor Mastery level: HEAVY armor grants flat P.Def, ×1.07 P.Def,
    /// 15% crit-damage reduction, ×mpReg MP regen and −2 evasion. Off-weights are inert (tank is
    /// immune to armor penalties). (CSV tank "heavy: mpReg x1.1, p.def +N, p.def x1.07, crit dmg
    /// reduction 15%, eva -2"; the @36 level is mpReg ×3.4.)</summary>
    private static ArmorMasteryProfile TankHeavy(int def, float mpReg = 1.1f) => new(
        Robe:  default,
        Light: default,
        Heavy: new StatMods(MpRegenPct: mpReg - 1f, PDef: def, PDefPct: 0.07f,
            CritDmgResist: 0.15f, Evasion: -2));

    /// <summary>Warrior armor-mastery level: flat P.Def + ×1.1 MP regen on all weights; light
    /// armor also adds the given evasion. (CSV warrior "with all mp[Reg] x1.1, p.def +N; light eva +E".)</summary>
    private static ArmorMasteryProfile WarriorArmor(int def, int lightEva) => new(
        Robe:  new StatMods(PDef: def, MpRegenPct: 0.1f),
        Light: new StatMods(PDef: def, MpRegenPct: 0.1f, Evasion: lightEva),
        Heavy: new StatMods(PDef: def, MpRegenPct: 0.1f));

    private static SkillDef ArmorMasteryPassive(string id, BaseClass cls, ArmorMasteryProfile profile) =>
        new(id, "Armor Mastery", cls, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { FighterArmorMastery },
            Description: "Passive. Adapts your defences to the armor weight you wear — your "
                       + "trained weight grants a bonus; wearing the wrong weight hinders you.",
            Levels: new[] { new SkillLevel(SpCost: 500) },
            ArmorMasteryLevels: new[] { profile });

    private static SkillDef[] ArmorMasterySkills() => new SkillDef[]
    {
        // Tank — Heavy Armor Mastery (CSV tank 2nd): in HEAVY armor, big flat P.Def plus
        // ×1.07 P.Def, 15% crit-damage reduction and ×1.1 max MP, at a small evasion cost.
        // 5 levels (@20/24/28/32/36). Immune to off-weight penalties (Neutral otherwise).
        new(TankArmorMastery, "Heavy Armor Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { FighterArmorMastery },
            Description: "Passive. In HEAVY armor: greatly increased physical defence, reduced "
                       + "critical damage taken and more max MP (slightly lower evasion).",
            Levels: new[]
            {
                new SkillLevel(SpCost: 1700),
                new SkillLevel(SpCost: 3200),
                new SkillLevel(SpCost: 6000),
                new SkillLevel(SpCost: 11000),
                new SkillLevel(SpCost: 20000),
            },
            ArmorMasteryLevels: new[]
            {
                TankHeavy(40), TankHeavy(47), TankHeavy(54), TankHeavy(61), TankHeavy(70, mpReg: 3.4f),
            }),

        // Warrior — Armor Mastery (CSV warrior 2nd): +P.Def and +max MP with any weight;
        // LIGHT armor additionally boosts evasion. Continues the base fighter mastery (which it
        // replaces) with 5 levels (@20/24/28/32/36).
        new(WarriorArmorMastery, "Armor Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { FighterArmorMastery },
            Description: "Passive. Improves defence and maximum MP with any armor weight; "
                       + "light armor also boosts evasion.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 1700),
                new SkillLevel(SpCost: 3200),
                new SkillLevel(SpCost: 6000),
                new SkillLevel(SpCost: 11000),
                new SkillLevel(SpCost: 20000),
            },
            ArmorMasteryLevels: new[]
            {
                WarriorArmor(19, 6), WarriorArmor(21, 8), WarriorArmor(23, 9),
                WarriorArmor(28, 9), WarriorArmor(32, 9),
            }),

        // Rogue — Armor Mastery (CSV rogue 2nd): "with all" = ×1.1 MP regen + flat P.Def (at L5
        // ×1.8 MP regen and ×1.2 HP regen); "with light" adds big evasion, +15% crit-rate resist
        // and (from L3) move speed. 5 levels (@20/24/28/32/36). Replaces the base fighter mastery.
        new(RogueArmorMastery, "Armor Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { FighterArmorMastery },
            Description: "Passive. Improves defence and MP regeneration with any armor weight; "
                       + "in LIGHT armor it also grants greatly increased evasion, resistance to "
                       + "critical hits and (at higher levels) speed.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 1700),
                new SkillLevel(SpCost: 3200),
                new SkillLevel(SpCost: 6000),
                new SkillLevel(SpCost: 11000),
                new SkillLevel(SpCost: 20000),
            },
            ArmorMasteryLevels: new[]
            {
                RogueArmor(new StatMods(MpRegenPct: 0.1f, PDef: 16), lightEva: 7),
                RogueArmor(new StatMods(MpRegenPct: 0.1f, PDef: 18), lightEva: 11),
                RogueArmor(new StatMods(MpRegenPct: 0.1f, PDef: 20), lightEva: 13, lightSpeed: 7f),
                RogueArmor(new StatMods(MpRegenPct: 0.1f, PDef: 22), lightEva: 13, lightSpeed: 7f),
                RogueArmor(new StatMods(MpRegenPct: 0.8f, HpRegenPct: 0.2f, PDef: 25), lightEva: 13, lightSpeed: 7f),
            }),

        // (Archer Armor Mastery DELETED 2026-08-07 with its id — the rogue light mastery above is
        //  what a bow character wears since the merge.)

        // Nuker — Mage Armor Mastery (CSV nuker 2nd): in ROBE, +MP regen, +P.Def, +max MP
        // and a "mpWhenRestored" bonus (extra MP each time Restore Spirit lands). Light/Heavy
        // penalise casting (mage). 4 levels (@20/25/30/35). Replaces the base Robe/Light mastery.
        new(NukerArmorMastery, "Mage Armor Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { MasteryRobe },
            Description: "Passive. In ROBE: faster MP regen, more defence and max MP, and extra "
                       + "MP from your own MP-restoration. Light/heavy armor slows your casting.",
            Levels: new[]
            {
                // 🔑 A MASTERY'S SP IS THE LEVEL'S SP, nothing else (owner, 2026-08-19: *"armor and
                // spell masteries should fallow the lvl SP requirement"* — *"i dont know why they were
                // so overinflated"*). The 20-35 band is 3200 / 6400 / 12800 / 25000, the same schedule
                // the cleric's Armor Mastery and both Spell Masteries run. The first two rungs were
                // 9600 / 12800, which made a passive the most expensive thing a level-20 nuker could buy.
                new SkillLevel(SpCost: 3200),
                new SkillLevel(SpCost: 6400),
                new SkillLevel(SpCost: 12800),
                new SkillLevel(SpCost: 25000),
                // Rungs 5-18 (@40 → 74) — HIS, from `nuker 3rd.csv` (2026-08-26). They replace the four
                // we invented at 40/50/60/70, and the SP is now the 3rd tier's own band ladder, the same
                // one the healer and buffer files run on: 36k → 880k. The old "no authored level→SP
                // table above 35" caveat is dead — there is one, and this is it.
                new SkillLevel(SpCost: 36000),  new SkillLevel(SpCost: 43000),
                new SkillLevel(SpCost: 64000),  new SkillLevel(SpCost: 74000),
                new SkillLevel(SpCost: 81000),  new SkillLevel(SpCost: 88000),
                new SkillLevel(SpCost: 120000), new SkillLevel(SpCost: 170000),
                new SkillLevel(SpCost: 190000), new SkillLevel(SpCost: 280000),
                new SkillLevel(SpCost: 320000), new SkillLevel(SpCost: 390000),
                new SkillLevel(SpCost: 650000), new SkillLevel(SpCost: 880000),
            },
            ArmorMasteryLevels: new[]
            {
                // 🔑 mpWhenRestored IS A PERCENT since 2026-08-19 (owner) — "+10%" means any MP
                // restore landing on this character is multiplied by 1.10. It was a flat "+N MP per
                // restore"; see StatMods.RestoreMpPct for why flat could not survive mana-over-time.
                //
                // ⚠ RUNGS 1-4 MIRROR THE AUTHORED CSV (`docs/data/classes_skills_csv/nuker 2nd.csv`)
                // and he RE-AUTHORED THEM ON 2026-08-24: **10 / 15 / 20 / 25%** at character
                // 20/25/30/35. They were 19/23/26/30, which was never his number — it was the
                // 2026-08-19 flat→percent conversion (old flat × 0.75) written into his file by us.
                // He has replaced it with a round 5-point ladder that is a good deal smaller, in
                // exactly the band where Restore Spirit's own number is smallest: an early nuker's
                // mana now comes from the SKILL, not from the robe. DO NOT RETUNE THESE HERE; that
                // file is the owner's source of truth for the whole 20-35 band.
                NukerRobe(pDef: 20, maxMp: 20, restorePct: 0.10f),
                NukerRobe(pDef: 25, maxMp: 20, restorePct: 0.15f),
                NukerRobe(pDef: 30, maxMp: 30, restorePct: 0.20f),
                NukerRobe(pDef: 35, maxMp: 30, restorePct: 0.25f),
                // ⚠ RUNGS 5-18 ARE HIS — `nuker 3rd.csv`, 2026-08-26 — and they replace the four we
                // invented at 40/50/60/70 with the note "when the 40+ nuker CSV lands, these four rungs
                // are the ones to replace". It landed.
                //
                // 🔑 P.DEF AND MAX MP NOW GROW, which the invented rungs deliberately refused to do
                // (frozen at rung 4 rather than inventing defensive growth he never authored). His file
                // authors both: 39 → 87 P.Def and 70 → 200 Max MP across the fourteen bands.
                //
                // 🔑 mpWhenRestored REACHES 60% AT 70 AND THEN PLATEAUS — his last three rows all read
                // 60%. That is the same endpoint the invented rung 8 carried, arrived at from his own
                // ladder instead of from our extrapolation, and it is his "+200 MP for −200 HP" late-game
                // balance against Restore Spirit. A plateau at the TOP of a ladder is authored, not a
                // typo (owner, 2026-08-26) — do not "fix" it upward.
                //
                // ⚠ These are ALMOST the healer's robe rungs and are NOT interchangeable with them: his
                // @48 row is 50 P.Def where the healer's is 47, and the healer's robe carries no restore
                // bonus at all. That single difference is why the nuker keeps its own skill id.
                NukerRobe(pDef: 39, maxMp:  70, restorePct: 0.33f),   // 40
                NukerRobe(pDef: 44, maxMp:  70, restorePct: 0.35f),   // 44
                NukerRobe(pDef: 50, maxMp: 100, restorePct: 0.37f),   // 48
                NukerRobe(pDef: 50, maxMp: 100, restorePct: 0.40f),   // 52
                NukerRobe(pDef: 53, maxMp: 140, restorePct: 0.43f),   // 56
                NukerRobe(pDef: 56, maxMp: 140, restorePct: 0.45f),   // 58
                NukerRobe(pDef: 58, maxMp: 150, restorePct: 0.47f),   // 60
                NukerRobe(pDef: 64, maxMp: 150, restorePct: 0.50f),   // 62
                NukerRobe(pDef: 68, maxMp: 150, restorePct: 0.53f),   // 64
                NukerRobe(pDef: 72, maxMp: 180, restorePct: 0.55f),   // 66
                NukerRobe(pDef: 75, maxMp: 180, restorePct: 0.57f),   // 68
                NukerRobe(pDef: 79, maxMp: 180, restorePct: 0.60f),   // 70
                NukerRobe(pDef: 83, maxMp: 200, restorePct: 0.60f),   // 72
                NukerRobe(pDef: 87, maxMp: 200, restorePct: 0.60f),   // 74
            }),
    };

    /// <summary>Nuker robe-mastery level: ROBE gets +MP regen, flat P.Def, flat max MP and the
    /// mpWhenRestored bonus. (CSV nuker "Robe: mpReg x1.2, pDef +N, maxMP +M, mpWhenRestored +R%".)
    /// ⚠ <paramref name="restorePct"/> is a FRACTION (0.60 = +60% MP from every restore), not the
    /// flat "+60 MP" it was until 2026-08-19 — see StatMods.RestoreMpPct.
    /// ⚠ 2026-08-07: the off-weight cast/attack penalty is GONE from here. It belongs to Spellcaster
    /// Mastery, which is never replaced — so this skill is pure bonus and the two now STACK (its
    /// ×1.2 MP regen multiplies the Spellcaster robe ×1.2, which is the owner's intent: *"giving
    /// him x1.2 mp regen (now stacks with the SpellcasterMastery)"*). Duplicating the penalty here
    /// would have applied it TWICE once masteries began stacking.</summary>
    private static ArmorMasteryProfile NukerRobe(int pDef, int maxMp, float restorePct) => new(
        Robe: new StatMods(MpRegenPct: 0.2f, PDef: pDef, MaxMp: maxMp, RestoreMpPct: restorePct));
}
