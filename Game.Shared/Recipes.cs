namespace Game.Shared;

/// <summary>One ingredient of a recipe: an item id (material or component) and a quantity.</summary>
public record RecipeInput(string ItemId, int Qty);

/// <summary>
/// A crafting recipe (`BL-273` part 2, 0.203.0): turns <see cref="Inputs"/> into <see cref="OutputId"/>
/// ×<see cref="OutputQty"/>. Anyone who has finished the crafter quest may hold it, in one of their
/// recipe SLOTS. There are no professions: <see cref="Type"/> only says which type level an attempt
/// raises (weapon / armour / jewels), and General recipes raise the generic level alone.
///
/// <para>Two kinds, told apart by <see cref="IsGear"/>:</para>
/// <list type="bullet">
/// <item><b>Gear</b> (T40-T80 weapons, armour, shields, jewels). Learned from a recipe ITEM
///   (<see cref="ItemCatalog.RecipeBookId"/>) that carries a % (20/40/60/100), and every attempt SPENDS one
///   such item at or below the learned %. The attempt succeeds at the used %, plus the T76/T80 crafter
///   bonus, and every input is scaled by it (<see cref="Crafting.ScaledQty"/>).
///   <see cref="SuccessChance"/> is unused for gear.</item>
/// <item><b>Generic</b> (potions, scrolls, refines). Bought at the Master for <see cref="LearnPrice"/>; no
///   recipe item per craft; always succeeds (step 9b, 2026-09-24); costs <see cref="GoldAt"/> gold a batch.</item>
/// </list>
/// <para><see cref="UnlockLevel"/> is the <see cref="Type"/> LEVEL needed to learn AND to craft it (the
/// crafter-points model, 0.204.0): a recipe whose type level is gone after a respec stays in its slot, LOCKED,
/// and crafts again once the level is back. For <see cref="CraftType.General"/> it reads the generic level.
/// <see cref="LearnLevel"/> is the CHARACTER level (*"i cannot learn T52 rcp @50"*).</para>
/// <para><see cref="BatchValue"/> (Scribe / Apothecary only) is what the batch costs to BUY. The crafter pays
/// <see cref="Crafting.PriceFactor"/> of it in all, the fixed inputs counted at their vendor sell price and
/// gold making up the rest: <see cref="GoldAt"/>.</para>
/// </summary>
public record Recipe(
    string Id,
    CraftType Type,
    string OutputId,
    RecipeInput[] Inputs,
    int OutputQty = 1,
    float SuccessChance = 1f,
    int LearnLevel = 1,
    int UnlockLevel = 0,
    int LearnPrice = 0,
    int GoldCost = 0,
    int GearItemLevel = 0,
    bool QuestOnly = false,
    int BatchValue = 0)
{
    /// <summary>The gold one batch costs a crafter at this type level. A recipe without a
    /// <see cref="BatchValue"/> charges its flat <see cref="GoldCost"/>. A Scribe/Apothecary one charges
    /// <c>PriceFactor(level) × BatchValue</c> minus its inputs at their vendor sell price, rounded to 10 and
    /// never below 10 (*"all need some amount of gold"*).</summary>
    public int GoldAt(int typeLevel)
    {
        if (BatchValue <= 0) return GoldCost;
        double inputs = 0;
        foreach (var i in Inputs)
            if (ItemCatalog.Get(i.ItemId) is ItemDef d) inputs += (double)i.Qty * ItemCatalog.SellPrice(d);
        double gold = Crafting.PriceFactor(typeLevel) * BatchValue - inputs;
        return System.Math.Max(10, (int)System.Math.Round(gold / 10.0) * 10);
    }

    /// <summary>A gear recipe: learned from and spent as a recipe ITEM, and rolled at that item's %.</summary>
    public bool IsGear => GearItemLevel > 0;
}

public static class RecipeCatalog
{
    // ⚠ Set in the EXPLICIT static constructor, NOT an inline initializer. Build() reads the static Cross
    // and Steps tables below; inline field initializers run in TEXTUAL order, so an inline "= Build()" here
    // would run before Cross/Steps exist and NRE (TypeInitializationException on first craft). The explicit
    // static cctor runs AFTER every field initializer, so Cross/Steps are ready. (Found 2026-07-25.)
    private static readonly Dictionary<string, Recipe> _byId;
    static RecipeCatalog() => _byId = Build();

    private static Dictionary<string, Recipe> Build()
    {
        var list = new List<Recipe>();
        list.AddRange(RefinementRecipes());
        list.AddRange(FinishedItemRecipes());
        list.AddRange(ConsumableRecipes());
        list.Add(HammerRecipe());

        var dict = new Dictionary<string, Recipe>();
        foreach (var r in list)
            if (!dict.TryAdd(r.Id, r))
                throw new InvalidOperationException($"Duplicate recipe id '{r.Id}'.");
        return dict;
    }

    /// <summary>An OLD material refine, kept on its 0.203.0 gate until step 10 deletes it: General, so the gate
    /// reads the GENERIC level (refining is open to every crafter), unlocked at 2·(rung − 1).</summary>
    private static Recipe Refine(string id, string output, RecipeInput[] inputs, int oldRung)
    {
        int unlock = System.Math.Clamp(2 * (oldRung - 1), 0, Crafting.MaxCraftLevel);
        return new Recipe(id, CraftType.General, output, inputs,
            LearnLevel: oldRung >= 5 ? 76 : Crafting.CrafterQuestLevel,
            UnlockLevel: unlock, LearnPrice: Crafting.LearnPriceLadder[unlock]);
    }

    /// <summary>The crafter quest's Blacksmith's Hammer (*"at least 20 quest wood and at least 20 iron …
    /// 20 quest gems … 1 hammer head"*). Unscaled: the quest's 40% recipe is a lesson, not a discount.</summary>
    private static Recipe HammerRecipe() => new(
        Crafting.HammerRecipeId, CraftType.General, ItemCatalog.CrafterHammer,
        new[]
        {
            new RecipeInput(ItemCatalog.CrafterQuestWood, 20),
            new RecipeInput(ItemCatalog.CrafterQuestIron, 20),
            new RecipeInput(ItemCatalog.CrafterQuestGem, 20),
            new RecipeInput(ItemCatalog.CrafterHammerHead, 1),
        },
        SuccessChance: Crafting.HammerRecipePercent / 100f,
        LearnLevel: Crafting.CrafterQuestLevel, QuestOnly: true);

    // Each material type upgrades using 5 of itself (one rarity lower) + 2 CROSS mats of two other
    // types (also the lower rarity). Refinement is guaranteed (the 5+2 cost is the gate).
    private static readonly Dictionary<MaterialType, (MaterialType A, MaterialType B)> Cross = new()
    {
        [MaterialType.Gem]     = (MaterialType.Ingot,  MaterialType.Wood),
        [MaterialType.Ingot]   = (MaterialType.Gem,    MaterialType.Leather),
        [MaterialType.Leather] = (MaterialType.Thread, MaterialType.Wood),
        [MaterialType.Thread]  = (MaterialType.Leather, MaterialType.Gem),
        [MaterialType.Wood]    = (MaterialType.Ingot,  MaterialType.Thread),
    };

    private static readonly (ItemRarity Low, ItemRarity High)[] Steps =
    {
        (ItemRarity.Common, ItemRarity.Uncommon),
        (ItemRarity.Uncommon, ItemRarity.Rare),
        (ItemRarity.Rare, ItemRarity.Epic),
        (ItemRarity.Epic, ItemRarity.Legendary),
        (ItemRarity.Legendary, ItemRarity.Mythic),
    };

    /// <summary>The old material refines, now GENERIC recipes: refining into rarity R sat on rung R.
    /// ⚠ Replaced by the Nightsilver / Nightsilk ladder in `BL-273` part 3.</summary>
    private static IEnumerable<Recipe> RefinementRecipes()
    {
        foreach (var type in Crafting.MaterialTypes)
        {
            var (a, b) = Cross[type];
            foreach (var (low, high) in Steps)
                yield return Refine(
                    $"refine_{type}_{high}".ToLowerInvariant(),
                    Crafting.MaterialId(type, high),
                    new[]
                    {
                        new RecipeInput(Crafting.MaterialId(type, low), 5),
                        new RecipeInput(Crafting.MaterialId(a, low), 1),
                        new RecipeInput(Crafting.MaterialId(b, low), 1),
                    },
                    oldRung: (int)high);
        }
    }

    /// <summary>The input-table index a gear tier reads (T40 → 1 … T80 → 5; the old E rung at 0 is gone).</summary>
    private static int GearRung(int itemLevel) => itemLevel switch
    {
        >= 80 => 5,
        >= 76 => 4,
        >= 61 => 3,
        >= 52 => 2,
        >= 40 => 1,
        _ => 0,
    };

    // =====================================================================================
    //  GEAR RECIPE COSTS — the owner's 2026-08-13 target curve, solved against the measured
    //  drop faucet. docs/balance/CraftingMats.md §7 is the measurement; `tools/BalanceMatrix`
    //  §M (M8-M12) is what prints it. DO NOT hand-retune these six numbers — change them,
    //  re-run the tool, read M12.
    // =====================================================================================

    /// <summary>The bulk mat rarity a rung eats — its OWN rung's rarity, except S, which eats Legendary
    /// like A does (the owner's table: *"A-legend(100-200)+1~2mytic, S-legend(1000~2000)+(10~20)mytic"*).
    /// Indexed by crafting level 1-6.</summary>
    private static readonly ItemRarity[] GearBulkRarity =
    {
        ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare,
        ItemRarity.Epic, ItemRarity.Legendary, ItemRarity.Legendary,
    };

    /// <summary>The accent mat — *"a pile of your own rung's mat, plus a few of the rung above"*.</summary>
    private static readonly ItemRarity[] GearAccentRarity =
    {
        ItemRarity.Uncommon, ItemRarity.Rare, ItemRarity.Epic,
        ItemRarity.Legendary, ItemRarity.Mythic, ItemRarity.Mythic,
    };

    /// <summary>Bulk mats for ONE WEAPON craft ATTEMPT at each rung (index = crafting level − 1).
    ///
    /// 🔑 These are SOLVED, not chosen. The owner ruled a target cost per FINISHED weapon —
    /// *"2-3h of farming for E grade per weapon craft, 3-5h per D grade, 5-10 C, 12-1d B, 1-3d A, 7-14d S
    /// … 1d of farming to mean the full 12h (auto+offline)"* — so E 2-3h · D 3-5h · C 5-10h · B 12-24h ·
    /// A 12-36h · S 84-168h. Divide the midpoint by the attempts a success costs
    /// (the old `BL-05` odds table) to get a per-ATTEMPT budget, then buy the owner's own
    /// 100-bulk-to-1-accent shape with it at the measured drop rates.
    ///
    /// ⚠ **Where this disagrees with the ranges he first wrote, the TARGET CURVE won.** Those ranges came
    /// with *"depending on drop rates/amount"* attached — they are an estimate awaiting a measurement — and
    /// the curve is a considered ruling in wall-clock hours. Two rungs moved as a result and both are worth
    /// knowing: **E and D land BELOW his ranges** (300 not 500-1000; 95 not 100-500), because his curve is
    /// ~2.5× cheaper than the one I had proposed and a cheaper target buys a smaller pile. And **S lands at
    /// 490, less than half his 1000-2000**: his own S pile is ~10× A's while his own S target is only ~5× A's,
    /// so the two cannot both hold. B and A land inside his ranges untouched.
    ///
    /// ⚠ B, A and S are only affordable at all because of <c>MobCatalog.EliteMatDrops</c> — before it,
    /// Legendary and Mythic mats dropped from NOTHING and one Legendary cost 467 kills of refining, which
    /// priced an S weapon at 3-6 YEARS. Delete that faucet and these three numbers become fiction.</summary>
    private static readonly int[] GearBulk = { 90, 75, 8, 147, 154, 1450 };

    /// <summary>Accent mats for one weapon attempt — the owner's shape is 100 bulk : 1 accent, and this
    /// is that ratio rounded to whole mats at each rung.</summary>
    private static readonly int[] GearAccent = { 1, 1, 1, 1, 1, 14 };

    /// <summary>What one gear SLOT costs as a fraction of one weapon (owner, 2026-08-13), authored so a
    /// full armor set and a full jewel set each come to **exactly one weapon**:
    /// <code>
    /// armor   gloves WH/10  boots WH/10  helmet WH/3.33  body WH/2                  = 1.000
    /// jewels  ring   WH/10  earring WH/5 necklace WH/2.5   (2 rings + 2 earrings)   = 1.000
    /// </code>
    /// Both sums check to 1.000 against the real <c>ArmorSlot</c> and <c>JewelType</c> slot counts, so a
    /// fully geared character is **3 weapons** — and at S that is 378 farm hours, which is the number to
    /// sanity-check rather than any per-item one.
    ///
    /// 🔑 **The SHIELD was the one slot his fractions missed**, because it is its own
    /// <see cref="EquipSlot.Shield"/> and sits outside both sums. His ruling (2026-08-13): *"It's armor so
    /// make it as a helmet price"* → WH/3.33. Note the consequence, which is intended and not a rounding
    /// slip: a shield user's kit is 1.30 weapons of armor, not 1.00, because the shield is a real extra
    /// slot with real stats and nothing else gives way to pay for it.</summary>
    private static float SlotFraction(ItemDef d) => d.Slot switch
    {
        EquipSlot.Weapon => 1f,
        EquipSlot.Shield => 1f / 3.33f,          // = a helmet (owner)
        EquipSlot.Armor  => d.ArmorSlot switch
        {
            ArmorSlot.Body => 1f / 2f,
            ArmorSlot.Head => 1f / 3.33f,
            _              => 1f / 10f,          // gloves, boots
        },
        EquipSlot.Jewel  => d.JewelType switch
        {
            JewelType.Necklace => 1f / 2.5f,
            JewelType.Earring  => 1f / 5f,
            _                  => 1f / 10f,      // ring
        },
        _ => 0.5f
    };

    private static (MaterialType Type, float Frac)[] Composition(ItemDef d)
    {
        switch (d.Slot)
        {
            case EquipSlot.Weapon:
                return new[] { (MaterialType.Ingot, 0.6f), (MaterialType.Gem, 0.2f), (MaterialType.Wood, 0.2f) };
            case EquipSlot.Jewel:
                return new[] { (MaterialType.Gem, 0.6f), (MaterialType.Ingot, 0.2f), (MaterialType.Leather, 0.2f) };
            case EquipSlot.Shield:
                return new[] { (MaterialType.Ingot, 0.6f), (MaterialType.Leather, 0.2f), (MaterialType.Gem, 0.2f) };
            case EquipSlot.Armor when d.ArmorSlot == ArmorSlot.Body:
                return d.Weight switch
                {
                    ArmorWeight.Heavy => new[] { (MaterialType.Ingot, 0.5f), (MaterialType.Leather, 0.2f), (MaterialType.Thread, 0.2f), (MaterialType.Gem, 0.1f) },
                    ArmorWeight.Robe  => new[] { (MaterialType.Thread, 0.5f), (MaterialType.Ingot, 0.2f), (MaterialType.Leather, 0.2f), (MaterialType.Gem, 0.1f) },
                    _                 => new[] { (MaterialType.Leather, 0.5f), (MaterialType.Ingot, 0.2f), (MaterialType.Thread, 0.2f), (MaterialType.Gem, 0.1f) },
                };
            case EquipSlot.Armor:   // weightless accessories (helm/gloves/boots)
                return new[] { (MaterialType.Leather, 0.4f), (MaterialType.Ingot, 0.3f), (MaterialType.Thread, 0.2f), (MaterialType.Gem, 0.1f) };
            default:
                return System.Array.Empty<(MaterialType, float)>();
        }
    }

    private static IEnumerable<Recipe> FinishedItemRecipes()
    {
        foreach (var d in ItemCatalog.AllItems)
        {
            if (d.ItemLevel <= 0) continue;                // only the tiered gear
            // Only the AUTHORED set piece is craftable; the derived quality copies are drop-only. That
            // authored piece used to be the Epic rung and is now the MYTHIC one (the ladder re-anchored
            // so the authored number is the ceiling rather than a 70% mid-point) — this filter is how
            // "the real item" is identified, so it had to move with it. Leaving it on Epic silently
            // produced ZERO craftable recipes, which the SmokeTest caught as RecipeCatalog returning
            // null for a known id.
            if (d.Rarity != ItemRarity.Mythic) continue;
            if (!Crafting.IsGearSlot(d.Slot)) continue;
            // F AND E GEAR ARE NOT CRAFTED (`BL-273` part 2): the rework's recipe tables start at T40. The
            // input tables below are still the `BL-05` ones, read at T40..T80 until part 3 replaces the mats.
            if (d.ItemLevel < Crafting.MinCraftedGearLevel) continue;
            int rung = GearRung(d.ItemLevel);

            float slot = SlotFraction(d);
            int bulk   = System.Math.Max(1, (int)System.Math.Round(GearBulk[rung] * slot));
            // The accent ROUNDS DOWN and may reach zero, and that is deliberate: a ring is a tenth of a
            // weapon, so at rungs where the weapon takes a single accent mat the ring genuinely takes
            // none. Flooring it up to 1 instead would have made the smallest slots the most expensive
            // per point of stat — a Mythic accent mat is 44 farm hours by itself at the top.
            int accent = (int)(GearAccent[rung] * slot);
            var bulkR   = GearBulkRarity[rung];
            var accentR = GearAccentRarity[rung];

            var inputs = new List<RecipeInput>();
            var comp = Composition(d);
            foreach (var (type, frac) in comp)
                inputs.Add(new RecipeInput(Crafting.MaterialId(type, bulkR),
                                           System.Math.Max(1, (int)System.Math.Round(bulk * frac))));
            // ⚠ The accent goes ENTIRELY on the dominant material, never split across the composition.
            // Splitting it was the old behaviour and it was a silent multiplier: `Max(1, accent * frac)`
            // turned "1 accent mat" into one PER TYPE, so a four-material body paid four Legendary mats
            // where the recipe said one — and at the top rungs a single mat is hours of farming.
            // Composition() lists the dominant material FIRST at every slot, so comp[0] is it.
            if (accent > 0 && comp.Length > 0)
                inputs.Add(new RecipeInput(Crafting.MaterialId(comp[0].Type, accentR), accent));

            // The success % is the recipe ITEM's, not the recipe's (see Recipe): SuccessChance is unused here.
            yield return new Recipe(
                $"craft_{d.Id}", Crafting.TypeOf(d), d.Id, inputs.ToArray(),
                LearnLevel: d.ItemLevel, UnlockLevel: Crafting.TierGate(d.ItemLevel), GearItemLevel: d.ItemLevel);
        }
    }

    // =====================================================================================
    //  STEP 9b — THE GENERIC-RECIPE TABLE (owner, 2026-09-24; design doc §2.2 "Your answers" and
    //  "The crafter-points model"). Every number below is AUTHORED, one literal per row.
    //
    //  • TYPE LEVEL (UnlockLevel): commons are open to every crafter at L0 (*"all can do T40 crafts + common
    //    scrolls common buff pots and common regen pots"*). An uncommon needs the type and its tier's gate:
    //    T40 L1 · T52 L2 · T61 L4 (the smiths' gate, and L1 at T40 because the type must be learned at all).
    //    Runes 1h L7 / 2h L10; rare HP L7 / rare MP L10 (*"apoth … adds rare pots at the end"*).
    //  • A BUFF's tier is its class skill's LAST rung (*"if a skill is learned at 40 but last lvl is at 52
    //    that s T52"*), measured 2026-09-24 over every class table: T40 = Might, Bulwark, Alacrity, Swift;
    //    T52 = Aim, Force, Ward, Fury, Agility, Focus, Ferocity, Frenzy, Vigor, Serenity; T61 = Body, Soul,
    //    Resolve, Insight, Vampirism. The tier sets the character level, the gate and the essence (D/C/B).
    //  • PRICE: BatchValue is what the batch costs to buy (shelf price; HP/MP 60/120 · 250/500 · 5000/10000,
    //    buff potions 1,500 / 5,000, buff scrolls 36,000, rune boxes 150k / 280k). The crafter pays
    //    ×0.9 → ×0.55 of it by type level (Crafting.PriceFactor); essence and mats are fixed, gold floats.
    //  • LEARN PRICE: the ladder of the rung each line was ruled on (Crafting.LearnPriceLadder).
    //  • OUT, by his ruling: stones, Return/Resurrection (+ Ultimates), every Dash, the Instant potion, and
    //    enchant + attribute scrolls (never craftable).
    // =====================================================================================
    private static IEnumerable<Recipe> ConsumableRecipes()
    {
        static RecipeInput M(MaterialType t, int n) => new(Crafting.MaterialId(t, ItemRarity.Common), n);
        static RecipeInput E(int grade, int n) => new(Crafting.EssenceIds[grade], n);
        static RecipeInput V(string id, int n) => new(id, n);

        Recipe R(CraftType type, string output, int qty, int charLevel, int gate, int ladder, int batchValue,
                 params RecipeInput[] inputs) =>
            new($"craft_{output}", type, output, inputs,
                OutputQty: qty, LearnLevel: charLevel, UnlockLevel: gate,
                LearnPrice: Crafting.LearnPriceLadder[ladder], BatchValue: batchValue);

        const CraftType Apo = CraftType.Apothecary, Scr = CraftType.Scribe;
        const int Gem = 0, Wood = 1, Iron = 2, Leather = 3;
        static RecipeInput Mat(int k, int n) => M(k switch
        {
            0 => MaterialType.Gem, 1 => MaterialType.Wood, 2 => MaterialType.Ingot, _ => MaterialType.Leather,
        }, n);

        // ---- HP / MP ------------------------------------------------------------------------------------
        //                                   batch  char gate ladder  batch value    inputs
        yield return R(Apo, ItemCatalog.MinorPotion,       100, 40, 0, 0,   6_000,   Mat(Gem, 5),  E(0, 1));
        yield return R(Apo, ItemCatalog.MinorManaPotion,   100, 40, 0, 0,  12_000,   Mat(Gem, 10), E(0, 2));
        yield return R(Apo, ItemCatalog.HealingPotion,      50, 52, 2, 2,  12_500,   Mat(Gem, 10), E(1, 1));
        yield return R(Apo, ItemCatalog.ManaPotion,         50, 52, 2, 2,  25_000,   Mat(Gem, 20), E(1, 2));
        // Rare: *"they are not rly sold anywhere so crafting is the only way"*. Unreachable until the volcanic
        // ash/stone drop in step 11.
        yield return R(Apo, ItemCatalog.GreaterPotion,      10, 76, 7, 5,  50_000,   Mat(Gem, 10), E(2, 1),
                       V(ItemCatalog.VolcanicAsh, 1), V(ItemCatalog.VolcanicStone, 1));
        yield return R(Apo, ItemCatalog.GreaterManaPotion,  10, 76, 10, 5, 100_000,  Mat(Gem, 20), E(2, 2),
                       V(ItemCatalog.VolcanicAsh, 2), V(ItemCatalog.VolcanicStone, 2));

        // ---- BUFF POTIONS: x6. Common = gems + wood, open to all (ladder L1); Uncommon adds the tier's
        //      essence behind the Apothecary gate (ladder L3). (common, uncommon, the family's tier)
        foreach (var (c, u, tier) in new[]
        {
            (ItemCatalog.MightPotionC,   ItemCatalog.MightPotionU,   40),
            (ItemCatalog.BulwarkPotionC, ItemCatalog.BulwarkPotionU, 40),
            (ItemCatalog.CastPotionC,    ItemCatalog.CastPotionU,    40),
            (ItemCatalog.SpeedPotionC,   ItemCatalog.SpeedPotionU,   40),
            (ItemCatalog.AimPotionC,     ItemCatalog.AimPotionU,     52),
            (ItemCatalog.ForcePotionC,   ItemCatalog.ForcePotionU,   52),
            (ItemCatalog.WardPotionC,    ItemCatalog.WardPotionU,    52),
            (ItemCatalog.AtkPotionC,     ItemCatalog.AtkPotionU,     52),
            (ItemCatalog.EvaPotionC,     ItemCatalog.EvaPotionU,     52),
        })
        {
            yield return R(Apo, c, 6, tier, 0, 1, 9_000, Mat(Gem, 3), Mat(Wood, 3));
            yield return tier >= 52
                ? R(Apo, u, 6, tier, 2, 3, 30_000, Mat(Gem, 5), Mat(Wood, 5), E(1, 1))
                : R(Apo, u, 6, tier, 1, 3, 30_000, Mat(Gem, 5), Mat(Wood, 5), E(0, 2));
        }

        // ---- BUFF SCROLLS: x2, leather + iron. The nine "basic" ones count as the common line (open to
        //      all, ladder L2); the ten "other" ones are the uncommon line: the Scribe gate + the tier's
        //      essence (ladder L4).
        foreach (var (id, tier) in new[]
        {
            (ItemCatalog.MightScrollR, 40), (ItemCatalog.BulwarkScrollR, 40), (ItemCatalog.CastScrollR, 40),
            (ItemCatalog.SpeedScrollR, 40), (ItemCatalog.AimScrollR, 52), (ItemCatalog.ForceScrollR, 52),
            (ItemCatalog.WardScrollR, 52), (ItemCatalog.AtkScrollR, 52), (ItemCatalog.EvaScrollR, 52),
        })
            yield return R(Scr, id, 2, tier, 0, 2, 72_000, Mat(Leather, 3), Mat(Iron, 3));
        foreach (var (id, tier) in new[]
        {
            (ItemCatalog.FocusScrollM, 52), (ItemCatalog.FerocityScrollM, 52), (ItemCatalog.FrenzyScrollM, 52),
            (ItemCatalog.VigorScrollM, 52), (ItemCatalog.SerenityScrollM, 52),
            (ItemCatalog.BodyScrollM, 61), (ItemCatalog.SoulScrollM, 61), (ItemCatalog.ResolveScrollM, 61),
            (ItemCatalog.InsightScrollM, 61), (ItemCatalog.VampScrollM, 61),
        })
            yield return tier >= 61
                ? R(Scr, id, 2, tier, 4, 4, 72_000, Mat(Leather, 5), Mat(Iron, 5), E(2, 2))
                : R(Scr, id, 2, tier, 2, 4, 72_000, Mat(Leather, 5), Mat(Iron, 5), E(1, 2));

        // ---- RUNE BOXES: x3 (*"1h from shop cost 450k … 2h x3 shop cost 840"*). The 2h is unreachable until
        //      the Volcanic Bar refine (step 10) and its ash/stone (step 11) exist.
        foreach (var box in new[] { ItemCatalog.BoxWarRune1h, ItemCatalog.BoxSpellRune1h })
            yield return R(Scr, box, 3, 70, 7, 7, 450_000, Mat(Iron, 10), Mat(Gem, 10), Mat(Wood, 10), E(2, 8));
        foreach (var box in new[] { ItemCatalog.BoxWarRune2h, ItemCatalog.BoxSpellRune2h })
            yield return R(Scr, box, 3, 80, 10, 10, 840_000, V(ItemCatalog.VolcanicBar, 2), E(4, 4));
    }

    public static Recipe? Get(string id) => id is null ? null : _byId.GetValueOrDefault(id);
    public static IEnumerable<Recipe> All => _byId.Values;
    /// <summary>The recipes the Master SELLS to learn (generic, not the quest's own): by type, then gate,
    /// then character level.</summary>
    public static IEnumerable<Recipe> GenericForSale => _byId.Values
        .Where(r => !r.IsGear && !r.QuestOnly)
        .OrderBy(r => r.Type).ThenBy(r => r.UnlockLevel).ThenBy(r => r.LearnLevel).ThenBy(r => r.Id);
}
