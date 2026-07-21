# Game.Client.Unity — mobile client

The Android client. A thin renderer over the same authoritative server the desktop client talks to:
it shares `Game.Shared` and the `NetworkChannel` transport seam, so the wire contract cannot drift
between the two clients.

**→ [Build & run guide](../docs/guides/UnityClient.md)** — phone setup, `adb reverse`, deploying,
reading device logs.
**→ [Test checklist](../docs/testing/TestChecklist.Unity.md)** — what to verify once it runs.

## What it does

Sign in, create or pick a character, enter the world, and play with one thumb: **tap the ground to
walk, tap a monster to target and attack.** On screen: your vitals and experience, a target panel,
floating nameplates with health bars, chat, and a connection strip that reports whether server
frames are actually arriving.

The view is near top-down and flat. Entities are coloured billboards standing upright rather than
lying on the floor, which means going 2.5D later is a camera change (lower `CameraRig.Pitch` toward
~50°) rather than a rewrite, and swapping billboards for animated models is a pure visual upgrade.

## How it fits together

```
GameBoot        connection state machine (offline → connecting → auth → select → in world)
                and the single source of truth the UI reads. Self-wires anything missing.
NetworkChannel  SignalR transport. Callbacks arrive off the main thread.
EntityManager   applies the server's per-tick delta (spawn / update / despawn) to the scene
EntityView      one entity: interpolates toward the last server position, billboards to camera
CameraRig       follows the player at a fixed orbit
TouchInput      tap → raycast → attack an entity, or walk to a point
GameHud         all UI: login, character select, in-world HUD, on-screen log console
WorldMapper     server's 2D world (X/Y) ↔ Unity's ground plane (X/Z)
```

Two details that are load-bearing rather than incidental:

- **Server callbacks arrive on a background thread**, and Unity APIs may only be touched on the main
  one. Everything hops through `UnityMainThreadDispatcher` first.
- **The world feed is a delta, not a snapshot.** An entity missing from a frame is *unchanged*, not
  removed — only an explicit despawn removes it.

## State of it

Working: connect, authenticate, character select and creation, entering the world, the delta feed,
movement, basic attack, and the HUD.

Not built yet: inventory, skills and casting, party, trade, shops, quests, cast bars, damage
numbers, animation, and camera controls. The desktop client remains the full-featured one; this is
a viewport.

The UI is Unity's IMGUI (`OnGUI`) rather than uGUI/TextMeshPro — a deliberate trade so it needs no
Canvas, prefabs or font assets and works on device with zero scene wiring. **It is meant to look
plain**; a proper UI pass belongs with the art pass.

## Requirements

Unity **6000.3.19f1**, the Android Build Support module, and a build of `Game.Shared` (copied into
`Assets/Plugins/` automatically by the server-side build). See the guide for the rest.
