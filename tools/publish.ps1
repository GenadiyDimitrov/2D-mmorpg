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
#>
[CmdletBinding()]
param(
    # Also build the Android client. Needs the Unity Editor CLOSED (a running Unity.exe locks the
    # project and the build fails).
    [switch]$Apk,
    # Skip the server side.
    [switch]$NoServer,
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
if ($Apk) {
    if (-not (Test-Path $UnityExe)) { throw "Unity not found at $UnityExe" }
    if (Get-Process Unity -ErrorAction SilentlyContinue) {
        throw "Unity.exe is running — close the Editor first, it locks the project."
    }

    $unityProject = Join-Path $repo "Game.Client.Unity"
    $log = Join-Path $env:TEMP "unitybuild-$version.log"
    Write-Host "Unity headless build (several minutes) — log: $log" -ForegroundColor Cyan

    # Start-Process -Wait, NOT the call operator: Unity.exe is a GUI-subsystem binary, so `& $UnityExe`
    # returns the instant it is launched, leaves $LASTEXITCODE empty and makes a perfectly good build
    # look like a failure (the script then checks for an APK that is still being written).
    #
    # -executeMethod needs the FULLY QUALIFIED name; a bare class name fails with "executeMethod class
    # could not be found" and Unity can still exit 0, so the log is checked below either way.
    $unity = Start-Process -FilePath $UnityExe -Wait -PassThru -NoNewWindow -ArgumentList @(
        "-quit", "-batchmode", "-nographics",
        "-projectPath", $unityProject,
        "-executeMethod", "Game.Client.Editor.CommandLineBuild.BuildAndroid",
        "-logFile", $log)
    $unityExit = $unity.ExitCode

    if (Test-Path $log) {
        $bad = Select-String -Path $log -Pattern "error CS|executeMethod class .* could not be found"
        if ($bad) { $bad | Select-Object -First 20 | ForEach-Object { Write-Host $_.Line -ForegroundColor Red } }
    }

    $apkSrc = Join-Path $unityProject "builds\L2Clone.apk"
    if ($unityExit -ne 0 -or -not (Test-Path $apkSrc)) { throw "Unity build failed (exit $unityExit) — see $log" }

    # An APK whose timestamp did not move means Unity built nothing (the classic silent failure).
    $age = (Get-Date) - (Get-Item $apkSrc).LastWriteTime
    if ($age.TotalMinutes -gt 30) { throw "APK is $([int]$age.TotalMinutes) min old — Unity built nothing. See $log" }

    $apkDst = Join-Path $buildsDir "L2Clone-$version.apk"
    Copy-Item $apkSrc $apkDst -Force
    "{0}  ({1:N1} MB)" -f $apkDst, ((Get-Item $apkDst).Length / 1MB) | Write-Host -ForegroundColor Green
    Write-Host "adb install -r `"$apkSrc`"" -ForegroundColor DarkGray
}
