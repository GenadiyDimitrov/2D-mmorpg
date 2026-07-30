# =============================================================================
#  publish-build.ps1 — put the current version's APK + server publish on the
#  `builds` BRANCH, so they can be downloaded from a phone browser anywhere.
#
#  WHY A BRANCH AND NOT builds/ ON Gena:
#  git stores every version of every file forever, and an APK is already
#  compressed, so it cannot be packed. At ~41 MB (APK) + ~15 MB (server zip)
#  per release, and several releases on a busy day, committing these to the
#  working branch would add more permanent history in a week than the entire
#  source repo has accumulated in months — and it could only be undone by
#  rewriting history.
#
#  So `builds` is an ORPHAN branch that is REWRITTEN, never appended to: each
#  publish creates one parentless commit holding the last $Keep versions and
#  force-pushes it. The blobs it drops become unreachable and GitHub garbage
#  collects them, so the remote stays roughly ONE generation of builds in size
#  instead of all of them.
#
#  It touches neither the working tree nor the index: the commit is assembled
#  with git plumbing against a temporary index file. Safe to run any time.
#
#  Usage (from anywhere):   pwsh tools/publish-build.ps1
#         keep more/fewer:  pwsh tools/publish-build.ps1 -Keep 5
#         see what it'd do: pwsh tools/publish-build.ps1 -WhatIf
# =============================================================================
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    # How many VERSIONS to keep on the branch (the newest N by version number).
    [int]$Keep = 3,
    # Publish something other than the version in GameConstants (a re-publish).
    [string]$Version,
    # Build the commit but do not push it.
    [switch]$NoPush
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    # ----- which version, and are its artifacts actually on disk? -------------
    if (-not $Version) {
        $m = Select-String -Path 'Game.Shared/GameConstants.cs' -Pattern 'GameVersion\s*=\s*"([^"]+)"'
        if (-not $m) { throw "Could not read GameVersion from Game.Shared/GameConstants.cs." }
        $Version = $m.Matches[0].Groups[1].Value
    }

    $artifacts = @(
        @{ Path = "builds/L2Clone-$Version.apk";        Name = "L2Clone-$Version.apk" }
        @{ Path = "builds/Game.Server-$Version.zip";    Name = "Game.Server-$Version.zip" }
    )
    foreach ($a in $artifacts) {
        if (-not (Test-Path $a.Path)) {
            throw "Missing $($a.Path). Build the APK and publish the server first — a half-published " +
                  "build branch is worse than none, because the version handshake refuses a mismatched pair."
        }
    }

    # ----- assemble the tree in a TEMPORARY index (working tree untouched) ----
    $env:GIT_INDEX_FILE = Join-Path ([System.IO.Path]::GetTempPath()) "l2clone-build-index-$([guid]::NewGuid()).idx"
    try {
        # Carry over the versions we are keeping from the previous branch state, reusing their
        # existing blobs (no re-upload of a file that has not changed).
        $carried = @()
        $existing = & git ls-tree refs/heads/builds 2>$null
        if ($LASTEXITCODE -eq 0 -and $existing) {
            # Newest $Keep versions, this one included — anything older falls off the branch.
            $versions = @($Version) + @(
                $existing | ForEach-Object {
                    if ($_ -match '\t(?:L2Clone|Game\.Server)-(\d+\.\d+\.\d+)\.(?:apk|zip)$') { $Matches[1] }
                }
            )
            $versions = $versions | Sort-Object -Unique -Property @{ Expression = { [version]$_ } } -Descending |
                        Select-Object -First $Keep

            foreach ($line in $existing) {
                if ($line -notmatch '^(\d{6})\s+blob\s+([0-9a-f]{40})\t(.+)$') { continue }
                $mode = $Matches[1]; $sha = $Matches[2]; $name = $Matches[3]
                if ($name -notmatch '-(\d+\.\d+\.\d+)\.(?:apk|zip)$') { continue }
                $v = $Matches[1]
                if ($v -eq $Version -or $versions -notcontains $v) { continue }   # replaced, or aged out
                & git update-index --add --cacheinfo "$mode,$sha,$name" | Out-Null
                $carried += $name
            }
        }

        # This version's artifacts.
        foreach ($a in $artifacts) {
            $sha = & git hash-object -w -- $a.Path
            if ($LASTEXITCODE -ne 0) { throw "git hash-object failed for $($a.Path)." }
            & git update-index --add --cacheinfo "100644,$sha,$($a.Name)" | Out-Null
            $carried += $a.Name
        }

        # A README so the branch explains itself to whoever lands on it (usually the owner, on a phone).
        $origin = (& git remote get-url origin) -replace '\.git$', ''
        $rows = ($carried | Sort-Object -Descending | ForEach-Object { "- [$_]($origin/raw/builds/$_)" }) -join "`n"
        $readme = @"
# Builds

Downloadable builds of the game. **This branch is REWRITTEN on every publish** — it is not history,
it is a shelf holding the newest $Keep versions. Do not merge it into anything and do not commit
source here; ``tools/publish-build.ps1`` overwrites whatever is on it.

Current version: **$Version**

$rows

## Taking a build to the phone

Both files have to go across **together**: the login handshake refuses a client whose version does
not match the server's, so a new APK on an old server (or the reverse) cannot log in.

- **APK** — download and install it over the previous one (same package id, so it upgrades in place).
- **Server** — unzip into ``/sdcard/Download/TermuxFiles/Game.Server/``, **clearing that folder
  first** so an older build's files cannot survive alongside the new ones. Start it with
  ``dotnet Game.Server.dll``. Keep the ``runtimes/`` folder next to the DLLs — the native SQLite
  library lives there and the database cannot open without it.

_Generated by tools/publish-build.ps1 on $(Get-Date -Format 'yyyy-MM-dd HH:mm')._
"@
        $tmpReadme = Join-Path ([System.IO.Path]::GetTempPath()) "l2clone-builds-readme-$([guid]::NewGuid()).md"
        Set-Content -Path $tmpReadme -Value $readme -Encoding UTF8 -NoNewline
        $sha = & git hash-object -w -- $tmpReadme
        & git update-index --add --cacheinfo "100644,$sha,README.md" | Out-Null
        Remove-Item $tmpReadme -Force

        $tree = (& git write-tree).Trim()
        if ($LASTEXITCODE -ne 0) { throw "git write-tree failed." }
    }
    finally {
        Remove-Item $env:GIT_INDEX_FILE -Force -ErrorAction SilentlyContinue
        Remove-Item Env:\GIT_INDEX_FILE -ErrorAction SilentlyContinue
    }

    # ----- one parentless commit, then replace the branch ---------------------
    # No -p: the commit has NO parent, which is what stops the branch growing.
    $commit = ($tree | & git commit-tree $tree -m "builds: $Version ($($carried.Count) files, newest $Keep versions kept)").Trim()
    if ($LASTEXITCODE -ne 0) { throw "git commit-tree failed." }

    if ($PSCmdlet.ShouldProcess("refs/heads/builds", "replace with $Version")) {
        & git update-ref refs/heads/builds $commit
        Write-Host "local branch 'builds' -> $($commit.Substring(0,8))  ($Version)"
        if (-not $NoPush) {
            # Force is CORRECT here and only here: the branch is deliberately rewritten, and it holds
            # nothing that is not reproducible from a tag plus a build.
            & git push --force origin builds
            if ($LASTEXITCODE -ne 0) { throw "push failed." }
            Write-Host "pushed. Download: $origin/tree/builds"
        }
    }
}
finally { Pop-Location }
