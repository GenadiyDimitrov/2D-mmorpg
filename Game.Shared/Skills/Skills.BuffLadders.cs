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

    // ---- Families with NO consumable at all: they exist only as children of a class buff.
    //      Rung count is free here (nothing has to line up with a rarity), so it is chosen to
    //      reproduce the values the cleric already casts today. ----
    public const string FamVamp      = "vamp";        // Vampirism — % melee vampirism
    public const string FamAccuracy  = "accuracy";    // Accuracy  — flat accuracy
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

    private static SkillDef[] BuffLadderSkills()
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

        // ===== No-consumable families — class buffs only =====
        list.AddRange(Ladder(FamVamp,     "Vampirism", SkillEffect.BuffMeleeVamp,      ModifierMode.Percent, "melee vampirism", 0.03f, 0.06f, 0.09f));
        list.AddRange(Ladder(FamAccuracy, "Accuracy",  SkillEffect.BuffAccuracy,       ModifierMode.Flat,    "Accuracy", 2, 3, 4));
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

        return list.ToArray();
    }
}
