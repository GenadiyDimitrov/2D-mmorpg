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
        private Vector3 _predictPos;      // where the prediction says we are, independent of the buffer
        private float _predictSpeed;      // Unity units per second

        /// <summary>
        /// How fast the prediction ERROR bleeds off once prediction stops, per second (exponential).
        ///
        /// This is the whole fix for the rubber-band. Prediction runs AHEAD of the server by the
        /// network delay, so at the moment it stops the two disagree — and that disagreement is
        /// normal, not a mistake. Cancelling it outright is a jump backwards; obeying the next server
        /// position is the same jump wearing a different hat (and over a near-zero interpolation span,
        /// so it plays out in a frame or two — which is exactly what the 0.28.30 attempt still did).
        ///
        /// Instead we keep drawing the offset and let it DECAY. The character is never yanked; the
        /// error is spread across ~half a second of ordinary motion, where nobody can see it. At 3/s
        /// it is down to 5% within a second — and it is sub-half-a-unit (50 server units) to begin
        /// with, against skill ranges of 400-900.
        /// </summary>
        private const float ErrorDecayRate = 3f;

        /// <summary>How far the drawn position currently leads the server's. Self only; decays to zero.</summary>
        private Vector3 _selfError;

        /// <summary>Distance from the SERVER's last position to our predicted destination, and whether
        /// we have one yet. Used to notice that the server is no longer heading where we predicted.</summary>
        private float _serverDist;
        private bool _hasServerDist;

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
            _fromPos = _toPos = _predictPos = transform.position;
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
            //
            // The check is against the PREDICTION's position, not the drawn one, and it no longer
            // suppresses the buffer: the server stream stays authoritative and up to date at all
            // times, and what we draw is that stream plus a decaying error (see Update). Suppressing
            // it was what left a stale segment waiting to be obeyed the moment prediction stopped.
            if (_predicting && IsSelf)
            {
                // Is the server still taking us where we think we are going? Distance from the SERVER's
                // position to our predicted destination should shrink every sample. When it GROWS, the
                // server is walking us somewhere else and the prediction is already wrong — no need to
                // wait for the error to pile up past ReconcileSnapDistance.
                //
                // That wait was the second-long "teleport": tap a skill on a far target and the server
                // discards your walk and closes on the ENEMY instead. The client happily predicted the
                // opposite direction until the guard tripped, then jumped. This notices in one sample
                // (~100ms) instead of ~1s, and hands over without a jump because the error decays.
                float dist = (next - _predictTarget).magnitude;
                bool serverGoingElsewhere = _hasServerDist && dist > _serverDist + 0.01f;

                if (serverGoingElsewhere
                    || (next - _predictPos).sqrMagnitude > ReconcileSnapDistance * ReconcileSnapDistance)
                {
                    // NOT SnapTo. Ending prediction leaves the accumulated error to decay, so the
                    // correction is a short glide instead of a teleport. A genuine teleport is still
                    // caught below, by distance, where it belongs.
                    EndPrediction();
                }
                else
                {
                    _serverDist = dist;
                    _hasServerDist = true;
                }
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
            // Start from where we are DRAWN, not from the server's position: any error still decaying
            // from the last walk is part of where the player sees themselves, and restarting from the
            // server's point would put that error back on screen as a jump at the first step.
            _predictPos = transform.position;
            _predicting = true;
            // A fresh destination: the previous walk's distance tells us nothing about this one, and
            // carrying it over would read as "the server is going elsewhere" on the very first sample.
            _hasServerDist = false;
        }

        public void CancelPrediction() => EndPrediction();

        /// <summary>Whether we are still predicting a walk — i.e. whether the destination the player
        /// was given is still one we believe in.</summary>
        public bool IsPredicting => _predicting;

        /// <summary>
        /// Stop predicting, LEAVING the accumulated error in place to decay (see <see cref="ErrorDecayRate"/>).
        ///
        /// 🔴 This is the rubber-band, and it took three goes to state correctly. Prediction is ahead
        /// of the server by the network delay — that is what prediction IS — so at the handover the
        /// two positions disagree, and every attempt to resolve that disagreement AT the handover is a
        /// jump. 0.28.30 seeded the buffer with our real position, which fixed the first frame and
        /// nothing after it: the very next server position (still behind us) opened a segment from
        /// here to there, spanning the few milliseconds since the seed, so the character shot backwards
        /// in one frame anyway.
        ///
        /// There is nothing to resolve. The offset is legitimate, it is small, and it is allowed to
        /// simply fade out while the player is looking at something else.
        /// </summary>
        private void EndPrediction()
        {
            _predicting = false;
        }

        /// <summary>Put the entity AT a position with no interpolation — for spawns, teleports and
        /// re-entry, where sliding in from wherever it used to be would be a lie.</summary>
        public void SnapTo(Vector3 groundPos)
        {
            var at = groundPos + Vector3.up * HalfHeight;
            _fromPos = _toPos = _predictPos = at;
            _fromTime = _toTime = Time.time;
            _hasFrom = true;
            // A snap is the one case where the server is deliberately overriding us, so any prediction
            // and any accumulated error are wrong by definition — drop both rather than let them drag
            // the character back off the point we were just told to be at.
            _predicting = false;
            _selfError = Vector3.zero;
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
            // The character you drive moves on YOUR clock, not on the network's. This is the standard
            // split (Valve, Overwatch, every modern netcode talk): predict the entity you control,
            // interpolate the ones you do not. Everything before this — smoothing the chase, making it
            // frame-rate independent, dropping the interpolation delay for self — was treating the
            // symptom. The character was still waiting for the network to tell it where it was.
            //
            // This is only safe because the walk is DETERMINISTIC and the server is still the
            // authority: it runs the same straight-line-at-Speed simulation, and SetTarget corrects us
            // the moment the two disagree by more than ReconcileSnapDistance.
            if (!_hasFrom) return;

            // 🔴 SELF IS DELAYED TOO, and that is not the regression it looks like.
            //
            // Self was given a delay of ZERO to keep the tap responsive. But zero delay makes this
            // interpolation DEGENERATE: renderAt is now, and _toTime is the instant the newest sample
            // arrived, so t is always 1. Self never interpolated at all — it jumped straight to each
            // arriving position and sat there, a 10Hz staircase of ~0.25-unit hops. That is a
            // permanent, visible jitter on every metre the SERVER moves you (an attack chase, the tail
            // of a walk after a skill cancels the prediction) and it survived all three attempts at
            // the rubber-band, because it was never the rubber-band.
            //
            // Responsiveness does not come from the delay any more — it comes from PREDICTION, which
            // draws your own walks on your own clock. So self can afford to render the past like
            // everything else for the motion it does not predict, and does.
            float span = _toTime - _fromTime;

            // 🔴 THE DELAY IS ONE SAMPLE INTERVAL, MEASURED — never a constant.
            //
            // This was `(Time.time - InterpolationDelay - _fromTime) / span` with a fixed 150ms delay,
            // against samples arriving every 100ms and a buffer holding exactly TWO of them. Rendering
            // 150ms in the past needs a sample at least 150ms old; the oldest one we keep is 100ms old.
            // So at every arrival t clamped to 0 and the position FROZE for 50ms, then interpolated for
            // 50ms, then froze again — starved half of every cycle.
            //
            // For self that was the visible bug: the base position is frozen while the prediction error
            // keeps decaying, so the character slips BACKWARDS during the frozen half and jumps forward
            // when it unfreezes. Walking with a step back on every frame, exactly as reported — and it
            // was in the interpolator the whole time, not in the prediction anyone kept rewriting.
            //
            // Starting the segment at _toTime makes the lag exactly one inter-arrival interval, whatever
            // that interval turns out to be: t = 0 (at _fromPos) the instant a sample lands, reaching
            // _toPos one span later, just as the next one arrives. Self-adjusting, so a change to the
            // server's send rate can never desynchronise it again. When an update is genuinely late the
            // clamp holds at _toPos — the newest thing we actually know — instead of freezing at an
            // older one.
            float t = span > 0.0001f ? Mathf.Clamp01((Time.time - _toTime) / span) : 1f;
            var basePos = Vector3.Lerp(_fromPos, _toPos, t);

            if (!IsSelf)
            {
                transform.position = basePos;
                return;
            }

            // ----- SELF: the server stream PLUS a decaying error --------------------------------
            //
            // basePos is what the server says. While predicting we draw the prediction instead and
            // measure how far ahead of the server it is; once it stops, that measurement is all that
            // is left, and it fades. Either way the character's drawn position is continuous — there
            // is no frame where it changes rule and jumps, which is what every previous attempt at
            // this bug got wrong.
            if (_predicting)
            {
                float step = _predictSpeed * Time.deltaTime;
                var to = _predictTarget - _predictPos;

                if (to.sqrMagnitude <= step * step)
                {
                    _predictPos = _predictTarget;
                    EndPrediction();          // arrived; the error simply starts decaying
                }
                else
                {
                    _predictPos += to.normalized * step;
                }

                _selfError = _predictPos - basePos;
                transform.position = _predictPos;
                return;
            }

            // Frame-rate independent decay. Using deltaTime as a blend factor directly is the bug that
            // started this whole chase — at 120ms a frame it overshoots past the target and back.
            _selfError *= Mathf.Exp(-ErrorDecayRate * Time.deltaTime);
            if (_selfError.sqrMagnitude < 0.000001f) _selfError = Vector3.zero;
            transform.position = basePos + _selfError;
        }

        private void LateUpdate()
        {
            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam != null) transform.rotation = _cam.rotation;   // billboard toward the camera
        }
    }
}
