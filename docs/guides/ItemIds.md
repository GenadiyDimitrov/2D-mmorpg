# Item ids — the complete `/give` reference

**Generated from `ItemCatalog`** by `tools/ItemIds` — do not hand-edit; re-run
`dotnet run --project tools/ItemIds` after adding or removing an item. Every id below is a real
id the server will accept today.

**1073 items.** Generated 2026-08-15.

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

## Weapons  (320)

### no tier (training / one-off)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `training_sword` | Training Sword | F | Common | untradable, Sword |
| `training_wand` | Training Wand | F | Common | untradable, Blunt |

### Lv 1

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `staff_t1_common` | Ferrite Battlestaff | F | Common | TwoHandedBlunt |
| `sword1h_t1_common` | Ferrite Blade | F | Common | Sword |
| `duals_t1_common` | Ferrite Fangs | F | Common | Dual |
| `sword2h_t1_common` | Ferrite Greatsword | F | Common | TwoHandedSword |
| `bow_t1_common` | Ferrite Longbow | F | Common | Bow |
| `blunt1h_t1_common` | Ferrite Mace | F | Common | Blunt |
| `blunt2h_t1_common` | Ferrite Maul | F | Common | TwoHandedBlunt |
| `wand_t1_common` | Ferrite Wand | F | Common | Blunt |
| `staff_t1_uncommon` | Ferrite Battlestaff | F | Uncommon | TwoHandedBlunt |
| `sword1h_t1_uncommon` | Ferrite Blade | F | Uncommon | Sword |
| `duals_t1_uncommon` | Ferrite Fangs | F | Uncommon | Dual |
| `sword2h_t1_uncommon` | Ferrite Greatsword | F | Uncommon | TwoHandedSword |
| `bow_t1_uncommon` | Ferrite Longbow | F | Uncommon | Bow |
| `blunt1h_t1_uncommon` | Ferrite Mace | F | Uncommon | Blunt |
| `blunt2h_t1_uncommon` | Ferrite Maul | F | Uncommon | TwoHandedBlunt |
| `wand_t1_uncommon` | Ferrite Wand | F | Uncommon | Blunt |
| `staff_t1_rare` | Ferrite Battlestaff | F | Rare | TwoHandedBlunt |
| `sword1h_t1_rare` | Ferrite Blade | F | Rare | Sword |
| `duals_t1_rare` | Ferrite Fangs | F | Rare | Dual |
| `sword2h_t1_rare` | Ferrite Greatsword | F | Rare | TwoHandedSword |
| `bow_t1_rare` | Ferrite Longbow | F | Rare | Bow |
| `blunt1h_t1_rare` | Ferrite Mace | F | Rare | Blunt |
| `blunt2h_t1_rare` | Ferrite Maul | F | Rare | TwoHandedBlunt |
| `wand_t1_rare` | Ferrite Wand | F | Rare | Blunt |
| `staff_t1_epic` | Ferrite Battlestaff | F | Epic | TwoHandedBlunt |
| `sword1h_t1_epic` | Ferrite Blade | F | Epic | Sword |
| `duals_t1_epic` | Ferrite Fangs | F | Epic | Dual |
| `sword2h_t1_epic` | Ferrite Greatsword | F | Epic | TwoHandedSword |
| `bow_t1_epic` | Ferrite Longbow | F | Epic | Bow |
| `blunt1h_t1_epic` | Ferrite Mace | F | Epic | Blunt |
| `blunt2h_t1_epic` | Ferrite Maul | F | Epic | TwoHandedBlunt |
| `wand_t1_epic` | Ferrite Wand | F | Epic | Blunt |
| `staff_t1_legendary` | Ferrite Battlestaff | F | Legendary | TwoHandedBlunt |
| `sword1h_t1_legendary` | Ferrite Blade | F | Legendary | Sword |
| `duals_t1_legendary` | Ferrite Fangs | F | Legendary | Dual |
| `sword2h_t1_legendary` | Ferrite Greatsword | F | Legendary | TwoHandedSword |
| `bow_t1_legendary` | Ferrite Longbow | F | Legendary | Bow |
| `blunt1h_t1_legendary` | Ferrite Mace | F | Legendary | Blunt |
| `blunt2h_t1_legendary` | Ferrite Maul | F | Legendary | TwoHandedBlunt |
| `wand_t1_legendary` | Ferrite Wand | F | Legendary | Blunt |
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
| `staff_t20_common` | Electrum Battlestaff | E | Common | TwoHandedBlunt |
| `sword1h_t20_common` | Electrum Blade | E | Common | Sword |
| `duals_t20_common` | Electrum Fangs | E | Common | Dual |
| `sword2h_t20_common` | Electrum Greatsword | E | Common | TwoHandedSword |
| `bow_t20_common` | Electrum Longbow | E | Common | Bow |
| `blunt1h_t20_common` | Electrum Mace | E | Common | Blunt |
| `blunt2h_t20_common` | Electrum Maul | E | Common | TwoHandedBlunt |
| `wand_t20_common` | Electrum Wand | E | Common | Blunt |
| `staff_t20_uncommon` | Electrum Battlestaff | E | Uncommon | TwoHandedBlunt |
| `sword1h_t20_uncommon` | Electrum Blade | E | Uncommon | Sword |
| `duals_t20_uncommon` | Electrum Fangs | E | Uncommon | Dual |
| `sword2h_t20_uncommon` | Electrum Greatsword | E | Uncommon | TwoHandedSword |
| `bow_t20_uncommon` | Electrum Longbow | E | Uncommon | Bow |
| `blunt1h_t20_uncommon` | Electrum Mace | E | Uncommon | Blunt |
| `blunt2h_t20_uncommon` | Electrum Maul | E | Uncommon | TwoHandedBlunt |
| `wand_t20_uncommon` | Electrum Wand | E | Uncommon | Blunt |
| `staff_t20_rare` | Electrum Battlestaff | E | Rare | TwoHandedBlunt |
| `sword1h_t20_rare` | Electrum Blade | E | Rare | Sword |
| `duals_t20_rare` | Electrum Fangs | E | Rare | Dual |
| `sword2h_t20_rare` | Electrum Greatsword | E | Rare | TwoHandedSword |
| `bow_t20_rare` | Electrum Longbow | E | Rare | Bow |
| `blunt1h_t20_rare` | Electrum Mace | E | Rare | Blunt |
| `blunt2h_t20_rare` | Electrum Maul | E | Rare | TwoHandedBlunt |
| `wand_t20_rare` | Electrum Wand | E | Rare | Blunt |
| `staff_t20_epic` | Electrum Battlestaff | E | Epic | TwoHandedBlunt |
| `sword1h_t20_epic` | Electrum Blade | E | Epic | Sword |
| `duals_t20_epic` | Electrum Fangs | E | Epic | Dual |
| `sword2h_t20_epic` | Electrum Greatsword | E | Epic | TwoHandedSword |
| `bow_t20_epic` | Electrum Longbow | E | Epic | Bow |
| `blunt1h_t20_epic` | Electrum Mace | E | Epic | Blunt |
| `blunt2h_t20_epic` | Electrum Maul | E | Epic | TwoHandedBlunt |
| `wand_t20_epic` | Electrum Wand | E | Epic | Blunt |
| `staff_t20_legendary` | Electrum Battlestaff | E | Legendary | TwoHandedBlunt |
| `sword1h_t20_legendary` | Electrum Blade | E | Legendary | Sword |
| `duals_t20_legendary` | Electrum Fangs | E | Legendary | Dual |
| `sword2h_t20_legendary` | Electrum Greatsword | E | Legendary | TwoHandedSword |
| `bow_t20_legendary` | Electrum Longbow | E | Legendary | Bow |
| `blunt1h_t20_legendary` | Electrum Mace | E | Legendary | Blunt |
| `blunt2h_t20_legendary` | Electrum Maul | E | Legendary | TwoHandedBlunt |
| `wand_t20_legendary` | Electrum Wand | E | Legendary | Blunt |
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
| `sword1h_t40_common` | Darksteel Blade | B | Common | Sword |
| `duals_t40_common` | Darksteel Fangs | B | Common | Dual |
| `sword2h_t40_common` | Darksteel Greatsword | B | Common | TwoHandedSword |
| `bow_t40_common` | Darksteel Longbow | B | Common | Bow |
| `blunt1h_t40_common` | Darksteel Mace | B | Common | Blunt |
| `blunt2h_t40_common` | Darksteel Maul | B | Common | TwoHandedBlunt |
| `wand_t40_common` | Darksteel Wand | B | Common | Blunt |
| `staff_t40_uncommon` | Darksteel Battlestaff | B | Uncommon | TwoHandedBlunt |
| `sword1h_t40_uncommon` | Darksteel Blade | B | Uncommon | Sword |
| `duals_t40_uncommon` | Darksteel Fangs | B | Uncommon | Dual |
| `sword2h_t40_uncommon` | Darksteel Greatsword | B | Uncommon | TwoHandedSword |
| `bow_t40_uncommon` | Darksteel Longbow | B | Uncommon | Bow |
| `blunt1h_t40_uncommon` | Darksteel Mace | B | Uncommon | Blunt |
| `blunt2h_t40_uncommon` | Darksteel Maul | B | Uncommon | TwoHandedBlunt |
| `wand_t40_uncommon` | Darksteel Wand | B | Uncommon | Blunt |
| `staff_t40_rare` | Darksteel Battlestaff | B | Rare | TwoHandedBlunt |
| `sword1h_t40_rare` | Darksteel Blade | B | Rare | Sword |
| `duals_t40_rare` | Darksteel Fangs | B | Rare | Dual |
| `sword2h_t40_rare` | Darksteel Greatsword | B | Rare | TwoHandedSword |
| `bow_t40_rare` | Darksteel Longbow | B | Rare | Bow |
| `blunt1h_t40_rare` | Darksteel Mace | B | Rare | Blunt |
| `blunt2h_t40_rare` | Darksteel Maul | B | Rare | TwoHandedBlunt |
| `wand_t40_rare` | Darksteel Wand | B | Rare | Blunt |
| `staff_t40_epic` | Darksteel Battlestaff | B | Epic | TwoHandedBlunt |
| `sword1h_t40_epic` | Darksteel Blade | B | Epic | Sword |
| `duals_t40_epic` | Darksteel Fangs | B | Epic | Dual |
| `sword2h_t40_epic` | Darksteel Greatsword | B | Epic | TwoHandedSword |
| `bow_t40_epic` | Darksteel Longbow | B | Epic | Bow |
| `blunt1h_t40_epic` | Darksteel Mace | B | Epic | Blunt |
| `blunt2h_t40_epic` | Darksteel Maul | B | Epic | TwoHandedBlunt |
| `wand_t40_epic` | Darksteel Wand | B | Epic | Blunt |
| `staff_t40_legendary` | Darksteel Battlestaff | B | Legendary | TwoHandedBlunt |
| `sword1h_t40_legendary` | Darksteel Blade | B | Legendary | Sword |
| `duals_t40_legendary` | Darksteel Fangs | B | Legendary | Dual |
| `sword2h_t40_legendary` | Darksteel Greatsword | B | Legendary | TwoHandedSword |
| `bow_t40_legendary` | Darksteel Longbow | B | Legendary | Bow |
| `blunt1h_t40_legendary` | Darksteel Mace | B | Legendary | Blunt |
| `blunt2h_t40_legendary` | Darksteel Maul | B | Legendary | TwoHandedBlunt |
| `wand_t40_legendary` | Darksteel Wand | B | Legendary | Blunt |
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
| `sword1h_t52_common` | Cobalt Blade | B | Common | Sword |
| `duals_t52_common` | Cobalt Fangs | B | Common | Dual |
| `sword2h_t52_common` | Cobalt Greatsword | B | Common | TwoHandedSword |
| `bow_t52_common` | Cobalt Longbow | B | Common | Bow |
| `blunt1h_t52_common` | Cobalt Mace | B | Common | Blunt |
| `blunt2h_t52_common` | Cobalt Maul | B | Common | TwoHandedBlunt |
| `wand_t52_common` | Cobalt Wand | B | Common | Blunt |
| `staff_t52_uncommon` | Cobalt Battlestaff | B | Uncommon | TwoHandedBlunt |
| `sword1h_t52_uncommon` | Cobalt Blade | B | Uncommon | Sword |
| `duals_t52_uncommon` | Cobalt Fangs | B | Uncommon | Dual |
| `sword2h_t52_uncommon` | Cobalt Greatsword | B | Uncommon | TwoHandedSword |
| `bow_t52_uncommon` | Cobalt Longbow | B | Uncommon | Bow |
| `blunt1h_t52_uncommon` | Cobalt Mace | B | Uncommon | Blunt |
| `blunt2h_t52_uncommon` | Cobalt Maul | B | Uncommon | TwoHandedBlunt |
| `wand_t52_uncommon` | Cobalt Wand | B | Uncommon | Blunt |
| `staff_t52_rare` | Cobalt Battlestaff | B | Rare | TwoHandedBlunt |
| `sword1h_t52_rare` | Cobalt Blade | B | Rare | Sword |
| `duals_t52_rare` | Cobalt Fangs | B | Rare | Dual |
| `sword2h_t52_rare` | Cobalt Greatsword | B | Rare | TwoHandedSword |
| `bow_t52_rare` | Cobalt Longbow | B | Rare | Bow |
| `blunt1h_t52_rare` | Cobalt Mace | B | Rare | Blunt |
| `blunt2h_t52_rare` | Cobalt Maul | B | Rare | TwoHandedBlunt |
| `wand_t52_rare` | Cobalt Wand | B | Rare | Blunt |
| `staff_t52_epic` | Cobalt Battlestaff | B | Epic | TwoHandedBlunt |
| `sword1h_t52_epic` | Cobalt Blade | B | Epic | Sword |
| `duals_t52_epic` | Cobalt Fangs | B | Epic | Dual |
| `sword2h_t52_epic` | Cobalt Greatsword | B | Epic | TwoHandedSword |
| `bow_t52_epic` | Cobalt Longbow | B | Epic | Bow |
| `blunt1h_t52_epic` | Cobalt Mace | B | Epic | Blunt |
| `blunt2h_t52_epic` | Cobalt Maul | B | Epic | TwoHandedBlunt |
| `wand_t52_epic` | Cobalt Wand | B | Epic | Blunt |
| `staff_t52_legendary` | Cobalt Battlestaff | B | Legendary | TwoHandedBlunt |
| `sword1h_t52_legendary` | Cobalt Blade | B | Legendary | Sword |
| `duals_t52_legendary` | Cobalt Fangs | B | Legendary | Dual |
| `sword2h_t52_legendary` | Cobalt Greatsword | B | Legendary | TwoHandedSword |
| `bow_t52_legendary` | Cobalt Longbow | B | Legendary | Bow |
| `blunt1h_t52_legendary` | Cobalt Mace | B | Legendary | Blunt |
| `blunt2h_t52_legendary` | Cobalt Maul | B | Legendary | TwoHandedBlunt |
| `wand_t52_legendary` | Cobalt Wand | B | Legendary | Blunt |
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
| `staff_t61_uncommon` | Bloodsteel Battlestaff | A | Uncommon | TwoHandedBlunt |
| `sword1h_t61_uncommon` | Bloodsteel Blade | A | Uncommon | Sword |
| `duals_t61_uncommon` | Bloodsteel Fangs | A | Uncommon | Dual |
| `sword2h_t61_uncommon` | Bloodsteel Greatsword | A | Uncommon | TwoHandedSword |
| `bow_t61_uncommon` | Bloodsteel Longbow | A | Uncommon | Bow |
| `blunt1h_t61_uncommon` | Bloodsteel Mace | A | Uncommon | Blunt |
| `blunt2h_t61_uncommon` | Bloodsteel Maul | A | Uncommon | TwoHandedBlunt |
| `wand_t61_uncommon` | Bloodsteel Wand | A | Uncommon | Blunt |
| `staff_t61_rare` | Bloodsteel Battlestaff | A | Rare | TwoHandedBlunt |
| `sword1h_t61_rare` | Bloodsteel Blade | A | Rare | Sword |
| `duals_t61_rare` | Bloodsteel Fangs | A | Rare | Dual |
| `sword2h_t61_rare` | Bloodsteel Greatsword | A | Rare | TwoHandedSword |
| `bow_t61_rare` | Bloodsteel Longbow | A | Rare | Bow |
| `blunt1h_t61_rare` | Bloodsteel Mace | A | Rare | Blunt |
| `blunt2h_t61_rare` | Bloodsteel Maul | A | Rare | TwoHandedBlunt |
| `wand_t61_rare` | Bloodsteel Wand | A | Rare | Blunt |
| `staff_t61_epic` | Bloodsteel Battlestaff | A | Epic | TwoHandedBlunt |
| `sword1h_t61_epic` | Bloodsteel Blade | A | Epic | Sword |
| `duals_t61_epic` | Bloodsteel Fangs | A | Epic | Dual |
| `sword2h_t61_epic` | Bloodsteel Greatsword | A | Epic | TwoHandedSword |
| `bow_t61_epic` | Bloodsteel Longbow | A | Epic | Bow |
| `blunt1h_t61_epic` | Bloodsteel Mace | A | Epic | Blunt |
| `blunt2h_t61_epic` | Bloodsteel Maul | A | Epic | TwoHandedBlunt |
| `wand_t61_epic` | Bloodsteel Wand | A | Epic | Blunt |
| `staff_t61_legendary` | Bloodsteel Battlestaff | A | Legendary | TwoHandedBlunt |
| `sword1h_t61_legendary` | Bloodsteel Blade | A | Legendary | Sword |
| `duals_t61_legendary` | Bloodsteel Fangs | A | Legendary | Dual |
| `sword2h_t61_legendary` | Bloodsteel Greatsword | A | Legendary | TwoHandedSword |
| `bow_t61_legendary` | Bloodsteel Longbow | A | Legendary | Bow |
| `blunt1h_t61_legendary` | Bloodsteel Mace | A | Legendary | Blunt |
| `blunt2h_t61_legendary` | Bloodsteel Maul | A | Legendary | TwoHandedBlunt |
| `wand_t61_legendary` | Bloodsteel Wand | A | Legendary | Blunt |
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
| `staff_t76_common` | Adamantine Battlestaff | A | Common | TwoHandedBlunt |
| `sword1h_t76_common` | Adamantine Blade | A | Common | Sword |
| `duals_t76_common` | Adamantine Fangs | A | Common | Dual |
| `sword2h_t76_common` | Adamantine Greatsword | A | Common | TwoHandedSword |
| `bow_t76_common` | Adamantine Longbow | A | Common | Bow |
| `blunt1h_t76_common` | Adamantine Mace | A | Common | Blunt |
| `blunt2h_t76_common` | Adamantine Maul | A | Common | TwoHandedBlunt |
| `wand_t76_common` | Adamantine Wand | A | Common | Blunt |
| `staff_t76_uncommon` | Adamantine Battlestaff | A | Uncommon | TwoHandedBlunt |
| `sword1h_t76_uncommon` | Adamantine Blade | A | Uncommon | Sword |
| `duals_t76_uncommon` | Adamantine Fangs | A | Uncommon | Dual |
| `sword2h_t76_uncommon` | Adamantine Greatsword | A | Uncommon | TwoHandedSword |
| `bow_t76_uncommon` | Adamantine Longbow | A | Uncommon | Bow |
| `blunt1h_t76_uncommon` | Adamantine Mace | A | Uncommon | Blunt |
| `blunt2h_t76_uncommon` | Adamantine Maul | A | Uncommon | TwoHandedBlunt |
| `wand_t76_uncommon` | Adamantine Wand | A | Uncommon | Blunt |
| `staff_t76_rare` | Adamantine Battlestaff | A | Rare | TwoHandedBlunt |
| `sword1h_t76_rare` | Adamantine Blade | A | Rare | Sword |
| `duals_t76_rare` | Adamantine Fangs | A | Rare | Dual |
| `sword2h_t76_rare` | Adamantine Greatsword | A | Rare | TwoHandedSword |
| `bow_t76_rare` | Adamantine Longbow | A | Rare | Bow |
| `blunt1h_t76_rare` | Adamantine Mace | A | Rare | Blunt |
| `blunt2h_t76_rare` | Adamantine Maul | A | Rare | TwoHandedBlunt |
| `wand_t76_rare` | Adamantine Wand | A | Rare | Blunt |
| `staff_t76_epic` | Adamantine Battlestaff | A | Epic | TwoHandedBlunt |
| `sword1h_t76_epic` | Adamantine Blade | A | Epic | Sword |
| `duals_t76_epic` | Adamantine Fangs | A | Epic | Dual |
| `sword2h_t76_epic` | Adamantine Greatsword | A | Epic | TwoHandedSword |
| `bow_t76_epic` | Adamantine Longbow | A | Epic | Bow |
| `blunt1h_t76_epic` | Adamantine Mace | A | Epic | Blunt |
| `blunt2h_t76_epic` | Adamantine Maul | A | Epic | TwoHandedBlunt |
| `wand_t76_epic` | Adamantine Wand | A | Epic | Blunt |
| `staff_t76_legendary` | Adamantine Battlestaff | A | Legendary | TwoHandedBlunt |
| `sword1h_t76_legendary` | Adamantine Blade | A | Legendary | Sword |
| `duals_t76_legendary` | Adamantine Fangs | A | Legendary | Dual |
| `sword2h_t76_legendary` | Adamantine Greatsword | A | Legendary | TwoHandedSword |
| `bow_t76_legendary` | Adamantine Longbow | A | Legendary | Bow |
| `blunt1h_t76_legendary` | Adamantine Mace | A | Legendary | Blunt |
| `blunt2h_t76_legendary` | Adamantine Maul | A | Legendary | TwoHandedBlunt |
| `wand_t76_legendary` | Adamantine Wand | A | Legendary | Blunt |
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
| `staff_t80_epic` | Soulcrystal Battlestaff | S | Epic | TwoHandedBlunt |
| `sword1h_t80_epic` | Soulcrystal Blade | S | Epic | Sword |
| `duals_t80_epic` | Soulcrystal Fangs | S | Epic | Dual |
| `sword2h_t80_epic` | Soulcrystal Greatsword | S | Epic | TwoHandedSword |
| `bow_t80_epic` | Soulcrystal Longbow | S | Epic | Bow |
| `blunt1h_t80_epic` | Soulcrystal Mace | S | Epic | Blunt |
| `blunt2h_t80_epic` | Soulcrystal Maul | S | Epic | TwoHandedBlunt |
| `wand_t80_epic` | Soulcrystal Wand | S | Epic | Blunt |
| `staff_t80_legendary` | Soulcrystal Battlestaff | S | Legendary | TwoHandedBlunt |
| `sword1h_t80_legendary` | Soulcrystal Blade | S | Legendary | Sword |
| `duals_t80_legendary` | Soulcrystal Fangs | S | Legendary | Dual |
| `sword2h_t80_legendary` | Soulcrystal Greatsword | S | Legendary | TwoHandedSword |
| `bow_t80_legendary` | Soulcrystal Longbow | S | Legendary | Bow |
| `blunt1h_t80_legendary` | Soulcrystal Mace | S | Legendary | Blunt |
| `blunt2h_t80_legendary` | Soulcrystal Maul | S | Legendary | TwoHandedBlunt |
| `wand_t80_legendary` | Soulcrystal Wand | S | Legendary | Blunt |
| `staff_t80` | Soulcrystal Battlestaff | S | Mythic | TwoHandedBlunt |
| `sword1h_t80` | Soulcrystal Blade | S | Mythic | Sword |
| `duals_t80` | Soulcrystal Fangs | S | Mythic | Dual |
| `sword2h_t80` | Soulcrystal Greatsword | S | Mythic | TwoHandedSword |
| `bow_t80` | Soulcrystal Longbow | S | Mythic | Bow |
| `blunt1h_t80` | Soulcrystal Mace | S | Mythic | Blunt |
| `blunt2h_t80` | Soulcrystal Maul | S | Mythic | TwoHandedBlunt |
| `wand_t80` | Soulcrystal Wand | S | Mythic | Blunt |

## Shields  (40)

### no tier (training / one-off)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_wooden` | Wooden Shield | F | Common | untradable |

### Lv 1

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t1_common` | Ferrite Aegis | F | Common |  |
| `shield_t1_uncommon` | Ferrite Aegis | F | Uncommon |  |
| `shield_t1_rare` | Ferrite Aegis | F | Rare |  |
| `shield_t1_epic` | Ferrite Aegis | F | Epic |  |
| `shield_t1_legendary` | Ferrite Aegis | F | Legendary |  |
| `shield_t1` | Ferrite Aegis | F | Mythic |  |

### Lv 20

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t20_common` | Electrum Aegis | E | Common |  |
| `shield_t20_uncommon` | Electrum Aegis | E | Uncommon |  |
| `shield_t20_rare` | Electrum Aegis | E | Rare |  |
| `shield_t20_epic` | Electrum Aegis | E | Epic |  |
| `shield_t20_legendary` | Electrum Aegis | E | Legendary |  |
| `shield_t20` | Electrum Aegis | E | Mythic |  |

### Lv 40

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t40_common` | Darksteel Aegis | B | Common |  |
| `shield_t40_uncommon` | Darksteel Aegis | B | Uncommon |  |
| `shield_t40_rare` | Darksteel Aegis | B | Rare |  |
| `shield_t40_epic` | Darksteel Aegis | B | Epic |  |
| `shield_t40_legendary` | Darksteel Aegis | B | Legendary |  |
| `shield_t40` | Darksteel Aegis | B | Mythic |  |

### Lv 52

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t52_common` | Cobalt Aegis | B | Common |  |
| `shield_t52_uncommon` | Cobalt Aegis | B | Uncommon |  |
| `shield_t52_rare` | Cobalt Aegis | B | Rare |  |
| `shield_t52_epic` | Cobalt Aegis | B | Epic |  |
| `shield_t52_legendary` | Cobalt Aegis | B | Legendary |  |
| `shield_t52` | Cobalt Aegis | B | Mythic |  |

### Lv 61

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t61_common` | Bloodsteel Aegis | A | Common |  |
| `shield_t61_uncommon` | Bloodsteel Aegis | A | Uncommon |  |
| `shield_t61_rare` | Bloodsteel Aegis | A | Rare |  |
| `shield_t61_epic` | Bloodsteel Aegis | A | Epic |  |
| `shield_t61_legendary` | Bloodsteel Aegis | A | Legendary |  |
| `shield_t61` | Bloodsteel Aegis | A | Mythic |  |

### Lv 76

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t76_common` | Adamantine Aegis | A | Common |  |
| `shield_t76_uncommon` | Adamantine Aegis | A | Uncommon |  |
| `shield_t76_rare` | Adamantine Aegis | A | Rare |  |
| `shield_t76_epic` | Adamantine Aegis | A | Epic |  |
| `shield_t76_legendary` | Adamantine Aegis | A | Legendary |  |
| `shield_t76` | Adamantine Aegis | A | Mythic |  |

### Lv 80

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `shield_t80_epic` | Soulcrystal Aegis | S | Epic |  |
| `shield_t80_legendary` | Soulcrystal Aegis | S | Legendary |  |
| `shield_t80` | Soulcrystal Aegis | S | Mythic |  |

## Armor  (251)

### no tier (training / one-off)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `training_leather_armor` | Training Leather Armor | F | Common | untradable, Light, Body |
| `training_robe` | Training Robe | F | Common | untradable, Robe, Body |

### Lv 1

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `heavy_t1_common` | Ferrite Bulwark | F | Common | Heavy, Body |
| `gloves_t1_common` | Ferrite Gauntlets | F | Common | Gloves |
| `boots_t1_common` | Ferrite Greaves | F | Common | Boots |
| `helm_t1_common` | Ferrite Helm | F | Common | Head |
| `light_t1_common` | Ferrite Leathers | F | Common | Light, Body |
| `robe_t1_common` | Ferrite Robe | F | Common | Robe, Body |
| `heavy_t1_uncommon` | Ferrite Bulwark | F | Uncommon | Heavy, Body |
| `gloves_t1_uncommon` | Ferrite Gauntlets | F | Uncommon | Gloves |
| `boots_t1_uncommon` | Ferrite Greaves | F | Uncommon | Boots |
| `helm_t1_uncommon` | Ferrite Helm | F | Uncommon | Head |
| `light_t1_uncommon` | Ferrite Leathers | F | Uncommon | Light, Body |
| `robe_t1_uncommon` | Ferrite Robe | F | Uncommon | Robe, Body |
| `heavy_t1_rare` | Ferrite Bulwark | F | Rare | Heavy, Body |
| `gloves_t1_rare` | Ferrite Gauntlets | F | Rare | Gloves |
| `boots_t1_rare` | Ferrite Greaves | F | Rare | Boots |
| `helm_t1_rare` | Ferrite Helm | F | Rare | Head |
| `light_t1_rare` | Ferrite Leathers | F | Rare | Light, Body |
| `robe_t1_rare` | Ferrite Robe | F | Rare | Robe, Body |
| `heavy_t1_epic` | Ferrite Bulwark | F | Epic | Heavy, Body |
| `gloves_t1_epic` | Ferrite Gauntlets | F | Epic | Gloves |
| `boots_t1_epic` | Ferrite Greaves | F | Epic | Boots |
| `helm_t1_epic` | Ferrite Helm | F | Epic | Head |
| `light_t1_epic` | Ferrite Leathers | F | Epic | Light, Body |
| `robe_t1_epic` | Ferrite Robe | F | Epic | Robe, Body |
| `heavy_t1_legendary` | Ferrite Bulwark | F | Legendary | Heavy, Body |
| `gloves_t1_legendary` | Ferrite Gauntlets | F | Legendary | Gloves |
| `boots_t1_legendary` | Ferrite Greaves | F | Legendary | Boots |
| `helm_t1_legendary` | Ferrite Helm | F | Legendary | Head |
| `light_t1_legendary` | Ferrite Leathers | F | Legendary | Light, Body |
| `robe_t1_legendary` | Ferrite Robe | F | Legendary | Robe, Body |
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
| `heavy_t20_common` | Electrum Bulwark | E | Common | Heavy, Body |
| `gloves_t20_common` | Electrum Gauntlets | E | Common | Gloves |
| `boots_t20_common` | Electrum Greaves | E | Common | Boots |
| `helm_t20_common` | Electrum Helm | E | Common | Head |
| `light_t20_common` | Electrum Leathers | E | Common | Light, Body |
| `robe_t20_common` | Electrum Robe | E | Common | Robe, Body |
| `heavy_t20_uncommon` | Electrum Bulwark | E | Uncommon | Heavy, Body |
| `gloves_t20_uncommon` | Electrum Gauntlets | E | Uncommon | Gloves |
| `boots_t20_uncommon` | Electrum Greaves | E | Uncommon | Boots |
| `helm_t20_uncommon` | Electrum Helm | E | Uncommon | Head |
| `light_t20_uncommon` | Electrum Leathers | E | Uncommon | Light, Body |
| `robe_t20_uncommon` | Electrum Robe | E | Uncommon | Robe, Body |
| `heavy_t20_rare` | Electrum Bulwark | E | Rare | Heavy, Body |
| `gloves_t20_rare` | Electrum Gauntlets | E | Rare | Gloves |
| `boots_t20_rare` | Electrum Greaves | E | Rare | Boots |
| `helm_t20_rare` | Electrum Helm | E | Rare | Head |
| `light_t20_rare` | Electrum Leathers | E | Rare | Light, Body |
| `robe_t20_rare` | Electrum Robe | E | Rare | Robe, Body |
| `heavy_t20_epic` | Electrum Bulwark | E | Epic | Heavy, Body |
| `gloves_t20_epic` | Electrum Gauntlets | E | Epic | Gloves |
| `boots_t20_epic` | Electrum Greaves | E | Epic | Boots |
| `helm_t20_epic` | Electrum Helm | E | Epic | Head |
| `light_t20_epic` | Electrum Leathers | E | Epic | Light, Body |
| `robe_t20_epic` | Electrum Robe | E | Epic | Robe, Body |
| `heavy_t20_legendary` | Electrum Bulwark | E | Legendary | Heavy, Body |
| `gloves_t20_legendary` | Electrum Gauntlets | E | Legendary | Gloves |
| `boots_t20_legendary` | Electrum Greaves | E | Legendary | Boots |
| `helm_t20_legendary` | Electrum Helm | E | Legendary | Head |
| `light_t20_legendary` | Electrum Leathers | E | Legendary | Light, Body |
| `robe_t20_legendary` | Electrum Robe | E | Legendary | Robe, Body |
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
| `gloves_t40_common` | Darksteel Gauntlets | B | Common | Gloves |
| `boots_t40_common` | Darksteel Greaves | B | Common | Boots |
| `helm_t40_common` | Darksteel Helm | B | Common | Head |
| `light_t40_common` | Darksteel Leathers | B | Common | Light, Body |
| `robe_t40_common` | Darksteel Robe | B | Common | Robe, Body |
| `heavy_t40_uncommon` | Darksteel Bulwark | B | Uncommon | Heavy, Body |
| `gloves_t40_uncommon` | Darksteel Gauntlets | B | Uncommon | Gloves |
| `boots_t40_uncommon` | Darksteel Greaves | B | Uncommon | Boots |
| `helm_t40_uncommon` | Darksteel Helm | B | Uncommon | Head |
| `light_t40_uncommon` | Darksteel Leathers | B | Uncommon | Light, Body |
| `robe_t40_uncommon` | Darksteel Robe | B | Uncommon | Robe, Body |
| `heavy_t40_rare` | Darksteel Bulwark | B | Rare | Heavy, Body |
| `gloves_t40_rare` | Darksteel Gauntlets | B | Rare | Gloves |
| `boots_t40_rare` | Darksteel Greaves | B | Rare | Boots |
| `helm_t40_rare` | Darksteel Helm | B | Rare | Head |
| `light_t40_rare` | Darksteel Leathers | B | Rare | Light, Body |
| `robe_t40_rare` | Darksteel Robe | B | Rare | Robe, Body |
| `heavy_t40_epic` | Darksteel Bulwark | B | Epic | Heavy, Body |
| `gloves_t40_epic` | Darksteel Gauntlets | B | Epic | Gloves |
| `boots_t40_epic` | Darksteel Greaves | B | Epic | Boots |
| `helm_t40_epic` | Darksteel Helm | B | Epic | Head |
| `light_t40_epic` | Darksteel Leathers | B | Epic | Light, Body |
| `robe_t40_epic` | Darksteel Robe | B | Epic | Robe, Body |
| `heavy_t40_legendary` | Darksteel Bulwark | B | Legendary | Heavy, Body |
| `gloves_t40_legendary` | Darksteel Gauntlets | B | Legendary | Gloves |
| `boots_t40_legendary` | Darksteel Greaves | B | Legendary | Boots |
| `helm_t40_legendary` | Darksteel Helm | B | Legendary | Head |
| `light_t40_legendary` | Darksteel Leathers | B | Legendary | Light, Body |
| `robe_t40_legendary` | Darksteel Robe | B | Legendary | Robe, Body |
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
| `gloves_t52_common` | Cobalt Gauntlets | B | Common | Gloves |
| `boots_t52_common` | Cobalt Greaves | B | Common | Boots |
| `helm_t52_common` | Cobalt Helm | B | Common | Head |
| `light_t52_common` | Cobalt Leathers | B | Common | Light, Body |
| `robe_t52_common` | Cobalt Robe | B | Common | Robe, Body |
| `heavy_t52_uncommon` | Cobalt Bulwark | B | Uncommon | Heavy, Body |
| `gloves_t52_uncommon` | Cobalt Gauntlets | B | Uncommon | Gloves |
| `boots_t52_uncommon` | Cobalt Greaves | B | Uncommon | Boots |
| `helm_t52_uncommon` | Cobalt Helm | B | Uncommon | Head |
| `light_t52_uncommon` | Cobalt Leathers | B | Uncommon | Light, Body |
| `robe_t52_uncommon` | Cobalt Robe | B | Uncommon | Robe, Body |
| `heavy_t52_rare` | Cobalt Bulwark | B | Rare | Heavy, Body |
| `gloves_t52_rare` | Cobalt Gauntlets | B | Rare | Gloves |
| `boots_t52_rare` | Cobalt Greaves | B | Rare | Boots |
| `helm_t52_rare` | Cobalt Helm | B | Rare | Head |
| `light_t52_rare` | Cobalt Leathers | B | Rare | Light, Body |
| `robe_t52_rare` | Cobalt Robe | B | Rare | Robe, Body |
| `heavy_t52_epic` | Cobalt Bulwark | B | Epic | Heavy, Body |
| `gloves_t52_epic` | Cobalt Gauntlets | B | Epic | Gloves |
| `boots_t52_epic` | Cobalt Greaves | B | Epic | Boots |
| `helm_t52_epic` | Cobalt Helm | B | Epic | Head |
| `light_t52_epic` | Cobalt Leathers | B | Epic | Light, Body |
| `robe_t52_epic` | Cobalt Robe | B | Epic | Robe, Body |
| `heavy_t52_legendary` | Cobalt Bulwark | B | Legendary | Heavy, Body |
| `gloves_t52_legendary` | Cobalt Gauntlets | B | Legendary | Gloves |
| `boots_t52_legendary` | Cobalt Greaves | B | Legendary | Boots |
| `helm_t52_legendary` | Cobalt Helm | B | Legendary | Head |
| `light_t52_legendary` | Cobalt Leathers | B | Legendary | Light, Body |
| `robe_t52_legendary` | Cobalt Robe | B | Legendary | Robe, Body |
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
| `heavy_t61_uncommon` | Bloodsteel Bulwark | A | Uncommon | Heavy, Body |
| `gloves_t61_uncommon` | Bloodsteel Gauntlets | A | Uncommon | Gloves |
| `boots_t61_uncommon` | Bloodsteel Greaves | A | Uncommon | Boots |
| `helm_t61_uncommon` | Bloodsteel Helm | A | Uncommon | Head |
| `light_t61_uncommon` | Bloodsteel Leathers | A | Uncommon | Light, Body |
| `robe_t61_uncommon` | Bloodsteel Robe | A | Uncommon | Robe, Body |
| `heavy_t61_rare` | Bloodsteel Bulwark | A | Rare | Heavy, Body |
| `gloves_t61_rare` | Bloodsteel Gauntlets | A | Rare | Gloves |
| `boots_t61_rare` | Bloodsteel Greaves | A | Rare | Boots |
| `helm_t61_rare` | Bloodsteel Helm | A | Rare | Head |
| `light_t61_rare` | Bloodsteel Leathers | A | Rare | Light, Body |
| `robe_t61_rare` | Bloodsteel Robe | A | Rare | Robe, Body |
| `heavy_t61_epic` | Bloodsteel Bulwark | A | Epic | Heavy, Body |
| `gloves_t61_epic` | Bloodsteel Gauntlets | A | Epic | Gloves |
| `boots_t61_epic` | Bloodsteel Greaves | A | Epic | Boots |
| `helm_t61_epic` | Bloodsteel Helm | A | Epic | Head |
| `light_t61_epic` | Bloodsteel Leathers | A | Epic | Light, Body |
| `robe_t61_epic` | Bloodsteel Robe | A | Epic | Robe, Body |
| `heavy_t61_legendary` | Bloodsteel Bulwark | A | Legendary | Heavy, Body |
| `gloves_t61_legendary` | Bloodsteel Gauntlets | A | Legendary | Gloves |
| `boots_t61_legendary` | Bloodsteel Greaves | A | Legendary | Boots |
| `helm_t61_legendary` | Bloodsteel Helm | A | Legendary | Head |
| `light_t61_legendary` | Bloodsteel Leathers | A | Legendary | Light, Body |
| `robe_t61_legendary` | Bloodsteel Robe | A | Legendary | Robe, Body |
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
| `heavy_t76_common` | Adamantine Bulwark | A | Common | Heavy, Body |
| `gloves_t76_common` | Adamantine Gauntlets | A | Common | Gloves |
| `boots_t76_common` | Adamantine Greaves | A | Common | Boots |
| `helm_t76_common` | Adamantine Helm | A | Common | Head |
| `light_t76_common` | Adamantine Leathers | A | Common | Light, Body |
| `robe_t76_common` | Adamantine Robe | A | Common | Robe, Body |
| `heavy_t76_uncommon` | Adamantine Bulwark | A | Uncommon | Heavy, Body |
| `gloves_t76_uncommon` | Adamantine Gauntlets | A | Uncommon | Gloves |
| `boots_t76_uncommon` | Adamantine Greaves | A | Uncommon | Boots |
| `helm_t76_uncommon` | Adamantine Helm | A | Uncommon | Head |
| `light_t76_uncommon` | Adamantine Leathers | A | Uncommon | Light, Body |
| `robe_t76_uncommon` | Adamantine Robe | A | Uncommon | Robe, Body |
| `heavy_t76_rare` | Adamantine Bulwark | A | Rare | Heavy, Body |
| `gloves_t76_rare` | Adamantine Gauntlets | A | Rare | Gloves |
| `boots_t76_rare` | Adamantine Greaves | A | Rare | Boots |
| `helm_t76_rare` | Adamantine Helm | A | Rare | Head |
| `light_t76_rare` | Adamantine Leathers | A | Rare | Light, Body |
| `robe_t76_rare` | Adamantine Robe | A | Rare | Robe, Body |
| `heavy_t76_epic` | Adamantine Bulwark | A | Epic | Heavy, Body |
| `gloves_t76_epic` | Adamantine Gauntlets | A | Epic | Gloves |
| `boots_t76_epic` | Adamantine Greaves | A | Epic | Boots |
| `helm_t76_epic` | Adamantine Helm | A | Epic | Head |
| `light_t76_epic` | Adamantine Leathers | A | Epic | Light, Body |
| `robe_t76_epic` | Adamantine Robe | A | Epic | Robe, Body |
| `heavy_t76_legendary` | Adamantine Bulwark | A | Legendary | Heavy, Body |
| `gloves_t76_legendary` | Adamantine Gauntlets | A | Legendary | Gloves |
| `boots_t76_legendary` | Adamantine Greaves | A | Legendary | Boots |
| `helm_t76_legendary` | Adamantine Helm | A | Legendary | Head |
| `light_t76_legendary` | Adamantine Leathers | A | Legendary | Light, Body |
| `robe_t76_legendary` | Adamantine Robe | A | Legendary | Robe, Body |
| `heavy_t76` | Adamantine Bulwark | A | Mythic | Heavy, Body |
| `gloves_t76` | Adamantine Gauntlets | A | Mythic | Gloves |
| `boots_t76` | Adamantine Greaves | A | Mythic | Boots |
| `helm_t76` | Adamantine Helm | A | Mythic | Head |
| `light_t76` | Adamantine Leathers | A | Mythic | Light, Body |
| `robe_t76` | Adamantine Robe | A | Mythic | Robe, Body |

### Lv 80

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `heavy_t80_epic` | Soulcrystal Bulwark | S | Epic | Heavy, Body |
| `gloves_t80_epic` | Soulcrystal Gauntlets | S | Epic | Gloves |
| `boots_t80_epic` | Soulcrystal Greaves | S | Epic | Boots |
| `helm_t80_epic` | Soulcrystal Helm | S | Epic | Head |
| `light_t80_epic` | Soulcrystal Leathers | S | Epic | Light, Body |
| `robe_t80_epic` | Soulcrystal Robe | S | Epic | Robe, Body |
| `heavy_t80_legendary` | Soulcrystal Bulwark | S | Legendary | Heavy, Body |
| `gloves_t80_legendary` | Soulcrystal Gauntlets | S | Legendary | Gloves |
| `boots_t80_legendary` | Soulcrystal Greaves | S | Legendary | Boots |
| `helm_t80_legendary` | Soulcrystal Helm | S | Legendary | Head |
| `light_t80_legendary` | Soulcrystal Leathers | S | Legendary | Light, Body |
| `robe_t80_legendary` | Soulcrystal Robe | S | Legendary | Robe, Body |
| `heavy_t80` | Soulcrystal Bulwark | S | Mythic | Heavy, Body |
| `gloves_t80` | Soulcrystal Gauntlets | S | Mythic | Gloves |
| `boots_t80` | Soulcrystal Greaves | S | Mythic | Boots |
| `helm_t80` | Soulcrystal Helm | S | Mythic | Head |
| `light_t80` | Soulcrystal Leathers | S | Mythic | Light, Body |
| `robe_t80` | Soulcrystal Robe | S | Mythic | Robe, Body |

## Jewels  (123)

### no tier (training / one-off)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `broken_earring` | Broken Earring | F | Common | Earring |
| `broken_necklace` | Broken Necklace | F | Common | Necklace |
| `broken_ring` | Broken Ring | F | Common | Ring |

### Lv 1

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t1_common` | Ferrite Band | F | Common | Ring |
| `necklace_t1_common` | Ferrite Pendant | F | Common | Necklace |
| `earring_t1_common` | Ferrite Stud | F | Common | Earring |
| `ring_t1_uncommon` | Ferrite Band | F | Uncommon | Ring |
| `necklace_t1_uncommon` | Ferrite Pendant | F | Uncommon | Necklace |
| `earring_t1_uncommon` | Ferrite Stud | F | Uncommon | Earring |
| `ring_t1_rare` | Ferrite Band | F | Rare | Ring |
| `necklace_t1_rare` | Ferrite Pendant | F | Rare | Necklace |
| `earring_t1_rare` | Ferrite Stud | F | Rare | Earring |
| `ring_t1_epic` | Ferrite Band | F | Epic | Ring |
| `necklace_t1_epic` | Ferrite Pendant | F | Epic | Necklace |
| `earring_t1_epic` | Ferrite Stud | F | Epic | Earring |
| `ring_t1_legendary` | Ferrite Band | F | Legendary | Ring |
| `necklace_t1_legendary` | Ferrite Pendant | F | Legendary | Necklace |
| `earring_t1_legendary` | Ferrite Stud | F | Legendary | Earring |
| `ring_t1` | Ferrite Band | F | Mythic | Ring |
| `necklace_t1` | Ferrite Pendant | F | Mythic | Necklace |
| `earring_t1` | Ferrite Stud | F | Mythic | Earring |
| `ring_t1_bound` | Newbie Ferrite Band | F | Mythic | untradable, Ring |
| `necklace_t1_bound` | Newbie Ferrite Pendant | F | Mythic | untradable, Necklace |
| `earring_t1_bound` | Newbie Ferrite Stud | F | Mythic | untradable, Earring |

### Lv 20

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t20_common` | Electrum Band | E | Common | Ring |
| `necklace_t20_common` | Electrum Pendant | E | Common | Necklace |
| `earring_t20_common` | Electrum Stud | E | Common | Earring |
| `ring_t20_uncommon` | Electrum Band | E | Uncommon | Ring |
| `necklace_t20_uncommon` | Electrum Pendant | E | Uncommon | Necklace |
| `earring_t20_uncommon` | Electrum Stud | E | Uncommon | Earring |
| `ring_t20_rare` | Electrum Band | E | Rare | Ring |
| `necklace_t20_rare` | Electrum Pendant | E | Rare | Necklace |
| `earring_t20_rare` | Electrum Stud | E | Rare | Earring |
| `ring_t20_epic` | Electrum Band | E | Epic | Ring |
| `necklace_t20_epic` | Electrum Pendant | E | Epic | Necklace |
| `earring_t20_epic` | Electrum Stud | E | Epic | Earring |
| `ring_t20_legendary` | Electrum Band | E | Legendary | Ring |
| `necklace_t20_legendary` | Electrum Pendant | E | Legendary | Necklace |
| `earring_t20_legendary` | Electrum Stud | E | Legendary | Earring |
| `ring_t20` | Electrum Band | E | Mythic | Ring |
| `necklace_t20` | Electrum Pendant | E | Mythic | Necklace |
| `earring_t20` | Electrum Stud | E | Mythic | Earring |

### Lv 40

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t40_common` | Darksteel Band | B | Common | Ring |
| `necklace_t40_common` | Darksteel Pendant | B | Common | Necklace |
| `earring_t40_common` | Darksteel Stud | B | Common | Earring |
| `ring_t40_uncommon` | Darksteel Band | B | Uncommon | Ring |
| `necklace_t40_uncommon` | Darksteel Pendant | B | Uncommon | Necklace |
| `earring_t40_uncommon` | Darksteel Stud | B | Uncommon | Earring |
| `ring_t40_rare` | Darksteel Band | B | Rare | Ring |
| `necklace_t40_rare` | Darksteel Pendant | B | Rare | Necklace |
| `earring_t40_rare` | Darksteel Stud | B | Rare | Earring |
| `ring_t40_epic` | Darksteel Band | B | Epic | Ring |
| `necklace_t40_epic` | Darksteel Pendant | B | Epic | Necklace |
| `earring_t40_epic` | Darksteel Stud | B | Epic | Earring |
| `ring_t40_legendary` | Darksteel Band | B | Legendary | Ring |
| `necklace_t40_legendary` | Darksteel Pendant | B | Legendary | Necklace |
| `earring_t40_legendary` | Darksteel Stud | B | Legendary | Earring |
| `ring_t40` | Darksteel Band | B | Mythic | Ring |
| `necklace_t40` | Darksteel Pendant | B | Mythic | Necklace |
| `earring_t40` | Darksteel Stud | B | Mythic | Earring |

### Lv 52

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t52_common` | Cobalt Band | B | Common | Ring |
| `necklace_t52_common` | Cobalt Pendant | B | Common | Necklace |
| `earring_t52_common` | Cobalt Stud | B | Common | Earring |
| `ring_t52_uncommon` | Cobalt Band | B | Uncommon | Ring |
| `necklace_t52_uncommon` | Cobalt Pendant | B | Uncommon | Necklace |
| `earring_t52_uncommon` | Cobalt Stud | B | Uncommon | Earring |
| `ring_t52_rare` | Cobalt Band | B | Rare | Ring |
| `necklace_t52_rare` | Cobalt Pendant | B | Rare | Necklace |
| `earring_t52_rare` | Cobalt Stud | B | Rare | Earring |
| `ring_t52_epic` | Cobalt Band | B | Epic | Ring |
| `necklace_t52_epic` | Cobalt Pendant | B | Epic | Necklace |
| `earring_t52_epic` | Cobalt Stud | B | Epic | Earring |
| `ring_t52_legendary` | Cobalt Band | B | Legendary | Ring |
| `necklace_t52_legendary` | Cobalt Pendant | B | Legendary | Necklace |
| `earring_t52_legendary` | Cobalt Stud | B | Legendary | Earring |
| `ring_t52` | Cobalt Band | B | Mythic | Ring |
| `necklace_t52` | Cobalt Pendant | B | Mythic | Necklace |
| `earring_t52` | Cobalt Stud | B | Mythic | Earring |

### Lv 61

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t61_common` | Bloodsteel Band | A | Common | Ring |
| `necklace_t61_common` | Bloodsteel Pendant | A | Common | Necklace |
| `earring_t61_common` | Bloodsteel Stud | A | Common | Earring |
| `ring_t61_uncommon` | Bloodsteel Band | A | Uncommon | Ring |
| `necklace_t61_uncommon` | Bloodsteel Pendant | A | Uncommon | Necklace |
| `earring_t61_uncommon` | Bloodsteel Stud | A | Uncommon | Earring |
| `ring_t61_rare` | Bloodsteel Band | A | Rare | Ring |
| `necklace_t61_rare` | Bloodsteel Pendant | A | Rare | Necklace |
| `earring_t61_rare` | Bloodsteel Stud | A | Rare | Earring |
| `ring_t61_epic` | Bloodsteel Band | A | Epic | Ring |
| `necklace_t61_epic` | Bloodsteel Pendant | A | Epic | Necklace |
| `earring_t61_epic` | Bloodsteel Stud | A | Epic | Earring |
| `ring_t61_legendary` | Bloodsteel Band | A | Legendary | Ring |
| `necklace_t61_legendary` | Bloodsteel Pendant | A | Legendary | Necklace |
| `earring_t61_legendary` | Bloodsteel Stud | A | Legendary | Earring |
| `ring_t61` | Bloodsteel Band | A | Mythic | Ring |
| `necklace_t61` | Bloodsteel Pendant | A | Mythic | Necklace |
| `earring_t61` | Bloodsteel Stud | A | Mythic | Earring |

### Lv 76

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t76_common` | Adamantine Band | A | Common | Ring |
| `necklace_t76_common` | Adamantine Pendant | A | Common | Necklace |
| `earring_t76_common` | Adamantine Stud | A | Common | Earring |
| `ring_t76_uncommon` | Adamantine Band | A | Uncommon | Ring |
| `necklace_t76_uncommon` | Adamantine Pendant | A | Uncommon | Necklace |
| `earring_t76_uncommon` | Adamantine Stud | A | Uncommon | Earring |
| `ring_t76_rare` | Adamantine Band | A | Rare | Ring |
| `necklace_t76_rare` | Adamantine Pendant | A | Rare | Necklace |
| `earring_t76_rare` | Adamantine Stud | A | Rare | Earring |
| `ring_t76_epic` | Adamantine Band | A | Epic | Ring |
| `necklace_t76_epic` | Adamantine Pendant | A | Epic | Necklace |
| `earring_t76_epic` | Adamantine Stud | A | Epic | Earring |
| `ring_t76_legendary` | Adamantine Band | A | Legendary | Ring |
| `necklace_t76_legendary` | Adamantine Pendant | A | Legendary | Necklace |
| `earring_t76_legendary` | Adamantine Stud | A | Legendary | Earring |
| `ring_t76` | Adamantine Band | A | Mythic | Ring |
| `necklace_t76` | Adamantine Pendant | A | Mythic | Necklace |
| `earring_t76` | Adamantine Stud | A | Mythic | Earring |

### Lv 80

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `ring_t80_epic` | Soulcrystal Band | S | Epic | Ring |
| `necklace_t80_epic` | Soulcrystal Pendant | S | Epic | Necklace |
| `earring_t80_epic` | Soulcrystal Stud | S | Epic | Earring |
| `ring_t80_legendary` | Soulcrystal Band | S | Legendary | Ring |
| `necklace_t80_legendary` | Soulcrystal Pendant | S | Legendary | Necklace |
| `earring_t80_legendary` | Soulcrystal Stud | S | Legendary | Earring |
| `ring_t80` | Soulcrystal Band | S | Mythic | Ring |
| `necklace_t80` | Soulcrystal Pendant | S | Mythic | Necklace |
| `earring_t80` | Soulcrystal Stud | S | Mythic | Earring |

## Runes  (59)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `rune_drop_10` | Rune of Drop (10%) | F | Epic | untradable |
| `rune_drop_100` | Rune of Drop (100%) | F | Epic | untradable |
| `rune_drop_20` | Rune of Drop (20%) | F | Epic | untradable |
| `rune_drop_30` | Rune of Drop (30%) | F | Epic | untradable |
| `rune_drop_40` | Rune of Drop (40%) | F | Epic | untradable |
| `rune_drop_5` | Rune of Drop (5%) | F | Epic | untradable |
| `rune_drop_50` | Rune of Drop (50%) | F | Epic | untradable |
| `rune_drop_60` | Rune of Drop (60%) | F | Epic | untradable |
| `rune_drop_70` | Rune of Drop (70%) | F | Epic | untradable |
| `rune_drop_80` | Rune of Drop (80%) | F | Epic | untradable |
| `rune_drop_90` | Rune of Drop (90%) | F | Epic | untradable |
| `rune_expsp_10` | Rune of Exp/SP (10%) | F | Epic | untradable |
| `rune_expsp_100` | Rune of Exp/SP (100%) | F | Epic | untradable |
| `rune_expsp_20` | Rune of Exp/SP (20%) | F | Epic | untradable |
| `rune_expsp_30` | Rune of Exp/SP (30%) | F | Epic | untradable |
| `rune_expsp_40` | Rune of Exp/SP (40%) | F | Epic | untradable |
| `rune_expsp_5` | Rune of Exp/SP (5%) | F | Epic | untradable |
| `rune_expsp_50` | Rune of Exp/SP (50%) | F | Epic | untradable |
| `rune_expsp_60` | Rune of Exp/SP (60%) | F | Epic | untradable |
| `rune_expsp_70` | Rune of Exp/SP (70%) | F | Epic | untradable |
| `rune_expsp_80` | Rune of Exp/SP (80%) | F | Epic | untradable |
| `rune_expsp_90` | Rune of Exp/SP (90%) | F | Epic | untradable |
| `rune_exp_10` | Rune of Experience (10%) | F | Epic | untradable |
| `rune_exp_100` | Rune of Experience (100%) | F | Epic | untradable |
| `rune_exp_20` | Rune of Experience (20%) | F | Epic | untradable |
| `rune_exp_30` | Rune of Experience (30%) | F | Epic | untradable |
| `rune_exp_40` | Rune of Experience (40%) | F | Epic | untradable |
| `rune_exp_5` | Rune of Experience (5%) | F | Epic | untradable |
| `rune_exp_50` | Rune of Experience (50%) | F | Epic | untradable |
| `rune_exp_60` | Rune of Experience (60%) | F | Epic | untradable |
| `rune_exp_70` | Rune of Experience (70%) | F | Epic | untradable |
| `rune_exp_80` | Rune of Experience (80%) | F | Epic | untradable |
| `rune_exp_90` | Rune of Experience (90%) | F | Epic | untradable |
| `rune_gold_10` | Rune of Gold (10%) | F | Epic | untradable |
| `rune_gold_100` | Rune of Gold (100%) | F | Epic | untradable |
| `rune_gold_20` | Rune of Gold (20%) | F | Epic | untradable |
| `rune_gold_30` | Rune of Gold (30%) | F | Epic | untradable |
| `rune_gold_40` | Rune of Gold (40%) | F | Epic | untradable |
| `rune_gold_5` | Rune of Gold (5%) | F | Epic | untradable |
| `rune_gold_50` | Rune of Gold (50%) | F | Epic | untradable |
| `rune_gold_60` | Rune of Gold (60%) | F | Epic | untradable |
| `rune_gold_70` | Rune of Gold (70%) | F | Epic | untradable |
| `rune_gold_80` | Rune of Gold (80%) | F | Epic | untradable |
| `rune_gold_90` | Rune of Gold (90%) | F | Epic | untradable |
| `rune_sinister` | Rune of Sinister | F | Epic | untradable |
| `rune_sinners` | Rune of Sinners | F | Epic | untradable, **soulbound** |
| `rune_sp_10` | Rune of Skillpoints (10%) | F | Epic | untradable |
| `rune_sp_100` | Rune of Skillpoints (100%) | F | Epic | untradable |
| `rune_sp_20` | Rune of Skillpoints (20%) | F | Epic | untradable |
| `rune_sp_30` | Rune of Skillpoints (30%) | F | Epic | untradable |
| `rune_sp_40` | Rune of Skillpoints (40%) | F | Epic | untradable |
| `rune_sp_5` | Rune of Skillpoints (5%) | F | Epic | untradable |
| `rune_sp_50` | Rune of Skillpoints (50%) | F | Epic | untradable |
| `rune_sp_60` | Rune of Skillpoints (60%) | F | Epic | untradable |
| `rune_sp_70` | Rune of Skillpoints (70%) | F | Epic | untradable |
| `rune_sp_80` | Rune of Skillpoints (80%) | F | Epic | untradable |
| `rune_sp_90` | Rune of Skillpoints (90%) | F | Epic | untradable |
| `rune_spell` | Spell Rune | F | Rare | untradable |
| `rune_war` | War Rune | F | Rare | untradable |

## Consumables (potions)  (56)

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
| `potion_dash_u` | Dash Potion | F | Uncommon | stacks |
| `potion_dash_l` | Dash Potion (Grand) | F | Legendary | stacks |
| `potion_dash_r` | Dash Potion (Greater) | F | Rare | stacks |
| `potion_dash_c` | Dash Potion (Lesser) | F | Common | stacks |
| `potion_dash_e` | Dash Potion (Superior) | F | Epic | stacks |
| `potion_dash_m` | Dash Potion (Supreme) | F | Mythic | stacks |
| `potion_dash_m_bound` | Dash Potion (Supreme) (Bound) | F | Mythic | untradable, stacks |
| `elemental_stone` | Elemental Stone | F | Rare | stacks |
| `potion_matk_u` | Force Potion | F | Uncommon | stacks |
| `potion_matk_c` | Force Potion (Lesser) | F | Common | stacks |
| `potion_atk_u` | Fury Potion | F | Uncommon | stacks |
| `potion_atk_c` | Fury Potion (Lesser) | F | Common | stacks |
| `potion_instant` | Instant Healing Potion | F | Rare | stacks |
| `potion_instant_bound` | Instant Healing Potion (Bound) | F | Rare | untradable, stacks |
| `potion_patk_u` | Might Potion | F | Uncommon | stacks |
| `potion_patk_c` | Might Potion (Lesser) | F | Common | stacks |
| `potion_greater` | Rare Healing Potion | F | Rare | stacks |
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
| `scroll_resurrect` | Scroll of Resurrection | F | Uncommon | stacks |
| `scroll_return` | Scroll of Return | F | Common | stacks |
| `scroll_mpreg_m` | Scroll of Serenity | F | Rare | untradable, stacks |
| `scroll_mp_m` | Scroll of Soul | F | Rare | untradable, stacks |
| `scroll_speed_r` | Scroll of Swift | F | Rare | untradable, stacks |
| `scroll_hpreg_m` | Scroll of Vigor | F | Rare | untradable, stacks |
| `scroll_mdef_r` | Scroll of Ward | F | Rare | untradable, stacks |
| `skill_stone` | Skill Stone | F | Uncommon | stacks |
| `potion_speed_u` | Swift Potion | F | Uncommon | stacks |
| `potion_speed_c` | Swift Potion (Lesser) | F | Common | stacks |
| `scroll_resurrect_ultimate` | Ultimate Scroll of Resurrection | F | Rare | stacks |
| `scroll_resurrect_ultimate_bound` | Ultimate Scroll of Resurrection (Bound) | F | Rare | untradable, stacks |
| `scroll_return_ultimate` | Ultimate Scroll of Return | F | Rare | untradable, stacks |
| `scroll_return_ultimate_bound` | Ultimate Scroll of Return (Bound) | F | Rare | untradable, stacks |
| `potion_healing` | Uncommon Healing Potion | F | Uncommon | stacks |
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

## Boxes  (63)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `box_acc_t76` | Adamantine Accessory Box | A | Rare |  |
| `box_buff_scrolls` | Blessing Box | F | Rare |  |
| `box_acc_t61` | Bloodsteel Accessory Box | A | Rare |  |
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
| `box_acc_t52` | Cobalt Accessory Box | B | Rare |  |
| `box_acc_t40` | Darksteel Accessory Box | B | Rare |  |
| `box_acc_t20` | Electrum Accessory Box | E | Rare |  |
| `box_acc_t1` | Ferrite Accessory Box | F | Rare |  |
| `box_newbie_armor_choice` | Newbie Armor Set | F | Common | untradable |
| `box_newbie` | Newbie Box | F | Common | untradable |
| `box_newbie_jewels` | Newbie Jewels Box | F | Common | untradable |
| `box_newbie_armor_light` | Newbie Light Armor Box | F | Common | untradable |
| `box_newbie_armor_robe` | Newbie Robe Armor Box | F | Common | untradable |
| `box_newbie_rune_choice` | Newbie Rune | F | Common | untradable |
| `box_newbie_weapons` | Newbie Weapons Box | F | Common | untradable |
| `box_daily_rune_choice` | Rune Box (1h) — Daily | F | Common | untradable |
| `box_acc_t80` | Soulcrystal Accessory Box | S | Rare |  |
| `box_spell_rune_24h` | Spell Rune Box (1d) | F | Rare | untradable |
| `box_spell_rune_1h` | Spell Rune Box (1h) | F | Rare |  |
| `box_spell_rune_2h` | Spell Rune Box (2h) | F | Rare |  |
| `box_spell_rune_30d` | Spell Rune Box (30d) | F | Rare | untradable |
| `box_training_armor_choice` | Training Armor Box | F | Common | untradable |
| `box_training_weapons` | Training Weapons Box | F | Common | untradable |
| `box_treasure` | Treasure Chest | F | Uncommon |  |
| `box_war_rune_24h` | War Rune Box (1d) | F | Rare | untradable |
| `box_war_rune_1h` | War Rune Box (1h) | F | Rare |  |
| `box_war_rune_2h` | War Rune Box (2h) | F | Rare |  |
| `box_war_rune_30d` | War Rune Box (30d) | F | Rare | untradable |

## Materials  (30)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `mat_gem_common` | Common Gem | F | Common | stacks |
| `mat_ingot_common` | Common Ingot | F | Common | stacks |
| `mat_leather_common` | Common Leather | F | Common | stacks |
| `mat_thread_common` | Common Thread | F | Common | stacks |
| `mat_wood_common` | Common Wood | F | Common | stacks |
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
| `mat_gem_uncommon` | Uncommon Gem | F | Uncommon | stacks |
| `mat_ingot_uncommon` | Uncommon Ingot | F | Uncommon | stacks |
| `mat_leather_uncommon` | Uncommon Leather | F | Uncommon | stacks |
| `mat_thread_uncommon` | Uncommon Thread | F | Uncommon | stacks |
| `mat_wood_uncommon` | Uncommon Wood | F | Uncommon | stacks |

## Quest items  (107)

| id | name | grade | rarity | notes |
|---|---|---|---|---|
| `quest_token_basilisk_scale` | Amber Scale | F | Common | stacks |
| `quest_token_ash_orc_insignia` | Ash Orc Insignia | F | Common | stacks |
| `qi_15_token` | Assassin Trial Token | F | Rare | stacks |
| `qi_15_proof` | Assassin's Proof | F | Epic | stacks |
| `quest_token_spider_hook` | Barbed Hook | F | Common | stacks |
| `quest_token_bear_pelt` | Bear Pelt | F | Common | stacks |
| `qi_1_token` | Beast Trial Token | F | Rare | stacks |
| `qi_1_proof` | Beast's Proof | F | Epic | stacks |
| `qi_101_token` | Bulwark Ordeal Mark | F | Epic | stacks |
| `qi_113_token` | Bulwark Ordeal Mark | F | Epic | stacks |
| `qi_125_token` | Bulwark Ordeal Mark | F | Epic | stacks |
| `qi_14_token` | Champion Trial Token | F | Rare | stacks |
| `qi_14_proof` | Champion's Proof | F | Epic | stacks |
| `qi_17_token` | Cleric Trial Token | F | Rare | stacks |
| `quest_clerics_proof` | Cleric's Proof | F | Epic | stacks |
| `qi_17_proof` | Cleric's Proof | F | Epic | stacks |
| `quest_token_cracked_rib` | Cracked Rib | F | Common | stacks |
| `quest_token_dread_sigil` | Dread Sigil | F | Common | stacks |
| `quest_token_ember_scale` | Emberwyrm Scale | F | Common | stacks |
| `quest_token_fox_pelt` | Fox Pelt | F | Common | stacks |
| `quest_token_harpy_feather` | Harpy Feather | F | Common | stacks |
| `qi_106_token` | Hunter Ordeal Mark | F | Epic | stacks |
| `qi_12_token` | Inquisitor Trial Token | F | Rare | stacks |
| `qi_12_proof` | Inquisitor's Proof | F | Epic | stacks |
| `qi_13_token` | Knight Trial Token | F | Rare | stacks |
| `qi_13_proof` | Knight's Proof | F | Epic | stacks |
| `qi_109_token` | Lightbringer Ordeal Mark | F | Epic | stacks |
| `qi_121_token` | Lightbringer Ordeal Mark | F | Epic | stacks |
| `qi_133_token` | Lightbringer Ordeal Mark | F | Epic | stacks |
| `qi_111_token` | Magus Ordeal Mark | F | Epic | stacks |
| `qi_123_token` | Magus Ordeal Mark | F | Epic | stacks |
| `qi_135_token` | Magus Ordeal Mark | F | Epic | stacks |
| `quest_token_mantis_claw` | Mantis Claw | F | Common | stacks |
| `quest_mark_of_faith` | Mark of Faith | F | Rare | stacks |
| `qi_129_token` | Nullblade Ordeal Mark | F | Epic | stacks |
| `qi_117_token` | Phantom Ordeal Mark | F | Epic | stacks |
| `qi_11_token` | Priest Trial Token | F | Rare | stacks |
| `qi_11_proof` | Priest's Proof | F | Epic | stacks |
| `quest_token_radiant_plume` | Radiant Plume | F | Common | stacks |
| `qi_103_token` | Ravager Ordeal Mark | F | Epic | stacks |
| `qi_115_token` | Ravager Ordeal Mark | F | Epic | stacks |
| `qi_127_token` | Ravager Ordeal Mark | F | Epic | stacks |
| `quest_token_redhorn_badge` | Redhorn Badge | F | Common | stacks |
| `quest_token_rusted_shard` | Rusted Shard | F | Common | stacks |
| `qi_101_proof` | Seal of the Bulwark | F | Legendary | stacks |
| `qi_113_proof` | Seal of the Bulwark | F | Legendary | stacks |
| `qi_125_proof` | Seal of the Bulwark | F | Legendary | stacks |
| `qi_106_proof` | Seal of the Hunter | F | Legendary | stacks |
| `qi_109_proof` | Seal of the Lightbringer | F | Legendary | stacks |
| `qi_121_proof` | Seal of the Lightbringer | F | Legendary | stacks |
| `qi_133_proof` | Seal of the Lightbringer | F | Legendary | stacks |
| `qi_111_proof` | Seal of the Magus | F | Legendary | stacks |
| `qi_123_proof` | Seal of the Magus | F | Legendary | stacks |
| `qi_135_proof` | Seal of the Magus | F | Legendary | stacks |
| `qi_129_proof` | Seal of the Nullblade | F | Legendary | stacks |
| `qi_117_proof` | Seal of the Phantom | F | Legendary | stacks |
| `qi_103_proof` | Seal of the Ravager | F | Legendary | stacks |
| `qi_115_proof` | Seal of the Ravager | F | Legendary | stacks |
| `qi_127_proof` | Seal of the Ravager | F | Legendary | stacks |
| `qi_130_proof` | Seal of the Sharpshooter | F | Legendary | stacks |
| `qi_112_proof` | Seal of the Tempest | F | Legendary | stacks |
| `qi_124_proof` | Seal of the Tempest | F | Legendary | stacks |
| `qi_136_proof` | Seal of the Tempest | F | Legendary | stacks |
| `qi_118_proof` | Seal of the Trapper | F | Legendary | stacks |
| `qi_102_proof` | Seal of the Vanguard | F | Legendary | stacks |
| `qi_114_proof` | Seal of the Vanguard | F | Legendary | stacks |
| `qi_126_proof` | Seal of the Vanguard | F | Legendary | stacks |
| `qi_105_proof` | Seal of the Venomweaver | F | Legendary | stacks |
| `qi_110_proof` | Seal of the Warchanter | F | Legendary | stacks |
| `qi_122_proof` | Seal of the Warchanter | F | Legendary | stacks |
| `qi_134_proof` | Seal of the Warchanter | F | Legendary | stacks |
| `qi_104_proof` | Seal of the Warlord | F | Legendary | stacks |
| `qi_116_proof` | Seal of the Warlord | F | Legendary | stacks |
| `qi_128_proof` | Seal of the Warlord | F | Legendary | stacks |
| `qi_8_token` | Sentinel Trial Token | F | Rare | stacks |
| `qi_8_proof` | Sentinel's Proof | F | Epic | stacks |
| `qi_9_token` | Shadowblade Trial Token | F | Rare | stacks |
| `qi_9_proof` | Shadowblade's Proof | F | Epic | stacks |
| `qi_5_token` | Shaman Trial Token | F | Rare | stacks |
| `qi_5_proof` | Shaman's Proof | F | Epic | stacks |
| `qi_130_token` | Sharpshooter Ordeal Mark | F | Epic | stacks |
| `qi_18_token` | Sorcerer Trial Token | F | Rare | stacks |
| `qi_18_proof` | Sorcerer's Proof | F | Epic | stacks |
| `quest_token_splinter_chitin` | Splinter Chitin | F | Common | stacks |
| `qi_3_token` | Stalker Trial Token | F | Rare | stacks |
| `qi_3_proof` | Stalker's Proof | F | Epic | stacks |
| `qi_112_token` | Tempest Ordeal Mark | F | Epic | stacks |
| `qi_124_token` | Tempest Ordeal Mark | F | Epic | stacks |
| `qi_136_token` | Tempest Ordeal Mark | F | Epic | stacks |
| `qi_7_token` | Templar Trial Token | F | Rare | stacks |
| `qi_7_proof` | Templar's Proof | F | Epic | stacks |
| `qi_118_token` | Trapper Ordeal Mark | F | Epic | stacks |
| `qi_102_token` | Vanguard Ordeal Mark | F | Epic | stacks |
| `qi_114_token` | Vanguard Ordeal Mark | F | Epic | stacks |
| `qi_126_token` | Vanguard Ordeal Mark | F | Epic | stacks |
| `qi_105_token` | Venomweaver Ordeal Mark | F | Epic | stacks |
| `qi_110_token` | Warchanter Ordeal Mark | F | Epic | stacks |
| `qi_122_token` | Warchanter Ordeal Mark | F | Epic | stacks |
| `qi_134_token` | Warchanter Ordeal Mark | F | Epic | stacks |
| `qi_104_token` | Warlord Ordeal Mark | F | Epic | stacks |
| `qi_116_token` | Warlord Ordeal Mark | F | Epic | stacks |
| `qi_128_token` | Warlord Ordeal Mark | F | Epic | stacks |
| `qi_2_token` | Warrior Trial Token | F | Rare | stacks |
| `qi_2_proof` | Warrior's Proof | F | Epic | stacks |
| `quest_token_werewolf_fang` | Werewolf Fang | F | Common | stacks |
| `qi_6_token` | Witch Trial Token | F | Rare | stacks |
| `qi_6_proof` | Witch's Proof | F | Epic | stacks |

