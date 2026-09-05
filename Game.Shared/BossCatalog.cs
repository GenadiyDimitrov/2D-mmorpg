namespace Game.Shared;

/// <summary>One skill in a boss's rotation. BossTick casts the first READY entry each opening:
/// off cooldown (its own <see cref="SkillDef.CooldownTicks"/>), the boss's HP fraction within
/// [<see cref="MinHpFraction"/>, <see cref="MaxHpFraction"/>], and a foe inside the skill's radius.
/// Order matters — earlier entries win ties, so put "phase" skills (with a tighter HP window) first.</summary>
public record BossSkillEntry(
    string SkillId,
    float MinHpFraction = 0f,
    float MaxHpFraction = 1f);

/// <summary>A one-time event fired when the boss's HP FIRST drops to/below <see cref="HpFraction"/>:
/// a shout, an optional enrage (the same one-time rage buff the timer grants), and/or a wave of adds
/// (spawned near the boss, already engaged on its target). Phases are listed high-HP → low-HP.</summary>
public record BossPhase(
    float HpFraction,
    string Announce,
    bool Enrage = false,
    string? AddTemplateId = null,
    int AddCount = 0,
    int AddLevelOffset = 0);

/// <summary>A boss's unique kit + phase script + STAT BLOCK, keyed by MOB TEMPLATE id. Applies only
/// when that template is spawned at Boss rank; a template with no profile falls back to the generic
/// Devastating Slam and to the plain rank numbers. This is the seam for per-mob boss identity.
///
/// <para>🔑 `BL-166` (owner, 2026-09-05): *"the bosses don't fallow the curve persay but have different
/// edits per boss. Some have fighters and given decrease in stats the others are solo and given
/// increase. The curve is the base and every boss edit is making the boss unique — are bosses separate
/// from the mobs file? they need their own to be edited/added - stat and skills as well."* The SKILLS
/// half of that has lived here since bosses were built; the STAT half did not exist at all — a boss's
/// numbers were the creature curve times <see cref="MobRankScale"/>, which is ONE set of numbers every
/// boss in the game shares. The fields below are that missing half: the rank is now the DEFAULT a
/// profile overrides, which is his "the curve is the base" said in code.</para></summary>
/// <param name="Solo">No escort — this boss fights alone, and takes his `solo boss` ×2 on BOTH attack
/// and HP (<see cref="MobRankScale"/>). His rule: *"some Bosses have fighters around them 2-5 which
/// have more p atk than bosses … not all but that who doesn't can have additional passive skill 'solo
/// boss'"*. ⚠ A boss that CALLS adds in a phase still counts as escorted — the adds are its escort.</param>
/// <param name="HpMult">Per-boss multiplier on top of the rank's HP, for the "3kk mage boss with
/// fighters vs 6kk solo bruiser" shapes. 1 = take the rank's number.</param>
/// <param name="PAtkMult">Per-boss P.Atk lean. 1 = the rank's number. Below 1 is legitimate and is half
/// his design: an escorted CASTER boss is *"a mage with less patk more m atk less Def"*.</param>
/// <param name="MAtkMult">Per-boss M.Atk lean. 1 = the rank's number.</param>
/// <param name="PDefMult">Per-boss P.Def lean. 1 = the rank's number.</param>
/// <param name="MDefMult">Per-boss M.Def lean. 1 = the rank's number.</param>
/// <param name="EnrageSeconds">Seconds of ENGAGED combat before the first enrage (×2 attack), and
/// before the second (×4 total). His ladder, 2026-09-05: *"if battle becomes longer than 20 min he
/// gets a buff that additionally doubles p/m atk and after 40 mins it's gives another x2"*, with the
/// second rung moved to 30 minutes the same day. World bosses run 2h/3h — *"only field/dungeon bosses,
/// world once the it times will be 2h and 3h"*.</param>
public record BossProfile(
    BossSkillEntry[] Skills,
    BossPhase[] Phases,
    bool Solo = false,
    float HpMult = 1f,
    float PAtkMult = 1f, float MAtkMult = 1f,
    float PDefMult = 1f, float MDefMult = 1f,
    int EnrageSeconds = BossCatalog.FieldEnrage1Seconds,
    int EnrageSeconds2 = BossCatalog.FieldEnrage2Seconds);

/// <summary>THE place to author per-boss mechanics. Add an entry keyed by the boss's mob-template
/// id; leave a boss out to keep the plain slam. Numbers/skills retune-later.</summary>
public static class BossCatalog
{
    /// <summary>His enrage ladder for a FIELD or DUNGEON boss — ×2 attack at 20 minutes of engaged
    /// combat, ×4 at 30. ⚠ The second rung was 40 minutes when he first drew it and he moved it to 30
    /// the same day (*"enrage x2 after 20 and x4 after 30m"*); 30 is the ruling.</summary>
    public const int FieldEnrage1Seconds = 20 * 60;
    public const int FieldEnrage2Seconds = 30 * 60;
    /// <summary>And for a WORLD boss, whose fight is meant to be a different order of length:
    /// *"world once the it times will be 2h and 3h"*.</summary>
    public const int WorldEnrage1Seconds = 2 * 3600;
    public const int WorldEnrage2Seconds = 3 * 3600;

    private static readonly Dictionary<string, BossProfile> All = Build();

    public static BossProfile? Get(string mobTypeId) =>
        All.TryGetValue(mobTypeId, out var p) ? p : null;

    /// <summary>Does this template fight ALONE? Drives the `solo boss` ×2 on attack AND HP
    /// (<see cref="MobRankScale"/>). ⚠ A template with NO profile is treated as ESCORTED — the
    /// conservative default, since the extra ×2 is an opt-in identity a boss is authored with, and a
    /// boss nobody has looked at yet should not silently be the hardest kind in the game.</summary>
    public static bool IsSolo(string? mobTypeId) =>
        mobTypeId is not null && All.TryGetValue(mobTypeId, out var p) && p.Solo;

    private static Dictionary<string, BossProfile> Build() => new()
    {
        // Demo: the Valley Treant Lord (valley_treant, the L60 boss zone). Slams from the start;
        // below 50% HP it enrages and calls two bogwood adds, and unlocks Thorn Nova (a wider magic
        // burst + slow); below 25% it lets out a final thorn storm shout.
        ["valley_treant"] = new BossProfile(
            Skills: new[]
            {
                // Phase skill first so it takes priority once the boss is wounded.
                new BossSkillEntry(SkillCatalog.BossThornNovaSkill, MaxHpFraction: 0.50f),
                new BossSkillEntry(SkillCatalog.BossSlamSkill),
            },
            Phases: new[]
            {
                new BossPhase(0.50f, "The Valley Treant Lord roars — the bog stirs to its defence!",
                    Enrage: true, AddTemplateId: "bogwood", AddCount: 2, AddLevelOffset: -8),
                new BossPhase(0.25f, "The Valley Treant Lord unleashes a storm of thorns!"),
            },
            // ESCORTED, and it is the clearest case in the game: it calls two bogwood adds at 50%, so
            // the escort is written into its own phase script. No `solo boss` ×2.
            // ⚠ It is also the WORLD boss (WorldMap, 21-hour respawn), so its enrage runs on the 2h/3h
            // ladder rather than the field one — his *"world once the it times will be 2h and 3h"*.
            EnrageSeconds:  WorldEnrage1Seconds,
            EnrageSeconds2: WorldEnrage2Seconds),

        // ═══ THE THREE DUNGEON BOSSES — `BL-155`'s full silence ══════════════════════════════════
        //
        // His ask, 2026-09-03: *"U can add dungeon bosses a full silence aoe skill for 15s duration
        // and 45s cd"*. The three bosses at the end of a dungeon corridor are named in
        // DungeonLayout.Dungeons — the Hollow Crypt's lich (44), the Sunless Warrens' knight (65) and
        // the Ashen Sepulchre's disciple (90). They are the only bosses in the game that are reached
        // through a door, which is what makes them "dungeon bosses".
        //
        // ⚠ THE SLAM HAS TO BE LISTED. A template with NO profile falls back to the generic
        // Devastating Slam; the moment it has one, the profile is the whole rotation. Leaving the slam
        // out would have traded each boss's only attack for a silence — a fight where nothing ever
        // hits you and you can never cast.
        //
        // ⚠ SILENCE FIRST, and that is what the ordering means here (earlier entries win ties): it is
        // the mechanic, the slam is the filler. Its own 45s cooldown is what stops it dominating.
        ["grave_lich"] = new BossProfile(
            Skills: new[]
            {
                new BossSkillEntry(SkillCatalog.BossFullSilenceSkill),
                new BossSkillEntry(SkillCatalog.BossSlamSkill),
            },
            Phases: System.Array.Empty<BossPhase>(),
            // SOLO — a dungeon boss stands alone at the end of its corridor, with the last mob group
            // in the room BEFORE it rather than beside it. So it takes his `solo boss` x2 on attack
            // AND HP: x4 attack, x20 HP, "nearly impossible" alone, which is what he asked for.
            Solo: true),

        ["dread_knight"] = new BossProfile(
            Skills: new[]
            {
                new BossSkillEntry(SkillCatalog.BossFullSilenceSkill),
                new BossSkillEntry(SkillCatalog.BossSlamSkill),
            },
            Phases: System.Array.Empty<BossPhase>(),
            // SOLO — a dungeon boss stands alone at the end of its corridor, with the last mob group
            // in the room BEFORE it rather than beside it. So it takes his `solo boss` x2 on attack
            // AND HP: x4 attack, x20 HP, "nearly impossible" alone, which is what he asked for.
            Solo: true),

        ["disciple_of_the_dawn"] = new BossProfile(
            Skills: new[]
            {
                new BossSkillEntry(SkillCatalog.BossFullSilenceSkill),
                new BossSkillEntry(SkillCatalog.BossSlamSkill),
            },
            Phases: System.Array.Empty<BossPhase>(),
            // SOLO — a dungeon boss stands alone at the end of its corridor, with the last mob group
            // in the room BEFORE it rather than beside it. So it takes his `solo boss` x2 on attack
            // AND HP: x4 attack, x20 HP, "nearly impossible" alone, which is what he asked for.
            Solo: true),
    };
}
