namespace Game.Shared;

// ===========================================================================
//  WHISPS — `BL-109`. His design, and `docs/data/classes_skills_csv/whisps_skills.csv`
//  is the authored file this is built from, row for row.
//
//  A whisp is a non-targetable support spirit that RIDES its master and fires its own skills on its
//  own judgement. His three-way separation is worth keeping in front of you while reading this file,
//  because each of the three is a different mechanism:
//
//      TOTEM — stands still, pulses an AoE at whoever walks into it, off the master's pvp-on/off.
//      PET   — does only what it is ordered to, and uses no skill unprompted.
//      WHISP — FOLLOWS, and ACTS ON ITS OWN, off the master's pvp-on/off.
//
//  🔑 NOT AN ENTITY, on his instruction: *"it can be part of the character game object no need a real
//  entety"*. The same reasoning the totem was built on and for the same reason — a new EntityKind has
//  to be audited through every "is this a mob / not a player" test in the server, and whichever one
//  was missed is where a whisp silently becomes a valid aggro, damage or loot target. A whisp lives
//  as a row on its master (`Entity.Whisps`) and is drawn from a position the server derives.
//
//  🔑 UNINFLUENCED BY MASTER GEAR — his rule. A whisp's own P/M.Atk is 1; what scales is the WHISP's
//  rung and the MASTER's LEVEL, and nothing else. So none of these skills reads an attack stat: the
//  debuffs contest on a flat whisp CC attack (GameConstants.WhispCcAtk) at the master's level, and
//  the heals are flat powers. A fully-geared tank and a naked one have the same whisp.
//
//  🔑 THEIR DEBUFFS SHARE THE PLAYER FAMILY'S BuffKey, deliberately — *"Whisp debuffs do not stack
//  with the player version"*. A whisp Armor Break and a Lightbringer's are the same family, so the
//  ordinary rank rules resolve them: any real rung outranks or ties-and-outlasts the whisp's, and the
//  whisp's can never overwrite a healer's work.
//
//  THE SUMMONS ARE AT THE BOTTOM OF THIS FILE — the six calls and Whisp Mastery, straight off the
//  `Whisps` block of `tank 3rd.csv`. WHO learns them is a separate question and lives in
//  RaceAndClasses/ClassSkillTables.Third.cs, like every other class assignment.
// ===========================================================================

public partial class SkillCatalog
{
    // ---- The whisp's own kit. One id per row of `whisps_skills.csv`. ----
    public const string WhispProvoke    = "whisp_provoke";
    public const string WhispCharm      = "whisp_charm";
    public const string WhispBind       = "whisp_bind";
    public const string WhispArmorBreak = "whisp_armor_break";
    public const string WhispWeaponBreak= "whisp_weapon_break";
    public const string WhispGravity    = "whisp_gravity";
    public const string WhispHeal       = "whisp_heal";
    public const string WhispQuickHeal  = "whisp_quick_heal";
    public const string WhispClear      = "whisp_clear";

    /// <summary>Is this id one of the whisp's own skills? Used to keep them out of every player-facing
    /// list — they are never learned, never bought, never placed on a bar, and never appear in the
    /// Learn tab. The whisp casts them; the player only chooses which whisp to call.</summary>
    public static bool IsWhispSkill(string id) => id.StartsWith("whisp_", System.StringComparison.Ordinal);

    /// <summary>The nine skills a whisp can cast, straight off `whisps_skills.csv`.
    ///
    /// <para>⚠ EVERY ONE OF THEM IS <c>BaseClass.Mage</c> AND CARRIES NO SP OR LEARN LINE. That is not
    /// laziness about the class field: a whisp skill is never on anyone's shelf, so the only thing the
    /// class would decide is a filter nobody applies to it. What matters is that no class table ever
    /// references these ids.</para>
    ///
    /// <para>⚠ THE RANGE IS THE WHISP'S, NOT THE MASTER'S — 400 on every offensive row, measured from
    /// where the whisp is floating. A whisp trails its master, so in practice it reaches slightly less
    /// far than he does, which is the intended feel: the spirit has to come along to help.</para></summary>
    // ---- HIS LADDERS, from the `Whisps` block of `tank 3rd.csv`. Eight rungs each, and the six
    //      summons split into two level sets: the "A" whisps start at 40, the "B" whisps at 43.
    //      Everything else about a rung is shared, so it is stated once here rather than six times.

    /// <summary>The A set's learn levels (Taunting / Charming / Armor Breaking).</summary>
    private static readonly int[] WhispLevelsA = { 40, 46, 52, 58, 62, 66, 70, 74 };
    /// <summary>The B set's (Binding / Healing / Weapon Breaking).</summary>
    private static readonly int[] WhispLevelsB = { 43, 49, 55, 60, 64, 68, 72, 74 };

    /// <summary>MP per rung. THE SAME EIGHT NUMBERS FOR ALL SIX WHISPS — his column, verbatim.</summary>
    private static readonly int[] WhispMp = { 50, 54, 58, 66, 76, 84, 92, 100 };

    /// <summary>SP per rung, one ladder per level set.
    ///
    /// <para>⚠ HIS COLUMN HAS NO `k`, and these are read as THOUSANDS. Every other file in the folder
    /// writes `36k` / `880k`; `tank 3rd.csv` writes `28` / `880`. The parallel that settles it is his
    /// own level-74 rows: this file says `880` where the healer rows he pasted into the SAME file say
    /// `880k` at 74. Flagged to him on `BL-109` — if it is wrong it is wrong on every row of the
    /// file, not just these.</para></summary>
    private static readonly int[] WhispSpA = { 28_000, 40_000, 74_000, 88_000, 170_000, 280_000, 390_000, 880_000 };
    private static readonly int[] WhispSpB = { 35_000, 50_000, 81_000, 120_000, 190_000, 320_000, 650_000, 880_000 };

    /// <summary>The AGGRO ladder his taunting and charming whisps share, cell for cell.</summary>
    private static readonly int[] WhispThreat = { 6500, 7500, 8500, 9500, 10500, 11200, 11600, 12000 };

    /// <summary>The healing whisp's power, from *"(Power 250)"* … *"(Power 740)"*. ⚠ It is +70 a rung
    /// exactly, all the way up — a straight line, not a curve, and not something to "fix".</summary>
    private static readonly int[] WhispHealPower = { 250, 320, 390, 460, 530, 600, 670, 740 };

    /// <summary>Armor Break's two ladders, from his DESCR cells. M.Def is EXACTLY HALF P.Def at every
    /// rung — the same identity the Lightbringer's own Armor Break holds. Keep it if these move.</summary>
    private static readonly float[] WhispArmorPDef = { .10f, .12f, .14f, .16f, .18f, .22f, .26f, .30f };
    private static readonly float[] WhispArmorMDef = { .05f, .06f, .07f, .08f, .09f, .11f, .13f, .15f };

    /// <summary>Weapon Break's single ladder (`DebuffAtk` covers P.Atk and M.Atk at once), which is
    /// the same eight numbers as Armor Break's M.Def half.</summary>
    private static readonly float[] WhispWeaponAtk = { .05f, .06f, .07f, .08f, .09f, .11f, .13f, .15f };

    private static SkillDef[] WhispSkills() => new SkillDef[]
    {
        // ---- WHISP TAUNT. *"Agro enemy - locks it onto you for 1s then leaves you X aggro ahead,
        //      Power depends on whisp lvl"*. The lock is 1s (10 ticks) — a third of the tank's own
        //      1.5s Provoke — and the aggro number comes from the summon rung, like every other
        //      "Power depends on whisp lvl" row. Both halves land on the MASTER, never on the whisp:
        //      a spirit that could hold aggro itself would be a pet, and an untargetable one at that.
        new(WhispProvoke, "Whisp Taunt", BaseClass.Mage, SkillEffect.Taunt,
            MpCost: 0, CastTicks: 0, CooldownTicks: 100, Range: 400, Power: 0,
            DurationTicks: 10, TauntPower: WhispThreat[0],
            Category: SkillCategory.Debuff,
            Description: "The whisp shrieks at an enemy — it turns on your master for a moment, and "
                       + "carries the grudge afterwards.",
            // The rungs of the WHISP, not of the summon: a whisp reads everything it does at the level
            // of the call that raised it (*"Power depends on whisp lvl"*), so laddering it is done here
            // and the summon carries only the price.
            Levels: WhispThreat.Select(t => new SkillLevel(TauntPower: t)).ToArray()),

        // ---- WHISP CHARM. `BL-110`'s charm, and the first thing in the game to author one. His own
        //      pairing of the two halves applies here exactly as it does to the tank's Charm: the
        //      AGGRO IS UNCONDITIONAL and only the walk rolls (*"charm can fail the actual debuff
        //      … but still adds the points"*), which is why this carries a TauntPower as well.
        new(WhispCharm, "Whisp Charm", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 100, Range: 400, Power: 0,
            DurationTicks: 30, BuffKey: "charm", Rank: 1, TauntPower: WhispThreat[0], SharesLadderKey: true,
            Charms: true,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff,
            Description: "The whisp beguiles an enemy: for 3s it walks helplessly toward your master, "
                       + "and it remembers who called it.",
            // The SAME eight numbers as the taunting whisp's — his two blocks share a ladder cell for
            // cell, which is the arithmetic behind his *"charm also adds aggro points"*: the two whisps
            // are worth the same to a tank, and only their control differs.
            Levels: WhispThreat.Select(t => new SkillLevel(TauntPower: t)).ToArray()),

        // ---- WHISP BIND. *"Hold enemy for 5s"* — a plain Root, sharing the player family's key so a
        //      real Hold is never downgraded by a whisp's.
        new(WhispBind, "Whisp Bind", BaseClass.Mage, SkillEffect.Root,
            MpCost: 0, CastTicks: 10, CooldownTicks: 100, Range: 400, Power: 0,
            DurationTicks: 50, BuffKey: "root", Rank: 1,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff,
            Description: "The whisp pins an enemy to the ground for 5s."),

        // ---- WHISP ARMOR BREAK. His summon row states the numbers: *"Deacreases P.Def by 10% and
        //      MDef by 5%"* — which is precisely rung 1 of the Lightbringer's own Armor Break. That
        //      is the whole design of the shared key: the whisp gives you the WEAKEST rung of a real
        //      healer debuff, so a party with a healer loses nothing by the tank's whisp and a party
        //      without one gains the bottom of the ladder.
        //      ⚠ The sign convention is the healer's: P.Def rides DebuffDef POSITIVE (subtracted),
        //      M.Def rides BuffMagicDef NEGATIVE. Two flags, two conventions, one meaning.
        new(WhispArmorBreak, "Whisp Armor Break", BaseClass.Mage,
            SkillEffect.DebuffDef | SkillEffect.BuffMagicDef,
            MpCost: 0, CastTicks: 10, CooldownTicks: 100, Range: 400, Power: 0,
            DurationTicks: 150, BuffKey: "armor_break", Rank: 1, SharesLadderKey: true,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffDef, WhispArmorPDef[0]),
                new(SkillEffect.BuffMagicDef, -WhispArmorMDef[0]),
            },
            Description: "The whisp frays an enemy's guard for 15s: −10% P.Def, −5% M.Def.",
            Levels: Enumerable.Range(0, WhispArmorPDef.Length).Select(i => new SkillLevel(
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.DebuffDef, WhispArmorPDef[i]),
                    new(SkillEffect.BuffMagicDef, -WhispArmorMDef[i]),
                },
                Description: $"−{WhispArmorPDef[i] * 100:0}% P.Def and −{WhispArmorMDef[i] * 100:0}% M.Def for 15s."
            )).ToArray()),

        // ---- WHISP WEAPON BREAK. His summon row: *"Decreases P/M.Atk 5%"*. `DebuffAtk` is the one
        //      flag that covers both channels, so 5% is one number and not two.
        new(WhispWeaponBreak, "Whisp Weapon Break", BaseClass.Mage, SkillEffect.DebuffAtk,
            MpCost: 0, CastTicks: 10, CooldownTicks: 100, Range: 400, Power: 0,
            DurationTicks: 150, BuffKey: "weapon_break", Rank: 1, SharesLadderKey: true,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffAtk, WhispWeaponAtk[0]) },
            Description: "The whisp blunts an enemy's weapon and their magic alike for 15s: −5%.",
            Levels: WhispWeaponAtk.Select(v => new SkillLevel(
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffAtk, v) },
                Description: $"−{v * 100:0}% P.Atk and M.Atk for 15s."
            )).ToArray()),

        // ---- WHISP GRAVITY. *"Decrease enemy Atack speed and cast speed"*. NO SUMMON CALLS IT YET —
        //      his `tank 3rd.csv` PoC is six whisps and this is not one of them. It is authored
        //      because the row exists in his file and the file is the data; the day a class table
        //      names it, it works. ⚠ Do not invent the summoner.
        new(WhispGravity, "Whisp Gravity", BaseClass.Mage,
            SkillEffect.DebuffAtkSpeed | SkillEffect.DebuffCastSpeed,
            MpCost: 0, CastTicks: 10, CooldownTicks: 100, Range: 400, Power: 0,
            DurationTicks: 150, BuffKey: "gravity", Rank: 1,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffAtkSpeed, 0.07f), new(SkillEffect.DebuffCastSpeed, 0.07f),
            },
            Description: "The whisp weighs an enemy down for 15s: slower attacks and slower casting."),

        // ---- THE TWO HEALS, and the reason there are two. His conditions column splits them by the
        //      master's HP BAND and nothing else: Whisp Heal covers *"50~99%"* on a 20s reuse, Quick
        //      Heal covers *"lower than 50%"* on a 10s reuse and no cast time. So the same whisp tops
        //      you up lazily when you are fine and reacts twice as fast when you are not — which is
        //      why one whisp carries both ids rather than there being two healing whisps.
        //      Power 300 is his summon row (*"Call a whisp to heal its master (Power 300)"*).
        new(WhispHeal, "Whisp Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 0, CastTicks: 10, CooldownTicks: 200, Range: 0, Power: WhispHealPower[0],
            Category: SkillCategory.Heal,
            Description: "The whisp mends its master while he is still on his feet.",
            Levels: WhispHealPower.Select(p => new SkillLevel(Power: p)).ToArray()),

        // ⚠ THE SAME POWER LADDER AS ITS SLOW HALF, deliberately. His summon row states ONE power per
        // rung and names both ids, so the difference between the two gears is the CONDITION and the
        // reuse — a fast reaction below half health, a lazy top-up above it — never the size of the
        // heal. Giving the emergency one its own number would be inventing a design he did not write.
        new(WhispQuickHeal, "Whisp Quick Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 0, CastTicks: 0, CooldownTicks: 100, Range: 0, Power: WhispHealPower[0],
            Category: SkillCategory.Heal,
            Description: "The whisp throws its master a fast heal when he is badly hurt.",
            Levels: WhispHealPower.Select(p => new SkillLevel(Power: p)).ToArray()),

        // ---- WHISP CLEAR. *"Removes up to 2 debuffs from master"*, and its condition is that he has
        //      one. Like Gravity, no summon calls it yet.
        new(WhispClear, "Whisp Clear", BaseClass.Mage, SkillEffect.Cleanse,
            MpCost: 0, CastTicks: 0, CooldownTicks: 150, Range: 0, Power: 0,
            Category: SkillCategory.Heal,
            DispelCount: 2,
            Description: "The whisp scours up to two afflictions off its master."),
    };

    // =======================================================================================
    //  THE SUMMONS — the `Whisps` block of `tank 3rd.csv`, six rows at level 40, plus Whisp
    //  Mastery at 60. This is the PoC he asked for, and it is the ONLY part of that (still open)
    //  file built so far: the taunt / charm / mass-taunt / intimidate / freeze / stay ladders, the
    //  masteries and Defensive Wall all wait for the one-pass tank delta.
    //
    //  🔑 RACE SPLITS THE SIX INTO THREE PAIRS — his own column: Human takes taunt and bind, Elf
    //  charm and heal, Demon the two breaks. So a tank's whisp is the clearest thing his race
    //  changes about him, and with one slot until 60 it is a real choice rather than a checklist.
    //
    //  ⚠ EVERY ROW IS IDENTICAL BUT FOR ITS PAYLOAD AND ITS PRICE: 1s cast, 30s reuse, 20 minutes,
    //  and the same MP ladder 50 → 100. He wrote them as one block copied six times, and they are
    //  kept that way — a difference between two of these would be a number nobody authored.
    // =======================================================================================

    public const string TankWhispTaunt       = "tank_whisp_taunt";
    public const string TankWhispBind        = "tank_whisp_bind";
    public const string TankWhispCharm       = "tank_whisp_charm";
    public const string TankWhispHeal        = "tank_whisp_heal";
    public const string TankWhispArmorBreak  = "tank_whisp_armor_break";
    public const string TankWhispWeaponBreak = "tank_whisp_weapon_break";
    public const string TankWhispMastery     = "tank_whisp_mastery";

    /// <summary>His six summon rows and the mastery. A summon is an ordinary self-cast skill whose
    /// entire payload is <see cref="SkillDef.SummonsWhisp"/> — no buff, no damage, no target.</summary>
    private static SkillDef[] WhispSummonSkills() => new SkillDef[]
    {
        WhispSummon(TankWhispTaunt,       "Taunting Whisp",        WhispProvoke,     WhispSpA,
            "Calls a whisp that shrieks your enemies onto you."),
        WhispSummon(TankWhispCharm,       "Charming Whisp",        WhispCharm,       WhispSpA,
            "Calls a whisp that lures your enemies to you."),
        WhispSummon(TankWhispArmorBreak,  "Armor Breaking Whisp",  WhispArmorBreak,  WhispSpA,
            "Calls a whisp that frays your enemies' guard."),

        WhispSummon(TankWhispBind,        "Binding Whisp",         WhispBind,        WhispSpB,
            "Calls a whisp that pins your enemies where they stand."),
        WhispSummon(TankWhispWeaponBreak, "Weapon Breaking Whisp", WhispWeaponBreak, WhispSpB,
            "Calls a whisp that blunts your enemies' weapons."),

        // The healer whisp is the one row carrying TWO ids — his comment cell says so in as many
        // words (*"uses whisp_heal and whisp_quick_heal"*). They are one whisp with two gears, split
        // by the master's HP band; see the two defs above.
        WhispSummon(TankWhispHeal,        "Healing Whisp",         WhispHeal,        WhispSpB,
            "Calls a whisp that tends its master's wounds.", second: WhispQuickHeal),

        // ---- WHISP MASTERY. *"Increase the limit of active whisps to 2"*. One rung today; the base
        //      is 1 and this adds 1. His design has a third slot behind a later rung — when it is
        //      authored it is a second SkillLevel carrying WhispSlots: 2, and nothing else changes.
        new(TankWhispMastery, "Whisp Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 120_000,
            Passive: new PassiveEffect(WhispSlots: 1),
            Description: "You may keep two whisps at once."),
    };

    /// <summary>One summon LADDER — eight rungs, and the only thing that differs between the six is
    /// which whisp it calls and which of the two SP columns it is priced on. The cast, the reuse, the
    /// duration and the MP ladder are shared, which is exactly how he wrote them.</summary>
    private static SkillDef WhispSummon(string id, string name, string whispSkill, int[] sp,
                                        string description, string? second = null)
    {
        var ids = second is null ? new[] { whispSkill } : new[] { whispSkill, second };
        return new(id, name, BaseClass.Fighter, SkillEffect.None,
            MpCost: WhispMp[0], CastTicks: 10, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 12000,          // 1200s = the 20 minutes a whisp lasts
            Category: SkillCategory.Buff,  // it is not a buff, but it is cast at yourself and leaves
                                           // something that expires — the closest existing category,
                                           // and what keeps it out of the offensive target checks
            TargetMode: TargetMode.SelfOnly,
            SpCost: sp[0],
            SummonsWhisp: ids,
            Description: description,
            Levels: Enumerable.Range(0, WhispMp.Length)
                .Select(i => new SkillLevel(MpCost: WhispMp[i], SpCost: sp[i]))
                .ToArray());
    }
}
