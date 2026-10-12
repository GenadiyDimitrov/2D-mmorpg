using System.Collections.Generic;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// The ground (`BL-344` step 3, 2026-10-12): flat, vertex-coloured, streamed in chunks around what the
    /// camera can see. It replaces the old single dark plane (which covered only the 0-24k quarter of the
    /// 48k world, hidden by a matching clear colour), the 1000-unit wireframe grid, and the flat
    /// level-coloured field fills.
    ///
    /// <para><b>Colour</b> comes from <see cref="Biomes.Sites"/>: each point mixes the biomes of the
    /// nearest territories, weighted by how much farther each is than the nearest one, so neighbouring
    /// biomes fade into each other over ~2000 units instead of meeting at a seam. Towns paint paving over
    /// that with a soft rim. Two layers of value noise give every biome patches, which is also what the
    /// eye moves against now that the grid is gone. The noise is a function of world position, so chunk
    /// edges sample the same values and there are no seams between chunks either.</para>
    ///
    /// <para><b>Chunks</b> are <see cref="ChunkSize"/> server units square with a vertex every
    /// <see cref="VertexStep"/>. Only chunks under the camera's view (plus one ring) exist; the rest go
    /// back to a pool. That is what keeps this valid for an open world many times today's size (`BL-346`):
    /// the cost follows the screen, never the map.</para>
    ///
    /// <para>Cosmetic only, and FLAT at y = 0: taps still resolve against the mathematical y = 0 plane in
    /// <see cref="TouchInput"/>, and the server never knows this exists.</para>
    /// </summary>
    public class BiomeGround : MonoBehaviour
    {
        public const float ChunkSize = 2000f;     // server units per chunk side
        public const float VertexStep = 100f;     // server units between vertices
        private const int Quads = (int)(ChunkSize / VertexStep);
        private const int Side = Quads + 1;

        /// <summary>No chunk is built farther than this from the screen centre, however far the camera
        /// zooms out or how low it tilts toward the horizon.</summary>
        public float MaxRange = 16000f;

        /// <summary>Chunks built per frame. A full screen at the widest zoom is ~120 chunks, so the
        /// first frames fill in over about a quarter of a second rather than in one hitch.</summary>
        public int BuildsPerFrame = 8;

        /// <summary>How far past the nearest territory another one still blends in, in server units.</summary>
        private const float Blend = 700f;

        private sealed class Chunk { public GameObject Go; public Mesh Mesh; }

        private readonly Dictionary<Vector2Int, Chunk> _live = new Dictionary<Vector2Int, Chunk>();
        private readonly Stack<Chunk> _pool = new Stack<Chunk>();
        private readonly List<Vector2Int> _wanted = new List<Vector2Int>();
        private readonly List<Vector2Int> _drop = new List<Vector2Int>();
        private readonly Color[] _colours = new Color[Side * Side];
        private Vector3[] _verts;
        private int[] _tris;
        private Material _material;
        private bool _linear;
        private PropScatter _props;

        private void Start()
        {
            _linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            _material = CreateMaterial();
            BuildSharedGeometry();
            PaintTheVoid();

            // Step 4: the scenery rides on these chunks (rolled when one is built, dropped when it is pooled).
            _props = gameObject.AddComponent<PropScatter>();
            _props.RerollAll = p => { foreach (var key in _live.Keys) p.ChunkBuilt(key); };
        }

        private void Update()
        {
            var cam = Camera.main;
            if (cam == null || _material == null) return;
            if (!VisibleRect(cam, out float minX, out float minY, out float maxX, out float maxY)) return;

            int cx0 = Mathf.FloorToInt(minX / ChunkSize) - 1, cx1 = Mathf.FloorToInt(maxX / ChunkSize) + 1;
            int cy0 = Mathf.FloorToInt(minY / ChunkSize) - 1, cy1 = Mathf.FloorToInt(maxY / ChunkSize) + 1;

            // Release what is two rings outside the view (one ring of slack, so panning back and forth
            // across a chunk edge does not rebuild the same chunk every frame).
            _drop.Clear();
            foreach (var key in _live.Keys)
                if (key.x < cx0 - 1 || key.x > cx1 + 1 || key.y < cy0 - 1 || key.y > cy1 + 1) _drop.Add(key);
            foreach (var key in _drop)
            {
                var c = _live[key];
                c.Go.SetActive(false);
                _pool.Push(c);
                _live.Remove(key);
                if (_props != null) _props.ChunkReleased(key);
            }

            // Build the missing ones, nearest the screen centre first.
            _wanted.Clear();
            for (int y = cy0; y <= cy1; y++)
                for (int x = cx0; x <= cx1; x++)
                {
                    var key = new Vector2Int(x, y);
                    if (!_live.ContainsKey(key)) _wanted.Add(key);
                }
            if (_wanted.Count == 0) return;

            float midX = (cx0 + cx1 + 1) * 0.5f, midY = (cy0 + cy1 + 1) * 0.5f;
            _wanted.Sort((a, b) =>
                ((a.x + 0.5f - midX) * (a.x + 0.5f - midX) + (a.y + 0.5f - midY) * (a.y + 0.5f - midY))
                .CompareTo((b.x + 0.5f - midX) * (b.x + 0.5f - midX) + (b.y + 0.5f - midY) * (b.y + 0.5f - midY)));

            int n = Mathf.Min(BuildsPerFrame, _wanted.Count);
            for (int i = 0; i < n; i++) BuildChunk(_wanted[i]);
        }

        /// <summary>The server-space rectangle the camera sees on the y = 0 plane, clamped to
        /// <see cref="MaxRange"/> around the screen centre. A corner ray that never meets the ground (a
        /// tilt above the horizon) counts as the range limit in that direction.</summary>
        private bool VisibleRect(Camera cam, out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = minY = float.MaxValue; maxX = maxY = float.MinValue;
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out float cd)) return false;
            var centre = WorldMapper.ToServer(cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).GetPoint(cd));

            for (int i = 0; i < 4; i++)
            {
                var ray = cam.ViewportPointToRay(new Vector3(i & 1, i >> 1, 0f));
                Vector2 p;
                if (plane.Raycast(ray, out float d)) p = WorldMapper.ToServer(ray.GetPoint(d));
                else
                {
                    var flat = new Vector3(ray.direction.x, 0f, ray.direction.z).normalized;
                    p = centre + new Vector2(flat.x, -flat.z) * MaxRange;
                }
                p.x = Mathf.Clamp(p.x, centre.x - MaxRange, centre.x + MaxRange);
                p.y = Mathf.Clamp(p.y, centre.y - MaxRange, centre.y + MaxRange);
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            return true;
        }

        private void BuildChunk(Vector2Int key)
        {
            var chunk = _pool.Count > 0 ? _pool.Pop() : NewChunk();
            float ox = key.x * ChunkSize, oy = key.y * ChunkSize;

            for (int j = 0; j < Side; j++)
                for (int i = 0; i < Side; i++)
                {
                    var c = ColourAt(ox + i * VertexStep, oy + j * VertexStep);
                    _colours[j * Side + i] = _linear ? c.linear : c;
                }
            chunk.Mesh.colors = _colours;

            chunk.Go.name = "Ground " + key.x + "," + key.y;
            chunk.Go.transform.position = WorldMapper.ToUnity(ox, oy);
            chunk.Go.SetActive(true);
            _live[key] = chunk;
            if (_props != null) _props.ChunkBuilt(key);
        }

        private Chunk NewChunk()
        {
            var mesh = new Mesh { name = "GroundChunk" };
            mesh.vertices = _verts;
            mesh.triangles = _tris;
            mesh.colors = _colours;
            mesh.RecalculateBounds();

            var go = new GameObject("Ground chunk");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return new Chunk { Go = go, Mesh = mesh };
        }

        /// <summary>Every chunk shares one vertex layout in its own local space; only the colours differ.
        /// Server Y grows downward and maps to -Z (see <see cref="WorldMapper"/>), hence the minus.</summary>
        private void BuildSharedGeometry()
        {
            float s = VertexStep * WorldMapper.Scale;
            _verts = new Vector3[Side * Side];
            for (int j = 0; j < Side; j++)
                for (int i = 0; i < Side; i++)
                    _verts[j * Side + i] = new Vector3(i * s, 0f, -j * s);

            _tris = new int[Quads * Quads * 6];
            int t = 0;
            for (int j = 0; j < Quads; j++)
                for (int i = 0; i < Quads; i++)
                {
                    int a = j * Side + i, b = a + 1, c = a + Side, d = c + 1;
                    _tris[t++] = a; _tris[t++] = b; _tris[t++] = c;
                    _tris[t++] = b; _tris[t++] = d; _tris[t++] = c;
                }
        }

        // ── colour ──────────────────────────────────────────────────────────────────────────────

        /// <summary>Below the overworld (negative coordinates) is where the dungeons and the jail live:
        /// a dark stone void, with each dungeon's own floor drawn on top by GameUi.</summary>
        private static readonly Color Deep = new Color(0.09f, 0.09f, 0.10f);
        private static readonly Color Paving = new Color(0.37f, 0.35f, 0.31f);

        /// <summary>The ground colour at a server position. Public so the minimap or anything else that
        /// wants "what the ground looks like here" reads the same answer.</summary>
        public static Color ColourAt(float x, float y)
        {
            float n1 = 0.65f * ValueNoise(x / 900f, y / 900f) + 0.35f * ValueNoise((0.8f * x + 0.6f * y) / 300f + 17.3f, (0.8f * y - 0.6f * x) / 300f + 5.1f);   // rotated, so the two octaves never share a lattice
            float n2 = ValueNoise(x / 170f + 41.7f, y / 170f + 23.9f);
            float shade = 0.93f + 0.14f * n2;

            if (x < 0f || y < 0f) return Deep * shade;

            // Distance-weighted mix of the nearest territories.
            var sites = Biomes.Sites;
            float best = float.MaxValue;
            var dist = SiteDist;
            if (dist == null || dist.Length != sites.Length) dist = SiteDist = new float[sites.Length];
            for (int i = 0; i < sites.Length; i++)
            {
                float dx = sites[i].X - x, dy = sites[i].Y - y;
                dist[i] = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist[i] < best) best = dist[i];
            }
            Color sum = Color.black;
            float wsum = 0f;
            for (int i = 0; i < sites.Length; i++)
            {
                float extra = dist[i] - best;
                if (extra > Blend * 5f) continue;
                float w = Mathf.Exp(-extra / Blend);
                sum += BiomeColour(sites[i].Biome, n1) * w;
                wsum += w;
            }
            var col = wsum > 0f ? sum / wsum : BiomeColour(GroundBiome.Meadow, n1);

            // Towns pave over it, fading out across their rim.
            foreach (var town in Towns.All)
            {
                float dx = town.X - x, dy = town.Y - y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > town.Radius + 200f) continue;
                float t = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(town.Radius - 300f, town.Radius + 200f, d));
                col = Color.Lerp(col, Paving * (0.95f + 0.1f * n1), t * 0.9f);
            }

            col *= shade;
            col.a = 1f;
            return col;
        }

        [System.ThreadStatic] private static float[] SiteDist;

        /// <summary>One biome for a point, drawn from the SAME territory weights <see cref="ColourAt"/>
        /// blends: <paramref name="r"/> in [0,1) picks in proportion to each site's weight. A scattered
        /// prop calls it, so across a border the props mix exactly where the colours do.</summary>
        public static GroundBiome PickBiome(float x, float y, float r)
        {
            var sites = Biomes.Sites;
            if (sites.Length == 0) return GroundBiome.Meadow;
            var dist = SiteDist;
            if (dist == null || dist.Length != sites.Length) dist = SiteDist = new float[sites.Length];
            float best = float.MaxValue;
            for (int i = 0; i < sites.Length; i++)
            {
                float dx = sites[i].X - x, dy = sites[i].Y - y;
                dist[i] = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist[i] < best) best = dist[i];
            }
            float wsum = 0f;
            for (int i = 0; i < sites.Length; i++)
            {
                float extra = dist[i] - best;
                dist[i] = extra > Blend * 5f ? 0f : Mathf.Exp(-extra / Blend);   // reuse the buffer for weights
                wsum += dist[i];
            }
            r *= wsum;
            for (int i = 0; i < sites.Length; i++)
            {
                if (r < dist[i]) return sites[i].Biome;
                r -= dist[i];
            }
            return sites[sites.Length - 1].Biome;
        }

        /// <summary>The ground's value noise, for anything that wants its patches in step with it.</summary>
        public static float Noise(float x, float y) => ValueNoise(x, y);

        /// <summary>Each biome is two colours mixed by the noise: the base and its patches. Lava's patches
        /// are a hard threshold, so the glow reads as cracks and pools in dark rock, not as an orange wash.
        /// Values stay mid-dark (the snow aside) so models and nameplates keep reading against the floor.</summary>
        private static Color BiomeColour(GroundBiome biome, float n)
        {
            switch (biome)
            {
                case GroundBiome.Moor: return Color.Lerp(new Color(0.30f, 0.30f, 0.23f), new Color(0.37f, 0.33f, 0.24f), n);
                case GroundBiome.Ash: return Color.Lerp(new Color(0.27f, 0.25f, 0.24f), new Color(0.18f, 0.17f, 0.17f), n);
                case GroundBiome.Marsh: return Color.Lerp(new Color(0.16f, 0.22f, 0.15f), new Color(0.23f, 0.21f, 0.13f), n);
                case GroundBiome.Highland: return Color.Lerp(new Color(0.38f, 0.27f, 0.20f), new Color(0.29f, 0.23f, 0.18f), n);
                case GroundBiome.Snow: return Color.Lerp(new Color(0.71f, 0.74f, 0.79f), new Color(0.57f, 0.62f, 0.70f), n);
                case GroundBiome.Lava:
                    return Color.Lerp(new Color(0.13f, 0.10f, 0.09f), new Color(0.78f, 0.25f, 0.06f),
                                      Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.64f, 0.71f, n)));
                default: return Color.Lerp(new Color(0.21f, 0.33f, 0.16f), new Color(0.30f, 0.40f, 0.18f), n);   // Meadow
            }
        }

        /// <summary>Smooth value noise in [0,1]: hashed lattice values, smoothstep-interpolated.</summary>
        private static float ValueNoise(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(ix, iy), b = Hash(ix + 1, iy), c = Hash(ix, iy + 1), d = Hash(ix + 1, iy + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        // ── material + clear colour ─────────────────────────────────────────────────────────────

        /// <summary>The vertex-colour shader ships from Resources; Sprites/Default (also vertex-coloured,
        /// always shipped) is the fallback, so a missing shader degrades instead of going magenta.</summary>
        private static Material CreateMaterial()
        {
            var shader = Resources.Load<Shader>("Shaders/UnlitVertexColor")
                         ?? Shader.Find("L2Clone/UnlitVertexColor")
                         ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("[BiomeGround] no vertex-colour shader in this build; the ground will not draw.");
                return null;
            }
            Debug.Log("[BiomeGround] using shader '" + shader.name + "'.");
            return new Material(shader) { renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry };
        }

        /// <summary>
        /// The camera clears to a dark neutral, never Unity's default blue-grey (playtest 29: the grey
        /// rectangle at full ortho zoom-out was the camera's clear colour showing past the old plane).
        /// The chunks now follow the view, so the clear colour should only flash for a frame while they
        /// fill in. The scene background's alpha is 0, which some Android targets composite as black,
        /// hence the explicit opaque colour.
        /// </summary>
        private static void PaintTheVoid()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(Deep.r, Deep.g, Deep.b, 1f);
        }
    }
}
