using System;
using System.Collections.Generic;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Reconciles the server's DELTA feed with the scene. Delta semantics differ from the old full
    /// snapshot in one way that matters: an entity absent from a frame is UNCHANGED, not removed —
    /// only an explicit despawn removes it. Treating absence as removal (as the full-snapshot code
    /// did) would delete every entity every tick.
    ///
    /// It also keeps the last known DTO per entity, because the HUD needs names/levels/HP that the
    /// lean per-tick update does not repeat. Call the Apply* methods on the MAIN thread.
    /// </summary>
    public class EntityManager : MonoBehaviour
    {
        public Guid SelfId;

        /// <summary>Raised when a frame referenced an entity we never saw spawn — the one symptom of a
        /// desynced delta stream. GameBoot answers it with a resync request.</summary>
        public event Action MissingEntity;

        /// <summary>Last full state per entity — spawns write it, lean updates patch the dynamic
        /// fields. This is what the HUD and nameplates read.</summary>
        public readonly Dictionary<Guid, EntityDto> States = new Dictionary<Guid, EntityDto>();

        private readonly Dictionary<Guid, EntityView> _views = new Dictionary<Guid, EntityView>();

        public EntityView Find(Guid id) => _views.TryGetValue(id, out var v) ? v : null;

        public bool TryGetState(Guid id, out EntityDto dto) => States.TryGetValue(id, out dto);

        public int Count => _views.Count;

        /// <summary>Apply one server delta frame.</summary>
        public void ApplyDelta(SnapshotDelta delta)
        {
            if (delta == null) return;

            if (delta.Spawns != null)
                foreach (var e in delta.Spawns) Upsert(e);

            if (delta.Updates != null)
                foreach (var lean in delta.Updates)
                {
                    // A lean update for an entity we never saw spawn can't be drawn (no name/kind).
                    // Don't invent a placeholder — but DO shout, because on its own this entity would
                    // stay invisible forever: the server believes we already have it and will never
                    // send the full DTO again.
                    if (!States.TryGetValue(lean.Id, out var prev)) { MissingEntity?.Invoke(); continue; }
                    Upsert(prev with
                    {
                        X = lean.X,
                        Y = lean.Y,
                        Speed = lean.Speed,
                        Hp = lean.Hp,
                        Mp = lean.Mp,
                        Dead = lean.Dead,
                        Disconnected = lean.Disconnected,
                        Flag = lean.Flag,
                    });
                }

            if (delta.Despawns != null)
                foreach (var id in delta.Despawns) Remove(id);
        }

        /// <summary>Legacy full-snapshot path, kept so the client still works if the server is ever
        /// pointed back at "Snapshot". Here absence DOES mean removal.</summary>
        public void ApplySnapshot(EntityDto[] entities)
        {
            if (entities == null) return;
            var seen = new HashSet<Guid>();
            foreach (var e in entities) { seen.Add(e.Id); Upsert(e); }

            var stale = new List<Guid>();
            foreach (var kv in _views) if (!seen.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale) Remove(id);
        }

        /// <summary>Name which entity is "you". EnterWorld only learns the id AFTER the first frames can
        /// already have arrived, so anything already on screen has to be re-tinted rather than assumed
        /// to spawn again — a stationary player is byte-identical every tick and is never re-sent.</summary>
        public void SetSelf(Guid id)
        {
            SelfId = id;
            foreach (var kv in _views)
            {
                if (kv.Value == null) continue;
                kv.Value.IsSelf = kv.Key == id;
                if (States.TryGetValue(kv.Key, out var dto)) kv.Value.SetColor(ColorFor(dto));
            }
        }

        public void Clear()
        {
            foreach (var kv in _views) if (kv.Value != null) Destroy(kv.Value.gameObject);
            _views.Clear();
            States.Clear();
        }

        private void Upsert(EntityDto e)
        {
            States[e.Id] = e;

            if (!_views.TryGetValue(e.Id, out var view) || view == null)
            {
                view = Create(e);
                _views[e.Id] = view;
            }

            view.IsSelf = e.Id == SelfId;
            view.SetTarget(WorldMapper.ToUnity(e.X, e.Y));
            view.SetColor(ColorFor(e));
            // Dead entities stay in the world (corpses are lootable/visible) but are dimmed rather
            // than hidden, so a kill is legible instead of things silently vanishing.
            view.SetDead(e.Dead);
        }

        private void Remove(Guid id)
        {
            if (_views.TryGetValue(id, out var view))
            {
                if (view != null) Destroy(view.gameObject);
                _views.Remove(id);
            }
            States.Remove(id);
        }

        private EntityView Create(EntityDto e)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = string.IsNullOrEmpty(e.Name) ? e.Kind.ToString() : e.Name;
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            go.transform.position = WorldMapper.ToUnity(e.X, e.Y) + Vector3.up * 0.75f;

            // Quads come with a MeshCollider; swap for a thin BoxCollider so tap-to-target is reliable.
            var mesh = go.GetComponent<Collider>();
            if (mesh != null) Destroy(mesh);
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 0.25f);

            var view = go.AddComponent<EntityView>();
            view.Id = e.Id;
            view.IsSelf = e.Id == SelfId;
            view.Init(ColorFor(e));
            return view;
        }

        private Color ColorFor(EntityDto e)
        {
            if (e.Id == SelfId) return Color.green;
            switch (e.Kind)
            {
                case EntityKind.Player: return Color.cyan;
                case EntityKind.Mob:    return Color.red;
                case EntityKind.Npc:    return Color.yellow;
                default:                return Color.white;
            }
        }
    }
}
