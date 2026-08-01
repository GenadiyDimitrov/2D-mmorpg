using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// The SELECTION RING — the circle on the ground under whatever you currently have targeted.
    ///
    /// Until this existed, the only place a target was visible was the target WINDOW: on screen, the
    /// mob you were about to attack looked exactly like the four next to it. That is fine on a desktop
    /// where you clicked it a moment ago and the cursor is still there, and useless on a phone, where
    /// you tap, the finger lifts, and nothing on the battlefield says which one answered.
    ///
    /// It is a REAL RING (a procedural annulus), not a filled disc, for two reasons: a disc would cover
    /// the thing it is pointing at, and the click-to-move marker is already a filled disc — two solid
    /// circles a moment apart would read as the same object. The colour says what kind of thing is
    /// selected, and it breathes gently so a stationary target still catches the eye.
    /// </summary>
    public class TargetMarker : MonoBehaviour
    {
        /// <summary>Outer/inner radius in Unity units. The entity marker is a 0.9-scale sphere (radius
        /// 0.45), so the ring sits just outside it rather than around its middle.</summary>
        public float OuterRadius = 0.78f, InnerRadius = 0.62f;

        /// <summary>How far the ring breathes, and how fast. Small on purpose: this is a "you are here"
        /// pulse, not an animation anyone should be watching.</summary>
        public float PulseAmount = 0.08f, PulseSpeed = 2.2f;

        /// <summary>Above the ground grid, below everything else — the same trick MoveMarker uses to
        /// avoid z-fighting with the floor.</summary>
        private const float GroundY = 0.04f;

        private const int Segments = 36;

        private Transform _ring;
        private Material _material;
        private Transform _follow;
        private Color _colour = Color.white;

        private void Awake()
        {
            var go = new GameObject("TargetRing", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);

            go.GetComponent<MeshFilter>().sharedMesh = BuildRing();

            var renderer = go.GetComponent<MeshRenderer>();
            _material = UnlitMaterials.Create(_colour);
            if (_material != null) renderer.material = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // No collider at all — the ring lies exactly where you tap to re-target or to walk, and a
            // marker that eats those taps would make the selected entity the hardest one to touch.
            _ring = go.transform;
            Hide();
        }

        /// <summary>Put the ring under <paramref name="follow"/> and colour it. Called every frame while
        /// something is targeted, so changing target is just a different transform.</summary>
        public void Show(Transform follow, Color colour)
        {
            _follow = follow;
            if (_ring == null) return;
            if (_colour != colour)
            {
                _colour = colour;
                UnlitMaterials.SetColor(_material, colour);
            }
            if (!_ring.gameObject.activeSelf) _ring.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _follow = null;
            if (_ring != null && _ring.gameObject.activeSelf) _ring.gameObject.SetActive(false);
        }

        /// <summary>LateUpdate, so the ring is placed AFTER EntityView has interpolated the entity for
        /// this frame. In Update it would draw one frame behind and visibly trail a running mob.</summary>
        private void LateUpdate()
        {
            if (_follow == null || _ring == null || !_ring.gameObject.activeSelf) return;

            var at = _follow.position;
            at.y = GroundY;                       // the entity floats; its ring lies on the floor
            _ring.position = at;

            float pulse = 1f + PulseAmount * Mathf.Sin(Time.unscaledTime * PulseSpeed);
            _ring.localScale = new Vector3(pulse, 1f, pulse);
        }

        /// <summary>
        /// A flat annulus in the XZ plane, built once: two rings of vertices joined by a quad per
        /// segment. Procedural because Unity ships no ring primitive and the alternatives are worse —
        /// a transparent texture needs an alpha-blended material (this project's unlit shader chain is
        /// opaque, see <see cref="UnlitMaterials"/>), and a circle of small cubes is dozens of objects
        /// to draw one shape.
        /// </summary>
        private Mesh BuildRing()
        {
            var vertices = new Vector3[Segments * 2];
            var triangles = new int[Segments * 6];

            for (int i = 0; i < Segments; i++)
            {
                float angle = (i / (float)Segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                vertices[i * 2] = new Vector3(cos * InnerRadius, 0f, sin * InnerRadius);
                vertices[i * 2 + 1] = new Vector3(cos * OuterRadius, 0f, sin * OuterRadius);

                int next = (i + 1) % Segments;
                int t = i * 6;
                // Wound so the face points UP — the camera looks down at it, and a back-facing ring
                // would simply be invisible.
                triangles[t] = i * 2;
                triangles[t + 1] = next * 2 + 1;
                triangles[t + 2] = i * 2 + 1;
                triangles[t + 3] = i * 2;
                triangles[t + 4] = next * 2;
                triangles[t + 5] = next * 2 + 1;
            }

            var mesh = new Mesh { name = "TargetRing" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
