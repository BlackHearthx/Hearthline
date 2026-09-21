# Valheim DB Dumper (BlackHearthx reference)

Zip: N1h1lius-ValheimDBDumper-1.0.0.zip (Downloads + Hearthline/branding/refs/)
GitHub: https://github.com/n1h1lius/Valheim-DB-Dumper

## What it gives us
- /textures/ creature skin albedos
- /models/ .obj meshes
- /icons/ UI icons
- /data/creatures.json stats/drops
- Dashboard: Desktop\ValheimDB_Export\index.html

## When to use
Cover art / accurate Valheim creature look, prefab facts — dump from live game instead of guessing.

## How (Mod tests only)
1. Copy ValheimDBDumper.dll into Mod tests plugins (temp OK)
2. Load a world with character
3. F5: dumpdb creatures
   Lighter: dumpdb creatures --no-json --no-prefab
4. Assets: Desktop\ValheimDB_Export\ (or cfg ExportFolder)

## Cover art rule
Use dump/wiki as appearance reference only. Final Thunderstore icons stay painted BlackHearth/Hearthwife style — never raw screenshot collage.
