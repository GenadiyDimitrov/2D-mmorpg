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

        // (skillId, displayName) placeholders — the two NUKER disciplines only, per his point 1.
        var kit = new Dictionary<Discipline, (string Skill, string Name)[]>
        {
            [Discipline.Magus]        = new[] { (FlameBolt, "Annihilate"), (GreaterWeakness, "Mana Burn") },
            [Discipline.Tempest]      = new[] { (FlameBolt, "Chain Lightning"), (GreaterWeakness, "Maelstrom") },
        };

        // Mage 3rd-class learn cadence: 40, 44, 48, … (step 4).
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            foreach (var (discipline, skills) in kit)
                ClassSkills.RegisterThird(race, discipline,
                    skills.Select((s, i) => new ClassSkill(s.Skill, 40 + i * 4, s.Name)).ToArray());

        // Nuker ULTIMATE — Elemental Burst (consumes 10 Elemental Stones). 10 levels at
        // char 40/44/48/…/72/75 (step 4, last capped at 75), power 150 → 250. Shared by
        // both nuker disciplines (Magus + Tempest), all races.
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            foreach (var disc in new[] { Discipline.Magus, Discipline.Tempest })
                ClassSkills.RegisterThird(race, disc,
                    Enumerable.Range(1, 10)
                        .Select(lvl => new ClassSkill(ElementalBurst,
                            lvl <= 9 ? 36 + lvl * 4 : 75, SkillLevel: lvl))
                        .ToArray());

        // Frost Bind (Slow) + Entangling Roots (Root) + Glacial Spike (+dmg vs slowed/rooted)
        // — nuker contested CC + conditional-damage payoff, @40/44 both disciplines.
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            foreach (var disc in new[] { Discipline.Magus, Discipline.Tempest })
                ClassSkills.RegisterThird(race, disc,
                    new ClassSkill(FrostBind, 40), new ClassSkill(EntanglingRoots, 40),
                    new ClassSkill(GlacialSpike, 44));
        // Creeping Frost — stacking slow (10/20/30%) + Phase Shift (blink-back) — Tempest.
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            ClassSkills.RegisterThird(race, Discipline.Tempest,
                new ClassSkill(CreepingFrost, 44), new ClassSkill(PhaseShift, 48));

        // Mana Barrier — Magus. The last survivor of the old shared fighter/mage block; every
        // tank, warrior and rogue grant that stood here went in the 40+ purge (see the top).
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            ClassSkills.RegisterThird(race, Discipline.Magus, new ClassSkill(ManaBarrier, 44));

        // ✅ THE LIGHTBRINGER IS ON, 2026-08-20. `healer 3rd.csv` is finished and he authorised the
        // build, so the healer discipline is no longer an exception carved out of the 40+ purge — it
        // is the first fully-authored 3rd class in the game, 40 to 74, every row his.
        //
        // ⚠ THE PURGE STILL STANDS FOR EVERYONE ELSE. This is not a precedent for switching the others
        // back on: the eight fighter disciplines have no file at all.
        RegisterLightbringer();
        // THE WARCHANTER, 40-74 — his `buffer 3rd.csv` rows 1-185, built 2026-08-21. The file is only
        // HALF authored (it carries a `NOT DONE` banner; his passives and attack skills are still being
        // written), so this teaches the buff/harmony/group layer and nothing else.
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
    /// (2026-07-29) split the rogue by RACE at 40: melee = Nullblade (human) · Venomweaver (ork) ·
    /// Phantom (elf); ranged = Sharpshooter (human) · Hunter (ork) · Trapper (elf). Registered for all
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

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
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
                    // ⚠ LEVEL 1 ONLY, deliberately: the 200/400/600 reach ladder and its SP are HIS to
                    // place as he writes `dual 3rd.csv` (*"I'll author it to the corresponding lvls as
                    // I'm making the file"*). Levels 2-3 exist in the catalog and are unreachable until
                    // he does — that is the intended state, not a gap to helpfully fill.
                    new ClassSkill(Lure, 40, SkillLevel: 1),
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
    /// ruling distinguishes a Human Bulwark from an Ork one.</para></summary>
    private static void RegisterPreservation()
    {
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
        {
            // The healer's: cast on an ally, and it is the ALLY who rises.
            ClassSkills.RegisterThird(race, Discipline.Lightbringer,
                new ClassSkill(RiteOfPreservation, 83));
            // The tank's: the self version.
            ClassSkills.RegisterThird(race, Discipline.Bulwark,
                new ClassSkill(UndyingWill, 83));
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

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            ClassSkills.RegisterThird(race, Discipline.Warchanter, kit.ToArray());
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
        // Once on the fast heal (Human throughput / Elf heal-and-cure / Ork planted totem) and once on
        // the control debuff (Gravity / Bind / Armor Break). Both are full 14-rung ladders, and the Ork
        // carries a third: the Mana Totem, from 52.
        ClassSkills.RegisterThird(Race.Human, Discipline.Lightbringer,
            shared.Concat(Full(LbHumanMend)).Concat(Full(LbHumanGravity)).ToArray());
        ClassSkills.RegisterThird(Race.Elf, Discipline.Lightbringer,
            shared.Concat(Full(LbElfDawn)).Concat(Full(LbElfBind)).ToArray());
        ClassSkills.RegisterThird(Race.Ork, Discipline.Lightbringer,
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
        ClassSkills.RegisterThird(Race.Ork, Discipline.Warchanter,
            new ClassSkill(WcOrkBolt, 40), new ClassSkill(WcOrkChant, 44),
            new ClassSkill(WcOrkRenew, 48), new ClassSkill(WcOrkPass, 52));
    }
}
