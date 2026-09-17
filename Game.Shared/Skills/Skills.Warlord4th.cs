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
    public const string WarriorChargeNormal  = "warrior_charge_normal";
    public const string WarriorChargeInstant = "warrior_charge_instant";
    public const string WarriorChargeStun    = "warrior_charge_stun";
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
        { WarriorChargeNormal, WarriorChargeInstant, WarriorChargeStun, WarriorChargeAoe };

    private static string[] ChargeReplaces(string self) =>
        new[] { WarriorCharge }.Concat(WarlordChargeIds.Where(id => id != self)).ToArray();

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
            Charge(WarriorChargeInstant, "Flash Step", WarriorChargeInstant,
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
            Charge(WarriorChargeStun, "Charge n Shock", WarriorChargeStun,
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
                AreaRadius: 200f, AreaAtTarget: true, SpCost: 0,
                Description: "The landing of a Warlord's charge."),
        };
    }
}
