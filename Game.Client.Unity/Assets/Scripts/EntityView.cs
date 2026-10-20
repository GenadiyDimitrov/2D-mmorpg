using System;
using UnityEngine;
using Game.Shared;

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
        public bool IsDead => _dead;
        /// <summary>The last teleport counter seen (EntityDto.Warp) — `BL-344` tells a res in place from a respawn.</summary>
        public byte LastWarp => _warp;

        private const float HalfHeight = 0.75f;   // lifts the billboard so its base sits on the ground

        /// <summary>The newest server position (for the prediction's checks in SetTarget) — what is DRAWN comes
        /// from the sample buffer below.</summary>
        private Vector3 _toPos;
        private bool _hasFrom;

        /// <summary>
        /// 🔑 THE SAMPLE BUFFER, stamped with SERVER time (0.271.0). Each position carries the tick the server
        /// produced it on (<see cref="NetClock"/>), and <see cref="Update"/> draws the moment
        /// <see cref="NetClock.RenderTime"/> between the two samples that bracket it.
        ///
        /// <para>It replaced a two-sample interpolator timed by ARRIVAL: the segment's length was the gap between
        /// two packets landing. A phone's network delivers in bursts — two deltas in one frame, then nothing for
        /// 200ms — so every mob raced through one segment and froze in the next. That was the owner's *"fps is 60
        /// but the animation jitters"*, and why your own character joined in near mobs: in a melee chase the
        /// server, not the prediction, is what moves you.</para>
        /// </summary>
        private const int MaxSamples = 12;
        private readonly double[] _sTime = new double[MaxSamples];
        private readonly Vector3[] _sPos = new Vector3[MaxSamples];
        private int _sCount;

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

        /// <summary>
        /// ARRIVAL HOLD — the overshoot-and-snap on STOP (owner, playtest-20 `53e`/`61j`: *"arriving at
        /// the destination overshoots with inertia and snaps back"*).
        ///
        /// The decaying error is right for a walk that is INTERRUPTED and wrong for one that ENDS. At
        /// the moment we arrive, the server's stream still lags by one sample interval, so the error we
        /// leave behind points FORWARD — from the server's position to the destination. The frames that
        /// follow draw <c>basePos + error</c> while basePos is itself still advancing toward that same
        /// destination, so the two additions carry the character straight PAST it, and the ease-back is
        /// the error finally decaying. Nothing was wrong with either half; the sum was.
        ///
        /// So an arrival holds AT the destination instead, and simply waits for the server to reach it —
        /// the error is then measured, not decayed, and lands on zero by itself. The hold gives up if it
        /// stops closing or outlives <see cref="ArrivalHoldSeconds"/>, which is what keeps a disagreement
        /// (the server stopped you somewhere else) from freezing the character on a point it never
        /// reached; from there the ordinary decay takes over exactly as before.
        /// </summary>
        private bool _arrived;
        private Vector3 _arrivedTarget;
        private float _arrivedSince;
        private float _arrivedDist;
        private const float ArrivalHoldSeconds = 0.75f;
        private const float ArrivalSettled = 0.02f;

        /// <summary>
        /// IS THE SERVER WALKING OUR WALK? (2026-10-01, three rubber-bands in one report.) A prediction
        /// is only believed while the server's own samples HEAD for our destination. Until one does
        /// (<see cref="_serverConfirmed"/>) the samples still in flight belong to the previous order and
        /// prove nothing either way; once one has, any sample heading elsewhere ends the prediction at
        /// once — the server took us toward a mob (an out-of-range skill) or somewhere we did not ask.
        ///
        /// <para>This replaced "is the distance to our destination shrinking", which a server walking
        /// at an angle of up to 90° off our line passes every sample — so a skill on a mob roughly ahead
        /// was predicted all the way to the tapped point and yanked back from there. And it watches for
        /// SILENCE too: a server that never moves us (the order was dropped) or stops short sends no
        /// samples at all, and the old check only ran when one arrived.</para>
        /// </summary>
        private bool _serverConfirmed;
        private float _predictStartTime;
        private float _lastServerMoveTime;

        /// <summary>cos 25°: how close to our line the server's step must point to count as ours.</summary>
        private const float HeadingAgreeCos = 0.906f;

        /// <summary>Below this distance from our destination (Unity units) heading is noise, not intent.</summary>
        private const float HeadingCheckMin = 0.2f;

        /// <summary>No server step heading our way this long after the tap → the order went nowhere.
        /// Generous on purpose: it has to cover a phone's round trip, not a LAN's.</summary>
        private const float UnconfirmedTimeout = 0.8f;

        /// <summary>A confirmed walk whose server stream goes quiet this long has stopped short (samples
        /// arrive every 0.1s while it moves).</summary>
        private const float StalledTimeout = 0.4f;

        /// <summary>The last <c>EntityDto.Warp</c> we saw for this entity, and whether we have one at
        /// all yet. A CHANGE means the server teleported it — see <see cref="SetTarget"/>.</summary>
        private byte _warp;
        private bool _hasWarp;

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

        // ----- `BL-93` the 3D model, if one exists for this creature -------------------------------
        //
        // Null for everything until art lands, and every branch below is written so that null means
        // "behave exactly as this class did before the feature existed".
        private Transform _model;
        private Animator _animator;
        private bool _hasSpeed, _hasDead, _hasSwingSpeed, _hasMoveMul;
        // The states a swing or a cast plays in. A humanoid has an upper-body "Action" layer (legs keep running under a
        // bow shot); a creature has no shoulders to mask, so its swing replaces locomotion on the base layer.
        private int _actionLayer;
        private string _restState;                          // what a swing returns to: "Empty" or "Locomotion"
        private bool _hasAttackState, _hasCastState;
        private float _lenMelee, _lenBow, _lenSpell;        // clip lengths, baked into parameter defaults by ModelSetup
        private float _walkNative, _runNative;              // ground speed each leg cycle covers, model units/s
        private bool _casting;

        // `BL-103` — what is in its hands. Remembered so a re-sent spawn with the same gear rebuilds nothing.
        private readonly System.Collections.Generic.List<GameObject> _held = new System.Collections.Generic.List<GameObject>();
        private WeaponType _heldWeapon;
        private bool _heldMagic, _heldShield, _heldDressed;
        private Vector3 _lastDrawn;
        private bool _hasLastDrawn;
        private float _facing;            // degrees, the direction the model is turned toward
        private bool _hasFacing;

        /// <summary>How fast a model turns to face where it is walking, degrees/second. Fast enough
        /// that it never looks like it is skating sideways, slow enough that a click behind you reads
        /// as a turn rather than a teleport of the head.</summary>
        private const float TurnSpeed = 720f;

        /// <summary>Below this drawn speed (Unity units/sec) we do not re-aim: interpolation jitter at
        /// a standstill has a direction, and obeying it makes an idle character twitch on the spot.</summary>
        private const float FacingDeadZone = 0.05f;

        // `BL-82` — the stealth fade and the god ring. Both are SELF-ONLY effects; nothing here is ever
        // driven by another player's state, because nothing on the wire describes it.
        private float _alpha = 1f;
        private Material _opaqueMaterial, _fadeMaterial;
        private Transform _halo;
        private Renderer _haloRenderer;

        // The model's half of the fade: each renderer's IMPORTED materials, kept while it wears FadeLit copies.
        private readonly System.Collections.Generic.Dictionary<Renderer, Material[]> _modelSolid =
            new System.Collections.Generic.Dictionary<Renderer, Material[]>();
        private static Shader _fadeShader;
        private static bool _fadeShaderSearched;

        public void Init(Color color)
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                // Unlit so it reads the same at any camera angle (no scene lighting set up yet).
                // Via UnlitMaterials, NOT Shader.Find here — see that class for why the direct call
                // made every entity magenta on the phone while looking fine in the Editor.
                var material = UnlitMaterials.Create(color);
                if (material != null) { _renderer.material = material; _opaqueMaterial = _renderer.material; }
            }
            SetColor(color);
            _cam = Camera.main != null ? Camera.main.transform : null;

            // Seed the buffer with where Create already placed us. Without this the first Update has
            // no samples and the entity would sit at the origin until its second server position.
            _toPos = _predictPos = transform.position;
            _hasFrom = true;
            _sCount = 0;
            PushSample(NetClock.Now, transform.position);
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
            // `BL-93` — a body falls over instead of just dimming. Still dims as well: a corpse has to
            // read as a corpse at top-down distance, where a lying-down mesh is a small smudge.
            if (_animator != null && _hasDead) _animator.SetBool("Dead", dead);
            // A body that dies mid-swing must fall with its arms too: the upper-body layer would otherwise keep
            // overriding the death pose with the rest of the swing.
            if (dead && _animator != null && _actionLayer > 0) _animator.Play("Empty", _actionLayer, 0f);
        }

        /// <summary>How see-through this marker draws (`BL-82`): 1 solid, 0.7 stealthed, 0.4 invisible.
        /// Only ever called for the SELF marker.
        ///
        /// <para>The material is swapped rather than made transparent up front, and that is deliberate.
        /// Every marker in this project is opaque on purpose (see <c>UnlitMaterials.CreateTransparent</c>:
        /// transparency in URP costs a second material, a render queue and back-to-front sorting), so a
        /// character who is not hiding keeps exactly the rendering he had before this feature existed.
        /// The transparent copy is built on the first fade and then kept — a rogue toggles a lot.</para></summary>
        public void SetOpacity(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            if (Mathf.Approximately(_alpha, alpha)) return;
            _alpha = alpha;

            if (_renderer != null)
            {
                bool fade = _alpha < 0.999f;
                if (fade && _fadeMaterial == null)
                    _fadeMaterial = UnlitMaterials.CreateTransparent(_color);

                var want = fade ? _fadeMaterial : _opaqueMaterial;
                if (want != null && _renderer.sharedMaterial != want)
                {
                    // Re-read through the getter and keep THAT: Unity hands a renderer its own instance
                    // of a material, and holding the object we passed in would make the check above
                    // false on every toggle — a fresh material per hide, for as long as the rogue plays.
                    _renderer.material = want;
                    if (fade) _fadeMaterial = _renderer.material;
                    else _opaqueMaterial = _renderer.material;
                }
            }
            Apply();
            ApplyModelFade();
        }

        /// <summary>The fade for a body that wears a MODEL — the sphere above is un-drawn then, so swapping its
        /// material alone faded nothing you could see (the owner, after `BL-344`: *"/invis don't seem to work on
        /// the models"*). Every renderer under the model, weapons included, swaps onto a FadeLit copy carrying its
        /// own texture and colour; going solid puts the imported materials back. Safe to call any time: it also
        /// picks up a weapon dressed while you were already faded.</summary>
        private void ApplyModelFade()
        {
            if (_model == null) return;

            if (_alpha >= 0.999f)
            {
                foreach (var kv in _modelSolid)
                {
                    if (kv.Key == null) continue;                 // a weapon swapped out while faded
                    foreach (var m in kv.Key.sharedMaterials) if (m != null) Destroy(m);
                    kv.Key.sharedMaterials = kv.Value;
                }
                _modelSolid.Clear();
                return;
            }

            if (!_fadeShaderSearched)
            {
                _fadeShaderSearched = true;
                _fadeShader = Resources.Load<Shader>("Shaders/FadeLit") ?? Shader.Find("L2Clone/FadeLit");
                if (_fadeShader == null) Debug.LogError("[EntityView] FadeLit shader missing — models will not fade.");
            }
            if (_fadeShader == null) return;

            var gone = new System.Collections.Generic.List<Renderer>();
            foreach (var r in _modelSolid.Keys) if (r == null) gone.Add(r);
            foreach (var r in gone) _modelSolid.Remove(r);

            foreach (var r in _model.GetComponentsInChildren<Renderer>(true))
            {
                if (!_modelSolid.ContainsKey(r))
                {
                    var solid = r.sharedMaterials;
                    var faded = new Material[solid.Length];
                    for (int i = 0; i < solid.Length; i++) faded[i] = FadeCopy(solid[i]);
                    _modelSolid[r] = solid;
                    r.sharedMaterials = faded;
                }
                foreach (var m in r.sharedMaterials) if (m != null) m.SetFloat("_Alpha", _alpha);
            }
        }

        private static Material FadeCopy(Material src)
        {
            var m = new Material(_fadeShader);
            if (src == null) return m;
            // URP Lit names these _BaseMap/_BaseColor, older imports _MainTex/_Color; mainTexture follows [MainTexture].
            var tex = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : src.mainTexture;
            if (tex != null)
            {
                m.SetTexture("_BaseMap", tex);
                string st = src.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                if (src.HasProperty(st))
                {
                    m.SetTextureScale("_BaseMap", src.GetTextureScale(st));
                    m.SetTextureOffset("_BaseMap", src.GetTextureOffset(st));
                }
            }
            var color = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor")
                      : src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white;
            color.a = 1f;
            m.SetColor("_BaseColor", color);
            return m;
        }

        /// <summary>A translucent shell around the marker — the god-mode ring (`BL-82`). Built on first
        /// use and then just shown/hidden, and it carries NO collider: it is bigger than the marker, and
        /// a ring that swallowed taps would make an admin unable to click past himself.</summary>
        public void SetHalo(bool on, Color color)
        {
            if (!on && _halo == null) return;   // the overwhelmingly common case: never build it

            if (_halo == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Halo";
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one * 1.5f;   // relative to the marker, so it scales with it
                var collider = go.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                _haloRenderer = go.GetComponent<Renderer>();
                var material = UnlitMaterials.CreateTransparent(color);
                if (material != null && _haloRenderer != null) _haloRenderer.material = material;
                _halo = go.transform;
            }

            if (_haloRenderer != null)
            {
                UnlitMaterials.SetColor(_haloRenderer.material, color);
                _haloRenderer.enabled = on;
            }
            _halo.gameObject.SetActive(on);
        }

        private void Apply()
        {
            if (_renderer == null || _renderer.material == null) return;
            var color = _dead ? _color * 0.3f : _color;
            color.a = _alpha;                      // last word: _color * 0.3f dims alpha too
            UnlitMaterials.SetColor(_renderer.material, color);
        }

        /// <summary>
        /// Record a newly arrived server position (ground plane); height is added here.
        /// <paramref name="serverTime"/> is when the SERVER was at it (<see cref="NetClock.Observe"/>), which is
        /// what places it in the buffer — never the moment its packet happened to land.
        /// </summary>
        public void SetTarget(Vector3 groundPos, byte warp, double serverTime)
        {
            // 🔑 A TELEPORT IS DECLARED, NOT MEASURED (playtest 29: *"phase shift don't visually update
            // my position"*). The server bumps EntityDto.Warp on every reposition that is not the walk
            // simulation, so a jump is now a fact on the wire instead of something inferred from how
            // far the position moved. It has to be: a rung-1 Phase Shift is 200 server units — TWO
            // Unity units — which fits under the 5-unit teleport test below AND under the 2.5-unit
            // prediction tolerance above, so the client used to keep drawing the walk it was
            // predicting while the server (and every mob) had already moved the character 200 away.
            //
            // The first sample only records the number: an entity that spawns into view has already
            // been snapped by Create, and treating its starting value as a change would re-snap
            // everything on screen the moment it appears.
            if (_hasWarp && warp != _warp)
            {
                _warp = warp;
                SnapTo(groundPos, serverTime);
                return;
            }
            _warp = warp;
            _hasWarp = true;

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
                // Is the server still taking us where we think we are going? Judge by the HEADING of
                // its step, from the last sample to this one, against the line from that sample to our
                // destination — see _serverConfirmed. Noticing in one sample (~100ms) is what turns a
                // disagreement into a short glide instead of a walk to the tapped point and a yank back.
                bool headingElsewhere = false;
                var stepDir = next - _toPos; stepDir.y = 0f;
                var wantDir = _predictTarget - _toPos; wantDir.y = 0f;
                if (_hasFrom && stepDir.sqrMagnitude > 0.000001f)
                {
                    _lastServerMoveTime = Time.time;
                    if (wantDir.sqrMagnitude > HeadingCheckMin * HeadingCheckMin)
                    {
                        if (Vector3.Dot(stepDir.normalized, wantDir.normalized) >= HeadingAgreeCos)
                            _serverConfirmed = true;
                        else if (_serverConfirmed)
                            headingElsewhere = true;
                    }
                }

                if (headingElsewhere
                    || (next - _predictPos).sqrMagnitude > ReconcileSnapDistance * ReconcileSnapDistance)
                {
                    // NOT SnapTo. Ending prediction leaves the accumulated error to decay, so the
                    // correction is a short glide instead of a teleport. A genuine teleport is still
                    // caught below, by distance, where it belongs.
                    EndPrediction();
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
                SnapTo(groundPos, serverTime);
                return;
            }

            _toPos = next;
            _hasFrom = true;
            PushSample(serverTime, next);
        }

        /// <summary>Append one stamped position. Two rules make a delta stream into a timeline:
        /// <list type="bullet">
        /// <item>A sample no newer than the last REPLACES it — the same tick twice, or two arrival-timed samples
        /// (an old server) landing in one frame.</item>
        /// <item>🔑 A GAP IS A STANDSTILL. The server sends only what changed, so an entity silent for a while
        /// was standing exactly where it was last sent until the tick before this sample. Without the held
        /// point the first step after a pause would be smeared across the whole pause, as a slow creep.</item>
        /// </list></summary>
        private void PushSample(double time, Vector3 pos)
        {
            // A clock that jumped BACK (a restarted server, or the first stamped delta landing on samples taken
            // on the local clock): the old timeline means nothing on the new one, so start over.
            if (_sCount > 0 && time < _sTime[_sCount - 1] - 1.0) _sCount = 0;
            if (_sCount > 0)
            {
                double last = _sTime[_sCount - 1];
                if (time <= last) { _sPos[_sCount - 1] = pos; return; }
                if (time - last > GameConstants.TickSeconds * 1.5f)
                    AppendSample(time - GameConstants.TickSeconds, _sPos[_sCount - 1]);
            }
            AppendSample(time, pos);
        }

        private void AppendSample(double time, Vector3 pos)
        {
            if (_sCount == MaxSamples) DropOldestSample();
            _sTime[_sCount] = time;
            _sPos[_sCount] = pos;
            _sCount++;
        }

        private void DropOldestSample()
        {
            Array.Copy(_sTime, 1, _sTime, 0, _sCount - 1);
            Array.Copy(_sPos, 1, _sPos, 0, _sCount - 1);
            _sCount--;
        }

        /// <summary>Where the server had this entity at <paramref name="time"/>: linear between the two samples
        /// around it, the oldest before the buffer, and the NEWEST once it runs dry — holding at the last thing
        /// we actually know rather than extrapolating somewhere the server never said. Samples wholly behind
        /// the render time are dropped as it passes them (it only moves forward).</summary>
        private Vector3 SampleAt(double time)
        {
            if (_sCount == 0) return transform.position;
            while (_sCount >= 2 && _sTime[1] <= time) DropOldestSample();
            if (_sCount == 1 || time <= _sTime[0]) return _sPos[0];
            float k = (float)((time - _sTime[0]) / (_sTime[1] - _sTime[0]));
            return Vector3.Lerp(_sPos[0], _sPos[1], k);
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
            _arrived = false;   // a new walk cancels any hold left by the last one's arrival
            // A fresh order: nothing the server sent before it can confirm or refute it.
            _serverConfirmed = false;
            _predictStartTime = _lastServerMoveTime = Time.time;
        }

        /// <summary>
        /// Follow the server's speed DURING the walk, not just at the tap (2026-10-01: *"he gets off the
        /// pavement but the move speed don't change visually ... he reaches the point then gets rubber
        /// back and walk the rest of the way"*). The speed was read once in <see cref="PredictTo"/>, so
        /// leaving the Paved Streets, a Run/Walk toggle, a slow landing mid-walk all left the prediction
        /// running at the old number — ahead of the server by more every second, under the reconcile
        /// distance on any normal walk, until it arrived and had to give it all back. Every server
        /// sample now carries the speed it is moving at (EntityLean.Speed); 0 = rooted/stunned, which no
        /// walk survives.
        /// </summary>
        public void SetPredictSpeed(float speed)
        {
            if (!_predicting) return;
            if (speed <= 0f) EndPrediction();
            else _predictSpeed = speed;
        }

        /// <summary>Has the server stopped answering our last walk order — never confirmed it in
        /// <see cref="UnconfirmedTimeout"/>, or confirmed it and then gone quiet for <see cref="StalledTimeout"/>?</summary>
        private bool ServerSilent() => _serverConfirmed
            ? Time.time - _lastServerMoveTime > StalledTimeout
            : Time.time - _predictStartTime > UnconfirmedTimeout;

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
            // Handing over is not arriving: a reconcile or a cancel drops any arrival hold, and the
            // ordinary decay resumes. The arrival path sets _arrived AFTER calling this, deliberately.
            _arrived = false;
        }

        /// <summary>Put the entity AT a position with no interpolation — for spawns, teleports and
        /// re-entry, where sliding in from wherever it used to be would be a lie.</summary>
        public void SnapTo(Vector3 groundPos) => SnapTo(groundPos, NetClock.Now);

        private void SnapTo(Vector3 groundPos, double serverTime)
        {
            var at = groundPos + Vector3.up * HalfHeight;
            _toPos = _predictPos = at;
            _hasFrom = true;
            _sCount = 0;                     // the old timeline is a path it never walked
            PushSample(serverTime, at);
            // A snap is the one case where the server is deliberately overriding us, so any prediction
            // and any accumulated error are wrong by definition — drop both rather than let them drag
            // the character back off the point we were just told to be at.
            _predicting = false;
            _arrived = false;
            _selfError = Vector3.zero;
            transform.position = at;
        }

        /// <summary>
        /// SNAPSHOT INTERPOLATION: draw where the entity WAS <see cref="NetClock.Delay"/> ago,
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

            // 🔑 EVERY ENTITY, SELF INCLUDED, IS DRAWN AT THE SAME SERVER MOMENT: NetClock.RenderTime, a fixed
            // delay behind the newest tick. Responsiveness for self comes from PREDICTION, not from rendering
            // self any sooner — a zero delay made the interpolation degenerate (a 10Hz staircase of hops), and a
            // delay measured from packet arrival was the "fps is 60 but it jitters" of 0.271.0 (see _sTime).
            var basePos = SampleAt(NetClock.RenderTime);

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
            // The server's SILENCE is an answer too (see _serverConfirmed): an order it never walked,
            // or a walk it stopped short, sends no samples for SetTarget to judge.
            if (_predicting && ServerSilent()) EndPrediction();

            if (_predicting)
            {
                float step = _predictSpeed * Time.deltaTime;
                var to = _predictTarget - _predictPos;

                if (to.sqrMagnitude <= step * step)
                {
                    _predictPos = _predictTarget;
                    EndPrediction();
                    // ARRIVED, as opposed to handed over. Hold here while the server catches up rather
                    // than leaving a forward-pointing error to be added to an advancing base — see the
                    // _arrived field for why that sum overshoots.
                    _arrived = true;
                    _arrivedTarget = _predictTarget;
                    _arrivedSince = Time.time;
                    _arrivedDist = float.MaxValue;
                }
                else
                {
                    _predictPos += to.normalized * step;
                }

                _selfError = _predictPos - basePos;
                transform.position = _predictPos;
                return;
            }

            if (_arrived)
            {
                float d = (_arrivedTarget - basePos).magnitude;
                // Keep holding only while the server is genuinely still closing on the same point. If it
                // stops closing it is taking us somewhere else, and standing still would be its own bug.
                // ...and only while it is still MOVING: a server that never walked this order, or
                // stopped short, sends nothing, so "not further away" alone held the character on a
                // point it was never going to reach for the whole hold.
                if (d > ArrivalSettled && d <= _arrivedDist + 0.001f
                    && Time.time - _arrivedSince < ArrivalHoldSeconds && !ServerSilent())
                {
                    _arrivedDist = d;
                    _selfError = _arrivedTarget - basePos;   // measured, not decayed
                    transform.position = _arrivedTarget;
                    return;
                }
                _arrived = false;
            }

            // Frame-rate independent decay. Using deltaTime as a blend factor directly is the bug that
            // started this whole chase — at 120ms a frame it overshoots past the target and back.
            _selfError *= Mathf.Exp(-ErrorDecayRate * Time.deltaTime);
            if (_selfError.sqrMagnitude < 0.000001f) _selfError = Vector3.zero;
            transform.position = basePos + _selfError;
        }

        private void LateUpdate()
        {
            // 🔑 A MODEL AND A BILLBOARD WANT OPPOSITE THINGS. The sphere turns to face the CAMERA
            // (that is what keeps a flat unlit marker reading as a clean circle from any angle); a
            // body has to face where it is WALKING. Doing both would spin every character to look at
            // the player whenever the camera moved, which is the single most obvious way this feature
            // could go wrong, so the two paths never mix.
            if (_model == null)
            {
                if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
                if (_cam != null) transform.rotation = _cam.rotation;   // billboard toward the camera
                return;
            }

            DriveModel();
        }

        /// <summary>
        /// Turn the model toward its motion and tell the animator what it is doing.
        ///
        /// <para>🔑 Everything here is read from the position we ALREADY DREW — not from the wire.
        /// Facing and walk speed cost no protocol field because the interpolator has both: it knows
        /// where the entity was last frame and where it is now, and the difference is the answer for
        /// a mob, a remote player and your own predicted walk alike, with no special case for any of
        /// them.</para>
        /// </summary>
        private void DriveModel()
        {
            var pos = transform.position;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            var delta = _hasLastDrawn ? pos - _lastDrawn : Vector3.zero;
            _lastDrawn = pos;
            _hasLastDrawn = true;

            delta.y = 0f;                       // walking is a ground-plane question
            float speed = delta.magnitude / dt;

            if (speed > FacingDeadZone)
            {
                float want = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                _facing = _hasFacing ? Mathf.MoveTowardsAngle(_facing, want, TurnSpeed * dt) : want;
                _hasFacing = true;
            }

            // The ROOT carries the facing, so the model prefab can be dropped in unrotated and the
            // halo (a sphere) does not care. The tap collider is a cube centred on the root, so
            // turning it changes nothing a finger can notice.
            transform.rotation = Quaternion.Euler(0f, _facing, 0f);

            if (_animator == null) return;
            // Every parameter is optional. A prefab whose controller only has "Speed" gets walking and
            // nothing else rather than a console full of "parameter does not exist" — which matters
            // when the art is arriving one clip at a time.
            // Damped (0.1s): the blend tree reads Speed every frame, and a raw per-frame value lets one short
            // frame flick the legs between idle and walk — which reads as the animation itself jittering.
            if (_hasSpeed) _animator.SetFloat("Speed", speed, 0.1f, dt);
            if (_actionLayer > 0) UpdateActionWeight();

            // Legs at the pace the ground actually moves. The blend tree picks WHICH cycle (walk or jog) but plays it at
            // its own cadence, so a slow runner pumped its legs like a sprinter while covering half the ground. The
            // locomotion state's speed is this multiplier: actual speed over what one cycle covers at this blend.
            if (_hasMoveMul)
            {
                float mul = 1f;
                float scale = _model.lossyScale.y;
                if (speed > 0.05f && _runNative > 0.01f && _walkNative > 0.01f && scale > 0.0001f)
                {
                    // The same thresholds as the blend tree (ModelSetup: walk 0.7, run 1.4): below walk the cycle
                    // fades out into idle, so its cadence stays the walk's own.
                    float t = Mathf.InverseLerp(WalkThreshold, RunThreshold, speed);
                    float native = Mathf.Lerp(_walkNative, _runNative, t) * scale;
                    if (speed >= WalkThreshold) mul = Mathf.Clamp(speed / native, 0.6f, 2f);
                }
                _animator.SetFloat("MoveMul", mul);
            }
        }

        private const float WalkThreshold = 0.7f, RunThreshold = 1.4f;

        /// <summary>
        /// `BL-93` — give this entity a body. Called once, right after the sphere is built, and only
        /// when <see cref="ModelLibrary"/> found a prefab for it.
        ///
        /// <para>The sphere is not destroyed, only un-drawn: its collider is the tap target, its
        /// transform is what the whole interpolator writes to, and the god halo hangs off it. What
        /// changes is which mesh the eye sees.</para>
        ///
        /// <para>⚠ The model is dropped at the root's BASE, not its centre. The interpolator parks the
        /// root half a marker above the ground so a sphere sits ON the floor rather than half inside
        /// it; a humanoid's origin is between its feet, so it needs that lift taken back off — in
        /// LOCAL units, which is why <see cref="RefreshModelOffset"/> exists and runs again whenever
        /// the entity-size slider moves.</para>
        /// </summary>
        public void AttachModel(GameObject model)
        {
            if (model == null) return;

            _model = model.transform;
            _model.SetParent(transform, false);
            _model.localRotation = Quaternion.identity;
            RefreshModelOffset();

            // A model prefab may well ship with colliders of its own (asset packs usually do). They
            // would sit in front of the deliberately generous box tap target and swallow taps on a
            // thin limb, so the root's collider stays the only one.
            foreach (var extra in model.GetComponentsInChildren<Collider>()) Destroy(extra);

            // Un-draw the sphere, keep everything else. The stealth fade (`BL-82`) reaches the model
            // through ApplyModelFade, which swaps its renderers onto FadeLit copies; the god halo is its
            // own child sphere and needs nothing.
            if (_renderer != null) _renderer.enabled = false;

            _animator = model.GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                // Every parameter is optional; the float ones double as a channel from ModelSetup, which bakes each
                // clip's length and each leg cycle's ground speed into the parameter's DEFAULT value.
                foreach (var p in _animator.parameters)
                {
                    switch (p.name)
                    {
                        case "Speed": _hasSpeed = true; break;
                        case "Dead": _hasDead = true; break;
                        case "SwingSpeed": _hasSwingSpeed = true; break;
                        case "MoveMul": _hasMoveMul = true; break;
                        case "LenMelee": _lenMelee = p.defaultFloat; break;
                        case "LenBow": _lenBow = p.defaultFloat; break;
                        case "LenSpell": _lenSpell = p.defaultFloat; break;
                        case "NativeWalk": _walkNative = p.defaultFloat; break;
                        case "NativeRun": _runNative = p.defaultFloat; break;
                    }
                }
                _actionLayer = Mathf.Max(0, _animator.GetLayerIndex("Action"));
                _restState = _actionLayer > 0 ? "Empty" : "Locomotion";
                _hasAttackState = _animator.HasState(_actionLayer, Animator.StringToHash("Attack"));
                _hasCastState = _animator.HasState(_actionLayer, Animator.StringToHash("Casting"));
                if (_hasDead) _animator.SetBool("Dead", _dead);

                // Off-screen entities do not need their skeletons evaluated. This is the cheapest
                // animation LOD there is and it belongs on by default, not behind the quality preset:
                // the server sends everything within ViewRange, which is a good deal more than the
                // camera is looking at.
                _animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
            ApplyModelFade();
        }

        /// <summary>Re-seat the model on the ground after the root's scale changes. See AttachModel.</summary>
        public void RefreshModelOffset()
        {
            if (_model == null) return;
            float scale = Mathf.Max(0.01f, transform.localScale.y);
            _model.localPosition = new Vector3(0f, -HalfHeight / scale, 0f);
        }

        /// <summary>This entity just landed (or threw) a blow — play the swing.
        ///
        /// <para>🔑 Driven by <c>CombatEvent</c>, which the server already sends to everyone nearby, so
        /// a full attack animation costs NO new message. The blow is resolved before the swing plays,
        /// which is backwards from how a real arm works and completely invisible at a ~150ms offset —
        /// and it buys the guarantee that nothing ever animates an attack that did not happen.</para></summary>
        ///
        /// <para>🔑 <b>Restarted from frame 0 on EVERY blow, never queued.</b> It used to be a trigger: a trigger set while
        /// the previous swing was still playing waited for it to end and then fired late, so a fast bow showed every
        /// other shot and the queue kept the body swinging after the fight. <paramref name="swing"/> is the server's
        /// <c>CombatEvent.Swing</c> — the time until the next blow — and the clip is sped up to finish inside it (never
        /// slowed: a slow weapon keeps its natural motion).</para></summary>
        public void PlayAttack(float swing)
        {
            if (_animator == null || !_hasAttackState || _dead || swing <= 0f) return;
            int style = HeldWeapons.StyleFor(_heldWeapon, _heldMagic);
            string state = style == 1 ? "AttackBow" : style == 2 ? "AttackSpell" : "Attack";
            if (!_animator.HasState(_actionLayer, Animator.StringToHash(state))) state = "Attack";
            float len = style == 1 ? _lenBow : style == 2 ? _lenSpell : _lenMelee;
            if (_hasSwingSpeed)
                _animator.SetFloat("SwingSpeed", len > 0.01f ? Mathf.Clamp(len * 0.95f / swing, 1f, 4f) : 1f);
            _casting = false;
            if (_actionLayer > 0) _animator.SetLayerWeight(_actionLayer, 1f);   // on now, not after the state updates
            _animator.Play(state, _actionLayer, 0f);
        }

        /// <summary>The upper-body "Action" layer is ON only while it plays something. 🔑 Its "Empty" rest state does
        /// NOT leave the base layer alone on a HUMANOID rig, write-defaults off or not: a weight-1 override writes the
        /// masked muscles, and with no clip they sit at zero — the arms held out in front ("hugging, like a lego
        /// character", owner 2026-10-13), and no arm swing while running. So the weight follows the layer: 0 at rest,
        /// 1 in a swing or a cast, and across a crossfade into or out of rest it follows the fade.</summary>
        private void UpdateActionWeight()
        {
            int empty = Animator.StringToHash("Empty");
            bool fromRest = _animator.GetCurrentAnimatorStateInfo(_actionLayer).shortNameHash == empty;
            float w;
            if (_animator.IsInTransition(_actionLayer))
            {
                float t = Mathf.Clamp01(_animator.GetAnimatorTransitionInfo(_actionLayer).normalizedTime);
                bool toRest = _animator.GetNextAnimatorStateInfo(_actionLayer).shortNameHash == empty;
                w = toRest ? 1f - t : fromRest ? t : 1f;
            }
            else w = fromRest ? 0f : 1f;
            _animator.SetLayerWeight(_actionLayer, w);
        }

        /// <summary>What this body holds — the FX layer reads it to give a bow's basic shot its arrow.</summary>
        public WeaponType HeldWeapon => _heldWeapon;

        /// <summary>`BL-103` — put this weapon set in the body's hands (see <see cref="HeldWeapons"/>), and pick the
        /// swing the next "Attack" plays: a bow shot, a spell thrust for a wand or staff, the sword swing for the rest.
        /// Called on every spawn DTO; only a CHANGE rebuilds anything. A body with no hands (a sphere, a rat) draws
        /// nothing and that is not an error.</summary>
        public void SetWeapons(WeaponType weapon, bool magic, bool shield)
        {
            if (_heldDressed && weapon == _heldWeapon && magic == _heldMagic && shield == _heldShield) return;
            _heldWeapon = weapon; _heldMagic = magic; _heldShield = shield; _heldDressed = true;
            if (_animator == null) return;
            HeldWeapons.Dress(_animator, _model, weapon, magic, shield, _held);
            ApplyModelFade();   // a weapon put in a faded hand fades with it
            if (_far) ApplyFar();   // …and one put in a hidden hand stays hidden
        }

        private bool _armorDressed, _armored;

        /// <summary>Underwear or the outfit (owner, 2026-10-14) — see <see cref="CharacterLook.Dress"/>. Called on every
        /// spawn DTO; only a change touches the model.</summary>
        public void SetArmor(bool armored)
        {
            if (_armorDressed && armored == _armored) return;
            _armored = armored; _armorDressed = true;
            if (_model == null) return;
            CharacterLook.Dress(_model.gameObject, armored);
            ApplyModelFade();   // the pieces just switched on join a running stealth fade
            if (_far) ApplyFar();
        }

        private bool _far;

        /// <summary>Past the player's view distance, inside the thick fog (DayNight.DrawDistance): drawn by nothing.
        /// RENDERERS off, never the GameObject — deactivating it would reset the animator and stand a corpse back up.</summary>
        public bool Far => _far;

        public void SetFar(bool far)
        {
            if (far == _far) return;
            _far = far;
            ApplyFar();
        }

        private void ApplyFar()
        {
            if (_model == null)
            {
                if (_renderer != null) _renderer.enabled = !_far;
                return;
            }
            foreach (var r in _model.GetComponentsInChildren<Renderer>(true)) r.enabled = !_far;
        }

        /// <summary>Show/hide the casting pose. Driven by <c>MobCastInfo</c> and the self cast bar —
        /// again, both already on the wire.
        ///
        /// <para>Played from code like the swing, not through a bool transition: the old transition left only from
        /// locomotion, so a cast that began while a queued swing was still playing never showed its pose at all.</para></summary>
        public void SetCasting(bool casting)
        {
            if (_animator == null || !_hasCastState || _dead || casting == _casting) return;
            _casting = casting;
            if (casting) _animator.CrossFadeInFixedTime("Casting", 0.1f, _actionLayer);
            else if (_animator.GetCurrentAnimatorStateInfo(_actionLayer).IsName("Casting"))
                _animator.CrossFadeInFixedTime(_restState, 0.15f, _actionLayer);
        }
    }

    /// <summary>
    /// 🔑 THE SERVER'S CLOCK, as this client can see it (0.271.0). Every <see cref="SnapshotDelta"/> carries the
    /// tick it describes; this turns that into seconds and tracks how far the server runs ahead of
    /// <c>Time.timeAsDouble</c>, so <see cref="EntityView"/> can draw every entity at one server moment.
    ///
    /// <para>The offset follows the FASTEST packets: a sample that says the server is further ahead than we
    /// thought is believed quickly (that packet simply had a clean path), one that says it is behind is
    /// believed slowly (it was most likely delayed). So a burst of late packets moves nothing, and the slow
    /// half still absorbs real drift between the server's 10Hz timer and this device's clock.</para>
    /// </summary>
    public static class NetClock
    {
        /// <summary>How far behind the newest tick we draw. One tick (100ms) to have a sample on both sides,
        /// plus 100ms for a packet to arrive late without the buffer running dry. Too small and a late packet
        /// is a visible stall again; every millisecond more is a millisecond the world lags your taps.</summary>
        public const double Delay = 0.2;

        private static long _lastTick;
        private static double _offset;
        private static bool _has;

        /// <summary>The server's time now, in seconds (tick × 0.1 — a double holds that to the microsecond for
        /// centuries of uptime). Before any stamped delta (an older server) it is the local clock, so
        /// arrival-time playback still works.</summary>
        public static double Now => Time.timeAsDouble + (_has ? _offset : 0.0);

        /// <summary>The moment every entity is drawn at.</summary>
        public static double RenderTime => Now - Delay;

        /// <summary>A delta arrived stamped <paramref name="tick"/>: learn from it, and return the server time
        /// its positions belong to. 0 = an unstamped (older) server: answered with <see cref="Now"/>.</summary>
        public static double Observe(long tick)
        {
            if (tick <= 0) return Now;
            // A tick going BACKWARDS is a restarted server: learn the offset afresh (EntityView.PushSample
            // drops a timeline that jumps back the same way).
            if (tick < _lastTick) _has = false;
            _lastTick = tick;

            double server = tick * (double)GameConstants.TickSeconds;
            double sample = server - Time.timeAsDouble;
            if (!_has) { _offset = sample; _has = true; }
            else _offset += (sample - _offset) * (sample > _offset ? 0.5 : 0.01);
            return server;
        }

        /// <summary>Forget the server (logout / a new connection): the next delta defines the clock afresh.</summary>
        public static void Reset() { _lastTick = 0; _has = false; }
    }
}
