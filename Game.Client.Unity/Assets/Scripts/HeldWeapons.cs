using System.Collections.Generic;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// `BL-103` / `BL-344` step 7 — what a character holds. The server sends the weapon TYPE (+ magic, + shield) on
    /// the spawn DTO; this turns it into meshes on the humanoid's hand bones.
    ///
    /// <para>🔑 <b>One prefab per type</b>, <c>Resources/Weapons/&lt;key&gt;</c>, built by <c>ModelSetup.BuildWeapons</c>.
    /// Every prefab shares ONE frame, so the code below never needs to know which weapon it is hanging: the root is
    /// the GRIP, +Y runs along the weapon (blade, head or bow limb up), +Z is the edge / striking face / the side the
    /// arrow flies to, and a shield's face looks down −X. Lengths are in the humanoid body's own units.</para>
    ///
    /// <para>🔑 <b>The hand frame is read off the hand's own children</b> (wrist → middle-finger root, wrist → thumb
    /// root). Both are children of the hand bone, so the frame is fixed in the hand's local space whatever the
    /// animation does, and no per-rig rotation has to be authored. The avatar's finger mapping is tried first; the
    /// children's names are the fallback, because an auto-mapped avatar often leaves the fingers out.</para>
    ///
    /// <para>A model without a Humanoid avatar (rat, spider, dragon, skeleton) has no hand to hold anything, so a
    /// mob whose weapon is "claws" draws nothing — which is right.</para>
    /// </summary>
    public static class HeldWeapons
    {
        private const string Dir = "Weapons/";

        /// <summary>Animator <c>AttackStyle</c> values: which swing the "Attack" trigger plays.</summary>
        public const int StyleMelee = 0, StyleBow = 1, StyleSpell = 2;

        /// <summary>The prefab key for a weapon type, or null for bare hands. The eight keys of `BL-103`'s design
        /// (`duals` draws the one dagger mesh twice).</summary>
        public static string KeyFor(WeaponType w, bool magic)
        {
            switch (w)
            {
                case WeaponType.Sword:          return "sword1h";
                case WeaponType.TwoHandedSword: return "sword2h";
                case WeaponType.Blunt:          return magic ? "wand" : "blunt1h";
                case WeaponType.TwoHandedBlunt: return magic ? "staff" : "blunt2h";
                case WeaponType.Dual:           return "dagger";
                case WeaponType.Bow:            return "bow";
                default:                        return null;
            }
        }

        public static int StyleFor(WeaponType w, bool magic) =>
            w == WeaponType.Bow ? StyleBow : magic ? StyleSpell : StyleMelee;

        /// <summary>`BL-371` — which dressing a <c>held</c> list is on. A weapon prefab can arrive frames after it was asked
        /// for (GameAssets), and by then the body may have been re-dressed (a weapon swap): a late weapon from the OLD
        /// dressing must not be hung next to the new one. Keyed weakly by the list, so a destroyed body leaves nothing.</summary>
        private sealed class Dressing { public int Number; }
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<List<GameObject>, Dressing> Dressings =
            new System.Runtime.CompilerServices.ConditionalWeakTable<List<GameObject>, Dressing>();

        /// <summary>Hang this weapon set on <paramref name="animator"/>'s hands, replacing whatever
        /// <paramref name="held"/> holds. Returns false when the body has no hands (nothing was drawn). The meshes
        /// may arrive LATER (`BL-371`); <paramref name="hung"/> runs after each one is hung, so the caller can fade or
        /// hide it like the rest of the body.</summary>
        public static bool Dress(Animator animator, Transform modelRoot, WeaponType w, bool magic, bool shield,
                                 List<GameObject> held, System.Action hung = null)
        {
            foreach (var go in held) if (go != null) Object.Destroy(go);
            held.Clear();
            var dressing = Dressings.GetOrCreateValue(held);
            int number = ++dressing.Number;
            if (animator == null || !animator.isHuman) return false;

            string key = KeyFor(w, magic);
            if (key != null)
            {
                // Everything rides the RIGHT hand, the bow included: the only shot take we have (UAL's Pistol_Shoot)
                // raises the right arm, and a bow in the left would hang at the hip through the shot. A real archer
                // holds it left-handed; a draw clip from a later library is when it moves.
                Hang(animator, modelRoot, key, false, held, dressing, number, hung);
                if (w == WeaponType.Dual) Hang(animator, modelRoot, key, true, held, dressing, number, hung);
            }
            if (shield) Hang(animator, modelRoot, "shield", true, held, dressing, number, hung);
            return true;
        }

        private static void Hang(Animator animator, Transform modelRoot, string key, bool left, List<GameObject> held,
                                 Dressing dressing, int number, System.Action hung)
        {
            GameAssets.Load<GameObject>(Dir + key, prefab =>
            {
                if (dressing.Number != number || animator == null || modelRoot == null) return;   // re-dressed or gone
                if (Hang(animator, modelRoot, prefab, left, held)) hung?.Invoke();
            });
        }

        private static bool Hang(Animator animator, Transform modelRoot, GameObject prefab, bool left, List<GameObject> held)
        {
            var socket = Socket(animator, left);
            if (prefab == null || socket == null) return false;

            var go = Object.Instantiate(prefab, socket, false);
            go.name = prefab.name;
            // The prefab is authored in the BODY's units. The hand bone carries the rig's import scale on top of the
            // model root's, so undo the difference: the weapon ends up scaled exactly like the body around it,
            // including the entity-size slider.
            float hand = Mathf.Max(1e-6f, socket.lossyScale.x);
            go.transform.localScale = Vector3.one * (modelRoot.lossyScale.x / hand);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            held.Add(go);
            return true;
        }

        /// <summary>The grip frame on one hand, built once per body and kept as a child of the hand bone.</summary>
        public static Transform Socket(Animator animator, bool left)
        {
            var hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            if (hand == null) return null;
            string name = left ? "grip_L" : "grip_R";
            var existing = hand.Find(name);
            if (existing != null) return existing;

            var middle = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal)
                         ?? animator.GetBoneTransform(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal)
                         ?? ChildNamed(hand, "middle") ?? ChildNamed(hand, "index");
            var thumb = animator.GetBoneTransform(left ? HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal)
                        ?? ChildNamed(hand, "thumb");

            // World-space directions NOW; the frame is stored in hand-local space, so the pose it was read in does not
            // matter. Fallbacks for a fingerless rig: along the forearm, and the body's forward for the thumb.
            Vector3 along = middle != null ? middle.position - hand.position
                          : hand.position - (hand.parent != null ? hand.parent.position : hand.position - animator.transform.right);
            float palm = along.magnitude;
            if (palm < 1e-5f) { along = left ? -animator.transform.right : animator.transform.right; palm = 0.08f; }
            along /= palm;
            Vector3 up = thumb != null ? thumb.position - hand.position : animator.transform.forward;
            up = Vector3.ProjectOnPlane(up, along);
            if (up.sqrMagnitude < 1e-8f) up = Vector3.ProjectOnPlane(animator.transform.forward, along);
            up.Normalize();

            // The back of the hand: +right for the right hand, −right for the left (see the class note on the frame).
            Vector3 back = Vector3.Cross(up, along);
            if (left) back = -back;

            var socket = new GameObject(name).transform;
            socket.SetParent(hand, false);
            // The fist's middle: half way down the palm and a little toward the palm side.
            socket.position = hand.position + along * (palm * 0.55f) - back * (palm * 0.25f);
            socket.rotation = Quaternion.LookRotation(along, up);
            return socket;
        }

        private static Transform ChildNamed(Transform hand, string part)
        {
            for (int i = 0; i < hand.childCount; i++)
            {
                var c = hand.GetChild(i);
                if (c.name.ToLowerInvariant().Contains(part)) return c;
            }
            return null;
        }
    }
}
