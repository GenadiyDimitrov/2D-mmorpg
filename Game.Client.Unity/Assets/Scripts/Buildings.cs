using System.Collections.Generic;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// `BL-356` — the towns stop being flat: a 3D building on every footprint <see cref="TownLayout"/> already draws, and a
    /// hut or two in some of the woods. Cosmetic and client-only, like <see cref="PropScatter"/>: the server never sees a
    /// building, nothing collides (his `BL-319` answer 1 — walls are visual until `BL-323`), and moving a town or a lot
    /// in Game.Shared moves its building with it, because every placement here is a pure function of that data.
    ///
    /// <para><b>The models</b> are <c>Resources/Buildings/&lt;Name&gt;_&lt;W&gt;x&lt;D&gt;</c>, assembled headlessly from the
    /// Quaternius Medieval Village kit by <c>ModelSetup.BuildBuildings</c>, one baked mesh each. The name's
    /// <c>WxD</c> is the WALL footprint in metres (the roof overhangs it), front door on +Z — that suffix is the whole
    /// contract between the two halves.</para>
    ///
    /// <para><b>Fitting a lot:</b> the door faces the NPC who stands at it (<see cref="WorldMap.Npcs"/>), or else the
    /// nearest road or path. Each candidate model for the lot's kind is scaled to fit inside the lot; the one that
    /// comes out LARGEST wins (a small lot picks a small house rather than a shrunken hall), with a hash choosing among
    /// the near-ties so a street is not one house repeated. A model may stretch up to <see cref="MaxStretch"/> on one
    /// axis to fill an oblong lot; its height follows the smaller scale so a stretch never makes it tower.</para>
    ///
    /// <para>⚠ The lots are SMALL next to a character — a house lot is ~3.5 × 3 Unity units against a ~1.6-unit body —
    /// so most buildings come out at 0.6-0.8 of the kit's real size. That is the owner's plan as drawn; the map
    /// reorganisation he has queued is where lots grow, and nothing here needs to change when they do.</para>
    /// </summary>
    public class Buildings : MonoBehaviour
    {
        /// <summary>One building in the world.</summary>
        public readonly struct Placed
        {
            public readonly string Model;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Vector3 Scale;
            public readonly bool Hut;
            public Placed(string model, Vector3 position, Quaternion rotation, Vector3 scale, bool hut)
            { Model = model; Position = position; Rotation = rotation; Scale = scale; Hut = hut; }
        }

        /// <summary>The candidate models per lot kind (the <c>WxD</c> suffix is read off the name).</summary>
        private static readonly Dictionary<LotKind, string[]> ByKind = new Dictionary<LotKind, string[]>
        {
            [LotKind.House] = new[] { "Cottage_4x4", "Townhouse_4x4", "Stonehouse_4x6", "Longhouse_4x8" },
            [LotKind.Shop] = new[] { "Shop_6x6", "Townhouse_4x4", "Stonehouse_4x6" },
            [LotKind.Church] = new[] { "Chapel_4x8" },
            [LotKind.Craft] = new[] { "Hall_6x10", "Shop_6x6" },
            [LotKind.Guild] = new[] { "Guild_8x8", "Shop_6x6" },
            [LotKind.Shrine] = new[] { "Pavilion_4x4" },
            [LotKind.Yard] = new[] { "Yard_4x4" },
        };

        public const string HutModel = "Hut_4x4";

        private const float Inset = 30f;          // server units kept clear inside each lot edge
        private const float MaxStretch = 1.3f;    // one axis may outgrow the other by this much
        private const float HutScale = 0.8f;      // a hut has no lot; this is its size against the kit's metres

        // ── the plan (pure; also read by PropScatter and the editor preview) ─────────────────────

        private static List<Placed> _all;

        /// <summary>Every building and hut in the world, computed once.</summary>
        public static IReadOnlyList<Placed> All => _all ??= Plan();

        private static List<Vector2> _huts;

        /// <summary>The huts' positions in SERVER units, for <see cref="PropScatter"/> to keep its trees off.</summary>
        public static IReadOnlyList<Vector2> HutSpots
        {
            get
            {
                if (_huts != null) return _huts;
                _huts = new List<Vector2>();
                foreach (var (x, y) in HutPlaces()) _huts.Add(new Vector2(x, y));
                return _huts;
            }
        }

        private static List<Placed> Plan()
        {
            var list = new List<Placed>();
            foreach (var plan in TownLayout.Plans)
                foreach (var lot in plan.Lots)
                {
                    var p = FitLot(plan, lot);
                    if (p.HasValue) list.Add(p.Value);
                }
            foreach (var (x, y) in HutPlaces())
            {
                float yaw = Rand(Mathf.RoundToInt(x), Mathf.RoundToInt(y), 31) * 360f;
                list.Add(new Placed(HutModel, WorldMapper.ToUnity(x, y), Quaternion.Euler(0f, yaw, 0f), Vector3.one * HutScale, true));
            }
            return list;
        }

        /// <summary>The <c>W</c> and <c>D</c> of a model name ending in <c>_WxD</c>, in metres.</summary>
        public static Vector2 Footprint(string model)
        {
            int us = model.LastIndexOf('_'), x = model.LastIndexOf('x');
            if (us < 0 || x < us) return new Vector2(4f, 4f);
            float.TryParse(model.Substring(us + 1, x - us - 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float w);
            float.TryParse(model.Substring(x + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float d);
            return new Vector2(w > 0f ? w : 4f, d > 0f ? d : 4f);
        }

        private static Placed? FitLot(TownPlan plan, TownLot lot)
        {
            if (!ByKind.TryGetValue(lot.Kind, out var models)) return null;

            // The lot's own axes, in server space (+Y south): local x along W, local y along H.
            float a = lot.AngleDeg * Mathf.Deg2Rad;
            var ax = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var ay = new Vector2(-Mathf.Sin(a), Mathf.Cos(a));
            var centre = new Vector2(lot.Centre.X, lot.Centre.Y);

            // Which side the door goes on: toward its NPC, else toward the nearest road or path.
            var toward = DoorTarget(plan, lot, centre, ax, ay) - centre;
            float lx = Vector2.Dot(toward, ax), ly = Vector2.Dot(toward, ay);
            // The building's front is +Z. Unity yaw = the lot's angle (server +Y is Unity -Z, so the map's clockwise
            // turn is Unity's positive yaw); the side offset turns +Z toward the door. A front on a local-x side lays
            // the building's width along the lot's H.
            // A long, narrow lot (a church arm) takes its door on a SHORT end, whichever is nearer: on a long side the
            // model's width would lie across the lot and shrink to fit its narrow depth.
            if (lot.W >= 1.8f * lot.H) ly = 0f;
            else if (lot.H >= 1.8f * lot.W) lx = 0f;
            float side; bool across;
            if (Mathf.Abs(lx) > Mathf.Abs(ly)) { side = lx > 0f ? 90f : 270f; across = true; }
            else { side = ly > 0f ? 180f : 0f; across = false; }
            float availW = (across ? lot.H : lot.W) - 2f * Inset, availD = (across ? lot.W : lot.H) - 2f * Inset;
            availW *= WorldMapper.Scale; availD *= WorldMapper.Scale;
            if (availW <= 0f || availD <= 0f) return null;

            // The largest fit wins; a hash picks among the near-ties.
            float best = 0f;
            var fits = new (string m, float sx, float sz, float s)[models.Length];
            for (int i = 0; i < models.Length; i++)
            {
                var f = Footprint(models[i]);
                float sx = availW / f.x, sz = availD / f.y, s = Mathf.Min(sx, sz);
                fits[i] = (models[i], Mathf.Min(sx, s * MaxStretch), Mathf.Min(sz, s * MaxStretch), s);
                best = Mathf.Max(best, s);
            }
            int near = 0;
            foreach (var f in fits) if (f.s >= best * 0.85f) near++;
            int pick = Mathf.Min(near - 1, (int)(Rand(Mathf.RoundToInt(lot.Centre.X), Mathf.RoundToInt(lot.Centre.Y), 17) * near));
            foreach (var f in fits)
            {
                if (f.s < best * 0.85f) continue;
                if (pick-- > 0) continue;
                return new Placed(f.m, WorldMapper.ToUnity(centre.x, centre.y),
                                  Quaternion.Euler(0f, lot.AngleDeg + side, 0f), new Vector3(f.sx, f.s, f.sz), false);
            }
            return null;
        }

        /// <summary>The NPC who stands at this lot's door (outside it, within 400 of its edge), else the nearest point
        /// of the town's roads and paths.</summary>
        private static Vector2 DoorTarget(TownPlan plan, TownLot lot, Vector2 centre, Vector2 ax, Vector2 ay)
        {
            float reach = Mathf.Max(lot.W, lot.H) * 0.5f + 400f;
            float bestD = float.MaxValue;
            Vector2 best = default;
            foreach (var n in WorldMap.Npcs)
            {
                var d = new Vector2(n.X, n.Y) - centre;
                if (d.magnitude > reach) continue;
                bool inside = Mathf.Abs(Vector2.Dot(d, ax)) < lot.W * 0.5f && Mathf.Abs(Vector2.Dot(d, ay)) < lot.H * 0.5f;
                if (inside) continue;   // the anvil, in its own yard
                if (d.sqrMagnitude < bestD) { bestD = d.sqrMagnitude; best = new Vector2(n.X, n.Y); }
            }
            if (bestD < float.MaxValue) return best;

            bestD = float.MaxValue;
            foreach (var r in plan.Roads)
            {
                var p = Closest(new Vector2(r.A.X, r.A.Y), new Vector2(r.B.X, r.B.Y), centre);
                float d = (p - centre).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
            return bestD < float.MaxValue ? best : centre + Vector2.up;
        }

        private static Vector2 Closest(Vector2 a, Vector2 b, Vector2 p)
        {
            var ab = b - a;
            float t = ab.sqrMagnitude < 1e-3f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return a + ab * t;
        }

        // ── huts ──────────────────────────────────────────────────────────────────────────────

        private const float HutTownClear = 800f, HutCampClear = 400f, HutRoadClear = 700f, HutNpcClear = 600f, HutApart = 3000f;

        /// <summary>One or two huts in SOME wooded fields (the owner: "one two huts in some forests just as scenery").
        /// Per field a hash decides 0, 1 or 2; each is the most wooded of 40 hashed spots inside the field that keeps
        /// clear of towns, camps, roads and NPCs, and only where it really is a wood (the same forest noise that clumps
        /// <see cref="PropScatter"/>'s trees). Never on ash or lava.</summary>
        private static IEnumerable<(float x, float y)> HutPlaces()
        {
            int fi = 0;
            foreach (var field in RegionMap.Fields)
            {
                fi++;
                if (field.Outline.Length < 3 || field.MinX < 0f || field.MinY < 0f) continue;   // dungeons
                float roll = Rand(fi, 0, 41);
                int want = roll < 0.4f ? 0 : roll < 0.85f ? 1 : 2;
                var chosen = new List<Vector2>();
                for (int k = 0; k < want; k++)
                {
                    float bestF = 0.58f;   // below this it is not a wood
                    Vector2? best = null;
                    for (int c = 0; c < 40; c++)
                    {
                        float x = Mathf.Lerp(field.MinX, field.MaxX, Rand(fi, c + 100 * k, 42));
                        float y = Mathf.Lerp(field.MinY, field.MaxY, Rand(fi, c + 100 * k, 43));
                        if (!field.Contains(x, y)) continue;
                        float f = BiomeGround.Noise(x / 1500f + 3.7f, y / 1500f + 9.1f);
                        if (f <= bestF || !HutClear(x, y, chosen)) continue;
                        var biome = BiomeGround.PickBiome(x, y, 0.5f);
                        if (biome == GroundBiome.Ash || biome == GroundBiome.Lava) continue;
                        bestF = f;
                        best = new Vector2(x, y);
                    }
                    if (best == null) break;
                    chosen.Add(best.Value);
                    yield return (best.Value.x, best.Value.y);
                }
            }
        }

        private static bool HutClear(float x, float y, List<Vector2> chosen)
        {
            foreach (var c in chosen)
                if ((c - new Vector2(x, y)).sqrMagnitude < HutApart * HutApart) return false;
            foreach (var t in Towns.All)
                if (Sq(t.X - x, t.Y - y) < Sq(t.Radius + HutTownClear)) return false;
            foreach (var z in WorldMap.SpawnZones)
                if (Sq(z.X - x, z.Y - y) < Sq(z.Radius + HutCampClear)) return false;
            foreach (var n in WorldMap.Npcs)
                if (Sq(n.X - x, n.Y - y) < Sq(HutNpcClear)) return false;
            foreach (var r in WorldMap.Roads)
                if (r.DistanceTo(x, y) < HutRoadClear) return false;
            return true;
        }

        private static float Sq(float d) => d * d;
        private static float Sq(float dx, float dy) => dx * dx + dy * dy;

        /// <summary>A hash in [0,1) — the same one PropScatter uses, so every phone builds the same town.</summary>
        private static float Rand(int x, int y, int salt)
        {
            unchecked
            {
                uint h = (uint)x * 73856093u ^ (uint)y * 19349663u ^ (uint)salt * 83492791u;
                h = (h ^ (h >> 15)) * 2246822519u;
                h = (h ^ (h >> 13)) * 3266489917u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        // ── the scene ────────────────────────────────────────────────────────────────────────

        private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();
        private readonly List<GameObject> _hutObjects = new List<GameObject>();
        private readonly List<(GameObject go, Vector3 at, bool hut)> _objects = new List<(GameObject, Vector3, bool)>();
        private Vector3 _culledAt = new Vector3(float.MaxValue, 0f, 0f);

        /// <summary>The owner (2026-10-14): nothing past your horizon. A building is drawn only within the server's
        /// <see cref="GameConstants.ViewRange"/> of you — the same 3000 that decides which mobs and players you see.</summary>
        private const float DrawRange = GameConstants.ViewRange * WorldMapper.Scale;   // Unity units
        private const float RecullStep = 1f;   // Unity units the player moves before the set is re-checked
        private DayNight.Shadows _shadows = (DayNight.Shadows)(-1);
        private PropScatter.Level _scenery = (PropScatter.Level)(-1);

        private void Awake()
        {
            var cache = new Dictionary<string, GameObject>();
            int missing = 0;
            foreach (var p in All)
            {
                if (!cache.TryGetValue(p.Model, out var prefab))
                    cache[p.Model] = prefab = Resources.Load<GameObject>("Buildings/" + p.Model);
                if (prefab == null) { missing++; continue; }
                var go = Instantiate(prefab, p.Position, p.Rotation, transform);
                go.transform.localScale = p.Scale;
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) _renderers.Add(r);
                if (p.Hut) _hutObjects.Add(go);
                _objects.Add((go, p.Position, p.Hut));
            }
            Debug.Log("[Buildings] " + (All.Count - missing) + " placed (" + _hutObjects.Count + " huts)"
                      + (missing > 0 ? ", " + missing + " with no model" : "") + ".");
        }

        private void Update()
        {
            // Shadows follow the scenery rule (only on the top setting); huts are scenery, so Scenery Off hides them.
            // Town buildings stay: they are the town, like the footprints under them.
            if (_shadows != DayNight.ShadowQuality)
            {
                _shadows = DayNight.ShadowQuality;
                var mode = _shadows == DayNight.Shadows.All ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                foreach (var r in _renderers) if (r != null) r.shadowCastingMode = mode;
            }
            bool sceneryChanged = _scenery != PropScatter.Quality;
            _scenery = PropScatter.Quality;
            if (!CameraRig.Focus.HasValue)
            {
                if (sceneryChanged)
                    foreach (var h in _hutObjects) if (h != null) h.SetActive(_scenery != PropScatter.Level.Off);
                return;
            }

            // Within the view range of YOU, like the mobs; re-checked once you have moved a step, or the setting changed.
            var me = CameraRig.Focus.Value;
            me.y = 0f;
            if (!sceneryChanged && (me - _culledAt).sqrMagnitude < RecullStep * RecullStep) return;
            _culledAt = me;
            const float range2 = DrawRange * DrawRange;
            foreach (var (go, at, hut) in _objects)
            {
                if (go == null) continue;
                float dx = at.x - me.x, dz = at.z - me.z;
                bool show = dx * dx + dz * dz <= range2 && !(hut && _scenery == PropScatter.Level.Off);
                if (go.activeSelf != show) go.SetActive(show);
            }
        }
    }
}
