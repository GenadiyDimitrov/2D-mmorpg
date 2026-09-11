namespace Game.Shared;

using static Game.Shared.SkillCatalog;

/// <summary>
/// 3rd-class (discipline) learn tables.
///
/// <para><b>ONE discipline is fully authored</b> — the Lightbringer (healer), 40 to 74, straight off
/// `docs/data/classes_skills_csv/healer 3rd.csv` (2026-08-20). See RegisterLightbringer.</para>
///
/// <para>Everything else in this file is still governed by the 2026-08-10 purge at the top of
/// RegisterThirdClasses: the two NUKER disciplines keep the placeholder kit he explicitly spared, the
/// Warchanter keeps the buff ladder he has a (placeholder) CSV for, and the eight fighter disciplines
/// teach nothing at all until their files exist. The Lightbringer being switched on is what a finished
/// CSV looks like — it is not a reason to switch on anything else.</para>
/// </summary>
public static partial class ClassSkillTables
{
    static partial void RegisterThirdClasses()
    {
        // ⚠⚠ 2026-08-10 — THE 40+ PURGE (owner). Everything a 3rd class taught was invented here
        // with no CSV behind it, so he cut it to the bone: *"Anything that's not inside the csv
        // should not exist except the class balance."* His four points, verbatim in effect:
        //   1. leave the mage (Tempest and Magus)          → the nuker blocks below survive
        //   2. leave the Warchanter buffs, he has a CSV    → RegisterWarchanterBuffs() survives
        //   3. evade_mastery / anti_magic / precision go INTO the 20-35 CSVs (rogue/tank/warrior)
        //   4. remove every other 40+ skill, and every class-change hidden bonus
        // DELETED here accordingly: the placeholder RENAME kit for all ten fighter disciplines, the
        // warrior demos (Cleaving Strike / Hamstring / War Focus), the tank kit (Shield Bash,
        // Provoke, Aegis, Last Stand, Indomitable), Terrifying Roar, the Venomweaver DoT trio, and
        // the rogue primitives (Shadowstep, Vanish, Repelling Shot, Snare Trap) for Phantom,
        // Trapper, Nullblade and Hunter.
        //
        // ⚠ Their SkillDefs all STAY in the catalog — only the learn assignments are gone, exactly
        // as with `PowerShot` (deleting a def is what the old warnings were about, and anything a
        // character already learned keeps working; LearnedSkills persists ids, not table entries).
        // They are the obvious raw material when the level-40+ CSVs arrive — do NOT re-grant them
        // before then, and do NOT invent replacements.
        //
        // (For the record, the ranged rogue's three `PowerShot` renames had already gone on
        //  2026-08-07, playtest-19 M7: *"remove it from after 40lvl as well"*.)

        // ✅ THE NUKER IS ON, 2026-08-26 — `nuker 3rd.csv`, 208 authored rows, finished long before the
        // healer's and never built. Everything the two nuker disciplines used to teach above 40 was
        // INVENTED here (the Annihilate/Chain-Lightning renames, the ten-rung Elemental Burst, Frost
        // Bind, Entangling Roots, Glacial Spike, Creeping Frost, Mana Barrier and a lone Phase Shift)
        // and survived the 2026-08-10 purge only under his point 1, *"leave the mage"* — i.e. as a
        // placeholder until the file arrived. It has arrived, so the placeholders are gone and this is
        // the third fully-authored 3rd class.
        //
        // ⚠ Their SkillDefs STAY in the catalog, same rule as always (LearnedSkills persists ids, so
        // deleting a def breaks every character who bought one). Orphaned but defined, and NOT to be
        // re-granted: Flamebolt, Greater Weakness, Frost Bind, Entangling Roots, Glacial Spike,
        // Creeping Frost, Mana Barrier, Weakness. 🔑 That also retires the two dead-end rungs memory
        // has been carrying — Flamebolt @40 and Glacial Spike @44 were single 40+ placeholders whose
        // fizzle curve killed them by 58 and 62; his ladders replace both.
        RegisterNuker3rd();

        // ✅ THE LIGHTBRINGER IS ON, 2026-08-20. `healer 3rd.csv` is finished and he authorised the
        // build, so the healer discipline is no longer an exception carved out of the 40+ purge — it
        // is the first fully-authored 3rd class in the game, 40 to 74, every row his.
        //
        // ⚠ THE PURGE STILL STANDS FOR EVERYONE ELSE. This is not a precedent for switching the others
        // back on: the eight fighter disciplines have no file at all.
        RegisterLightbringer();
        // THE WARCHANTER, 40-74 — his `buffer 3rd.csv`. Built so far: the buff/harmony/group layer
        // (2026-08-21) and Shield Mastery (same day, registered at the bottom of RegisterWarchanterBuffs).
        // ⚠ HE FINISHED AUTHORING THE FILE ON 2026-08-21 and removed its `NOT DONE` banner, so
        // `--check` now walks all 341 rows: sixteen skill families below the old banner report as
        // 🔴 NOT REGISTERED (Sound Burst, Sound Smash, Sharpening, Reinforcement, Harmony of
        // Restoration, Combo Mastery, Mana Vampirism, the three armour/bow masteries, Great Heal,
        // Armor Mastery, Spell Mastery, Bow Expertise, …). Those are the remaining build, not defects.
        // 🔴 `RegisterWarchanter()` at the bottom of this file STAYS OFF, and is now genuinely dead:
        // its per-race Bolt/Chant/Renew/Pass were the INVENTED pre-CSV kit, and the attack half of his
        // file is what replaces them. Do not switch it back on — delete it when his rows land.
        RegisterWarchanterBuffs();
        // …and now a SECOND, equally narrow one: the two level-83 preservation skills he authorised
        // by name on 2026-08-14 (`BL-35`). Two learn lines, nothing else — the Lightbringer and
        // Bulwark kits above stay commented out.
        RegisterPreservation();
        // …and a THIRD, on the same terms: the three HIDE skills, which he placed by hand.
        RegisterHideKit();
        // …and a FOURTH: Shield Mastery's rung 4 at 52, the single row he authored into the previously
        // empty `tank 3rd.csv` on 2026-08-21. Same narrow terms — one learn line, nothing around it.
        RegisterTankShieldMastery();
        // …and a FIFTH: HP Boost above 40 — the warrior's L4-L10 and the buffer's L1-L7, the rows he
        // added to `warrior 3rd.csv` and `buffer 3rd.csv` on 2026-08-27. Same narrow terms again.
        RegisterHpBoost();
        // …and a SIXTH: the WHISPS (`BL-109`) — the six calls and Whisp Mastery, which are the
        // `Whisps` block of `tank 3rd.csv` and nothing else from that file. Same narrow terms as
        // every entry above: he asked for the whisp system, its PoC rows are the ones he authored
        // for it, and the rest of that (still open) file waits for the one-pass tank delta.
        RegisterWhisps();
        // …and the SEVENTH, which is no longer narrow at all: THE WHOLE BULWARK, 40-74. His
        // `tank 3rd.csv` is finished (*"U can finish the tank 3rd"*, 2026-09-02), so the 40+ purge
        // that governs this file no longer applies to the tank — a finished CSV is exactly the
        // condition it was waiting for, and the Lightbringer, Warchanter and Magus went the same way.
        RegisterBulwark();
        // …and an EIGHTH, `BL-185`: the WARRIOR'S and the ARCHER'S damage kits, 40-74. These are the
        // one entry here whose numbers came from a RECIPE rather than a file — he gave the derivation
        // in chat (*"take the elf harmonist skills and bow passives ... increase them with ~20%"*)
        // while saying *"I'll try next week to finish the csvs"*. Same standing as every narrow entry
        // above: he asked for it by name. See Skills.FighterKits3rd.cs and `BL-185`.
        RegisterWarriorAndArcherKits();
        // …and the NINTH and TENTH, both on the same footing as the Bulwark and the Magus: his
        // `dual 3rd.csv` and `archer 3rd.csv` are finished (*"build/fix rogue 2nd, archer and duals
        // 3rd"*, 2026-09-09), so six more of the eight fighter disciplines come off the 40+ purge.
        // What is left waiting for a file is the WARRIOR's two, and nothing else.
        RegisterDual3rd();
        RegisterArcher3rd();
        // …and the ELEVENTH, 2026-09-11: the WARRIOR's two, `warrior 3rd.csv` (Ravager) and
        // `war_aoe 3rd.csv` (Warlord). With these the 40+ purge covers no fighter discipline at all —
        // every one of the eight now learns from a file. ⚠ HALF a file each, in his own words: they
        // carry the passives and the buffs and *"are missing only teir dmg and control (active dmg)
        // skills"*, which is why `war_sundering_blow` above is still registered.
        RegisterWarrior3rd();
        // (A FOURTH, `RegisterHealerMasteries()`, existed for one day and is gone: it taught the two
        //  healer masteries and Frenzy L2 while RegisterLightbringer was still commented out. Those
        //  rungs are in the shared ladder now, and keeping both would have registered every one twice.)
    }

    /// <summary>The three invisibility skills, re-homed exactly where he put them in playtest 23
    /// (2026-08-15). ⚠ Same standing as <see cref="RegisterPreservation"/>: skills HE placed by name and
    /// by level, not a repeal of the 40+ purge at the top of this file.
    ///
    /// <para>His three lines, verbatim: *"`Prowl` should be learnable by all mele rogues @40 3rd class
    /// (not auto, like a normal skill)"* · *"`Signal Flare` should be learnable by all archers @60 3rd
    /// class"* · *"`Vanish` … should be learnable by all mele rogues @60 3rd class … cool down 2 min,
    /// duration - 30s."* "Not auto" is already how every entry in these tables works — a
    /// <see cref="ClassSkill"/> makes a skill LEARNABLE at that level for SP; nothing here grants it.</para>
    ///
    /// <para>🔑 "All melee rogues" and "all archers" are three disciplines each, because the archer merge
    /// (2026-07-29) split the rogue by RACE at 40: melee = Nullblade (human) · Venomweaver (demon) ·
    /// Phantom (elf); ranged = Sharpshooter (human) · Hunter (demon) · Trapper (elf). Registered for all
    /// three races on each, matching the file's idiom — <see cref="Disciplines.Of"/> is what actually
    /// gates who can hold the discipline, so the off-race keys are inert.</para>
    ///
    /// <para>This SUPERSEDES the 0.67.2 stopgap that put Vanish on the Phantom alone at 40, which existed
    /// only because Vanish is the one skill in the game carrying <c>GrantsHide</c> and the purge had left
    /// `BL-69`'s headline feature with no player-reachable trigger. He has now placed it properly.</para>
    ///
    /// <para>⚠ Vanish's SP price is still the record default of 1 and is still HIS to set — it is 40+
    /// balance, and Prowl (3400) and Signal Flare (12000) are the neighbours it should be priced against.</para></summary>
    private static void RegisterHideKit()
    {
        var melee  = new[] { Discipline.Nullblade, Discipline.Venomweaver, Discipline.Phantom };
        var ranged = new[] { Discipline.Sharpshooter, Discipline.Hunter, Discipline.Trapper };

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
        {
            foreach (var d in melee)
                ClassSkills.RegisterThird(race, d,
                    new ClassSkill(Prowl, 40, SkillLevel: 1),
                    // 🔴 Lure joins the hide kit, his ruling 2026-08-19: *"move lure from rogue to dual
                    // 3rd (@40) … No lure for lvl 29 and below .. It's a skill that need the prawl
                    // effect."* It used to sit on the 2nd-class rogue at 20/28/36 (see
                    // ClassSkillTables.Common.cs), which handed out the pull twenty levels before the
                    // stance that makes a pull survivable — and to archers as well, since the 2nd-class
                    // rogue block covers both weapons to 40.
                    // ✅ ALL THREE RUNGS, 2026-09-09 — the gap this note used to describe is closed.
                    // He named the levels and the MP himself (*"the lure should be at 52,62,74 (with
                    // sp for the levels) and mp should be 65,80,95"*) and the rows are in
                    // `dual 3rd.csv` now. ⚠ RUNG 1 MOVED 40 → 52 with them: his three levels are the
                    // whole ladder, so the pull no longer arrives with Prowl but a tier later.
                    new ClassSkill(Lure, 52, SkillLevel: 1),
                    new ClassSkill(Lure, 62, SkillLevel: 2),
                    new ClassSkill(Lure, 74, SkillLevel: 3),
                    new ClassSkill(Vanish, 60));
            foreach (var d in ranged)
                ClassSkills.RegisterThird(race, d, new ClassSkill(SignalFlare, 60, SkillLevel: 1));
        }
    }

    /// <summary>`BL-35` — the two level-83 auto-resurrect skills, and NOTHING else from either kit.
    ///
    /// <para>⚠ Read this next to the 40+ purge at the top of the file. That purge deleted every
    /// invented 3rd-class learn assignment and its rule still stands: no 40+ skill until his CSVs
    /// land. These two are here because he named them individually on 2026-08-14 — *"two skills, both
    /// at level 83"*, one Lightbringer and one Bulwark — which is an EXCEPTION to that rule, not a
    /// repeal of it. Adding a third skill to either discipline on the strength of this method would
    /// be exactly the mistake the purge was cleaning up.</para>
    ///
    /// <para>Both are shared by all three races: he specified them per DISCIPLINE, and nothing in his
    /// ruling distinguishes a Human Bulwark from an Demon one.</para></summary>
    /// <summary>`BL-109` — THE WHISPS, off the `Whisps` block of `tank 3rd.csv`. Six calls at 40,
    /// split by RACE exactly as his column splits them, and Whisp Mastery at 60 for everybody.
    ///
    /// <para>🔑 THE RACE SPLIT IS THE POINT. Human = taunt + bind, Elf = charm + heal, Demon = the two
    /// breaks — and with ONE slot until level 60, a tank picks one of his two. That is the largest
    /// thing race has ever decided about a class in this game, and it is his design, not an
    /// interpretation: the rows carry the races in their own column.</para>
    ///
    /// <para>⚠ NOTHING ELSE FROM THAT FILE. It is still open — his `NOT DONE` banner is at line 228 —
    /// and the taunt, mass-taunt, intimidate, freeze, stay and charm ladders, the anti-magic and
    /// weapon masteries and Defensive Wall all wait for the single tank pass. The whisps are built
    /// alone because the whisp SYSTEM is what he queued, and these are the rows he wrote for it.</para></summary>
    /// <summary>THE BULWARK, 40-74 — every row of `docs/data/classes_skills_csv/tank 3rd.csv`.
    /// Built 2026-09-02 when he said the file was finished: *"U can finish the tank 3rd"*.
    ///
    /// <para>🔑 <b>RACE DECIDES FOUR OF HIS TOOLS</b>, and this is the first class where it decides
    /// anything at all beyond flavour. His RACE column: Taunt is Human;Demon and Charm is Elf (and
    /// Charm REPLACES Taunt, so nobody holds both); Mass Taunt is Human, Intimidate is Demon, Freeze
    /// is Elf; Shield Smash — Rate is Human;Elf and — Power is Demon. Everything else — four
    /// masteries, Final Defense, Aggravated State, Stay, Shield Shock, Defensive Wall, Shield
    /// Reinforcement, Whisp Mastery — is shared by all three.</para>
    ///
    /// <para>🔑 <b>THE WHOLE FILE RUNS ON ONE LEVEL LADDER</b> — 40/43/46/49/52/55/58/60/62/64/66/68/
    /// 70/72/74 — and one SP ladder, so the only thing this table decides is WHICH skills a race gets
    /// and at which rung each starts. Two skills break the cadence and both are his: Final Defense is
    /// a single rung at 60, and Aggravated State is three at 52/60/68.</para>
    ///
    /// <para>⚠ <b>WHAT IS NOT HERE.</b> His `tank 4th.csv` is still under its own `NOT DONE` banner,
    /// so nothing above 74 was built — including the Whisp Mastery rung at 80 that would raise the
    /// limit to three.</para></summary>
    private static void RegisterBulwark()
    {
        int[] lv = { 40, 43, 46, 49, 52, 55, 58, 60, 62, 64, 66, 68, 70, 72, 74 };
        // The four continued ladders each already own rungs from the 2nd class, so their 3rd-tier
        // rungs START above those: the masteries at rung 6, Taunt and Charm at rung 5.
        static IEnumerable<ClassSkill> Ladder(string id, int[] levels, int firstRung) =>
            levels.Select((l, i) => new ClassSkill(id, l, SkillLevel: firstRung + i));

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
        {
            var kit = new List<ClassSkill>();

            // ---- The four masteries, continued. Armor / Anti-Magic / Weapon start at rung 6 (five
            //      exist below 40); Shield Mastery starts at rung 3 (two exist below 40) and STOPS at
            //      52 — it is the one mastery that does not run the file's full fifteen rungs.
            kit.AddRange(Ladder(TankArmorMastery, lv, 6));
            kit.AddRange(Ladder(TankAntiMagic,    lv, 6));
            kit.AddRange(Ladder(TankWeaponMastery, lv, 6));
            // 🔑 FIVE ROWS SINCE 2026-09-04, was two (40 and 52). His tank pass filled the gap with
            // 43/46/49, and those three rungs buy bow resistance ALONE — see the ladder in
            // Skills.Fighter.cs. `lv.Take(5)` rather than a hand-written list so it cannot drift off
            // his one level ladder; the rung numbers continue the 2nd class's 1-2 at 20/28.
            kit.AddRange(Ladder(TankShieldMastery, lv.Take(5).ToArray(), 3));

            // ---- The shared actives and the two odd-cadence passives.
            kit.AddRange(Ladder(TankStay, lv, 1));            // moved here from the 2nd class
            kit.AddRange(Ladder(TankShieldStun, TankShieldShockLevels, 5));   // Shield Shock, continued from 24-36
            kit.Add(new ClassSkill(DefensiveWall, 46, SkillLevel: 2));
            kit.Add(new ClassSkill(TankShieldReinforce, 60, SkillLevel: 1));
            // Final Defense — THREE rungs since 2026-09-11 (his *"fixed tanks final_defence to have
            // lvls"*). 60 keeps the numbers it always had; 40 and 52 are the new lower two.
            kit.Add(new ClassSkill(TankFinalDefense, 40, SkillLevel: 1));
            kit.Add(new ClassSkill(TankFinalDefense, 52, SkillLevel: 2));
            kit.Add(new ClassSkill(TankFinalDefense, 60, SkillLevel: 3));
            kit.Add(new ClassSkill(TankAggravatedState, 52, SkillLevel: 1));
            kit.Add(new ClassSkill(TankAggravatedState, 60, SkillLevel: 2));
            kit.Add(new ClassSkill(TankAggravatedState, 68, SkillLevel: 3));

            // ---- The race half.
            if (race == Race.Elf)
            {
                kit.AddRange(Ladder(TankCharm, lv, 5));       // continues his 2nd-class 24-36
                kit.AddRange(Ladder(TankFreeze, lv, 1));
                // ✅ ANTIDOTE, 2026-09-09 — six rungs at 52/58/62/66/70/74, and the 🔴 NOT REGISTERED
                // line `--check` has printed against this file since the Bulwark was built is gone with
                // it. The rows were always there; the SKILL was not. `elf_antidote` is a SELF cure and
                // a different id from the healer's targeted `antidote`, which is what the tool was
                // matching against by name — see the def in Skills.Dual3rd.cs. His `dual 3rd.csv` and
                // `archer 3rd.csv` author the identical six rows for their Elf, so one skill serves all
                // three files and the Elf is the cure-carrier of every fighter branch.
                kit.AddRange(Ladder(ElfAntidote, new[] { 52, 58, 62, 66, 70, 74 }, 1));
            }
            else
            {
                kit.AddRange(Ladder(Provoke, lv, 5));         // ditto
            }
            if (race == Race.Human)
                kit.AddRange(Ladder(TankMassProvoke, lv, 1));
            if (race == Race.Demon)
                kit.AddRange(Ladder(TankFear, lv, 1));

            // Shield Smash: Human and Elf get the RATE version, the Demon the POWER one.
            kit.AddRange(Ladder(race == Race.Demon ? TankSmashPower : TankSmashRate, lv, 1));

            ClassSkills.RegisterThird(race, Discipline.Bulwark, kit.ToArray());
        }
    }

    private static void RegisterWhisps()
    {
        // His two level sets, from the LEARN @ LVL column: the A whisps open at 40, the B whisps at
        // 43, and both top out at 74. Eight rungs each, so eight learn rows each.
        int[] a = { 40, 46, 52, 58, 62, 66, 70, 74 };
        int[] b = { 43, 49, 55, 60, 64, 68, 72, 74 };
        static IEnumerable<ClassSkill> Ladder(string id, int[] levels) =>
            levels.Select((lvl, i) => new ClassSkill(id, lvl, SkillLevel: i + 1));

        ClassSkills.RegisterThird(Race.Human, Discipline.Bulwark,
            Ladder(TankWhispTaunt, a).Concat(Ladder(TankWhispBind, b)).ToArray());
        ClassSkills.RegisterThird(Race.Elf, Discipline.Bulwark,
            Ladder(TankWhispCharm, a).Concat(Ladder(TankWhispHeal, b)).ToArray());
        ClassSkills.RegisterThird(Race.Demon, Discipline.Bulwark,
            Ladder(TankWhispArmorBreak, a).Concat(Ladder(TankWhispWeaponBreak, b)).ToArray());

        // The mastery carries no race in his row, so all three learn it.
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
            ClassSkills.RegisterThird(race, Discipline.Bulwark, new ClassSkill(TankWhispMastery, 60));
    }

    private static void RegisterPreservation()
    {
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
        {
            // 🔴 THE HEALER'S HALF MOVED TO THE 4th TIER, 2026-08-26. `healer 4th.csv` carries Rite of
            //    Preservation at 83 with its own (much dearer) price and a five-Holy-Stone reagent, so
            //    its learn line now lives in ClassSkillTables.Fourth.cs and costs the Rite of Ascension
            //    as well. Do NOT re-add it here — two learn lines for one skill would let a
            //    non-ascended level-83 buy the 4th-tier version at the 3rd tier's price.
            // 🔴 THE TANK'S HALF MOVED TOO, 2026-09-04, for exactly the same reason and to the same
            //    place. `tank 4th.csv` carries Undying Will at 83 with a 500kk SP + 100kk gold price
            //    and a two-Physical-Stone reagent; it sat here at a placeholder 100k. Its learn line
            //    is in ClassSkillTables.Fourth.cs now and costs the Rite of Ascension as well.
            //    (Nothing else in this method registers anything any more — the loop is kept so the
            //    healer's note above stays where the next reader will look for it.)
            _ = race;
        }
    }

    /// <summary>THE WARCHANTER (buffer), 40-74 — every row of
    /// <c>docs/data/classes_skills_csv/buffer 3rd.csv</c> above its <c>NOT DONE</c> marker (line 186),
    /// built 2026-08-21 when he said the buff half was finished: *"I managed to do all the buffs all
    /// the harmonies and all the group buffs to lvl 74 .. leter ill do his passive/atack skills"*.
    ///
    /// <para>🔑 <b>HIS SINGLES ARE THE HEALER'S LADDER, RUNG FOR RUNG.</b> All twenty-five buff
    /// families below are learned at the SAME character level and the SAME rung index the
    /// Lightbringer gets them at — that is his standing rule (*"once the healers buffs are in place
    /// they will be the same for the buffer 3rd"*), and it was verified family by family against
    /// <see cref="RegisterLightbringer"/> before this table was written. So nothing here authors a
    /// value: the whole table is WHICH rung and WHEN. If a ladder is ever retuned, both classes move
    /// together automatically.</para>
    ///
    /// <para>🔑 <b>WHAT IS ACTUALLY NEW IS THE TOP LAYER</b> — nine improved GROUPS split by lane
    /// (Feral* fighter, Arcane* mage, the rest combined), three party ECHOES of single-target buffs,
    /// and four HARMONY ladders. Those live in Skills.Warchanter3rd.cs; the reasoning and the
    /// self-checking MP rule are documented there.</para>
    ///
    /// <para>🔑 <b>EVERY GROUP AND ECHO ARRIVES ONE LEARN TIER AFTER ITS LAST CHILD TOPS OUT</b> —
    /// his rule, verbatim: *"The group buff shoul be learned 1 learn tire after the last buff is
    /// maxed out"*. Each line below carries the child levels it is derived from, so the rule stays
    /// checkable rather than being a number somebody has to trust.</para>
    ///
    /// <para>⚠ <b>THE FIVE OLD GROUPS ARE NO LONGER GRANTED.</b> Might and Bulwark, Force and Ward,
    /// Focus and Ferocity, Body and Soul and Swift and Sure each mixed the physical and magic
    /// channels in one cast, which is exactly what he asked to end. Their defs stay in the catalog
    /// (deleting one orphans every character who bought it) but no class teaches them any more.
    /// <c>HolyShield</c> ("Shield Bless and Harden") is likewise superseded, by
    /// <c>Shield Reinforcement</c> at 74.</para>
    ///
    /// <para>⚠ <b>Resurrection STOPS at 66</b> here (80% of lost exp) where the Lightbringer's runs to
    /// 74 and 100%. That is his file, and it is the clearest line between the two disciplines: the
    /// buffer can raise you, the healer raises you properly.</para>
    ///
    /// <para>⚠ <b>Shrouding Hymn moved 30 → 74.</b> It used to arrive with the class change. His
    /// ruling: it is the PARTY stealth and belongs at the top (*"IG learns it at ~80"*); the level-40
    /// SELF version is <c>Conceal</c>, which the healer and the rogue already have.</para>
    ///
    /// <para>⚠ <b>MADNESS IS GONE FROM THIS TABLE.</b> It sat at 76 as an explicitly temporary home
    /// (*"and when the kits land we will move it"*) — this is that kit landing. Nothing grants it
    /// today; it needs a home in his 4th-class file.</para></summary>
    private static void RegisterWarchanterBuffs()
    {
        int[] band = SkillCatalog.HealerBands;   // 40 44 48 52 56 58 60 62 64 66 68 70 72 74

        ClassSkill[] Full(string skill, int startLevel = 1) =>
            Enumerable.Range(0, band.Length)
                .Select(i => new ClassSkill(skill, band[i], SkillLevel: startLevel + i))
                .ToArray();

        ClassSkill[] At(string skill, params (int Level, int Rung)[] rows) =>
            rows.Select(r => new ClassSkill(skill, r.Level, SkillLevel: r.Rung)).ToArray();

        var kit = new List<ClassSkill>();

        // ---- PASSIVES. Anti-Magic continues the cleric's ladder exactly as the healer's does:
        //      rung 7 is his @40 row, one rung per band to +108 M.Def / 25% mRes at 74. ----
        kit.AddRange(Full(MageAntiMagic, startLevel: 7));

        // ---- ACTIVE SUPPORT. Ten rungs and it stops — see the class note above. ----
        kit.AddRange(At(Resurrection,
            (40, 3), (44, 4), (48, 5), (52, 6), (56, 7),
            (58, 8), (60, 9), (62, 10), (64, 11), (66, 12)));

        // ---- THE FIGHTER SINGLES ----
        kit.AddRange(At(CastId(FamPhysAtk),  (40, 3)));                      // Might      +15% P.Atk
        kit.AddRange(At(CastId(FamAs),       (44, 1), (52, 3)));             // Fury       15 → 33%
        kit.AddRange(At(CastId(FamVamp),     (44, 3), (58, 4), (72, 5)));    // Vampirism  7 → 9%
        kit.AddRange(At(CastId(FamCritRate), (44, 5), (52, 6)));             // Focus      25 → 30%
        kit.AddRange(At(CastId(FamCritDmg),  (40, 4), (48, 5), (56, 6)));    // Ferocity   25 → 35%
        kit.AddRange(At(CastId(FamAccuracy), (40, 2), (48, 3), (56, 4)));    // Aim        +2 → +4

        // ---- THE MAGE SINGLES ----
        kit.AddRange(At(CastId(FamMagAtk),   (44, 3), (52, 4)));             // Force      28 → 32%
        kit.AddRange(At(CastId(FamMagCrit),  (62, 3), (70, 6)));             // Insight    50 → 100%
        kit.AddRange(At(CastId(FamCast),     (48, 3)));                      // Alacrity   +30% cast
        kit.AddRange(At(CastId(FamInterrupt),(44, 3), (52, 5), (60, 6), (68, 7)));  // Resolve 36 → 54
        kit.AddRange(At(CastId(FamMpRegen),  (40, 2), (48, 4), (56, 6)));    // Serenity   10 → 20%
        kit.AddRange(At(CastId(FamMagDef),   (44, 3), (52, 4)));             // Ward       23 → 30%
        kit.AddRange(At(CastId(FamMaxMp),    (44, 1), (48, 2), (52, 3), (56, 4), (62, 5), (70, 6)));  // Soul
        kit.AddRange(At(ManaBlessing,        (58, 1), (66, 2), (72, 3)));    // −10/5 → −20/10% MP cost

        // ---- THE COMBINED SINGLES ----
        kit.AddRange(At(CastId(FamPhysDef),  (44, 3)));                      // Bulwark    +15% P.Def
        kit.AddRange(At(CastId(FamHpRegen),  (48, 4), (56, 6)));             // Vigor      15 → 20%
        kit.AddRange(At(CastId(FamMaxHp),    (44, 1), (48, 2), (52, 3), (56, 4), (64, 5), (70, 6)));  // Body
        kit.AddRange(At(CastId(FamEva),      (44, 3), (52, 4)));             // Agility    +3 → +4
        kit.AddRange(At(CastId(FamCcResMag), (40, 2), (48, 3), (56, 4)));    // Clarity    30 → 50%
        kit.AddRange(At(CastId(FamCcResPhys),(40, 1), (52, 2), (64, 3), (72, 4)));  // Fortitude 15 → 40%
        // The shield pair. ⚠ Both do NOTHING for the buffer himself unless he carries a shield —
        // they are a PERCENT of what the shield already has. They are for the tank he is blessing.
        kit.AddRange(At(CastId(FamShieldBlock), (40, 1), (48, 2), (56, 3), (62, 4), (66, 5), (70, 6)));
        kit.AddRange(At(CastId(FamShieldDef),   (58, 1), (66, 2), (72, 3)));

        // ---- FRENZY, and the "GREAT" pair that shares one key (an ally wears one, never both). ----
        kit.AddRange(At(HolyFrenzy,   (52, 2)));
        kit.AddRange(At(GreatMight,   (58, 1), (66, 2), (72, 3)));
        kit.AddRange(At(GreatBulwark, (58, 1), (66, 2), (72, 3)));

        // ---- THE PARTY ECHOES ----
        kit.Add(new ClassSkill(WarFrenzy, 56));    // Frenzy maxed at 52
        kit.Add(new ClassSkill(WcWarMight, 74));     // Great Might maxed at 72
        kit.Add(new ClassSkill(WcWarBulwark, 74));   // Great Bulwark maxed at 72

        // ---- THE NINE GROUPS ----
        kit.Add(new ClassSkill(WcWindGrace, 56));         // Swift 30, Agility 52
        kit.Add(new ClassSkill(WcFeralPrecision, 58));    // Focus 52, Ferocity 56, Aim 56
        kit.Add(new ClassSkill(WcArcaneSerenity, 70));    // Alacrity 48, Resolve 68, Serenity 56
        kit.Add(new ClassSkill(WcArcaneInsight, 72));     // Force 52, Insight 70
        kit.Add(new ClassSkill(WcBodyReinforce, 72));     // Body 70, Bulwark 44, Vigor 56
        kit.Add(new ClassSkill(WcFeralBloodlust, 74));    // Might 40, Fury 52, Vampirism 72
        kit.Add(new ClassSkill(WcSoulReinforce, 74));     // Ward 52, Soul 70, Mana Blessing 72
        kit.Add(new ClassSkill(WcShieldReinforce, 74));   // Shield Blessing 70, Shield Hardening 72
        kit.Add(new ClassSkill(WcArcaneFeralProt, 74));   // Clarity 56, Fortitude 72

        // ---- THE PARTY STEALTH, at the top. ----
        kit.Add(new ClassSkill(ShroudingHymn, 74, SkillLevel: 1));

        // ---- THE FOUR HARMONIES. Not groups: own key, cover nothing, MULTIPLY on top of the basic
        //      layer. 5 minutes on a 2-minute reuse — the buffer has to stay with the party.
        //      ⚠ Speed stops at 58 and the Wizard at 52 BY RULING, not by omission. ----
        kit.AddRange(At(NpcHarmonyWarrior,    (40, 1), (44, 2), (48, 3), (56, 4), (58, 5), (74, 6)));
        kit.AddRange(At(NpcHarmonyProtection, (44, 1), (52, 2), (56, 3), (66, 4), (74, 5)));
        kit.AddRange(At(WcHarmonySpeed,       (48, 1), (58, 2)));
        kit.AddRange(At(NpcHarmonyWizard,     (48, 1), (52, 2)));

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
            ClassSkills.RegisterThird(race, Discipline.Warchanter, kit.ToArray());

        // ═══ THE NON-BUFF HALF — his passives, actives and toggles, 40-74 ═════════════════════════
        // Built 2026-08-21 from the rows below his old `NOT DONE` banner, once he said the file was
        // finished. Defs in Skills.Warchanter3rd.Kit.cs; the two extended ladders in Skills.Healer.cs.

        // The bands his file uses. FOURTEEN = 40 44 48 52 56 58 60 62 64 66 68 70 72 74 (the full
        // 3rd-class spine); THIRTEEN is the same list with 44 dropped, which is what every damage and
        // toggle ladder in the file runs on.
        int[] band14 = { 40, 44, 48, 52, 56, 58, 60, 62, 64, 66, 68, 70, 72, 74 };
        int[] band13 = { 40, 48, 52, 56, 58, 60, 62, 64, 66, 68, 70, 72, 74 };
        int[] band8  = { 40, 48, 56, 60, 64, 68, 70, 74 };

        // Rungs 1..N of `id` at the given levels. Used for every ladder that starts at rung 1.
        static IEnumerable<ClassSkill> Ladder(string id, int[] levels, int startRung = 1) =>
            levels.Select((lv, i) => new ClassSkill(id, lv, SkillLevel: startRung + i));

        var kit2 = new List<ClassSkill>();

        // ---- Shared by all three races -----------------------------------------------------------
        // Armor Mastery and Spell Mastery CONTINUE the cleric's ladders: rungs 1-4 were bought at
        // 20-35, so the buffer's fourteen rows are rungs 5-18. Getting that offset wrong is the whole
        // difficulty here — a `ClassSkill` names the RUNG, not the row number in his file.
        // 🔴 `buffer_armor_mastery`, NOT rungs 5-18 of `armor_mastery`, since 2026-09-02 (`BL-119`).
        //    Its own id, so its own rung numbering: fourteen rungs starting at ONE.
        kit2.AddRange(Ladder(BufferArmorMastery, band14));
        kit2.AddRange(Ladder(SpellMastery, band14, startRung: 5));
        // Great Heal: ELEVEN rungs, 40-68. His file stops there; the healer's own ladder runs to 74,
        // and the extra three rungs are the Lightbringer's alone.
        kit2.AddRange(Ladder(GreatHeal, new[] { 40, 44, 48, 52, 56, 58, 60, 62, 64, 66, 68 }));
        kit2.AddRange(Ladder(WcHarmonyRestoration, band14));
        kit2.AddRange(Ladder(WcReinforcement, band13));
        kit2.AddRange(Ladder(WcSharpening, band13));
        kit2.AddRange(Ladder(WcComboMastery, new[] { 52, 64, 74 }));
        // 🔑 MANA VAMPIRISM IS ALL THREE RACES since 2026-08-29 — it was Human+Demon, which is why the
        //    elf's half of its blunt-OR-BOW gate looked pointless. His reason is the class's whole
        //    economy, not a bonus: *"it's their way of rebuffing every 5 mins with 500mp buffs (mp
        //    pots now help but not in pvp)"*. A full re-buff costs more than the pool holds, the
        //    potions are on a cooldown the pull does not wait for, and in PvP they are not an option
        //    at all — so the mana comes back through the weapon or the buffer stops buffing. The elf
        //    was the one race that could not do that. ⚠ His CSV row has always had a BLANK race
        //    column, i.e. all three; the code was the odd one out.
        kit2.AddRange(Ladder(WcManaVampirism, new[] { 40, 60, 70 }));

        // ---- HUMAN: the shield tank. Blunt + shield, ONE damage skill. ---------------------------
        var human = new List<ClassSkill>(kit2);
        human.AddRange(Ladder(WcBufferHeavy, new[] { 40 }));
        human.AddRange(Ladder(WcSoundSmash, band13));
        // The Human's own weapon line, authored 2026-09-02 — the same eight-rung band the Elf's bow
        // and the Demon's maul run on, so all three buffers finally have one.
        human.AddRange(Ladder(DoctorBluntMastery, band8));

        // ---- ELF: the archer. Light armour, bow, ranged damage, no shield and no blunt line. ------
        var elf = new List<ClassSkill>(kit2);
        elf.AddRange(Ladder(WcHarmonistLight, new[] { 40 }));
        elf.AddRange(Ladder(WcHarmonistBowProf, new[] { 40 }));
        elf.AddRange(Ladder(WcHarmonistBowMast, band8));
        elf.AddRange(Ladder(WcBowExpertise, new[] { 56 }));
        elf.AddRange(Ladder(WcSoundBurst, band13));

        // ---- DEMON: the melee fighter. Heavy armour, blunt, and TWO damage skills — his ruling,
        //      2026-08-21: *"ork is mele fighter so need more than 1dmg skill"*. Acoustic Shock is
        //      Sound Smash's twin with a stun, and it exists for exactly that reason. -------------
        var demon = new List<ClassSkill>(kit2);
        demon.AddRange(Ladder(WcBufferHeavy, new[] { 40 }));
        demon.AddRange(Ladder(WcWarlockWeapon, band8));
        demon.AddRange(Ladder(WcSoundSmash, band13));
        demon.AddRange(Ladder(WcAcousticShock, band13));

        ClassSkills.RegisterThird(Race.Human, Discipline.Warchanter, human.ToArray());
        ClassSkills.RegisterThird(Race.Elf,   Discipline.Warchanter, elf.ToArray());
        ClassSkills.RegisterThird(Race.Demon,   Discipline.Warchanter, demon.ToArray());

        // ---- SHIELD MASTERY — HUMAN ONLY, and the same skill the tank learns. ------------------
        // His `buffer 3rd.csv` rows (RACE column = Human): 40 / 60 / 70, rungs 1-3, and he gives the
        // Human Warchanter NO rung 4 — the tank's 52 is the only place that one exists. The SP is the
        // buffer's own band price (36k / 120k / 390k, the same numbers every other 40/60/70 row in his
        // file carries), which is why ClassSkill carries an SpCost override: the ability is shared, the
        // price is a property of the level you buy it at.
        //
        // 🔑 It is NOT in `kit` above because `kit` is registered for all three races — the Elf gets
        // the bow line and the Demon the blunt line in its place.
        ClassSkills.RegisterThird(Race.Human, Discipline.Warchanter,
            new ClassSkill(TankShieldMastery, 40, SkillLevel: 1, SpCost: 36_000),
            new ClassSkill(TankShieldMastery, 60, SkillLevel: 2, SpCost: 120_000),
            new ClassSkill(TankShieldMastery, 70, SkillLevel: 3, SpCost: 390_000));
    }

    /// <summary>Shield Mastery's TOP rung, at 52 — the row that in 2026-08-21 was the only authored
    /// line in <c>docs/data/classes_skills_csv/tank 3rd.csv</c>, back when the rest of the file was an
    /// empty placeholder. That is history now: the file is finished, `RegisterBulwark` owns all of it,
    /// and the only thing left here is the RETIRED Vanguard's copy of this one line.
    ///
    /// <para>⚠ It was rung 4 and is rung 7 since 2026-09-04 — 43/46/49 now sit between it and 40. The
    /// LEVEL and the PRICE are unchanged; only the index into the ladder moved.</para></summary>
    private static void RegisterTankShieldMastery()
    {
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
            foreach (var disc in new[] { Discipline.Bulwark, Discipline.Vanguard })
                ClassSkills.RegisterThird(race, disc,
                    // 🔴 BULWARK IS NO LONGER REGISTERED HERE — `RegisterBulwark` owns his whole tank
                    // file now, the whole 40-52 band alike. Only the RETIRED Vanguard keeps this
                    // line, so a character who took that discipline before it was retired (`BL-97`)
                    // still holds the rung he bought.
                    // ⚠ THE RUNG NUMBER MOVED 4 → 7 on 2026-09-04 and the LEVEL and PRICE did not:
                    // the level-52 row is still the top of Shield Mastery, it is simply the seventh
                    // rung now that 43/46/49 exist between it and 40. A stale `SkillLevel: 4` would
                    // silently demote a retired Vanguard to the level-43 payload.
                    disc == Discipline.Vanguard
                        ? new[] { new ClassSkill(TankShieldMastery, 52, SkillLevel: 7, SpCost: 74_000) }
                        : System.Array.Empty<ClassSkill>());
    }

    /// <summary>HP BOOST above 40 — the warrior's rungs L4-L10 and the buffer's L1-L7, both authored
    /// 2026-08-27. <c>warrior 3rd.csv</c> until that day read *"nothing above level 40 exists yet for
    /// this discipline — start here"*; this is what he started it with.
    ///
    /// <para>The warrior continues the L1-L3 he buys at 20/28/36 on the 2nd-class table and finishes at
    /// +1000 Max HP. The buffer starts at rung 1 twenty levels later and stops at rung 7, +700 —
    /// *"warrior gets L1~3 of the passive at 20-36 and 40+ L4~10 while buffers start L1~7 40+"*.</para>
    ///
    /// <para>⚠ This is the ONLY 3rd-class row a WARRIOR has, and it is deliberately alone. The rest of
    /// Ravager/Warlord is unauthored and stays that way until he writes it — one authored line is not
    /// permission to invent a kit around it. Same standing as <see cref="RegisterTankShieldMastery"/>.</para>
    ///
    /// <para>⚠ The buffer's SP prices are HIS 3rd-class ladder, so every buffer rung carries an explicit
    /// <see cref="ClassSkill.SpCost"/>. The SkillDef's own prices are the warrior's — rung 1 is 3,400
    /// there against the buffer's 36,000, so without the override he would buy it at a tenth of price.
    /// Both band lists are irregular, and irregular is correct.</para></summary>
    private static void RegisterHpBoost()
    {
        int[] warriorBands = { 43, 49, 55, 62, 66, 70, 74 };
        int[] bufferBands = { 40, 44, 48, 52, 56, 62, 70 };
        int[] bufferSp = { 36_000, 43_000, 64_000, 74_000, 81_000, 170_000, 390_000 };
        // 🔑 THE TWO WARRIOR DISCIPLINES PRICE THE SAME SEVEN RUNGS DIFFERENTLY, and only on the first
        // two: `warrior 3rd.csv` (Ravager) reads 35k/50k where `war_aoe 3rd.csv` (Warlord) reads
        // 42k/65k, which is also the SkillDef's own ladder. Both files agree from rung 6 up. Same
        // payload, two prices — so an SpCost override, never a second SkillDef.
        int[] ravagerSp = { 35_000, 50_000, 80_000, 170_000, 280_000, 390_000, 880_000 };
        int[] warlordSp = { 42_000, 65_000, 80_000, 170_000, 280_000, 390_000, 880_000 };

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
        {
            foreach (var (disc, sp) in new[] { (Discipline.Ravager, ravagerSp), (Discipline.Warlord, warlordSp) })
                ClassSkills.RegisterThird(race, disc,
                    warriorBands.Select((lvl, i) => new ClassSkill(HpBoost, lvl, SkillLevel: i + 4, SpCost: sp[i]))
                                .ToArray());

            ClassSkills.RegisterThird(race, Discipline.Warchanter,
                bufferBands.Select((lvl, i) => new ClassSkill(HpBoost, lvl, SkillLevel: i + 1,
                                                              SpCost: bufferSp[i]))
                           .ToArray());
        }
    }

    /// <summary>THE LIGHTBRINGER, 40-74 — every row of <c>docs/data/classes_skills_csv/healer 3rd.csv</c>,
    /// built 2026-08-20 when he said go. The first discipline in the game that is authored end to end.
    ///
    /// <para>🔑 <b>THE BANDS ARE HIS FILE'S SPINE</b>: 40, 44, 48, 52, then 56, 58, 60, 62, 64, 66, 68,
    /// 70, 72, 74. The stride HALVES at 56 — a 3rd class levels slowly enough by then that four levels
    /// between rungs would be most of an evening — which is why a ladder here is fourteen rungs and not
    /// nine, and why <c>Band(i)</c> exists rather than <c>40 + i * 4</c>.</para>
    ///
    /// <para>🔑 <b>WHAT IS NOT HERE.</b> Five invented skills the discipline used to teach — Blessing of
    /// Light, Devotion, Purify, Warding Step and Soul Sap — are on none of his rows and are no longer
    /// granted by anybody. Their DEFS survive in the catalog (deleting one orphans every character who
    /// bought it), which is the same treatment the 2026-08-10 purge gave the fighter kits. Do not put
    /// them back: *"Anything that's not inside the csv should not exist except the class balance."*</para>
    ///
    /// <para>⚠ The two masteries and Frenzy L2 used to live in a narrow <c>RegisterHealerMasteries()</c>,
    /// because this function was commented out while his 44+ rows were still being drafted. That helper
    /// is GONE — its rungs are in the shared ladder below. Registering both would have taught every rung
    /// twice.</para></summary>

    /// <summary>THE NUKER, 40-74 — every row of `docs/data/classes_skills_csv/nuker 3rd.csv`.
    ///
    /// <para>🔑 <b>ONE DISCIPLINE — THE MAGUS — SINCE 2026-08-28 (`BL-97`).</b> His file carries no
    /// discipline column, so while the archetype had two branches this kit was registered to BOTH of
    /// them, identically. He then ruled *"Tempests must go"*, and because the two were holding the very
    /// same 208 rows the retirement deleted a duplicate registration and not one authored row. What the
    /// kit's two halves were once read as — Elemental Wave and Arcane Wave the "AoE" shape, Elemental
    /// Blast and the bursts the single-target one — is now simply what ONE nuker can do. The three
    /// identities are the RACES below, which is what he asked for: *"1 discipline ... 3 identities"*.</para>
    ///
    /// <para>🔑 <b>THE RACE SPLITS IT, and unlike the Lightbringer's it splits FOUR ways, not two:</b>
    /// Human takes Arcane Wave / Vampiric Bolt / Arcane Void / Arcane Burst, Elf takes Frost Spikes /
    /// Frost Pierce / Frost Burst, Demon takes Witches Curse / Witches Scarecrow / Pyro Burst. Eleven
    /// families are shared. The Human's four vs the other two's three is his authoring, not a slip —
    /// Arcane Void is a utility cast, not a damage one.</para>
    ///
    /// <para>🔴 <b>CALM SPIRIT IS NOT REGISTERED.</b> Its six rows (@40/48/56/62/68/74) multiply MP
    /// regen by 0.3 → 0.7 while RUNNING and 1.03 → 1.2 while WALKING, on top of the engine's own stance
    /// multipliers (run ×1.0, walk ×1.2). That makes a mage who learns it regenerate LESS while running
    /// than one who never did — which is exactly what he intends (*"a farming mage will click walking,
    /// and in pvp need to click run but regen slower"*), but he asked to hold it while the wider MP-regen
    /// question is open (`BL-92`, *"ours is several times more than IG"*). Owner, 2026-08-26: *"w8 on
    /// calm spirit"*. The rows stay in his file and `--check` will report the family as NOT REGISTERED
    /// until he says go; that is the flag working, not a defect.</para></summary>
    private static void RegisterNuker3rd()
    {
        // The same fourteen bands and the same SP column as the healer and the buffer — this IS the
        // mage 3rd tier's cadence, and all three of his files were authored against it.
        int[] band14 = { 40, 44, 48, 52, 56, 58, 60, 62, 64, 66, 68, 70, 72, 74 };

        static IEnumerable<ClassSkill> Ladder(string id, int[] levels, int startRung = 1) =>
            levels.Select((lv, i) => new ClassSkill(id, lv, SkillLevel: startRung + i));

        static IEnumerable<ClassSkill> At(string id, params (int Level, int Rung)[] rows) =>
            rows.Select(r => new ClassSkill(id, r.Level, SkillLevel: r.Rung));

        var shared = new List<ClassSkill>();

        // ---- The three passives. NONE of them is a new skill: two are shared outright with the
        //      healer's kit and the third is the nuker's own, continued.
        //      • Anti-Magic continues the mage ladder — rungs 1-6 were bought at 20-35, so his
        //        fourteen rows are rungs 7-20, and those rungs are already in the def
        //        (`HealerAntiMagicRungs`, his own "one shared ladder for all three files").
        //      • Spellcaster Weapon Mastery IS the healer's skill. Its fourteen rungs matched this
        //        file's rows to the last digit — see the note on HealerWeaponMasterySkill, which
        //        predicted exactly that. `Replaces` retires the nuker's Spell Mastery for him.
        //      • Mage Armor Mastery is the nuker's own (rungs 5-18) because it alone carries
        //        mpWhenRestored, and because his @48 P.Def differs from the healer's by 3.
        shared.AddRange(Ladder(MageAntiMagic, band14, startRung: 7));
        shared.AddRange(Ladder(HealerWeaponMasterySkill, band14));
        shared.AddRange(Ladder(NukerArmorMastery, band14, startRung: 5));

        // ---- The FOURTH passive. Calm Spirit was authored with the rest of this file and deliberately
        //      held on 2026-08-26 (*"w8 on calm spirit"*) because the stance model it needs did not
        //      exist yet — we had no standing state at all. `BL-92` built that, so it lands now. Its
        //      six rungs are his own irregular levels, not the fourteen-band the rest of the file uses.
        shared.AddRange(At(CalmSpirit, (40, 1), (48, 2), (56, 3), (62, 4), (68, 5), (74, 6)));

        // ---- The two mana tools. Restore Spirit continues from the 2nd class (rung 1 @25), which is
        //      why it starts at rung 2; Phase Shift is a fresh three-rung ladder whose ladder is its
        //      distance.
        shared.AddRange(At(RestoreSpirit, (40, 2), (52, 3), (58, 4), (66, 5)));
        shared.AddRange(At(PhaseShift, (52, 1), (62, 2), (72, 3)));

        // ---- The shared attack spells. Elemental Blast and Quick Blast REPLACE the 2nd-class bolts
        //      (see the purge in ClassSkillTables.Common.cs — their 40+ half was ours, not his).
        shared.AddRange(Ladder(ElementalBlast, band14));
        shared.AddRange(Ladder(QuickBlast, band14));
        shared.AddRange(Ladder(ElementalWave, band14));

        // ---- The two five-minute nukes. Both eat Elemental Stones; Thunderstorm's five-second cast
        //      is the price of its 216 power.
        shared.AddRange(At(ElementalBurst, (58, 1), (66, 2), (74, 3)));
        shared.AddRange(At(Thunderstorm,   (62, 1), (70, 2), (74, 3)));

        // ═══ THE RACE SPLIT ══════════════════════════════════════════════════════════════════════
        var human = new List<ClassSkill>(shared);
        human.AddRange(Ladder(ArcaneWave, band14));
        // Vampiric Bolt continues from the 2nd class too — rungs 1-5 were bought at 14-35, so his
        // fourteen 3rd-class rows are rungs 6-19.
        human.AddRange(Ladder(VampiricBolt, band14, startRung: 6));
        human.AddRange(At(ArcaneVoid, (52, 1), (62, 2), (72, 3)));
        human.Add(new ClassSkill(ArcaneBurst, 74));

        var elf = new List<ClassSkill>(shared);
        elf.AddRange(Ladder(FrostSpikes, band14));
        elf.AddRange(Ladder(FrostPierce, band14));
        elf.Add(new ClassSkill(FrostBurst, 74));

        var demon = new List<ClassSkill>(shared);
        demon.AddRange(Ladder(WitchesCurse, band14));
        demon.AddRange(Ladder(WitchesScarecrow, band14));
        demon.Add(new ClassSkill(PyroBurst, 74));

        // ONE discipline now (`BL-97`, 2026-08-28). This used to loop over Magus AND Tempest, which
        // is exactly why retiring the Tempest lost nothing: the two were being handed the identical
        // array. The race is what splits this kit, and it always was.
        ClassSkills.RegisterThird(Race.Human, Discipline.Magus, human.ToArray());
        ClassSkills.RegisterThird(Race.Elf,   Discipline.Magus, elf.ToArray());
        ClassSkills.RegisterThird(Race.Demon,   Discipline.Magus, demon.ToArray());
    }
    private static void RegisterLightbringer()
    {
        // His fourteen learn levels, and a rung index → level lookup so every ladder below reads as
        // "one rung per band" rather than as fourteen hand-written numbers that can drift.
        int[] band = SkillCatalog.HealerBands;
        int Band(int i) => band[i];

        // A full 14-rung ladder: skill level i+1 at band i. The shape of almost everything he wrote.
        ClassSkill[] Full(string skill, int startBand = 0, int startLevel = 1) =>
            Enumerable.Range(0, band.Length - startBand)
                .Select(i => new ClassSkill(skill, Band(startBand + i), SkillLevel: startLevel + i))
                .ToArray();

        // A ladder that appears only at some bands — his buffs, the cures, the four-rung debuffs.
        // Pairs are (character level, skill level), read straight off the rows.
        ClassSkill[] At(string skill, params (int Level, int Rung)[] rows) =>
            rows.Select(r => new ClassSkill(skill, r.Level, SkillLevel: r.Rung)).ToArray();

        // ---- THE SHARED KIT. Everything below is learned by all three races; only the fast heal and
        //      the control debuff differ, and those are the three blocks at the bottom. ----
        var shared = new List<ClassSkill>();

        // --- The three passives, one rung per band. Anti-Magic continues the cleric's ladder (rung 7
        //     is his @40 row); the two masteries REPLACE the cleric's pair rather than continuing it,
        //     which is the whole point of the 2026-08-20 split — the healer's kit is a wand and a robe,
        //     and the BUFFER is the caster who keeps the sword half and the light-armor row.
        shared.AddRange(Full(MageAntiMagic, startLevel: 7));
        shared.AddRange(Full(HealerWeaponMasterySkill));
        shared.AddRange(Full(HealerArmorMasterySkill));

        // --- The nuke and the two ordinary heals, one rung per band. Each replaces its 2nd-class
        //     original (Holy Bolt / Heal / Party Heal), so the bar does not fill with obsolete rows.
        shared.AddRange(Full(HolyRay));
        shared.AddRange(Full(GreatHeal));
        shared.AddRange(Full(PartyGreatHeal));

        // --- Restore Mana and Resurrection continue the cleric's ladders (both are at rung 3 by 40).
        shared.AddRange(Full(Resurrection, startLevel: 3));
        shared.AddRange(Full(RestoreMana, startLevel: 3));

        // --- The 44+ heals. Urgent Heal has FOUR rungs and then stops for good at 56: it heals a
        //     PERCENTAGE of the target's own bar, so it never needs another one. The two Ultimates
        //     start at 58 and run to 74, and cost Skill Stones rather than a bigger number.
        shared.AddRange(Full(UrgentHeal, startBand: 1).Take(4).ToArray());
        shared.AddRange(Full(UltimateHeal, startBand: 5));
        shared.AddRange(Full(UltimatePartyHeal, startBand: 5));

        // --- Resurrection Field: four rungs, deliberately far apart. Antidote's ceiling climbs on its
        //     own schedule (his rows, not a band per rung).
        shared.AddRange(At(ResurrectionField, (44, 1), (58, 2), (66, 3), (74, 4)));
        shared.AddRange(At(Antidote, (44, 3), (52, 4), (58, 5), (62, 6), (66, 7), (70, 8), (74, 9)));

        // --- The mana kit. Mana Ray from 56, Mana Strain from 52, Meditation on four rungs.
        shared.AddRange(Full(ManaRay, startBand: 4));
        shared.AddRange(Full(ManaStrain, startBand: 3));
        shared.AddRange(At(Meditation, (56, 1), (60, 2), (64, 3), (68, 4)));
        shared.AddRange(At(WeaponBreak, (62, 1), (66, 2), (70, 3), (74, 4)));

        // --- Conceal: one rung at 40, the self-only twin of the buffer's party stealth.
        shared.Add(new ClassSkill(Conceal, 40));

        // ═══ THE BUFF ROWS ═══════════════════════════════════════════════════════════════════════
        // 🔑 EVERY ONE OF THESE IS A RUNG OF A FAMILY, not a skill of its own. His row says "learn
        // Ferocity at 48 for +30% crit damage"; the family ladder in Skills.BuffLadders.cs holds the
        // number and this table holds only WHICH rung and WHEN. That is what stops a healer's Ferocity
        // and a Ferocity scroll from stacking — they are literally the same buff.
        //
        // ⚠ SIX of these indices moved on 2026-08-20 because his file authored values BETWEEN existing
        // rungs (M.Atk 28%, M.Def 23%, +3 accuracy, +3 evasion, 7/8% vampirism, 36/42/48 interrupt).
        // Read the comment, not the number: `SkillLevel: 5` on Focus is +25%, which is his 44 row.
        // ⚠ INTERRUPT MOVED AGAIN on 2026-08-21: his 68 row authored +54, between 48 and the old top of
        // 60 — and he then CAPPED the family there (*"54 is max resolve for now"*), so 60 left the ladder
        // altogether. Resolve is SEVEN rungs, rung 7 is 54, and everyone who used to get 60 now gets 54.
        shared.AddRange(At(CastId(FamPhysAtk),   (40, 3)));                     // Might      15%
        shared.AddRange(At(CastId(FamPhysDef),   (44, 3)));                     // Bulwark    15%
        shared.AddRange(At(CastId(FamMagAtk),    (44, 3), (52, 4)));            // Force      28 → 32%
        shared.AddRange(At(CastId(FamMagDef),    (44, 3), (52, 4)));            // Ward       23 → 30%
        shared.AddRange(At(CastId(FamAccuracy),  (40, 2), (48, 3), (56, 4)));   // Aim        +2 → +4
        shared.AddRange(At(CastId(FamEva),       (44, 3), (52, 4)));            // Agility    +3 → +4
        shared.AddRange(At(CastId(FamAs),        (44, 1), (52, 3)));            // Fury       15 → 33%
        // ⚠ ALACRITY WAS MISSING until 2026-08-21 (owner: *"have forgoten on healer the cast speed
        // buff"*), and its absence was invisible because the cleric already teaches rungs 1-2 — the
        // healer simply never finished the family. His row is rung 3 verbatim, so nothing new was
        // authored: `ClassSkillTables.Common.cs` already said cast speed past L2 is a 3rd-class reward.
        shared.AddRange(At(CastId(FamCast),      (48, 3)));                     // Alacrity   +30% cast
        shared.AddRange(At(CastId(FamVamp),      (44, 3), (58, 4), (72, 5)));   // Vampirism  7 → 9%
        shared.AddRange(At(CastId(FamInterrupt), (44, 3), (52, 5), (60, 6), (68, 7)));  // Resolve  36 → 54
        shared.AddRange(At(CastId(FamCritRate),  (44, 5), (52, 6)));            // Focus      25 → 30%
        shared.AddRange(At(CastId(FamCritDmg),   (40, 4), (48, 5), (56, 6)));   // Ferocity   25 → 35%
        shared.AddRange(At(CastId(FamMagCrit),   (62, 3), (70, 6)));            // Insight    50 → 100%
        shared.AddRange(At(CastId(FamMaxHp),     (44, 1), (48, 2), (52, 3), (56, 4), (64, 5), (70, 6)));  // Body
        shared.AddRange(At(CastId(FamMaxMp),     (44, 1), (48, 2), (52, 3), (56, 4), (62, 5), (70, 6)));  // Soul
        shared.AddRange(At(CastId(FamHpRegen),   (48, 4), (56, 6)));            // Vigor      15 → 20%
        shared.AddRange(At(CastId(FamMpRegen),   (40, 2), (48, 4), (56, 6)));   // Serenity   10 → 20%
        shared.AddRange(At(CastId(FamCcResMag),  (40, 2), (48, 3), (56, 4)));   // Clarity    30 → 50%
        shared.AddRange(At(CastId(FamCcResPhys), (40, 1), (52, 2), (64, 3), (72, 4)));  // Fortitude 15 → 40%
        // The shield pair. ⚠ These do NOTHING for a healer holding no shield (both are a PERCENT of
        // what the shield already carries, and 0 × 1.5 is 0) — they are for the tank he is blessing.
        shared.AddRange(At(CastId(FamShieldBlock), (40, 1), (48, 2), (56, 3), (62, 4), (66, 5), (70, 6)));
        shared.AddRange(At(CastId(FamShieldDef),   (58, 1), (66, 2), (72, 3)));
        // Frenzy L2 @52 — his ruling, and the SAME rung for healer and buffer: *"Frenzy(L2) is learned
        // from Healers and Buffers at 52"*. The cleric's L1 at 35 is the rung below it.
        shared.Add(new ClassSkill(HolyFrenzy, 52, SkillLevel: 2));
        // Mana Blessing and the "Great" pair: three rungs each, at 58 / 66 / 74. Great Might and Great
        // Bulwark share a buff key and therefore EXCLUDE each other — an ally wears one, never both.
        shared.AddRange(At(ManaBlessing, (58, 1), (66, 2), (72, 3)));
        shared.AddRange(At(GreatMight,   (58, 1), (66, 2), (72, 3)));
        shared.AddRange(At(GreatBulwark, (58, 1), (66, 2), (72, 3)));

        // ═══ THE RACE SPLIT — it happens TWICE, and only twice ════════════════════════════════════
        // Once on the fast heal (Human throughput / Elf heal-and-cure / Demon planted totem) and once on
        // the control debuff (Gravity / Bind / Armor Break). Both are full 14-rung ladders, and the Demon
        // carries a third: the Mana Totem, from 52.
        ClassSkills.RegisterThird(Race.Human, Discipline.Lightbringer,
            shared.Concat(Full(LbHumanMend)).Concat(Full(LbHumanGravity)).ToArray());
        ClassSkills.RegisterThird(Race.Elf, Discipline.Lightbringer,
            shared.Concat(Full(LbElfDawn)).Concat(Full(LbElfBind)).ToArray());
        ClassSkills.RegisterThird(Race.Demon, Discipline.Lightbringer,
            shared.Concat(Full(LbOrkFont)).Concat(Full(LbOrkArmorBreak))
                  .Concat(Full(ManaTotem, startBand: 3)).ToArray());
    }

    // Warchanter (Healer B) — buffer: per-race DMG + party mega-buff + party HoT + passive.
    private static void RegisterWarchanter()
    {
        // Mage 3rd-class learn cadence: 40, 44, 48, 52.
        ClassSkills.RegisterThird(Race.Human, Discipline.Warchanter,
            new ClassSkill(WcHumanBolt, 40), new ClassSkill(WcHumanChant, 44),
            new ClassSkill(WcHumanRenew, 48), new ClassSkill(WcHumanPass, 52));
        ClassSkills.RegisterThird(Race.Elf, Discipline.Warchanter,
            new ClassSkill(WcElfBolt, 40), new ClassSkill(WcElfChant, 44),
            new ClassSkill(WcElfRenew, 48), new ClassSkill(WcElfPass, 52));
        ClassSkills.RegisterThird(Race.Demon, Discipline.Warchanter,
            new ClassSkill(WcOrkBolt, 40), new ClassSkill(WcOrkChant, 44),
            new ClassSkill(WcOrkRenew, 48), new ClassSkill(WcOrkPass, 52));
    }

    /// <summary>`BL-185` — THE WARRIOR'S AND THE ARCHER'S 3rd-CLASS DAMAGE KITS, 40-74.
    ///
    /// <para>⚠ The 40+ purge at the top of this file still stands. This is an entry under the same
    /// narrow terms as <see cref="RegisterHideKit"/> and <see cref="RegisterHpBoost"/> — he named the
    /// skills himself — with one difference worth stating plainly: he gave a DERIVATION, not a file.
    /// *"For archer take the elf harmonist skills and bow passives ... Increase them with ~20% and
    /// you'll get the dmg part of the archer kit"* / *"For fighter kit take demon harmonist skills and
    /// 2h wepon passives increase them by ~20%"*, 2026-09-06, said in the same breath as *"I'll try
    /// next week to finish the csvs"*. So these rows are explicitly PROVISIONAL: when
    /// `warrior 3rd.csv` and the archer's file arrive they overwrite every number here.</para>
    ///
    /// <para>🔑 WHY IT COULD NOT WAIT FOR THE FILES. `--dmgmatrix` measured our archer hitting a
    /// buffed level-90 mage for 235 where the ELF HARMONIST — a BUFFER — hit the same target for 495.
    /// Both damage kits were empty: the three ranged disciplines had ONE 3rd-class skill between them
    /// (Signal Flare) and the warrior had only HP Boost. See docs/balance/DamageVsIG.md.</para>
    ///
    /// <para>🔑 THE ARMOUR LINES ARE RUNGS 6-20 OF THE 2nd-CLASS MASTERY, not new skills — the tank's
    /// idiom. That is why they carry <c>startRung: 6</c> and why the archer's rungs are registered on
    /// the RANGED disciplines only: a melee rogue shares the skill id and simply never learns past
    /// rung 5. See the header of Skills.FighterKits3rd.cs for why a separate skill would have either
    /// double-granted crit-rate resistance or deleted the light branch.</para>
    ///
    /// <para>⚠ HP Boost is NOT here and owes nothing: <see cref="RegisterHpBoost"/> already gives the
    /// warrior rungs 4-10, i.e. up to +1000 max HP, which is exactly what he asked for.</para></summary>
    private static void RegisterWarriorAndArcherKits()
    {
        // The Warchanter's own band, so the ONE surviving derived ladder learns on the same levels as
        // its source. (`band8` and `armour15` went with the sword mastery and the armour rungs.)
        int[] band13 = { 40, 48, 52, 56, 58, 60, 62, 64, 66, 68, 70, 72, 74 };

        static IEnumerable<ClassSkill> Ladder(string id, int[] levels, int startRung = 1) =>
            levels.Select((lv, i) => new ClassSkill(id, lv, SkillLevel: startRung + i));

        // 🔴 EVERYTHING BUT THIS ONE LADDER IS GONE, 2026-09-11 — his `warrior 3rd.csv` and
        //    `war_aoe 3rd.csv` landed and RegisterWarrior3rd() below teaches his rows instead. The
        //    derived sword mastery and the tank-copy armour rungs are deleted; see the header of
        //    Skills.FighterKits3rd.cs. Sundering Blow stays because his files carry the passives and
        //    buffs and *"are missing only teir dmg and control (active dmg) skills"* — so the DAMAGE
        //    half is still owed, and this is what stands in for it.
        // ⚠ It is registered on BOTH disciplines, which is a stand-in's shape and not his: when his
        //   damage rows land they will almost certainly split sword from blunt like everything else in
        //   those two files does.
        var warrior = new List<ClassSkill>();
        warrior.AddRange(Ladder(WarSunderingBlow, band13));

        // 🔴 THE ARCHER'S HALF IS GONE, 2026-09-09 — `archer 3rd.csv` landed and RegisterArcher3rd()
        //    below teaches his rows instead. The four derived skills it used to register are orphaned
        //    in Skills.ArcherKitRetired.cs and retired by `Replaces` on their authored successors.
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
            foreach (var d in new[] { Discipline.Ravager, Discipline.Warlord })
                ClassSkills.RegisterThird(race, d, warrior.ToArray());
    }

    /// <summary>THE WARRIOR'S TWO DISCIPLINES, 40-74 — every row of `warrior 3rd.csv` (the RAVAGER)
    /// and `war_aoe 3rd.csv` (the WARLORD), 2026-09-11. The numbers are in Skills.Warrior3rd.cs; this
    /// is which of them each discipline learns, and when.
    ///
    /// <para>🔑 <b>THE WEAPON IS THE SPLIT.</b> Ravager = two-handed SWORD + the two Battle stances;
    /// Warlord = two-handed BLUNT whose basic attack CLEAVES, no stances, and two extra rungs of
    /// Battle Regeneration instead. Everything else is shared rung for rung because both files author
    /// it identically.</para>
    ///
    /// <para>🔑 <b>WHERE THE TWO FILES DISAGREE IT IS ONLY THE PRICE OR THE LEVEL</b>, never the
    /// payload — so those ride as <see cref="ClassSkill.SpCost"/> overrides rather than as a second
    /// SkillDef. Three skills need one: HP Boost and HP Regeneration (the Ravager's first two rungs
    /// are cheaper) and Warrior's Strength (rung 3 is 46/40k for him, 52/74k for the Warlord).</para>
    ///
    /// <para>⚠ HP Boost's LEVELS are already right for both — <see cref="RegisterHpBoost"/> gives every
    /// warrior discipline rungs 4-10 at 43-74, which is both files' column. Only two of its SP cells
    /// move.</para></summary>
    private static void RegisterWarrior3rd()
    {
        int[] band15 = SkillCatalog.Warrior3rdLevels;
        int[] sp15   = SkillCatalog.Warrior3rdSp;
        int[] regen7 = { 43, 49, 55, 62, 66, 70, 74 };   // HP Regeneration, both files' column
        // Same story as HP Boost: one payload, two prices, differing on the first two rungs only.
        int[] regenSpRavager = { 28_000, 50_000, 80_000, 170_000, 280_000, 390_000, 880_000 };
        int[] regenSpWarlord = { 42_000, 65_000, 80_000, 170_000, 280_000, 390_000, 880_000 };

        static IEnumerable<ClassSkill> Ladder(string id, int[] levels, int startRung = 1) =>
            levels.Select((lv, i) => new ClassSkill(id, lv, SkillLevel: startRung + i));

        static IEnumerable<ClassSkill> PricedLadder(string id, int[] levels, int[] sp) =>
            levels.Select((lv, i) => new ClassSkill(id, lv, SkillLevel: i + 1, SpCost: sp[i]));

        // ---- What both disciplines learn, identically. ----
        var shared = new List<ClassSkill>();
        // Armour Mastery rungs 6-20 — APPENDED to the 2nd-class ladder, same id (the tank's idiom).
        shared.AddRange(Ladder(WarriorArmorMastery, band15, startRung: 6));
        // Final Stand — the P.Atk twin of the tank's Final Defense, three rungs on the same levels.
        shared.AddRange(Ladder(WarriorFinalStand, new[] { 40, 52, 60 }));
        // Battle Regeneration rungs 2-4 (15/20/25%). The Warlord takes two more, below.
        shared.AddRange(Ladder(BattleRegeneration, new[] { 40, 49, 58 }, startRung: 2));
        // Battle Resilience rungs 2-3 and Monster Knowledge rungs 2-6, both files' columns.
        shared.AddRange(Ladder(BattleResilience, new[] { 49, 62 }, startRung: 2));
        shared.AddRange(Ladder(MonsterKnowledgeActive, new[] { 40, 46, 52, 58, 64 }, startRung: 2));
        // `BL-191` — OVERPOWER rung 2 (7%) at 40, his *"3,7,10% @20,40,76"*. Rung 1 is on the
        // 2nd-class table, rung 3 on the 4th. Both his files carry the row.
        shared.Add(new ClassSkill(Overpower, 40, SkillLevel: 2));

        // ---- THE RAVAGER: the sword, the stances, and the cheaper early prices. ----
        var ravager = new List<ClassSkill>(shared);
        ravager.AddRange(PricedLadder(WarriorSwordMastery, band15, sp15));
        ravager.AddRange(PricedLadder(WarriorHpRegeneration, regen7, regenSpRavager));
        // Warrior's Strength rungs 2-4 at 40/46/52. ⚠ The Warlord takes only 2-3, and takes rung 3
        //   twelve levels later for nearly twice the SP — hence two lists rather than one.
        ravager.Add(new ClassSkill(WarriorStrength, 40, SkillLevel: 2, SpCost: 28_000));
        ravager.Add(new ClassSkill(WarriorStrength, 46, SkillLevel: 3, SpCost: 40_000));
        ravager.Add(new ClassSkill(WarriorStrength, 52, SkillLevel: 4, SpCost: 74_000));
        // The two Battle stances — the Ravager's alone; `war_aoe 3rd.csv` has neither.
        ravager.Add(new ClassSkill(BattlePresence, 46, SkillLevel: 2));
        ravager.Add(new ClassSkill(BattlePresence, 55, SkillLevel: 3));
        ravager.Add(new ClassSkill(BattleDefence, 43, SkillLevel: 2));
        ravager.Add(new ClassSkill(BattleDefence, 52, SkillLevel: 3));
        // ⚠ HP Boost's two cheaper cells are NOT here — they are in RegisterHpBoost, which owns that
        //   ladder for every class that learns it. An override added beside a ladder rather than ON
        //   it registers the rung TWICE, and the second row's price is not the one anything reads.

        // ---- THE WARLORD: the blunt that cleaves, and the longer Battle Regeneration. ----
        var warlord = new List<ClassSkill>(shared);
        warlord.AddRange(PricedLadder(WarriorBluntMastery, band15, sp15));
        warlord.AddRange(PricedLadder(WarriorHpRegeneration, regen7, regenSpWarlord));
        warlord.Add(new ClassSkill(WarriorStrength, 40, SkillLevel: 2, SpCost: 28_000));
        warlord.Add(new ClassSkill(WarriorStrength, 52, SkillLevel: 3, SpCost: 74_000));
        // Rungs 5-6 (30/35%) — the two levels the Ravager spends on Battle Presence instead.
        warlord.Add(new ClassSkill(BattleRegeneration, 64, SkillLevel: 5));
        warlord.Add(new ClassSkill(BattleRegeneration, 70, SkillLevel: 6));

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
        {
            ClassSkills.RegisterThird(race, Discipline.Ravager, ravager.ToArray());
            ClassSkills.RegisterThird(race, Discipline.Warlord, warlord.ToArray());
        }
    }

    /// <summary>THE MELEE ROGUE, 40-74 — every row of `dual 3rd.csv` (2026-09-09). See
    /// Skills.Dual3rd.cs for the numbers and for what each family is.
    ///
    /// <para>🔑 <b>ONE DISCIPLINE PER RACE, so the race column IS the discipline.</b> Unlike the tank —
    /// one Bulwark shared by three races, where a RACE has to be named — the melee rogue split by race
    /// at 40 back at the archer merge: Nullblade is the Human, Phantom the Elf, Venomweaver the Demon.
    /// So his `Human;Elf` cell on Killing Stab means Nullblade and Phantom, and nothing else.</para>
    ///
    /// <para>⚠ Prowl, Vanish and Lure are NOT here — they are <see cref="RegisterHideKit"/>'s, which he
    /// placed by hand and which now carries Lure's full three-rung ladder.</para></summary>
    private static void RegisterDual3rd()
    {
        int[] band15 = { 40, 43, 46, 49, 52, 55, 58, 60, 62, 64, 66, 68, 70, 72, 74 };
        int[] jump3  = { 52, 60, 74 };
        int[] cure6  = { 52, 58, 62, 66, 70, 74 };
        int[] focus3 = { 40, 60, 70 };   // `BL-188` — the race-split blow buff

        static IEnumerable<ClassSkill> Ladder(string id, int[] levels, int startRung = 1) =>
            levels.Select((lv, i) => new ClassSkill(id, lv, SkillLevel: startRung + i));

        // What all three melee disciplines share. ⚠ Armor Mastery is rungs 6-20 of the 2nd-class
        // ladder (APPENDED, same id); Dual Mastery is a NEW skill that REPLACES the rogue's weapon
        // mastery outright — his two rows say so, and the difference is explained on the defs.
        var shared = new List<ClassSkill>();
        shared.AddRange(Ladder(RogueArmorMastery, band15, startRung: 6));
        shared.AddRange(Ladder(DualWeaponMastery, band15));
        shared.Add(new ClassSkill(Sprint, 46, SkillLevel: 2));
        shared.Add(new ClassSkill(EvasionBoost, 60, SkillLevel: 2));
        // `BL-188` — Vital Points, the shared blow-rate passive. THREE rungs at 52/64/74, and the
        // one part of the blow ladder that is NOT race-split.
        shared.AddRange(Ladder(VitalPoints, new[] { 52, 64, 74 }));
        // `BL-203` — OVERPOWER, the melee rogue's, and it is the WARRIOR'S LADDER ONE RUNG SHORT.
        // Owner, 2026-09-11: *"give duals 3rd/4th stabs a [double] flag and overpower passive but
        // least than warrior @40 3% @76-7% (1 rung less. If it's too low I'll give him the last rung
        // at 80 - for now only the 2 rungs)"*.
        //
        // 🔑 SAME DEF, SAME RUNGS, LATER LEVELS — `SkillLevel: 1` is the 3% the warrior buys at 20 and
        //    `SkillLevel: 2` (in the 4th table) the 7% he buys at 40. Nothing is authored twice: the
        //    ladder is one array in Skills.SkillMasteries.cs and the class table decides who reaches
        //    which rung and when.
        // ⚠ `SpCost` OVERRIDES THE RUNG'S PRICE, and it has to: rung 1 costs 3,400 because that is
        //   what a level-20 warrior pays, and SP in this game is priced by the LEVEL YOU LEARN AT, not
        //   by the ability (see ClassSkill.SpCost). A melee rogue meets it at 40, where every first
        //   rung in `dual 3rd.csv` costs 28,000 — which is also the only price his `SP COST (x1000)`
        //   column can even spell.
        // ⚠ THE TOP RUNG (10%) IS DELIBERATELY UNREACHABLE for this class. If he adds it, it is one
        //   line at 80 with `SkillLevel: 3` in the 4th table and one CSV row — not a new ladder.
        shared.Add(new ClassSkill(Overpower, 40, SkillLevel: 1, SpCost: RogueSp[0]));

        var human = new List<ClassSkill>(shared);
        human.AddRange(Ladder(KillingStab, band15));
        human.AddRange(Ladder(HeavyStab, band15));
        human.AddRange(Ladder(PhantomJumpHuman, jump3));
        human.AddRange(Ladder(LethalFocus, focus3));       // `BL-188` — half rate, half crit damage

        var elf = new List<ClassSkill>(shared);
        elf.AddRange(Ladder(KillingStab, band15));
        elf.AddRange(Ladder(SwiftStab, band15));
        elf.AddRange(Ladder(PhantomJumpElf, jump3));
        elf.AddRange(Ladder(LethalPrecision, focus3));     // `BL-188` — all crit damage (the Elf already leads on AGI)
        elf.AddRange(Ladder(ElfAntidote, cure6));

        // ⚠ THE DEMON GETS NO KILLING STAB. His RACE cell on it is `Human;Elf`, and Venom Stab +
        //   Venom Burst are the whole of the Venomweaver's damage instead — half the power per blow,
        //   banked and spent. That is the largest thing race decides in this file.
        var demon = new List<ClassSkill>(shared);
        demon.AddRange(Ladder(VenomStab, band15));
        demon.AddRange(Ladder(VenomBurst, band15));
        demon.AddRange(Ladder(PhantomJumpDemon, jump3));
        demon.AddRange(Ladder(LethalFrenzy, focus3));      // `BL-188` — all rate (the Demon trails on AGI)

        ClassSkills.RegisterThird(Race.Human, Discipline.Nullblade,   human.ToArray());
        ClassSkills.RegisterThird(Race.Elf,   Discipline.Phantom,     elf.ToArray());
        ClassSkills.RegisterThird(Race.Demon, Discipline.Venomweaver, demon.ToArray());
    }

    /// <summary>THE ARCHER, 40-74 — every row of `archer 3rd.csv` (2026-09-09). See
    /// Skills.Archer3rd.cs.
    ///
    /// <para>🔑 Same one-discipline-per-race shape as the melee rogue: Sharpshooter is the Human,
    /// Trapper the Elf, Hunter the Demon. Each owns one trap, one Magic Arrow and one race stance, and
    /// the Elf additionally owns the self-Antidote (the same six rows his tank and dual files give the
    /// Elf, one skill shared by all three).</para>
    ///
    /// <para>⚠ Bow Expertise is the WARCHANTER's <c>wc_bow_expertise</c>, named in his own cell — not a
    /// clone. Nothing in the learn path gates on <see cref="BaseClass"/>, so a Fighter discipline can
    /// teach a def declared Mage; the alternative was a second skill sharing its buff key, which is
    /// what the retired derived kit did and which his cell explicitly does not ask for.
    /// 🔴 ITS SP DISAGREES BETWEEN HIS TWO FILES — 42,000 in `buffer 3rd.csv`, 37,000 here. One skill
    /// has one price, and the buffer's is the one the def carries; `--check` will report the archer
    /// row until he settles it.</para>
    ///
    /// <para>⚠ Signal Flare is <see cref="RegisterHideKit"/>'s, placed by him at 60 long before this
    /// file existed. Only its SP moved (12,000 → his 120,000).</para></summary>
    private static void RegisterArcher3rd()
    {
        int[] band15 = { 40, 43, 46, 49, 52, 55, 58, 60, 62, 64, 66, 68, 70, 72, 74 };
        int[] buff3  = { 58, 66, 74 };
        int[] cure6  = { 52, 58, 62, 66, 70, 74 };

        static IEnumerable<ClassSkill> Ladder(string id, int[] levels, int startRung = 1) =>
            levels.Select((lv, i) => new ClassSkill(id, lv, SkillLevel: startRung + i));

        // ⚠ Armor Mastery here is a NEW skill starting at rung 1, not appended rungs: his file gives
        //   it its own id and a REPLACES cell. The dual file does the opposite with the same ladder.
        var shared = new List<ClassSkill>();
        shared.AddRange(Ladder(ArcherArmorMastery, band15));
        shared.AddRange(Ladder(BowWeaponMastery, band15));
        shared.AddRange(Ladder(ArcherTwinArrows, band15));
        shared.AddRange(Ladder(ArcherExplosiveArrow, band15));
        shared.AddRange(Ladder(ArcherBowBlessing, buff3));
        shared.AddRange(Ladder(ArcherBowSpirit, buff3));
        // ✅ 37,000 SP, NOT the def's 42,000 — his answer, 2026-09-09: *"bow expertise for buffer and
        //    archer is at different lvls so it cost different SP"*. The buffer learns it at 56 and pays
        //    42,000; the archer learns it at 52 and pays 37,000. ONE ability, one set of magnitudes,
        //    two prices — which is precisely the case `ClassSkill.SpCost` was added for (Shield
        //    Mastery, 2026-08-21): SP in this game is priced by the LEVEL YOU LEARN AT, not by the
        //    ability. Splitting it into two SkillDefs would duplicate a ladder and invite it to drift.
        shared.Add(new ClassSkill(WcBowExpertise, 52, SpCost: 37_000));
        shared.Add(new ClassSkill(ArcherBowStance, 60));

        var human = new List<ClassSkill>(shared);
        human.AddRange(Ladder(ArcherBowFocus, buff3));
        human.AddRange(Ladder(ArcherPoisonTrap, band15));
        human.AddRange(Ladder(ArcherMagicArrowHuman, band15));

        var elf = new List<ClassSkill>(shared);
        elf.AddRange(Ladder(ArcherBowSwiftness, buff3));
        elf.AddRange(Ladder(ArcherBindingTrap, band15));
        elf.AddRange(Ladder(ArcherMagicArrowElf, band15));
        elf.AddRange(Ladder(ElfAntidote, cure6));

        var demon = new List<ClassSkill>(shared);
        demon.AddRange(Ladder(ArcherBowFerocity, buff3));
        demon.AddRange(Ladder(ArcherBleedTrap, band15));
        demon.AddRange(Ladder(ArcherMagicArrowDemon, band15));

        ClassSkills.RegisterThird(Race.Human, Discipline.Sharpshooter, human.ToArray());
        ClassSkills.RegisterThird(Race.Elf,   Discipline.Trapper,      elf.ToArray());
        ClassSkills.RegisterThird(Race.Demon, Discipline.Hunter,       demon.ToArray());
    }
}
