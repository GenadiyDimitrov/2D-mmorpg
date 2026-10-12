namespace Game.Shared;

/// <summary>`BL-344` step 6 — how a skill TRAVELS on screen, from the caster to whoever it lands on.
///
/// <para>🔑 Only the DELIVERY is a per-skill choice. What plays where it lands is read off the outcome the
/// server already reports (<see cref="CombatEvent.Outcome"/>): a hit sparks, a crit sparks bigger, a heal glows
/// green, MP glows blue, a buff rings gold, a debuff drifts purple, a raised corpse gets a pillar of light. Those
/// never need authoring, and splitting it this way means one short enum covers every skill in the game.</para>
///
/// <para>⚠ Append-only: the values are matched by NAME in `docs/data/skill_fx.csv`, never by number, but the
/// client and the server must still agree on the numbers when the field travels.</para></summary>
public enum SkillFx
{
    /// <summary>Not authored — <see cref="SkillFxRules.Delivery"/> decides from the skill's shape.</summary>
    Auto = 0,
    /// <summary>Draw nothing at all, not even the landing.</summary>
    None = 1,
    /// <summary>No travel: the landing plays on the target at once (self buffs, area skills, touch spells).</summary>
    Instant = 2,
    /// <summary>A short arc of sparks in front of the attacker, aimed at the target. Melee strikes.</summary>
    Slash = 3,
    /// <summary>A glowing orb flies to the target; the landing plays when it arrives. Ranged spells.</summary>
    Projectile = 4,
    /// <summary>A thin fast streak, the bow's projectile.</summary>
    Arrow = 5,
    /// <summary>A line of light from caster to target, there and gone. Heals and buffs on someone else.</summary>
    Beam = 6,
}

/// <summary>The automatic pick for <see cref="SkillFx.Auto"/>, read off what the skill already declares.</summary>
public static class SkillFxRules
{
    /// <summary>Range at or under this (server units) counts as melee. Melee strikes are authored at 40 and a blank
    /// range means 40; the shortest ranged spells (Quick Bolt, the mob bolt) sit at 150.</summary>
    public const float MeleeReach = 100f;

    /// <summary>The delivery this skill draws with: its authored <see cref="SkillDef.Fx"/>, or the shape rule.</summary>
    public static SkillFx Delivery(SkillDef def)
    {
        if (def.Fx != SkillFx.Auto) return def.Fx;
        return AutoDelivery(def);
    }

    /// <summary>The shape rule alone, ignoring any authored exception — `SkillCsvSeed --check` uses it to flag an
    /// exception row that only restates what the rule would pick anyway.</summary>
    public static SkillFx AutoDelivery(SkillDef def)
    {
        if (def.Category == SkillCategory.Passive) return SkillFx.None;
        // Caster-centred areas already flash their circle on the ground; every foe inside gets its own landing.
        if (def.AreaRadius > 0f && !def.AreaAtTarget) return SkillFx.Instant;
        if (def.TargetMode == TargetMode.SelfOnly) return SkillFx.Instant;

        bool ranged = def.Range > MeleeReach || def.RequiredWeapon == WeaponType.Bow;
        bool damage = (def.Effect & (SkillEffect.PhysicalDamage | SkillEffect.MagicDamage)) != 0;
        if (damage)
        {
            if (def.RequiredWeapon == WeaponType.Bow) return SkillFx.Arrow;
            if (!ranged) return def.Category == SkillCategory.Physical ? SkillFx.Slash : SkillFx.Instant;
            return SkillFx.Projectile;
        }
        if (!ranged) return SkillFx.Instant;
        return def.Category is SkillCategory.Heal or SkillCategory.Buff ? SkillFx.Beam : SkillFx.Projectile;
    }

    /// <summary>Does this skill's "applied" event (<see cref="CombatOutcome.Buff"/>) mean something BAD landed? The
    /// landing then drifts purple instead of ringing gold.</summary>
    public static bool IsHostile(SkillDef def) =>
        def.Category == SkillCategory.Debuff || def.BuffRow == BuffRow.Debuff;

    /// <summary>The authored exceptions, applied once while the catalog is built.
    /// <para>⚠ An unknown id is SKIPPED, not thrown — unlike a bad child buff, which breaks a mechanic. This is a
    /// look: a deleted skill must not stop the server booting, nor the generator that would fix the table (it
    /// reads this catalog). `SkillCsvSeed --check` is what catches the dead row.</para></summary>
    internal static void ApplyOverrides(Dictionary<string, SkillDef> catalog)
    {
        foreach (var kv in SkillFxTable.Overrides)
            if (catalog.TryGetValue(kv.Key, out var def))
                catalog[kv.Key] = def with { Fx = kv.Value };
    }
}
