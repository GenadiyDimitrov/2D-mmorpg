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
    /// <summary>What the ADMIN buff button hands out: EVERYTHING that exists — his nine lane groups,
    /// the two "War" party echoes, all four Harmonies and every single. Those top layers are the ones
    /// no NPC sells and no consumable can reach, so this is the only way to see a fully-buffed
    /// character, which is the state the balance numbers are read at.
    ///
    /// ⚠ The GROUPS come first, and they simply WIN: a group covers its families at group rank, so
    /// every single it contains is refused when it arrives after. The bar ends up as the group squares
    /// + the Harmonies + War Frenzy — which is the point of the improved tier. The order is kept
    /// deliberate anyway: reversed, the singles would land first and then be evicted, which is the
    /// same picture through twice the work.
    ///
    /// ⚠ War Might is listed and War Bulwark is NOT — they share one buff key on purpose (an ally
    /// wears one or the other, never both), so granting both would only show whichever landed last.</summary>
    /// same picture through twice the work.</summary>
    public static readonly string[] AdminBuffSet =
        new[] { WcFeralPrecision, WcFeralBloodlust, WcArcaneInsight, WcArcaneSerenity,
                WcSoulReinforce, WcBodyReinforce, WcShieldReinforce, WcArcaneFeralProt, WcWindGrace,
                WarFrenzy, WcWarMight,
                NpcHarmonyProtection, WcHarmonySpeed, NpcHarmonyWarrior, NpcHarmonyWizard }
            .Concat(NewbieBuffSet).ToArray();

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
            "+54 interrupt resistance"),

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
            "+32% M.Atk, +30% M.Def, +54 interrupt resistance"),
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
        NpcSingle(NpcHaste, "Fury", BuffHasteR, SkillEffect.BuffAtkSpeed, "+33% Attack Speed"),

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

        // ----- The three original "Harmony" blessings MOVED OUT on 2026-08-21 -----
        // They are now LADDERS (Protection 5 rungs, Warrior 6, Wizard 2) on his `buffer 3rd.csv`
        // rows, with a 5-minute duration and a 2-minute reuse, and they live beside the fourth one
        // (Harmony of Speed) in Skills.Warchanter3rd.cs. Their IDS DID NOT CHANGE — `npc_harmony_*`
        // is append-only and AdminBuffSet still names them.
    };
}
