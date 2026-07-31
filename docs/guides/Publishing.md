# Publishing a build (docs/guides/Publishing.md)

`builds/` is the collection point: the owner remote-desktops into the workstation and takes the files
from there. It is **git-ignored on purpose** — the `builds` git branch experiment was tried and
reverted, so nothing here is ever committed.

## One command

```powershell
pwsh tools/publish.ps1            # server zip only (fast, no Unity)
pwsh tools/publish.ps1 -Apk       # server zip + a fresh headless Unity APK
pwsh tools/publish.ps1 -Apk -NoServer
```

It produces, both named from `GameConstants.GameVersion`:

```
builds/Game.Server-<version>.zip    unzip on the phone → `dotnet Game.Server.dll`
builds/L2Clone-<version>.apk        the Android client
```

**Everything is stamped from the one constant** — the same one the login handshake compares and the
one `CommandLineBuild.StampVersion` writes into `bundleVersion`. So bump
`GameConstants.GameVersion` *before* publishing, or the zip and the APK will both carry the previous
number. (This is the same rule as the deploy order: `dotnet build` before the Unity build, or the APK
ships a stale version constant.)

## What the script takes care of

- **A framework-dependent publish.** The phone has the .NET runtime; the zip carries only the app.
- **`ServerGarbageCollection=false` rides along** in `Game.Server.runtimeconfig.json` (set in
  `Game.Server.csproj`). That is what ended the `nano Game.Server.runtimeconfig.json` chore after every
  deploy — the phone's Server GC tried to reserve 256 GiB and the process died with
  `GC heap initialization failed 0x8007000E`.
- **`game.db` is never shipped.** The database in the zip would overwrite the phone's live characters.
- **Unity must be CLOSED** for `-Apk`; the script refuses if `Unity.exe` is running, checks the log for
  `error CS` and for the "executeMethod class could not be found" failure (which can still exit 0), and
  treats an APK whose timestamp did not move as a failed build.
- The Unity project keeps writing its own `Game.Client.Unity/builds/L2Clone.apk` — that unversioned
  path is what `adb install -r` uses, and the versioned copy in `builds/` is the deliverable.

## History note

Before this script, the server was published by hand and irregularly: `builds/` held a
`Game.Server-0.36.0.zip`, a loose unzipped **0.37.0** in `builds/Game.Server/`, and an unversioned
`L2Clone.apk` — so **0.38.0 and 0.38.1 were never published at all**, which is exactly the question
that prompted the script. The stale loose `builds/server/` (0.36.0) folder is a leftover of the same
era and can be deleted.
