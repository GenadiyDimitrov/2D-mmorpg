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
    // "Harmony" GREATER buffs (max-level; owner 2026-07-03). They STACK on top of the six above
    // (distinct BuffKeys). Later the NPC buffer will hand out ONE level below each max so a real
    // buffer stays valuable, and 76+/ultimate buffs come after.
    public const string NpcHarmonyProtection = "npc_harmony_protection";
    public const string NpcHarmonyWarrior    = "npc_harmony_warrior";
    public const string NpcHarmonyWizard     = "npc_harmony_wizard";

    public const int NpcBuffTicks = 36000;   // 1 hour @ 10 ticks/s
    public const int NpcBuffRank  = 100;     // overrides player self-buffs (rank 1-4)

    /// <summary>The buffs the newbie buffer NPC grants — the full max-level set (the six basics +
    /// the three greater Harmony buffs). Frenzy stays in (it's a FULL buffer; cancel that one buff
    /// if you don't want its −10% Max HP/MP).</summary>
    public static readonly string[] NewbieBuffSet =
        { NpcMight, NpcForce, NpcFocus, NpcSpeed, NpcBody, NpcFrenzy,
          NpcHarmonyProtection, NpcHarmonyWarrior, NpcHarmonyWizard };

    private static SkillDef NpcBuff(string id, string name, string buffKey,
        SkillEffect effect, EffectMagnitude[] mags, string desc) =>
        new(id, name, BaseClass.Mage, effect,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: NpcBuffTicks, BuffKey: buffKey, Rank: NpcBuffRank,
            Category: SkillCategory.Buff, Magnitudes: mags,
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
                new(SkillEffect.BuffMagAtk, 0.75f), new(SkillEffect.BuffMagicDef, 0.30f),
            },
            "+75% M.Atk, +30% M.Def, strong cast-cancel resist"),

        NpcBuff(NpcFocus, "Focus", "holy_focus",
            SkillEffect.BuffCritRate | SkillEffect.BuffCritDamage | SkillEffect.BuffMagicCritRate,
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffCritRate, 0.30f), new(SkillEffect.BuffCritDamage, 0.35f),
                new(SkillEffect.BuffMagicCritRate, 1.0f),
            },
            "+30% physical crit rate, +35% crit damage, double magic crit rate"),

        NpcBuff(NpcSpeed, "Speed", "holy_speed",
            SkillEffect.BuffAtkSpeed | SkillEffect.BuffMoveSpeed | SkillEffect.BuffCastSpeed | SkillEffect.BuffEvasion,
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffAtkSpeed, 0.33f), new(SkillEffect.BuffMoveSpeed, 33, ModifierMode.Flat),
                new(SkillEffect.BuffCastSpeed, 0.30f), new(SkillEffect.BuffEvasion, 4, ModifierMode.Flat),
            },
            "+33% attack speed, +30% cast speed, +33 move, +4 evasion"),

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
                new(SkillEffect.BuffPhysAtk, 0.08f), new(SkillEffect.BuffMagAtk, 0.16f),
                new(SkillEffect.BuffMoveSpeed, 8, ModifierMode.Flat), new(SkillEffect.BuffEvasion, -8, ModifierMode.Flat),
            },
            "-10% Max HP/MP but +8% P.Atk / +16% M.Atk / +8% atk & cast speed / +8 move / -8 evasion"),

        // ----- Greater "Harmony" buffs (max-level). Reflect (Protection) is now WIRED; the
        // "-physical/magic MP consumption" (Warrior/Wizard) is still OMITTED (skill-MP-cost-reduction
        // mechanic unbuilt) — add those lines when that system lands. -----
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
            "+12% P.Atk, +15% atk speed, +35% crit damage, +75% crit rate, +4 acc, 8% vamp (−MP cost pending)"),

        NpcBuff(NpcHarmonyWizard, "Harmony of the Wizard", "harmony_wizard",
            SkillEffect.BuffCastSpeed | SkillEffect.BuffMagAtk | SkillEffect.BuffMpRegen,
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffCastSpeed, 0.30f), new(SkillEffect.BuffMagAtk, 0.20f),
                new(SkillEffect.BuffMpRegen, 0.20f),
            },
            "+30% cast speed, +20% M.Atk, +20% MP regen (−MP cost pending)"),
    };
}
