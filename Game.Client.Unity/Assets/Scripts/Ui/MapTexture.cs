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
        private static bool _dirtySinceApply;

        public static bool Ready => _texture != null && _nextRow >= Size;

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
            // Upload at most every 8 rows (and at the end): one Apply per row would cost more than the painting.
            if (_dirtySinceApply && (_nextRow % 8 == 0 || _nextRow >= Size))
            {
                tex.SetPixels32(_pixels);
                tex.Apply(false);
                _dirtySinceApply = false;
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
