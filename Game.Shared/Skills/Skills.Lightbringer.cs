namespace Game.Shared;

/// <summary>Lightbringer — the 3rd-class Healer discipline, levels 40-74, authored end to end in
/// <c>docs/data/classes_skills_csv/healer 3rd.csv</c> and built on 2026-08-20 when he said go.
///
/// <para>🔑 <b>THE FILE IS THE SKILL DATA.</b> Every number below is his; nothing here is interpolated
/// and nothing is invented. A first pass mirrored a dozen flat rungs and dipping prices verbatim and
/// reported them; **he ruled on all of them the same day** and both sides were corrected together, so
/// the file and the code now agree AND every ladder is monotonic. His four rules, worth keeping:
/// Resurrection (and Restore Mana) SP = the BUFF ladder · Antidote SP = the COMBAT band ladder and its
/// cure rank = one band behind the Elf's Healer Blessing · a buff's MP = the standard price at the level
/// it is learned · <b>a rung whose description repeats the one below it is a broken DESCRIPTION, and a
/// rung whose description rises while its MP does not is a broken MP.</b>
/// The one thing that is NOT verbatim is the shape: a CSV row says "learn Ferocity at 48 for
/// +30% crit damage", and the code expresses that as *rung 5 of the crit-damage family*, because a buff
/// that competes has one number line (docs/design/BuffLadders.md). Those live in Skills.BuffLadders.cs,
/// not here; this file holds the discipline's OWN spells.</para>
///
/// <para>Three races, one job. The split is deliberately narrow and it happens TWICE — once on the fast
/// heal (Human throughput / Elf heal-and-cure / Ork planted totem) and once on the control debuff
/// (Gravity / Bind / Armor Break). Everything else in the kit is shared.</para>
///
/// <para>(Who learns these, and when, is in RaceAndClasses/ClassSkillTables.Third.cs.)</para></summary>
public static partial class SkillCatalog
{
    // ---- Skills invented before his file existed. Their DEFS stay in the catalog (deleting a def is
    //      what the old orphan warnings were about) but NOTHING GRANTS THEM any more: his 40-74 rows
    //      are the discipline now, and none of these is on one. Do not re-add them to the class table.
    //      `LbElfWarden` in particular duplicated his Bind, which is what the 2026-08-17 note flagged.
    public const string LbBlessing = "lb_blessing";
    public const string LbDevotion = "lb_devotion";   // passive
    public const string LbHumanPurify = "lb_human_purify"; // cleanse an ally
    public const string LbElfWarden = "lb_elf_warden";     // root enemy + self de-taunt
    public const string LbOrkSap = "lb_ork_sap";           // anti-heal debuff

    // ---- The three per-race heals. Their IDS ARE REUSED, not retired: each of his rows occupies the
    //      exact slot (race + level 40 + role) an invented skill already held, so a character who
    //      learned one keeps a working skill instead of an orphan.
    public const string LbHumanMend = "lb_human_mend";     // → Quick Great Heal
    public const string LbElfDawn = "lb_elf_dawn";         // → Healer Blessing
    public const string LbOrkFont = "lb_ork_font";         // → Healing Totem
    // ---- Shared upgrades: each REPLACES its 2nd-class original rather than stacking beside it.
    public const string HolyRay = "holy_ray";              // replaces Holy Bolt
    public const string GreatHeal = "great_heal";          // replaces Heal
    public const string PartyGreatHeal = "party_great_heal"; // replaces Party Heal
    public const string Conceal = "conceal";               // self-only mob stealth
    // His 2026-08-20 split of the two shared cleric masteries — the healer's halves (the BUFFER keeps
    // continuing the originals). Both REPLACE their cleric original at 40.
    public const string HealerWeaponMasterySkill = "healer_weapon_mastery"; // replaces Spell Mastery
    public const string HealerArmorMasterySkill = "healer_armor_mastery";   // replaces Armor Mastery
    // One control debuff per race, all contested ATK vs SPT.
    public const string LbHumanGravity = "lb_human_gravity";  // slows attack + cast
    public const string LbElfBind = "lb_elf_bind";            // 30s hold
    public const string LbOrkArmorBreak = "lb_ork_armor_break"; // shreds P.Def / M.Def

    // ---- The 44+ kit, all new ids (2026-08-20) ----
    public const string UrgentHeal = "urgent_heal";              // % of the target's Max HP, long reuse
    public const string UltimateHeal = "ultimate_heal";          // costs a Skill Stone
    public const string UltimatePartyHeal = "ultimate_party_heal"; // costs four
    public const string ResurrectionField = "resurrection_field"; // res every fallen ally in radius
    public const string ManaTotem = "mana_totem";                // the Ork's second planted object
    public const string ManaRay = "mana_ray";                    // drains a SHARE of the target's MP
    public const string ManaStrain = "mana_strain";              // the enemy's skills cost more
    public const string Meditation = "meditation";               // huge MP regen, no defence, breaks on damage
    public const string WeaponBreak = "weapon_break";            // −P.Atk and −M.Atk
    public const string ManaBlessing = "mana_blessing";          // your own skills cost less
    public const string GreatMight = "great_might";              // the 58+ P.Atk blessing …
    public const string GreatBulwark = "great_bulwark";          // … and its P.Def twin: pick ONE

    // ═══ HIS LADDER, AS DATA ═════════════════════════════════════════════════════════════════════
    //
    // 🔑 THE TWELVE BANDS ARE THE FILE'S SPINE. `healer 3rd.csv` is written in blocks headed 40, 44,
    // 48, 52, 56, 58, 60, 62, 64, 66, 68, 70, 72, 74 — note the stride HALVES at 56, which is why a
    // 3rd-class ladder is 14 rungs and not 9. A skill that starts late simply enters the array later;
    // `HealerRungs` takes that starting BAND INDEX so a ladder's rung 1 and its learn level can never
    // drift apart.

    /// <summary>The fourteen character levels this discipline learns at.</summary>
    internal static readonly int[] HealerBands = { 40, 44, 48, 52, 56, 58, 60, 62, 64, 66, 68, 70, 72, 74 };

    /// <summary>His SP price per band for a COMBAT skill — attack, heal, debuff, passive. The buff rows
    /// run on a second, much cheaper ladder that is priced per rung in Skills.BuffLadders.cs, and the
    /// support rows (res, Restore Mana, Antidote) on a third; only this one is regular enough to be an
    /// array. ⚠ Resurrection, Restore Mana and Antidote do NOT use it — the first two run on the BUFF
    /// ladder and Antidote on this one, per his 2026-08-20 rulings; see their rung builders.</summary>
    private static readonly int[] HealerSp =
        { 36000, 43000, 64000, 74000, 81000, 88000, 120000, 170000, 190000, 280000, 320000, 390000, 650000, 880000 };

    /// <summary>Build a ladder that starts at band <paramref name="firstBand"/> and runs
    /// <paramref name="count"/> rungs. <paramref name="mk"/> receives the rung index (0-based, within
    /// the ladder) and that band's SP, so a skill states its own numbers and never restates his SP.</summary>
    private static SkillLevel[] HealerRungs(int firstBand, int count, Func<int, int, SkillLevel> mk) =>
        Enumerable.Range(0, count).Select(i => mk(i, HealerSp[firstBand + i])).ToArray();

    private static SkillDef[] LightbringerSkills() => new SkillDef[]
    {
        // ═══ THE SHARED ATTACK SPELL ═════════════════════════════════════════════════════════════
        // Holy Ray replaces Holy Bolt: faster (2.5s vs 4s) and stronger, at shorter range (600 vs 750)
        // — the healer's nuke is something he casts while standing with the party, not from the back.
        new(HolyRay, "Holy Ray", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 30, CastTicks: 25, CooldownTicks: 10, Range: 600, Power: 42,
            Category: SkillCategory.Magic,  SpCost: 36000,
            Replaces: new[] { HolyStrike },
            Description: "The healer's attack spell: faster and stronger than Holy Bolt, at shorter range.",
            // ⚠ The level-52 rung read 52, the SAME as 48. He ruled a duplicate description is the
            // ERROR (2026-08-20), so it is 57 — continuing the +5 stride and smoothing the +11 jump to 63.
            Levels: HealerRungs(0, 14, (i, sp) =>
            {
                int[] pow = { 42, 47, 52, 57, 63, 66, 68, 71, 74, 77, 79, 82, 84, 87 };
                int[] mp  = { 30, 38, 44, 48, 52, 54, 55, 58, 60, 62, 64, 65, 67, 69 };
                return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp,
                    Description: $"Magic damage, m.Atk +{pow[i]}.");
            }).Concat(HealerFourthHolyRayRungs()).ToArray()),

        // ═══ THE HEALS ═══════════════════════════════════════════════════════════════════════════
        new(GreatHeal, "Great Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 62, CastTicks: 50, CooldownTicks: 20, Range: 600, Power: 400,
            Category: SkillCategory.Heal,  SpCost: 36000,
            Replaces: new[] { Heal },
            Description: "The healer's workhorse: a big single-target heal on a 5s cast.",
            Levels: HealerRungs(0, 14, (i, sp) =>
            {
                int[] pow = { 400, 460, 520, 570, 630, 660, 690, 720, 750, 770, 800, 820, 840, 860 };
                int[] mp  = {  62,  68,  76,  83,  90,  95,  98, 100, 105, 108, 112, 114, 117, 120 };
                return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp,
                    Description: $"Heals a single ally for {pow[i]}.");
            }).Concat(HealerFourthGreatHealRungs()).ToArray()),

        new(PartyGreatHeal, "Party Great Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 124, CastTicks: 70, CooldownTicks: 50, Range: 600, Power: 320,
            Category: SkillCategory.Heal,  SpCost: 36000,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600f,
            Replaces: new[] { PartyHeal },
            Description: "Heals you and nearby party members. Exactly twice the MP of Great Heal, at "
                       + "every rung — the party version is priced per head, not discounted.",
            Levels: HealerRungs(0, 14, (i, sp) =>
            {
                int[] pow = { 320, 370, 410, 430, 510, 540, 550, 560, 600, 620, 640, 660, 680, 700 };
                int[] mp  = { 124, 136, 152, 166, 180, 190, 196, 200, 210, 216, 224, 228, 234, 240 };
                return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp,
                    Description: $"Heals you and nearby party members for {pow[i]}.");
            }).Concat(HealerFourthPartyHealRungs()).ToArray()),

        // ---- URGENT HEAL — a PERCENTAGE of the target's own Max HP, and the only heal in the game
        //      that is. Four rungs (44-56) and then it stops: 15 → 30% of a bar, on an 8s cast with a
        //      30s reuse, so it is the button you press when someone is about to die and not one you
        //      rotate. The percentage ignores the healer's HealPower stats entirely (see HealOne) —
        //      it is the tank's HP pool that decides how big it is, which is exactly why it stays
        //      relevant at 74 without a single rung after 56.
        new(UrgentHeal, "Urgent Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 136, CastTicks: 80, CooldownTicks: 300, Range: 600, Power: 0,
            Category: SkillCategory.Heal,  SpCost: 43000,
            Description: "Heals an ally for a share of their OWN maximum HP — the bigger the target, "
                       + "the bigger the heal. Long cast, long reuse: an emergency, not a rotation.",
            Levels: HealerRungs(1, 4, (i, sp) =>
            {
                float[] pct = { 0.15f, 0.20f, 0.25f, 0.30f };
                int[] mp    = { 136, 152, 166, 180 };
                return new SkillLevel(MpCost: mp[i], SpCost: sp,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.Heal, pct[i], ModifierMode.Percent) },
                    Description: $"Heals an ally for {pct[i] * 100:0}% of their maximum HP.");
            })),

        // ---- ULTIMATE HEAL / ULTIMATE PARTY HEAL — the same two shapes as above with a REAGENT.
        //      One Skill Stone for the single, four for the party version (his numbers), which is what
        //      keeps a strictly-stronger heal from simply retiring Great Heal: it costs the same MP and
        //      hits harder, and you can only cast it as often as you can buy stones.
        new(UltimateHeal, "Ultimate Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 95, CastTicks: 50, CooldownTicks: 20, Range: 600, Power: 730,
            Category: SkillCategory.Heal,  SpCost: 88000,
            ConsumableId: ItemCatalog.SkillStone, ConsumableAmount: 1,
            Description: "Great Heal's stronger twin. Consumes one Skill Stone per cast.",
            Levels: HealerRungs(5, 9, (i, sp) =>
            {
                int[] pow = { 730, 750, 780, 800, 840, 870, 900, 930, 1000 };
                int[] mp  = {  95,  98, 100, 105, 108, 112, 114, 117, 120 };
                return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp,
                    Description: $"Heals a single ally for {pow[i]}. Consumes 1 Skill Stone.");
            }).Concat(HealerFourthUltimateHealRungs()).ToArray()),

        new(UltimatePartyHeal, "Ultimate Party Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 190, CastTicks: 70, CooldownTicks: 50, Range: 600, Power: 730,
            Category: SkillCategory.Heal,  SpCost: 88000,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600f,
            ConsumableId: ItemCatalog.SkillStone, ConsumableAmount: 4,
            Description: "Party Great Heal's stronger twin — and it heals every ally for the FULL "
                       + "single-target number, not a reduced one. Consumes four Skill Stones.",
            Levels: HealerRungs(5, 9, (i, sp) =>
            {
                int[] pow = { 730, 750, 780, 800, 840, 870, 900, 930, 1000 };
                int[] mp  = { 190, 196, 200, 210, 216, 224, 228, 234, 240 };
                return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp,
                    Description: $"Heals you and nearby party members for {pow[i]}. Consumes 4 Skill Stones.");
            }).Concat(HealerFourthUltimatePartyRungs()).ToArray()),

        // ---- RESURRECTION FIELD — a res aimed at the GROUND (his rows at 44 / 58 / 66 / 74).
        //
        // 🔑 IT IS A NEW MECHANIC, not a bigger Resurrection: nothing else in the game targets the
        // DEAD in an area (`PlayersInRadius` skips them by construction), so it needed its own scan
        // and its own arm through the three places a res is gated. See GameLoopService.AreaResurrect.
        //
        // Its ladder is the reach and the exp — 600 → 900 radius, 10% → 70% of the lost experience —
        // and it is deliberately WORSE per head than the single-target res at every level (74: 70% vs
        // 100%). What you are buying is that a wipe recovers in one 10s channel instead of eight.
        // ⚠ The 44 and 58 rungs also cast for 15s, longer than any other spell in the game.
        new(ResurrectionField, "Resurrection Field", BaseClass.Mage, SkillEffect.None,
            MpCost: 200, CastTicks: 150, CooldownTicks: 600, Range: 0, Power: 0,
            Category: SkillCategory.Heal,  SpCost: 22000,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600f,
            Resurrect: true, ResExpPct: 0.10f,
            Description: "Raises every fallen party member around you at 30% HP and MP, giving back "
                       + "part of the experience each of them lost. Cast where they fell.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 200, SpCost: 22000,  ResExpPct: 0.10f, AreaRadius: 600f, CastTicks: 150,
                    Description: "Revives fallen allies within 600 at 30% HP/MP; restores 10% of lost exp."),
                new SkillLevel(MpCost: 240, SpCost: 45000,  ResExpPct: 0.30f, AreaRadius: 700f, CastTicks: 150,
                    Description: "Revives fallen allies within 700 at 30% HP/MP; restores 30% of lost exp."),
                new SkillLevel(MpCost: 280, SpCost: 145000, ResExpPct: 0.50f, AreaRadius: 800f, CastTicks: 100,
                    Description: "Revives fallen allies within 800 at 30% HP/MP; restores 50% of lost exp."),
                new SkillLevel(MpCost: 320, SpCost: 450000, ResExpPct: 0.70f, AreaRadius: 900f, CastTicks: 100,
                    Description: "Revives fallen allies within 900 at 30% HP/MP; restores 70% of lost exp."),
            }.Concat(HealerFourthResFieldRungs()).ToArray()),

        // ═══ THE MANA KIT ════════════════════════════════════════════════════════════════════════
        //
        // ---- MANA RAY — model D, ruled 2026-08-20: the drain is a SHARE OF THE TARGET'S OWN MAX MP,
        //      not a damage number. `StatCalculator.ManaDrain` reads Power as PER MILLE (145 → 14.5%),
        //      which is the trick that lets his CSV cell stay an ordinary `+145 Power` and keeps
        //      `--check` and the DESCR reader looking at the same number the engine uses.
        //
        //      🔑 WHY A SHARE AND NOT DAMAGE: M.Def is nearly identical across classes (697-782 at 74)
        //      while MP POOLS differ 4.5× (fighter 696 vs healer 3158), so any model whose number is
        //      independent of the pool is lopsided by construction — measured, not argued
        //      (tools/BalanceMatrix -- --mana-ray). At 14.5% it is 7 casts to zero ANYONE.
        //
        //      ⚠ The MP column is already ×3 in his file and he then hand-tuned it (216/222/225/233/
        //      240/245/250/270/300/360). Do NOT triple it again. A full drain costs the healer ~60% of
        //      his own bar, which is what makes it *"a strategy move, not a farming tool"* — hence
        //      `NeverAuto`, and `PveDamageMult: 0.5` for his *"half effect on monsters"*.
        //      ⚠ SP at 68 read 280k, the same as 66. Corrected to the band ladder's 320k (2026-08-20):
        //      every other Mana Ray rung already sits exactly on that ladder.
        new(ManaRay, "Mana Ray", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 216, CastTicks: 20, CooldownTicks: 20, Range: 600, Power: 100,
            Category: SkillCategory.Magic,  SpCost: 81000,
            DamageToMp: true, PveDamageMult: 0.5f, NeverAuto: true,
            Description: "Burns away a share of an enemy's MAXIMUM MP — the bigger their pool, the "
                       + "more it takes. Half as effective against monsters. Never auto-cast.",
            Levels: HealerRungs(4, 10, (i, _) =>
            {
                int[] pow = { 100, 105, 110, 115, 120, 125, 130, 135, 140, 145 };
                int[] mp  = { 216, 222, 225, 233, 240, 245, 250, 270, 300, 360 };
                int[] sp  = { 81000, 88000, 120000, 170000, 190000, 280000, 320000, 390000, 650000, 880000 };
                return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp[i],
                    Description: $"Drains {pow[i] / 10f:0.#}% of the target's maximum MP (half that on a monster).");
            }).Concat(HealerFourthManaRayRungs()).ToArray()),

        // ---- MANA STRAIN — the other half of the same idea: instead of taking their mana, make
        //      everything they cast cost more. +100% at 52 climbing to +200% at 74, which the engine
        //      expresses as a NEGATIVE MP-cost reduction (his own worked example: 200% = ×3, and
        //      RecomputeDerived clamps the reduction to [−2, +0.8] for exactly that ceiling).
        //
        //      🔑 ITS PAYLOAD IS A FIELD, NOT A FLAG — the SkillEffect enum is full — so the skill's
        //      Effect is None and it is recognised as an enemy curse by its CATEGORY. Both the
        //      offensive-target test and the debuff-apply arm gained a Category arm for it.
        //      ⚠ `NeverAuto` on his ruling, same sentence as Mana Ray: *"same goes for 'Mana Strain'"*.
        new(ManaStrain, "Mana Strain", BaseClass.Mage, SkillEffect.None,
            MpCost: 48, CastTicks: 40, CooldownTicks: 40, Range: 600, Power: 0,
            DurationTicks: 600, BuffKey: "mana_strain", Rank: 1,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff, SpCost: 74000,
            // his healer CSV: "(success chance x0.5)" = 25% at parity. Contested via DebuffSchool.
            DebuffLandMod: 0.5f,
            NeverAuto: true,
            Description: "For 60s every skill the target uses costs far more MP.",
            Levels: HealerRungs(3, 11, (i, sp) =>
            {
                float[] pct = { 1.0f, 1.1f, 1.2f, 1.3f, 1.4f, 1.5f, 1.6f, 1.7f, 1.8f, 1.9f, 2.0f };
                int[] mp    = { 48, 52, 54, 55, 58, 60, 62, 64, 65, 67, 69 };
                // NEGATIVE reduction = higher cost. Both channels take the same number, which is what
                // his row says ("Physical and Magic MP Cost"). ⚠ At the top rung −2.0 means ×3, and
                // that is exactly where RecomputeDerived clamps — his own worked example.
                return new SkillLevel(MpCost: mp[i], SpCost: sp,
                    PhysMpCostPct: -pct[i], MagicMpCostPct: -pct[i],
                    Description: $"Raises the target's physical and magic MP costs by {pct[i] * 100:0}% for 60s.");
            }).Concat(HealerFourthManaStrainRungs()).ToArray(),
            PhysMpCostPct: -1.0f, MagicMpCostPct: -1.0f),

        // ---- MEDITATION — 30 seconds of enormous MP regeneration bought with your defence, and it
        //      ends the instant anything lands on you (`EndsOnDamageTaken`, checked in ApplyDamage —
        //      the one choke point every source of damage passes through, so a DoT tick, an AoE and a
        //      reflect all end it with no per-source code).
        //
        //      🔑 The −90% P.Def is a NEGATIVE percent on `BuffDef`, not a `DebuffDef` magnitude, and
        //      that is not cosmetic: DebuffDef is in `AnyDebuff`, so the skill would have been treated
        //      as offensive and demanded an enemy target. `ModifiedStat` multiplies by (1 + percent),
        //      so −0.90 is precisely his "x0.1". (The same trick Armor Break uses for M.Def.)
        //      ⚠ 15 minutes of reuse. Four rungs only, at 56/60/64/68.
        new(Meditation, "Meditation", BaseClass.Mage,
            SkillEffect.BuffDef | SkillEffect.BuffMpRegen,
            MpCost: 40, CastTicks: 20, CooldownTicks: 9000, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "meditation", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, SpCost: 42000,
            TargetMode: TargetMode.SelfOnly, EndsOnDamageTaken: true,
            Description: "Sit inside your own magic for 30s: MP floods back and your Physical Defence "
                       + "all but disappears. The first hit you take ends it.",
            Levels: new[]
            {
                MeditationRung(mpPerSecond: 25, mp: 40, sp: 42000),
                MeditationRung(mpPerSecond: 30, mp: 47, sp: 61000),
                MeditationRung(mpPerSecond: 35, mp: 52, sp: 100000),
                MeditationRung(mpPerSecond: 40, mp: 57, sp: 165000),
            }),

        // ---- MANA BLESSING — the friendly mirror of Mana Strain, on an ally. PHYSICAL costs fall
        //      twice as fast as magic ones at every rung (10/5, 15/7, 20/10), which is his: the people
        //      whose MP bar actually runs out are the ones swinging, not the ones casting.
        //      Payload is the same pair of FIELDS, so `SkillEffect.None` + Category Buff.
        new(ManaBlessing, "Mana Blessing", BaseClass.Mage, SkillEffect.None,
            MpCost: 90, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "mana_blessing", Rank: 1,
            Category: SkillCategory.Buff, SpCost: 45000,
            Description: "Blesses an ally (or self): their skills cost less MP for 20 minutes.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 90,  SpCost: 45000,  PhysMpCostPct: 0.10f, MagicMpCostPct: 0.05f,
                    Description: "−10% physical and −5% magic skill MP cost."),
                new SkillLevel(MpCost: 110, SpCost: 145000, PhysMpCostPct: 0.15f, MagicMpCostPct: 0.07f,
                    Description: "−15% physical and −7% magic skill MP cost."),
                new SkillLevel(MpCost: 125, SpCost: 330000, PhysMpCostPct: 0.20f, MagicMpCostPct: 0.10f,
                    Description: "−20% physical and −10% magic skill MP cost."),
            }.Concat(HealerFourthManaBlessingRungs()).ToArray(),
            PhysMpCostPct: 0.10f, MagicMpCostPct: 0.05f),

        // ═══ THE 58+ "GREAT" PAIR — ONE OR THE OTHER, NEVER BOTH ═════════════════════════════════
        //
        // 🔑 THEY SHARE A BUFF KEY, which is the whole ruling. His two rows both say *"Does not stack
        // with Other Great Might|Bulwark effects"* and the comment column spells out the four names
        // that clash. A shared key IS non-stacking in this engine (ApplyBuff arbitrates by family),
        // so the rule needs no new mechanism — and because both sit at Rank 1 at every level, casting
        // one always evicts the other. That is deliberate: the choice has to be re-makeable mid-fight,
        // and a rank ladder would have let a level-74 Might lock out a level-74 Bulwark.
        //
        // ⚠ They are NOT rungs of the ordinary Might / Bulwark families and must not be folded into
        // them: those are the 8/12/15% ladder every potion and scroll also sells, and these stack ON
        // TOP of it. A healer's party gets 15% + 10% P.Atk, not 10% instead of 15%.
        new(GreatMight, "Great Might", BaseClass.Mage, SkillEffect.BuffPhysAtk,
            MpCost: 90, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: GreatBlessingKey, Rank: 1, FlatRank: true,
            Category: SkillCategory.Buff, SpCost: 45000,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffPhysAtk, 0.05f) },
            Description: "A second layer of Physical Attack, on top of Might. Does not stack with "
                       + "Great Bulwark — an ally carries one of the two, never both.",
            Levels: new[]
            {
                GreatRung(SkillEffect.BuffPhysAtk, 0.05f, mp: 90,  sp: 45000,  "P.Atk"),
                GreatRung(SkillEffect.BuffPhysAtk, 0.07f, mp: 110, sp: 145000, "P.Atk"),
                GreatRung(SkillEffect.BuffPhysAtk, 0.10f, mp: 125, sp: 330000, "P.Atk"),
            }),

        new(GreatBulwark, "Great Bulwark", BaseClass.Mage, SkillEffect.BuffDef,
            MpCost: 90, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: GreatBlessingKey, Rank: 1, FlatRank: true,
            Category: SkillCategory.Buff, SpCost: 45000,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffDef, 0.05f) },
            Description: "A second layer of Physical Defence, on top of Bulwark. Does not stack with "
                       + "Great Might — an ally carries one of the two, never both.",
            Levels: new[]
            {
                // ⚠ Bulwark's ladder is STEEPER than Might's (5/10/15 vs 5/7/10) and that is his file:
                // the defensive half is the one worth taking on a tank.
                GreatRung(SkillEffect.BuffDef, 0.05f, mp: 90,  sp: 45000,  "P.Def"),
                GreatRung(SkillEffect.BuffDef, 0.10f, mp: 110, sp: 145000, "P.Def"),
                GreatRung(SkillEffect.BuffDef, 0.15f, mp: 125, sp: 330000, "P.Def"),
            }),

        // ═══ CONCEAL ═════════════════════════════════════════════════════════════════════════════
        // The SELF-only twin of Shrouding Hymn: same promise (only mobs that have not already noticed
        // you), a third of the duration, and no party to carry.
        new(Conceal, "Conceal", BaseClass.Mage, SkillEffect.None,
            MpCost: 100, CastTicks: 50, CooldownTicks: 100, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "conceal", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, SpCost: 19000,
            TargetMode: TargetMode.SelfOnly, GrantsMobStealth: true,
            Description: "For 30s, monsters that haven't already noticed you leave you alone. " +
                         "Anything already chasing you keeps chasing."),

        // ═══ HUMAN: single-target throughput + Gravity ═══════════════════════════════════════════
        //
        // 🔑 `LbHumanMend` IS his "Quick Great Heal" — same race, same level 40, same job, so the id is
        // reused and retuned rather than retired next to a near-duplicate. It shipped as "Mending
        // Light" at power 230.
        //
        // It heals for EXACTLY what Great Heal does at every rung, in 2 seconds instead of 5, for
        // roughly 1.5× the MP. That is the Human's whole discipline in one line: the same throughput
        // measured in HP, far more of it measured per second, and a bar that empties much faster.
        new(LbHumanMend, "Quick Great Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 93, CastTicks: 20, CooldownTicks: 10, Range: 600, Power: 400,
            Category: SkillCategory.Heal,  SpCost: 36000,
            Replaces: new[] { QuickHeal },
            Description: "Great Heal's power on a 2s cast, for half again the MP. The Human "
                       + "Lightbringer's hallmark: throughput now, at a price.",
            Levels: HealerRungs(0, 14, (i, sp) =>
            {
                // 🔑 POWER IS EXACTLY GREAT HEAL'S AT EVERY RUNG. That identity is what fixed the @72 rung:
                // it read 820, duplicating @70, where Great Heal moves 820 → 840.
                int[] pow = { 400, 460, 520, 570, 630, 660, 690, 720, 750, 770, 800, 820, 840, 860 };
                int[] mp  = {  93, 100, 114, 124, 134, 142, 147, 150, 153, 162, 167, 171, 175, 180 };
                return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp,
                    Description: $"Heals a single ally for {pow[i]} on a 2s cast.");
            }).Concat(HealerFourthQuickHealRungs()).ToArray()),

        new(LbHumanGravity, "Gravity", BaseClass.Mage,
            SkillEffect.DebuffAtkSpeed | SkillEffect.DebuffCastSpeed,
            MpCost: 35, CastTicks: 25, CooldownTicks: 50, Range: 600, Power: 0,
            DurationTicks: 300, BuffKey: "gravity", Rank: 1,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff, SpCost: 36000,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffAtkSpeed, 0.07f), new(SkillEffect.DebuffCastSpeed, 0.07f),
            },
            Description: "Weighs an enemy down for 30s: slower attacks AND slower casting.",
            // ⚠ ONE NUMBER FOR BOTH CHANNELS since his 2026-08-20 edit (*"changed values"*). It used to
            // be −23% attack / −12% cast at level 1; the whole ladder is now 7% → 23% on both.
            // 🔑 IT PLATEAUS AT 23% FROM LEVEL 64 — and that is a DELIBERATE REVERSAL of his 2026-08-20
            // ruling, made on 2026-08-26: *"u had debuffs percent going up and I wanted it to stop (the
            // lvling is the spell not to fail)"*. What a higher rung buys past the plateau is the
            // LANDING CHANCE, not a bigger number — the contested/fizzle rolls read the rung's own learn
            // level, so the top rungs land more often at the same magnitude. His 08-20 objection
            // (*"if the 40 lvl description is the same as 44 one then the description is wrong"*) was
            // about duplicated rungs at the BOTTOM of a ladder; a designed ceiling at the top is not that.
            // ⚠ Armor Break plateaus the same way, for the same reason. Don't "fix" either back.
            Levels: HealerRungs(0, 14, (i, sp) =>
                new SkillLevel(MpCost: DebuffMp[i], SpCost: sp,
                    Magnitudes: new EffectMagnitude[]
                    {
                        new(SkillEffect.DebuffAtkSpeed, GravityPct[i]),
                        new(SkillEffect.DebuffCastSpeed, GravityPct[i]),
                    },
                    Description: $"−{GravityPct[i] * 100:0}% attack speed and cast speed for 30s.")).Concat(HealerFourthGravityRungs()).ToArray()),

        // ═══ ELF: the heal-and-cure + Bind ═══════════════════════════════════════════════════════
        //
        // 🔑 `LbElfDawn` IS his "Healer Blessing". Reused and retuned like the Human's. ⚠ Two things his
        // rows CHANGED about the original: it is SINGLE-TARGET now, not an area heal, and its cure is
        // SCOPED — bleed and poison up to a rank that climbs 3 → 9 across the ladder, rather than the
        // blanket "cleanses everything" it used to be. That ceiling is the same one Antidote runs on,
        // so the Elf's two cures sit on one number line.
        //
        // Its heal is BELOW Great Heal's at every rung (360 vs 400 … 800 vs 860) on a 3s cast: the Elf
        // pays for the cure in healing, where the Human pays for speed in MP.
        new(LbElfDawn, "Healer Blessing", BaseClass.Mage, SkillEffect.Heal | SkillEffect.Cleanse,
            MpCost: 62, CastTicks: 30, CooldownTicks: 10, Range: 600, Power: 360,
            Category: SkillCategory.Heal,  SpCost: 36000,
            DispelMask: SkillEffect.Bleed | SkillEffect.Poison | SkillEffect.Venom,
            DispelMaxLevel: 3,
            Replaces: new[] { QuickHeal },
            Description: "Heals an ally and cures their bleed and poison in one cast. The Elf "
                       + "Lightbringer answers a hurt ally and a poisoned one with one button.",
            Levels: HealerRungs(0, 14, (i, sp) =>
            {
                int[] pow  = { 360, 410, 470, 510, 570, 595, 620, 650, 675, 700, 720, 740, 760, 800 };
                int[] mp   = {  62,  68,  76,  83,  90,  95,  98, 100, 105, 108, 112, 114, 117, 120 };
                int[] rank = {   3,   3,   4,   4,   5,   5,   6,   6,   7,   7,   8,   8,   9,   9 };
                return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp, DispelMaxLevel: rank[i],
                    Description: $"Heals an ally for {pow[i]} and cures their bleed and poison of rank {rank[i]} or lower.");
            }).Concat(HealerFourthBlessingRungs()).ToArray()),

        new(LbElfBind, "Bind", BaseClass.Mage, SkillEffect.Root,
            MpCost: 35, CastTicks: 25, CooldownTicks: 50, Range: 600, Power: 0,
            DurationTicks: 300, BuffKey: "root", Rank: 1,
            DebuffLandMod: 0.7f,   // his healer CSV: "(success chance x0.7)"
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff, SpCost: 36000,
            Description: "Holds an enemy in place for 30s. Contested ATK vs SPT.",
            // ⚠ NOTHING BUT THE PRICE MOVES. A hold is a hold — 30 seconds at rung 1 and at rung 14 —
            // so what the ladder buys is the LEVEL CONTEST: `DebuffLandChance` reads the attacker's
            // level, and a rung is what keeps Bind landing on things your own level. That is the whole
            // reason a CC ladder exists (see the CC level-curve work) and it is why this looks empty.
            Levels: HealerRungs(0, 14, (i, sp) =>
                new SkillLevel(MpCost: DebuffMp[i], SpCost: sp,
                    Description: "Holds an enemy in place for 30s.")).Concat(HealerFourthBindRungs()).ToArray()),

        // ═══ ORK: the two planted totems + Armor Break ═══════════════════════════════════════════
        //
        // 🔑 A TOTEM IS NOT AN ENTITY (see World.Totems / TickTotems). A trap waits once for an enemy
        // and dies; a totem pulses at ALLIES on a timer. Making it an EntityKind would have meant
        // auditing ~137 server call sites, 54 of which ask "is this a mob"; PETS are what will justify
        // paying that, because they must move, fight and be targetable. A totem needs none of it.
        //
        // 🔑 WHICH POOL a totem fills is the SKILL's own Effect, not a totem setting — `Heal` for the
        // Healing Totem, `RestoreMp` for the Mana one. That is why the second totem below is an
        // ordinary skill and not a second mechanic.
        //
        // ⚠ HIS 40 ROW RETUNED THE HEALING TOTEM HARD (*"Increased power and manacost - to match a
        // heal over time"*): 64 HP/s for 238 MP, against the 30/93 it shipped with. Over its 30s life
        // that is 1920 HP to one ally — or to six — for less than four Great Heals' worth of MP. The
        // 25s reuse against a 30s duration is what stops two ever overlapping.
        new(LbOrkFont, "Healing Totem", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 238, CastTicks: 10, CooldownTicks: 250, Range: 0, Power: 64,
            Category: SkillCategory.Heal,  SpCost: 36000,
            // SelfOnly, like the trap: you plant it where you stand, so the cast needs no target and
            // must not be rejected for lacking one.
            TargetMode: TargetMode.SelfOnly,
            PlacesTotem: true, TotemRadius: 300f, TotemLifeTicks: 300, TotemPulseTicks: 10,
            Description: "Plants a totem where you stand. For 30s it heals you and every nearby party "
                       + "member, every second. The Ork Lightbringer heals ground, not people.",
            Levels: HealerRungs(0, 14, (i, sp) =>
            {
                int[] pow = {  64,  74,  82,  86, 102, 108, 110, 112, 120, 125, 130, 135, 140, 150 };
                int[] mp  = { 238, 272, 304, 352, 360, 380, 392, 400, 420, 432, 448, 452, 464, 476 };
                return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp,
                    Description: $"A totem healing +{pow[i]}/s within 300 for 30s.");
            }).Concat(HealerFourthTotemRungs()).ToArray()),

        // ---- MANA TOTEM — the same object pointed at the other bar, from 52. Its numbers look tiny
        //      next to the healing one (20/s at 74 against 150/s) and that is the correct comparison:
        //      MP pools are a fifth the size of HP pools and nothing else in the game refills a
        //      party's mana passively. 60s reuse against 30s of life, so it is up half the time.
        new(ManaTotem, "Mana Totem", BaseClass.Mage, SkillEffect.RestoreMp,
            MpCost: 352, CastTicks: 10, CooldownTicks: 600, Range: 0, Power: 10,
            Category: SkillCategory.Heal,  SpCost: 74000,
            TargetMode: TargetMode.SelfOnly,
            // ⚠ REPLACES Restore Mana since his 2026-08-26 edit — the totem is the ork healer's upgrade
            // of the single-target MP restore, not a second skill beside it. His CSV row: [Restore Mana].
            Replaces: new[] { SkillCatalog.RestoreMana },
            PlacesTotem: true, TotemRadius: 300f, TotemLifeTicks: 300, TotemPulseTicks: 10,
            Description: "Plants a totem where you stand. For 30s it restores MP to you and every "
                       + "nearby party member, every second.",
            Levels: HealerRungs(3, 11, (i, sp) =>
            {
                int[] pow = {  10,  11,  12,  13,  14,  15,  16,  17,  18,  19,  20 };
                int[] mp  = { 352, 360, 380, 392, 400, 420, 432, 448, 452, 464, 476 };
                return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp,
                    Description: $"A totem restoring +{pow[i]} MP/s within 300 for 30s.");
            })),

        // ⚠ There is no `DebuffMagicDef` flag and one must not be invented — the enum is full. M.Def is
        // applied LIVE through `BuffMagicDef` (see Entity.EffectiveMagicDefence), so a NEGATIVE
        // magnitude on the buff flag is how the engine already expresses an M.Def debuff. P.Def has its
        // own `DebuffDef` flag, whose magnitudes are authored positive and subtracted — hence the two
        // signs below looking inconsistent while meaning the same thing.
        new(LbOrkArmorBreak, "Armor Break", BaseClass.Mage,
            SkillEffect.DebuffDef | SkillEffect.BuffMagicDef,
            MpCost: 35, CastTicks: 25, CooldownTicks: 50, Range: 600, Power: 0,
            DurationTicks: 300, BuffKey: "armor_break", Rank: 1,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff, SpCost: 36000,
            DebuffLandMod: 1.5f,   // his healer CSV: "(success chance x1.5)" = 75% at parity
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffDef, 0.10f), new(SkillEffect.BuffMagicDef, -0.05f),
            },
            Description: "Shatters an enemy's guard for 30s: less P.Def and less M.Def.",
            // 🔑 M.DEF IS EXACTLY HALF P.DEF AT EVERY RUNG. That identity resolved his 2026-08-20 defect
            // at @56 (the row read 18/10 where the half-rule says 9, and 10 duplicated @58). Keep the
            // ratio if these are ever retuned.
            // 🔑 THE LADDER PLATEAUS AT 30/15 FROM LEVEL 66 — put back deliberately on 2026-08-26:
            // *"u had debuffs percent going up and I wanted it to stop (the lvling is the spell not to
            // fail)"*. Past the ceiling a higher rung buys LANDING CHANCE, not magnitude. See the longer
            // note on Gravity above; the two move together and neither should be "fixed" back to a
            // climbing stride.
            Levels: HealerRungs(0, 14, (i, sp) =>
            {
                float[] pDef = { .10f, .12f, .14f, .16f, .18f, .20f, .22f, .24f, .26f, .30f, .30f, .30f, .30f, .30f };
                float[] mDef = { .05f, .06f, .07f, .08f, .09f, .10f, .11f, .12f, .13f, .15f, .15f, .15f, .15f, .15f };
                return new SkillLevel(MpCost: DebuffMp[i], SpCost: sp,
                    Magnitudes: new EffectMagnitude[]
                    {
                        new(SkillEffect.DebuffDef, pDef[i]), new(SkillEffect.BuffMagicDef, -mDef[i]),
                    },
                    Description: $"−{pDef[i] * 100:0}% P.Def and −{mDef[i] * 100:0}% M.Def for 30s.");
            }).Concat(HealerFourthArmorBreakRungs()).ToArray()),

        // ---- WEAPON BREAK — the shared offensive debuff, from 62. Four rungs, both attack channels
        //      at once (`DebuffAtk` is the flag that covers P.Atk and M.Atk together), so one cast
        //      answers a physical boss and a caster one alike.
        new(WeaponBreak, "Weapon Break", BaseClass.Mage, SkillEffect.DebuffAtk,
            MpCost: 58, CastTicks: 25, CooldownTicks: 50, Range: 600, Power: 0,
            DurationTicks: 300, BuffKey: "weapon_break", Rank: 1,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff, SpCost: 170000,
            DebuffLandMod: 1.5f,   // his healer CSV: "(success chance x1.5)" = 75% at parity
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffAtk, 0.09f) },
            Description: "Blunts an enemy's weapon and their magic alike for 30s.",
            Levels: new[]
            {
                WeaponBreakRung(0.09f, mp: 58, sp: 170000),
                WeaponBreakRung(0.11f, mp: 62, sp: 280000),
                WeaponBreakRung(0.13f, mp: 65, sp: 390000),
                WeaponBreakRung(0.15f, mp: 69, sp: 880000),
            }.Concat(HealerFourthWeaponBreakRungs()).ToArray()),

        // ═══ THE HEALER'S OWN TWO MASTERIES ══════════════════════════════════════════════════════
        // He split the shared cleric masteries at 40: the healer takes these two, the BUFFER keeps
        // continuing the originals (Spell Mastery / Armor Mastery, rung 5 in Skills.Healer.cs).
        // Both REPLACE their cleric original, so nothing stacks and nothing is lost — the replaced
        // skill's rungs simply stop being read (Entity.RecomputeDerived's `supersededMasteries`).
        //
        // 🔑 WHAT HE ACTUALLY CHANGED, in his own two comment cells:
        //   *"Removed the P.Atk bonus and made it only for magic weapons"*  → HealerWeaponMastery
        //   *"Removed the Light Armor bonus"*                               → HealerArmorMastery
        // A pure healer therefore has no reason left to hold a sword and no reason to wear light: his
        // whole kit is a wand and a robe. ⚠ That is NOT a penalty — Divine Focus (the old ×0.5 heal
        // for holding no wand) was DELETED in the same pass. He forgoes a BONUS, which is the shape
        // every other mastery in the game already has.

        // ---- Spellcaster Weapon Mastery — replaces Spell Mastery. BLUNT ONLY, and no P.Atk at any rung.
        //      FOURTEEN rungs now (his file runs the ladder to 74; it stopped at 64 on 2026-08-20
        //      because that was as far as the draft went).
        //
        //      ⚠ RENAMED 2026-08-24 in `healer 3rd.csv`, from "Healer Weapon Mastery" — the skill is
        //      not healer-flavoured, it is what a 3rd-class CASTER's blunt does, and the nuker file
        //      will want the same row. **The id stays `healer_weapon_mastery`**: skill ids are
        //      append-only and a rename would strand every learned row in every saved character.
        //      Display names are free to move, ids are not.
        //
        //      🔑 THE GATE IS THE TYPE `Blunt`, NOT a magic-weapon flag — his ruling: *"the healers
        //      weapon mastery can say blunt .. as both wand/staff are blunts .. that way a sword wont
        //      work on a healer and cariing a normal blunt is lower matk and no attri .. so its a
        //      choice"*. A `MagicWeaponOnly` flag was built first and removed the same day: Blunt
        //      leaves a plain mace WORKING, it just carries less M.Atk and rolls no caster attributes.
        //      A mastery that can refuse a weapon the type system says is fine is a wall, not a choice.
        new(HealerWeaponMasterySkill, "Spellcaster Weapon Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, Replaces: new[] { SpellMastery },
            Description: "Passive. Divine power flows through a BLUNT weapon — wand, staff or mace: "
                       + "more M.Atk, faster casting, shorter reuse and much stronger HP/MP "
                       + "regeneration. A sword grants none of it.",
            WeaponMasteryLevels: HealerWeaponRungs.Concat(HealerFourthWeaponRungs).Select(r =>
                HealerWeapon(r.MAtk, r.Reuse, r.Cast, r.MpFlat, r.HpReg)).ToArray(),
            Levels: HealerRungs(0, 14, (i, sp) =>
            {
                var r = HealerWeaponRungs[i];
                return new SkillLevel(SpCost: sp,
                    Description: $"With a wand or staff: +{r.MAtk} M.Atk, +{r.Cast * 100:0}% cast, "
                               + $"−{r.Reuse * 100:0}% reuse, MP regen +{r.MpFlat:0.#}/s, HP regen +{r.HpReg:0.#}/s.");
            }).Concat(HealerFourthWeaponMasteryRungs()).ToArray()),

        // ---- Healer Armor Mastery — replaces Armor Mastery. ROBE ONLY: the Light row is cut, so a
        //      healer in light armor keeps Spellcaster Mastery's raw cast ×0.5 / atk ×0.5 with nothing
        //      cancelling it. Same 14 rungs, same SP ladder.
        new(HealerArmorMasterySkill, "Healer Armor Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, Replaces: new[] { ArmorMasterySkill },
            Description: "Passive. A ROBE is the healer's vestment: much more defence, max MP and MP "
                       + "regeneration. Light armor no longer keeps you casting — that is the "
                       + "buffer's path now.",
            ArmorMasteryLevels: HealerRobeRungs.Select(r => HealerRobe(r.PDef, r.MaxMp))
                .Concat(HealerFourthRobeRungs.Select(HealerRobe4)).ToArray(),
            Levels: HealerRungs(0, 14, (i, sp) =>
            {
                var r = HealerRobeRungs[i];
                return new SkillLevel(SpCost: sp,
                    Description: $"In a robe: +{r.PDef} P.Def, +{r.MaxMp} Max MP, MP regen x1.2.");
            }).Concat(HealerFourthArmorMasteryRungs()).ToArray()),

        // ═══ ORPHANED BY HIS FILE — DEFINED, NEVER GRANTED ═══════════════════════════════════════
        //
        // These five were invented before `healer 3rd.csv` existed and none of them is on one of his
        // rows, so the class table no longer teaches any of them. ⚠ THE DEFS STAY, and that is the
        // rule rather than tidiness: `LearnedSkills` persists skill IDS, so deleting a def turns every
        // character who already bought one into a broken row on their bar and their skill window
        // (see the retired-id leak). The same treatment the 40+ purge gave the fighter kits in 2026-08.
        //
        // 🔴 `LbElfWarden` in particular is the overlap the 2026-08-17 note flagged: it was an invented
        // 8-second root, and his `Bind` at 40 is a 30-second one. Now that Bind is real, Warding Step
        // is simply gone from the kit rather than sitting beside it as a strictly worse duplicate.
        new(LbBlessing, "Blessing of Light", BaseClass.Mage,
            SkillEffect.BuffHp | SkillEffect.BuffDef,
            MpCost: 50, CastTicks: 20, CooldownTicks: 30, Range: 0, Power: 0,
            DurationTicks: 12000, BuffKey: "lb_blessing", Rank: 1,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffHp, 0.15f), new(SkillEffect.BuffDef, 0.15f),
            },
            Category: SkillCategory.Buff, TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600,
            SpCost: 500, Description: "Party: +15% max HP and +15% defence."),
        new(LbDevotion, "Devotion", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 500,
            Passive: new PassiveEffect(MaxMpPct: 0.10f, MpRegen: 2f, MagicDefence: 10),
            Description: "Passive. +10% max MP, +MP regen, +10 magic defence."),
        new(LbHumanPurify, "Purify", BaseClass.Mage, SkillEffect.Cleanse,
            MpCost: 24, CastTicks: 8, CooldownTicks: 50, Range: 500, Power: 0,
            Category: SkillCategory.Heal, SpCost: 6000,
            Description: "Removes harmful effects (curses, anti-heal, roots) from an ally."),
        new(LbElfWarden, "Warding Step", BaseClass.Mage, SkillEffect.Root | SkillEffect.Detaunt,
            MpCost: 30, CastTicks: 6, CooldownTicks: 120, Range: 500, Power: 0,
            DurationTicks: 80, BuffKey: "root", Rank: 1, DebuffSchool: DebuffSchool.Magical,
            DebuffLandMod: 0.5f,   // BL-90: a MAGICAL hold, his general "x0.5". Physical ones stay x1 (CON saves).
            Category: SkillCategory.Debuff, SpCost: 6000,
            Description: "Holds an enemy in place for 8s and sheds the caster's aggro "
                       + "from nearby foes (they look elsewhere)."),
        new(LbOrkSap, "Soul Sap", BaseClass.Mage, SkillEffect.DebuffHealRecv,
            MpCost: 28, CastTicks: 8, CooldownTicks: 150, Range: 500, Power: 0,
            DurationTicks: 150, BuffKey: "antiheal", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffHealRecv, 0.50f) },
            Category: SkillCategory.Debuff, SpCost: 6000,
            Description: "Curses an enemy so it recovers only half the HP from any "
                       + "healing for 15s."),
    };

    // ═══════════════════════════════════════════════════════════════════════════════════════════════
    //  Ladder data and rung builders. Kept out of the array above so a 14-rung skill reads as one
    //  definition rather than a page of near-identical records.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>The buff key BOTH "Great" blessings land on — a shared key IS non-stacking here, and
    /// it is the entire implementation of his *"Does not stack with Other Great Might|Bulwark"*.</summary>
    private const string GreatBlessingKey = "great_blessing";

    /// <summary>The MP price of a control debuff at each of the fourteen bands. Bind, Armor Break and
    /// Gravity share it exactly, which is his file: one price for "a 2.5s contested curse", whatever
    /// the curse happens to be.</summary>
    private static readonly int[] DebuffMp = { 35, 38, 44, 48, 52, 54, 55, 58, 60, 62, 64, 65, 67, 69 };

    /// <summary>Gravity's single percentage, both channels. 7% → 23%, then flat for the last six.</summary>
    private static readonly float[] GravityPct =
        { .07f, .09f, .11f, .13f, .15f, .17f, .19f, .21f, .23f, .23f, .23f, .23f, .23f, .23f };

    private static SkillLevel WeaponBreakRung(float pct, int mp, int sp) =>
        new(MpCost: mp, SpCost: sp,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffAtk, pct) },
            Description: $"−{pct * 100:0}% P.Atk and M.Atk for 30s.");

    /// <summary>One rung of a "Great" blessing. The buff itself carries no rank ladder (see the key
    /// above) — only its magnitude and its price climb.</summary>
    private static SkillLevel GreatRung(SkillEffect effect, float pct, int mp, int sp, string what) =>
        new(MpCost: mp, SpCost: sp,
            Magnitudes: new EffectMagnitude[] { new(effect, pct) },
            Description: $"+{pct * 100:0}% {what}, on top of the ordinary blessing.");

    /// <summary>One rung of Meditation: the MP/s is a FLAT BuffMpRegen (per second — Regenerate reads
    /// the flat inside the stance multiplier, so sitting to meditate pays), against a −90% P.Def that
    /// never changes. Only the regen and the price move.</summary>
    private static SkillLevel MeditationRung(int mpPerSecond, int mp, int sp) =>
        new(MpCost: mp, SpCost: sp,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffDef, -0.90f),
                new(SkillEffect.BuffMpRegen, mpPerSecond, ModifierMode.Flat),
            },
            Description: $"+{mpPerSecond} MP/s and −90% P.Def for 30s; ends on any damage taken.");

    /// <summary>One rung of Spellcaster Weapon Mastery, as his row writes it.
    /// <para>⚠ BOTH REGEN COLUMNS ARE FLAT PER-SECOND GRANTS, read verbatim off his CSV row: <c>mpReg
    /// +3.4</c> and <c>hpReg +2.7</c> are 3.4f and 2.7f here — the WHOLE rung, never its excess over
    /// 1.0. MP converted on 2026-08-26 (*"the other increases are flat increases so the 1.9~3.4 is +
    /// not x"*) and HP followed the same day, once he had seen the measurement (*"I want to make the
    /// passives + not x as the mp"*).</para>
    /// <para>Both ladders read as MULTIPLIERS before that. On MP it reached ×4.84 by level 74 and a
    /// buffed mage regenerated ~290% of his own spam cost; on HP it reached ×2.7 and put a level-74
    /// nuker at 27.5 HP/s against a tank's 16.4 — the class IG gives the LOWEST base regen holding the
    /// game's highest. Never re-enter either as a percent.</para></summary>
    private readonly record struct WeaponRung(int MAtk, float Reuse, float Cast, float MpFlat, float HpReg);

    /// <summary>His fourteen weapon-mastery rows, 40 → 74. ⚠ M.Atk repeats 45 at 48 and 52, and that is
    /// LEFT ALONE: his "a duplicate description is wrong" rule is about the RUNG, and this rung still
    /// climbs — MP regen x1.9 → x2.3 and HP regen x1.6 → x1.7. Compare Healer Armor Mastery, whose
    /// 48/52 pair improved in NOTHING and therefore did need a fix. The
    /// two regen multipliers step on their own schedule; mirrored exactly.</summary>
    private static readonly WeaponRung[] HealerWeaponRungs =
    {
        new(23, 0.15f, 0.07f, 1.5f, 1.1f),   // 40
        new(29, 0.15f, 0.07f, 1.9f, 1.6f),   // 44
        new(45, 0.20f, 0.07f, 1.9f, 1.6f),   // 48
        new(45, 0.20f, 0.07f, 2.3f, 1.7f),   // 52
        new(52, 0.20f, 0.10f, 2.3f, 1.7f),   // 56
        new(57, 0.20f, 0.10f, 2.3f, 2.1f),   // 58
        new(62, 0.20f, 0.10f, 2.7f, 2.1f),   // 60
        new(67, 0.20f, 0.10f, 2.7f, 2.1f),   // 62
        new(72, 0.20f, 0.10f, 2.7f, 2.6f),   // 64
        new(77, 0.20f, 0.10f, 2.7f, 2.6f),   // 66
        new(83, 0.20f, 0.10f, 3.1f, 2.6f),   // 68
        new(88, 0.20f, 0.10f, 3.1f, 2.6f),   // 70
        new(94, 0.20f, 0.10f, 3.1f, 2.6f),   // 72
        new(99, 0.20f, 0.10f, 3.4f, 2.7f),   // 74
    };

    private readonly record struct RobeRung(int PDef, int MaxMp);

    /// <summary>His fourteen armor-mastery rows, 40 → 74. MP regen is x1.2 at every rung.</summary>
    private static readonly RobeRung[] HealerRobeRungs =
    {
        new(39,  70), new(44,  70), new(47, 100), new(50, 100), new(53, 140), new(56, 140), new(58, 150),
        new(64, 150), new(68, 150), new(72, 180), new(75, 180), new(79, 180), new(83, 200), new(87, 200),
    };

    /// <summary>One rung of Spellcaster Weapon Mastery — the BLUNT slot only (wand, staff and mace all fold
    /// to <c>WeaponType.Blunt</c> via <c>Base()</c>, 1H and 2H alike). NO P.Atk at any rung, which is the
    /// whole point of the split; a sword earns nothing here.</summary>
    private static WeaponMasteryProfile HealerWeapon(int mAtk, float reuse, float cast, float mpFlat, float hpReg) =>
        new(Blunt: new PassiveEffect(MagAtk: mAtk, CooldownPct: reuse, CastSpeedPct: cast,
                                     MpRegen: mpFlat, HpRegen: hpReg));

    /// <summary>One rung of Healer Armor Mastery — ROBE only, every other weight left inert so
    /// Spellcaster Mastery's penalty stands uncancelled (his *"Removed the Light Armor bonus"*).</summary>
    private static ArmorMasteryProfile HealerRobe(int pDef, int maxMp) =>
        new(Robe: new StatMods(PDef: pDef, MaxMp: maxMp));

    /// <summary>Resurrection levels 3-16 — his `healer 3rd.csv` rows, 40 → 74. Levels 1-2 (the cleric's
    /// @20 and @30) stay in Skills.Healer.cs, which concatenates these onto them.
    ///
    /// <para>🔑 <b>SP IS THE BUFF LADDER, RUNG FOR RUNG</b> — his ruling, 2026-08-20: *"Resurrection sp
    /// should match the buffs of the same lvl"*. So a res costs what a blessing learned at the same
    /// character level costs (19k at 40 … 450k at 74), not the far dearer COMBAT band ladder that an
    /// attack or a heal runs on. His draft had exactly one rung off it — 81k at 56, where the buff price
    /// is 42k — and that is now 42k. Restore Mana and Resurrection Field are on the same ladder.</para></summary>
    internal static SkillLevel[] HealerResurrectionRungs()
    {
        float[] exp  = { .35f, .40f, .45f, .50f, .55f, .60f, .65f, .70f, .75f, .80f, .85f, .90f, .95f, 1.00f };
        int[] mp     = { 95, 100, 105, 110, 115, 120, 125, 130, 135, 140, 145, 150, 155, 160 };
        int[] sp     = { 19000, 22000, 32000, 38000, 42000, 45000, 61000, 86000,
                         100000, 145000, 165000, 200000, 330000, 450000 };
        // Cast ticks: 10s to 52, then 9 / 8 / 7 / 6 / 5 and flat 5s from 62.
        int[] cast   = { 100, 100, 100, 90, 80, 70, 60, 50, 50, 50, 50, 50, 50, 50 };
        return Enumerable.Range(0, 14).Select(i => new SkillLevel(
            MpCost: mp[i], SpCost: sp[i], ResExpPct: exp[i], CastTicks: cast[i],
            Description: $"Revive at 30% HP/MP; restore {exp[i] * 100:0}% of lost exp "
                       + $"({cast[i] / 10f:0.#}s cast).")).ToArray();
    }

    /// <summary>Restore Mana levels 4-17 — his rows 44 → 74, continuing the cleric's three.
    ///
    /// <para>🔑 <b>IT IS LOSSLESS AT EVERY RUNG</b>, his 2026-08-17 rule (*"same mana cost as power"*).
    /// The @44 row used to disagree with itself — DESCR *"Transfers 77 MP (costs 77)"* against an MP
    /// column of 79 — and he settled which side wins on 2026-08-20: *"mp cost should match the descr"*.
    /// So the column moved to 77, not the text to 79, and the one lossy rung in the game is gone.</para>
    ///
    /// <para>SP is the BUFF ladder, the same one Resurrection runs on. His @72 rung read 200k, repeating
    /// @70; the buff price at 72 is 330k.</para></summary>
    internal static SkillLevel[] HealerRestoreManaRungs()
    {
        int[] pow = { 77, 86, 94, 102, 106, 108, 113, 116, 120, 124, 129, 133, 140 };
        int[] mp  = { 77, 86, 94, 102, 106, 108, 113, 116, 120, 124, 129, 133, 140 };
        int[] sp  = { 22000, 32000, 38000, 42000, 45000, 61000, 86000,
                      100000, 145000, 165000, 200000, 330000, 450000 };
        return Enumerable.Range(0, 13).Select(i => new SkillLevel(
            MpCost: mp[i], SpCost: sp[i], Power: pow[i],
            Description: $"Transfers {pow[i]} MP to an ally (costs {mp[i]}). Can't be used on "
                       + "yourself or another mana-restorer.")).ToArray();
    }

    /// <summary>Antidote levels 3-9 — his rows at 44/52/58/62/66/70/74, RE-RULED 2026-08-20. The cure's
    /// ladder is a RANK CEILING, never a list: what it cures (poison, venom, bleed) never changes.
    ///
    /// <para>🔑 <b>ITS RANK IS EXACTLY ONE BAND BEHIND HEALER BLESSING'S.</b> His rule: *"value should go
    /// 1 lvl behind the healing blessing the elf one. Elf learns tire 3 at 40 antidote tire 3 at 44...
    /// Elf tire 4 at 48 antidote t4 at 52"*. The Elf first reaches rank 3/4/5/6/7/8/9 at
    /// 40/48/56/60/64/68/72, so Antidote reaches each one band later: 44/52/58/62/66/70/74.</para>
    ///
    /// <para>⚠ That is SEVEN rungs, not the eight his draft carried. The level-64 row gave rank 7 at the
    /// SAME level the Elf gets it (not one behind) and duplicated the 66 row, so it could not satisfy the
    /// rule at any value — it was removed from the CSV rather than given an invented number.</para>
    ///
    /// <para>SP is the ordinary COMBAT band ladder, his other rule the same day: *"Antidote should cost
    /// sp as much as any other passive/active (except buffs and resurrect)"*. MP climbs with it.</para></summary>
    internal static SkillLevel[] HealerAntidoteRungs()
    {
        int[] rank = { 3, 4, 5, 6, 7, 8, 9 };
        int[] mp   = { 34, 42, 50, 53, 57, 60, 64 };
        int[] sp   = { 43000, 74000, 88000, 170000, 280000, 390000, 880000 };
        return Enumerable.Range(0, rank.Length).Select(i => new SkillLevel(
            MpCost: mp[i], SpCost: sp[i], DispelMaxLevel: rank[i],
            Description: $"Cures poison, venom and bleed of rank {rank[i]} or lower from an "
                       + "ally (or self).")).ToArray();
    }

    /// <summary>Anti-Magic levels 8-20 — the healer's continuation of a ladder the base mage starts at
    /// 7 and the cleric carries to 35. It lives HERE rather than in Skills.Mage.cs because these
    /// thirteen rungs are `healer 3rd.csv` rows 44-74 and the CSV they answer to is this discipline's;
    /// Skills.Mage.cs concatenates them onto its own seven.
    ///
    /// <para>⚠ Level 7 (@40) is NOT in this array — it was written before the rest of his file landed
    /// and stays where it is, so the two halves meet at exactly one rung. Rung 8 here is his @44.</para>
    ///
    /// <para>M.Def climbs 49 → 108 and magic RESISTANCE steps 15 → 20 → 25% in three flat plateaus.
    /// mRes is a damage REDUCTION, not a fizzle chance (his ruling, 2026-08-10) — don't route it back
    /// into MagicFailFloor, which is where the CSVs' "mRes" used to land for want of a stat.</para></summary>
    internal static SkillLevel[] HealerAntiMagicRungs() =>
        HealerRungs(1, 13, (i, sp) =>
        {
            int[] mDef   = { 49, 56, 63, 70, 74, 78, 82, 86, 91, 95, 99, 104, 108 };
            float[] mRes = { .15f, .15f, .20f, .20f, .20f, .20f, .20f, .20f, .25f, .25f, .30f, .30f, .30f };
            return new SkillLevel(SpCost: sp,
                Passive: new PassiveEffect(MagicDefence: mDef[i], MagicResist: mRes[i]),
                Description: $"+{mDef[i]} magic defence and {mRes[i] * 100:0}% magic resistance.");
        });
}
