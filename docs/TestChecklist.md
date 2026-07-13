# Test Checklist — L2Clone (branch Gena)

Running list of things to verify in-game. Claude keeps this updated as features land;
the owner tests manually and ticks items off. **`[ ]` = not tested, `[x]` = verified,
`[~]` = tested, needs tuning.** Newest features first. When asked to test, Claude shows
this file.

---

## To test now (BUFF ROWS / SET TOOLTIP / TRAINING OUTPOST — 2026-07-13)

### Buff bar — 4 rows by subtype
- [ ] The buff bar is now **four rows**, each hiding itself when empty, tinted to read apart:
  **buffs** (blue), **debuffs** (red), **item effects** (bronze), **consumables** (green).
- [ ] Drink a **healing or buff potion** → it appears in the **consumables** row (green), not mixed
  in with your buffer's buffs.
- [ ] A buffer's buffs stay in row 1; a mob's slow/bleed lands in the **debuff** row.
- [ ] The **item row is empty for now** — armor sets and weapon specials are still StatMods, not
  buffs. That row appears the moment they become skills (owner said row 3 can stay invisible).
- [ ] Double-click still drops a (beneficial) buff early.

### Item tooltip — set bonus
- [ ] Hovering a **set piece** now shows the set section: the set name, **Bonus:** (what it gives),
  **Items 3/4**, and a line per piece — **green ✔ = worn**, **grey ✖ = missing**.
- [ ] The Bonus line + count go **green only when the set is complete**, grey otherwise.
- [ ] Accessories are shared across a tier's bodies, so hovering a helm shows it against whichever
  body you're actually wearing.
### Set SHIELD bonus (2026-07-13 — I had this wrong first time)
- [ ] Each tier's **shield now belongs to that tier's HEAVY set** (the CSV puts shields in the same
  GroupId). It is **not** required to complete the set — wearing it adds an **extra** bonus on top.
- [ ] The tooltip now shows a **Shield Bonus:** line + the shield piece (green ✔ worn / grey ✖ not).
  It only goes green when the **4-piece set is complete AND its shield is equipped**.
- [ ] The shield extras, straight from the CSV (def-oriented heavy line only — the `_dmg` variants
  get none):
  - **Heavy 20** → +10% Shield Def
  - **Heavy 40** → +5% P.Def
  - **Heavy 52** → +25% Shield Def
  - **Heavy 61** → **reflect 5%** of melee basic-attack damage back at the attacker
  - **Heavy 76** → +5% P.Def, +5% M.Def, +25% Shield Def, reflect 5%
- [ ] Wear a full Heavy 61 set + its shield and let something melee you → you should see the
  attacker take **reflect** damage (bows are excluded; capped at 50%).
- [ ] Equipping a shield from the WRONG tier gives no extra (its SetId won't match).

### Training Outpost (safe zone by the dummies)
- [ ] A small **safe zone "Training Outpost"** (24000, 5000, radius 400) sits just SOUTH of the
  training dummies (they're at y=4000). It's deliberately clear of them — a safe zone keeps mobs
  out, and the dummies ARE mobs.
- [ ] Inside it: **Gatekeeper Vess** at the north edge and **Spirit Helper Ilva** (buffer) at the
  south, offset so their labels don't overlap.
- [ ] Buff up at Ilva → walk ~800 north to the dummies → test → teleport out with Vess.
- [ ] The outpost is now also a **teleport destination from every other gatekeeper**.

---

## To test now (EVERYTHING IS A SKILL — potions/scrolls — 2026-07-13)

### Typing in text boxes (the real cause of "can't write in the auto-potion boxes")
- [ ] You can now **type into every text box**: auto-hunt HP%/MP%, farm range, per-skill reuse,
  debug tuning. The key handler only excused the CHAT box, so typing anywhere else fired the game
  hotkeys and ate the keystroke — "5" cast skill 5, "i" opened the inventory, and the digit never
  landed. That's why it "used skills" instead of writing.

### Consumables now cast skills
- [ ] **Healing potions are skills.** Minor/Healing are heal-over-time BUFFS (1%/s and 2%/s for 15s)
  — they now appear on the **buff bar** instead of a bespoke potion channel. Greater is an instant
  50% heal.
- [ ] Drinking a **Greater potion over a Minor HoT** replaces it (BuffKey + Rank does the
  "stronger cancels weaker" that the old hand-rolled potion-rarity state did).
- [ ] The **shared 30s drink cooldown** across healing potions still works; **buff potions still
  ignore it**.
- [ ] **Auto-potion (auto-hunt) still drinks the best HP potion** below your threshold, and the
  "keep buff potions active" top-up still works.
- [ ] Potion **tooltips** now read from the skill's description.

### Return scrolls are no longer learned skills
- [ ] **Scroll of Return / Ultimate Scroll of Return no longer appear in your skill list.** They were
  auto-granted as learned skills; now the ITEM grants the skill. Double-click the scroll → it still
  channels and teleports, and is still consumed (refunded if interrupted).
- [ ] The plain **Return** skill (30s cast, 5min cd) IS still a learned skill everyone has — unchanged.

---

## To test now (DEBUG GEAR PICKER — 2026-07-13)

- [ ] Debug → **Equip** tab is now a drill-down: **Armor & Shields / Weapons / Jewels** → pick a
  **level (20 / 40 / 52 / 61 / 76)** → click any individual piece to receive it.
- [ ] The **level 20-40 sets now exist in the menu** — they were never exposed before (the old tab
  only had a few hardcoded E-grade "rare" items and the named sets, none of which were the tiered
  gear). That's why they looked missing.
- [ ] Armor levels also offer **★ Full Heavy / Light / Robe Set** (body + helm + gloves + boots) in
  one click, since a set bonus needs all four. Individual pieces are listed underneath.
- [ ] Weapons list all 8 families per level (sword 1H/2H, blunt 1H/2H, duals, bow, wand, staff) —
  use these to feel the new **weapon channel factors** (a staff should melee poorly, a sword should
  cast poorly).
- [ ] The lists are read from `ItemCatalog` (not hardcoded), so **new gear added to the CSV appears
  here automatically**.
- [ ] The old **Rare Weapons (E) / Rare Armor Sets / Named Sets (Dark Dominion)** blocks are gone,
  as requested. Boxes + Legendary (God's Judgment/Robes) are kept.

---

## To test now (SKILL BAR — 2026-07-13)

- [ ] **The bar is now saved per character** and survives relog exactly as you arranged it
  (`client-settings.json`, next to the exe, under `SkillBars`). Rearrange it, log out, log back
  in → identical layout.
- [ ] **It no longer reshuffles itself.** The old bar was rebuilt each login by enumerating a
  `HashSet` of learned skills — whose order is unspecified — so it silently reordered. New skills
  now go into the first FREE slot, in a stable order, and **never move a skill you placed**.
- [ ] Level up / learn a skill / change class → your existing layout is untouched; only genuinely
  new skills appear, in free slots.
- [ ] **Cooldowns survive a re-render.** Previously levelling up, changing class, or even dragging
  a skill rebuilt every slot object and silently wiped all running cooldowns. Cast something with
  a long cooldown, then level up / drag another skill → the cooldown keeps ticking.
- [ ] **Drag & drop** — re-verify. The payload now carries the SKILL ID (not just a slot index),
  and the drop re-locates the source by identity, so a bar that re-rendered mid-drag can no longer
  move the wrong skill. ⚠ If it STILL grabs the wrong one, tell me exactly which slot you dragged
  from, which you dropped on, and what actually moved — I could not reproduce the off-by-one by
  reading the code, so I hardened the path rather than pinpointing it.
- [ ] Side-fix: **your character's name now shows in the status line** (`_myName` was declared but
  never assigned, so it was always blank — which also broke the whisper self-check).

---

## To test now (DAMAGE RETUNE — 2026-07-13) ⚠ delete game.db first

**⚠ Delete `Game.Server/game.db` (+ `-shm`/`-wal`) before testing.** Not a schema change —
but existing characters have the OLD class-change stat bonuses (+10 CON tank, +10 WIT nuker…)
baked into their persisted stats, and those grants are gone now. A fresh DB avoids ghost stats.
*(Note: with `dotnet run` the DB is `Game.Server/game.db` — the `bin/Debug/net8.0/` path is
only used when launching from Visual Studio.)*

### Magic damage — the big one
- [ ] `MagicK` 8 → **91** (L2's real constant). Magic was doing ~1/11th of intended damage.
  A Lv-21 healer's Magic Bolt on a same-level tank should now hit for **~170, not 15**.
- [ ] Check this did NOT break **PvE** — 11× more magic damage means mages/healers may now
  shred mobs. Owner's standing target: *mage TTK ~60s @75, do not over-buff*. A healer
  should now kill a same-level mob in a reasonable time (it used to take ~1 minute).

### Archetype damage multipliers REMOVED
- [ ] The per-archetype basic-attack multiplier (tank ×0.55, rogue ×0.65, mage ×0.15,
  warrior ×1.10) is **gone**. Basic-attack damage = pure formula; the **weapon** differentiates.
- [ ] A Lv-21 tank's basic attack on a robe target should now land ~**43** (was ~24).
- [ ] **Daggers should no longer feel crippled**; bows should hit clearly harder.
- [ ] ⚠ **Watch mage melee**: with the ×0.15 gone, a mage's staff swing is now full-strength,
  and the newbie staff's P.Atk (23) is nearly the newbie sword's (24) — so a mage may melee
  about as hard as a tank. If that feels wrong, the fix is the **weapon table** (lower caster
  weapon P.Atk), not a class coefficient.
- [ ] New **"Class Balance"** passive on every class — visible in the skills window, does
  **nothing** (all-zero). It's the hook for later per-class PvE/PvP damage nudges.

### Cast speed rebased
- [ ] Mage base cast speed 166 → **333** (the 1.0× baseline); **ork mage 300**; fighters 150.
  Every mage cast used to silently take ~2× its listed time — a 4s bolt really took ~6.5s.
- [ ] Wearing **non-robe armor as a mage still halves casting** (Robe Mastery's existing
  −50% penalty) → the old 166. Confirm a mage in light/heavy armor casts at half speed.
  (Same numbers as L2's Spellcraft, which *doubles* cast speed while in a robe.)
- [ ] **Spirit Training** now gives a FLAT **+40** casting speed, not +40% — matching the real
  spiritshot. (The old percent was applied as a time cut = +67% speed, and compounded with
  WIT/gear/buffs; it alone inflated a buffed Lv-40 mage to ~2200 vs the 1999 cap.)
  Its magic-attack half is unchanged and correct: +100% at max = ×2 M.Atk = ×1.414 magic
  damage — exactly the spiritshot ratio.
- [ ] Expected casting speed now: Lv-40 elf mage in robe **unbuffed ~493** (L2 reference: ~500
  for a Lv-60 Spellsinger in robe with passives); **fully buffed ~1097**; with +5 WIT ~1388.
  The 1999 cap should only be reachable with an Enlightenment-style +50% buff — as in L2.

### Weapon channel split (P.Atk / M.Atk factors)
- [ ] A weapon now carries **ONE power number + two channel factors**. Fighter weapons: power =
  their P.Atk, ×1.0 P / ×0.6 M. Mage weapons: power = their **M.Atk**, ×1.0 M / ×0.6 P (their
  P.Atk is deliberately nerfed). Factors multiply the FINISHED channel, so they suppress the
  shared base (`AtkStat + level*2`) — which is what a second authored number could never do.
- [ ] **Mage melee is nerfed**: a Lv-21 healer's staff swing drops ~36 → **~21**. The nuke
  (~172) and the tank's basic (~43) are unchanged.
- [ ] A fighter's M.Atk is now ~60% of before — harmless (fighters have no magic skills).
- [ ] **A cleric who equips a SWORD should melee at full strength** (×1.0 P.Atk) and lose most
  of his casting. The class no longer decides — the weapon does. Verify this feels right.
- [ ] ⚠ **KNOWN GAP — the buffer.** At the current ×0.6, a buffer swapping staff → 2H sword
  loses only **15%** magic damage but **doubles** P.Atk — still a free win. Closing it needs the
  off-channel factor at ~0.2–0.3 (`const OffChannel` in `Items.cs` / the newbie weapon defs).
  Owner is feeling out 0.6 first.
- [ ] ⚠ **Check heals**: if heal power scales off M.Atk, a sword-wielding cleric can barely heal.
  That may be the intended trade — confirm.

### Stats no longer grow on their own
- [ ] **Class change no longer raises main stats** (the old +10 CON / +10 WIT grants are gone) —
  the class-change dialog no longer advertises a stat bonus.
- [ ] `LevelStatBonus` (the free +1@20 … +5@80 "dye stand-in") is **removed** — CON/ATK/WIT/DEX
  stay what you were born with. The level-40 stat-swap passives (not built yet) replace it.
- [ ] Sanity-check that mages don't feel *slower* than before despite losing the free WIT
  (the 333 rebase should more than cover it).

---

## To test now (disconnect / exit / combat + Return — 2026-07-09)

### Return skill + scrolls
- [ ] Everyone has a **Return** skill (30s cast, 5min cd): channel it → teleport to the nearest town.
  Taking **any** damage cancels it. Cast speed / cooldown buffs do NOT change its 30s/5min.
- [ ] Buy a **Scroll of Return** from the Apothecary (500g); double-click it → 10s cast → teleport.
  It's consumed on success, refunded if interrupted. Sells for 0.
- [ ] (Debug-give) **Ultimate Scroll of Return** → ~instant cast return. Not sold/dropped.

### Combat state + exit
- [ ] **Exit Game** (Settings) works out of combat (app closes). During combat (dealt/took damage in
  the last 30s) it's **blocked** with a message; 30s after the last hit you can exit.

### Disconnect fates (use 2+ clients)
- [ ] **Go Offline (Auto-Farm)** button → you return to account select and your char keeps farming
  (offline), visible to others, until the 2h cap / death / relogin.
- [ ] Drop while **auto-farming** (out of town) → offline farm (2h cap). The 2h cap is ONLY for
  offline farming — NOT for a network blip.
- [ ] Drop **mid-combat but not auto-farming** → your char **keeps defending** its current target
  (anti-combat-log) and the 180s grace timer is **paused** until combat ends (30s after the last
  hit); then the grace counts down. It is NOT put into the 2h offline farm.
- [ ] Drop while **out of combat, not auto-farming** → your char shows a **"⚠ Disconnected"** title
  above its head to nearby players, stays frozen and **in your party** (OFFLINE tag) for **180s**.
  Reconnect within 180s → resume seamlessly. After 180s → normal removal (leaves party).
- [ ] A disconnected (grace) char that a mob kills is removed immediately.
- [ ] Offline-FARMING chars still look like normal players to non-party (no Disconnected title);
  only the grace state shows the title.

---

## To test now (auto-hunt / idle farming — Phase 1, 2026-07-08)

**⚠️ Schema change:** added the `AutoHuntJson` column → **delete `Game.Server/bin/Debug/net8.0/game.db`
(+ `-shm`/`-wal`)** so it recreates before running.

### Auto-Hunt window (WPF client)
- [ ] Enter world → an **"Auto-Hunt"** button appears top-right; it opens the config window.
- [ ] The window lists your **active skills** (passives hidden) with an enable checkbox, a type tag
  (attack/buff/debuff/heal) and a **reuse (s)** box prefilled with each skill's own cooldown.
- [ ] HP%/MP% potion boxes + "Keep buff potions active" checkbox. **Apply** saves; settings **survive
  relog** (persisted).

### Behavior
- [ ] Toggle **Enabled** (checkbox) → you auto-walk to the nearest mob, basic-attack, and cast your
  enabled auto-skills; killing one retargets the next. Turn it off → you stop acquiring new targets.
- [ ] **Attack** skills fire on cooldown; a **buff** skill only casts when its buff is missing on you;
  a **debuff** only when the target doesn't already have it; a self-**heal** only below 70% HP.
- [ ] Raising a skill's **reuse (s)** above its default makes it fire slower (never faster than default).
- [ ] **Auto-potions** work even with auto-hunt OFF: drop below the HP% and the best HP potion is drunk
  (shared potion cooldown respected). (No MP potions exist yet — MP% is a no-op.)
- [ ] "Keep buff potions active" re-drinks any buff potion in your bag whose buff has expired.
- [ ] The window footer shows **Mana: X /s** (sum of enabled auto-skills, after any MP-cost/CD buffs)
  and updates as buffs go up/down.
- [ ] Loot/XP/gold behave exactly like manual play (incl. party loot rules) — no idle penalty.

### Offline farming (Phase 2, 2026-07-08)
- [ ] With auto-hunt **ON** and standing in a mob field (not a town), **close the client / disconnect**.
  A second logged-in character nearby should still **see your character** and watch it fight mobs
  (it keeps hunting; a "keeps hunting while away" line appears).
- [ ] **Log back in** → you re-attach to that same character with the loot/XP it gained while away
  (not a stale copy).
- [ ] Disconnecting **in a town** (safe zone), while **dead**, or with auto **off** does a normal
  logout (no offline farming).
- [ ] An offline farmer that **dies** (mobs out-damage its potions) stops and logs out ("stopped
  hunting"); on next login it's alive with auto **off** (must re-enable).
- [ ] Caps: idle **8h** online / offline **2h** (constants — verify via a temporary lower value if
  needed). Hitting the idle cap stops auto and **blocks re-enabling until you re-log**.
- [ ] Auto-hunt while offline still obeys the shared potion cooldown, buff-potion top-up, and skill
  conditions (same brain as online).

### Debug Tuning panel (2026-07-10) — live tuning while you play
- [ ] Settings → **Debug Tuning (admin)** opens a panel of live values (rates, karma, caps).
- [ ] Change **Exp/Drop rates** + Apply → next kills/drops use the new rate immediately.
- [ ] Change **karma** values (base / ×consec / ×level / −death / −mob) + Apply → next PK/death/mob-kill
  uses them. Change **idle/offline/grace** caps (seconds) + Apply → observe a cap/grace fire quickly.
- [ ] **Idle/offline cap = 0 → unlimited** (leave someone farming/levelling to gauge speed).
- [ ] Debug values **persist across server restarts** (`debug-config.json` in the server folder).
- [ ] **Window size persists** — resize the client, close, reopen → same size (`%LocalAppData%\L2Clone`).
- [ ] Config files are gitignored + not in the build/Debug output.
- [ ] Non-admins: the panel does nothing (server refuses).

### PvP + flag/karma/PK (2026-07-10) — ⚠ delete game.db first (new karma columns)
- [ ] Top-right **PvP** and **Counter** toggle buttons (tint when on).
- [ ] With **PvP: On**, attack another player **outside a town** → you turn **purple** (name), the
  target can hit back; skills/basics land and show damage. (Damage-check the migrated crit/eva + skills.)
- [ ] Attacking an **innocent (white)** needs PvP On; a **purple/red** player is attackable without it
  (retaliation / hunting a PK) and attacking a **red** player does NOT flag you.
- [ ] **Kill an innocent** → you go **red (PK)**, gain **karma** (200 base; more per consecutive kill and
  the more levels above the victim); status line shows KARMA.
- [ ] **Kill a flagged/red** player → **PvP count** up, no karma.
- [ ] **Dying** as a PK lowers your karma by 200; at 0 the **red name clears**.
- [ ] **Farming as a PK** sheds karma: each mob kill is −20 (take a camper's spot, grind it clean).
  (All karma values are tunable consts: base/consec/level growth, per-death, per-mob.)
- [ ] **No PvP in towns** (safe zones block it; 0 damage if someone runs to town mid-fight).
- [ ] **Counter-attack**: an auto-hunting/offline char with **Counter: On** retaliates against a player
  attacker (finishes a near-dead mob first, else switches).
- [ ] Karma persists across relog (a PK is still red after logging back in).

### Stats-via-skills identity migration (2026-07-10) — verify no regressions
- [ ] Rogue still has its crit/evasion identity (now from the **Evasion Mastery** passive: +20% crit,
  +20 eva); archer from **Reflexes** (+15% crit, +10 eva). Numbers should feel unchanged (parity).
- [ ] **Intentional change:** the **tank** no longer gets the old +level/2 magic defence (his Anti-Magic
  passive is his magic identity now) — confirm tank magic survivability still feels right.
- [ ] **Intentional change:** a base **rogue's basic attacks no longer interrupt casts** (that "cancel"
  becomes a 3rd-class discipline passive later) — confirm that's the intended feel.

### Roaming + target filters (2026-07-10)
- [ ] Auto-Hunt window now has a **Farm range** box, a **Static spot** checkbox, **Mobs/Elites/Bosses**
  checkboxes, and a **Basic Attack** row atop the skill list.
- [ ] **Roaming** (Static off): with auto on and no mob nearby, the character **wanders** within the
  farm range and engages mobs it finds; the search circle follows you.
- [ ] **Static spot** (on): it only engages mobs within the circle centered where you turned auto on;
  when none are left it **walks back to the center**. It may chase a fleeing mob slightly outside.
- [ ] **Rank filter**: with only **Mobs** checked it ignores elites/bosses; tick Elites/Bosses to include them.
- [ ] **Basic Attack** row: fighters tick it → they melee when no skill is ready. A **mage unticks it**
  → it only casts skills and never melees (walks into skill range instead).
- [ ] Settings survive relog (persisted).

### Party + AFK interaction (2026-07-08)
- [ ] You **can't invite** a player who is auto-hunting (idle) or offline-farming — you get "X is
  auto-hunting and can't be invited right now."
- [ ] A party member who turns auto-hunt **on** shows a yellow **• AFK** tag on their roster row;
  a member who goes **offline-farming** shows a grey **• OFFLINE** tag (hover for a tooltip). The
  tag clears when they turn auto off / reconnect.
- [ ] The party can **kick** an AFK/offline member normally.
- [ ] If the **party leader** goes offline-farming, leadership passes to an online member (★ moves).
- [ ] A party **invite** left unanswered **auto-expires after ~30s**: the invitee's prompt disappears
  and the inviter is told "X didn't respond," so they can re-invite (no permanent "considering
  another invite" block).
- [ ] An offline member that logs out (cap/death) **leaves the party**; one that reconnects **stays**
  in it (tag clears).

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
- [ ] A new party **defaults to Random** loot (settings-panel-configurable later).
- [ ] The party panel shows a **Loot** dropdown. Only the **leader** can change it (disabled for
  members).
- [ ] Leader changing the dropdown **starts a vote**: every other member gets an **Agree/Decline**
  prompt; the leader sees "waiting for the party to agree." It applies **only if ALL agree**
  ("Loot rule set to … (agreed by all)"); any **Decline cancels** it, and it **times out** (~30s).
  On cancel the leader's dropdown snaps back to the current rule.
- [ ] A party invite **prompt shows the inviter's name AND the loot rule** you'd join under.
- [ ] **Finders Keepers**: item drops go to whoever landed the kill (as before).
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
