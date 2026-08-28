using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Follows the player from straight overhead. Pitch is 90° — a true top-down view, matching the
    /// flat WPF harness (78° was tried on the phone and read as "weird"; 55° was a 2.5D experiment).
    /// To go 2.5D LATER, lower <see cref="Pitch"/> toward ~45–55° — that is the ENTIRE change; the
    /// world, entities and logic are untouched.
    ///
    /// Distance is the player's to choose: **pinch to zoom**, and the choice is remembered across
    /// launches (PlayerPrefs). Guessing a distance for someone else's phone is how we ended up with
    /// two taste calls in a row.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform Target;

        [Range(20f, 90f)] public float Pitch = 90f;   // 90 = straight down; lower to ~55 for a 2.5D look
        public float Yaw = 0f;
        public float Distance = 38f;                  // starting height; pinch overrides and persists
        public float Follow = 12f;                    // position smoothing

        /// <summary>
        /// Orthographic projection: same tilt, but the ground plane maps AFFINELY to the screen, so
        /// up-screen and down-screen cover the same world distance.
        ///
        /// This is the fix for the asymmetry a tilted PERSPECTIVE camera has: its far half of the
        /// frustum covers more ground than its near half, so at 55° you see roughly 3k ahead and 2k
        /// behind — enemies approaching from "below" appear later, even though the server already sent
        /// them (ViewRange is a radius, so the data was always symmetric; only the drawing was not).
        ///
        /// Cost: no foreshortening, so distance stops shrinking things and the scene reads flatter.
        /// Nothing is WARPED — parallel stays parallel, a sphere is still a circle — and with unlit
        /// spheres and billboards there is almost no perspective information to lose anyway.
        /// </summary>
        public bool Orthographic;

        /// <summary>Half the visible world HEIGHT under orthographic projection — the ortho equivalent
        /// of Distance, and what pinch drives in that mode.</summary>
        public float OrthoSize = 22f;
        public float MinOrthoSize = 6f;
        public float MaxOrthoSize = 60f;

        public float MinDistance = 10f;
        public float MaxDistance = 90f;
        public float PinchSpeed = 0.08f;              // world units per pixel of pinch
        public float ScrollSpeed = 4f;                // editor mouse wheel

        private const string DistanceKey = "cam.distance";

        private float _lastPinch;
        private bool _pinching;
        private float _savedDistance;

        private void Awake()
        {
            if (PlayerPrefs.HasKey(DistanceKey))
                Distance = PlayerPrefs.GetFloat(DistanceKey, Distance);
            Distance = Mathf.Clamp(Distance, MinDistance, MaxDistance);
            _savedDistance = Distance;
        }

        private void Update()
        {
            // Two fingers = zoom. One finger is tap-to-move (TouchInput), which ignores multi-touch
            // frames, so the two never fight.
            if (Input.touchCount == 2)
            {
                float gap = (Input.GetTouch(0).position - Input.GetTouch(1).position).magnitude;
                if (!_pinching) { _pinching = true; _lastPinch = gap; }
                else
                {
                    // Fingers apart = zoom IN. Under ortho that means a SMALLER ortho size; under
                    // perspective, a smaller distance. Same gesture, different knob.
                    float delta = (gap - _lastPinch) * PinchSpeed;
                    if (Orthographic)
                        OrthoSize = Mathf.Clamp(OrthoSize - delta, MinOrthoSize, MaxOrthoSize);
                    else
                        Distance = Mathf.Clamp(Distance - delta, MinDistance, MaxDistance);
                    _lastPinch = gap;
                }
            }
            else
            {
                if (_pinching) Persist();
                _pinching = false;

                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    Distance = Mathf.Clamp(Distance - scroll * ScrollSpeed, MinDistance, MaxDistance);
                    Persist();
                }
            }
        }

        /// <summary>Written only when a gesture ENDS — PlayerPrefs.Save() every pinch frame would hit
        /// the disk ~60 times a second.</summary>
        private void Persist()
        {
            if (Mathf.Approximately(_savedDistance, Distance)) return;
            _savedDistance = Distance;
            PlayerPrefs.SetFloat(DistanceKey, Distance);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Push the projection onto the Camera. Distance still positions the rig under ortho — the
        /// camera has to stay far enough away that the world is inside the near/far planes, even
        /// though the ortho SIZE is what decides how much you see.
        /// </summary>
        private void ApplyProjection()
        {
            var cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            if (cam.orthographic != Orthographic) cam.orthographic = Orthographic;
            if (Orthographic) cam.orthographicSize = OrthoSize;
        }

        /// <summary>
        /// How far back the RIG actually sits, which under orthographic projection is NOT the same
        /// question as how much you can see.
        ///
        /// <para>🔴 <b>The bug this fixes: a growing unrendered band along the BOTTOM of the screen
        /// as you zoom out in ortho.</b> That band is the NEAR CLIP PLANE eating the ground. An ortho
        /// camera's clip planes are a SLAB, not a frustum: everything nearer than <c>nearClipPlane</c>
        /// (0.3, the scene default) is cut, no matter how wide the view is. Tilt the camera and the
        /// bottom of the screen is the NEAR end of the ground — at pitch θ the ground under the bottom
        /// edge sits at depth <c>Distance − OrthoSize·cot θ</c>. Zooming out raises OrthoSize while
        /// Distance stays put, so that depth marches toward the camera, crosses zero, and the ground
        /// starts being clipped from the bottom up — a band that GROWS with every further zoom.</para>
        ///
        /// <para>Why it appeared only now: at Pitch 90 (the shipped top-down default) cot θ = 0 and the
        /// term vanishes, so this could not happen. It is unreachable until you tilt, and the camera
        /// angle slider only opened to 45° for the 2.5D look the models are being judged in.</para>
        ///
        /// <para>The fix is free because under ortho <b>distance does not zoom</b> — moving the rig
        /// back changes nothing you can see. So back it off by exactly the term that was eating the
        /// margin, which pins the bottom-edge ground depth at <see cref="Distance"/> (≥10) at every
        /// angle and every zoom. Under perspective, distance IS the zoom and nothing is touched.</para>
        /// </summary>
        private float RigDistance =>
            Orthographic
                ? Distance + OrthoSize / Mathf.Max(0.01f, Mathf.Tan(Pitch * Mathf.Deg2Rad))
                : Distance;

        private Vector3 _followPoint;
        private Transform _followTarget;

        private void LateUpdate()
        {
            if (Target == null) return;

            ApplyProjection();

            var rot = Quaternion.Euler(Pitch, Yaw, 0f);

            // 🔴 THE SMOOTHING BELONGS TO THE FOLLOW, NOT TO THE ORBIT.
            //
            // This used to smooth the camera's WORLD POSITION toward `Target.position + orbit`. That
            // is fine while only the target moves, and wrong the moment YAW moves: rotation is applied
            // exactly (below) while position lags by the follow time constant, so the camera ends up
            // aiming with the new yaw from an old place. The target stops being the point the world
            // turns around and instead swings around a SMALLER, phase-lagged circle — which is exactly
            // what the rotation slider felt like ("it rotates like a smaller circle in the middle").
            //
            // The lag is not subtle: at Follow 12 the time constant is 83ms, so dragging the slider at
            // ~360°/s throws the character roughly Distance·cos(Pitch)·0.46 off centre — about half the
            // screen at the settings this was reported at.
            //
            // Smoothing the FOLLOW POINT instead keeps the character exactly at the centre of rotation
            // at every yaw, while the chase-the-player damping it was always there for is unchanged:
            // the point being smoothed is still the player's position, and the orbit is now rigid
            // geometry hung off it rather than something the filter can distort.
            if (_followTarget != Target)
            {
                _followTarget = Target;
                _followPoint = Target.position;    // snap on a new target, never sweep the world
            }

            // FRAME-RATE INDEPENDENT smoothing. This was `Lerp(pos, desired, deltaTime * Follow)` —
            // deltaTime used as a BLEND FACTOR, which is the exact bug that was found and fixed in
            // EntityView and then left standing here. At Follow 12 any frame longer than 83ms gives a
            // factor above 1: the camera flies PAST the character and comes back. And because the
            // camera is what the player actually watches, a wobble here is indistinguishable from the
            // character jittering — the entity can be perfectly smooth in world space and still look
            // wrong on screen.
            _followPoint = Vector3.Lerp(
                _followPoint, Target.position, 1f - Mathf.Exp(-Follow * Time.deltaTime));

            transform.position = _followPoint + rot * Vector3.back * RigDistance;

            // Assign the rotation directly rather than LookAt(Target): at Pitch 90 the view direction
            // is parallel to Vector3.up, which makes LookAt's up-vector degenerate and the image spin
            // or flip. This is exact by construction — the position above was derived from the same
            // rotation, so the follow point is dead centre of frame.
            transform.rotation = rot;
        }
    }
}
