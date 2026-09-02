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
        // ✅ TWO disciplines have a finished 4th CSV. The Lightbringer since 2026-08-26; the
        //    WARCHANTER since 2026-09-02, when he called `buffer 4th.csv` done (`BL-108`).
        //    The other eight files are still two lines long — the 40+ rule stands for them.
        RegisterLightbringerFourth();
        RegisterWarchanterFourth();
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
                  .Concat(At(LifeMark, (78, 1), (83, 2)))
                  .Concat(new[] { new ClassSkill(LifeRestoration, 83) })
                  .ToArray());

        ClassSkills.RegisterFourth(Race.Elf, Discipline.Lightbringer,
            shared.Concat(Ladder(LbElfDawn, HealerFourthBands, 15))
                  .Concat(Ladder(LbElfBind, HealerFourthEven,  15))
                  .Concat(Ladder(HealerPartyBlessing, new[] { 83, 84, 85, 86, 87, 88, 89, 90 }, 1))
                  .Concat(At(HolyMark, (78, 1), (83, 2)))
                  .Concat(new[] { new ClassSkill(ElvenRestoration, 83) })
                  .ToArray());

        ClassSkills.RegisterFourth(Race.Demon, Discipline.Lightbringer,
            shared.Concat(Ladder(LbOrkFont,       HealerFourthBands, 15))
                  .Concat(Ladder(LbOrkArmorBreak, HealerFourthEven,  15))
                  .Concat(At(BloodMark, (78, 1), (83, 2)))
                  .Concat(new[] { new ClassSkill(SpiritRestoration, 83) })
                  .ToArray());
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE WARCHANTER, 76-90 — `docs/data/classes_skills_csv/buffer 4th.csv` (`BL-108`)
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>The buffer's 4th tier. Same two band shapes as the healer's — every level for a full
    /// ladder, every OTHER level for the sparse ones — and the same price ladder, because both files
    /// carry the identical header.
    ///
    /// <para>🔑 <b>THE START RUNGS ARE WHERE THE 3rd-CLASS LADDERS ACTUALLY ENDED</b>, not guesses, and
    /// getting one wrong is the only real hazard in this table: a <see cref="ClassSkill"/> names the
    /// RUNG, so an off-by-one sells a level-76 buffer his level-74 numbers for 6.5kk SP. Anti-Magic
    /// reached 20 (six cleric rungs + the buffer's fourteen), Armor Mastery 14 on its own id, Spell
    /// Mastery 18 (four cleric rungs + fourteen), Harmony of Restoration 14, the three Sound skills
    /// and both toggles 13, the three weapon masteries 8, and the two groups exactly 1.</para>
    ///
    /// <para>🔑 <b>THE RACE SPLIT IS THE 3rd TIER'S, CONTINUED.</b> Human blunt + shield, Elf bow,
    /// Demon blunt with two damage skills — and each race's weapon mastery continues its own ladder.
    /// The two things genuinely new at this tier are <see cref="SkillCatalog.BufferShieldMastery"/>
    /// (robe AND shield, so only the Human ever benefits) and the three harmonies.</para>
    ///
    /// <para>⚠ <b>HARMONY OF SPEED STILL STOPS AT 58</b> and Harmony of the Warrior at 74. Neither has
    /// a row in his 4th file, and that is his ruling both times — do not invent continuations.</para></summary>
    private static void RegisterWarchanterFourth()
    {
        ClassSkill[] Ladder(string skill, int[] bands, int startLevel) =>
            bands.Select((lvl, i) => new ClassSkill(skill, lvl, SkillLevel: startLevel + i)).ToArray();

        ClassSkill[] At(string skill, params (int Level, int Rung)[] rows) =>
            rows.Select(r => new ClassSkill(skill, r.Level, SkillLevel: r.Rung)).ToArray();

        int[] all  = BufferFourthBands;   // 76…90
        int[] even = BufferFourthEven;    // 76, 78 … 90

        var shared = new List<ClassSkill>();

        // ---- THE CONTINUING LADDERS, every level ----
        shared.AddRange(Ladder(MageAntiMagic,         all, 21));
        shared.AddRange(Ladder(BufferArmorMastery,    all, 15));
        shared.AddRange(Ladder(SpellMastery,          all, 19));
        shared.AddRange(Ladder(WcHarmonyRestoration,  all, 15));
        // ---- …and every other level ----
        shared.AddRange(Ladder(WcReinforcement,       even, 14));
        shared.AddRange(Ladder(WcSharpening,          even, 14));
        shared.AddRange(Ladder(WcSoulReinforce,       even, 2));
        shared.AddRange(Ladder(WcArcaneFeralProt,     even, 2));

        // ---- THE HARMONIES. Protection gains a sixth rung; the Wizard's three continue a ladder his
        //      3rd-class file deliberately stopped at 52; Soul and Madness are new skills. ----
        shared.AddRange(At(NpcHarmonyProtection, (76, 6)));
        shared.AddRange(At(NpcHarmonyWizard,     (77, 3), (78, 4), (79, 5)));
        shared.AddRange(Ladder(WcHarmonySoul, new[] { 77, 78, 79, 80, 81, 82, 83 }, 1));
        shared.Add(new ClassSkill(WcHarmonyMadness, 83));
        // Harmony Mark: 79, then its second rung at 83 — the only two it has.
        shared.AddRange(At(WcHarmonyMark, (79, 1), (83, 2)));

        // ---- HUMAN: the shield buffer. His own blunt line, and the robe+shield passive nobody else
        //      can satisfy — the elf holds a bow and the demon a two-handed maul. ----
        var human = new List<ClassSkill>(shared);
        human.Add(new ClassSkill(BufferShieldMastery, 76));
        human.AddRange(Ladder(DoctorBluntMastery, even, 9));
        human.AddRange(Ladder(WcSoundSmash,       all,  14));

        // ---- ELF: the archer. ----
        var elf = new List<ClassSkill>(shared);
        elf.AddRange(Ladder(WcHarmonistBowMast, even, 9));
        elf.AddRange(Ladder(WcSoundBurst,       all,  14));

        // ---- DEMON: the melee fighter, two damage skills as always. ----
        var demon = new List<ClassSkill>(shared);
        demon.AddRange(Ladder(WcWarlockWeapon,  even, 9));
        demon.AddRange(Ladder(WcSoundSmash,     all,  14));
        demon.AddRange(Ladder(WcAcousticShock,  all,  14));

        ClassSkills.RegisterFourth(Race.Human, Discipline.Warchanter, human.ToArray());
        ClassSkills.RegisterFourth(Race.Elf,   Discipline.Warchanter, elf.ToArray());
        ClassSkills.RegisterFourth(Race.Demon, Discipline.Warchanter, demon.ToArray());
    }
}
