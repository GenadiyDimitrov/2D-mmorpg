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
/// "flame_bolt"). Timings are in server ticks (10/s). Effect is a [Flags]
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
    // {Flat, Mod} damage model (docs/design/DamageModel.md): physical = 77·(Flat + Mod·pAtk)/def,
    // magic = 91·(Flat + Mod·√mAtk)/def. Mod == 0 → LEGACY: fall back to Power (physical adds Power
    // to pAtk; magic multiplies √mAtk by Power) so every existing skill is unchanged.
    int Flat = 0,
    float Mod = 0f,
    int DurationTicks = 0,
    EffectMagnitude[]? Magnitudes = null,
    string BuffKey = "",
    int Rank = 0,
    // 🔑 BL-85: a multi-level buff with NO children used to resolve to this flat Rank at EVERY rung,
    // so rung 1 and rung 5 competed as equals — and equal rank keeps whichever has the longer time
    // left, so a level-44 Warchanter's Harmony Lv1 replaced a level-74's Lv5. BuffPlan now carries
    // the level (`Rank + level - 1`) for a childless multi-level buff, exactly as GroupRank already
    // does for groups. Set this TRUE to opt out and keep the flat number at every rung.
    //
    // ⚠ There is exactly one reason to opt out, and it is the "one or the other, never both" pair:
    // Great Might and Great Bulwark deliberately SHARE a key at Rank 1 so casting either evicts the
    // other and the choice stays re-makeable mid-fight. Carrying the level there would let a Lv3
    // Might lock out a Lv1 Bulwark, which is the opposite of that ruling — see Skills.Lightbringer.cs.
    bool FlatRank = false,
    string[]? Replaces = null,
    // The single buff(s) this skill hands out — see docs/design/BuffLadders.md. The COUNT decides
    // what it is:
    //
    //   ONE child  = a WRAPPER (a potion, a scroll, a buffer's blessing, a class's single buff). The
    //                buff that lands is the CHILD — the family's rung, on the family's key, at the
    //                family's rank — and the wrapper contributes only DURATION, target mode, MP cost
    //                and the icon. That is what makes a Greater Might potion and a cleric's Might
    //                the same buff from different bottles, so they can never stack.
    //   MANY       = an IMPROVED (group) buff. It lands as ONE buff carrying every child's numbers,
    //                on the skill's own BuffKey, at GROUP rank (above every ladder), COVERING each
    //                child's family: it evicts those singles when it lands and no single, potion or
    //                scroll can override any part of it afterwards. One blessing, one bar square.
    //
    // ⚠ AUTHORING RULE for a group: it locks its families out, so it must be at least as strong as
    // the best single obtainable in EVERY family it contains, or it is a downgrade the player cannot
    // refuse. Today every group is granted at its max level only (Warchanter 66-74, the NPC/admin
    // sets), which satisfies it.
    // The skill's own Effect must still be the UNION of the children's flags — that is what marks
    // it a buff at all (AnyBuff gates the cast/consume paths). null/empty = an ordinary single buff.
    string[]? ChildBuffs = null,
    string Description = "",
    int SpCost = 1,
    SkillCategory Category = SkillCategory.Physical,
    /// <summary>Which buff-bar ROW this skill's effect lands in. Defaults to the ordinary buff row;
    /// a debuff is detected from its effect flags regardless. Set Consumable on potion skills and
    /// Item on always-on gear effects.</summary>
    BuffRow BuffRow = BuffRow.Buff,
    /// <summary>Mutually-exclusive group ("" = none). Learning ANY skill in a group permanently
    /// locks out every other skill in it — you commit to one trade-off. Used by the level-40
    /// stat-swap passives: take +CON−AGI and you can never also take +CON−ATK.</summary>
    string ExclusiveGroup = "",
    TargetMode TargetMode = TargetMode.SelfOrTarget,
    float AreaRadius = 0f,
    /// <summary>Extra interrupt resistance THIS cast carries, as a FRACTION (0.25 = 25%). Added to the
    /// caster's own resist buffs inside IG's <c>(1 - Buffs)</c> term. Unauthored today — it is the lever
    /// for "this particular spell is hard to break" without touching the caster's sheet.</summary>
    float InterruptDefense = 0f,
    /// <summary>Interrupt power this skill's hit carries, in percentage POINTS added to the final
    /// interrupt roll (Disrupt: 99999 = a guaranteed cancel).</summary>
    int InterruptPower = 0,
    /// <summary>This skill's MULTIPLIER on the interrupt chance it rolls when it hits a caster
    /// (`BL-91`). 1 = ordinary. This is his nuker 3rd column *"Higher chance to interrupt enemy
    /// casts"* — a multiplier rather than more <see cref="InterruptPower"/> because power is a STAT
    /// term (it rides the attacker/defender points ratio, so its value depends on the target's WIT and
    /// level) while "this spell is better at interrupting" is a property of the spell alone.
    /// <para>⚠ Unrelated to <see cref="DebuffLandMod"/>, and deliberately so: his rule for those
    /// spells is *"does dmg but have a lower success rate for the slow - interrupt unaffected"*.</para></summary>
    float InterruptMult = 1f,
    float BlockAccuracy = 0f,
    bool SureHit = false,
    // For contested crowd-control (Slow, later Stun/Root/Fear): which stat the LAND
    // chance contests against — Physical = ATK vs CON, Magical = ATK vs WIT.
    DebuffSchool DebuffSchool = DebuffSchool.None,
    /// <summary>PER-SKILL DEBUFF SUCCESS MULTIPLIER (`BL-90`, owner 2026-08-24). Multiplies the
    /// probability this skill's DEBUFF sticks — and nothing else. 1.0 = the stock chance.
    ///
    /// <para>🔑 HIS NUMBERS ARE READ OFF THE CONTESTED CURVE, WHOSE PARITY IS 50%: *"armor/weapon break
    /// + gravity + Arcane/Fros/Pyro blasts(nuker 3rd) should be 75% at parity (x1.5) and the other
    /// should be 25% at parity (x0.5)"*. So ×1.5 → 75%, ×1 → 50%, ×0.7 → 35%, ×0.5 → 25%, ×0.3 → 15%.
    /// A skill carrying one of these must therefore be CONTESTED, which for a non-`ContestCc` payload
    /// means declaring a <see cref="DebuffSchool"/> — see <c>GameLoopService.IsContestedDebuff</c>.</para>
    ///
    /// <para>🔑 THE VALUES LIVE IN HIS CSVs, NOT IN A TIER TABLE. He authors them into the DESCR column
    /// as <c>(success chance x1.5)</c>; `SkillCsvSeed --check` reads that column and compares it here,
    /// so the two cannot drift. Default 1 = untagged = the stock chance.</para>
    ///
    /// <para>It also works on the FIZZLE path (a debuff with neither a `ContestCc` flag nor a school),
    /// where it scales the surviving <c>1 − fail</c> — the same quantity, so one number means one thing
    /// everywhere. ⚠ But his parity arithmetic does NOT hold there (that base is ~99%, not 50%), so an
    /// authored value belongs with a school.</para>
    ///
    /// <para>🔑 IT NEVER TOUCHES DAMAGE OR INTERRUPT. His rule for the hybrid nukes is *"does dmg but
    /// have a lower success rate for the slow - interrupt unaffected"*: the damage arm keeps its own
    /// fizzle roll and the interrupt is a separate contest (<see cref="StatCalculator.InterruptChance"/>),
    /// so one cast can hit for full, break the target's cast, and still miss its rider.</para>
    ///
    /// <para>Per-LEVEL via <see cref="SkillLevel.DebuffLandMod"/> (a level's 0 = inherit), because his
    /// ask was explicitly *"a sucess multiplier (per skill/lvl)"* — a ladder can buy reliability as it
    /// climbs.</para></summary>
    float DebuffLandMod = 1f,
    // "[Double]" physical skills: a flat ×2 on the caster's ATK curve (2.5-25%). Ordinary
    // physical skills never double. Magic skills use magic crit.
    bool CanDouble = false,
    // "Can Crit" — a physical skill may roll the caster's CRIT rate. Owner ruling (playtest-19
    // M8): "if a skill is not described as Can Crit or Can Double it doesn't do it." These two
    // are EXCLUSIVE and both OPT-IN: a [Double] skill must never also crit, and a skill with
    // neither flag lands flat (it can still miss and still be blocked). BlowOnCrit implies the
    // crit roll — that IS the blow's landing gate — so a blow does not set this.
    bool CanCrit = false,
    // Weapon requirement: an ACTIVE skill only casts while the equipped weapon's type is in
    // this [Flags] MASK (e.g. Strike = Sword|Blunt, Stab = Dual, Shot = Bow). Checked at
    // cast-start with one bitwise-AND. None = usable with any weapon. Passives ignore this.
    WeaponType RequiredWeapon = WeaponType.None,
    // BLOW skill (dagger Stab): lands for FULL Power only if it CRITS (or, with CanDouble,
    // doubles). A blow that fails to crit deals only BlowFailFraction of its damage — a soft
    // floor, not IG's 0-damage whiff. Only meaningful with a physical-damage effect.
    bool BlowOnCrit = false,
    float BlowFailFraction = 0.10f,
    // A per-SKILL crit-rate modifier, multiplying the caster's own crit chance for THIS skill's
    // roll only (1.0 = exactly the character's rate). This is IG's rule that a blow's landing
    // chance was never the raw crit rate — Mortal/Deadly Blow carried a bonus of their own — and
    // it is the knob the crit-rate rework is paid for with: it lifts the dagger's BLOW without
    // touching his basic-attack crit, his buffs, or anyone else's numbers. Only read where a crit
    // is actually rolled: BlowOnCrit and CanCrit. Deliberately NOT capped by StatCaps.
    float CritRateMod = 1f,
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
    /// <summary>DAMAGE PER SECOND of this skill's damage-over-time, when that is NOT the same number as
    /// the skill's <see cref="Power"/>. 0 = use Power, which is what every DoT before the nuker's Pyro
    /// Burst did — a bleed IS its DoT, so one number served both. Pyro Burst hits for 150 on impact and
    /// then burns for his authored 100 a second, and there was nowhere to say so.</summary>
    int DotPower = 0,
    /// <summary>How much of the MP a target RECEIVES this debuff cuts away (0.70 = they get 30%). The
    /// mana twin of <see cref="SkillEffect.DebuffHealRecv"/>, which does the same to healing; Pyro Burst
    /// asks for both in one row ("decrease hp/mp received by 70%") and a burn that stopped heals but let
    /// the victim refill their bar with Restore Spirit would be half a skill. A FIELD, not a flag — the
    /// SkillEffect enum has no bits left.</summary>
    float MpReceivedPct = 0f,
    // Movement effects. BlinkRange: 0 = teleport the caster just BEHIND the target (gap-closer);
    // > 0 = teleport the caster that far AWAY from the target (escape). KnockbackRange: shove
    // the target that far away from the caster.
    float BlinkRange = 0f,
    float KnockbackRange = 0f,
    // A TOGGLE skill (stance/aura): clicking it applies its self-buff indefinitely;
    // clicking again removes it. Instant, no cast bar; MP charged on activation only.
    bool Toggle = false,
    // How many times ONE cast resolves against the target. 1 = an ordinary skill. The Warchanter's
    // Sound Burst is his *"Deals Physical Damag With Power +1000 Twice"* — TWO independent
    // resolutions of the SAME power, so each one rolls its own evasion, block and crit rather than
    // being one hit at double power. That distinction is the whole reason it is a field: a 2×1000
    // volley against an evasive target is worth less than one 2000, and against a shield it is worth
    // more. MP and cooldown are charged ONCE, for the cast.
    int HitCount = 1,
    // ---- ON-HIT PROC (Warchanter Combo Mastery, 2026-08-21) --------------------------------------
    // A PASSIVE that fires when its owner DEALS damage. ProcChance is rolled per damaging hit;
    // on success the two named skills' buffs are applied (self / party) at the SAME level as the
    // passive's own, and the passive will not fire again for ProcCooldownTicks. This is his
    // "Doing Damage Increases Attack/Cast Speed ... With 3% Chance" row, and it is the first proc in
    // the game — hence the fields rather than a bespoke hook. ⚠ RequiredWeapon is honoured: his row
    // says "Require: Box/Blunt", so it is inert with a bow.
    float ProcChance = 0f,
    int ProcCooldownTicks = 0,
    // DEFENSIVE trigger (the Sigils, 2026-08-26): roll this proc when the owner TAKES damage instead
    // of when they deal it. Same chance, same cooldown, same rung payload — only the moment differs,
    // and the two triggers are mutually exclusive on one passive. His sigils need both shapes in the
    // same breath ("3% chance on attack …" vs "3% chance on dmg received …"), so it is a flag on the
    // existing machinery rather than a second one.
    bool ProcOnDamaged = false,
    // Narrows a ProcOnDamaged trigger to MAGIC hits only — his Strong Spirit is *"with 20% chance on
    // magic dmg taken"*, and a passive that fired on a sword swing would be a different skill.
    // Inert unless ProcOnDamaged is set.
    bool ProcMagicOnly = false,
    // The buff rungs the proc hands out, INDEXED BY THIS PASSIVE'S LEVEL (rung[level-1]). Two arrays
    // because the caster and the party get DIFFERENT rungs of the SAME family — his design, 2026-08-21:
    // *"half of both goes to the party as buff (u get the 20% and party 10%) -> so something like 6
    // levels of that passives proc-buff and u get 4,5,6 while party gets 1,2,3"*. Keeping the offset in
    // the DATA rather than as an arithmetic rule is deliberate: which rung a level hands out is the
    // thing he is authoring, and it is legible here.
    string[]? ProcSelfRungs = null,
    string[]? ProcPartyRungs = null,
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
    // HIDE (BL-69, kind 1 — the rogue's full vanish): a self-cast that makes the caster invisible
    // to EVERYTHING for DurationTicks — unrendered, untargetable, and shed by every mob aggro'd on
    // them. Broken by any action but movement: a hit, a skill, a potion, damage taken.
    //
    // 🔑 The reveal is at EXECUTION, not at the click (his rule, and the reason a gap-closer works):
    // *"i want to click the skill and im not in range to start to move towards the target but still
    // invisible once the skill is executed then i appear."* Nothing on the cast-START path may end a
    // hide. (SkillEffect enum is full, so this rides as a flag field.)
    bool GrantsHide = false,
    // STEALTH (BL-69, kind 2): hides the target from UNAGGROED monsters only. Players still see
    // them, mobs already chasing keep chasing, and it does NOT break when they act — only when the
    // buff goes. Delivered as an ordinary buff (BuffInstance.HidesFromMobs), so it works both as a
    // rogue TOGGLE and as a buffer's timed party version with no second code path.
    bool GrantsMobStealth = false,
    // MP upkeep for a TOGGLE, charged once a second while it is up. The toggle drops itself when
    // the caster cannot pay. 0 = no upkeep (every other stance in the game).
    int MpPerSecond = 0,
    // HP upkeep for a TOGGLE, the twin of MpPerSecond and charged in the same tick. Holy Soul is the
    // first stance to want one (his row: 30% cheaper skills, 10% slower casts, *50 HP a second*) —
    // without it that toggle was a free MP discount, which is not what the row says.
    // ⚠ A stance NEVER kills you: the drain stops and the toggle drops itself while HP still remains,
    // so the trade is "hold it or heal", not "hold it and die".
    int HpPerSecond = 0,
    // REVEAL (BL-69): a non-damaging AoE that ends every HIDE within AreaRadius and stamps those it
    // caught with NoHideTicks, during which they cannot hide again. The counter to kind 1 — an
    // archer's answer to a rogue who is simply not there.
    bool RevealsHidden = false,
    int NoHideTicks = 0,
    // TRAP (Trapper): instead of hitting now, the cast DROPS a trap at the caster's feet that
    // arms and, when a hostile steps within TrapRadius, delivers THIS skill's damage + any
    // contested CC (Root/Stun/etc.) to that intruder, then vanishes. TrapLifeTicks = how long
    // it waits before expiring unused. Uses the skill's own Effect/Power/Magnitudes.
    bool PlacesTrap = false,
    float TrapRadius = 150f,
    int TrapLifeTicks = 300,
    // TOTEM (owner 2026-08-17, the Ork healer's level-40 skill — *"u can start with the totems, it
    // will be needed and probably will help for pets later"*). The cast plants a stationary object at
    // the caster's feet which PULSES this skill's payload to nearby ALLIES every TotemPulseTicks for
    // TotemLifeTicks, then expires. The mirror image of a trap: a trap waits once for an enemy and
    // dies; a totem fires repeatedly at friends on a timer.
    //
    // The pulse amount is the skill's own Power, so a totem's ladder is authored exactly like every
    // other heal's. TotemRadius is the reach around the TOTEM, not the caster — planting it well is
    // the skill.
    //
    // 🔑 WHICH POOL it fills is the skill's own Effect, not a totem setting: `Heal` = HP (Healing
    // Totem), `RestoreMp` = MP (Mana Totem), both = both. So a new totem flavour is an ORDINARY
    // heal/restore skill with PlacesTotem set — there is no second totem type to add, and the skill
    // card (SkillText) reads the same flags to describe itself.
    // A pulse goes through the SAME pipe as a cast (HealOne / RestoreMpOne), so heal-received and
    // restore-received percentages apply to it exactly as they would to a direct heal or recharge.
    bool PlacesTotem = false,
    float TotemRadius = 300f,
    int TotemLifeTicks = 300,
    int TotemPulseTicks = 10,
    // Fixed-timing flags (Return skill + future ultimate/event skills). FixedCast = cast time
    // ignores cast-speed (always the authored CastTicks). FixedCooldown = reuse ignores
    // cooldown-reduction buffs. FragileCast = ANY damage taken cancels the cast (bypasses the
    // interrupt contest). TeleportsToTown = on completion, teleport the caster to the nearest safe
    // town (the SkillEffect enum is full, so these ride as flag fields).
    bool FixedCast = false,
    bool FixedCooldown = false,
    bool FragileCast = false,
    bool TeleportsToTown = false,
    /// <summary>SP granted on use, for a consumable that IS skill points — the SP Bottle
    /// (owner, 2026-08-26). A FIELD and not a `SkillEffect` flag because the flag enum has no bits
    /// left, and because this is a one-off payload rather than a stat.
    /// <para>⚠ `Entity.SkillPoints` is a 32-bit int, so the grant CLAMPS at int.MaxValue — see the
    /// handler in <c>GameLoopService.UsePotion</c>. Two bottles fit; three do not.</para></summary>
    int GrantsSp = 0,
    // Resurrect: targets a DEAD ally (or self, via a scroll) — revives them to 30% HP/MP and restores
    // ResExpPct (0..1) of the exp they lost to the death penalty. The SkillEffect enum is full, so this
    // rides as a flag field.
    bool Resurrect = false, float ResExpPct = 0f,
    // How full the revived ally stands up — a FRACTION of their max HP and MP. 0 = the game default
    // (30%), which is what every res before the 4th tier used and what the scroll still uses. His
    // `healer 4th.csv` is the first thing to move it: *"Revive at 35% HP/MP"* @76, 40% @80.
    float ResHpPct = 0f,
    // KeepsBuffsOnDeath (Angel's Protection / noblesse): a self-buff that, while up, makes death remove ONLY
    // the protection buff(s) and keep every OTHER buff. Rides as a flag field so a buff with no stat effect
    // can still exist purely as this marker. The SkillEffect enum is full.
    bool KeepsBuffsOnDeath = false,
    // AutoResurrect: a preservation buff that ALSO auto-revives you on death (30% HP/MP, no prompt) instead
    // of leaving you dead — the future tank self-res / healer target-auto-res. Angel's Protection does NOT
    // set this (it only preserves buffs; you still need a manual res). Groundwork: no shipped skill uses it yet.
    bool AutoResurrect = false,
    // IMMORTALITY (the Immortality Sigil, owner 2026-08-26): while this buff is up the holder's HP is
    // FROZEN. His words, verbatim: *"Immortality: HP is unchanged; move and casts are unnafected ->
    // also healing dont increase HP as DMG dont decrease it"*.
    //
    // 🔑 NOT `Entity.Immune`, which makes attacks MISS. A hit still lands here — it still threatens,
    // still flags PvP, still contests your cast, still applies its debuff — it just moves no HP. And
    // the freeze cuts BOTH ways, which is the balancing half of it: a healer cannot top you up during
    // it, so five seconds of immortality is five seconds you do not spend healing.
    // Rides as a flag field: the SkillEffect enum is full (1L << 62 was the last bit).
    bool FreezesHp = false,
    // REWARD RATES — what this buff does to the four things a monster pays (exp / sp / gold / drop
    // chance). This is the premium REWARD RUNE family; see RewardRates. A multi-level rune puts its
    // rung here per LEVEL (SkillLevel.Rewards) — read it with RewardsAt(level), never this field
    // directly. Rides as a field rather than a SkillEffect flag because the flag enum is FULL:
    // 1L << 62 was the last bit, and four channels would want four of them.
    RewardRates Rewards = default,
    // TAUNT POWER (BL-71) — how much threat a SkillEffect.Taunt skill is worth, on the same scale
    // as damage (1 threat = 1 damage). A taunt first jumps the caster to the TOP of the table and
    // then adds this on top, so the number reads directly as "how much damage another player must
    // out-deal me by to steal this back". That is why the owner sized it against a skill hit:
    // a tank taunt has to be tens of thousands late on, or a single 7-8k physical skill takes the
    // mob off him. 0 = fall back to a small default (see GameLoopService).
    //
    // Per-level on SkillLevel.TauntPower; read it with TauntPowerAt(level), never this field.
    // Rides as a field rather than reusing Power because a taunt may ALSO do damage.
    int TauntPower = 0,
    // MOB-ONLY targeting (BL-70). The skill refuses a player target outright rather than fizzling
    // on one. The rogue's Lure is what this is for: it is a taunt, and a taunt aimed at a person is
    // meaningless — but without an explicit refusal it would look like a working PvP skill that
    // silently does nothing, which is worse than a skill that says no.
    bool MobTargetOnly = false,
    // SKILL EVASION (BL-06) — the chance the HOLDER of this buff dodges an incoming physical SKILL.
    // A physical skill is never evaded by the ordinary accuracy-vs-evasion contest any more (owner,
    // playtest-21 `69e`: *"normaly no1 can evade a physical skill … now on then i miss a skill which
    // is anoying — stab fails"*); the ONLY way one is dodged is an explicit grant like this.
    // Rides as a field, not a SkillEffect flag — the flag enum is full (1L << 62 was the last bit).
    float SkillEvadeChance = 0f,
    // CONTROL RESISTANCE, PER SCHOOL (owner 2026-08-17 — the healer's Clarity and Fortitude). Cuts the
    // LAND chance of a contested debuff against the HOLDER of this buff, on top of the school-blind
    // `CcResist` that armour grants. TWO numbers rather than one because his two rows are two separate
    // buffs with different values: *"30% resist to SPT debuffs"* and *"15% resist to CON debuffs"* —
    // and because the schools are defended by different stats (SPT vs CON), so a single number could
    // never express "tough in body, open in mind".
    // Rides as fields, not SkillEffect flags — the flag enum is full (1L << 62 was the last bit).
    float CcResistMagical = 0f,
    float CcResistPhysical = 0f,
    // MAGIC CRIT DAMAGE (owner 2026-08-19 — the 4th-class buffer/healer blessings). A FRACTION the
    // HOLDER's magic-crit multiplier is raised by: 0.30 = "+30% magic crit dmg", i.e. x2 base -> x2.6.
    // Two of these COMPOUND (x2 × 1.3 × 1.3 = x3.38), which is the owner's own arithmetic.
    // `MagicCritDamageDebuff` is the `(1 - debuffs)` term of the same formula — it SUBTRACTS, and is
    // summed rather than compounded, so two debuffs of 0.2 take 40% off and not 36%.
    // Rides as fields, not SkillEffect flags — the flag enum is full (1L << 62 was the last bit).
    float MagicCritDamage = 0f,
    float MagicCritDamageDebuff = 0f,
    // MANA DAMAGE (the healer's Mana Ray, his 3rd-class 56/58 rows — *"Like a magic attack but damages
    // MP not HP. Same formula; mRes; can fizzle; etc). In PVE dmg is halved (x0.5)"*). Set it on an
    // ordinary `SkillEffect.MagicDamage` skill and the number the magic pipeline produces is subtracted
    // from the target's MP instead of its HP — SAME formula, same M.Def divisor, same magic-resist
    // coefficient, same fizzle roll, same magic crit. That is the point: it is not a second damage
    // system, it is the one we already have, pointed at the other pool.
    //
    // 🔑 The PvE halving is NOT part of this flag — it is `PveDamageMult: 0.5f`, which already exists
    // above and is already applied by FinalizeDamage. Two knobs for one number would drift apart.
    // ⚠ Absorb shields, mana shields and the lethal save are all deliberately skipped for a mana hit
    // (a mana shield diverting mana damage INTO mana is a loop), and it can never kill: MP floors at 0.
    // Everything else a hit does — threat, the combat timer, PvP flagging, breaking a hide, the
    // interrupt contest — is unchanged, because being hit is being hit.
    // Rides as a flag field: the SkillEffect enum is full (1L << 62 was the last bit).
    bool DamageToMp = false,
    // ENDS ON DAMAGE TAKEN (the healer's Meditation, his 3rd-class 56 row — *"Decrease Pdef by
    // 90%(x0.1) and Add MP regen +30/s for 30 seconds - The status is canceled on dmg taken"*). While
    // this buff is up its owner is wide open and refilling fast; the moment anything actually lands on
    // them it is gone. Checked in ApplyDamage, the single choke point every source of damage passes
    // through, so a DoT tick, an AoE, a reflect and a mob's swing all end it with no per-source code.
    //
    // ⚠ NOT the same rule as a HIDE, which also breaks on the owner's own ACTIONS. This one only cares
    // about damage arriving — his words are "canceled on dmg taken", and whether a meditating healer
    // may cast is his call to make, not a rule to assume here.
    // Rides as a flag field for the same reason as DamageToMp.
    bool EndsOnDamageTaken = false,
    // NEVER AUTOMATABLE (owner 2026-08-19, on Mana Ray): *"can be removed from auto - no point in
    // 'farming' with it .. its a strategy move - depleate boss/enemy mp not a farming tool ... same
    // goes for 'Mana Strain'"*. The skill is sorted into the auto chain's never-cast bucket no matter
    // what its effect flags say, and `WarnUncastableAutoSkills` then TELLS the player, the moment they
    // arm the row, that it is theirs to press — so a deliberately manual skill can never again look
    // like the silent-skip bug playtest 23 found.
    //
    // 🔑 It is a property of the SKILL, not of its shape. Mana Ray classifies as an Attack purely
    // because it carries MagicDamage, and no rule about damage flags could tell it apart from a nuke;
    // the thing that makes it manual is what it is FOR. Anything else he rules manual — `BL-83`'s
    // taunts are the queued case — sets this one flag rather than growing another branch below.
    bool NeverAuto = false,
    // ---- THE BUFF-SLOT FLAG (owner, playtest 27) ------------------------------------------------
    // Does this buff occupy one of GameConstants.MaxBuffSlots? His rule, verbatim: *"A self buff that
    // is 20min still counts as a buff while a self 30s buff is temporary and is not .... For example
    // the bow expertise is a buff that counts toward the limit ... the flag is not self or not, the
    // flag is per buff .. default is true (counts towards max) - toggle don't and heals etc"*.
    //
    // 🔑 So it is NOT derived from TargetMode, and NOT derived from duration: it is authored, and it
    // DEFAULTS TO TRUE, which is the safe direction — a new blessing costs a slot unless its author
    // says otherwise. Today every `false` in the catalog is also ≤90 seconds, but that is a
    // consequence of what short buffs ARE, not the rule; a future 30-second blessing that should
    // cost a slot only has to say nothing.
    //
    // ⚠ Read it off the buff that LANDS, never the wrapper. A one-child wrapper (a potion, a scroll,
    // a blessing) resolves to its CHILD in ApplyBuff, so a Dash potion is flagged on `buff_dash_*`
    // and a Might potion is not flagged at all — a Might potion IS a single of the might family, out
    // of a bottle, and should cost its slot.
    //
    // Toggles are excluded by the engine regardless (CountsAgainstBuffCap), as are debuffs and the
    // gear/rune row; this flag is for the collected buffs that remain.
    // HEAL POWER (caster side) and HEAL RECEIVED (target side) granted by this BUFF. Fractions for the
    // two Pct fields, a flat number for the first. Ride as fields, like everything else down here,
    // because the SkillEffect flag enum is FULL. Per-LEVEL twins live on SkillLevel.
    // "M.Accuracy" this BUFF grants — flat percentage points off the holder's own spell-fail roll. The
    // mirror of SkillEffect.BuffMagicEvasion, which is a real flag; this rides as a field because the
    // enum has no bits left. Per-LEVEL twin on SkillLevel.
    float BuffMagicAccuracy = 0f,
    int BuffHealPowerFlat = 0,
    float BuffHealPowerPct = 0f,
    float BuffHealReceivedPct = 0f,
    bool CountsTowardBuffLimit = true)
{
    /// <summary>Hash on the ID alone — and this override MUST stay.
    ///
    /// A positional record's compiler-generated GetHashCode is ONE expression of the form
    /// <c>hash = hash * PRIME + field</c> repeated per field, which IL2CPP transpiles to equally
    /// nested C++ (<c>add(multiply(add(multiply(…))))</c>). With this record's ~100 members that
    /// exceeded clang's hard limit and **failed the Android build**:
    ///   Game.Shared__6.cpp: fatal error: bracket nesting level exceeded maximum of 256
    /// The desktop build is unaffected, so it only ever shows up as a broken phone build.
    ///
    /// Skill ids are unique and collision-guarded at startup, so the id IS the identity. Equals is
    /// left alone (it generates a flat &amp;&amp; chain, not a nested one) — differing skills that
    /// share a hash simply collide, which is legal.</summary>
    public override int GetHashCode() => Id?.GetHashCode() ?? 0;

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

    /// <summary>How long this level's effect lasts, in ticks. Falls back to the skill's own.</summary>
    public int DurationTicksAt(int level)
    {
        int v = Lvl(level)?.DurationTicks ?? 0;
        return v > 0 ? v : DurationTicks;
    }

    /// <summary>How many reagents this level consumes. Falls back to the skill's own.</summary>
    public int ConsumableAmountAt(int level)
    {
        int v = Lvl(level)?.ConsumableAmount ?? 0;
        return v > 0 ? v : ConsumableAmount;
    }

    /// <summary>This level's reuse delay in ticks, falling back to the skill's own.</summary>
    public int CooldownTicksAt(int level)
    {
        int v = Lvl(level)?.CooldownTicks ?? 0;
        return v > 0 ? v : CooldownTicks;
    }

    /// <summary>How full a revived ally stands up at this level, as a fraction of max HP/MP. Falls
    /// back through the level, then the def, then the game-wide 30%.</summary>
    public float ResHpPctAt(int level)
    {
        float lvl = Lvl(level)?.ResHpPct ?? 0f;
        if (lvl > 0f) return lvl;
        return ResHpPct > 0f ? ResHpPct : GameConstants.DefaultResurrectHpPct;
    }

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
    /// <summary>The CHILD buff ids this (group) buff applies at a level — an improved buff's levels
    /// are pure child references (see SkillDef.ChildBuffs). Null/empty = an ordinary single buff.</summary>
    public string[]? ChildBuffsAt(int level) => Lvl(level)?.ChildBuffs ?? ChildBuffs;
    public int MpCostAt(int level) => Lvl(level)?.MpCost ?? MpCost;
    /// <summary>HP price at a level (Restore Spirit). A level's -1 falls back to the SkillDef's
    /// HpCost, so a single-level HP skill needs no per-level entry at all.</summary>
    public int HpCostAt(int level)
    {
        int hp = Lvl(level) is { } sl ? sl.HpCost : HpCost;
        return hp < 0 ? HpCost : hp;
    }
    public int SpCostAt(int level) => Lvl(level)?.SpCost ?? SpCost;
    /// <summary>GOLD price of a level (0 = not bought with gold).</summary>
    public int GoldCostAt(int level) => Lvl(level)?.GoldCost ?? 0;
    public PassiveEffect? PassiveAt(int level) => Lvl(level)?.Passive ?? Passive;
    public string DescriptionAt(int level) => Lvl(level)?.Description ?? Description;
    /// <summary>The reward-rate package at a LEVEL — a rune ladder's rung. Falls back to the
    /// SkillDef's own for a single-level reward buff (Sinister / Sinners).</summary>
    public RewardRates RewardsAt(int level) => Lvl(level)?.Rewards ?? Rewards;

    /// <summary>Taunt threat at a LEVEL. A level's 0 means "inherit", so a single-level taunt
    /// needs no per-level entry at all (see SkillDef.TauntPower).</summary>
    public int TauntPowerAt(int level)
    {
        int t = Lvl(level)?.TauntPower ?? 0;
        return t > 0 ? t : TauntPower;
    }

    /// <summary>Per-school control resistance at a LEVEL. A level's 0 means "inherit", so a
    /// single-level resist buff needs no per-level entry (see SkillLevel.CcResistMagical).</summary>
    /// <para>⚠ The test is <c>!= 0</c>, not <c>&gt; 0</c>, since 2026-08-26: the nuker's Arcane Burst
    /// authors a NEGATIVE value (it EATS 40 points of the target's resistance), and a positives-only
    /// test would have silently discarded it and fallen back to the def. No existing skill authors a
    /// negative here, so nothing else changed.</para>
    public float CcResistMagicalAt(int level)
    {
        float v = Lvl(level)?.CcResistMagical ?? 0f;
        return v != 0f ? v : CcResistMagical;
    }

    /// <summary>How far this LEVEL blinks (0 = the def's BlinkRange). Phase Shift's whole ladder.</summary>
    public float BlinkRangeAt(int level)
    {
        float v = Lvl(level)?.BlinkRange ?? 0f;
        return v > 0f ? v : BlinkRange;
    }

    /// <summary>How many positive effects this LEVEL strips (0 = the def's, which in turn means "all
    /// matching"). Arcane Void's ladder is nothing but this number.</summary>
    public int DispelCountAt(int level)
    {
        int v = Lvl(level)?.DispelCount ?? 0;
        return v > 0 ? v : DispelCount;
    }

    /// <summary>The DoT damage per second this LEVEL deals: the level's own DotPower, else the def's,
    /// else the skill's Power — which is what every DoT authored before Pyro Burst relies on.</summary>
    public int DotPowerAt(int level)
    {
        int v = Lvl(level)?.DotPower ?? 0;
        if (v > 0) return v;
        return DotPower > 0 ? DotPower : PowerAt(level);
    }

    /// <summary>Heal POWER this level of the buff grants its holder (flat), falling back to the def.</summary>
    public int HealPowerFlatAt(int level)
    {
        int v = Lvl(level)?.HealPowerFlat ?? 0;
        return v != 0 ? v : BuffHealPowerFlat;
    }

    /// <summary>Heal POWER as a fraction, same fallback.</summary>
    public float HealPowerPctAt(int level)
    {
        float v = Lvl(level)?.HealPowerPct ?? 0f;
        return v != 0f ? v : BuffHealPowerPct;
    }

    /// <summary>Heal RECEIVED (target side) as a fraction, same fallback.</summary>
    public float HealReceivedPctAt(int level)
    {
        float v = Lvl(level)?.HealReceivedPct ?? 0f;
        return v != 0f ? v : BuffHealReceivedPct;
    }

    /// <summary>"M.Accuracy" points this level grants, falling back to the def's.</summary>
    public float MagicAccuracyAt(int level)
    {
        float v = Lvl(level)?.MagicAccuracy ?? 0f;
        return v != 0f ? v : BuffMagicAccuracy;
    }

    /// <summary>Per-school control resistance at a LEVEL. See <see cref="CcResistMagicalAt"/>.</summary>
    public float CcResistPhysicalAt(int level)
    {
        float v = Lvl(level)?.CcResistPhysical ?? 0f;
        return v > 0f ? v : CcResistPhysical;
    }

    /// <summary>This skill's DEBUFF SUCCESS MULTIPLIER at a LEVEL. A level's 0 means "inherit", so a
    /// skill carrying one flat factor needs no per-level entry at all. See <see cref="DebuffLandMod"/>.</summary>
    public float DebuffLandModAt(int level)
    {
        float v = Lvl(level)?.DebuffLandMod ?? 0f;
        return v > 0f ? v : DebuffLandMod;
    }

    /// <summary>Magic-crit-DAMAGE bonus at a LEVEL (a fraction: 0.30 = +30%). A level's 0 means
    /// "inherit", so a single-rung blessing needs no per-level entry — but a ladder can climb
    /// 0.10/0.20/0.30 like every other buff. See <see cref="CcResistMagicalAt"/>.</summary>
    public float MagicCritDamageAt(int level)
    {
        float v = Lvl(level)?.MagicCritDamage ?? 0f;
        return v > 0f ? v : MagicCritDamage;
    }

    /// <summary>The `(1 − debuffs)` side of the same stat, at a LEVEL. See <see cref="MagicCritDamageAt"/>.</summary>
    public float MagicCritDamageDebuffAt(int level)
    {
        float v = Lvl(level)?.MagicCritDamageDebuff ?? 0f;
        return v > 0f ? v : MagicCritDamageDebuff;
    }

    /// <summary>Skill MP-cost change at a LEVEL — positive makes skills CHEAPER (Mana Blessing),
    /// negative DEARER (Mana Strain). ⚠ The "unset = inherit" test is <c>!= 0</c>, not <c>&gt; 0</c> like
    /// its neighbours above: a negative value here is meaningful, and a <c>&gt; 0</c> test would have
    /// thrown away every rung of the curse and silently fallen back to the def's.</summary>
    public float PhysMpCostPctAt(int level)
    {
        float v = Lvl(level)?.PhysMpCostPct ?? 0f;
        return v != 0f ? v : PhysMpCostPct;
    }

    /// <summary>See <see cref="PhysMpCostPctAt"/>.</summary>
    public float MagicMpCostPctAt(int level)
    {
        float v = Lvl(level)?.MagicMpCostPct ?? 0f;
        return v != 0f ? v : MagicMpCostPct;
    }

    /// <summary>The highest ailment Rank this skill can strip at a LEVEL. A level's 0 means "inherit",
    /// so a cure with one flat ceiling needs no per-level entry (see SkillLevel.DispelMaxLevel).</summary>
    public int DispelMaxLevelAt(int level)
    {
        int m = Lvl(level)?.DispelMaxLevel ?? 0;
        return m > 0 ? m : DispelMaxLevel;
    }

    /// <summary>Authored range at a LEVEL. A level's 0 means "inherit", so every skill whose reach
    /// does not change with its level needs no per-level entry (see SkillLevel.Range).</summary>
    public float RangeAt(int level)
    {
        float r = Lvl(level)?.Range ?? 0f;
        return r > 0f ? r : Range;
    }

    /// <summary>Base cast time at a LEVEL, in ticks (0 = inherit the SkillDef's). Almost every skill
    /// casts in the same time at every rung — the exception is his Resurrection ladder, which is the
    /// whole reason this exists: 10s at 40 down to 5s at 62 (`healer 3rd.csv`), so investing in the
    /// skill is what makes a battlefield res possible, not just cast speed. Same "0 = inherit" shape
    /// as <see cref="RangeAt"/>. ⚠ Cooldown deliberately has no twin — no authored ladder moves it.</summary>
    public int CastTicksAt(int level)
    {
        int c = Lvl(level)?.CastTicks ?? 0;
        return c > 0 ? c : CastTicks;
    }

    /// <summary>Area radius at a LEVEL (0 = inherit the SkillDef's). His Resurrection Field grows its
    /// reach with the rung (600/700/800/900) while everything else about it stays put, so the radius
    /// needs the same per-level treatment <see cref="RangeAt"/> gives reach.</summary>
    public float AreaRadiusAt(int level)
    {
        float r = Lvl(level)?.AreaRadius ?? 0f;
        return r > 0f ? r : AreaRadius;
    }

    /// <summary>Threat a HEAL is worth to every monster fighting the people it helped:
    /// <c>power / castSeconds × 10 × peopleHealed</c>.
    ///
    /// The rate is his playtest-22 rule; the <b>× people</b> is his 2026-08-14 correction, and it puts
    /// a heal on exactly the same footing as a buff (<see cref="BuffThreat"/>) — a per-head value times
    /// the heads it reached. His worked example: a 1500-power party heal on a 10s cast is 150/s, and
    /// across a full party of 9 that is <b>13,500</b>, against 7,500 for the same power thrown at one
    /// ally in 2s. Blanketing the group is what takes the room's attention.
    ///
    /// One cast still produces ONE number, and each mob fighting any healed ally receives it once —
    /// the multiplier is the size of the cast, not a per-mob repeat.</summary>
    public static float SupportThreat(int power, int castTicks, int targets)
    {
        if (power <= 0 || targets <= 0) return 0f;
        float seconds = Math.Max(GameConstants.ThreatMinCastSeconds,
                                 castTicks / (float)GameConstants.TickRate);
        return power / seconds * GameConstants.ThreatHealFactor * targets;
    }

    /// <summary>Threat a BUFF is worth (owner, 2026-08-14): <c>grantLevel × 20 × peopleAffected</c>.
    ///
    /// A buff has no power to read, which is the whole reason this is a different formula rather than
    /// the heal one with a substitution — so it is priced on the two things it does have, the level it
    /// was learned at and how many people it lands on. See <see cref="GameConstants.ThreatBuffPerLevel"/>.
    ///
    /// <paramref name="grantLevel"/> is the CHARACTER level the class learns this rung at, so two buffs
    /// on one caster are worth different amounts. Callers pass the caster's own level only when no
    /// class list owns the skill.</summary>
    public static float BuffThreat(int grantLevel, int targets)
    {
        if (grantLevel <= 0 || targets <= 0) return 0f;
        return grantLevel * GameConstants.ThreatBuffPerLevel * targets;
    }

    public float MagnitudeOf(SkillEffect effect, ModifierMode mode, int level = 1)
    {
        var mags = Lvl(level)?.Magnitudes ?? Magnitudes;
        if (mags is null) return 0f;
        float sum = 0f;
        foreach (var m in mags)
            if (m.Effect == effect && m.Mode == mode) sum += m.Value;
        return sum;
    }

}

/// <summary>Per-armor-weight <see cref="StatMods"/> profile for an armor-mastery PASSIVE
/// (one entry per skill level). The worn BODY weight selects which StatMods applies in
/// Entity.RecomputeDerived — pure per-level DATA, no character-level/class formula (the same
/// pattern future classes reuse for weapon-type-conditional passives).</summary>
/// <remarks>Every weight defaults to INERT (2026-08-07). Since masteries STACK and the wrong-weight
/// penalty lives in exactly one skill (Spellcaster Mastery), a class mastery normally states only the
/// weights it REWARDS — an omitted weight means "this skill has nothing to say about it", not "no
/// penalty applies". Before, `Light`/`Heavy` were required, which quietly pushed every author into
/// re-declaring the same penalty on every mastery.</remarks>
public readonly record struct ArmorMasteryProfile(
    StatMods Robe = default, StatMods Light = default, StatMods Heavy = default,
    // No BODY armor equipped — the "can't dodge the robe requirement by going naked" slot.
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
    // (A `MagicWeaponOnly` flag — gating on ItemDef.IsMagicWeapon rather than on the type — was added
    //  here on 2026-08-20 for the healer's mastery and removed the same day: he ruled the gate should
    //  be plain `Blunt`, so a mace still works and simply gives less. Nothing else wanted the flag, and
    //  a mastery that can refuse a weapon the type system says is fine is a wall, not a choice.)
    WeaponType RequiredWeapon = WeaponType.None)
{
    /// <summary>The effect for the equipped weapon. Named slots for the four base types;
    /// <see cref="Other"/> for empty hand / anything else. Inert outside RequiredWeapon.</summary>
    public PassiveEffect For(WeaponType wt)
    {
        if (!wt.Satisfies(RequiredWeapon)) return default;
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
    // GOLD price of this level (0 = free). The stat-swap passives are bought with gold, not SP.
    int GoldCost = 0,
    // Resurrect skills: fraction (0..1) of the target's lost exp restored at THIS level.
    float ResExpPct = 0f,
    // Revive fullness at THIS level (0 = inherit the SkillDef's, which in turn falls back to 30%).
    float ResHpPct = 0f,
    // IMPROVED (group) buff: the CHILD buff ids this LEVEL applies (see SkillDef.ChildBuffs).
    // A group buff's levels are pure child references — level N simply names a stronger rung of
    // each family's ladder. null = inherit the SkillDef's ChildBuffs.
    string[]? ChildBuffs = null,
    // HP price of THIS level (-1 = inherit the SkillDef's HpCost). Restore Spirit is the only
    // skill paid in HP, and it is the reason this exists: a fixed HP price on a skill whose MP
    // return grows for 55 levels is either free at 80 or unpayable at 25.
    int HpCost = -1,
    // REWARD RATES for THIS rung of a reward-rune ladder (null = inherit the SkillDef's).
    RewardRates? Rewards = null,
    // TAUNT POWER at THIS level (0 = inherit the SkillDef's). See SkillDef.TauntPower.
    int TauntPower = 0,
    // RANGE at THIS level (0 = inherit the SkillDef's). Almost no skill needs this — range is a
    // property of the spell, not of how far up its ladder you are — but the rogue's LURE is exactly
    // the exception: its whole ladder IS reach (200/400/600), because how far away you can start a
    // pull is the skill.
    float Range = 0f,
    // BASE CAST TIME at THIS level, in ticks (0 = inherit the SkillDef's CastTicks). His Resurrection
    // shortens from 10s to 5s as it climbs — the rung is what buys a res you can land in a fight.
    int CastTicks = 0,
    // AREA RADIUS at THIS level (0 = inherit the SkillDef's AreaRadius). Resurrection Field's reach
    // is its ladder (600/700/800/900); nothing else in the game grows an area with its rung.
    float AreaRadius = 0f,
    // HOW STRONG AN AILMENT THIS LEVEL CAN STRIP (0 = inherit the SkillDef's DispelMaxLevel). This is
    // a cure's whole ladder: WHAT it cures never changes, only how far up the ailment's own Rank it
    // can reach. Owner 2026-08-17: *"antidote have lvl and should cure poison + bleed below some level
    // ... you get a posion 10 and bleed 5 .. ur antidote is lvl 3 (cures below lvl 7) and cures the
    // bleed only"*. Same "unset = inherit" shape as TauntPower above.
    int DispelMaxLevel = 0,
    // PER-SCHOOL CONTROL RESISTANCE at THIS level (0 = inherit the SkillDef's). Clarity and Fortitude
    // climb a ladder like every other buff; see SkillDef.CcResistMagical.
    float CcResistMagical = 0f,
    float CcResistPhysical = 0f,
    // MAGIC CRIT DAMAGE at THIS level (0 = inherit the SkillDef's). See SkillDef.MagicCritDamage.
    float MagicCritDamage = 0f,
    float MagicCritDamageDebuff = 0f,
    // SKILL MP-COST CHANGE at THIS level (0 = inherit the SkillDef's). Positive = costs LESS (Mana
    // Blessing), negative = costs MORE (Mana Strain). Both of those are LADDERS — 10/15/20% and
    // 100%…200% — and without a per-level slot every rung would have silently applied rung 1's number,
    // because the fields live on the SkillDef and a def has one of each.
    float PhysMpCostPct = 0f,
    float MagicMpCostPct = 0f,
    // DEBUFF SUCCESS MULTIPLIER at THIS level (0 = inherit the SkillDef's). See SkillDef.DebuffLandMod.
    // His ask names the ladder explicitly — *"a sucess multiplier (per skill/lvl)"* — so a hold whose
    // rungs are otherwise identical can still get more reliable as it climbs.
    float DebuffLandMod = 0f,
    // DURATION at THIS level, in ticks (0 = inherit the SkillDef's DurationTicks). Almost every buff in
    // the game runs one duration for its whole ladder — the healer's 4th-tier Bind is the exception, and
    // it is his: *"intentional increasing in duration"*, 31s at 76 climbing to 40s at 90.
    int DurationTicks = 0,
    // REAGENT COUNT at THIS level (0 = inherit the SkillDef's ConsumableAmount). His Ultimate Heal costs
    // ONE Skill Stone at the 3rd tier and TWO at the 4th (the party version, four then five) — the same
    // skill, a dearer price for the stronger rungs, which is the only thing keeping it off the rotation.
    int ConsumableAmount = 0,
    // REUSE DELAY at THIS level, in ticks (0 = inherit the SkillDef's CooldownTicks). His 4th-tier
    // Resurrection drops from 10s to 5s and Ultimate Party Heal from 5s to 2s — a rung buying a shorter
    // reuse is a real ladder, and without this the whole skill would have taken the 4th tier's number.
    int CooldownTicks = 0,
    // HEAL POWER / HEAL RECEIVED granted by THIS level of a BUFF (0 = inherit the SkillDef's). The
    // healer's Healer's Power is a five-rung ladder of +1000 … +2000 flat heal power, and the Ork's
    // Spirit Restoration leaves +30% healing RECEIVED behind it. Both channels existed only as
    // PASSIVE fields until the 4th tier; these are their buff-side twins.
    int HealPowerFlat = 0,
    float HealPowerPct = 0f,
    float HealReceivedPct = 0f,
    float MagicAccuracy = 0f,
    // HOW MANY POSITIVE EFFECTS THIS RUNG STRIPS (0 = inherit the SkillDef's DispelCount). The nuker's
    // Arcane Void is the whole reason: 1-2 effects at 52, 2-3 at 62, 2-4 at 72 — the ladder IS the
    // count, and without a per-level slot every rung would have silently stripped rung 1's two.
    int DispelCount = 0,
    // DoT DAMAGE PER SECOND at THIS level (0 = inherit the SkillDef's DotPower, which in turn falls
    // back to Power). See SkillDef.DotPower.
    int DotPower = 0,
    // HOW FAR THIS RUNG BLINKS (0 = inherit the SkillDef's BlinkRange). The nuker's Phase Shift is the
    // rogue's Lure all over again — 200 / 400 / 600 — where the whole ladder is the distance.
    float BlinkRange = 0f);

/// <summary>What a buff does to the four things a MONSTER pays out: experience, skill points, the
/// gold it drops and the CHANCE its table rolls. The premium rune family (Rune of Experience /
/// Skillpoints / Exp-SP / Gold / Drop, and the two zeroing runes) is nothing but this record plus an
/// item to carry it.
///
/// <para>The four bonuses are FRACTIONS ADDED to a neutral 1 (0.25 = +25%). They are combined by
/// MAX, never summed (see Entity.RecomputeDerived): holding a +50% and a +20% Exp rune gives +50%,
/// not +70%, so an Exp/SP rune and a plain Exp rune can sit in the same bag without multiplying each
/// other. The whole point of one rune per channel is that the best one wins.</para>
///
/// <para><b>StopsExpSp</b> is the Rune of Sinister — the grinder's rune: farm items and gold with the
/// levelling switched OFF. <b>StopsGoldDrop</b> joins it on the Rune of Sinners, which zeroes all
/// four. A stop is a HARD OVERRIDE applied after the max, so no pile of bonus runes can dilute a
/// punishment.</para>
///
/// <para>These are MONSTER rewards. Quest rewards are authored numbers and are deliberately left
/// alone — his three phrasings ("exp/sp gain from monsters", "gold DROP amount", "drop CHANCE") are
/// all kill-side, and a rune that inflated hand-authored quest exp would need every quest retuned.</para></summary>
public readonly record struct RewardRates(
    float Exp = 0f,
    float Sp = 0f,
    float Gold = 0f,
    float Drop = 0f,
    bool StopsExpSp = false,
    bool StopsGoldDrop = false)
{
    /// <summary>Does this package do nothing at all? (Almost every skill in the game.)</summary>
    public bool IsNeutral => Exp == 0f && Sp == 0f && Gold == 0f && Drop == 0f
                             && !StopsExpSp && !StopsGoldDrop;
}

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
    // 🔑 SHIELD-GATED. The whole effect is off while no shield is equipped — his Healer's Shield
    // Mastery row is *"When Sheild is equiped"*, and until this existed those two numbers rode a
    // plain passive that a healer kept after dropping the shield for a two-handed staff.
    // ⚠ ALL-OR-NOTHING, unlike the per-field `BlockChancePct`/`ShieldDefPct` gate further down: those
    // scale the shield's OWN numbers and are inert without one anyway, while this gates effects
    // (heal power, MP regen) that would otherwise apply perfectly well bare-handed.
    bool RequiresShield = false,
    float MaxHpPct = 0f, float MaxMpPct = 0f,
    int MaxHp = 0, int MaxMp = 0,      // flat max HP / MP
    int Defence = 0, int MagicDefence = 0,
    float DefencePct = 0f, float MagicDefencePct = 0f,  // percent def (−0.20 = ×0.8, e.g. 2H mastery)
    int Attack = 0, float AttackPct = 0f,
    int PhysAtk = 0, int MagAtk = 0,   // flat, channel-specific (Weapon Mastery etc.)
    float PhysAtkPct = 0f, float MagAtkPct = 0f,  // percent, channel-specific
    int Evasion = 0, int Accuracy = 0,
    float CritRate = 0f, float CritDamage = 0f, float MagicCritRate = 0f,
    // MAGIC crit DAMAGE — a fraction the caster's ×2 base is raised by (0.30 = +30% → ×2.6).
    // Its own channel: `CritDamage` above is PHYSICAL and never touches a spell (owner ruling
    // 2026-08-06, kept). Compounds with buffs of the same kind. See StatCalculator.MagicCritMult.
    float MagicCritDamage = 0f,
    // FLAT crit damage — the class CSVs' "crit dmg +80". NOT a multiplier: it joins ATTACK
    // inside the damage ratio on a crit only, K·((atk + flat)·… )/def, then the crit multiplier
    // scales the result. Off a crit it does nothing. See docs/design/CritBlowAndDouble.md §3.
    float CritDamageFlat = 0f,
    float HpRegen = 0f, float MpRegen = 0f,            // FLAT regen per tick
    float HpRegenPct = 0f, float MpRegenPct = 0f,      // regen MULTIPLIER (additive: 0.20 = +20%)
    float AtkSpeedPct = 0f, float CastSpeedPct = 0f, float MoveSpeedPct = 0f,
    float CooldownPct = 0f,       // spell reuse-delay reduction (0.10 = -10%)
    // Defensive resists (fractions). MeleeVamp/SpellVamp = lifesteal fractions.
    float CritRateResist = 0f, float CritDmgResist = 0f, float BowResist = 0f,
    // MAGIC RESISTANCE — a DAMAGE REDUCTION, not a fizzle chance (owner ruling 2026-08-10, `57d`).
    // Authored the way the CSVs write it: "mRes +5%" = 0.05f. Sums across passives/buffs and lands
    // as a divisor inside M.Def, so +25% total is the mob ladder's 1.25 → ×0.8 magic damage taken.
    // ⚠ These used to be built as MagicFailFloor (a fizzle chance) ONLY because there was no magic
    // damage-reduction stat to put them in. There is one now. Don't route mRes back into fail.
    float MagicResist = 0f,
    // Shield passive (Tank Shield Mastery): scale the equipped shield's block chance and
    // shield defence (fractions; only matter with a shield equipped). Re-clamped after passives.
    float BlockChancePct = 0f, float ShieldDefPct = 0f,
    // ⚠ NOT the same stat as ShieldDefPct above. This raises the wearer's WHOLE physical defence —
    // armour, jewels, everything — but only while a shield is equipped; ShieldDefPct scales the
    // shield's own contribution. Shield Mastery rungs 3-4 carry "+10% P.Def" and he ruled it
    // shield-conditional (2026-08-21: *"The 10% pDef (overall pDef not only shieldPDef) is only when
    // shield is equipped"*). It exists as its own field because plain DefencePct is unconditional and
    // is used by masteries that must keep paying with a two-hander.
    float DefencePctWithShield = 0f,
    // Bow range bonus (Rogue/Archer Weapon Mastery "range +200"): added to basic-attack range
    // while a BOW is equipped. Inert with any other weapon.
    float BowRange = 0f,
    // ⚠ UNITS, since IG's interrupt formula landed (2026-08-26): InterruptPower is percentage POINTS
    // added to the final interrupt roll; InterruptResist is a FRACTION subtracted from it (0.10 = 10%).
    int InterruptPower = 0, float InterruptResist = 0f,
    float MeleeVamp = 0f, float SpellVamp = 0f,
    // MANA vampirism — a BASIC physical attack returns this fraction of its damage to you as MP.
    // His `buffer 3rd.csv` Mana Vampirism row, and deliberately its own field rather than a reuse of
    // MeleeVamp: that one heals HP, this one refills the bar the buffer actually runs out of. Basic
    // attacks only ("physical basic atack only" — a skill never drains), and gated by RequiredWeapon
    // on the mastery that grants it.
    float ManaVamp = 0f,
    // Damage-OUT bonuses (fractions): the 2×3 matrix of context (PvE/PvP) × source
    // (skill=physical skill / magic / basic). Set as many as the effect should touch.
    float PveSkillDamagePct = 0f, float PveMagicDamagePct = 0f, float PveBasicDamagePct = 0f,
    float PvpSkillDamagePct = 0f, float PvpMagicDamagePct = 0f, float PvpBasicDamagePct = 0f,
    float CancelResistPct = 0f,   // chance each of your buffs resists an enemy cancel
    // Combat-resolution "sure" floors (see docs/design/CombatResolution.md). These are
    // GUARANTEES (the resolver takes the MAX across passives, not a sum):
    float EvadeFloor = 0f,        // min chance to dodge physical (rogue/archer)
    float HitFloor = 0f,          // min chance THIS entity lands a physical hit (warrior)
    // MAGIC has no floor — it is not resolved by ResolveAvoidChance at all since 2026-08-10. The
    // defender's only lever is this MULTIPLIER on the fail formula (the tank's Anti-Magic = 2).
    // The resolver takes the MAX across passives, like the floors above, never a product.
    float MagicFailMod = 0f,
    // THE CASTER'S OWN side of the same roll, and the OPPOSITE convention: a PRODUCT, not a max, so
    // penalties and bonuses compose in one chain (owner, 2026-08-20). The untrained weapon puts ×25
    // in; a passive carrying 0.04 takes it back out exactly. ⚠ 0 = NOT IN THE CHAIN, never ×0 —
    // `default(PassiveEffect)` is the inert value every unset mastery slot hands out.
    float MagicFailSelfMult = 0f,
    // THE OTHER TWO THIRDS OF THE UNTRAINED-WEAPON PENALTY, on the same "0 = not in the chain,
    // otherwise a PRODUCT" convention as MagicFailSelfMult above. Spellcaster Mastery puts ×0.5 into
    // each of CastSpeedPenaltyMult and MagicWeaponPenaltyMult for a bow/dagger/bare hand; a passive
    // carrying 2.0 takes one back out exactly. Written for the Elf Warchanter's Harmonist Bow
    // Proficiency — *"Bow: Removed Penalty [cast(x2), mAtk(x2), mAcc(x0.04)]"* — which is the first
    // skill in the game that UNDOES the caster weapon rule instead of working around it.
    // ⚠ Applied AFTER the penalty block in RecomputeDerived, so the order the two are written in
    // does not matter; they are factors on the same two numbers.
    float CastPenaltyMult = 0f,
    float MagicPenaltyMult = 0f,
    // FLAT addition to the casting-speed STAT (not a percent). This is how IG's spirit-
    // the spell rune works: +40 flat on top of the multiplicative chain, so it matters a lot at low
    // cast speed and barely at high — unlike a percent, which compounds and runs away.
    float CastSpeedFlat = 0f,
    // ----- PRIMARY-stat deltas (the level-40 stat-swap passives). Folded in RecomputeDerived's
    // PRE-PASS, before anything is derived, so "+CON" genuinely raises Max HP and "+AGI" genuinely
    // raises evasion/accuracy/crit/attack-speed — not just a number in the stat window.
    // SPT is a full stat here like the rest (owner, 2026-07-20): a "±Spirit" swap is now literally
    // ±1 SPT per level, not a bundle of MaxMpPct/MagicDefencePct/MpRegenPct. Those percent fields
    // still exist for ordinary gear that touches only ONE of the three (a robe's +20% MP regen).
    int Con = 0, int Agi = 0, int Atk = 0, int Wit = 0, int Spt = 0,
    // Heal power (healer OUTPUT) and heal received (target side). Heals no longer use M.Atk:
    // endHeal = (HealPowerFlat + skillPower)·HealPowerMod, then the target's (HealReceivedFlat +
    // endHeal)·HealReceivedMod. Default 0 flat / +0% (so an untrained healer heals exactly skillPower).
    int HealPowerFlat = 0, float HealPowerPct = 0f,
    int HealReceivedFlat = 0, float HealReceivedPct = 0f,
    // ----- The three DEFENCE CHANNELS against skills (BL-06/07/08, playtest-21 "New formulas").
    // All three are GUARANTEES, so the resolver takes the MAX across passives/buffs, never a sum —
    // the same rule as EvadeFloor / HitFloor / MagicFailMod above. -----
    // BL-06: chance to dodge an incoming PHYSICAL SKILL. Nothing else can dodge one.
    float SkillEvadeChance = 0f,
    // BL-07: a physical skill that hits you is reflected back at its caster — Chance to reflect,
    // Pct of the damage returned. His own framing: *"we can have a 100% chance to reflect 15% p
    // skill dmg, or 15% chance to reflect 100% p skill dmg"*; the warrior default is the latter.
    float PhysSkillReflectChance = 0f, float PhysSkillReflectPct = 0f,
    // BL-08: chance that a DEBUFF cast at you lands on its caster instead — *"u cast on tank he
    // reflects u get the debuff"*. Rolled before the land/fizzle contest; a reflected debuff is
    // not re-rolled and cannot bounce a second time.
    float DebuffReflectChance = 0f,
    // ---- 4th TIER (2026-08-26) -------------------------------------------------------------------
    // CONTROL RESISTANCE, PER SCHOOL, as a PASSIVE. The buff side of this already existed on SkillDef
    // (the healer's Clarity/Fortitude); his `shared 4th.csv` needs the same two numbers on an always-on
    // passive — *"Increase resistance to SPT debuffs by 20%"* (Strong Body) and *"…CON debuffs by 10%"*
    // (Strong Mind), plus the Holy Protection / Fortitude Sigils. Summed with the buff sources and
    // clamped once, in RecomputeDerived, so a passive and a buff cannot together make you CC-immune.
    float CcResistMagical = 0f,
    float CcResistPhysical = 0f,
    // MP-COST REDUCTION as a passive (fractions; positive = cheaper). His Magic Proficiency's *"decrease
    // MP consumation with 5%"* and the 78+ rungs of Healer Armor Mastery. Same two channels the buff
    // side uses (SkillDef.PhysMpCostPct / MagicMpCostPct) and the same clamp, so a mastery, a Mana
    // Blessing and a Mana Strain all meet in one number.
    float PhysMpCostPct = 0f,
    float MagicMpCostPct = 0f,
    // "M.Evasion" — flat points added to the chance a spell aimed at the HOLDER fails. The buff channel
    // is SkillEffect.BuffMagicEvasion; this is its passive twin, for the Agility Sigil's
    // *"Increase P/M.Evasion +3"* (the P half is `Evasion` above).
    float MagicEvasion = 0f,
    // "M.Accuracy" — the exact MIRROR of the line above: flat points taken OFF the fail chance of the
    // holder's OWN spells. His Holy Mark authors +4 and his Blood Mark +3. See StatCalculator.MagicFailChance.
    float MagicAccuracy = 0f,
    // PvP DAMAGE TAKEN, as a delta: −0.05 = the Duel Sigil's *"5% PvP Defence"*. Multiplied into
    // Entity.PvpDamageTaken exactly as the armour-set channel (StatMods.PvpDamageTakenPct) is, so the
    // two compose. Inert outside player-vs-player.
    float PvpDamageTakenPct = 0f)
{
    /// <summary>Hash on a few representative fields instead of all ~60. Same IL2CPP bracket-nesting
    /// reason as <see cref="SkillDef.GetHashCode"/>: this record is the SECOND largest in the
    /// assembly and grows every time a passive gains a knob, so it is the next one that would have
    /// broken the Android build. A PassiveEffect is a value bundle with no identity and is never a
    /// dictionary key, so extra hash collisions cost nothing; Equals still compares every field.</summary>
    public override int GetHashCode() =>
        (MaxHpPct, MaxMpPct, Con, Agi, Atk, Wit, Spt).GetHashCode();
}

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
        list.AddRange(BuffLadderSkills(list)); // Skills.BuffLadders.cs (every family but speed + its potions/scrolls)
        list.AddRange(StatSwapSkillDefs());   // Skills.StatSwap.cs (the level-40 +stat/−stat passives)
        list.AddRange(FighterSkills());       // Skills.Fighter.cs
        list.AddRange(MageSkills());          // Skills.Mage.cs
        list.AddRange(HealerSkills());        // Skills.Healer.cs (2nd-class Healer kit)
        list.AddRange(ArmorMasterySkills());  // Skills.Masteries.cs (data-driven per-archetype)
        list.AddRange(WeaponMasterySkills()); // Skills.WeaponMasteries.cs (weapon-type-conditional)
        list.AddRange(BufferSkills());        // Skills.Buffer.cs (NPC newbie-buffer buffs)
        list.AddRange(LightbringerSkills());  // Skills.Lightbringer.cs
        list.AddRange(WarchanterSkills());    // Skills.Warchanter.cs
        list.AddRange(Warchanter3rdSkills()); // Skills.Warchanter3rd.cs (his 40-74 groups + harmonies)
        list.AddRange(Nuker3rdSkills());      // Skills.Nuker3rd.cs (his `nuker 3rd.csv`, 40-74)
        list.AddRange(WarchanterKitSkills()); // Skills.Warchanter3rd.Kit.cs (his 40-74 passives/actives/toggles)
        list.AddRange(MobSpellSkills());      // Skills.MobSpells.cs (caster-mob nuke + jab)
        list.AddRange(RewardRuneSkills());    // Skills.RewardRunes.cs (exp/sp/gold/drop runes + Sinister/Sinners)
        list.AddRange(Lightbringer4thSkills()); // Skills.Lightbringer4th.cs (his `healer 4th.csv` 76-90 kit)
        list.AddRange(Shared4thSkills());     // Skills.Shared4th.cs (his `shared 4th.csv` ALL-CLASSES passives)
        list.AddRange(SigilSkills());         // Skills.Sigils.cs (the 4th class's 18 Attack/Defence/Support sigils)

        var dict = new Dictionary<string, SkillDef>();
        foreach (var sk in list)
            if (!dict.TryAdd(sk.Id, sk))
                throw new InvalidOperationException($"Duplicate skill id '{sk.Id}'.");

        // CHILD-ID guard, the sibling of the duplicate-id guard above. A group buff names its
        // children by STRING; a typo there does not fail to compile, it just silently applies
        // nothing — a buff that casts, costs MP and does absolutely no work. Fail at startup.
        foreach (var sk in dict.Values)
        {
            foreach (var child in sk.ChildBuffs ?? Array.Empty<string>())
                if (!dict.ContainsKey(child))
                    throw new InvalidOperationException($"Skill '{sk.Id}' names unknown child buff '{child}'.");
            foreach (var lvl in sk.Levels ?? Array.Empty<SkillLevel>())
                foreach (var child in lvl.ChildBuffs ?? Array.Empty<string>())
                    if (!dict.ContainsKey(child))
                        throw new InvalidOperationException($"Skill '{sk.Id}' names unknown child buff '{child}'.");
        }

        // 🔑 BL-85 GUARD. A childless multi-level buff now carries its LEVEL in its rank
        // (GameLoopService.BuffPlan), which is only safe while its family key is its own: if a
        // SECOND def shares that key, the rung ladder silently starts competing with that other
        // skill too, and a Lv3 of one can lock out a Lv1 of the other. That is exactly the trap
        // Great Might / Great Bulwark were written to avoid — they share `great_blessing` on
        // purpose and opt out with FlatRank. Any NEW sharer must make the same decision
        // deliberately, so fail at startup rather than discovering it in a playtest.
        var laddered = new Dictionary<string, string>();   // buffKey -> the def that ladders on it
        foreach (var sk in dict.Values)
        {
            if (sk.FlatRank || sk.Levels is not { Length: > 1 }) continue;
            if (sk.ChildBuffs is { Length: > 0 }) continue;
            // PASSIVES never reach ApplyBuff — they are folded into RecomputeDerived — so two of
            // them on one name (the tank's Anti-Magic and the mage's, which is how this guard first
            // fired) is not the collision this is about.
            if (sk.Category == SkillCategory.Passive) continue;
            string key = string.IsNullOrEmpty(sk.BuffKey) ? sk.Name : sk.BuffKey;
            if (laddered.TryGetValue(key, out var other))
                throw new InvalidOperationException(
                    $"Buff key '{key}' is laddered by BOTH '{other}' and '{sk.Id}'. A childless "
                  + "multi-level buff carries its level in its rank (BL-85), so two of them on one "
                  + "key make each other's rungs compete. Give one its own BuffKey, or set "
                  + "FlatRank: true on both if they are meant to be mutually exclusive.");
            laddered[key] = sk.Id;
        }
        return dict;
    }

    public static SkillDef? Get(string id) => id is null ? null : All.GetValueOrDefault(id);
    public static string DescriptionOf(string id) => Get(id)?.Description ?? "";
    public static IEnumerable<SkillDef> AllSkills => All.Values;
}

/// <summary>Combat math + range/cast helpers.</summary>
public static class SkillMath
{
    // ===== THE MP SPLIT ==============================================================================
    //  A skill has ONE price. The player is quoted that one number and the cast gate demands all of it
    //  up front; the two-stage payment below is an ENGINE detail, not an authoring one.
    //
    //  🔑 Owner, 2026-08-20: *"i can sum the IG values and we just split it in the code as 20/80 ..
    //  (looking at IG numbers they seem like a 20/80 split)"*. The CSVs' `INIT MP` + `FINIT MP` columns
    //  were collapsed into one `MP` the same day — his sheets never agreed on a ratio (20/80 on a heal,
    //  100/0 on a physical strike) because the ratio was never a design decision, only bookkeeping.
    //
    //  What the split still BUYS: an interrupted cast keeps the 20% (the cost of trying) and refunds
    //  nothing, while the 80% is only charged on a cast that actually lands.
    // =================================================================================================

    /// <summary>Fraction of a skill's MP charged when the cast STARTS; the remainder is charged when it
    /// COMPLETES. Uniform for every skill — see the note above before making it per-skill again.</summary>
    public const float InitialMpFraction = 0.20f;

    /// <summary>The up-front slice of a cast's total MP. Pass the EFFECTIVE total (after the caster's
    /// MP-cost buffs/debuffs), not the authored one, so the two halves always sum back to what the
    /// player was charged.</summary>
    public static int InitialMpOf(int totalMp) => (int)(totalMp * InitialMpFraction);

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
    /// <param name="level">The CHARACTER's level — it selects the bow tier, nothing else.</param>
    /// <param name="skillLevel">The learned level of this skill, for the rare ladder whose rungs are
    /// reach (the rogue's Lure). Defaults to 1, which is what every single-level skill has.</param>
    public static float EffectiveRange(SkillDef def, Archetype? archetype, float basicAttackRange,
        int level, int skillLevel = 1)
    {
        float authored = def.RangeAt(skillLevel < 1 ? 1 : skillLevel);
        if (authored <= 0)
            return basicAttackRange;

        // Bow skills: ranged physical attacks still scale by bow tier. ROGUE counts since the archer
        // merge — the rogue line owns the bow now, from level 20 all the way through Sharpshooter /
        // Hunter / Trapper. Archer is kept for any character still carrying the retired archetype.
        // The `Range >= 300` test is what separates a bow skill from a dagger one, so a rogue's melee
        // skills are unaffected by being in the same class.
        bool isBowSkill = def.Effect.HasFlag(SkillEffect.PhysicalDamage)
            && archetype is Archetype.Archer or Archetype.Rogue && authored >= 300;
        if (isBowSkill)
            return RangeTier(level) switch { 3 => 900f, 2 => 600f, _ => 350f };

        return authored;   // the spell's authored range
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
    /// That target came from IG itself: there `heal = power + √mAtk`, so a 1000-power heal lands
    /// ~1025-1080 almost regardless of M.Atk (it contributes 3-8%) — and IG's skill ENCHANT roughly
    /// doubles it, giving ~2100. We aim at the enchanted value because we have no enchant system.
    /// (K = 8 gave 3750 at L76 — the "paladin heals 3k" number the owner explicitly rejected.)</summary>
    public const float HealK = 15f;

    /// <summary>The FLAT part of a heal: skill power scaled by the healer's M.Atk (√ = diminishing
    /// returns, as in every other magic formula). This is a healer's bread-and-butter heal — cheap,
    /// fast, and it SUFFERS the moment you trade your caster weapon for a damage one, because the
    /// weapon channel factors suppress M.Atk. "Want to be a fighter? Then you heal less."
    ///
    /// NOTE this deliberately DIVERGES from IG, which is ADDITIVE — `power + √mAtk` — where M.Atk is
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
    // (PhysicalSkillAccuracyBonus DELETED 2026-08-14, BL-06. It was +10 accuracy on a physical
    //  skill's miss roll — a softener on a roll that no longer happens at all: a physical skill is
    //  not evaded by accuracy-vs-evasion any more, only by an explicit SkillEvadeChance grant.
    //  Don't re-add it; it would have nothing to modify.)
}
