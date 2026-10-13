using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.ClientEditor
{
    /// <summary>
    /// The Quaternius outfit bodies (`Art/Models/Characters/Outfits`, CC0): clothed characters on the SAME skeleton as
    /// the Universal Animation Library, so the clips play on them with no retarget distortion — the old Adventurer rig
    /// (Hips / UpperLeg.L) crossed its legs on the run like a catwalk because its rest pose disagreed with the clips'.
    ///
    /// <para>🔑 <b>An outfit has NO HEAD</b> (the pack's readme: "only the head of the model is required"). The free base
    /// pack ships its head welded to a full body, so <see cref="HeadMesh"/> cuts the triangles the head and neck bones
    /// own out of it, and the head, eyes, brows and hair are re-bound to the OUTFIT's bones by name — same rig, so each
    /// follows the outfit's own head bone. The outfit pieces are split per slot (Arms / Body / Legs / Feet / Head_Hood),
    /// which is what `BL-103`'s per-grade armour looks will swap later.</para>
    /// </summary>
    public static partial class ModelSetup
    {
        private const string OutfitDir = ArtDir + "/Characters/Outfits";
        private const string OutfitHeadDir = OutfitDir + "/Head";
        private const string OutfitTex = OutfitDir + "/Textures";
        private const string OutfitMats = OutfitDir + "/Materials";
        private const string BaseBodyFbx = OutfitHeadDir + "/Superhero_Male_FullBody.fbx";
        private const string BaseBodyFemaleFbx = OutfitHeadDir + "/Superhero_Female_FullBody.fbx";

        private readonly struct OutfitMat
        {
            public readonly string Name, Base, Normal;
            public readonly int Size;
            public readonly bool Clip;
            public readonly Color Tint;
            public OutfitMat(string name, string baseTex, string normal, int size, bool clip = false, Color? tint = null)
            { Name = name; Base = baseTex; Normal = normal; Size = size; Clip = clip; Tint = tint ?? Color.white; }
        }

        /// <summary>Our URP Lit copy of every material the packs name, by the pack's own material name. Phone sizes: the
        /// pack's 2048-4096 maps are a desktop budget, and a character is ~120 px tall on screen.</summary>
        private static readonly OutfitMat[] OutfitMats_ =
        {
            new OutfitMat("MI_Ranger",          "T_Ranger_BaseColor",            "T_Ranger_Normal",         1024),
            new OutfitMat("MI_Peasant",         "T_Peasant_BaseColor",           "T_Peasant_Normal",        1024),
            new OutfitMat("MI_Regular_Male",    "T_Regular_Male_Dark_BaseColor", "T_Regular_Male_Normal",    512),
            new OutfitMat("MI_Superhero_Male",  "T_Superhero_Male_Dark",         null,                       512),
            new OutfitMat("MI_Regular_Female",  "T_Regular_Female_Dark_BaseColor", "T_Regular_Female_Normal", 512),
            new OutfitMat("MI_Superhero_Female", "T_Superhero_Female_Dark_BaseColor", null,                  512),
            new OutfitMat("MI_Eyes",            "T_Eye_Brown",                   null,                       128),
            new OutfitMat("MI_Hair_1",          "T_Hair_1_BaseColor",            null,                       512, clip: true,
                          tint: new Color(0.42f, 0.27f, 0.16f)),   // the map is a grey mask the pack's own shader colours
            new OutfitMat("MI_Hair_2",          "T_Hair_2_BaseColor",            null,                       512, clip: true,
                          tint: new Color(0.42f, 0.27f, 0.16f)),
        };

        private static Dictionary<string, Material> _outfitMats;

        /// <summary>Puts a head, eyes, brows and hair on an instantiated outfit and swaps every material for ours.</summary>
        private static void DressOutfit(GameObject inst, Body b)
        {
            _outfitMats ??= BuildOutfitMaterials();
            var bones = inst.GetComponentsInChildren<Transform>(true)
                            .GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());

            // Another file's clothes on THIS skeleton: the body's own pieces go, the other file's are rebound by bone name.
            if (!string.IsNullOrEmpty(b.Pieces))
            {
                if (AssetImporter.GetAtPath(b.Pieces) is ModelImporter pimp && pimp.animationType != ModelImporterAnimationType.Generic)
                {
                    pimp.animationType = ModelImporterAnimationType.Generic;   // no avatar of its own is needed or possible
                    pimp.SaveAndReimport();
                }
                var pieces = AssetDatabase.LoadAssetAtPath<GameObject>(b.Pieces);
                if (pieces == null) { Debug.LogError($"[BL-93] {b.Key}: missing pieces {b.Pieces}"); return; }
                // A prefab instance refuses to lose children; this body is saved as its own prefab anyway.
                if (PrefabUtility.IsPartOfPrefabInstance(inst))
                    PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var own in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    Object.DestroyImmediate(own.gameObject);
                foreach (var src in pieces.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    Rebind(inst, bones, src, src.sharedMesh, src.name);
            }

            // The Ranger's hood hides the hair the player picked at creation, so it is off. Kept in the prefab, hidden:
            // the day a helmet slot draws (BL-354 per-grade looks), this is the piece it turns back on.
            foreach (var r in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (r.name.Contains("Head_Hood")) r.gameObject.SetActive(false);

            string baseFbx = b.Female ? BaseBodyFemaleFbx : BaseBodyFbx;
            var baseBody = AssetDatabase.LoadAssetAtPath<GameObject>(baseFbx);
            if (baseBody == null) { Debug.LogError($"[BL-93] Missing {baseFbx}"); return; }
            PrepareCharacterBody(baseFbx);
            foreach (var src in baseBody.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                // The body mesh contributes its head only; the eyes and brows come over whole.
                bool body = src.name.StartsWith("SuperHero", System.StringComparison.OrdinalIgnoreCase);
                var mesh = body ? HeadMesh(src, b.Female ? "Head_Female" : "Head_Male") : src.sharedMesh;
                if (mesh != null) Rebind(inst, bones, src, mesh, body ? "Head" : src.name);
            }
            for (int h = 0; h < b.Hair.Length; h++)
            {
                string hair = b.Hair[h];
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{OutfitHeadDir}/{hair}.fbx");
                var src = fbx != null ? fbx.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
                if (src == null) { Debug.LogError($"[BL-93] {b.Key}: hair {hair} not found."); continue; }
                var go = Rebind(inst, bones, src, src.sharedMesh, hair);
                // Only the first style shows until CharacterLook picks one (a mob wearing this body keeps it).
                if (go != null && h > 0) go.SetActive(false);
            }

            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && _outfitMats.TryGetValue(mats[i].name, out var ours)) mats[i] = ours;
                    else Debug.LogError($"[BL-93] {b.Key}: renderer {r.name} keeps material " +
                                        $"'{(mats[i] == null ? "null" : mats[i].name)}' — add it to OutfitMats_.");
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = false;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
            }
        }

        /// <summary>A skinned mesh from another file of the same rig, driven by THIS body's bones (matched by name). The
        /// mesh keeps its own bind poses, so each vertex rides its bone exactly as it did in its source file.</summary>
        private static GameObject Rebind(GameObject inst, Dictionary<string, Transform> bones, SkinnedMeshRenderer src,
                                   Mesh mesh, string name)
        {
            var mapped = new Transform[src.bones.Length];
            for (int i = 0; i < mapped.Length; i++)
            {
                if (src.bones[i] == null || !bones.TryGetValue(src.bones[i].name, out mapped[i]))
                {
                    Debug.LogError($"[BL-93] {name}: bone '{(src.bones[i] == null ? "null" : src.bones[i].name)}' " +
                                   "is not on the outfit's skeleton.");
                    return null;
                }
            }
            var go = new GameObject(name);
            go.transform.SetParent(inst.transform, false);
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            smr.bones = mapped;
            smr.rootBone = src.rootBone != null && bones.TryGetValue(src.rootBone.name, out var root) ? root : mapped[0];
            smr.sharedMaterials = src.sharedMaterials;
            smr.localBounds = src.localBounds;
            return go;
        }

        /// <summary>The triangles of the base body that its head and neck bones own (every corner ≥ half-weighted to
        /// them), as a compacted mesh asset. Rebuilt every run so a new base body needs nothing by hand.</summary>
        private static Mesh HeadMesh(SkinnedMeshRenderer body, string assetName)
        {
            var src = body.sharedMesh;
            var headBones = new HashSet<int>();
            for (int i = 0; i < body.bones.Length; i++)
            {
                string n = body.bones[i] != null ? body.bones[i].name.ToLowerInvariant() : "";
                if (n == "head" || n.StartsWith("neck")) headBones.Add(i);
            }
            if (headBones.Count == 0) { Debug.LogError("[BL-93] base body has no head/neck bones."); return null; }

            var weights = src.boneWeights;
            float HeadShare(int v)
            {
                var w = weights[v];
                float s = 0f;
                if (headBones.Contains(w.boneIndex0)) s += w.weight0;
                if (headBones.Contains(w.boneIndex1)) s += w.weight1;
                if (headBones.Contains(w.boneIndex2)) s += w.weight2;
                if (headBones.Contains(w.boneIndex3)) s += w.weight3;
                return s;
            }

            var remap = new Dictionary<int, int>();
            var verts = new List<Vector3>(); var normals = new List<Vector3>(); var tangents = new List<Vector4>();
            var uvs = new List<Vector2>(); var bw = new List<BoneWeight>();
            var srcV = src.vertices; var srcN = src.normals; var srcT = src.tangents; var srcUv = src.uv;
            var tris = new List<int>();
            var all = src.triangles;
            for (int t = 0; t < all.Length; t += 3)
            {
                if (HeadShare(all[t]) < 0.5f || HeadShare(all[t + 1]) < 0.5f || HeadShare(all[t + 2]) < 0.5f) continue;
                for (int k = 0; k < 3; k++)
                {
                    int v = all[t + k];
                    if (!remap.TryGetValue(v, out int nv))
                    {
                        nv = verts.Count; remap[v] = nv;
                        verts.Add(srcV[v]);
                        if (srcN.Length > 0) normals.Add(srcN[v]);
                        if (srcT.Length > 0) tangents.Add(srcT[v]);
                        if (srcUv.Length > 0) uvs.Add(srcUv[v]);
                        bw.Add(weights[v]);
                    }
                    tris.Add(nv);
                }
            }

            var head = new Mesh { name = assetName };
            head.SetVertices(verts);
            if (normals.Count > 0) head.SetNormals(normals);
            if (tangents.Count > 0) head.SetTangents(tangents);
            if (uvs.Count > 0) head.SetUVs(0, uvs);
            head.SetTriangles(tris, 0);
            head.boneWeights = bw.ToArray();
            head.bindposes = src.bindposes;
            head.RecalculateBounds();

            string path = $"{OutfitHeadDir}/{assetName}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(head, path);
            Debug.Log($"[BL-93] head cut from {src.name}: {tris.Count / 3} of {all.Length / 3} triangles, {verts.Count} vertices.");
            return head;
        }

        private static Dictionary<string, Material> BuildOutfitMaterials()
        {
            if (!AssetDatabase.IsValidFolder(OutfitMats)) AssetDatabase.CreateFolder(OutfitDir, "Materials");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var result = new Dictionary<string, Material>();
            foreach (var m in OutfitMats_)
            {
                var baseTex = OutfitTexture(m.Base, m.Size, normal: false, alpha: m.Clip);
                var normal = m.Normal != null ? OutfitTexture(m.Normal, m.Size / 2, normal: true, alpha: false) : null;
                string path = $"{OutfitMats}/{m.Name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, path); }
                mat.shader = lit;
                mat.SetTexture("_BaseMap", baseTex);
                mat.SetTexture("_MainTex", baseTex);
                mat.SetColor("_BaseColor", m.Tint);
                mat.SetColor("_Color", m.Tint);
                mat.SetTexture("_BumpMap", normal);
                if (normal != null) mat.EnableKeyword("_NORMALMAP"); else mat.DisableKeyword("_NORMALMAP");
                mat.SetFloat("_Surface", 0f);
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Smoothness", 0.25f);
                mat.SetFloat("_EnvironmentReflections", 0f);
                mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                mat.SetFloat("_ReceiveShadows", 0f);
                mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
                if (m.Clip)
                {
                    mat.SetFloat("_AlphaClip", 1f);
                    mat.SetFloat("_Cutoff", 0.5f);
                    mat.SetFloat("_Cull", (float)CullMode.Off);   // hair cards are seen from both sides
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
                EditorUtility.SetDirty(mat);
                result[m.Name] = mat;
            }
            return result;
        }

        private static Texture2D OutfitTexture(string name, int size, bool normal, bool alpha)
        {
            string path = $"{OutfitTex}/{name}.png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti)
            {
                Debug.LogError($"[BL-93] Missing texture {path}");
                return null;
            }
            bool changed = false;
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (ti.textureType != type) { ti.textureType = type; changed = true; }
            if (ti.maxTextureSize != size) { ti.maxTextureSize = size; changed = true; }
            if (alpha && !ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }
            if (changed) ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>`BL-354` — every body × race × hairstyle on one sheet, in the idle pose, through the client's own
        /// <see cref="CharacterLook"/>: a row per (body, race), a column per hairstyle, the hair colour and skin tone
        /// stepping along the row so the ranges show too. The last row is the two NPC bodies. Batchmode WITHOUT
        /// -nographics; <c>-previewOut path.png</c>.</summary>
        [MenuItem("Tools/BL-93/Preview character looks (BL-354)")]
        public static void PreviewLooks()
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-previewOut");
            string outPath = at >= 0 && at + 1 < args.Length ? args[at + 1] : "looks-preview.png";

            int editorQuality = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(0, true);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            RenderSettings.sun = sun;
            Game.Client.DayNight.Apply(sun, Game.Client.DayNight.LookAt(12.0, Game.Client.DayNight.NightDefault), true);

            var rows = new (string key, Game.Shared.Race race, Game.Shared.Gender g)[]
            {
                ("player", Game.Shared.Race.Human, Game.Shared.Gender.Male),
                ("player_female", Game.Shared.Race.Human, Game.Shared.Gender.Female),
                ("player", Game.Shared.Race.Elf, Game.Shared.Gender.Male),
                ("player_female", Game.Shared.Race.Elf, Game.Shared.Gender.Female),
                ("player", Game.Shared.Race.Demon, Game.Shared.Gender.Male),
                ("player_female", Game.Shared.Race.Demon, Game.Shared.Gender.Female),
                ("npc_female", Game.Shared.Race.Human, Game.Shared.Gender.Female),
                ("humanoid", Game.Shared.Race.Human, Game.Shared.Gender.Male),
            };
            const int cols = Game.Shared.Appearance.HairStyles;
            var bodies = new List<GameObject>();
            for (int r = 0; r < rows.Length; r++)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/{rows[r].key}.prefab");
                if (pf == null) { Debug.LogError($"[BL-354] preview: no {rows[r].key} prefab"); return; }
                for (int c = 0; c < cols; c++)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, scene);
                    go.name = $"look_{r}_{c}";
                    go.transform.SetPositionAndRotation(new Vector3(c * 4f + r * 30f, 0f, 0f), Quaternion.Euler(0f, 160f, 0f));
                    Game.Client.CharacterLook.Apply(go, rows[r].race, rows[r].g, c, (c + r) % Game.Shared.Appearance.HairColors, c);
                    go.GetComponentInChildren<Animator>().Rebind();
                    bodies.Add(go);
                }
            }

            var clip = AssetDatabase.LoadAllAssetsAtPath(ArtDir + "/Characters/Animations/UAL1_Standard.fbx")
                                    .OfType<AnimationClip>().FirstOrDefault(c => c.name == "Idle_Loop");
            AnimationMode.StartAnimationMode();
            foreach (var go in bodies)
            {
                if (clip != null)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(go, clip, 0.3f);
                    AnimationMode.EndSampling();
                }
                BakeSkin(go);
            }

            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.65f, 0.75f);
            cam.orthographic = true;
            cam.orthographicSize = 1.25f;
            const int FW = 300, FH = 480;
            var sheet = new Texture2D(FW * cols, FH * rows.Length, TextureFormat.RGB24, false);
            var rt = new RenderTexture(FW, FH, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();   // warm-up: the first render draws before textures upload
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < cols; c++)
                {
                    cam.transform.rotation = Quaternion.Euler(5f, 180f, 0f);
                    cam.transform.position = new Vector3(c * 4f + r * 30f, 1.1f, 0f) - cam.transform.forward * 5f;
                    cam.Render(); cam.Render();
                    RenderTexture.active = rt;
                    var img = new Texture2D(FW, FH, TextureFormat.RGB24, false);
                    img.ReadPixels(new Rect(0, 0, FW, FH), 0, 0);
                    img.Apply();
                    sheet.SetPixels(c * FW, (rows.Length - 1 - r) * FH, FW, FH, img.GetPixels());
                    Object.DestroyImmediate(img);
                }
            AnimationMode.StopAnimationMode();
            RenderTexture.active = null;
            sheet.Apply();
            System.IO.File.WriteAllBytes(outPath, sheet.EncodeToPNG());
            QualitySettings.SetQualityLevel(editorQuality, true);
            Debug.Log($"[BL-354] looks preview ({rows.Length} x {cols}) -> {outPath}");
        }

        /// <summary>`BL-354` — the character-creation preview stage (Game.Client.CharacterPreview) rendered for six
        /// race × body picks under a MIDNIGHT sun, which is the case its own lights exist for. Batchmode WITHOUT
        /// -nographics, <c>-previewOut &lt;png&gt;</c>.</summary>
        [MenuItem("Tools/BL-93/Preview the creation-screen model (BL-354)")]
        public static void PreviewCreation()
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-previewOut");
            string outPath = at >= 0 && at + 1 < args.Length ? args[at + 1] : "creation-preview.png";

            int editorQuality = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(0, true);
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            RenderSettings.sun = sun;
            Game.Client.DayNight.Apply(sun, Game.Client.DayNight.LookAt(0.0, Game.Client.DayNight.NightDefault), true);

            var picks = new (Game.Shared.Race race, Game.Shared.BaseClass cls, Game.Shared.Gender g, int hair, int colour, int skin)[]
            {
                (Game.Shared.Race.Human, Game.Shared.BaseClass.Fighter, Game.Shared.Gender.Male, 3, 1, 2),
                (Game.Shared.Race.Human, Game.Shared.BaseClass.Mage, Game.Shared.Gender.Female, 0, 4, 0),
                (Game.Shared.Race.Elf, Game.Shared.BaseClass.Mage, Game.Shared.Gender.Male, 1, 6, 1),
                (Game.Shared.Race.Elf, Game.Shared.BaseClass.Fighter, Game.Shared.Gender.Female, 1, 3, 2),
                (Game.Shared.Race.Demon, Game.Shared.BaseClass.Fighter, Game.Shared.Gender.Male, 2, 0, 3),
                (Game.Shared.Race.Demon, Game.Shared.BaseClass.Mage, Game.Shared.Gender.Female, 4, 7, 4),
            };
            var stage = Game.Client.CharacterPreview.Create(264, 442);
            var cam = stage.GetComponentInChildren<Camera>();
            var clip = AssetDatabase.LoadAllAssetsAtPath(ArtDir + "/Characters/Animations/UAL1_Standard.fbx")
                                    .OfType<AnimationClip>().FirstOrDefault(c => c.name == "Idle_Loop");
            const int FW = 264, FH = 442;
            var sheet = new Texture2D(FW * picks.Length, FH, TextureFormat.RGB24, false);
            AnimationMode.StartAnimationMode();
            for (int i = 0; i <= picks.Length; i++)
            {
                var p = picks[System.Math.Max(0, i - 1)];
                stage.Show(p.race, p.cls, p.g, p.hair, p.colour, p.skin);
                var body = stage.transform.GetComponentInChildren<Animator>().gameObject;
                if (clip != null)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(body, clip, 0.3f);
                    AnimationMode.EndSampling();
                }
                BakeSkin(body);
                cam.Render(); cam.Render();
                if (i == 0) continue;   // the first pass only warms the textures up
                RenderTexture.active = stage.Texture;
                var img = new Texture2D(FW, FH, TextureFormat.RGB24, false);
                img.ReadPixels(new Rect(0, 0, FW, FH), 0, 0);
                img.Apply();
                sheet.SetPixels((i - 1) * FW, 0, FW, FH, img.GetPixels());
                Object.DestroyImmediate(img);
            }
            AnimationMode.StopAnimationMode();
            RenderTexture.active = null;
            sheet.Apply();
            System.IO.File.WriteAllBytes(outPath, sheet.EncodeToPNG());
            QualitySettings.SetQualityLevel(editorQuality, true);
            Debug.Log($"[BL-354] creation preview ({picks.Length}) -> {outPath}");
        }

        /// <summary>Logs what each outfit FBX holds — renderers, their materials, the avatar, the height — so the
        /// material table can be written against real names instead of guesses.</summary>
        [MenuItem("Tools/BL-93/Inspect outfit bodies")]
        public static void InspectOutfits()
        {
            foreach (var path in AssetDatabase.FindAssets("t:Model", new[] { OutfitDir, OutfitHeadDir }).Select(AssetDatabase.GUIDToAssetPath))
            {
                PrepareCharacterBody(path);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                var lines = new List<string>();
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var smr = r as SkinnedMeshRenderer;
                    var mesh = smr != null ? smr.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                    int tris = mesh == null ? 0 : (int)Enumerable.Range(0, mesh.subMeshCount).Sum(i => (long)mesh.GetIndexCount(i)) / 3;
                    lines.Add($"    {r.name} [{r.GetType().Name}] tris={tris} bounds={r.bounds.size} mats=[" +
                              string.Join(", ", r.sharedMaterials.Select(m => m == null ? "null" : m.name +
                                  (m.mainTexture != null ? "(" + m.mainTexture.name + ")" : ""))) + "]");
                }
                Debug.Log($"[OUTFIT] {path}: avatar={(avatar == null ? "none" : avatar.isHuman ? "human" : "generic")} " +
                          $"height={MeshHeight(go):0.000}\n" + string.Join("\n", lines));
            }
        }
    }
}
