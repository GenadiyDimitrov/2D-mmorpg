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
            if (r == null || _regionToastBg == null) return;
            string band = r.MaxLevel > 0 ? "   (Lv " + r.MinLevel + "-" + r.MaxLevel + ")" : "";
            _regionToast.text = "You entered " + r.Name + band;
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
                var go = new GameObject(region.Id);
                go.transform.SetParent(_regionOutlines.transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.loop = true;
                lr.widthMultiplier = 0.6f;
                lr.material = new Material(UnlitMaterials.Shader);   // IL2CPP-safe (no magenta on device)
                Color col = region.Kind == RegionKind.Town
                    ? new Color(0.35f, 0.70f, 1.00f, 0.85f)          // towns: cyan
                    : new Color(1.00f, 0.72f, 0.30f, 0.80f);         // fields: warm amber
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
    }
}
