namespace Game.Shared;

/// <summary>Cross-class skills shared by many classes / not tied to one discipline:
/// armor-weight masteries, the combat-training passives, the class-identity "sure"
/// floor passives (and their auto-grant mapping), buff-potion buffs, the HP Boost
/// line, and Wind Walk. Class-defining combat skills live in their own files.</summary>
public static partial class SkillCatalog
{
    // ---- Base MAGE armor mastery (learnable PASSIVE, per-level StatMods data; applied
    //      in RecomputeDerived by the worn body weight). Fighters get FighterArmorMastery
    //      instead; 2nd classes REPLACE these with their archetype mastery. ----
    public const string MasteryRobe  = "mastery_robe";
    // ---- Weapon Proficiency: all mages auto-learn this at level 1. While NOT wielding a mage-trained
    //      weapon (sword or blunt — incl. wand/staff), casting speed is halved. Handled in Entity by
    //      weapon type, not a StatMod. ----
    public const string WeaponProficiency = "weapon_proficiency";   // RETIRED — see SpellcasterMastery
    public const string SpellcasterMastery = "spellcaster_mastery";  // armor weight + weapon type, one rule
    // ---- Divine Focus: clerics (Healer 2nd class) auto-learn Lv1 at 20; the Warchanter discipline
    //      upgrades to Lv2 at 40. While NO magic weapon is equipped, healing OUTPUT is scaled: Lv1 ×0.5
    //      (pure healers must wield a magic weapon), Lv2 ×0.75 (buffers stay relevant in fighter gear). ----
    public const string DivineFocus = "divine_focus";
    // ---- Novice's Grace: DISPLAY-ONLY passive, auto-shown below GameConstants.DeathExpPenaltyMinLevel so a
    //      newbie can SEE that death costs no exp yet. No mechanical effect (the level check in
    //      ApplyDeathExpPenalty does the work); auto-removed once they reach the level. ----
    public const string NoviceGrace = "novice_grace";
    // ---- Combat "training" passives, auto-granted at level 40 (war rune/spell rune
    //      stand-in). Doubling the atk STAT gives ×2 physical (linear) but ×1.414
    //      magic (√mAtk) — the war rune/spell rune ratio. ----
    public const string PhysicalTraining = "physical_training";  // multi-level (9): +10%…+100% atk
    public const string SpiritTraining   = "spirit_training";    // multi-level (9): +atk + cast speed
    // ---- RUNE buffs — the item-driven REPLACEMENT for the training passives above. A held rune
    //      applies one of these while it's in the main inventory and unexpired; the reconciliation loop
    //      keeps it in sync with the item. War Rune = +100% P.Atk (×2 physical); Spell Rune = +41%
    //      effective M.Atk (×1.414 magic) + a flat +40 cast, exactly the old passive's numbers. ----
    public const string WarRuneBuff   = "rune_war";
    public const string SpellRuneBuff = "rune_spell";

    /// <summary>Is this buff granted by a held RUNE rather than cast? Such buffs are owned by the
    /// reconciliation loop, which re-derives them from the rune item (and its expiry) — so they must
    /// never be saved/restored as ordinary buffs, or login would apply them a second time.</summary>
    public static bool IsRuneBuff(string skillId) =>
        skillId == WarRuneBuff || skillId == SpellRuneBuff;
    // ---- Class identity "sure" floor passives — now ONE multi-level skill each
    //      (auto-granted at the class-change milestone, level = tier 1/2/3). The floor
    //      VALUES live in the SkillDef Levels, not in code. See FloorPassiveFor. ----
    public const string EvadeMastery = "evade_mastery"; // Rogue   10/20/30%
    // (`reflexes` — the Archer floor — deleted 2026-08-07: no class carries Archetype.Archer after
    //  the archer→rogue merge, so it was granted to nobody. See the CommonSkills() note.)
    public const string Precision    = "precision";     // Warrior 10/20/30% hit floor
    public const string AntiMagic    = "anti_magic";    // Tank    10/15/20% magic fizzle
    // ---- HEALING-potion skills. The potion ITEM names one of these; the SKILL does the
    //      healing and consumes the item (its ConsumableId). Everything is a skill — only
    //      what GRANTS it differs. The HoT ones are ordinary buffs, so they show on the buff
    //      bar and get "stronger cancels weaker" free from BuffKey + Rank (which is exactly
    //      what the old bespoke PotionRarity/PotionEffectTicks state did by hand). ----
    public const string PotHealMinor   = "pot_heal_minor";     // Common HoT tier
    public const string PotHeal        = "pot_heal";           // Uncommon HoT tier
    public const string PotHealGreater = "pot_heal_greater";   // Rare HoT tier
    public const string PotHealInstant = "pot_heal_instant";   // Instant %-heal panic potion
    // ======================================================================================
    //  BUFF LADDERS (docs/design/BuffLadders.md) — the SINGLE buffs of the speed group.
    //
    //  Four families, one number line each: move / cast / evasion / attack speed. Every source
    //  of an effect — a potion, a scroll, one rung of the cleric's improved Speed — applies the
    //  SAME single-buff skill, so they can never stack: they compete on the family's BuffKey by
    //  Rank (1/2/3 = the Common/Uncommon/Rare rung), which is what ApplyBuff already arbitrates.
    //  These are never learned and never cast directly; they are applied as CHILDREN.
    //  ONE FAMILY = ONE MODIFIER MODE (all flat or all percent) or the ranking would lie.
    // ======================================================================================
    public const string FamMove = "spd_move";   // Swift    — flat move speed
    public const string FamCast = "spd_cast";   // Alacrity — % cast speed
    public const string FamEva  = "spd_eva";    // Agility  — flat evasion
    public const string FamAs   = "spd_as";     // Haste    — % attack speed

    public const string BuffSwiftC = "buff_swift_c";        // +15 move
    public const string BuffSwiftU = "buff_swift_u";        // +20 move
    public const string BuffSwiftR = "buff_swift_r";        // +33 move
    public const string BuffAlacrityC = "buff_alacrity_c";  // +15% cast
    public const string BuffAlacrityU = "buff_alacrity_u";  // +23% cast
    public const string BuffAlacrityR = "buff_alacrity_r";  // +30% cast
    public const string BuffAgilityC = "buff_agility_c";    // +1 evasion
    public const string BuffAgilityU = "buff_agility_u";    // +2 evasion
    public const string BuffAgilityR = "buff_agility_r";    // +4 evasion
    public const string BuffHasteC = "buff_haste_c";        // +15% attack speed
    public const string BuffHasteU = "buff_haste_u";        // +23% attack speed
    public const string BuffHasteR = "buff_haste_r";        // +33% attack speed

    // ---- The consumables that grant them. A potion and a scroll of the SAME tier grant the
    //      SAME single buff and differ only in duration (20 min vs 1 h) and cast (instant vs 1s):
    //      drinking a potion over an equal-tier scroll is refused, not silently eaten. ----
    public const string PotSwiftC = "pot_swift_c";
    public const string PotSwiftU = "pot_swift_u";
    public const string PotSwiftR = "pot_swift_r";
    public const string PotAlacrityC = "pot_alacrity_c";
    public const string PotAlacrityU = "pot_alacrity_u";
    public const string PotAlacrityR = "pot_alacrity_r";
    public const string PotAgilityC = "pot_agility_c";
    public const string PotAgilityU = "pot_agility_u";
    public const string PotAgilityR = "pot_agility_r";
    public const string PotHasteC = "pot_haste_c";
    public const string PotHasteU = "pot_haste_u";
    public const string PotHasteR = "pot_haste_r";
    public const string ScrSwiftC = "scr_swift_c";
    public const string ScrSwiftU = "scr_swift_u";
    public const string ScrSwiftR = "scr_swift_r";
    public const string ScrAlacrityC = "scr_alacrity_c";
    public const string ScrAlacrityU = "scr_alacrity_u";
    public const string ScrAlacrityR = "scr_alacrity_r";
    public const string ScrAgilityC = "scr_agility_c";
    public const string ScrAgilityU = "scr_agility_u";
    public const string ScrAgilityR = "scr_agility_r";
    public const string ScrHasteC = "scr_haste_c";
    public const string ScrHasteU = "scr_haste_u";
    public const string ScrHasteR = "scr_haste_r";

    // ---- DASH — deliberately OUTSIDE the spd_move family (owner 2026-07-31). A 15-second burst
    //      on a 1-minute reuse, six rarities up to +60 move, no scroll. If it shared spd_move it
    //      would evict your 1-hour Swift scroll and hand it back fifteen seconds later.
    //
    //      G5 (playtest-18): the rogue's SPRINT joins this family — "Dash potion is the same as
    //      sprint skill just weaker (longer cd and weaker value) … same effects or weaker are
    //      removed and replaced by the new effect". One ordered ladder by MAGNITUDE, so the two
    //      lines interleave instead of stacking:
    //
    //          rank 1  Dash C      +15      rank 5  Dash E      +50
    //          rank 2  Dash U      +30      rank 6  Dash L      +55
    //          rank 3  Sprint L1   +40      rank 7  Dash M      +60
    //          rank 4  Dash R      +45      rank 8  Sprint L2   +60
    //
    //      That gives exactly his two sentences: Sprint L1 replaces Dash C/U (and is refused under
    //      anything above it), Sprint L2 replaces everything including Sprint L1. Sprint L2 sits
    //      ABOVE Dash M at the same +60 on purpose — a class skill you levelled must not be
    //      overridable by a bottle, which is the same rule a group buff follows.
    //
    //      ⚠ Sprint's two levels have DIFFERENT ranks, and Rank lives on the SkillDef, not on
    //      SkillLevel. So Sprint is authored as a one-child WRAPPER whose level picks the child —
    //      the same machinery a potion uses. The child is what lands and what carries the rank.
    public const string FamDash = "dash";
    public const string BuffDashC = "buff_dash_c";
    public const string BuffDashU = "buff_dash_u";
    public const string BuffDashR = "buff_dash_r";
    public const string BuffDashE = "buff_dash_e";
    public const string BuffDashL = "buff_dash_l";
    public const string BuffDashM = "buff_dash_m";
    public const string PotDashC = "pot_dash_c";
    public const string PotDashU = "pot_dash_u";
    public const string PotDashR = "pot_dash_r";
    public const string PotDashE = "pot_dash_e";
    public const string PotDashL = "pot_dash_l";
    public const string PotDashM = "pot_dash_m";
    // The two rungs the ROGUE's Sprint hands out (G5). Named "Sprint" so the buff square says which
    // of the two lines put it there, even though they share the family.
    public const string BuffSprint1 = "buff_sprint_1";
    public const string BuffSprint2 = "buff_sprint_2";
    // (`hp_boost` — deleted 2026-08-07 with the God layer, playtest-19 `0b`.)
    // ============================ TEST ONLY — DELETE ME ============================
    // A 1000-power heal, auto-granted to EVERY character at level 76, purely to calibrate the heal
    // formula against the owner's target (a 1000-power heal should land ~2000 for a healer at 76).
    // It is deliberately given to fighters too, so the tank-vs-healer gap can be read directly.
    // REMOVE: this const, TestHealSkill() below, its line in CommonSkills(), and the auto-grant in
    // GameLoopService.AutoLearnCoreSkills. Search "TEST ONLY" to find all four.
    public const string TestHeal = "test_heal";
    // Two debug damage skills: they use Flat=TestSkillPower, Mod=TestSkillMod from the Debug panel, so the
    // owner can read the {Flat, Mod} curve live. Auto-granted with TestHeal. Search "TEST ONLY" to remove.
    public const string TestPhysSkill  = "test_phys";
    public const string TestMagicSkill = "test_magic";
    // ==============================================================================
    // ---- "Class Balance" — one zeroed passive per class, auto-granted. See ClassBalanceFor. ----
    public const string BalanceTank    = "class_balance_tank";
    public const string BalanceWarrior = "class_balance_warrior";
    public const string BalanceRogue   = "class_balance_rogue";
    public const string BalanceArcher  = "class_balance_archer";
    public const string BalanceNuker   = "class_balance_nuker";
    public const string BalanceHealer  = "class_balance_healer";
    public const string BalanceFighter = "class_balance_fighter";   // base class, pre-2nd
    public const string BalanceMage    = "class_balance_mage";      // base class, pre-2nd
    // ---- Universal "Return" line: teleport to the nearest town. All auto-granted; the scroll
    //      variants require (and consume) their scroll item. Fixed cast + fixed cooldown. ----
    public const string ReturnSkill          = "return_town";        // 30s cast, 5min cd, fragile
    public const string ScrollReturnSkill    = "use_scroll_return";  // 10s cast, needs a scroll
    public const string ScrollReturnUltSkill = "use_scroll_return_ult"; // ~0.4s, needs an ult scroll
    public const string ScrollResurrectSkill    = "use_scroll_resurrect";     // 10s ally-res, 0% exp back
    public const string ScrollResurrectUltSkill = "use_scroll_resurrect_ult"; // 0.5s ally-res, 100% exp back
    public const string AngelsProtection        = "angels_protection";        // noblesse: keep buffs on death

    // ======================================================================================
    //  BUFF-LADDER factories (docs/design/BuffLadders.md).
    // ======================================================================================

    /// <summary>A SINGLE buff — one effect, one family key, one rung. Never learned and never cast
    /// on its own: it is applied as a CHILD, by a potion, a scroll or one level of an improved
    /// group buff, and the applier supplies the duration (hence DurationTicks 0 here).</summary>
    private static SkillDef SingleBuff(string id, string name, string family, int rank,
        SkillEffect effect, EffectMagnitude mag, string desc) => new(
        id, name, BaseClass.Fighter, effect,
        MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
        BuffKey: family, Rank: rank, Magnitudes: new[] { mag },
        Category: SkillCategory.Buff, Description: desc);

    /// <summary>A consumable that grants ONE single buff: the item's skill, which owns the
    /// DURATION, the cast time and the reuse, and applies the child. Potion and scroll of a tier
    /// share the child (same family, same rank) and differ only in how long they last — so drinking
    /// a potion on top of an equal-tier scroll is refused instead of quietly wasting it.</summary>
    private static SkillDef ConsumableBuff(string id, string name, string child, SkillEffect effect,
        int durationTicks, int castTicks, int cooldownTicks, string desc) => new(
        id, name, BaseClass.Fighter, effect,
        MpCost: 0, CastTicks: castTicks, CooldownTicks: cooldownTicks, Range: 0, Power: 0,
        DurationTicks: durationTicks, ChildBuffs: new[] { child },
        Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable,
        TargetMode: TargetMode.SelfOnly, Description: desc);

    private const int PotionBuffTicks = 12000;   // 20 min
    private const int ScrollBuffTicks = 36000;   // 1 hour

    private static SkillDef Potion(string id, string name, string child, SkillEffect effect, string what) =>
        ConsumableBuff(id, name, child, effect, PotionBuffTicks, castTicks: 0, cooldownTicks: 10,
            desc: $"{what} for 20 minutes.");

    private static SkillDef Scroll(string id, string name, string child, SkillEffect effect, string what) =>
        ConsumableBuff(id, name, child, effect, ScrollBuffTicks, castTicks: 10, cooldownTicks: 10,
            desc: $"{what} for 1 hour.");

    /// <summary>Dash: a 15-second sprint on a 1-minute reuse, on its OWN family — it must never
    /// join spd_move, or it would evict an hour-long Swift scroll and give it back 15s later.</summary>
    private static SkillDef DashPotion(string id, string name, string child, int move) =>
        ConsumableBuff(id, name, child, SkillEffect.BuffMoveSpeed, durationTicks: 150,
            castTicks: 0, cooldownTicks: 600, desc: $"+{move} Move Speed for 15 seconds.");

    // ---- Multi-level PASSIVE factory: a pure passive whose levels each carry a
    //      PassiveEffect (the floor/lean value for that level). ----
    private static SkillDef LeveledPassive(string id, string name, BaseClass cls, string desc,
        params PassiveEffect[] perLevel) => new(
        id, name, cls, SkillEffect.None,
        MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
        Category: SkillCategory.Passive, SpCost: 0, Description: desc,
        Levels: perLevel.Select(p => new SkillLevel(Passive: p)).ToArray());

    // Combat-training passive: 9 levels, +10%…+80% then +100% attack (the war rune/
    // spell rune stand-in). At max level the +100% atk = ×2 P.Atk/M.Atk, which is exactly
    // what a rune does: ×2 physical damage (linear), ×1.414 magic (√mAtk).
    // castSpeedFlat mirrors the real spell rune bonus: a FLAT +40 to the cast stat. It used
    // to be a 0.40 PERCENT, applied as a time cut (×0.6 time = +67% speed), which compounded
    // with WIT/gear/buffs and inflated a buffed L40 mage to ~2200 against the 1999 cap.
    /// <param name="magic">A MAGE's training (spell runes) — boosts M.Atk. A fighter's (war runes)
    /// boosts P.Atk. This used to be one channel-blind <c>AttackPct</c>, which applies to BOTH
    /// channels — so a fighter's PHYSICAL conditioning was doubling his MAGIC attack. That is what
    /// let a level-76 tank heal almost as hard as a healer (his M.Atk was silently ×2), and it made
    /// the whole "a caster weapon makes a healer" rule leak. Now channel-specific.</param>
    private static SkillDef TrainingPassive(string id, string name, BaseClass cls, bool magic,
        float castSpeedFlat, string desc)
    {
        float[] atk = { 0.10f, 0.20f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 1.00f };
        return new(id, name, cls, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 0, Description: desc,
            Levels: atk.Select(p => new SkillLevel(
                Passive: magic
                    // MagAtkPct is stored as the EFFECTIVE magic % (it gets squared in RecomputeDerived to
                    // cancel the √), so stored value = description = effect. √(1+p)-1 reproduces the old
                    // spell rune dampening exactly: physical +100% → magic +41%, but the number now READS 41%.
                    ? new PassiveEffect(MagAtkPct: MathF.Sqrt(1f + p) - 1f, CastSpeedFlat: castSpeedFlat)
                    : new PassiveEffect(PhysAtkPct: p))).ToArray());
    }

    /// <summary>The "Class Balance" passive for a class — auto-granted, always level 1.
    /// It does NOTHING today (an all-zero PassiveEffect). It exists as the tuning HOOK that
    /// replaced the hardcoded per-archetype basic-attack multiplier: if a tank later turns
    /// out to hit too softly in PvE, raise PveBasicDamagePct on its balance passive rather
    /// than adding a coefficient back into StatCalculator. Damage stays formula + weapon,
    /// and per-class deviation stays DATA (see docs — stats-via-skills).</summary>
    public static string ClassBalanceFor(Archetype? archetype, BaseClass cls) => archetype switch
    {
        Archetype.Tank    => BalanceTank,
        Archetype.Warrior => BalanceWarrior,
        Archetype.Rogue   => BalanceRogue,
        Archetype.Archer  => BalanceArcher,
        Archetype.Nuker   => BalanceNuker,
        Archetype.Healer  => BalanceHealer,
        _ => cls == BaseClass.Mage ? BalanceMage : BalanceFighter,   // base class, pre-2nd
    };

    /// <summary>Every Class Balance id. ⚠ While the hook is commented out (owner, 2026-08-07) this
    /// is the CLEANUP list: AutoLearnCoreSkills strips these from anyone who was granted one before
    /// the ruling, because a learned id with no <see cref="SkillDef"/> behind it has nothing to
    /// render. Uncommenting the defs in CommonSkills() and the grant makes it a no-op again.</summary>
    public static readonly string[] ClassBalanceIds =
    {
        BalanceTank, BalanceWarrior, BalanceRogue, BalanceArcher,
        BalanceNuker, BalanceHealer, BalanceFighter, BalanceMage,
    };

    /// <summary>A Class Balance passive: one level, an all-zero <see cref="PassiveEffect"/>.
    /// Fill in fields here (PveBasicDamagePct, PvpMagicDamagePct, AttackPct, …) to tune a class.
    /// ⚠ Currently unreferenced on purpose — see the commented block in CommonSkills().</summary>
    private static SkillDef BalancePassive(string id, string name, BaseClass cls) => new(
        id, name, cls, SkillEffect.None,
        MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
        Category: SkillCategory.Passive, SpCost: 0,
        Description: "Class balance. Reserved for tuning — currently no effect.",
        Levels: new[] { new SkillLevel(Passive: new PassiveEffect()) });

    /// <summary>The identity floor passive an archetype receives at its current class
    /// tier (milestones 20/40/76) — as (skill id, skill LEVEL), or null. Granted in
    /// AutoLearnCoreSkills. The floor VALUES live in the SkillDef Levels, not in code.
    ///
    /// ⚠ The ROGUE's ladder is NOT the plain 20/40/76 the others use (owner, 2026-08-07, refining
    /// playtest-19 M7). It is tied to the CLASS CHANGE, not to the level number:
    ///
    ///   Lv1 @20 — every rogue, on the 2nd class change. The 10% floor.
    ///   Lv2 @40 — <b>only on taking a MELEE discipline</b> (Phantom / Venomweaver / Nullblade).
    ///             A rogue who is level 40+ but has not chosen yet stays at Lv1, and a RANGED
    ///             discipline (Sharpshooter / Trapper / Hunter) stays at Lv1 forever —
    ///             *"the archer should not have evasion mastery after 40 .. the 10% are ok"*.
    ///   Lv3     — 🔴 NOBODY, and that is deliberate: its milestone is the <b>4th class change,
    ///             WHICH DOES NOT EXIST YET</b>. 76 is only a level; granting a rung there would
    ///             hand out a 3rd-class-sized bonus for no class change at all. When the 4th tier
    ///             lands, gate Lv3 on holding it — not on <c>level >= 76</c>.
    ///
    /// Since the archer→rogue merge, ONE 2nd class covers bow and dagger, so the discipline is the
    /// only thing that can tell a bow rogue from a dagger one — hence the parameter.
    ///
    /// ⚠ Warrior/Tank still use the plain 20/40/76 curve, so `precision` and `anti_magic` DO still
    /// grant a Lv3 at 76. The owner ruled on the rogue's; the same "76 is not a class change"
    /// argument applies to those two and is <b>owed back to him</b> before changing them.
    ///
    /// ⚠ 2026-08-10 — ALL THREE ARE NOW AUTHORED IN THE CSVs (owner: *"Put evasion mastery/anymagic/
    /// precision inside the csv for the warrior/tank/rogue"*), as a level-20 row in
    /// `docs/data/classes_skills_csv/{rogue,tank,warrior} 20-35.csv`. The CSV is the AUTHORITY on
    /// their numbers — change a floor there first, then mirror it into the Levels below. They stay
    /// auto-granted from here (SP 0) rather than bought, which is why the CSV rows carry SP 0.
    /// ⚠ The tank's CSV also has a *different* skill called "Tank Anti-Magic" (m.def +25/+45) —
    /// do not conflate the two: that one is a stat, this one is the fizzle floor.</summary>
    public static (string Id, int Level)? FloorPassiveFor(Archetype? archetype, int level,
        Discipline? discipline = null)
    {
        int tier = level >= 76 ? 3 : level >= 40 ? 2 : level >= 20 ? 1 : 0;
        if (tier == 0) return null;
        return archetype switch
        {
            // The rogue's own ladder — see the block above. Lv2 needs a MELEE discipline in hand;
            // everything else about being level 40, 76 or 90 is irrelevant to it.
            Archetype.Rogue   => (EvadeMastery,
                level >= 40 && discipline is { } d && !Disciplines.IsRanged(d) ? 2 : 1),
            Archetype.Warrior => (Precision, tier),
            Archetype.Tank    => (AntiMagic, tier),
            // Archetype.Archer gets nothing: `reflexes` is deleted and no 2nd class carries
            // Archer any more. A bow character is a Rogue whose discipline is ranged (above).
            // Mages get NO auto magic-fail floor — it comes from their LEARNED Anti-Magic
            // (anti_magic_mage), available to every mage class.
            _ => null
        };
    }

    // ⚠ RESTRUCTURED 2026-08-07 (owner). The old Robe Mastery did TWO jobs at once — it granted the
    // robe's P.Def AND carried the wrong-weight casting penalty — which is why every 2nd-class mage
    // mastery had to re-declare that same penalty, and why replacing it silently deleted the penalty
    // along with the bonus. Split in two:
    //   • ROBE ARMOR MASTERY (this table, id `mastery_robe`) = the BONUS only. No penalties at all.
    //   • SPELLCASTER MASTERY (below) = the PENALTY only, and it is never replaced, so it applies to
    //     every mage at every level. His words: *"Robe mastery is only to cut wrong armor weights."*
    // A robed mage now collects BOTH (armor masteries stack — see Entity.RecomputeDerived).
    private static readonly ArmorMasteryProfile[] MageRobeLevels = new[]
    {
        new ArmorMasteryProfile(Robe: new StatMods(PDef: 7)),
        new ArmorMasteryProfile(Robe: new StatMods(PDef: 9)),
    };

    /// <summary>SPELLCASTER MASTERY, the armor half: a ROBE is the caster's weight (+20% MP regen);
    /// light, heavy and NOTHING all halve casting and attack speed. Auto-granted at level 1 and
    /// NEVER replaced — it is the one place the wrong-weight penalty lives, so a 2nd-class mastery
    /// can be pure bonus. The cleric's light-armor row is authored to cancel this exact ×0.50.
    /// (The WEAPON half is not data: it drives MagicWeaponPenaltyMult / CastSpeedPenaltyMult /
    /// MagicFailResist, which have no StatMods field — it stays in Entity.RecomputeDerived.)</summary>
    private static readonly ArmorMasteryProfile[] SpellcasterLevels = new[]
    {
        new ArmorMasteryProfile(
            Robe:  new StatMods(MpRegenPct: 0.2f),
            Light: new StatMods(AtkSpeedPct: -0.5f, CastSpeedPct: -0.5f),
            Heavy: new StatMods(AtkSpeedPct: -0.5f, CastSpeedPct: -0.5f),
            None:  new StatMods(AtkSpeedPct: -0.5f, CastSpeedPct: -0.5f)),
    };

    private static SkillDef[] CommonSkills() => new SkillDef[]
    {
        // ======================== TEST ONLY — DELETE ME ========================
        // Power 1000, so the heal formula can be read straight off the screen:
        //   heal = power × √M.Atk / HealK(15)
        // Expected at level 76, fully geared: HEALER (t76 staff, M.Atk ~900) ≈ 2000.
        //                                     TANK  (t76 2H sword)            ≈ 1193 @ OffChannel 0.6
        //                                                                     ≈  689 @ OffChannel 0.2
        // Cast/cooldown kept short so it's quick to spam-test. Costs no MP.
        new(TestHeal, "TestHeal", BaseClass.Fighter, SkillEffect.Heal,
            MpCost: 0, CastTicks: 20, CooldownTicks: 20, Range: 600, Power: 1000,
            Category: SkillCategory.Heal,
            Description: "TEST ONLY. Flat heal, power 1000. Used to calibrate the heal formula "
                       + "(heal = power × √M.Atk / 15). Remove before release."),
        // TEST ONLY: two debug damage skills. Power 0 in the def — the server overrides Flat/Mod with the
        // Debug-panel TestSkillPower / TestSkillMod at cast time, so you can read the {Flat, Mod} curve live.
        new(TestMagicSkill, "TestMagic", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 0, CastTicks: 10, CooldownTicks: 10, Range: 900, Power: 0,
            Category: SkillCategory.Magic,
            Description: "TEST ONLY. Magic hit using the Debug TestSkillPower (Flat) + TestSkillMod (Mod): "
                       + "91·(Flat + Mod·√M.Atk)/mDef. Cast 1s. Remove before release."),
        new(TestPhysSkill, "TestPhys", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 0, CastTicks: 5, CooldownTicks: 10, Range: 600, Power: 0,
            Category: SkillCategory.Physical,
            Description: "TEST ONLY. Physical hit using the Debug TestSkillPower (Flat) + TestSkillMod (Mod): "
                       + "77·(Flat + Mod·P.Atk)/def. Cast 0.5s. Remove before release."),
        // ======================================================================

        // ----- HEALING potions, as skills. Each consumes its own potion (ConsumableId) and
        //       casts instantly (CastTicks 0). The shared "one potion per 30s" rule stays an
        //       ITEM property (PotionCooldownTicks) — it's a rule about drinking, not about
        //       the effect. Same BuffKey/Rank as the buff potions: a Greater cancels a Minor. -----
        // FLAT heal-over-time tiers (owner, 2026-07-23): flat HP/s, not %, so a tier stays relevant at
        // its level band. Shared BuffKey "potion_heal" + Rank means a higher tier REMOVES a lower one's
        // effect and a lower can't be drunk while a higher runs (UsePotion refuses it). The per-potion
        // DRINK cooldown lives on the ITEM (PotionCooldownTicks), independent per tier.
        new(PotHealMinor, "Common Healing", BaseClass.Fighter, SkillEffect.HealOverTime,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 150, BuffKey: "potion_heal", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.HealOverTime, 20f, ModifierMode.Flat) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable,
            Description: "Restores 20 HP per second for 15s."),
        new(PotHeal, "Uncommon Healing", BaseClass.Fighter, SkillEffect.HealOverTime,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 150, BuffKey: "potion_heal", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.HealOverTime, 70f, ModifierMode.Flat) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable,
            Description: "Restores 70 HP per second for 15s."),
        new(PotHealGreater, "Rare Healing", BaseClass.Fighter, SkillEffect.HealOverTime,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "potion_heal", Rank: 3,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.HealOverTime, 150f, ModifierMode.Flat) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable,
            Description: "Restores 150 HP per second for 30s."),
        // The one % potion kept: an INSTANT panic heal that scales with your HP pool and (being %)
        // ignores heal-received debuffs — the one thing that saves you while debuffed. 1-min cooldown.
        new(PotHealInstant, "Instant Healing", BaseClass.Fighter, SkillEffect.Heal,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Heal, 0.30f) },
            Category: SkillCategory.Heal,
            Description: "Instantly restores 30% of max HP."),

        // ----- RUNE buffs. Applied/kept by the rune reconciliation while a matching rune sits in the
        //       main inventory unexpired; its remaining time is driven by the item's wall-clock expiry, so
        //       DurationTicks here is only the nominal apply value (the loop overwrites TicksRemaining). -----
        new(WarRuneBuff, "War Rune", BaseClass.Fighter, SkillEffect.BuffPhysAtk,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 36000, BuffKey: "rune_war", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffPhysAtk, 1.00f) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable,
            Description: "War Rune: +100% P.Atk (physical damage) while the rune is held."),
        new(SpellRuneBuff, "Spell Rune", BaseClass.Mage, SkillEffect.BuffMagAtk | SkillEffect.BuffCastSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 36000, BuffKey: "rune_spell", Rank: 1,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffMagAtk, 0.414f),                       // +41% EFFECTIVE M.Atk = ×1.414 magic
                new(SkillEffect.BuffCastSpeed, 40, ModifierMode.Flat),     // flat +40 cast stat (not %, per the old passive)
            },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable,
            Description: "Spell Rune: +magic damage and cast speed while the rune is held."),

        // ================== BUFF LADDERS — the single buffs and their consumables ==================
        //  See docs/design/BuffLadders.md. Four families, three rungs each; the improved "Speed"
        //  buff (cleric / NPC buffer) applies these SAME skills as its children, so a potion and a
        //  class buff can never stack — they compete on the family key and the better one wins.
        // ==========================================================================================
        SingleBuff(BuffSwiftC, "Swift", FamMove, 1, SkillEffect.BuffMoveSpeed,
            new(SkillEffect.BuffMoveSpeed, 15, ModifierMode.Flat), "+15 Move Speed."),
        SingleBuff(BuffSwiftU, "Swift", FamMove, 2, SkillEffect.BuffMoveSpeed,
            new(SkillEffect.BuffMoveSpeed, 20, ModifierMode.Flat), "+20 Move Speed."),
        SingleBuff(BuffSwiftR, "Swift", FamMove, 3, SkillEffect.BuffMoveSpeed,
            new(SkillEffect.BuffMoveSpeed, 33, ModifierMode.Flat), "+33 Move Speed."),

        SingleBuff(BuffAlacrityC, "Alacrity", FamCast, 1, SkillEffect.BuffCastSpeed,
            new(SkillEffect.BuffCastSpeed, 0.15f), "+15% Cast Speed."),
        SingleBuff(BuffAlacrityU, "Alacrity", FamCast, 2, SkillEffect.BuffCastSpeed,
            new(SkillEffect.BuffCastSpeed, 0.23f), "+23% Cast Speed."),
        SingleBuff(BuffAlacrityR, "Alacrity", FamCast, 3, SkillEffect.BuffCastSpeed,
            new(SkillEffect.BuffCastSpeed, 0.30f), "+30% Cast Speed."),

        SingleBuff(BuffAgilityC, "Agility", FamEva, 1, SkillEffect.BuffEvasion,
            new(SkillEffect.BuffEvasion, 1, ModifierMode.Flat), "+1 Evasion."),
        SingleBuff(BuffAgilityU, "Agility", FamEva, 2, SkillEffect.BuffEvasion,
            new(SkillEffect.BuffEvasion, 2, ModifierMode.Flat), "+2 Evasion."),
        SingleBuff(BuffAgilityR, "Agility", FamEva, 3, SkillEffect.BuffEvasion,
            new(SkillEffect.BuffEvasion, 4, ModifierMode.Flat), "+4 Evasion."),

        SingleBuff(BuffHasteC, "Haste", FamAs, 1, SkillEffect.BuffAtkSpeed,
            new(SkillEffect.BuffAtkSpeed, 0.15f), "+15% Attack Speed."),
        SingleBuff(BuffHasteU, "Haste", FamAs, 2, SkillEffect.BuffAtkSpeed,
            new(SkillEffect.BuffAtkSpeed, 0.23f), "+23% Attack Speed."),
        SingleBuff(BuffHasteR, "Haste", FamAs, 3, SkillEffect.BuffAtkSpeed,
            new(SkillEffect.BuffAtkSpeed, 0.33f), "+33% Attack Speed."),

        // ---- Potions: 20 minutes, instant, 1s reuse. ----
        Potion(PotSwiftC, "Swift Potion (Lesser)", BuffSwiftC, SkillEffect.BuffMoveSpeed, "+15 Move Speed"),
        Potion(PotSwiftU, "Swift Potion",          BuffSwiftU, SkillEffect.BuffMoveSpeed, "+20 Move Speed"),
        Potion(PotSwiftR, "Swift Potion (Greater)",BuffSwiftR, SkillEffect.BuffMoveSpeed, "+33 Move Speed"),
        Potion(PotAlacrityC, "Alacrity Potion (Lesser)", BuffAlacrityC, SkillEffect.BuffCastSpeed, "+15% Cast Speed"),
        Potion(PotAlacrityU, "Alacrity Potion",          BuffAlacrityU, SkillEffect.BuffCastSpeed, "+23% Cast Speed"),
        Potion(PotAlacrityR, "Alacrity Potion (Greater)",BuffAlacrityR, SkillEffect.BuffCastSpeed, "+30% Cast Speed"),
        Potion(PotAgilityC, "Agility Potion (Lesser)", BuffAgilityC, SkillEffect.BuffEvasion, "+1 Evasion"),
        Potion(PotAgilityU, "Agility Potion",          BuffAgilityU, SkillEffect.BuffEvasion, "+2 Evasion"),
        Potion(PotAgilityR, "Agility Potion (Greater)",BuffAgilityR, SkillEffect.BuffEvasion, "+4 Evasion"),
        Potion(PotHasteC, "Haste Potion (Lesser)", BuffHasteC, SkillEffect.BuffAtkSpeed, "+15% Attack Speed"),
        Potion(PotHasteU, "Haste Potion",          BuffHasteU, SkillEffect.BuffAtkSpeed, "+23% Attack Speed"),
        Potion(PotHasteR, "Haste Potion (Greater)",BuffHasteR, SkillEffect.BuffAtkSpeed, "+33% Attack Speed"),

        // ---- Scrolls: the same tiers for an HOUR, but they take a second to read. ----
        Scroll(ScrSwiftC, "Scroll of Swift (Lesser)", BuffSwiftC, SkillEffect.BuffMoveSpeed, "+15 Move Speed"),
        Scroll(ScrSwiftU, "Scroll of Swift",          BuffSwiftU, SkillEffect.BuffMoveSpeed, "+20 Move Speed"),
        Scroll(ScrSwiftR, "Scroll of Swift (Greater)",BuffSwiftR, SkillEffect.BuffMoveSpeed, "+33 Move Speed"),
        Scroll(ScrAlacrityC, "Scroll of Alacrity (Lesser)", BuffAlacrityC, SkillEffect.BuffCastSpeed, "+15% Cast Speed"),
        Scroll(ScrAlacrityU, "Scroll of Alacrity",          BuffAlacrityU, SkillEffect.BuffCastSpeed, "+23% Cast Speed"),
        Scroll(ScrAlacrityR, "Scroll of Alacrity (Greater)",BuffAlacrityR, SkillEffect.BuffCastSpeed, "+30% Cast Speed"),
        Scroll(ScrAgilityC, "Scroll of Agility (Lesser)", BuffAgilityC, SkillEffect.BuffEvasion, "+1 Evasion"),
        Scroll(ScrAgilityU, "Scroll of Agility",          BuffAgilityU, SkillEffect.BuffEvasion, "+2 Evasion"),
        Scroll(ScrAgilityR, "Scroll of Agility (Greater)",BuffAgilityR, SkillEffect.BuffEvasion, "+4 Evasion"),
        Scroll(ScrHasteC, "Scroll of Haste (Lesser)", BuffHasteC, SkillEffect.BuffAtkSpeed, "+15% Attack Speed"),
        Scroll(ScrHasteU, "Scroll of Haste",          BuffHasteU, SkillEffect.BuffAtkSpeed, "+23% Attack Speed"),
        Scroll(ScrHasteR, "Scroll of Haste (Greater)",BuffHasteR, SkillEffect.BuffAtkSpeed, "+33% Attack Speed"),

        // ---- DASH — its own family, so it never touches your Swift buff. 15s, 1 min reuse.
        //      Ranks are the MAGNITUDE order of the whole family, Sprint included (see FamDash). ----
        SingleBuff(BuffDashC, "Dash", FamDash, 1, SkillEffect.BuffMoveSpeed,
            new(SkillEffect.BuffMoveSpeed, 15, ModifierMode.Flat), "+15 Move Speed."),
        SingleBuff(BuffDashU, "Dash", FamDash, 2, SkillEffect.BuffMoveSpeed,
            new(SkillEffect.BuffMoveSpeed, 30, ModifierMode.Flat), "+30 Move Speed."),
        SingleBuff(BuffSprint1, "Sprint", FamDash, 3, SkillEffect.BuffMoveSpeed,
            new(SkillEffect.BuffMoveSpeed, 40, ModifierMode.Flat), "+40 Move Speed."),
        SingleBuff(BuffDashR, "Dash", FamDash, 4, SkillEffect.BuffMoveSpeed,
            new(SkillEffect.BuffMoveSpeed, 45, ModifierMode.Flat), "+45 Move Speed."),
        SingleBuff(BuffDashE, "Dash", FamDash, 5, SkillEffect.BuffMoveSpeed,
            new(SkillEffect.BuffMoveSpeed, 50, ModifierMode.Flat), "+50 Move Speed."),
        SingleBuff(BuffDashL, "Dash", FamDash, 6, SkillEffect.BuffMoveSpeed,
            new(SkillEffect.BuffMoveSpeed, 55, ModifierMode.Flat), "+55 Move Speed."),
        SingleBuff(BuffDashM, "Dash", FamDash, 7, SkillEffect.BuffMoveSpeed,
            new(SkillEffect.BuffMoveSpeed, 60, ModifierMode.Flat), "+60 Move Speed."),
        SingleBuff(BuffSprint2, "Sprint", FamDash, 8, SkillEffect.BuffMoveSpeed,
            new(SkillEffect.BuffMoveSpeed, 60, ModifierMode.Flat), "+60 Move Speed."),

        DashPotion(PotDashC, "Dash Potion (Lesser)",   BuffDashC, 15),
        DashPotion(PotDashU, "Dash Potion",            BuffDashU, 30),
        DashPotion(PotDashR, "Dash Potion (Greater)",  BuffDashR, 45),
        DashPotion(PotDashE, "Dash Potion (Superior)", BuffDashE, 50),
        DashPotion(PotDashL, "Dash Potion (Grand)",    BuffDashL, 55),
        DashPotion(PotDashM, "Dash Potion (Supreme)",  BuffDashM, 60),

        // (HP Boost DELETED 2026-08-07 with the God layer, playtest-19 `0b` — its only learn table
        //  was the God one. The Max-HP buff family the players actually use is `Body` / FamMaxHp.)

        // ---- Robe Armor Mastery — the BONUS half of the old Robe Mastery: robe P.Def, nothing else.
        //      2 levels at char 7 / 14 (owner's table: +7, +9). No penalties of any kind live here any
        //      more, which is what lets the 2nd-class masteries REPLACE it without deleting the
        //      wrong-weight rule along with it — that now belongs to Spellcaster Mastery, which is
        //      never replaced. Id kept (`mastery_robe`): same skill, narrowed job. ----
        new(MasteryRobe, "Robe Armor Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. While wearing a ROBE: extra physical defence (rising with level).",
            Levels: new[]
            {
                new SkillLevel(SpCost: 480,  Description: "Robe Armor Mastery Lv.1 (+7 P.Def in a robe)."),
                new SkillLevel(SpCost: 2200, Description: "Robe Armor Mastery Lv.2 (+9 P.Def in a robe)."),
            },
            ArmorMasteryLevels: MageRobeLevels),

        // ---- Spellcaster Mastery — REPLACES Weapon Proficiency (2026-08-07 restructure). One skill
        //      now states the whole "what a caster may wear and hold" rule, and it is auto-granted at
        //      level 1 and never superseded, so no 2nd-class mastery has to restate it.
        //
        //      ARMOR (data, SpellcasterLevels): robe = +20% MP regen · light/heavy/none = cast ×0.5,
        //      attack speed ×0.5.
        //      WEAPON (Entity.RecomputeDerived — these three have no StatMods field):
        //        • wand/staff  → cast ×1, M.Atk ×1                      (the trained weapon)
        //        • sword/blunt → cast ×1, M.Atk ×NonMagicWeaponMagicMult (a mace casts, but weakly)
        //        • bow/dagger/bare → cast ×0.5, M.Atk ×0.5, magic accuracy ×0.5
        //      ⚠ The wrong-weapon magic penalty was a COLLAPSE to ×0.05 before this; the owner set it
        //      to ×0.5. "magic accuracy" maps to MagicFailResist, the only spell-landing stat we have.
        new(SpellcasterMastery, "Spellcaster Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { WeaponProficiency },
            Description: "Passive. A ROBE is a caster's armor: +20% MP regeneration. Light or heavy "
                       + "armor — or none — halves your casting and attack speed. A wand or staff is "
                       + "your trained weapon; a sword or mace still casts at full speed but for much "
                       + "less magic attack; a bow, dual blades or bare hands halve your casting speed, "
                       + "magic attack and magic accuracy.",
            ArmorMasteryLevels: SpellcasterLevels),

        // ---- Weapon Proficiency — RETIRED 2026-08-07, replaced by Spellcaster Mastery above. The def
        //      stays so an existing character's learned id still resolves (and is then superseded); the
        //      class tables and AutoLearnCoreSkills grant Spellcaster Mastery instead. Don't re-home it. ----
        new(WeaponProficiency, "Weapon Proficiency", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Superseded by Spellcaster Mastery."),

        // ---- Divine Focus — clerics (Healer 2nd class) auto-learn Lv1 at 20; the Warchanter discipline
        //      upgrades to Lv2 at 40. EFFECT: with NO magic weapon equipped, healing OUTPUT is scaled down
        //      (Lv1 ×0.5, Lv2 ×0.75). Handled in Entity/heal by the magic-weapon flag. ----
        new(DivineFocus, "Divine Focus", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Divine power flows through a magic weapon. With NO magic weapon (wand or "
                       + "staff) equipped, your healing is halved — pure healers must wield one.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 0),
                new SkillLevel(SpCost: 0, Description: "Divine Focus Lv.2 — the non-magic-weapon healing penalty eases to ×0.75 (buffers stay useful in fighter gear)."),
            }),

        // Novice's Grace — display-only newbie protection (the level check does the real work; this just
        // tells the player). The description embeds the threshold constant so it stays in sync.
        new(NoviceGrace, "Novice's Grace", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: $"Passive. Below level {GameConstants.DeathExpPenaltyMinLevel} you lose NO experience "
                       + "when you die. This grace fades once you reach that level."),

        // ===== Class Balance — the per-class tuning hook (auto-granted, currently no-ops) =====
        // ⚠ COMMENTED OUT 2026-08-07, owner's ruling (playtest-19 `0a`): *"class_balance should be
        // commented for now"*. NOT DELETED — the ids, the consts, ClassBalanceFor() and
        // BalancePassive() all stay, so restoring the hook is uncommenting these eight lines and the
        // grant in GameLoopService.AutoLearnCoreSkills. Every level here was an ALL-ZERO
        // PassiveEffect, so removing them changes no number; they only cluttered the skill window.
        // Characters that already learned one are cleaned up on their next AutoLearnCoreSkills.
        // BalancePassive(BalanceTank,    "Class Balance (Tank)",    BaseClass.Fighter),
        // BalancePassive(BalanceWarrior, "Class Balance (Warrior)", BaseClass.Fighter),
        // BalancePassive(BalanceRogue,   "Class Balance (Rogue)",   BaseClass.Fighter),
        // BalancePassive(BalanceArcher,  "Class Balance (Archer)",  BaseClass.Fighter),
        // BalancePassive(BalanceFighter, "Class Balance",           BaseClass.Fighter),
        // BalancePassive(BalanceNuker,   "Class Balance (Nuker)",   BaseClass.Mage),
        // BalancePassive(BalanceHealer,  "Class Balance (Healer)",  BaseClass.Mage),
        // BalancePassive(BalanceMage,    "Class Balance",           BaseClass.Mage),

        // ===== (Combat training passives REMOVED 2026-07-24) — the soul/spell rune bonus is now a held
        //       RUNE item (WarRuneBuff / SpellRuneBuff above), not an auto-granted passive. =====

        // ===== Class identity "sure" floor passives (auto-granted at 20/40/76 = lvl 1/2/3) =====
        // Evasion Mastery is the evade FLOOR AND NOTHING ELSE (owner ruling, playtest-19 M9): the
        // +20% crit and the +20 evasion are GONE. The crit moved to the rogue Weapon Mastery at
        // level 20 (he wants the high crit rate EARLY, not a spike at 32), and the evasion budget
        // is already closed at ~18 by authoring — 14 from armor mastery + 4 from the buff. More
        // than that and "everything else will make him untouchable". The floor is an anti-ACCURACY
        // tool only: it exists for fighting the classes that stack accuracy, not as a stat lean.
        // ⚠ Lv3 is authored but UNREACHABLE on purpose (owner, 2026-08-07): its milestone is the 4th
        // class change, which does not exist yet, and 76 is only a level. `FloorPassiveFor` grants
        // Lv1 at the 2nd class and Lv2 only to a MELEE discipline. Leave the rung here — when the 4th
        // tier lands it is already written, and gating it is one condition in FloorPassiveFor.
        LeveledPassive(EvadeMastery, "Evasion Mastery", BaseClass.Fighter,
            "Passive. Dodge floor 10/20/30%.",
            new PassiveEffect(EvadeFloor: 0.10f),
            new PassiveEffect(EvadeFloor: 0.20f),
            new PassiveEffect(EvadeFloor: 0.30f)),
        // (Reflexes — the ARCHER floor passive — DELETED 2026-08-07, playtest-19 `0a`/G1. It was the
        //  one genuinely dead line on that list: no 2nd class has carried Archetype.Archer since the
        //  archer→rogue merge, so nothing could ever be granted it. Don't re-add it; a ranged rogue's
        //  floor comes from Evasion Mastery, and after 40 the ranged DISCIPLINES get none — see M7.)
        LeveledPassive(Precision, "Precision", BaseClass.Fighter,
            "Passive. Your physical attacks always land at least 10/20/30% of the time.",
            new PassiveEffect(HitFloor: 0.10f), new PassiveEffect(HitFloor: 0.20f), new PassiveEffect(HitFloor: 0.30f)),
        LeveledPassive(AntiMagic, "Anti-Magic", BaseClass.Fighter,
            "Passive. Spells fizzle on you at least 10/15/20% of the time.",
            new PassiveEffect(MagicFailFloor: 0.10f), new PassiveEffect(MagicFailFloor: 0.15f), new PassiveEffect(MagicFailFloor: 0.20f)),

        // (Wind Walk / Mass Wind Walk DELETED 2026-07-31 — the buff-ladder pass. They were a second,
        //  unranked source of move speed sitting outside every family; the improved Speed buff and
        //  the Swift line replace them. Don't re-home them.)

        // ---- Return line (universal escape / recall to the nearest town) ----
        // The FREE fallback: a long 30s channel that ANY damage cancels (FragileCast), 5-min reuse.
        // Fixed cast + fixed cooldown = no haste/CD buffs speed it up. For when you forgot scrolls.
        new(ReturnSkill, "Return", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 300, CooldownTicks: 3000, Range: 0, Power: 0,
            Category: SkillCategory.Magic, SpCost: 0, TargetMode: TargetMode.SelfOnly,
            FixedCast: true, FixedCooldown: true, FragileCast: true, TeleportsToTown: true,
            Description: "Channel 30s to return to the nearest town. ANY damage cancels it. 5 min reuse."),

        // Bought scroll: 10s fixed cast, 10s reuse. Consumes one Scroll of Return.
        new(ScrollReturnSkill, "Scroll of Return", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 100, CooldownTicks: 100, Range: 0, Power: 0,
            Category: SkillCategory.Magic, SpCost: 0, TargetMode: TargetMode.SelfOnly,
            FixedCast: true, FixedCooldown: true, TeleportsToTown: true,
            ConsumableId: ItemCatalog.ScrollReturn, ConsumableAmount: 1,
            Description: "Use a Scroll of Return: 10s cast, teleport to the nearest town."),

        // Ultimate scroll: TRULY INSTANT — CastTicks 0, 1s reuse. Consumes one Ultimate scroll.
        // This is the ESCAPE button (owner, 2026-07-17): you use it to get out of a fight you're losing,
        // so it must not be a cast at all. Any cast time > 0 roots you, can be interrupted, and the cast
        // pipeline floors every cast at 2 ticks anyway — so 0 is the only way to get a real escape.
        // A 0-tick consumable bypasses the cast pipeline entirely and is delivered by UsePotion, which
        // handles TeleportsToTown + the reuse timer for exactly this reason.
        new(ScrollReturnUltSkill, "Ultimate Scroll of Return", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 10, Range: 0, Power: 0,
            Category: SkillCategory.Magic, SpCost: 0, TargetMode: TargetMode.SelfOnly,
            FixedCast: true, FixedCooldown: true, TeleportsToTown: true,
            ConsumableId: ItemCatalog.ScrollReturnUltimate, ConsumableAmount: 1,
            Description: "Use an Ultimate Scroll of Return: INSTANTLY return to the nearest town."),

        // ---- Resurrection scrolls (used by a LIVING player on a DEAD ally, like the healer's res) ----
        // Basic: 10s fixed cast, 10s reuse, revives at 30% HP/MP with NO exp restored. Consumes one scroll.
        new(ScrollResurrectSkill, "Scroll of Resurrection", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 100, CooldownTicks: 100, Range: 600, Power: 0,
            Category: SkillCategory.Magic, SpCost: 0,
            FixedCast: true, FixedCooldown: true, Resurrect: true, ResExpPct: 0f,
            ConsumableId: ItemCatalog.ScrollResurrect, ConsumableAmount: 1,
            Description: "Channel 10s to revive a fallen ally at 30% HP and MP. Restores none of the "
                       + "experience they lost on death. 10s reuse."),

        // Ultimate: 0.5s fixed cast, 10s reuse, revives at 30% HP/MP and restores ALL lost exp. Consumes one.
        new(ScrollResurrectUltSkill, "Ultimate Scroll of Resurrection", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 5, CooldownTicks: 100, Range: 600, Power: 0,
            Category: SkillCategory.Magic, SpCost: 0,
            FixedCast: true, FixedCooldown: true, Resurrect: true, ResExpPct: 1f,
            ConsumableId: ItemCatalog.ScrollResurrectUltimate, ConsumableAmount: 1,
            Description: "Revive a fallen ally at 30% HP and MP almost instantly and restore ALL the "
                       + "experience they lost on death. 10s reuse."),

        // ---- Angel's Protection (noblesse) — a buff that makes the TARGET's other buffs SURVIVE death ----
        // A pure marker buff (no stat effect): while it's up, dying removes ONLY this buff and keeps the
        // rest. Consumes 5 Skill Stones per cast (not free). For now every class auto-learns it at 76;
        // LATER it becomes a long noblesse quest reward (subclass @76 + a 4th class; changing class resets it).
        // Cast on an ALLY or yourself (owner, 2026-07-17 — it used to be SelfOnly): default SelfOrTarget +
        // a real range, so it reads like every other castable buff (cf. Might).
        // SHARED BuffKey "buff_preservation" + Rank 1 = the WEAKEST preservation tier: the future tank
        // self-auto-res (Rank 3) and healer target-auto-res (Rank 2) OVERRIDE it and it can't override them.
        // FIXED 1s cast / FIXED 10s reuse (owner, 2026-07-17): a FIGHTER has poor cast speed, and a
        // protection you can't get up before you die is worthless — so neither number bends to stats.
        new(AngelsProtection, "Angel's Protection", BaseClass.Fighter, SkillEffect.None,
            MpCost: 30, CastTicks: 10, CooldownTicks: 100, Range: 600, Power: 0,
            Category: SkillCategory.Buff, SpCost: 0,
            FixedCast: true, FixedCooldown: true,
            DurationTicks: 36000, BuffKey: "buff_preservation", Rank: 1, InitialMpCost: 6,
            KeepsBuffsOnDeath: true,
            ConsumableId: ItemCatalog.SkillStone, ConsumableAmount: 5,
            Description: "Blesses an ally (or yourself) so their other buffs SURVIVE their next death (only "
                       + "this blessing is consumed). Costs 5 Skill Stones. Lasts 60 minutes or until they die."),
    };
}
