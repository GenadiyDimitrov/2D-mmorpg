namespace Game.Shared;

/// <summary>`BL-339` — THE MENTOR SHOP (0.239.0): the one shelf that takes Bond and Graduation Certificates. Its
/// own Mentor Steward stands beside the Apothecary in each of the three major cities (owner, 2026-10-12: *"in
/// every big city as mentees also need to buy - i agree next to apoth but only in major cities"*).
///
/// <para>🔑 <b>Everything it sells is BOUND</b> (his *"yes bonded - its char only"*): the certificates are bound,
/// so whatever they buy is too, or they would turn into gold through the goods. Timed pieces run on the WALL
/// CLOCK like the Newbie kit (*"yes like the newbie kit - same idea"*), offline included.</para>
///
/// <para>🔑 <b>The prices are his</b> (spec 2026-10-09, passes 2-3, the shop pass 2026-10-12) except where marked
/// <i>my pick</i>, which he took as written: the 60/40 armour/weapon split of each pair, and the permanent T76 table
/// (= the gold table's D column ÷ 1000, helmet 1,000 for his *"100 mentees = helmet"*).</para>
///
/// <para>The box chain is his own description: a kind box (7-day box) → a set box or weapon-kind box (7 days) → the
/// gear, on its own 30-day clock (7 days at T76/T80). A weapon at T40 and above comes in ONE attribute at its
/// maximum, one option per line of its pool; T20 cannot hold an attribute, so its weapon box skips that step.</para></summary>
public static class MentorShop
{
    /// <summary>The steward's NPC id in Brackenford; the other two major cities add their town key.</summary>
    public const string StewardId = "mentor_steward";

    // ===== TIMED GEAR ===============================================================================

    /// <summary>The tiers sold timed, his three pairs plus the T80 one.</summary>
    public static readonly int[] TimedTiers = { 20, 40, 76, 80 };

    /// <summary>Days a timed piece lasts: 30 at T20/T40, 7 at T76/T80 (his "temp30d" / "temp7d").</summary>
    public static int TimedDays(int tier) => tier >= 76 ? 7 : 30;

    /// <summary>How long a mentor BOX lasts unopened (his *"duraton of the actual box 7d"*), at every step.</summary>
    public const int BoxLifetimeSeconds = 7 * 24 * 3600;

    public static readonly string[] WeaponStems =
        { "sword1h", "sword2h", "blunt1h", "blunt2h", "duals", "bow", "wand", "staff" };
    public static readonly string[] ArmorWeights = { "heavy", "light", "robe" };
    /// <summary>Every set carries a shield (*"shield included in any set"*).</summary>
    public static readonly string[] ArmorShared = { "helm", "gloves", "boots", "shield" };
    public static readonly string[] JewelStems = { "necklace", "earring", "ring" };

    /// <summary>A timed copy of a Mythic piece: "heavy_t40" → "heavy_t40_mtemp".</summary>
    public static string TimedId(string mythicId) => mythicId + "_mtemp";
    /// <summary>A timed weapon carrying one max attribute: "wand_t40" + M.Atk → "wand_t40_mtemp_a13".</summary>
    public static string TimedAttrId(string mythicId, AttributeType type) => $"{mythicId}_mtemp_a{(int)type}";
    /// <summary>A permanent bound copy (the Graduation shelf): "helm_t76" → "helm_t76_mbound".</summary>
    public static string PermanentId(string mythicId) => mythicId + "_mbound";
    /// <summary>Any of the three copies above — the admin Equip tab hides them (owner, 2026-10-14: *"only the real
    /// mythic and common nothing else .. others can be /give"*).</summary>
    public static bool IsCopy(string id) => id.Contains("_mtemp") || id.EndsWith("_mbound");

    public static string ArmorBoxId(int tier) => $"box_mentor_armor_t{tier}";
    public static string SetBoxId(string weight, int tier) => $"box_mentor_{weight}_t{tier}";
    public static string WeaponBoxId(int tier) => $"box_mentor_weapon_t{tier}";
    public static string WeaponKindBoxId(string stem, int tier) => $"box_mentor_{stem}_t{tier}";

    /// <summary>The attribute lines a weapon stem offers, from its type alone (BoxCatalog may not read ItemCatalog).
    /// <c>ItemCatalog.MentorWares</c> checks this against each real piece's pool at boot and throws on a mismatch.</summary>
    public static AttributeType[] AttrTypesOf(string stem) => (stem switch
    {
        "wand" or "staff" => AttributeSystem.WeaponPool(WeaponType.Blunt, magic: true),
        "sword1h" or "sword2h" => AttributeSystem.WeaponPool(WeaponType.Sword, false),
        "blunt1h" or "blunt2h" => AttributeSystem.WeaponPool(WeaponType.Blunt, false),
        "duals" => AttributeSystem.WeaponPool(WeaponType.Dual, false),
        "bow" => AttributeSystem.WeaponPool(WeaponType.Bow, false),
        _ => throw new ArgumentException($"Mentor shop: no attribute pool for '{stem}'."),
    }).Select(l => l.Type).ToArray();

    /// <summary>Does a weapon at this tier come pre-attributed? Attributes start at T40 (<see cref="AttributeSystem.TierOf"/>).</summary>
    public static bool Attributed(int tier) => AttributeSystem.TierOf(tier) != AttrTier.None;

    // ===== THE OTHER WARES ============================================================================

    /// <summary>The 1-day +50% exp AND SP rune (`rune_expsp` level 6, his 2026-10-09), in a sealed box so the day
    /// starts when it is opened, like every other rune.</summary>
    public const string ExpSpRuneBox = "box_rune_expsp_50_1d";
    public const int ExpSpRunePercent = 50;

    /// <summary>The Blessing Box, bound. Its scrolls are already bound (playtest-17 E3); the box was not.</summary>
    public const string BlessingBoxBound = ItemCatalog.BoxBuffScrolls + "_bound";   // = ItemCatalog.BoundId, as a const: BoxCatalog reads it

    /// <summary>The potions with no bound copy before the shop. The other four on his list already had one (the
    /// Newbie completion kit).</summary>
    public static readonly string[] NewBoundConsumables =
        { ItemCatalog.HealingPotion, ItemCatalog.ManaPotion, ItemCatalog.GreaterPotion, ItemCatalog.GreaterManaPotion };

    /// <summary>His 1-Graduation-Certificate list, verbatim (spec 2026-10-09): what one certificate buys, all bound.</summary>
    public static readonly (string BaseId, int Qty)[] GraduationBundles =
    {
        (ItemCatalog.HealingPotion, 500),
        (ItemCatalog.ManaPotion, 250),
        (ItemCatalog.GreaterPotion, 100),
        (ItemCatalog.GreaterManaPotion, 50),
        (ItemCatalog.InstantPotion, 10),
        (ItemCatalog.DashPotionM, 5),
        (ItemCatalog.ScrollReturnUltimate, 2),
        (ItemCatalog.ScrollResurrectUltimate, 1),
    };

    // ===== PRICES ===================================================================================

    /// <summary>Bond Certificates for each timed tier's (armour box, weapon box). His pair totals 100 / 250 / 500,
    /// split 60/40 (my pick, his "Ok"): a set is four pieces and a shield.</summary>
    private static (int Armor, int Weapon) BondPair(int tier) => tier switch
    {
        20 => (60, 40),
        40 => (150, 100),
        _  => (300, 200),   // 76
    };

    /// <summary>The T80 7-day pair: his *"1000bond + 2-3 graduations"*, which no mentee can pay (they never
    /// receive a Graduation Certificate) — split 600 + 2 and 400 + 1.</summary>
    private static (int Bond, int Grad) T80Armor => (600, 2);
    private static (int Bond, int Grad) T80Weapon => (400, 1);

    public const int GrandRunePrice = 50;     // his, pass 2
    public const int ExpSpRunePrice = 50;     // his, pass 3
    public const int BlessingBoxPrice = 10;   // his "~10"

    /// <summary>The permanent T76 piece in Graduation Certificates — the gold table's D column ÷ 1000, so the slots
    /// keep the proportions every other price in the game has (my table, his *"lets try with perma T76 with your
    /// prices"*). An AUTHORED table, not a formula off gold: a retune of the gold row must not move these.</summary>
    public static int PermanentPrice(ItemDef def) => def.Slot switch
    {
        EquipSlot.Shield => 1000,
        EquipSlot.Armor => def.ArmorSlot switch
        {
            ArmorSlot.Body => 1800,
            ArmorSlot.Head => 1000,
            _ => 600,                                  // gloves / boots
        },
        EquipSlot.Weapon => def.WeaponType.IsTwoHanded() ? 3000 : 2700,
        EquipSlot.Jewel => def.JewelType switch
        {
            JewelType.Necklace => 1500,
            JewelType.Earring => 500,
            _ => 250,                                  // ring
        },
        _ => 0,
    };

    /// <summary>The tier the permanent pieces are sold at. T80 would be Graduation-priced too (his ruling), but is
    /// not sold yet.</summary>
    public const int PermanentTier = 76;

    // ===== THE SHELF ================================================================================

    private static ShopCost[] Bond(int n) => new[] { new ShopCost(ItemCatalog.BondCertificate, n) };
    private static ShopCost[] Grad(int n) => new[] { new ShopCost(ItemCatalog.GraduationCertificate, n) };

    /// <summary>Every row in shelf order: runes, the timed pairs by tier, the T80 pair, the bundles, the permanent
    /// T76 pieces. Ids only — <see cref="ShopCatalog"/> owns the tabs.</summary>
    public static IEnumerable<(string ItemId, ShopRow Row)> Rows()
    {
        yield return (ItemCatalog.BoxGrandRune24h, new ShopRow(Bond(GrandRunePrice)));
        yield return (ExpSpRuneBox, new ShopRow(Bond(ExpSpRunePrice)));
        yield return (BlessingBoxBound, new ShopRow(Bond(BlessingBoxPrice)));

        foreach (int tier in TimedTiers)
        {
            if (tier == 80)
            {
                yield return (ArmorBoxId(tier), new ShopRow(Bond(T80Armor.Bond).Concat(Grad(T80Armor.Grad)).ToArray()));
                yield return (WeaponBoxId(tier), new ShopRow(Bond(T80Weapon.Bond).Concat(Grad(T80Weapon.Grad)).ToArray()));
                continue;
            }
            var (armor, weapon) = BondPair(tier);
            yield return (ArmorBoxId(tier), new ShopRow(Bond(armor)));
            yield return (WeaponBoxId(tier), new ShopRow(Bond(weapon)));
        }

        foreach (var (baseId, qty) in GraduationBundles)
            yield return (ItemCatalog.BoundId(baseId), new ShopRow(Grad(1), qty));

        foreach (var stem in WeaponStems.Concat(ArmorWeights).Concat(ArmorShared).Concat(JewelStems))
        {
            string id = PermanentId($"{stem}_t{PermanentTier}");
            if (ItemCatalog.Get(id) is ItemDef d)
                yield return (id, new ShopRow(Grad(PermanentPrice(d))));
        }
    }
}
