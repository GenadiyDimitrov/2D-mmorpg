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

        /// <summary>Diameter of an entity marker, in Unity units (the world is 0.01 scale, so 240
        /// units across). Sized down from 1.5 on 2026-07-21 to match how small entities read in the
        /// WPF view — at 1.5 they crowded the top-down camera.
        ///
        /// A FIELD, not a const, because it is tunable from the Settings window: the right value is a
        /// look question that can only be answered on the device, and a rebuild per guess is a bad
        /// way to answer it.</summary>
        public static float EntityScale = 0.9f;

        /// <summary>Re-scale markers already in the world after the setting changes — otherwise the
        /// new size would only apply to things that happen to respawn.</summary>
        public void ApplyEntityScale()
        {
            foreach (var kv in _views)
                if (kv.Value != null)
                    kv.Value.transform.localScale = Vector3.one * EntityScale;
        }

        /// <summary>The marker is smaller than a fingertip, so the tap target is deliberately bigger
        /// than the marker.</summary>
        public const float TapTargetScale = 2.0f;

        // How the SELF marker currently draws (`BL-82`): opacity for the stealth family, a golden ring
        // for god mode. Held here rather than pushed straight at the view because the view is created
        // and destroyed by the snapshot feed — walk far enough for a resync and a freshly built self
        // marker would come back solid, mid-stealth, with nothing to tell it otherwise.
        private float _selfAlpha = 1f;
        private bool _selfGod;

        /// <summary>Set how your own marker draws. See <c>GameBoot.ApplySelfVisibility</c> for the
        /// rule; this end just applies it and remembers it for the next self view.</summary>
        public void SetSelfVisual(float alpha, bool god)
        {
            _selfAlpha = alpha;
            _selfGod = god;
            if (SelfId != Guid.Empty && _views.TryGetValue(SelfId, out var self) && self != null)
                ApplySelfVisual(self);
        }

        private void ApplySelfVisual(EntityView view)
        {
            view.SetOpacity(_selfAlpha);
            view.SetHalo(_selfGod, GodHalo);
        }

        /// <summary>The god-mode ring. Gold, and deliberately translucent — it is a border around your
        /// marker, not a repaint of it, so you can still read your own dot's normal colour through it.</summary>
        private static readonly Color GodHalo = new Color(1.00f, 0.82f, 0.25f, 0.42f);

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

        /// <summary>Start predicting your own walk to a destination, at the speed the server last
        /// reported for you. Called the moment the order is given, not when the reply lands.</summary>
        public void PredictSelfMoveTo(float serverX, float serverY)
        {
            if (SelfId == Guid.Empty) return;
            if (!_views.TryGetValue(SelfId, out var view) || view == null) return;

            float speed = States.TryGetValue(SelfId, out var dto) ? dto.Speed : 0f;
            if (speed <= 0f) return;   // no speed known yet — let the server drive

            view.PredictTo(WorldMapper.ToUnity(serverX, serverY), speed * WorldMapper.Scale);
        }

        /// <summary>Abandon the prediction — for anything that stops you where you stand (an attack
        /// order, sitting, a cast). Without this the character would keep walking locally toward a
        /// destination the server has already discarded.</summary>
        /// <summary>Whether your own walk is still being predicted. False once it arrives, is cancelled,
        /// or the server turns out to be taking you somewhere else.</summary>
        public bool SelfIsPredicting =>
            SelfId != Guid.Empty && _views.TryGetValue(SelfId, out var v) && v != null && v.IsPredicting;

        public void CancelSelfPrediction()
        {
            if (SelfId != Guid.Empty && _views.TryGetValue(SelfId, out var view) && view != null)
                view.CancelPrediction();
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
            if (view.IsSelf) ApplySelfVisual(view);   // `BL-82` — survives a respawn of the marker
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
            // A SPHERE, not a quad: lit by nothing and drawn in a flat colour, a sphere reads as a
            // clean CIRCLE from any angle — which is what the top-down view wants, and it needs no
            // texture, no alpha and no transparency sorting to get round edges. The art pass will
            // replace it with a textured billboard anyway; until then this is the cheapest circle.
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = string.IsNullOrEmpty(e.Name) ? e.Kind.ToString() : e.Name;
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * EntityScale;
            go.transform.position = WorldMapper.ToUnity(e.X, e.Y) + Vector3.up * 0.75f;

            // Spheres come with a SphereCollider sized to the mesh. Now that the marker is small, that
            // is a small tap target too — so give it a deliberately generous one. Fingers are ~9mm.
            var fitted = go.GetComponent<Collider>();
            if (fitted != null) Destroy(fitted);
            var box = go.AddComponent<BoxCollider>();
            box.size = Vector3.one * (TapTargetScale / EntityScale);

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
