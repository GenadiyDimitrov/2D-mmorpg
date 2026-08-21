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
    // Aim is accuracy's ladder and it is the exact mirror of Agility's (evasion): 1 / 2 / 3 / 4, its
    // own potion and its own scroll. Hit and evasion are the two halves of one contest, so a player
    // who can buy one must be able to buy the other (owner 2026-07-31) — and when his healer file
    // added a +3 rung to one on 2026-08-20, the other took it in the same pass for the same reason.
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

    // ---- CONTROL RESISTANCE, one family per SCHOOL (owner 2026-08-17, the healer's level-40 pair).
    //      They are two families and not one because the schools are defended by different stats —
    //      SPT for magical, CON for physical — so "hard to lock down" is two different investments,
    //      and a party can be blessed against the threat it actually faces. ----
    public const string FamCcResMag  = "cc_res_mag";   // Clarity   — % resist vs SPT-defended debuffs
    public const string FamCcResPhys = "cc_res_phys";  // Fortitude — % resist vs CON-defended debuffs

    // ---- THE SHIELD PAIR (owner, 2026-08-19). Two families, because his group at buffer 66 is
    //      "Shield Bless AND Harden" and *"it should rapladse Shield Harder and Shield Bless"* — a
    //      group names the singles it contains, so the singles have to exist for it to be one.
    //      ✅ BOTH ARE FULL LADDERS since 2026-08-20 — the healer authors Shield Blessing at 5/10/15/20/
    //      25/30% (@40-70) and Shield Hardening at 30/40/50% (@58/66/72), and the group now points at the
    //      TOP rung of each rather than at a lone placeholder. ⚠ He had them REVERSED in an earlier
    //      draft and fixed it himself in the 40 row's comment (*"Changed to the rate increase buff
    //      (harden at 58); my mistake to have them reversed"*): Bless is the block CHANCE, Harden the
    //      shield's P.Def. Nothing in either family ever raises block REDUCTION (playtest 22).
    public const string FamShieldDef   = "shield_def";     // Shield Hardening — % shield P.Def
    public const string FamShieldBlock = "shield_block";   // Shield Blessing — % block CHANCE (never reduction)

    // ---------------------------------------------------------------------------------------
    //  Single-buff ids. `Rung(family, n)` builds them, so these consts are only for the places
    //  that name one directly (class-buff child lists, the NPC buffer).
    // ---------------------------------------------------------------------------------------
    public static string Rung(string family, int rank) => $"buff_{family}_{rank}";

    public const string BuffPAtk1 = "buff_atk_phys_1", BuffPAtk2 = "buff_atk_phys_2", BuffPAtk3 = "buff_atk_phys_3";
    public const string BuffPDef1 = "buff_def_phys_1", BuffPDef2 = "buff_def_phys_2", BuffPDef3 = "buff_def_phys_3";
    // ⚠⚠ SIX LADDERS GREW A MIDDLE RUNG on 2026-08-20, when his `healer 3rd.csv` authored values that
    // sat BETWEEN two existing ones (M.Atk 28, M.Def 23, accuracy 3, evasion 3, vampirism 7 and 8,
    // interrupt 36/42/48). A family's rung index IS its rank, so the only monotonic way to hold a new
    // middle value is to renumber everything above it — which is why the "top" const of each of those
    // families is no longer the number you would guess from its name.
    //
    // 🔑 What did NOT change: the family's TOP VALUE, and every consumer of it. The Greater potions,
    // the NPC buffer, the improved groups and the Harmonies all still hand out exactly what they did
    // — they just name a higher index to do it. Read a const's COMMENT, never its number.
    // ⚠ Ids are append-only and none was reused: `buff_atk_mag_3` still exists, it just means 28% now.
    // Nothing persists a rung id (buffs die with the session), so no character carries a stale one.
    public const string BuffMAtk1 = "buff_atk_mag_1",  BuffMAtk2 = "buff_atk_mag_2";
    public const string BuffMAtk3 = "buff_atk_mag_3",  BuffMAtk4 = "buff_atk_mag_4";   // 28% / 32% ← TOP
    public const string BuffMDef1 = "buff_def_mag_1",  BuffMDef2 = "buff_def_mag_2";
    public const string BuffMDef3 = "buff_def_mag_3",  BuffMDef4 = "buff_def_mag_4";   // 23% / 30% ← TOP

    public const string BuffVamp1 = "buff_vamp_1", BuffVamp2 = "buff_vamp_2", BuffVamp3 = "buff_vamp_3";
    public const string BuffVamp4 = "buff_vamp_4", BuffVamp5 = "buff_vamp_5";          //  8% /  9% ← TOP
    public const string BuffAcc1  = "buff_accuracy_1", BuffAcc2 = "buff_accuracy_2";
    public const string BuffAcc3  = "buff_accuracy_3", BuffAcc4 = "buff_accuracy_4";   // +3 / +4 ← TOP
    public const string BuffIntr1 = "buff_interrupt_1", BuffIntr2 = "buff_interrupt_2";
    public const string BuffIntr3 = "buff_interrupt_3", BuffIntr4 = "buff_interrupt_4";  // 36 / 40
    public const string BuffIntr5 = "buff_interrupt_5", BuffIntr6 = "buff_interrupt_6";  // 42 / 48
    public const string BuffIntr7 = "buff_interrupt_7";                                  // 54
    // public const string BuffIntr8 = "buff_interrupt_8";                               // 60 — PARKED

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
    /// <summary>One rung of a control-resistance family. Like <see cref="FrenzyRung"/> this cannot go
    /// through <see cref="Ladder"/>, because its payload is a FIELD rather than an EffectMagnitude — the
    /// SkillEffect flag enum has no bits left, so per-school CC resistance rides on SkillDef fields.
    /// Carries no Effect at all: it changes nothing until a debuff is rolled against its holder.</summary>
    private static SkillDef CcResistRung(string family, string name, int rank,
        float magical = 0f, float physical = 0f)
    {
        string what = magical > 0f ? "magical (SPT)" : "physical (CON)";
        float v = magical > 0f ? magical : physical;
        return new SkillDef(Rung(family, rank), name, BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            BuffKey: family, Rank: rank, Category: SkillCategory.Buff,
            CcResistMagical: magical, CcResistPhysical: physical,
            Description: $"+{v * 100:0.#}% resistance to {what} debuffs.");
    }

    /// <summary>One rung of Frenzy: ONE `gain` drives P.Atk, M.Atk, cast speed and attack speed alike,
    /// against a slightly larger `penalty` on Max HP/MP.
    ///
    /// <para>🔑 A `magGain` parameter existed here for part of 2026-08-20, because his then-current row
    /// read "pAtk x1.05, mAtk x1.1" and one shared gain could not say so. He re-authored the skill the
    /// same day and the two channels are equal again at every rung, so the parameter went. Don't
    /// reintroduce it on a hunch — reintroduce it if a row splits the channels again.</para></summary>
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

    /// <summary>The price of ONE rung, as authored in his CSVs: the `MP` total and the `SP`. A zero
    /// field means "nobody has authored this rung" and falls back to the formula below.
    ///
    /// ⚠ 2026-08-20: this used to carry the sheet's `INIT MP` as a third number. His CSVs collapsed to a
    /// single MP column that day and the engine splits every skill 20/80 (`SkillMath.InitialMpFraction`),
    /// so the up-front slice is no longer a per-rung authoring decision.
    ///
    /// ⚠ These exist because the 2026-08-19 pass on `cleric 2nd.csv` re-priced the buffs by the LEVEL
    /// THEY ARE LEARNED AT (20 → 20 MP, 25 → 26, 30 → 33, 35 → 40) rather than by their rung, which is
    /// what the `30 + 20·i/(n−1)` formula assumed. The two readings cannot both be true — Focus L4 is
    /// 42 by formula and 26 in his sheet — so the sheet wins and the formula is only a default for the
    /// rungs no CSV has reached yet. Don't "regularise" a ladder that looks irregular here: it looks
    /// that way because a cleric rung and a buffer rung were priced on different days.</summary>
    private readonly record struct RungCost(int Total, int Sp);

    /// <summary>One castable single buff, with a level per rung of its family. Where his CSV prices a
    /// rung, `costs` carries that price verbatim; where it does not, MP climbs across the rungs from
    /// 30 to 50 — the ceiling is the owner's, and it is what makes the improved group (150+) the
    /// efficient choice for a class that has one.</summary>
    /// <param name="children">The family's rungs, weakest first — one skill LEVEL each. Passed in
    /// rather than derived, because the speed families shipped first and their ids are named
    /// (`buff_swift_c`), not numbered.</param>
    /// <param name="text">The rung's own description, used as the level's.</param>
    /// <param name="costs">Authored per-rung prices, weakest first. May be shorter than the ladder,
    /// and any zero field falls back to the formula.</param>
    private static SkillDef CastSingle(string family, string name, SkillEffect effect,
        string[] children, Func<string, string> text, string desc, RungCost[]? costs = null)
    {
        int n = children.Length;
        var levels = new SkillLevel[n];
        for (int i = 0; i < n; i++)
        {
            var c = costs is not null && i < costs.Length ? costs[i] : default;
            int mp = c.Total > 0 ? c.Total
                   : n == 1 ? SingleBuffMpLow
                   : SingleBuffMpLow + (SingleBuffMpHigh - SingleBuffMpLow) * i / (n - 1);
            levels[i] = new SkillLevel(MpCost: mp,
                SpCost: c.Sp > 0 ? c.Sp : BuffSpCosts[Math.Min(i, BuffSpCosts.Length - 1)],
                ChildBuffs: new[] { children[i] }, Description: text(children[i]));
        }
        // The def's own MpCost/SpCost are level 1's — a skill with Levels reads them per level, but
        // anything that looks at the def alone (the learn list, a tooltip before you own it) sees
        // rung 1, which is what it would be handed.
        return new SkillDef(CastId(family), name, BaseClass.Mage, effect,
            MpCost: levels[0].MpCost, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000,
            ChildBuffs: new[] { children[0] },
            Category: SkillCategory.Buff, SpCost: levels[0].SpCost,
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
        // M.Atk tops out at the NPC buffer's 32%; rung 2 is the cleric's 25% (`cleric 2nd.csv` @25)
        // and rung 3 the healer's 28% (`healer 3rd.csv` @44). Percent M.Atk is authored at the
        // EFFECTIVE value (see docs — magic-buff authoring).
        list.AddRange(Ladder(FamMagAtk,  "Force",   SkillEffect.BuffMagAtk,  ModifierMode.Percent, "M.Atk", 0.15f, 0.25f, 0.28f, 0.32f));
        // Ward: 10% is the cleric's @35, 23% the healer's @44, 30% his @52 (and the NPC buffer's).
        list.AddRange(Ladder(FamMagDef,  "Ward",    SkillEffect.BuffMagicDef,ModifierMode.Percent, "M.Def", 0.10f, 0.20f, 0.23f, 0.30f));
        // Aim mirrors Agility exactly — the two sides of the hit/evade contest cost the same. Both
        // grew a +3 rung for his healer @44/@48; see the note on BuffAgility3 in Skills.Common.cs.
        list.AddRange(Ladder(FamAccuracy,"Aim",     SkillEffect.BuffAccuracy,ModifierMode.Flat,    "Accuracy", 1, 2, 3, 4));

        // ===== No-consumable families — class buffs only =====
        list.AddRange(Ladder(FamVamp,     "Vampirism", SkillEffect.BuffMeleeVamp,      ModifierMode.Percent, "melee vampirism", 0.03f, 0.06f, 0.07f, 0.08f, 0.09f));
        // ⚠ 40 IS NOT ONE OF HIS NUMBERS and is kept anyway. His healer authors 36 / 42 / 48 (@44/52/60)
        // between the cleric's 25 and what was then the top of 60 — but 40 was already the rung `Force and Ward`
        // levels 3-4 hand out, and dropping it would silently retune a group nobody has re-authored.
        // A ladder may carry a rung no CSV names; it may not carry one that goes backwards.
        // ⚠ 2026-08-21: the old top rung of 60 is GONE from the ladder — his ruling, *"54 is max resolve
        // for now .. no1 is learning it atm"*. It is parked, not deleted: `BuffIntr8` above is the id it
        // would reclaim, and re-adding 60 here is the whole change. Everything that used to hand out 60
        // — the NPC buffer, `Force and Ward`, the Warchanter's row at 52 — now hands out his 54.
        list.AddRange(Ladder(FamInterrupt,"Resolve",   SkillEffect.BuffInterruptResist,ModifierMode.Flat,    "interrupt resistance", 18, 25, 36, 40, 42, 48, 54));

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
        // ===== Control resistance, per school — only the rungs he has authored =====
        // Every rung below is his: `cleric 2nd.csv` @25 for Clarity's first, `healer 3rd.csv` for the
        // other seven. ⚠ Inventing rungs to fill a ladder out is exactly what BL-02 forbids, and a
        // resist ladder is not a curve anyone can guess — the two schools do not even share a shape.
        // ===== The shield pair — full ladders since his healer file authored the singles =====
        // Both are PERCENT of what the shield already carries, which makes them free of a "do you have
        // a shield" check: a bare arm has 0 shield defence and 0 block chance, and 0 × 1.5 is still 0.
        //
        // ✅ BOTH ARE REAL LADDERS SINCE 2026-08-20 — the singles he promised (*"im now authoring its
        // singles in the healers file"*) landed in `healer 3rd.csv`, so the one-rung placeholders are
        // gone. Shield Blessing climbs 5 → 30% over six rungs (40/48/56/62/66/70) and Shield Hardening
        // 30 → 50% over three (58/66/72). The TOP of each is unchanged, which is what keeps
        // `Shield Bless and Harden` (the buffer's group @66) handing out exactly what it always did.
        list.AddRange(Ladder(FamShieldDef,   "Shield Hardening", SkillEffect.BuffShieldDef,   ModifierMode.Percent, "shield P.Def", 0.30f, 0.40f, 0.50f));
        list.AddRange(Ladder(FamShieldBlock, "Shield Blessing",  SkillEffect.BuffBlockChance, ModifierMode.Percent, "block chance", 0.05f, 0.10f, 0.15f, 0.20f, 0.25f, 0.30f));

        // Clarity: 20% is the cleric's @25; 30/40/50 are the healer's @40/48/56.
        list.Add(CcResistRung(FamCcResMag, "Clarity", 1, magical: 0.20f));
        list.Add(CcResistRung(FamCcResMag, "Clarity", 2, magical: 0.30f));
        list.Add(CcResistRung(FamCcResMag, "Clarity", 3, magical: 0.40f));
        list.Add(CcResistRung(FamCcResMag, "Clarity", 4, magical: 0.50f));
        // Fortitude is the healer's alone — 15/20/30/40% at 40/52/64/72. ⚠ It climbs SLOWER than
        // Clarity and ends lower on purpose: the physical school is the one CON already defends, and
        // his two 40-level rows (30% vs 15%) set that gap on the first rung.
        list.Add(CcResistRung(FamCcResPhys, "Fortitude", 1, physical: 0.15f));
        list.Add(CcResistRung(FamCcResPhys, "Fortitude", 2, physical: 0.20f));
        list.Add(CcResistRung(FamCcResPhys, "Fortitude", 3, physical: 0.30f));
        list.Add(CcResistRung(FamCcResPhys, "Fortitude", 4, physical: 0.40f));

        // ═══ RUNGS 1 AND 2 ARE HIS, verbatim (2026-08-20) ═══════════════════════════════════════════
        //   L1, cleric @35:            −7% HP/MP, +5% P/M.Atk, +5% atk/cast speed, +5 move, −5 eva
        //   L2, healer + buffer @52:  −10% HP/MP, +8% P/M.Atk, +8% atk/cast speed, +8 move, −8 eva
        //
        // 🔑 WHY THESE NUMBERS AND NOT IG'S: IG's Frenzy reads +16% M.Atk / +8% P.Atk, but IG applies
        // magic buffs under a √ — √1.16 = ×1.077 — so its real effect is ~7.7%. Ours stores the HONEST
        // percent (Entity squares BuffMagAtk precisely so the stored number is what lands), which is why
        // 8% here equals 16% there. His words: *"the IG is sqrt(1.16) so x1.077 which is 7.7% .. we need
        // to make it 8% increase in Matk and Patk .. we fixed the Force (IG-75% == Our-32%) and forgot
        // the frenzy"*. ⚠ Copying an IG percentage straight into a magic buff DOUBLES it. Force was
        // converted; Frenzy was missed, and rung 1 sat at −30% Max HP/MP for a +5% return.
        //
        // ⚠ RUNGS 3-7 BELOW ARE OURS and are now OUT OF LINE with these two — rung 3 gives +6% for −22%
        // Max HP/MP, which is strictly worse than rung 2's +8% for −10%. That is not academic: a buffer
        // learns rung 3 at 62, and ApplyBuff replaces on rank ≥ rank, so the weaker buff EVICTS the
        // stronger one he had at 52. Left as-is rather than invented over (they are also the three
        // Frenzy SCROLLS, ranks 2/4/6) — see the note in the CHANGELOG. His call.
        list.Add(FrenzyRung(1, 0.07f, 0.05f, 5, eva: 5));
        list.Add(FrenzyRung(2, 0.10f, 0.08f, 8, eva: 8));
        list.Add(FrenzyRung(3, 0.22f, 0.06f, 6));
        list.Add(FrenzyRung(4, 0.18f, 0.07f, 7));
        list.Add(FrenzyRung(5, 0.14f, 0.07f, 7));
        list.Add(FrenzyRung(6, 0.10f, 0.08f, 8));
        // Rung 7 — the TOP of the family. ⚠ NOTHING HANDS IT OUT ANY MORE: it was invented for `Madness`
        // at level 76, and on 2026-08-21 that skill became `War Frenzy` at 56 handing out RUNG 2 (his
        // row). A ladder may carry a rung no CSV names — Resolve does too — but do not read this as
        // live content. Nothing
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
        Pair(PotForceR, ScrForceR, "Force Potion (Greater)", "Scroll of Force (Greater)", BuffMAtk4, SkillEffect.BuffMagAtk, "+32% M.Atk");
        Pair(PotWardC, ScrWardC, "Ward Potion (Lesser)",  "Scroll of Ward (Lesser)",  BuffMDef1, SkillEffect.BuffMagicDef, "+10% M.Def");
        Pair(PotWardU, ScrWardU, "Ward Potion",           "Scroll of Ward",           BuffMDef2, SkillEffect.BuffMagicDef, "+20% M.Def");
        Pair(PotWardR, ScrWardR, "Ward Potion (Greater)", "Scroll of Ward (Greater)", BuffMDef4, SkillEffect.BuffMagicDef, "+30% M.Def");
        Pair(PotAimC, ScrAimC, "Aim Potion (Lesser)",  "Scroll of Aim (Lesser)",  Rung(FamAccuracy, 1), SkillEffect.BuffAccuracy, "+1 Accuracy");
        Pair(PotAimU, ScrAimU, "Aim Potion",           "Scroll of Aim",           Rung(FamAccuracy, 2), SkillEffect.BuffAccuracy, "+2 Accuracy");
        Pair(PotAimR, ScrAimR, "Aim Potion (Greater)", "Scroll of Aim (Greater)", BuffAcc4, SkillEffect.BuffAccuracy, "+4 Accuracy");

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

        void Castable(string family, string name, SkillEffect effect, string[] children, string what,
                      RungCost[]? costs = null) =>
            list.Add(CastSingle(family, name, effect, children, Text,
                $"Blesses an ally (or self) with {what} for 20 minutes.", costs));

        // The prices below are read straight off his sheets — `mage 1st.csv` (rungs a base mage buys),
        // `cleric 2nd.csv` (2026-08-19), `healer 3rd.csv` (2026-08-20) and `buffer 3rd.csv`. A
        // `default` entry is a rung no CSV has priced yet and keeps the 30→50 formula. See RungCost
        // for why the two schemes disagree.
        //
        // 🔑 A rung's price is the price of the LEVEL ITS OWNER LEARNS IT AT, and the healer file makes
        // that unmistakable: every 40-level buff row is 60 MP / 19k SP, every 44 row 72 / 22k, every 52
        // row 80 / 38k, and so on to 125 / 330k at 72. So a family's cost array is NOT a curve — it is
        // whatever levels happened to claim those rungs. Do not smooth one.
        RungCost[] C(params RungCost[] c) => c;
        RungCost R(int total, int sp) => new(total, sp);
        // The healer's per-LEVEL buff price, so his rows read as the level he wrote them at rather
        // than as two loose numbers. (Serenity @40 is the one row that breaks the table: 80 / 32k.)
        RungCost H40 = R(60, 19000),  H44 = R(72, 22000),  H48 = R(75, 32000),  H52 = R(80, 38000);
        RungCost H56 = R(85, 42000),  H58 = R(90, 45000),  H60 = R(95, 61000),  H62 = R(100, 86000);
        RungCost H64 = R(105, 100000), H66 = R(110, 145000), H68 = R(115, 165000);
        RungCost H70 = R(120, 200000), H72 = R(125, 330000);

        Castable(FamPhysAtk, "Might", SkillEffect.BuffPhysAtk, Rungs(FamPhysAtk, 3), "more Physical Attack",
            C(R(20, 960), R(20, 1700), H40));
        Castable(FamPhysDef, "Bulwark", SkillEffect.BuffDef, Rungs(FamPhysDef, 3), "more Physical Defence",
            C(R(20, 960), R(26, 3200), H44));
        Castable(FamMagAtk, "Force", SkillEffect.BuffMagAtk, Rungs(FamMagAtk, 4), "more Magic Attack",
            C(default, R(26, 3200), H44, H52));
        // ⚠ Ward L1 is his cleric row at 35 and L2 his buffer row at 48: the same 40 MP, and the SP
        // actually goes DOWN (12800 → 6400). Both are authored; neither is a transcription slip here.
        Castable(FamMagDef, "Ward", SkillEffect.BuffMagicDef, Rungs(FamMagDef, 4), "more Magic Defence",
            C(R(40, 12800), R(40, 6400), H44, H52));
        Castable(FamAccuracy, "Aim", SkillEffect.BuffAccuracy, Rungs(FamAccuracy, 4), "a steadier hand",
            C(R(30, 6400), H40, H48, H56));
        Castable(FamVamp, "Vampirism", SkillEffect.BuffMeleeVamp, Rungs(FamVamp, 5), "melee attacks that heal",
            C(default, R(33, 6400), H44, H58, H72));
        // ⚠ Rung 4 (the un-authored 40) keeps the FORMULA price, so the array dips 72 → ~43 → 80 across
        // rungs 3-5. That is the honest reading: rungs 3/5/6 are the healer's 44/52/60 rows and rung 4
        // belongs to nobody. It is only reachable through `Force and Ward`, which prices itself.
        Castable(FamInterrupt, "Resolve", SkillEffect.BuffInterruptResist, Rungs(FamInterrupt, 7), "casting that is harder to cancel",
            C(R(20, 1700), R(26, 3200), H44, default, H52, H60, H68));
        Castable(FamCritRate, "Focus", SkillEffect.BuffCritRate, Rungs(FamCritRate, 6), "a higher critical rate",
            C(default, default, default, R(26, 3200), H44, H52));
        Castable(FamCritDmg, "Ferocity", SkillEffect.BuffCritDamage, Rungs(FamCritDmg, 6), "heavier criticals",
            C(default, default, R(38, 12800), H40, H48, H56));
        Castable(FamMagCrit, "Insight", SkillEffect.BuffMagicCritRate, Rungs(FamMagCrit, 6), "more magic criticals",
            C(default, R(34, 6400), H62, R(42, 25000), default, H70));
        // Body and Soul are the one pair the healer owns end to end — all six rungs are his rows.
        Castable(FamMaxHp, "Body", SkillEffect.BuffHp, Rungs(FamMaxHp, 6), "more Max HP",
            C(H44, H48, H52, H56, H64, H70));
        Castable(FamMaxMp, "Soul", SkillEffect.BuffMp, Rungs(FamMaxMp, 6), "more Max MP",
            C(H44, H48, H52, H56, H62, H70));
        Castable(FamHpRegen, "Vigor", SkillEffect.BuffHpRegen, Rungs(FamHpRegen, 6), "faster HP regeneration",
            C(default, R(40, 12800), default, H48, default, H56));
        // ⚠ Serenity rung 2 is his level-40 *"New insert"* row. It read 80 MP / 32k SP, out of line with
        // every other 40-level buff; he ruled it *"should match other buffs mp cost at its learning
        Castable(FamMpRegen, "Serenity", SkillEffect.BuffMpRegen, Rungs(FamMpRegen, 6), "faster MP regeneration",
            C(default, H40, default, H48, default, H56));
        // The speed four shipped first, so their rungs are named rather than numbered.
        Castable(FamMove, "Swift", SkillEffect.BuffMoveSpeed, new[] { BuffSwiftC, BuffSwiftU, BuffSwiftR }, "more Move Speed",
            C(default, R(20, 1700), R(33, 6400)));
        // ⚠ Rung 3 was UNPRICED until 2026-08-21 — the array stopped at two entries while the family has
        // three children, so +30% fell through to the 30→50 formula. His healer row at 48 prices it
        // (75 / 32k, the level's own price), which is why H48 appears here and not a hand-picked number.
        Castable(FamCast, "Alacrity", SkillEffect.BuffCastSpeed, new[] { BuffAlacrityC, BuffAlacrityU, BuffAlacrityR }, "faster casting",
            C(R(20, 1700), R(40, 12800), H48));
        Castable(FamEva, "Agility", SkillEffect.BuffEvasion, new[] { BuffAgilityC, BuffAgilityU, BuffAgility3, BuffAgilityR }, "more Evasion",
            C(default, R(33, 6400), H44, H52));
        // ⚠ Haste rung 1 is the healer's @44 row and rung 3 his @52 one; rung 2 (23%) belongs to nobody
        // and keeps the formula, so the array reads 72 / ~40 / 80. Same shape as Resolve above.
        Castable(FamAs, "Fury", SkillEffect.BuffAtkSpeed, new[] { BuffHasteC, BuffHasteU, BuffHasteR }, "faster attacks",
            C(H44, R(40, 6400), H52));
        // The two control-resistance blessings. `SkillEffect.None` because their payload is a field,
        // not a magnitude — the wrapper still buffs, it just has no flag that describes it.
        Castable(FamCcResMag, "Clarity", SkillEffect.None, Rungs(FamCcResMag, 4), "a mind harder to bind",
            C(R(26, 3200), H40, H48, H56));
        Castable(FamCcResPhys, "Fortitude", SkillEffect.None, Rungs(FamCcResPhys, 4), "a body harder to break",
            C(H40, H52, H64, H72));
        // The shield pair — LEARNABLE at last (his healer rows at 40-70 and 58-72 respectively). They
        // still double as the children of *Shield Bless and Harden*, the buffer's group at 66, which
        // now names each family's TOP rung.
        Castable(FamShieldDef, "Shield Hardening", SkillEffect.BuffShieldDef, Rungs(FamShieldDef, 3), "a sturdier shield",
            C(H58, H66, H72));
        Castable(FamShieldBlock, "Shield Blessing", SkillEffect.BuffBlockChance, Rungs(FamShieldBlock, 6), "a shield that catches more",
            C(H40, H48, H56, H62, H66, H70));
        // Frenzy's castable single already exists as the cleric's `holy_frenzy` (Skills.Healer.cs) —
        // it was a wrapper over one family before this, so it needed no second copy.

        return list.ToArray();
    }
}
