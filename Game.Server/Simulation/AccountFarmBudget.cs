namespace Game.Server.Simulation;

/// <summary>
/// One account's daily allowance of automated farming, shared by every character on it.
///
/// <para>This replaces a per-SESSION elapsed counter that lived on the character. The old model was
/// broken twice over (verified 2026-08-05): entering the world zeroed the counters, and
/// <c>BeginOfflineFarm</c> zeroed the offline one again — so hitting the "2h offline cap" and
/// re-logging handed the whole 2h straight back, forever. Being per-ENTITY, it also meant three
/// characters farmed 6h per 2h of wall clock.</para>
///
/// <para>So the allowance is a BALANCE that is SPENT, not an elapsed time compared to a cap, and it
/// hangs off the ACCOUNT. The drain rule falls out of that with no special cases: every tick, each of
/// the account's characters that is farming spends one tick of it. One character gets the full 2h;
/// ten characters get twelve minutes each. Ten characters × 12 min yields exactly the same gold as
/// one × 2h, which is the whole point — the ceiling is gold/hour/ACCOUNT.</para>
///
/// <para>Refill is a fixed server midnight (<see cref="EnsureFresh"/>). Deliberately NOT a rolling
/// "24h since the last refill": that anchors the reset to whenever you last spent, so it drifts —
/// play at 08:00 and your next window is 08:00; miss it, start at 22:00, and it walks round the clock
/// and eventually costs you a whole day. A fixed 00:00 means the allowance is yours to spend anywhere
/// in the day, which is what a daily allowance is for.</para>
///
/// <para>⚠ The midnight double-tank is ACCEPTED and deliberate: start at 22:00, drain 2h, reset at
/// 00:00, drain 2h more = 4h between 22:00 and 02:00. It still averages to the cap per day, and it is
/// player agency. Do not "fix" it.</para>
/// </summary>
public sealed class AccountFarmBudget
{
    public int AccountId { get; init; }

    /// <summary>Ticks of ONLINE auto-hunt left today. Meaningless while <see cref="AutoUnlimited"/>.</summary>
    public long AutoTicksLeft { get; set; }

    /// <summary>Ticks of OFFLINE farming left today.</summary>
    public long OfflineTicksLeft { get; set; }

    /// <summary>Server-local date of the last refill. See the class remarks for why it is a DATE.</summary>
    public DateOnly LastResetDate { get; set; }

    /// <summary>Per-account cap override in seconds — the premium knob.
    /// <c>-1</c> = use the server default · <c>0</c> = unlimited · &gt;0 = explicit.</summary>
    public int AutoCapSeconds { get; set; } = -1;
    public int OfflineCapSeconds { get; set; } = -1;

    /// <summary>Set on every spend; cleared by the autosave that writes the row. Without it the
    /// periodic save would rewrite an account row per tick for an account that isn't farming.</summary>
    public bool Dirty { get; set; }

    /// <summary>Resolve a cap against the server default: the account override wins when it is &ge; 0.
    /// A resolved cap of 0 (or less) means UNLIMITED, on either side.</summary>
    public static int ResolveCap(int accountOverride, int serverDefault)
        => accountOverride >= 0 ? accountOverride : serverDefault;

    /// <summary>Refill both balances if the server date has rolled over since the last refill. Called
    /// lazily on every read/spend, so it costs nothing and needs no scheduler — and a server that was
    /// down over midnight still comes back with a full allowance.</summary>
    public void EnsureFresh(int autoCapSeconds, int offlineCapSeconds, int tickRate)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);   // SERVER-local midnight, on purpose
        if (LastResetDate >= today) return;
        LastResetDate = today;
        AutoTicksLeft = (long)Math.Max(0, autoCapSeconds) * tickRate;
        OfflineTicksLeft = (long)Math.Max(0, offlineCapSeconds) * tickRate;
        Dirty = true;
    }
}
