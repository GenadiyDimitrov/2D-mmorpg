namespace Game.Shared;

/// <summary>Gameplay archetype of a second class. The 18 named classes from
/// the design doc map onto 6 archetypes that drive skills and bonuses;
/// per-class flavour differences (race passives etc.) come later.</summary>
public enum Archetype
{
    Tank = 0,     // heavy+shield: fortress
    Warrior = 1,  // heavy+2h: big hits
    Rogue = 2,    // light+dual: fast melee
    Archer = 3,   // light+bow: ranged basic attacks (+500 range)
    Healer = 4,   // robe+1h: heals/buffs (+500 spell range)
    Nuker = 5     // robe+2h: damage spells (+500 spell range)
}

/// <summary>A second class. RequiredQuestId is groundwork for class-change
/// quests — null means level-gated only (current behaviour); when the quest
/// system lands, a non-null id will require that quest be completed first.</summary>
/// <summary>A bundle of flat stat deltas. ⚠ Despite the name it is an ARMOR-SET type now
/// (<see cref="ArmorSetDef"/>) — <b>no class grants stats any more</b>. Both the 2nd-class and the
/// 3rd-class `Bonus` fields were deleted 2026-08-10 on the owner's ruling that class identity is the
/// skill/passive kit alone; see the note on the deleted `FlatFor` in Classes.Third.cs. Do not add a
/// `Bonus` back to a class def. Both primary and secondary deltas are supported.</summary>
public record ClassFlatBonus(
    int Con = 0, int Atk = 0, int Wit = 0, int Agi = 0,        // primary
    int MaxHp = 0, int MaxMp = 0, int Defence = 0, int Attack = 0, // secondary
    int Evasion = 0, int Accuracy = 0)
{
    /// <summary>Every field scaled — used to derive a lower-QUALITY armour set's bonus from the
    /// authored (Mythic) one. See StatMods.Scaled.</summary>
    public ClassFlatBonus Scaled(float f) => new(
        (int)MathF.Round(Con * f), (int)MathF.Round(Atk * f),
        (int)MathF.Round(Wit * f), (int)MathF.Round(Agi * f),
        (int)MathF.Round(MaxHp * f), (int)MathF.Round(MaxMp * f),
        (int)MathF.Round(Defence * f), (int)MathF.Round(Attack * f),
        (int)MathF.Round(Evasion * f), (int)MathF.Round(Accuracy * f));
}

public record SecondClassDef(int Id, string Name, Race Race, BaseClass Base, Archetype Archetype,
    string? RequiredQuestId = null);

public static class ClassCatalog
{
    // ===== THE ARCHER MERGE (owner, 2026-07-29) ==================================================
    // Archer is no longer a 2ND class. Bow and dagger are ONE class until 40 — you are a Rogue, you
    // learn both the Stab and the Shot ladders, and the split happens at the 3rd class (see
    // Disciplines.Of, which is race-aware for exactly this).
    //
    // Ids 4 (Hunter/Ork), 10 (Warden/Elf) and 16 (Marksman/Human) are therefore GONE. They are left
    // as gaps, never reused: class ids are persisted on characters, so recycling one would silently
    // turn an old save into a different class. `Archetype.Archer` stays in the enum — the bow range
    // tier and the HP track still refer to it — but no 2nd class carries it any more.
    //
    // This also closed the bug that started it: Archer's 2nd-class table only ever had two skills
    // (BattleFury @20, PowerShot @24) while every other archetype had a full 20-36 ladder, so archers
    // were hollow. The Rogue table already taught BOTH weapons, so the merge fixed it by deletion.
    private static readonly Dictionary<int, SecondClassDef> All = new SecondClassDef[]
    {
        // Ork / Demon
        new(1,  "Beast",       Race.Ork,   BaseClass.Fighter, Archetype.Tank),
        new(2,  "Warrior",     Race.Ork,   BaseClass.Fighter, Archetype.Warrior),
        new(3,  "Stalker",     Race.Ork,   BaseClass.Fighter, Archetype.Rogue),
        // (id 4 was Hunter, the Ork archer — see the ARCHER MERGE note below. The name now belongs to
        //  the Ork's ranged 3rd-class discipline.)
        new(5,  "Shaman",      Race.Ork,   BaseClass.Mage,    Archetype.Healer),
        new(6,  "Witch",       Race.Ork,   BaseClass.Mage,    Archetype.Nuker),
        // Elf / Angel
        new(7,  "Templar",     Race.Elf,   BaseClass.Fighter, Archetype.Tank),
        new(8,  "Sentinel",    Race.Elf,   BaseClass.Fighter, Archetype.Warrior),
        new(9,  "Shadowblade", Race.Elf,   BaseClass.Fighter, Archetype.Rogue),
        // (id 10 was Warden, the Elf archer — merged into Shadowblade; see the ARCHER MERGE note.)
        new(11, "Priest",      Race.Elf,   BaseClass.Mage,    Archetype.Healer),
        new(12, "Inquisitor",  Race.Elf,   BaseClass.Mage,    Archetype.Nuker),
        // Human
        new(13, "Knight",      Race.Human, BaseClass.Fighter, Archetype.Tank),
        new(14, "Champion",    Race.Human, BaseClass.Fighter, Archetype.Warrior),
        new(15, "Assassin",    Race.Human, BaseClass.Fighter, Archetype.Rogue),
        // (id 16 was Marksman, the Human archer — merged into Assassin; see the ARCHER MERGE note.)
        // (Cleric was the ONE 2nd class of eighteen carrying a stat bonus — +60 MP/+30 HP/+10 Def,
        //  never mirrored on the other seventeen. Deleted 2026-08-10 with the whole class-bonus
        //  layer; see the ClassFlatBonus note above.)
        new(17, "Cleric",      Race.Human, BaseClass.Mage,    Archetype.Healer),
        new(18, "Sorcerer",    Race.Human, BaseClass.Mage,    Archetype.Nuker),
        // (The God race's two classes — 98 Demigod / 99 Ascendant — were deleted 2026-08-07 with the
        //  rest of the God layer, playtest-19 `0b`. Ids 98/99 stay retired; never reuse them.)
    }.ToDictionary(c => c.Id);

    public static SecondClassDef? Get(int id) => All.GetValueOrDefault(id);

    /// <summary>All second classes — the ones with quest chains. Kept as its own member (rather
    /// than folded into <c>All</c>) because it used to filter out the God debug race; every class
    /// is playable now, and this is still the name the rest of the code asks for.</summary>
    public static IEnumerable<SecondClassDef> Playable =>
        All.Values.OrderBy(c => c.Id);

    public static IEnumerable<SecondClassDef> OptionsFor(Race race, BaseClass baseClass) =>
        All.Values.Where(c => c.Race == race && c.Base == baseClass).OrderBy(c => c.Id);

    /// <summary>A one-line "what this class does" blurb by archetype, shown to the
    /// player by the quest giver before they commit to a class.</summary>
    public static string ArchetypeBlurb(Archetype a) => a switch
    {
        Archetype.Tank    => "Tank — heavy armor + shield. Soaks hits and holds enemies; the hardest to kill and the party's wall.",
        Archetype.Warrior => "Warrior — heavy armor + two-handed weapons. Raw melee damage with strong staying power.",
        Archetype.Rogue   => "Rogue — light armor + dual daggers. Fast, evasive, crit-heavy melee that interrupts casters.",
        Archetype.Archer  => "Archer — light armor + bow. Ranged physical attacks with extended range and high crit rate.",
        Archetype.Healer  => "Healer — robe or light armor. Heals, buffs and cleanses allies; in light armor can melee to farm solo.",
        Archetype.Nuker   => "Nuker — robe + staff. Devastating offensive spells at long range, but fragile up close.",
        _ => ""
    };

    // The permanent core-stat bonus that used to be applied at class change (tank +10 CON,
    // nuker +10 WIT, …) is GONE. Main stats are the ones you were born with; the only thing
    // that moves them is the level-40 stat-swap passives. Class identity comes from skills,
    // the weapon, and the Class Balance passive — not from a hidden stat grant.
}
