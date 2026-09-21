#Requires -Version 5.1
# Installs this Hearthline project into D:\Valheim Mods\Hearthline
# (next to Hearthwife / Hearthwait). Safe to re-run.

$ErrorActionPreference = "Stop"
$src = $PSScriptRoot
$dest = "D:\Valheim Mods\Hearthline"
$sealSrc = "D:\Valheim Mods\Hearthwife\branding\blackhearth_mark_official.png"
$sealDest = Join-Path $dest "branding\blackhearth_mark_official.png"

if (-not (Test-Path "D:\Valheim Mods")) {
    throw "D:\Valheim Mods not found. Create that folder (or adjust `$dest) first."
}

New-Item -ItemType Directory -Force -Path $dest | Out-Null

robocopy $src $dest /E /XD bin obj .vs .git agent-tools uploads /XF *.user /NFL /NDL /NJH /NJS /nc /ns /np
if ($LASTEXITCODE -ge 8) { throw "robocopy failed: $LASTEXITCODE" }

if ((Test-Path $sealSrc) -and -not (Test-Path $sealDest)) {
    New-Item -ItemType Directory -Force -Path (Split-Path $sealDest) | Out-Null
    Copy-Item $sealSrc $sealDest -Force
    Write-Host "Copied official BlackHearth seal from Hearthwife."
}

Write-Host ""
Write-Host "Done. Local mod folder: $dest"
Write-Host "Open: $dest\Hearthline.sln"
Write-Host "Build:  cd `"$dest`"; dotnet build Hearthline.sln -c Release"
