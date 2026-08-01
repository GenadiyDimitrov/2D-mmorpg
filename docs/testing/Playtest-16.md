# Playtest-16 — 2026-08-01, on the phone, server 0.42.0

The owner's own test sheet, **verbatim**, and his answers against it. He replies **"True"** when an
item passes.

## ⚠ His numbering is the VERSION, not the checklist section

He keeps his copy numbered **36x / 37x / 39x / 40x / 41x = 0.36.0 / 0.37.0 / 0.39.0 / 0.40.0 / 0.41.x**.
`TestChecklist.Unity.md` numbers the same items by **section** (§32 / §33 / §34). So **"37c" is not a
§37** — translate before searching. The mapping is in the results table below. (0.38.0/0.38.1 — jewel
slots and `/droprate item` — are not on his sheet at all.)

## Verdict

**17 items pass. 4 pass and still fail the reader. 2 new bugs, both client-side.** Nothing crashed,
nothing corrupted, no missing system. The one real simulation bug of the session wasn't on this sheet —
mob regen running on the *player's* CON curve — and was found by playing, not by reading.

All 4 follow-ups were built the same day (0.42.2, checklist §35a-d) and both bugs fixed in 0.42.1.

---

## The sheet, verbatim

```
36a.[ ] A potion overrides ONE part — a Rare Alacrity potion takes the cast-speed
        part of Improved Speed and leaves movement/atk-speed alone.
36b.[ ] Equal rank keeps the LONGER time — a 20-min potion must NOT eat your 1h scroll
        of the same tier.
36c.[ ] A refused consumable stays in the bag — no cooldown, and it says a stronger
        blessing is up.
36d.[ ] Auto-farm doesn't re-buff a live buff and doesn't drink a stack one bottle at a
        time; Dash is never auto-drunk.
36e.[ ] Relog is not a free refresh — buffs come back with LESS time, siblings included.
36f.[ ] Wind Walk is gone. Dash has six rarities on its own family and never evicts Swift.
37a.[ ] An improved buff is ONE square (was four). Timer = shortest part, tap lists the
        parts, press-and-hold removes the whole group. A potion square still reads "Swift".
37b.[ ] NPC buffer = basic only, 1h each, no group and no Harmony; price unchanged.
37c.[ ] Set bonus lists its pieces and which slots you have filled.
37d.[ ] A stackable at a vendor opens details FIRST, not the numpad.
37e.[ ] Character select has a Delete button (this also unblocks 28e).
37f.[ ] Drop list is a tree — group title + group %, items indented under it.
37g.[ ] Mastery/passive numbers group by STAT, not "+cast, −cast, +cast".
37h.[ ] Hotbar consumables count: 1, 2 … 99, then 99+.
37i.[ ] Auto-farm / offline farm show remaining time (24h00m01s format).
37j.[ ] The range ring shows only when the toggle AND auto-farm are both on.
39a.[ ] Cyclic OFF = first-available: 1-2-1-3-1-2. Cyclic ON = 1-2-3-1-2-3 (it may wrap
        early only if everything later is on cooldown).
39b.[ ] Priority: a needed heal beats a buff, a buff beats an attack.
39c.[ ] Heal threshold 50 = nothing above 50%. Set 100 on a healer = heals on cooldown,
        onto the most injured party member. Heal row OFF = never heals.
39d.[ ] A buff is renewed under 60s left, not only when expired, and never over a
        stronger version of the family. A rank-2 debuff replaces rank-1 on the mob.
39e.[ ] Assist party leader ON: you hit only his target, follow him onto a new one, and
        stand with an EMPTY target frame when he has none.
39f.[ ] All of it survives a relog (no db reset needed).
39g.[ ] Under 40 every gatekeeper row reads "Free" and costs 0 gold. At 40 the distance
        fee is back, minimum 50 — quoted price must equal the gold you lose.
40a.[ ] THE ONE THAT MATTERS: cleric's Might and Bulwark up (+8/+8) → a Lesser Might
        Potion is REFUSED and not consumed, P.Atk unmoved. A Greater one (+15%) replaces
        The P.Atk part while the +8% P.Def stays.
40b.[ ] Class buffs cast the same numbers as before — one exception: Might no longer
        Raises M.Atk. Check a mage's M.Atk falls, and that Force restores it.
40c.[ ] Apothecary stocks Might · Bulwark · Force · Ward (Lesser); scrolls drop. 20 min
        Potion / 1 h scroll; the scroll takes a second to read, the potion is instant.
40d.[ ] NPC buffer = 19 single blessings + Full buff, the list scrolls, each cancellable
        Alone. Full buff should cost about what nine buttons did — say so if it doubled.
40e.[ ] Scroll-only families (Body · Soul · Vigor · Serenity · Focus · Ferocity ·
        Insight · Frenzy) have NO potion at any price; cheapest scroll is Epic, drops
        60+/76+. Mythic buff scrolls have no source yet — not a bug.
40f.[ ] Nothing orphaned: no stale square that never expires, no dead skill on the bar.
41a.[ ] Aim: accuracy +1/+2/+4, potion AND scroll at Common/Uncommon/Rare, vendor-stocked
        at Common, sits next to Agility. Cleric's Aim and an Aim potion do not stack.
41b.[ ] A cleric learns 14 SEPARATE buffs at 30-50 MP (base mage gets Might + Bulwark at
        7). Buffing the whole list must land the same numbers as before — more casts,
        more MP, same result.
41c.[ ] Warchanter kit: singles top out 40-64, Harmony at 60/62/64 (200 MP, 20 min,
        nothing else grants them), the five improved groups at 66/68/70/72/74 (150-200 MP).
        Each family is at its MAX rung before its group appears.
41d.[ ] Improved buffs and Harmony are PARTY buffs — cast one in a party and every member
        within 800 gets it, not just the target.
41e.[ ] Learning an improved group DELETES its singles from the bar and the learn list
        (buff bar unaffected).
41f.[ ] Admin buff = 27: five groups + three Harmony + 19 singles. The bar shows the
        groups COLLAPSED (Might and Bulwark · Force and Ward · Focus and Ferocity ·
        Body and Soul · Swift and Sure), Harmony as its own three squares.
```

---

## Results

| His | Checklist | Item | Result |
|---|---|---|---|
| 36f | 32i | Wind Walk gone; Dash's own family, never evicts Swift | ✅ |
| 37b | 32w | NPC buffer basic-only, 1 h, no group/Harmony | ✅ |
| 37c | 32c | set bonus lists its pieces + filled slots | ✅ **but the EFFECT isn't shown** — *"what does that set do?"* |
| 37d | 32d | vendor stackable opens details first | ✅ **but it's a double confirmation** |
| 37e | 32e | character-select delete button | ✅ (also unblocks 28e) |
| 37f | 32f | drop list is a tree | ✅ **but the item rows want their own %** |
| 37g | 32g | passive numbers grouped | ✅ **but grouped by the WRONG axis** |
| 37h | 32n | hotbar consumable count 1…99, 99+ | ✅ |
| 37i | 32q | auto/offline farm remaining time | ✅ |
| 37j | 32r | range ring needs the toggle AND auto-farm | ✅ |
| 39g | 32u | free travel under 40, fee back at 40 | ✅ |
| 40c-40f | 33c/33d/33f/33g | potions+scrolls stocked · buffer 19+Full · scroll-only families · nothing orphaned | ✅ (incl. *"mythic scrolls have no source — not a bug"*) |
| 41b, 41c, 41e | 33i, 33j, 33l | cleric's 14 singles · Warchanter kit · groups delete their singles | ✅ |

**Not reported on:** 36a-36e · 37a · 39a-39f · 40a · 40b · 41a · 41d · 41f.

## ⚠ Four rows on this sheet now test a rule that no longer exists

**0.42.0 reversed the "a group is a bag of independent children" model** (his call: *"in l2 an improved
buff overrides its single parts… a single buff cannot override it"*). A group is ONE buff again, rank
`100 + level`, which evicts its singles and cannot be overridden by a potion or a scroll. So:

- **36a is dead.** A potion overriding one part of an improved buff is exactly what 0.42.0 removed.
- **40a is dead.** Its second half — a Greater potion replacing the P.Atk part while the +8% P.Def
  stays — is the old model. The live rule: the group refuses both.
- **41f is dead.** The admin buff is **9 rows** now (5 groups + 3 Harmony + Frenzy, the only family no
  group covers), not 27, and there are no separate squares to collapse.
- **37a is half-dead.** A group is still ONE square, but because it *is* one buff — not because four
  squares are merged. Its timer is the buff's own, not the shortest child's.

**36b-36e still hold** (equal rank keeps the longer time, a refused consumable stays in the bag, the
autopilot doesn't re-buff or drink a stack, relog is not a free refresh). The live sheet for all of
this is checklist **§34**.

## The 4 follow-ups — all built in 0.42.2, checklist §35a-d

- **32c → 35a** — a set says which pieces it needs, never **what it grants**. He literally can't tell
  what a set does.
- **32d → 35b** — details-then-numpad is **two confirmations**. Wanted: the detail **on the row**, for
  every vendor item; for a consumable the **numpad IS the confirmation**.
- **32f → 35c** — the tree shows group title + group %; the indented item rows need **their own %**.
- **32g → 35d** — grouping by STAT was the wrong axis. Verbatim: *"sword/blunt +10 pAtk +10 mAtk +10
  cast, dagger/bow -100 cast — more compact, logically written, not throw everything there."* Group by
  **weapon group**, list its stats after it.

## 2 new bugs — both fixed in 0.42.1

1. **Press-and-hold fired on RELEASE.** *"I start to hold and after a 1-2s to register it as hold, not
   to wait for a release."* `PressAndHold` decided the gesture in `OnPointerUp`. It fires in `Update` at
   the threshold now, release is a no-op, plus a 40 px travel cancel — a scrolling list carries the row
   under the finger, so `OnPointerExit` never fires and a slow scroll would otherwise arm a hold.
   (Threshold 1.0 s here, cut to **0.65 s** in 0.42.3 after he read 1.0 s as *"like 2s"*.)
2. **A box's contents never appeared in the bag.** The server pushed fine; `RefreshBag`'s change stamp
   hashed only length + equipped + quantity + enchant, and opening a box is a **swap** — one out, one
   in — so the stamp was identical and the rows were never rebuilt. Fixed by folding `InstanceId` in.
   **The vendor stamp had the same blind spot** and was fixed with it.
