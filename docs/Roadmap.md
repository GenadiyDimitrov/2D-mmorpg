# Roadmap — L2Clone (branch Gena)

Development TODO for game systems / in-game functions, bucketed by time horizon.
This is the "what to build" list (the "what to verify" list is `docs/TestChecklist.md`).
Claude keeps this updated as work moves between buckets.

Legend: `[ ]` open · `[~]` partially done · `[>]` blocked/waiting · `[x]` done (kept briefly for context).

---

## NOW (active / immediate)

- [~] **Tune placeholder numbers** after the next playtest: fighter weapon masteries, armor
  masteries, healer powers, mob modifiers, caster bow penalty. (See `docs/TestChecklist.md`.)
- [ ] **Cleric-solos-a-30-mob** balance pass (owner target): mob HP/atk vs cleric melee+heal.
- [ ] Low-level **physical mob damage** so mobs don't ~one-shot players (magic/phys mob parity).

## NEXT (clear, mostly self-contained — can do without owner input)

- [~] **Combat primitives layer** (prerequisite for disciplines, bosses, PvP). Build to
  `docs/Disciplines.md` rules. **Started:** the ATK-vs-CON/WIT **debuff hit-contest**
  (`StatCalculator.DebuffLandChance`, 10–90%, 50% at equal, bosses immune) + **Slow**
  (move-speed %, first contested CC; demo skill "Frost Bind" for nukers) + physical
  **`[Double]` crit** (`SkillDef.CanDouble`, ×2 from higher of DEX/ATK cap 30%; demo skill
  "Cleaving Strike" for warriors; existing skills unchanged) + **Stun & Fear** (contested,
  action-locking; demo skills "Shield Bash"/"Terrifying Roar") + **Root-via-contest** +
  physical **Slow** (demo "Hamstring") + **skill-damage % stat** (`SkillDamageBonus`,
  passive `SkillDamagePct` / buff `BuffSkillDamage`; demo "War Focus"). Existing
  non-contest debuffs left on the fizzle model (owner: new-only). **Still to do (P2 heavy):**
  DoT+stacks (bleed/poison/venom), absorb shields, taunt/real threat, blink/charge,
  knockback, stealth, mana shield, lethal-save, traps, conditional damage (+% vs slowed/rooted).
- [ ] **Base-class armor mastery + universal penalty → data** (only 2nd classes are data so
  far). Finishes the [[stats-via-skills-not-hardcoded]] migration. (Note: changes the
  unlearned-penalty semantics slightly — confirm intent.)
- [x] **1H vs 2H weapon-mastery gating** — done: `Entity.WeaponHands` tracks equipped hands;
  `WeaponMasteryProfile.RequiredHands` gates the bonus (Warrior = 2H only, Tank = 1H only).
- [x] **Toggle-skill mechanic** + **Healer "Combat Stance"** — done: a toggle skill applies
  its self-buff indefinitely (click again / double-click buff to end); the stance trades
  +50% P.Atk for −50% M.Atk. (Numbers untuned. Future: per-tick MP drain for toggles.)
- [x] **Skill reagents/consumables** — done: `SkillDef.ConsumableId`/`ConsumableAmount`; a
  skill with a reagent checks it up front and consumes it on cast completion (refunded on
  interrupt). Empty = casts freely. No skill uses it yet — assign to "ultimate" skills.
- [ ] **Premium class-reset item** — lets a player undo the irreversible class-chain commitment.

## LATER (bigger systems)

- [>] **3rd-class discipline kits** — 12 disciplines × per-race skill lists. Framework +
  flat stat leans exist; needs the combat primitives layer first, then per-race kits.
  Lightbringer (healer) + Warchanter (buffer, gets a "Prophecy" party buff) are first up.
  ([[discipline-skills-plan]], [[class-tier-design]], [[mage-path-wip]])
- [ ] **Party / grouping system** — replaces the current "allies in radius" stand-in for
  party heals/buffs; shared XP, party UI.
- [ ] **Active mob skills** — mobs casting spells / using specials (today mobs only have
  PASSIVE stat modifiers). ([[mob-modifiers]])
- [ ] **Boss mechanics** — ±10-level rule, boss skills, enrage; raid-boss timers already exist.
- [ ] **Pets & summons** — immovable healing totem, class pets (Trapper/tank), mage
  summoner. ([[pets-summons-design]])
- [ ] **Stronger buff versions** — the NPC newbie buffer is a 3rd-class-max stand-in; real
  Buffer/4th-class buff tiers.
- [ ] **Position bonuses** — backstab / flanking damage (hook reserved).
- [ ] **PvP / PvE multipliers** — wire the multiplier hooks (currently default 1.0).
- [ ] **Perfect / excellent block** — shield block tiers above the current flat block.
- [ ] **Class-vs-class balance matrix** (buffed) + damage-K tuning once all kits exist.
  ([[class-race-identity]])

## EVENTUALLY (long-term / large)

- [ ] **The real client** — 2.5D Unity (no Z axis; server stays 2D), reusing `Game.Shared`
  + `NetworkChannel`. The WPF app is only a test harness. ([[client-3d-and-los-design]])
- [ ] **Line of sight** — server-side LoS using STATIC occluder data (not entities), for
  the new client. ([[client-3d-and-los-design]])
- [ ] **Instances / dungeons**.
- [ ] **Castles + vault** — consumes the reserved `VendorBuyTaxRate` hook; siege loop.
- [ ] **4th class tier** — the top of the 4-tier tree. ([[class-tier-design]])

## BLOCKED / WAITING ON OWNER

- [>] **Lightbringer + Warchanter CSVs** — 3rd-class kits @40 need owner's skill numbers.
- [>] **Two real starter armor SETS** — owner to provide; current newbie light/robe sets are
  placeholders. ([[item-properties-boxes]])
- [>] **Newbie items via quests** — give the starter weapon/armor/jewel boxes through lvl
  6/8/10 quests (owner's plan).

## DROPPED (decided NOT to build — don't re-add)

- ~~Magic-resist layer~~ — magic mitigation is ONLY mDef (divisor) + the magic-fail/fizzle
  floor. "mRes" in owner CSVs = the fizzle floor. No flat magic-damage-reduction stat.
- ~~Soulshots / spiritshots~~ — the leveled **Attack-training** passive is the permanent
  replacement; there is no shot consumable system.
