namespace Game.Shared;

public enum ItemGrade { F = 0, E = 1, B = 2, A = 3, S = 4 }

/// <summary>L2-style GRADE PENALTY (owner 2026-07-16): wearing gear above your level scales its combat
/// stats down. Each grade has an intended minimum character level; below it, the item's weapon ATK /
/// armor DEF is multiplied by a flat under-level factor. Applied per-item in Entity.RecomputeDerived
/// BEFORE masteries and set bonuses. Numbers are deliberately simple/tunable.</summary>
public static class GradePenalty
{
    /// <summary>Intended minimum character level for each grade.</summary>
    public static int MinLevel(ItemGrade g) => g switch
    {
        ItemGrade.F => 1,
        ItemGrade.E => 20,
        ItemGrade.B => 40,
        ItemGrade.A => 52,
        ItemGrade.S => 61,
        _ => 1,
    };

    /// <summary>Multiplier applied to a grade's weapon ATK / armor DEF when the wearer is BELOW its min
    /// level (flat while under-level); 1.0 at or above it. F never penalises.</summary>
    public static float Factor(ItemGrade grade, int level)
    {
        if (level >= MinLevel(grade)) return 1f;
        return grade switch
        {
            ItemGrade.E => 0.5f,
            ItemGrade.B => 0.4f,
            ItemGrade.A => 0.3f,
            ItemGrade.S => 0.2f,
            _ => 1f,
        };
    }
}

public enum ItemRarity { Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, God = 99 }

// Jewel = the magic-defence slot. ONE jewel equips for now; the equip code is
// written to expand to the L2 layout (2 rings / 2 earrings / 1 necklace) later
// by allowing several Jewel-slot items at once.
public enum EquipSlot { Weapon = 0, Armor = 1, Consumable = 2, Scroll = 3, QuestItem = 4, Shield = 5, Jewel = 6, Box = 7, Material = 8 }

/// <summary>Jewel sub-type — limits how many can be worn: 2 Rings, 2 Earrings, 1 Necklace.</summary>
public enum JewelType { None = 0, Ring = 1, Earring = 2, Necklace = 3 }

/// <summary>Unified TOP-LEVEL item category, derived from EquipSlot (see ItemDef.Type).
/// One clean axis for grouping/filtering. A 2H weapon is MainHand AND occupies the
/// OffHand (no separate type — see ItemDef.OccupiesOffHand).</summary>
public enum ItemType { Other = 0, MainHand, OffHand, Armor, Jewel, Consumable, Scroll, Box, Quest, Material }

/// <summary>Unified SUB-TYPE across all items (see ItemDef.Subtype), derived from the
/// per-domain enums (WeaponType / ArmorSlot / JewelType / ScrollKind …). Lets you ask
/// "all Boots" or "all Sword main-hands" uniformly.</summary>
public enum ItemSubtype
{
    None = 0,
    // MainHand
    Sword, Blunt, Bow, Dual,
    // OffHand
    Shield,
    // Armor
    Helmet, Body, Gloves, Boots,
    // Jewel
    Ring, Earring, Necklace,
    // Consumable
    Potion, BuffPotion,
    // Scroll
    EnchantScroll, AttributeScroll,
    // misc
    Box, QuestToken, Material,
}

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
// A "Staff" is a TwoHandedBlunt; the "Staff" name is just an item noun. Blunt = higher
// accuracy, lower crit than bladed. HANDS + TYPE are encoded in this ONE [Flags] enum: an
// ITEM has exactly one value, while a skill's weapon REQUIREMENT is a mask (e.g. Strike =
// AnySword | AnyBlunt, Battle Presence = TwoHandedSword | TwoHandedBlunt), tested with a
// single bitwise-AND. Bow and Dual are inherently two-handed (no 1H variants).
[Flags]
public enum WeaponType
{
    None = 0,
    Sword = 1, Blunt = 2, Dual = 4, Bow = 8,
    TwoHandedSword = 16, TwoHandedBlunt = 32,
    // Convenience masks (for skill requirements + hands tests):
    AnySword  = Sword | TwoHandedSword,
    AnyBlunt  = Blunt | TwoHandedBlunt,
    OneHanded = Sword | Blunt,
    TwoHanded = TwoHandedSword | TwoHandedBlunt | Bow | Dual,
}

/// <summary>Helpers over the merged WeaponType (hands + type in one enum).</summary>
public static class WeaponTypes
{
    /// <summary>True for a two-handed weapon (occupies the offhand → no shield).</summary>
    public static bool IsTwoHanded(this WeaponType w) => (w & WeaponType.TwoHanded) != 0;

    /// <summary>Fold a hands-specific type down to its BASE type (TwoHandedSword→Sword,
    /// TwoHandedBlunt→Blunt) so hands-agnostic stat tables (variance/speed/crit) still match.</summary>
    public static WeaponType Base(this WeaponType w) => w switch
    {
        WeaponType.TwoHandedSword => WeaponType.Sword,
        WeaponType.TwoHandedBlunt => WeaponType.Blunt,
        _ => w
    };
}

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
    // A weapon carries ONE power number (AtkBonus) plus two CHANNEL FACTORS. The factors
    // multiply the FINISHED channel (base stat + level + gear), which is the whole point:
    // P.Atk and M.Atk are both built on the same shared base (AtkStat + level*2), so only a
    // MULTIPLIER can stop that base leaking into the channel a weapon isn't meant to serve.
    // A second authored number could never do it — a 2H sword's flat M.Atk is small, but the
    // shared base handed a sword-wielding buffer ~85% of a staff's magic damage for free.
    // Fighter weapon: power = its P.Atk, PAtkFactor 1.0, MAtkFactor ~0.6.
    // Mage weapon:    power = its M.Atk, MAtkFactor 1.0, PAtkFactor ~0.6 (P.Atk nerfed).
    int AtkBonus = 0,
    float PAtkFactor = 1f,
    float MAtkFactor = 1f,
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
    // ----- Consumables -----
    // A consumable does NOT implement an effect. It names a SKILL, and the skill does the work
    // (heal, HoT, buff, teleport) — "everything is a skill; only what GRANTS it differs".
    // The skill's CastTicks decides the feel: 0 = drink it (instant), > 0 = a channelled scroll.
    // The old bespoke heal fields (HealPercentPerSecond / InstantHealPercent /
    // PotionDurationTicks) are gone: a HoT potion is now just a buff, so it shows on the buff
    // bar and gets "stronger cancels weaker" from the skill's BuffKey + Rank.
    string UseSkillId = "",
    // The shared "one healing potion per N ticks" rule. This stays an ITEM property because it's
    // a rule about DRINKING, not about the effect — and it's what separates a heal potion (has
    // one) from a buff potion (doesn't). 0 = no shared cooldown.
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
    string SetId = "",
    // ----- Gold value (vendor pricing). 0 = filled from DefaultValue at build time;
    //       quest items and god-tier one-offs stay 0 = not buyable/sellable. Pass an
    //       explicit Value to override the formula for a specific item. -----
    int Value = 0,
    // ----- Trade/price control (every item carries these three) -----
    //  Tradable=false: can't be sold to a vendor or traded to players (only DELETED).
    //  BuyPriceOverride:  null = use the Value formula; -1 = cannot be purchased; 0 = free.
    //  SellPriceOverride: null = use the Value formula; 0 = sells for nothing.
    bool Tradable = true,
    int? BuyPriceOverride = null,
    int? SellPriceOverride = null,
    // NoAttributes=true: never rolls a random attribute and can't be given one
    // (newbie/starter gear). Enforced in AttributeSystem.Roll.
    bool NoAttributes = false,
    // Jewel sub-type (only when Slot == Jewel) — gates how many can be worn.
    JewelType JewelType = JewelType.None,
    // Gear TIER by character level (0 = legacy grade/rarity gear). The level tiers
    // (20/40/52/61/76 ≈ E/D/C/B/A) drive the number + max of rolled weapon attributes.
    int ItemLevel = 0,
    // Caster weapon flag: a wand/staff is a Blunt/TwoHandedBlunt type but rolls the CASTER
    // attribute pool (cast/M.Atk/magic-crit) instead of the fighter blunt pool. Owner's pick —
    // a simple flag that doesn't touch the WeaponType logic.
    bool IsMagicWeapon = false,
    // Per-item basic-attack speed base (333 = normal; higher = faster). 0 = use the weapon
    // type default (StatCalculator.WeaponAttackBaseSpeed). Lets two bows differ (slow vs very slow).
    int AttackSpeedBase = 0,
    // Recipe BOOK: the recipe id this item teaches when "opened" ("" = not a book). A book is an
    // EquipSlot.Box so the client's open flow reuses; opening adds the id to the char's KnownRecipes.
    string TeachesRecipeId = "")
{
    /// <summary>Unified top-level category (derived from EquipSlot). Weapons are MainHand,
    /// shields OffHand; everything else maps 1:1.</summary>
    public ItemType Type => Slot switch
    {
        EquipSlot.Weapon => ItemType.MainHand,
        EquipSlot.Shield => ItemType.OffHand,
        EquipSlot.Armor => ItemType.Armor,
        EquipSlot.Jewel => ItemType.Jewel,
        EquipSlot.Consumable => ItemType.Consumable,
        EquipSlot.Scroll => ItemType.Scroll,
        EquipSlot.Box => ItemType.Box,
        EquipSlot.QuestItem => ItemType.Quest,
        EquipSlot.Material => ItemType.Material,
        _ => ItemType.Other
    };

    /// <summary>Unified sub-type (derived from the per-domain enums).</summary>
    public ItemSubtype Subtype => Slot switch
    {
        EquipSlot.Weapon => WeaponType.Base() switch
        {
            WeaponType.Sword => ItemSubtype.Sword,
            WeaponType.Blunt => ItemSubtype.Blunt,
            WeaponType.Bow => ItemSubtype.Bow,
            WeaponType.Dual => ItemSubtype.Dual,
            _ => ItemSubtype.None
        },
        EquipSlot.Shield => ItemSubtype.Shield,
        EquipSlot.Armor => ArmorSlot switch
        {
            ArmorSlot.Head => ItemSubtype.Helmet,
            ArmorSlot.Body => ItemSubtype.Body,
            ArmorSlot.Gloves => ItemSubtype.Gloves,
            ArmorSlot.Boots => ItemSubtype.Boots,
            _ => ItemSubtype.None
        },
        EquipSlot.Jewel => JewelType switch
        {
            JewelType.Ring => ItemSubtype.Ring,
            JewelType.Earring => ItemSubtype.Earring,
            JewelType.Necklace => ItemSubtype.Necklace,
            _ => ItemSubtype.None
        },
        // A heal potion is the one with the shared drink cooldown; anything else consumable
        // that grants an effect is a buff potion (scrolls carry a ScrollKind and fall through).
        EquipSlot.Consumable => PotionCooldownTicks > 0 ? ItemSubtype.Potion : ItemSubtype.BuffPotion,
        EquipSlot.Scroll => AttrScroll != AttrScrollKind.None ? ItemSubtype.AttributeScroll : ItemSubtype.EnchantScroll,
        EquipSlot.Box => ItemSubtype.Box,
        EquipSlot.QuestItem => ItemSubtype.QuestToken,
        EquipSlot.Material => ItemSubtype.Material,
        _ => ItemSubtype.None
    };

    /// <summary>True if this is a two-handed MAIN-HAND weapon — it also claims the
    /// OffHand slot (so a shield can't be worn with it; enforced in HandleEquip).</summary>
    public bool OccupiesOffHand => Slot == EquipSlot.Weapon && WeaponType.IsTwoHanded();
}

public static class ItemCatalog
{
    // -----------------------------------------------------------------------
    // Stable string keys for hand-referenced items (potions, scrolls, legendary).
    // Weapon/armor keys are generated as "<type>_<grade>_<rarity>" — see below.
    // -----------------------------------------------------------------------
    public const string MinorPotion = "potion_minor";
    public const string HealingPotion = "potion_healing";
    public const string GreaterPotion = "potion_greater";
    // Buff potions (rarity = tier). Common sold by vendors; Uncommon/Rare drop.
    public const string SpeedPotionC = "potion_speed_c";
    public const string SpeedPotionU = "potion_speed_u";
    public const string SpeedPotionR = "potion_speed_r";
    public const string CastPotionC = "potion_cast_c";
    public const string CastPotionU = "potion_cast_u";
    public const string CastPotionR = "potion_cast_r";
    public const string AtkPotionC = "potion_atk_c";
    public const string AtkPotionU = "potion_atk_u";
    public const string AtkPotionR = "potion_atk_r";
    public const string ScrollCommon = "scroll_common";
    public const string ScrollUncommon = "scroll_uncommon";
    public const string ScrollRare = "scroll_rare";
    public const string ScrollReturn = "scroll_return";
    public const string ScrollReturnUltimate = "scroll_return_ultimate";
    public const string ScrollResurrect = "scroll_resurrect";
    public const string ScrollResurrectUltimate = "scroll_resurrect_ultimate";
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
    public const string ElementalStone = "elemental_stone";     // reagent for Elemental Burst
    /// <summary>How much of your ATK power a weapon lets through to the channel it does NOT
    /// exist to serve: a sword's magic, a staff's melee. THE tuning knob for weapon identity.
    /// 0.6 reproduces the gear CSV's second column (a sword's 92 P.Atk × 0.6 ≈ its authored 54
    /// M.Atk). NOTE: 0.6 does NOT yet close the buffer's staff→2H-sword swap — because magic
    /// damage goes as √mAtk, he loses only ~15% of it while doubling P.Atk. Dropping this to
    /// ~0.2-0.3 makes that a real trade. Any single weapon can override via its own
    /// PAtkFactor/MAtkFactor.</summary>
    public const float OffChannelFactor = 0.6f;

    // Newbie STARTER weapons — given on character creation. Untradeable, sold for 0,
    // can't be purchased (see the "newbie" item flags below).
    public const string NewbieSword1H = "newbie_sword_1h";
    public const string NewbieDaggers = "newbie_daggers";
    public const string NewbieSword2H = "newbie_sword_2h";
    public const string NewbieBow     = "newbie_bow";
    public const string NewbieStaff   = "newbie_staff";
    // Newbie armor (two sets: Light for fighters, Robe for mages) + jewels. No random
    // attributes, untradeable, sell 0, buy -1. Full set = body + helm + gloves + boots.
    public const string NewbieLightBody   = "newbie_light_body";
    public const string NewbieRobeBody    = "newbie_robe_body";
    // SHARED accessories — used by BOTH the light and robe newbie sets.
    public const string NewbieHelm        = "newbie_helm";
    public const string NewbieGloves      = "newbie_gloves";
    public const string NewbieBoots       = "newbie_boots";
    public const string NewbieEarring     = "newbie_earring";
    public const string NewbieRing        = "newbie_ring";
    public const string NewbieNecklace    = "newbie_necklace";
    // Boxes/chests — opened from the inventory; roll their BoxCatalog loot table.
    public const string BoxNewbie         = "box_newbie";
    public const string BoxTreasure       = "box_treasure";
    public const string BoxNewbieArmorLight = "box_newbie_armor_light";
    public const string BoxNewbieArmorRobe  = "box_newbie_armor_robe";
    public const string BoxNewbieJewels     = "box_newbie_jewels";
    public const string BoxNewbieWeapons    = "box_newbie_weapons";   // SELECTION box
    // Dark Dominion armor set: two BODY weight variants (heavy/robe) sharing the
    // same three accessories. Wearing a body + all 3 accessories grants the set bonus.
    public const string DarkDominionHeavyBody = "set_dark_dominion_body_heavy";
    public const string DarkDominionLightBody = "set_dark_dominion_body_light";
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
        var weaponInfo = new (WeaponType Type, string Noun, int BaseAtk, float Range, int MpBonus)[]
        {
            (WeaponType.Sword, "Sword", 6,  0,   0),
            (WeaponType.Dual,  "Daggers", 5, 0,  0),   // dual: lower per-hit, faster
            (WeaponType.Bow,   "Bow",   7,  400, 0),
            // Generated Blunt line = the 2H caster STAFF (real caster weapon: meaningful
            // P/M.Atk, gives MP, no weapon range; the mage's tiny BASIC hit comes from the
            // 0.15 basic-attack multiplier, not the weapon). 1H blunts are hand-added below.
            (WeaponType.TwoHandedBlunt, "Staff", 23, 0,   20),
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

                    // ONE power number + channel factors (see OffChannelFactor). The generated
                    // Blunt line IS the 2H caster staff: its power is MAGIC power, and its melee
                    // is what gets suppressed. Everything else is a physical weapon.
                    bool magic = w.Type.Base() == WeaponType.Blunt;
                    int power = magic ? (int)(atk * 1.05f) : atk;

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
                        AtkBonus: power,
                        PAtkFactor: magic ? OffChannelFactor : 1f,
                        MAtkFactor: magic ? 1f : OffChannelFactor,
                        MpBonus: mp,
                        IsMagicWeapon: magic,
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
        list.Add(new ItemDef(DarkDominionLightBody, "Dark Dominion Leathers", EquipSlot.Armor,
            ItemGrade.E, ItemRarity.Rare, Weight: ArmorWeight.Light, ArmorSlot: ArmorSlot.Body,
            DefBonus: 18, HpBonus: 70, EvaBonus: 6, SetId: ArmorSetCatalog.DarkDominion));
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
        // Healing potions: priced so heals are a real gold sink (~500 for the staple).
        // Healing potions: the potion NAMES a skill and the skill heals. PotionCooldownTicks is
        // what marks it a HEAL potion (the shared "one per 30s" drink rule).
        list.Add(new ItemDef(MinorPotion, "Minor Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common,
            UseSkillId: SkillCatalog.PotHealMinor, PotionCooldownTicks: 300, Value: 200));
        list.Add(new ItemDef(HealingPotion, "Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon,
            UseSkillId: SkillCatalog.PotHeal, PotionCooldownTicks: 300, Value: 500));
        list.Add(new ItemDef(GreaterPotion, "Greater Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            UseSkillId: SkillCatalog.PotHealGreater, PotionCooldownTicks: 300, Value: 1500));

        // Return scrolls: same mechanism, but their skill has a CAST time, so double-clicking one
        // channels it. The skills are NOT learned — the ITEM is what grants them.
        list.Add(new ItemDef(ScrollReturn, "Scroll of Return", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common,
            UseSkillId: SkillCatalog.ScrollReturnSkill, Value: 500, SellPriceOverride: 0));
        list.Add(new ItemDef(ScrollReturnUltimate, "Ultimate Scroll of Return", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            UseSkillId: SkillCatalog.ScrollReturnUltSkill,
            Tradable: false, BuyPriceOverride: -1, SellPriceOverride: 0));

        // Resurrection scrolls: used WHILE DEAD to self-revive (their skill channels a cast). Basic
        // restores no exp (1500g vendor); the ultimate restores all lost exp (not shop-stocked).
        list.Add(new ItemDef(ScrollResurrect, "Scroll of Resurrection", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon,
            UseSkillId: SkillCatalog.ScrollResurrectSkill, Value: 1500, SellPriceOverride: 0));
        list.Add(new ItemDef(ScrollResurrectUltimate, "Ultimate Scroll of Resurrection", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            UseSkillId: SkillCatalog.ScrollResurrectUltSkill,
            Tradable: false, BuyPriceOverride: -1, SellPriceOverride: 0));

        // Elemental Stone — a crafting/reagent material (not drinkable). Stacks; consumed
        // by skills that list it as a ConsumableId (e.g. the nuker's Elemental Burst).
        list.Add(new ItemDef(ElementalStone, "Elemental Stone", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, Value: 100));

        // ----- Buff potions: consume to gain a timed (weaker-than-class) buff. Rarity
        //       is the tier; same line supersedes by rank. No heal cooldown. -----
        // Common (Lesser) buff potions are vendor-sold staples: ~1.5k each. The
        // Uncommon/Greater tiers are drop-only; priced higher for sell value.
        list.Add(new ItemDef(SpeedPotionC, "Swiftness Potion (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.PBuffSpeedC, Value: 1500));
        list.Add(new ItemDef(SpeedPotionU, "Swiftness Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.PBuffSpeedU, Value: 5000));
        list.Add(new ItemDef(SpeedPotionR, "Swiftness Potion (Greater)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, UseSkillId: SkillCatalog.PBuffSpeedR, Value: 12000));
        list.Add(new ItemDef(CastPotionC, "Focus Potion (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.PBuffCastC, Value: 1500));
        list.Add(new ItemDef(CastPotionU, "Focus Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.PBuffCastU, Value: 5000));
        list.Add(new ItemDef(CastPotionR, "Focus Potion (Greater)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, UseSkillId: SkillCatalog.PBuffCastR, Value: 12000));
        list.Add(new ItemDef(AtkPotionC, "Haste Potion (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.PBuffAtkC, Value: 1500));
        list.Add(new ItemDef(AtkPotionU, "Haste Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.PBuffAtkU, Value: 5000));
        list.Add(new ItemDef(AtkPotionR, "Haste Potion (Greater)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, UseSkillId: SkillCatalog.PBuffAtkR, Value: 12000));

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
        list.Add(new ItemDef(IronMace, "Iron Mace", EquipSlot.Weapon,   // 1H PHYSICAL mace
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Blunt,
            AtkBonus: 7, PAtkFactor: 1f, MAtkFactor: OffChannelFactor));
        list.Add(new ItemDef(AshWand, "Ash Wand", EquipSlot.Weapon,     // 1H CASTER blunt (weaker than a staff)
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Blunt,
            AtkBonus: 5, PAtkFactor: OffChannelFactor, MAtkFactor: 1f, MpBonus: 10, IsMagicWeapon: true));

        // ===================================================================
        //  NEWBIE STARTER WEAPONS — handed out on character creation. They are
        //  UNTRADEABLE, sell for 0, and cannot be purchased (buy -1). A fighter gets
        //  all four melee/ranged options; a mage gets the staff. P.Atk / M.Atk per owner.
        // ===================================================================
        // ONE power number + channel factors (see ItemDef). Fighter weapons: power = P.Atk,
        // P×1.0 / M×0.6. The staff: power = its M.Atk, M×1.0 / P×0.6 (its P.Atk is nerfed —
        // a mage should not swing like a fighter now that the archetype multiplier is gone).
        list.Add(new ItemDef(NewbieSword1H, "Newbie Sword", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Sword,
            AtkBonus: 24, PAtkFactor: 1.0f, MAtkFactor: 0.6f, Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(NewbieDaggers, "Newbie Daggers", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Dual,
            AtkBonus: 21, PAtkFactor: 1.0f, MAtkFactor: 0.6f, Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(NewbieSword2H, "Newbie Greatsword", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.TwoHandedSword,
            AtkBonus: 29, PAtkFactor: 1.0f, MAtkFactor: 0.6f, Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(NewbieBow, "Newbie Bow", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Bow,
            AtkBonus: 49, PAtkFactor: 1.0f, MAtkFactor: 0.6f, WeaponRange: 400, Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(NewbieStaff, "Newbie Staff", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.TwoHandedBlunt,
            AtkBonus: 24, PAtkFactor: 0.6f, MAtkFactor: 1.0f, MpBonus: 20, Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));

        // ===================================================================
        //  BOXES / CHESTS — opened from inventory; contents roll the BoxCatalog table.
        // ===================================================================
        list.Add(new ItemDef(BoxNewbie, "Newbie Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(BoxTreasure, "Treasure Chest", EquipSlot.Box, ItemGrade.F, ItemRarity.Uncommon));
        list.Add(new ItemDef(BoxNewbieArmorLight, "Newbie Light Armor Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(BoxNewbieArmorRobe, "Newbie Robe Armor Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(BoxNewbieJewels, "Newbie Jewels Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(BoxNewbieWeapons, "Newbie Weapons Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));

        // ===================================================================
        //  NEWBIE ARMOR (two sets) + JEWELS — no attributes, untradeable, sell 0, buy -1.
        //  Light set (fighter): +42 HP, +2% P.Def. Robe set (mage): +15% cast speed.
        //  Full set = body + helm + gloves + boots (set bonus from ArmorSetCatalog).
        // ===================================================================
        // Bodies — each grants its set's bonus (light/robe). They SHARE the accessories below.
        list.Add(new ItemDef(NewbieLightBody, "Newbie Light Armor", EquipSlot.Armor, ItemGrade.F, ItemRarity.Common,
            Weight: ArmorWeight.Light, ArmorSlot: ArmorSlot.Body, DefBonus: 86, SetId: ArmorSetCatalog.NewbieLight,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(NewbieRobeBody, "Newbie Robe Armor", EquipSlot.Armor, ItemGrade.F, ItemRarity.Common,
            Weight: ArmorWeight.Robe, ArmorSlot: ArmorSlot.Body, DefBonus: 49, MpBonus: 109, SetId: ArmorSetCatalog.NewbieRobe,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));

        // SHARED accessories — complete EITHER newbie set (SetId = NewbieAccessories).
        list.Add(new ItemDef(NewbieHelm, "Newbie Helmet", EquipSlot.Armor, ItemGrade.F, ItemRarity.Common,
            ArmorSlot: ArmorSlot.Head, DefBonus: 21, SetId: ArmorSetCatalog.NewbieAccessories,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(NewbieGloves, "Newbie Gloves", EquipSlot.Armor, ItemGrade.F, ItemRarity.Common,
            ArmorSlot: ArmorSlot.Gloves, DefBonus: 15, SetId: ArmorSetCatalog.NewbieAccessories,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(NewbieBoots, "Newbie Boots", EquipSlot.Armor, ItemGrade.F, ItemRarity.Common,
            ArmorSlot: ArmorSlot.Boots, DefBonus: 15, SetId: ArmorSetCatalog.NewbieAccessories,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));

        // Jewels (earring/ring give M.Def; necklace gives P.Def). Earrings/rings handed
        // out in pairs (see the jewels box). One jewel slot equips for now (expandable).
        list.Add(new ItemDef(NewbieEarring, "Newbie Earring", EquipSlot.Jewel, ItemGrade.F, ItemRarity.Common,
            MDefBonus: 13, JewelType: JewelType.Earring, Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(NewbieRing, "Newbie Ring", EquipSlot.Jewel, ItemGrade.F, ItemRarity.Common,
            MDefBonus: 9, JewelType: JewelType.Ring, Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(NewbieNecklace, "Newbie Necklace", EquipSlot.Jewel, ItemGrade.F, ItemRarity.Common,
            DefBonus: 18, JewelType: JewelType.Necklace, Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));

        // ===================================================================
        //  JEWELS — the ONLY source of magic defence (beyond the level base).
        //  One jewel equips for now; the slot is built to expand to 5 later.
        // ===================================================================
        list.Add(new ItemDef(BrassAmulet, "Brass Amulet", EquipSlot.Jewel,
            ItemGrade.F, ItemRarity.Common, MDefBonus: 30, JewelType: JewelType.Necklace));
        list.Add(new ItemDef(SilverTalisman, "Silver Talisman", EquipSlot.Jewel,
            ItemGrade.E, ItemRarity.Uncommon, MDefBonus: 70, MpBonus: 40, JewelType: JewelType.Ring));

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

        // ===================================================================
        //  CLASS-CHANGE PROOFS — two non-tradeable quest items per playable second
        //  class, awarded by its quest chain and consumed at the class change.
        // ===================================================================
        foreach (var cls in ClassCatalog.Playable)
        {
            list.Add(new ItemDef(ClassTokenId(cls.Id), $"{cls.Name} Trial Token",
                EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Rare));
            list.Add(new ItemDef(ClassProofId(cls.Id), $"{cls.Name}'s Proof",
                EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Epic));
        }
        // 3rd-class (discipline) proofs — same helpers, 3rd-class ids (101-136).
        foreach (var cls in ThirdClassCatalog.Playable)
        {
            list.Add(new ItemDef(ClassTokenId(cls.Id), $"{cls.Name} Ordeal Mark",
                EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Epic));
            list.Add(new ItemDef(ClassProofId(cls.Id), $"Seal of the {cls.Name}",
                EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Legendary));
        }

        // ----- Level-tier gear (docs/gear/gear_sets.csv): weapons + base armor/shield/accessory/
        //       jewel pieces. SET BONUSES (and the dmg/support VARIANTS) come later; these carry only
        //       their own base stats via the existing equip rails, so no new mechanic to test. -----
        // The tiered gear pieces (Epic rarity) = the craft/boss SET tier. From each base piece we
        // also generate weaker Common/Uncommon/Rare DROP versions (scaled stats, no set), so mobs
        // can drop usable-now gear while the full set stays a crafting/boss goal.
        var tieredGear = TieredWeapons().Concat(TieredArmor()).ToList();
        list.AddRange(tieredGear);
        list.AddRange(ScaledDropItems(tieredGear));
        list.AddRange(Materials());
        list.AddRange(RecipeBooks(tieredGear));

        // ----- Duplicate-key guard + value fill: any item left at Value 0 gets the
        //       formula price (quest items / god one-offs stay 0 = not for trade). -----
        var dict = new Dictionary<string, ItemDef>();
        foreach (var raw in list)
        {
            var item = raw.Value > 0 ? raw : raw with { Value = DefaultValue(raw) };
            if (!dict.TryAdd(item.Id, item))
                throw new InvalidOperationException(
                    $"Duplicate item id '{item.Id}' ({item.Name} collides with {dict[item.Id].Name}).");
        }
        return dict;
    }

    /// <summary>Crafting MATERIALS: 5 types × 5 rarities (docs/Crafting.md). Tradable + stackable,
    /// no attributes; rarity drives the value. Each type is refined by its owning profession
    /// (Crafting.RefinerOf) but every rarity also drops from mobs.</summary>
    private static IEnumerable<ItemDef> Materials()
    {
        foreach (var type in Crafting.MaterialTypes)
            foreach (var rarity in Crafting.MaterialRarities)
                yield return new ItemDef(Crafting.MaterialId(type, rarity),
                    Crafting.MaterialName(type, rarity),
                    EquipSlot.Material, ItemGrade.F, rarity,
                    Value: MaterialValue(rarity), NoAttributes: true);
    }

    private static int MaterialValue(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => 5,
        ItemRarity.Uncommon => 25,
        ItemRarity.Rare => 120,
        ItemRarity.Epic => 600,
        ItemRarity.Legendary => 3000,
        _ => 5
    };

    /// <summary>The scaled Common/Uncommon/Rare DROP versions of the tiered gear. Each base tier
    /// piece (the Epic set item) spawns three weaker copies at ~65/78/90% of its stats, standalone
    /// (no SetId, so no set bonus). Only the plain base-tier pieces get copies — the alternate body
    /// VARIANTS (e.g. "heavy_t52_dmg") stay set-only. Ids: "<baseid>_common" etc.</summary>
    // A property, not a static field: BuildCatalog() runs from the `All` field initializer above
    // this declaration, so a field here would still be null when ScaledDropItems reads it.
    private static (ItemRarity Rarity, float Scale)[] DropTiers => new[]
    {
        (ItemRarity.Common,   0.65f),
        (ItemRarity.Uncommon, 0.78f),
        (ItemRarity.Rare,     0.90f),
    };

    /// <summary>True for a plain base-tier id like "heavy_t52" (the part after the last "_t" is all
    /// digits) — excludes alternate variants like "heavy_t52_dmg".</summary>
    private static bool IsBaseTier(string id)
    {
        int i = id.LastIndexOf("_t", StringComparison.Ordinal);
        if (i < 0) return false;
        string tail = id.Substring(i + 2);
        return tail.Length > 0 && tail.All(char.IsDigit);
    }

    /// <summary>Recipe BOOKS for the DropOnly recipes — the A-grade (level-76) SET pieces, whose
    /// craft recipe (`craft_&lt;id&gt;`) is DropOnly (see RecipeCatalog.FinishedItemRecipes). Each book
    /// is an EquipSlot.Box (reuses the client open flow) that teaches its recipe. Derived from the
    /// tiered gear here (NOT from RecipeCatalog) to avoid a circular static-init with the recipe
    /// catalog, which itself reads ItemCatalog.AllItems.</summary>
    private static IEnumerable<ItemDef> RecipeBooks(IEnumerable<ItemDef> tiered)
    {
        foreach (var d in tiered)
        {
            if (d.ItemLevel < 76 || d.Rarity != ItemRarity.Epic) continue;   // A-grade set pieces only
            if (d.Slot is not (EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield or EquipSlot.Jewel)) continue;
            string recipeId = $"craft_{d.Id}";
            yield return new ItemDef(RecipeBookId(recipeId), $"Recipe: {d.Name}",
                EquipSlot.Box, ItemGrade.A, ItemRarity.Epic,
                TeachesRecipeId: recipeId);
        }
    }

    private static IEnumerable<ItemDef> ScaledDropItems(IEnumerable<ItemDef> tiered)
    {
        foreach (var d in tiered)
        {
            if (d.Slot is not (EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield or EquipSlot.Jewel)) continue;
            if (!IsBaseTier(d.Id)) continue;   // only plain base-tier pieces spawn drop copies

            foreach (var (rarity, scale) in DropTiers)
            {
                int S(int v) => v == 0 ? 0 : Math.Max(1, (int)(v * scale));
                string name = $"{rarity} {d.Name}";
                yield return d with
                {
                    Id = $"{d.Id}_{rarity.ToString().ToLowerInvariant()}",
                    Name = name,
                    Rarity = rarity,
                    AtkBonus = S(d.AtkBonus),
                    MAtkBonus = S(d.MAtkBonus),
                    DefBonus = S(d.DefBonus),
                    MDefBonus = S(d.MDefBonus),
                    HpBonus = S(d.HpBonus),
                    MpBonus = S(d.MpBonus),
                    EvaBonus = S(d.EvaBonus),
                    ShieldDefense = S(d.ShieldDefense),
                    SetId = "",            // drop copies are standalone (no set bonus)
                    NoAttributes = true,   // armors carry no attributes for now (owner)
                    Value = 0,             // filled from DefaultValue (rarity-scaled)
                };
            }
        }
    }

    /// <summary>Display letter for a gear LEVEL tier (20/40/52/61/76 → E/D/C/B/A). Cosmetic —
    /// the item's <see cref="ItemDef.ItemLevel"/> drives the mechanics, not the letter.</summary>
    public static string TierLetter(int level) =>
        level >= 76 ? "A" : level >= 61 ? "B" : level >= 52 ? "C" : level >= 40 ? "D" : "E";

    // Enum grade for pricing/sorting only (the enum has no C/D). ItemLevel is the real tier.
    private static ItemGrade TierGrade(int level) =>
        level >= 61 ? ItemGrade.A : level >= 40 ? ItemGrade.B : ItemGrade.E;

    /// <summary>The level-tier weapons from docs/gear/gear_sets.csv — id "<key>_t<level>", base
    /// P.Atk/M.Atk straight from the CSV (the two numbers), bow attack-speed variants, and the
    /// IsMagicWeapon flag on wands/staves (their attributes roll the caster pool). Attribute COUNT
    /// + MAX come from the level (AttributeSystem tiered methods), not grade/rarity.</summary>
    private static IEnumerable<ItemDef> TieredWeapons()
    {
        var weapons = new (string Key, string Noun, WeaponType Type, bool Magic, float Range,
            (int L, int P, int M, int As)[] Rows)[]
        {
            ("sword1h", "Sword",      WeaponType.Sword,          false, 0,
                new[] { (20,92,54,0),(40,156,83,0),(52,194,99,0),(61,232,114,0),(76,281,132,0) }),
            ("sword2h", "Greatsword", WeaponType.TwoHandedSword, false, 0,
                new[] { (20,112,54,0),(40,190,83,0),(52,236,99,0),(61,282,114,0),(76,342,132,0) }),
            ("blunt1h", "Mace",       WeaponType.Blunt,          false, 0,
                new[] { (20,92,54,0),(40,156,83,0),(52,194,99,0),(61,232,114,0),(76,281,132,0) }),
            ("blunt2h", "Warhammer",  WeaponType.TwoHandedBlunt, false, 0,
                new[] { (20,112,54,0),(40,190,83,0),(52,236,99,0),(61,282,114,0),(76,342,132,0) }),
            ("duals",   "Daggers",    WeaponType.Dual,           false, 0,
                new[] { (20,80,54,0),(40,136,83,0),(52,170,99,0),(61,203,114,0),(76,271,132,0) }),
            ("bow",     "Bow",        WeaponType.Bow,            false, 400,
                new[] { (20,191,55,293),(40,316,84,293),(52,400,99,293),(61,528,114,227),(76,581,132,293) }),
            ("wand",    "Wand",       WeaponType.Blunt,          true,  0,
                new[] { (20,74,72,0),(40,111,101,0),(52,140,122,0),(61,186,152,0),(76,225,175,0) }),
            ("staff",   "Staff",      WeaponType.TwoHandedBlunt, true,  0,
                new[] { (20,90,79,0),(40,135,111,0),(52,189,145,0),(61,226,167,0),(76,274,193,0) }),
        };
        foreach (var w in weapons)
            foreach (var (L, P, M, As) in w.Rows)
                yield return new ItemDef($"{w.Key}_t{L}", $"{TierLetter(L)}-Grade {w.Noun}",
                    EquipSlot.Weapon, TierGrade(L), ItemRarity.Epic,
                    WeaponType: w.Type,
                    AtkBonus: w.Magic ? M : P,
                    PAtkFactor: w.Magic ? OffChannelFactor : 1f,
                    MAtkFactor: w.Magic ? 1f : OffChannelFactor,
                    WeaponRange: w.Range,
                    ItemLevel: L, IsMagicWeapon: w.Magic, AttackSpeedBase: As);
    }

    /// <summary>The level-tier ARMOR from docs/gear/gear_sets.csv — base bodies (Heavy/Light/Robe),
    /// shields, weightless accessories (Gloves/Boots/Helm) and jewels (Necklace/Ring/Earring). Each
    /// carries only its own base stat (P.Def / M.Def / +MP), via the existing equip path — SET BONUSES
    /// and the dmg/support VARIANTS are deferred (they need the StatMods main-stat pass + a playtest).
    /// Armors roll NO attributes for now (owner). Ids: "<key>_t<level>".</summary>
    private static IEnumerable<ItemDef> TieredArmor()
    {
        int[] lv = { 20, 40, 52, 61, 76 };

        // ---- Bodies: (key, noun, weight, pDef[5], mp[5]) — robe carries inherent +MaxMP. ----
        var bodies = new (string Key, string Noun, ArmorWeight W, int[] Def, int[] Mp)[]
        {
            ("heavy", "Plate Armor",   ArmorWeight.Heavy, new[]{167,240,270,293,332}, new[]{0,0,0,0,0}),
            ("light", "Leather Armor", ArmorWeight.Light, new[]{125,218,202,220,249}, new[]{0,0,0,0,0}),
            ("robe",  "Robe",          ArmorWeight.Robe,  new[]{84,110,135,147,166},  new[]{274,508,613,718,866}),
        };
        foreach (var b in bodies)
            for (int i = 0; i < lv.Length; i++)
                yield return new ItemDef($"{b.Key}_t{lv[i]}", $"{TierLetter(lv[i])}-Grade {b.Noun}",
                    EquipSlot.Armor, TierGrade(lv[i]), ItemRarity.Epic,
                    Weight: b.W, ArmorSlot: ArmorSlot.Body, DefBonus: b.Def[i], MpBonus: b.Mp[i],
                    ItemLevel: lv[i], NoAttributes: true, SetId: $"set_{b.Key}_t{lv[i]}");

        // ---- Body VARIANTS: same base P.Def as the tier's body, alternate SET bonus (dmg/support/
        //      nuke lines from the CSV). They share the tier's accessory line. (Bonuses in ArmorSets.) ----
        var variants = new (string Key, ArmorWeight W, string Noun, int L, int Def, int Mp, string Role)[]
        {
            ("heavy_t52_dmg",  ArmorWeight.Heavy, "Plate Armor",   52, 270, 0,   "Assault"),
            ("heavy_t61_dmg",  ArmorWeight.Heavy, "Plate Armor",   61, 293, 0,   "Assault"),
            ("light_t40_pdef", ArmorWeight.Light, "Leather Armor", 40, 218, 0,   "Bulwark"),
            ("light_t40_mdef", ArmorWeight.Light, "Leather Armor", 40, 218, 0,   "Warded"),
            ("light_t40_str",  ArmorWeight.Light, "Leather Armor", 40, 218, 0,   "Brawler"),
            ("light_t52_sup",  ArmorWeight.Light, "Leather Armor", 52, 202, 0,   "Sage"),
            ("light_t61_dmg",  ArmorWeight.Light, "Leather Armor", 61, 220, 0,   "Assault"),
            ("robe_t40_sup",   ArmorWeight.Robe,  "Robe",          40, 110, 508, "Warden"),
            ("robe_t40_nuke",  ArmorWeight.Robe,  "Robe",          40, 110, 508, "Destroyer"),
        };
        foreach (var v in variants)
            yield return new ItemDef(v.Key, $"{TierLetter(v.L)}-Grade {v.Noun} ({v.Role})",
                EquipSlot.Armor, TierGrade(v.L), ItemRarity.Epic,
                Weight: v.W, ArmorSlot: ArmorSlot.Body, DefBonus: v.Def, MpBonus: v.Mp,
                ItemLevel: v.L, NoAttributes: true, SetId: $"set_{v.Key}");

        // ---- Weightless accessories (shared across weights). ----
        var acc = new (string Key, string Noun, ArmorSlot Slot, int[] Def)[]
        {
            ("gloves", "Gauntlets", ArmorSlot.Gloves, new[]{29,39,44,49,55}),
            ("boots",  "Boots",     ArmorSlot.Boots,  new[]{29,39,44,49,55}),
            ("helm",   "Helmet",    ArmorSlot.Head,   new[]{41,58,66,73,83}),
        };
        foreach (var a in acc)
            for (int i = 0; i < lv.Length; i++)
                yield return new ItemDef($"{a.Key}_t{lv[i]}", $"{TierLetter(lv[i])}-Grade {a.Noun}",
                    EquipSlot.Armor, TierGrade(lv[i]), ItemRarity.Epic,
                    ArmorSlot: a.Slot, DefBonus: a.Def[i], ItemLevel: lv[i], NoAttributes: true,
                    SetId: $"set_acc_t{lv[i]}");   // shared accessory line per tier (all weights)

        // ---- Shields (ShieldDefense from the CSV P.Def; block stats extrapolate Wooden→Iron, tunable). ----
        int[] shDef = { 143, 203, 230, 256, 299 };
        float[] shBlock = { 0.22f, 0.24f, 0.26f, 0.28f, 0.30f };
        float[] shReduce = { 0.37f, 0.39f, 0.41f, 0.43f, 0.45f };
        float[] shCrit = { 0.10f, 0.11f, 0.12f, 0.13f, 0.15f };
        int[] shEvaPen = { 7, 7, 8, 8, 9 };
        // The shield belongs to its tier's HEAVY set (the CSV puts shields in the same GroupId).
        // It is NOT required to complete the set — wearing it just adds the set's ShieldBonus.
        for (int i = 0; i < lv.Length; i++)
            yield return new ItemDef($"shield_t{lv[i]}", $"{TierLetter(lv[i])}-Grade Kite Shield",
                EquipSlot.Shield, TierGrade(lv[i]), ItemRarity.Epic,
                BlockChance: shBlock[i], BlockReduction: shReduce[i], ShieldDefense: shDef[i],
                ShieldCritDefense: shCrit[i], ShieldEvasionPenalty: shEvaPen[i],
                SetId: $"set_heavy_t{lv[i]}",
                ItemLevel: lv[i], NoAttributes: true);

        // ---- Jewels (M.Def + inherent +MP at 61/76). L2 layout = 1 necklace / 2 rings / 2 earrings. ----
        var jewels = new (string Key, string Noun, JewelType T, int[] MDef, int[] Mp)[]
        {
            ("necklace", "Necklace", JewelType.Necklace, new[]{45,64,72,85,95}, new[]{0,0,0,33,42}),
            ("ring",     "Ring",     JewelType.Ring,     new[]{22,32,36,42,48}, new[]{0,0,0,17,21}),
            ("earring",  "Earring",  JewelType.Earring,  new[]{34,45,54,63,71}, new[]{0,0,0,25,31}),
        };
        foreach (var j in jewels)
            for (int i = 0; i < lv.Length; i++)
                yield return new ItemDef($"{j.Key}_t{lv[i]}", $"{TierLetter(lv[i])}-Grade {j.Noun}",
                    EquipSlot.Jewel, TierGrade(lv[i]), ItemRarity.Epic,
                    MDefBonus: j.MDef[i], MpBonus: j.Mp[i], JewelType: j.T,
                    ItemLevel: lv[i], NoAttributes: true);

        // ---- Accessory BOX per tier (debug convenience): opens into the 3 accessories, so you
        //      grab a full accessory line at once instead of three items (see BoxCatalog). ----
        foreach (int L in lv)
            yield return new ItemDef($"box_acc_t{L}", $"{TierLetter(L)}-Grade Accessory Box",
                EquipSlot.Box, TierGrade(L), ItemRarity.Rare);
    }

    /// <summary>Formula gold value by slot/grade/rarity, used when an item def does
    /// not set an explicit Value. Quest items and god-tier one-offs return 0 so they
    /// can be neither bought nor sold.</summary>
    public static int DefaultValue(ItemDef def)
    {
        if (def.Slot == EquipSlot.QuestItem || def.Rarity == ItemRarity.God)
            return 0;

        int gradeBase = def.Grade switch
        {
            ItemGrade.F => 10,
            ItemGrade.E => 35,
            ItemGrade.B => 120,
            ItemGrade.A => 400,
            ItemGrade.S => 1200,
            _ => 10,
        };
        float rarityMul = def.Rarity switch
        {
            ItemRarity.Common => 1f,
            ItemRarity.Uncommon => 2f,
            ItemRarity.Rare => 4f,
            ItemRarity.Epic => 8f,
            ItemRarity.Legendary => 16f,
            _ => 1f,
        };
        float slotMul = def.Slot switch
        {
            EquipSlot.Weapon => 2.0f,
            EquipSlot.Armor => def.ArmorSlot == ArmorSlot.Body ? 1.6f : 0.8f,
            EquipSlot.Shield => 1.2f,
            EquipSlot.Jewel => 1.4f,
            EquipSlot.Consumable => 0.8f,
            EquipSlot.Scroll => 3.0f,
            _ => 1f,
        };
        return Math.Max(1, (int)(gradeBase * rarityMul * slotMul));
    }

    /// <summary>Gold paid to a player who SELLS this item. SellPriceOverride wins
    /// (0 = sells for nothing); otherwise the Value formula (0 = not sellable).</summary>
    public static int SellPrice(ItemDef def) =>
        def.SellPriceOverride is int s ? Math.Max(0, s)
        : def.Value <= 0 ? 0 : Math.Max(1, (int)(def.Value * GameConstants.VendorSellFraction));

    /// <summary>Gold charged when BUYING this item from a vendor (incl. the future
    /// castle surcharge). BuyPriceOverride wins (-1 = unbuyable, 0 = free); otherwise
    /// the Value formula (-1 = not buyable).</summary>
    public static int BuyPrice(ItemDef def) =>
        def.BuyPriceOverride is int b ? b
        : def.Value <= 0 ? -1 : Math.Max(1, (int)(def.Value * (1f + GameConstants.VendorBuyTaxRate)));

    /// <summary>An item the player can sell to a vendor: TRADABLE, not a quest item,
    /// and worth something. Untradeable items can only be deleted.</summary>
    public static bool IsSellable(ItemDef def) =>
        def.Tradable && def.Slot != EquipSlot.QuestItem && SellPrice(def) > 0;

    /// <summary>An openable box/chest (rolls its BoxCatalog loot table).</summary>
    public static bool IsBox(ItemDef def) => def.Slot == EquipSlot.Box;

    /// <summary>A recipe BOOK — opening it teaches its recipe (see TeachesRecipeId).</summary>
    public static bool IsRecipeBook(ItemDef def) => def.TeachesRecipeId.Length > 0;

    /// <summary>The item id of the recipe book that teaches a given recipe.</summary>
    public static string RecipeBookId(string recipeId) => $"recipe_{recipeId}";

    /// <summary>How many jewels of a given sub-type can be worn at once.</summary>
    public static int MaxOfJewelType(JewelType t) => t switch
    {
        JewelType.Ring => 2,
        JewelType.Earring => 2,
        JewelType.Necklace => 1,
        _ => 1   // untyped jewel: single
    };

    public static ItemDef? Get(string id) => id is null ? null : All.GetValueOrDefault(id);

    /// <summary>All catalog items of a top-level category (e.g. every OffHand).</summary>
    public static IEnumerable<ItemDef> OfType(ItemType type) => All.Values.Where(d => d.Type == type);

    /// <summary>All catalog items of a sub-type (e.g. every Boots, or every Sword).</summary>
    public static IEnumerable<ItemDef> OfSubtype(ItemSubtype subtype) => All.Values.Where(d => d.Subtype == subtype);

    // Per-class quest-item ids (the two proofs a class-change chain awards). Generated
    // in BuildCatalog from ClassCatalog; the quest chains reference them by these ids.
    public static string ClassTokenId(int classId) => $"qi_{classId}_token";
    public static string ClassProofId(int classId) => $"qi_{classId}_proof";

    public static IEnumerable<ItemDef> AllItems => All.Values;

    public static bool IsPotion(ItemDef def) => def.Slot == EquipSlot.Consumable;
    /// <summary>A HEALING potion — the one bound by the shared drink cooldown.</summary>
    public static bool IsHealPotion(ItemDef def) =>
        def.Slot == EquipSlot.Consumable && def.PotionCooldownTicks > 0 && !string.IsNullOrEmpty(def.UseSkillId);

    /// <summary>A BUFF potion: grants a lasting effect instantly, free of the heal cooldown.
    /// Excludes the cast-on-use scrolls (their skill has a cast time) and inert reagents.</summary>
    public static bool IsBuffPotion(ItemDef def) =>
        def.Slot == EquipSlot.Consumable && def.PotionCooldownTicks == 0
        && !string.IsNullOrEmpty(def.UseSkillId)
        && SkillCatalog.Get(def.UseSkillId) is { CastTicks: 0 };
    public static bool IsScroll(ItemDef def) => def.Slot == EquipSlot.Scroll;
    public static bool IsEnchantScroll(ItemDef def) => def.Slot == EquipSlot.Scroll && def.ScrollKind != ScrollKind.None;
    public static bool IsAttributeScroll(ItemDef def) => def.AttrScroll != AttrScrollKind.None;
    public static bool IsQuestItem(ItemDef def) => def.Slot == EquipSlot.QuestItem;
    public static bool IsEquippable(ItemDef def) => def.Slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Jewel;

    /// <summary>The level at which a grade reaches FULL power (below it you may still equip, but the
    /// GRADE PENALTY scales its combat stats down). No longer a hard equip gate — single source of truth
    /// is <see cref="GradePenalty.MinLevel"/>. F returns 0 so the UI shows no note for it.</summary>
    public static int RequiredLevel(ItemGrade grade) =>
        grade == ItemGrade.F ? 0 : GradePenalty.MinLevel(grade);
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
                F(WeaponType.TwoHandedBlunt, ItemRarity.Common, 0.16f),
                F(WeaponType.Sword, ItemRarity.Common, 0.16f),
                F(WeaponType.TwoHandedBlunt, ItemRarity.Uncommon, 0.06f),
                E(WeaponType.TwoHandedBlunt, ItemRarity.Common, 0.12f),
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
                F(WeaponType.TwoHandedBlunt, ItemRarity.Common, 0.12f),
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
