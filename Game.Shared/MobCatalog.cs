namespace Game.Shared;

/// <summary>One possible drop from a mob: an item, a float chance [0..1], a
/// quantity range, and an OPTIONAL level band. The drop only rolls when the
/// mob's spawned level is within [MinLevel, MaxLevel] (0/0 = any level). This is
/// what lets ONE creature drop different loot at different levels — e.g. a
/// grey_wolf drops common hide at any level but wolf fangs only at level 25+.
/// Chance and amount are scaled by the server RateConfig.</summary>
public record DropEntry(string ItemId, float Chance, int MinQty = 1, int MaxQty = 1,
    int MinLevel = 0, int MaxLevel = 0, int GroupId = 0)
{
    /// <summary>Does this drop apply to a mob spawned at the given level?</summary>
    public bool AppliesAtLevel(int level) =>
        (MinLevel == 0 || level >= MinLevel) && (MaxLevel == 0 || level <= MaxLevel);
}

// GroupId semantics (L2 drop groups): entries with GroupId == 0 roll INDEPENDENTLY (each its own
// chance). Entries sharing a GroupId > 0 form a MUTUALLY-EXCLUSIVE group — the group rolls once at
// the SUM of its members' chances; on a hit, exactly ONE member is picked, weighted by its chance
// (so a group yields at most one item). Use it for "one of these equips" style loot.

/// <summary>
/// A mob TEMPLATE: identity (id + display name), movement speeds, behavior, and
/// its drop table. A mob has NO fixed level — the spawning ZONE assigns the
/// level (and stats derive from it). So the same creature can appear at any
/// level with the same drops; a genuinely different creature (different loot)
/// gets its own id.
/// </summary>
/// <summary>A mob's "passive skills": stat MODIFIERS applied on top of its level-derived
/// stats, so a template can be a glass-cannon, a MAGIC monster (high M.Def / low P.Def →
/// hard for mages, easy for fighters), an armored brute (high P.Def / low M.Def → the
/// reverse), a bruiser, or a boss. Multipliers default to 1 (no change); resists are
/// fractions (0 = none). Use <see cref="MobCatalog.MobTier"/> for L2-style leveled
/// magnitudes (tier 3 = ×1) if you prefer. Null on a template = no modifiers.</summary>
public readonly record struct MobMod(
    float Hp = 1f, float PDef = 1f, float MDef = 1f,
    float PAtk = 1f, float MAtk = 1f,
    float Evasion = 1f, float Accuracy = 1f,
    float BowResist = 0f,    // fraction of BOW damage taken removed (0..1)
    float CritResist = 0f,   // reduces an attacker's physical crit CHANCE vs this mob
    // Weapon-TYPE resistance: a multiplier on this mob's P.Def applied only when the
    // attacker uses that weapon type (1 = neutral, >1 = resistant, <1 = weak, ≤0 = one-shot).
    // e.g. a stone golem resists arrows/daggers (Pierce/Bow >1) but is weak to blunt (<1).
    float PierceResist = 1f, // vs sword / dual
    float BluntResist = 1f,  // vs blunt
    float BowDefResist = 1f, // vs bow (P.Def route; distinct from BowResist damage fraction)
    // Extra leveled-mastery multipliers (see MobMasteries): max MP, attack speed (>1 = faster),
    // HP/MP regen, and a FLAT evasion add (from the Armor Weight mastery). Defaults inert.
    float MaxMp = 1f, float AtkSpeed = 1f, float HpRegen = 1f, float MpRegen = 1f,
    int EvaFlat = 0,
    bool Boss = false,       // raid-boss passive (adds crit/bow resistance on spawn)
    string Name = "")        // display label for the inspect/target window
{
    /// <summary>Human-readable passive lines for the target-inspect window.</summary>
    public IEnumerable<string> Describe()
    {
        if (!string.IsNullOrEmpty(Name)) yield return Name;
        if (Hp != 1f)       yield return $"Max HP {Sign(Hp)}";
        if (PDef != 1f)     yield return $"P.Def {Sign(PDef)}";
        if (MDef != 1f)     yield return $"M.Def {Sign(MDef)}";
        if (PAtk != 1f)     yield return $"P.Atk {Sign(PAtk)}";
        if (MAtk != 1f)     yield return $"M.Atk {Sign(MAtk)}";
        if (Evasion != 1f)  yield return $"Evasion {Sign(Evasion)}";
        if (Accuracy != 1f) yield return $"Accuracy {Sign(Accuracy)}";
        if (PierceResist != 1f) yield return $"Sword/Dual {ResistWord(PierceResist)}";
        if (BluntResist != 1f)  yield return $"Blunt {ResistWord(BluntResist)}";
        if (BowDefResist != 1f) yield return $"Bow {ResistWord(BowDefResist)}";
        if (MaxMp != 1f)   yield return $"Max MP {Sign(MaxMp)}";
        if (AtkSpeed != 1f) yield return $"Atk.Spd {Sign(AtkSpeed)}";
        if (HpRegen != 1f) yield return $"HP Regen {Sign(HpRegen)}";
        if (MpRegen != 1f) yield return $"MP Regen {Sign(MpRegen)}";
        if (EvaFlat != 0)  yield return $"Evasion {(EvaFlat > 0 ? "+" : "")}{EvaFlat}";
        // Bow/Crit resist are rendered from the numeric DTO fields (uniform for mobs
        // and players), so they're not repeated here.
        if (Boss) yield return "Raid Boss";
    }

    private static string Sign(float mult) =>
        mult >= 1f ? $"+{(mult - 1f) * 100:0}%" : $"-{(1f - mult) * 100:0}%";

    // A P.Def coefficient >1 means the mob RESISTS that weapon type (takes less), <1 = WEAK.
    private static string ResistWord(float coef) =>
        coef <= 0f ? "Vulnerable" : coef > 1f ? $"Resist {(coef - 1f) * 100:0}%" : $"Weak {(1f - coef) * 100:0}%";
}

/// <summary>Creature family — flavor today, a hook for faction/damage-type rules later
/// (e.g. holy vs Undead, bane potions vs Insect). Maps the CSV "Type" column.</summary>
public enum MobCategory
{
    Animal, Humanoid, Undead, Insect, Demon, Dragon, Plant, MagicCreature, Angel
}

/// <summary>How a mob FIGHTS. Melee = the default basic-attack chaser. Archer = ranged basic
/// attacks (bow, ~450 range, boosted P.Atk, light armor). Mage = NO basic attack, casts the two
/// mob spells gated on MP (out of MP → helpless). Applied at spawn in GameLoopService.</summary>
public enum MobRole { Melee, Archer, Mage }

public record MobType(
    string Id,
    string Name,
    float WalkSpeed,
    float RunSpeed,
    bool Aggressive = false,
    DropEntry[]? Drops = null,
    MobMod? Mod = null,      // per-template stat modifiers ("passive skills")
    bool Dummy = false,      // training dummy: immortal, immobile, never attacks
    int Level = 0,           // natural level (0 = let the zone assign it)
    MobCategory Category = MobCategory.Humanoid,
    MobRole Role = MobRole.Melee);   // how it fights (melee chaser / ranged archer / caster mage)

/// <summary>
/// THE place to manage mobs. Each entry is a creature template with its own drop
/// table; zones reference these by id and assign the level. Run speeds sit below
/// the player move cap (250) and vary so players can kite.
///
/// To add a mob: add a template here with its drops. To make an existing mob
/// tougher somewhere, just spawn it in a higher-level zone (same id, same drops).
/// To give different loot, make a NEW id.
/// </summary>
public static class MobCatalog
{
    private static readonly Dictionary<string, MobType> All = Build();

    /// <summary>L2-style monster stat TIER → multiplier (tier 3 = normal ×1; lower = weaker,
    /// higher = stronger). e.g. an "HP Lv4" mob = MobTier(4) = ×2 HP. Tunable.</summary>
    public static float MobTier(int tier) => tier switch
    {
        1 => 0.33f, 2 => 0.5f, 3 => 1f, 4 => 2f, 5 => 3f, 6 => 4f, 7 => 5f, _ => 1f
    };

    /// <summary>Compact template factory: walk = 0.55×run, a level-banded drop table, plus
    /// the mob's natural level + family. Base stats come from the level curve (MobBaseStats)
    /// at spawn — the template only carries identity, movement, level, family and passives.</summary>
    private static MobType Mob(string id, string name, int level, MobCategory cat,
        float run, bool aggressive, MobMod? mod = null, MobRole role = MobRole.Melee) =>
        new(id, name, run * 0.55f, run, Aggressive: aggressive,
            Drops: StandardDrops(level, cat), Mod: mod, Level: level, Category: cat, Role: role);

    /// <summary>Nearest gear TIER (1/20/40/52/61/76) a mob's level drops — the level-appropriate set.
    /// The bottom rung is the F tier at level 1: it used to FLOOR at 20, which is why gear drops had to
    /// be gated away from low-level mobs entirely (a level-8 mob dropping E-grade gear).
    ///
    /// This IS the GRADE LOCK (playtest-14 §4, "a level-40 mob drops D — never E or C"): a mob offers
    /// exactly ONE tier, so there is nothing to lock out. S (level 80) is deliberately absent — S carries
    /// only the top half of the quality ladder and stays craft/boss-only.</summary>
    private static int GearTier(int level) =>
        level >= 76 ? 76 : level >= 61 ? 61 : level >= 52 ? 52 : level >= 40 ? 40
        : level >= 20 ? 20 : ItemCatalog.FGradeLevel;

    // ===================================================================
    //  DROP GROUPS (playtest-14 §4)
    // ===================================================================
    // Every gear group is MUTUALLY EXCLUSIVE, so one kill yields at most one armor body, one accessory,
    // one weapon and one jewel — never "20 light armors off one lucky kill" (owner).
    //
    // The engine rolls a group once at the SUM of its members' chances and then picks ONE member weighted
    // by that chance, which means a member's authored chance IS its marginal drop chance. So authoring the
    // §2 rate divided across the slot family reproduces the owner's "trigger % -> rarity roll -> randomise
    // the slot" exactly, with no new mechanism: his Armor row (50% x C 10 / U 4 / R 0.4 / E 0.02) and the
    // §3 target (C 5 / U 2 / R 0.2 / E 0.01) are the same numbers, and §3 is the one written here.
    //
    // A gear group id is `10 + family*10 + (int)rarity` — one group PER RARITY RUNG. That is what lets a
    // BOSS row whose chances sum past 100% (E 70 + L 40 + M 2) drop several pieces while each rung still
    // randomises across the family. For a normal mob the cost is a 0.1% chance of both a Common and an
    // Uncommon armor off one kill, which is not the failure mode the groups exist to prevent.
    public const int GroupMats = 1, GroupScrolls = 2, GroupAlways = 3;
    private const int FamilyArmor = 0, FamilyAccessory = 1, FamilyWeapon = 2, FamilyJewel = 3;

    private static int GearGroupId(int family, ItemRarity rarity) => 10 + family * 10 + (int)rarity;

    /// <summary>Is this drop group one of the four GEAR groups? Elite and boss kills REPLACE the normal
    /// gear table with their own rank row (§3), so the drop roll has to tell gear from mats/consumables.</summary>
    public static bool IsGearGroup(int groupId) => groupId >= 10;

    /// <summary>Groups authored as ABSOLUTE chances rather than as a x1 design: mats 100%, always 100%,
    /// scrolls 70%. The global drop rate does not apply to them (owner: *"at x10 or x200 I still want the
    /// group chances at their current ones"*) — multiplying a 100% group by a server rate cannot make it
    /// more generous, it only pins it at the clamp and discards every weight inside it.</summary>
    public static bool IsGuaranteedGroup(int groupId) =>
        groupId is GroupMats or GroupScrolls or GroupAlways;

    /// <summary>The tuning NAME of a drop group — the key into <see cref="RateConfig.DropGroupRates"/>
    /// and the word the admin types at <c>/droprate</c>. Independent entries (GroupId 0) are "other".</summary>
    public static string GroupName(int groupId) => groupId switch
    {
        GroupMats => "mats",
        GroupScrolls => "scrolls",
        GroupAlways => "always",
        // Parenthesised on purpose: without them `a / 10 switch {…}` parses as `a / (10 switch {…})`.
        _ when IsGearGroup(groupId) => ((groupId - 10) / 10) switch
        {
            FamilyArmor => "armor",
            FamilyAccessory => "accessory",
            FamilyWeapon => "weapon",
            _ => "jewel",
        },
        _ => "other",
    };

    /// <summary>THE rate a drop entry actually rolls at: the global rate (unless the group is guaranteed)
    /// times the group's own multiplier. Everything that shows or rolls a drop chance must go through
    /// here — the kill roll, the target-inspect list and tools/BalanceMatrix — or the number on screen
    /// stops being the number you get, which is the one bug this whole system exists to avoid.</summary>
    public static float EffectiveRate(int groupId) =>
        (IsGuaranteedGroup(groupId) ? 1f : RateConfig.DropChanceRate)
        * RateConfig.DropGroupRate(GroupName(groupId));

    /// <summary>An entry's authored chance with its PER-ITEM multiplier applied, but NOT the group or
    /// global rate. This is the number that acts as a WEIGHT inside an exclusive group, so the weighted
    /// pick and the group's fire chance are built from the same quantity and cannot disagree.</summary>
    public static float ItemWeight(DropEntry e) => e.Chance * RateConfig.DropItemRate(e.ItemId);

    /// <summary>THE chance one drop entry actually rolls at, before the level-gap penalty: the authored
    /// chance times all three knobs (per-item x per-group x global). Everything that rolls or DISPLAYS a
    /// drop chance goes through here — the kill roll, the target-inspect list and tools/BalanceMatrix —
    /// so the number on screen stays the number you get.</summary>
    public static float EffectiveChance(DropEntry e) => ItemWeight(e) * EffectiveRate(e.GroupId);

    // The tables below are PROPERTIES, not static readonly fields, and that is load-bearing: `All =
    // Build()` is declared at the top of this class and C# runs static field initializers in declaration
    // order, so any field declared here would still be null when Build() reaches StandardDrops. Same
    // trick, same reason, as ItemCatalog.DropTiers.

    // The slot FAMILY behind each gear group. A hit randomises across the whole family (owner), so where
    // you farm no longer decides which armor weight or weapon line you are able to loot at all.
    private static (int Family, string[] Keys)[] GearFamilies => new[]
    {
        (FamilyArmor,     new[] { "heavy", "light", "robe" }),
        (FamilyAccessory, new[] { "helm", "gloves", "boots", "shield" }),
        (FamilyWeapon,    new[] { "sword1h", "sword2h", "blunt1h", "blunt2h", "duals", "bow", "wand", "staff" }),
        (FamilyJewel,     new[] { "necklace", "ring", "earring" }),
    };

    /// <summary>NORMAL mobs (playtest-14 §3). Per GROUP, not in total — four groups means ~20% of kills
    /// yield some Common piece, spread over 18 item lines instead of the 3 that used to drop.</summary>
    private static (ItemRarity Rarity, float Chance)[] NormalGearRates => new[]
    {
        (ItemRarity.Common,   0.050f),
        (ItemRarity.Uncommon, 0.020f),
        (ItemRarity.Rare,     0.002f),
        (ItemRarity.Epic,     0.0001f),
    };

    /// <summary>ELITE / dungeon / instance (§3): no Common rung at all, and a full band better.</summary>
    private static (ItemRarity Rarity, float Chance)[] EliteGearRates => new[]
    {
        (ItemRarity.Uncommon, 0.100f),
        (ItemRarity.Rare,     0.020f),
        (ItemRarity.Epic,     0.002f),
    };

    /// <summary>BOSS (§3). Sums past 100% on purpose — a boss is meant to pay out several pieces, which
    /// the per-rung grouping allows (each rung rolls on its own).</summary>
    private static (ItemRarity Rarity, float Chance)[] BossGearRates => new[]
    {
        (ItemRarity.Epic,      0.70f),
        (ItemRarity.Legendary, 0.40f),
        (ItemRarity.Mythic,    0.02f),
    };

    /// <summary>The tiered item id for a slot key at a grade + quality. MYTHIC is the AUTHORED piece, so
    /// it carries no rarity suffix — the scaled copies are what get one.</summary>
    private static string TieredId(string key, int tier, ItemRarity rarity) =>
        rarity == ItemRarity.Mythic ? $"{key}_t{tier}"
        : $"{key}_t{tier}_{rarity.ToString().ToLowerInvariant()}";

    /// <summary>Which qualities a mob of this level may drop. Rarity is introduced BY MOB LEVEL (owner,
    /// §1) so the first hour has somewhere to go, and EPIC and above are held to E grade and up — F is
    /// Common/Uncommon/Rare only, because F gear is worn for under an hour.</summary>
    private static bool RarityDrops(ItemRarity r, int level, int tier) => r switch
    {
        ItemRarity.Common => true,
        ItemRarity.Uncommon => level >= 5,
        ItemRarity.Rare => level >= 10,
        // The MYTHIC rung is the AUTHORED piece, which exists at every tier including F — so a boss below
        // E grade still has something to pay out instead of dropping nothing but a mat pile.
        ItemRarity.Mythic => true,
        _ => tier >= 20,
    };

    /// <summary>The GEAR half of a mob's drop table at one level and rank. Normal-rank entries are baked
    /// into the template (below); Elite and Boss are built at KILL time by the drop roll, because rank is
    /// a property of the SPAWN — the zone assigns it — and not of the template.</summary>
    public static IEnumerable<DropEntry> GearDrops(int level, MobRank rank)
    {
        int tier = GearTier(level);
        var rates = rank switch
        {
            MobRank.Boss => BossGearRates,
            MobRank.Elite => EliteGearRates,
            _ => NormalGearRates,
        };
        foreach (var (family, keys) in GearFamilies)
            foreach (var (rarity, chance) in rates)
            {
                if (!RarityDrops(rarity, level, tier)) continue;
                float each = chance / keys.Length;
                foreach (var key in keys)
                    yield return new DropEntry(TieredId(key, tier, rarity), each,
                        GroupId: GearGroupId(family, rarity));
            }
    }

    /// <summary>MATS-PRIMARY drop table (docs/design/Crafting.md): every mob drops crafting materials
    /// (amount rises with level; rarity gates at 30/60/76 = uncommon/rare/epic), family-flavored mat
    /// types, plus potions/scrolls and a LOW chance at a finished tiered piece (the "usable now" drop).
    /// Bosses layer more via zone rank. Retune via chances or the global RateConfig.</summary>
    private static DropEntry[] StandardDrops(int level, MobCategory cat)
    {
        // Family-flavored primary mat types (+ Gem is universal). The mats keep their category flavor —
        // only the GEAR families were randomised (owner, §4); what a wolf is made of is not a slot roll.
        (MaterialType A, MaterialType B) mats = cat switch
        {
            MobCategory.Animal or MobCategory.Plant => (MaterialType.Leather, MaterialType.Wood),
            MobCategory.Humanoid => (MaterialType.Ingot, MaterialType.Thread),
            MobCategory.Undead => (MaterialType.Thread, MaterialType.Gem),
            MobCategory.Insect => (MaterialType.Thread, MaterialType.Leather),
            MobCategory.Demon or MobCategory.Dragon => (MaterialType.Ingot, MaterialType.Gem),
            _ => (MaterialType.Gem, MaterialType.Wood),   // MagicCreature / Angel
        };
        string Mat(MaterialType type, ItemRarity r) => Crafting.MaterialId(type, r);

        var drops = new List<DropEntry>();

        // ---- MATS (§4): every kill yields exactly one material stack, and THE ROLL IS THE AMOUNT
        //      (owner: "roll the material and let rarity BE the amount"). 50% -> 1, 40% -> 2, 9% -> 4,
        //      1% -> 10, authored as one member per (type, amount) so the existing weighted group picks
        //      both in a single roll. The three types share the weight, so the group totals exactly 1.0.
        var matRungs = new (int Qty, float Weight)[] { (1, 0.50f), (2, 0.40f), (4, 0.09f), (10, 0.01f) };
        var matTypes = new[] { mats.A, mats.B, MaterialType.Gem };
        foreach (var type in matTypes)
            foreach (var (qty, w) in matRungs)
                drops.Add(new(Mat(type, ItemRarity.Common), w / matTypes.Length, qty, qty, GroupId: GroupMats));

        // Higher-rarity mats stay INDEPENDENT low-chance rolls (group "other"), gated by mob level. These
        // are the ORIGINAL x1 numbers — the global rate still applies to them, as it always did.
        if (level >= 30) { drops.Add(new(Mat(mats.A, ItemRarity.Uncommon), 0.08f)); drops.Add(new(Mat(mats.B, ItemRarity.Uncommon), 0.05f)); }
        if (level >= 60) drops.Add(new(Mat(mats.A, ItemRarity.Rare), 0.03f));
        if (level >= 76) drops.Add(new(Mat(mats.A, ItemRarity.Epic), 0.005f));

        // ---- SCROLLS (§4): one per trigger at C 40 / U 20 / R 10 — half an enchant scroll of the grade,
        //      half a BUFF potion (never a healing one; those are the Always group's job). The rungs
        //      unlock on the thresholds the enchant-scroll tier already used (20 / 45): a level-3 mob has
        //      no business handing out a Rare enchant scroll.
        void ScrollRung(float weight, string enchant, string[] buffs)
        {
            drops.Add(new(enchant, weight * 0.5f, GroupId: GroupScrolls));
            foreach (var b in buffs)
                drops.Add(new(b, weight * 0.5f / buffs.Length, GroupId: GroupScrolls));
        }
        // The buff rungs WITHOUT an enchant scroll — used above Rare, where no enchant scroll of
        // that rarity exists. Same halved weight, so a high-level mob's scroll group is not richer
        // than a low one's; it just has more to choose between.
        void BuffRung(float weight, string[] buffs)
        {
            foreach (var b in buffs)
                drops.Add(new(b, weight * 0.5f / buffs.Length, GroupId: GroupScrolls));
        }
        // Each rung carries that rarity's buff potions, its scrolls (the same buff for an hour) and
        // the Dash potion. The group's total weight does NOT grow when a rung gains items — it is
        // split finer. That is the point: more variety at the same faucet.
        // Mythic (rung 6) buff scrolls have no drop source yet, the way Dash Mythic doesn't: they
        // wait for the §3 drop-group rework, which wants to roll the ITEM rather than the rarity.
        ScrollRung(0.40f, ItemCatalog.ScrollCommon,
            new[] { ItemCatalog.SpeedPotionC, ItemCatalog.CastPotionC, ItemCatalog.AtkPotionC,
                    ItemCatalog.EvaPotionC, ItemCatalog.DashPotionC,
                    ItemCatalog.MightPotionC, ItemCatalog.BulwarkPotionC,
                    ItemCatalog.ForcePotionC, ItemCatalog.WardPotionC, ItemCatalog.AimPotionC,
                    ItemCatalog.SpeedScrollC, ItemCatalog.CastScrollC, ItemCatalog.AtkScrollC,
                    ItemCatalog.EvaScrollC,
                    ItemCatalog.MightScrollC, ItemCatalog.BulwarkScrollC,
                    ItemCatalog.ForceScrollC, ItemCatalog.WardScrollC, ItemCatalog.AimScrollC });
        if (level >= 20)
            ScrollRung(0.20f, ItemCatalog.ScrollUncommon,
                new[] { ItemCatalog.SpeedPotionU, ItemCatalog.CastPotionU, ItemCatalog.AtkPotionU,
                        ItemCatalog.EvaPotionU, ItemCatalog.DashPotionU,
                        ItemCatalog.MightPotionU, ItemCatalog.BulwarkPotionU,
                        ItemCatalog.ForcePotionU, ItemCatalog.WardPotionU, ItemCatalog.AimPotionU,
                        ItemCatalog.SpeedScrollU, ItemCatalog.CastScrollU, ItemCatalog.AtkScrollU,
                        ItemCatalog.EvaScrollU,
                        ItemCatalog.MightScrollU, ItemCatalog.BulwarkScrollU,
                        ItemCatalog.ForceScrollU, ItemCatalog.WardScrollU, ItemCatalog.AimScrollU });
        if (level >= 45)
            ScrollRung(0.10f, ItemCatalog.ScrollRare,
                new[] { ItemCatalog.SpeedPotionR, ItemCatalog.CastPotionR, ItemCatalog.AtkPotionR,
                        ItemCatalog.EvaPotionR, ItemCatalog.DashPotionR,
                        ItemCatalog.MightPotionR, ItemCatalog.BulwarkPotionR,
                        ItemCatalog.ForcePotionR, ItemCatalog.WardPotionR, ItemCatalog.AimPotionR,
                        ItemCatalog.SpeedScrollR, ItemCatalog.CastScrollR, ItemCatalog.AtkScrollR,
                        ItemCatalog.EvaScrollR,
                        ItemCatalog.MightScrollR, ItemCatalog.BulwarkScrollR,
                        ItemCatalog.ForceScrollR, ItemCatalog.WardScrollR, ItemCatalog.AimScrollR });
        // The SCROLL-ONLY families enter here: Epic from 60, Legendary from 76. They have no potion
        // at any rarity — a scroll is the only way to get Max HP/MP, regeneration, criticals or
        // Frenzy out of an item — which is why their cheapest rung is Epic in the first place.
        if (level >= 60)
            BuffRung(0.06f,
                new[] { ItemCatalog.BodyScrollE, ItemCatalog.SoulScrollE, ItemCatalog.VigorScrollE,
                        ItemCatalog.SerenityScrollE, ItemCatalog.FocusScrollE, ItemCatalog.FerocityScrollE,
                        ItemCatalog.InsightScrollE, ItemCatalog.FrenzyScrollE, ItemCatalog.DashPotionE });
        if (level >= 76)
            BuffRung(0.04f,
                new[] { ItemCatalog.BodyScrollL, ItemCatalog.SoulScrollL, ItemCatalog.VigorScrollL,
                        ItemCatalog.SerenityScrollL, ItemCatalog.FocusScrollL, ItemCatalog.FerocityScrollL,
                        ItemCatalog.InsightScrollL, ItemCatalog.FrenzyScrollL, ItemCatalog.DashPotionL });

        // ---- ALWAYS (§4): every kill yields one consumable — a healing potion, a return scroll or a
        //      resurrection scroll. C 70 / U 30 below level 75, C 55 / U 40 / R 5 from 75, where the Rare
        //      rung adds the Greater potion and the two Ultimate scrolls. The potion TIER still tracks the
        //      mob's level (Minor / Healing / Greater), so the rarity rung is which of the two you get,
        //      not a level-70 mob handing out Minor potions.
        string potLow = level >= 60 ? ItemCatalog.GreaterPotion
                      : level >= 30 ? ItemCatalog.HealingPotion
                      : ItemCatalog.MinorPotion;
        string potHigh = level >= 30 ? ItemCatalog.GreaterPotion : ItemCatalog.HealingPotion;
        bool topLevel = level >= 75;
        // ⚠ POTIONS ARE A MINORITY OF THIS GROUP (owner, 2026-07-31, playtest-15). The group still fires
        // on EVERY kill — he explicitly liked never having to buy basic potions again (30f passed) — so
        // what changed is the split inside it, not the 100 %.
        //
        // Before: each rung divided its weight EVENLY between a potion and a scroll (35/35 and 15/15),
        // giving a 50 % potion share, and the first rung stacked up to TWO at level 30+. That is ~0.75
        // healing potions per kill, which is why he reported being unable to die while still taking real
        // damage. Now: a 30 % potion share and a stack of ONE — a ~2.5x cut — with the freed weight going
        // to the return/resurrection scrolls that were always the other half of the group.
        //
        // Deliberately NOT fixed with /droprate always: that multiplier scales the whole group, so it
        // would take the scrolls down with the potions. The weights are the right lever. Each branch
        // must SUM TO 1.0 — that sum is what makes the group exactly 100 %.
        void Always(string item, float weight) =>
            drops.Add(new(item, weight, 1, 1, GroupId: GroupAlways));
        if (!topLevel)
        {
            Always(potLow,  0.20f); Always(ItemCatalog.ScrollReturn,    0.50f);
            Always(potHigh, 0.10f); Always(ItemCatalog.ScrollResurrect, 0.20f);
        }
        else
        {
            Always(potLow,  0.15f); Always(ItemCatalog.ScrollReturn,    0.40f);
            Always(potHigh, 0.10f); Always(ItemCatalog.ScrollResurrect, 0.28f);
            Always(ItemCatalog.GreaterPotion,           0.02f);
            Always(ItemCatalog.ScrollReturnUltimate,    0.03f);
            Always(ItemCatalog.ScrollResurrectUltimate, 0.02f);
        }

        // ---- GEAR (§2/§3/§4): the four grade-locked, slot-randomised groups. The BROKEN jewels that
        //      used to be the level 1-5 accessory line are gone from here — §1 makes the F Common jewels
        //      (necklace/ring/earring_t1_common) that line, and the Jewel group drops them from level 1.
        //      The broken pieces stay in the catalog and on the starter vendor's shelf.
        drops.AddRange(GearDrops(level, MobRank.Normal));

        if (level >= 70) drops.Add(new(ItemCatalog.AttrScrollLegendary, 0.01f));
        return drops.ToArray();
    }

    private static Dictionary<string, MobType> Build()
    {
        var list = new[]
        {
            // ===== The level 1-85 roster (docs/data/mobs/mob_base_stats.csv). Base stats are the
            //       shared level curve; a few carry a passive (MobMod) for family/champion
            //       identity. Levels are natural — the mob brings its level, the zone picks
            //       which mobs by band. =====
            Mob("ridgeback_pup", "Ridgeback Pup", 1, MobCategory.Animal, 120f, false),
            Mob("fox", "Fox", 4, MobCategory.Animal, 125f, false),
            Mob("goblin_scout", "Goblin Scout", 8, MobCategory.Humanoid, 132f, false),
            Mob("ashen_wolf", "Ashen Wolf", 10, MobCategory.Animal, 140f, true),
            Mob("werewolf", "Werewolf", 12, MobCategory.Humanoid, 132f, true),
            Mob("hook_spider", "Hook Spider", 14, MobCategory.Insect, 130f, true),
            Mob("orc_archer", "Orc Archer", 16, MobCategory.Humanoid, 132f, true, role: MobRole.Archer),
            Mob("skeleton_grunt", "Skeleton Grunt", 18, MobCategory.Undead, 120f, true),
            Mob("shield_skeleton", "Shield Skeleton", 20, MobCategory.Undead, 115f, true),
            Mob("grizzly_bear", "Grizzly Bear", 22, MobCategory.Animal, 135f, true),
            Mob("cinder_imp", "Cinder Imp", 24, MobCategory.Demon, 142f, true),
            // MAGIC monster: high M.Def / low P.Def — hard for mages, easy for fighters.
            // Also a CASTER (Mage role): no basic attack, nukes from range, sits helpless at 0 MP.
            Mob("watcher_eye", "Watcher Eye", 26, MobCategory.MagicCreature, 130f, true,
                new MobMod(MDef: 2f, PDef: 0.5f, Name: "Magic Monster"), MobRole.Mage),
            Mob("lizardman_warrior", "Lizardman Warrior", 28, MobCategory.Humanoid, 132f, true),
            Mob("marauder_recruit", "Marauder Recruit", 30, MobCategory.Humanoid, 132f, true),
            Mob("mantis_worker", "Mantis Worker", 32, MobCategory.Insect, 140f, true),
            Mob("grave_robber_fighter", "Grave Robber Fighter", 32, MobCategory.Humanoid, 132f, true),
            Mob("medusa", "Medusa", 34, MobCategory.Humanoid, 132f, true),
            Mob("plunder_beetle", "Plunder Beetle", 34, MobCategory.Insect, 140f, true),
            Mob("wyrm", "Wyrm", 35, MobCategory.Dragon, 150f, true),
            Mob("marsh_mantis_soldier", "Marsh Mantis Soldier", 37, MobCategory.Insect, 140f, true),
            Mob("fen_lizardman_archer", "Fen Lizardman Archer", 39, MobCategory.Humanoid, 132f, true, role: MobRole.Archer),
            // CHAMPION outlier: the same L40 curve × a big HP/P.Def passive (≈3.5×/2.2×). Caster.
            Mob("rift_portling", "Rift Portling", 40, MobCategory.MagicCreature, 110f, true,
                new MobMod(Hp: 3.56f, PDef: 2.2f, MDef: 1.27f, Name: "Rift Champion"), MobRole.Mage),
            Mob("dune_orc_archer", "Dune Orc Archer", 40, MobCategory.Humanoid, 132f, true, role: MobRole.Archer),
            Mob("ridge_orc_overlord", "Ridge Orc Overlord", 42, MobCategory.Humanoid, 132f, true),
            Mob("harpy", "Harpy", 42, MobCategory.Humanoid, 138f, true),
            Mob("grave_lich", "Grave Lich", 44, MobCategory.Undead, 120f, true),
            Mob("fomor_brute", "Fomor Brute", 45, MobCategory.Humanoid, 132f, true),
            Mob("marsh_marauder", "Marsh Marauder", 46, MobCategory.Humanoid, 132f, true),
            Mob("warped_drake", "Warped Drake", 47, MobCategory.Dragon, 150f, true),
            Mob("wildhorn_grunt", "Wildhorn Grunt", 48, MobCategory.Humanoid, 132f, true),
            Mob("amber_basilisk", "Amber Basilisk", 48, MobCategory.Animal, 120f, true),
            Mob("ravener", "Ravener", 50, MobCategory.Demon, 145f, true),
            Mob("mantis_follower", "Mantis Follower", 50, MobCategory.Insect, 140f, true),
            Mob("marauder_warrior", "Marauder Warrior", 51, MobCategory.Humanoid, 132f, true),
            Mob("fallen_angel", "Fallen Angel", 52, MobCategory.Demon, 135f, true),
            Mob("thornback", "Thornback", 53, MobCategory.Animal, 135f, true),
            Mob("gaze_hound", "Gaze Hound", 54, MobCategory.Animal, 140f, true),
            Mob("ash_orc_soldier", "Ash Orc Soldier", 55, MobCategory.Humanoid, 132f, true),
            Mob("mirror_wraith", "Hall of Mirrors Wraith", 56, MobCategory.Undead, 125f, true),
            Mob("mirror_ghost", "Mirror Ghost", 56, MobCategory.Undead, 125f, true),
            Mob("dune_orc_porter", "Dune Orc Porter", 57, MobCategory.Humanoid, 132f, false),
            Mob("aether_wisp", "Aether Wisp", 58, MobCategory.MagicCreature, 115f, true, role: MobRole.Mage),
            Mob("hollow_one", "Hollow One", 58, MobCategory.Humanoid, 132f, true),
            Mob("valley_treant", "Valley Treant", 60, MobCategory.Plant, 90f, false),
            Mob("sand_ratman", "Sand Ratman", 60, MobCategory.Humanoid, 132f, true),
            Mob("cursed_blade", "Cursed Blade", 61, MobCategory.Undead, 130f, true),
            Mob("bogwood", "Bogwood", 62, MobCategory.Plant, 90f, false),
            Mob("fen_lizardman", "Fen Lizardman", 62, MobCategory.Humanoid, 132f, true),
            // Golem-type stone/obsidian body, authored via the leveled MASTERY table: Piercing
            // Resistance L10 (×1.43 P.Def vs sword/dual), Bow Resistance L12 (×2), Blunt Resistance
            // L2 (×0.5 = weak). Same effect as a hand MobMod, but "picks a level" like a class.
            Mob("obsidian_knight", "Obsidian Knight", 63, MobCategory.Humanoid, 132f, true,
                MobMasteries.Build(pierce: 10, bow: 12, blunt: 2, name: "Stoneplate")),
            Mob("crimson_drake", "Crimson Drake", 64, MobCategory.Dragon, 150f, true),
            Mob("wildhorn_scout", "Wildhorn Scout", 64, MobCategory.Humanoid, 138f, true),
            Mob("dread_knight", "Dread Knight", 65, MobCategory.Undead, 135f, true),
            Mob("wildhorn_elder", "Wildhorn Elder", 66, MobCategory.Humanoid, 132f, true),
            Mob("spiteful_ghost", "Spiteful Ghost", 66, MobCategory.Undead, 125f, true),
            Mob("highland_kookaburra", "Highland Kookaburra", 67, MobCategory.Animal, 135f, false),
            Mob("highland_buffalo", "Highland Buffalo", 68, MobCategory.Animal, 130f, false),
            Mob("highland_buffalo_tamed", "Highland Buffalo (Tamed)", 68, MobCategory.Animal, 130f, false),
            Mob("dread_archer", "Dread Archer", 69, MobCategory.Undead, 132f, true, role: MobRole.Archer),
            Mob("dire_beast", "Dire Beast", 70, MobCategory.Animal, 140f, true),
            Mob("revenant_minion", "Revenant Minion", 71, MobCategory.Demon, 145f, true),
            Mob("redhorn_footman", "Redhorn Footman", 72, MobCategory.Humanoid, 132f, true),
            Mob("sunland_orc_scout", "Sunland Orc Scout", 73, MobCategory.Humanoid, 138f, true),
            Mob("redhorn_elite", "Redhorn Elite", 73, MobCategory.Humanoid, 132f, true),
            Mob("redhorn_recruit", "Redhorn Recruit", 74, MobCategory.Humanoid, 132f, true),
            Mob("sunland_orc_warrior", "Sunland Orc Warrior", 75, MobCategory.Humanoid, 132f, true),
            Mob("redhorn_soldier", "Redhorn Soldier", 76, MobCategory.Humanoid, 132f, true),
            Mob("sunland_orc_commander", "Sunland Orc Commander", 76, MobCategory.Humanoid, 132f, true),
            Mob("sunland_orc_captain", "Sunland Orc Captain", 77, MobCategory.Humanoid, 132f, true),
            Mob("redhorn_general", "Redhorn General", 78, MobCategory.Humanoid, 132f, true),
            Mob("emberwyrm_drake", "Emberwyrm Drake", 79, MobCategory.Dragon, 155f, true),
            Mob("wrathborn_demon", "Wrathborn Demon", 80, MobCategory.Demon, 145f, true),
            Mob("scarlet_mantis", "Scarlet Mantis", 80, MobCategory.Insect, 142f, true),
            Mob("radiant_scout", "Radiant Scout", 81, MobCategory.Angel, 140f, true),
            Mob("radiant_berserker", "Radiant Berserker", 82, MobCategory.Angel, 135f, true),
            Mob("radiant_mage", "Radiant Mage", 82, MobCategory.Angel, 132f, true, role: MobRole.Mage),
            Mob("splinter_mantis_drone", "Splinter Mantis Drone", 83, MobCategory.Insect, 142f, true),
            Mob("needle_mantis_overseer", "Needle Mantis Overseer", 84, MobCategory.Insect, 140f, true),
            Mob("splinter_mantis_walker", "Splinter Mantis Walker", 84, MobCategory.Insect, 142f, true),
            Mob("drake_leader", "Drake Leader", 85, MobCategory.Dragon, 150f, true),
            Mob("disciple_of_the_dawn", "Disciple of the Dawn", 85, MobCategory.Humanoid, 132f, true),

            // Training dummy: immortal, stationary, deals no damage. The ZONE sets its level
            // (20/40/60/80 training grounds). No drops. For testing damage/skills.
            new MobType("training_dummy", "Training Dummy", 0f, 0f, Dummy: true),
        };
        var dict = new Dictionary<string, MobType>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in list)
            if (!dict.TryAdd(m.Id, m))
                throw new InvalidOperationException($"Duplicate mob id '{m.Id}'.");
        return dict;
    }

    /// <summary>Look up a mob template by id. Falls back to a sane default so a
    /// mistyped zone id never crashes spawning.</summary>
    public static MobType Get(string id) =>
        All.TryGetValue(id, out var m) ? m : new MobType(id, id, 60f, 110f);

    public static bool IsAggressive(string id) => Get(id).Aggressive;

    /// <summary>Every template, ordered by natural level then id. Deterministic order matters: it is
    /// what makes the GENERATED spawner rosters (see <see cref="WorldPlan"/>) reproducible.</summary>
    public static IEnumerable<MobType> Templates =>
        All.Values.OrderBy(m => m.Level).ThenBy(m => m.Id, StringComparer.Ordinal);

    /// <summary>The templates whose NATURAL level falls inside [min,max], ascending.
    ///
    /// This is what lets a spawner's roster be derived from its level band instead of hand-listed, and
    /// it is the fix for the owner's *"how am I supposed to kill a pig next to a werewolf"*: a mob with a
    /// natural level ignores the zone's band (its stat curve is tuned for its own level), so a hand-listed
    /// roster could — and did — put a level-12 Werewolf in the level 1-12 starter camp. Choosing the
    /// roster BY level makes that impossible by construction rather than by vigilance.
    ///
    /// Dummies are excluded: the training dummies are placed by hand at fixed levels.</summary>
    public static MobType[] InBand(int min, int max) =>
        Templates.Where(m => !m.Dummy && m.Level >= min && m.Level <= max).ToArray();
}
