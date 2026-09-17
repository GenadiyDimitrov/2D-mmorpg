using System;
using System.Linq;

namespace Game.Shared;

/// <summary>THE WARLORD'S OWN KIT, 40-74 — every `waraoe_*` row of
/// <c>docs/data/classes_skills_csv/war_aoe 3rd.csv</c> (`BL-237` §5, authored 2026-09-16, built
/// 2026-09-17). Until today **not one of these ten ids existed in the codebase**: the blunt discipline
/// had his passives, his buffs and Charge, and for its damage it borrowed the derived
/// <c>war_sundering_blow</c> stand-in. That stand-in is deleted in the same commit — his rows are what
/// it was waiting for.
///
/// <para>🔑 <b>THE WARLORD IS THE AoE DISCIPLINE, AND EVERY ACTIVE HERE SAYS SO.</b> Where the Ravager
/// has five single-target strikes, this file has one self-centred shout that stuns a ring, one that is
/// four seconds of spinning blade, one race debuff that lands on a ring instead of on a man, and a
/// long taunt. Range 0 on almost all of them: the Warlord walks into the middle and detonates.</para>
///
/// <para>🔑 <b>HIS `enemy/aoe` EDIT IS WHY THEY READ THAT WAY</b> (2026-09-17). Every one of these
/// rows said `self/aoe` the day before and says `enemy/aoe` now — the ring is centred on the CASTER
/// (Range 0, no <see cref="SkillDef.AreaAtTarget"/>) and what it catches is ENEMIES. Only Shocking
/// Javelin, in the 4th file, throws the ring 900 away.</para>
///
/// <para>🔑 <b>THE THREE SHOUTS ARE THE THREE SLASHES WITH THE DAMAGE TAKEN OUT.</b> Same three rots,
/// race for race — Human cuts P.Def, Demon cuts P/M.Atk, Elf cuts all three speeds — same fifteen
/// rungs, same MP and SP columns, same <c>Replaces: [smash]</c>. What differs is that a Slash is a
/// strike that also curses (×0.7 to land) and a Shout is a **solo debuff on a ring**, which he priced
/// himself in the one cell he wrote beside them: *"Single debuff x1.5"*.</para>
///
/// <para>⚠ <b>THE SUPPORTS PROC ON YOUR OWN HIT, and that is a READING, not his words.</b> His DESCR
/// says only *"10% chance to heal for 5% max HP"* — it never names the trigger. On-hit is chosen
/// because the Warlord's whole design is swinging a blunt into a crowd (so every swing is a roll) and
/// because the Demon's and Human's riders are VAMPIRISM, which is offensive by nature. One word from
/// him flips it to <see cref="SkillDef.ProcOnDamaged"/>.</para>
/// </summary>
public static partial class SkillCatalog
{
    public const string WaraoeBattleRevival = "waraoe_battle_revival";
    public const string WaraoeLifeSupport   = "waraoe_life_support";    // Elf
    public const string WaraoeBloodSupport  = "waraoe_blood_support";   // Demon
    public const string WaraoeSupport       = "waraoe_support";         // Human ("Vanguard support")
    public const string WaraoeShockShout    = "waraoe_shock_shout";
    public const string WaraoeWhirlwind     = "waraoe_wirlwind";        // his spelling — ids are append-only
    public const string WaraoeTauntingShout = "waraoe_taunting_shout";
    public const string WaraoeHumanShout    = "waraoe_human_shout";
    public const string WaraoeDemonShout    = "waraoe_demon_shout";
    public const string WaraoeElfShout      = "waraoe_elf_shout";
    /// <summary>ONE STROKE of Whirlwind — the sub-skill the wrapper fires twenty times. Never learned,
    /// never on a bar, no MP of its own. Same shape as the Elf's Sword Dance stroke; see
    /// <see cref="SkillDef.ChannelSkill"/>.</summary>
    public const string WaraoeWhirlwindStroke = "waraoe_wirlwind_stroke";

    /// <summary>The proc rungs the three Supports hand to <see cref="SkillDef.ProcSelfRungs"/>. Never
    /// learned; one per rung of the passive that owns them.</summary>
    private static string SupportPayload(string owner, int rung) => $"{owner}_payload_{rung}";

    // ---- HIS THREE SUPPORT RUNGS, read straight off the rows: 60 / 66 / 74. ----
    internal static readonly int[] WaraoeSupportLevels = { 60, 66, 74 };
    private static readonly int[]   WaraoeSupportSp    = { 120_000, 280_000, 880_000 };
    private static readonly float[] WaraoeSupportChance = { .10f, .15f, .20f };
    private static readonly float[] WaraoeSupportHeal   = { .05f, .07f, .10f };
    /// <summary>The Elf's lingering heal — a FLAT HP/second, not a fraction, which is why it is
    /// <c>ModifierMode.Flat</c> on <see cref="SkillEffect.HealOverTime"/> (the tick reads both).</summary>
    private static readonly int[] WaraoeElfHot   = { 50, 100, 150 };
    private static readonly float[] WaraoeDemonVamp = { .03f, .06f, .09f };
    /// <summary>The Human takes a THIRD of each: 1/2/3% vampirism against the Demon's 3/6/9, and
    /// 17/33/50 HP/s against the Elf's 50/100/150. His numbers, and the arithmetic is exact — a
    /// Vanguard gets both halves at a third of the strength of either specialist.</summary>
    private static readonly float[] WaraoeHumanVamp = { .01f, .02f, .03f };
    private static readonly int[]   WaraoeHumanHot  = { 17, 33, 50 };

    // ═══ THE FIFTEEN-RUNG POWER COLUMNS ══════════════════════════════════════════════════════════
    internal static readonly int[] WaraoeShockShoutPower =
        { 1000, 1200, 1400, 1600, 1800, 2000, 2400, 2600, 2800, 3000, 3200, 3400, 3600, 3800, 4000 };

    /// <summary>ONE STROKE of Whirlwind, twenty of which land over four seconds.
    /// <para>🔴 <b>THIS COLUMN DIPS AT RUNG 13 AND IT IS AUTHORED THAT WAY</b> — 1540 at level 68, then
    /// **900** at 70. Built verbatim because the CSV is the authority, and raised with him as `BL-261`,
    /// because three separate checks say the first twelve cells are an older column left behind:</para>
    /// <list type="bullet">
    /// <item>rungs 8-12 (1125 … 1540) are the Elf Sword Dance's cells <b>exactly</b>, and rungs 1-7 are
    /// that same ladder minus 75;</item>
    /// <item>`war_aoe 4th.csv` opens at <b>1050</b> and climbs +50 a rung — which continues 900 / 950 /
    /// 1000 perfectly and is far BELOW 1540;</item>
    /// <item>+50 a rung backwards from 1000 over fifteen rungs lands on <b>300</b>, which is what rung 1
    /// already says. Both ends of his ladder agree; only the middle disagrees.</item>
    /// </list>
    /// <para>⚠ His own rule is that a ladder which RISES is his and a ladder that DIPS is a typo
    /// (`BL-237`, Armor Mastery). This one dips, so it is asked rather than assumed — and it is asked
    /// rather than silently corrected, because which twelve cells move is his call, not mine.</para></summary>
    internal static readonly int[] WaraoeWhirlwindPower =
        { 300, 415, 525, 640, 715, 825, 940, 1125, 1240, 1350, 1465, 1540, 900, 950, 1000 };

    // ---- THE THREE ROTS. Same three channels as the Ravager's Slashes, same plateau at rung 6. ----
    private static readonly float[] WaraoeHumanShoutDef =
        { .10f, .13f, .16f, .19f, .21f, .23f, .23f, .23f, .23f, .23f, .23f, .23f, .23f, .23f, .23f };
    private static readonly float[] WaraoeDemonShoutAtk =
        { .05f, .05f, .05f, .05f, .05f, .10f, .10f, .10f, .10f, .10f, .10f, .10f, .10f, .10f, .10f };
    private static readonly float[] WaraoeElfShoutSpeed =
        { .10f, .12f, .14f, .16f, .18f, .20f, .20f, .20f, .20f, .20f, .20f, .20f, .20f, .20f, .20f };

    /// <summary>HIS LANDING MODIFIER FOR ALL THREE SHOUTS, and it is the one number he wrote himself —
    /// the cell *"Single debuff x1.5"* sits beside the first rung of each. It is the `BL-232` rule
    /// working exactly as designed: a shout does NOTHING but curse, so it lands more often than a Slash
    /// (×0.7), which curses AND strikes. ⚠ `debuff_landmods.csv` is still the authority; this constant
    /// only has to agree with it, and `SkillCsvSeed --check` is what proves that it does.</summary>
    private const float WaraoeShoutLandMod = 1.5f;

    private static SkillDef[] Warlord3rdSkills()
    {
        var list = new System.Collections.Generic.List<SkillDef>();

        // ═══ BATTLE REVIVAL — *"Instantly heals to full HP"*, five minutes of reuse ══════════════
        // 🔑 IT IS BATTLE REGENERATION'S LADDER TAKEN TO 100%, not a new mechanic: the same
        //    `Heal` + Percent magnitude the fighter's self-heal has always used, at 1.0. His row is
        //    MP 0, cast 0 and CD 300 — a panic button, not a rotation.
        // ⚠ It does NOT `Replaces` Battle Regeneration. His file authors both, and the Warlord's
        //   Battle Regeneration runs to rung 6 (35%) at level 70 — the same level this arrives. Two
        //   heals on two reuses is what he wrote.
        list.Add(new SkillDef(WaraoeBattleRevival, "Battle Revival", BaseClass.Fighter, SkillEffect.Heal,
            MpCost: 0, CastTicks: 0, CooldownTicks: 3000, Range: 0, Power: 0,
            Category: SkillCategory.Heal, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
            SpCost: 390_000, FixedCast: true,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Heal, 1.00f, ModifierMode.Percent) },
            Description: "Back from the brink: your HP is restored in full. Five minutes of reuse."));

        // ═══ THE THREE SUPPORTS — one per race, a proc on your own blow ══════════════════════════
        list.AddRange(SupportKit(WaraoeLifeSupport, "Life Support",
            i => new EffectMagnitude[] { new(SkillEffect.HealOverTime, WaraoeElfHot[i], ModifierMode.Flat) },
            i => $"and {WaraoeElfHot[i]} HP a second for 10s",
            new EffectMagnitude[] { new(SkillEffect.HealOverTime, 300f, ModifierMode.Flat) },
            "and 300 HP a second for 10s"));
        list.AddRange(SupportKit(WaraoeBloodSupport, "Blood Support",
            i => new EffectMagnitude[] { new(SkillEffect.BuffMeleeVamp, WaraoeDemonVamp[i]) },
            i => $"and {WaraoeDemonVamp[i] * 100f:0}% melee vampirism for 10s",
            new EffectMagnitude[] { new(SkillEffect.BuffMeleeVamp, .15f) },
            "and 15% melee vampirism for 10s"));
        list.AddRange(SupportKit(WaraoeSupport, "Vanguard Support",
            i => new EffectMagnitude[]
            {
                new(SkillEffect.BuffMeleeVamp, WaraoeHumanVamp[i]),
                new(SkillEffect.HealOverTime, WaraoeHumanHot[i], ModifierMode.Flat),
            },
            i => $"and {WaraoeHumanVamp[i] * 100f:0}% melee vampirism plus {WaraoeHumanHot[i]} HP a second for 10s",
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffMeleeVamp, .05f),
                new(SkillEffect.HealOverTime, 100f, ModifierMode.Flat),
            },
            "and 5% melee vampirism plus 100 HP a second for 10s"));

        // ═══ SHOCKING SHOUT — the ring that stuns ════════════════════════════════════════════════
        // *"Shouts to do Physical damage with +N power and Stuns around for 5s, Cannot be blocked,
        //  Can double"*. Range 0 + AreaRadius 200 and NO `AreaAtTarget` = centred on the caster.
        // ⚠ ITS STUN IS CONTESTED (`DebuffSchool.Physical`, ATK vs CON) and its landing modifier is
        //   OWED BY HIM — see `BL-259`. `debuff_landmods.csv` carries the row with the code default.
        list.Add(WarlordShout(WaraoeShockShout, "Shocking Shout", SkillEffect.Stun,
            WaraoeShockShoutPower, castTicks: 10, cooldownTicks: 50, durationTicks: 50,
            "A bellow that flattens everything around you.",
            _ => "and stuns everything around you for 5s", landMod: 1f,
            fourth: WarlordShockShoutRungs()));

        // ═══ WHIRLWIND — four seconds of blade ═══════════════════════════════════════════════════
        // *"Deals Physical damage with +N power 20 times over 4s"*. A CHANNEL WRAPPER, the shape the
        // Elf's Sword Dance and Arrow Barrage already use: each stroke is a REAL execution with its own
        // miss, crit and splash, so nothing about twenty hits is special-cased.
        // 🔑 20 strokes × 2 ticks = 40 ticks = his 4 seconds, exactly.
        // 🔑 THE LADDER IS ON THE WRAPPER and the stroke authors Power 0 — a channel's shots resolve at
        //    the wrapper's level and take its power (Entity.ChannelPower).
        list.Add(new SkillDef(WaraoeWhirlwind, "Whirlwind", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: W3ActiveMp[0], CastTicks: 5, CooldownTicks: 80, Range: 0,
            Power: WaraoeWhirlwindPower[0], DurationTicks: 40,
            Category: SkillCategory.Physical, CanDouble: true, BlockAccuracy: 1f,
            // The wrapper carries the radius so the ring is DRAWN at cast time and `Retarget.FromDef`
            // knows this is an area skill — it resolves nothing itself. Same note as the Sword Dance.
            AreaRadius: 200f,
            RequiredWeapon: WeaponType.AnyBlunt, RequiredHands: WeaponHands.Two,
            ChannelSkill: WaraoeWhirlwindStroke, ChannelShots: 20, ChannelIntervalTicks: 2,
            SpCost: Warrior3rdSp[0],
            Description: "Four seconds of spinning blunt: twenty strokes, each catching everything "
                       + "around you. Requires a two-handed blunt.",
            Levels: Enumerable.Range(0, Warrior3rdLevels.Length).Select(i => new SkillLevel(
                Power: WaraoeWhirlwindPower[i], MpCost: W3ActiveMp[i], SpCost: Warrior3rdSp[i],
                Description: WhirlwindRungText(WaraoeWhirlwindPower[i])))
                .Concat(WarlordWhirlwindRungs()).ToArray()));

        // ONE STROKE. No MP (the wrapper charges once), no power (the wrapper's rung supplies it),
        // never learned. The AoE lives HERE because it is the stroke that splashes, not the wrapper.
        list.Add(new SkillDef(WaraoeWhirlwindStroke, "Whirlwind", BaseClass.Fighter,
            SkillEffect.PhysicalDamage,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Physical, CanDouble: true, BlockAccuracy: 1f,
            AreaRadius: 200f, SpCost: 0,
            Description: "One stroke of a Warlord's whirlwind."));

        // ═══ TAUNTING SHOUT — the long provoke, and the first VULNERABILITY in the game ══════════
        // *"provoke enemies in a large area and make them vunarable to bludgering attacks (10% more dmg
        //  from blunts)"*, 600 radius at 52 and 800 at 74, thirty seconds, twenty of reuse.
        // 🔑 THE VULNERABILITY IS A NEW CHANNEL — see SkillDef.VulnerableToWeapon. It is the one damage
        //    channel that cannot be a derived stat, because it asks what the ATTACKER is holding.
        // 🔑 IT PAYS THE WHOLE PARTY, not just the Warlord: anything holding a blunt hits the marked
        //    ring harder, which is what makes this the blunt discipline's group tool rather than a
        //    personal one.
        // ⚠ Its landing modifier is OWED BY HIM (`BL-259`) — it is `Physical/Debuf` in his TYPE cell.
        list.Add(new SkillDef(WaraoeTauntingShout, "Taunting Shout", BaseClass.Fighter, SkillEffect.None,
            MpCost: 50, CastTicks: 10, CooldownTicks: 200, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: WaraoeTauntingShout, Rank: 1,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Physical,
            AreaRadius: 600f, TauntPower: 3000,
            VulnerableToWeapon: WeaponType.AnyBlunt, WeaponVulnerabilityPct: .10f,
            RequiredWeapon: WeaponType.AnyBlunt, RequiredHands: WeaponHands.Two,
            SpCost: 74_000,
            Description: "A roar that pulls a whole field onto you — and leaves everything in it "
                       + "softer to bludgeoning. Requires a two-handed blunt.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 50, SpCost: 74_000, Range: 0f, AreaRadius: 600f,
                    WeaponVulnerabilityPct: .10f,
                    Description: "Provokes everything within 600 for 30s and leaves it taking 10% more "
                               + "damage from blunt weapons."),
                // 74 — his second row: the ring grows to 800, the rot doubles, and the MP goes 50 → 60.
                // ⚠ His SP cell repeats 74 at this rung. Written through as authored; it is the only
                //   ladder in either warrior file whose SP does not climb, and it is one cell.
                new SkillLevel(MpCost: 60, SpCost: 74_000, Range: 0f, AreaRadius: 800f,
                    WeaponVulnerabilityPct: .20f,
                    Description: "Provokes everything within 800 for 30s and leaves it taking 20% more "
                               + "damage from blunt weapons."),
            }.Concat(WarlordTauntingShoutRungs()).ToArray()));

        // ═══ THE THREE RACE SHOUTS — the Slashes' rots, on a ring, with no strike ════════════════
        list.Add(RaceShout(WaraoeHumanShout, "Shattering Shout", SkillEffect.DebuffDef,
            WaraoeHumanShoutDef, W4WaraoeHumanShoutDef, v => new EffectMagnitude[] { new(SkillEffect.DebuffDef, v) },
            "A shout that opens armour: every guard in the ring fails for 15s.",
            v => $"Cuts the P.Def of everything within 200 by {v * 100f:0}% for 15s."));
        list.Add(RaceShout(WaraoeDemonShout, "Breaking Shout",
            // ⚠ ONE FLAG FOR BOTH HALVES. `DebuffAtk` cuts P.Atk AND M.Atk — the Demon's Slash has
            //   always been authored this way and its own rung text says so. There is no
            //   `DebuffMagicAtk`, and the enum has no bit left to add one.
            SkillEffect.DebuffAtk, WaraoeDemonShoutAtk, W4WaraoeDemonShoutAtk,
            v => new EffectMagnitude[] { new(SkillEffect.DebuffAtk, v) },
            "A shout that breaks the swing: everything in the ring hits softer for 15s.",
            v => $"Cuts the P.Atk and M.Atk of everything within 200 by {v * 100f:0}% for 15s."));
        list.Add(RaceShout(WaraoeElfShout, "Crippling Shout",
            SkillEffect.Slow | SkillEffect.DebuffAtkSpeed | SkillEffect.DebuffCastSpeed,
            WaraoeElfShoutSpeed, W4WaraoeElfShoutSpeed,
            v => new EffectMagnitude[]
            {
                new(SkillEffect.Slow, v),
                new(SkillEffect.DebuffAtkSpeed, v),
                new(SkillEffect.DebuffCastSpeed, v),
            },
            "A shout that takes the legs: everything in the ring moves, swings and casts slower.",
            v => $"Cuts the attack, cast and move speed of everything within 200 by {v * 100f:0}% for 15s."));

        return list.ToArray();
    }

    private static string WhirlwindRungText(int power) =>
        $"Twenty strokes of power {power:N0} over 4s, each catching everything within 200. "
      + "Cannot be blocked, can double.";

    /// <summary>ONE OF THE WARLORD'S THREE SUPPORTS: the passive that rolls, plus the three payload
    /// defs it hands out. Four defs for one shelf entry — the payloads are never learned and never
    /// registered, they exist only for <see cref="SkillDef.ProcSelfRungs"/> to name.
    ///
    /// <para>🔑 HIS CD AND DURATION COLUMNS DESCRIBE THE PROC, NOT THE SKILL — CD 15 is the internal
    /// cooldown and DURR 10 is how long the lingering half lasts. That is the reading `SkillCsvSeed`
    /// already applies to every proc passive in the game, so his rows verify without a special
    /// case.</para>
    ///
    /// <para>🔑 EACH PAYLOAD DOES TWO THINGS AT ONCE — an instant heal for a share of max HP AND a ten
    /// second lingering buff — which is his sentence, verbatim: *"chance to heal for 5% max HP, **and**
    /// leave lingering …"*. `PayOutProc` used to return the moment it paid an instant heal, so the
    /// second half would have been silently dropped; it now falls through whenever the payload has a
    /// real duration.</para></summary>
    private static SkillDef[] SupportKit(string id, string name,
                                         Func<int, EffectMagnitude[]> lingering,
                                         Func<int, string> lingeringText,
                                         EffectMagnitude[] rung4Lingering, string rung4Text)
    {
        // +2: the three 3rd-tier payloads, the level-80 payload, and the passive itself.
        var defs = new SkillDef[WaraoeSupportLevels.Length + 2];

        for (int r = 0; r < WaraoeSupportLevels.Length; r++)
        {
            int i = r;
            defs[r + 1] = new SkillDef(SupportPayload(id, i + 1), name, BaseClass.Fighter,
                SkillEffect.Heal | lingering(i).Aggregate(SkillEffect.None, (a, m) => a | m.Effect),
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                DurationTicks: 100,               // his DURR cell: ten seconds of lingering
                BuffKey: id, Rank: i + 1,
                Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly, SpCost: 0,
                Magnitudes: new[] { new EffectMagnitude(SkillEffect.Heal, WaraoeSupportHeal[i], ModifierMode.Percent) }
                            .Concat(lingering(i)).ToArray(),
                Description: $"Heals {WaraoeSupportHeal[i] * 100f:0}% of max HP {lingeringText(i)}.");
        }

        // ---- RUNG 4, level 80 (`war_aoe 4th.csv`): 20% for 15% of max HP, and a far heavier tail. ----
        int last = WaraoeSupportLevels.Length + 1;
        defs[last] = new SkillDef(SupportPayload(id, WaraoeSupportLevels.Length + 1), name,
            BaseClass.Fighter,
            SkillEffect.Heal | rung4Lingering.Aggregate(SkillEffect.None, (a, m) => a | m.Effect),
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 100, BuffKey: id, Rank: WaraoeSupportLevels.Length + 1,
            Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly, SpCost: 0,
            Magnitudes: new[] { new EffectMagnitude(SkillEffect.Heal, .15f, ModifierMode.Percent) }
                        .Concat(rung4Lingering).ToArray(),
            Description: $"Heals 15% of max HP {rung4Text}.");

        defs[0] = new SkillDef(id, name, BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, TargetMode: TargetMode.SelfOnly,
            SpCost: WaraoeSupportSp[0],
            ProcChance: WaraoeSupportChance[0], ProcCooldownTicks: 150,
            ProcSelfRungs: Enumerable.Range(1, WaraoeSupportLevels.Length + 1)
                                     .Select(r => SupportPayload(id, r)).ToArray(),
            Description: "Every blow you land may pay you back.",
            Levels: Enumerable.Range(0, WaraoeSupportLevels.Length).Select(i => new SkillLevel(
                SpCost: WaraoeSupportSp[i], ProcChance: WaraoeSupportChance[i],
                Description: $"{WaraoeSupportChance[i] * 100f:0}% chance on a landed blow (15s reuse) to "
                           + $"heal {WaraoeSupportHeal[i] * 100f:0}% of max HP {lingeringText(i)}."))
                .Append(SupportRung4(rung4Lingering, rung4Text))
                .ToArray());

        return defs;
    }

    /// <summary>A WARLORD SHOUT THAT STRIKES — Shocking Shout today. Range 0 + a radius and no
    /// <see cref="SkillDef.AreaAtTarget"/> is the self-centred ring; everything else is the Ravager's
    /// <c>WarriorStrike</c> shape, including *"Cannot be blocked"* as <c>BlockAccuracy: 1</c> and the
    /// contested rider on CON.</summary>
    private static SkillDef WarlordShout(string id, string name, SkillEffect rider, int[] power,
                                         int castTicks, int cooldownTicks, int durationTicks,
                                         string blurb, Func<int, string> what, float landMod,
                                         SkillLevel[]? fourth = null)
    {
        string Rung(int i, int p) =>
            $"Strikes everything within 200 for power {p:N0} {what(i)}. Cannot be blocked, can double.";

        return new SkillDef(id, name, BaseClass.Fighter, SkillEffect.PhysicalDamage | rider,
            MpCost: W3ActiveMp[0], CastTicks: castTicks, CooldownTicks: cooldownTicks,
            Range: 0, Power: power[0],
            DurationTicks: durationTicks,
            BuffKey: rider == SkillEffect.None ? "" : id,
            Rank: rider == SkillEffect.None ? 0 : 1,
            DebuffSchool: rider == SkillEffect.None ? DebuffSchool.None : DebuffSchool.Physical,
            DebuffLandMod: landMod,
            Category: SkillCategory.Physical, CanDouble: true, BlockAccuracy: 1f,
            AreaRadius: 200f,
            RequiredWeapon: WeaponType.AnyBlunt, RequiredHands: WeaponHands.Two,
            SpCost: Warrior3rdSp[0],
            Description: blurb,
            Levels: Enumerable.Range(0, Warrior3rdLevels.Length).Select(i => new SkillLevel(
                Power: power[i], MpCost: W3ActiveMp[i], SpCost: Warrior3rdSp[i],
                Description: Rung(i, power[i])))
                .Concat(fourth ?? Array.Empty<SkillLevel>()).ToArray());
    }

    /// <summary>ONE OF THE THREE RACE SHOUTS — a SOLO debuff on a self-centred ring, no damage cell at
    /// all. Fifteen rungs on the shared MP/SP columns, `Replaces: [smash]` from his cell, and
    /// <see cref="WaraoeShoutLandMod"/> because a shout that only curses lands more readily than a
    /// Slash that curses and strikes.</summary>
    private static SkillDef RaceShout(string id, string name, SkillEffect rot, float[] column,
                                      float[] fourth,
                                      Func<float, EffectMagnitude[]> mags,
                                      string blurb, Func<float, string> rungText) =>
        new(id, name, BaseClass.Fighter, rot,
            MpCost: W3ActiveMp[0], CastTicks: 10, CooldownTicks: 100, Range: 0, Power: 0,
            DurationTicks: 150, BuffKey: id, Rank: 1,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Physical,
            DebuffLandMod: WaraoeShoutLandMod,
            AreaRadius: 200f,
            RequiredWeapon: WeaponType.AnyBlunt, RequiredHands: WeaponHands.Two,
            Replaces: new[] { Smash },
            SpCost: Warrior3rdSp[0],
            Magnitudes: mags(column[0]),
            Description: blurb,
            Levels: Enumerable.Range(0, Warrior3rdLevels.Length).Select(i => new SkillLevel(
                MpCost: W3ActiveMp[i], SpCost: Warrior3rdSp[i],
                Magnitudes: mags(column[i]),
                Description: rungText(column[i])))
                .Concat(WarlordRaceShoutRungs(fourth, mags, rungText)).ToArray());
}
