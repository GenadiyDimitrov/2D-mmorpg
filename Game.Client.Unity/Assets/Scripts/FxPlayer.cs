using System;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// `BL-344` step 6 — the skill effects. Every skill gets a look with nothing authored: the DELIVERY (how it
    /// travels) is <see cref="SkillFxRules.Delivery"/> off the skill's shape, the LANDING (what plays on the target)
    /// is read off the outcome the server already reports. See docs/design/VisualPass.md §0.5.
    ///
    /// <para>🔑 <b>Cheap on a phone by construction.</b> Three particle systems for the whole world — soft dots,
    /// sparks, streaks — each one draw call, fed with <see cref="ParticleSystem.Emit(ParticleSystem.EmitParams,int)"/>.
    /// A cast creates no GameObject and allocates nothing: the emit parameters are one reused struct and the
    /// projectiles in flight live in a fixed array. A crowd fight just fills the particle cap, it never stalls.</para>
    ///
    /// <para>⚠ The default camera looks straight DOWN, where a purely vertical rise is a dot that never moves. So
    /// every landing also spreads outward on the ground plane — the ring, the glow and the pillar all read from
    /// above, and the rise is a bonus at a tilted camera.</para>
    /// </summary>
    public class FxPlayer : MonoBehaviour
    {
        public EntityManager Entities;

        // Heights over the feet. The entity root sits HalfHeight (0.75) above the ground; the humanoid is ~1.6 tall.
        private const float RootLift = 0.75f, ChestHeight = 0.9f;

        private static readonly Color MagicTint = new Color(0.65f, 0.45f, 1f);
        private static readonly Color PhysicalTint = new Color(1f, 0.78f, 0.45f);
        private static readonly Color ArrowTint = new Color(1f, 0.95f, 0.75f);
        private static readonly Color HealTint = new Color(0.4f, 1f, 0.5f);
        private static readonly Color ManaTint = new Color(0.35f, 0.6f, 1f);
        private static readonly Color BuffTint = new Color(1f, 0.85f, 0.35f);
        private static readonly Color DebuffTint = new Color(0.6f, 0.25f, 0.85f);
        private static readonly Color FailTint = new Color(0.55f, 0.5f, 0.6f);
        private static readonly Color PillarTint = new Color(1f, 0.93f, 0.6f);

        private enum Landing : byte { None, Impact, ImpactCrit, ImpactDim, Heal, Mana, Aura, Debuff }

        private struct Flight
        {
            public bool Live;
            public Guid Target;
            public Vector3 From, To;
            public float Start, Arrive, NextTrail;
            public Landing Landing;
            public Color Tint;
            public bool Arrow;
            public float Size;    // the orb and its trail, ×; 1 = a spell's
        }

        /// <summary>A bow's BASIC shot: the spell projectile's shape, thinner (owner, 2026-10-13: "like the magic_arrow
        /// just a bit thinner").</summary>
        private const float BasicArrowSize = 0.45f;

        private const int MaxFlights = 64;
        private readonly Flight[] _flights = new Flight[MaxFlights];

        private ParticleSystem _dots, _sparks, _streaks;
        private ParticleSystem.EmitParams _p;
        private Material _material;

        private float _now;
        private bool _ready;

        private void Awake() => Init();

        /// <summary>Builds the three systems. Awake calls it; the editor preview calls it too, because an edit-mode
        /// AddComponent never runs Awake.</summary>
        public void Init()
        {
            if (_ready) return;
            _ready = true;
            var shader = Resources.Load<Shader>("Shaders/FxAdditive") ?? Shader.Find("L2Clone/FxAdditive");
            if (shader == null)
            {
                Debug.LogError("[FxPlayer] FxAdditive shader missing from this build — skill effects are off.");
                enabled = false;
                return;
            }
            _material = new Material(shader) { name = "FxAdditive" };
            _dots = Make("FxDots", ParticleSystemRenderMode.Billboard, 1500, drag: 0f);
            _sparks = Make("FxSparks", ParticleSystemRenderMode.Stretch, 800, drag: 5f);
            _streaks = Make("FxStreaks", ParticleSystemRenderMode.Stretch, 300, drag: 0f);
            _p = new ParticleSystem.EmitParams { applyShapeToPosition = false };
        }

        private ParticleSystem Make(string name, ParticleSystemRenderMode mode, int max, float drag)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.duration = 1f;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;

            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;

            // Everything fades out over its life and shrinks a little: one curve for every effect, so an emit only
            // has to say where, how fast, how big, how long and what colour.
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = new ParticleSystem.MinMaxGradient(g);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.4f));

            if (drag > 0f)
            {
                var limit = ps.limitVelocityOverLifetime;
                limit.enabled = true;
                limit.drag = drag;
                limit.multiplyDragByParticleSize = false;
                limit.multiplyDragByParticleVelocity = false;
            }

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = _material;
            r.renderMode = mode;
            if (mode == ParticleSystemRenderMode.Stretch) { r.velocityScale = 0.05f; r.lengthScale = 1.5f; }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortMode = ParticleSystemSortMode.None;   // additive: order does not matter
            ps.Play();
            return ps;
        }

        // ----- Entry points -------------------------------------------------------------------------

        /// <summary>One resolved combat action, as every nearby client receives it.</summary>
        public void Play(CombatEvent e)
        {
            if (!enabled || e == null || e.Outcome == CombatOutcome.Death || Entities == null) return;
            var target = Entities.Find(e.TargetId);
            if (target == null) return;

            if (string.IsNullOrEmpty(e.SkillId))
            {
                // A bow's basic attack is a SHOT: every swing (Swing > 0, so never a tick) flies an arrow, hit or miss.
                // The crit still sparks — on arrival, where the arrow lands.
                if (e.Swing > 0f && e.AttackerId != e.TargetId)
                {
                    var archer = Entities.Find(e.AttackerId);
                    if (archer != null && archer.HeldWeapon == WeaponType.Bow)
                    {
                        Launch(e.TargetId, Chest(archer), Chest(target),
                               e.Outcome == CombatOutcome.Crit ? Landing.Impact : Landing.None, ArrowTint, false,
                               BasicArrowSize);
                        return;
                    }
                }
                // A plain basic attack: clean, except that a crit sparks (the owner's pick, 2026-10-12). Ticks,
                // procs and traps carry a Skill label and no id, so they never land here as a crit spark.
                if (e.Skill == null && e.Outcome == CombatOutcome.Crit)
                    Impact(Chest(target), PhysicalTint, 0.6f);
                return;
            }

            var def = SkillCatalog.Get(e.SkillId);
            if (def == null) return;
            var delivery = SkillFxRules.Delivery(def);
            if (delivery == SkillFx.None) return;

            var landing = LandingOf(e, def);
            var attacker = e.AttackerId == e.TargetId ? null : Entities.Find(e.AttackerId);
            var tint = TintOf(def, landing);

            if (attacker == null || delivery == SkillFx.Instant)
            {
                Land(target, landing);
                return;
            }

            var from = Chest(attacker);
            var to = Chest(target);
            switch (delivery)
            {
                case SkillFx.Projectile:
                case SkillFx.Arrow:
                    Launch(e.TargetId, from, to, landing, tint, delivery == SkillFx.Arrow);
                    break;
                case SkillFx.Beam:
                    Beam(from, to, tint);
                    Land(target, landing);
                    break;
                case SkillFx.Slash:
                    Slash(from, to, tint);
                    Land(target, landing);
                    break;
                default:
                    Land(target, landing);
                    break;
            }
        }

        /// <summary>A corpse came back to life where it lay (a resurrection, not a respawn in town).</summary>
        public void Pillar(EntityView view)
        {
            if (!enabled || view == null) return;
            PillarAt(Feet(view));
        }

        private void PillarAt(Vector3 feet)
        {
            for (int i = 0; i < 50; i++)
            {
                Vector2 c = UnityEngine.Random.insideUnitCircle * 0.45f;
                Dot(_dots, feet + new Vector3(c.x, UnityEngine.Random.Range(0f, 0.4f), c.y),
                    new Vector3(c.x * 0.6f, UnityEngine.Random.Range(2.5f, 5f), c.y * 0.6f),
                    UnityEngine.Random.Range(0.15f, 0.3f), UnityEngine.Random.Range(1f, 1.5f), PillarTint);
            }
            Ring(feet + Vector3.up * 0.05f, 28, 0.3f, outward: 1.4f, up: 0.2f, size: 0.22f, life: 1f, PillarTint);
            Dot(_dots, feet + Vector3.up * 0.6f, Vector3.zero, 2.2f, 0.5f, PillarTint * 0.8f);
        }

        // ----- The landings -------------------------------------------------------------------------

        private static Landing LandingOf(CombatEvent e, SkillDef def)
        {
            switch (e.Outcome)
            {
                case CombatOutcome.Hit:
                case CombatOutcome.Double:
                case CombatOutcome.Block: return Landing.Impact;
                case CombatOutcome.Crit: return Landing.ImpactCrit;
                case CombatOutcome.Fail: return e.Damage > 0 ? Landing.ImpactDim : Landing.None;   // 0 = resisted
                case CombatOutcome.Heal: return Landing.Heal;
                case CombatOutcome.ManaHeal: return Landing.Mana;
                case CombatOutcome.Buff: return SkillFxRules.IsHostile(def) ? Landing.Debuff : Landing.Aura;
                default: return Landing.None;   // Miss: the shot flies, nothing lands
            }
        }

        private static Color TintOf(SkillDef def, Landing landing)
        {
            switch (landing)
            {
                case Landing.Heal: return HealTint;
                case Landing.Mana: return ManaTint;
                case Landing.Aura: return BuffTint;
                case Landing.Debuff: return DebuffTint;
            }
            if (def.RequiredWeapon == WeaponType.Bow) return ArrowTint;
            switch (def.Category)
            {
                case SkillCategory.Heal: return HealTint;
                case SkillCategory.Buff: return BuffTint;
                case SkillCategory.Debuff: return DebuffTint;
                case SkillCategory.Magic: return MagicTint;
                default: return PhysicalTint;
            }
        }

        private void Land(EntityView target, Landing landing) => LandAt(Feet(target), landing);

        private void LandAt(Vector3 feet, Landing landing)
        {
            var chest = feet + Vector3.up * ChestHeight;
            switch (landing)
            {
                case Landing.Impact: Impact(chest, PhysicalTint, 1f); break;
                case Landing.ImpactCrit: Impact(chest, new Color(1f, 0.55f, 0.25f), 2f); break;
                case Landing.ImpactDim: Impact(chest, FailTint, 0.5f); break;
                case Landing.Heal: Glow(feet, HealTint); break;
                case Landing.Mana: Glow(feet, ManaTint); break;
                case Landing.Aura: Ring(feet + Vector3.up * 0.05f, 20, 0.45f, outward: 0.5f, up: 1.3f, size: 0.24f, life: 0.85f, BuffTint); break;
                case Landing.Debuff: Motes(feet); break;
            }
        }

        /// <summary>A burst of sparks and one flash. <paramref name="scale"/> 2 = a crit.</summary>
        private void Impact(Vector3 at, Color tint, float scale)
        {
            int n = Mathf.RoundToInt(10 * scale);
            for (int i = 0; i < n; i++)
            {
                var dir = UnityEngine.Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 0.6f;
                Dot(_sparks, at, dir * UnityEngine.Random.Range(2.5f, 4.5f) * Mathf.Sqrt(scale),
                    UnityEngine.Random.Range(0.05f, 0.09f), UnityEngine.Random.Range(0.2f, 0.35f), tint);
            }
            Dot(_dots, at, Vector3.zero, 0.55f * scale, 0.15f, tint);
        }

        /// <summary>Heal and MP: motes rising from a widening disc, and a soft flash.</summary>
        private void Glow(Vector3 feet, Color tint)
        {
            for (int i = 0; i < 22; i++)
            {
                Vector2 c = UnityEngine.Random.insideUnitCircle * 0.45f;
                Dot(_dots, feet + new Vector3(c.x, UnityEngine.Random.Range(0.1f, 1.2f), c.y),
                    new Vector3(c.x * 0.9f, UnityEngine.Random.Range(0.6f, 1.4f), c.y * 0.9f),
                    UnityEngine.Random.Range(0.18f, 0.3f), UnityEngine.Random.Range(0.8f, 1.2f), tint);
            }
            Dot(_dots, feet + Vector3.up * ChestHeight, Vector3.zero, 1.3f, 0.35f, tint * 0.8f);
        }

        /// <summary>A debuff: purple motes spiralling DOWN onto the target and settling outward.</summary>
        private void Motes(Vector3 feet)
        {
            for (int i = 0; i < 14; i++)
            {
                float a = i * (Mathf.PI * 2f / 14f);
                var radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var tangent = new Vector3(-radial.z, 0f, radial.x);
                Dot(_dots, feet + radial * 0.3f + Vector3.up * UnityEngine.Random.Range(1.5f, 2f),
                    tangent * 0.7f + radial * 0.35f + Vector3.down * UnityEngine.Random.Range(0.9f, 1.3f),
                    UnityEngine.Random.Range(0.2f, 0.3f), UnityEngine.Random.Range(1f, 1.3f), DebuffTint);
            }
        }

        private void Ring(Vector3 centre, int count, float radius, float outward, float up, float size, float life, Color tint)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i * (Mathf.PI * 2f / count);
                var radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Dot(_dots, centre + radial * radius,
                    radial * outward + Vector3.up * up * UnityEngine.Random.Range(0.8f, 1.2f),
                    size, life * UnityEngine.Random.Range(0.85f, 1.1f), tint);
            }
        }

        // ----- The deliveries -----------------------------------------------------------------------

        private void Beam(Vector3 from, Vector3 to, Color tint)
        {
            var d = to - from;
            int n = Mathf.Clamp(Mathf.RoundToInt(d.magnitude / 0.2f), 4, 48);
            for (int i = 0; i <= n; i++)
                Dot(_dots, from + d * (i / (float)n), UnityEngine.Random.insideUnitSphere * 0.15f,
                    0.22f, UnityEngine.Random.Range(0.3f, 0.4f), tint);
        }

        /// <summary>A short arc of streaks swept across the front of the attacker, aimed at the target.</summary>
        private void Slash(Vector3 from, Vector3 to, Color tint)
        {
            var fwd = to - from;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            fwd.Normalize();
            bool left = UnityEngine.Random.value < 0.5f;   // alternate the sweep so a combo does not look stamped
            for (int i = 0; i < 9; i++)
            {
                float deg = Mathf.Lerp(-70f, 70f, i / 8f) * (left ? 1f : -1f);
                var dir = Quaternion.AngleAxis(deg, Vector3.up) * fwd;
                var tangent = Quaternion.AngleAxis(left ? 90f : -90f, Vector3.up) * dir;
                Dot(_streaks, from + dir * 0.55f + Vector3.up * (i / 8f - 0.5f) * 0.3f, tangent * 7f,
                    0.08f, 0.14f + i * 0.01f, tint);
            }
        }

        private void Launch(Guid target, Vector3 from, Vector3 to, Landing landing, Color tint, bool arrow, float size = 1f)
        {
            float dist = (to - from).magnitude;
            float flight = Mathf.Clamp(dist / (arrow ? 30f : 20f), 0.1f, 0.4f);
            for (int i = 0; i < MaxFlights; i++)
            {
                if (_flights[i].Live) continue;
                _flights[i] = new Flight
                {
                    Live = true, Target = target, From = from, To = to, Start = _now, Arrive = _now + flight,
                    NextTrail = _now, Landing = landing, Tint = tint, Arrow = arrow, Size = size,
                };
                var v = (to - from) / flight;
                if (arrow) Dot(_streaks, from, v, 0.07f, flight, tint);
                else Dot(_dots, from, v, 0.6f * size, flight, tint);
                return;
            }
            // Every slot busy (a huge fight): skip the flight, still show what landed.
            LandAt(to - Vector3.up * ChestHeight, landing);
        }

        private void Update() => Step(Time.deltaTime);

        /// <summary>Advances the projectiles in flight. Its own clock rather than Time.time, so the editor preview
        /// can drive it frame by frame.</summary>
        private void Step(float dt)
        {
            _now += dt;
            float now = _now;
            for (int i = 0; i < MaxFlights; i++)
            {
                ref var f = ref _flights[i];
                if (!f.Live) continue;
                if (now >= f.Arrive)
                {
                    f.Live = false;
                    // Land where the target IS now, not where it stood at launch — a step aside should not
                    // leave the burst hanging in the air beside it.
                    var view = Entities != null ? Entities.Find(f.Target) : null;
                    LandAt(view != null ? Feet(view) : f.To - Vector3.up * ChestHeight, f.Landing);
                    continue;
                }
                if (!f.Arrow && now >= f.NextTrail)
                {
                    f.NextTrail = now + 0.02f;
                    float t = (now - f.Start) / (f.Arrive - f.Start);
                    Dot(_dots, Vector3.Lerp(f.From, f.To, t), UnityEngine.Random.insideUnitSphere * 0.2f * f.Size,
                        0.28f * f.Size, 0.25f, f.Tint * 0.7f);
                }
            }
        }

        // ----- Editor preview (ModelSetup.PreviewFx) --------------------------------------------------

        /// <summary>Clears every particle and flight, so each preview frame shows one effect alone.</summary>
        public void PreviewClear()
        {
            for (int i = 0; i < MaxFlights; i++) _flights[i].Live = false;
            // ⚠ NOT Clear(): on a system Simulate has paused, an Emit right after Clear() is silently dropped (the
            // first preview drew only the effects emitted mid-simulation). Running them past every lifetime empties
            // them through the same path that works.
            _dots.Simulate(3f, true, false, false);
            _sparks.Simulate(3f, true, false, false);
            _streaks.Simulate(3f, true, false, false);
        }

        /// <summary>Fires one archetype between two FEET positions, as <see cref="Play"/> would for a real event.</summary>
        public void PreviewShot(string what, Vector3 fromFeet, Vector3 toFeet)
        {
            var from = fromFeet + Vector3.up * ChestHeight;
            var to = toFeet + Vector3.up * ChestHeight;
            switch (what)
            {
                case "projectile": Launch(Guid.Empty, from, to, Landing.Impact, MagicTint, false); break;
                case "arrow": Launch(Guid.Empty, from, to, Landing.Impact, ArrowTint, true); break;
                case "basicarrow": Launch(Guid.Empty, from, to, Landing.None, ArrowTint, false, BasicArrowSize); break;
                case "beam": Beam(from, to, HealTint); LandAt(toFeet, Landing.Heal); break;
                case "slash": Slash(from, to, PhysicalTint); LandAt(toFeet, Landing.Impact); break;
                case "crit": LandAt(toFeet, Landing.ImpactCrit); break;
                case "heal": LandAt(toFeet, Landing.Heal); break;
                case "mana": LandAt(toFeet, Landing.Mana); break;
                case "aura": LandAt(toFeet, Landing.Aura); break;
                case "debuff": LandAt(toFeet, Landing.Debuff); break;
                case "pillar": PillarAt(toFeet); break;
            }
        }

        /// <summary>Live particles per system, for the preview log.</summary>
        public string PreviewCounts =>
            $"dots {_dots.particleCount} (playing {_dots.isPlaying}), sparks {_sparks.particleCount}, streaks {_streaks.particleCount}";

        /// <summary>Moves the flights AND the particles on by <paramref name="dt"/>; edit mode simulates nothing itself.</summary>
        public void PreviewAdvance(float dt)
        {
            Step(dt);
            _dots.Simulate(dt, true, false, false);
            _sparks.Simulate(dt, true, false, false);
            _streaks.Simulate(dt, true, false, false);
        }

        // ----- Helpers ------------------------------------------------------------------------------

        private void Dot(ParticleSystem ps, Vector3 pos, Vector3 vel, float size, float life, Color tint)
        {
            _p.position = pos;
            _p.velocity = vel;
            _p.startSize = size;
            _p.startLifetime = life;
            _p.startColor = tint;
            ps.Emit(_p, 1);
        }

        private static Vector3 Feet(EntityView v) => v.transform.position - Vector3.up * RootLift;
        private static Vector3 Chest(EntityView v) => Feet(v) + Vector3.up * ChestHeight;
    }
}
