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

// GroupId semantics (IG drop groups): entries with GroupId == 0 roll INDEPENDENTLY (each its own
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
/// fractions (0 = none). Use <see cref="MobCatalog.MobTier"/> for IG-style leveled
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
    // MAGIC RESISTANCE (BL-11) — the fourth resist, and the only one that is not a P.Def coefficient:
    // it is the mRes DAMAGE-REDUCTION channel players already have (Entity.MagicResist), authored the
    // way the CSVs write it. +0.25 = takes ×0.8 magic damage; NEGATIVE = a magic WEAKNESS, which is
    // how the "anti physical" half of his pair is expressed. 0 = neutral.
    float MagicResist = 0f,
    // Extra leveled-mastery multipliers (see MobMasteries): max MP, attack speed (>1 = faster),
    // HP/MP regen, and a FLAT evasion add (from the Armor Weight mastery). Defaults inert.
    float MaxMp = 1f, float AtkSpeed = 1f, float HpRegen = 1f, float MpRegen = 1f,
    int EvaFlat = 0,
    bool Boss = false,       // raid-boss passive (adds crit/bow resistance on spawn)
    // WEAPON TYPE passive (owner, 2026-08-10). Which weapon the creature "holds" — it drives the
    // basic-attack SPEED through StatCalculator.WeaponAttackBaseSpeed, exactly as a player's does.
    // None = fall through to MobCatalog.DefaultWeaponFor(category). His rule: *"most mobs must have
    // a weapon ... a fast attacking mob with claws needs the mob passive that says mob weapon type,
    // so a fast attacking mob can be knives type, goblins use a club so 1h blunt, knights/skeletons
    // use swords ... so weaponless won't be for many mobs."*
    WeaponType Weapon = WeaponType.None,
    // CC-RESIST OVERRIDES (owner ruling 2026-08-19). 0 = take the ROLE default
    // (StatCalculator.MobCcCon / MobCcSpt). This is how a TANK creature is authored — there is no
    // MobRole.Tank, because Role says how a creature fights and a tank fights melee. His numbers:
    // a tank is Con: 50, Spt: 40 (a same-level stun drops from 47% to 44%).
    //
    // 🔑 They cost nothing else. A mob's CON and SPT feed the contested-debuff roll and NOTHING
    // MORE — its HP, MP and regen all come off MobBaseStats and its own pool. So this pair is safe
    // to author freely per template: it changes how controllable the creature is and no other number
    // in the game.
    int Con = 0, int Spt = 0,
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
        // mRes reads as a percentage, not a coefficient — it is the player-facing "takes X% less
        // magic damage" number, and negative means the opposite, so it must say which.
        if (MagicResist != 0f)
            yield return MagicResist > 0f
                ? $"Magic resistant (−{MagicResist * 100:0}% magic damage taken)"
                : $"Magic WEAK (+{-MagicResist * 100:0}% magic damage taken)";
        if (MaxMp != 1f)   yield return $"Max MP {Sign(MaxMp)}";
        if (AtkSpeed != 1f) yield return $"Atk.Spd {Sign(AtkSpeed)}";
        if (HpRegen != 1f) yield return $"HP Regen {Sign(HpRegen)}";
        if (MpRegen != 1f) yield return $"MP Regen {Sign(MpRegen)}";
        if (EvaFlat != 0)  yield return $"Evasion {(EvaFlat > 0 ? "+" : "")}{EvaFlat}";
        if (Weapon != WeaponType.None) yield return $"Wields: {MobCatalog.WeaponWord(Weapon)}";
        // The two CC-resist stats read as what they DO, not as raw numbers — "CON 50" tells a player
        // nothing, "hard to stun" tells them which debuff to bring. Only shown when the template
        // overrides its role default, so an ordinary creature's plate is unchanged.
        if (Con != 0) yield return $"Stun/bleed resistance {(Con >= 48 ? "high" : Con >= 42 ? "average" : "low")} (CON {Con})";
        if (Spt != 0) yield return $"Hold/fear resistance {(Spt >= 48 ? "high" : Spt >= 38 ? "average" : "low")} (SPT {Spt})";
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

/// <summary>BL-47 step 2 — a creature whose stats come from the PLAYER pipeline instead of the
/// authored mob curve: real base stats, a real class HP/MP curve, real gear it actually wears.
///
/// <para>His ruling (playtest 24, `86b`): *"try to recreate mobs with different races (main stats)
/// with player formulas ... so same weapon type and just enchanted or a mob passives that boost PAtk
/// and or other stats."* The shape below is the one `BalanceMatrix` `G3.7` measured and he named
/// first — the WEAPON and the ARMOUR are dressed **independently**, because the one loadout that
/// reconciles a player-shaped entity with the mob curve is an over-enchanted weapon over under-grade
/// armour (*"S grade Mace enchanted to +60 ... and B grade leather"*). Moving every slot together —
/// what the earlier `G3.2` sweep did — cannot express that, and wrongly concluded no gear closes it.</para>
///
/// <para>RACE is a flat ±5 lean on the stat block and nothing else (his `B1`: *"ork have higher
/// con/atk less agi ..while elf have higher agi less atk/con ... No lvl curve. Can go +-5 same as the
/// swap passives"*). ±5 on a ~40-point stat is ±12.5%, so race is FLAVOUR — what actually separates
/// a lich from a goblin is its kit, its gear and its <see cref="MobMod"/> passives, which still ride
/// on top of everything here exactly as they do for an ordinary creature.</para>
///
/// <para>A held item (his `B3`: *"if mob is a player it can have inventory (not a dropped one..but
/// just to hold stuff)"*) is carried and NEVER looted — a mob's loot is its drop table and nothing
/// reads its bag. That is what admits the War Rune, which is worth ×2.00 P.Atk against the ×1.55-1.60
/// an authored attack passive would otherwise have to supply.</para></summary>
public readonly record struct MobBuild(
    BaseClass Class,
    int SecondClass,          // a real ClassCatalog id → an Archetype → the HP/MP class-level curve
    string Body,              // "heavy" / "light" / "robe"
    string Weapon,            // "sword1h" / "sword2h" / "staff" / …
    int ArmorTier, ItemRarity ArmorQuality, int ArmorEnchant,
    int WeaponTier, ItemRarity WeaponQuality, int WeaponEnchant,
    // The race lean, ±5 (his B1). Added to the StatBase block below; 0 = no lean on that stat.
    int Con = 0, int Atk = 0, int Wit = 0, int Agi = 0, int Spt = 0,
    // Which player stat block the lean is applied TO. Human is the neutral one, and keeping every
    // demo creature on it is what makes the ±5 the ONLY difference between them.
    Race StatBase = Race.Human,
    string Held = "",         // a held, never-looted item (ItemCatalog.WarRune) — "" = none
    bool Jewels = true,       // a creature need not wear jewels; dropping them is the M.Def lever
    // Wears its tier's shield (BL-79's tank guard). ONE-HANDED BUILDS ONLY — a shield beside a 2H
    // weapon is not a loadout a player could reproduce, and reproducibility is the whole point of a
    // player-built creature. There is one shield per tier and it is Mythic, so this takes no rarity.
    bool Shield = false,
    // Learn the PASSIVE half of the class kit — weapon/armour masteries and discipline passives — so
    // the creature has a real player's STATS rather than only a real player's gear (owner: "If we
    // treat them like a player give them classes so they atleast have the player stats").
    // ⚠ Default FALSE so the five BL-47 demo creatures are untouched: they exist to measure what gear
    // ALONE does, and teaching them a kit would silently rewrite every G3 reading in the docs.
    bool LearnsKit = false)
{
    /// <summary>Item-id quality suffix. The AUTHORED piece is the Mythic one (bare id); every lesser
    /// quality is a generated copy suffixed with its rarity name — see ItemCatalog's DropTiers. This
    /// is why an admin gear picker that drilled down by name could only ever hand out Mythic.</summary>
    public static string QualitySuffix(ItemRarity q) =>
        q == ItemRarity.Mythic ? "" : "_" + q.ToString().ToLowerInvariant();

    /// <summary>Everything this creature carries, as (item id, enchant) pairs. Accessories and jewels
    /// follow the ARMOUR — only the weapon is dressed on its own axis.</summary>
    public IEnumerable<(string DefId, int Enchant)> Pieces()
    {
        string aq = QualitySuffix(ArmorQuality), wq = QualitySuffix(WeaponQuality);
        yield return ($"{Weapon}_t{WeaponTier}{wq}", WeaponEnchant);
        yield return ($"{Body}_t{ArmorTier}{aq}", ArmorEnchant);
        yield return ($"helm_t{ArmorTier}{aq}", ArmorEnchant);
        yield return ($"gloves_t{ArmorTier}{aq}", ArmorEnchant);
        yield return ($"boots_t{ArmorTier}{aq}", ArmorEnchant);
        if (Jewels)
        {
            yield return ($"necklace_t{ArmorTier}{aq}", ArmorEnchant);
            yield return ($"ring_t{ArmorTier}{aq}", ArmorEnchant);
            yield return ($"ring_t{ArmorTier}{aq}", ArmorEnchant);
            yield return ($"earring_t{ArmorTier}{aq}", ArmorEnchant);
            yield return ($"earring_t{ArmorTier}{aq}", ArmorEnchant);
        }
        // A SHIELD, for the builds that carry one (BL-79's tank guard). The tier ladder authors exactly
        // one shield per tier and it is Mythic — there is no rarity suffix to apply, which is why this
        // does not take `aq` the way every line above does. Never pair it with a two-handed weapon.
        if (Shield) yield return ($"shield_t{ArmorTier}", ArmorEnchant);
        if (Held.Length > 0) yield return (Held, 0);
    }

    /// <summary>One line for the target-inspect window, so the thing you are fighting says what it is.
    /// It rides <see cref="MobMod.Describe"/>'s existing passive list — no protocol change, no client
    /// build: the creature simply has one more sentence about itself.</summary>
    public IEnumerable<string> Describe()
    {
        // Three short lines rather than one long one: this list is drawn in a phone-width panel, and a
        // sentence that wraps mid-number is harder to read than three that do not wrap at all.
        static string Ench(int e) => e > 0 ? $" +{e}" : "";
        yield return "Built like a player";
        yield return $"  Weapon: t{WeaponTier} {WeaponQuality} {Weapon}{Ench(WeaponEnchant)}";
        yield return $"  Armour: t{ArmorTier} {ArmorQuality} {Body}{Ench(ArmorEnchant)}"
                   + (Jewels ? "" : ", no jewels");
        if (Held.Length > 0) yield return $"  Holds: {ItemCatalog.Get(Held)?.Name ?? Held} (never dropped)";
    }
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

/// <summary>What a TRAINING DUMMY hits back with. The owner asked for a dummy that attacks YOU
/// (playtest-20 `56c`): *"a magic training dummy (lvl 80, 50 range, 1 magic dmg every 0.1s) so 10s =
/// 100 hits and mob magic crit can actually be observed. Same idea for a physical-skill dummy."*
///
/// <para>A plain dummy is a target you hit; these are the mirror — something that hits you, at a
/// known rate, for a known amount, so an OUTCOME (crit / fail / miss / block) can be counted over a
/// hundred samples instead of guessed at over five. The damage is deliberately 1: the point is the
/// resolution, not the number, and 1 damage never kills anyone standing there counting.</para></summary>
public enum DummyAttack { None, Magic, Physical }

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
    MobRole Role = MobRole.Melee,    // how it fights (melee chaser / ranged archer / caster mage)
    DummyAttack Strikes = DummyAttack.None,    // a DUMMY that hits back, for counting outcomes
    string Title = "",       // the line drawn ABOVE the name, like an NPC's role ("Elder" over "Marius")
    // SOCIAL CLAN (BL-70). A named group — "orc", "mantis", "redhorn". Damage one member and every
    // clanmate within MobClanCallRadius joins the fight. "" = a loner, which is most animals and
    // every solitary creature: a bear does not have a warband.
    //
    // 🔑 The trigger is DAMAGE and NOTHING else (owner): not a taunt, not a debuff, not walking into
    // aggro range. That single rule is what makes a LURE the intended way to pull one member out of
    // a camp — his picture is a rogue crossing an elite field, taunting the one mob the party wants
    // and walking it back, with the rest of the settlement never learning it happened.
    string Clan = "",
    // BL-47 step 2. Set this and the creature's stats come from the PLAYER pipeline — base stats, a
    // class HP/MP curve and gear it actually wears — instead of MobBaseStats' authored curve. Null
    // (every template but the demo five) = today's mob, unchanged. See MobBuild.
    MobBuild? Build = null,
    // HAND-PLACED: this template is placed by an authored spawner and must never be rostered into a
    // generated camp by its level band. See MobCatalog.InBand — without it a level-40 demo creature
    // would immediately appear in every generated 40-44 camp in the game, which is the one thing the
    // BL-47 experiment promises not to do.
    bool HandPlaced = false,
    // ===================================================================================
    //  GUARD (BL-79) — a creature that polices outlaws instead of hunting players.
    //
    //  Owner, 2026-08-27: "try make town guards and one archer(overenchanded) in several zones (like
    //  piesfull farming zones) killing a guard dont give karma nor flags .. but they should be strong
    //  enough so a player attacking them (pvp-on must be on) and when the guard retaliate a player has
    //  its hands full .. to match a 80 lvl player S grade equip (no nenchanted)". Playtest 25, the
    //  original design: "only aggressive thowards PK (ignores mobs/pvpOrNormal-players)", "ofc if u
    //  hit them (pvp-on) they act as passive mobs".
    //
    //  Setting this changes FOUR things, all of them in GameLoopService:
    //   1. AGGRO IS KARMA-KEYED. It acquires PK players only — never an innocent, never a merely
    //      flagged one, never another creature. Everyone else walks past it.
    //   2. ATTACKING ONE NEEDS PvP ON. The toggle is the GATE (the 0.93.2 rule) — you cannot swing at
    //      the town watch by accident, and choosing to is a deliberate act.
    //   3. KILLING ONE PAYS NOTHING AND COSTS NOTHING — no exp, no drop, and critically NO KARMA
    //      SHED. Without that last one a guard post is a karma laundry: a PK grinding the watch to
    //      clean his record is the exact opposite of what a guard is for.
    //   4. IT NEVER FLAGS ITS ATTACKER. Already true (a mob target cannot set a PvP flag), and his
    //      "dont give karma nor flags" makes it a promise rather than an accident.
    //
    //  ⚠ A guard is HandPlaced too — it must never be rostered into a generated camp.
    bool Guard = false,
    // Per-template aggro radius, overriding the global GameConstants.MobAggroRange. 0 = the global.
    // His guard numbers are 400 melee / 600 archer — the archer notices you from where it can shoot.
    float AggroRange = 0f);

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

    /// <summary>IG-style monster stat TIER → multiplier (tier 3 = normal ×1; lower = weaker,
    /// higher = stronger). e.g. an "HP Lv4" mob = MobTier(4) = ×2 HP. Tunable.</summary>
    public static float MobTier(int tier) => tier switch
    {
        1 => 0.33f, 2 => 0.5f, 3 => 1f, 4 => 2f, 5 => 3f, 6 => 4f, 7 => 5f, _ => 1f
    };

    /// <summary>Compact template factory: walk = 0.55×run, a level-banded drop table, plus
    /// the mob's natural level + family. Base stats come from the level curve (MobBaseStats)
    /// at spawn — the template only carries identity, movement, level, family and passives.</summary>
    private static MobType Mob(string id, string name, int level, MobCategory cat,
        float run, bool aggressive, MobMod? mod = null, MobRole role = MobRole.Melee,
        string clan = "") =>
        new(id, name, run * 0.55f, run, Aggressive: aggressive,
            Drops: StandardDrops(level, cat), Mod: mod, Level: level, Category: cat, Role: role,
            Clan: clan);

    // ----- BL-47 step 2 authoring helpers. See the demo block at the bottom of Build(). -----

    /// <summary>A player-built demo creature. Never aggressive (you pick the fight, one at a time, the
    /// way you would with a dummy) and no drops (an experiment changes no economy). Its Title says which
    /// half of the experiment it is, in the same place every plate already draws one.</summary>
    private static MobType Demo(string id, string name, int level, MobCategory cat, float run,
        MobBuild build, MobMod mod, MobRole role = MobRole.Melee) =>
        new(id, name, run * 0.55f, run, Aggressive: false, Drops: null, Mod: mod, Level: level,
            Category: cat, Role: role, Title: "Player-built", Build: build, HandPlaced: true);

    /// <summary>The CURVE TWIN of a demo creature: an ordinary MobBaseStats mob at the same level with
    /// no passive, so a column of the Proving Grounds is one level built the two ways. Also never
    /// aggressive and dropless, for the same reasons.</summary>
    private static MobType Curve(string id, string name, int level, WeaponType weapon,
        MobRole role = MobRole.Melee) =>
        new(id, $"{name} (Lv {level})", 132f * 0.55f, 132f, Aggressive: false, Drops: null,
            Mod: new MobMod(Weapon: weapon, Name: "Today's mob curve, no passives"),
            Level: level, Category: MobCategory.Humanoid, Role: role, Title: "Curve", HandPlaced: true);

    /// <summary>A player-built WARRIOR (Champion, id 14): heavy body, two-handed sword — the archetype
    /// every `G3` table is measured on. The armour and the weapon take SEPARATE tiers on purpose; that
    /// split is the whole finding of G3.7 and collapsing them back into one is what G3.2 got wrong.</summary>
    private static MobBuild Warrior(int armorTier, ItemRarity armorQ, int armorEnch,
        int weaponTier, ItemRarity weaponQ, int weaponEnch,
        int con = 0, int atk = 0, int wit = 0, int agi = 0, int spt = 0, string held = "") =>
        new(BaseClass.Fighter, 14, "heavy", "sword2h",
            armorTier, armorQ, armorEnch, weaponTier, weaponQ, weaponEnch,
            Con: con, Atk: atk, Wit: wit, Agi: agi, Spt: spt, Held: held);

    /// <summary>A player-built NUKER (Sorcerer, id 18): robe and staff. Its M.Atk, cast speed and magic
    /// crit all come from the same places a player's do — which is why the Mage ROLE's stat lean is
    /// skipped for a player-built creature (see Entity.ApplyMobScale); it would pay for the caster
    /// shape twice.</summary>
    private static MobBuild Nuker(int armorTier, ItemRarity armorQ, int armorEnch,
        int weaponTier, ItemRarity weaponQ, int weaponEnch,
        int con = 0, int atk = 0, int wit = 0, int agi = 0, int spt = 0, string held = "") =>
        new(BaseClass.Mage, 18, "robe", "staff",
            armorTier, armorQ, armorEnch, weaponTier, weaponQ, weaponEnch,
            Con: con, Atk: atk, Wit: wit, Agi: agi, Spt: spt, Held: held);

    // ===================================================================================
    //  BL-79 — THE GUARDS. His two tiers, and the one number they are calibrated against.
    //
    //  Owner, 2026-08-27: "pieasfull zone guards have everithing s grade +16 and are 90lvl -> town
    //  80lvl S grade +0", and the target: "to match a 80 lvl player S grade equip (no nenchanted)".
    //
    //  So the TOWN guard is a mirror of that reference player — same level, same grade, same enchant —
    //  and is meant to be a real fight he can win. The FIELD guard is the same creature four levels of
    //  gear further up and is meant to be a wall: "overenchanded", his word.
    //
    //  ⚠ S GRADE IS ItemLevel 80 AND IT IS TOP-HALF ONLY (Items.cs: Epic / Legendary / Mythic — the
    //  cliff his 0.93.1 goldflow measurement found). He named the grade and the enchant but not the
    //  rarity, so both tiers wear the BASE S rung, Epic, and the only two things that differ between
    //  a town guard and a field guard are the level and the enchant. Change the rarity and you have
    //  quietly changed the calibration.
    //
    //  ⚠ THEY ARE PLAYER-BUILT, so they run the PLAYER HP/attack/defence curves — which is what makes
    //  "match a level-80 player" literally true rather than roughly true, and it is also why the mob
    //  ROLE's stat lean is skipped for them (Entity.ApplyMobScale). Their power is what they WEAR.
    //
    //  "they dont use skills (only normal attack) but can have rune_war (unlimited)" — so no kit, and
    //  the War Rune is authored on the field pair only, where the wall is wanted.
    // ===================================================================================

    /// <summary>A guard TANK: Knight (id 13), HEAVY armour, one-handed SWORD and the tier's shield —
    /// his loadout, named exactly ("tank guard; heavy + sword"). It learns the class kit, so its
    /// heavy-armour and sword masteries are real learned passives, not a multiplier.</summary>
    private static MobBuild GuardTank(int tier, int ench, string held = "") =>
        new(BaseClass.Fighter, 13, "heavy", "sword1h",
            tier, ItemRarity.Epic, ench, tier, ItemRarity.Epic, ench,
            Held: held, Shield: true, LearnsKit: true);

    /// <summary>A guard ARCHER: Assassin (id 15, the merged bow/dagger rogue), LIGHT armour, BOW —
    /// again his own words ("archer: bow +light"). No shield; a bow is two-handed in every sense that
    /// matters here.</summary>
    private static MobBuild GuardArcher(int tier, int ench, string held = "") =>
        new(BaseClass.Fighter, 15, "light", "bow",
            tier, ItemRarity.Epic, ench, tier, ItemRarity.Epic, ench,
            Held: held, LearnsKit: true);

    /// <summary>A guard template. Never aggressive in the ordinary sense — <see cref="MobType.Guard"/>
    /// makes its aggro karma-keyed, so `Aggressive: true` here means "acquires targets at all", and the
    /// only targets it will ever acquire are PKs. Dropless and hand-placed, like every authored
    /// creature that is content rather than roster.</summary>
    private static MobType GuardMob(string id, string name, int level, float run, MobBuild build,
        MobRole role, float aggroRange, MobMod? mod = null) =>
        new(id, name, run * 0.55f, run, Aggressive: true, Drops: null, Mod: mod, Level: level,
            Category: MobCategory.Humanoid, Role: role, Title: "Town Watch", Build: build,
            HandPlaced: true, Guard: true, AggroRange: aggroRange);

    /// <summary>THE GUARD TOWER — what makes a FIELD guard something other than a tougher watchman.
    ///
    /// Owner, 2026-08-27: *"Field guard they are like a guard tower ... Faster stronger almost 1 shot
    /// a pk"*. A town guard is a fair fight and gets NO passive at all — its power is its class kit
    /// and its gear, exactly like the player it is calibrated against. A field guard is not a fight;
    /// it is a closed road, and this is the difference.
    ///
    /// 🔑 THESE ARE THE ELITE RANK'S OWN RUNGS, NOT NEW NUMBERS — his "elite oassives". MobRankScale
    /// prices an elite at HP x4 / attack x1.5 / defence x1.33; a tower leans harder on ATTACK than an
    /// elite does, because "almost 1 shot a pk" is a damage statement, not a health one.
    /// </summary>
    private static MobMod GuardTower(string name) =>
        new(PAtk: 3.0f, MAtk: 3.0f, PDef: 1.33f, MDef: 1.33f, Hp: 2.0f, Name: name);

    // ===================================================================================
    //  SOCIAL CLANS (BL-70). Which creatures answer each other's cry for help.
    //
    //  Authored on the families that already READ as a warband or a nest — the ones sharing a name
    //  root, which is the same grouping his "demon settlement" picture describes (BL-21). Everything
    //  else is deliberately clanless: a bear, a treant, a wisp and a lone medusa have nobody to call.
    //
    //  A clan name may span levels because a clan is only ever LOCAL — the cry reaches 450 units and
    //  a level-16 Orc Archer and a level-76 Sunland Orc Commander never stand in the same field. The
    //  name is the family, the radius is the fight.
    // ===================================================================================
    private const string ClanOrc      = "orc";
    private const string ClanLizard   = "lizardman";
    private const string ClanMarauder = "marauder";
    private const string ClanMantis   = "mantis";
    private const string ClanSkeleton = "skeleton";
    private const string ClanDread    = "dread";
    private const string ClanMirror   = "mirror";
    private const string ClanWildhorn = "wildhorn";
    private const string ClanRedhorn  = "redhorn";
    private const string ClanRadiant  = "radiant";
    private const string ClanDrake    = "drake";
    private const string ClanWolf     = "wolf";

    // ===================================================================================
    //  MOB WEAPON TYPE (owner, 2026-08-10). A mob's basic-attack SPEED comes from the weapon it
    //  holds, through the same StatCalculator.WeaponAttackBaseSpeed a player uses. Until now every
    //  mob was WeaponType.None — "weaponless" — which he ruled wrong: *"most mobs must have a
    //  weapon ... so weaponless won't be for many mobs."*
    //
    //  Resolution order at spawn (GameLoopService.BuildMob):
    //    1. MobRole.Archer            -> Bow (a bow IS the role)
    //    2. the template's MobMod.Weapon passive, if it set one
    //    3. DefaultWeaponFor(category) below
    //  So a whole family gets a sensible weapon for free and any single template can override it
    //  with one passive, which is what keeps this from being 80 hand-edits.
    // ===================================================================================

    /// <summary>The weapon a creature FAMILY fights with when its template says nothing.
    ///
    /// Claws and fangs are <see cref="WeaponType.Dual"/> — that is already how the game models
    /// "fast, low per-hit" (daggers are Duals), so a wolf and a rogue share one speed rule instead
    /// of inventing a claw type. Humanoids club (1H Blunt, his goblin example); the armed dead and
    /// demons carry blades (1H Sword, his knight/skeleton example). Plants and magic creatures stay
    /// genuinely weaponless — a treant has no weapon to hold, and that is what None is FOR.</summary>
    public static WeaponType DefaultWeaponFor(MobCategory cat) => cat switch
    {
        MobCategory.Animal   => WeaponType.Dual,   // claws/fangs: fast, low per-hit
        MobCategory.Insect   => WeaponType.Dual,   // mandibles/pincers: same shape
        MobCategory.Dragon   => WeaponType.Dual,   // claws — the size is in its stats, not its speed
        MobCategory.Humanoid => WeaponType.Blunt,  // clubs (goblins); override to Sword for soldiery
        MobCategory.Undead   => WeaponType.Sword,  // skeletons carry blades
        MobCategory.Demon    => WeaponType.Sword,
        MobCategory.Angel    => WeaponType.Blunt,  // maces/staves
        _ => WeaponType.None,                      // Plant, MagicCreature: nothing to hold
    };

    /// <summary>Player-facing noun for a mob's weapon, for the target-inspect passive list.</summary>
    public static string WeaponWord(WeaponType w) => w switch
    {
        WeaponType.Dual => "claws (very fast)",
        WeaponType.Sword => "a blade (fast)",
        WeaponType.Blunt => "a club (fast)",
        WeaponType.TwoHandedSword => "a greatblade (normal)",
        WeaponType.TwoHandedBlunt => "a maul (normal)",
        WeaponType.Bow => "a bow (slow)",
        _ => "nothing",
    };

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
    /// scrolls 70%.
    ///
    /// <para>⚠ These used to be EXEMPT from <see cref="RateConfig.DropChanceRate"/> (owner: *"at x10 or
    /// x200 I still want the group chances at their current ones"*), and the reason was the 100% CLAMP:
    /// multiplying a 100% group by a server rate could not make it more generous, it only pinned it at
    /// the clamp and discarded every weight inside it. <see cref="DropCopies"/> removed the clamp, so
    /// that reason is gone — a 100% group at x30 now fires THIRTY weighted picks and the authored
    /// weights survive intact. The exemption came off with it (owner, 2026-08-18: *"killing a mob to
    /// yield stuff as much as i killed 30 ... no economy nor drop % nor drop amount is wasted"*, and his
    /// own worked example: *"100% chance 100% item => x20 = 100% chance x20 amount"*).</para>
    ///
    /// <para>The predicate stays because the distinction is still real when READING a table — these are
    /// the groups whose authored numbers are absolutes rather than a x1 design.</para></summary>
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
    /// times the group's own multiplier, times the PLAYER's own drop multiplier. Everything that shows or
    /// rolls a drop chance must go through here — the kill roll, the target-inspect list and
    /// tools/BalanceMatrix — or the number on screen stops being the number you get, which is the one bug
    /// this whole system exists to avoid.</summary>
    /// <param name="playerMult">The looking/killing player's own drop multiplier — a premium Rune of Drop
    /// (1.2 = +20%), or 0 for a Rune of Sinners. It is a PARAMETER rather than arithmetic at the call site
    /// precisely because of the rule above: a player wearing a Drop rune must be shown the chance they
    /// actually roll, and the only way to keep three readers agreeing is to give them one function.
    /// Defaults to 1 for every caller with no player in hand (BalanceMatrix, catalog audits).
    ///
    /// <para>Unlike the global rate it applies to the GUARANTEED groups too. That exemption protects his
    /// authored absolutes from a server-wide rate ("at x10 or x200 I still want the group chances at their
    /// current ones"); a rune is one player's own purchase, and a Rune of Drop that visibly skipped the
    /// scroll group would read as broken. The 100% groups are unaffected in practice — they are already at
    /// the clamp.</para></param>
    public static float EffectiveRate(int groupId, float playerMult = 1f) =>
        RateConfig.World.DropChance
        * RateConfig.DropGroupRate(GroupName(groupId))
        * Math.Max(0f, playerMult);

    /// <summary>An entry's authored chance with its PER-ITEM multiplier applied, but NOT the group or
    /// global rate. This is the number that acts as a WEIGHT inside an exclusive group, so the weighted
    /// pick and the group's fire chance are built from the same quantity and cannot disagree.</summary>
    public static float ItemWeight(DropEntry e) => e.Chance * RateConfig.DropItemRate(e.ItemId);

    /// <summary>THE chance one drop entry actually rolls at, before the level-gap penalty: the authored
    /// chance times all FOUR knobs (per-item x per-group x global x the player's own rune). Everything
    /// that rolls or DISPLAYS a drop chance goes through here — the kill roll, the target-inspect list and
    /// tools/BalanceMatrix — so the number on screen stays the number you get.</summary>
    public static float EffectiveChance(DropEntry e, float playerMult = 1f) =>
        ItemWeight(e) * EffectiveRate(e.GroupId, playerMult);

    /// <summary>Hard ceiling on the copies ONE entry (or one group) may yield from a single kill. The
    /// rate is admin-editable LIVE and the drop roll runs inside the single-writer tick loop, so a
    /// mistyped x100000 must not turn one kill into a hundred thousand iterations.</summary>
    public const int MaxDropCopies = 1000;

    /// <summary>HOW MANY TIMES a drop fires. This is the whole of the rate model above 100%: the WHOLE
    /// part of the chance is guaranteed and the FRACTION is rolled, so <c>E[copies] == chance</c> exactly
    /// and "x30" means "as if you had killed thirty" with nothing lost to a clamp (owner, 2026-08-18).
    ///
    /// <para>His worked example: 250% = two guaranteed copies plus a flat 50% roll for a third. ⚠ The
    /// remainder is FLAT — never re-multiplied by the item's or group's own chance. Those factors are
    /// already inside <paramref name="chance"/>; applying them twice would under-deliver the rate, and
    /// worst on the rarest rows (a 3.6% entry at x30 would pay x27.9 instead of x30).</para>
    ///
    /// <para>⚠ FLOOR, not roll, was considered and rejected: it would make the knob DEAD between whole
    /// numbers — x1.5 and x1.99 would both pay exactly 1 — so every rate set between two integers would
    /// silently do nothing.</para>
    ///
    /// <para>For a GROUP the chance passed here is the SUM of its members and each copy is then its own
    /// weighted pick, so the authored weights are preserved however high the rate goes. That is what let
    /// the guaranteed-group exemption come off — see <see cref="IsGuaranteedGroup"/>.</para></summary>
    /// <param name="roll">A uniform [0,1) sample. Passed IN so this stays deterministic and RNG-free:
    /// the server hands it its tick RNG, tools hand it whatever they measure with.</param>
    public static int DropCopies(float chance, double roll)
    {
        if (chance <= 0f) return 0;
        if (chance > MaxDropCopies) chance = MaxDropCopies;
        int whole = (int)chance;
        return whole + (roll < chance - whole ? 1 : 0);
    }

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
    /// <summary>The ENCHANT-SCROLL half of an elite's or boss's table (0.49.0, owner's D1 spec).
    /// Like <see cref="GearDrops"/> this is built at KILL time, because rank belongs to the SPAWN;
    /// unlike it, it ADDS to the normal table rather than replacing it — an elite still rolls the
    /// ordinary scrolls group, and this is the rarer layer on top.
    ///
    /// Everything is keyed off the BAND the mob's level sits in, which is why his ladder falls out
    /// with no special cases: an elite at 78 is in the A band, so "Legendary from elites at a low
    /// chance and bosses higher, 76+" is just the band's Normal scroll; a boss at 82 is in S, so
    /// "Mythic from bosses 80+" is the same line. A boss also pays the band BELOW its own, which is
    /// how an 80+ boss drops both the S and the A scroll (bosses pay several pieces — see
    /// <see cref="BossGearRates"/>).
    ///
    /// GREATER is the elite reward and SAFE is boss-only, both at the mob's own band — the two types
    /// that make an enchant worth attempting are exactly the two you cannot farm off a normal mob.
    ///
    /// ⚠ These are INDEPENDENT rolls (GroupId 0 = the "other" tuning group), so they take the global
    /// rate AND that group's x3 — the delivered chances are three times the numbers authored here,
    /// noted per line. The "dungeon monsters at 90" rung of his spec has nowhere to live until
    /// instances exist; it is flagged, not faked.</summary>
    public static IEnumerable<DropEntry> EnchantScrollDrops(int level, MobRank rank)
    {
        var band = EnchantRules.GradeOf(level);
        if (band == EnchantGrade.None || rank == MobRank.Normal)
            yield break;   // F-band creatures have no scroll at all — there is no F scroll.

        string Normal(EnchantGrade g) => ItemCatalog.EnchantScrollKey(ScrollKind.Normal, g);

        if (rank == MobRank.Elite)
        {
            yield return new DropEntry(Normal(band), 0.030f);                                    // 9%
            yield return new DropEntry(ItemCatalog.EnchantScrollKey(ScrollKind.Greater, band),
                                       0.006f);                                                  // 1.8%
            yield break;
        }

        // BOSS. Its own band and the one below it, so the top of the ladder is reachable at all.
        yield return new DropEntry(Normal(band), 0.100f);                                        // 30%
        if (band > EnchantGrade.E)
            yield return new DropEntry(Normal(band - 1), 0.100f);                                // 30%
        yield return new DropEntry(ItemCatalog.EnchantScrollKey(ScrollKind.Greater, band),
                                   0.030f);                                                      // 9%
        yield return new DropEntry(ItemCatalog.EnchantScrollKey(ScrollKind.Safe, band),
                                   0.0015f);                                                     // 0.45%
    }

    /// <summary>The TOP-RUNG material faucet: Epic, Legendary and Mythic crafting mats off ELITES and
    /// BOSSES, banded by the creature's own grade (`BL-05`, 2026-08-13).
    ///
    /// 🔑 **Why this exists at all.** Measured (`tools/BalanceMatrix` §M, docs/balance/CraftingMats.md):
    /// <c>StandardDrops</c> gates mats at Common 1.76/kill, Uncommon 30+, Rare 60+, Epic 76+ at
    /// 0.015 — **and then stops**. Legendary and Mythic materials dropped from *nothing in the game*.
    /// Their only source was refining at 7-in-1-out on top of that 0.015, which put **one Legendary mat at
    /// 467 kills and one Mythic at 3,267**, and priced the owner's own authored S recipe at
    /// **3 to 6 YEARS of continuous farming** (15-30 for a Mythic one). His crafting ladder needs its top
    /// three rungs to be reachable; no target curve can buy a *pile* of a mat that costs 6 farm hours each.
    ///
    /// 🔑 **Why ELITES and not bosses.** Also measured (`M11`), and it corrected the estimate that came
    /// with the proposal: an elite camp runs **110 kills/h — 147% of a normal farm** — because a camp is
    /// RESPAWN-limited (6 camps, ~3.8 held, 125s timer) where ordinary farming is WALK-limited. A boss is
    /// **0.09 kills/h** on a ~10.75 h timer, so a boss can gate a one-off and never a quantity. Bosses are
    /// still the richer roll here; they are simply not the supply.
    ///
    /// This is the same move `D1` already made for enchant scrolls when the normal-mob faucet closed at B
    /// (<see cref="EnchantScrollDrops"/>), and it finally gives an elite camp a reason to exist for a
    /// level-80 farmer — the same argument that justified it for scrolls.
    ///
    /// 🔑 **ALL FIVE material types, and this is the one place mat flavor is deliberately dropped.**
    /// Everywhere else a creature's materials follow its CATEGORY, which is right and is what forces
    /// cross-profession trade. It cannot survive at the top, and `M12` is what proved it rather than
    /// argued it: above level 61 the categories that actually exist do not span the five types, so a
    /// flavored top faucet leaves whole recipes with an ingredient that **drops from nothing anywhere in
    /// the band** — the A band priced every weapon, body, helmet and jewel at *never*, because no A-band
    /// creature is an Animal and a weapon needs Wood. Flavor at the top is not a trade incentive, it is
    /// an uncraftable recipe. (`M10` had already measured the same thing in its milder form: a **2-3.6×**
    /// per-type penalty, *"A 2.5/1.6/1.0/1.0 — Ingot-only band"*.)
    ///
    /// ⚠ INDEPENDENT rolls in the "other" tuning group, so the delivered chance is **3×** what is
    /// authored below (same as <see cref="EnchantScrollDrops"/>). Per-kill totals noted per line, and
    /// those totals are split five ways across the types.</summary>
    public static IEnumerable<DropEntry> EliteMatDrops(int level, MobRank rank, MobCategory cat)
    {
        if (rank == MobRank.Normal) yield break;
        _ = cat;   // flavor is deliberately NOT applied here — see the 🔑 above

        // A boss is worth ~4 elites per kill. It cannot be the supply (0.09 kills/h), so this is a
        // reward for the trip, not a rate anyone can farm against.
        float rankMult = rank == MobRank.Boss ? 4f : 1f;
        var types = Crafting.MaterialTypes;

        IEnumerable<DropEntry> Rung(ItemRarity rarity, float perKill)
        {
            float each = perKill * rankMult / (3f * types.Length);   // /3 = the "other" group's x3
            foreach (var t in types)
                yield return new DropEntry(Crafting.MaterialId(t, rarity), each);
        }

        // C band (52-60) — the first rung whose recipe names a mat its own band cannot drop: an L3 smith's
        // ACCENT is Epic, and normal creatures pay no Epic until 76. Without this a C weapon is not
        // expensive, it is impossible.
        if (level >= 52 && level < 61)
            foreach (var e in Rung(ItemRarity.Epic, 0.05f)) yield return e;      // 0.05/kill — accent only
        // B band (61-75) — Epic is now the BULK, so the rate steps up four-fold.
        else if (level >= 61 && level < 76)
        {
            foreach (var e in Rung(ItemRarity.Epic, 0.55f)) yield return e;      // 0.55/kill
            foreach (var e in Rung(ItemRarity.Legendary, 0.01f)) yield return e; // 0.01/kill — accent only
        }
        // A band (76-79) — Epic bulk for the B crafter behind you, Legendary bulk for your own rung.
        else if (level >= 76 && level < 80)
        {
            foreach (var e in Rung(ItemRarity.Epic, 0.60f)) yield return e;      // 0.60/kill
            foreach (var e in Rung(ItemRarity.Legendary, 0.55f)) yield return e; // 0.55/kill
            foreach (var e in Rung(ItemRarity.Mythic, 0.012f)) yield return e;   // 0.012/kill — accent only
        }
        // S band (80+) — the only place Mythic is a quantity rather than a keepsake.
        else if (level >= 80)
        {
            foreach (var e in Rung(ItemRarity.Legendary, 1.60f)) yield return e; // 1.60/kill
            foreach (var e in Rung(ItemRarity.Mythic, 0.055f)) yield return e;   // 0.055/kill
        }
    }

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
        // ENCHANT SHARE (owner, playtest-18 V2b, 2026-08-05: *"the attribute scrolls and enchant scrolls
        // also need to lower the chances + move them in the lvls a bit"*). The enchant scroll used to take
        // HALF the rung, which measured at 20 % of all kills from level 1, 30 % from 20 and 35 % from 45 —
        // ~360 scrolls over a 14-15 h farm. That does not flood the ECONOMY (an enchant scroll has no
        // Value and sells for 0) but it floods the BAG and makes enchanting feel free. At 0.15 it is
        // roughly one per eleven kills. The buff half is unchanged; the rung simply sums to less, which
        // this group is already designed to do (see the Always-group note below).
        //
        // 🔴 0.15 WAS STILL A FLOOD (owner, playtest-21 `62j`): *"I got 80 scroll by lvl 28 .. I need
        // like 2-3 .. enchant scrolls must be for over farm not a casual one"*. His measurement pins
        // the arithmetic exactly — the E rung was 0.40 x 0.15 = 6% of every kill and is the only rung
        // live below 40, so 80 scrolls is ~1330 kills across 20→28, which is what that stretch takes.
        // 2-3 over the same 1330 kills is 0.2% per kill, so the E rung wants 0.002 and the share wants
        // 0.005 — a 30x cut. The RUNG WEIGHTS are untouched: the ladder's shape (E richer than B) was
        // never the complaint, only its height. Resulting marginal chances per kill:
        // E 0.20% · D 0.15% · C 0.10% · B 0.075%.
        //
        // ⚠ This is a faucet, not a supply: the Apothecary's boxes, elites and bosses (EnchantScrollDrops)
        // are unchanged, so "over farm" still pays — a normal-mob kill simply stops being the source.
        const float EnchantShare = 0.005f;

        // ENCHANT SCROLLS ARE BANDED NOW, not floored (0.49.0, D1). A scroll is locked to one grade of
        // gear, so the old "every rung keeps dropping forever" would rain E-grade scrolls on a level-80
        // farm that can never spend one — the exact bag clutter the inventory pass was about. Each rung
        // therefore has a CEILING as well as a floor, and it is deliberately generous: a rung lives
        // until the band TWO above it opens, so you keep finding scrolls for gear you are still wearing
        // while levelling past it. Two or three rungs are live at any level, same as before.
        //
        // ⚠ Normal mobs stop at the B rung. A and S normal scrolls, and every Greater and Safe, come
        // from elites and bosses only — see EnchantScrollDrops, which the kill roll layers on by RANK.
        //
        // The share is unchanged from playtest-18 V2b (see the note above): weight x 0.15 into the
        // exclusive scrolls group, so the live rungs sum to roughly what the three old ones did.
        void EnchantRung(bool live, float weight, string id)
        {
            if (live) drops.Add(new(id, weight * EnchantShare, GroupId: GroupScrolls));
        }
        EnchantRung(level is >= 20 and < 52, 0.40f, ItemCatalog.ScrollNormalE);
        EnchantRung(level is >= 40 and < 61, 0.30f, ItemCatalog.ScrollNormalD);
        EnchantRung(level is >= 52 and < 76, 0.20f, ItemCatalog.ScrollNormalC);
        EnchantRung(level is >= 61 and < 80, 0.15f, ItemCatalog.ScrollNormalB);
        // Nothing above B from a normal creature — that is his ladder, not an omission: "Legendary
        // from elites at a low chance and bosses higher 76+, Mythic from bosses 80+". So the normal-mob
        // enchant faucet CLOSES at 80, and from there the scrolls are an elite/boss reward. It is also
        // what finally gives an elite camp a reason to exist for a level-80 farmer.
        // ⚠ The buff half is an explicit PER-ITEM chance, not the rung's share split N ways (which is
        // what it was until playtest-17 E3). With the 17 buff scrolls removed, a split would have
        // handed their entire share to the potions left standing — silently DOUBLING the potion faucet
        // as a side effect of a change whose whole point was to remove drops, not move them. The
        // numbers passed below are exactly what one item delivered under the old split
        // (rung weight × 0.5 ÷ 19, or ÷ 9 for the two top rungs), so every surviving potion drops at
        // the rate it always did and the scrolls' share simply leaves the world.
        void BuffRung(float perItem, string[] buffs)
        {
            foreach (var b in buffs)
                drops.Add(new(b, perItem, GroupId: GroupScrolls));
        }
        // Each rung carries that rarity's buff potions and the Dash potion. The group's total weight
        // does NOT grow when a rung gains items — it is split finer. That is the point: more variety
        // at the same faucet.
        // ⚠ NO BUFF SCROLL DROPS AT ALL, at any rung, from any creature, boss included (owner,
        // playtest-17 E3: *"remove every buff SCROLL from drops, even bosses"*). They come out of the
        // Apothecary's Blessing Box and nothing else, which is what gives 250k something to buy. The
        // rung weights are UNCHANGED, so removing 17 ids from the lists does not cut the faucet — it
        // concentrates it on the potions and Dash, which is the intended trade.
        //
        // 🔑 ONLY THREE FAMILIES DROP, playtest 28 (owner: *"potions drop are limited to
        // alacrity/fury/swift + dash-ocassionally -> the other buff potions are only from the
        // apothecary masters"*). Swift, Alacrity and Fury are the THREE SPEED families — move, cast
        // and attack — and Dash is the burst. The six that left the faucet (Agility, Might, Bulwark,
        // Force, Ward, Aim) are on the Apothecary's shelf instead, BOTH rungs, so nothing became
        // unobtainable; it moved from a faucet to a price.
        //
        // ⚠ The rung WEIGHTS are untouched again, exactly as when the scrolls came out: this does not
        // narrow the faucet, it concentrates it. Ten ids became four at rung 1, so a buff potion drops
        // just as often and is two and a half times more likely to be one of the three you can only
        // get this way. That is the point — a speed potion is a levelling consumable you burn, the
        // stat potions are a shopping decision.
        // ---- SWIFT / ALACRITY / FURY ARE BANDED TOO, on their OWN ladder (owner, 2026-09-03) -------
        // 🔑 HIS BAND, verbatim: *"Buff potions below 40 drop common at 40~52 start to mix at 52~60
        // drom uncommon and 61+ stop"*. Same SHAPE as the dash band below, shifted down a tier and with
        // a hard stop at the end:
        //
        //      level < 40     Common only
        //      level 40-51    Common + Uncommon    (the mix)
        //      level 52-60    Uncommon only
        //      level 61+      NOTHING — the buff-potion faucet closes
        //
        // ⚠ Both ends move. The Uncommon rung used to open at **20** and now opens at 40; the Common
        // rung used to drop forever and now stops at 51. And 61+ is the part with no precedent in this
        // group: from there a normal kill pays no buff potion at all, so the scrolls group above 60 is
        // the enchant rungs alone (C to 76, B to 80) and, past 80, nothing.
        //
        // 🔑 THAT IS NOT A DEAD END, which is the only reason a hard stop is safe here: these three
        // have two sources that do not care about level — the Apothecary sells the COMMON rung of all
        // nine families for gold forever, and a player Potion Master crafts Common at L2 and UNCOMMON
        // at L4. So closing the faucet at 61 moves an endgame consumable from loot to the economy,
        // which is the same trade playtest 28 made when the six stat potions left the tables. It does
        // NOT make anything unobtainable — check that again before narrowing any other faucet.
        if (level <= 51)
            BuffRung(0.0105f,
                new[] { ItemCatalog.SpeedPotionC, ItemCatalog.CastPotionC, ItemCatalog.AtkPotionC });
        if (level is >= 40 and <= 60)
            BuffRung(0.0053f,
                new[] { ItemCatalog.SpeedPotionU, ItemCatalog.CastPotionU, ItemCatalog.AtkPotionU });

        // ---- DASH IS BANDED, and it is the ONLY one of the four that is (owner, 2026-09-03) --------
        // 🔑 HIS BAND, verbatim: *"Common drop from mobs to 52 after 52~60 start to mix with uncommon
        // and after 60 is only uncommon"*. So the Lesser rung has a CEILING and the plain one has a
        // floor, and they overlap for the nine levels between:
        //
        //      level < 52     Lesser only          (+15 move)
        //      level 52-60    Lesser + plain       (the mix)
        //      level 61+      plain only           (+30 move)
        //
        // ⚠ This is NOT the shape `BL-152` left three hours earlier, and both halves move:
        //   • the plain Dash used to drop from level **20**, and now starts at 52 — the whole 20-51
        //     stretch was handing out the +30 rung he says belongs to a level-52 farm; and
        //   • the Lesser used to drop FOREVER, with no ceiling, so a level-85 kill could still pay a
        //     +15 bottle. It stops at 60.
        // Neither was wrong before — he had simply never given the bands, and `BL-152` only ruled on
        // WHICH RARITIES drop, not on where. This is that second half.
        //
        // 🔑 WHY DASH ALONE. Swift / Alacrity / Fury stay on the shared rungs above, unbanded, because
        // his sentence is about the dash line and nothing else. It is also the only one of the four
        // whose ladder COLLIDES WITH A CLASS SKILL — see the FamDash ordering in Skills.Common.cs,
        // where the rogue's Sprint interleaves with these very rungs. A stat potion has no such twin.
        //
        // The per-item weights are the ones the two rungs already carried, so where a rung is live the
        // faucet is exactly what it was; what changed is only WHERE each is live. (It nets out slightly
        // narrower: a mob under 52 no longer pays the plain rung at all.)
        if (level <= 60)
            drops.Add(new(ItemCatalog.DashPotionC, 0.0105f, GroupId: GroupScrolls));
        if (level >= 52)
            drops.Add(new(ItemCatalog.DashPotionU, 0.0053f, GroupId: GroupScrolls));
        // 🔑 `BL-152` — DASH NOW STOPS AT UNCOMMON, like every other potion line (owner, 2026-09-03:
        // *"dash pots to drop to uncommon ... all else from crafters"*). Greater, Superior and Grand
        // left the faucet; Supreme was already craft-only. So the two rungs above are gone and rungs
        // 3+ of this group are now the ENCHANT SCROLL alone.
        //
        // 🔑 This finishes the rule the two passes above started. Playtest-17 `E3` took the scrolls
        // out and playtest 28 cut the stat potions to three speed families, each time on the same
        // principle — **the top of a ladder is bought, not found** — and each time Dash was written
        // down as the deliberate exception (*"the one consumable he asked to leave alone"*). It is no
        // longer an exception, so the sentence is now true without a footnote.
        //
        // ⚠ Unlike those two passes, this one DOES narrow the faucet rather than concentrate it.
        // There is nothing left at rungs 3-5 to redistribute the weight onto: the three removed ids
        // were the whole of their rungs, so a level 45+ mob simply rolls this group less often now.
        // That is the intended shape — from 45 up, a Dash above Uncommon comes from a crafter.

        // ---- ALWAYS (§4): the consumable group — a healing potion, a return scroll or a resurrection
        //      scroll. The name is now historical: it fired on EVERY kill until playtest-17 cut the
        //      potion and return-scroll faucets (see the weights below). The potion TIER still tracks
        //      the mob's level, so the rarity rung is which of the two you get, not a level-70 mob
        //      handing out Minor potions.
        // POTION TIER FLOORS (owner, 2026-08-03, playtest-17 E2): "Uncommon must not drop before 40,
        // Rare before 61". So the ladder is Common below 40, Common+Uncommon to 60, Uncommon+Rare from
        // 61 — potHigh is one rung above potLow, and neither may cross its floor. Below 40 both rungs
        // are the Minor potion; the two weights simply add, which is the intended "only Common exists".
        string potLow  = level >= 61 ? ItemCatalog.HealingPotion : ItemCatalog.MinorPotion;
        string potHigh = level >= 61 ? ItemCatalog.GreaterPotion
                       : level >= 40 ? ItemCatalog.HealingPotion
                       : ItemCatalog.MinorPotion;
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
        // would take the scrolls down with the potions. The weights are the right lever.
        //
        // ⚠ THE GROUP NO LONGER SUMS TO 1.0, AND THAT IS THE POINT (owner, 2026-08-03, playtest-17
        // E1/E2). It used to: every kill yielded a consumable, which by level 23 had handed him 550
        // return scrolls (he uses ~1 per 250 kills) and 320 healing potions. His verdict: return
        // scrolls /20, healing potions /10 — "if I need them I need to buy them". The drop roll takes
        // a group once at its SUMMED chance and only then picks a member, so a branch summing to ~0.06
        // simply means the group fires on ~6 % of kills and is silent on the rest. Do not "restore"
        // the 1.0 sum; the potion economy is now a faucet, not a guarantee.
        //
        // RESURRECTION is a TENTH of the return scroll at every rung (owner, same day): *"if you want
        // to resurrect you buy — if you're lucky you get the drop"*. It is the one consumable whose
        // absence is supposed to hurt, so it is the rarest thing in the group rather than, as it was,
        // the most common. The ratio is deliberate and holds for the Ultimate pair too.
        void Always(string item, float weight) =>
            drops.Add(new(item, weight, 1, 1, GroupId: GroupAlways));
        if (!topLevel)
        {
            Always(potLow,  0.020f); Always(ItemCatalog.ScrollReturn,    0.025f);
            Always(potHigh, 0.010f); Always(ItemCatalog.ScrollResurrect, 0.0025f);
        }
        else
        {
            Always(potLow,  0.015f); Always(ItemCatalog.ScrollReturn,    0.020f);
            Always(potHigh, 0.010f); Always(ItemCatalog.ScrollResurrect, 0.0020f);
            Always(ItemCatalog.GreaterPotion,           0.002f);
            Always(ItemCatalog.ScrollReturnUltimate,    0.0015f);
            Always(ItemCatalog.ScrollResurrectUltimate, 0.00015f);
        }

        // ---- GEAR (§2/§3/§4): the four grade-locked, slot-randomised groups. The BROKEN jewels that
        //      used to be the level 1-5 accessory line are gone from here — §1 makes the F Common jewels
        //      (necklace/ring/earring_t1_common) that line, and the Jewel group drops them from level 1.
        //      The broken pieces stay in the catalog and on the starter vendor's shelf.
        drops.AddRange(GearDrops(level, MobRank.Normal));

        // ---- ATTRIBUTE SCROLLS. Each is banded to the gear grade it can touch (D-C-B / A / S),
        //      so a mob only drops the scrolls that are useful against the gear at its own level.
        //      These are the ONLY source of attributes now, which is why the entry scroll of each
        //      band is the common one and the "top half" scrolls stay rare.
        //      ⚠ THESE ARE INDEPENDENT ROLLS, so unlike the guaranteed groups they DO take the global
        //      DropChanceRate (×3). That is why the authored 0.05+0.03+0.01 was landing as a measured
        //      **27 % of every kill** from level 40 — an accident of which side of the exemption they sit
        //      on, not a decision. Cut ~5× and the three rungs spread out over the band they serve
        //      (owner, playtest-18 V2b: "lower the chances + move them in the lvls a bit"), so the
        //      top-half re-roll is not handed out the moment the band opens.
        if (level >= 40) drops.Add(new(ItemCatalog.AttrScrollCommon, 0.012f));
        if (level >= 52) drops.Add(new(ItemCatalog.AttrScrollUncommon, 0.006f));
        if (level >= 61) drops.Add(new(ItemCatalog.AttrScrollRare, 0.002f));
        if (level >= 76) drops.Add(new(ItemCatalog.AttrScrollEpic, 0.008f));
        if (level >= 80) drops.Add(new(ItemCatalog.AttrScrollLegendary, 0.003f));
        if (level >= 84) drops.Add(new(ItemCatalog.AttrScrollMythic, 0.001f));
        return drops.ToArray();
    }

    // ===================================================================================
    //  THE TWO DEFENCE ARCHETYPES (BL-11). *"We had a anti magic mobs (lower pdef more mdef) and
    //  anty physical (less m def more pdef) — this should feed your mres passive."*
    //
    //  The pair only existed as a comment on MobMod and as ONE template (Watcher Eye), and neither
    //  half touched mRes — so an "anti magic" mob was just a bigger M.Def divisor, which a levelling
    //  mage out-scales, and there was no anti-physical mob in the game at all.
    //
    //  Authored ONCE here and shared, so the pattern reads in the bestiary instead of being eight
    //  hand-tuned numbers. Each is a genuine two-way trade: the warded thing wants a fighter, the
    //  armoured thing wants a mage. That is what makes a party composition matter in a field.
    // ===================================================================================
    /// <summary>Warded: harder for a MAGE (M.Def ×1.5 AND −20% magic damage taken), softer for a
    /// fighter (P.Def ×0.8). Wisps, wraiths, liches — things made of magic.</summary>
    private static MobMod AntiMagic(string name = "Warded") =>
        new(PDef: 0.8f, MDef: 1.5f, MagicResist: 0.20f, Name: name);

    /// <summary>Ironhide: harder for a FIGHTER (P.Def ×1.5), softer for a mage (M.Def ×0.8 and
    /// +20% magic damage TAKEN — a real weakness, not merely the absence of a resist). Shielded
    /// skeletons, plated knights, brutes.</summary>
    private static MobMod AntiPhysical(string name = "Ironhide") =>
        new(PDef: 1.5f, MDef: 0.8f, MagicResist: -0.20f, Name: name);

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
            Mob("ashen_wolf", "Ashen Wolf", 10, MobCategory.Animal, 140f, true, clan: ClanWolf),
            Mob("werewolf", "Werewolf", 12, MobCategory.Humanoid, 132f, true, clan: ClanWolf),
            Mob("hook_spider", "Hook Spider", 14, MobCategory.Insect, 130f, true),
            Mob("orc_archer", "Orc Archer", 16, MobCategory.Humanoid, 132f, true, role: MobRole.Archer, clan: ClanOrc),
            Mob("skeleton_grunt", "Skeleton Grunt", 18, MobCategory.Undead, 120f, true, clan: ClanSkeleton),
            // ANTI-PHYSICAL (BL-11): the shield is the whole creature. The first of the pair a
            // player meets, deliberately early — it is where "bring the mage" is taught.
            Mob("shield_skeleton", "Shield Skeleton", 20, MobCategory.Undead, 115f, true,
                AntiPhysical("Shieldwall"), clan: ClanSkeleton),
            Mob("grizzly_bear", "Grizzly Bear", 22, MobCategory.Animal, 135f, true),
            Mob("cinder_imp", "Cinder Imp", 24, MobCategory.Demon, 142f, true),
            // MAGIC monster: high M.Def / low P.Def — hard for mages, easy for fighters.
            // Also a CASTER (Mage role): no basic attack, nukes from range, sits helpless at 0 MP.
            // Its M.Def 2.0 is kept — steeper than the shared AntiMagic preset, and it is the
            // archetype's namesake; BL-11 added the mRes half it never had.
            // ⚠ ITS P.DEF WAS 0.5 AND IS NOW 0.8 (BL-78 item 2, 2026-08-27). 0.5 was authored
            // against a Mage role that ALSO multiplied defence by 0.7, so the creature actually
            // stood in ×0.35 — the single worst case of his "caster mobs are not weaker than the
            // other ... a bit less pdef" and the reason the fix is two-sided. At 0.8 it matches the
            // shared AntiMagic preset, and with the role's new ×0.85 it lands on ×0.68.
            Mob("watcher_eye", "Watcher Eye", 26, MobCategory.MagicCreature, 130f, true,
                new MobMod(MDef: 2f, PDef: 0.8f, MagicResist: 0.25f, Name: "Magic Monster"), MobRole.Mage),
            Mob("lizardman_warrior", "Lizardman Warrior", 28, MobCategory.Humanoid, 132f, true, clan: ClanLizard),
            Mob("marauder_recruit", "Marauder Recruit", 30, MobCategory.Humanoid, 132f, true, clan: ClanMarauder),
            Mob("mantis_worker", "Mantis Worker", 32, MobCategory.Insect, 140f, true, clan: ClanMantis),
            Mob("grave_robber_fighter", "Grave Robber Fighter", 32, MobCategory.Humanoid, 132f, true),
            Mob("medusa", "Medusa", 34, MobCategory.Humanoid, 132f, true),
            Mob("plunder_beetle", "Plunder Beetle", 34, MobCategory.Insect, 140f, true),
            Mob("wyrm", "Wyrm", 35, MobCategory.Dragon, 150f, true, clan: ClanDrake),
            Mob("marsh_mantis_soldier", "Marsh Mantis Soldier", 37, MobCategory.Insect, 140f, true, clan: ClanMantis),
            Mob("fen_lizardman_archer", "Fen Lizardman Archer", 39, MobCategory.Humanoid, 132f, true, role: MobRole.Archer, clan: ClanLizard),
            // ⚠ `rift_portling` (Rift Portling) WAS HERE AND IS DELETED — owner, 2026-08-28: *"remove
            // the rune porting mob it's to op for a normal zone +120% pDef to op"*. It carried
            // MobMod(Hp: 3.56, PDef: 2.2, MDef: 1.27) — that P.Def 2.2 IS his +120%.
            //
            // 🔑 THE REAL LESSON IS HOW IT GOT INTO A NORMAL ZONE AT ALL. It was authored as a
            // deliberate "CHAMPION outlier" — a mini-boss shape — but rosters are DERIVED from the
            // level band (MobCatalog.InBand), so a champion template with a natural level of 40 is
            // rostered into EVERY generated 40-44 camp automatically. Nothing marked it as special,
            // so nothing kept it out. An outlier needs `HandPlaced: true` (the fence the BL-47 demo
            // creatures use) or it is not an outlier, it is just a very hard normal mob.
            //
            // Deleted rather than fenced because he said remove; nothing referenced it (no quest, no
            // authored zone, no drop-table cross-reference), so the 40-44 roster simply loses one
            // entry. If a champion tier is wanted later, author it HandPlaced and place it.
            Mob("dune_orc_archer", "Dune Orc Archer", 40, MobCategory.Humanoid, 132f, true, role: MobRole.Archer, clan: ClanOrc),
            Mob("ridge_orc_overlord", "Ridge Orc Overlord", 42, MobCategory.Humanoid, 132f, true, clan: ClanOrc),
            Mob("harpy", "Harpy", 42, MobCategory.Humanoid, 138f, true),
            // ANTI-MAGIC (BL-11): a lich is made of the stuff. Also the Hollow Crypt boss.
            Mob("grave_lich", "Grave Lich", 44, MobCategory.Undead, 120f, true, AntiMagic("Deathward")),
            // ANTI-PHYSICAL (BL-11): the mid-band plated brute.
            Mob("fomor_brute", "Fomor Brute", 45, MobCategory.Humanoid, 132f, true, AntiPhysical("Ironhide")),
            Mob("marsh_marauder", "Marsh Marauder", 46, MobCategory.Humanoid, 132f, true, clan: ClanMarauder),
            Mob("warped_drake", "Warped Drake", 47, MobCategory.Dragon, 150f, true, clan: ClanDrake),
            Mob("wildhorn_grunt", "Wildhorn Grunt", 48, MobCategory.Humanoid, 132f, true, clan: ClanWildhorn),
            Mob("amber_basilisk", "Amber Basilisk", 48, MobCategory.Animal, 120f, true),
            Mob("ravener", "Ravener", 50, MobCategory.Demon, 145f, true),
            Mob("mantis_follower", "Mantis Follower", 50, MobCategory.Insect, 140f, true, clan: ClanMantis),
            Mob("marauder_warrior", "Marauder Warrior", 51, MobCategory.Humanoid, 132f, true, clan: ClanMarauder),
            Mob("fallen_angel", "Fallen Angel", 52, MobCategory.Demon, 135f, true),
            Mob("thornback", "Thornback", 53, MobCategory.Animal, 135f, true),
            Mob("gaze_hound", "Gaze Hound", 54, MobCategory.Animal, 140f, true, clan: ClanWolf),
            Mob("ash_orc_soldier", "Ash Orc Soldier", 55, MobCategory.Humanoid, 132f, true, clan: ClanOrc),
            Mob("mirror_wraith", "Hall of Mirrors Wraith", 56, MobCategory.Undead, 125f, true, clan: ClanMirror),
            Mob("mirror_ghost", "Mirror Ghost", 56, MobCategory.Undead, 125f, true, clan: ClanMirror),
            Mob("dune_orc_porter", "Dune Orc Porter", 57, MobCategory.Humanoid, 132f, false, clan: ClanOrc),
            // ANTI-MAGIC (BL-11): a wisp IS magic. A caster too, so a fighter has to close on it.
            Mob("aether_wisp", "Aether Wisp", 58, MobCategory.MagicCreature, 115f, true,
                AntiMagic("Aetherward"), MobRole.Mage),
            Mob("hollow_one", "Hollow One", 58, MobCategory.Humanoid, 132f, true),
            Mob("valley_treant", "Valley Treant", 60, MobCategory.Plant, 90f, false),
            Mob("sand_ratman", "Sand Ratman", 60, MobCategory.Humanoid, 132f, true),
            Mob("cursed_blade", "Cursed Blade", 61, MobCategory.Undead, 130f, true),
            Mob("bogwood", "Bogwood", 62, MobCategory.Plant, 90f, false),
            Mob("fen_lizardman", "Fen Lizardman", 62, MobCategory.Humanoid, 132f, true, clan: ClanLizard),
            // Golem-type stone/obsidian body, authored via the leveled MASTERY table: Piercing
            // Resistance L10 (×1.43 P.Def vs sword/dual), Bow Resistance L12 (×2), Blunt Resistance
            // IG (×0.5 = weak). Same effect as a hand MobMod, but "picks a level" like a class.
            Mob("obsidian_knight", "Obsidian Knight", 63, MobCategory.Humanoid, 132f, true,
                MobMasteries.Build(pierce: 10, bow: 12, blunt: 2, magicResist: 5, name: "Stoneplate")),
            Mob("crimson_drake", "Crimson Drake", 64, MobCategory.Dragon, 150f, true, clan: ClanDrake),
            Mob("wildhorn_scout", "Wildhorn Scout", 64, MobCategory.Humanoid, 138f, true, clan: ClanWildhorn),
            // ANTI-PHYSICAL (BL-11): full plate. Also the Sunless Warrens boss.
            Mob("dread_knight", "Dread Knight", 65, MobCategory.Undead, 135f, true,
                AntiPhysical("Dreadplate"), clan: ClanDread),
            Mob("wildhorn_elder", "Wildhorn Elder", 66, MobCategory.Humanoid, 132f, true, clan: ClanWildhorn),
            // ANTI-MAGIC (BL-11): incorporeal — a blade passes through it, a spell does not.
            Mob("spiteful_ghost", "Spiteful Ghost", 66, MobCategory.Undead, 125f, true, AntiMagic("Spiteward")),
            Mob("highland_kookaburra", "Highland Kookaburra", 67, MobCategory.Animal, 135f, false),
            Mob("highland_buffalo", "Highland Buffalo", 68, MobCategory.Animal, 130f, false),
            Mob("highland_buffalo_tamed", "Highland Buffalo (Tamed)", 68, MobCategory.Animal, 130f, false),
            Mob("dread_archer", "Dread Archer", 69, MobCategory.Undead, 132f, true, role: MobRole.Archer, clan: ClanDread),
            Mob("dire_beast", "Dire Beast", 70, MobCategory.Animal, 140f, true, clan: ClanWolf),
            Mob("revenant_minion", "Revenant Minion", 71, MobCategory.Demon, 145f, true),
            Mob("redhorn_footman", "Redhorn Footman", 72, MobCategory.Humanoid, 132f, true, clan: ClanRedhorn),
            Mob("sunland_orc_scout", "Sunland Orc Scout", 73, MobCategory.Humanoid, 138f, true, clan: ClanOrc),
            Mob("redhorn_elite", "Redhorn Elite", 73, MobCategory.Humanoid, 132f, true, clan: ClanRedhorn),
            Mob("redhorn_recruit", "Redhorn Recruit", 74, MobCategory.Humanoid, 132f, true, clan: ClanRedhorn),
            Mob("sunland_orc_warrior", "Sunland Orc Warrior", 75, MobCategory.Humanoid, 132f, true, clan: ClanOrc),
            Mob("redhorn_soldier", "Redhorn Soldier", 76, MobCategory.Humanoid, 132f, true, clan: ClanRedhorn),
            Mob("sunland_orc_commander", "Sunland Orc Commander", 76, MobCategory.Humanoid, 132f, true, clan: ClanOrc),
            Mob("sunland_orc_captain", "Sunland Orc Captain", 77, MobCategory.Humanoid, 132f, true, clan: ClanOrc),
            Mob("redhorn_general", "Redhorn General", 78, MobCategory.Humanoid, 132f, true, clan: ClanRedhorn),
            Mob("emberwyrm_drake", "Emberwyrm Drake", 79, MobCategory.Dragon, 155f, true, clan: ClanDrake),
            Mob("wrathborn_demon", "Wrathborn Demon", 80, MobCategory.Demon, 145f, true),
            Mob("scarlet_mantis", "Scarlet Mantis", 80, MobCategory.Insect, 142f, true, clan: ClanMantis),
            Mob("radiant_scout", "Radiant Scout", 81, MobCategory.Angel, 140f, true, clan: ClanRadiant),
            Mob("radiant_berserker", "Radiant Berserker", 82, MobCategory.Angel, 135f, true, clan: ClanRadiant),
            Mob("radiant_mage", "Radiant Mage", 82, MobCategory.Angel, 132f, true, role: MobRole.Mage, clan: ClanRadiant),
            Mob("splinter_mantis_drone", "Splinter Mantis Drone", 83, MobCategory.Insect, 142f, true, clan: ClanMantis),
            Mob("needle_mantis_overseer", "Needle Mantis Overseer", 84, MobCategory.Insect, 140f, true, clan: ClanMantis),
            Mob("splinter_mantis_walker", "Splinter Mantis Walker", 84, MobCategory.Insect, 142f, true, clan: ClanMantis),
            Mob("drake_leader", "Drake Leader", 85, MobCategory.Dragon, 150f, true, clan: ClanDrake),
            Mob("disciple_of_the_dawn", "Disciple of the Dawn", 85, MobCategory.Humanoid, 132f, true),

            // Training dummy: immortal, stationary, deals no damage. The ZONE sets its level
            // (20/40/60/80 training grounds). No drops. For testing damage/skills.
            new MobType("training_dummy", "Training Dummy", 0f, 0f, Dummy: true,
                Title: "Normal"),

            // The two dummies that HIT BACK (owner, `56c`). Immortal and stationary like the plain
            // one, but each strikes for 1 damage every tick at GameConstants.DummyStrikeRange — one
            // through the magic resolution (fail / crit), one through the physical (miss / crit /
            // block). Ten seconds of standing next to one is a hundred samples of that outcome.
            //
            // The TITLES are the owner's `63h`: three dummies in a row were indistinguishable on
            // screen, so the one line that says which is which goes where every plate already draws
            // one. Without it you cannot report "the magic dummy does nothing" and be believed.
            new MobType("dummy_magic", "Magic Training Dummy", 0f, 0f,
                Dummy: true, Strikes: DummyAttack.Magic, Title: "Magic"),
            new MobType("dummy_physical", "Striking Training Dummy", 0f, 0f,
                Dummy: true, Strikes: DummyAttack.Physical, Title: "Physical"),

            // ===================================================================================
            //  BL-47 STEP 2 — THE FIVE CREATURES BUILT LIKE PLAYERS, and the four ordinary ones to
            //  fight them beside. His words: *"and later we can do 2~5 mobs so I can test"*, and
            //  *"make a demo then we do a system number"* — so these exist to be FOUGHT, not to be
            //  the roster. They are hand-placed in the Proving Grounds and fenced out of `InBand`,
            //  so nothing else in the world changes.
            //
            //  ⚠ THE RACES ARE PLACEHOLDERS. Goblin / Lich / Angel are three names to hang a ±5 lean
            //  on, per his B1 (*"ork have higher con/atk less agi ..while elf have higher agi less
            //  atk/con ... No lvl curve. Can go +-5"*). They are not a proposed bestiary.
            //
            //  🔑 TWO COMPARISONS DECIDE THE SYSTEM, and the row is laid out so each is one step:
            //    • Raider 40 vs Raider 45 — the SAME authored loadout across the ±5 band a template
            //      can spawn in. If one loadout covers the band, no level→grade function is ever
            //      needed, and his "prefixed 100+ mobs with +-5 lvl ranges" costs one number per mob.
            //    • Seraph vs Seraph (Rune) — an authored ×1.55 attack passive against a HELD War Rune
            //      and no passive at all. If the rune stands in, the whole attack side of this design
            //      collapses into an item a creature carries.
            //
            //  The loadouts are `BalanceMatrix` G3.7's own answers, not hand-picked: the optimiser
            //  chose lowest-tier armour under a weapon at or near level tier with enchant on top —
            //  which is his "S grade Mace enchanted to +60 and B grade leather", found by search.
            //  No drops: an experiment pays exp for the fight and changes no economy.
            //
            //  ⚠ THE MobMod MULTIPLIERS ARE MEASURED, NOT COPIED FROM G3.7. They started as G3.7's
            //  "passive still needed" column and were then corrected against `G3.8`, which spawns the
            //  creature and divides it by the twin standing next to it. They differ, and the reason
            //  matters: G3.7 measures against the BARE `MobBaseStats` curve, while a real creature of
            //  the same level ALSO carries BL-14's weapon power factor (a slow 2H weapon buys per-hit
            //  damage). So the attack passive the Seraph actually needs is **×2.07**, not G3.7's
            //  ×1.55 — a whisker past the ×2 he proposed, and past it for a reason that is nobody's
            //  mistake. Re-run `G3.8` after any change here; the numbers are fitted, not derived.
            // ===================================================================================

            // #1 — the BASELINE. At 40 the split loadout alone lands x1.04 / x0.99 / x1.02, so this
            //      creature is deliberately given NO stat passive. If it fights like a level-40 mob,
            //      gear alone reproduced the curve.
            Demo("demo_goblin_raider", "Goblin Raider", 40, MobCategory.Humanoid, 132f,
                Warrior(armorTier: 1, armorQ: ItemRarity.Uncommon, armorEnch: 0,
                        weaponTier: 40, weaponQ: ItemRarity.Rare, weaponEnch: 0,
                        con: +5, atk: +5, agi: -5),
                new MobMod(Name: "Goblin blood (CON +5, ATK +5, AGI -5)")),

            // #2 — the ±5 BAND. Byte-identical build to #1, five levels up, and deliberately left just
            //      as bare, because what it reads IS the answer. Measured (G3.8): defence and HP hold
            //      (P.Def x1.04 -> x0.95, HP x1.10 -> x1.06) but **P.Atk falls x0.87 -> x0.64 in five
            //      levels** — the mob attack curve is the steep one. So one loadout covers a ±5 band on
            //      everything except how hard the creature hits, and that is the number a per-band
            //      passive (or one more enchant rung) would have to carry. Do NOT tune this row flat:
            //      it exists to show the drift, and hiding it would answer his question with a guess.
            Demo("demo_goblin_raider_elder", "Goblin Elder Raider", 45, MobCategory.Humanoid, 132f,
                Warrior(armorTier: 1, armorQ: ItemRarity.Uncommon, armorEnch: 0,
                        weaponTier: 40, weaponQ: ItemRarity.Rare, weaponEnch: 0,
                        con: +5, atk: +5, agi: -5),
                new MobMod(Name: "Goblin blood (CON +5, ATK +5, AGI -5)")),

            // #3 — THE ONE ARCHETYPE THAT MISSES HIS ×2. A caster creature's HP is the single stat
            //      gear cannot reach: G3.6 reads x2.01 at 20 rising to x3.48 at 80, and x3.32 at 60.
            //      His own *"and hp boost"* anticipated it. The question this one asks is not whether
            //      the number works — it does, arithmetically — but whether a x3.3 HP passive READS as
            //      a fair caster or as a sponge.
            Demo("demo_lich", "Cairn Lich", 60, MobCategory.Undead, 120f,
                Nuker(armorTier: 1, armorQ: ItemRarity.Epic, armorEnch: 0,
                      weaponTier: 52, weaponQ: ItemRarity.Common, weaponEnch: 30,
                      con: -5, wit: +5),
                new MobMod(Hp: 3.73f, PDef: 1.02f, MDef: 0.78f, MAtk: 0.97f,
                           Name: "Deathward (CON -5, WIT +5)"),
                MobRole.Mage),

            // #4 — THE TOP BAND, where gear alone still leaves the attack passive real work: x1.55.
            Demo("demo_seraph", "Fallen Seraph", 80, MobCategory.Angel, 140f,
                Warrior(armorTier: 52, armorQ: ItemRarity.Common, armorEnch: 0,
                        weaponTier: 80, weaponQ: ItemRarity.Epic, weaponEnch: 16,
                        agi: +5, con: -5),
                new MobMod(Hp: 1.46f, PDef: 1.05f, MDef: 0.61f, PAtk: 2.07f,
                           Name: "Seraphic wrath (AGI +5, CON -5)")),

            // #5 — HIS B3 ANSWER, MEASURED, AND IT IS A YES. Identical to #4 except the attack passive
            //      is GONE and the creature holds a War Rune instead. Bare, this build reads x0.48 of
            //      its curve's P.Atk; the rune's +100% takes it to **x0.97** — the same place #4 gets to
            //      with an authored, per-band, per-creature ×2.07. One item, no table, no drift.
            Demo("demo_seraph_rune", "Fallen Seraph, Runebearer", 80, MobCategory.Angel, 140f,
                Warrior(armorTier: 52, armorQ: ItemRarity.Common, armorEnch: 0,
                        weaponTier: 80, weaponQ: ItemRarity.Epic, weaponEnch: 16,
                        agi: +5, con: -5, held: ItemCatalog.WarRune),
                new MobMod(Hp: 1.46f, PDef: 1.05f, MDef: 0.61f,
                           Name: "Seraphic wrath (AGI +5, CON -5)")),

            // The CURVE TWINS — ordinary creatures off MobBaseStats at the same levels, carrying no
            // passive at all, so each column of the Proving Grounds is "the same level, built the two
            // ways". They wield what the player-built one wields (BL-14: a mob's weapon decides its
            // per-hit power and rate) — otherwise the comparison silently includes a weapon swap.
            Curve("demo_curve_40", "Standard Marker", 40, WeaponType.TwoHandedSword),
            Curve("demo_curve_45", "Standard Marker", 45, WeaponType.TwoHandedSword),
            Curve("demo_curve_60", "Standard Marker", 60, WeaponType.None, MobRole.Mage),
            Curve("demo_curve_80", "Standard Marker", 80, WeaponType.TwoHandedSword),

            // ===========================================================================
            //  BL-79 — THE GUARDS. Two posts, two tiers. See the GuardTank/GuardArcher note.
            //
            //  Aggro radii are his: "aggro 400 melee / 600 archer" — the archer notices you from
            //  where it can already shoot, which is what makes walking past one a decision.
            //  Run speeds sit at the human-ish 130-135 band: a guard must be able to catch a PK
            //  who picked the fight, but it is leashed to its post like any other creature.
            // ===========================================================================

            // THE TOWN PAIR — level 80, S grade (t80) Epic, +0. The mirror of his reference player,
            // and the tier that is meant to be a real fight rather than a wall. No War Rune here:
            // the rune is what separates the field pair from this one.
            GuardMob("guard_town_tank", "Town Watchman", 80, 132f,
                GuardTank(tier: 80, ench: 0, held: ItemCatalog.WarRune), MobRole.Melee, aggroRange: 400f),
            GuardMob("guard_town_archer", "Town Marksman", 80, 135f,
                GuardArcher(tier: 80, ench: 0, held: ItemCatalog.WarRune), MobRole.Archer, aggroRange: 600f),

            // THE FIELD PAIR — level 90, S grade Epic, +16, and holding a War Rune. His "pieasfull
            // zone guards have everithing s grade +16 and are 90lvl", posted in the quiet farming
            // fields where an outlaw would otherwise hunt undisturbed. Ten levels and sixteen
            // enchant rungs above the town pair: this one is not a duel, it is a closed road.
            GuardMob("guard_field_tank", "Field Warden", 90, 132f,
                GuardTank(tier: 80, ench: 16, held: ItemCatalog.WarRune), MobRole.Melee,
                aggroRange: 400f, mod: GuardTower("Guard tower")),
            GuardMob("guard_field_archer", "Field Longbow", 90, 135f,
                GuardArcher(tier: 80, ench: 16, held: ItemCatalog.WarRune), MobRole.Archer,
                aggroRange: 600f, mod: GuardTower("Guard tower")),
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
    /// Dummies are excluded: the training dummies are placed by hand at fixed levels.
    ///
    /// ⚠ So is everything else HAND-PLACED, which today means the BL-47 Proving Grounds — the five
    /// player-built creatures and their four curve twins. Same reason: the demo's whole promise is that
    /// *nothing else in the world changes*, and without this clause a level-40 Goblin Raider would
    /// immediately be rostered into every generated 40-44 camp in the game. **Clear `HandPlaced` on a
    /// creature when it is ready to join the roster** — that is the switch, and it is per-template.</summary>
    public static MobType[] InBand(int min, int max) =>
        Templates.Where(m => !m.Dummy && !m.HandPlaced && m.Level >= min && m.Level <= max).ToArray();

    /// <summary>Boot guard: every piece a <see cref="MobBuild"/> names must actually exist in the item
    /// catalogue. A missing id is silent and flattering — the creature simply spawns without that slot,
    /// and a naked entity reads as "the player pipeline under-delivers" when the truth is a typo. Not
    /// every rung exists (the S grade is Epic-and-up), so this is a real hazard, not a theoretical one.
    /// Same spirit as the skill-id and abbreviation guards: fail the boot, name the offenders.</summary>
    public static void ValidateBuilds()
    {
        var bad = new List<string>();
        foreach (var m in Templates)
        {
            if (m.Build is not MobBuild b) continue;
            foreach (var (defId, _) in b.Pieces())
                if (ItemCatalog.Get(defId) is null)
                    bad.Add($"{m.Id} (Lv {m.Level}): no such item '{defId}'");
        }
        if (bad.Count > 0)
            throw new InvalidOperationException(
                "Player-built creatures naming gear that does not exist:\n  " + string.Join("\n  ", bad));
    }
}
