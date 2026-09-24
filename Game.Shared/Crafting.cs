namespace Game.Shared;

/// <summary>Crafting professions — one per character, granted by that profession's MASTER after his
/// joining quest, and quittable at him (`BL-05`). Each REFINES one material type and crafts one item
/// family. See docs/design/CraftingProfessions.md for the level/exp model and
/// docs/design/Crafting.md for the material economy underneath it.</summary>
public enum Profession { None = 0, WeaponSmith, ArmorSmith, Jeweler, PotionMaster, ScrollScribe }

/// <summary>The 5 crafting material types. Each is REFINED (raw → higher rarity) only by its owning
/// profession, but every rarity also DROPS from mobs — so professions are an efficiency/trade path,
/// not a gate. Finished items need several types → cross-profession trade.</summary>
public enum MaterialType { Ingot = 0, Thread, Wood, Leather, Gem }

public static class Crafting
{
    /// <summary>Which profession refines a material type (and is the one that can upgrade it).</summary>
    public static Profession RefinerOf(MaterialType type) => type switch
    {
        MaterialType.Ingot   => Profession.WeaponSmith,
        MaterialType.Leather => Profession.ArmorSmith,
        MaterialType.Gem     => Profession.Jeweler,
        MaterialType.Wood    => Profession.PotionMaster,
        MaterialType.Thread  => Profession.ScrollScribe,
        _ => Profession.None
    };

    /// <summary>Material rarities — the FULL six, matching <see cref="ItemRarity"/> exactly.
    ///
    /// ⚠ Mythic was added 2026-08-12 with `BL-05`. It used to stop at Legendary, because materials only
    /// ever fed gear up to the Legendary rung. The crafting-level ladder made that an exception it could
    /// not afford: level N crafts goods of rarity N-1 out of mats of THAT SAME rarity (the whole of
    /// `BL-40`'s fix), and L6 crafts Mythic goods — so without a Mythic mat the top rung would have had
    /// to eat Legendary mats at some invented multiple, and the one irregular rung would have been the
    /// only place the economy could not be reasoned about. Five generated item defs and one more refine
    /// step buy an exceptionless ladder.</summary>
    public static readonly ItemRarity[] MaterialRarities =
        { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare,
          ItemRarity.Epic, ItemRarity.Legendary, ItemRarity.Mythic };

    public static readonly MaterialType[] MaterialTypes =
        { MaterialType.Ingot, MaterialType.Thread, MaterialType.Wood, MaterialType.Leather, MaterialType.Gem };

    /// <summary>Stable item id for a material of a type + rarity, e.g. "mat_gem_rare".</summary>
    public static string MaterialId(MaterialType type, ItemRarity rarity) =>
        $"mat_{type.ToString().ToLowerInvariant()}_{rarity.ToString().ToLowerInvariant()}";

    /// <summary>Display name, e.g. "Rare Gem".</summary>
    public static string MaterialName(MaterialType type, ItemRarity rarity) =>
        $"{rarity} {type}";

    // =====================================================================================
    //  CRAFTING LEVELS L1-L6 (owner, playtest-21 `66a` → `BL-05`)
    //  docs/design/CraftingProfessions.md is the spec; this is the arithmetic.
    // =====================================================================================

    /// <summary>The top crafting level. Six, because <see cref="ItemRarity"/> has exactly six rungs and
    /// a crafting level IS a rarity: **at level N you craft goods of rarity N-1 and refine up to rarity
    /// N**. L1 crafts Common and refines into Uncommon; L6 crafts Mythic and refines nothing.</summary>
    public const int MaxCraftLevel = 6;

    /// <summary>One craft AT YOUR OWN LEVEL is worth this much internal exp.
    ///
    /// 🔑 Why 12 and not 1. The owner's spec is in whole "exp" whose marks are 0/5/15/30/50/100, where a
    /// same-level craft is 1/10th of a mark, a craft one rung BELOW pays a third of that, and one rung
    /// ABOVE pays a quarter more (*"higher 1 grade gives 20% more exp (so ~8 items)"*). A tenth, a third
    /// of a tenth and a quarter more than a tenth have no common representation in an int, and in a float
    /// they drift — 150 crafts would not land exactly on a level boundary, which is the one place a
    /// player counts. 12 is the smallest unit divisible by both 3 and 4, so **every rung is an exact
    /// integer** (4 / 12 / 15) and his craft counts come out whole: 150 same-level, 450 below, 120 above.
    /// Divide by <see cref="CraftExpPerMark"/> to show his numbers back to him.</summary>
    public const int CraftExpPerCraft = 12;

    /// <summary>Internal exp per one point of the owner's 0/5/15/30/50/100 scale — ten same-level
    /// crafts, from *"x10 crafts per difference of same level"*.</summary>
    public const int CraftExpPerMark = CraftExpPerCraft * 10;   // 120

    /// <summary>CUMULATIVE internal exp at which each crafting level begins, indexed by level-1. The
    /// owner's marks (0/5/15/30/50/100) × <see cref="CraftExpPerMark"/>.</summary>
    public static readonly int[] CraftLevelMarks =
        { 0, 5 * CraftExpPerMark, 15 * CraftExpPerMark, 30 * CraftExpPerMark,
          50 * CraftExpPerMark, 100 * CraftExpPerMark };   // 0 / 600 / 1800 / 3600 / 6000 / 12000

    /// <summary>The crafting level a raw exp total is worth, 1-6. Never 0: holding a profession at all
    /// means L1 (*"After quest u become l1"*).</summary>
    public static int LevelForExp(int exp)
    {
        int lvl = 1;
        for (int i = 1; i < CraftLevelMarks.Length; i++)
            if (exp >= CraftLevelMarks[i]) lvl = i + 1;
        return lvl;
    }

    /// <summary>The level actually in force: what the exp is worth, held down to what the character's
    /// progression allows.
    ///
    /// 🔑 This is where the freeze becomes visible, and the two halves have to be kept apart to get the
    /// owner's *"the l2@100% becomes l3@0%"* right. <see cref="CapExp"/> stops the exp ON the mark that
    /// opens the next level (1800 = the start of L3), and this clamps the LEVEL below it. So a level-20
    /// character sits at exp 1800 / level 2 — which <see cref="LevelProgress"/> then reads as a full
    /// bar, because 1800 is the top of L2. The instant the band opens, the same 1800 is level 3 with an
    /// empty bar. Nothing is recomputed and no exp moves; only the ceiling lifts.</summary>
    public static int EffectiveLevel(int exp, int bandCap) =>
        System.Math.Min(LevelForExp(exp), System.Math.Max(1, bandCap));

    /// <summary>Progress THROUGH <paramref name="level"/> as 0..1 — the *"l2@100%"* the owner writes in.
    /// Pass the <see cref="EffectiveLevel"/>, not the raw one: at a frozen band cap this then genuinely
    /// reads 100%, which is the point — a frozen bar is supposed to look full and stop.</summary>
    public static float LevelProgress(int exp, int level)
    {
        if (level >= MaxCraftLevel) return 1f;
        int from = CraftLevelMarks[level - 1], to = CraftLevelMarks[level];
        return to <= from ? 1f : System.Math.Clamp((exp - from) / (float)(to - from), 0f, 1f);
    }

    /// <summary>Exp for ONE craft of a recipe rung against the crafter's current level. Returns 0 for a
    /// recipe two or more rungs below (*"a -2 grades dont give of exp"* / *"L1 does nothing only craft
    /// result no exp"*). A recipe two or more rungs ABOVE is not craftable at all and never reaches
    /// here — see <see cref="CanCraftAt"/>.</summary>
    public static int CraftExp(int recipeLevel, int craftLevel) => (recipeLevel - craftLevel) switch
    {
        -1 => CraftExpPerCraft / 3,        // 4  — "lower 1 grade = 3 times more" crafts
        0  => CraftExpPerCraft,            // 12
        1  => CraftExpPerCraft * 5 / 4,    // 15 — "higher 1 grade gives 20% more exp (so ~8 items)"
        _  => 0                            // -2 and below pay nothing; +2 and above never get here
    };

    /// <summary>May a crafter at <paramref name="craftLevel"/> attempt this recipe at all? Everything at
    /// or below your level, plus exactly one rung above (*"L5 should not be available"* to an L3).</summary>
    public static bool CanCraftAt(int recipeLevel, int craftLevel) =>
        recipeLevel <= craftLevel + 1;

    /// <summary>The CHARACTER level a crafting level demands: *"L1,2 crafts need lvl20 (2nd class) ·
    /// L3,4 needs 40 (3rd class) · L5,6 needs 76 (4th class)"*.</summary>
    public static int CharLevelFor(int craftLevel) => craftLevel switch
    {
        <= 2 => 20,
        <= 4 => 40,
        _    => 76
    };

    /// <summary>The highest crafting level this character's PROGRESSION allows right now — the ceiling
    /// the exp freezes against.
    ///
    /// 🔑 *"my exp freezes until i reach the next class … then the l2@100% becomes l3@0%"*. The band is
    /// what stops a level-20 character grinding out L6 in town, which is the owner's stated reason for
    /// the gate existing at all (*"not i just to make 10 chars to sit in town and craft"*).
    ///
    /// ✅ The owner's gate is *"L5,6 needs 76 (4th class)"*, and as of 2026-08-17 a 4th class EXISTS —
    /// so <see cref="RequireFourthClassForL5"/> was flipped and the top band now needs the ascension,
    /// not merely level 76. This is the whole flip: the argument was already threaded through every
    /// caller, and `Entity.CraftBandCap` stopped passing a hard-coded false the same day.</summary>
    public static int BandCap(int charLevel, bool hasThirdClass, bool hasFourthClass) =>
        charLevel >= 76 && (hasFourthClass || !RequireFourthClassForL5) ? MaxCraftLevel
        : charLevel >= 40 && hasThirdClass ? 4
        : charLevel >= 20 ? 2
        : 0;   // below 20 you cannot hold a profession at all — the master's quest is gated there too

    /// <summary>TRUE since 2026-08-17, the day the 4th class landed. It was false only because gating
    /// on a class nobody could take would have made the top two rungs unreachable — that reason is
    /// gone. ⚠ This is a real gate change for anyone already at 76: L5/L6 now costs the 100kk Rite of
    /// Ascension. Set it back to false to reopen L5/L6 on level alone.</summary>
    public const bool RequireFourthClassForL5 = true;

    /// <summary>Clamp raw exp to the top of the band the character has earned. The excess is DISCARDED,
    /// not banked: banking would let a character sit at the cap accumulating invisible progress and then
    /// jump several levels the moment they class up, which is the exact grind-in-town the gate forbids.
    ///
    /// The wall sits ON the mark that opens the next level (band 2 → exp 1800, the first point of L3),
    /// and <see cref="EffectiveLevel"/> holds the level at 2 there. That pairing is what makes the freeze
    /// read as "L2, 100%" instead of "L2, 99.9%" — and makes lifting the band a pure ceiling change with
    /// no exp to migrate. Stopping one point short would show a bar that never fills.</summary>
    public static int CapExp(int exp, int charLevel, bool hasThirdClass, bool hasFourthClass) =>
        CapExpToBand(exp, BandCap(charLevel, hasThirdClass, hasFourthClass));

    /// <summary>The same clamp against an ALREADY-COMPUTED band, for callers that derive the band from
    /// something richer than one character level — the server takes it from the BEST subclass, so that
    /// swapping to a fresh subclass cannot appear to shrink a crafter's band.</summary>
    public static int CapExpToBand(int exp, int bandCap)
    {
        if (bandCap <= 0) return 0;
        return System.Math.Min(exp, CraftLevelMarks[System.Math.Min(bandCap, MaxCraftLevel - 1)]);
    }

    /// <summary>The rarity of the GOODS a crafting level makes. L1 → Common … L6 → Mythic.</summary>
    public static ItemRarity GoodsRarity(int craftLevel) =>
        (ItemRarity)System.Math.Clamp(craftLevel - 1, 0, (int)ItemRarity.Mythic);

    /// <summary>The crafting level that makes goods of this rarity — the inverse of
    /// <see cref="GoodsRarity"/>, and the rung a recipe is filed under.</summary>
    public static int CraftLevelOf(ItemRarity rarity) => (int)rarity + 1;

    /// <summary>The rarity a crafting level can REFINE INTO. L1 turns Common into Uncommon; L5 turns
    /// Legendary into Mythic; L6 has nothing above it and refines nothing.</summary>
    public static ItemRarity? RefineTarget(int craftLevel) =>
        craftLevel >= 1 && craftLevel < MaxCraftLevel ? (ItemRarity)craftLevel : null;

    // =====================================================================================
    //  GEAR: the ladder is GRADE-based, not rarity-based (owner, 2026-08-13)
    //  *"just the idea is grade based not as much as rarity based"*
    //  docs/design/CraftingProfessions.md §5c.
    // =====================================================================================

    /// <summary>The gear ITEM LEVELS each crafting rung serves, indexed by level-1 — the grade floors
    /// E 20 · D 40 · C 52 · B 61 · A 76 · S 80.
    ///
    /// 🔑 **F is deliberately absent, and that absence is what makes the ladder exact.** The owner:
    /// *"rly no point to craft F grade … its mostly to get you to 20 (as u get free mytic @10/15) … so
    /// 7 grades - 1 = 6"*. Seven grades minus F is six, against six crafting rungs, so nothing is shared
    /// and nothing is invented. It also keeps every grade at or below its own character band, which the
    /// alternative (C and B sharing a rung) did not — that parked B behind the character-76 gate and let
    /// a level-61 player WEAR B gear he could not MAKE.</summary>
    public static readonly int[] GearItemLevels = { 20, 40, 52, 61, 76, 80 };

    /// <summary>The crafting rung that makes gear of this item level, 1-6 — or **0 for F**, which is not
    /// craftable at all. Anything at or above the S floor is L6; there is no rung above it.</summary>
    public static int GearCraftLevel(int itemLevel)
    {
        int lvl = 0;
        for (int i = 0; i < GearItemLevels.Length; i++)
            if (itemLevel >= GearItemLevels[i]) lvl = i + 1;
        return lvl;
    }

    // =====================================================================================
    //  ESSENCE (`BL-273` part 1, 0.200.0; tables rewritten by `BL-287`, 0.201.0) — breaking gear gives
    //  its GRADE's essence, nothing else.
    // =====================================================================================
    //
    // His rule (2026-09-23, design doc §2.4): *"breaking common or even mythic darksteel gives you
    // 'Darksteel essence' (Cobolt -> 'Cobolt Essence' ... etc...) - and crafting to require also this
    // essence that is aquired only by breaking full items -> so not mindlessly selling in the vendor"*.
    // It replaced the `BL-22` disassembly roll, which is deleted outright, not kept beside it.
    //
    // 🔑 THE AMOUNT IS AUTHORED, NEVER COMPUTED. *"changing prices later should not change the essence
    //    amount"*. The tables below are literals on purpose: retuning a price must not move a single cell
    //    here, and nothing reads a price to produce them.
    //
    // How they were written (`BL-287`, owner, 2026-09-24, design doc §2.5 — generated ONCE from the
    // 0.201.0 prices, so the next cell can be written the same way):
    //   * BREAK = 0.4 × the item's own BUY price ÷ its essence's SELL price, rounded half away from zero.
    //     Selling returns 50%, breaking 40% (as essence at its sell price): *"breaking it down gives less
    //     than its actual sell price"*, and *"one common item or 2 [must] not allow me to craft an item"*.
    //   * Mythic and Common alike (a Common is 0.05 × its Mythic, so it breaks for 1/20th); the 0.200.0
    //     "Common breaks for 70%" is gone. T76/T80 have no Commons (their essence DROPS, step 12).
    //   * The essence sell prices are his: D 1500 / C 4500 / B 7500 / A 12500 / S 25000.
    //
    // ⚠ F and E gear (T1 / T20) CANNOT be broken (ruled 2026-09-24): the five essences are D..S, and a
    //   Ferrite or Electrum piece is sold, not broken.

    /// <summary>The five grade essences, index 0 = D (Darksteel) … 4 = S (Soulcrystal). The id is stable;
    /// the display name is "{GradeTheme} Essence".</summary>
    public static readonly string[] EssenceIds =
        { "essence_d", "essence_c", "essence_b", "essence_a", "essence_s" };

    /// <summary>The item level each essence is named after (its <see cref="ItemCatalog.GradeTheme"/>).</summary>
    public static readonly int[] EssenceItemLevels = { 40, 52, 61, 76, 80 };

    /// <summary>What a vendor pays for ONE essence (`BL-287`, his numbers): D 1500 / C 4500 / B 7500 /
    /// A 12500 / S 25000. The break tables were authored against these. The essence's <c>Value</c> is twice
    /// this, so the ordinary half-price sell rule pays exactly this number; that Value is a yardstick only,
    /// never a shop price (*"no vendor sells essence"*).</summary>
    public static readonly int[] EssenceSellPrice = { 1_500, 4_500, 7_500, 12_500, 25_000 };

    /// <summary>Which essence grade an item level breaks into, or -1 for F/E (no essence).</summary>
    public static int EssenceGrade(int itemLevel) => itemLevel switch
    {
        >= 80 => 4, >= 76 => 3, >= 61 => 2, >= 52 => 1, >= 40 => 0, _ => -1,
    };

    // Column order of the two tables: 2H, 1H, body, helm/shield, gloves/boots, necklace, earring, ring.
    private static readonly int[][] MythicBreak =
    {
        new[] { 1143, 1029,  686,  381,  229,  571,  190,  95 },   // T40 Darksteel   (2H 4.29M ÷ 1500)
        new[] { 1200, 1080,  720,  400,  240,  600,  200, 100 },   // T52 Cobalt      (2H 13.5M ÷ 4500)
        new[] { 3200, 2880, 1920, 1067,  640, 1600,  533, 267 },   // T61 Bloodsteel  (2H 60M ÷ 7500)
        new[] { 3840, 3456, 2304, 1280,  768, 1920,  640, 320 },   // T76 Adamantine  (2H 120M ÷ 12500)
        new[] { 9600, 8640, 5760, 3200, 1920, 4800, 1600, 800 },   // T80 Soulcrystal (2H 600M ÷ 25000)
    };

    private static readonly int[][] CommonBreak =
    {
        new[] {  57,  51,  34,  19,  11,  29,  10,   5 },   // T40
        new[] {  60,  54,  36,  20,  12,  30,  10,   5 },   // T52
        new[] { 160, 144,  96,  53,  32,  80,  27,  13 },   // T61
    };

    /// <summary>Which column of the break tables a piece of gear reads, or -1 if it is not gear.</summary>
    private static int BreakColumn(ItemDef def) => def.Slot switch
    {
        EquipSlot.Weapon => def.WeaponType.IsTwoHanded() ? 0 : 1,
        EquipSlot.Shield => 3,
        EquipSlot.Armor => def.ArmorSlot switch
        {
            ArmorSlot.Body => 2,
            ArmorSlot.Head => 3,
            ArmorSlot.Gloves or ArmorSlot.Boots => 4,
            _ => -1,
        },
        EquipSlot.Jewel => def.JewelType switch
        {
            JewelType.Necklace => 5,
            JewelType.Earring => 6,
            JewelType.Ring => 7,
            _ => -1,
        },
        _ => -1,
    };

    /// <summary>What breaking an item gives: an essence id and how many.</summary>
    public readonly record struct EssenceYield(string EssenceId, int Qty);

    /// <summary>The authored essence a piece of gear breaks into (`BL-273`), or null if it cannot be
    /// broken: not gear, untiered (quest/debug one-offs), F/E grade, or a rarity the tables do not
    /// hold. Mythic reads <c>MythicBreak</c>, Common reads <c>CommonBreak</c>; there is no third rung.
    /// Server and client both ask this, so the Break button can never offer what the server refuses.</summary>
    public static EssenceYield? BreakYield(ItemDef? def)
    {
        if (def is null || def.ItemLevel <= 0) return null;
        int grade = EssenceGrade(def.ItemLevel);
        int col = BreakColumn(def);
        if (grade < 0 || col < 0) return null;
        int[][] table = def.Rarity switch
        {
            ItemRarity.Mythic => MythicBreak,
            ItemRarity.Common => CommonBreak,
            _ => Array.Empty<int[]>(),
        };
        if (grade >= table.Length) return null;
        int qty = table[grade][col];
        return qty > 0 ? new EssenceYield(EssenceIds[grade], qty) : null;
    }

    /// <summary>What a SHATTERED enchant leaves behind (`BL-273`, second round): failing +N → N+1 with a
    /// Normal scroll returns <b>N × 10%</b> of the item's break value (+3→4 = 30%, +10→11 = 100%,
    /// +15→16 = 150%). *"if u invest in safe enchants just to break it .. good for u"* — enchanting up to
    /// break is intended. Only a Normal scroll destroys the item, so only that path calls this; Commons
    /// cannot be enchanted, so in practice it is Mythic only. Rounded down.</summary>
    public static EssenceYield? ShatterYield(ItemDef? def, int enchantBefore)
    {
        if (BreakYield(def) is not EssenceYield y || enchantBefore <= 0) return null;
        int qty = y.Qty * enchantBefore / 10;
        return qty > 0 ? y with { Qty = qty } : null;
    }

    /// <summary>The odds of one gear craft attempt. Sums to 1: a craft lands on the (Mythic) piece, or
    /// FAILS and eats the materials.
    ///
    /// ⚠ It was three-way (Mythic / Legendary / fail) until `BL-272` deleted the Legendary rung. His
    /// interim ruling (2026-09-24) until the recipe-% rework (`BL-273` part 2): the old Legendary share
    /// becomes a FAIL, so the Success column is his old Mythic column unchanged.</summary>
    public readonly record struct GearOdds(float Success, float Fail);

    /// <summary>The owner's success table (2026-08-13): *"E - (50% for mytic, 40% for legend, 10% fail);
    /// D - 45m, 40l, 15fail; C - 40m, 40l, 20fail; B - 30m, 40l, 30fail; A - 20, 30, 50fail; S - 5m,
    /// 20l, 75 fail"*, with the Legendary share folded into the fail (see <see cref="GearOdds"/>).
    ///
    /// ⚠ The fail rate and the mat cost are ONE knob, not two: at 95% a successful S item costs twenty
    /// attempts. `Recipes.GearBulk` was sized against the old 75% (four), so an S craft is 5× dearer until the
    /// `BL-273` recipe rework replaces this table.</summary>
    public static GearOdds GearCraftOdds(int craftLevel) => craftLevel switch
    {
        1 => new(0.50f, 0.50f),   // E
        2 => new(0.45f, 0.55f),   // D
        3 => new(0.40f, 0.60f),   // C
        4 => new(0.30f, 0.70f),   // B
        5 => new(0.20f, 0.80f),   // A
        _ => new(0.05f, 0.95f),   // S
    };

    /// <summary>True if this rung's recipes are GEAR (success-or-fail outcome) rather than materials or
    /// consumables (a plain <see cref="Recipe.SuccessChance"/> roll).</summary>
    public static bool IsGearSlot(EquipSlot slot) =>
        slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield or EquipSlot.Jewel;
}
