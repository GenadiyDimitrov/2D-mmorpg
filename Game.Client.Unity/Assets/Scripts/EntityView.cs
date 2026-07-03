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

        public void Init(Color color)
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                // Unlit so it reads the same at any camera angle (no scene lighting set up yet).
                var shader = Shader.Find("Unlit/Color");
                if (shader != null) _renderer.material = new Material(shader);
                _renderer.material.color = color;
            }
            _cam = Camera.main != null ? Camera.main.transform : null;
            _target = transform.position;
        }

        /// <summary>Set the newest server position (ground plane); height is added here.</summary>
        public void SetTarget(Vector3 groundPos)
        {
            _target = groundPos + Vector3.up * HalfHeight;
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, _target, Time.deltaTime * 10f);
        }

        private void LateUpdate()
        {
            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam != null) transform.rotation = _cam.rotation;   // billboard toward the camera
        }
    }
}
