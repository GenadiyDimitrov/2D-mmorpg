using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>
/// Short LETTER labels for skill-bar and buff-bar squares, guaranteed UNIQUE across every skill AND
/// every consumable in the game.
///
/// They used to be derived per-item, in isolation, by taking word initials — which is fine until two
/// names share their initials, and then the bar lies to you. "Heal over Time (Wounds)" and "Heal over
/// Time (Mana)" both rendered <c>HOT</c>; "Ultimate Scroll of Return" and "Ultimate Scroll of
/// Resurrection" both rendered <c>USO</c>. Two different squares, one label, no way to tell them apart
/// in the middle of a fight.
///
/// The fix is that uniqueness is a property of the WHOLE SET, so it cannot be decided one name at a
/// time. This class builds every label once, at startup, sees the collisions, and lengthens only the
/// entries that need it — then asserts the result really is collision-free
/// (<see cref="Validate"/> is called from the same startup check that guards skill-id collisions).
///
/// Labels are only a FALLBACK: a skill with an emoji icon shows the icon instead. They still have to
/// be distinct, because plenty of skills have no icon yet.
/// </summary>
public static class Abbreviations
{
    /// <summary>Words that carry no identity and only dilute an initialism ("Scroll OF Return").</summary>
    private static readonly HashSet<string> Noise = new(StringComparer.OrdinalIgnoreCase)
    {
        "of", "the", "a", "an", "and", "to",
    };

    /// <summary>Hand-authored labels that beat anything derived. Reserved for names whose automatic
    /// label would be technically unique but useless to read — the owner named these two directly.</summary>
    private static readonly Dictionary<string, string> Overrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ultimate Scroll of Return"]       = "URet",
        ["Ultimate Scroll of Resurrection"] = "URes",
        ["Scroll of Return"]                = "Ret",
        ["Scroll of Resurrection"]          = "Res",
    };

    private static Dictionary<string, string>? _byName;

    /// <summary>The label for a display name. Falls back to a derived label for anything not in the
    /// catalog (a runtime-only buff, say), which is why this never throws.</summary>
    public static string For(string displayName)
    {
        _byName ??= Build();
        return _byName.TryGetValue(displayName, out var abbrev) ? abbrev : Derive(displayName, 3);
    }

    /// <summary>Every name the catalog covers, with its label. Startup validation reads this.</summary>
    public static IReadOnlyDictionary<string, string> All => _byName ??= Build();

    /// <summary>Throw if two names ended up sharing a label. Called at startup beside the skill-id
    /// collision guard, so a new skill or potion that clashes fails the server immediately rather than
    /// quietly drawing an ambiguous square months later.</summary>
    public static void Validate()
    {
        var duplicates = All
            .GroupBy(kv => kv.Value, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} <- {string.Join(", ", g.Select(kv => kv.Key))}")
            .ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                "Duplicate skill/item abbreviations: " + string.Join(" | ", duplicates));
    }

    /// <summary>Every name that needs a label: all skills, plus consumables and scrolls (the only items
    /// that can sit on the skill bar).</summary>
    private static IEnumerable<string> Names()
    {
        foreach (var skill in SkillCatalog.AllSkills)
            yield return skill.Name;
        foreach (var item in ItemCatalog.AllItems)
            if (item.Slot is EquipSlot.Consumable or EquipSlot.Scroll)
                yield return item.Name;
    }

    private static Dictionary<string, string> Build()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var taken = new HashSet<string>(StringComparer.Ordinal);

        // Overrides are claimed FIRST so a derived label can never steal one of them.
        var names = Names().Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToList();
        foreach (var name in names)
        {
            if (!Overrides.TryGetValue(name, out var forced)) continue;
            result[name] = forced;
            taken.Add(forced);
        }

        foreach (var name in names)
        {
            if (result.ContainsKey(name)) continue;

            // Try progressively longer labels until one is free. Growing the label is what separates
            // "…Return" from "…Resurrection": the initials are identical, the letters are not.
            string? chosen = null;
            for (int width = 3; width <= 6 && chosen is null; width++)
            {
                var candidate = Derive(name, width);
                if (taken.Add(candidate)) chosen = candidate;
            }
            // Pathological case (identical names, or six letters still not enough): number them, so a
            // duplicate is at worst ugly rather than indistinguishable.
            if (chosen is null)
            {
                string stem = Derive(name, 4);
                for (int n = 2; chosen is null; n++)
                {
                    var candidate = stem + n;
                    if (taken.Add(candidate)) chosen = candidate;
                }
            }
            result[name] = chosen;
        }

        return result;
    }

    /// <summary>Build a label of roughly <paramref name="width"/> characters.
    ///
    /// Multi-word names become initials, then — as the width grows — the LAST word gives up more of its
    /// letters, because that is nearly always where two similar names diverge ("Ultimate Scroll of
    /// RETurn" vs "…RESurrection"). Single words are simply truncated.</summary>
    private static string Derive(string name, int width)
    {
        var words = name
            .Split(new[] { ' ', '-', '\'', '(', ')', ',', ':' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !Noise.Contains(w))
            .ToList();

        if (words.Count == 0) return name.Length <= width ? name : name[..width];

        if (words.Count == 1)
        {
            var single = words[0];
            return single.Length <= width ? Capitalize(single) : Capitalize(single[..width]);
        }

        // One capital per word, then extra lower-case letters from the final word.
        var initials = string.Concat(words.Select(w => char.ToUpperInvariant(w[0])));
        if (initials.Length >= width) return initials[..Math.Min(initials.Length, Math.Max(width, 2))];

        var last = words[^1];
        int extra = Math.Min(width - initials.Length, last.Length - 1);
        return extra <= 0 ? initials : initials + last.Substring(1, extra).ToLowerInvariant();
    }

    private static string Capitalize(string s) =>
        s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
}
