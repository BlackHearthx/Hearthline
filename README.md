# Hearthline

![What Hearthline adds](https://cdn.jsdelivr.net/gh/BlackHearthx/Hearthline@main/docs/tutorial/tutorial_00_overview.png)

**Breed stronger livestock, steal wild cubs, and run a clearer yard** — one livestock mod for Valheim.

By **BlackHearthx**.

> Feed a favorite → roll for stronger young. Spot a wild cub → **Z** to steal and carry it home. Hover the pen to see bond, pregnancy, and growth.

**Do not install with** BreedingUpgrades or Procreation Plus (same star systems).

---

## What you get

| Feature | What it means in play |
| --- | --- |
| **Stronger young** | Babies and eggs can be born **+1★** above the parent |
| **Favorite meals** | Right food → much higher star chance while still fed |
| **Steal and carry** | **Z** grabs wild young (or carries your tamed animals) |
| **Yard hover** | Bond, expecting timer, pen full, cub growth — at a glance |
| **Mate draw** | Fed, calm adults walk to a partner and stay close so breeding starts sooner |
| **While you are away** | Tamed pens keep breeding up to the cap. Wild herds do not catch up |
| **Hare livestock** | Tame and breed Mistlands hares, with small kits that grow |
| **Wild herds** | Wild adults can breed while that area is loaded; cubs stay wild |

Star cap is **2★**. For higher stars you need [CLLC](https://thunderstore.io/c/valheim/p/Smoothbrain/CreatureLevelAndLootControl/).

---

## How to play

### 1. Breed for stars

Keep two fed adults of the same species. When a baby (or egg) is born, there is a chance it is **one star above** the parent.

Tamed pairs keep breeding while you are away. The pen still stops at its cap. They need a partner nearby, and food on the ground once they get hungry. A pregnancy that already started still finishes. Wild herds do not catch up when that area was unloaded.

Wild adults you are taming also keep filling the tame bar while you are away, if their food is still on the ground. No food nearby means that bar does not move.

| Situation | Chance |
| --- | --- |
| Normal birth | **5%** |
| Last meal was a **favorite**, and they are still fed | **25%** |

![1. Stars and hover](https://cdn.jsdelivr.net/gh/BlackHearthx/Hearthline@main/docs/tutorial/tutorial_01_hover.png)

### 2. Feed favorites

Favorites must be foods that animal **already eats**. Hearthline adds food support for hares.

| Animal | Favorite (default) |
| --- | --- |
| Boar | Carrot |
| Wolf | Raw Meat |
| Lox | Barley |
| Hen | Barley |
| Hare | Carrot |
| Asksvin | Vineberry |
| Moose | Lingonberry |

![2. Favorite meals](https://cdn.jsdelivr.net/gh/BlackHearthx/Hearthline@main/docs/tutorial/tutorial_02_favorite.png)

### 3. Steal and carry (**Z**)

| Key | Action |
| --- | --- |
| **Z** | Steal a **wild** young animal, or pick up / put down young and **tamed** adults |
| **E** | Pet on the animal. On the saddle, **E** rides. Carry does not replace either |

Wild steal: costs stamina, has a cooldown, and can aggro nearby wild adults of the same species.  
If you take damage while carrying, the animal drops. You cannot attack while carrying.

![3. Steal and carry](https://cdn.jsdelivr.net/gh/BlackHearthx/Hearthline@main/docs/tutorial/tutorial_03_steal_carry.png)

### 4. Read the yard

Hover livestock to see:

- **Bond** — love progress toward breeding  
- **Expecting** — countdown to birth  
- **Pen full** — no more breeding in range  
- **Growing** — cub maturity  
- Favorite tip when it applies  

**Mate draw:** fed, calm tamed adults walk over to a same-species partner and stay beside it until one is expecting (not while following you, hungry, alert, or when the pen is full).

### 5. Wild herds

Wild adults with offspring can breed while that area is loaded; babies stay wild. Soft pregnancy chance and occasional “already has a cub” spawns keep the forest from flooding. Leaving and coming back does not fast-forward wild births.

![4. Wild herds](https://cdn.jsdelivr.net/gh/BlackHearthx/Hearthline@main/docs/tutorial/tutorial_04_wild_herd.png)

---

### 6. Raise hares

Mistlands hares can now be tamed and bred without another mod. Feed carrots, turnips or Jotun Puffs; optional Apple items are accepted when installed. Carrot is their favorite meal.

Tamed hares support Follow/Stay and Z carry. Kits can be stolen/carried and grow into adults while keeping their stars. They have no drops and are protected from the butcher knife. Wild adults flee threats.

Default timings are 25 minutes to tame, 5 minutes fed per meal, 33 minutes 20 seconds to grow and 90 seconds of pregnancy. The pen holds 7 adults/kits within 10 meters. Configure hare food and timings under **10. Hares**; restart the world when enabling/disabling this feature. Server and clients need the same Hearthline version.

If TameableHares is installed, it controls the adult hares and their food/timings. Hearthline supplies its normal star and yard features. Existing kits from that mod remain supported if you remove it after upgrading.

---

## Como usar (PT-BR)

**Hearthline** melhora o gado: filhotes mais fortes, comida favorita, roubar cria selvagem e ver o curral com clareza.

| O que muda | Em jogo |
| --- | --- |
| **Lebres** | Domar e reproduzir lebres; filhotes crescem mantendo as estrelas. Cenoura é a favorita |
| **Filhotes ★** | Chance de nascer com **+1★** (5% normal · **25%** com comida favorita ainda alimentado) |
| **Cria longe** | Curral domado segue até encher. Manada selvagem não adianta quando você volta |
| **Doma longe** | Solta a comida do adulto selvagem e sai. A barra continua. Sem comida perto, não anda |
| **Favoritas** | Javali cenoura · Lobo carne crua · Lox/galinha cevada · Asksvin vinhaberry · Alce lingonberry |
| **Z** | Rouba filhote selvagem ou carrega cria / adulto **domado**. No corpo, **E** acaricia. Na sela, **E** monta |
| **Hover** | Vínculo, gravidez com contagem, curral cheio, crescimento |
| **Mate draw** | Adultos calmos e alimentados vão até o parceiro e ficam juntos |
| **Manadas** | Selvagens podem procriar; filhote nasce selvagem |

Teto **2★** (acima disso precisa CLLC). Não use com BreedingUpgrades ou Procreation Plus.

---

## Requirements

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)

## Config

After first launch: `BepInEx/config/blackhearthx.hearthline.cfg`

## Identity

| | |
| --- | --- |
| Package | `blackhearthx-Hearthline` |
| GUID | `blackhearthx.hearthline` |

Thunderstore page README: [`thunderstore/README.md`](thunderstore/README.md) (same player guide; used by the package zip).

Hare livestock inspired by nick008's TameableHares, implemented independently in Hearthline.
