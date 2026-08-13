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
/// is *"crafts need char level + crafting lvl"*.
///
/// **Leave it 0 and it is DERIVED** from what the recipe makes (a material's own rarity, a gear piece's
/// GRADE), which is how all 173 mat/gear recipes are filed — a recipe cannot then sit under a rung that
/// disagrees with its output. A non-zero value is an AUTHORED rung, and exactly two things use that: the
/// Scroll Scribe's and the Potion Master's ladders, which the owner deliberately offset from rarity
/// (§5d — his Scribe's L1 is *"nothing gear related"*, and his Potion Master alternates HP and buff lines
/// on a two-rung stride). For those two the rung IS the design and cannot be read off the output.</para>
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
    int CraftLevel = 0);

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
            if (!dict.TryAdd(r.Id, r.CraftLevel > 0 ? r : r with { CraftLevel = DeriveCraftLevel(r) }))
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
    ///   • GEAR is filed by its GRADE, not its rarity (owner, 2026-08-13: *"just the idea is grade based
    ///     not as much as rarity based"*). This is not a nuance — every craftable gear recipe outputs the
    ///     authored MYTHIC piece, so reading the rung off rarity filed all 135 of them at L6 and left a
    ///     fresh smith able to reach 2 recipes out of 67. Grade spreads them E→S across L1→L6 exactly.
    ///   • Anything else is goods — making rarity R is level R+1 (L2 makes Uncommon potions).
    /// An unknown output id falls back to L1 rather than throwing: the catalogs are cross-checked at
    /// boot, and a startup crash inside a static constructor is the least debuggable failure there is.</summary>
    private static int DeriveCraftLevel(Recipe r) =>
        ItemCatalog.Get(r.OutputId) is not { } def ? 1
        : def.Slot == EquipSlot.Material ? (int)def.Rarity
        : Crafting.IsGearSlot(def.Slot) ? Math.Max(1, Crafting.GearCraftLevel(def.ItemLevel))
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
    /// (<see cref="Crafting.GearCraftOdds"/>) to get a per-ATTEMPT budget, then buy the owner's own
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
            if (!Crafting.IsGearSlot(d.Slot)) continue;
            var prof = ProfOf(d);
            if (prof == Profession.None) continue;

            // F GEAR IS NOT CRAFTABLE (owner, 2026-08-13): *"rly no point to craft F grade … its mostly
            // to get you to 20 (as u get free mytic @10/15)"*. It is also what makes the ladder exact —
            // seven grades minus F is six, against six crafting rungs.
            int rung = Crafting.GearCraftLevel(d.ItemLevel);
            if (rung <= 0) continue;

            float slot = SlotFraction(d);
            int bulk   = System.Math.Max(1, (int)System.Math.Round(GearBulk[rung - 1] * slot));
            // The accent ROUNDS DOWN and may reach zero, and that is deliberate: a ring is a tenth of a
            // weapon, so at rungs where the weapon takes a single accent mat the ring genuinely takes
            // none. Flooring it up to 1 instead would have made the smallest slots the most expensive
            // per point of stat — a Mythic accent mat is 44 farm hours by itself at the top.
            int accent = (int)(GearAccent[rung - 1] * slot);
            var bulkR   = GearBulkRarity[rung - 1];
            var accentR = GearAccentRarity[rung - 1];

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

            yield return new Recipe(
                $"craft_{d.Id}", prof, d.Id, inputs.ToArray(),
                // ⚠ NOT the roll the server makes. A gear craft is three-way — Mythic / Legendary / fail
                // (Crafting.GearCraftOdds) — and HandleCraft rolls that table directly. This field is the
                // chance the attempt yields ANYTHING, which is what the recipe list shows the player and
                // what every non-gear recipe means by SuccessChance.
                SuccessChance: 1f - Crafting.GearCraftOdds(rung).Fail,
                LearnLevel: d.ItemLevel,
                DropOnly: d.ItemLevel >= 76);      // A/S recipes come from bosses/trade
        }
    }

    // =====================================================================================
    //  CONSUMABLE recipes — the Potion Master's and the Scroll Scribe's ladders, as the owner
    //  authored them on 2026-08-13 (docs/design/CraftingProfessions.md §5d).
    //
    //  🔑 These two are the ONLY recipes in the game with an AUTHORED crafting rung. Everything
    //  else derives its rung from what it makes; these cannot, because he deliberately offset
    //  both ladders from rarity:
    //    • the Scribe's L1 is *"nothing gear related"*, which pushes his gear service to D on L2
    //      and lets five grades D→S fill five rungs L2→L6 exactly;
    //    • the Potion Master alternates an HP line and a buff line on a TWO-rung stride, so his
    //      Common buff potions sit at L2 while his Common HP potion sits at L1 — the same rarity
    //      on two different rungs, which no derivation can express.
    //  The rung IS the design here, so it is passed in and the derivation is skipped.
    // =====================================================================================
    private static IEnumerable<Recipe> ConsumableRecipes()
    {
        // 🔴 `BL-40` — THE INPUT RARITY IS NOT AUTHORED. It is read off the OUTPUT item, so a recipe
        // cannot be cheaper than the thing it makes. (2026-08-12.)
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
        //
        // ⚠ The INPUT rarity is still the OUTPUT's own rarity — it is only the RUNG that is authored.
        // A Common buff potion filed at L2 is still made of Common mats: moving it up a rung is a
        // statement about who may make it, not a licence to charge Uncommon mats for a Common good.
        static ItemRarity RarityOf(string itemId) => ItemCatalog.Get(itemId)?.Rarity ?? ItemRarity.Common;

        // The CHARACTER-level floor a crafting rung carries anyway (20 / 20 / 40 / 40 / 76 / 76). Used as
        // the consumables' LearnLevel so the two gates cannot drift: hand-listing a character level beside
        // an authored rung is exactly how the refine ladder went wrong before it was derived.
        static int CharFloor(int rung) => Crafting.CharLevelFor(rung);

        Recipe Potion(string output, int rung, int qty)
        {
            var r = RarityOf(output);
            return new($"craft_{output}", Profession.PotionMaster, output,
                new[]
                {
                    new RecipeInput(Crafting.MaterialId(MaterialType.Wood, r), 3),
                    new RecipeInput(Crafting.MaterialId(MaterialType.Thread, r), 1),
                    new RecipeInput(Crafting.MaterialId(MaterialType.Gem, r), 1),
                },
                OutputQty: qty, SuccessChance: 0.9f, LearnLevel: CharFloor(rung), CraftLevel: rung);
        }

        Recipe Scroll(string output, int rung, int qty)
        {
            var r = RarityOf(output);
            return new($"craft_{output}", Profession.ScrollScribe, output,
                new[]
                {
                    new RecipeInput(Crafting.MaterialId(MaterialType.Thread, r), 3),
                    new RecipeInput(Crafting.MaterialId(MaterialType.Wood, r), 1),
                    new RecipeInput(Crafting.MaterialId(MaterialType.Gem, r), 1),
                },
                OutputQty: qty, SuccessChance: 0.8f, LearnLevel: CharFloor(rung), CraftLevel: rung);
        }

        // =================================================================================
        //  POTION MASTER — *"L1 - common hp pots + dash, l2 - common buff pots + unc-dash, l3 -
        //  uncommon hp pots + rare-dash, l4 - uncommon buff pots + epic-dash, l5 - rare hp pots +
        //  legend-dash, l6 - rare/mythic wahtever is the strongest buff pots + mytic dash"*
        //
        //  A two-stride alternation: the HP line and the buff line take turns, each advancing a rarity
        //  every SECOND rung, while DASH advances every single rung and is the one line that reaches
        //  Mythic. The six Dash rarities already exist in Items.cs and map to the six rungs exactly —
        //  that is not a coincidence to lean on, it is why he picked dash as the spine.
        // =================================================================================
        var buffPotionsC = new[]
        {
            ItemCatalog.SpeedPotionC, ItemCatalog.CastPotionC, ItemCatalog.AtkPotionC,
            ItemCatalog.EvaPotionC, ItemCatalog.MightPotionC, ItemCatalog.BulwarkPotionC,
            ItemCatalog.ForcePotionC, ItemCatalog.WardPotionC, ItemCatalog.AimPotionC,
        };
        var buffPotionsU = new[]
        {
            ItemCatalog.SpeedPotionU, ItemCatalog.CastPotionU, ItemCatalog.AtkPotionU,
            ItemCatalog.EvaPotionU, ItemCatalog.MightPotionU, ItemCatalog.BulwarkPotionU,
            ItemCatalog.ForcePotionU, ItemCatalog.WardPotionU, ItemCatalog.AimPotionU,
        };

        // L1 — Common HP + Common dash. `potion_minor` is also `BL-57`'s answer for this profession:
        // without a rung-1 recipe a fresh Apothecary could never earn his way off L1 at all.
        yield return Potion(ItemCatalog.MinorPotion, 1, 5);
        yield return Potion(ItemCatalog.DashPotionC, 1, 3);
        // L2 — the nine Common buff potions + Uncommon dash.
        foreach (var id in buffPotionsC) yield return Potion(id, 2, 3);
        yield return Potion(ItemCatalog.DashPotionU, 2, 3);
        // L3 — Uncommon HP + Rare dash.
        yield return Potion(ItemCatalog.HealingPotion, 3, 5);
        yield return Potion(ItemCatalog.DashPotionR, 3, 3);
        // L4 — the nine Uncommon buff potions + Epic dash.
        foreach (var id in buffPotionsU) yield return Potion(id, 4, 3);
        yield return Potion(ItemCatalog.DashPotionE, 4, 3);
        // L5 — Rare HP + Legendary dash.
        yield return Potion(ItemCatalog.GreaterPotion, 5, 5);
        yield return Potion(ItemCatalog.DashPotionL, 5, 3);
        // L6 — Mythic dash, plus the Instant (panic) potion as the top of the HP line.
        //
        // ⚠ HIS L6 SAYS *"rare/mythic wahtever is the strongest buff pots"* AND THERE IS NO SUCH THING.
        // The buff-POTION ladder stops at Uncommon by his own playtest-17 `E3` ruling — above it sits the
        // buff SCROLL, one per buff, Rare, and that is the Scribe's line (L3/L5 below), not this one.
        // So this rung takes the strongest thing the Potion Master actually has. If he wants a Mythic
        // buff potion it is a new ITEM first and a recipe second; inventing one here would re-open the
        // exact "six colours for one effect" wall E3 closed. Flagged, not faked.
        yield return Potion(ItemCatalog.InstantPotion, 6, 3);
        yield return Potion(ItemCatalog.DashPotionM, 6, 3);

        // =================================================================================
        //  SCROLL SCRIBE — *"l1-20lvl can craft common resurection scrols, scrols of return; (nothing
        //  gear related) L2- scrol enchant common(D), attri uncommon(D); L3- atri rare, scrolls rare ..
        //  anytign for C grade + basic scrolls for buffs; l4 - anything for B grade; L5 - any scrolls
        //  (anything) for A grade + other buff scrolls; L6 - S grade stuff + ultimate escape +
        //  ultimate resurect"*
        //
        //  🔑 His ladder is offset ONE RUNG from the smiths': gear service starts at D (L2), not E (L1).
        //  That offset is what buys the non-gear L1 and it makes five grades D→S fill five rungs L2→L6
        //  exactly, with no rung empty and no invented recipe. It is also how `BL-57` was answered
        //  without compromising either of his two prior rulings (see §6 of the design doc).
        // =================================================================================

        // L1 — nothing gear-related, exactly as he ruled. Both scrolls are utility.
        // ⚠ `scroll_resurrect` is an UNCOMMON item sitting on rung 1, which is the one place the
        // "input rarity = output rarity" rule bites: it costs Uncommon mats at a rung whose character
        // floor is 20. That is intended — the rung says who may make it, the mats say what it is worth —
        // but it does mean a fresh Scribe will level on Scrolls of Return and buy into the other later.
        yield return Scroll(ItemCatalog.ScrollReturn, 1, 3);
        yield return Scroll(ItemCatalog.ScrollResurrect, 1, 3);

        // L2-L6 — the enchant ladder, one normal scroll per grade, D→S.
        //
        // ⚠ HIS LABEL AND THE ITEM ID DISAGREE, and the ID is right: he writes *"scrol enchant
        // common(D)"*, and `scroll_common` is the **E**-band scroll while `scroll_uncommon` is the D one.
        // He named the D GRADE and reached for the rarity word next to it. D is what L2 gets.
        //
        // ⚠ Greater and Safe scrolls stay uncraftable at every rung — they are the elite/boss reward
        // that makes an enchant worth attempting, and he has not moved that.
        //
        // 🔑 The A and S rungs are NEW, and the measurement argues for them: `M10` shows the normal-mob
        // enchant faucet closing at 80 by design (`D1`), so the S band drops **zero** enchant scrolls
        // per hour. At the exact level the crafting ladder needs its top rung, the drop it would be
        // priced against does not exist — which makes the Scribe the *intended* A/S supply rather than
        // a convenience.
        yield return Scroll(ItemCatalog.ScrollNormalD, 2, 5);
        yield return Scroll(ItemCatalog.ScrollNormalC, 3, 5);
        yield return Scroll(ItemCatalog.ScrollNormalB, 4, 5);
        yield return Scroll(ItemCatalog.ScrollNormalA, 5, 5);
        yield return Scroll(ItemCatalog.ScrollNormalS, 6, 5);

        // Attribute scrolls, on the same grade ladder.
        // ⚠ The Common (entry) scroll and the S-grade MYTHIC one are still NOT craftable, and that is a
        // prior ruling of his, not an omission of this one: the attribute economy keeps a faucet the
        // scribe cannot flood at either end. His *"L6 - S grade stuff"* is served by the S enchant
        // scroll and the two ultimates below.
        yield return Scroll(ItemCatalog.AttrScrollUncommon, 2, 3);
        yield return Scroll(ItemCatalog.AttrScrollRare, 3, 3);
        yield return Scroll(ItemCatalog.AttrScrollEpic, 5, 3);
        yield return Scroll(ItemCatalog.AttrScrollLegendary, 5, 3);

        // Buff scrolls — *"L3 … + basic scrolls for buffs"*, *"L5 … + other buff scrolls"*.
        //
        // 🔑 THIS SUPERSEDES playtest-17 `E3`'s *"box-only"*, and only because he said so here. E3 made a
        // buff scroll one-per-buff, top rung, out of the Apothecary's Blessing Box **or nowhere**; §5d
        // gives the Scribe two rungs of them. The split follows his two words: the NINE families that
        // also have a potion line are the *"basic"* ones (L3), and the EIGHT scroll-only families —
        // which are the NPC buffer's own value — are the *"other"* ones (L5).
        //
        // ⚠ The scrolls are `Tradable: false` as items, so a crafted one cannot be sold. That is E3's
        // rule and it is what keeps this from being a gold faucet: the Scribe crafts for himself and his
        // party, which is the determinism `M10` says is the real reason to craft a consumable at all
        // (potion uptime already runs 193-231 buff-min/h against his 60/h parity target — 3-4× OVER).
        foreach (var id in new[]
        {
            ItemCatalog.SpeedScrollR, ItemCatalog.CastScrollR, ItemCatalog.AtkScrollR,
            ItemCatalog.EvaScrollR, ItemCatalog.MightScrollR, ItemCatalog.BulwarkScrollR,
            ItemCatalog.ForceScrollR, ItemCatalog.WardScrollR, ItemCatalog.AimScrollR,
        }) yield return Scroll(id, 3, 3);
        foreach (var id in new[]
        {
            ItemCatalog.BodyScrollM, ItemCatalog.SoulScrollM, ItemCatalog.VigorScrollM,
            ItemCatalog.SerenityScrollM, ItemCatalog.FocusScrollM, ItemCatalog.FerocityScrollM,
            ItemCatalog.InsightScrollM, ItemCatalog.FrenzyScrollM,
        }) yield return Scroll(id, 5, 3);

        // L6 — *"ultimate escape + ultimate resurect"*. Both are untradable Rare consumables that had
        // no source at all outside the finisher box; the top of the Scribe's ladder is where he asked
        // them to live.
        yield return Scroll(ItemCatalog.ScrollReturnUltimate, 6, 1);
        yield return Scroll(ItemCatalog.ScrollResurrectUltimate, 6, 1);
    }

    public static Recipe? Get(string id) => id is null ? null : _byId.GetValueOrDefault(id);
    public static IEnumerable<Recipe> All => _byId.Values;
    public static IEnumerable<Recipe> ForProfession(Profession p) => _byId.Values.Where(r => r.Profession == p);
}
