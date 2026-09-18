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
    public const string HolyPowerSigil   = "holy_power_sigil";
    public const string HolyProtectionSigil  = "holy_protection_sigil";
    public const string HolySupportSigil  = "holy_support_sigil";
    public const string FurySigil  = "fury_sigil";
    public const string DuelSigil = "duel_sigil";
    public const string FortitudeSigil = "fortitude_sigil";
    public const string SoulSigil   = "soul_sigil";
    public const string SpiritSigil  = "spirit_sigil";
    public const string ImmortalitySigil  = "immortality_sigil";
    public const string BodySigil     = "body_sigil";
    public const string AegisSigil    = "aegis_sigil";
    public const string CriticalProtectionSigil    = "critical_protection_sigil";
    public const string MageAttackSigil     = "mage_attack_sigil";
    public const string MageDefenceSigil    = "mage_defence_sigil";
    public const string MageSupportSigil    = "mage_support_sigil";
    public const string FocusSigil    = "focus_sigil";
    public const string AgilitySigil   = "agility_sigil";
    public const string AimSigil   = "aim_sigil";

    /// <summary>`BL-250` §3 — WHICH SIGIL GROUP A DISCIPLINE UNLOCKS. His table, verbatim: the three
    /// Apprentice unlock <b>mage</b>, the Priest's healer discipline <b>healer</b> and its buffer
    /// discipline <b>buffer</b>, the six Rogue <b>rogue</b>, the six Warrior <b>warrior</b>, the three
    /// Knight <b>tank</b>.
    ///
    /// <para>🔑 <b>IT IS KEYED ON THE ARCHETYPE EXCEPT WHERE HE SPLIT IT</b> — the Healer archetype is
    /// the one row that maps to two groups, because Lightbringer and Warchanter are two different
    /// rewards and he listed them separately. Everything else falls out of the archetype, which is why
    /// the archer's six and the dagger's six both read "rogue": they are one archetype pair in his
    /// table, not two.</para>
    ///
    /// <para>⚠ Today this only DESCRIBES a class in the subclass panel (`BL-250` §8). The gating half —
    /// a group being locked until you own a subclass of it — ships with the sigil rework.</para></summary>
    public static SigilFlavour SigilGroupOf(Discipline d) => d switch
    {
        Discipline.Warchanter => SigilFlavour.Buffer,
        Discipline.Lightbringer => SigilFlavour.Healer,
        _ => Disciplines.PathOf(d).Archetype switch
        {
            Archetype.Tank => SigilFlavour.Tank,
            Archetype.Warrior => SigilFlavour.Warrior,
            Archetype.Nuker => SigilFlavour.Mage,
            Archetype.Healer => SigilFlavour.Healer,
            _ => SigilFlavour.Rogue,   // Rogue AND Archer — one group in his table
        },
    };

    /// <summary>The three sigils of one group, in slot order (Attack, Defence, Support).</summary>
    public static string[] SigilsOfGroup(SigilFlavour flavour) =>
        AllSigilIds.Where(id => SigilTable[id].Flavour == flavour)
                   .OrderBy(id => (int)SigilTable[id].Slot)
                   .ToArray();

    /// <summary>The exclusive group per SLOT. Three groups, not one: two sigils of the same slot may
    /// never be held together, but an Attack and a Defence sigil obviously may.</summary>
    public const string SigilGroupAttack  = "sigil_attack";
    public const string SigilGroupDefence = "sigil_defence";
    public const string SigilGroupSupport = "sigil_support";

    /// <summary>4th class only — and the LEVEL is not the gate that matters. The kit is injected by
    /// <c>ClassSkills.Cumulative</c> only when the character has actually ascended (paid the 100kk
    /// Rite of Ascension), so a level-76 who has not is offered nothing.</summary>
    public const int SigilLearnLevel = FourthClassCatalog.ChangeLevel;   // 76

    /// <summary>🔴 <b>FREE SINCE `BL-250` §4</b> (2026-09-17): *"we can remove their sp/gold cost -> they
    /// are their own system. only clearing will cost 100kk (its 10kk now i think + losing the 60kk sp and
    /// 30kk gold)"*. They were 20kk SP + 10kk gold each.
    ///
    /// <para>🔑 <b>THE PRICE DID NOT VANISH, IT MOVED ONTO THE ROAD.</b> Three sigils used to be level 76
    /// plus 60kk SP and 30kk gold on ONE character. Three sigils are now <b>three subclasses each
    /// levelled to 75</b>, every one of them born at 40 with no SP — *"yes sigils become end game and
    /// hard"*. Charging for the commit on top of that would be charging twice for the same thing.</para>
    ///
    /// <para>⚠ Kept as named constants at zero rather than deleted: they are read by the learn path, the
    /// Sigils tab and the affordability label, and a literal 0 in three places is how a price quietly
    /// comes back in one of them.</para></summary>
    public const int SigilSpCost = 0;
    public const int SigilGoldCost = 0;

    /// <summary>What the Mindwright charges to CLEAR YOUR SIGILS — *"only clearing will cost 100kk"*,
    /// and his 2026-09-17 ruling on the one reading the entry was holding: <b>100kk WIPES ALL THREE</b>.
    ///
    /// <para>🔑 <b>IT IS ONE PAYMENT FOR THE WHOLE BOARD, NOT A PER-SIGIL FEE.</b> That was a real fork —
    /// his sentence priced "clearing" against the old cost of ONE sigil, so per-sigil was the other
    /// reading, and it would have made a full reset 300kk. He chose the wipe. ⚠ Which means striking off
    /// a single sigil is NOT a thing you can do any more: tapping any worn sigil at the Mindwright clears
    /// the lot, and the dialog says so before it takes the gold.</para></summary>
    public const int SigilResetGold = 100_000_000;

    /// <summary>`BL-250` §1/§2 — how many sigils a character may wear at once, and therefore how many
    /// SLOTS the ladder can ever open. Three, and the third is the last: *"the 1st three subs are
    /// required to open the 3 slot -> then every other just opens their tree (if not opened)"*.
    ///
    /// <para>⚠ Not the same three as before. It used to mean one Attack, one Defence and one Support;
    /// it now means three of the eighteen, in any mix.</para></summary>
    public const int MaxSigils = 3;

    /// <summary>`BL-250` §3 — the unlocked sigil GROUPS as a bitmask, one bit per
    /// <see cref="SigilFlavour"/>. A mask rather than a list because it crosses the wire on every
    /// subclass push and the client only ever asks "is this one in".</summary>
    public static int SigilGroupBit(SigilFlavour f) => 1 << (int)f;

    /// <summary>Is this group unlocked in that mask?</summary>
    public static bool SigilGroupUnlocked(int mask, SigilFlavour f) => (mask & SigilGroupBit(f)) != 0;

    /// <summary>The eighteen, in slot-then-flavour order (the order the Sigils tab renders).</summary>
    public static readonly string[] AllSigilIds =
    {
        HolyPowerSigil,  FurySigil,  SoulSigil,
        BodySigil,    MageAttackSigil,     FocusSigil,
        HolyProtectionSigil, DuelSigil, SpiritSigil,
        AegisSigil,   MageDefenceSigil,    AgilitySigil,
        HolySupportSigil, FortitudeSigil, ImmortalitySigil,
        CriticalProtectionSigil,   MageSupportSigil,    AimSigil,
    };

    private static readonly Dictionary<string, (SigilFlavour Flavour, SigilSlot Slot)> SigilTable =
        new()
        {
            [HolyPowerSigil]   = (SigilFlavour.Healer,  SigilSlot.Attack),
            [HolyProtectionSigil]  = (SigilFlavour.Healer,  SigilSlot.Defence),
            [HolySupportSigil]  = (SigilFlavour.Healer,  SigilSlot.Support),
            [FurySigil]  = (SigilFlavour.Warrior, SigilSlot.Attack),
            [DuelSigil] = (SigilFlavour.Warrior, SigilSlot.Defence),
            [FortitudeSigil] = (SigilFlavour.Warrior, SigilSlot.Support),
            [SoulSigil]   = (SigilFlavour.Buffer,  SigilSlot.Attack),
            [SpiritSigil]  = (SigilFlavour.Buffer,  SigilSlot.Defence),
            [ImmortalitySigil]  = (SigilFlavour.Buffer,  SigilSlot.Support),
            [BodySigil]     = (SigilFlavour.Tank,    SigilSlot.Attack),
            [AegisSigil]    = (SigilFlavour.Tank,    SigilSlot.Defence),
            [CriticalProtectionSigil]    = (SigilFlavour.Tank,    SigilSlot.Support),
            [MageAttackSigil]     = (SigilFlavour.Mage,    SigilSlot.Attack),
            [MageDefenceSigil]    = (SigilFlavour.Mage,    SigilSlot.Defence),
            [MageSupportSigil]    = (SigilFlavour.Mage,    SigilSlot.Support),
            [FocusSigil]    = (SigilFlavour.Rogue,   SigilSlot.Attack),
            [AgilitySigil]   = (SigilFlavour.Rogue,   SigilSlot.Defence),
            [AimSigil]   = (SigilFlavour.Rogue,   SigilSlot.Support),
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
                // 🔴 NO `ExclusiveGroup` AND NO `Replaces` SINCE `BL-250` §1 (2026-09-17). The three
                // slots stopped being Attack / Defence / Support and became three IDENTICAL slots —
                // *"any three of the eighteen, so long as you have unlocked them"* — so neither an
                // exclusion group nor a replace list has anything left to say. What limits you now is
                // the COUNT (three) and the GROUPS your subclasses have unlocked, both enforced in
                // `GameLoopService.HandleLearnSkill`.
                // ⚠ `Replaces` would have been actively destructive here, not merely stale: it means
                // "gone for good", so a second Attack sigil would have deleted the first.
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
            Sigil(HolyPowerSigil, "Holy Power Sigil", SigilFlavour.Healer, SigilSlot.Attack,
                "Your healing is 5% stronger.",
                passive: new PassiveEffect(HealPowerPct: 0.05f)),

            Sigil(HolyProtectionSigil, "Holy Protection Sigil", SigilFlavour.Healer, SigilSlot.Defence,
                "Debuffs that contest your Spirit are 10% less likely to land on you.",
                passive: new PassiveEffect(CcResistMagical: 0.10f)),

            Sigil(HolySupportSigil, "Holy Support Sigil", SigilFlavour.Healer, SigilSlot.Support,
                "When you are hit, a 5% chance to mend 2% of your maximum HP.",
                procChance: 0.05f, procOnDamaged: true, procCooldownTicks: 50,
                procRung: SigilHolyMend),

            // ═══ WARRIOR ═════════════════════════════════════════════════════════════════════════
            Sigil(FurySigil, "Fury Sigil", SigilFlavour.Warrior, SigilSlot.Attack,
                "When you attack, a 3% chance to swing 30% faster for 15 seconds.",
                procChance: 0.03f, procCooldownTicks: 200, procRung: SigilFuryHaste),

            Sigil(DuelSigil, "Duel Sigil", SigilFlavour.Warrior, SigilSlot.Defence,
                "You take 5% less damage from other players.",
                // NEGATIVE = less taken, the same convention the armour sets' PvP clause uses.
                passive: new PassiveEffect(PvpDamageTakenPct: -0.05f)),

            Sigil(FortitudeSigil, "Fortitude Sigil", SigilFlavour.Warrior, SigilSlot.Support,
                "Debuffs that contest your Constitution are 5% less likely to land on you.",
                passive: new PassiveEffect(CcResistPhysical: 0.05f)),

            // ═══ BUFFER ══════════════════════════════════════════════════════════════════════════
            Sigil(SoulSigil, "Soul Sigil", SigilFlavour.Buffer, SigilSlot.Attack,
                "Maximum MP +10%.",
                passive: new PassiveEffect(MaxMpPct: 0.10f)),

            Sigil(SpiritSigil, "Spirit Sigil", SigilFlavour.Buffer, SigilSlot.Defence,
                "MP regeneration +10%.",
                passive: new PassiveEffect(MpRegenPct: 0.10f)),

            Sigil(ImmortalitySigil, "Immortality Sigil", SigilFlavour.Buffer, SigilSlot.Support,
                "When you are hit, a 3% chance to become immortal for 5 seconds — your HP cannot fall, "
                + "and cannot be healed either.",
                procChance: 0.03f, procOnDamaged: true, procCooldownTicks: 200,
                procRung: SigilImmortality),

            // ═══ TANK ════════════════════════════════════════════════════════════════════════════
            Sigil(BodySigil, "Body Sigil", SigilFlavour.Tank, SigilSlot.Attack,
                "Maximum HP +10%.",
                passive: new PassiveEffect(MaxHpPct: 0.10f)),

            Sigil(AegisSigil, "Aegis Sigil", SigilFlavour.Tank, SigilSlot.Defence,
                "When you are hit, a 3% chance to raise both defences by 25% for 15 seconds.",
                procChance: 0.03f, procOnDamaged: true, procCooldownTicks: 200,
                procRung: SigilAegisGuard),

            Sigil(CriticalProtectionSigil, "Critical Protection Sigil", SigilFlavour.Tank, SigilSlot.Support,
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
            Sigil(FocusSigil, "Focus Sigil", SigilFlavour.Rogue, SigilSlot.Attack,
                "When you attack, a 3% chance to raise your critical rate by 5 points for 15 seconds.",
                procChance: 0.03f, procCooldownTicks: 200, procRung: SigilFocusEdge),

            Sigil(AgilitySigil, "Agility Sigil", SigilFlavour.Rogue, SigilSlot.Defence,
                "Evasion +3, and spells aimed at you fail 3 points more often.",
                passive: new PassiveEffect(Evasion: 3, MagicEvasion: 3f)),

            Sigil(AimSigil, "Aim Sigil", SigilFlavour.Rogue, SigilSlot.Support,
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

    /// <summary>🔴 <b>GONE — `BL-250` §1, 2026-09-17.</b> This generated his REPLACES column: every OTHER
    /// flavour's SAME slot, five ids, which is what made the three slots an exclusion axis.
    ///
    /// <para>The slots are <b>three identical slots</b> now — any three of the eighteen — so there is
    /// nothing for a sigil to replace. ⚠ Leaving it would have been the worst kind of leftover, because
    /// <c>Replaces</c> means <b>gone for good</b> in this engine, not "swapped": committing a Warrior
    /// Attack sigil would have silently DESTROYED a Mage Attack sigil you had paid for, in a system whose
    /// whole point is that you may now hold both.</para>
    ///
    /// <para>🔴 <b>To restore the one-per-slot rule</b> if a playtest asks for it: put this method and
    /// the <c>ExclusiveGroup</c> line in <c>Sigil()</c> back. The three <c>SigilGroup*</c> constants and
    /// <see cref="SigilGroup"/> are kept for exactly that, and because the reset NPC's list is keyed on
    /// a skill being resettable rather than on this.</para></summary>
    // private static string[] SigilReplaces(SigilFlavour flavour, SigilSlot slot) { … }
}
