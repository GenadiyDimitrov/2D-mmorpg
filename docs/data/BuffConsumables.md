# Consumable buffs — what exists as a potion, what as a scroll, and what has neither

> 🤖 **GENERATED — do not edit by hand.** Every number and every yes/no on this page is a
> query against `ItemCatalog` + `SkillCatalog` + `ShopCatalog` + `RecipeCatalog` + `BoxCatalog` +
> the mob drop tables, so it cannot go stale the way a typed table would. Regenerate with:
>
> ```
> dotnet run --project tools/BalanceMatrix -- --buff-consumables
> ```

`BL-147`. **Potion vs scroll is a DURATION, not a different buff** — a Might Potion and a
Scroll of Might hand out the *same* rung of the *same* family; the potion runs 20 minutes and
the scroll an hour, which is why drinking a potion over an equal-rung scroll is refused
rather than wasted. **Slot** = does it occupy one of the 20 buff squares (the server's own
`CountsAgainstBuffCap`), which is a different question from which BAR it draws in.

## 1. Buff families you can BUY, CRAFT or LOOT

| Buff | Family | Potion | Scroll | Where it comes from | Same as the NPC buffer? | Slot |
|---|---|---|---|---|---|---|
| **Agility** | `spd_eva` | rung 1 / rung 2 | rung 4 | box (Blessing Box), craft, vendor (Apothecary) | yes — identical rung | **yes** |
| **Aim** | `accuracy` | rung 1 / rung 2 | rung 4 | box (Blessing Box), craft, vendor (Apothecary) | yes — identical rung | **yes** |
| **Alacrity** | `spd_cast` | rung 1 / rung 2 | rung 3 | box (Blessing Box), craft, drop, vendor (Apothecary) | yes — identical rung | **yes** |
| **Body** | `hp_max` | — | rung 6 | box (Blessing Box), craft | yes — identical rung | **yes** |
| **Bulwark** | `def_phys` | rung 1 / rung 2 | rung 3 | box (Blessing Box), craft, vendor (Apothecary) | yes — identical rung | **yes** |
| **Common Healing** | `potion_heal` | burst: rung 1 / rung 2 / rung 3 | — | box (Newbie Box), box (Treasure Chest), craft, drop, vendor (Apothecary) | no | no |
| **Common Mana** | `potion_mana` | burst: rung 1 / rung 2 / rung 3 | — | craft, vendor (Apothecary) | no | no |
| **Dash** | `dash` | burst: rung 1 / rung 2 / rung 4 / rung 5 / rung 6 / rung 7 / rung 7 | — | craft, drop | no | no |
| **Ferocity** | `crit_dmg` | — | rung 6 | box (Blessing Box), craft | no | **yes** |
| **Focus** | `crit_rate` | — | rung 6 | box (Blessing Box), craft | no | **yes** |
| **Force** | `atk_mag` | rung 1 / rung 2 | rung 4 | box (Blessing Box), craft, vendor (Apothecary) | yes — identical rung | **yes** |
| **Frenzy** | `frenzy` | — | rung 6 | box (Blessing Box), craft | yes — identical rung | **yes** |
| **Fury** | `spd_as` | rung 1 / rung 2 | rung 3 | box (Blessing Box), craft, drop, vendor (Apothecary) | yes — identical rung | **yes** |
| **Insight** | `mcrit_rate` | — | rung 6 | box (Blessing Box), craft | no | **yes** |
| **Might** | `atk_phys` | rung 1 / rung 2 | rung 3 | box (Blessing Box), craft, vendor (Apothecary) | yes — identical rung | **yes** |
| **Serenity** | `mp_regen` | — | rung 6 | box (Blessing Box), craft | yes — identical rung | **yes** |
| **Soul** | `mp_max` | — | rung 6 | box (Blessing Box), craft | yes — identical rung | **yes** |
| **Swift** | `spd_move` | rung 1 / rung 2 | rung 3 | box (Blessing Box), craft, drop, vendor (Apothecary) | yes — identical rung | **yes** |
| **Vigor** | `hp_regen` | — | rung 6 | box (Blessing Box), craft | yes — identical rung | **yes** |
| **Ward** | `def_mag` | rung 1 / rung 2 | rung 4 | box (Blessing Box), craft, vendor (Apothecary) | yes — identical rung | **yes** |

## 2. SINGLE BUFFS with no consumable — the ladder families you cannot buy

The half of the ask that matters most, and the half a hand-written page always gets wrong:
a family of *rungs* — the same shape as Might or Focus, something a potion COULD hand out —
that has no potion and no scroll. Only a class or the buffer NPC can give you these, so a
solo player without a buffer cannot have them at all.

| Buff | Family | Rungs | Top rung | Who can give it to you | NPC buffer? |
|---|---|---|---|---|---|
| **Clarity** | `cc_res_mag` | 4 | +50% resistance to magical (SPT) debuffs | Clarity, Arcane and Feral Protection (group) | no |
| **Fortitude** | `cc_res_phys` | 12 | +65% resistance to physical (CON) debuffs | Fortitude, Arcane and Feral Protection (group) | no |
| **Resolve** | `interrupt` | 7 | +54% interrupt resistance | Force and Ward, Resolve, Arcane Serenity (group), Force and Ward (group) | yes |
| **Shield Blessing** | `shield_block` | 6 | +30% block chance | Shield Blessing, Shield Bless and Harden (group), Shield Reinforcement (group) | no |
| **Shield Hardening** | `shield_def` | 3 | +50% shield P.Def | Shield Hardening, Shield Bless and Harden (group), Shield Reinforcement (group) | no |
| **Vampirism** | `vamp` | 5 | +9% melee vampirism | Vampirism, Feral Bloodlust (group), Might and Bulwark (group) | yes |

## 3. Class and self buffs — not part of the single-buff ladder at all

Context, not a gap. Each of these carries its own duration and its own MP price: it IS a
skill rather than a rung something else hands out, so "why is there no potion of it" does
not apply the way it does to the table above.

| Buff | Family | Rungs | Top rung | Who can give it to you | NPC buffer? |
|---|---|---|---|---|---|
| **Aegis** | `aegis` | 1 | Raises a shield that absorbs 8% of your max HP for 15s before HP is hit | Aegis | no |
| **Aggravated State** | `aggravated_state` | 6 | +7% P.Atk and attack speed, +20% physical skill power, for 30s | Aggravated State | no |
| **Angel's Protection** | `buff_preservation` | 3 | You DIE normally and keep your buffs, then choose when to rise where you fell at 30% HP and MP, losing NO experience — the offer waits as long as you like. Lasts 60 minutes or until it saves you. 60 minute reuse | Angel's Protection, Rite of Preservation, Undying Will | no |
| **Arcane Resistance** | `arcane_resistance` | 1 | For 20 minutes each of the target's buffs has a 30% chance to survive an enemy's dispel | Arcane Resistance | no |
| **Battle Fury** | `battle_fury` | 1 | +20% Attack and +15% Move Speed for 30s | Battle Fury | no |
| **Battle Presence** | `battle_stance` | 2 | A desperate defence: DOUBLES your P.Def for 90s. Usable only at ≤60% HP. Cannot be combined with Battle Presence | Battle Defence, Battle Presence | no |
| **Blessing of Light** | `lb_blessing` | 1 | Party: +15% max HP and +15% defence | Blessing of Light | no |
| **Bow Expertise** | `bow_expertise` | 2 | Steadies your aim: +12% attack speed while wielding a bow, for 20 minutes | Bow Expertise | no |
| **Combat Stance** | `healer_combat_stance` | 1 | Toggle. Channel your magic into melee: +50% P.Atk but -50% M.Atk (weaker heals and spells). Click again to end | Combat Stance | no |
| **Combo Rush** | `wc_combo` | 6 | A surge of momentum: +20% attack speed and +15% cast speed for 30s | Combo Rush | no |
| **Conceal** | `conceal` | 1 | For 30s, monsters that haven't already noticed you leave you alone. Anything already chasing you keeps chasing | Conceal | no |
| **Defensive Wall** | `defensive_wall` | 1 | Raise an impregnable guard for 30s: +1800 P.Def, +1600 M.Def and high cancel resistance, but your movement is halved | Defensive Wall | no |
| **Evasion Boost** | `evasion_boost` | 1 | Slip every blow for 30s: +20 Evasion, a 25% chance to dodge physical SKILLS outright, spells cast at you are 4% more likely to fail, and your buffs strongly resist being cancelled | Evasion Boost | no |
| **Fortify** | `fortify` | 1 | Tank stance: +50% Defence for 25s | Fortify | no |
| **Grand Anthem** | `wc_chant` | 3 | Party: +35% max HP/MP, +30% magic def & cast/attack speed, +15% atk/def, +move & regen | Grand Anthem, Sylvan Anthem, War Anthem | no |
| **Great Might** | `great_blessing` | 4 | +15% P.Def for the whole party, on top of Bulwark, for 20 minutes. Does not stack with War Might or Great Might — an ally carries one, never both | Great Bulwark, Great Might, War Bulwark, War Might | no |
| **Harmony of Madness** | `harmony_madness` | 1 | A hymn that trades the party's guard for its edge: less life and mana, more of everything that ends a fight | Harmony of Madness | no |
| **Harmony of Protection** | `harmony_protection` | 1 | Shields you and nearby allies. Stacks on top of every ordinary defensive buff | Harmony of Protection | no |
| **Harmony of Speed** | `harmony_speed` | 1 | Quickens you and nearby allies. Stacks on top of Swift and Agility | Harmony of Speed | no |
| **Harmony of the Soul** | `harmony_soul` | 1 | Quickens the party's hands and spares their mana: shorter reuse and cheaper skills, and at the top healing lands harder and the mind holds | Harmony of the Soul | no |
| **Harmony of the Warrior** | `harmony_warrior` | 1 | Drives you and nearby allies into a fighting song. Stacks on top of Focus and Ferocity | Harmony of the Warrior | no |
| **Harmony of the Wizard** | `harmony_wizard` | 1 | Sharpens the casters around you. Stacks on top of Force and Alacrity | Harmony of the Wizard | no |
| **Healer's Power** | `healers_power` | 1 | For 15 seconds every heal you cast lands for far more | Healer's Power | no |
| **Holy Mark** | `healer_mark` | 4 | The party's own Mark. Does not stack with a healer's | Blood Mark, Harmony Mark, Holy Mark, Life Mark | no |
| **Holy Soul** | `holy_soul` | 1 | Stance. Every skill costs 30% less MP, but you cast 10% slower and burn 50 HP a second | Holy Soul | no |
| **Indomitable** | `indomitable` | 1 | For 30s your buffs have an 80% chance to resist being cancelled/dispelled | Indomitable | no |
| **Last Stand** | `last_stand` | 1 | For 10s, the next blow that would kill you instead leaves you at 50% HP | Last Stand | no |
| **Mana Barrier** | `mana_barrier` | 1 | Diverts 70% of incoming damage to MP (0.5 MP per damage) for 30s, while MP lasts | Mana Barrier | no |
| **Mana Blessing** | `mana_blessing` | 2 | −20% physical and −10% magic skill MP cost | Soul Reinforcement (group), Mana Blessing | no |
| **Meditation** | `meditation` | 1 | Sit inside your own magic for 30s: MP floods back and your Physical Defence all but disappears. The first hit you take ends it | Meditation | no |
| **Reinforcement** | `wc_reinforcement` | 1 | Toggle. Brace yourself: greater physical defence for as long as you can pay for it | Reinforcement | no |
| **Rune of Drop** | `rune_drop` | 1 | Held rune: +5% DROP CHANCE on every monster drop while it is in your bag | Rune of Drop | no |
| **Rune of Exp/SP** | `rune_expsp` | 1 | Held rune: +5% experience AND +5% SP from monsters while it is in your bag | Rune of Exp/SP | no |
| **Rune of Experience** | `rune_exp` | 1 | Held rune: +5% EXPERIENCE from monsters while it is in your bag | Rune of Experience | no |
| **Rune of Gold** | `rune_gold` | 1 | Held rune: +5% GOLD from monsters while it is in your bag | Rune of Gold | no |
| **Rune of Sinister** | `rune_sinister` | 1 | Held rune: you gain NO experience and NO SP from monsters while it is in your bag. Gold and drops are untouched — grind for items without levelling | Rune of Sinister | no |
| **Rune of Sinners** | `rune_sinners` | 1 | A timed rune given by the Gods to punish those who sinned: experience, SP, gold and drops are ALL zero while it is in your bag. Bound to your soul for the time it has left — no keeper will accept it, and it cannot be sold, traded or destroyed. It leaves when it expires | Rune of Sinners | no |
| **Rune of Skillpoints** | `rune_sp` | 1 | Held rune: +5% SP from monsters while it is in your bag | Rune of Skillpoints | no |
| **Sharpening** | `wc_sharpening` | 1 | Toggle. Hone your weapon: greater physical attack for as long as you can pay for it | Sharpening | no |
| **Shield Mastery** | `shield_mastery` | 1 | Tank passive: greatly improves your shield's block chance and defence (only while a shield is equipped) | Shield Mastery | no |
| **Shield Reinforcement** | `shield_reinforcement` | 1 | Stance. Brace behind your shield: +300 P.Def and half again your block chance, for 15 MP a second | Shield Reinforcement | no |
| **Shrouding Hymn** | `shrouding_hymn` | 1 | For 1 minute, monsters that haven't already noticed you and your nearby allies leave you alone. Anything already chasing keeps chasing | Shrouding Hymn | no |
| **Spell Rune** | `rune_spell` | 1 | Spell Rune: +magic damage and cast speed while the rune is held | Spell Rune | no |
| **War Cry** | `might` | 2 | Battle shout: +30% Attack Power for 30s | Greater War Cry, War Cry | no |
| **War Focus** | `war_focus` | 1 | A 20-min focus: +15% attack speed and +25% PvP physical-skill & basic damage | War Focus | no |
| **War Rune** | `rune_war` | 1 | War Rune: +100% P.Atk (physical damage) while the rune is held | War Rune | no |

## 4. Every consumable-buff item

| Item | Rarity | Kind | Gives | Rung | Runs for | Where it comes from | Bar | Slot |
|---|---|---|---|---|---|---|---|---|
| Agility Potion (Lesser) | Common | Potion | Agility — +1 Evasion | 1 | 20 min | vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), craft | Consumable | yes |
| Agility Potion | Uncommon | Potion | Agility — +2 Evasion | 2 | 20 min | craft | Consumable | yes |
| Scroll of Agility | Rare | Scroll | Agility — +4 Evasion | 4 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Aim Potion (Lesser) | Common | Potion | Aim — +1 Accuracy | 1 | 20 min | vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), craft | Consumable | yes |
| Aim Potion | Uncommon | Potion | Aim — +2 Accuracy | 2 | 20 min | craft | Consumable | yes |
| Scroll of Aim | Rare | Scroll | Aim — +4 Accuracy | 4 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Alacrity Potion (Lesser) | Common | Potion | Alacrity — +15% Cast Speed | 1 | 20 min | vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), craft, drop | Consumable | yes |
| Alacrity Potion | Uncommon | Potion | Alacrity — +23% Cast Speed | 2 | 20 min | craft, drop | Consumable | yes |
| Scroll of Alacrity | Rare | Scroll | Alacrity — +30% Cast Speed | 3 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Scroll of Body | Rare | Scroll | Body — +35% Max HP | 6 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Bulwark Potion (Lesser) | Common | Potion | Bulwark — +8% P.Def | 1 | 20 min | vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), craft | Consumable | yes |
| Bulwark Potion | Uncommon | Potion | Bulwark — +12% P.Def | 2 | 20 min | craft | Consumable | yes |
| Scroll of Bulwark | Rare | Scroll | Bulwark — +15% P.Def | 3 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Common Healing Potion | Common | None | Common Healing — Restores 20 HP per second for 15s | 1 | 15s | vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), craft, box (Newbie Box), drop | Consumable | no |
| Uncommon Healing Potion | Uncommon | None | Common Healing — Restores 70 HP per second for 15s | 2 | 15s | vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), craft, box (Treasure Chest), drop | Consumable | no |
| Rare Healing Potion | Rare | None | Common Healing — Restores 150 HP per second for 30s | 3 | 30s | craft, box (Newbie Box), drop | Consumable | no |
| Common Mana Potion | Common | None | Common Mana — Restores 20 MP per second for 15s | 1 | 15s | vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary) | Consumable | no |
| Uncommon Mana Potion | Uncommon | None | Common Mana — Restores 70 MP per second for 15s | 2 | 15s | vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary) | Consumable | no |
| Rare Mana Potion | Rare | None | Common Mana — Restores 150 MP per second for 15s | 3 | 15s | craft | Consumable | no |
| Dash Potion (Lesser) | Common | None | Dash — +15 Move Speed | 1 | 15s | craft, drop | Consumable | no |
| Dash Potion | Uncommon | None | Dash — +30 Move Speed | 2 | 15s | craft, drop | Consumable | no |
| Dash Potion (Greater) | Rare | None | Dash — +45 Move Speed | 4 | 15s | craft, drop | Consumable | no |
| Dash Potion (Superior) | Epic | None | Dash — +50 Move Speed | 5 | 15s | craft, drop | Consumable | no |
| Dash Potion (Grand) | Legendary | None | Dash — +55 Move Speed | 6 | 15s | craft, drop | Consumable | no |
| Dash Potion (Supreme) | Mythic | None | Dash — +60 Move Speed | 7 | 15s | craft | Consumable | no |
| Dash Potion (Supreme) (Bound) | Mythic | None | Dash — +60 Move Speed | 7 | 15s | **nothing grants it** | Consumable | no |
| Scroll of Ferocity | Rare | Scroll | Ferocity — +35% critical damage | 6 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Scroll of Focus | Rare | Scroll | Focus — +30% critical rate | 6 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Force Potion (Lesser) | Common | Potion | Force — +15% M.Atk | 1 | 20 min | vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), craft | Consumable | yes |
| Force Potion | Uncommon | Potion | Force — +25% M.Atk | 2 | 20 min | craft | Consumable | yes |
| Scroll of Force | Rare | Scroll | Force — +32% M.Atk | 4 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Scroll of Frenzy | Rare | Scroll | Frenzy — −10% Max HP/MP, +8% P.Atk / M.Atk / attack & cast speed, +8 Move Speed, −8 Evasion | 6 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Fury Potion (Lesser) | Common | Potion | Fury — +15% Attack Speed | 1 | 20 min | vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), craft, drop | Consumable | yes |
| Fury Potion | Uncommon | Potion | Fury — +23% Attack Speed | 2 | 20 min | craft, drop | Consumable | yes |
| Scroll of Fury | Rare | Scroll | Fury — +33% Attack Speed | 3 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Scroll of Insight | Rare | Scroll | Insight — +100% magic critical rate | 6 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Might Potion (Lesser) | Common | Potion | Might — +8% P.Atk | 1 | 20 min | vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), craft | Consumable | yes |
| Might Potion | Uncommon | Potion | Might — +12% P.Atk | 2 | 20 min | craft | Consumable | yes |
| Scroll of Might | Rare | Scroll | Might — +15% P.Atk | 3 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Scroll of Serenity | Rare | Scroll | Serenity — +20% MP regeneration | 6 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Scroll of Soul | Rare | Scroll | Soul — +35% Max MP | 6 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Swift Potion (Lesser) | Common | Potion | Swift — +15 Move Speed | 1 | 20 min | vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), craft, drop | Consumable | yes |
| Swift Potion | Uncommon | Potion | Swift — +20 Move Speed | 2 | 20 min | craft, drop | Consumable | yes |
| Scroll of Swift | Rare | Scroll | Swift — +33 Move Speed | 3 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Scroll of Vigor | Rare | Scroll | Vigor — +20% HP regeneration | 6 | 60 min | craft, box (Blessing Box) | Consumable | yes |
| Ward Potion (Lesser) | Common | Potion | Ward — +10% M.Def | 1 | 20 min | vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), vendor (Apothecary), craft | Consumable | yes |
| Ward Potion | Uncommon | Potion | Ward — +20% M.Def | 2 | 20 min | craft | Consumable | yes |
| Scroll of Ward | Rare | Scroll | Ward — +30% M.Def | 4 | 60 min | craft, box (Blessing Box) | Consumable | yes |

