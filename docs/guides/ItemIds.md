# Item ids — the complete `/give` reference

**Generated from `ItemCatalog`** by `tools/ItemIds` — do not hand-edit; re-run
`dotnet run --project tools/ItemIds` after adding or removing an item. Every id below is a real
id the server will accept today.

**602 items.** Generated 2026-09-24.

```
/give <player> <itemId> [sellPrice] [tradable] [timed] ["name"] [enchant] [canStorePrivate] [canStoreAccount] [amount]

/give Gena mat_iron - - - - - - - 1000     # a thousand of a material, in one bag slot
```

Everything after the item id is optional and **positional**; `-` in any slot means *no opinion,
use the catalog*. See [ChatCommands.md](ChatCommands.md) for what each argument does.

**`[amount]`** defaults to 1 and is capped at 10,000. A **stackable** (materials, potions,
scrolls, quest items — the `stacks` note below) arrives as ONE bag row carrying the quantity;
**gear** cannot stack, so an amount there is that many separate rows and stops when the bag
is full (it tells you how many fit).

> 🔑 **Ids are also on the item card in game**, under the enchant line, for staff only —
> so you can read one off the thing in your bag instead of coming here.

## Weapons  (104)

### no tier (training / one-off)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `training_sword` | Training Sword | F | Common | untradable, Sword |
| `training_wand` | Training Wand | F | Common | untradable, Blunt |

### Lv 1

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `staff_t1` | Ferrite Battlestaff | F | Mythic | TwoHandedBlunt |
| `sword1h_t1` | Ferrite Blade | F | Mythic | Sword |
| `duals_t1` | Ferrite Fangs | F | Mythic | Dual |
| `sword2h_t1` | Ferrite Greatsword | F | Mythic | TwoHandedSword |
| `bow_t1` | Ferrite Longbow | F | Mythic | Bow |
| `blunt1h_t1` | Ferrite Mace | F | Mythic | Blunt |
| `blunt2h_t1` | Ferrite Maul | F | Mythic | TwoHandedBlunt |
| `wand_t1` | Ferrite Wand | F | Mythic | Blunt |
| `staff_t1_bound` | Newbie Ferrite Battlestaff | F | Mythic | untradable, TwoHandedBlunt |
| `sword1h_t1_bound` | Newbie Ferrite Blade | F | Mythic | untradable, Sword |
| `duals_t1_bound` | Newbie Ferrite Fangs | F | Mythic | untradable, Dual |
| `sword2h_t1_bound` | Newbie Ferrite Greatsword | F | Mythic | untradable, TwoHandedSword |
| `bow_t1_bound` | Newbie Ferrite Longbow | F | Mythic | untradable, Bow |
| `wand_t1_bound` | Newbie Ferrite Wand | F | Mythic | untradable, Blunt |

### Lv 20

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `staff_t20` | Electrum Battlestaff | E | Mythic | TwoHandedBlunt |
| `sword1h_t20` | Electrum Blade | E | Mythic | Sword |
| `duals_t20` | Electrum Fangs | E | Mythic | Dual |
| `sword2h_t20` | Electrum Greatsword | E | Mythic | TwoHandedSword |
| `bow_t20` | Electrum Longbow | E | Mythic | Bow |
| `blunt1h_t20` | Electrum Mace | E | Mythic | Blunt |
| `blunt2h_t20` | Electrum Maul | E | Mythic | TwoHandedBlunt |
| `wand_t20` | Electrum Wand | E | Mythic | Blunt |

### Lv 40

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `staff_t40_common` | Darksteel Battlestaff | B | Common | TwoHandedBlunt |
| `staff_t40_temp` | Darksteel Battlestaff | B | Common | untradable, TwoHandedBlunt |
| `sword1h_t40_common` | Darksteel Blade | B | Common | Sword |
| `sword1h_t40_temp` | Darksteel Blade | B | Common | untradable, Sword |
| `duals_t40_common` | Darksteel Fangs | B | Common | Dual |
| `duals_t40_temp` | Darksteel Fangs | B | Common | untradable, Dual |
| `sword2h_t40_common` | Darksteel Greatsword | B | Common | TwoHandedSword |
| `sword2h_t40_temp` | Darksteel Greatsword | B | Common | untradable, TwoHandedSword |
| `bow_t40_common` | Darksteel Longbow | B | Common | Bow |
| `bow_t40_temp` | Darksteel Longbow | B | Common | untradable, Bow |
| `blunt1h_t40_common` | Darksteel Mace | B | Common | Blunt |
| `blunt1h_t40_temp` | Darksteel Mace | B | Common | untradable, Blunt |
| `blunt2h_t40_common` | Darksteel Maul | B | Common | TwoHandedBlunt |
| `blunt2h_t40_temp` | Darksteel Maul | B | Common | untradable, TwoHandedBlunt |
| `wand_t40_common` | Darksteel Wand | B | Common | Blunt |
| `wand_t40_temp` | Darksteel Wand | B | Common | untradable, Blunt |
| `staff_t40` | Darksteel Battlestaff | B | Mythic | TwoHandedBlunt |
| `sword1h_t40` | Darksteel Blade | B | Mythic | Sword |
| `duals_t40` | Darksteel Fangs | B | Mythic | Dual |
| `sword2h_t40` | Darksteel Greatsword | B | Mythic | TwoHandedSword |
| `bow_t40` | Darksteel Longbow | B | Mythic | Bow |
| `blunt1h_t40` | Darksteel Mace | B | Mythic | Blunt |
| `blunt2h_t40` | Darksteel Maul | B | Mythic | TwoHandedBlunt |
| `wand_t40` | Darksteel Wand | B | Mythic | Blunt |

### Lv 52

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `staff_t52_common` | Cobalt Battlestaff | B | Common | TwoHandedBlunt |
| `staff_t52_temp` | Cobalt Battlestaff | B | Common | untradable, TwoHandedBlunt |
| `sword1h_t52_common` | Cobalt Blade | B | Common | Sword |
| `sword1h_t52_temp` | Cobalt Blade | B | Common | untradable, Sword |
| `duals_t52_common` | Cobalt Fangs | B | Common | Dual |
| `duals_t52_temp` | Cobalt Fangs | B | Common | untradable, Dual |
| `sword2h_t52_common` | Cobalt Greatsword | B | Common | TwoHandedSword |
| `sword2h_t52_temp` | Cobalt Greatsword | B | Common | untradable, TwoHandedSword |
| `bow_t52_common` | Cobalt Longbow | B | Common | Bow |
| `bow_t52_temp` | Cobalt Longbow | B | Common | untradable, Bow |
| `blunt1h_t52_common` | Cobalt Mace | B | Common | Blunt |
| `blunt1h_t52_temp` | Cobalt Mace | B | Common | untradable, Blunt |
| `blunt2h_t52_common` | Cobalt Maul | B | Common | TwoHandedBlunt |
| `blunt2h_t52_temp` | Cobalt Maul | B | Common | untradable, TwoHandedBlunt |
| `wand_t52_common` | Cobalt Wand | B | Common | Blunt |
| `wand_t52_temp` | Cobalt Wand | B | Common | untradable, Blunt |
| `staff_t52` | Cobalt Battlestaff | B | Mythic | TwoHandedBlunt |
| `sword1h_t52` | Cobalt Blade | B | Mythic | Sword |
| `duals_t52` | Cobalt Fangs | B | Mythic | Dual |
| `sword2h_t52` | Cobalt Greatsword | B | Mythic | TwoHandedSword |
| `bow_t52` | Cobalt Longbow | B | Mythic | Bow |
| `blunt1h_t52` | Cobalt Mace | B | Mythic | Blunt |
| `blunt2h_t52` | Cobalt Maul | B | Mythic | TwoHandedBlunt |
| `wand_t52` | Cobalt Wand | B | Mythic | Blunt |

### Lv 61

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `staff_t61_common` | Bloodsteel Battlestaff | A | Common | TwoHandedBlunt |
| `sword1h_t61_common` | Bloodsteel Blade | A | Common | Sword |
| `duals_t61_common` | Bloodsteel Fangs | A | Common | Dual |
| `sword2h_t61_common` | Bloodsteel Greatsword | A | Common | TwoHandedSword |
| `bow_t61_common` | Bloodsteel Longbow | A | Common | Bow |
| `blunt1h_t61_common` | Bloodsteel Mace | A | Common | Blunt |
| `blunt2h_t61_common` | Bloodsteel Maul | A | Common | TwoHandedBlunt |
| `wand_t61_common` | Bloodsteel Wand | A | Common | Blunt |
| `staff_t61` | Bloodsteel Battlestaff | A | Mythic | TwoHandedBlunt |
| `sword1h_t61` | Bloodsteel Blade | A | Mythic | Sword |
| `duals_t61` | Bloodsteel Fangs | A | Mythic | Dual |
| `sword2h_t61` | Bloodsteel Greatsword | A | Mythic | TwoHandedSword |
| `bow_t61` | Bloodsteel Longbow | A | Mythic | Bow |
| `blunt1h_t61` | Bloodsteel Mace | A | Mythic | Blunt |
| `blunt2h_t61` | Bloodsteel Maul | A | Mythic | TwoHandedBlunt |
| `wand_t61` | Bloodsteel Wand | A | Mythic | Blunt |

### Lv 76

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `staff_t76` | Adamantine Battlestaff | A | Mythic | TwoHandedBlunt |
| `sword1h_t76` | Adamantine Blade | A | Mythic | Sword |
| `duals_t76` | Adamantine Fangs | A | Mythic | Dual |
| `sword2h_t76` | Adamantine Greatsword | A | Mythic | TwoHandedSword |
| `bow_t76` | Adamantine Longbow | A | Mythic | Bow |
| `blunt1h_t76` | Adamantine Mace | A | Mythic | Blunt |
| `blunt2h_t76` | Adamantine Maul | A | Mythic | TwoHandedBlunt |
| `wand_t76` | Adamantine Wand | A | Mythic | Blunt |

### Lv 80

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `staff_t80` | Soulcrystal Battlestaff | S | Mythic | TwoHandedBlunt |
| `sword1h_t80` | Soulcrystal Blade | S | Mythic | Sword |
| `duals_t80` | Soulcrystal Fangs | S | Mythic | Dual |
| `sword2h_t80` | Soulcrystal Greatsword | S | Mythic | TwoHandedSword |
| `bow_t80` | Soulcrystal Longbow | S | Mythic | Bow |
| `blunt1h_t80` | Soulcrystal Mace | S | Mythic | Blunt |
| `blunt2h_t80` | Soulcrystal Maul | S | Mythic | TwoHandedBlunt |
| `wand_t80` | Soulcrystal Wand | S | Mythic | Blunt |

## Shields  (13)

### no tier (training / one-off)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_wooden` | Wooden Shield | F | Common | untradable |

### Lv 1

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t1` | Ferrite Aegis | F | Mythic |  |

### Lv 20

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t20` | Electrum Aegis | E | Mythic |  |

### Lv 40

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t40_common` | Darksteel Aegis | B | Common |  |
| `shield_t40_temp` | Darksteel Aegis | B | Common | untradable |
| `shield_t40` | Darksteel Aegis | B | Mythic |  |

### Lv 52

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t52_common` | Cobalt Aegis | B | Common |  |
| `shield_t52_temp` | Cobalt Aegis | B | Common | untradable |
| `shield_t52` | Cobalt Aegis | B | Mythic |  |

### Lv 61

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t61_common` | Bloodsteel Aegis | A | Common |  |
| `shield_t61` | Bloodsteel Aegis | A | Mythic |  |

### Lv 76

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t76` | Adamantine Aegis | A | Mythic |  |

### Lv 80

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t80` | Soulcrystal Aegis | S | Mythic |  |

## Armor  (89)

### no tier (training / one-off)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `training_leather_armor` | Training Leather Armor | F | Common | untradable, Light, Body |
| `training_robe` | Training Robe | F | Common | untradable, Robe, Body |

### Lv 1

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `heavy_t1` | Ferrite Bulwark | F | Mythic | Heavy, Body |
| `gloves_t1` | Ferrite Gauntlets | F | Mythic | Gloves |
| `boots_t1` | Ferrite Greaves | F | Mythic | Boots |
| `helm_t1` | Ferrite Helm | F | Mythic | Head |
| `light_t1` | Ferrite Leathers | F | Mythic | Light, Body |
| `robe_t1` | Ferrite Robe | F | Mythic | Robe, Body |
| `gloves_t1_bound` | Newbie Ferrite Gauntlets | F | Mythic | untradable, Gloves |
| `boots_t1_bound` | Newbie Ferrite Greaves | F | Mythic | untradable, Boots |
| `helm_t1_bound` | Newbie Ferrite Helm | F | Mythic | untradable, Head |
| `light_t1_bound` | Newbie Ferrite Leathers | F | Mythic | untradable, Light, Body |
| `robe_t1_bound` | Newbie Ferrite Robe | F | Mythic | untradable, Robe, Body |

### Lv 20

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `heavy_t20` | Electrum Bulwark | E | Mythic | Heavy, Body |
| `gloves_t20` | Electrum Gauntlets | E | Mythic | Gloves |
| `boots_t20` | Electrum Greaves | E | Mythic | Boots |
| `helm_t20` | Electrum Helm | E | Mythic | Head |
| `light_t20` | Electrum Leathers | E | Mythic | Light, Body |
| `robe_t20` | Electrum Robe | E | Mythic | Robe, Body |

### Lv 40

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `heavy_t40_common` | Darksteel Bulwark | B | Common | Heavy, Body |
| `heavy_t40_temp` | Darksteel Bulwark | B | Common | untradable, Heavy, Body |
| `gloves_t40_common` | Darksteel Gauntlets | B | Common | Gloves |
| `gloves_t40_temp` | Darksteel Gauntlets | B | Common | untradable, Gloves |
| `boots_t40_common` | Darksteel Greaves | B | Common | Boots |
| `boots_t40_temp` | Darksteel Greaves | B | Common | untradable, Boots |
| `helm_t40_common` | Darksteel Helm | B | Common | Head |
| `helm_t40_temp` | Darksteel Helm | B | Common | untradable, Head |
| `light_t40_common` | Darksteel Leathers | B | Common | Light, Body |
| `light_t40_temp` | Darksteel Leathers | B | Common | untradable, Light, Body |
| `robe_t40_common` | Darksteel Robe | B | Common | Robe, Body |
| `robe_t40_temp` | Darksteel Robe | B | Common | untradable, Robe, Body |
| `light_t40_str` | Darksteel Brawlhide | B | Mythic | Light, Body |
| `heavy_t40` | Darksteel Bulwark | B | Mythic | Heavy, Body |
| `gloves_t40` | Darksteel Gauntlets | B | Mythic | Gloves |
| `boots_t40` | Darksteel Greaves | B | Mythic | Boots |
| `light_t40_pdef` | Darksteel Guardhide | B | Mythic | Light, Body |
| `helm_t40` | Darksteel Helm | B | Mythic | Head |
| `light_t40` | Darksteel Leathers | B | Mythic | Light, Body |
| `robe_t40_sup` | Darksteel Raiment | B | Mythic | Robe, Body |
| `robe_t40` | Darksteel Robe | B | Mythic | Robe, Body |
| `robe_t40_nuke` | Darksteel Vestments | B | Mythic | Robe, Body |
| `light_t40_mdef` | Darksteel Wardhide | B | Mythic | Light, Body |

### Lv 52

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `heavy_t52_common` | Cobalt Bulwark | B | Common | Heavy, Body |
| `heavy_t52_temp` | Cobalt Bulwark | B | Common | untradable, Heavy, Body |
| `gloves_t52_common` | Cobalt Gauntlets | B | Common | Gloves |
| `gloves_t52_temp` | Cobalt Gauntlets | B | Common | untradable, Gloves |
| `boots_t52_common` | Cobalt Greaves | B | Common | Boots |
| `boots_t52_temp` | Cobalt Greaves | B | Common | untradable, Boots |
| `helm_t52_common` | Cobalt Helm | B | Common | Head |
| `helm_t52_temp` | Cobalt Helm | B | Common | untradable, Head |
| `light_t52_common` | Cobalt Leathers | B | Common | Light, Body |
| `light_t52_temp` | Cobalt Leathers | B | Common | untradable, Light, Body |
| `robe_t52_common` | Cobalt Robe | B | Common | Robe, Body |
| `robe_t52_temp` | Cobalt Robe | B | Common | untradable, Robe, Body |
| `heavy_t52` | Cobalt Bulwark | B | Mythic | Heavy, Body |
| `gloves_t52` | Cobalt Gauntlets | B | Mythic | Gloves |
| `boots_t52` | Cobalt Greaves | B | Mythic | Boots |
| `helm_t52` | Cobalt Helm | B | Mythic | Head |
| `light_t52` | Cobalt Leathers | B | Mythic | Light, Body |
| `robe_t52` | Cobalt Robe | B | Mythic | Robe, Body |
| `light_t52_sup` | Cobalt Sagehide | B | Mythic | Light, Body |
| `heavy_t52_dmg` | Cobalt Warplate | B | Mythic | Heavy, Body |

### Lv 61

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `heavy_t61_common` | Bloodsteel Bulwark | A | Common | Heavy, Body |
| `gloves_t61_common` | Bloodsteel Gauntlets | A | Common | Gloves |
| `boots_t61_common` | Bloodsteel Greaves | A | Common | Boots |
| `helm_t61_common` | Bloodsteel Helm | A | Common | Head |
| `light_t61_common` | Bloodsteel Leathers | A | Common | Light, Body |
| `robe_t61_common` | Bloodsteel Robe | A | Common | Robe, Body |
| `heavy_t61` | Bloodsteel Bulwark | A | Mythic | Heavy, Body |
| `gloves_t61` | Bloodsteel Gauntlets | A | Mythic | Gloves |
| `boots_t61` | Bloodsteel Greaves | A | Mythic | Boots |
| `helm_t61` | Bloodsteel Helm | A | Mythic | Head |
| `light_t61` | Bloodsteel Leathers | A | Mythic | Light, Body |
| `robe_t61_sup` | Bloodsteel Raiment | A | Mythic | Robe, Body |
| `robe_t61` | Bloodsteel Robe | A | Mythic | Robe, Body |
| `light_t61_dmg` | Bloodsteel Warhide | A | Mythic | Light, Body |
| `heavy_t61_dmg` | Bloodsteel Warplate | A | Mythic | Heavy, Body |

### Lv 76

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `heavy_t76` | Adamantine Bulwark | A | Mythic | Heavy, Body |
| `gloves_t76` | Adamantine Gauntlets | A | Mythic | Gloves |
| `boots_t76` | Adamantine Greaves | A | Mythic | Boots |
| `helm_t76` | Adamantine Helm | A | Mythic | Head |
| `light_t76` | Adamantine Leathers | A | Mythic | Light, Body |
| `robe_t76` | Adamantine Robe | A | Mythic | Robe, Body |

### Lv 80

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `heavy_t80` | Soulcrystal Bulwark | S | Mythic | Heavy, Body |
| `gloves_t80` | Soulcrystal Gauntlets | S | Mythic | Gloves |
| `boots_t80` | Soulcrystal Greaves | S | Mythic | Boots |
| `helm_t80` | Soulcrystal Helm | S | Mythic | Head |
| `light_t80` | Soulcrystal Leathers | S | Mythic | Light, Body |
| `robe_t80` | Soulcrystal Robe | S | Mythic | Robe, Body |

## Jewels  (36)

### no tier (training / one-off)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `broken_earring` | Broken Earring | F | Common | Earring |
| `broken_necklace` | Broken Necklace | F | Common | Necklace |
| `broken_ring` | Broken Ring | F | Common | Ring |

### Lv 1

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t1` | Ferrite Band | F | Mythic | Ring |
| `necklace_t1` | Ferrite Pendant | F | Mythic | Necklace |
| `earring_t1` | Ferrite Stud | F | Mythic | Earring |
| `ring_t1_bound` | Newbie Ferrite Band | F | Mythic | untradable, Ring |
| `necklace_t1_bound` | Newbie Ferrite Pendant | F | Mythic | untradable, Necklace |
| `earring_t1_bound` | Newbie Ferrite Stud | F | Mythic | untradable, Earring |

### Lv 20

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t20` | Electrum Band | E | Mythic | Ring |
| `necklace_t20` | Electrum Pendant | E | Mythic | Necklace |
| `earring_t20` | Electrum Stud | E | Mythic | Earring |

### Lv 40

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t40_common` | Darksteel Band | B | Common | Ring |
| `necklace_t40_common` | Darksteel Pendant | B | Common | Necklace |
| `earring_t40_common` | Darksteel Stud | B | Common | Earring |
| `ring_t40` | Darksteel Band | B | Mythic | Ring |
| `necklace_t40` | Darksteel Pendant | B | Mythic | Necklace |
| `earring_t40` | Darksteel Stud | B | Mythic | Earring |

### Lv 52

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t52_common` | Cobalt Band | B | Common | Ring |
| `necklace_t52_common` | Cobalt Pendant | B | Common | Necklace |
| `earring_t52_common` | Cobalt Stud | B | Common | Earring |
| `ring_t52` | Cobalt Band | B | Mythic | Ring |
| `necklace_t52` | Cobalt Pendant | B | Mythic | Necklace |
| `earring_t52` | Cobalt Stud | B | Mythic | Earring |

### Lv 61

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t61_common` | Bloodsteel Band | A | Common | Ring |
| `necklace_t61_common` | Bloodsteel Pendant | A | Common | Necklace |
| `earring_t61_common` | Bloodsteel Stud | A | Common | Earring |
| `ring_t61` | Bloodsteel Band | A | Mythic | Ring |
| `necklace_t61` | Bloodsteel Pendant | A | Mythic | Necklace |
| `earring_t61` | Bloodsteel Stud | A | Mythic | Earring |

### Lv 76

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t76` | Adamantine Band | A | Mythic | Ring |
| `necklace_t76` | Adamantine Pendant | A | Mythic | Necklace |
| `earring_t76` | Adamantine Stud | A | Mythic | Earring |

### Lv 80

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t80` | Soulcrystal Band | S | Mythic | Ring |
| `necklace_t80` | Soulcrystal Pendant | S | Mythic | Necklace |
| `earring_t80` | Soulcrystal Stud | S | Mythic | Earring |

## Runes  (64)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `rune_blessing_boost_1h` | Blessing Booster Rune (1h) | F | Mythic | untradable |
| `rune_blessing_boost_2h` | Blessing Booster Rune (2h) | F | Mythic | untradable |
| `rune_favor_keep_1h` | Favor Keep-Rune (1h) | F | Mythic | untradable |
| `rune_favor_keep_2h` | Favor Keep-Rune (2h) | F | Mythic | untradable |
| `rune_grand` | Grand Rune | F | Mythic | untradable |
| `rune_drop_10` | Rune of Drop (10%) | F | Mythic | untradable |
| `rune_drop_100` | Rune of Drop (100%) | F | Mythic | untradable |
| `rune_drop_20` | Rune of Drop (20%) | F | Mythic | untradable |
| `rune_drop_30` | Rune of Drop (30%) | F | Mythic | untradable |
| `rune_drop_40` | Rune of Drop (40%) | F | Mythic | untradable |
| `rune_drop_5` | Rune of Drop (5%) | F | Mythic | untradable |
| `rune_drop_50` | Rune of Drop (50%) | F | Mythic | untradable |
| `rune_drop_60` | Rune of Drop (60%) | F | Mythic | untradable |
| `rune_drop_70` | Rune of Drop (70%) | F | Mythic | untradable |
| `rune_drop_80` | Rune of Drop (80%) | F | Mythic | untradable |
| `rune_drop_90` | Rune of Drop (90%) | F | Mythic | untradable |
| `rune_expsp_10` | Rune of Exp/SP (10%) | F | Mythic | untradable |
| `rune_expsp_100` | Rune of Exp/SP (100%) | F | Mythic | untradable |
| `rune_expsp_20` | Rune of Exp/SP (20%) | F | Mythic | untradable |
| `rune_expsp_30` | Rune of Exp/SP (30%) | F | Mythic | untradable |
| `rune_expsp_40` | Rune of Exp/SP (40%) | F | Mythic | untradable |
| `rune_expsp_5` | Rune of Exp/SP (5%) | F | Mythic | untradable |
| `rune_expsp_50` | Rune of Exp/SP (50%) | F | Mythic | untradable |
| `rune_expsp_60` | Rune of Exp/SP (60%) | F | Mythic | untradable |
| `rune_expsp_70` | Rune of Exp/SP (70%) | F | Mythic | untradable |
| `rune_expsp_80` | Rune of Exp/SP (80%) | F | Mythic | untradable |
| `rune_expsp_90` | Rune of Exp/SP (90%) | F | Mythic | untradable |
| `rune_exp_10` | Rune of Experience (10%) | F | Mythic | untradable |
| `rune_exp_100` | Rune of Experience (100%) | F | Mythic | untradable |
| `rune_exp_20` | Rune of Experience (20%) | F | Mythic | untradable |
| `rune_exp_30` | Rune of Experience (30%) | F | Mythic | untradable |
| `rune_exp_40` | Rune of Experience (40%) | F | Mythic | untradable |
| `rune_exp_5` | Rune of Experience (5%) | F | Mythic | untradable |
| `rune_exp_50` | Rune of Experience (50%) | F | Mythic | untradable |
| `rune_exp_60` | Rune of Experience (60%) | F | Mythic | untradable |
| `rune_exp_70` | Rune of Experience (70%) | F | Mythic | untradable |
| `rune_exp_80` | Rune of Experience (80%) | F | Mythic | untradable |
| `rune_exp_90` | Rune of Experience (90%) | F | Mythic | untradable |
| `rune_gold_10` | Rune of Gold (10%) | F | Mythic | untradable |
| `rune_gold_100` | Rune of Gold (100%) | F | Mythic | untradable |
| `rune_gold_20` | Rune of Gold (20%) | F | Mythic | untradable |
| `rune_gold_30` | Rune of Gold (30%) | F | Mythic | untradable |
| `rune_gold_40` | Rune of Gold (40%) | F | Mythic | untradable |
| `rune_gold_5` | Rune of Gold (5%) | F | Mythic | untradable |
| `rune_gold_50` | Rune of Gold (50%) | F | Mythic | untradable |
| `rune_gold_60` | Rune of Gold (60%) | F | Mythic | untradable |
| `rune_gold_70` | Rune of Gold (70%) | F | Mythic | untradable |
| `rune_gold_80` | Rune of Gold (80%) | F | Mythic | untradable |
| `rune_gold_90` | Rune of Gold (90%) | F | Mythic | untradable |
| `rune_sinister` | Rune of Sinister | F | Mythic | untradable |
| `rune_sinners` | Rune of Sinners | F | Mythic | untradable, **soulbound** |
| `rune_sp_10` | Rune of Skillpoints (10%) | F | Mythic | untradable |
| `rune_sp_100` | Rune of Skillpoints (100%) | F | Mythic | untradable |
| `rune_sp_20` | Rune of Skillpoints (20%) | F | Mythic | untradable |
| `rune_sp_30` | Rune of Skillpoints (30%) | F | Mythic | untradable |
| `rune_sp_40` | Rune of Skillpoints (40%) | F | Mythic | untradable |
| `rune_sp_5` | Rune of Skillpoints (5%) | F | Mythic | untradable |
| `rune_sp_50` | Rune of Skillpoints (50%) | F | Mythic | untradable |
| `rune_sp_60` | Rune of Skillpoints (60%) | F | Mythic | untradable |
| `rune_sp_70` | Rune of Skillpoints (70%) | F | Mythic | untradable |
| `rune_sp_80` | Rune of Skillpoints (80%) | F | Mythic | untradable |
| `rune_sp_90` | Rune of Skillpoints (90%) | F | Mythic | untradable |
| `rune_spell` | Spell Rune | F | Mythic | untradable |
| `rune_war` | War Rune | F | Mythic | untradable |

## Consumables (potions)  (66)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `potion_eva_u` | Agility Potion | F | Uncommon | stacks |
| `potion_eva_c` | Agility Potion (Lesser) | F | Common | stacks |
| `potion_acc_u` | Aim Potion | F | Uncommon | stacks |
| `potion_acc_c` | Aim Potion (Lesser) | F | Common | stacks |
| `potion_cast_u` | Alacrity Potion | F | Uncommon | stacks |
| `potion_cast_c` | Alacrity Potion (Lesser) | F | Common | stacks |
| `potion_pdef_u` | Bulwark Potion | F | Uncommon | stacks |
| `potion_pdef_c` | Bulwark Potion (Lesser) | F | Common | stacks |
| `potion_minor` | Common Healing Potion | F | Common | stacks |
| `potion_mana_minor` | Common Mana Potion | F | Common | stacks |
| `potion_dash_u` | Dash Potion | F | Uncommon | stacks |
| `potion_dash_l` | Dash Potion (Grand) | F | Legendary | stacks |
| `potion_dash_r` | Dash Potion (Greater) | F | Rare | stacks |
| `potion_dash_c` | Dash Potion (Lesser) | F | Common | stacks |
| `potion_dash_e` | Dash Potion (Superior) | F | Epic | stacks |
| `potion_dash_m` | Dash Potion (Supreme) | F | Mythic | stacks |
| `potion_dash_m_bound` | Dash Potion (Supreme) (Bound) | F | Mythic | untradable, stacks |
| `elemental_stone` | Elemental Stone | F | Rare | stacks |
| `potion_favor_restore` | Favor Restore Potion | F | Mythic | untradable, stacks |
| `potion_matk_u` | Force Potion | F | Uncommon | stacks |
| `potion_matk_c` | Force Potion (Lesser) | F | Common | stacks |
| `potion_atk_u` | Fury Potion | F | Uncommon | stacks |
| `potion_atk_c` | Fury Potion (Lesser) | F | Common | stacks |
| `holy_stone` | Holy Stone | F | Rare | stacks |
| `potion_instant` | Instant Healing Potion | F | Rare | stacks |
| `potion_instant_bound` | Instant Healing Potion (Bound) | F | Rare | untradable, stacks |
| `potion_patk_u` | Might Potion | F | Uncommon | stacks |
| `potion_patk_c` | Might Potion (Lesser) | F | Common | stacks |
| `physical_stone` | Physical Stone | F | Rare | stacks |
| `potion_greater` | Rare Healing Potion | F | Rare | stacks |
| `potion_mana_greater` | Rare Mana Potion | F | Rare | stacks |
| `rune_title_colour` | Rune of Tincture | F | Uncommon | stacks |
| `scroll_eva_r` | Scroll of Agility | F | Rare | untradable, stacks |
| `scroll_acc_r` | Scroll of Aim | F | Rare | untradable, stacks |
| `scroll_cast_r` | Scroll of Alacrity | F | Rare | untradable, stacks |
| `scroll_hp_m` | Scroll of Body | F | Rare | untradable, stacks |
| `scroll_pdef_r` | Scroll of Bulwark | F | Rare | untradable, stacks |
| `scroll_critdmg_m` | Scroll of Ferocity | F | Rare | untradable, stacks |
| `scroll_crit_m` | Scroll of Focus | F | Rare | untradable, stacks |
| `scroll_matk_r` | Scroll of Force | F | Rare | untradable, stacks |
| `scroll_frenzy_m` | Scroll of Frenzy | F | Rare | untradable, stacks |
| `scroll_atk_r` | Scroll of Fury | F | Rare | untradable, stacks |
| `scroll_mcrit_m` | Scroll of Insight | F | Rare | untradable, stacks |
| `scroll_patk_r` | Scroll of Might | F | Rare | untradable, stacks |
| `scroll_interrupt_m` | Scroll of Resolve | F | Rare | untradable, stacks |
| `scroll_resurrect` | Scroll of Resurrection | F | Uncommon | stacks |
| `scroll_return` | Scroll of Return | F | Common | stacks |
| `scroll_mpreg_m` | Scroll of Serenity | F | Rare | untradable, stacks |
| `scroll_mp_m` | Scroll of Soul | F | Rare | untradable, stacks |
| `scroll_speed_r` | Scroll of Swift | F | Rare | untradable, stacks |
| `scroll_vamp_m` | Scroll of Vampirism | F | Rare | untradable, stacks |
| `scroll_hpreg_m` | Scroll of Vigor | F | Rare | untradable, stacks |
| `scroll_mdef_r` | Scroll of Ward | F | Rare | untradable, stacks |
| `skill_stone` | Skill Stone | F | Uncommon | stacks |
| `sp_bottle` | SP Bottle | S | Epic | stacks |
| `subclass_ticket` | Subclass Ticket | F | Mythic | untradable, stacks |
| `potion_speed_u` | Swift Potion | F | Uncommon | stacks |
| `potion_speed_c` | Swift Potion (Lesser) | F | Common | stacks |
| `scroll_resurrect_ultimate` | Ultimate Scroll of Resurrection | F | Rare | stacks |
| `scroll_resurrect_ultimate_bound` | Ultimate Scroll of Resurrection (Bound) | F | Rare | untradable, stacks |
| `scroll_return_ultimate` | Ultimate Scroll of Return | F | Rare | untradable, stacks |
| `scroll_return_ultimate_bound` | Ultimate Scroll of Return (Bound) | F | Rare | untradable, stacks |
| `potion_healing` | Uncommon Healing Potion | F | Uncommon | stacks |
| `potion_mana` | Uncommon Mana Potion | F | Uncommon | stacks |
| `potion_mdef_u` | Ward Potion | F | Uncommon | stacks |
| `potion_mdef_c` | Ward Potion (Lesser) | F | Common | stacks |

## Scrolls  (24)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `attrscroll_common` | Attribute Scroll (Common) | F | Common | stacks |
| `attrscroll_epic` | Attribute Scroll (Epic) | A | Epic | stacks |
| `attrscroll_legendary` | Attribute Scroll (Legendary) | A | Legendary | stacks |
| `attrscroll_mythic` | Attribute Scroll (Mythic) | S | Mythic | stacks |
| `attrscroll_rare` | Attribute Scroll (Rare) | F | Rare | stacks |
| `attrscroll_uncommon` | Attribute Scroll (Uncommon) | F | Uncommon | stacks |
| `scroll_greater_a` | Greater Scroll of Enchant (A) | A | Legendary | stacks |
| `scroll_greater_b` | Greater Scroll of Enchant (B) | B | Epic | stacks |
| `scroll_greater_c` | Greater Scroll of Enchant (C) | F | Rare | stacks |
| `scroll_greater_d` | Greater Scroll of Enchant (D) | F | Uncommon | stacks |
| `scroll_greater_e` | Greater Scroll of Enchant (E) | F | Common | stacks |
| `scroll_greater_s` | Greater Scroll of Enchant (S) | S | Mythic | stacks |
| `scroll_safe_a` | Safe Scroll of Enchant (A) | A | Legendary | stacks |
| `scroll_safe_b` | Safe Scroll of Enchant (B) | B | Epic | stacks |
| `scroll_safe_c` | Safe Scroll of Enchant (C) | F | Rare | stacks |
| `scroll_safe_d` | Safe Scroll of Enchant (D) | F | Uncommon | stacks |
| `scroll_safe_e` | Safe Scroll of Enchant (E) | F | Common | stacks |
| `scroll_safe_s` | Safe Scroll of Enchant (S) | S | Mythic | stacks |
| `scroll_enchant_a` | Scroll of Enchant (A) | A | Legendary | stacks |
| `scroll_enchant_b` | Scroll of Enchant (B) | B | Epic | stacks |
| `scroll_rare` | Scroll of Enchant (C) | F | Rare | stacks |
| `scroll_uncommon` | Scroll of Enchant (D) | F | Uncommon | stacks |
| `scroll_common` | Scroll of Enchant (E) | F | Common | stacks |
| `scroll_enchant_s` | Scroll of Enchant (S) | S | Mythic | stacks |

## Boxes  (75)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `box_acc_t76` | Adamantine Accessory Box | A | Rare | stacks |
| `box_buff_scrolls` | Blessing Box | F | Rare | stacks |
| `box_acc_t61` | Bloodsteel Accessory Box | A | Rare | stacks |
| `recipe_craft_shield_t76` | Blueprint: Adamantine Aegis | A | Epic | stacks |
| `recipe_craft_ring_t76` | Blueprint: Adamantine Band | A | Epic | stacks |
| `recipe_craft_staff_t76` | Blueprint: Adamantine Battlestaff | A | Epic | stacks |
| `recipe_craft_sword1h_t76` | Blueprint: Adamantine Blade | A | Epic | stacks |
| `recipe_craft_heavy_t76` | Blueprint: Adamantine Bulwark | A | Epic | stacks |
| `recipe_craft_duals_t76` | Blueprint: Adamantine Fangs | A | Epic | stacks |
| `recipe_craft_gloves_t76` | Blueprint: Adamantine Gauntlets | A | Epic | stacks |
| `recipe_craft_sword2h_t76` | Blueprint: Adamantine Greatsword | A | Epic | stacks |
| `recipe_craft_boots_t76` | Blueprint: Adamantine Greaves | A | Epic | stacks |
| `recipe_craft_helm_t76` | Blueprint: Adamantine Helm | A | Epic | stacks |
| `recipe_craft_light_t76` | Blueprint: Adamantine Leathers | A | Epic | stacks |
| `recipe_craft_bow_t76` | Blueprint: Adamantine Longbow | A | Epic | stacks |
| `recipe_craft_blunt1h_t76` | Blueprint: Adamantine Mace | A | Epic | stacks |
| `recipe_craft_blunt2h_t76` | Blueprint: Adamantine Maul | A | Epic | stacks |
| `recipe_craft_necklace_t76` | Blueprint: Adamantine Pendant | A | Epic | stacks |
| `recipe_craft_robe_t76` | Blueprint: Adamantine Robe | A | Epic | stacks |
| `recipe_craft_earring_t76` | Blueprint: Adamantine Stud | A | Epic | stacks |
| `recipe_craft_wand_t76` | Blueprint: Adamantine Wand | A | Epic | stacks |
| `recipe_craft_shield_t80` | Blueprint: Soulcrystal Aegis | A | Epic | stacks |
| `recipe_craft_ring_t80` | Blueprint: Soulcrystal Band | A | Epic | stacks |
| `recipe_craft_staff_t80` | Blueprint: Soulcrystal Battlestaff | A | Epic | stacks |
| `recipe_craft_sword1h_t80` | Blueprint: Soulcrystal Blade | A | Epic | stacks |
| `recipe_craft_heavy_t80` | Blueprint: Soulcrystal Bulwark | A | Epic | stacks |
| `recipe_craft_duals_t80` | Blueprint: Soulcrystal Fangs | A | Epic | stacks |
| `recipe_craft_gloves_t80` | Blueprint: Soulcrystal Gauntlets | A | Epic | stacks |
| `recipe_craft_sword2h_t80` | Blueprint: Soulcrystal Greatsword | A | Epic | stacks |
| `recipe_craft_boots_t80` | Blueprint: Soulcrystal Greaves | A | Epic | stacks |
| `recipe_craft_helm_t80` | Blueprint: Soulcrystal Helm | A | Epic | stacks |
| `recipe_craft_light_t80` | Blueprint: Soulcrystal Leathers | A | Epic | stacks |
| `recipe_craft_bow_t80` | Blueprint: Soulcrystal Longbow | A | Epic | stacks |
| `recipe_craft_blunt1h_t80` | Blueprint: Soulcrystal Mace | A | Epic | stacks |
| `recipe_craft_blunt2h_t80` | Blueprint: Soulcrystal Maul | A | Epic | stacks |
| `recipe_craft_necklace_t80` | Blueprint: Soulcrystal Pendant | A | Epic | stacks |
| `recipe_craft_robe_t80` | Blueprint: Soulcrystal Robe | A | Epic | stacks |
| `recipe_craft_earring_t80` | Blueprint: Soulcrystal Stud | A | Epic | stacks |
| `recipe_craft_wand_t80` | Blueprint: Soulcrystal Wand | A | Epic | stacks |
| `box_acc_t52` | Cobalt Accessory Box | B | Rare | stacks |
| `box_acc_t40` | Darksteel Accessory Box | B | Rare | stacks |
| `box_acc_t20` | Electrum Accessory Box | E | Rare | stacks |
| `box_acc_t1` | Ferrite Accessory Box | F | Rare | stacks |
| `box_grand_rune_24h` | Grand Rune Box (1d) | F | Rare | untradable, stacks |
| `box_newbie_armor_choice` | Newbie Armor Set | F | Common | untradable, stacks |
| `box_newbie` | Newbie Box | F | Common | untradable, stacks |
| `box_newbie_jewels` | Newbie Jewels Box | F | Common | untradable, stacks |
| `box_newbie_armor_light` | Newbie Light Armor Box | F | Common | untradable, stacks |
| `box_newbie_armor_robe` | Newbie Robe Armor Box | F | Common | untradable, stacks |
| `box_newbie_rune_choice` | Newbie Rune | F | Common | untradable, stacks |
| `box_newbie_weapons` | Newbie Weapons Box | F | Common | untradable, stacks |
| `box_daily_rune_choice` | Rune Box (1h) — Daily | F | Common | untradable, stacks |
| `box_acc_t80` | Soulcrystal Accessory Box | S | Rare | stacks |
| `box_spell_rune_24h` | Spell Rune Box (1d) | F | Rare | untradable, stacks |
| `box_spell_rune_1h` | Spell Rune Box (1h) | F | Rare | stacks |
| `box_spell_rune_2h` | Spell Rune Box (2h) | F | Rare | stacks |
| `box_spell_rune_30d` | Spell Rune Box (30d) | F | Rare | untradable, stacks |
| `box_temp_armor_t52` | Temporary Cobalt Armor | B | Common | untradable, stacks |
| `box_temp_heavy_t52` | Temporary Cobalt Bulwark Set | B | Common | untradable, stacks |
| `box_temp_light_t52` | Temporary Cobalt Leathers Set | B | Common | untradable, stacks |
| `box_temp_robe_t52` | Temporary Cobalt Robe Set | B | Common | untradable, stacks |
| `box_temp_weapon_t52` | Temporary Cobalt Weapon | B | Common | untradable, stacks |
| `box_temp_armor_t40` | Temporary Darksteel Armor | B | Common | untradable, stacks |
| `box_temp_heavy_t40` | Temporary Darksteel Bulwark Set | B | Common | untradable, stacks |
| `box_temp_light_t40` | Temporary Darksteel Leathers Set | B | Common | untradable, stacks |
| `box_temp_robe_t40` | Temporary Darksteel Robe Set | B | Common | untradable, stacks |
| `box_temp_weapon_t40` | Temporary Darksteel Weapon | B | Common | untradable, stacks |
| `box_training_armor_choice` | Training Armor Box | F | Common | untradable, stacks |
| `box_training_weapons` | Training Weapons Box | F | Common | untradable, stacks |
| `box_treasure` | Treasure Chest | F | Uncommon | stacks |
| `box_war_rune_24h` | War Rune Box (1d) | F | Rare | untradable, stacks |
| `box_war_rune_1h` | War Rune Box (1h) | F | Rare | stacks |
| `box_war_rune_2h` | War Rune Box (2h) | F | Rare | stacks |
| `box_war_rune_30d` | War Rune Box (30d) | F | Rare | untradable, stacks |
| `box_wayfarer_subclass` | Wayfarer's Subclass Box | F | Mythic | untradable, stacks |

## Materials  (35)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `essence_a` | Adamantine Essence | F | Common | stacks |
| `essence_b` | Bloodsteel Essence | F | Common | stacks |
| `essence_c` | Cobalt Essence | F | Common | stacks |
| `mat_gem_common` | Common Gem | F | Common | stacks |
| `mat_ingot_common` | Common Ingot | F | Common | stacks |
| `mat_leather_common` | Common Leather | F | Common | stacks |
| `mat_thread_common` | Common Thread | F | Common | stacks |
| `mat_wood_common` | Common Wood | F | Common | stacks |
| `essence_d` | Darksteel Essence | F | Common | stacks |
| `mat_gem_epic` | Epic Gem | F | Epic | stacks |
| `mat_ingot_epic` | Epic Ingot | F | Epic | stacks |
| `mat_leather_epic` | Epic Leather | F | Epic | stacks |
| `mat_thread_epic` | Epic Thread | F | Epic | stacks |
| `mat_wood_epic` | Epic Wood | F | Epic | stacks |
| `mat_gem_legendary` | Legendary Gem | F | Legendary | stacks |
| `mat_ingot_legendary` | Legendary Ingot | F | Legendary | stacks |
| `mat_leather_legendary` | Legendary Leather | F | Legendary | stacks |
| `mat_thread_legendary` | Legendary Thread | F | Legendary | stacks |
| `mat_wood_legendary` | Legendary Wood | F | Legendary | stacks |
| `mat_gem_mythic` | Mythic Gem | F | Mythic | stacks |
| `mat_ingot_mythic` | Mythic Ingot | F | Mythic | stacks |
| `mat_leather_mythic` | Mythic Leather | F | Mythic | stacks |
| `mat_thread_mythic` | Mythic Thread | F | Mythic | stacks |
| `mat_wood_mythic` | Mythic Wood | F | Mythic | stacks |
| `mat_gem_rare` | Rare Gem | F | Rare | stacks |
| `mat_ingot_rare` | Rare Ingot | F | Rare | stacks |
| `mat_leather_rare` | Rare Leather | F | Rare | stacks |
| `mat_thread_rare` | Rare Thread | F | Rare | stacks |
| `mat_wood_rare` | Rare Wood | F | Rare | stacks |
| `essence_s` | Soulcrystal Essence | F | Common | stacks |
| `mat_gem_uncommon` | Uncommon Gem | F | Uncommon | stacks |
| `mat_ingot_uncommon` | Uncommon Ingot | F | Uncommon | stacks |
| `mat_leather_uncommon` | Uncommon Leather | F | Uncommon | stacks |
| `mat_thread_uncommon` | Uncommon Thread | F | Uncommon | stacks |
| `mat_wood_uncommon` | Uncommon Wood | F | Uncommon | stacks |

## Quest items  (96)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `quest_token_basilisk_scale` | Amber Scale | F | Common | stacks |
| `quest_token_ash_orc_insignia` | Ash Orc Insignia | F | Common | stacks |
| `qi_129_token` | Assassin Ordeal Mark | F | Epic | stacks |
| `quest_token_spider_hook` | Barbed Hook | F | Common | stacks |
| `quest_token_bear_pelt` | Bear Pelt | F | Common | stacks |
| `qi_127_token` | Champion Ordeal Mark | F | Epic | stacks |
| `quest_clerics_proof` | Cleric's Proof | F | Epic | stacks |
| `quest_token_cracked_rib` | Cracked Rib | F | Common | stacks |
| `qi_109_token` | Dark Healer Ordeal Mark | F | Epic | stacks |
| `qi_6_token` | Demon Apprentice Trial Token | F | Rare | stacks |
| `qi_6_proof` | Demon Apprentice's Proof | F | Epic | stacks |
| `qi_1_token` | Demon Knight Trial Token | F | Rare | stacks |
| `qi_1_proof` | Demon Knight's Proof | F | Epic | stacks |
| `qi_5_token` | Demon Priest Trial Token | F | Rare | stacks |
| `qi_5_proof` | Demon Priest's Proof | F | Epic | stacks |
| `qi_3_token` | Demon Rogue Trial Token | F | Rare | stacks |
| `qi_3_proof` | Demon Rogue's Proof | F | Epic | stacks |
| `qi_2_token` | Demon Warrior Trial Token | F | Rare | stacks |
| `qi_2_proof` | Demon Warrior's Proof | F | Epic | stacks |
| `qi_134_token` | Doctor Ordeal Mark | F | Epic | stacks |
| `qi_101_token` | Dread Knight Ordeal Mark | F | Epic | stacks |
| `quest_token_dread_sigil` | Dread Sigil | F | Common | stacks |
| `qi_110_token` | Dreadcaller Ordeal Mark | F | Epic | stacks |
| `qi_12_token` | Elf Apprentice Trial Token | F | Rare | stacks |
| `qi_12_proof` | Elf Apprentice's Proof | F | Epic | stacks |
| `qi_7_token` | Elf Knight Trial Token | F | Rare | stacks |
| `qi_7_proof` | Elf Knight's Proof | F | Epic | stacks |
| `qi_11_token` | Elf Priest Trial Token | F | Rare | stacks |
| `qi_11_proof` | Elf Priest's Proof | F | Epic | stacks |
| `qi_9_token` | Elf Rogue Trial Token | F | Rare | stacks |
| `qi_9_proof` | Elf Rogue's Proof | F | Epic | stacks |
| `qi_8_token` | Elf Warrior Trial Token | F | Rare | stacks |
| `qi_8_proof` | Elf Warrior's Proof | F | Epic | stacks |
| `quest_token_ember_scale` | Emberwyrm Scale | F | Common | stacks |
| `qi_111_token` | Fire Adept Ordeal Mark | F | Epic | stacks |
| `qi_121_token` | Forest Whisperer Ordeal Mark | F | Epic | stacks |
| `quest_token_fox_pelt` | Fox Pelt | F | Common | stacks |
| `qi_122_token` | Harmonist Ordeal Mark | F | Epic | stacks |
| `quest_token_harpy_feather` | Harpy Feather | F | Common | stacks |
| `qi_133_token` | Holy Priest Ordeal Mark | F | Epic | stacks |
| `qi_18_token` | Human Apprentice Trial Token | F | Rare | stacks |
| `qi_18_proof` | Human Apprentice's Proof | F | Epic | stacks |
| `qi_13_token` | Human Knight Trial Token | F | Rare | stacks |
| `qi_13_proof` | Human Knight's Proof | F | Epic | stacks |
| `qi_17_token` | Human Priest Trial Token | F | Rare | stacks |
| `qi_17_proof` | Human Priest's Proof | F | Epic | stacks |
| `qi_15_token` | Human Rogue Trial Token | F | Rare | stacks |
| `qi_15_proof` | Human Rogue's Proof | F | Epic | stacks |
| `qi_14_token` | Human Warrior Trial Token | F | Rare | stacks |
| `qi_14_proof` | Human Warrior's Proof | F | Epic | stacks |
| `qi_125_token` | Iron Guard Ordeal Mark | F | Epic | stacks |
| `qi_135_token` | Mana Adept Ordeal Mark | F | Epic | stacks |
| `quest_token_mantis_claw` | Mantis Claw | F | Common | stacks |
| `quest_mark_of_faith` | Mark of Faith | F | Rare | stacks |
| `qi_117_token` | Phantom Ordeal Mark | F | Epic | stacks |
| `quest_token_radiant_plume` | Radiant Plume | F | Common | stacks |
| `qi_103_token` | Ravager Ordeal Mark | F | Epic | stacks |
| `quest_token_redhorn_badge` | Redhorn Badge | F | Common | stacks |
| `qi_ascension_rite` | Rite of Ascension | F | Legendary | untradable, stacks |
| `quest_token_rusted_shard` | Rusted Shard | F | Common | stacks |
| `qi_129_proof` | Seal of the Assassin | F | Legendary | stacks |
| `qi_127_proof` | Seal of the Champion | F | Legendary | stacks |
| `qi_109_proof` | Seal of the Dark Healer | F | Legendary | stacks |
| `qi_134_proof` | Seal of the Doctor | F | Legendary | stacks |
| `qi_101_proof` | Seal of the Dread Knight | F | Legendary | stacks |
| `qi_110_proof` | Seal of the Dreadcaller | F | Legendary | stacks |
| `qi_111_proof` | Seal of the Fire Adept | F | Legendary | stacks |
| `qi_121_proof` | Seal of the Forest Whisperer | F | Legendary | stacks |
| `qi_122_proof` | Seal of the Harmonist | F | Legendary | stacks |
| `qi_133_proof` | Seal of the Holy Priest | F | Legendary | stacks |
| `qi_125_proof` | Seal of the Iron Guard | F | Legendary | stacks |
| `qi_135_proof` | Seal of the Mana Adept | F | Legendary | stacks |
| `qi_117_proof` | Seal of the Phantom | F | Legendary | stacks |
| `qi_103_proof` | Seal of the Ravager | F | Legendary | stacks |
| `qi_118_proof` | Seal of the Sentinel | F | Legendary | stacks |
| `qi_130_proof` | Seal of the Sharpshooter | F | Legendary | stacks |
| `qi_116_proof` | Seal of the Skirmisher | F | Legendary | stacks |
| `qi_106_proof` | Seal of the Soultracker | F | Legendary | stacks |
| `qi_105_proof` | Seal of the Stalker | F | Legendary | stacks |
| `qi_115_proof` | Seal of the Swiftblade | F | Legendary | stacks |
| `qi_113_proof` | Seal of the Templar | F | Legendary | stacks |
| `qi_128_proof` | Seal of the Vanguard | F | Legendary | stacks |
| `qi_104_proof` | Seal of the Warborn | F | Legendary | stacks |
| `qi_123_proof` | Seal of the Water Adept | F | Legendary | stacks |
| `qi_118_token` | Sentinel Ordeal Mark | F | Epic | stacks |
| `qi_130_token` | Sharpshooter Ordeal Mark | F | Epic | stacks |
| `qi_116_token` | Skirmisher Ordeal Mark | F | Epic | stacks |
| `qi_106_token` | Soultracker Ordeal Mark | F | Epic | stacks |
| `quest_token_splinter_chitin` | Splinter Chitin | F | Common | stacks |
| `qi_105_token` | Stalker Ordeal Mark | F | Epic | stacks |
| `qi_115_token` | Swiftblade Ordeal Mark | F | Epic | stacks |
| `qi_113_token` | Templar Ordeal Mark | F | Epic | stacks |
| `qi_128_token` | Vanguard Ordeal Mark | F | Epic | stacks |
| `qi_104_token` | Warborn Ordeal Mark | F | Epic | stacks |
| `qi_123_token` | Water Adept Ordeal Mark | F | Epic | stacks |
| `quest_token_werewolf_fang` | Werewolf Fang | F | Common | stacks |

