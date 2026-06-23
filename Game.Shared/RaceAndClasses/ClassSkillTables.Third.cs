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

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            foreach (var (discipline, skills) in kit)
                ClassSkills.RegisterThird(race, discipline,
                    skills.Select(s => new ClassSkill(s.Skill, 40, s.Name)).ToArray());

        RegisterLightbringer();
        RegisterWarchanter();
    }

    // The first fully-authored discipline (Phase 24.1): one shared idea (keep the
    // party alive), three race expressions. The 2nd-class healer kit still applies
    // cumulatively, so these are the discipline's NEW tools on top. Each race now
    // gets the shared Blessing (party buff) + Devotion (passive) to fill the kit.
    private static void RegisterLightbringer()
    {
        ClassSkills.RegisterThird(Race.Human, Discipline.Lightbringer,
            new ClassSkill(LbHumanMend, 40), new ClassSkill(LbHumanPurify, 40),
            new ClassSkill(LbBlessing, 40), new ClassSkill(LbDevotion, 40));
        ClassSkills.RegisterThird(Race.Elf, Discipline.Lightbringer,
            new ClassSkill(LbElfDawn, 40), new ClassSkill(LbElfWarden, 40),
            new ClassSkill(LbBlessing, 40), new ClassSkill(LbDevotion, 40));
        ClassSkills.RegisterThird(Race.Ork, Discipline.Lightbringer,
            new ClassSkill(LbOrkFont, 40), new ClassSkill(LbOrkSap, 40),
            new ClassSkill(LbBlessing, 40), new ClassSkill(LbDevotion, 40));
    }

    // Warchanter (Healer B) — buffer: per-race DMG + party mega-buff + party HoT + passive.
    private static void RegisterWarchanter()
    {
        ClassSkills.RegisterThird(Race.Human, Discipline.Warchanter,
            new ClassSkill(WcHumanBolt, 40), new ClassSkill(WcHumanChant, 40),
            new ClassSkill(WcHumanRenew, 40), new ClassSkill(WcHumanPass, 40));
        ClassSkills.RegisterThird(Race.Elf, Discipline.Warchanter,
            new ClassSkill(WcElfBolt, 40), new ClassSkill(WcElfChant, 40),
            new ClassSkill(WcElfRenew, 40), new ClassSkill(WcElfPass, 40));
        ClassSkills.RegisterThird(Race.Ork, Discipline.Warchanter,
            new ClassSkill(WcOrkBolt, 40), new ClassSkill(WcOrkChant, 40),
            new ClassSkill(WcOrkRenew, 40), new ClassSkill(WcOrkPass, 40));
    }
}
