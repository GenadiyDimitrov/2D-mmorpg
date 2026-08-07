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
        // (skillId, displayName) placeholders per discipline.
        var kit = new Dictionary<Discipline, (string Skill, string Name)[]>
        {
            [Discipline.Bulwark]      = new[] { (Fortify, "Aegis Wall"), (ShieldMastery, "Bulwark Stance") },
            [Discipline.Vanguard]     = new[] { (MightyBlow, "Punisher"), (Fortify, "Iron Guard") },
            [Discipline.Ravager]      = new[] { (MightyBlow, "Rend"), (GreaterWarCry, "Bloodlust") },
            [Discipline.Warlord]      = new[] { (GreaterWarCry, "Rally"), (MightyBlow, "Cleave") },
            [Discipline.Phantom]      = new[] { (BattleFury, "Shadowstep"), (TwinSlash, "Ambush") },
            [Discipline.Venomweaver]  = new[] { (TwinSlash, "Venom Strike"), (BattleFury, "Creeping Toxin") },
            // 🔴 The three RANGED rogue disciplines each opened with a rename of Heavy Draw
            // (`PowerShot`) — "Piercing Shot", "Snare Shot", "Rending Shot". All three were removed
            // 2026-08-07 (playtest-19 M7): *"remove it from after 40lvl as well"*. He is done with
            // that skill on the rogue line at every level, not just the @24 grant.
            // ⚠ The `PowerShot` SkillDef itself STAYS — deleting it is what the old warnings were
            // about, and it now has no learn assignment at all until the level-40 bow CSV lands.
            [Discipline.Sharpshooter] = new[] { (BattleFury, "Steady Aim") },
            [Discipline.Trapper]      = new[] { (Disrupt, "Net Trap") },
            // The two disciplines the ARCHER MERGE added. Nullblade is the HUMAN melee rogue —
            // docs/design/Disciplines.md already wrote that kit as "Phantom, Human flavour: anti
            // magic", so it inherits the stealth/ambush shape; Phantom itself went to the Elf, whose
            // authored flavour ("anti phys", evasion) is what the name has always described.
            // Hunter is the ORK bow, from that doc's "Sharpshooter, Ork flavour: dmg focus".
            [Discipline.Nullblade]    = new[] { (BattleFury, "Nullstep"), (TwinSlash, "Silencing Cut") },
            [Discipline.Hunter]       = new[] { (BattleFury, "Blood Draw") },
            // Lightbringer + Warchanter are fully authored per race below.
            [Discipline.Magus]        = new[] { (FlameBolt, "Annihilate"), (GreaterWeakness, "Mana Burn") },
            [Discipline.Tempest]      = new[] { (FlameBolt, "Chain Lightning"), (GreaterWeakness, "Maelstrom") },
        };

        // 3rd-class learn cadence starts at 40: fighter disciplines step every 3
        // (40,43,46,…), mage disciplines every 4 (40,44,48,…).
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            foreach (var (discipline, skills) in kit)
            {
                int step = discipline is Discipline.Magus or Discipline.Tempest ? 4 : 3;
                ClassSkills.RegisterThird(race, discipline,
                    skills.Select((s, i) => new ClassSkill(s.Skill, 40 + i * step, s.Name)).ToArray());
            }

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

        // Warrior 3rd-class kit demos: [Double] burst, physical Slow, +skill-damage buff.
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            foreach (var disc in new[] { Discipline.Ravager, Discipline.Warlord })
                ClassSkills.RegisterThird(race, disc,
                    new ClassSkill(CleavingStrike, 40), new ClassSkill(Hamstring, 40),
                    new ClassSkill(WarFocus, 40));

        // Contested CC demos: Vanguard (tank) gets the Stun; warriors get the Fear.
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
        {
            ClassSkills.RegisterThird(race, Discipline.Vanguard, new ClassSkill(ShieldBash, 40));
            // Provoke — taunt — both tank disciplines.
            ClassSkills.RegisterThird(race, Discipline.Bulwark, new ClassSkill(Provoke, 40));
            ClassSkills.RegisterThird(race, Discipline.Vanguard, new ClassSkill(Provoke, 40));
            // Aegis — self absorb-shield — both tank disciplines.
            ClassSkills.RegisterThird(race, Discipline.Bulwark, new ClassSkill(Aegis, 40));
            ClassSkills.RegisterThird(race, Discipline.Vanguard, new ClassSkill(Aegis, 40));
            // Last Stand (lethal save) + Indomitable (cancel resist) — Bulwark; Mana Barrier — Magus.
            ClassSkills.RegisterThird(race, Discipline.Bulwark, new ClassSkill(LastStand, 44));
            ClassSkills.RegisterThird(race, Discipline.Bulwark, new ClassSkill(Indomitable, 48));
            ClassSkills.RegisterThird(race, Discipline.Magus, new ClassSkill(ManaBarrier, 44));
            foreach (var disc in new[] { Discipline.Ravager, Discipline.Warlord })
                ClassSkills.RegisterThird(race, disc, new ClassSkill(TerrifyingRoar, 40));
            // Venomweaver — per-race DoT trio: Human bleed (−MS), Elf poison (−AS/cast),
            // Ork venom (−atk/def). Each: a stacking DoT applier @40 + a burst @44.
            switch (race)
            {
                case Race.Elf:
                    ClassSkills.RegisterThird(race, Discipline.Venomweaver,
                        new ClassSkill(ToxicSting, 40), new ClassSkill(ToxicBurst, 44));
                    break;
                case Race.Ork:
                    ClassSkills.RegisterThird(race, Discipline.Venomweaver,
                        new ClassSkill(Envenom, 40), new ClassSkill(VenomBurst, 44));
                    break;
                default:   // Human: bleed
                    ClassSkills.RegisterThird(race, Discipline.Venomweaver,
                        new ClassSkill(Rupture, 40), new ClassSkill(DetonateWounds, 44));
                    break;
            }
            // Movement + primitives: Phantom blink (Shadowstep) + stealth (Vanish); Trapper
            // knockback (Repelling Shot) + a rooting damage trap (Snare Trap).
            ClassSkills.RegisterThird(race, Discipline.Phantom,
                new ClassSkill(Shadowstep, 40), new ClassSkill(Vanish, 44));
            ClassSkills.RegisterThird(race, Discipline.Trapper,
                new ClassSkill(RepellingShot, 40), new ClassSkill(SnareTrap, 44));
            // Nullblade shares Phantom's primitives (it IS the human Phantom kit under its own name):
            // blink in from stealth, then vanish again.
            ClassSkills.RegisterThird(race, Discipline.Nullblade,
                new ClassSkill(Shadowstep, 40), new ClassSkill(Vanish, 44));
            // Hunter shares Sharpshooter's ranged primitive — the knockback shot that buys an ork the
            // distance its bow wants.
            ClassSkills.RegisterThird(race, Discipline.Hunter,
                new ClassSkill(RepellingShot, 40));
        }

        // Healer disciplines (Lightbringer = healer, Warchanter = buffer) are dropped
        // pending the new lvl-40 CSVs. Their skill DEFS remain in the catalog; only the
        // learn assignments are gone, so nothing references them until re-authored.
        // RegisterLightbringer();  RegisterWarchanter();
        //
        // …with ONE exception: the buffer's two exclusive layers now have somewhere to live.
        RegisterWarchanterBuffs();
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
                new ClassSkill(HolySpeed, 66, SkillLevel: 6),    // Swift and Sure
                new ClassSkill(Might, 68, SkillLevel: 6),        // Might and Bulwark
                new ClassSkill(HolyForce, 70, SkillLevel: 6),    // Force and Ward
                new ClassSkill(HolyFocus, 72, SkillLevel: 6),    // Focus and Ferocity
                new ClassSkill(HolyBody, 74, SkillLevel: 6));    // Body and Soul
    }

    // The first fully-authored discipline (Phase 24.1): one shared idea (keep the
    // party alive), three race expressions. The 2nd-class healer kit still applies
    // cumulatively, so these are the discipline's NEW tools on top. Each race now
    // gets the shared Blessing (party buff) + Devotion (passive) to fill the kit.
    private static void RegisterLightbringer()
    {
        // Mage 3rd-class learn cadence: 40, 44, 48, 52.
        // Resurrection L3/L4 are the Lightbringer's alone — every cleric learns L1/L2 from the 2nd-class
        // table, but only the dedicated healer restores 75%/100% of a death's lost exp.
        ClassSkills.RegisterThird(Race.Human, Discipline.Lightbringer,
            new ClassSkill(LbHumanMend, 40), new ClassSkill(LbHumanPurify, 44),
            new ClassSkill(LbBlessing, 48), new ClassSkill(LbDevotion, 52),
            new ClassSkill(Resurrection, 52, SkillLevel: 3), new ClassSkill(Resurrection, 61, SkillLevel: 4));
        ClassSkills.RegisterThird(Race.Elf, Discipline.Lightbringer,
            new ClassSkill(LbElfDawn, 40), new ClassSkill(LbElfWarden, 44),
            new ClassSkill(LbBlessing, 48), new ClassSkill(LbDevotion, 52),
            new ClassSkill(Resurrection, 52, SkillLevel: 3), new ClassSkill(Resurrection, 61, SkillLevel: 4));
        ClassSkills.RegisterThird(Race.Ork, Discipline.Lightbringer,
            new ClassSkill(LbOrkFont, 40), new ClassSkill(LbOrkSap, 44),
            new ClassSkill(LbBlessing, 48), new ClassSkill(LbDevotion, 52),
            new ClassSkill(Resurrection, 52, SkillLevel: 3), new ClassSkill(Resurrection, 61, SkillLevel: 4));
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
