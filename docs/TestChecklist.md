# Test Checklist — L2Clone (branch Gena)

Running list of things to verify in-game. Claude keeps this updated as features land;
the owner tests manually and ticks items off. **`[ ]` = not tested, `[x]` = verified,
`[~]` = tested, needs tuning.** Newest features first. When asked to test, Claude shows
this file.

---

## To test now (this session — 2026-06-27)

### Combat primitives P2: DoT (separated effect + stack counter) — NUMBERS UNTUNED
- [ ] Venomweaver "Rupture" @40 applies a bleed: a FLAT "DoT" tick each second + 15% slow; reapplying refreshes 30s and builds a stack (counter is hidden — not on the buff bar).
- [ ] Bleed tick damage does NOT grow with stacks (it's the damage effect); stacks only fuel the burst.
- [ ] "Detonate Wounds" @44 hits for ~damage × stacks (×10 at full), removes the COUNTER, and leaves the bleed DoT ticking.
- [ ] Detonate consumes only ITS line's stacks (ConsumeStackKey) — another applier's stacks are untouched.
- [ ] Bleed damage effect overrides by Rank (a stronger bleed replaces a weaker); counters stay independent.
- [ ] A DoT can finish the kill (credit + drops go to the applier).
- [~] Poison/venom + their −AS/cast, −atk/def secondaries not authored (need debuff channels outside AnyBuff). Cure/cancel skills not built yet.

### Cure / cancel (dispel) — NUMBERS UNTUNED
- [ ] Healer "Antidote" @25 removes poison/venom from an ally (or self); does NOT remove other debuffs (slow/bleed/stun).
- [ ] Nuker "Dispel Magic" @35 on an enemy strips up to 2 random beneficial buffs (test vs a buffed player, or self-cast a buff then have someone dispel).
- [ ] Internal DoT stack counters are NOT removed by cure/cancel; a non-Cancellable effect is immune.
- [ ] Existing Lightbringer full Cleanse still removes all debuffs (empty DispelMask = all).

### Stack / effect visibility
- [ ] A stacking buff on YOU shows "Name xN" on the buff bar (count updates as it stacks).
- [ ] Expand the target window on a bled/slowed mob → "Effects:" line lists its active effects with stacks (e.g. "Rupture (stacks) x5", "Slow") — so you can time Detonate Wounds.
- [ ] Effects line refreshes ~1/s while the panel is open.

### Combat primitives: generalized stacking (per-stack effect table) — NUMBERS UNTUNED
- [ ] Tempest "Creeping Frost" @44: each landing cast adds a stack — slow 10% → 20% → 30% on stacks 1-3, then the **4th stack FREEZES** the target (stun, no slow). Same skill, different effect per stack level.
- [ ] A resisted cast does NOT add a stack (stack only on success); re-landing refreshes the timer.
- [ ] Rogue bleed counter caps at its skill's MaxStacks (10) — editable per skill, not a global constant.
- [ ] A non-stacking buff/debuff (MaxStacks 1) behaves exactly as before.

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

### Combat primitives P1: Root + physical Slow + skill-damage% — NUMBERS UNTUNED
- [ ] Nuker learns "Entangling Roots" @40 (magical root, ATK-vs-WIT): target can't move for 8s but can still act.
- [ ] Warrior learns "Hamstring" @40 (PHYSICAL slow, ATK-vs-CON, −60% MS) — confirms slow exists in both schools (vs the magical Frost Bind).
- [ ] Warrior learns "War Focus" @40 (20-min self-buff): +15% attack speed shows in the stats window; the +25% PvP skill/basic damage is latent (no PvP yet). Confirms the damage matrix wiring (PvE damage unchanged by it).
- [ ] Root lands via the contest (not fizzle); existing non-contest debuffs (Weakness/anti-heal) still behave as before.

### Combat primitives P1: conditional damage ("Glacial Spike") — NUMBERS UNTUNED
- [ ] Nuker learns "Glacial Spike" @44; on a normal target it does power-90 damage.
- [ ] After Frost Bind (slow) or Entangling Roots (root) on the same target, Glacial Spike hits ~50% harder.
- [ ] The bonus only applies while the target is slowed/rooted (wears off when the CC ends).

### Combat primitives P1: Stun + Fear ("Shield Bash" / "Terrifying Roar") — NUMBERS UNTUNED
- [ ] Vanguard learns "Shield Bash" @40 (stun 3s); warriors learn "Terrifying Roar" @40 (fear 5s).
- [ ] Stun: target can't move, cast or attack for the duration (a mob freezes; a casting target's cast breaks).
- [ ] Fear: target can't cast or attack but CAN still move.
- [ ] Both land via ATK-vs-CON contest (10–90%); bosses immune; cleansable; show on the target/expire normally.
- [ ] While YOU are stunned/feared, your skills are refused ("You are stunned." / "...too afraid to act.").

### Combat primitives P1: physical [Double] crit ("Cleaving Strike") — NUMBERS UNTUNED
- [ ] Warrior (Ravager/Warlord) learns "Cleaving Strike" @40; it sometimes hits for ~2× (shown as Crit).
- [ ] Double chance scales with the higher of DEX/ATK, capped 30%; ordinary skills never double.
- [ ] Existing physical skills (Power Strike, Mighty Blow, etc.) crit exactly as before (basic crit path unchanged).
- [ ] A shield can still block a non-doubled Cleaving Strike; a double ignores block (like a crit).

### Combat primitives P1: debuff contest + Slow ("Frost Bind") — NUMBERS UNTUNED
- [ ] Nuker (Magus/Tempest) learns "Frost Bind" @40; casting it on a mob visibly halves its move speed for 10s.
- [ ] Landing varies with the ATK-vs-WIT contest: high-WIT targets resist more (shows "Fail"/resisted), 10–90% bounds.
- [ ] Slowed target still moves (never fully stopped — that's Root); slow is cleansable / expires after 10s.
- [ ] Existing debuffs (Weakness, anti-heal, Root) behave exactly as before (not switched to the contest).

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
