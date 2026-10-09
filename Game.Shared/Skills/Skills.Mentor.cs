namespace Game.Shared;

/// <summary>`BL-339` — THE ELEVEN MENTOR BLESSINGS: what a mentee gets while the bond holds (his spec, option 1 of
/// `docs/design/Mentoring.md`). Each is a SELF-ONLY, 1-hour copy of a real buffer's buff, granted free for as long as
/// the bond lasts (GameLoopService.SyncMentorBlessings) and castable only while the mentor is online
/// (<c>Entity.MentorGuidance</c>, the same flag that draws Mentor's Guidance).
///
/// <para>🔑 <b>EACH ONE LANDS IN ITS ORIGINAL'S FAMILY</b> — same <c>BuffKey</c>, same children, same rank at the
/// copied rung. That is the whole design: Mentor Precision must not stack on a real Warchanter's Precision, and with
/// the family shared the stacking rules that already exist decide it (the stronger rung wins, equal rank = last cast
/// wins). So a real buffer at a HIGHER rung still outranks the blessing, which is what keeps one worth having.</para>
///
/// <para>The rungs are his: a `wc_*` group at the rung learned at level ≤ 74 (every one of the six is rung 1), War
/// Frenzy as authored, and the four harmonies at Protection L4 / Speed L1 / Warrior L5 / Wizard L5.</para>
///
/// <para>⚠ <b>A harmony copy CARRIES ITS RUNG'S RANK.</b> A harmony's rank is <c>Rank + level − 1</c> (BuffPlan), and
/// the copy is a one-rung skill, so without the authored <see cref="SkillLevel.Rank"/> it would land at rank 1 of its
/// ladder: a "Lv.5" blessing that any real Lv.2 evicts, and that itself evicts nothing.</para>
///
/// <para>⚠ <b><c>Replaces</c> IS CLEARED.</b> On a class skill it retires the singles a group contains from the learn
/// list (<c>IsSuperseded</c>); on a granted blessing it would hide the mentee's own Focus and Might the moment the bond
/// formed. Covering still evicts the singles on landing, which is the part that matters.</para>
///
/// <para>MY CALLS, not his: free to cast (0 MP — a level-5 mentee could not pay a 464-MP harmony), 1s cast, 1s reuse.
/// No SP: they are granted, never bought.</para></summary>
public static partial class SkillCatalog
{
    /// <summary>One hour, his number.</summary>
    public const int MentorBlessingTicks = 36000;

    /// <summary>(original, blessing id, rung). Append-only ids, like every skill id.</summary>
    private static readonly (string Source, string Id, int Rung)[] MentorBlessingTable =
    {
        (WarFrenzy,            "mentor_war_frenzy",          1),
        (WcFeralPrecision,     "mentor_feral_precision",     1),
        (WcFeralBloodlust,     "mentor_feral_bloodlust",     1),
        (WcArcaneInsight,      "mentor_arcane_insight",      1),
        (WcArcaneSerenity,     "mentor_arcane_serenity",     1),
        (WcBodyReinforce,      "mentor_body_reinforcement",  1),
        (WcWindGrace,          "mentor_wind_grace",          1),
        (NpcHarmonyProtection, "mentor_harmony_protection",  4),
        (HarmonyOfSpeed,       "mentor_harmony_speed",       1),
        (NpcHarmonyWarrior,    "mentor_harmony_warrior",     5),
        (NpcHarmonyWizard,     "mentor_harmony_wizard",      5),
    };

    /// <summary>Every blessing id, in table order — what the bond grants and takes back.</summary>
    public static readonly string[] MentorBlessingIds = MentorBlessingTable.Select(t => t.Id).ToArray();

    private static readonly HashSet<string> MentorBlessingSet = new(MentorBlessingIds, StringComparer.Ordinal);

    public static bool IsMentorBlessing(string id) => MentorBlessingSet.Contains(id);

    /// <summary>Built from the ORIGINALS already in the list, so a retune of a group or a harmony rung reaches its
    /// blessing with nothing to copy by hand.</summary>
    private static IEnumerable<SkillDef> MentorBlessingSkills(List<SkillDef> built)
    {
        var byId = built.ToDictionary(d => d.Id);
        foreach (var (source, id, rung) in MentorBlessingTable)
            yield return MentorBlessing(byId[source], id, rung);
    }

    private static SkillDef MentorBlessing(SkillDef src, string id, int rung)
    {
        string name = "Mentor Blessing: " + src.Name;
        // The payload sentence, without the original's "who it lands on / how long" tail.
        string payload = (src.Levels is { Length: > 0 } ? src.Levels[rung - 1].Description : null) ?? src.Description;
        payload = payload.Replace(" Blesses you and nearby allies for 20 minutes.", "")
                         .Replace(" (5 minutes).", "")
                         .Replace(" for you and nearby allies", "")
                         .Replace("Drives you and nearby allies to fight recklessly: ", "")
                         .Replace(" for 20 minutes", "")
                         .TrimEnd('.');
        string desc = $"Mentor is watching over you: {payload}. Lasts 1 hour; cast only while your mentor is online.";

        SkillLevel[]? levels = null;
        if (src.Levels is { Length: > 0 })
        {
            var lv = src.Levels[rung - 1];
            int authored = lv.Rank;
            bool childless = src.ChildBuffsAt(rung) is not { Length: > 0 };
            int rank = authored != 0 ? authored
                     : childless && !src.FlatRank && src.Levels.Length > 1 ? src.Rank + rung - 1
                     : 0;
            levels = new[] { lv with { MpCost = 0, SpCost = 0, Rank = rank, DurationTicks = MentorBlessingTicks, Description = desc } };
        }

        return src with
        {
            Id = id,
            Name = name,
            MpCost = 0,
            SpCost = 0,
            CastTicks = 10,
            CooldownTicks = 10,
            DurationTicks = MentorBlessingTicks,
            TargetMode = TargetMode.SelfOnly,
            AreaRadius = 0f,
            Replaces = null,
            Levels = levels,
            Description = desc,
        };
    }
}
