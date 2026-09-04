namespace Game.Shared;

/// <summary>THE WARCHANTER'S 3rd-CLASS BUFF LAYER, 40-74 — every group, party echo and Harmony
/// rung of <c>docs/data/classes_skills_csv/buffer 3rd.csv</c> rows 1-185 (owner, 2026-08-21:
/// *"I managed to do all the buffs all the harmonies and all the group buffs to lvl 74"*).
/// The singles themselves are NOT here: they are the ordinary ladder families in
/// Skills.BuffLadders.cs, and the buffer simply learns rungs of them — his buffer singles are
/// the healer's ladder, value for value. Who learns what, and when, is in
/// RaceAndClasses/ClassSkillTables.Third.cs.
///
/// <para>🔑 <b>THE GROUPS ARE SPLIT BY LANE NOW</b> (owner, opening this session: *"not to mix
/// magic with fighters buff .. to be like the haromonies several for fighter several for mage and
/// several combined(defences)"*). His naming carries the lane: <b>Feral*</b> = fighter,
/// <b>Arcane*</b> = mage, <b>Arcane and Feral*</b> = both, and the neutral survivability pair
/// (<c>Body/Soul Reinforcement</c>, <c>Shield Reinforcement</c>, <c>Wind Grace</c>) is combined.
/// The five OLD groups — Might and Bulwark, Force and Ward, Focus and Ferocity, Body and Soul,
/// Swift and Sure — each mixed the two channels in one cast and are no longer granted to anybody.
/// ⚠ Their defs survive on purpose (deleting one orphans every character who bought it), exactly
/// as the 2026-08-10 purge treated the fighter kits. Do not re-grant them.</para>
///
/// <para>🔑 <b>THE MP COLUMN IS SELF-CHECKING — USE IT.</b> Row 2 of his file states the rule:
/// *"Each max lvl buff of the group MP + the group learned lvl MP cost"*, i.e.
/// <c>groupMp = Σ(each child single's TOP-rung MP) + the band MP at the group's own learn level</c>.
/// All eleven groups and party echoes below satisfy it to the MP, and it is how three copy-paste
/// slips were caught while reading the file (a wrong REPLACES list, a wrong DESCR and a wrong SP
/// all survived proof-reading; the arithmetic did not). <b>If you ever retune a single's top rung,
/// re-derive every group that contains it</b> — the check is only as good as the sum.</para>
///
/// <para>🔑 <b>A HARMONY IS NOT A GROUP.</b> It keeps its own <c>BuffKey</c>, covers no family and
/// evicts nothing — harmonies MULTIPLY on top of the basic layer, which is the whole reason the
/// tier exists (see docs/design/BuffLadders.md and the rejected `buffer_auto 3rd.md` draft). What
/// changed on 2026-08-21 is their SHAPE: they are now <b>5-minute buffs on a 2-minute reuse</b>,
/// not 20-minute ones. His reasoning, verbatim: *"its not a buffs they are additional support …
/// The idea is the buffer is a must .. not enter party buffs get kicked for 20 mins ... need to
/// stay and rebuff thats his job"*. IG's own is 2 minutes; 5 is the compromise.</para>
///
/// <para>🔑 <b>HARMONY MP IS <c>60 × 1.1^i</c> PER BUFF INSIDE</b>, summed — 60 / 66 / 73 / 80 /
/// 88 / 97, so the ladder totals run 60 / 126 / 199 / 279 / 367 / 464. A harmony rung that gains
/// an effect gains the next term. ⚠ The 4-effect rung was authored as 379 in two ladders before
/// being corrected to 279: it is the one value that makes the ladder run BACKWARDS (379 → 367),
/// which is how it was found. Ladders are monotonic — always.</para>
///
/// <para>⚠ <b>Harmony of the Wizard stops at 52 and Harmony of Speed at 58 ON PURPOSE.</b> Owner:
/// *"The speed one stops - no more buffs for it; Wizard continue in Buffer 4th"*. The Wizard's
/// +20% MP regen and −30% magic MP cost, which the old single-rung def carried, are deliberately
/// NOT here — they move to the 4th-class ladder, and the MP-cost half is Mana Blessing's job now.
/// Do not "restore" them.</para></summary>
public static partial class SkillCatalog
{
    // ---- The nine improved GROUPS, by lane. Ids are append-only; the `wc_` prefix marks the
    //      Warchanter's own kit the same way `holy_` marks the Lightbringer's. ----
    public const string WcFeralPrecision   = "wc_feral_precision";    // fighter: crit rate + crit dmg + accuracy
    public const string WcFeralBloodlust   = "wc_feral_bloodlust";    // fighter: P.Atk + attack speed + vampirism
    public const string WcArcaneInsight    = "wc_arcane_insight";     // mage:    M.Atk + magic crit
    public const string WcArcaneSerenity   = "wc_arcane_serenity";    // mage:    cast speed + interrupt + MP regen
    public const string WcSoulReinforce    = "wc_soul_reinforcement"; // mage:    Max MP + M.Def + MP cost
    public const string WcBodyReinforce    = "wc_body_reinforcement"; // combined: Max HP + P.Def + HP regen
    public const string WcShieldReinforce  = "wc_shield_reinforcement"; // tank:  shield P.Def + block chance
    public const string WcArcaneFeralProt  = "wc_arcane_feral_protection"; // combined: both CC resists
    public const string WcWindGrace        = "wc_wind_grace";         // combined: move speed + evasion

    // ---- Two of the three PARTY ECHOES: a single-target buff the buffer re-learns as a party cast.
    //      Not groups — each hands out the SAME thing its single does, to everyone in radius.
    //      (The third is War Frenzy, which is `Madness` renamed and lives in Skills.Healer.cs.) ----
    public const string WcWarMight   = "wc_war_might";
    public const string WcWarBulwark = "wc_war_bulwark";

    /// <summary>The fourth Harmony (the other three keep their original `npc_harmony_*` ids, which
    /// are append-only and predate this file).</summary>
    public const string WcHarmonySpeed = "wc_harmony_speed";

    /// <summary>A hidden top rung on Mana Blessing's OWN buff key, so `Soul Reinforcement` can name
    /// it as a child and therefore COVER it.
    ///
    /// <para>🔑 Why this exists at all: a group covers a family by naming a child that sits on that
    /// family's key, and <see cref="ManaBlessing"/> is not a ladder family — it is a standalone buff
    /// with its own key and its own three levels. Without a child on `mana_blessing`, the group
    /// would carry the MP-cost payload in its own fields and NOT evict the single, so an ally could
    /// wear both and get −40% physical cost. That is the double-dip the group mechanism exists to
    /// prevent, and his row says the group REPLACES Mana Blessing.</para>
    ///
    /// <para>⚠ Not learnable and never granted directly — it is reachable only as a child. Its
    /// numbers are Mana Blessing's level 3, verbatim; if that rung is ever retuned, retune this
    /// too or the group and the single will silently disagree.</para></summary>
    public const string BuffManaBless3 = "buff_mana_blessing_3";

    // ---------------------------------------------------------------------------------------
    //  Factories
    // ---------------------------------------------------------------------------------------

    /// <summary>An improved GROUP: one buff carrying every child's magnitudes, covering each
    /// child's family at GROUP rank (so it evicts those singles and refuses anything weaker
    /// afterwards) and collapsing them off the learn list via <c>Replaces</c>. 20 minutes, party,
    /// 1s cast / 1s reuse — his columns for every group row.</summary>
    /// <param name="fourth">His `buffer 4th.csv` 76-90 rungs, if this group has any (`BL-108`). Two
    /// do — Soul Reinforcement and Arcane and Feral Protection — and both ladder a FIELD (MP cost, CC
    /// resistance) rather than a child, so rung 1 is written out here as the plain group and every
    /// rung above it states its own numbers. A level with no <c>ChildBuffs</c> inherits the def's, so
    /// the +35% Max MP and +30% M.Def keep coming from the same two children at every rung.</param>
    private static SkillDef WcGroup(string id, string name, SkillEffect effect,
        string[] children, string[] replaces, int mp, int sp, string desc,
        SkillLevel[]? fourth = null) =>
        new(id, name, BaseClass.Mage, effect,
            MpCost: mp, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, ChildBuffs: children,
            Category: SkillCategory.Buff, SpCost: sp,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: replaces,
            Levels: fourth is null ? null
                : new[] { new SkillLevel(MpCost: mp, SpCost: sp, Description: desc) }
                    .Concat(fourth).ToArray(),
            Description: desc + " Blesses you and nearby allies for 20 minutes.");

    /// <summary>A HARMONY rung. Own key, covers nothing, stacks on top of everything — 5 minutes
    /// on a 2-minute reuse (owner 2026-08-21). <paramref name="mags"/> is the CUMULATIVE payload
    /// at this rung: a harmony level does not add to the one below it, it replaces it.</summary>
    private static SkillLevel HarmonyRung(int mp, int sp, EffectMagnitude[] mags, string desc,
        float physMpCost = 0f, float magicMpCost = 0f) =>
        new(MpCost: mp, SpCost: sp, Magnitudes: mags, Description: desc + " (5 minutes).",
            PhysMpCostPct: physMpCost, MagicMpCostPct: magicMpCost);

    /// <param name="replaces">`BL-160` — the NPC buffer's SINGLE harmonies this one contains. A
    /// harmony carries `Magnitudes`, not `ChildBuffs`, so it is not a "group" in the engine's sense and
    /// cannot cover a family automatically; `Replaces` is what makes the owner's rule true —
    /// *"his acts as a group one so replaces them"*. Casting Harmony of Protection tears out the
    /// player's bought Harmony of Ward / Bulwark / Body and takes ONE bar slot instead of three, which
    /// is exactly his argument: *"at 56 mine is already 1 space 2 buffs .. its strategy"*.
    ///
    /// ⚠ It removes them; it does not BLOCK them. Buy a single harmony after a Warchanter has already
    /// blessed you and it still lands, redundantly, for 50k. That is the player's mistake to make and
    /// the same shape as buying a potion over a group.</param>
    private static SkillDef WcHarmony(string id, string name, string buffKey,
        SkillEffect effect, SkillLevel[] levels, string desc, string[]? replaces = null) =>
        new(id, name, BaseClass.Mage, effect,
            MpCost: levels[0].MpCost, CastTicks: 10, CooldownTicks: 1200, Range: 600, Power: 0,
            DurationTicks: 3000, BuffKey: buffKey, Rank: NpcBuffRank,
            Category: SkillCategory.Buff, SpCost: levels[0].SpCost,
            Magnitudes: levels[0].Magnitudes,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: replaces,
            Levels: levels,
            Description: desc);

    private static SkillDef[] Warchanter3rdSkills() => new SkillDef[]
    {
        // ═══ THE FIGHTER LANE ════════════════════════════════════════════════════════════════
        // MP 340 = Focus 80 + Ferocity 85 + Aim 85, + the 90 his band charges at level 58.
        WcGroup(WcFeralPrecision, "Feral Precision",
            SkillEffect.BuffCritRate | SkillEffect.BuffCritDamage | SkillEffect.BuffAccuracy,
            new[] { Rung(FamCritRate, 6), Rung(FamCritDmg, 6), Rung(FamAccuracy, 4) },
            new[] { CastId(FamCritRate), CastId(FamCritDmg), CastId(FamAccuracy) },
            mp: 340, sp: 88000,
            "+30% critical rate, +35% critical damage, +4 accuracy."),

        // MP 395 = Might 60 + Fury 80 + Vampirism 125, + 130 at level 74.
        WcGroup(WcFeralBloodlust, "Feral Bloodlust",
            SkillEffect.BuffPhysAtk | SkillEffect.BuffAtkSpeed | SkillEffect.BuffMeleeVamp,
            new[] { Rung(FamPhysAtk, 3), BuffHasteR, Rung(FamVamp, 5) },
            new[] { CastId(FamPhysAtk), CastId(FamAs), CastId(FamVamp) },
            mp: 395, sp: 880000,
            "+15% P.Atk, +33% attack speed, 9% melee vampirism."),

        // ═══ THE MAGE LANE ═══════════════════════════════════════════════════════════════════
        // MP 325 = Force 80 + Insight 120, + 125 at level 72.
        WcGroup(WcArcaneInsight, "Arcane Insight",
            SkillEffect.BuffMagAtk | SkillEffect.BuffMagicCritRate,
            new[] { Rung(FamMagAtk, 4), Rung(FamMagCrit, 6) },
            new[] { CastId(FamMagAtk), CastId(FamMagCrit) },
            mp: 325, sp: 650000,
            "+32% M.Atk and double magic critical rate."),

        // MP 395 = Alacrity 75 + Resolve 115 + Serenity 85, + 120 at level 70.
        WcGroup(WcArcaneSerenity, "Arcane Serenity",
            SkillEffect.BuffCastSpeed | SkillEffect.BuffInterruptResist | SkillEffect.BuffMpRegen,
            new[] { BuffAlacrityR, Rung(FamInterrupt, 7), Rung(FamMpRegen, 6) },
            new[] { CastId(FamCast), CastId(FamInterrupt), CastId(FamMpRegen) },
            mp: 395, sp: 170000,
            "+30% cast speed, +54% interrupt resistance, +20% MP regeneration."),

        // MP 455 = Ward 80 + Soul 120 + Mana Blessing 125, + 130 at level 74.
        // ⚠ The third child is the HIDDEN mana-blessing rung, not a family — see BuffManaBless3.
        WcGroup(WcSoulReinforce, "Soul Reinforcement",
            SkillEffect.BuffMp | SkillEffect.BuffMagicDef,
            new[] { Rung(FamMaxMp, 6), Rung(FamMagDef, 4), BuffManaBless3 },
            new[] { CastId(FamMaxMp), CastId(FamMagDef), ManaBlessing },
            mp: 455, sp: 880000,
            "+35% Max MP, +30% M.Def, and −20% physical / −10% magic skill MP cost.",
            BufferFourthSoulRungs()),

        // ═══ THE COMBINED LANE ═══════════════════════════════════════════════════════════════
        // MP 402 = Body 120 + Bulwark 72 + Vigor 85, + 125 at level 72. That sum is the ONLY
        // reading that works, which is how the row's own REPLACES list (copied from Soul
        // Reinforcement) and its DESCR (which said "Max MP" and omitted P.Def) were both caught.
        WcGroup(WcBodyReinforce, "Body Reinforcement",
            SkillEffect.BuffHp | SkillEffect.BuffDef | SkillEffect.BuffHpRegen,
            new[] { Rung(FamMaxHp, 6), Rung(FamPhysDef, 3), Rung(FamHpRegen, 6) },
            new[] { CastId(FamMaxHp), CastId(FamPhysDef), CastId(FamHpRegen) },
            mp: 402, sp: 650000,
            "+35% Max HP, +15% P.Def, +20% HP regeneration."),

        // MP 375 = Shield Blessing 120 + Shield Hardening 125, + 130 at level 74.
        // 🔑 Both numbers are a PERCENT of what the shield already carries, so this is self-gating:
        // an ally with no shield has 0 block chance and 0 shield defence, and 0 × 1.5 is still 0.
        WcGroup(WcShieldReinforce, "Shield Reinforcement",
            SkillEffect.BuffShieldDef | SkillEffect.BuffBlockChance,
            new[] { Rung(FamShieldDef, 3), Rung(FamShieldBlock, 6) },
            new[] { CastId(FamShieldDef), CastId(FamShieldBlock) },
            mp: 375, sp: 880000,
            "+50% shield P.Def and +30% block chance. Does nothing for anyone not carrying a shield."),

        // MP 340 = Clarity 85 + Fortitude 125, + 130 at level 74.
        WcGroup(WcArcaneFeralProt, "Arcane and Feral Protection",
            SkillEffect.None,
            new[] { Rung(FamCcResMag, 4), Rung(FamCcResPhys, 4) },
            new[] { CastId(FamCcResMag), CastId(FamCcResPhys) },
            mp: 340, sp: 880000,
            "50% resistance to SPT-defended debuffs and 40% to CON-defended ones.",
            BufferFourthArcaneFeralRungs()),

        // MP 198 = Swift 33 (the cleric's level-30 rung) + Agility 80, + 85 at level 56. That the
        // sum only closes with the CLERIC's Swift is what identifies the two children: the buffer
        // file authors no Swift row of its own.
        WcGroup(WcWindGrace, "Wind Grace",
            SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion,
            new[] { BuffSwiftR, BuffAgilityR },
            new[] { CastId(FamMove), CastId(FamEva) },
            mp: 198, sp: 38000,
            "+33 Move Speed and +4 Evasion."),

        // ═══ THE PARTY ECHOES ════════════════════════════════════════════════════════════════
        //
        // 🔑 NOT groups. Each hands out exactly what its single hands out, to the whole party, and
        // competes on the same key — so the party version simply replaces the single-target one
        // rather than stacking with it. That is why they carry ONE child and no CoveredKeys.
        //
        // ⚠ WAR FRENZY IS NOT HERE. It is `Madness` renamed and moved down to 56 (owner 2026-08-21),
        // so it keeps its original `madness` id and stays in Skills.Healer.cs where it has always
        // lived. An early draft of this file invented a second `wc_war_frenzy` for it — that would
        // have been two skills for one concept, with the old one orphaned. See the note on its def.
        // MP 255 = Great Might 125 + 130 at level 74. ⚠ RANK 1, deliberately the SAME rank as the
        // single-target pair: Great Might and Great Bulwark share one key so an ally wears one or
        // the other, never both, and that choice has to stay re-makeable. At equal rank ApplyBuff
        // keeps the LONGER remaining time, so a fresh cast of either always wins — which is exactly
        // the swap behaviour. Giving the party version a higher rank would have LOCKED the party
        // into whichever half was cast first until it expired.
        new(WcWarMight, "War Might", BaseClass.Mage, SkillEffect.BuffPhysAtk,
            MpCost: 255, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: GreatBlessingKey, Rank: 1,
            Category: SkillCategory.Buff, SpCost: 880000,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffPhysAtk, 0.10f) },
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { GreatMight },
            Description: "+10% P.Atk for the whole party, on top of Might, for 20 minutes. Does not "
                       + "stack with War Bulwark or Great Bulwark — an ally carries one, never both."),

        new(WcWarBulwark, "War Bulwark", BaseClass.Mage, SkillEffect.BuffDef,
            MpCost: 255, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: GreatBlessingKey, Rank: 1,
            Category: SkillCategory.Buff, SpCost: 880000,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffDef, 0.15f) },
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { GreatBulwark },
            Description: "+15% P.Def for the whole party, on top of Bulwark, for 20 minutes. Does not "
                       + "stack with War Might or Great Might — an ally carries one, never both."),

        // ═══ THE HIDDEN MANA-BLESSING RUNG ═══════════════════════════════════════════════════
        // Mana Blessing level 3, verbatim, on Mana Blessing's own key so a group can cover it.
        // Never learnable, never granted — reachable only as Soul Reinforcement's third child.
        new(BuffManaBless3, "Mana Blessing", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 12000, BuffKey: "mana_blessing", Rank: 3,
            Category: SkillCategory.Buff,
            PhysMpCostPct: 0.20f, MagicMpCostPct: 0.10f,
            Description: "−20% physical and −10% magic skill MP cost."),

        // ═══ THE HARMONIES ═══════════════════════════════════════════════════════════════════
        //
        // Only the FOURTH one is defined here — Protection, Warrior and Wizard keep their original
        // `npc_harmony_*` ids (append-only) and are laddered in place in Skills.Buffer.cs, where
        // they have always lived.
        //
        // Two rungs and it STOPS (owner: *"The speed one stops - no more buffs for it"*). It is the
        // only harmony that never reaches 74, and that is deliberate, not an unfinished ladder.
        WcHarmony(WcHarmonySpeed, "Harmony of Speed", "harmony_speed",
            SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion,
            new[]
            {
                HarmonyRung(60, 64000,
                    new EffectMagnitude[] { new(SkillEffect.BuffMoveSpeed, 20, ModifierMode.Flat) },
                    "+20 Move Speed for you and nearby allies"),
                HarmonyRung(126, 88000,
                    new EffectMagnitude[]
                    {
                        new(SkillEffect.BuffMoveSpeed, 20, ModifierMode.Flat),
                        new(SkillEffect.BuffEvasion, 3, ModifierMode.Flat),
                    },
                    "+20 Move Speed and +3 Evasion for you and nearby allies"),
            },
            "Quickens you and nearby allies. Stacks on top of Swift and Agility.",
             replaces: new[] { NpcHSwift }),

        // ── HARMONY OF PROTECTION — 5 rungs @44/52/56/66/74 ──────────────────────────────────
        // The defensive harmony, and the only one that reaches its final effect at 74. Reflect is
        // last on purpose: it is the rung that makes a tank's party genuinely different, not just
        // sturdier. ⚠ Its magnitudes are CUMULATIVE per rung — read the whole array, not the tail.
        WcHarmony(NpcHarmonyProtection, "Harmony of Protection", "harmony_protection",
            SkillEffect.BuffMagicDef | SkillEffect.BuffHpRegen | SkillEffect.BuffDef
            | SkillEffect.BuffHp | SkillEffect.BuffReflect,
            new[]
            {
                HarmonyRung(60, 43000, new EffectMagnitude[]
                    { new(SkillEffect.BuffMagicDef, 0.30f) },
                    "+30% M.Def"),
                HarmonyRung(126, 74000, new EffectMagnitude[]
                    { new(SkillEffect.BuffMagicDef, 0.30f), new(SkillEffect.BuffHpRegen, 0.20f) },
                    "+30% M.Def, +20% HP regeneration"),
                HarmonyRung(199, 81000, new EffectMagnitude[]
                    { new(SkillEffect.BuffMagicDef, 0.30f), new(SkillEffect.BuffHpRegen, 0.20f),
                      new(SkillEffect.BuffDef, 0.25f) },
                    "+30% M.Def, +20% HP regeneration, +25% P.Def"),
                HarmonyRung(279, 280000, new EffectMagnitude[]
                    { new(SkillEffect.BuffMagicDef, 0.30f), new(SkillEffect.BuffHpRegen, 0.20f),
                      new(SkillEffect.BuffDef, 0.25f), new(SkillEffect.BuffHp, 0.30f) },
                    "+30% M.Def, +20% HP regeneration, +25% P.Def, +30% Max HP"),
                HarmonyRung(367, 880000, new EffectMagnitude[]
                    { new(SkillEffect.BuffMagicDef, 0.30f), new(SkillEffect.BuffHpRegen, 0.20f),
                      new(SkillEffect.BuffDef, 0.25f), new(SkillEffect.BuffHp, 0.30f),
                      new(SkillEffect.BuffReflect, 0.20f) },
                    "+30% M.Def, +20% HP regeneration, +25% P.Def, +30% Max HP, reflects 20% of melee damage"),
            // Rung 6, his `buffer 4th.csv` @76 (`BL-108`) — the same five lines plus bow resistance.
            }.Concat(BufferFourthProtectionRungs()).ToArray(),
            "Shields you and nearby allies. Stacks on top of every ordinary defensive buff.",
             replaces: new[] { NpcHWard, NpcHBulwark, NpcHBody }),

        // ── HARMONY OF THE WARRIOR — 6 rungs @40/44/48/56/58/74 ──────────────────────────────
        //
        // 🔑 CRIT RATE IS +100% HERE, NOT +75% (owner, 2026-08-21). The old single-rung def carried
        // 0.75f from 2026-07-03, a month BEFORE his crit model landed, and it was never reconciled:
        // the worked ladder in docs/design/CritBlowAndDouble.md §5 uses "Harmony x2" at every line
        // (dagger 132 x1.3 x1.5 x2 = 514 -> capped 500; bow 205 x2 = 410; sword 88 x1.3 x2 = 228).
        // Every OTHER multiplier in that chain already matched the code — Focus x1.30, the rogue
        // passives x1.20/x1.50, the 3:2:1 weapon factors — so Harmony was the lone survivor of the
        // old numbers. At x2 a maxed melee rogue reaches the 50% cap, which is what the cap is FOR;
        // at x1.75 he stopped at 45% and nothing in the game ever touched the ceiling.
        // ⚠ TestChecklist.Unity.md §52d still asserts the x1.75 figure ("-> 36%"); it is 41% now.
        WcHarmony(NpcHarmonyWarrior, "Harmony of the Warrior", "harmony_warrior",
            SkillEffect.BuffCritRate | SkillEffect.BuffCritDamage | SkillEffect.BuffAccuracy
            | SkillEffect.BuffPhysAtk | SkillEffect.BuffAtkSpeed | SkillEffect.BuffMeleeVamp,
            new[]
            {
                HarmonyRung(60, 36000, new EffectMagnitude[]
                    { new(SkillEffect.BuffCritRate, 1.00f) },
                    "double critical rate"),
                HarmonyRung(126, 43000, new EffectMagnitude[]
                    { new(SkillEffect.BuffCritRate, 1.00f), new(SkillEffect.BuffCritDamage, 0.35f) },
                    "double critical rate, +35% critical damage"),
                HarmonyRung(199, 64000, new EffectMagnitude[]
                    { new(SkillEffect.BuffCritRate, 1.00f), new(SkillEffect.BuffCritDamage, 0.35f),
                      new(SkillEffect.BuffAccuracy, 4, ModifierMode.Flat) },
                    "double critical rate, +35% critical damage, +4 accuracy"),
                HarmonyRung(279, 81000, new EffectMagnitude[]
                    { new(SkillEffect.BuffCritRate, 1.00f), new(SkillEffect.BuffCritDamage, 0.35f),
                      new(SkillEffect.BuffAccuracy, 4, ModifierMode.Flat),
                      new(SkillEffect.BuffPhysAtk, 0.12f) },
                    "double critical rate, +35% critical damage, +4 accuracy, +12% P.Atk"),
                HarmonyRung(367, 88000, new EffectMagnitude[]
                    { new(SkillEffect.BuffCritRate, 1.00f), new(SkillEffect.BuffCritDamage, 0.35f),
                      new(SkillEffect.BuffAccuracy, 4, ModifierMode.Flat),
                      new(SkillEffect.BuffPhysAtk, 0.12f), new(SkillEffect.BuffAtkSpeed, 0.15f) },
                    "double critical rate, +35% critical damage, +4 accuracy, +12% P.Atk, +15% attack speed"),
                HarmonyRung(464, 880000, new EffectMagnitude[]
                    { new(SkillEffect.BuffCritRate, 1.00f), new(SkillEffect.BuffCritDamage, 0.35f),
                      new(SkillEffect.BuffAccuracy, 4, ModifierMode.Flat),
                      new(SkillEffect.BuffPhysAtk, 0.12f), new(SkillEffect.BuffAtkSpeed, 0.15f),
                      new(SkillEffect.BuffMeleeVamp, 0.08f) },
                    "double critical rate, +35% critical damage, +4 accuracy, +12% P.Atk, "
                    + "+15% attack speed, 8% melee vampirism"),
            },
            "Drives you and nearby allies into a fighting song. Stacks on top of Focus and Ferocity.",
            replaces: new[] { NpcHMight, NpcHFury }),

        // ── HARMONY OF THE WIZARD — 2 rungs @48/52, and it STOPS ─────────────────────────────
        // ⚠ The old def also carried +20% MP regen and a −30% magic-skill MP cost. Both are GONE
        // from the 3rd tier by his ruling — the MP-cost half is Mana Blessing's job now, and the
        // regen half is on the 4th-class ladder (`buffer 4th.csv` @77). Do not restore them here.
        WcHarmony(NpcHarmonyWizard, "Harmony of the Wizard", "harmony_wizard",
            SkillEffect.BuffMagAtk | SkillEffect.BuffCastSpeed,
            new[]
            {
                HarmonyRung(60, 64000, new EffectMagnitude[]
                    { new(SkillEffect.BuffMagAtk, 0.10f) },
                    "+10% M.Atk"),
                HarmonyRung(126, 74000, new EffectMagnitude[]
                    { new(SkillEffect.BuffMagAtk, 0.10f), new(SkillEffect.BuffCastSpeed, 0.30f) },
                    "+10% M.Atk, +30% cast speed"),
            // 🔴 "IT STOPS" WAS TRUE OF THE 3rd TIER ONLY. The comment above says the MP-regen half is
            //    on the 4th-class ladder @77, and this is that ladder arriving: rungs 3-5 at 77/78/79
            //    (`BL-108`), adding MP regen, then magic crit rate, then magic crit damage.
            }.Concat(BufferFourthWizardRungs()).ToArray(),
            "Sharpens the casters around you. Stacks on top of Force and Alacrity.",
            replaces: new[] { NpcHForce, NpcHAlacrity }),
    };
}
