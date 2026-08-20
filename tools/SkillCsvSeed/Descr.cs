using System.Globalization;
using System.Text.RegularExpressions;
using Game.Shared;

// =====================================================================================================
//  THE DESCR COLUMN — read, not skipped. His ask, 2026-08-20: *"make it so a DESCR is read .. as it
//  contains the values for the skills."*
//
//  🔑 WHY THIS MATTERS MORE THAN THE REST OF --check: every other column is a number in its own cell, so
//  a mismatch is trivially comparable and the tool has caught them for days. The VALUES that decide how
//  a skill FEELS — power, +M.Atk, mpReg x1.2, -15% reuse — live in free text, and were the one thing
//  nothing verified. That is exactly how `Spell Mastery` rung 5 sat on 10% reuse for two days while both
//  authored rows said 15%, and how the bolt powers drifted 25-30% over his sheet.
//
//  🔑 HOW IT WORKS, and what it deliberately does NOT do:
//    1. SEGMENT the text on `;` and on scope labels (`Robe:`, `Light:`, `with sword/blunt`, `with all`).
//       A scope picks WHICH source the values must come from — the robe row of an armor mastery, the
//       blunt slot of a weapon mastery — so "Robe pDef +20, Light pDef +25" cannot cross-match.
//    2. TOKENISE each segment into (metric, flat-or-percent, value) with a table of aliases, because he
//       writes the same stat six ways: `p.def` / `pDef` / `P.Def` / `physical defence`.
//    3. RESOLVE each metric against the code — PassiveEffect, StatMods, WeaponMasteryProfile, the skill's
//       power, or its EffectMagnitudes — and compare.
//    4. REPORT what it could NOT read. Every number in the text must be consumed by a token or matched
//       by an ignore rule (`20 min`, `rank 3`, `300 range`). Anything left over prints as UNREAD, so the
//       COVERAGE IS VISIBLE instead of silently zero. A parser that quietly skips what it doesn't
//       understand is worse than no parser: it reports "no discrepancies" over an unchecked file.
//
//  ⚠ It never edits a CSV and never guesses. An unknown metric is UNCHECKED, not a defect — the CSV is
//  the authority (see the header of Check.cs), so the tool's job is to say what it verified and what it
//  could not, and let him decide.
// =====================================================================================================

internal static class Descr
{
    /// <summary>One value read out of the DESCR text.</summary>
    /// <param name="Metric">Canonical stat key — see <see cref="Aliases"/>.</param>
    /// <param name="Pct">true = a fraction (0.15 = +15% / x1.15), false = a flat addend.</param>
    internal sealed record Token(string Metric, bool Pct, float Value, string Raw);

    /// <summary>What a segment's values must be read from. null = anything the skill has.</summary>
    internal sealed record Scope(ArmorWeight? Weight, WeaponType? Weapon, string Label);

    // ---- the vocabulary --------------------------------------------------------------------------
    //  LONGEST FIRST inside each metric: `magic def` must win over `def`, `cast speed` over `speed`,
    //  `crit dmg` over `dmg`. The matcher walks this list in order and takes the first hit.

    private static readonly (string Metric, string[] Words)[] Aliases =
    {
        // POWER IS FIRST ON PURPOSE. Ties in distance fall back to this order, and his transfer/heal
        // lines put a stat word the same distance away on the other side ("Transfers 60 MP to an ally" —
        // `transfers` one char left, `mp` one char right). The number there is the skill's POWER.
        ("power",         new[] { "power", "transfers", "heal for", "heals for", "restores" }),
        ("mdef",          new[] { "magic defence", "magic defense", "magic def", "m.def", "mdef" }),
        ("matk",          new[] { "magic attack", "m.atk", "matk", "mattack" }),
        ("patk",          new[] { "physical attack", "p.atk", "patk", "pattack", "p.attack" }),
        ("pdef",          new[] { "physical defence", "physical defense", "p.def", "pdef", "p. def" }),
        ("maxhp",         new[] { "maxhp", "max hp" }),
        ("maxmp",         new[] { "maxmp", "max mp" }),
        // ⚠ Bare `mp` / `hp` LAST in each list and AFTER maxmp/maxhp above: his warrior row writes the
        // regen multiplier as just "mp x1.1". A tie in distance keeps the earlier metric, so "maxMP +20"
        // still reads as maxmp and only a naked "mp" falls through to regen.
        ("mpreg",         new[] { "mp regeneration", "mp regen", "mpreg", "mp reg", "mp" }),
        ("hpreg",         new[] { "hp regeneration", "hp regen", "hpreg", "hp reg" }),
        ("cast",          new[] { "cast speed", "casting speed", "cast" }),
        ("as",            new[] { "attack speed", "atack speed", "atk speed", "as" }),
        ("ms",            new[] { "move speed", "movement speed", "ms", "speed", "move" }),
        ("reuse",         new[] { "reuse delay", "reuse", "cooldown" }),
        // ⚠ "chance for spells to fizzle" IS mRes in his mage file. The number is the same one the code
        // carries as MagicResist; only the WORDS are stale — mRes was built as a fizzle chance until
        // 2026-08-10, when it became a damage reduction and the row was never rewritten. Matching his
        // wording is the point of an alias table (and the row is his to reword, not the tool's).
        ("mres",          new[] { "mres", "magic resist", "magic resistance",
                                  "chance for spells to fizzle", "spells to fizzle" }),
        ("critdmg",       new[] { "critical damage", "crit damage", "crit dmg", "critdmg" }),
        ("critrate",      new[] { "critical rate", "crit rate", "critrate", "critical" }),
        ("magiccritrate", new[] { "magic critical", "magic crit" }),
        // ⚠ THREE evasion channels, and his rogue row names all three in one cell: plain "evasion +20",
        // "skill evasion x1.25" (dodging a physical SKILL outright) and "magic evasion x1.1" (points on
        // the caster's fail roll). Read as one metric they overwrite each other; the specific spellings
        // must come first so they out-reach the bare word.
        ("skilleva",      new[] { "skill evasion" }),
        ("magiceva",      new[] { "magic evasion" }),
        ("eva",           new[] { "evasion", "eva" }),
        ("acc",           new[] { "accuracy", "acc" }),
        ("interrupt",     new[] { "interrupt resistance", "interrupt" }),
        ("vamp",          new[] { "vampirism", "vamp" }),
        ("restoremp",     new[] { "mpwhenrestored", "mp when restored" }),
        // ⚠ "shield defence RATE" is the block CHANCE and "shield defence" is the shield's P.Def — two
        // different stats one word apart, so the rate spellings must be able to out-reach the other.
        ("blockrate",     new[] { "shield defence rate", "shield defense rate", "shield rate",
                                  "block rate", "block chance" }),
        // "shiled" is his spelling in `tank 2nd.csv` and the CSV is the authority, so the reader learns
        // it rather than the file being corrected to suit the reader.
        ("shielddef",     new[] { "shield.p.def", "shiled defence", "shield defence", "shield def" }),
        ("bowrange",      new[] { "range" }),
        ("bowresist",     new[] { "bow resistance", "bow resist", "arrow defence" }),
        ("ccresist",      new[] { "cc resist", "ccresist" }),
        ("critrateres",   new[] { "crit rate resist", "critical rate resist" }),
        ("critdmgres",    new[] { "crit dmg reduction", "crit dmg resist", "crit damage reduction",
                                  "critical damage reduction" }),
        ("ccresist",      new[] { "resist to spt", "resist to con" }),
        ("cancelresist",  new[] { "cancel resist", "buff cancel resist" }),
        ("aggro",         new[] { "aggro", "threat" }),
        ("resexp",        new[] { "of lost exp", "lost exp" }),
        ("lifesteal",     new[] { "of the damage dealt", "of damage dealt", "heals you" }),
        // ONE number, FOUR stats — his Frenzy shorthand "+5% offence and speed" means P.Atk, M.Atk,
        // attack speed and cast speed all move together. See the pool builder: the key only exists when
        // the code really does carry one value for all four, so a rung that split them reads as
        // UNCHECKED rather than passing on whichever channel happened to match.
        ("offencespeed",  new[] { "offence and speed", "offense and speed" }),
    };

    /// <summary>Numbers that are NOT stats — durations, ranks, ranges, counts. Matched against the text
    /// around a leftover number so it can be dismissed instead of printed as unread.</summary>
    private static readonly Regex[] NotAStat =
    {
        new(@"\d+\s*(min|minutes|sec|secs|seconds|s\b)", RegexOptions.IgnoreCase),
        new(@"rank\s*\d+",                               RegexOptions.IgnoreCase),
        new(@"\d+\s*range",                              RegexOptions.IgnoreCase),
        new(@"\(\s*\d+\s*range\s*\)",                    RegexOptions.IgnoreCase),
        new(@"\bl\d\b",                                  RegexOptions.IgnoreCase),   // "in l2 is ..."
        new(@"lvl\s*\d+",                                RegexOptions.IgnoreCase),
        new(@"\d+\s*targets?",                           RegexOptions.IgnoreCase),
        new(@"\d+\s*(hp|mp)\s*/\s*s",                    RegexOptions.IgnoreCase),
        new(@"\d+h\s",                                   RegexOptions.IgnoreCase),   // "requires 2h sword"
        new(@"below\s*\d+\s*%",                          RegexOptions.IgnoreCase),   // a CONDITION, not a stat
        new(@"revive at\s*\d+\s*%",                      RegexOptions.IgnoreCase),
        new(@"\d+\s*%\s*(hp|mp)\s*/",                    RegexOptions.IgnoreCase),
        // "power 314 - only when skill does critical - otherwise 31": the second number is the
        // non-crit damage, a RESTATEMENT of power/10 and not a field of its own.
        new(@"otherwise\s*\d+(\.\d+)?",                  RegexOptions.IgnoreCase),
    };

    // ---- segmentation ----------------------------------------------------------------------------

    private static readonly (string Word, ArmorWeight Weight)[] WeightWords =
    {
        ("robe", ArmorWeight.Robe), ("light", ArmorWeight.Light),
        ("heavy", ArmorWeight.Heavy), ("none", ArmorWeight.None),
    };

    private static readonly (string Word, WeaponType Weapon)[] WeaponWords =
    {
        ("sword/blunt", WeaponType.Blunt), ("blunt/sword", WeaponType.Blunt),
        ("duals", WeaponType.Dual), ("dual", WeaponType.Dual),
        ("bow", WeaponType.Bow), ("blunt", WeaponType.Blunt), ("sword", WeaponType.Sword),
    };

    /// <summary>Split the text into scoped segments. `;` separates weight rows in his armor masteries
    /// ("Robe: … ; Light: …"); a `with X` clause opens a weapon or weight scope that runs to the end of
    /// its segment. Everything unlabelled is scope-free and may match any source the skill has.</summary>
    /// <summary>A PARENTHETICAL IS COMMENTARY, never data — and reading it as data is a false alarm
    /// machine. The cleric's light row explains itself: "cast x1.9, as x2 (composes to cast x0.95 / as x1
    /// after Spellcaster Mastery)". Both halves are true, but only the first is the authored value; the
    /// second is the RESULT after another skill. Read literally it reported four defects on a row that is
    /// correct. Same for "(in l2 is strikes the target with 35 power added to p.atk)".</summary>
    private static readonly Regex Parenthetical = new(@"\([^)]*\)");

    private static List<(Scope? Scope, string Text)> Segments(string descr)
    {
        descr = Parenthetical.Replace(descr, " ");
        var outp = new List<(Scope?, string)>();
        foreach (var chunk in descr.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string text = chunk.Trim();
            if (text.Length == 0) continue;
            string low = text.ToLowerInvariant();
            Scope? scope = null;

            // "Robe: …" / "Light: …" — a weight label followed by a colon.
            int colon = low.IndexOf(':');
            if (colon > 0 && colon <= 8)
                foreach (var (word, weight) in WeightWords)
                    if (low[..colon].Trim() == word)
                    { scope = new Scope(weight, null, word); text = text[(colon + 1)..]; break; }

            // "with sword/blunt …" / "with light …" / "with all …" / "with any weapon …"
            if (scope is null && low.Contains("with "))
            {
                if (low.Contains("with all") || low.Contains("with any"))
                    scope = null;                       // applies everywhere — leave it unscoped
                else
                {
                    foreach (var (word, weight) in WeightWords)
                        if (low.Contains("with " + word)) { scope = new Scope(weight, null, word); break; }
                    if (scope is null)
                        foreach (var (word, weapon) in WeaponWords)
                            if (low.Contains("with " + word) || low.Contains("with a " + word))
                            { scope = new Scope(null, weapon, word); break; }
                }
            }
            outp.Add((scope, text));
        }
        return outp;
    }

    // ---- tokenising ------------------------------------------------------------------------------

    // A NUMBER with whatever decorates it: an optional leading sign, an optional trailing %.
    // ⚠ THE SIGN CLASS CARRIES THREE MINUSES. He writes "−5% Max HP/MP" with U+2212 MINUS SIGN (and
    // sometimes an en dash) because that is what a spreadsheet and a copy-paste produce — read as ASCII
    // only, the sign silently vanishes and a −5% penalty is compared as +5%. The tool reported exactly
    // that against Frenzy on the day this was written.
    private static readonly Regex Number = new(@"(?<mult>x\s*)?(?<sign>[+\-−–])?(?<num>\d+(\.\d+)?)(?<pct>\s*%)?",
                                               RegexOptions.IgnoreCase);

    /// <summary>Read one segment into tokens. A number is bound to the metric word NEAREST it — the one
    /// immediately before ("mAtk +23") or immediately after ("+15% P.Atk", "power 400"). Numbers with no
    /// metric within reach, and no ignore rule, come back in <paramref name="unread"/>.</summary>
    /// <summary>A metric further away than this is not describing this number. Without the cap, "with 2h
    /// sword/blunt: acc +3" bound the `2` of "2h" to `acc` fifteen characters later and reported the
    /// warrior's accuracy as wrong on all five rungs.</summary>
    private const int MaxMetricDistance = 18;

    private static List<Token> Tokens(string text, List<string> unread)
    {
        // ⚠ THE IGNORE RULES RUN FIRST, and they run on POSITIONS, not on a text window. Both matter:
        // filtering after the metric search let "2h" become a stat, and matching a rule anywhere near the
        // number would let one "rank 3" silence a real value beside it.
        var ignore = NotAStat.SelectMany(r => r.Matches(text).Cast<Match>())
                             .Select(m => (Start: m.Index, End: m.Index + m.Length)).ToList();

        var found = new List<Token>();
        var used = new HashSet<int>();   // absolute index of each metric word already bound to a number
        foreach (Match m in Number.Matches(text))
        {
            string raw = m.Value.Trim();
            if (!float.TryParse(m.Groups["num"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out float n))
                continue;
            if (ignore.Any(s => m.Index >= s.Start && m.Index < s.End)) continue;

            // Look ~24 chars either side, CLIPPED AT THE NEAREST SEPARATOR. A metric on the far side of a
            // comma or a dash belongs to the next clause, not to this number: "…reuse delay with 10%,
            // mAtk +6" put `mAtk` two characters after the 10% and read the reuse reduction as M.Atk.
            // ⚠ The WINDOW is wide (40) while MaxMetricDistance is narrow (18). They are different jobs:
            // the window must be long enough to CONTAIN a multi-word alias ("chance for spells to
            // fizzle" is 27 chars), the cap decides whether what it found is close enough to count. A
            // 24-char window silently truncated the long aliases and they could never match.
            string before = Clip(text[Math.Max(0, m.Index - 40)..m.Index], fromEnd: true);
            int after0 = m.Index + m.Length;
            string after = Clip(text[after0..Math.Min(text.Length, after0 + 40)], fromEnd: false);

            // Absolute positions, so a metric WORD can be marked used — see below.
            int beforeStart = m.Index - before.Length;
            var (metricB, distB, atB) = Nearest(before, fromEnd: true, offset: beforeStart);
            var (metricA, distA, atA) = Nearest(after, fromEnd: false, offset: after0);

            // 🔑 TWO RULES, and both were learned from a false positive.
            //  (1) THE METRIC BEFORE THE NUMBER WINS. His files are overwhelmingly "stat value" ("pDef
            //      +20", "power 400", "mpReg x1.2"); "value stat" ("+15% P.Atk") happens only when the
            //      clause STARTS with the number, so there is nothing before it to find. Taking the
            //      merely-CLOSER side instead read "P.Def + 9 mpReg x1.1" as mpReg 9 — the NEXT stat's
            //      name is often nearer than the one the number actually belongs to.
            //  (2) ONE METRIC WORD BINDS ONE NUMBER. "+4 M.Atk and +2 P.Atk" is value-first, so rule (1)
            //      would hand the +2 to the M.Atk behind it; that word is already spoken for. Tracked by
            //      POSITION, not by name, because "p.atk x1.5 p.atk +15" is one clause with two of them.
            string? metric = null; int dist = int.MaxValue; int at = -1;
            if (metricB is not null && !used.Contains(atB)) { metric = metricB; dist = distB; at = atB; }
            else if (metricA is not null && !used.Contains(atA)) { metric = metricA; dist = distA; at = atA; }
            if (dist > MaxMetricDistance) metric = null;
            if (metric is not null) used.Add(at);

            if (metric is null)
            {
                unread.Add(raw + "  (…" +
                           text[Math.Max(0, m.Index - 18)..Math.Min(text.Length, after0 + 18)].Trim() + "…)");
                continue;
            }

            bool mult = m.Groups["mult"].Success;                 // "x1.2"
            bool pct  = m.Groups["pct"].Success || mult;          // "15%" or "x1.15"
            float val = mult ? n - 1f : pct ? n / 100f : n;
            if (m.Groups["sign"].Value is "-" or "−" or "–") val = -val;
            // "Decreses the reuse delay with 10%" is a REDUCTION authored as a positive number, and
            // CooldownPct stores reductions positive — so reuse never flips sign. Same for the resists.
            found.Add(new Token(metric, pct, val, raw));
        }
        return found;
    }

    /// <summary>Cut a search window at the clause boundary nearest the number. `-` is in the set because
    /// his prose uses it as one ("drain power 21 - heals you 40% of damage dealt" — the 40% is the drain
    /// fraction, not the power). A number's own sign is never in the window: the regex swallowed it.</summary>
    private static string Clip(string window, bool fromEnd)
    {
        int cut = fromEnd ? window.LastIndexOfAny(Separators) : window.IndexOfAny(Separators);
        if (cut < 0) return window.ToLowerInvariant();
        return (fromEnd ? window[(cut + 1)..] : window[..cut]).ToLowerInvariant();
    }

    /// ⚠ NO `.` IN HERE. It is a clause boundary in English and a letter in his stat names — clipping on
    /// it turns "p.def +20" into "def +20" and the alias stops matching. `,` `;` `:` `-` are enough.
    private static readonly char[] Separators = { ',', ';', ':', '-' };

    /// <summary>The metric word closest to a number, and how far away it is (chars). Returns int.MaxValue
    /// when the window holds none.</summary>
    private static (string? Metric, int Dist, int At) Nearest(string window, bool fromEnd, int offset)
    {
        string? best = null; int bestDist = int.MaxValue, bestAt = -1;
        foreach (var (metric, words) in Aliases)
            foreach (var w in words)
            {
                int i = fromEnd ? window.LastIndexOf(w, StringComparison.Ordinal)
                                : window.IndexOf(w, StringComparison.Ordinal);
                if (i < 0) continue;
                int dist = fromEnd ? window.Length - (i + w.Length) : i;
                if (dist < bestDist) { bestDist = dist; best = metric; bestAt = offset + i; }
            }
        return (best, bestDist, bestAt);
    }

    // ---- the code side ---------------------------------------------------------------------------

    /// <summary>Every (metric, pct) → value this skill actually carries at this rung, within the given
    /// scope. A scope narrows the source; no scope pools everything the skill has, which is what keeps
    /// an unlabelled line like "+8% PAttack" from caring whether it is a buff or a passive.</summary>
    private static Dictionary<(string, bool), List<float>> Pool(SkillDef def, int level, Scope? scope)
    {
        var pool = new Dictionary<(string, bool), List<float>>();
        void Add(string metric, bool pct, float v)
        {
            if (v == 0f) return;
            if (!pool.TryGetValue((metric, pct), out var list)) pool[(metric, pct)] = list = new List<float>();
            if (!list.Contains(v)) list.Add(v);
        }

        // POWER — the single most-authored value in the files ("heal with power 400").
        if (scope is null) Add("power", false, def.PowerAt(level));

        // ARMOR MASTERY: the row for the named weight, or every row when unscoped.
        if (def.ArmorMasteryAt(level) is ArmorMasteryProfile amp)
        {
            if (scope?.Weight is ArmorWeight w) AddStatMods(Add, Row(amp, w));
            else if (scope?.Weapon is null)
                foreach (var weight in new[] { ArmorWeight.Robe, ArmorWeight.Light, ArmorWeight.Heavy, ArmorWeight.None })
                    AddStatMods(Add, Row(amp, weight));
        }

        // WEAPON MASTERY: the slot for the named weapon, or every slot when unscoped.
        // ⚠ Read the SLOTS directly, never through `For()`. For() honours `RequiredWeapon`, and the
        // warrior's Two-Hand Mastery sets it to WeaponType.TwoHanded — so For(Sword) returns an all-zero
        // profile and every one of that skill's authored values came back "the code has no such value".
        // The gate is a runtime question about the equipped weapon; pooling asks what the rung CARRIES.
        if (def.WeaponMasteryAt(level) is WeaponMasteryProfile wmp)
        {
            if (scope?.Weapon is WeaponType wt) AddPassive(Add, Slot(wmp, wt));
            else if (scope?.Weight is null)
                foreach (var t in new[] { WeaponType.Sword, WeaponType.Blunt, WeaponType.Dual, WeaponType.Bow })
                    AddPassive(Add, Slot(wmp, t));
        }

        // Skills whose value is not a stat at all: a taunt's aggro, a resurrect's exp restored, and the
        // two CC-resistance channels — those ride on SkillDef FIELDS because the SkillEffect flag enum
        // has no bits left, so they are invisible to the magnitude walk above.
        if (scope is null)
        {
            Add("aggro", false, def.TauntPowerAt(level));
            Add("lifesteal", true, def.Lifesteal);
            Add("skilleva", true, def.SkillEvadeChance);
            Add("resexp", true, def.ResExpPctAt(level));
            Add("ccresist", true, def.CcResistMagical);
            Add("ccresist", true, def.CcResistPhysical);
            foreach (var childId in def.ChildBuffsAt(level) ?? Array.Empty<string>())
                if (SkillCatalog.Get(childId) is SkillDef c)
                {
                    Add("ccresist", true, c.CcResistMagical);
                    Add("ccresist", true, c.CcResistPhysical);
                }
        }

        // A PLAIN PASSIVE and the skill's buff MAGNITUDES apply whatever the scope.
        if (def.PassiveAt(level) is PassiveEffect pe) AddPassive(Add, pe);
        AddMagnitudes(Add, def, level);

        // 🔑 FOLLOW THE BUFF LADDER. `Might` is `cast_atk_phys`, and a cast-skill's level carries only
        // CHILD IDS (`buff_atk_phys_2`) — the +12% lives on the child def, not on the one his row names.
        // Without this every buff in every file came back "the code has no such value", which is more
        // than half of what the cleric file authors.
        foreach (var childId in def.ChildBuffsAt(level) ?? Array.Empty<string>())
            if (SkillCatalog.Get(childId) is SkillDef child)
                AddMagnitudes(Add, child, 1);

        // "offence and speed" is only a real metric when the four channels agree. Built LAST, from the
        // finished pool, so it sees whatever the passive/magnitude/child walk above put there.
        if (pool.TryGetValue(("patk", true), out var pa) && pa.Count == 1
            && pool.TryGetValue(("matk", true), out var ma) && ma.Contains(pa[0])
            && pool.TryGetValue(("as", true), out var asp) && asp.Contains(pa[0])
            && pool.TryGetValue(("cast", true), out var cs) && cs.Contains(pa[0]))
            pool[("offencespeed", true)] = new List<float> { pa[0] };

        return pool;
    }

    private static void AddMagnitudes(Action<string, bool, float> add, SkillDef def, int level)
    {
        foreach (var mag in def.MagnitudesAt(level) ?? Array.Empty<EffectMagnitude>())
            if (Metric(mag.Effect) is string metric)
                add(metric, mag.Mode == ModifierMode.Percent, mag.Value);
    }

    /// <summary>A weapon-mastery slot, RAW — deliberately not <c>For()</c>; see the note at the call site.</summary>
    private static PassiveEffect Slot(WeaponMasteryProfile p, WeaponType t) => t switch
    {
        WeaponType.Sword => p.Sword, WeaponType.Blunt => p.Blunt,
        WeaponType.Dual  => p.Dual,  WeaponType.Bow   => p.Bow,
        _ => p.Other,
    };

    private static StatMods Row(ArmorMasteryProfile p, ArmorWeight w) => w switch
    {
        ArmorWeight.Robe => p.Robe, ArmorWeight.Light => p.Light,
        ArmorWeight.Heavy => p.Heavy, _ => p.None,
    };

    private static void AddStatMods(Action<string, bool, float> add, StatMods m)
    {
        add("pdef", false, m.PDef);           add("pdef", true, m.PDefPct);
        add("mdef", false, m.MDef);           add("mdef", true, m.MDefPct);
        add("patk", false, m.PAtk);           add("patk", true, m.PAtkPct);
        add("matk", false, m.MAtk);           add("matk", true, m.MAtkPct);
        add("maxhp", false, m.MaxHp);         add("maxhp", true, m.MaxHpPct);
        add("maxmp", false, m.MaxMp);         add("maxmp", true, m.MaxMpPct);
        add("acc", false, m.Accuracy);        add("acc", true, m.AccuracyPct);
        add("eva", false, m.Evasion);         add("eva", true, m.EvasionPct);
        add("critrate", true, m.CritRate);    add("critrate", false, m.CritRateFlat);
        add("critdmg", true, m.CritDamage);   add("critdmg", false, m.CritDamageFlat);
        add("magiccritrate", true, m.MagicCritRate);
        add("as", true, m.AtkSpeedPct);       add("cast", true, m.CastSpeedPct);
        add("ms", false, m.MoveSpeed);        add("ms", true, m.MoveSpeedPct);
        add("hpreg", false, m.HpRegen);       add("hpreg", true, m.HpRegenPct);
        add("mpreg", false, m.MpRegen);       add("mpreg", true, m.MpRegenPct);
        add("interrupt", false, m.InterruptResist);
        add("critdmgres", true, m.CritDmgResist);
        add("critrateres", true, m.CritRateResist);
        add("ccresist", true, m.CcResist);
        add("restoremp", true, m.RestoreMpPct);
        add("vamp", true, m.MeleeVamp);
        add("shielddef", true, m.ShieldDefPct);
    }

    private static void AddPassive(Action<string, bool, float> add, PassiveEffect p)
    {
        // `Attack` feeds BOTH channels (the one power stat), so it is offered as either.
        add("patk", false, p.PhysAtk);        add("patk", false, p.Attack);
        add("matk", false, p.MagAtk);         add("matk", false, p.Attack);
        add("patk", true, p.PhysAtkPct);      add("patk", true, p.AttackPct);
        add("matk", true, p.MagAtkPct);       add("matk", true, p.AttackPct);
        add("pdef", false, p.Defence);        add("pdef", true, p.DefencePct);
        add("mdef", false, p.MagicDefence);   add("mdef", true, p.MagicDefencePct);
        add("maxhp", false, p.MaxHp);         add("maxhp", true, p.MaxHpPct);
        add("maxmp", false, p.MaxMp);         add("maxmp", true, p.MaxMpPct);
        add("acc", false, p.Accuracy);        add("eva", false, p.Evasion);
        add("critrate", true, p.CritRate);    add("critdmg", true, p.CritDamage);
        add("critdmg", false, p.CritDamageFlat);
        add("magiccritrate", true, p.MagicCritRate);
        add("as", true, p.AtkSpeedPct);       add("cast", true, p.CastSpeedPct);
        add("ms", true, p.MoveSpeedPct);      add("reuse", true, p.CooldownPct);
        add("hpreg", false, p.HpRegen);       add("hpreg", true, p.HpRegenPct);
        add("mpreg", false, p.MpRegen);       add("mpreg", true, p.MpRegenPct);
        add("mres", true, p.MagicResist);     add("vamp", true, p.MeleeVamp);
        add("interrupt", false, p.InterruptResist);
        add("interrupt", false, p.InterruptPower);
        add("critdmgres", true, p.CritDmgResist);
        add("critrateres", true, p.CritRateResist);
        add("blockrate", true, p.BlockChancePct);
        add("shielddef", true, p.ShieldDefPct);
        add("bowresist", true, p.BowResist);
        add("bowrange", false, p.BowRange);
        add("cancelresist", true, p.CancelResistPct);
    }

    /// <summary>Buff/debuff flag → metric. Only the ones his files actually name; an unmapped effect
    /// simply contributes nothing, and its number then prints as UNCHECKED rather than as a defect.</summary>
    private static string? Metric(SkillEffect e) => e switch
    {
        SkillEffect.BuffAtk or SkillEffect.BuffPhysAtk or SkillEffect.DebuffAtk => "patk",
        SkillEffect.BuffMagAtk => "matk",
        SkillEffect.BuffDef or SkillEffect.DebuffDef => "pdef",
        SkillEffect.BuffMagicDef => "mdef",
        SkillEffect.BuffCastSpeed or SkillEffect.DebuffCastSpeed => "cast",
        SkillEffect.BuffAtkSpeed or SkillEffect.DebuffAtkSpeed => "as",
        SkillEffect.BuffMoveSpeed => "ms",
        SkillEffect.BuffEvasion => "eva",
        SkillEffect.BuffAccuracy => "acc",
        SkillEffect.BuffCritRate => "critrate",
        SkillEffect.BuffCritDamage => "critdmg",
        SkillEffect.BuffMagicCritRate => "magiccritrate",
        SkillEffect.BuffHpRegen => "hpreg",
        SkillEffect.BuffMpRegen => "mpreg",
        SkillEffect.BuffHp => "maxhp",       // ⚠ `BuffHp` is MAX HP, not a heal — the heal flag is `Heal`
        SkillEffect.BuffMp => "maxmp",
        SkillEffect.BuffMeleeVamp => "vamp",
        SkillEffect.BuffMagicResist => "mres",
        SkillEffect.BuffBlockChance => "blockrate",
        SkillEffect.BuffShieldDef => "shielddef",
        SkillEffect.BuffCritRateResist => "critrateres",
        SkillEffect.BuffCritDmgResist => "critdmgres",
        SkillEffect.BuffCooldown => "reuse",
        SkillEffect.BuffCancelResist => "cancelresist",
        SkillEffect.BuffBowResist => "bowresist",
        SkillEffect.BuffMagicEvasion => "magiceva",
        SkillEffect.BuffInterruptResist => "interrupt",
        SkillEffect.Heal or SkillEffect.RestoreMp => "power",
        _ => null,
    };

    // ---- the comparison --------------------------------------------------------------------------

    /// <summary>DIVERGENCES HE HAS ALREADY RULED ON — (skill name, metric) → why. The reader still reads
    /// them and still shows both numbers, but as ⚪ RULED rather than 🟡, because "the CSV is the
    /// authority" has one exception: a LATER ruling in chat. Without this list the tool cries wolf on
    /// every run, and a tool that always prints a defect stops being read.
    ///
    /// ⚠ Add an entry only for a decision he actually made, with the date, and never to silence a number
    /// nobody has looked at. The whole value of this pass is that an unexplained difference is loud.</summary>
    private static readonly Dictionary<(string Skill, string Metric), string> Ruled = new()
    {
        [("shield mastery", "shielddef")] =
            "2026-08-12 — ShieldDefPct went x5 (0.30/0.40 → 1.50/2.00) as the other half of cutting every "
          + "shield's flat defence 5x in Items.cs. His words: \"40% tanks to become 200%\". The CSV row "
          + "predates the pair and was deliberately not re-authored.",
        [("evasion boost", "magiceva")] =
            "2026-08-11 (`62e`) — magic evasion became FLAT POINTS on the fail roll, his words: \"the "
          + "magic evasion should be magic fail chance like 3-4\". A multiplier is worth almost nothing "
          + "at parity (1% x 1.1) and enormous across a level gap, the opposite of a defensive burst. "
          + "His x1.1 predates that call; the built value is 4 points.",
    };

    /// <summary>Every value this DESCR cell authors, keyed by stat + mode + scope. Used for the LADDER
    /// check: his rule, 2026-08-20 — *"the stats should go up not down - if they got down i made a
    /// mistake or swaped two levels"*. A rung whose number is SMALLER than the rung below it is a typo
    /// signal, and it is the one defect class that lives in the CSV rather than in the code, which is
    /// why the caller reports it separately instead of counting it with the rest.
    ///
    /// <para>⚠ Penalties are stored SIGNED ("MaxHP x0.7" = −0.30), so a shrinking penalty
    /// (−0.30 → −0.26) correctly reads as going UP. That is the behaviour you want and it falls out of
    /// comparing the raw signed value; don't "fix" it with an absolute value.</para></summary>
    public static Dictionary<string, float> Values(string descr)
    {
        var outp = new Dictionary<string, float>();
        if (string.IsNullOrWhiteSpace(descr)) return outp;
        var ignored = new List<string>();
        foreach (var (scope, text) in Segments(descr))
            foreach (var t in Tokens(text, ignored))
                outp[$"{t.Metric}|{(t.Pct ? "%" : "#")}|{scope?.Label ?? ""}"] = t.Value;
        return outp;
    }

    /// <summary>Compare one authored DESCR against one registered rung. Returns the lines to print;
    /// only 🟡 lines count as defects (the caller counts them).</summary>
    public static List<(bool Defect, string Line)> Compare(string descr, SkillDef def, int level)
    {
        var outp = new List<(bool, string)>();
        if (string.IsNullOrWhiteSpace(descr)) return outp;

        var unread = new List<string>();
        foreach (var (scope, text) in Segments(descr))
        {
            var pool = Pool(def, level, scope);
            string where = scope is null ? "" : $" [{scope.Label}]";

            foreach (var t in Tokens(text, unread))
            {
                // 🔑 "m.Atk +21" ON A DAMAGE SPELL IS ITS POWER, not a stat bonus — his shorthand in the
                // cleric and nuker files ("Holy Bolt … m.Atk +21" against `Power: 21`). Only when the
                // skill has no M.Atk of its own, so a passive that really does grant M.Atk still compares
                // against the passive.
                var key = (t.Metric, t.Pct);
                if (!pool.ContainsKey(key) && !t.Pct && (t.Metric == "matk" || t.Metric == "patk")
                    && pool.ContainsKey(("power", false)))
                    key = ("power", false);

                if (!pool.TryGetValue(key, out var values))
                {
                    // The other mode is worth one look before giving up: "+8% PAttack" against a flat
                    // PhysAtk is a real authoring disagreement, not an unreadable line.
                    Ruled.TryGetValue((def.Name.ToLowerInvariant(), t.Metric), out var ruledMode);
                    outp.Add((false, pool.ContainsKey((t.Metric, !t.Pct))
                        ? $"    ⚪ MODE       {t.Metric}{where}: CSV says {t.Raw} but the code carries it as a "
                          + (t.Pct ? "FLAT" : "PERCENT") + " value."
                          + (ruledMode is null ? "" : " — " + ruledMode)
                        : $"    ⚪ UNCHECKED  {t.Metric}{where} {t.Raw} — the code has no such value on this rung."));
                    continue;
                }
                if (values.Any(v => Math.Abs(v - t.Value) < 0.005f)) continue;
                string shown = $"CSV {Show(t.Value, t.Pct)} vs code " +
                               string.Join(" / ", values.Select(v => Show(v, t.Pct)));
                if (Ruled.TryGetValue((def.Name.ToLowerInvariant(), t.Metric), out var why))
                    outp.Add((false, $"    ⚪ RULED      {t.Metric}{where}: {shown} — {why}"));
                else
                    outp.Add((true, $"    🟡 VALUE      {t.Metric}{where}: {shown}"));
            }
        }
        foreach (var u in unread.Distinct())
            outp.Add((false, $"    ⚪ UNREAD     {u}"));
        return outp;
    }

    private static string Show(float v, bool pct) =>
        pct ? (v * 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%"
            : v.ToString("0.##", CultureInfo.InvariantCulture);
}
