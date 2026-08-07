using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>
/// Default EMOJI/glyph per skill id, for the skill bar, buff bar and skills window. Kept as ONE table
/// rather than an Icon: on every SkillDef so the whole set is reviewable in one place and easy to keep
/// de-duplicated. Precedence (resolved on the client + in the server's buff push):
///   per-class ClassSkill.Icon  →  SkillDef.Icon  →  this table  →  letters fallback.
///
/// RULE (owner): no two skills a SINGLE class can hold may share an icon; the same glyph on skills of
/// DIFFERENT classes is fine (a warrior's Fear and a nuker's Fear can both be 😱). Priority for coverage:
/// BUFFS first, then mage/healer, then the rest. Emoji only — when the real (Unity) client wants art,
/// this string becomes the sprite key, so nothing here is wasted.
/// </summary>
public static class SkillIcons
{
    private static readonly Dictionary<string, string> Map = new()
    {
        // ---- Universal / common ----
        ["return_town"]      = "🏠",
        ["use_scroll_return"]     = "📜",
        ["use_scroll_return_ult"] = "🕊",
        ["use_scroll_resurrect"]     = "📃",
        ["use_scroll_resurrect_ult"] = "⛑",
        ["angels_protection"] = "😇",

        // ---- BUFFS (highest priority) ----
        // The four SINGLE speed buffs (docs/design/BuffLadders.md): a potion, a scroll and one rung
        // of the improved Speed group all apply these, so the glyph is the EFFECT's, not the source's.
        ["buff_swift_c"] = "🌀", ["buff_swift_u"] = "🌀", ["buff_swift_r"] = "🌀",
        ["buff_alacrity_c"] = "🌠", ["buff_alacrity_u"] = "🌠", ["buff_alacrity_r"] = "🌠",
        ["buff_agility_c"] = "🤸", ["buff_agility_u"] = "🤸", ["buff_agility_r"] = "🤸",
        ["buff_haste_c"] = "⏩", ["buff_haste_u"] = "⏩", ["buff_haste_r"] = "⏩",
        ["buff_dash_c"] = "🏃", ["buff_dash_u"] = "🏃", ["buff_dash_r"] = "🏃",
        ["buff_dash_e"] = "🏃", ["buff_dash_l"] = "🏃", ["buff_dash_m"] = "🏃",
        // Sprint shares the Dash family, so it shares the glyph — same effect, different bottle.
        // The WRAPPER's id is what a one-child buff stamps on the square, so "sprint" is the entry
        // that actually gets read; the two children are here to match how the dash rungs are listed.
        ["sprint"] = "🏃", ["buff_sprint_1"] = "🏃", ["buff_sprint_2"] = "🏃",
        ["mage_might"]  = "💪",   // Might — atk/def
        ["holy_speed"]  = "💨",   // the improved Speed group
        ["holy_body"]   = "🌿",   // HP-regen
        ["holy_force"]  = "🔰",   // interrupt resist / +M.Atk
        ["holy_focus"]  = "🎯",   // crit rate
        ["holy_frenzy"] = "😤",   // berserk
        ["healer_combat_stance"] = "🥊",   // toggle: trade M.Atk for P.Atk

        // ---- MAGE / NUKER ----
        ["magic_bolt"]      = "🔷",
        ["vampiric_bolt"]   = "🩸",
        ["elemental_bolt"]  = "🌟",
        ["quick_bolt"]      = "⚡",
        ["flame_bolt"]      = "🔥",
        ["holy_strike"]     = "☀️",   // "Holy Bolt"
        ["glacial_spike"]   = "🧊",
        ["elemental_burst"] = "💥",
        ["frost_bind"]      = "❄️",
        ["entangling_roots"] = "🪢",
        ["creeping_frost"]  = "🌨",
        ["mana_barrier"]    = "🔮",
        ["phase_shift"]     = "🌌",
        ["weakness"]        = "📉",
        ["greater_weakness"] = "🔻",
        ["restore_spirit"]  = "♻️",

        // ---- HEALER ----
        ["self_heal"]    = "💗",
        ["heal"]         = "💚",
        ["quick_heal"]   = "💛",
        ["party_heal"]   = "💞",
        ["restore_mana"] = "🔋",
        ["antidote"]     = "🧪",
        ["resurrection"] = "✝️",
    };

    /// <summary>Glyph per buff FAMILY, for the ladders authored in Skills.BuffLadders.cs. Those ids
    /// are `buff_{family}_{rung}` and there are six rungs of some of them, so one entry per family
    /// beats sixty per-id entries — and it enforces the rule that matters here anyway: every rung of
    /// a family is the SAME buff at a different strength, so it must show the SAME glyph whether it
    /// came from a potion, a scroll or a cleric.</summary>
    private static readonly Dictionary<string, string> FamilyMap = new()
    {
        ["atk_phys"] = "💪", ["def_phys"] = "🛡", ["atk_mag"] = "🔮", ["def_mag"] = "🌐",
        ["vamp"] = "🩸", ["accuracy"] = "🏹", ["interrupt"] = "🔰",
        ["hp_max"] = "❤", ["mp_max"] = "💙", ["hp_regen"] = "🌿", ["mp_regen"] = "🫧",
        ["crit_rate"] = "🎲", ["crit_dmg"] = "🗡", ["mcrit_rate"] = "✨", ["frenzy"] = "😤",
        // The speed four: their RUNGS are listed by id in Map above (they shipped first, with
        // named rather than numbered ids), but their castable singles resolve through here.
        ["spd_move"] = "🌀", ["spd_cast"] = "🌠", ["spd_eva"] = "🤸", ["spd_as"] = "⏩",
    };

    /// <summary>The default glyph for a skill id, or "" if none is mapped.</summary>
    public static string For(string skillId)
    {
        if (skillId is null) return "";
        if (Map.TryGetValue(skillId, out var g)) return g;
        // `cast_{family}` — the castable single a buffer class learns. Same effect, same glyph.
        if (skillId.StartsWith("cast_", StringComparison.Ordinal)
            && FamilyMap.TryGetValue(skillId[5..], out var cast))
            return cast;
        // `buff_{family}_{rung}` — strip the prefix and the trailing rung number.
        if (skillId.StartsWith("buff_", StringComparison.Ordinal)
            && skillId.LastIndexOf('_') is var cut && cut > 4
            && FamilyMap.TryGetValue(skillId[5..cut], out var fam))
            return fam;
        return "";
    }

    /// <summary>The glyph for a skill's DISPLAY NAME, or "" if none is mapped. Buffs and debuffs travel
    /// to the client as names, not ids (they aren't SkillDefs), so the party roster and buff bar need
    /// this reverse lookup to show an icon instead of spelling the whole thing out.</summary>
    public static string ForName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return "";
        _byName ??= SkillCatalog.AllSkills
            .Where(s => For(s.Id) != "")
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => For(g.First().Id), StringComparer.OrdinalIgnoreCase);
        return _byName.TryGetValue(displayName, out var g) ? g : "";
    }

    private static Dictionary<string, string>? _byName;
}
