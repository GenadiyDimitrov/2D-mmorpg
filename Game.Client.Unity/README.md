# Game.Client.Unity — mobile client (vertical slice)

Goal of this slice: **connect to the server on your phone, see entities, tap-to-move, tap-to-attack.**
Built flat (near top-down) now; going 2.5D later is just lowering the camera pitch (see the end).

This folder holds the **scripts + setup steps** only. Unity generates the project (scene,
ProjectSettings, .meta files) — you create it once in the Editor and drop these `Assets/Scripts` in.
The heavy lifting (`Game.Shared`, `NetworkChannel`) is reused from the server solution.

---

## 1. Create the Unity project
- Install **Unity 2022.3 LTS** (via Unity Hub).
- New project → **3D (Built-in Render Pipeline) / "3D Core"** → name it `Game.Client.Unity`,
  and point it at THIS folder (so `Assets/Scripts` here is picked up), or create elsewhere and
  copy `Assets/Scripts` in.

## 2. Add the networking dependencies (the one fiddly part)
Unity can't reference `net8.0`, so it uses the **netstandard2.1** build of our shared code + the
SignalR client:

1. **Install NuGetForUnity** (package manager for NuGet DLLs inside Unity):
   Window → Package Manager → **+** → *Add package from git URL* →
   `https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity`
2. **NuGet → Manage NuGet Packages** → search & install **`Microsoft.AspNetCore.SignalR.Client`**
   (pulls its dependencies into `Assets/Packages`).
3. **Build our shared DLL for Unity and copy it in:**
   ```bash
   dotnet build Game.Shared/Game.Shared.csproj -c Release -f netstandard2.1
   ```
   Copy `Game.Shared/bin/Release/netstandard2.1/Game.Shared.dll` into
   `Game.Client.Unity/Assets/Plugins/` (create the folder). Re-copy whenever DTOs/formulas change.

> IL2CPP/Android note: if a SignalR method gets stripped, add a `link.xml` preserving
> `Microsoft.AspNetCore.SignalR.Client` and `System.Text.Json`. Start with the default Mono/IL2CPP
> settings; only touch this if you hit a runtime "method not found".

## 3. Build the scene
Create an empty scene with:
- **Ground**: GameObject → 3D Object → **Plane**, scale ~ (30,1,30), position (120,0,120)
  (roughly the middle of the 240×240 mapped world). Just so the ground raycast/move has a floor.
- **Main Camera**: add the **`CameraRig`** component (leave Pitch 78 for now).
- An empty **`GameController`** GameObject with:
  - **`EntityManager`**
  - **`GameBoot`** — drag the `EntityManager` into *Entities* and the Main Camera's `CameraRig`
    into *CameraRig*. Set **Server URL** (see below).
  - **`TouchInput`** — drag the `GameBoot` into *Boot*.
- (`UnityMainThreadDispatcher` self-creates; you don't need to add it.)

## 4. Point it at your server
- **Emulator (Android Studio AVD):** `http://10.0.2.2:5238/game`
- **Real phone on the same Wi-Fi:** `http://<your-PC-LAN-IP>:5238/game` (e.g. `http://192.168.1.20:5238/game`)

**Two server-side gotchas for a real phone:**
1. The server must listen on the LAN, not just localhost. Run it with:
   ```bash
   ASPNETCORE_URLS=http://0.0.0.0:5238 dotnet run --project Game.Server
   ```
   (and allow port 5238 through the PC firewall).
2. Android blocks cleartext HTTP by default. For testing, enable it: Project Settings → Player →
   Android → *Publishing Settings*, or add a custom `AndroidManifest.xml` with
   `android:usesCleartextTraffic="true"`. (Later: put the server behind HTTPS and drop this.)

## 5. Run
- **In the Editor** first (Play): with the server running locally use `http://localhost:5238/game`.
  You should auto-login, spawn, see colored billboards (green = you, cyan = players, red = mobs,
  yellow = NPCs), and tap-to-move / tap-to-attack.
- **On the phone:** File → Build Settings → Android → *Switch Platform* → connect the phone (USB
  debugging on) → **Build And Run**.

---

## Going 2.5D later (the promised one-liner)
Select the Main Camera and lower **`CameraRig.Pitch`** from ~78 toward **~50**, and bump *Distance*.
The upright billboards tilt into an angled view with **zero** other changes. When you swap the quad
billboards for animated 3D models, it's a pure visual upgrade in `EntityManager.Create` /
`EntityView` — the networking, coordinates, camera and input all stay exactly as they are.

## What's in the slice / what's next
- **In:** connect + auth + enter world, snapshot → billboards with interpolation, follow camera,
  tap move/attack. `NetworkChannel` also already exposes UseSkill/SetMoveState/etc. for later.
- **Next:** name/HP labels, target frame + skill buttons UI, cast bars (incl. the mob cast bar),
  inventory/quest/shop screens, then the 3D-model swap.
