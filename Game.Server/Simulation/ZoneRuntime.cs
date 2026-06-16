using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>
/// Runtime state for one <see cref="SpawnZone"/>. The game loop ticks every
/// zone once per server tick. A zone keeps its living-mob count up to MaxCount,
/// and when a mob dies it schedules a respawn after a delay rolled from the
/// zone's (RespawnSeconds ± Variance). Boss/elite death times can be persisted
/// so their long timers survive a restart.
/// </summary>
public class ZoneRuntime
{
    public SpawnZone Zone { get; }

    /// <summary>Number of this zone's mobs currently alive in the world.</summary>
    public int AliveCount { get; private set; }

    /// <summary>Pending respawns: server-time (ticks) at which to spawn one.</summary>
    private readonly List<long> _pendingRespawnTicks = new();

    public ZoneRuntime(SpawnZone zone) => Zone = zone;

    public void OnSpawned() => AliveCount++;

    /// <summary>Called when one of this zone's mobs dies: schedule a respawn.</summary>
    public void OnDeath(long nowTicks, Random rng)
    {
        AliveCount = Math.Max(0, AliveCount - 1);
        _pendingRespawnTicks.Add(nowTicks + RollDelayTicks(rng));
    }

    /// <summary>Schedule a respawn at a specific absolute tick (used to restore
    /// a persisted boss timer on startup).</summary>
    public void ScheduleAt(long absoluteTick) => _pendingRespawnTicks.Add(absoluteTick);

    public long RollDelayTicks(Random rng)
    {
        double variance = Zone.RespawnVariance <= 0
            ? 0
            : (rng.NextDouble() * 2 - 1) * Zone.RespawnVariance; // ±variance
        double seconds = Math.Max(0.5, Zone.RespawnSeconds + variance);
        return (long)(seconds * GameConstants.TickRate);
    }

    /// <summary>How many mobs are due to (re)spawn this tick, respecting the cap
    /// and the active time-of-day window. Removes the matured timers.</summary>
    public int DueToSpawn(long nowTicks, DayPhase phase)
    {
        if (!Zone.IsActiveAt(phase))
            return 0;

        int due = 0;
        for (int i = _pendingRespawnTicks.Count - 1; i >= 0; i--)
        {
            if (_pendingRespawnTicks[i] <= nowTicks)
            {
                _pendingRespawnTicks.RemoveAt(i);
                if (AliveCount + due < Zone.MaxCount)
                    due++;
            }
        }
        return due;
    }

    /// <summary>Initial fill at startup: how many to spawn right now to reach the
    /// cap, if the zone is active. (Boss/elite timers may instead be restored
    /// from persistence — see GameLoopService.)</summary>
    public int InitialFill(DayPhase phase) =>
        Zone.IsActiveAt(phase) ? Zone.MaxCount : 0;

    /// <summary>Earliest pending respawn tick (for persisting boss timers), or null.</summary>
    public long? NextPendingTick =>
        _pendingRespawnTicks.Count == 0 ? null : _pendingRespawnTicks.Min();
}
