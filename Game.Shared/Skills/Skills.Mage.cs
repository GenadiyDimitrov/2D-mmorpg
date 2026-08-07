namespace Game.Shared;

/// <summary>Base Mage kit — nukes, the basic heal, and the def-curse line,
/// available to all mages (the nuker/healer upgrades that replace the basics live
/// here too; 3rd-class discipline spells are in their own Skills.&lt;Discipline&gt;.cs).</summary>
public static partial class SkillCatalog
{
    public const string MagicBolt = "magic_bolt";
    public const string Heal = "heal";
    public const string SelfHeal = "self_heal";
    public const string Might = "might";
    public const string MageAntiMagic = "anti_magic_mage";
    public const string VampiricBolt = "vampiric_bolt";
    public const string WeaponMastery = "weapon_mastery";
    public const string Weakness = "weakness";
    public const string GreaterWeakness = "greater_weakness";
    // (`greater_heal` — deleted 2026-08-07 with the God layer, playtest-19 `0b`.)
    public const string FlameBolt = "flame_bolt";
    public const string HolyStrike = "holy_strike";
    public const string ElementalBurst = "elemental_burst";   // nuker 3rd-class ultimate (consumes Elemental Stones)
    public const string FrostBind = "frost_bind";             // nuker CC — magical Slow (first contested-CC skill)
    public const string EntanglingRoots = "entangling_roots"; // nuker CC — magical Root (contested)
    public const string GlacialSpike = "glacial_spike";       // nuke with +dmg vs slowed/rooted
    public const string CreepingFrost = "creeping_frost";     // stacking slow (10/20/30% over 3)
    // (`dispel_magic` — deleted 2026-08-07, playtest-19 `0a`/G1: on no class table, learnable by
    //  nobody. SkillEffect.Cancel / DispelCount remain in the engine for a future authored skill.)
    public const string ManaBarrier = "mana_barrier";         // mana shield (damage→MP)
    public const string PhaseShift = "phase_shift";           // blink away from target (escape)
    // --- Nuker 2nd-class (CSV nuker 20-35) ---
    public const string ElementalBolt = "elemental_bolt";     // nuker basic nuke (replaces Magic Bolt)
    public const string QuickBolt = "quick_bolt";             // short-range fast nuke
    public const string RestoreSpirit = "restore_spirit";     // trades HP for MP (self)

    private static SkillDef[] MageSkills() => new SkillDef[]
    {
        // Magic Bolt — the starter nuke, 3 levels (auto-learn Lv.1; Lv.2/3 learned).
        new(MagicBolt, "Magic Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 9, CastTicks: 40, CooldownTicks: 10, Range: 600, Power: 12,
            Category: SkillCategory.Magic, InitialMpCost: 2,
            Description: "Hurls a bolt of force. Spells fail rather than miss.",
            Levels: new[]
            {                                                                              // learn level
                new SkillLevel(Power: 12, MpCost: 9,  InitialMpCost: 2, SpCost: 0,    Description: "Magic damage, power 12."),   // 1
                new SkillLevel(Power: 17, MpCost: 12, InitialMpCost: 2, SpCost: 480,  Description: "Magic damage, power 17."),   // 5
                new SkillLevel(Power: 24, MpCost: 17, InitialMpCost: 3, SpCost: 2200, Description: "Magic damage, power 24."),   // 10
            }),

        // Self Heal — the base MAGE heal: SELF ONLY, 3 levels (1/7/14). The nuker keeps this
        // (self-only) so a high-M.Atk nuker can't spam-heal the party; the HEALER replaces it
        // with the targeted Heal at level 20.
        new(SelfHeal, "Self Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 7, CastTicks: 50, CooldownTicks: 20, Range: 0, Power: 42,
            Category: SkillCategory.Heal, InitialMpCost: 2,
            TargetMode: TargetMode.SelfOnly,
            Description: "Restores your own HP. Scales with WIT.",
            Levels: new[]
            {
                new SkillLevel(Power: 42,  MpCost: 7,  InitialMpCost: 2, SpCost: 160,  Description: "Self heal power 42."),
                new SkillLevel(Power: 67,  MpCost: 14, InitialMpCost: 3, SpCost: 480,  Description: "Self heal power 67."),
                new SkillLevel(Power: 107, MpCost: 22, InitialMpCost: 5, SpCost: 2200, Description: "Self heal power 107."),
            }),

        // Heal — the HEALER's targeted heal (ally or self); REPLACES Self Heal at level 20.
        // 4 levels @20/25/30/35 (base-mage no longer learns this).
        new(Heal, "Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 30, CastTicks: 50, CooldownTicks: 20, Range: 600, Power: 151,
            Category: SkillCategory.Heal, InitialMpCost: 6,
            Replaces: new[] { SelfHeal },
            Description: "Restores a friendly target's HP (or your own). Scales with WIT.",
            Levels: new[]
            {
                new SkillLevel(Power: 151, MpCost: 30, InitialMpCost: 6,  SpCost: 3200,  Description: "Heal power 151."),
                new SkillLevel(Power: 195, MpCost: 38, InitialMpCost: 8,  SpCost: 6400,  Description: "Heal power 195."),
                new SkillLevel(Power: 245, MpCost: 44, InitialMpCost: 9,  SpCost: 12800, Description: "Heal power 245."),
                new SkillLevel(Power: 301, MpCost: 52, InitialMpCost: 11, SpCost: 25000, Description: "Heal power 301."),
            }),

        // Might and Bulwark — the P.Atk / P.Def blessing, now a GROUP: it applies no buff of its
        // own, only children off the atk_phys / def_phys / vamp / accuracy ladders, so a Might
        // potion competes with the Might part alone and leaves the rest of the blessing standing.
        // Levels 1-4 are the SAME numbers this buff has always cast (8/8 → 12/12 + 6% vamp);
        // 5-6 climb to the NPC buffer's max.
        // ⚠ NOBODY LEARNS THIS BELOW 74 ANY MORE (owner 2026-07-31). The base mage and the cleric
        // learn the INDIVIDUAL buffs (`cast_atk_phys`, `cast_def_phys`, …) at 30-50 MP; the group
        // is the Warchanter's, at 150-200 MP — five effects in one cast is what a buffer class buys.
        // ⚠ One real change: the old buff used BuffAtk, which raised BOTH channels — a mage's
        // M.Atk rode along on a *physical* blessing. The Might family is P.Atk only; M.Atk has
        // its own family (Force) and its own potion. See docs/design/BuffLadders.md.
        new(Might, "Might and Bulwark", BaseClass.Mage, SkillEffect.BuffPhysAtk | SkillEffect.BuffDef,
            MpCost: 150, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "mage_might", Rank: 1, InitialMpCost: 30,
            ChildBuffs: new[] { BuffPAtk1, BuffPDef1 },
            Category: SkillCategory.Buff, SpCost: 960,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { CastId(FamPhysAtk), CastId(FamPhysDef), CastId(FamVamp), CastId(FamAccuracy) },
            Description: "Blesses you and nearby allies with +P.Atk and +P.Def for 20 minutes.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 150, InitialMpCost: 30, SpCost: 960,
                    ChildBuffs: new[] { BuffPAtk1, BuffPDef1 },
                    Description: "+8% P.Atk and +8% P.Def for 20 minutes."),
                new SkillLevel(MpCost: 160, InitialMpCost: 32, SpCost: 3200,
                    ChildBuffs: new[] { BuffPAtk2, BuffPDef1 },
                    Description: "+12% P.Atk and +8% P.Def for 20 minutes."),
                new SkillLevel(MpCost: 170, InitialMpCost: 34, SpCost: 6400,
                    ChildBuffs: new[] { BuffPAtk2, BuffPDef2 },
                    Description: "+12% P.Atk and +12% P.Def for 20 minutes."),
                new SkillLevel(MpCost: 180, InitialMpCost: 36, SpCost: 12800,
                    ChildBuffs: new[] { BuffPAtk2, BuffPDef2, BuffVamp2 },
                    Description: "+12% P.Atk, +12% P.Def, and 6% melee-attack vampirism for 20 minutes."),
                new SkillLevel(MpCost: 190, InitialMpCost: 38, SpCost: 25000,
                    ChildBuffs: new[] { BuffPAtk3, BuffPDef3, BuffVamp2, BuffAcc2 },
                    Description: "+15% P.Atk, +15% P.Def, 6% melee vampirism, +2 Accuracy."),
                new SkillLevel(MpCost: 200, InitialMpCost: 40, SpCost: 50000,
                    ChildBuffs: new[] { BuffPAtk3, BuffPDef3, BuffVamp3, BuffAcc3 },
                    Description: "+15% P.Atk, +15% P.Def, 9% melee vampirism, +4 Accuracy."),
            }),

        // Anti-Magic — learnable mage passive: +M.Def and a magic-fail (fizzle) floor.
        // Lvls 1-2 = base mage; the Healer CONTINUES it at lvls 3-6 (20/25/30/35). The
        // CSV "mRes %" is modelled as the fizzle floor (the resolver takes the max floor).
        new(MageAntiMagic, "Anti-Magic", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Hardens you against hostile magic.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 480,   Passive: new PassiveEffect(MagicDefence: 12), Description: "+12 magic defence."),
                new SkillLevel(SpCost: 2200,  Passive: new PassiveEffect(MagicDefence: 16, MagicFailFloor: 0.05f),
                    Description: "+16 magic defence and a 5% chance for spells to fizzle on you."),
                new SkillLevel(SpCost: 3200,  Passive: new PassiveEffect(MagicDefence: 20, MagicFailFloor: 0.05f),
                    Description: "+20 magic defence; spells fizzle on you at least 5% of the time."),
                new SkillLevel(SpCost: 6400,  Passive: new PassiveEffect(MagicDefence: 25, MagicFailFloor: 0.05f),
                    Description: "+25 magic defence; spells fizzle on you at least 5% of the time."),
                new SkillLevel(SpCost: 12800, Passive: new PassiveEffect(MagicDefence: 30, MagicFailFloor: 0.10f),
                    Description: "+30 magic defence; spells fizzle on you at least 10% of the time."),
                new SkillLevel(SpCost: 25000, Passive: new PassiveEffect(MagicDefence: 36, MagicFailFloor: 0.10f),
                    Description: "+36 magic defence; spells fizzle on you at least 10% of the time."),
            }),

        // Vampiric Bolt — magic nuke that heals the caster for 40% of damage dealt. Level 1 is
        // the base-mage skill (@14); the Nuker CONTINUES it at levels 2-5 (@20/25/30/35).
        new(VampiricBolt, "Vampiric Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 28, CastTicks: 40, CooldownTicks: 10, Range: 750, Power: 21,
            Category: SkillCategory.Magic, InitialMpCost: 6, SpCost: 2200, Lifesteal: 0.40f,
            Description: "A draining bolt that heals you for 40% of the damage dealt.",
            Levels: new[]
            {                                                                                     // learn level
                new SkillLevel(Power: 29,  MpCost: 40,  InitialMpCost: 8,  SpCost: 2200,   Description: "Drain power 29; heals 40% of damage."),   // 14
                new SkillLevel(Power: 37,  MpCost: 54,  InitialMpCost: 11, SpCost: 3200,   Description: "Drain power 37; heals 40% of damage."),   // 20
                new SkillLevel(Power: 44,  MpCost: 64,  InitialMpCost: 13, SpCost: 6400,   Description: "Drain power 44; heals 40% of damage."),   // 25
                new SkillLevel(Power: 50,  MpCost: 72,  InitialMpCost: 14, SpCost: 12800,  Description: "Drain power 50; heals 40% of damage."),   // 30
                new SkillLevel(Power: 57,  MpCost: 82,  InitialMpCost: 16, SpCost: 25000,  Description: "Drain power 57; heals 40% of damage."),   // 35
                new SkillLevel(Power: 63,  MpCost: 90,  InitialMpCost: 18, SpCost: 40000,  Description: "Drain power 63; heals 40% of damage."),   // 40
                new SkillLevel(Power: 70,  MpCost: 100, InitialMpCost: 20, SpCost: 60000,  Description: "Drain power 70; heals 40% of damage."),   // 45
                new SkillLevel(Power: 76,  MpCost: 110, InitialMpCost: 22, SpCost: 85000,  Description: "Drain power 76; heals 40% of damage."),   // 50
                new SkillLevel(Power: 83,  MpCost: 120, InitialMpCost: 24, SpCost: 115000, Description: "Drain power 83; heals 40% of damage."),   // 55
                new SkillLevel(Power: 90,  MpCost: 130, InitialMpCost: 26, SpCost: 150000, Description: "Drain power 90; heals 40% of damage."),   // 60
                new SkillLevel(Power: 96,  MpCost: 138, InitialMpCost: 28, SpCost: 190000, Description: "Drain power 96; heals 40% of damage."),   // 65
                new SkillLevel(Power: 103, MpCost: 148, InitialMpCost: 30, SpCost: 235000, Description: "Drain power 103; heals 40% of damage."),  // 70
                new SkillLevel(Power: 109, MpCost: 158, InitialMpCost: 32, SpCost: 285000, Description: "Drain power 109; heals 40% of damage."),  // 75
                new SkillLevel(Power: 116, MpCost: 168, InitialMpCost: 34, SpCost: 340000, Description: "Drain power 116; heals 40% of damage."),  // 80
            }),

        // Elemental Bolt — the Nuker's MAIN nuke (replaces Magic Bolt). 13 levels, learned
        // every 5 levels from 20 to 80.
        //
        // The power ladder is L2's own nuke curve: linear in character level, anchored at
        // POWER 108 @ LEVEL 74 (L2's Hurricane / Hydro Blast / Death Spike / Prominence).
        // It used to stop at 4 levels and power 44 — which is why a level-85 mage still fought
        // with a level-35 spell and needed ~79 casts to kill a same-level mob, and why he hit
        // a tank for ~100 instead of the ~350 he should. This ladder IS the mage's scaling;
        // don't cap it at the 2nd class (in L2 your main nuke keeps gaining levels for life).
        new(ElementalBolt, "Elemental Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 27, CastTicks: 40, CooldownTicks: 10, Range: 750, Power: 37,
            Replaces: new[] { MagicBolt },
            Category: SkillCategory.Magic, InitialMpCost: 5,
            Description: "A bolt of raw elemental force — the Nuker's basic attack (replaces Magic Bolt).",
            Levels: new[]
            {                                                                                    // learn level
                new SkillLevel(Power: 37,  MpCost: 27, InitialMpCost: 5,  SpCost: 3200,   Description: "Magic damage, power 37."),   // 20
                new SkillLevel(Power: 44,  MpCost: 32, InitialMpCost: 6,  SpCost: 6400,   Description: "Magic damage, power 44."),   // 25
                new SkillLevel(Power: 50,  MpCost: 36, InitialMpCost: 7,  SpCost: 12800,  Description: "Magic damage, power 50."),   // 30
                new SkillLevel(Power: 57,  MpCost: 41, InitialMpCost: 8,  SpCost: 25000,  Description: "Magic damage, power 57."),   // 35
                new SkillLevel(Power: 63,  MpCost: 45, InitialMpCost: 9,  SpCost: 40000,  Description: "Magic damage, power 63."),   // 40
                new SkillLevel(Power: 70,  MpCost: 50, InitialMpCost: 10, SpCost: 60000,  Description: "Magic damage, power 70."),   // 45
                new SkillLevel(Power: 76,  MpCost: 55, InitialMpCost: 11, SpCost: 85000,  Description: "Magic damage, power 76."),   // 50
                new SkillLevel(Power: 83,  MpCost: 60, InitialMpCost: 12, SpCost: 115000, Description: "Magic damage, power 83."),   // 55
                new SkillLevel(Power: 90,  MpCost: 65, InitialMpCost: 13, SpCost: 150000, Description: "Magic damage, power 90."),   // 60
                new SkillLevel(Power: 96,  MpCost: 69, InitialMpCost: 14, SpCost: 190000, Description: "Magic damage, power 96."),   // 65
                new SkillLevel(Power: 103, MpCost: 74, InitialMpCost: 15, SpCost: 235000, Description: "Magic damage, power 103."),  // 70  (74 ≈ 108)
                new SkillLevel(Power: 109, MpCost: 79, InitialMpCost: 16, SpCost: 285000, Description: "Magic damage, power 109."),  // 75
                new SkillLevel(Power: 116, MpCost: 84, InitialMpCost: 17, SpCost: 340000, Description: "Magic damage, power 116."),  // 80
            }),

        // Quick Bolt — a short-range (150), fast (1.5s) nuke for weaving between casts.
        // Same 13-level ladder as Elemental Bolt at ~80% of its power (it trades damage for
        // cast time), same MP — the point is casts-per-second, not damage-per-cast.
        new(QuickBolt, "Quick Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 27, CastTicks: 15, CooldownTicks: 10, Range: 150, Power: 30,
            Category: SkillCategory.Magic, InitialMpCost: 5,
            Description: "A fast, close-range bolt (1.5s cast).",
            Levels: new[]
            {                                                                                    // learn level
                new SkillLevel(Power: 30, MpCost: 27, InitialMpCost: 5,  SpCost: 3200,   Description: "Magic damage, power 30."),   // 20
                new SkillLevel(Power: 35, MpCost: 32, InitialMpCost: 6,  SpCost: 6400,   Description: "Magic damage, power 35."),   // 25
                new SkillLevel(Power: 40, MpCost: 36, InitialMpCost: 7,  SpCost: 12800,  Description: "Magic damage, power 40."),   // 30
                new SkillLevel(Power: 46, MpCost: 41, InitialMpCost: 8,  SpCost: 25000,  Description: "Magic damage, power 46."),   // 35
                new SkillLevel(Power: 50, MpCost: 45, InitialMpCost: 9,  SpCost: 40000,  Description: "Magic damage, power 50."),   // 40
                new SkillLevel(Power: 56, MpCost: 50, InitialMpCost: 10, SpCost: 60000,  Description: "Magic damage, power 56."),   // 45
                new SkillLevel(Power: 61, MpCost: 55, InitialMpCost: 11, SpCost: 85000,  Description: "Magic damage, power 61."),   // 50
                new SkillLevel(Power: 66, MpCost: 60, InitialMpCost: 12, SpCost: 115000, Description: "Magic damage, power 66."),   // 55
                new SkillLevel(Power: 72, MpCost: 65, InitialMpCost: 13, SpCost: 150000, Description: "Magic damage, power 72."),   // 60
                new SkillLevel(Power: 77, MpCost: 69, InitialMpCost: 14, SpCost: 190000, Description: "Magic damage, power 77."),   // 65
                new SkillLevel(Power: 82, MpCost: 74, InitialMpCost: 15, SpCost: 235000, Description: "Magic damage, power 82."),   // 70
                new SkillLevel(Power: 87, MpCost: 79, InitialMpCost: 16, SpCost: 285000, Description: "Magic damage, power 87."),   // 75
                new SkillLevel(Power: 93, MpCost: 84, InitialMpCost: 17, SpCost: 340000, Description: "Magic damage, power 93."),   // 80
            }),

        // Restore Spirit — trades 65 HP for 20 MP (self; boosted by the nuker robe mastery's
        // "mpWhenRestored", +25/30/35/40). Costs HP, not MP. Single level @25.
        // ⚠ The HP price was 130 until 2026-08-07 (owner: "lower its hp consumption to half"). It was
        // priced against a bonus that was never landing — the base Robe Mastery was silently winning
        // the armor-mastery pick, so the skill really did return a flat 20 MP for 130 HP. With the
        // pick fixed it returns 45-60, and at 65 HP the trade is ~1.2 HP per MP instead of 6.5.
        new(RestoreSpirit, "Restore Spirit", BaseClass.Mage, SkillEffect.RestoreMp,
            MpCost: 0, CastTicks: 40, CooldownTicks: 50, Range: 0, Power: 20,
            Category: SkillCategory.Heal, TargetMode: TargetMode.SelfOnly, SpCost: 6400,
            HpCost: 65,
            Description: "Burns 65 HP to restore 20 MP to yourself (much more with robe mastery)."),

        // Weapon Mastery — flat attack passive (asymmetric: more M.Atk than P.Atk).
        // Also carries the caster bow penalty (half cast speed while wielding a bow).
        new(WeaponMastery, "Weapon Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 2200,
            WeaponMasteryLevels: new[] { CasterMastery(new PassiveEffect(MagAtk: 4, PhysAtk: 2)) },
            Description: "Passive. With a sword or blunt: +4 M.Atk and +2 P.Atk. Casting with "
                       + "anything else (bow, dagger, or bare-handed) is half speed."),

        // (Dispel Magic DELETED 2026-08-07, playtest-19 `0a`/G1 — it was on no class table, so it
        //  was in the catalog and learnable by nobody. The Cancel EFFECT and DispelCount stay in the
        //  engine; a real cancel skill can be authored onto a class list whenever one is wanted.)

        // Mana Barrier — MANA SHIELD: while up, 70% of incoming damage is paid from MP instead
        // of HP, at 0.5 MP per 1 damage (until MP runs out). Self, 30s.
        new(ManaBarrier, "Mana Barrier", BaseClass.Mage, SkillEffect.ManaShield,
            MpCost: 30, CastTicks: 0, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "mana_barrier", Rank: 1, TargetMode: TargetMode.SelfOnly,
            Category: SkillCategory.Buff,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.ManaShield, 0.70f, ModifierMode.Percent),  // 70% of damage diverted
                new(SkillEffect.ManaShield, 0.5f,  ModifierMode.Flat),     // 0.5 MP per 1 damage
            },
            Description: "Diverts 70% of incoming damage to MP (0.5 MP per damage) for 30s, while MP lasts."),

        // Phase Shift — BLINK back 400 (escape). No target needed: jumps away from the nearest
        // enemy. Tempest kite tool.
        new(PhaseShift, "Phase Shift", BaseClass.Mage, SkillEffect.Blink,
            MpCost: 20, CastTicks: 0, CooldownTicks: 80, Range: 0, Power: 0,
            Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly, BlinkRange: 400f,
            Description: "Blink 400 away from the nearest enemy to create distance (no target needed)."),

        new(Weakness, "Weakness", BaseClass.Mage, SkillEffect.DebuffDef,
            MpCost: 15, CastTicks: 5, CooldownTicks: 300, Range: 600, Power: 0,
            DurationTicks: 150, BuffKey: "curse_def", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffDef, 0.30f) },
            Category: SkillCategory.Debuff, SureHit: true,
            Description: "Curses the target: -30% Defence for 15s (instant cast, never fizzles)."),

        // (Greater Heal DELETED 2026-08-07 with the God layer, playtest-19 `0b` — it was on the God
        //  learn table and nothing else. The cleric's heal ladder is authored on its own class list.)

        new(FlameBolt, "Flamebolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 24, CastTicks: 40, CooldownTicks: 10, Range: 900, Power: 95,
            Replaces: new[] { MagicBolt },   // upgrades (replaces) the basic nuke
            Category: SkillCategory.Magic,
            Description: "A searing bolt — the nuker's stronger basic attack (replaces Magic Bolt)."),

        // Elemental Burst — NUKER 3rd-class ULTIMATE. Consumes 1 Elemental Stone per
        // cast (the reagent system) and ramps power 150 → 250 across 10 learn levels
        // (char 40/44/48/…/72/75). Numbers are placeholders — tune freely.
        new(ElementalBurst, "Elemental Burst", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 60, CastTicks: 50, CooldownTicks: 30, Range: 900, Power: 150,
            Category: SkillCategory.Magic, InitialMpCost: 12,
            ConsumableId: ItemCatalog.ElementalStone, ConsumableAmount: 1,
            Description: "An overwhelming elemental detonation. Consumes 1 Elemental Stone; "
                       + "its power grows each level (150 → 250).",
            Levels: new[]
            {
                new SkillLevel(Power: 150, MpCost: 60,  InitialMpCost: 12, SpCost: 4000,  Description: "Magic damage, power 150. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 161, MpCost: 65,  InitialMpCost: 13, SpCost: 5000,  Description: "Magic damage, power 161. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 172, MpCost: 70,  InitialMpCost: 14, SpCost: 6000,  Description: "Magic damage, power 172. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 183, MpCost: 75,  InitialMpCost: 15, SpCost: 7000,  Description: "Magic damage, power 183. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 194, MpCost: 80,  InitialMpCost: 16, SpCost: 8000,  Description: "Magic damage, power 194. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 205, MpCost: 85,  InitialMpCost: 17, SpCost: 9000,  Description: "Magic damage, power 205. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 216, MpCost: 90,  InitialMpCost: 18, SpCost: 10000, Description: "Magic damage, power 216. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 227, MpCost: 95,  InitialMpCost: 19, SpCost: 11000, Description: "Magic damage, power 227. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 238, MpCost: 100, InitialMpCost: 20, SpCost: 12000, Description: "Magic damage, power 238. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 250, MpCost: 105, InitialMpCost: 21, SpCost: 13000, Description: "Magic damage, power 250. Consumes 1 Elemental Stone."),
            }),

        // Frost Bind — first CONTESTED crowd-control skill (P1 primitive demo). A magical
        // Slow: lands via ATK-vs-WIT (DebuffLandChance), reduces move speed 50% for 10s.
        // Numbers are placeholders; this is the nuker's control tool until disciplines author theirs.
        new(FrostBind, "Frost Bind", BaseClass.Mage, SkillEffect.Slow,
            MpCost: 25, CastTicks: 20, CooldownTicks: 60, Range: 900, Power: 0,
            DurationTicks: 100, BuffKey: "slow_frost", Rank: 1, InitialMpCost: 5,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Magical,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Slow, 0.50f) },
            Description: "Magic slow — cuts the target's move speed by 50% for 10s. Lands on an "
                       + "ATK-vs-WIT contest (bosses are immune)."),

        // Entangling Roots — contested ROOT (magical): target cannot move for 8s (can still
        // act). Lands on ATK-vs-WIT; bosses immune. Demonstrates root-via-contest.
        new(EntanglingRoots, "Entangling Roots", BaseClass.Mage, SkillEffect.Root,
            MpCost: 28, CastTicks: 15, CooldownTicks: 80, Range: 900, Power: 0,
            DurationTicks: 80, BuffKey: "root", Rank: 1, InitialMpCost: 6,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Magical,
            Description: "Snares the target in place for 8s (cannot move, can still act). "
                       + "ATK-vs-WIT contest; bosses immune."),

        // Glacial Spike — nuke that deals +50% damage to a SLOWED or ROOTED target (combos
        // with Frost Bind / Entangling Roots). Demonstrates conditional damage.
        new(GlacialSpike, "Glacial Spike", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 30, CastTicks: 40, CooldownTicks: 15, Range: 900, Power: 90,
            Category: SkillCategory.Magic, InitialMpCost: 6,
            ConditionalOn: TargetCondition.Slowed | TargetCondition.Rooted, ConditionalDamagePct: 0.50f,
            Description: "A shard of ice (power 90) that strikes for +50% damage if the target "
                       + "is slowed or rooted."),

        // Creeping Frost — a STACKING chill with a per-stack effect table: 10% / 20% / 30%
        // slow on stacks 1-3, then a FREEZE (stun, no slow) on stack 4. Effect = Slow|Stun
        // (union) so it's recognised as contested CC; each landing cast adds a stack.
        new(CreepingFrost, "Creeping Frost", BaseClass.Mage, SkillEffect.Slow | SkillEffect.Stun,
            MpCost: 18, CastTicks: 15, CooldownTicks: 20, Range: 900, Power: 0,
            DurationTicks: 100, BuffKey: "creeping_frost", Rank: 1, InitialMpCost: 4,
            DebuffSchool: DebuffSchool.Magical,
            StackLevels: new[]
            {
                new StackLevel(SkillEffect.Slow, new EffectMagnitude[] { new(SkillEffect.Slow, 0.10f) }),
                new StackLevel(SkillEffect.Slow, new EffectMagnitude[] { new(SkillEffect.Slow, 0.20f) }),
                new StackLevel(SkillEffect.Slow, new EffectMagnitude[] { new(SkillEffect.Slow, 0.30f) }),
                new StackLevel(SkillEffect.Stun, System.Array.Empty<EffectMagnitude>()),   // freeze
            },
            Description: "A deepening chill — slows 10%/20%/30% on stacks 1-3, then FREEZES "
                       + "(stuns) on the 4th. Each landing cast adds a stack; ATK-vs-WIT contest."),

        // Holy Bolt — the Healer's offensive spell (replaces Magic Bolt). ONE skill;
        // per-race NAME only (Holy/Moonlight/Spirit Bolt) via ClassSkill.DisplayName.
        // 4 levels learned at 20/25/30/35.
        new(HolyStrike, "Holy Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 20, CastTicks: 40, CooldownTicks: 10, Range: 750, Power: 21,
            Replaces: new[] { MagicBolt },   // the healer's nuke replaces the basic
            Category: SkillCategory.Magic, InitialMpCost: 4,
            Description: "A bolt of holy power — the Healer's offensive spell (replaces Magic Bolt). Spells fail rather than miss.",
            Levels: new[]
            {
                new SkillLevel(Power: 21, MpCost: 20, InitialMpCost: 4, SpCost: 3200,  Description: "Magic damage, power 21."),
                new SkillLevel(Power: 25, MpCost: 23, InitialMpCost: 5, SpCost: 3200,  Description: "Magic damage, power 25."),
                new SkillLevel(Power: 30, MpCost: 26, InitialMpCost: 6, SpCost: 12800, Description: "Magic damage, power 30."),
                new SkillLevel(Power: 36, MpCost: 31, InitialMpCost: 7, SpCost: 25000, Description: "Magic damage, power 36."),
            }),

        new(GreaterWeakness, "Greater Weakness", BaseClass.Mage, SkillEffect.DebuffDef,
            MpCost: 22, CastTicks: 5, CooldownTicks: 300, Range: 900, Power: 0,
            DurationTicks: 200, BuffKey: "curse_def", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffDef, 0.45f) },
            Category: SkillCategory.Debuff, SureHit: true,
            Description: "A deeper curse: -45% Defence for 20s (never fizzles)."),
    };
}
