using Game.Shared;
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

        /// <summary>How far a finger may travel and still count as a tap rather than a drag.</summary>
        public float TapSlopPixels = 40f;

        private void Update()
        {
            if (Boot == null || Boot.Phase != ClientPhase.InWorld) return;

            bool tapped = false;
            Vector2 screen = default;
            if (Input.touchCount > 0)
            {
                // A tap is decided on RELEASE, not on press, and only while exactly one finger is
                // down. Acting on Began made the first finger of a pinch-to-zoom queue a walk before
                // the second finger ever landed. Requiring the finger not to have travelled far also
                // stops a drag from being read as a tap.
                var t = Input.GetTouch(0);
                if (Input.touchCount == 1 && t.phase == TouchPhase.Ended &&
                    (t.position - t.rawPosition).magnitude < TapSlopPixels)
                {
                    tapped = true;
                    screen = t.position;
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                tapped = true;
                screen = Input.mousePosition;
            }
            if (!tapped) return;

            // uGUI answers this itself now: the canvas raycaster is asked about the tap POINT, so we
            // no longer keep a hand-maintained list of UI rectangles that could silently go stale
            // whenever a panel moved.
            if (UiKit.OverUi(screen)) return;

            var cam = Camera.main;
            if (cam == null) return;
            var ray = cam.ScreenPointToRay(screen);

            // 1) an entity?
            if (Physics.Raycast(ray, out var hit, 1000f))
            {
                var view = hit.collider.GetComponent<EntityView>();
                if (view != null && !view.IsSelf)
                {
                    // Tapping SELECTS. What happens next depends on WHAT it is:
                    //   mob  → attack (tapping a vendor used to swing at him, which is how an NPC got
                    //          killed on the phone; the server refuses that now, this stops us asking)
                    //   NPC  → TALK. Every service in the game — quests, class change, vendors,
                    //          gatekeepers, buffers — is behind a conversation, so the tap that
                    //          selects an NPC should also start one.
                    Boot.TargetId = view.Id;
                    if (Boot.Entities != null && Boot.Entities.TryGetState(view.Id, out var dto) && !dto.Dead)
                    {
                        if (dto.Kind == EntityKind.Mob) Boot.Attack(view.Id);
                        else if (dto.Kind == EntityKind.Npc) Boot.TalkToNpc(view.Id);
                    }
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
