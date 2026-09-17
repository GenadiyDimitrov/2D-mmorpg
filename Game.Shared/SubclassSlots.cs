namespace Game.Shared;

/// <summary>
/// THE SUBCLASS SLOT LADDER (`BL-250` §5, owner 2026-09-16, trimmed 2026-09-17).
///
/// <para>A character does not simply "have" subclass slots. <b>Three arrive with your progress and
/// every one after that is bought</b>, and what you actually receive is a <b>Subclass Ticket</b> — an
/// ITEM, not a counter. Earning or buying one puts it in your bag; CONSUMING it opens the slot. His
/// words: *"those values give you a subclassTicket and u can unlock them using(consumable) ticket"*.
/// That separation is not flavour: it lets the ticket your main's 4th class paid you sit in the bag
/// until you know which class you want.</para>
///
/// <list type="table">
///   <item><term>slot 1</term><description>earned — your MAIN reaches 76 and takes its 4th class</description></item>
///   <item><term>slot 2</term><description>earned — subclass #1 reaches 75</description></item>
///   <item><term>slot 3</term><description>earned — subclass #2 reaches 75 (#3 reaching 75 pays nothing)</description></item>
///   <item><term>slot 4</term><description>bought — 500kk gold</description></item>
///   <item><term>slot 5</term><description>bought — 5kkk gold</description></item>
///   <item><term>slot 6</term><description>bought — 100 platinum</description></item>
///   <item><term>slot 7</term><description>bought — 1,000 platinum</description></item>
/// </list>
///
/// <para>🔑 <b>SEVEN RUNGS FOR SEVEN REACHABLE SUBCLASSES.</b> He originally priced an eighth at 5,000
/// platinum. Measured, the twelve live disciplines fold into <b>eight paths</b> (`BL-255`) and a main
/// occupies one, so seven is every subclass anybody can ever hold — the eighth ticket would have sat
/// permanently behind its own *"no more available subclasses"* text. He <b>cut the top rung</b>
/// (2026-09-17): *"remove last platinum rung for now and when summoner is build we will add it back"*.
/// </para>
///
/// <para>🔑 <b>PUTTING IT BACK IS ONE ROW HERE AND NOTHING ELSE</b>, and that is by construction: the
/// prices are an authored LIST whose length defines the ceiling, and whether another ticket may be
/// bought is a COMPUTED question ("is there a path this character could still legally add") rather
/// than an authored number. <b>Never hard-code seven, or eight, anywhere.</b> The day the summoner
/// adds a ninth path, add the 5,000-platinum rung back to <see cref="BoughtRungs"/> and the ladder,
/// the shop text and the refusal message all move with it.</para>
/// </summary>
public static class SubclassSlots
{
    /// <summary>Slots that arrive with your progress and are never bought. Slot 1 is your main's 4th
    /// class; slots 2 and 3 are subclasses #1 and #2 reaching <see cref="ThirdClassCatalog.SubclassLevel"/>.</summary>
    public const int EarnedSlots = 3;

    /// <summary>Character level at which a MAIN pays its earned ticket — the 4th class change. The
    /// CLASS is the real gate (see <c>EarnedTicketsDue</c>); the level is stated here because both
    /// halves of one rule belong in one place.</summary>
    public const int MainTicketLevel = FourthClassCatalog.ChangeLevel;   // 76

    /// <summary>What one bought rung costs. Gold, platinum, or both — the same two-number shape every
    /// price in the game has carried since `BL-257`, and both are charged when both are set.</summary>
    public readonly record struct Rung(long Gold, int Platinum);

    /// <summary>The BOUGHT rungs in ladder order — slot <see cref="EarnedSlots"/>+1 upward.
    /// ⚠ APPEND ONLY, and the length IS the ceiling. The retired 5,000-platinum rung goes back on the
    /// END of this array the day a ninth class path exists.</summary>
    public static readonly Rung[] BoughtRungs =
    {
        new(500_000_000L, 0),      // slot 4 — 500kk gold
        new(5_000_000_000L, 0),    // slot 5 — 5kkk gold (Gold is a long on the entity, the record and
                                   //           every DTO, so five billion fits with room to spare)
        new(0L, 100),              // slot 6 — 100 platinum
        new(0L, 1_000),            // slot 7 — 1,000 platinum
        // new(0L, 5_000),         // slot 8 — RETIRED 2026-09-17 until the summoner adds a ninth path.
    };

    /// <summary>Every slot the ladder can open, earned and bought. NOT counting the main class.</summary>
    public static int MaxSlots => EarnedSlots + BoughtRungs.Length;

    /// <summary>The price of the ticket that opens slot <paramref name="slot"/> (1-based, subclass
    /// slots only — the main is not a slot). <c>null</c> when that slot is EARNED rather than bought,
    /// or when it is past the end of the ladder; the two cases are distinguished by comparing against
    /// <see cref="EarnedSlots"/>, because they mean opposite things to a caller.</summary>
    public static Rung? PriceOf(int slot) =>
        slot > EarnedSlots && slot <= MaxSlots
            ? BoughtRungs[slot - EarnedSlots - 1]
            : null;
}
