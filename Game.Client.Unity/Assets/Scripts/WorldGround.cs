using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Sizes and colours the ground the world stands on.
    ///
    /// Two problems it fixes, both found on the device rather than at a desk:
    ///
    /// 1. **The plane did not cover the screen.** The scene ships a default 10×10 Unity plane; the
    ///    world is 240 units across. At any camera height above a shallow zoom the plane simply ran
    ///    out, leaving the camera's clear colour along the top and right of the frame. This scales it
    ///    to the whole world footprint, so there is no "edge of the map" to find.
    ///
    /// 2. **Grey mobs on a grey plane.** The entity markers are flat unlit spheres and the ground was
    ///    a mid grey of almost the same value, so a mob and the floor hid each other. The ground is
    ///    now DARK and slightly desaturated-green: everything drawn on it — red mobs, the green self
    ///    marker, yellow NPCs, the zone discs, the grid — reads against it by value, not just hue,
    ///    which is what keeps it legible for a colour-blind eye too.
    ///
    /// The plane is cosmetic only. Taps are resolved against a mathematical y=0 plane in
    /// <see cref="TouchInput"/>, never against this collider, so resizing it cannot affect movement.
    /// </summary>
    public class WorldGround : MonoBehaviour
    {
        [Tooltip("Server world size (square), matching GroundGrid.")]
        public float ServerWorldSize = 24000f;

        /// <summary>Dark enough that every marker drawn on it wins on brightness alone.</summary>
        public Color GroundColour = new Color(0.13f, 0.16f, 0.14f);

        private void Start()
        {
            float size = ServerWorldSize * WorldMapper.Scale;

            // Unity's built-in Plane mesh is 10×10 at scale 1, hence the tenth. The world spans
            // x 0..size and z 0..-size (server Y grows downward — see WorldMapper), so the centre sits
            // at +half / -half rather than at the origin.
            transform.localScale = new Vector3(size / 10f, 1f, size / 10f);
            transform.position = new Vector3(size * 0.5f, 0f, -size * 0.5f);

            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = UnlitMaterials.Create(GroundColour);
                if (material != null) renderer.material = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }
    }
}
