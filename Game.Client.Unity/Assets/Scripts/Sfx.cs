using System;
using System.Collections.Generic;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// `BL-45` — the game's sound effects. Client-only: every cue is read off a message the client already gets
    /// (the combat event, the progress push, the gold push, a loot line) — the server knows nothing about sound.
    ///
    /// <para>🔑 <b>A CUE IS A FOLDER.</b> <c>Resources/Sounds/&lt;cue&gt;/</c> holds the clips, and every clip in it
    /// is a random variant of that cue. Changing a sound = dropping a file in (or out of) a folder; no code, no
    /// table. An empty or missing folder is silence, never an error. Where each clip came from:
    /// <c>docs/CREDITS.md</c>.</para>
    ///
    /// <para>🔑 <b>2D, PANNED BY HAND.</b> The camera hangs 20+ units over the player, so Unity's 3D rolloff, measured
    /// from the listener on the camera, would make a fight at your feet as quiet as one across the clearing. Every
    /// voice is 2D instead, its volume falling off with the distance from YOUR character and its pan read off the
    /// camera's right vector. A fight you are in is always at full volume.</para>
    ///
    /// <para>Cheap on a phone: a fixed pool of voices, and a cue that just played will not play again for a few
    /// frames, so a crowd fight is a texture rather than a wall.</para>
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public GameBoot Boot;

        /// <summary>The player's volume, 0..1 (Settings → Sound volume).</summary>
        public static float Volume = 0.8f;
        public const string VolumePref = "snd.volume";

        public static void LoadPrefs() => Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePref, 0.8f));

        /// <summary>The one instance, for the UI's button click. Null before the world boots, which is silence.</summary>
        public static Sfx Instance { get; private set; }

        // Beyond Silent nothing is heard; inside Full it is full volume. Unity units (the humanoid is ~1.6 tall).
        private const float Full = 6f, Silent = 40f;
        private const int Voices = 16;
        private const float Repeat = 0.05f;   // the same cue, again within this, is dropped

        /// <summary>Each cue's loudness relative to the others — the one knob a folder cannot carry.</summary>
        private static readonly Dictionary<string, float> Gain = new Dictionary<string, float>
        {
            { "hit_blade", 0.7f }, { "hit_blunt", 0.7f }, { "crit", 0.8f }, { "block", 0.6f }, { "miss", 0.35f },
            { "bow_shot", 0.6f }, { "arrow_hit", 0.6f },
            { "spell_cast", 0.5f }, { "spell_hit", 0.6f }, { "spell_fail", 0.4f },
            { "heal", 0.45f }, { "mana", 0.45f }, { "buff", 0.4f }, { "debuff", 0.5f },
            { "death", 0.7f }, { "death_self", 0.8f }, { "levelup", 0.8f },
            { "loot_gold", 0.6f }, { "loot_item", 0.6f },
            { "ui_click", 0.3f },
            { "step_grass", 0.25f }, { "step_stone", 0.25f },
        };

        private readonly Dictionary<string, AudioClip[]> _clips = new Dictionary<string, AudioClip[]>();
        private readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        private AudioSource[] _voices;
        private int _next;

        /// <summary>A sound waiting for its projectile to arrive.</summary>
        private struct Pending { public float At; public string Cue; public Guid Target; public float Gain; }
        private readonly List<Pending> _pending = new List<Pending>();

        private Vector3 _lastSelf;
        private bool _hasLastSelf;
        private float _nextStep, _stepSpeed;

        private void Awake()
        {
            Instance = this;
            LoadPrefs();
            _voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _voices[i] = src;
            }
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        // ----- The cues ----------------------------------------------------------------------------------

        /// <summary>One combat event, for any fight in earshot (the server only sends the ones nearby).</summary>
        public void Play(CombatEvent e)
        {
            if (e == null || Boot == null || Boot.Entities == null) return;
            bool self = e.TargetId == Boot.SelfId;

            if (e.Outcome == CombatOutcome.Death)
            {
                if (self) Ui("death_self");
                else At(e.TargetId, "death");
                return;
            }

            var target = Boot.Entities.Find(e.TargetId);
            if (target == null) return;
            var attacker = e.AttackerId == e.TargetId ? null : Boot.Entities.Find(e.AttackerId);

            if (string.IsNullOrEmpty(e.SkillId))
            {
                // A tick, a reflect or a proc carries a label and no swing: the floating number is enough.
                if (e.Swing <= 0f) return;
                bool bow = attacker != null && attacker.HeldWeapon == WeaponType.Bow;
                if (bow)
                {
                    At(e.AttackerId, "bow_shot");
                    Arrive(attacker, target, true, e.Outcome == CombatOutcome.Miss ? null : "arrow_hit", e);
                    return;
                }
                Strike(e, attacker);
                return;
            }

            var def = SkillCatalog.Get(e.SkillId);
            if (def == null) return;
            var delivery = SkillFxRules.Delivery(def);

            switch (e.Outcome)
            {
                case CombatOutcome.Heal: At(e.TargetId, "heal"); return;
                case CombatOutcome.ManaHeal: At(e.TargetId, "mana"); return;
                case CombatOutcome.Buff: At(e.TargetId, SkillFxRules.IsHostile(def) ? "debuff" : "buff"); return;
            }

            if (def.RequiredWeapon == WeaponType.Bow || delivery == SkillFx.Arrow)
            {
                if (attacker != null) At(e.AttackerId, "bow_shot");
                Arrive(attacker, target, true, e.Outcome == CombatOutcome.Miss ? null : "arrow_hit", e);
                return;
            }
            if (def.Category == SkillCategory.Magic || delivery == SkillFx.Projectile)
            {
                if (attacker != null) At(e.AttackerId, "spell_cast");
                string land = e.Outcome == CombatOutcome.Fail ? "spell_fail" : "spell_hit";
                Arrive(attacker, target, false, land, e);
                return;
            }
            Strike(e, attacker);
        }

        /// <summary>A melee blow, basic or skill: the weapon decides the sound, the outcome adds to it.</summary>
        private void Strike(CombatEvent e, EntityView attacker)
        {
            switch (e.Outcome)
            {
                case CombatOutcome.Miss: At(e.TargetId, "miss"); return;
                case CombatOutcome.Block: At(e.TargetId, "block"); return;
            }
            var weapon = attacker != null ? attacker.HeldWeapon : WeaponType.None;
            bool blade = (weapon & (WeaponType.AnySword | WeaponType.Dual)) != 0;
            At(e.TargetId, blade ? "hit_blade" : "hit_blunt");
            if (e.Outcome == CombatOutcome.Crit || e.Outcome == CombatOutcome.Double) At(e.TargetId, "crit");
        }

        /// <summary>The landing sound, held back for as long as <see cref="FxPlayer"/>'s projectile flies (the same
        /// formula), so the impact is heard when the orb arrives rather than when it leaves.</summary>
        private void Arrive(EntityView from, EntityView to, bool arrow, string cue, CombatEvent e)
        {
            if (cue == null) return;
            if (e.Outcome == CombatOutcome.Crit) Queue(from, to, arrow, "crit", e.TargetId);
            Queue(from, to, arrow, cue, e.TargetId);
        }

        private void Queue(EntityView from, EntityView to, bool arrow, string cue, Guid target)
        {
            float flight = 0f;
            if (from != null)
            {
                float dist = (to.transform.position - from.transform.position).magnitude;
                flight = Mathf.Clamp(dist / (arrow ? 30f : 20f), 0.1f, 0.4f);
            }
            _pending.Add(new Pending { At = Time.time + flight, Cue = cue, Target = target, Gain = 1f });
        }

        public void LevelUp() => Ui("levelup");
        public void Gold() => Ui("loot_gold");
        public void Loot() => Ui("loot_item");
        public void Click() => Ui("ui_click");

        // ----- Playing ------------------------------------------------------------------------------------

        /// <summary>A sound at an entity, attenuated and panned from where you stand.</summary>
        private void At(Guid id, string cue, float gain = 1f)
        {
            var view = Boot != null && Boot.Entities != null ? Boot.Entities.Find(id) : null;
            if (view == null) return;
            var self = Boot.Entities.Find(Boot.SelfId);
            if (self == null || id == Boot.SelfId) { Emit(cue, gain, 0f); return; }

            var d = view.transform.position - self.transform.position;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist >= Silent) return;
            float fall = dist <= Full ? 1f : 1f - (dist - Full) / (Silent - Full);
            float pan = 0f;
            var cam = Camera.main;
            if (cam != null && dist > 0.01f)
            {
                var right = cam.transform.right;
                right.y = 0f;
                pan = Mathf.Clamp(Vector3.Dot(d / dist, right.normalized) * Mathf.Clamp01(dist / Full), -1f, 1f) * 0.7f;
            }
            Emit(cue, gain * fall * fall, pan);
        }

        /// <summary>A sound with no place — the UI, your own level-up.</summary>
        private void Ui(string cue) => Emit(cue, 1f, 0f);

        private void Emit(string cue, float gain, float pan)
        {
            if (Volume <= 0.001f || gain <= 0.01f || _voices == null) return;
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(cue, out var last) && now - last < Repeat) return;
            var clips = ClipsOf(cue);
            if (clips.Length == 0) return;
            _lastPlayed[cue] = now;

            var src = _voices[_next];
            _next = (_next + 1) % _voices.Length;
            src.Stop();
            src.clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            src.volume = Volume * gain * (Gain.TryGetValue(cue, out var g) ? g : 0.6f);
            src.panStereo = pan;
            src.pitch = UnityEngine.Random.Range(0.94f, 1.06f);   // the same clip twice should not sound like a loop
            src.Play();
        }

        private AudioClip[] ClipsOf(string cue)
        {
            if (_clips.TryGetValue(cue, out var clips)) return clips;
            clips = Resources.LoadAll<AudioClip>("Sounds/" + cue) ?? new AudioClip[0];
            _clips[cue] = clips;
            return clips;
        }

        // ----- Per frame: projectiles landing, and your footsteps ------------------------------------------

        private void Update()
        {
            float now = Time.time;
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (_pending[i].At > now) continue;
                var p = _pending[i];
                _pending.RemoveAt(i);
                At(p.Target, p.Cue, p.Gain);
            }
            Footsteps();
        }

        /// <summary>Your own steps only — everyone's would be a drum roll in town. Paced off the ground actually
        /// covered (walk ≈1 u/s, run ≈2.7 u/s), so a slowed character steps slower.</summary>
        private void Footsteps()
        {
            if (Boot == null || Boot.Entities == null) return;
            var self = Boot.Entities.Find(Boot.SelfId);
            if (self == null) { _hasLastSelf = false; return; }
            var pos = self.transform.position;
            float dt = Time.deltaTime;
            if (_hasLastSelf && dt > 0f)
            {
                var d = pos - _lastSelf;
                d.y = 0f;
                float speed = d.magnitude / dt;
                if (speed > 8f) speed = 0f;   // a teleport, not a stride
                _stepSpeed = Mathf.Lerp(_stepSpeed, speed, 1f - Mathf.Exp(-dt * 10f));
            }
            _lastSelf = pos;
            _hasLastSelf = true;

            if (_stepSpeed < 0.5f) { _nextStep = 0f; return; }
            float now = Time.time;
            if (_nextStep == 0f) _nextStep = now + 0.15f;
            if (now < _nextStep) return;
            _nextStep = now + Mathf.Lerp(0.55f, 0.32f, Mathf.InverseLerp(1f, 2.7f, _stepSpeed));

            var server = WorldMapper.ToServer(pos);
            Emit(WorldMap.InAnySafeZone(server.x, server.y) ? "step_stone" : "step_grass", 1f, 0f);
        }
    }
}
