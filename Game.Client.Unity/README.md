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
3. **Build our shared DLL for Unity — the copy is AUTOMATIC:**
   ```bash
   dotnet build Game.Shared/Game.Shared.csproj -c Release -f netstandard2.1
   ```
   That's it. A post-build target in `Game.Shared.csproj` (`CopyToUnityPlugins`) drops
   `Game.Shared.dll` + `.pdb` into `Game.Client.Unity/Assets/Plugins/`, creating the folder if needed.
   It runs on **every** netstandard2.1 build — including the plain `dotnet build Game.sln` you already
   run — so the Unity project simply never goes stale.

   There is nothing to re-copy when DTOs or formulas change; that manual step was the whole problem.
   Missing one copy doesn't look like a missed copy, it looks like a protocol mismatch, and you'd hunt
   it in the networking code.

   - Skip the copy: `dotnet build -p:UnityPluginCopy=false`
   - The target no-ops if `Game.Client.Unity/` isn't there, so a server-only checkout still builds.
   - Unity's `.meta` files next to the DLL are left alone — Unity owns them, and overwriting them
     loses the importer settings.

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
- **Real phone over USB (easiest — see 4a):** `http://localhost:5238/game`
- **Real phone on the same Wi-Fi:** `http://<your-PC-LAN-IP>:5238/game` (e.g. `http://192.168.1.20:5238/game`)

> ⚠ **`ServerUrl` is a serialized field on the `GameBoot` component**, so the value saved in the SCENE
> wins over the default in `GameBoot.cs`. Editing the C# default changes nothing for a scene that
> already has a `GameBoot` on it — set it in the **Inspector**.

### 4a. Cabled phone: `adb reverse` (skips Wi-Fi, LAN IP *and* the firewall)
If the phone is on a USB cable, don't bother with the LAN at all. `adb reverse` tunnels the
**phone's** `localhost:5238` over the cable to **your PC's** `localhost:5238`:

```bash
adb reverse tcp:5238 tcp:5238
```

Then set the client's Server URL to `http://localhost:5238/game` and run the server normally.
This removes all three of the Wi-Fi route's failure points at once:

- no LAN IP to look up (and no picking the right subnet),
- **no Windows firewall rule** for port 5238 — the one manual step the Wi-Fi route can't automate,
- no requirement that the phone be on the same Wi-Fi, or on Wi-Fi at all.

`adb` ships with Unity's Android module, so it's already on your disk even if it isn't on `PATH`:

```
C:\Program Files\Unity\Hub\Editor\<version>\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe
```

Check the phone is actually visible first — `adb devices -l` should list it as `device`
(`unauthorized` means the "Allow USB debugging?" prompt is still waiting on the phone's screen).

> **The tunnel does NOT survive** unplugging the phone, rebooting it, or an adb server restart, and
> it fails silently — the client just can't connect. Re-run the `adb reverse` line after any of those.

**Two server-side gotchas for a real phone — both are now DONE for you:**

1. **LAN binding: already handled.** The server binds `http://0.0.0.0:5238` (all interfaces), so
   `localhost` keeps working for the WPF client *and* the phone can reach your PC. Just
   `dotnet run --project Game.Server` — no environment variable needed.

   On startup it prints the exact URL to paste into the Unity client:
   ```
   Unity/phone clients on this LAN: http://192.168.1.20:5238/game
   ```
   (If several are listed, pick the one on the same subnet as your phone.) You still need to **allow
   port 5238 through the Windows firewall** — that part is not automatable from here, and a blocked
   port looks exactly like a wrong IP: a connection that just times out.

   > This README used to say `ASPNETCORE_URLS=http://0.0.0.0:5238 dotnet run`. That never worked:
   > `Program.cs` hardcoded `UseUrls("http://localhost:5238")`, which silently beat the variable, so the
   > server stayed on loopback and the phone failed to connect with nothing in the log to explain it.
   > An explicit `ASPNETCORE_URLS` is honoured now, and `launchSettings.json` no longer sets
   > `applicationUrl` (that key is *also* delivered as `ASPNETCORE_URLS` and quietly won).

2. **Cleartext HTTP: already handled** in `Assets/Plugins/Android/AndroidManifest.xml`. Since Android 9
   plain HTTP is blocked, and our dev server is `http://…`. The fix is an **attribute on the
   `<application>` element**, not an element of its own — that's the bit that isn't obvious:

   ```xml
   <uses-permission android:name="android.permission.INTERNET" />

   <application android:usesCleartextTraffic="true">
       ...activities...
   </application>
   ```

   Without it the phone fails with a bare "cleartext not permitted" and no other clue.
   *(Dev only — put the server behind HTTPS for a real release and delete the attribute.)*

## 5. Run
- **In the Editor** first (Play): with the server running locally use `http://localhost:5238/game`.
  You should auto-login, spawn, see colored billboards (green = you, cyan = players, red = mobs,
  yellow = NPCs), and tap-to-move / tap-to-attack.
- **On the phone:** File → Build Settings → Android → *Switch Platform* (slow the first time — it
  reimports every asset) → connect the phone (USB debugging on) → **Build And Run**. That compiles
  the APK, installs it over USB and launches it.

> **If Editor Play mode is unusable** (it has hard-crashed the owner's PC), skip it entirely and go
> straight to *Build And Run* — the device build doesn't involve Play mode at all. Pair it with the
> `adb reverse` tunnel from 4a and you never need the Editor to connect to anything.

**Debugging a phone build.** You get no Console window, so read the device log:

```bash
adb logcat -s Unity          # only Unity's lines
adb logcat -c                # clear first, so you see only this run
```

Two failures that look identical on-screen (an app that starts and just never connects):
- **Wrong URL / no tunnel** → a connect timeout in logcat. Re-check 4a, and remember the tunnel dies
  on unplug.
- **IL2CPP stripping** → a "method not found" for a SignalR or `System.Text.Json` member, NOT a
  network error. Fix with the `link.xml` noted at the end of §2.

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
