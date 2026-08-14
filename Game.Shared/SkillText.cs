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

        // Shield / bow CONDITIONALS. The condition is part of the number (`BL-42`): a Shield Mastery
        // that reads "Block chance +200%" on a card is a promise the skill does not keep with an empty
        // off-hand, and his ask was explicitly for *"the conditional lines"* — the "light-armour-only"
        // bonuses and their kind — to say what they depend on. The armour and weapon masteries below
        // already state their condition by being grouped under "Light:" / "Bow:"; these three were the
        // ones stating a bare number with the condition buried in the prose.
        Pct(o, "Block chance (with a shield)", p.BlockChancePct);
        Pct(o, "Shield def (with a shield)", p.ShieldDefPct);
        Flat(o, "Bow range (with a bow)", p.BowRange);

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
        // The three skill-defence channels (BL-06/07/08). Worded from the DEFENDER's side, which is
        // whose card they appear on, and the reflect pair reads as one line — the chance alone is
        // meaningless without knowing how much comes back.
        Pct(o, "Dodge physical skills", p.SkillEvadeChance);
        if (p.PhysSkillReflectChance > 0f)
            o.Add($"Reflect physical skills {p.PhysSkillReflectChance * 100f:0.#}% "
                + $"of the time, for {p.PhysSkillReflectPct * 100f:0.#}% damage");
        Pct(o, "Reflect debuffs", p.DebuffReflectChance);

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
        Flat(o, "Accuracy", m.Accuracy);       Pct(o, "Accuracy", m.AccuracyPct);
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
        // 🔴 THE FOUR S-GRADE CHANNELS (playtest-21 `67i`). He read the light-S set's flat +200 crit
        // damage on his stat sheet and then could not find it on the Leather armour card: these four
        // were APPENDED to StatMods for the 0.59.1 S sets and never given a line here, so every set
        // that carries one described itself with the number silently missing — not just Nightleaf's
        // crit damage, but Ironforge's flat crit rate, both S sets' magic resist, and the PvP-taken
        // reduction on all of them. The labels match the stat sheet's own wording on purpose ("Crit dmg
        // flat"), because comparing the two is exactly what he was doing when he found this.
        //
        // ⚠ Whenever a field is appended to StatMods, it needs a line here in the same commit — the
        // formatter is the only thing that makes an authored number visible, and nothing fails loudly
        // when it is missing.
        Pct(o, "Crit rate flat", m.CritRateFlat);
        Flat(o, "Crit dmg flat", m.CritDamageFlat);
        Pct(o, "Magic resist", m.MagicResist);
        Pct(o, "PvP damage taken", m.PvpDamageTakenPct);
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

    // ----- mechanics: the payloads that ride as FIELDS, not as effect flags --------------------

    /// <summary>Everything a skill does that is carried by a FIELD on <see cref="SkillDef"/> rather
    /// than by a <see cref="SkillEffect"/> flag or a stat magnitude — stated with its real numbers, at
    /// the given LEVEL (`BL-42`).
    ///
    /// <para>🔑 Why this had to exist at all. The <c>SkillEffect</c> flag enum has been FULL for a long
    /// time (1L &lt;&lt; 62 was the last bit), so for years every new mechanic has been added as a plain
    /// field on the record: <c>Resurrect</c>, <c>KeepsBuffsOnDeath</c>, <c>AutoResurrect</c>,
    /// <c>Lifesteal</c>, <c>GrantsHide</c>, <c>PlacesTrap</c>, <c>Rewards</c>, <c>TauntPower</c>,
    /// <c>BlockAccuracy</c>, the fixed-timing flags, and more. The skill card reads flags and
    /// magnitudes, so <b>none of those ever appeared on it</b>. That is the gap behind his
    /// *"all skills and passive should show the desctiption with numbers"*: the numbers existed on the
    /// def the whole time and nothing formatted them, exactly as with passives before
    /// <see cref="Passive"/>. Angel's Protection is the clearest case — its entire payload is one bool,
    /// so its card could say nothing whatsoever about what it does.</para>
    ///
    /// <para>⚠ AUTHORING RULE, the same one <see cref="Mods"/> carries: when a field is added to
    /// SkillDef, give it a line HERE in the same commit. Nothing fails loudly when it is missing — the
    /// skill just quietly stops describing itself, which is the bug this method is fixing.</para>
    ///
    /// <para>Conditions are stated with the effect, never left to the prose: a bonus that only applies
    /// while slowed, only below 40% HP or only with a bow says so on its own line.</para></summary>
    public static List<string> Mechanics(SkillDef def, int level = 1)
    {
        var o = new List<string>();
        if (def is null) return o;

        // ---- Resurrection / death ----
        if (def.Resurrect)
        {
            float pct = def.ResExpPctAt(level);
            o.Add(pct > 0f
                ? $"Revives a fallen ally at 30% HP/MP and restores {pct * 100f:0.#}% of the exp they lost"
                : "Revives a fallen ally at 30% HP/MP (restores none of their lost exp)");
        }
        if (def.KeepsBuffsOnDeath)
            o.Add("Your other buffs SURVIVE your next death (only this blessing is consumed)");
        if (def.AutoResurrect)
        {
            float pct = def.ResExpPctAt(level);
            o.Add(pct > 0f
                ? $"Revives you where you fell at 30% HP/MP, restoring {pct * 100f:0.#}% of the lost exp"
                : "Revives you where you fell at 30% HP/MP");
        }

        // ---- Damage shaping ----
        if (def.Lifesteal > 0f)
            o.Add($"Heals you for {def.Lifesteal * 100f:0.#}% of the damage dealt");
        if (def.ConditionalOn != TargetCondition.None && def.ConditionalDamagePct != 0f)
            o.Add($"{Sign(def.ConditionalDamagePct)}{def.ConditionalDamagePct * 100f:0.#}% damage "
                + $"against a {def.ConditionalOn.ToString().ToLowerInvariant()} target");
        if (def.PveDamageMult != 1f) o.Add($"Damage vs monsters x{def.PveDamageMult:0.##}");
        if (def.PvpDamageMult != 1f) o.Add($"Damage vs players x{def.PvpDamageMult:0.##}");

        // ---- Hit resolution ----
        if (def.SureHit) o.Add("Never misses");
        if (def.BlockAccuracy > 0f)
            o.Add($"Bypasses shield blocks {def.BlockAccuracy * 100f:0.#}% of the time");
        if (def.InterruptPower > 0) o.Add($"Interrupt power {def.InterruptPower}");
        if (def.InterruptDefense > 0) o.Add($"Interrupt resistance while casting +{def.InterruptDefense}");
        if (def.DebuffSchool != DebuffSchool.None)
            o.Add($"Landing is contested ({def.DebuffSchool.ToString().ToLowerInvariant()})");

        // ---- Stacking ----
        int stacks = def.EffectiveMaxStacks;
        if (stacks > 1) o.Add($"Stacks up to {stacks} times");
        if (def.ConsumeStackKey.Length > 0)
            o.Add("Consumes its stacks, multiplying the damage by how many had built up");

        // ---- Dispel ----
        if ((def.Effect & (SkillEffect.Cleanse | SkillEffect.Cancel)) != 0)
        {
            bool cancel = (def.Effect & SkillEffect.Cancel) != 0;
            string what = cancel ? "enemy buffs" : "debuffs";
            string many = def.DispelCount > 0 ? $"up to {def.DispelCount}" : "all";
            string cap = def.DispelMaxLevel > 0 ? $" of rank {def.DispelMaxLevel} or lower" : "";
            o.Add($"Removes {many} {what}{cap}");
        }
        if (!def.Cancellable) o.Add("Cannot be cancelled or cured");

        // ---- Movement ----
        if (def.BlinkRange > 0f) o.Add($"Teleports you {(int)def.BlinkRange} away from the target");
        else if ((def.Effect & SkillEffect.Blink) != 0) o.Add("Teleports you behind the target");
        if (def.KnockbackRange > 0f) o.Add($"Knocks the target back {(int)def.KnockbackRange}");
        if (def.TeleportsToTown) o.Add("Teleports you to the nearest town");

        // ---- Stealth (BL-69) ----
        if (def.GrantsHide)
            o.Add("Makes you invisible — broken by any action except moving");
        if (def.GrantsMobStealth)
            o.Add("Monsters that have not already noticed you ignore you (players still see you)");
        if (def.RevealsHidden)
            o.Add(def.NoHideTicks > 0
                ? $"Reveals everyone hidden in the area and stops them re-hiding for {Secs(def.NoHideTicks)}"
                : "Reveals everyone hidden in the area");

        // ---- Traps ----
        if (def.PlacesTrap)
            o.Add($"Drops a trap at your feet: triggers within {(int)def.TrapRadius}, "
                + $"lasts {Secs(def.TrapLifeTicks)}");

        // ---- Threat (BL-71) ----
        int taunt = def.TauntPowerAt(level);
        if (taunt > 0) o.Add($"Threat {taunt:N0}, on top of jumping you to the top of the table");

        // ---- Toggles + MP economy ----
        if (def.Toggle)
            o.Add(def.MpPerSecond > 0
                ? $"Toggle — stays up until switched off, costing {def.MpPerSecond} MP a second"
                : "Toggle — stays up until switched off");
        if (def.PhysMpCostPct > 0f) o.Add($"Physical skills cost {def.PhysMpCostPct * 100f:0.#}% less MP");
        if (def.MagicMpCostPct > 0f) o.Add($"Magic skills cost {def.MagicMpCostPct * 100f:0.#}% less MP");

        // ---- Reward rates (the premium runes) ----
        var r = def.RewardsAt(level);
        if (!r.IsNeutral)
        {
            Pct(o, "Experience", r.Exp);
            Pct(o, "Skill points", r.Sp);
            Pct(o, "Gold from monsters", r.Gold);
            Pct(o, "Drop chance", r.Drop);
            if (r.StopsExpSp) o.Add("You gain NO experience or skill points");
            if (r.StopsGoldDrop) o.Add("Monsters drop NO gold or items for you");
        }

        // ---- Conditions on USING it ----
        if (def.RequiredWeapon != WeaponType.None)
            o.Add("Requires a " + def.RequiredWeapon.ToString().ToLowerInvariant().Replace(",", " or")
                + " weapon");
        if (def.RequireHpBelowFraction > 0f)
            o.Add($"Only usable at or below {def.RequireHpBelowFraction * 100f:0.#}% HP");
        if (def.ConsumableId.Length > 0)
            o.Add($"Consumes {def.ConsumableAmount} x {ItemCatalog.Get(def.ConsumableId)?.Name ?? def.ConsumableId}");

        // ---- Timing quirks. Worth stating because they are exactly the promises a player plans
        //      around: a fixed cast does not reward cast speed, and a fragile one dies to a scratch.
        if (def.FixedCast) o.Add("Cast time is fixed — cast speed does not shorten it");
        if (def.FixedCooldown) o.Add("Reuse is fixed — cooldown reduction does not shorten it");
        if (def.FragileCast) o.Add("ANY damage taken cancels the cast");

        return o;
    }

    private static string Secs(int ticks) =>
        (ticks / (float)GameConstants.TickRate).ToString("0.#") + "s";
}
