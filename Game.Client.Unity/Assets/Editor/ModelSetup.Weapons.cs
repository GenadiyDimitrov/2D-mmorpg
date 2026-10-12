using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Client;
using Game.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.ClientEditor
{
    /// <summary>
    /// `BL-103` / `BL-344` step 7: the weapon prefabs <see cref="HeldWeapons"/> hangs on a body's hands, and a headless
    /// preview of every set in hand.
    ///
    /// <para>🔑 <b>Every prefab is built into ONE frame</b> (the class note on <see cref="HeldWeapons"/>): the root is
    /// the grip, +Y runs along the weapon, +Z is the edge, a shield faces −X. Both packs model their weapons standing
    /// on +Y with the origin at or near the grip, so each row only says how long the thing is (as a share of the
    /// body's height), where the grip sits (as a share of its length from the FBX origin) and how far to turn it about
    /// its own axis. Nothing here is guessed per rig: the hand frame is the runtime's job.</para>
    ///
    /// <para>Sources (both CC0) in <c>Art/Models/Weapons/SOURCES.txt</c>. Only the prefabs under
    /// <c>Resources/Weapons/</c> ship, with their FBX and our own materials.</para>
    /// </summary>
    public static partial class ModelSetup
    {
        private const string WeaponArt = ArtDir + "/Weapons";
        private const string WeaponMats = WeaponArt + "/Materials";
        private const string WeaponsDir = "Assets/Resources/Weapons";

        private readonly struct WeaponRow
        {
            public readonly string Key, Fbx;
            public readonly float Length, Grip, Yaw;
            public WeaponRow(string key, string fbx, float length, float grip, float yaw)
            { Key = key; Fbx = fbx; Length = length; Grip = grip; Yaw = yaw; }
        }

        /// <summary>Length = share of the body's height (a 1.8 m man: sword 0.95 m, staff 1.75 m). Grip = where the
        /// hand closes, as a share of the length measured from the FBX origin (negative = below it, on the handle).
        /// Yaw turns the mesh about its long axis into the frame (the packs lay a guard / hammer head across X).</summary>
        private static readonly WeaponRow[] WeaponRows =
        {
            new WeaponRow("sword1h", "Sword",           0.52f, -0.07f, 90f),
            new WeaponRow("sword2h", "Sword_Big",       0.80f, -0.03f, 90f),
            new WeaponRow("blunt1h", "Hammer_Small",    0.42f,  0.00f, 90f),
            new WeaponRow("blunt2h", "Hammer_Double",   0.72f,  0.00f, 90f),
            new WeaponRow("dagger",  "Dagger",          0.30f, -0.10f, 90f),
            new WeaponRow("bow",     "Bow_Wooden",      0.75f,  0.00f, 90f),
            new WeaponRow("wand",    "Wand",            0.30f,  0.00f, 90f),
            new WeaponRow("staff",   "Staff",           0.97f,  0.00f, 90f),
            new WeaponRow("shield",  "Shield_Heater_2", 0.46f,  0.00f, 270f),
        };

        [MenuItem("Tools/BL-93/Build weapon prefabs (BL-103)")]
        public static void BuildWeapons()
        {
            bool ok = BuildWeaponPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!ok && Application.isBatchMode) EditorApplication.Exit(1);
        }

        /// <summary>Headless: rebuild the character (for the new attack styles) and the weapons, then render the preview.</summary>
        public static void BuildWeaponsAndPreview()
        {
            bool ok = BuildCharacterBodies();
            ok &= BuildWeaponPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!ok) { if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            PreviewWeapons();
        }

        private static bool BuildWeaponPrefabs()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) { Debug.LogError("[BL-103] URP Lit shader not found."); return false; }

            var body = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/humanoid.prefab");
            if (body == null) { Debug.LogError("[BL-103] Models/humanoid.prefab missing — build the characters first."); return false; }
            var bodyInst = (GameObject)PrefabUtility.InstantiatePrefab(body);
            float height = MeshHeight(bodyInst);
            Object.DestroyImmediate(bodyInst);
            if (height < 0.01f) { Debug.LogError("[BL-103] humanoid has no height."); return false; }

            if (AssetImporter.GetAtPath(WeaponArt + "/mage_texture.png") is TextureImporter ti && ti.maxTextureSize != 256)
            {
                ti.maxTextureSize = 256;          // a 1024² colour-swatch atlas: 256 loses nothing
                ti.SaveAndReimport();
            }

            Directory.CreateDirectory(WeaponMats);
            if (AssetDatabase.IsValidFolder(WeaponsDir)) AssetDatabase.DeleteAsset(WeaponsDir);
            AssetDatabase.CreateFolder("Assets/Resources", "Weapons");

            var mats = new Dictionary<string, Material>();
            int built = 0;
            foreach (var w in WeaponRows)
                if (BuildWeapon(w, height, lit, mats)) built++;

            Debug.Log($"[BL-103] ModelSetup: built {built}/{WeaponRows.Length} weapon prefabs (body height {height:0.00}).");
            return built == WeaponRows.Length;
        }

        private static bool BuildWeapon(WeaponRow w, float bodyHeight, Shader lit, Dictionary<string, Material> mats)
        {
            string fbxPath = $"{WeaponArt}/{w.Fbx}.fbx";
            if (AssetImporter.GetAtPath(fbxPath) is not ModelImporter imp)
            {
                Debug.LogError($"[BL-103] Not an importable model: {fbxPath}");
                return false;
            }

            imp.animationType = ModelImporterAnimationType.None;
            imp.importAnimation = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.importBlendShapes = false;
            imp.isReadable = false;
            imp.meshCompression = ModelImporterMeshCompression.Medium;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
            foreach (var kv in imp.GetExternalObjectMap())
                if (kv.Key.type == typeof(Material)) imp.RemoveRemap(kv.Key);
            imp.SaveAndReimport();

            // Our own URP Lit copy of every material the FBX carries (flat colours for Quaternius, the swatch atlas for
            // KayKit), named after it, so the pack's imported materials never reach the build.
            foreach (var src in AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>().ToList())
            {
                if (!mats.TryGetValue(src.name, out var ours))
                {
                    ours = WeaponMaterial(src, lit);
                    mats[src.name] = ours;
                }
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src.name), ours);
            }
            imp.SaveAndReimport();

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null) { Debug.LogError($"[BL-103] Missing FBX: {fbxPath}"); return false; }

            var root = new GameObject(w.Key);
            var mesh = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            mesh.transform.SetParent(root.transform, false);

            var rs = mesh.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) { Debug.LogError($"[BL-103] {w.Fbx}: no renderers."); Object.DestroyImmediate(root); return false; }
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);

            // Long along Y is what every row assumes; say so loudly if a pack ever disagrees.
            if (b.size.y < b.size.x || b.size.y < b.size.z)
                Debug.LogWarning($"[BL-103] {w.Fbx}: bounds {b.size} are not longest on Y — check its preview.");

            float s = w.Length * bodyHeight / b.size.y;
            float originToGrip = w.Grip * b.size.y;     // in the unscaled mesh, from the FBX origin up
            mesh.transform.localScale = mesh.transform.localScale * s;
            mesh.transform.localRotation = Quaternion.Euler(0f, w.Yaw, 0f) * mesh.transform.localRotation;
            mesh.transform.localPosition = new Vector3(0f, -originToGrip * s, 0f);

            int tris = 0;
            foreach (var r in mesh.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = false;
                foreach (var m in r.sharedMaterials)
                    if (m == null || !mats.ContainsValue(m))
                    {
                        Debug.LogError($"[BL-103] {w.Fbx}: renderer '{r.name}' keeps material '{(m == null ? "null" : m.name)}', not ours.");
                        Object.DestroyImmediate(root);
                        return false;
                    }
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                    for (int i = 0; i < mf.sharedMesh.subMeshCount; i++) tris += (int)mf.sharedMesh.GetIndexCount(i) / 3;
            }
            foreach (var c in root.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

            string matNames = string.Join(", ", rs.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct());
            string outPath = $"{WeaponsDir}/{w.Key}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, outPath, out bool ok);
            Object.DestroyImmediate(root);
            if (!ok) { Debug.LogError($"[BL-103] {w.Key}: SaveAsPrefabAsset failed."); return false; }
            Debug.Log($"[BL-103] {w.Key} <- {w.Fbx}: raw bounds {b.size}, scale {s:0.000}, {tris} tris, " +
                      $"materials [{matNames}] -> {outPath}");
            return true;
        }

        private static Material WeaponMaterial(Material src, Shader lit)
        {
            string path = $"{WeaponMats}/{src.name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = lit;

            Color color = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor")
                        : src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white;
            Texture tex = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap")
                        : src.HasProperty("_MainTex") ? src.GetTexture("_MainTex") : null;
            color.a = 1f;
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.3f);
            mat.SetFloat("_EnvironmentReflections", 0f);
            mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            mat.SetFloat("_ReceiveShadows", 0f);
            mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(mat);
            Debug.Log($"[BL-103] material {src.name}: colour {color}, texture {(tex == null ? "-" : tex.name)}");
            return mat;
        }

        /// <summary>
        /// Every weapon set in a body's hands: idle and mid-attack from the side, then idle from straight above (the
        /// game's default camera). Batchmode WITHOUT <c>-nographics</c>; the PNG goes where <c>-previewOut</c> says.
        /// It dresses the body with the RUNTIME code (<see cref="HeldWeapons.Dress"/>), so the preview checks the same
        /// sockets the phone builds.
        /// </summary>
        public static void PreviewWeapons()
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-previewOut");
            string outPath = at >= 0 && at + 1 < args.Length ? args[at + 1] : "weapons-preview.png";

            int editorQuality = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(0, true);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);

            var sets = new (string label, WeaponType w, bool magic, bool shield)[]
            {
                ("sword", WeaponType.Sword, false, false), ("sword+shield", WeaponType.Sword, false, true),
                ("2h sword", WeaponType.TwoHandedSword, false, false), ("blunt", WeaponType.Blunt, false, false),
                ("2h blunt", WeaponType.TwoHandedBlunt, false, false), ("duals", WeaponType.Dual, false, false),
                ("bow", WeaponType.Bow, false, false), ("wand+shield", WeaponType.Blunt, true, true),
                ("staff", WeaponType.TwoHandedBlunt, true, false), ("bare", WeaponType.None, false, false),
            };

            var pf = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/humanoid.prefab");
            if (pf == null) { Debug.LogError("[BL-103] preview: no humanoid prefab"); return; }

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            RenderSettings.sun = sun;
            DayNight.Apply(sun, DayNight.LookAt(12.0, DayNight.NightDefault), true);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 4f);
            ground.transform.position = new Vector3(sets.Length * 2.5f, 0f, 0f);
            var gmat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            gmat.SetColor("_BaseColor", new Color(0.22f, 0.36f, 0.18f));
            ground.GetComponent<Renderer>().sharedMaterial = gmat;

            var bodies = new List<(GameObject go, Animator anim, int style)>();
            for (int i = 0; i < sets.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, scene);
                go.transform.SetPositionAndRotation(new Vector3(i * 5f, 0f, 0f), Quaternion.Euler(0f, 25f, 0f));
                go.name = "body_" + i;
                go.transform.localScale *= EntityManager.EntityScale;
                var anim = go.GetComponentInChildren<Animator>();
                anim.Rebind();
                var held = new List<GameObject>();
                bool hands = HeldWeapons.Dress(anim, go.transform, sets[i].w, sets[i].magic, sets[i].shield, held);
                Debug.Log($"[BL-103] preview {sets[i].label}: hands={hands}, held [{string.Join(", ", held.Select(h => h.name))}]");
                bodies.Add((go, anim, HeldWeapons.StyleFor(sets[i].w, sets[i].magic)));
            }

            float h = Mathf.Max(0.5f, MeshHeight(bodies[0].go));
            var clips = bodies[0].anim.runtimeAnimatorController.animationClips;
            AnimationClip Clip(string n) => clips.FirstOrDefault(c => c.name.ToLowerInvariant() == n);
            var idle = Clip("idle_loop");
            var swing = new[] { Clip("sword_attack"), Clip("pistol_shoot"), Clip("spell_simple_shoot") };
            Debug.Log($"[BL-103] preview clips: idle={idle?.name} swings=[{string.Join(", ", swing.Select(c => c == null ? "-" : c.name))}]");

            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.65f, 0.75f);
            cam.orthographic = true;

            const int FW = 360, FH = 420;
            int cols = sets.Length, rows = 3;
            var sheet = new Texture2D(FW * cols, FH * rows, TextureFormat.RGB24, false);
            var rt = new RenderTexture(FW, FH, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();   // warm-up: the FIRST render draws before textures upload
            AnimationMode.StartAnimationMode();

            for (int row = 0; row < rows; row++)
            {
                // Pose the whole row, then draw BAKED copies: headless, neither the skin nor a renderer under a bone
                // ever redraws after the bones move (see BakeSkin / BakeHeld).
                foreach (var (bgo, banim, bstyle) in bodies)
                {
                    // AnimationMode, the Editor's own preview sampler: it retargets a Humanoid clip, which
                    // AnimationClip.SampleAnimation and an edit-mode Animator.Update did not do reliably headless.
                    var clip = row == 1 ? swing[bstyle] ?? swing[0] : idle;
                    if (clip == null) continue;
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(banim.gameObject, clip, row == 1 ? clip.length * 0.45f : 0.3f);
                    AnimationMode.EndSampling();
                    BakeSkin(bgo);
                    BakeHeld(bgo);
                }
                for (int i = 0; i < sets.Length; i++)
                {
                    var (go, anim, style) = bodies[i];

                    var c = go.transform.position + Vector3.up * (h * 0.5f);
                    if (row < 2)
                    {
                        cam.orthographicSize = h * 0.62f;
                        cam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
                        cam.transform.position = c - cam.transform.forward * 6f;
                    }
                    else
                    {
                        cam.orthographicSize = h * 0.62f;
                        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                        cam.transform.position = go.transform.position + Vector3.up * 6f;
                    }
                    cam.Render();
                    cam.Render();
                    RenderTexture.active = rt;
                    var img = new Texture2D(FW, FH, TextureFormat.RGB24, false);
                    img.ReadPixels(new Rect(0, 0, FW, FH), 0, 0);
                    img.Apply();
                    sheet.SetPixels(i * FW, (rows - 1 - row) * FH, FW, FH, img.GetPixels());
                    Object.DestroyImmediate(img);
                }
            }
            AnimationMode.StopAnimationMode();
            RenderTexture.active = null;
            sheet.Apply();
            File.WriteAllBytes(outPath, sheet.EncodeToPNG());
            QualitySettings.SetQualityLevel(editorQuality, true);
            Debug.Log($"[BL-103] weapons preview ({sets.Length} sets x {rows}) -> {outPath}");
        }

        /// <summary>Headless, a skinned mesh is never re-skinned: it drew the bind pose while the bones (and the weapons on
        /// them) were posed. So the preview draws a BAKED copy of the skin in the current pose instead.</summary>
        /// <summary>The same staleness for the weapons: their TRANSFORMS follow the posed hand (logged), but headless a
        /// renderer under a bone keeps drawing the matrix it had when it was created. So each pose gets fresh copies at
        /// the transforms' real world matrices, outside the skeleton, and the originals are hidden.</summary>
        private static void BakeHeld(GameObject body)
        {
            var old = GameObject.Find(body.name + "__held");
            if (old != null) Object.DestroyImmediate(old);
            var holder = new GameObject(body.name + "__held");
            foreach (var mr in body.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.name.EndsWith("__baked")) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null) continue;
                var copy = new GameObject(mr.name, typeof(MeshFilter), typeof(MeshRenderer));
                copy.transform.SetParent(holder.transform, false);
                var m = mr.transform.localToWorldMatrix;
                copy.transform.SetPositionAndRotation(m.GetPosition(), m.rotation);
                copy.transform.localScale = m.lossyScale;
                copy.GetComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                copy.GetComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
                mr.enabled = false;
            }
        }

        private static readonly Dictionary<SkinnedMeshRenderer, float> BakeScale = new Dictionary<SkinnedMeshRenderer, float>();

        private static void BakeSkin(GameObject body)
        {
            foreach (var smr in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.name.EndsWith("__baked")) continue;
                var t = smr.transform.Find(smr.name + "__baked");
                GameObject baked;
                if (t == null)
                {
                    baked = new GameObject(smr.name + "__baked", typeof(MeshFilter), typeof(MeshRenderer));
                    baked.transform.SetParent(smr.transform, false);
                    baked.GetComponent<MeshRenderer>().sharedMaterials = smr.sharedMaterials;
                    baked.GetComponent<MeshFilter>().sharedMesh = new Mesh();
                }
                else baked = t.gameObject;
                // The copy sits on the renderer. BakeMesh leaves this rig's centimetre units out, so the first bake (the
                // idle, near the bind pose) is scaled to the renderer's own bind-pose bounds, which headless never move.
                baked.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
                var mesh = baked.GetComponent<MeshFilter>().sharedMesh;
                smr.enabled = true;
                smr.BakeMesh(mesh, true);
                smr.enabled = false;
                if (!BakeScale.TryGetValue(smr, out float k))
                {
                    // the stale bounds are the prefab's, from before the entity-size scale was applied
                    k = smr.bounds.size.y * EntityManager.EntityScale / Mathf.Max(1e-6f, mesh.bounds.size.y);
                    BakeScale[smr] = k;
                }
                baked.transform.localScale = Vector3.one * k / Mathf.Max(1e-6f, smr.transform.lossyScale.x);
            }
        }
    }
}
