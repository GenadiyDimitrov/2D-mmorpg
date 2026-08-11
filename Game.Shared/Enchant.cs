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

    // ===================================================================================
    //  WHAT AN ENCHANT IS WORTH — his table, 2026-08-11 (0.60.0)
    // ===================================================================================
    //
    // ⚠ THIS REPLACED A PERCENTAGE. Until 0.60.0 every enchanted stat ran through one formula,
    // `base + 0.20*base*level + level`, applied to EVERY bonus on EVERY slot. That is ×4.2 at +16,
    // against a ladder whose top weapon is 437 P.Atk — so a +16 S blade hit for 1851 and a +16 S
    // armour set quartered incoming damage. Enchanting was worth about two and a half grades in
    // both directions at once, which made PvP a count of scrolls rather than of gear.
    //
    // His replacement is FLAT PER ENCHANT, per slot, with a grade table only where the ladder is
    // meant to show through (armour HP, jewel MP, bow P.Atk). Three consequences he ruled on
    // explicitly, so don't "fix" them later:
    //
    //  * It is the SAME OFFSET FOR EVERY CLASS. A full +16 armour set is +1920 Max HP at S whether
    //    it is worn by a tank or a healer — which is +36% for the tank and +129% for the healer.
    //    That is deliberate: *"a healer spends gold/farm to enchant gear to +16, he gets the full
    //    bonus — he will be stronger than just a warrior."* An enchant is an offset from the norm,
    //    identical for all; it is not scaled by armour weight, by class, or by which piece it is on.
    //  * It is by GRADE, not by RARITY. A Common S body and a Mythic S body both gain 30 HP per
    //    enchant, so enchanting a cheap piece is relatively better value than enchanting a top one.
    //  * The BOW is the one weapon whose per-enchant value climbs with grade (10 -> 20), on top of
    //    already having the highest base P.Atk: *"as archer they rely on basic attack and acc so a
    //    more P.Atk jump is better, while the others should rely more on crit/skills."*
    //
    // Everything NOT in his table stopped scaling with enchant at 0.60.0: Evasion, a robe's inherent
    // +MP, a weapon's +MP, an armour piece's M.Def. They pay their authored number and nothing more.
    // Measure any change here with tools/BalanceMatrix §E (the +0 vs +16 DPS/EHP table).

    /// <summary>P.Def a single ARMOUR piece gains per enchant level.</summary>
    public const int ArmorDefPerEnchant = 3;

    /// <summary>Defence a SHIELD gains per enchant level — <b>TRIPLE</b> an armour piece's, so +16 is
    /// +144 (him, 2026-08-11: *"shield should get triple the def because S grade only 30% chance to
    /// block"*). The shield's own damage reduction only pays out on a successful block — 25% of hits
    /// at S since the 2026-08-11 block re-cut — so its enchant has to pay in the flat defence that
    /// applies to every hit, or enchanting a shield would be the worst scroll in the game. Its Max HP
    /// is the ordinary armour row (480 at S), not tripled.</summary>
    public const int ShieldDefPerEnchant = ArmorDefPerEnchant * 3;

    /// <summary>M.Def a single JEWEL gains per enchant level.</summary>
    public const int JewelMDefPerEnchant = 3;

    /// <summary>M.Atk ANY weapon gains per enchant level — sword and wand alike (him: *"all weapon —
    /// sword and wand get the same +16 M.Atk … so if I want a magic fighter later on it can be
    /// done"*). A caster still out-casts a fighter through cast speed and the class kit, not through
    /// a bigger enchant.</summary>
    public const int WeaponMAtkPerEnchant = 6;

    /// <summary>P.Atk a ONE-HANDED weapon gains per enchant level — sword, blunt, wand and duals.
    /// (Duals are one-handed for this purpose even though they occupy both hands.)</summary>
    public const int WeaponAtkPerEnchant1H = 6;

    /// <summary>P.Atk a TWO-HANDED weapon gains per enchant level — greatsword, maul and staff.</summary>
    public const int WeaponAtkPerEnchant2H = 8;

    // Indexed by (int)EnchantGrade: None, E, D, C, B, A, S. None takes E's value for the flat rows
    // (F gear is not scroll-enchantable, but `/enchant` reaches it and must still do something) and
    // zero for the rows that only open at C.

    /// <summary>P.Atk a BOW gains per enchant level, by grade: E 10 … S 20.</summary>
    private static readonly int[] BowAtkPerEnchant = { 10, 10, 12, 14, 16, 18, 20 };

    /// <summary>Max HP an ARMOUR piece — or a SHIELD — gains per enchant level, by grade: nothing at
    /// E/D, then C 15 / B 20 / A 25 / S 30, so at +16 a piece is worth 240/320/400/480 HP. The shield
    /// takes this row untouched (*"but same HP … 480HP and 144 shield defense"*); only its DEFENCE
    /// is tripled.</summary>
    private static readonly int[] ArmorHpPerEnchant = { 0, 0, 0, 15, 20, 25, 30 };

    /// <summary>Max MP a JEWEL gains per enchant level, by grade: nothing at E/D, then
    /// C 1 / B 2 / A 3 / S 5 — so at +16 a jewel is worth 16/32/48/80 MP.</summary>
    private static readonly int[] JewelMpPerEnchant = { 0, 0, 0, 1, 2, 3, 5 };

    /// <summary>P.Atk this weapon gains per enchant level (bow by grade, 2H 8, everything else 6).
    /// Public so the item card and BalanceMatrix can print "+N per enchant" without re-deriving it.</summary>
    public static int AtkPerEnchant(ItemDef def) =>
        def.Slot != EquipSlot.Weapon ? 0 :
        def.WeaponType == WeaponType.Bow ? BowAtkPerEnchant[(int)GradeOf(def)] :
        def.WeaponType is WeaponType.TwoHandedSword or WeaponType.TwoHandedBlunt ? WeaponAtkPerEnchant2H :
        WeaponAtkPerEnchant1H;

    /// <summary>Total P.Atk an enchant level adds to this item (0 for anything but a weapon).</summary>
    public static int AtkDelta(ItemDef def, int enchant) =>
        enchant <= 0 ? 0 : AtkPerEnchant(def) * enchant;

    /// <summary>Total M.Atk an enchant level adds to this item (0 for anything but a weapon).</summary>
    public static int MAtkDelta(ItemDef def, int enchant) =>
        enchant <= 0 || def.Slot != EquipSlot.Weapon ? 0 : WeaponMAtkPerEnchant * enchant;

    /// <summary>Total P.Def an enchant level adds to an ARMOUR piece.</summary>
    public static int DefDelta(ItemDef def, int enchant) =>
        enchant <= 0 || def.Slot != EquipSlot.Armor ? 0 : ArmorDefPerEnchant * enchant;

    /// <summary>Total shield defence an enchant level adds (9/level — see
    /// <see cref="ShieldDefPerEnchant"/>). A SEPARATE method because a shield's defence is
    /// <see cref="ItemDef.ShieldDefense"/>, a different field on a different accumulator; folding the
    /// two together would double-count. Block chance / reduction / crit-defence do NOT scale.</summary>
    public static int ShieldDefDelta(ItemDef def, int enchant) =>
        enchant <= 0 || def.Slot != EquipSlot.Shield ? 0 : ShieldDefPerEnchant * enchant;

    /// <summary>Total M.Def an enchant level adds. Jewels are the only source of M.Def, and the only
    /// slot this pays out on — an armour piece's authored M.Def does not scale.</summary>
    public static int MDefDelta(ItemDef def, int enchant) =>
        enchant <= 0 || def.Slot != EquipSlot.Jewel ? 0 : JewelMDefPerEnchant * enchant;

    /// <summary>Total Max HP an enchant level adds — ARMOUR and SHIELDS, by grade (nothing below C).
    /// A shield is a worn defensive piece and pays the same HP row; it is only its DEFENCE that is
    /// tripled. So a tank enchanting five pieces (body + 3 accessories + shield) is buying five
    /// helpings of it, and is the one build that can.</summary>
    public static int HpDelta(ItemDef def, int enchant) =>
        enchant <= 0 || def.Slot is not (EquipSlot.Armor or EquipSlot.Shield)
            ? 0 : ArmorHpPerEnchant[(int)GradeOf(def)] * enchant;

    /// <summary>Total Max MP an enchant level adds — JEWELS only, by grade (nothing below C). A
    /// robe's own large +MP is NOT enchant-scaled; the mana an enchant buys comes from jewellery.</summary>
    public static int MpDelta(ItemDef def, int enchant) =>
        enchant <= 0 || def.Slot != EquipSlot.Jewel
            ? 0 : JewelMpPerEnchant[(int)GradeOf(def)] * enchant;

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
