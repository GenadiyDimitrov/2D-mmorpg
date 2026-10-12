using System;
using Game.Shared;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Client
{
    /// <summary>
    /// `BL-344` step 5: the sun. Turns the scene's one directional light by <see cref="GameClock"/> (synced from
    /// the server at login, so every phone shows the same hour) and sets the ambient to match: a warm, low sun at
    /// dawn and dusk, a white one at midday, and a dim blue moon at night that still leaves the map readable.
    ///
    /// <para><b>Cosmetic only.</b> The server's own day/night (<see cref="DayPhase"/>, 06:00-18:00) decides what
    /// spawns; this only decides what it looks like. The look is a table of keys over the 24 hours, blended
    /// linearly, so retuning it is editing numbers in <see cref="Keys"/>.</para>
    ///
    /// <para><b>Shadows</b> are a Settings choice (<see cref="Shadows"/>): characters only by default, because the
    /// one shadow map they need is cheap; trees and rocks are an extra step up, since they double the scenery's
    /// vertex work. The shadow range follows the camera so a zoom-out never cuts them off mid-screen.</para>
    /// </summary>
    public class DayNight : MonoBehaviour
    {
        // ── Settings → Shadows ──────────────────────────────────────────────────────────────────

        public enum Shadows { Off = 0, Characters = 1, All = 2 }
        public const string ShadowPref = "gfx.shadows";
        public static Shadows ShadowQuality = Shadows.Characters;

        public static void LoadPrefs()
        {
            ShadowQuality = (Shadows)Mathf.Clamp(PlayerPrefs.GetInt(ShadowPref, (int)Shadows.Characters), 0, 2);
            NightLevel = Mathf.Clamp(PlayerPrefs.GetFloat(NightPref, NightDefault), NightMin, NightMax);
        }

        public static void SetShadows(Shadows s)
        {
            ShadowQuality = s;
            PlayerPrefs.SetInt(ShadowPref, (int)s);
        }

        public static string ShadowLabel => ShadowQuality switch
        {
            Shadows.Off => "Shadows: off (faster)",
            Shadows.All => "Shadows: characters + trees",
            _ => "Shadows: characters",
        };

        // ── the look ────────────────────────────────────────────────────────────────────────────

        /// <summary>How bright the ground is at midnight, as a fraction of midday, in LINEAR light (0.30 looks like about
        /// 60% on screen, because the eye is not linear). Settings → Night brightness moves it per player.</summary>
        public static float NightLevel = NightDefault;
        public const float NightDefault = 0.30f, NightMin = 0.10f, NightMax = 0.60f;   // owner, 2026-10-12: 0.30 ("0.12 is to dark, 0.2 as well")
        public const string NightPref = "gfx.night";

        /// <summary>What the light does at one moment.</summary>
        public struct Look
        {
            public Quaternion Rotation;
            public Color Colour;          // the sun/moon colour; its intensity is folded in
            public Color Ambient;
            public float ShadowStrength;
        }

        private readonly struct Key
        {
            public readonly float Hour, Intensity, Shadow;
            public readonly Color Colour, Ambient;
            public Key(float hour, Color colour, float intensity, Color ambient, float shadow)
            { Hour = hour; Colour = colour; Intensity = intensity; Ambient = ambient; Shadow = shadow; }
        }

        // Day ground brightness ≈ intensity × sin(elevation) + ambient: 0.8 × 0.9 + 0.33 ≈ 1.05 at noon, i.e. the
        // biome colours as authored (the unlit ground showed them at 1.0). Dawn and dusk lift the ambient because
        // a low sun on flat ground lights it weakly; the warmth is in the colours, and it is what the upright
        // characters catch. Night keys are filled from NightLevel (see NightKey).
        private static readonly Color DayLight = new Color(1.00f, 0.96f, 0.90f);
        private static readonly Color DayAmbient = new Color(0.31f, 0.33f, 0.37f);
        private static readonly Color WarmLight = new Color(1.00f, 0.62f, 0.36f);
        private static readonly Color WarmAmbient = new Color(0.36f, 0.30f, 0.31f);
        private static readonly Color DuskLight = new Color(1.00f, 0.52f, 0.30f);
        private static readonly Color DuskAmbient = new Color(0.36f, 0.27f, 0.30f);
        private static readonly Color TwilightLight = new Color(0.62f, 0.52f, 0.85f);
        private static readonly Color MoonLight = new Color(0.45f, 0.62f, 1.00f);
        private static readonly Color NightTint = new Color(0.50f, 0.78f, 1.70f);   // luma ≈ 0.79, deliberately saturated: .gamma washes it out

        private static Key[] _keys;
        private static float _keysFor = -1f;

        private static Key[] Keys(float night)
        {
            if (_keys != null && night == _keysFor) return _keys;
            _keysFor = night;
            // Night: half the brightness from the moon (at 50°, sin ≈ 0.77), half from a blue ambient.
            float total = night * 1.05f;
            float moon = 0.5f * total / 0.77f;
            var nightAmbient = NightTint * (0.5f * total / 0.79f);
            var twilightAmbient = Color.Lerp(nightAmbient, DayAmbient, 0.45f);
            return _keys = new[]
            {
                new Key(0.0f,  MoonLight, moon, nightAmbient, 0.35f),
                new Key(4.5f,  MoonLight, moon, nightAmbient, 0.35f),
                new Key(5.5f,  TwilightLight, Mathf.Lerp(moon, 0.5f, 0.5f), twilightAmbient, 0.3f),
                new Key(6.5f,  WarmLight, 0.60f, WarmAmbient, 0.6f),
                new Key(8.5f,  DayLight, 0.80f, DayAmbient, 0.7f),
                new Key(15.5f, DayLight, 0.80f, DayAmbient, 0.7f),
                new Key(17.5f, DuskLight, 0.60f, DuskAmbient, 0.6f),
                new Key(18.5f, TwilightLight, Mathf.Lerp(moon, 0.5f, 0.5f), twilightAmbient, 0.3f),
                new Key(19.5f, MoonLight, moon, nightAmbient, 0.35f),
                new Key(24.0f, MoonLight, moon, nightAmbient, 0.35f),
            };
        }

        /// <summary>The look at <paramref name="hour"/> (0-24), with night at <paramref name="night"/> of midday.</summary>
        public static Look LookAt(double hour, float night)
        {
            float h = (float)(((hour % 24.0) + 24.0) % 24.0);
            var keys = Keys(night);
            int i = 0;
            while (i < keys.Length - 2 && h >= keys[i + 1].Hour) i++;
            var a = keys[i]; var b = keys[i + 1];
            float t = Mathf.InverseLerp(a.Hour, b.Hour, h);

            // Direction. The sun rises in the east (+X), stands in the south at noon (shadows point up the
            // screen) and sets in the west, never lower than 25° so a flat ground still gets some of it. The moon
            // crosses the same way at a steady 50°. Between them, over the twilight hours when the light is
            // dimmest, the direction swings from one to the other, so the swap is never seen.
            float dayT = Mathf.Clamp01((h - 6f) / 12f);
            var sun = Quaternion.Euler(25f + 40f * Mathf.Sin(Mathf.PI * dayT), -90f + 180f * dayT, 0f);
            float nightT = ((h - 18f + 24f) % 24f) / 12f;
            var moonRot = Quaternion.Euler(50f, -90f + 180f * Mathf.Clamp01(nightT), 0f);
            float dayness = Mathf.Clamp01(Mathf.Min(Mathf.InverseLerp(4.5f, 6.5f, h), Mathf.InverseLerp(19.5f, 17.5f, h)));

            return new Look
            {
                Rotation = Quaternion.Slerp(moonRot, sun, Mathf.SmoothStep(0f, 1f, dayness)),
                Colour = Color.Lerp(a.Colour * a.Intensity, b.Colour * b.Intensity, t),
                Ambient = Color.Lerp(a.Ambient, b.Ambient, t),
                ShadowStrength = Mathf.Lerp(a.Shadow, b.Shadow, t),
            };
        }

        /// <summary>Puts <paramref name="look"/> on the light and the ambient. Also used by the editor preview.</summary>
        public static void Apply(Light sun, Look look, bool shadows)
        {
            // Every number in the keys is a LINEAR brightness (what the shader multiplies by). Unity reads a light's
            // colour and the flat ambient as sRGB and converts them, so in a linear-space project they go in as
            // .gamma, or night lands at a third of what the keys say (measured in the first preview).
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            if (sun != null)
            {
                sun.transform.rotation = look.Rotation;
                // Colour carries the intensity, so the light's own intensity stays at 1 and nothing doubles up.
                sun.color = linear ? look.Colour.gamma : look.Colour;
                sun.intensity = 1f;
                sun.shadows = shadows ? LightShadows.Hard : LightShadows.None;
                sun.shadowStrength = look.ShadowStrength;
            }

            // Flat ambient, and the probe set by hand as well: URP's Lit (the models and props) and the ground
            // shader both read the probe, and setting it explicitly does not depend on Unity re-baking it.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = linear ? look.Ambient.gamma : look.Ambient;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(look.Ambient);
            RenderSettings.ambientProbe = sh;
        }

        // ── runtime ─────────────────────────────────────────────────────────────────────────────

        private Light _sun;
        private float _shadowDistance = -1f;

        private void Start()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { _sun = l; break; }
            if (_sun == null)
            {
                _sun = new GameObject("Sun").AddComponent<Light>();
                _sun.type = LightType.Directional;
            }
            _sun.shadowBias = 0.05f;
            _sun.shadowNormalBias = 0.4f;
            RenderSettings.sun = _sun;
        }

        private void Update()
        {
            Apply(_sun, LookAt(GameClock.HourOfDay(DateTime.UtcNow), NightLevel), ShadowQuality != Shadows.Off);
            if (ShadowQuality != Shadows.Off) FitShadowDistance();
        }

        /// <summary>
        /// The shadow range has to reach the far edge of the screen, or shadows stop on a line partway up it, but
        /// every unit past that spreads the same shadow map thinner. So: the distance from the camera to the
        /// farthest ground corner on screen, plus a little. Only on the phone: in the Editor the pipeline asset
        /// would carry the change back to disk.
        /// </summary>
        private void FitShadowDistance()
        {
            if (Application.isEditor) return;
            var cam = Camera.main;
            if (cam == null || !(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)) return;

            var ground = new Plane(Vector3.up, Vector3.zero);
            float far = 0f;
            foreach (var corner in new[] { new Vector3(0f, 1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f) })
            {
                var ray = cam.ViewportPointToRay(corner);
                far = Mathf.Max(far, ground.Raycast(ray, out float d) ? d : 250f);
            }
            float want = Mathf.Clamp(far * 1.05f + 2f, 30f, 250f);
            if (Mathf.Abs(want - _shadowDistance) < _shadowDistance * 0.05f) return;
            _shadowDistance = want;
            urp.shadowDistance = want;
        }
    }
}
