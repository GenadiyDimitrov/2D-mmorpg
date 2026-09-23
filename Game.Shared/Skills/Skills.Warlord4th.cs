using System.Linq;

namespace Game.Shared;

/// <summary>THE WARLORD'S 4th CLASS, 76-90 — `docs/data/classes_skills_csv/war_aoe 4th.csv`.
///
/// <para>🔑 <b>THE FOUR CHARGES ARE ONE CHOICE, MADE ONCE</b> (owner, 2026-09-17: *"in the war_aoe 4th
/// i have several charges that picking one should remove the others and never be able to learn them
/// (interlock each other)"*). He built it out of MUTUAL <see cref="SkillDef.Replaces"/> — each of the
/// four names the other three plus the base Charge — and that is all it takes, because `Replaces` was
/// already enforced in three places: the learn gate (<c>GameLoopService.IsSuperseded</c>), the client's
/// learn tab (<c>GameUi.Skills.Superseded</c>), and retroactively on every login
/// (<c>PersistenceService</c>). Learn one and the other three leave the tree for good.</para>
///
/// <para>🔑 <b>HIS DURATION COLUMN IS THE STRIDE</b>, and it is the whole of the second half of his
/// note: *"i gave on charge duration .. it should move the distance for the duration .. not
/// instantly"*. 1s for the normal charge, 2s for the slow one that stuns, 1s for the stomp, and
/// <b>0 for Flash Step, which is the instant one</b>. It lands in <see cref="SkillDef.PullSeconds"/>,
/// which the drag machinery has always read; a charge whose seconds round to zero ticks becomes a
/// teleport rather than a one-tick crawl. See <c>GameLoopService.BeginDrag</c>.</para>
///
/// <para>🔑 <b>THEY DIFFER IN EXACTLY ONE TRADE EACH</b>, straight off his four lines:
/// <list type="bullet">
/// <item>Charge — 1s stride, 5s reuse. The baseline, 200 further than the 76 rung.</item>
/// <item>Flash Step — instant, no cast at all, and pays for it with DOUBLE the reuse.</item>
/// <item>Charge n Shock — twice as slow to arrive, and stuns what it reaches. Double reuse.</item>
/// <item>Charge n Stomp — *"give up on cooldown for a dmg"*: the normal stride, double reuse, and an
/// AoE on landing.</item>
/// </list></para>
///
/// <para>⚠ <b>THE FLOOR IS ON ALL FOUR AND ON THE BASE CHARGE</b> — *"Min charge distance 150"*,
/// authored on every charge row in both files on 2026-09-17. A nearer target refuses the cast rather
/// than spending the reuse on a stride of nine units. See <see cref="SkillDef.MinChargeDistance"/>.</para>
/// </summary>
public static partial class SkillCatalog
{
    public const string MasterOfCombat  = "master_of_combat";
    public const string ShockingJavelin  = "shocking_javelin";

    /// <summary>76 → 90, every level — the band his fifteen-rung 4th-tier families use, and the same
    /// one `warrior 4th.csv` uses. Shocking Shout, Shocking Javelin and Whirlwind ride it.</summary>
    internal static int[] Warlord4thLevels => Warrior4thLevels;
    /// <summary>76 → 90 every OTHER level — the three race Shouts' column, and HP Boost's.</summary>
    internal static int[] Warlord4thEven => Warrior4thEven;

    /// <summary>🔑 SHOCKING SHOUT AND SHOCKING JAVELIN SHARE ONE POWER COLUMN AT THE 4th TIER, and it
    /// is <see cref="W4SlashPower"/> to the cell — 4500 → 6500. Two skills, one ladder, which is his
    /// authoring: the Javelin is the Shout thrown 900 away with a tighter ring (150 against 200), and
    /// the trade is reach for radius, not reach for power.</summary>
    internal static int[] W4WaraoeShoutPower => W4SlashPower;
    /// <summary>Whirlwind's 4th tier: 1050 climbing +50 a rung to 1750 — the SAME +50 stride as
    /// <see cref="WaraoeWhirlwindPower"/>, continuing it with no step at the tier boundary. It was this
    /// column that showed the 3rd-tier one had been mis-transcribed (`BL-261`, fixed 2026-09-17).</summary>
    internal static readonly int[] W4WaraoeWhirlwindPower =
        { 1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400, 1450, 1500, 1550, 1600, 1650, 1700, 1750 };

    /// <summary>TWO-HAND (BLUNT) MASTERY rungs 16-30 — his `war_aoe 4th.csv` column. P.Atk 153 → 200
    /// (+3 a rung, widening to +4 at 86) and crit damage 632 → 860 (+17 a rung, one +12 at 79).
    /// ⚠ The CLEAVE plateaus at 10 for the whole tier: it already reached 10 at 3rd-tier rung 6, so
    /// this tier buys power and never width. That is his column, not a ceiling in the code.</summary>
    internal static readonly int[] W4BluntAtk =
        { 153, 156, 159, 162, 165, 168, 171, 174, 177, 180, 184, 188, 192, 196, 200 };
    /// <inheritdoc cref="W4BluntAtk"/>
    internal static readonly float[] W4BluntCritDmg =
        { 632, 649, 666, 678, 690, 707, 724, 741, 758, 775, 792, 809, 826, 843, 860 };

    /// <summary>The three race Shouts, one last step each across eight even rungs — P.Def 25 → 27%,
    /// P/M.Atk 12 → 15%, speeds 25 → 30%. Flat then one move, exactly like the Ravager's Slashes.</summary>
    internal static readonly float[] W4WaraoeHumanShoutDef =
        { .25f, .25f, .25f, .25f, .27f, .27f, .27f, .27f };
    internal static readonly float[] W4WaraoeDemonShoutAtk =
        { .12f, .12f, .12f, .12f, .12f, .15f, .15f, .15f };
    internal static readonly float[] W4WaraoeElfShoutSpeed =
        { .25f, .25f, .25f, .25f, .25f, .30f, .30f, .30f };

    public const string WarriorChargeNormal  = "warrior_charge_normal";
    public const string FlashStep = "flash_step";
    public const string ChargeNShock    = "charge_n_shock";
    public const string WarriorChargeAoe     = "warrior_charge_aoe";
    /// <summary>THE STOMP ITSELF — the hidden sub-skill Charge n Stomp fires when it ARRIVES. Never
    /// learned, never on a bar, no MP and no reuse of its own; see
    /// <see cref="SkillDef.ChargeArrivalSkill"/>. It exists because the drag's stun TAIL can express a
    /// stun and nothing else, and *"Dmg enemies with power +5000"* in a 200 radius is a real skill
    /// execution — its own crit, its own block, its own splash.</summary>
    public const string WarriorChargeStomp   = "warrior_charge_stomp";

    /// <summary>All four are learned at 80 for 0 SP and 5kk gold — his cells, and <see cref="F4"/>(4)
    /// to the coin, which is the same rung every other level-80 row of that file is priced at.</summary>
    private static (int Sp, int Gold) WarlordCharge80() => F4(4);

    /// <summary>The interlock, built from the id list: each variant replaces the base Charge and every
    /// OTHER variant. One place, so a fifth charge cannot be added and half-wired.</summary>
    private static readonly string[] WarlordChargeIds =
        { WarriorChargeNormal, FlashStep, ChargeNShock, WarriorChargeAoe };

    private static string[] ChargeReplaces(string self) =>
        new[] { WarriorCharge }.Concat(WarlordChargeIds.Where(id => id != self)).ToArray();

    /// <summary>THE TWO SKILLS THAT ARE NEW AT THE 4th TIER, and the only ones — everything else in
    /// `war_aoe 4th.csv` is a continuation of a ladder his 3rd file already opened.</summary>
    private static SkillDef[] Warlord4thNewSkills()
    {
        var (mcSp, mcGold) = F4New(76);

        return new SkillDef[]
        {
            // ═══ MASTER OF COMBAT — the Warlord's stance ═════════════════════════════════════════
            // *"Increase accuracy +10, P.Def with 25%, M.Def with 15%, Atk.Speed +10%, Decrease move speed
            //  with 30% and evasion with 10; Consume 30 MP/s"* (`BL-275`, 2026-09-23: Atk.Speed was +20%).
            // 🔑 THE SPEED CUT IS A MINUS ON THE BUFF, not a Slow debuff — the `BL-237` lesson from
            //    Battle Frenzy's healing penalty, verbatim: **a downside you chose is not a curse
            //    somebody cast on you.** A `Slow` would sit in `AnyDebuff` and `ControlCc`, so a cleanse
            //    would strip it, CON resistance would refuse it and a boss would be immune to your own
            //    stance. `MoveSpeedPenaltyPct` is the field the Marks already use (`BL-238`).
            // ⚠ A TOGGLE, so it runs until you cannot pay for it — see TickToggleUpkeep, which reads
            //   the RUNG's `MpPerSecond` and not the def's.
            new SkillDef(MasterOfCombat, "Master of Combat", BaseClass.Fighter,
                SkillEffect.BuffAccuracy | SkillEffect.BuffDef | SkillEffect.BuffMagicDef
                | SkillEffect.BuffAtkSpeed | SkillEffect.BuffEvasion,
                MpCost: 30, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                Toggle: true, MpPerSecond: 30, MoveSpeedPenaltyPct: 0.30f,
                BuffKey: MasterOfCombat, Rank: 1,
                Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
                RequiredWeapon: WeaponType.AnyBlunt, RequiredHands: WeaponHands.Two,
                SpCost: mcSp,
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.BuffAccuracy, 10f, ModifierMode.Flat),
                    new(SkillEffect.BuffDef, .25f),
                    new(SkillEffect.BuffMagicDef, .15f),
                    new(SkillEffect.BuffAtkSpeed, .10f),
                    new(SkillEffect.BuffEvasion, -10f, ModifierMode.Flat),   // `BL-275`
                },
                Description: "A stance of total control: you guard and swing far better, and move far "
                           + "worse. 30 MP a second, and a two-handed blunt.",
                Levels: new[]
                {
                    new SkillLevel(MpCost: 30, SpCost: mcSp, GoldCost: mcGold,
                        Description: "+10 accuracy, +25% P.Def, +15% M.Def and +10% attack speed, "
                                   + "at −30% move speed, −10 evasion and 30 MP a second."),
                }),

            // ═══ SHOCKING JAVELIN — the Shout, thrown ════════════════════════════════════════════
            // *"Trows thunder javelin to do Physical damage with +N power and Stuns around for 5s"*,
            // range 900, AOE 150 — the one Warlord active that is NOT centred on the caster, which is
            // why it is the only one carrying `AreaAtTarget`.
            // 🔑 SAME POWER LADDER AS SHOCKING SHOUT, cell for cell. It buys reach with RADIUS (150
            //    against 200), not with damage.
            // ⚠ Its landing modifier rides Shocking Shout's, and both are owed by him (`BL-259`).
            WarlordShout4th(ShockingJavelin, "Shocking Javelin",
                range: 900f, radius: 150f, atTarget: true,
                "A javelin of thunder, thrown into a crowd 900 away."),
        };
    }

    /// <summary>A 4th-tier Warlord ring-strike — Shocking Javelin today. Fifteen rungs on
    /// <see cref="W4WaraoeShoutPower"/> and <see cref="W4HeavyMp"/>, a contested 5s stun, unblockable.
    /// A skill first LEARNED at 76, so <see cref="F4New"/> prices rung 1 and <see cref="F4"/> the rest —
    /// the shape every "new at 76" ladder in the healer's file established.</summary>
    private static SkillDef WarlordShout4th(string id, string name, float range, float radius,
                                            bool atTarget, string blurb)
    {
        string Rung(int p) =>
            $"Strikes everything within {(int)radius} for power {p:N0} and stuns it for 5s. "
          + "Cannot be blocked, can double.";

        return new SkillDef(id, name, BaseClass.Fighter,
            SkillEffect.PhysicalDamage | SkillEffect.Stun,
            MpCost: W4HeavyMp[0], CastTicks: 10, CooldownTicks: 50, Range: range,
            Power: W4WaraoeShoutPower[0], DurationTicks: 50,
            BuffKey: id, Rank: 1, DebuffSchool: DebuffSchool.Physical,
            Category: SkillCategory.Physical, CanDouble: true, BlockAccuracy: 1f,
            // ⚠ `EnemiesInRadius` — `BL-265`. It keeps `AreaAtTarget`, so unlike the Warlord's other
            //   rings this one still NEEDS a body to throw at: the circle sits 900 away, on the target.
            AreaRadius: radius, AreaAtTarget: atTarget, TargetMode: TargetMode.EnemiesInRadius,
            RequiredWeapon: WeaponType.AnyBlunt, RequiredHands: WeaponHands.Two,
            SpCost: F4New(76).Sp,
            Description: blurb,
            Levels: Enumerable.Range(0, Warlord4thLevels.Length).Select(i =>
            {
                var (sp, gold) = i == 0 ? F4New(76) : F4(i);
                return new SkillLevel(Power: W4WaraoeShoutPower[i], MpCost: W4HeavyMp[i],
                                      SpCost: sp, GoldCost: gold,
                                      Description: Rung(W4WaraoeShoutPower[i]));
            }).ToArray());
    }

    private static SkillDef[] Warlord4thCharges()
    {
        var (sp, gold) = WarlordCharge80();

        // Everything his four rows share: 800 range, 70 MP, a 150 floor, two-handed sword or blunt,
        // and the enemy-single target the base Charge already had.
        SkillDef Charge(string id, string name, string self, int castTicks, int cooldownTicks,
                        float strideSeconds, SkillEffect effect, string description,
                        int durationTicks = 0, float areaRadius = 0f,
                        DebuffSchool school = DebuffSchool.None, string? arrival = null) =>
            new(id, name, BaseClass.Fighter, effect,
                MpCost: 70, CastTicks: castTicks, CooldownTicks: cooldownTicks, Range: 800, Power: 0,
                DurationTicks: durationTicks,
                Category: school == DebuffSchool.None ? SkillCategory.Physical : SkillCategory.Debuff,
                DebuffSchool: school, SpCost: sp,
                ChargesToTarget: true, PullSeconds: strideSeconds, MinChargeDistance: 150f,
                ChargeArrivalSkill: arrival,
                AreaRadius: areaRadius, AreaAtTarget: areaRadius > 0f,
                RequiredWeapon: WeaponType.AnySword | WeaponType.AnyBlunt,
                RequiredHands: WeaponHands.Two,
                Replaces: ChargeReplaces(self),
                Description: description,
                // ⚠ ONE RUNG, and it exists only because GOLD is a per-LEVEL cell (SkillLevel.GoldCost)
                //   while SP is a def-level default. A single-rung skill with a gold price has to say
                //   so here or the 5kk is silently free.
                Levels: new[] { new SkillLevel(MpCost: 70, SpCost: sp, GoldCost: gold,
                                               Description: description) });

        return new[]
        {
            // ═══ CHARGE — the baseline, 200 further than the 76 rung and nothing else new ════════
            Charge(WarriorChargeNormal, "Charge", WarriorChargeNormal,
                castTicks: 5, cooldownTicks: 50, strideSeconds: 1f, SkillEffect.None,
                "Close the ground to an enemy up to 800 away, over one second."),

            // ═══ FLASH STEP — *"isntant (like phantom jump) -> higher cd no duration no cast"* ═══
            // 🔑 STILL A CHARGE, NOT A BLINK, and deliberately: it keeps the 150 floor, the weapon
            //    requirement and the arrival seam, and it is the DURATION cell alone that makes it
            //    instant. Authoring it as `SkillEffect.Blink` would have given it a second, parallel
            //    set of rules to keep in step with the other three for no gain.
            Charge(FlashStep, "Flash Step", FlashStep,
                castTicks: 0, cooldownTicks: 100, strideSeconds: 0f, SkillEffect.None,
                "Step instantly to an enemy up to 800 away. No cast — and twice the reuse."),

            // ═══ CHARGE N SHOCK — *"charges slower for 2 sec, but in the end stuns"* ═════════════
            // 🔑 THE STUN IS THE DRAG'S TAIL WITH THE ENDS SWAPPED, which is why this is one skill id
            //    and one row in `debuff_landmods.csv` rather than a hidden sub-skill: `BL-154` already
            //    built "apply the stun WHEN THE JOURNEY ARRIVES" for the tank's Grapple, and the only
            //    difference here is which end of the journey takes it. See GameLoopService.StartCharge.
            // ⚠ TWO SEPARATE 2s, and they only LOOK like one number: `PullSeconds` is the stride (his
            //   DURR cell) and `DurationTicks` is the stun (*"Chance to stun target for 2s"*, his
            //   DESCR). They are authored apart so that changing one never silently moves the other.
            Charge(ChargeNShock, "Charge n Shock", ChargeNShock,
                castTicks: 5, cooldownTicks: 100, strideSeconds: 2f, SkillEffect.Stun,
                "A heavy, deliberate charge — slower to arrive, and it puts the target down for 2s.",
                durationTicks: 20, school: DebuffSchool.Physical),

            // ═══ CHARGE N STOMP — *"charges like normal charge just give up on cooldown for a dmg"* ═
            // The stride is the normal one; the price is the reuse, and the payload lands on arrival.
            Charge(WarriorChargeAoe, "Charge n Stomp", WarriorChargeAoe,
                castTicks: 5, cooldownTicks: 100, strideSeconds: 1f, SkillEffect.None,
                "Close the ground and land hard: everything within 200 of your target is caught.",
                areaRadius: 200f, arrival: WarriorChargeStomp),

            // ═══ THE STOMP. No MP, no reuse, no class table — fired by the arrival, nothing else ═══
            // ⚠ NO `ChargesToTarget`. It runs at the end of a charge; giving it one would start a
            //   second drag from a body that has just finished one.
            new(WarriorChargeStomp, "Charge n Stomp", BaseClass.Fighter, SkillEffect.PhysicalDamage,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 40, Power: 5000,
                Category: SkillCategory.Physical, CanDouble: true,
                // ⚠ `EnemiesInRadius` — `BL-265`. `FinishPull`'s own comment already claimed the
                //   arrival *"brings its own AoE"*; without this line it brought a single-target hit
                //   on the anchor and the whole difference between Stomp and plain Charge was the reuse.
                AreaRadius: 200f, AreaAtTarget: true, TargetMode: TargetMode.EnemiesInRadius, SpCost: 0,
                Description: "The landing of a Warlord's charge."),
        };
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE CONTINUING RUNGS. Each returns ONLY the 4th-tier half; the def in Skills.Warlord3rd.cs
    //  concatenates it, exactly as the archer's, the tank's and the Ravager's 4th tiers do.
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>SHOCKING SHOUT rungs 16-30 — his 4500 → 6500 column on the heavy MP ladder.</summary>
    internal static SkillLevel[] WarlordShockShoutRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(Power: W4WaraoeShoutPower[i], MpCost: W4HeavyMp[i], SpCost: sp, GoldCost: gold,
            Description: $"Strikes everything within 200 for power {W4WaraoeShoutPower[i]:N0} and stuns "
                       + "it for 5s. Cannot be blocked, can double."));

    /// <summary>WHIRLWIND rungs 16-30 — 1050 → 1750, +50 a rung, PER STROKE. Twenty of them land.</summary>
    internal static SkillLevel[] WarlordWhirlwindRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(Power: W4WaraoeWhirlwindPower[i], MpCost: W4HeavyMp[i], SpCost: sp, GoldCost: gold,
            Description: WhirlwindRungText(W4WaraoeWhirlwindPower[i])));

    /// <summary>TAUNTING SHOUT rungs 3-4, at 80 and 90 — the ring stays 600 and the vulnerability goes
    /// 25% then 30%. ⚠ HIS 4th-FILE RADIUS IS 600 AT BOTH, which is BELOW the 800 his 3rd file reaches
    /// at level 74. Written through as authored: only a DIP in a stat is a defect, and reach is not a
    /// stat — but it is listed in `BL-261` beside the Whirlwind column so he sees both at once.</summary>
    internal static SkillLevel[] WarlordTauntingShoutRungs() => new[]
    {
        new SkillLevel(MpCost: 70, SpCost: F4(4).Sp, GoldCost: F4(4).Gold, Range: 0f, AreaRadius: 600f,
            WeaponVulnerabilityPct: .25f,
            Description: "Provokes everything within 600 for 30s and leaves it taking 25% more damage "
                       + "from blunt weapons."),
        new SkillLevel(MpCost: 80, SpCost: F4(14).Sp, GoldCost: F4(14).Gold, Range: 0f, AreaRadius: 600f,
            WeaponVulnerabilityPct: .30f,
            Description: "Provokes everything within 600 for 30s and leaves it taking 30% more damage "
                       + "from blunt weapons."),
    };

    /// <summary>A RACE SHOUT'S rungs 16-23 — eight EVEN levels, 76 → 90, on the Focused Blast's MP
    /// column (80 → 110), which is the column his eight-rung 4th-tier rows share.</summary>
    internal static SkillLevel[] WarlordRaceShoutRungs(float[] column,
                                                       Func<float, EffectMagnitude[]> mags,
                                                       Func<float, string> rungText) =>
        F4Rungs(8, 2, (i, sp, gold) => new SkillLevel(
            MpCost: W4FocusedBlastMp[i], SpCost: sp, GoldCost: gold,
            Magnitudes: mags(column[i]), Description: rungText(column[i])));

    /// <summary>A SUPPORT'S rung 4, at level 80 — *"20% chance to heal for 15% max HP"* plus a much
    /// stronger lingering half. The passive's own rung; its payload def is built beside the other three.
    ///
    /// <para>🔴 <b>HIS 4th FILE NAMES `waraoe_life_support` FOR ALL THREE RACES</b>, while the section
    /// headers immediately above those rows read Life Support / Blood Support / Vanguard Support — the
    /// 3rd file's three separate ids. Built as rung 4 of EACH RACE'S OWN ladder, because three rows with
    /// three different payloads and three different race cells are three skills however they are spelt.
    /// Flagged to him in 0.166.0's CHANGELOG and in `BL-237` §5; one word reverses it.</para></summary>
    internal static SkillLevel SupportRung4(EffectMagnitude[] lingering, string lingeringText) =>
        new(SpCost: F4(4).Sp, GoldCost: F4(4).Gold, ProcChance: .20f,
            Description: $"20% chance on a landed blow (15s reuse) to heal 15% of max HP {lingeringText}.");
}
