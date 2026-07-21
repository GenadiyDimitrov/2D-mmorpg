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
                    // Fingers apart = zoom IN = smaller distance.
                    Distance = Mathf.Clamp(Distance - (gap - _lastPinch) * PinchSpeed,
                                           MinDistance, MaxDistance);
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

        private void LateUpdate()
        {
            if (Target == null) return;

            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            var desired = Target.position + rot * Vector3.back * Distance;
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * Follow);

            // Assign the rotation directly rather than LookAt(Target): at Pitch 90 the view direction
            // is parallel to Vector3.up, which makes LookAt's up-vector degenerate and the image spin
            // or flip. This is exact by construction — `desired` was derived from the same rotation.
            transform.rotation = rot;
        }
    }
}
