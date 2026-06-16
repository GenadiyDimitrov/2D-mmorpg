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
    /// Set once at startup so all clients agree.</summary>
    public static DateTime Epoch { get; set; } = DateTime.UtcNow;

    /// <summary>In-game hour-of-day [0,24) for a given real UTC moment.</summary>
    public static double HourOfDay(DateTime utcNow)
    {
        double gameSeconds = (utcNow - Epoch).TotalSeconds * TimeScale;
        double secondsIntoDay = ((gameSeconds % GameDaySeconds) + GameDaySeconds) % GameDaySeconds;
        return secondsIntoDay / 3600.0;
    }

    public static DayPhase PhaseAt(double hourOfDay) =>
        hourOfDay >= DayStartHour && hourOfDay < NightStartHour ? DayPhase.Day : DayPhase.Night;

    public static DayPhase CurrentPhase(DateTime utcNow) => PhaseAt(HourOfDay(utcNow));

    /// <summary>"HH:MM" in-game clock string for UI.</summary>
    public static string Format(double hourOfDay)
    {
        int h = (int)hourOfDay;
        int m = (int)((hourOfDay - h) * 60);
        return $"{h:00}:{m:00}";
    }
}
