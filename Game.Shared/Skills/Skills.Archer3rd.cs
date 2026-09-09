using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>THE ARCHER'S 3rd CLASS, 40-74 — every row of
/// `docs/data/classes_skills_csv/archer 3rd.csv`. Built 2026-09-09 on his word: *"build/fix rogue
/// 2nd, archer and duals 3rd"*.
///
/// <para>🔴 <b>IT REPLACES THE DERIVED KIT OF THREE DAYS AGO.</b> `BL-185` built Archer Bow Mastery,
/// Split Volley, a cloned Bow Expertise and Killing Focus off his *"take the elf harmonist skills and
/// bow passives … increase them with ~20%"* recipe, with a note saying his file would overwrite them.
/// It has. Those four are orphaned in Skills.ArcherKitRetired.cs and retired by `Replaces` clauses
/// below; nothing here is derived from anything.</para>
///
/// <para>🔑 <b>THE ARCHER IS A BUFF CLASS THAT SHOOTS.</b> Eleven of his eighteen families are
/// self-buffs, and they are structured as a ladder of commitments: two universal 20-minute ones
/// (Blessing = cheaper reuse, Spirit = cheaper MP), then ONE 5-minute race stance (Focus / Ferocity /
/// Swiftness — a big stat package with a 5% on-hit rider), then Bow Stance, which is the trade the
/// whole class points at: +15% on four offensive channels and +200 range for HALF YOUR MOVEMENT.</para>
///
/// <para>🔑 <b>RACE DECIDES THREE THINGS, and they line up.</b> Human = stun (Magic Arrow) + poison
/// (trap) + bleed (Focus). Elf = slow (Magic Arrow) + root (trap) + a self-heal (Swiftness), plus the
/// Antidote nobody else gets. Demon = an attack/defence curse (Magic Arrow) + bleed (trap) + poison
/// (Ferocity). Each race owns one trap, one Magic Arrow and one stance; nothing overlaps.</para>
///
/// <para>🔑 <b>ONE LADDER, HIS</b> — 40/43/46/49/52/55/58/60/62/64/66/68/70/72/74 and the same SP
/// schedule the tank's file runs on, so <see cref="BulwarkLevels"/> and <see cref="RogueSp"/> are
/// reused rather than copied. The buffs break it deliberately: Bow Expertise is one rung at 52, Bow
/// Stance one at 60, Signal Flare one at 60, and the six three-rung buffs all land 58/66/74.</para>
///
/// <para>⚠ <b>THE TRAPS ARE THE FIRST PLAYER TRAPS IN THE GAME.</b> `PlacesTrap` has existed since the
/// Trapper was sketched and nothing has ever authored one. All three are pure delivery — no damage,
/// only the rider — because his DESCR gives them none.</para>
/// </summary>
public static partial class SkillCatalog
{
    // ---- HIS `SKILL_ID` COLUMN, verbatim (`archer_explosive_arrows` is plural, `archer_bow_stence`
    //      is spelled that way; an id is a wire and save value and is never tidied after the fact).
    public const string ArcherArmorMastery  = "archer_armor_mastery";
    public const string BowWeaponMastery    = "bow_weapon_mastery";
    public const string ArcherBowBlessing   = "archer_bow_blessing";
    public const string ArcherBowSpirit     = "archer_bow_spirit";
    public const string ArcherBowFocus      = "archer_bow_focus";
    public const string ArcherBowFerocity   = "archer_bow_ferocity";
    public const string ArcherBowSwiftness  = "archer_bow_swiftness";
    public const string ArcherBowStance     = "archer_bow_stence";
    public const string ArcherTwinArrows    = "archer_twin_arrows";
    /// <summary>ONE ARROW of Twin Arrows — the sub-skill the wrapper fires twice. Never learned,
    /// never on a bar; see the note on the wrapper for why it exists at all.</summary>
    public const string ArcherTwinArrow     = "archer_twin_arrows_arrow";
    public const string ArcherExplosiveArrow = "archer_explosive_arrows";
    public const string ArcherBindingTrap   = "archer_binding_trap";
    public const string ArcherPoisonTrap    = "archer_poison_trap";
    public const string ArcherBleedTrap     = "archer_bleed_trap";
    public const string ArcherMagicArrowHuman = "archer_human_magic_arrow";
    public const string ArcherMagicArrowElf   = "archer_elf_magic_arrow";
    public const string ArcherMagicArrowDemon = "archer_demon_magic_arrow";

    // ---- The three stances' on-hit riders and Swiftness's self-heal. Payload defs: never learned,
    //      never on a bar, applied only by the proc machinery (the Sigils' shape).
    public const string ArcherFocusBleed1    = "archer_focus_bleed_1";
    public const string ArcherFocusBleed2    = "archer_focus_bleed_2";
    public const string ArcherFocusBleed3    = "archer_focus_bleed_3";
    public const string ArcherFerocityPoison1 = "archer_ferocity_poison_1";
    public const string ArcherFerocityPoison2 = "archer_ferocity_poison_2";
    public const string ArcherFerocityPoison3 = "archer_ferocity_poison_3";
    public const string ArcherSwiftnessMend1 = "archer_swiftness_mend_1";
    public const string ArcherSwiftnessMend2 = "archer_swiftness_mend_2";
    public const string ArcherSwiftnessMend3 = "archer_swiftness_mend_3";

    // ---- HIS LADDERS ----------------------------------------------------------------------------

    /// <summary>The MP ladder Twin Arrows, Explosive Arrow and the Binding Trap share.</summary>
    private static readonly int[] ArcherMp =
        { 72, 79, 85, 90, 94, 101, 107, 112, 116, 123, 129, 134, 140, 146, 152 };

    /// <summary>…and the one the Poison Trap, the Bleeding Trap and all three Magic Arrows share. It
    /// is the SAME twelve numbers and then THREE CHEAPER RUNGS — 138/145/151 against 140/146/152.
    /// ⚠ Two ladders where one would have done is his authoring, not a typo to smooth over: six
    /// families run on one column for twelve rungs and then split, and the three that stay cheaper are
    /// exactly the three that carry a rider rather than raw damage.</summary>
    private static readonly int[] ArcherMpLow =
        { 72, 79, 85, 90, 94, 101, 107, 112, 116, 123, 129, 134, 138, 145, 151 };

    /// <summary>Twin Arrows — power PER ARROW, and there are two of them (see the def).</summary>
    private static readonly int[] TwinArrowPower =
    {
        1000, 1300, 1600, 1900, 2100, 2400, 2700, 3000,
        3300, 3600, 3900, 4100, 4400, 4700, 5000,
    };

    /// <summary>Explosive Arrow and all three Magic Arrows share ONE power column, 500 → 2500. Half of
    /// Twin Arrows' per-arrow number, which is what pays for the area and the riders.</summary>
    private static readonly int[] ArcherArrowPower =
    {
        500, 650, 800, 950, 1050, 1200, 1350, 1500,
        1650, 1800, 1950, 2050, 2200, 2350, 2500,
    };

    /// <summary>Bow Mastery's flat P.Atk with a bow, 200 → 800.</summary>
    private static readonly int[] BowMasteryAtk =
        { 200, 233, 266, 300, 350, 400, 450, 500, 533, 566, 600, 650, 700, 750, 800 };

    /// <summary>…and its flat crit damage, 195 → 665.</summary>
    private static readonly int[] BowMasteryCritDmg =
        { 195, 222, 252, 285, 322, 362, 405, 435, 466, 499, 531, 565, 598, 632, 665 };

    /// <summary>The archer's armour evasion — FLAT 12 on all fifteen rungs. This one number is the
    /// ONLY difference between his archer and dual armour ladders (the melee rogue's climbs to 14),
    /// and it is why they are two skills rather than one shared one. Do not unify them.</summary>
    private const int ArcherArmorEva = 12;

    /// <summary>The three-rung levels every archer buff but Expertise and Stance uses, and their SP.
    /// 58 / 66 / 74, priced off the file's own ladder at those levels.</summary>
    private static readonly int[] BuffLevels3 = { 58, 66, 74 };
    private static readonly int[] BuffSp3     = { 88_000, 280_000, 880_000 };
    private static readonly int[] BuffMp3     = { 50, 75, 100 };

    /// <summary>Twenty minutes, his `DURRATION` cell on the two universal buffs and Bow Expertise.</summary>
    private const int TwentyMinutes = 12000;
    /// <summary>Five minutes — the three race stances. Short enough that holding one is a decision.</summary>
    private const int FiveMinutes = 3000;

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  THE SKILLS
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    private static SkillDef[] Archer3rdSkills()
    {
        var list = new List<SkillDef>();

        // ═══ ARMOR MASTERY — a SEPARATE skill that REPLACES the rogue's ══════════════════════════
        //
        // 🔑 THE ARCHER BRANCHES OFF, THE DAGGER BRANCH CONTINUES. His two files do opposite things
        //    with the same 2nd-class ladder: `dual 3rd.csv` keeps the id `rogue_armor_mastery` and
        //    appends rungs 6-20; `archer 3rd.csv` gives a NEW id and `REPLACES [rougue_armor_mastery]`.
        //    That works because every rung here is an ABSOLUTE profile — rung 1 at level 40 already
        //    states +28 P.Def, +12 evasion, the lot — so nothing is lost when the old skill goes.
        // ⚠ LIGHT ONLY, his WEIGHT column. A robe, plate or a bare torso pays nothing.
        list.Add(new SkillDef(ArcherArmorMastery, "Armor Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: RogueSp[0],
            Replaces: new[] { RogueArmorMastery, FighterArmorMastery },
            Description: "Passive. In LIGHT armor: more defence, evasion, speed and regeneration, and "
                       + "far less often critted. A robe or a bare torso gets nothing.",
            Levels: BulwarkRungs(i => new SkillLevel(SpCost: RogueSp[i],
                Description: $"With light armor: +{RogueArmorPDef(i)} P.Def, +{ArcherArmorEva} evasion, "
                           + $"+{RogueArmorSpeed(i):0} speed, {RogueArmorCritRes(i) * 100:0}% less often "
                           + $"critted, ×{1f + RogueArmorMpReg[i]:0.0} MP regen, "
                           + $"+{RogueArmorHpReg[i]:0.0} HP/s.")).Concat(ArcherFourthArmorMasteryRungs()).ToArray(),
            ArmorMasteryLevels: Enumerable.Range(0, BulwarkLevels.Length).Select(i =>
                new ArmorMasteryProfile(
                    Robe: default, None: default, Heavy: default,
                    Light: new StatMods(
                        PDef: RogueArmorPDef(i), Evasion: ArcherArmorEva,
                        CritRateResist: RogueArmorCritRes(i), MoveSpeed: RogueArmorSpeed(i),
                        MpRegenPct: RogueArmorMpReg[i], HpRegen: RogueArmorHpReg[i]))).
                Concat(ArcherFourthArmorMasteryProfiles()).ToArray()));

        // ═══ BOW MASTERY — the ranged branch's weapon passive ════════════════════════════════════
        //
        // ⚠ IT RETIRES THREE THINGS. `rogue_weapon_mastery` is his own REPLACES cell (and dropping the
        //   DUAL half of it is the point of choosing the bow branch); `archer_bow_mastery` and
        //   `archer_crit_focus` are the derived kit's, cleaned up here rather than left to stack a
        //   second bow passive and a permanent +20% crit damage on anyone who bought them.
        list.Add(new SkillDef(BowWeaponMastery, "Bow Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: RogueSp[0],
            Replaces: new[] { RogueWeaponMastery, ArcherBowMastery, ArcherCritFocus, FighterWeaponMastery },
            Description: "Passive. Your bow reaches far further and bites far harder. No effect with "
                       + "anything else in your hands.",
            Levels: BulwarkRungs(i => new SkillLevel(SpCost: RogueSp[i],
                Description: $"Bow: +{BowMasteryAtk[i]} P.Atk, +400 range, ×1.085 P.Atk, "
                           + $"+{BowMasteryCritDmg[i]} crit damage, +3 accuracy, ×1.2 crit rate, "
                           + $"×1.05 attack speed.")).Concat(ArcherFourthBowMasteryRungs()).ToArray(),
            WeaponMasteryLevels: Enumerable.Range(0, BulwarkLevels.Length).Select(i =>
                new WeaponMasteryProfile(
                    Bow: new PassiveEffect(
                        PhysAtk: BowMasteryAtk[i], PhysAtkPct: 0.085f, BowRange: 400f,
                        CritDamageFlat: BowMasteryCritDmg[i], Accuracy: 3,
                        CritRate: 0.20f, AtkSpeedPct: 0.05f)))
                .Concat(ArcherFourthBowMasteryProfiles()).ToArray()));

        // ═══ BOW BLESSING — cheaper physical reuse, twenty minutes ═══════════════════════════════
        // −10 / −15 / −20% on PHYSICAL reuse only (`PhysCooldownPct`), which is every skill an archer
        // owns. Requires a bow, like everything in this block.
        float[] blessing = { 0.10f, 0.15f, 0.20f };
        list.Add(new SkillDef(ArcherBowBlessing, "Bow Blessing", BaseClass.Fighter, SkillEffect.BuffCooldown,
            MpCost: BuffMp3[0], CastTicks: 30, CooldownTicks: 20, Range: 0, Power: 0,
            DurationTicks: TwentyMinutes, BuffKey: "archer_bow_blessing", Rank: 1,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
            RequiredWeapon: WeaponType.Bow, SpCost: BuffSp3[0],
            PhysCooldownPct: blessing[0],
            Description: "Twenty minutes of a steadier draw: your physical skills come back sooner.",
            Levels: Enumerable.Range(0, 3).Select(i => new SkillLevel(
                MpCost: BuffMp3[i], SpCost: BuffSp3[i], PhysCooldownPct: blessing[i],
                Description: $"−{blessing[i] * 100:0}% physical reuse for 20 minutes. Requires a bow.")).ToArray()));

        // ═══ BOW SPIRIT — cheaper physical MP, twenty minutes ════════════════════════════════════
        // −10 / −20 / −30%. ⚠ It runs through `EffectiveMpCost` like every other MP modifier, so the
        // number the player is quoted and the number he is charged are the same one.
        float[] spirit = { 0.10f, 0.20f, 0.30f };
        list.Add(new SkillDef(ArcherBowSpirit, "Bow Spirit", BaseClass.Fighter, SkillEffect.BuffMp,
            MpCost: BuffMp3[0], CastTicks: 30, CooldownTicks: 20, Range: 0, Power: 0,
            DurationTicks: TwentyMinutes, BuffKey: "archer_bow_spirit", Rank: 1,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
            RequiredWeapon: WeaponType.Bow, SpCost: BuffSp3[0],
            PhysMpCostPct: spirit[0],
            Description: "Twenty minutes of economy: your physical skills cost less to loose.",
            Levels: Enumerable.Range(0, 3).Select(i => new SkillLevel(
                MpCost: BuffMp3[i], SpCost: BuffSp3[i], PhysMpCostPct: spirit[i],
                Description: $"−{spirit[i] * 100:0}% MP on physical skills for 20 minutes. Requires a bow.")).ToArray()));

        // ═══ THE THREE RACE STANCES — five minutes, one at a time ════════════════════════════════
        //
        // 🔑 THEY SHARE A BUFF KEY on purpose. No race can hold two (nobody can — they are one per
        //    race), but sharing it means a future group buff or a potion competes with all three at
        //    once rather than with whichever one this character happens to own.
        // 🔑 EACH CARRIES A 5% ON-HIT RIDER, and those are the first procs in the game that pay the
        //    VICTIM. See SkillDef.ProcVictimRungs, and note that a proc on a BUFF only rolls while the
        //    buff is actually up — before 2026-09-09 every proc sat on a passive, where merely having
        //    learned it was the whole gate.
        // ⚠ HIS `atk.speed +10%` ON FOCUS AND FEROCITY vs `+15/20/25%` ON SWIFTNESS is the Elf's whole
        //   identity in this block: he trades the other two's growing crit or skill damage for speed.

        // --- HUMAN: Bow Focus. Accuracy and CRIT RATE climb; a bleed rides along.
        int[] focusAcc      = { 13, 15, 17 };
        float[] focusCrit   = { 0.15f, 0.20f, 0.25f };
        int[] focusAtk      = { 100, 150, 200 };
        list.Add(Stance(ArcherBowFocus, "Bow Focus",
            skillDmg: new[] { 0.10f, 0.10f, 0.10f },
            acc: focusAcc, critRate: focusCrit, critDmg: new[] { 0.10f, 0.10f, 0.10f },
            physAtk: focusAtk, atkSpeed: new[] { 0.10f, 0.10f, 0.10f }, moveSpeed: new[] { 3, 3, 3 },
            victimRungs: new[] { ArcherFocusBleed1, ArcherFocusBleed2, ArcherFocusBleed3 },
            rider: "a 5% chance to open a bleeding wound",
            blurb: "Five minutes of cold aim: everything sharpens, and your arrows leave the target bleeding."));

        // --- DEMON: Bow Ferocity. SKILL DAMAGE and CRIT DAMAGE climb; a poison rides along.
        list.Add(Stance(ArcherBowFerocity, "Bow Ferocity",
            skillDmg: new[] { 0.15f, 0.20f, 0.25f },
            acc: new[] { 10, 10, 10 }, critRate: new[] { 0.10f, 0.10f, 0.10f },
            critDmg: new[] { 0.15f, 0.20f, 0.25f },
            physAtk: focusAtk, atkSpeed: new[] { 0.10f, 0.10f, 0.10f }, moveSpeed: new[] { 3, 3, 3 },
            victimRungs: new[] { ArcherFerocityPoison1, ArcherFerocityPoison2, ArcherFerocityPoison3 },
            rider: "a 5% chance to poison what you hit",
            blurb: "Five minutes of malice: your shots hit far harder and carry venom with them."));

        // --- ELF: Bow Swiftness. ATTACK SPEED and MOVE SPEED climb; it mends you instead of hurting them.
        list.Add(Stance(ArcherBowSwiftness, "Bow Swiftness",
            skillDmg: new[] { 0.10f, 0.10f, 0.10f },
            acc: new[] { 10, 10, 10 }, critRate: new[] { 0.10f, 0.10f, 0.10f },
            critDmg: new[] { 0.10f, 0.10f, 0.10f },
            // 🔴 SPEED IS 5/7/10, his 2026-09-09 edit (*"archer 3rd changed the elf buff to a bit more
            //    ms"*) — it was 3/5/7 when the file was first read that morning. The Elf's stance is
            //    the only one whose move speed climbs at all, and this is what makes it the kiting one.
            physAtk: focusAtk, atkSpeed: new[] { 0.15f, 0.20f, 0.25f }, moveSpeed: new[] { 5, 7, 10 },
            victimRungs: new[] { ArcherSwiftnessMend1, ArcherSwiftnessMend2, ArcherSwiftnessMend3 },
            rider: "a 5% chance to mend your own wounds",
            blurb: "Five minutes of motion: faster on your feet and at the string, and the rhythm heals you.",
            payVictim: false));

        // The nine payloads. ⚠ THE BLEED AND POISON TIERS ARE 5 / 7 / 9, his cells — the same rank an
        // Antidote has to reach, and deliberately one step under the Venomweaver's top venom.
        int[] riderTier = { 5, 7, 9 };
        for (int i = 0; i < 3; i++)
        {
            list.Add(new SkillDef($"archer_focus_bleed_{i + 1}", "Bleeding", BaseClass.Fighter,
                SkillEffect.Bleed | SkillEffect.Slow,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 40 * (i + 1),
                DurationTicks: 100, BuffKey: "bleed", Rank: riderTier[i], SharesLadderKey: true,
                Category: SkillCategory.Debuff,
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.Slow, 0.15f) },
                Description: $"A bleeding wound (tier {riderTier[i]}) for 10s, and 15% slower."));

            list.Add(new SkillDef($"archer_ferocity_poison_{i + 1}", "Poisoned", BaseClass.Fighter,
                SkillEffect.Poison | SkillEffect.DebuffAtkSpeed | SkillEffect.DebuffCastSpeed,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 40 * (i + 1),
                DurationTicks: 100, BuffKey: "poison", Rank: riderTier[i], SharesLadderKey: true,
                Category: SkillCategory.Debuff,
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.DebuffAtkSpeed, 0.15f), new(SkillEffect.DebuffCastSpeed, 0.15f),
                },
                Description: $"Poison (tier {riderTier[i]}) for 10s, and 15% slower to act."));

            int[] mend = { 400, 450, 500 };
            list.Add(new SkillDef($"archer_swiftness_mend_{i + 1}", "Bow Swiftness", BaseClass.Fighter,
                SkillEffect.Heal,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: mend[i],
                Category: SkillCategory.Heal, TargetMode: TargetMode.SelfOnly,
                Description: $"Mends {mend[i]} of your own wounds."));
        }

        // ═══ BOW STANCE — the trade the class points at ══════════════════════════════════════════
        //
        // 🔑 +15% ON FOUR CHANNELS AND +200 RANGE FOR HALF YOUR MOVEMENT, sixty seconds, ten minutes'
        //    reuse. The move-speed half is a NEGATIVE `BuffMoveSpeed` magnitude, which is the idiom
        //    Meditation's −90% P.Def already uses: a self-inflicted penalty is a negative buff, not a
        //    debuff, so it never has to survive its own landing contest.
        // ⚠ The +200 reach needed a buff-side channel — `SkillDef.BuffBowRange`. Bow range had been
        //   passive-only since the masteries were written.
        list.Add(new SkillDef(ArcherBowStance, "Bow Stance", BaseClass.Fighter,
            SkillEffect.BuffPhysAtk | SkillEffect.BuffCritRate | SkillEffect.BuffCritDamage
            | SkillEffect.BuffPveSkillDamage | SkillEffect.BuffPvpSkillDamage
            | SkillEffect.BuffMoveSpeed,
            MpCost: 50, CastTicks: 10, CooldownTicks: 6000, Range: 0, Power: 0,
            DurationTicks: 600, BuffKey: "archer_bow_stance", Rank: 1,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
            RequiredWeapon: WeaponType.Bow, SpCost: 88_000,
            BuffBowRange: 200f,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffPhysAtk, 0.15f),
                new(SkillEffect.BuffCritRate, 0.15f),
                new(SkillEffect.BuffCritDamage, 0.15f),
                new(SkillEffect.BuffPveSkillDamage, 0.15f),
                new(SkillEffect.BuffPvpSkillDamage, 0.15f),
                new(SkillEffect.BuffMoveSpeed, -0.50f, ModifierMode.Percent),
            },
            Description: "Plant your feet for 60s: +15% attack power, critical rate, critical damage "
                       + "and physical skill power, and +200 bow range — at half your movement."));

        // ═══ TWIN ARROWS — the archer's main hand ════════════════════════════════════════════════
        //
        // ✅ `HitCount: 2` IS SETTLED AND IS NOT A BUG. His row is *"Shot two arrows each dealing
        //    +1000 power"*, and the two resolve INDEPENDENTLY — each rolls its own crit, its own
        //    evasion and its own block, which is worth less than one 2000 against a dodgy target and
        //    more against a shield. That distinction is why HitCount is a field, and it survived a
        //    round of "is the archer double-counted?" on 2026-09-09 by his own arithmetic.
        // ⚠ It also retires `archer_split_volley`, the derived skill it replaces.
        // 🔴 IT IS A WRAPPER NOW, NOT `HitCount: 2` — his ruling of 2026-09-09, made while choosing
        //    Arrow Barrage's shape: *"this will change the twin arrow skill to same logic (1 wrapper
        //    and while cast just cast 2 times same skill per arrow)"*. ✅ The BEHAVIOUR he settled is
        //    unchanged — two arrows, each resolving on its own — and `HitCount: 2` already delivered
        //    that. What the wrapper buys is that the two archer volleys are ONE mechanism instead of
        //    two: an arrow is an arrow, whether two of them fly or ten.
        // ⚠ The sub-skill is `archer_twin_arrows_arrow`, and its POWER is the rung's. A channel's
        //   shots resolve at the wrapper's level (ExecuteSkill's `levelOverride`), so the ladder lives
        //   on the wrapper exactly as it did — nothing had to be duplicated onto the arrow.
        list.Add(new SkillDef(ArcherTwinArrows, "Twin Arrows", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: ArcherMp[0], CastTicks: 30, CooldownTicks: 50, Range: 900, Power: TwinArrowPower[0],
            Category: SkillCategory.Physical, SpCost: RogueSp[0],
            RequiredWeapon: WeaponType.Bow,
            // Two arrows, 200ms apart — the same cadence Arrow Barrage fires at, because it is the
            // same act done fewer times.
            ChannelSkill: ArcherTwinArrow, ChannelShots: 2, ChannelIntervalTicks: 2,
            Replaces: new[] { PreciseShot, ArcherSplitVolley },
            Description: "Two arrows on one breath — each finds its own way in.",
            Levels: BulwarkRungs(i => new SkillLevel(
                Power: TwinArrowPower[i], MpCost: ArcherMp[i], SpCost: RogueSp[i],
                Description: $"Looses 2 arrows, each for power {TwinArrowPower[i]:N0}."))
                .Concat(ArcherFourthTwinArrowRungs()).ToArray()));

        // ONE ARROW of Twin Arrows. No area (his AOE cell is 0, unlike Barrage's 150), no MP of its
        // own, never learned, never on a bar. Its POWER is 0 because the rung's power comes from the
        // wrapper — see the note above.
        list.Add(new SkillDef(ArcherTwinArrow, "Twin Arrows", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 900, Power: 0,
            Category: SkillCategory.Physical,
            RequiredWeapon: WeaponType.Bow,
            Description: "One arrow of a twin volley."));

        // ═══ EXPLOSIVE ARROW — the archer's AoE ══════════════════════════════════════════════════
        // `target/aoe`: thrown 600 and detonating 200 around whatever it lands on (AreaAtTarget), NOT
        // a circle on the archer's own feet. His one-second reuse makes it the rotation filler.
        list.Add(new SkillDef(ArcherExplosiveArrow, "Explosive Arrow", BaseClass.Fighter,
            SkillEffect.PhysicalDamage,
            MpCost: ArcherMp[0], CastTicks: 30, CooldownTicks: 10, Range: 600, Power: ArcherArrowPower[0],
            Category: SkillCategory.Physical, SpCost: RogueSp[0],
            AreaRadius: 200f, AreaAtTarget: true, TargetMode: TargetMode.EnemiesInRadius,
            RequiredWeapon: WeaponType.Bow,
            Description: "An arrow that bursts where it lands, catching everything within 200.",
            Levels: BulwarkRungs(i => new SkillLevel(
                Power: ArcherArrowPower[i], MpCost: ArcherMp[i], SpCost: RogueSp[i], AreaRadius: 200f,
                Description: $"Bursts for power {ArcherArrowPower[i]:N0} on everything within 200."))
                .Concat(ArcherFourthExplosiveArrowRungs()).ToArray()));

        // ═══ THE THREE TRAPS — one per race ══════════════════════════════════════════════════════
        //
        // 🔑 A TRAP IS DROPPED, NOT THROWN. `PlacesTrap` puts it at the caster's feet, it arms, and the
        //    first hostile within `TrapRadius` takes this skill's effect and the trap vanishes. Thirty
        //    seconds to wait, thirty seconds of reuse — so an archer can hold exactly one.
        // ⚠ NO DAMAGE, on his cells: all three DESCRs describe only the rider. The trap is delivery.
        list.Add(Trap(ArcherBindingTrap, "Binding Trap", SkillEffect.Root, DebuffSchool.Physical,
            tiers: null, mp: ArcherMp,
            "Holds everything it catches where it stands for 30s.",
            i => "Holds everything within 400 of the trap for 30s when it springs."));

        int[] poisonTier = { 3, 3, 4, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 10 };
        list.Add(Trap(ArcherPoisonTrap, "Poison Trap", SkillEffect.Poison, DebuffSchool.Magical,
            tiers: poisonTier, mp: ArcherMpLow,
            "Poisons everything it catches.",
            i => $"Poisons everything within 400 of the trap (tier {poisonTier[i]}) for 30s when it springs."));

        int[] bleedTier = { 3, 3, 4, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 10 };
        list.Add(Trap(ArcherBleedTrap, "Bleeding Trap", SkillEffect.Bleed, DebuffSchool.Physical,
            tiers: bleedTier, mp: ArcherMpLow,
            "Opens wounds on everything it catches.",
            i => $"Bleeds everything within 400 of the trap (tier {bleedTier[i]}) for 30s when it springs."));

        // ═══ THE THREE MAGIC ARROWS — one per race, one power column ═════════════════════════════
        //
        // ⚠ ALL THREE ARE PHYSICAL, on his TYPE cell (`Physical/Active`) and his own parenthesis
        //   (*"+500 power (physical)"*). "Magic Arrow" is the NAME, not the channel — an archer has no
        //   magic. So they are paced by attack speed and contested on CON, not SPT.
        list.Add(MagicArrow(ArcherMagicArrowHuman, SkillEffect.Stun, durationTicks: 50,
            Array.Empty<EffectMagnitude>(),
            "An arrow to the temple: the target drops where it stands.",
            "and stuns it for 5s"));

        list.Add(MagicArrow(ArcherMagicArrowElf, SkillEffect.Slow | SkillEffect.DebuffAtkSpeed,
            durationTicks: 150,
            new EffectMagnitude[]
            {
                new(SkillEffect.Slow, 0.30f), new(SkillEffect.DebuffAtkSpeed, 0.30f),
            },
            "An arrow that drags: the target slows to a crawl and swings like it.",
            "and cuts its attack and move speed by 30% for 15s"));

        list.Add(MagicArrow(ArcherMagicArrowDemon, SkillEffect.DebuffAtk | SkillEffect.DebuffDef,
            durationTicks: 150,
            new EffectMagnitude[]
            {
                new(SkillEffect.DebuffAtk, 0.30f), new(SkillEffect.DebuffDef, 0.30f),
            },
            "An arrow that unmakes: the target's guard and its blows both fail it.",
            "and cuts its P.Def and P.Atk by 30% for 15s"));

        return list.ToArray();
    }

    /// <summary>One of the three race stances. They differ only in which columns climb; everything
    /// else — three rungs at 58/66/74, five minutes, a bow, the shared buff key, the 5% rider — is
    /// identical by construction.</summary>
    /// <param name="payVictim">false for the Elf, whose rider heals HIM rather than hurting them, and
    /// so rides <see cref="SkillDef.ProcSelfRungs"/> instead.</param>
    private static SkillDef Stance(string id, string name,
                                   float[] skillDmg, int[] acc, float[] critRate, float[] critDmg,
                                   int[] physAtk, float[] atkSpeed, int[] moveSpeed,
                                   string[] victimRungs, string rider, string blurb,
                                   bool payVictim = true)
    {
        EffectMagnitude[] Mags(int i) => new EffectMagnitude[]
        {
            new(SkillEffect.BuffPhysAtk, physAtk[i], ModifierMode.Flat),
            new(SkillEffect.BuffAccuracy, acc[i], ModifierMode.Flat),
            new(SkillEffect.BuffCritRate, critRate[i]),
            new(SkillEffect.BuffCritDamage, critDmg[i]),
            new(SkillEffect.BuffAtkSpeed, atkSpeed[i]),
            new(SkillEffect.BuffMoveSpeed, moveSpeed[i], ModifierMode.Flat),
            new(SkillEffect.BuffPveSkillDamage, skillDmg[i]),
            new(SkillEffect.BuffPvpSkillDamage, skillDmg[i]),
        };

        return new SkillDef(id, name, BaseClass.Fighter,
            SkillEffect.BuffPhysAtk | SkillEffect.BuffAccuracy | SkillEffect.BuffCritRate
            | SkillEffect.BuffCritDamage | SkillEffect.BuffAtkSpeed | SkillEffect.BuffMoveSpeed
            | SkillEffect.BuffPveSkillDamage | SkillEffect.BuffPvpSkillDamage,
            MpCost: BuffMp3[0], CastTicks: 30, CooldownTicks: 20, Range: 0, Power: 0,
            // ⚠ ONE KEY FOR ALL THREE, and `SharesLadderKey` is what says so on purpose — the startup
            // guard is right that two multi-rung ladders on one key make each other's rungs compete
            // (`BL-85`). It is harmless here and only here: a stance is one per RACE, and a race has
            // exactly one, so no character can ever hold two. Sharing the key is what keeps them one
            // family — one bar square, and a future group buff or potion competes with all three at
            // once rather than with whichever one this archer happens to own.
            DurationTicks: FiveMinutes, BuffKey: "archer_bow_stance_race", Rank: 1, SharesLadderKey: true,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
            RequiredWeapon: WeaponType.Bow, SpCost: BuffSp3[0],
            ProcChance: 0.05f, ProcCooldownTicks: 100,
            ProcVictimRungs: payVictim ? victimRungs : null,
            ProcSelfRungs: payVictim ? null : victimRungs,
            Magnitudes: Mags(0),
            Description: blurb,
            Levels: Enumerable.Range(0, 3).Select(i => new SkillLevel(
                MpCost: BuffMp3[i], SpCost: BuffSp3[i], Magnitudes: Mags(i),
                Description: $"For 5 minutes: +{physAtk[i]} P.Atk, +{acc[i]} accuracy, "
                           + $"+{critRate[i] * 100:0}% crit rate, +{critDmg[i] * 100:0}% crit damage, "
                           + $"+{atkSpeed[i] * 100:0}% attack speed, +{moveSpeed[i]} speed, "
                           + $"+{skillDmg[i] * 100:0}% physical skill damage, and {rider}.")).ToArray());
    }

    /// <summary>One of the three traps. Fifteen rungs, and only the tier moves — the reach (400), the
    /// wait (30s), the reuse (30s) and the MP ladder are the same in all three of his blocks.</summary>
    /// <param name="tiers">The DoT's rank per rung, or null for Binding Trap, whose root has no tier.</param>
    private static SkillDef Trap(string id, string name, SkillEffect rider, DebuffSchool school,
                                 int[]? tiers, int[] mp, string blurb, Func<int, string> rung)
    {
        var mags = rider switch
        {
            SkillEffect.Poison => new EffectMagnitude[]
            {
                new(SkillEffect.DebuffAtkSpeed, 0.15f), new(SkillEffect.DebuffCastSpeed, 0.15f),
            },
            SkillEffect.Bleed => new EffectMagnitude[] { new(SkillEffect.Slow, 0.15f) },
            _ => Array.Empty<EffectMagnitude>(),
        };
        var effect = rider | (rider == SkillEffect.Poison
            ? SkillEffect.DebuffAtkSpeed | SkillEffect.DebuffCastSpeed
            : rider == SkillEffect.Bleed ? SkillEffect.Slow : SkillEffect.None);

        return new SkillDef(id, name, BaseClass.Fighter, effect,
            MpCost: mp[0], CastTicks: 0, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: id, Rank: tiers?[0] ?? 1,
            DebuffSchool: school, Category: SkillCategory.Debuff, PhysicalCast: true,
            SpCost: RogueSp[0], RequiredWeapon: WeaponType.Bow,
            PlacesTrap: true, TrapRadius: 400f, TrapLifeTicks: 300,
            Magnitudes: mags,
            Description: blurb + " Waits 30s at your feet for something to walk into it.",
            Levels: BulwarkRungs(i => new SkillLevel(
                MpCost: mp[i], SpCost: RogueSp[i],
                Rank: tiers is null ? 0 : tiers[i], Magnitudes: mags,
                Description: rung(i)))
                .Concat(ArcherFourthTrapRungs(tiers is not null)).ToArray());
    }

    /// <summary>One race's Magic Arrow. Fifteen rungs on the shared arrow power column, 900 range, a
    /// one-second draw and a five-second reuse; only the rider differs.</summary>
    private static SkillDef MagicArrow(string id, SkillEffect rider, int durationTicks,
                                       EffectMagnitude[] mags, string blurb, string what)
        => new(id, "Magic Arrow", BaseClass.Fighter, SkillEffect.PhysicalDamage | rider,
            MpCost: ArcherMpLow[0], CastTicks: 10, CooldownTicks: 50, Range: 900, Power: ArcherArrowPower[0],
            DurationTicks: durationTicks, BuffKey: id, Rank: 1,
            DebuffSchool: DebuffSchool.Physical,
            Category: SkillCategory.Physical, SpCost: RogueSp[0],
            RequiredWeapon: WeaponType.Bow,
            Magnitudes: mags,
            Description: blurb,
            Levels: BulwarkRungs(i => new SkillLevel(
                Power: ArcherArrowPower[i], MpCost: ArcherMpLow[i], SpCost: RogueSp[i], Magnitudes: mags,
                Description: $"Strikes for power {ArcherArrowPower[i]:N0} {what}."))
                .Concat(ArcherFourthMagicArrowRungs(mags, what)).ToArray());
}
