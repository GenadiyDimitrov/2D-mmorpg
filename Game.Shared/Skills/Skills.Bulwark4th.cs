namespace Game.Shared;

// ===========================================================================
//  THE BULWARK (tank), 76-90 — the FIRST three rows of `tank 4th.csv`.
//
//  ⚠⚠ HIS FILE IS STILL A PLACEHOLDER, and these three skills are the deliberate exception to the
//  40+ rule (*"Anything that's not inside the csv should not exist"*). He asked for them by name on
//  2026-09-03, and for exactly this reason: *"put pull for the three tanks 4th, one m.silence skill
//  for elf tank 4th and one p.silence for human/demon tank 4th in the csv so when I author it to
//  remember to fix ranges/duration etc"*.
//
//  🔑 SO EVERY NUMBER BELOW IS A PLACEHOLDER EXCEPT THE ONES HE RULED. What is HIS:
//      · the drag is 1-1.5s and the stun tail 1-2s        (`BL-154`)
//      · one CON contest covers the pull AND its stun     (`BL-154`)
//      · the drag stops at melee range                    (`BL-154`)
//      · threat above a damage skill, below the real taunt (`BL-154`)
//      · a physical silence leaves the BASIC ATTACK alone (`BL-155`)
//      · Human + Demon get the physical silence, Elf the magical one (`BL-155`)
//  Everything else — learn level, range, cast, cooldown, MP, SP, the silence durations — is a
//  placeholder chosen to be sane, written into the CSV row in the same commit, and his to overwrite.
//
//  ⚠ ONE RUNG EACH, on purpose. A ladder invented under a placeholder is fifteen numbers to unpick
//  instead of one, and his 4th-tier ladders are authored per file (the Lightbringer's fifteen bands,
//  the Warchanter's).
//
//  🔵 THE TWO AoE PULLS ARE NOT HERE. `BL-154` rule 4 gives the pull two more shapes — a ranged one
//  that takes the target plus 2-4 around IT, and a self-centred one taking 2-5 around the CASTER —
//  and the ENGINE serves both already (`TargetMode.EnemiesInRadius` + `AreaAtTarget` for the centre,
//  `MaxTargets` for his cap of five, and DeliverSimpleHit's pull arm). They are absent because he
//  named one pull, not three, and a skill nobody asked for is a skill nobody can retune.
// ===========================================================================

public partial class SkillCatalog
{
    public const string TankPull            = "tank_pull";
    public const string TankSilencePhysical = "tank_silence_physical";
    public const string TankSilenceMagical  = "tank_silence_magical";

    private static SkillDef[] Bulwark4thSkills() => new SkillDef[]
    {
        // ═══ GRAPPLE — the pull (`BL-154`) ═══════════════════════════════════════════════════════
        //
        // 🔑 IT IS AN ORDINARY CONTESTED PHYSICAL DEBUFF whose payload happens to be movement: ATK vs
        // CON, the same contest Stay and Shield Shock run on, at the 0.4-0.5 he expected (parity is
        // 0.5 exactly). `Pulls` is the drag; the Stun effect below is the TAIL, and StartPull holds it
        // back until the body arrives so the two windows run in sequence — 1.2s of travel, then 1s of
        // stun — instead of overlapping into one.
        //
        // ⚠ TauntPower 3000 is the *"lower power than the actual taunt skill but still higher than
        // most dmg onse"* he asked for: the Bulwark's own Taunt ladder runs 4,500 → 12,000, so this
        // sits under its first rung and well over anything a damage skill puts on the table.
        //
        // ⚠ RANGE IS FREE TO GROW. The drag is TIMED, so 600 and 900 both take PullSeconds and the
        // lockdown does not scale with reach — see the note on SkillDef.Pulls.
        new(TankPull, "Grapple", BaseClass.Fighter, SkillEffect.Stun,
            MpCost: 80, CastTicks: 5, CooldownTicks: 150, Range: 600, Power: 0,
            DurationTicks: 10,                       // the STUN tail: his 1-2s, placed at 1s
            Pulls: true, PullSeconds: 1.2f,          // his 1-1.5s drag
            TauntPower: 3000,
            DebuffSchool: DebuffSchool.Physical, Category: SkillCategory.Debuff,
            BuffKey: "tank_pull_stun", Rank: 1,
            SpCost: 100_000,
            Description: "Hauls an enemy across the ground to your side and leaves it reeling."),

        // ═══ NUMBING STRIKE — the PHYSICAL silence (`BL-155`), Human + Demon ══════════════════════
        //
        // 🔑 IT DOES NOT STOP A BASIC ATTACK, and that is his boundary in as many words:
        // *"physical skill silence (only basic attack)"*. What it stops is every skill
        // SkillMath.IsPhysical is true for — which is the SAME test the cast-speed model uses, never a
        // second classification (see the note on that method).
        //
        // ⚠ CONTESTED ON CON, which follows his own `BL-133` split: the human and demon tank's tools
        // are the physical ones (Stay, Shield Shock), the elf's are the magical ones (Charm, Freeze).
        // The pair below is the same divide applied to the new mechanic.
        new(TankSilencePhysical, "Numbing Strike", BaseClass.Fighter, SkillEffect.None,
            MpCost: 70, CastTicks: 5, CooldownTicks: 300, Range: 400, Power: 0,
            DurationTicks: 80,
            SilencePhysical: true,
            DebuffSchool: DebuffSchool.Physical, Category: SkillCategory.Debuff,
            BuffKey: "silence_physical", Rank: 1,
            SpCost: 100_000,
            Description: "A blow to the nerve: the target's body will not perform a skill, though it "
                       + "can still swing."),

        // ═══ SILENCING WARD — the MAGICAL silence (`BL-155`), Elf ════════════════════════════════
        //
        // The elf tank's half. Landing this AND the strike above on one target is a FULL silence —
        // his *"both at once a full silence"* — and it needs no third skill to express: two fields,
        // two debuffs, and the cast gate asks the two questions separately.
        new(TankSilenceMagical, "Silencing Ward", BaseClass.Fighter, SkillEffect.None,
            MpCost: 70, CastTicks: 15, CooldownTicks: 300, Range: 600, Power: 0,
            DurationTicks: 80,
            SilenceMagical: true,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff,
            BuffKey: "silence_magical", Rank: 1,
            SpCost: 100_000,
            Description: "Smothers the words of a spell before they are spoken."),
    };
}
