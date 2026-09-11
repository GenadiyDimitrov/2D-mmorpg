namespace Game.Shared;

// ===========================================================================
//  THE SKILL MASTERIES — the passives that turn `BL-190`'s three engine
//  channels on, authored by him on 2026-09-10 (`BL-191`).
//
//      "- buffers and healers get the duration passive with base 10% @76
//       - Mages get cooldown passive with base 5% @76
//       - warriors/aoe-warriors get the double dmg passive with base 3,7,10% @20,40,76
//         - and warriors/aoe-war get another toggle skill that doubles the effect
//           of the double passive drain 50hp/s and increases the p.mp.consumtion with 25%"
//
//  🔑 WHY ONE FILE FOR FOUR SKILLS ACROSS FOUR ARCHETYPES. These are not a
//  discipline's kit — they are the four authors of ONE engine feature, and the
//  numbers only make sense read together (the warrior's 3/7/10 ladder against
//  the mage's flat 5, the toggle against the ladder it doubles). Splitting them
//  into `Skills.Warchanter4th.cs` / `Skills.Nuker4th.cs` / a warrior file that
//  does not exist yet would scatter one ruling across four places and put two of
//  them in files whose CSVs are not finished.
//
//  ⚠ WHAT THESE ARE WORTH IS NOT WRITTEN HERE. A base is a base: the finished
//  rate is `clamp(base × buffs × MasteryAtkMod(EffectiveAtk), 0, 25%)`, which is
//  StatCalculator.SkillMasteryRate. See docs/Formulas.md.
// ===========================================================================

public static partial class SkillCatalog
{
    // ---- ids ----
    /// <summary>WARRIOR + AoE WARRIOR — the double-damage mastery. 3 / 7 / 10% at 20 / 40 / 76.</summary>
    public const string Overpower = "overpower";

    /// <summary>THE TOGGLE that doubles whatever mastery bases its holder carries. 50 HP/s,
    /// +25% MP on physical skills. Learned at <b>81</b>.
    ///
    /// <para>🔑 <b>ONE SKILL, TWO NAMES, TWO ARCHETYPES</b> (his 2026-09-10 rename). The warrior calls
    /// it <b>"Overpower Mastery"</b> and it doubles his damage base; the melee rogue calls it
    /// <b>"Momentum Mastery"</b> and it doubles his reuse base. The def is one; the flavour is a
    /// <c>ClassSkill.DisplayName</c> override, which is this project's standard way to do that.</para>
    ///
    /// <para>⚠ It was `blood_rage` / "Blood Rage" for exactly one version (0.124.x). Renamed because
    /// the skill stopped being the warrior's alone.</para></summary>
    public const string DoubleMastery = "double_mastery";

    /// <summary>BUFFER + HEALER — the duration mastery. 10% at 76.</summary>
    public const string LastingEnchantment = "lasting_enchantment";

    /// <summary>THE REUSE-RESET mastery, 5% at 76. The nuker calls it <b>"Arcane Momentum"</b>, the
    /// melee rogue <b>"Stab Momentum"</b>.
    ///
    /// <para>⚠ It was `arcane_momentum` for exactly one version. He renamed it 2026-09-10 with the
    /// reason attached — *"as other classes can aqure it too"* — so the ID is now the MECHANIC and
    /// the flavour lives in the per-class display name.</para>
    ///
    /// <para>⚠ <b>THIS ID IS DELIBERATELY CROSS-CHAIN</b> — a Mage discipline (Magus) and Fighter
    /// disciplines (the three melee rogues) both learn it, which is the one thing
    /// <c>SkillCsvSeed --chains</c> exists to report. It will name this id under "CROSS-CHAIN IDS"
    /// and that is CORRECT output, not a defect to tidy away: the audit's automatic exemption only
    /// covers ids every ascended class learns, and this one is not that.</para></summary>
    public const string ReuseResetMomentum = "reuse_reset_momentum";

    /// <summary>The warrior's Overpower ladder, as authored: three rungs, three bases.
    /// ⚠ These are the CSV's numbers and the only three that exist — do not extend the array to
    /// "finish" it. `warrior 4th.csv` is a placeholder with one row in it, and the day he writes the
    /// rest is the day a fourth rung can appear.</summary>
    private static readonly float[] OverpowerBase = { 0.03f, 0.07f, 0.10f };

    private static SkillDef[] SkillMasterySkills()
    {
        // 4th-tier prices come from the shared ladder (`shared 4th.csv`'s 76 row: 6.5kk SP + 1kk
        // gold), so a mastery costs what every other 76 passive costs. Blood Rage is the exception:
        // it is learned at 81 and pays THAT rung — 200kk SP + 25kk gold, which is what he wrote into
        // both warrior files on 2026-09-10 and exactly what `F4New(81)` returns.
        var (sp76, gold76) = F4New(76);
        var (sp81, gold81) = F4New(81);

        return new SkillDef[]
        {
            // ═══════════════════════════════════════════════════════════════════════════════════
            //  OVERPOWER @20 / 40 / 76 — WARRIOR and AoE WARRIOR
            // ═══════════════════════════════════════════════════════════════════════════════════
            //
            // 🔑 THE ONLY THING IN THE GAME THAT LETS A [Double] SKILL DOUBLE. Before this passive
            // every physical skill flagged CanDouble rolled a per-race ATK constant that nothing
            // could raise; since `BL-190` the flag means "eligible" and the rate is the character's,
            // which is to say: this passive's, and nobody else's.
            //
            // ⚠ THE LADDER SPANS THREE CLASS TIERS on purpose — 20 is the 2nd class change, 40 the
            // 3rd, 76 the 4th. That is his own shape ("3,7,10% @20,40,76") and it means the rungs
            // live in THREE different CSV files. `warrior 2nd.csv` is the only one `--check` walks.
            //
            // ⚠ The SP prices are the WARRIOR's own ladder at each tier, read off the neighbouring
            // rows: 3,400 at 20 (Smash / Two-Hand Mastery / HP Boost all charge it), 28,000 at 40
            // (the 3rd tier's first rung everywhere), and the shared 4th-tier price at 76.
            new(Overpower, "Overpower", BaseClass.Fighter, SkillEffect.None,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                // `BL-205` — PASSIVE, not Physical. It is the SKILL-WINDOW GROUP and nothing else:
                // the Known tab heads each block with the category name, so Overpower used to sit
                // among the damage skills while being neither castable nor damage. Owner,
                // 2026-09-11: *"all classes overpowerd/momentum is in buffs group not in passives in
                // the skill window"*.
                // ⚠ It also makes the server refuse to CAST it, which is correct and was already true
                //   in practice: `def.Passive != null` has always kept these off the bar.
                Category: SkillCategory.Passive, SpCost: 3400,
                TargetMode: TargetMode.SelfOnly,
                Passive: new PassiveEffect(DoubleDamageRate: OverpowerBase[0]),
                Levels: new[]
                {
                    new SkillLevel(SpCost: 3400,
                        Passive: new PassiveEffect(DoubleDamageRate: OverpowerBase[0]),
                        Description: "Overpower — your [Double] skills strike twice as hard 3% of the time."),
                    new SkillLevel(SpCost: 28_000,
                        Passive: new PassiveEffect(DoubleDamageRate: OverpowerBase[1]),
                        Description: "Overpower — 7%."),
                    new SkillLevel(SpCost: sp76, GoldCost: gold76,
                        Passive: new PassiveEffect(DoubleDamageRate: OverpowerBase[2]),
                        Description: "Overpower — 10%."),
                },
                Description: "Skills marked [Double] can strike for DOUBLE damage. Without this "
                           + "passive they never do. Scales with your ATK."),

            // ═══════════════════════════════════════════════════════════════════════════════════
            //  `double_mastery` @81 — the WARRIOR's "Overpower Mastery" and the MELEE ROGUE's
            //  "Momentum Mastery". A TOGGLE, and ONE def with two names.
            // ═══════════════════════════════════════════════════════════════════════════════════
            //
            // *"another toggle skill that doubles the effect of the double passive drain 50hp/s and
            //   increases the p.mp.consumtion with 25%"*
            //
            // 🔑 IT DOUBLES EVERY MASTERY BASE THE HOLDER HAS, NOT JUST THE DAMAGE ONE. His
            // 2026-09-10 ruling: *"change blood rage toggle not to double only the overpowered base
            // but all double passives bases that a class have"*, in the same breath as giving the
            // melee rogue a copy. That generalisation is the ONLY reason one def can serve both: the
            // warrior's base is damage (Overpower), the rogue's is reuse (Stab Momentum), and this
            // stance doubles whichever you actually carry. `Entity.RecomputeDerived` multiplies all
            // three accumulators by `MasteryMult`.
            //
            // 🔑 IT MULTIPLIES THE BASES, NOT THE FINISHED RATES — `SkillDef.MasteryMult` folds into
            // the accumulators before the ATK band and before the 25% cap. So a level-81 warrior on
            // Overpower's 10% reads 20% base, then his band, then the cap. Doubling the finished
            // number instead would have skipped the cap entirely.
            //
            // 🔑 AND IT IS WORTH NOTHING WITHOUT A MASTERY PASSIVE: ×2 of a zero base is zero. There
            // is no guard for that and none is wanted — it is the `BL-190` gate working, and someone
            // who has not bought a passive simply pays 50 HP a second for a stance that does nothing.
            //
            // ⚠ `PhysMpCostPct` is NEGATIVE here. The field is a REDUCTION everywhere else in the
            // game (Holy Soul carries +0.30 for "30% cheaper"), and `Entity.PhysMpCostReduction`
            // clamps to [−2, 0.8] precisely so a penalty can ride the same channel. His "p.mp" is
            // the PHYSICAL side — the magic channel is deliberately untouched, because a warrior
            // casting a magic skill is not what this stance is about.
            //
            // 🔑 THE LEVEL IS 81 AND IT IS HIS — and the REASON is the rate ramp, not the price.
            // He wrote 81 into both warrior CSVs on 2026-09-10 and then said why in as many words:
            //
            //   *"i dont want at 76 lvl warrior to start doubling at 25% .. until 81 he is at base
            //     10% .. at 81 then gets the toggle and become stronger"*
            //   *"well some toggles will cost 50 hp .. doesnt matter if its 76/78/81 .. etc lvl"*
            //
            // 🔑 SO THE FIVE LEVELS ARE A PLATEAU, DELIBERATELY. Overpower's last rung lands at 76
            // and the warrior sits on a 10% base (13% after a high-ATK band) for five levels. At 81
            // the toggle doubles the base to 20%, which the band carries to 26% and `StatCaps`
            // trims to 25 — the ceiling, reached exactly once, at the end. Had this shipped at 76
            // the warrior would have hit the cap the day he ascended and had nothing left to climb.
            //
            // ⚠ THE NUMBER 81 ITSELF IS NOT SACRED ("doesnt matter if its 76/78/81"); THE ORDER IS.
            // If it ever moves, it moves LATER than Overpower's top rung, never onto it. The price
            // follows the level for free: `F4New` reads the shared ladder, so 81 charges 200kk SP +
            // 25kk gold, which is what he authored in the CSV to the digit.
            new(DoubleMastery, "Overpower Mastery", BaseClass.Fighter, SkillEffect.None,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                BuffKey: "double_mastery", Rank: 1,
                Category: SkillCategory.Buff, SpCost: sp81,
                TargetMode: TargetMode.SelfOnly,
                Toggle: true, CountsTowardBuffLimit: false,
                HpPerSecond: 50,
                MasteryMult: 2f,
                PhysMpCostPct: -0.25f,
                Levels: new[] { new SkillLevel(SpCost: sp81, GoldCost: gold81) },
                Description: "Stance. Your skill-mastery chances are DOUBLED, but every physical "
                           + "skill costs 25% more MP and you burn 50 HP a second."),

            // ═══════════════════════════════════════════════════════════════════════════════════
            //  LASTING ENCHANTMENT @76 — BUFFER and HEALER
            // ═══════════════════════════════════════════════════════════════════════════════════
            //
            // *"buffers and healers get the duration passive with base 10% @76"*
            //
            // 🔑 IT COVERS BOTH SIGNS — *"doubles duration of bad and good buffs"*. One roll per
            // cast, so an area blessing doubles for everyone in it or for nobody, and a debuff you
            // land is the same coin. It is IG's level-76 Skill Mastery, and until `BL-190` it was
            // free, universal, and rolled off the DAMAGE curve.
            //
            // ⚠ BaseClass.Mage covers healer and buffer both — they are one archetype (Healer) that
            // splits into Lightbringer and Warchanter at 40, so ONE def serves both disciplines and
            // the learn tables decide who gets it.
            new(LastingEnchantment, "Lasting Enchantment", BaseClass.Mage, SkillEffect.None,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                // `BL-205` — PASSIVE, not Buff. See Overpower above: the skill window groups by this
                // field, and a passive listed under "Buffs" reads as something you cast.
                Category: SkillCategory.Passive, SpCost: sp76,
                TargetMode: TargetMode.SelfOnly,
                Passive: new PassiveEffect(DoubleDurationRate: 0.10f),
                Levels: new[]
                {
                    new SkillLevel(SpCost: sp76, GoldCost: gold76,
                        Passive: new PassiveEffect(DoubleDurationRate: 0.10f)),
                },
                Description: "Buffs and debuffs you cast have a chance to last TWICE as long. "
                           + "Scales with your ATK."),

            // ═══════════════════════════════════════════════════════════════════════════════════
            //  `reuse_reset_momentum` @76 — the MAGUS's "Arcane Momentum" and the MELEE ROGUE's
            //  "Stab Momentum". ONE def, two names.
            // ═══════════════════════════════════════════════════════════════════════════════════
            //
            // 🔑 THE ID IS THE MECHANIC, NOT THE FLAVOUR — that is exactly why he renamed it off
            // `arcane_momentum` on 2026-09-10: *"as other classes can aqure it too"*. A second class
            // arrived in the same message. Anything else that ever gets a reuse reset gets THIS id
            // plus a `DisplayName`, never a copy of the def.
            //
            //
            // *"Mages get cooldown passive with base 5% @76"* — and, from the same message,
            // *"u for now build the reuse passive and add it to the mages but the other skills
            //   tommorow"*, while `nuker 4th.csv` is still being written.
            //
            // 🔑 "MAGE" IS THE NUKER, not every caster. His own vocabulary throughout `shared
            // 4th.csv` separates Mage / Healer / Buffer / Warrior / Tank / Rogue sigils, and this
            // message gives the healer and buffer a DIFFERENT passive in the line above.
            //
            // ⚠ 5% IS THE LOWEST BASE OF THE THREE AND THAT IS RIGHT: a reset is worth far more per
            // hit than a ×2 is. A nuker's rotation is one big spell on a long reuse, so 5% of casts
            // coming back instantly is roughly a 5% damage increase with no cast time attached.
            //
            // ⚠ It never fires on a FixedCooldown skill (Return, the ultimates) — that exemption is
            // in ExecuteSkill, not here, and it is the same one reuse REDUCTION already has.
            new(ReuseResetMomentum, "Arcane Momentum", BaseClass.Mage, SkillEffect.None,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                // `BL-205` — PASSIVE, not Buff. See Overpower above. ⚠ NOT `double_mastery`, which
                // stays a Buff on purpose: that one is a TOGGLE the player switches on, so it needs a
                // bar slot and belongs with the stances.
                Category: SkillCategory.Passive, SpCost: sp76,
                TargetMode: TargetMode.SelfOnly,
                Passive: new PassiveEffect(CooldownResetRate: 0.05f),
                Levels: new[]
                {
                    new SkillLevel(SpCost: sp76, GoldCost: gold76,
                        Passive: new PassiveEffect(CooldownResetRate: 0.05f)),
                },
                Description: "A skill you cast has a chance to come off reuse immediately. "
                           + "Scales with your ATK."),
        };
    }
}
