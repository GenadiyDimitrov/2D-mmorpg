namespace Game.Shared;

// =========================================================================================
//  BUFF LADDERS, part 2 — every family EXCEPT the speed group (which shipped first and still
//  lives in Skills.Common.cs, together with Dash). See docs/design/BuffLadders.md.
//
//  The rule, unchanged: an effect that competes has ONE number line (a "family"). Every source
//  of that effect — a potion, a scroll, one rung of a class buff, the NPC buffer's hour-long
//  blessing — applies the SAME single-buff skill, so they can never stack. They compete on the
//  family's BuffKey by Rank, which ApplyBuff already arbitrates.
//
//  ⚠ An IMPROVED (group) buff is NOT a bundle of independent parts (it was, 0.36-0.41). It is one
//  buff at GROUP rank that COVERS several families at once: casting it evicts those singles, and
//  no potion, scroll or single blessing can take one part of it back afterwards. The rungs below
//  are still where its numbers come from — a group names the rungs it contains.
//
//  ⚠ RANK IS NOT RARITY. For the four families that have a potion, they coincide (three rungs,
//  Common/Uncommon/Rare). For the scroll-only families they do NOT: a scroll's rarity is its
//  price/drop tier — chosen because the family has no potion analogue — while its rank is its
//  position on a SIX-rung ladder the class buff also climbs. An Epic Body scroll is rung 2 of 6.
//
//  ⚠ IDS ARE STAT-SHAPED, NOT NAME-SHAPED (`buff_patk_2`, not `buff_might_u`). Skill ids are
//  append-only, and these buffs have already been renamed twice while the design settled. An id
//  that spells the STAT survives any number of display-name changes.
// =========================================================================================
public static partial class SkillCatalog
{
    // ---- Families that have BOTH a potion and a scroll: three rungs, Common/Uncommon/Rare,
    //      and the Rare rung equals the strongest class buff. That is deliberate (design doc):
    //      consumables can cover the whole BASIC layer, and what keeps a buffer worth grouping
    //      with is Harmony, which has no consumable at all. ----
    public const string FamPhysAtk = "atk_phys";    // Might   — % P.Atk
    public const string FamPhysDef = "def_phys";    // Bulwark — % P.Def
    public const string FamMagAtk  = "atk_mag";     // Force   — % M.Atk
    public const string FamMagDef  = "def_mag";     // Ward    — % M.Def
    // Aim is accuracy's ladder and it is the exact mirror of Agility's (evasion): 1 / 2 / 4, its own
    // potion and its own scroll. Hit and evasion are the two halves of one contest, so a player who
    // can buy one must be able to buy the other (owner 2026-07-31).
    public const string FamAccuracy = "accuracy";   // Aim     — flat accuracy

    // ---- Families with NO consumable at all: they exist only as children of a class buff.
    //      Rung count is free here (nothing has to line up with a rarity), so it is chosen to
    //      reproduce the values the cleric already casts today. ----
    public const string FamVamp      = "vamp";        // Vampirism — % melee vampirism
    public const string FamInterrupt = "interrupt";   // Resolve   — flat interrupt resistance

    // ---- Scroll-only families: six rungs, and the scrolls sit on rungs 2 / 4 / 6
    //      (Epic / Legendary / Mythic). ----
    public const string FamMaxHp    = "hp_max";       // Body     — % Max HP
    public const string FamMaxMp    = "mp_max";       // Soul     — % Max MP
    public const string FamHpRegen  = "hp_regen";     // Vigor    — % HP regeneration
    public const string FamMpRegen  = "mp_regen";     // Serenity — % MP regeneration
    public const string FamCritRate = "crit_rate";    // Focus    — % physical crit rate
    public const string FamCritDmg  = "crit_dmg";     // Ferocity — % physical crit damage
    public const string FamMagCrit  = "mcrit_rate";   // Insight  — % magic crit rate
    public const string FamFrenzy   = "frenzy";       // Frenzy   — the whole trade-off buff

    // ---------------------------------------------------------------------------------------
    //  Single-buff ids. `Rung(family, n)` builds them, so these consts are only for the places
    //  that name one directly (class-buff child lists, the NPC buffer).
    // ---------------------------------------------------------------------------------------
    public static string Rung(string family, int rank) => $"buff_{family}_{rank}";

    public const string BuffPAtk1 = "buff_atk_phys_1", BuffPAtk2 = "buff_atk_phys_2", BuffPAtk3 = "buff_atk_phys_3";
    public const string BuffPDef1 = "buff_def_phys_1", BuffPDef2 = "buff_def_phys_2", BuffPDef3 = "buff_def_phys_3";
    public const string BuffMAtk1 = "buff_atk_mag_1",  BuffMAtk2 = "buff_atk_mag_2",  BuffMAtk3 = "buff_atk_mag_3";
    public const string BuffMDef1 = "buff_def_mag_1",  BuffMDef2 = "buff_def_mag_2",  BuffMDef3 = "buff_def_mag_3";

    public const string BuffVamp1 = "buff_vamp_1", BuffVamp2 = "buff_vamp_2", BuffVamp3 = "buff_vamp_3";
    public const string BuffAcc1  = "buff_accuracy_1", BuffAcc2 = "buff_accuracy_2", BuffAcc3 = "buff_accuracy_3";
    public const string BuffIntr1 = "buff_interrupt_1", BuffIntr2 = "buff_interrupt_2";
    public const string BuffIntr3 = "buff_interrupt_3", BuffIntr4 = "buff_interrupt_4";

    // ---------------------------------------------------------------------------------------
    //  Consumable skill ids (the item's UseSkillId). The item owns the rarity; the SKILL owns
    //  the duration, the cast time and which rung it hands out.
    // ---------------------------------------------------------------------------------------
    public const string PotMightC = "pot_patk_c", PotMightU = "pot_patk_u", PotMightR = "pot_patk_r";
    public const string ScrMightC = "scr_patk_c", ScrMightU = "scr_patk_u", ScrMightR = "scr_patk_r";
    public const string PotBulwarkC = "pot_pdef_c", PotBulwarkU = "pot_pdef_u", PotBulwarkR = "pot_pdef_r";
    public const string ScrBulwarkC = "scr_pdef_c", ScrBulwarkU = "scr_pdef_u", ScrBulwarkR = "scr_pdef_r";
    public const string PotForceC = "pot_matk_c", PotForceU = "pot_matk_u", PotForceR = "pot_matk_r";
    public const string ScrForceC = "scr_matk_c", ScrForceU = "scr_matk_u", ScrForceR = "scr_matk_r";
    public const string PotWardC = "pot_mdef_c", PotWardU = "pot_mdef_u", PotWardR = "pot_mdef_r";
    public const string ScrWardC = "scr_mdef_c", ScrWardU = "scr_mdef_u", ScrWardR = "scr_mdef_r";
    public const string PotAimC = "pot_acc_c", PotAimU = "pot_acc_u", PotAimR = "pot_acc_r";
    public const string ScrAimC = "scr_acc_c", ScrAimU = "scr_acc_u", ScrAimR = "scr_acc_r";

    public const string ScrBodyE = "scr_hp_e", ScrBodyL = "scr_hp_l", ScrBodyM = "scr_hp_m";
    public const string ScrSoulE = "scr_mp_e", ScrSoulL = "scr_mp_l", ScrSoulM = "scr_mp_m";
    public const string ScrVigorE = "scr_hpreg_e", ScrVigorL = "scr_hpreg_l", ScrVigorM = "scr_hpreg_m";
    public const string ScrSerenityE = "scr_mpreg_e", ScrSerenityL = "scr_mpreg_l", ScrSerenityM = "scr_mpreg_m";
    public const string ScrFocusE = "scr_crit_e", ScrFocusL = "scr_crit_l", ScrFocusM = "scr_crit_m";
    public const string ScrFerocityE = "scr_critdmg_e", ScrFerocityL = "scr_critdmg_l", ScrFerocityM = "scr_critdmg_m";
    public const string ScrInsightE = "scr_mcrit_e", ScrInsightL = "scr_mcrit_l", ScrInsightM = "scr_mcrit_m";
    public const string ScrFrenzyE = "scr_frenzy_e", ScrFrenzyL = "scr_frenzy_l", ScrFrenzyM = "scr_frenzy_m";

    // ---------------------------------------------------------------------------------------
    //  The ladders themselves. One line per family; the array IS the number line.
    // ---------------------------------------------------------------------------------------

    /// <summary>Builds one family's whole ladder: rung i (1-based) carries values[i-1].
    /// `unit` is what the number means in the tooltip ("% P.Atk", "Evasion", …).</summary>
    private static IEnumerable<SkillDef> Ladder(string family, string name, SkillEffect effect,
        ModifierMode mode, string unit, params float[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            float v = values[i];
            string shown = mode == ModifierMode.Percent ? $"+{v * 100:0.#}% {unit}" : $"+{v:0.#} {unit}";
            yield return SingleBuff(Rung(family, i + 1), name, family, i + 1, effect,
                new EffectMagnitude(effect, v, mode), shown + ".");
        }
    }

    /// <summary>Frenzy is the one family whose rung is a WHOLE buff rather than a single stat —
    /// the owner asked for the scroll to carry "the full frenzy". It still behaves like every
    /// other family: one key, six rungs, stronger replaces weaker. The Max HP/MP penalty SHRINKS
    /// as the rung climbs, so power is monotonic even though two of its numbers move opposite ways.</summary>
    private static SkillDef FrenzyRung(int rank, float penalty, float gain, int move, int eva = 8) =>
        new(Rung(FamFrenzy, rank), "Frenzy", BaseClass.Fighter,
            SkillEffect.BuffHp | SkillEffect.BuffMp | SkillEffect.BuffPhysAtk | SkillEffect.BuffMagAtk
            | SkillEffect.BuffCastSpeed | SkillEffect.BuffAtkSpeed | SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            BuffKey: FamFrenzy, Rank: rank,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffHp, -penalty), new(SkillEffect.BuffMp, -penalty),
                new(SkillEffect.BuffPhysAtk, gain), new(SkillEffect.BuffMagAtk, gain),
                new(SkillEffect.BuffCastSpeed, gain), new(SkillEffect.BuffAtkSpeed, gain),
                new(SkillEffect.BuffMoveSpeed, move, ModifierMode.Flat),
                new(SkillEffect.BuffEvasion, -eva, ModifierMode.Flat),
            },
            Category: SkillCategory.Buff,
            Description: $"−{penalty * 100:0}% Max HP/MP, +{gain * 100:0}% P.Atk / M.Atk / attack & cast speed, "
                       + $"+{move} Move Speed, −{eva} Evasion.");

    // ---------------------------------------------------------------------------------------
    //  CASTABLE singles — what a BUFFER CLASS learns (owner 2026-07-31: "I want the cleric to
    //  learn the individual buffs"). One skill per family, one level per rung, 20 minutes, on an
    //  ally or yourself. Mechanically identical to a potion or a scroll: the wrapper owns the
    //  duration and hands out the family's rung, so a cleric's Might and a Might potion are the
    //  same buff and the better one wins.
    //
    //  The improved GROUPS are the tier above (150+ MP, a Warchanter's skill) — the whole point
    //  of splitting is that the cleric now spends five casts and a lot of MP on what the group
    //  does in one.
    // ---------------------------------------------------------------------------------------
    public static string CastId(string family) => $"cast_{family}";

    private const int SingleBuffMpLow = 30, SingleBuffMpHigh = 50;   // owner: "make it 30-50"
    private static readonly int[] BuffSpCosts = { 3200, 6400, 12800, 25000, 50000, 100000 };

    /// <summary>One castable single buff, with a level per rung of its family. MP climbs across the
    /// rungs from 30 to 50 — the ceiling is the owner's, and it is what makes the improved group
    /// (150+) the efficient choice for a class that has one.</summary>
    /// <param name="children">The family's rungs, weakest first — one skill LEVEL each. Passed in
    /// rather than derived, because the speed families shipped first and their ids are named
    /// (`buff_swift_c`), not numbered.</param>
    /// <param name="text">The rung's own description, used as the level's.</param>
    private static SkillDef CastSingle(string family, string name, SkillEffect effect,
        string[] children, Func<string, string> text, string desc)
    {
        int n = children.Length;
        var levels = new SkillLevel[n];
        for (int i = 0; i < n; i++)
        {
            int mp = n == 1 ? SingleBuffMpLow
                   : SingleBuffMpLow + (SingleBuffMpHigh - SingleBuffMpLow) * i / (n - 1);
            levels[i] = new SkillLevel(MpCost: mp, InitialMpCost: mp / 5,
                SpCost: BuffSpCosts[Math.Min(i, BuffSpCosts.Length - 1)],
                ChildBuffs: new[] { children[i] }, Description: text(children[i]));
        }
        return new SkillDef(CastId(family), name, BaseClass.Mage, effect,
            MpCost: SingleBuffMpLow, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, InitialMpCost: SingleBuffMpLow / 5,
            ChildBuffs: new[] { children[0] },
            Category: SkillCategory.Buff, SpCost: BuffSpCosts[0],
            Description: desc, Levels: levels);
    }

    /// <param name="alreadyBuilt">Everything BuildCatalog has assembled so far. Needed because the
    /// four SPEED families shipped in Skills.Common.cs, and the castable singles below quote their
    /// rungs' descriptions rather than restating the numbers.</param>
    private static SkillDef[] BuffLadderSkills(IReadOnlyList<SkillDef> alreadyBuilt)
    {
        var list = new List<SkillDef>();

        // ===== Families with a potion AND a scroll — three rungs, C / U / R =====
        // P.Atk and P.Def use the owner's anchor 8 / 12 / 15%. Rungs 1-3 are exactly the values
        // the base-mage and cleric Might already casts, so nobody's buff changes number today.
        list.AddRange(Ladder(FamPhysAtk, "Might",   SkillEffect.BuffPhysAtk, ModifierMode.Percent, "P.Atk", 0.08f, 0.12f, 0.15f));
        list.AddRange(Ladder(FamPhysDef, "Bulwark", SkillEffect.BuffDef,     ModifierMode.Percent, "P.Def", 0.08f, 0.12f, 0.15f));
        // M.Atk tops out at the NPC buffer's 32%; the middle rung is the cleric's current 25%.
        // Percent M.Atk is authored at the EFFECTIVE value (see docs — magic-buff authoring).
        list.AddRange(Ladder(FamMagAtk,  "Force",   SkillEffect.BuffMagAtk,  ModifierMode.Percent, "M.Atk", 0.15f, 0.25f, 0.32f));
        list.AddRange(Ladder(FamMagDef,  "Ward",    SkillEffect.BuffMagicDef,ModifierMode.Percent, "M.Def", 0.10f, 0.20f, 0.30f));
        // Aim mirrors Agility exactly — the two sides of the hit/evade contest cost the same.
        list.AddRange(Ladder(FamAccuracy,"Aim",     SkillEffect.BuffAccuracy,ModifierMode.Flat,    "Accuracy", 1, 2, 4));

        // ===== No-consumable families — class buffs only =====
        list.AddRange(Ladder(FamVamp,     "Vampirism", SkillEffect.BuffMeleeVamp,      ModifierMode.Percent, "melee vampirism", 0.03f, 0.06f, 0.09f));
        list.AddRange(Ladder(FamInterrupt,"Resolve",   SkillEffect.BuffInterruptResist,ModifierMode.Flat,    "interrupt resistance", 18, 25, 40, 60));

        // ===== Scroll-only families — SIX rungs; the scrolls are rungs 2 / 4 / 6 =====
        list.AddRange(Ladder(FamMaxHp,   "Body",     SkillEffect.BuffHp,           ModifierMode.Percent, "Max HP", 0.10f, 0.15f, 0.20f, 0.25f, 0.30f, 0.35f));
        list.AddRange(Ladder(FamMaxMp,   "Soul",     SkillEffect.BuffMp,           ModifierMode.Percent, "Max MP", 0.10f, 0.15f, 0.20f, 0.25f, 0.30f, 0.35f));
        list.AddRange(Ladder(FamHpRegen, "Vigor",    SkillEffect.BuffHpRegen,      ModifierMode.Percent, "HP regeneration", 0.05f, 0.10f, 0.12f, 0.15f, 0.17f, 0.20f));
        list.AddRange(Ladder(FamMpRegen, "Serenity", SkillEffect.BuffMpRegen,      ModifierMode.Percent, "MP regeneration", 0.05f, 0.10f, 0.12f, 0.15f, 0.17f, 0.20f));
        list.AddRange(Ladder(FamCritRate,"Focus",    SkillEffect.BuffCritRate,     ModifierMode.Percent, "critical rate", 0.05f, 0.10f, 0.15f, 0.20f, 0.25f, 0.30f));
        list.AddRange(Ladder(FamCritDmg, "Ferocity", SkillEffect.BuffCritDamage,   ModifierMode.Percent, "critical damage", 0.10f, 0.15f, 0.20f, 0.25f, 0.30f, 0.35f));
        list.AddRange(Ladder(FamMagCrit, "Insight",  SkillEffect.BuffMagicCritRate,ModifierMode.Percent, "magic critical rate", 0.20f, 0.35f, 0.50f, 0.65f, 0.80f, 1.00f));

        // Frenzy: the Max HP/MP penalty shrinks 30 → 10% while the gain climbs 5 → 8%, so power is
        // monotonic on both counts. The −8 evasion is FLAT across the ladder on purpose: it is the
        // buff's identity (recklessness), not a number you buy your way out of — and a penalty that
        // grew with the rung would make a higher rung genuinely worse in one respect, which is
        // exactly what the rank rule cannot express. Rung 1 = the cleric's Frenzy today (bar that
        // evasion), rung 6 = the NPC buffer's.
        list.Add(FrenzyRung(1, 0.30f, 0.05f, 5));
        list.Add(FrenzyRung(2, 0.26f, 0.06f, 6));
        list.Add(FrenzyRung(3, 0.22f, 0.06f, 6));
        list.Add(FrenzyRung(4, 0.18f, 0.07f, 7));
        list.Add(FrenzyRung(5, 0.14f, 0.07f, 7));
        list.Add(FrenzyRung(6, 0.10f, 0.08f, 8));
        // Rung 7 — the TOP of the family, and the only thing `Madness` hands out (`BL-34`). Nothing
        // else in the game reaches it: no potion, no scroll, no NPC buffer, and no single-target
        // Frenzy. It is the party cast's whole reward for being a level-76 skill.
        //
        // ⚠ ONE number here is mine and it is flagged so he can move it in one line. The penalty
        // stride is HIS and is perfectly regular (−0.04 a rung: .30 .26 .22 .18 .14 .10), so 0.06
        // is simply the next one. The GAIN is where his ladder is ambiguous — it steps +0.01 on the
        // EVEN rungs (2→.06, 4→.07, 6→.08), which would leave rung 7 at .08 and make the top rung
        // differ from the one below it by the penalty alone. For a skill gated at 76 that reads as
        // no reward at all, so the step is taken here: +9% and +9 move. If he wants the strict even-
        // rung reading, this line becomes FrenzyRung(7, 0.06f, 0.08f, 8).
        //
        // The −8 evasion does NOT move, on his own rule: it is the buff's identity (recklessness),
        // not a number you buy your way out of.
        list.Add(FrenzyRung(7, 0.06f, 0.09f, 9));

        // ===== The consumables =====
        // Potion + scroll pairs. Same rung, same buff; the scroll just lasts an hour instead of
        // twenty minutes, so drinking a potion on top of an equal scroll is refused, not wasted.
        void Pair(string potId, string scrId, string potName, string scrName, string child,
                  SkillEffect effect, string what)
        {
            list.Add(Potion(potId, potName, child, effect, what));
            list.Add(Scroll(scrId, scrName, child, effect, what));
        }

        Pair(PotMightC, ScrMightC, "Might Potion (Lesser)",  "Scroll of Might (Lesser)",  BuffPAtk1, SkillEffect.BuffPhysAtk, "+8% P.Atk");
        Pair(PotMightU, ScrMightU, "Might Potion",           "Scroll of Might",           BuffPAtk2, SkillEffect.BuffPhysAtk, "+12% P.Atk");
        Pair(PotMightR, ScrMightR, "Might Potion (Greater)", "Scroll of Might (Greater)", BuffPAtk3, SkillEffect.BuffPhysAtk, "+15% P.Atk");
        Pair(PotBulwarkC, ScrBulwarkC, "Bulwark Potion (Lesser)",  "Scroll of Bulwark (Lesser)",  BuffPDef1, SkillEffect.BuffDef, "+8% P.Def");
        Pair(PotBulwarkU, ScrBulwarkU, "Bulwark Potion",           "Scroll of Bulwark",           BuffPDef2, SkillEffect.BuffDef, "+12% P.Def");
        Pair(PotBulwarkR, ScrBulwarkR, "Bulwark Potion (Greater)", "Scroll of Bulwark (Greater)", BuffPDef3, SkillEffect.BuffDef, "+15% P.Def");
        Pair(PotForceC, ScrForceC, "Force Potion (Lesser)",  "Scroll of Force (Lesser)",  BuffMAtk1, SkillEffect.BuffMagAtk, "+15% M.Atk");
        Pair(PotForceU, ScrForceU, "Force Potion",           "Scroll of Force",           BuffMAtk2, SkillEffect.BuffMagAtk, "+25% M.Atk");
        Pair(PotForceR, ScrForceR, "Force Potion (Greater)", "Scroll of Force (Greater)", BuffMAtk3, SkillEffect.BuffMagAtk, "+32% M.Atk");
        Pair(PotWardC, ScrWardC, "Ward Potion (Lesser)",  "Scroll of Ward (Lesser)",  BuffMDef1, SkillEffect.BuffMagicDef, "+10% M.Def");
        Pair(PotWardU, ScrWardU, "Ward Potion",           "Scroll of Ward",           BuffMDef2, SkillEffect.BuffMagicDef, "+20% M.Def");
        Pair(PotWardR, ScrWardR, "Ward Potion (Greater)", "Scroll of Ward (Greater)", BuffMDef3, SkillEffect.BuffMagicDef, "+30% M.Def");
        Pair(PotAimC, ScrAimC, "Aim Potion (Lesser)",  "Scroll of Aim (Lesser)",  Rung(FamAccuracy, 1), SkillEffect.BuffAccuracy, "+1 Accuracy");
        Pair(PotAimU, ScrAimU, "Aim Potion",           "Scroll of Aim",           Rung(FamAccuracy, 2), SkillEffect.BuffAccuracy, "+2 Accuracy");
        Pair(PotAimR, ScrAimR, "Aim Potion (Greater)", "Scroll of Aim (Greater)", Rung(FamAccuracy, 3), SkillEffect.BuffAccuracy, "+4 Accuracy");

        // Scroll-only families. The rarity is the PRICE tier, the rung is the POWER — Epic reads
        // rung 2, Legendary rung 4, Mythic rung 6. There is no potion of any of these.
        void ScrollTrio(string e, string l, string m, string name, string family, SkillEffect effect,
                        string unit, float v2, float v4, float v6, bool percent = true)
        {
            string Show(float v) => percent ? $"+{v * 100:0.#}% {unit}" : $"+{v:0.#} {unit}";
            list.Add(Scroll(e, $"Scroll of {name} (Superior)", Rung(family, 2), effect, Show(v2)));
            list.Add(Scroll(l, $"Scroll of {name} (Grand)",    Rung(family, 4), effect, Show(v4)));
            list.Add(Scroll(m, $"Scroll of {name} (Supreme)",  Rung(family, 6), effect, Show(v6)));
        }

        ScrollTrio(ScrBodyE, ScrBodyL, ScrBodyM, "Body", FamMaxHp, SkillEffect.BuffHp, "Max HP", 0.15f, 0.25f, 0.35f);
        ScrollTrio(ScrSoulE, ScrSoulL, ScrSoulM, "Soul", FamMaxMp, SkillEffect.BuffMp, "Max MP", 0.15f, 0.25f, 0.35f);
        ScrollTrio(ScrVigorE, ScrVigorL, ScrVigorM, "Vigor", FamHpRegen, SkillEffect.BuffHpRegen, "HP regeneration", 0.10f, 0.15f, 0.20f);
        ScrollTrio(ScrSerenityE, ScrSerenityL, ScrSerenityM, "Serenity", FamMpRegen, SkillEffect.BuffMpRegen, "MP regeneration", 0.10f, 0.15f, 0.20f);
        ScrollTrio(ScrFocusE, ScrFocusL, ScrFocusM, "Focus", FamCritRate, SkillEffect.BuffCritRate, "critical rate", 0.10f, 0.20f, 0.30f);
        ScrollTrio(ScrFerocityE, ScrFerocityL, ScrFerocityM, "Ferocity", FamCritDmg, SkillEffect.BuffCritDamage, "critical damage", 0.15f, 0.25f, 0.35f);
        ScrollTrio(ScrInsightE, ScrInsightL, ScrInsightM, "Insight", FamMagCrit, SkillEffect.BuffMagicCritRate, "magic critical rate", 0.35f, 0.65f, 1.00f);

        // Frenzy's scroll can't use ScrollTrio — its rung is a whole buff, not one number.
        list.Add(Scroll(ScrFrenzyE, "Scroll of Frenzy (Superior)", Rung(FamFrenzy, 2),
            SkillEffect.BuffPhysAtk, "−26% Max HP/MP but +6% offence and speed"));
        list.Add(Scroll(ScrFrenzyL, "Scroll of Frenzy (Grand)", Rung(FamFrenzy, 4),
            SkillEffect.BuffPhysAtk, "−18% Max HP/MP but +7% offence and speed"));
        list.Add(Scroll(ScrFrenzyM, "Scroll of Frenzy (Supreme)", Rung(FamFrenzy, 6),
            SkillEffect.BuffPhysAtk, "−10% Max HP/MP but +8% offence and speed"));

        // ===== What a buffer CLASS casts: one skill per family, one level per rung =====
        // The level descriptions are the rungs' own, read back out of what we just built, so a
        // ladder value is written down exactly once.
        string Text(string childId) =>
            (list.FirstOrDefault(s => s.Id == childId)
             ?? alreadyBuilt.FirstOrDefault(s => s.Id == childId))?.Description ?? "";
        string[] Rungs(string family, int n) => Enumerable.Range(1, n).Select(i => Rung(family, i)).ToArray();

        void Castable(string family, string name, SkillEffect effect, string[] children, string what) =>
            list.Add(CastSingle(family, name, effect, children, Text,
                $"Blesses an ally (or self) with {what} for 20 minutes."));

        Castable(FamPhysAtk, "Might", SkillEffect.BuffPhysAtk, Rungs(FamPhysAtk, 3), "more Physical Attack");
        Castable(FamPhysDef, "Bulwark", SkillEffect.BuffDef, Rungs(FamPhysDef, 3), "more Physical Defence");
        Castable(FamMagAtk, "Force", SkillEffect.BuffMagAtk, Rungs(FamMagAtk, 3), "more Magic Attack");
        Castable(FamMagDef, "Ward", SkillEffect.BuffMagicDef, Rungs(FamMagDef, 3), "more Magic Defence");
        Castable(FamAccuracy, "Aim", SkillEffect.BuffAccuracy, Rungs(FamAccuracy, 3), "a steadier hand");
        Castable(FamVamp, "Vampirism", SkillEffect.BuffMeleeVamp, Rungs(FamVamp, 3), "melee attacks that heal");
        Castable(FamInterrupt, "Resolve", SkillEffect.BuffInterruptResist, Rungs(FamInterrupt, 4), "casting that is harder to cancel");
        Castable(FamCritRate, "Focus", SkillEffect.BuffCritRate, Rungs(FamCritRate, 6), "a higher critical rate");
        Castable(FamCritDmg, "Ferocity", SkillEffect.BuffCritDamage, Rungs(FamCritDmg, 6), "heavier criticals");
        Castable(FamMagCrit, "Insight", SkillEffect.BuffMagicCritRate, Rungs(FamMagCrit, 6), "more magic criticals");
        Castable(FamMaxHp, "Body", SkillEffect.BuffHp, Rungs(FamMaxHp, 6), "more Max HP");
        Castable(FamMaxMp, "Soul", SkillEffect.BuffMp, Rungs(FamMaxMp, 6), "more Max MP");
        Castable(FamHpRegen, "Vigor", SkillEffect.BuffHpRegen, Rungs(FamHpRegen, 6), "faster HP regeneration");
        Castable(FamMpRegen, "Serenity", SkillEffect.BuffMpRegen, Rungs(FamMpRegen, 6), "faster MP regeneration");
        // The speed four shipped first, so their rungs are named rather than numbered.
        Castable(FamMove, "Swift", SkillEffect.BuffMoveSpeed, new[] { BuffSwiftC, BuffSwiftU, BuffSwiftR }, "more Move Speed");
        Castable(FamCast, "Alacrity", SkillEffect.BuffCastSpeed, new[] { BuffAlacrityC, BuffAlacrityU, BuffAlacrityR }, "faster casting");
        Castable(FamEva, "Agility", SkillEffect.BuffEvasion, new[] { BuffAgilityC, BuffAgilityU, BuffAgilityR }, "more Evasion");
        Castable(FamAs, "Haste", SkillEffect.BuffAtkSpeed, new[] { BuffHasteC, BuffHasteU, BuffHasteR }, "faster attacks");
        // Frenzy's castable single already exists as the cleric's `holy_frenzy` (Skills.Healer.cs) —
        // it was a wrapper over one family before this, so it needed no second copy.

        return list.ToArray();
    }
}
