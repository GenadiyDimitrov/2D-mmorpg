# Instances & Dungeons — design (owner spec, 2026-07-14)

**NOT BUILT.** This is the owner's specification, recorded verbatim in intent, plus the architectural
decisions that fall out of it. Read this before starting the work.

Two separate features. They are very different sizes and should not be conflated.

---

## DUNGEONS — small. Mostly data.

> "normal field zones with harder mobs, more exp — better with a party. Mobs respawn like
> normal/elites depending on the mob. Can also have bosses. It's like a normal editable zone, just not
> on the main map (or if the map has layers — under/over ground?)."

A dungeon **is** a `SpawnZone`. We already have everything it needs: zones with level bands, mob
rosters, elites, bosses, drop tables, respawn timers, and teleport-for-fee.

The world is **48000 × 48000** and the seven-town ring only occupies the middle, so a dungeon is simply
a zone authored **outside the ring**, with a teleporter entrance. No engine work — author the zone, the
roster and the entrance.

"Map layers (under/over ground)" is a *client* concern, not a server one: the server is 2D and doesn't
care. If layers are wanted visually, that's a rendering decision for the future client, and it does not
block the server-side dungeon.

**Increment:** one dungeon zone + entrance teleport + a boss. Then tune.

---

## INSTANCES — a real system. Plan it, don't wing it.

### The rules (owner)

**Attempts & reset**
- **One instance per day, per player.** ⚠ Read as a *global* daily attempt, not per-instance — see the
  level-29→30 rule below, which only makes sense if the attempt is global. **CONFIRM BEFORE BUILDING.**
- The attempt **resets at 00:00 server time**, every day of the week. In DEBUG: every 10 minutes.
- If an instance is only open on (say) Thursday and Friday, the player's attempt **still resets daily** —
  the *instance* is what's closed on other days, not the attempt.
- The attempt is **consumed on START**, not on completion. Finished or not, that was your go.
- **Leaving the party** — manually, kicked, or by outlasting the 3-minute grace — **loses the attempt**.
- **One active instance per player** at a time.
- **Level-range trap:** finish today's 20-29 instance at level 29, then ding 30 → you **cannot** enter the
  30-39 instance today. You already spent the day's attempt. (This is the rule that proves the attempt is
  global, not per-instance.)

**Opening hours**
- Every instance has an **open window**: a time range, default `00:00:00 – 23:59:59`, plus a day-of-week
  mask. Needed now even if unused, so event instances can reuse it later.

**Entry**
- **One NPC serves all instances of a category.** Separate NPCs per *category* (normal/daily, event,
  world boss, …) — so `NpcRole` grows a role per category, not per instance.
- Only the **party leader** can start one, and only when the party has **≥ 4 players** and **every member
  is eligible** (in the level range, and has not used today's attempt).

**Inside**
- Level bands per instance: **20-29, 30-39, 40-49, 50-59, 60-69, 70-75, 76-85.**
- Mobs are in the band, and are **all ELITES**. Killed mobs **do not respawn**.
- Laid out as **rooms**. e.g. a 20-29 instance = 5 rooms (20/21, 22/23, 24/25, 26/27, 28/29) + a **boss
  room** with a level-26 boss. *(Note: the boss is MID-band, not top — deliberate.)*
- **Trash gives NOTHING** — no exp, no drops, no gold. **Only the boss pays**, with a custom, better drop
  table and far more exp than a field boss of the same level.
- **1 hour limit.** Time out and the attempt is gone until the instance reopens.
- **Death:** respawn beside the entrance NPC, and you may re-enter (the attempt is already spent).
- **No subclass swapping while an instance is active.**

---

### Architecture — the decision everything else hangs off

**The problem:** `World` has ONE flat `Entities` dictionary and ONE spatial grid. There is no notion of
"this party's private copy of a room."

**The cheap way in — COORDINATE SLABS.** Visibility and interest management are already radius-based
(`GameConstants.ViewRange` = 3000) over a spatial cell grid, so **two parties 20,000 units apart already
cannot see each other**. So an instance can be an **off-map coordinate slab allocated per running
instance**: spawn that party's copy of the rooms there, and teleport them in.

This reuses the ENTIRE existing engine for free — spawning, the grid, combat, threat, loot, death,
respawn — and needs no change to `World`, snapshots or broadcast.

The alternative (a real per-instance `Entities` dict + grid) is conceptually cleaner but touches World,
the grid, snapshots, broadcast, teleport and party all at once. **Not worth it.** Start with slabs.

**Bounds:** movement is clamped to the world rectangle today. Instance slabs need either their own clamp
or a reserved band inside the 48000² world. Reserve a band; it's simpler.

### ⚠ `GameClock` IS NOT SERVER TIME
`GameClock` is *in-game* time running at **×6** real time. **Daily resets must use real wall-clock time
(UTC or a configured server timezone), not `GameClock`.** Using GameClock here would reset the daily
attempt every 4 real hours. This is an easy and expensive mistake to make.

### What has to be built
1. **`InstanceDef`** (shared, data): id, category, level band, open window + day mask, room layout, mob
   roster per room, boss, boss drop table, time limit, min party size.
2. **Per-character attempt state, PERSISTED**: the last date an attempt was consumed. Reset is then a
   *comparison*, not a scheduled job — a character whose stored date < today has an attempt. (This
   survives restarts for free and needs no timer.)
3. **`InstanceRuntime`** (server): the slab, the party, the spawned mobs, the deadline, the members.
4. **Lifecycle**: start (validate leader/party≥4/all eligible/open now/level band) → allocate slab →
   spawn rooms → teleport party → consume attempts → tick the deadline → on boss death or timeout or
   party collapse: reward, teleport out, free the slab, despawn.
5. **Gates**: no subclass swap while in an instance (gate `SwitchSubclassCmd` — the rule belongs on the
   COMMAND, not on the mechanism); one active instance per player.
6. **NPC roles** per category.
7. **Exp/drop suppression** for instance trash; boss reward table.

### Suggested increments (each testable on its own)
1. **Dungeons** (data only) — get value immediately, zero risk.
2. **Instance skeleton**: one instance, one room, slab allocation, party entry, teleport in/out, no
   rewards. Proves the isolation approach.
3. **Attempts + reset + open windows** (persisted; real server time).
4. **Rooms, elites, no-respawn, boss + reward table, trash pays nothing.**
5. **The 1h timer, death→entrance respawn, party-collapse handling, subclass lock.**

### Open questions for the owner
- **Is the daily attempt GLOBAL (one instance of any kind per day) or PER-INSTANCE?** The level-29→30
  rule says global. Confirm — it changes the persisted data model.
- If the party drops below 4 mid-run, does the instance end, or continue for whoever is left?
- Is the level-range checked only at entry, or continuously (does dinging 30 inside a 20-29 instance
  eject you)?
- Does the 1-hour timer pause while a player is disconnected (we have a 180s link-dead grace)?
- World bosses / event instances are mentioned as future categories — same attempt pool, or their own?
