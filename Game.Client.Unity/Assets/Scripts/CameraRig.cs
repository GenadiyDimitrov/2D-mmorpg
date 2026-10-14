using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Follows the player, looking at its chest. Pitch 90° (straight down, north up) is the default; one finger
    /// dragged across the world tilts it down to 0° (level with the horizon) and turns it (`BL-344` step 8).
    ///
    /// Distance is the player's to choose: **pinch to zoom** (0.1-20, logarithmic), and the choice is
    /// remembered across launches (PlayerPrefs). Guessing a distance for someone else's phone is how we
    /// ended up with two taste calls in a row.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform Target;

        [Range(0f, 90f)] public float Pitch = 90f;    // 90 = straight down, 0 = level with the horizon
        public float Yaw = 0f;
        public float Distance = 20f;                  // starting zoom; pinch overrides and persists
        public float Follow = 12f;                    // position smoothing

        // 🔑 PERSPECTIVE ONLY (owner, 2026-10-13: *"remove the ortho option … as we now have tilting ortho brakes the
        // visuals"*). The orthographic mode and its rig-distance correction are gone; git history has them.

        /// <summary>The zoom range, in world units from the character's chest (owner, 2026-10-13: 0.1-30, "0 is
        /// inside the char"). At 0.1 you are inside the body; the near clip plane follows (see ApplyProjection).</summary>
        public const float MinDistance = 0.1f, MaxDistance = 20f;   // owner, 0.264.0: "decrease the max zoomout to 20"

        /// <summary>
        /// 🔑 ZOOM IS LOGARITHMIC: every step changes the distance by the same RATIO, never the same number of units.
        /// What you see scales with 1/distance, so 1 → 2 halves everything on screen while 29 → 30 changes it by 3% —
        /// a linear pinch felt like a jump close in and nothing far out. His ask: *"each step to look equal"*. A full
        /// pinch of <see cref="PinchPixelsForFullRange"/> pixels crosses the whole 0.1-30 range (×300).
        /// </summary>
        public float PinchPixelsForFullRange = 1200f;
        public float ScrollRatio = 1.12f;             // editor mouse wheel: ×/÷ this per notch

        /// <summary>The camera looks at the character's CHEST, not its feet, so a level view (pitch 0) is at eye
        /// height rather than lying in the ground plane. Scaled with Settings → Entity size.</summary>
        public const float LookHeight = 0.9f;

        /// <summary>Distance → 0..1 along the logarithmic range: what the Settings zoom slider holds, so its steps
        /// are as even as the pinch's.</summary>
        public static float ZoomToSlider(float distance) =>
            Mathf.InverseLerp(Mathf.Log(MinDistance), Mathf.Log(MaxDistance), Mathf.Log(Mathf.Clamp(distance, MinDistance, MaxDistance)));

        public static float SliderToZoom(float t) =>
            Mathf.Exp(Mathf.Lerp(Mathf.Log(MinDistance), Mathf.Log(MaxDistance), Mathf.Clamp01(t)));

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

        /// <summary>The tilt range (owner, 2026-10-13: 0-90): straight down to level with the horizon. The 45° floor
        /// was the ortho rig's, and went with it.</summary>
        public const float MinPitch = 0f, MaxPitch = 90f;
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
                    // Fingers apart = zoom IN: a smaller distance, by a RATIO (see PinchPixelsForFullRange).
                    float perPixel = Mathf.Log(MaxDistance / MinDistance) / PinchPixelsForFullRange;
                    ZoomBy(Mathf.Exp(-(gap - _lastPinch) * perPixel));
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
                    ZoomBy(Mathf.Pow(ScrollRatio, -scroll));
                    Persist();
                }
            }
        }

        private void ZoomBy(float ratio) => Distance = Mathf.Clamp(Distance * ratio, MinDistance, MaxDistance);

        /// <summary>Written only when a gesture ENDS — PlayerPrefs.Save() every pinch frame would hit
        /// the disk ~60 times a second.</summary>
        private void Persist()
        {
            if (Mathf.Approximately(_savedDistance, Distance)) return;
            _savedDistance = Distance;
            PlayerPrefs.SetFloat(DistanceKey, Distance);
            PlayerPrefs.Save();
        }

        /// <summary>Settings → Camera zoom: set the distance and remember it, like a pinch does.</summary>
        public void SetZoom(float distance)
        {
            Distance = Mathf.Clamp(distance, MinDistance, MaxDistance);
            Persist();
        }

        /// <summary>
        /// Perspective always (a scene or an old build may have left the camera orthographic), and a near clip
        /// plane that follows the zoom. The scene's 0.3 would cut away everything closer than that — at a zoom
        /// of 0.1 that is the whole character and the ground at its feet. A fifth of the distance keeps the
        /// near plane well in front of the camera at every zoom while giving the depth buffer as much range as
        /// it can have; it never goes above the old 0.3, so nothing that drew before stops drawing.
        /// </summary>
        private void ApplyProjection()
        {
            var cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            if (cam.orthographic) cam.orthographic = false;
            cam.nearClipPlane = Mathf.Clamp(Distance * 0.2f, 0.02f, 0.3f);
        }

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

            Pose(transform, _followPoint + Vector3.up * (LookHeight * EntityManager.EntityScale), Pitch, Yaw, Distance);
            Focus = _followPoint;
        }

        /// <summary>`BL-356` — the point the camera follows (you), on the ground, or null before there is one. The scenery
        /// and the ground stream AROUND THIS, not around the middle of the screen: tilted toward the horizon the screen's
        /// middle runs off to the map border, and a circle drawn round that point left the ground at your feet bare (the
        /// owner, at pitch 30-35 and zoom 30: "the center is not me but somewhere in the distance").</summary>
        public static Vector3? Focus;

        private void OnDisable() => Focus = null;

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
