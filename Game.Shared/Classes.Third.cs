namespace Game.Shared;

/// <summary>
/// The 3rd-class identity (level 40). A 2nd-class Archetype splits into one or TWO
/// disciplines. A <b>Discipline</b> is the shared "main idea"; <b>Discipline +
/// Race</b> is how that idea is expressed (a different skill list per race).
///
/// <para>⚠ <b>Not every archetype splits.</b> The NUKER and the TANK offer ONE discipline each since
/// 2026-08-28 (`BL-97`), so ids 102, 114, 126 (tank) and 112, 124, 136 (nuker) are permanently
/// vacant. <b>TWELVE discipline values are live</b> (of the fourteen the enum holds), and they make
/// <b>EIGHT choosable paths per race</b> — tank, ravager, war_aoe, dual, archer, healer, buffer,
/// nuker — which is the roster his 2026-08-17 map drew. See <see cref="Disciplines.Of"/>.</para>
/// </summary>
public enum Discipline
{
    // Tank
    Bulwark = 0,      // the ONLY tank discipline — three identities, one per race
    // ⚠ RETIRED 2026-08-28 (`BL-97`, owner: *"Remove the vacant tank as well .. the 3 tanks must have
    //   their name and the other is the same for the 3 races ... So is the one that must go"*). Same
    //   treatment as the Tempest below: the VALUE stays because it was persisted on characters, and
    //   nothing mints, names, offers or teaches it any more. Never renumber, never reuse.
    //   🔑 His TEST for which of a pair dies is worth keeping: the one whose name is the SAME for all
    //   three races is the one that was never really three classes.
    Vanguard = 1,     // RETIRED — was "offensive tank"; the warrior covers it (*"we have a warrior for that"*)
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
    Magus = 10,       // the ONLY nuker discipline — three identities, one per race
    // ⚠ RETIRED 2026-08-28 (`BL-97`, owner: *"Tempests must go"* — one nuker per race). The VALUE stays
    //   because it was persisted on characters; nothing offers it, names it or teaches it any more, and
    //   ThirdClassCatalog no longer mints a class for it. Never renumber and never reuse the number for
    //   something else. What an old save does: see ThirdClassCatalog.Surviving.
    Tempest = 11,     // RETIRED — was "AoE damage + control"; its kit was always Magus's too
    // Rogue, continued — the ARCHER MERGE (2026-07-29). Bow and dagger are one class to 40, and the
    // split at 40 is RACE-BASED, so the rogue line needs two more disciplines. APPENDED, never
    // renumbered: these values are persisted on characters.
    Nullblade = 12,   // HUMAN melee rogue — anti-magic dagger (stealth + bleed + crit)
    Hunter = 13       // ORK ranged rogue — the demon's own bow kit (damage focus, party atk buff)
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
    ///   Demon   — Venomweaver (venom DoT dagger)     / Hunter (demon bow kit)
    ///   Elf   — Phantom (physical-evasion dagger)  / Trapper (utility bow)
    ///
    /// That mirrors the race flavours already written into docs/design/Disciplines.md — "human evades
    /// magic, the elf evades phys, the demon should outlive the target" — so no kit had to be invented.
    /// Every other archetype ignores race and returns the same pair for everyone.
    ///
    /// <para>⚠ <b>B IS NULLABLE.</b> An archetype may offer only ONE discipline — the Nuker does, since
    /// `BL-97` retired the Tempest — and then the catalog mints no second class for it at all. A null B
    /// is not "A twice"; there is simply no second door.</para></summary>
    public static (Discipline A, Discipline? B) Of(Race race, Archetype a) => a switch
    {
        // ONE TANK PER RACE (owner, 2026-08-28 — `BL-97`, the Tempest's ruling extended to the tank on
        // the same day). His 2026-08-17 map had already dropped it — *"one tank is enough — wont have
        // the vanguard the off tank, we have a warrior for that"* — and the 2026-08-10 purge had
        // already taken every Vanguard learn line, so this cost even less than the Tempest did: it was
        // an empty class that could still be chosen. Three identities, one per race: Bulwark / Aegis /
        // Ironhide. ⚠ The NAME `Vanguard` is free to be reused elsewhere — a name is not an id.
        Archetype.Tank    => (Discipline.Bulwark, null),
        Archetype.Warrior => (Discipline.Ravager, Discipline.Warlord),
        Archetype.Rogue   => race switch
        {
            Race.Demon => (Discipline.Venomweaver, Discipline.Hunter),
            Race.Elf => (Discipline.Phantom, Discipline.Trapper),
            _        => (Discipline.Nullblade, Discipline.Sharpshooter),   // Human
        },
        // No 2nd class carries Archer any more (see ClassCatalog's ARCHER MERGE note); kept so an old
        // persisted value still resolves to something sane rather than falling through to Bulwark.
        Archetype.Archer  => (Discipline.Sharpshooter, Discipline.Trapper),
        Archetype.Healer  => (Discipline.Lightbringer, Discipline.Warchanter),
        // ONE NUKER PER RACE (owner, 2026-08-28 — `BL-97`: *"Tempests must go"*). The Tempest was the
        // second branch until that day and was never a different character: by his own 2026-08-10
        // ruling a class grants NO stats, and `nuker 3rd.csv` carries no discipline column, so both
        // disciplines were registered the very same 208-row kit. Retiring it deleted a duplicate, not
        // content — not one authored row was lost. The three identities the archetype has are the three
        // RACES (Magus / Starweaver / Cinderwitch), which is the shape he asked for in the 2026-08-17
        // map: *"same logic as the tank, 1 discipline ... 3 identities"*.
        Archetype.Nuker   => (Discipline.Magus, null),
        _ => (Discipline.Bulwark, null),
    };

    /// <summary>A one-line "what this discipline does" blurb, shown by the grandmaster
    /// before the (irreversible) 3rd-class choice — the discipline sibling of
    /// <see cref="ClassCatalog.ArchetypeBlurb"/>.</summary>
    public static string Blurb(Discipline d) => d switch
    {
        Discipline.Bulwark      => "Bulwark — the immovable wall. Near-immortal defence, but deals little damage; built to outlast anything.",
        Discipline.Ravager      => "Ravager — pure single-target burst. Tears one foe down fast, but fragile when focused.",
        Discipline.Warlord      => "Warlord — a balanced bruiser with area attacks. Sustained melee that also hits groups.",
        Discipline.Phantom      => "Phantom — stealth and evasion. Vanishes, then opens with a devastating ambush.",
        Discipline.Venomweaver  => "Venomweaver — stacks damage-over-time, then bursts. Blinks in, restacks, escapes.",
        Discipline.Sharpshooter => "Sharpshooter — extreme range and high single-target damage; the premier ranged striker.",
        Discipline.Trapper      => "Trapper — utility and control: traps, snares and crowd control over raw damage.",
        Discipline.Lightbringer => "Lightbringer — the pure healer. Area heals and single-target shields keep the party alive.",
        Discipline.Warchanter   => "Warchanter — the buffer. Stat buffs and heal-over-time; built to empower allies and farm.",
        Discipline.Magus        => "Magus — the master of offensive magic. Devastating spells single and wide, but very fragile.",
        Discipline.Nullblade    => "Nullblade — the anti-magic dagger. Strikes from stealth and leaves foes' spells failing.",
        Discipline.Hunter       => "Hunter — the demon bow. Raw ranged damage, and a war cry that sharpens the whole party.",
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
        // Vanguard is retired (`BL-97`) but keeps its parent, same as the Tempest: a value read off an
        // old row must still resolve to the archetype it belonged to.
        Discipline.Bulwark or Discipline.Vanguard => Archetype.Tank,
        Discipline.Ravager or Discipline.Warlord => Archetype.Warrior,
        // Every rogue-line discipline — melee AND ranged — parents to Rogue now, because Rogue is the
        // 2nd class all six evolve from. This is what makes the 2nd-class skill lookup work: the
        // discipline is asked for its parent to find the class table it continues.
        Discipline.Phantom or Discipline.Venomweaver or Discipline.Nullblade => Archetype.Rogue,
        Discipline.Sharpshooter or Discipline.Trapper or Discipline.Hunter => Archetype.Rogue,
        Discipline.Lightbringer or Discipline.Warchanter => Archetype.Healer,
        // Tempest is retired (`BL-97`) but keeps its parent: a value read off an old row must still
        // resolve to the archetype it belonged to rather than falling through to Tank.
        Discipline.Magus or Discipline.Tempest => Archetype.Nuker,
        _ => Archetype.Tank,
    };
}

/// <summary>A 3rd class. It evolves a specific 2nd class (ParentSecondClassId,
/// which the character must already hold) along one Discipline. It carries NO stats —
/// a discipline is its skill kit and nothing else (owner, 2026-08-10; see the note on
/// the deleted `FlatFor` below).</summary>
public record ThirdClassDef(int Id, string Name, Race Race, int ParentSecondClassId,
    Discipline Discipline);

/// <summary>
/// The third classes, generated over <see cref="ClassCatalog.Playable"/>: each playable 2nd class
/// yields its one or two disciplines. Ids live at 101-136 so they never collide with 2nd-class ids
/// (1-18) or the retired God ids (98/99).
///
/// <para>⚠ <b>TWENTY-FOUR CLASSES IN A 36-WIDE ID SPACE.</b> The id is computed from the PARENT's
/// id, never from a running counter, so the space is deliberately full of holes and always was:
/// only <b>15</b> of the 18 second-class ids are playable (4/10/16 are the retired archers), which
/// left 30 third classes; retiring the Tempest and the Vanguard (`BL-97`, 2026-08-28) unminted
/// 102, 112, 114, 124, 126 and 136 as well, leaving <b>24</b>. Every surviving id kept the number it
/// has always had — that is the whole reason the scheme is positional rather than sequential.
/// 🔴 All twelve dead numbers, and their ascensions (202/212/214/224/226/236 plus 207/208, 219/220,
/// 231/232), are dead forever. Never reuse one. ⚠ <b>Count classes by asking the catalog</b>
/// (`Playable.Count()`), never by multiplying — "18 × 2 = 36" was written in this very comment for
/// months and was never true.</para>
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
            // The NAME is per (discipline, RACE) now — see ClassNames. It used to be
            // `a.ToString()`, which is why every race's tank read "Bulwark". The id is unchanged
            // and is the only thing persisted, so this renames labels and nothing else.
            d[baseId + 1] = new ThirdClassDef(baseId + 1, ClassNames.Third(a, sc.Race), sc.Race, sc.Id, a);
            // The B slot is SKIPPED when the archetype has no second discipline (the nuker, `BL-97`).
            // Leaving the id unminted is deliberate: `Get` then returns null for it, which is what
            // `Surviving` below keys on.
            if (b is { } second)
                d[baseId + 2] = new ThirdClassDef(baseId + 2, ClassNames.Third(second, sc.Race), sc.Race, sc.Id, second);
        }
        return d;
    }

    public static ThirdClassDef? Get(int id) => All.GetValueOrDefault(id);

    public static IEnumerable<ThirdClassDef> Playable => All.Values.OrderBy(c => c.Id);

    /// <summary>Map a persisted 3rd-class id onto one that still exists — the load-time migration for
    /// a RETIRED discipline (`BL-97`: the Tempest, ids 112/124/136).
    ///
    /// <para>Why it is needed at all: a character stores the numeric class id, and everything
    /// downstream — the class NAME, the learn table, the 4th-class ascension — is looked up through
    /// <see cref="Get"/>. An id nobody mints any more resolves to null, so the character would show no
    /// class name, learn nothing above 40 and be unable to ascend at 76. That is a bricked character,
    /// not a cosmetic problem, which is why this exists even though a `game.db` reset is owed anyway.</para>
    ///
    /// <para>The rule is POSITIONAL, not a table of literals: a retired B slot is always one above its
    /// surviving A sibling (`baseId + 2` → `baseId + 1`), so a Tempest becomes that race's Magus and
    /// keeps its parent 2nd class. Anything else unknown is returned unchanged for the caller to
    /// reject.</para></summary>
    public static int Surviving(int thirdClassId) =>
        thirdClassId > 0 && !All.ContainsKey(thirdClassId) && All.ContainsKey(thirdClassId - 1)
            ? thirdClassId - 1
            : thirdClassId;

    /// <summary>The (two) third classes a given 2nd class can evolve into.</summary>
    public static IEnumerable<ThirdClassDef> ForParent(int secondClassId) =>
        All.Values.Where(c => c.ParentSecondClassId == secondClassId).OrderBy(c => c.Id);

    // ⚠ `FlatFor` — the per-discipline flat stat lean (Bulwark +220 HP/+45 Def, Ravager +45 Atk, …)
    //   — was DELETED 2026-08-10 on the owner's ruling: *"There is no identity. The identity is just
    //   the skills/passives kit … the magus and the tempest have same stats, just one has more dmg
    //   skills while the other more debuffs … no more u change your class and get bonus."*
    //
    //   Do NOT reinstate it, and do NOT re-home the same numbers as invented passives — he ruled on
    //   that too: *"Remove them, don't add them as passives. W8 on the 40+ csvs."* The lean comes
    //   back only when the level-40+ class CSVs land, authored inside the discipline's own kit.
    //
    //   The bug that killed it, worth remembering: this table was where `Discipline.Phantom` hid an
    //   `Evasion: 32` (and Trapper `Evasion: 12`) — nearly twice the entire ~18-point evasion budget
    //   (13 armor mastery + 4 Agility), and since 1 point = 1% miss (StatCaps.AvoidStatSlope) it was
    //   a silent flat +32% dodge. It is why a level-60 Elf Phantom read 140 evasion against the
    //   expected 108, and why evasion visibly JUMPED at the discipline change. An invisible bonus on
    //   a class def is unreadable and untunable — that is the whole case for the ruling.
    //
    //   The standing rule regardless of where stats are authored: evasion comes from armor mastery,
    //   buffs and AGI. `evade_mastery` raises the FLOOR only and grants no evasion; `Agi` is not a
    //   back door (it feeds evasion 1:1). See the note at Skills.Common.cs' evade_mastery.
}
