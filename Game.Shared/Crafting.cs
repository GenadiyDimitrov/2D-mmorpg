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
    /// ⚠ The owner's gate is *"L5,6 needs 76 (4th class)"*, but *there is no 4th class yet* — it is
    /// blocked on his 40+ CSVs. So the top band is opened by **level 76 alone** for now.
    /// <see cref="RequireFourthClassForL5"/> is the one line to flip when 4th classes land; the argument
    /// is already threaded through every caller so that flip touches nothing else.</summary>
    public static int BandCap(int charLevel, bool hasThirdClass, bool hasFourthClass) =>
        charLevel >= 76 && (hasFourthClass || !RequireFourthClassForL5) ? MaxCraftLevel
        : charLevel >= 40 && hasThirdClass ? 4
        : charLevel >= 20 ? 2
        : 0;   // below 20 you cannot hold a profession at all — the master's quest is gated there too

    /// <summary>Flip to true the day a 4th class exists. Until then level 76 opens L5/L6 on its own,
    /// because gating on a class nobody can take would make the top two rungs unreachable.</summary>
    public const bool RequireFourthClassForL5 = false;

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

    /// <summary>The odds of one gear craft attempt. Sums to 1: a craft lands on Mythic, lands on
    /// Legendary, or FAILS and eats the materials.</summary>
    public readonly record struct GearOdds(float Mythic, float Legendary, float Fail);

    /// <summary>The owner's success table, verbatim (2026-08-13): *"the gear is not crafted at 100% …
    /// E - (50% for mytic, 40% for legend, 10% fail); D - 45m, 40l, 15fail; C - 40m, 40l, 20fail;
    /// B - 30m, 40l, 30fail; A - 20, 30, 50fail; S - 5m, 20l, 75 fail"*.
    ///
    /// 🔑 **Only Legendary and Mythic gear is craftable** — *"the only craftable gears should be legend,
    /// mytic (others are drop based anyways)"* — so there is no third success rung to fall to. A fail
    /// produces nothing and consumes the mats, and that is the first real sink the crafting economy has.
    ///
    /// ⚠ The fail rate and the mat cost are ONE knob, not two: at 75% a successful S item costs four
    /// attempts, so quadrupling the fail rate is arithmetically the same act as quadrupling the pile.
    /// Both were solved together in <c>Recipes.GearBulk</c> — move one and the other is wrong.</summary>
    public static GearOdds GearCraftOdds(int craftLevel) => craftLevel switch
    {
        1 => new(0.50f, 0.40f, 0.10f),   // E
        2 => new(0.45f, 0.40f, 0.15f),   // D
        3 => new(0.40f, 0.40f, 0.20f),   // C
        4 => new(0.30f, 0.40f, 0.30f),   // B
        5 => new(0.20f, 0.30f, 0.50f),   // A
        _ => new(0.05f, 0.20f, 0.75f),   // S
    };

    /// <summary>True if this rung's recipes are GEAR (three-way outcome) rather than materials or
    /// consumables (a plain <see cref="Recipe.SuccessChance"/> roll).</summary>
    public static bool IsGearSlot(EquipSlot slot) =>
        slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield or EquipSlot.Jewel;
}
