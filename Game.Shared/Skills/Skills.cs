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
    PassiveEffect? Passive = null,
    string Abbrev = "")
{
    public float MagnitudeOf(SkillEffect effect, ModifierMode mode)
    {
        if (Magnitudes is null) return 0f;
        float sum = 0f;
        foreach (var m in Magnitudes)
            if (m.Effect == effect && m.Mode == mode) sum += m.Value;
        return sum;
    }

    /// <summary>MP charged when the cast STARTS. Default (-1) = 0 up front,
    /// full cost on completion (so existing skills are unchanged). Set
    /// InitialMpCost to split (e.g. 34 of 50 up front, 16 on finish).</summary>
    public int InitialMp => InitialMpCost < 0 ? 0 : Math.Min(InitialMpCost, MpCost);

    /// <summary>MP charged when the cast COMPLETES (the remainder).</summary>
    public int FinishMp => MpCost - InitialMp;
}

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
    int Defence = 0, int MagicDefence = 0,
    int Attack = 0, float AttackPct = 0f,
    int Evasion = 0, int Accuracy = 0,
    float CritRate = 0f, float CritDamage = 0f, float MagicCritRate = 0f,
    float HpRegen = 0f, float MpRegen = 0f,
    float AtkSpeedPct = 0f, float CastSpeedPct = 0f, float MoveSpeedPct = 0f,
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

    /// <summary>Effective range of a skill for a given caster. The skill's Range
    /// field is the tier-1 base; magic and bow skills step up by tier:
    ///   magic: 500 / 750 / 900   bow: 350 / 600 / 900
    /// Non-tiered skills (melee, range 0) just use their own value / basic range.</summary>
    public static float EffectiveRange(SkillDef def, Archetype? archetype, float basicAttackRange, int level)
    {
        if (def.Range <= 0)
            return basicAttackRange;

        int tier = RangeTier(level);

        bool isSpell = def.Effect.HasFlag(SkillEffect.MagicDamage)
            || def.Effect.HasFlag(SkillEffect.DebuffDef)
            || def.Effect.HasFlag(SkillEffect.Heal);

        // Bow skills: archer ranged physical attacks scale 350/600/900.
        bool isBowSkill = def.Effect.HasFlag(SkillEffect.PhysicalDamage)
            && archetype is Archetype.Archer && def.Range >= 300;

        if (isSpell)
            return tier switch { 3 => 900f, 2 => 750f, _ => 500f };
        if (isBowSkill)
            return tier switch { 3 => 900f, 2 => 600f, _ => 350f };

        return def.Range;
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
