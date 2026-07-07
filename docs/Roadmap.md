# Roadmap — L2Clone (branch Gena)

Development TODO for game systems / in-game functions, bucketed by time horizon.
This is the "what to build" list (the "what to verify" list is `docs/TestChecklist.md`).
Claude keeps this updated as work moves between buckets.

Legend: `[ ]` open · `[~]` partially done · `[>]` blocked/waiting · `[x]` done (kept briefly for context).

---

## NOW (active / immediate)

- [x] **Mob base-stat curve** — owner sent a L1-85 mob CSV (`docs/mobs/mob_base_stats.csv`).
  Wired as `MobBaseStats` (per-level HP/MP/P.Def/M.Def/P.Atk/M.Atk curve, interpolated);
  `Entity.RecomputeDerived` mob branch now reads it (`MobMaxHp/MobMaxMp/MobDefence`/the
  `MobMagicDefence` override are retired for mobs). Structure = `curve(level) × conMod × passives`
  (conMod hook = ×1 for now). Outliers (Rift Portling) = curve × HP/P.Def MobMod passive.
- [x] **Mob roster + zone/drops rollout** — the old placeholder mobs replaced by the 80-mob
  renamed roster (Level + `MobCategory`); the ~15 field zones + boss/elite + 3rd-class & class-
  change quest targets rewired by band; drop tables ported by level via `MobCatalog.StandardDrops`.
- [x] **Weapon-type resistance (P.Def route)** — `StatCalculator.WeaponDefenceCoef` folds a
  per-weapon (Pierce/Blunt/Bow) coefficient INTO pDef at the hit (so an ignore-def skill bypasses
  it); `Entity.{Pierce,Blunt,Bow}DefCoef`; demo `obsidian_knight` (resist sword/arrow, weak to blunt).
- [x] **Ranged + caster mobs (mob roles)** — `MobRole` {Melee, Archer, Mage}. Archer = bow basic
  from ~450 range, ×2 P.Atk, light armor (orc/dune/fen/dread archers). Mage = NO basic attack,
  casts two leveled mob spells (`mob_nuke` 600/4s/1s pow 18-129, `mob_bolt` 150/1.5s/0.5s pow 7-33;
  13 levels by mob level), higher M.Atk / lower P.Atk+P.Def, MP-gated → out of MP it stands
  helpless (watcher_eye/aether_wisp/rift_portling/radiant_mage). Reuses the player cast pipeline
  (LearnedSkills + QueuedSkillId); mobs cast at authored time (WIT multiplier bypassed).
- [x] **Golem-type resist** — obsidian_knight: Pierce ×1.43 P.Def, Bow ×2, Blunt ×0.5 (weak).
- [~] **RE-CHECK balance after the mob curve** — mobs are now ~2-3× prior HP/def/atk. **Matrices
  regenerated** (`docs/BalanceMatrix.md` §H Mob↔Player @40/@75; §I per-gear-tier 40/52/61/76 vs the mob
  curve from `gear_sets.csv`). NO one-shots; gear DEFENSE keeps pace, but **player OFFENSE falls behind at
  high tiers** (solo grind balloons — fighter 13→131 hits, mage 19→210 casts L40→76). **Owner decision:**
  raise high-tier weapon atk / ease mob HP+def at 61-85 / lean on crit+attributes+party; add a jewel tier.
  Also open: cleric-solo-L30, <40 feel, archer ×2 lethality to squishies.
- [~] **Gear/item overhaul** (`docs/gear/gear_sets.csv`). **DONE:** foundation (StatMods carries item/set
  stats; MAtk%/MagicCrit attr types + ToStatMods bridge) + **40 tiered WEAPONS** (8 types × 20/40/52/61/76,
  ids `<key>_t<level>`, D/C/B/A display) with level-driven attributes (count 40→1/52→1/61→2/76→3, per-level
  maxes, caster pool via `IsMagicWeapon`, bow slow/very-slow via `AttackSpeedBase`) + **attribute-cancel
  debug** (`DebugCancelAttr(index)`; -1 = all). + **50 base ARMOR/shield/accessory/JEWEL pieces**
  (`TieredArmor()`, base stats on existing rails, no attributes). **NEXT:** set BONUSES via StatMods incl.
  main-stats (main-stat pre-pass in RecomputeDerived) + dmg/support variants + cohesive names; debug-give
  single body + accessory box; remove old armor drops + add new as rare; regen matrix. See [[gear-item-overhaul]].
- [>] **Base class kits** — owner to provide several passives/buffs/skills per class; wire them
  as real per-class content (beyond the placeholder discipline kits), then tune.
- [>] **Fighter balance pass** — awaiting owner targets (Venomweaver burst cap, tank durability
  vs +N-level mobs, etc.). Mechanics are fine; numbers need it.
- [>] **Skill-detail TITLE shows base name** — owner to give exact skill + race/2nd/3rd class
  next test; suspect client `_myThirdClass` not synced after a DEBUG 3rd-class change.

- [x] **Training Grounds** — immortal/stationary/0-damage **Training Dummy** mobs at Lv
  20/40/60/80 (MobType.Dummy + Entity.TrainingDummy; spawn zones ~(22500–25500, 4000)) for
  damage/skill testing. Reach via debug Teleport → Zones.

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
  physical **Slow** (demo "Hamstring") + a **damage-OUT pipeline** (`FinalizeDamage`):
  a 2×3 matrix of PvE/PvP × skill/magic/basic damage bonuses + per-skill `Pvp/PveDamageMult`
  (all neutral until PvP exists). Demo "War Focus" (+15% AS, +25% PvP skill/basic).
  + **conditional damage** (+% vs slowed/rooted/stunned/feared; `SkillDef.ConditionalOn`/
  `ConditionalDamagePct`; demo "Glacial Spike"). Existing non-contest debuffs left on the
  fizzle model (owner: new-only). **P1 light items DONE.**
- [~] **P2 heavy systems — STARTED.** **DoT-with-stacks DONE (L2 separated model)**: a DoT
  applies (1) a **damage effect** (flat per-tick, overrides by Rank, cure/cancel by flag+level)
  and (2) a separate **stack counter** (`SkillDef.StackKey`, hidden/`Internal`) that the burst
  consumes (`ConsumeStackKey`) for ×stacks — leaving the DoT. Counters are per-skill and
  shareable, independent of override/cure. Demo: Rupture → Detonate Wounds (Venomweaver).
  **Generalized stacking**: editable max + a per-stack effect TABLE (`SkillDef.StackLevels`) —
  each stack is an effect level (its own Effect + Magnitudes), so a stack can change the effect
  qualitatively (Tempest "Creeping Frost" = slow 10/20/30% on 1-3, FREEZE on 4). Stacks only on
  a successful land. A bare counter = stacking with no table (rogue burst fuel).
  **Poison/venom secondaries DONE**: new `DebuffAtk` / `DebuffAtkSpeed` / `DebuffCastSpeed`
  stat-debuff channels (outside AnyBuff, folded in the Effective getters); Venomweaver is now
  per-race — Human bleed (−MS), Elf poison (Toxic Sting/Burst, −AS/cast), Ork venom
  (Envenom/Venom Burst, −atk/def). **Stack/effect visibility DONE**: buff bar "Name xN"; inspect window "Effects:" line w/ stacks.
  **Cure/cancel DONE**: one `Dispel` helper + `SkillEffect.Cancel`; `SkillDef.DispelMask`
  (effect filter, e.g. cure-poison = Poison|Venom), `DispelCount` (random N), `DispelMaxLevel`
  (Rank ≤), `Cancellable` flag (internal counters immune). Demo: healer "Antidote" (cure
  poison/venom), nuker "Dispel Magic" (strip 2 random buffs). **Cancel resist**: each cancelled
  buff rolls a save vs the victim's `CancelResist` (`SkillEffect.BuffCancelResist` /
  `PassiveEffect.CancelResistPct`); tank ult "Indomitable" = +80%. **Absorb shields DONE**:
  `SkillEffect.Shield` + `BuffInstance.ShieldPool` (flat Power + % max HP); `ApplyDamage` soaks
  the pool before HP for all damage types, removes the buff when empty. Demo: tank "Aegis"
  (8% max HP, 15s). **Mana shield + lethal save DONE**: `SkillEffect.ManaShield` (divert % of
  damage to MP at a per-dmg rate) + `LethalSave` (survive one fatal blow → revive %), both in
  `ApplyDamage` after shields. Demos: Magus "Mana Barrier", Bulwark "Last Stand".
  **Taunt + real threat DONE**: mobs keep a threat table (`Entity.Threat`, threat = damage)
  and target the top-threat foe; `SkillEffect.Taunt` spikes threat + locks the mob briefly
  (`TauntLockTicks`); detaunt sheds 90% threat and retargets. Demo: tank "Provoke".
  **Blink + knockback DONE**: `SkillEffect.Blink` (caster → behind target, or away by
  `BlinkRange`) + `SkillEffect.Knockback` (shove target by `KnockbackRange`); `PlaceEntity`
  clamps + regrids. Demos: Phantom "Shadowstep", Trapper "Repelling Shot", Tempest "Phase
  Shift". **STEALTH + TRAPS DONE (2026-07-07):** `SkillDef.GrantsStealth` → `Entity.StealthTicks`
  (invisible to mob AI, sheds current aggro via `DropAggroOn`, broken by any offensive action; demo
  Phantom "Vanish"); `SkillDef.PlacesTrap`/`TrapRadius`/`TrapLifeTicks` → server-only `World.Traps`
  scanned each tick (`TickTraps`/`FireTrap` delivers the skill's damage + contested CC to the first
  intruder; demo Trapper "Snare Trap" = damage + Root). Both ride flag FIELDS, not new SkillEffect bits
  (enum full). **P2 combat primitives COMPLETE** (poison/venom secondaries were already done). (Shield
  floating-text shows pre-absorb damage — cosmetic.)
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
- [x] **Crafting & material economy** — BUILT (`docs/Crafting.md` / [[crafting-economy-design]]): mats drop
  from mobs (5 types ↔ 5 professions), refine 5-same+2-cross, finished-item recipes, all 5 professions craft,
  profession persist+choose, boss/elite mat piles. Scaled Common/Unc/Rare DROP gear (Epic set = craft/boss).
  **Polish DONE:** `KnownRecipes` (persisted) unlocks DropOnly A-grade recipes via a dropped recipe BOOK
  (EquipSlot.Box → open to learn; A-grade bosses drop them); L2 mutually-exclusive drop GROUPS (`DropEntry.GroupId`
  — one weighted pick per group; body/weapon copies grouped in StandardDrops). Numbers retune-later.
- [x] **Party / grouping system** — COMPLETE. Server + transport: `Party` (leader + members) in World;
  invite/accept-decline/leave/kick commands+hub+handlers; leader reassigns + auto-disband under 2;
  XP SPLIT among in-range members (level-weighted + size bonus) + kill-quest credit to all in range;
  AoE ally heals/buffs (`PlayersInRadius`) target PARTY members only (solo = self). WPF party WINDOW +
  invite button + invite prompt done last session. **LOOT RULES DONE (2026-07-07):** `LootMode`
  {FindersKeepers, Random, RoundRobin, LeaderOnly} on `Party`; `LootRecipient` routes each item drop
  (RoundRobin cursor / random / leader-if-in-range / killer); boss-mat pile → one recipient; **GOLD
  ALWAYS splits** evenly among in-range members (`AwardGold`, killer keeps remainder) regardless of
  mode; leader-only `PartySetLootMode` cmd+hub+channel; `PartyUpdate` carries `LootMode`; client party
  panel loot dropdown (leader-editable). See [[party-loot-modes]].
- [~] **Active mob skills** — caster (Mage-role) mobs cast two generic leveled spells (nuke + jab,
  MP-gated); BOSSES now have data-driven unique kits + phases + adds (see Boss mechanics, `BossCatalog`);
  client cast-bar for mobs done. Still to do: mob buffs/heals/CC for NON-boss mobs (shaman heals, etc.).
- [~] **Leveled MobMastery layer (mobs_passives.csv)** — BUILT (`Game.Shared/MobMasteries.cs`): the
  per-level tables (Weapon/Armor Weight, M.Atk/P.Atk/Max HP/MP/Regen HP/MP/M.Def/P.Def Mods, Pierce/
  Blunt/Bow Resistance) + `MobMasteries.Build(...)` that resolves per-mastery LEVEL picks into a
  `MobMod` (extended with MaxMp/AtkSpeed/HpRegen/MpRegen mults + flat Eva; applied at spawn). Demo:
  obsidian_knight authored via `Build(pierce:10, bow:12, blunt:2)`. STILL TODO: Stun/Fear/status
  resists (with the CC layer), and moving mob picks off `MobMod` onto a mob StatMods fold if desired.
- [x] **Boss mechanics** — DONE. **±10-level rule** (`StatCalculator.RaidLevelGapMult` in `FinalizeDamage`);
  **enrage** timer (`BossTick`: one-time +50% atk / faster-swing rage after ~90s, undone on leash-reset);
  **telegraphed AoE** "Devastating Slam" (`boss_slam`, `TargetMode.EnemiesInRadius`); **visible mob cast-bar**
  (`MobCastInfo` DTO + client rendering). **PER-MOB UNIQUE SKILLS + PHASES + ADDS DONE (2026-07-07):**
  data-driven `BossCatalog` (`BossProfile` keyed by mob-template id = a `BossSkillEntry[]` kit with HP-gated
  entries + a `BossPhase[]` HP-threshold script). `BossTick` now runs the enrage timer, the phase script
  (`AdvanceBossPhases` → announce / `EnrageBoss` / `SummonAdds`) and a skill rotation (`SelectBossSkill` picks
  the first ready HP-gated skill with a foe in radius; reuse via per-skill `CooldownTicks`/`SkillCooldowns`).
  `SummonAdds` spawns Normal-rank, no-zone (no respawn) minions engaged on the boss's target via a refactored
  `BuildMob` (extracted from `SpawnOneInZone`; also used by zone spawns). New phase skill **"Thorn Nova"**
  (`boss_thorn_nova`, magic AoE + slow). Demo boss: Valley Treant Lord (slam → 50% enrage+2 bogwood
  adds+Thorn Nova → 25% shout). `ResetMob` re-arms phases + clears reuse. See [[boss-mechanics]].
  **Deferred:** boss buffs/heals, multi-stage HP-bar phases, unique skills for the other bosses.
- [ ] **Pets & summons** — immovable healing totem, class pets (Trapper/tank), mage
  summoner. ([[pets-summons-design]])
- [ ] **Buffer = "Enchanter" + full-buff NPC to 75** — owner direction ([[buffer-enchanter-design]]):
  ONE buffer class holds ALL buffs (race-flavored); add **dances/songs** (extra atk/cast mults) to the NPC
  buffer later; a **full-buff NPC buffer up to lvl 75** is the SOLO stopgap. High-tier solo being hard is
  INTENDED — buffs/party close the gap, don't nerf the mob curve.
- [ ] **Position bonuses** — backstab / flanking damage (hook reserved).
- [ ] **PvP / PvE multipliers** — wire the multiplier hooks (currently default 1.0).
- [ ] **Perfect / excellent block** — shield block tiers above the current flat block.
- [ ] **Class-vs-class balance matrix** (buffed) + damage-K tuning once all kits exist.
  ([[class-race-identity]])

## EVENTUALLY (long-term / large)

- [~] **The real client** — 2.5D Unity (no Z axis; server stays 2D), reusing `Game.Shared`
  + `NetworkChannel`. The WPF app is only a test harness. ([[client-3d-and-los-design]])
  **STARTED (2026-07-03):** `Game.Shared` now multi-targets `net8.0;netstandard2.1` (+ IsExternalInit
  polyfill) so Unity can consume it. A vertical slice lives in `Game.Client.Unity/` (scripts + README):
  ported `NetworkChannel`, `GameBoot` (auto-login→enter world), `EntityManager`/`EntityView`
  (billboard quads + interpolation), `CameraRig` (steep pitch now → lower to ~50 for 2.5D), touch
  move/attack, main-thread dispatcher. Owner builds the Unity project + Android per the README. NEXT:
  UI (target frame/skill bar/cast bars), then swap billboards for animated 3D models (visual-only).
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
