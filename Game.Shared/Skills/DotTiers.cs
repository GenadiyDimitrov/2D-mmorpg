using System.Linq;

namespace Game.Shared;

/// <summary>THE DAMAGE A DAMAGE-OVER-TIME EFFECT DEALS, PER TYPE AND PER TIER.
///
/// <para>Owner's model, 2026-09-10, in answer to the playtest that found Bleeding Arrow ticking for
/// 15,000 a second: *"ill write u each type each tire what dmg it does .. and depending on
/// dmg-magic/phys and it does it as flat dmg ... just the landing rate depends on stat"*.</para>
///
/// So the three rules this table exists to enforce:
/// <list type="number">
///   <item>A tick is a **flat number** read from (type, tier). It is not derived from the skill's
///         Power, not from the caster's ATK, and not divided by the target's defence.</item>
///   <item>The **channel** (physical / magical) is the skill's own <c>DebuffSchool</c>, which already
///         picks CON or SPT for the landing contest. Nothing here re-states it.</item>
///   <item>**Only the landing rate depends on a stat** — <c>StatCalculator.DebuffLandChance</c>,
///         untouched by any of this.</item>
/// </list>
///
/// <para>🔴🔑 WHAT THIS REPLACED, AND WHY NOTHING MAY FALL BACK TO IT AGAIN. Until 0.125.1 a DoT's
/// per-second damage came from <c>SkillDef.DotPowerAt</c>, which fell back to the skill's own
/// <c>Power</c> when no <c>DotPower</c> was authored — and <c>DotPower</c> was authored on exactly ONE
/// skill in the entire catalogue (Pyro Burst). Every other DoT in the game therefore ticked for its
/// DIRECT HIT's power, flat, undivided, once a second, for the whole duration. Bleeding Arrow
/// (power 15,000 over 30s) dealt 450,000; Venom Stab dealt 7,500 a second. A skill that carries both
/// a damage flag and a DoT flag has two different numbers, and re-using one for the other is the bug.
/// </para>
///
/// <para>⚠ THE NUMBERS ARE HIS AND ARE NOT YET WRITTEN. Every entry is 0 until he sends the table, and
/// 0 means the DoT deals NOTHING — deliberately, and following his own precedent on the skill
/// masteries (*"Nobody — dead until you author it"*): an invented placeholder gets mistaken for a
/// tuned number and ships. An inert bleed is loud; a plausible-looking one is not. Fill the arrays in
/// and every DoT in the game starts working, with no other change anywhere.</para>
/// </summary>
public static class DotTiers
{
    /// <summary>Highest tier any authored skill asks for today: Bleeding Arrow's <c>rank: 11</c>.
    /// Raising this is free — the arrays are sized from it.</summary>
    public const int MaxTier = 11;

    // ── THE TABLE ────────────────────────────────────────────────────────────────────────────────
    // Index = tier. Index 0 is unused (a tier is 1-based); every value is FLAT DAMAGE PER SECOND.
    // 🔵 AWAITING HIS NUMBERS — see the class note. Do not invent them.
    //
    //                                   t0  t1  t2  t3  t4  t5  t6  t7  t8  t9 t10 t11
    private static readonly int[] BleedDps  = { 0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0 };
    private static readonly int[] PoisonDps = { 0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0 };
    private static readonly int[] VenomDps  = { 0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0 };

    /// <summary>Flat damage per second for one stack of this DoT at this tier. 0 = unauthored, which
    /// the tick loop treats as "this DoT deals nothing" rather than as a floor.
    ///
    /// <para><paramref name="effect"/> may carry other flags (Bleeding Arrow is
    /// <c>PhysicalDamage | Bleed | Slow</c>) — only the DoT bit is read, and the three are mutually
    /// exclusive in every skill authored so far. If one ever carries two, the first match wins and
    /// that is a skill-authoring mistake, not a case to support here.</para></summary>
    public static int DamagePerSecond(SkillEffect effect, int tier)
    {
        if (tier <= 0 || tier > MaxTier) return 0;
        if ((effect & SkillEffect.Bleed) != 0) return BleedDps[tier];
        if ((effect & SkillEffect.Poison) != 0) return PoisonDps[tier];
        if ((effect & SkillEffect.Venom) != 0) return VenomDps[tier];
        return 0;
    }

    /// <summary>True while nothing has been authored — used by the boot log so an inert DoT layer
    /// announces itself instead of being discovered in a playtest.</summary>
    public static bool Unauthored =>
        BleedDps.All(v => v == 0) && PoisonDps.All(v => v == 0) && VenomDps.All(v => v == 0);
}
