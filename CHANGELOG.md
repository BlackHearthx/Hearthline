## 1.0.0
- First stable release for Thunderstore.
- Breed ★ chance (5% default, 25% on favorite meal), steal/carry on Z, wild herds, yard hover, mate draw.
- Production defaults: Max Star Level 2, debug/fast breed off.

## 0.3.14
- Debug: star roll logs only on actual birth ticks (stops Fast Breeding flood).

## 0.3.13
- Debug Fast Breeding: test toggle for love need 1, ~5s pregnancy, ~8m partner range, Procreate every 2s.

## 0.3.12
- Hover Expecting: real due countdown from `s_pregnant` + `m_pregnancyDuration` (ZNet time). Shows `any moment` when due.

## 0.3.11

- Hover: `Pen full — no breeding` when vanilla `m_maxCreatures` is reached in `m_totalCheckRange`.
- Mate draw: fed/calm tamed adults gently walk toward a same-species partner (config under Hover). Skips hungry, alert, pregnant, young, and full pens.

## 0.3.10

- Carry / steal on **Z** (E remains vanilla pet).
- Favorite hover: `Favorite meal — better young chance` (clearer Valheim-style wording).

## 0.3.9

- Carry / steal: **Left Alt + E** (E alone is vanilla pet again).
- Steal aggro: no longer sets Hunt Player — Alert + target + Theif aggravate only (closer to vanilla chase drop-off, still aggressive).

## 0.3.8

- Anti-abuse: wild steal costs stamina (default 40) and has a cooldown (default 120 s). Already-tamed carry unchanged.
- Carry: animal hitboxes stay active (player↔carried collision ignored); taking damage while carrying drops the animal (config Drop On Damage).
- Wild family: wait for ZDO ownership after Awake (console `spawn Boar` often failed the old instant IsOwner check).
- Steal HUD: Valheim-style `You took the young` only; herd aggro has no second Center message.
- Carry: cannot attack (melee/bow) while carrying — config Block Attack While Carrying (default on).

## 0.3.7

- Stealing a wild cub alerts nearby wild adults of the same species (Alert, hunt player, `AggravatedReason.Theif`). Config: Steal Alerts Herd / Steal Alert Range (default 20 m).

## 0.3.6

- Carry tamed adults (Tameable livestock) with E as well as young. Wild adults still refused; young steal unchanged.

## 0.3.5

- Wild family spawn: 25% chance (config) that a wild adult with live Growup offspring already has one wild cub nearby on first ZDO appear. Eggs skipped; one roll per creature.

## 0.3.4

- Wild herds: Procreate guard is now `m_tameable.IsTamed()` (current Valheim); transpiler updated (was matching old `m_character.IsTamed()` and logging "found 0").

## 0.3.3

- Steal/carry: vanilla cubs (`Boar_piggy`, `Wolf_cub`, `Lox_Calf`, …) have **Growup + AnimalAI and no Tameable** — steal no longer requires `Tameable`; uses `Character.SetTamed` after ownership claim. Hover text built for cubs that return empty vanilla hover.
- Reworked steal/carry after decompiling vanilla `Tameable` / `Player`: wild Interact was a no-op; `Tame()` needs ZDO ownership first; hover now redirects to the cub under the crosshair (RaycastAll, Offspring Portal pattern).
- E only via `Player.Interact` (priority above Offspring Portal follow). Put down with E while carrying.

## 0.3.2

- Carry / steal young with **E** only (removed Left Alt + E).
- Broader young detection: Growup **or** tameable without Procreation (fixes false "Only young").
- Put down with E while carrying.

## 0.3.1

- Wild herds on by default, with an extra wild-only `MakePregnant` chance (default 30%) so herds do not explode. Vanilla `m_maxCreatures` still applies.
- Steal wild Growup young with E (`Tameable.Tame` via AccessTools).
- Carry tamed Growup young with Left Alt + E (not adults — use CreatureCarry for those). Alt+E on a wild cub steals then picks up.

## 0.3.0

- Per-prefab star chance overrides (`Boar:8,Wolf:5`).
- Favorite meals from vanilla consume lists; last favorite while fed uses a higher star chance.
- Hover: bond (lovePoints), expecting (pregnant ZDO), cub growth (GetTimeSinceSpawned). No pregnancy percent.

## 0.2.0

- Optional wild herds: first `IsTamed` check in `Procreation.Procreate` can pass for untamed adults that already have `m_offspring`.
- `ReadyForProcreation` postfix so wild partners count; hunger ignore only during `Procreate` for those adults.
- Cub still uses vanilla `SetTamed(parent.IsTamed())`.

## 0.1.0

- First slice: Harmony prefix/postfix on `Procreation.Procreate` raising `m_minOffspringLevel` for one call, then restoring it.
- Global upgrade chance (default 5%) and 2-star cap (CLLC for higher).
- ServerSync lock for dedicated servers.
