namespace Game.Shared;

/// <summary>
/// THE WARCHANTER'S 4th TIER, 76-90 — `docs/data/classes_skills_csv/buffer 4th.csv`, built
/// 2026-09-02 for `BL-108` on his word: *"buffer/healer 3rd/4th and shared 4th are done for now ..
/// build them"*. The SECOND 4th-class kit in the game, after the Lightbringer's.
///
/// <para><b>Two halves, the same shape as the healer's.</b> Sixteen of his families are skills the
/// 3rd class already teaches, simply continued past 74 — those are the <c>Wc4th*Rungs()</c> builders
/// below, concatenated onto each skill's own definition site. FOUR are new and are defined here in
/// full: Buffer Shield Mastery, Harmony of the Soul, Harmony of Madness and Harmony Mark.</para>
///
/// <para><b>THE PRICE LADDER IS THE SAME ONE</b> his healer file uses and it is stated in this file's
/// own header too — 6.5kk/11kk/16kk/80kk SP plus a token 1kk gold to 79, then NO SP at all and gold
/// climbing 5kk → 100kk. So the <c>F4</c> / <c>F4New</c> / <c>F4Rungs</c> helpers in
/// Skills.Lightbringer4th.cs are reused verbatim; they are the TIER's ladder, not the healer's.
/// ✅ <b>Spell Mastery</b> was the one ladder that did not, and he ruled on it the same day: its
/// fifteen rows had been pasted out of `buffer 3rd.csv` carrying that file's 36k … 880k SP and its
/// `[]` in the gold cell, which priced a level-90 rung of the buffer's core caster passive at a
/// 3rd-class 880k. They run on the tier's ladder now, like every other row in the file.</para>
///
/// <para><b>⚠ FOUR AUTHORING SLIPS were corrected, per the standing monotonic rule</b> (a value going
/// backwards is a typo — interpolate or report, never accept). All four are flagged back to him and
/// the CSV was edited to match, so the file and the game still agree:
/// <list type="bullet">
///   <item><b>Harmony of Restoration</b> alternated 110 / <b>100</b> / 120 / <b>100</b> / 130 … —
///         every ODD rung was an untouched copy of the 3rd tier's last row (100 HP/s, 10 MP/s), so
///         buying rung 89 would have made the hymn worse than rung 88. Interpolated to the straight
///         110 → 180 in fives that his even rungs describe.</item>
///   <item><b>Harmony of the Wizard</b> authored TWO rows at level 78. Its SP/gold cells (80kk + 1kk)
///         are the 79 band, so the second one is a 79 row with the level cell not typed.</item>
///   <item><b>Sound Burst</b> carried a second, identical level-90 row at the bottom of the Sound
///         Smash block — a stray paste. Removed; the ladder already ends at 90.</item>
///   <item><b>Doctor / Warlock Weapon Mastery</b> and the two toggle ladders left the WEAPON column
///         blank at this tier while the 3rd tier gates them (`blunt/1`, `blunt/2`). The gate is
///         CARRIED FORWARD — one ladder cannot change hands halfway up — and the column was filled
///         in to say so.</item>
/// </list></para>
///
/// <para><b>⚠ TWO cells were merely EMPTY and were filled from the row's own siblings</b>: Magic
/// Proficiency's reuse/duration pair in `shared 4th.csv` (0/0 against Arcane Protection's and
/// Physical Proficiency's 30/10, on a row that is nothing but a proc), and the AoE radius of Harmony
/// of the Soul / Madness / Mark (blank, where every other harmony says 800).</para>
///
/// <para><b>THE RACE SPLIT CONTINUES UNCHANGED</b> — Human blunt+shield, Elf bow, Demon blunt with
/// two damage skills. What the 4th tier adds is that all three finally have a WEAPON mastery: the
/// Human's <c>doctor_blunt_mastery</c> landed at the 3rd tier in the same pass (`buffer 3rd.csv`).</para>
/// </summary>
public static partial class SkillCatalog
{
    // ---- The four NEW ids. Append-only, as always. ----
    public const string BufferShieldMastery = "buffer_shield_mastery";
    public const string WcHarmonySoul       = "wc_harmony_soul";
    public const string WcHarmonyMadness    = "wc_harmony_madness";
    public const string WcHarmonyMark       = "wc_harmony_mark";

    /// <summary>The 4th tier's every-level band, 76-90. Same list the healer uses; named here so the
    /// buffer's table never has to reach into a file called "Lightbringer".</summary>
    internal static readonly int[] BufferFourthBands =
        { 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90 };

    /// <summary>…and the every-OTHER-level one: 76, 78 … 90. Eight rungs. His masteries, the two
    /// toggles and the two groups run on it.</summary>
    internal static readonly int[] BufferFourthEven =
        { 76, 78, 80, 82, 84, 86, 88, 90 };

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE CONTINUING LADDERS. Each returns ONLY the 4th-tier rungs; the definition site concatenates.
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>Armor Mastery rungs 15-29 (`buffer_armor_mastery`). Two things arrive at this tier that
    /// the 40-74 ladder never had: a PERCENT magic defence and an MP-cost reduction. Both ride
    /// <see cref="StatMods"/> fields that already exist — the healer's own 78+ rungs pay the MP-cost
    /// one — so nothing new was needed to express them.</summary>
    private static readonly int[] Wc4ArmorPDef =
        { 89, 91, 92, 93, 95, 96, 97, 99, 100, 101, 103, 104, 105, 107, 108 };
    private static readonly int[] Wc4ArmorMaxMp =
        { 220, 220, 250, 250, 250, 290, 290, 300, 300, 300, 330, 330, 350, 350, 400 };
    private static readonly float[] Wc4ArmorMDefPct =
        { .02f, .04f, .05f, .07f, .08f, .10f, .11f, .13f, .14f, .16f, .17f, .19f, .20f, .22f, .25f };
    private static readonly float[] Wc4ArmorMpCost =
        { 0f, 0f, .05f, .05f, .05f, .08f, .08f, .08f, .08f, .08f, .10f, .10f, .10f, .10f, .10f };

    internal static ArmorMasteryProfile[] BufferFourthArmorProfiles() =>
        Enumerable.Range(0, 15).Select(i =>
        {
            // The three weights stay IDENTICAL, exactly as they are at the 3rd tier: his rows are one
            // line — "Light/Heavy/Robe: …" — and the penalty-cancelling belongs to the RACE masteries.
            // Putting a speed clause here would apply it twice; see BufferArmorMasteryLevels.
            var m = new StatMods(PDef: Wc4ArmorPDef[i], MaxMp: Wc4ArmorMaxMp[i],
                                 MDefPct: Wc4ArmorMDefPct[i], MpCostPct: Wc4ArmorMpCost[i]);
            return new ArmorMasteryProfile(Robe: m, Light: m, Heavy: m);
        }).ToArray();

    internal static SkillLevel[] BufferFourthArmorRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(SpCost: sp, GoldCost: gold,
            Description: $"+{Wc4ArmorPDef[i]} P.Def, +{Wc4ArmorMaxMp[i]} Max MP, "
                       + $"+{Wc4ArmorMDefPct[i] * 100:0}% M.Def"
                       + (Wc4ArmorMpCost[i] > 0f ? $", and skills cost {Wc4ArmorMpCost[i] * 100:0}% less MP." : ".")));

    /// <summary>Spell Mastery rungs 19-33. ONLY the two attack numbers move: reuse (−20%), cast speed
    /// (+10%) and both regen multipliers are flat across the whole tier in his file, and each already
    /// stands at that value at rung 18.
    ///
    /// <para>✅ PRICED ON THE TIER'S OWN LADDER since he ruled on it (2026-09-02): *"spell mastery
    /// 76-90 to have its coresponding sp/gold cost"*. Its fifteen rows had been pasted out of
    /// `buffer 3rd.csv` and still carried that file's 36k … 880k SP and its `[]` in the gold cell,
    /// which made a level-90 rung of the buffer's core caster passive cost a 3rd-class price.</para></summary>
    private static readonly int[] Wc4SpellMAtk =
        { 101, 102, 104, 105, 106, 108, 109, 110, 112, 113, 115, 116, 117, 119, 120 };
    private static readonly int[] Wc4SpellPAtk =
        { 85, 90, 95, 100, 105, 110, 115, 120, 125, 130, 135, 140, 145, 150, 155 };

    internal static WeaponMasteryProfile[] BufferFourthSpellProfiles() =>
        Enumerable.Range(0, 15).Select(i => BufferMastery(new PassiveEffect(
            MagAtk: Wc4SpellMAtk[i], PhysAtk: Wc4SpellPAtk[i],
            CastSpeedPct: 0.10f, CooldownPct: 0.20f, MpRegen: 3.4f, HpRegen: 2.7f))).ToArray();

    internal static SkillLevel[] BufferFourthSpellRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(SpCost: sp, GoldCost: gold,
            Description: $"With a blunt weapon or a bow: +{Wc4SpellMAtk[i]} M.Atk, "
                       + $"+{Wc4SpellPAtk[i]} P.Atk, −20% skill reuse, +10% cast speed."));

    /// <summary>The three per-race WEAPON masteries, rungs 9-16. All eight rungs of all three share his
    /// P.Atk column; the elf's is a different number entirely because a bow mastery has always been.
    /// ⚠ Each carries the SAME hands gate as its 3rd-tier rungs — a ladder does not change hands
    /// halfway up, and his 76-90 rows simply left the WEAPON column blank.</summary>
    private static readonly int[] Wc4BluntAtk = { 110, 120, 130, 140, 150, 160, 180, 200 };
    private static readonly int[] Wc4BowAtk   = { 650, 700, 720, 800, 850, 900, 950, 1000 };
    private static readonly int[] Wc4WarlockAcc = { 4, 4, 4, 4, 4, 5, 5, 5 };
    private static readonly int[] Wc4DoctorAcc  = { 1, 1, 1, 1, 1, 2, 2, 2 };

    internal static WeaponMasteryProfile[] BufferFourthBowProfiles() =>
        Wc4BowAtk.Select(a => new WeaponMasteryProfile(
            Bow: new PassiveEffect(PhysAtk: a, BowRange: 400f))).ToArray();

    internal static WeaponMasteryProfile[] BufferFourthWarlockProfiles() =>
        Enumerable.Range(0, 8).Select(i => new WeaponMasteryProfile(
            Blunt: new PassiveEffect(PhysAtk: Wc4BluntAtk[i], Accuracy: Wc4WarlockAcc[i]),
            RequiredWeapon: WeaponType.AnyBlunt, RequiredHands: WeaponHands.Two)).ToArray();

    internal static WeaponMasteryProfile[] BufferFourthDoctorProfiles() =>
        Enumerable.Range(0, 8).Select(i => new WeaponMasteryProfile(
            Blunt: new PassiveEffect(PhysAtk: Wc4BluntAtk[i], Accuracy: Wc4DoctorAcc[i]),
            RequiredWeapon: WeaponType.AnyBlunt, RequiredHands: WeaponHands.One)).ToArray();

    /// <summary>Eight rungs of price, shared by all three weapon masteries and both toggles and both
    /// groups — every every-other-level ladder in his 4th file.</summary>
    internal static SkillLevel[] BufferFourthEvenRungs(Func<int, string> text) =>
        F4Rungs(8, 2, (i, sp, gold) => new SkillLevel(SpCost: sp, GoldCost: gold, Description: text(i)));

    /// <summary>Harmony of Restoration rungs 15-29.
    ///
    /// <para>🔴 THE HP/MP LADDER IS INTERPOLATED. His rows read 110 / 100 / 120 / 100 / 130 / 100 …:
    /// every odd rung is an untouched copy of the level-74 row he built the block from, so a level-89
    /// buffer would have paid 100kk gold to drop from 170 HP/s back to 100. The even rungs describe a
    /// clean 110 → 180, so the odd ones are filled in at the halfway step. His MP COLUMN is kept
    /// verbatim, dip and all (it falls 488 → 454 at level 80) — that one only makes the hymn cheaper,
    /// which breaks nothing.</para></summary>
    private static readonly int[] Wc4HotHp =
        { 110, 115, 120, 125, 130, 135, 140, 145, 150, 155, 160, 165, 170, 175, 180 };
    private static readonly int[] Wc4HotMp =
        { 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 16, 16, 17, 17, 18 };
    private static readonly int[] Wc4HotCost =
        { 470, 476, 482, 488, 454, 460, 466, 472, 478, 484, 490, 496, 502, 508, 514 };

    internal static SkillLevel[] BufferFourthRestorationRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(MpCost: Wc4HotCost[i], SpCost: sp, GoldCost: gold,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.HealOverTime, Wc4HotHp[i], ModifierMode.Flat),
                new(SkillEffect.RestoreMp,    Wc4HotMp[i], ModifierMode.Flat),
            },
            Description: $"Restores {Wc4HotHp[i]} HP and {Wc4HotMp[i]} MP per second to the party for 30s."));

    /// <summary>The two TOGGLES, rungs 14-21. Note the MP/s stops climbing: 30 and 15 flat across the
    /// whole tier where the 3rd tier's rose with every rung. His column, and it is the thing that makes
    /// the stance affordable to hold at 90.</summary>
    private static readonly int[] Wc4ReinforceDef = { 620, 640, 660, 680, 700, 725, 750, 800 };
    private static readonly int[] Wc4SharpenAtk   = { 310, 320, 330, 340, 350, 360, 380, 400 };

    internal static SkillLevel[] BufferFourthStanceRungs(SkillEffect effect, int[] amounts, int mpPerSec) =>
        F4Rungs(8, 2, (i, sp, gold) => new SkillLevel(
            MpCost: mpPerSec, MpPerSecond: mpPerSec, SpCost: sp, GoldCost: gold,
            Magnitudes: new EffectMagnitude[] { new(effect, amounts[i], ModifierMode.Flat) },
            Description: $"+{amounts[i]} while active, {mpPerSec} MP per second."));

    internal static SkillLevel[] BufferFourthReinforcementRungs() =>
        BufferFourthStanceRungs(SkillEffect.BuffDef, Wc4ReinforceDef, 30);
    internal static SkillLevel[] BufferFourthSharpeningRungs() =>
        BufferFourthStanceRungs(SkillEffect.BuffPhysAtk, Wc4SharpenAtk, 15);

    /// <summary>The three SOUND skills, rungs 14-28. One power column and one MP column for all three,
    /// exactly as at the 3rd tier — 4100 → 6500 and 123 → 195, continuing 4000/120 without a step.</summary>
    private static readonly int[] Wc4SoundPower =
        { 4100, 4200, 4300, 4400, 4500, 4600, 4700, 4800, 4900, 5000, 5300, 5600, 5900, 6200, 6500 };
    private static readonly int[] Wc4SoundMp =
        { 123, 126, 129, 132, 135, 138, 141, 144, 147, 150, 159, 168, 177, 186, 195 };

    internal static SkillLevel[] BufferFourthSoundRungs(int hits, int stunTicks) =>
        F4Rungs(15, 1, (i, sp, gold) => new SkillLevel(
            Power: Wc4SoundPower[i], MpCost: Wc4SoundMp[i], SpCost: sp, GoldCost: gold,
            Magnitudes: stunTicks > 0
                ? new EffectMagnitude[] { new(SkillEffect.Stun, 1f, ModifierMode.Flat) }
                : null,
            Description: stunTicks > 0
                ? $"Strikes for power {Wc4SoundPower[i]} and stuns for {stunTicks / 10f:0.#}s."
                : hits > 1
                    ? $"Strikes {hits} times for power {Wc4SoundPower[i]} each."
                    : $"Strikes for power {Wc4SoundPower[i]}."));

    /// <summary>Harmony of Protection's SIXTH rung, at 76 — everything rung 5 carries plus 10% bow
    /// resistance, which is the only thing his 76 row adds.</summary>
    internal static SkillLevel[] BufferFourthProtectionRungs()
    {
        var (sp, gold) = F4(0);
        return new[]
        {
            new SkillLevel(MpCost: 464, SpCost: sp, GoldCost: gold,
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.BuffMagicDef, 0.30f), new(SkillEffect.BuffHpRegen, 0.20f),
                    new(SkillEffect.BuffDef, 0.25f), new(SkillEffect.BuffHp, 0.30f),
                    new(SkillEffect.BuffReflect, 0.20f), new(SkillEffect.BuffBowResist, 0.10f),
                },
                Description: "+30% M.Def, +20% HP regeneration, +25% P.Def, +30% Max HP, reflects 20% "
                           + "of melee damage, 10% bow resistance (5 minutes)."),
        };
    }

    /// <summary>Harmony of the Wizard's rungs 3-5, at 77 / 78 / 79. Each is the one before plus one
    /// line: MP regen, then magic crit rate, then magic crit damage.
    /// ⚠ HIS THIRD ROW SAYS 78, like the second. Its price cells are the 79 band (80kk SP + 1kk gold)
    /// and a ladder cannot have two rungs at one level, so it is read as 79.</summary>
    internal static SkillLevel[] BufferFourthWizardRungs()
    {
        var (sp3, g3) = F4(1);
        var (sp4, g4) = F4(2);
        var (sp5, g5) = F4(3);
        var baseMags = new EffectMagnitude[]
        {
            new(SkillEffect.BuffMagAtk,    0.10f), new(SkillEffect.BuffCastSpeed, 0.30f),
            new(SkillEffect.BuffMpRegen,   0.20f),
        };
        return new[]
        {
            new SkillLevel(MpCost: 199, SpCost: sp3, GoldCost: g3, Magnitudes: baseMags,
                Description: "+10% M.Atk, +30% cast speed, +20% MP regeneration (5 minutes)."),
            new SkillLevel(MpCost: 279, SpCost: sp4, GoldCost: g4,
                Magnitudes: baseMags.Append(new EffectMagnitude(SkillEffect.BuffMagicCritRate, 0.30f)).ToArray(),
                Description: "+10% M.Atk, +30% cast speed, +20% MP regeneration, +30% magic critical "
                           + "rate (5 minutes)."),
            new SkillLevel(MpCost: 367, SpCost: sp5, GoldCost: g5,
                Magnitudes: baseMags.Append(new EffectMagnitude(SkillEffect.BuffMagicCritRate, 0.30f)).ToArray(),
                MagicCritDamage: 0.30f,
                Description: "+10% M.Atk, +30% cast speed, +20% MP regeneration, +30% magic critical "
                           + "rate and +30% magic critical damage (5 minutes)."),
        };
    }

    /// <summary>Soul Reinforcement rungs 2-9. The +35% Max MP and +30% M.Def never move — they are the
    /// group's two children and stay exactly what rung 1 hands out. What ladders is the MP-cost pair,
    /// 21/11% up to 30/20%, and it is authored on the RUNG rather than through a child: a group's own
    /// per-level field overrides what its children contribute (see ApplyBuff's GroupOr).</summary>
    internal static SkillLevel[] BufferFourthSoulRungs()
    {
        int[] mp    = { 460, 470, 480, 490, 500, 510, 520, 530 };
        float[] phys = { .21f, .22f, .23f, .24f, .25f, .26f, .28f, .30f };
        float[] mag  = { .11f, .12f, .13f, .14f, .15f, .16f, .18f, .20f };
        return F4Rungs(8, 2, (i, sp, gold) => new SkillLevel(
            MpCost: mp[i], SpCost: sp, GoldCost: gold,
            PhysMpCostPct: phys[i], MagicMpCostPct: mag[i],
            Description: $"+35% Max MP, +30% M.Def, and −{phys[i] * 100:0}% physical / "
                       + $"−{mag[i] * 100:0}% magic skill MP cost."));
    }

    /// <summary>Arcane and Feral Protection rungs 2-9. Both resistances are FIELDS, so they are stated
    /// on the rung for the same reason Soul Reinforcement's MP cost is. The SPT half is pinned at 50%
    /// for the whole tier and only the CON half climbs — his column, 43% to 65%.</summary>
    internal static SkillLevel[] BufferFourthArcaneFeralRungs()
    {
        int[] mp = { 350, 360, 370, 380, 390, 400, 410, 420 };
        float[] con = { .43f, .47f, .50f, .54f, .57f, .60f, .63f, .65f };
        return F4Rungs(8, 2, (i, sp, gold) => new SkillLevel(
            MpCost: mp[i], SpCost: sp, GoldCost: gold,
            CcResistMagical: 0.50f, CcResistPhysical: con[i],
            Description: $"{con[i] * 100:0}% resistance to Constitution-defended debuffs and 50% to "
                       + "Spirit-defended ones."));
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE FOUR NEW SKILLS
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    private static SkillDef[] Warchanter4thSkills()
    {
        var (sp76, gold76) = F4New(76);
        var (sp79, gold79) = F4New(79);
        var (sp83, gold83) = F4New(83);
        var (_, goldUp83)  = F4(83 - 76);

        // Harmony of the Soul's seven rungs, 77-83. Every one is CUMULATIVE — a harmony rung replaces
        // the one below it rather than adding to it, which is how every other harmony in the game reads.
        SkillLevel SoulRung(int i, int mp, float magReuse, float physReuse,
                            float magMp, float physMp, float healRecv, float sptResist)
        {
            var (sp, gold) = F4(i + 1);
            string text = $"−{magReuse * 100:0}% magical and −{physReuse * 100:0}% physical reuse, "
                        + $"−{magMp * 100:0}% magic and −{physMp * 100:0}% physical skill MP cost"
                        + (healRecv > 0f ? $", +{healRecv * 100:0}% healing received" : "")
                        + (sptResist > 0f ? $", +{sptResist * 100:0}% Spirit debuff resistance" : "")
                        + " (5 minutes).";
            return new SkillLevel(MpCost: mp, SpCost: sp, GoldCost: gold,
                PhysCooldownPct: physReuse, MagicCooldownPct: magReuse,
                PhysMpCostPct: physMp, MagicMpCostPct: magMp,
                HealReceivedPct: healRecv, CcResistMagical: sptResist,
                Description: text);
        }

        var soulRungs = new[]
        {
            SoulRung(0, 279, .10f, .20f, .10f, .10f, 0f,    0f),
            SoulRung(1, 279, .15f, .25f, .12f, .15f, 0f,    0f),
            SoulRung(2, 279, .20f, .30f, .15f, .20f, 0f,    0f),
            SoulRung(3, 367, .20f, .30f, .15f, .20f, .05f,  0f),
            SoulRung(4, 367, .20f, .30f, .15f, .20f, .10f,  0f),
            SoulRung(5, 464, .20f, .30f, .15f, .20f, .15f, .20f),
            SoulRung(6, 464, .20f, .30f, .15f, .20f, .20f, .30f),
        };

        // Harmony Mark's universal half — everything both rungs carry.
        var markMags = new EffectMagnitude[]
        {
            new(SkillEffect.BuffAtk,           0.10f, ModifierMode.Percent),
            new(SkillEffect.BuffAccuracy,      3f,    ModifierMode.Flat),
            new(SkillEffect.BuffAtkSpeed,      0.20f, ModifierMode.Percent),
            new(SkillEffect.BuffCastSpeed,     0.20f, ModifierMode.Percent),
            new(SkillEffect.BuffDef,           0.20f, ModifierMode.Percent),
            new(SkillEffect.BuffMagicDef,      0.20f, ModifierMode.Percent),
            new(SkillEffect.BuffCritRate,      0.20f, ModifierMode.Percent),
            new(SkillEffect.BuffMagicCritRate, 0.20f, ModifierMode.Percent),
            new(SkillEffect.BuffCritDamage,    0.20f, ModifierMode.Percent),
            new(SkillEffect.BuffHp,            0.20f, ModifierMode.Percent),
            new(SkillEffect.BuffMp,            0.20f, ModifierMode.Percent),
            new(SkillEffect.BuffHpRegen,       0.20f, ModifierMode.Percent),
            new(SkillEffect.BuffMpRegen,       0.20f, ModifierMode.Percent),
        };
        var markMags2 = markMags.Concat(new EffectMagnitude[]
        {
            new(SkillEffect.BuffCritRateResist, 0.10f, ModifierMode.Percent),
            new(SkillEffect.BuffCritDmgResist,  0.30f, ModifierMode.Percent),
        }).ToArray();

        return new SkillDef[]
        {
            // ═══ BUFFER SHIELD MASTERY @76 ═══════════════════════════════════════════════════════
            // 🔑 ROBE **AND** SHIELD, which is a combination nothing else in the game asks for and the
            //    reason `BL-107` had to make the shield its own axis: a shield is not an armour weight,
            //    it is a slot that coexists with every weight. His row is *"When Robe+Shield are
            //    equipped"* — so this is the pay-off for the Human buffer who took the shield line and
            //    kept casting in a robe, and it is worth NOTHING to the elf's bow or the demon's maul.
            // ⚠ It is a DIFFERENT skill from `tank_shield_mastery`, which the Human Warchanter also
            //   learns at 40/60/70: that one scales the shield, this one scales HIM.
            new(BufferShieldMastery, "Buffer Shield Mastery", BaseClass.Mage, SkillEffect.None,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                Category: SkillCategory.Passive, SpCost: sp76,
                Description: "Passive. In a robe and carrying a shield: +10% maximum HP and MP and "
                           + "+10% natural regeneration.",
                Levels: new[]
                {
                    new SkillLevel(SpCost: sp76, GoldCost: gold76,
                        Passive: new PassiveEffect(
                            RequiredArmor: ArmorWeights.Robe, RequiresShield: true,
                            MaxHpPct: 0.10f, MaxMpPct: 0.10f,
                            HpRegenPct: 0.10f, MpRegenPct: 0.10f),
                        Description: "In a robe and carrying a shield: +10% maximum HP and MP and "
                                   + "+10% natural regeneration."),
                }),

            // ═══ HARMONY OF THE SOUL, 77-83 ══════════════════════════════════════════════════════
            // The party's REUSE-and-MP harmony, and the reason the engine grew a per-channel reuse
            // reduction: his row asks for −10% magical and −20% physical on ONE buff, which the
            // single `BuffCooldown` number could not say. See SkillDef.PhysCooldownPct.
            // ⚠ Own key, covers nothing, MULTIPLIES on top of the basic layer — the harmony rule.
            new(WcHarmonySoul, "Harmony of the Soul", BaseClass.Mage, SkillEffect.None,
                MpCost: soulRungs[0].MpCost, CastTicks: 10, CooldownTicks: 1200, Range: 600, Power: 0,
                DurationTicks: 3000, BuffKey: "harmony_soul", Rank: NpcBuffRank,
                Category: SkillCategory.Buff, SpCost: soulRungs[0].SpCost,
                TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
                Levels: soulRungs,
                Description: "Quickens the party's hands and spares their mana: shorter reuse and "
                           + "cheaper skills, and at the top healing lands harder and the mind holds."),

            // ═══ HARMONY OF MADNESS @83 ══════════════════════════════════════════════════════════
            // 🔑 THIS IS THE HOME HIS OLD `Madness` NEVER HAD. That skill was renamed War Frenzy and
            //    moved to 56 in 2026-08-21, and the note left behind said the reckless party buff
            //    *"needs a home in his 4th-class file"*. Here it is, as a harmony: it costs the party
            //    10% of both pools and pays 8% of everything that kills things with.
            // ⚠ The two costs are NEGATIVE magnitudes — a buff may reduce, and Max HP/MP handle it the
            //   same way the older Frenzy already does.
            new(WcHarmonyMadness, "Harmony of Madness", BaseClass.Mage,
                SkillEffect.BuffHp | SkillEffect.BuffMp | SkillEffect.BuffAtk
                | SkillEffect.BuffAtkSpeed | SkillEffect.BuffCastSpeed
                | SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion,
                MpCost: 367, CastTicks: 10, CooldownTicks: 1200, Range: 600, Power: 0,
                DurationTicks: 3000, BuffKey: "harmony_madness", Rank: NpcBuffRank,
                Category: SkillCategory.Buff, SpCost: sp83,
                TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.BuffHp,        -0.10f, ModifierMode.Percent),
                    new(SkillEffect.BuffMp,        -0.10f, ModifierMode.Percent),
                    new(SkillEffect.BuffAtk,        0.08f, ModifierMode.Percent),
                    new(SkillEffect.BuffAtkSpeed,   0.08f, ModifierMode.Percent),
                    new(SkillEffect.BuffCastSpeed,  0.08f, ModifierMode.Percent),
                    new(SkillEffect.BuffMoveSpeed,  8f,    ModifierMode.Flat),
                    new(SkillEffect.BuffEvasion,   -8f,    ModifierMode.Flat),
                },
                Levels: new[]
                {
                    new SkillLevel(MpCost: 367, SpCost: sp83, GoldCost: gold83,
                        Description: "−10% maximum HP and MP, +8% attack, attack speed and cast speed, "
                                   + "+8 move speed, −8 evasion (5 minutes)."),
                },
                Description: "A hymn that trades the party's guard for its edge: less life and mana, "
                           + "more of everything that ends a fight."),

            // ═══ HARMONY MARK, 79 and 83 ═════════════════════════════════════════════════════════
            // 🔑 THE BUFFER'S MARK, and it carries the HEALER'S KEY on purpose — an ally wears ONE
            //    Mark, whichever class got to them. That was written into Skills.Lightbringer4th.cs
            //    the day the three healer Marks were built, naming this id in advance.
            // 🔑 FLAT RANK for the same reason the healer's three are: four skills share one key, so a
            //    rung must not ride in the rank or a Lv2 Harmony Mark would lock out a Lv1 Holy Mark.
            // ⚠ IT IS THE PARTY-WIDE ONE — the healer's three are single-target. That, the 2-minute
            //   reuse and the ten Skill Stones are what it pays for covering everybody at once.
            new(WcHarmonyMark, "Harmony Mark", BaseClass.Mage,
                markMags2.Aggregate(SkillEffect.None, (a, m) => a | m.Effect),
                MpCost: 300, CastTicks: 50, CooldownTicks: 1200, Range: 900, Power: 0,
                DurationTicks: 3000, BuffKey: MarkKey, Rank: 1, FlatRank: true,
                Category: SkillCategory.Buff, SpCost: sp79,
                TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
                ConsumableId: ItemCatalog.SkillStone, ConsumableAmount: 10,
                Magnitudes: markMags,
                Levels: new[]
                {
                    new SkillLevel(MpCost: 300, SpCost: sp79, GoldCost: gold79,
                        Magnitudes: markMags, LearnConsumableAmount: 0,
                        Description: "The whole party: +10% attack, +3 accuracy, and +20% to both "
                                   + "defences, attack and cast speed, critical rate and damage, "
                                   + "maximum HP and MP and regeneration, for five minutes. Consumes "
                                   + "10 Skill Stones. Only one Mark at a time."),
                    new SkillLevel(MpCost: 300, SpCost: 0, GoldCost: goldUp83,
                        Magnitudes: markMags2, MagicCritRateDebuff: 0.10f,
                        ConsumableAmount: 15,
                        Description: "The whole party: +10% attack, +3 accuracy, and +20% to both "
                                   + "defences, attack and cast speed, critical rate and damage, "
                                   + "maximum HP and MP and regeneration; blows and spells aimed at "
                                   + "them are 10% less likely to crit and physical criticals deal "
                                   + "30% less extra damage. Five minutes, 15 Skill Stones. Only one "
                                   + "Mark at a time."),
                },
                Description: "The party's own Mark. Does not stack with a healer's."),
        };
    }
}
