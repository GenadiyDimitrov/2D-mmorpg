using System.Diagnostics;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// `BL-365` — the overworld painted into ONE texture, for the minimap and the world map.
    ///
    /// <para>Every pixel is <see cref="BiomeGround.ColourAt"/>, the same function that colours the 3D ground, so the map
    /// can never disagree with what you walk on. The overworld is [0, ZoneWidth] × [0, ZoneHeight]; row 0 of the texture
    /// is its SOUTH edge (server Y grows downward, see <see cref="WorldMapper"/>), so a map UV is
    /// <c>(x / W, 1 − y / H)</c> — <see cref="Uv"/> is the one place that says so.</para>
    ///
    /// <para>It is built a few rows at a time under a per-frame time budget (<see cref="Step"/>), so opening the world
    /// never hitches on a phone: ~260k samples arrive over about a second, and the minimap simply shows them as they
    /// land.</para>
    /// </summary>
    public static class MapTexture
    {
        public const int Size = 512;
        public static float WorldW => GameConstants.ZoneWidth;
        public static float WorldH => GameConstants.ZoneHeight;

        private static Texture2D _texture;
        private static Color32[] _pixels;
        private static int _nextRow;
        private static bool _dirtySinceApply, _detailed;

        public static bool Ready => _texture != null && _nextRow >= Size && _detailed;

        public static Texture2D Texture
        {
            get
            {
                if (_texture == null)
                {
                    _texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
                    {
                        name = "WorldMap", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
                    };
                    _pixels = new Color32[Size * Size];
                    var blank = (Color32)new Color(0.09f, 0.09f, 0.10f);
                    for (int i = 0; i < _pixels.Length; i++) _pixels[i] = blank;
                    _texture.SetPixels32(_pixels);
                    _texture.Apply(false);
                }
                return _texture;
            }
        }

        /// <summary>Paint rows for up to <paramref name="budgetMs"/> milliseconds. Call once a frame; free once done.</summary>
        public static void Step(float budgetMs = 3f)
        {
            if (Ready) return;
            var tex = Texture;
            var clock = Stopwatch.StartNew();
            float stepX = WorldW / Size, stepY = WorldH / Size;
            while (_nextRow < Size && clock.Elapsed.TotalMilliseconds < budgetMs)
            {
                int j = _nextRow++;
                float y = WorldH - (j + 0.5f) * stepY;   // row 0 = the south edge
                for (int i = 0; i < Size; i++)
                    _pixels[j * Size + i] = BiomeGround.ColourAt((i + 0.5f) * stepX, y);
                _dirtySinceApply = true;
            }
            if (_nextRow >= Size && !_detailed)
            {
                PaintDetail();   // `BL-366` — once, on the finished ground; a few ms
                _detailed = true;
                _dirtySinceApply = true;
            }
            // Upload at most every 8 rows (and at the end): one Apply per row would cost more than the painting.
            if (_dirtySinceApply && (_nextRow % 8 == 0 || _nextRow >= Size))
            {
                tex.SetPixels32(_pixels);
                tex.Apply(false);
                _dirtySinceApply = false;
            }
        }


        // ----- `BL-366`: roads and town plans on top of the ground ----------------------------------
        //
        // Painted ONCE, after the last ground row, from the same shared data the 3D ground is built from
        // (WorldMap.Roads, TownLayout.Plans) — so the map shows the road you are standing on. One texel is ~94 world
        // units, so every shape is drawn with a one-texel soft edge and at least ~1.5 texels wide (a 300-wide road is
        // 3 texels; a side path, 70 wide, is under one and is left to the city view, which draws it crisp).
        //
        // Map colours are a step LIGHTER than the 3D ground's (GameUi.BuildTownPlans): at this size a road the colour
        // of dry earth vanishes into a brown biome.

        private static readonly Color MapRoad = new Color(0.62f, 0.54f, 0.40f);
        private static readonly Color MapPlaza = new Color(0.70f, 0.63f, 0.48f);
        private static readonly Color MapLot = new Color(0.28f, 0.27f, 0.27f);

        private static void PaintDetail()
        {
            foreach (var r in WorldMap.Roads)
                for (int i = 0; i < r.Points.Length - 1; i++)
                    Strip(r.Points[i].X, r.Points[i].Y, r.Points[i + 1].X, r.Points[i + 1].Y, 300f, MapRoad);
            foreach (var plan in TownLayout.Plans)
            {
                foreach (var s in plan.Roads)
                    if (!s.Path) Strip(s.A.X, s.A.Y, s.B.X, s.B.Y, s.Width, MapRoad);
                Disc(plan.Centre.X, plan.Centre.Y, plan.PlazaRadius, MapPlaza);
                foreach (var lot in plan.Lots)
                    Box(lot.Centre.X, lot.Centre.Y, lot.W, lot.H, lot.AngleDeg, MapLot);
            }
        }

        private static float Texel => WorldW / Size;

        private static void Strip(float ax, float ay, float bx, float by, float width, Color col)
        {
            float half = Mathf.Max(width / 2f, Texel * 0.75f);
            float dx = bx - ax, dy = by - ay, len2 = Mathf.Max(dx * dx + dy * dy, 1f);
            Paint(Mathf.Min(ax, bx) - half, Mathf.Min(ay, by) - half, Mathf.Max(ax, bx) + half, Mathf.Max(ay, by) + half,
                  (x, y) =>
                  {
                      float t = Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / len2);
                      float ex = x - (ax + t * dx), ey = y - (ay + t * dy);
                      return Edge(half - Mathf.Sqrt(ex * ex + ey * ey));
                  }, col);
        }

        private static void Disc(float cx, float cy, float r, Color col) =>
            Paint(cx - r, cy - r, cx + r, cy + r,
                  (x, y) => Edge(r - Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy))), col);

        /// <summary>A rotated rectangle — TownLot's rule: local (lx, ly) turned by <paramref name="angleDeg"/> in server axes.</summary>
        private static void Box(float cx, float cy, float w, float h, float angleDeg, Color col)
        {
            float t = angleDeg * Mathf.Deg2Rad, cs = Mathf.Cos(t), sn = Mathf.Sin(t);
            float hw = Mathf.Max(w / 2f, Texel * 0.6f), hh = Mathf.Max(h / 2f, Texel * 0.6f), reach = hw + hh;
            Paint(cx - reach, cy - reach, cx + reach, cy + reach, (x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float lx = dx * cs + dy * sn, ly = -dx * sn + dy * cs;   // the inverse turn
                return Edge(hw - Mathf.Abs(lx)) * Edge(hh - Mathf.Abs(ly));
            }, col);
        }

        /// <summary>Coverage of a texel whose centre is <paramref name="inside"/> world units inside an edge.</summary>
        private static float Edge(float inside) => Mathf.Clamp01(inside / Texel + 0.5f);

        /// <summary>Blend <paramref name="col"/> into every texel of a world-space box by its coverage.</summary>
        private static void Paint(float minX, float minY, float maxX, float maxY, System.Func<float, float, float> cover, Color col)
        {
            float step = Texel;
            int i0 = Mathf.Max(0, Mathf.FloorToInt(minX / step)), i1 = Mathf.Min(Size - 1, Mathf.CeilToInt(maxX / step));
            // Row j's centre is y = H − (j + 0.5)·step, so a world y range maps to rows the other way round.
            int j0 = Mathf.Max(0, Mathf.FloorToInt((WorldH - maxY) / step)), j1 = Mathf.Min(Size - 1, Mathf.CeilToInt((WorldH - minY) / step));
            for (int j = j0; j <= j1; j++)
            {
                float y = WorldH - (j + 0.5f) * step;
                for (int i = i0; i <= i1; i++)
                {
                    float a = cover((i + 0.5f) * step, y);
                    if (a <= 0f) continue;
                    int k = j * Size + i;
                    _pixels[k] = Color32.Lerp(_pixels[k], (Color32)col, a);
                }
            }
        }
        /// <summary>A server position as a UV on <see cref="Texture"/>.</summary>
        public static Vector2 Uv(float x, float y) => new Vector2(x / WorldW, 1f - y / WorldH);

        public static bool InOverworld(float x, float y) => x >= 0f && y >= 0f && x <= WorldW && y <= WorldH;

        // ----- the small shapes the maps are drawn with ---------------------------------------------
        //
        // Drawn, not glyphs: the TMP atlas is static and has no arrow or bullet (the same reason UiKit's icons are drawn).

        private static Sprite _circle, _arrow;

        /// <summary>A soft-edged white disc — the minimap's mask and frame, and every dot.</summary>
        public static Sprite Circle => _circle ??= Make(64, (u, v) =>
        {
            float d = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 64f;
            return Mathf.Clamp01(32f - d);
        });

        /// <summary>A white arrowhead pointing UP (+y), notched at the back so its heading reads at a glance.</summary>
        public static Sprite Arrow => _arrow ??= Make(64, (u, v) =>
        {
            float x = (u - 0.5f) * 2f;   // -1..1
            // Tip at v = 0.95, wings at v = 0.08, notch at v = 0.35 on the centre line.
            float half = Mathf.InverseLerp(0.95f, 0.08f, v) * 0.85f;   // the outline's half-width at this height
            float notch = Mathf.Lerp(0.35f, 0.08f, Mathf.Abs(x) / 0.85f);
            bool inside = v <= 0.95f && v >= 0.08f && Mathf.Abs(x) <= half && v >= notch;
            return inside ? 1f : 0f;
        });

        private static Sprite Make(int size, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                    px[j * size + i] = new Color32(255, 255, 255,
                        (byte)(255f * alpha((i + 0.5f) / size, (j + 0.5f) / size)));
            tex.SetPixels32(px);
            tex.Apply(false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
