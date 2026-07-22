using System;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// One visible entity: an UPRIGHT BILLBOARD that (a) plays back its server positions with
    /// SNAPSHOT INTERPOLATION (see <see cref="Update"/>) and (b) faces the camera every frame.
    /// Because it's an upright billboard (not a floor decal), tilting the camera to 2.5D "just works"
    /// with no change here — swap the sphere for a 3D model and it's a pure visual upgrade.
    /// </summary>
    public class EntityView : MonoBehaviour
    {
        public Guid Id;
        public bool IsSelf;

        private const float HalfHeight = 0.75f;   // lifts the billboard so its base sits on the ground

        /// <summary>
        /// How far BEHIND the newest server position we render, in seconds.
        ///
        /// This is the whole trick of snapshot interpolation: by deliberately staying one update in
        /// the past, we always have a position on BOTH sides of the moment being drawn, so movement
        /// is an exact interpolation between two known facts rather than a guess at the present. The
        /// cost is a fixed, invisible delay; the gain is that jitter in arrival times stops being
        /// visible at all.
        ///
        /// 150ms covers the server's 100ms tick plus normal jitter. Too small and the buffer runs dry
        /// (which looks like the old stutter); too large and the world feels laggy to control.
        /// </summary>
        private const float InterpolationDelay = 0.15f;

        /// <summary>The two most recent server positions and when each ARRIVED. Two is enough: we only
        /// ever draw between the newest pair, and keeping a longer history would only add latency.</summary>
        private Vector3 _fromPos, _toPos;
        private float _fromTime, _toTime;
        private bool _hasFrom;

        private Transform _cam;
        private Renderer _renderer;
        private Color _color = Color.white;
        private bool _dead;

        public void Init(Color color)
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                // Unlit so it reads the same at any camera angle (no scene lighting set up yet).
                // Via UnlitMaterials, NOT Shader.Find here — see that class for why the direct call
                // made every entity magenta on the phone while looking fine in the Editor.
                var material = UnlitMaterials.Create(color);
                if (material != null) _renderer.material = material;
            }
            SetColor(color);
            _cam = Camera.main != null ? Camera.main.transform : null;

            // Seed the buffer with where Create already placed us. Without this the first Update has
            // no samples and the entity would sit at the origin until its second server position.
            _fromPos = _toPos = transform.position;
            _fromTime = _toTime = Time.time;
            _hasFrom = true;
        }

        public void SetColor(Color color)
        {
            if (_color == color && _renderer != null) return;
            _color = color;
            Apply();
        }

        /// <summary>Corpses stay visible but dim — a kill should read as "that thing died", not as
        /// an entity mysteriously popping out of existence.</summary>
        public void SetDead(bool dead)
        {
            if (_dead == dead) return;
            _dead = dead;
            Apply();
        }

        private void Apply()
        {
            if (_renderer == null || _renderer.material == null) return;
            UnlitMaterials.SetColor(_renderer.material, _dead ? _color * 0.3f : _color);
        }

        /// <summary>
        /// Record a newly arrived server position (ground plane); height is added here.
        ///
        /// The PREVIOUS target becomes the segment's start, and the arrival times become the segment's
        /// duration — so playback speed is set by how fast updates actually came, not by a constant we
        /// picked. An entity that was sent twice in 100ms and one sent twice in 400ms both move at
        /// exactly the speed the server described.
        /// </summary>
        public void SetTarget(Vector3 groundPos)
        {
            var next = groundPos + Vector3.up * HalfHeight;

            // Ignore a repeat of the same place: it carries no motion, and letting it start a new
            // segment would stretch the previous one into a stall.
            if (_hasFrom && (next - _toPos).sqrMagnitude < 0.0001f) return;

            // A TELEPORT is not movement. At the 250 speed cap an entity covers ~0.25 Unity units per
            // tick, so anything past this is a gatekeeper, a respawn or a debug jump — and
            // interpolating it would slide the character smoothly across the entire map.
            const float teleportDistance = 5f;
            if (_hasFrom && (next - _toPos).sqrMagnitude > teleportDistance * teleportDistance)
            {
                SnapTo(groundPos);
                return;
            }

            _fromPos = _hasFrom ? _toPos : next;
            _fromTime = _hasFrom ? _toTime : Time.time;
            _toPos = next;
            _toTime = Time.time;
            _hasFrom = true;
        }

        /// <summary>Put the entity AT a position with no interpolation — for spawns, teleports and
        /// re-entry, where sliding in from wherever it used to be would be a lie.</summary>
        public void SnapTo(Vector3 groundPos)
        {
            var at = groundPos + Vector3.up * HalfHeight;
            _fromPos = _toPos = at;
            _fromTime = _toTime = Time.time;
            _hasFrom = true;
            transform.position = at;
        }

        /// <summary>
        /// SNAPSHOT INTERPOLATION: draw where the entity WAS <see cref="InterpolationDelay"/> ago,
        /// interpolated linearly between the two server positions that bracket that moment.
        ///
        /// What this replaces and why it mattered: the old code chased the newest position with
        /// `Lerp(position, target, k * dt)`. An exponential chase never arrives — it decelerates as it
        /// closes — so every time an update was late the entity visibly slowed down and then lurched
        /// when the next one landed. And updates ARE irregular here by design: the server sends only
        /// what changed, so a walking mob can produce ten updates a second and a pausing one none at
        /// all. The chase turned that irregularity straight into visible stutter.
        ///
        /// Interpolating between two KNOWN positions instead means the motion is constant-speed and
        /// correct between them, and network jitter is absorbed by the delay rather than displayed.
        ///
        /// When the buffer runs dry (the entity stopped, or an update is genuinely late) the clamp at
        /// t = 1 simply holds it at the last known position — which is the truth — instead of
        /// extrapolating it somewhere the server never said it was.
        /// </summary>
        private void Update()
        {
            if (!_hasFrom) return;

            float renderAt = Time.time - InterpolationDelay;
            float span = _toTime - _fromTime;

            // A zero or negative span means both samples arrived in the same frame; there is nothing
            // to interpolate along, so sit on the newest.
            float t = span > 0.0001f ? Mathf.Clamp01((renderAt - _fromTime) / span) : 1f;
            transform.position = Vector3.Lerp(_fromPos, _toPos, t);
        }

        private void LateUpdate()
        {
            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam != null) transform.rotation = _cam.rotation;   // billboard toward the camera
        }
    }
}
