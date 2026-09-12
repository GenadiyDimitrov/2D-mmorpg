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
        // ✅ THE THIRD FINISHED FILE, 2026-09-04 — `tank 4th.csv`, 205 rows, his *"im done with tank
        //   2/3/4"*. It was three placeholder rows until this pass. See RegisterBulwarkFourth and
        //   Skills.Bulwark4th.cs.
        RegisterBulwarkFourth();
        // ✅ THE FOURTH FINISHED FILE, 2026-09-09 — `archer 4th.csv`, his *"archer 4th done as well"*
        //   plus the Twin Arrows dip he fixed the moment it was reported. See RegisterArcherFourth
        //   and Skills.Archer4th.cs.
        RegisterArcherFourth();
        // 🔴 THE MELEE ROGUE IS A PARTIAL: `dual 4th.csv` is STILL the two-line placeholder, and this
        //   registers THREE skills only — the top of `BL-188`'s blow ladder, which he ruled outright
        //   on 2026-09-09 and asked to be built with the rest of it. Everything else the discipline
        //   will own waits on his file, exactly as before. See RegisterDual4th / Skills.Dual4th.cs.
        RegisterDual4th();
        // ✅ `BL-191` (2026-09-10) — the four SKILL MASTERIES he authored the day `BL-190` shipped.
        //    Like RegisterDual4th above it, this is a PARTIAL that crosses unfinished files: the
        //    warrior's two 4th CSVs are still placeholders. What is registered here is exactly what he
        //    ruled and nothing around it.
        //    ⚠ The nuker's file is FINISHED now (see below), so the Magus's Arcane Momentum rung is no
        //      longer a partial crossing an unfinished file — but it still lives there, beside its
        //      three siblings, because the four masteries are one layer and one ruling.
        RegisterSkillMasteriesFourth();
        // ✅ THE FIFTH FINISHED FILE, 2026-09-11 — `nuker 4th.csv`, 236 rows, his *"so i think im done
        //    with nuker 4th"*. `BL-192`. ONE discipline: the Magus, because the Tempest was retired
        //    (`BL-97`) and the race is what splits this kit, exactly as it does at the 3rd tier.
        RegisterMagusFourth();
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE SKILL MASTERIES, 76 — `BL-191`. See Skills.SkillMasteries.cs for the numbers.
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>The 76 rungs of the four mastery passives, per his 2026-09-10 ruling.
    ///
    /// <para>🔑 <b>FOUR ARCHETYPES, FOUR DIFFERENT ANSWERS</b>, and the split is the design:
    /// <list type="bullet">
    ///   <item><b>Ravager + Warlord</b> — Overpower rung 3 (10%) at 76 AND the `double_mastery`
    ///         toggle ("Overpower Mastery") at <b>81</b> (his own level, written into both warrior
    ///         CSVs 2026-09-10). The only line in the game that can double a skill's damage, and the
    ///         only tool that doubles the doubler.</item>
    ///   <item><b>Lightbringer + Warchanter</b> — Lasting Enchantment (10%): the buff/debuff duration
    ///         double, which until `BL-190` was free and universal.</item>
    ///   <item><b>Magus</b> — `reuse_reset_momentum` ("Arcane Momentum", 5%). *"Mages"* is the NUKER
    ///         in his vocabulary; the healer and buffer got a different passive in the line above.</item>
    ///   <item><b>Nullblade + Phantom + Venomweaver</b> (the MELEE rogue, added 2026-09-10) — the
    ///         SAME two ids the Magus and the warrior carry, under the names <b>"Stab Momentum"</b>
    ///         and <b>"Momentum Mastery"</b>. See the block at the bottom of the method.</item>
    /// </list></para>
    ///
    /// <para>🔴🔑 <b>`BL-213`, 2026-09-12 — HE REWROTE THE WHOLE ROSTER AFTER A PLAYTEST, AND IT IS
    /// A TABLE NOW, NOT A LIST OF EXCEPTIONS.</b> His words, verbatim and complete:
    /// <code>- mages -&gt; duration/reuse, no toggle mastery
    /// - archers/warriors/duals -&gt; double_dmg/reuse + toggle mastery
    ///     (archers and duals take 1 rung lower than warriors on the double one (40,76))
    /// - buffer/healer -&gt; duration no toggle
    /// - tank -&gt; nothing</code>
    /// which lands as:
    /// <list type="bullet">
    ///   <item><b>Magus</b> — reuse (had it) <b>+ duration, NEW at 76</b>. No toggle, by name.</item>
    ///   <item><b>Ravager + Warlord</b> — Overpower 3/7/10% at 20/40/76 and the toggle at 81 (had both)
    ///         <b>+ reuse, NEW at 76</b>, which is also his separate line *"Warriors can get the reuse
    ///         passive as well"*.</item>
    ///   <item><b>Nullblade + Phantom + Venomweaver</b> — unchanged: Overpower 3/7% at 40/76
    ///         (`BL-203`), reuse at 76, toggle at 81. His "(40,76)" clause is a DESCRIPTION of this
    ///         class, written so the archer could be told to match it.</item>
    ///   <item><b>Sharpshooter + Trapper + Hunter</b> — <b>all four, NEW</b>: Overpower 3/7% at 40/76,
    ///         reuse at 76, toggle at 81. *"archers must get the same double passives and overpower
    ///         mastery as duals"*.</item>
    ///   <item><b>Lightbringer + Warchanter</b> — unchanged: duration at 76, no toggle.</item>
    ///   <item><b>Bulwark</b> — unchanged: nothing, and it is the only line of the old ruling that
    ///         survived. *"if i give a tank some of the passives he will become even more
    ///         unstopable"*, and the 2026-09-12 playtest re-confirmed it in the same breath
    ///         (*"tanks for now seem OK"*).</item>
    /// </list></para>
    ///
    /// <para>🔴🔑 <b>WHAT THIS REVERSED, AND WHY IT IS WORTH KNOWING.</b> `BL-191` ruled on
    /// 2026-09-10 that the ARCHER gets a mastery <i>never</i>, with a reason: *"archer have enough
    /// skills that are always hit wit big power (not like daggers 80% chance)"* — a double would be
    /// compensation for a reliability problem he does not have. I ASKED before writing that down,
    /// because the sentence could be read either way, and he confirmed the no. Two days of play
    /// reversed it outright: *"archers are like mages ...Strongest skill does only 5k dmg to elit ...
    /// archers must get the same double passives and overpower mastery as duals"*. 🔑 The lesson is
    /// not that the first answer was wrong — it is that a RELIABILITY argument does not settle a
    /// DAMAGE question, and this layer was declared provisional (*"ill try with those changes and
    /// after playtest ill deside if i add or remove"*) precisely so it could move.</para>
    ///
    /// <para>⚠ <b>TWO DISPLAY NAMES IN THIS BLOCK ARE MINE, NOT HIS</b> — the only unauthored thing
    /// here. `reuse_reset_momentum` is one def with one number and a per-class name (his own rename
    /// rationale: *"as other classes can aqure it too"*), so the warrior's and the archer's copies each
    /// need one. I took them from each file's own vocabulary — <b>"Battle Momentum"</b> beside Battle
    /// Regeneration / Battle Resilience / the Battle stances, and <b>"Bow Momentum"</b> beside Bow
    /// Blessing / Bow Spirit / Bow Focus / Bow Stance. Both are one line and one CSV cell to change.
    /// The MECHANIC is never renamed; only the label is.</para>
    ///
    /// <para>🟡 <b>THE WHOLE MASTERY LAYER IS PROVISIONAL BY HIS OWN WORDS</b> — *"ill try with those
    /// changes and after playtest ill deside if i add or remove"*. Do not build on it as though it
    /// were settled; do not extend it either.</para>
    ///
    /// <para>✅ Every level in this table is now a number he wrote — Blood Rage's 81 was the last
    /// assumption and he replaced it himself on 2026-09-10.</para></summary>
    private static void RegisterSkillMasteriesFourth()
    {
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
        {
            // THE WARRIORS — Overpower's top rung, the toggle, and (`BL-213`) the reuse passive.
            foreach (var d in new[] { Discipline.Ravager, Discipline.Warlord })
                ClassSkills.RegisterFourth(race, d,
                    new ClassSkill(Overpower, 76, SkillLevel: 3),
                    new ClassSkill(ReuseResetMomentum, 76, "Battle Momentum", SkillLevel: 1),
                    new ClassSkill(DoubleMastery, 81, SkillLevel: 1));

            // HEALER + BUFFER — duration only. The one archetype `BL-213` did not move.
            foreach (var d in new[] { Discipline.Lightbringer, Discipline.Warchanter })
                ClassSkills.RegisterFourth(race, d,
                    new ClassSkill(LastingEnchantment, 76, SkillLevel: 1));

            // THE MAGUS — reuse, and (`BL-213`) duration beside it. *"mages -> duration/reuse"*, and
            // *"Mages also must take the double duration passive"* earlier in the same message.
            // 🔑 WHAT A DURATION DOUBLE IS WORTH TO A NUKER, since it is not obvious: it covers BOTH
            //    signs (*"doubles duration of bad and good buffs"*), so on this class it is mostly his
            //    DEBUFFS — the Frost pair, Witches Curse, the Scarecrow — plus his own stances. Same
            //    def and the same 10% the healer and buffer carry; nothing is authored twice.
            ClassSkills.RegisterFourth(race, Discipline.Magus,
                new ClassSkill(ReuseResetMomentum, 76, SkillLevel: 1),
                new ClassSkill(LastingEnchantment, 76, SkillLevel: 1));

            // THE ARCHER — `BL-213`, all of it new: *"archers must get the same double passives and
            // overpower mastery as duals"*. Overpower's two rungs live on the archer's own 3rd/4th
            // tables beside the rest of his kit (RegisterArcher3rd / RegisterArcherFourth), exactly
            // where the melee rogue's two sit; the reuse passive and the toggle are here.
            // ⚠ "Bow Momentum" is MY label, mirroring the melee rogue's "Stab Momentum" — see the
            //   summary above. The toggle keeps its own name, "Overpower Mastery", on his 2026-09-11
            //   correction for the dagger: a DisplayName is for when the flavour genuinely differs, and
            //   the stance does the same thing for an archer that it does for a warrior.
            foreach (var d in new[] { Discipline.Sharpshooter, Discipline.Trapper, Discipline.Hunter })
                ClassSkills.RegisterFourth(race, d,
                    new ClassSkill(ReuseResetMomentum, 76, "Bow Momentum", SkillLevel: 1),
                    new ClassSkill(DoubleMastery, 81, SkillLevel: 1));
        }

        // ---- THE MELEE ROGUE, 2026-09-10 — the same two skills under two other names. ----
        //
        // *"add to duals 4th same reuse_reset_momentum with name Stab Momentum"*
        // *"add to duals 4th the same double_mastery with name Momentum Mastery"*
        //
        // 🔑 SAME IDS, NOT COPIES. One def, one set of numbers, a `DisplayName` override per class —
        // which is this project's standing convention for per-class flavour and the reason he renamed
        // both ids off their mage/warrior wording in the first place (*"as other classes can aqure it
        // too"*). If the reuse base ever moves, it moves for the Magus and the rogue together, which
        // is what he asked for.
        //
        // 🔑 THE PAIR IS COHERENT ONLY BECAUSE THE TOGGLE WAS GENERALISED. A rogue has no Overpower,
        // so a toggle that doubled only the damage base would be 50 HP/s for nothing. It now doubles
        // all three bases, so on this class it doubles the reuse base Stab Momentum supplies: 5% → 10%
        // before the ATK band.
        //
        // ⚠ THE TWO LEARN LEVELS ARE MINE, NOT HIS — the one open number in this block. 76 and 81
        // mirror the Magus's reuse rung and the warrior's toggle exactly, which is the only defensible
        // read of *"the same"*. If he meant otherwise these are two lines and two CSV rows. Flagged in
        // `BL-191` so it is not mistaken for an authored number.
        //
        // ⚠ ONLY THE THREE MELEE ROGUES. The archer branch was ruled out by name the same day.
        foreach (var (race, d) in new[]
                 {
                     (Race.Human, Discipline.Nullblade),
                     (Race.Elf,   Discipline.Phantom),
                     (Race.Demon, Discipline.Venomweaver),
                 })
            // ⚠ ONLY `reuse_reset_momentum` IS RENAMED HERE. The toggle keeps its own name —
            //   **Overpower Mastery** — on his 2026-09-11 correction: *"dagger double_mastery is with
            //   name Overpower Mastery not Momentum Mastery"*. The override was mine and it was the
            //   wrong instinct: a DisplayName is for when the flavour genuinely differs per class, and
            //   here it does not — the toggle does the same thing for the rogue that it does for the
            //   warrior. `Stab Momentum` stays because the rogue's base IS a different mechanic from
            //   the Magus's Arcane Momentum wearing the same id.
            ClassSkills.RegisterFourth(race, d,
                new ClassSkill(ReuseResetMomentum, 76, "Stab Momentum", SkillLevel: 1),
                new ClassSkill(DoubleMastery,      81, SkillLevel: 1));
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE BULWARK, 76-90 — `docs/data/classes_skills_csv/tank 4th.csv` (`BL-154`, `BL-155`, `BL-02`)
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>The tank's 4th tier. The third discipline to get one, and the last `NOT DONE` file in
    /// `BL-02` to close.
    ///
    /// <para>🔑 <b>THE START RUNGS ARE WHERE THE 3rd-CLASS LADDERS ACTUALLY ENDED</b>, and getting one
    /// wrong is the only real hazard in this table — a <see cref="ClassSkill"/> names the RUNG, so an
    /// off-by-one sells a level-76 tank his level-74 numbers. Counted, not guessed: Taunt, Charm and
    /// Shield Shock reached <b>19</b> (four 2nd-class rungs + fifteen 3rd-class ones), the two
    /// masteries <b>20</b> (five + fifteen), Mass Taunt / Intimidate / Freeze / Stay / both Shield
    /// Smashes <b>15</b>, Defensive Wall exactly <b>2</b> (one at 20, one at 46), the six whisp calls
    /// <b>8</b>, and Whisp Mastery <b>1</b>.</para>
    ///
    /// <para>🔑 <b>THE RACE SPLIT IS THE 3rd TIER'S, CONTINUED</b> — Human taunt + mass taunt, Elf
    /// charm + freeze, Demon taunt + intimidate; Shield Smash Rate for Human and Elf, Power for the
    /// Demon; the whisps in the same three pairs. What the 4th tier ADDS to it is the silence pair
    /// (`BL-155`: Numbing Shock for Human and Demon, Silencing Shock for the Elf) and Backlash, whose
    /// two halves are rungs 1-3 and 4-6 of one id.</para>
    ///
    /// <para>⚠ <b>WHAT IS DELIBERATELY NOT HERE.</b> Shield Mastery, Final Defense, Aggravated State
    /// and Shield Reinforcement have NO row in his 4th file, so their ladders stop where the 3rd tier
    /// left them. Do not invent continuations — the same ruling Harmony of Speed got at the buffer's
    /// 4th tier. (Tank <b>Weapon</b> Mastery was on this list for one commit; he had simply forgotten
    /// it and wrote its fifteen rows the same day. Which is the argument for the rule: the answer to
    /// a missing ladder is to ASK, never to invent one.)</para></summary>
    private static void RegisterBulwarkFourth()
    {
        ClassSkill[] Ladder(string skill, int[] bands, int startRung) =>
            bands.Select((lvl, i) => new ClassSkill(skill, lvl, SkillLevel: startRung + i)).ToArray();

        ClassSkill[] At(string skill, params (int Level, int Rung)[] rows) =>
            rows.Select(r => new ClassSkill(skill, r.Level, SkillLevel: r.Rung)).ToArray();

        int[] all  = TankFourthAll;    // 76…90
        int[] even = TankFourthEven;   // 76, 78 … 90
        int[] odd  = TankFourthOdd;    // 77, 79 … 91 — the B whisps only

        var shared = new List<ClassSkill>();

        // ---- THE TWO EVERY-LEVEL PASSIVES ----
        shared.AddRange(Ladder(TankArmorMastery,  all, 21));
        shared.AddRange(Ladder(TankAntiMagic,     all, 21));
        // ✅ WEAPON MASTERY JOINED THEM 2026-09-04 — he had forgotten it and added its fifteen rows to
        //    the file after the first build (*"and I have forgotten the weapon mastery"*). Same rung
        //    arithmetic as the other two: five 2nd-class rungs + fifteen 3rd-class ones ended at 20.
        shared.AddRange(Ladder(TankWeaponMastery, all, 21));

        // ---- THE SHARED ACTIVES, every other level ----
        // `BL-188` — Vital Organ Protection, ONE rung at 80, all three races. Not in `tank 4th.csv`
        // when he called that file finished; it was ruled on 2026-09-09 with the blow rework and its
        // row was written into the file in the same commit.
        shared.Add(new ClassSkill(TankVitalOrganProtection, 80));
        shared.AddRange(Ladder(TankStay,        even, 16));
        shared.AddRange(Ladder(TankShieldStun,  even, 20));
        shared.AddRange(Ladder(DefensiveWall,   even, 3));
        shared.AddRange(Ladder(TankMagicWall,   even, 1));
        shared.AddRange(Ladder(TankPull,        even, 1));

        // ---- NEW AT THE 4th TIER, on their own levels ----
        shared.Add(new ClassSkill(TankTauntingWall, 80));
        // The Perfect Whisp: six rungs, 80 → 90, and no race — the only whisp all three tanks share.
        shared.AddRange(Ladder(TankWhispHelp, new[] { 80, 82, 84, 86, 88, 90 }, 1));
        // Whisp Mastery's third slot, at 83.
        shared.AddRange(At(TankWhispMastery, (83, 2)));
        // 🔴 UNDYING WILL MOVES TIER, exactly as the healer's Rite of Preservation did. It was
        //    registered at 83 on the THIRD-class table with a placeholder 100k SP; his `tank 4th.csv`
        //    prices it at 500kk + 100kk gold and two Physical Stones, which is a 4th-tier price. Two
        //    learn lines for one skill would let a non-ascended level-83 tank buy it at the old one.
        shared.Add(new ClassSkill(UndyingWill, 83));

        // ---- BACKLASH. ONE ID, SIX RUNGS, and the rung index is what carries the race: 1-3 are
        //      Physical Backlash, 4-6 Magical Backlash. The DisplayName override is what makes each
        //      race see his own name for it — the same mechanism the per-class flavour names use.
        var physicalBacklash = At(Backlash, (77, 1), (80, 2), (83, 3))
            .Select(cs => cs with { DisplayName = "Physical Backlash" }).ToArray();
        var magicalBacklash = At(Backlash, (77, 4), (80, 5), (83, 6))
            .Select(cs => cs with { DisplayName = "Magical Backlash" }).ToArray();

        // ---- HUMAN: taunt, mass taunt, the rate smash, the physical silence, taunt+bind whisps. ----
        var human = new List<ClassSkill>(shared);
        human.AddRange(Ladder(Provoke,             even, 20));
        human.AddRange(Ladder(TankMassProvoke,     even, 16));
        human.AddRange(Ladder(TankSmashRate,       even, 16));
        human.AddRange(Ladder(TankSilencePhysical, even, 1));
        human.AddRange(Ladder(TankWhispTaunt,      even, 9));
        human.AddRange(Ladder(TankWhispBind,       odd,  9));
        human.AddRange(physicalBacklash);

        // ---- ELF: charm (which replaces taunt), freeze, the rate smash, the MAGICAL silence,
        //      charm+heal whisps. ----
        var elf = new List<ClassSkill>(shared);
        elf.AddRange(Ladder(TankCharm,            even, 20));
        elf.AddRange(Ladder(TankFreeze,           even, 16));
        elf.AddRange(Ladder(TankSmashRate,        even, 16));
        elf.AddRange(Ladder(TankSilenceMagical,   even, 1));
        elf.AddRange(Ladder(TankWhispCharm,       even, 9));
        elf.AddRange(Ladder(TankWhispHeal,        odd,  9));
        elf.AddRange(magicalBacklash);

        // ---- DEMON: taunt, intimidate, the POWER smash, the physical silence, the two break whisps.
        var demon = new List<ClassSkill>(shared);
        demon.AddRange(Ladder(Provoke,             even, 20));
        demon.AddRange(Ladder(TankFear,            even, 16));
        demon.AddRange(Ladder(TankSmashPower,      even, 16));
        demon.AddRange(Ladder(TankSilencePhysical, even, 1));
        demon.AddRange(Ladder(TankWhispArmorBreak, even, 9));
        demon.AddRange(Ladder(TankWhispWeaponBreak, odd, 9));
        demon.AddRange(physicalBacklash);

        ClassSkills.RegisterFourth(Race.Human, Discipline.Bulwark, human.ToArray());
        ClassSkills.RegisterFourth(Race.Elf,   Discipline.Bulwark, elf.ToArray());
        ClassSkills.RegisterFourth(Race.Demon, Discipline.Bulwark, demon.ToArray());
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
        // ⚠ RUNGS 6-8, NOT 3-5, SINCE `BL-217` — three 3rd-tier rungs were inserted below them at
        //   58/66/74. The LEVELS are unchanged; an off-by-three here sells a level-77 buffer his
        //   level-58 rung.
        shared.AddRange(At(NpcHarmonyWizard,     (77, 6), (78, 7), (79, 8)));
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

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE ARCHER, 76-90 — `docs/data/classes_skills_csv/archer 4th.csv`
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>THE ARCHER'S 4th CLASS, 76-90. Built 2026-09-09, the same day as its 3rd tier.
    ///
    /// <para>🔑 <b>NINE FAMILIES SIMPLY CONTINUE</b> — every 3rd-tier ladder gains rungs 16-30 on
    /// every level from 76 to 90. What is new is three PARTY PROCS at 76 (one per race) and five
    /// ultimates at 84/85 bought with gold and SP BOTTLES rather than SP.</para>
    ///
    /// <para>🔑 Same one-discipline-per-race shape as the 3rd tier: Sharpshooter is the Human, Trapper
    /// the Elf, Hunter the Demon. Each race's trap, Magic Arrow, party mastery and ultimate follow the
    /// split his RACE column already made at 40 — nothing crosses over.</para>
    ///
    /// <para>⚠ Arrow Barrage and Heavy Arrow carry NO race, so all three learn them.</para></summary>
    private static void RegisterArcherFourth()
    {
        // Rungs 16-30 of a 3rd-tier ladder, one per level from 76 to 90.
        ClassSkill[] Ladder(string skill, int startRung = 16) =>
            HealerFourthBands.Select((lvl, i) => new ClassSkill(skill, lvl, SkillLevel: startRung + i))
                             .ToArray();

        var shared = new List<ClassSkill>();
        shared.AddRange(Ladder(ArcherArmorMastery));
        shared.AddRange(Ladder(BowWeaponMastery));
        shared.AddRange(Ladder(ArcherTwinArrows));
        shared.AddRange(Ladder(ArcherExplosiveArrow));
        // The two ultimates his RACE column leaves blank.
        shared.Add(new ClassSkill(ArcherHeavyArrow,   84));
        shared.Add(new ClassSkill(ArcherArrowBarrage, 85));
        // 🔴 `BL-213` — OVERPOWER's SECOND AND LAST RUNG for the bow branch: 7% at 76, where the
        //    warrior's third (10%) would be. The melee rogue's line, copied deliberately — see the 3rd
        //    tier for his ruling, and RegisterMeleeRogueFourth's own copy for why the top rung is left
        //    unreachable and why the price is overridden upward to this tier's 6.5kk.
        // ⚠ GOLD IS 0 for the same reason it is 0 on the dual's row: `ClassSkill` can override SP and
        //   nothing else, and rung 2 of a ladder authored for level 40 carries no `GoldCost`. If that
        //   matters it wants a `GoldCost` override on ClassSkill, not a second Overpower def.
        shared.Add(new ClassSkill(Overpower, 76, SkillLevel: 2, SpCost: 6_500_000));

        var human = new List<ClassSkill>(shared);
        human.AddRange(Ladder(ArcherPoisonTrap));
        human.AddRange(Ladder(ArcherMagicArrowHuman));
        human.Add(new ClassSkill(BowDamageMastery,   76));
        human.Add(new ClassSkill(ArcherDazzlingArrow, 85));

        var elf = new List<ClassSkill>(shared);
        elf.AddRange(Ladder(ArcherBindingTrap));
        elf.AddRange(Ladder(ArcherMagicArrowElf));
        elf.Add(new ClassSkill(BowSpiritMastery,   76));
        elf.Add(new ClassSkill(ArcherHealingArrow, 85));
        // 🔴 NO ANTIDOTE RUNG AT 76, and it took `--check` to say so. A tenth rung was added here by
        //    analogy with the healer's (whose Antidote does get one more at 76, reaching rank 10) —
        //    and `archer 4th.csv` authors no Antidote row at all. The Elf archer's cure stops at rank
        //    9, which means the Venomweaver's top-rung venom stays uncurable by anything in either
        //    rogue file. That is his file; the analogy was mine.


        var demon = new List<ClassSkill>(shared);
        demon.AddRange(Ladder(ArcherBleedTrap));
        demon.AddRange(Ladder(ArcherMagicArrowDemon));
        demon.Add(new ClassSkill(BowSwiftMastery,    76));
        demon.Add(new ClassSkill(ArcherBleedingArrow, 85));

        ClassSkills.RegisterFourth(Race.Human, Discipline.Sharpshooter, human.ToArray());
        ClassSkills.RegisterFourth(Race.Elf,   Discipline.Trapper,      elf.ToArray());
        ClassSkills.RegisterFourth(Race.Demon, Discipline.Hunter,       demon.ToArray());
    }

    /// <summary>THE MELEE ROGUE'S 4th CLASS — Nullblade (Human) / Shadowblade (Elf) / Venomblade
    /// (Demon) — and it is THREE SKILLS, not a kit.
    ///
    /// <para>🔴 `dual 4th.csv` is still the two-line placeholder and the 40+ rule stands for the rest
    /// of it. What is here is only the top of the blow ladder, which he ruled outright on 2026-09-09
    /// while settling `BL-188` and asked to be built in the same breath. The three rows are in the
    /// CSV; the file has NOT earned a `Check.Specs` line, because the checker walks whole files.</para>
    ///
    /// <para>🔑 No race split — all three disciplines learn the same three. The race split in this
    /// class lives entirely on the 3rd tier's Lethal Focus / Precision / Frenzy, which is where it
    /// does its balancing work.</para></summary>
    private static void RegisterDual4th()
    {
        int[] all = { 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90 };

        static IEnumerable<ClassSkill> Ladder(string id, int[] levels, int startRung) =>
            levels.Select((lv, i) => new ClassSkill(id, lv, SkillLevel: startRung + i));

        // ---- WHAT HE RULED: the top of `BL-188`'s blow ladder. ----
        var shared = new List<ClassSkill>
        {
            new(AssassinationInstinct, 76),
            // The @80 CHOICE. Both are offered and both are learnable — they are mutually exclusive
            // in the BUFF BAR (one shared key), never in the learn tab.
            new(PerfectStrike, 80),
            new(BrutalStrike,  80),
        };

        // ---- WHAT IS DERIVED so the class is MEASURABLE above 76 (owner, 2026-09-10: *"Build the
        //      new skills for duals 4 so it's measurable after 76 (even with lower power skills)"*).
        //      Fifteen rungs each, one per level — the archer's band shape, because a melee rogue's
        //      families are the same fifteen-rung ladders his are.
        //      ⚠ Armor Mastery is rungs 21-35 of the SAME id (`rogue_armor_mastery`) and Dual Mastery
        //      rungs 16-30 of its own, exactly as the 3rd tier appended rungs 6-20 and 1-15.
        shared.AddRange(Ladder(RogueArmorMastery, all, 21));
        shared.AddRange(Ladder(DualWeaponMastery, all, 16));
        // `BL-203` — OVERPOWER's SECOND AND LAST RUNG for this class: 7% at 76, where the warrior's
        // third (10%) would be. See the 3rd-tier table for his ruling and for why the top rung is
        // left unreachable.
        // ⚠ PRICED AT THIS TIER, not at the warrior's 40 — rung 2's own 28,000 is a 3rd-class price
        //   and every first rung of an ascended kit charges the shared 76 ladder (6.5kk). Same reason
        //   the 40 entry overrides upward; see ClassSkill.SpCost.
        // 🔑 THE GOLD IS 0 AND THAT IS A GAP, NOT A CHOICE: `ClassSkill` can override SP and nothing
        //    else, and rung 2 of a ladder authored for level 40 carries no `GoldCost`. Every other
        //    76 row in `dual 4th.csv` charges 1kk gold beside its SP. If that matters it wants a
        //    `GoldCost` override on ClassSkill, not a second Overpower def.
        shared.Add(new ClassSkill(Overpower, 76, SkillLevel: 2, SpCost: 6_500_000));

        // ---- HIS 2026-09-11 BLOCK — the race IDENTITY passives and the race ULTIMATES, which is what
        //      closed the file (*"With that duals 4th is finihed"*). ONE axis per race, carried at two
        //      strengths: a permanent 5/7/10% passive at 80/85/90, and the same defence turned up to
        //      25-30% for ten seconds at 83. Human vs magic, Elf vs physical skills, Demon vs people.
        //      ⚠ Three rungs, so `SkillLevel` is 1/2/3 — not the fifteen-rung ladders above.
        static IEnumerable<ClassSkill> Identity(string id) =>
            new[] { 80, 85, 90 }.Select((lv, i) => new ClassSkill(id, lv, SkillLevel: i + 1));

        var human = new List<ClassSkill>(shared);
        human.AddRange(Ladder(KillingStab, all, 16));
        human.AddRange(Ladder(HeavyStab,   all, 16));
        human.AddRange(Identity(DualAntiMagic));
        human.Add(new ClassSkill(DualMagicArmor, 83));

        var elf = new List<ClassSkill>(shared);
        elf.AddRange(Ladder(KillingStab, all, 16));
        elf.AddRange(Ladder(SwiftStab,   all, 16));
        elf.AddRange(Identity(DualAntiPhysical));
        elf.Add(new ClassSkill(DualDodge, 83));

        // ⚠ THE DEMON STILL GETS NO KILLING STAB — his `Human;Elf` cell on it at the 3rd tier is a
        //   race ruling, not a tier one, and Venom Stab + Venom Burst remain the whole of the
        //   Venomweaver's damage.
        var demon = new List<ClassSkill>(shared);
        demon.AddRange(Ladder(VenomStab,  all, 16));
        demon.AddRange(Ladder(VenomBurst, all, 16));
        demon.AddRange(Identity(DualDuelExpertise));
        demon.Add(new ClassSkill(DualDemonContract, 83));

        ClassSkills.RegisterFourth(Race.Human, Discipline.Nullblade,   human.ToArray());
        ClassSkills.RegisterFourth(Race.Elf,   Discipline.Phantom,     elf.ToArray());
        ClassSkills.RegisterFourth(Race.Demon, Discipline.Venomweaver, demon.ToArray());
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE MAGUS, 76-90 — `docs/data/classes_skills_csv/nuker 4th.csv` (`BL-192`)
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>THE NUKER'S 4th CLASS. His file, 236 rows, finished 2026-09-10.
    ///
    /// <para>🔑 <b>ONE DISCIPLINE, THREE RACES.</b> The Magus is the archetype's only discipline since
    /// the Tempest was retired (`BL-97`), and — exactly as at the 3rd tier — it is the RACE that splits
    /// the kit: Human takes Arcane Wave / Vampiric Bolt / Arcane Void / Arcane Burst, Elf takes Frost
    /// Spikes / Frost Pierce / Frost Burst, Demon takes Witches Curse / Witches Scarecrow / Pyro Burst,
    /// and each gets its own Spell Empowerment.</para>
    ///
    /// <para>🔑 <b>THE START RUNGS ARE WHERE THE 3rd-CLASS LADDERS ACTUALLY ENDED</b>, counted off
    /// <c>RegisterNuker3rd</c> and the defs, never guessed — an off-by-one sells a level-76 Magus his
    /// level-74 numbers for 6.5kk of SP. Anti-Magic reached 20 (six mage rungs + fourteen), Spellcaster
    /// Weapon Mastery 14, Mage Armor Mastery 18 (four at 20-35 + fourteen), the three shared nukes 14,
    /// Vampiric Bolt 19 (five below 40 + fourteen), the two stone-eating ultimates 3, the three race
    /// Bursts 1, and Arcane Void 3.</para>
    ///
    /// <para>⚠ <b>FOUR 3rd-TIER FAMILIES GAIN NOTHING HERE, AND THAT IS HIS FILE.</b> Calm Spirit,
    /// Restore Spirit, Phase Shift and Meditation have no row in `nuker 4th.csv`. Do not invent
    /// continuations for them — the same ruling that stopped Harmony of Speed at 58.</para>
    ///
    /// <para>⚠ <b>ARCANE MOMENTUM IS NOT LISTED.</b> Its @76 rung is registered with its three siblings
    /// in <see cref="RegisterSkillMasteriesFourth"/> (`BL-191`), which is one layer and one ruling;
    /// adding it here as well would register the same rung twice.</para></summary>
    private static void RegisterMagusFourth()
    {
        // Rungs `startRung`… of a continuing ladder, one per level from 76 to 90.
        ClassSkill[] Ladder(string skill, int startRung) =>
            HealerFourthBands.Select((lvl, i) => new ClassSkill(skill, lvl, SkillLevel: startRung + i))
                             .ToArray();

        // Explicit (character level, rung) rows, for the ladders that are neither shape.
        ClassSkill[] At(string skill, params (int Level, int Rung)[] rows) =>
            rows.Select(r => new ClassSkill(skill, r.Level, SkillLevel: r.Rung)).ToArray();

        // The three ULTIMATE bands his file uses: 80 / 85 / 90, three rungs, one price.
        ClassSkill[] Ult(string skill, int startRung) =>
            new[] { 80, 85, 90 }.Select((lvl, i) => new ClassSkill(skill, lvl, SkillLevel: startRung + i))
                                .ToArray();

        var shared = new List<ClassSkill>();

        // ---- THE THREE PASSIVES. Two are shared outright with the healer's kit (same def, same
        //      rungs); Mage Armor Mastery is the nuker's own because it alone carries mpWhenRestored.
        shared.AddRange(Ladder(MageAntiMagic,            21));
        shared.AddRange(Ladder(HealerWeaponMasterySkill, 15));
        shared.AddRange(Ladder(NukerArmorMastery,        19));

        // ---- THE THREE SHARED ATTACK SPELLS, one rung a level.
        shared.AddRange(Ladder(ElementalBlast, 15));
        shared.AddRange(Ladder(QuickBlast,     15));
        shared.AddRange(Ladder(ElementalWave,  15));

        // ---- THE TWO STONE-EATING NUKES. Both stop at rung 6 and then they are finished.
        shared.AddRange(Ult(ElementalBurst, 4));
        shared.AddRange(Ult(Thunderstorm,   4));

        // ---- NEW AT THE 4th TIER, and shared by all three races.
        shared.Add(new ClassSkill(NukerShieldMastery, 76));
        shared.AddRange(At(NukerForceEmpowerment, (78, 1), (80, 2), (82, 3)));
        shared.Add(new ClassSkill(ManaBarrier, 85));

        // ═══ THE RACE SPLIT ══════════════════════════════════════════════════════════════════════
        var human = new List<ClassSkill>(shared);
        human.AddRange(Ladder(ArcaneWave,   15));
        human.AddRange(Ladder(VampiricBolt, 20));
        // ⚠ Arcane Void is the ONE family on a 76/80/85/90 shape — four rungs, not fifteen and not
        //   three. His rows; only the MP moves across them.
        human.AddRange(At(ArcaneVoid, (76, 4), (80, 5), (85, 6), (90, 7)));
        human.AddRange(Ult(ArcaneBurst, 2));
        human.AddRange(Ult(NukerSpellEmpowermentHuman, 1));

        var elf = new List<ClassSkill>(shared);
        elf.AddRange(Ladder(FrostSpikes, 15));
        elf.AddRange(Ladder(FrostPierce, 15));
        elf.AddRange(Ult(FrostBurst, 2));
        elf.AddRange(Ult(NukerSpellEmpowermentElf, 1));

        var demon = new List<ClassSkill>(shared);
        demon.AddRange(Ladder(WitchesCurse,     15));
        demon.AddRange(Ladder(WitchesScarecrow, 15));
        demon.AddRange(Ult(PyroBurst, 2));
        demon.AddRange(Ult(NukerSpellEmpowermentDemon, 1));

        ClassSkills.RegisterFourth(Race.Human, Discipline.Magus, human.ToArray());
        ClassSkills.RegisterFourth(Race.Elf,   Discipline.Magus, elf.ToArray());
        ClassSkills.RegisterFourth(Race.Demon, Discipline.Magus, demon.ToArray());
    }
}
