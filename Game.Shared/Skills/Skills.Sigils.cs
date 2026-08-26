namespace Game.Shared;

/// <summary>
/// THE SIGILS — the 4th class's three permanent passives (owner, 2026-08-26, `shared 4th.csv`).
///
/// <para>On ascending at 76 you commit to <b>one ATTACK, one DEFENCE and one SUPPORT</b> sigil, each
/// for <b>20kk SP + 10kk gold</b>. There are eighteen: six class flavours (Healer / Warrior / Buffer /
/// Tank / Mage / Rogue) × the three slots. <b>Every class may take any of them</b> — his file's
/// "Fighter ideal: Warrior Attack; Tank Defence; Buffer Support" lines are a recommendation, and he
/// relabelled them "ideal" the moment the question was asked.</para>
///
/// <para><b>THE EXCLUSION RULE IS ONE RULE: ONE PER SLOT.</b> His REPLACES column, e.g. Holy Protection:
/// <c>[Warrior / Mage / Tank / Buffer / Rogue Defence]</c> — every OTHER flavour's Defence sigil and
/// nothing else. It is carried by <see cref="SkillDef.ExclusiveGroup"/>, which the learn path already
/// enforces and which is also what makes a sigil show up at the Mindwright's reset list for free.</para>
///
/// <para>⚠ <b>THERE WAS A SECOND RULE AND HE REMOVED IT</b> (2026-08-26). Until that afternoon a sigil
/// also replaced the SAME flavour's other two, so your three always came from three different classes;
/// he relaxed it to *"1-attack, 1-Defence, 1-support from any race/descipline"* after asking whether any
/// same-flavour trio was overpowered. <b>None is</b>, and the reason is structural rather than lucky:
/// the eighteen were authored one-per-slot-per-flavour with <b>no intra-flavour synergy</b> — a flavour's
/// three act on three different channels, so nothing in a trio multiplies another member of it.
///
/// The trio worth checking was the TANK's, the only all-mitigation one: +10% max HP, Aegis's +25% to
/// both defences (a DIVISOR, so −20% damage) at ~40% uptime, and −10 points of crit chance and −10%
/// crit damage. That is about +26% effective HP — and the OLD rule's best defensive pairing, <b>Aegis
/// (Tank-Defence) + Immortality (Buffer-Support)</b>, was already worth the same and left the Attack
/// slot free. The trio is not stronger than what was buildable before, only purer.
///
/// 🔴 <b>TO PUT IT BACK</b> if a playtest disagrees: restore the same-flavour arm of
/// <c>SigilReplaces</c> below (one XOR), and the <c>SigilFlavourClash</c> guard in
/// <c>GameLoopService.HandleLearnSkill</c> plus its row label in the client's Sigils tab.</para>
///
/// <para><b>WHY "SIGIL" AND NOT "RUNE".</b> His CSV called them runes; RUNE is already taken in this
/// game by a HELD ITEM — the War Rune / Spell Rune that replaced shots — and MARK is taken by the
/// healer's Prophecy-shaped blessings. He picked Sigil (2026-08-26) so all three stay distinct words
/// in the same UI. ⚠ The ids below are the persisted keys and are append-only from here.</para>
///
/// <para><b>THE PROC SIGILS.</b> Six of the eighteen are not flat bonuses but "3% chance on attack /
/// on damage received to …". They ride the on-hit proc machinery the Warchanter's Combo Mastery
/// introduced (<see cref="SkillDef.ProcChance"/>), extended here with the DEFENSIVE trigger — the
/// same fields, rolled when the owner TAKES damage instead of deals it (<c>SkillDef.ProcOnDamaged</c>).
/// The payload is a rung skill named in <c>ProcSelfRungs</c>, which is why the little buffs at the
/// bottom of this file exist; two of them are an instant heal / recharge rather than a buff, and the
/// proc handler dispatches on the rung's own effect flags.</para>
///
/// <para>⚠ <b>Their durations and cooldowns are FIXED and they do not count against the buff cap</b> —
/// his line, verbatim: *"Durations and Cooldowns are Fixed and dont count towards buff limit"*. So every
/// payload below carries <c>FixedCooldown</c> and <c>CountsTowardBuffLimit: false</c>; a sigil proc must
/// never be the thing that pushes a blessing off your bar.</para>
/// </summary>
public static partial class SkillCatalog
{
    /// <summary>Which of the three slots a sigil occupies. One per character.</summary>
    public enum SigilSlot { Attack, Defence, Support }

    /// <summary>Which class's flavour a sigil carries. One per character — see the class summary.
    /// This is NOT a gate on who may learn it; every 4th class may take any of the eighteen.</summary>
    public enum SigilFlavour { Healer, Warrior, Buffer, Tank, Mage, Rogue }

    // ---- Sigil ids. `<flavour>_<slot>_sigil`, which is his own naming from the CSV's comment
    //      column (`Healer_Defence_Rune`) with the word he chose on 2026-08-26. ----
    public const string HealerAttackSigil   = "healer_attack_sigil";
    public const string HealerDefenceSigil  = "healer_defence_sigil";
    public const string HealerSupportSigil  = "healer_support_sigil";
    public const string WarriorAttackSigil  = "warrior_attack_sigil";
    public const string WarriorDefenceSigil = "warrior_defence_sigil";
    public const string WarriorSupportSigil = "warrior_support_sigil";
    public const string BufferAttackSigil   = "buffer_attack_sigil";
    public const string BufferDefenceSigil  = "buffer_defence_sigil";
    public const string BufferSupportSigil  = "buffer_support_sigil";
    public const string TankAttackSigil     = "tank_attack_sigil";
    public const string TankDefenceSigil    = "tank_defence_sigil";
    public const string TankSupportSigil    = "tank_support_sigil";
    public const string MageAttackSigil     = "mage_attack_sigil";
    public const string MageDefenceSigil    = "mage_defence_sigil";
    public const string MageSupportSigil    = "mage_support_sigil";
    public const string RogueAttackSigil    = "rogue_attack_sigil";
    public const string RogueDefenceSigil   = "rogue_defence_sigil";
    public const string RogueSupportSigil   = "rogue_support_sigil";

    /// <summary>The exclusive group per SLOT. Three groups, not one: two sigils of the same slot may
    /// never be held together, but an Attack and a Defence sigil obviously may.</summary>
    public const string SigilGroupAttack  = "sigil_attack";
    public const string SigilGroupDefence = "sigil_defence";
    public const string SigilGroupSupport = "sigil_support";

    /// <summary>4th class only — and the LEVEL is not the gate that matters. The kit is injected by
    /// <c>ClassSkills.Cumulative</c> only when the character has actually ascended (paid the 100kk
    /// Rite of Ascension), so a level-76 who has not is offered nothing.</summary>
    public const int SigilLearnLevel = FourthClassCatalog.ChangeLevel;   // 76

    /// <summary>His price, the same for all eighteen: 20kk SP + 10kk gold.</summary>
    public const int SigilSpCost = 20_000_000;
    public const int SigilGoldCost = 10_000_000;

    /// <summary>What the Mindwright charges to strike ONE sigil off, so the slot is free to commit to
    /// again — *"then to reset them u go to the mindweaver and reset them for 10kk gold (no sp/no gold
    /// refund)"*. ⚠ Read as PER SIGIL, which is what the existing per-skill Forget button at that NPC
    /// already is, and which makes the reset cost exactly what re-committing costs.</summary>
    public const int SigilResetGold = 10_000_000;

    /// <summary>The eighteen, in slot-then-flavour order (the order the Sigils tab renders).</summary>
    public static readonly string[] AllSigilIds =
    {
        HealerAttackSigil,  WarriorAttackSigil,  BufferAttackSigil,
        TankAttackSigil,    MageAttackSigil,     RogueAttackSigil,
        HealerDefenceSigil, WarriorDefenceSigil, BufferDefenceSigil,
        TankDefenceSigil,   MageDefenceSigil,    RogueDefenceSigil,
        HealerSupportSigil, WarriorSupportSigil, BufferSupportSigil,
        TankSupportSigil,   MageSupportSigil,    RogueSupportSigil,
    };

    private static readonly Dictionary<string, (SigilFlavour Flavour, SigilSlot Slot)> SigilTable =
        new()
        {
            [HealerAttackSigil]   = (SigilFlavour.Healer,  SigilSlot.Attack),
            [HealerDefenceSigil]  = (SigilFlavour.Healer,  SigilSlot.Defence),
            [HealerSupportSigil]  = (SigilFlavour.Healer,  SigilSlot.Support),
            [WarriorAttackSigil]  = (SigilFlavour.Warrior, SigilSlot.Attack),
            [WarriorDefenceSigil] = (SigilFlavour.Warrior, SigilSlot.Defence),
            [WarriorSupportSigil] = (SigilFlavour.Warrior, SigilSlot.Support),
            [BufferAttackSigil]   = (SigilFlavour.Buffer,  SigilSlot.Attack),
            [BufferDefenceSigil]  = (SigilFlavour.Buffer,  SigilSlot.Defence),
            [BufferSupportSigil]  = (SigilFlavour.Buffer,  SigilSlot.Support),
            [TankAttackSigil]     = (SigilFlavour.Tank,    SigilSlot.Attack),
            [TankDefenceSigil]    = (SigilFlavour.Tank,    SigilSlot.Defence),
            [TankSupportSigil]    = (SigilFlavour.Tank,    SigilSlot.Support),
            [MageAttackSigil]     = (SigilFlavour.Mage,    SigilSlot.Attack),
            [MageDefenceSigil]    = (SigilFlavour.Mage,    SigilSlot.Defence),
            [MageSupportSigil]    = (SigilFlavour.Mage,    SigilSlot.Support),
            [RogueAttackSigil]    = (SigilFlavour.Rogue,   SigilSlot.Attack),
            [RogueDefenceSigil]   = (SigilFlavour.Rogue,   SigilSlot.Defence),
            [RogueSupportSigil]   = (SigilFlavour.Rogue,   SigilSlot.Support),
        };

    /// <summary>(flavour, slot) for a sigil id, or null if the id is not a sigil. The one place any
    /// caller asks "is this a sigil, and which one" — server, client and the reset NPC all read it.</summary>
    public static (SigilFlavour Flavour, SigilSlot Slot)? SigilOf(string skillId) =>
        SigilTable.TryGetValue(skillId, out var v) ? v : null;

    /// <summary>The exclusive group a slot uses.</summary>
    public static string SigilGroup(SigilSlot slot) => slot switch
    {
        SigilSlot.Attack  => SigilGroupAttack,
        SigilSlot.Defence => SigilGroupDefence,
        _                 => SigilGroupSupport,
    };

    // ---- The little payload skills the PROC sigils hand out. Not learnable, never on a bar, never
    //      counted against the buff cap; they exist because a proc's payload is named as a skill id
    //      (`ProcSelfRungs`), which is the shape the Warchanter's Combo Mastery established. ----
    private const string SigilFuryHaste     = "sigil_fury_haste";
    private const string SigilFrenzySurge   = "sigil_frenzy_surge";
    private const string SigilAegisGuard    = "sigil_aegis_guard";
    private const string SigilFocusEdge     = "sigil_focus_edge";
    private const string SigilImmortality   = "sigil_immortality";
    private const string SigilHolyMend      = "sigil_holy_mend";
    private const string SigilArcaneWell    = "sigil_arcane_well";

    /// <summary>Everything in this file, for BuildCatalog.</summary>
    private static SkillDef[] SigilSkills()
    {
        // One sigil, stated the same way every time. All eighteen share price, learn level, target and
        // shape; what differs is the name, the payload and which two lists it replaces.
        SkillDef Sigil(string id, string name, SigilFlavour flavour, SigilSlot slot, string blurb,
                       PassiveEffect? passive = null,
                       float procChance = 0f, bool procOnDamaged = false,
                       int procCooldownTicks = 0, string? procRung = null) =>
            new(id, name, BaseClass.Fighter, SkillEffect.None,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                Category: SkillCategory.Passive,
                SpCost: SigilSpCost,
                // ⚠ GOLD lives on the LEVEL, not on the SkillDef — `SkillDef.GoldCostAt` reads
                // `Lvl(level)?.GoldCost`, so a single-level skill with no Levels array is FREE in gold
                // however its def is written. One level, carrying the whole price.
                Levels: new[] { new SkillLevel(SpCost: SigilSpCost, GoldCost: SigilGoldCost,
                                               Passive: passive, Description: blurb) },
                ExclusiveGroup: SigilGroup(slot),
                // Both halves of his REPLACES column, generated rather than typed out 18 × 7 times:
                // the same flavour's other two slots, and every other flavour's same slot.
                Replaces: SigilReplaces(flavour, slot),
                Passive: passive,
                ProcChance: procChance, ProcOnDamaged: procOnDamaged,
                ProcCooldownTicks: procCooldownTicks,
                ProcSelfRungs: procRung is null ? null : new[] { procRung },
                Description: blurb);

        // ⚠ `BaseClass.Fighter` above is inert: a passive with no weapon requirement is class-blind,
        // and WHO may learn it is decided entirely by the class table (here: every ascended class).
        // Every sigil uses it for the same reason the stat swaps do.

        return new SkillDef[]
        {
            // ═══ HEALER ══════════════════════════════════════════════════════════════════════════
            Sigil(HealerAttackSigil, "Holy Power Sigil", SigilFlavour.Healer, SigilSlot.Attack,
                "Your healing is 5% stronger.",
                passive: new PassiveEffect(HealPowerPct: 0.05f)),

            Sigil(HealerDefenceSigil, "Holy Protection Sigil", SigilFlavour.Healer, SigilSlot.Defence,
                "Debuffs that contest your Spirit are 10% less likely to land on you.",
                passive: new PassiveEffect(CcResistMagical: 0.10f)),

            Sigil(HealerSupportSigil, "Holy Support Sigil", SigilFlavour.Healer, SigilSlot.Support,
                "When you are hit, a 5% chance to mend 2% of your maximum HP.",
                procChance: 0.05f, procOnDamaged: true, procCooldownTicks: 50,
                procRung: SigilHolyMend),

            // ═══ WARRIOR ═════════════════════════════════════════════════════════════════════════
            Sigil(WarriorAttackSigil, "Fury Sigil", SigilFlavour.Warrior, SigilSlot.Attack,
                "When you attack, a 3% chance to swing 30% faster for 15 seconds.",
                procChance: 0.03f, procCooldownTicks: 200, procRung: SigilFuryHaste),

            Sigil(WarriorDefenceSigil, "Duel Sigil", SigilFlavour.Warrior, SigilSlot.Defence,
                "You take 5% less damage from other players.",
                // NEGATIVE = less taken, the same convention the armour sets' PvP clause uses.
                passive: new PassiveEffect(PvpDamageTakenPct: -0.05f)),

            Sigil(WarriorSupportSigil, "Fortitude Sigil", SigilFlavour.Warrior, SigilSlot.Support,
                "Debuffs that contest your Constitution are 5% less likely to land on you.",
                passive: new PassiveEffect(CcResistPhysical: 0.05f)),

            // ═══ BUFFER ══════════════════════════════════════════════════════════════════════════
            Sigil(BufferAttackSigil, "Soul Sigil", SigilFlavour.Buffer, SigilSlot.Attack,
                "Maximum MP +10%.",
                passive: new PassiveEffect(MaxMpPct: 0.10f)),

            Sigil(BufferDefenceSigil, "Spirit Sigil", SigilFlavour.Buffer, SigilSlot.Defence,
                "MP regeneration +10%.",
                passive: new PassiveEffect(MpRegenPct: 0.10f)),

            Sigil(BufferSupportSigil, "Immortality Sigil", SigilFlavour.Buffer, SigilSlot.Support,
                "When you are hit, a 3% chance to become immortal for 5 seconds — your HP cannot fall, "
                + "and cannot be healed either.",
                procChance: 0.03f, procOnDamaged: true, procCooldownTicks: 200,
                procRung: SigilImmortality),

            // ═══ TANK ════════════════════════════════════════════════════════════════════════════
            Sigil(TankAttackSigil, "Body Sigil", SigilFlavour.Tank, SigilSlot.Attack,
                "Maximum HP +10%.",
                passive: new PassiveEffect(MaxHpPct: 0.10f)),

            Sigil(TankDefenceSigil, "Aegis Sigil", SigilFlavour.Tank, SigilSlot.Defence,
                "When you are hit, a 3% chance to raise both defences by 25% for 15 seconds.",
                procChance: 0.03f, procOnDamaged: true, procCooldownTicks: 200,
                procRung: SigilAegisGuard),

            Sigil(TankSupportSigil, "Critical Protection Sigil", SigilFlavour.Tank, SigilSlot.Support,
                "Attackers are 10% less likely to crit you, and their crits hit 10% softer.",
                passive: new PassiveEffect(CritRateResist: 0.10f, CritDmgResist: 0.10f)),

            // ═══ MAGE ════════════════════════════════════════════════════════════════════════════
            Sigil(MageAttackSigil, "Frenzy Sigil", SigilFlavour.Mage, SigilSlot.Attack,
                "When you attack, a 3% chance to raise both attacks and both speeds by 8% for 15 seconds.",
                procChance: 0.03f, procCooldownTicks: 200, procRung: SigilFrenzySurge),

            Sigil(MageDefenceSigil, "Mage Defence Sigil", SigilFlavour.Mage, SigilSlot.Defence,
                "Magic defence +7%.",
                passive: new PassiveEffect(MagicDefencePct: 0.07f)),

            Sigil(MageSupportSigil, "Arcane Support Sigil", SigilFlavour.Mage, SigilSlot.Support,
                "When you attack, a 5% chance to recover 2% of your maximum MP.",
                procChance: 0.05f, procCooldownTicks: 50, procRung: SigilArcaneWell),

            // ═══ ROGUE ═══════════════════════════════════════════════════════════════════════════
            Sigil(RogueAttackSigil, "Focus Sigil", SigilFlavour.Rogue, SigilSlot.Attack,
                "When you attack, a 3% chance to raise your critical rate by 5 points for 15 seconds.",
                procChance: 0.03f, procCooldownTicks: 200, procRung: SigilFocusEdge),

            Sigil(RogueDefenceSigil, "Agility Sigil", SigilFlavour.Rogue, SigilSlot.Defence,
                "Evasion +3, and spells aimed at you fail 3 points more often.",
                passive: new PassiveEffect(Evasion: 3, MagicEvasion: 3f)),

            Sigil(RogueSupportSigil, "Aim Sigil", SigilFlavour.Rogue, SigilSlot.Support,
                "With a bow, attack range +100. With any other weapon, accuracy +5.",
                // BowRange is already bow-conditional in RecomputeDerived; the accuracy is not, so a
                // bow user technically gets both. That is his row read literally — the two clauses
                // name different weapons, and gating the accuracy as well would need a second
                // conditional field for a 5-point difference nobody would notice.
                passive: new PassiveEffect(Accuracy: 5, BowRange: 100f)),

            // ═══ THE PROC PAYLOADS ═══════════════════════════════════════════════════════════════
            // ⚠ Every one of these is FixedCooldown + CountsTowardBuffLimit:false, per his line
            //   *"Durations and Cooldowns are Fixed and dont count towards buff limit"*. Their own
            //   BuffKeys are distinct, so two different sigils never evict one another.

            new(SigilFuryHaste, "Fury", BaseClass.Fighter, SkillEffect.BuffAtkSpeed,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                DurationTicks: 150, BuffKey: "sigil_fury", Rank: 1,
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtkSpeed, 0.30f, ModifierMode.Percent) },
                FixedCooldown: true, CountsTowardBuffLimit: false,
                Description: "Attack speed +30%."),

            new(SigilFrenzySurge, "Frenzy", BaseClass.Fighter,
                SkillEffect.BuffPhysAtk | SkillEffect.BuffMagAtk | SkillEffect.BuffAtkSpeed | SkillEffect.BuffCastSpeed,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                DurationTicks: 150, BuffKey: "sigil_frenzy", Rank: 1,
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.BuffPhysAtk,   0.08f, ModifierMode.Percent),
                    new(SkillEffect.BuffMagAtk,    0.08f, ModifierMode.Percent),
                    new(SkillEffect.BuffAtkSpeed,  0.08f, ModifierMode.Percent),
                    new(SkillEffect.BuffCastSpeed, 0.08f, ModifierMode.Percent),
                },
                FixedCooldown: true, CountsTowardBuffLimit: false,
                Description: "P.Atk, M.Atk, attack speed and cast speed all +8%."),

            new(SigilAegisGuard, "Aegis", BaseClass.Fighter,
                SkillEffect.BuffDef | SkillEffect.BuffMagicDef,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                DurationTicks: 150, BuffKey: "sigil_aegis", Rank: 1,
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.BuffDef,      0.25f, ModifierMode.Percent),
                    new(SkillEffect.BuffMagicDef, 0.25f, ModifierMode.Percent),
                },
                FixedCooldown: true, CountsTowardBuffLimit: false,
                Description: "P.Def and M.Def +25%."),

            // ⚠ "+50" on his 0-1000 crit scale is +5 percentage points of crit chance — his own note in
            // the comment column: *"Flat 50-> everyone gets flat 5% increase in crit"*. It is the FLAT
            // channel on purpose (the same reason gear crit is flat): a multiplier here would only pay
            // the dagger who already crits, and this sigil is meant to be worth taking on a blunt.
            new(SigilFocusEdge, "Focus", BaseClass.Fighter, SkillEffect.BuffCritRate,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                DurationTicks: 150, BuffKey: "sigil_focus", Rank: 1,
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffCritRate, 0.05f, ModifierMode.Flat) },
                FixedCooldown: true, CountsTowardBuffLimit: false,
                Description: "Critical rate +5 points."),

            // The only buff in the game with no stat effect at all — its whole payload is the flag.
            new(SigilImmortality, "Immortality", BaseClass.Fighter, SkillEffect.None,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                DurationTicks: 50, BuffKey: "sigil_immortality", Rank: 1,
                FreezesHp: true, Cancellable: false,
                FixedCooldown: true, CountsTowardBuffLimit: false,
                Description: "Your HP cannot change — nothing damages it, and nothing heals it."),

            // The two INSTANT payloads. The proc handler dispatches on these flags rather than calling
            // ApplyBuff, so a proc can pay out in HP or MP as easily as in a buff.
            new(SigilHolyMend, "Holy Mend", BaseClass.Mage, SkillEffect.Heal,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                Category: SkillCategory.Heal,
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.Heal, 0.02f, ModifierMode.Percent) },
                FixedCooldown: true, CountsTowardBuffLimit: false,
                Description: "Restores 2% of your maximum HP."),

            new(SigilArcaneWell, "Arcane Wellspring", BaseClass.Mage, SkillEffect.RestoreMp,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                Category: SkillCategory.Heal,
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.RestoreMp, 0.02f, ModifierMode.Percent) },
                FixedCooldown: true, CountsTowardBuffLimit: false,
                Description: "Restores 2% of your maximum MP."),
        };
    }

    /// <summary>His REPLACES column, generated: every OTHER flavour's SAME slot. Five ids, exactly as
    /// he wrote each row out by hand.
    ///
    /// <para>⚠ It used to be seven — the same-flavour XOR — until he dropped that half on 2026-08-26.
    /// See the class summary for the arithmetic that said it was safe to, and for how to restore it.</para></summary>
    private static string[] SigilReplaces(SigilFlavour flavour, SigilSlot slot)
    {
        var list = new List<string>(5);
        foreach (var (id, v) in SigilTable)
            if (v.Slot == slot && v.Flavour != flavour)
                list.Add(id);
        list.Sort(StringComparer.Ordinal);   // stable order so a diff of this file is readable
        return list.ToArray();
    }
}
