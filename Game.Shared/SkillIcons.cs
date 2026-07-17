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
        ["wind_walk"]        = "🌀",   // Wind Walk (nuker move-speed buff)
        ["return_town"]      = "🏠",
        ["use_scroll_return"]     = "📜",
        ["use_scroll_return_ult"] = "🕊",
        ["use_scroll_resurrect"]     = "📃",
        ["use_scroll_resurrect_ult"] = "⛑",
        ["angels_protection"] = "😇",

        // ---- BUFFS (highest priority) ----
        ["mage_might"]  = "💪",   // Might — atk/def
        ["holy_speed"]  = "💨",   // cleric "Holy Speed"
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
        ["dispel_magic"]    = "🌫",
        ["mana_barrier"]    = "🔮",
        ["phase_shift"]     = "🌌",
        ["weakness"]        = "📉",
        ["greater_weakness"] = "🔻",
        ["restore_spirit"]  = "♻️",

        // ---- HEALER ----
        ["self_heal"]    = "💗",
        ["heal"]         = "💚",
        ["greater_heal"] = "💖",
        ["quick_heal"]   = "💛",
        ["party_heal"]   = "💞",
        ["restore_mana"] = "🔋",
        ["antidote"]     = "🧪",
        ["resurrection"] = "✝️",
    };

    /// <summary>The default glyph for a skill id, or "" if none is mapped.</summary>
    public static string For(string skillId) =>
        skillId is not null && Map.TryGetValue(skillId, out var g) ? g : "";
}
