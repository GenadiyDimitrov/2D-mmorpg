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
        /// 24000 units of reference you look up once; this is a 260-unit wall you are standing against,
        /// and it is only ever built into ~18 dashes that render only while you are inside it — which is
        /// also why it cannot bring back the 0.28.78 renderer flood. Dungeon boxes deliberately get no
        /// ring: their bounding box is not the polygon the map already draws, so a rectangle there would
        /// contradict the coloured outline rather than explain it.</para></summary>
        private void BuildJailBorder()
        {
            const float y = 0.09f;
            var colour = new Color(0.95f, 0.55f, 0.15f, 0.85f);
            var jail = WorldDomain.Jail;

            _jailBorder = new GameObject("JailBorder");
            var sharedMat = new Material(UnlitMaterials.Shader) { color = colour };

            // 36 arc segments, every other one drawn: a dashed circle without arc-length arithmetic.
            const int segments = 36;
            for (int i = 0; i < segments; i += 2)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                // dash longer than any chord + no gap = "draw this segment whole"; the GAP is the
                // segment we skip by stepping i by two.
                DashedEdge(_jailBorder, sharedMat, colour, 4f, jail.Radius * 2f, 0f, y,
                           jail.CentreX + Mathf.Cos(a0) * jail.Radius, jail.CentreY + Mathf.Sin(a0) * jail.Radius,
                           jail.CentreX + Mathf.Cos(a1) * jail.Radius, jail.CentreY + Mathf.Sin(a1) * jail.Radius);
            }

            _jailBorder.SetActive(false);   // shown only while you are actually in the cell
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

        /// <summary>A flat filled polygon on the ground at height <paramref name="height"/>. Triangulated
        /// as a fan (the outlines are convex) and made double-sided so it shows regardless of winding.
        /// Fields use their level colour at a low y; towns use a neutral fill at a higher y so they mask
        /// the field beneath them (the island look).</summary>
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

            // Fan (0, i, i+1) — the field outlines are convex, so a fan tessellates them cleanly. Emit
            // each triangle in BOTH windings so back-face culling can never hide it.
            int tri = poly.Length - 2;
            var tris = new int[tri * 6];
            int t = 0;
            for (int i = 1; i < poly.Length - 1; i++)
            {
                tris[t++] = 0; tris[t++] = i; tris[t++] = i + 1;
                tris[t++] = 0; tris[t++] = i + 1; tris[t++] = i;
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
