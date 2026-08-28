# OPEN CHECKLIST — the 0.93.0 pass

> **Rolling and unversioned, and this one is a CLEAN RESET** — your own instruction: *"whatever is
> build/finishes goes to backlog/playarchive … now my finds is full and annoying but its build"*.
> Everything you already answered is out of this file:
> - **Your finds from playtests 27 and 28** (all twelve, plus playtest 27's five) →
>   [Playtest-Archive.md](Playtest-Archive.md#playtest-27-28).
> - **§90's rows you played** — the two 3rd-class kits, the circles, the demon mage →
>   [Playtest-Archive.md](Playtest-Archive.md#pass-0770-0890). §91 and §89's routing went with them.
> - **The one thing still unbuilt** — *"shouldn't hit at all on the floor"* — is now **`BL-94`** in
>   [docs/Backlog.md](../Backlog.md), where it will not get lost between passes.
>
> What is left below is **only what has never been played**. §93 is this build; §92, §90, §89, §85,
> §81 and CARRIED FORWARD are rows that survived earlier passes without being reached.

> 🔴 **THE APK IS NEW — the first since 0.89.0**, and seven versions rode in on it: `BL-93`'s model
> plumbing (0.90.0), **the player HP rebuild** (0.91.0), HP Boost + Swift (0.91.1), the MP measurement
> and the toggle-upkeep bug (0.91.2), **MP potions** (0.92.0/0.92.1) and **stack caps** (0.93.0).
> **`ProtocolVersion` is 28 → 29** — but the server still accepts from 8, so an **old client connects
> and looks perfectly fine while having none of it. Install BOTH halves.**
>
> 🔴 **BEFORE YOU PLAY: delete `Game.Server/game.db`** (and `-shm`/`-wal`) — owed since the 0.71.0
> schema change, again since `AccountRole` renumbered, and again for the chat log table. ⚠ It is in
> `Game.Server/`, not `bin/Debug/`. **0.93.0 adds no schema of its own** and migrates your stacks on
> login instead, so that one is not a reason to reset.
>
> ⚠ **Your HP is 2-3× bigger and no creature number moved** (0.91.0). That is the single largest change
> in this build and it will colour every other reading you take — §93A first.
>
> **Rows are the format you picked (option 2):** write your comment after the `->`. Put `x` in the `[]`
> if it passed with nothing to say, `~` if it works but wants a change, `!` if it is a bug or priority,
> `?` for a question. A `-` row with no id is a free line for that section — add as many as you like.
> **Your own "My Finds" section is at the top** — keep using it, it is where most of the real content
> arrives.

---

## My Finds — next pass (empty, write here)

*Blank page. The previous seventeen are answered and archived.*

⚠ **What is worth aiming at first**, because it is where this build changed most and none of it has
been played: **how long you now live** against a same-level creature (§93A), **a caster's mana over a
real farm hour** now that potions exist and a buffer's toggles cost triple (§93C, §93E), and **whether
a stack of 9 buff scrolls is the friction you wanted or just friction** (§93D).

- [ ]

- [ ] 

- [ ] 

---

---

---

## 96. THE BOSS'S JUDGMENT + THE RAID LOCK — `BL-98` and `BL-99`, built in 0.95.0

Your ladder, built as given. The gap that triggers it is unchanged at **±9**.

```
rung  lasts    runs out into   offend while holding it
L1     3 min   → L2            (cannot act)
L2     1 h     → clean         → L3
L3    30 min   → L4            (cannot act)
L4     1 h     → clean         → L5
L5     2 h     → L6            (cannot act)
L6    24 h     → clean         → L5      ← cycles L5<->L6 until 24h pass un-offended
```

🔴 **DELETE `Game.Server/game.db` (and `-shm`/`-wal`) BEFORE THIS PASS** — two new columns
(`BossJudgmentRung`, `BossJudgmentUntilUtc`), and `EnsureCreated()` will not add them to an existing
file.

⚠ **The three petrifying rungs are 3 min / 30 min / 2 h. Test L1 and L2→L3 properly; L5 costs you two
hours of a character.** Everything below L5 is enough to prove the ladder.

| # | test | expected |
|---|---|---|
| 98a | 10+ levels above a raid boss, hit it once. | The hit lands, then stone. System line: **THE BOSS'S JUDGMENT — L1. Petrified for 3 minutes.** |
| 98b | While petrified, let anything swing at you, and have someone heal you. | **Zero damage and zero healing, every time.** HP does not move in either direction. |
| 98c | While petrified, try to move, attack, cast, sit. | All refused. A cast in flight was broken the instant it landed. Message names the **rung**, not "stunned". |
| 98d | Wait out the 3 minutes. | You unfreeze and immediately hold **L2** — a bar icon with a 1-hour clock and **no effect at all**. You play normally. |
| 98e | While holding L2, offend again. | **L3 — 30 minutes.** This is the escalation; it is the row that proves the ladder. |
| 98f | Take L1, then let the whole hour of L2 run out without offending. | *"The boss's judgment has lifted. You are clean."* The next offence starts at **L1** again. |
| 98g | 10+ levels above, **heal or buff a PARTY MEMBER fighting a raid** — never touching the boss. | Same ladder. This is the exploit the entry was raised for, and since `BL-99` it is the only way this cause fires. |
| 98h | Same, but you are **inside the band** (gap ≤ 9) and in the party. | **Nothing.** The band is the whole test for a party member. |
| 98i | Stand next to a raid boss and let it **aggro you** without acting. | **No judgment.** Being noticed is not an act. |
| 98j | Cast a **party/AoE heal** that reaches two people fighting the raid at once. | **One rung, not two.** An area heal must not jump you from clean to L3. |

**Now the "unremovable" rows — these are the ones I would actually try to break:**

| # | test | expected |
|---|---|---|
| 98k | Cleanse / dispel a petrified player, with everything you have. | Nothing lifts. |
| 98l | Get petrified, then **log out and back in**. | Still petrified, with the time it had left. |
| 98m | Take **L2**, log out for **90 minutes**, log back in. | **Clean** — not a fresh hour. The ladder ran while you were away, exactly. |
| 98n | Take **L1**, log out for 10 minutes, log back in. | On **L2**, with ~50 minutes left. The L1 ended offline and handed over on schedule. |
| 98o | Take **L2**, then **die**. | Death clears your buff list — and L2 is **still there** a second later. This is the one I most want you to try; death was a real hole. |
| 98p | Take **L2**, then **change subclass**. | Same: the swap wipes buffs, the rung survives. |

### `BL-99` — a raid participant is unhelpable by outsiders

Your ruling: *"If you are boss engaged nothing can heal you outside your party … the splash never
reaches the pipe so never punish him … a single/target heal is deliberately trying an exploit and it's
punishable."* Two rules that divide the world between them — **`BL-99` gates on PARTY, at any level;
`BL-98` gates on the ±9 BAND, inside the party.**

```
caster is...        area / party support        aimed single-target support
in his party        lands (BL-98 band applies)  lands (BL-98 band applies)
NOT in his party    silently skipped, no cost   REFUSED at cast start + one rung, ANY level
```

| # | test | expected |
|---|---|---|
| 99a | Engage a raid boss. Have a **non-party** healer single-target heal you. | Refused before the cast starts: *"X is locked in a raid battle — only their own party can aid them."* **He takes L1** — at any level, no gap needed. |
| 99b | Same, but he casts **Urgent Great Heal** or plants a **totem** with you in range. | You are simply **skipped**. He is **not** punished, not even a message. He gets no rung ever from this. |
| 99c | Same as 99b, but he has a party-mate standing next to you who is also in the raid. | His party-mate **is healed normally** — your *"if heal comes for a party member u take the benifit"*. |
| 99d | Now **join his party**, still engaged. Every heal, buff and area heal from him. | All land normally. Party membership is the whole gate. |
| 99e | In the party but **10+ levels from the boss**, heal someone. | The `BL-98` ladder, as before. This is now the ONLY way the "aided the raid" cause fires. |
| 99f | Non-party **resurrect** on someone who just died in a raid (within 30s). | Refused + a rung. Reviving the raid's tank from outside is the same interference. |
| 99g | Non-party **buff / MP restore / cleanse** aimed at a raid participant. | Same as the heal — I applied it to all support, not just heals. Tell me if you wanted heals only. |
| 99h | 🔴 Press a **self-buff** while a locked raider happens to be your selected target. | **Nothing bad happens** — normal self-cast, no refusal, no rung. I broke this while building it and fixed it; worth one press. |
| 99i | Wait 30s after a raid participant stops fighting, then heal him from outside the party. | Lands normally. The claim expires. |

⚠ **Your *"heal per next -3%"* is moot now** (it was for a party-scoped chain heal) — and it was
already built at **2%**: `TargetFalloff: 0.02f`, 11 targets, 30%→10%. −3% cannot fit 11 slots.

🔵 **When an alliance / raid group exists, this must read "your raid group", not "your party."** Today
a party caps at 9 and a boss is tuned for a 5-man, so one party *is* the raid. The day two parties are
meant to fight one boss together, this is the line that would stop them healing each other.

⚠ **Deliberate boundary, not a miss:** an over-levelled character who only stands there and
**self**-heals is not judged. Your rule is "help some1", and he is invulnerable to a boss that far
below him anyway. Say if you want tanking-by-standing caught too. ⚠ Note `BL-99` does not close this
either — he is in the party, so nothing is locked against him.

🔴 **And the one thing still yours from last time: the band is SYMMETRIC** because you wrote *"9lvl
+-"*. That judges someone 10+ levels **BELOW** the boss too. The exploit you raised this for is only
the over half — one comparison in `StatCalculator.BossJudges` if the under half reads wrong.

---

## 95. THE 0.94.1 FIXES — your playtest finds of 2026-08-28

### A. AOE ACTUALLY HITS NOW — and it was two bugs stacked

Your report: *"AOE don't work ..it shows the red circle pulse but tont hit the mobs"*. Both halves
found, and the reason it looked like a display bug is that the display was the only half working.

1. **No player offensive AoE ever swept.** The damage sweep runs only for
   `TargetMode.EnemiesInRadius`, and **only mob spells ever set it** — Elemental Wave and Arcane Wave
   (the only two player attack AoEs in the game) set a radius but no mode, so they drew their circle
   and then hit the single target. Both now carry the mode.
2. **A latent second one that would have re-broken it**: the sweep read `def.AreaRadius` (the
   SkillDef field) while the circle read `def.AreaRadiusAt(lvl)` (the per-rung value). Any skill
   authoring its radius per rung got a **zero-radius sweep under a full-size circle** — the same
   symptom, waiting for the next skill. Both read `AreaRadiusAt` now.

| # | test | expected |
|---|---|---|
| 95a | Pull 3-4 mobs and cast **Elemental Wave**. | Every mob within 200 takes damage. Numbers on all of them, not just the target. |
| 95b | Same with **Arcane Wave** from max range. | Everything within 400 **of the mob** takes damage. |
| 95c | Watch the red circle on Arcane Wave. | It draws **on the mob**, not on you — the circle and the damage now come from one decision, so if they ever disagree again it is one bug not two. |
| 95d | Cast an area nuke repeatedly and watch for **Fail** and **Crit** lines. | Both must still appear. The shared hit path had neither; a player's AoE would have silently lost fizzle and magic crit. Mob/boss AoE deliberately unchanged. |

### B. Arcane Wave vs Elemental Wave — the shapes are right now

*"the arcane wave should AOE around the mob not the player like elemental wave"*. New
`SkillDef.AreaAtTarget` decides where the circle sits; `EnemiesInRadius` takes an origin.

**You asked whether 400/900 were swapped — they were not.** Arcane is range 900 (thrown from safety)
and radius 400 (the blast), which is what the code and the design note already said. Only the CENTRE
was wrong. **Elemental Wave's range moved 200 → 0** on your reading (*"self/aoe with 0 range"*), and
its 14 CSV rows moved with it.

| # | test | expected |
|---|---|---|
| 95e | Cast Elemental Wave with no target selected / standing in a pack. | Range 0 — it erupts where you stand, reach 200. |
| 95f | Arcane Wave at a mob ~800 away. | Lands. Blast is centred on it, 400 wide. |

🔵 **A conflict I did NOT resolve for you — see `BL-96`.** You called Elemental Wave `self/aoe`, but
on 2026-08-27 you ruled the TARGET column deliberately does NOT encode where the circle sits. Both
cannot be true of one column. The CSV keeps `enemy/aoe` (the 0.93.0 rule) until you rule; the GAME
behaves exactly as you described either way.

### C. Auto-on no longer leaks between subclasses

*"I'm buffer and have in skill belt the atack as auto on .. Then I change to/add new subclass ..and I
put atack on belt it's auto-on from the getgo"*. The skill BAR was per-subclass but the AUTO marks
were per-CHARACTER and keyed by skill id alone, so any class whose bar held that id rendered it armed.
⚠ Filtering the shared list on swap could not have fixed it — one list, so pruning for the incoming
class destroys the outgoing class's marks. The marks moved to `Subclass`, with a new
`SubclassRecord.AutoSkillsJson` column, and `ActivateSubclass` now pushes the corrected set.

⚠ **This is the same bug as playtest-17 B1, one level down** — that one leaked marks between
CHARACTERS. A subclass swap never leaves the world, so it never passed through that fix.

| # | test | expected |
|---|---|---|
| 95g | On your buffer, arm attack as auto. Swap to another subclass. Put attack on the bar. | **Not armed.** Never touched on this class = never auto. |
| 95h | Arm something on the new class, swap back to the buffer. | The buffer's own marks are exactly as you left them — the fix must not have cost you the other class's set. |
| 95i | Relog and check both. | Both survive; they persist per class now. |
| 95j | Auto-hunt on/off and the potion thresholds. | Still per CHARACTER — deliberately not moved; those are preferences, not kit. |

### D. 🔵 CHAT ON RELOG — I did NOT change this, because you ruled the opposite in playtest 28

*"relog in etc never clears the chat ...and it should. 3h later I reconnect or enter after a closed
game chat is on"*.

⚠ **The current behaviour is a feature you explicitly asked for.** `ClientLog.cs` records both sides:
your `C1` (*"chat must reset on exit"*) and then playtest 28 (*"chat again is saved between logins.
Don't reset"*). It now files the log per character (`chat_<characterId>.log` in the app's data dir)
instead of wiping it, and nothing ages it out — which is exactly why 3 hours later it is still there.
The server replays nothing; this is 100% client-side.

🔵 **Which do you want?** Three options, pick one and it is a small change:
1. **Back to wiping** on every exit — reverses playtest 28.
2. **Age it out** — restore the log on a quick reconnect, open clean if it is older than N minutes
   (my pick: it satisfies both rulings, and "3h later" is the case you actually complained about).
3. **Keep it** and add a "clear on login" toggle in Options.

### E. `BL-96` — the AOE column, and the Portling is gone

`RANGE, AOE, TARGET` in all 24 skill CSVs. RANGE = how far you throw it; AOE = how wide it goes off.
The radius is checked against the code now instead of living in the DESCR prose.

| # | test | expected |
|---|---|---|
| 95k | Open any skill CSV. | `LEARN, NAME, TYPE, RANGE, AOE, TARGET, …`. Elemental Wave is `0,200,enemy/aoe`; Arcane Wave `900,400,enemy/aoe`. |
| 95l | Read a **party heal** row. | `600,600` — NOT the `0,600` you sketched. The range gate really does apply to the ally you target, so 600 is what the game does. 🔵 Tell me if you meant the behaviour to change. |
| 95m | Farm a **level 40-44** camp. | No more **Rift Portling**. It carried `PDef 2.2` = your +120% and was rostered into every 40-44 camp automatically because rosters derive from the level band. |

### F. The party heals are cast on YOURSELF now (0.94.3)

*"The party heals … should be cast able without a target.. So 0/x party/AOE"*, and a full retune:
**0 range / 1000 radius** on every one of them.

| # | test | expected |
|---|---|---|
| 95n | Cast **Party Heal / Party Great Heal / Ultimate Party Heal** with **nothing selected**. | It fires. Circle on you, everyone within 1000 healed. |
| 95o | Cast one with an **ENEMY** selected. | Also fires, on you — a support skill with 0 range never takes the selected target. |
| 95p | Reuse timers. | Party Heal + Party Great Heal **6s**, Ultimate Party **3s**, Healer Party Blessing **9s**, Urgent Great Heal **5s**. |
| 95q | Cast times. | Party heals **7s**, Party Blessing **3s**, Urgent Great Heal **3s**. |
| 95r | Single-target: Heal / Great Heal **5s cast 3s reuse**, Ultimate Heal **5s cast 1s reuse**, Healer Blessing **3s cast 3s reuse**. | As listed. |
| 95s | **Ultimate Party Heal at 76+** specifically. | 7s / 3s — your `healer 4th.csv` said 5s / 2s and I overrode it on your *"let's redo … I just estimated"*. Tell me if the 4th tier was meant to keep its own faster numbers. |
| 95t | Range 1000 vs the old 600/800. | You should be able to stand noticeably further from the party and still land it. |

✅ **URGENT GREAT HEAL IS `target/aoe` AND YOU CONFIRMED IT** (2026-08-28): *"Urgent great heal is 11
targets so it's never a party one while healer PARTY blessing implies only party :) urgent heal is a
safe anyone anywhere"*. The ELEVEN is the argument — a party caps at 9, so a skill reaching 11 cannot
be describing a party. It stays `FriendlyInRadius`; Healer Party Blessing beside it stays party-only.

🔵 **`BL-98` OPENED FROM THIS**: *"prevent outside help of high-level healers to a low lvl boss
fights"*. Not built — the options are on the entry and the line-drawing is yours. 🔑 The anti-cheese
curve already exists for DAMAGE to a boss (`RaidLevelGapMult`) and was never mirrored onto support.

## 94. THE 0.94.0 BUILD — the guards, the field HP ladder, the caster fix

**Server-side only.** An installed 0.93.x APK plays every row here; the version strip will read the
client's own number, not 0.94.0.

### A. `BL-79` — the guards are posted

Eight posts. **Five city gates** — just outside each city's safe radius, on the bearing of that
city's first hunting field — and **three fields**: Ashen Barrens (Stonewatch), Sunken Hollow
(Greymarsh), Radiant Expanse (Frostmere). Each post is a **tank + an archer**.

| # | test | expected |
|---|---|---|
| 94a | Walk up to a town post as a normal (white) character. | Nothing happens. They ignore you completely — aggro is karma-keyed. |
| 94b | Try to attack one with **PvP off**. | Refused: *"… is under the town's protection. (Enable PvP to attack it.)"* |
| 94c | Enable PvP and fight a **town** guard (level 80, S+0). | Near parity — measured 105s/124s against an S+0 warrior. It should feel like fighting a player in your own gear, because it now literally is one. |
| 94d | Fight a **field** guard (level 90, S+16, War Rune). | It kills you in **16-30s**. A guard tower, not a duel. |
| 94e | Kill a guard and watch your **karma**. | Unchanged. It must NOT drop — a guard is not a way to work off a PK record. |
| 94f | Kill a guard and watch exp / drops / quest credit. | Nothing at all. |
| 94g | Kill a **town** guard, wait. | Back in **60-90s**. A **field** guard is back in **1-2s** — if you manage to kill it. |
| 94h | Get PK (red), then walk near a post. | NOW it comes for you. Melee at 400, archer at 600. |
| 94i | Get PK and walk near a post with a **party**. | Only YOU are acquired. A flagged (purple) or white member is not the watch's business. |
| 94j | **As a PK, walk into town.** | You get in and you are SAFE from other players — that is the design. |
| 94k | As a PK, try each NPC: vendor buy-back, gatekeeper, buffer, SP broker, warehouse in/out, class change, profession master, mindwriter, stat re-roll. | **All ten refuse you.** |
| 94l | As a PK, **SELL** to a vendor. | Still works — deliberately exempt, so being red is expensive rather than crippling. |

⚠ 94j-94l are the OTHER half of BL-79, and they are the point of the whole feature: killing the watch
buys a PK the SAFE ZONE and nothing else. If any of the ten NPCs serves a red character, that is a bug.

### B. `BL-78` item 1 — a field can be made heavy

The **zone** now carries an HP multiplier, on your ruling. **×1 below 40, ×2 from 40, ×3 from 61.**

| # | test | expected |
|---|---|---|
| 94m | Kill things in a **level 61+** field and read the HP bar. | ~**15,480** at level 80 — your "15k not 5". |
| 94n | Kill things in a **level 40-60** field. | ×2. Noticeably longer to clear, hitting you exactly as hard as before. |
| 94o | Kill things **below 40**. | Unchanged. The newbie stretch is deliberately untouched. |
| 94p | Fight a **field boss**. | Unchanged — a boss is exempt, so 0.89.0's 12-25 min band still holds. Check one. |
| 94q | Fight an **elite camp**. | It DOES get the ladder. Tell me if that reads wrong — it was my call, not yours. |

🔴 **THE QUESTION THIS ROW REALLY ASKS:** ×2/×3 the HP for the same reward is ×2/×3 the farm time.
Nothing was retuned to compensate, on purpose. Does the field feel *heavier* (good) or just *slower*
(bad)? That is `BL-78` item 4, the bill, and it is still yours to rule.

### C. `BL-78` item 2 — caster mobs

| # | test | expected |
|---|---|---|
| 94r | Fight a caster mob (Watcher Eye @26, Aether Wisp @58, Radiant Mage @82). | It should no longer fold to a fighter. P.Def ×0.85 not ×0.7 — and it must NOT dodge more than before: the +8 evasion was reverted on your ruling (a robe is not light armour). |
| 94s | Watcher Eye specifically. | The worst case: it stood in ×0.35 defence and now stands in ×0.68. |

### D. Carried in from 0.93.1 / 0.93.2 — never given rows

These shipped after the checklist reset and have never been played.

| # | test | expected |
|---|---|---|
| 94t | Level an 83 Lightbringer and cast **Urgent Great Heal**. | Triage: 11 slots, ordered by FRACTION of HP missing, ladder ending at 10%. ⚠ A party caps at 9, so the real span is 30→14%. |
| 94u | Open several skills and read the **TARGET** column in the CSVs against what the skill does. | `[self\|target\|party\|enemy]/[single\|aoe]` on all 1,268 rows. |

## 93. THE 0.90.0 → 0.93.0 BUILD — none of it has been played

### A. Your HP is 2-3× bigger — `BL-78` item 3 (0.91.0)

Your words: *"the hp of players seems twice if not trice as low from IG"*. They were. The cause was
`0.73.0` refitting creature **attack** up ~×1.65 against IG's current chronicle and never re-running
the player side — so **this changes no mob number at all**, only the pool it lands on.

Max HP is now a growth rate that **steps at every class change** (`g × (L+1)` per level, `g` keyed by
**discipline**), fitted to IG's own per-class tables: 0% error at 1 / 40 / 80, +7% at 20 worst case.
Your three anchors read **2414 / 1184 / 9969** against your **2380 / 1180 / 9840**.

| what your pool is multiplied by | @20 | @40 | @60 | @80 |
|---|---|---|---|---|
| tank | ×1.03 | ×1.63 | ×1.83 | ×1.92 |
| warrior | ×1.08 | ×1.77 | ×2.02 | ×2.12 |
| rogue | ×1.15 | ×1.97 | ×2.29 | ×2.42 |
| buffer | ×1.51 | ×2.54 | ×3.28 | ×3.61 |
| healer | ×1.51 | ×2.25 | ×2.78 | ×3.00 |
| nuker | ×1.38 | ×1.97 | ×2.39 | ×2.56 |

- `93a` [ ] - **Stand in front of a same-level creature and count.** Measured standing still: a **robe
  at 52 goes 9s → 21s**, a tank 73s → 132s, a rogue 27s → 58s, a champion 36s → 69s. The question this
  whole build exists to answer is whether *"a healer with 1500 hp getting hit for 300"* is finally the
  right shape — or whether the creatures now feel like paper from the other direction. ->

- `93b` [ ] - **Class-change at 40 and watch the bar jump.** Nothing is accumulated, so taking a
  discipline recomputes the whole curve on the new track: **a Warchanter visibly gains +20% HP the
  instant he class-changes.** That is deliberate — it is how IG's per-class table jump is reproduced
  without a discontinuity — but it is a thing you will see happen and should confirm you want. 🔑 The
  track is keyed by **discipline**, which is the only way Warchanter and Lightbringer can differ at all
  (they share `Archetype.Healer`), and it is what puts a buffer *"in between the nukers and rogues"*
  exactly as you asked. ->

- `93c` [ ] - 🔴 **LEVELS 1-10 GOT SMALLER, and it is the one place this could bite.** `level1Base`
  went 126 → 44, so a **level-1 tank reads 89 HP, not 186**. That is IG's own level-1 row and creature
  P.Atk at level 1 is 7, so the arithmetic says it is fine — but the early game is the one thing here
  measured only in a spreadsheet. **Roll a fresh character and play the first ten levels.** ->

- `93d` [ ] - **Interrupt should now bite you LESS often**, and that is on purpose. The formula
  (`damageTaken / casterMaxHp`) did not move; the denominator did. Your own reasoning: *"a mage with
  500hp getting hit by 100 .. is 20% base interrupt chance"* — that mage now has ~1200. If casting
  through damage now feels too easy, say so; the honest fix is the formula, not the pool. ->

- `93e` [ ] - **Heals, HoTs and HP potions did NOT change**, your ruling: *"now u just need more pots to
  heal to max, they do not touch the survavability factor"*. So a full heal is a smaller fraction of
  your bar than it used to be. That is the trade — confirm it reads as intended rather than as the
  healer getting weaker. ⚠ It is also the row that decides whether **`BL-16`** (the heal ladder) is now
  urgent. ->

### B. HP Boost, Swift, and where the buffer's ×1.2 lives (0.91.1)

⚠ **All of §93B needs the new APK** — the client builds its Learn tab locally from the compiled class
tables, so an old build simply will not show these rows.

- `93f` [ ] - **HP Boost — ten rungs, +120 climbing to +1000.** The warrior takes L1-L3 at
  **20 / 28 / 36** and L4-L10 at **43 / 49 / 55 / 62 / 66 / 70 / 74**. 🔴 **Your numbers are already
  doubled and were built as written** — *"i doubled the hp passive read as is"* — because our flats
  stack **outside** the buff multiplier where IG puts them inside. Unbuffed reads a little above IG,
  buffed lands on it. **Never scale them again.** ⚠ This is the **only** 3rd-class row a warrior has;
  Ravager/Warlord stays unauthored until you write it. ->

- `93g` [ ] - **The buffer's HP Boost is a different ladder** — rungs 1-7 at **40 / 44 / 48 / 52 / 56 /
  62 / 70**, ending at +700 where the warrior ends at +1000. ⚠ **Check the price**: every buffer rung
  carries an explicit SP override off your 3rd-class ladder — rung 1 is **36,000**, against the
  warrior's 3,400 for the same rung. Without the override he would buy it at a tenth of price. ->

- `93h` [ ] - **Armor Mastery is now P.Def + Max MP and NOTHING ELSE**, and the ×1.2 MP regen moved to
  the race masteries — your words: *"the mp regen is moved to the represented masteries per race
  (human/ork heavy, elf light)"*. So it rides **Chanter Heavy Mastery** (Human, Ork) and **Harmonist
  Light Mastery** (Elf). 🔑 The consequence worth checking in play: **a Human Warchanter in LIGHT armour
  now gets no ×1.2 at all**, where Armor Mastery used to hand it to him regardless of what he wore. The
  `BL-92` rule still holds exactly — one ×1.2 per mage. ->

- `93i` [ ] - **Swift is back on the newbie buffer** (*"add swift in the NPC buffer - i missed it
  apparently"*), reversing the playtest-28 cut. **Twelve** buffs against the cap of twenty, so a real
  buffer's groups still fit beside the full NPC set — that is the thing to confirm, not the button. ->

### C. MP POTIONS — three tiers, PvE only (0.92.0, retuned in 0.92.1)

The mana ladder is now **the healing ladder's rates at double the healing ladder's price**, your final
ruling after the first cut shipped at 20/50/100.

| item | restores | window | drink reuse | **sustained** | buy | source |
|---|---|---|---|---|---|---|
| Common Mana Potion | 20 MP/s | 15s | 30s | **10 MP/s** | **120** | Apothecary |
| Uncommon Mana Potion | 70 MP/s | 15s | 30s | **35 MP/s** | **500** | Apothecary |
| Rare Mana Potion | 150 MP/s | 15s | 30s | **75 MP/s** | — | Potion Master, craft L5 |

- `93j` [ ] - **Buy the two low tiers off the Apothecary shelf and drink them.** 🔑 **The sustained
  column is the real number** — 15s up on a 30s reuse is a 50% duty cycle, unlike the Rare healing
  potion which runs 30s on a 20s reuse and therefore never stops. Your own arithmetic is what the build
  matches: *"uncommon its 1050/15s mp"*, *"60k/hour for uncommon is ok"*. ->

- `93k` [ ] - 🔴 **MANA POTIONS DO NOT DROP — ANYWHERE.** Two sources and no faucet, which is exactly
  what the double price buys: *"common/uncommon healing potions are dropped so u dont spend there …
  u need to buy mp pots"*. Confirm nothing on any drop table hands you one. ->

- `93l` [ ] - **PvE only, and the gate is on the DRINK, never on the effect.** Your spec: *"having mp
  pot On and then entering pvp it works until stop but the next one is forbidden"*. So flag yourself
  mid-potion — it must tick out its full 15 seconds — and then try to drink again, which must be
  refused. ⚠ **An innocent VICTIM can still drink**: the flag follows what *you* did, not what was done
  to you. Say the word if being *hit* by a player should also close the gate. ->

- `93m` [ ] - **A boss fight is allowed and that is a ruling, not an oversight** — the gate is the PvP
  flag and a boss is PvE. *"in a party as alt char helping main char to heal ocasionally its still
  consider farm so its ok"*. Confirm you still want it that way once you have actually drunk one in a
  20-minute boss fight. ->

- `93n` [ ] - **Auto-hunt's MP slider works now.** The client has been sending `MpPotionPct` all along
  against a `=> null` stub; it is filled in. ⚠ And the HP line **can no longer drink your mana
  potions** to top up a bar they cannot touch — that was a real bug the split fixed on the way past. ->

### D. STACK CAPS — a row has a bottom now (0.93.0)

Your ruling, verbatim: *"buff scrolls 9, buff pots 99, atri and enchant scrolls 99, buff and other
boxes 99, hp/mp pots 999 (max shop buy = 1 stack), mats 9999, quest items uncapped so the 10,100,1000
etc item to make new stack — stacks work the same everywhere warehouses, trades, etc"*.

Nothing is destroyed and nothing is refused for being over a cap: **the (cap+1)-th item opens a new
row**, and a container still refuses only when it runs out of **rows**. The caps are **derived from the
item's category and authored nowhere**, so retuning one is a single edit — your condition when you
ordered it. `dotnet run --project tools/BalanceMatrix -- --stacks` prints the whole table from the
catalog.

- `93o` [ ] - 🔑 **BUFF SCROLLS STACK TO 9 — this is the only cap here a player will ever feel, and it
  is the one you meant to be felt.** 17 blessings at an hour each means a fully-buffed player burns 17
  scrolls an hour and **his row count stays flat while he farms**, unlike potions. At 9 a stack an hour
  of full buffs is ~2 rows and a long session is a visible pile. Your own reasoning: *"having 99 of each
  is indefenetily buffed … while having 10 is 10h of buffs"*. **Play a session with them and say whether
  9 is friction or annoyance** — it is one number in one file. ->

- `93p` [ ] - **The other caps: 99 · 999 · 9,999 · uncapped.** Buff potions, enchant + attribute
  scrolls, boxes and blueprints at 99; HP/MP potions at 999; materials at 9,999; quest items uncapped
  (a gathering contract hands out a token per kill — a cap there would be a bug wearing a mechanic's
  clothes). Buy 1,000 potions and confirm you get two rows, not a refusal. ->

- `93q` [ ] - **A shop sells at most ONE stack per purchase** — your *"max shop buy = 1 stack"*. It
  replaces a hard-coded clamp of 999 for everything, so mana potions still buy 999 at a time and buff
  scrolls buy 9. 🔑 It also deletes the partial-order question: a single stack either fits or it does
  not, so **a purchase can never half-complete and take your gold with it**. ->

- `93r` [ ] - **Boxes stack now** (they never did, apart from blueprints). ⚠ **The row that matters:
  two boxes must NOT merge when one has picks left.** Merging is by **identity** — enchant, expiry,
  picks remaining, bound/renamed/price overrides all have to match — so a fresh Blessing Box cannot
  absorb one you have half-opened. Half-open a selection box, get another, and confirm they sit as two
  rows. ⚠ Runes and timed items deliberately do not stack at all: one row per acquisition is the only
  way two clocks stay two clocks. ->

- `93s` [ ] - **The account warehouse fee follows ROWS.** 10k buys a slot, so a deposit that needs three
  slots costs **30k** and says so before it takes it. Nothing changed about the rule — it simply never
  had to open more than one row before. ->

- `93t` [ ] - **Your existing characters migrate on LOGIN.** Any row saved over its new cap is split
  into legal ones the moment it loads — bag, warehouse and account bank. It only ever splits, so
  nothing is lost, and a stack with nowhere to spill stays as one oversized row rather than being
  deleted. ⚠ This is why 0.93.0 needs **no `game.db` reset**. If you `/give` yourself 10,000 of
  something, relog and watch it become four rows. ->

- `93u` [ ] - **Trade and warehouse under a full bag** — this is `37d`/`37e` finally having a reason to
  be tested. The trade room-check is now a **simulation** of the real placement rather than a per-def
  yes/no, so the check and the move cannot disagree. A shortfall must still abort the whole trade with
  nothing moved. ->

### E. The MP economy, measured — read, don't test (0.91.2)

`dotnet run --project tools/BalanceMatrix -- --mpdrain` answers the question you asked in flat MP/s,
for all three races, 20 → 80. Race cuts **twice in the same direction**: WIT drives cast speed
(×1.63 per +10) so a faster caster empties the bar sooner, and SPT drives regen so he also refills it
slower. At 80 an **elf healer is 14.7 MP/s under water where the demon is 5.6** — same spell, same rung.

- `93v` [ ] - 🔴 **A TOGGLE WAS CHARGING RUNG 1's UPKEEP AT EVERY RUNG, and it is fixed.** The tick loop
  read one number per skill, so the Warchanter's Reinforcement — authored as a 13-rung ladder from
  **12 MP/s up to 30** — really took **12** at rung 13. Both stances together were **15 MP/s charged
  against 45 authored, a 3× discount** on the one class whose mana is supposed to be a decision.
  ⚠ **So your buffer's toggles now cost roughly triple what they did last time you played him.** That
  is the reading to take: with MP potions existing and the discount gone, is a buffer's mana a decision
  or a wall? ->

- `93w` [ ] - **Read the numbers, don't test them:** a caster's deficit is **0-15 MP/s at every level**
  and below ~40 there is none at all. **The buffer is the real customer** — two stances plus his sound
  skill run 33 MP/s at 40 and **81 at 80** against 17.6 of regen, a full bar every ~55 seconds at every
  level from 40 up. Against `--mpnpc`'s measured deficits at 74 with a full NPC buff pack (healer −25.6,
  nuker −14.8, buffer toggles −32.0), **Uncommon's 35 sustained covers the healer AND the buffer** and
  Common's 10 covers the nuker. ->

### F. `BL-93` step 1 — deliberately invisible (0.90.0)

- `93x` [ ] - **Nothing looks different, and that is correct.** The engine half of the model pass is
  complete; the world still renders flat coloured spheres because **there is no art in the repo**. The
  wire now carries what a creature **is** (`MobCategory` × `MobRole`, the taxonomy every template
  already declares) and never what it looks like — the client decides that. One
  `Resources/Models/humanoid.prefab` would give every player, NPC and humanoid mob in the game a body
  through the fallback chain. ⚠ **The other half is one Unity Editor session** — import a rigged model,
  set its Rig to **Humanoid** (never Generic), save it at that path. Steps are in
  `docs/guides/UnityClient.md`. ->

- `93y` [ ] - **"3D models: off" is in Settings with the other look options**, persisted, and the OFF
  position is the exact client that shipped before this change. Confirm it is there and that toggling
  it costs nothing today. ->

---

## 92. THE 0.89.0 BOSS REWORK — `BL-13`, `BL-81`, `BL-83`

Everything here is measured, not derived (`dotnet run --project tools/BalanceMatrix`), and the whole
point is whether the measurement matches what it FEELS like. Take a party if you can; the numbers below
assume one. ⚠ **Read this section knowing §93A moved your pool** — the tank-survival rows below were
measured *before* the HP rebuild, so they are now conservative.

### A. A boss is 10 to 30 minutes now — is it?

Measured with a tank + healer + 3 DDs in best-for-tier gear, **at the four levels a boss actually
spawns at**: **18 min at 44** (Grave Lich, Hollow Crypt) · **23 at 60** (the world boss) · **23 at 65**
(Dread Knight, Sunless Warrens) · **21 at 90** (Disciple of the Dawn). The 44 is what moved most — it
was about 6 minutes.

- `92a` [ ] - **The Hollow Crypt boss (Grave Lich, 44).** The biggest change in the build: its HP went
  up ~3×. Does it now read as a set-piece rather than a fat elite? ->
- `92b` [ ] - **A high-level boss (65+).** Should feel about as long as it did, because the top of the
  game was already inside your band. If it feels *longer*, the defence below is why. ->
- `92c` [ ] - **A boss now has real DEFENCE (×2 P.Def and M.Def) — it had NONE before.** Your hits on it
  should be visibly smaller than on an elite of the same level, which is the thing that was missing
  when a boss read as a sponge. ->
- `92d` [ ] - **EXP from a boss went up with the time it takes**, automatically — the Grave Lich now
  pays about **half a level per head in a nine-man** for a 20-minute fight (it was ~a sixth of that).
  Nothing is capping it: the sanity rail only bites below level ~37 and nothing spawns there. Too rich,
  about right, or too thin? ->

### B. Not one-shotting, but a tank can feel it

🔴 **Boss P.Atk came DOWN from ×10 to ×4 and that is a number of yours I moved** — the reasoning is in
the 0.89.0 entry of `CHANGELOG.md`, and the short version is that at ×10 a boss killed a robe with an
ordinary swing at every level from 40 up, and put 752 dps through a shielded Knight while the best
heal in the game sustains 391. **This is the number most likely to be wrong, so it is the one to
watch.**

- `92e` [ ] - **Tank a boss.** Unhealed you should live **19-33 seconds** — ⚠ that was measured before
  §93A, so expect closer to 35-60 now. Healed by one Lightbringer you should hold, but not comfortably. ->
- `92f` [ ] - **Does it still feel dangerous?** A basic swing was 6-9% of a tank's bar at ×4 (it was
  15-22% at ×10), and §93A has roughly halved that again. If a boss reads as harmless, the ×4 goes back
  up and the heal ladder (`BL-16`) is the other half of the answer. ->
- `92g` [ ] - **Stand a robe in front of one.** It should survive a basic attack and should still be
  deleted by the telegraphed **Devastating Slam** if it stands in the 250 radius — that one is a
  positioning mistake, not a balance failure. ->

### C. Debuff immunity — `BL-81`

- `92h` [ ] - **God mode: everything is RESISTED, nothing is refused.** Turn god mode on, have someone
  (or a mob) stun/slow/curse you: the cast must go through, cost its MP, start its cooldown and report
  **Resisted**. That is deliberate — a refused cast tells you nothing about the skill you are debugging. ->
- `92i` [ ] - **God mode also resists a dispel and a knockback.** ->
- `92j` [ ] - **A boss: control is refused, attrition still lands.** Stun/root/fear/slow must all
  resist; **DoTs, stat-downs on p/m Atk and Def, and regen suppression must still work**. A knockback
  on a boss now resists too. ->

### D. Taunt is manual — `BL-83`

- `92k` [ ] - **Arm a taunt on the auto bar and save.** It must **NOT** fire, and the moment you save
  you should be told: *"Auto-hunt cannot cast Taunt — it is for you to press yourself."* ->
- `92l` [ ] - **`Lure` too** (the rogue pull) — same rule, every threat skill. ->
- `92m` [ ] - **Everything else on the chain still fires** — heals, mana restores, buffs, debuffs and
  attacks. The taunt rung was removed from the middle of that ladder, so this is the regression to
  check. ->

---

## 90. ENGINE AND ECONOMY — never played, only measured

What is left of the 0.77.0 pass after the rows you answered were archived. Nothing here has a client
tell; it shows up as numbers feeling different.

- `90a` [ ] - **THE LIGHTBRINGER, 40-74** (0.74.0, off your finished `healer 3rd.csv`). The first
  authored 3rd-class kit in the project. **Race splits it twice**, once on the heal and once on the
  debuff: **Human** Quick Great Heal + Gravity · **Elf** Healer Blessing + Bind · **Demon** the Healing
  Totem + Armor Break. Everything in the file is built to **74**; nothing above it exists. 🔑 What is
  worth your attention is not whether the skills fire but whether the kit **plays as a healer** —
  heal-per-cast against a fight's damage, MP against a fight's length. ⚠ Both halves of that question
  moved this build: §93A doubled the pool a heal fills, and §93C gave him mana potions. ->

- `90f` [ ] - **Combo Rush — the one ladder in the game that goes BACKWARDS, deliberately.** Rungs give
  AS 5/7.5/10/10/15/20 and cast 2.5/5/7.5/**5**/10/15; rung 3 → 4 loses 2.5% cast on purpose, your call:
  *"even if some other buffer procs lvl 3 buff u still get your effect over"*. One family, one key, index
  as rank — so your own rung 4-6 always outranks a party-mate's rung 1-3. ->

- `90o` [ ] - **`BL-78`: THE CREATURES STOP BEING PAPER (0.73.0).** Your *"mobs feel easy"* got its two
  halves built: **P.Def, M.Def, P.Atk and M.Atk are now four smooth `a·(L+shift)^k` curves**, refitted
  off **2,831 IG creatures** in the chronicle you actually play. ⚠ **HP DID NOT MOVE** — that is your
  own park (*authored later, with instances*), so an 80 mob is still ~5k and not the 15k you named.
  ⚠ **And item 3 of `BL-78` has now landed on the OTHER side** (§93A): the same 0.73.0 attack refit is
  what the player HP rebuild was paying off. So the question is narrower than it was — **with mob
  defence and attack fixed, mob HP unchanged, and your own pool 2-3× bigger, where does it sit now?** ->

- `90p` [ ] - **A skill has ONE MP price, and the engine splits it 20/80.** One `MP` column, the number
  you are quoted, and the gate demands **all** of it before the cast starts — you can never begin a cast
  you cannot finish. An interrupt costs you the 20% and nothing more. ->

- `90q` [ ] - **Mana Ray drains a SHARE OF THE TARGET'S MP POOL**, not a magic-damage number — ~14.5% a
  cast, seven casts to empty anyone. 🔴 You brought IG's own drain formula, it measured within ±8%, and
  you ruled *"leave it as is"* — this row is only asking whether it FEELS right in a fight. ->

- `90r` [ ] - **Magic crit: rate cap 50% → 40%, damage ×2, and the rate ladder gets headroom** (8/16/32%).
  The cut buys room under the cap for the 40+ rungs that now exist. ->

- `90t` [ ] - **Rates: ×N now means ×N of everything, and never clamps.** Above 100% a drop pays **COPIES**
  (250% = two plus a 50% roll for a third), so the guaranteed-group exemption came off — a 100% mats
  group at ×30 fires 30 weighted picks in your authored proportions. 🔴 **Bug fixed in passing: quest gold
  and quest SP were paid RAW** — on a ×30 server every quest paid ×1. ⚠ **`DropAmount` is not a rate**;
  setting it too squares the multiplier, so it left the Debug panel for `/droprate amount <x>`. ->

- `90u` [ ] - **Buff prices are per-rung, verbatim from your sheet** (`RungCost[]`). ⚠ The irregular
  spacing is correct — it is what you authored, not a smoothing error. ->

- `90v` [ ] - **The debuff contest gets a LEVEL, and mobs get stats in human ranges.** 🔑 The level term
  scales the **defender's** stat. 🔴 **Known and unfixed**: the attacker's level is read as the RUNG's
  learn level, so all five CC skills expire out of usefulness — the fix is a CSV ladder and it is owed
  from you. ->

- `90w` [ ] - **Renames — display names only, every id untouched.** `Haste` → **Fury** (the consumables
  followed: Fury Potion, Scroll of Fury) · `Provoke` → **Taunt**, which is your name and your four-rung
  ladder at 24/28/32/36 · **Resolve caps at +54** (the 60 rung is commented out, not deleted — *"no1 is
  leatrning it atm"*) · Alacrity gained **rung 3 at 48**, the cast-speed buff missing from the healer. ->

---

## 89. THE THREE UI CHANGES — `BL-88`, built in 0.89.0

Built after three passes without an id, and still never looked at.

- `89a` [ ] - **The target frame's title bar is now the NAME only.** The worn title moved down beside
  the level: `Mob: 44, Field Boss, Aggressive`. Nothing should overflow the frame any more, on any
  target — check a titled NPC and an elite as well as a plain mob. ->
- `89b` [ ] - **Shrink the chat window to its narrowest.** The six tab buttons (All/Local/World/PM/
  System/Combat) must all still sit inside the frame — that row is 488px wide now against a 520
  minimum. ->
- `89c` [ ] - **Admin → Equip.** The filter chips are shorter again, and there is a **splitter line**
  under the tier row (the `[S 80]` one) separating the filters from the gear list. ->

---

## 85. THE 40+ FILES — author them, there is nothing to test

- `85n` [ ] - 🔑 **THREE OF THE TEN 40+ FILES ARE NOW AUTHORED AND BUILT** — `healer 3rd.csv` (0.74.0),
  `buffer 3rd.csv` (0.76.0) and `nuker 3rd.csv` (0.87.0), plus the Lightbringer's **4th**-class kit and
  the eighteen Sigils (0.85.0). **The rest are still empty**: `tank` · `warrior` · `war_aoe` · `dual` ·
  `archer`, each `3rd` and `4th`, plus the `4th` files for the ones you have finished. They are seeded
  in `docs/data/classes_skills_csv/` holding **exactly what the game already registers above 40** —
  nothing is invented — so you start by editing, not from an empty sheet. ⚠ **A warrior's only 3rd-class
  row in the game today is HP Boost** (§93B), and a tank's is Shield Mastery. **Nothing to test —
  author them.** ->

---

## 81. THE TWO REFLECTS — never reached in playtest 23, 24, 25, 26, 27 OR 28

⚠ The reflect-FLAG bug is confirmed fixed on all three paths. These two are the other two paths and
have still never been played. Check the flag behaviour in the same sitting.

- `81b` [ ] - **`Deflection` — physical-skill reflect, warrior.** *"default warrior @40 → 0.15 chance ×1
  reflected; @76 → 0.3 chance ×1 reflected."* Your numbers verbatim: the fraction stays **×1.0** at both
  rungs and only the **chance** moves. A landed physical skill rolls the victim's chance; on a hit the full
  damage goes back at the caster, **who can die to it**. Kept separate from the armour sets' `MeleeReflect`
  (5%, basic attacks only) — no blow is ever taxed by both, and two Deflection warriors terminate after
  one bounce. ->

- `81c` [ ] - **`Backlash` — debuff reflect, tank, 30%.** *"tanks get 30% chance to reflect a debuff →
  u cast on tank he reflects u get the debuff."* Rolled **before** the land contest on both debuff
  paths, because a bounce is not a resist: a tank who throws your stun back was never tested against
  it. The caster gets the effect with no resist roll of their own and no second bounce. ->

---

## CARRIED FORWARD — never reached in any playtest, needs a deliberate setup

- `0a` [ ] - **Nuker vs champion, unbuffed.** Half of this closed itself in playtest 23: the **mage** can
  now farm solo. The **champion** half is untouched — *"they both have hard time to farm without
  buffs"* — and is `BL-72`. ⚠ **Two blockers have now cleared**: `BL-78`'s defence and attack halves
  landed (`90o`), and the player HP rebuild landed (§93A). A reading taken this pass is a reading that
  stands. Do it in the same sitting as an auto-farm run. ->

- `37d` [ ] - A trade **shortfall aborts the whole trade** with nothing moved. ⚠ See `93u` — the trade
  room-check was rewritten this build. ->

- `37e` [ ] - **Full-bag judging** on a trade: merges into an existing stack succeed, brand-new items
  are refused. ⚠ Now interacts with the stack caps: a stack that has hit 9 or 99 needs a **new row**,
  so "merges succeed" is no longer unconditional. ->

---

## 0. ANSWERS I OWE YOU — read, don't test

### ✅ Closed since the last update

- ✅ ~~**`BL-78` item 3, the player HP half**~~ — **built in 0.91.0** (§93A). The cause was named and it
  was ours: 0.73.0 raised creature attack ~×1.65 and the player side was never re-run. **No mob number
  moved.** ⚠ You sent two IG tables **3.46× apart** and ruled the per-class one; the CON curve is IG's,
  normalised at 20, and **moves with the base table** — changing one alone rescales every pool.
- ✅ ~~**"why don't we have mp consumption decrease"**~~ — we do, and the whole chain already existed
  (`MpCostFactor` / `EffectiveMpCost` / `MpCostPct`). It was **pure authoring**, which is why HP Boost
  and the MP potions could be built the same day.
- ✅ ~~**"why do you tell me no one has an mp problem"**~~ — **I was wrong and you were right.** The
  model under-read **cast speed** because it carried no buff stack; with Alacrity it matches your live
  client exactly (2.79s cycle, 10.77 MP/s drain against 8.7 regen on your demon 43 Bloodmender). That
  withdrawal is what `--mpdrain` (§93E) and the potions exist because of.
- ✅ ~~**Max stack counts**~~ — your numbers, built as ruled (§93D). The one thing I would still flag is
  `93o`: buff scrolls at 9 is the only cap a player meets.

### 🔴 Still yours to rule

- 🔴 **`BL-94` — the fizzle floor.** *"shouldn't hit at all on the floor"*, carried out of playtest 28
  unbuilt and now a backlog entry rather than a checklist row. It is one line and it changes how it
  feels to fight above your level, so it wants your word: **flat 0 on a fizzle, or 0 only once the fail
  chance is at its ceiling** (the second is what today's `damage / 3` approximates).

- 🔴 **The CC ladder is yours to author** — `90v`'s red half. The attacker's level in the debuff contest
  is the rung's learn level, so every CC skill expires out of usefulness; a rung ladder in the CSVs is
  the fix, and nine CC skills are today learnable by nobody at all.

- 🔴 **`BL-22` salvage: the S row cannot be moved by this feature at all.** Your budget was *"10~20%
  decrease in time"*; the early rungs got exactly that (E −3% · D −10% · C −18%) and **A and S got −0%**.
  The cause is your own *"rarity for mats rarity"* mapping: salvage pays the rarity of gear that
  **drops**, and a normal mob and an **elite both cap at Epic** — only a boss (0.09 kills/h) drops
  Legendary. The A and S recipes bind on **Legendary Ingot**. `M13` in BalanceMatrix prints all three.
  ⏸ Parked with the rest of crafting, along with the **603h craft time you accepted**.

- ⚠ **The buff-vs-heal threat ratio, `BL-16`.** You sized the buff against a ~1500-power quick heal at
  70; the cleric's ladder stopped at skill level **4** (learned at 35, power **301**). The Lightbringer's
  40-74 rungs now exist, so the heal side finally has numbers above 35 — and §93A has just made every
  heal a **smaller fraction of a bigger bar**. `93e` and `90a` are where you would feel it, and together
  they are what decides whether `BL-16` is urgent.

- ⚠ **Numbers that are mine, not yours** — each flagged in the source: the top rung of **Madness**; the
  Ultimate Scroll of Resurrection's **15,000 Value**; the three subclass-swap clauses; the **0.25 respawn
  exponent**, which your `85j` park leaves standing as mine; and now the **Rare Mana Potion's craft
  rung** (Potion Master L5, inferred from the Rare healing potion sitting there).

- **The heavy sets' shield clauses are still unchanged PERCENTAGES** (`shield.p.def x1.10 / x1.25 /
  x1.30`). Left alone a sixth time on purpose: the block channel moved once and Shield Mastery moved
  once, and moving these in the same pass would make neither reading attributable.

- **`/give`'s `sellPrice` argument, your `[?]`.** `-1` → *unsellable* · `0`, `-` or omitted → use the
  catalog's price · any positive number → that exact price (`k`/`m`/`b` and `1_000_000` both parse).
  Every argument after the item id follows the same rule: `-` is always *no opinion*.

---

## KNOWN OPEN — not defects, don't spend the pass on them

**Everything you asked to be BUILT lives in [docs/Backlog.md](../Backlog.md) with a permanent id.**

- **`BL-02` the 40+ kits** — **three of ten done** (`85n`), plus one 4th-class kit. The authoring format
  is settled in **[docs/design/Disciplines.md](../design/Disciplines.md)**: you author **by DISCIPLINE
  with a trailing RACE column**, **10 CSVs not 30**. Still the single biggest unlock, and your `85j` EXP
  park depends on it landing.
- **`BL-84` rename every skill id to match its name** — unblocked the day the healer landed, and your
  standing order puts it **after** the healer. Where an authored row hit an existing skill's exact slot
  the id was **reused rather than retired**, which is right for the data and wrong for reading code.
- **`BL-93` the in-game visuals** — step 1 is in this build and is invisible by design (§93F). The
  direction is agreed: **budget per FAMILY not per mob**, one humanoid rig, terrain cosmetic and derived
  from the zone circles, downloads via Addressables later. 🔴 **The camera is deferred** — your call, and
  I will not re-propose it.
- **`BL-79` / `BL-80`** — the hand-placed player-stat mobs (PK guards over-enchanted, fortress NPCs
  under-geared). Content, not fixes. Field mobs stay on the curve — your ruling.
- **`BL-05` / `BL-22` / `BL-50` crafting** — ⏸ parked by you; it wants its own ×100-rate playtest.
- **`BL-73` mob social clans** — off by one switch at your instruction, back on when the world map
  spreads the camps out. ⚠ `BL-80`'s garrison presumes clans, so it is one of that entry's prerequisites.
- **`BL-74` the game launcher** — still not treating the app as a game; research owed.
- **`BL-76` boss skill gems** — recorded, not built; five shape questions on the entry. ⚠ Read it beside
  `BL-13` — a 20-minute boss is a different drop proposition.
- **Instances** — you are holding (`BL-48`); the dungeons were the cheap half and are built. ⚠ This is
  also what `BL-78`'s mob-HP half is parked behind.
- **Two playtest-20 bugs closed on a reading of the code, never re-tested**: Frost Bind stripping a
  dummy's/elite's HP multiplier (`BL-63`) and the target lost during a physical cast (`BL-64`).
