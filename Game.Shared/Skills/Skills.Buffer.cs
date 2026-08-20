namespace Game.Shared;

/// <summary>NPC "newbie buffer" buffs — strong, long (1 hour) versions that represent a
/// 3rd-class buffer's MAX-level buffs (a 4th class will give more; the buffer class also
/// gets a Prophecy). They share the SAME BuffKey as the player self-buffs so they OVERRIDE
/// the weaker self-cast versions (high Rank). Not learnable — applied by an NpcRole.Buffer
/// NPC. Channel-split atk (Might = P.Atk, Force = M.Atk) via the buff/effect layer.</summary>
public static partial class SkillCatalog
{
    // The five ORIGINAL blessings. Each was one monolithic multi-effect buff on its own key —
    // which is exactly why a Might potion could stack on top of the buffer's Might. They are now
    // SINGLES too (owner 2026-07-31): npc_might hands out the P.Atk rung and nothing else, and its
    // old companions have their own buttons below. Ids kept (append-only), meaning changed.
    public const string NpcMight  = "npc_might";     // Might   — % P.Atk
    public const string NpcForce  = "npc_force";     // Force   — % M.Atk
    public const string NpcFocus  = "npc_focus";     // Focus   — % crit rate
    public const string NpcSpeed  = "npc_speed";     // the old GROUP — no longer offered
    public const string NpcBody   = "npc_body";      // Body    — % Max HP
    public const string NpcFrenzy = "npc_frenzy";    // Frenzy  — the whole trade-off buff
    // The four SPEED singles, one hour each — the scroll tier handed out one buff at a time.
    // They replaced the "Improved Speed" GROUP the buffer used to give (owner 2026-07-31).
    public const string NpcSwift    = "npc_swift";
    public const string NpcAlacrity = "npc_alacrity";
    public const string NpcAgility  = "npc_agility";
    public const string NpcHaste    = "npc_haste";
    // The rest of the singles, one per family, so every effect the buffer used to bundle can now
    // be taken (and cancelled) on its own — and so a potion of that family competes with it.
    public const string NpcBulwark   = "npc_bulwark";     // % P.Def
    public const string NpcVampirism = "npc_vampirism";   // % melee vampirism
    public const string NpcAccuracy  = "npc_accuracy";    // flat accuracy
    public const string NpcWard      = "npc_ward";        // % M.Def
    public const string NpcResolve   = "npc_resolve";     // flat interrupt resistance
    public const string NpcFerocity  = "npc_ferocity";    // % crit damage
    public const string NpcInsight   = "npc_insight";     // % magic crit rate
    public const string NpcSoul      = "npc_soul";        // % Max MP
    public const string NpcVigor     = "npc_vigor";       // % HP regeneration
    public const string NpcSerenity  = "npc_serenity";    // % MP regeneration
    // The IMPROVED (group) versions at max rungs. No NPC hands these out — a buffer CLASS does —
    // but the admin buff button grants them so the group shape can be seen and tested (owner).
    public const string NpcMightGroup = "npc_might_group";   // Might and Bulwark
    public const string NpcForceGroup = "npc_force_group";   // Force and Ward
    public const string NpcFocusGroup = "npc_focus_group";   // Focus and Ferocity
    public const string NpcBodyGroup  = "npc_body_group";    // Body and Soul
    // "Harmony" GREATER buffs (max-level; owner 2026-07-03). They STACK on top of the six above
    // (distinct BuffKeys). NO LONGER OFFERED by the newbie buffer — see NewbieBuffSet. The defs
    // stay because a real 3rd-class buffer is meant to have them; nothing grants them today.
    public const string NpcHarmonyProtection = "npc_harmony_protection";
    public const string NpcHarmonyWarrior    = "npc_harmony_warrior";
    public const string NpcHarmonyWizard     = "npc_harmony_wizard";
    // (Shield Bless and Harden — his `buffer 3rd.csv` @66 — is NOT here. It is a real improved GROUP
    // over the two shield families, so it lives with the other groups in Skills.Healer.cs as
    // `HolyShield`. AdminBuffSet still hands it out.)

    public const int NpcBuffTicks = 36000;   // 1 hour @ 10 ticks/s
    public const int NpcBuffRank  = 100;     // overrides player self-buffs (rank 1-4)

    /// <summary>The buffs the newbie buffer NPC grants: the BASIC tier only, one hour each — the
    /// scroll tier (owner 2026-07-31: "not the improved and harmonies … just the scroll buffs,
    /// 1h of single basic buff"). So no GROUP buff and no Harmony: the buffer's edge over a potion
    /// is the DURATION, and its ceiling stays below a real buffer class, which keeps the improved
    /// groups. Frenzy stays in (it's a FULL buffer; cancel that one buff if you don't want its
    /// −10% Max HP/MP).</summary>
    public static readonly string[] NewbieBuffSet =
        { NpcMight, NpcBulwark, NpcVampirism, NpcAccuracy,
          NpcForce, NpcWard, NpcResolve,
          NpcFocus, NpcFerocity, NpcInsight,
          NpcBody, NpcSoul, NpcVigor, NpcSerenity,
          NpcSwift, NpcAlacrity, NpcAgility, NpcHaste,
          NpcFrenzy };

    /// <summary>What the ADMIN buff button hands out: EVERYTHING that exists — the improved groups,
    /// the three Harmony blessings and every single (owner 2026-07-31). Harmony and the groups are
    /// the two layers no NPC sells and no consumable can reach, so this is the only way to see a
    /// fully-buffed character, which is the state the balance numbers are read at.
    ///
    /// ⚠ The GROUPS come first, and now they simply WIN: a group covers its families at group rank,
    /// so every single it contains is refused when it arrives after. The bar ends up as the five
    /// group squares + the three Harmonies + Frenzy (the one family no group contains) — nine rows
    /// for the whole game's buff layer, which is the point of the improved tier. The order is kept
    /// deliberate anyway: reversed, the singles would land first and then be evicted, which is the
    /// same picture through twice the work.</summary>
    public static readonly string[] AdminBuffSet =
        new[] { NpcMightGroup, NpcForceGroup, NpcFocusGroup, NpcBodyGroup, NpcSpeed,
                NpcHarmonyProtection, NpcHarmonyWarrior, NpcHarmonyWizard, HolyShield }
            .Concat(NewbieBuffSet).ToArray();

    /// <summary>A HARMONY blessing — the layer above the basic buffs, with no potion, no scroll and
    /// no NPC that sells it. Unlike the rest of this file these are LEARNABLE (Warchanter, from
    /// level 40 — owner 2026-07-31: "for now just to have somewhere the harmony to go"), so they
    /// carry a real MP cost, a cast time and a range. Duration is the ordinary player-buff 20
    /// minutes, not the NPC's hour.
    ///
    /// They keep their own BuffKeys and are NOT split into children: Harmony's whole job is to
    /// stack ON TOP of the basic layer, and nothing else grants what they grant, so there is no
    /// family for them to compete on. Splitting them waits for a second Harmony-tier source.</summary>
    private static SkillDef HarmonyBuff(string id, string name, string buffKey,
        SkillEffect effect, EffectMagnitude[] mags, string desc,
        float physMpCost = 0f, float magicMpCost = 0f) =>
        new(id, name, BaseClass.Mage, effect,
            MpCost: 200, CastTicks: 15, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: buffKey, Rank: NpcBuffRank,
            Category: SkillCategory.Buff, Magnitudes: mags,  SpCost: 100000,
            // Party buffs, like the improved groups they sit above. This was already the recorded
            // decision in docs/design/BuffLadders.md: the autopilot hard-targets SELF for buffs, so a
            // single-target Harmony could never be handed out by a buffer left on auto-farm.
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            PhysMpCostPct: physMpCost, MagicMpCostPct: magicMpCost,
            Description: desc + " (20 minutes).");

    private static SkillDef NpcBuff(string id, string name, string buffKey,
        SkillEffect effect, EffectMagnitude[] mags, string desc,
        float physMpCost = 0f, float magicMpCost = 0f) =>
        new(id, name, BaseClass.Mage, effect,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: NpcBuffTicks, BuffKey: buffKey, Rank: NpcBuffRank,
            Category: SkillCategory.Buff, Magnitudes: mags,
            PhysMpCostPct: physMpCost, MagicMpCostPct: magicMpCost,
            Description: desc + " (buffer's blessing, 1 hour).");

    /// <summary>An NPC-buffer buff that is a GROUP: ONE buff carrying the numbers of every child
    /// named here, covering each child's family. It evicts those singles and outranks anything the
    /// player can drink or read afterwards — a group is by definition the max version of its parts
    /// (docs/design/BuffLadders.md). NpcBuffRank does not apply: a group's rank is the GROUP rank.</summary>
    private static SkillDef NpcBuffGroup(string id, string name, SkillEffect effect,
        string[] children, string desc) =>
        new(id, name, BaseClass.Mage, effect,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: NpcBuffTicks, ChildBuffs: children,
            Category: SkillCategory.Buff,
            Description: desc + " (buffer's blessing, 1 hour).");

    /// <summary>An NPC-buffer buff that hands out exactly ONE single buff (the scroll tier) for an
    /// hour. Mechanically the same shape as a Scroll — one child, the wrapper owns the duration —
    /// so it competes on the child's family key by Rank and can never stack with a potion or a
    /// cleric's rung of the same effect.</summary>
    private static SkillDef NpcSingle(string id, string name, string child,
        SkillEffect effect, string desc) =>
        new(id, name, BaseClass.Mage, effect,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: NpcBuffTicks, ChildBuffs: new[] { child },
            Category: SkillCategory.Buff,
            Description: desc + " (buffer's blessing, 1 hour).");

    private static SkillDef[] BufferSkills() => new SkillDef[]
    {
        // ---- The MIGHT family, one button each (was a single four-effect "Might"). ----
        NpcSingle(NpcMight, "Might", BuffPAtk3, SkillEffect.BuffPhysAtk, "+15% P.Atk"),
        NpcSingle(NpcBulwark, "Bulwark", BuffPDef3, SkillEffect.BuffDef, "+15% P.Def"),
        NpcSingle(NpcVampirism, "Vampirism", BuffVamp5, SkillEffect.BuffMeleeVamp, "9% melee vampirism"),
        NpcSingle(NpcAccuracy, "Accuracy", BuffAcc4, SkillEffect.BuffAccuracy, "+4 Accuracy"),

        // ---- The FORCE family (was a single three-effect "Force"). ----
        NpcSingle(NpcForce, "Force", BuffMAtk4, SkillEffect.BuffMagAtk, "+32% M.Atk"),
        NpcSingle(NpcWard, "Ward", BuffMDef4, SkillEffect.BuffMagicDef, "+30% M.Def"),
        NpcSingle(NpcResolve, "Resolve", BuffIntr7, SkillEffect.BuffInterruptResist,
            "+60 interrupt resistance"),

        // ---- The FOCUS family (was a single three-effect "Focus"). ----
        NpcSingle(NpcFocus, "Focus", Rung(FamCritRate, 6), SkillEffect.BuffCritRate, "+30% critical rate"),
        NpcSingle(NpcFerocity, "Ferocity", Rung(FamCritDmg, 6), SkillEffect.BuffCritDamage, "+35% critical damage"),
        NpcSingle(NpcInsight, "Insight", Rung(FamMagCrit, 6), SkillEffect.BuffMagicCritRate,
            "double magic critical rate"),

        // Speed used to be an IMPROVED (group) buff here. The owner cut it (2026-07-31): the NPC
        // buffer gives the SCROLL tier — four separate single buffs, bought and cancelled one at a
        // time — and the improved GROUP is what a buffer CLASS gives. The def below is kept (nothing
        // grants it) so the group shape stays documented in one place; NewbieBuffSet no longer lists
        // it. The buffer's edge is the DURATION (1 hour vs a potion's 20 minutes), which the
        // equal-rank "longer time wins" rule in ApplyBuff protects. See docs/design/BuffLadders.md.
        NpcBuffGroup(NpcSpeed, "Swift and Sure",
            SkillEffect.BuffAtkSpeed | SkillEffect.BuffMoveSpeed | SkillEffect.BuffCastSpeed | SkillEffect.BuffEvasion,
            new[] { BuffSwiftR, BuffAlacrityR, BuffAgilityR, BuffHasteR },
            "+33% attack speed, +30% cast speed, +33 move, +4 evasion"),

        // ---- The IMPROVED (group) versions at max rungs. Nothing in the world grants these — the
        //      admin buff button does, so the collapsed-group display and the group's own numbers
        //      can be checked without levelling a Warchanter to 74. ----
        NpcBuffGroup(NpcMightGroup, "Might and Bulwark",
            SkillEffect.BuffPhysAtk | SkillEffect.BuffDef | SkillEffect.BuffMeleeVamp | SkillEffect.BuffAccuracy,
            new[] { BuffPAtk3, BuffPDef3, BuffVamp5, BuffAcc4 },
            "+15% P.Atk & P.Def, 9% melee vampirism, +4 accuracy"),
        NpcBuffGroup(NpcForceGroup, "Force and Ward",
            SkillEffect.BuffMagAtk | SkillEffect.BuffMagicDef | SkillEffect.BuffInterruptResist,
            new[] { BuffMAtk4, BuffMDef4, BuffIntr7 },
            "+32% M.Atk, +30% M.Def, +60 interrupt resistance"),
        NpcBuffGroup(NpcFocusGroup, "Focus and Ferocity",
            SkillEffect.BuffCritRate | SkillEffect.BuffCritDamage | SkillEffect.BuffMagicCritRate,
            new[] { Rung(FamCritRate, 6), Rung(FamCritDmg, 6), Rung(FamMagCrit, 6) },
            "+30% critical rate, +35% critical damage, double magic critical rate"),
        NpcBuffGroup(NpcBodyGroup, "Body and Soul",
            SkillEffect.BuffHp | SkillEffect.BuffMp | SkillEffect.BuffHpRegen | SkillEffect.BuffMpRegen,
            new[] { Rung(FamMaxHp, 6), Rung(FamMaxMp, 6), Rung(FamHpRegen, 6), Rung(FamMpRegen, 6) },
            "+35% Max HP & MP, +20% HP & MP regeneration"),

        // ---- The four speed singles the buffer actually offers, one hour each. ----
        NpcSingle(NpcSwift, "Swift", BuffSwiftR, SkillEffect.BuffMoveSpeed, "+33 Move Speed"),
        NpcSingle(NpcAlacrity, "Alacrity", BuffAlacrityR, SkillEffect.BuffCastSpeed, "+30% Cast Speed"),
        NpcSingle(NpcAgility, "Agility", BuffAgilityR, SkillEffect.BuffEvasion, "+4 Evasion"),
        NpcSingle(NpcHaste, "Haste", BuffHasteR, SkillEffect.BuffAtkSpeed, "+33% Attack Speed"),

        // ---- The BODY family (was a single four-effect "Body"). ----
        NpcSingle(NpcBody, "Body", Rung(FamMaxHp, 6), SkillEffect.BuffHp, "+35% Max HP"),
        NpcSingle(NpcSoul, "Soul", Rung(FamMaxMp, 6), SkillEffect.BuffMp, "+35% Max MP"),
        NpcSingle(NpcVigor, "Vigor", Rung(FamHpRegen, 6), SkillEffect.BuffHpRegen, "+20% HP regeneration"),
        NpcSingle(NpcSerenity, "Serenity", Rung(FamMpRegen, 6), SkillEffect.BuffMpRegen, "+20% MP regeneration"),

        // Frenzy — a reckless trade-off buff, and the one family whose rung is a whole buff rather
        // than one stat. INCLUDED in the full set (it's a FULL buffer); a player who doesn't want
        // the -10% Max HP/MP can just cancel this one buff.
        NpcSingle(NpcFrenzy, "Frenzy", Rung(FamFrenzy, 6), SkillEffect.BuffPhysAtk,
            "-10% Max HP/MP but +8% P.Atk / +8% M.Atk / +8% atk & cast speed / +8 move / -8 evasion"),

        // ----- Greater "Harmony" buffs (max-level). Reflect (Protection) and the −physical/−magic
        // MP-consumption (Warrior/Wizard) are now WIRED. -----
        HarmonyBuff(NpcHarmonyProtection, "Harmony of Protection", "harmony_protection",
            SkillEffect.BuffDef | SkillEffect.BuffMagicDef | SkillEffect.BuffHp
            | SkillEffect.BuffHpRegen | SkillEffect.BuffEvasion | SkillEffect.BuffReflect,
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffDef, 0.25f), new(SkillEffect.BuffMagicDef, 0.25f),
                new(SkillEffect.BuffHp, 0.30f), new(SkillEffect.BuffHpRegen, 0.20f),
                new(SkillEffect.BuffEvasion, 3, ModifierMode.Flat), new(SkillEffect.BuffReflect, 0.20f),
            },
            "+25% P.Def & M.Def, +30% Max HP, +20% HP regen, +3 evasion, reflects 20% of melee damage"),

        HarmonyBuff(NpcHarmonyWarrior, "Harmony of the Warrior", "harmony_warrior",
            SkillEffect.BuffPhysAtk | SkillEffect.BuffAtkSpeed | SkillEffect.BuffCritDamage
            | SkillEffect.BuffCritRate | SkillEffect.BuffAccuracy | SkillEffect.BuffMeleeVamp,
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffPhysAtk, 0.12f), new(SkillEffect.BuffAtkSpeed, 0.15f),
                new(SkillEffect.BuffCritDamage, 0.35f), new(SkillEffect.BuffCritRate, 0.75f),
                new(SkillEffect.BuffAccuracy, 4, ModifierMode.Flat), new(SkillEffect.BuffMeleeVamp, 0.08f),
            },
            "+12% P.Atk, +15% atk speed, +35% crit damage, +75% crit rate, +4 acc, 8% vamp, −20% physical-skill MP cost",
            physMpCost: 0.20f),

        HarmonyBuff(NpcHarmonyWizard, "Harmony of the Wizard", "harmony_wizard",
            SkillEffect.BuffCastSpeed | SkillEffect.BuffMagAtk | SkillEffect.BuffMpRegen,
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffCastSpeed, 0.30f), new(SkillEffect.BuffMagAtk, 0.10f),
                new(SkillEffect.BuffMpRegen, 0.20f),
            },
            "+30% cast speed, +10% M.Atk, +20% MP regen, −30% magic-skill MP cost",
            magicMpCost: 0.30f),
    };
}
