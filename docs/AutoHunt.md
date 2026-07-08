# Auto-Hunt / Idle Farming (docs/AutoHunt.md)

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
  - **Heal** (self) → cast if self HP < 70% (simple Phase-1 rule).
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

## Not in Phase 1 (deferred)
Offline continuation (survive disconnect), roaming/pathing to find mobs, auto-heal of
party members, per-skill target-type overrides, a reduced idle rate.
