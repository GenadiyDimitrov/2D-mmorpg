using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Follows the player at a fixed orbit. START value is a STEEP pitch (~78°, near top-down) so it
    /// reads like the flat 2D WPF view. To go 2.5D LATER, just lower <see cref="Pitch"/> toward
    /// ~45–55° (and maybe raise Distance) — that is the ENTIRE change; the world, entities and logic
    /// are untouched. Tune Pitch/Yaw/Distance live in the Inspector.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform Target;
        [Range(20f, 89f)] public float Pitch = 78f;   // 78 ≈ top-down (WPF-like); ~50 = 2.5D later
        public float Yaw = 0f;
        public float Distance = 28f;
        public float Follow = 12f;                     // position smoothing

        private void LateUpdate()
        {
            if (Target == null) return;
            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            var desired = Target.position + rot * Vector3.back * Distance;
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * Follow);
            transform.LookAt(Target.position);
        }
    }
}
