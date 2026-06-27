# Test Checklist — L2Clone (branch Gena)

Running list of things to verify in-game. Claude keeps this updated as features land;
the owner tests manually and ticks items off. **`[ ]` = not tested, `[x]` = verified,
`[~]` = tested, needs tuning.** Newest features first. When asked to test, Claude shows
this file.

---

## To test now (this session — 2026-06-27)

### Expandable target window (commit ccb5805)
- [ ] Targeting a mob shows a `▼` expand button on the target frame; plain NPCs (vendor/gatekeeper) show no button.
- [ ] Clicking `▼` opens the panel and shows HP/MP, P/M.Atk, P/M.Def, Acc/Eva/Crit.
- [ ] A mob's passive lines appear (e.g. Green Slime → "Magic Monster", "M.Def +100%", "P.Def −50%"; Stone Golem → "Armored Brute").
- [ ] Bow/Crit resist lines show only when non-zero, and are NOT duplicated.
- [ ] Panel refreshes ~once/sec during a fight (HP/MP track damage).
- [ ] `▲` collapses it; switching targets re-queries; clearing target (Esc/✕) hides it.

### Weapon masteries — fighters (commit a574309) — NUMBERS UNTUNED
- [ ] Learnable @20 (500 SP) in the skills window for Tank/Warrior/Rogue/Archer.
- [ ] Bonus applies ONLY while the matching weapon is held; no penalty for a "wrong" weapon.
- [ ] Warrior "Two-Hand Mastery": sword +15% pAtk/+3% crit; blunt +12% pAtk/+10 acc.
- [ ] Rogue "Dual Mastery": dual +10% pAtk/+5% crit/+15% crit dmg.
- [ ] Archer "Bow Mastery": bow +12% pAtk/+20% crit dmg/+5 acc.
- [ ] Tank "Weapon Expertise": sword/blunt +6% pAtk/+5–10 acc.
- [ ] Stat window reflects the change when you swap weapons.
- [ ] **1H/2H gating**: Warrior bonus applies ONLY with a 2H sword/blunt (not the 1H sword); Tank ONLY with a 1H sword/blunt (not the 2H greatsword). Dual/bow unaffected (always 2H).
- [~] Tune the percentages once the feel is clear.

### Mage masteries (commit 361127f)
- [ ] Nukers learn **Spell Mastery** (same as healers, @20/25/30/35); it replaces base Weapon Mastery (no double-apply).
- [ ] Caster **bow penalty**: a mage holding a bow casts at ~half speed (cast bar ~2× longer). Inert with staff/other weapons.
- [ ] No "Staff Mastery"/"Mace Mastery" anywhere (removed).

### Class-change dialog blurbs (commits f754f11, f03b23e)
- [ ] 2nd-class NPC shows the archetype blurb under each class option.
- [ ] 3rd-class (grandmaster) NPC shows the discipline blurb per option.

### Skill reagents / consumables + Nuker "Elemental Burst" (NEW) — NUMBERS UNTUNED
- [ ] Debug → Consumables → "Elemental Stone +10" grants 10 stones per click (stacks).
- [ ] Nuker 3rd class (Magus/Tempest) learns "Elemental Burst" @40, then 44/48/…/72/75 (10 levels, power 150→250).
- [ ] Casting without ≥10 stones is refused up front: "requires 10x Elemental Stone".
- [ ] Casting with stones works and consumes exactly 10 (inventory updates); damage scales with skill level.
- [ ] Stones are NOT consumed if the cast is interrupted / target lost.
- [ ] Other skills (empty `ConsumableId`) cast freely as before.

### Toggle skills + Healer "Combat Stance" (NEW — toggle mechanic) — NUMBERS UNTUNED
- [ ] Cleric learns "Combat Stance" @20; clicking it activates (costs 20 MP), clicking again deactivates (free).
- [ ] Active stance: P.Atk +50%, M.Atk −50% in the stats window; melee hits harder, heals/Holy Bolt weaker.
- [ ] The stance shows on the buff bar with `⟳` (no countdown); double-clicking it also turns it off.
- [ ] Stance does NOT expire over time; it clears on death/relog (runtime-only).
- [ ] No MP drain while held (activation cost only — by design for now).
- [~] Tune the ±50% swap once melee-cleric farming is tested.

---

## Tuning targets (owner-stated)

- [ ] **Cleric can solo a same-level (~30) mob** — slower than a fighter, but possible (not impossible, not two-shot).
- [ ] Low-level physical mobs do NOT ~one-shot players (magic-vs-physical mob parity).
- [ ] Mage TTK ~60s @75 is acceptable pre-CC — do NOT over-buff mage damage.
- [ ] Healer numbers: heals, Force (interrupt resist), Frenzy.
- [ ] Armor-mastery numbers per archetype (bonuses + untrained penalties).
- [ ] Mob passive modifiers (Magic Monster / Armored Brute) feel right vs mage/fighter.
- [ ] NPC newbie buffer set (Might/Force/Focus/Speed/Body/Frenzy) applies and shows stats.

---

## Carryover from prior sessions (verify still good)

- [ ] Buff/effect layer: Might applies def/atk; Speed applies cast speed; Force applies M.Atk @rank2.
- [ ] Buff bar: double-click/✕ drops a buff and stats update.
- [ ] Economy: merchants reject untradeable newbie items; boxes (random + selection) open and grant loot.
- [ ] Jewels: 2 rings / 2 earrings / 1 necklace caps enforced; jewel attributes roll.
- [ ] Debug teleport tab: NPCs / Zones / Cities; zones drop you ~400 outside the spawn ring.
- [ ] Enchant/reroll popup matches the inventory (no ±1 desync).
- [ ] Per-race Holy Bolt name (Human Holy / Elf Moonlight / Ork Spirit Bolt).
