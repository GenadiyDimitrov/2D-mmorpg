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
            [Discipline.Sharpshooter] = new[] { (PowerShot, "Piercing Shot"), (BattleFury, "Steady Aim") },
            [Discipline.Trapper]      = new[] { (PowerShot, "Snare Shot"), (Disrupt, "Net Trap") },
            // The two disciplines the ARCHER MERGE added. Nullblade is the HUMAN melee rogue —
            // docs/design/Disciplines.md already wrote that kit as "Phantom, Human flavour: anti
            // magic", so it inherits the stealth/ambush shape; Phantom itself went to the Elf, whose
            // authored flavour ("anti phys", evasion) is what the name has always described.
            // Hunter is the ORK bow, from that doc's "Sharpshooter, Ork flavour: dmg focus".
            [Discipline.Nullblade]    = new[] { (BattleFury, "Nullstep"), (TwinSlash, "Silencing Cut") },
            [Discipline.Hunter]       = new[] { (PowerShot, "Rending Shot"), (BattleFury, "Blood Draw") },
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
                default:   // Human (+ God): bleed
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
