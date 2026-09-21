# Pack Thunderstore zip (Hearthline)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root "dist\thunderstore"
$dll = Join-Path $root "bin\Hearthline\BlackHearthx.Hearthline.dll"
$manifestPath = Join-Path $root "thunderstore\manifest.json"
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$version = $manifest.version_number
$zip = Join-Path $root "dist\blackhearthx-Hearthline-$version.zip"

if (-not (Test-Path $dll)) {
  throw "Build first: dotnet build src\Hearthline\Hearthline.csproj -c Release"
}

if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Force -Path $dist | Out-Null

Copy-Item (Join-Path $root "thunderstore\icon.png") (Join-Path $dist "icon.png") -Force
Copy-Item (Join-Path $root "thunderstore\README.md") (Join-Path $dist "README.md") -Force
Copy-Item (Join-Path $root "thunderstore\CHANGELOG.md") (Join-Path $dist "CHANGELOG.md") -Force
Copy-Item $manifestPath (Join-Path $dist "manifest.json") -Force
Copy-Item $dll (Join-Path $dist "BlackHearthx.Hearthline.dll") -Force

$tut = Join-Path $root "docs\tutorial"
if (Test-Path $tut) {
  New-Item -ItemType Directory -Force -Path (Join-Path $dist "docs\tutorial") | Out-Null
  Copy-Item (Join-Path $tut "*") (Join-Path $dist "docs\tutorial") -Force
}

New-Item -ItemType Directory -Force -Path (Join-Path $root "dist") | Out-Null
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $dist "*") -DestinationPath $zip -Force
Write-Host "Ready: $zip"
Get-Item $zip | Format-List FullName, Length, LastWriteTime
