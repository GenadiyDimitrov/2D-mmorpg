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
            _regionToastBg.gameObject.SetActive(false);

            BuildRegionOutlines();
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
        }

        private void BuildRegionOutlines()
        {
            _regionOutlines = new GameObject("RegionOutlines");
            foreach (var region in RegionMap.All)
            {
                if (region.Outline == null || region.Outline.Length < 3) continue;

                // FIELDS get a filled polygon coloured by their LEVEL band — this is the field colour the
                // owner asked for, replacing the spawn-zone circles (ZoneOverlay skips the circles a field
                // covers). Towns are left as an outline only.
                if (region.Kind == RegionKind.Field)
                    BuildRegionFill(region);

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

        /// <summary>A flat filled polygon on the ground, coloured by the field's LEVEL band (same green→
        /// red reading as the nameplate colours and the old zone discs). Triangulated as a fan and made
        /// double-sided so it shows regardless of the outline's winding.</summary>
        private void BuildRegionFill(Region region)
        {
            var band = RegionMap.LevelBand(region.Id);
            Color col = ColourForLevel(band?.Max ?? 1);

            var poly = region.Outline;
            var verts = new Vector3[poly.Length];
            for (int i = 0; i < poly.Length; i++)
            {
                var u = WorldMapper.ToUnity(poly[i].X, poly[i].Y);
                u.y = 0.02f;                     // above the ground, below the 0.06 outline
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
