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

/// <summary>A boss's unique kit + phase script, keyed by MOB TEMPLATE id. Applies only when that
/// template is spawned at Boss rank; a template with no profile falls back to the generic
/// Devastating Slam. This is the seam for per-mob boss identity (unique skills, adds, phases).</summary>
public record BossProfile(
    BossSkillEntry[] Skills,
    BossPhase[] Phases);

/// <summary>THE place to author per-boss mechanics. Add an entry keyed by the boss's mob-template
/// id; leave a boss out to keep the plain slam. Numbers/skills retune-later.</summary>
public static class BossCatalog
{
    private static readonly Dictionary<string, BossProfile> All = Build();

    public static BossProfile? Get(string mobTypeId) =>
        All.TryGetValue(mobTypeId, out var p) ? p : null;

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
            }),

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
            Phases: System.Array.Empty<BossPhase>()),

        ["dread_knight"] = new BossProfile(
            Skills: new[]
            {
                new BossSkillEntry(SkillCatalog.BossFullSilenceSkill),
                new BossSkillEntry(SkillCatalog.BossSlamSkill),
            },
            Phases: System.Array.Empty<BossPhase>()),

        ["disciple_of_the_dawn"] = new BossProfile(
            Skills: new[]
            {
                new BossSkillEntry(SkillCatalog.BossFullSilenceSkill),
                new BossSkillEntry(SkillCatalog.BossSlamSkill),
            },
            Phases: System.Array.Empty<BossPhase>()),
    };
}
