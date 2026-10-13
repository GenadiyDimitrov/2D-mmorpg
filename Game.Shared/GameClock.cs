using System;
using System.Linq;

namespace Game.Shared;

public enum DayPhase { Day = 0, Night = 1 }

/// <summary>
/// In-game time of day. Real time is multiplied by <see cref="TimeScale"/> to
/// get game time, so day/night cycle while you play.
///
/// === HOW TO CHANGE THE SPEED (the one knob) ===
/// TimeScale = how many in-game seconds pass per real second.
///   6   -> 1 real sec = 6 game sec; a full game day = 4 real hours (default).
///   60  -> a full game day = 24 real minutes (handy for testing).
///   600 -> a full game day = ~2.4 real minutes (watch night fall fast).
/// Change ONLY TimeScale; everything else derives from it.
///
/// === ADMIN CONTROL (`/time`, 2026-10-13) ===
/// The clock is TWO values, both pushed to every client (`ClockState`): the <see cref="Epoch"/> and an optional
/// <see cref="FrozenHour"/>. Setting the time SHIFTS the epoch (the clock keeps running from the new hour); stopping
/// it freezes the hour; reset puts back <see cref="NaturalEpoch"/>, the server's start. Runtime only — a restart is a
/// natural clock again.
/// </summary>
public static class GameClock
{
    /// <summary>In-game seconds per real second. The single speed knob.</summary>
    public const double TimeScale = 6.0;

    /// <summary>Length of a full in-game day, in in-game seconds (24h).</summary>
    public const double GameDaySeconds = 24 * 3600;

    /// <summary>In-game hour at which day begins / night begins.
    /// Day runs [DayStartHour, NightStartHour); night is the rest.</summary>
    public const double DayStartHour = 6.0;    // 06:00
    public const double NightStartHour = 18.0; // 18:00

    /// <summary>The server's start time (UTC). Game time is measured from here.
    /// Set once at startup so all clients agree; `/time &lt;value&gt;` shifts it.</summary>
    public static DateTime Epoch { get; set; } = DateTime.UtcNow;

    /// <summary>The epoch the server started with — what `/time reset` returns to. Server-side only.</summary>
    public static DateTime NaturalEpoch { get; set; } = DateTime.UtcNow;

    /// <summary>Non-null = the clock is stopped at this hour (`/time stop`).</summary>
    public static double? FrozenHour { get; set; }

    /// <summary>The named moments `/time &lt;keyword&gt;` accepts — the start of each stretch of the day. Dawn, dusk
    /// and the two hours around them are read off the client's light keys (`DayNight.Keys`); morning and night are the
    /// server's own phase flips, where the day/night mob populations swap.</summary>
    public static readonly (string Word, double Hour)[] Keywords =
    {
        ("midnight", 0.0),
        ("dawn", 4.5),
        ("morning", DayStartHour),
        ("day", DayStartHour),
        ("noon", 12.0),
        ("dusk", 17.5),
        ("evening", 17.5),
        ("night", NightStartHour),
    };

    /// <summary>"midnight 00:00, dawn 04:30, morning/day 06:00, …" — what `/help time` prints, so it cannot trail
    /// <see cref="Keywords"/>.</summary>
    public static string KeywordList() =>
        string.Join(", ", Keywords.GroupBy(k => k.Hour)
                                  .Select(g => string.Join("/", g.Select(k => k.Word)) + " " + Format(g.Key)));

    /// <summary>In-game hour-of-day [0,24) for a given real UTC moment.</summary>
    public static double HourOfDay(DateTime utcNow)
    {
        if (FrozenHour is double frozen) return frozen;
        double gameSeconds = (utcNow - Epoch).TotalSeconds * TimeScale;
        double secondsIntoDay = ((gameSeconds % GameDaySeconds) + GameDaySeconds) % GameDaySeconds;
        return secondsIntoDay / 3600.0;
    }

    /// <summary>Move the clock to <paramref name="hour"/>. A running clock keeps running from there (the epoch
    /// shifts); a stopped one stays stopped, at the new hour.</summary>
    public static void SetHour(double hour, DateTime utcNow)
    {
        if (FrozenHour.HasValue) { FrozenHour = hour; return; }
        Epoch = EpochFor(hour, utcNow);
    }

    /// <summary>Stop the clock at the hour it shows now. No-op when already stopped.</summary>
    public static void Stop(DateTime utcNow) => FrozenHour ??= HourOfDay(utcNow);

    /// <summary>Let a stopped clock run again, from the hour it was stopped at.</summary>
    public static void Start(DateTime utcNow)
    {
        if (FrozenHour is not double frozen) return;
        Epoch = EpochFor(frozen, utcNow);
        FrozenHour = null;
    }

    /// <summary>The natural clock again: the server's own epoch, running.</summary>
    public static void Reset()
    {
        Epoch = NaturalEpoch;
        FrozenHour = null;
    }

    private static DateTime EpochFor(double hour, DateTime utcNow) =>
        utcNow - TimeSpan.FromSeconds(hour * 3600.0 / TimeScale);

    /// <summary>`H:mm` / `HH:mm` (24h) or a <see cref="Keywords"/> word → an hour in [0,24).</summary>
    public static bool TryParseHour(string text, out double hour)
    {
        hour = 0;
        text = text.Trim().ToLowerInvariant();
        foreach (var (word, h) in Keywords)
            if (word == text) { hour = h; return true; }

        int colon = text.IndexOf(':');
        if (colon < 1 || colon != text.Length - 3) return false;
        if (!int.TryParse(text[..colon], out int hh) || !int.TryParse(text[(colon + 1)..], out int mm)) return false;
        if (hh < 0 || hh > 23 || mm < 0 || mm > 59) return false;
        hour = hh + mm / 60.0;
        return true;
    }

    public static DayPhase PhaseAt(double hourOfDay) =>
        hourOfDay >= DayStartHour && hourOfDay < NightStartHour ? DayPhase.Day : DayPhase.Night;

    public static DayPhase CurrentPhase(DateTime utcNow) => PhaseAt(HourOfDay(utcNow));

    /// <summary>"HH:MM" in-game clock string for UI.</summary>
    public static string Format(double hourOfDay)
    {
        // The epsilon: a clock SET to 13:00 runs from an epoch whose round trip can land on 12.99999…, which plain
        // truncation would print as 12:59.
        int total = (int)(hourOfDay * 60 + 1e-6) % (24 * 60);
        return $"{total / 60:00}:{total % 60:00}";
    }
}
