#Requires -Version 5.1
<#
.SYNOPSIS
  Fails if any markdown image in thunderstore/README.md is missing or not an image.
.DESCRIPTION
  Prevents the classic Thunderstore "broken image" ship: README uploaded before
  GitHub/CDN can serve the files, or relative/blob URLs that TS cannot load.
#>
param(
  [string]$ReadmePath = (Join-Path $PSScriptRoot "thunderstore\README.md"),
  [string]$RepoSlug = "BlackHearthx/Hearthline"
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path -LiteralPath $ReadmePath)) {
  throw "README not found: $ReadmePath"
}

$raw = [IO.File]::ReadAllText($ReadmePath)
$matches = [regex]::Matches($raw, '!\[([^\]]*)\]\(([^)]+)\)')
if ($matches.Count -eq 0) {
  Write-Host "No markdown images found (OK if text-only)."
  exit 0
}

$failed = @()
foreach ($m in $matches) {
  $alt = $m.Groups[1].Value
  $url = $m.Groups[2].Value.Trim()

  if ($alt -match "[\r\n]" -or $url -match "[\r\n]") {
    $failed += "NEWLINE inside image markdown: alt='$alt'"
    continue
  }
  if ($url -notmatch '^https://') {
    $failed += "Not absolute HTTPS: $url"
    continue
  }
  if ($url -match 'github\.com/.+/(blob|raw)/') {
    $failed += "Use jsDelivr or raw.githubusercontent.com, not github blob/raw page: $url"
    continue
  }
  if ($url -notmatch 'cdn\.jsdelivr\.net/gh/' -and $url -notmatch 'raw\.githubusercontent\.com/') {
    $failed += "Unexpected host (prefer jsDelivr): $url"
    continue
  }

  try {
    $resp = Invoke-WebRequest -Uri $url -Method Head -UseBasicParsing -TimeoutSec 30
    $ct = [string]$resp.Headers["Content-Type"]
    if ($resp.StatusCode -ne 200 -or $ct -notmatch '^image/') {
      $failed += "HTTP $($resp.StatusCode) Content-Type=$ct  $url"
    } else {
      Write-Host "OK  $url"
    }
  } catch {
    $failed += "FETCH FAIL  $url  $($_.Exception.Message)"
  }
}

# Local files referenced by CDN must exist in repo
$localChecks = @(
  "icon.png",
  "docs\tutorial\tutorial_01_hover.png",
  "docs\tutorial\tutorial_02_favorite.png",
  "docs\tutorial\tutorial_03_steal_carry.png",
  "docs\tutorial\tutorial_04_wild_herd.png"
)
foreach ($rel in $localChecks) {
  $p = Join-Path $PSScriptRoot $rel
  if (-not (Test-Path -LiteralPath $p)) {
    $failed += "Missing local file for README image: $rel"
  }
}

if ($failed.Count -gt 0) {
  Write-Host "`nFAILED Thunderstore README image check:" -ForegroundColor Red
  $failed | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
  Write-Host "`nPush images to $RepoSlug main, wait for CDN, re-run. Do NOT upload the TS zip yet." -ForegroundColor Yellow
  exit 1
}

Write-Host "`nAll $($matches.Count) README images OK. Safe to pack Thunderstore." -ForegroundColor Green
exit 0
