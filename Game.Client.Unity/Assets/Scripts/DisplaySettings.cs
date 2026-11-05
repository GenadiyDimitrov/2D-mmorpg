using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Client
{
    /// <summary>
    /// `BL-364` items 5-8 — the per-device DISPLAY and MOUSE settings: render scale, frame-rate cap (+ vsync on Windows),
    /// window / fullscreen, and the mouse's turn speed, invert Y and wheel-zoom speed. LOOK settings like the rest of
    /// Settings: PlayerPrefs, nothing goes to the server. <see cref="Load"/> applies them all at start.
    /// Render scale and the FPS cap are on the phone too; the rest are desktop-only (Settings shows them only there).
    /// </summary>
    public static class DisplaySettings
    {
        public const string RenderScalePref = "disp.renderScale", FpsPref = "disp.fps", VSyncPref = "disp.vsync",
                            TurnPref = "mouse.turn", InvertPref = "mouse.invertY", WheelPref = "mouse.wheel";

        public static readonly string[] AllPrefs = { RenderScalePref, FpsPref, VSyncPref, TurnPref, InvertPref, WheelPref };

        public static bool IsWeb => Application.platform == RuntimePlatform.WebGLPlayer;
        public static bool IsWindows => Application.platform == RuntimePlatform.WindowsPlayer;

        // ----- render scale (item 5) -------------------------------------------------------------

        public const float RenderScaleMin = 0.5f, RenderScaleMax = 1f;
        public static float RenderScale { get; private set; } = 1f;

        /// <summary>URP draws the 3D world at this fraction of the screen's pixels and stretches it; the HUD is a
        /// screen-space canvas and stays sharp. The biggest FPS lever on a big browser canvas. Not in the Editor,
        /// where the change would be written back into the pipeline asset on disk (the same rule as DayNight's shadows).</summary>
        public static void SetRenderScale(float v)
        {
            RenderScale = Mathf.Clamp(v, RenderScaleMin, RenderScaleMax);
            if (Application.isEditor) return;
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp) urp.renderScale = RenderScale;
        }

        // ----- frame rate (item 6) ---------------------------------------------------------------

        /// <summary>The cap steps. 0 = no cap of ours: as fast as the machine goes on Windows, and the screen's own
        /// rate in a browser (requestAnimationFrame — Unity's advice for WebGL, where any number is a timer that
        /// paces less evenly). The phone gets no "unlimited": it only cooks the battery.</summary>
        public static int[] FpsSteps => Application.isMobilePlatform ? new[] { 30, 60, 120 } : new[] { 30, 60, 120, 0 };

        /// <summary>The browser's default is the screen's rate; everywhere else 60, as since the Android fix.</summary>
        public static int FpsDefault => IsWeb ? 0 : 60;
        public static int FpsCap { get; private set; } = 60;

        public static void SetFpsCap(int fps)
        {
            FpsCap = System.Array.IndexOf(FpsSteps, fps) >= 0 ? fps : FpsDefault;
            ApplyFrameRate();
        }

        public static string FpsLabel => FpsCap > 0 ? FpsCap + " FPS" : IsWeb ? "screen rate" : "unlimited";

        /// <summary>Windows only: vsync on = the monitor paces the frames and the cap is ignored (Unity's rule).</summary>
        public static bool VSync { get; private set; }

        public static void SetVSync(bool on)
        {
            VSync = on && IsWindows;
            ApplyFrameRate();
        }

        private static void ApplyFrameRate()
        {
            if (IsWindows) QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = FpsCap > 0 ? FpsCap : -1;
        }

        // ----- window (item 7) -------------------------------------------------------------------

        /// <summary>Fullscreen on Windows = borderless full-screen window; in a browser = the page's fullscreen, which
        /// Unity asks for inside the next click (a browser grants it only there), so a button press is the right trigger.
        /// Unity itself remembers the Windows mode and size between launches.</summary>
        public static bool Fullscreen => Screen.fullScreen;

        public static void ToggleFullscreen()
        {
            if (IsWindows)
                Screen.fullScreenMode = Screen.fullScreen ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
            else
                Screen.fullScreen = !Screen.fullScreen;
        }

        /// <summary>Windows: the window sizes to step through — the monitor's own modes, one per size, smallest first,
        /// from 1024 wide up (smaller does not fit the HUD).</summary>
        public static Vector2Int[] Resolutions()
        {
            var list = new System.Collections.Generic.List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (size.x >= 1024 && !list.Contains(size)) list.Add(size);
            }
            if (list.Count == 0) list.Add(new Vector2Int(Screen.width, Screen.height));
            list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return list.ToArray();
        }

        /// <summary>Next size up (wrapping), kept in the current mode. In fullscreen it is the rendered size.</summary>
        public static void NextResolution()
        {
            var steps = Resolutions();
            int at = -1;
            for (int i = 0; i < steps.Length; i++)
                if (steps[i].x == Screen.width && steps[i].y == Screen.height) at = i;
            var next = steps[(at + 1) % steps.Length];
            Screen.SetResolution(next.x, next.y, Screen.fullScreenMode);
        }

        public static string ResolutionLabel => Screen.width + "×" + Screen.height;

        // ----- mouse (item 8) --------------------------------------------------------------------

        public const float MouseScaleMin = 0.25f, MouseScaleMax = 3f;

        /// <summary>× on the right-drag camera turn (mouse only: a finger's drag is unchanged).</summary>
        public static float TurnSpeed = 1f;
        /// <summary>Mouse up turns the camera DOWN instead of up.</summary>
        public static bool InvertY;
        /// <summary>× on each wheel notch's zoom.</summary>
        public static float WheelSpeed = 1f;

        public static void Load()
        {
            SetRenderScale(PlayerPrefs.GetFloat(RenderScalePref, 1f));
            FpsCap = PlayerPrefs.GetInt(FpsPref, FpsDefault);
            if (System.Array.IndexOf(FpsSteps, FpsCap) < 0) FpsCap = FpsDefault;
            VSync = IsWindows && PlayerPrefs.GetInt(VSyncPref, 0) == 1;
            ApplyFrameRate();
            TurnSpeed = Mathf.Clamp(PlayerPrefs.GetFloat(TurnPref, 1f), MouseScaleMin, MouseScaleMax);
            InvertY = PlayerPrefs.GetInt(InvertPref, 0) == 1;
            WheelSpeed = Mathf.Clamp(PlayerPrefs.GetFloat(WheelPref, 1f), MouseScaleMin, MouseScaleMax);
        }
    }
}
