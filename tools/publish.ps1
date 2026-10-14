<#
.SYNOPSIS
  Puts a VERSIONED, ready-to-take build into builds/ — the folder the owner pulls from over remote
  desktop.

.DESCRIPTION
  Everything here is named from ONE source of truth, GameConstants.GameVersion, the same constant the
  login handshake and the APK's bundleVersion already use. Nothing is typed by hand, so nothing can
  drift:

      builds/Game.Server-<version>.zip   unzip on the phone, `dotnet Game.Server.dll`, done
      builds/L2Clone-<version>.apk       the client

  builds/ is git-ignored on purpose (see memory: the `builds` branch experiment was reverted) — these
  files are collected from the working copy, never committed.

.EXAMPLE
  pwsh tools/publish.ps1                 # server zip only (fast, no Unity)
  pwsh tools/publish.ps1 -Apk            # server zip + a fresh headless Unity APK
  pwsh tools/publish.ps1 -Apk -NoServer  # APK only
  pwsh tools/publish.ps1 -Windows -NoServer   # the PC test client only (BL-360)
#>
[CmdletBinding()]
param(
    # Also build the Android client. Needs the Unity Editor CLOSED (a running Unity.exe locks the
    # project and the build fails).
    [switch]$Apk,
    # Also build the Windows test client (BL-360): builds/Vardelar-Windows-<version>.zip — unzip anywhere,
    # run Vardelar.exe. Opens as a bordered, resizable window.
    [switch]$Windows,
    # Skip the server side.
    [switch]$NoServer,
    # How many of each (server zip, APK) builds/ keeps; older ones are deleted at the end.
    [int]$Keep = 4,
    [string]$Configuration = "Release",
    [string]$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$buildsDir = Join-Path $repo "builds"

# ----- the one version number ---------------------------------------------------------------------
$constants = Join-Path $repo "Game.Shared\GameConstants.cs"
$match = Select-String -Path $constants -Pattern 'GameVersion\s*=\s*"([^"]+)"' | Select-Object -First 1
if (-not $match) { throw "Could not read GameVersion from $constants" }
$version = $match.Matches[0].Groups[1].Value
Write-Host "Publishing version $version" -ForegroundColor Cyan

if (-not (Test-Path $buildsDir)) { New-Item -ItemType Directory -Path $buildsDir | Out-Null }

# ----- server -------------------------------------------------------------------------------------
if (-not $NoServer) {
    $stage = Join-Path $buildsDir "Game.Server"
    if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }

    # A framework-dependent publish: the phone already has the .NET runtime, and the runtimeconfig
    # carries ServerGarbageCollection=false from the csproj, which is what stopped the phone's
    # "GC heap initialization failed" hand-edit after every deploy.
    dotnet publish (Join-Path $repo "Game.Server\Game.Server.csproj") -c $Configuration -o $stage --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

    # Never ship a database: game.db is the PHONE's live save. Shipping one would overwrite his
    # characters with whatever this workstation happens to have.
    Get-ChildItem $stage -Filter "game.db*" -ErrorAction SilentlyContinue | Remove-Item -Force

    $zip = Join-Path $buildsDir "Game.Server-$version.zip"
    if (Test-Path $zip) { Remove-Item -Force $zip }
    Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip
    "{0}  ({1:N1} MB)" -f $zip, ((Get-Item $zip).Length / 1MB) | Write-Host -ForegroundColor Green
}

# ----- client -------------------------------------------------------------------------------------
$unityProject = Join-Path $repo "Game.Client.Unity"

# One headless Unity run of a CommandLineBuild method. Returns the log path; throws when Unity failed.
function Invoke-UnityBuild([string]$method, [string]$buildTarget, [string]$log) {
    if (-not (Test-Path $UnityExe)) { throw "Unity not found at $UnityExe" }
    if (Get-Process Unity -ErrorAction SilentlyContinue) {
        throw "Unity.exe is running — close the Editor first, it locks the project."
    }
    Write-Host "Unity headless build $buildTarget (several minutes) — log: $log" -ForegroundColor Cyan

    # Start-Process -Wait, NOT the call operator: Unity.exe is a GUI-subsystem binary, so `& $UnityExe`
    # returns the instant it is launched, leaves $LASTEXITCODE empty and makes a perfectly good build
    # look like a failure (the script then checks for an APK that is still being written).
    #
    # -executeMethod needs the FULLY QUALIFIED name; a bare class name fails with "executeMethod class
    # could not be found" and Unity can still exit 0, so the log is checked below either way.
    #
    # ⚠ WaitForExit(), NOT -Wait: -Wait also waits for every DESCENDANT, and Unity leaves its Roslyn
    # compiler server (VBCSCompiler) running ~10 minutes after it exits, so the script sat idle long after
    # the APK was written (0.214.12).
    $unity = Start-Process -FilePath $UnityExe -PassThru -NoNewWindow -ArgumentList @(
        "-quit", "-batchmode", "-nographics",
        "-projectPath", $unityProject,
        "-buildTarget", $buildTarget,
        "-executeMethod", "Game.Client.Editor.CommandLineBuild.$method",
        "-logFile", $log)
    $null = $unity.Handle   # cache the handle now, or ExitCode reads back empty after the exit
    $unity.WaitForExit()
    $unityExit = $unity.ExitCode

    if (Test-Path $log) {
        $bad = Select-String -Path $log -Pattern "error CS|executeMethod class .* could not be found"
        if ($bad) { $bad | Select-Object -First 20 | ForEach-Object { Write-Host $_.Line -ForegroundColor Red } }
    }
    if ($unityExit -ne 0) { throw "Unity build failed (exit $unityExit) — see $log" }
}

if ($Apk) {
    $log = Join-Path $env:TEMP "unitybuild-$version.log"
    Invoke-UnityBuild "BuildAndroid" "Android" $log

    $apkSrc = Join-Path $unityProject "builds\L2Clone.apk"
    if (-not (Test-Path $apkSrc)) { throw "Unity built no APK — see $log" }

    # An APK whose timestamp did not move means Unity built nothing (the classic silent failure).
    $age = (Get-Date) - (Get-Item $apkSrc).LastWriteTime
    if ($age.TotalMinutes -gt 30) { throw "APK is $([int]$age.TotalMinutes) min old — Unity built nothing. See $log" }

    $apkDst = Join-Path $buildsDir "L2Clone-$version.apk"
    Copy-Item $apkSrc $apkDst -Force
    "{0}  ({1:N1} MB)" -f $apkDst, ((Get-Item $apkDst).Length / 1MB) | Write-Host -ForegroundColor Green
    Write-Host "adb install -r `"$apkSrc`"" -ForegroundColor DarkGray
}

if ($Windows) {
    $log = Join-Path $env:TEMP "unitybuild-win-$version.log"
    $winDir = Join-Path $unityProject "builds\windows"
    # A fresh folder, so a file a previous build left behind can never ride along in the zip.
    if (Test-Path $winDir) { Remove-Item -Recurse -Force $winDir }
    Invoke-UnityBuild "BuildWindows" "Win64" $log

    $exe = Join-Path $winDir "Vardelar.exe"
    if (-not (Test-Path $exe)) { throw "Unity built no Vardelar.exe — see $log" }

    # Unity's debug-symbol folder is for crash dumps on THIS machine; it doubles the zip and players never need it.
    Get-ChildItem $winDir -Directory -Filter "*_BurstDebugInformation_DoNotShip" | Remove-Item -Recurse -Force

    $winZip = Join-Path $buildsDir "Vardelar-Windows-$version.zip"
    if (Test-Path $winZip) { Remove-Item -Force $winZip }
    Compress-Archive -Path (Join-Path $winDir "*") -DestinationPath $winZip
    "{0}  ({1:N1} MB)" -f $winZip, ((Get-Item $winZip).Length / 1MB) | Write-Host -ForegroundColor Green
}

# ----- keep builds/ small ---------------------------------------------------------------------------
# Only the newest $Keep server zips and $Keep APKs stay (owner, 2026-10-14: "make the /builds to have only the last
# 3~5 servers and apks ... with the server up and running and the deploy command i wont be entering drive to manage
# it"). Ranked by the VERSION in the file name, never the file date, so a republished old version cannot push a newer
# one out. The live box keeps its own last 3 APKs (tools/deploy.ps1); nothing here touches it.
foreach ($pattern in @("Game.Server-*.zip", "L2Clone-*.apk", "Vardelar-Windows-*.zip")) {
    $old = Get-ChildItem $buildsDir -Filter $pattern -File |
        Where-Object { $_.BaseName -match '-(\d+\.\d+\.\d+)$' } |
        Sort-Object { [version]($_.BaseName -replace '^.*-(\d+\.\d+\.\d+)$', '$1') } -Descending |
        Select-Object -Skip $Keep
    foreach ($f in $old) { Remove-Item $f.FullName -Force; Write-Host "builds/: removed $($f.Name)" -ForegroundColor DarkGray }
}
