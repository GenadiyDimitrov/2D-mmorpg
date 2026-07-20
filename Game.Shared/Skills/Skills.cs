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
    // {Flat, Mod} damage model (docs/DamageModel.md): physical = 77·(Flat + Mod·pAtk)/def,
    // magic = 91·(Flat + Mod·√mAtk)/def. Mod == 0 → LEGACY: fall back to Power (physical adds Power
    // to pAtk; magic multiplies √mAtk by Power) so every existing skill is unchanged.
    int Flat = 0,
    float Mod = 0f,
    int DurationTicks = 0,
    EffectMagnitude[]? Magnitudes = null,
    string BuffKey = "",
    int Rank = 0,
    string[]? Replaces = null,
    string Description = "",
    int SpCost = 1,
    SkillCategory Category = SkillCategory.Physical,
    /// <summary>Which buff-bar ROW this skill's effect lands in. Defaults to the ordinary buff row;
    /// a debuff is detected from its effect flags regardless. Set Consumable on potion skills and
    /// Item on always-on gear effects.</summary>
    BuffRow BuffRow = BuffRow.Buff,
    /// <summary>Mutually-exclusive group ("" = none). Learning ANY skill in a group permanently
    /// locks out every other skill in it — you commit to one trade-off. Used by the level-40
    /// stat-swap passives: take +CON−DEX and you can never also take +CON−ATK.</summary>
    string ExclusiveGroup = "",
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
    // Weapon requirement: an ACTIVE skill only casts while the equipped weapon's type is in
    // this [Flags] MASK (e.g. Strike = Sword|Blunt, Stab = Dual, Shot = Bow). Checked at
    // cast-start with one bitwise-AND. None = usable with any weapon. Passives ignore this.
    WeaponType RequiredWeapon = WeaponType.None,
    // BLOW skill (dagger Stab): lands for FULL Power only if it CRITS (or, with CanDouble,
    // doubles). A blow that fails to crit deals only BlowFailFraction of its damage — a soft
    // floor, not L2's 0-damage whiff. Only meaningful with a physical-damage effect.
    bool BlowOnCrit = false,
    float BlowFailFraction = 0.10f,
    // HP-gated activation (warrior Battle Presence/Defence): the skill can only be USED while
    // the caster's HP is at or below this fraction of max (0.6 = ≤60%). Once active the buff
    // persists its full duration even if HP recovers (checked only at cast-start). 0 = no gate.
    float RequireHpBelowFraction = 0f,
    // HP paid on cast (Restore Spirit trades HP for MP). Deducted when the cast completes;
    // never reduces the caster below 1 HP. 0 = free.
    int HpCost = 0,
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
    // Movement effects. BlinkRange: 0 = teleport the caster just BEHIND the target (gap-closer);
    // > 0 = teleport the caster that far AWAY from the target (escape). KnockbackRange: shove
    // the target that far away from the caster.
    float BlinkRange = 0f,
    float KnockbackRange = 0f,
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
    // Optional EMOJI/glyph for the skill square + buff bar. Deliberately a STRING of characters, not an
    // image path: the WPF client is a test harness, so a bitmap pipeline here would be thrown away — and
    // when the real (Unity) client wants art, this same string becomes the SPRITE KEY. Empty = fall back
    // to Abbrev (the letters). A per-CLASS override lives on ClassSkill.Icon and wins over this.
    // RULE (owner): no two skills of the SAME class may share an icon; reuse across classes is fine.
    string Icon = "",
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
    WeaponMasteryProfile[]? WeaponMasteryLevels = null,
    // Skill MP-cost reduction granted by this (buff) skill: PHYSICAL-category skills cost
    // less MP by PhysMpCostPct, magic/buff/heal skills by MagicMpCostPct (fractions, 0 = none).
    // Carried on the BuffInstance and applied when a skill charges its MP. (The SkillEffect enum
    // is full, so this rides as explicit fields, not a flag.)
    float PhysMpCostPct = 0f,
    float MagicMpCostPct = 0f,
    // STEALTH (rogue "Hide"): a self-cast that makes the caster invisible to mob AI for
    // DurationTicks. Broken early by taking any OFFENSIVE action (attack / offensive skill).
    // Movement is allowed. (SkillEffect enum is full, so this rides as a flag field.)
    bool GrantsStealth = false,
    // TRAP (Trapper): instead of hitting now, the cast DROPS a trap at the caster's feet that
    // arms and, when a hostile steps within TrapRadius, delivers THIS skill's damage + any
    // contested CC (Root/Stun/etc.) to that intruder, then vanishes. TrapLifeTicks = how long
    // it waits before expiring unused. Uses the skill's own Effect/Power/Magnitudes.
    bool PlacesTrap = false,
    float TrapRadius = 150f,
    int TrapLifeTicks = 300,
    // Fixed-timing flags (Return skill + future ultimate/event skills). FixedCast = cast time
    // ignores cast-speed (always the authored CastTicks). FixedCooldown = reuse ignores
    // cooldown-reduction buffs. FragileCast = ANY damage taken cancels the cast (bypasses the
    // interrupt contest). TeleportsToTown = on completion, teleport the caster to the nearest safe
    // town (the SkillEffect enum is full, so these ride as flag fields).
    bool FixedCast = false,
    bool FixedCooldown = false,
    bool FragileCast = false,
    bool TeleportsToTown = false,
    // Resurrect: targets a DEAD ally (or self, via a scroll) — revives them to 30% HP/MP and restores
    // ResExpPct (0..1) of the exp they lost to the death penalty. The SkillEffect enum is full, so this
    // rides as a flag field.
    bool Resurrect = false, float ResExpPct = 0f,
    // KeepsBuffsOnDeath (Angel's Protection / noblesse): a self-buff that, while up, makes death remove ONLY
    // the protection buff(s) and keep every OTHER buff. Rides as a flag field so a buff with no stat effect
    // can still exist purely as this marker. The SkillEffect enum is full.
    bool KeepsBuffsOnDeath = false,
    // AutoResurrect: a preservation buff that ALSO auto-revives you on death (30% HP/MP, no prompt) instead
    // of leaving you dead — the future tank self-res / healer target-auto-res. Angel's Protection does NOT
    // set this (it only preserves buffs; you still need a manual res). Groundwork: no shipped skill uses it yet.
    bool AutoResurrect = false)
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
    public int FlatAt(int level) => Lvl(level)?.Flat ?? Flat;
    public float ModAt(int level) => Lvl(level)?.Mod ?? Mod;
    /// <summary>Resurrect skills: the fraction of lost exp restored at a given level (falls back to the
    /// SkillDef's ResExpPct for a single-level res, e.g. the scrolls).</summary>
    public float ResExpPctAt(int level) => Lvl(level)?.ResExpPct ?? ResExpPct;

    /// <summary>Resolve this level's PHYSICAL {Flat, Mod}. The old <c>Power</c> IS the Flat part and Mod
    /// defaults to 1, so an untuned skill is exactly K·(Power + pAtk)/def (no change). To make a skill scale
    /// with pAtk later, just set <c>Mod</c> (e.g. 2.0) — the Power stays as the flat FLOOR automatically;
    /// set <c>Flat</c> only to override that floor. Feed to StatCalculator.PhysicalDamageFM.</summary>
    public (int Flat, float Mod) PhysDamageAt(int level) =>
        (FlatAt(level) > 0 ? FlatAt(level) : PowerAt(level), ModAt(level) > 0f ? ModAt(level) : 1f);

    /// <summary>Resolve this level's MAGIC {Flat, Mod}, with the legacy fallback: Mod 0 → (Flat=0,
    /// Mod=Power) so an old skill's K·Power·√mAtk/def is reproduced. Feed to StatCalculator.MagicDamageFM.</summary>
    public (int Flat, float Mod) MagicDamageAt(int level) =>
        ModAt(level) > 0f ? (FlatAt(level), ModAt(level)) : (0, PowerAt(level));
    public EffectMagnitude[]? MagnitudesAt(int level) => Lvl(level)?.Magnitudes ?? Magnitudes;
    public int MpCostAt(int level) => Lvl(level)?.MpCost ?? MpCost;
    public int SpCostAt(int level) => Lvl(level)?.SpCost ?? SpCost;
    /// <summary>GOLD price of a level (0 = not bought with gold).</summary>
    public int GoldCostAt(int level) => Lvl(level)?.GoldCost ?? 0;
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

/// <summary>Per-armor-weight <see cref="StatMods"/> profile for an armor-mastery PASSIVE
/// (one entry per skill level). The worn BODY weight selects which StatMods applies in
/// Entity.RecomputeDerived — pure per-level DATA, no character-level/class formula (the same
/// pattern future classes reuse for weapon-type-conditional passives).</summary>
public readonly record struct ArmorMasteryProfile(
    StatMods Robe, StatMods Light, StatMods Heavy,
    // No BODY armor equipped. Defaults inert; a caster mastery sets it so wearing NOTHING
    // is penalised like the wrong weight (can't dodge the robe requirement by going naked).
    StatMods None = default);

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
    // Fallback for ANY other equipped state — EMPTY HAND, or a base type without its own slot.
    // Lets a caster mastery penalise "anything but sword/blunt", empty hand included.
    PassiveEffect Other = default,
    // When set, the bonus applies ONLY if the equipped weapon's type is in this [Flags] MASK
    // (e.g. Warrior trains WeaponType.TwoHanded). None = any. The Sword/Blunt slots serve both
    // 1H and 2H via WeaponType.Base(); Dual/Bow are inherently 2H.
    WeaponType RequiredWeapon = WeaponType.None)
{
    /// <summary>The effect for the equipped weapon. Named slots for the four base types;
    /// <see cref="Other"/> for empty hand / anything else. Inert outside RequiredWeapon.</summary>
    public PassiveEffect For(WeaponType wt)
    {
        if (RequiredWeapon != WeaponType.None && (RequiredWeapon & wt) == 0) return default;
        return wt.Base() switch
        {
            WeaponType.Sword => Sword,
            WeaponType.Dual  => Dual,
            WeaponType.Bow   => Bow,
            WeaponType.Blunt => Blunt,
            _ => Other
        };
    }
}

/// <summary>Per-level tunables for a multi-level skill (see SkillDef.Levels). Only the
/// fields that change between levels live here; identity (id/name/effect/range/flags)
/// stays on the SkillDef. Level 1 = Levels[0].</summary>
public record SkillLevel(
    int Power = 0,
    int Flat = 0,
    float Mod = 0f,
    EffectMagnitude[]? Magnitudes = null,
    PassiveEffect? Passive = null,
    int MpCost = 0,
    int SpCost = 1,
    string? Description = null,
    int InitialMpCost = -1,   // -1 = inherit the SkillDef's split (0 up front)
    // GOLD price of this level (0 = free). The stat-swap passives are bought with gold, not SP.
    int GoldCost = 0,
    // Resurrect skills: fraction (0..1) of the target's lost exp restored at THIS level.
    float ResExpPct = 0f);

/// <summary>Skill window grouping. Passive = a learned, always-on effect (armor
/// masteries, discipline passives) — never cast and never placed on the action bar.</summary>
public enum SkillCategory { Physical = 0, Magic = 1, Buff = 2, Debuff = 3, Heal = 4, Passive = 5 }

/// <summary>Which ROW of the buff bar an effect belongs in. This is the effect's SUBTYPE — what
/// granted it — not what it does, so the client can group them: a Might buff and a Swiftness
/// potion both raise stats, but they belong in different rows because one came from a buffer and
/// the other from your bag.
///   Buff       — row 1: ordinary buffs (what a buffer gives you).
///   Debuff     — row 2: harmful effects on you.
///   Item       — row 3: PERSISTENT item effects (armor sets, weapon special abilities). Always-on
///                while the gear is worn, so the client may hide/collapse this row.
///   Consumable — row 4: temporary effects from things you used (potions).
/// A debuff is still detected from its effect flags (AnyDebuff) and overrides whatever is set here,
/// so an offensive skill never has to declare a row.</summary>
public enum BuffRow { Buff = 0, Debuff = 1, Item = 2, Consumable = 3 }

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
    float DefencePct = 0f, float MagicDefencePct = 0f,  // percent def (−0.20 = ×0.8, e.g. 2H mastery)
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
    // Shield passive (Tank Shield Mastery): scale the equipped shield's block chance and
    // shield defence (fractions; only matter with a shield equipped). Re-clamped after passives.
    float BlockChancePct = 0f, float ShieldDefPct = 0f,
    // Bow range bonus (Rogue/Archer Weapon Mastery "range +200"): added to basic-attack range
    // while a BOW is equipped. Inert with any other weapon.
    float BowRange = 0f,
    int InterruptPower = 0, int InterruptResist = 0,
    float MeleeVamp = 0f, float SpellVamp = 0f,
    // Damage-OUT bonuses (fractions): the 2×3 matrix of context (PvE/PvP) × source
    // (skill=physical skill / magic / basic). Set as many as the effect should touch.
    float PveSkillDamagePct = 0f, float PveMagicDamagePct = 0f, float PveBasicDamagePct = 0f,
    float PvpSkillDamagePct = 0f, float PvpMagicDamagePct = 0f, float PvpBasicDamagePct = 0f,
    float CancelResistPct = 0f,   // chance each of your buffs resists an enemy cancel
    // Combat-resolution "sure" floors (see docs/CombatResolution.md). These are
    // GUARANTEES (the resolver takes the MAX across passives, not a sum):
    float EvadeFloor = 0f,        // min chance to dodge physical (rogue/archer)
    float HitFloor = 0f,          // min chance THIS entity lands a physical hit (warrior)
    float MagicFailFloor = 0f,    // min chance a spell fizzles vs this entity (tank/mage anti-magic)
    // FLAT addition to the casting-speed STAT (not a percent). This is how L2's spirit-
    // shots work: +40 flat on top of the multiplicative chain, so it matters a lot at low
    // cast speed and barely at high — unlike a percent, which compounds and runs away.
    float CastSpeedFlat = 0f,
    // ----- PRIMARY-stat deltas (the level-40 stat-swap passives). Folded in RecomputeDerived's
    // PRE-PASS, before anything is derived, so "+CON" genuinely raises Max HP and "+DEX" genuinely
    // raises evasion/accuracy/crit/attack-speed — not just a number in the stat window.
    // SPT is a full stat here like the rest (owner, 2026-07-20): a "±Spirit" swap is now literally
    // ±1 SPT per level, not a bundle of MaxMpPct/MagicDefencePct/MpRegenPct. Those percent fields
    // still exist for ordinary gear that touches only ONE of the three (a robe's +20% MP regen).
    int Con = 0, int Dex = 0, int Atk = 0, int Wit = 0, int Spt = 0,
    // Heal power (healer OUTPUT) and heal received (target side). Heals no longer use M.Atk:
    // endHeal = (HealPowerFlat + skillPower)·HealPowerMod, then the target's (HealReceivedFlat +
    // endHeal)·HealReceivedMod. Default 0 flat / +0% (so an untrained healer heals exactly skillPower).
    int HealPowerFlat = 0, float HealPowerPct = 0f,
    int HealReceivedFlat = 0, float HealReceivedPct = 0f);

/// <summary>Who a skill affects. SelfOnly = caster only; AlliesInRadius = caster + party members
/// in radius (heals/buffs); EnemiesInRadius = every HOSTILE in radius (an offensive AoE, e.g. a
/// boss slam — mobs hit players, players hit mobs).</summary>
public enum TargetMode { SelfOrTarget = 0, SelfOnly = 1, AlliesInRadius = 2, EnemiesInRadius = 3 }

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
    private static readonly Dictionary<string, SkillDef> All;

    // Built in an explicit static ctor, NOT a field initializer: BuildCatalog() reads static arrays
    // declared in the other partial files (StatSwapGold, FighterArmorLevels, ...), and field
    // initializers across partial files run in compiler file order — so a field initializer here
    // could (and did) run while those are still null. A static ctor body runs after all of them.
    static SkillCatalog() => All = BuildCatalog();

    /// <summary>THE single place the catalog is assembled — one AddRange per
    /// definition file. Duplicate ids throw at startup (collision guard).</summary>
    private static Dictionary<string, SkillDef> BuildCatalog()
    {
        var list = new List<SkillDef>();
        list.AddRange(CommonSkills());        // Skills.Common.cs
        list.AddRange(StatSwapSkillDefs());   // Skills.StatSwap.cs (the level-40 +stat/−stat passives)
        list.AddRange(FighterSkills());       // Skills.Fighter.cs
        list.AddRange(MageSkills());          // Skills.Mage.cs
        list.AddRange(HealerSkills());        // Skills.Healer.cs (2nd-class Healer kit)
        list.AddRange(ArmorMasterySkills());  // Skills.Masteries.cs (data-driven per-archetype)
        list.AddRange(WeaponMasterySkills()); // Skills.WeaponMasteries.cs (weapon-type-conditional)
        list.AddRange(BufferSkills());        // Skills.Buffer.cs (NPC newbie-buffer buffs)
        list.AddRange(LightbringerSkills());  // Skills.Lightbringer.cs
        list.AddRange(WarchanterSkills());    // Skills.Warchanter.cs
        list.AddRange(MobSpellSkills());      // Skills.MobSpells.cs (caster-mob nuke + jab)

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

    /// <summary>Divisor on the FLAT heal. Solved against the owner's target: a **1000-power heal at
    /// level 76** with a tier-76 staff (M.Atk ≈ 900) should land **~2000**.
    ///   heal = 1000 × √900 / K = 30000 / K  →  K = 15.
    /// That target came from L2 itself: there `heal = power + √mAtk`, so a 1000-power heal lands
    /// ~1025-1080 almost regardless of M.Atk (it contributes 3-8%) — and L2's skill ENCHANT roughly
    /// doubles it, giving ~2100. We aim at the enchanted value because we have no enchant system.
    /// (K = 8 gave 3750 at L76 — the "paladin heals 3k" number the owner explicitly rejected.)</summary>
    public const float HealK = 15f;

    /// <summary>The FLAT part of a heal: skill power scaled by the healer's M.Atk (√ = diminishing
    /// returns, as in every other magic formula). This is a healer's bread-and-butter heal — cheap,
    /// fast, and it SUFFERS the moment you trade your caster weapon for a damage one, because the
    /// weapon channel factors suppress M.Atk. "Want to be a fighter? Then you heal less."
    ///
    /// NOTE this deliberately DIVERGES from L2, which is ADDITIVE — `power + √mAtk` — where M.Atk is
    /// almost irrelevant (16,000 M.Atk buys only +126 HP, and a sword-cleric heals essentially as
    /// well as a staff-cleric). Multiplying instead is what makes gear and weapon choice matter.
    ///
    /// The %-of-max-HP part of a heal does NOT come through here: it ignores M.Atk entirely and
    /// ignores heal-reduction. That's the point of it — when an anti-heal ultimate lands, the big
    /// flat heals wither and only the % heals still work.</summary>
    /// <summary>Heal OUTPUT (endHeal) — owner 2026-07-17: NO M.Atk. = (HealPowerFlat + skillPower)·HealPowerMod.
    /// With the default HealPower (0 flat / ×1) a heal is EXACTLY its skill power, so nobody overheals unless
    /// a class / gear / buff grants HealPower. The target's HealReceived is applied separately in HealOne.</summary>
    public static int HealAmount(int skillPower, int healPowerFlat, float healPowerMod) =>
        Math.Max(1, (int)((healPowerFlat + skillPower) * healPowerMod));

    public const float CritMultiplierSkills = 2.0f;
    public const int PhysicalSkillAccuracyBonus = 10;
}
