using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.ClientEditor
{
    /// <summary>
    /// `BL-93` / `BL-102` — BUILDS THE CREATURE **AND CHARACTER** PREFABS AND THEIR ANIMATOR
    /// CONTROLLERS, HEADLESSLY.
    ///
    /// <para><b>Two halves, one command.</b> <see cref="BuildAll"/> does both:</para>
    /// <list type="bullet">
    /// <item><b>Creatures</b> (`BL-93`) — a row in <see cref="Families"/> per <c>MobCategory</c>. The
    /// monster FBXs ship their own Idle/Walk/Run/Attack/Death takes, so a family needs nothing but its
    /// line.</item>
    /// <item><b>Characters</b> (`BL-102`) — a row in <see cref="Bodies"/> per player body. 🔴 <b>The
    /// character FBXs ship ZERO animation</b> (measured: <c>AnimationStack</c> count 0 in all 21), so
    /// the clips come from a SEPARATE source folder, <see cref="CharAnimDir"/>, and are retargeted onto
    /// the body by its Humanoid avatar. <b>Drop clip files in that folder, re-run, done</b> — see
    /// <see cref="BuildCharacters"/> for exactly what a clip file has to be called.</item>
    /// </list>
    ///
    /// <para>Run with the Editor CLOSED:</para>
    /// <code>
    /// "C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" -batchmode -quit -nographics ^
    ///     -projectPath G:\Work\Repository\L2Clone\Game.Client.Unity ^
    ///     -executeMethod Game.ClientEditor.ModelSetup.BuildAll -logFile -
    /// </code>
    ///
    /// <para>🔑 <b>WHY THIS EXISTS RATHER THAN A HAND-WRITTEN .prefab:</b> a prefab that references an
    /// imported model does it by <c>fileID</c> into the FBX's generated sub-assets. Those ids are not
    /// stored anywhere readable — they are produced by the importer — so hand-writing the YAML means
    /// guessing them, and a wrong guess does not error: it produces a prefab with a missing child that
    /// loads fine and draws nothing. On a phone, with no PC to check it on, that failure is invisible.
    /// Letting the Editor build them in batchmode makes the ids correct by construction.</para>
    ///
    /// <para>🔑 <b>ADDING A FAMILY IS ONE LINE IN <see cref="Families"/>.</b> The key is the file name
    /// <c>ModelLibrary.Keys</c> already asks for — <c>mob_&lt;category&gt;</c>, lowercase, matching
    /// <c>MobCategory</c> — so a new row peels that category off the shared humanoid body with no code
    /// change anywhere else. That is the whole point of the fallback chain.</para>
    ///
    /// <para>Idempotent: it overwrites its own outputs and touches nothing else, so re-running after
    /// dropping in a better FBX is the update path.</para>
    /// </summary>
    public static class ModelSetup
    {
        /// <summary>One creature family: the prefab key, the source FBX, and how tall the thing should
        /// stand in Unity units at the root's own scale.
        ///
        /// <para>⚠ <b>Scale is authored, not inherited.</b> Asset packs each pick their own units and
        /// these monsters do not agree with each other or with the character pack. The entity root is
        /// scaled by the player's "Entity size" slider and the model hangs off it, so a creature that
        /// imports at 4× swamps the screen and one at 0.2× is invisible — and neither is something the
        /// slider should have to compensate for. Normalising to a stated height here is what makes
        /// "tint + scale separate the members of a family" a knob rather than a guess.</para></summary>
        private readonly struct Family
        {
            public readonly string Key;
            public readonly string Fbx;
            public readonly float Height;

            public Family(string key, string fbx, float height) { Key = key; Fbx = fbx; Height = height; }
        }

        private const string ModelsDir = "Assets/Resources/Models";

        /// <summary>
        /// 🔑 THE TWO HE ASKED FOR, chosen by WHAT A NEW CHARACTER ACTUALLY MEETS, not by which mesh
        /// looks best in a screenshot. Animal covers the first creature in the game (Ridgeback Pup,
        /// Lv 1) plus Fox at 4 and Ashen Wolf at 10 — 11 templates; Insect covers Hook Spider at 14 —
        /// 9 templates. Between them a fresh character stops seeing human bodies on wildlife within
        /// its first hour, which is the question the proof of concept is asking.
        ///
        /// <para>The remaining seven families are a line each and the FBXs for four of them are already
        /// committed: Undead → Skeleton (9 templates, from Lv 18), Dragon → Dragon (5, from Lv 35),
        /// and Bat/Frog/Slime/Snake/Wasp are spare bodies for whichever family wants them. Humanoid
        /// deliberately has NO row — it is the 40-template majority and it is what
        /// <c>Models/humanoid.prefab</c> already serves correctly.</para>
        /// </summary>
        private static readonly Family[] Families =
        {
            new Family("mob_animal", ModelsDir + "/Monsters/Rat.fbx",    1.1f),
            new Family("mob_insect", ModelsDir + "/Monsters/Spider.fbx", 1.0f),
        };

        /// <summary>Clip names that must LOOP. Imported takes do not loop by default, so an idle plays
        /// once and the creature freezes in whatever pose the last frame left it in — which reads as a
        /// broken model, not a missing setting.
        ///
        /// <para>⚠ Matched as a SUBSTRING of the lower-cased clip name, so <c>Walking</c>,
        /// <c>Run_Fwd</c> and <c>Standing Idle</c> all land — which is what lets a Mixamo file name be
        /// dropped in unrenamed. <b>Attack and death are deliberately absent</b>: a looping death
        /// re-kills the corpse forever.</para></summary>
        private static readonly string[] Looping = { "idle", "walk", "run", "flying", "cast" };

        /// <summary>
        /// 🔴 `BL-102` — ONE PLAYER BODY PER ROW, and the animation comes from somewhere else.
        ///
        /// <para>The key is what <c>ModelLibrary.Keys</c> asks for, walking most-specific to least:
        /// <c>player_&lt;race&gt;_&lt;class&gt;</c> → <c>player_&lt;class&gt;</c> → <c>player</c> →
        /// <c>humanoid</c>. <b>Only <c>humanoid</c> exists today</b> — it is the universal last resort
        /// and therefore dresses every player, NPC and humanoid mob at once, which is exactly what the
        /// proof of concept wants before anyone commissions per-race bodies.</para>
        ///
        /// <para>⚠ <b>Height 0 means "leave the model's own scale alone".</b> The creature rows
        /// normalise because the monster packs disagree wildly on units; the character body has already
        /// been looked at on the phone at its native scale and a number here would silently move it.
        /// Author a real height only when a NEW body imports at the wrong size.</para>
        /// </summary>
        private static readonly Body[] Bodies =
        {
            new Body("humanoid", ModelsDir + "/Characters/Man/Adventurer.fbx", 0f),
        };

        /// <summary>Where the character clips live. 🔑 <b>This folder is the whole of `BL-102`</b> — the
        /// bodies are rigged, avatared and committed; what is missing is motion, and motion is files in
        /// here. Anything Unity can import as a model is read: one multi-take FBX, or one file per
        /// action, or both.</summary>
        private const string CharAnimDir = ModelsDir + "/Characters/Animations";

        /// <summary>A player body: the prefab key, the source FBX, and its height in Unity units — or
        /// <c>0</c> to keep whatever the model imports at. See <see cref="Bodies"/>.</summary>
        private readonly struct Body
        {
            public readonly string Key;
            public readonly string Fbx;
            public readonly float Height;

            public Body(string key, string fbx, float height) { Key = key; Fbx = fbx; Height = height; }
        }

        [MenuItem("Tools/BL-93/Build ALL prefabs (creatures + characters)")]
        public static void BuildAll()
        {
            bool ok = BuildFamilies();
            ok &= BuildCharacterBodies();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // A non-zero exit code is the only thing a headless caller can see. Without this a failed
            // import is a green build that ships an APK with no models in it.
            if (!ok && Application.isBatchMode) EditorApplication.Exit(1);
        }

        [MenuItem("Tools/BL-93/Build creature prefabs")]
        public static void BuildCreatures()
        {
            bool ok = BuildFamilies();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!ok && Application.isBatchMode) EditorApplication.Exit(1);
        }

        private static bool BuildFamilies()
        {
            int built = 0;
            foreach (var f in Families)
            {
                if (Build(f)) built++;
            }

            Debug.Log($"[BL-93] ModelSetup: built {built}/{Families.Length} creature prefabs.");
            return built == Families.Length;
        }

        /// <summary>
        /// 🔴 `BL-102` — THE CHARACTER HALF. Reads every clip in <see cref="CharAnimDir"/>, retargets
        /// them onto each body in <see cref="Bodies"/>, and writes the prefab + controller.
        ///
        /// <para><b>What you have to supply, and nothing else:</b> animation files in
        /// <c>Assets/Resources/Models/Characters/Animations/</c>. The matcher is a substring match on
        /// the clip name, lower-cased, so the natural names all work:</para>
        /// <code>
        ///   idle.fbx      → Idle          (loops)   REQUIRED — nothing is built without it
        ///   walk.fbx      → Walking       (loops)   falls back to idle
        ///   run.fbx       → Running       (loops)   falls back to walk
        ///   attack.fbx    → Attack        (once)    optional
        ///   death.fbx     → Death         (once)    optional
        ///   cast.fbx      → Casting       (loops)   optional
        /// </code>
        ///
        /// <para>🔑 <b>A single-clip file is RENAMED to its own file name.</b> Mixamo calls every take
        /// <c>mixamo.com</c> — six downloads would be six clips with one name and the matcher could not
        /// tell them apart, so the file name wins. A multi-take FBX keeps its authored take names,
        /// because those are real.</para>
        ///
        /// <para>🔑 <b>Why a separate folder at all rather than clips inside the body:</b> retargeting.
        /// The bodies import as <b>Humanoid</b>, so one set of clips drives all 21 of them — and the
        /// elf and demon bodies added later — with no per-model work. Clips baked into one body would
        /// have to be re-authored for every new mesh, which is the rebuild the Humanoid rule exists to
        /// avoid. Clips that DO live inside a body FBX are still picked up and merged.</para>
        ///
        /// <para>⚠ <b>Missing folder is a SKIP, not a failure.</b> Until the art lands the hand-made
        /// <c>humanoid.prefab</c> is the best thing that exists and must not be overwritten with a
        /// clipless regeneration; and a headless build must not go red over an art file nobody has
        /// downloaded yet.</para>
        /// </summary>
        [MenuItem("Tools/BL-93/Build character prefab (BL-102)")]
        public static void BuildCharacters()
        {
            bool ok = BuildCharacterBodies();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!ok && Application.isBatchMode) EditorApplication.Exit(1);
        }

        private static bool BuildCharacterBodies()
        {
            var sources = AnimationSources();
            if (sources.Count == 0)
            {
                Debug.LogWarning(
                    $"[BL-102] No animation files in {CharAnimDir} — character prefabs SKIPPED (the " +
                    "existing ones are left exactly as they are). Drop idle/walk/run/attack FBXs in " +
                    "there and re-run; see docs/guides/UnityClient.md.");
                return true;
            }

            var shared = new List<AnimationClip>();
            foreach (var path in sources)
            {
                if (!PrepareAnimationSource(path)) return false;
                shared.AddRange(ClipsAt(path));
            }

            if (shared.Count == 0)
            {
                Debug.LogError($"[BL-102] {sources.Count} file(s) in {CharAnimDir} but not one " +
                               "animation clip in them. An FBX exported WITHOUT its take is the usual " +
                               "cause — on Mixamo, download 'FBX' (not 'FBX for Unity' collada) and " +
                               "leave 'Skin' as 'Without Skin'.");
                return false;
            }

            int built = 0;
            foreach (var b in Bodies)
            {
                if (BuildBody(b, shared)) built++;
            }

            Debug.Log($"[BL-102] ModelSetup: {shared.Count} shared clips " +
                      $"[{string.Join(", ", shared.Select(c => c.name))}] -> {built}/{Bodies.Length} " +
                      "character prefabs.");
            return built == Bodies.Length;
        }

        /// <summary>Every importable model under the animation folder, or an empty list if the folder
        /// is not there. `FindAssets` throws on a missing search folder, so the existence check is not
        /// optional.</summary>
        private static List<string> AnimationSources()
        {
            if (!Directory.Exists(CharAnimDir)) return new List<string>();

            return AssetDatabase.FindAssets("t:Model", new[] { CharAnimDir })
                                .Select(AssetDatabase.GUIDToAssetPath)
                                .Where(p => !string.IsNullOrEmpty(p))
                                .Distinct()
                                .OrderBy(p => p)
                                .ToList();
        }

        /// <summary>The real clips at a path — Unity's hidden <c>__preview</c> duplicates are not
        /// animation, they are the inspector's scrubber.</summary>
        private static List<AnimationClip> ClipsAt(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                         .Where(c => !c.name.StartsWith("__preview"))
                         .ToList();

        private static bool BuildBody(Body b, List<AnimationClip> shared)
        {
            if (!PrepareCharacterBody(b.Fbx)) return false;

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(b.Fbx);
            if (fbx == null) { Debug.LogError($"[BL-102] Missing body FBX: {b.Fbx}"); return false; }

            var avatar = AssetDatabase.LoadAllAssetsAtPath(b.Fbx).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isHuman)
            {
                // Without a HUMAN avatar on the body there is nothing to retarget ONTO: the clips play
                // against a skeleton they were never authored for and the body folds up. The inspector
                // gives no hint — the clips are right there and the rig tab says Humanoid.
                Debug.LogError($"[BL-102] {b.Key}: {b.Fbx} has no valid Humanoid avatar. Open its Rig " +
                               "tab, set Animation Type = Humanoid, Avatar Definition = Create From " +
                               "This Model, and Apply.");
                return false;
            }

            var clips = new List<AnimationClip>(shared);
            clips.AddRange(ClipsAt(b.Fbx));   // a body that DOES ship its own takes still contributes

            var controller = BuildController(b.Key, clips);
            if (controller == null) return false;

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            if (inst == null) { Debug.LogError($"[BL-102] {b.Key}: could not instantiate."); return false; }

            // See the note in Build(): NOT `?? AddComponent`.
            var animator = inst.GetComponent<Animator>();
            if (animator == null) animator = inst.AddComponent<Animator>();

            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;   // the SERVER moves things; root motion would fight it

            if (b.Height > 0.001f)
            {
                var h = MeshHeight(inst);
                if (h > 0.001f) inst.transform.localScale = Vector3.one * (b.Height / h);
            }

            PrefabUtility.SaveAsPrefabAsset(inst, $"{ModelsDir}/{b.Key}.prefab", out bool ok);
            Object.DestroyImmediate(inst);

            if (!ok) { Debug.LogError($"[BL-102] {b.Key}: SaveAsPrefabAsset failed."); return false; }
            Debug.Log($"[BL-102] {b.Key}: {clips.Count} clips -> {ModelsDir}/{b.Key}.prefab");
            return true;
        }

        /// <summary>The body imports Humanoid with its own avatar — the setting the whole swappability
        /// argument rests on. Forced here so a newly dropped body needs no inspector visit.</summary>
        private static bool PrepareCharacterBody(string fbxPath)
        {
            if (AssetImporter.GetAtPath(fbxPath) is not ModelImporter imp)
            {
                Debug.LogError($"[BL-102] Not an importable model: {fbxPath}");
                return false;
            }

            bool changed = false;
            if (imp.animationType != ModelImporterAnimationType.Human)
            {
                imp.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (imp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (changed) imp.SaveAndReimport();
            return true;
        }

        /// <summary>
        /// Get a clip-source FBX importing as retargetable humanoid motion.
        ///
        /// <para>🔑 <b>Human + CreateFromThisModel, not CopyFromOther.</b> The source file carries its
        /// own skeleton (Mixamo's, the pack's), so Unity builds an avatar from it and retargets at
        /// runtime through the two avatars. <c>CopyFromOther</c> demands a bone-for-bone identical
        /// hierarchy and fails the moment the clips come from a different pack than the body — which is
        /// the entire situation here.</para>
        ///
        /// <para>🔑 <b>Root position is LOCKED for looping clips</b> (<c>lockRootPositionXZ</c>). A walk
        /// exported with travel would slide the body away from the position the server put it at; with
        /// root motion off the transform never moves, so the mesh would simply drift out of its own
        /// capsule. Baking XZ into the pose makes any clip behave like an "in place" one.</para>
        /// </summary>
        private static bool PrepareAnimationSource(string fbxPath)
        {
            if (AssetImporter.GetAtPath(fbxPath) is not ModelImporter imp)
            {
                Debug.LogError($"[BL-102] Not an importable model: {fbxPath}");
                return false;
            }

            bool changed = false;

            if (imp.animationType != ModelImporterAnimationType.Human)
            {
                imp.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (imp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (!imp.importAnimation) { imp.importAnimation = true; changed = true; }

            var defs = imp.defaultClipAnimations;
            if (defs != null && defs.Length > 0)
            {
                // ONE take in the file → the FILE NAME is the clip name. Every Mixamo download is a
                // take called "mixamo.com"; six of them would be six clips the matcher cannot tell
                // apart, and the last one loaded would silently win every lookup.
                string fileName = Path.GetFileNameWithoutExtension(fbxPath);
                bool dirty = false;

                foreach (var c in defs)
                {
                    if (defs.Length == 1 && c.name != fileName) { c.name = fileName; dirty = true; }

                    bool loop = Looping.Any(l => c.name.ToLowerInvariant().Contains(l));
                    if (c.loopTime != loop) { c.loopTime = loop; dirty = true; }
                    if (c.loopPose != loop) { c.loopPose = loop; dirty = true; }
                    if (c.lockRootPositionXZ != loop) { c.lockRootPositionXZ = loop; dirty = true; }
                }

                if (dirty) { imp.clipAnimations = defs; changed = true; }
            }

            if (changed) imp.SaveAndReimport();

            var avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isHuman)
            {
                Debug.LogError($"[BL-102] {fbxPath}: Unity could not build a Humanoid avatar from this " +
                               "file, so its clips cannot retarget onto a character. Its skeleton is " +
                               "probably not a biped (or the export dropped the bones). Replace the file.");
                return false;
            }

            return true;
        }

        private static bool Build(Family f)
        {
            // Importer FIRST, asset SECOND: the reimport regenerates every sub-asset, so a GameObject
            // or clip loaded before it is a stale handle.
            if (!PrepareImporter(f.Fbx)) return false;

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(f.Fbx);
            if (fbx == null) { Debug.LogError($"[BL-93] Missing FBX: {f.Fbx}"); return false; }

            var clips = AssetDatabase.LoadAllAssetsAtPath(f.Fbx).OfType<AnimationClip>()
                                     .Where(c => !c.name.StartsWith("__preview"))
                                     .ToList();
            if (clips.Count == 0) { Debug.LogError($"[BL-93] {f.Key}: FBX has no animation clips."); return false; }

            var controller = BuildController(f.Key, clips);
            if (controller == null) return false;

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            if (inst == null) { Debug.LogError($"[BL-93] {f.Key}: could not instantiate."); return false; }

            // ⚠ NOT `GetComponent<Animator>() ?? AddComponent<Animator>()`. `??` is a plain C# null
            // check and UnityEngine.Object overloads `==` to report destroyed/absent objects as null
            // WITHOUT being null to the runtime — so the coalesce keeps the empty handle and the very
            // next line throws MissingComponentException. This exact line did.
            var animator = inst.GetComponent<Animator>();
            if (animator == null) animator = inst.AddComponent<Animator>();

            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(f.Fbx).OfType<Avatar>().FirstOrDefault();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;   // the SERVER moves things; root motion would fight it

            // Normalise the imported height — see the note on Family.
            var bounds = MeshHeight(inst);
            if (bounds > 0.001f) inst.transform.localScale = Vector3.one * (f.Height / bounds);

            PrefabUtility.SaveAsPrefabAsset(inst, $"{ModelsDir}/{f.Key}.prefab", out bool ok);
            Object.DestroyImmediate(inst);

            if (!ok) { Debug.LogError($"[BL-93] {f.Key}: SaveAsPrefabAsset failed."); return false; }
            Debug.Log($"[BL-93] {f.Key}: {clips.Count} clips -> {ModelsDir}/{f.Key}.prefab");
            return true;
        }

        /// <summary>
        /// Get the FBX importing the way an animated creature needs, then reimport.
        ///
        /// <para>🔴 <b>These packs land with <c>avatarSetup: NoAvatar</c>.</b> Without an Avatar a
        /// Generic rig has nothing to play clips ON, so Unity does not put an <c>Animator</c> on the
        /// imported model at all — the FBX arrives full of perfectly good animation that literally
        /// cannot run. Nothing in the file says so; the clips are right there in the inspector. This
        /// is the whole reason the first headless run failed, and it will be true of every monster FBX
        /// dropped in later, so it is fixed HERE rather than per-file by hand.</para>
        ///
        /// <para>⚠ <b>Generic, never Humanoid, for creatures</b> — a rat has no shoulders to map. The
        /// Humanoid rule (`BL-93`) is what keeps CHARACTER art swappable; forcing it on a quadruped
        /// produces a mangled avatar instead of an error.</para>
        ///
        /// <para>Loop flags are set on the IMPORTER, not on the clips: a clip inside an FBX is
        /// generated on import, so anything set on the instance is discarded the next time the file is
        /// touched. An unlooped idle plays once and freezes the creature in its last frame — which
        /// reads as a broken model rather than a missing checkbox.</para>
        /// </summary>
        private static bool PrepareImporter(string fbxPath)
        {
            if (AssetImporter.GetAtPath(fbxPath) is not ModelImporter imp)
            {
                Debug.LogError($"[BL-93] Not an importable model: {fbxPath}");
                return false;
            }

            bool changed = false;

            if (imp.animationType != ModelImporterAnimationType.Generic)
            {
                imp.animationType = ModelImporterAnimationType.Generic;
                changed = true;
            }

            if (imp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (!imp.importAnimation) { imp.importAnimation = true; changed = true; }

            var defs = imp.defaultClipAnimations;
            if (defs != null && defs.Length > 0)
            {
                bool loops = false;
                foreach (var c in defs)
                {
                    bool loop = Looping.Any(l => c.name.ToLowerInvariant().Contains(l));
                    if (c.loopTime == loop) continue;
                    c.loopTime = loop;
                    loops = true;
                }

                if (loops) { imp.clipAnimations = defs; changed = true; }
            }

            if (changed) imp.SaveAndReimport();
            return true;
        }

        /// <summary>
        /// The state machine, wired to the parameter names <c>EntityView</c> already drives.
        ///
        /// <para>🔑 <b>Only parameters with a clip behind them are declared.</b> <c>AttachModel</c>
        /// scans the controller's parameters once and silently skips whatever is absent, so a family
        /// with no cast animation simply never poses for one — no console spam, no missing-parameter
        /// warnings, and the same controller shape works for every pack regardless of what it ships.
        /// These monster packs have Idle/Walk/Attack/Death and no cast, so "Casting" is not declared
        /// and the client will not ask for it — and a character folder holding nothing but an idle
        /// still produces a valid prefab that simply stands there.</para>
        ///
        /// <para>🔑 <b>The same builder serves creatures and characters.</b> The controller shape does
        /// not care where the clips came from, only what they are called, so `BL-102` cost no second
        /// state machine.</para>
        /// </summary>
        private static AnimatorController BuildController(string key, List<AnimationClip> clips)
        {
            string path = $"{ModelsDir}/{key}.controller";
            AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            if (ac == null) { Debug.LogError($"[BL-93] {key}: could not create controller."); return null; }

            AnimationClip Find(params string[] wanted) => FindClip(clips, wanted);

            var idle    = Find("idle", "flying");
            var walk    = Find("walk", "run", "running", "flying", "jump") ?? idle;
            var run     = Find("run", "running", "walk", "flying") ?? walk;
            var attack  = Find("attack", "attack2", "punch", "slash");
            var death   = Find("death", "dying", "die");
            var casting = Find("cast", "casting", "spell", "summon");

            if (idle == null)
            {
                Debug.LogError($"[BL-93] {key}: no idle clip among [{string.Join(", ", clips.Select(c => c.name))}]");
                return null;
            }

            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var sm = ac.layers[0].stateMachine;

            // Locomotion is a 1D blend tree rather than three states with thresholds: the crossfade is
            // continuous, so a creature accelerating out of an idle does not pop, and there are no
            // transition times to tune per family.
            //
            // ⚠ The thresholds are in UNITY UNITS PER SECOND, which is what EntityView measures off the
            // drawn position (WorldMapper.Scale = 0.01, so a mob running at 132 server units/s arrives
            // here as 1.32). Getting this wrong is the difference between a creature that runs and one
            // that moonwalks at full speed.
            var locomotion = ac.CreateBlendTreeInController("Locomotion", out var tree);
            tree.blendParameter = "Speed";
            tree.blendType = BlendTreeType.Simple1D;
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 0.7f);
            tree.AddChild(run, 1.4f);
            sm.defaultState = locomotion;

            if (attack != null)
            {
                ac.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
                var st = sm.AddState("Attack");
                st.motion = attack;

                var into = locomotion.AddTransition(st);
                into.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
                into.hasExitTime = false;
                into.duration = 0.05f;

                var back = st.AddTransition(locomotion);
                back.hasExitTime = true;      // the swing plays out; the server already resolved the blow
                back.exitTime = 0.9f;
                back.duration = 0.1f;
            }

            if (casting != null)
            {
                // A BOOL, not a trigger: casting has a DURATION the server owns (EntityView.SetCasting
                // is driven by the cast bar and MobCastInfo), and it ends on interrupt as well as on
                // landing. A trigger would fire once and the pose would end whenever the clip did,
                // which is the one thing it must not do.
                ac.AddParameter("Casting", AnimatorControllerParameterType.Bool);
                var st = sm.AddState("Casting");
                st.motion = casting;

                var into = locomotion.AddTransition(st);
                into.AddCondition(AnimatorConditionMode.If, 0f, "Casting");
                into.hasExitTime = false;
                into.duration = 0.1f;

                var back = st.AddTransition(locomotion);
                back.AddCondition(AnimatorConditionMode.IfNot, 0f, "Casting");
                back.hasExitTime = false;
                back.duration = 0.1f;
            }

            if (death != null)
            {
                ac.AddParameter("Dead", AnimatorControllerParameterType.Bool);
                var st = sm.AddState("Death");
                st.motion = death;

                // FROM ANY STATE: a creature can die mid-swing, and a death that only reaches from
                // locomotion leaves the corpse finishing its attack.
                var die = sm.AddAnyStateTransition(st);
                die.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
                die.hasExitTime = false;
                die.duration = 0.1f;
                die.canTransitionToSelf = false;

                // And back out again — Dead is a BOOL, not a one-way trip: the same entity object is
                // reused when a mob respawns, and SetBool(false) has to actually undo the pose.
                var up = st.AddTransition(locomotion);
                up.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
                up.hasExitTime = false;
                up.duration = 0.1f;
            }

            EditorUtility.SetDirty(ac);
            return ac;
        }

        /// <summary>
        /// First clip matching any of <paramref name="wanted"/>, in the order asked for.
        ///
        /// <para>Three passes per word, most precise first — exact name, then the monster packs'
        /// <c>Rat_idle</c> suffix, then a plain substring. The last pass is what makes a raw download
        /// work: <c>Walking</c>, <c>Standing Melee Attack Downward</c> and <c>Run_Fwd</c> all land
        /// without renaming a single file. ⚠ It is a substring match, so a clip whose name happens to
        /// contain a keyword ("drunk" contains "run") can be picked — precise names win first, which is
        /// why the exact pass exists at all.</para>
        /// </summary>
        private static AnimationClip FindClip(List<AnimationClip> clips, params string[] wanted)
        {
            foreach (var w in wanted)
            {
                var hit = clips.FirstOrDefault(c => c.name.ToLowerInvariant() == w)
                       ?? clips.FirstOrDefault(c => c.name.ToLowerInvariant().EndsWith("_" + w))
                       ?? clips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains(w));
                if (hit != null) return hit;
            }

            return null;
        }

        /// <summary>World-space height of the renderers under this object, used to normalise scale.</summary>
        private static float MeshHeight(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0f;

            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b.size.y;
        }
    }
}
