using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.ClientEditor
{
    /// <summary>
    /// `BL-344` step 4: the scenery props. Builds <c>Resources/Props/&lt;Kind&gt;/&lt;Model&gt;.prefab</c>
    /// from the Quaternius Stylized Nature MegaKit, headlessly, for <c>PropScatter</c> to instance.
    ///
    /// <code>
    /// "C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" -batchmode -quit -nographics ^
    ///     -projectPath G:\Work\Repository\L2Clone\Game.Client.Unity ^
    ///     -executeMethod Game.ClientEditor.ModelSetup.BuildProps -logFile -
    /// </code>
    ///
    /// <para>🔑 <b>The materials are OURS, shipped as assets, never the FBX's own.</b> Three reasons, all
    /// of which fail silently on the phone:</para>
    /// <list type="bullet">
    /// <item><b>Instancing.</b> The build strips shader variants no shipped material uses (GraphicsSettings
    /// "Strip Unused"). <c>PropScatter</c> draws with <c>Graphics.RenderMeshInstanced</c>, so the
    /// instanced variant must be on a material the build can SEE — an asset with
    /// <c>enableInstancing</c>, not a runtime copy.</item>
    /// <item><b>Alpha CLIP, two-sided.</b> Leaves and grass are cards with transparent texels. The pack's
    /// import makes them opaque (white squares) or, set to Transparent, sorts them wrong and costs
    /// blending overdraw. Clipping is the mobile-correct answer.</item>
    /// <item><b>Texture size.</b> The pack ships 2048² bark and rock maps for a camera that sees a tree at
    /// a couple of hundred pixels. The sizes in <see cref="PropMaterials"/> are what reaches the APK.</item>
    /// </list>
    /// <para>The FBX importer is told to REMAP each of its material names onto ours
    /// (<c>AddRemap</c>), so no embedded material — and none of the 2048² maps it would drag along —
    /// is generated at all.</para>
    /// </summary>
    public static partial class ModelSetup
    {
        private const string NatureDir = ArtDir + "/Nature";
        private const string NatureFbx = NatureDir + "/FBX";
        private const string NatureTex = NatureDir + "/Textures";
        private const string NatureMats = NatureDir + "/Materials";
        private const string PropsDir = "Assets/Resources/Props";

        /// <summary>One material, by the name the FBXs use for it. Texture is a file in
        /// <see cref="NatureTex"/>; MaxSize caps that texture's import.</summary>
        private readonly struct PropMat
        {
            public readonly string Name, Texture;
            public readonly bool Clip;
            public readonly int MaxSize;
            public readonly Color Tint;
            public PropMat(string name, string texture, bool clip, int maxSize, Color? tint = null)
            { Name = name; Texture = texture; Clip = clip; MaxSize = maxSize; Tint = tint ?? Color.white; }
        }

        /// <summary>
        /// The pack's material names → our materials. The `_C` maps are the COLOURED leaves; the plain
        /// ones are white masks for the pack's own tint shader and would draw white. The normal maps are
        /// deliberately unused: a stylised hand-painted bark seen from above gains nothing from them on a
        /// phone, and they were 3 × 2048² of APK.
        /// </summary>
        private static readonly PropMat[] PropMaterials =
        {
            new PropMat("Bark_NormalTree",    "Bark_NormalTree.png",       false, 512),
            new PropMat("Bark_TwistedTree",   "Bark_TwistedTree.png",      false, 512),
            new PropMat("Bark_DeadTree",      "Bark_DeadTree.png",         false, 512),
            new PropMat("Leaves_NormalTree",  "Leaves_NormalTree_C.png",   true,  512),
            // The twisted tree's `_C` leaf map is AUTUMN RED (and the plain bush wears it too): the white mask, tinted
            // moss green, is what a marsh tree and a field bush should be.
            new PropMat("Leaves_TwistedTree", "Leaves_TwistedTree.png",    true,  512, new Color(0.34f, 0.50f, 0.20f)),
            new PropMat("Leaves_Pine",        "Leaf_Pine_C.png",           true,  512),
            new PropMat("Leaves",             "Leaves.png",                true,  1024),
            new PropMat("Flowers",            "Flowers.png",               true,  512),
            new PropMat("Grass",              "Grass.png",                 true,  128),
            new PropMat("Rocks",              "Rocks_Diffuse.png",         false, 512),
            // The wispy grass's UVs already sit on the ORANGE strip of Grass.png; this tint only mutes it towards
            // straw. Only DryGrass uses it (a per-entry swap below), so the green Grass stays untouched.
            new PropMat("Grass_Dry",          "Grass.png",                 true,  128, new Color(0.90f, 0.85f, 0.72f)),
        };

        /// <summary>One prop model: the PropKind folder it lands in, the FBX, and an optional material
        /// swap (pack name → one of ours) for a kind that reuses another kind's mesh.</summary>
        private readonly struct Prop
        {
            public readonly string Kind, Fbx, SwapFrom, SwapTo;
            public Prop(string kind, string fbx, string swapFrom = null, string swapTo = null)
            { Kind = kind; Fbx = fbx; SwapFrom = swapFrom; SwapTo = swapTo; }
        }

        /// <summary>
        /// 🔑 THE PICKS — two per kind (three rocks: they are tiny), chosen as the LIGHTEST variants of
        /// each set. A screen holds dozens of each, all instanced, so the per-model triangle count is
        /// multiplied by the crowd; the heavy variants look the same from a top-down camera.
        /// <list type="bullet">
        /// <item>TwistedTree → its own kind for the MARSH, where a gnarled tree says "swamp".</item>
        /// <item>Flower groups → their own kind for the MEADOW, the one biome that should look kind.</item>
        /// <item>DryGrass = the wispy grass with the straw tint (the pack has no dry grass).</item>
        /// </list>
        /// The kind names MUST match <c>PropKind</c>: <c>PropScatter</c> loads <c>Props/&lt;Kind&gt;</c>.
        /// </summary>
        private static readonly Prop[] Props =
        {
            new Prop("Tree",        "CommonTree_3"),
            new Prop("Tree",        "CommonTree_5"),
            new Prop("TwistedTree", "TwistedTree_1"),
            new Prop("TwistedTree", "TwistedTree_2"),
            new Prop("Pine",        "Pine_4"),
            new Prop("Pine",        "Pine_5"),
            new Prop("DeadTree",    "DeadTree_4"),
            new Prop("DeadTree",    "DeadTree_5"),
            new Prop("Bush",        "Bush_Common"),
            new Prop("Bush",        "Bush_Common_Flowers"),
            new Prop("Rock",        "Rock_Medium_1"),
            new Prop("Rock",        "Rock_Medium_2"),
            new Prop("Rock",        "Rock_Medium_3"),
            new Prop("Grass",       "Grass_Common_Short"),
            new Prop("Grass",       "Grass_Common_Tall"),
            new Prop("DryGrass",    "Grass_Wispy_Short", "Grass", "Grass_Dry"),
            new Prop("DryGrass",    "Grass_Wispy_Tall",  "Grass", "Grass_Dry"),
            new Prop("Flower",      "Flower_3_Group"),
            new Prop("Flower",      "Flower_4_Group"),
        };

        [MenuItem("Tools/BL-93/Build scenery props (BL-344)")]
        public static void BuildProps()
        {
            bool ok = BuildPropPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!ok && Application.isBatchMode) EditorApplication.Exit(1);
        }

        private static bool BuildPropPrefabs()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) { Debug.LogError("[BL-344] URP Lit shader not found."); return false; }

            Directory.CreateDirectory(NatureMats);
            var mats = new Dictionary<string, Material>();
            foreach (var m in PropMaterials)
            {
                var mat = BuildPropMaterial(m, lit);
                if (mat == null) return false;
                mats[m.Name] = mat;
            }

            // A clean slate per kind folder: a model dropped from the picks must stop shipping.
            foreach (var kind in Props.Select(p => p.Kind).Distinct())
            {
                string dir = $"{PropsDir}/{kind}";
                if (AssetDatabase.IsValidFolder(dir)) AssetDatabase.DeleteAsset(dir);
                AssetDatabase.CreateFolder(PropsDir, kind);
            }

            int built = 0;
            foreach (var p in Props)
                if (BuildProp(p, mats)) built++;

            Debug.Log($"[BL-344] ModelSetup: built {built}/{Props.Length} prop prefabs.");
            return built == Props.Length;
        }

        private static Material BuildPropMaterial(PropMat m, Shader lit)
        {
            string texPath = $"{NatureTex}/{m.Texture}";
            if (AssetImporter.GetAtPath(texPath) is not TextureImporter ti)
            {
                Debug.LogError($"[BL-344] Missing texture {texPath} for material {m.Name}.");
                return null;
            }

            bool changed = false;
            if (ti.maxTextureSize != m.MaxSize) { ti.maxTextureSize = m.MaxSize; changed = true; }
            if (ti.textureType != TextureImporterType.Default) { ti.textureType = TextureImporterType.Default; changed = true; }
            if (m.Clip)
            {
                // Fill the transparent texels with the edge colour (no white fringe once filtered), and keep
                // each mip's clipped COVERAGE equal to the full-size map's — without it a tree's leaves
                // thin to nothing at the distance the camera actually sees it from.
                if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }
                if (!ti.mipMapsPreserveCoverage) { ti.mipMapsPreserveCoverage = true; changed = true; }
                if (Mathf.Abs(ti.alphaTestReferenceValue - 0.5f) > 0.001f) { ti.alphaTestReferenceValue = 0.5f; changed = true; }
            }
            if (changed) ti.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            string path = $"{NatureMats}/{m.Name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(lit);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = lit;
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
            mat.SetColor("_BaseColor", m.Tint);
            mat.SetColor("_Color", m.Tint);
            mat.SetFloat("_Surface", 0f);                  // opaque queue, clipped — never blended
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.1f);             // painted foliage, not wet plastic
            mat.SetFloat("_SpecularHighlights", 0f);
            mat.SetFloat("_EnvironmentReflections", 0f);
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            mat.SetFloat("_ReceiveShadows", 0f);           // PropScatter draws them with shadows off
            mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");

            if (m.Clip)
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.SetFloat("_Cull", (float)CullMode.Off);   // a leaf card is seen from both sides
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.SetOverrideTag("RenderType", "TransparentCutout");
                mat.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                mat.SetFloat("_AlphaClip", 0f);
                mat.SetFloat("_Cull", (float)CullMode.Back);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.renderQueue = (int)RenderQueue.Geometry;
            }

            mat.enableInstancing = true;                     // THE point — see the class summary
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static bool BuildProp(Prop p, Dictionary<string, Material> mats)
        {
            string fbxPath = $"{NatureFbx}/{p.Fbx}.fbx";
            if (AssetImporter.GetAtPath(fbxPath) is not ModelImporter imp)
            {
                Debug.LogError($"[BL-344] Not an importable model: {fbxPath}");
                return false;
            }

            // Static scenery: no rig, no clips, no cameras/lights, no CPU copy of the mesh.
            imp.animationType = ModelImporterAnimationType.None;
            imp.importAnimation = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.importBlendShapes = false;
            imp.isReadable = false;
            imp.meshCompression = ModelImporterMeshCompression.Medium;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
            imp.SaveAndReimport();

            // The names the FBX asks for, read off the import (remapped or not, the slot keeps its name).
            var names = new HashSet<string>();
            foreach (var mi in imp.GetExternalObjectMap())
                if (mi.Key.type == typeof(Material)) names.Add(mi.Key.name);
            foreach (var m in AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>()) names.Add(m.name);

            foreach (var n in names)
            {
                if (!mats.TryGetValue(n, out var ours))
                {
                    Debug.LogError($"[BL-344] {p.Fbx}: material '{n}' has no entry in PropMaterials.");
                    return false;
                }
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), ours);
            }
            imp.SaveAndReimport();

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null) { Debug.LogError($"[BL-344] Missing FBX: {fbxPath}"); return false; }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            if (inst == null) { Debug.LogError($"[BL-344] {p.Fbx}: could not instantiate."); return false; }

            int tris = 0;
            var used = new HashSet<string>();
            foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
            {
                var shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null || !mats.ContainsValue(shared[i]))
                    {
                        Debug.LogError($"[BL-344] {p.Fbx}: renderer '{r.name}' slot {i} is " +
                                       $"'{(shared[i] == null ? "null" : shared[i].name)}', not one of ours — " +
                                       "it would ship the pack's own material (pink or white on the phone).");
                        Object.DestroyImmediate(inst);
                        return false;
                    }
                    if (p.SwapFrom != null && shared[i].name == p.SwapFrom) shared[i] = mats[p.SwapTo];
                    used.Add(shared[i].name);
                }
                r.sharedMaterials = shared;
                r.shadowCastingMode = ShadowCastingMode.Off;
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                    for (int s = 0; s < mf.sharedMesh.subMeshCount; s++) tris += (int)mf.sharedMesh.GetIndexCount(s) / 3;
            }

            if (inst.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0)
                Debug.LogWarning($"[BL-344] {p.Fbx}: has a SkinnedMeshRenderer — PropScatter only draws MeshFilters.");

            float height = MeshHeight(inst);
            string outPath = $"{PropsDir}/{p.Kind}/{p.Fbx}.prefab";
            PrefabUtility.SaveAsPrefabAsset(inst, outPath, out bool ok);
            Object.DestroyImmediate(inst);

            if (!ok) { Debug.LogError($"[BL-344] {p.Fbx}: SaveAsPrefabAsset failed."); return false; }
            Debug.Log($"[BL-344] {p.Kind}/{p.Fbx}: {tris} tris, height {height:0.00}, " +
                      $"materials [{string.Join(", ", used.OrderBy(s => s))}] -> {outPath}");
            return true;
        }
        /// <summary>Renders every prop prefab in a row to a PNG, so the textures can be checked (no pink,
        /// no white leaf cards) WITHOUT opening the Editor or installing an APK. Batchmode WITHOUT
        /// <c>-nographics</c>; the file goes where <c>-previewOut &lt;path&gt;</c> says.</summary>
        public static void PreviewProps()
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-previewOut");
            string outPath = at >= 0 && at + 1 < args.Length ? args[at + 1] : "props-preview.png";

            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);

            var prefabs = Props.Select(p => AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsDir}/{p.Kind}/{p.Fbx}.prefab"))
                               .Where(g => g != null).ToList();
            float x = 0f;
            foreach (var pf in prefabs)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, scene);
                float h = Mathf.Max(0.01f, MeshHeight(go));
                go.transform.localScale *= 2f / h;               // every prop 2 units tall
                go.transform.position = new Vector3(x, 0f, 0f);
                x += 2.4f;
            }

            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.7f, 0.85f);
            cam.orthographic = true;
            cam.orthographicSize = x / 2f * 1080f / 2400f / 2f + 1.6f;
            cam.transform.position = new Vector3(x / 2f - 1.2f, 4f, -10f);
            cam.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
            var rt = new RenderTexture(2400, 540, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var img = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            img.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            img.Apply();
            File.WriteAllBytes(outPath, img.EncodeToPNG());
            RenderTexture.active = null;
            Debug.Log($"[BL-344] preview of {prefabs.Count} props -> {outPath}");
        }

    }
}
