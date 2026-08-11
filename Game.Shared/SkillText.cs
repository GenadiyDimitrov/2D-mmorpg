namespace Game.Shared;

// ===========================================================================
//  SkillText — turning a skill's DATA into the numbers a player reads.
//
//  Why this is shared and not written twice: a passive skill's authored
//  Description is prose ("hardens your hide"), and prose is what the owner kept
//  reporting as wrong — the window said what the skill was ABOUT and never what
//  it DID. The numbers already exist on the def; nothing was formatting them.
//
//  Both clients ask this one place, so the WPF harness and the Unity client can
//  never disagree about what a passive is worth, and a new PassiveEffect field
//  becomes visible in both by being added HERE once.
// ===========================================================================

/// <summary>Human-readable, NUMERIC descriptions of what a skill's effects do.
/// Every helper returns one short "Label +12%" line per non-zero field, in a fixed
/// order (vitals, defence, offence, accuracy, crit, regen, speed, then the rest),
/// so the same passive always reads the same way.</summary>
public static class SkillText
{
    // ----- formatting ---------------------------------------------------------------------

    private static string Sign(float v) => v >= 0f ? "+" : "";

    /// <summary>A FRACTION as a percent: 0.07 → "+7%".</summary>
    private static void Pct(List<string> into, string label, float v)
    {
        if (v > -0.00005f && v < 0.00005f) return;
        into.Add(label + " " + Sign(v) + (v * 100f).ToString("0.##") + "%");
    }

    /// <summary>A flat addition: 12 → "+12".</summary>
    private static void Flat(List<string> into, string label, float v)
    {
        if (v > -0.0005f && v < 0.0005f) return;
        into.Add(label + " " + Sign(v) + v.ToString("0.##"));
    }

    // ----- PassiveEffect ------------------------------------------------------------------

    /// <summary>Every non-zero field of an always-on passive bonus, as display lines.</summary>
    public static List<string> Passive(PassiveEffect p)
    {
        var o = new List<string>();
        Passive(p, o);
        return o;
    }

    public static void Passive(PassiveEffect p, List<string> o)
    {
        // Vitals
        Flat(o, "Max HP", p.MaxHp);            Pct(o, "Max HP", p.MaxHpPct);
        Flat(o, "Max MP", p.MaxMp);            Pct(o, "Max MP", p.MaxMpPct);

        // Defence
        Flat(o, "P.Def", p.Defence);           Pct(o, "P.Def", p.DefencePct);
        Flat(o, "M.Def", p.MagicDefence);      Pct(o, "M.Def", p.MagicDefencePct);

        // Offence. Attack/AttackPct feed BOTH channels (see CLAUDE.md: one power stat);
        // the PhysAtk/MagAtk pair is the channel-specific one, so they are named apart.
        Flat(o, "Attack", p.Attack);           Pct(o, "Attack", p.AttackPct);
        Flat(o, "P.Atk", p.PhysAtk);           Pct(o, "P.Atk", p.PhysAtkPct);
        Flat(o, "M.Atk", p.MagAtk);            Pct(o, "M.Atk", p.MagAtkPct);

        Flat(o, "Accuracy", p.Accuracy);
        Flat(o, "Evasion", p.Evasion);

        Pct(o, "Crit rate", p.CritRate);
        Pct(o, "Crit damage", p.CritDamage);
        Flat(o, "Crit damage", p.CritDamageFlat);
        Pct(o, "Magic crit", p.MagicCritRate);

        // Regen: the flat fields are PER TICK on the server (10/sec), which is meaningless
        // to read — state them per second, the unit the player experiences.
        Flat(o, "HP regen/s", p.HpRegen * GameConstants.TickRate);
        Flat(o, "MP regen/s", p.MpRegen * GameConstants.TickRate);
        Pct(o, "HP regen", p.HpRegenPct);
        Pct(o, "MP regen", p.MpRegenPct);

        // Speeds
        Pct(o, "Atk speed", p.AtkSpeedPct);
        Pct(o, "Cast speed", p.CastSpeedPct);
        Flat(o, "Cast speed", p.CastSpeedFlat);
        Pct(o, "Move speed", p.MoveSpeedPct);
        Pct(o, "Reuse delay", -p.CooldownPct);   // authored as a REDUCTION; show it as one

        // Primary stats (the level-40 swap passives)
        Flat(o, "ATK", p.Atk);
        Flat(o, "CON", p.Con);
        Flat(o, "AGI", p.Agi);
        Flat(o, "WIT", p.Wit);
        Flat(o, "SPT", p.Spt);

        // Shield / bow conditionals
        Pct(o, "Block chance", p.BlockChancePct);
        Pct(o, "Shield def", p.ShieldDefPct);
        Flat(o, "Bow range", p.BowRange);

        // Resists + interrupt
        Flat(o, "Interrupt power", p.InterruptPower);
        Flat(o, "Interrupt resist", p.InterruptResist);
        Pct(o, "Crit rate resist", p.CritRateResist);
        Pct(o, "Crit dmg resist", p.CritDmgResist);
        Pct(o, "Bow resist", p.BowResist);
        Pct(o, "Magic resist", p.MagicResist);
        Pct(o, "Cancel resist", p.CancelResistPct);

        // Lifesteal
        Pct(o, "Melee vamp", p.MeleeVamp);
        Pct(o, "Spell vamp", p.SpellVamp);

        // Damage-out matrix
        Pct(o, "PvE skill dmg", p.PveSkillDamagePct);
        Pct(o, "PvE magic dmg", p.PveMagicDamagePct);
        Pct(o, "PvE basic dmg", p.PveBasicDamagePct);
        Pct(o, "PvP skill dmg", p.PvpSkillDamagePct);
        Pct(o, "PvP magic dmg", p.PvpMagicDamagePct);
        Pct(o, "PvP basic dmg", p.PvpBasicDamagePct);

        // Guaranteed floors — worded as the guarantee they are, not as a bonus.
        Pct(o, "Min evade chance", p.EvadeFloor);
        Pct(o, "Min hit chance", p.HitFloor);
        // Magic has no floor — a MULTIPLIER on the enemy's fail chance instead.
        if (p.MagicFailMod > 1f) o.Add($"Enemy spells fizzle ×{p.MagicFailMod:0.##}");

        // Healing
        Flat(o, "Heal power", p.HealPowerFlat);       Pct(o, "Heal power", p.HealPowerPct);
        Flat(o, "Heal received", p.HealReceivedFlat); Pct(o, "Heal received", p.HealReceivedPct);
    }

    // ----- StatMods (armor mastery profiles, gear/set bonuses) ------------------------------

    /// <summary>Every non-zero field of a <see cref="StatMods"/> bundle, as display lines.</summary>
    public static List<string> Mods(StatMods m)
    {
        var o = new List<string>();
        Flat(o, "Max HP", m.MaxHp);            Pct(o, "Max HP", m.MaxHpPct);
        Flat(o, "Max MP", m.MaxMp);            Pct(o, "Max MP", m.MaxMpPct);
        Flat(o, "P.Def", m.PDef);              Pct(o, "P.Def", m.PDefPct);
        Flat(o, "M.Def", m.MDef);              Pct(o, "M.Def", m.MDefPct);
        Flat(o, "P.Atk", m.PAtk);              Pct(o, "P.Atk", m.PAtkPct);
        Flat(o, "M.Atk", m.MAtk);              Pct(o, "M.Atk", m.MAtkPct);
        Flat(o, "Accuracy", m.Accuracy);
        Flat(o, "Evasion", m.Evasion);         Pct(o, "Evasion", m.EvasionPct);
        // Crit rate is a MULTIPLIER now (0.05 = ×1.05), so it reads as a percent, not a flat add.
        Pct(o, "Crit rate", m.CritRate);       Pct(o, "Crit rate", m.CritRatePct);
        Flat(o, "Crit damage", m.CritDamage);  Pct(o, "Crit damage", m.CritDamagePct);
        Pct(o, "Magic crit", m.MagicCritRate);   // a MULTIPLIER like CritRate above, not a flat add
        Pct(o, "Atk speed", m.AtkSpeedPct);
        Pct(o, "Cast speed", m.CastSpeedPct);
        Flat(o, "Move speed", m.MoveSpeed);    Pct(o, "Move speed", m.MoveSpeedPct);
        Flat(o, "HP regen/s", m.HpRegen * GameConstants.TickRate);
        Pct(o, "HP regen", m.HpRegenPct);
        Flat(o, "MP regen/s", m.MpRegen * GameConstants.TickRate);
        Pct(o, "MP regen", m.MpRegenPct);
        Flat(o, "Interrupt resist", m.InterruptResist);
        Pct(o, "Crit dmg resist", m.CritDmgResist);
        Pct(o, "Crit rate resist", m.CritRateResist);
        Pct(o, "Bow resist", m.BowResist);
        Pct(o, "CC resist", m.CcResist);
        Flat(o, "MP restored", m.RestoreMpBonus);
        Flat(o, "STR", m.Str);
        Flat(o, "AGI", m.Agi);
        Flat(o, "CON", m.Con);
        Flat(o, "INT", m.Int);
        Flat(o, "WIT", m.Wit);
        Flat(o, "SPT", m.Spt);
        Pct(o, "Melee vamp", m.MeleeVamp);
        Pct(o, "Spell vamp", m.SpellVamp);
        Pct(o, "Reflect", m.Reflect);
        Pct(o, "Shield def", m.ShieldDefPct);
        return o;
    }

    // ----- masteries ----------------------------------------------------------------------

    /// <summary>Group a per-SOURCE listing (one entry per weapon / per armour weight) by the SOURCE,
    /// folding together every source that grants exactly the same thing:
    ///
    ///     Sword/Blunt:    P.Atk +10, M.Atk +10, Cast speed +10%
    ///     Dual/dagger/Bow:  Cast speed -100%
    ///
    /// This replaced a by-STAT pivot ("Cast speed:  Sword +10%, Blunt +10%, Bow −100% …"), which was
    /// 32g's answer to "+cast, −cast, +cast" and which the owner rejected on sight (playtest-16):
    /// *"you can still make P.Atk: sword +10 blunt +10, cast: sword +10 blunt +10 dagger −100 bow
    /// −100 … to become sword/blunt +10 pAtk +10 mAtk +10 cast, dagger/bow −100 cast — more compact,
    /// logically written, not throw everything there."*
    ///
    /// He is right, and the reason is that these masteries are authored per WEAPON GROUP: the same
    /// numbers repeat across two or three weapons, so a stat-major listing prints every one of them
    /// again under each stat. Source-major with identical sources merged prints each number ONCE, and
    /// what you read is the decision you are actually making — which weapon to hold.
    ///
    /// Sources granting nothing are dropped rather than listed as empty: a mastery that ignores bows
    /// says so by not mentioning them.</summary>
    private static List<string> BySource(List<(string Source, List<string> Lines)> groups)
    {
        var order = new List<string>();                       // first appearance wins the position
        var names = new Dictionary<string, List<string>>();   // effects -> the sources granting them
        foreach (var (source, lines) in groups)
        {
            if (lines.Count == 0) continue;
            string key = string.Join(", ", lines);
            if (!names.TryGetValue(key, out var list))
            {
                names[key] = list = new List<string>();
                order.Add(key);
            }
            list.Add(source);
        }

        var o = new List<string>();
        foreach (var key in order) o.Add(string.Join("/", names[key]) + ":  " + key);
        return o;
    }

    /// <summary>An armor mastery at one skill level, grouped by WEIGHT, e.g.
    /// "Heavy:  P.Def +8%". Only what actually does something is listed — including the PENALTY
    /// weights, which are the whole point of an armor mastery.</summary>
    public static List<string> ArmorMastery(ArmorMasteryProfile prof) => BySource(new()
    {
        ("Robe", Mods(prof.Robe)),
        ("Light", Mods(prof.Light)),
        ("Heavy", Mods(prof.Heavy)),
        ("No armor", Mods(prof.None)),
    });

    /// <summary>A weapon mastery at one skill level. Same shape as <see cref="ArmorMastery"/>;
    /// "Other" covers empty hand and any unlisted type.</summary>
    public static List<string> WeaponMastery(WeaponMasteryProfile prof) => BySource(new()
    {
        ("Sword", Passive(prof.Sword)),
        ("Blunt", Passive(prof.Blunt)),
        ("Dual/dagger", Passive(prof.Dual)),
        ("Bow", Passive(prof.Bow)),
        ("Other/unarmed", Passive(prof.Other)),
    });

    // ----- active buffs / debuffs ----------------------------------------------------------

    /// <summary>The per-effect magnitudes a buff/debuff applies at a given level, e.g.
    /// "Attack +20%". Percent magnitudes are fractions; flat ones add directly.</summary>
    public static List<string> Buff(SkillDef def, int level = 1)
    {
        var o = new List<string>();
        var mags = def.MagnitudesAt(level);
        if (mags is null) return o;
        foreach (var m in mags)
        {
            string label = EffectLabel(m.Effect);
            if (label.Length == 0) continue;
            if (m.Mode == ModifierMode.Flat) Flat(o, label, m.Value);
            else Pct(o, label, m.Value);
        }
        return o;
    }

    /// <summary>Display name for a buff/debuff effect flag. "" = not worth showing on its own
    /// (a marker flag with no magnitude).</summary>
    public static string EffectLabel(SkillEffect e) => e switch
    {
        SkillEffect.BuffAtk => "Attack",
        SkillEffect.BuffMagAtk => "M.Atk",
        SkillEffect.BuffDef => "P.Def",
        SkillEffect.BuffMagicDef => "M.Def",
        SkillEffect.BuffCastSpeed => "Cast speed",
        SkillEffect.BuffAtkSpeed => "Atk speed",
        SkillEffect.BuffMoveSpeed => "Move speed",
        SkillEffect.BuffEvasion => "Evasion",
        SkillEffect.BuffAccuracy => "Accuracy",
        SkillEffect.BuffHp => "Max HP",
        SkillEffect.BuffMp => "Max MP",
        SkillEffect.BuffHpRegen => "HP regen",
        SkillEffect.BuffMpRegen => "MP regen",
        SkillEffect.BuffCritRate => "Crit rate",
        SkillEffect.BuffMagicCritRate => "M.crit rate",
        SkillEffect.BuffCritDamage => "Crit damage",
        SkillEffect.BuffBlockChance => "Block chance",
        SkillEffect.BuffShieldDef => "Shield def",
        SkillEffect.BuffCooldown => "Reuse delay",
        SkillEffect.BuffPhysAtk => "P.Atk",
        SkillEffect.BuffCritDmgResist => "Crit dmg resist",
        SkillEffect.BuffCritRateResist => "Crit rate resist",
        SkillEffect.BuffBowResist => "Bow resist",
        SkillEffect.BuffMagicResist => "Magic resist",
        SkillEffect.BuffMagicEvasion => "Magic evasion (enemy fail %)",
        SkillEffect.BuffInterruptPower => "Cancel power",
        SkillEffect.BuffInterruptResist => "Cancel resist",
        SkillEffect.BuffMeleeVamp => "Vampiric",
        SkillEffect.BuffSpellVamp => "Spell vamp",
        SkillEffect.BuffCancelResist => "Cancel resist (buffs)",
        SkillEffect.BuffReflect => "Reflect",
        SkillEffect.BuffPveSkillDamage => "PvE skill dmg",
        SkillEffect.BuffPveMagicDamage => "PvE magic dmg",
        SkillEffect.BuffPveBasicDamage => "PvE basic dmg",
        SkillEffect.BuffPvpSkillDamage => "PvP skill dmg",
        SkillEffect.BuffPvpMagicDamage => "PvP magic dmg",
        SkillEffect.BuffPvpBasicDamage => "PvP basic dmg",
        SkillEffect.DebuffDef => "Def down",
        SkillEffect.DebuffHealRecv => "Healing down",
        SkillEffect.HealOverTime => "Heal/tick",
        _ => ""
    };
}
