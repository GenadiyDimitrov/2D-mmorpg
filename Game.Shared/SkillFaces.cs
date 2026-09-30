namespace Game.Shared;

// =====================================================================================================
//  SKILL FACES — what a skill LOOKS like, apart from what it DOES (owner, 2026-09-30, `BL-327`).
//
//  *"is it possible to tell a skill to use "this" shell for the visuals (name/description/icon/animation)
//  but underneath to be "that" skill?"* — yes: `docs/data/skill_faces.csv` is the shell, the class CSVs
//  are the numbers. His split, verbatim: *"the class csv is the numbers per lvl while the face is the
//  display"*. One skill id, one ladder, one balance; as many faces as he authors.
//
//  🔑 HOW A FACE IS CHOSEN — most specific wins:
//     1. a row whose CLASS is anywhere in the character's lineage, nearest first
//        (4th name → 3rd name → 2nd name → "Fighter"/"Mage")
//     2. a row whose RACE is the character's (CLASS blank)
//     3. the blank row (no race, no class) — every skill has one; mobs and NPCs always get it
//
//  🔑 THE DESCRIPTION IS PRE-RENDERED PER LEVEL by `SkillCsvSeed --gen-faces` (SkillFaces.g.cs). His
//  template's `@` / `@{m.def}` placeholders are filled from the SAME numbers `--check` proves against the
//  class CSVs, so a retune can never leave a stale number in a tooltip. Nothing is templated at runtime.
//
//  🔑 A BUFF WEARS ITS CASTER'S FACE. An Elf's Might on a Human reads "Forest Might" on the Human's bar:
//  the face is resolved from the caster at cast time and its <see cref="SkillFace.Id"/> is stored on the
//  buff (and persisted), so it survives a relog without knowing who cast it.
// =====================================================================================================

/// <summary>One row of <c>skill_faces.csv</c>, with its description already rendered for every level.</summary>
public sealed class SkillFace
{
    public string SkillId { get; }
    /// <summary>"human" / "elf" / "demon", or "" for every race.</summary>
    public string Race { get; }
    /// <summary>A class name from the roster ("Ice Master"), or "" for the whole race.</summary>
    public string ClassName { get; }
    public string Name { get; }
    private readonly string[] _descriptions;   // [0] = level 1; ONE entry = the same text at every level

    public SkillFace(string skillId, string race, string className, string name, string[] descriptions)
    {
        SkillId = skillId; Race = race; ClassName = className; Name = name; _descriptions = descriptions;
    }

    /// <summary>Stable key for this face — what a buff stores so it keeps its caster's face after a relog.</summary>
    public string Id => SkillId + "|" + Race + "|" + ClassName;

    /// <summary>The rendered description at a level; clamps to the ladder, so level 0 (not learned yet)
    /// reads as rung 1.</summary>
    public string DescriptionAt(int level)
    {
        if (_descriptions.Length == 0) return "";
        return _descriptions[Math.Clamp(level - 1, 0, _descriptions.Length - 1)];
    }

    /// <summary>The name to show at a level. A per-RUNG name the code authored (the Grade ladders) still
    /// shows while the face is the skill's own default name; an authored face name always wins.</summary>
    public string NameAt(SkillDef? def, int level) =>
        def is not null && Name == def.Name ? def.NameAt(level) : Name;
}

public static partial class SkillFaces
{
    private static readonly Dictionary<string, List<SkillFace>> BySkill = new();
    private static readonly Dictionary<string, SkillFace> ById = new();

    static SkillFaces()
    {
        foreach (var line in Data.Split('\n'))
        {
            if (line.Length == 0) continue;
            var f = line.Split('\t');
            if (f.Length < 5) continue;
            var face = new SkillFace(f[0], f[1], f[2], f[3], f.Skip(4).ToArray());
            if (!BySkill.TryGetValue(face.SkillId, out var list)) BySkill[face.SkillId] = list = new List<SkillFace>();
            list.Add(face);
            ById[face.Id] = face;
        }
    }

    /// <summary>Every face the file carries — the checker's view.</summary>
    public static IEnumerable<SkillFace> All => BySkill.Values.SelectMany(l => l);

    /// <summary>A face by its stored id (see <see cref="SkillFace.Id"/>); null if the row was since removed.</summary>
    public static SkillFace? Get(string? id) =>
        string.IsNullOrEmpty(id) ? null : ById.GetValueOrDefault(id!);

    /// <summary>The class names a character answers to, most specific first: 4th, 3rd, 2nd, base class.</summary>
    public static List<string> Lineage(Race race, BaseClass baseClass, Archetype? archetype,
                                       Discipline? discipline, bool fourth)
    {
        var names = new List<string>(4);
        if (discipline is Discipline d)
        {
            if (fourth) names.Add(ClassNames.Fourth(d, race));
            names.Add(ClassNames.Third(d, race));
        }
        if (archetype is Archetype a
            && ClassCatalog.OptionsFor(race, baseClass).FirstOrDefault(c => c.Archetype == a) is { } second)
            names.Add(second.Name);
        names.Add(baseClass.ToString());
        return names;
    }

    /// <summary>THE lookup. <paramref name="race"/> null = a creature or NPC: only the blank row.</summary>
    public static SkillFace? For(string skillId, Race? race, IReadOnlyList<string>? lineage)
    {
        if (!BySkill.TryGetValue(skillId, out var rows)) return null;
        if (race is Race r)
        {
            string rs = RaceKey(r);
            if (lineage is not null)
                foreach (var cls in lineage)
                    foreach (var row in rows)
                        if (row.ClassName.Length > 0
                            && string.Equals(row.ClassName, cls, StringComparison.OrdinalIgnoreCase)
                            && (row.Race.Length == 0 || row.Race == rs))
                            return row;
            foreach (var row in rows)
                if (row.ClassName.Length == 0 && row.Race == rs) return row;
        }
        foreach (var row in rows)
            if (row.ClassName.Length == 0 && row.Race.Length == 0) return row;
        return null;
    }

    /// <summary>Convenience over <see cref="For(string, Race?, IReadOnlyList{string})"/> for callers that
    /// hold the class as its parts.</summary>
    public static SkillFace? For(string skillId, Race race, BaseClass baseClass, Archetype? archetype,
                                 Discipline? discipline, bool fourth) =>
        For(skillId, race, Lineage(race, baseClass, archetype, discipline, fourth));

    /// <summary>The name a skill shows under a face, falling back to the def when the file has no row.</summary>
    public static string NameOf(SkillFace? face, string skillId, int level)
    {
        var def = SkillCatalog.Get(skillId);
        if (face is not null) return face.NameAt(def, level);
        return def?.NameAt(level) ?? skillId;
    }

    /// <summary>The description under a face, falling back to the def when the file has no row.</summary>
    public static string DescriptionOf(SkillFace? face, string skillId, int level)
    {
        if (face is not null) return face.DescriptionAt(level);
        return SkillCatalog.Get(skillId)?.DescriptionAt(level) ?? "";
    }

    /// <summary>The file's spelling of a race.</summary>
    public static string RaceKey(Race r) => r.ToString().ToLowerInvariant();
}
