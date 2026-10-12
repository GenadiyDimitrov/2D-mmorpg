using System.IO;
using System.Linq;
using Game.Client;
using UnityEditor;
using UnityEngine;

namespace Game.ClientEditor
{
    /// <summary>
    /// `BL-344` step 6: renders the skill effects headlessly (<see cref="FxPlayer"/>), one per frame, so the looks can
    /// be judged without the Editor or a phone. Two players and a rat on a patch of meadow, noon light, the PHONE's
    /// quality level. Batchmode WITHOUT <c>-nographics</c>; the PNG goes where <c>-previewOut</c> says.
    ///
    /// <para>Rows, left to right: projectile in flight / projectile landed / arrow / heal beam; slash / crit /
    /// heal / mana; buff aura / debuff / res pillar / the aura again from straight above (the default camera).</para>
    /// </summary>
    public static partial class ModelSetup
    {
        public static void PreviewFx()
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-previewOut");
            string outPath = at >= 0 && at + 1 < args.Length ? args[at + 1] : "fx-preview.png";

            int editorQuality = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(0, true);   // "Mobile" (put back at the end, or it is saved)
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            var gmat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            gmat.SetColor("_BaseColor", new Color(0.22f, 0.36f, 0.18f));
            ground.GetComponent<Renderer>().sharedMaterial = gmat;

            var caster = new Vector3(-2.2f, 0f, 0f);
            var target = new Vector3(2.2f, 0f, 0f);
            Place("Assets/Resources/Models/humanoid.prefab", caster, 90f);
            Place("Assets/Resources/Models/humanoid.prefab", target, 270f);
            Place("Assets/Resources/Models/mob_animal.prefab", new Vector3(0f, 0f, 3f), 180f);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            RenderSettings.sun = sun;
            DayNight.Apply(sun, DayNight.LookAt(12.0, DayNight.NightDefault), true);

            var fx = new GameObject("FxPlayer").AddComponent<FxPlayer>();
            fx.Init();

            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.09f, 0.10f);
            cam.fieldOfView = 60f;

            // (what, seconds after firing, top-down?)
            var frames = new (string what, float t, bool top)[]
            {
                ("projectile", 0.10f, false), ("projectile", 0.30f, false), ("arrow", 0.06f, false), ("beam", 0.10f, false),
                ("slash", 0.06f, false), ("crit", 0.08f, false), ("heal", 0.45f, false), ("mana", 0.45f, false),
                ("aura", 0.35f, false), ("debuff", 0.55f, false), ("pillar", 0.45f, false), ("aura", 0.35f, true),
            };
            const int FW = 640, FH = 400, Cols = 4;
            int rows = (frames.Length + Cols - 1) / Cols;
            var sheet = new Texture2D(FW * Cols, FH * rows, TextureFormat.RGB24, false);
            var rt = new RenderTexture(FW, FH, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            Aim(false);
            cam.Render();   // warm-up: the FIRST render draws before textures upload
            for (int f = 0; f < frames.Length; f++)
            {
                Aim(frames[f].top);
                fx.PreviewClear();
                fx.PreviewShot(frames[f].what, caster, target);
                string emitted = fx.PreviewCounts;
                for (float s = 0f; s < frames[f].t - 1e-4f; s += 0.02f) fx.PreviewAdvance(0.02f);
                Debug.Log($"[BL-344] fx frame {f} {frames[f].what}: after emit {emitted}; at render {fx.PreviewCounts}");
                cam.Render();
                RenderTexture.active = rt;
                var img = new Texture2D(FW, FH, TextureFormat.RGB24, false);
                img.ReadPixels(new Rect(0, 0, FW, FH), 0, 0);
                img.Apply();
                int col = f % Cols, row = rows - 1 - f / Cols;   // texture rows run bottom-up
                sheet.SetPixels(col * FW, row * FH, FW, FH, img.GetPixels());
                Object.DestroyImmediate(img);
            }
            RenderTexture.active = null;
            sheet.Apply();
            File.WriteAllBytes(outPath, sheet.EncodeToPNG());
            QualitySettings.SetQualityLevel(editorQuality, true);
            Debug.Log($"[BL-344] fx preview ({frames.Length} frames) -> {outPath}");

            void Aim(bool top)
            {
                // Tilted: the 60° angle the light preview uses. Top: the game's default straight-down view.
                cam.transform.rotation = Quaternion.Euler(top ? 90f : 60f, 0f, 0f);
                cam.transform.position = new Vector3(0f, 0.6f, 0f) - cam.transform.forward * (top ? 6.5f : 5.5f);
            }

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
        }
    }
}
