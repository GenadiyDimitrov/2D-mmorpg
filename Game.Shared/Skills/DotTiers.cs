using System;
using System.Linq;

namespace Game.Shared;

/// <summary>Which damage-over-time family an effect belongs to. THE TYPE, not the skill.
///
/// <para>🔑 IT IS A FIELD AND NOT A <see cref="SkillEffect"/> BIT, and it had to be: 1L &lt;&lt; 62 is
/// the last free flag and is taken by BuffReflect (63 is the sign), so <c>AnyDot</c> could not grow a
/// fourth member for Burn. The three older families keep their flags — those still do the work of
/// saying "this is a DoT" (<c>AnyDot</c>), what a cure may strip (<c>DispelMask</c>) and which contest
/// it lands on — and this field says which ROW OF THE TABLE the damage and the rider come from.</para>
///
/// <para>⚠ So a Burn skill still carries <c>SkillEffect.Poison</c> for membership, and declares
/// <c>DotKind.Burn</c> for its numbers. Do not read the flag to decide damage; read this.</para></summary>
public enum DotKind
{
    None = 0,
    Bleed,
    Poison,
    Venom,
    Burn,
}

/// <summary>THE DAMAGE AND THE SIDE EFFECT A DoT DEALS, PER TYPE AND PER TIER — the mirror of
/// <c>docs/data/dot_table.csv</c>, and the only place either number lives.
///
/// <para>Owner's model, 2026-09-10: *"ill write u each type each tire what dmg it does .. and
/// depending on dmg-magic/phys and it does it as flat dmg ... just the landing rate depends on
/// stat"*, then his table, then: *"lets make them as authored ... remove the dot side effect from the
/// skills"*. So both halves of a DoT — the damage AND the rider — are properties of the TYPE and the
/// TIER, never of the skill that delivered it.</para>
///
/// The rules this enforces:
/// <list type="number">
///   <item>A tick is a FLAT number, per stack, from (kind, tier). Not the skill's Power, not the
///         caster's ATK, and never divided by the target's defence.</item>
///   <item>*"its true all effects do flat dmg. So magic postion or physical posion is no difference
///         just naming stuff and dos CON or SPT protects"* — the channel is ONLY which stat saves.</item>
///   <item>ONLY VENOM STACKS. *"bleed is slow, poison is just a magic posion and venom is the only
///         stacking dot atm"*.</item>
///   <item>BURN CANNOT BE SAVED AGAINST AND ALWAYS LANDS (*"for burn nothing protects .. always
///         land"*), and TIER 12 CANNOT BE CURED — cures reach 11.</item>
///   <item>NO DoT LOWERS DEFENCE. *"for now no dot will decrease def"* — venom's old <c>DebuffDef</c>
///         is gone.</item>
/// </list>
///
/// <para>🔴🔑 WHAT THIS REPLACED. Until 0.125.1 a DoT's per-second damage came from
/// <c>SkillDef.DotPowerAt</c>, which fell back to the skill's own <c>Power</c> when no <c>DotPower</c>
/// was authored — and it was authored on exactly ONE skill in the catalogue. So every other DoT ticked
/// for its DIRECT HIT's power: Bleeding Arrow (power 15,000 over 30s) dealt 450,000. A skill's Power is
/// its direct hit; a DoT rider is a second number. Do not reinstate the fallback.</para></summary>
public static class DotTiers
{
    /// <summary>Highest tier the table defines — Burn's 12. The three older families stop at 11, and
    /// asking for a tier a family does not define returns 0.</summary>
    public const int MaxTier = 12;

    /// <summary>The highest tier ANY cure can strip. *"i want healers holy blessing or whatever that
    /// cures/clences to clence to t11. so pyromancer ultimate is uncurable"* — so tier 12 exists
    /// precisely to be beyond every cleanse in the game.</summary>
    public const int MaxCurableTier = 11;

    // ── THE TABLE ────────────────────────────────────────────────────────────────────────────────
    // ✅ HIS NUMBERS, read off docs/data/dot_table.csv. Index = tier; index 0 is unused (tiers are
    //    1-based). Every value is FLAT DAMAGE PER SECOND, PER STACK — his own column header.
    // ⚠ THIS FILE AND THAT CSV ARE ONE THING IN TWO PLACES. A number changed in either is changed in
    //   both, in the same commit — the rule the skill CSVs already run on.
    //
    //                                   t0  t1  t2  t3  t4  t5  t6  t7   t8   t9  t10  t11  t12
    private static readonly int[] BleedDps  = { 0, 20, 20, 40, 40, 60, 60, 80,  80, 100, 100, 150,   0 };
    private static readonly int[] PoisonDps = { 0, 20, 30, 40, 50, 60, 70, 90, 110, 130, 150, 200,   0 };
    private static readonly int[] VenomDps  = { 0,  5,  5,  7,  7,  9,  9, 10,  10,  15,  15,  20,   0 };
    // ⚠ BURN STARTS AT 10 — *"burn dont have 1-9 .. because no1 is using them .. as the other dont
    //   have tire 12"*. The zeros below 10 are the table saying "no such thing", not "not yet tuned".
    private static readonly int[] BurnDps   = { 0,  0,  0,  0,  0,  0,  0,  0,   0,   0, 100, 125, 150 };

    /// <summary>Burn's rider: the fraction by which HP AND MP RECEIVED are cut, per tier.
    /// ⚠ Received, not regenerated — his table says *"Decrease hp/mp received by 70%"* and his own
    /// debuff-bar wording says *"Decreases Hp/Mp Received with 75%"*.</summary>
    private static readonly float[] BurnRecvCut = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.70f, 0.72f, 0.75f };

    /// <summary>Flat damage per second for ONE STACK at this tier. 0 = this family has no such tier.</summary>
    public static int DamagePerSecond(DotKind kind, int tier)
    {
        if (tier <= 0 || tier > MaxTier) return 0;
        return kind switch
        {
            DotKind.Bleed  => BleedDps[tier],
            DotKind.Poison => PoisonDps[tier],
            DotKind.Venom  => VenomDps[tier],
            DotKind.Burn   => BurnDps[tier],
            _              => 0,
        };
    }

    /// <summary>How many stacks this family may hold. *"venom is the only stacking dot atm"*.</summary>
    public static int MaxStacks(DotKind kind) => kind == DotKind.Venom ? 10 : 1;

    /// <summary>Which stat saves against it — the ONLY thing the physical/magical naming decides
    /// (*"just naming stuff and dos CON or SPT protects"*). <see cref="DebuffSchool.None"/> = nothing
    /// protects, which is Burn: *"for burn nothing protects .. always land"*.</summary>
    public static DebuffSchool Save(DotKind kind) => kind switch
    {
        DotKind.Bleed  => DebuffSchool.Physical,   // CON
        DotKind.Venom  => DebuffSchool.Physical,   // CON
        DotKind.Poison => DebuffSchool.Magical,    // SPT
        _              => DebuffSchool.None,       // Burn — always lands
    };

    /// <summary>Can a cleanse strip it? Everything up to <see cref="MaxCurableTier"/>; tier 12 never.</summary>
    public static bool Curable(int tier) => tier > 0 && tier <= MaxCurableTier;

    /// <summary>THE SIDE EFFECT, which belongs to the TYPE and no longer to the skill that delivered it
    /// (*"remove the dot side effect from the skills"*). Returns the flags the buff must carry for the
    /// stat system to see them, and the magnitudes themselves.
    ///
    /// <para>⚠ These are FLAT PER APPLICATION, not per stack. <c>BuffInstance.Percent</c> sums
    /// magnitudes and does NOT multiply by <c>Stacks</c>, so venom's -10% is -10% whether you are
    /// carrying one stack or ten. That is the cheaper of the two shapes he offered (*"or one -10% for
    /// any stack count .. whichever is easier"*); a 2%-per-stack version would need the stat layer to
    /// learn about stacks, which nothing else in the game needs.</para></summary>
    public static (SkillEffect Flags, EffectMagnitude[] Mags) Rider(DotKind kind, int tier) => kind switch
    {
        // -20% move speed at EVERY rank (*"yes lets make bleed generally to slow 20% at all ranks"*).
        DotKind.Bleed => (SkillEffect.Slow,
            new[] { new EffectMagnitude(SkillEffect.Slow, 0.20f) }),

        // -15% attack AND cast speed.
        DotKind.Poison => (SkillEffect.DebuffAtkSpeed | SkillEffect.DebuffCastSpeed,
            new[] { new EffectMagnitude(SkillEffect.DebuffAtkSpeed, 0.15f),
                    new EffectMagnitude(SkillEffect.DebuffCastSpeed, 0.15f) }),

        // -10% P.Atk AND M.Atk. ⚠ ONE flag does both: DebuffAtk wraps EffectiveAttack,
        // EffectiveMagicAttack and EffectiveBasicAttack alike (Entity.AtkDebuffed).
        // ⚠ NO DebuffDef any more — *"for now no dot will decrease def"*.
        DotKind.Venom => (SkillEffect.DebuffAtk,
            new[] { new EffectMagnitude(SkillEffect.DebuffAtk, 0.10f) }),

        // -70/72/75% HP received. The MP half is a FIELD, not a magnitude — see BurnMpCut.
        DotKind.Burn => (SkillEffect.DebuffHealRecv,
            new[] { new EffectMagnitude(SkillEffect.DebuffHealRecv, RecvCut(tier)) }),

        _ => (SkillEffect.None, Array.Empty<EffectMagnitude>()),
    };

    /// <summary>The MP half of Burn's rider, which rides a FIELD (<c>BuffInstance.MpReceivedPct</c>)
    /// rather than a magnitude because there is no flag for it. 0 for every other family.</summary>
    public static float BurnMpCut(DotKind kind, int tier) =>
        kind == DotKind.Burn ? RecvCut(tier) : 0f;

    private static float RecvCut(int tier) =>
        tier > 0 && tier < BurnRecvCut.Length ? BurnRecvCut[tier] : 0f;

    /// <summary>THE DEBUFF-BAR LINE, built from the type and the tier rather than authored on the
    /// skill. His format, verbatim: <c>Burn T12; -150hp/s; Decreases Hp/Mp Received with 75%;
    /// Uncurable;</c></summary>
    public static string Describe(DotKind kind, int tier, int stacks = 1)
    {
        int dps = DamagePerSecond(kind, tier);
        var sb = new System.Text.StringBuilder();
        sb.Append(kind).Append(" T").Append(tier).Append("; ");
        // Venom is the only family that stacks, so it is the only one that ever shows the arithmetic.
        if (stacks > 1)
            sb.Append('-').Append(dps * stacks).Append("hp/s (").Append(stacks)
              .Append(" x ").Append(dps).Append("); ");
        else
            sb.Append('-').Append(dps).Append("hp/s; ");

        switch (kind)
        {
            case DotKind.Bleed:  sb.Append("Slows Movement Speed by 20%; "); break;
            case DotKind.Poison: sb.Append("Decreases Attack and Cast Speed by 15%; "); break;
            case DotKind.Venom:  sb.Append("Decreases P.Atk and M.Atk by 10%; "); break;
            case DotKind.Burn:
                sb.Append("Decreases Hp/Mp Received with ")
                  .Append((int)Math.Round(RecvCut(tier) * 100f)).Append("%; ");
                break;
        }
        if (!Curable(tier)) sb.Append("Uncurable; ");
        return sb.ToString().TrimEnd();
    }

    /// <summary>Which family a skill's DoT belongs to: its explicit <c>DotKind</c> when it declares one
    /// (the only way to say Burn), else read off the legacy flag.</summary>
    public static DotKind KindOf(DotKind declared, SkillEffect effect)
    {
        if (declared != DotKind.None) return declared;
        if ((effect & SkillEffect.Bleed) != 0) return DotKind.Bleed;
        if ((effect & SkillEffect.Venom) != 0) return DotKind.Venom;
        if ((effect & SkillEffect.Poison) != 0) return DotKind.Poison;
        return DotKind.None;
    }

    /// <summary>True while nothing has been authored — the boot warning's test.</summary>
    public static bool Unauthored =>
        BleedDps.All(v => v == 0) && PoisonDps.All(v => v == 0)
        && VenomDps.All(v => v == 0) && BurnDps.All(v => v == 0);
}
