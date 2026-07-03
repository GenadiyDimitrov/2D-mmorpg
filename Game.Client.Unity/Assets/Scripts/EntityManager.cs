using System;
using System.Collections.Generic;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Reconciles each server WorldSnapshot with the scene: creates a billboard for a newly-seen
    /// entity, moves existing ones, and destroys those that left view. Colors distinguish self /
    /// players / mobs / NPCs (placeholder art until 3D models land). Call ApplySnapshot on the
    /// MAIN thread (GameBoot marshals it).
    /// </summary>
    public class EntityManager : MonoBehaviour
    {
        public Guid SelfId;

        private readonly Dictionary<Guid, EntityView> _views = new Dictionary<Guid, EntityView>();
        private readonly HashSet<Guid> _seen = new HashSet<Guid>();
        private readonly List<Guid> _stale = new List<Guid>();

        public EntityView Find(Guid id) => _views.TryGetValue(id, out var v) ? v : null;

        public void ApplySnapshot(EntityDto[] entities)
        {
            _seen.Clear();
            foreach (var e in entities)
            {
                _seen.Add(e.Id);
                if (!_views.TryGetValue(e.Id, out var view))
                {
                    view = Create(e);
                    _views[e.Id] = view;
                }
                view.SetTarget(WorldMapper.ToUnity(e.X, e.Y));
                view.gameObject.SetActive(!e.Dead);
            }

            _stale.Clear();
            foreach (var kv in _views)
                if (!_seen.Contains(kv.Key)) _stale.Add(kv.Key);
            foreach (var id in _stale)
            {
                Destroy(_views[id].gameObject);
                _views.Remove(id);
            }
        }

        private EntityView Create(EntityDto e)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = string.IsNullOrEmpty(e.Name) ? e.Kind.ToString() : e.Name;
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
