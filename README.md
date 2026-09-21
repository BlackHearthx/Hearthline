# Hearthline

Dois lugares, um código:

| Onde | O que é |
|---|---|
| **Este projeto Cursor** | Repositório do Hearthline (raiz = o mod) |
| **`D:\Valheim Mods\Hearthline`** | Cópia no disco dos teus mods, ao lado de Hearthwife / Hearthwait |

Nesta VM não dá para gravar no `D:`. No Windows, na raiz deste repo:

```powershell
.\Copy-To-ValheimMods.ps1
```

Mod **BlackHearthx** para Valheim: parto com chance de +1 estrela, comida favorita, hover de vínculo/gravidez/crescimento, mate draw, **manada selvagem** (com freio), **roubar/carregar com Z** (E fica pet vanilla).

## O que já existia (pesquisa, não inventei)

| Mod | Autor | O que faz | Versão conferida |
|---|---|---|---|
| [BreedingUpgrades](https://thunderstore.io/c/valheim/p/Dumba/BreedingUpgrades/) | Dumba | Chance global (default 5%) de +1 estrela em nascimento e ovo. Transpiler em `Character.SetLevel` + prefix em `ItemDrop.SetQuality`. README do autor: incompatível com Star Level Systems. | 1.0.2 (zip que você mandou) |
| [Procreation Plus](https://thunderstore.io/c/valheim/p/MaxFoxGaming/Procreation_Plus/) | MaxFoxGaming | Chance **por prefab**. Sobe temporariamente `Procreation.m_minOffspringLevel` no Prefix de `Procreate` e restaura no Postfix. | 1.2.1 (fonte no Thunderstore) / 1.2.2 na página |

Vanilla (confirmado no decompile público de `Procreation.Procreate`, o mesmo bloco usado por HogRiders / yggdrahsbetterhorse):

```
character.SetLevel(Mathf.Max(m_minOffspringLevel, parent.GetLevel()));
// ovos:
itemDrop.SetQuality(Mathf.Max(m_minOffspringLevel, parent.GetLevel()));
```

Hearthline usa o gancho do **Procreation Plus** (`m_minOffspringLevel`), não o transpiler do Dumba: um único patch cobre javali/lobo/lox/alce **e** ovos de galinha/asksvin, sem desviar `SetLevel`.

Níveis vanilla: 0★ = level 1, 1★ = level 2, 2★ = level 3.

## Manadas selvagens — o que o vanilla permite

**Confirmado no `Procreate` vanilla:**

1. O método **sai imediatamente** se o adulto não estiver tamed (guard atual: `m_tameable.IsTamed()`). Selvagem **não** procria sem patch.
2. O bebê **não é inventado**: nasce de `Procreation.m_offspring`. Prefabs com `Growup` listados no Valheim Tools: `Boar_piggy`, `Wolf_cub`, `Lox_Calf`, `Chicken`, `Asksvin_hatchling`, `Moose_calf`. Ovos: `ChickenEgg`, `AsksvinEgg`.
3. No parto: `SetTamed(parent.IsTamed())`. Pai selvagem → filhote **selvagem**.
4. Parceiro: `SpawnSystem.GetNrOfInstances(..., procreationOnly: true)` só conta quem passa em `ReadyForProcreation()`.
5. `Tameable.IsHungry()`: compara `TameLastFeeding` com `m_fedDuration`. Sem comida no chão o timestamp é 0 → **sempre faminto**. Sem ignorar fome, manada selvagem quase nunca passa.
6. Continua valendo: não alerta, teto `m_maxCreatures`, love points, gravidez, zona carregada.

**Extra Hearthline:** chance de um adulto selvagem já aparecer com 1 cria Growup perto (`Wild Family Spawn Chance`).

## Config (`BepInEx/config/blackhearthx.hearthline.cfg`)

| Chave | Default | Efeito |
|---|---|---|
| Enable Mod | true | Liga/desliga |
| Upgrade Chance | 5% | Chance de +1 estrela |
| Max Star Level | 2 | Teto vanilla. Acima de 2 só com CLLC |
| Lock Configuration | true | ServerSync |
| Enable Debug Logging | false | Log verbose (nascimentos) |
| Debug Fast Breeding | false | TEST ONLY — pregnancy/love turbo |
| Chance Per Prefab | (vazio) | `Boar:8,Wolf:5,...` |
| Favorite Foods | `Boar:Carrot;…` | Só IDs da lista vanilla de comida |
| Favorite Upgrade Chance | 25% | Chance de +1★ com refeição favorita |
| Show Yard Hover | true | Bond, Expecting countdown, pen full, Growing %, favorite note |
| Enable Mate Draw | true | Adultos fed/calmos andam até o parceiro |
| Wild Herds Can Procreate | true | Selvagens com `m_offspring` procriam |
| Wild Herds Ignore Hunger | true | Ignora fome só no `Procreate` selvagem |
| Wild Herd Pregnancy Chance | 30% | Freio em `MakePregnant` selvagem |
| Wild Family Spawn Chance | 25% | Adulto selvagem novo pode já ter 1 cria |
| Claim Wild Young | true | **Z** no filhote selvagem → amansa e carrega |
| Steal Alerts Herd | true | Roubo alerta adultos da espécie |
| Steal Alert Range | 20 | Metros |
| Steal Cooldown Seconds | 120 | Cooldown após roubo |
| Steal Stamina Cost | 40 | Custo de stamina |
| Enable Cub Carry | true | **Z** pega/larga cria e adultos tamed |
| Carry Range | 5 | Distância ao soltar |
| Drop On Damage | true | Dano derruba o animal |
| Block Attack While Carrying | true | Sem ataque enquanto carrega |

Não use junto com BreedingUpgrades ou Procreation Plus.

## Roubar e carregar

1. Manada selvagem pode gerar filhote selvagem (freio 30% + teto vanilla).
2. **Z** no filhote → rouba (se selvagem) e carrega; em adulto tamed só carrega.
3. **Z** de novo → solta.
4. **E** permanece pet / interact vanilla.

## Compilar

```bat
cd /d D:\Valheim Mods\Hearthline
dotnet build Hearthline.sln -c Release
```

Deploy de teste: Thunderstore profile **Mod tests** via `Directory.Build.props.user` → `MOD_DEPLOYPATH`.

Dependência Thunderstore: `denikson-BepInExPack_Valheim-5.4.2350`

ServerSync entra compilado no mesmo DLL (fonte oficial [blaxxun-boop/ServerSync](https://github.com/blaxxun-boop/ServerSync), MIT).

## Testes sem o jogo

```bash
dotnet test src/Hearthline.Tests/Hearthline.Tests.csproj
```

## Ícone Thunderstore

Use o template Hearthwife + selo oficial `blackhearth_mark_official.png` no canto inferior esquerdo antes de publicar. Não redesenhar o selo.
