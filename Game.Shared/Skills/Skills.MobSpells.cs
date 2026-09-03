namespace Game.Shared;

/// <summary>
/// Mob-only MAGIC skills for caster mobs (shamans / wizards / magic creatures). Two spells:
/// a short-range "melee" jab (150 range, 1.5s cast, 0.5s reuse) and a long-range nuke
/// (600 range, 4s cast, 1s reuse). Power + MP cost scale with the MOB's level via a 13-step
/// table whose steps are tied to mob-level anchors (10..85); a caster mob learns BOTH at the
/// level its own level maps to (<see cref="MobSpellLevel"/>). Damage = spell Power × the mob's
/// M.Atk through the normal magic formula. Caster mobs have NO basic attack — only these.
/// </summary>
public static partial class SkillCatalog
{
    public const string MobNukeSkill = "mob_nuke";   // 600 range · 4.0s cast · 1.0s reuse
    public const string MobBoltSkill = "mob_bolt";   // 150 range · 1.5s cast · 0.5s reuse
    public const string BossSlamSkill = "boss_slam"; // boss AoE: telegraphed slam (dmg + stun) around it
    public const string BossThornNovaSkill = "boss_thorn_nova"; // boss AoE: magic burst + slow (phase skill)
    public const string BossFullSilenceSkill = "boss_full_silence"; // `BL-155` boss AoE: 15s full silence

    // The 13 spell levels are tied to these mob levels (interpolation anchors from the CSV
    // ask: nuke power 18→129 / MP 7→40, bolt power 7→33 / MP 5→10 across mob levels 10..85).
    private static readonly int[] MobSpellAnchors =
        { 10, 16, 22, 28, 34, 40, 46, 52, 58, 64, 70, 76, 85 };

    /// <summary>Which spell LEVEL (1..13) a caster mob of the given level casts at
    /// (the highest anchor ≤ its level; floored at 1).</summary>
    public static int MobSpellLevel(int mobLevel)
    {
        int lvl = 1;
        for (int i = 0; i < MobSpellAnchors.Length; i++)
            if (mobLevel >= MobSpellAnchors[i]) lvl = i + 1;
        return lvl;
    }

    private static SkillDef[] MobSpellSkills()
    {
        // Long nuke (600): power 18..129, MP 7..40 (integers, interpolated across the anchors).
        int[] nukePow = { 18, 27, 36, 45, 54, 62, 71, 80, 89, 98, 107, 116, 129 };
        int[] nukeMp  = { 7, 10, 12, 15, 18, 20, 23, 25, 28, 31, 33, 36, 40 };
        // Short jab (150): power 7..33, MP 5..10.
        int[] boltPow = { 7, 9, 11, 13, 15, 17, 19, 22, 24, 26, 28, 30, 33 };
        int[] boltMp  = { 5, 5, 6, 6, 7, 7, 7, 8, 8, 9, 9, 9, 10 };

        static SkillLevel[] Rows(int[] pow, int[] mp)
        {
            var arr = new SkillLevel[pow.Length];
            for (int i = 0; i < pow.Length; i++)
                arr[i] = new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: 0);
            return arr;
        }

        return new[]
        {
            new SkillDef(MobNukeSkill, "Arcane Blast", BaseClass.Mage, SkillEffect.MagicDamage,
                MpCost: nukeMp[0], CastTicks: 40, CooldownTicks: 10, Range: 600, Power: nukePow[0],
                Category: SkillCategory.Magic, SpCost: 0,
                Description: "A caster mob's long-range nuke.",
                Levels: Rows(nukePow, nukeMp)),

            new SkillDef(MobBoltSkill, "Arcane Jab", BaseClass.Mage, SkillEffect.MagicDamage,
                MpCost: boltMp[0], CastTicks: 15, CooldownTicks: 5, Range: 150, Power: boltPow[0],
                Category: SkillCategory.Magic, SpCost: 0,
                Description: "A caster mob's short-range magic jab.",
                Levels: Rows(boltPow, boltMp)),

            // Boss SLAM — a telegraphed (3s cast) AoE around the boss: physical damage + a
            // contested Stun to everyone in ~250. The long cast is the "boss skill" tell —
            // players can move out / interrupt. Damage rides the boss's (high) P.Atk; the flat
            // Power is a modest add. MP-free (bosses aren't MP-gated).
            new SkillDef(BossSlamSkill, "Devastating Slam", BaseClass.Fighter,
                SkillEffect.PhysicalDamage | SkillEffect.Stun,
                MpCost: 0, CastTicks: 30, CooldownTicks: 120, Range: 0, Power: 60,
                Category: SkillCategory.Physical, SpCost: 0,
                TargetMode: TargetMode.EnemiesInRadius, AreaRadius: 250f,
                DebuffSchool: DebuffSchool.Physical, DurationTicks: 20,
                Description: "A boss's ground slam — heavy damage and a brief stun to all nearby foes."),

            // Boss THORN NOVA — a wider (300) MAGIC burst + a contested Slow, on a longer cast
            // (2.5s) and reuse. Authored as a PHASE skill (BossCatalog gates it to sub-50% HP), so
            // a boss picks up a second, distinct attack once wounded. Rides the boss's M.Atk.
            new SkillDef(BossThornNovaSkill, "Thorn Nova", BaseClass.Mage,
                SkillEffect.MagicDamage | SkillEffect.Slow,
                MpCost: 0, CastTicks: 25, CooldownTicks: 200, Range: 0, Power: 90,
                Category: SkillCategory.Magic, SpCost: 0,
                TargetMode: TargetMode.EnemiesInRadius, AreaRadius: 300f,
                DebuffSchool: DebuffSchool.Magical, DurationTicks: 60,
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.Slow, 0.40f) },
                Description: "A boss's storm of thorns — magic damage and a slow to all nearby foes."),

            // ═══ BOSS FULL SILENCE (`BL-155`, his own ask, 2026-09-03) ═══════════════════════════
            // *"U can add dungeon bosses a full silence aoe skill for 15s duration and 45s cd (mp
            //  cost u deside)"*. His two numbers are exact: 150 ticks and 450 ticks.
            //
            // 🔑 BOTH FIELDS ON ONE SKILL — this is the *"full silence can be a Boss skill"* half of
            // his ruling. The tank pair reaches the same state by landing two debuffs; a boss says it
            // in one word. Nothing new is needed for it: the cast gate asks the two questions
            // separately and a skill that sets both answers yes to both.
            //
            // ⚠ MP 0, which is my call and the one every other boss skill already makes. A boss's
            // rotation must not stall on mana — BossTick picks the first READY entry, and a skill the
            // creature cannot afford would silently drop out of the fight rather than fail loudly.
            //
            // ⚠ RADIUS 500, wider than the slam's 250 and the nova's 300, and deliberately: a silence
            // that only reaches the melee ring silences the tank and leaves the healer — who is the
            // whole point of it — standing outside the circle casting.
            //
            // ⚠ SPT-DEFENDED. It is a spoken thing, and it means the stat that already answers holds
            // and fears answers this too; with `BL-156` a mage's SPT also SHORTENS it (41 → x0.84, so
            // 15s becomes ~12.6s), which is the *"investing have benefits"* rule landing exactly where
            // it should. 🔵 WATCH IT IN PLAY: 15s on a 45s reuse is 33% uptime with no heals, which is
            // brutal by design and the number to move first if a boss becomes unkillable.
            new SkillDef(BossFullSilenceSkill, "Word of Unmaking", BaseClass.Mage, SkillEffect.None,
                MpCost: 0, CastTicks: 30, CooldownTicks: 450, Range: 0, Power: 0,
                Category: SkillCategory.Debuff, SpCost: 0,
                TargetMode: TargetMode.EnemiesInRadius, AreaRadius: 500f,
                DebuffSchool: DebuffSchool.Magical, DurationTicks: 150,
                SilencePhysical: true, SilenceMagical: true,
                BuffKey: "boss_full_silence", Rank: 1,
                Description: "A boss's word of unmaking — every skill fails for all nearby foes."),
        };
    }
}
