using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Tap/click to play: tapping an entity targets + attacks it; tapping the ground walks there.
    /// Works with touch (phone) and mouse (editor). Raycasts against entity BoxColliders first,
    /// then falls back to the ground plane (y = 0).
    ///
    /// Taps that land on the HUD are ignored — otherwise pressing "Login" would also queue a walk
    /// to whatever ground happened to be under the button.
    /// </summary>
    public class TouchInput : MonoBehaviour
    {
        public GameBoot Boot;
        public GameHud Hud;

        private void Awake()
        {
            if (Hud == null) Hud = FindAnyObjectByType<GameHud>();
        }

        private void Update()
        {
            if (Boot == null || Boot.Phase != ClientPhase.InWorld) return;

            bool tapped = false;
            Vector2 screen = default;
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began) { tapped = true; screen = t.position; }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                tapped = true;
                screen = Input.mousePosition;
            }
            if (!tapped) return;

            if (Hud == null) Hud = FindAnyObjectByType<GameHud>();
            if (Hud != null && Hud.BlocksScreenPoint(screen)) return;

            var cam = Camera.main;
            if (cam == null) return;
            var ray = cam.ScreenPointToRay(screen);

            // 1) an entity?
            if (Physics.Raycast(ray, out var hit, 1000f))
            {
                var view = hit.collider.GetComponent<EntityView>();
                if (view != null && !view.IsSelf)
                {
                    Boot.Attack(view.Id);
                    return;
                }
            }

            // 2) the ground → move there
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float dist))
            {
                var point = ray.GetPoint(dist);
                var server = WorldMapper.ToServer(point);
                Boot.Move(server.x, server.y);
            }
        }
    }
}
