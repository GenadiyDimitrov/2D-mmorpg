namespace Game.Shared;

/// <summary>`BL-339` — THE MENTOR BLESSINGS: the spirit helper's shelf, cast on yourself for free while your mentor is
/// online. One blessing per SINGLE on the shelf (the nineteen blessings + the eight single harmonies); the three Marks
/// are left out (his: *"marks are 78 gated so no mentor buff for them"* — a mentee is below 76).
///
/// <para>🔑 <b>HIS SECOND PASS (2026-10-09) REPLACED THE ELEVEN GROUP COPIES</b>: *"lets make the buffs singles not the
/// grouped ones -&gt; if having an online mentor u can rebuff yourself like you are buffing from the npc buffer -&gt;
/// meaning you get the same effects as the npc for your lvl get you"*. A level-1 character with a mentor was wearing a
/// Warchanter's whole kit. Now each blessing is LEVEL-GATED EXACTLY AS THE SHELF IS: it is KNOWN only once the shelf
/// would sell it to you (<c>NpcBuffShelf.TierFor &gt; 0</c>, GameLoopService.SyncMentorBlessings), and a cast lands
/// the RUNG the shelf would hand you at your level — Frenzy L1 at 40, L2 at 52.</para>
///
/// <para>🔑 <b>THE DEF IS A SHELL.</b> It carries no payload: the shelf is a server file the client never reads, so the
/// rung is resolved on the server at the moment the cast lands (GameLoopService.LandMentorBlessing) through the same
/// <c>SkillCatalog.NpcBuffRung</c> the NPC grant uses. One resolution, two doors — the mentor's blessing and the NPC's
/// can never disagree about what your level buys, and they share a family, so one replaces the other.</para>
///
/// <para>MY CALLS, not his: 1 hour (the buffer's hour), free, 1s cast, 1s reuse. No SP: granted, never bought.</para></summary>
public static partial class SkillCatalog
{
    /// <summary>One hour, his number.</summary>
    public const int MentorBlessingTicks = 36000;

    /// <summary>(shelf blessing, mentor id, the name after "Mentor Blessing: "). Append-only ids, like every skill id.
    /// The SHELF id, not a rung: the rung follows the mentee's level.</summary>
    private static readonly (string Shelf, string Id, string Name)[] MentorBlessingTable =
    {
        (NpcMight,      "mentor_might",      "Might"),
        (NpcBulwark,    "mentor_bulwark",    "Bulwark"),
        (NpcVampirism,  "mentor_vampirism",  "Vampirism"),
        (NpcForce,      "mentor_force",      "Force"),
        (NpcWard,       "mentor_ward",       "Ward"),
        (NpcResolve,    "mentor_resolve",    "Resolve"),
        (NpcBody,       "mentor_body",       "Body"),
        (NpcVigor,      "mentor_vigor",      "Vigor"),
        (NpcSoul,       "mentor_soul",       "Soul"),
        (NpcSerenity,   "mentor_serenity",   "Serenity"),
        (NpcAlacrity,   "mentor_alacrity",   "Alacrity"),
        (NpcHaste,      "mentor_haste",      "Fury"),
        (NpcSwift,      "mentor_swift",      "Swift"),
        (NpcAgility,    "mentor_agility",    "Agility"),
        (NpcAccuracy,   "mentor_accuracy",   "Aim"),
        (NpcFrenzy,     "mentor_frenzy",     "Frenzy"),
        (NpcFocus,      "mentor_focus",      "Focus"),
        (NpcFerocity,   "mentor_ferocity",   "Ferocity"),
        (NpcInsight,    "mentor_insight",    "Insight"),
        (NpcHWard,      "mentor_harmony_ward",     "Harmony of Ward"),
        (NpcHForce,     "mentor_harmony_force",    "Harmony of Force"),
        (NpcHSwift,     "mentor_harmony_swift",    "Harmony of Swift"),
        (NpcHAlacrity,  "mentor_harmony_alacrity", "Harmony of Alacrity"),
        (NpcHBulwark,   "mentor_harmony_bulwark",  "Harmony of Bulwark"),
        (NpcHMight,     "mentor_harmony_might",    "Harmony of the Might"),
        (NpcHFury,      "mentor_harmony_fury",     "Harmony of the Fury"),
        (NpcHBody,      "mentor_harmony_body",     "Harmony of Body"),
    };

    /// <summary>Every blessing id, in shelf order — what the bond can grant and takes back.</summary>
    public static readonly string[] MentorBlessingIds = MentorBlessingTable.Select(t => t.Id).ToArray();

    private static readonly Dictionary<string, string> MentorBlessingShelfOf =
        MentorBlessingTable.ToDictionary(t => t.Id, t => t.Shelf, StringComparer.Ordinal);

    public static bool IsMentorBlessing(string id) => MentorBlessingShelfOf.ContainsKey(id);

    /// <summary>The shelf blessing a Mentor Blessing casts, or null for any other skill.</summary>
    public static string? MentorBlessingShelf(string id) =>
        MentorBlessingShelfOf.TryGetValue(id, out var shelf) ? shelf : null;

    private static IEnumerable<SkillDef> MentorBlessingSkills()
    {
        foreach (var (_, id, name) in MentorBlessingTable)
            yield return new SkillDef(id, "Mentor Blessing: " + name, BaseClass.Mage, SkillEffect.None,
                0, 10, 10, 0f, 0,
                DurationTicks: MentorBlessingTicks,
                Description: $"Your mentor watches over you: you bless yourself with {name} exactly as the spirit helper "
                           + "would sell it at your level, and it grows as you do. Lasts 1 hour; cast only while your "
                           + "mentor is online.",
                Category: SkillCategory.Buff,
                TargetMode: TargetMode.SelfOnly);
    }
}
