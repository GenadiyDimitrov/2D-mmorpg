namespace Game.Shared;

public enum ItemGrade { F = 0, E = 1, B = 2, A = 3, S = 4 }

public enum ItemRarity { Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, God = 99 }

// Jewel = the magic-defence slot. ONE jewel equips for now; the equip code is
// written to expand to the L2 layout (2 rings / 2 earrings / 1 necklace) later
// by allowing several Jewel-slot items at once.
public enum EquipSlot { Weapon = 0, Armor = 1, Consumable = 2, Scroll = 3, QuestItem = 4, Shield = 5, Jewel = 6 }

public enum ArmorWeight { None = 0, Heavy = 1, Light = 2, Robe = 3 }

/// <summary>Body-part slot for armor. A full set is one of each (Head/Body/Gloves/
/// Boots). Only BODY carries an ArmorWeight (Heavy/Light/Robe) and the bulk of the
/// defence + 2 rolled attributes; Head/Gloves/Boots are WEIGHTLESS accessories shared
/// across builds, each carrying a single slot-specific attribute (Head HP/MP regen,
/// Gloves atk/cast speed, Boots move speed/eva). None = not a body-armor piece.</summary>
public enum ArmorSlot { None = 0, Head = 1, Body = 2, Gloves = 3, Boots = 4 }

/// <summary>Broad weapon category. Drives which skills work and the base
/// attack range. All classes CAN equip any weapon; skills gate usefulness.</summary>
// Daggers ARE the Dual type (treated as dual-wield): lower per-hit, very fast,
// high crit, no shield. There is deliberately no separate Dagger value.
// A STAFF is just a 2H Blunt (magic) — there is no separate Staff value; the
// "Staff" name is an item noun. Blunt = higher accuracy, lower crit than bladed.
public enum WeaponType { None = 0, Sword = 1, Dual = 2, Bow = 3, Blunt = 4 }

/// <summary>One- vs two-handed. A 2H weapon occupies the offhand, so it cannot be
/// paired with a shield (the equip code mutually excludes them).</summary>
public enum WeaponHands { OneHand = 0, TwoHand = 1 }

/// <summary>Enchant scroll failure behaviour (design doc):
/// Common -> item breaks on fail; Uncommon -> enchant resets to +0;
/// Rare -> enchant drops by 1 (never breaks).</summary>
public enum ScrollKind { None = 0, Common = 1, Uncommon = 2, Rare = 3 }

/// <summary>Attribute (re-roll) scroll tier. Rarity decides how many of the item's
/// rolled attributes you can LOCK while the rest re-roll: Common locks 0 (reroll all),
/// Uncommon 1, Rare 2; Legendary rerolls ALL and forces each to its MAX value (for a
/// legendary item whose every stat must be maxed).</summary>
public enum AttrScrollKind { None = 0, Common = 1, Uncommon = 2, Rare = 3, Legendary = 4 }

/// <summary>
/// An item template. The Id is a STABLE STRING KEY (e.g. "sword_e_rare") — it
/// is the item's permanent identity, stored in saves and referenced by loot
/// tables, the debug menu, etc. IDs are never renumbered; new items get new
/// keys. WeaponRange &gt; 0 marks ranged weapons (bows/staves).
/// </summary>
public record ItemDef(
    string Id,
    string Name,
    EquipSlot Slot,
    ItemGrade Grade,
    ItemRarity Rarity,
    ArmorWeight Weight = ArmorWeight.None,
    ArmorSlot ArmorSlot = ArmorSlot.None,
    WeaponType WeaponType = WeaponType.None,
    WeaponHands Hands = WeaponHands.OneHand,
    int AtkBonus = 0,
    int MAtkBonus = 0,
    int DefBonus = 0,
    int HpBonus = 0,
    int MpBonus = 0,
    int EvaBonus = 0,
    float WeaponRange = 0,
    // ----- Shield stats (only when Slot == Shield) -----
    float BlockChance = 0f,        // flat chance to block (0..1); buffs/passives add
    float BlockReduction = 0f,     // fraction of damage removed on a block (0..1)
    int ShieldDefense = 0,         // flat defence while shield equipped
    float ShieldCritDefense = 0f,  // reduces attacker crit CHANCE (0..1)
    int ShieldEvasionPenalty = 0,  // lowers your evasion (the L2 tradeoff)
    // ----- Consumables (potions) -----
    float HealPercentPerSecond = 0f,
    float InstantHealPercent = 0f,
    int PotionDurationTicks = 0,
    int PotionCooldownTicks = 0,
    ScrollKind ScrollKind = ScrollKind.None,
    // ----- Fixed (non-rolled) attributes, e.g. for the legendary one-off -----
    ItemAttribute[]? FixedAttributes = null,
    // ----- Jewel stat: magic defence. Jewels are the ONLY source of magic
    // defence beyond the level-based base (see StatCalculator.MagicDefence). -----
    int MDefBonus = 0,
    // ----- Attribute (re-roll) scroll tier (None = not an attribute scroll). -----
    AttrScrollKind AttrScroll = AttrScrollKind.None,
    // ----- Armor SET id ("" = not a set piece). Wearing all 4 slots of one set
    // grants its set bonus (see ArmorSetCatalog). -----
    string SetId = "");

public static class ItemCatalog
{
    // -----------------------------------------------------------------------
    // Stable string keys for hand-referenced items (potions, scrolls, legendary).
    // Weapon/armor keys are generated as "<type>_<grade>_<rarity>" — see below.
    // -----------------------------------------------------------------------
    public const string MinorPotion = "potion_minor";
    public const string HealingPotion = "potion_healing";
    public const string GreaterPotion = "potion_greater";
    public const string ScrollCommon = "scroll_common";
    public const string ScrollUncommon = "scroll_uncommon";
    public const string ScrollRare = "scroll_rare";
    public const string AttrScrollCommon = "attrscroll_common";
    public const string AttrScrollUncommon = "attrscroll_uncommon";
    public const string AttrScrollRare = "attrscroll_rare";
    public const string AttrScrollLegendary = "attrscroll_legendary";
    public const string MarkOfFaith = "quest_mark_of_faith";
    public const string ClericsProof = "quest_clerics_proof";
    public const string GodWeapon = "god_judgment";
    public const string GodArmor = "god_robes";
    public const string WoodenShield = "shield_wooden";
    public const string IronShield = "shield_iron";
    public const string BrassAmulet = "jewel_brass_amulet";
    public const string SilverTalisman = "jewel_silver_talisman";
    public const string IronMace = "blunt_1h_iron_mace";        // 1H physical blunt (shield-ok)
    public const string AshWand = "blunt_1h_ash_wand";          // 1H magic blunt (mAtk > pAtk)
    // Dark Dominion armor set: two BODY weight variants (heavy/robe) sharing the
    // same three accessories. Wearing a body + all 3 accessories grants the set bonus.
    public const string DarkDominionHeavyBody = "set_dark_dominion_body_heavy";
    public const string DarkDominionRobeBody = "set_dark_dominion_body_robe";
    public const string DarkDominionHead = "set_dark_dominion_head";
    public const string DarkDominionGloves = "set_dark_dominion_gloves";
    public const string DarkDominionBoots = "set_dark_dominion_boots";

    public static string WeaponKey(WeaponType type, ItemGrade grade, ItemRarity rarity) =>
        $"{type.ToString().ToLowerInvariant()}_{grade.ToString().ToLowerInvariant()}_{rarity.ToString().ToLowerInvariant()}";

    // 3-arg overload defaults to the Body piece (keeps existing drop/debug callers
    // giving the main weighted piece; pass a slot for accessories).
    public static string ArmorKey(ArmorWeight weight, ItemGrade grade, ItemRarity rarity) =>
        ArmorKey(weight, ArmorSlot.Body, grade, rarity);

    public static string ArmorKey(ArmorWeight weight, ArmorSlot slot, ItemGrade grade, ItemRarity rarity) =>
        $"{weight.ToString().ToLowerInvariant()}_{slot.ToString().ToLowerInvariant()}_" +
        $"{grade.ToString().ToLowerInvariant()}_{rarity.ToString().ToLowerInvariant()}";

    private static readonly Dictionary<string, ItemDef> All = BuildCatalog();

    private static Dictionary<string, ItemDef> BuildCatalog()
    {
        var list = new List<ItemDef>();

        // ===================================================================
        //  WEAPONS — every type x grade x rarity. All classes can equip these;
        //  whether a class's SKILLS work depends on the weapon (design doc).
        // ===================================================================
        // Per-type display names and the base attack at F-common; higher grade
        // and rarity scale up from there.
        var weaponInfo = new (WeaponType Type, WeaponHands Hands, string Noun, int BaseAtk, float Range, int MpBonus)[]
        {
            (WeaponType.Sword, WeaponHands.OneHand, "Sword", 6,  0,   0),
            (WeaponType.Dual,  WeaponHands.TwoHand, "Daggers", 5, 0,  0),   // dual: lower per-hit, faster
            (WeaponType.Bow,   WeaponHands.TwoHand, "Bow",   7,  400, 0),
            // Generated Blunt line = the 2H caster STAFF (tiny basic atk, high mAtk,
            // gives MP; power is in SPELLS, no weapon range). 1H blunts are hand-added below.
            (WeaponType.Blunt, WeaponHands.TwoHand, "Staff", 2,  0,   20),
        };

        foreach (var w in weaponInfo)
        {
            foreach (var grade in new[] { ItemGrade.F, ItemGrade.E })
            {
                // Grade scaling: E roughly doubles F.
                int gradeAtk = grade == ItemGrade.F ? w.BaseAtk : w.BaseAtk * 2 + 4;
                int gradeMp = grade == ItemGrade.F ? w.MpBonus : w.MpBonus * 2;

                foreach (var rarity in new[] { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare })
                {
                    // Rarity scaling: +40% per tier on top of grade.
                    int atk = (int)(gradeAtk * (1f + 0.40f * (int)rarity));
                    int mp = (int)(gradeMp * (1f + 0.20f * (int)rarity));

                    // Magic attack per weapon type: caster weapons (staff) carry
                    // most of their power as mAtk, melee weapons a small splash,
                    // so hybrid weapons are possible. Tune the fractions here.
                    float mAtkFraction = w.Type switch
                    {
                        WeaponType.Blunt => 1.20f,   // 2H staff: caster weapon, high mAtk
                        WeaponType.Bow => 0.25f,
                        WeaponType.Dual => 0.30f,
                        _ => 0.35f                    // sword: small splash
                    };
                    int mAtk = (int)(atk * mAtkFraction);

                    string gradeName = grade == ItemGrade.F ? "Worn" : "Steel";
                    string rarityName = rarity switch
                    {
                        ItemRarity.Uncommon => "Fine ",
                        ItemRarity.Rare => "Masterwork ",
                        _ => ""
                    };

                    list.Add(new ItemDef(
                        WeaponKey(w.Type, grade, rarity),
                        $"{rarityName}{gradeName} {w.Noun}",
                        EquipSlot.Weapon, grade, rarity,
                        WeaponType: w.Type,
                        Hands: w.Hands,
                        AtkBonus: atk,
                        MAtkBonus: mAtk,
                        MpBonus: mp,
                        WeaponRange: w.Range));
                }
            }
        }

        // ===================================================================
        //  ARMOR — only the BODY piece carries weight (Heavy/Light/Robe) and the
        //  bulk of the defence; Head/Gloves/Boots are WEIGHTLESS accessories shared
        //  across builds, valued by their single slot-specific rolled attribute. This
        //  keeps the item count low (3 body weights + 3 accessories, x grade x rarity).
        // ===================================================================
        string GradeName(ItemGrade g) => g == ItemGrade.F ? "Worn" : "Tempered";
        string RarityName(ItemRarity r) => r switch
        {
            ItemRarity.Uncommon => "Fine ",
            ItemRarity.Rare => "Masterwork ",
            _ => ""
        };

        // ----- Weighted BODY armor (full profile + 2 rolled attributes by weight) -----
        var bodyInfo = new (ArmorWeight Weight, string Noun, int BaseDef, int Hp, int Mp, int Eva)[]
        {
            (ArmorWeight.Heavy, "Plate",   6, 30, 0,  0),
            (ArmorWeight.Light, "Leather", 4, 10, 0,  6),
            (ArmorWeight.Robe,  "Robe",    2, 0,  30, 0),
        };
        foreach (var a in bodyInfo)
        {
            foreach (var grade in new[] { ItemGrade.F, ItemGrade.E })
            {
                int gd = grade == ItemGrade.F ? a.BaseDef : a.BaseDef * 2 + 3;
                int ghp = grade == ItemGrade.F ? a.Hp : a.Hp * 2;
                int gmp = grade == ItemGrade.F ? a.Mp : a.Mp * 2;
                int gev = grade == ItemGrade.F ? a.Eva : a.Eva * 2;

                foreach (var rarity in new[] { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare })
                {
                    float rmul = 1f + 0.35f * (int)rarity;
                    list.Add(new ItemDef(
                        ArmorKey(a.Weight, ArmorSlot.Body, grade, rarity),
                        $"{RarityName(rarity)}{GradeName(grade)} {a.Noun} Armor",
                        EquipSlot.Armor, grade, rarity,
                        Weight: a.Weight,
                        ArmorSlot: ArmorSlot.Body,
                        DefBonus: (int)(gd * rmul),
                        HpBonus: (int)(ghp * rmul),
                        MpBonus: (int)(gmp * rmul),
                        EvaBonus: (int)(gev * rmul)));
                }
            }
        }

        // ----- Weightless ACCESSORIES (Head/Gloves/Boots): no base stats; their value
        //       is the single slot-specific attribute rolled on them (value by grade). -----
        var accessoryInfo = new (ArmorSlot Slot, string Noun)[]
        {
            (ArmorSlot.Head,   "Helmet"),
            (ArmorSlot.Gloves, "Gauntlets"),
            (ArmorSlot.Boots,  "Boots"),
        };
        foreach (var acc in accessoryInfo)
        {
            foreach (var grade in new[] { ItemGrade.F, ItemGrade.E })
            {
                foreach (var rarity in new[] { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare })
                {
                    list.Add(new ItemDef(
                        ArmorKey(ArmorWeight.None, acc.Slot, grade, rarity),
                        $"{RarityName(rarity)}{GradeName(grade)} {acc.Noun}",
                        EquipSlot.Armor, grade, rarity,
                        Weight: ArmorWeight.None,
                        ArmorSlot: acc.Slot));
                }
            }
        }

        // ===================================================================
        //  NAMED ARMOR SETS — hand-authored. A set tags its pieces with a SetId;
        //  wearing all 4 slots of one set grants its bonus (ArmorSetCatalog).
        //  Dark Dominion: heavy OR robe body, sharing the same 3 accessories.
        // ===================================================================
        list.Add(new ItemDef(DarkDominionHeavyBody, "Dark Dominion Plate", EquipSlot.Armor,
            ItemGrade.E, ItemRarity.Rare, Weight: ArmorWeight.Heavy, ArmorSlot: ArmorSlot.Body,
            DefBonus: 28, HpBonus: 130, SetId: ArmorSetCatalog.DarkDominion));
        list.Add(new ItemDef(DarkDominionRobeBody, "Dark Dominion Robe", EquipSlot.Armor,
            ItemGrade.E, ItemRarity.Rare, Weight: ArmorWeight.Robe, ArmorSlot: ArmorSlot.Body,
            DefBonus: 10, HpBonus: 20, MpBonus: 130, SetId: ArmorSetCatalog.DarkDominion));
        list.Add(new ItemDef(DarkDominionHead, "Dark Dominion Helm", EquipSlot.Armor,
            ItemGrade.E, ItemRarity.Rare, ArmorSlot: ArmorSlot.Head, SetId: ArmorSetCatalog.DarkDominion));
        list.Add(new ItemDef(DarkDominionGloves, "Dark Dominion Gauntlets", EquipSlot.Armor,
            ItemGrade.E, ItemRarity.Rare, ArmorSlot: ArmorSlot.Gloves, SetId: ArmorSetCatalog.DarkDominion));
        list.Add(new ItemDef(DarkDominionBoots, "Dark Dominion Sabatons", EquipSlot.Armor,
            ItemGrade.E, ItemRarity.Rare, ArmorSlot: ArmorSlot.Boots, SetId: ArmorSetCatalog.DarkDominion));

        // ===================================================================
        //  POTIONS
        // ===================================================================
        list.Add(new ItemDef(MinorPotion, "Minor Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common,
            HealPercentPerSecond: 0.01f, PotionDurationTicks: 150, PotionCooldownTicks: 300));
        list.Add(new ItemDef(HealingPotion, "Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon,
            HealPercentPerSecond: 0.02f, PotionDurationTicks: 150, PotionCooldownTicks: 300));
        list.Add(new ItemDef(GreaterPotion, "Greater Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            InstantHealPercent: 0.50f, PotionCooldownTicks: 300));

        // ===================================================================
        //  SHIELDS — equippable by any class (with a one-hand weapon), but only
        //  tanks make them matter via Shield Mastery passives. Base values are
        //  modest; passives/buffs scale them. Block = flat % damage reduction.
        // ===================================================================
        list.Add(new ItemDef(WoodenShield, "Wooden Shield", EquipSlot.Shield,
            ItemGrade.F, ItemRarity.Common,
            BlockChance: 0.15f, BlockReduction: 0.30f, ShieldDefense: 40,
            ShieldCritDefense: 0.05f, ShieldEvasionPenalty: 4));
        list.Add(new ItemDef(IronShield, "Iron Shield", EquipSlot.Shield,
            ItemGrade.E, ItemRarity.Uncommon,
            BlockChance: 0.20f, BlockReduction: 0.35f, ShieldDefense: 90,
            ShieldCritDefense: 0.08f, ShieldEvasionPenalty: 6));

        // ===================================================================
        //  1H BLUNTS — maces/wands. Blunt = higher accuracy, lower crit. One hand,
        //  so they CAN pair with a shield. A "magic" blunt simply carries more
        //  mAtk than pAtk (mages who want a shield use these instead of a staff).
        // ===================================================================
        list.Add(new ItemDef(IronMace, "Iron Mace", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Blunt,
            Hands: WeaponHands.OneHand, AtkBonus: 7, MAtkBonus: 3));   // physical mace
        list.Add(new ItemDef(AshWand, "Ash Wand", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Blunt,
            Hands: WeaponHands.OneHand, AtkBonus: 2, MAtkBonus: 5, MpBonus: 10));  // 1H magic blunt: mAtk>pAtk, < staff

        // ===================================================================
        //  JEWELS — the ONLY source of magic defence (beyond the level base).
        //  One jewel equips for now; the slot is built to expand to 5 later.
        // ===================================================================
        list.Add(new ItemDef(BrassAmulet, "Brass Amulet", EquipSlot.Jewel,
            ItemGrade.F, ItemRarity.Common, MDefBonus: 30));
        list.Add(new ItemDef(SilverTalisman, "Silver Talisman", EquipSlot.Jewel,
            ItemGrade.E, ItemRarity.Uncommon, MDefBonus: 70, MpBonus: 40));

        // ===================================================================
        //  ENCHANT SCROLLS
        // ===================================================================
        list.Add(new ItemDef(ScrollCommon, "Enchant Scroll (Common)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Common, ScrollKind: ScrollKind.Common));
        list.Add(new ItemDef(ScrollUncommon, "Enchant Scroll (Uncommon)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Uncommon, ScrollKind: ScrollKind.Uncommon));
        list.Add(new ItemDef(ScrollRare, "Enchant Scroll (Rare)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Rare, ScrollKind: ScrollKind.Rare));

        // ----- Attribute (re-roll) scrolls: reroll an item's rolled attributes,
        //       locking some by scroll tier. Legendary rerolls all at MAX value. -----
        list.Add(new ItemDef(AttrScrollCommon, "Attribute Scroll (Common)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Common, AttrScroll: AttrScrollKind.Common));
        list.Add(new ItemDef(AttrScrollUncommon, "Attribute Scroll (Uncommon)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Uncommon, AttrScroll: AttrScrollKind.Uncommon));
        list.Add(new ItemDef(AttrScrollRare, "Attribute Scroll (Rare)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Rare, AttrScroll: AttrScrollKind.Rare));
        list.Add(new ItemDef(AttrScrollLegendary, "Attribute Scroll (Legendary)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Legendary, AttrScroll: AttrScrollKind.Legendary));

        // ===================================================================
        //  QUEST ITEMS — non-droppable, non-tradeable proofs for class changes.
        // ===================================================================
        list.Add(new ItemDef(MarkOfFaith, "Mark of Faith", EquipSlot.QuestItem,
            ItemGrade.F, ItemRarity.Rare));
        list.Add(new ItemDef(ClericsProof, "Cleric's Proof", EquipSlot.QuestItem,
            ItemGrade.F, ItemRarity.Epic));

        // ===================================================================
        //  GOD-TIER one-offs (debug). Every attribute maxed at 100%.
        // ===================================================================
        list.Add(new ItemDef(GodWeapon, "God's Judgment", EquipSlot.Weapon,
            ItemGrade.S, ItemRarity.God, WeaponType: WeaponType.Sword,
            AtkBonus: 1000, WeaponRange: 1000,
            FixedAttributes: new ItemAttribute[]
            {
                new(AttributeType.AttackPercent, 100),
                new(AttributeType.AttackSpeedPercent, 100),
                new(AttributeType.CastSpeedPercent, 100),
                new(AttributeType.SpeedPercent, 100),
                new(AttributeType.HealthPercent, 100),
                new(AttributeType.ManaPercent, 100),
                new(AttributeType.EvasionPercent, 100),
                new(AttributeType.DefencePercent, 100),
            }));

        list.Add(new ItemDef(GodArmor, "God's Robes", EquipSlot.Armor,
            ItemGrade.S, ItemRarity.God, Weight: ArmorWeight.Robe, ArmorSlot: ArmorSlot.Body,
            DefBonus: 1000, HpBonus: 1000, MpBonus: 1000, EvaBonus: 1000,
            FixedAttributes: new ItemAttribute[]
            {
                new(AttributeType.HealthPercent, 100),
                new(AttributeType.ManaPercent, 100),
                new(AttributeType.DefencePercent, 100),
                new(AttributeType.EvasionPercent, 100),
                new(AttributeType.SpeedPercent, 100),
                new(AttributeType.CastSpeedPercent, 100),
            }));

        // ----- Duplicate-key guard: clear startup error instead of a crash ---
        var dict = new Dictionary<string, ItemDef>();
        foreach (var item in list)
        {
            if (!dict.TryAdd(item.Id, item))
                throw new InvalidOperationException(
                    $"Duplicate item id '{item.Id}' ({item.Name} collides with {dict[item.Id].Name}).");
        }
        return dict;
    }

    public static ItemDef? Get(string id) => id is null ? null : All.GetValueOrDefault(id);

    public static IEnumerable<ItemDef> AllItems => All.Values;

    public static bool IsPotion(ItemDef def) => def.Slot == EquipSlot.Consumable;
    public static bool IsScroll(ItemDef def) => def.Slot == EquipSlot.Scroll;
    public static bool IsEnchantScroll(ItemDef def) => def.Slot == EquipSlot.Scroll && def.ScrollKind != ScrollKind.None;
    public static bool IsAttributeScroll(ItemDef def) => def.AttrScroll != AttrScrollKind.None;
    public static bool IsQuestItem(ItemDef def) => def.Slot == EquipSlot.QuestItem;
    public static bool IsEquippable(ItemDef def) => def.Slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Jewel;

    /// <summary>Grade gates per design doc: F 0+, E 20+, B 40+, A 60+, S 80+.</summary>
    public static int RequiredLevel(ItemGrade grade) => grade switch
    {
        ItemGrade.F => 0,
        ItemGrade.E => 20,
        ItemGrade.B => 40,
        ItemGrade.A => 60,
        _ => 80
    };
}

/// <summary>One possible drop: an item key, its chance, and the mob-level band
/// it applies to.</summary>
public record LootEntry(string ItemId, float Chance, int MinLevel, int MaxLevel);

/// <summary>
/// Per-mob loot tables, keyed by mob name. Each mob drops different gear;
/// entries are gated by the killed mob's level (low = F, high = E grade).
/// Every mob also rolls the shared potion + scroll tables.
/// </summary>
public static class LootTables
{
    private static readonly LootEntry[] SharedPotions =
    {
        new(ItemCatalog.MinorPotion, 0.10f, 1, 30),
        new(ItemCatalog.HealingPotion, 0.04f, 4, 30),
        new(ItemCatalog.GreaterPotion, 0.01f, 8, 30),
    };

    // Scrolls: rarer than everything else, higher-level mobs only.
    private static readonly LootEntry[] SharedScrolls =
    {
        new(ItemCatalog.ScrollCommon, 0.030f, 5, 30),
        new(ItemCatalog.ScrollUncommon, 0.015f, 9, 30),
        new(ItemCatalog.ScrollRare, 0.006f, 13, 30),
    };

    private static readonly Dictionary<string, LootEntry[]> Tables = BuildTables();

    private static Dictionary<string, LootEntry[]> BuildTables()
    {
        // Helper to roll an F (low level) and E (high level) entry for a key.
        LootEntry F(WeaponType t, ItemRarity r, float c) =>
            new(ItemCatalog.WeaponKey(t, ItemGrade.F, r), c, 1, 10);
        LootEntry E(WeaponType t, ItemRarity r, float c) =>
            new(ItemCatalog.WeaponKey(t, ItemGrade.E, r), c, 11, 30);
        LootEntry FA(ArmorWeight w, ItemRarity r, float c) =>
            new(ItemCatalog.ArmorKey(w, ItemGrade.F, r), c, 1, 10);
        LootEntry EA(ArmorWeight w, ItemRarity r, float c) =>
            new(ItemCatalog.ArmorKey(w, ItemGrade.E, r), c, 11, 30);

        return new Dictionary<string, LootEntry[]>
        {
            ["Boar"] = new[]
            {
                F(WeaponType.Blunt, ItemRarity.Common, 0.16f),
                F(WeaponType.Sword, ItemRarity.Common, 0.16f),
                F(WeaponType.Blunt, ItemRarity.Uncommon, 0.06f),
                E(WeaponType.Blunt, ItemRarity.Common, 0.12f),
                E(WeaponType.Sword, ItemRarity.Common, 0.12f),
            },
            ["Wolf"] = new[]
            {
                FA(ArmorWeight.Light, ItemRarity.Common, 0.16f),
                FA(ArmorWeight.Heavy, ItemRarity.Common, 0.14f),
                FA(ArmorWeight.Light, ItemRarity.Uncommon, 0.05f),
                EA(ArmorWeight.Light, ItemRarity.Common, 0.12f),
                EA(ArmorWeight.Heavy, ItemRarity.Common, 0.10f),
            },
            ["Slime"] = new[]
            {
                FA(ArmorWeight.Robe, ItemRarity.Common, 0.16f),
                F(WeaponType.Blunt, ItemRarity.Common, 0.12f),
                FA(ArmorWeight.Robe, ItemRarity.Rare, 0.05f),
                EA(ArmorWeight.Robe, ItemRarity.Common, 0.12f),
            },
            ["Spider"] = new[]
            {
                FA(ArmorWeight.Light, ItemRarity.Common, 0.18f),
                F(WeaponType.Dual, ItemRarity.Uncommon, 0.10f),
                F(WeaponType.Bow, ItemRarity.Uncommon, 0.10f),
                EA(ArmorWeight.Light, ItemRarity.Common, 0.14f),
                E(WeaponType.Bow, ItemRarity.Common, 0.10f),
            },
            ["Bandit"] = new[]
            {
                F(WeaponType.Sword, ItemRarity.Common, 0.16f),
                F(WeaponType.Sword, ItemRarity.Uncommon, 0.10f),
                F(WeaponType.Sword, ItemRarity.Rare, 0.04f),
                FA(ArmorWeight.Heavy, ItemRarity.Uncommon, 0.06f),
                E(WeaponType.Sword, ItemRarity.Common, 0.12f),
            },
        };
    }

    public static List<string> Roll(string mobName, int mobLevel, Random rng)
    {
        var drops = new List<string>();
        if (Tables.TryGetValue(mobName, out var table))
            RollEntries(table, mobLevel, rng, drops);
        RollEntries(SharedPotions, mobLevel, rng, drops);
        RollEntries(SharedScrolls, mobLevel, rng, drops);
        return drops;
    }

    private static void RollEntries(LootEntry[] table, int mobLevel, Random rng, List<string> drops)
    {
        foreach (var entry in table)
        {
            if (mobLevel < entry.MinLevel || mobLevel > entry.MaxLevel) continue;
            if (rng.NextDouble() < entry.Chance) drops.Add(entry.ItemId);
        }
    }
}
