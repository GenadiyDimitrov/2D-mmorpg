namespace Game.Shared;

/// <summary>
/// The 3rd-class identity (level 40). Each 2nd-class Archetype splits into TWO
/// disciplines. A <b>Discipline</b> is the shared "main idea"; <b>Discipline +
/// Race</b> is how that idea is expressed (a different skill list per race). So
/// there are 12 disciplines (6 archetypes × 2) and 36 third classes (× 3 races).
/// </summary>
public enum Discipline
{
    // Tank
    Bulwark = 0,      // defensive wall: near-immortal, minimal damage
    Vanguard = 1,     // offensive tank: high defence + real damage, punished if careless
    // Warrior
    Ravager = 2,      // pure single-target burst, low survivability
    Warlord = 3,      // balanced bruiser with AoE
    // Rogue
    Phantom = 4,      // hide, high evasion, ambush burst
    Venomweaver = 5,  // DoT stacks then burst (blink in / escape / restack)
    // Archer
    Sharpshooter = 6, // long range, high single-target damage
    Trapper = 7,      // utility / traps / crowd control
    // Healer
    Lightbringer = 8, // pure healer: AoE heals + single-target shields
    Warchanter = 9,   // buffer: stat buffs + heal-over-time (farm-oriented)
    // Nuker
    Magus = 10,       // single-target glass cannon
    Tempest = 11,     // AoE damage + control
    // Rogue, continued — the ARCHER MERGE (2026-07-29). Bow and dagger are one class to 40, and the
    // split at 40 is RACE-BASED, so the rogue line needs two more disciplines. APPENDED, never
    // renumbered: these values are persisted on characters.
    Nullblade = 12,   // HUMAN melee rogue — anti-magic dagger (stealth + bleed + crit)
    Hunter = 13       // ORK ranged rogue — the ork's own bow kit (damage focus, party atk buff)
}

/// <summary>Maps between archetypes and their two disciplines.</summary>
public static class Disciplines
{
    /// <summary>The two disciplines an archetype splits into (branch A, branch B).
    ///
    /// RACE-AWARE, because of the archer merge (owner, 2026-07-29): the ROGUE now covers both dagger
    /// and bow to level 40, and which pair of specialisations it opens into depends on the race —
    /// each race gets one melee and one ranged branch, with its own identity:
    ///
    ///   Human — Nullblade (anti-magic dagger)      / Sharpshooter (accuracy, single-target)
    ///   Ork   — Venomweaver (venom DoT dagger)     / Hunter (ork bow kit)
    ///   Elf   — Phantom (physical-evasion dagger)  / Trapper (utility bow)
    ///
    /// That mirrors the race flavours already written into docs/design/Disciplines.md — "human evades
    /// magic, the elf evades phys, the ork should outlive the target" — so no kit had to be invented.
    /// Every other archetype ignores race and returns the same pair for everyone.</summary>
    public static (Discipline A, Discipline B) Of(Race race, Archetype a) => a switch
    {
        Archetype.Tank    => (Discipline.Bulwark, Discipline.Vanguard),
        Archetype.Warrior => (Discipline.Ravager, Discipline.Warlord),
        Archetype.Rogue   => race switch
        {
            Race.Ork => (Discipline.Venomweaver, Discipline.Hunter),
            Race.Elf => (Discipline.Phantom, Discipline.Trapper),
            _        => (Discipline.Nullblade, Discipline.Sharpshooter),   // Human
        },
        // No 2nd class carries Archer any more (see ClassCatalog's ARCHER MERGE note); kept so an old
        // persisted value still resolves to something sane rather than falling through to Bulwark.
        Archetype.Archer  => (Discipline.Sharpshooter, Discipline.Trapper),
        Archetype.Healer  => (Discipline.Lightbringer, Discipline.Warchanter),
        Archetype.Nuker   => (Discipline.Magus, Discipline.Tempest),
        _ => (Discipline.Bulwark, Discipline.Vanguard),
    };

    /// <summary>A one-line "what this discipline does" blurb, shown by the grandmaster
    /// before the (irreversible) 3rd-class choice — the discipline sibling of
    /// <see cref="ClassCatalog.ArchetypeBlurb"/>.</summary>
    public static string Blurb(Discipline d) => d switch
    {
        Discipline.Bulwark      => "Bulwark — the immovable wall. Near-immortal defence, but deals little damage; built to outlast anything.",
        Discipline.Vanguard     => "Vanguard — the offensive tank. Heavy defence plus real damage, though careless play is punished.",
        Discipline.Ravager      => "Ravager — pure single-target burst. Tears one foe down fast, but fragile when focused.",
        Discipline.Warlord      => "Warlord — a balanced bruiser with area attacks. Sustained melee that also hits groups.",
        Discipline.Phantom      => "Phantom — stealth and evasion. Vanishes, then opens with a devastating ambush.",
        Discipline.Venomweaver  => "Venomweaver — stacks damage-over-time, then bursts. Blinks in, restacks, escapes.",
        Discipline.Sharpshooter => "Sharpshooter — extreme range and high single-target damage; the premier ranged striker.",
        Discipline.Trapper      => "Trapper — utility and control: traps, snares and crowd control over raw damage.",
        Discipline.Lightbringer => "Lightbringer — the pure healer. Area heals and single-target shields keep the party alive.",
        Discipline.Warchanter   => "Warchanter — the buffer. Stat buffs and heal-over-time; built to empower allies and farm.",
        Discipline.Magus        => "Magus — single-target glass cannon. The highest burst spells, but very fragile.",
        Discipline.Tempest      => "Tempest — area spells and control. Devastates groups and locks them down.",
        Discipline.Nullblade    => "Nullblade — the anti-magic dagger. Strikes from stealth and leaves foes' spells failing.",
        Discipline.Hunter       => "Hunter — the ork bow. Raw ranged damage, and a war cry that sharpens the whole party.",
        _ => ""
    };

    /// <summary>Is this one of the three RANGED (bow) rogue disciplines? The archer merge made
    /// bow-vs-dagger a level-40 choice, so the discipline — not the archetype — is what tells a
    /// bow character from a dagger one. Used by <see cref="SkillCatalog.FloorPassiveFor"/>: after
    /// 40 the ranged branches stop taking Evasion Mastery rungs (playtest-19 M7, *"the archer
    /// should not have evasion mastery after 40 .. the 10% are ok"*).</summary>
    public static bool IsRanged(Discipline d) =>
        d is Discipline.Sharpshooter or Discipline.Trapper or Discipline.Hunter;

    /// <summary>The parent archetype a discipline evolves from.</summary>
    public static Archetype Parent(Discipline d) => d switch
    {
        Discipline.Bulwark or Discipline.Vanguard => Archetype.Tank,
        Discipline.Ravager or Discipline.Warlord => Archetype.Warrior,
        // Every rogue-line discipline — melee AND ranged — parents to Rogue now, because Rogue is the
        // 2nd class all six evolve from. This is what makes the 2nd-class skill lookup work: the
        // discipline is asked for its parent to find the class table it continues.
        Discipline.Phantom or Discipline.Venomweaver or Discipline.Nullblade => Archetype.Rogue,
        Discipline.Sharpshooter or Discipline.Trapper or Discipline.Hunter => Archetype.Rogue,
        Discipline.Lightbringer or Discipline.Warchanter => Archetype.Healer,
        Discipline.Magus or Discipline.Tempest => Archetype.Nuker,
        _ => Archetype.Tank,
    };
}

/// <summary>A 3rd class. It evolves a specific 2nd class (ParentSecondClassId,
/// which the character must already hold) along one Discipline. Bonus is the
/// placeholder stat lean that gives each discipline some feel until the real
/// per-(race,discipline) skill sets land in the content slices.</summary>
public record ThirdClassDef(int Id, string Name, Race Race, int ParentSecondClassId,
    Discipline Discipline, ClassFlatBonus? Bonus = null);

/// <summary>
/// The 36 third classes, generated over <see cref="ClassCatalog.Playable"/>:
/// each playable 2nd class yields its two disciplines. Ids live at 101-136 so
/// they never collide with 2nd-class ids (1-18) or the retired God ids (98/99).
/// </summary>
public static class ThirdClassCatalog
{
    /// <summary>Character level required to take a 3rd class.</summary>
    public const int ChangeLevel = 40;

    /// <summary>Level to ADD A NEW SUBCLASS. Owner's rule with no 4th tier yet: you must have your 3rd
    /// class AND be level 75 (a 4th class would be the "real" gate, but we don't force subclasses toward
    /// one). Adding also requires EVERY class you own to already be at this level (see HandleDebugAddSubclass).</summary>
    public const int SubclassLevel = 75;

    private static readonly Dictionary<int, ThirdClassDef> All = Build();

    private static Dictionary<int, ThirdClassDef> Build()
    {
        var d = new Dictionary<int, ThirdClassDef>();
        foreach (var sc in ClassCatalog.Playable)
        {
            var (a, b) = Disciplines.Of(sc.Race, sc.Archetype);
            int baseId = 100 + (sc.Id - 1) * 2;   // 2nd id 1 -> 101/102 ... 18 -> 135/136
            d[baseId + 1] = new ThirdClassDef(baseId + 1, a.ToString(), sc.Race, sc.Id, a, FlatFor(a));
            d[baseId + 2] = new ThirdClassDef(baseId + 2, b.ToString(), sc.Race, sc.Id, b, FlatFor(b));
        }
        return d;
    }

    public static ThirdClassDef? Get(int id) => All.GetValueOrDefault(id);

    public static IEnumerable<ThirdClassDef> Playable => All.Values.OrderBy(c => c.Id);

    /// <summary>The (two) third classes a given 2nd class can evolve into.</summary>
    public static IEnumerable<ThirdClassDef> ForParent(int secondClassId) =>
        All.Values.Where(c => c.ParentSecondClassId == secondClassId).OrderBy(c => c.Id);

    /// <summary>Placeholder flat stat lean per discipline (real identity will come
    /// from skills in the content slices). Applied additively in RecomputeDerived.</summary>
    private static ClassFlatBonus FlatFor(Discipline d) => d switch
    {
        Discipline.Bulwark      => new ClassFlatBonus(MaxHp: 220, Defence: 45),
        Discipline.Vanguard     => new ClassFlatBonus(MaxHp: 130, Defence: 22, Attack: 18),
        Discipline.Ravager      => new ClassFlatBonus(Attack: 45, MaxHp: 40),
        Discipline.Warlord      => new ClassFlatBonus(Attack: 26, MaxHp: 90),
        Discipline.Phantom      => new ClassFlatBonus(Evasion: 32, Attack: 16),
        Discipline.Venomweaver  => new ClassFlatBonus(Attack: 22, Accuracy: 10),
        Discipline.Sharpshooter => new ClassFlatBonus(Attack: 32, Accuracy: 16),
        Discipline.Trapper      => new ClassFlatBonus(Attack: 16, Accuracy: 12, Evasion: 12),
        Discipline.Lightbringer => new ClassFlatBonus(MaxMp: 200, Defence: 18, MaxHp: 40),
        Discipline.Warchanter   => new ClassFlatBonus(MaxMp: 150, MaxHp: 90),
        Discipline.Magus        => new ClassFlatBonus(MaxMp: 170, Attack: 28),
        Discipline.Tempest      => new ClassFlatBonus(MaxMp: 130, MaxHp: 70, Attack: 16),
        _ => new ClassFlatBonus(),
    };
}
