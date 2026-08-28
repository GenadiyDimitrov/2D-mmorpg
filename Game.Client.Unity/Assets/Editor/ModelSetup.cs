using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.ClientEditor
{
    /// <summary>
    /// `BL-93` — BUILDS THE CREATURE PREFABS AND THEIR ANIMATOR CONTROLLERS, HEADLESSLY.
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
        /// broken model, not a missing setting.</summary>
        private static readonly string[] Looping = { "idle", "walk", "run", "flying" };

        [MenuItem("Tools/BL-93/Build creature prefabs")]
        public static void BuildAll()
        {
            int built = 0;
            foreach (var f in Families)
            {
                if (Build(f)) built++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BL-93] ModelSetup: built {built}/{Families.Length} creature prefabs.");

            // A non-zero exit code is the only thing a headless caller can see. Without this a failed
            // import is a green build that ships an APK with no models in it.
            if (built != Families.Length && Application.isBatchMode) EditorApplication.Exit(1);
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
        /// and the client will not ask for it.</para>
        /// </summary>
        private static AnimatorController BuildController(string key, List<AnimationClip> clips)
        {
            string path = $"{ModelsDir}/{key}.controller";
            AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            if (ac == null) { Debug.LogError($"[BL-93] {key}: could not create controller."); return null; }

            AnimationClip Find(params string[] wanted) =>
                wanted.Select(w => clips.FirstOrDefault(
                                  c => c.name.ToLowerInvariant().EndsWith("_" + w)))
                      .FirstOrDefault(c => c != null);

            var idle   = Find("idle", "flying");
            var walk   = Find("walk", "run", "running", "flying", "jump") ?? idle;
            var run    = Find("run", "running", "walk", "flying") ?? walk;
            var attack = Find("attack", "attack2");
            var death  = Find("death");

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
