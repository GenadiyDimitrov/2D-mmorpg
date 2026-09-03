using System.Linq;

namespace Game.Shared;

// ===========================================================================
//  THE BULWARK (tank), 40-74 — his `docs/data/classes_skills_csv/tank 3rd.csv`, plus the 2nd-class
//  retune that arrived in the same pass (`tank 2nd.csv`).
//
//  🔑 RACE IS THE WHOLE 3rd-CLASS IDENTITY HERE, and it is his, from the RACE column:
//
//      HUMAN   Taunt · Mass Taunt · Shield Smash - Rate · whisps: taunt + bind
//      ELF     Charm · Freeze     · Shield Smash - Rate · whisps: charm + heal
//      DEMON   Taunt · Intimidate · Shield Smash - Power · whisps: armor + weapon break
//      ALL     the four masteries, Final Defense, Aggravated State, Stay, Shield Shock,
//              Defensive Wall, Shield Reinforcement, Whisp Mastery
//
//  So a Human holds a pack (mass taunt), an Elf controls one thing at a time (charm + a 50% slow),
//  and a Demon breaks what he is fighting (fear + the crit-damage smash). None of it is a stat lean —
//  it is the kit, which is his standing rule for what a class IS.
//
//  ⚠ CHARM REPLACES TAUNT for the Elf (his REPLACES column, `[provoke]`), which is why the Elf has no
//  Taunt row at any tier: he was never meant to hold two of the same tool.
//
//  ⚠ THE SP COLUMN OF `tank 3rd.csv` IS IN THOUSANDS and its header says so — `SP COST (x1000)`. He
//  writes 28 for 28,000 because *"it was annoying for me to write [the k] each time"*. The checker
//  reads that header; nothing here multiplies by hand.
// ===========================================================================

public partial class SkillCatalog
{
    // ---- New ids. The rest of the tank's kit already existed and is EXTENDED, not replaced:
    //      provoke, tank_stay, tank_shield_stun, tank_anti_magic, tank_armor_mastery,
    //      tank_shield_mastery, tank_weapon_mastery, defensive_wall.
    public const string TankCharm              = "charm";
    public const string TankMassProvoke        = "mass_provoke";
    public const string TankFear               = "tank_fear";
    public const string TankFreeze             = "tank_freeze";
    public const string TankSmashRate          = "tank_smash_rate";
    public const string TankSmashPower         = "tank_smash_power";
    public const string TankFinalDefense       = "tank_final_defense";
    public const string TankAggravatedState    = "tank_aggravated_state";
    public const string TankShieldReinforce    = "tank_shield_reinforcement";
    // The two proc payloads of Aggravated State. Never learned, never on a bar: a proc's rung is an
    // ordinary SkillDef that only the proc machinery ever applies (the Sigils work the same way).
    public const string TankAggravatedSelf     = "tank_aggravated_self";
    public const string TankAggravatedParty    = "tank_aggravated_party";

    // ---- HIS LADDERS. Fifteen rungs on almost everything, at these levels. ----
    private static readonly int[] BulwarkLevels =
        { 40, 43, 46, 49, 52, 55, 58, 60, 62, 64, 66, 68, 70, 72, 74 };

    /// <summary>SP per rung, in his own units (the file's header says `x1000`). One ladder for the
    /// whole file — every fifteen-rung skill in it is priced identically.</summary>
    private static readonly int[] BulwarkSp =
    {
        28_000, 35_000, 40_000, 50_000, 74_000, 81_000, 88_000, 120_000,
        170_000, 190_000, 280_000, 320_000, 390_000, 650_000, 880_000,
    };

    /// <summary>The MP ladder his control skills share (Intimidate, Freeze, Stay, Mass Taunt).</summary>
    private static readonly int[] BulwarkControlMp =
        { 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100, 105, 110 };

    /// <summary>The MP ladder both Shield Smashes and Shield Shock share.</summary>
    private static readonly int[] BulwarkSmashMp =
        { 62, 76, 76, 76, 83, 90, 95, 98, 100, 105, 108, 112, 114, 117, 120 };

    /// <summary>His RANGE column: 600 to level 58, then 800. (Stay is 400 → 600, and the smashes are
    /// a flat 40 — they are shield bashes.) Written once because six skills step together.</summary>
    private static float BulwarkRange(int i) => i <= 6 ? 600f : 800f;

    private static SkillLevel[] BulwarkRungs(System.Func<int, SkillLevel> mk) =>
        Enumerable.Range(0, BulwarkLevels.Length).Select(mk).ToArray();

    private static SkillDef[] Bulwark3rdSkills() => new SkillDef[]
    {
        // ═══ CHARM — the Elf's taunt (`BL-110` made the mechanic; this is the class skill) ═══════
        //
        // 🔑 THE PAIRING THAT MAKES IT A TANK TOOL, and his own: *"charm can fail the actual debuff
        // (the un-charm-movement) but still adds the points"*. So the aggro ladder — the SAME 4,500
        // → 12,000 the Human's Taunt runs on — is paid on cast, and only the 3-second walk rolls
        // against the target's resistance at his authored ×0.7. An Elf tank therefore holds aggro
        // exactly as reliably as a Human one, and gets a body dragged toward him when it lands.
        //
        // ⚠ FOUR RUNGS AT THE 2ND CLASS (24-36) AND FIFTEEN AT THE 3RD, one continuous ladder, so the
        // rung numbers here are 5-19. Same shape as Taunt, which it replaces.
        new(TankCharm, "Charm", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 60, Range: 400, Power: 0,
            DurationTicks: 30, BuffKey: "charm", Rank: 1, TauntPower: 4500,   // 3s of walking — his 3rd file's DURR column, and what both files' DESCR cells say
            Charms: true, DebuffLandMod: 0.7f,
            // 🔑 MAGICAL, so the save is SPT — his ruling, 2026-09-03 (`BL-133`): *"charm is a magic
            // taunt not phisical -> charm is saved by SPT, Freeze as well, Stay and Shield Shock are
            // the only physical debuffs atm and are saved by CON"*. It was Physical here while his own
            // `tank 3rd.csv` already read `Magical Debuff` in the TYPE column — the disagreement was
            // invisible because `--check` never compared that column. This is also what makes the elf
            // the MAGIC KNIGHT of the three tanks: his control is resisted by a different stat than
            // the human's and the demon's, and it is the reason he now has a WIT swap (`BL-134`).
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff,
            Replaces: new[] { "taunt_lock" },
            SpCost: 6400,
            Description: "Lures an enemy: it walks helplessly toward you, and remembers who called it.",
            Levels: TankCharmRungs()),

        // ═══ MASS TAUNT — the Human's, and the only AoE aggro tool in the game ═══════════════════
        // 400 radius, 30s reuse, a 3-second lock on everything in it, and a smaller ladder per head
        // (4,000 → 7,200) than the single-target Taunt's, which is the right trade for hitting a pack.
        new(TankMassProvoke, "Mass Taunt", BaseClass.Fighter, SkillEffect.Taunt,
            MpCost: BulwarkControlMp[0], CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 30, AreaRadius: 400f, TauntPower: 4000,
            // `BL-132` — physical, like the single-target Taunt it scales out from; his TYPE cell says so.
            Category: SkillCategory.Debuff, PhysicalCast: true, TargetMode: TargetMode.EnemiesInRadius,
            SpCost: BulwarkSp[0],
            Description: "Roars at everything around you: each of them turns on you for 3s and "
                       + "carries the grudge afterwards.",
            Levels: BulwarkRungs(i =>
            {
                int power = 4000 + i * 200;
                if (i >= 11) power = 6300 + (i - 11) * 300;   // his stride widens at 68: 6300/6600/6900/7200
                return new SkillLevel(MpCost: BulwarkControlMp[i], SpCost: BulwarkSp[i],
                    AreaRadius: 400f, TauntPower: power,
                    Description: $"Taunts everything within 400 for 3s and adds {power:N0} to your aggro on each.");
            })),

        // ═══ INTIMIDATE — the Demon's FEAR (`BL-110`) ════════════════════════════════════════════
        // *"Intimidate the enemy to run in place"* — his own words, and the exact shape `BL-110` gave
        // fear: the victim cannot act and bolts at a run to random points nearby, for ten seconds.
        // The longest control in the tank's kit, and the reason the Demon needs no mass taunt: one
        // thing is simply out of the fight.
        new(TankFear, "Intimidate", BaseClass.Fighter, SkillEffect.Fear,
            MpCost: BulwarkControlMp[0], CastTicks: 0, CooldownTicks: 50, Range: 600, Power: 0,
            DurationTicks: 100, BuffKey: "fear", Rank: 1, SharesLadderKey: true,
            DebuffSchool: DebuffSchool.Physical, Category: SkillCategory.Debuff,
            SpCost: BulwarkSp[0],
            Description: "Terrifies an enemy for 10s: it cannot act, and runs where its feet take it. "
                       + "ATK-vs-CON; bosses immune.",
            // NOTHING BUT THE PRICE AND THE REACH MOVES, and that is his file: a fear is a fear, and
            // what a rung buys is the level contest (DebuffLandChance reads the RUNG's learn level).
            Levels: BulwarkRungs(i => new SkillLevel(
                MpCost: BulwarkControlMp[i], SpCost: BulwarkSp[i], Range: BulwarkRange(i)))),

        // ═══ FREEZE — the Elf's slow, 30 seconds of it ═══════════════════════════════════════════
        // 30% → 50%, and it PLATEAUS at 50 from rung 10. Deliberate, and the same shape the healer's
        // debuff ladders take: past the ceiling a rung buys LANDING CHANCE, not magnitude.
        new(TankFreeze, "Freeze", BaseClass.Fighter, SkillEffect.Slow,
            MpCost: BulwarkControlMp[0], CastTicks: 0, CooldownTicks: 30, Range: 600, Power: 0,
            DurationTicks: 300, BuffKey: "slow", Rank: 1, SharesLadderKey: true,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff,
            SpCost: BulwarkSp[0],
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Slow, 0.30f) },
            Description: "Chills an enemy for 30s, cutting its movement.",
            Levels: BulwarkRungs(i =>
            {
                float[] slow = { .30f, .31f, .33f, .35f, .37f, .40f, .43f, .45f, .47f, .50f, .50f, .50f, .50f, .50f, .50f };
                return new SkillLevel(MpCost: BulwarkControlMp[i], SpCost: BulwarkSp[i],
                    Range: BulwarkRange(i),
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.Slow, slow[i]) },
                    Description: $"Cuts an enemy's movement by {slow[i] * 100:0}% for 30s.");
            })),

        // ═══ FINAL DEFENSE — the passive that reads your own HP bar ══════════════════════════════
        // One rung at 60. Its numbers live in `Entity.FinalDefenceBonus`, not here, because they are
        // read LIVE off current HP — see that method for why a buff would have been the wrong shape.
        new(TankFinalDefense, "Final Defense", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 120_000,
            Description: "Passive. The worse it is going, the harder you are: below 75% HP +10% P.Def; "
                       + "below 50% +20% P.Def and +5% M.Def; below 25% +30% P.Def and +10% M.Def."),

        // ═══ AGGRAVATED STATE — the tank's gift to the party, paid for in blood ═══════════════════
        // *"When dmg is received with 15% chance increase P.Atk and Attack speed of party memebrs
        // with 2% and owner with 3% and P.Skill.Power with 10%"*. A defensive proc: it fires because
        // he is being hit, which is the one thing a tank reliably is.
        //
        // ⚠ TWO PAYLOADS, and the owner's is the stronger — his row names both numbers. The party
        // rung and the self rung share a buff key, so a tank standing in his own party aura is never
        // holding two of them.
        new(TankAggravatedState, "Aggravated State", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 120_000,
            ProcChance: 0.15f, ProcOnDamaged: true, ProcCooldownTicks: 250,
            // ⚠ THESE ARRAYS ARE INDEXED BY RUNG, not a single payload repeated — `Rung(...)` picks
            // entry `level - 1`. That is what carries his three rows (52 / 60 / 68) up the ladder.
            ProcSelfRungs: new[] { TankAggravatedSelf, TankAggravatedSelf + "_2", TankAggravatedSelf + "_3" },
            ProcPartyRungs: new[] { TankAggravatedParty, TankAggravatedParty + "_2", TankAggravatedParty + "_3" },
            Description: "Passive. Taking a hit has a 15% chance to rouse you and your party: more "
                       + "attack power, attack speed and physical skill power for 30s.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 120_000),
                new SkillLevel(SpCost: 120_000),
                new SkillLevel(SpCost: 120_000),
            }),

        AggravatedPayload(TankAggravatedSelf,      "Aggravated State", 0.03f, 0.10f),
        AggravatedPayload(TankAggravatedSelf + "_2", "Aggravated State", 0.05f, 0.15f),
        AggravatedPayload(TankAggravatedSelf + "_3", "Aggravated State", 0.07f, 0.20f),
        AggravatedPayload(TankAggravatedParty,      "Aggravated State", 0.02f, 0.10f, party: true),
        AggravatedPayload(TankAggravatedParty + "_2", "Aggravated State", 0.03f, 0.15f, party: true),
        AggravatedPayload(TankAggravatedParty + "_3", "Aggravated State", 0.05f, 0.20f, party: true),

        // ═══ SHIELD REINFORCEMENT — a toggle you pay for by the second ═══════════════════════════
        // +300 P.Def and +50% block RATE while it burns 15 MP/s. Heavy armour and a shield, like
        // every other shield line in the kit.
        new(TankShieldReinforce, "Shield Reinforcement", BaseClass.Fighter,
            SkillEffect.BuffDef | SkillEffect.BuffBlockChance,
            MpCost: 15, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            BuffKey: "shield_reinforcement", Rank: 1, MpPerSecond: 15,
            // 🔴 `Toggle: true` WAS MISSING AND THE SKILL DID NOTHING (`BL-139`, playtest 2026-09-03:
            // *"it not act as a toggle at all .. it casts something but doesnt do nothing ... for a
            // split second i see my pdef rises"*). A stance carries `DurationTicks: 0`, which is only
            // meaningful WITH the flag — `ApplyBuff` gives a toggle `int.MaxValue` and everything else
            // the authored duration, so without it the +300 P.Def landed and expired on the same tick.
            // ⚠ His CSV row has said `Toggle` in the TYPE column since the file was written; `--check`
            // now compares that word, so the next stance that forgets the flag is a yellow line.
            Category: SkillCategory.Buff, Toggle: true, TargetMode: TargetMode.SelfOnly, SpCost: 120_000,
            RequiredArmor: ArmorWeights.Heavy, RequiredShield: ShieldGate.Required,
            CountsTowardBuffLimit: false,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffDef, 300, ModifierMode.Flat),
                new(SkillEffect.BuffBlockChance, 0.50f, ModifierMode.Percent),
            },
            Description: "Stance. Brace behind your shield: +300 P.Def and half again your block "
                       + "chance, for 15 MP a second."),

        // ═══ THE TWO SHIELD SMASHES — the tank's damage, and the party's crit defence ════════════
        //
        // 🔑 THEY ARE THE SAME SKILL WITH DIFFERENT DEBUFFS, split by race: Human and Elf blunt how
        // OFTEN the thing crits, the Demon how HARD. Both need a shield, both reach 40 (a bash),
        // both ladder Power 1,000 → 4,000 identically. That symmetry is his, and it is what makes the
        // choice a flavour rather than a tier.
        //
        // ⚠ The debuffs are HOLDER-SIDE penalties on the creature, not resistances on the tank — see
        // BuffInstance.CritRatePenalty. A tank cannot out-tank a crit aimed at his healer; he can make
        // the monster worse at critting anyone.
        //
        // 🔑 BOTH REPLACE `strike` (his ruling, 2026-09-03: *"shield smash to replace strike"*). Strike
        // is the level-5 sword/blunt bash off `fighter 1st.csv`; a smash is the same beat with a shield
        // behind it, so the 40 rung retires it the way Holy Ray retires Holy Bolt. It is a WITHIN-chain
        // replace (fighter → tank), which is what his cross-chain id rule allows.
        new(TankSmashRate, "Shield Smash - Rate", BaseClass.Fighter,
            SkillEffect.PhysicalDamage,
            MpCost: BulwarkSmashMp[0], CastTicks: 10, CooldownTicks: 60, Range: 40, Power: 1000,
            DurationTicks: 300, BuffKey: "smash_rate", Rank: 1,
            Category: SkillCategory.Physical, SpCost: BulwarkSp[0],
            RequiredShield: ShieldGate.Required,
            Replaces: new[] { Strike },
            CritRatePenalty: 0.30f, MagicCritRatePenalty: 0.05f,
            Description: "Slams an enemy with your shield and leaves it clumsy for 30s: much less "
                       + "likely to land a critical blow, on either channel. Requires a shield.",
            Levels: BulwarkRungs(i =>
            {
                int[] power = { 1000, 1200, 1200, 1200, 1400, 1600, 1800, 2000, 2200, 2400, 2700, 3000, 3300, 3700, 4000 };
                float[] pRate = { .30f, .31f, .33f, .35f, .37f, .40f, .43f, .45f, .47f, .50f, .50f, .50f, .50f, .50f, .50f };
                float[] mRate = { .05f, .06f, .08f, .10f, .12f, .15f, .18f, .20f, .22f, .25f, .25f, .25f, .25f, .25f, .25f };
                return new SkillLevel(MpCost: BulwarkSmashMp[i], SpCost: BulwarkSp[i], Power: power[i],
                    CritRatePenalty: pRate[i], MagicCritRatePenalty: mRate[i],
                    Description: $"Power {power[i]:N0}; −{pRate[i] * 100:0}% P.Crit rate and "
                               + $"−{mRate[i] * 100:0}% M.Crit rate for 30s.");
            })),

        new(TankSmashPower, "Shield Smash - Power", BaseClass.Fighter,
            SkillEffect.PhysicalDamage,
            MpCost: BulwarkSmashMp[0], CastTicks: 10, CooldownTicks: 60, Range: 40, Power: 1000,
            DurationTicks: 300, BuffKey: "smash_power", Rank: 1,
            Category: SkillCategory.Physical, SpCost: BulwarkSp[0],
            RequiredShield: ShieldGate.Required,
            Replaces: new[] { Strike },
            CritDamagePenalty: 0.15f, MagicCritDamageDebuff: 0.03f,
            Description: "Slams an enemy with your shield and blunts its critical blows for 30s — "
                       + "when it does crit, it hurts far less. Requires a shield.",
            Levels: BulwarkRungs(i =>
            {
                int[] power = { 1000, 1200, 1200, 1200, 1400, 1600, 1800, 2000, 2200, 2400, 2700, 3000, 3300, 3700, 4000 };
                float[] pDmg = { .15f, .17f, .19f, .22f, .24f, .26f, .28f, .31f, .33f, .35f, .35f, .35f, .35f, .35f, .35f };
                float[] mDmg = { .03f, .04f, .05f, .07f, .08f, .09f, .11f, .12f, .13f, .15f, .15f, .15f, .15f, .15f, .15f };
                return new SkillLevel(MpCost: BulwarkSmashMp[i], SpCost: BulwarkSp[i], Power: power[i],
                    CritDamagePenalty: pDmg[i], MagicCritDamageDebuff: mDmg[i],
                    Description: $"Power {power[i]:N0}; −{pDmg[i] * 100:0}% P.Crit damage and "
                               + $"−{mDmg[i] * 100:0}% M.Crit damage for 30s.");
            })),
    };

    /// <summary>One rung of Aggravated State's payload. The self and party versions differ only in
    /// the attack numbers, share a buff key so a tank never wears both, and both run 30 seconds.</summary>
    private static SkillDef AggravatedPayload(string id, string name, float atk, float skillPower,
                                              bool party = false)
        => new(id, name, BaseClass.Fighter,
            SkillEffect.BuffAtk | SkillEffect.BuffAtkSpeed
            | SkillEffect.BuffPveSkillDamage | SkillEffect.BuffPvpSkillDamage,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "aggravated_state", Rank: party ? 1 : 2,
            AreaRadius: 900f, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffAtk, atk),
                new(SkillEffect.BuffAtkSpeed, atk),
                new(SkillEffect.BuffPveSkillDamage, skillPower),
                new(SkillEffect.BuffPvpSkillDamage, skillPower),
            },
            Description: $"+{atk * 100:0}% P.Atk and attack speed, +{skillPower * 100:0}% physical "
                       + "skill power, for 30s.");

    /// <summary>Charm's nineteen rungs: four at the 2nd class (24-36) and fifteen at the 3rd, one
    /// continuous ladder. The aggro numbers are Taunt's, rung for rung — an Elf tank holds a monster
    /// exactly as well as a Human one, which is the point of Charm replacing Taunt rather than
    /// sitting beside it.</summary>
    private static SkillLevel[] TankCharmRungs()
    {
        int[] secondSp = { 6400, 12000, 22000, 40000 };
        var rungs = Enumerable.Range(0, 4).Select(i => new SkillLevel(
            MpCost: 0, SpCost: secondSp[i], Range: 400f, TauntPower: 4500 + i * 500,
            Description: $"Lures an enemy toward you and adds {4500 + i * 500:N0} to your aggro on it."))
            .ToList();

        // The 3rd tier: 6,500 → 12,000, matching his Taunt column cell for cell.
        int[] third = { 6500, 7000, 7500, 8000, 8500, 9000, 9500, 10000, 10500, 11000, 11200, 11400, 11600, 11800, 12000 };
        rungs.AddRange(Enumerable.Range(0, BulwarkLevels.Length).Select(i => new SkillLevel(
            MpCost: 0, SpCost: BulwarkSp[i], Range: BulwarkRange(i), TauntPower: third[i],
            Description: $"Lures an enemy toward you for 3s and adds {third[i]:N0} to your aggro on it.")));
        return rungs.ToArray();
    }

    // =======================================================================================
    //  THE LADDER EXTENSIONS. These eight skills already existed with 2nd-class rungs; his 3rd
    //  file continues them, so the rungs are APPENDED to the existing def rather than replacing it.
    //  Same shape as the healer's `HealerFourth*Rungs()`.
    // =======================================================================================

    /// <summary>TAUNT, rungs 5-19 (his `tank 3rd.csv`). 6,500 → 12,000.
    /// ⚠ His 2nd-class rows were retuned in the same pass — 3s → **1.5s**, range 600 → **400**, and
    /// the MP cost dropped to **0** at every rung. A taunt that costs mana is a taunt a tank stops
    /// spamming, which is the opposite of the threat economy `BL-123` settled.</summary>
    private static SkillLevel[] TankTauntThirdRungs()
    {
        int[] power = { 6500, 7000, 7500, 8000, 8500, 9000, 9500, 10000, 10500, 11000, 11200, 11400, 11600, 11800, 12000 };
        return BulwarkRungs(i => new SkillLevel(
            MpCost: 0, SpCost: BulwarkSp[i], Range: BulwarkRange(i), TauntPower: power[i],
            Description: $"Locks a monster onto you for 1.5s and adds {power[i]:N0} to your aggro on it. "
                       + "It does not put you at the top for free — hold it by keeping the taunt up."));
    }

    /// <summary>STAY, fifteen rungs — and it MOVED. It was the 2nd class's single level-36 skill and
    /// is now the 3rd's whole ladder from 40 (his pass removed the 2nd-class row). 10 seconds, not
    /// the old 15.</summary>
    private static SkillLevel[] TankStayThirdRungs() => BulwarkRungs(i => new SkillLevel(
        MpCost: BulwarkControlMp[i], SpCost: BulwarkSp[i], Range: i <= 6 ? 400f : 600f,
        Description: "Roots the target for 10s (it can still act). ATK-vs-CON; bosses immune."));

    /// <summary>SHIELD SHOCK, rungs 5-19. Renamed from "Shield Stun" in his pass, its reuse cut from
    /// 10s to 3, and its landing multiplier set to ×0.7 — a 9-second stun on a 3-second reuse would
    /// be a perma-lock at ×1, and the ×0.7 is what pays for the cadence.</summary>
    /// ⚠ ITS SIXTH RUNG IS AT **56**, not the 55 every other ladder in the file uses. Monotonic, so
    /// not a dip the rule may straighten — one rung of one skill arrives a level later, and the CSV
    /// is the authority on that.
    internal static readonly int[] TankShieldShockLevels =
        { 40, 43, 46, 49, 52, 56, 58, 60, 62, 64, 66, 68, 70, 72, 74 };

    private static SkillLevel[] TankShieldShockThirdRungs() => BulwarkRungs(i => new SkillLevel(
        MpCost: BulwarkSmashMp[i], SpCost: BulwarkSp[i]));

    /// <summary>HEAVY ARMOR MASTERY, rungs 6-20 (five exist at the 2nd class).
    /// ⚠ HIS WEIGHT COLUMN SAYS `robe` ON ALL FIFTEEN ROWS AND IS A PASTE — every DESCR cell says
    /// *"with heavy"*, the skill is called Heavy Armor Mastery, and the 2nd-class rows it continues
    /// all say `heavy`. Built HEAVY and flagged on `BL-02`; the `BL-105` "the column wins" rule is
    /// for a column that disagrees with the prose about a REAL choice, not for a pasted cell that
    /// contradicts the skill's own name.</summary>
    private static readonly int[] TankArmorPDef =
        { 65, 70, 76, 81, 94, 107, 113, 120, 127, 135, 142, 150, 157, 165, 173 };
    private static readonly float[] TankArmorMpReg =
        { 3.5f, 3.5f, 3.9f, 3.9f, 3.9f, 4.3f, 4.3f, 4.3f, 4.3f, 4.7f, 4.7f, 4.7f, 4.7f, 5.1f, 5.1f };
    private static readonly float[] TankArmorPDefPct =
        { .11f, .11f, .11f, .11f, .11f, .11f, .15f, .15f, .15f, .15f, .15f, .15f, .15f, .15f, .15f };
    private static readonly float[] TankArmorCritRed =
        { .25f, .25f, .25f, .25f, .25f, .25f, .25f, .35f, .35f, .35f, .35f, .35f, .35f, .35f, .35f };

    /// <summary>⚠ ONE CELL OF HIS DIFFERS FROM THE SHARED SP LADDER: Heavy Armor Mastery at level 55
    /// costs 80,000 where every other skill in the file costs 81,000 at that rung. It is his number
    /// and it is monotonic, so it is not a typo the ladder rule may straighten — it is simply a
    /// thousand cheaper, and the CSV is the authority.</summary>
    private static int TankArmorMasterySp(int i) => i == 5 ? 80_000 : BulwarkSp[i];

    private static SkillLevel[] TankArmorMasteryThirdRungs() =>
        BulwarkRungs(i => new SkillLevel(SpCost: TankArmorMasterySp(i),
            Description: $"With heavy armor: +{TankArmorPDef[i]} P.Def, "
                       + $"×{1f + TankArmorPDefPct[i]:0.00} P.Def, ×{TankArmorMpReg[i]:0.0} MP regen, "
                       + $"{TankArmorCritRed[i] * 100:0}% less crit damage taken, −2 evasion."));

    /// <summary>The armour PROFILES for those rungs — a parallel array, like the weapon mastery's,
    /// because an armour mastery's payload rides <c>ArmorMasteryLevels</c> rather than the SkillLevel.
    /// ⚠ Both `p.def x1.11 → x1.15` and the crit-damage reduction `25% → 35%` step at this tier; the
    /// 2nd class's helper hard-codes 0.07 and 0.15, which is why this cannot reuse it.</summary>
    private static ArmorMasteryProfile[] TankArmorMasteryThirdProfiles() =>
        Enumerable.Range(0, BulwarkLevels.Length).Select(i => new ArmorMasteryProfile(
            Robe: default, Light: default,
            Heavy: new StatMods(
                MpRegenPct: TankArmorMpReg[i] - 1f,   // "mpReg x3.5" is a MULTIPLIER on the stack
                PDef: TankArmorPDef[i], PDefPct: TankArmorPDefPct[i],
                CritDmgResist: TankArmorCritRed[i], Evasion: -2))).ToArray();

    /// <summary>TANK ANTI-MAGIC, rungs 6-20. M.Def 51 → 130, and it gains MAGIC RESISTANCE at this
    /// tier — 5% → 20%, a real damage reduction rather than a fizzle chance (his 2026-08-10 ruling).
    /// ⚠ The `robe` in his WEIGHT column is the same paste as Heavy Armor Mastery's, and here it is
    /// even clearer that it is one: not a single DESCR cell mentions armour at all.</summary>
    private static SkillLevel[] TankAntiMagicThirdRungs()
    {
        int[] mDef = { 51, 55, 59, 67, 75, 84, 89, 94, 98, 103, 109, 114, 119, 124, 130 };
        float[] mRes = { .05f, .05f, .05f, .05f, .10f, .10f, .10f, .10f, .15f, .15f, .15f, .15f, .20f, .20f, .20f };
        return BulwarkRungs(i => new SkillLevel(SpCost: BulwarkSp[i],
            Passive: new PassiveEffect(MagicDefence: mDef[i], MagicResist: mRes[i]),
            Description: $"+{mDef[i]} M.Def and {mRes[i] * 100:0}% magic resistance."));
    }

    /// <summary>TANK WEAPON MASTERY, rungs 6-20. ×1.085 P.Atk at every rung plus a flat climb.
    /// ⚠ HIS ROW AT 52 READS `+26` BETWEEN `+31` AND `+41` — a dip, and by the monotonic rule a typo
    /// rather than a design (a rung you pay 74,000 SP for cannot make you weaker). Interpolated to
    /// **36**, which is the midpoint his own neighbours describe, and reported.</summary>
    private static SkillLevel[] TankWeaponMasteryThirdRungs()
    {
        return BulwarkRungs(i => new SkillLevel(SpCost: BulwarkSp[i],
            Description: $"With a one-handed sword or blunt: ×1.085 P.Atk and "
                       + $"+{TankWeaponMasteryFlatAtk[i]} P.Atk."));
    }

    /// <summary>The FLAT half of the rungs above. It lives apart because a weapon mastery's payload
    /// rides <c>SkillDef.WeaponMasteryLevels</c> — a parallel array indexed by rung — rather than the
    /// SkillLevel, so the numbers have to be handed to two places and are written once here.</summary>
    private static readonly int[] TankWeaponMasteryFlatAtk =
        { 19, 22, 26, 31, 36, 41, 46, 50, 54, 59, 63, 67, 72, 76, 81 };

    /// <summary>Tank Weapon Mastery's 3rd-tier weapon profiles, in the same rung order.</summary>
    private static WeaponMasteryProfile[] TankWeaponMasteryThirdProfiles() =>
        TankWeaponMasteryFlatAtk
            .Select(v => OneHand(new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: v)))
            .ToArray();

    // (SHIELD MASTERY needs nothing here. Its 3rd-tier pair — rungs 3 and 4 — was already in the def
    //  from the single row he authored in 2026-08-21; this pass only corrected rung 3's SP to his
    //  28,000 and added the LEARN line at 40, which is what was actually missing.
    //  ⚠ His percentages are IG units and the SHIELD-P.DEF COLUMN ONLY is ×5: "Shield P.Def +50%" is
    //  2.50 in the def, "+60%" is 3.00. Block RATE and bow resistance are read straight.)

    /// <summary>DEFENSIVE WALL's 3rd-tier rung (46). Rung 2 of the ladder.
    /// ⚠ HIS PASS DELETED THE `x2` PERCENT TERMS from the 2nd-class row — it is flat P.Def and M.Def
    /// now, on both rungs. A doubling on top of a four-figure flat was the single largest defensive
    /// number in the game and he took it out; do not put it back.</summary>
    private static SkillLevel[] TankDefensiveWallThirdRungs() => new[]
    {
        new SkillLevel(MpCost: 20, SpCost: 40_000,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffDef, 3700, ModifierMode.Flat),
                new(SkillEffect.BuffMagicDef, 3300, ModifierMode.Flat),
                new(SkillEffect.BuffCancelResist, 0.80f, ModifierMode.Percent),
                new(SkillEffect.BuffMoveSpeed, -0.50f, ModifierMode.Percent),
            },
            Description: "Raise an impregnable guard for 30s: +3700 P.Def, +3300 M.Def and high "
                       + "cancel resistance, but your movement is halved."),
    };
}
