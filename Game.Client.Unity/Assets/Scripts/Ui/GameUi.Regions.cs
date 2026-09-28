using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: REGIONS — the transient "you entered X" notice (which replaced the always-on
    /// zone label; the HUD carries no permanent place text), and the region polygon OUTLINES drawn on
    /// the ground so the map reads as authored shapes instead of scattered circles.
    ///
    /// The outlines are governed by the same toggle as the zone colours (owner: that toggle will also
    /// govern region polygons). They're static, so they're built once from RegionMap.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private Image _regionToastBg;
        private TextMeshProUGUI _regionToast;
        private float _regionToastBorn = -99f;
        private const float RegionToastSeconds = 3.5f;

        private GameObject _regionOutlines;

        private void BuildRegionUi()
        {
            _regionToastBg = UiKit.Box(_root, "RegionToast", new Color(0.05f, 0.06f, 0.09f, 0.72f));
            UiKit.Place(UiKit.Rect(_regionToastBg.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -110f), new Vector2(760f, 56f));
            _regionToast = UiKit.Label(_regionToastBg.transform, "", 28f, UiKit.Text, TextAlignmentOptions.Center);
            _regionToast.fontStyle = FontStyles.Bold;
            UiKit.Stretch(UiKit.Rect(_regionToast.gameObject), 8f, 4f, 8f, 4f);

            // The banner must NOT eat touches. It sits centre-top over open ground, and as a plain
            // Image + text it was a raycast target, so every tap that landed on it was swallowed —
            // "the 'you entered a field' message prevents me clicking below my char". It is a notice,
            // never a control, so nothing about it should ever be interactive.
            _regionToastBg.raycastTarget = false;
            _regionToast.raycastTarget = false;

            _regionToastBg.gameObject.SetActive(false);

            BuildRegionOutlines();
            BuildWorldBorder();
            BuildJailBorder();
            BuildTownPlans();
        }

        /// <summary>`BL-319` — the towns drawn flat on the ground: the world roads, each town's main roads, plaza
        /// and side paths, the building footprints, and the wall with a gap at every gate. All of it is read from
        /// <see cref="TownLayout"/> and <see cref="WorldMap.Roads"/>, the same data the server places its NPCs
        /// and guards from, so a door and the person standing at it cannot disagree.
        ///
        /// <para>Always on — it is the town, not a map overlay. Walls are VISUAL (his answer 1): you walk through
        /// them until the 3D models (`BL-281`) and a server-side collision check (`BL-323`) exist.</para>
        ///
        /// <para>⚠ ONE mesh per colour, not one object per building: ~100 footprints in five towns would be ~200
        /// renderers, and the dashed world border already taught us what a renderer-per-piece costs on the phone.
        /// The layers sit at 0.035-0.045 — above the grid (0.02) and the town fill (0.03), under the region
        /// outlines (0.06) and the move marker (0.07).</para></summary>
        private void BuildTownPlans()
        {
            var root = new GameObject("TownPlans");
            var layers = new System.Collections.Generic.Dictionary<string, FlatLayer>();
            FlatLayer L(string key, Color col, float y)
            {
                if (!layers.TryGetValue(key, out var layer)) layers[key] = layer = new FlatLayer(col, y);
                return layer;
            }

            var road = L("road", new Color(0.36f, 0.31f, 0.24f), 0.035f);
            var path = L("path", new Color(0.29f, 0.25f, 0.19f), 0.036f);
            var plaza = L("plaza", new Color(0.42f, 0.37f, 0.28f), 0.037f);
            var wall = L("wall", new Color(0.42f, 0.46f, 0.48f), 0.038f);
            var edge = L("edge", new Color(0.10f, 0.12f, 0.13f), 0.040f);

            // The world roads first: a 300-wide band (the 600 in RoadPath is how far mobs are kept off it).
            foreach (var r in WorldMap.Roads)
                for (int i = 0; i < r.Points.Length - 1; i++)
                {
                    var a = new Vec2(r.Points[i].X, r.Points[i].Y);
                    var b = new Vec2(r.Points[i + 1].X, r.Points[i + 1].Y);
                    road.Strip(a, b, 300f);
                    road.Disc(b, 150f, 12);                         // round the elbow at each gate
                }

            foreach (var plan in TownLayout.Plans)
            {
                foreach (var s in plan.Roads)
                    (s.Path ? path : road).Strip(s.A, s.B, s.Width);
                plaza.Disc(plan.Centre, plan.PlazaRadius, 32);

                foreach (var lot in plan.Lots)
                {
                    // An edge rect under a slightly smaller fill: a crisp outline with no LineRenderer.
                    edge.Rect(lot.Centre, lot.W, lot.H, lot.AngleDeg);
                    L("lot" + (int)lot.Kind, LotColour(lot.Kind), 0.042f)
                        .Rect(lot.Centre, lot.W - 36f, lot.H - 36f, lot.AngleDeg);
                }

                // The wall: the town octagon (RegionMap.Town — corners on the circle, flat sides at 0.924·r),
                // with a 320 gap at the middle of every side that holds a gate.
                var town = Towns.ById(plan.TownId);
                if (town == null) continue;
                for (int k = 0; k < 8; k++)
                {
                    float a0 = Mathf.PI / 8f + (k - 1) * Mathf.PI / 4f, a1 = a0 + Mathf.PI / 4f;
                    var p0 = new Vec2(town.X + town.Radius * Mathf.Cos(a0), town.Y + town.Radius * Mathf.Sin(a0));
                    var p1 = new Vec2(town.X + town.Radius * Mathf.Cos(a1), town.Y + town.Radius * Mathf.Sin(a1));
                    var mid = new Vec2((p0.X + p1.X) / 2f, (p0.Y + p1.Y) / 2f);
                    bool gated = false;
                    foreach (var g in plan.Gates)
                        if ((g.Centre.X - mid.X) * (g.Centre.X - mid.X) + (g.Centre.Y - mid.Y) * (g.Centre.Y - mid.Y) < 200f * 200f)
                            gated = true;
                    if (!gated) { wall.Strip(p0, p1, 50f); continue; }
                    float len = Mathf.Sqrt((p1.X - p0.X) * (p1.X - p0.X) + (p1.Y - p0.Y) * (p1.Y - p0.Y));
                    float dx = (p1.X - p0.X) / len, dy = (p1.Y - p0.Y) / len;
                    wall.Strip(p0, new Vec2(mid.X - dx * 160f, mid.Y - dy * 160f), 50f);
                    wall.Strip(new Vec2(mid.X + dx * 160f, mid.Y + dy * 160f), p1, 50f);
                }
            }

            foreach (var layer in layers.Values)
                layer.Build(root.transform);
        }

        private static Color LotColour(LotKind kind) => kind switch
        {
            LotKind.Shop => new Color(0.33f, 0.42f, 0.50f),
            LotKind.Church => new Color(0.48f, 0.32f, 0.44f),
            LotKind.Craft => new Color(0.52f, 0.37f, 0.22f),
            LotKind.Yard => new Color(0.31f, 0.25f, 0.17f),
            LotKind.Guild => new Color(0.27f, 0.45f, 0.37f),
            LotKind.Shrine => new Color(0.58f, 0.55f, 0.36f),
            _ => new Color(0.30f, 0.33f, 0.35f),               // House
        };

        /// <summary>One colour's worth of flat ground shapes, gathered in SERVER coordinates and mapped through
        /// <see cref="WorldMapper"/> vertex by vertex (so the Y flip is never re-derived here), then built as ONE
        /// double-sided unlit mesh.</summary>
        private sealed class FlatLayer
        {
            private readonly Color _col;
            private readonly float _y;
            private readonly System.Collections.Generic.List<Vector3> _v = new System.Collections.Generic.List<Vector3>();
            private readonly System.Collections.Generic.List<int> _t = new System.Collections.Generic.List<int>();

            public FlatLayer(Color col, float y) { _col = col; _y = y; }

            private int Add(float x, float y)
            {
                var u = WorldMapper.ToUnity(x, y);
                u.y = _y;
                _v.Add(u);
                return _v.Count - 1;
            }

            private void Tri(int a, int b, int c)
            {
                _t.Add(a); _t.Add(b); _t.Add(c);
                _t.Add(a); _t.Add(c); _t.Add(b);                  // both windings: never culled
            }

            private void Quad(float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy)
            {
                int a = Add(ax, ay), b = Add(bx, by), c = Add(cx, cy), d = Add(dx, dy);
                Tri(a, b, c); Tri(a, c, d);
            }

            /// <summary>A strip of <paramref name="width"/> from a to b.</summary>
            public void Strip(Vec2 a, Vec2 b, float width)
            {
                float dx = b.X - a.X, dy = b.Y - a.Y, len = Mathf.Sqrt(dx * dx + dy * dy);
                if (len < 1f) return;
                float nx = -dy / len * width / 2f, ny = dx / len * width / 2f;
                Quad(a.X + nx, a.Y + ny, b.X + nx, b.Y + ny, b.X - nx, b.Y - ny, a.X - nx, a.Y - ny);
            }

            /// <summary>A rectangle centred on c, w × h, turned <paramref name="angleDeg"/> the way
            /// <see cref="TownLayout.At"/> turns a town (server axes).</summary>
            public void Rect(Vec2 c, float w, float h, float angleDeg)
            {
                float t = angleDeg * Mathf.Deg2Rad, cs = Mathf.Cos(t), sn = Mathf.Sin(t);
                float hw = w / 2f, hh = h / 2f;
                float X(float lx, float ly) => c.X + lx * cs - ly * sn;
                float Y(float lx, float ly) => c.Y + lx * sn + ly * cs;
                Quad(X(-hw, -hh), Y(-hw, -hh), X(hw, -hh), Y(hw, -hh), X(hw, hh), Y(hw, hh), X(-hw, hh), Y(-hw, hh));
            }

            public void Disc(Vec2 c, float r, int segments)
            {
                int centre = Add(c.X, c.Y), first = _v.Count;
                for (int i = 0; i < segments; i++)
                {
                    float a = i * 2f * Mathf.PI / segments;
                    Add(c.X + r * Mathf.Cos(a), c.Y + r * Mathf.Sin(a));
                }
                for (int i = 0; i < segments; i++)
                    Tri(centre, first + i, first + (i + 1) % segments);
            }

            public void Build(Transform parent)
            {
                if (_v.Count == 0) return;
                var mesh = new Mesh { name = "town_" + _y };
                // A single layer can pass 65k vertices (four vertices per shape); lift the 16-bit index cap.
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(_v);
                mesh.SetTriangles(_t, 0);
                mesh.RecalculateBounds();
                var go = new GameObject(mesh.name);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.material = UnlitMaterials.Create(_col);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        private GameObject _worldBorder;
        private GameObject _jailBorder;

        /// <summary>The edge of the world, as an orange DASHED rectangle on the ground (owner: "like the
        /// jail's orange dashed line — just for reference").
        ///
        /// ⚠ REWRITTEN after the 0.28.78 device playtest, where the log flooded with per-frame yellow
        /// warnings. The first version created ONE Material PER DASH — ~192 of them — all always on, and
        /// that (a LineRenderer × material-instance per-frame render warning, ×192) is the prime suspect.
        /// Now: ONE shared material for every dash, far fewer/bigger dashes, and — critically — the
        /// border rides the SAME zone-colours toggle as the region outlines, so it is OFF by default.
        /// That guarantees the flood stops (nothing renders when off) and lets the owner confirm the
        /// diagnosis by toggling. If it needs to be always-on again, the material must first be proven
        /// not to warn per frame.</summary>
        private void BuildWorldBorder()
        {
            const float dash = 1500f, gap = 1200f, y = 0.08f;   // bigger dashes → far fewer renderers
            var colour = new Color(0.95f, 0.55f, 0.15f, 0.75f);

            _worldBorder = new GameObject("WorldBorder");
            var sharedMat = new Material(UnlitMaterials.Shader) { color = colour };   // ONE material, not one-per-dash
            float w = GameConstants.ZoneWidth, h = GameConstants.ZoneHeight;

            void Edge(float x0, float z0, float x1, float z1) =>
                DashedEdge(_worldBorder, sharedMat, colour, 8f, dash, gap, y, x0, z0, x1, z1);

            Edge(0f, 0f, w,  0f);
            Edge(w,  0f, w,  h);
            Edge(w,  h,  0f, h);
            Edge(0f, h,  0f, 0f);

            _worldBorder.SetActive(false);   // off until the zone-colours toggle turns it on
        }

        /// <summary>One dashed straight segment on the ground, as a run of 2-point LineRenderers sharing
        /// ONE material. Shared by the world border and the jail ring — the 0.28.78 device playtest's
        /// per-frame render-warning flood was a material PER DASH, so there is exactly one place that
        /// creates these and it always takes the material from its caller.</summary>
        private static void DashedEdge(GameObject parent, Material mat, Color colour, float width,
                                       float dash, float gap, float y,
                                       float x0, float z0, float x1, float z1)
        {
            float len = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0));
            if (len <= 0f) return;
            float dx = (x1 - x0) / len, dz = (z1 - z0) / len;
            for (float t = 0f; t < len; t += dash + gap)
            {
                float e = Mathf.Min(t + dash, len);
                var go = new GameObject("Dash");
                go.transform.SetParent(parent.transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.widthMultiplier = width;
                lr.sharedMaterial = mat;
                lr.startColor = lr.endColor = colour;
                lr.positionCount = 2;
                var a = WorldMapper.ToUnity(x0 + dx * t, z0 + dz * t); a.y = y;
                var b = WorldMapper.ToUnity(x0 + dx * e, z0 + dz * e); b.y = y;
                lr.SetPosition(0, a);
                lr.SetPosition(1, b);
            }
        }

        /// <summary>The JAIL's wall, drawn (playtest-11 item 1 / `B9`).
        ///
        /// <para>The cell has always been enforced — a jailed player, and an admin visiting one, are both
        /// clamped to <see cref="WorldDomain.Jail"/> — but nothing on screen said where it ended, so the
        /// clamp read as "the game keeps yanking me". Same orange dashed language as the world border,
        /// because it means the same thing: this is the end, you cannot go further.</para>
        ///
        /// <para>Unlike the world border this is NOT on the map-overlay toggle. The world rectangle is
        /// 24000 units of reference you look up once; this is a wall you are standing against, and it is
        /// only ever four dashed edges that render while you are inside — which is also why it cannot
        /// bring back the 0.28.78 renderer flood. Dungeon boxes deliberately get no ring: their bounding
        /// box is not the polygon the map already draws, so a rectangle there would contradict the
        /// coloured outline rather than explain it.</para>
        ///
        /// <para>It was a dashed CIRCLE until the owner asked for a room (playtest-20 `61d`); the jail is
        /// now a 300 × 500 box domain, and the wall is drawn as the four walls it actually is.</para></summary>
        private void BuildJailBorder()
        {
            const float y = 0.09f;
            var colour = new Color(0.95f, 0.55f, 0.15f, 0.85f);
            var jail = WorldDomain.Jail;

            _jailBorder = new GameObject("JailBorder");
            var sharedMat = new Material(UnlitMaterials.Shader) { color = colour };

            // The four walls of the yard, each dashed along its own length.
            const float dash = 40f, gap = 26f;
            DashedEdge(_jailBorder, sharedMat, colour, 4f, dash, gap, y, jail.MinX, jail.MinY, jail.MaxX, jail.MinY);
            DashedEdge(_jailBorder, sharedMat, colour, 4f, dash, gap, y, jail.MaxX, jail.MinY, jail.MaxX, jail.MaxY);
            DashedEdge(_jailBorder, sharedMat, colour, 4f, dash, gap, y, jail.MaxX, jail.MaxY, jail.MinX, jail.MaxY);
            DashedEdge(_jailBorder, sharedMat, colour, 4f, dash, gap, y, jail.MinX, jail.MaxY, jail.MinX, jail.MinY);

            _jailBorder.SetActive(false);   // shown only while you are actually in the yard
        }

        /// <summary>Show the transient region banner. Called from the server's Region push.</summary>
        public void ShowRegionNotice(RegionNotice r)
        {
            if (r == null) return;
            string band = r.MaxLevel > 0 ? "   (Lv " + r.MinLevel + "-" + r.MaxLevel + ")" : "";
            ShowToast("You entered " + r.Name + band);
        }

        /// <summary>Show any transient centre-top banner (region entry, the 3h "take a break" nudge, …).
        /// Reuses the region toast slot; the newest message wins and re-arms the fade.</summary>
        public void ShowToast(string text)
        {
            if (_regionToastBg == null || string.IsNullOrEmpty(text)) return;
            _regionToast.text = text;
            _regionToastBorn = Time.unscaledTime;
            _regionToastBg.gameObject.SetActive(true);
        }

        private void RefreshRegionUi()
        {
            // Fade + hide the banner.
            if (_regionToastBg != null && _regionToastBg.gameObject.activeSelf)
            {
                float age = (Time.unscaledTime - _regionToastBorn) / RegionToastSeconds;
                if (age >= 1f) _regionToastBg.gameObject.SetActive(false);
                else
                {
                    float a = age < 0.65f ? 1f : Mathf.Lerp(1f, 0f, (age - 0.65f) / 0.35f);
                    var bc = _regionToastBg.color; bc.a = 0.72f * a; _regionToastBg.color = bc;
                    var tc = _regionToast.color;   tc.a = a;         _regionToast.color = tc;
                }
            }

            // Outlines follow the zone-colours toggle (same control governs both, per the owner).
            if (_regionOutlines != null)
            {
                bool show = Boot.Zones != null && Boot.Zones.gameObject.activeSelf
                            && Boot.Phase == ClientPhase.InWorld;
                if (_regionOutlines.activeSelf != show) _regionOutlines.SetActive(show);
            }

            // The world border now rides the SAME toggle as the region outlines (was: always-on). This
            // is the flood mitigation from the 0.28.78 playtest — off by default, so its LineRenderers
            // don't render (and can't warn) unless the map overlay is on. Re-evaluate once the per-frame
            // warning is confirmed fixed.
            if (_worldBorder != null)
            {
                bool show = Boot.Zones != null && Boot.Zones.gameObject.activeSelf
                            && Boot.Phase == ClientPhase.InWorld;
                if (_worldBorder.activeSelf != show) _worldBorder.SetActive(show);
            }

            // The jail ring shows itself: it is on exactly while you STAND in the cell — serving a
            // sentence, or an admin who teleported in to talk to an inmate. No toggle, because a wall
            // you are pressed against is not map reference, it is the thing stopping you.
            if (_jailBorder != null)
            {
                bool show = Boot.Phase == ClientPhase.InWorld
                            && Boot.Entities != null
                            && Boot.Entities.TryGetState(Boot.SelfId, out var self)
                            && WorldDomain.Jail.Contains(self.X, self.Y);
                if (_jailBorder.activeSelf != show) _jailBorder.SetActive(show);
            }
        }

        private void BuildRegionOutlines()
        {
            _regionOutlines = new GameObject("RegionOutlines");
            foreach (var region in RegionMap.All)
            {
                if (region.Outline == null || region.Outline.Length < 3) continue;

                // FIELDS get a filled polygon coloured by their LEVEL band — the field colour that
                // replaces the spawn-zone circles. TOWNS get a neutral fill drawn ON TOP (higher y), which
                // masks the field colour under a town so it reads as an island/lake in the field — the
                // "donut" look without a donut polygon. Gameplay containment is separate (At() = town first).
                if (region.Kind == RegionKind.Field)
                    BuildRegionFill(region, ColourForLevel(RegionMap.LevelBand(region.Id)?.Max ?? 1), 0.02f);
                else
                    BuildRegionFill(region, new Color(0.28f, 0.30f, 0.35f), 0.03f);   // town island: calm slate

                var go = new GameObject(region.Id);
                go.transform.SetParent(_regionOutlines.transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.loop = true;
                lr.widthMultiplier = 0.6f;
                lr.material = new Material(UnlitMaterials.Shader);   // IL2CPP-safe (no magenta on device)
                Color col = region.Kind == RegionKind.Town
                    ? new Color(0.20f, 0.42f, 0.68f, 0.55f)          // towns: muted steel-blue (owner: less blue/lighter)
                    : new Color(1.00f, 0.90f, 0.55f, 0.90f);         // fields: a bright rim over the fill
                lr.startColor = lr.endColor = col;

                lr.positionCount = region.Outline.Length;
                for (int i = 0; i < region.Outline.Length; i++)
                {
                    var u = WorldMapper.ToUnity(region.Outline[i].X, region.Outline[i].Y);
                    u.y = 0.06f;
                    lr.SetPosition(i, u);
                }
            }
            _regionOutlines.SetActive(false);
        }

        /// <summary>A flat filled polygon on the ground at height <paramref name="height"/>, made
        /// double-sided so it shows regardless of winding. Fields use their level colour at a low y;
        /// towns use a neutral fill at a higher y so they mask the field beneath them (the island look).
        ///
        /// ⚠ This used to be a TRIANGLE FAN from vertex 0, on the stated grounds that "the outlines are
        /// convex". They are not, and since 2026-08-24 they are emphatically not: a dungeon is now a
        /// corridor with side rooms off it (`DungeonLayout`), and a fan across that shape fills in every
        /// gap between two rooms — the map would draw a solid blob where the walls are, with only the
        /// LineRenderer rim hinting at the real outline. Ear clipping costs a few dozen lines and is
        /// correct for any simple polygon, convex ones included.</summary>
        private void BuildRegionFill(Region region, Color col, float height)
        {
            var poly = region.Outline;
            var verts = new Vector3[poly.Length];
            for (int i = 0; i < poly.Length; i++)
            {
                var u = WorldMapper.ToUnity(poly[i].X, poly[i].Y);
                u.y = height;                    // fields low (0.02), towns higher (0.03) to mask the field
                verts[i] = u;
            }

            // Emit each triangle in BOTH windings so back-face culling can never hide it.
            var ears = Triangulate(poly);
            var tris = new int[ears.Count * 2];
            for (int i = 0; i < ears.Count; i += 3)
            {
                int a = ears[i], b = ears[i + 1], c = ears[i + 2];
                tris[i * 2 + 0] = a; tris[i * 2 + 1] = b; tris[i * 2 + 2] = c;
                tris[i * 2 + 3] = a; tris[i * 2 + 4] = c; tris[i * 2 + 5] = b;
            }

            var mesh = new Mesh { name = region.Id + "_fill" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            var go = new GameObject(region.Id + "_fill");
            go.transform.SetParent(_regionOutlines.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = UnlitMaterials.Create(col);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        /// <summary>EAR CLIPPING — triangulate a simple polygon into index triples into its own vertex
        /// array. O(n²) and run ONCE per region at startup on outlines of a few dozen points, so the
        /// simple algorithm is the right one; the alternative was a fan that is only correct for the
        /// convex case the world stopped being.
        ///
        /// <para>Works in WORLD coordinates (the region's own Vec2s), not the mapped Unity ones, so the
        /// winding test is not at the mercy of whatever axis flip <c>WorldMapper</c> applies. The
        /// resulting indices address the same slots either way.</para>
        ///
        /// <para>Degenerate input never hangs it: if no ear can be found — which a self-intersecting
        /// outline would cause — it falls back to a fan over whatever is left, so a bad polygon draws
        /// something slightly wrong instead of freezing the map build.</para></summary>
        private static System.Collections.Generic.List<int> Triangulate(Vec2[] poly)
        {
            var tris = new System.Collections.Generic.List<int>((poly.Length - 2) * 3);
            if (poly.Length < 3) return tris;

            // Work on a ring of indices, wound counter-clockwise so "convex" has one meaning below.
            var ring = new System.Collections.Generic.List<int>(poly.Length);
            for (int i = 0; i < poly.Length; i++) ring.Add(i);
            if (SignedArea(poly) < 0f) ring.Reverse();

            int guard = ring.Count * ring.Count;
            while (ring.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < ring.Count; i++)
                {
                    int ia = ring[(i + ring.Count - 1) % ring.Count], ib = ring[i], ic = ring[(i + 1) % ring.Count];
                    Vec2 a = poly[ia], b = poly[ib], c = poly[ic];
                    if (Cross(a, b, c) <= 0f) continue;              // reflex corner — not an ear

                    bool contains = false;
                    foreach (int j in ring)
                    {
                        if (j == ia || j == ib || j == ic) continue;
                        if (InTriangle(poly[j], a, b, c)) { contains = true; break; }
                    }
                    if (contains) continue;                          // another vertex is inside it

                    tris.Add(ia); tris.Add(ib); tris.Add(ic);
                    ring.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped) break;                                 // no ear found: bail to the fan below
            }

            for (int i = 1; i < ring.Count - 1; i++)
            {
                tris.Add(ring[0]); tris.Add(ring[i]); tris.Add(ring[i + 1]);
            }
            return tris;
        }

        private static float SignedArea(Vec2[] poly)
        {
            float sum = 0f;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                sum += (poly[j].X * poly[i].Y) - (poly[i].X * poly[j].Y);
            return sum * 0.5f;
        }

        private static float Cross(Vec2 a, Vec2 b, Vec2 c) =>
            (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        private static bool InTriangle(Vec2 p, Vec2 a, Vec2 b, Vec2 c)
        {
            // Strictly inside, and >= 0 on the edges so a vertex lying exactly on one still blocks the
            // ear — clipping through it would produce a zero-area sliver.
            float d1 = Cross(a, b, p), d2 = Cross(b, c, p), d3 = Cross(c, a, p);
            return d1 >= 0f && d2 >= 0f && d3 >= 0f;
        }

        /// <summary>Green (low) → yellow → red (high), matching ZoneOverlay's disc colours so a field and
        /// a nameplate of the same level read the same.</summary>
        private static Color ColourForLevel(int level)
        {
            float t = Mathf.Clamp01(level / 80f);
            return t < 0.5f
                ? Color.Lerp(new Color(0.25f, 0.55f, 0.25f), new Color(0.70f, 0.68f, 0.20f), t * 2f)
                : Color.Lerp(new Color(0.70f, 0.68f, 0.20f), new Color(0.65f, 0.20f, 0.20f), (t - 0.5f) * 2f);
        }
    }
}
