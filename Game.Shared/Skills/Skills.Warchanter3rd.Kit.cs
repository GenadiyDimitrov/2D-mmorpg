namespace Game.Shared;

/// <summary>
/// THE WARCHANTER'S NON-BUFF HALF, 40-90 — every row of
/// <c>docs/data/classes_skills_csv/buffer 3rd.csv</c> / <c>buffer 4th.csv</c> that is not a buff, a harmony
/// or a group. The buff layer lives in Skills.Warchanter3rd.cs; the singles and harmonies it draws on are
/// in Skills.BuffLadders.cs.
///
/// <para>🔑 <b>THE WARCHANTER HITS WITH MAGIC</b> (`BL-335`, built 2026-10-06; design and measurements in
/// <c>docs/design/MagicMeleeBuffers.md</c>). His reason: *"we have warriors and tanks and I want to give
/// them something unique"*. All three races wear a ROBE, swing a magic weapon, and every hit they deal is
/// MAGIC:</para>
/// <list type="bullet">
///   <item>HUMAN — wand + shield. Sound Smash. Sharpening needs the shield.</item>
///   <item>DEMON — battlestaff (two-handed blunt). Sound Smash + the stunning Acoustic Shock.</item>
///   <item>ELF — fangs (duals). Magic Stab, a melee spell with a high fail chance.</item>
/// </list>
/// <para>The BASIC ATTACK becomes a magic hit through <see cref="MagicSwing"/> (no cast, nothing to
/// interrupt, attack speed paces it). Two TOGGLES per race buy back what the robe gave up — Reinforcement
/// (defence) and Sharpening (the race's other half) — each for +15% skill MP and an MP-per-second upkeep.</para>
///
/// <para>🔑 <b>DIFFERENT NUMBERS PER RACE = DIFFERENT IDS</b> (`BL-327`: a face never changes a number). So the
/// Human keeps the original ids (`sound_smash`, `reinforcement`, `sharpening`, `combo_mastery`) and the
/// Demon and Elf have their own. A character holds one race, so no two of a set can ever meet.</para>
///
/// <para>⚠ <b>THE NUMBERS ARE MEASURED, NOT HIS YET.</b> Powers and the swing ladder come from
/// `BalanceMatrix --magicmelee` at every learn level (2026-10-06); the toggles' P.Def % from its §C gap,
/// held to a rising line. Monotonic smoothing was applied where gear tiers made the measurement dip. He tunes
/// them in the CSV after the playtest.</para>
/// </summary>
public static partial class SkillCatalog
{
    // ---- PASSIVES ----
    /// <summary>`BL-335` — Resonant Strikes: the basic attack resolves as MAGIC at this rung's power. ONE id;
    /// each race climbs it on its own schedule (ClassSkillTables), which is why its ladder skips.</summary>
    public const string MagicSwing         = "magic_swing";
    public const string ComboMastery       = "combo_mastery";        // Human, 1H blunt, 3%
    public const string ComboMasteryDemon  = "combo_mastery_demon";  // Demon, 2H blunt, 3.5%
    public const string ComboMasteryElf    = "combo_mastery_elf";    // Elf, duals, 2.6%
    /// <summary>Combo Rush — the proc's buff, ONE family of SIX rungs sharing the key `wc_combo`.
    /// Hidden: never taught, never on a bar, only ever applied by a Combo Mastery proc.
    /// Rungs 1-3 are what your PARTY gets, rungs 4-6 what YOU get; see <see cref="ComboRushRungs"/>.</summary>
    public static readonly string[] WcComboRush =
        { "wc_combo_rush_1", "wc_combo_rush_2", "wc_combo_rush_3",
          "wc_combo_rush_4", "wc_combo_rush_5", "wc_combo_rush_6" };
    public const string ManaVampirism      = "mana_vampirism";
    /// <summary>Elf. Cancels the untrained-weapon caster penalty on DUALS. It replaced
    /// `harmonist_bow_proficiency` when the elf put the bow down (`BL-335`).</summary>
    public const string HarmonistDualProficiency = "harmonist_dual_proficiency";
    // ---- ACTIVES ----
    public const string HarmonyOfRestoration = "harmony_of_restoration";
    public const string SoundSmash         = "sound_smash";          // Human + Demon, any blunt
    public const string AcousticBash       = "acoustic_bash";        // Human only, shield + STUN + aggro
    public const string AcousticShock      = "acoustic_shock";       // Demon only, blunt + STUN
    public const string MagicStab          = "magic_stab";           // Elf, duals, high fail
    // ---- TOGGLES ----
    public const string Reinforcement      = "reinforcement";        // Human
    public const string ReinforcementDemon = "reinforcement_demon";
    public const string ReinforcementElf   = "reinforcement_elf";
    public const string Sharpening         = "sharpening";           // Human, shield
    public const string SharpeningDemon    = "sharpening_demon";     // 2H blunt
    public const string SharpeningElf      = "sharpening_elf";       // duals

    /// <summary>His SP column for the 40-74 band, in file order. Every 14-rung ladder in
    /// `buffer 3rd.csv` carries exactly these numbers, so they are written once.</summary>
    internal static readonly int[] BandSp14 =
        { 36_000, 43_000, 64_000, 74_000, 81_000, 88_000, 120_000, 170_000,
          190_000, 280_000, 320_000, 390_000, 650_000, 880_000 };

    /// <summary>The same column for the ladders that skip 44 and run 40/48/52/56/58/60/62/64/66/68/
    /// 70/72/74. ⚠ The class prices come from the CSV (`ClassSkillTables.SpPrices`); this is the fallback.</summary>
    private static readonly int[] BandSp13 =
        { 36_000, 64_000, 74_000, 81_000, 88_000, 120_000, 170_000,
          190_000, 280_000, 320_000, 390_000, 650_000, 880_000 };

    /// <summary>The old physical "Sound" power ladder. No Warchanter skill reads it any more; the Warrior's
    /// Sundering Blow and the retired archer kit still do.</summary>
    private static readonly int[] SoundPower =
        { 1000, 1200, 1400, 1600, 1800, 2000, 2200, 2400, 2700, 3000, 3300, 3700, 4000 };
    private static readonly int[] SoundMp =
        { 62, 76, 83, 90, 95, 98, 100, 105, 108, 112, 114, 117, 120 };

    // ═══ THE NUMBERS (`BL-335`, measured 2026-10-06) ═════════════════════════════════════════════════
    // 3rd tier = 13 rungs @40 48 52 56 58 60 62 64 66 68 70 72 74; 4th = 15 rungs @76…90 (damage) or
    // 8 rungs @76 78 … 90 (toggles).

    /// <summary>The magic swing's power ladder: the union of the three races' measured values. Each race
    /// learns only the rungs its own measurement reaches (see ClassSkillTables).</summary>
    internal static readonly int[] MagicSwingPower =
        { 10, 12, 13, 14, 15, 16, 17, 18, 20, 21, 23, 24, 25, 26 };

    // His second pass (2026-10-06): Smash and Acoustic Shock hit at HOLY RAY's power (the healer's nuke, rung for
    // rung), Magic Stab at Holy Ray x1.5 (*"so at 90 about 160ish"*). All three are half power in PvP: *"Buffers
    // can farm but not stronger in pvp. They are annoying but not strong."* (Acoustic Bash does no damage.)
    // (The first pass, the same day, was Smash/Stab /3 and the Demon's Smash x1.5.)
    private static readonly int[] HolyRayPower =
        { 42, 52, 57, 63, 66, 68, 71, 74, 77, 79, 82, 84, 87,
          88, 90, 91, 93, 94, 96, 99, 100, 101, 102, 103, 105, 106, 108, 109 };
    private static readonly int[] StabElf =
        { 63, 78, 86, 95, 99, 102, 107, 111, 116, 119, 123, 126, 131,
          132, 135, 137, 140, 141, 144, 149, 150, 152, 153, 155, 158, 159, 162, 164 };
    /// <summary>Acoustic Bash's aggro: HALF the tank's Taunt (`provoke`) at the same level (his rule). The tank's
    /// 4th tier climbs every other level, so the odd levels take the midpoint.</summary>
    private static readonly int[] BashTaunt =
        { 3250, 3750, 4250, 4500, 4750, 5000, 5250, 5500, 5600, 5700, 5800, 5900, 6000,
          6200, 6400, 6600, 6800, 7000, 7200, 7400, 7600, 7800, 8000, 8200, 8400, 8600, 8800, 9000 };
    /// <summary>The PvP factor on every Warchanter damage spell.</summary>
    private const float WarchanterPvp = 0.5f;

    /// <summary>The melee pair's MP (his 2026-10-02 edit), 3rd then 4th tier — unchanged by `BL-335`.</summary>
    private static readonly int[] SoundMeleeMp =
        { 36, 43, 47, 50, 55, 56, 58, 62, 65, 68, 70, 75, 78,
          80, 82, 84, 86, 88, 90, 92, 95, 98, 100, 102, 104, 106, 108, 110 };
    /// <summary>Magic Stab keeps Sound Burst's MP column.</summary>
    private static readonly int[] MagicStabMp =
        { 62, 76, 83, 90, 95, 98, 100, 105, 108, 112, 114, 117, 120,
          123, 126, 129, 132, 135, 138, 141, 144, 147, 150, 159, 168, 177, 186, 195 };

    /// <summary>Magic Stab's own fizzle, in points on top of the ordinary curve (~60% at parity; §8 D).</summary>
    internal const float MagicStabFailPoints = 59f;

    // ---- THE TOGGLES: 13 rungs 40-74, then 8 rungs 76-90 --------------------------------------------
    /// <summary>MP per second: HALF of the old Sharpening's (his answer: *"half of today's Sharpening"* each,
    /// so both lit = the old one).</summary>
    private static readonly int[] StanceMpPerSec =
        { 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8,   8, 8, 8, 8, 8, 8, 8, 8 };
    /// <summary>+15% skill MP per toggle (his *"+15% MP cost each"*); MP-cost modifiers ADD, so both = +30%.</summary>
    internal const float StanceMpSurcharge = 0.15f;

    /// <summary>Reinforcement's P.Def — a PERCENT, so it follows the NPC shelf as heavy armour does
    /// (§8 C: a flat cannot). Human/Demon close the heavy gap, the elf the light-mastery gap.</summary>
    private static readonly float[] ReinforceHuman =
        { .22f, .225f, .23f, .235f, .24f, .245f, .25f, .255f, .26f, .265f, .27f, .275f, .28f,
          .36f, .365f, .37f, .375f, .38f, .385f, .39f, .40f };
    private static readonly float[] ReinforceDemon =
        { .22f, .225f, .23f, .235f, .24f, .245f, .25f, .255f, .26f, .265f, .27f, .275f, .28f,
          .29f, .30f, .305f, .31f, .315f, .32f, .325f, .33f };
    private static readonly float[] ReinforceElf =
        { .09f, .0925f, .095f, .0975f, .10f, .1025f, .105f, .1075f, .11f, .1125f, .115f, .1175f, .12f,
          .15f, .155f, .16f, .165f, .17f, .175f, .18f, .185f };
    /// <summary>The Demon's Sharpening: accuracy only (Hit Rate Mastery's +3/+4/+5 moved in). Its M.Atk % left
    /// 2026-10-06: *"Demon basic attacks do alot of dmg so remove the matk increase form sharpening"*.</summary>
    private static readonly int[] SharpenDemonAcc =
        { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,   4, 4, 4, 4, 4, 5, 5, 5 };
    /// <summary>The Elf's Sharpening: evasion (light armour's, §8 C) and spell damage, plus a flat
    /// +20 M.Accuracy that cuts Magic Stab's fail 60% → 40%.</summary>
    private static readonly int[] SharpenElfEva =
        { 6, 6, 7, 7, 7, 8, 8, 8, 8, 9, 9, 9, 9,   9, 10, 11, 11, 11, 11, 11, 12 };
    private static readonly float[] SharpenElfSpell =
        { .05f, .0525f, .055f, .0575f, .06f, .0625f, .065f, .0675f, .07f, .0725f, .075f, .0775f, .08f,
          .085f, .09f, .095f, .10f, .105f, .11f, .115f, .12f };
    private const float SharpenElfMAcc = 20f;

    private static SkillDef[] WarchanterKitSkills()
    {
        var list = new List<SkillDef>();

        // ===== PASSIVES ==========================================================================

        // ---- Resonant Strikes — THE MAGIC SWING (his 2026-10-05 call: *"make the 0mp spell a passive
        //      that swaps the basic attack action to a magic dmg one ... then no need for cast speed and
        //      reuse because attack speed will measure them"*). The power per rung is the number the
        //      swing resolves at in GameLoopService.ResolveBasicSwing; Entity.MagicSwingPower reads it. ----
        list.Add(new SkillDef(MagicSwing, "Resonant Strikes", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: MagicSwingPower[0],
            Category: SkillCategory.Passive,
            Description: "Passive. Your basic attacks ring with sound instead of steel: they deal MAGIC "
                       + "damage, crit as spells do, and can never be blocked.",
            Levels: MagicSwingPower.Select(p => new SkillLevel(Power: p, SpCost: 36_000,
                Description: $"Your basic attack deals magic damage with power {p}.")).ToArray()));

        // ---- Harmonist Dual Proficiency (Elf) — the same cancellation the bow one did (×2 cast, ×2 M.Atk,
        //      ×0.04 fizzle = the exact inverse of Spellcaster Mastery's untrained-weapon charge), on DUALS. ----
        list.Add(new SkillDef(HarmonistDualProficiency, "Harmonist Dual Proficiency", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Duals are no longer an untrained weapon for you: they cost you no "
                       + "casting speed, no magic attack, and no extra chance for spells to fizzle.",
            Levels: new[] { new SkillLevel(SpCost: 36_000) },
            WeaponMasteryLevels: new[]
            {
                new WeaponMasteryProfile(Dual: new PassiveEffect(
                    CastPenaltyMult: 2f, MagicPenaltyMult: 2f, MagicFailSelfMult: 0.04f)),
            }));

        // ---- Mana Vampirism — 3/6/9% of a landed BASIC attack back as MP, 3 rungs @40/60/70. ⚠ ManaVamp is
        //      its own field, not MeleeVamp. Since `BL-335` the gate is BLUNT OR DUALS (the elf holds fangs)
        //      and all three races reach 9%: the elf stopped at 6% only because of the bow. It is paid in
        //      ResolveBasicSwing, so the magic swing drains and the skills never do. ----
        float[] manaVamp = { 0.03f, 0.06f, 0.09f };
        list.Add(new SkillDef(ManaVampirism, "Mana Vampirism", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Your basic attacks with a blunt weapon or duals drain mana back to you.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 36_000),
                new SkillLevel(SpCost: 120_000),
                new SkillLevel(SpCost: 390_000),
            },
            WeaponMasteryLevels: manaVamp.Select(v => new WeaponMasteryProfile(
                Blunt: new PassiveEffect(ManaVamp: v), Dual: new PassiveEffect(ManaVamp: v))).ToArray()));

        // ---- Combo Mastery — 3 rungs @52/64/74, the on-hit proc. ONE ID PER RACE since `BL-335` (his
        //      answer: *"one chance field, one id per race"*), each gated to that race's weapon: Human 3%
        //      with a one-handed blunt, Demon 3.5% with a two-handed one (the slower swing rolls less often),
        //      Elf 2.6% with duals (the faster swing rolls more). ----
        list.Add(ComboMasteryDef(ComboMastery, WeaponType.Blunt, WeaponHands.One, 0.03f, "a one-handed blunt"));
        list.Add(ComboMasteryDef(ComboMasteryDemon, WeaponType.Blunt, WeaponHands.Two, 0.035f, "a two-handed blunt"));
        list.Add(ComboMasteryDef(ComboMasteryElf, WeaponType.Dual, WeaponHands.Any, 0.026f, "duals"));
        list.AddRange(ComboRushRungs());

        // ===== ACTIVES ===========================================================================

        // ---- Harmony of Restoration — the party heal-over-time, 14 rungs, replacing PARTY HEAL.
        //      +30 to +100 HP/s for 30s, and from rung 9 (@64) it also carries MP/s. ----
        int[] hotHp   = { 30, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100 };
        int[] hotMp   = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 10 };
        // His 2026-10-06 edit: the cost cut to 65 → 280 and the MP/s ramped 1 → 10 from rung 9 (level 64), *"so it start to restore mp if
        // not spammed"* (at 74: 10 MP/s × 30s = 300 back for 280 spent).
        int[] hotCost = { 65, 70, 75, 80, 85, 90, 95, 100, 110, 120, 130, 140, 150, 280 };
        list.Add(new SkillDef(HarmonyOfRestoration, "Harmony of Restoration", BaseClass.Mage,
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

        // ---- THE DAMAGE SKILLS — all single-hit MAGIC since `BL-335` (his point 8). They keep their old
        //      cast and reuse and become SPELLS: paced by cast speed, fizzle-able, interruptible. ----
        //      🔑 SOUND SMASH IS ONE SKILL AGAIN (owner, 2026-10-06: *"The two smashes will be the same skill no
        //      point in two different skills ... Same cd and power"*): Human (wand) and Demon (battlestaff) both
        //      learn `sound_smash`, any blunt, either hand. `sound_smash_demon` is retired.
        //      It retires Vampiric Bolt too: the Human's mage-1st ranged nuke runs to 80 otherwise.
        list.Add(SoundSpell(SoundSmash, "Sound Smash", WeaponType.Blunt, range: 40, castTicks: 10,
            cooldownTicks: 100, stunTicks: 0, HolyRayPower, SoundMeleeMp,
            desc: "A concussive blow of pure sound that rings through armour.",
            alsoReplaces: new[] { HumanVampiricBolt }));
        // Acoustic Shock (DEMON ONLY) — Sound Smash with a contested 5s STUN, now a MAGIC debuff (WIT-side
        // vs SPT). ⚠ A re-landed stun still REFRESHES: the IG no-refresh rule is `BL-336`, deferred by him
        // (2026-10-06: *"i have always played with resetting stuns no difference for now"*).
        list.Add(SoundSpell(AcousticShock, "Acoustic Shock", WeaponType.Blunt, range: 40, castTicks: 10,
            cooldownTicks: 100, stunTicks: 50, HolyRayPower, SoundMeleeMp,
            desc: "A blow pitched to shatter the senses: magic damage, and the target reels.", hands: WeaponHands.Two));
        // Acoustic Bash (HUMAN ONLY, shield) — NO DAMAGE (owner, 2026-10-07: *"no dmg .. Only stun+taunt"*): a
        // contested 5s magic STUN plus AGGRO, half the tank's Taunt at the same level, paid whether or not the
        // stun lands (*"if fail the stun the taunt value is applied anyway"*). Threat only, no target lock:
        // GameLoopService pays a non-taunt skill's TauntPower through the charm's unconditional AddThreat.
        // A threat skill is never auto-cast.
        list.Add(new SkillDef(AcousticBash, "Acoustic Bash", BaseClass.Mage, SkillEffect.Stun,
            MpCost: SoundMeleeMp[0], CastTicks: 10, CooldownTicks: 100, Range: 40, Power: 0,
            DurationTicks: 50, BuffKey: AcousticBash,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Magical,
            RequiredShield: ShieldGate.Required, TauntPower: BashTaunt[0],
            Description: "A ringing blow off the shield: the target reels, and it turns on you. Requires a shield.",
            Levels: Enumerable.Range(0, BashTaunt.Length).Select(i =>
            {
                var (sp, gold) = i < 13 ? (BandSp13[i], 0) : F4(i - 13, 1);
                return new SkillLevel(MpCost: SoundMeleeMp[i], SpCost: sp, GoldCost: gold, TauntPower: BashTaunt[i],
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.Stun, 1f, ModifierMode.Flat) },
                    Description: $"Stuns for 5s and adds {BashTaunt[i]:N0} aggro, even if the stun fails.");
            }).ToArray()));
        // Magic Stab (ELF) — Sound Burst's successor: melee, ONE hit, its old 3s cast / 5s reuse, and a big
        // fizzle of its own (+59 points) that Sharpening's +20 M.Accuracy cuts (his: *"high chance to fail
        // ... the toggle just to give less fail chance"*).
        list.Add(SoundSpell(MagicStab, "Magic Stab", WeaponType.Dual, range: 40, castTicks: 30,
            cooldownTicks: 50, stunTicks: 0, StabElf, MagicStabMp,
            desc: "A thrust of focused sound — devastating when it holds, and it often does not.",
            failPoints: MagicStabFailPoints, alsoReplaces: new[] { FrostSpikes }));

        // ===== TOGGLES ===========================================================================
        // Two per race: Reinforcement (defence) and Sharpening (the race's other half). Instant on and
        // off; each burns MP every second (SkillDef.MpPerSecond, TickToggleUpkeep) and makes every skill
        // 15% dearer. Sharpening is WEAPON-GATED (shield / 2H blunt / duals): Entity.RefreshBuffSuppression
        // switches it dark the moment the weapon no longer fits.

        // HUMAN — Reinforcement: heavy armour's P.Def (+its mastery), Critical Damage Resist's 15%, and
        // Shield Mastery's bow resistance (16% from 60). Sharpening: Shield Mastery's shield numbers and,
        // from 70, its +10% P.Def.
        list.Add(Stance(Reinforcement, "Reinforcement", "reinforcement", WeaponType.None, WeaponHands.Any, false,
            i => new List<EffectMagnitude>
            {
                new(SkillEffect.BuffDef, ReinforceHuman[i], ModifierMode.Percent),
                new(SkillEffect.BuffCritDmgResist, 0.15f, ModifierMode.Percent),
            }.Concat(i >= 5 ? new[] { new EffectMagnitude(SkillEffect.BuffBowResist, 0.16f, ModifierMode.Percent) }
                            : Array.Empty<EffectMagnitude>()).ToArray(),
            i => $"P.Def +{ReinforceHuman[i] * 100:0.#}%, P.Crit Damage Resist 15%"
               + (i >= 5 ? ", Bow Resistance 16%" : ""),
            "Brace yourself: the defence of plate, for as long as you can pay for it."));
        list.Add(Stance(Sharpening, "Sharpening", "sharpening", WeaponType.None, WeaponHands.Any, true,
            i =>
            {
                var (red, rate) = i < 5 ? (0.15f, 0.50f) : i < 10 ? (0.20f, 0.70f) : (0.25f, 0.85f);
                var m = new List<EffectMagnitude>
                {
                    new(SkillEffect.BuffShieldDef, red, ModifierMode.Percent),
                    new(SkillEffect.BuffBlockChance, rate, ModifierMode.Percent),
                };
                if (i >= 10) m.Add(new(SkillEffect.BuffDef, 0.10f, ModifierMode.Percent));
                return m.ToArray();
            },
            i => i < 5 ? "Shield Reduction +15%, Shield Rate +50%"
               : i < 10 ? "Shield Reduction +20%, Shield Rate +70%"
               : "Shield Reduction +25%, Shield Rate +85%, P.Def +10%",
            "Set your shield: it blocks more often and turns more of the blow. Requires a shield."));

        // DEMON — Reinforcement: heavy armour's P.Def, crit rate AND crit damage resistance. Sharpening
        // (two-handed blunt): accuracy.
        list.Add(Stance(ReinforcementDemon, "Reinforcement", "reinforcement_demon", WeaponType.None, WeaponHands.Any, false,
            i => new EffectMagnitude[]
            {
                new(SkillEffect.BuffDef, ReinforceDemon[i], ModifierMode.Percent),
                new(SkillEffect.BuffCritRateResist, 0.08f, ModifierMode.Percent),
                new(SkillEffect.BuffCritDmgResist, 0.08f, ModifierMode.Percent),
            },
            i => $"P.Def +{ReinforceDemon[i] * 100:0.#}%, P.Crit Rate Resist 8%, P.Crit Damage Resist 8%",
            "Brace yourself: the defence of plate, for as long as you can pay for it."));
        list.Add(Stance(SharpeningDemon, "Sharpening", "sharpening_demon", WeaponType.Blunt, WeaponHands.Two, false,
            i => new EffectMagnitude[]
            {
                new(SkillEffect.BuffAccuracy, SharpenDemonAcc[i], ModifierMode.Flat),
            },
            i => $"Accuracy +{SharpenDemonAcc[i]}",
            "Tune the staff: surer blows. Requires a two-handed blunt."));

        // ELF — Reinforcement: Light Armor Mastery's P.Def and Critical Resist's crit-rate resistance.
        // Sharpening (duals): evasion, spell damage and +20 M.Accuracy.
        list.Add(Stance(ReinforcementElf, "Reinforcement", "reinforcement_elf", WeaponType.None, WeaponHands.Any, false,
            i => new EffectMagnitude[]
            {
                new(SkillEffect.BuffDef, ReinforceElf[i], ModifierMode.Percent),
                new(SkillEffect.BuffCritRateResist, 0.15f, ModifierMode.Percent),
            },
            i => $"P.Def +{ReinforceElf[i] * 100:0.##}%, P.Crit Rate Resist 15%",
            "Brace yourself: the defence of leather, for as long as you can pay for it."));
        list.Add(Stance(SharpeningElf, "Sharpening", "sharpening_elf", WeaponType.Dual, WeaponHands.Any, false,
            i => new EffectMagnitude[]
            {
                new(SkillEffect.BuffEvasion, SharpenElfEva[i], ModifierMode.Flat),
                new(SkillEffect.BuffPveMagicDamage, SharpenElfSpell[i], ModifierMode.Percent),
                new(SkillEffect.BuffPvpMagicDamage, SharpenElfSpell[i], ModifierMode.Percent),
            },
            i => $"Evasion +{SharpenElfEva[i]}, PVE/PVP spell power +{SharpenElfSpell[i] * 100:0.##}%, M.Acc +{SharpenElfMAcc:0}",
            "Quicken the fangs: harder to hit, and your spells find their mark. Requires duals.",
            magicAccuracy: SharpenElfMAcc));

        return list.ToArray();
    }

    /// <summary>One race's Combo Mastery: 3 rungs, its own chance and weapon gate, the shared Combo Rush
    /// payload (caster rungs 4-6, party rungs 1-3 — his *"u get 4,5,6 while party gets 1,2,3"*).</summary>
    private static SkillDef ComboMasteryDef(string id, WeaponType weapon, WeaponHands hands, float chance, string with)
    {
        int[] comboSp = { 74_000, 190_000, 880_000 };
        return new SkillDef(id, "Combo Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 600, Range: 0, Power: 0,
            DurationTicks: 300,
            Category: SkillCategory.Passive,
            RequiredWeapon: weapon, RequiredHands: hands,
            ProcChance: chance, ProcCooldownTicks: 600,
            ProcSelfRungs:  new[] { WcComboRush[3], WcComboRush[4], WcComboRush[5] },
            ProcPartyRungs: new[] { WcComboRush[0], WcComboRush[1], WcComboRush[2] },
            Description: $"Passive. Landing a blow with {with} can send a surge through you and your "
                       + "party — faster attacks and faster casting for 30s.",
            Levels: comboSp.Select((sp, i) => new SkillLevel(SpCost: sp,
                Description: $"{chance * 100:0.#}% chance on hit: "
                           + $"+{ComboAs[i + 3] * 100:0.#}% attack and "
                           + $"+{ComboCast[i + 3] * 100:0.#}% cast speed for you, "
                           + $"+{ComboAs[i] * 100:0.#}%/+{ComboCast[i] * 100:0.#}% for the party, 30s."))
                .ToArray());
    }

    // ===== COMBO RUSH — the proc's buff, and the one ladder in the game that is NOT monotonic =====
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
    // 🔑 ONE FAMILY IS THE WHOLE MECHANISM: all six share the key `wc_combo`, so ApplyBuff's same-family
    // rule (higher Rank wins) does everything. ⚠⚠ RUNG 3 -> 4 GOES BACKWARDS ON CAST SPEED ON PURPOSE — he
    // called it: *"even if some other buffer procs lvl 3 buff u still get your effect over (loosing only 2%
    // cast in the process)"*. Do NOT straighten it into a rising line.
    private static readonly float[] ComboAs   = { 0.05f, 0.075f, 0.10f, 0.10f, 0.15f, 0.20f };
    private static readonly float[] ComboCast = { 0.025f, 0.05f, 0.075f, 0.05f, 0.10f, 0.15f };

    /// <summary>The six Combo Rush rungs. HIDDEN — never taught, never on a bar; only a Combo Mastery proc
    /// applies one. Every rung carries the same BuffKey and its index as Rank, so they compete.</summary>
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

    /// <summary>A Warchanter stance: 21 rungs (13 on the 40-74 band, 8 on 76/78…90), its magnitudes per
    /// rung, the shared MP/s upkeep and the +15% skill-MP surcharge on BOTH channels (NEGATIVE = dearer).
    /// The 4th-tier rungs price on the tier's every-other-level ladder (<c>F4(i, 2)</c>).</summary>
    private static SkillDef Stance(string id, string name, string buffKey, WeaponType weapon, WeaponHands hands,
        bool shield, Func<int, EffectMagnitude[]> mags, Func<int, string> text, string desc,
        float magicAccuracy = 0f)
    {
        SkillLevel Rung(int i)
        {
            var (sp, gold) = i < 13 ? (BandSp13[i], 0) : F4(i - 13, 2);
            int mps = StanceMpPerSec[i];
            return new SkillLevel(MpCost: mps, MpPerSecond: mps, SpCost: sp, GoldCost: gold,
                MagicMpCostPct: -StanceMpSurcharge, PhysMpCostPct: -StanceMpSurcharge,
                MagicAccuracy: magicAccuracy,
                Magnitudes: mags(i),
                Description: $"{text(i)}; MP Consumption +15%; drains {mps} MP per second.");
        }
        var first = mags(0);
        var mask = Enumerable.Range(0, StanceMpPerSec.Length).SelectMany(mags)
            .Aggregate(SkillEffect.None, (e, m) => e | m.Effect);
        return new SkillDef(id, name, BaseClass.Mage, mask,
            MpCost: StanceMpPerSec[0], CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 0, BuffKey: buffKey, Rank: 1,
            Category: SkillCategory.Buff, Toggle: true, TargetMode: TargetMode.SelfOnly,
            MpPerSecond: StanceMpPerSec[0],
            RequiredWeapon: weapon, RequiredHands: hands, RequiredShield: shield ? ShieldGate.Required : ShieldGate.Any,
            MagicMpCostPct: -StanceMpSurcharge, PhysMpCostPct: -StanceMpSurcharge,
            BuffMagicAccuracy: magicAccuracy,
            Magnitudes: first,
            Description: "Toggle. " + desc,
            Levels: Enumerable.Range(0, StanceMpPerSec.Length).Select(Rung).ToArray());
    }

    /// <summary>One of the Warchanter's MAGIC damage spells: one hit, 28 rungs (13 on the 40-74 band, 15 on
    /// 76-90), weapon-gated, and (Acoustic Shock) a contested magic stun. Every one retires Holy Bolt and
    /// Holy Spike (playtest 28: *"holy bolt should be replaced from sound smash/burst"*). The Human Smash also
    /// retires Vampiric Bolt and the Elf Stab Frost Spikes — the mage-1st ranged nukes a buffer must not keep
    /// (2026-10-06: *"Human and elf have ranged spell that they must not have"*).</summary>
    private static SkillDef SoundSpell(string id, string name, WeaponType weapon, float range, int castTicks,
        int cooldownTicks, int stunTicks, int[] power, int[] mp, string desc, float failPoints = 0f,
        WeaponHands hands = WeaponHands.Any, string[]? alsoReplaces = null)
    {
        var effect = SkillEffect.MagicDamage | (stunTicks > 0 ? SkillEffect.Stun : SkillEffect.None);
        SkillLevel Rung(int i)
        {
            var (sp, gold) = i < 13 ? (BandSp13[i], 0) : F4(i - 13, 1);
            return new SkillLevel(Power: power[i], MpCost: mp[i], SpCost: sp, GoldCost: gold,
                Magnitudes: stunTicks > 0
                    ? new EffectMagnitude[] { new(SkillEffect.Stun, 1f, ModifierMode.Flat) }
                    : null,
                Description: stunTicks > 0
                    ? $"Magic damage, power {power[i]}, and stuns for {stunTicks / 10f:0.#}s."
                    : $"Magic damage, power {power[i]}.");
        }
        return new SkillDef(id, name, BaseClass.Mage, effect,
            MpCost: mp[0], CastTicks: castTicks, CooldownTicks: cooldownTicks, Range: range, Power: power[0],
            Category: SkillCategory.Magic, BuffKey: id,
            Replaces: new[] { HolyBolt, HolySpike }.Concat(alsoReplaces ?? Array.Empty<string>()).ToArray(),
            RequiredWeapon: weapon, RequiredHands: hands,
            PvpDamageMult: WarchanterPvp,
            DurationTicks: stunTicks,
            DebuffSchool: stunTicks > 0 ? DebuffSchool.Magical : DebuffSchool.None,
            MagicFailPoints: failPoints,
            Description: desc,
            Levels: Enumerable.Range(0, power.Length).Select(Rung).ToArray());
    }
}
