# Test Checklist — L2Clone (branch Gena)

Running list of things to verify in-game. Claude keeps this updated as features land;
the owner tests manually and ticks items off. **`[ ]` = not tested, `[x]` = verified,
`[~]` = tested, needs tuning.** Newest features first. When asked to test, Claude shows
this file.

---

## To test now (party window + mob cast-bar UI — 2026-07-07)

### Party window (WPF client)
- [ ] Target another player → the target frame shows an **"Invite to Party"** button. Click it →
  they get a centered **accept/decline prompt**; you see a "Party invite sent" chat line.
- [ ] On accept, both of you show a **Party panel** (top-left, under the vitals/buff bar) listing
  every member with name/Lv/class + **live HP and MP bars**. Leader has a ★.
- [ ] Leader sees a small **✕ kick** button on other rows (not on self); a non-leader sees none.
  Kicking removes that member (their panel hides); the kicked player gets a chat notice.
- [ ] **Leave** button removes you; when a party drops below 2 it **disbands** (everyone's panel
  hides). The invite button is hidden for players already in your party, and for non-leaders.
- [ ] Roster HP/MP bars update as members take damage / heal (server refresh).

### Party loot rules (2026-07-07)
- [ ] The party panel shows a **Loot** dropdown. Only the **leader** can change it (disabled for
  members); changing it posts a "Loot rule set to …" chat line to everyone.
- [ ] **Finders Keepers** (default): item drops go to whoever landed the kill (as before).
- [ ] **Random**: each item drop goes to a random in-range member; others see "X looted Y."
- [ ] **Round Robin**: consecutive drops rotate through in-range members in join order.
- [ ] **Leader Only**: all item drops go to the leader (if in range; else the killer).
- [ ] **Gold is ALWAYS split** evenly among in-range members regardless of the loot rule (killer
  keeps the odd remainder); solo = the killer gets it all.
- [ ] Boss/elite crafting-mat pile goes to a single recipient per the loot rule.
- [ ] Only members **in share range** (ViewRange) and alive are eligible; out-of-range members are
  skipped (loot falls back toward the killer where applicable).

### Mob / boss cast-bar
- [ ] When a mob/boss begins a visible cast (e.g. the boss **"Devastating Slam"**), an orange
  **cast-bar appears under its nameplate** and fills over the cast time, then disappears.
- [ ] Interrupting / killing the caster (or the cast finishing) clears the bar cleanly.

### Boss unique skills + phases + adds (2026-07-07)
- Fight the **Valley Treant Lord** (Boss zone ~(24000, 45000), L60). Bring a party/high level — it
  has 20× HP. (Long real respawn; use debug teleport to reach the zone.)
- [ ] From full HP it casts **Devastating Slam** (telegraphed slam, dmg + stun) on its reuse timer.
- [ ] At **50% HP** it announces + **enrages** (rage buff, faster/harder hits) and **summons 2 adds**
  (bogwood, ~L52) that immediately attack whoever it's fighting.
- [ ] Below 50% it also starts casting **Thorn Nova** (wider magic burst + a slow) — a second,
  distinct boss skill it did NOT use above 50%. Its name shows in the cast-bar.
- [ ] At **25% HP** it announces the thorn storm (flavor line).
- [ ] Leash/reset (walk it home) **re-arms** the phases and clears its skill reuse; a fresh pull
  starts at Slam-only again. Adds do NOT respawn when killed.
- [ ] Other bosses with no profile still use the plain slam (unchanged).

---

## To test now (ranged + caster mobs — 2026-07-03)

### Archer mobs (orc_archer L16, dune_orc_archer L40, fen_lizardman_archer L39, dread_archer L69)
- [ ] They shoot from ~450 range (don't run into melee), hit noticeably harder (×2 P.Atk), and
  are squishier (light armor: lower P.Def, a bit more evasion). Bow attacks apply bow variance.

### Mage mobs (watcher_eye L26, aether_wisp L58, rift_portling L40, radiant_mage L82)
- [ ] NO basic attacks — they only cast. Long nuke from ~600 (4s cast), short jab up close (~150,
  1.5s). Damage scales with mob level (nuke pow 18→129, jab 7→33).
- [ ] Higher M.Atk, lower P.Atk/P.Def than a same-level melee mob.
- [ ] They burn MP per cast; when MP runs out they stand HELPLESS (no attacks) — a free kill if you
  outlast their mana. (Mob cast-bar now renders under the nameplate — see the 2026-07-07 section.)
- [ ] rift_portling = a beefy caster (champion HP) that nukes; watcher_eye also has high M.Def.

### Golem-type resist (obsidian_knight L63, Duskvale)
- [ ] Sword/dual hits land for less (Pierce ×1.43 P.Def), arrows much less (Bow ×2), blunt MORE
  (×0.5). Inspect shows the resist lines.

---

## To test now (mob overhaul — 2026-07-02)

### Mob base-stat curve (docs/mobs/mob_base_stats.csv) — BIG BALANCE SHIFT
- [ ] Mobs now use the CSV level curve → ~2-3× their old HP/def/atk. Fights should feel
  meaningfully longer/harder. Inspect a mob (▼ on the target frame) and sanity-check its
  HP/P.Def/M.Def/P.Atk vs the CSV row for its level (should match at authored levels).
- [ ] **Cleric can still SOLO a same-level mob** (target: ~L30) — slower but possible.
- [ ] Low-level mobs don't ~one-shot players (physical mob damage sane at 2-3× atk).

### Weapon-type resistance (P.Def route)
- [ ] `obsidian_knight` (Lv 63, Duskvale): sword & bow hits land for noticeably LESS, a
  blunt weapon for MORE (vs its normal P.Def). Inspect shows "Sword/Dual Resist / Bow Resist
  / Blunt Weak" lines.
- [ ] `watcher_eye` (Lv 26) is hard for mages (high M.Def) / easy for fighters; `rift_portling`
  (Lv 40 champion) has ~3.5× the normal L40 HP.

### New 80-mob roster + zones + drops
- [ ] Every field zone spawns the new named mobs at their natural level (levels roughly match
  each zone's band). L80-85 mobs appear in Frostmere (9000,17600).
- [ ] Drops still flow (potions/gear/scrolls by level; gear TYPE by family — undead/caster→robe,
  animal→light, insect→daggers, demon/dragon→heavy, humanoid→sword).
- [ ] Class-change hunt quests (orc_archer/skeleton_grunt/shield_skeleton, Lv 16-21) and the
  3rd-class chain (medusa/marsh_mantis_soldier/fen_lizardman_archer, Lv 34-39) count kills.
- [ ] Boss = Valley Treant (Lv 60, south), Elite = Emberwyrm Drake (Lv ~78, NW) spawn & fight.

---

## To test now (this session — 2026-06-29)

### Mage no auto-attack after a spell
- [ ] After casting an OFFENSIVE spell on a mob, a mage (Nuker/Healer) no longer runs at
  the target to melee — it stays put. Fighters still flow skill → auto-attack as before.

### Physical skills scale by ATTACK speed (not cast speed) — NUMBERS UNTUNED
- [ ] A fighter's physical skill cast time now follows ATTACK speed (DEX + weapon), not the
  WIT-driven cast speed — so a fighter no longer casts melee skills sluggishly.
- [ ] Faster attack speed (buffs / fast weapon) shortens physical-skill cast; a slow heavy
  2H weapon lengthens it slightly. Magic / buff / heal skills still use cast speed.
- [ ] NEXT CSV: owner gives fighters a real `CastTicks` per physical skill so this can be
  felt against the actual attack speed (heavy strikes ~1s, lighter ~0.1–0.2s).

---

## Playtest 1 results (2026-06-28)

**Verified working:** damage & crits (incl. [Double]) at all levels; control lands (slow/
root/stun/fear); DoT + burst; defensive skills + Provoke/threat; movement (blink/knockback);
weapon masteries; mage damage feels OK for now.

**Fixed this round (RE-TEST next launch):**
- [ ] **Restore Mana** now costs ~1.2× what it restores (72 MP → 60) and CANNOT target self
  or another mana-restorer (healer→non-healer only).
- [ ] **Phase Shift** no longer needs a target — blinks ~400 away from the nearest enemy.
- [ ] **Cast bar** shows the class skill name (e.g. "Moonlight Bolt"), not the base form.
- [ ] **Debug** menu: "Level +10" and "Learn all skills (to my level)" buttons.

**Open items:**
- [ ] **FIGHTER BALANCE (big)** — Venomweaver burst ~1500; a Lv-49 tank solos hordes of Lv-64
  mobs. Skills work as intended; numbers need a tuning pass (damage-out / mastery / skill power).
- [ ] **Stacks not visible as a LEVEL on the mob** — expand the target window (▼) to see
  "Effects: Creeping Frost x3"; consider a stack readout on the always-visible target frame.
- [ ] **Friendly target dummy** to test heals/cure/buffs on an ally — needs ally-targeting
  (likely after PvP, so you can also damage/debuff friendly dummies).
- [ ] **Skill-detail TITLE shows base name** (not the class name) — owner will give the exact
  skill + race/2nd/3rd class next test. Suspect: client `_myThirdClass` not synced after a
  DEBUG 3rd-class change, so discipline-renamed skills fall back to the base name.
- Dummies don't regen — owner: don't care (they never die via the 1-HP floor).

---

## To test now (this session — 2026-06-27)

### Training Grounds (test dummies)
- [ ] A cluster of immortal **Training Dummy (Lv 20/40/60/80)** spawns at ~(22500–25500, 4000) — reach via debug Teleport → Zones.
- [ ] Dummies never move, never attack, and never die — but they DO take (and display) damage; HP drops then regens (~1M HP, ~10k/s regen, floored at 1).
- [ ] Use them to verify [Double] crits, DoT ticks/stacks (Effects line in the target window), slow/stun/etc. land, and damage scaling.

### Movement: blink + knockback — NUMBERS UNTUNED
- [ ] Phantom "Shadowstep" @40: teleports you behind the target, then hits ([Double]).
- [ ] Trapper "Repelling Shot" @40: damages and shoves the target ~200 away.
- [ ] Tempest "Phase Shift" @48: blinks you ~400 away from the target (escape).
- [ ] Blink/knockback respect world bounds; the moved entity stops its current path (doesn't slide).

### Taunt + real threat/aggro — NUMBERS UNTUNED
- [ ] A mob now targets the highest-THREAT attacker (threat = damage dealt), not just the last hitter — e.g. a high-damage player pulls aggro off a low-damage one.
- [ ] Tank "Provoke" @40 forces the mob onto the tank (its target switches to you) and holds ~3s even if others out-damage you.
- [ ] Detaunt (e.g. rogue Shadowstep/BattleFury detaunt) sheds ~90% of your threat → the mob retargets to the next-highest, or leashes home if no one else.
- [ ] Mob still leashes/resets correctly (threat clears on reset).

### Combat primitives P2: poison & venom (Venomweaver per-race trio) — NUMBERS UNTUNED
- [ ] Venomweaver DoT is now per race: Human = bleed (−MS), **Elf = poison** (Toxic Sting/Burst), **Ork = venom** (Envenom/Venom Burst).
- [ ] Poison (Toxic Sting): magic DoT (ATK-vs-WIT) + slows the target's attack & cast speed ~15% (stat window of a player target; mobs just attack/cast slower). Toxic Burst spends stacks.
- [ ] Venom (Envenom): physical DoT (DEX-vs-CON) + lowers target attack ~15% and defence ~15% (a venomed mob hits softer and takes more). Venom Burst spends stacks.
- [ ] These secondary debuffs are cleansable and expire with the DoT; new DebuffAtk/DebuffAtkSpeed/DebuffCastSpeed channels don't affect buffs.

### Combat primitives P2: DoT (separated effect + stack counter) — NUMBERS UNTUNED
- [ ] Venomweaver "Rupture" @40 applies a bleed: a FLAT "DoT" tick each second + 15% slow; reapplying refreshes 30s and builds a stack (counter is hidden — not on the buff bar).
- [ ] Bleed tick damage does NOT grow with stacks (it's the damage effect); stacks only fuel the burst.
- [ ] "Detonate Wounds" @44 hits for ~damage × stacks (×10 at full), removes the COUNTER, and leaves the bleed DoT ticking.
- [ ] Detonate consumes only ITS line's stacks (ConsumeStackKey) — another applier's stacks are untouched.
- [ ] Bleed damage effect overrides by Rank (a stronger bleed replaces a weaker); counters stay independent.
- [ ] A DoT can finish the kill (credit + drops go to the applier).
- [~] Poison/venom + their −AS/cast, −atk/def secondaries not authored (need debuff channels outside AnyBuff). Cure/cancel skills not built yet.

### Mana shield + lethal save ("Mana Barrier" / "Last Stand") — NUMBERS UNTUNED
- [ ] Magus "Mana Barrier" @44: while up, taking damage drains MP (0.5 per damage) for 70% of the hit; HP loss is reduced; stops diverting when MP runs out.
- [ ] Bulwark "Last Stand" @44: a blow that would kill you within 10s instead leaves you at 50% HP, and the buff is consumed (one save).
- [ ] Both interact correctly with absorb shields (shield soaks first, then mana shield, then lethal save).

### Absorb shields ("Aegis") — NUMBERS UNTUNED
- [ ] Tank (Bulwark/Vanguard) "Aegis" @40: a self-shield absorbing 8% of max HP for 15s shows on the buff bar.
- [ ] While shielded, incoming damage drains the shield first; HP only drops once it's depleted; the shield buff vanishes when empty.
- [ ] Works vs all damage types (basic, skills, DoT ticks) — they all route through ApplyDamage.
- [~] Known cosmetic: floating combat text shows pre-absorb damage (HP loss is correct). To refine later.

### Cure / cancel (dispel) + cancel resist — NUMBERS UNTUNED
- [ ] Healer "Antidote" @25 removes poison/venom from an ally (or self); does NOT remove other debuffs (slow/bleed/stun).
- [ ] Nuker "Dispel Magic" @35 on an enemy strips up to 2 random beneficial buffs (test vs a buffed player, or self-cast a buff then have someone dispel).
- [ ] Internal DoT stack counters are NOT removed by cure/cancel; a non-Cancellable effect is immune.
- [ ] Existing Lightbringer full Cleanse still removes all debuffs (empty DispelMask = all).
- [ ] Tank "Indomitable" @48 (+80% cancel resist 30s): while up, most of the tank's buffs survive a Dispel Magic (each rolls an 80% save). Cure on debuffs is unaffected by resist.

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
