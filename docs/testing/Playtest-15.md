# Playtest-15 — 2026-07-31, on the phone, server 0.34.3

Two characters: **Mage 1→25** (~2h: 1-20 in ~1h, 20-25 mostly idle/auto-farm) and **Rogue 1→20** (~1h).
First time the server ran on the phone under the new portable publish.

This file is the owner's report **verbatim**. Triage/build order lives in memory `playtest-15-queue`
and `docs/RoadmapNext.md`.

---

## Checklist results (against `TestChecklist.Unity.md`)

**Untested:** 25 (all) · 30i · 30j · 30l

**PASS:** 26 (all) · 27 (all) · 28a-28d · 29 (all) · 30b, 30c, 30d, 30f, 30g, 30h, 30k · 31a, 31b, 31c, 31d, 31f

**PASS with a finding:**
- **26d** — the quantity/stackable items have no details window in the vendor; tapping goes directly to the numpad.
- **28e** — there is **no delete button in character select** at all, so the admin-delete window can't be reached.
- **30a** — "now seems fine and will be better when we fix the drop logic". **The faucet test passes.**
- **30e** — the drop list still looks clustered. Wanted: the **group is a TITLE** carrying the group name
  and the group %, with its rows **indented below it**.
- **31e** — passive numbers are right but unreadable when they alternate; e.g. the mage's weapon
  proficiency reads "+cast, then −cast, then + again…". **Group them.**

---

## Running the server on the phone for the first time

```
root@localhost:~/Game.Server# dotnet Game.Server.dll
GC: Reserving 274877906944 bytes (256 GiB) for the regions range failed, do you have a virtual memory limit set on this process?
GC heap initialization failed with error 0x8007000E
Failed to create CoreCLR, HRESULT: 0x8007000E
```

Fix he applies by hand every update:
```
root@localhost:~/Game.Server# nano Game.Server.runtimeconfig.json
```
…and sets `"System.GC.Server"` from `true` to `false`.

> **Can we make it so it ships with this false so I don't have to do it each time on my phone after
> a server update?**

---

## Mage 1-25 (1-20 ~1h, 20-25 more idle, ~1h)

1. For 2h I managed to get to lvl 25 have **~1kk gold** (haven't sold potions/scrolls — because of
   current high price I can make lots).
   - Found common wand + uncommon aegis + the mythic Ferrite … Found rare E robe but the others are
     weaker so I'm using the newbie set.
   - **Should decrease the HP potion drop rate** — because of infinite amount of potions I cannot die.
     But I take good dmg and there were times I had to use the vampiric just to keep me alive
     (which drains MP like crazy — that's ok).
   - good levelling pace
   - good amount of gold
   - **nuker has Wind Walk**, a self buff that stacks with other buffs and should not be there.

## Rogue 1-20 ~1h

- good levelling pace
- good amount of gold
- **Battle Fury must go** — it's not in the original CSV.

---

## Bugs

1. Finishing the quests for class change and changing it from the class master:
   - my class doesn't update
   - need to relog
   - after relog there is a delay for the skills window to refresh to access my unlearned skills list
2. The **set bonus is shown but not the equipment required/filled** for the set — that's missing now.

## Need change

1. Training (Wooden shield) should have **35 def**.
2. Ferrite Aegis (F shield) should have **90 pDef as Mythic**.
3. Despite all please update **all training weapons to have the 5 mAtk** as stats shown + **wand to have
   pAtk 6 and mAtk 7** and **remove the +6 maxMP**.
4. In auto-farm there should be a **retaliate**:
   - a mob hitting you is higher priority than nearest
   - I'm getting ganked by orc archers and still kill the nearest
5. Need **NextTarget** (targeting closest/retaliate 5 and cycling through them) — **DEFERRED**.
6. There is **auto move** in auto-farm:
   - when auto-farm is on, whatever class you are, you don't move towards the target when
     `BasicAttackAction` is not active in the hotbar
   - now my mage goes for melee and just sits over the mob waiting for the next cast — that goes for all
7. Make **basic attack on tap/click be AFTER the target window**:
   - I click once, it only shows the target, not immediately going towards it
   - after the target is shown, if I click again (on the same target) it starts to move
   - if the target window is open and I click another target it only changes the target window, not
     going for a basic attack
   - it's very annoying on mages/archers — basic attack is on the second click on the same target, or
     the basic-attack button
8. **Consumables need a count on the hotbar** — 1, 2, 3 … 98, 99, 99+. Over 99 it shows `99+`.
9. **Scrolls of escape and return cannot be sold** even when they are tradable.
10. **Buff potions should lower their selling price** the same ÷25 as the others: 1500/25 = 60, now it's
    450. I didn't sell any potions in the shop because having 100 is too OP.
11. **Add timers for auto-farm and offline farming**:
    - same logic as the buff timers — `24h00m01s` == `1d`
    - when I enable auto-farm, show the time on the button
    - or just a single line in chat with the remaining on both, on each auto-farm on/off change
12. The **"show farming range" toggle should only show when it is enabled AND auto-farm is on**. Now I
    have a rogue circle in the farm zone while I'm selling in the shop with auto-farm off.
13. **Cannot kill party members even with PvP on.**
14. **Jewels should be like helmet/gloves — designated slots.**
    - now I equip jewels in a list, and I can try to equip a 3rd ring and it tells me that I can't
    - I want **two ring slots, two earring slots, one necklace slot**
    - when I equip a glove it replaces the one I'm wearing — I want jewels to do the same (pendant is a
      single slot, same logic)
    - equipping a ring/earring **switches the weaker one first**:
      - if both equipped are the same (2× common, 2× uncommon, 2× rare) → switch **slot 1**
      - if one equipped is weaker than the other → switch the **weaker** one
      - ordering: `no slot < common < uncommon < … < mythic`
      - worked example: no rings → 1st common goes to slot 1 (both slots same) → 2nd common goes to
        slot 2 (free/weaker) → a rare goes to slot 1 (both same weight) → an uncommon goes to slot 2
        (rare > common) → another uncommon replaces slot 2 again (rare > uncommon)

## Bigger changes

### 1. Auto-farm: cyclic and first-available skill order

- **cyclic**
  - when auto-farm is on, skills are executed 1-2-3 (skipping buffs/debuffs/heals)
  - even when skill 1 is ready, it does not go back to 1 until the last skill has been used
  - use 1 → use 2 if available (not on cooldown, not a heal/buff/debuff) → 3 → 4 → … → 1 → repeat
- **skill order (first available)**
  - skills are executed 1-2-1-3-1-4-1-2 (skipping buffs/debuffs/heals)
  - when skill 1 is available it is next in line
  - use 1 → 2 if available → 1 if available → 2 if available → 3 if available → 1 → 2 → 1 → 2 → 3 → …
- **heals**
  - there should be a **healing threshold %** below which the auto-healing skills become active
  - depending on cyclic/cooldown they are executed with the same logic, only when HP is below threshold
  - when HP drops below the %, it waits for the current cast to finish, then the healing chain starts;
    when HP is back over the %, normal skill execution resumes
- **buffs/debuffs**
  - if any buffs or debuffs are on the bar, the chain is always active (after the healing one),
    dependent on cyclic/cooldown
  - a **buff** (castable on self) fires if the same buff effect on the character is
    **not active / below 60s / a lesser effect**
  - a **debuff** (castable on the enemy) fires if the same debuff effect on the enemy is
    **not active / a lesser effect**
- **priority order: Heals → Buffs/Debuffs → Attack skills**
- there should be a checkbox/toggle for **AssistPartyLeader**
  - when on, you only assist — if the party leader has no target you wait; don't choose on your own
- **healers and buffers should be actively played** to keep the party alive and buffed
  - you cannot have an alt-bot buffer and an alt-bot healer on auto-farm always auto-buffing/healing you
  - your main damage dealer that auto-farms with 2 alt chars needs to "alt+tab" to buff/heal himself
  - the only auto-help: if the healer sets his threshold to 100% he always heals on cooldown → that
    activates the party heals on cooldown/custom, and he has party buffs

### 2. Buff potions and buff scrolls

- Buff potions now **stack with the current buffs**, making characters stronger than intended.
- I want buff potions/scrolls to be a **split buff**:
  - example, the cleric's speed buff:
    `L1 – 20 speed, 15% cast; L2 – 33 speed, 23% cast; L3 – 33 speed, 23% cast, 2 eva;`
    `L4 (not yet written for warchanter) – 33 speed, 30% cast, 2 eva, 15% AS;`
    `L5 – 33 speed, 30% cast, 4 eva, 23% AS; L6 – 33 speed, 30% cast, 4 eva, 33% AS`
  - the cleric's/warchanter's buff is an **improved buff that groups several buffs**
  - the potion/scroll buffs are **single buffs**:
    - **swift** — C 15 ms, U 20 ms, R 33 ms · Potion: 20 min duration, 1s cooldown, instant cast ·
      Scroll: 1h duration, 1s cooldown, 1s cast
    - **force** — C 15% cast, U 23% cast, R 30% cast · same potion/scroll terms
    - **agility** — C 1 eva, U 2 eva, R 4 eva · same potion/scroll terms
    - **haste** — C 15% AS, U 23% AS, R 33% AS · same potion/scroll terms
  - **potions AND scrolls** available for: Attack (pAtk), Defence (pDef), Magic-Attack (mAtk),
    Magic-Defence (mDef)
  - **scroll only**: Health (maxHP), Mana (maxMP), Health-Regeneration (hpRegen),
    Mana-Regeneration (mpRegen), Critical (pCritRate), Critical-Damage (pCritDmg),
    Magic-Critical (mCritRate), Frenzy (the full frenzy buff — −hp/mp +pAtk/mAtk etc.)
  - potions are **less duration, lower quality** — basic buffs can be covered by potions
  - scrolls are **longer duration, higher quality** — basic buffs start from **common** (if they have a
    potion analogue); scroll-only buffs start from **epic** (where they have no potion analogue)
    - e.g. the Health scroll (the body buff at L6 gives 35% max HP + other stuff — estimate, not exact,
      but 6 levels: 10, 15, 20, 25, 30, 35%): the scroll at **Mythic** gives 35%, **Legendary** 25,
      **Epic** 15
  - you have the max levels of buffs and I gave the list of what scrolls/potions can be — so estimate
    the buffs you don't have levels for
  - the pAtk/pDef is 8, 12, 15%; the mDef is 10, 20, 30% — something like that
- the current **Swift potion is renamed to Dash potion**:
  - **Dash potion** — C 15 ms, U 30 ms, R 45 ms, E 50 ms, L 55 ms, M 60 ms ·
    Potion: **15s duration, 1 min cooldown, instant cast** · **no scroll of that type**

### 3. The drop group idea needs to change

- I still want groups, but **inside the group the next roll should not be for a rarity, but directly
  for the drop**.
  - you roll for the group *armor*
    - then inside there is a standard drop list that you roll for
    - all the common armors are at 5% and uncommon at 2% — is there a way to have 10 items in a list all
      at the same % and, when you roll 0.048 (more than 2%, less than 5%), select one of the Commons,
      because all are within that range?
- In a way I want to **simplify** it.
- If I have a group at 100% and all items inside are at 100%, how do I select only one of them at random?
  - if the roll returns several items, roll again?
- That way I will be able to make a specific item drop less despite its rarity — like the Scroll of
  Resurrect — and will have better control over it per mob. (I start to understand why each entity is fixed.)
- I want the **potions/scrolls group to be more controlled**.
- Same for the **always** group — I can make the common health potion rate decrease, but if we make
  another health potion that is instant and it's common, I will not want it dropped the same as a
  normal HoT.

## Questions

- **What happened to the free teleport for levels < 40?**
