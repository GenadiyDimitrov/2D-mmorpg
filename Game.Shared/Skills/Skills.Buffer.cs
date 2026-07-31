namespace Game.Shared;

/// <summary>NPC "newbie buffer" buffs — strong, long (1 hour) versions that represent a
/// 3rd-class buffer's MAX-level buffs (a 4th class will give more; the buffer class also
/// gets a Prophecy). They share the SAME BuffKey as the player self-buffs so they OVERRIDE
/// the weaker self-cast versions (high Rank). Not learnable — applied by an NpcRole.Buffer
/// NPC. Channel-split atk (Might = P.Atk, Force = M.Atk) via the buff/effect layer.</summary>
public static partial class SkillCatalog
{
    public const string NpcMight  = "npc_might";
    public const string NpcForce  = "npc_force";
    public const string NpcFocus  = "npc_focus";
    public const string NpcSpeed  = "npc_speed";
    public const string NpcBody   = "npc_body";
    public const string NpcFrenzy = "npc_frenzy";
    // The four SPEED singles, one hour each — the scroll tier handed out one buff at a time.
    // They replaced the "Improved Speed" GROUP the buffer used to give (owner 2026-07-31).
    public const string NpcSwift    = "npc_swift";
    public const string NpcAlacrity = "npc_alacrity";
    public const string NpcAgility  = "npc_agility";
    public const string NpcHaste    = "npc_haste";
    // "Harmony" GREATER buffs (max-level; owner 2026-07-03). They STACK on top of the six above
    // (distinct BuffKeys). NO LONGER OFFERED by the newbie buffer — see NewbieBuffSet. The defs
    // stay because a real 3rd-class buffer is meant to have them; nothing grants them today.
    public const string NpcHarmonyProtection = "npc_harmony_protection";
    public const string NpcHarmonyWarrior    = "npc_harmony_warrior";
    public const string NpcHarmonyWizard     = "npc_harmony_wizard";

    public const int NpcBuffTicks = 36000;   // 1 hour @ 10 ticks/s
    public const int NpcBuffRank  = 100;     // overrides player self-buffs (rank 1-4)

    /// <summary>The buffs the newbie buffer NPC grants: the BASIC tier only, one hour each — the
    /// scroll tier (owner 2026-07-31: "not the improved and harmonies … just the scroll buffs,
    /// 1h of single basic buff"). So no GROUP buff and no Harmony: the buffer's edge over a potion
    /// is the DURATION, and its ceiling stays below a real buffer class, which keeps the improved
    /// groups. Frenzy stays in (it's a FULL buffer; cancel that one buff if you don't want its
    /// −10% Max HP/MP).</summary>
    public static readonly string[] NewbieBuffSet =
        { NpcMight, NpcForce, NpcFocus, NpcBody, NpcFrenzy,
          NpcSwift, NpcAlacrity, NpcAgility, NpcHaste };

    private static SkillDef NpcBuff(string id, string name, string buffKey,
        SkillEffect effect, EffectMagnitude[] mags, string desc,
        float physMpCost = 0f, float magicMpCost = 0f) =>
        new(id, name, BaseClass.Mage, effect,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: NpcBuffTicks, BuffKey: buffKey, Rank: NpcBuffRank,
            Category: SkillCategory.Buff, Magnitudes: mags,
            PhysMpCostPct: physMpCost, MagicMpCostPct: magicMpCost,
            Description: desc + " (buffer's blessing, 1 hour).");

    /// <summary>An NPC-buffer buff that is a GROUP: it applies the named single buffs (children)
    /// rather than one monolithic buff of its own, so each part competes on its own family ladder
    /// with whatever the player already drank, read or was blessed with. Rank lives on the CHILDREN
    /// (NpcBuffRank is meaningless for a group — the buffer's advantage is its 1-hour duration).</summary>
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
        NpcBuff(NpcMight, "Might", "mage_might",
            SkillEffect.BuffPhysAtk | SkillEffect.BuffDef | SkillEffect.BuffMeleeVamp | SkillEffect.BuffAccuracy,
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffPhysAtk, 0.15f), new(SkillEffect.BuffDef, 0.15f),
                new(SkillEffect.BuffMeleeVamp, 0.09f), new(SkillEffect.BuffAccuracy, 4, ModifierMode.Flat),
            },
            "+15% P.Atk & P.Def, 9% melee vampirism, +4 accuracy"),

        NpcBuff(NpcForce, "Force", "holy_force",
            SkillEffect.BuffInterruptResist | SkillEffect.BuffMagAtk | SkillEffect.BuffMagicDef,
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffInterruptResist, 60, ModifierMode.Flat),
                new(SkillEffect.BuffMagAtk, 0.32f), new(SkillEffect.BuffMagicDef, 0.30f),
            },
            "+32% M.Atk, +30% M.Def, strong cast-cancel resist"),

        NpcBuff(NpcFocus, "Focus", "holy_focus",
            SkillEffect.BuffCritRate | SkillEffect.BuffCritDamage | SkillEffect.BuffMagicCritRate,
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffCritRate, 0.30f), new(SkillEffect.BuffCritDamage, 0.35f),
                new(SkillEffect.BuffMagicCritRate, 1.0f),
            },
            "+30% physical crit rate, +35% crit damage, double magic crit rate"),

        // Speed used to be an IMPROVED (group) buff here. The owner cut it (2026-07-31): the NPC
        // buffer gives the SCROLL tier — four separate single buffs, bought and cancelled one at a
        // time — and the improved GROUP is what a buffer CLASS gives. The def below is kept (nothing
        // grants it) so the group shape stays documented in one place; NewbieBuffSet no longer lists
        // it. The buffer's edge is the DURATION (1 hour vs a potion's 20 minutes), which the
        // equal-rank "longer time wins" rule in ApplyBuff protects. See docs/design/BuffLadders.md.
        NpcBuffGroup(NpcSpeed, "Improved Speed",
            SkillEffect.BuffAtkSpeed | SkillEffect.BuffMoveSpeed | SkillEffect.BuffCastSpeed | SkillEffect.BuffEvasion,
            new[] { BuffSwiftR, BuffAlacrityR, BuffAgilityR, BuffHasteR },
            "+33% attack speed, +30% cast speed, +33 move, +4 evasion"),

        // ---- The four speed singles the buffer actually offers, one hour each. ----
        NpcSingle(NpcSwift, "Swift", BuffSwiftR, SkillEffect.BuffMoveSpeed, "+33 Move Speed"),
        NpcSingle(NpcAlacrity, "Alacrity", BuffAlacrityR, SkillEffect.BuffCastSpeed, "+30% Cast Speed"),
        NpcSingle(NpcAgility, "Agility", BuffAgilityR, SkillEffect.BuffEvasion, "+4 Evasion"),
        NpcSingle(NpcHaste, "Haste", BuffHasteR, SkillEffect.BuffAtkSpeed, "+33% Attack Speed"),

        NpcBuff(NpcBody, "Body", "holy_body",
            SkillEffect.BuffHp | SkillEffect.BuffMp | SkillEffect.BuffHpRegen | SkillEffect.BuffMpRegen,
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffHp, 0.35f), new(SkillEffect.BuffMp, 0.35f),
                new(SkillEffect.BuffHpRegen, 0.20f), new(SkillEffect.BuffMpRegen, 0.20f),
            },
            "+35% Max HP & MP, +20% HP & MP regen"),

        // Frenzy — a reckless trade-off buff. INCLUDED in the full NPC buffer set (it's a FULL
        // buffer); a player who doesn't want the -10% Max HP/MP can just cancel this one buff.
        NpcBuff(NpcFrenzy, "Frenzy", "holy_frenzy",
            SkillEffect.BuffHp | SkillEffect.BuffMp | SkillEffect.BuffAtkSpeed | SkillEffect.BuffCastSpeed
            | SkillEffect.BuffPhysAtk | SkillEffect.BuffMagAtk | SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion,
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffHp, -0.10f), new(SkillEffect.BuffMp, -0.10f),
                new(SkillEffect.BuffAtkSpeed, 0.08f), new(SkillEffect.BuffCastSpeed, 0.08f),
                new(SkillEffect.BuffPhysAtk, 0.08f), new(SkillEffect.BuffMagAtk, 0.08f),
                new(SkillEffect.BuffMoveSpeed, 8, ModifierMode.Flat), new(SkillEffect.BuffEvasion, -8, ModifierMode.Flat),
            },
            "-10% Max HP/MP but +8% P.Atk / +8% M.Atk / +8% atk & cast speed / +8 move / -8 evasion"),

        // ----- Greater "Harmony" buffs (max-level). Reflect (Protection) and the −physical/−magic
        // MP-consumption (Warrior/Wizard) are now WIRED. -----
        NpcBuff(NpcHarmonyProtection, "Harmony of Protection", "harmony_protection",
            SkillEffect.BuffDef | SkillEffect.BuffMagicDef | SkillEffect.BuffHp
            | SkillEffect.BuffHpRegen | SkillEffect.BuffEvasion | SkillEffect.BuffReflect,
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffDef, 0.25f), new(SkillEffect.BuffMagicDef, 0.25f),
                new(SkillEffect.BuffHp, 0.30f), new(SkillEffect.BuffHpRegen, 0.20f),
                new(SkillEffect.BuffEvasion, 3, ModifierMode.Flat), new(SkillEffect.BuffReflect, 0.20f),
            },
            "+25% P.Def & M.Def, +30% Max HP, +20% HP regen, +3 evasion, reflects 20% of melee damage"),

        NpcBuff(NpcHarmonyWarrior, "Harmony of the Warrior", "harmony_warrior",
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

        NpcBuff(NpcHarmonyWizard, "Harmony of the Wizard", "harmony_wizard",
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
