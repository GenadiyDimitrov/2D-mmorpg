namespace Game.Shared;

/// <summary>
/// `BL-314` — THE SHARED PASSIVE LADDERS, the hand-written half. The ladders themselves are GENERATED from the class
/// CSVs into <c>Skills.PassiveLadders.g.cs</c> (and every class's learn rows into
/// <c>ClassSkillTables.Passives.g.cs</c>) by <c>dotnet run --project tools/SkillCsvSeed -- --gen-passives</c>; this file
/// holds the builders they call and the one piece that is not a stat.
///
/// <para>🔑 <b>ONE PIECE = ONE STAT, ONE LADDER PER STAT</b> (his 2026-09-29 split, `docs/design/PassiveSplit.md`
/// §10-§12). The old armour and weapon bundles are gone: `light_armor_mastery` is the light-armour P.Def of EVERY class
/// that wears light, from the fighter's +9 at 5 to the dagger's +155 at 90, and each class learns only its own rungs of
/// it (<see cref="ClassSkills.NextLearnableLevel"/> already skips the rungs that are other classes'). A rung exists
/// only where some class's number moves, so the ladder is the union of every value authored for that stat. Nothing
/// needs <c>Replaces</c> any more: a 3rd class simply continues the ladder its 2nd class started.</para>
///
/// <para>🔑 <b>EACH PIECE KEEPS THE ENGINE CHANNEL ITS BUNDLE USED.</b> An armour piece (P.Def, evasion, regen, cast
/// speed, M.Def %…) is an <see cref="ArmorMasteryProfile"/>, folded in by the armour-mastery pass, so its percentages
/// compose exactly as they did inside the bundle; a weapon piece is a <see cref="WeaponMasteryProfile"/>; the rest are a
/// plain <see cref="PassiveEffect"/> gated by <c>RequiredArmor</c>. An ungated armour piece fills all four weights.</para>
/// </summary>
public static partial class SkillCatalog
{
    /// <summary>One rung of a generated ladder, as plain numbers: its stat values in the ladder's own order, its ×1
    /// price and gold, his words, and its gate. ⚠ A CLASS, deliberately: the generated ladders hold 756 of these,
    /// and as structs carrying a whole PassiveEffect each they overflowed the stack of the one method that built
    /// them (IL2CPP would have done it on the phone too). The builder lambda makes the stat struct per rung.</summary>
    private sealed record RungRow(float[] S, int Sp, int Gold, string Text,
                                  ArmorWeights Armor = ArmorWeights.None,
                                  WeaponType Weapon = WeaponType.None, WeaponHands Hands = WeaponHands.Any);

    /// <summary>An ARMOUR-channel ladder: each rung's StatMods in every weight its gate names, or in all four (bare
    /// included) when it has none.</summary>
    private static SkillDef ArmorLadder(string id, string name, BaseClass cls, string description,
                                        Func<RungRow, StatMods> make, params RungRow[] rows) =>
        LadderDef(id, name, cls, description, rows, armor: rows.Select(r =>
        {
            var m = make(r);
            bool On(ArmorWeights x) => r.Armor == ArmorWeights.None || (r.Armor & x) == x;
            return new ArmorMasteryProfile(
                Robe:  On(ArmorWeights.Robe)  ? m : default,
                Light: On(ArmorWeights.Light) ? m : default,
                Heavy: On(ArmorWeights.Heavy) ? m : default,
                None:  On(ArmorWeights.Bare)  ? m : default);
        }).ToArray());

    /// <summary>A plain-passive ladder, each rung gated by armour weight (None = ungated).</summary>
    private static SkillDef PlainLadder(string id, string name, BaseClass cls, string description,
                                        Func<RungRow, PassiveEffect> make, params RungRow[] rows) =>
        LadderDef(id, name, cls, description, rows, passives: rows.Select(r => make(r) with { RequiredArmor = r.Armor }).ToArray());

    /// <summary>A weapon ladder: each rung's effect in every slot, gated by its WEAPON cell. An ungated rung
    /// (WeaponType.None — strength_mastery's first) fills the empty-hand slot too, so it pays with anything.</summary>
    private static SkillDef WeaponLadder(string id, string name, BaseClass cls, string description,
                                         Func<RungRow, PassiveEffect> make, params RungRow[] rows) =>
        WeaponLadder(id, name, cls, description, make, false, rows);

    /// <param name="payHighestGatedRung">`strength_mastery`: rung 1 (the fighter's ×1.085) has no weapon gate and the
    /// warrior's rungs above it need a two-handed sword or blunt. His rule for it: a passive pays its HIGHEST LEARNED
    /// RUNG WHOSE GATE HOLDS, so a warrior holding a bow keeps the ×1.085 and never gets the +30%. See
    /// <see cref="SkillDef.PayHighestGatedRung"/>.</param>
    private static SkillDef WeaponLadder(string id, string name, BaseClass cls, string description,
                                         Func<RungRow, PassiveEffect> make, bool payHighestGatedRung, params RungRow[] rows) =>
        LadderDef(id, name, cls, description, rows, payHighestGatedRung: payHighestGatedRung, weapon: rows.Select(r =>
        {
            var pe = make(r);
            return new WeaponMasteryProfile(Sword: pe, Blunt: pe, Dual: pe, Bow: pe,
                                            Other: r.Weapon == WeaponType.None ? pe : default,
                                            RequiredWeapon: r.Weapon, RequiredHands: r.Hands);
        }).ToArray());

    private static SkillDef LadderDef(string id, string name, BaseClass cls, string description, RungRow[] rows,
                                      ArmorMasteryProfile[]? armor = null, PassiveEffect[]? passives = null,
                                      WeaponMasteryProfile[]? weapon = null, bool payHighestGatedRung = false) =>
        new(id, name, cls, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            SpCost: rows[0].Sp,
            Description: description,
            Levels: rows.Select((r, i) => new SkillLevel(SpCost: r.Sp, GoldCost: r.Gold, Description: r.Text,
                                                         Passive: passives?[i])).ToArray(),
            ArmorMasteryLevels: armor,
            WeaponMasteryLevels: weapon,
            PayHighestGatedRung: payHighestGatedRung);

    // ═══ DUAL PROFICIENCY — the one piece that is not a stat ════════════════════════════════════════════════════════
    // His `dual 3rd.csv` @40: *"With 3% chance to decrease skill mp consumption with 60% and increase crit.dmg with 10%
    // for 5 sec"*, CD 8 / DURATION 5 = the proc's internal cooldown and the buff's life. It was the proc half of the old
    // `dual_weapon_mastery` bundle and keeps that bundle's payload (`dual_mastery_rush`, Skills.Dual3rd.cs) unchanged.

    private static SkillDef[] PassiveLadderHandSkills() => new[]
    {
        new SkillDef(DualWeaponProf, "Dual Proficiency", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            RequiredWeapon: WeaponType.Dual,
            ProcChance: 0.03f, ProcCooldownTicks: 80,
            ProcSelfRungs: new[] { DualMasteryRush },
            Description: "Passive. With duals: a 3% chance on attack to find the rhythm — for 5s your physical "
                       + "skills cost 60% less MP and your critical hits land 10% harder.",
            Levels: new[] { new SkillLevel(SpCost: 9300, Description: "With duals: 3% chance, 60% less MP and +10% crit damage for 5s (8s cooldown).") }),
    };

    /// <summary>Every shared ladder, generated and hand-written.</summary>
    private static IEnumerable<SkillDef> SharedPassiveSkills() => PassiveLadderSkills().Concat(PassiveLadderHandSkills());
}
