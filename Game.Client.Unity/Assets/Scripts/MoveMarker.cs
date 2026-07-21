using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// The click-to-move destination ring — the flat disc that drops where you tapped so you can see
    /// where the character is heading, as in any click-to-move game.
    ///
    /// It is a squashed CYLINDER, which gives a solid circle lying on the ground with no texture and
    /// no alpha: transparency in URP would mean a second material, a render queue and sorting against
    /// the entity spheres, for a shape a primitive already draws. It pulses inward on appearing (so a
    /// second tap on the same spot still reads as a new order) and clears itself when you arrive.
    /// </summary>
    public class MoveMarker : MonoBehaviour
    {
        public Color Colour = new Color(0.35f, 0.65f, 1f);   // click-to-move blue
        // A quarter of the old size: at 1.6 the ring was as big as the character it was leading, which
        // reads as an object in the world rather than as a destination marker.
        public float Size = 0.4f;
        public float PulseFrom = 2.2f;      // starts wide and snaps in
        public float PulseSeconds = 0.25f;
        public float ArriveDistance = 1.2f; // Unity units — ~120 server units
        public float MaxSeconds = 30f;      // never leave a stale marker if the walk is interrupted

        private Transform _disc;
        private Vector3 _target;
        private float _shownAt = -1f;

        /// <summary>The entity to measure arrival against — the player. Set by GameBoot.</summary>
        public Transform Follow;

        private void Awake()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "MoveMarkerDisc";
            go.transform.SetParent(transform, false);

            // Cylinders come with a capsule collider — the marker must never eat a tap meant for the
            // ground underneath it, or tapping the same spot twice would do nothing.
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            var material = UnlitMaterials.Create(Colour);
            if (material != null) renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            _disc = go.transform;
            Hide();
        }

        /// <summary>Drop the marker at a ground position (Unity space).</summary>
        public void ShowAt(Vector3 groundPos)
        {
            _target = new Vector3(groundPos.x, 0.03f, groundPos.z);   // just above the grid
            _shownAt = Time.time;
            transform.position = _target;
            _disc.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _shownAt = -1f;
            if (_disc != null) _disc.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_shownAt < 0f) return;

            float age = Time.time - _shownAt;

            float t = PulseSeconds <= 0f ? 1f : Mathf.Clamp01(age / PulseSeconds);
            float width = Mathf.Lerp(Size * PulseFrom, Size, t * t);
            // Cylinder is 2 units tall by default, so Y 0.01 keeps it a disc rather than a drum.
            _disc.localScale = new Vector3(width, 0.01f, width);

            if (age > MaxSeconds) { Hide(); return; }

            if (Follow != null)
            {
                var here = Follow.position;
                here.y = 0f;
                var there = _target;
                there.y = 0f;
                if ((here - there).sqrMagnitude <= ArriveDistance * ArriveDistance) Hide();
            }
        }
    }
}
