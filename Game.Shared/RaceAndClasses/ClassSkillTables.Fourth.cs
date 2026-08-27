namespace Game.Shared;

using static Game.Shared.SkillCatalog;

/// <summary>
/// 4th-class (ASCENDED discipline) learn tables — the 76-90 band.
///
/// <para><b>Nothing here reaches a character who has not ASCENDED.</b> A 4th class is the same
/// discipline under a new name (<see cref="FourthClassDef"/>), so these rows are registered against a
/// key that carries a TIER (<c>ClassSkills.ClassKey.Fourth</c>) and are only unioned into
/// <c>Cumulative</c> when the character has paid the 100kk Rite of Ascension. That tier is what this
/// whole file was waiting on since 0.70.0 — the note in Classes.Fourth.cs said the kit could not exist
/// until <c>ClassKey</c> grew one, and now it has.</para>
///
/// <para><b>Two blocks:</b>
/// <list type="bullet">
///   <item><b>The SHARED kit</b> — his `shared 4th.csv`, every class, five passives plus the eighteen
///         sigils. Registered ONCE via <c>ClassSkills.RegisterFourthShared</c>; the sigils are injected
///         from the catalog in <c>Cumulative</c> (they are a fixed grid and have their own tab).</item>
///   <item><b>The per-discipline kits</b> — one `*.4th.csv` each. Today exactly ONE is authored: the
///         LIGHTBRINGER, off `healer 4th.csv` (255 rows, he calls it finished). ⚠ The 40+ rule still
///         stands for the other nine: *"Anything that's not inside the csv should not exist"* — do not
///         invent a 4th kit for a discipline whose file is a placeholder.</item>
/// </list></para>
/// </summary>
public static partial class ClassSkillTables
{
    static partial void RegisterFourthClasses()
    {
        RegisterSharedFourth();
        // ✅ The only discipline with a finished 4th CSV. `buffer 4th.csv` was still in progress on
        //    2026-08-26; the other eight files are two lines long.
        RegisterLightbringerFourth();
    }

    /// <summary>His `shared 4th.csv` ALL-CLASSES block. Five passives, two price bands, no race split
    /// and no class split — every ascended character is offered all five.</summary>
    private static void RegisterSharedFourth()
    {
        ClassSkills.RegisterFourthShared(
            new ClassSkill(StrongBody, 76),
            new ClassSkill(StrongMind, 76),
            new ClassSkill(ArcaneProtection, 83),
            new ClassSkill(MagicProficiency, 83),
            new ClassSkill(PhysicalProficiency, 83));
        // ⚠ The eighteen SIGILS are NOT listed here. They come out of `SkillCatalog.AllSigilIds` in
        //   ClassSkills.Cumulative, for the same reason the level-40 stat swaps do: a fixed grid with
        //   its own purchase surface is data, not eighteen hand-written learn lines that can drift.
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE LIGHTBRINGER, 76-90 — `docs/data/classes_skills_csv/healer 4th.csv`
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>His 4th-tier bands. Unlike the 3rd class's fourteen irregular steps, the 4th tier is
    /// EVERY LEVEL from 76 to 90 for a full ladder — fifteen rungs — and every SECOND level for the
    /// ladders he authored sparsely. Both shapes are read off the file rather than derived.</summary>
    internal static readonly int[] HealerFourthBands =
        { 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90 };

    /// <summary>The every-other-level shape: 76, 78, 80 … 90. Eight rungs. His Ultimate Heal, Mana
    /// Ray, Mana Strain, Weapon Break, Gravity/Bind/Armor Break, Mana Blessing and Fortitude.</summary>
    internal static readonly int[] HealerFourthEven =
        { 76, 78, 80, 82, 84, 86, 88, 90 };

    private static void RegisterLightbringerFourth()
    {
        // A ladder over one of the two band shapes, continuing an existing skill from
        // `startLevel` (the rung AFTER the 3rd class's last one).
        ClassSkill[] Ladder(string skill, int[] bands, int startLevel) =>
            bands.Select((lvl, i) => new ClassSkill(skill, lvl, SkillLevel: startLevel + i)).ToArray();

        // Explicit (character level, rung) rows, for the ladders that are neither shape.
        ClassSkill[] At(string skill, params (int Level, int Rung)[] rows) =>
            rows.Select(r => new ClassSkill(skill, r.Level, SkillLevel: r.Rung)).ToArray();

        var shared = new List<ClassSkill>();

        // ═══ THE CONTINUING LADDERS ══════════════════════════════════════════════════════════════
        // Each of these already runs to 74 on the 3rd-class table; the numbers below are simply the
        // next rung. ⚠ The START LEVELS are where 3rd-class ladders actually ENDED, not guesses —
        // Anti-Magic reached 20 (it began at rung 7 in the cleric's table), the two masteries and the
        // three ordinary heals reached 14, Resurrection reached 16.
        shared.AddRange(Ladder(MageAntiMagic,             HealerFourthBands, 21));
        shared.AddRange(Ladder(HealerWeaponMasterySkill,  HealerFourthBands, 15));
        shared.AddRange(Ladder(HealerArmorMasterySkill,   HealerFourthBands, 15));
        shared.AddRange(Ladder(HolyRay,                   HealerFourthBands, 15));
        shared.AddRange(Ladder(GreatHeal,                 HealerFourthBands, 15));
        shared.AddRange(Ladder(PartyGreatHeal,            HealerFourthBands, 15));
        shared.AddRange(Ladder(UltimateHeal,              HealerFourthEven,  10));
        shared.AddRange(Ladder(UltimatePartyHeal,         HealerFourthEven,  10));
        shared.AddRange(Ladder(ManaRay,                   HealerFourthEven,  11));
        shared.AddRange(Ladder(ManaStrain,                HealerFourthEven,  12));
        shared.AddRange(Ladder(WeaponBreak,               HealerFourthEven,   5));
        shared.AddRange(Ladder(ManaBlessing,              HealerFourthEven,   4));
        // Fortitude is a rung of the SHARED CC-resist family, not a skill of its own — the same rule
        // as every buff row on the 3rd-class table. Rungs 5-12 are his 43% → 65%.
        shared.AddRange(Ladder(CastId(FamCcResPhys),      HealerFourthEven,   5));
        // Resurrection and its field: two rungs each, at 76 and 80, and then they stop for good.
        shared.AddRange(At(Resurrection,      (76, 17), (80, 18)));
        shared.AddRange(At(ResurrectionField, (76, 5),  (80, 6)));
        // Antidote gets exactly ONE more rung and never another: it cures "rank 10 or lower", and 10
        // is the top rank there is.
        shared.AddRange(At(Antidote, (76, 10)));

        // ═══ NEW AT THE 4th TIER ═════════════════════════════════════════════════════════════════
        shared.Add(new ClassSkill(HealerShieldMastery, 76));
        shared.Add(new ClassSkill(ArcaneResistance,    76));
        shared.Add(new ClassSkill(HolyBlessing,        78));
        shared.Add(new ClassSkill(HolySoul,            76));
        shared.Add(new ClassSkill(RiteOfPreservation,  83));
        // Urgent Great Heal @83 — SHARED, not race-split: his row carries no RACE, so all three learn
        // it. It REPLACES Urgent Heal (the SkillDef says so), which is why the 3rd tier's four rungs
        // stop at 56 and never gain a fifth.
        shared.Add(new ClassSkill(UrgentGreatHeal,     83));
        // Healer's Power: five rungs on his own irregular levels.
        shared.AddRange(At(HealersPower, (80, 1), (83, 2), (85, 3), (87, 4), (90, 5)));

        // ═══ THE RACE SPLIT ══════════════════════════════════════════════════════════════════════
        // The same two places it splits at the 3rd tier — the fast heal and the control debuff — plus
        // two new ones the 4th tier adds: the RESTORATION ultimate (83) and the MARK blessing (78),
        // which are three different skills rather than three rungs of one, because their payloads have
        // nothing in common beyond the level they arrive at.
        //
        // ⚠ The Elf also gains a fourth: Healer PARTY Blessing, 83-90, which the other two races have
        // no equivalent of. That asymmetry is his (`healer 4th.csv`), not an omission here.
        ClassSkills.RegisterFourth(Race.Human, Discipline.Lightbringer,
            shared.Concat(Ladder(LbHumanMend,    HealerFourthBands, 15))
                  .Concat(Ladder(LbHumanGravity, HealerFourthEven,  15))
                  .Concat(new[] { new ClassSkill(LifeRestoration, 83), new ClassSkill(LifeMark, 78) })
                  .ToArray());

        ClassSkills.RegisterFourth(Race.Elf, Discipline.Lightbringer,
            shared.Concat(Ladder(LbElfDawn, HealerFourthBands, 15))
                  .Concat(Ladder(LbElfBind, HealerFourthEven,  15))
                  .Concat(Ladder(HealerPartyBlessing, new[] { 83, 84, 85, 86, 87, 88, 89, 90 }, 1))
                  .Concat(new[] { new ClassSkill(ElvenRestoration, 83), new ClassSkill(HolyMark, 78) })
                  .ToArray());

        ClassSkills.RegisterFourth(Race.Ork, Discipline.Lightbringer,
            shared.Concat(Ladder(LbOrkFont,       HealerFourthBands, 15))
                  .Concat(Ladder(LbOrkArmorBreak, HealerFourthEven,  15))
                  .Concat(new[] { new ClassSkill(SpiritRestoration, 83), new ClassSkill(BloodMark, 78) })
                  .ToArray());
    }
}
