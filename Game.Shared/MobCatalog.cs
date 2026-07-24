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

    /// <summary>Nearest gear TIER (20/40/52/61/76) a mob's level drops — the level-appropriate set.</summary>
    private static int GearTier(int level) =>
        level >= 76 ? 76 : level >= 61 ? 61 : level >= 52 ? 52 : level >= 40 ? 40 : 20;

    /// <summary>MATS-PRIMARY drop table (docs/design/Crafting.md): every mob drops crafting materials
    /// (amount rises with level; rarity gates at 30/60/76 = uncommon/rare/epic), family-flavored mat
    /// types, plus potions/scrolls and a LOW chance at a finished tiered piece (the "usable now" drop).
    /// Bosses layer more via zone rank. Retune via chances or the global RateConfig.</summary>
    private static DropEntry[] StandardDrops(int level, MobCategory cat)
    {
        string potion = level >= 60 ? ItemCatalog.GreaterPotion
                      : level >= 30 ? ItemCatalog.HealingPotion
                      : ItemCatalog.MinorPotion;
        string scroll = level >= 45 ? ItemCatalog.ScrollRare
                      : level >= 20 ? ItemCatalog.ScrollUncommon
                      : ItemCatalog.ScrollCommon;
        // Family-flavored primary mat types (+ Gem is universal).
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
        int matMax = 1 + level / 15;   // amount rises with mob level (L15→2 … L75→6)

        var drops = new List<DropEntry>
        {
            new(potion, 0.30f, 1, level >= 30 ? 2 : 1),
            new(scroll, 0.06f),
            // Common materials — the MAIN loot.
            new(Mat(mats.A, ItemRarity.Common), 0.55f, 1, matMax),
            new(Mat(mats.B, ItemRarity.Common), 0.40f, 1, matMax),
            new(Mat(MaterialType.Gem, ItemRarity.Common), 0.20f, 1, Math.Max(1, matMax / 2)),
        };
        // Higher-rarity mats gated by mob level (low → very low chances).
        if (level >= 30) { drops.Add(new(Mat(mats.A, ItemRarity.Uncommon), 0.08f)); drops.Add(new(Mat(mats.B, ItemRarity.Uncommon), 0.05f)); }
        if (level >= 60) drops.Add(new(Mat(mats.A, ItemRarity.Rare), 0.03f));
        if (level >= 76) drops.Add(new(Mat(mats.A, ItemRarity.Epic), 0.005f));

        // BROKEN jewels — the level 1-5 line (owner, 2026-07-24). A new character no longer starts with
        // any jewels, so these are the first accessory anyone owns: earned off the mobs in the starting
        // zones, or bought cheaply. One mutually-exclusive GROUP, so a kill yields at most one piece.
        if (level <= 5)
        {
            drops.Add(new(ItemCatalog.BrokenEarring, 0.04f, GroupId: 3));
            drops.Add(new(ItemCatalog.BrokenRing, 0.04f, GroupId: 3));
            drops.Add(new(ItemCatalog.BrokenNecklace, 0.02f, GroupId: 3));
        }

        // Usable-now GEAR drops: the SCALED Common/Uncommon/Rare copies of the mob's tier gear
        // (the full Epic set stays craft/boss-only). Family weight picks the body + weapon flavor.
        //
        // ⚠ GATED at level 18 (owner: "a lvl-8 mob drops E-grade gear"). The lowest tiered gear is E at
        // level 20, and GearTier() FLOORS everything below 40 to that 20 tier — so without this gate a
        // level-1..17 mob dropped level-20 (E-grade) gear, badly ahead of a character that band. Below
        // 18, loot is training/broken gear (the level-10 quest kit), mats and the broken-jewel line; the
        // first tiered gear appears as you approach its own level.
        const int GearDropMinLevel = 18;
        if (level >= GearDropMinLevel)
        {
            int tier = GearTier(level);
            (string Body, string Weapon) fam = cat switch
            {
                MobCategory.Undead or MobCategory.Angel or MobCategory.MagicCreature => ("robe", "wand"),
                MobCategory.Animal or MobCategory.Plant or MobCategory.Insect => ("light", "bow"),
                _ => ("heavy", "sword1h"),
            };
            // Body armor + weapon, at each drop rarity. Each is a mutually-exclusive drop GROUP
            // (GroupId 1 = body, 2 = weapon), so a kill yields at most one body and one weapon — the
            // rarer copy is a weighted chance within the group, not a stack of three bodies.
            drops.Add(new($"{fam.Body}_t{tier}_common", 0.040f, GroupId: 1));
            drops.Add(new($"{fam.Body}_t{tier}_uncommon", 0.015f, GroupId: 1));
            drops.Add(new($"{fam.Body}_t{tier}_rare", 0.004f, GroupId: 1));
            drops.Add(new($"{fam.Weapon}_t{tier}_common", 0.025f, GroupId: 2));
            drops.Add(new($"{fam.Weapon}_t{tier}_uncommon", 0.010f, GroupId: 2));
            // A scaled accessory (helm) rounds out the set slots (independent roll).
            drops.Add(new($"helm_t{tier}_common", 0.030f));
        }
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
}
