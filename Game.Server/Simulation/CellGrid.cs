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
    private readonly int _cols;
    private readonly int _rows;

    public CellGrid(float width, float height, float cellSize)
    {
        _cellSize = cellSize;
        _cols = (int)MathF.Ceiling(width / cellSize);
        _rows = (int)MathF.Ceiling(height / cellSize);
    }

    public (int, int) CellOf(float x, float y) =>
        ((int)Math.Clamp(x / _cellSize, 0, _cols - 1),
         (int)Math.Clamp(y / _cellSize, 0, _rows - 1));

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
