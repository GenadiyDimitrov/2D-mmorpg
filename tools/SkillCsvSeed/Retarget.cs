using System.Text;
using Game.Shared;

// =====================================================================================================
//  `--retarget` — REWRITE THE `TARGET` COLUMN OF EVERY SKILL CSV INTO HIS `[scope]/[breadth]` SCHEME.
//
//  His ruling, 2026-08-27:
//      *"the logic is [self-onlyMe/target-anyFriendly/party-anyPartyMemeber/enemy]/[single-affectOne/
//        aoe-affectsMany]; aoe depends on skill around the caster or around the target but still the
//        same logic its just the aoe circle where to execute"*
//
//  and the worked examples in the same message:
//      buffs      -> party/single    (one target, must be in your party)
//      harmonies  -> party/aoe       (many, party only)
//      heals      -> target/single   (anyone friendly, one target)
//      party heals-> party/aoe
//      totems +
//      Urgent Great Heal -> target/aoe   (anyone friendly in a radius)
//      dmg        -> enemy/single ; the waves -> enemy/aoe
//      self buffs -> self/single
//  plus his follow-up: *"a recharge should be party/single as buff but the heal is target/single"*.
//
//  🔑 THE OLD COLUMN CANNOT ANSWER THIS ON ITS OWN, which is the whole reason this is a tool and not a
//  find-and-replace. `self/target` collapses BOTH `party/single` (a buff) and `target/single` (a heal),
//  and `enemy` collapses `enemy/single` and `enemy/aoe`. Guessing from the word would get the split
//  wrong on exactly the rows he cares about.
//
//  🔑 SO IT ASKS THE CATALOG FIRST. For any row whose skill is BUILT, the answer is derived from the
//  real `SkillDef` — `TargetMode`, `AreaRadius`, `PlacesTotem`, and which effects it carries. That is
//  the same authority `--check` uses, and it means a row can only be wrong here if the code is wrong
//  too. Only rows with no `SkillDef` (a tier he has not authored yet) fall back to reading the old
//  column, and every one of those is printed so he can eye it.
//
//  ⚠ IT REWRITES FIELD 5 IN PLACE — a character-span replacement on the raw line, never a re-serialise.
//  The CSVs have been corrupted twice by tools that read them and wrote them back "helpfully"; nothing
//  here touches a byte outside the one field, quoting included. Run `git diff --numstat docs/data/`
//  after, as always.
// =====================================================================================================

internal static class Retarget
{
    /// <summary>What a row's TARGET column becomes, and how confident we are about it.</summary>
    private enum How
    {
        FromCatalog,   // derived from a real SkillDef — trustworthy
        FromOldColumn, // the skill is not built; mapped from the old word
        Judgement,     // mapped, but the old word was ambiguous and he should eyeball it
    }

    public static int Run(string csvDir)
    {
        var files = Directory.GetFiles(csvDir, "*.csv").OrderBy(f => f).ToArray();
        if (files.Length == 0) { Console.Error.WriteLine($"No CSVs under {csvDir}"); return 1; }

        // Name -> SkillDef, built once. Two skills can share a display name across races (the
        // per-class DisplayName override), which is fine: they agree on scope and breadth, and where
        // they would not, the first one still answers for a column that is about SHAPE, not payload.
        var byName = new Dictionary<string, SkillDef>(StringComparer.OrdinalIgnoreCase);

        // 🔑 CLASS-TABLE SKILLS FIRST, and the order is load-bearing. Names COLLIDE: three skills are
        // called "Sprint" — the rogue's actual skill and the two buff squares it hands out — and taking
        // whichever `AllSkills` yielded first gave the rogue's row `party/single` off a buff-square def
        // instead of `self/single` off the real one. `--check` compares against the CLASS TABLE, so
        // this must resolve the same way or the two tools disagree with each other by construction.
        // (It found exactly this, on exactly one row, which is the argument for having built it.)
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            foreach (var bc in new[] { BaseClass.Fighter, BaseClass.Mage })
                foreach (var cs in AllClassSkills(race, bc))
                    if (SkillCatalog.Get(cs.SkillId) is SkillDef sd)
                    {
                        byName.TryAdd(sd.Name.Trim(), sd);
                        // The per-class display name too, so "Chain Lightning" finds what it renames.
                        string disp = ClassSkills.DisplayName(cs.SkillId, race, bc, null, null);
                        if (!string.IsNullOrWhiteSpace(disp)) byName.TryAdd(disp.Trim(), sd);
                    }
        // Then everything else — potions, runes, mob spells, buff squares. A name that got here first
        // above keeps its class-table meaning.
        foreach (var d in SkillCatalog.AllSkills)
            byName.TryAdd(d.Name.Trim(), d);

        int rewritten = 0, unchanged = 0;
        var review = new List<string>();

        foreach (string path in files)
        {
            var lines = File.ReadAllLines(path);
            bool touched = false;
            bool stop = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                // Same gates `Check.ReadCsv` uses, so the two tools agree on what a ROW is.
                if (stop || line.Length == 0 || line.StartsWith('#') || line.StartsWith("LEARN")) continue;
                if (line.IndexOf("NOT DONE", StringComparison.OrdinalIgnoreCase) >= 0) { stop = true; continue; }

                var spans = FieldSpans(line);
                if (spans.Count < 12) continue;

                string name = Field(line, spans, 1).Trim();
                if (name.Length == 0) continue;              // a `----- Force -----` banner row
                string type = Field(line, spans, 2).Trim();
                string old  = Field(line, spans, 4).Trim();
                if (old.Length == 0) continue;               // nothing authored in the column

                var (neu, how) = Resolve(name, type, old, byName);
                if (neu is null) continue;

                if (!string.Equals(old, neu, StringComparison.Ordinal))
                {
                    lines[i] = line[..spans[4].Start] + neu + line[spans[4].End..];
                    touched = true;
                    rewritten++;
                }
                else unchanged++;

                if (how != How.FromCatalog)
                    review.Add($"  {Path.GetFileName(path),-18} {name,-28} {old,-12} -> {neu,-14}"
                             + (how == How.Judgement ? "  ⚠ judgement (not built — check this one)"
                                                     : "  (not built — mapped from the old word)"));
            }

            if (touched)
            {
                // Preserve the file's own line ending. These files come off Windows and a silent
                // LF rewrite would show as a whole-file diff and bury the real change.
                string raw = File.ReadAllText(path);
                string nl = raw.Contains("\r\n") ? "\r\n" : "\n";
                bool trailing = raw.EndsWith("\n");
                File.WriteAllText(path, string.Join(nl, lines) + (trailing ? nl : ""));
                Console.WriteLine($"  wrote {Path.GetFileName(path)}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{rewritten} row(s) rewritten, {unchanged} already correct.");
        if (review.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("--- ROWS THE CATALOG COULD NOT ANSWER (unbuilt skills, mapped from the old word) ---");
            Console.WriteLine();
            foreach (var r in review) Console.WriteLine(r);
            Console.WriteLine();
            Console.WriteLine("  These are the ones worth his eye. Everything not listed was derived from a real");
            Console.WriteLine("  SkillDef and is as right as the code is.");
        }
        return 0;
    }

    private static IEnumerable<ClassSkill> AllClassSkills(Race race, BaseClass bc)
    {
        foreach (Archetype a in Enum.GetValues<Archetype>())
            foreach (var cs in Safe(race, bc, a))
                yield return cs;

        static IEnumerable<ClassSkill> Safe(Race r, BaseClass b, Archetype a)
        {
            try { return ClassSkills.Cumulative(r, b, a, null); }
            catch { return Array.Empty<ClassSkill>(); }
        }
    }

    /// <summary>The new column value, and how much to trust it.</summary>
    private static (string? Value, How How) Resolve(
        string name, string type, string old, Dictionary<string, SkillDef> byName)
    {
        if (byName.TryGetValue(name, out var def))
            return (FromDef(def), How.FromCatalog);

        // ---- No SkillDef: the tier is unauthored. Map the old word, and say which calls were guesses.
        string t = type.ToLowerInvariant();
        bool buffish = t.Contains("buff") || t.Contains("passive");
        switch (old.ToLowerInvariant())
        {
            case "self":
            case "slef":                                   // his typo, 5 rows in `warrior 2nd.csv`
                return ("self/single", How.FromOldColumn);
            case "self/party":
                return ("party/aoe", How.FromOldColumn);
            case "ground":
                return ("target/aoe", How.FromOldColumn);
            case "dead allies":
                return ("target/aoe", How.FromOldColumn);
            case "dead ally":
            case "ally":
                return ("target/single", How.FromOldColumn);
            case "enemy":
                // `enemy` alone cannot say whether it is an AoE. Single is the safe default — most
                // damage skills are — and every one lands in the review list.
                return ("enemy/single", How.Judgement);
            case "self/target":
                // 🔑 THE ONE THAT MATTERS. His rule: a BUFF is party/single, a HEAL is target/single —
                // *"a recharge should be party/single as buff but the heal is target/single"*. TYPE is
                // the only signal an unbuilt row carries, and a recharge authored as `Magic/Heal` will
                // therefore land on target/single and need his correction. Flagged, not hidden.
                return (buffish ? "party/single" : "target/single", How.Judgement);
            default:
                return (null, How.FromOldColumn);
        }
    }

    /// <summary>Derive `[scope]/[breadth]` from the compiled skill. THE authoritative answer, and the
    /// one `--check` holds his CSV column against — so this method is the single definition of the
    /// scheme and nothing else may re-derive it.</summary>
    internal static string FromDef(SkillDef d)
    {
        // 🔑 A DEBUFF IS OFFENSIVE EVEN WITH NO DAMAGE ON IT, and forgetting that was the one class of
        // row this method got wrong in the wild. Armor Break, Weapon Break, Bind, Gravity and Mana
        // Strain carry no damage flag at all, matched none of the tests below, and fell through the
        // friendly branch to come out `party/single` — a healer's curse authored as a party buff. He
        // caught it reading `healer 4th.csv` (2026-08-27: *"healer 4th had its debuffs as party/single
        // -> enemy/single"*) and `--check` then reproduced all 40 rungs against his correction, which
        // is precisely the loop the column was made checkable for.
        //
        // `DebuffSchool` is the reliable marker — every contested debuff declares one, because it is
        // what picks the ATK-vs-CON / ATK-vs-WIT contest — with `Category` as the belt-and-braces for
        // anything authored as a debuff that never reaches the contest path.
        bool offensive = d.Effect.HasFlag(SkillEffect.PhysicalDamage)
                      || d.Effect.HasFlag(SkillEffect.MagicDamage)
                      || d.TargetMode == TargetMode.EnemiesInRadius
                      || d.Effect.HasFlag(SkillEffect.Cancel)
                      || d.DebuffSchool != DebuffSchool.None
                      || d.Category == SkillCategory.Debuff;

        // BREADTH first: anything with a real radius affects many, whatever it is centred on. His own
        // point — *"aoe depends on skill around the caster or around the target but still the same
        // logic its just the aoe circle where to execute"* — so the centre is deliberately NOT encoded.
        bool aoe = d.TargetMode is TargetMode.AlliesInRadius or TargetMode.FriendlyInRadius
                                or TargetMode.EnemiesInRadius
                || d.AreaRadius > 0f
                || d.PlacesTotem;

        if (offensive)
            return aoe ? "enemy/aoe" : "enemy/single";

        // 🔑 A PASSIVE IS ALWAYS `self/single`, and it needs saying explicitly. A passive sets no
        // TargetMode at all, so it inherits the record default `SelfOrTarget` and would otherwise fall
        // through the friendly branch below and come out `party/single` — which `--check` caught the
        // moment it was pointed at his files: 67 mastery and Anti-Magic rungs, every one of them a
        // defect in THIS method rather than in his column. It never leaves the caster; there is nothing
        // to target.
        if (d.Category == SkillCategory.Passive)
            return "self/single";

        // ⚠ SelfOnly is about who you CAST it on, not who it REACHES, and the two differ for a totem:
        // a Healing Totem is cast on yourself (SelfOnly) and then heals a circle. Testing SelfOnly
        // before the radius made it `self/single`, which is the opposite of his ruling that *"totems
        // are target/aoe"*. So the breadth is decided first and SelfOnly only answers when nothing
        // spreads.
        if (d.TargetMode == TargetMode.SelfOnly && !aoe)
            return "self/single";

        // A friendly skill. `target` = anyone friendly, `party` = party only.
        //
        // 🔑 SCOPE IS AUTHORED FOR AN AREA SKILL AND DERIVED FOR A SINGLE ONE, and that asymmetry is
        // his, not a shortcut:
        //
        //   * AREA — `AlliesInRadius` is `party/aoe` and `FriendlyInRadius` is `target/aoe`. It CANNOT
        //     be derived: Party Heal and Urgent Great Heal are both area heals and he puts them on
        //     opposite sides (*"harmonies are party/aoe … (party heals are party heals)"* against
        //     *"urgent great heal and totems are target/aoe"*). Two skills that reach different people
        //     need two modes, which is why `FriendlyInRadius` was added the same day.
        //
        //   * SINGLE — a HEAL reaches anyone friendly, a buff or a recharge is party business
        //     (*"a recharge should be party/single as buff but the heal is target/single"*). `Heal`
        //     and `Resurrect` travel; `RestoreMp` does not; a Cleanse rides with whatever carries it.
        if (aoe)
            return d.TargetMode == TargetMode.AlliesInRadius ? "party/aoe" : "target/aoe";

        bool travels = d.Effect.HasFlag(SkillEffect.Heal) || d.Resurrect;
        return travels ? "target/single" : "party/single";
    }

    // ---- raw-line field spans, so a rewrite touches ONE field and nothing else ---------------------

    private readonly record struct Span(int Start, int End);

    /// <summary>Character spans of each comma-separated field, honouring double quotes — the same
    /// quoting rule `Check.SplitCsv` reads with, but keeping POSITIONS instead of values.</summary>
    private static List<Span> FieldSpans(string line)
    {
        var spans = new List<Span>();
        bool q = false;
        int start = 0;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (q)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') i++;
                else if (c == '"') q = false;
            }
            else if (c == '"') q = true;
            else if (c == ',') { spans.Add(new Span(start, i)); start = i + 1; }
        }
        spans.Add(new Span(start, line.Length));
        return spans;
    }

    private static string Field(string line, List<Span> spans, int i) =>
        i < spans.Count ? line[spans[i].Start..spans[i].End].Trim('"') : "";
}
