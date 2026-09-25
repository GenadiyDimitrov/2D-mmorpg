# Item ids — the complete `/give` reference

**Generated from `ItemCatalog`** by `tools/ItemIds` — do not hand-edit; re-run
`dotnet run --project tools/ItemIds` after adding or removing an item. Every id below is a real
id the server will accept today.

**826 items.** Generated 2026-09-25.

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
| `staff_t40_common` | Darksteel Battlestaff | D | Common | TwoHandedBlunt |
| `staff_t40_temp` | Darksteel Battlestaff | D | Common | untradable, TwoHandedBlunt |
| `sword1h_t40_common` | Darksteel Blade | D | Common | Sword |
| `sword1h_t40_temp` | Darksteel Blade | D | Common | untradable, Sword |
| `duals_t40_common` | Darksteel Fangs | D | Common | Dual |
| `duals_t40_temp` | Darksteel Fangs | D | Common | untradable, Dual |
| `sword2h_t40_common` | Darksteel Greatsword | D | Common | TwoHandedSword |
| `sword2h_t40_temp` | Darksteel Greatsword | D | Common | untradable, TwoHandedSword |
| `bow_t40_common` | Darksteel Longbow | D | Common | Bow |
| `bow_t40_temp` | Darksteel Longbow | D | Common | untradable, Bow |
| `blunt1h_t40_common` | Darksteel Mace | D | Common | Blunt |
| `blunt1h_t40_temp` | Darksteel Mace | D | Common | untradable, Blunt |
| `blunt2h_t40_common` | Darksteel Maul | D | Common | TwoHandedBlunt |
| `blunt2h_t40_temp` | Darksteel Maul | D | Common | untradable, TwoHandedBlunt |
| `wand_t40_common` | Darksteel Wand | D | Common | Blunt |
| `wand_t40_temp` | Darksteel Wand | D | Common | untradable, Blunt |
| `staff_t40` | Darksteel Battlestaff | D | Mythic | TwoHandedBlunt |
| `sword1h_t40` | Darksteel Blade | D | Mythic | Sword |
| `duals_t40` | Darksteel Fangs | D | Mythic | Dual |
| `sword2h_t40` | Darksteel Greatsword | D | Mythic | TwoHandedSword |
| `bow_t40` | Darksteel Longbow | D | Mythic | Bow |
| `blunt1h_t40` | Darksteel Mace | D | Mythic | Blunt |
| `blunt2h_t40` | Darksteel Maul | D | Mythic | TwoHandedBlunt |
| `wand_t40` | Darksteel Wand | D | Mythic | Blunt |

### Lv 52

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `staff_t52_common` | Cobalt Battlestaff | C | Common | TwoHandedBlunt |
| `staff_t52_temp` | Cobalt Battlestaff | C | Common | untradable, TwoHandedBlunt |
| `sword1h_t52_common` | Cobalt Blade | C | Common | Sword |
| `sword1h_t52_temp` | Cobalt Blade | C | Common | untradable, Sword |
| `duals_t52_common` | Cobalt Fangs | C | Common | Dual |
| `duals_t52_temp` | Cobalt Fangs | C | Common | untradable, Dual |
| `sword2h_t52_common` | Cobalt Greatsword | C | Common | TwoHandedSword |
| `sword2h_t52_temp` | Cobalt Greatsword | C | Common | untradable, TwoHandedSword |
| `bow_t52_common` | Cobalt Longbow | C | Common | Bow |
| `bow_t52_temp` | Cobalt Longbow | C | Common | untradable, Bow |
| `blunt1h_t52_common` | Cobalt Mace | C | Common | Blunt |
| `blunt1h_t52_temp` | Cobalt Mace | C | Common | untradable, Blunt |
| `blunt2h_t52_common` | Cobalt Maul | C | Common | TwoHandedBlunt |
| `blunt2h_t52_temp` | Cobalt Maul | C | Common | untradable, TwoHandedBlunt |
| `wand_t52_common` | Cobalt Wand | C | Common | Blunt |
| `wand_t52_temp` | Cobalt Wand | C | Common | untradable, Blunt |
| `staff_t52` | Cobalt Battlestaff | C | Mythic | TwoHandedBlunt |
| `sword1h_t52` | Cobalt Blade | C | Mythic | Sword |
| `duals_t52` | Cobalt Fangs | C | Mythic | Dual |
| `sword2h_t52` | Cobalt Greatsword | C | Mythic | TwoHandedSword |
| `bow_t52` | Cobalt Longbow | C | Mythic | Bow |
| `blunt1h_t52` | Cobalt Mace | C | Mythic | Blunt |
| `blunt2h_t52` | Cobalt Maul | C | Mythic | TwoHandedBlunt |
| `wand_t52` | Cobalt Wand | C | Mythic | Blunt |

### Lv 61

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `staff_t61_common` | Bloodsteel Battlestaff | B | Common | TwoHandedBlunt |
| `sword1h_t61_common` | Bloodsteel Blade | B | Common | Sword |
| `duals_t61_common` | Bloodsteel Fangs | B | Common | Dual |
| `sword2h_t61_common` | Bloodsteel Greatsword | B | Common | TwoHandedSword |
| `bow_t61_common` | Bloodsteel Longbow | B | Common | Bow |
| `blunt1h_t61_common` | Bloodsteel Mace | B | Common | Blunt |
| `blunt2h_t61_common` | Bloodsteel Maul | B | Common | TwoHandedBlunt |
| `wand_t61_common` | Bloodsteel Wand | B | Common | Blunt |
| `staff_t61` | Bloodsteel Battlestaff | B | Mythic | TwoHandedBlunt |
| `sword1h_t61` | Bloodsteel Blade | B | Mythic | Sword |
| `duals_t61` | Bloodsteel Fangs | B | Mythic | Dual |
| `sword2h_t61` | Bloodsteel Greatsword | B | Mythic | TwoHandedSword |
| `bow_t61` | Bloodsteel Longbow | B | Mythic | Bow |
| `blunt1h_t61` | Bloodsteel Mace | B | Mythic | Blunt |
| `blunt2h_t61` | Bloodsteel Maul | B | Mythic | TwoHandedBlunt |
| `wand_t61` | Bloodsteel Wand | B | Mythic | Blunt |

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
| `shield_t40_common` | Darksteel Aegis | D | Common |  |
| `shield_t40_temp` | Darksteel Aegis | D | Common | untradable |
| `shield_t40` | Darksteel Aegis | D | Mythic |  |

### Lv 52

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t52_common` | Cobalt Aegis | C | Common |  |
| `shield_t52_temp` | Cobalt Aegis | C | Common | untradable |
| `shield_t52` | Cobalt Aegis | C | Mythic |  |

### Lv 61

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t61_common` | Bloodsteel Aegis | B | Common |  |
| `shield_t61` | Bloodsteel Aegis | B | Mythic |  |

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
| `heavy_t40_common` | Darksteel Bulwark | D | Common | Heavy, Body |
| `heavy_t40_temp` | Darksteel Bulwark | D | Common | untradable, Heavy, Body |
| `gloves_t40_common` | Darksteel Gauntlets | D | Common | Gloves |
| `gloves_t40_temp` | Darksteel Gauntlets | D | Common | untradable, Gloves |
| `boots_t40_common` | Darksteel Greaves | D | Common | Boots |
| `boots_t40_temp` | Darksteel Greaves | D | Common | untradable, Boots |
| `helm_t40_common` | Darksteel Helm | D | Common | Head |
| `helm_t40_temp` | Darksteel Helm | D | Common | untradable, Head |
| `light_t40_common` | Darksteel Leathers | D | Common | Light, Body |
| `light_t40_temp` | Darksteel Leathers | D | Common | untradable, Light, Body |
| `robe_t40_common` | Darksteel Robe | D | Common | Robe, Body |
| `robe_t40_temp` | Darksteel Robe | D | Common | untradable, Robe, Body |
| `light_t40_str` | Darksteel Brawlhide | D | Mythic | Light, Body |
| `heavy_t40` | Darksteel Bulwark | D | Mythic | Heavy, Body |
| `gloves_t40` | Darksteel Gauntlets | D | Mythic | Gloves |
| `boots_t40` | Darksteel Greaves | D | Mythic | Boots |
| `light_t40_pdef` | Darksteel Guardhide | D | Mythic | Light, Body |
| `helm_t40` | Darksteel Helm | D | Mythic | Head |
| `light_t40` | Darksteel Leathers | D | Mythic | Light, Body |
| `robe_t40_sup` | Darksteel Raiment | D | Mythic | Robe, Body |
| `robe_t40` | Darksteel Robe | D | Mythic | Robe, Body |
| `robe_t40_nuke` | Darksteel Vestments | D | Mythic | Robe, Body |
| `light_t40_mdef` | Darksteel Wardhide | D | Mythic | Light, Body |

### Lv 52

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `heavy_t52_common` | Cobalt Bulwark | C | Common | Heavy, Body |
| `heavy_t52_temp` | Cobalt Bulwark | C | Common | untradable, Heavy, Body |
| `gloves_t52_common` | Cobalt Gauntlets | C | Common | Gloves |
| `gloves_t52_temp` | Cobalt Gauntlets | C | Common | untradable, Gloves |
| `boots_t52_common` | Cobalt Greaves | C | Common | Boots |
| `boots_t52_temp` | Cobalt Greaves | C | Common | untradable, Boots |
| `helm_t52_common` | Cobalt Helm | C | Common | Head |
| `helm_t52_temp` | Cobalt Helm | C | Common | untradable, Head |
| `light_t52_common` | Cobalt Leathers | C | Common | Light, Body |
| `light_t52_temp` | Cobalt Leathers | C | Common | untradable, Light, Body |
| `robe_t52_common` | Cobalt Robe | C | Common | Robe, Body |
| `robe_t52_temp` | Cobalt Robe | C | Common | untradable, Robe, Body |
| `heavy_t52` | Cobalt Bulwark | C | Mythic | Heavy, Body |
| `gloves_t52` | Cobalt Gauntlets | C | Mythic | Gloves |
| `boots_t52` | Cobalt Greaves | C | Mythic | Boots |
| `helm_t52` | Cobalt Helm | C | Mythic | Head |
| `light_t52` | Cobalt Leathers | C | Mythic | Light, Body |
| `robe_t52` | Cobalt Robe | C | Mythic | Robe, Body |
| `light_t52_sup` | Cobalt Sagehide | C | Mythic | Light, Body |
| `heavy_t52_dmg` | Cobalt Warplate | C | Mythic | Heavy, Body |

### Lv 61

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `heavy_t61_common` | Bloodsteel Bulwark | B | Common | Heavy, Body |
| `gloves_t61_common` | Bloodsteel Gauntlets | B | Common | Gloves |
| `boots_t61_common` | Bloodsteel Greaves | B | Common | Boots |
| `helm_t61_common` | Bloodsteel Helm | B | Common | Head |
| `light_t61_common` | Bloodsteel Leathers | B | Common | Light, Body |
| `robe_t61_common` | Bloodsteel Robe | B | Common | Robe, Body |
| `heavy_t61` | Bloodsteel Bulwark | B | Mythic | Heavy, Body |
| `gloves_t61` | Bloodsteel Gauntlets | B | Mythic | Gloves |
| `boots_t61` | Bloodsteel Greaves | B | Mythic | Boots |
| `helm_t61` | Bloodsteel Helm | B | Mythic | Head |
| `light_t61` | Bloodsteel Leathers | B | Mythic | Light, Body |
| `robe_t61_sup` | Bloodsteel Raiment | B | Mythic | Robe, Body |
| `robe_t61` | Bloodsteel Robe | B | Mythic | Robe, Body |
| `light_t61_dmg` | Bloodsteel Warhide | B | Mythic | Light, Body |
| `heavy_t61_dmg` | Bloodsteel Warplate | B | Mythic | Heavy, Body |

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
| `ring_t40_common` | Darksteel Band | D | Common | Ring |
| `necklace_t40_common` | Darksteel Pendant | D | Common | Necklace |
| `earring_t40_common` | Darksteel Stud | D | Common | Earring |
| `ring_t40` | Darksteel Band | D | Mythic | Ring |
| `necklace_t40` | Darksteel Pendant | D | Mythic | Necklace |
| `earring_t40` | Darksteel Stud | D | Mythic | Earring |

### Lv 52

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t52_common` | Cobalt Band | C | Common | Ring |
| `necklace_t52_common` | Cobalt Pendant | C | Common | Necklace |
| `earring_t52_common` | Cobalt Stud | C | Common | Earring |
| `ring_t52` | Cobalt Band | C | Mythic | Ring |
| `necklace_t52` | Cobalt Pendant | C | Mythic | Necklace |
| `earring_t52` | Cobalt Stud | C | Mythic | Earring |

### Lv 61

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t61_common` | Bloodsteel Band | B | Common | Ring |
| `necklace_t61_common` | Bloodsteel Pendant | B | Common | Necklace |
| `earring_t61_common` | Bloodsteel Stud | B | Common | Earring |
| `ring_t61` | Bloodsteel Band | B | Mythic | Ring |
| `necklace_t61` | Bloodsteel Pendant | B | Mythic | Necklace |
| `earring_t61` | Bloodsteel Stud | B | Mythic | Earring |

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
| `rune_blessing_boost_1h` | Blessing Booster Rune (1h) | - | Mythic | untradable |
| `rune_blessing_boost_2h` | Blessing Booster Rune (2h) | - | Mythic | untradable |
| `rune_favor_keep_1h` | Favor Keep-Rune (1h) | - | Mythic | untradable |
| `rune_favor_keep_2h` | Favor Keep-Rune (2h) | - | Mythic | untradable |
| `rune_grand` | Grand Rune | - | Mythic | untradable |
| `rune_drop_10` | Rune of Drop (10%) | - | Mythic | untradable |
| `rune_drop_100` | Rune of Drop (100%) | - | Mythic | untradable |
| `rune_drop_20` | Rune of Drop (20%) | - | Mythic | untradable |
| `rune_drop_30` | Rune of Drop (30%) | - | Mythic | untradable |
| `rune_drop_40` | Rune of Drop (40%) | - | Mythic | untradable |
| `rune_drop_5` | Rune of Drop (5%) | - | Mythic | untradable |
| `rune_drop_50` | Rune of Drop (50%) | - | Mythic | untradable |
| `rune_drop_60` | Rune of Drop (60%) | - | Mythic | untradable |
| `rune_drop_70` | Rune of Drop (70%) | - | Mythic | untradable |
| `rune_drop_80` | Rune of Drop (80%) | - | Mythic | untradable |
| `rune_drop_90` | Rune of Drop (90%) | - | Mythic | untradable |
| `rune_expsp_10` | Rune of Exp/SP (10%) | - | Mythic | untradable |
| `rune_expsp_100` | Rune of Exp/SP (100%) | - | Mythic | untradable |
| `rune_expsp_20` | Rune of Exp/SP (20%) | - | Mythic | untradable |
| `rune_expsp_30` | Rune of Exp/SP (30%) | - | Mythic | untradable |
| `rune_expsp_40` | Rune of Exp/SP (40%) | - | Mythic | untradable |
| `rune_expsp_5` | Rune of Exp/SP (5%) | - | Mythic | untradable |
| `rune_expsp_50` | Rune of Exp/SP (50%) | - | Mythic | untradable |
| `rune_expsp_60` | Rune of Exp/SP (60%) | - | Mythic | untradable |
| `rune_expsp_70` | Rune of Exp/SP (70%) | - | Mythic | untradable |
| `rune_expsp_80` | Rune of Exp/SP (80%) | - | Mythic | untradable |
| `rune_expsp_90` | Rune of Exp/SP (90%) | - | Mythic | untradable |
| `rune_exp_10` | Rune of Experience (10%) | - | Mythic | untradable |
| `rune_exp_100` | Rune of Experience (100%) | - | Mythic | untradable |
| `rune_exp_20` | Rune of Experience (20%) | - | Mythic | untradable |
| `rune_exp_30` | Rune of Experience (30%) | - | Mythic | untradable |
| `rune_exp_40` | Rune of Experience (40%) | - | Mythic | untradable |
| `rune_exp_5` | Rune of Experience (5%) | - | Mythic | untradable |
| `rune_exp_50` | Rune of Experience (50%) | - | Mythic | untradable |
| `rune_exp_60` | Rune of Experience (60%) | - | Mythic | untradable |
| `rune_exp_70` | Rune of Experience (70%) | - | Mythic | untradable |
| `rune_exp_80` | Rune of Experience (80%) | - | Mythic | untradable |
| `rune_exp_90` | Rune of Experience (90%) | - | Mythic | untradable |
| `rune_gold_10` | Rune of Gold (10%) | - | Mythic | untradable |
| `rune_gold_100` | Rune of Gold (100%) | - | Mythic | untradable |
| `rune_gold_20` | Rune of Gold (20%) | - | Mythic | untradable |
| `rune_gold_30` | Rune of Gold (30%) | - | Mythic | untradable |
| `rune_gold_40` | Rune of Gold (40%) | - | Mythic | untradable |
| `rune_gold_5` | Rune of Gold (5%) | - | Mythic | untradable |
| `rune_gold_50` | Rune of Gold (50%) | - | Mythic | untradable |
| `rune_gold_60` | Rune of Gold (60%) | - | Mythic | untradable |
| `rune_gold_70` | Rune of Gold (70%) | - | Mythic | untradable |
| `rune_gold_80` | Rune of Gold (80%) | - | Mythic | untradable |
| `rune_gold_90` | Rune of Gold (90%) | - | Mythic | untradable |
| `rune_sinister` | Rune of Sinister | - | Mythic | untradable |
| `rune_sinners` | Rune of Sinners | - | Mythic | untradable, **soulbound** |
| `rune_sp_10` | Rune of Skillpoints (10%) | - | Mythic | untradable |
| `rune_sp_100` | Rune of Skillpoints (100%) | - | Mythic | untradable |
| `rune_sp_20` | Rune of Skillpoints (20%) | - | Mythic | untradable |
| `rune_sp_30` | Rune of Skillpoints (30%) | - | Mythic | untradable |
| `rune_sp_40` | Rune of Skillpoints (40%) | - | Mythic | untradable |
| `rune_sp_5` | Rune of Skillpoints (5%) | - | Mythic | untradable |
| `rune_sp_50` | Rune of Skillpoints (50%) | - | Mythic | untradable |
| `rune_sp_60` | Rune of Skillpoints (60%) | - | Mythic | untradable |
| `rune_sp_70` | Rune of Skillpoints (70%) | - | Mythic | untradable |
| `rune_sp_80` | Rune of Skillpoints (80%) | - | Mythic | untradable |
| `rune_sp_90` | Rune of Skillpoints (90%) | - | Mythic | untradable |
| `rune_spell` | Spell Rune | - | Mythic | untradable |
| `rune_war` | War Rune | - | Mythic | untradable |

## Consumables (potions)  (66)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `potion_eva_u` | Agility Potion | - | Uncommon | stacks |
| `potion_eva_c` | Agility Potion (Lesser) | - | Common | stacks |
| `potion_acc_u` | Aim Potion | - | Uncommon | stacks |
| `potion_acc_c` | Aim Potion (Lesser) | - | Common | stacks |
| `potion_cast_u` | Alacrity Potion | - | Uncommon | stacks |
| `potion_cast_c` | Alacrity Potion (Lesser) | - | Common | stacks |
| `potion_pdef_u` | Bulwark Potion | - | Uncommon | stacks |
| `potion_pdef_c` | Bulwark Potion (Lesser) | - | Common | stacks |
| `potion_minor` | Common Healing Potion | - | Common | stacks |
| `potion_mana_minor` | Common Mana Potion | - | Common | stacks |
| `potion_dash_u` | Dash Potion | - | Uncommon | stacks |
| `potion_dash_l` | Dash Potion (Grand) | - | Legendary | stacks |
| `potion_dash_r` | Dash Potion (Greater) | - | Rare | stacks |
| `potion_dash_c` | Dash Potion (Lesser) | - | Common | stacks |
| `potion_dash_e` | Dash Potion (Superior) | - | Epic | stacks |
| `potion_dash_m` | Dash Potion (Supreme) | - | Mythic | stacks |
| `potion_dash_m_bound` | Dash Potion (Supreme) (Bound) | - | Mythic | untradable, stacks |
| `elemental_stone` | Elemental Stone | - | Rare | stacks |
| `potion_favor_restore` | Favor Restore Potion | - | Mythic | untradable, stacks |
| `potion_matk_u` | Force Potion | - | Uncommon | stacks |
| `potion_matk_c` | Force Potion (Lesser) | - | Common | stacks |
| `potion_atk_u` | Fury Potion | - | Uncommon | stacks |
| `potion_atk_c` | Fury Potion (Lesser) | - | Common | stacks |
| `holy_stone` | Holy Stone | - | Rare | stacks |
| `potion_instant` | Instant Healing Potion | - | Rare | stacks |
| `potion_instant_bound` | Instant Healing Potion (Bound) | - | Rare | untradable, stacks |
| `potion_patk_u` | Might Potion | - | Uncommon | stacks |
| `potion_patk_c` | Might Potion (Lesser) | - | Common | stacks |
| `physical_stone` | Physical Stone | - | Rare | stacks |
| `potion_greater` | Rare Healing Potion | - | Rare | stacks |
| `potion_mana_greater` | Rare Mana Potion | - | Rare | stacks |
| `rune_title_colour` | Rune of Tincture | - | Uncommon | stacks |
| `scroll_eva_r` | Scroll of Agility | - | Rare | untradable, stacks |
| `scroll_acc_r` | Scroll of Aim | - | Rare | untradable, stacks |
| `scroll_cast_r` | Scroll of Alacrity | - | Rare | untradable, stacks |
| `scroll_hp_m` | Scroll of Body | - | Rare | untradable, stacks |
| `scroll_pdef_r` | Scroll of Bulwark | - | Rare | untradable, stacks |
| `scroll_critdmg_m` | Scroll of Ferocity | - | Rare | untradable, stacks |
| `scroll_crit_m` | Scroll of Focus | - | Rare | untradable, stacks |
| `scroll_matk_r` | Scroll of Force | - | Rare | untradable, stacks |
| `scroll_frenzy_m` | Scroll of Frenzy | - | Rare | untradable, stacks |
| `scroll_atk_r` | Scroll of Fury | - | Rare | untradable, stacks |
| `scroll_mcrit_m` | Scroll of Insight | - | Rare | untradable, stacks |
| `scroll_patk_r` | Scroll of Might | - | Rare | untradable, stacks |
| `scroll_interrupt_m` | Scroll of Resolve | - | Rare | untradable, stacks |
| `scroll_resurrect` | Scroll of Resurrection | - | Uncommon | stacks |
| `scroll_return` | Scroll of Return | - | Common | stacks |
| `scroll_mpreg_m` | Scroll of Serenity | - | Rare | untradable, stacks |
| `scroll_mp_m` | Scroll of Soul | - | Rare | untradable, stacks |
| `scroll_speed_r` | Scroll of Swift | - | Rare | untradable, stacks |
| `scroll_vamp_m` | Scroll of Vampirism | - | Rare | untradable, stacks |
| `scroll_hpreg_m` | Scroll of Vigor | - | Rare | untradable, stacks |
| `scroll_mdef_r` | Scroll of Ward | - | Rare | untradable, stacks |
| `skill_stone` | Skill Stone | - | Uncommon | stacks |
| `sp_bottle` | SP Bottle | - | Epic | stacks |
| `subclass_ticket` | Subclass Ticket | - | Mythic | untradable, stacks |
| `potion_speed_u` | Swift Potion | - | Uncommon | stacks |
| `potion_speed_c` | Swift Potion (Lesser) | - | Common | stacks |
| `scroll_resurrect_ultimate` | Ultimate Scroll of Resurrection | - | Rare | stacks |
| `scroll_resurrect_ultimate_bound` | Ultimate Scroll of Resurrection (Bound) | - | Rare | untradable, stacks |
| `scroll_return_ultimate` | Ultimate Scroll of Return | - | Rare | untradable, stacks |
| `scroll_return_ultimate_bound` | Ultimate Scroll of Return (Bound) | - | Rare | untradable, stacks |
| `potion_healing` | Uncommon Healing Potion | - | Uncommon | stacks |
| `potion_mana` | Uncommon Mana Potion | - | Uncommon | stacks |
| `potion_mdef_u` | Ward Potion | - | Uncommon | stacks |
| `potion_mdef_c` | Ward Potion (Lesser) | - | Common | stacks |

## Scrolls  (24)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `attrscroll_common` | Attribute Scroll (Common) | - | Common | stacks |
| `attrscroll_epic` | Attribute Scroll (Epic) | - | Epic | stacks |
| `attrscroll_legendary` | Attribute Scroll (Legendary) | - | Legendary | stacks |
| `attrscroll_mythic` | Attribute Scroll (Mythic) | - | Mythic | stacks |
| `attrscroll_rare` | Attribute Scroll (Rare) | - | Rare | stacks |
| `attrscroll_uncommon` | Attribute Scroll (Uncommon) | - | Uncommon | stacks |
| `scroll_greater_a` | Greater Scroll of Enchant (A) | - | Legendary | stacks |
| `scroll_greater_b` | Greater Scroll of Enchant (B) | - | Epic | stacks |
| `scroll_greater_c` | Greater Scroll of Enchant (C) | - | Rare | stacks |
| `scroll_greater_d` | Greater Scroll of Enchant (D) | - | Uncommon | stacks |
| `scroll_greater_e` | Greater Scroll of Enchant (E) | - | Common | stacks |
| `scroll_greater_s` | Greater Scroll of Enchant (S) | - | Mythic | stacks |
| `scroll_safe_a` | Safe Scroll of Enchant (A) | - | Legendary | stacks |
| `scroll_safe_b` | Safe Scroll of Enchant (B) | - | Epic | stacks |
| `scroll_safe_c` | Safe Scroll of Enchant (C) | - | Rare | stacks |
| `scroll_safe_d` | Safe Scroll of Enchant (D) | - | Uncommon | stacks |
| `scroll_safe_e` | Safe Scroll of Enchant (E) | - | Common | stacks |
| `scroll_safe_s` | Safe Scroll of Enchant (S) | - | Mythic | stacks |
| `scroll_enchant_a` | Scroll of Enchant (A) | - | Legendary | stacks |
| `scroll_enchant_b` | Scroll of Enchant (B) | - | Epic | stacks |
| `scroll_rare` | Scroll of Enchant (C) | - | Rare | stacks |
| `scroll_uncommon` | Scroll of Enchant (D) | - | Uncommon | stacks |
| `scroll_common` | Scroll of Enchant (E) | - | Common | stacks |
| `scroll_enchant_s` | Scroll of Enchant (S) | - | Mythic | stacks |

## Boxes  (215)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `box_acc_t76` | Adamantine Accessory Box | A | Rare | stacks |
| `box_buff_scrolls` | Blessing Box | - | Rare | stacks |
| `box_acc_t61` | Bloodsteel Accessory Box | B | Rare | stacks |
| `box_acc_t52` | Cobalt Accessory Box | C | Rare | stacks |
| `box_acc_t40` | Darksteel Accessory Box | D | Rare | stacks |
| `box_acc_t20` | Electrum Accessory Box | E | Rare | stacks |
| `box_acc_t1` | Ferrite Accessory Box | - | Rare | stacks |
| `box_grand_rune_24h` | Grand Rune Box (1d) | - | Rare | untradable, stacks |
| `box_newbie_armor_choice` | Newbie Armor Set | F | Common | untradable, stacks |
| `box_newbie` | Newbie Box | - | Common | untradable, stacks |
| `box_newbie_jewels` | Newbie Jewels Box | F | Common | untradable, stacks |
| `box_newbie_armor_light` | Newbie Light Armor Box | F | Common | untradable, stacks |
| `box_newbie_armor_robe` | Newbie Robe Armor Box | F | Common | untradable, stacks |
| `box_newbie_rune_choice` | Newbie Rune | - | Common | untradable, stacks |
| `box_newbie_weapons` | Newbie Weapons Box | F | Common | untradable, stacks |
| `recipe_craft_shield_t76_20` | Recipe: Adamantine Aegis (20%) | - | Common | stacks |
| `recipe_craft_shield_t76_40` | Recipe: Adamantine Aegis (40%) | - | Common | stacks |
| `recipe_craft_shield_t76_60` | Recipe: Adamantine Aegis (60%) | - | Common | stacks |
| `recipe_craft_ring_t76_20` | Recipe: Adamantine Band (20%) | - | Common | stacks |
| `recipe_craft_ring_t76_40` | Recipe: Adamantine Band (40%) | - | Common | stacks |
| `recipe_craft_ring_t76_60` | Recipe: Adamantine Band (60%) | - | Common | stacks |
| `recipe_craft_staff_t76_20` | Recipe: Adamantine Battlestaff (20%) | - | Common | stacks |
| `recipe_craft_staff_t76_40` | Recipe: Adamantine Battlestaff (40%) | - | Common | stacks |
| `recipe_craft_staff_t76_60` | Recipe: Adamantine Battlestaff (60%) | - | Common | stacks |
| `recipe_craft_sword1h_t76_20` | Recipe: Adamantine Blade (20%) | - | Common | stacks |
| `recipe_craft_sword1h_t76_40` | Recipe: Adamantine Blade (40%) | - | Common | stacks |
| `recipe_craft_sword1h_t76_60` | Recipe: Adamantine Blade (60%) | - | Common | stacks |
| `recipe_craft_heavy_t76_20` | Recipe: Adamantine Bulwark (20%) | - | Common | stacks |
| `recipe_craft_heavy_t76_40` | Recipe: Adamantine Bulwark (40%) | - | Common | stacks |
| `recipe_craft_heavy_t76_60` | Recipe: Adamantine Bulwark (60%) | - | Common | stacks |
| `recipe_craft_duals_t76_20` | Recipe: Adamantine Fangs (20%) | - | Common | stacks |
| `recipe_craft_duals_t76_40` | Recipe: Adamantine Fangs (40%) | - | Common | stacks |
| `recipe_craft_duals_t76_60` | Recipe: Adamantine Fangs (60%) | - | Common | stacks |
| `recipe_craft_gloves_t76_20` | Recipe: Adamantine Gauntlets (20%) | - | Common | stacks |
| `recipe_craft_gloves_t76_40` | Recipe: Adamantine Gauntlets (40%) | - | Common | stacks |
| `recipe_craft_gloves_t76_60` | Recipe: Adamantine Gauntlets (60%) | - | Common | stacks |
| `recipe_craft_sword2h_t76_20` | Recipe: Adamantine Greatsword (20%) | - | Common | stacks |
| `recipe_craft_sword2h_t76_40` | Recipe: Adamantine Greatsword (40%) | - | Common | stacks |
| `recipe_craft_sword2h_t76_60` | Recipe: Adamantine Greatsword (60%) | - | Common | stacks |
| `recipe_craft_boots_t76_20` | Recipe: Adamantine Greaves (20%) | - | Common | stacks |
| `recipe_craft_boots_t76_40` | Recipe: Adamantine Greaves (40%) | - | Common | stacks |
| `recipe_craft_boots_t76_60` | Recipe: Adamantine Greaves (60%) | - | Common | stacks |
| `recipe_craft_helm_t76_20` | Recipe: Adamantine Helm (20%) | - | Common | stacks |
| `recipe_craft_helm_t76_40` | Recipe: Adamantine Helm (40%) | - | Common | stacks |
| `recipe_craft_helm_t76_60` | Recipe: Adamantine Helm (60%) | - | Common | stacks |
| `recipe_craft_light_t76_20` | Recipe: Adamantine Leathers (20%) | - | Common | stacks |
| `recipe_craft_light_t76_40` | Recipe: Adamantine Leathers (40%) | - | Common | stacks |
| `recipe_craft_light_t76_60` | Recipe: Adamantine Leathers (60%) | - | Common | stacks |
| `recipe_craft_bow_t76_20` | Recipe: Adamantine Longbow (20%) | - | Common | stacks |
| `recipe_craft_bow_t76_40` | Recipe: Adamantine Longbow (40%) | - | Common | stacks |
| `recipe_craft_bow_t76_60` | Recipe: Adamantine Longbow (60%) | - | Common | stacks |
| `recipe_craft_blunt1h_t76_20` | Recipe: Adamantine Mace (20%) | - | Common | stacks |
| `recipe_craft_blunt1h_t76_40` | Recipe: Adamantine Mace (40%) | - | Common | stacks |
| `recipe_craft_blunt1h_t76_60` | Recipe: Adamantine Mace (60%) | - | Common | stacks |
| `recipe_craft_blunt2h_t76_20` | Recipe: Adamantine Maul (20%) | - | Common | stacks |
| `recipe_craft_blunt2h_t76_40` | Recipe: Adamantine Maul (40%) | - | Common | stacks |
| `recipe_craft_blunt2h_t76_60` | Recipe: Adamantine Maul (60%) | - | Common | stacks |
| `recipe_craft_necklace_t76_20` | Recipe: Adamantine Pendant (20%) | - | Common | stacks |
| `recipe_craft_necklace_t76_40` | Recipe: Adamantine Pendant (40%) | - | Common | stacks |
| `recipe_craft_necklace_t76_60` | Recipe: Adamantine Pendant (60%) | - | Common | stacks |
| `recipe_craft_robe_t76_20` | Recipe: Adamantine Robe (20%) | - | Common | stacks |
| `recipe_craft_robe_t76_40` | Recipe: Adamantine Robe (40%) | - | Common | stacks |
| `recipe_craft_robe_t76_60` | Recipe: Adamantine Robe (60%) | - | Common | stacks |
| `recipe_craft_earring_t76_20` | Recipe: Adamantine Stud (20%) | - | Common | stacks |
| `recipe_craft_earring_t76_40` | Recipe: Adamantine Stud (40%) | - | Common | stacks |
| `recipe_craft_earring_t76_60` | Recipe: Adamantine Stud (60%) | - | Common | stacks |
| `recipe_craft_wand_t76_20` | Recipe: Adamantine Wand (20%) | - | Common | stacks |
| `recipe_craft_wand_t76_40` | Recipe: Adamantine Wand (40%) | - | Common | stacks |
| `recipe_craft_wand_t76_60` | Recipe: Adamantine Wand (60%) | - | Common | stacks |
| `recipe_craft_crafter_hammer_40` | Recipe: Blacksmith's Hammer (40%) | - | Common | untradable, stacks |
| `recipe_craft_shield_t61_100` | Recipe: Bloodsteel Aegis (100%) | - | Common | stacks |
| `recipe_craft_shield_t61_60` | Recipe: Bloodsteel Aegis (60%) | - | Common | stacks |
| `recipe_craft_ring_t61_100` | Recipe: Bloodsteel Band (100%) | - | Common | stacks |
| `recipe_craft_ring_t61_60` | Recipe: Bloodsteel Band (60%) | - | Common | stacks |
| `recipe_craft_staff_t61_100` | Recipe: Bloodsteel Battlestaff (100%) | - | Common | stacks |
| `recipe_craft_staff_t61_60` | Recipe: Bloodsteel Battlestaff (60%) | - | Common | stacks |
| `recipe_craft_sword1h_t61_100` | Recipe: Bloodsteel Blade (100%) | - | Common | stacks |
| `recipe_craft_sword1h_t61_60` | Recipe: Bloodsteel Blade (60%) | - | Common | stacks |
| `recipe_craft_heavy_t61_100` | Recipe: Bloodsteel Bulwark (100%) | - | Common | stacks |
| `recipe_craft_heavy_t61_60` | Recipe: Bloodsteel Bulwark (60%) | - | Common | stacks |
| `recipe_craft_duals_t61_100` | Recipe: Bloodsteel Fangs (100%) | - | Common | stacks |
| `recipe_craft_duals_t61_60` | Recipe: Bloodsteel Fangs (60%) | - | Common | stacks |
| `recipe_craft_gloves_t61_100` | Recipe: Bloodsteel Gauntlets (100%) | - | Common | stacks |
| `recipe_craft_gloves_t61_60` | Recipe: Bloodsteel Gauntlets (60%) | - | Common | stacks |
| `recipe_craft_sword2h_t61_100` | Recipe: Bloodsteel Greatsword (100%) | - | Common | stacks |
| `recipe_craft_sword2h_t61_60` | Recipe: Bloodsteel Greatsword (60%) | - | Common | stacks |
| `recipe_craft_boots_t61_100` | Recipe: Bloodsteel Greaves (100%) | - | Common | stacks |
| `recipe_craft_boots_t61_60` | Recipe: Bloodsteel Greaves (60%) | - | Common | stacks |
| `recipe_craft_helm_t61_100` | Recipe: Bloodsteel Helm (100%) | - | Common | stacks |
| `recipe_craft_helm_t61_60` | Recipe: Bloodsteel Helm (60%) | - | Common | stacks |
| `recipe_craft_light_t61_100` | Recipe: Bloodsteel Leathers (100%) | - | Common | stacks |
| `recipe_craft_light_t61_60` | Recipe: Bloodsteel Leathers (60%) | - | Common | stacks |
| `recipe_craft_bow_t61_100` | Recipe: Bloodsteel Longbow (100%) | - | Common | stacks |
| `recipe_craft_bow_t61_60` | Recipe: Bloodsteel Longbow (60%) | - | Common | stacks |
| `recipe_craft_blunt1h_t61_100` | Recipe: Bloodsteel Mace (100%) | - | Common | stacks |
| `recipe_craft_blunt1h_t61_60` | Recipe: Bloodsteel Mace (60%) | - | Common | stacks |
| `recipe_craft_blunt2h_t61_100` | Recipe: Bloodsteel Maul (100%) | - | Common | stacks |
| `recipe_craft_blunt2h_t61_60` | Recipe: Bloodsteel Maul (60%) | - | Common | stacks |
| `recipe_craft_necklace_t61_100` | Recipe: Bloodsteel Pendant (100%) | - | Common | stacks |
| `recipe_craft_necklace_t61_60` | Recipe: Bloodsteel Pendant (60%) | - | Common | stacks |
| `recipe_craft_robe_t61_sup_100` | Recipe: Bloodsteel Raiment (100%) | - | Common | stacks |
| `recipe_craft_robe_t61_sup_60` | Recipe: Bloodsteel Raiment (60%) | - | Common | stacks |
| `recipe_craft_robe_t61_100` | Recipe: Bloodsteel Robe (100%) | - | Common | stacks |
| `recipe_craft_robe_t61_60` | Recipe: Bloodsteel Robe (60%) | - | Common | stacks |
| `recipe_craft_earring_t61_100` | Recipe: Bloodsteel Stud (100%) | - | Common | stacks |
| `recipe_craft_earring_t61_60` | Recipe: Bloodsteel Stud (60%) | - | Common | stacks |
| `recipe_craft_wand_t61_100` | Recipe: Bloodsteel Wand (100%) | - | Common | stacks |
| `recipe_craft_wand_t61_60` | Recipe: Bloodsteel Wand (60%) | - | Common | stacks |
| `recipe_craft_light_t61_dmg_100` | Recipe: Bloodsteel Warhide (100%) | - | Common | stacks |
| `recipe_craft_light_t61_dmg_60` | Recipe: Bloodsteel Warhide (60%) | - | Common | stacks |
| `recipe_craft_heavy_t61_dmg_100` | Recipe: Bloodsteel Warplate (100%) | - | Common | stacks |
| `recipe_craft_heavy_t61_dmg_60` | Recipe: Bloodsteel Warplate (60%) | - | Common | stacks |
| `recipe_craft_shield_t52_100` | Recipe: Cobalt Aegis (100%) | - | Common | stacks |
| `recipe_craft_ring_t52_100` | Recipe: Cobalt Band (100%) | - | Common | stacks |
| `recipe_craft_staff_t52_100` | Recipe: Cobalt Battlestaff (100%) | - | Common | stacks |
| `recipe_craft_sword1h_t52_100` | Recipe: Cobalt Blade (100%) | - | Common | stacks |
| `recipe_craft_heavy_t52_100` | Recipe: Cobalt Bulwark (100%) | - | Common | stacks |
| `recipe_craft_duals_t52_100` | Recipe: Cobalt Fangs (100%) | - | Common | stacks |
| `recipe_craft_gloves_t52_100` | Recipe: Cobalt Gauntlets (100%) | - | Common | stacks |
| `recipe_craft_sword2h_t52_100` | Recipe: Cobalt Greatsword (100%) | - | Common | stacks |
| `recipe_craft_boots_t52_100` | Recipe: Cobalt Greaves (100%) | - | Common | stacks |
| `recipe_craft_helm_t52_100` | Recipe: Cobalt Helm (100%) | - | Common | stacks |
| `recipe_craft_light_t52_100` | Recipe: Cobalt Leathers (100%) | - | Common | stacks |
| `recipe_craft_bow_t52_100` | Recipe: Cobalt Longbow (100%) | - | Common | stacks |
| `recipe_craft_blunt1h_t52_100` | Recipe: Cobalt Mace (100%) | - | Common | stacks |
| `recipe_craft_blunt2h_t52_100` | Recipe: Cobalt Maul (100%) | - | Common | stacks |
| `recipe_craft_necklace_t52_100` | Recipe: Cobalt Pendant (100%) | - | Common | stacks |
| `recipe_craft_robe_t52_100` | Recipe: Cobalt Robe (100%) | - | Common | stacks |
| `recipe_craft_light_t52_sup_100` | Recipe: Cobalt Sagehide (100%) | - | Common | stacks |
| `recipe_craft_earring_t52_100` | Recipe: Cobalt Stud (100%) | - | Common | stacks |
| `recipe_craft_wand_t52_100` | Recipe: Cobalt Wand (100%) | - | Common | stacks |
| `recipe_craft_heavy_t52_dmg_100` | Recipe: Cobalt Warplate (100%) | - | Common | stacks |
| `recipe_craft_shield_t40_100` | Recipe: Darksteel Aegis (100%) | - | Common | stacks |
| `recipe_craft_ring_t40_100` | Recipe: Darksteel Band (100%) | - | Common | stacks |
| `recipe_craft_staff_t40_100` | Recipe: Darksteel Battlestaff (100%) | - | Common | stacks |
| `recipe_craft_sword1h_t40_100` | Recipe: Darksteel Blade (100%) | - | Common | stacks |
| `recipe_craft_light_t40_str_100` | Recipe: Darksteel Brawlhide (100%) | - | Common | stacks |
| `recipe_craft_heavy_t40_100` | Recipe: Darksteel Bulwark (100%) | - | Common | stacks |
| `recipe_craft_duals_t40_100` | Recipe: Darksteel Fangs (100%) | - | Common | stacks |
| `recipe_craft_gloves_t40_100` | Recipe: Darksteel Gauntlets (100%) | - | Common | stacks |
| `recipe_craft_sword2h_t40_100` | Recipe: Darksteel Greatsword (100%) | - | Common | stacks |
| `recipe_craft_boots_t40_100` | Recipe: Darksteel Greaves (100%) | - | Common | stacks |
| `recipe_craft_light_t40_pdef_100` | Recipe: Darksteel Guardhide (100%) | - | Common | stacks |
| `recipe_craft_helm_t40_100` | Recipe: Darksteel Helm (100%) | - | Common | stacks |
| `recipe_craft_light_t40_100` | Recipe: Darksteel Leathers (100%) | - | Common | stacks |
| `recipe_craft_bow_t40_100` | Recipe: Darksteel Longbow (100%) | - | Common | stacks |
| `recipe_craft_blunt1h_t40_100` | Recipe: Darksteel Mace (100%) | - | Common | stacks |
| `recipe_craft_blunt2h_t40_100` | Recipe: Darksteel Maul (100%) | - | Common | stacks |
| `recipe_craft_necklace_t40_100` | Recipe: Darksteel Pendant (100%) | - | Common | stacks |
| `recipe_craft_robe_t40_sup_100` | Recipe: Darksteel Raiment (100%) | - | Common | stacks |
| `recipe_craft_robe_t40_100` | Recipe: Darksteel Robe (100%) | - | Common | stacks |
| `recipe_craft_earring_t40_100` | Recipe: Darksteel Stud (100%) | - | Common | stacks |
| `recipe_craft_robe_t40_nuke_100` | Recipe: Darksteel Vestments (100%) | - | Common | stacks |
| `recipe_craft_wand_t40_100` | Recipe: Darksteel Wand (100%) | - | Common | stacks |
| `recipe_craft_light_t40_mdef_100` | Recipe: Darksteel Wardhide (100%) | - | Common | stacks |
| `recipe_craft_shield_t80_40` | Recipe: Soulcrystal Aegis (40%) | - | Common | stacks |
| `recipe_craft_shield_t80_60` | Recipe: Soulcrystal Aegis (60%) | - | Common | stacks |
| `recipe_craft_ring_t80_40` | Recipe: Soulcrystal Band (40%) | - | Common | stacks |
| `recipe_craft_ring_t80_60` | Recipe: Soulcrystal Band (60%) | - | Common | stacks |
| `recipe_craft_staff_t80_40` | Recipe: Soulcrystal Battlestaff (40%) | - | Common | stacks |
| `recipe_craft_staff_t80_60` | Recipe: Soulcrystal Battlestaff (60%) | - | Common | stacks |
| `recipe_craft_sword1h_t80_40` | Recipe: Soulcrystal Blade (40%) | - | Common | stacks |
| `recipe_craft_sword1h_t80_60` | Recipe: Soulcrystal Blade (60%) | - | Common | stacks |
| `recipe_craft_heavy_t80_40` | Recipe: Soulcrystal Bulwark (40%) | - | Common | stacks |
| `recipe_craft_heavy_t80_60` | Recipe: Soulcrystal Bulwark (60%) | - | Common | stacks |
| `recipe_craft_duals_t80_40` | Recipe: Soulcrystal Fangs (40%) | - | Common | stacks |
| `recipe_craft_duals_t80_60` | Recipe: Soulcrystal Fangs (60%) | - | Common | stacks |
| `recipe_craft_gloves_t80_40` | Recipe: Soulcrystal Gauntlets (40%) | - | Common | stacks |
| `recipe_craft_gloves_t80_60` | Recipe: Soulcrystal Gauntlets (60%) | - | Common | stacks |
| `recipe_craft_sword2h_t80_40` | Recipe: Soulcrystal Greatsword (40%) | - | Common | stacks |
| `recipe_craft_sword2h_t80_60` | Recipe: Soulcrystal Greatsword (60%) | - | Common | stacks |
| `recipe_craft_boots_t80_40` | Recipe: Soulcrystal Greaves (40%) | - | Common | stacks |
| `recipe_craft_boots_t80_60` | Recipe: Soulcrystal Greaves (60%) | - | Common | stacks |
| `recipe_craft_helm_t80_40` | Recipe: Soulcrystal Helm (40%) | - | Common | stacks |
| `recipe_craft_helm_t80_60` | Recipe: Soulcrystal Helm (60%) | - | Common | stacks |
| `recipe_craft_light_t80_40` | Recipe: Soulcrystal Leathers (40%) | - | Common | stacks |
| `recipe_craft_light_t80_60` | Recipe: Soulcrystal Leathers (60%) | - | Common | stacks |
| `recipe_craft_bow_t80_40` | Recipe: Soulcrystal Longbow (40%) | - | Common | stacks |
| `recipe_craft_bow_t80_60` | Recipe: Soulcrystal Longbow (60%) | - | Common | stacks |
| `recipe_craft_blunt1h_t80_40` | Recipe: Soulcrystal Mace (40%) | - | Common | stacks |
| `recipe_craft_blunt1h_t80_60` | Recipe: Soulcrystal Mace (60%) | - | Common | stacks |
| `recipe_craft_blunt2h_t80_40` | Recipe: Soulcrystal Maul (40%) | - | Common | stacks |
| `recipe_craft_blunt2h_t80_60` | Recipe: Soulcrystal Maul (60%) | - | Common | stacks |
| `recipe_craft_necklace_t80_40` | Recipe: Soulcrystal Pendant (40%) | - | Common | stacks |
| `recipe_craft_necklace_t80_60` | Recipe: Soulcrystal Pendant (60%) | - | Common | stacks |
| `recipe_craft_robe_t80_40` | Recipe: Soulcrystal Robe (40%) | - | Common | stacks |
| `recipe_craft_robe_t80_60` | Recipe: Soulcrystal Robe (60%) | - | Common | stacks |
| `recipe_craft_earring_t80_40` | Recipe: Soulcrystal Stud (40%) | - | Common | stacks |
| `recipe_craft_earring_t80_60` | Recipe: Soulcrystal Stud (60%) | - | Common | stacks |
| `recipe_craft_wand_t80_40` | Recipe: Soulcrystal Wand (40%) | - | Common | stacks |
| `recipe_craft_wand_t80_60` | Recipe: Soulcrystal Wand (60%) | - | Common | stacks |
| `box_daily_rune_choice` | Rune Box (1h) — Daily | - | Common | untradable, stacks |
| `box_acc_t80` | Soulcrystal Accessory Box | - | Rare | stacks |
| `box_spell_rune_24h` | Spell Rune Box (1d) | - | Rare | untradable, stacks |
| `box_spell_rune_1h` | Spell Rune Box (1h) | - | Rare | stacks |
| `box_spell_rune_2h` | Spell Rune Box (2h) | - | Rare | stacks |
| `box_spell_rune_30d` | Spell Rune Box (30d) | - | Rare | untradable, stacks |
| `box_temp_heavy_t52` | Temporary Cobalt Bulwark Set | C | Common | untradable, stacks |
| `box_temp_light_t52` | Temporary Cobalt Leathers Set | C | Common | untradable, stacks |
| `box_temp_robe_t52` | Temporary Cobalt Robe Set | C | Common | untradable, stacks |
| `box_temp_armor_t40` | Temporary Common Armor Selection Box | D | Common | untradable, stacks |
| `box_temp_armor_t52` | Temporary Common Armor Selection Box | C | Common | untradable, stacks |
| `box_temp_weapon_t40` | Temporary Common Weapon Selection Box | D | Common | untradable, stacks |
| `box_temp_weapon_t52` | Temporary Common Weapon Selection Box | C | Common | untradable, stacks |
| `box_temp_heavy_t40` | Temporary Darksteel Bulwark Set | D | Common | untradable, stacks |
| `box_temp_light_t40` | Temporary Darksteel Leathers Set | D | Common | untradable, stacks |
| `box_temp_robe_t40` | Temporary Darksteel Robe Set | D | Common | untradable, stacks |
| `box_training_armor_choice` | Training Armor Box | F | Common | untradable, stacks |
| `box_training_weapons` | Training Weapons Box | F | Common | untradable, stacks |
| `box_treasure` | Treasure Chest | S | Uncommon | stacks |
| `box_war_rune_24h` | War Rune Box (1d) | - | Rare | untradable, stacks |
| `box_war_rune_1h` | War Rune Box (1h) | - | Rare | stacks |
| `box_war_rune_2h` | War Rune Box (2h) | - | Rare | stacks |
| `box_war_rune_30d` | War Rune Box (30d) | - | Rare | untradable, stacks |
| `box_wayfarer_subclass` | Wayfarer's Subclass Box | - | Mythic | untradable, stacks |

## Materials  (114)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `part_heavy_t76` | Adamantine Armor Plate | - | Rare | stacks |
| `part_ring_t76` | Adamantine Band Setting | - | Rare | stacks |
| `part_bow_t76` | Adamantine Bow Limb | - | Rare | stacks |
| `essence_a` | Adamantine Essence | - | Common | stacks |
| `part_duals_t76` | Adamantine Fang Hilt | - | Rare | stacks |
| `part_gloves_t76` | Adamantine Gauntlet Frame | - | Rare | stacks |
| `part_sword2h_t76` | Adamantine Greatsword Blade | - | Rare | stacks |
| `part_boots_t76` | Adamantine Greave Frame | - | Rare | stacks |
| `part_helm_t76` | Adamantine Helm Shell | - | Rare | stacks |
| `part_light_t76` | Adamantine Hide Panel | - | Rare | stacks |
| `part_blunt1h_t76` | Adamantine Mace Head | - | Rare | stacks |
| `part_blunt2h_t76` | Adamantine Maul Head | - | Rare | stacks |
| `part_necklace_t76` | Adamantine Pendant Setting | - | Rare | stacks |
| `part_robe_t76` | Adamantine Robe Weave | - | Rare | stacks |
| `part_shield_t76` | Adamantine Shield Boss | - | Rare | stacks |
| `part_staff_t76` | Adamantine Staff Crown | - | Rare | stacks |
| `part_earring_t76` | Adamantine Stud Setting | - | Rare | stacks |
| `part_sword1h_t76` | Adamantine Sword Blade | - | Rare | stacks |
| `part_wand_t76` | Adamantine Wand Core | - | Rare | stacks |
| `mat_alloy` | Alloy | - | Common | stacks |
| `part_heavy_t61` | Bloodsteel Armor Plate | - | Rare | stacks |
| `part_ring_t61` | Bloodsteel Band Setting | - | Rare | stacks |
| `part_bow_t61` | Bloodsteel Bow Limb | - | Rare | stacks |
| `essence_b` | Bloodsteel Essence | - | Common | stacks |
| `part_duals_t61` | Bloodsteel Fang Hilt | - | Rare | stacks |
| `part_gloves_t61` | Bloodsteel Gauntlet Frame | - | Rare | stacks |
| `part_sword2h_t61` | Bloodsteel Greatsword Blade | - | Rare | stacks |
| `part_boots_t61` | Bloodsteel Greave Frame | - | Rare | stacks |
| `part_helm_t61` | Bloodsteel Helm Shell | - | Rare | stacks |
| `part_light_t61` | Bloodsteel Hide Panel | - | Rare | stacks |
| `part_blunt1h_t61` | Bloodsteel Mace Head | - | Rare | stacks |
| `part_blunt2h_t61` | Bloodsteel Maul Head | - | Rare | stacks |
| `part_necklace_t61` | Bloodsteel Pendant Setting | - | Rare | stacks |
| `part_robe_t61` | Bloodsteel Robe Weave | - | Rare | stacks |
| `part_shield_t61` | Bloodsteel Shield Boss | - | Rare | stacks |
| `part_staff_t61` | Bloodsteel Staff Crown | - | Rare | stacks |
| `part_earring_t61` | Bloodsteel Stud Setting | - | Rare | stacks |
| `part_sword1h_t61` | Bloodsteel Sword Blade | - | Rare | stacks |
| `part_wand_t61` | Bloodsteel Wand Core | - | Rare | stacks |
| `part_heavy_t52` | Cobalt Armor Plate | - | Rare | stacks |
| `part_ring_t52` | Cobalt Band Setting | - | Rare | stacks |
| `part_bow_t52` | Cobalt Bow Limb | - | Rare | stacks |
| `essence_c` | Cobalt Essence | - | Common | stacks |
| `part_duals_t52` | Cobalt Fang Hilt | - | Rare | stacks |
| `part_gloves_t52` | Cobalt Gauntlet Frame | - | Rare | stacks |
| `part_sword2h_t52` | Cobalt Greatsword Blade | - | Rare | stacks |
| `part_boots_t52` | Cobalt Greave Frame | - | Rare | stacks |
| `part_helm_t52` | Cobalt Helm Shell | - | Rare | stacks |
| `part_light_t52` | Cobalt Hide Panel | - | Rare | stacks |
| `part_blunt1h_t52` | Cobalt Mace Head | - | Rare | stacks |
| `part_blunt2h_t52` | Cobalt Maul Head | - | Rare | stacks |
| `part_necklace_t52` | Cobalt Pendant Setting | - | Rare | stacks |
| `part_robe_t52` | Cobalt Robe Weave | - | Rare | stacks |
| `part_shield_t52` | Cobalt Shield Boss | - | Rare | stacks |
| `part_staff_t52` | Cobalt Staff Crown | - | Rare | stacks |
| `part_earring_t52` | Cobalt Stud Setting | - | Rare | stacks |
| `part_sword1h_t52` | Cobalt Sword Blade | - | Rare | stacks |
| `part_wand_t52` | Cobalt Wand Core | - | Rare | stacks |
| `part_heavy_t40` | Darksteel Armor Plate | - | Rare | stacks |
| `part_ring_t40` | Darksteel Band Setting | - | Rare | stacks |
| `part_bow_t40` | Darksteel Bow Limb | - | Rare | stacks |
| `essence_d` | Darksteel Essence | - | Common | stacks |
| `part_duals_t40` | Darksteel Fang Hilt | - | Rare | stacks |
| `part_gloves_t40` | Darksteel Gauntlet Frame | - | Rare | stacks |
| `part_sword2h_t40` | Darksteel Greatsword Blade | - | Rare | stacks |
| `part_boots_t40` | Darksteel Greave Frame | - | Rare | stacks |
| `part_helm_t40` | Darksteel Helm Shell | - | Rare | stacks |
| `part_light_t40` | Darksteel Hide Panel | - | Rare | stacks |
| `part_blunt1h_t40` | Darksteel Mace Head | - | Rare | stacks |
| `part_blunt2h_t40` | Darksteel Maul Head | - | Rare | stacks |
| `part_necklace_t40` | Darksteel Pendant Setting | - | Rare | stacks |
| `part_robe_t40` | Darksteel Robe Weave | - | Rare | stacks |
| `part_shield_t40` | Darksteel Shield Boss | - | Rare | stacks |
| `part_staff_t40` | Darksteel Staff Crown | - | Rare | stacks |
| `part_earring_t40` | Darksteel Stud Setting | - | Rare | stacks |
| `part_sword1h_t40` | Darksteel Sword Blade | - | Rare | stacks |
| `part_wand_t40` | Darksteel Wand Core | - | Rare | stacks |
| `mat_gem` | Gem | - | Common | stacks |
| `mat_iron` | Iron | - | Common | stacks |
| `mat_leather` | Leather | - | Common | stacks |
| `nightsilk_4` | Legendary Nightsilk | - | Legendary | stacks |
| `nightsilver_4` | Legendary Nightsilver | - | Legendary | stacks |
| `nightsilk_0` | Nightsilk | - | Common | stacks |
| `nightsilver_0` | Nightsilver | - | Common | stacks |
| `nightsilk_2` | Rare Nightsilk | - | Rare | stacks |
| `nightsilver_2` | Rare Nightsilver | - | Rare | stacks |
| `nightsilk_1` | Refined Nightsilk | - | Uncommon | stacks |
| `nightsilver_1` | Refined Nightsilver | - | Uncommon | stacks |
| `nightsilk_3` | Refined Rare Nightsilk | - | Epic | stacks |
| `nightsilver_3` | Refined Rare Nightsilver | - | Epic | stacks |
| `part_heavy_t80` | Soulcrystal Armor Plate | - | Rare | stacks |
| `part_ring_t80` | Soulcrystal Band Setting | - | Rare | stacks |
| `part_bow_t80` | Soulcrystal Bow Limb | - | Rare | stacks |
| `essence_s` | Soulcrystal Essence | - | Common | stacks |
| `part_duals_t80` | Soulcrystal Fang Hilt | - | Rare | stacks |
| `part_gloves_t80` | Soulcrystal Gauntlet Frame | - | Rare | stacks |
| `part_sword2h_t80` | Soulcrystal Greatsword Blade | - | Rare | stacks |
| `part_boots_t80` | Soulcrystal Greave Frame | - | Rare | stacks |
| `part_helm_t80` | Soulcrystal Helm Shell | - | Rare | stacks |
| `part_light_t80` | Soulcrystal Hide Panel | - | Rare | stacks |
| `part_blunt1h_t80` | Soulcrystal Mace Head | - | Rare | stacks |
| `part_blunt2h_t80` | Soulcrystal Maul Head | - | Rare | stacks |
| `part_necklace_t80` | Soulcrystal Pendant Setting | - | Rare | stacks |
| `part_robe_t80` | Soulcrystal Robe Weave | - | Rare | stacks |
| `part_shield_t80` | Soulcrystal Shield Boss | - | Rare | stacks |
| `part_staff_t80` | Soulcrystal Staff Crown | - | Rare | stacks |
| `part_earring_t80` | Soulcrystal Stud Setting | - | Rare | stacks |
| `part_sword1h_t80` | Soulcrystal Sword Blade | - | Rare | stacks |
| `part_wand_t80` | Soulcrystal Wand Core | - | Rare | stacks |
| `mat_thread` | Thread | - | Common | stacks |
| `mat_volcanic_ash` | Volcanic Ash | - | Rare | stacks |
| `mat_volcanic_bar` | Volcanic Bar | - | Epic | stacks |
| `mat_volcanic_stone` | Volcanic Stone | - | Rare | stacks |
| `mat_wood` | Wood | - | Common | stacks |

## Quest items  (101)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `quest_token_basilisk_scale` | Amber Scale | - | Common | stacks |
| `quest_token_ash_orc_insignia` | Ash Orc Insignia | - | Common | stacks |
| `qi_129_token` | Assassin Ordeal Mark | - | Epic | stacks |
| `quest_token_spider_hook` | Barbed Hook | - | Common | stacks |
| `quest_token_bear_pelt` | Bear Pelt | - | Common | stacks |
| `quest_crafter_hammer` | Blacksmith's Hammer | - | Rare | stacks |
| `qi_127_token` | Champion Ordeal Mark | - | Epic | stacks |
| `quest_clerics_proof` | Cleric's Proof | - | Epic | stacks |
| `quest_token_cracked_rib` | Cracked Rib | - | Common | stacks |
| `qi_109_token` | Dark Healer Ordeal Mark | - | Epic | stacks |
| `qi_6_token` | Demon Apprentice Trial Token | - | Rare | stacks |
| `qi_6_proof` | Demon Apprentice's Proof | - | Epic | stacks |
| `qi_1_token` | Demon Knight Trial Token | - | Rare | stacks |
| `qi_1_proof` | Demon Knight's Proof | - | Epic | stacks |
| `qi_5_token` | Demon Priest Trial Token | - | Rare | stacks |
| `qi_5_proof` | Demon Priest's Proof | - | Epic | stacks |
| `qi_3_token` | Demon Rogue Trial Token | - | Rare | stacks |
| `qi_3_proof` | Demon Rogue's Proof | - | Epic | stacks |
| `qi_2_token` | Demon Warrior Trial Token | - | Rare | stacks |
| `qi_2_proof` | Demon Warrior's Proof | - | Epic | stacks |
| `qi_134_token` | Doctor Ordeal Mark | - | Epic | stacks |
| `qi_101_token` | Dread Knight Ordeal Mark | - | Epic | stacks |
| `quest_token_dread_sigil` | Dread Sigil | - | Common | stacks |
| `qi_110_token` | Dreadcaller Ordeal Mark | - | Epic | stacks |
| `qi_12_token` | Elf Apprentice Trial Token | - | Rare | stacks |
| `qi_12_proof` | Elf Apprentice's Proof | - | Epic | stacks |
| `qi_7_token` | Elf Knight Trial Token | - | Rare | stacks |
| `qi_7_proof` | Elf Knight's Proof | - | Epic | stacks |
| `qi_11_token` | Elf Priest Trial Token | - | Rare | stacks |
| `qi_11_proof` | Elf Priest's Proof | - | Epic | stacks |
| `qi_9_token` | Elf Rogue Trial Token | - | Rare | stacks |
| `qi_9_proof` | Elf Rogue's Proof | - | Epic | stacks |
| `qi_8_token` | Elf Warrior Trial Token | - | Rare | stacks |
| `qi_8_proof` | Elf Warrior's Proof | - | Epic | stacks |
| `quest_token_ember_scale` | Emberwyrm Scale | - | Common | stacks |
| `qi_111_token` | Fire Adept Ordeal Mark | - | Epic | stacks |
| `qi_121_token` | Forest Whisperer Ordeal Mark | - | Epic | stacks |
| `quest_token_fox_pelt` | Fox Pelt | - | Common | stacks |
| `quest_crafter_hammer_head` | Hammer Head | - | Rare | stacks |
| `qi_122_token` | Harmonist Ordeal Mark | - | Epic | stacks |
| `quest_token_harpy_feather` | Harpy Feather | - | Common | stacks |
| `qi_133_token` | Holy Priest Ordeal Mark | - | Epic | stacks |
| `qi_18_token` | Human Apprentice Trial Token | - | Rare | stacks |
| `qi_18_proof` | Human Apprentice's Proof | - | Epic | stacks |
| `qi_13_token` | Human Knight Trial Token | - | Rare | stacks |
| `qi_13_proof` | Human Knight's Proof | - | Epic | stacks |
| `qi_17_token` | Human Priest Trial Token | - | Rare | stacks |
| `qi_17_proof` | Human Priest's Proof | - | Epic | stacks |
| `qi_15_token` | Human Rogue Trial Token | - | Rare | stacks |
| `qi_15_proof` | Human Rogue's Proof | - | Epic | stacks |
| `qi_14_token` | Human Warrior Trial Token | - | Rare | stacks |
| `qi_14_proof` | Human Warrior's Proof | - | Epic | stacks |
| `qi_125_token` | Iron Guard Ordeal Mark | - | Epic | stacks |
| `qi_135_token` | Mana Adept Ordeal Mark | - | Epic | stacks |
| `quest_token_mantis_claw` | Mantis Claw | - | Common | stacks |
| `quest_mark_of_faith` | Mark of Faith | - | Rare | stacks |
| `qi_117_token` | Phantom Ordeal Mark | - | Epic | stacks |
| `quest_token_radiant_plume` | Radiant Plume | - | Common | stacks |
| `qi_103_token` | Ravager Ordeal Mark | - | Epic | stacks |
| `quest_crafter_iron` | Raw Iron | - | Common | stacks |
| `quest_token_redhorn_badge` | Redhorn Badge | - | Common | stacks |
| `qi_ascension_rite` | Rite of Ascension | - | Legendary | untradable, stacks |
| `quest_crafter_gem` | Rough Gem | - | Common | stacks |
| `quest_token_rusted_shard` | Rusted Shard | - | Common | stacks |
| `qi_129_proof` | Seal of the Assassin | - | Legendary | stacks |
| `qi_127_proof` | Seal of the Champion | - | Legendary | stacks |
| `qi_109_proof` | Seal of the Dark Healer | - | Legendary | stacks |
| `qi_134_proof` | Seal of the Doctor | - | Legendary | stacks |
| `qi_101_proof` | Seal of the Dread Knight | - | Legendary | stacks |
| `qi_110_proof` | Seal of the Dreadcaller | - | Legendary | stacks |
| `qi_111_proof` | Seal of the Fire Adept | - | Legendary | stacks |
| `qi_121_proof` | Seal of the Forest Whisperer | - | Legendary | stacks |
| `qi_122_proof` | Seal of the Harmonist | - | Legendary | stacks |
| `qi_133_proof` | Seal of the Holy Priest | - | Legendary | stacks |
| `qi_125_proof` | Seal of the Iron Guard | - | Legendary | stacks |
| `qi_135_proof` | Seal of the Mana Adept | - | Legendary | stacks |
| `qi_117_proof` | Seal of the Phantom | - | Legendary | stacks |
| `qi_103_proof` | Seal of the Ravager | - | Legendary | stacks |
| `qi_118_proof` | Seal of the Sentinel | - | Legendary | stacks |
| `qi_130_proof` | Seal of the Sharpshooter | - | Legendary | stacks |
| `qi_116_proof` | Seal of the Skirmisher | - | Legendary | stacks |
| `qi_106_proof` | Seal of the Soultracker | - | Legendary | stacks |
| `qi_105_proof` | Seal of the Stalker | - | Legendary | stacks |
| `qi_115_proof` | Seal of the Swiftblade | - | Legendary | stacks |
| `qi_113_proof` | Seal of the Templar | - | Legendary | stacks |
| `qi_128_proof` | Seal of the Vanguard | - | Legendary | stacks |
| `qi_104_proof` | Seal of the Warborn | - | Legendary | stacks |
| `qi_123_proof` | Seal of the Water Adept | - | Legendary | stacks |
| `quest_crafter_wood` | Seasoned Hardwood | - | Common | stacks |
| `qi_118_token` | Sentinel Ordeal Mark | - | Epic | stacks |
| `qi_130_token` | Sharpshooter Ordeal Mark | - | Epic | stacks |
| `qi_116_token` | Skirmisher Ordeal Mark | - | Epic | stacks |
| `qi_106_token` | Soultracker Ordeal Mark | - | Epic | stacks |
| `quest_token_splinter_chitin` | Splinter Chitin | - | Common | stacks |
| `qi_105_token` | Stalker Ordeal Mark | - | Epic | stacks |
| `qi_115_token` | Swiftblade Ordeal Mark | - | Epic | stacks |
| `qi_113_token` | Templar Ordeal Mark | - | Epic | stacks |
| `qi_128_token` | Vanguard Ordeal Mark | - | Epic | stacks |
| `qi_104_token` | Warborn Ordeal Mark | - | Epic | stacks |
| `qi_123_token` | Water Adept Ordeal Mark | - | Epic | stacks |
| `quest_token_werewolf_fang` | Werewolf Fang | - | Common | stacks |

