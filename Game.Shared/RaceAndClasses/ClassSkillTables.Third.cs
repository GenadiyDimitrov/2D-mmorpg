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

        // Frost Bind — the nuker's contested-CC (Slow) tool, @40 for both disciplines.
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            foreach (var disc in new[] { Discipline.Magus, Discipline.Tempest })
                ClassSkills.RegisterThird(race, disc, new ClassSkill(FrostBind, 40));

        // Cleaving Strike — the warriors' "[Double]" burst, @40 for both disciplines.
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            foreach (var disc in new[] { Discipline.Ravager, Discipline.Warlord })
                ClassSkills.RegisterThird(race, disc, new ClassSkill(CleavingStrike, 40));

        // Contested CC demos: Vanguard (tank) gets the Stun; warriors get the Fear.
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
        {
            ClassSkills.RegisterThird(race, Discipline.Vanguard, new ClassSkill(ShieldBash, 40));
            foreach (var disc in new[] { Discipline.Ravager, Discipline.Warlord })
                ClassSkills.RegisterThird(race, disc, new ClassSkill(TerrifyingRoar, 40));
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
        ClassSkills.RegisterThird(Race.Human, Discipline.Lightbringer,
            new ClassSkill(LbHumanMend, 40), new ClassSkill(LbHumanPurify, 44),
            new ClassSkill(LbBlessing, 48), new ClassSkill(LbDevotion, 52));
        ClassSkills.RegisterThird(Race.Elf, Discipline.Lightbringer,
            new ClassSkill(LbElfDawn, 40), new ClassSkill(LbElfWarden, 44),
            new ClassSkill(LbBlessing, 48), new ClassSkill(LbDevotion, 52));
        ClassSkills.RegisterThird(Race.Ork, Discipline.Lightbringer,
            new ClassSkill(LbOrkFont, 40), new ClassSkill(LbOrkSap, 44),
            new ClassSkill(LbBlessing, 48), new ClassSkill(LbDevotion, 52));
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
