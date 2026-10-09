using System;

namespace Game.Shared;

/// <summary>
/// `BL-339` — THE MENTOR SYSTEM's numbers, in one place. The design and his three answer passes are
/// `docs/design/Mentoring.md`; every number here is his unless the comment says it is mine.
///
/// <list type="bullet">
/// <item>A MENTOR is a character whose MAIN class is level 76+ with a 4th class. Up to 10 mentees, and
/// never two mentees of the same account.</item>
/// <item>A MENTEE is a character below 76 with no mentor. One mentor, never one of their own account.</item>
/// <item>The bond pays at the mentee's level 20 / 40 / 76 (Bond Certificates to both, and the mentor's
/// one-off Graduation Certificates at 76), and ends at 76.</item>
/// <item>While bonded, an online mentor gives the mentee +5..50% exp/SP by level and lets them cast the Mentor
/// Blessings, the shelf singles their level buys (Skills.Mentor.cs). ONLINE mentees (AFK too) give the mentor a 10-rung exp/SP aura weighted by their
/// level; ACTIVE ones give a 5-rung drop/gold Knowledge, one rung each.</item>
/// </list>
/// </summary>
public static class Mentoring
{
    /// <summary>The level a mentor's MAIN class must reach — with a 4th class, which cannot be taken below it.</summary>
    public const int MentorLevel = 76;
    /// <summary>A mentee is below this. Reaching it is the graduation.</summary>
    public const int GraduationLevel = 76;
    public const int MaxMentees = 10;

    /// <summary>The three milestones, the Bond Certificates each pays the MENTEE and the MENTOR.</summary>
    public static readonly int[] MilestoneLevels = { 20, 40, 76 };
    public static readonly int[] MenteeBondPay = { 150, 300, 550 };
    public static readonly int[] MentorBondPay = { 15, 30, 55 };

    /// <summary>His third pass: the mentor's Graduation Certificates, paid ONCE at 76 — 10 minus every
    /// milestone this mentor missed. 6 always, +1 if he held the bond at 20, +3 if he held it at 40.</summary>
    public const int GraduationBase = 6, GraduationAt20 = 1, GraduationAt40 = 3;

    public static int GraduationCertificates(bool heldAt20, bool heldAt40) =>
        GraduationBase + (heldAt20 ? GraduationAt20 : 0) + (heldAt40 ? GraduationAt40 : 0);

    // ----- The two auras -----

    /// <summary>MENTOR'S GUIDANCE — the mentee's exp AND SP bonus while the mentor is online, in five level rungs. His
    /// second pass (2026-10-09): *"the Mentor guidence buff exp/sp is also to op 50% at the begining ... so we will make
    /// it 5 rungs -&gt; @1~19 5%, @20~39 10%, @40~51 20%, @52~60 35%, @61~75 50%"*. (MinLevel, bonus), lowest first.</summary>
    public static readonly (int MinLevel, float Bonus)[] GuidanceRungs =
        { (1, 0.05f), (20, 0.10f), (40, 0.20f), (52, 0.35f), (61, 0.50f) };

    /// <summary>The 1-based Guidance rung at this level.</summary>
    public static int GuidanceRung(int level)
    {
        int rung = 1;
        for (int i = 0; i < GuidanceRungs.Length; i++)
            if (level >= GuidanceRungs[i].MinLevel) rung = i + 1;
        return rung;
    }

    public static float MenteeExpSpBonus(int level) => GuidanceRungs[GuidanceRung(level) - 1].Bonus;

    // 🔑 HIS FOURTH PASS (2026-10-09) SPLIT THE MENTOR'S BUFF IN TWO: *"Mentor aura is only from online mentees (afk
    //    also count) and increases Exp/SP; Mentor Knowledge ins for your 10min active mentees and increases gold/drop
    //    … the formula is only for mentors aura and it dont have the 10 min exp gain penalty"*.

    /// <summary>MENTOR AURA — exp/SP only. 10 rungs, +10% each (L10 = +100%), from every ONLINE mentee, AFK
    /// included (*"an afk player in town to count as online … afk for 10h is still online"*), weighted by level.</summary>
    public const int MaxAuraRung = 10;
    public const float AuraExpSpPerRung = 0.10f;

    /// <summary>A mentee's weight toward the aura: level² / 1800 (mine, fitted to his three targets —
    /// 10 at lvl 20 → L2, 10 at lvl 40 → L8, 5 at lvl 60 → L10). The rung is the floor of the sum.</summary>
    public static float MenteeWeight(int level) => level * level / 1800f;

    public static int AuraRung(float weightSum) =>
        Math.Clamp((int)MathF.Floor(weightSum + 1e-4f), 0, MaxAuraRung);

    /// <summary>MENTOR KNOWLEDGE — drop chance and gold only. One rung per ACTIVE mentee whatever their level (his:
    /// *"5 active mentees get u L5 10% gold amount and drop rates despite mentees lvl"*), cap 5, +2% each.</summary>
    public const int MaxKnowledgeRung = 5;
    public const float KnowledgeDropGoldPerRung = 0.02f;

    public static int KnowledgeRung(int activeMentees) => Math.Clamp(activeMentees, 0, MaxKnowledgeRung);

    /// <summary>Both are re-checked this often, and a mentee who logged out keeps counting for the
    /// grace window so a crash costs nothing (his "ok" to 2 min / 3 min).</summary>
    public const int RecheckSeconds = 120;
    public const int GraceSeconds = 180;

    /// <summary>ACTIVE, for Mentor Knowledge only: combat or exp gained within this long (the 10 min is mine, and his
    /// fourth pass kept it: *"the active 10 mins timer can be used for mentors gold/drop rates"*).</summary>
    public const int ActiveWindowSeconds = 600;

    // ----- Removal penalty -----

    /// <summary>His five-step table, keyed on the OTHER side's last login: under 24h → 24h, under 3d → 12h,
    /// under 5d → 6h, under 7d → 3h, 7d+ → none. An online partner is "0m".</summary>
    public static TimeSpan RemovalPenalty(TimeSpan sinceOtherOnline)
    {
        double d = sinceOtherOnline.TotalDays;
        if (d < 1) return TimeSpan.FromHours(24);
        if (d < 3) return TimeSpan.FromHours(12);
        if (d < 5) return TimeSpan.FromHours(6);
        if (d < 7) return TimeSpan.FromHours(3);
        return TimeSpan.Zero;
    }

    /// <summary>The activity figure a mentee reads off a candidate mentor: days logged in of the last 7.</summary>
    public const int ActivityDays = 7;

    /// <summary>The list's last-online column, his rule: *"the same logic as buffs duration -&gt; 1m,5m,1h,5h,1d,30d
    /// … no seconds needed, when over 1h no minutes need, when its over a day no need for hours"*.</summary>
    public static string Ago(TimeSpan t)
    {
        if (t.TotalHours < 1) return $"{Math.Max(0, (int)t.TotalMinutes)}m";
        if (t.TotalDays < 1) return $"{(int)t.TotalHours}h";
        return $"{(int)t.TotalDays}d";
    }

    /// <summary>A penalty's remaining time, rounded UP so "0h" is never shown while one still runs.</summary>
    public static string Remaining(TimeSpan t)
    {
        if (t.TotalHours >= 1) return $"{(int)Math.Ceiling(t.TotalHours)}h";
        return $"{Math.Max(1, (int)Math.Ceiling(t.TotalMinutes))}m";
    }
}
