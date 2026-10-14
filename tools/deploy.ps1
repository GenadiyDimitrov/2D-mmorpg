<#
.SYNOPSIS
  DEPLOY = put the current version's build from builds/ onto the LIVE server (the Oracle box), with the
  players warned first. The owner's keyword (2026-10-14): "build"/"publish" only ever fill builds/;
  "deploy" / "update the server" is this script, and nothing else touches the live machine.

.DESCRIPTION
  Separate from tools/publish.ps1 on purpose: publishing is free to repeat, deploying restarts a server
  people are playing on. It ships what builds/ holds for the CURRENT GameConstants.GameVersion, and when
  that is missing it RUNS tools/publish.ps1 first (owner, 2026-10-14: "deploy" alone must be enough).
  -Republish forces a fresh publish even when the files are there.

  On the server, in order:
    1. upload the zip + APK to ~/deploy and unpack the zip there (the live folder is untouched so far)
    2. drop `shutdown <Minutes>` into ~/game/server-command.txt — the game runs it as if an admin typed
       `/server shutdown <Minutes>`: the same countdown banner (every second for the last minute) and
       the same last save of every character. Then wait for the service to go down by itself.
    3. back up game.db (+ -wal/-shm) to ~/backups/predeploy-<old>-to-<new>-<time>.db
    4. rsync the new build over ~/game — NEVER the live state: game.db*, owner.txt, registration.txt,
       debug-config.json, admin-only.flag, apk-path.txt, apk/
    5. copy the APK into ~/game/apk/ and the Windows zip into ~/game/pc/ (each keeps the newest 3; BL-360),
       `systemctl start game`, and wait for the
       `L2Clone server v<new> starting.` line. No line = the deploy reports FAILED.

  🔑 A version that crosses the CHANGELOG's "Last game.db reset" REFUSES unless -ResetDb is given; with
  it, the old game.db is MOVED into ~/backups (not deleted) and the server starts on a fresh one.

.EXAMPLE
  pwsh tools/deploy.ps1                  # server + APK + Windows zip, 1-minute countdown
  pwsh tools/deploy.ps1 -Minutes 10      # a 10-minute warning
  pwsh tools/deploy.ps1 -Minutes 0       # no countdown (nobody online)
  pwsh tools/deploy.ps1 -NoApk           # server only
  pwsh tools/deploy.ps1 -NoServer        # clients only: no restart at all
  pwsh tools/deploy.ps1 -NoPc            # skip the Windows test client (BL-360)
#>
[CmdletBinding()]
param(
    [int]$Minutes = 1,
    [switch]$NoApk,
    # Skip the Windows test client (BL-360), served at /pc like the APK at /apk.
    [switch]$NoPc,
    [switch]$NoServer,
    [switch]$ResetDb,
    [switch]$Republish,
    [string]$Target = "ubuntu@vardelar.duckdns.org",
    [string]$Key = (Join-Path $HOME ".ssh\oracle.key")
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$builds = Join-Path $repo "builds"

$version = (Select-String -Path (Join-Path $repo "Game.Shared\GameConstants.cs") `
    -Pattern 'GameVersion\s*=\s*"([^"]+)"' | Select-Object -First 1).Matches[0].Groups[1].Value
$zip = Join-Path $builds "Game.Server-$version.zip"
$apk = Join-Path $builds "L2Clone-$version.apk"
$pc  = Join-Path $builds "Vardelar-Windows-$version.zip"
# Publish whatever this version is missing (or everything, with -Republish) — "deploy" alone is enough.
$needServer = -not $NoServer -and ($Republish -or -not (Test-Path $zip))
$needApk    = -not $NoApk    -and ($Republish -or -not (Test-Path $apk))
$needPc     = -not $NoPc     -and ($Republish -or -not (Test-Path $pc))
if ($needServer -or $needApk -or $needPc) {
    $pub = @{}
    if ($needApk) { $pub.Apk = $true }
    if ($needPc)  { $pub.Windows = $true }
    if (-not $needServer) { $pub.NoServer = $true }
    Write-Host "builds/ lacks $version ($(if ($needServer) { 'server ' })$(if ($needApk) { 'APK ' })$(if ($needPc) { 'Windows' })) — publishing first." -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot "publish.ps1") @pub
}
$files = @()
if (-not $NoServer) { if (-not (Test-Path $zip)) { throw "Publish produced no $zip" }; $files += $zip }
if (-not $NoApk)    { if (-not (Test-Path $apk)) { throw "Publish produced no $apk" }; $files += $apk }
if (-not $NoPc)     { if (-not (Test-Path $pc))  { throw "Publish produced no $pc" };  $files += $pc }
if ($files.Count -eq 0) { throw "Nothing to deploy (-NoServer, -NoApk and -NoPc)." }

$ssh = @("-i", $Key, "-o", "ConnectTimeout=20", "-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=accept-new")

# ----- the reset guard: which version is live, and does the jump cross the last game.db reset? ----------
$resetAt = (Select-String -Path (Join-Path $repo "docs\CHANGELOG.md") `
    -Pattern 'Last `game\.db` reset: ([0-9.]+)' | Select-Object -First 1).Matches[0].Groups[1].Value
$live = (& ssh @ssh $Target "cat ~/game/VERSION 2>/dev/null || journalctl -u game --no-pager 2>/dev/null | grep -o 'L2Clone server v[0-9.]*[0-9]' | tail -1 | sed 's/.*v//'")
$live = "$live".Trim()
Write-Host "Deploying $version to $Target (live: $(if ($live) { $live } else { 'unknown' }); last db reset: $resetAt)" -ForegroundColor Cyan
$crossesReset = $false
if (-not $NoServer) {
    $crossesReset = (-not $live) -or (([version]$live -lt [version]$resetAt) -and ([version]$version -ge [version]$resetAt))
    if ($crossesReset -and -not $ResetDb) {
        throw "Live $(if ($live) { $live } else { '(unknown)' }) -> $version crosses the game.db reset at $resetAt. " +
              "Re-run with -ResetDb to start on a fresh database (the old one is moved to ~/backups, not deleted)."
    }
}

# ----- upload ---------------------------------------------------------------------------------------------
& ssh @ssh $Target "mkdir -p ~/deploy ~/backups"
& scp @ssh @files "${Target}:deploy/"
if ($LASTEXITCODE -ne 0) { throw "upload failed" }

# ----- the server half: one bash script, written with LF (a CR would break every line of it) -------------
$remote = @'
set -euo pipefail
VER="$1"; MIN="$2"; SERVER="$3"; APK="$4"; RESET="$5"; LIVE="${6:-unknown}"; PC="${7:-0}"
G=~/game; D=~/deploy

if [ "$APK" = 1 ]; then
  mkdir -p "$G/apk"
  cp "$D/L2Clone-$VER.apk" "$G/apk/"
  { ls "$G"/apk/*.apk 2>/dev/null || true; } | sort -V | head -n -3 | xargs -r rm --
  rm -f "$D/L2Clone-$VER.apk"
  echo "APK $VER is served by /apk."
fi
if [ "$PC" = 1 ]; then
  mkdir -p "$G/pc"
  cp "$D/Vardelar-Windows-$VER.zip" "$G/pc/"
  { ls "$G"/pc/*.zip 2>/dev/null || true; } | sort -V | head -n -3 | xargs -r rm --
  rm -f "$D/Vardelar-Windows-$VER.zip"
  echo "Windows client $VER is served by /pc."
fi
[ "$SERVER" = 1 ] || exit 0

rm -rf "$D/$VER"; mkdir -p "$D/$VER"
unzip -q -o "$D/Game.Server-$VER.zip" -d "$D/$VER"

if systemctl is-active --quiet game; then
  echo "Countdown: /server shutdown $MIN ..."
  printf 'shutdown %s\n' "$MIN" > "$G/server-command.txt.tmp"
  mv "$G/server-command.txt.tmp" "$G/server-command.txt"     # atomic: the server never reads half a line
  limit=$(( MIN * 60 + 120 )); waited=0
  while systemctl is-active --quiet game; do
    sleep 5; waited=$(( waited + 5 ))
    if [ "$waited" -ge "$limit" ]; then
      rm -f "$G/server-command.txt"
      echo "FAILED: the server did not stop within ${limit}s. Nothing was changed; it is still running."; exit 1
    fi
  done
  echo "Server stopped after ${waited}s (everyone saved)."
else
  echo "Server was not running."
fi

TS=$(date +%Y%m%d-%H%M%S)
for f in game.db game.db-wal game.db-shm; do
  [ -f "$G/$f" ] || continue
  if [ "$RESET" = 1 ]; then mv "$G/$f" ~/backups/"reset-$LIVE-to-$VER-$TS.${f#game.}"
  else cp "$G/$f" ~/backups/"predeploy-$LIVE-to-$VER-$TS.${f#game.}"; fi
done
{ ls ~/backups/predeploy-*.db 2>/dev/null || true; } | sort | head -n -10 | sed 's/\.db$//' | while read -r b; do rm -f "$b".db "$b".db-wal "$b".db-shm; done

rsync -a --exclude='game.db*' --exclude=owner.txt --exclude=registration.txt --exclude=debug-config.json \
      --exclude=admin-only.flag --exclude=apk-path.txt --exclude='apk/' --exclude='pc/' --exclude='server-command.txt*' --exclude=VERSION \
      "$D/$VER/" "$G/"
rm -rf "$D/$VER" "$D/Game.Server-$VER.zip"

SINCE=$(date '+%Y-%m-%d %H:%M:%S')
sudo systemctl start game
for i in $(seq 1 30); do
  if journalctl -u game --since "$SINCE" --no-pager | grep -q "L2Clone server v$VER starting"; then
    echo "$VER" > "$G/VERSION"; echo "DEPLOYED: server v$VER is up."; exit 0
  fi
  sleep 1
done
echo "FAILED: no 'L2Clone server v$VER starting' line within 30s. Last log:"
journalctl -u game -n 25 --no-pager; exit 1
'@ -replace "`r`n", "`n"

$tmp = Join-Path ([IO.Path]::GetTempPath()) "deploy-remote.sh"
[IO.File]::WriteAllText($tmp, $remote, (New-Object Text.UTF8Encoding $false))
& scp @ssh $tmp "${Target}:deploy/deploy-remote.sh"
if ($LASTEXITCODE -ne 0) { throw "upload of the remote script failed" }

& ssh @ssh $Target "bash ~/deploy/deploy-remote.sh $version $Minutes $([int](-not $NoServer)) $([int](-not $NoApk)) $([int]($crossesReset -and $ResetDb.IsPresent)) $(if ($live) { $live } else { 'unknown' }) $([int](-not $NoPc))"
if ($LASTEXITCODE -ne 0) { throw "deploy FAILED on the server (see above)" }
Write-Host "Deploy of $version finished." -ForegroundColor Green
