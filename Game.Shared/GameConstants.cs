namespace Game.Shared;

/// <summary>
/// Tunable values shared by server and all clients.
/// Server is authoritative — clients use these only for prediction/UI.
/// </summary>
public static class GameConstants
{
    /// <summary>Simulation ticks per second on the server.</summary>
    public const int TickRate = 10;

    /// <summary>Seconds per tick (0.1s at 10 t/s).</summary>
    public const float TickSeconds = 1f / TickRate;

    /// <summary>How far (world units) an entity can see other entities.</summary>
    public const float ViewRange = 3000f;

    /// <summary>Interest-management cell size. Equal to ViewRange so a 3x3
    /// cell neighborhood always covers the full view circle.</summary>
    public const float CellSize = 3000f;

    /// <summary>Demo zone size. Design target is 75000x75000; we use a
    /// smaller zone while there is only one of them.</summary>
    public const float ZoneWidth = 15000f;
    public const float ZoneHeight = 15000f;

    /// <summary>Base/maximum player movement speed in units per second.</summary>
    public const float BasePlayerSpeed = 250f;

    public const int MaxCharacterNameLength = 16;

    // ----- Combat (Phase 2) -------------------------------------------------

    /// <summary>Base melee range. Design doc: base attack range is 40;
    /// we use 80 for a forgiving feel until weapons define ranges.</summary>
    public const float MeleeRange = 80f;

    /// <summary>Ticks between player basic attacks (1.5s). Attack-speed
    /// stats/buffs will modify this in a later phase.</summary>
    public const int PlayerAttackIntervalTicks = 15;

    /// <summary>Ticks between mob basic attacks (2.0s).</summary>
    public const int MobAttackIntervalTicks = 20;

    /// <summary>Aggressive mobs attack players that come this close.</summary>
    public const float MobAggroRange = 1000f;

    /// <summary>A mob chased this far from home resets: returns and heals.</summary>
    public const float MobLeashRange = 4000f;

    /// <summary>Ticks until a dead mob respawns at its home position (10s).</summary>
    public const int MobRespawnTicks = 100;

    /// <summary>Out-of-combat regen is applied once per this many ticks (1s).</summary>
    public const int RegenIntervalTicks = 10;
}
