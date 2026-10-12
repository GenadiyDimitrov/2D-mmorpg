using System.Collections.Generic;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>What a scattered prop is. Client-only: the server never sees scenery.</summary>
    public enum PropKind { Tree = 0, Pine = 1, DeadTree = 2, Bush = 3, Rock = 4, Grass = 5, DryGrass = 6 }

    /// <summary>
    /// Trees, rocks and grass clumps (`BL-344` step 4, 2026-10-12). Cosmetic only: no collision, nothing
    /// sent to the server, and the server's world stays 2D.
    ///
    /// <para><b>Placement</b> is a pure function of world position, so a chunk always grows the same
    /// props, whichever way you walk in and however often it streams out and back. Each
    /// <see cref="CellSize"/> cell of a ground chunk rolls (from a hash of the cell) one candidate: a
    /// jittered spot, a biome picked from the same territory weights that colour the ground (so props
    /// mix across a border exactly where the colours do), a kind from that biome's table, and an
    /// acceptance roll against the biome's density × a low-frequency "forest" noise, which is what
    /// clumps trees into woods and clearings instead of spreading them evenly.</para>
    ///
    /// <para><b>Kept clear:</b> towns (and their rims), roads, the centre of every mob camp, NPCs
    /// standing outside a town, and the dungeon void below 0. Those are where the player looks and
    /// fights, and from the top-down camera a tree there would hide the thing you tap.</para>
    ///
    /// <para><b>Streaming</b> rides on <see cref="BiomeGround"/>: a chunk's props are rolled when its
    /// ground is built and dropped when it is pooled. <b>Drawing</b> is GPU instancing, gathered across
    /// every chunk in range, so the draw-call count is (prop meshes × their materials) — about a dozen —
    /// however many chunks are on screen. Props farther than the quality's range from the screen centre
    /// are not drawn.</para>
    ///
    /// <para><b>Models:</b> each kind draws the prefabs in <c>Resources/Props/&lt;Kind&gt;/</c> (built
    /// headlessly from the nature pack) and falls back to a primitive placeholder while that folder is
    /// empty, so the scatter can be judged before any art exists.</para>
    /// </summary>
    public class PropScatter : MonoBehaviour
    {
        public const float CellSize = 100f;   // server units; one candidate prop per cell

        // ── quality ─────────────────────────────────────────────────────────────────────────────

        /// <summary>Settings → Scenery. Off / Low / Medium / High: density × and draw range.</summary>
        public enum Level { Off = 0, Low = 1, Medium = 2, High = 3 }
        public const string Pref = "gfx.scenery";
        public static Level Quality = Level.High;
        private static readonly float[] DensityAt = { 0f, 0.35f, 0.65f, 1f };
        private static readonly float[] RangeAt = { 0f, 4000f, 6500f, 9000f };   // server units from the screen centre

        public static void LoadQuality() =>
            Quality = (Level)Mathf.Clamp(PlayerPrefs.GetInt(Pref, (int)Level.High), 0, 3);

        /// <summary>Raised by the Settings button. Every live chunk is re-rolled, because density decides
        /// WHICH props exist, not just how many are drawn.</summary>
        public static event System.Action QualityChanged;

        public static void SetQuality(Level q)
        {
            Quality = q;
            PlayerPrefs.SetInt(Pref, (int)q);
            QualityChanged?.Invoke();
        }

        // ── per-biome tables ────────────────────────────────────────────────────────────────────

        private readonly struct Table
        {
            public readonly float Density;          // chance a cell holds a prop, before clumping and quality
            public readonly PropKind[] Kinds;
            public readonly float[] Weights;
            public Table(float density, params (PropKind k, float w)[] rows)
            {
                Density = density;
                Kinds = new PropKind[rows.Length];
                Weights = new float[rows.Length];
                for (int i = 0; i < rows.Length; i++) { Kinds[i] = rows[i].k; Weights[i] = rows[i].w; }
            }
        }

        /// <summary>Indexed by <see cref="GroundBiome"/>. Woodland where the ground is green, pines up
        /// high and in the snow, dead trees and stone where it burned. Density is per 100×100 cell.</summary>
        private static readonly Table[] Tables =
        {
            /* Meadow   */ new Table(0.22f, (PropKind.Tree, 30), (PropKind.Bush, 15), (PropKind.Rock, 8), (PropKind.Grass, 47)),
            /* Moor     */ new Table(0.16f, (PropKind.Pine, 10), (PropKind.Bush, 15), (PropKind.Rock, 25), (PropKind.Grass, 50)),
            /* Ash      */ new Table(0.10f, (PropKind.DeadTree, 30), (PropKind.Rock, 55), (PropKind.DryGrass, 15)),
            /* Marsh    */ new Table(0.22f, (PropKind.Tree, 15), (PropKind.DeadTree, 15), (PropKind.Bush, 25), (PropKind.Grass, 45)),
            /* Highland */ new Table(0.18f, (PropKind.Pine, 35), (PropKind.Rock, 35), (PropKind.DryGrass, 30)),
            /* Snow     */ new Table(0.14f, (PropKind.Pine, 60), (PropKind.Rock, 40)),
            /* Lava     */ new Table(0.08f, (PropKind.DeadTree, 20), (PropKind.Rock, 80)),
        };

        private static bool IsTall(PropKind k) => k == PropKind.Tree || k == PropKind.Pine || k == PropKind.DeadTree;

        /// <summary>Base height of each kind in Unity units (a player is ~1.6). The roll varies it ±25%.</summary>
        private static float BaseSize(PropKind k)
        {
            switch (k)
            {
                case PropKind.Tree: return 4.2f;
                case PropKind.Pine: return 4.8f;
                case PropKind.DeadTree: return 3.4f;
                case PropKind.Bush: return 1.0f;
                case PropKind.Rock: return 0.9f;
                default: return 0.55f;   // grass clumps
            }
        }

        // ── clearances (server units) ───────────────────────────────────────────────────────────

        private const float TownMargin = 250f;      // past the town radius: its paved rim
        private const float RoadClear = 380f;       // from the road's centre line (the road is 600 wide)
        private const float CampCentre = 0.55f;     // × camp radius: the middle where the fighting is
        private const float NpcClear = 250f;

        // ── state ───────────────────────────────────────────────────────────────────────────────

        private sealed class Variant
        {
            public PropKind Kind;
            public Mesh Mesh;
            public Material[] Materials;      // one per submesh
            public Matrix4x4 Local;           // the part's own transform inside its prefab, normalised to height 1
            public readonly List<Matrix4x4> Gathered = new List<Matrix4x4>();
        }

        private struct Placed { public int Kind; public Matrix4x4 M; public Vector2 At; }

        /// <summary>A prop's model is one or more parts; every part of the chosen model gets the same
        /// instance matrix, so a tree whose bark and leaves are separate meshes stays one tree.</summary>
        private readonly List<Variant>[] _parts = new List<Variant>[7];     // per kind: all parts of all its models
        private readonly List<int>[] _modelOf = new List<int>[7];           // per kind: part index → model index
        private readonly int[] _modelCount = new int[7];

        private readonly Dictionary<Vector2Int, List<Placed>> _chunks = new Dictionary<Vector2Int, List<Placed>>();
        private readonly Stack<List<Placed>> _pool = new Stack<List<Placed>>();
        private readonly List<Variant> _all = new List<Variant>();
        private readonly Matrix4x4[] _batch = new Matrix4x4[1023];
        private Material _placeholderMaterial;
        private bool _linear;

        /// <summary>Set by <see cref="BiomeGround"/>: re-roll every live chunk (quality change).</summary>
        public System.Action<PropScatter> RerollAll;

        private void Awake()
        {
            _linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            LoadQuality();
            LoadModels();
            QualityChanged += OnQualityChanged;
        }

        private void OnDestroy() => QualityChanged -= OnQualityChanged;

        private void OnQualityChanged()
        {
            foreach (var list in _chunks.Values) { list.Clear(); _pool.Push(list); }
            _chunks.Clear();
            RerollAll?.Invoke(this);
        }

        // ── chunk lifecycle (called by BiomeGround) ─────────────────────────────────────────────

        public void ChunkBuilt(Vector2Int key)
        {
            var list = _pool.Count > 0 ? _pool.Pop() : new List<Placed>();
            if (Quality != Level.Off) Roll(key, list);
            _chunks[key] = list;
        }

        public void ChunkReleased(Vector2Int key)
        {
            if (!_chunks.TryGetValue(key, out var list)) return;
            list.Clear();
            _pool.Push(list);
            _chunks.Remove(key);
        }

        // ── placement ───────────────────────────────────────────────────────────────────────────

        private readonly List<Vector3> _circles = new List<Vector3>();   // x, y, radius of every keep-clear disc touching this chunk

        private void Roll(Vector2Int key, List<Placed> into)
        {
            float ox = key.x * BiomeGround.ChunkSize, oy = key.y * BiomeGround.ChunkSize;
            if (ox + BiomeGround.ChunkSize <= 0f || oy + BiomeGround.ChunkSize <= 0f) return;   // all void
            GatherClearings(ox, oy);
            float density = DensityAt[(int)Quality];
            int cells = (int)(BiomeGround.ChunkSize / CellSize);
            int gx0 = Mathf.RoundToInt(ox / CellSize), gy0 = Mathf.RoundToInt(oy / CellSize);

            for (int j = 0; j < cells; j++)
                for (int i = 0; i < cells; i++)
                {
                    int gx = gx0 + i, gy = gy0 + j;
                    float x = (gx + 0.1f + 0.8f * Rand(gx, gy, 1)) * CellSize;
                    float y = (gy + 0.1f + 0.8f * Rand(gx, gy, 2)) * CellSize;
                    if (x < 0f || y < 0f) continue;

                    var table = Tables[(int)BiomeGround.PickBiome(x, y, Rand(gx, gy, 3))];
                    var kind = PickKind(table, Rand(gx, gy, 4));
                    float p = table.Density * density * Clumping(kind, x, y);
                    if (Rand(gx, gy, 5) >= p) continue;
                    if (Blocked(x, y, kind)) continue;

                    int models = _modelCount[(int)kind];
                    int model = Mathf.Min(models - 1, (int)(Rand(gx, gy, 6) * models));
                    float size = BaseSize(kind) * (0.75f + 0.5f * Rand(gx, gy, 7));
                    float yaw = Rand(gx, gy, 8) * 360f;
                    var m = Matrix4x4.TRS(WorldMapper.ToUnity(x, y), Quaternion.Euler(0f, yaw, 0f), Vector3.one * size);
                    into.Add(new Placed { Kind = (int)kind | (model << 8), M = m, At = new Vector2(x, y) });
                }
        }

        private static PropKind PickKind(in Table t, float r)
        {
            float total = 0f;
            foreach (var w in t.Weights) total += w;
            r *= total;
            for (int i = 0; i < t.Kinds.Length; i++)
            {
                if (r < t.Weights[i]) return t.Kinds[i];
                r -= t.Weights[i];
            }
            return t.Kinds[t.Kinds.Length - 1];
        }

        /// <summary>Trees gather into woods (a 1500-unit noise: dense where it is high, near-bare where it
        /// is low); rocks into outcrops on a finer one; grass is spread evenly.</summary>
        private static float Clumping(PropKind kind, float x, float y)
        {
            if (IsTall(kind) || kind == PropKind.Bush)
            {
                float f = BiomeGround.Noise(x / 1500f + 3.7f, y / 1500f + 9.1f);
                return 0.15f + 2.1f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.8f, f));
            }
            if (kind == PropKind.Rock)
                return 0.4f + 1.4f * BiomeGround.Noise(x / 600f + 51.3f, y / 600f + 7.7f);
            return 1f;
        }

        private void GatherClearings(float ox, float oy)
        {
            _circles.Clear();
            float cx0 = ox, cy0 = oy, cx1 = ox + BiomeGround.ChunkSize, cy1 = oy + BiomeGround.ChunkSize;
            void Add(float x, float y, float r)
            {
                if (x + r < cx0 || x - r > cx1 || y + r < cy0 || y - r > cy1) return;
                _circles.Add(new Vector3(x, y, r));
            }
            foreach (var t in Towns.All) Add(t.X, t.Y, t.Radius + TownMargin);
            foreach (var z in WorldMap.SpawnZones) Add(z.X, z.Y, z.Radius * CampCentre);
            foreach (var n in WorldMap.Npcs) Add(n.X, n.Y, NpcClear);
        }

        private bool Blocked(float x, float y, PropKind kind)
        {
            // Grass may creep a little nearer than the rest: it hides nothing.
            float shrink = kind == PropKind.Grass || kind == PropKind.DryGrass ? 0.75f : 1f;
            foreach (var c in _circles)
            {
                float dx = c.x - x, dy = c.y - y, r = c.z * shrink;
                if (dx * dx + dy * dy < r * r) return true;
            }
            foreach (var road in WorldMap.Roads)
                if (road.DistanceTo(x, y) < RoadClear * shrink) return true;
            return false;
        }

        /// <summary>A hash of the cell and a salt, in [0,1). Same cell, same answer, on every phone.</summary>
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

        // ── drawing ─────────────────────────────────────────────────────────────────────────────

        private void LateUpdate()
        {
            if (Quality == Level.Off || _chunks.Count == 0) return;
            var cam = Camera.main;
            if (cam == null) return;
            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float d)) return;
            var centre = WorldMapper.ToServer(ray.GetPoint(d));
            float range = RangeAt[(int)Quality], range2 = range * range;
            float half = BiomeGround.ChunkSize * 0.5f;

            foreach (var v in _all) v.Gathered.Clear();
            foreach (var kv in _chunks)
            {
                // Whole chunks in or out by their nearest point, then each prop by its own distance.
                float nx = Mathf.Clamp(centre.x, kv.Key.x * BiomeGround.ChunkSize, kv.Key.x * BiomeGround.ChunkSize + 2f * half);
                float ny = Mathf.Clamp(centre.y, kv.Key.y * BiomeGround.ChunkSize, kv.Key.y * BiomeGround.ChunkSize + 2f * half);
                if ((nx - centre.x) * (nx - centre.x) + (ny - centre.y) * (ny - centre.y) > range2) continue;
                foreach (var p in kv.Value)
                {
                    if ((p.At - centre).sqrMagnitude > range2) continue;
                    int kind = p.Kind & 0xFF, model = p.Kind >> 8;
                    var parts = _parts[kind];
                    var owners = _modelOf[kind];
                    for (int i = 0; i < parts.Count; i++)
                        if (owners[i] == model) parts[i].Gathered.Add(p.M * parts[i].Local);
                }
            }

            foreach (var v in _all)
            {
                int n = v.Gathered.Count;
                if (n == 0) continue;
                for (int sub = 0; sub < v.Materials.Length; sub++)
                {
                    var rp = new RenderParams(v.Materials[sub])
                    {
                        shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                        receiveShadows = false,
                        worldBounds = new Bounds(WorldMapper.ToUnity(centre.x, centre.y), Vector3.one * (range * WorldMapper.Scale * 2f + 20f)),
                    };
                    for (int start = 0; start < n; start += _batch.Length)
                    {
                        int count = Mathf.Min(_batch.Length, n - start);
                        v.Gathered.CopyTo(start, _batch, 0, count);
                        Graphics.RenderMeshInstanced(rp, v.Mesh, sub, _batch, count);
                    }
                }
            }
        }

        // ── models ──────────────────────────────────────────────────────────────────────────────

        private void LoadModels()
        {
            for (int k = 0; k < _parts.Length; k++)
            {
                _parts[k] = new List<Variant>();
                _modelOf[k] = new List<int>();
                var kind = (PropKind)k;
                var prefabs = Resources.LoadAll<GameObject>("Props/" + kind);
                int model = 0;
                foreach (var prefab in prefabs)
                    if (AddPrefab(kind, prefab, model)) model++;
                if (model == 0) { AddPlaceholder(kind); model = 1; }
                _modelCount[k] = model;
            }
            int real = 0;
            for (int k = 0; k < _parts.Length; k++) if (_parts[k].Count > 0 && _parts[k][0].Materials[0] != _placeholderMaterial) real++;
            Debug.Log("[PropScatter] " + real + " of " + _parts.Length + " prop kinds have models; the rest are placeholders.");
        }

        /// <summary>Every mesh in the prefab becomes a part, its transform taken relative to the root and
        /// scaled so the whole model stands 1 unit tall with its base on y = 0. The roll's size then
        /// means "height in Unity units", whatever scale the source file was authored at.</summary>
        private bool AddPrefab(PropKind kind, GameObject prefab, int model)
        {
            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) return false;

            var root = prefab.transform.worldToLocalMatrix;
            var bounds = new Bounds();
            bool first = true;
            foreach (var f in filters)
            {
                if (f.sharedMesh == null) continue;
                var b = TransformBounds(root * f.transform.localToWorldMatrix, f.sharedMesh.bounds);
                if (first) { bounds = b; first = false; } else bounds.Encapsulate(b);
            }
            if (first || bounds.size.y <= 0f) return false;
            var norm = Matrix4x4.Scale(Vector3.one / bounds.size.y) * Matrix4x4.Translate(new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z));

            foreach (var f in filters)
            {
                var mr = f.GetComponent<MeshRenderer>();
                if (f.sharedMesh == null || mr == null) continue;
                var mats = new Material[f.sharedMesh.subMeshCount];
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mr.sharedMaterials.Length > 0 ? mr.sharedMaterials[Mathf.Min(i, mr.sharedMaterials.Length - 1)] : null;
                    mats[i] = src != null ? Instanced(src) : PlaceholderMaterial();
                }
                AddPart(kind, model, f.sharedMesh, mats, norm * root * f.transform.localToWorldMatrix);
            }
            return true;
        }

        private readonly Dictionary<Material, Material> _instanced = new Dictionary<Material, Material>();

        /// <summary>A copy with instancing switched on, shared by every part that used the original.</summary>
        private Material Instanced(Material src)
        {
            if (_instanced.TryGetValue(src, out var m)) return m;
            m = new Material(src) { enableInstancing = true };
            _instanced[src] = m;
            return m;
        }

        private void AddPart(PropKind kind, int model, Mesh mesh, Material[] mats, Matrix4x4 local)
        {
            var v = new Variant { Kind = kind, Mesh = mesh, Materials = mats, Local = local };
            _parts[(int)kind].Add(v);
            _modelOf[(int)kind].Add(model);
            _all.Add(v);
        }

        private static Bounds TransformBounds(Matrix4x4 m, Bounds b)
        {
            var result = new Bounds(m.MultiplyPoint3x4(b.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                result.Encapsulate(m.MultiplyPoint3x4(corner));
            }
            return result;
        }

        // ── placeholders: primitive shapes, vertex-coloured, 1 unit tall ────────────────────────

        private Material PlaceholderMaterial()
        {
            if (_placeholderMaterial != null) return _placeholderMaterial;
            // A shipped ASSET with instancing on, not only a runtime material: the build strips shader
            // variants no shipped material uses (GraphicsSettings "Strip Unused"), and a material made at
            // runtime is invisible to that check, so the instanced variant would be missing on the phone.
            var shipped = Resources.Load<Material>("Props/PropPlaceholder");
            if (shipped != null) return _placeholderMaterial = shipped;
            var shader = Resources.Load<Shader>("Shaders/UnlitVertexColor") ?? Shader.Find("L2Clone/UnlitVertexColor") ?? Shader.Find("Sprites/Default");
            _placeholderMaterial = new Material(shader) { enableInstancing = true };
            return _placeholderMaterial;
        }

        private void AddPlaceholder(PropKind kind)
        {
            var bark = new Color(0.30f, 0.21f, 0.13f);
            var parts = new List<(Mesh mesh, Matrix4x4 m, Color c)>();
            switch (kind)
            {
                case PropKind.Tree:
                    parts.Add((Cylinder, TRS(0f, 0.2f, 0f, 0.08f, 0.2f, 0.08f), bark));
                    parts.Add((Sphere, TRS(0f, 0.65f, 0f, 0.62f, 0.7f, 0.62f), new Color(0.16f, 0.36f, 0.12f)));
                    break;
                case PropKind.Pine:
                    parts.Add((Cylinder, TRS(0f, 0.12f, 0f, 0.06f, 0.12f, 0.06f), bark));
                    parts.Add((Cone, TRS(0f, 0.15f, 0f, 0.42f, 0.85f, 0.42f), new Color(0.10f, 0.26f, 0.16f)));
                    break;
                case PropKind.DeadTree:
                    var grey = new Color(0.27f, 0.24f, 0.21f);
                    parts.Add((Cylinder, TRS(0f, 0.45f, 0f, 0.06f, 0.45f, 0.06f), grey));
                    parts.Add((Cylinder, Matrix4x4.TRS(new Vector3(0.12f, 0.65f, 0f), Quaternion.Euler(0f, 0f, -40f), new Vector3(0.035f, 0.18f, 0.035f)), grey));
                    parts.Add((Cylinder, Matrix4x4.TRS(new Vector3(-0.1f, 0.55f, 0.04f), Quaternion.Euler(20f, 0f, 45f), new Vector3(0.03f, 0.15f, 0.03f)), grey));
                    break;
                case PropKind.Bush:
                    parts.Add((Sphere, TRS(0f, 0.45f, 0f, 1.2f, 0.9f, 1.1f), new Color(0.19f, 0.31f, 0.13f)));
                    break;
                case PropKind.Rock:
                    parts.Add((Sphere, Matrix4x4.TRS(new Vector3(0f, 0.45f, 0f), Quaternion.Euler(8f, 30f, 12f), new Vector3(1.4f, 0.9f, 1.1f)), new Color(0.42f, 0.41f, 0.39f)));
                    break;
                default:
                    var blade = kind == PropKind.DryGrass ? new Color(0.48f, 0.42f, 0.25f) : new Color(0.27f, 0.45f, 0.17f);
                    for (int b = 0; b < 5; b++)
                    {
                        float a = b * 72f * Mathf.Deg2Rad, r = b == 0 ? 0f : 0.22f;
                        parts.Add((Cone, Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), Quaternion.Euler(b == 0 ? 0f : 12f, b * 72f, 0f),
                                                       new Vector3(0.09f, b == 0 ? 1f : 0.8f, 0.09f)), blade));
                    }
                    break;
            }

            var combine = new CombineInstance[parts.Count];
            for (int i = 0; i < parts.Count; i++)
                combine[i] = new CombineInstance { mesh = Coloured(parts[i].mesh, parts[i].c), transform = parts[i].m };
            var mesh = new Mesh { name = "Placeholder " + kind };
            mesh.CombineMeshes(combine, true, true);
            mesh.RecalculateBounds();
            AddPart(kind, 0, mesh, new[] { PlaceholderMaterial() }, Matrix4x4.identity);
        }

        private static Matrix4x4 TRS(float x, float y, float z, float sx, float sy, float sz) =>
            Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.identity, new Vector3(sx, sy, sz));

        private Mesh Coloured(Mesh src, Color c)
        {
            var copy = Instantiate(src);
            var verts = copy.vertices;
            var cols = new Color[verts.Length];
            var lin = _linear ? c.linear : c;
            for (int i = 0; i < cols.Length; i++)
            {
                // A little top-lit shading so a sphere reads as round without any lighting yet.
                float shade = 0.75f + 0.25f * Mathf.Clamp01(verts[i].y + 0.5f);
                cols[i] = lin * shade;
                cols[i].a = 1f;
            }
            copy.colors = cols;
            return copy;
        }

        private static Mesh _sphere, _cylinder, _cone;
        private static Mesh Sphere => _sphere != null ? _sphere : (_sphere = PrimitiveMesh(PrimitiveType.Sphere));
        private static Mesh Cylinder => _cylinder != null ? _cylinder : (_cylinder = PrimitiveMesh(PrimitiveType.Cylinder));

        private static Mesh PrimitiveMesh(PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Destroy(go);
            return mesh;
        }

        /// <summary>An 8-sided cone, base on y = 0, apex at y = 1, radius 1. Unity has no cone primitive.</summary>
        private static Mesh Cone
        {
            get
            {
                if (_cone != null) return _cone;
                const int n = 8;
                var v = new Vector3[n + 2];
                var t = new int[n * 6];
                for (int i = 0; i < n; i++)
                {
                    float a = i * Mathf.PI * 2f / n;
                    v[i] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                }
                v[n] = new Vector3(0f, 1f, 0f);
                v[n + 1] = Vector3.zero;
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    t[i * 6 + 0] = i; t[i * 6 + 1] = n; t[i * 6 + 2] = j;
                    t[i * 6 + 3] = j; t[i * 6 + 4] = n + 1; t[i * 6 + 5] = i;
                }
                _cone = new Mesh { name = "Cone", vertices = v, triangles = t };
                _cone.RecalculateNormals();
                _cone.RecalculateBounds();
                return _cone;
            }
        }
    }
}
