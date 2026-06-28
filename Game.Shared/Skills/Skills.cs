namespace Game.Shared;

// ===========================================================================
//  SKILLS — core types + the single catalog assembly.
//
//  SkillCatalog is a PARTIAL class spread across this folder so each class /
//  discipline owns its own definitions, while the dictionary is built in ONE place:
//    Skills.cs            — this file: the SkillDef record, the shared enums,
//                           PassiveEffect, the one BuildCatalog() that assembles
//                           the dictionary, and lookup (Get / AllSkills).
//    Skills.Common.cs     — cross-class: armor masteries, training/floor passives,
//                           buff potions, HP boost, wind walk.
//    Skills.Fighter.cs    — base Fighter kit.   Skills.Mage.cs — base Mage kit.
//    Skills.<Discipline>.cs — one file per 3rd-class discipline (Skills.Lightbringer.cs,
//                           Skills.Warchanter.cs, … future Skills.Bulwark.cs / .Warlord.cs).
//
//  TO ADD A DISCIPLINE: drop a Skills.<Name>.cs that declares `partial class
//  SkillCatalog` with its const ids + a `static SkillDef[] NameSkills()`, then add
//  one `list.AddRange(NameSkills());` line in BuildCatalog below. That's it.
//
//  WHO LEARNS WHAT (and at what level) is a SEPARATE concern — see
//  RaceAndClasses/ClassSkillTables*.cs. These files only DEFINE skills.
// ===========================================================================

/// <summary>
/// A skill definition. Id is now a STABLE STRING KEY (e.g. "magic_bolt",
/// "greater_heal"). Timings are in server ticks (10/s). Effect is a [Flags]
/// value; per-effect magnitudes live in Magnitudes with flat/percent modes.
/// Buff identity/stacking: BuffKey + Rank + Replaces (see ApplyBuff).
/// </summary>
public record SkillDef(
    string Id,
    string Name,
    BaseClass Class,
    SkillEffect Effect,
    int MpCost,
    int CastTicks,
    int CooldownTicks,
    float Range,
    int Power,
    int DurationTicks = 0,
    EffectMagnitude[]? Magnitudes = null,
    string BuffKey = "",
    int Rank = 0,
    string[]? Replaces = null,
    string Description = "",
    int SpCost = 1,
    SkillCategory Category = SkillCategory.Physical,
    TargetMode TargetMode = TargetMode.SelfOrTarget,
    float AreaRadius = 0f,
    int InterruptDefense = 0,
    int InterruptPower = 0,
    int InitialMpCost = -1,
    float BlockAccuracy = 0f,
    bool SureHit = false,
    // For contested crowd-control (Slow, later Stun/Root/Fear): which stat the LAND
    // chance contests against — Physical = ATK vs CON, Magical = ATK vs WIT.
    DebuffSchool DebuffSchool = DebuffSchool.None,
    // "[Double]" physical skills: can deal ×2 damage on a chance from the higher of
    // DEX/ATK (cap 30%). Ordinary physical skills never double. Magic skills use magic crit.
    bool CanDouble = false,
    // Per-skill context damage MULTIPLIERS (1.0 = unchanged). Let one skill hit differently
    // in PvE vs PvP — e.g. PvP 1.25 (a PvP-tuned warrior strike) or PvP 0 (a mob-only nuke).
    // Applied in the central damage pipeline once PvP exists; neutral today.
    float PveDamageMult = 1f,
    float PvpDamageMult = 1f,
    // Conditional damage: +ConditionalDamagePct when the TARGET is in any ConditionalOn
    // state (e.g. +50% vs slowed/rooted). None/0 = no conditional bonus.
    TargetCondition ConditionalOn = TargetCondition.None,
    float ConditionalDamagePct = 0f,
    // Stacking: how high this skill's stacking effect builds (1 = doesn't stack). Reapplying
    // (on a SUCCESSFUL land) adds a stack up to MaxStacks and refreshes. A bare counter (no
    // StackLevels) just counts — the rogue's burst fuel. See also StackLevels.
    int MaxStacks = 1,
    // Per-STACK effect table: each stack is an effect LEVEL, and the applied status takes that
    // level's Effect + Magnitudes (so a stacking slow can read 10/20/30% and the 4th stack can
    // be a different effect entirely, e.g. freeze). When set, MaxStacks = StackLevels.Length.
    // SkillDef.Effect should be the UNION of the levels' flags so the skill is recognised as
    // a (contested) debuff. null = not a leveled-stack effect.
    StackLevel[]? StackLevels = null,
    // DoT model (separated): a DoT applier writes a per-skill STACK COUNTER under StackKey
    // (independent of the shared damage effect, which overrides by Rank). Skills can SHARE a
    // StackKey to pool stacks (e.g. two races' Venomweavers). A burst names ConsumeStackKey:
    // it multiplies its damage by that counter's stacks, then removes the counter (the bleed
    // damage effect itself stays). "" = none.
    string StackKey = "",
    string ConsumeStackKey = "",
    // Cure (Cleanse) / Cancel targeting. DispelMask = which effect flags to remove (None =
    // all of the relevant polarity: Cleanse→any debuff, Cancel→any positive buff). DispelCount
    // = how many (0 = all matching; N = up to N at random). DispelMaxLevel = only effects with
    // Rank ≤ this (0 = any) — e.g. "cure bleeds ≤ 3". Cancellable = can THIS skill's effect be
    // cured/cancelled (false = immune; internal counters are always immune).
    SkillEffect DispelMask = SkillEffect.None,
    int DispelCount = 0,
    int DispelMaxLevel = 0,
    bool Cancellable = true,
    // A TOGGLE skill (stance/aura): clicking it applies its self-buff indefinitely;
    // clicking again removes it. Instant, no cast bar; MP charged on activation only.
    bool Toggle = false,
    // Optional REAGENT: an item this skill consumes to cast (e.g. an ultimate that needs
    // a rare catalyst). "" = no requirement (casts freely). The amount is consumed when the
    // cast COMPLETES; availability is checked up front so the cast isn't started in vain.
    string ConsumableId = "",
    int ConsumableAmount = 1,
    PassiveEffect? Passive = null,
    string Abbrev = "",
    // Optional per-LEVEL data. A skill with no Levels is single-level (level 1) and
    // uses the inline fields above. A multi-level skill puts its per-level Power /
    // Magnitudes / Passive / MpCost / SpCost in Levels[level-1]; see *At(level).
    SkillLevel[]? Levels = null,
    // Fraction of magic damage dealt that heals the caster (Vampiric Bolt etc.).
    float Lifesteal = 0f,
    // Armor-mastery passive: per-level, per-worn-weight stat profiles (see
    // ArmorMasteryProfile). When set, this skill behaves as a data-driven armor mastery.
    ArmorMasteryProfile[]? ArmorMasteryLevels = null,
    // Weapon-mastery passive: per-level, per-equipped-WEAPON-TYPE PassiveEffects (see
    // WeaponMasteryProfile). The bonus applies only while the matching weapon is held —
    // the same data-driven pattern as armor mastery, keyed on weapon type instead of weight.
    WeaponMasteryProfile[]? WeaponMasteryLevels = null)
{
    /// <summary>The armor-mastery per-weight profile for a learned skill LEVEL, or null
    /// if this skill isn't an armor mastery.</summary>
    public ArmorMasteryProfile? ArmorMasteryAt(int level) =>
        ArmorMasteryLevels is { Length: > 0 } && level >= 1 && level <= ArmorMasteryLevels.Length
            ? ArmorMasteryLevels[level - 1] : null;

    /// <summary>The weapon-mastery per-weapon profile for a learned skill LEVEL, or null
    /// if this skill isn't a weapon mastery.</summary>
    public WeaponMasteryProfile? WeaponMasteryAt(int level) =>
        WeaponMasteryLevels is { Length: > 0 } && level >= 1 && level <= WeaponMasteryLevels.Length
            ? WeaponMasteryLevels[level - 1] : null;

    /// <summary>Highest stack count: the StackLevels length if set, else MaxStacks.</summary>
    public int EffectiveMaxStacks => StackLevels is { Length: > 0 } ? StackLevels.Length : MaxStacks;

    /// <summary>The (Effect, Magnitudes) a stacking effect uses at the given stack count
    /// (1-based, clamped). Null if this skill has no per-stack table.</summary>
    public StackLevel? StackLevelAt(int stacks) =>
        StackLevels is { Length: > 0 } sl ? sl[Math.Clamp(stacks, 1, sl.Length) - 1] : null;

    /// <summary>Highest level this skill can reach (1 for a single-level skill).</summary>
    public int MaxLevel => Levels is { Length: > 0 } ? Levels.Length : 1;

    private SkillLevel? Lvl(int level) =>
        Levels is { Length: > 0 } && level >= 1 && level <= Levels.Length ? Levels[level - 1] : null;

    public int PowerAt(int level) => Lvl(level)?.Power ?? Power;
    public EffectMagnitude[]? MagnitudesAt(int level) => Lvl(level)?.Magnitudes ?? Magnitudes;
    public int MpCostAt(int level) => Lvl(level)?.MpCost ?? MpCost;
    public int SpCostAt(int level) => Lvl(level)?.SpCost ?? SpCost;
    public PassiveEffect? PassiveAt(int level) => Lvl(level)?.Passive ?? Passive;
    public string DescriptionAt(int level) => Lvl(level)?.Description ?? Description;

    public float MagnitudeOf(SkillEffect effect, ModifierMode mode, int level = 1)
    {
        var mags = Lvl(level)?.Magnitudes ?? Magnitudes;
        if (mags is null) return 0f;
        float sum = 0f;
        foreach (var m in mags)
            if (m.Effect == effect && m.Mode == mode) sum += m.Value;
        return sum;
    }

    /// <summary>MP charged when the cast STARTS (level-aware). Default (-1) = 0 up
    /// front, full cost on completion. A level's own InitialMpCost overrides the
    /// SkillDef's; both -1 = nothing up front.</summary>
    public int InitialMpAt(int level)
    {
        int init = Lvl(level) is { } sl ? sl.InitialMpCost : InitialMpCost;
        if (init < 0) init = InitialMpCost;   // level didn't specify → fall back to the def
        return init < 0 ? 0 : Math.Min(init, MpCostAt(level));
    }

    /// <summary>MP charged when the cast COMPLETES (the remainder), level-aware.</summary>
    public int FinishMpAt(int level) => MpCostAt(level) - InitialMpAt(level);

    // Level-1 convenience (single-level skills / display fallback).
    public int InitialMp => InitialMpAt(1);
    public int FinishMp => FinishMpAt(1);
}

/// <summary>Per-armor-weight stat profile for an armor-mastery PASSIVE (one entry per
/// skill level). The worn BODY weight selects which MasteryEffect applies in
/// Entity.RecomputeDerived — turning the old hardcoded ArmorMastery table into skill DATA
/// (the same pattern future classes reuse for weapon-type-conditional passives).</summary>
public readonly record struct ArmorMasteryProfile(
    MasteryEffect Robe, MasteryEffect Light, MasteryEffect Heavy);

/// <summary>Per-equipped-WEAPON-TYPE stat profile for a weapon-mastery PASSIVE (one entry
/// per skill level). The held weapon's type selects which <see cref="PassiveEffect"/>
/// applies in Entity.RecomputeDerived — rewarding the "right" weapon for the class. Unlike
/// armor mastery there is NO penalty for the wrong weapon (you just get no bonus); each
/// unset weapon defaults to an all-zero (inert) PassiveEffect. Keyed on WeaponType only
/// (1H vs 2H is not distinguished here yet).</summary>
/// <summary>One stack LEVEL of a stacking effect: the Effect flags + Magnitudes the status
/// takes at that stack count. Lets a stacking debuff change qualitatively per stack (e.g.
/// slow 10/20/30% on stacks 1-3, then a freeze/stun on stack 4).</summary>
public readonly record struct StackLevel(SkillEffect Effect, EffectMagnitude[] Magnitudes);

public readonly record struct WeaponMasteryProfile(
    PassiveEffect Sword = default, PassiveEffect Dual = default,
    PassiveEffect Bow = default, PassiveEffect Blunt = default,
    // When set, the bonus applies ONLY if the equipped weapon matches these hands
    // (e.g. Warrior trains TwoHand, Tank OneHand). null = either. Dual/Bow are always 2H.
    WeaponHands? RequiredHands = null)
{
    /// <summary>The bonus for the equipped weapon (type + hands). Inert default for None,
    /// an unset slot, or a hands mismatch.</summary>
    public PassiveEffect For(WeaponType wt, WeaponHands hands)
    {
        if (RequiredHands is WeaponHands req && req != hands) return default;
        return wt switch
        {
            WeaponType.Sword => Sword,
            WeaponType.Dual  => Dual,
            WeaponType.Bow   => Bow,
            WeaponType.Blunt => Blunt,
            _ => default
        };
    }
}

/// <summary>Per-level tunables for a multi-level skill (see SkillDef.Levels). Only the
/// fields that change between levels live here; identity (id/name/effect/range/flags)
/// stays on the SkillDef. Level 1 = Levels[0].</summary>
public record SkillLevel(
    int Power = 0,
    EffectMagnitude[]? Magnitudes = null,
    PassiveEffect? Passive = null,
    int MpCost = 0,
    int SpCost = 1,
    string? Description = null,
    int InitialMpCost = -1);   // -1 = inherit the SkillDef's split (0 up front)

/// <summary>Skill window grouping. Passive = a learned, always-on effect (armor
/// masteries, discipline passives) — never cast and never placed on the action bar.</summary>
public enum SkillCategory { Physical = 0, Magic = 1, Buff = 2, Debuff = 3, Heal = 4, Passive = 5 }

/// <summary>
/// An always-on bonus carried by a learnable PASSIVE skill (discipline passives).
/// Applied in Entity.RecomputeDerived for every learned skill whose SkillDef sets
/// one. ALL fields default to 0 = no effect (so `default`/`new()` is safely inert —
/// unlike MasteryEffect's speed factors). Pct fields are fractions (0.10 = +10%);
/// speed Pct fields make you FASTER (0.10 = 10% faster). Flat ints add directly.
/// </summary>
public readonly record struct PassiveEffect(
    float MaxHpPct = 0f, float MaxMpPct = 0f,
    int MaxHp = 0, int MaxMp = 0,      // flat max HP / MP
    int Defence = 0, int MagicDefence = 0,
    int Attack = 0, float AttackPct = 0f,
    int PhysAtk = 0, int MagAtk = 0,   // flat, channel-specific (Weapon Mastery etc.)
    float PhysAtkPct = 0f, float MagAtkPct = 0f,  // percent, channel-specific
    int Evasion = 0, int Accuracy = 0,
    float CritRate = 0f, float CritDamage = 0f, float MagicCritRate = 0f,
    float HpRegen = 0f, float MpRegen = 0f,            // FLAT regen per tick
    float HpRegenPct = 0f, float MpRegenPct = 0f,      // regen MULTIPLIER (additive: 0.20 = +20%)
    float AtkSpeedPct = 0f, float CastSpeedPct = 0f, float MoveSpeedPct = 0f,
    float CooldownPct = 0f,       // spell reuse-delay reduction (0.10 = -10%)
    // Defensive resists (fractions). MeleeVamp/SpellVamp = lifesteal fractions.
    float CritRateResist = 0f, float CritDmgResist = 0f, float BowResist = 0f,
    float MagicFailResist = 0f,
    int InterruptPower = 0, int InterruptResist = 0,
    float MeleeVamp = 0f, float SpellVamp = 0f,
    // Damage-OUT bonuses (fractions): the 2×3 matrix of context (PvE/PvP) × source
    // (skill=physical skill / magic / basic). Set as many as the effect should touch.
    float PveSkillDamagePct = 0f, float PveMagicDamagePct = 0f, float PveBasicDamagePct = 0f,
    float PvpSkillDamagePct = 0f, float PvpMagicDamagePct = 0f, float PvpBasicDamagePct = 0f,
    // Combat-resolution "sure" floors (see docs/CombatResolution.md). These are
    // GUARANTEES (the resolver takes the MAX across passives, not a sum):
    float EvadeFloor = 0f,        // min chance to dodge physical (rogue/archer)
    float HitFloor = 0f,          // min chance THIS entity lands a physical hit (warrior)
    float MagicFailFloor = 0f);   // min chance a spell fizzles vs this entity (tank/mage anti-magic)

/// <summary>Who a (beneficial) skill affects. SelfOnly = caster only;
/// AlliesInRadius = caster + nearby player characters (a "party" buff until real
/// party groups exist).</summary>
public enum TargetMode { SelfOrTarget = 0, SelfOnly = 1, AlliesInRadius = 2 }

// ===========================================================================
//  SKILL CATALOG — partial across this folder. This file owns the assembly.
//
//  WHERE TO EDIT:
//   - A skill's *definition* (numbers, text) goes in its class/discipline file
//     (Skills.Fighter.cs, Skills.Lightbringer.cs, …) as a SkillDef in that file's
//     XxxSkills() method, with its const id alongside.
//   - *Who learns it and at what level* lives in the per-class files under
//     RaceAndClasses/ (e.g. Classes.Human.Mage.cs), via ClassSkills.Register.
// ===========================================================================
public static partial class SkillCatalog
{
    private static readonly Dictionary<string, SkillDef> All = BuildCatalog();

    /// <summary>THE single place the catalog is assembled — one AddRange per
    /// definition file. Duplicate ids throw at startup (collision guard).</summary>
    private static Dictionary<string, SkillDef> BuildCatalog()
    {
        var list = new List<SkillDef>();
        list.AddRange(CommonSkills());        // Skills.Common.cs
        list.AddRange(FighterSkills());       // Skills.Fighter.cs
        list.AddRange(MageSkills());          // Skills.Mage.cs
        list.AddRange(HealerSkills());        // Skills.Healer.cs (2nd-class Healer kit)
        list.AddRange(ArmorMasterySkills());  // Skills.Masteries.cs (data-driven per-archetype)
        list.AddRange(WeaponMasterySkills()); // Skills.WeaponMasteries.cs (weapon-type-conditional)
        list.AddRange(BufferSkills());        // Skills.Buffer.cs (NPC newbie-buffer buffs)
        list.AddRange(LightbringerSkills());  // Skills.Lightbringer.cs
        list.AddRange(WarchanterSkills());    // Skills.Warchanter.cs

        var dict = new Dictionary<string, SkillDef>();
        foreach (var sk in list)
            if (!dict.TryAdd(sk.Id, sk))
                throw new InvalidOperationException($"Duplicate skill id '{sk.Id}'.");
        return dict;
    }

    public static SkillDef? Get(string id) => id is null ? null : All.GetValueOrDefault(id);
    public static string DescriptionOf(string id) => Get(id)?.Description ?? "";
    public static IEnumerable<SkillDef> AllSkills => All.Values;
}

/// <summary>Combat math + range/cast helpers.</summary>
public static class SkillMath
{
    /// <summary>Class-change tier from level: 1 (1-20), 2 (21-40), 3 (40+).
    /// Skill ranges scale with tier so reach grows as you advance.</summary>
    public static int RangeTier(int level) =>
        level >= 41 ? 3 : level >= 21 ? 2 : 1;

    /// <summary>Effective range of a skill for a given caster. SPELLS (and everything
    /// else) use their OWN <see cref="SkillDef.Range"/> — range is a property of the spell,
    /// not the class tier (heals are shorter than attack spells; a healer's bolt reaches
    /// 750, a nuker's 900, by how each spell is authored). The ONE exception kept is BOW
    /// skills, whose reach still grows with the archer's bow tier (350/600/900), matching
    /// the bow basic-attack range scaling.</summary>
    public static float EffectiveRange(SkillDef def, Archetype? archetype, float basicAttackRange, int level)
    {
        if (def.Range <= 0)
            return basicAttackRange;

        // Bow skills: archer ranged physical attacks still scale by bow tier.
        bool isBowSkill = def.Effect.HasFlag(SkillEffect.PhysicalDamage)
            && archetype is Archetype.Archer && def.Range >= 300;
        if (isBowSkill)
            return RangeTier(level) switch { 3 => 900f, 2 => 600f, _ => 350f };

        return def.Range;   // the spell's authored range
    }

    // Backwards-compatible overload (assumes tier 1) for any caller without level.
    public static float EffectiveRange(SkillDef def, Archetype? archetype, float basicAttackRange) =>
        EffectiveRange(def, archetype, basicAttackRange, 1);

    public const int CastBaselineWit = 25;
    public const float CastPercentPerWit = 0.012f;

    public static float CastModifier(int wit) =>
        Math.Clamp((CastBaselineWit - wit) * CastPercentPerWit, -0.50f, 0.50f);

    public static int AdjustedCastTicks(int baseTicks, int wit) =>
        Math.Max(2, (int)MathF.Round(baseTicks * (1f + CastModifier(wit))));

    public static float SpellFailChance(int casterLevel, int targetLevel) =>
        Math.Clamp(0.03f + (targetLevel - casterLevel) * 0.02f, 0.01f, 0.80f);

    public static int PhysicalSkillDamage(int power, float effAttack, float effDefence) =>
        Math.Max(1, power + (int)(effAttack * 2 - effDefence));

    public static int MagicSkillDamage(int power, float effAttack, int wit, float effDefence) =>
        Math.Max(1, power + (int)(effAttack + wit * 2 - effDefence / 2));

    public static int HealAmount(int power, int wit) => power + wit * 2;

    public const float CritMultiplierSkills = 2.0f;
    public const int PhysicalSkillAccuracyBonus = 10;
}
