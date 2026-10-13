using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Client;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.ClientEditor
{
    /// <summary>
    /// `BL-356`: the town buildings and the forest hut, ASSEMBLED from the Quaternius Medieval Village MegaKit (CC0) —
    /// the free kit ships wall, roof and trim pieces, not finished houses — and BAKED to one mesh each, so a house is one
    /// renderer with a submesh per material, not thirty objects. Plus the Anvil's stone (`Models/npc_anvil`).
    ///
    /// <code>
    /// "C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" -batchmode -quit -nographics ^
    ///     -projectPath G:\Work\Repository\L2Clone\Game.Client.Unity ^
    ///     -executeMethod Game.ClientEditor.ModelSetup.BuildBuildings -logFile -
    /// </code>
    ///
    /// <para>🔑 <b>The kit's grid:</b> a wall piece is 2 m wide (x −1..1), 3 m a storey (3.12 with its top trim), its
    /// OUTSIDE face toward +Z (the shutters sit there). Roofs are named by the wall footprint they cover, W along X and
    /// D along Z (<c>Roof_RoundTiles_6x10</c>), with the gable triangles separate (<c>Roof_Front_Brick6</c>). Measured off
    /// the kit's glTF before any of this was written. A recipe below is a footprint plus one string per side —
    /// <c>D</c> door, <c>W</c> wide window, <c>T</c> thin window, <c>G</c> timber grid, <c>P</c> plain — so a house
    /// reads like its own floor plan.</para>
    ///
    /// <para>The prefab NAME ends in <c>_WxD</c> (the wall footprint in metres): <see cref="Buildings"/> reads it to fit
    /// the model to a lot. Front door on +Z. Materials follow the props' rules (<see cref="BuildPropMaterial"/>): ours,
    /// shipped as assets, textures capped at 512, the FBX's own remapped away. Two-sided, as the kit authored them.</para>
    /// </summary>
    public static partial class ModelSetup
    {
        private const string VillageDir = ArtDir + "/Village";
        private const string VillageFbx = VillageDir + "/FBX";
        private const string VillageTex = VillageDir + "/Textures";
        private const string VillageMats = VillageDir + "/Materials";
        private const string VillageBaked = VillageDir + "/Baked";
        private const string BuildingsDir = "Assets/Resources/Buildings";

        /// <summary>The kit's material names → texture (null = flat colour) and tint.</summary>
        private static readonly (string Name, string Texture, Color Tint)[] VillageMaterials =
        {
            ("MI_Plaster",       "T_Plaster_BaseColor.png",     Color.white),
            ("MI_WoodTrim",      "T_WoodTrim_BaseColor.png",    Color.white),
            ("MI_WoodTrim_Wear", "T_WoodTrim_BaseColor.png",    new Color(0.85f, 0.82f, 0.78f)),
            ("MI_RockTrim",      "T_RockTrim_BaseColor.png",    Color.white),
            ("MI_UnevenBrick",   "T_UnevenBrick_BaseColor.png", Color.white),
            ("MI_Brick",         "T_Brick_BaseColor.png",       Color.white),
            ("MI_RoundTiles",    "T_RoundTiles_BaseColor.png",  Color.white),
            // The kit's glass is a near-transparent blend; from above an opaque dark pane reads the same and costs nothing.
            ("MI_WindowGlass",   null,                          new Color(0.16f, 0.20f, 0.24f)),
        };

        // ── a recipe ──────────────────────────────────────────────────────────────────────────

        private sealed class Plan
        {
            public readonly string Name;
            public readonly int W, D;
            public readonly List<(string Piece, Matrix4x4 M)> Parts = new List<(string, Matrix4x4)>();
            public Plan(string name, int w, int d) { Name = name + "_" + w + "x" + d; W = w; D = d; }

            public void Put(string piece, Vector3 pos, float yaw, Vector3? scale = null) =>
                Parts.Add((piece, Matrix4x4.TRS(pos, Quaternion.Euler(0f, yaw, 0f), scale ?? Vector3.one)));

            /// <summary>One storey of walls at height <paramref name="y"/>. Each string is one character per 2 m cell:
            /// front (+Z) left→right, then right (+X), back (−Z) and left (−X), each read clockwise from above.</summary>
            public void Storey(float y, bool stone, string front, string right, string back, string left, bool shutters = false)
            {
                Side(front, y, stone, shutters, i => new Vector3(-W / 2f + 1 + 2 * i, y, D / 2f), 0f);
                Side(right, y, stone, shutters, i => new Vector3(W / 2f, y, D / 2f - 1 - 2 * i), 90f);
                Side(back, y, stone, shutters, i => new Vector3(W / 2f - 1 - 2 * i, y, -D / 2f), 180f);
                Side(left, y, stone, shutters, i => new Vector3(-W / 2f, y, -D / 2f + 1 + 2 * i), 270f);
                foreach (var (cx, cz) in new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) })
                    Put("Corner_ExteriorWide_Wood", new Vector3(cx * W / 2f, y, cz * D / 2f), 0f);
            }

            private void Side(string cells, float y, bool stone, bool shutters, System.Func<int, Vector3> at, float yaw)
            {
                string wall = stone ? "Wall_UnevenBrick_" : "Wall_Plaster_";
                var rot = Quaternion.Euler(0f, yaw, 0f);
                for (int i = 0; i < cells.Length; i++)
                {
                    var p = at(i);
                    switch (cells[i])
                    {
                        case 'D':
                            Put(wall + "Door_Flat", p, yaw);
                            Put("Door_1_Flat", p + rot * new Vector3(-0.51f, 0f, 0f), yaw);
                            break;
                        case 'R':   // a round-topped door
                            Put(wall + "Door_Round", p, yaw);
                            Put("Door_1_Round", p + rot * new Vector3(-0.51f, 0f, 0f), yaw);
                            break;
                        case 'W':
                            Put(wall + "Window_Wide_Flat", p, yaw);
                            Put("Window_Wide_Flat1", p, yaw);
                            if (shutters) Put("WindowShutters_Wide_Flat_Open", p, yaw);
                            break;
                        case 'T':
                            Put(wall + "Window_Thin_Round", p, yaw);
                            Put("Window_Thin_Round1", p, yaw);
                            if (shutters) Put("WindowShutters_Thin_Round_Closed", p, yaw);
                            break;
                        case 'G':
                            Put(stone ? "Wall_UnevenBrick_Straight" : "Wall_Plaster_WoodGrid", p, yaw);
                            break;
                        default:
                            Put(wall + "Straight", p, yaw);
                            break;
                    }
                }
            }

            /// <summary>The tiled roof for this footprint on walls ending at <paramref name="top"/>, with its two gables.</summary>
            public void Roof(float top)
            {
                Put($"Roof_RoundTiles_{W}x{D}", new Vector3(0f, top, 0f), 0f);
                Put($"Roof_Front_Brick{W}", new Vector3(0f, top, D / 2f), 0f);
                Put($"Roof_Front_Brick{W}", new Vector3(0f, top, -D / 2f), 180f);
            }

            /// <summary>A chimney through the roof slope at (x, z), its foot buried under the tiles.</summary>
            public void Chimney(float top, float x, float z, bool stone = false)
            {
                // Measured roof profiles: ridge height and half-span (with the overhang) per width, eaves 0.52-0.78 down.
                float ridge = W == 4 ? 3.73f : W == 6 ? 4.89f : 6.0f, half = W == 4 ? 2.76f : W == 6 ? 4.12f : 4.98f;
                float eave = W == 4 ? 0.52f : 0.78f;
                float surface = ridge - (ridge + eave) * Mathf.Abs(x) / half;
                Put(stone ? "Prop_Chimney2" : "Prop_Chimney", new Vector3(x, top + surface - 0.9f, z), 0f);
            }
        }

        /// <summary>
        /// 🔑 THE BUILDINGS. <see cref="Buildings"/> maps each lot kind to some of these and picks the largest that fits;
        /// a new one is a recipe here plus its name in that table.
        /// </summary>
        private static IEnumerable<Plan> BuildingPlans()
        {
            // A plaster cottage: the commonest house.
            var p = new Plan("Cottage", 4, 4);
            p.Storey(0f, false, "DW", "TP", "WP", "PT");
            p.Roof(3f); p.Chimney(3f, 1f, -1f);
            yield return p;

            // Stone below, timber-framed plaster above, shutters: the town house.
            p = new Plan("Townhouse", 4, 4);
            p.Storey(0f, true, "DW", "TP", "PW", "PT");
            p.Storey(3f, false, "WW", "TG", "GW", "GT", shutters: true);
            p.Roof(6f); p.Chimney(6f, -1f, 1f, stone: true);
            yield return p;

            // A long stone house.
            p = new Plan("Stonehouse", 4, 6);
            p.Storey(0f, true, "DW", "TPT", "WP", "PWP");
            p.Roof(3f); p.Chimney(3f, 1f, -2f, stone: true);
            yield return p;

            p = new Plan("Longhouse", 4, 8);
            p.Storey(0f, false, "DW", "TWPT", "WP", "PTWT");
            p.Roof(3f); p.Chimney(3f, -1f, 2.5f);
            yield return p;

            // The shop: three bays, the door in the middle, living space above.
            p = new Plan("Shop", 6, 6);
            p.Storey(0f, true, "WDW", "WPW", "PWP", "TPT");
            p.Storey(3f, false, "WGW", "TGT", "GWG", "GTG", shutters: true);
            p.Roof(6f); p.Chimney(6f, 2f, -1.5f);
            yield return p;

            // The crafthall: one tall storey of stone, two chimneys for the forges.
            p = new Plan("Hall", 6, 10);
            p.Storey(0f, true, "WDW", "WPWPW", "TPT", "TPWPT");
            p.Roof(3f); p.Chimney(3f, 2f, 3f, stone: true); p.Chimney(3f, -2f, -3f, stone: true);
            p.Put("Prop_Crate", new Vector3(3.8f, 0f, 3.6f), 15f);
            yield return p;

            // The Adventurers Guild: the biggest house in town.
            p = new Plan("Guild", 8, 8);
            p.Storey(0f, true, "WRWW", "WPTW", "WPWT", "TWPW");
            p.Storey(3f, false, "WGWG", "GWGW", "WGWG", "GWGW", shutters: true);
            p.Roof(6f); p.Chimney(6f, 2.5f, -2f);
            yield return p;

            // A church arm: tall, plaster, thin round windows. Two of these crossed make the church (its two lots).
            p = new Plan("Chapel", 4, 8);
            p.Storey(0f, false, "RT", "TTTT", "TT", "TTTT");
            p.Storey(3f, false, "TT", "GTGT", "TT", "TGTG");
            p.Roof(6f);
            yield return p;

            // The shrine: a roof on four posts over a paved floor.
            p = new Plan("Pavilion", 4, 4);
            foreach (var (cx, cz) in new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) })
            {
                p.Put("Corner_ExteriorWide_Wood", new Vector3(cx * 2f, 0f, cz * 2f), 0f);
                p.Put("Floor_Brick", new Vector3(cx, 0.02f, cz), 0f);
            }
            p.Roof(3f);
            yield return p;

            // The anvil yard: fenced on three sides, open toward the Master Crafter, a few crates in the back.
            p = new Plan("Yard", 4, 4);
            foreach (float s in new[] { -1f, 1f })
            {
                p.Put("Prop_WoodenFence_Single", new Vector3(s, 0f, -2f), 0f);
                p.Put("Prop_WoodenFence_Single", new Vector3(2f, 0f, s), 90f);
                p.Put("Prop_WoodenFence_Single", new Vector3(-2f, 0f, s), 90f);
            }
            p.Put("Prop_Crate", new Vector3(-1.3f, 0f, -1.3f), 10f, Vector3.one * 0.8f);
            p.Put("Prop_Crate", new Vector3(1.4f, 0f, -1.4f), -20f, Vector3.one * 0.7f);
            yield return p;

            // The forest hut: one storey of stone, a woodpile and a stretch of fence.
            p = new Plan("Hut", 4, 4);
            p.Storey(0f, true, "DP", "TP", "PT", "PP");
            p.Roof(3f); p.Chimney(3f, -1f, -1f, stone: true);
            p.Put("Prop_Crate", new Vector3(2.8f, 0f, 1.2f), 20f, Vector3.one * 0.8f);
            p.Put("Prop_Crate", new Vector3(2.7f, 0f, 0.1f), -10f, Vector3.one * 0.6f);
            p.Put("Prop_WoodenFence_Single", new Vector3(-2.2f, 0f, 3.2f), 0f);
            p.Put("Prop_WoodenFence_Single", new Vector3(-3.2f, 0f, 2.2f), 90f);
            yield return p;
        }

        // ── building them ─────────────────────────────────────────────────────────────────────

        [MenuItem("Tools/BL-93/Build town buildings (BL-356)")]
        public static void BuildBuildings()
        {
            bool ok = BuildBuildingPrefabs() && BuildAnvil();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!ok && Application.isBatchMode) EditorApplication.Exit(1);
        }

        private static bool BuildBuildingPrefabs()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) { Debug.LogError("[BL-356] URP Lit shader not found."); return false; }

            Directory.CreateDirectory(VillageMats);
            var mats = new Dictionary<string, Material>();
            foreach (var m in VillageMaterials)
            {
                var mat = BuildVillageMaterial(m.Name, m.Texture, m.Tint, lit);
                if (mat == null) return false;
                mats[m.Name] = mat;
            }

            var plans = BuildingPlans().ToList();
            var pieces = new Dictionary<string, GameObject>();
            foreach (var name in plans.SelectMany(pl => pl.Parts.Select(x => x.Piece)).Distinct())
            {
                var go = ImportVillagePiece(name, mats);
                if (go == null) return false;
                pieces[name] = go;
            }

            if (AssetDatabase.IsValidFolder(BuildingsDir)) AssetDatabase.DeleteAsset(BuildingsDir);
            AssetDatabase.CreateFolder("Assets/Resources", "Buildings");
            if (AssetDatabase.IsValidFolder(VillageBaked)) AssetDatabase.DeleteAsset(VillageBaked);
            AssetDatabase.CreateFolder(VillageDir, "Baked");

            int built = 0;
            foreach (var pl in plans)
            {
                var parts = pl.Parts.Select(x => (pieces[x.Piece], x.M));
                if (SaveBaked(pl.Name, parts, BuildingsDir, out var info)) { built++; Debug.Log($"[BL-356] {pl.Name}: {info}"); }
            }
            Debug.Log($"[BL-356] built {built}/{plans.Count} building prefabs.");
            return built == plans.Count;
        }

        /// <summary>The Anvil (`BL-356`): a squat block of the nature kit's medium rock instead of a man. ~0.75 m tall
        /// and a metre across, its base on y = 0 like a body's feet.</summary>
        private static bool BuildAnvil()
        {
            var rock = AssetDatabase.LoadAssetAtPath<GameObject>(PropsDir + "/Rock/Rock_Medium_1.prefab");
            if (rock == null) { Debug.LogError("[BL-356] Props/Rock/Rock_Medium_1.prefab missing — run BuildProps first."); return false; }
            var b = MeshBounds(rock);
            float sx = 1.0f / Mathf.Max(0.01f, b.size.x), sy = 0.75f / Mathf.Max(0.01f, b.size.y), sz = 0.8f / Mathf.Max(0.01f, b.size.z);
            var m = Matrix4x4.Scale(new Vector3(sx, sy, sz)) * Matrix4x4.Translate(new Vector3(-b.center.x, -b.min.y, -b.center.z));
            bool ok = SaveBaked("npc_anvil", new[] { (rock, m) }, ModelsDir, out var info);
            if (ok) Debug.Log($"[BL-356] npc_anvil: {info}");
            return ok;
        }

        private static Material BuildVillageMaterial(string name, string texture, Color tint, Shader lit)
        {
            Texture2D tex = null;
            if (texture != null)
            {
                string texPath = $"{VillageTex}/{texture}";
                if (AssetImporter.GetAtPath(texPath) is not TextureImporter ti)
                {
                    Debug.LogError($"[BL-356] Missing texture {texPath} for {name}.");
                    return null;
                }
                if (ti.maxTextureSize != 512) { ti.maxTextureSize = 512; ti.SaveAndReimport(); }
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            }

            string path = $"{VillageMats}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(lit); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = lit;
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
            mat.SetColor("_BaseColor", tint);
            mat.SetColor("_Color", tint);
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.1f);
            mat.SetFloat("_SpecularHighlights", 0f);
            mat.SetFloat("_EnvironmentReflections", 0f);
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            mat.SetFloat("_ReceiveShadows", 0f);
            mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            mat.SetFloat("_AlphaClip", 0f);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_Cull", (float)CullMode.Off);   // the kit is authored double-sided (gables and trims are cards)
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = (int)RenderQueue.Geometry;
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject ImportVillagePiece(string piece, Dictionary<string, Material> mats)
        {
            string fbxPath = $"{VillageFbx}/{piece}.fbx";
            if (AssetImporter.GetAtPath(fbxPath) is not ModelImporter imp)
            {
                Debug.LogError($"[BL-356] Not an importable model: {fbxPath} (copy it from the kit's FBX folder).");
                return null;
            }
            imp.animationType = ModelImporterAnimationType.None;
            imp.importAnimation = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.importBlendShapes = false;
            imp.isReadable = true;   // the bake reads it; the FBX itself never ships (only the baked mesh does)
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
            imp.SaveAndReimport();

            var names = new HashSet<string>();
            foreach (var mi in imp.GetExternalObjectMap())
                if (mi.Key.type == typeof(Material)) names.Add(mi.Key.name);
            foreach (var m in AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>()) names.Add(m.name);
            foreach (var n in names)
            {
                if (!mats.TryGetValue(n, out var ours))
                {
                    Debug.LogError($"[BL-356] {piece}: material '{n}' has no entry in VillageMaterials.");
                    return null;
                }
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), ours);
            }
            imp.SaveAndReimport();

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var b = MeshBounds(go);
            Debug.Log($"[BL-356] piece {piece}: x[{b.min.x:0.00},{b.max.x:0.00}] y[{b.min.y:0.00},{b.max.y:0.00}] z[{b.min.z:0.00},{b.max.z:0.00}]");
            return go;
        }

        /// <summary>The bounds of every mesh in a prefab asset, in the prefab root's own space.</summary>
        private static Bounds MeshBounds(GameObject prefab)
        {
            var root = Matrix4x4.Translate(-prefab.transform.position);   // keep the root's own rotation + unit scale: the kit's FBXs carry their Z-up turn and cm→m there
            var result = new Bounds();
            bool first = true;
            foreach (var f in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null) continue;
                var m = root * f.transform.localToWorldMatrix;
                var mb = f.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(c);
                    if (first) { result = new Bounds(p, Vector3.zero); first = false; } else result.Encapsulate(p);
                }
            }
            return result;
        }

        /// <summary>Bakes the parts (each a prefab asset placed by a matrix) into ONE mesh with a submesh per material,
        /// saves it under <see cref="VillageBaked"/>, and a prefab of it at <paramref name="dir"/>/<paramref name="name"/>.</summary>
        private static bool SaveBaked(string name, IEnumerable<(GameObject Src, Matrix4x4 M)> parts, string dir, out string info)
        {
            info = "";
            var byMat = new Dictionary<Material, List<CombineInstance>>();
            foreach (var (src, place) in parts)
            {
                var root = Matrix4x4.Translate(-src.transform.position);   // see MeshBounds: the root's rotation and scale are part of the piece
                foreach (var f in src.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r = f.GetComponent<MeshRenderer>();
                    if (f.sharedMesh == null || r == null) continue;
                    var m = place * root * f.transform.localToWorldMatrix;
                    for (int s = 0; s < f.sharedMesh.subMeshCount; s++)
                    {
                        var mat = r.sharedMaterials[Mathf.Min(s, r.sharedMaterials.Length - 1)];
                        if (mat == null) { Debug.LogError($"[BL-356] {name}: {src.name} has a null material."); return false; }
                        if (!byMat.TryGetValue(mat, out var list)) byMat[mat] = list = new List<CombineInstance>();
                        list.Add(new CombineInstance { mesh = f.sharedMesh, subMeshIndex = s, transform = m });
                    }
                }
            }
            if (byMat.Count == 0) { Debug.LogError($"[BL-356] {name}: nothing to bake."); return false; }

            var materials = byMat.Keys.OrderBy(m => m.name).ToArray();
            var perMat = new List<CombineInstance>();
            int verts = 0;
            foreach (var mat in materials)
            {
                var sub = new Mesh { indexFormat = IndexFormat.UInt32 };
                sub.CombineMeshes(byMat[mat].ToArray(), true, true);
                verts += sub.vertexCount;
                perMat.Add(new CombineInstance { mesh = sub, transform = Matrix4x4.identity });
            }
            var mesh = new Mesh { name = name, indexFormat = verts > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.CombineMeshes(perMat.ToArray(), false, false);
            foreach (var c in perMat) Object.DestroyImmediate(c.mesh);
            mesh.RecalculateBounds();
            MeshUtility.Optimize(mesh);
            MeshUtility.SetMeshCompression(mesh, ModelImporterMeshCompression.Medium);


            string meshPath = $"{VillageBaked}/{name}.asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);

            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterials = materials;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/{name}.prefab", out bool ok);
            Object.DestroyImmediate(go);

            int tris = 0;
            for (int s = 0; s < mesh.subMeshCount; s++) tris += (int)mesh.GetIndexCount(s) / 3;
            var b = mesh.bounds;
            info = $"{tris} tris, {verts} verts, {materials.Length} materials, size {b.size.x:0.0}x{b.size.y:0.0}x{b.size.z:0.0}";
            if (!ok) Debug.LogError($"[BL-356] {name}: SaveAsPrefabAsset failed.");
            return ok;
        }

        // ── preview ───────────────────────────────────────────────────────────────────────────

        /// <summary>Two renders, no Editor and no phone: every building in a row (tilted), and Brackenford from above
        /// with the buildings where <see cref="Buildings"/> puts them, its footprints and its NPCs. Batchmode WITHOUT
        /// <c>-nographics</c>; files go to <c>-previewOut &lt;dir&gt;</c>.</summary>
        public static void PreviewBuildings()
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-previewOut");
            string outDir = at >= 0 && at + 1 < args.Length ? args[at + 1] : ".";

            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);

            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.55f, 0.40f);
            cam.orthographic = true;
            cam.farClipPlane = 500f;

            // 1) the row, every model at the kit's own scale, with a 1.8 m post for a person.
            var names = BuildingPlans().Select(pl => pl.Name).Concat(new[] { "npc_anvil" }).ToList();
            float x = 0f;
            foreach (var n in names)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(n == "npc_anvil" ? $"{ModelsDir}/{n}.prefab" : $"{BuildingsDir}/{n}.prefab");
                if (pf == null) { Debug.LogError($"[BL-356] preview: {n} missing"); continue; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, scene);
                float w = Mathf.Max(2f, go.GetComponent<MeshRenderer>().bounds.size.x);
                go.transform.position = new Vector3(x + w / 2f, 0f, 0f);
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.transform.localScale = new Vector3(0.3f, 1.8f, 0.3f);
                post.transform.position = new Vector3(x + w / 2f, 0.9f, 3.5f);
                x += w + 2f;
            }
            Shot(cam, outDir + "/buildings-row.png", 3200, 900, new Vector3(x / 2f, 4f, -30f), Quaternion.Euler(20f, 0f, 0f), x * 900f / 3200f / 2f + 1f);
            foreach (var go in scene.GetRootGameObjects()) if (go != sun.gameObject && go != cam.gameObject) Object.DestroyImmediate(go);

            // 2) every town from above, as the game places them: footprints (grey), buildings, NPCs (red posts).
            foreach (var plan in Game.Shared.TownLayout.Plans)
            {
                var c = WorldMapper.ToUnity(plan.Centre.X, plan.Centre.Y);
                var holder = new GameObject("Town " + plan.TownId);
                foreach (var lot in plan.Lots)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    q.transform.SetParent(holder.transform);
                    q.transform.position = WorldMapper.ToUnity(lot.Centre.X, lot.Centre.Y) + Vector3.down * 0.05f;
                    q.transform.rotation = Quaternion.Euler(0f, lot.AngleDeg, 0f);
                    q.transform.localScale = new Vector3(lot.W * WorldMapper.Scale, 0.1f, lot.H * WorldMapper.Scale);
                }
                foreach (var s in plan.Roads)
                {
                    var a = WorldMapper.ToUnity(s.A.X, s.A.Y); var bb = WorldMapper.ToUnity(s.B.X, s.B.Y);
                    var q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    q.transform.SetParent(holder.transform);
                    q.transform.position = (a + bb) / 2f + Vector3.down * 0.08f;
                    q.transform.rotation = Quaternion.LookRotation(bb - a);
                    q.transform.localScale = new Vector3(s.Width * WorldMapper.Scale, 0.05f, (bb - a).magnitude);
                }
                foreach (var p in Buildings.All)
                {
                    if ((p.Position - c).magnitude > 40f) continue;
                    var pf = AssetDatabase.LoadAssetAtPath<GameObject>($"{BuildingsDir}/{p.Model}.prefab");
                    if (pf == null) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, scene);
                    go.transform.SetParent(holder.transform);
                    go.transform.SetPositionAndRotation(p.Position, p.Rotation);
                    go.transform.localScale = p.Scale;
                }
                foreach (var n in Game.Shared.WorldMap.Npcs)
                {
                    var np = WorldMapper.ToUnity(n.X, n.Y);
                    if ((np - c).magnitude > 40f) continue;
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.transform.SetParent(holder.transform);
                    post.transform.position = np + Vector3.up * 0.8f;
                    post.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
                    var r = post.GetComponent<MeshRenderer>();
                    var red = new Material(Shader.Find("Universal Render Pipeline/Unlit")); red.SetColor("_BaseColor", Color.red); r.sharedMaterial = red;
                }
                Shot(cam, $"{outDir}/town-{plan.TownId}.png", 1400, 1400, c + Vector3.up * 100f, Quaternion.Euler(90f, 0f, 0f), 30f);
                Shot(cam, $"{outDir}/town-{plan.TownId}-tilt.png", 1600, 1000, c + new Vector3(0f, 30f, -32f), Quaternion.Euler(42f, 0f, 0f), 14f);
                Object.DestroyImmediate(holder);
            }
            int huts = Buildings.All.Count(p => p.Hut);
            Debug.Log($"[BL-356] preview done: {Buildings.All.Count} placed, {huts} huts -> {outDir}");
            foreach (var p in Buildings.All.Where(p => p.Hut))
                Debug.Log($"[BL-356] hut at server {WorldMapper.ToServer(p.Position)}");
        }

        private static void Shot(Camera cam, string path, int w, int h, Vector3 pos, Quaternion rot, float size)
        {
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.orthographicSize = size;
            var rt = new RenderTexture(w, h, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();   // warm-up: the FIRST render draws before textures upload
            cam.Render();
            RenderTexture.active = rt;
            var img = new Texture2D(w, h, TextureFormat.RGB24, false);
            img.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            img.Apply();
            File.WriteAllBytes(path, img.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(img);
        }
    }
}
