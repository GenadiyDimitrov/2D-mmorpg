using Game.Client;
using UnityEngine;

namespace Game.ClientEditor
{
    /// <summary>
    /// `BL-344` step 8: what the drag gesture can show, rendered headlessly. The light preview's set
    /// (<see cref="BuildPreviewWorld"/>) at noon, posed by the game's own <see cref="CameraRig.Pose"/> at
    /// the tilt and turn limits. Batchmode WITHOUT <c>-nographics</c>; the PNG goes where <c>-previewOut</c> says.
    ///
    /// <para>Top row, facing north: 90° (the default), 70°, 55°, 45° (the limit). Bottom row, turned: 55° east,
    /// 55° south, 45° at 250°, 60° at 320°.</para>
    /// </summary>
    public static partial class ModelSetup
    {
        public static void PreviewCamera()
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-previewOut");
            string outPath = at >= 0 && at + 1 < args.Length ? args[at + 1] : "camera-preview.png";

            int editorQuality = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(0, true);   // "Mobile", put back at the end or it is saved
            var sun = BuildPreviewWorld();
            DayNight.Apply(sun, DayNight.LookAt(12.0, DayNight.NightDefault), true);

            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.09f, 0.10f);
            cam.fieldOfView = 60f;

            var poses = new (float pitch, float yaw)[]
            {
                (90f, 0f), (70f, 0f), (55f, 0f), (CameraRig.MinPitch, 0f),
                (55f, 90f), (55f, 180f), (CameraRig.MinPitch, 250f), (60f, 320f),
            };
            var target = new Vector3(0.05f, 0f, 0.3f);   // between the two players
            RenderSheet(cam, outPath, poses.Length, f => CameraRig.Pose(cam.transform, target, poses[f].pitch, poses[f].yaw, 14f));
            QualitySettings.SetQualityLevel(editorQuality, true);
            Debug.Log($"[BL-344] camera preview ({poses.Length} poses) -> {outPath}");
        }
    }
}
