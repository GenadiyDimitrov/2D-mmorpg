using System;
using System.Collections.Generic;

namespace Game.Shared;

/// <summary>`BL-283` — **CHARISMA**, model (c) as he ruled it 2026-09-23. Full design:
/// <c>docs/design/Rework-2026-09-23.md</c> §3 and the archived `BL-283` entry.
///
/// <para>A recommendation from another player is worth <see cref="PointsPerRecommendation"/>. You RECEIVE at
/// most <see cref="MaxReceivedPerDay"/> a day, one per giver. <b>Current</b> = the sum of the last
/// <see cref="WindowDays"/> days, capped at <see cref="CurrentCap"/> when READ (never by refusing one), so a
/// full 10 a day fills it on day 10 and up to 20 missed days cost nothing. <b>Lifetime</b> = everything ever
/// received; it is what the board and the #1 title ("Beloved") rank on, and what PK/moderation penalties
/// drain (his call, 2026-09-24: penalties touch LIFETIME only, a ban zeroes both).</para>
///
/// <para>🔑 **Current does ONE thing: it speeds up the Wayfarer's Blessing fill** (<see cref="FillMultiplier"/>),
/// multiplied with the booster rune (×4 with both). The old +0-50% EXP/SP bonus is gone (ruling 4,
/// *"nothing else"*, confirmed 2026-09-24).</para>
///
/// <para>Storage is a ring of daily totals, NEWEST FIRST: slot 0 is the day <c>ringDay</c>, slot i is
/// <c>ringDay − i</c>. Days are UTC day numbers (<see cref="Today"/>), the same midnight the like budget
/// resets on. Shared so the online path (the live entity) and the offline path (the DB row) run one rule.</para></summary>
public static class Charisma
{
    public const int PointsPerRecommendation = 10;
    /// <summary>Recommendations a character may RECEIVE per UTC day (one per giver).</summary>
    public const int MaxReceivedPerDay = 10;
    /// <summary>The ring's length: current = the last 30 days.</summary>
    public const int WindowDays = 30;
    /// <summary>Current's ceiling (applied when read). Lifetime is uncapped.</summary>
    public const int CurrentCap = 1000;
    /// <summary>A giver must be at least this level (rule 1).</summary>
    public const int MinGiverLevel = 20;
    /// <summary>+<see cref="FillPerStep"/> Blessing fill per FULL 100 current (123 is still +10%).</summary>
    public const int PointsPerFillStep = 100;
    public const float FillPerStep = 0.1f;

    /// <summary>Today's UTC day number.</summary>
    public static int Today() => (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerDay);

    /// <summary>The Blessing fill multiplier for a CURRENT value: 1 + 0.1 × floor(current / 100), so 1.0 … 2.0.</summary>
    public static float FillMultiplier(int current) =>
        1f + FillPerStep * (Math.Clamp(current, 0, CurrentCap) / PointsPerFillStep);

    /// <summary>Current charisma as of <paramref name="today"/>, WITHOUT advancing the ring: slots that
    /// have aged out of the window since <paramref name="ringDay"/> are simply not counted.</summary>
    public static int Current(int[] ring, int ringDay, int today)
    {
        int aged = Math.Max(0, today - ringDay);
        long sum = 0;
        for (int i = 0; i + aged < WindowDays && i < ring.Length; i++) sum += ring[i];
        return (int)Math.Min(CurrentCap, sum);
    }

    /// <summary>Roll the ring forward to <paramref name="today"/>: every reset that passed (max 30, however
    /// long the character was away) drops the oldest slot and opens a 0. A new day also clears the list of
    /// who gave today.</summary>
    public static void Advance(int[] ring, ref int ringDay, List<string> giversToday, int today)
    {
        if (today <= ringDay) return;
        int shift = Math.Min(WindowDays, today - ringDay);
        for (int i = WindowDays - 1; i >= 0; i--) ring[i] = i >= shift ? ring[i - shift] : 0;
        ringDay = today;
        giversToday.Clear();
    }

    public enum Refusal { None, AlreadyToday, FullToday }

    /// <summary>Receive one recommendation from <paramref name="giver"/> into the ring (advanced to today
    /// first). Refused if that giver already gave today or 10 have been received today; otherwise +10 on
    /// today's slot. The caller adds the lifetime points.</summary>
    public static Refusal TryReceive(int[] ring, ref int ringDay, List<string> giversToday, string giver, int today)
    {
        Advance(ring, ref ringDay, giversToday, today);
        foreach (var g in giversToday)
            if (string.Equals(g, giver, StringComparison.OrdinalIgnoreCase)) return Refusal.AlreadyToday;
        if (giversToday.Count >= MaxReceivedPerDay) return Refusal.FullToday;
        giversToday.Add(giver);
        ring[0] += PointsPerRecommendation;
        return Refusal.None;
    }

    public static string RefusalText(Refusal r, string target) => r switch
    {
        Refusal.AlreadyToday => $"You have already recommended {target} today.",
        Refusal.FullToday    => $"{target} has already received {MaxReceivedPerDay} recommendations today.",
        _                    => "",
    };

    // ----- persistence: the ring and the givers are two small CSV columns -----

    public static int[] ParseRing(string? csv)
    {
        var ring = new int[WindowDays];
        if (string.IsNullOrEmpty(csv)) return ring;
        var parts = csv!.Split(',');
        for (int i = 0; i < parts.Length && i < WindowDays; i++)
            if (int.TryParse(parts[i], out int v) && v > 0) ring[i] = v;
        return ring;
    }

    public static string FormatRing(int[] ring) => string.Join(",", ring);

    public static List<string> ParseGivers(string? csv) =>
        string.IsNullOrEmpty(csv) ? new List<string>() : new List<string>(csv!.Split(','));

    public static string FormatGivers(List<string> givers) => string.Join(",", givers);
}
