namespace Game.Shared;

/// <summary>Per-mob-type definition: movement speeds (walk while wandering, run
/// while aggroed) and behavior. Mobs are spawned by name from a zone's MobTypes;
/// this is the one place to tune how each type moves and acts.</summary>
public record MobType(
    string Name,
    float WalkSpeed,
    float RunSpeed,
    bool Aggressive = false);

/// <summary>
/// THE place to manage mob types. Add a mob's speeds/behavior here; spawning
/// reads it by name. Run speeds are below the player move cap (250) and varied
/// so players can kite — a fighter outruns a bandit; only fast mobs (wolf 150)
/// threaten a slow mage.
/// </summary>
public static class MobCatalog
{
    private static readonly Dictionary<string, MobType> All = Build();

    private static Dictionary<string, MobType> Build()
    {
        var list = new[]
        {
            //            name       walk  run   aggressive
            new MobType("Wolf",      80f,  150f, Aggressive: true),
            new MobType("Boar",      55f,  100f),
            new MobType("Slime",     35f,   60f),
            new MobType("Spider",    70f,  120f, Aggressive: true),
            new MobType("Bandit",    60f,  108f, Aggressive: true),
        };
        var dict = new Dictionary<string, MobType>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in list)
            dict[m.Name] = m;
        return dict;
    }

    /// <summary>Look up a mob type by name (case-insensitive). Falls back to a
    /// sane default for unknown names so spawning never crashes.</summary>
    public static MobType Get(string name) =>
        All.TryGetValue(name, out var m) ? m : new MobType(name, 60f, 110f);

    /// <summary>Is this mob type aggressive by default? (Elites/bosses can also
    /// be forced aggressive at spawn regardless.)</summary>
    public static bool IsAggressive(string name) => Get(name).Aggressive;
}
