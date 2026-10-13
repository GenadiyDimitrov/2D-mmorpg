using System.IO;
using System.Linq;
using Game.Client;
using Game.Shared;
using UnityEditor;
using UnityEngine;

namespace Game.ClientEditor
{
    /// <summary>
    /// `BL-344` step 5: renders the day/night look headlessly, so the light can be judged without the Editor or a
    /// phone. A real patch of biome ground (BiomeGround's own colours, beside Brackenford), two players, a mob and
    /// a few props, lit by <see cref="DayNight"/> at several hours with the PHONE's quality level (one shadow
    /// cascade, hard shadows). Batchmode WITHOUT <c>-nographics</c>; the PNG goes where <c>-previewOut</c> says.
    ///
    /// <para>Top row: 06:15 dawn, 12:00 noon, 20:45 dusk, 21:30 twilight. Bottom row: midnight at night levels
    /// 0.12 / 0.20 / 0.30 of midday (linear), then 05:30, early dawn, at 0.20.</para>
    /// </summary>
    public static partial class ModelSetup
    {
        public static void PreviewLight()
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-previewOut");
            string outPath = at >= 0 && at + 1 < args.Length ? args[at + 1] : "light-preview.png";

            int editorQuality = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(0, true);   // "Mobile": what Android runs (put back at the end, or it is saved)
            var sun = BuildPreviewWorld();

            // The game's default 2.5D-ish angle when tilted: 60° down, 12 units back (zoomed in so the characters can be judged).
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.09f, 0.10f);
            cam.fieldOfView = 60f;
            cam.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            cam.transform.position = new Vector3(0f, 0f, 0.5f) - cam.transform.forward * 12f;

            var frames = new (double hour, float night)[]
            {
                (6.25, DayNight.NightDefault), (12.0, DayNight.NightDefault), (20.75, DayNight.NightDefault), (21.5, DayNight.NightDefault),
                (0.0, DayNight.NightMin), (0.0, DayNight.NightDefault), (0.0, DayNight.NightMax), (5.5, DayNight.NightDefault),
            };
            RenderSheet(cam, outPath, frames.Length, f => DayNight.Apply(sun, DayNight.LookAt(frames[f].hour, frames[f].night), true));
            QualitySettings.SetQualityLevel(editorQuality, true);
            Debug.Log($"[BL-344] light preview ({frames.Length} frames) -> {outPath}");
        }

        /// <summary>The preview set (shared by the light and camera previews): a 60 × 40 patch of real biome
        /// ground beside Brackenford, two players and a rat in their idle pose, a few props, and a sun.</summary>
        private static Light BuildPreviewWorld()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);

            // Ground: 60 × 40 Unity units (6000 × 4000 server units), one vertex per 100, coloured by the game's
            // own ColourAt, starting at Brackenford's edge so paving, meadow and the blend all show.
            var site = Biomes.Sites.First(s => s.Biome == GroundBiome.Meadow);
            float ox = site.X + 600f, oy = site.Y - 2000f;
            const int W = 61, H = 41;
            var verts = new Vector3[W * H]; var cols = new Color[W * H]; var norms = new Vector3[W * H];
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            for (int j = 0; j < H; j++)
                for (int i = 0; i < W; i++)
                {
                    int k = j * W + i;
                    verts[k] = new Vector3(i, 0f, -j);
                    var c = BiomeGround.ColourAt(ox + i * 100f, oy + j * 100f);
                    cols[k] = linear ? c.linear : c;
                    norms[k] = Vector3.up;
                }
            var tris = new int[(W - 1) * (H - 1) * 6]; int t = 0;
            for (int j = 0; j < H - 1; j++)
                for (int i = 0; i < W - 1; i++)
                {
                    int a = j * W + i, b = a + 1, c = a + W, d = c + 1;
                    tris[t++] = a; tris[t++] = b; tris[t++] = c; tris[t++] = b; tris[t++] = d; tris[t++] = c;
                }
            var mesh = new Mesh { vertices = verts, colors = cols, normals = norms, triangles = tris };
            var ground = new GameObject("Ground");
            ground.AddComponent<MeshFilter>().sharedMesh = mesh;
            var gmr = ground.AddComponent<MeshRenderer>();
            gmr.sharedMaterial = new Material(Shader.Find("L2Clone/LitVertexColor"));
            gmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            gmr.receiveShadows = true;
            ground.transform.position = new Vector3(-30f, 0f, 20f);

            // Characters in their idle pose, and some scenery around them.
            Place("Assets/Resources/Models/humanoid.prefab", new Vector3(-1.2f, 0f, 0f), 0f);
            Place("Assets/Resources/Models/humanoid.prefab", new Vector3(1.3f, 0f, 0.6f), 200f);
            Place("Assets/Resources/Models/mob_animal.prefab", new Vector3(3.5f, 0f, -1.5f), 120f);
            Prop("Tree", 0, new Vector3(-5f, 0f, 3f), 4.2f);
            Prop("Tree", 1, new Vector3(6f, 0f, 4.5f), 4.0f);
            Prop("Pine", 0, new Vector3(-8f, 0f, -2f), 4.5f);
            Prop("Rock", 0, new Vector3(4f, 0f, 2f), 1.0f);
            Prop("Bush", 0, new Vector3(-3f, 0f, -3f), 1.2f);
            Prop("Grass", 0, new Vector3(0.5f, 0f, -2.5f), 0.5f);
            Prop("Flower", 0, new Vector3(-2.5f, 0f, 1.8f), 0.4f);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadowBias = 0.05f; sun.shadowNormalBias = 0.4f;
            RenderSettings.sun = sun;

            return sun;

            void Place(string path, Vector3 pos, float yaw)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (pf == null) { Debug.LogWarning("[BL-344] preview: missing " + path); return; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, scene);
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
                go.transform.localScale *= EntityManager.EntityScale;
                var anim = go.GetComponentInChildren<Animator>();
                var idle = anim != null && anim.runtimeAnimatorController != null
                    ? anim.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains("idle"))
                    : null;
                if (idle != null) idle.SampleAnimation(anim.gameObject, 0.3f);
            }

            void Prop(string kind, int index, Vector3 pos, float height)
            {
                var pf = Directory.GetFiles($"{PropsDir}/{kind}", "*.prefab").OrderBy(p => p).Skip(index).FirstOrDefault();
                if (pf == null) { Debug.LogWarning("[BL-344] preview: no prop " + kind); return; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(pf.Replace('\\', '/')), scene);
                float h = Mathf.Max(0.01f, MeshHeight(go));
                go.transform.localScale *= height / h;
                go.transform.position = pos;
                // Props cast nothing by default in the game (Settings → Shadows: characters).
                foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>Up to eight 800 × 450 frames on a 4 × 2 sheet, first four on top. <paramref name="setup"/> poses
        /// frame f before it is drawn. Renders once to warm up: the FIRST render draws before the textures upload
        /// (white characters, a green rat).</summary>
        private static void RenderSheet(Camera cam, string outPath, int count, System.Action<int> setup)
        {
            const int FW = 800, FH = 450;
            var sheet = new Texture2D(FW * 4, FH * 2, TextureFormat.RGB24, false);
            var rt = new RenderTexture(FW, FH, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            setup(0);
            cam.Render();
            for (int f = 0; f < count; f++)
            {
                setup(f);
                cam.Render();
                RenderTexture.active = rt;
                var img = new Texture2D(FW, FH, TextureFormat.RGB24, false);
                img.ReadPixels(new Rect(0, 0, FW, FH), 0, 0);
                img.Apply();
                int col = f % 4, row = 1 - f / 4;   // texture rows run bottom-up: first four frames on top
                sheet.SetPixels(col * FW, row * FH, FW, FH, img.GetPixels());
                Object.DestroyImmediate(img);
            }
            RenderTexture.active = null;
            sheet.Apply();
            File.WriteAllBytes(outPath, sheet.EncodeToPNG());
        }
    }
}
