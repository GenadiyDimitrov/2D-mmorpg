namespace Game.Shared;

public enum EnchantResult { Success = 0, Broke = 1, Reset = 2, Downgraded = 3, Failed = 4 }

/// <summary>The GRADE BAND an enchant scroll serves — the item grades a scroll may be spent on.
/// This is the real ladder (<see cref="ItemDef.ItemLevel"/>), NOT the <see cref="ItemGrade"/> enum,
/// which has no C/D and exists for pricing only. It matches <see cref="AttrTier"/> at D and above and
/// simply extends one step further down, to E.
///
/// <b>There is no F scroll</b> (owner, playtest-17 D1: the ladder is Common→E … Mythic→S). F is the
/// training tier you leave behind by level 20, so F gear is not scroll-enchantable at all — which is
/// exactly why he also asked for the unrestricted admin path (`/enchant 999999` on an F weapon).</summary>
public enum EnchantGrade { None = 0, E = 1, D = 2, C = 3, B = 4, A = 5, S = 6 }

/// <summary>
/// Enchanting rules (owner's spec, playtest-17 D1 — the 0.49.0 rework).
///
/// A scroll now has TWO independent axes, and the item ladder finally means something on both:
///   • <see cref="ScrollKind"/> — what a FAILURE costs (Normal breaks / Greater −1 / Safe keeps).
///   • <see cref="EnchantGrade"/> — WHICH grade of gear it works on, signalled by the scroll's
///     RARITY (Common→E, Uncommon→D, Rare→C, Epic→B, Legendary→A, Mythic→S).
///
/// Before this the three scrolls were the failure behaviours alone and ANY scroll worked on ANY
/// item, so a Common scroll found at level 10 was a legitimate tool against endgame gear. Now the
/// band gates it, one grade below the attribute-scroll bands.
///
/// Lives in Shared so the client can show the odds and the cost of failure before committing.
/// </summary>
public static class EnchantRules
{
    public const int MaxEnchant = 16;

    /// <summary>Chance the NEXT enchant (current -> current+1) succeeds. Owner's table,
    /// playtest-20 `49a`: <b>+1..+3 = 100% (safe), +4..+9 = 66%, +10..+15 = 33%, +16 = 5%.</b>
    ///
    /// It was four bands (100/66/40/20) split at 3/6/9. His is four bands split at 3/9/15, which
    /// makes the ladder read as the four things a player actually experiences: a free run to +3, a
    /// long two-thirds stretch, a grinding third, and a single 1-in-20 wall at the top.
    ///
    /// The budget this is authored to hit, with SAFE scrolls (a failure costs the scroll, never the
    /// item), is his own: 3/1.00 + 6/0.66 + 6/0.33 + 1/0.05 = 3 + 9.1 + 18.2 + 20 =
    /// <b>~50 scrolls for +0 -> +16</b>. With Greater scrolls a failure also costs a level, so the
    /// same climb runs to the high hundreds (~823 by his estimate) — that gap IS the price of the
    /// safe scroll, and it is why the bands must not be tuned without re-checking both numbers.</summary>
    public static float SuccessChance(int currentLevel) => currentLevel switch
    {
        < 3 => 1.00f,            // going to +1, +2, +3 — safe
        < 9 => 0.66f,            // going to +4 .. +9
        < 15 => 0.33f,           // going to +10 .. +15
        < MaxEnchant => 0.05f,   // going to +16 — the wall
        _ => 0f                  // already maxed
    };

    /// <summary>Each enchant level adds this fraction of the item's base
    /// bonus, so a +10 weapon is meaningfully stronger. Flat per-level on top
    /// keeps low-bonus items relevant.</summary>
    public static int BonusAt(int baseBonus, int enchantLevel)
    {
        if (enchantLevel <= 0 || baseBonus <= 0)
            return baseBonus;
        // +20% of base per level, plus 1 flat per level.
        return baseBonus + (int)(baseBonus * 0.20f * enchantLevel) + enchantLevel;
    }

    // ===================================================================================
    //  THE GRADE BAND. One scroll, one grade — the same shape as an attribute scroll, so the
    //  client can filter its target list from the identical code the server validates with.
    // ===================================================================================

    /// <summary>The band an item level sits in. Below 20 (F, the training tier) there is no band
    /// and no scroll can touch it. Thresholds are the ladder's own: E 20, D 40, C 52, B 61, A 76,
    /// S 80 — the same numbers <see cref="GradePenalty.GradeLevels"/> and
    /// <see cref="AttributeSystem.TierOf"/> already use.</summary>
    public static EnchantGrade GradeOf(int itemLevel) =>
        itemLevel >= 80 ? EnchantGrade.S :
        itemLevel >= 76 ? EnchantGrade.A :
        itemLevel >= 61 ? EnchantGrade.B :
        itemLevel >= 52 ? EnchantGrade.C :
        itemLevel >= 40 ? EnchantGrade.D :
        itemLevel >= 20 ? EnchantGrade.E : EnchantGrade.None;

    /// <summary>The band an ITEM sits in, taking the def (so legacy items with no ItemLevel fall
    /// back through <see cref="GradePenalty.ItemGradeLevel"/> exactly as everywhere else).</summary>
    public static EnchantGrade GradeOf(ItemDef def) => GradeOf(GradePenalty.ItemGradeLevel(def));

    /// <summary>Display letter for a band, for tooltips and refusal messages.</summary>
    public static string GradeName(EnchantGrade g) => g switch
    {
        EnchantGrade.E => "E", EnchantGrade.D => "D", EnchantGrade.C => "C",
        EnchantGrade.B => "B", EnchantGrade.A => "A", EnchantGrade.S => "S",
        _ => "F"
    };

    /// <summary>May this scroll be spent on this item? A scroll serves EXACTLY its own band —
    /// there is no "or better", which is what stops a cheap scroll reaching endgame gear.</summary>
    public static bool Accepts(ItemDef scrollDef, ItemDef targetDef) =>
        scrollDef.ScrollGrade != EnchantGrade.None
        && scrollDef.ScrollGrade == GradeOf(targetDef);

    /// <summary>What a failure costs, in the player's words. Shared by the client's confirmation
    /// popup and the server's refusal/outcome messages so the two can never disagree.</summary>
    public static string FailureText(ScrollKind kind) => kind switch
    {
        ScrollKind.Normal => "the item is DESTROYED",
        ScrollKind.Greater => "the enchant drops by 1",
        ScrollKind.Safe => "nothing — the enchant is kept",
        _ => "nothing"
    };

    /// <summary>Resolve one enchant attempt. Returns the outcome and the new
    /// enchant level. Caller consumes the scroll regardless of outcome — including a Safe
    /// failure, which is the whole price of the safety.</summary>
    public static (EnchantResult Result, int NewLevel) Attempt(
        int currentLevel, ScrollKind kind, Random rng)
    {
        if (currentLevel >= MaxEnchant)
            return (EnchantResult.Failed, currentLevel);

        if (rng.NextDouble() < SuccessChance(currentLevel))
            return (EnchantResult.Success, currentLevel + 1);

        // Failure path depends on the scroll TYPE. Note that "reset to +0"
        // (EnchantResult.Reset) is no longer reachable — the owner's three-type ladder has no
        // scroll that does it. The enum value stays so old outcome-handling code still compiles.
        return kind switch
        {
            ScrollKind.Normal => (EnchantResult.Broke, currentLevel),        // item destroyed
            ScrollKind.Greater => (EnchantResult.Downgraded, Math.Max(0, currentLevel - 1)),
            ScrollKind.Safe => (EnchantResult.Failed, currentLevel),         // keeps its level
            _ => (EnchantResult.Failed, currentLevel)
        };
    }
}
