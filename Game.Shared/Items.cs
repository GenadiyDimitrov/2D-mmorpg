namespace Game.Shared;

public enum ItemGrade { F = 0, E = 1, B = 2, A = 3, S = 4 }

/// <summary>L2-style GRADE PENALTY (owner 2026-07-16, redesigned 2026-07-17).
///
/// The penalty is driven by the GAP between YOUR grade and the ITEM's grade — not by the item's grade
/// alone. Both sides sit on the same ladder of six steps (<see cref="GradeLevels"/>): F=1, E=20, D=40,
/// C=52, B=61, A=76 — the very tiers <see cref="ItemCatalog.TierLetter"/> already names. So a level-1
/// character (step 0 = F) in E gear (step 1) is ONE step over and keeps x0.5; the same character in A gear
/// (step 5) is FIVE steps over and keeps x0.1. Level up and the gap closes on its own.
///
/// ⚠ The <see cref="ItemGrade"/> enum is NOT the ladder — it has no C/D and exists for pricing/sorting
/// only. <see cref="ItemDef.ItemLevel"/> is the real tier; hand-authored items that predate tiers have no
/// ItemLevel, so they fall back to <see cref="LegacyGradeLevel"/> and behave exactly as they did.
///
/// In normal play this NEVER fires: a level-40 character wears level-40 gear (gap 0 → x1). It exists to
/// stop a level-1 twink swinging A-grade.</summary>
public static class GradePenalty
{
    /// <summary>Character level at which each grade STEP becomes "yours" (F, E, D, C, B, A).</summary>
    public static readonly int[] GradeLevels = { 1, 20, 40, 52, 61, 76 };

    /// <summary>Display letter per step, so UI never re-derives the ladder.</summary>
    public static readonly string[] GradeNames = { "F", "E", "D", "C", "B", "A" };

    /// <summary>What each GAP (steps over your grade) leaves of the affected stats. Index = gap, so
    /// index 0 (at or above your grade) is the no-op x1.</summary>
    private static readonly float[] GapFactors = { 1f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };

    /// <summary>The grade STEP (0..5) a character level sits at.</summary>
    public static int StepForLevel(int level)
    {
        int step = 0;
        for (int i = 0; i < GradeLevels.Length; i++)
            if (level >= GradeLevels[i]) step = i;
        return step;
    }

    /// <summary>Fallback grade level for the hand-authored items that carry no ItemLevel.</summary>
    private static int LegacyGradeLevel(ItemGrade g) => g switch
    {
        ItemGrade.E => 20,
        ItemGrade.B => 40,
        ItemGrade.A => 52,
        ItemGrade.S => 61,
        _ => 1,
    };

    /// <summary>The level an ITEM's grade sits at (its tier, or its legacy grade's level).</summary>
    public static int ItemGradeLevel(ItemDef def) =>
        def.ItemLevel > 0 ? def.ItemLevel : LegacyGradeLevel(def.Grade);

    /// <summary>How many grade steps this item is ABOVE the wearer (0 = no penalty).
    /// <paramref name="gradeLevelBonus"/> is the future "equip N levels early" perk: it lifts the
    /// character's effective level for grade purposes only, so the thresholds slide down for them.</summary>
    public static int Gap(ItemDef def, int level, int gradeLevelBonus = 0)
    {
        int itemStep = StepForLevel(ItemGradeLevel(def));
        int charStep = StepForLevel(level + gradeLevelBonus);
        return Math.Max(0, itemStep - charStep);
    }

    /// <summary>Multiplier this item's gap imposes (1 = none).</summary>
    public static float Factor(ItemDef def, int level, int gradeLevelBonus = 0) =>
        GapFactors[Math.Min(Gap(def, level, gradeLevelBonus), GapFactors.Length - 1)];

    /// <summary>The multiplier for an already-computed gap (1 = none).</summary>
    public static float FactorForGap(int gap) =>
        GapFactors[Math.Clamp(gap, 0, GapFactors.Length - 1)];

    /// <summary>Grade letter of an item, for UI ("A", "D", …).</summary>
    public static string GradeNameOf(ItemDef def) => GradeNames[StepForLevel(ItemGradeLevel(def))];
}

/// <summary>Item quality. The ladder is one item at six qualities (owner, 2026-07-29):
/// Common 45% / Uncommon 55% / Rare 70% / Epic 70% / Legendary 85% / Mythic 100% of the piece's full
/// stats. THE SPLIT IS AT 70%: Rare and Epic carry identical raw numbers, and Epic is where set
/// bonuses and rolled attributes switch on. Below Epic you are buying numbers; from Epic up you are
/// buying identity — which is what makes the ladder readable.
///
/// Mythic is APPENDED (5), never inserted: these values are persisted on every saved item.
/// God (99) is the untouchable debug tier and is not part of the ladder.</summary>
public enum ItemRarity { Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, Mythic = 5, God = 99 }

// Jewel = the magic-defence slot. Five DESIGNATED slots: 2 rings, 2 earrings, 1 necklace
// (see MaxOfJewelType). Equipping into a full pair displaces one rather than refusing —
// the choice is JewelStrength + "ties go to slot 1".
public enum EquipSlot { Weapon = 0, Armor = 1, Consumable = 2, Scroll = 3, QuestItem = 4, Shield = 5, Jewel = 6, Box = 7, Material = 8, Rune = 9 }

/// <summary>Jewel sub-type — limits how many can be worn: 2 Rings, 2 Earrings, 1 Necklace.</summary>
public enum JewelType { None = 0, Ring = 1, Earring = 2, Necklace = 3 }

/// <summary>Unified TOP-LEVEL item category, derived from EquipSlot (see ItemDef.Type).
/// One clean axis for grouping/filtering. A 2H weapon is MainHand AND occupies the
/// OffHand (no separate type — see ItemDef.OccupiesOffHand).</summary>
public enum ItemType { Other = 0, MainHand, OffHand, Armor, Jewel, Consumable, Scroll, Box, Quest, Material, Rune }

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

/// <summary>Attribute scroll tier (0.45.0). Each kind serves ONE grade band and does ONE
/// thing — see <see cref="AttributeSystem.ActionOf"/> / <see cref="AttributeSystem.Accepts"/>:
///   D-C-B: Common = roll a type, Uncommon = re-roll the value, Rare = re-roll in the top half.
///   A:     Epic = roll a type, Legendary = re-roll in the top half.
///   S:     Mythic = roll a type at its maximum.
/// There is no attribute LOCK any more, and outside S no scroll guarantees the top value.
/// (Epic/Mythic are appended out of ladder order because the values are persisted.)</summary>
public enum AttrScrollKind { None = 0, Common = 1, Uncommon = 2, Rare = 3, Legendary = 4, Epic = 5, Mythic = 6 }

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
    string TeachesRecipeId = "",
    // ----- RUNE (soul/spell rune). A held, non-equipped item that grants a timed buff while it's in the
    // MAIN inventory and not expired (wall-clock ExpiresAtUtc lives on the item INSTANCE, not here). The
    // buff is the named skill. Delete-protected (see the bin handler). Not equipped, not consumed. -----
    bool IsRune = false,
    string RuneBuffSkillId = "",
    // ----- BOX that grants a RUNE: seconds the granted rune lasts, stamped as ExpiresAtUtc at OPEN time
    // (so buying the sealed box doesn't start the clock). 0 = not a rune box. -----
    int GrantsRuneSeconds = 0,
    // ----- Optional blunt flavour/warning text shown in item details ("" = none; consumables fall back to
    // their use-skill's description). Used to spell out e.g. "War Runes boost PHYSICAL damage only". -----
    string Description = "")
{
    /// <summary>Hash on the ID alone. Same reason as SkillDef.GetHashCode — a positional record's
    /// generated hash is one deeply-nested expression per field, and IL2CPP turns that into C++ that
    /// can exceed clang's 256-bracket limit and break the Android build. Item DefIds are unique, so
    /// the id is the identity. Keep this override.</summary>
    public override int GetHashCode() => Id?.GetHashCode() ?? 0;

    /// <summary>Does this item merge into a single inventory ROW with a quantity, rather than
    /// occupying one row per unit? Consumables, scrolls and crafting materials stack.
    ///
    /// This lives HERE, on the shared def, because it is asked on BOTH sides and the two copies
    /// drifted: the server's AddItem stacked Material while the client's vendor did not, so a
    /// stack of 11 gems showed as "x11" but sold one at a time with no quantity numpad. Anything
    /// that needs the answer asks this property — never re-list the slots.
    ///
    /// <para>Quest items stack too, and must: a GATHERING quest hands out one token per kill, so an
    /// hour of farming is 200+ of them. One row per token would fill the bag in twenty minutes and
    /// make the feature unusable. The class-change proofs are quest items as well and are unaffected —
    /// a chain grants exactly one of each, so a stack of one is the row it always was.</para></summary>
    public bool IsStackable => Slot is EquipSlot.Consumable or EquipSlot.Scroll or EquipSlot.Material
                                    or EquipSlot.QuestItem;

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
        EquipSlot.Rune => ItemType.Rune,
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
    public const string MinorPotion = "potion_minor";      // Common HoT
    public const string HealingPotion = "potion_healing";  // Uncommon HoT
    public const string GreaterPotion = "potion_greater";  // Rare HoT
    public const string InstantPotion = "potion_instant";  // Instant %-heal panic potion
    // ---- Buff potions and scrolls (rarity = the rung on the family's ladder). A potion and a
    //      scroll of the same rarity grant the SAME buff and differ only in duration: 20 minutes
    //      vs an hour. Common sold by vendors; the rest drop or are crafted.
    //      See docs/design/BuffLadders.md. ----
    public const string SpeedPotionC = "potion_speed_c";   // Swift    (move speed)
    public const string SpeedPotionU = "potion_speed_u";
    public const string SpeedPotionR = "potion_speed_r";
    public const string CastPotionC = "potion_cast_c";     // Alacrity (cast speed)
    public const string CastPotionU = "potion_cast_u";
    public const string CastPotionR = "potion_cast_r";
    public const string AtkPotionC = "potion_atk_c";       // Haste    (attack speed)
    public const string AtkPotionU = "potion_atk_u";
    public const string AtkPotionR = "potion_atk_r";
    public const string EvaPotionC = "potion_eva_c";       // Agility  (evasion)
    public const string EvaPotionU = "potion_eva_u";
    public const string EvaPotionR = "potion_eva_r";
    public const string SpeedScrollC = "scroll_speed_c";
    public const string SpeedScrollU = "scroll_speed_u";
    public const string SpeedScrollR = "scroll_speed_r";
    public const string CastScrollC = "scroll_cast_c";
    public const string CastScrollU = "scroll_cast_u";
    public const string CastScrollR = "scroll_cast_r";
    public const string AtkScrollC = "scroll_atk_c";
    public const string AtkScrollU = "scroll_atk_u";
    public const string AtkScrollR = "scroll_atk_r";
    public const string EvaScrollC = "scroll_eva_c";
    public const string EvaScrollU = "scroll_eva_u";
    public const string EvaScrollR = "scroll_eva_r";
    // ---- The rest of the buff ladders (docs/design/BuffLadders.md, step 6). Four families with
    //      a potion AND a scroll (Common/Uncommon/Rare), then eight SCROLL-ONLY families whose
    //      cheapest rung is Epic — that is what "no potion analogue" buys you. Item ids spell the
    //      STAT, not the buff's display name, which has already changed twice. ----
    public const string MightPotionC = "potion_patk_c";
    public const string MightPotionU = "potion_patk_u";
    public const string MightPotionR = "potion_patk_r";
    public const string MightScrollC = "scroll_patk_c";
    public const string MightScrollU = "scroll_patk_u";
    public const string MightScrollR = "scroll_patk_r";
    public const string BulwarkPotionC = "potion_pdef_c";
    public const string BulwarkPotionU = "potion_pdef_u";
    public const string BulwarkPotionR = "potion_pdef_r";
    public const string BulwarkScrollC = "scroll_pdef_c";
    public const string BulwarkScrollU = "scroll_pdef_u";
    public const string BulwarkScrollR = "scroll_pdef_r";
    public const string ForcePotionC = "potion_matk_c";
    public const string ForcePotionU = "potion_matk_u";
    public const string ForcePotionR = "potion_matk_r";
    public const string ForceScrollC = "scroll_matk_c";
    public const string ForceScrollU = "scroll_matk_u";
    public const string ForceScrollR = "scroll_matk_r";
    public const string WardPotionC = "potion_mdef_c";
    public const string WardPotionU = "potion_mdef_u";
    public const string WardPotionR = "potion_mdef_r";
    public const string WardScrollC = "scroll_mdef_c";
    public const string WardScrollU = "scroll_mdef_u";
    public const string WardScrollR = "scroll_mdef_r";
    public const string AimPotionC = "potion_acc_c";
    public const string AimPotionU = "potion_acc_u";
    public const string AimPotionR = "potion_acc_r";
    public const string AimScrollC = "scroll_acc_c";
    public const string AimScrollU = "scroll_acc_u";
    public const string AimScrollR = "scroll_acc_r";
    // Scroll-only: Epic / Legendary / Mythic = rungs 2 / 4 / 6 of a six-rung ladder.
    public const string BodyScrollE = "scroll_hp_e";
    public const string BodyScrollL = "scroll_hp_l";
    public const string BodyScrollM = "scroll_hp_m";
    public const string SoulScrollE = "scroll_mp_e";
    public const string SoulScrollL = "scroll_mp_l";
    public const string SoulScrollM = "scroll_mp_m";
    public const string VigorScrollE = "scroll_hpreg_e";
    public const string VigorScrollL = "scroll_hpreg_l";
    public const string VigorScrollM = "scroll_hpreg_m";
    public const string SerenityScrollE = "scroll_mpreg_e";
    public const string SerenityScrollL = "scroll_mpreg_l";
    public const string SerenityScrollM = "scroll_mpreg_m";
    public const string FocusScrollE = "scroll_crit_e";
    public const string FocusScrollL = "scroll_crit_l";
    public const string FocusScrollM = "scroll_crit_m";
    public const string FerocityScrollE = "scroll_critdmg_e";
    public const string FerocityScrollL = "scroll_critdmg_l";
    public const string FerocityScrollM = "scroll_critdmg_m";
    public const string InsightScrollE = "scroll_mcrit_e";
    public const string InsightScrollL = "scroll_mcrit_l";
    public const string InsightScrollM = "scroll_mcrit_m";
    public const string FrenzyScrollE = "scroll_frenzy_e";
    public const string FrenzyScrollL = "scroll_frenzy_l";
    public const string FrenzyScrollM = "scroll_frenzy_m";
    // Dash — the short sprint burst, six rarities, no scroll.
    public const string DashPotionC = "potion_dash_c";
    public const string DashPotionU = "potion_dash_u";
    public const string DashPotionR = "potion_dash_r";
    public const string DashPotionE = "potion_dash_e";
    public const string DashPotionL = "potion_dash_l";
    public const string DashPotionM = "potion_dash_m";
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
    public const string AttrScrollEpic = "attrscroll_epic";
    public const string AttrScrollMythic = "attrscroll_mythic";
    public const string MarkOfFaith = "quest_mark_of_faith";
    public const string ClericsProof = "quest_clerics_proof";

    // ----- Repeatable-hunt gathering tokens (Quests.Repeatable.cs authors which creature drops which).
    //       Named after the creature, not the quest: the Huntmaster's list can be re-cut without the
    //       token in the player's bag suddenly meaning something else.
    public const string TokenFoxPelt = "quest_token_fox_pelt";
    public const string TokenWerewolfFang = "quest_token_werewolf_fang";
    public const string TokenSpiderHook = "quest_token_spider_hook";
    public const string TokenCrackedRib = "quest_token_cracked_rib";
    public const string TokenBearPelt = "quest_token_bear_pelt";
    public const string TokenMantisClaw = "quest_token_mantis_claw";
    public const string TokenHarpyFeather = "quest_token_harpy_feather";
    public const string TokenBasiliskScale = "quest_token_basilisk_scale";
    public const string TokenAshOrcInsignia = "quest_token_ash_orc_insignia";
    public const string TokenRustedShard = "quest_token_rusted_shard";
    public const string TokenDreadSigil = "quest_token_dread_sigil";
    public const string TokenRedhornBadge = "quest_token_redhorn_badge";
    public const string TokenEmberScale = "quest_token_ember_scale";
    public const string TokenRadiantPlume = "quest_token_radiant_plume";
    public const string TokenSplinterChitin = "quest_token_splinter_chitin";

    /// <summary>Every gathering token and its display name, in one list so the defs are built from the
    /// same place the ids are declared.</summary>
    private static readonly (string Id, string Name)[] GatherTokens =
    {
        (TokenFoxPelt,        "Fox Pelt"),
        (TokenWerewolfFang,   "Werewolf Fang"),
        (TokenSpiderHook,     "Barbed Hook"),
        (TokenCrackedRib,     "Cracked Rib"),
        (TokenBearPelt,       "Bear Pelt"),
        (TokenMantisClaw,     "Mantis Claw"),
        (TokenHarpyFeather,   "Harpy Feather"),
        (TokenBasiliskScale,  "Amber Scale"),
        (TokenAshOrcInsignia, "Ash Orc Insignia"),
        (TokenRustedShard,    "Rusted Shard"),
        (TokenDreadSigil,     "Dread Sigil"),
        (TokenRedhornBadge,   "Redhorn Badge"),
        (TokenEmberScale,     "Emberwyrm Scale"),
        (TokenRadiantPlume,   "Radiant Plume"),
        (TokenSplinterChitin, "Splinter Chitin"),
    };
    public const string GodWeapon = "god_judgment";
    public const string GodArmor = "god_robes";
    public const string WoodenShield = "shield_wooden";
    public const string IronShield = "shield_iron";
    public const string BrassAmulet = "jewel_brass_amulet";
    public const string SilverTalisman = "jewel_silver_talisman";
    public const string IronMace = "blunt_1h_iron_mace";        // 1H physical blunt (shield-ok)
    public const string AshWand = "blunt_1h_ash_wand";          // 1H magic blunt (mAtk > pAtk)
    public const string ElementalStone = "elemental_stone";     // reagent for Elemental Burst
    public const string SkillStone = "skill_stone";             // reagent for skill costs (e.g. Angel's Protection)
    /// <summary>How much of your ATK power a weapon lets through to the channel it does NOT
    /// exist to serve: a sword's magic, a staff's melee. THE tuning knob for weapon identity.
    /// 0.6 reproduces the gear CSV's second column (a sword's 92 P.Atk × 0.6 ≈ its authored 54
    /// M.Atk). NOTE: 0.6 does NOT yet close the buffer's staff→2H-sword swap — because magic
    /// damage goes as √mAtk, he loses only ~15% of it while doubling P.Atk. Dropping this to
    /// ~0.2-0.3 makes that a real trade. Any single weapon can override via its own
    /// PAtkFactor/MAtkFactor.</summary>
    public const float OffChannelFactor = 0.6f;

    // ---- TRAINING tier: levels 1-10, the WEAKEST gear in the game (owner, 2026-07-24) ----
    // A new character starts with these instead of the Newbie set. The Newbie gear did not go away —
    // it became the reward for the level-10 starter QUEST, so the first ten levels are played in kit
    // that cannot one-shot a mob. Bought at any weapon/armor vendor for 400g so a death or a bad pick
    // is recoverable. Untradeable and attribute-less like all starter gear; unlike the Newbie tier
    // these DO have a buy price, because being able to replace them is the point.
    public const string TrainingSword   = "training_sword";
    public const string TrainingClub    = "training_club";
    public const string TrainingKnives  = "training_knives";
    public const string TrainingBow     = "training_bow";
    public const string TrainingWand    = "training_wand";
    public const string TrainingLeather = "training_leather_armor";
    public const string TrainingRobe    = "training_robe";
    /// <summary>Price of every training weapon and armor at a vendor (owner: 400g each).</summary>
    public const int TrainingGearPrice = 400;

    // ---- BROKEN jewels: the level 1-5 drop line, and the only jewels a new character can get ----
    // The Newbie jewels box is gone from character creation (owner: "no shot/jewels"). These drop from
    // level 1-5 mobs and are sold in the shop, so the first accessories are EARNED. Tradable, unlike
    // the bound starter kit — an early player having something worth selling is the point.
    public const string BrokenEarring  = "broken_earring";
    public const string BrokenRing     = "broken_ring";
    public const string BrokenNecklace = "broken_necklace";

    /// <summary>Training WEAPONS selection box — pick ONE of the five. Given at character creation.</summary>
    public const string BoxTrainingWeapons = "box_training_weapons";
    /// <summary>Training ARMOR choice — pick leather (fighter) or robe (mage). Given at creation.</summary>
    public const string BoxTrainingArmorChoice = "box_training_armor_choice";

    // ===== THE NEWBIE KIT *IS* THE F-GRADE TOP (owner, 2026-07-30) ============================
    // "Make the newbie gear the Ferrite Mythic — it's the top for its grade."
    //
    // These were a parallel item line sitting beside the real ladder at the same levels. They are now
    // ALIASES onto the F tier's MYTHIC rung — the authored F piece, themed "Ferrite" by GradeTheme — so
    // the level-10 quest hands out the best F-grade gear there is instead of a separate set that has to
    // be kept in step with it by hand. The F tier's Mythic numbers were authored FROM these, so nothing
    // got stronger or weaker in the swap; there is simply one item where there were two.
    //
    // The names change with it (Newbie Sword → Ferrite Blade), which is the point: it is a real rung on
    // the ladder, not a tutorial prop.
    public const string NewbieSword1H = "sword1h_t1";
    public const string NewbieDaggers = "duals_t1";
    public const string NewbieSword2H = "sword2h_t1";
    public const string NewbieBow     = "bow_t1";
    public const string NewbieStaff   = "staff_t1";
    public const string NewbieLightBody   = "light_t1";
    public const string NewbieRobeBody    = "robe_t1";
    // SHARED accessories — used by BOTH the light and robe newbie sets.
    public const string NewbieHelm        = "helm_t1";
    public const string NewbieGloves      = "gloves_t1";
    public const string NewbieBoots       = "boots_t1";
    public const string NewbieEarring     = "earring_t1";
    public const string NewbieRing        = "ring_t1";
    public const string NewbieNecklace    = "necklace_t1";
    // RUNES + their sealed boxes (open → rune, wall-clock expiry set on open).
    public const string WarRune      = "rune_war";
    public const string SpellRune    = "rune_spell";
    public const string BoxWarRune1h     = "box_war_rune_1h";
    public const string BoxWarRune2h     = "box_war_rune_2h";
    public const string BoxWarRune24h    = "box_war_rune_24h";
    public const string BoxWarRune30d    = "box_war_rune_30d";
    public const string BoxSpellRune1h   = "box_spell_rune_1h";
    public const string BoxSpellRune2h   = "box_spell_rune_2h";
    public const string BoxSpellRune24h  = "box_spell_rune_24h";
    public const string BoxSpellRune30d  = "box_spell_rune_30d";
    // Newbie CHOICE selection-boxes (pick one of two sub-boxes).
    public const string BoxNewbieArmorChoice = "box_newbie_armor_choice";
    public const string BoxNewbieRuneChoice  = "box_newbie_rune_choice";
    /// <summary>The Apothecary's daily reward — pick soul or spirit, 1 hour, untradable.</summary>
    public const string BoxDailyRuneChoice   = "box_daily_rune_choice";
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
        // Flat heal-over-time tiers (owner, 2026-07-23). PotionCooldownTicks is the PER-POTION drink
        // cooldown (each tier independent): Common/Uncommon 10s, Rare 20s, Instant 60s. It also marks the
        // item as a heal potion (IsHealPotion). Rarer potions restore more per second and last longer.
        list.Add(new ItemDef(MinorPotion, "Common Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common,
            UseSkillId: SkillCatalog.PotHealMinor, PotionCooldownTicks: 100, Value: 60));
        list.Add(new ItemDef(HealingPotion, "Uncommon Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon,
            UseSkillId: SkillCatalog.PotHeal, PotionCooldownTicks: 100, Value: 250));
        list.Add(new ItemDef(GreaterPotion, "Rare Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            UseSkillId: SkillCatalog.PotHealGreater, PotionCooldownTicks: 200, Value: 1500));
        // The instant panic potion: +30% max HP, 60s cooldown, rare quality. (Meant for the future
        // boss/challenge-point shop; a gold value stands in until that exists.)
        list.Add(new ItemDef(InstantPotion, "Instant Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            UseSkillId: SkillCatalog.PotHealInstant, PotionCooldownTicks: 600, Value: 5000));

        // ----- RUNES + their boxes. The rune is HELD (not equipped/consumed): while it's in the main
        // inventory and unexpired the reconciliation loop keeps its buff up. Tradable:false (can't sell/
        // trade) AND delete-protected in the bin handler (IsRune). The box is sealed — OPENING it stamps
        // ExpiresAtUtc = now + GrantsRuneSeconds, so the clock starts on open, not on purchase. -----
        // GrantsRuneSeconds on the RUNE is its DEFAULT lifespan, stamped at ACQUIRE time (AddItem) for any
        // source — a drop, a direct give, a quest — not just a box. A box that grants the rune OVERRIDES
        // this with its own duration. So a rune always has a wall-clock expiry the moment you get it.
        list.Add(new ItemDef(WarRune, "War Rune", EquipSlot.Rune, ItemGrade.F, ItemRarity.Rare,
            IsRune: true, RuneBuffSkillId: SkillCatalog.WarRuneBuff, GrantsRuneSeconds: 3600,
            Tradable: false, Value: 0,
            Description: "Held rune: +100% P.Atk while in your bag. Boosts PHYSICAL damage only (melee/bow) — useless for spells. Move it to the warehouse to switch it off; it can't be deleted."));
        list.Add(new ItemDef(SpellRune, "Spell Rune", EquipSlot.Rune, ItemGrade.F, ItemRarity.Rare,
            IsRune: true, RuneBuffSkillId: SkillCatalog.SpellRuneBuff, GrantsRuneSeconds: 3600,
            Tradable: false, Value: 0,
            Description: "Held rune: +magic damage & cast speed while in your bag. Boosts MAGIC (spells) only — useless for melee/bow. Move it to the warehouse to switch it off; it can't be deleted."));

        // Sealed rune boxes. 1h/2h are vendor-stocked (Apothecary, real gold price) and TRADABLE (giftable
        // sealed — the RUNE inside is still bound). 24h/30d are premium/pass items: not buyable (BuyPrice
        // -1) and NOT tradable. Prices/tradability are a stand-in for the future premium economy.
        const int H = 3600, D = 24 * 3600;
        void RuneBox(string id, string name, int seconds, int buyPrice, bool tradable, string desc) =>
            list.Add(new ItemDef(id, name, EquipSlot.Box, ItemGrade.F, ItemRarity.Rare,
                GrantsRuneSeconds: seconds, Tradable: tradable, BuyPriceOverride: buyPrice, SellPriceOverride: 0,
                Description: desc));
        RuneBox(BoxWarRune1h,  "War Rune Box (1h)",  1 * H, 150000, true,  "Opens to a War Rune lasting 1 hour. War Runes boost PHYSICAL damage only (melee/bow) — useless for spells.");
        RuneBox(BoxWarRune2h,  "War Rune Box (2h)",  2 * H, 280000, true,  "Opens to a War Rune lasting 2 hours. War Runes boost PHYSICAL damage only (melee/bow) — useless for spells.");
        RuneBox(BoxWarRune24h, "War Rune Box (1d)",  1 * D, -1,  false,  "Opens to a War Rune lasting 24 hours. War Runes boost PHYSICAL damage only (melee/bow) — useless for spells.");
        RuneBox(BoxWarRune30d, "War Rune Box (30d)", 30 * D, -1, false,  "Opens to a War Rune lasting 30 days. War Runes boost PHYSICAL damage only (melee/bow) — useless for spells.");
        RuneBox(BoxSpellRune1h,  "Spell Rune Box (1h)",  1 * H, 150000, true,  "Opens to a Spell Rune lasting 1 hour. Spell Runes boost MAGIC (spells) only — useless for melee/bow.");
        RuneBox(BoxSpellRune2h,  "Spell Rune Box (2h)",  2 * H, 280000, true,  "Opens to a Spell Rune lasting 2 hours. Spell Runes boost MAGIC (spells) only — useless for melee/bow.");
        RuneBox(BoxSpellRune24h, "Spell Rune Box (1d)",  1 * D, -1,  false,  "Opens to a Spell Rune lasting 24 hours. Spell Runes boost MAGIC (spells) only — useless for melee/bow.");
        RuneBox(BoxSpellRune30d, "Spell Rune Box (30d)", 30 * D, -1, false,  "Opens to a Spell Rune lasting 30 days. Spell Runes boost MAGIC (spells) only — useless for melee/bow.");

        // Newbie CHOICE selection-boxes (untradable): armor set (fighter vs mage) and a 1-day rune
        // (soul vs spirit). Each is a PickCount:1 box whose OPTIONS are other boxes — pick one, open it.
        list.Add(new ItemDef(BoxNewbieArmorChoice, "Newbie Armor Set", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, Description: "Choose ONE: a Fighter (light) or Mage (robe) armor set."));
        list.Add(new ItemDef(BoxNewbieRuneChoice, "Newbie Rune", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, Description: "Choose ONE: a 1-day War Rune (physical) or Spell Rune (magic) rune box."));
        // The DAILY quest reward. Untradable and worth nothing at a vendor, unlike the 1h boxes the
        // Apothecary SELLS: a free daily that could be farmed across characters and sold on would be a
        // gold faucet rather than a leg-up (owner: quest rune boxes untradable, bought ones not).
        list.Add(new ItemDef(BoxDailyRuneChoice, "Rune Box (1h) — Daily", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, BuyPriceOverride: -1, SellPriceOverride: 0,
            Description: "Choose ONE: a 1-hour War Rune (physical) or Spell Rune (magic) rune box. "
                       + "Untradable — the Apothecary gives these out, she does not sell them."));

        // Return scrolls: same mechanism, but their skill has a CAST time, so double-clicking one
        // channels it. The skills are NOT learned — the ITEM is what grants them.
        // Sellable as of 2026-07-31 (playtest-15): it is tradable and drops constantly, so refusing it
        // at the vendor read as a bug. It sells through the use-consumable /25 rule (500 -> 20), not the
        // generic 30 %, so making it sellable does not re-open the faucet playtest-14 just closed.
        list.Add(new ItemDef(ScrollReturn, "Scroll of Return", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common,
            UseSkillId: SkillCatalog.ScrollReturnSkill, Value: 500));
        list.Add(new ItemDef(ScrollReturnUltimate, "Ultimate Scroll of Return", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            UseSkillId: SkillCatalog.ScrollReturnUltSkill,
            Tradable: false, BuyPriceOverride: -1, SellPriceOverride: 0));

        // Resurrection scrolls: used WHILE DEAD to self-revive (their skill channels a cast). Basic
        // restores no exp (1500g vendor); the ultimate restores all lost exp (not shop-stocked).
        // Sellable too, same rule and same reason as the Scroll of Return (1500 -> 60). The UNTRADABLE
        // ultimates below keep SellPriceOverride: 0 — the owner's complaint was specifically about
        // scrolls that are tradable yet still refused at the counter.
        list.Add(new ItemDef(ScrollResurrect, "Scroll of Resurrection", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon,
            UseSkillId: SkillCatalog.ScrollResurrectSkill, Value: 1500));
        list.Add(new ItemDef(ScrollResurrectUltimate, "Ultimate Scroll of Resurrection", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            UseSkillId: SkillCatalog.ScrollResurrectUltSkill,
            Tradable: false, BuyPriceOverride: -1, SellPriceOverride: 0));

        // Elemental Stone — a crafting/reagent material (not drinkable). Stacks; consumed
        // by skills that list it as a ConsumableId (nuker's Elemental Burst = 1/cast). Vendor 20k.
        list.Add(new ItemDef(ElementalStone, "Elemental Stone", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, Value: 100, BuyPriceOverride: 20000));

        // Skill Stone — cheap reagent consumed by skills that cost stones (e.g. Angel's Protection = 5/cast).
        // Not free, not expensive: 400g at the vendor. Stacks; not drinkable.
        list.Add(new ItemDef(SkillStone, "Skill Stone", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, Value: 400));

        // ----- Buff potions and scrolls: consume to gain one SINGLE buff off a family's ladder
        //       (docs/design/BuffLadders.md). Rarity IS the rung, so a Greater potion supersedes a
        //       Lesser one — and equally supersedes the same rung of a cleric's blessing, because
        //       they are literally the same buff. No heal cooldown; 1s reuse.
        //       Common (Lesser) potions are vendor-sold staples; the rest drop or are crafted.
        //       A scroll is the same rung for an HOUR instead of 20 minutes, so it costs 3x. -----
        list.Add(new ItemDef(SpeedPotionC, "Swift Potion (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.PotSwiftC, Value: 1500));
        list.Add(new ItemDef(SpeedPotionU, "Swift Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.PotSwiftU, Value: 5000));
        list.Add(new ItemDef(SpeedPotionR, "Swift Potion (Greater)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, UseSkillId: SkillCatalog.PotSwiftR, Value: 12000));
        list.Add(new ItemDef(CastPotionC, "Alacrity Potion (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.PotAlacrityC, Value: 1500));
        list.Add(new ItemDef(CastPotionU, "Alacrity Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.PotAlacrityU, Value: 5000));
        list.Add(new ItemDef(CastPotionR, "Alacrity Potion (Greater)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, UseSkillId: SkillCatalog.PotAlacrityR, Value: 12000));
        list.Add(new ItemDef(AtkPotionC, "Haste Potion (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.PotHasteC, Value: 1500));
        list.Add(new ItemDef(AtkPotionU, "Haste Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.PotHasteU, Value: 5000));
        list.Add(new ItemDef(AtkPotionR, "Haste Potion (Greater)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, UseSkillId: SkillCatalog.PotHasteR, Value: 12000));
        list.Add(new ItemDef(EvaPotionC, "Agility Potion (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.PotAgilityC, Value: 1500));
        list.Add(new ItemDef(EvaPotionU, "Agility Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.PotAgilityU, Value: 5000));
        list.Add(new ItemDef(EvaPotionR, "Agility Potion (Greater)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, UseSkillId: SkillCatalog.PotAgilityR, Value: 12000));

        list.Add(new ItemDef(SpeedScrollC, "Scroll of Swift (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.ScrSwiftC, Value: 4500));
        list.Add(new ItemDef(SpeedScrollU, "Scroll of Swift", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.ScrSwiftU, Value: 15000));
        list.Add(new ItemDef(SpeedScrollR, "Scroll of Swift (Greater)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, UseSkillId: SkillCatalog.ScrSwiftR, Value: 36000));
        list.Add(new ItemDef(CastScrollC, "Scroll of Alacrity (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.ScrAlacrityC, Value: 4500));
        list.Add(new ItemDef(CastScrollU, "Scroll of Alacrity", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.ScrAlacrityU, Value: 15000));
        list.Add(new ItemDef(CastScrollR, "Scroll of Alacrity (Greater)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, UseSkillId: SkillCatalog.ScrAlacrityR, Value: 36000));
        list.Add(new ItemDef(AtkScrollC, "Scroll of Haste (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.ScrHasteC, Value: 4500));
        list.Add(new ItemDef(AtkScrollU, "Scroll of Haste", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.ScrHasteU, Value: 15000));
        list.Add(new ItemDef(AtkScrollR, "Scroll of Haste (Greater)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, UseSkillId: SkillCatalog.ScrHasteR, Value: 36000));
        list.Add(new ItemDef(EvaScrollC, "Scroll of Agility (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.ScrAgilityC, Value: 4500));
        list.Add(new ItemDef(EvaScrollU, "Scroll of Agility", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.ScrAgilityU, Value: 15000));
        list.Add(new ItemDef(EvaScrollR, "Scroll of Agility (Greater)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, UseSkillId: SkillCatalog.ScrAgilityR, Value: 36000));

        // ----- The other buff ladders (BuffLadders.md step 6). Same shape as the four speed
        //       families above, so the prices are the same ladder: potion 1.5k/5k/12k, scroll
        //       3x that. The SCROLL-ONLY families have no cheap rung at all — their entry price
        //       is Epic, which is the point: no potion analogue means you pay for the hour.
        //       One line per rarity, so a price change is a one-line edit. -----
        void BuffPotion(string id, string name, ItemRarity rarity, string skill) =>
            list.Add(new ItemDef(id, name, EquipSlot.Consumable, ItemGrade.F, rarity,
                UseSkillId: skill, Value: rarity switch
                {
                    ItemRarity.Common => 1500, ItemRarity.Uncommon => 5000, _ => 12000,
                }));

        void BuffScroll(string id, string name, ItemRarity rarity, string skill) =>
            list.Add(new ItemDef(id, name, EquipSlot.Consumable, ItemGrade.F, rarity,
                UseSkillId: skill, Value: rarity switch
                {
                    ItemRarity.Common => 4500, ItemRarity.Uncommon => 15000, ItemRarity.Rare => 36000,
                    ItemRarity.Epic => 75000, ItemRarity.Legendary => 150000, _ => 300000,
                }));

        BuffPotion(MightPotionC, "Might Potion (Lesser)",  ItemRarity.Common,   SkillCatalog.PotMightC);
        BuffPotion(MightPotionU, "Might Potion",           ItemRarity.Uncommon, SkillCatalog.PotMightU);
        BuffPotion(MightPotionR, "Might Potion (Greater)", ItemRarity.Rare,     SkillCatalog.PotMightR);
        BuffScroll(MightScrollC, "Scroll of Might (Lesser)",  ItemRarity.Common,   SkillCatalog.ScrMightC);
        BuffScroll(MightScrollU, "Scroll of Might",           ItemRarity.Uncommon, SkillCatalog.ScrMightU);
        BuffScroll(MightScrollR, "Scroll of Might (Greater)", ItemRarity.Rare,     SkillCatalog.ScrMightR);

        BuffPotion(BulwarkPotionC, "Bulwark Potion (Lesser)",  ItemRarity.Common,   SkillCatalog.PotBulwarkC);
        BuffPotion(BulwarkPotionU, "Bulwark Potion",           ItemRarity.Uncommon, SkillCatalog.PotBulwarkU);
        BuffPotion(BulwarkPotionR, "Bulwark Potion (Greater)", ItemRarity.Rare,     SkillCatalog.PotBulwarkR);
        BuffScroll(BulwarkScrollC, "Scroll of Bulwark (Lesser)",  ItemRarity.Common,   SkillCatalog.ScrBulwarkC);
        BuffScroll(BulwarkScrollU, "Scroll of Bulwark",           ItemRarity.Uncommon, SkillCatalog.ScrBulwarkU);
        BuffScroll(BulwarkScrollR, "Scroll of Bulwark (Greater)", ItemRarity.Rare,     SkillCatalog.ScrBulwarkR);

        BuffPotion(ForcePotionC, "Force Potion (Lesser)",  ItemRarity.Common,   SkillCatalog.PotForceC);
        BuffPotion(ForcePotionU, "Force Potion",           ItemRarity.Uncommon, SkillCatalog.PotForceU);
        BuffPotion(ForcePotionR, "Force Potion (Greater)", ItemRarity.Rare,     SkillCatalog.PotForceR);
        BuffScroll(ForceScrollC, "Scroll of Force (Lesser)",  ItemRarity.Common,   SkillCatalog.ScrForceC);
        BuffScroll(ForceScrollU, "Scroll of Force",           ItemRarity.Uncommon, SkillCatalog.ScrForceU);
        BuffScroll(ForceScrollR, "Scroll of Force (Greater)", ItemRarity.Rare,     SkillCatalog.ScrForceR);

        BuffPotion(WardPotionC, "Ward Potion (Lesser)",  ItemRarity.Common,   SkillCatalog.PotWardC);
        BuffPotion(WardPotionU, "Ward Potion",           ItemRarity.Uncommon, SkillCatalog.PotWardU);
        BuffPotion(WardPotionR, "Ward Potion (Greater)", ItemRarity.Rare,     SkillCatalog.PotWardR);
        BuffScroll(WardScrollC, "Scroll of Ward (Lesser)",  ItemRarity.Common,   SkillCatalog.ScrWardC);
        BuffScroll(WardScrollU, "Scroll of Ward",           ItemRarity.Uncommon, SkillCatalog.ScrWardU);
        BuffScroll(WardScrollR, "Scroll of Ward (Greater)", ItemRarity.Rare,     SkillCatalog.ScrWardR);

        // Aim — accuracy, the mirror of the Agility (evasion) line and priced identically.
        BuffPotion(AimPotionC, "Aim Potion (Lesser)",  ItemRarity.Common,   SkillCatalog.PotAimC);
        BuffPotion(AimPotionU, "Aim Potion",           ItemRarity.Uncommon, SkillCatalog.PotAimU);
        BuffPotion(AimPotionR, "Aim Potion (Greater)", ItemRarity.Rare,     SkillCatalog.PotAimR);
        BuffScroll(AimScrollC, "Scroll of Aim (Lesser)",  ItemRarity.Common,   SkillCatalog.ScrAimC);
        BuffScroll(AimScrollU, "Scroll of Aim",           ItemRarity.Uncommon, SkillCatalog.ScrAimU);
        BuffScroll(AimScrollR, "Scroll of Aim (Greater)", ItemRarity.Rare,     SkillCatalog.ScrAimR);

        // Scroll-only. Rarity here is the PRICE tier, not the power rung — see BuffLadders.md.
        void ScrollOnly(string e, string l, string m, string name, string se, string sl, string sm)
        {
            BuffScroll(e, $"Scroll of {name} (Superior)", ItemRarity.Epic, se);
            BuffScroll(l, $"Scroll of {name} (Grand)",    ItemRarity.Legendary, sl);
            BuffScroll(m, $"Scroll of {name} (Supreme)",  ItemRarity.Mythic, sm);
        }

        ScrollOnly(BodyScrollE, BodyScrollL, BodyScrollM, "Body",
            SkillCatalog.ScrBodyE, SkillCatalog.ScrBodyL, SkillCatalog.ScrBodyM);
        ScrollOnly(SoulScrollE, SoulScrollL, SoulScrollM, "Soul",
            SkillCatalog.ScrSoulE, SkillCatalog.ScrSoulL, SkillCatalog.ScrSoulM);
        ScrollOnly(VigorScrollE, VigorScrollL, VigorScrollM, "Vigor",
            SkillCatalog.ScrVigorE, SkillCatalog.ScrVigorL, SkillCatalog.ScrVigorM);
        ScrollOnly(SerenityScrollE, SerenityScrollL, SerenityScrollM, "Serenity",
            SkillCatalog.ScrSerenityE, SkillCatalog.ScrSerenityL, SkillCatalog.ScrSerenityM);
        ScrollOnly(FocusScrollE, FocusScrollL, FocusScrollM, "Focus",
            SkillCatalog.ScrFocusE, SkillCatalog.ScrFocusL, SkillCatalog.ScrFocusM);
        ScrollOnly(FerocityScrollE, FerocityScrollL, FerocityScrollM, "Ferocity",
            SkillCatalog.ScrFerocityE, SkillCatalog.ScrFerocityL, SkillCatalog.ScrFerocityM);
        ScrollOnly(InsightScrollE, InsightScrollL, InsightScrollM, "Insight",
            SkillCatalog.ScrInsightE, SkillCatalog.ScrInsightL, SkillCatalog.ScrInsightM);
        ScrollOnly(FrenzyScrollE, FrenzyScrollL, FrenzyScrollM, "Frenzy",
            SkillCatalog.ScrFrenzyE, SkillCatalog.ScrFrenzyL, SkillCatalog.ScrFrenzyM);

        // Dash — 15 seconds of sprint on a 1-minute reuse, six rarities, no scroll. Priced at half
        // a buff potion of the same rarity: it is a burst, not a blessing.
        list.Add(new ItemDef(DashPotionC, "Dash Potion (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.PotDashC, Value: 750));
        list.Add(new ItemDef(DashPotionU, "Dash Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.PotDashU, Value: 2500));
        list.Add(new ItemDef(DashPotionR, "Dash Potion (Greater)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, UseSkillId: SkillCatalog.PotDashR, Value: 6000));
        list.Add(new ItemDef(DashPotionE, "Dash Potion (Superior)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Epic, UseSkillId: SkillCatalog.PotDashE, Value: 12500));
        list.Add(new ItemDef(DashPotionL, "Dash Potion (Grand)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Legendary, UseSkillId: SkillCatalog.PotDashL, Value: 25000));
        list.Add(new ItemDef(DashPotionM, "Dash Potion (Supreme)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Mythic, UseSkillId: SkillCatalog.PotDashM, Value: 50000));

        // ===================================================================
        //  SHIELDS — equippable by any class (with a one-hand weapon), but only
        //  tanks make them matter via Shield Mastery passives. Base values are
        //  modest; passives/buffs scale them. Block = flat % damage reduction.
        // ===================================================================
        // The Wooden Shield is STARTER kit, so it sits below the F ladder the same way the training
        // armor does (owner, 2026-07-31): 40 -> 35. At 40 it equalled the F-tier Ferrite Aegis, so the
        // first shield you could loot was never an upgrade — the identical bug the training armor had.
        list.Add(new ItemDef(WoodenShield, "Wooden Shield", EquipSlot.Shield,
            ItemGrade.F, ItemRarity.Common,
            BlockChance: 0.15f, BlockReduction: 0.30f, ShieldDefense: 35,
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

        // ===================================================================
        //  TRAINING tier — levels 1-10, the weakest gear in the game (owner). Roughly a QUARTER of the
        //  Newbie weapons' power, so a starting character cannot one-shot its way through the first zones.
        //  Buyable at 400g; untradeable and attribute-less like the rest of the starter kit.
        //
        //  The owner authored these as "P.Atk / M.Atk" pairs: sword 6/5, club 6/5, knives 5/5,
        //  bow 11/5, wand 6/7 — and as of 2026-07-31 (playtest-15) BOTH numbers are authored, exactly
        //  as the level-tier ladder below already does (P -> AtkBonus, M -> MAtkBonus, factors 1.0).
        //
        //  This tier was simply left behind by the 2026-07-24 migration: it still carried one power
        //  number and reconstructed the M column with OffChannelFactor, which meant no training weapon
        //  ever showed an M.Atk line on its card and a mage's starter numbers were whatever the factor
        //  happened to produce. The old objection here — "daggers with MAtkFactor 1.0 cast as well as a
        //  staff" — does not apply to authored numbers: knives are 5/5 because that is what was written,
        //  not because a factor let the shared base leak through. Weapon identity now lives in the
        //  class passive and IsMagicWeapon, per the note on the ladder below.
        list.Add(new ItemDef(TrainingSword, "Training Sword", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Sword,
            AtkBonus: 6, MAtkBonus: 5,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: TrainingGearPrice, NoAttributes: true,
            Description: "Blunt-edged practice sword. The weakest blade there is — replace it as soon as you can."));
        list.Add(new ItemDef(TrainingClub, "Training Club", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Blunt,
            AtkBonus: 6, MAtkBonus: 5,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: TrainingGearPrice, NoAttributes: true,
            Description: "A weighted stick. It hits about as well as you would expect."));
        list.Add(new ItemDef(TrainingKnives, "Training Knives", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Dual,
            AtkBonus: 5, MAtkBonus: 5,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: TrainingGearPrice, NoAttributes: true,
            Description: "Dulled practice knives. Fast, and barely dangerous."));
        list.Add(new ItemDef(TrainingBow, "Training Bow", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Bow,
            AtkBonus: 11, MAtkBonus: 5, WeaponRange: 400,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: TrainingGearPrice, NoAttributes: true,
            Description: "A short practice bow. Hits harder than the melee training gear, from a distance."));
        // The wand's +6 MaxMP is GONE (owner, 2026-07-31): the training tier is meant to be the weakest
        // gear in the game, and a stat no other training weapon carried made it the default pick.
        list.Add(new ItemDef(TrainingWand, "Training Wand", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Blunt,
            AtkBonus: 6, MAtkBonus: 7,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: TrainingGearPrice, NoAttributes: true,
            Description: "An apprentice's wand. Poor in the hand, but it carries a spell."));

        // Training ARMOR — light (fighter) and robe (mage). No set bonus: the set line starts at Newbie.
        //
        // ⚠ These were RE-CUT (owner, 2026-07-30): light 53 → 35, robe 27 → 20, +MP unchanged. The old
        // numbers were the sum of an L2 upper + lower body, taken from the TOP of the no-grade range,
        // while this ladder's F Common rung is 45 % of a MID no-grade set — so the starter armor sat
        // ABOVE the first armor you could loot and every early armor drop was a DOWNGRADE. The weapons
        // never had this problem: they were cut from L2's top no-grade weapon, which lines up with the
        // ladder, which is why only the armor moved.
        //
        // The rule this restores: you start in gear that is WORSE than what drops, and gear UP as you
        // play. Fixed on the STARTER side rather than by lifting the F rung, so the ladder keeps its one
        // rule (every quality is a fixed fraction of the authored Mythic piece). Defence is a small share
        // of survival at this level anyway — the owner has levelled a melee fighter wearing none.
        list.Add(new ItemDef(TrainingLeather, "Training Leather Armor", EquipSlot.Armor,
            ItemGrade.F, ItemRarity.Common, Weight: ArmorWeight.Light, ArmorSlot: ArmorSlot.Body,
            DefBonus: 35,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: TrainingGearPrice, NoAttributes: true,
            Description: "Scuffed practice leathers."));
        list.Add(new ItemDef(TrainingRobe, "Training Robe", EquipSlot.Armor,
            ItemGrade.F, ItemRarity.Common, Weight: ArmorWeight.Robe, ArmorSlot: ArmorSlot.Body,
            DefBonus: 20, MpBonus: 29,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: TrainingGearPrice, NoAttributes: true,
            Description: "A rough apprentice's robe."));

        // BROKEN jewels — the level 1-5 drop line. TRADABLE on purpose: these are the first thing a new
        // player owns that is worth anything, and selling them is the first bit of economy they touch.
        // The owner's "40g / 30g / 60g" is the SHOP price, so it goes on BuyPriceOverride; the sell-back
        // value falls out of the normal Value formula, as it does for every other tradable item.
        list.Add(new ItemDef(BrokenEarring, "Broken Earring", EquipSlot.Jewel, ItemGrade.F, ItemRarity.Common,
            MDefBonus: 11, JewelType: JewelType.Earring, Value: 40, BuyPriceOverride: 40, NoAttributes: true,
            Description: "Cracked, but it still turns a little magic."));
        list.Add(new ItemDef(BrokenRing, "Broken Ring", EquipSlot.Jewel, ItemGrade.F, ItemRarity.Common,
            MDefBonus: 7, JewelType: JewelType.Ring, Value: 30, BuyPriceOverride: 30, NoAttributes: true,
            Description: "Bent out of shape, and worth a few coins."));
        list.Add(new ItemDef(BrokenNecklace, "Broken Necklace", EquipSlot.Jewel, ItemGrade.F, ItemRarity.Common,
            MDefBonus: 15, JewelType: JewelType.Necklace, Value: 60, BuyPriceOverride: 60, NoAttributes: true,
            Description: "The chain is mended with wire."));

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
        list.Add(new ItemDef(BoxTrainingWeapons, "Training Weapons Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true,
            Description: "Pick one training weapon."));
        list.Add(new ItemDef(BoxTrainingArmorChoice, "Training Armor Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true,
            Description: "Pick leather or robe."));

        // ===================================================================
        //  NEWBIE ARMOR (two sets) + JEWELS — no attributes, untradeable, sell 0, buy -1.
        //  Light set (fighter): +42 HP, +2% P.Def. Robe set (mage): +15% cast speed.
        //  Full set = body + helm + gloves + boots (set bonus from ArmorSetCatalog).
        // ===================================================================
        // Bodies — each grants its set's bonus (light/robe). They SHARE the accessories below.

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

        // ----- Attribute scrolls (0.45.0). A weapon or jewel drops BARE; a scroll is the only
        //       way it ever gains an attribute. Three scrolls cover the D-C-B stretch, a pair
        //       covers A, and one covers S. Each is locked to its grade band so a cheap scroll
        //       can never touch endgame gear. -----
        list.Add(new ItemDef(AttrScrollCommon, "Attribute Scroll (Common)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Common, AttrScroll: AttrScrollKind.Common,
            Description: "D, C or B grade. Gives the item a random attribute for its type, "
                       + "at a random value. Replaces whatever it had."));
        list.Add(new ItemDef(AttrScrollUncommon, "Attribute Scroll (Uncommon)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Uncommon, AttrScroll: AttrScrollKind.Uncommon,
            Description: "D, C or B grade. Keeps the attribute the item already has and "
                       + "re-rolls its value. The item must already have one."));
        list.Add(new ItemDef(AttrScrollRare, "Attribute Scroll (Rare)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Rare, AttrScroll: AttrScrollKind.Rare,
            Description: "D, C or B grade. Keeps the attribute and re-rolls its value in the "
                       + "TOP HALF of the range. The item must already have one."));
        list.Add(new ItemDef(AttrScrollEpic, "Attribute Scroll (Epic)", EquipSlot.Scroll,
            ItemGrade.A, ItemRarity.Epic, AttrScroll: AttrScrollKind.Epic,
            Description: "A grade only. Gives the item a random attribute for its type, at a "
                       + "random value. Replaces whatever it had."));
        list.Add(new ItemDef(AttrScrollLegendary, "Attribute Scroll (Legendary)", EquipSlot.Scroll,
            ItemGrade.A, ItemRarity.Legendary, AttrScroll: AttrScrollKind.Legendary,
            Description: "A grade only. Keeps the attribute and re-rolls its value in the TOP "
                       + "HALF of the range. The item must already have one."));
        list.Add(new ItemDef(AttrScrollMythic, "Attribute Scroll (Mythic)", EquipSlot.Scroll,
            ItemGrade.S, ItemRarity.Mythic, AttrScroll: AttrScrollKind.Mythic,
            Description: "S grade only. Gives the item a random attribute for its type, "
                       + "always at its MAXIMUM value."));

        // ===================================================================
        //  QUEST ITEMS — non-droppable, non-tradeable proofs for class changes.
        // ===================================================================
        list.Add(new ItemDef(MarkOfFaith, "Mark of Faith", EquipSlot.QuestItem,
            ItemGrade.F, ItemRarity.Rare));
        list.Add(new ItemDef(ClericsProof, "Cleric's Proof", EquipSlot.QuestItem,
            ItemGrade.F, ItemRarity.Epic));

        // ----- GATHERING TOKENS: the trophies the repeatable hunt quests collect. One token per
        //       creature, so the Huntmaster can pay a different QuestItemRewardModifier for each
        //       (see Quests.Repeatable.cs). Common rarity — they are proof of work, not treasure, and
        //       they carry no value: the whole reward is the exp+gold paid at turn-in.
        foreach (var (id, name) in GatherTokens)
            list.Add(new ItemDef(id, name, EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Common,
                Description: "A hunting trophy. Worth nothing to a merchant — bring it to the "
                           + "Huntmaster who asked for it."));

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

        // ----- Level-tier gear (docs/data/gear/gear_sets.csv): weapons + base armor/shield/accessory/
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

    /// <summary>Crafting MATERIALS: 5 types × 5 rarities (docs/design/Crafting.md). Tradable + stackable,
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
    /// <summary>The percentage of a piece's FULL power each quality carries. See <see cref="ItemRarity"/>.
    /// Mythic (100) is the ceiling; the authored numbers in the tiered tables are the EPIC anchor, which
    /// is why the scale factors below divide by 70.</summary>
    public static int RarityPercent(ItemRarity r) => r switch
    {
        ItemRarity.Common    => 45,
        ItemRarity.Uncommon  => 55,
        ItemRarity.Rare      => 70,
        ItemRarity.Epic      => 70,
        ItemRarity.Legendary => 85,
        ItemRarity.Mythic    => 100,
        _ => 70,
    };

    /// <summary>Does this quality carry set bonuses and rolled attributes? The 70% split: Rare and Epic
    /// have the same raw stats, and THIS is the difference between them.</summary>
    public static bool HasIdentity(ItemRarity r) => r >= ItemRarity.Epic && r != ItemRarity.God;

    /// <summary>Stat multiplier relative to the AUTHORED numbers.
    ///
    /// The authored tier tables ARE the Mythic piece (owner, 2026-07-29) — 100% — and every lesser
    /// quality is a fraction of it. This was re-anchored from Epic: anchoring at 70% meant a GENERATED
    /// Mythic sat 43% above anything the game had ever been balanced for, which was a real ceiling
    /// raise nobody had measured. Anchoring at the top instead makes the authored number the ceiling
    /// again, and the ladder above A becomes authored content (the S grade) rather than a multiplier
    /// artefact. The owner's reading of it: our A-grade is L2's LOW S-grade, so A at full power is
    /// already about right for level 85.</summary>
    public static float RarityScale(ItemRarity r) => RarityPercent(r) / 100f;

    /// <summary>How much stronger the S grade is than A. ONE number, because S is DERIVED from the
    /// A row rather than authored (owner: "not so much authoring") — retune the grade by moving this.</summary>
    public const float SGradeOverA = 1.60f;

    /// <summary>The level S gear is built for: 80+, sitting above A's 76-80 window.</summary>
    public const int SGradeLevel = 80;

    /// <summary>S carries only the TOP HALF of the quality ladder — Epic, Legendary, Mythic (owner).
    /// Two reasons. Below Epic is where a piece has no set bonus and no attributes, which is not what
    /// endgame gear is for; and CRAFTING PRODUCES LEGENDARY ONLY, so an S grade without a Legendary
    /// rung could never be crafted at all and the whole blueprint economy would stop at A.</summary>
    public static bool IsTopHalfOnly(int itemLevel) => itemLevel >= SGradeLevel;

    // The DROP copies generated off each authored piece. MYTHIC is the authored item itself, so it is
    // not in this list — it would collide with its own id.
    private static (ItemRarity Rarity, float Scale)[] DropTiers => new[]
    {
        (ItemRarity.Common,    RarityScale(ItemRarity.Common)),
        (ItemRarity.Uncommon,  RarityScale(ItemRarity.Uncommon)),
        (ItemRarity.Rare,      RarityScale(ItemRarity.Rare)),
        (ItemRarity.Epic,      RarityScale(ItemRarity.Epic)),
        (ItemRarity.Legendary, RarityScale(ItemRarity.Legendary)),
    };

    /// <summary>True for a plain base-tier id like "heavy_t52" (the part after the last "_t" is all
    /// digits) — excludes alternate variants like "heavy_t52_dmg".</summary>
    private static bool IsBaseTier(string id)
    {
        int i = id.LastIndexOf("_t", StringComparison.Ordinal);
        if (i < 0) return false;
        string tail = id.Substring(i + 2);
        // The LOW sets of a grade share the grade's equip level with the top set, so their id carries a
        // "lo" suffix after the level (e.g. sword1h_t20lo = Low E, alongside sword1h_t20 = Top E). Strip it
        // so they still count as base tiers and get their own rarity drop copies.
        if (tail.EndsWith("lo", StringComparison.Ordinal)) tail = tail[..^2];
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
            // A- and S-grade SET pieces (the authored tier item, which is the Mythic rung).
            if (d.ItemLevel < 76 || d.Rarity != ItemRarity.Mythic) continue;
            if (d.Slot is not (EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield or EquipSlot.Jewel)) continue;
            string recipeId = $"craft_{d.Id}";
            yield return new ItemDef(RecipeBookId(recipeId), $"Blueprint: {d.Name}",
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
                // S grade is TOP HALF ONLY — Epic / Legendary / Mythic. Below Epic a piece has no set
                // bonus and no attributes, which is not what endgame gear is for, and the low rungs
                // would just be clutter nobody would ever equip at 80+.
                if (IsTopHalfOnly(d.ItemLevel) && !HasIdentity(rarity)) continue;

                int S(int v) => v == 0 ? 0 : Math.Max(1, (int)(v * scale));
                // The quality is NOT in the name (owner). "Common Electrum Longbow" became a different
                // item's name in the player's head; the piece is an Electrum Longbow and its quality is
                // a property — shown by the name's COLOUR and a Rarity: row in the description.
                string name = d.Name;
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
                    // THE 70% SPLIT. Below Epic a piece is numbers only — no set bonus, no rolled
                    // attributes — and from Epic up it keeps its identity. That one rule is what makes
                    // Rare and Epic (identical raw stats) different things worth wanting.
                    // QUALITY-MATCHED set id. A copy joins the set of ITS OWN quality, not the authored
                    // one — otherwise a Mythic body + Epic accessories completed the Mythic set and paid
                    // full price, making a mixed bag strictly better than a matched one (owner).
                    // Mythic IS the authored item, so only Epic/Legendary need the suffix.
                    SetId = HasIdentity(rarity) && !string.IsNullOrEmpty(d.SetId)
                        ? d.SetId + "_" + rarity.ToString().ToLowerInvariant()
                        : "",
                    NoAttributes = !HasIdentity(rarity),
                    Value = 0,             // filled from DefaultValue (rarity-scaled)
                };
            }
        }
    }

    /// <summary>Display letter for a gear LEVEL tier (20/40/52/61/76 → E/D/C/B/A). Cosmetic —
    /// the item's <see cref="ItemDef.ItemLevel"/> drives the mechanics, not the letter.</summary>
    public static string TierLetter(int level) =>
        level >= 76 ? "A" : level >= 61 ? "B" : level >= 52 ? "C" : level >= 40 ? "D"
        : level >= 20 ? "E" : "F";

    /// <summary>The grade's MATERIAL name — the display prefix that signals grade at a glance (owner
    /// 2026-07-25). Each starts with the grade's LETTER as a mnemonic (D→Darksteel, A→Adamantine…). The
    /// item name is "{GradeTheme} {noun}", e.g. "Darksteel Warplate". S/S*/S** are ready for the endgame
    /// CSV. Retune any word here — it changes every item of that grade at once.</summary>
    public static string GradeTheme(int itemLevel) => itemLevel switch
    {
        >= 85 => "Seraphite",       // S**
        >= 83 => "Starstone",       // S*
        >= 80 => "Soulcrystal",     // S
        >= 76 => "Adamantine",      // A
        >= 61 => "Bloodsteel",      // B
        >= 52 => "Cobalt",          // C
        >= 40 => "Darksteel",       // D
        >= 20 => "Electrum",        // E
        _     => "Ferrite",         // F
    };

    // Enum grade for pricing/sorting only (the enum has no C/D). ItemLevel is the real tier.
    // ⚠ The S rung is NOT optional cosmetics: without it the Soulcrystal/Starstone/Seraphite
    // items (levels 80/83/85) came out labelled A grade, and every grade-banded system that
    // reads the enum — pricing, the attribute scrolls — put them in the wrong band.
    private static ItemGrade TierGrade(int level) =>
        level >= 80 ? ItemGrade.S : level >= 61 ? ItemGrade.A : level >= 40 ? ItemGrade.B
        : level >= 20 ? ItemGrade.E : ItemGrade.F;

    /// <summary>The F tier's level. F-grade is now part of the ONE ladder rather than a separate
    /// "(Lesser)" line, and its MYTHIC rung is deliberately the old Newbie gear's power — the newbie
    /// kit IS the top of F grade (owner, 2026-07-30), not a parallel item set beside it.</summary>
    public const int FGradeLevel = 1;

    /// <summary>The level-tier weapons from docs/data/gear/gear_sets.csv — id "<key>_t<level>", base
    /// P.Atk/M.Atk straight from the CSV (the two numbers), bow attack-speed variants, and the
    /// IsMagicWeapon flag on wands/staves (their attributes roll the caster pool). Attribute COUNT
    /// + MAX come from the level (AttributeSystem tiered methods), not grade/rarity.</summary>
    private static IEnumerable<ItemDef> TieredWeapons()
    {
        var weapons = new (string Key, string Noun, WeaponType Type, bool Magic, float Range,
            (int L, int P, int M, int As)[] Rows)[]
        {
            // The FIRST row of each is the F tier (level 1), whose Mythic rung is the old Newbie gear's
            // power — so the newbie kit is the top of F grade instead of a parallel item line.
            ("sword1h", "Blade",      WeaponType.Sword,          false, 0,
                new[] { (1,24,14,0),(20,92,54,0),(40,156,83,0),(52,194,99,0),(61,232,114,0),(76,281,132,0) }),
            ("sword2h", "Greatsword", WeaponType.TwoHandedSword, false, 0,
                new[] { (1,29,14,0),(20,112,54,0),(40,190,83,0),(52,236,99,0),(61,282,114,0),(76,342,132,0) }),
            ("blunt1h", "Mace",       WeaponType.Blunt,          false, 0,
                new[] { (1,24,14,0),(20,92,54,0),(40,156,83,0),(52,194,99,0),(61,232,114,0),(76,281,132,0) }),
            ("blunt2h", "Maul",       WeaponType.TwoHandedBlunt, false, 0,
                new[] { (1,29,14,0),(20,112,54,0),(40,190,83,0),(52,236,99,0),(61,282,114,0),(76,342,132,0) }),
            ("duals",   "Fangs",      WeaponType.Dual,           false, 0,
                new[] { (1,21,14,0),(20,80,54,0),(40,136,83,0),(52,170,99,0),(61,203,114,0),(76,271,132,0) }),
            ("bow",     "Longbow",    WeaponType.Bow,            false, 400,
                new[] { (1,50,14,293),(20,191,55,293),(40,316,84,293),(52,400,99,293),(61,528,114,227),(76,581,132,293) }),
            ("wand",    "Wand",       WeaponType.Blunt,          true,  0,
                new[] { (1,20,19,0),(20,74,72,0),(40,111,101,0),(52,140,122,0),(61,186,152,0),(76,225,175,0) }),
            ("staff",   "Battlestaff",WeaponType.TwoHandedBlunt, true,  0,
                new[] { (1,24,21,0),(20,90,79,0),(40,135,111,0),(52,189,145,0),(61,226,167,0),(76,274,193,0) }),
        };
        // BOTH CSV numbers are authored now (owner, 2026-07-24): P -> AtkBonus, M -> MAtkBonus. Until
        // this, only ONE of the pair survived — a fighter weapon kept P and threw M away, a magic weapon
        // kept M and threw P away — and the discarded channel was reconstructed by multiplying the WHOLE
        // finished channel by OffChannelFactor. That hid the second number from the item card entirely
        // (no weapon in the game set MAtkBonus, so no weapon ever showed an M.Atk line) and made the
        // split an invisible property of code rather than of data.
        //
        // The factors are gone (1.0 both ways). What they were really enforcing — "a caster swinging a
        // mace should not cast like a caster holding a wand" — is not a property of the WEAPON at all;
        // it is a property of the CLASS's training, so it belongs in a passive that says so out loud.
        // Entity's caster check now keys on IsMagicWeapon instead of the weapon TYPE, which is what
        // actually distinguishes a wand from a mace (both are Blunt).
        foreach (var w in weapons)
        {
            // The S row is DERIVED from A × SGradeOverA rather than authored, so the whole grade is one
            // number to retune (owner: "not so much authoring"). Attack speed carries over unchanged —
            // S is stronger, not faster.
            var a = w.Rows[w.Rows.Length - 1];
            var rows = w.Rows.Append(
                (SGradeLevel, Scale(a.P), Scale(a.M), a.As));

            foreach (var (L, P, M, As) in rows)
                yield return new ItemDef($"{w.Key}_t{L}", $"{GradeTheme(L)} {w.Noun}",
                    EquipSlot.Weapon, TierGrade(L), ItemRarity.Mythic,
                    WeaponType: w.Type,
                    AtkBonus: P,
                    MAtkBonus: M,
                    WeaponRange: w.Range,
                    ItemLevel: L, IsMagicWeapon: w.Magic, AttackSpeedBase: As);
        }
    }

    /// <summary>A-grade stat → S-grade stat. Zero stays zero so a weapon with no M.Atk does not
    /// suddenly gain one.</summary>
    private static int Scale(int aValue) =>
        aValue == 0 ? 0 : Math.Max(1, (int)Math.Round(aValue * SGradeOverA));

    /// <summary>The level-tier ARMOR from docs/data/gear/gear_sets.csv — base bodies (Heavy/Light/Robe),
    /// shields, weightless accessories (Gloves/Boots/Helm) and jewels (Necklace/Ring/Earring). Each
    /// carries only its own base stat (P.Def / M.Def / +MP), via the existing equip path — SET BONUSES
    /// and the dmg/support VARIANTS are deferred (they need the StatMods main-stat pass + a playtest).
    /// Armors roll NO attributes for now (owner). Ids: "<key>_t<level>".</summary>
    private static IEnumerable<ItemDef> TieredArmor()
    {
        // …plus the derived S tier. Every table below is authored A-last, so appending SGradeLevel and
        // scaling the final entry gives the whole grade without another column of hand numbers.
        int[] lv = { FGradeLevel, 20, 40, 52, 61, 76, SGradeLevel };

        static int[] WithS(int fVal, int[] a) => a.Prepend(fVal).Append(Scale(a[a.Length - 1])).ToArray();

        // ---- Bodies: (key, noun, weight, pDef[5], mp[5]) — robe carries inherent +MaxMP. ----
        var bodies = new (string Key, string Noun, ArmorWeight W, int[] Def, int[] Mp)[]
        {
            ("heavy", "Bulwark",       ArmorWeight.Heavy, WithS(115, new[]{167,240,270,293,332}), WithS(0, new[]{0,0,0,0,0})),
            ("light", "Leathers",      ArmorWeight.Light, WithS(86, new[]{125,218,202,220,249}), WithS(0, new[]{0,0,0,0,0})),
            ("robe",  "Robe",          ArmorWeight.Robe,  WithS(49, new[]{84,110,135,147,166}),  WithS(109, new[]{274,508,613,718,866})),
        };
        foreach (var b in bodies)
            for (int i = 0; i < lv.Length; i++)
                yield return new ItemDef($"{b.Key}_t{lv[i]}", $"{GradeTheme(lv[i])} {b.Noun}",
                    EquipSlot.Armor, TierGrade(lv[i]), ItemRarity.Mythic,
                    Weight: b.W, ArmorSlot: ArmorSlot.Body, DefBonus: b.Def[i], MpBonus: b.Mp[i],
                    ItemLevel: lv[i], NoAttributes: true,
                    SetId: $"set_{b.Key}_t{lv[i]}");

        // ---- Body VARIANTS: same base P.Def as the tier's body, alternate SET bonus (dmg/support/
        //      nuke lines from the CSV). They share the tier's accessory line. (Bonuses in ArmorSets.) ----
        var variants = new (string Key, ArmorWeight W, string Noun, int L, int Def, int Mp, string Role)[]
        {
            // Noun carries the ROLE (dmg=Warplate/Warhide, tank/def=Guardhide, etc.); "{GradeTheme} {Noun}".
            ("heavy_t52_dmg",  ArmorWeight.Heavy, "Warplate",  52, 270, 0,   "Assault"),
            ("heavy_t61_dmg",  ArmorWeight.Heavy, "Warplate",  61, 293, 0,   "Assault"),
            ("light_t40_pdef", ArmorWeight.Light, "Guardhide", 40, 218, 0,   "Bulwark"),
            ("light_t40_mdef", ArmorWeight.Light, "Wardhide",  40, 218, 0,   "Warded"),
            ("light_t40_str",  ArmorWeight.Light, "Brawlhide", 40, 218, 0,   "Brawler"),
            ("light_t52_sup",  ArmorWeight.Light, "Sagehide",  52, 202, 0,   "Sage"),
            ("light_t61_dmg",  ArmorWeight.Light, "Warhide",   61, 220, 0,   "Assault"),
            ("robe_t40_sup",   ArmorWeight.Robe,  "Raiment",   40, 110, 508, "Warden"),
            ("robe_t40_nuke",  ArmorWeight.Robe,  "Vestments", 40, 110, 508, "Destroyer"),
        };
        foreach (var v in variants)
            yield return new ItemDef(v.Key, $"{GradeTheme(v.L)} {v.Noun}",
                EquipSlot.Armor, TierGrade(v.L), ItemRarity.Mythic,
                Weight: v.W, ArmorSlot: ArmorSlot.Body, DefBonus: v.Def, MpBonus: v.Mp,
                ItemLevel: v.L, NoAttributes: true, SetId: $"set_{v.Key}");

        // ---- Weightless accessories (shared across weights). ----
        var acc = new (string Key, string Noun, ArmorSlot Slot, int[] Def)[]
        {
            ("gloves", "Gauntlets", ArmorSlot.Gloves, WithS(15, new[]{29,39,44,49,55})),
            ("boots",  "Greaves",   ArmorSlot.Boots,  WithS(15, new[]{29,39,44,49,55})),
            ("helm",   "Helm",      ArmorSlot.Head,   WithS(21, new[]{41,58,66,73,83})),
        };
        foreach (var a in acc)
            for (int i = 0; i < lv.Length; i++)
                yield return new ItemDef($"{a.Key}_t{lv[i]}", $"{GradeTheme(lv[i])} {a.Noun}",
                    EquipSlot.Armor, TierGrade(lv[i]), ItemRarity.Mythic,
                    ArmorSlot: a.Slot, DefBonus: a.Def[i], ItemLevel: lv[i], NoAttributes: true,
                    SetId: $"set_acc_t{lv[i]}");   // shared accessory line per tier (all weights)

        // ---- Shields (ShieldDefense from the CSV P.Def; block stats extrapolate Wooden→Iron, tunable). ----
        // F rung 40 -> 90 (owner, 2026-07-31): the Ferrite Aegis at MYTHIC used to match the starter
        // Wooden Shield exactly, so the entire F shield rarity ladder was worthless. 90 puts the Mythic
        // F shield where the ladder expects it, and its Common rung (45%) still lands above the 35 starter.
        int[] shDef = WithS(90, new[]{ 143, 203, 230, 256, 299 });
        float[] shBlock = { 0.15f, 0.22f, 0.24f, 0.26f, 0.28f, 0.30f, 0.32f };
        float[] shReduce = { 0.34f, 0.37f, 0.39f, 0.41f, 0.43f, 0.45f, 0.47f };
        float[] shCrit = { 0.08f, 0.10f, 0.11f, 0.12f, 0.13f, 0.15f, 0.16f };
        int[] shEvaPen = { 5, 7, 7, 8, 8, 9, 9 };
        // The shield belongs to its tier's HEAVY set (the CSV puts shields in the same GroupId).
        // It is NOT required to complete the set — wearing it just adds the set's ShieldBonus.
        for (int i = 0; i < lv.Length; i++)
            yield return new ItemDef($"shield_t{lv[i]}", $"{GradeTheme(lv[i])} Aegis",
                EquipSlot.Shield, TierGrade(lv[i]), ItemRarity.Mythic,
                BlockChance: shBlock[i], BlockReduction: shReduce[i], ShieldDefense: shDef[i],
                ShieldCritDefense: shCrit[i], ShieldEvasionPenalty: shEvaPen[i],
                SetId: $"set_heavy_t{lv[i]}",
                ItemLevel: lv[i], NoAttributes: true);

        // ---- Jewels (M.Def + inherent +MP at 61/76). L2 layout = 1 necklace / 2 rings / 2 earrings. ----
        var jewels = new (string Key, string Noun, JewelType T, int[] MDef, int[] Mp)[]
        {
            ("necklace", "Pendant",  JewelType.Necklace, WithS(18, new[]{45,64,72,85,95}), WithS(0, new[]{0,0,0,33,42})),
            ("ring",     "Band",     JewelType.Ring,     WithS(9, new[]{22,32,36,42,48}), WithS(0, new[]{0,0,0,17,21})),
            ("earring",  "Stud",     JewelType.Earring,  WithS(13, new[]{34,45,54,63,71}), WithS(0, new[]{0,0,0,25,31})),
        };
        foreach (var j in jewels)
            for (int i = 0; i < lv.Length; i++)
                yield return new ItemDef($"{j.Key}_t{lv[i]}", $"{GradeTheme(lv[i])} {j.Noun}",
                    EquipSlot.Jewel, TierGrade(lv[i]), ItemRarity.Mythic,
                    MDefBonus: j.MDef[i], MpBonus: j.Mp[i], JewelType: j.T,
                    ItemLevel: lv[i], NoAttributes: true);

        // ---- Accessory BOX per tier (debug convenience): opens into the 3 accessories, so you
        //      grab a full accessory line at once instead of three items (see BoxCatalog). ----
        foreach (int L in lv)
            yield return new ItemDef($"box_acc_t{L}", $"{GradeTheme(L)} Accessory Box",
                EquipSlot.Box, TierGrade(L), ItemRarity.Rare);
    }


    /// <summary>Formula gold value by slot/grade/rarity, used when an item def does
    /// not set an explicit Value. Quest items and god-tier one-offs return 0 so they
    /// can be neither bought nor sold.</summary>
    /// <summary>Vendor price of TIERED GEAR at all seven grades, or null if this item is not tiered
    /// gear and should fall through to the generic formula.
    ///
    /// The table below is the **MYTHIC** rung, because Mythic is the 100 % base the rarity scale is a
    /// fraction of. But the F/E/D half is NOT authored here — it is written as <c>Shop(x)</c>, where
    /// x is the owner's shop price. **The shop sells RARE only, at F-D**, and those shop numbers are
    /// the fixed points of this whole table (owner, playtest-14: *"the F, E, D prices that are in the
    /// shop are for rare"*). <see cref="Shop"/> divides by the Rare multiplier to find the Mythic rung
    /// above them, so the Rare price always comes back out at exactly the authored number — the source
    /// keeps showing the shop's numbers and the round-trip is exact by construction.
    ///
    ///                    F        E         D          C           B           A            S
    ///   gloves/boots     6 000    175 000     600 000    5 400 000   12 000 000   24 000 000  120 000 000
    ///   helm/shield     10 000    250 000   1 000 000    9 000 000   20 000 000   40 000 000  200 000 000
    ///   body armor      18 000    400 000   1 800 000   16 200 000   36 000 000   72 000 000  360 000 000
    ///   1H weapon       27 000    670 000   2 700 000   24 300 000   54 000 000  108 000 000  540 000 000
    ///   2H weapon       30 000    750 000   3 000 000   27 000 000   60 000 000  120 000 000  600 000 000
    ///   ring             3 000     70 000     250 000    2 250 000    5 000 000   10 000 000   50 000 000
    ///   earring          6 000    140 000     500 000    4 500 000   10 000 000   20 000 000  100 000 000
    ///   necklace        12 000    280 000   1 500 000   13 500 000   30 000 000   60 000 000  300 000 000
    ///           (F/E/D columns are RARE prices; C/B/A/S columns are MYTHIC prices)
    ///
    /// The 2H weapon's C..S column is the owner's own: C 27kk, B 60kk ("30 was to cheap"), A 120kk,
    /// S 600kk. Every other C..S cell is DERIVED by holding that column's slot fractions, which is not
    /// a guess — they are the fractions the authored F/E/D numbers already satisfy:
    ///   * a 2H weapon = 75 % of a full 4-piece armor set (body+helm+gloves+boots);
    ///   * the set splits 45 / 25 / 15 / 15 between those pieces;
    ///   * 1H = 90 % of 2H — it hits softer AND needs a shield bought beside it, about a third of the
    ///     shield's price being the saving;
    ///   * jewels: ring 1/12, earring 1/6, necklace 1/2 of the 2H price (the D column's split).
    /// So retuning a grade is ONE number — the 2H cell — not eight.
    ///
    /// RARITY then scales it at HALF the power ratio (owner): the rarity ladder's power runs
    /// 45/55/70/70/85/100 %, so gold moves 22.5/27.5/35/35/42.5 % — rarity is worth less in gold than
    /// it is in stats, deliberately. Mythic is a 2.35x jump over Legendary, which is intended: Mythic
    /// is craft-only and meant to be traded between players for absurd sums. Epic and above are NOT
    /// vendor stock; their multipliers exist only so selling one pays sensibly.</summary>
    private static int? TieredGearPrice(ItemDef def)
    {
        int tier = def.ItemLevel switch
        {
            >= SGradeLevel => 6,   // S (and S*/S** when they are authored)
            >= 76 => 5,            // A
            >= 61 => 4,            // B
            >= 52 => 3,            // C
            >= 40 => 2,            // D
            >= 20 => 1,            // E
            >= 1  => 0,            // F
            _ => -1,
        };
        if (tier < 0) return null;   // untiered/legacy gear keeps the old formula

        int[]? row = def.Slot switch
        {
            EquipSlot.Shield => new[] { Shop(10_000), Shop(250_000), Shop(1_000_000), 9_000_000, 20_000_000, 40_000_000, 200_000_000 },
            EquipSlot.Armor => def.ArmorSlot switch
            {
                ArmorSlot.Body => new[] { Shop(18_000), Shop(400_000), Shop(1_800_000), 16_200_000, 36_000_000, 72_000_000, 360_000_000 },
                ArmorSlot.Head => new[] { Shop(10_000), Shop(250_000), Shop(1_000_000), 9_000_000, 20_000_000, 40_000_000, 200_000_000 },
                _              => new[] { Shop(6_000), Shop(175_000), Shop(600_000), 5_400_000, 12_000_000, 24_000_000, 120_000_000 },   // gloves / boots
            },
            EquipSlot.Weapon => def.WeaponType.IsTwoHanded()
                ? new[] { Shop(30_000), Shop(750_000), Shop(3_000_000), 27_000_000, 60_000_000, 120_000_000, 600_000_000 }
                : new[] { Shop(27_000), Shop(670_000), Shop(2_700_000), 24_300_000, 54_000_000, 108_000_000, 540_000_000 },
            EquipSlot.Jewel => def.JewelType switch
            {
                JewelType.Necklace => new[] { Shop(12_000), Shop(280_000), Shop(1_500_000), 13_500_000, 30_000_000, 60_000_000, 300_000_000 },
                JewelType.Earring  => new[] { Shop(6_000), Shop(140_000), Shop(500_000), 4_500_000, 10_000_000, 20_000_000, 100_000_000 },
                _                  => new[] { Shop(3_000), Shop(70_000), Shop(250_000), 2_250_000, 5_000_000, 10_000_000, 50_000_000 },   // ring
            },
            _ => null,
        };
        if (row is null) return null;

        return Math.Max(1, (int)Math.Round(row[tier] * (double)RarityPriceMul(def.Rarity)));
    }

    /// <summary>The owner's F/E/D shop price is the price of a **RARE** item. This lifts it to the
    /// MYTHIC rung the price table is expressed in, so that multiplying back down by the Rare
    /// multiplier returns the shop's number exactly.</summary>
    private static int Shop(int rareShopPrice) =>
        (int)Math.Round(rareShopPrice / (double)RarityPriceMul(ItemRarity.Rare));

    /// <summary>Rarity's effect on GOLD — the power ratio halved (see <see cref="TieredGearPrice"/>).
    /// Epic shares Rare's 70 % power, so it shares its price too; Mythic is the 100 % base.</summary>
    public static float RarityPriceMul(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common    => 0.225f,
        ItemRarity.Uncommon  => 0.275f,
        ItemRarity.Rare      => 0.350f,
        ItemRarity.Epic      => 0.350f,
        ItemRarity.Legendary => 0.425f,
        ItemRarity.Mythic    => 1.000f,
        _ => 0.350f,
    };

    public static int DefaultValue(ItemDef def)
    {
        if (def.Slot == EquipSlot.QuestItem || def.Rarity == ItemRarity.God)
            return 0;

        if (TieredGearPrice(def) is int tiered) return tiered;

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
    /// (0 = sells for nothing); otherwise it DERIVES from the buy price.
    ///
    /// Tiered gear divides its buy price by <see cref="GameConstants.GearSellDivisor"/> (25) instead of
    /// taking the generic 30 % — that is the playtest-14 faucet fix, and it is deliberately confined to
    /// gear: mats, potions and scrolls are not what made a level-25 character rich, and cutting them
    /// too would quietly nerf crafting income nobody asked to nerf. Everything else keeps
    /// <see cref="GameConstants.VendorSellFraction"/>.</summary>
    /// USE-CONSUMABLES (buff potions and the cast-on-use scrolls) take the same /25 as gear, added
    /// 2026-07-31 for playtest-15. They are the other half of the same faucet: the Always and Scrolls
    /// drop groups hand one out on essentially every kill, so at the generic 30 % a Lesser buff potion
    /// paid 450 — a third of a tiered F body — for something the player never has to buy. /25 puts it
    /// at 60, which is the owner's own number. HEALING potions are deliberately NOT in this branch:
    /// they carry a PotionCooldownTicks and their oversupply is being fixed at the DROP rate instead,
    /// so their price is left alone rather than nerfed twice.
    public static int SellPrice(ItemDef def) =>
        def.SellPriceOverride is int s ? Math.Max(0, s)
        : TieredGearPrice(def) is int gear ? Math.Max(1, gear / GameConstants.GearSellDivisor)
        : IsUseConsumable(def) && def.Value > 0
            ? Math.Max(1, def.Value / GameConstants.GearSellDivisor)
        : def.Value <= 0 ? 0 : Math.Max(1, (int)(def.Value * GameConstants.VendorSellFraction));

    /// <summary>A consumable whose worth is the EFFECT it casts, not a heal on a timer: buff potions
    /// and the Return/Resurrection scrolls. Keyed off "no heal cooldown + it uses a skill", which is
    /// what separates them from healing potions.</summary>
    private static bool IsUseConsumable(ItemDef def) =>
        def.Slot == EquipSlot.Consumable && def.PotionCooldownTicks == 0
        && !string.IsNullOrEmpty(def.UseSkillId);

    /// <summary>Gold charged when BUYING this item from a vendor (incl. the future
    /// castle surcharge). BuyPriceOverride wins (-1 = unbuyable, 0 = free); otherwise
    /// the Value formula (-1 = not buyable).</summary>
    /// <summary>Vendor equipment can't cost less than this (owner: "equipments must start at 200g at
    /// least" — they were "very very cheap"). Applies to weapons/armor/shields only; JEWELS are exempt
    /// (the broken-jewel line is deliberately 40-60g) and so are consumables/boxes.</summary>
    public const int EquipmentMinBuyPrice = 200;

    public static int BuyPrice(ItemDef def)
    {
        int price = def.BuyPriceOverride is int b ? b
                  : def.Value <= 0 ? -1 : Math.Max(1, (int)(def.Value * (1f + GameConstants.VendorBuyTaxRate)));
        if (price > 0 && def.Slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield)
            price = Math.Max(EquipmentMinBuyPrice, price);
        return price;
    }

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

    /// <summary>How strong a worn jewel is, for deciding which one a new one displaces and which
    /// end of a PAIR it sits in. RARITY is the owner's stated ordering (empty &lt; common &lt;
    /// uncommon &lt; … &lt; mythic); enchant only breaks a rarity tie, so a +3 common outranks a +0
    /// common instead of the choice falling to an arbitrary slot.
    ///
    /// Deliberately NOT persisted: which physical slot a jewel occupies is a pure function of what
    /// you are wearing (strongest first — see JewelSlotOrder), so it survives a relog with no extra
    /// column and can never drift out of sync with the items themselves.</summary>
    public static long JewelStrength(ItemDef def, int enchant) => (long)def.Rarity * 1000 + enchant;

    /// <summary>Sort key placing worn jewels of one sub-type into their designated slots: the
    /// STRONGER of a pair takes slot 1. DefId breaks a full tie so the order is stable across
    /// relogs (a live InstanceId is regenerated on load and would not be).</summary>
    public static (long NegStrength, string DefId) JewelSlotOrder(ItemDef def, int enchant)
        => (-JewelStrength(def, enchant), def.Id);

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
    /// Excludes inert reagents and the cast-on-use scrolls.
    ///
    /// It must actually GRANT A BUFF — "instant + no heal cooldown" alone is not enough. The Ultimate
    /// Scroll of Return is a 0-cast Consumable too, so the moment it became truly instant it started
    /// matching this test, and auto-hunt's keep-your-buff-potions-up loop would have happily drunk it and
    /// teleported the farmer to town on repeat.</summary>
    public static bool IsBuffPotion(ItemDef def) =>
        def.Slot == EquipSlot.Consumable && def.PotionCooldownTicks == 0
        && !string.IsNullOrEmpty(def.UseSkillId)
        && SkillCatalog.Get(def.UseSkillId) is { CastTicks: 0 } s
        && (s.Effect & SkillEffect.AnyBuff) != 0;
    public static bool IsScroll(ItemDef def) => def.Slot == EquipSlot.Scroll;
    public static bool IsEnchantScroll(ItemDef def) => def.Slot == EquipSlot.Scroll && def.ScrollKind != ScrollKind.None;
    public static bool IsAttributeScroll(ItemDef def) => def.AttrScroll != AttrScrollKind.None;
    public static bool IsQuestItem(ItemDef def) => def.Slot == EquipSlot.QuestItem;
    public static bool IsEquippable(ItemDef def) => def.Slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Jewel;

    /// <summary>The level at which an ITEM reaches FULL power (below it you may still equip it, but the
    /// GRADE PENALTY scales your stats down by the grade GAP). Not a hard equip gate. Takes the DEF, not
    /// the grade: the real tier is <see cref="ItemDef.ItemLevel"/> — the ItemGrade enum has no C/D and is
    /// for pricing only. F-level returns 0 so the UI shows no note for starter gear.</summary>
    public static int RequiredLevel(ItemDef def)
    {
        int lvl = GradePenalty.ItemGradeLevel(def);
        return lvl <= 1 ? 0 : lvl;
    }
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
