namespace Game.Shared;

/// <summary>One ingredient of a recipe: an item id (material or component) and a quantity.</summary>
public record RecipeInput(string ItemId, int Qty);

/// <summary>
/// A crafting recipe: a <see cref="Profession"/> turns <see cref="Inputs"/> into
/// <see cref="OutputId"/> ×<see cref="OutputQty"/>, succeeding with <see cref="SuccessChance"/>
/// (a failed craft consumes the mats — the risk). Auto-known once the crafter's char level reaches
/// <see cref="LearnLevel"/>, UNLESS <see cref="DropOnly"/> (the recipe itself must be found/bought,
/// e.g. the A-grade sets). See docs/design/Crafting.md.
///
/// <para><see cref="CraftLevel"/> is the CRAFTING level rung (1-6, `BL-05`) — a second, independent gate
/// from <see cref="LearnLevel"/>, which stays a CHARACTER level. Both must be satisfied: the owner's rule
/// is *"crafts need char level + crafting lvl"*. It is never authored per recipe; it is derived from the
/// output's own rarity in <see cref="RecipeCatalog"/>, so a recipe cannot be filed under a rung that
/// disagrees with what it makes.</para>
/// </summary>
public record Recipe(
    string Id,
    Profession Profession,
    string OutputId,
    RecipeInput[] Inputs,
    int OutputQty = 1,
    float SuccessChance = 1f,
    int LearnLevel = 1,
    bool DropOnly = false,
    int CraftLevel = 1);

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

        var dict = new Dictionary<string, Recipe>();
        foreach (var r in list)
            if (!dict.TryAdd(r.Id, r with { CraftLevel = DeriveCraftLevel(r) }))
                throw new InvalidOperationException($"Duplicate recipe id '{r.Id}'.");
        return dict;
    }

    /// <summary>The crafting-level rung a recipe belongs to, read off WHAT IT MAKES (`BL-05`).
    ///
    /// 🔑 Derived in one place rather than authored on 173 recipes, because the rung and the output are
    /// the same fact stated twice, and the moment they can disagree the ladder stops meaning anything.
    /// The two cases are genuinely different, and the owner's rule names both:
    /// *"at level N you craft goods of rarity N-1, and refine up to rarity N"*.
    ///   • A MATERIAL output is a refine — refining INTO rarity R is level R (L1 makes Uncommon mats).
    ///   • Anything else is goods — making rarity R is level R+1 (L2 makes Uncommon potions).
    /// An unknown output id falls back to L1 rather than throwing: the catalogs are cross-checked at
    /// boot, and a startup crash inside a static constructor is the least debuggable failure there is.</summary>
    private static int DeriveCraftLevel(Recipe r) =>
        ItemCatalog.Get(r.OutputId) is not { } def ? 1
        : def.Slot == EquipSlot.Material ? (int)def.Rarity
        : Crafting.CraftLevelOf(def.Rarity);

    // Each material type upgrades using 5 of itself (one rarity lower) + 2 CROSS mats from two
    // DIFFERENT professions' types (also the lower rarity) → forces trade. Refinement is guaranteed
    // (the 5+2 cost is the gate); it's known once the crafter reaches the rarity's level gate.
    private static readonly Dictionary<MaterialType, (MaterialType A, MaterialType B)> Cross = new()
    {
        [MaterialType.Gem]     = (MaterialType.Ingot,  MaterialType.Wood),
        [MaterialType.Ingot]   = (MaterialType.Gem,    MaterialType.Leather),
        [MaterialType.Leather] = (MaterialType.Thread, MaterialType.Wood),
        [MaterialType.Thread]  = (MaterialType.Leather, MaterialType.Gem),
        [MaterialType.Wood]    = (MaterialType.Ingot,  MaterialType.Thread),
    };

    /// <summary>⚠ The Legendary→Mythic rung was added 2026-08-12 with `BL-05` — the material ladder now
    /// runs the full six rarities so the top crafting level has mats of its own rung to eat. See
    /// Crafting.MaterialRarities.</summary>
    private static readonly (ItemRarity Low, ItemRarity High)[] Steps =
    {
        (ItemRarity.Common, ItemRarity.Uncommon),
        (ItemRarity.Uncommon, ItemRarity.Rare),
        (ItemRarity.Rare, ItemRarity.Epic),
        (ItemRarity.Epic, ItemRarity.Legendary),
        (ItemRarity.Legendary, ItemRarity.Mythic),
    };

    /// <summary>Char level a crafter can refine INTO a given rarity.
    ///
    /// 🔑 Derived from the CRAFTING level that refines into this rarity (`BL-05`), not hand-listed, so
    /// the character gate and the crafting gate cannot drift apart — the owner's rule is *"crafts need
    /// char level + crafting lvl"*, and two independently-authored ladders is exactly how that becomes a
    /// lie. Refining into rarity R is crafting level R (L1 makes Uncommon), and each crafting level
    /// carries its own character floor of 20/40/76.
    ///
    /// ⚠ This MOVED the Epic and Legendary rungs: they were hand-set to 61 and 76 and are now 40 and 40,
    /// because refining into Epic is L3 and into Legendary is L4, and both of those sit in the 40+3rd-class
    /// band. The real gate did not loosen — a level-40 character still has to have *reached* L3 and L4,
    /// which is 1800 and 3600 crafting exp away.</summary>
    private static int RefineLearnLevel(ItemRarity high) =>
        Crafting.CharLevelFor((int)high);

    private static IEnumerable<Recipe> RefinementRecipes()
    {
        foreach (var type in Crafting.MaterialTypes)
        {
            var (a, b) = Cross[type];
            foreach (var (low, high) in Steps)
                yield return new Recipe(
                    $"refine_{type}_{high}".ToLowerInvariant(),
                    Crafting.RefinerOf(type),
                    Crafting.MaterialId(type, high),
                    new[]
                    {
                        new RecipeInput(Crafting.MaterialId(type, low), 5),
                        new RecipeInput(Crafting.MaterialId(a, low), 1),
                        new RecipeInput(Crafting.MaterialId(b, low), 1),
                    },
                    SuccessChance: 1f,
                    LearnLevel: RefineLearnLevel(high));
        }
    }

    // ----- Finished-item (Epic SET) recipes: each tiered gear piece is craftable from mats. Cost =
    //  a BULK rarity (×2) + an ACCENT rarity (one higher), scaled by grade × slot, split by the item's
    //  material composition. Reproduces the owner's E-body anchor (100 common 50/20/20/10 + 50 unc
    //  25/10/10/5 ingot/leather/thread/gem). Sets = 100% success; A-grade (76) recipe is DropOnly. -----
    private static ItemRarity BulkRarity(int lvl) =>
        lvl >= 76 ? ItemRarity.Epic : lvl >= 52 ? ItemRarity.Rare : lvl >= 40 ? ItemRarity.Uncommon : ItemRarity.Common;
    private static ItemRarity AccentRarity(int lvl) =>
        lvl >= 76 ? ItemRarity.Legendary : lvl >= 52 ? ItemRarity.Epic : lvl >= 40 ? ItemRarity.Rare : ItemRarity.Uncommon;
    private static float GradeMult(int lvl) =>
        lvl >= 76 ? 2.8f : lvl >= 61 ? 2.2f : lvl >= 52 ? 1.7f : lvl >= 40 ? 1.3f : 1.0f;

    private static float SlotWeight(ItemDef d) => d.Slot switch
    {
        EquipSlot.Weapon => 1.0f,
        EquipSlot.Shield => 0.5f,
        EquipSlot.Jewel  => d.JewelType == JewelType.Necklace ? 0.5f : 0.3f,
        EquipSlot.Armor  => d.ArmorSlot switch { ArmorSlot.Body => 1.0f, ArmorSlot.Head => 0.5f, _ => 0.35f },
        _ => 0.5f
    };

    private static Profession ProfOf(ItemDef d) => d.Slot switch
    {
        EquipSlot.Weapon => Profession.WeaponSmith,
        EquipSlot.Armor or EquipSlot.Shield => Profession.ArmorSmith,
        EquipSlot.Jewel => Profession.Jeweler,
        _ => Profession.None
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
            if (d.Slot is not (EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield or EquipSlot.Jewel)) continue;
            var prof = ProfOf(d);
            if (prof == Profession.None) continue;

            int bulk = System.Math.Max(1, (int)(100 * SlotWeight(d) * GradeMult(d.ItemLevel)));
            int accent = System.Math.Max(1, bulk / 2);
            var bulkR = BulkRarity(d.ItemLevel);
            var accentR = AccentRarity(d.ItemLevel);

            var inputs = new List<RecipeInput>();
            foreach (var (type, frac) in Composition(d))
            {
                inputs.Add(new RecipeInput(Crafting.MaterialId(type, bulkR), System.Math.Max(1, (int)(bulk * frac))));
                inputs.Add(new RecipeInput(Crafting.MaterialId(type, accentR), System.Math.Max(1, (int)(accent * frac))));
            }

            yield return new Recipe(
                $"craft_{d.Id}", prof, d.Id, inputs.ToArray(),
                SuccessChance: 1f,                 // Epic set = guaranteed; the mats are the gate
                LearnLevel: d.ItemLevel,
                DropOnly: d.ItemLevel >= 76);      // A-grade recipes come from bosses/trade
        }
    }

    // ----- Consumable recipes: Potion Master (Wood+Thread+Gem) + Scroll Scribe (Thread+Wood+Gem).
    //  Cheaper than gear, produce a small stack. Rounds out all 5 professions crafting something. -----
    private static IEnumerable<Recipe> ConsumableRecipes()
    {
        // 🔴 `BL-40` — THE INPUT RARITY IS NOT AUTHORED ANY MORE. It is read off the OUTPUT item, so a
        // recipe cannot be cheaper than the thing it makes. (2026-08-12.)
        //
        // The bug this closes, in his words: *"A lvl 30 Potion Crafter had crafted 450 uncommon potions …
        // A lvl 30 Scroll crafter had crafted 690 uncommon attri scrolls."* Every consumable recipe except
        // the ten buff potions was authored EXACTLY ONE RARITY RUNG TOO CHEAP — `potion_healing` is an
        // Uncommon item and took Common mats; `attrscroll_legendary` is Legendary and took Rare, two rungs
        // off. Refining costs 7-in-1-out, so a rung is a 7× subsidy (49× for the two-rung one): one common
        // mat became one uncommon potion, while the refiner beside him paid seven for a single uncommon
        // MAT. Nothing was looping and nothing was batching — the ratio was simply wrong, everywhere.
        //
        // Deriving it also makes the fix permanent. The old signature invited the mistake by asking the
        // author to restate a rarity the item already knows, and it was got wrong 8 times out of 18.
        static ItemRarity RarityOf(string itemId) => ItemCatalog.Get(itemId)?.Rarity ?? ItemRarity.Common;

        Recipe Potion(string output, int lvl, int qty)
        {
            var r = RarityOf(output);
            return new($"craft_{output}", Profession.PotionMaster, output,
                new[]
                {
                    new RecipeInput(Crafting.MaterialId(MaterialType.Wood, r), 3),
                    new RecipeInput(Crafting.MaterialId(MaterialType.Thread, r), 1),
                    new RecipeInput(Crafting.MaterialId(MaterialType.Gem, r), 1),
                },
                OutputQty: qty, SuccessChance: 0.9f, LearnLevel: lvl);
        }

        Recipe Scroll(string output, int lvl, int qty)
        {
            var r = RarityOf(output);
            return new($"craft_{output}", Profession.ScrollScribe, output,
                new[]
                {
                    new RecipeInput(Crafting.MaterialId(MaterialType.Thread, r), 3),
                    new RecipeInput(Crafting.MaterialId(MaterialType.Wood, r), 1),
                    new RecipeInput(Crafting.MaterialId(MaterialType.Gem, r), 1),
                },
                OutputQty: qty, SuccessChance: 0.8f, LearnLevel: lvl);
        }

        // ---- `BL-57` — the L1 recipe these professions need to be startable at all. ----
        // *"Two professions can craft NOTHING until level 20"* → *"and my luck i picked exactly those :)"*.
        // Under the crafting ladder this stopped being cosmetic: L1 is where you earn your way to L2, so
        // without a COMMON recipe a fresh Apothecary or Scribe can never leave L1 at all.
        //
        // The Potion Master's is free of any conflict: `potion_minor` ("Common Healing Potion") is the
        // entry rung of a line he already makes the other two rungs of.
        yield return Potion(ItemCatalog.MinorPotion, 20, 5);
        //
        // 🔵 THE SCRIBE'S L1 IS AN OPEN QUESTION FOR THE OWNER — deliberately not authored here, because
        // both candidates collide with a ruling he has already made:
        //   • `attrscroll_common` — reserved as drop-only on purpose, see the note below: *"the attribute
        //     economy has a faucet the scribe can't flood."*
        //   • `scroll_common` (the E-band normal enchant scroll) — fits the scribe's existing line
        //     perfectly, but it is the exact item whose drop rate `62j` cut **30×** for flooding him with
        //     80 scrolls by level 28. Handing him a craft for it at ~1 common mat per scroll would undo
        //     that ruling from the other end, three builds after he made it.
        // Until he rules, the SCROLL SCRIBE CANNOT LEAVE L1. That is worse than the bug `BL-57` reported,
        // so it must not ship un-answered — it is on the checklist as a blocker, not a nicety.

        yield return Potion(ItemCatalog.HealingPotion, 20, 5);
        yield return Potion(ItemCatalog.GreaterPotion, 40, 5);
        yield return Potion(ItemCatalog.SpeedPotionU, 30, 3);
        yield return Potion(ItemCatalog.CastPotionU, 30, 3);
        yield return Potion(ItemCatalog.AtkPotionU, 30, 3);
        yield return Potion(ItemCatalog.EvaPotionU, 30, 3);
        yield return Potion(ItemCatalog.MightPotionU, 30, 3);
        yield return Potion(ItemCatalog.BulwarkPotionU, 30, 3);
        yield return Potion(ItemCatalog.ForcePotionU, 30, 3);
        yield return Potion(ItemCatalog.WardPotionU, 30, 3);
        yield return Potion(ItemCatalog.AimPotionU, 30, 3);
        yield return Potion(ItemCatalog.DashPotionU, 30, 3);
        // ⚠ NO buff-scroll recipes (playtest-17 E3, 2026-08-05). The scribe used to make the Common and
        // Uncommon rungs of nine families — 18 recipes for items that no longer exist. A buff scroll is
        // now one per buff, top rung, and comes out of the Apothecary's Blessing Box or nowhere; a
        // craftable scroll would be a second faucet on the one consumable he asked to have exactly one.
        // The scribe keeps the enchant + attribute scrolls, which is the trade he actually gates.
        // Enchant scrolls: the scribe makes the NORMAL (item-breaks) scroll of the D and C bands and
        // nothing else. The craft level is the band's own floor now (0.49.0) — a D scroll works on
        // level-40 gear, so letting a level-20 scribe stock them served nobody. Greater and Safe are
        // never craftable: they are the elite/boss reward that makes an enchant worth attempting.
        yield return Scroll(ItemCatalog.ScrollNormalD, 40, 5);
        yield return Scroll(ItemCatalog.ScrollNormalC, 52, 5);
        // Attribute scrolls: the D-C-B pair is craftable early, the A-grade pair late. The
        // Common (entry) scroll and the S-grade Mythic are deliberately NOT craftable — those
        // stay a drop, so the attribute economy has a faucet the scribe can't flood.
        yield return Scroll(ItemCatalog.AttrScrollUncommon, 20, 3);
        yield return Scroll(ItemCatalog.AttrScrollRare, 40, 3);
        yield return Scroll(ItemCatalog.AttrScrollEpic, 76, 3);
        yield return Scroll(ItemCatalog.AttrScrollLegendary, 76, 3);
    }

    public static Recipe? Get(string id) => id is null ? null : _byId.GetValueOrDefault(id);
    public static IEnumerable<Recipe> All => _byId.Values;
    public static IEnumerable<Recipe> ForProfession(Profession p) => _byId.Values.Where(r => r.Profession == p);
}
