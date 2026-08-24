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
    // --- Nuker 2nd-class (CSV nuker 2nd) ---
    public const string ElementalBolt = "elemental_bolt";     // nuker basic nuke (replaces Magic Bolt)
    public const string QuickBolt = "quick_bolt";             // short-range fast nuke
    public const string RestoreSpirit = "restore_spirit";     // trades HP for MP (self)

    private static SkillDef[] MageSkills() => new SkillDef[]
    {
        // Magic Bolt — the starter nuke, 3 levels (auto-learn Lv.1; Lv.2/3 learned).
        new(MagicBolt, "Magic Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 9, CastTicks: 40, CooldownTicks: 10, Range: 600, Power: 12,
            Category: SkillCategory.Magic,  
            Description: "Hurls a bolt of force. Spells fail rather than miss.",
            Levels: new[]
            {                                                                              // learn level
                // MP is his `mage 1st.csv` (2026-08-19): 2+7 / 2+8 / 3+12.
                // ⚠ POWER IS HIS TOO, and was 12/17/24 until 2026-08-19 — the CSV says 12/15/21 and the
                // `--check` tool does not compare power, so this drifted unseen. His learn levels are
                // 1/7/14, not the 1/5/10 the old trailing comment claimed.
                new SkillLevel(Power: 12, MpCost: 9,   SpCost: 0,    Description: "Magic damage, power 12."),   // 1
                new SkillLevel(Power: 15, MpCost: 10,  SpCost: 480,  Description: "Magic damage, power 15."),   // 7
                new SkillLevel(Power: 21, MpCost: 15,  SpCost: 2200, Description: "Magic damage, power 21."),   // 14
            }),

        // Self Heal — the base MAGE heal: SELF ONLY, 3 levels (1/7/14). The nuker keeps this
        // (self-only) so a high-M.Atk nuker can't spam-heal the party; the HEALER replaces it
        // with the targeted Heal at level 20.
        new(SelfHeal, "Self Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 7, CastTicks: 50, CooldownTicks: 20, Range: 0, Power: 42,
            Category: SkillCategory.Heal,  
            TargetMode: TargetMode.SelfOnly,
            Description: "Restores your own HP. Scales with WIT.",
            Levels: new[]
            {
                new SkillLevel(Power: 42,  MpCost: 7,   SpCost: 160,  Description: "Self heal power 42."),
                new SkillLevel(Power: 67,  MpCost: 14,  SpCost: 480,  Description: "Self heal power 67."),
                new SkillLevel(Power: 107, MpCost: 22,  SpCost: 2200, Description: "Self heal power 107."),
            }),

        // Heal — the HEALER's targeted heal (ally or self); REPLACES Self Heal at level 20.
        // 4 levels @20/25/30/35 (base-mage no longer learns this).
        new(Heal, "Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 30, CastTicks: 50, CooldownTicks: 20, Range: 600, Power: 151,
            Category: SkillCategory.Heal,  
            Replaces: new[] { SelfHeal },
            Description: "Restores a friendly target's HP (or your own). Scales with WIT.",
            Levels: new[]
            {
                new SkillLevel(Power: 151, MpCost: 30,  SpCost: 3200,  Description: "Heal power 151."),
                new SkillLevel(Power: 195, MpCost: 38,  SpCost: 6400,  Description: "Heal power 195."),
                new SkillLevel(Power: 245, MpCost: 44,  SpCost: 12800, Description: "Heal power 245."),
                new SkillLevel(Power: 301, MpCost: 52,  SpCost: 25000, Description: "Heal power 301."),
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
            DurationTicks: 12000, BuffKey: "mage_might", Rank: 1,  
            ChildBuffs: new[] { BuffPAtk1, BuffPDef1 },
            Category: SkillCategory.Buff, SpCost: 960,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { CastId(FamPhysAtk), CastId(FamPhysDef), CastId(FamVamp), CastId(FamAccuracy) },
            Description: "Blesses you and nearby allies with +P.Atk and +P.Def for 20 minutes.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 150,  SpCost: 960,
                    ChildBuffs: new[] { BuffPAtk1, BuffPDef1 },
                    Description: "+8% P.Atk and +8% P.Def for 20 minutes."),
                new SkillLevel(MpCost: 160,  SpCost: 3200,
                    ChildBuffs: new[] { BuffPAtk2, BuffPDef1 },
                    Description: "+12% P.Atk and +8% P.Def for 20 minutes."),
                new SkillLevel(MpCost: 170,  SpCost: 6400,
                    ChildBuffs: new[] { BuffPAtk2, BuffPDef2 },
                    Description: "+12% P.Atk and +12% P.Def for 20 minutes."),
                new SkillLevel(MpCost: 180,  SpCost: 12800,
                    ChildBuffs: new[] { BuffPAtk2, BuffPDef2, BuffVamp2 },
                    Description: "+12% P.Atk, +12% P.Def, and 6% melee-attack vampirism for 20 minutes."),
                new SkillLevel(MpCost: 190,  SpCost: 25000,
                    ChildBuffs: new[] { BuffPAtk3, BuffPDef3, BuffVamp2, BuffAcc2 },
                    Description: "+15% P.Atk, +15% P.Def, 6% melee vampirism, +2 Accuracy."),
                new SkillLevel(MpCost: 200,  SpCost: 50000,
                    // ⚠ BuffVamp5 / BuffAcc4 are the TOPS of their families, not typos: both ladders
                    // gained middle rungs for his healer file on 2026-08-20 and everything above the
                    // insertion renumbered. The numbers this level hands out did not change.
                    ChildBuffs: new[] { BuffPAtk3, BuffPDef3, BuffVamp5, BuffAcc4 },
                    Description: "+15% P.Atk, +15% P.Def, 9% melee vampirism, +4 Accuracy."),
            }),

        // Anti-Magic — learnable mage passive: +M.Def and MAGIC RESISTANCE (damage reduction).
        // Lvls 1-2 = base mage; the Healer/Nuker CONTINUE it at lvls 3-6 (20/25/30/35).
        // ⚠ The CSVs' "mRes +5%" WAS built here as a fizzle floor, purely because no magic
        // damage-reduction stat existed (owner, 2026-08-10: *"the problem was we didn't have a mdmg
        // reduction, that's why we converted them to a floor"*). It is a damage reduction now, and
        // the mage@14 rung's old "5% chance for spells to fizzle on you" wording went with it.
        // The numbers are straight from cleric/nuker 2nd.csv — don't retune them here.
        new(MageAntiMagic, "Anti-Magic", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Hardens you against hostile magic.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 480,   Passive: new PassiveEffect(MagicDefence: 12), Description: "+12 magic defence."),
                new SkillLevel(SpCost: 2200,  Passive: new PassiveEffect(MagicDefence: 16, MagicResist: 0.05f),
                    Description: "+16 magic defence and 5% magic resistance."),
                new SkillLevel(SpCost: 3200,  Passive: new PassiveEffect(MagicDefence: 20, MagicResist: 0.05f),
                    Description: "+20 magic defence and 5% magic resistance."),
                new SkillLevel(SpCost: 6400,  Passive: new PassiveEffect(MagicDefence: 25, MagicResist: 0.05f),
                    Description: "+25 magic defence and 5% magic resistance."),
                new SkillLevel(SpCost: 12800, Passive: new PassiveEffect(MagicDefence: 30, MagicResist: 0.10f),
                    Description: "+30 magic defence and 10% magic resistance."),
                new SkillLevel(SpCost: 25000, Passive: new PassiveEffect(MagicDefence: 36, MagicResist: 0.10f),
                    Description: "+36 magic defence and 10% magic resistance."),
                // Level 7 = the healer's level-40 row (`healer 3rd.csv`, his).
                new SkillLevel(SpCost: 36000, Passive: new PassiveEffect(MagicDefence: 43, MagicResist: 0.15f),
                    Description: "+43 magic defence and 15% magic resistance."),
            }.Concat(HealerAntiMagicRungs()).ToArray()),

        // Vampiric Bolt — magic nuke that heals the caster for 40% of damage dealt. Level 1 is
        // the base-mage skill (@14); the Nuker CONTINUES it at levels 2-5 (@20/25/30/35).
        new(VampiricBolt, "Vampiric Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 28, CastTicks: 40, CooldownTicks: 10, Range: 750, Power: 21,
            Category: SkillCategory.Magic,  SpCost: 2200, Lifesteal: 0.40f,
            Description: "A draining bolt that heals you for 40% of the damage dealt.",
            Levels: new[]
            {                                                                                     // learn level
                // ⚠ MP RUNGS 1-5 ARE HIS (`mage 1st.csv` @14, `nuker 2nd.csv` @20-35, 2026-08-19):
                // 6+22 / 8+32 / 10+36 / 12+40 / 14+48. They were ~37% higher, which is what made this
                // the one bolt nobody could afford to spam. Rungs 6-14 have no CSV, so they carry the
                // SAME RATIO his band implies (×0.728 of the old numbers, rounded) — that keeps the
                // curve's shape and avoids a cliff at 40, where 62 used to jump straight to 90.
                //
                // ⚠ POWER RUNGS 1-5 ARE ALSO HIS, and were WRONG until 2026-08-19 (his call: "fix the
                // nuker bolt power"). His files say 21 @14 and 26/32/38/44 @20-35; the code carried
                // 29/37/44/50/57, ~25-30% high. `--check` compares MP and SP but NOT power, so nothing
                // caught it. His four nuker points are exactly linear — **26 + 1.2 per character level**
                // — and rungs 6-14 are that same line continued, which is the identical treatment the MP
                // column above already got. Vampiric and Elemental Bolt share one power ladder on his
                // sheet (both 26/32/38/44), so they are kept identical here.
                new SkillLevel(Power: 21,  MpCost: 28,   SpCost: 2200,   Description: "Drain power 21; heals 40% of damage."),   // 14
                new SkillLevel(Power: 26,  MpCost: 40,   SpCost: 3200,   Description: "Drain power 26; heals 40% of damage."),   // 20
                new SkillLevel(Power: 32,  MpCost: 46,   SpCost: 6400,   Description: "Drain power 32; heals 40% of damage."),   // 25
                new SkillLevel(Power: 38,  MpCost: 52,   SpCost: 12800,  Description: "Drain power 38; heals 40% of damage."),   // 30
                new SkillLevel(Power: 44,  MpCost: 62,   SpCost: 25000,  Description: "Drain power 44; heals 40% of damage."),   // 35
                new SkillLevel(Power: 50,  MpCost: 65,   SpCost: 40000,  Description: "Drain power 50; heals 40% of damage."),   // 40
                new SkillLevel(Power: 56,  MpCost: 73,   SpCost: 60000,  Description: "Drain power 56; heals 40% of damage."),   // 45
                new SkillLevel(Power: 62,  MpCost: 80,   SpCost: 85000,  Description: "Drain power 62; heals 40% of damage."),   // 50
                new SkillLevel(Power: 68,  MpCost: 87,   SpCost: 115000, Description: "Drain power 68; heals 40% of damage."),   // 55
                new SkillLevel(Power: 74,  MpCost: 95,   SpCost: 150000, Description: "Drain power 74; heals 40% of damage."),   // 60
                new SkillLevel(Power: 80,  MpCost: 100,  SpCost: 190000, Description: "Drain power 80; heals 40% of damage."),   // 65
                new SkillLevel(Power: 86,  MpCost: 108,  SpCost: 235000, Description: "Drain power 86; heals 40% of damage."),   // 70
                new SkillLevel(Power: 92,  MpCost: 115,  SpCost: 285000, Description: "Drain power 92; heals 40% of damage."),   // 75
                new SkillLevel(Power: 98,  MpCost: 122,  SpCost: 340000, Description: "Drain power 98; heals 40% of damage."),   // 80
            }),

        // Elemental Bolt — the Nuker's MAIN nuke (replaces Magic Bolt). 13 levels, learned
        // every 5 levels from 20 to 80.
        //
        // The power ladder is LINEAR IN CHARACTER LEVEL, and since 2026-08-19 the line is HIS:
        // `nuker 2nd.csv` authors 26/32/38/44 at 20/25/30/35, which is exactly **26 + 1.2 per
        // level**, and rungs 5-13 are that same line continued to 98 @ 80.
        // ⚠ It used to be anchored at POWER 108 @ LEVEL 74 (an IG top-nuke reading) and ran
        // 37 → 116, ~25-30% above his own band in the four levels he actually authored. That is
        // the drift he told us to fix; the anchor was ours, the band is his, and the band wins.
        // The ladder still IS the mage's scaling — don't cap it at the 2nd class (in IG your main
        // nuke keeps gaining levels for life), and don't re-raise the 40+ half on its own: it is
        // one straight line through his four points and a kink there is a kink in the mage curve.
        new(ElementalBolt, "Elemental Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 27, CastTicks: 40, CooldownTicks: 10, Range: 750, Power: 26,
            Replaces: new[] { MagicBolt },
            Category: SkillCategory.Magic,  
            Description: "A bolt of raw elemental force — the Nuker's basic attack (replaces Magic Bolt).",
            Levels: new[]
            {                                                                                    // learn level
                // ⚠ MP RUNGS 1-4 ARE HIS (`nuker 2nd.csv`, 2026-08-19): 4+16 / 5+18 / 6+20 / 7+24.
                // Rungs 5-13 have no CSV and carry the same ratio his band implies (×0.734 of the old
                // numbers), so there is no cliff at 40. Quick Bolt below runs the identical MP line —
                // that is his sheet's choice, not a copy slip: the two bolts differ in cast time and
                // range, not in what they cost.
                // ⚠ POWER RUNGS 1-4 ARE HIS TOO (same file, same date, his "fix the nuker bolt power").
                // 26/32/38/44 — the code carried 37/44/50/57. Rungs 5-13 continue his own +1.2/level.
                new SkillLevel(Power: 26,  MpCost: 20,  SpCost: 3200,   Description: "Magic damage, power 26."),   // 20
                new SkillLevel(Power: 32,  MpCost: 23,  SpCost: 6400,   Description: "Magic damage, power 32."),   // 25
                new SkillLevel(Power: 38,  MpCost: 26,  SpCost: 12800,  Description: "Magic damage, power 38."),   // 30
                new SkillLevel(Power: 44,  MpCost: 31,  SpCost: 25000,  Description: "Magic damage, power 44."),   // 35
                new SkillLevel(Power: 50,  MpCost: 33,  SpCost: 40000,  Description: "Magic damage, power 50."),   // 40
                new SkillLevel(Power: 56,  MpCost: 37,  SpCost: 60000,  Description: "Magic damage, power 56."),   // 45
                new SkillLevel(Power: 62,  MpCost: 40,  SpCost: 85000,  Description: "Magic damage, power 62."),   // 50
                new SkillLevel(Power: 68,  MpCost: 44,  SpCost: 115000, Description: "Magic damage, power 68."),   // 55
                new SkillLevel(Power: 74,  MpCost: 48,  SpCost: 150000, Description: "Magic damage, power 74."),   // 60
                new SkillLevel(Power: 80,  MpCost: 51,  SpCost: 190000, Description: "Magic damage, power 80."),   // 65
                new SkillLevel(Power: 86,  MpCost: 54,  SpCost: 235000, Description: "Magic damage, power 86."),   // 70
                new SkillLevel(Power: 92,  MpCost: 58,  SpCost: 285000, Description: "Magic damage, power 92."),   // 75
                new SkillLevel(Power: 98,  MpCost: 62,  SpCost: 340000, Description: "Magic damage, power 98."),   // 80
            }),

        // Quick Bolt — a short-range (150), fast (1.5s) nuke for weaving between casts.
        // Same 13-level ladder as Elemental Bolt at ~80% of its power (it trades damage for
        // cast time), same MP — the point is casts-per-second, not damage-per-cast.
        new(QuickBolt, "Quick Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 27, CastTicks: 15, CooldownTicks: 10, Range: 150, Power: 21,
            Category: SkillCategory.Magic,  
            Description: "A fast, close-range bolt (1.5s cast).",
            Levels: new[]
            {                                                                                    // learn level
                // MP: his `nuker 2nd.csv` rungs 1-4, then the same ×0.734 continuation as Elemental
                // Bolt above — the two bolts share one MP line on his sheet.
                // ⚠ POWER rungs 1-4 are his as well (21/26/30/36, was 30/35/40/46). His four points run
                // **21 + 1.0 per character level**, so rungs 5-13 continue that; it keeps Quick Bolt at
                // ~81% of Elemental Bolt at every rung, which is the trade this skill is built on.
                new SkillLevel(Power: 21, MpCost: 20,  SpCost: 3200,   Description: "Magic damage, power 21."),   // 20
                new SkillLevel(Power: 26, MpCost: 23,  SpCost: 6400,   Description: "Magic damage, power 26."),   // 25
                new SkillLevel(Power: 30, MpCost: 26,  SpCost: 12800,  Description: "Magic damage, power 30."),   // 30
                new SkillLevel(Power: 36, MpCost: 31,  SpCost: 25000,  Description: "Magic damage, power 36."),   // 35
                new SkillLevel(Power: 41, MpCost: 33,  SpCost: 40000,  Description: "Magic damage, power 41."),   // 40
                new SkillLevel(Power: 46, MpCost: 37,  SpCost: 60000,  Description: "Magic damage, power 46."),   // 45
                new SkillLevel(Power: 51, MpCost: 40,  SpCost: 85000,  Description: "Magic damage, power 51."),   // 50
                new SkillLevel(Power: 56, MpCost: 44,  SpCost: 115000, Description: "Magic damage, power 56."),   // 55
                new SkillLevel(Power: 61, MpCost: 48,  SpCost: 150000, Description: "Magic damage, power 61."),   // 60
                new SkillLevel(Power: 66, MpCost: 51,  SpCost: 190000, Description: "Magic damage, power 66."),   // 65
                new SkillLevel(Power: 71, MpCost: 54,  SpCost: 235000, Description: "Magic damage, power 71."),   // 70
                new SkillLevel(Power: 76, MpCost: 58,  SpCost: 285000, Description: "Magic damage, power 76."),   // 75
                new SkillLevel(Power: 81, MpCost: 62,  SpCost: 340000, Description: "Magic damage, power 81."),   // 80
            }),

        // Restore Spirit — trades HP for MP (self). Costs HP, not MP.
        //
        // ⚠ LEVEL 1 IS THE AUTHORED CSV and must stay verbatim: `nuker 2nd.csv` says
        // "exchanges HP (-66) for MP (+22)", SP 6400, learned at 25 — he re-authored it from -65/+20 on
        // 2026-08-24. Nothing in the 20-35 band may be retuned here; that file is the source of truth.
        //
        // Levels 2-10 (@40 then every 5 to 80) are OURS, in the band that has no CSV, exactly like
        // the bolt ladder. They exist because ONE level for life was the real defect: 20 MP is most
        // of a nuke at 25 and a rounding error at 60, so the skill slowed the drain instead of
        // sustaining a rotation the moment the bolt ladder passed it. (Owner, 2026-08-07, confirming
        // the diagnosis: it "needs levels, not a bigger HP trade".)
        //
        // The ENDPOINT is his: IG's Body to Mind is +120 MP for −360 HP; our HP pools are about half
        // of IG's, so the like-for-like price is 180 and he rounded it to **200 to balance**. Level
        // 10 is therefore 120 MP for 200 HP, and with the robe mastery's late +80 that is his
        // "**+200 MP, −200 HP** is a good late-levels balance" exactly.
        //
        // The HP prices are authored so DELIVERED MP (base + the mastery's mpWhenRestored) per HP
        // spent sits at ~1.00-1.09 from 40 up, landing on exactly 1.00 at 80. The CSV band below it
        // is stingier (0.77 → 0.92) and stays that way. That is the design he stated: a mage's mana
        // management gets *easier* than a healer's but never free — "farm 30~40 mins, rest a bit, or
        // get a restorer with you".
        //
        // ⚠ A nuker in LIGHT or HEAVY earns no mastery bonus and pays full price for the base number
        // alone (3.25 → 1.67 HP per MP). That is the robe's identity, not a bug.
        new(RestoreSpirit, "Restore Spirit", BaseClass.Mage, SkillEffect.RestoreMp,
            MpCost: 0, CastTicks: 40, CooldownTicks: 50, Range: 0, Power: 22,
            Category: SkillCategory.Heal, TargetMode: TargetMode.SelfOnly, SpCost: 6400,
            HpCost: 66,
            Description: "Burns HP to restore MP to yourself (much more with robe mastery).",
            Levels: new[]
            {
                new SkillLevel(Power: 22,  HpCost: 66,  SpCost: 6400,   Description: "Burns 66 HP to restore 22 MP."),   // 25  ← CSV
                new SkillLevel(Power: 45,  HpCost: 90,  SpCost: 26000,  Description: "Burns 90 HP to restore 45 MP."),   // 40
                new SkillLevel(Power: 55,  HpCost: 105, SpCost: 40000,  Description: "Burns 105 HP to restore 55 MP."),  // 45
                new SkillLevel(Power: 65,  HpCost: 118, SpCost: 56000,  Description: "Burns 118 HP to restore 65 MP."),  // 50
                new SkillLevel(Power: 75,  HpCost: 131, SpCost: 75000,  Description: "Burns 131 HP to restore 75 MP."),  // 55
                new SkillLevel(Power: 85,  HpCost: 144, SpCost: 98000,  Description: "Burns 144 HP to restore 85 MP."),  // 60
                new SkillLevel(Power: 95,  HpCost: 157, SpCost: 124000, Description: "Burns 157 HP to restore 95 MP."),  // 65
                new SkillLevel(Power: 105, HpCost: 170, SpCost: 153000, Description: "Burns 170 HP to restore 105 MP."), // 70
                new SkillLevel(Power: 113, HpCost: 185, SpCost: 185000, Description: "Burns 185 HP to restore 113 MP."), // 75
                new SkillLevel(Power: 120, HpCost: 200, SpCost: 220000, Description: "Burns 200 HP to restore 120 MP."), // 80
            }),

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
            DurationTicks: 300, BuffKey: "mana_barrier", Rank: 1, CountsTowardBuffLimit: false, TargetMode: TargetMode.SelfOnly,
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
            Category: SkillCategory.Magic,  
            ConsumableId: ItemCatalog.ElementalStone, ConsumableAmount: 1,
            Description: "An overwhelming elemental detonation. Consumes 1 Elemental Stone; "
                       + "its power grows each level (150 → 250).",
            Levels: new[]
            {
                new SkillLevel(Power: 150, MpCost: 60,   SpCost: 4000,  Description: "Magic damage, power 150. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 161, MpCost: 65,   SpCost: 5000,  Description: "Magic damage, power 161. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 172, MpCost: 70,   SpCost: 6000,  Description: "Magic damage, power 172. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 183, MpCost: 75,   SpCost: 7000,  Description: "Magic damage, power 183. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 194, MpCost: 80,   SpCost: 8000,  Description: "Magic damage, power 194. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 205, MpCost: 85,   SpCost: 9000,  Description: "Magic damage, power 205. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 216, MpCost: 90,   SpCost: 10000, Description: "Magic damage, power 216. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 227, MpCost: 95,   SpCost: 11000, Description: "Magic damage, power 227. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 238, MpCost: 100,  SpCost: 12000, Description: "Magic damage, power 238. Consumes 1 Elemental Stone."),
                new SkillLevel(Power: 250, MpCost: 105,  SpCost: 13000, Description: "Magic damage, power 250. Consumes 1 Elemental Stone."),
            }),

        // Frost Bind — first CONTESTED crowd-control skill (P1 primitive demo). A magical
        // Slow: lands via ATK-vs-WIT (DebuffLandChance), reduces move speed 50% for 10s.
        // Numbers are placeholders; this is the nuker's control tool until disciplines author theirs.
        new(FrostBind, "Frost Bind", BaseClass.Mage, SkillEffect.Slow,
            MpCost: 25, CastTicks: 20, CooldownTicks: 60, Range: 900, Power: 0,
            DurationTicks: 100, BuffKey: "slow_frost", Rank: 1,  
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Magical,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Slow, 0.50f) },
            Description: "Magic slow — cuts the target's move speed by 50% for 10s. Lands on an "
                       + "ATK-vs-WIT contest (bosses are immune)."),

        // Entangling Roots — contested ROOT (magical): target cannot move for 8s (can still
        // act). Lands on ATK-vs-WIT; bosses immune. Demonstrates root-via-contest.
        new(EntanglingRoots, "Entangling Roots", BaseClass.Mage, SkillEffect.Root,
            MpCost: 28, CastTicks: 15, CooldownTicks: 80, Range: 900, Power: 0,
            DurationTicks: 80, BuffKey: "root", Rank: 1,  
            DebuffLandMod: 0.5f,   // BL-90: a MAGICAL hold, his general "x0.5". Physical ones stay x1 (CON saves).
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Magical,
            Description: "Snares the target in place for 8s (cannot move, can still act). "
                       + "ATK-vs-WIT contest; bosses immune."),

        // Glacial Spike — nuke that deals +50% damage to a SLOWED or ROOTED target (combos
        // with Frost Bind / Entangling Roots). Demonstrates conditional damage.
        new(GlacialSpike, "Glacial Spike", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 30, CastTicks: 40, CooldownTicks: 15, Range: 900, Power: 90,
            Category: SkillCategory.Magic,  
            ConditionalOn: TargetCondition.Slowed | TargetCondition.Rooted, ConditionalDamagePct: 0.50f,
            Description: "A shard of ice (power 90) that strikes for +50% damage if the target "
                       + "is slowed or rooted."),

        // Creeping Frost — a STACKING chill with a per-stack effect table: 10% / 20% / 30%
        // slow on stacks 1-3, then a FREEZE (stun, no slow) on stack 4. Effect = Slow|Stun
        // (union) so it's recognised as contested CC; each landing cast adds a stack.
        new(CreepingFrost, "Creeping Frost", BaseClass.Mage, SkillEffect.Slow | SkillEffect.Stun,
            MpCost: 18, CastTicks: 15, CooldownTicks: 20, Range: 900, Power: 0,
            DurationTicks: 100, BuffKey: "creeping_frost", Rank: 1,  
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
            Category: SkillCategory.Magic,  
            Description: "A bolt of holy power — the Healer's offensive spell (replaces Magic Bolt). Spells fail rather than miss.",
            Levels: new[]
            {
                new SkillLevel(Power: 21, MpCost: 20,  SpCost: 3200,  Description: "Magic damage, power 21."),
                new SkillLevel(Power: 25, MpCost: 23,  SpCost: 6400,  Description: "Magic damage, power 25."),
                new SkillLevel(Power: 30, MpCost: 26,  SpCost: 12800, Description: "Magic damage, power 30."),
                new SkillLevel(Power: 36, MpCost: 31,  SpCost: 25000, Description: "Magic damage, power 36."),
            }),

        new(GreaterWeakness, "Greater Weakness", BaseClass.Mage, SkillEffect.DebuffDef,
            MpCost: 22, CastTicks: 5, CooldownTicks: 300, Range: 900, Power: 0,
            DurationTicks: 200, BuffKey: "curse_def", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffDef, 0.45f) },
            Category: SkillCategory.Debuff, SureHit: true,
            Description: "A deeper curse: -45% Defence for 20s (never fizzles)."),
    };
}
