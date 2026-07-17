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
    public const string WeaponProficiency = "weapon_proficiency";
    // ---- Divine Focus: clerics (Healer 2nd class) auto-learn Lv1 at 20; the Warchanter discipline
    //      upgrades to Lv2 at 40. While NO magic weapon is equipped, healing OUTPUT is scaled: Lv1 ×0.5
    //      (pure healers must wield a magic weapon), Lv2 ×0.75 (buffers stay relevant in fighter gear). ----
    public const string DivineFocus = "divine_focus";
    // ---- Novice's Grace: DISPLAY-ONLY passive, auto-shown below GameConstants.DeathExpPenaltyMinLevel so a
    //      newbie can SEE that death costs no exp yet. No mechanical effect (the level check in
    //      ApplyDeathExpPenalty does the work); auto-removed once they reach the level. ----
    public const string NoviceGrace = "novice_grace";
    // ---- Combat "training" passives, auto-granted at level 40 (soulshot/spiritshot
    //      stand-in). Doubling the atk STAT gives ×2 physical (linear) but ×1.414
    //      magic (√mAtk) — the soulshot/spiritshot ratio. ----
    public const string PhysicalTraining = "physical_training";  // multi-level (9): +10%…+100% atk
    public const string SpiritTraining   = "spirit_training";    // multi-level (9): +atk + cast speed
    // ---- Class identity "sure" floor passives — now ONE multi-level skill each
    //      (auto-granted at the class-change milestone, level = tier 1/2/3). The floor
    //      VALUES live in the SkillDef Levels, not in code. See FloorPassiveFor. ----
    public const string EvadeMastery = "evade_mastery"; // Rogue   10/20/30%
    public const string Reflexes     = "reflexes";      // Archer    5/10/15%
    public const string Precision    = "precision";     // Warrior 10/20/30% hit floor
    public const string AntiMagic    = "anti_magic";    // Tank    10/15/20% magic fizzle
    // ---- HEALING-potion skills. The potion ITEM names one of these; the SKILL does the
    //      healing and consumes the item (its ConsumableId). Everything is a skill — only
    //      what GRANTS it differs. The HoT ones are ordinary buffs, so they show on the buff
    //      bar and get "stronger cancels weaker" free from BuffKey + Rank (which is exactly
    //      what the old bespoke PotionRarity/PotionEffectTicks state did by hand). ----
    public const string PotHealMinor   = "pot_heal_minor";
    public const string PotHeal        = "pot_heal";
    public const string PotHealGreater = "pot_heal_greater";
    // ---- Buff-potion buffs (consumed, not cast). ----
    public const string PBuffSpeedC = "pbuff_speed_c";
    public const string PBuffSpeedU = "pbuff_speed_u";
    public const string PBuffSpeedR = "pbuff_speed_r";
    public const string PBuffCastC = "pbuff_cast_c";
    public const string PBuffCastU = "pbuff_cast_u";
    public const string PBuffCastR = "pbuff_cast_r";
    public const string PBuffAtkC = "pbuff_atk_c";
    public const string PBuffAtkU = "pbuff_atk_u";
    public const string PBuffAtkR = "pbuff_atk_r";
    // ---- Learnable HP Boost — ONE multi-level skill (3 levels: +5/+15/+35%). ----
    public const string HpBoost = "hp_boost";
    public const string WindWalk = "wind_walk";
    public const string MassWindWalk = "mass_wind_walk";
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

    // ---- Multi-level PASSIVE factory: a pure passive whose levels each carry a
    //      PassiveEffect (the floor/lean value for that level). ----
    private static SkillDef LeveledPassive(string id, string name, BaseClass cls, string desc,
        params PassiveEffect[] perLevel) => new(
        id, name, cls, SkillEffect.None,
        MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
        Category: SkillCategory.Passive, SpCost: 0, Description: desc,
        Levels: perLevel.Select(p => new SkillLevel(Passive: p)).ToArray());

    // Combat-training passive: 9 levels, +10%…+80% then +100% attack (the soulshot/
    // spiritshot stand-in). At max level the +100% atk = ×2 P.Atk/M.Atk, which is exactly
    // what a shot does: ×2 physical damage (linear), ×1.414 magic (√mAtk).
    // castSpeedFlat mirrors the real spiritshot bonus: a FLAT +40 to the cast stat. It used
    // to be a 0.40 PERCENT, applied as a time cut (×0.6 time = +67% speed), which compounded
    // with WIT/gear/buffs and inflated a buffed L40 mage to ~2200 against the 1999 cap.
    /// <param name="magic">A MAGE's training (spiritshots) — boosts M.Atk. A fighter's (soulshots)
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
                    // spiritshot dampening exactly: physical +100% → magic +41%, but the number now READS 41%.
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

    /// <summary>A Class Balance passive: one level, an all-zero <see cref="PassiveEffect"/>.
    /// Fill in fields here (PveBasicDamagePct, PvpMagicDamagePct, AttackPct, …) to tune a class.</summary>
    private static SkillDef BalancePassive(string id, string name, BaseClass cls) => new(
        id, name, cls, SkillEffect.None,
        MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
        Category: SkillCategory.Passive, SpCost: 0,
        Description: "Class balance. Reserved for tuning — currently no effect.",
        Levels: new[] { new SkillLevel(Passive: new PassiveEffect()) });

    /// <summary>The identity floor passive an archetype receives at its current class
    /// tier (milestones 20/40/76) — as (skill id, skill LEVEL), or null. Granted in
    /// AutoLearnCoreSkills. The floor VALUES live in the SkillDef Levels, not in code.</summary>
    public static (string Id, int Level)? FloorPassiveFor(Archetype? archetype, int level)
    {
        int tier = level >= 76 ? 3 : level >= 40 ? 2 : level >= 20 ? 1 : 0;
        if (tier == 0) return null;
        return archetype switch
        {
            Archetype.Rogue   => (EvadeMastery, tier),
            Archetype.Archer  => (Reflexes, tier),
            Archetype.Warrior => (Precision, tier),
            Archetype.Tank    => (AntiMagic, tier),
            // Mages get NO auto magic-fail floor — it comes from their LEARNED Anti-Magic
            // (anti_magic_mage), available to every mage class.
            _ => null
        };
    }

    // Base MAGE robe mastery per level (char 1/7/14, mage CSV). ROBE: +20% MP regen and P.Def
    // (no cast change). Any non-robe body — light/heavy AND no armor at all — penalises: attack
    // speed ×0.8 and cast speed ×0.5. Penalty literals are inlined (not shared fields) to avoid
    // static-init ordering across partials. The nuker/healer masteries REPLACE this from level 20.
    private static readonly ArmorMasteryProfile[] MageRobeLevels = new[]
    {
        new ArmorMasteryProfile(
            Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 0),
            Light: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            Heavy: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            None:  new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f)),
        new ArmorMasteryProfile(
            Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 7),
            Light: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            Heavy: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            None:  new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f)),
        new ArmorMasteryProfile(
            Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 9),
            Light: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            Heavy: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            None:  new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f)),
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
        new(PotHealMinor, "Minor Healing", BaseClass.Fighter, SkillEffect.HealOverTime,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 150, BuffKey: "potion_heal", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.HealOverTime, 0.01f) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable,
            Description: "Restores 1% of max HP per second for 15s."),
        new(PotHeal, "Healing", BaseClass.Fighter, SkillEffect.HealOverTime,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 150, BuffKey: "potion_heal", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.HealOverTime, 0.02f) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable,
            Description: "Restores 2% of max HP per second for 15s."),
        new(PotHealGreater, "Greater Healing", BaseClass.Fighter, SkillEffect.Heal,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Heal, 0.50f) },
            Category: SkillCategory.Heal,
            Description: "Instantly restores 50% of max HP."),

        // ----- Buff-potion buffs (consumed, not cast). Same BuffKey per line so a
        //       rarer potion supersedes a weaker one; rare = bigger + longer. -----
        new(PBuffSpeedC, "Swiftness (Lesser)", BaseClass.Fighter, SkillEffect.BuffMoveSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 600, BuffKey: "pbuff_speed", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMoveSpeed, 15, ModifierMode.Flat) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable, Description: "+15 Move Speed for 60s."),
        new(PBuffSpeedU, "Swiftness", BaseClass.Fighter, SkillEffect.BuffMoveSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 900, BuffKey: "pbuff_speed", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMoveSpeed, 20, ModifierMode.Flat) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable, Description: "+20 Move Speed for 90s."),
        new(PBuffSpeedR, "Swiftness (Greater)", BaseClass.Fighter, SkillEffect.BuffMoveSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 1800, BuffKey: "pbuff_speed", Rank: 3,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMoveSpeed, 30, ModifierMode.Flat) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable, Description: "+30 Move Speed for 180s."),

        new(PBuffCastC, "Focus (Lesser)", BaseClass.Mage, SkillEffect.BuffCastSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 600, BuffKey: "pbuff_cast", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffCastSpeed, 0.08f) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable, Description: "+8% Cast Speed for 60s."),
        new(PBuffCastU, "Focus", BaseClass.Mage, SkillEffect.BuffCastSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 900, BuffKey: "pbuff_cast", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffCastSpeed, 0.12f) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable, Description: "+12% Cast Speed for 90s."),
        new(PBuffCastR, "Focus (Greater)", BaseClass.Mage, SkillEffect.BuffCastSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 1800, BuffKey: "pbuff_cast", Rank: 3,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffCastSpeed, 0.20f) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable, Description: "+20% Cast Speed for 180s."),

        new(PBuffAtkC, "Haste (Lesser)", BaseClass.Fighter, SkillEffect.BuffAtkSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 600, BuffKey: "pbuff_atkspeed", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtkSpeed, 0.08f) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable, Description: "+8% Attack Speed for 60s."),
        new(PBuffAtkU, "Haste", BaseClass.Fighter, SkillEffect.BuffAtkSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 900, BuffKey: "pbuff_atkspeed", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtkSpeed, 0.12f) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable, Description: "+12% Attack Speed for 90s."),
        new(PBuffAtkR, "Haste (Greater)", BaseClass.Fighter, SkillEffect.BuffAtkSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 1800, BuffKey: "pbuff_atkspeed", Rank: 3,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtkSpeed, 0.20f) },
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable, Description: "+20% Attack Speed for 180s."),

        // ---- Learnable HP Boost — ONE skill, 3 levels (+5 / +15 / +35% Max HP) ----
        new(HpBoost, "HP Boost", BaseClass.Mage, SkillEffect.BuffHp,
            MpCost: 25, CastTicks: 10, CooldownTicks: 5, Range: 0, Power: 0,
            DurationTicks: 6000, BuffKey: "hp_boost", Rank: 1,
            Category: SkillCategory.Buff,
            Description: "Raises your Max HP for a time.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 25, SpCost: 1000,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffHp, 0.05f) },
                    Description: "Raises Max HP by 5%."),
                new SkillLevel(MpCost: 35, SpCost: 3000,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffHp, 0.15f) },
                    Description: "Raises Max HP by 15%."),
                new SkillLevel(MpCost: 45, SpCost: 8000,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffHp, 0.35f) },
                    Description: "Raises Max HP by 35%."),
            }),

        // ---- Robe Mastery — base MAGE armor mastery (PASSIVE, per-level StatMods data).
        //      Levels 1/2/3 at char 1/7/14; the nuker/healer 2nd-class masteries REPLACE it.
        //      Robe = caster lean; light/heavy hinder casting. Applied in RecomputeDerived
        //      by the worn body weight. Numbers are placeholders (carried over from the old
        //      formula) pending the real mage table. ----
        new(MasteryRobe, "Robe Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. While wearing a ROBE: faster casting, more MP and MP regen, "
                       + "and defence (rising with level). Light/heavy armor slows your casting.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 0),
                new SkillLevel(SpCost: 480,  Description: "Robe Mastery Lv.2 (+P.Def)."),
                new SkillLevel(SpCost: 2200, Description: "Robe Mastery Lv.3 (+P.Def)."),
            },
            ArmorMasteryLevels: MageRobeLevels),

        // ---- Weapon Proficiency — all mages auto-learn at level 1. Worded like Robe Mastery (the trained
        //      weapon gives the bonus); the EFFECT is a ×0.5 cast-speed penalty on an untrained weapon.
        //      Handled in Entity.RecomputeDerived by WeaponType (sword/blunt incl. wand/staff = trained). ----
        new(WeaponProficiency, "Weapon Proficiency", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Equipping a sword or blunt weapon (wands and staves count) lets you cast "
                       + "spells efficiently. With a bow, dual blades, or bare hands your casting speed is "
                       + "halved and your magic attack collapses to a fraction."),

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
        BalancePassive(BalanceTank,    "Class Balance (Tank)",    BaseClass.Fighter),
        BalancePassive(BalanceWarrior, "Class Balance (Warrior)", BaseClass.Fighter),
        BalancePassive(BalanceRogue,   "Class Balance (Rogue)",   BaseClass.Fighter),
        BalancePassive(BalanceArcher,  "Class Balance (Archer)",  BaseClass.Fighter),
        BalancePassive(BalanceFighter, "Class Balance",           BaseClass.Fighter),
        BalancePassive(BalanceNuker,   "Class Balance (Nuker)",   BaseClass.Mage),
        BalancePassive(BalanceHealer,  "Class Balance (Healer)",  BaseClass.Mage),
        BalancePassive(BalanceMage,    "Class Balance",           BaseClass.Mage),

        // ===== Combat training passives (auto-granted; level by character level) =====
        // 9 levels: +10%…+80% attack (40→75) then +100% (76+). The auto-grant level
        // comes from StatCalculator.TrainingLevelFor.
        TrainingPassive(PhysicalTraining, "Physical Training", BaseClass.Fighter, magic: false, 0f,
            "Passive. Relentless conditioning — PHYSICAL attack grows with level (+10% to +100%)."),
        TrainingPassive(SpiritTraining, "Spirit Training", BaseClass.Mage, magic: true, 40f,
            "Passive. Honed focus — +40 casting speed and MAGIC attack growing with level (+5% to +41%)."),

        // ===== Class identity "sure" floor passives (auto-granted at 20/40/76 = lvl 1/2/3) =====
        // Rogue identity now DATA: the evade floor + the archetype crit/evasion LEANS (+20% crit,
        // +20 eva) migrated here from StatCalculator's hardcoded Archetype switches (stats-via-skills).
        LeveledPassive(EvadeMastery, "Evasion Mastery", BaseClass.Fighter,
            "Passive. Dodge floor 10/20/30%, +20% crit chance, +20 evasion.",
            new PassiveEffect(EvadeFloor: 0.10f, CritRate: 0.20f, Evasion: 20),
            new PassiveEffect(EvadeFloor: 0.20f, CritRate: 0.20f, Evasion: 20),
            new PassiveEffect(EvadeFloor: 0.30f, CritRate: 0.20f, Evasion: 20)),
        // Archer identity now DATA: evade floor + +15% crit / +10 eva leans.
        LeveledPassive(Reflexes, "Reflexes", BaseClass.Fighter,
            "Passive. Dodge floor 5/10/15%, +15% crit chance, +10 evasion.",
            new PassiveEffect(EvadeFloor: 0.05f, CritRate: 0.15f, Evasion: 10),
            new PassiveEffect(EvadeFloor: 0.10f, CritRate: 0.15f, Evasion: 10),
            new PassiveEffect(EvadeFloor: 0.15f, CritRate: 0.15f, Evasion: 10)),
        LeveledPassive(Precision, "Precision", BaseClass.Fighter,
            "Passive. Your physical attacks always land at least 10/20/30% of the time.",
            new PassiveEffect(HitFloor: 0.10f), new PassiveEffect(HitFloor: 0.20f), new PassiveEffect(HitFloor: 0.30f)),
        LeveledPassive(AntiMagic, "Anti-Magic", BaseClass.Fighter,
            "Passive. Spells fizzle on you at least 10/15/20% of the time.",
            new PassiveEffect(MagicFailFloor: 0.10f), new PassiveEffect(MagicFailFloor: 0.15f), new PassiveEffect(MagicFailFloor: 0.20f)),

        // ---- Wind Walk (move-speed self buff, learnable) ----
        new(WindWalk, "Wind Walk", BaseClass.Mage,
            SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion,
            MpCost: 30, CastTicks: 10, CooldownTicks: 10, Range: 0, Power: 0,
            DurationTicks: 12000, BuffKey: "wind_walk", Rank: 1,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffMoveSpeed, 33, ModifierMode.Flat),
                new(SkillEffect.BuffEvasion, 5, ModifierMode.Flat),
            },
            Category: SkillCategory.Buff, SpCost: 1500, TargetMode: TargetMode.SelfOnly,
            Description: "Move +33 and Evasion +5 for 20 minutes (self)."),

        // Party version: same effect + same BuffKey, but buffs nearby allies.
        new(MassWindWalk, "Mass Wind Walk", BaseClass.Mage,
            SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion,
            MpCost: 120, CastTicks: 15, CooldownTicks: 50, Range: 0, Power: 0,
            DurationTicks: 12000, BuffKey: "wind_walk", Rank: 1,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffMoveSpeed, 33, ModifierMode.Flat),
                new(SkillEffect.BuffEvasion, 5, ModifierMode.Flat),
            },
            Category: SkillCategory.Buff, SpCost: 5000,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Description: "Move +33 and Evasion +5 to nearby allies for 20 minutes."),

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

        // Ultimate scroll: ~0.4s (near-instant) fixed cast, 1s reuse. Consumes one Ultimate scroll.
        new(ScrollReturnUltSkill, "Ultimate Scroll of Return", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 4, CooldownTicks: 10, Range: 0, Power: 0,
            Category: SkillCategory.Magic, SpCost: 0, TargetMode: TargetMode.SelfOnly,
            FixedCast: true, FixedCooldown: true, TeleportsToTown: true,
            ConsumableId: ItemCatalog.ScrollReturnUltimate, ConsumableAmount: 1,
            Description: "Use an Ultimate Scroll of Return: near-instant return to the nearest town."),

        // ---- Resurrection scrolls (used by a LIVING player on a DEAD ally, like the healer's res) ----
        // Basic: 10s fixed cast, 1-min reuse, revives at 30% HP/MP with NO exp restored. Consumes one scroll.
        new(ScrollResurrectSkill, "Scroll of Resurrection", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 100, CooldownTicks: 600, Range: 600, Power: 0,
            Category: SkillCategory.Magic, SpCost: 0,
            FixedCast: true, FixedCooldown: true, Resurrect: true, ResExpPct: 0f,
            ConsumableId: ItemCatalog.ScrollResurrect, ConsumableAmount: 1,
            Description: "Channel 10s to revive a fallen ally at 30% HP and MP. Restores none of the "
                       + "experience they lost on death. 1 min reuse."),

        // Ultimate: 0.5s fixed cast, 1-min reuse, revives at 30% HP/MP and restores ALL lost exp. Consumes one.
        new(ScrollResurrectUltSkill, "Ultimate Scroll of Resurrection", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 5, CooldownTicks: 600, Range: 600, Power: 0,
            Category: SkillCategory.Magic, SpCost: 0,
            FixedCast: true, FixedCooldown: true, Resurrect: true, ResExpPct: 1f,
            ConsumableId: ItemCatalog.ScrollResurrectUltimate, ConsumableAmount: 1,
            Description: "Revive a fallen ally at 30% HP and MP almost instantly and restore ALL the "
                       + "experience they lost on death. 1 min reuse."),

        // ---- Angel's Protection (noblesse) — a self-buff that makes your OTHER buffs SURVIVE death ----
        // A pure marker buff (no stat effect): while it's up, dying removes ONLY this buff and keeps the
        // rest. Consumes 5 Skill Stones per cast (not free). For now every class auto-learns it at 76;
        // LATER it becomes a long noblesse quest reward (subclass @76 + a 4th class; changing class resets it).
        // SHARED BuffKey "buff_preservation" + Rank 1 = the WEAKEST preservation tier: the future tank
        // self-auto-res (Rank 3) and healer target-auto-res (Rank 2) OVERRIDE it and it can't override them.
        new(AngelsProtection, "Angel's Protection", BaseClass.Fighter, SkillEffect.None,
            MpCost: 30, CastTicks: 10, CooldownTicks: 20, Range: 0, Power: 0,
            Category: SkillCategory.Buff, SpCost: 0, TargetMode: TargetMode.SelfOnly,
            DurationTicks: 36000, BuffKey: "buff_preservation", Rank: 1, InitialMpCost: 6,
            KeepsBuffsOnDeath: true,
            ConsumableId: ItemCatalog.SkillStone, ConsumableAmount: 5,
            Description: "Blesses you so your other buffs SURVIVE your next death (only this blessing is "
                       + "consumed). Costs 5 Skill Stones. Lasts 60 minutes or until you die."),
    };
}
