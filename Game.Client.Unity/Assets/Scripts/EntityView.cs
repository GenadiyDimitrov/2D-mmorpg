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

        // ----- client-side prediction (SELF ONLY) -------------------------------------------------
        private bool _predicting;
        private Vector3 _predictTarget;
        private float _predictSpeed;      // Unity units per second

        /// <summary>
        /// How far the server may disagree with our prediction before we give up and SNAP to it.
        ///
        /// Small differences are normal and must be tolerated — correcting every one of them is
        /// literally rubber-banding. A large one means the server did something we did not predict
        /// (refused the move, rooted us, a knockback, a teleport), and there the server is simply
        /// right: it is authoritative, and pretending otherwise would let the client walk through
        /// walls it cannot see.
        /// </summary>
        private const float ReconcileSnapDistance = 2.5f;

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

            // RECONCILIATION. While predicting, the server's position is a CHECK, not an instruction:
            // accept it silently when it is close (that is prediction working), and snap only when it
            // is far enough away that we clearly predicted something the server refused — a move
            // during a root, a knockback, a teleport. Correcting the small differences too is what
            // turns authoritative movement into rubber-banding.
            if (_predicting && IsSelf)
            {
                // Deliberately NOT written into the interpolation buffer — see EndPrediction. While
                // predicting we are ahead of this position on purpose, and storing it would be storing
                // the very point that used to yank us backwards.
                if ((next - transform.position).sqrMagnitude
                        > ReconcileSnapDistance * ReconcileSnapDistance)
                {
                    transform.position = next;   // the server did something we did not predict
                    EndPrediction();
                }
                return;
            }

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

        /// <summary>Begin predicting a walk to <paramref name="groundTarget"/> at
        /// <paramref name="speed"/> Unity units per second. Self only.</summary>
        public void PredictTo(Vector3 groundTarget, float speed)
        {
            _predictTarget = groundTarget + Vector3.up * HalfHeight;
            _predictSpeed = speed;
            _predicting = true;
        }

        public void CancelPrediction() => EndPrediction();

        /// <summary>
        /// Stop predicting AND hand the current position over to the interpolator.
        ///
        /// 🔴 This is the rubber-band. While predicting, every arriving server position was written
        /// straight into the interpolation buffer — and that position is behind us BY DESIGN, because
        /// prediction is ahead of the network. So the instant prediction stopped, Update() fell back
        /// to interpolation and teleported the character to that stale point. It fired at the end of
        /// EVERY predicted walk, and most visibly when a skill cut one short: tap, walk, cast, snap
        /// back to where you started.
        ///
        /// Seeding the buffer with where we ACTUALLY are means the handover is silent, and the next
        /// server position simply interpolates forward from here.
        /// </summary>
        private void EndPrediction()
        {
            _predicting = false;
            _fromPos = _toPos = transform.position;
            _fromTime = _toTime = Time.time;
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
            // ----- SELF: predict, then reconcile ------------------------------------------------
            //
            // The character you drive moves on YOUR clock, not on the network's. This is the standard
            // split (Valve, Overwatch, every modern netcode talk): predict the entity you control,
            // interpolate the ones you do not. Everything before this — smoothing the chase, making it
            // frame-rate independent, dropping the interpolation delay for self — was treating the
            // symptom. The character was still waiting for the network to tell it where it was.
            //
            // This is only safe because the walk is DETERMINISTIC and the server is still the
            // authority: it runs the same straight-line-at-Speed simulation, and SetTarget below
            // corrects us the moment the two disagree by more than a step.
            if (_predicting)
            {
                var flatTarget = _predictTarget;
                var here = transform.position;
                float step = _predictSpeed * Time.deltaTime;
                var to = flatTarget - here;

                if (to.sqrMagnitude <= step * step)
                {
                    transform.position = flatTarget;
                    EndPrediction();         // arrived; hand over cleanly — see EndPrediction
                }
                else
                {
                    transform.position = here + to.normalized * step;
                }
                return;
            }

            if (!_hasFrom) return;

            // YOUR OWN character is NOT delayed. Interpolation deliberately renders the past, which is
            // right for entities whose next move you cannot know — and wrong for the one you are
            // driving: it put 150ms between the tap and the character (and the camera follows him, so
            // the whole world lagged the input). That is the regression that made movement feel worse
            // rather than better.
            //
            // Big-engine practice splits these two jobs and so do we: OTHERS are interpolated,
            // SELF is not. The proper next step for self is client-side PREDICTION — simulate the same
            // walk the server does and reconcile when it disagrees. The simulation is deterministic
            // (walk toward the target at Speed), so it is available; it needs the move target on the
            // wire, which is why it is its own change and not smuggled in here.
            float renderAt = Time.time - (IsSelf ? 0f : InterpolationDelay);
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
