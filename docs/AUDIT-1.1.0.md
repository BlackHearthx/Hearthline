# Hearthline 1.1.0 audit

Audited on 2026-10-05 against the installed Valheim managed assemblies and Unity 6000.0.61f1.

## Corrections before release

- Validate the Boar component templates before changing Hare.
- Preserve the Hare model eye reference; avoid copying callback delegates from templates.
- Keep cloned kits on prey AI even when the source adult comes from TameableHares, preventing adult feeding behavior on kits.
- Rebuild independent food lists; ignore blank, duplicate and absent optional food entries.
- Refresh food/configuration together rather than performing component lookups on every AI frame.
- Apply synchronized growth time when a kit's growth update runs, while respecting TameableHares ownership of its own kits.
- Count both offspring identifiers toward the pen cap and recognize both as Hare offspring during guardian reactions.

## Evidence

- Release build: successful, zero errors. Existing nullable/attribute warnings remain.
- Unit/regression suite: 76 passed, zero failed.
- Isolated Unity/Mono audit: 22 checks passed using native game types and synthetic inactive prefabs.
- Native checks cover AI delegate resolution, one Character/AI per adult, health/eye preservation, offspring registration, growth target, lack of kit breeding/drops/taming, inverted pregnancy threshold, idempotent registration, unchanged Boar components, food parsing/isolation, synchronized growth, legacy registration, patched pen counts in both directions, external-mod ownership and disabled-feature behavior.
- All five Thunderstore README image URLs returned HTTP 200 with image content types.

The isolated checks do not simulate a complete loaded Valheim world, network ownership transfers, actual taming/feeding animations, butcher hits or multiplayer gameplay. No full gameplay session was performed as part of this audit.

## Repeating the isolated audit

Build Hearthline first. Create a Unity 6000.0.61f1 project under `<workspace>/_checks/HearthlineAuditUnity`, with an empty package manifest. Copy `tools/UnityAudit/HareAudit.cs` into its `Assets/Editor` folder. Set `HEARTHLINE_AUDIT_WORKSPACE` to the parent folder containing the Hearthline repository and `VALHEIM_INSTALL` to the game installation when using non-default paths.

Run Unity in batch mode with `-nographics -projectPath <project> -executeMethod HareAudit.Run -logFile <project>/editor.log`. The runner writes `result.txt` and exits with status 0 on success. The fixtures and configuration stay inside the audit project; no game world is loaded or edited.
