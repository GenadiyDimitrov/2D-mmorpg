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
            new OutfitMat("MI_Eyes",            "T_Eye_Brown",                   null,                       128),
            new OutfitMat("MI_Hair_1",          "T_Hair_1_BaseColor",            null,                       512, clip: true,
                          tint: new Color(0.42f, 0.27f, 0.16f)),   // the map is a grey mask the pack's own shader colours
        };

        private static Dictionary<string, Material> _outfitMats;

        /// <summary>Puts a head, eyes, brows and hair on an instantiated outfit and swaps every material for ours.</summary>
        private static void DressOutfit(GameObject inst, Body b)
        {
            _outfitMats ??= BuildOutfitMaterials();
            var bones = inst.GetComponentsInChildren<Transform>(true)
                            .GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());

            var baseBody = AssetDatabase.LoadAssetAtPath<GameObject>(BaseBodyFbx);
            if (baseBody == null) { Debug.LogError($"[BL-93] Missing {BaseBodyFbx}"); return; }
            foreach (var src in baseBody.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                // The body mesh contributes its head only; the eyes and brows come over whole.
                var mesh = src.name.StartsWith("SuperHero") ? HeadMesh(src) : src.sharedMesh;
                if (mesh != null) Rebind(inst, bones, src, mesh, src.name.StartsWith("SuperHero") ? "Head" : src.name);
            }
            foreach (var hair in b.Hair)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{OutfitHeadDir}/{hair}.fbx");
                var src = fbx != null ? fbx.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
                if (src == null) { Debug.LogError($"[BL-93] {b.Key}: hair {hair} not found."); continue; }
                Rebind(inst, bones, src, src.sharedMesh, hair);
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
        private static void Rebind(GameObject inst, Dictionary<string, Transform> bones, SkinnedMeshRenderer src,
                                   Mesh mesh, string name)
        {
            var mapped = new Transform[src.bones.Length];
            for (int i = 0; i < mapped.Length; i++)
            {
                if (src.bones[i] == null || !bones.TryGetValue(src.bones[i].name, out mapped[i]))
                {
                    Debug.LogError($"[BL-93] {name}: bone '{(src.bones[i] == null ? "null" : src.bones[i].name)}' " +
                                   "is not on the outfit's skeleton.");
                    return;
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
        }

        /// <summary>The triangles of the base body that its head and neck bones own (every corner ≥ half-weighted to
        /// them), as a compacted mesh asset. Rebuilt every run so a new base body needs nothing by hand.</summary>
        private static Mesh HeadMesh(SkinnedMeshRenderer body)
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

            var head = new Mesh { name = "Head_Male" };
            head.SetVertices(verts);
            if (normals.Count > 0) head.SetNormals(normals);
            if (tangents.Count > 0) head.SetTangents(tangents);
            if (uvs.Count > 0) head.SetUVs(0, uvs);
            head.SetTriangles(tris, 0);
            head.boneWeights = bw.ToArray();
            head.bindposes = src.bindposes;
            head.RecalculateBounds();

            string path = $"{OutfitHeadDir}/Head_Male.asset";
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
