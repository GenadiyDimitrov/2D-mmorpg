namespace Game.Shared;

using static Game.Shared.SkillCatalog;

/// <summary>
/// PLACEHOLDER 3rd-class (discipline) kits for Phase 24.0. Each discipline gets a
/// small list at learn-level 40, reusing existing skill ids with a display-name
/// hint so the framework is exercised end-to-end (learn tab, skill bar, class
/// label) before the real per-(race,discipline) skills are authored in the
/// content slices. The same idea is shared by all three races for now; the slices
/// will diverge them (e.g. the Ork Lightbringer's totem vs. the Human's cleanse).
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

        // Healer disciplines (Lightbringer = healer, Warchanter = buffer) are dropped
        // pending the new lvl-40 CSVs. Their skill DEFS remain in the catalog; only the
        // learn assignments are gone, so nothing references them until re-authored.
        // RegisterLightbringer();  RegisterWarchanter();
        //
        // …with ONE exception: the buffer's two exclusive layers now have somewhere to live.
        RegisterWarchanterBuffs();
        // …and now a SECOND, equally narrow one: the two level-83 preservation skills he authorised
        // by name on 2026-08-14 (`BL-35`). Two learn lines, nothing else — the Lightbringer and
        // Bulwark kits above stay commented out.
        RegisterPreservation();
        // …and a THIRD, on the same terms: the three HIDE skills, which he placed by hand.
        RegisterHideKit();
        // …and a FOURTH: the healer's own two masteries, which he authored into `healer 3rd.csv` on
        // 2026-08-20 with a full 9-rung ladder. Same narrow standing as the three above — two learn
        // lines' worth of skills he named himself, NOT a repeal of the purge. The rest of the
        // Lightbringer kit stays commented out until his 44+ heals are settled.
        RegisterHealerMasteries();
    }

    /// <summary>The healer's 3rd-class masteries — <b>Healer Weapon Mastery</b> (magic weapon only, no
    /// P.Atk) and <b>Healer Armor Mastery</b> (robe only), 9 rungs each at 40/44/48/52/56/58/60/62/64.
    ///
    /// <para>They REPLACE the cleric's Spell Mastery / Armor Mastery rather than continuing them, which
    /// is the whole point of his split: from 40 the healer's kit is a wand and a robe, and the BUFFER is
    /// the caster who keeps the sword half and the light-armor row (see RegisterWarchanterBuffs).</para>
    ///
    /// <para>⚠ This lives here, not in <c>RegisterLightbringer()</c>, because that function is still
    /// commented out pending his 44+ rows — and these two rungs are the one part of that band that is
    /// fully authored and unambiguous. When it is switched back on, do NOT re-add them there.</para>
    ///
    /// <para>⚠ His last block's rows carry <c>56</c> in the LEARN column where the section header says
    /// 64 — a copy/paste slip in a file he is still drafting. Built as 64, since a second rung at 56
    /// would collide with the real one. Worth a one-word fix in the CSV.</para></summary>
    private static void RegisterHealerMasteries()
    {
        int[] levels = { 40, 44, 48, 52, 56, 58, 60, 62, 64 };
        var rungs = levels.SelectMany((lvl, i) => new[]
        {
            new ClassSkill(HealerWeaponMasterySkill, lvl, SkillLevel: i + 1),
            new ClassSkill(HealerArmorMasterySkill, lvl, SkillLevel: i + 1),
        })
        // Frenzy L2 @52 — the healer's half of *"learned from Healers and Buffers at 52"*. The buffer's
        // copy is in RegisterWarchanterBuffs; both point at the same rung, which is the point of the
        // ruling. It rides along here because this is the only ACTIVE Lightbringer registration.
        .Append(new ClassSkill(HolyFrenzy, 52, SkillLevel: 2))
        .ToArray();

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            ClassSkills.RegisterThird(race, Discipline.Lightbringer, rungs);
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

    /// <summary>The Warchanter's buff kit — the rest of the discipline still waits for its CSV
    /// (owner 2026-07-31: *"just to have somewhere the improved and harmony to go … it will be
    /// changed with the 3rd class CSV"*), so every level below is a placeholder.
    ///
    /// The shape the owner asked for, and the reason it reads bottom-up:
    ///
    /// - **40-64: the singles, topped out.** The cleric leaves off mid-ladder (Might L2 of 3, Focus
    ///   L4 of 6, …) and never sees Ferocity, Insight, Body, Soul or Serenity at all. The Warchanter
    ///   finishes every ladder, and finishes each family **before** the improved buff that contains
    ///   it. Not a hard requirement — nothing enforces the order — just the logic of the class:
    ///   you learn the parts, then you learn to cast them in one breath.
    /// - **60 / 62 / 64: the three Harmony blessings.** The layer with no potion, no scroll and no
    ///   NPC that sells it, stacking on top of the basic buffs. It is what keeps a buffer worth
    ///   grouping with now that consumables can cover the whole basic layer.
    /// - **66-74: the five improved groups**, one per learnable level. Each one `Replaces` the
    ///   singles it contains, so the bar collapses as the class matures: four skills become one.
    ///
    /// Frenzy is deliberately NOT one of the five — its "rung" is already a whole eight-effect buff,
    /// so it ramps with the singles. See docs/design/BuffLadders.md.</summary>
    private static void RegisterWarchanterBuffs()
    {
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            ClassSkills.RegisterThird(race, Discipline.Warchanter,
                // ---- Shrouding Hymn — the party stealth. It was the cleric's; he moved its row into
                // `buffer 3rd.csv` on 2026-08-19, settling the one row in the cleric file whose values
                // were never his. His level 30 is kept verbatim, and since a Warchanter does not exist
                // below 40 the practical effect is that it arrives with the class change. ----
                new ClassSkill(ShroudingHymn, 30, SkillLevel: 1),
                // ---- The two CLERIC masteries, CONTINUED (his `buffer 3rd.csv`, 2026-08-20: *"Continue
                //      the line"* on both rows). This is the other half of the healer's split: the healer
                //      swapped to his own robe-only/magic-weapon-only pair, the buffer just gets rung 5.
                //      He is now the only caster who keeps the LIGHT-armor row and the P.Atk half — which
                //      is exactly the class his heavy/shield passives are meant for. ----
                new ClassSkill(SpellMastery, 40, SkillLevel: 5),
                new ClassSkill(ArmorMasterySkill, 40, SkillLevel: 5),
                // ---- Frenzy L2 @52 — his 2026-08-20 ruling, the SAME rung for healer and buffer:
                //      *"Frenzy(L2) is learned from Healers and Buffers at 52"*. The cleric's L1 at 35
                //      (ClassSkillTables.Common.cs) is the rung below it. ⚠ The 62/64 grants further
                //      down are rungs 3 and 6, which are OUR invented values and are now WEAKER than
                //      this one — see the note on FrenzyRung. ----
                new ClassSkill(HolyFrenzy, 52, SkillLevel: 2),
                // ---- 40-44: finish SPEED and MIGHT (the cleric got most of speed already) ----
                new ClassSkill(CastId(FamEva), 40, SkillLevel: 3),        // Agility   +4 evasion
                new ClassSkill(CastId(FamAs), 40, SkillLevel: 2),         // Haste     +23% atk speed
                new ClassSkill(CastId(FamPhysAtk), 40, SkillLevel: 3),    // Might     +15% P.Atk
                new ClassSkill(CastId(FamPhysDef), 40, SkillLevel: 3),    // Bulwark   +15% P.Def
                new ClassSkill(CastId(FamAs), 44, SkillLevel: 3),         // Haste     +33% atk speed
                new ClassSkill(CastId(FamVamp), 44, SkillLevel: 3),       // Vampirism 9%
                new ClassSkill(CastId(FamAccuracy), 44, SkillLevel: 3),   // Aim       +4 accuracy
                // ---- 48-52: finish FORCE, start FOCUS ----
                new ClassSkill(CastId(FamMagAtk), 48, SkillLevel: 3),     // Force     +32% M.Atk
                new ClassSkill(CastId(FamMagDef), 48, SkillLevel: 2),     // Ward      +20% M.Def
                new ClassSkill(CastId(FamInterrupt), 48, SkillLevel: 3),  // Resolve   +40 interrupt
                new ClassSkill(CastId(FamMagDef), 52, SkillLevel: 3),     // Ward      +30% M.Def
                new ClassSkill(CastId(FamInterrupt), 52, SkillLevel: 4),  // Resolve   +60 interrupt
                new ClassSkill(CastId(FamCritRate), 52, SkillLevel: 5),   // Focus     +25% crit
                new ClassSkill(CastId(FamCritDmg), 52, SkillLevel: 3),    // Ferocity  +20% crit dmg
                new ClassSkill(CastId(FamMagCrit), 52, SkillLevel: 2),    // Insight   +35% magic crit
                // ---- 56: finish FOCUS ----
                new ClassSkill(CastId(FamCritRate), 56, SkillLevel: 6),   // Focus     +30% crit
                new ClassSkill(CastId(FamCritDmg), 56, SkillLevel: 6),    // Ferocity  +35% crit dmg
                new ClassSkill(CastId(FamMagCrit), 56, SkillLevel: 4),    // Insight   +65% magic crit
                // ---- 60-64: the BODY ladder, Frenzy, and the three Harmonies ----
                new ClassSkill(NpcHarmonyProtection, 60),
                new ClassSkill(CastId(FamMagCrit), 60, SkillLevel: 6),    // Insight   double magic crit
                new ClassSkill(CastId(FamMaxHp), 60, SkillLevel: 3),      // Body      +20% Max HP
                new ClassSkill(CastId(FamMaxMp), 60, SkillLevel: 3),      // Soul      +20% Max MP
                new ClassSkill(CastId(FamHpRegen), 60, SkillLevel: 4),    // Vigor     +15% HP regen
                new ClassSkill(CastId(FamMpRegen), 60, SkillLevel: 4),    // Serenity  +15% MP regen
                new ClassSkill(NpcHarmonyWarrior, 62),
                new ClassSkill(CastId(FamMaxHp), 62, SkillLevel: 5),      // Body      +30% Max HP
                new ClassSkill(CastId(FamMaxMp), 62, SkillLevel: 5),      // Soul      +30% Max MP
                new ClassSkill(CastId(FamHpRegen), 62, SkillLevel: 6),    // Vigor     +20% HP regen
                new ClassSkill(CastId(FamMpRegen), 62, SkillLevel: 6),    // Serenity  +20% MP regen
                new ClassSkill(HolyFrenzy, 62, SkillLevel: 3),            // Frenzy    rung 3
                new ClassSkill(NpcHarmonyWizard, 64),
                new ClassSkill(CastId(FamMaxHp), 64, SkillLevel: 6),      // Body      +35% Max HP
                new ClassSkill(CastId(FamMaxMp), 64, SkillLevel: 6),      // Soul      +35% Max MP
                new ClassSkill(HolyFrenzy, 64, SkillLevel: 6),            // Frenzy    rung 6
                // ---- 66-74: the improved groups, one per level. Each REPLACES its singles. ----
                new ClassSkill(HolyShield, 66),                  // Shield Bless and Harden (his 66 row)
                new ClassSkill(HolySpeed, 66, SkillLevel: 6),    // Swift and Sure
                new ClassSkill(Might, 68, SkillLevel: 6),        // Might and Bulwark
                new ClassSkill(HolyForce, 70, SkillLevel: 6),    // Force and Ward
                new ClassSkill(HolyFocus, 72, SkillLevel: 6),    // Focus and Ferocity
                new ClassSkill(HolyBody, 74, SkillLevel: 6),     // Body and Soul
                // ---- 76: MADNESS (`BL-34`) — the party Frenzy at the top of the family. ----
                // His 2026-08-14 ruling put it here on purpose: *"put it at 76 on the buffer"*, so an
                // ADMIN (whose seed character is a level-90 Warchanter) can party-buff with it now,
                // *"and when the kits land we will move it"*. Treat this line as temporary — it is the
                // first thing the 40+ CSVs should relocate, not a rung to build more on top of.
                new ClassSkill(Madness, 76));
    }

    // The first fully-authored discipline (Phase 24.1): one shared idea (keep the
    // party alive), three race expressions. The 2nd-class healer kit still applies
    // cumulatively, so these are the discipline's NEW tools on top. Each race now
    // gets the shared Blessing (party buff) + Devotion (passive) to fill the kit.
    private static void RegisterLightbringer()
    {
        // Mage 3rd-class learn cadence: 40, 44, 48, 52.
        // ⚠ RESURRECTION HAS NO 40+ RUNGS ANY MORE. It used to hand the Lightbringer L3@52 / L4@61
        // (75% / 100% of the lost exp), but that ladder was invented here, and the owner replaced its
        // bottom with his own on 2026-08-17 — *"resurect - lvl 20 no exp, @30 - 20% exp .. ill add 40+"*.
        // The top is his to write (BL-02), so the grants are gone rather than left at invented values.
        // ---- LEVEL 40 IS HIS, authored in `healer 3rd.csv` (rows 3-22, 2026-08-17). Everything at 44
        //      and above in that file is marked "not done", so the 44/48/52 rows below are still the
        //      INVENTED kit and are left exactly as they were — they will be reconciled when he writes
        //      those levels. ⚠ That leaves one known overlap: the Elf has both his `Bind` at 40 and the
        //      invented `Warding Step` at 44, and both are roots. ----
        // The shared half of level 40: three passives, the three upgraded heals/nuke, the res rung, and
        // the buff blessings. Every race learns these.
        ClassSkill[] LightbringerShared40() => new[]
        {
            new ClassSkill(MageAntiMagic, 40, SkillLevel: 7),
            // ⚠ 2026-08-20: the healer's two masteries are NOT here. He no longer continues the cleric
            // pair — he REPLACES it — and since this whole function is still commented out pending his
            // 44+ rows, the ladder lives in the ACTIVE RegisterHealerMasteries() instead. Do not add it
            // back here as well: it would register every rung twice the day this block is switched on.
            new ClassSkill(HolyRay, 40),
            new ClassSkill(GreatHeal, 40),
            new ClassSkill(PartyGreatHeal, 40),
            new ClassSkill(Conceal, 40),
            new ClassSkill(Resurrection, 40, SkillLevel: 3),
            new ClassSkill(RestoreMana, 40, SkillLevel: 3),
            new ClassSkill(CastId(FamPhysAtk), 40, SkillLevel: 3),    // Might L3   +15% P.Atk
            new ClassSkill(CastId(FamAccuracy), 40, SkillLevel: 2),   // Aim L2     +2 Accuracy
            new ClassSkill(CastId(FamCritDmg), 40, SkillLevel: 4),    // Ferocity   +25% crit damage
            // ⚠ Clarity is rung 2 since 2026-08-19, NOT because this row was retuned: he added a 20%
            // rung to `cleric 2nd.csv` at 25, so his 30% here is the SECOND rung of the family now.
            // The number he authored is unchanged; only its index moved.
            new ClassSkill(CastId(FamCcResMag), 40, SkillLevel: 2),   // Clarity    30% vs SPT debuffs
            new ClassSkill(CastId(FamCcResPhys), 40, SkillLevel: 1),  // Fortitude  15% vs CON debuffs
        };
        ClassSkills.RegisterThird(Race.Human, Discipline.Lightbringer,
            LightbringerShared40().Concat(new[]
            {
                new ClassSkill(LbHumanMend, 40),        // Quick Great Heal
                new ClassSkill(LbHumanGravity, 40),     // Gravity
                new ClassSkill(LbHumanPurify, 44),
                new ClassSkill(LbBlessing, 48), new ClassSkill(LbDevotion, 52),
            }).ToArray());
        ClassSkills.RegisterThird(Race.Elf, Discipline.Lightbringer,
            LightbringerShared40().Concat(new[]
            {
                new ClassSkill(LbElfDawn, 40),          // Healer Blessing
                new ClassSkill(LbElfBind, 40),          // Bind
                new ClassSkill(LbElfWarden, 44),
                new ClassSkill(LbBlessing, 48), new ClassSkill(LbDevotion, 52),
            }).ToArray());
        ClassSkills.RegisterThird(Race.Ork, Discipline.Lightbringer,
            LightbringerShared40().Concat(new[]
            {
                new ClassSkill(LbOrkFont, 40),          // Healing Totem
                new ClassSkill(LbOrkArmorBreak, 40),    // Armor Break
                new ClassSkill(LbOrkSap, 44),
                new ClassSkill(LbBlessing, 48), new ClassSkill(LbDevotion, 52),
            }).ToArray());
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
