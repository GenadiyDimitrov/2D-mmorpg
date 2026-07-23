using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>
/// Interest management: the zone is divided into cells of ViewRange size,
/// so "everything I can see" is always inside my cell + the 8 neighbors.
/// Structurally this is the same nested-dictionary bookkeeping as any
/// registry: add on enter, remove on leave, move between buckets on update.
/// Touched only by the game-loop thread — no locking needed.
/// </summary>
public class CellGrid
{
    private readonly Dictionary<(int, int), HashSet<Entity>> _cells = new();
    private readonly float _cellSize;

    // width/height are no longer needed to size a fixed grid (the dictionary is sparse and unbounded, so
    // it spans the negative quadrant for free) — kept in the signature so callers/World.cs stay unchanged.
    public CellGrid(float width, float height, float cellSize) => _cellSize = cellSize;

    // FLOOR, not truncation + clamp: the grid is a sparse dictionary, so negative coordinates (the
    // dungeon/jail quadrant) get their OWN negative cells instead of being clamped into cell 0 alongside
    // the overworld corner. Floor(-0.2) = -1 is the correct bucket; (int)(-0.2) = 0 would not be.
    public (int, int) CellOf(float x, float y) =>
        ((int)MathF.Floor(x / _cellSize), (int)MathF.Floor(y / _cellSize));

    public void Add(Entity e)
    {
        e.Cell = CellOf(e.X, e.Y);
        GetOrCreate(e.Cell).Add(e);
    }

    public void Remove(Entity e)
    {
        if (_cells.TryGetValue(e.Cell, out var set))
        {
            set.Remove(e);
            if (set.Count == 0)
                _cells.Remove(e.Cell); // don't leak empty buckets
        }
    }

    /// <summary>Call after an entity's X/Y changed. Moves it between cells
    /// only when it actually crossed a boundary.</summary>
    public void UpdatePosition(Entity e)
    {
        var newCell = CellOf(e.X, e.Y);
        if (newCell == e.Cell)
            return;

        Remove(e);
        e.Cell = newCell;
        GetOrCreate(newCell).Add(e);
    }

    /// <summary>All entities within ViewRange of <paramref name="center"/>,
    /// including the center entity itself.</summary>
    public IEnumerable<Entity> Nearby(Entity center)
    {
        const float rangeSq = GameConstants.ViewRange * GameConstants.ViewRange;
        var (cx, cy) = center.Cell;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (!_cells.TryGetValue((cx + dx, cy + dy), out var set))
                    continue;

                foreach (var e in set)
                {
                    float ddx = e.X - center.X;
                    float ddy = e.Y - center.Y;
                    if (ddx * ddx + ddy * ddy <= rangeSq)
                        yield return e;
                }
            }
        }
    }

    private HashSet<Entity> GetOrCreate((int, int) cell) =>
        _cells.TryGetValue(cell, out var set) ? set : _cells[cell] = new HashSet<Entity>();
}
