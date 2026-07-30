using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>
/// Runtime state for one <see cref="SpawnZone"/>. The game loop ticks every
/// zone once per server tick. A zone keeps its living-mob count up to MaxCount,
/// and when a mob dies it schedules a respawn after a delay rolled from the
/// zone's (RespawnSeconds ± Variance). Boss/elite death times can be persisted
/// so their long timers survive a restart.
///
/// A zone has TWO kinds of population, tracked separately:
///
///   • the MIXED pool — MaxCount mobs, each spawn rolling the zone's roster. A death here respawns
///     "something from this camp", which is what gives a camp its variety.
///   • one bucket per <see cref="DedicatedSpawn"/> — a fixed count of ONE template, where a death
///     respawns THAT template. This is the owner's playtest-14 requirement: killing a werewolf must
///     put a werewolf back, not re-roll four possibilities.
///
/// They are kept apart on purpose. If the two shared one counter, a mixed roll that happened to pick
/// a dedicated template would overfill that bucket and starve the pool, and the guaranteed population
/// a quest depends on would drift right back to the dice. That is also why a spawn says WHICH spawner
/// it came from rather than being inferred from its template.
/// </summary>
public class ZoneRuntime
{
    public SpawnZone Zone { get; }

    /// <summary>Alive from the mixed roster pool.</summary>
    private int _aliveMixed;

    /// <summary>Alive per dedicated template (keys are the zone's <see cref="DedicatedSpawn"/> ids).</summary>
    private readonly Dictionary<string, int> _aliveDedicated =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Pending mixed respawns: server-time (ticks) at which to spawn one.</summary>
    private readonly List<long> _pendingMixed = new();

    /// <summary>Pending dedicated respawns, each carrying the template it owes.</summary>
    private readonly List<(long Tick, string MobId)> _pendingDedicated = new();

    public ZoneRuntime(SpawnZone zone)
    {
        Zone = zone;
        foreach (var d in zone.DedicatedSpawns)
            _aliveDedicated[d.MobId] = 0;
    }

    /// <summary>Number of this zone's mobs currently alive in the world (both kinds).</summary>
    public int AliveCount => _aliveMixed + _aliveDedicated.Values.Sum();

    /// <summary>A mob was placed. <paramref name="dedicatedMobId"/> is the template whose dedicated
    /// spawner produced it, or null for a mixed-pool spawn.</summary>
    public void OnSpawned(string? dedicatedMobId = null)
    {
        if (dedicatedMobId is null || !_aliveDedicated.ContainsKey(dedicatedMobId))
            _aliveMixed++;
        else
            _aliveDedicated[dedicatedMobId]++;
    }

    /// <summary>Called when one of this zone's mobs dies: schedule its respawn. A mob from a dedicated
    /// spawner schedules the SAME template; a mixed-pool mob schedules another roster roll.</summary>
    public void OnDeath(long nowTicks, Random rng, string? dedicatedMobId = null)
    {
        long at = nowTicks + RollDelayTicks(rng);

        if (dedicatedMobId is not null && _aliveDedicated.ContainsKey(dedicatedMobId))
        {
            _aliveDedicated[dedicatedMobId] = Math.Max(0, _aliveDedicated[dedicatedMobId] - 1);
            _pendingDedicated.Add((at, dedicatedMobId));
        }
        else
        {
            _aliveMixed = Math.Max(0, _aliveMixed - 1);
            _pendingMixed.Add(at);
        }
    }

    /// <summary>Schedule a respawn at a specific absolute tick (used to restore
    /// a persisted boss timer on startup). Boss/elite zones carry no dedicated
    /// spawners, so this is always a mixed-pool spawn.</summary>
    public void ScheduleAt(long absoluteTick) => _pendingMixed.Add(absoluteTick);

    public long RollDelayTicks(Random rng)
    {
        double variance = Zone.RespawnVariance <= 0
            ? 0
            : (rng.NextDouble() * 2 - 1) * Zone.RespawnVariance; // ±variance
        double seconds = Math.Max(0.5, Zone.RespawnSeconds + variance);
        return (long)(seconds * GameConstants.TickRate);
    }

    /// <summary>Which mobs are due to (re)spawn this tick, respecting each spawner's own cap and the
    /// active time-of-day window. Removes the matured timers. Each entry is the template to spawn, or
    /// null for "roll the roster" (a mixed-pool spawn).</summary>
    public List<string?> DueToSpawn(long nowTicks, DayPhase phase)
    {
        var due = new List<string?>();
        if (!Zone.IsActiveAt(phase))
            return due;

        // Dedicated first: a guaranteed population should not lose a tick's slot to the pool.
        for (int i = _pendingDedicated.Count - 1; i >= 0; i--)
        {
            var (tick, mobId) = _pendingDedicated[i];
            if (tick > nowTicks) continue;

            _pendingDedicated.RemoveAt(i);
            int pendingSame = due.Count(m => string.Equals(m, mobId, StringComparison.OrdinalIgnoreCase));
            if (_aliveDedicated.GetValueOrDefault(mobId) + pendingSame < Zone.DedicatedCount(mobId))
                due.Add(mobId);
        }

        int mixedDue = 0;
        for (int i = _pendingMixed.Count - 1; i >= 0; i--)
        {
            if (_pendingMixed[i] > nowTicks) continue;

            _pendingMixed.RemoveAt(i);
            if (_aliveMixed + mixedDue < Zone.MaxCount)
            {
                mixedDue++;
                due.Add(null);
            }
        }
        return due;
    }

    /// <summary>Initial fill at startup: everything to spawn right now to reach every cap, if the zone
    /// is active — each dedicated spawner's full count, then the mixed pool. (Boss/elite timers may
    /// instead be restored from persistence — see GameLoopService.)</summary>
    public List<string?> InitialFill(DayPhase phase)
    {
        var fill = new List<string?>();
        if (!Zone.IsActiveAt(phase))
            return fill;

        foreach (var d in Zone.DedicatedSpawns)
            for (int i = 0; i < d.Count; i++)
                fill.Add(d.MobId);

        for (int i = 0; i < Zone.MaxCount; i++)
            fill.Add(null);

        return fill;
    }

    /// <summary>Refill after a day/night flip emptied the zone: what is missing from each cap, given
    /// what is currently alive. Counts are recomputed from the world by the caller via
    /// <see cref="ResetAlive"/> before this is asked.</summary>
    public List<string?> RefillNeeded(DayPhase phase)
    {
        var need = new List<string?>();
        if (!Zone.IsActiveAt(phase))
            return need;

        foreach (var d in Zone.DedicatedSpawns)
            for (int i = _aliveDedicated.GetValueOrDefault(d.MobId); i < d.Count; i++)
                need.Add(d.MobId);

        for (int i = _aliveMixed; i < Zone.MaxCount; i++)
            need.Add(null);

        return need;
    }

    /// <summary>Re-seed the alive counts from the world (used after a day/night despawn, where mobs are
    /// removed without dying). Dedicated buckets are filled first, up to their cap, and the remainder
    /// of that template counts as mixed — the same split the spawners would have produced.</summary>
    public void ResetAlive(IEnumerable<string> aliveMobTypeIds)
    {
        _aliveMixed = 0;
        foreach (string key in _aliveDedicated.Keys.ToList())
            _aliveDedicated[key] = 0;

        foreach (string mobId in aliveMobTypeIds)
        {
            if (_aliveDedicated.ContainsKey(mobId) &&
                _aliveDedicated[mobId] < Zone.DedicatedCount(mobId))
                _aliveDedicated[mobId]++;
            else
                _aliveMixed++;
        }
    }

    /// <summary>Earliest pending respawn tick (for persisting boss timers), or null.</summary>
    public long? NextPendingTick
    {
        get
        {
            long? best = _pendingMixed.Count == 0 ? null : _pendingMixed.Min();
            if (_pendingDedicated.Count > 0)
            {
                long d = _pendingDedicated.Min(p => p.Tick);
                best = best is long b ? Math.Min(b, d) : d;
            }
            return best;
        }
    }
}
