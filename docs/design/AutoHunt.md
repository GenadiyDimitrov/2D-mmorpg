# Auto-Hunt / Idle Farming (docs/design/AutoHunt.md)

Owner request (2026-07-08). A server-driven automation layer so a character can farm
hands-off. **Phase 1 = online idle** (must stay logged in). **Phase 2 = true offline**
(entity kept in the world after disconnect) — deferred.

Loot/XP behave EXACTLY like manual play (owner decision), incl. party loot modes. A
reduced idle/offline rate is intentionally NOT added (a hook can come later).

## Where it runs
The automation "brain" runs **server-side** in the single-writer loop — it reuses the
same cast/attack pipeline manual actions use (`QueuedSkillId`/`QueuedTargetId`,
`UpdateAutoAttack`, `UsePotion`). The client is only a **config editor + HUD**. This is
the exact seam Phase 2 (offline) needs.

## Config (per character, persisted as `AutoHuntJson`)
- `AutoHuntEnabled` — the hunt loop (acquire target / move / auto-skills). Toggle.
- `HpPotionPct`, `MpPotionPct` — auto-drink the best matching potion below this % of
  max. **Always active** (not gated on AutoHuntEnabled) — a general QoL. (No MP potion
  items exist yet → MP side is reserved plumbing.)
- `AutoBuffPotions` + `BuffPotionIds[]` — keep the listed buff potions up (treated like a
  buff: re-used only when their buff key is missing). Always active.
- `Skills[]` — ordered `AutoSkillDto(SkillId, Enabled, ExtraDelayTicks)`. Priority = list
  order. The USE CONDITION is inferred from the skill (owner rule), not set by hand:
  - **Attack** (deals PhysicalDamage/MagicDamage) → cast at the target on cooldown.
  - **Buff** (`SkillCategory.Buff` / `AnyBuff`, self) → cast only if its buff key is
    MISSING on self.
  - **Debuff** (ContestCc / DebuffSchool, no damage) → cast only if the TARGET lacks it.
  - **Heal** (self) → cast if self HP < 70% (Phase-1 rule; superseded by the configurable
    `HealThresholdPct` — see *Skill chains* below).
  - `ExtraDelayTicks ≥ 0` — an ADDITIONAL post-cast delay on top of the skill's own
    reuse (so the effective auto-reuse is never below the skill default; you can only slow
    it down). Enforced via `Entity.AutoReadyTick[skillId]`.

## Tick logic (`AutoPilot(player)`, called before `UpdateAction`)
1. **Auto-potions** (always): HP/MP thresholds + buff-potion top-up (reuses `UsePotion`).
2. If `AutoHuntEnabled` and not mid-cast/queue:
   - Validate current target (mob, alive, in scan range, not in a safe zone) or acquire
     the nearest such mob within `AutoHuntScanRange`.
   - Engage it (`CombatTargetId`+`Engaged`) so `UpdateAutoAttack` chases + basic-attacks.
   - `TryAutoSkill`: first eligible entry (known, enabled, off base-cd AND past its extra
     delay, MP affordable, condition met) → queue it (`QueuedSkillId`/`QueuedTargetId`);
     `UpdateQueuedSkill` then chases to the skill's own range and casts. If none is ready,
     the basic auto-attack carries the fight.
   - No mob in range → idle (Phase 1 parks in place; roaming is a future add).

## MP/s HUD (`AutoHuntStatus`)
Server computes, per enabled auto-skill, `finalMp / effectiveReuseSeconds` where
`finalMp = (InitialMp+FinishMp)·MpCostFactor` (already reflects MP-cost-reduction buffs)
and `effectiveReuse = reducedCooldown + extraDelay` (reflects cooldown-reduction). Sums to
a total **MP/s** and lists each skill's reuse. Pushed on config change + each regen tick
(so it tracks buffs going up/down). The client shows the total + per-skill breakdown.

## Transport
- `SetAutoHuntConfigCmd(conn, AutoHuntConfigDto)` → store on entity + persist + recompute
  status. `ToggleAutoHuntCmd(conn, bool)` → flip `AutoHuntEnabled`.
- Server → client: `AutoHuntStatus(bool Enabled, float MpPerSec, AutoSkillReuse[] Skills)`.
- Hub: `SetAutoHuntConfig` / `ToggleAutoHunt`; NetworkChannel mirrors + `AutoHuntReceived`.

## Phase 2 — offline farming (BUILT 2026-07-08)
Offline is just the online `AutoPilot` running **without a connection** (SendTo already no-ops when
an entity has no connection, so every UI push is skipped automatically) — no new brain.

- **Disconnect** (`HandleLeave`): if auto-hunt is on, alive, unlocked and out of a safe zone, the
  character is KEPT in the world (`Entity.IsOfflineFarming = true`) instead of removed; only the
  connection maps are dropped. It stays in the grid, so `BroadcastSnapshotsAsync`/`BroadcastCombat`
  still show it to nearby players and mobs still aggro it (attackable like a normal player; PvP
  retaliation / "no counter-attack vs players" is a future hook — PvP isn't built). It leaves its
  party (`RemoveFromParty`).
- **Reconnect** (`HandleEnterWorld`): re-attaches to the live offline entity (keeps offline gains)
  instead of loading a fresh copy; refills the budgets and clears the lock.
- **Runtime caps** (`TickAutoHuntBudget`, per tick while enabled): **online idle 8h**
  (`AutoIdleCapTicks`), **offline 2h** (`AutoOfflineCapTicks`). Hitting the idle cap →
  `StopAutoHunt(locked)` (can't re-enable until re-log). Hitting the offline cap → end the session.
  Caps are constants now; **purchasable extensions (12h / 4h)** are the obvious hook (swap the
  constant read for a per-character premium value when a shop item exists).
- **Death** stops auto-hunt: an offline farmer's session ends (deferred logout); an online idle
  hunter just stops (re-enable after respawn).
- **Ending a session** (`EndOfflineSession`, deferred out of the entity loop via `_endOfflineQueue`
  so we never mutate the entity dict mid-iteration): turn auto off (so it doesn't auto-re-arm next
  login), remove + save the character (a normal logout).
- **Lock** blocks `ToggleAutoHunt`/`SetAutoHuntConfig` from enabling; cleared on the next login.

## Party integration (2026-07-08)
- **No inviting AFK players:** `HandlePartyInvite` rejects a target that is auto-hunting
  (`AutoHuntEnabled`) or offline-farming — they won't answer, so no stuck invite.
- **Invite timeout (~30s):** `PendingPartyInviteExpiry` + `SweepPartyInvites` (regen tick) drops
  unanswered invites and tells the inviter; the client prompt auto-dismisses on the same timer.
- **Offline members stay in the party** with a status flag so the roster shows an AFK/OFFLINE tag
  (the party can kick if it's not a network blip). `PartyMemberDto.Status`
  (`PartyMemberStatus` Online/Auto/Offline). If the leader goes offline, `ReassignLeaderIfNeeded`
  hands off to an online member. Ending an offline session (`EndOfflineSession`) removes the member
  from the party; reconnecting keeps them in it.

## Roaming + target filters (BUILT 2026-07-10)
Config (defaults until a settings window): `FarmRange` (200–2000, default 1000), `StaticSpot`
(false = roam / true = fixed circle at the start point), `AttackNormal/Elite/Boss`. Roam = the scan
circle follows the character and it wanders when idle; static = only engage mobs inside the fixed
circle and walk back to the centre when empty (with a soft chase margin). **Basic attack** is an opt-in
pseudo-skill (`AutoHuntIds.BasicAttack`) in the skill list: on = melee when no skill is ready (fighters),
off = skills-only (mages). Debug `/testcaps` shrinks the caps/grace to seconds for testing.

## Skill CHAINS (BUILT 0.39.0 — playtest-15 big design #1)
The single top-to-bottom priority scan became **three groups scanned in priority order**, and the
condition rules got the sharper tests the owner asked for.

- **Priority: heals → buffs/debuffs → attacks.** Only the first group with something castable acts on
  a given tick (`TryAutoSkill` → `TryAutoChain` per group). Inside a group the order is still the
  config/bar order.
- **`CyclicOrder`** (per character): `false` = *first available* — the scan restarts at the top every
  time, so a short-reuse skill 1 dominates (1-2-1-3-1-4). `true` = *cyclic* — the scan starts from the
  entry after the last one cast in that group and only wraps once the rest have been offered their
  turn (1-2-3-4-1). Each group carries its own cursor (`Entity.AutoChainCursor`), cleared whenever the
  config changes. **Cyclic wraps rather than waits**: a strict "never return to 1 until the last one
  has fired" would leave the character standing idle while a long-reuse skill recharges.
- **`HealThresholdPct`** replaces the hard-coded 70%. `0` = never auto-heal; `100` = heal on cooldown,
  which is the owner's one sanctioned bit of auto-support (a played healer parks there). The heal
  chain also picks a TARGET: the most injured party member under the threshold and inside the skill's
  own range, else yourself — so a party heal is not wasted on a full-HP caster.
- **Buffs are renewed, not just re-applied**: missing, a weaker rank, or **under 60s remaining**
  (`AutoBuffRefreshTicks`, capped at half the buff's own duration so a short buff isn't spammed). A
  *strictly stronger* buff of the same family counts as covered — recasting under it would only be
  refused by `ApplyBuff` and burn the MP. Group (improved) buffs test every child, as before.
- **Debuffs** fire when missing **or weaker** on the enemy; the old test was "any buff with this key",
  which let a rank-1 debuff block its own rank-3 upgrade for a whole duration.
- **`AssistPartyLeader`** — in a party you don't lead, the only target you may take is the leader's,
  and with no leader target you stand still (no acquisition, no retaliation, no roaming). The leader's
  pick is still filtered by your own rank toggles / PvP rules / farm radius + chase margin.

## Still deferred
Full common-skills bar (sit/walk/dance placeable), per-skill target-type overrides, a reduced
idle/offline loot rate, drag-reorder skill priority, purchasable cap extensions, and a client "offline
status / gains" summary.
