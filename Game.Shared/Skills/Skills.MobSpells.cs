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
        };
    }
}
