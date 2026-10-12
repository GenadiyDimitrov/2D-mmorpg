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

        /// <summary>The Settings sliders and the drag gesture write the SAME two keys, so whichever moved
        /// the camera last is what the next launch starts from.</summary>
        public const string PitchKey = "cam.pitch";
        public const string YawKey = "cam.yaw";

        // ── `BL-344` step 8: ONE FINGER DRAGGED ACROSS THE WORLD TURNS AND TILTS THE VIEW ──────────
        //
        // A one-finger drag did nothing before this (a tap is decided on release, and anything that
        // travelled past the slop was thrown away), so it was the free gesture. Two-finger TWIST was the
        // other candidate and loses: it is hard to tell from a pinch, and both hands are already busy.
        // Sideways turns (180° per screen width), up/down tilts within [MinPitch, 90] — the "limited
        // vertical" of the owner's Q4. Finger UP looks further ahead (lower pitch), as a mouse-look does.
        // A press that STARTS over UI never turns the camera: the skill bar and window drags own it.

        /// <summary>The tilt limit. Matches the Settings slider's floor: below ~45° the ortho rig's
        /// near-plane correction grows and the ground reaches the far plane at full zoom-out.</summary>
        public const float MinPitch = 45f, MaxPitch = 90f;
        public float YawPerScreen = 180f;      // degrees for a drag across the full screen width
        public float PitchPerScreen = 90f;     // degrees for a drag up the full screen height
        /// <summary>Same as <see cref="TouchInput.TapSlopPixels"/>: a finger that stays inside it is a tap.</summary>
        public const float DragSlopPixels = 40f;

        /// <summary>True from the moment the current touch became a camera gesture (a drag past the slop,
        /// or a second finger for a pinch) until every finger is up. <see cref="TouchInput"/> reads it so
        /// that releasing such a touch is never also a tap — a drag that wanders back to where it began,
        /// or the finger left over from a pinch, used to walk you to the spot under it.</summary>
        public bool TouchConsumed { get; private set; }

        /// <summary>The view is off the top-down, north-up home (the HUD compass shows only then).</summary>
        public bool Turned => Mathf.Abs(Mathf.DeltaAngle(Yaw, 0f)) > 0.5f || Pitch < MaxPitch - 0.5f;

        private float _lastPinch;
        private bool _pinching;
        private float _savedDistance;
        private bool _dragOk, _dragging;
        private Vector2 _dragLast;
        private float _resetFrom = -1f, _resetYaw, _resetPitch;
        private const float ResetSeconds = 0.35f;

        private void Awake()
        {
            if (PlayerPrefs.HasKey(DistanceKey))
                Distance = PlayerPrefs.GetFloat(DistanceKey, Distance);
            Distance = Mathf.Clamp(Distance, MinDistance, MaxDistance);
            _savedDistance = Distance;
        }

        /// <summary>The compass: swing back to north-up, straight down. Animated, so you see which way
        /// the world turns and keep your bearings.</summary>
        public void ResetView()
        {
            _resetFrom = Time.unscaledTime;
            _resetYaw = Yaw;
            _resetPitch = Pitch;
        }

        private void Update()
        {
            Zoom();
            Turn();

            if (_resetFrom >= 0f)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - _resetFrom) / ResetSeconds);
                t = t * t * (3f - 2f * t);
                Yaw = Mathf.LerpAngle(_resetYaw, 0f, t);
                Pitch = Mathf.Lerp(_resetPitch, MaxPitch, t);
                if (t >= 1f) { _resetFrom = -1f; Yaw = 0f; PersistView(); }
            }
        }

        private void Turn()
        {
            if (Input.touchCount == 0)
            {
                if (_dragging) PersistView();
                _dragging = false;
                TouchConsumed = false;
                MouseTurn();
                return;
            }

            var t = Input.GetTouch(0);
            if (Input.touchCount > 1)
            {
                // A pinch. It ends the drag, and nothing more may turn or tap until every finger is up.
                if (_dragging) PersistView();
                _dragging = false;
                _dragOk = false;
                TouchConsumed = true;
                return;
            }

            if (t.phase == TouchPhase.Began)
            {
                _dragOk = !TouchConsumed && !UiKit.OverUi(t.position);
                _dragging = false;
                _dragLast = t.position;
                return;
            }
            if (!_dragOk) return;

            if (!_dragging)
            {
                if ((t.position - t.rawPosition).magnitude < DragSlopPixels) return;
                _dragging = true;
                TouchConsumed = true;
                _resetFrom = -1f;
                _dragLast = t.rawPosition;   // the slop is not lost: the view catches up to the finger
            }
            Drag(t.position - _dragLast);
            _dragLast = t.position;
        }

        /// <summary>The Editor stand-in: hold the RIGHT mouse button and drag. The left one stays a click.</summary>
        private void MouseTurn()
        {
            if (Input.GetMouseButtonDown(1)) { _dragLast = Input.mousePosition; _resetFrom = -1f; }
            if (Input.GetMouseButton(1))
            {
                Vector2 now = Input.mousePosition;
                Drag(now - _dragLast);
                _dragLast = now;
            }
            if (Input.GetMouseButtonUp(1)) PersistView();
        }

        private void Drag(Vector2 delta)
        {
            Yaw = Mathf.Repeat(Yaw + delta.x / Mathf.Max(1, Screen.width) * YawPerScreen, 360f);
            Pitch = Mathf.Clamp(Pitch - delta.y / Mathf.Max(1, Screen.height) * PitchPerScreen, MinPitch, MaxPitch);
        }

        private void PersistView()
        {
            PlayerPrefs.SetFloat(PitchKey, Pitch);
            PlayerPrefs.SetFloat(YawKey, Yaw);
            PlayerPrefs.Save();
        }

        private void Zoom()
        {
            // Two fingers = zoom. One finger is a tap (TouchInput) or, dragged, a turn (Turn); a touch
            // that ever had a second finger is neither (TouchConsumed).
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
        ///
        /// <para>🔴 <b>AND THE SECOND BUG, WHICH THIS CORRECTION ITSELF CAUSED: the world vanished
        /// above ortho zoom ≈ 9.</b> (Owner, 2026-09-04: *"ortho zoom-out is still broken over 9
        /// zoom"* — the same grey, a third time, and this one is arithmetic, not geometry.)</para>
        ///
        /// <para>🔑 <c>Mathf.Tan(90f * Mathf.Deg2Rad)</c> IS NEGATIVE. Unity's <c>Deg2Rad</c> is
        /// <c>(float)PI * 2 / 360</c>, and float PI rounds UP, so <c>90 * Deg2Rad</c> lands a hair PAST
        /// π/2 and the tangent comes back around −2·10⁷ instead of +∞. <c>Mathf.Max(0.01f, tan)</c> then
        /// chose <b>0.01</b>, not the huge number, and the correction became <c>OrthoSize · 100</c>:
        /// at the default <c>Distance</c> 38 the rig sat 938 units back at OrthoSize 9 and <b>1038 at
        /// OrthoSize 10 — past the camera's 1000 far clip plane</b>. The entire world fell out of the
        /// slab and the screen became the clear colour, which since 0.102.11 is the ground's own grey.
        /// Pitch 90 is the shipped default, and it is the ONLY angle that trips it: at 89° the tangent
        /// is a healthy 57.3 and the term is under a unit.</para>
        ///
        /// <para>So the correction is written as <c>cos/sin</c> — cot θ directly, which is a clean
        /// −4·10⁻⁸ at 90° instead of a sign flip — and floored at ZERO rather than at a divisor. The
        /// clamp is the belt to that braces: nothing here may ever push the rig far enough to lose the
        /// world, because losing the world looks exactly like a rendering bug and reads as one.</para>
        /// </summary>
        private float RigDistance
        {
            get
            {
                if (!Orthographic) return Distance;
                float rad = Pitch * Mathf.Deg2Rad;
                // cot θ: 1 at 45°, 0 at 90°. Never a division by a value that can flip sign.
                float cot = Mathf.Cos(rad) / Mathf.Max(0.001f, Mathf.Sin(rad));
                float back = Distance + OrthoSize * Mathf.Max(0f, cot);
                return Mathf.Min(back, MaxRigDistance);
            }
        }

        /// <summary>The hard ceiling on how far back the ortho rig may sit. Well inside the camera's
        /// 1000 far plane and far beyond anything the sliders can legitimately ask for (Distance 90 +
        /// OrthoSize 60 × cot 45° = 150), so it only ever catches a bug.</summary>
        private const float MaxRigDistance = 400f;

        private Vector3 _followPoint;
        private Transform _followTarget;

        private void LateUpdate()
        {
            if (Target == null) return;

            ApplyProjection();

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

            Pose(transform, _followPoint, Pitch, Yaw, RigDistance);
        }

        /// <summary>
        /// Hang a camera off a point: <paramref name="distance"/> back along the view, looking at it. Public and
        /// static so the Editor preview (<c>ModelSetup.PreviewCamera</c>) places its camera with THIS code.
        ///
        /// The rotation is assigned directly rather than LookAt(Target): at Pitch 90 the view direction
        /// is parallel to Vector3.up, which makes LookAt's up-vector degenerate and the image spin
        /// or flip. This is exact by construction — the position was derived from the same rotation,
        /// so the point is dead centre of frame.
        /// </summary>
        public static void Pose(Transform camera, Vector3 at, float pitch, float yaw, float distance)
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            camera.SetPositionAndRotation(at + rot * Vector3.back * distance, rot);
        }
    }
}
