# Buff ladders — improved buffs as groups of single buffs

**Status: steps 1-4 BUILT 2026-07-31 (0.36.0).** The mechanism and the whole speed group ship;
steps 5-6 (client grouping, the other families) are still open — see *Build order* at the bottom.
Supersedes the buff half of playtest-15 §2 (`docs/testing/Playtest-15.md`).

## Naming (owner, 2026-07-31)

A **single** buff is named for its effect and nothing else; the **group** that hands out several
of them is named "Improved …" or as a compound. So the cleric's buff is **Improved Speed** and it
gives you Swift, Alacrity, Agility and Haste. Applied so far:

| Effect | Single buff | Group |
|---|---|---|
| move speed | Swift | **Improved Speed** |
| cast speed | Alacrity (*not* "Force" — that name belongs to M.Atk) | |
| evasion | Agility | |
| attack speed | Haste | |

Reserved for the families still to author (owner's words): **Force** = M.Atk, **Force Defence** =
M.Def, group **Improved Force**. **Body** = Max HP, **Spirit** = Max MP, plus Body Regeneration and
Spirit Regeneration, group **Body and Spirit** (not "Body" — the group names both halves).

## The problem

Buff potions currently **stack** with class buffs instead of competing with them, because
stacking is decided by `BuffKey` and the potion (`pbuff_speed`) and the cleric buff
(`holy_speed`) are different keys. A player with the cleric's Speed *and* a Swiftness potion
gets both move-speed bonuses added — stronger than intended.

Fixing it by giving them the same key is too coarse: the cleric's Speed is *one* buff carrying
move + cast + evasion + attack speed, so a single key forces an all-or-nothing decision. A rare
Force potion (30% cast) would be refused in favour of a low-level buff's 15% cast, leaving the
player strictly worse off with no recourse but to cancel the whole buff.

## The decision

**An improved buff is a bag of single buffs.** The cleric/warchanter "Speed" does not apply a
buff of its own — it applies four *children* (swift, force, agility, haste), each an ordinary
buff on its own family key with its own rank. Each child resolves independently against whatever
the player already has, using the rank rule already implemented in `ApplyBuff`.

The client shows the group as **one icon** (see *Display* below), so it still reads as a single
buff to the player.

Consequences, all intended:

- A rare Force potion **can** override the cast child of a low-level Speed buff, and the move
  child is untouched. No need to cancel the whole buff.
- A full set of rare scrolls covers the entire base layer, exactly matching a max-level Speed
  buff. That is deliberate — see *Harmony*.
- Nothing needs an override/exclusion table. There is one number line per effect.

### Rejected alternative

An `Override[]` + `OverrideRank` collection on each buff level, listing the potion/scroll keys it
outranks. Rejected: it is an N×M relation that must be hand-maintained (one buff family × every
consumable × every level), it rots silently when a new consumable is added (the failure mode is
"the potion stacks again" — invisible until a playtest), it needs new fields on `SkillDef`,
`SkillLevel` and `BuffInstance` (protocol bump), and it still cannot express partial overlap.

## Mechanism

### Rank = position on the family's value ladder

Every effect that competes gets a **family key** (`spd_move`, `spd_cast`, …). Every source that
grants that effect gets an integer `Rank` = its position on that family's list of values, sorted
weakest to strongest. `ApplyBuff` rule 1 (already written, `GameLoopService.cs:6100`) does the
rest: lower rank is refused, equal-or-higher replaces.

⚠ **Rank is not rarity.** For the four speed families they coincide (three values, C/U/R). For
scroll-only families they do not: a scroll's rarity is its *price/drop* tier, chosen because it
has no potion analogue, not its power position. An Epic Health scroll sits at rank 2 of 6.

⚠ **One family = one modifier mode.** All flat or all percent. A family mixing `+30 flat` and
`+20%` cannot be rank-ordered honestly, because which is stronger depends on the base stat.

### `ChildBuffs`

New field on `SkillDef` / `SkillLevel`: an array of child skill ids. Each child is an ordinary
`SkillDef` in the catalog carrying one effect, one family `BuffKey`, and its `Rank`. Resolved
per level like `MagnitudesAt(level)`.

An improved buff with `ChildBuffs` applies **no buff of its own** — `ApplyBuff` fans out to the
children and returns. Duration, target mode and MP cost stay on the parent.

### Three code changes

1. **`ApplyBuff` fan-out** — if `def.ChildBuffs` is non-empty, loop and apply each child
   (`refresh: false`), then refresh once. ~10 lines.

2. **Equal rank keeps the longer remaining time.** `GameLoopService.cs:6104` currently replaces
   whenever `Rank >= existing`, which means a 20-minute potion silently eats a 1-hour scroll of
   the same tier — and potions/scrolls share tiers, differing *only* in duration, so this fires
   constantly. On equal rank take `Math.Max(existing.TicksRemaining, incoming)`.
   **Load-bearing, not a nicety.**

3. **Auto-buff "already up" must test the children.** `GameLoopService.cs:3362-3367`:

   ```csharp
   string key = string.IsNullOrEmpty(def.BuffKey) ? def.Name : def.BuffKey;
   if (p.Buffs.Any(b => b.Key == key)) continue;   // already up
   ```

   Once the parent stops applying a buff under its own key this **never matches**, so the
   autopilot re-queues the buff every cycle forever — draining MP and, on a party buff,
   re-stamping the whole party each time. An offline buffer would run dry in minutes.

   Replace with: *up* = every child present at ≥ its own rank. This is also better behaviour —
   when an overriding potion expires, the check goes false and the buffer restores that one
   child on the next cycle instead of leaving a hole until the parent expires.

   Sweep for other parent-key assumptions while doing this: buff-bar grouping,
   `RuneBuffKeys` reconciliation (`:5161`), the cancel-buff command (`:8105`).

### Display — one icon (⚠ this DOES need a protocol bump)

`ApplyBuff` stamps the **parent's** id as each child's `SourceSkillId`, so the server side of the
grouping is done. But the claim that this needs no protocol change was **wrong**: `BuffDto`
(`Dtos.cs:203`) carries `Name / Description / SecondsLeft / IsDebuff / Key / Stacks / Row / Icon`
and *no* `SourceSkillId` — the client never sees it. Step 5 must add that field and bump
`ProtocolVersion`. Until then an Improved Speed shows as up to four separate squares, one per
child (each with its own effect glyph — see `SkillIcons`).

Bonus: when a potion overrides one child, that child's `SourceSkillId` becomes the potion's, so
it visibly pops out of the group — the bar *shows* which part of the buff the potion replaced.
Group duration = max of the children's remaining.

### Refused consumables must not be consumed

Pre-existing bug, made common by this change. `GameLoopService.cs:2499-2505` applies the buff and
then calls `ConsumeOne(player, item)` unconditionally, so a potion refused on rank is still eaten
and still sets its cooldown. Have `ApplyBuff` report applied/refused; on refusal skip the consume
and tell the player *"A stronger effect is already active."*

## The base speed group — fully specified

Owner-authored (playtest-15 §2). Single buffs, C/U/R = ranks 1/2/3:

| Family | Effect | Mode | r1 (C) | r2 (U) | r3 (R) |
|---|---|---|---|---|---|
| `spd_move` | swift — move speed | flat | 15 | 20 | 33 |
| `spd_cast` | force — cast speed | % | 15 | 23 | 30 |
| `spd_eva`  | agility — evasion  | flat | 1 | 2 | 4 |
| `spd_as`   | haste — attack speed | % | 15 | 23 | 33 |

Consumables, per tier: **potion** 20 min / 1s cooldown / instant cast · **scroll** 1 h /
1s cooldown / 1s cast. Same tiers, same ranks — only duration differs.

The improved buff "Speed" (cleric / warchanter), six levels — every value is exactly one of the
tiers above, so the levels are pure child references:

| Level | swift | force | agility | haste |
|---|---|---|---|---|
| L1 | U (20) | C (15%) | — | — |
| L2 | R (33) | U (23%) | — | — |
| L3 | R (33) | U (23%) | U (2) | — |
| L4 | R (33) | R (30%) | U (2) | C (15%) |
| L5 | R (33) | R (30%) | R (4) | U (23%) |
| L6 | R (33) | R (30%) | R (4) | R (33%) |

L6 == the NPC buffer's Speed (`Skills.Buffer.cs:72`: 33 AS / 33 move / 30% cast / 4 eva) —
confirms the NPC buffer is the max-level set. Current `HolySpeed` (`Skills.Healer.cs:77`) has
only 4 levels and different values; it gets re-authored to the table above.

## Out-list — keeps its own key, keeps stacking

Not every skill granting move speed belongs to `spd_move`. A family is the set of sources that
are *alternatives* to each other. These are deliberately outside it:

- **Dash potion** (the renamed old Swift potion): 15s / 1 min cooldown, C/U/R/E/L/M =
  15/30/45/50/55/60 move, no scroll. Own family `dash`, own 6-rank ladder. **Critical** — if it
  joined `spd_move` it would evict a 1-hour swift scroll and hand it back 15 seconds later.
- **Sprint** (`Skills.Fighter.cs:329`) — +40 move / 15s burst steroid.
- **Warchanter chant** (`Skills.Warchanter.cs:35`) — +45 move, a song.
- **Frenzy** (`holy_frenzy`) — +8 move, a trade-off buff.
- **Healer combat stance** (`Skills.Healer.cs:216`) — +5 move.
- **`defensive_wall`** (−50% move) and **`Skills.Fighter.cs:540`** (+15% move) — percent mode;
  they cannot share a flat family regardless.
- **Wind Walk / Mass Wind Walk** (`Skills.Common.cs:449`, `:462`) — **being deleted**, owner
  2026-07-31. Remove rather than re-home.
- **Spiritshot rune** — flat +40 cast stat, not a percent; stays its own key.

## Harmony — the second layer

Design intent (owner): scrolls and potions can cover the **entire base layer**, so the buffer's
basic buffs alone would make him redundant. **Harmony is what keeps a buffer relevant** — it has
no potion or scroll analogue at all.

- Harmony children use a **separate family prefix** (`harmony_move`, `harmony_cast`, …), so they
  stack additively with the base layer. Same mechanism, one more ladder per family.
- **Duration 10 min** (vs 1 h for the NPC buffer's base set).
- **DECIDED: Harmony buffs become party buffs** (`TargetMode.AlliesInRadius`). Required — the
  autopilot hard-targets *self* for buffs (`:3368`), so a single-target Harmony could never be
  handed out by an auto-farming buffer alt. As a radius buff it fans out at `:6043` regardless of
  target, so a buffer left on auto-farm re-casts Harmony on the party every 10 minutes until MP
  runs out. That is the intended play pattern.
- Harmony's own three buffs (`harmony_protection` / `warrior` / `wizard`) barely collide with each
  other today, so they may stay monolithic at first; split them into children only when a second
  Harmony-tier source exists.

## Auto-hunt changes that ship with this

- **`ChildBuffs`-aware "already up"** — see *Three code changes* #3. Mandatory, not optional.
- **DECIDED: heal threshold 100% = cast on cooldown.** Today `:3370` hard-gates heals at
  `Hp/MaxHp >= 0.70f`, so a buffer only casts the party HoT when *he himself* drops below 70% —
  the party HoT effectively never fires. Make the threshold configurable; at 100% the heal casts
  on cooldown (or on a **custom cooldown** the player sets, reusing the existing
  `AutoSkillEntry.ExtraDelayTicks`). This is also playtest-15 §1.
- Note: the "already up" test reads the *caster's* buff list, so a member who joined late, was out
  of radius, or died and lost buffs waits for the next natural expiry. Acceptable for now.

## Still to author

The remaining improved buffs need their six-level child tables written. The **single-buff ladders**
are mostly derivable from the NPC buffer's max values (`Skills.Buffer.cs`), which are L6:

| Buff | L6 values (= NPC buffer) | Consumables |
|---|---|---|
| Might | +15% P.Atk, +15% P.Def, 9% melee vamp, +4 accuracy | potion+scroll: Attack (pAtk), Defence (pDef) |
| Force | +32% M.Atk, +30% M.Def, +60 interrupt resist | potion+scroll: Magic-Attack, Magic-Defence |
| Focus | +30% crit rate, +35% crit dmg, ×2 magic crit | scroll only: Critical, Critical-Damage, Magic-Critical |
| Body | +35% Max HP, +35% Max MP, +20% HP/MP regen | scroll only: Health, Mana, HP-Regen, MP-Regen |
| Frenzy | −10% HP/MP, +8% AS/cast/P.Atk/M.Atk, +8 move, −8 eva | scroll only: Frenzy (whole buff) |

Owner-given anchors: pAtk/pDef = 8/12/15% (C/U/R) · mDef = 10/20/30% · Max HP six levels =
10/15/20/25/30/35%, with scrolls at Epic 15 / Legendary 25 / Mythic 35 (ranks 2/4/6 — rarity ≠
rank, as above).

Scroll-only families start at **Epic** rarity; families with a potion analogue start at **Common**.

## Build order

1. ✅ `ChildBuffs` on `SkillDef`/`SkillLevel` + the `ApplyBuff` fan-out + parent `SourceSkillId`.
2. ✅ The equal-rank duration rule, and the refused-consumable fix.
3. ✅ The auto-buff "already up" child test + the sweep for parent-key assumptions.
4. ✅ The four `spd_*` families, the new Swift/Alacrity/Agility/Haste potions + scrolls, the Dash
   line, Wind Walk deleted, Improved Speed re-authored to six levels.
5. ⬜ Client: collapse buffs by `SourceSkillId` — **needs `BuffDto.SourceSkillId` + a protocol
   bump** (see *Display* above). Also: a double-click on a collapsed group should drop the whole
   group, which means sending the group id, not one child's `Key`, to `RemoveBuffCmd`.
6. ⬜ Everything else (Might / Force / Focus / Body and Spirit / Frenzy families, Harmony split) is
   data entry after the mechanism is proven on the speed group.

### What steps 1-4 actually touched (0.36.0)

- `SkillDef.ChildBuffs` + `SkillLevel.ChildBuffs` + `ChildBuffsAt(level)`.
- `ApplyBuff` — fans out to children; **returns bool** (landed / refused); gained
  `durationOverride`, `sourceSkillId` and `rowOverride` so a child runs for the parent's time,
  under the parent's icon, in the parent's bar row.
- Equal rank now **keeps the longer remaining time** by refusing the shorter source outright,
  which is what makes the refused-consumable fix work: the potion is not drunk at all.
- `UseConsumable` — reuse timer moved to *after* the buff lands; a refused item is neither eaten
  nor put on cooldown, and says so.
- `BuffAlreadyUp(p, def, level)` — the child-aware test. Wired into **both** the auto-skill loop
  and the **auto-buff-potion** loop, where the same parent-key assumption would have made the
  autopilot drink the entire stack one bottle per cycle. Burst potions (< 60s) are never
  auto-drunk.
- `HandleToggle` — group-aware (no shipped toggle is a group yet; it just won't fail silently).
- **Persistence**: `BuffInstance.SkillId` is new and distinct from `SourceSkillId` (parent).
  Saving the parent would have re-applied every sibling at full duration on login — a free buff
  refresh for anyone who relogged. `BuffSnapshot` now stores both.
- Six levels of Improved Speed exist, but the cleric table still stops at **level 4** (char 35):
  levels 5-6 have no learn slot until the Warchanter discipline tables are re-authored.
- Dash Epic/Legendary/Mythic have **no drop or shop source** yet (vendor stocks Common, drops
  cover C/U/R) — they wait for the §3 drop-group rework.

**Run `tools/SmokeTest` after** — buffs persist, and the family-key rename orphans any buff saved
under an old key (harmless, short-lived, and `game.db` is reset regularly anyway).
