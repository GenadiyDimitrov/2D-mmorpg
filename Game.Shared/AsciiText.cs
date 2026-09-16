namespace Game.Shared;

using System.Text;

/// <summary>Folds the TYPOGRAPHIC characters our prose is written with down to plain ASCII, for text
/// that is about to be drawn by the Unity client.
///
/// <para>🔴🔑 WHY THIS EXISTS (§100, 2026-09-16). His report: *"the unicode character with value []
/// cannot be found in [liberation sans srf] any asset or any potential Fallback. It was replaced with
/// unicode character [] in text object [lable]"*, with the FPS drop that comes with it — and only in
/// the System tab. **The TMP font atlas is STATIC**: a character outside it is logged once per FRAME
/// per label, so one em dash in a system message is a permanent frame cost for as long as the line is
/// on screen. The System tab is where it showed because that is where server prose streams, and our
/// server prose is full of em dashes: 394 of them in string literals, plus 102 U+2212 minus signs, 77
/// multiplication signs and 30 ellipses.</para>
///
/// <para>🔑 IT IS A FOLD, NOT A STRIP, AND THAT IS THE WHOLE DESIGN. Anything it does not recognise is
/// passed through untouched — <b>the owner types Bulgarian</b>, and a function that deleted "unknown"
/// characters would eat every word he says. Only characters with an unambiguous ASCII spelling are
/// mapped. Emoji are deliberately left alone: they are skill/buff ICONS (`SkillDef.Icon`), authored on
/// purpose, and folding one would silently blank an icon.</para>
///
/// <para>⚠ Applied to SERVER-AUTHORED text only — the three ChatMessage sites the server writes
/// itself. Player chat is never folded, for the Bulgarian reason above. The same characters still
/// appear in skill and item DESCRIPTIONS, which the client builds locally out of `Game.Shared`; those
/// are not covered here and are recorded in the checklist.</para></summary>
public static class AsciiText
{
    /// <summary>Fold one character, or 0 for "leave it alone". A multi-character spelling is handled
    /// by <see cref="Fold"/> separately — this table is only the 1:1 cases.</summary>
    private static char One(char c) => c switch
    {
        '—' => '-',   // — em dash            (394 uses, by far the commonest)
        '–' => '-',   // – en dash
        '−' => '-',   // − minus sign         (102) — NOT the ASCII hyphen, and it reads identically
        '×' => 'x',   // × multiplication     (77)
        '·' => '-',   // · middot, our list separator (23)
        '•' => '*',   // • bullet
        '‘' or '’' => '\'',   // ' ' curly single quotes
        '“' or '”' => '"',    // " " curly double quotes
        ' ' => ' ',   // non-breaking space
        '′' => '\'',  // ′ prime
        _ => '\0',
    };

    /// <summary>The characters whose ASCII spelling is longer than one character.</summary>
    private static string? Many(char c) => c switch
    {
        '…' => "...",   // … ellipsis (30)
        '±' => "+/-",   // ± plus-minus
        '→' => "->",    // → arrow
        '←' => "<-",    // ←
        '≤' => "<=",    // ≤
        '≥' => ">=",    // ≥
        '≈' => "~",     // ≈
        '½' => "1/2",   // ½
        _ => null,
    };

    /// <summary>Fold the typographic characters in <paramref name="text"/> to ASCII. Everything this
    /// does not know — Cyrillic, emoji, accented letters — is returned exactly as it arrived.
    /// <para>Allocation-free for the common case: a string with nothing to fold is returned as-is,
    /// and this runs on every system line the server sends.</para></summary>
    public static string Fold(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        // One pass to decide whether to build anything at all. Most lines are already plain ASCII.
        bool needs = false;
        foreach (char c in text!)
            if (c > 127 && (One(c) != '\0' || Many(c) != null)) { needs = true; break; }
        if (!needs) return text;

        var sb = new StringBuilder(text.Length + 8);
        foreach (char c in text)
        {
            if (c <= 127) { sb.Append(c); continue; }
            char one = One(c);
            if (one != '\0') { sb.Append(one); continue; }
            string? many = Many(c);
            if (many != null) { sb.Append(many); continue; }
            sb.Append(c);   // unknown — the owner's Bulgarian, an emoji icon: pass it through
        }
        return sb.ToString();
    }
}
