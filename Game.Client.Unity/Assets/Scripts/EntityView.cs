using System;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// One visible entity: an UPRIGHT BILLBOARD quad that (a) smoothly interpolates toward the
    /// last server position (the server ticks ~10/s) and (b) faces the camera every frame. Because
    /// it's an upright billboard (not a floor decal), tilting the camera later to 2.5D "just works"
    /// with no change here — swap the quad for a 3D model and it's a pure visual upgrade.
    /// </summary>
    public class EntityView : MonoBehaviour
    {
        public Guid Id;
        public bool IsSelf;

        private const float HalfHeight = 0.75f;   // lifts the billboard so its base sits on the ground
        private Vector3 _target;
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
            _target = transform.position;
            transform.position = _target;
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

        /// <summary>Set the newest server position (ground plane); height is added here.</summary>
        public void SetTarget(Vector3 groundPos)
        {
            _target = groundPos + Vector3.up * HalfHeight;
        }

        /// <summary>
        /// Chase the last server position, frame-rate independently.
        ///
        /// This was `Lerp(position, target, Time.deltaTime * 10f)`, which is the classic broken
        /// smoothing: `deltaTime * 10` is a BLEND FACTOR, so the result depends on how often Update
        /// runs. At 60fps it is 0.17 per frame; on a frame that hitches to 120ms it is 1.2 — past 1,
        /// which overshoots the target and snaps back. Every stutter in the renderer therefore became
        /// a stutter in the MOTION, on top of whatever the network was doing.
        ///
        /// `1 - exp(-k * dt)` is the same exponential chase expressed correctly: the same convergence
        /// per SECOND regardless of frame rate, and never greater than 1.
        ///
        /// It still eases in — a chase always lags and decelerates as it closes — which is visible
        /// when updates are sparse. The real cure is snapshot interpolation (buffer the last two
        /// positions and play back ~150ms behind at constant speed), which the server's per-entity
        /// Speed field already carries what is needed for. Left as the next step rather than smuggled
        /// in with a bug fix.
        /// </summary>
        private void Update()
        {
            const float chasePerSecond = 12f;
            float t = 1f - Mathf.Exp(-chasePerSecond * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, _target, t);
        }

        private void LateUpdate()
        {
            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam != null) transform.rotation = _cam.rotation;   // billboard toward the camera
        }
    }
}
