using System.Collections.Generic;
using Game.Shared;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client
{
    /// <summary>
    /// `BL-354` — the model standing beside the character-creation buttons, wearing exactly what they say: race,
    /// body, hairstyle, hair colour, skin and the class's starting weapon. The owner (2026-10-13): <i>"the creation
    /// screen need a model standing .. to see each type of hair color etc .. now its just words"</i>.
    ///
    /// <para>It is a tiny stage far below the world (<see cref="StageOrigin"/>) with its own camera drawing into a
    /// <see cref="RenderTexture"/> that a RawImage shows. The body comes through the SAME two calls a spawned player
    /// uses — <see cref="ModelLibrary.PlayerPrefab"/> and <see cref="CharacterLook.Apply(GameObject, Race, Gender, int, int, int)"/>
    /// — so what you pick here is what the world draws, with nothing to keep in step.</para>
    ///
    /// <para>🔑 It brings its OWN light: two point lights on the stage. The scene's sun follows the game clock, so
    /// at night it would hand you a black silhouette to choose a hair colour by. A point light's range keeps it off
    /// the world (5 km above), so no culling mask is needed — which matters, URP ignores <c>Light.cullingMask</c>.</para>
    ///
    /// <para>Every pick rebuilds the body from the prefab. The race build is a multiply on the root's scale
    /// (<see cref="CharacterLook"/>), so re-applying a look onto the same instance would compound it.</para>
    /// </summary>
    public class CharacterPreview : MonoBehaviour
    {
        public static readonly Vector3 StageOrigin = new Vector3(0f, -5000f, 0f);

        /// <summary>The same pose the editor's look sheet uses: the body's forward is -Z, so 160° turns it to face
        /// the camera with a quarter of its side showing.</summary>
        public const float FrontYaw = 160f;

        public RenderTexture Texture { get; private set; }

        private Camera _cam;
        private GameObject _model;
        private Animator _animator;
        private readonly List<GameObject> _held = new List<GameObject>();
        private float _yaw = FrontYaw;

        public static CharacterPreview Create(int width, int height)
        {
            var go = new GameObject("CharacterPreview");
            go.transform.position = StageOrigin;
            var p = go.AddComponent<CharacterPreview>();
            p.Build(width, height);
            return p;
        }

        private void Build(int width, int height)
        {
            Texture = new RenderTexture(width, height, 24) { antiAliasing = 4, name = "CharacterPreview" };

            _cam = new GameObject("PreviewCamera").AddComponent<Camera>();
            _cam.transform.SetParent(transform, false);
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f, 1f);
            _cam.fieldOfView = 30f;
            _cam.nearClipPlane = 0.3f;
            _cam.farClipPlane = 15f;   // the stage and nothing else
            _cam.targetTexture = Texture;
            // Framed for a 2 m body (an elf is 1.1×): aim at the waist from 5 m, a 30° lens shows ±1.34 m of it.
            _cam.transform.localRotation = Quaternion.Euler(4f, 180f, 0f);
            _cam.transform.localPosition = new Vector3(0f, 1.0f, 0f) - _cam.transform.forward * 5f;

            // Key light high on the camera's left, a cooler fill low on its right. Intensities are for URP's
            // physical falloff (1/d²) at ~3.5 m.
            AddLight("Key", new Vector3(-1.8f, 2.6f, 2.6f), new Color(1.00f, 0.95f, 0.88f), 14f);
            AddLight("Fill", new Vector3(2.2f, 1.2f, 2.4f), new Color(0.75f, 0.82f, 1.00f), 6f);
            AddLight("Rim", new Vector3(0.6f, 2.2f, -2.2f), new Color(1.00f, 1.00f, 1.00f), 6f);
        }

        private void AddLight(string name, Vector3 local, Color colour, float intensity)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(transform, false);
            l.transform.localPosition = local;
            l.type = LightType.Point;
            l.color = colour;
            l.intensity = intensity;
            l.range = 8f;
            l.shadows = LightShadows.None;
        }

        /// <summary>Put this look on the stage. Returns false when no prefab answers (a build without models), and
        /// the caller hides the frame rather than showing an empty box.</summary>
        public bool Show(Race race, BaseClass cls, Gender gender, int hair, int hairColor, int skin)
        {
            if (_model != null) Kill(_model);
            _held.Clear();   // they were children of the old body's hands
            _model = null;
            _animator = null;

            var prefab = ModelLibrary.PlayerPrefab(race, cls, gender);
            if (prefab == null) return false;

            _model = Instantiate(prefab);
            CharacterLook.Apply(_model, race, gender, hair, hairColor, skin);   // before parenting: the build is its scale
            foreach (var c in _model.GetComponentsInChildren<Collider>()) Kill(c);
            _model.transform.SetParent(transform, false);
            _model.transform.localPosition = Vector3.zero;
            _model.transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);

            // The starting weapon of the class: a sword for a fighter, a staff for a mage.
            _animator = _model.GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                bool mage = cls == BaseClass.Mage;
                HeldWeapons.Dress(_animator, _model.transform, mage ? WeaponType.TwoHandedBlunt : WeaponType.Sword,
                                  mage, false, _held);
            }
            return true;
        }

        /// <summary>Spin the body (a drag on the picture).</summary>
        public void Turn(float degrees)
        {
            _yaw = Mathf.Repeat(_yaw + degrees, 360f);
            if (_model != null) _model.transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        /// <summary>Back to the front view (every time the form opens).</summary>
        public void ResetTurn() => Turn(FrontYaw - _yaw);

        /// <summary>The camera only renders while the form is open.</summary>
        public void SetVisible(bool on)
        {
            if (_cam != null) _cam.enabled = on;
            if (_model != null) _model.SetActive(on);
        }

        private void OnDestroy()
        {
            if (Texture != null) { Texture.Release(); Kill(Texture); }
        }

        /// <summary>The editor's headless check (ModelSetup.PreviewCreation) builds this stage outside play mode.</summary>
        private static void Kill(Object o)
        {
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }

    /// <summary>Drag sideways on the preview picture to turn the model.</summary>
    public class CharacterPreviewDrag : MonoBehaviour, IDragHandler
    {
        public CharacterPreview Preview;

        public void OnDrag(PointerEventData e)
        {
            // Half a turn per picture-width, the same feel as the world camera's drag.
            float width = Mathf.Max(1f, ((RectTransform)transform).rect.width * transform.lossyScale.x);
            if (Preview != null) Preview.Turn(-e.delta.x / width * 180f);
        }
    }
}
