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

            PaintTheVoid();
        }

        /// <summary>
        /// 🔴 **THE GREY RECTANGLE AT FULL ORTHO ZOOM-OUT** (playtest 29: *"now in ortho if I zoom out
        /// to much it become the gray rectangle clip … now it just covers the whole screen if I don't
        /// zoomin to much"*). It is NOT a clip plane this time, and it is not a bug in the projection.
        /// It is the EDGE OF THE WORLD.
        ///
        /// <para>The arithmetic, which is the whole diagnosis. The world is 24 000 server units, and at
        /// <c>WorldMapper.Scale</c> 0.01 that is <b>240 Unity units</b> across. An ortho camera at
        /// <c>MaxOrthoSize</c> 60 shows <c>2 · 60 · aspect</c> units horizontally — on a 19.5:9 phone
        /// held landscape that is <b>260</b>. So at the far end of the zoom slider the view is WIDER
        /// THAN THE MAP, the ground plane genuinely runs out, and every pixel it does not cover is the
        /// camera's clear colour: Unity's default blue-grey, with the alpha at 0 and no skybox
        /// assigned. Vertically it is <c>2 · 60 / sin(Pitch)</c> = 170 of the 240, so standing anywhere
        /// near an edge puts most of the frame outside the world at that zoom.</para>
        ///
        /// <para>🔑 The previous grey band WAS the near clip plane (see CameraRig.RigDistance) and that
        /// fix still holds — at the slider's own limits (Pitch 45-90, OrthoSize 6-60, Distance 10-90)
        /// the near edge of the ground sits at Distance ≥ 10 against a 0.3 plane and the far edge at
        /// ≤ 210 against 1000. Neither plane can be reached any more. Two different bugs wearing the
        /// same grey; the second one only became visible once the first stopped hiding it.</para>
        ///
        /// <para>The fix is to stop rendering a HOLE. Clearing to the ground's own colour means
        /// "beyond the map" reads as more ground instead of a void, at every zoom and — this is the
        /// half that matters more day to day — while standing at the world edge at ANY zoom, which was
        /// already true before he ever touched the slider. The grid still stops at the real boundary,
        /// so the edge of the world is still legible; it just is not a grey rectangle any more.
        /// The zoom range is deliberately NOT reduced: he asked for that zoom.</para>
        /// </summary>
        private void PaintTheVoid()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Opaque: the scene's stored background carries alpha 0, which on some Android targets
            // composites as a transparent (black) clear instead of the colour.
            cam.backgroundColor = new Color(GroundColour.r, GroundColour.g, GroundColour.b, 1f);
        }
    }
}
