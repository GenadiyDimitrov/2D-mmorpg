namespace Game.Shared;

/// <summary>
/// THE WARCHANTER'S NON-BUFF HALF, 40-74 — every row of
/// <c>docs/data/classes_skills_csv/buffer 3rd.csv</c> that is not a buff, a harmony or a group.
/// Built 2026-08-21 when he finished authoring the file and removed its <c>NOT DONE</c> banner
/// (*"Ok i finished the buffer"*). The buff layer lives in Skills.Warchanter3rd.cs; the singles and
/// harmonies it draws on are in Skills.BuffLadders.cs.
///
/// <para>🔑 <b>THE RACE SPLIT IS THE WHOLE DESIGN HERE</b>, and it is his: *"human is tank - 1dmg
/// skill and higher Def, elf is archer - range/evasion 1dmg skill, demon is mele fighter so need more
/// than 1dmg skill"*. So one buffer class wears three different combat kits:</para>
/// <list type="bullet">
///   <item>HUMAN — heavy armour, blunt + SHIELD (Shield Mastery, Skills.Fighter.cs), one melee
///         damage skill (Sound Smash).</item>
///   <item>ELF — light armour, BOW: the penalty-cancelling Bow Proficiency, a Bow Mastery ladder,
///         Bow Expertise, and a ranged two-hit damage skill (Sound Burst).</item>
///   <item>DEMON — heavy armour, blunt, no shield: TWO melee damage skills, Sound Smash and the
///         stunning Acoustic Shock, plus the Warlock Weapon Mastery ladder.</item>
/// </list>
///
/// <para>⚠ Four things his file names are NOT defined here because they already exist and are simply
/// re-learned by this class at new levels: <c>Anti magic</c>, <c>Resurrection</c>, <c>Great Heal</c>
/// (Skills.Lightbringer.cs) and the two ladders extended in Skills.Healer.cs — <c>Armor Mastery</c>
/// and <c>Spell Mastery</c>, whose rungs 5-14 are the Warchanter's alone.</para>
/// </summary>
public static partial class SkillCatalog
{
    // ---- PASSIVES ----
    public const string WcComboMastery       = "wc_combo_mastery";
    /// <summary>Combo Rush — the proc's buff, ONE family of SIX rungs sharing the key `wc_combo`.
    /// Hidden: never taught, never on a bar, only ever applied by Combo Mastery's proc.
    /// Rungs 1-3 are what your PARTY gets, rungs 4-6 what YOU get; see <see cref="ComboRushRungs"/>.</summary>
    public static readonly string[] WcComboRush =
        { "wc_combo_rush_1", "wc_combo_rush_2", "wc_combo_rush_3",
          "wc_combo_rush_4", "wc_combo_rush_5", "wc_combo_rush_6" };
    public const string WcManaVampirism      = "wc_mana_vampirism";
    // ⚠ THE ID STRINGS BELOW ARE FROZEN AND NO LONGER MATCH THEIR NAMES, ON PURPOSE. Both skills were
    // renamed 2026-08-29 when the class names caught up with the Ork→Demon change (`BL-101`), and a
    // skill id is APPEND-ONLY: characters persist their learned ids, so `wc_chanter_heavy_mastery` and
    // `wc_bloodhanter_blunt_mastery` can never move without orphaning every save that holds them. The
    // C# const identifiers were renamed to match the new names — those are compile-checked and cost
    // nothing — and the id strings stayed. Read the string as a serial number, not as a name.
    public const string WcBufferHeavy        = "wc_chanter_heavy_mastery";      // Human;Demon
    public const string WcHarmonistLight     = "wc_harmonist_light_mastery";    // Elf
    public const string WcHarmonistBowProf   = "wc_harmonist_bow_proficiency";  // Elf
    public const string WcHarmonistBowMast   = "wc_harmonist_bow_mastery";      // Elf
    public const string WcWarlockWeapon      = "wc_bloodhanter_blunt_mastery";  // Demon
    public const string DoctorBluntMastery   = "doctor_blunt_mastery";          // Human, 1H blunt
    // ---- ACTIVES ----
    public const string WcHarmonyRestoration = "wc_harmony_restoration";
    public const string WcSoundBurst         = "wc_sound_burst";        // Elf, bow, hits twice
    public const string WcSoundSmash         = "wc_sound_smash";        // Demon;Human, blunt
    public const string WcAcousticShock      = "wc_acoustic_shock";     // Demon only, blunt + STUN
    public const string WcBowExpertise       = "wc_bow_expertise";      // Elf
    // ---- TOGGLES ----
    public const string WcReinforcement      = "wc_reinforcement";
    public const string WcSharpening         = "wc_sharpening";

    /// <summary>His SP column for the 40-74 band, in file order. Every 14-rung ladder in
    /// `buffer 3rd.csv` carries exactly these numbers, so they are written once.</summary>
    internal static readonly int[] BandSp14 =
        { 36_000, 43_000, 64_000, 74_000, 81_000, 88_000, 120_000, 170_000,
          190_000, 280_000, 320_000, 390_000, 650_000, 880_000 };

    /// <summary>The same column for the ladders that skip 44 and run 40/48/52/56/58/60/62/64/66/68/
    /// 70/72/74 — Sound Burst, Sound Smash, Acoustic Shock, Reinforcement, Sharpening.</summary>
    private static readonly int[] BandSp13 =
        { 36_000, 64_000, 74_000, 81_000, 88_000, 120_000, 170_000,
          190_000, 280_000, 320_000, 390_000, 650_000, 880_000 };

    /// <summary>A Warchanter damage rung: his power ladder is shared verbatim between Sound Burst,
    /// Sound Smash and Acoustic Shock (1000 to 4000 over thirteen rungs), and so is the MP column.</summary>
    private static readonly int[] SoundPower =
        { 1000, 1200, 1400, 1600, 1800, 2000, 2200, 2400, 2700, 3000, 3300, 3700, 4000 };
    private static readonly int[] SoundMp =
        { 62, 76, 83, 90, 95, 98, 100, 105, 108, 112, 114, 117, 120 };

    private static SkillDef[] WarchanterKitSkills()
    {
        var list = new List<SkillDef>();

        // ===== PASSIVES ==========================================================================

        // ---- Heavy Armor Mastery (Human + Demon) — the heavy-armour half of the caster penalty,
        //      bought back. Spellcaster Mastery charges light/heavy/none cast x0.5 and attack x0.5;
        //      his row restores them to "90%(x1.8)" and "100%(x2)", which is why the numbers look
        //      like over-corrections in isolation: 1.8 x 0.5 = 0.9 and 2.0 x 0.5 = 1.0. Same
        //      arithmetic as the cleric's light row (see HealerArmorMastery). ----
        // ⚠ RENAMED 2026-08-29, his call: *"'chanter heavy mastery' should become 'heavy armor mastery'
        //   as human and demon are no longer 'chanter' as class name"*. The Human buffer is a Doctor and
        //   the Demon a Dreadcaller since `BL-100`/`BL-101`; "Chanter" named a class that no longer
        //   exists. 🔑 The TANK already has a "Heavy Armor Mastery" and that is fine: one is Fighter and
        //   one is Mage, no character can hold both, and `Abbreviations` de-duplicates names so the two
        //   simply share a bar label they can never both draw.
        //
        // 🔴 IT REPLACES `armor_mastery` SINCE 2026-09-02 (`BL-119`) — his find: *"I managed to make x4
        //    cast speed with light armor ... both remove the light penalty"*. The cleric's rung 4 also
        //    cancels the Spellcaster penalty, armour masteries stack multiplicatively, and a buffer who
        //    had not yet bought the 40 rung still held it. Superseding it here means the cancel can
        //    only ever be applied once, whichever of the two the character happens to own. The 40+
        //    ladder is `buffer_armor_mastery` now and is NOT replaced: it carries no speed clause, so
        //    the two are additive by design, not duplicates.
        list.Add(new SkillDef(WcBufferHeavy, "Heavy Armor Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { ArmorMasterySkill },
            Description: "Passive. Heavy armour stops hindering you — full attack speed, near-full "
                       + "casting speed — and blunts critical damage against you.",
            Levels: new[] { new SkillLevel(SpCost: 36_000) },
            ArmorMasteryLevels: new[]
            {
                // 🔑 MpRegenPct 0.2 (the ×1.2) MOVED HERE from Armor Mastery on 2026-08-27 — *"the mp
                //    regen is moved to the represented masteries per race"*. Heavy is the Human/Demon
                //    buffer's own weight, so this is the one place he can earn it; Spellcaster Mastery
                //    pays heavy nothing. Still exactly one ×1.2 per mage (`BL-92`).
                new ArmorMasteryProfile(
                    Heavy: new StatMods(CastSpeedPct: 0.80f, AtkSpeedPct: 1.00f, CritDmgResist: 0.15f,
                                        MpRegenPct: 0.2f)),
            }));

        // ---- Harmonist Light Mastery (Elf) — the same trade in light armour, and the elf's own
        //      evasion / crit-rate lean on top. ----
        // 🔴 REPLACES `armor_mastery` — see Heavy Armor Mastery above; this is the exact skill his ×4
        //    cast-speed reading came from, and light is the weight the cleric's rung 4 also covers.
        list.Add(new SkillDef(WcHarmonistLight, "Harmonist Light Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { ArmorMasterySkill },
            Description: "Passive. Light armour stops hindering you — full attack speed, near-full "
                       + "casting speed — and you dodge better and are critted less often.",
            Levels: new[] { new SkillLevel(SpCost: 36_000) },
            ArmorMasteryLevels: new[]
            {
                // 🔑 MpRegenPct 0.2 moved here from Armor Mastery the same day, same reasoning — light
                //    is the ELF buffer's represented weight.
                new ArmorMasteryProfile(
                    Light: new StatMods(CastSpeedPct: 0.80f, AtkSpeedPct: 1.00f,
                                        Evasion: 6, CritRateResist: 0.15f, MpRegenPct: 0.2f)),
            }));

        // ---- Harmonist Bow Proficiency (Elf) — *"Bow: Removed Penalty [cast(x2), mAtk(x2),
        //      mAcc(x0.04)]"*. THE FIRST SKILL THAT UNDOES THE UNTRAINED-WEAPON RULE rather than
        //      working around it. All three numbers are exact inverses of what Spellcaster Mastery
        //      charges a bow (x0.5 cast, x0.5 M.Atk, x25 fizzle), so an Elf Warchanter with a bow is
        //      a full caster — which is the entire reason his elf can be an archer AND a buffer. ----
        list.Add(new SkillDef(WcHarmonistBowProf, "Harmonist Bow Proficiency", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. A bow is no longer an untrained weapon for you: it costs you no "
                       + "casting speed, no magic attack, and no extra chance for spells to fizzle.",
            Levels: new[] { new SkillLevel(SpCost: 36_000) },
            WeaponMasteryLevels: new[]
            {
                new WeaponMasteryProfile(Bow: new PassiveEffect(
                    CastPenaltyMult: 2f, MagicPenaltyMult: 2f, MagicFailSelfMult: 0.04f)),
            }));

        // ---- Harmonist Bow Mastery (Elf) — 8 rungs. Range +400 flat at EVERY rung (it does not
        //      ladder; only the P.Atk does), and the P.Atk climbs 100 to 600. ----
        int[] bowMastAtk = { 100, 200, 300, 400, 500, 540, 560, 600 };
        int[] raceMastSp = { 36_000, 64_000, 81_000, 120_000, 190_000, 320_000, 390_000, 880_000 };
        list.Add(new SkillDef(WcHarmonistBowMast, "Harmonist Bow Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Your bow reaches much further and hits much harder.",
            // Rungs 1-8 are his 40-74 band; 9-16 are `buffer 4th.csv`'s 76-90 (`BL-108`), where the
            // P.Atk jumps 600 → 650 → 1000 and the +400 range stays flat as it always has.
            Levels: raceMastSp.Select(sp => new SkillLevel(SpCost: sp))
                .Concat(BufferFourthEvenRungs(i => $"Bow: +{Wc4BowAtk[i]} P.Atk, +400 range.")).ToArray(),
            WeaponMasteryLevels: bowMastAtk
                .Select(a => new WeaponMasteryProfile(Bow: new PassiveEffect(PhysAtk: a, BowRange: 400f)))
                .Concat(BufferFourthBowProfiles()).ToArray()));

        // ---- Warlock Weapon Mastery (Demon) — 8 rungs, the demon's answer to the elf's bow line.
        //      Flat P.Atk 30 to 100 and a constant +3 accuracy. ----
        //
        // ⚠ RENAMED 2026-08-29. It was "Bloodhanter", which he points out was a TYPO for Bloodchanter —
        //   and rather than fix the typo he retired the word: *"as we changed the orks to demons and
        //   changed the classes names -> so rename it to 'warlock weapon mastery' the 4th classes
        //   name"*. Warlock is the Demon buffer's 4th class (`Classes.Names.cs`). 🔑 The IP test passes
        //   on his own rule — word + SAME RACE + SAME ROLE: ours is a BUFFER, IG's is a summoner.
        //   ⚠ "Blunt" left the name too, because the requirement now lives in the WEAPON column
        //   (`blunt/2`), not in prose.
        //
        // 🔑 TWO-HANDED ONLY since 2026-08-29 (owner: *"for demon buffer it's maul/staff (2h blunt)"*).
        //    His own CSV section header has said "Bloodhanter TWO HAND Mastery" since the file landed;
        //    the rows read "Blunt:" only because a bare type means any hands and there was no way to
        //    write the other half. The three buffers now read: Human 1H blunt (mace/wand, and his own
        //    Shield Mastery is what pushes him there), Elf bow, Demon 2H blunt (maul/staff).
        // ⚠ The SHARED Spell Mastery stays hands-agnostic blunt-or-bow on purpose — his ruling:
        //    *"the spell mastery ... they share one so we gate only the type, and their additional
        //    passives are hands gated"*. Do not push hands up into BufferMastery.
        int[] bluntAtk = { 30, 40, 50, 60, 70, 80, 90, 100 };
        list.Add(new SkillDef(WcWarlockWeapon, "Warlock Weapon Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. A TWO-HANDED blunt weapon — a maul or a staff — strikes harder "
                       + "and truer in your hands. No effect one-handed.",
            Levels: raceMastSp.Select(sp => new SkillLevel(SpCost: sp))
                .Concat(BufferFourthEvenRungs(i =>
                    $"Two-handed blunt: +{Wc4BluntAtk[i]} P.Atk, +{Wc4WarlockAcc[i]} accuracy.")).ToArray(),
            WeaponMasteryLevels: bluntAtk
                .Select(a => new WeaponMasteryProfile(Blunt: new PassiveEffect(PhysAtk: a, Accuracy: 3),
                                                     RequiredWeapon: WeaponType.AnyBlunt,
                                                     RequiredHands: WeaponHands.Two))
                .Concat(BufferFourthWarlockProfiles()).ToArray()));

        // ---- Doctor Weapon Mastery (HUMAN) — his 2026-09-02 addition to `buffer 3rd.csv`, and the
        //      row that finally gives the third buffer a weapon line of its own. Eight rungs on the
        //      same 40/48/56/60/64/68/70/74 band and the same P.Atk 30 → 100 as the Demon's, one axis
        //      apart: `blunt/1`, ONE-HANDED, because the Human buffer is the shield one (`BL-107` —
        //      all four Shield Mastery rungs are his) and a maul would cost him the shield. That is
        //      also why he gets no accuracy where the Demon gets +3: the shield is the compensation.
        //
        // ⚠ HIS DESCR CELL READ "2h Blunt:" ON ALL EIGHT ROWS while the WEAPON column read `blunt/1` —
        //   the prose was a copy of the Warlock block above it. Raised, and he settled it the same day
        //   (2026-09-02): the cells now read "Blunt:", matching his own 76-90 rows, and the hands stay
        //   in the WEAPON column where they are checkable. That is the `BL-105` rule working — a
        //   requirement written in prose cannot be compared to the one the engine enforces.
        list.Add(new SkillDef(DoctorBluntMastery, "Doctor Weapon Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. A ONE-HANDED blunt weapon — a mace or a wand, the hand that keeps "
                       + "your shield — strikes harder in your hands. No effect two-handed.",
            Levels: raceMastSp.Select(sp => new SkillLevel(SpCost: sp))
                .Concat(BufferFourthEvenRungs(i =>
                    $"One-handed blunt: +{Wc4BluntAtk[i]} P.Atk, +{Wc4DoctorAcc[i]} accuracy.")).ToArray(),
            WeaponMasteryLevels: bluntAtk
                .Select(a => new WeaponMasteryProfile(Blunt: new PassiveEffect(PhysAtk: a),
                                                     RequiredWeapon: WeaponType.AnyBlunt,
                                                     RequiredHands: WeaponHands.One))
                .Concat(BufferFourthDoctorProfiles()).ToArray()));

        // ---- Mana Vampirism — 3 rungs @40/60/70. His only mana-return line, and the reason the
        //      blunt buffer can keep buffing: a slice of a BASIC attack.s damage back as MP.
        //      ⚠ ManaVamp is its own field, not MeleeVamp — see PassiveEffect.
        //
        //      RETUNED 2026-08-23, playtest 27: *"Should lower the buffers mana vamp - to op - same
        //      levels just 1,1.5,2% or 10% on 10/15/20% chance"*. It was 3/7/10% of EVERY blunt hit,
        //      unconditionally, which on a buffer who attacks all day is a second mana bar.
        //
        //      🔑 His two options are the SAME EXPECTED VALUE — 10% x 10/15/20% chance is 1/1.5/2%
        //      — so this is a feel question, not a numbers one, and the FLAT one won: a sustain line
        //      is the wrong place for variance. You want to know whether you can keep buffing, not
        //      roll for it. The proc version is one ProcChance field away if he wants the spike. ----
        //
        // 🔴 BLUNT **OR BOW** — fixed 2026-08-29, his correction: *"the mana vamp works on basic attack
        //    with required weapon blunt or bow ... not only blunt"*. His CSV row has always said
        //    `Require: Bow/Blunt`; only the code said blunt. Same shape as the Combo Mastery bug found
        //    the same day, and the reason the WEAPON column now exists: a requirement written in prose
        //    cannot be compared to the one the engine enforces.
        list.Add(new SkillDef(WcManaVampirism, "Mana Vampirism", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Your basic attacks with a blunt weapon or a bow drain mana back to you.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 36_000),
                new SkillLevel(SpCost: 120_000),
                new SkillLevel(SpCost: 390_000),
            },
            WeaponMasteryLevels: new[]
            {
                new WeaponMasteryProfile(Blunt: new PassiveEffect(ManaVamp: 0.010f),
                                         Bow:   new PassiveEffect(ManaVamp: 0.010f)),
                new WeaponMasteryProfile(Blunt: new PassiveEffect(ManaVamp: 0.015f),
                                         Bow:   new PassiveEffect(ManaVamp: 0.015f)),
                new WeaponMasteryProfile(Blunt: new PassiveEffect(ManaVamp: 0.020f),
                                         Bow:   new PassiveEffect(ManaVamp: 0.020f)),
            }));

        // ---- Combo Mastery — 3 rungs @52/64/74, and THE FIRST ON-HIT PROC IN THE GAME.
        //      *"Doing Damage Increases Attack/Cast Speed ... With 3% Chance"*, 30s, 60s internal
        //      cooldown. See ComboRushRungs below for the buff it hands out and why the
        //      caster and the party take different rungs of ONE family. ----
        //
        // 🔴 BLUNT **OR BOW** — fixed 2026-08-29. His CSV row says *"Require: Box/Blunt"* (Bow/Blunt)
        //    and this skill is in the SHARED kit, taught to all three races — but it was gated `Blunt`
        //    alone, so the ELF Warchanter, whose whole identity is the bow, could never once proc a
        //    passive he had paid 74k-880k SP for. Nothing in the game said so: a proc that never fires
        //    looks exactly like a 3% roll that keeps missing. It is the same blunt-or-bow pair the
        //    shared Spell Mastery uses, and hands-agnostic for the same reason.
        int[] comboSp = { 74_000, 190_000, 880_000 };
        list.Add(new SkillDef(WcComboMastery, "Combo Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 600, Range: 0, Power: 0,
            DurationTicks: 300,
            Category: SkillCategory.Passive,
            RequiredWeapon: WeaponType.AnyBlunt | WeaponType.Bow,
            // 🔑 TWO CHANCES, ONE PROC (`BL-120`, owner 2026-09-02): *"3% chance with blunt/1 and 3.45%
            //    chance with bow|blunt/2"*, because *"2h weapons are slower by ~12/18%, so increasing
            //    the chance balances the slower attack speed (bow is faster than 2h blunt as harmonist
            //    have bow expertise)"*. The proc is rolled per LANDED HIT, so the slower weapon rolls
            //    it less often; 3.45/3.00 = ×1.15 is the middle of his own 12-18%. The gate is
            //    unchanged — blunt or bow, any hands — this only splits the number once you are past it.
            ProcChance: 0.03f, ProcChanceTwoHanded: 0.0345f, ProcCooldownTicks: 600,
            // Level 1 hands the caster rung 4 and the party rung 1; level 2, rungs 5 and 2; level 3,
            // rungs 6 and 3. His mapping, verbatim: *"u get 4,5,6 while party gets 1,2,3"*.
            ProcSelfRungs:  new[] { WcComboRush[3], WcComboRush[4], WcComboRush[5] },
            ProcPartyRungs: new[] { WcComboRush[0], WcComboRush[1], WcComboRush[2] },
            Description: "Passive. Landing a blow with a blunt weapon or a bow can send a surge "
                       + "through you and your party — faster attacks and faster casting for 30s. "
                       + "A two-handed weapon or a bow procs it slightly more often, to pay for its "
                       + "slower swing.",
            Levels: comboSp.Select((sp, i) => new SkillLevel(SpCost: sp,
                Description: $"3% chance on hit (3.45% with a bow or a two-handed blunt): "
                           + $"+{ComboAs[i + 3] * 100:0.#}% attack and "
                           + $"+{ComboCast[i + 3] * 100:0.#}% cast speed for you, "
                           + $"+{ComboAs[i] * 100:0.#}%/+{ComboCast[i] * 100:0.#}% for the party, 30s."))
                .ToArray()));

        list.AddRange(ComboRushRungs());

        // ===== ACTIVES ===========================================================================

        // ---- Harmony of Restoration — the party heal-over-time, 14 rungs, replacing PARTY HEAL.
        //      ⚠ His CSV column said `[Quick Heal]` and he corrected it himself on 2026-08-21:
        //      *"harmony of restoration (my bad that I have forgot) but need to replace party heal"*.
        //      It is the right way round — this is the party heal, so it supersedes the party heal;
        //      Great Heal already takes Heal, and Quick Heal survives as the fast single-target one
        //      the Warchanter still needs. The CSV rows moved with this.
        //      +30 to +100 HP/s for 30s, and from rung 9 (@64) it also carries MP/s. The MP half
        //      rides RestoreMp's Flat magnitude on a lasting buff — see TickHealOverTime; there were
        //      no SkillEffect bits left for an "MP over time" of its own. ----
        int[] hotHp   = { 30, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100 };
        int[] hotMp   = { 0, 0, 0, 0, 0, 0, 0, 0, 5, 5, 5, 5, 5, 10 };
        int[] hotCost = { 238, 272, 304, 352, 360, 380, 392, 400, 420, 432, 448, 452, 458, 464 };
        list.Add(new SkillDef(WcHarmonyRestoration, "Harmony of Restoration", BaseClass.Mage,
            SkillEffect.HealOverTime | SkillEffect.RestoreMp,
            MpCost: hotCost[0], CastTicks: 20, CooldownTicks: 100, Range: 600, Power: 0,
            DurationTicks: 300, BuffKey: "wc_restoration", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Heal, TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { PartyHeal },
            Description: "A sustained hymn: heals you and your party a little every second for 30s.",
            Levels: Enumerable.Range(0, 14).Select(i => new SkillLevel(
                MpCost: hotCost[i], SpCost: BandSp14[i],
                Magnitudes: hotMp[i] > 0
                    ? new EffectMagnitude[]
                      {
                          new(SkillEffect.HealOverTime, hotHp[i], ModifierMode.Flat),
                          new(SkillEffect.RestoreMp, hotMp[i], ModifierMode.Flat),
                      }
                    : new EffectMagnitude[] { new(SkillEffect.HealOverTime, hotHp[i], ModifierMode.Flat) },
                Description: hotMp[i] > 0
                    ? $"Restores {hotHp[i]} HP and {hotMp[i]} MP per second to the party for 30s."
                    : $"Restores {hotHp[i]} HP per second to the party for 30s."))
                .Concat(BufferFourthRestorationRungs()).ToArray()));

        // ---- Sound Burst (Elf) — 900 range, BOW, and it hits TWICE. Two independent resolutions of
        //      the same power, not one hit at double power: see SkillDef.HitCount. ----
        list.Add(SoundSkill(WcSoundBurst, "Sound Burst", WeaponType.Bow, range: 900, castTicks: 30,
            hits: 2, stunTicks: 0,
            desc: "Looses two arrows on one breath — each resolves on its own."));

        // ---- Sound Smash (Demon + Human) — the melee twin: 40 range, blunt, one hit, faster cast. ----
        list.Add(SoundSkill(WcSoundSmash, "Sound Smash", WeaponType.Blunt, range: 40, castTicks: 10,
            hits: 1, stunTicks: 0,
            desc: "A concussive blow that rings through armour."));

        // ---- Acoustic Shock (DEMON ONLY) — HIS ADDITION, 2026-08-21: *"Add another skill to the ork
        //      buffer same as sound smash (name it Acoustic Shock) just with a stun effect ... demon is
        //      mele fighter so need more than 1dmg skill"*. Identical ladder to Sound Smash — same
        //      power, MP, SP, range, cast and reuse — with a contested 5s STUN on top. That is the
        //      demon's second damage skill, and the reason it is worth pressing over Sound Smash.
        //      ⚠ Stun is CONTESTED (ATK vs CON, DebuffSchool.Physical) like every other CC in the
        //      game, so it is not a guaranteed lock and bosses are immune. ----
        list.Add(SoundSkill(WcAcousticShock, "Acoustic Shock", WeaponType.Blunt, range: 40, castTicks: 10,
            hits: 1, stunTicks: 50,
            desc: "A blow pitched to shatter the senses: damage, and the target reels."));

        // ---- Bow Expertise (Elf) — his own rung, NOT the rogue's. The rogue's `bow_expertise` is
        //      +8% for 22000 SP; his buffer row is +12% for 42000 and 85 MP. Same BuffKey at a higher
        //      Rank, so the two never stack and the buffer's wins if a character ever held both. ----
        list.Add(new SkillDef(WcBowExpertise, "Bow Expertise", BaseClass.Mage, SkillEffect.BuffAtkSpeed,
            MpCost: 85, CastTicks: 30, CooldownTicks: 20, Range: 0, Power: 0,
            DurationTicks: 12000, BuffKey: "bow_expertise", Rank: 2,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly, SpCost: 42_000,
            RequiredWeapon: WeaponType.Bow,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtkSpeed, 0.12f) },
            Description: "Steadies your aim: +12% attack speed while wielding a bow, for 20 minutes."));

        // ===== TOGGLES ===========================================================================
        // Both are stances: instant on, instant off, and they burn MP every second while lit
        // (SkillDef.MpPerSecond, drained by the tick loop). His "(Consumes: N/s)" IS that number, and
        // his MP column carries the same N — a toggle's "cost" is its per-second burn, not a one-off.

        int[] reinforceDef = { 240, 260, 280, 300, 320, 340, 380, 400, 440, 480, 520, 560, 600 };
        int[] reinforceMp  = { 12, 13, 14, 15, 16, 17, 19, 20, 22, 24, 26, 28, 30 };
        list.Add(BuildStance(WcReinforcement, "Reinforcement", SkillEffect.BuffDef, "wc_reinforcement",
            reinforceDef, reinforceMp,
            "Brace yourself: greater physical defence for as long as you can pay for it.",
            BufferFourthReinforcementRungs()));

        int[] sharpenAtk = { 60, 80, 100, 120, 140, 160, 180, 200, 220, 240, 260, 280, 300 };
        int[] sharpenMp  = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        list.Add(BuildStance(WcSharpening, "Sharpening", SkillEffect.BuffPhysAtk, "wc_sharpening",
            sharpenAtk, sharpenMp,
            "Hone your weapon: greater physical attack for as long as you can pay for it.",
            BufferFourthSharpeningRungs()));

        return list.ToArray();
    }
    // ===== COMBO RUSH — the proc's buff, and the one ladder in the game that is NOT monotonic =====
    //
    // His design, 2026-08-21: *"Cast speed goes 5->10->15%, atack speed goes 10->15->20% and half of
    // both goes to the party as buff (u get the 20% and party 10%) -> so something like 6 levels of
    // that passives proc-buff and u get 4,5,6 while party gets 1,2,3"*.
    //
    //   rung | atk spd | cast | who gets it
    //   -----+---------+------+-------------------------------------------
    //     1  |    5%   | 2.5% | your PARTY, from a Combo Mastery L1 buffer
    //     2  |  7.5%   |   5% | your PARTY, from an L2 buffer
    //     3  |   10%   | 7.5% | your PARTY, from an L3 buffer
    //     4  |   10%   |   5% | YOU, at Combo Mastery L1
    //     5  |   15%   |  10% | YOU, at L2
    //     6  |   20%   |  15% | YOU, at L3
    //
    // 🔑 ONE FAMILY IS THE WHOLE MECHANISM. All six share the key `wc_combo`, so the ordinary
    // ApplyBuff rule (same family -> higher Rank wins, weaker is ignored entirely) does everything:
    // your own rung 4-6 simply outranks any rung 1-3 a party-mate's proc throws at you. Nothing is
    // special-cased, and two buffers in one party never fight over a bar square.
    //
    // ⚠⚠ RUNG 3 -> RUNG 4 GOES BACKWARDS ON CAST SPEED (7.5% -> 5%) AND THAT IS DELIBERATE. A ladder
    // that moves backwards normally means a typo — see the monotonic rule — but here it falls out of
    // ranking "half of a strong buffer's" above "all of a weak buffer's", and HE CALLED IT in the same
    // breath as the design: *"even if some other buffer procs lvl 3 buff u still get your effect over
    // (loosing only 2% cast in the process)"*. That IS this row: an L1 buffer standing next to an L3
    // buffer keeps his own rung 4 and forgoes the 2.5% extra cast speed rung 3 would have handed him.
    // Do NOT straighten it into a rising line.
    private static readonly float[] ComboAs   = { 0.05f, 0.075f, 0.10f, 0.10f, 0.15f, 0.20f };
    private static readonly float[] ComboCast = { 0.025f, 0.05f, 0.075f, 0.05f, 0.10f, 0.15f };

    /// <summary>The six Combo Rush rungs. HIDDEN — never taught, never on a bar, no SP and no learn row
    /// anywhere; the only thing that ever applies one is Combo Mastery's proc. Rungs 1-3 are cast on the
    /// PARTY (hence the radius) and 4-6 on the caster, but every rung carries the same BuffKey and its
    /// index as Rank, which is what makes them compete instead of stacking.</summary>
    private static IEnumerable<SkillDef> ComboRushRungs() =>
        Enumerable.Range(0, 6).Select(i => new SkillDef(
            WcComboRush[i], "Combo Rush", BaseClass.Mage,
            SkillEffect.BuffAtkSpeed | SkillEffect.BuffCastSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "wc_combo", Rank: i + 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff,
            TargetMode: i < 3 ? TargetMode.AlliesInRadius : TargetMode.SelfOnly,
            AreaRadius: i < 3 ? 800f : 0f,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffAtkSpeed, ComboAs[i]),
                new(SkillEffect.BuffCastSpeed, ComboCast[i]),
            },
            Description: $"A surge of momentum: +{ComboAs[i] * 100:0.#}% attack speed and "
                       + $"+{ComboCast[i] * 100:0.#}% cast speed for 30s."));

    /// <summary>A Warchanter MP-per-second stance (Reinforcement / Sharpening). Thirteen rungs on the
    /// 40/48/52...74 band, one flat stat each, and a per-second MP burn that IS his "(Consumes: N/s)".</summary>
    /// <param name="fourth">The 76-90 rungs from `buffer 4th.csv` (`BL-108`), appended. Null before
    /// that file was built; both stances have eight of them.</param>
    private static SkillDef BuildStance(string id, string name, SkillEffect effect, string buffKey,
        int[] amounts, int[] mpPerSec, string desc, SkillLevel[]? fourth = null) =>
        new(id, name, BaseClass.Mage, effect,
            MpCost: mpPerSec[0], CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 0, BuffKey: buffKey, Rank: 1,
            Category: SkillCategory.Buff, Toggle: true, TargetMode: TargetMode.SelfOnly,
            MpPerSecond: mpPerSec[0],
            Magnitudes: new EffectMagnitude[] { new(effect, amounts[0], ModifierMode.Flat) },
            Description: "Toggle. " + desc,
            Levels: Enumerable.Range(0, amounts.Length).Select(i => new SkillLevel(
                MpCost: mpPerSec[i], MpPerSecond: mpPerSec[i], SpCost: BandSp13[i],
                Magnitudes: new EffectMagnitude[] { new(effect, amounts[i], ModifierMode.Flat) },
                Description: $"{name} — +{amounts[i]} while active, {mpPerSec[i]} MP per second."))
                .Concat(fourth ?? Enumerable.Empty<SkillLevel>()).ToArray());

    /// <summary>One of the three "Sound" damage skills. They share his power ladder, his MP column and
    /// his SP column verbatim; what differs is the weapon, the range, the cast, how many times a cast
    /// resolves, and (Acoustic Shock only) a contested stun.</summary>
    private static SkillDef SoundSkill(string id, string name, WeaponType weapon, float range,
        int castTicks, int hits, int stunTicks, string desc)
    {
        var effect = SkillEffect.PhysicalDamage | (stunTicks > 0 ? SkillEffect.Stun : SkillEffect.None);
        return new SkillDef(id, name, BaseClass.Mage, effect,
            MpCost: SoundMp[0], CastTicks: castTicks, CooldownTicks: 30, Range: range, Power: SoundPower[0],
            Category: SkillCategory.Physical,
            // 🔑 A SOUND SKILL RETIRES HOLY BOLT (owner, playtest 28: *"holy bolt should be replaced from
            // sound smash/burst — [they] are the attack skills of buffers; healers replace [it] with a
            // stronger one, same should be valit for the buffers"*). He is describing something the
            // healer already does and the buffer did not: Holy Ray carries `Replaces: [HolyStrike]`, so a
            // Lightbringer's Learn tab and bar lose the obsolete bolt the moment the real spell arrives.
            // The Warchanter inherited Holy Bolt from the cleric tier and kept it forever beside a kit
            // that was supposed to have superseded it.
            //
            // ⚠ ALL THREE carry it, not just the first one a race learns. Sound Smash and Acoustic Shock
            // are both learnable at 40 by an demon, in whichever order he buys them — putting the clause on
            // one of them would make the retirement depend on the shopping order.
            //
            // ⚠ THE TRADE IS REAL AND IT IS HIS TO ACCEPT: Holy Bolt is a SPELL with no weapon
            // requirement, and all three of these are weapon-gated (blunt, blunt, bow). A Warchanter
            // caught with the wrong weapon in his hands now has no attack skill at all rather than a weak
            // one. That is consistent with the rest of his 3rd-class design — each race's Warchanter is
            // built around one weapon — but it is a door closing, not just a door opening.
            Replaces: new[] { HolyStrike },
            RequiredWeapon: weapon, HitCount: hits,
            DurationTicks: stunTicks,
            DebuffSchool: stunTicks > 0 ? DebuffSchool.Physical : DebuffSchool.None,
            Description: desc,
            Levels: Enumerable.Range(0, SoundPower.Length).Select(i => new SkillLevel(
                Power: SoundPower[i], MpCost: SoundMp[i], SpCost: BandSp13[i],
                Magnitudes: stunTicks > 0
                    ? new EffectMagnitude[] { new(SkillEffect.Stun, 1f, ModifierMode.Flat) }
                    : null,
                Description: stunTicks > 0
                    ? $"Strikes for power {SoundPower[i]} and stuns for {stunTicks / 10f:0.#}s."
                    : hits > 1
                        ? $"Strikes {hits} times for power {SoundPower[i]} each."
                        : $"Strikes for power {SoundPower[i]}."))
                .Concat(BufferFourthSoundRungs(hits, stunTicks)).ToArray());
    }
}
