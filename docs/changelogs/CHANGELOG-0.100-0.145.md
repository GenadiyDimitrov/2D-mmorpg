# Changelog, 0.100.0 to 0.145.x, 2026-08-28 to 2026-09-14 (archived volume)

Newest first, verbatim. The live file is [../CHANGELOG.md](../CHANGELOG.md), which lists every volume.

## 2026-09-14 — 0.145.1: a healer's bundle retires its singles, and two bundles ladder to 90

🔴 **NEW APK REQUIRED** — a class-skill-TABLE change. No protocol change and no `game.db` delete.

> *"i want grouped buffs once learned to remove the single ones (example: once elf learns the "arcane
> insight" -> it removes/replaces his "Force"+"Insight") .. like the buffer does"*

**This reverses 0.145.0.** That version deliberately left `Replaces` off the nine twins so a healer
would keep his cheap singles. Your ruling is the opposite: the bundle is the upgrade, the same as the
buffer's group. The twins now inherit their group's REPLACES list, which is the list you wrote into
`healer 3rd.csv`. The buffer's groups already had it, so nothing changed for him.

**The 4th tier splits two singles by race, per your `healer 4th.csv` RACE cells:**

| Race | Mana Blessing 76-90 | Fortitude 76-90 | Bundle that ladders 76-90 instead |
|---|---|---|---|
| Human | ✅ | ❌ (retired by Arcane and Feral Protection) | **Arcane and Feral Protection**, 215 → 285 MP |
| Elf | ❌ (retired by Soul Reinforcement) | ✅ | **Soul Reinforcement**, 330 → 400 MP |
| Demon | ✅ | ✅ | none |

Both bundle ladders use the buffer group's rungs 2-9 (same payload, SP and gold) at your healer MP.
**Still the sum of the parts**, with the laddered child at its own 4th-tier rung: Clarity 85 +
Fortitude 130 → 200, and Ward 80 + Soul 120 + Mana Blessing 130 → 200. (My first pass had your
earlier 290/335 figures; you corrected the file, and `BL-236` closed the same day.)

In passing, a stale comment claiming Fortitude's 4th-tier rungs ran "43% → 65%" now says 23% → 35%.

**Verified:** `SkillCsvSeed --check` passes both healer files. The checker merges all three races, so
I read the race split directly from `ClassSkills.Cumulative`: all three races match the table above,
and both twins carry nine rungs with your MP.

## 2026-09-14 — 0.145.0: the bundled blessings are the HEALER's, and they cost what their parts cost

🔴 **NEW APK REQUIRED** — a class-skill-TABLE change. No protocol bump, no `game.db` delete.

**0.144.0 put the nine single-target twins on the wrong class.** Your correction:

> *"No no ... Those single buffs to be given to healers not buffers .... And mp should be decreased to
> the sum of buffs it gives .. Healer 3rd (elf) learns "Arcane Insight" @70 and it costs 200(120+80)Mp"*

Both halves are in. The **Warchanter learns none of them** — he keeps the party groups, which is the
whole split between the two classes. The **Lightbringer** learns three each, by your lanes:

| Race | Lane | Learns | Level | MP | = |
|---|---|---|---|---|---|
| **Demon** | attack | Wind Grace | 52 | **113** | Swift 33 + Agility 80 |
| | | Feral Precision | 56 | **250** | Focus 80 + Ferocity 85 + Aim 85 |
| | | Feral Bloodlust | 72 | **265** | Might 60 + Fury 80 + Vampirism 125 |
| **Elf** | magic | Arcane Serenity | 68 | **275** | Alacrity 75 + Resolve 115 + Serenity 85 |
| | | **Arcane Insight** | **70** | **200** | **Insight 120 + Force 80** — your example |
| | | Soul Reinforcement | 72 | **325** | Ward 80 + Soul 120 + Mana Blessing 125 |
| **Human** | defence | Body Reinforcement | 70 | **277** | Body 120 + Bulwark 72 + Vigor 85 |
| | | Shield Reinforcement | 72 | **245** | Shield Blessing 120 + Shield Hardening 125 |
| | | Arcane and Feral Protection | 72 | **210** | Clarity 85 + Fortitude 125 |

**The MP rule is now the thing that makes the two versions different.** A group's price is your own
*"each max lvl buff of the group MP + the group learned lvl MP cost"* — so Σ(children) is exactly the
group minus the band premium. The healer pays what his three singles would cost and saves the casts;
the Warchanter pays the premium on top, and gets the whole party. Nothing else separates them.

**The learn level is where the last child ladder maxes in the *healer's own* table** — which is what
puts Arcane Insight at **70** (Force 52, Insight 70) where the Warchanter's group sits at 72. Any
earlier and a twin would hand out a rung its caster has not bought. SP is the healer file's own top
band at that level (74k / 81k / 320k / 390k / 650k).

🔑 **A twin carries no `Replaces`, unlike the group it copies.** The group retires its singles because a
party-wide version of all three is strictly better; for a healer that would be a straight loss — he
would stop being able to learn Force, Insight, Alacrity, Resolve, Ward, Soul, Mana Blessing, and would
have to pay 200-325 MP to hand out one blessing. The covering still stops any double-dip: the twin's
children cover those families at group rank, so a single cast over it is refused on the target.

✅ **`BL-235` is closed by this and needed no ruling** — nothing is priced twice because nothing is
bought twice. The healer's twins are one rung; the 76-90 ladder stays the Warchanter's alone, and
above 76 his laddered groups simply outrank the healer's version, which is correct.

⚠ Ids are `holy_*_single` now, and the CSV rows moved with them: `buffer 3rd.csv` and `buffer 4th.csv`
are back to what they were, and `healer 3rd.csv` carries all nine (+18 rows, each with the MP
arithmetic in its comment cell). `SkillCsvSeed --check` is clean.

## 2026-09-14 — 0.144.0: the buffer's groups get single-target twins, and a wand stops out-nuking a staff

⚠ **Section 1 below was corrected the same day — see 0.145.0.** The twins went to the Warchanter and
kept the group's MP; they are the Lightbringer's now, and priced at the sum of their parts. Section 2
(Mage Shield Mastery) stands as written.

🔴 **NEW APK REQUIRED** — a class-skill-TABLE change (the client builds its Learn tab locally from the
compiled `ClassSkills`). No protocol bump, no `game.db` delete.

### 1. Nine single-target twins of the Warchanter's group buffs, three per race

> *"I want buffers to learn few grouped buffs (but single target -> work the same as buffers just a
> single target one) … Names can be the same just casting to be target/single as other healers buffs
> … They replace each other with the buffers party equivalents (like (war) great bulwark/might)"*

Built exactly that. Each of the nine groups now has a **single-target twin**: same name, same MP, same
SP, same payload, same 76-90 ladder where there is one. **The only difference is the target.** Your
lanes, one race each:

| Race | Lane | Twins learned |
|---|---|---|
| **Elf** | magic | Arcane Serenity (70), Arcane Insight (72), Soul Reinforcement (74) |
| **Human** | defence | Body Reinforcement (72), Shield Reinforcement (74), Arcane and Feral Protection (74) |
| **Demon** | attack | Wind Grace (56), Feral Precision (58), Feral Bloodlust (74) |

**Why they are worth owning when the party version is the same buff:** `AlliesInRadius` — the mode
every group cast uses — **never leaves the party**. So a Warchanter could not bless one ungrouped
ally at all, which is most of what a buffer is actually asked to do. The twin is his door to that.

**"They replace each other" needed no new machinery.** The pair share the group's buff key and, both
being groups, land at the same `GroupRank(level)`; at equal rank the engine keeps the LONGER remaining
time, so a fresh 20-minute cast always wins. That is the same swap **Great Might / War Might** already
do — the pair you named.

Each twin is **built from its group def** (`WcSingleTwin`) rather than authored a second time, so the
two can never drift apart. One consequence worth stating: the two groups that ladder into 76-90 (Soul
Reinforcement, Arcane and Feral Protection) have their twins laddering with them, level for level —
they have to, or the twin would stop being able to replace the party version at rung 2 and would go on
handing out a level-74 blessing at 90. **That ladder is currently priced twice** (once per skill) —
see **`BL-235`**, the one thing here I would rather you ruled on than I guessed.

⚠ One wording note: the CSV cells say **`party/single`**, not `target/single`. That is your own column
grammar for a single-target *buff* (*"a recharge should be party/single as buff but the heal is
target/single"*) and it is what every other single-target buff row in these files says. The mechanic is
the one you described — it is `SelfOrTarget`, and the engine lets a clean caster support anyone
friendly, party or not.

### 2. Mage Shield Mastery loses its +5% M.Atk

> *"on nukers in their shield mastery remove the % matk increase … they are not suppose to have more
> matk with wand then Battlestaff"*

Removed. A shield is only ever worn with a **one-handed** weapon, so the M.Atk half was paying the
nuker to give up his two-hander: wand + shield came out ahead of the battle staff the class is built
around, and the weapon is supposed to be the decision. The other three halves stay (−10% MP cost,
+10% MP regen, +100 P.Def, and the shield still never blocks) — those are survivability and economy,
not power.

Both sides of the CSV contract moved with the code: `buffer 3rd.csv` (+9 rows), `buffer 4th.csv`
(+16 rows) and the `nuker 4th.csv` row. `SkillCsvSeed --check` is clean on all of them.

## 2026-09-13 — 0.143.0: heal power is a FLAT-heal stat, and the buffer gets a triage heal

🔴 **NEW APK REQUIRED** — a class-skill-TABLE change (the client builds its Learn tab locally from the
compiled `ClassSkills`). No protocol bump, no `game.db` delete.

### 1. Every healing stat now touches ONLY flat heals

> *"Can we make heling power and healing amount and heling increase/receive or whatever heling power
> ups we have to affect only flat heals … Now healing power with urgent great heal is a bit to much."*

You are right, and the reason it was "a bit too much" is arithmetic, not tuning. A pure %-of-max-HP
heal authors `Power: 0`, and the flat half was computed anyway:

```
was:  flat = (HealPowerFlat + 0) x HealPowerMod        <- the whole sheet, on a skill with no power
now:  flat = 0 when the skill authors no power
```

So Healer's Power at its top rung (+2000, ×1.10 with the shield passive) was adding **2,200 HP per
target** to a skill whose entire design is *"the size comes from the TARGET'S own pool"* — and Urgent
Great Heal pays eleven targets. All four channels are now gated the same way: your `HealPowerFlat`
and `HealPowerMod`, and the target's `HealReceivedFlat` and `HealReceivedMod` (the *Mod* already only
touched this half; the *Flat* did not).

📐 `dotnet run --project tools/BalanceMatrix -- --healpower 90 epic` — level 90, epic gear, on a
20,917-HP Bulwark, Healer's Power up. The "was" column is the OLD arithmetic recomputed, not remembered:

| skill | power | share | slots | one target, was → now | whole cast, was → now |
|---|---|---|---|---|---|
| Urgent Great Heal | 0 | 30% | 11 | 8,475 → **6,275** | 70,213 → **46,013** |
| Life Restoration | 0 | 100% | 1 | 23,117 → **20,917** | — |
| Great Heal | 1400 | — | 1 | 3,740 → **3,740** | unchanged |
| Ultimate Party Heal | 2000 | — | 1 | 4,400 → **4,400** | unchanged |

**Not one flat heal moved.** The cut is −24,200 on a single Urgent Great Heal and −2,200 on a
Restoration, and nothing else in the game changed.

⚠ A skill carrying BOTH halves keeps them independent — the flat half is fully modified, the % half
is not. That was always the design of the % channel (it is the half an anti-heal ultimate cannot
touch); heal POWER is now on the same side of the line as heal REDUCTION.

### 2. Urgent Lesser Heal — the buffer's own triage button, at 83

> *"I would like buffers to get at same lvl as healers get the urgent great heal … Buffers to get
> urgent lesser heal … Half mp cost (250) same cd same cast 3 skill stones same aoe range but only 5
> targets 20% on 1st .. Same just half mp/stones cost for half the targets healed and less heling
> factor."*

Built for all three buffer races at **83**, as Urgent Great Heal with four numbers changed and
nothing else:

| | healer (83) | buffer (83) |
|---|---|---|
| MP | 500 | **250** |
| Skill Stones | 5 | **3** |
| targets | 11 (10 + you) | **5** (4 + you) |
| first share | 30% | **20%** |
| cast / reuse / range | 3s / 5s / 0-1000 | *identical* |

Worst-hurt first, the caster placed by his own injury like anybody else, and — like its parent — a
pure % heal, so §1 above means a buffed Warchanter cannot inflate it. On a 21k tank the whole cast is
~16,700 HP across five people against the healer's ~46,000 across eleven.

❓ **One number is mine, not yours: the per-rank falloff.** You gave the first share and the target
count; I carried the healer's **−2%** over, so the five slots pay **20 / 18 / 16 / 14 / 12%**. Filed
as **`BL-234`** — say the word and it is one edit in the code and one in the CSV row.

`buffer 4th.csv` gained its row in the same commit, and `SkillCsvSeed --check` is clean.

### 3. The demon-vs-elf P.Def report — measured, and I cannot reproduce it (`BL-233`)

> *"check demon buffer (epic 76 heavy + maul) had less pDef than elf buffer (epic 76 light + bow)
> both @90lvl admin buffed"*

New mode: `dotnet run --project tools/BalanceMatrix -- --bufferdef 90 epic 76 [--buffed]`. It builds
exactly those two characters and reads the same `EffectiveDefence` the character sheet prints. The
demon is **+104 P.Def ahead bare and +166 ahead admin-buffed** — and the gap can only *widen* when
you buff, because every P.Def buff on that bar is a percent. P.Def has no stat term in this game at
all, so the only inputs are items (232 vs 174 on the body), armour masteries (identical for all three
weights, by your own one-line rows), set bonuses (neither tier-76 set grants P.Def) and those buffs.
`BL-233` lists the three things that would settle it — the two numbers off your sheets is the
quickest.

### Also in this version

- `docs/Formulas.md` gained a **Healing** section — the flat/% split, the four channels, the area
  triage ordering, in one place. It had none.
- 🔴 **A sixth field channel was missing from the balance rig's buff builder**: the three HEAL
  channels ride as plain fields (`SkillEffect` has had no bits for years), so Harmony of the Soul's
  *"+20% Healing Received"* landed as **nothing** in every `--buffed` table. Fifth was `BL-218`'s
  CC-resist pair, fourth `BL-210`'s magic crit damage. **If a buff has a number, ask where it rides.**
- `--bufferdef` and `--healpower` are new; `ApplyAdminBuffs` models the ADMIN full buff (the buffer
  class's own kit) as `ApplyNpcBuffs` models the Spirit Helper's shelf — the two shelves are separate
  lists by your 2026-09-03 ruling, and a table that says "admin buffed" must use the right one.

## 2026-09-13 — 0.142.1: the debuff landing file

⚠ **NEW APK not required.** No protocol bump, no `game.db` delete.
🔑 **A NEW AUTHORING FILE: `docs/data/debuff_landmods.csv`.** It is the authority for
`DebuffLandMod`, on the same contract as the class CSVs. The four class CSVs that carried
`(success chance xN)` no longer do — **231 rows stripped**.

### Your rule changed, and it is a better one

> *"I group them but it's not OK as u said ... So dmg + debuff should have lower chance than a solo
> debuff ... a solo slow or a solo dot should be at x1 but combined should be x0.85 ... A solo stun
> can sit at x1 but with dmg or other debuff should go lower ... An armor break should stay as solo
> debuff and nothing else at x1.5 but witches curse that does dmg should be x0.85"*

The modifier prices **how much one cast does at once**, not what kind of effect it is. That is why
the five buckets could not work: they keyed on the effect, and Armor Break (two debuffs, no damage,
×1.5) and Witches Curse (one debuff plus damage) landed in the same bucket while deserving opposite
numbers. **Applied so far: Witches Curse ×1.00 → ×0.85**, your worked example.

### The file

| column | what it is |
|---|---|
| `SKILL`, `SKILL_ID` | name and id |
| `CLASS` | who learns it — collapsed, so `Magus` = all three races, `Magus(Elf)` = a race split |
| `DESCR` | what it does and what it cuts, **at the top rung** |
| `SAVE` | `SPT` / `CON` / `none (fizzle roll)` |
| **`SUCCESS`** | **yours.** The modifier. |
| `SHAPE` | `DEBUFF ONLY (2)` vs `dmg+1 debuff` — the axis your new rule prices on |
| `IN_CODE` | what the build ships, regenerated every run |

Regenerate the derived columns with
`dotnet run --project tools/BalanceMatrix -- --dump-landmod-csv`. ⚠ **It preserves `SUCCESS`** — the
seed columns refresh around whatever you have authored.

### And it is CHECKED, or it would be decoration

`dotnet run --project tools/SkillCsvSeed -- --check` now walks it too, and reports two different
things: **DRIFT** (file and code disagree — the code owes it) and **NOT IN THE FILE** (a debuff
nobody has priced, which is your *"each new debuff to go there and to ask for modifier edit"*).
It earned its place immediately: it caught Witches Curse mid-edit, file ×1 against code ×0.85.

### ⚠ 39 of the 74 rows are not learnable by any class

`Shield Bash`, `Envenom`, `Rupture`, `Terrifying Roar`, `Snare Trap`, `Entangling Roots`, `Soul Sap`,
`Warding Step`, `Weakness`, `Greater Weakness`, `Frost Bind`, `Creeping Frost`, `Hamstring`,
`Toxic Sting` — orphaned by the **2026-08-10 40+ purge** and the nuker rebuild, which deleted the
learn assignments and kept the defs on purpose (`LearnedSkills` persists ids). The rest are boss,
whisp and proc-granted skills. **Don't spend modifiers on those rows** — the `CLASS` column says so.

## 2026-09-13 — 0.142.0: the attacker is always ATK, and three riders stop apologising

⚠ **NEW APK not required** — all server-side. No protocol bump, no `game.db` delete.
⚠ **`nuker 3rd.csv` and `nuker 4th.csv` moved with the code**, same commit: 87 rows.

### 1. `ATK vs CON` / `ATK vs SPT` — the whole rule, in two lines

> *"Still why bleed is agi vs con ? Physical buffs should be atk vs con magical atk vs spt ?"*

A bleed or a venom rolled the caster's **AGI**; everything else rolled **ATK**. That made the
*attacking* half of the contest depend on the EFFECT rather than on the channel — so an Ice Master's
Frost Pierce and his Frost Spikes threw two different stats at the same archer, and the nuker's bleed
rolled a different stat from the archer's identical one. Now:

```
physical debuff → ATK vs CON
magical  debuff → ATK vs SPT
```

The **defending** side is untouched and still comes from the DoT **family**, not the skill's own
`DebuffSchool` (your 2026-09-10 ruling) — so a bleed is saved by CON even when the spell that opened
it is `Magical`. Which stat throws the punch and which stat takes it are two different questions.

🔑 Both roll sites — the cast path and the on-hit rider path — now read one
`GameLoopService.CcContest`. They had **already drifted apart once** (the comment on the second site
said so), and this is three readings each; leaving them as two copies was the bug waiting to happen.

### 2. The three riders

> *"maybe we need to remove the 3 skills the success decrease .. As they don't Harm as a buff removal
> or bind or silence etc... They are not sure kill if the land"*

| skill | was | now | why |
|---|---|---|---|
| **Witches Curse** | ×0.70 | **×1.00** | carries no control at all — only an M.Def cut |
| **Frost Spikes** | ×0.70 | **×0.85** | carries a slow |
| **Frost Pierce** | ×0.50 | **×0.85** | its bleed carries the family's 20% slow |
| Witches Scarecrow | ×0.50 | ×0.50 | a **fear** takes the target's turn |
| Arcane Void | ×0.30 | ×0.30 | a **cancel** takes their buff bar |

🔑 **The test you drew is what landing TAKES AWAY, not how big the number is.** A cancel, hold,
silence or fear costs the target their turn and keeps its steep penalty. A slow, a bleed and an M.Def
cut only make the fight worse — you play through all three — so they are priced as riders.

### 3. Why 0.85 and not 1.00 on the two frost skills — your own number, confirmed

> *"If we make the pierce a atk vs con it adds 20% slow and the 45% slow from spike it makes the
> archer with 68 speed"*

Measured (`--slowstack`, new): Frost Spikes' top rung slows **45%**, the **bleed family's** own flat
slow is **20%** — authored in `DotTiers`, so *neither skill's row advertises it* — and slows **SUM**
(`Entity.SlowFraction`, clamped 90%), they do not compound. A fully-buffed Demon Hunter runs **216**
and lands on **76** with both. Your 68 was the right shape and very nearly the right number.

### What it does, vs a level-90 Demon Hunter

| | bare | + full shelf | + Warchanter + Holy Mark |
|---|---|---|---|
| Frost Pierce | 13% → **23%** | 13% → **23%** | 9% → **15%** |
| Frost Spikes | 23% → **28%** | 20% → **24%** | 13% → **15%** |
| Witches Curse | 24% → **35%** | 21% → **30%** | 13% → **19%** |

⚠ And the two nukers are no longer twins: Witches Curse **35%** against Frost Spikes **28%**, which is
the ×1.00-vs-×0.85 plus the Demon Magus's 43 ATK against the Elf's 38.

## 2026-09-13 — 0.141.2: the Grand Rune was missing the cast-time cut

⚠ **NEW APK not required** — cast length is computed server-side and sent to the client. No protocol
bump, no `game.db` delete.

### The report

> *"Also spell rune don't decrease the cast time with 40% as we spoke"*

**The cut works — on the Spell Rune.** It is the GRAND Rune that never got it, and the Grand Rune is
the one in the admin menu, so it is the one being tested with.

`BL-216` gave the Spell Rune `CastTimePct: 0.30f` on 2026-09-12 and did not come back to the Grand
Rune. That rune's own note says it is *"BOTH SINGLES AT FULL STRENGTH, NOT A COMPROMISE"* — and since
`ReconcileRuneBuffs` **drops both singles the moment a Grand Rune is held**, there was no way to hold
one and still get the cut. For a day the premium rune was strictly **worse** in the magic channel than
the vendor rune it supersedes.

🔑 **This is the covering-group rule wearing a different hat:** a thing that supersedes others must be
≥ them in **every channel they carry**, and every channel is a separate number to forget. The Grand
Rune now carries the same `0.30`, and `--castcycle` grew a **Grand Rune row directly beneath the Spell
Rune row** so the two `castTimeMult` cells can be read against each other and never silently diverge
again. Both item descriptions now say the 30%.

| Magus 90, full caster stack | castTimeMult | Elemental Blast | Quick Blast |
|---|---|---|---|
| + Harmony of Soul (no rune) | ×1.00 | 1.60s | 0.80s |
| + Spell Rune | ×0.70 | **1.10s** | **0.50s** |
| + Grand Rune — **was ×1.00** | **×0.70** | **1.10s** | **0.50s** |

### ❓ On the 40% — it is 30%, and that was your own arithmetic

Your `BL-216` spec gave the number three ways and all three agreed on **30% off the final cast time**:

> *"It increases the cast speed behind the scene with ~40% ..which is actually **30% decrease on the
> final cast time**. So if rune is active the cast time of a spell is:
> `(baseCastTime/(charCastSpeed/333))x(runeActive ? 0.7 : 1)`"* — and your worked example,
> *"4000/(1999/333) = 667 ms but with rune active it becomes **467ms**"* (667 × 0.7 = 467 ✓).

×1.4 cast **speed** is ×0.714 cast **time** — the 40% and the 30% are the same statement from the two
ends. So `0.70` is what shipped. **If you now want 40% off the FINAL time** (`×0.60`), say so and it is
a one-character change in two places; `--castcycle` already prints that row: Elemental Blast **1.30s →
0.80s** on the NPC shelf alone.

## 2026-09-13 — 0.141.1: a curse is not a blessing

⚠ **NEW APK not required** — the server already told the client which row a buff belongs in; it was
telling it the wrong thing. No protocol bump, no `game.db` delete.

### The report

> *"I apply witches curse to an enemy .. and he gets it as a buff and can be hold to dismiss.. It
> don't feel the curses land so often"*

Both halves were one bug. **Witches Curse expresses its M.Def rot as a NEGATIVE `BuffMagicDef`
magnitude** — the idiom Armor Break, Frost Burst and every whisp curse already use, because the
`SkillEffect` enum is full and there is no `DebuffMagicDef` bit left to spend. A negative buff flag is
still a buff flag, so `Effect & AnyDebuff` was **0**, so `BuffInstance.IsDebuff` said *blessing* and
everything downstream agreed with it:

- it rendered in the **buff row**, not the debuff row;
- **hold-to-cancel offered to dismiss it** — the victim could delete the curse with a long press;
- a *"cancel positive buffs"* would have **stripped it as a blessing**;
- it **counted against the target's 24 buff slots** and could **evict one of their real buffs**;
- `PersistenceService` **saved it across a relog** (the save skips `IsDebuff`).

A curse you can press off, that eats a slot and survives logout, is a curse that does not feel like
it landed. That is the second half of his sentence, and it needed no balance change to explain.

### The fix — one predicate, `SkillMath.IsHostile`

`ExecuteSkill` had **already** been asking the right question in three places: the contested-debuff
arm, the fizzle arm, and the buff arm's `harmfulPayload` guard (added 2026-09-03 for exactly this
class of bug, when the same negative-magnitude idiom made Armor Break resolve **twice**). The buff
**instance** — the thing that outlives the cast — was the one place still asking the narrow flag-mask
question. So the three doors are now one predicate in `Game.Shared`:

```
IsHostile(def, effect) = IsContestedDebuff(def, effect)        // ContestCc | DebuffSchool | Charms | Pulls | Silence
                       | (effect & AnyDebuff) != 0
                       | def.Category == SkillCategory.Debuff
```

`IsContestedDebuff` moved out of `GameLoopService` to sit beside `IsPhysical` in `SkillMath` — it is a
question about a def and nothing else. **`ApplyBuff` stamps the answer onto the instance** as
`BuffInstance.Hostile`, and `IsDebuff` reads the stamp. It is carried rather than re-derived because
the flags on a landed buff genuinely cannot answer it: only the def knows, and only while it lands.
The same number now drives the slot-cap eviction test, so the cap, the row and hold-to-cancel can no
longer disagree.

⚠ **A self-buff with a downside is NOT hostile**, and the line is sharp: Frenzy pays −Max HP,
Defensive Wall pays −50% move speed, Combat Stance pays −50% M.Atk, Holy Soul pays −10% cast speed —
all negative magnitudes on buff flags, all `Category.Buff` with no `DebuffSchool`, none of them
tripping any of the three doors. A downside you chose is not a curse somebody cast on you.

### Ten skills were in the wrong row

| skill | why the old test missed it |
|---|---|
| **Witches Curse** | payload is a negative `BuffMagicDef` |
| **Arcane Burst** | payload is the `CcResistMagical −40%` **field** |
| Arcane Void, Mana Strain | payload is a field (`DispelCount`, MP-cost %) |
| Taunt, Mass Taunt, Lure, Whisp Taunt, Tauting Wall | `Taunt` is not in the `AnyDebuff` mask |
| Boss's Judgment | `Effect` is `None` — the mark is all field |

### On the landing rate itself — measured, not derived

Against a **same-level** creature, holding the **best rung you can learn**, Witches Curse lands:

| target | contest | × his `x0.70` |
|---|---|---|
| melee creature (SPT 38) | 52.5% | **36.7%** |
| caster creature (SPT 58) | 42.0% | **29.4%** |

That is his own CSV working as authored — *"(success chance x0.7)"* off a 50%-at-parity base. **The
number that collapses is an OLD RUNG**, because `DebuffLandChance` reads the rung's LEARN level, not
the caster's: at level 90 the `@74` rung lands **9.5%** and everything below `@72` sits on the 10%
floor at **7.0%**. ⚠ So the one real cliff is **74 → 76**: a Magus who has not paid the Rite at
Archmaster Sevrin keeps casting the `@74` rung while the creatures keep levelling, and by 85 it is
landing 15.7%. Nothing was changed here — flagged so he can rule on it.

## 2026-09-13 — 0.141.0: magic resistance also resists magic debuffs

⚠ **NEW APK.** No protocol bump, no `game.db` delete.

### `BL-227` — mRes joins the magical side of the control product

> *"OK I like the idea mresist to decrease the chance ..it look not so much op (it takes of tank/nage
> ~5% and 2% for nullblade) and we espect nullblade with magical armor to resist more."*

`mRes` now buys two things off one number: less magic **damage** (it is the divisor in
`MagicDefCoef`) and fewer magic **debuffs** landing. A plain `(1 − r)` factor on the magical side,
like every other source since `BL-225` — his *"endLandRate x 0.3(30% mresist)"* is linear, not a
second divisor. One place, `GameLoopService.SchoolCcRetain`, so all three roll sites inherit it.

| a ×1.00 magic debuff, fully buffed, level 90 | mRes | without | **with** |
|---|---|---|---|
| Magus (mage) | 35% | 19.8% | **12.9%** |
| Bulwark (tank) | 21% | 22.8% | **17.9%** |
| Nullblade | 10% | 22.4% | **20.2%** |
| Nullblade **+ Magical Armor** (10s) | 60% | 22.4% | **9.0%** |

⚠ **Passive mRes is included, deliberately.** It was flagged to him that this makes the MAGE — whose
`anti_magic` ladder is the largest passive mRes in the game at 35% — the hardest of the three to land
a magic debuff on, which reads backwards. He looked at that row and took it; it is written into the
code comment so nobody quietly narrows it later. ⚠ A negative mRes (a "Magic WEAK" creature, −20%)
correctly becomes a ×1.20 factor.

`--ccprofile`'s table A now prints the built number with a `no mRes` counterfactual beside it, so what
the change bought stays visible.

### ⏸ `BL-228` — boss jewels, filed as future content

His idea, parked by him in the same breath: jewels that grant a chance on one school of control and
resist another, so a build chooses *"resist stuns and your fears land, or resist fears and land
holds"*. Not scheduled. Recorded because the engine is already shaped for it — every resistance is a
factor in a product now, and a second differently-sourced one (mRes) just joined it — and because the
two things it would still need are worth knowing: a per-EFFECT axis (everything today is per-SCHOOL)
and an attacker-side land channel, which the engine still does not have at all.

## 2026-09-13 — 0.140.0: the CON resistance comes down, and a Clarity cliff that was never there

⚠ **NEW APK.** No protocol bump, no `game.db` delete.

### `BL-226` — Fortitude's CON resistance 65% → 35% at the top

> *"OK make it 35% con resistance on the fortitude at max rung and I'll test it"*

🔑 **IT COULD NOT BE JUST THE MAX RUNG.** Rung 4 was already **40%**, above the new ceiling, so
changing only rung 12 would have produced 15 / 20 / 30 / 40 / … / 35 — a ladder running DOWNWARDS,
and every ladder here is monotonic. The twelve rungs are re-spread between his two anchors: rung 1
keeps his **15%** and rung 12 is his new **35%**; his old 20/30/40 at rungs 2-4 could not survive.

⚠ It reached three more families:
- **Arcane and Feral Protection's CON column** (the GROUP over Fortitude) mirrors rungs 5-12 and moved
  with it, 43→65% becoming 23→35% — a group may never be weaker than a single it covers.
- 🔴 **CLARITY, 50% → 20%, and this was a REAL DEFECT SHIPPED IN 0.138.0.** His 20% SPT ruling moved
  the GROUP to 20% while the SINGLE it covers still granted 50%. Clarity tops out at level 72 and the
  group takes over at 74, so **SPT resistance would have DROPPED from 50% to 20% on levelling up**, and
  a party with no buffer would have been harder to debuff than one with a buffer. Caught by
  `SkillCsvSeed --check` complaining about a CSV row, not by anyone reasoning about it. Re-spread to
  11/14/17/20.
- The group's rung-1 blurb and the `cleric 2nd` Clarity row followed.

Five CSV files moved with the code. `--check` is back to its two pre-existing Sundering Blow lines.

### The result

| ×1.00 debuff, fully buffed, level 90 | before | now |
|---|---|---|
| magical (SPT) | 10-11% | **20-23%** |
| physical (CON) | 8-10% | **19-24%** |

Both inside his *"15-25% which is good"*, and the two schools are within a couple of points of each
other for the first time.

### `--ccprofile` — the two tables he asked for

New rig mode printing one row per DEFENDER and one column per layer of the product, for a `×1.00`
magic debuff and for a stun, with the "was 65%" column recomputed rather than remembered.

**His open question is measured but NOT built** — *"shouldnt mresist add to magic debuffs
resistance?"*. It does what he wants for the two classes he named (a Nullblade's Magical Armor becomes
a real 10-second control-immunity window at 9.0%, the tank picks up a few points) — **but the MAGE has
the most magic resistance of the three (35%, from the nuker's own anti-nuke passive ladder), so he
would end up the hardest of all to land a magic debuff on**, which is backwards from the rest of this
design. Suggested to him as "buffs and ultimates only, not passives" if he wants it. → `BL-218`.

⚠ Also flagged, not touched: **Battle Resilience is now the biggest number in the file** — 80% CON
*and* SPT for 60s on a 150s reuse takes a stun from 19% to **3.8%**. It was already dominant when
resistances summed (it alone reached the old 0.8 clamp); compounding made it cleaner, not smaller.

### ❌ Declined

The mage's ×2 SPT land-rate passive — *"if you haven't added the passive on mage for x2 spt resistance
..good don't and remove it as ruling"*. Never built; struck from `BL-218`.

## 2026-09-13 — 0.139.0: control resistances compound, and three more clamps go

⚠ **NEW APK.** No protocol bump. ⚠ **A `game.db` delete is NOT needed** — nothing schema-side moved.

### `BL-225` — every control resistance is its own factor

> *"I want the cc resist formula to be something if we have harmony 20%,buff 20%,Passive 20% ->
> baseLandRate x LandMod x (1-buff1/passive1) x (1- buff2/passive2) x(1-buffN/passiveN) … having those
> 3 20% resists make the debuff land 2 times less (x 0.512) not ~4 (x 0.28) as it was. And adding a
> set bonus u get to ~3 times less -> which is nicably less but not never"*

```
land = clamp(contest, 10%, 90%) × DebuffLandMod
     × Π(1 − r) over every BLANKET source     (armour sets, shields)
     × Π(1 − r) over every SCHOOL source      (passive, class buff, harmony, Mark)
```

Three 20% resistances are **×0.512** exactly as he wrote; with an epic set's 28% it is **×0.369**, his
*"~3 times less"*.

🔑 **THE FIELD STORES WHAT SURVIVES AND THE RESISTANCE IS A GETTER** — the same shape `BL-217` used for
reuse in 0.136.0, for the same three reasons: every existing reader kept working untouched, nothing on
the wire moved, and a stray `+=` on `CcResist` is now a **compile error** rather than a silent return
to summing. `AddCcResist` / `AddCcResistMagical` / `AddCcResistPhysical` are the only writers.

⚠ **THE THREE 0.8 CLAMPS ARE DELETED.** They existed because summing could reach 100%; a product of
factors below 1 never reaches 0, so they had become ceilings the stack was already pinned on — *"Don't
add clamp no need when it mutiolicative"*, his own words a week earlier about reuse. Only a sign guard
remains, against a source authored above 100%.

⚠ **NEGATIVE RESISTANCES STILL WORK**, and are the second reason to store the retain: the Magus's
curses author `CcResistMagical: -0.40` and contribute a ×1.40 factor to the product for free.

⚠ `SchoolCcResist` deleted in favour of `SchoolCcRetain`, and the three roll sites read the retain
instead of writing `1f - CcResist`. Identical by algebra — but a helper returning a *resistance* beside
a product is how the next edit reintroduces summing.

### ✅ He was right about the Marks

*"the con/spt resists are on a single marks not on the harmony one ... Ppl will chose harmony mark"* —
confirmed: **Harmony Mark carries no control resistance at all.** Only Holy Mark (15% SPT) and Life
Mark (10% CON) do, and all four share `MarkKey` with `FlatRank`, so taking the Harmony Mark costs the
grant outright. `--ccland` now prints both cases as separate rows. ⚠ One correction to his arithmetic:
the SPT Mark is 15%, not 10%, so his ×0.460 is the CON case and the SPT case is ×0.435.

### The result

| ×1.00 skill, fully buffed, level 90 | this morning | now |
|---|---|---|
| magical (SPT) | 10-11% | **20-22%** |
| physical (CON) | 8-10% | **10-13%** |

His *"15-25% which is good"* is hit on the magical side. 🔵 **CON is not, and is now the outlier** —
he ruled on SPT only, so Feral Protection's 43→65% column is untouched and is the biggest resistance
in the game; the tank's whole control kit sits at half the mage's reliability. One number, and it is
his CSV. → `BL-218`. 📐 [balance/DebuffLandRate.md](balance/DebuffLandRate.md), `docs/Formulas.md`.

## 2026-09-13 — 0.138.0: traps see people, the barrage stops doubling, and the rig was wearing four Marks

⚠ **NEW APK.** No protocol bump. ⚠ **Every `--buffed` number this repo has quoted since 0.113.0 was
too high** — see `BL-223`.

### `BL-224` — Arrow Barrage: the `[Double]` off, power 2,500 → 2,000

> *"a mage is killed by one arrow barrage .. Also remove double of arrow barrage if it can (I haven't
> seen for about a x10 bae ages not a single arrow crit) but I don't hwat it to have.. Also decrease
> it's dmg to 2k per arrow"*

🔑 **THE DOUBLE WAS THE PROBLEM AND ALSO THE REASON HE NEVER SAW IT.** `BL-213` gave this skill **ten
independent doubling rolls** — one per arrow — where every other skill gets one per cast. At a ~10%
mastery rate a ten-arrow volley doubles *some* arrow **65% of the time**, so the volley's average ran
~10% hot while no single arrow ever looked doubled on screen. A per-shot roll on a ten-shot channel is
not the same mechanic as a per-cast roll. Wrapper and arrow both drop the flag; the CSV row moved with
the power.

### `BL-222` — a trap could only ever see MOBS

> *"both players are flagged both players are with pvp on ..and enemy cannot trigger trap ... Only
> mobs... A trap should trigger when I'm put it and I'm with pvp on ... (by any enemy that wont flag me)"*

🔴 `FindTrapVictim` filtered `e.Kind != EntityKind.Mob`, above a doc-comment reading "(and, once PvP
exists, enemy players)" — a TODO from before PvP shipped that nothing came back to. The Trapper's whole
discipline was PvE-only and nothing said so.

🔑 **THE RULE IS `CanPvpHit`, ASKED AS THE OWNER.** His *"any enemy that wont flag me"* is already what
that predicate means, so the trap asks the ordinary attack question and inherits the safe-zone check,
the never-your-own-party rule and the guard/NPC doors. The toggle is **captured when the trap is armed**
(his *"when I'm put it"*) and the safe-zone test reads the **trap's** position, not the owner's —
`CanPvpHit` grew an overload for both. `FireTrap`'s sweep had its own copy of the mobs-only filter, so
without fixing it too an enemy would have sprung a trap and walked away unharmed: one predicate,
`TrapCatches`, both places — the `BL-154`/`BL-123` lesson for the third time.

### `BL-221` — Magical Armor 30% → 50%

mRes 10% → **60%**, divisor 1.100 → **1.600**, a Magus's Arcane Burst 734 → **505 (×0.69)**. So −31% on
his own damage instead of −21%, and −38% against a dual carrying neither.

### `BL-218` — his 20% SPT ruling is in; the band it aimed at is not reached

> *"I calculated we must do the harmony and buff also be 20% (not 30/50) that way the land rate will be
> 15-25% which is good"*

Harmony of the Soul's top rung 30% → 20% SPT and Arcane and Feral Protection 50% → 20% SPT, CSV rows
with them. The CON half (43→65%) is untouched — his message is about SPT throughout and that column is
his authored CSV.

🔑 **HE WAS PINNED ON THE 80% CLAMP**: passive 20 + buff 50 + harmony 30 + **Mark 15** = 115 → 80. Same
trap as the reuse clamp he killed in 0.136.0, which is why only changing *both* numbers moved anything.

🔴 **THE BAND IS STILL NOT REACHED** — ×1.50 skills 15-16%, ×1.00 skills 10-11%, ×0.50 skills 5%. Two
arithmetic reasons: he counted three sources and there are **four** (a Mark carries 15% SPT), and they
**SUM** where he multiplied (×0.25 rather than ×0.512). Making them compound — his own 0.136.0 ruling,
same shape — gives 16% / 24% / 8%, almost exactly his band, and takes the clamp out of reach. Put to
him rather than done: it reaches the CON side too, where 75% summed becomes ×0.315, a real loosening
for tanks. 📐 [balance/DebuffLandRate.md](balance/DebuffLandRate.md).

### 🔴 `BL-223` — THE BALANCE RIG HAS BEEN WEARING FOUR MARKS AND SIXTEEN HARMONIES SINCE 0.113.0

`SkillCatalog.NewbieBuffSet` has **contained** `NpcSingleHarmonySet` and `NpcMarkSet` since
`BL-160`/`BL-161` — its own doc-comment says "19 + 8 + 3 = THIRTY" — but `ApplyNpcBuffs` concatenated
both **again** for `fullShelf: true`. The eight harmonies landed twice and the Marks four times; even
the PLAIN shelf wore all three Marks, where all three share `MarkKey` with `FlatRank` and the engine
allows exactly **one**.

**Every "buffed" row this tool has printed since 0.113.0 was a character wearing buffs the game cannot
give him** — inflated M.Def, crit damage, cast speed and control resistance. `--mcrit` carried a
hand-written workaround for the Mark half, which is the clue that had been sitting there the whole time.

🔑 **THE FIX IS THE ENGINE'S OWN INVARIANT: A BUFF KEY IS BUFF IDENTITY** — two buffs with one key never
coexist. The shelf is deduped by key, which repairs this and any future overlap without the builder
knowing which sets contain which. `fullShelf` now means what its name says.

⚠ Tables that move (lower, and correct): `--dmgmatrix`, `--stab`, `--castcycle`, `--magicdef`,
`--defbreak`, and `--ccland`'s own shelf rows from yesterday. Also added: `WarchanterParty()`, because
he plays beside a real buffer whose class blessings the NPC shelf does not sell — and it applies
`CoveredKeys`, so a class buff evicts the NPC singles it contains instead of stacking (`BL-183`).
`CCDEBUG=1` itemises every control-resist source: a summed stat sitting on its clamp hides how many
addends there were.

## 2026-09-12 — 0.137.0: the target window keeps up, and why debuffs don't land

⚠ **NEW APK** (client-side changes). No protocol bump — nothing on the wire moved.

Four asks from the second message of the 2026-09-12 playtest. Three are built; two are answered with
measurements and need a ruling.

### `BL-219` — the target window: debuffs only, abbreviated, and LIVE

> *"Remove the positive effect of the target window …leave only the abriviation of debuffs.. Also I
> don't think the target window even debuffs are updated when they suppose to … when stab lands I
> suppose to see x3 but I dont ... I land several more then in one go I see x9 ... Some times I se
> 3-6-9-10 ... At random .. Like some kind of update interval ..."*

🔑 **IT WAS EXACTLY AN UPDATE INTERVAL.** `PushTargetBuffs` ran on the once-a-second `secondTick`,
the same beat as the player's own buff bar. A stab banks its venom the instant it connects, so two
stabs inside one second arrived as a single jump of six, and the same two either side of the beat
arrived as two threes — the 3-6-9-10 / 6-9-10 pattern, random-looking because the beat has no
relationship to when he presses anything.

It runs **every tick** now. The change that makes 10/s affordable is the ORDER: the signature is
built FIRST, straight off the buff list (name + stacks + whole seconds), and the expensive half — the
DTO list with descriptions, icons and source lookups — only runs on ticks where something moved. A
selected creature with an empty buff list is zero iterations and no message. Seconds are still
rounded into the signature, so the countdown did not become ten times chattier; only the STACKS
became immediate.

⚠ `StacksShown` extracted so the signature and the DTO read the SAME number. Writing those two out
separately, with a stack fold applied to one and not the other, is exactly how the bar came to show
"x7" while the venom ticked for one (`BL-198`).

Client: the beneficial half of the line is gone, names are abbreviated (multi-word → INITIALS,
"Venom Stab" → `VS`; single word → first four, "Gravity" → `Grav`), stack counts never shortened,
and the green/red colouring went with the positive half.

### `BL-220` — DoT/HoT out of the combat chat

> *"Remove dot/hot from combat chat (or make it option for the client) it's to much flood and miss
> the dmg."*

Both: removed by default, **Settings → `DoT/HoT in chat`** brings them back. Default OFF because he
asked for removal and offered the toggle as the alternative.

🔑 It hides a LINE OF TEXT and nothing else — the filter sits below `CombatHappened`, so floating
numbers, attack animations and the "who is hitting me" list still see every tick.

Covers the DoT tick, the HoT tick and the MANA half of a heal-over-time. ⚠ Mana VAMPIRISM shares the
`Mana` tag but is a per-HIT effect broadcast as `Heal` rather than `ManaHeal`, so it is not filtered.
The three tags moved to `GameConstants` (`DotTag`/`HotTag`/`ManaTickTag`/`IsTickTag`) — they were
string literals on the server with a matching literal in the client's floater code, the arrangement
where renaming one half silently breaks the other and nothing errors.

### `BL-218` 🔵 — why debuffs don't land: measured, and it is NOT the floor

> *"debuffs almost never land wit all the resistanses we have … Can you get me same lvl debuffs and
> check their land rate with and without buffs/passives ? I think we hit the floor for landing."*

New rig mode **`--ccland [level] [quality]`**, and the whole table is in
[balance/DebuffLandRate.md](balance/DebuffLandRate.md).

🔑 **`CcLandMin` (10%) clamps the STAT CONTEST only, and the contest between two level-90 characters
comes out at 50-54% — nowhere near it.** What eats the number is the three multipliers applied
AFTER the clamp, none of which is floored:

```
land = clamp(contest, 10%, 90%)   × DebuffLandMod   × (1 − CcResist)   × (1 − CcResist<school>)
       ~52% at parity               1.50 … 0.30       0/0/28/40% BY GEAR   20% → 35% → 50% by shelf
```

A fully-blessed level-90 in mythic gear multiplies every incoming debuff by **×0.30**; a `×0.50`
skill by **×0.15**. Numbing Shock lands 11%, Arcane Void 6%.

🔴 **The flat `CcResist` is a GEAR CLIFF — 0% at common and rare, 28% at epic, 40% at mythic** — no
buff feeds it, every class gets the same number from its own tier's set, and nothing on the
attacker's side can answer it. 🔴 **And there is no attacker-side land channel in the engine at all**,
which is why his own proposed SPT passive is the right shape and bigger than it looks. Three things
to rule; his call on each.

⚠ **THE RIG WAS LYING AND IT IS THE FIFTH TIME THE SAME BUILDER HAS DONE IT.** `ApplyNpcBuffs` never
copied the `CcResistMagical`/`CcResistPhysical` **fields** off a buff def — they are fields, not
`Effect`+`Magnitudes`, because the flag enum is full — so the NPC shelf moved control resistance by
ZERO and the first run of this table read identical buffed and unbuffed. Both builders fixed.

### `BL-221` ❓ — Magical Armor "does nothing": the engine says it does

> *"magic armor of null blade does nothing …with magic armor on the dmg is the same… Not 30% less"*

New rig mode **`--mres`**. A level-90 Nullblade under a same-level Magus's Arcane Burst: mRes
10% → **40%**, divisor 1.100 → **1.400**, damage 734 → **577 (×0.79)**, and 272 → **213** with the
shelf on. The def carries `BuffMagicResist 0.3 Percent` at both skill and rung level and every layer
folds it, crits included.

🔑 **+30% MAGIC RESIST IS NOT −30% DAMAGE.** Resistance is a DIVISOR (`damage ÷ (1 + mRes)`), the
same shape as defence everywhere else here: 1.1 → 1.4 is **−21%** on his own damage, but **−29.5%**
against a dual who has neither — which is probably the comparison he was making, and is his "30%".
Two numbers off one target will close it either way.

## 2026-09-12 — 0.136.0: reuse reductions compound

⚠ **NEW APK** (rebuilt for the same evening's class-table change). No protocol bump.

One ruling, and it is a change to how a whole channel stacks.

*"Make it mutiolicative if u haven't as any other buff is ... Don't add clamp no need when it
mutiolicative - I want to test with the 0.42 not 0.25 and if it additive to 80% the spell never can go
bellow 0.2s."*

```
reuse  = authored × retain                     min 1 tick; skipped when FixedCooldown
retain = CooldownRetain × (physical ? CooldownRetainPhysical : CooldownRetainMagic)
         each source multiplies its channel's retain by its own (1 − r).  NO CLAMP.
```

**What is stored is what SURVIVES.** `Entity.CooldownReduction` / `…Physical` / `…Magic` are computed
getters now (`1 − retain`), so the stat panel, the `StatsUpdate` DTO and the target inspector keep an
ordinary reduction fraction and nothing on the wire moved. A stray `+=` on one of them is a **compile
error** instead of a silent return to summing — that is why they are read-only.

**The three 0.8 clamps are gone**, on his reasoning: they were a hard floor of 0.2× the authored reuse
that the caster stack had already reached (75%), so his next tuning step would have moved a number the
engine had stopped reading. A product of `(1 − r)` approaches zero and never arrives, and
`ExecuteSkill` floors the result at one tick.

📐 `--castcycle 90 epic`, Elemental Blast on a Magus: Spell Mastery 20% (blanket) × Harmony of the Soul
20% × Harmony of the Wizard 35% (magic) = **×0.416**, his number to three decimals. Reuse 0.40s, and
the full cycle 0.90s against the 1.70s he was playing.

⚠ **It reaches PHYSICAL reuse too** — Harmony of the Soul's −30% beside Bow Blessing's −20% now reads
×0.56 where it read ×0.50. Every stack of TWO reuse sources got slightly weaker; only stacks of three
or more got stronger. Worth an eye at the next playtest.

### Verified

All four projects build; the server boots; `SmokeTest` passes; `--check` and `--maskaudit` clean but
for the two `BL-202` rows.

## 2026-09-12 — 0.135.0: the shot shortens a cast, the buffer gets a reuse ladder, and Elemental Blast keeps its second

⚠ **NEW APK.** The class-skill tables changed (Harmony of the Wizard gains three rungs). No protocol bump.

His third pass of the evening, and two of the three items were corrections to 0.134.0.

### The shot cuts 30% off the final cast time (`BL-216`)

His formula: `(baseCastTime / (charCastSpeed/333)) × (runeActive ? 0.7 : 1)`. Built as
`CastTimePct: 0.30f` on the Spell Rune. The old `BuffCastSpeed 40` FLAT grant stays beside it — forty
points on a stat an endgame caster carries at 1400-1900 is **+2%**, and nothing at all at the 1999 cap,
which is the whole reason the cast half felt missing. A cast-TIME cut survives the cap; a cast-SPEED
grant does not. ⚠ The War Rune is deliberately not given the same: he described the blessed spiritshot.

### Harmony of the Wizard gains a magic-reuse ladder (`BL-217`)

Three new 3rd-tier rungs at **58 / 66 / 74**, at **−15 / −25 / −35% magic reuse**, cumulative, and
carried forward by the 77/78/79 rungs (which are numbered 6-8 now, not 3-5). It is his "gift of
seraphim" third source, put in the buffer's hands.

✅ **And his suspicion about Harmony of the Soul was wrong** — *"if the harmony buff don't reach the
spell reuse and we fix it"* — it reaches: `SoulRung` authors `MagicCooldownPct` 0.10 → 0.20 and the
whole chain to `CooldownReductionFor` is intact. Checked before building; the stack was one source
short, not broken.

🔴 **But our reductions SUM and his arithmetic multiplies**: 20 + 20 + 35 is a **75%** cut (×0.25)
where his `0.8 × 0.8 × 0.65` is ×0.416. His numbers are authored exactly as given and the divergence is
reported rather than corrected, because the summing rule is a documented engine decision reaching every
class. ⚠ It also means his stated next step (*"up the souls and mastery to 30%"*) would read 95%,
**clamped to 80%**, and stop responding. Three ways out in `BL-217`.

📐 Elemental Blast's cycle at 90, epic (`--castcycle 90 epic`):

| stack | cast | reuse | cycle |
|---|---|---|---|
| NPC shelf only | 0.90s | 0.80s | **1.70s** |
| + Harmony of the Wizard L8 | 0.70s | 0.40s | 1.10s |
| + Harmony of the Soul L7 | 0.70s | 0.20s | 0.90s |
| + Spell Rune | **0.50s** | 0.20s | **0.70s** |

### Elemental Blast's reuse goes back to 1.0s

*"no need to decrease the cooldown to 0.5"* — 0.134.0's change is reverted. The reuse was indeed the
slow half of the cycle, but the fix is the buffer's ladder rather than a shorter authored number on
one spell: a party buff the mage has to be given, not a spell that is faster for everyone forever.

### And the nuker CSVs

The ×1.30 spell powers from 0.134.0 stand and the reuse cells are back to 1 — *"update the csv with the
increased spell powers (and untouched cooldown for now)"*. `--check` is back to its two pre-existing
`BL-202` rows.

### Verified

All four projects build; the server boots; `SmokeTest` passes; `--check` green but for `BL-202`.

## 2026-09-12 — 0.134.0: the creature curve becomes a level mod, the mage gets his power and his reuse, the archer gets his double

⚠ **NEW APK** — the class-skill tables changed again (`archer 3rd`/`archer 4th` gain a `[Double]`
label the client renders from the flag). No protocol bump.

His six follow-ups to 0.133.0, the same evening. Five built; the sixth is measured and handed back.

### The creature damage curve is a LEVEL MOD, not a flat ×2 (`BL-212`)

*"Let's make it lvl mod as u said. But <76 to restore what they lost (be as it was before bl185) and
76 to become harder."*

```
mult(L) = levelMod(L) x (1 + 0.40 x clamp((L-76)/14, 0, 1))
          L 20 -> x1.09    L 52 -> x1.41    L 76 -> x1.65    L 90 -> x2.51
BOSS: exempt
```

Below 76 it restores exactly what `BL-185` took and not one point more; above 76 it overshoots on
purpose. 📐 `--mobdmg 90 epic --buffed` against his two anchors: a normal creature **107 → 268**, an
elite **161 → 806** — ×2.5 and ×5.0, which is his "130 → 300" and "300 → 1500" to the ratio.
🔴 A **boss is exempt** — *"Bosses to compensate with their passive so they won't change"* — written
as an exemption rather than as a division of the boss attack rung, because the multiplier is
level-shaped and that rung is one constant. An **elite is not**: it takes the curve and its ×3.0 rung.
🔑 It reads the **target's** level (the term it undoes is the defender's) and tests both sides: the
attacker must not be a player, the target must be one.

### Does IG carry a level mod on P.Def and M.Def? Yes — and we had already proved it (`BL-215`)

His question decided everything else. `docs/balance/DamageVsIG.md` divides his five in-game M.Def
rows by gear and then by `levelMod` and the remainder is constant to ±2% across 56 levels:
`IG M.Def = SUM(jewel M.Def) × MENbonus × (level+89)/100`. P.Def divides out the same way. So the
level term is shared with IG, `MagicK 91` stays IG's verbatim constant, and the answer is his second
branch: keep the term, fix `BL-212` as a level mod, raise the 76+ spell power.

### The nuker's 4th-tier rotation power ×1.30 (`BL-215`)

| ladder | skills | was | now |
|---|---|---|---|
| blast | Elemental Blast · Vampiric Bolt | 110 → 138 | **143 → 179** |
| fast/rider | Quick Blast · Witches Curse | 88 → 109 | **114 → 142** |
| area/rider | Elemental Wave · Arcane Wave · Frost Spikes · Frost Pierce | 66 → 105 | **86 → 137** |

⚠ ×1.30 rather than his two point figures, which disagree with each other: +20 on 110 is +18%, under
his own *"atleast 30%"* floor. All three ladders, not just the blast he quoted — raising only it would
have retuned the other two DOWN by 30% against it. The ultimates keep their power.

### Casting is not slow. The REUSE was. (`BL-216`, and his item 6)

Our cast model is IG's arithmetic exactly — `authored × 333 / castSpeedStat`. What the measurement
found instead: an NPC-buffed Magus of every race is **on the cast-speed cap** (1999), so a 4s cast
resolves in **0.60s** while its 1s reuse, cut only 20% by Spell Mastery, ran **0.80s**. **57% of the
cycle was reuse.** Elemental Blast's reuse is **1.0s → 0.5s** on his ruling; the cycle goes
**1.40s → 1.00s**. The cast is untouched — *"4cast as is"*.

🔵 And the finding he guessed at himself: the **Spell Rune grants `BuffCastSpeed 40` FLAT** on a stat
already at 1400-1999, i.e. **+2%** — and at the cap a cast-SPEED grant is worth nothing at all, while
a cast-TIME cut still multiplies. If IG's blessed shot is −40% on the final cast, `CastTimePct: 0.40f`
is one line and takes the cast to 0.30s. **Not built** — his premise is unverified and the mage is
already at ≈×2.3 from this pass. `BL-216`.

### The archer's `[Double]` (`BL-213`)

*"Every archer dmg skill without explotion and magic arrow - so the twin/heavy/barrage(each arrow on
its own)"*. `CanDouble` on **Twin Arrows**, **Heavy Arrow** and **Arrow Barrage**. 🔑 *"each arrow on
its own"* needed nothing: both volleys are channels, the wrapper resolves nothing, and every arrow is
its own resolution — so Arrow Barrage rolls ten times. The flag sits on the arrow (the mechanic) and
on the wrapper (the label). Without this the archer's new Overpower would have measured as zero.

### What the mage has gained since 0.132.0

crit ×1.25 · power ×1.30 · cycle ×1.40 → **≈ ×2.3**. Worth re-playtesting before `BL-215`'s last
lever (the 20% magic crit-RATE cap, which every race now sits exactly on).

### Tooling

- `--mobdmg <level> <quality> [--buffed]` — what a normal / elite / boss lands on each class sheet,
  with the raw pre-curve number beside it, plus the curve across the whole game.
- `--castcycle <level> <quality>` — cast-speed stat, the 333 multiplier, cast, reuse and the CYCLE.
- `Shot()` takes the defender now, because the creature curve reads the defender's level.

### Verified

All four projects build; the server boots; `SmokeTest` passes; `SkillCsvSeed --check` is back to its
two pre-existing `BL-202` rows after 134 CSV cells moved with the code (`nuker 3rd`, `nuker 4th`).

## 2026-09-12 — 0.133.0: the mage's crit chain, blows against light armour, creatures that hit twice as hard — and four buffs that had never applied

⚠ **NEW APK.** No protocol bump — nothing on the wire changed — but the class-skill TABLES did, and
the client builds its Learn tab locally from the compiled `ClassSkills`.

A playtest pass at 90 in epic gear. Seven changes, six of them his and one found on the way.

### 🔴 Four buff payloads had never applied, and now a startup guard says so (`BL-214`)

Found while measuring `BL-209`: its crit-rate number went 30% → 100% and the rig printed the same rate
before and after. **A `BuffInstance` takes its `Effect` mask from the SkillDef and its magnitudes from
the RUNG, and every channel in `RecomputeDerived` is read behind `buff.Has(flag)`** — so a magnitude
whose flag the def omits is discarded in silence, while `SkillText` still advertises it and
`SkillCsvSeed --check` still verifies it against the value the engine throws away.

| skill | dead payload | dead since |
|---|---|---|
| Harmony of the Wizard | +20% MP regen **and** the whole magic crit-rate line | 0.106.0 |
| Harmony of Protection | 10% bow resistance (rung 6, @76) | 0.106.0 |
| Lethal Precision (Elf) | +10/15/20% crit damage — **the entire buff** | 0.121.0 |
| Lethal Focus (Human) | the crit-damage half of it | 0.121.0 |

The Lethal pair is the expensive one: `BL-188`'s whole design is *"the race split IS the balance"* —
Elf buys crit DAMAGE, Demon buys rate, Human splits — and only the Demon's ever worked, because his
rides a FIELD rather than a magnitude. `--blowrate` had been printing the Elf's "+ race buff" column
identical to his "passives only" column for nine versions.

`SkillCatalog.BuildCatalog` now throws at startup if any def authors a magnitude its mask omits — the
third guard beside the duplicate-id, child-id and `CoveredKeys` ones, all there for the same reason.
`dotnet run --project tools/BalanceMatrix -- --maskaudit` lists them; it reads CLEAN.

### The mage's crit chain (`BL-209`, `BL-210`)

- **Harmony of the Wizard's magic crit rate is Insight's** — `0.30f` → `1.00f` on rungs 4 and 5
  (*"make harmony of wizard crit rate be same as insight (x2 not x1.3)"*).
- **Harmony Mark grants magic crit damage**, +20%, matching every other universal line on that Mark and
  its own physical twin. His comment column on the level-83 row had said so for weeks
  (*"30+20% for magical"*); the code had never carried it. It is a FIELD, which is why it was missed.

📐 `--mcrit 90 epic`, a fully-blessed Magus: rate **8.8% → 20%** (the cap), crit damage **×2.60 →
×3.12** — his arithmetic to the digit — and **+25% average magic damage**. Roughly half of that is the
mask fix, not the two rulings.

⚠ The 20% rate cap now binds for every race, so it, not the buff, is the next lever. `BL-215`.

### A blow is cut by crit-rate resist (`BL-211`)

```
rolledBlow = BlowRate × (1 − target.BlowResist) × (1 − target.CritRateResist)
```

*"the light armor mastery and every crit chance reduction passive/buff to lower the blow rate as well
(the blow is crit dmg so heaving less chance to be hit by crit means blows as well)"*. The two terms
multiply rather than sum, so two independently-capped ladders can never pass 100% and invert the roll.

This **knowingly reverses `BL-188`**, which left it out because the rogue's own Armor Mastery carries
25-35% of it and it makes light armour the best anti-rogue armour in the game. That is now the intent.
📐 A light kit at 90 reads 35% and multiplies an incoming blow by **0.65**, against the tank's 0.70.
A shield's `ShieldCritDefense` still does not touch the roll — he named passives and buffs.

### Creatures hit twice as hard, elites four times (`BL-212`)

*"mobs should get x2 power ... (not patk just dmg) and elits should get (x2 p atk on what they have
now) so elit with the double in dmg and double in patk should do ~x4 dmg as of now"*

`MobRankScale.MobDamageOut = 2.0`, applied once in `FinalizeDamage` to any attacker that is not a
player, and the elite's attack rung ×1.5 → **×3.0**. Damage rather than P.Atk, on his call and for two
good reasons: the creature attack curve is fitted to IG off 2,831 measured monsters and sits on the
inspect panel, and damage is a ratio — doubling P.Atk is ×2 only while `power` is 0.

⚠ **It is level-flat and the loss it corrects is not.** `BL-185` gave the physical channel the
defender level term M.Def always had, so a defender's P.Def is ×1.79 at 90 (−44% creature damage) but
only ×1.09 at 20 (−8%). Measured, HP per kill doubles at every level and kills-until-empty falls
29 → 11 for a level-36 tank and 8 → 3 for a level-36 nuker. Carried to him as `BL-212`, with the
level-shaped alternative costed.

### The mastery roster, rewritten to his table (`BL-213`)

| | double dmg | reuse | duration | toggle |
|---|---|---|---|---|
| Magus | — | 76 | **76 (new)** | — |
| Ravager · Warlord | 20/40/76 | **76 (new)** | — | 81 |
| the three melee rogues | 40/76 | 76 | — | 81 |
| **the three archers** | **40/76 (new)** | **76 (new)** | — | **81 (new)** |
| Lightbringer · Warchanter | — | — | 76 | — |
| Bulwark | — | — | — | — |

This reverses `BL-191`'s *"ARCHER — never"* (2026-09-10, *"archer have enough skills that are always
hit wit big power"*) after two days of play: *"archers are like mages ...Strongest skill does only 5k
dmg to elit"*. Archers and duals take Overpower one rung under the warrior at 40 and 76, which is the
ladder `BL-203` already gave the duals.

⚠ Two display names are mine — **"Battle Momentum"** and **"Bow Momentum"**, from each file's own
vocabulary — and so are four learn levels, all mirroring existing rungs. Flagged in `BL-213`.
🔴 **No archer skill is flagged `[Double]`** above level 40, so his new Overpower pays out on nothing
until he names one. Also in `BL-213`.

### Tooling

- `BalanceMatrix --mcrit <level> <quality>` — the nuker's crit chain stage by stage. It strips the
  shelf's Mark before wearing the one being measured: all four Marks share a key, so a naive
  "shelf + Harmony Mark" row double-counts +20% and reports a ×3.74 nobody can reach.
- `BalanceMatrix --maskaudit` — the effect-mask audit above.
- `--blowrate` grew three defender columns (tank / archer / rogue) and a line saying what each brings.
- `Shot()` carries the creature ×2, beside the rune, exactly as `FinalizeDamage` does.
- `ApplyNpcBuffs` and `ApplyOneBuff` stopped dropping `MagicCritDamage` — **the fourth field channel**
  one of those two builders has silently lost.
- `SkillCsvSeed`'s `Descr.cs` learned `magiccritdmg`, declared ABOVE `magiccritrate` so that
  *"magic critical dmg"* is no longer read as the crit RATE. It had been, since the metric existed —
  invisible only because both numbers on the one row that used it were 30.

### Verified

`Game.sln`, `Game.Client.Unity`, `tools/BalanceMatrix` and `tools/SmokeTest` all build; the server
boots as `v0.133.0` (which is what proves the new startup guard passes); `SmokeTest` reports ALL
CHECKS PASSED; `SkillCsvSeed --check` is at its two pre-existing `BL-202` rows and nothing else.

## 2026-09-11 — 0.132.0: the Magus's 4th class, and a defensive proc that can finally reach the attacker

⚠ **NEW APK.** No protocol bump (nothing on the wire changed), but the client builds its Learn tab
locally from the compiled `ClassSkills`, and the Magus's table gains 236 rows.

`BL-192`. His `nuker 4th.csv` — 236 rows, *"so i think im done with nuker 4th"* — is built. It is the
**fifth finished 4th-tier file** after the healer, the buffer, the tank and the archer, and it leaves
only the warrior's two.

### Nineteen families simply continue, and six are new

One rung per level, 76 → 90, on the shared 4th-tier price ladder (6.5kk SP at 76 climbing to 80kk at
79, then **no SP at all** and gold 5kk → 100kk):

| | |
|---|---|
| **shared** | Anti-Magic (21-35) · Spellcaster Weapon Mastery (15-29) · Mage Armor Mastery (19-33) · Elemental Blast · Quick Blast · Elemental Wave (15-29) · Elemental Burst + Thunderstorm (rungs 4-6 @80/85/90) |
| **Human** | Arcane Wave (15-29) · Vampiric Bolt (20-34) · Arcane Void (4-7 @76/80/85/90) · Arcane Burst (2-4) |
| **Elf** | Frost Spikes · Frost Pierce (15-29) · Frost Burst (2-4) |
| **Demon** | Witches Curse · Witches Scarecrow (15-29) · Pyro Burst (2-4) |

The six new ones: **Mage Shield Mastery** @76, **Force Empowerment** @78/80/82, **Mana Barrier** @85 and
one **Spell Empowerment** per race @80/85/90.

**Mage Armor Mastery grows two columns it never had** — an M.Def *percent* (2 → 25%) and an MP-cost cut
(0 → 10%) — and all four of its moving numbers are the healer's 4th-tier robe rung to the digit. The one
thing that still makes it the nuker's own skill is `mpWhenRestored`, which resumes climbing at 60% and
reaches 70%.

**Mage Shield Mastery is a shield that cannot block.** His row: *"increase m.atk +5%, mp consumption
-10% and mpReg +10%, Pdef +100, but shield can never block (block rate x0)"*. No new primitive —
`BlockChancePct` is already a ×(1 + pct) channel, so −1 lands on exactly ×0.

**Mana Barrier already existed and had never been learnable.** A def in `Skills.Mage.cs` carrying his
exact numbers (70% of damage paid from MP at 0.5 MP a point, 30s) that **no class table taught** — an
orphan like Dispel Magic was. Its id moved to his `nuker_mana_barrier` rather than a second def being
authored. 🔴 Its **reuse was 30 seconds against his 300**: `CooldownTicks` is tenths, and a 30-second
reuse on a 30-second shield is a permanent one.

**Pyro Burst reaches burn tier 12, which nothing in the game can cure.** His three rows read
−100/−125/−150 HP a second at 70/72/75% HP-and-MP-received — that is the burn table's t10/t11/t12 line
verbatim, so the whole rider is the RANK and no damage number is authored anywhere. *"i want healers
holy blessing … to clence to t11. so pyromancer ultimate is uncurable"* — `DotTiers.Curable` refuses 12,
so the level-90 Demon Magus's burn is beyond Holy Blessing, an Antidote and everything else.

### 🔴 The engine gap: a defensive proc had nowhere to put its payload

His three Spell Empowerments are *"being attacked with 5% chance [do something] **to the attacker**"*.
Both halves of that machinery existed — `ProcOnDamaged` since the Sigils, `ProcVictimRungs` since the
archer stances — but `TryOnDamagedProcs` called `TryProcs` **with no payload target at all**, so the
victim arm was silently unreachable on that trigger. All three would have cost 150kk SP and done
nothing. The attacker is passed through now, and the victim arm learned to deal **direct damage** for
the Human's *"inflicts damage on attackers with power 47"* — the first victim payload in the game that
is a hit rather than a debuff, delivered through the existing `DeliverSimpleHit` so it brings fizzle,
magic crit and the PvE/PvP matrix with it. It cannot ping-pong: the proc's internal cooldown is set
before any payout.

### 🔴 The `BL-85` boot guard refused the build — again

Three Spell Empowerments, three rungs each, one buff key. Same shape as the warrior's two Battle
stances in 0.130.0, and the same answer the archer's three race stances already use: `SharesLadderKey:
true`. It is safe here for the reason it is safe there — a Spell Empowerment is one per RACE, so no
character can hold two, and sharing the key keeps them one family for a future group buff to compete
with.

### What was corrected in his file, and what was left alone

- 🟡 **13 TARGET cells read bare `self`** where the `scope/breadth` scheme — and all 47 other self rows
  in the same file, and every row of every other file — read `self/single`. Corrected; a missing breadth
  half is a formatting slip, not a different mechanic.
- 🔑 **The first 4th rung of all three race Bursts repeats the 3rd tier's last** (power 150, same
  rider, 100kk of gold), then climbs 150 → 200 → 250. Built as authored — and see the last bullet
  below for why it is a real rung and not a dead one.
- ⚠ **Pyro Burst's `(success chance x1.5)` is carried and is inert.** His 3rd-tier row has no such
  clause and these three do — almost certainly copied from the two Burst rows beside them. A burn's save
  is `DebuffSchool.None` (*"for burn nothing protects .. always land"*), so the landing branch skips the
  contest this number would modify. Carried so his cell and the code read alike, rather than deleted
  from his file over a number that cannot bite.
- ⚠ **Four families gain nothing at the 4th tier** — Calm Spirit, Restore Spirit, Phase Shift and
  Meditation have no row in his file. No continuations invented, same ruling that stopped Harmony of
  Speed at 58.
- ✅ **A TOGGLE'S HP DRAIN NOW LADDERS PER RUNG** — his ruling the same day (`BL-208`): *"Make togles to
  can change value of drain per lvl .. Some can drain more mp why some cant drain less hp?"*, and the
  asymmetry was exactly that. `SkillLevel.MpPerSecond` got its per-rung slot on 2026-08-27 and the HP
  half never did, so a stance whose drain is a ladder was charged rung 1's number at every rung — the
  identical bug the MP half had. `SkillLevel.HpPerSecond` + `SkillDef.HpPerSecondAt(level)`, and **both**
  readers go through it (the tick loop and the description card). Force Empowerment really drains
  **50 / 40 / 30** at 78 / 80 / 82. Holy Soul and Overpower Mastery are untouched: a rung of 0 inherits
  the def's flat number.
- ✅ **The Spell Empowerment rider's 10s duration and 10s proc cooldown are ratified** — they were mine
  (the archer stances' values), and he took them: *"I haven't written the duration and cd of empowerment
  debuff part 10/10 is good call"*.
- 🔴 **THE RACE-BURST @80 RUNG IS NOT A WASTED RUNG, AND THE REPORT WAS WRONG.** It was flagged as a
  100kk rung that buys nothing; his answer is the mechanic: *"it don't give power but it gives higher
  debuff chance .. the magic become lvl 80 not 74 .. (it won't fail anyway but atleast debuff will land
  more often)"*. `DebuffLandChance` reads the RUNG's own **learn level**, so an identical spell bought
  at 80 wins a level contest a 74 one loses — the same rule Witches Scarecrow's whole ladder runs on.
  A Burst cannot fizzle (`SureHit`), so the level term has nowhere else to show up: the rung buys the
  rider's landing rate and nothing else. Damage stays as authored until he playtests it.
- ❓ **"Decrease Mp Consumption" unqualified = BOTH channels.** Where he means one he says so (Spell
  Empowerment is *"magic MP consumption"*, the warrior's toggle was *"p.mp"*). The robe mastery, the
  shield mastery and Force Empowerment are unqualified, so they take both — which is also exactly what
  the healer's identical robe clause has shipped as since 0.107.0.

### `--check` gains a line and loses eight false alarms

`nuker 4th` earns its `Check.Specs` line and reads **clean** — all 236 rows verified in both directions.

The ladder-dip detector learned the **magnitude-only penalty ladder**. `mpcost` is authored both ways
(*"Decrease Mp Consumption with 5%"* and *"−15% Magic MP Consumption"*), so the reader strips the sign
and the existing "both values negative" exemption could not fire. The Magus's two stances are the first
ladders where the MP cost is a **price that shrinks** — Force Empowerment's 20 → 15 → 10% surcharge —
and every step of both was reporting as a typo. Gated on his own word *"increase"* sitting in front of
the phrase on both rungs, so a real discount ladder that falls still reports.

## 2026-09-11 — 0.131.0: the melee rogue's blows halve and learn to double; the burst stops failing on a DEX roll

⚠ **NEW APK.** No protocol bump (nothing on the wire changed), but the client builds its Learn tab
locally from the compiled `ClassSkills`, and the melee rogue's table gains Overpower at 40 and 76.

Seven fixes off one playtest message, all of them the melee rogue's except the last two.

### The stab ladders are HALVED — and every stab becomes a `[Double]` skill

*"cut dual 3rd/4th stab skills to have twice less power … now ~11k dmg on a 90 mob with 19k hp. And
78k mob hit for 8k. A bit too much. Let atleast this dmg to be a double dmg."*

Both halves are one change. Killing / Swift / Heavy / Venom Stab and Venom Burst each lost half their
authored power at both tiers, and all four stab families are now flagged `CanDouble`, so the number he
was measuring comes back on the Overpower roll instead of on every cast.

🔴 **HALVING THE POWER DOES NOT HALVE THE DAMAGE.** Power is a term *inside* the ratio —
`K·(atk·lvlMod + power)/def` — so the attacker's own P.Atk rides through untouched. Measured at 90 in
mythic gear (the new `BalanceMatrix --stab`): a Killing Stab went **6,970 → 3,978** (×0.57, not ×0.5)
and a **doubled** one lands **7,956**, about 14% *above* what the skill used to do flat. The floor came
down; the ceiling went up slightly.

| level 90, mythic, unbuffed | power | blow | vs 19.5k mob | DOUBLE |
|---|---|---|---|---|
| Killing / Swift / Venom Stab | 7,500 | 3,978 | 20% | 7,956 |
| Heavy Stab (×2 hits) | 5,625 | 6,460 per use | 33% | 12,920 |

### Overpower comes to the melee rogue — two rungs, not three

*"give duals 3rd/4th stabs a [double] flag and overpower passive but least than warrior @40 3% @76-7%
(1 rung less. If it's too low I'll give him the last rung at 80 — for now only the 2 rungs)."*

Same def, same ladder, later levels: `SkillLevel 1` (3%) at 40 and `SkillLevel 2` (7%) at 76. The
warrior's third rung (10%) is deliberately unreachable. The **price** is overridden per class
(`ClassSkill.SpCost`) — rung 1 costs 3,400 because that is a level-20 warrior's price, and SP here is
priced by the level you learn at, not by the ability.

### Venom Stab now hits as hard as a Killing Stab

*"make venomWeaver — venom stab to have the same power as killing strike (the new /2 dmg)."* The
Demon's old ×0.50 per blow would have compounded with the halving — half of a half, on the one
discipline with no Killing Stab at all. In practice the venom ladder barely moves (675 → 625 at the
first rung, nothing else); it is the other three families that halved.

### Venom Burst lands on a flat 80%, and nothing scales it

*"make venom burst to always have max 80% roof land rate independent on dex … the venom burst if fails
takes stacks and don't do dmg, so it burns twice."*

🔑 **What was failing was the RIDER, not the damage.** The burst has no blow gate and its damage always
landed — but it spends the pool in the damage arm and only *then* rolls its venom contest, so a lost
AGI-vs-CON roll broadcast `Fail` on the same cast that had just consumed ten stacks. A new
`SkillDef.FixedLandChance` replaces the stat curve outright (not a ceiling on it — a cap would still be
DEX-shaped underneath), and skips the per-skill multiplier and the target's CC resistances with it.
Total immunity still applies. The stacks are still spent on a failure: *"if it fails it fails."*

### Venom Burst becomes a stab — `BL-207`, and it replaces the 80% rule above

*"venom burst is a single stab skill that it's effective power depend on stacks count … it's one stab
x15k power … So venom burst also must land a double. Can we make venom burst to be with normal land
rate (30% like other stabs) and on fail not to take all stacks but to restore 3."*

It now rolls the **blow gate** like every other stab and **can Double**. It used to land always and
flat — no crit values at all — so this cuts both ways: much bigger when it lands, a basic swing when
it does not. A **failed** burst empties the pool and banks one cast's worth back (10 → 3 at the top
rungs), so a Demon restarts at 3 rather than 0. The flat-80% rider from `BL-204` is gone with
`SkillDef.FixedLandChance`; the cosmetic `Fail` it was working around is fixed at its source instead —
a burst that spent a pool no longer rolls a rider contest whose every branch was already a no-op.

🔴 **`damage × stacks` is not `power × stacks`.** The engine multiplies the *resolved damage*, and
power sits beside `atk·lvlMod` inside the ratio — so a full pool reads **10,850** at 90 (**2.73×** a
Killing Stab) where "one stab of 15k power" would read 6,970 (1.75×). Kept as built, because it is the
2.73× that lands his own race-parity table:

| race | 10s rotation | × its own plain stab |
|---|---|---|
| Elf | 3 Killing + 2 Swift | **5.00** |
| Human | 3 Killing + 1.5 Heavy | **5.44** |
| Demon | 3 Venom Stab + 1 Burst (9 stacks) | **5.47** |

### Reuse prices the three races apart

*"Increase heavy stab reuse to 7.5s, swift strike reuse to 5s. To balance the dmg~reuse for races."*
All three races carry the same power ladder now, so time is the only thing left to separate them:
Heavy Stab **7.5s** (two resolutions, 1.5 Killing Stabs a cast), Swift Stab **5s** (half the cast
time), Venom Stab keeps 3s (its damage is banked, not dealt).

### The two skill masteries move to the Passives group

*"all classes overpowerd/momentum is in buffs group not in passives in the skill window."* Overpower,
Arcane/Stab Momentum and Lasting Enchantment were `Category` Physical or Buff, and the Known tab heads
each block with that name. They are `Passive` now. ⚠ **Not** `double_mastery` — Overpower Mastery /
Momentum Mastery is a TOGGLE the player switches on, so it needs a bar slot and stays with the stances.

### A trap's circle is no longer painted over

*"traps orange-gold circle for the owner is under the red zone poligon and I see only the half that is
outside if any."* Exactly what the numbers said: the map paints spawn-zone discs at y=0.01, coloured
FIELD polygons at 0.02 (red at the high bands), town islands at 0.03. The trap disc sat at 0.01 and the
totem at 0.02, z-fighting the fill. The whole decal stack moved above every painted layer (trap 0.10,
totem 0.11, flash 0.13) and keeps its own order. **The rule going forward: ground paint is scenery,
decals are gameplay — every decal sits above every painted layer.**

### Tooling

`BalanceMatrix --stab [level] [quality] [--buffed]` — the melee rogue has never had a damage row
anywhere in the rig, because `--dmgmatrix`'s skill picker deliberately skips `BlowOnCrit` skills. It
prints power, the landed blow, per-use, the doubled number and the blow rate, against the
**zone-laddered** mob pool (`MobBaseStats.Hp × WorldPlan.HpScaleFor`) — 19,560 at 90, which is the
19k creature he quotes, with the ×4 elite beside it.


## 2026-09-11 — 0.130.0: his five CSV edits land — the warrior gets its two 3rd-class kits, and a blunt starts cleaving

⚠ **NEW APK.** No protocol bump (nothing on the wire changed), but the client builds its Learn tab
locally from the compiled `ClassSkills`, and this moves five class tables at once.

Five files of his, in the order he asked for them: *"U can apply changes to tank(3rd)/fighter(1st) -
then build(apply changes) to warrior 2nd - then u can build warrior/aoe 3rd whats there."*

### `fighter 1st.csv` — the ×1.1 MP regen becomes a skill of its own

His words: *"fixed fighter 1st all fighters to get a passive for x1.1 (i was mistaken when i though
it was a multi/additive mistake .. so all fighters now get their x1.1 mp regen)."*

🔑 **It had to MOVE to be "all fighters".** The ×1.1 lived on `fighter_armor_mastery`, and every
2nd-class armour mastery carries `Replaces: [fighter_armor_mastery]` — so the regen died at the class
change and survived only where the replacing mastery happened to re-state it. The warrior's did and
the rogue's did; **the tank's never has**. So `fighter_spirit_mastery` is a new one-rung passive at 5
(SP 160) that **nothing replaces**, and all four archetypes keep it for the rest of the game.

Two consequences, both of them removals of a number that would now be paid twice:
- `warrior_armor_mastery` drops its `MpRegenPct` on all twenty rungs — his rows no longer mention it.
- ✅ **The rogue's unauthored ×1.1 on rungs 1-4 is finally gone.** It was the same invented value the
  tank had (ruled out 2026-09-04: *"Remove the x1.1 mp regen from tank 20~32"*), flagged in the code
  and deliberately left because removing it was his call. He has now made that call from the other
  end. ⚠ Rung 5's `MpRegenPct: 0.8f` against a cell reading `mpReg +1.8` is a **units** question, not
  a duplicate, and is untouched — still owed a ruling.

### `tank 3rd.csv` — Final Defense grows a ladder

*"fixed tanks final_defence to have lvls."* Rungs at **40** (5/10/15% P.Def, 2.5/5% M.Def) and **52**
(7/14/21%, 3.5/7%) under the level-60 one, whose numbers are **unchanged** — a tank who reaches 60 is
exactly as strong as he was, and the two new rows are a cheaper on-ramp twenty levels earlier.
`Entity.FinalDefenceBonus` now reads a `[skill level][HP band]` table instead of three hard-coded
`return`s. Aggravated State re-priced to his 74k / 120k / 320k (all three rungs read 120k before).

### `warrior 2nd.csv` — Two-Hand Mastery is split, and loses its penalty

🔴 **Four columns left that skill and only two went somewhere else.** His rows now read
*"crit dmg +35; p.atk +13; With 2h Blunt: Allow basic attack to hit around in 150 range (max 2
targets)"* and nothing more:
- `acc +3` and `p.atk ×1.2` **moved** to a new **Warrior's Strength** (`warrior_strenght`) — same
  learn level, same weapon gate, its own SP row, and the 3rd class continues *that* ladder.
- `eva −3` and `p.def ×0.9` **are simply gone.** The penalty he cut to −10% in playtest-19 (*"I want
  a warrior in a heavy not to have lower defence than a mage"*) has now been cut the rest of the way.
- Top-rung flat P.Atk 20 → 22, so the 13/15/17/20/22 ladder no longer plateaus.

🔑 **THE BLUNT CLEAVE IS A NEW ENGINE CHANNEL.** *"Allow basic attack to hit around in 150 range"* —
`PassiveEffect.CleaveTargets`/`CleaveRadius`, riding a WEAPON MASTERY so the gate is the same one
every other number on the rung uses: put the mace away and the cleave goes with it. Each extra body
takes a **whole swing** through `ResolveBasicSwing` — its own miss roll, crit, block and on-hit
riders — not a share of one, and its own Retaliate and Kill so a cleaved kill still credits the drop.
`CleaveTargets` counts the primary target, which is what his "max 2" means.

Two new self-buffs, both continuing into the 3rd tier:
- **Battle Resilience** (36) — *"Increase resistance to Stun/Shock, Hold/Bind and Buff-Removal
  Attacks with 40%"*. 🔑 His sentence names **three** things and they are three different channels:
  stun and hold land through the ATK-vs-CON/WIT contest, defended per SCHOOL (`CcResistPhysical` /
  `CcResistMagical` — he named the effects, not a school, so both get the number), and Buff-Removal
  is the Cancel roll, which is its own.
- **Monster Knowledge** (32) — +5% PvE damage, 10 minutes, 5s reuse. 🔑 "PVE Dmg" is **all three** PvE
  channels (skill / magic / basic): his sentence names the context and says nothing about the source,
  and setting only the skill channel would exempt the basic attacks that are most of a warrior's
  damage between reuses. The three PvP bits stay off — that is his line too, by omission.

🔴 **`Precision` LEFT THAT FILE — AND NOW THE WARRIOR TOO** (`BL-201`). The row's absence was raised
rather than acted on (deleting a combat mechanic on the strength of a deleted row is not a
transcription), and he ruled the same day: *"Delete precition ... Now he have +9 which kills 50% of
the rogues evasion anyway (no need for another hit floor) - leave the mechanic but not the skill on
warrior."*

🔑 **ACCURACY IS THE REPLACEMENT, and it measures out.** Warrior's Strength now carries up to **+9
accuracy**, and the resolver is one line — `miss = 5% + (EVA − ACC) × 1%` — so nine points *is* nine
points of a rogue's evasion lead. The floor existed to stop an evasion-stacked rogue locking a warrior
out entirely; `--dmgmatrix` §E1 puts a buffed champion at **16% miss** against a light rogue at 36,
nowhere near the 90% ceiling the floor capped. It was a second layer that never bound.

`FloorPassiveFor` no longer names `Archetype.Warrior`. ⚠ The `Precision` def and
`PassiveEffect.HitFloor` both **stay**: "leave the mechanic".

🔑 **AND NOTHING UN-GRANTS IT.** A strip in `AutoLearnCoreSkills` was written first — an auto-grant is
a plain assignment, so it is permanent, which is why `BL-143` needed exactly that for the tank's
Backlash — and he deleted it the same afternoon: *"no1 except me plays this game for now .. so no
lingering warriors when I clear a db .. So no point of migration type to remove a skill from some1.
They will never have it in the 1st place."* **Pre-release, a `game.db` delete IS the migration.**
Un-grant code is only worth writing for a skill that shipped to somebody who is not him.

### `warrior 3rd.csv` + `war_aoe 3rd.csv` — both disciplines get their kits

*"I made some passives and buffs for warrior/aoe 3rd - they are missing only teir dmg and control
(active dmg) skills."* So this is the **passive and buff half** of two kits, and the damage half is
still owed — the derived `war_sundering_blow` stands in until it lands.

🔑 **THE WEAPON IS THE WHOLE SPLIT.** The **Ravager** trains a two-handed SWORD
(`warrior_sword_mastery`, +52→150 P.Atk and +145→615 crit damage over fifteen rungs) and keeps both
Battle stances. The **Warlord** trains a two-handed BLUNT (`warrior_blunt_mastery`) whose basic attack
cleaves **5 → 10** bodies, has no stances at all, and pays for it with twenty fewer points of P.Atk at
every rung. Everything else is shared rung for rung because both files author it identically.

New, and shared: **Final Stand** (the P.Atk twin of the tank's Final Defense, read live off the HP
bar) and **HP Regeneration** — the first passive in the game that pays for **sitting**
(`PassiveEffect.HpRegenSitting`/`MpRegenSitting`, flat, added outside the stance multiplier because
sitting already pays ×1.5 and folding his +2.0 inside would have quietly made it +3.0). A warrior has
no other MP regen of his own, so that half is what lets him sit ten seconds between pulls instead of
thirty.

🔴 **`war_sword_mastery` IS GONE** — `warrior_sword_mastery` is its successor, a rename plus his own
fifteen-rung ladder, not a second skill. Safe to delete only because it was five days old, lived on
these two disciplines alone, and reaching 40 as a Ravager takes longer than it existed. The derived
tank-copy armour rungs went with it; his own shape is nothing like them (no ×1.07 P.Def, no −2
evasion, and a light branch that keeps growing instead of freezing at the level-36 rung).

### What the tooling caught, and what it still says

Both files got their `Check.Specs` line the day they landed, **unfinished** — `BL-197`'s lesson was
that a code side half-authored and half-derived is exactly where the two drift apart silently. It
paid immediately:

- 🔴 The **`BL-85` startup guard** refused the build outright: both Battle stances now ladder on one
  shared `battle_stance` key. They are the deliberate "one or the other, never both" pair, so both
  take `FlatRank: true` — the documented opt-out, same as Great Might / Great Bulwark.
- 🔑 The checker learned that **a weight clause is an ADDITION, not a total**. His row reads *"P.def
  +40 …; Heavy: P.Def +10"* — the leading clause is every trained weight and `Heavy:` says what heavy
  adds on top. The code stores the total (50), so all fifteen rungs reported as wrong when the code
  was right and the reading was not.
- 🔑 It also learned the **sitting-regen clause**, which it had been reading as the main `hpReg`.
- Two of his cells fixed in place, both making a row agree with itself: the Battle Presence WEAPON
  column (empty, while its own DESCR says *"requres 2h sword/blunt"*) and `+1,8` → `+1.8` (a
  Bulgarian decimal comma the reader parsed as `1`).

✅ **TWO LADDER DIPS FOUND AND RULED THE SAME AFTERNOON** (`BL-200`). Both were raised rather than
guessed at — the CSV is never quietly retuned — and both came back *"Typo on both"*:
- `warrior_sword_mastery` rung 8 (level 60) read **+91 P.Atk** between 94 and 108 → **101**, so the
  +7 step now runs unbroken 52 → 150.
- `battle_defence` went **×2.5 at 43 → ×2.3 at 52**, making the 74k-SP rung weaker than the 42k one
  → **×3**, a clean +0.5 ladder (×2 → ×2.5 → ×3).

Code array and CSV cell moved together, in this commit.

🔑 **`--check`'s LADDER DIP found both, on files that are not finished** — which is the whole argument
for giving a half-authored file its `Check.Specs` line the day it lands rather than the day it is
done (`BL-197`). Two typos caught the same afternoon they were written, in rows nobody had played.

### Measured, not derived

🔴 **The rig's flagship level-74 comparison had no discipline on the champion** — `BuildPlayer`
teaches nothing above the 2nd class without one, so "is the warrior keeping up with the nuker" was
measuring a level-74 **Champion** against a full **Magus**, two class tiers apart, and had been since
`nuker 3rd.csv` landed in August. With `Discipline.Ravager` passed and his kit built:

| | P.Atk | total DPS | vs nuker |
|---|---|---|---|
| champion, no discipline (what the rig measured) | 815 | 604.8 | 0.95× |
| Ravager with his kit (what it measures now) | 1079 | 719.4 | **1.13×** |

…and that is **without** the damage skills he still owes. `--check` is green on all seventeen finished
files; the two warrior files report only the stand-in Sundering Blow and the two dips above.

## 2026-09-11 — 0.129.0: the channel becomes a real channel — it roots you, it shows a bar, and a second skill queues

✅ **No new APK, no protocol bump.** Every part of this is server-side: the volley reuses the cast
bar's own message, and the client already roots, draws the X and cancels off "am I casting".

His report, on Arrow Barrage: *"it does the 10 times dmg .. just i have 2s to cast it .. i cast it and
it does then 10times dmg .. and in that time i can move .. but when i cast another skill it cancels
... the idea with the channel skill is it locks me in place ... its high dmg skill that requires
strategy to use not blindly click and run"*.

The volley worked; the **commitment** did not. Three gaps, and each one made the skill strictly better
than it was designed to be — you fired ten arrows and then walked away with the payment already made.

### The volley roots you

`Entity.IsCommitted` — a cast in flight **or** a volley in progress — is now what the movement doors
ask, in place of `CastingSkillId`. (Distinct from `IsRooted`, which is the Root debuff: that one is
done to you, this one you chose.) It gates the move tap (`HandleMove`), sitting down, the item-use
cast, the auto-hunt loop, and `TickFollow`.

🔑 **TickFollow is the one that had to be found rather than reasoned about.** It writes a destination
*every tick*, so refusing the player's own taps would have achieved nothing: a following archer would
have been walked through his own barrage by his auto-repath. The follow is not dropped — it resumes
by itself when the last arrow leaves.

### The volley has a bar

The channel pushes the **same `CastInfo` message** the cast does, for `shots × interval`, and every
way a volley can end takes it down again — which is why `EndChannel` is no longer static: it owns that
bar. `CancelCast` could not do it, because its own clearing push sits past an early return that fires
for exactly this case (a channel runs with `CastingSkillId` already null).

🔑 **The bar is also the entire client change, and it is why there isn't one.** The cast bar's X and
the skill slot's X both key off "am I casting", and both already send `CancelCast` — which has ended
volleys since the day the channel shipped. Giving the volley a bar handed it the cancel UI for free.

⚠ Sent when the volley STARTS rather than added to the cast's seconds up front: the length is only
known to be real once every gate has passed, and a bar that kept running after a barrage failed to
fire would be advertising a root the player does not have.

### A second skill CHAINS instead of killing the arrows

*"clicking on next skill dosnt cancel the cast just mark it in the queue"* — which is the playtest-27
rule the cast has followed for a month: the same skill cancels, anything else follows. A volley fell
past that test (it is not a cast and holds no queued skill) and died on the belt-and-braces
`CancelCast` at the foot of `BeginSkill`. It now takes the same branch, compared by the **wrapper's**
id (`Entity.ChannelWrapperId`, new) — the arrow's id is an engine detail and is on nobody's bar.

🔴 **And the chain had a second bug waiting behind that one.** `TryStartChainedSkill` is called the
instant a cast LANDS — and a barrage's cast landing is precisely the moment its volley BEGINS. The
chained skill would have started on top of ten arrows still in the air, and its re-entry (`fromChain`,
so it skips the cancel/chain test) would have reached that same belt `CancelCast` and thrown the rest
of the volley away. The "not yet" test now stands **before** the chain slot is cleared, unlike the
failures around it: those are *"it was tried and it did not work"*, this is *"it is not its turn"*,
and consuming the chain there would have silently swallowed a skill the player was promised. The
volley's own end calls it again — the last arrow is when the promise comes due.

A volley that ends early because its target died takes the chain too; a cancel, a stun or an interrupt
still clears it, exactly as for a cast.

⚠ **Twin Arrows is the same mechanism and gets the same treatment** — a 0.4s root and a 0.4s bar.
Uniform on purpose: the channel is one rule, not one skill's rule.


## 2026-09-11 — 0.128.0: venom stacks stop needing two rolls; the buff limit becomes a collection

🔴 **New APK required. Protocol 37** — `TrapList` is a new server→client message, and the reuse,
cast-time and skill-description changes below are all rendered from the client's compiled catalogue.

Six playtest asks, then his two rulings on them and the close of `dual 4th.csv`.

### `BL-199` — a venom stack is the record of a blade going in

*"Now venomweaver almost cannot stack venom... Stacks should be independent of dot... So each landed
venom blow adds stacks that do not do nothing just stacks, and try to do a venom debuff that do dmg
depending on those stacks and when venom burst is used it takes with it the stacks + the dot debuff"*

Four separate things were wrong, and each one on its own would have been enough to make the
discipline unplayable:

- 🔴 **A STACK NEEDED TWO ROLLS TO COME UP.** Stacks were added inside the DoT's AGI-vs-CON contest,
  so a Venom Stab had to land its blow *and* win the contest to bank anything. They are banked by the
  **strike** now — any resolution of the damage arm that connects, including a blow that fell through
  to a basic swing and hit. Only a clean miss banks nothing. The venom debuff still lands on its own
  contest; losing it costs the damage, never the pool.
- 🔴 **THE VENOM TICKED FOR ONE STACK NO MATTER WHAT.** A stacking DoT is two statuses — the damage
  buff, pinned at one stack, and a hidden counter that holds the real number. `TickDots` read the
  pinned one. The fold existed in exactly one place, `PushTargetBuffs`, so **the bar showed "x7" while
  the damage was x1** — and the bar was the half telling the truth about intent. One helper,
  `DotStacksOf`, is now read by both.
- 🔴 **THE BURST LEFT THE VENOM RUNNING**, so spending ten stacks changed nothing visible: same debuff
  on the bar, a number that looked like an ordinary stab. It takes the DoT with the pool now.
- **The burst always did multiply by stacks** — he was right to suspect it and wrong about which half
  was broken; it was the pool being empty. It now says so: *"Venom Burst detonated 7 stack(s) — ×7
  damage."*

🔑 **`BL-197` — ONLY A SUCCESSFUL STAB BANKS.** "Each landed blow" has two readings on a `BlowOnCrit`
skill and he settled it the tight way: *"only the succesfull stab .. not the failed/basick attack
one"*. His reason is the better design and it was not the one I had: the blow RATE is meant to be the
knob the player chooses with — *"if i chose to use perfect_strike i land more ophen i stack faster but
for less dmg ... if i use brutal_strike i land less ofthen i stack slower but do more dmg"*. The @80
pair is only a real choice if the rate it moves is also the stacking rate. So ONE roll gates a stack,
and the player has a buff for it.

### `BL-195` → `BL-198` — the buff limit is a COLLECTION, not a timer

*"let's make all buffs that are not 20min and not harmonies or marks (the current ones) not enter the
limit ... Like bow expertise and bow blessing/egc to count towards limit but bow Ferocity/swiftness
don't."* He offered a self-vs-party split as the alternative and rejected it himself.

Built as a 20-minute duration test (`BL-195`) — and he overturned the shape the same day, correctly:

> *"it should not work only on timer ... the limit should have an id collection ... i gave the
> duration as filter not as solution ... if one buff a 10 min buff and it doubles it probanbly break
> en enter the count .. but it shouldns"*

🔴 **His counterexample is unanswerable.** `BL-190`'s `DoubleDurationRate` doubles a landed duration
**on a roll**, so under a duration test a 10-minute buff that rolled a double would start costing a
square — the same buff on the same character, decided by a die. **A property of the skill must never
read a number something else in the game is allowed to multiply.**

So membership of `SkillCatalog.BuffLimitIds` is now the whole test. The collection is **derived, not
typed out** — a typed list goes stale and whole tiers vanish from it — from two sources that between
them are exactly his enumeration:

1. **the two shelves unioned**, which is the same universe the admin Buffs menu's four drawers come
   from: every single, group, harmony and Mark, at any duration;
2. **every other `BuffRow.Buff` skill whose AUTHORED duration is ≥ 20 minutes** — the archer's Bow
   Expertise / Blessing / Spirit, and anything authored that long later, with no edit anywhere.

`BuffRow.Buff` in rule 2 is what keeps the three **runes** out, which closes the other half of
`BL-198`: they run an hour, but a ~1/s reconciliation loop owns them, so evicting one frees a square
for a fraction of a second and then puts it back. Potions and scrolls are `Consumable` too — but their
CHILDREN are in via rule 1, because a potion of Might and a cleric's Might are the same buff from
different bottles.

📐 **`dotnet run --project tools/BalanceMatrix -- --bufflimit`** prints both halves — 221 buffs that
cost a square and 149 that do not. That listing is the answer to his *"U can ask me for some that i
didnt meantion"*: reading a derived list beats remembering the buffs.

🔴 **Found in passing: a venom stack counter was eating one of its VICTIM's buff slots.** The hidden
counter is a `BuffInstance` with no effect and `Internal = true`, so no debuff test caught it and it
sat in the default buff row — invisible, and in PvP able to evict a real blessing. `Internal` is an
exclusion now. It went unnoticed only because banking a stack used to need two rolls.

### `BL-196` — cast SPEED and cast TIME are two different channels

*"the elf archer spirit mastery should increase the phisical cast speed as well. It should not
increase cast speed as stat for mages only. It should increase the end cast time."*

Spirit Mastery is the **archer's own** party proc, and its 20% rode `BuffCastSpeed` — the mage's stat.
A physical skill is paced by ATTACK speed, so the buff reached every mage in the party and did nothing
at all for the Elf who cast it. His own CSV cell says `p.skill cast time`, which no cast-speed channel
could ever have expressed.

`SkillDef.CastTimePct` is the new channel and it multiplies the **finished** cast, after the 333 model
has already picked whichever stat paces the skill — exactly where his formula puts it:
*"(baseCastOrAttackSpeedValue x castOrAttackSpeedBuffs x castOrAttackSpeedDebuffs / 333 or whatever) x
castTimeDebffs x spirit_mastery and other cast time buffs"*. A negative value lengthens a cast, which
is the `castTimeDebffs` half; nothing authors one yet.

### The archer's five ultimates get a reuse ladder of their own

*"archers arrow barrage to have 30s cd, bleeding arrow and other class analogies to have 15s, heavy
arrow 10s -> now with all the cd reduction 25k dmg skill is used every 3~4s"*

His file gave all five 10s. **Arrow Barrage 30s · the three race ultimates 15s · Heavy Arrow 10s**
(unchanged), in the code and in `archer 4th.csv`. The cooldown-reduction stack is deliberately left
alone — the lever is the base number, not the buffs he already bought.

### Sprint is physical — and so is every other physical skill, for reuse

*"rogues sprint is physical not magical"*. Sprint already carried `PhysicalCast`, so it was physical
for cast pacing and for silence. `Entity.CooldownReductionFor` was the **last call site still asking
`Category`** — a ROLE tag — so Sprint, the three traps and every physical stance were filed under
MAGIC reuse, and Bow Blessing's *"−20% physical reuse"* did not in fact reach *"every skill an archer
owns"* as its own comment claimed. It asks `SkillMath.IsPhysical` now, like the speed model and
silence. Same mistake `BL-132` fixed once already.

### `dual 4th.csv` closes — three race passives, three race ultimates, a fixed Vanish

*"I added 3 new ulsitmate skills, 3 new passiives for identity for each race and make vanish cooldown
fixed. With that duals 4th is finihed (untill dmg is rly tested)"* — his rows, built as authored.

**ONE AXIS PER RACE, CARRIED AT TWO STRENGTHS.** A small permanent passive at 80/85/90 (5 → 7 → 10%)
and the same defence turned up for ten seconds at 83. That pairing is what makes them identity rather
than six unrelated numbers:

| race | passive (80/85/90) | ultimate (83, 10s, 90s reuse) |
|---|---|---|
| **Human** | `Anti-Magic` — magic resistance +5/7/10% | `Magical Armor` — +30% magic resistance |
| **Elf** | `Anti-Physical` — physical-SKILL evasion +5/7/10% | `Dodge` — 30% to evade a physical skill |
| **Demon** | `Duel-Expertise` — PvP damage +5/7/10% | `Demon Contract` — +25% PvP damage |

⚠ *"p.skill evasion"* is `SkillEvadeChance`, not Evasion — the grant `BL-06` left as the only way a
physical SKILL can be dodged at all. ⚠ `PvP.Dmg` is unqualified in his cell, so it rides all three PvP
channels: a dagger's damage comes from skills, basics and Venom Burst, and covering one would read as
broken on the other two.

**Vanish is `FixedCooldown` now.** Two minutes is the price of thirty seconds untouchable, and the
reuse stack a 4th-tier rogue carries — physical reuse buffs, the Sigils, Stab Momentum's reset roll,
and Overpower Mastery doubling that roll — was aimed straight at it. A vanish on a 40-second real
reuse is not an escape, it is a movement mode. The flag skips reduction entirely, so it is immune to
whatever is added to that stack later; raising the number would not have been.

**Wording, which is the half he asked for separately:**
- 🔴 **Venom Stab and Venom Burst still advertised `atk -15%; def -15%`** on all 30 rows — the
  per-skill venom rider, dead since `DotTiers` made the rider a property of the (kind, tier) table.
  The real tier-10 venom is 20 hp/s **per stack** and −10% P.Atk/M.Atk, with **no** defence cut at all
  (*"for now no dot will decrease def"*). Both halves of that row were wrong, in opposite directions.
- **`double_mastery` is `Overpower Mastery` for the rogue too** — *"dagger double_mastery is with name
  Overpower Mastery not Momentum Mastery"*. The rename was mine and it was the wrong instinct: a
  `DisplayName` is for when the flavour genuinely differs, and the toggle does the same thing for the
  rogue that it does for the warrior. `Stab Momentum` keeps its override, because there the base
  mechanic really is different from the Magus's Arcane Momentum wearing the same id.
- **Three derived families advertised the wrong DURATION** (Venom Stab/Burst 0 instead of 30s, Swift
  Stab 0 instead of its 5s rush).

🔑 **`dual 4th.csv` EARNED ITS `Check.Specs` LINE**, and that is what found the last three: the file is
walked now, and it reports **no discrepancies** against the code — 18 files green. Most of the file is
still DERIVED rather than authored, and the DAMAGE is the part he has not signed off (*"untill dmg is
rly tested"*), but a derived half that silently drifts from the code is exactly how `atk -15%` survived
a whole chronicle.


### Traps: an arming time, a radius that means something, and you can see your own

*"a mob start to walk with binding trap debuff. Also it don't say 'resisted' and for every trap the
owner should see it where he placed it so he can lure the enemy to it. Also traps should have arming
time ... Add to traps 2s cast time."*

- 🔴 **A TRAP ONLY EVER HIT ONE BODY.** It is authored `target/aoe` with a 400 reach and its own text
  says *"holds the enemis in range"* — plural — but only the nearest was delivered to. A pack walking
  over a Binding Trap had one held and **the rest strolled on**, which is what he saw. It catches
  everything in the circle now. Same mistake AoE taunt (`BL-123`) and the AoE pull (`BL-154`) each
  made once.
- 🔴 **A RESISTED TRAP SAID NOTHING AT ALL** — no float, no line. `DeliverSimpleHit` (traps and boss
  slams) had no `else` on its contest, so the only honest reading from the floor was "the skill is
  broken". It broadcasts `Fail` now, like the cast path it shares the contest with. That path had also
  drifted on WHICH stats contest: a bleed or venom is an AGI roll, and for any DoT the FAMILY decides
  what saves — a Bleeding Trap was rolling ATK-vs-CON while the identical arrow rolled AGI.
- **You can see your own traps.** New `TrapList`, owner-only — a totem is ground you want your party
  standing in, a trap is ground the enemy must not know about. Amber disc at the real trigger radius,
  breathing on the opposite phase to a totem so overlapping circles still read.
- **2 seconds to arm**, in the code and in both archer CSVs (his cells read 0).

🔵 **Still open on the trap report:** the ENGINE holds a rooted mob at speed 0 — `EffectiveSpeed`
returns 0 for `IsRooted` before anything else, and `MoveTowardTarget` is the only stepper in the game.
So *"a mob start to walk with binding trap debuff"* is best explained by the one-victim bug above.
Worth one re-test now that the trap catches the whole pack and says when it is resisted.
## 2026-09-10 — 0.127.0: his DoT table lands, and Burn becomes a real family

🟡 **New APK recommended, not required.** Protocol stays **36** — nothing on the wire changed. The
debuff bar's text comes from the server and is correct on an old client; only the *skill-detail*
descriptions, which the client renders from its compiled catalogue, would read stale.

His table arrived as `docs/data/dot_table.csv`, and the rulings that came with it turn a damage
lookup into the whole DoT model.

### Both halves of a DoT belong to the (TYPE, TIER) table

*"lets make them as authored ... remove the dot side effect from the skills"*. So the damage AND the
rider are properties of the family, and every skill that delivered one was stripped:

| type | tier 1 → 11 (dmg/s **per stack**) | stacks | side effect | saves on |
|---|---|---|---|---|
| Venom | 5,5,7,7,9,9,10,10,15,15,20 | **10** | −10% P.Atk and M.Atk | CON |
| Poison | 20,30,40,50,60,70,90,110,130,150,200 | 1 | −15% attack and cast speed | SPT |
| Bleed | 20,20,40,40,60,60,80,80,100,100,150 | 1 | −20% move speed, every rank | CON |
| Burn | *(10/11/12 only)* 100,125,150 | 1 | −70/72/75% HP **and MP** received | nothing |

Consequences worth knowing:
- **Every bleed now slows 20%.** The archer's trap authored 15% and Bleeding Arrow 30%; both are gone.
- **Venom no longer lowers defence** (*"for now no dot will decrease def"*) and its −15% atk became
  −10% off **both** P.Atk and M.Atk — one flag does both, `DebuffAtk` wraps `EffectiveAttack`,
  `EffectiveMagicAttack` and `EffectiveBasicAttack` alike.
- **Only venom stacks.** `DotTiers.MaxStacks` caps the family, so a bleed cannot be authored into a
  stacking one. ⚠ The three orphan Venomweaver builders (Rupture / Toxic Sting / Envenom) still ask
  for 10 and are now capped to 1 — harmless, since **no class table grants any of them**.
- The rider does **not** scale with stacks: `BuffInstance.Percent` sums magnitudes and never
  multiplies by `Stacks`. That is the cheaper of the two shapes he offered and it is now documented.

### 🔑 Burn is a real family, and it needed a FIELD because the enum is full

`1L << 62` is the last free `SkillEffect` bit and it is taken, so `AnyDot` could never grow a fourth
member. **`DotKind` is a field on `SkillDef`**: a Burn skill still carries `SkillEffect.Poison` for
membership — `AnyDot`, the buff bar, what a cure may strip — and declares `DotKind.Burn` for its
damage, its rider and its save.

**Pyro Burst is Burn tier 10, and that is not a guess:** its authored `DotPower: 100` and
`MpReceivedPct: 0.70f` are his tier-10 row exactly, written months before the table existed.

- **Nothing saves against a burn** (*"for burn nothing protects .. always land"*). He offered the hack
  himself — *"code success chance x9999"* — but a contest that cannot be lost is better expressed as
  no contest: a ×9999 would still be scaled by `CcResist` and the per-school blessing and could come
  back under 1. `DebuffLandMod: 1.5f` is gone from Pyro Burst, and its CSV row with it.
- **`Cancellable: false` is gone from Pyro Burst**, which is a real change: it made *every* tier of the
  skill uncurable, where the rule is now the tier's. Pyro Burst at 10 **is** curable.

### Cures: Antidote to 10, Holy Blessing to 11, nothing to 12

*"no no antidot stays to tire 10 ... tire 11 is cured buy the skill holy_blessing -> removes any
debuff + dots to T11 .. T12 or debuff that is uncurable is not removed"*.

Antidote is untouched. **Holy Blessing keeps its "any debuff, no rank ceiling"** — that half is
correct as authored — and the T11 wall is the *ailment's*: `DotTiers.Curable` refuses tier 12 to every
cure in the game, so it cannot be lifted by mis-authoring a cleanse. Its description no longer
promises "at any rank".

### The debuff bar describes itself

A DoT's bar text is generated from (kind, tier, stacks) in his format — `Burn T12; -150hp/s; Decreases
Hp/Mp Received with 75%; Uncurable;` — because the skill no longer knows any of it, and because the
stack count changes while it runs.

⚠ Tier 12 has no skill yet. Pyro Burst is 10; **11 and 12 arrive with the nuker's 4th kit (`BL-192`)**.

## 2026-09-10 — 0.126.0: the enemy's debuffs and stack counts

🔴 **NEW APK REQUIRED — `ProtocolVersion` 35 → 36.** A new server→client message.

*"i cannot see stacks on enemy (need to see debuffs+stacks)"* — and the reason: *"so i know when to
burst"*. A venom pool he could not see was a burst he had to guess at, which is also why the burst
itself read as *"dont do nothing .. or atleast dont show that it does"*.

### There was no wire message for another entity's buffs at all

Not a filter to relax — nothing existed. `BuffUpdate` only ever carried your own bar; the party roster
carried debuff NAMES for members; an enemy carried nothing. So this adds the whole path:

- **`TargetBuffUpdate(TargetId, BuffDto[])`** — the enemy-side twin of `BuffUpdate`.
- **A selected target now exists server-side.** Selection used to be purely client-side
  (`GameBoot.TargetId`), which is precisely why nothing here could answer "what is on the thing he is
  looking at". The client sends `SetTarget` on every change; the server keeps `Entity.UiTargetId`.
  ⚠ `InspectTarget` is a different thing and stays what it was — a one-shot pull for the stats sheet,
  not a subscription.
- **Pushed once a second**, off the same `secondTick` as his own buff bar, and **only when the list
  actually changed** (`LastTargetBuffSig`) — a selected mob usually stands there with nothing on it and
  that has to cost nothing. Selecting pushes once immediately so the row fills on the tap.

### 🔑 The stack counter is folded into the debuff it counts

On the server a stacking DoT is **two** buffs: the damage effect keyed on the skill's `BuffKey`, and a
separate `Internal` counter keyed on its `StackKey` (`ApplyDotStack` deliberately pins the damage
effect at `maxStacks: 1`). The counter being `Internal` is what made the venom pool invisible on every
bar. Rather than expose it as its own row, its count is merged onto the row a player already
understands — so the HUD reads `Venom x7`, not `Venom` beside `Venom (stacks)`.

### The client

One line under the target frame's detail row, debuffs first (red) then buffs (green), stack counts
shown only above 1. Ellipsised rather than wrapped, and the panel grew 160 → **186px** to pay for the
row: the action buttons are bottom-anchored, the detail row is top-anchored, and anything inserted
between them eats the gap two earlier playtest fixes bought.

⚠ `GameBoot.TargetId` is a real property now, not an auto-property — that is what makes every existing
assignment site (tap, tab-target, auto-hunt, the clear-on-death rule) notify the server for free.

## 2026-09-10 — 0.125.1: the DoT layer was ticking for the skill's whole Power

🟢 **SERVER-ONLY — your 0.125.0 APK is fine.** Protocol stays **35**, and the version label does not
gate anything once a client sends a protocol number.

Playtest finds, four of them with one root cause each.

### 🔴 Every DoT in the game ticked for its DIRECT HIT's power

*"bleeding trap is a bleeding arrow that does 15k dmg each second ... which is increadibly op"* ·
*"i think all dots are OP ... venomancer dot does 7500"*.

`SkillDef.DotPowerAt` fell back to the skill's own **`Power`** when no `DotPower` was authored — and
`DotPower` is authored on **exactly one skill in the catalogue** (Pyro Burst). Every other DoT
therefore ticked for the number meant for its direct hit: **flat, undivided by defence, once a second,
for the whole duration**. Bleeding Arrow (power 15,000, 30s) dealt **450,000** from a single arrow.

🔑 **A skill's Power is its direct hit; a DoT rider is a second number.** Inferring one from the other
is the bug, and the fallback is gone.

**The replacement is `Game.Shared/Skills/DotTiers.cs`** — a (type, tier) → flat damage-per-second
table, to his model: *"ill write u each type each tire what dmg it does .. and depending on
dmg-magic/phys and it does it as flat dmg ... just the landing rate depends on stat"*. So flat damage,
no defence division (correct and deliberate), the channel already carried by `DebuffSchool`, and the
landing contest untouched.

🔵 **The numbers are his and are not written yet, so every DoT ticks for 0** — following his own
precedent on the masteries (*"Nobody — dead until you author it"*). The server logs a warning at boot
while the table is empty. An invented placeholder gets mistaken for a tuned number and ships; an inert
bleed gets reported.

### 🔴 Arrow Barrage paid a fifth of its MP and had no reuse

*"barage have no cd"* — and it was worse than that. The channel branch sat ~80 lines too early in
`ExecuteSkill`, and its `return` jumped over the **80% finish MP**, the HP cost **and** the cooldown.
The barrage charged only the 20% initial (~42 of its 208) and started no reuse: a spammable ultimate
at a fifth price. The block now sits beside the trap and totem branches, past every gate a cast owes.

⚠ `CancelCast`'s note that *"the wrapper's cooldown was already started when its cast landed"* had
been false since the channel shipped.

### 🔴 No area skill could hit a training dummy

*"barage ... does no dmg to a training fummy"*. `EnemiesInRadius` opened with
`if (e.Dead || e.TrainingDummy) continue;` — so Barrage's `EnemiesInRadius` arrow swept an empty set
and ten arrows resolved against nobody. **Every AoE in the game was untestable on the one thing built
for testing it.** A dummy still takes no HP (GodMode); what it does now is show the number, which is
what it is for — its bar was never the readout, it regenerates 10,000 HP/sec.

### 🟢 The traps are fine — nothing was owed

*"archers dont have a trap skill"*. Each archer race gets exactly one, in the code and in his CSVs:
Human = Poison Trap, Elf = Binding Trap, Demon = Bleeding Trap, from 40 and again at the 4th. He was
on the Demon, which holds both Bleeding **Trap** and the level-85 Bleeding **Arrow** — the 15k/sec
was the Arrow. `Mighty Blow`-style naming confusion, nothing more.

### 🔵 Still owed: seeing a target's debuffs and stacks

*"burs dont do nothing .. or atleast dont show that it does"* · *"i cannot see stacks on enemy"*.
Same root: the stack counter is `Internal`, and more fundamentally **there is no wire message for a
target's buffs at all** — `BuffUpdate` only carries your own. Needs a selected-target concept on the
server, a new DTO, a protocol bump and a new APK. Its own increment.

## 2026-09-10 — 0.125.0: a blow that misses its mark is a NORMAL ATTACK (`BL-193`)

🔴 **NEW APK REQUIRED** — the skill-detail line changed and the client builds it locally from the
compiled catalogue. No schema change, protocol stays **35** (`SkillDef` never crosses the wire).

*"can we make if a blow fails to hit as normal atack (with crit chance and everithing)?"* — and on
what it replaces: *"we remove the 10% wiff and floor or whatever .. if it missies or is blocked so be
it ... its a normal baisc attack"*.

**`BlowFailFraction` is deleted from the game.** A dagger blow that fails its landing roll used to
pay a flat fraction of the skill's damage — 10% at the 1st/2nd tier, **1%** at the 3rd/4th — which
could neither crit, nor double, nor be blocked. It now resolves as an **ordinary basic attack**: full
damage off `EffectiveBasicAttack`, its own accuracy roll, its own crit, its own block, and every
on-hit rider a real swing carries (melee vamp, mana vamp, reflect, interrupt, procs).

🔑 **THE ORDER IS THE DESIGN.** The blow gate is now rolled **before** the skill's own miss roll. Put
it after, and a failed blow would be gated twice — once by `SkillEvadeChance` and again by the basic
attack's accuracy — which is not "as if I never used the skill". Each branch now carries exactly one
miss gate.

🔑 **The fallback SHARES the basic-attack body, it does not re-implement it.** `ResolveBasicAttack`
was split so its resolution half is `ResolveBasicSwing`, called from both places. A fallback with its
own copy of "a basic attack" drifts from the real one the first time a rider is added to either side.
The floating text still names the skill that fired; that is a label, nothing mechanical.

**Measured, not derived** — `BalanceMatrix` §C1 grew a `gate%` column and a `fail OLD / fail NEW`
pair, so the change is readable directly:

| lvl | gate | fail OLD | fail NEW |
|---|---|---|---|
| 20 | 30.0% | 64 | 174 |
| 28 | 30.0% | 76 | 150 |
| 36 | 30.0% | 91 | 140 |

That is **+5% to +17%** expected damage per stab at the 2nd tier, and more at the 3rd/4th where the
floor was 1% — against a level-90 tank a basic swing is worth ~80 where the floor paid single digits.
The rogue was sitting at 0.65x the warrior's DPS early precisely *because* a failed blow was nearly
nothing, so the direction is intended.

🔴🔑 **THE RIG'S OWN BLOW MATH WAS STALE AND WAS FIXED ON THE WAY PAST.** `SkillHitFactor` still gated
a blow on `CritChance * CritRateMod` — the **pre-`BL-188`** model, nine versions old — so every blow
row in §C1 had been wrong since 0.121.0. It reads `Entity.BlowRate` now. (Check the rig before the
subject; this is the fourth time.)

**Which skills:** every blow in the game, and they are all dual/rogue-line — `Stab` (fighter 1st),
`Piercing Stab` (rogue 2nd), `Killing`/`Swift`/`Heavy`/`Venom Stab` (dual 3rd + their 4th rungs).
**There is no warrior-line blow.** `Mighty Blow` is a name, not a mechanic: it is `SureHit`, carries
no `BlowOnCrit`, and no class table grants it — an orphan definition like `Heavy Draw`.

**The CSVs moved with the code, same increment:** 128 authored cells across `dual 3rd`, `dual 4th`,
`fighter 1st` and `rogue 2nd` read *"only when skill does critical - otherwise N"*; the dead second
number is now *"otherwise normal attack"*. `--check` is green on all 17 files.

## 2026-09-10 — 0.124.3: the Grand Rune closes `BL-187`, and `BL-186` is postponed

🔴 **NEW APK REQUIRED** — the Admin panel gained a row. No schema change, protocol stays **35**.

*"build one rune that stays in admin menu and its 1d rune that combine both. it will be prmium
curency and close the quesion"*

**Grand Rune** (`rune_grand`), from a **Grand Rune Box (1d)** (`box_grand_rune_24h`):

- `PhysDamageMult: 2.0` **and** `MagicDamageMult: 2.0`, plus the Spell Rune's flat +40 cast speed.
- **24 hours, one rung.** No 1h/2h/30d ladder — one item, not a fourth column.
- **Premium: not buyable, not tradable, not dropped.** Its only route into a bag is the Admin
  panel's new **Runes (premium)** row, under Items → Boxes.

🔑 **Three of `BL-187`'s four open questions were answered by the delivery model, not by a number.**
The entry worried that a ×2/×2 rune strictly dominates both singles — a trap for a pure class, a tax
on hybrids. Making it premium and admin-only removes the shelf it would have had to compete on: it
never appears beside the two vendor runes, so a hybrid gets both channels at full strength and a pure
class gets exactly what its own single already gave. The fourth question was the name.

🔴🔑 **The superseding rule is NOT `CoveredKeys`, and the entry's own recommendation would have
misfired.** `BL-187` proposed the `BL-183` group shape: declare `CoveredKeys` over `rune_war` /
`rune_spell` and outrank them. That is correct for a *cast* buff and wrong for a *rune*, because rune
buffs are owned by `ReconcileRuneBuffs`, which re-derives them from the held items roughly once a
second. Under a covering rank that loop would try to apply the War Rune on **every pass**, have
`ApplyBuff` refuse it as outranked, find the buff still missing, and mark stats dirty — a `SendStats`
every second for as long as a player held both runes. The rule lives where the *wanted set* is built
instead: hold a Grand Rune and the two singles are dropped from it once. The items are untouched — a
superseded War Rune keeps ticking down in the bag and comes back by itself when the Grand Rune ends.

⏸ **`BL-186` (removing the max level cap) is POSTPONED**, his call: *"a big discussion and rely on
current systems to work so to be changed"*. Not declined, not closed, and nothing in it investigated.

⚠ **`nuker 4th.csv` is NOT built yet.** He finished authoring it (236 rows, 25 skills) and the
research for the build is done — the ladders, the rung start indices and the four engine gaps are
written up in `BL-192`. Nothing was half-built; the file is untouched.

### Files

`GameConstants.cs` (0.124.3) · `Skills.Common.cs` (`GrandRuneBuff`) · `Items.cs` · `Boxes.cs` ·
`GameLoopService.cs` (`ReconcileRuneBuffs` step 2b) · `GameUi.Debug.cs` · `Backlog.md` +
`BacklogArchive.md`

---

## 2026-09-10 — 0.124.2: the masteries stop belonging to one class each (`BL-191`)

🔴 **NEW APK REQUIRED** — two skill ids changed and the melee rogue gained two learnable skills, and
the client builds its Learn tab locally from the compiled `ClassSkills`. Wire untouched: protocol
**35**.

His asks, in order, and what each one turned into:

| He said | Built as |
|---|---|
| *"changed `arcane_momentum` → `reuse_reset_momentum` — as other classes can aqure it too"* | id renamed; display name stays **"Arcane Momentum"** on the Magus |
| *"add to duals 4th same `reuse_reset_momentum` with name **Stab Momentum**"* | the same id on Nullblade / Phantom / Venomweaver, `DisplayName` override |
| *"change blood rage toggle not to double only the overpowered base but **all double passives bases** that a class have"* | `SkillDef.MasteryMult` now scales **all three** accumulators |
| *"`blood_rage` → `double_mastery`"* + *"`Blood Rage` → `Overpower Mastery`"* | id and name changed; `BuffKey` too |
| *"add to duals 4th the same `double_mastery` with name **Momentum Mastery**"* | same id, second `DisplayName` override |

🔑 **The generalisation is what makes the rest of it possible, and it is the only engine change
here.** A melee rogue has no Overpower, so a toggle that doubled only the damage base would have been
50 HP a second for nothing. It now multiplies whatever bases its holder actually carries — the
warrior's damage, the rogue's reuse — which is why ONE def can wear two names instead of becoming two
skills that drift apart. `Entity.RecomputeDerived` applies it to `DoubleDamageAcc`,
`DoubleDurationAcc` and `CooldownResetAcc` alike, still before the ATK band and the 25% cap.

🔑 **Two names, one number.** Per-class flavour is a `ClassSkill.DisplayName` override, this
project's standing convention. If the reuse base ever moves it moves for the Magus and the rogue
together — which is exactly what "the same" has to mean for it to be worth saying.

### Measured — `BalanceMatrix` §C1 grew two rows and a second toggle test

```
  Magus (4th)                   0.0%     0.0%     5.5%
  Nullblade (4th)               0.0%     0.0%     4.6%
  Sharpshooter (4th)            0.0%     0.0%     0.0%
  Ravager + toggle             18.8%     0.0%     0.0%
  Nullblade + toggle            0.0%     0.0%     9.1%
```

The last two lines are the point: **one def has to move a different column on each class.** A test
that only ran the warrior would have passed while the rogue's stance did nothing at all.

### Also

- **Perfect Strike / Brutal Strike were already right, only described wrongly.** His clarification —
  *"brutal/perfect strike can be bot learned but they just dont stack as buffs .. a dual class can
  have them both and chose depending on situatuion which to use"* — is what the code does: both sit
  in the learn table at 80 and the exclusion is a shared `BuffKey`. But the rung text said "Replaces
  Brutal Strike", which reads like a learn-tab consequence. Reworded on both, and in the CSV.
- 🔴 **`dual 4th.csv` had its `REPLACES` values sitting in the `SP Bottles` column.** Its header was
  the only 4th-tier one missing that column, so every value from `Gold` rightward was off by one
  against its siblings. Header and all 126 rows realigned to the canonical 20-column shape, and the
  top comment block rewritten free of the stray commas and unbalanced quotes that made three of its
  own lines parse as one field.
- ⚠ `--chains` will now name `reuse_reset_momentum` under "CROSS-CHAIN IDS" — a Mage discipline and
  three Fighter ones learn it. That is **correct output**, not a defect: the audit's automatic
  exemption only covers ids that *every* ascended class learns.
- 🟡 The two melee-rogue learn levels (76 and 81) are the only unauthored numbers in the layer; they
  mirror the Magus and the warrior. Flagged in `BL-191`.
- 🟡 The whole layer is provisional: *"ill try with those changes and after playtest ill deside if i
  add or remove"*.

### Files

`GameConstants.cs` (0.124.2) · `Skills.cs` (`DoubleDamageMult` → `MasteryMult`) ·
`Skills.SkillMasteries.cs` · `Skills.Dual4th.cs` · `ClassSkillTables.Fourth.cs` · `Entity.cs` ·
`GameLoopService.cs` · `BalanceMatrix/Program.cs` · `dual 4th.csv` + `warrior 4th.csv` +
`war_aoe 4th.csv` · `Formulas.md` · `Backlog.md`

---

## 2026-09-10 — 0.124.1: Blood Rage is a level **81** skill, so the cap is the end of a climb

🔴 **NEW APK REQUIRED** — a learn level is part of the class-skill table, and the client builds its
Learn tab locally from the compiled `ClassSkills`. An old APK offers the toggle at 76. The wire is
untouched: `ProtocolVersion` stays **35**.

0.124.0 shipped Blood Rage at **76** and said, in three places, that the level was the one assumption
in the build. He answered it the only way that counts — by editing the CSVs:

```
81,Blood Rage,blood_rage,Toggle,...,0,200kk,25kk,,,,Worth nothing without Overpower
```

🔑 **The five levels are a PLATEAU, and that is the point.** His reason, given in as many words:

> i dont want at 76 lvl warrior to start doubling at 25% .. until 81 he is at base 10% .. at 81 then
> gets the toggle and become stronger

Overpower's last rung lands at 76 and leaves the warrior on a **10%** base — ≈13% once the high-ATK
band is applied — for five levels. At 81 the toggle doubles the base to 20%, the band carries that to
26%, and `StatCaps.SkillMasteryRateMax` trims it to **25%**: the ceiling is reached exactly once, at
the end of the climb, rather than on the day he ascends.

⚠ **The number 81 is not sacred — the ORDER is.** *"doesnt matter if its 76/78/81 .. etc lvl"*. If it
ever moves it moves *later* than Overpower's top rung, never onto it. The price follows for free,
because `F4New` reads the shared ladder: 81 charges `200kk` SP + `25kk` gold, which is what he
authored in the CSV to the digit.

Changed: `ClassSkillTables.Fourth.cs` (the one `ClassSkill` line, 76 → 81) and
`Skills.SkillMasteries.cs` (the def now reads `F4New(81)` for both `SpCost` and its single rung's
`GoldCost`). Overpower is untouched at 20/40/76. `Formulas.md` and `BL-191`'s table moved with it, and
`BL-191`'s open question 1 is answered and cut to `BacklogArchive.md` — what is still open there is
only *who else, if anyone, gets a mastery*.

⚠ The 81 row is authored in **both** warrior files (`warrior 4th.csv` and `war_aoe 4th.csv`) — they
are the same two rows in two files because Ravager and Warlord share this pair. Neither file earns a
`Check.Specs` line yet; the rest of both is still his to write.

### Files

`GameConstants.cs` (0.124.1) · `ClassSkillTables.Fourth.cs` · `Skills.SkillMasteries.cs` ·
`warrior 4th.csv` + `war_aoe 4th.csv` (his) · `Formulas.md` · `Backlog.md` + `BacklogArchive.md`

---

## 2026-09-10 — 0.124.0: four passives turn `[Double]` back on (`BL-191`)

🔴 **NEW APK REQUIRED** — the class-skill tables changed (four new learnable skills), and the client
builds its Learn tab locally from the compiled `ClassSkills`.

0.123.0 built the three mastery channels and shipped them switched off, because nothing authored one.
He authored them the same day. His words, in full:

> - buffers and healers get the duration passive with base 10% @76
> - Mages get cooldown passive with base 5% @76
> - warriors/aoe-warriors get the double dmg passive with base 3,7,10% @20,40,76
>   - and warriors/aoe-war get another toggle skill that doubles the effect of the double passive
>     drain 50hp/s and increases the p.mp.consumtion with 25%
> - Fighters skills can double only if the skill say so (not all)

### The four skills

| Skill | Who | Learn | What it grants |
|---|---|---|---|
| **Overpower** | Warrior → Ravager + Warlord | 20 / 40 / 76 | double-damage base 3% / 7% / 10% |
| **Blood Rage** (Toggle) | Ravager + Warlord | 76 | ×2 on Overpower's base; **50 HP/s**, **+25% MP** on physical skills |
| **Lasting Enchantment** | Lightbringer + Warchanter | 76 | buff/debuff duration-double base 10% |
| **Arcane Momentum** | Magus | 76 | reuse-reset base 5% |

All four live in one file, `Skills.SkillMasteries.cs` — they are not a discipline's kit, they are the
four authors of one engine feature, and the numbers only read correctly together.

🔑 **"Mages" is the NUKER.** His own vocabulary throughout `shared 4th.csv` separates Mage / Healer /
Buffer / Warrior / Tank / Rogue, and the line above gives the healer and buffer a *different*
passive. Per his PS, only the reuse passive was built into the nuker today — the rest of
`nuker 4th.csv` is still being written and waits for tomorrow.

🔑 **Blood Rage multiplies the BASE, not the finished rate.** `SkillDef.DoubleDamageMult` folds into
`Entity.DoubleDamageAcc` *before* the ATK band and *before* the shared 25% cap, so the stance can
never step over the ceiling. And it is worth exactly nothing without Overpower: ×2 of a zero base is
zero, which is `BL-190`'s gate working rather than a case to special-case.

⚠ **`PhysMpCostPct` is NEGATIVE on it (−0.25).** The field is a *reduction* everywhere else in the
game; `Entity.PhysMpCostReduction` clamps to [−2, 0.8] precisely so a penalty can ride the same
channel. His "p.mp" is the physical side, and the magic channel is left alone on purpose.

### Measured, not derived — `BalanceMatrix` §C1 now reads real characters

```
  --- what a REAL level-90 character carries (dmg / buff-duration / reuse-reset) ---
  Ravager (4th)                 9.4%     0.0%     0.0%
  Warlord (4th)                 9.4%     0.0%     0.0%
  Warchanter (4th)              0.0%     9.7%     0.0%
  Lightbringer (4th)            0.0%    10.9%     0.0%
  Magus (4th)                   0.0%     0.0%     5.5%
  Bulwark (4th)                 0.0%     0.0%     0.0%
  warrior, NO discipline        3.3%     0.0%     0.0%
  Ravager + BLOOD RAGE         18.8%     0.0%     0.0%
```

The last row is the one worth having: it is the only proof the toggle's field actually reaches
`RecomputeDerived`. 🔴 **And getting it required fixing the rig first** — both of its buff builders
constructed a `BuffInstance` from `Effect` + `Magnitudes` only, so every FIELD channel (blow rate, MP
cost, and now the mastery multiplier) was silently dropped. Blood Rage measured as doing nothing at
all until they were taught to copy them. Same trap, third time: *half of what a buff carries is not a
`SkillEffect` bit, because that enum ran out of bits years ago.*

### The daggers lose `[Double]`

*"if the daggers skills say can crit/double u can remove that part of them"*. 128 CSV rows across
`fighter 1st`, `rogue 2nd`, `dual 3rd` and `dual 4th` lost the phrase, and the five defs behind them
lost `CanDouble: true` — Stab, Piercing Stab, the whole `StabSkill` family (Killing / Swift / Heavy),
Venom Stab and Venom Burst. A blow already has its own landing roll since `BL-188`; a second one on
top of it was never the design, and no rogue has Overpower anyway. Bow and sword skills that say
"can double" in his CSVs keep it.

### Also

- `SkillCsvSeed --check` learned three metrics — `double rate`, `double duration rate`,
  `reuse reset` — so every number in the five new rows is **verified, not UNREAD**. All fifteen
  walked files stay green.
- The two warrior 4th CSVs stop being two-line placeholders: they get the 4th-tier column header and
  the two rows he authored, plus a banner saying the rest is still his. Neither earns a
  `Check.Specs` line yet — same standing as `warrior 3rd`, which has had HP Boost rows for weeks.
- ⚠ **Blood Rage's LEVEL is the one assumption in this build.** He gave the toggle its effects but no
  learn level, in a sub-bullet under the 20/40/76 ladder. 76 is the defensible read — Holy Soul, the
  only other 50 HP/s toggle in the game, is a 76 skill, and at level 20 this would kill a warrior in
  under a minute. One line to move if he meant 40. → **He meant 81. Corrected in 0.124.1 above**, with
  the 81 price rung to match; neither 76 nor 40.

### Files

`Skills.SkillMasteries.cs` (new) · `Skills.cs` (`SkillDef.DoubleDamageMult`, the `CanDouble` comment,
one `AddRange`) · `Entity.cs` (`BuffInstance.DoubleDamageMult` + its fold) · `GameLoopService.cs` (one
line wiring the buff field) · `ClassSkillTables.Common/Third/Fourth.cs` · `Skills.Fighter.cs`,
`Skills.Dual3rd.cs` (five `CanDouble` removals) · `SkillCsvSeed/Descr.cs` · `BalanceMatrix` §C1 and
both buff builders · nine CSVs · `docs/Formulas.md`, `docs/design/CritBlowAndDouble.md`.

## 2026-09-10 — 0.123.0: `[Double]` stops being a birthright (`BL-190`)

⚠ **NEW APK wanted** (an old one still plays) — the stats window gained a `Buff x2 dur` /
`Reuse reset` line, and the `[Double]` figure it used to derive locally is now sent by the server.
`ProtocolVersion` 34 → 35, a pure addition at the end of `StatsUpdate`; `MinAcceptedProtocol` stays 8.

🔴 **Read this first: nothing in the game doubles any more, and that is the point.** See the last
section. ⚠ **That state lasted one version** — he authored the four passives the same day and
**0.124.0** turns them back on. Everything below is still the model; only "nobody has one" expired.

### What he ruled

`BL-190` had three ❓ owed. He answered all three and widened the last one into a family:

> *"I want several passives .. One that resets cooldown of skills, one that doubles duration of bad
> and good buffs, and one that allow double dmg ... All will calculate the same just the base is
> based on the passive."*

plus, on the rate: **the passive carries the base, ATK is only a band around it**; and on who has
one today: **nobody, until he authors it**.

### The math — one function, three channels

```
MasteryAtkMod = 1 + 0.03 × (clamp(EffectiveAtk, 30, 50) − 40)      ×0.70 … ×1.30
rate          = 0                                                   when no passive grants it
              = clamp(passiveBase × MasteryAtkMod, 0, 25%)
```

Deliberately the same shape as `BL-188`'s blow rate — a passive-fed base with a stat band on top —
because that one is proven and because he asked for the three to "calculate the same". The band is
`BlowAgiMod` moved onto ATK's anchor of 40, so a human fighter (ATK 40) sits at exactly ×1.00 and
the race spread is ±12% instead of the 7%-vs-10.75% *rate* gap the old curve handed out.

| Channel | What it does | Rolled |
|---|---|---|
| `Entity.DoubleDamageRate` | a physical skill flagged `[Double]` deals ×2 | per hit; on a BLOW it is the second roll, after the blow lands |
| `Entity.DoubleDurationRate` | a buff or debuff you cast lasts twice as long | once per cast (area = everyone or no one), player casts only |
| `Entity.CooldownResetRate` | the skill just cast comes straight off reuse | once per cast, after the reuse reduction; never on a `FixedCooldown` skill |

`PassiveEffect` gained the three matching fields and they **SUM** (a rung ladder replaces itself
through the normal skill-level machinery, so a sum only ever adds genuinely different passives — and
a sum is the only thing that can start from zero). `StatCaps.SkillMasteryRateMax` = 25%, shared.

The rig prints the whole surface (`BalanceMatrix` §C1):

```
  ATK stat        30      33      36      40      41      45      50      55
  band         x0.70   x0.79   x0.88   x1.00   x1.03   x1.15   x1.30   x1.30
  base  10%      7.0%    7.9%    8.8%   10.0%   10.3%   11.5%   13.0%   13.0%
  base  20%     14.0%   15.8%   17.6%   20.0%   20.6%   23.0%   25.0%   25.0%
```

### The cooldown reset is the one genuinely new mechanic

`ExecuteSkill` rolls it **after** the reuse reduction, so the two never fight over the same number,
and never on a `FixedCooldown` skill (Return, the ultimates) — the same exemption the reduction
already has, for the same reason: those cooldowns *are* the balance. On a hit the cooldown key is
**removed** rather than set to 0, so the tick loop and the bar's reuse overlay both see a skill that
is simply ready rather than one counting down from nothing, and the caster is told:
`Killing Stab is ready again!`.

### Two things this fixed on the way past

- 🔴 **Duration doubling was literally the same roll as damage doubling.** IG's level-76 Skill
  Mastery shared `PhysicalDoubleChance(caster.AtkStat)` with the damage side, which meant any change
  to the damage curve silently moved every buff duration in the game. Splitting them was done FIRST,
  before either rate was touched — it is the entry's own "thing most likely to be missed".
- 🔴 **The rate read the RAW `AtkStat`, not `EffectiveAtk`.** Its doc comment defended this ("a
  better weapon must not buy Double chance, only the build does") — but `EffectiveAtk` is not p.Atk,
  it IS the build: the level-40 `+5 ATK` swap, the armour sets and every `+ATK` passive land in it,
  and not one of them bought a point of Double. The band reads the effective stat now.

### 🔴 What this deliberately breaks, and why it is not a bug

**`StatCalculator.PhysicalDoubleChance` is deleted with no replacement door**, and nothing in the
CSVs authors a mastery passive. So as of this build **every character in the game reads 0% / 0% / 0%
and no skill doubles** — the ~14 `[Double]` skills (Strike, Smash, Shot, Precise Shot, Cleaving
Strike, the bleed/poison detonators, and the second roll on Killing / Venom / Swift Stab) all land
flat, and buff durations no longer double for anyone.

That is his ruling verbatim — *"Nobody — dead until you author it"* — chosen over an auto-granted
tier ladder he was offered. The rig reports it honestly rather than hiding it: the Champion line now
reads `double x1.00` where it read `x1.10`, i.e. about 9% off a warrior's expected skill damage.

📌 **`BL-191`** is filed with the four things owed from him: which kits get which mastery and at what
rung, the base rates, whether the reuse-reset is a fighter or caster tool, and whether a BUFF should
be able to scale a mastery. For reference when picking numbers: the retired curve paid **10.0%** to a
human, and a base of `0.10` reproduces that to the decimal.

### Files

`StatCalculator.cs` (`MasteryAtkMod`, `SkillMasteryRate`; `PhysicalDoubleChance` deleted) ·
`StatCaps.cs` (`SkillMasteryRateMax` replaces `PhysicalDoubleRate`) · `Skills.cs`
(`PassiveEffect` ×3 fields, the `CanDouble` comment) · `Entity.cs` (three accumulators, three folded
rates) · `GameLoopService.cs` (the three roll sites + the new reuse-reset roll) · `Dtos.cs` +
`GameConstants.cs` (protocol 35) · `GameUi.Stats.cs`, `GameUi.SkillDetail.cs` ·
`BalanceMatrix` §C1 · `docs/Formulas.md`, `docs/design/CritBlowAndDouble.md` §1 and §4.

## 2026-09-10 — 0.122.0: every class wears its own weight, and the melee rogue exists above 76

⚠ **NEW APK** — the class-skill tables changed (the melee rogue's 4th class is 105 learn rows now,
not three).

Three asks, in his order.

### 1. 🔴 THE RIG DRESSED EVERY FIGHTER IN HEAVY PLATE, A SHIELD AND A ONE-HANDED SWORD

`BalanceMatrix.BuildPlayer` gave every `BaseClass.Fighter` `heavy_t{n}` + `shield_t{n}` +
`sword1h_t{n}`, and each caller then hand-swapped the WEAPON back with a `Dress()` helper — so the
weapon was usually right and **the armour was wrong for everyone but the tank**. The rogue and the
archer were measured in plate their own Armor Mastery (`light`) pays nothing for, and the heavy sets
carry `Agi: -2` where the light ones carry `+1…+3`. On a stat that now drives the blow rate
(`BL-188`) that is not cosmetic: it read the rogue **4 AGI light**.

His table, built into the builder — there is **not one `Dress` call left**, and the helper is gone:

| role | armour | weapon | shield |
|---|---|---|---|
| mage | robe | staff | — |
| healer | robe | wand | ✔ |
| buffer — Demon | heavy | 2H blunt | — |
| buffer — Human | heavy | 1H blunt | ✔ |
| buffer — Elf | light | bow | — |
| warrior | heavy | 2H sword | — |
| rogue | light | duals, or bow for the three archer disciplines | — |
| tank | heavy | 1H sword | ✔ |

⚠ **Every damage table in the tool moves.** The archer's level-85 P.Atk goes 4540 → 2460 — it was
wearing the warrior's set. He also ruled the two support roles out of the comparison entirely
(*"buffers and healers are not measurable because their roles are different"*); they are dressed
correctly regardless, so any table that does print them is at least honest about the loadout.

### 2. Sound Burst's reuse is 5s

The 28 `--check` discrepancies flagged in 0.121.0, all one number. His `buffer 3rd.csv` and
`buffer 4th.csv` say 5s on all 28 rungs while `SoundSkill` hard-coded 3s for all three sound skills.
The CSV is the authority and it was authored after the code, which is the rule he restated:
*"Make it 5s reuse - if csv is authored after the code"*. Sound Smash and Acoustic Shock stay at 3 —
the ranged one, which also hits twice, is deliberately slower. **`--check` is now green on all
seventeen walked files.**

### 3. The melee rogue is measurable above 76

🔴 **Half of `dual 4th.csv` is DERIVED, and it is marked so in the file.** The 40+ rule normally
forbids inventing a kit; he lifted it for this one job — *"Build the new skills for duals 4 so it's
measurable after 76 (even with lower power skills)"* — because a class with no 76-90 rungs cannot be
put on a damage table at all. Every derived block carries `(DERIVED)` in its separator and the header
says outright that it is his to overwrite. **The file still has no `Check.Specs` line.**

🔑 **The one anchor that IS his is the damage.** He gave the melee rogue's Stab as **7k-11k power at
85 and 10k-15k at 90** (2026-09-06, the reference `--his` measures against), so Killing Stab is built
to land on **11,000 at 85 and 15,000 at 90 exactly** — the top of each band — and the other three
families keep the ratio they already hold to it at the 3rd tier.

| family | 74 (his) | 76 | 85 | 90 |
|---|---|---|---|---|
| Killing / Swift Stab | 6,400 | 6,900 | **11,000** | **15,000** |
| Heavy Stab (×0.75, twice) | 4,800 | 5,175 | 8,250 | 11,250 |
| Venom Stab (×0.50) | 3,200 | 3,450 | 5,500 | 7,500 |
| Venom Burst (×0.20/stack) | 1,280 | 1,380 | 2,200 | 3,000 |
| Dual Mastery P.Atk | 80 | 85 | 130 | 150 |
| Dual Mastery crit damage | 1,015 | 1,040 | 1,220 | 1,300 |

✅ **Armor Mastery's fifteen rungs are the ARCHER's, verbatim** — both wear light, and his
`archer 4th.csv` already authors that ladder (P.Def 72→100, evasion 15→19, speed 12→15). Reusing it
means half of this block is authored rather than none of it, and it avoids inventing a difference he
never asked for. ⚠ `archer_armor_mastery` REPLACES `rogue_armor_mastery` at 40, so the appended rungs
reach the three melee disciplines and nobody else.

⚠ **Venom tier and stacks-per-cast are FROZEN at the 3rd tier's endpoint** (rank 10, 3 stacks): rank
10 is the top rank a debuff can carry, so there is nothing above it to author. Same call the archer's
regen cells got.

Measured, level 85, mythic, `--dmgmatrix … --his`:

```
-- ROGUE  [Nullblade]  P.Atk 934  crit 23.2%   skill: Stab (power 11000)
   tank    def 2599  HP 12673 |  706 hit  1143 crit  |  18.0 hits
   warrior def 1713  HP 12563 | 1072      2399       |  11.7
   mage    def 1124  HP  4479 | 1634      3657       |   2.7
```

The catalogue and his reference now agree on the number, which they could not before: `BestSkill`
skips blows, so a rogue row without `--his` had nothing to print.

## 2026-09-09 — 0.121.0: `BL-188` — THE BLOW LANDING RATE BECOMES ITS OWN STAT

⚠ **NEW APK** — the class-skill tables changed (five new skills across the melee rogue's 3rd and 4th
tiers, one on the tank's 4th).

His ruling, after the old `BL-188` entry was rewritten with what the code actually does: a dagger
BLOW no longer lands off the character's crit rate. It has its own stat, its own ladder and its own
defence.

### What was wrong (measured, not derived)

The entry filed on 2026-09-09 was wrong in **both** halves, and the corrections are the reason this
was worth doing at all:

- **The blow roll had no 50% cap — it ran to 100%.** `ResolveBlow` clamped
  `attacker.CritChance × def.CritRateMod` to `[0, 1]`, not to `StatCaps.PhysicalCritRate`. `CritChance`
  is *already* clamped at 50% and was then multiplied by an unauthored `CritRateMod: 2.0`, so a
  Nullblade landed blows at **38.8% unbuffed at 74, 60.6% buffed at 78 and 80.6% buffed at 85** — and
  at the crit cap, **every stab**. That is playtest-19 M9's *"each blow lands with the 64+% chance"*
  arriving again from the other direction.
- **`[Double]` is a per-race CONSTANT nothing can raise.** Player fighter ATK is Elf 36 / Human 40 /
  Demon 41 → 7.0 / 10.0 / 10.75%, and the 25% cap needs ATK 60. Worse, the call passes the **raw
  `AtkStat`** while its crit twin uses `EffectiveAgi` — so the level-40 `+5 ATK` swap, the armour sets
  and every `+ATK` passive buy **zero** double chance. Left alone here on his instruction and moved to
  its own entry: he wants a PASSIVE to gate which skills may double, not the ATK curve.

### The new model

```
blowRate = clamp(0.30 × buffs × passives × BlowAgiMod(AGI), 20%, 80%) × (1 − BlowResist)
BlowAgiMod(AGI) = 1 + 0.03 × (clamp(AGI, 20, 40) − 30)      →  ×0.70 … ×1.30
```

🔑 **The AGI anchor already existed.** `StatCalculator.MobAgiReference` is that same 30, and
`CritAgiMod` is `1 + 0.01·(AGI−30)` — so `BlowAgiMod` is literally that function **at 3× the slope**,
and the Human fighter's base AGI is 30 on the nose, so an unswapped human is exactly ×1.00.
⚠ It hands AGI a **fifth job, now its biggest** (+1.8pp of landing per point against +0.13pp of a
dagger's crit). That is deliberate and does not license inflating `CritAgiMod`, whose guardrail stands.

🔑 **The cap comes BEFORE the defender's resist**, which is his own worked example (`~80% × 0.7 =
~56%`) and is what makes an 80% ceiling something a tank can still reach past.

⚠ **Three things deliberately no longer touch the blow roll**: `CritRateMod` (the field survives for
the `CanCrit` path), a shield's `ShieldCritDefense`, and `CritRateResist` — the last matters most,
because the rogue's *own* Armor Mastery carries 25-35% crit-rate resist and would otherwise have made
rogues the best anti-rogue armour in the game.

### The ladder (all five skills authored into the CSVs in this commit)

`dual 3rd.csv` — **the race split is the balance**, and it is his: *"the race based buffs balance the
blow rate and lack of dex and atk"*. One budget per rung (10 / 15 / 20%), and the race decides how it
is spent. The Elf already leads on AGI (36 vs 30 vs 28) so spends it all on crit damage; the Demon
trails so spends it all on rate; the Human splits it.

| | 40 | 60 | 70 | MP | reuse / duration |
|---|---|---|---|---|---|
| **Lethal Focus** (Human) | +5% rate, +5% dmg | +7.5% / +7.5% | +10% / +10% | 100 / 125 / 150 | 90s / 5 min |
| **Lethal Precision** (Elf) | +10% crit dmg | +15% | +20% | ″ | ″ |
| **Lethal Frenzy** (Demon) | +10% rate | +15% | +20% | ″ | ″ |

- **Vital Points** — a passive at 52 / 64 / 74, +10 / 15 / 20% rate, shared by all three disciplines.
- **Assassination Instinct** (76) — his name, his number: a passive, +5%.
- **Perfect Strike / Brutal Strike** (80) — 200 MP, five minutes up and five minutes down, and a
  CHOICE: +40% blow rate or +30% crit damage, never both. They take Great Might / Great Bulwark's
  exact recipe — one shared `BuffKey` at `Rank 1` with `FlatRank` — so casting either evicts the other
  and the pick stays re-makeable.
- **Vital Organ Protection** (`tank 4th.csv`, 80) — the tank's answer and the only blow defence in the
  game: −30% off an attacker's already-capped rate.

### Measured — `dotnet run --project tools/BalanceMatrix -- --blowrate 85`

```
  race                  AGI  agiMod | passives only |  + race buff  | + Perfect Strike | vs a tank
  Human  (Nullblade)     30 ×1.00 |       37.8%   |     41.6%   |        58.2%     |   40.7%
  Elf    (Shadowblade)   36 ×1.18 |       44.6%   |     44.6%   |        62.4%     |   43.7%
  Demon  (Venomblade)    28 ×0.94 |       35.5%   |     42.6%   |        59.7%     |   41.8%
```

✅ **His target — *"~60% rate and a lot of dmg or 80% rate and less dmg"* — is the AGI-40 column, on
the nose: 59.0% and 80.0% (the cap, exactly).** The ladder is built to reach his numbers at a MAXED
AGI build rather than at a race's base, which is the AGI class having to buy AGI. ⚠ Only the ELF gets
there cheaply (36 + the five-point swap is already past 40); Human 30 and Demon 28 need the swap AND
light-set AGI on top.

🔴 **A RIG DEFECT FOUND ON THE WAY, NOT FIXED HERE:** `BalanceMatrix.BuildPlayer` dresses **every**
Fighter in HEAVY plate and a shield — the melee rogue and the archer included, though both wear LIGHT
by their own Armor Mastery. The heavy sets carry `Agi: -2` and the light ones `+1…+3`, so a rogue
measured in plate reads 2 AGI light. `--blowrate` swaps it for its own probe only; fixing it for every
table moves signed-off numbers and is a wider change than this one.

### Engine

- `StatCaps.BlowRateBase/Min/Max/BlowResist`, `StatCalculator.BlowAgiMod/BlowRate`.
- `Entity.BlowRateMult` (accumulator) → `Entity.BlowRate` (folded and clamped once, with the two crit
  chains), and `Entity.BlowResist`.
- `SkillDef.BlowRatePct` + `SkillLevel.BlowRatePct` + `BlowRatePctAt(level)`, and
  `PassiveEffect.BlowRate` / `BlowResist`. ⚠ Fields, not a `SkillEffect` bit — the flag enum has been
  full since `1L << 62` — so a blow-rate buff still declares `BuffCritRate` purely to BE a buff, and
  `SkillText` reads the field directly so a card never advertises the carrier.
- `BuffInstance.BlowRatePct`, copied **per rung** in `ApplyBuff`.
- `SkillCsvSeed/Descr.cs` learned `blow rate` and `blow resist`, so all fourteen new authored numbers
  are verified rather than `UNREAD`.

### ⚠ Still open, reported not fixed

- **28 pre-existing `--check` discrepancies**, all one thing: **Sound Burst's reuse reads 5s in
  `buffer 3rd.csv` and `buffer 4th.csv` and 3s in the code**, on every one of its 28 rungs. It is on
  HEAD, predates this commit and is a Warchanter balance number — flagged rather than folded into a
  rogue commit.
- **`dual 4th.csv` is STILL the two-line placeholder** plus these three rows, so it has NOT earned a
  `Check.Specs` line (the checker walks whole files). The melee rogue's 4th class is three skills, not
  a kit; the rest waits on his file, as before.


## 2026-09-09 — 0.120.0: the archer's 4th class, and the first CHANNEL in the game

⚠ **NEW APK** — the class-skill tables changed again (three disciplines gain a 76-90 kit).

His word, hours after the 3rd tier shipped: *"i fixed the dips, build archer 4th"*. **`archer 4th.csv`
is the fourth finished 4th-tier file**, after the Lightbringer, the Warchanter and the Bulwark.

### Most of it is the 3rd tier continuing

Nine families gain rungs 16-30, one per level from 76 to 90: Armor Mastery (P.Def 72 → 100), Bow
Mastery (P.Atk 820 → 1300, crit damage 682 → 900), Twin Arrows (5200 → 8000 **per arrow**), Explosive
Arrow, the three traps and the three Magic Arrows. The SP/gold ladder is the healer's exactly —
6.5kk/11kk/16kk/80kk and then **gold alone**, 1kk climbing to 100kk — so `F4`/`F4Rungs` are reused
rather than copied.

### What is new

- **Three party procs at 76, one per race.** The archer's first contribution to anybody but himself:
  3% on a landed hit, 30s lockout, 30s buff, at 900 radius. Human = **Damage Mastery** (+10% final
  physical *and* magical damage), Demon = **Swift Mastery** (+10% attack and cast speed), Elf =
  **Spirit Mastery** (−20% MP on every skill, +20% cast speed, +10% crit damage).
  ⚠ Damage Mastery rides `PhysDamageMult`/`MagicDamageMult`, the SHOT channel `BL-185` built — not a
  P.Atk buff, which inside an additive ratio would have been worth a fraction of what his row says.
- **Five ultimates at 84/85**, the first fighter skills bought with **SP BOTTLES** — 100,000,000 gold
  plus 2 bottles (Heavy Arrow, 84) or 5 (the rest). Heavy Arrow hits for **17,000**; Bleeding Arrow
  (Demon) for 15,000 plus a tier-11 bleed, Dazzling Arrow (Human) for 15,000 plus a 10s stun and 3
  buffs stripped, Healing Arrow (Elf) for 15,000 and heals 40% of it back.

### 🔑 ARROW BARRAGE — and the mechanic it needed

His comment cell asked for something the engine had never done: *"like a channeling skill; start to
cast and for the next 2 second it continue to cast 1 arrow/200ms; can be canceled like normal skill"*.
He laid out two ways to build it and **killed the first himself**: a pulsating ground effect *"removes
our game logic point — always hit then calculates evasions etc"*.

The one he kept is a **WRAPPER**: *"inside the wrapper each arrow is same skill (power 2500, range
900, aoe 150, etc..) and its cast 10 times or until wrapper stops"*. So `archer_arrow_barrage_arrow`
is a real `SkillDef`, and each of the ten goes through `ExecuteSkill` on its own — its own crit, its
own block, its own 150 splash. Nothing about it is special-cased, which is exactly why the rule
survives.

`SkillDef.ChannelSkill` / `ChannelShots` / `ChannelIntervalTicks`, driven by a new branch in
`UpdateAction` that ticks **before** the cast branch and returns: a caster mid-volley does nothing
else. Three things fall out of it and all three are deliberate:

- **The wrapper charges MP and reuse; the shots charge nothing.** One cast, one price.
- **It dies to what a cast dies to.** `CancelCast` ends a volley *before* its own early return —
  a barrage is not a cast (`CastingSkillId` is already null while it runs), so an ESC would otherwise
  have found nothing to cancel and the arrows would have kept coming. A stun ends it too.
- **The arrows not yet loosed are lost, and the MP is not refunded.** That is what makes ten arrows a
  commitment rather than a button you can take back.

**…and TWIN ARROWS moved to the same shape**, on his instruction in the same breath: *"this will
change the twin arrow skill to same logic (1 wrapper and while cast just cast 2 times same skill per
arrow)"*. ✅ The behaviour he settled is unchanged — two arrows, each resolving independently, which
`HitCount: 2` already delivered. What the wrapper buys is that both archer volleys are ONE mechanism.

⚠ `Entity.ChannelPower` exists because the two channels read their power from opposite ends, and both
readings are his: Twin Arrows is *"two arrows EACH dealing +5200"* — the ladder is per arrow and lives
on the wrapper — while Arrow Barrage's arrow carries a flat 2,500 of its own. Storing the resolved
number lets one mechanism serve both without the sub-skill duplicating a thirty-rung column.

### His three fixes, and four paste artifacts found on the way in

✅ He fixed **Twin Arrows' dip** (1000 → 5200 at 76) the moment it was reported, set **Sprint L2 to 20
MP**, ruled **Lure's aggro 400, not 500** (*"also mobs aggro should be 400 not 500"*), and settled
**Bow Expertise's two prices**: *"bow expertise for buffer and archer is at different lvls so it cost
different SP"* — 56 for 42,000, 52 for 37,000. One ability, one set of magnitudes, two prices, which
is precisely what `ClassSkill.SpCost` was added for in August.

`--check` then found four more of the same paste artifact in `archer 4th.csv`, all corrected in the
file so it says what the game does:

- **Armor Mastery's two regen columns** read `mpReg x1.8; hpReg +1.2` — `rogue 2nd.csv`'s level-36
  rung — against a 3rd tier ending at `+2.5` / `+6.0`. **FROZEN at the 3rd tier's endpoint**: the
  ladder rule says report or interpolate, never accept, and freezing changes the least while keeping
  his own flat-across-the-tier shape. 🔵 Two numbers for him.
- **Six of the thirteen blocks kept the 3rd tier's SP column** (28 → 880 with no gold at all) while
  the other seven carry the tier's own 6.5kk + gold. The file's own majority defines the price, so
  the traps and Magic Arrows now use it.
- **Dazzling Arrow's DURR cell said 30** where its own DESCR says *"stun for 10s"*. The DESCR is the
  more specific statement; 30 is Bleeding Arrow's number one row up.
- **An Antidote rung at 76 that I invented** by analogy with the healer's, and `--check` caught it:
  his file authors no Antidote row, so the Elf archer's cure stops at rank 9 — which means the
  Venomweaver's top venom stays uncurable by anything in either rogue file.

### The tool learned three more things, all of them it being wrong about correct code

- **A party proc's reach lives on its PAYLOAD**, not on the passive that rolls it.
- **A channel wrapper's AoE is its arrow's** — the splash is on the thing that lands.
- **A party proc is now exempt from both reach columns, because HIS OWN FILES DISAGREE**: Combo
  Mastery (`buffer 3rd`) and Aggravated State (`tank 3rd`) are authored `self/single`, describing the
  TRIGGER; the archer's three masteries are `self/party` at 900, describing the EFFECT. Both are
  defensible and the code cannot be both, so it compares neither rather than reporting three good
  rows or six. 🔵 One for him to settle.

### 🔴 Open

- **`dual 4th.csv` is still the two-line placeholder**, so the melee rogue stops at 74.
- Armor Mastery's frozen regen pair, and the `self/single` vs `self/party` convention above.


## 2026-09-09 — 0.119.0: the rogue's two branches get their kits (`dual 3rd.csv`, `archer 3rd.csv`)

⚠ **NEW APK** — the class-skill TABLES changed, and the client builds its Learn tab locally from the
compiled `ClassSkills`. Six disciplines gain a full kit; without a new client none of it is visible.

His word, 2026-09-09: *"build/fix rogue 2nd, archer and duals 3rd"*, then *"on duals 3rd add the
'lure' skill rows"*. Both files were finished that morning — he filled in an MP column he had left at
zero (*"my slip"*) and raised the Elf stance's move speed while this was being built. **Six of the
eight fighter disciplines come off the 2026-08-10 forty-plus purge with this commit**; only the
warrior's two are still waiting for a file.

### The melee rogue — `dual 3rd.csv` → `Skills.Dual3rd.cs`

**Race decides the damage skill, and that is the whole shape of the file.** All three share Armor
Mastery (rungs 6-20, appended), Dual Mastery, Sprint L2, Evasion Boost L2 and the hide kit; then:

| | Human — Nullblade | Elf — Phantom | Demon — Venomweaver |
|---|---|---|---|
| main blow | Killing Stab 1250 → 6400 | Killing Stab | — |
| second | **Heavy Stab**, that power twice | **Swift Stab**, +5 speed / +15% AS for 5s | **Venom Stab** (half power) + **Venom Burst** |
| Phantom Jump | blink + **stun** | blink + **charm** + 75% slow | blink + **fear** + 90% slow |
| also | | **Antidote** (self cure) | |

- **A 3rd-tier blow floors at 1%, not 10%.** Every stab row reads "power N … otherwise N/100" where
  the 2nd class's Piercing Stab reads 314/31. The dagger branch is far more all-or-nothing than the
  line it continues, and that is authored, not inherited.
- **Dual Mastery replaces the rogue's Weapon Mastery outright**, which is what drops the bow half —
  choosing daggers costs you the bow. It also carries the first proc ever put on a weapon mastery:
  3% on hit for 5s of −60% skill MP and +10% crit damage.
- **Venom Stab and Venom Burst are one rotation**, pooled through the `venom_venom` stack key the
  primitives have used since they were sketched. `venom_burst` is that primitive's own id,
  re-authored from one placeholder rung at power 12 to fifteen at 250 → 1280 **per stack**.

### The archer — `archer 3rd.csv` → `Skills.Archer3rd.cs`

**Eleven of eighteen families are self-buffs.** Two universal 20-minute ones (Bow Blessing = −20%
physical reuse, Bow Spirit = −30% physical MP), one 5-minute race stance, and **Bow Stance** — the
trade the class points at: +15% on attack power, crit rate, crit damage and skill power, **+200 bow
range, for half your movement**.

- **Race owns one trap, one Magic Arrow and one stance, and nothing overlaps.** Human = stun + poison
  trap + Bow Focus (bleed rider); Elf = slow + binding trap + Bow Swiftness (self-heal rider) +
  Antidote; Demon = a P.Atk/P.Def curse + bleed trap + Bow Ferocity (poison rider).
- **These are the first player traps in the game.** `PlacesTrap` has existed since the Trapper was
  sketched and nothing had ever authored one.
- `HitCount: 2` on **Twin Arrows** is his, settled, and unchanged — two independent resolutions, each
  rolling its own crit and its own evasion.

### 🔴 The derived archer kit of three days ago is retired

`BL-185` built Archer Bow Mastery, Split Volley, a cloned Bow Expertise and Killing Focus off his
*"take the elf harmonist skills … increase them with ~20%"* recipe, with a note saying his file would
overwrite them. **It has.** The four defs are kept — `LearnedSkills` persists IDs, so deleting one
breaks anybody who bought it — orphaned in `Skills.ArcherKitRetired.cs` and retired by `Replaces` on
their authored successors. Without that, `archer_bow_mastery` would have stacked a second bow passive
on top of his and `archer_crit_focus` would have been a permanent +20% crit damage nobody could
account for. **The warrior's half stays derived**: `warrior 3rd.csv` is not finished.

### `rogue 2nd.csv` — fifteen discrepancies, all closed

His numbers the code had never caught up with: Precise Shot's MP ladder (52/59/66 — rung 4 read
**34**, a hole in a rising line), Armor Mastery's evasion (7/9/12/12/12, not 7/11/13/13/13), its HP
regen (**2.5**, not 1.2), and Evasion Boost coming **down** to his authored +15 evasion / 15% skill
evasion from the +20/25% it shipped with. And the mastery is **light-armour only** now, on his WEIGHT
column — a rogue in plate gets nothing from it, the same road as the 2026-08-29 robe ruling.

### Five engine additions, each because a row asked for it

- **`SkillLevel.Rank`** — a DoT's TIER per rung. His Venom Stab climbs 3,3,4,4,5,5,…,10, which is
  neither flat nor the `BL-85` level+1, and a tier is exactly what an Antidote has to out-reach.
- **`SkillDef.StacksPerCast`** (per rung) — one cast may lay more than one stack. His 1 → 3 buys how
  FAST the burst fills, not how big it gets; the cap stays at ten.
- **A burst that spent stacks does not hand them back.** Venom Burst carries the venom flags *and*
  `ConsumeStackKey`, so his *"if no stacks present apply 1"* works — but without the new
  `spentStacks` guard the CC arm would have re-applied what the damage arm had just spent, making
  the skill a no-op that refunds itself.
- **`SkillDef.ProcVictimRungs`** — the first procs that pay the ENEMY (Bow Focus's bleed, Bow
  Ferocity's poison). Not contested: his 5% *is* the landing chance.
- **A proc on a BUFF only rolls while that buff is up.** Every proc before this sat on a passive,
  where "learned" and "active" are the same thing — so an archer who had merely *learned* Bow Focus
  would have bled everything he hit for the rest of his life. No new field: a `Buff` category with a
  `BuffKey` says what it is.

Plus `SkillLevel.SkillEvadeChance` (Evasion Boost is a two-rung ladder now) and
`SkillDef.BuffBowRange` (bow range had been passive-only since the masteries were written). Each of
them owes `SkillText.cs` a line and got one.

### `--check` walks both files now, and the tool learned three things

`dual 3rd` and `archer 3rd` earned their `Check.Specs` lines. Each needs **three disciplines**, not
one — the rogue split by race at 40, so a file is the union of three kits, which is what the new
`Disciplines` parameter is for. Three reader fixes came out of the first run, and every one of them
was the tool being wrong about correct code:

- **A short alias must be a whole word.** `as` lives inside "incre**as**e" and "decre**as**e", so his
  Dual Mastery proc clause handed its 10% to ATTACK SPEED and reported six good rungs as defects.
- **A trap's DURATION is its wait and its AOE is its catch radius** (`TrapLifeTicks` / `TrapRadius`),
  not the buff duration and `AreaRadius` a normal AoE carries. 45 rows, reported twice each.
- **The proc override is for PASSIVES.** A buff carrying a proc has timings of its very own; reading
  the payload's numbers hid Bow Focus's real five-minute duration behind a zero.

And `target/…` is now accepted where the code says `enemy/…`: by the letter of his scheme `target`
means "any friendly", and by that letter every offensive row of the archer file is mis-scoped, which
is 105 rows. Nobody authors a two-arrow volley as a friendly buff — he is using it for "the thing I
have aimed at". The comparison that actually caught something (a healer's curse authored
`party/single`, 2026-08-27) is untouched.

### Lure is priced, and finally has all three rungs

His instruction: *"the lure should be at 52,62,74 (with sp for the levels) and mp should be
65,80,95"*. The 200/400/600 reach ladder has been in the catalog since `BL-70` with **only rung 1
reachable** — the note beside it has said "his to place" since 2026-08-19. Rung 1 also moves **40 →
52** with them. Three rows written into `dual 3rd.csv` in this commit; SP off the file's own ladder.

### Also

- **Antidote is registered on the Elf tank at last.** `tank 3rd.csv` has authored those six rows
  since the Bulwark was built and `--check` has printed 🔴 NOT REGISTERED against them ever since:
  the rows existed, the skill did not. `elf_antidote` is a SELF cure, a different id from the
  healer's targeted `antidote`, and one skill now serves all three fighter files.
- **Sprint L2's invented level-40 rung is gone.** It was my pick off the 2nd-class cadence with a
  note saying "one line to move when his level-40 CSV lands". It landed: 46, melee rogue only.
- Prowl 3,400 → **28,000** SP, Vanish 1 → **120,000**, Signal Flare 12,000 → **120,000** — all three
  were stand-ins from before the files existed.
- **Two CSV cells were corrected, both flagged to him.** The Demon's Phantom Jump block carried the
  ELF's skill id on all three rows (a different type and a different rider — two skills cannot share
  one id), and the same block's TARGET said `self/single` for a gap-closer that stuns an enemy.
- **His SP ladder is not quite the tank's**: rung 6 (level 55) is **80,000** in both rogue files
  where `tank 3rd.csv` says 81,000. Thirteen skills across two files agree, so it is authoring.

### 🔴 Two things left open, both his

- **Sprint rung 2 costs 0 MP in his file** and rung 1 costs 10. It is the one MP cell he did not fill
  on his 2026-09-09 pass; the code keeps 16 and `--check` reports the disagreement rather than my
  guessing at a ladder that runs backwards to nothing.
- **Bow Expertise has two prices.** `buffer 3rd.csv` says 42,000 SP and `archer 3rd.csv` says 37,000
  for the same `wc_bow_expertise`. One skill has one price; the def keeps the buffer's.



## 2026-09-09 — 0.118.0: the shield stops being armour (`BL-185`)

⚠ **NEW APK** — `StatsUpdate` loses a field and the item card loses a line.

Your ruling: *"lets remove defence as additional armor … the shield only will provide dmg reduction
based on actual block"*. An S-grade shield carried **+83 flat P.Def**, which was added into
`EffectiveDefence` on **every** hit and then multiplied by every P.Def passive, buff and set on top —
**300 points** on a level-76 tank, **428** at 90 — and a block then removed another slice on top of
that. The shield now pays through one channel only.

### What changed

- **`ShieldDefense` is gone end-to-end** — the `ItemDef` field, the `Entity` accumulator, the
  `StatsUpdate` DTO field, the item-card line, and `EnchantRules.ShieldDefDelta`. `Entity.cs`'s
  `EffectiveDefence` no longer reads it.
- **`ShieldDefPct` became `BlockReductionPct`** and now thickens the block instead of a defence pool
  that no longer exists, in all three channels (passive, buff, armour set).
- **Every authored value halved**, per your numbers: Shield Mastery `30/40/50/50/50/50/60%` →
  `15/20/25/25/25/25/30%`; Shield Hardening (`cast_shield_def`) `30/40/50%` → `15/20/25%`;
  Shield Reinforcement (`wc_shield_reinforcement`) `50%` → `25%`. The CSVs moved with them and now
  say "Shield Dmg Reduction" in those words — so the **×5 IG-units exemption in `SkillCsvSeed` is
  deleted**: column and build are 1:1 again.
- **`StatCaps.BlockChance` 100% → 80%**, your new ceiling.

### Where it lands

| | block chance | reduction | average mitigation |
|---|---|---|---|
| any class, S shield, no mastery | 25% | 25% | **6.3%** |
| tank @90, unbuffed | 50% | 32.5% | 16.2% |
| tank @90, with the buffer | 80% (cap) | 40.6% | **~32%** |

Both of your targets — ~6% for a shield-carrying non-tank, ~32% for a fully-buffed tank — land
without further tuning. And `tank:mage` P.Def falls to **2.03 @76 / 2.37 @90** (from 3.09 / 3.56 two
versions ago) with **no CSV number retuned**: the shield leaving P.Def did it.

⚠ **This reverses playtest-22 ruling `70b`** (*"Shields dmg reduction is never increased by any means
…only chance"*), which `Entity.cs` explicitly enforced. Newest ruling wins, but the consequence is
that the item card's "25%" is no longer the literal number subtracted — the stats window shows the
effective figure, the card shows the item's own.

🔵 **Two things left open, both yours:** the armour sets' shield clause (four sets carried a shield
multiplier; repointing it at block reduction overshot your 40%, so it is **dropped** for now — say if
it should instead buy block CHANCE), and shield enchanting, which now buys Max HP only.

## 2026-09-09 — 0.117.0: the runes become the SHOT, and defence gets its level term (`BL-185`)

⚠ **NEW APK** — the rune buff descriptions and the buff-tooltip text come from `Game.Shared`, and the
defence change moves every P.Def number the client displays.

### The runes are the IG shot now — ×2 on the FINISHED damage, not on the attack stat

Your ruling: *"change the runes to not directly increase the stat (p/m atk) but to double the dmg (as
the IG shots do) … like the opposite of mReduction: it doesn't directly increase the mDef but just
decreases the dmg"*. Both runes now carry `PhysDamageMult` / `MagicDamageMult = 2.0`, applied in
`GameLoopService.FinalizeDamage` — the one seam every hit already passes.

🔑 **THE OLD FORM WAS NOT EQUIVALENT, AND THE PHYSICAL ONE WAS NEARLY A NO-OP.** `BuffPhysAtk 1.00`
read "+100% P.Atk", but physical damage is `77·(pAtk + power)/pDef` — **additive** — so on a
high-power skill it delivered ×1.29, not ×2. Measured at level 90 mythic, the switch changes:

| | before | after | |
|---|---|---|---|
| archer BASIC → mage | 498 | 498 | **unchanged** — +100% P.Atk on a power-0 hit already *was* ×2 |
| archer SKILL → mage | 1037 | 1574 | **×1.52** — the additive-formula loss, returned |
| warrior SKILL → mage | 884 | 1422 | **×1.61** |
| mage SKILL → tank | 206 | 290 | **×1.414** — plain spiritshot → blessed |

The magic side went from ×1.414 to ×2.00, which is IG's **blessed** shot exactly (M.Atk ×4 under the
damage formula's `√`). Measured against your own five in-game rows the real shot is ×2.35, so this
closes ×2.00 of it; the remaining ×1.17 is a separate open question (`MagicK` 91 vs ~107).
The Spell Rune's flat +40 cast speed is unchanged — that half was never part of the shot.

`BL-187` filed for the third rune combining both channels, as you asked. The engine already supports
it (the two fields are independent); what is owed is the economy, not code.

### 🔴 P.Def now scales with `levelMod`, and both defence bases became IG's empty-slot defaults

P.Def was `68 + level²/100 + gear`, with **no multiplicative level term at all**, while P.Atk has
carried `levelMod` since the IG-shape rebuild — so attack outran defence as level rose, the opposite
of IG where both sides carry it and largely cancel. M.Def already had it. Now both do.

The two bases were also ours alone. IG's "base" defence is what a slot pays while **EMPTY** — chest
31 + legs 18 + head 12 + gloves 8 + feet 7 + underwear 3 = **79-80** P.Def, and rear 9 + lear 9 +
neck 13 + rfinger 5 + lfinger 5 = **41** M.Def — and equipping a piece REPLACES that slot rather than
adding to it, so a geared IG character carries none of it. They are now flat 80 / 41 with no level
term. (Your own rows prove the level² term was wrong: your 76 mage reads 911 M.Def against a 333
jewel sheet, which is `333 × 1.64 × 1.65` to within 1% — with **no** additive base.)

🔑 **THE RESULT VALIDATES THE SHAPE.** At level 76 against your measured sheet (P.Def 703 / M.Def 911):

| our tier @76 | P.Def | M.Def |
|---|---|---|
| **rare** | **688 (×0.98 yours)** | **900 (×0.99 yours)** |
| epic | 688 | 892 |
| legendary | 775 | 1009 |
| mythic | 867 | 1134 |

Before the change our mythic read 571 P.Def — **below** your 703 — and rare read 462. Now our rare
tier reproduces your S-grade sheet on **both** channels to within 2%, with epic/legendary/mythic
sitting above it exactly as four rarities over one grade should.

⚠ **WHAT THIS COSTS: mobs hit players ~`1/levelMod` softer** — about −8% at level 20, −29% at 52,
−44% at 90. That is a real PvE softening and it is deliberate: our mob P.Atk curve was refitted off
IG's own creatures, which already assume an IG player defence that carries `levelMod`. It still wants
your eye before it is called finished.

⚠ **The tank:mage P.Def spread barely moved** — 3.25× → 3.09× at 76, 3.75× → 3.56× at 90, against
IG's 1.47×. `levelMod` is a common factor, so it cannot narrow a ratio. This confirms the diagnosis:
the spread is the **heavy Armor Mastery `PDefPct` ladder** (+11%→+15% at the 3rd tier, +20%→+30% at
the 4th, compounding), which is your *"if we have weight based modifiers we should remove them"*.
Those numbers live in `tank 3rd.csv` / `tank 4th.csv`, so they are yours to cut — not retuned here.

### The measurement rig had to move with it, and nearly lied

`tools/BalanceMatrix` computes damage by calling `StatCalculator` directly. While the runes rode on
P.Atk/M.Atk it picked them up for free; now it must apply them itself, so all **37** damage sites are
wrapped in a new `Shot(entity, magic, dmg)` helper. 🔑 **And the first run after that was still wrong**
— the rig hand-builds its rune buff and copied only `Effect` + `Magnitudes`, which is the FLAG half of
the payload; the new fields never arrived and every table quietly *lost* the rune. Ten hand-built buff
constructors now carry the field half too. Same trap as the group-buff bug in 0.102.

## 2026-09-06 — 0.116.0: the warrior and archer damage kits, and the IG damage fit (`BL-185`)

⚠ **NEW APK** — this changes `ClassSkills`, and the client builds its Learn tab locally from the
compiled tables. 🟢 **No `game.db` delete** — no schema change.

### The measurement that started it

You said the damage was *"laughable"* and then supplied a full IG damage matrix **with the stats
behind it** — 64 rows, four attackers × four defenders × four gear grades. Fitting our own formulas
to it killed the first proposal outright: `PhysicalK = 77` is already correct, because the K your
archer and tank rows demand is **flat across all four defenders at every level** (85: 86.7 / 87.0 /
87.0 / 86.0). One free scalar will fit almost any small set of target cells; the inputs are what
tell you whether the formula is wrong. The full fit is [balance/DamageVsIG.md](balance/DamageVsIG.md)
and your table is preserved as [balance/IG-reference.csv](balance/IG-reference.csv).

What it actually found: our ELF HARMONIST — a buffer — hit a buffed level-90 mage for **495** where
our ARCHER hit the same target for **235**. The three ranged rogue disciplines had exactly one
3rd-class skill between them (Signal Flare) and the warrior had only HP Boost. **Both damage kits
were simply never authored.**

### The kits, built from your recipe

*"For archer take the elf harmonist skills and bow passives ... Increase them with ~20%"* /
*"For fighter kit take demon harmonist skills and 2h wepon passives increase them by ~20%"*. Built at
**×1.25**, the midpoint — every number is a source ladder times that factor, and each one names its
source in `Skills.FighterKits3rd.cs`.

- **Warrior** — *Two-Handed Sword Mastery* (the Warlock's ladder, sword, P.Atk 38→125) · *Sundering
  Blow* (Sound Smash's 13 rungs, power 1250→5000, 2H sword) · rungs 6-20 of his own Armor Mastery =
  the tank's heavy ladder **minus the crit-damage reduction**, as you specified.
- **Archer** — *Archer Bow Mastery* (P.Atk 125→750, +400 range) · *Split Volley* (Sound Burst's
  ladder, 900 range, two arrows) · *Bow Expertise* (+12%, the harmonist's rung) · *Killing Focus*
  (+20% crit damage, +700 flat) · rungs 6-20 of the rogue's Armor Mastery = **half the tank's P.Def
  ladder**, your pick when asked.

Archer P.Atk **4494 → 6647**; his skill crit on a buffed mage **618 → 1513 per arrow**. ⚠ Split
Volley fires twice, so per press that is **3025** against the 1500 you named — your recipe applied
exactly (the harmonist's own per-use crit is 2525). Warrior skill crit on a mage **524 → 1088**,
inside your 700-1500.

**Your HP-boost item was already built** and nothing here touches it: `RegisterHpBoost` has given the
warrior rungs 4-10, up to **+1000 max HP**, since it was written.

### Two rig bugs it exposed, both fixed

- **The boss party's three DDs had no 3rd class.** `BL-169` gave the tank and the healer their
  disciplines and stopped there, so `BL-13` had been measuring every boss against two 2nd-class
  Champions and a 2nd-class Sorcerer in endgame gear. It surfaced because the warrior kit landed and
  the boss table did not move by one second. Boss pace with a real party: 60 **84m→69m**, 65
  **93m→70m**, 76 99m→119m, 85 30m→32m.
- **`TopPhysSkillPower` ignored the weapon gate**, picking the highest-power skill *learned* rather
  than the highest castable. Harmless until a 2H-sword skill and a bow skill sat at the top of two
  ladders.

`--dmgmatrix` now prints **per-use** damage beside per-hit, because a two-hit skill read as a clean
hit on your target number when it was double it.

🔴 **New open finding:** party DPS **dips at 76** (679 at 60, 698 at 65, **444 at 76**, 1746 at 85).
A ladder going backwards is a defect by your own monotonic rule. Not caused by the kits — it appears
the moment the DDs get any 3rd class — and it is `BL-170`'s neighbourhood. Filed under `BL-185`.

## 2026-09-06 — 0.115.1: harmonies stop stacking, and a Clear All (`BL-183`, `BL-184`)

⚠ **NEW APK** — `BL-184` adds two buttons. 🟢 **No `game.db` delete.** `BL-183` is entirely
server-side; an old client gets that fix without updating.

## `BL-183` — a harmony is a GROUP over the eight single harmonies

Your ruling: *"Harmony of swift should not stack with harmony of speed. Harmony of warrior replaces
harmony of fury, harmony of might. Same goes hor harmony of (body,ward,bulwark) == harmony of
protection. Think of the as single harmonies and group harmonies -> group buffs replaces singles."*

### It was already written that way, and it had never once worked

`BL-160` (0.109.x) put the eight **single harmonies** on the Spirit Helper's shelf — Ward, Force,
Swift, Alacrity, Bulwark, Might, Fury, Body, 50,000 gold each — and the four **class harmonies** were
authored to tear them off, with your words in the comment: *"his acts as a group one so replaces
them"*. The rule was expressed as `SkillDef.Replaces`.

🔴 **`ApplyBuff` matches `Replaces` against buff KEYS. Every author in the catalog writes skill IDs
into it.** Those are almost never the same string — a ladder rung's id is `buff_<family>_<rank>` and
its key is `<family>`; the single harmonies are `npc_harmony_swift` keyed `npc_h_swift`. So the list
matched nothing, removed nothing, and the two tiers stacked in silence for three versions. Nothing on
screen said so; the ids looked right at both ends of the file.

🔑 The reason ids got written is that `Replaces` has a **second job** — collapsing a superseded skill
off the learn list — and all five of *those* call sites are id-based. One field, two meanings, and the
buff half lost. **The general lesson, and it is the third time this exact shape has cost a version:
when a wrapper and its buff both carry a string, name which of the two a list is holding.**

### What replaces it

**`SkillDef.CoveredKeys` / `SkillLevel.CoveredKeys`** — a childless buff can now declare the families
it contains. Covering is the engine's only "these two occupy the same slot" relationship: a GROUP gets
it free from its `ChildBuffs`, and a harmony (which carries magnitudes, not children) had no way to say
it at all. Declared, the ordinary family contest does **both halves** of your sentence — the class
harmony **evicts** the single when it lands, and the Spirit Helper **refuses to sell** it, without
charging, while the class one stands.

🔑 **Rank had to move with it.** Both tiers sat at rank 100, and at equal rank the engine keeps
whichever has longer left — the bought single runs an **hour**, a class harmony **five minutes**. The
covering alone would have resolved backwards and let a 50k single refuse a Warchanter's own harmony.
Class harmonies now sit one rank above the shelf they cover (`SkillCatalog.HarmonyRank`).

🔑 **It is per RUNG.** A harmony's payload is cumulative and every single goes on sale at exactly the
level the harmony gains that effect, so:

| rung | Harmony of Protection gains | and claims |
|---|---|---|
| 1 @44 | +30% M.Def | Harmony of Ward (sold @44) |
| 2 @52 | +20% HP regen | — |
| 3 @56 | +25% P.Def | + Harmony of Bulwark (sold @56) |
| 4 @66 | +30% Max HP | + Harmony of Body (sold @66) |

Warrior claims Might at rung 4 (@56) and Fury at rung 5 (@58), its first three rungs claim nothing;
Wizard claims Force at rung 1 (@48) and Alacrity at rung 2 (@52); Speed claims Swift at rung 1 (@48).
Covering the whole list from rung 1 would let a level-44 Warchanter strip a level-56 player's
50,000-gold Harmony of Bulwark and hand back nothing — a downgrade the player cannot refuse. Rung by
rung the swap is exactly even: the harmony's number at that rung **is** the single's number.

⚠ **`Replaces` was removed from the four harmonies rather than repaired** — even fixed it is
unconditional and per-skill, so it would have broken the rung rule above. Its id→key resolution was
fixed in the engine as well (it now accepts both), so any other author who wrote ids there gets what
they meant.

### So it cannot go quietly dead again

- **Startup throws** on a `CoveredKeys` entry that names no real buff key — the def's own list and
  every rung's.
- 📐 **`dotnet run --project tools/BalanceMatrix -- --buffs`** prints the covering ladder rung by rung
  against the level each single sells at, and warns if any of the eight is covered by nothing. The
  census's "NPC families not covered" check now includes the eight harmonies, which had been excluded
  on the reasoning that a harmony is covered by nothing — true of a harmony over the *basic* layer, and
  false of these.
- The rule is written into `docs/design/BuffLadders.md` and both CSVs (`buffs.csv` banner, the four
  harmony ladders in `buffer 3rd.csv`).

## `BL-184` — Clear All: every blessing off, ailments left alone

Your ask: *"Add a clear all in the functions menu and in the npc buffer (free) to remove all active
effects (no debuffs)"*. Both, and the NPC one is free.

- **`Functions > CLEAR ALL BUFFS (debuffs stay)`** — the way back from the button directly above it.
  Every other lever on that page puts something ON; the only routes off were waiting an hour or
  relogging, and a relog **restores** your buffs, so that one never worked at all. Both the buffed and
  the unbuffed state are things a balance read needs and only one of them had a button.
- **Spirit Helper > `Clear all blessings   free`** — the missing half of your own preset workflow
  (*"buff fully from npc then remove what u don't need as that class and save it"*). Removing one
  square at a time was already possible; starting over was not, and with a twenty-slot bar the usual
  reason to start over is that you filled it with the wrong set. It **asks first** — it is the one row
  on that window that destroys blessings you may have paid 50,000 gold each for, and it sits directly
  under a row you came there to press.
- **`/clearbuffs [name]`** is the command behind both, so it also works from an old client and can be
  aimed at someone else.

**What survives, and why** — the filter is `BuffInstance.IsDebuff`, the same one every cure and cancel
path in the game already uses. 🔑 That is deliberately *"carries no harmful flag"* rather than
*"carries a buff flag"*: half the payloads in this game are fields rather than flags (CC resistance,
MP cost, heal-received), so asking "is this a buff?" would quietly skip them.

| kept | why |
|---|---|
| **debuffs** | your parenthesis. A free button that also cured poison would make every curse in the game a walk back to town. |
| **internal effects** | the DoT stack counters — bookkeeping, never drawn on the bar, consumed by their own burst skill. |
| **`Cancellable: false`** | the existing "cannot be cured or cancelled" flag (Burn, the boss judgment). All harmful today, so this line is belt-and-braces. |
| **rune buffs / the Item row** | a rune's buff is the item in your bag being worn, not something you were given. `ReconcileTimedItems` re-applies it within the second, so clearing it would flicker the bar and inflate the count with something that never left. |

⚠ **Toggles DO go.** A stance is an active effect and you said all of them. Removing the buff *is* how
a toggle is turned off — the same thing the skill button and the bar's double-click do — so nothing is
left behind claiming it is still on. One tap to put back.

## 2026-09-06 — 0.115.0: the admin buff drawers, FullHeal, and staff flags that survive a relog

`BL-180`…`BL-182`, three asks from one message, all built. ⚠ **NEW APK**, and 🔴 **delete
`Game.Server/game.db`** — `BL-182` adds two columns and `EnsureCreated()` never adds one to an
existing file.

### `BL-180` — `Functions > [Buffs]`, four drawers over every buff in the game

*"add [buffs] -> sub menu to open with 4 more submenues -> single, group, harmonies, marks"*, and
*"Can remove the 4 harmonies as they will be insoide their colection and fullbuff gives harmony mark
anyways"*.

The Functions tab had six hand-written buff buttons on it; every other buff in the game was reachable
only by typing `/buff <name>` on a phone keyboard. It now has **`Buffs >`** and four drawers behind it —
**Singles (31) · Groups (9) · Harmonies (14) · Marks (4)**, fifty-eight buttons. Each sends
`/buff <id>`: that buff's **top rung for one hour**, the same thing the Full Buffs button hands out.
The four Mark buttons moved into the Marks drawer; `[Full Buffs]`, `[War Might]` and `[War Bulwark]`
stayed, because those two share one buff key and the button is how you swap them.

🔑 **The lists are DERIVED** (`SkillCatalog.AdminBuffMenu`, new `Skills.AdminMenu.cs`) — the rule the
gear, town, zone and class lists in that window already run on, and for the reason the hand-listed WPF
menu proved: a typed list goes stale and whole tiers silently vanish from it. Add a harmony to
`buffer 3rd.csv` and its button appears with no second edit.

The universe is the game's **two shelves, unioned**: what a max-level buffer CLASS can cast
(`AdminBuffSet` + the three a full buff deliberately withholds — Shrouding Hymn, Bow Expertise, War
Bulwark) and what the Spirit Helper SELLS (`NewbieBuffSet`, incl. the eight single harmonies and the
three Marks). It reads both and writes to neither — the only relationship those two separate shelves
are allowed to have.

**Which drawer is asked of the data, and the order of the tests is the design:**

| drawer | decided by | why not something else |
|---|---|---|
| Mark | the shared buff **KEY** (`healer_mark`) | so the *Harmony Mark* files as a Mark, where he listed it |
| Harmony | the **NAME** | the 4 class harmonies carry `Magnitudes`, the 8 NPC ones are one-child wrappers — no structural test sees both |
| Group | **STRUCTURE** (>1 child) | the same test that puts groups first in a full buff |
| Single | everything else | |

⚠ **One name, one button.** "Might" is the Warchanter's own ladder AND the NPC's hour-long single, so
the union is deduplicated by display name, strongest first — exactly what `MatchBuffsByName` already
does when you type it, so the button and the command land the same buff. Nineteen NPC duplicates are
shadowed that way and the dump names them.

📐 `dotnet run --project tools/BalanceMatrix -- --buffmenu` prints all four drawers plus anything
grantable the menu fails to reach. Discovering a mis-filed harmony on a phone is how one survives
three versions.

⚠ Server side: **an exact skill id now wins outright** in `/buff`'s match, ahead of the fuzzy ladder
(acronym → words-in-order → prefix → substring). Ids are unique so it can never be ambiguous, and
fifty-eight buttons is too many to leave riding on a fuzzy search. Restricted to skills that land a
timed effect, so an exact id on an attack skill still falls through to the name search.

### `BL-181` — `FullHeal`: both pools to full, instantly, in combat

*"Functions -> FullHeal -> heals instantly mp/hp in combat or no"*. A button, and `/heal [name]`
behind it.

🔑 **It is a SET, not a heal.** The healing path would drag in everything that makes a heal interesting
and useless here — healing-received modifiers, the potion cooldown, the in-combat refusal, the aggro a
heal generates — and the point of the button is a known starting state *mid-fight*.

⚠ **Not a resurrection.** Death has its own path; a command that silently did both would make "did
that kill me?" unanswerable in a test. On a dead character it says so.

### `BL-182` — `/god` and `/invis` survive a relog

*"after a DC(long stay in background) the char that is incis+god is visible and mortal .. whatever i
left my admin/owner with he stais again in the next login/reconnect"*.

🔑 **Why it looked intermittent.** A short disconnect re-attaches to the LIVE entity and always kept
both flags; a long one evicts the character to the database, and neither flag was ever stored — so the
state was lost at exactly the moment he noticed it.

Two columns on `CharacterRecord`, written the instant either command is typed rather than at the next
autosave (a crash in between is the case this entry is about).

🔑 **Re-applied only if the character is still an ADMIN**, and the load does it *after* the role.
`IsAdmin`, not `IsStaff`: both commands are on the admin side of the allow-list, so a Moderator could
never have set either. `/role` clears god mode on a *live* demotion, but a character demoted while
OFFLINE would otherwise log back in immortal and unseeable off a row nobody could reach. The stored
bits are not wiped by that refusal — a re-promotion restores exactly the state he left.

⚠ Nothing new pushes the badge or the 0.4 fade: the tick loop's own change-test (`PushSelfState`)
sends it on the first tick after entering the world, which is the same mechanism that covers hide and
stealth.


## 2026-09-06 — 0.114.0: six of his nine asks — four admin-menu tidies, the chat clipboard, `/return`, and the scroll faucet

`BL-173`…`BL-178`. Six of the eight entries filed from his 2026-09-05 messages; `BL-172` (`/unstuck`)
and `BL-179` (the two TEST skills) were explicitly held back for a later pass.

### `BL-178` — the Android copy/paste menu, and it was our own switch all along

His report: *"I can paste from the keyboard copy clipboard (SwiftKey keyboard) but the context menu
after selection that shows 'copy/cut/select all' is not there ... I do not want new inner copy/paste
system if we can make the normal work"*.

🔑 **It was one line we set on purpose.** `UiKit.InputField` sets `shouldHideMobileInput = true` on
**every** field in the client. With that on, Android's real `EditText` is off-screen and the soft
keyboard only delivers keystrokes; TMP owns the buffer and draws its own caret and its own selection.
**The clipboard menu belongs to that hidden native view**, so it could never appear — which is exactly
the symptom: selection works (that is TMP's), the menu does not, and paste only works when the
*keyboard* sends it as keystrokes.

⚠ **And the line is there because of him.** 0.47.0: *"if there is a 1 I cannot make it 10 — it becomes
01"*, plus a saved password that could not be edited at all. On Android the keyboard owned the buffer,
so tapping inside a field could not move the caret. `shouldHideMobileInput = true` is what fixed that.
**The native context menu and the working caret are one switch pointing opposite ways.**

So it is **per field** now, not global: an optional `nativeMobileInput` argument, `true` for the **chat
entry box alone**, `false` (unchanged) everywhere else. Chat is where you type fresh text rather than
edit a pre-filled value, so the caret bug has nothing to bite on there, while the URL / password / gold
/ tune fields — the ones that bug was actually about — keep the behaviour that fixed them.

⚠ **Look at it on the device before trusting it.** With the switch off, Android puts its **own** input
strip above the keyboard and that is where you type and where the menu appears — our on-screen box is
no longer the thing being edited while the keyboard is up. That may also make the keyboard-lift offset
on the chat row pointless for that one field. Neither is a blocker; both are things to see.

🔵 **Not covered:** copying a name *out of the chat log*. The log is a plain `TextMeshProUGUI` with no
selection at all; say the word and it becomes a read-only `TMP_InputField`.

### `BL-175` — the admin Teleport menu: a `[Bosses]` page, three kinds of clutter gone

*"admin tp menu to have [bosses] whit all the bosses inside and from 'zones' all the training dummies,
all the watchmen and the bosses to be remived"*.

A fourth page, `Bosses >`, built from the boss-ranked zones — field and dungeon alike — showing level,
name and a `[solo]` / `[world]` mark, landing you at the zone CENTRE (a boss ring is small and holds one
creature; arriving at its edge means walking in anyway).

`Spawn zones` now lists hunting grounds only. 🔑 **All three exclusions are asked of the DATA, never of
an id list:** the zone is boss-ranked, or its roster is `MobType.Dummy`, or it is `MobType.Guard`. The
six dummy grounds and both towers of every town's guard post are generated by `WorldPlan`, so a
hand-written list would go stale the first time a town is added.

### `BL-176` — the admin Items tab: three flat walls become four sub-pages

Three of his asks, one change: *"ecnahnt scrolls to be one button and then selection per grade"*,
*"group of attribute scrolls -> click attri scroll and opens all of them like a selection"*, *"buttons
with potions/stones -> potions have all healing/mp potions, stones to have all stones skills and
holy/etc"*.

The page printed **eighteen** enchant rows under six headers, six attribute rows, and scattered the
potions across three headers — one screen, no grouping, on a phone. The drill-down pattern was already
in the file (`Crafting materials >`, `Blueprints >`); it is applied four more times:

| button | opens |
|---|---|
| `Enchant scrolls >` | a grade picker (F…S), then that grade's three types |
| `Attribute scrolls >` | all six rarities |
| `Potions (HP & MP) >` | every healing and mana rung |
| `Stones >` | all four stone reagents, plus a `GIVE ALL x50` |

⚠ **Every one stayed GENERATED**, which was the trap on this entry: the enchant rows come from
`ItemCatalog.EnchantScrollBands` × `EnchantScrollTypes` precisely so a scroll cannot be authored and
left unreachable, and a hand-written grade list would lose that. The attribute page now reads
`ItemDef.AttrScroll` off the catalog instead of the six constants it used to list, and the stones page
sweeps the catalog by id.

🔑 **And that sweep found a real gap, not just clutter:** the flat `Reagents` header listed only the
Elemental and Skill stones, so the **Holy and Physical stones** — the divine and fighter twins that
skills burn identically — **could not be obtained from the admin menu at all.** The Instant Healing
Potion was missing the same way. Both are on the new pages.

### `BL-177` — the SP button gives 10kk

*"admin menu function SP button to give 10kk not 1kk"*. One number, and the same reason the Gold button
beside it already gives 10,000,000: a 3rd/4th-class skill ladder costs far more than 1kk to walk, so
the old button meant ten taps before the thing you wanted to test was learnable.

### `BL-173` — `/return`: it already existed, and it wanted two numbers

*"Like a normal rerun just 60s/10s not 30s/5m like now (I noticed each time I put skills to bar but
somehow ignore as I haven't noticed :))"*.

`SkillCatalog.ReturnSkill` (`return_town`) is granted to **every** character by `AutoLearnCoreSkills`
and has been on his bar the whole time.

| | was | now |
|---|---|---|
| cast | 30s (`CastTicks: 300`) | **60s** (`600`) |
| reuse | 5 min (`CooldownTicks: 3000`) | **10s** (`100`) |
| any damage cancels it (`FragileCast`) | yes | **yes — KEPT** |

✅ **`FragileCast` stays**, his ruling against my recommendation, and it is the better call: fragile +
10s reuse is a **free out-of-combat return**, retryable the moment you disengage, and it takes nothing
from either scroll — the plain scroll still buys 10s instead of 60s, the Ultimate still buys the escape
from a fight you are losing. Dropping fragile would have made this a slow Ultimate scroll and devalued
both. ⚠ The 5-minute reuse was the only thing that made it feel like a resource; the 60s channel **is**
the price now.

Plus a **`/return` chat alias** — an alias, not a second mechanism: it casts the same skill, with the
same refusals. Like `/offline` it sits above the admin passthrough, or a non-staff character would be
told "unknown command" for a command meant for everyone.

### `BL-174` — Return and Resurrection scrolls come off ordinary mobs

*"remove scroll of return and resurrection from all mobs can leave them only on elits, and the starting
3 mobs can keep droping return scrolls and no resurrection (pig/fox/goblin)"*.

Both scrolls sat in the **ALWAYS** group in `MobCatalog.StandardDrops`, which every creature carries.
That group had already been cut for them twice — playtest 15, then playtest 17 `E1`, where **550 return
scrolls by level 23** was the finding. This is the third cut and the one that finishes it.

The shape is the one `EnchantScrollDrops` already uses: a **rank-gated layer**, so a scroll is authored
**once against the rank that earns it** rather than subtracted from a table everything shares. New
`MobCatalog.UtilityScrollDrops(level, rank)`, layered by the kill roll and by target-inspect exactly as
the enchant and elite-mat layers are. Measured after the change:

```
lv 20 Normal: (none)
lv 20 Elite : Return 2.500%  Resurrection 0.250%
lv 20 Boss  : Return 5.000%  Resurrection 0.500%
lv 80 Elite : Return 2.000%  Resurrection 0.200%  ULT Return 0.150%  ULT Resurrection 0.015%
3 of 95 templates still carry one — ridgeback_pup (1), fox (4), goblin_scout (8), return only.
```

🔑 **The numbers are the old ones; the cut is entirely in WHO pays them.** An elite is a camp you go to
rather than a thing you walk past, so the same 2.5% against a much smaller population of kills is
already the reduction; re-tuning on top would cut the same faucet twice. A boss pays double, this
file's standing rule for a boss row.

🔑 **The potions did not move, and that is arithmetic rather than luck:** in a guaranteed group a
member's authored weight **is** its marginal per-kill chance (the group fires at the SUM, then picks
weighted), so removing members changes how often the group fires and not what the survivors pay.
Measured: `potLow` 2.00%, `potHigh` 1.00%, before and after.

🔑 **The Ultimate pair moved too.** They are Return and Resurrection scrolls, they sat in the same
group, and they are stocked by no vendor — so this layer is now their only source in play, which makes
them an elite/boss prize instead of a 75+ trickle off every creature.

⚠ **It cuts a faucet with nothing replacing it**, which is the stated intent: from level 9 up a return
scroll comes off an elite or off a vendor. Noted so the next playtest does not read it as a bug — and
it is what raises the value of `BL-173`, since the free 60s Return is now the thing you fall back on
when you forgot to buy one, which is the *"if you forgot to buy"* in his own spec.

🔴 **One trap worth recording:** `StandardDrops` gained the mob **id** (not a rank — rank is a property
of the SPAWN, so a template's table cannot know it), and the starter trio was first written as a
`static readonly HashSet`. That threw on the first run: static fields initialise in declaration order
and `All = Build()` is declared at the top of the class, so any collection field is still null when
Build() reaches StandardDrops. It is a method now. The file's own comment at the mat rungs warns about
this exact thing.

⚠ **NEW APK** — four of the six are client-side, and the client builds its skill cards locally, so the
60s/10s Return also needs it. Protocol is unchanged (33): no new wire.

## 2026-09-05 — 0.113.1: the treant is a FIELD boss, and the world boss is a tier of its own

Two corrections to 0.113.0, both his, hours after it shipped.

### 🔴 The Valley Treant was mis-filed as the world boss

0.113.0 put it on the 2h/3h enrage ladder because it respawns every 21 hours — **I read the respawn as
the classification**. His correction: *"The treat is field boss (same as dungeon one) world boss is a
clan/party of clans mass pvp massacre where the boss is the target"*. **A world boss is a kind of
ENCOUNTER, not a rare spawn.** The treant is back on the ordinary 20/30-minute field ladder, and there
is now no world boss in the game at all — which is the honest state: the tier exists and nothing wears
it yet.

### `BL-171` — the world-boss stat rung

*"that boss will have about x2~3 aditional stats and x10 additional hp .. So now if boss have 28k p atk/
6kk hp a world one will have 50~60k p atk and 120kk~180kk hp(6kk x2~3 x10) so several parties can fight
it while fighting others for the best loot in the game"*.

`BossProfile.World` takes **×2.5 on every stat** (attack *and* defence — *"x2~3 aditional stats"* is not
the attack columns alone) and **×25 on HP** (his ×2~3 × ×10), both measured off the SOLO boss, which is
what his own "(6kk × 2~3 × 10)" starts from. At level 90 that is **171.7 kk HP** — inside his
120-180kk — with 3,860 P.Def. Its enrage ladder is 2h/3h.

🔑 **It is a FLAG on the profile, not a fourth `MobRank`.** Fourteen places in
GameLoopService/Entity/StatCaps test `Rank == MobRank.Boss` for behaviour a world boss wants *unchanged*
— control immunity, the zone-HP exemption, `AutoAttackBoss`, the raid-level lock, boss judgment, the exp
rank, the plate title. A new rank meant fourteen edits with a silent bug waiting at any one of them, for
numbers the profile can carry. `BossProfile.Enrage1`/`Enrage2` now resolve the ladder a boss's KIND
implies, so a profile only writes a time when it wants an unusual one.

### What it measures, and the two things it leaves open

One 5-man party does **694 dps** against a level-90 world boss, so 9 full parties need **7.6 hours** and
2 hours would take 34 parties. Either the pool comes down or a world boss is explicitly an all-server
event — his call, and it is one constant.

⚠ And his two examples disagree: *"28k p atk → 50~60k"* is ×2 off an **escorted** boss while
*"6kk × 2~3 × 10"* is off a **solo** one. The solo base is used for both, giving ~128k P.Atk rather than
50-60k — **9.3% of his mythic-S tank's pool per swing**, against 4.4% at his number, which would be
softer than a solo boss already hits. Flagged in `BL-171` rather than guessed at.

Nothing is authored with the flag: the encounter needs a place, the mass-PvP rules and the loot, which
is `BL-171`. No protocol change, no schema change, no new APK.

## 2026-09-05 — 0.113.0: bosses get his ×2/×10, a `solo boss` doubles both, and the enrage timer finally means twenty minutes

His ruling, after soloing the level-90 boss on a tank in epic A gear and finding it could not kill
him: *"bosses as we desided get x2 atk and x10hp, if boss is solo gets another x2 on both (atk/hp) …
Now I just want to feel it and going solo vs boss to be nearly impossible"*.

### `BL-166` — a boss finally has a STAT block beside its kit

His question was *"are bosses separate from the mobs file (in code .cs)? they need their own to be
edited/added - stat and skills as well"*, and the answer was **half**. `BossCatalog.cs` has held a
per-boss `BossProfile` — skill rotation with HP windows, phase script, add waves — since bosses were
built. The STATS were not there at all: a boss was the creature curve × `MobRankScale`, which is **one
set of numbers every boss in the game shares**, and three of the four boss templates carried no
`MobMod` either. `BossProfile` now carries `Solo`, `HpMult`, `PAtkMult`, `MAtkMult`, `PDefMult`,
`MDefMult` and its own two enrage times, so **one entry in one file is the whole boss** and the rank is
the DEFAULT a profile overrides — his *"the curve is the base and every boss edit is making the boss
unique"*.

### `BL-167` — the two ladders, and the timer that was lying

`MobRankScale` now applies **×2 attack and ×10 HP to every boss** (the War/Spell Rune every boss
carries), and **×2 more on both when the profile says `Solo`** — a boss with no escort of 2-5 fighters.
The three dungeon bosses are `Solo: true`; the Valley Treant is not, because it calls two adds in its
own phase script.

🔴 **And the enrage timer was ninety seconds.** `BossEnrageTicks = 900` at 10 ticks/sec — it read as
"~90s" in its own comment and everyone including the owner believed it was on a twenty-minute clock. It
fired **once**, for **×1.5**, forever. It is now his ladder: **×2 at 20 minutes of engaged combat, ×4 at
30** for a field or dungeon boss, **2h and 3h** for a world boss, carried per-profile.

🔑 **The enrage moved out of the attack fields and onto a SCALE** (`Entity.MobEnrageScale`, applied in
`ApplyMobScale`). It used to be multiplied into `AttackPower`/`MagicAttack`/`BasicAttackPower` in
place — which is exactly what the comment on `MobHpScale` warns against, because a recompute rebuilds a
mob's stats from the level curve. **Any buff, debuff or mod change landing on an enraged boss silently
un-enraged it.** That is a real bug fixed on the way past, and the leash undo is now "set it to 1 and
recompute" instead of dividing by a magic number that had to stay in step.

### What it measures at (`BL-169`'s honest party)

**His numbers land exactly where he read them off IG.** At level 90 a boss is **3.43 kk HP escorted and
6.87 kk solo** against his *"bosses 85 with 3~6kk hp not like out 350k"*, and the top of the game is in
or near his band — 85 escorted is **30 minutes**, 90 escorted **33**, 90 solo **65**.

🔴 **But below 80 the same flat multipliers are far too much, and this is not a small miss.** A level-44
dungeon boss now takes a full party **109 minutes** escorted and **219 solo**; 60 and 76 are 84 and 99.
The cause is not the boss curve — it is that **party dps is flat at ~370-560 from 44 to 76 and then
triples to 1,884 at 85** when the gear tier flips to S. A flat ×10 lands the top correctly and
overshoots the bottom by 3-7×. Related: at levels 20-44 one basic attack now takes **91-95% of a robe's
pool**, which is against his own *"not one shooting"* line.

⚠ **Left as ruled, and reported rather than quietly tapered** — the numbers are his and the endgame,
where he is playing, is right. The fix is a decision about the **cliff at 80**, not about bosses. See
[design/BossRework.md](design/BossRework.md).

No protocol change and no schema change: `EnrageStage` and `MobEnrageScale` are runtime-only fields on
a mob, and mobs are not persisted. **No new APK** — a boss's stats are computed server-side.

## 2026-09-05 — `BL-169`: the balance rig was measuring a tank nobody plays

No game code changed. This is `tools/BalanceMatrix` and two design documents — but it invalidates a
number the whole boss design rests on, so it is worth a full entry.

### What was wrong

`BalanceMatrix.BuildBossParty` built its tank as `BuildPlayer(Human, Fighter, level)`, and that
function took **no discipline** (so at level 90 it was still a 2nd-class Knight) and applied **exactly
one buff, the War Rune** — no NPC buffer at all, though the buffer has been in the game since
`BL-149`…`BL-162` and every real player walks out of town with it up. Back-computed from its own
output, the level-90 tank in that table sat at **~1,533 P.Def** against the owner's real **2,600**.

Two more defects surfaced while validating the fix:

* **`ApplyNpcBuffs` only ever offered `NewbieBuffSet`** — the *levelling* shelf. `BL-160` put eight
  single harmonies on the NPC and `BL-161` put the three Marks on it, and the census was never told.
* 🔴 **`quality: "epic"` silently meant MYTHIC.** `BuildPlayer` mapped `null or "epic"` to the bare
  item id on a stale comment (*"the bare id IS the Epic"*), while `ItemCatalog.QualityId` is explicit
  that **Mythic is the authored item and carries no suffix**. So every default caller has been
  measuring a full mythic loadout. ⚠ `_mythic` is not an id: asking for it prints "missing item" and
  dresses a naked character.

### What changed

`BuildPlayer` gained `npcBuffed` and `gearTier`; `ApplyNpcBuffs` gained `fullShelf` (harmonies + one
Mark). The boss party now takes `Discipline.Bulwark` on the tank and the full shelf on all five. Both
new options are **opt-in**, because the other tables in the tool were read and signed off unbuffed and
moving all of them silently would make the diff unreadable.

The "is a party mandatory" table gained `gear` / `bare` / `tankPDef` / `tankHP` / `blocked` /
`avg-swing` columns, and there is a new table — **`BL-169`: DOES THE RIG MATCH HIS SCREEN** — that
prints one level-90 tank in the owner's two gear sets with **his own readings hardcoded as the expected
values**. 🔑 It is the only falsifiable table in the tool: every other one prints whatever the formulas
say and cannot disagree with anything, which is exactly how a straw tank survived this long.

### What it revealed

Party dps at 90 went **270 → 1,752 (×6.5)** and boss time-to-kill went **1,274s → 196s**. Every level
from 60 up now prints **TOO FAST**, and the tank verdict from 60 up is **"a tank cannot feel it"** — at
90 a boss swing costs 1% of his pool and he stands there **327 seconds unhealed**. `BL-13`'s 10-30
minute band *and* its ×4 attack multiplier were both fitted against the straw tank, which is precisely
the complaint that opened this pass: *"our 90 boss cannot kill my 90 tank with epic A gear"*.

Validation against his screen: **bare P.Def within +9%** (the gear + passive model is right), buffed
P.Def **+32%** consistently (the buff layer — the rig buys all twenty-nine shelf items where he buys
some), and Max HP **+45/62%**, which does not track the P.Def gap and is therefore a separate cause.

Full write-up, including his rulings and what is still open:
[design/BossRework.md](design/BossRework.md). New backlog entries `BL-166` (a per-boss stat block
beside the per-boss kit `BossCatalog` already holds), `BL-167` (the attack ladders and a real enrage
timer — the current one is **90 seconds**, not the 20 minutes it looks like: `BossEnrageTicks = 900` at
10 ticks/sec), `BL-168` (boss HP), `BL-169` (this).

## 2026-09-04 — 0.112.3: three shields, one weapon, and a message that fired 10×/s

Three playtest finds off 0.112.2, all three real, all three fixed. Two are missing **gates** on the
tank's kit and one is a **flood**.

### The three Shocks are shield bashes again

*"I can use shield shock and silencing shock without a shield .. (numbing shock I guess is the same)"* —
and it was. `Shield Shock`, `Numbing Shock` and `Silencing Shock` now carry
`RequiredShield: ShieldGate.Required`, which is the **same field both Shield Smashes have carried since
the Bulwark was built** in 0.105.0. Nothing bespoke was needed and nothing new was invented: the general
armour gate already refuses the cast in `BeginSkill` and `SkillText` already prints a "Requires a shield"
line off the field, so setting it does the whole job on both sides.

🔑 **It was an omission, not a decision.** The Smashes were authored with the gate because his CSV row
said `/shield` in the WEIGHT column; the Shock rows' WEIGHT cell was **empty**, so there was nothing for
`--check` to disagree with. That is the same shape as 0.112.2's invented ×1.1 MP regen, in reverse — a
checker that compares cells cannot see a gate that **neither** side asserts.

| skill | rungs | file |
|---|---|---|
| Shield Shock | 4 + 15 + 8 = **27** | `tank 2nd.csv`, `tank 3rd.csv`, `tank 4th.csv` |
| Numbing Shock (Human, Demon) | **8** | `tank 4th.csv` |
| Silencing Shock (Elf) | **8** | `tank 4th.csv` |

Every one of those rows now reads `/shield` in WEIGHT and carries `Requre: Shield;` in DESCR, his own
phrasing off the Smash rows — so `--check` compares the gate from here on, in both directions.

### Grapple wants a sword or a blunt

*"Also grapple need sword/blunt"* — `RequiredWeapon: Sword | Blunt`, a **bare type pair**, which in this
engine means *any hands of it* (playtest 28's ruling). A two-handed sword still grapples; a bow and a
pair of daggers do not. WEAPON cell `sword|blunt` on all eight rungs, the same cell `strike` has carried
since `fighter 1st.csv` was written.

⚠ **Deliberately NOT shield-gated.** The three Shocks are shield bashes; Grapple is a reach-and-haul at
600 range, and he named a weapon and not a shield. One word and it gets one.

### The potion refusal that fired ten times a second

*"Drinking hp pot let say uncommon then my hp staying below I get system msg flood 'a stronger effect
(uncommon healing) is already active'"*.

🔑 **The message was right and the messenger was wrong.** The Potions tab is a **fallback ladder** by
construction — common@80 / uncommon@70 / rare@50 — so while the uncommon HoT is running, every weaker
armed line is *correctly* refused on rank. The bug is that `AutoPotions` re-walks the whole ladder on
**every tick**: one refusal is information, 600 a minute is a wall of text over the fight.

`UsePotion` grew a `quiet` parameter and **the autopilot always passes it**; the manual command keeps
every line, because a player who taps a bottle is owed an answer.

⚠ **It silences ALL the refusals, not just the rank one**, and that is the point rather than a
side-effect — the same per-tick loop would equally flood `"{skill} is on cooldown"`, and an armed MP
potion drunk while PvP-flagged would flood *"cannot be used while you are flagged for PvP"* for the
whole sixty seconds of the flag. Both were live and neither had been reached yet.

### Build

`--check` is green on all 15 files, `Game.sln` and `Assembly-CSharp.csproj` both build clean.

⚠ **NEW APK.** The gate itself is server-authoritative — an old client is refused at the cast either
way — but `SkillDef` and `SkillText` live in `Game.Shared`, which the client compiles in and reads
LOCALLY for the skill cards, so without a fresh APK the cards keep promising a shieldless Shock.
`ProtocolVersion` is unchanged.

## 2026-09-04 — 0.112.2: the tank's MP-regen column is entirely his

One ruling and one build. *"Remove the x1.1 mp regen from tank 20~32, at 36 he jumps to +3.1
directly."* Done — and with it the tank's MP-regen column has no invented number left in it anywhere:

| level | Heavy Armor Mastery MP regen |
|---|---|
| 20 / 24 / 28 / 32 | **nothing** (was an invented ×1.1) |
| 36 | **+3.1/s** (his `tank 2nd.csv` cell; the code had 3.4, and as a multiplier) |
| 40-74 | **+3.5 → +5.1/s** (was ×3.5 → ×5.1 on the whole regen chain) |
| 76-90 | **+5.1/s** (his cells first read x3.4 — the mage's number) |

🔑 **`--check` could never have found the ×1.1**, and that is worth keeping in view: the tool compares
what your CELLS say against the code, so a value the code grants and no cell mentions appears in no
comparison at all. It was found by reading the rungs while converting the column, and it survived
because nothing was looking for it. **An unauthored value is invisible to a CSV checker by
construction** — the only defence is reading the code beside the file.

### ⚠ AND THEY WERE NOT SERVER-ONLY, SO BOTH ARE REBUILT

You asked me to build the server *if* these changes only affect it. They do not, so **both artifacts are
fresh**: `builds/Game.Server-0.112.2.zip` and `builds/L2Clone-0.112.2.apk`.

The *behaviour* is server-authoritative — regen is computed server-side and so is what you may learn —
but all three changes live in **Game.Shared**, which the Unity client compiles into the APK and reads
LOCALLY for two things: the **Learn tab** (built from `ClassSkills`) and the **skill cards** (built from
`SkillText`). A server-only build would have left the phone showing the three whisp rungs at **91** and
Heavy Armor Mastery promising **×5.1 MP regen**, while the server quietly did the right thing — a
display that disagrees with the game, which is the failure mode the local-build rule exists to catch.

🔴 **`game.db` delete still owed** (Backlash stopped being auto-granted in 0.112.0).
## 2026-09-04 — 0.112.1: the tank's WEAPON MASTERY, and the last two CSV corrections

Your read of the pass, the same day, and both halves of it are in this build.

**1. THE `mpReg` MYSTERY IS SOLVED, AND YOU WERE RIGHT ABOUT WHERE IT CAME FROM.** Your words:
*"The x3.4 and x5.1 mp regen is + … the mages one we did, it stayed from there"*. Checked rather than
assumed: **`healer 4th.csv` carries `mpReg +3.4`** — a plus, not a multiplier — on all fifteen of its
own armour-mastery rows, and nothing else in the folder has a 3.4. So the tank's `x3.4` was the
mage's number sitting in the tank's file, exactly as you said. The tank's own column is the `x` ladder
that ends at **x5.1**, which is what 0.112.0 held it at and what the cells now say.

**2. `Shield Smash - Power`'s cells are corrected too**, on your *"fix all the csv to match what you
told me needed fixing"*. Its crit-damage column restarted at 15% and re-trod 19/24/28/33 back to 35;
the code has shipped it flat at **35% / 15%** since 0.112.0 (its Rate twin is flat at 50%/25% across
the same eight rungs) and the eight DESCR cells now agree. That was the last of the six — **all of
them are now fixed on both sides**, and `BL-165` is down to the three things that are genuinely yours.

### 🔴 …AND THEN TWO MORE, AFTER THE BUILD WAS PUBLISHED

⚠ **`builds/Game.Server-0.112.1.zip` and `builds/L2Clone-0.112.1.apk` were made BEFORE these two and
do not contain them.** You said no new server/APK is needed for it, so nothing was rebuilt — but if
you want the MP change live, or the last whisp rung to read 90 in the Learn tab, say the word and it
is one command.

**1. 🔴 THE TANK'S MP REGEN IS ADDITIVE, AND IT NEVER WAS.** Your ruling: *"The mp regen of tank is
also additive, not multiplicative … Armor mastery of tank 4th still says x5.1, it should be +5.1 and
build that way"*. Correct on every count, and the evidence is your own files:

- `tank 2nd.csv` already writes `mpReg +3.1` at level 36 — a **plus**, where you got it right. Only
  the 3rd and 4th tiers switched to `x`, and that was the notation slip.
- Read additively the whole thing is **ONE ladder across three tiers: 3.1 → 3.5 → 3.9 → 4.3 → 4.7 →
  5.1**, in exactly the units the mage's armour mastery has used since `BL-92` — and the comment on
  the MP formula says so in as many words: `MpRegenBonus` *"carries the weapon-mastery ladder
  (+1.5…+3.4) that used to be a ×1.5…×3.4 multiplier and was the entire reason a mage could spam
  forever"*. **The mage was converted that day and the tank's column was not.** That is your *"the
  mages one we did, it stayed from there"*, and it was two separate leftovers from the same pass.
- So it is <c>PassiveEffect.MpRegen</c> (flat MP/s, outside the multiplier chain) on all three tiers
  now, not `MpRegenPct`. **It is a real nerf and an intended one** — a level-74 tank was multiplying
  his whole regen chain by 5.1 and now adds 5.1/s, which is the same correction the mage took.
- ⚠ **I went past the 4th tier to do it**, because stopping there would have put a cliff at level 40
  (×410% → +3.5/s). The 3rd- and 4th-tier cells now read `+`, and the level-36 rung is your `+3.1`
  rather than the `3.4` the code had invented. If you only wanted the 4th, it is one array.
- 🔵 **One thing left alone and flagged** (`BL-165`): the four rungs below level 36 still grant ×1.1 MP
  regen, which **no `tank 2nd.csv` row authors** — it is mine, it has shipped since the 2nd class was
  written, and removing it is a balance change you have not asked for.

**2. ✅ THE THREE WHISP LADDERS END AT 90, NOT 91.** *"The intelisence of vsCode or whatever with tab
key make it go by 2 from 89lvl and I missed it"*. Binding, Healing and Weapon Breaking Whisp now read
77 / 79 / 81 / 83 / 85 / 87 / 89 / **90**, and `Check.Specs` is back to the plain 76-90 band. 🔑 **An
editor's autofill is a source of typos like any other** — the last rung of an odd-start ladder is
where to look for one. It shipped as 91 because the level was in your file and level 91 is genuinely
reachable (`ExpCurve.MaxLevel` is 100), so it was flagged rather than straightened; that was the right
call for the wrong number, and this is why it gets flagged rather than silently "fixed".

`--check` is green on all fifteen files, and both changes were proved rather than trusted: the mpReg
field was broken on purpose (5.1 → 6.0, and a 3rd-tier rung → 9.9) to watch the checker report them,
then reverted.
### 🔑 AND THE WEAPON MASTERY YOU FORGOT — fifteen rungs, 76-90

*"Make the 15 rungs of it in the csv, going from p.atk +90@76 to +200@90, keeps the x1.085 patk as
well and adds 1% @76 to 79, 3% @80 to 84 and +5% @85+ atack speed"*. Written into `tank 4th.csv` in
your own grammar (`sword|blunt/1`, *"with 1h sword/blunt: p.Atk x1.085 and p.Atk +N and attack speed
+M%"*) and built as rungs 21-35, on the every-level band and the standard price ladder.

- 🔑 **ATTACK SPEED IS NEW TO THIS PASSIVE.** Its 2nd and 3rd tiers are flat P.Atk plus ×1.085 and
  nothing else — the 4th is where a Bulwark's sword-and-board finally starts swinging faster. Three
  flat BANDS rather than a per-rung ladder, which is your shape: inside a band a rung buys only the
  flat P.Atk.
- ⚠ **The flat ladder is the straight line between your two endpoints, rounded** — you named 90 and
  200 and nothing between, and 110 over fourteen steps is 7.857 a rung: 90 / 98 / 106 / 114 / 121 /
  129 / 137 / 145 / 153 / 161 / 169 / 176 / 184 / 192 / 200. Monotonic at every step. If you want a
  shape rather than a line it is one array.
- ✅ **The checker really reads the new cell** — proved by breaking it on purpose (1% → 2% in the code)
  and watching `--check` report four rungs, then reverting. A checker's silence is not evidence.

⚠ **NEW APK, and it is the one to install** — 0.112.0's tables shipped without this ladder, so a client
built from that one shows a Weapon Mastery that stops at level 74.
## 2026-09-04 — 0.112.0: THE TANK'S 4th TIER — `tank 4th.csv` built whole (`BL-02`, `BL-154`, `BL-155`)

His word, the same day: *"im done with tank 2/3/4 so its ready to build after the npc buffer"*. The
`NOT DONE` banner is gone, the file is **205 authored rows**, and this builds every one of them. It
closes the last unbuilt file in `BL-02`, and it closes `BL-154` (pull) and `BL-155` (silence) — both of
which had been *engine built, rows placeholder* since 0.110.0.

⚠ **NEW APK.** `ProtocolVersion` stays **33** (no wire field moved), but the client builds its Learn tab
and its skill cards LOCALLY from the compiled `ClassSkills` and `SkillCatalog` — and this pass rewrites
the tank's tables from top to bottom. An old build shows the old kit.

🔴 **AND IT WANTS A `game.db` DELETE**, for the same reason 0.110.3 did and one more: Backlash stops being
auto-granted and becomes a six-rung LEARNED ladder whose rung index carries the race (1-3 physical,
4-6 magical), and a saved `id:level` row never reconciles at login.

### What landed

**`tank 4th` earned its line in `Check.Specs`** — the rule from the nuker's file (*a finished file that
no spec walks is invisible*), applied the same day. `dotnet run --project tools/SkillCsvSeed -- --check`
is **green on all fifteen walked files**, tank 4th included.

**Fourteen ladders CONTINUED** past 74, on his two band shapes (every level for the two masteries, every
other level for the rest): Heavy Armor Mastery and Tank Anti-Magic (rungs 21-35), Taunt / Charm /
Shield Shock (20-27), Mass Taunt / Intimidate / Freeze / Stay / both Shield Smashes (16-23), Defensive
Wall (3-10), and the six whisp calls with the whisp skills they carry (9-16). The price ladder is the
healer's and the buffer's — SP up to 79, **gold only** from 80.

**Six things are NEW at the tier:**
- **MAGIC WALL** (76→90) — Defensive Wall's M.Def half (+4,000 → +6,000) with no movement price. Its
  own buff key, so the two walls stack: spend both cooldowns and you are immovable and enormous.
- **TAUNTING WALL** (80) — one cast that mass-taunts everything within 800 for 11,400 threat **and**
  plants a Defensive Wall on you. Both halves are the top of their own ladders ten levels early, which
  is what a 10-minute reuse buys. Not race-split: it is the Elf's and the Demon's only AoE aggro tool.
- **PERFECT WHISP** (80→90) — one whisp with six gears: cleanse, heal (900→1,000), MP restore
  (100→200), armor break (30%/15%), weapon break (15%) and gravity (23%). Weaker than the single
  whisps it stands in for, and it costs the same slot — breadth against depth.
- **BACKLASH**, rebuilt. It was one auto-granted 30% number; his file makes it **three bought rungs at
  77/80/83, race-split**: Physical Backlash (Human + Demon) 10/20/30% against CON debuffs and 5/10/15%
  against SPT, Magical Backlash (Elf) the mirror.
- **WHISP MASTERY rung 2** (83) — the third slot, exactly where the 3rd-tier note said it would be, and
  what makes the Perfect Whisp a choice rather than an ultimatum.
- **SILENCING SHOCK** — the Elf's magical silence, laddered 76→90 beside the Human/Demon's **Numbing
  Shock**. `BL-155`'s engine has served both since 0.110.0; these are the authored rows.

**GRAPPLE is his now** (`BL-154`): 8 rungs, Power **2,100 → 3,500**, 600 reach, 0.5s cast, 15s reuse,
1.2s drag, 1s stun. The 3,000 the placeholder carried sat mid-ladder.

**UNDYING WILL moves tier**, exactly as the healer's Rite of Preservation did: his row prices it at
500kk SP + 100kk gold and **two Physical Stones**, which is a 4th-tier price. Its learn line left the
3rd-class table so a non-ascended level-83 tank cannot buy it at the old placeholder 100k.

### Engine

- **`PassiveEffect.DebuffReflectPhysChance` / `DebuffReflectMagicChance`** — Backlash reflects the two
  schools at different rates, which one blanket number could not carry. `TryReflectDebuff` picks the
  channel off the skill's own `DebuffSchool`; the old school-agnostic field still means "either" and
  feeds both. Both are on the skill card.
- **`SkillDef.SelfBuff`** — a buff a skill also lays on its caster, named by a payload def. It exists
  for one shape the engine could not express: `TargetMode.EnemiesInRadius` **returns** from
  `ExecuteSkill` after its sweep, so Tauting Wall's wall half would never have run. The card recurses
  into the payload so half the skill is not invisible.
- **A whisp can restore MP.** `FireWhispSupport` learned `RestoreMp` (flat, off the whisp's own rung,
  never through the master's stats) and `WhispSupportWanted` gained its condition — and the Perfect
  Whisp's heal needed one too, because `TryWhispAct` fires ONE skill per tick and a heal that says yes
  at full health starves everything behind it.
- **`--check` learned two things.** A `SelfBuff` skill's DURATION column describes the BUFF, not the
  taunt lock (the same special case totems and procs already have). And **a PENALTY ladder is not a
  dip**: Defensive Wall's movement (×0.45 → ×0.20) and Heavy Armor Mastery's evasion (−3 → −6) deepen
  as the rung rises, and a "bigger is better" test reported all ten steps. Both values negative on
  both sides is the test; a real dip still has a positive `was`.

### 🔴 AND THE SMOKE TEST HAD BEEN RED SINCE 2026-09-03 — 12 failures, none of them the tank

Running `tools/SmokeTest` after the pass reported **12 CHECK(S) FAILED**. None was caused by this work,
and two were real:

- **Eleven were one line.** `Healing Whisp requires 4x Skill Stone.` The 4-stone reagent went onto
  every whisp call on 2026-09-03 (*"whisps to take 4 skillstone each summon"*); the whisp checks were
  written on 2026-09-02, and nothing ever stocked the test character. One `DebugGive` fixes it. 🔑 **A
  REAGENT ADDED TO A SKILL OWES EVERY TEST THAT CASTS IT A STOCK LINE** — the same shape as "a new
  `SkillDef` field owes `SkillText` a line", and the reason it went unnoticed for a day is that a red
  harness looks the same whether one thing is wrong or eleven.
- **One check was asserting a rule you had REVERSED.** It demanded that re-calling a live whisp be
  refused (*"is still with you"*) — `BL-109`'s five-second re-summon window, which `BL-130` deleted the
  next day on your ruling (*"charming whisp … resummon on cd not when whisps disapear"*). That string
  no longer exists in the server, so the check could only ever fail. It now asserts what the game
  actually does: past the reuse a re-call is allowed and **refreshes the whisp in place**. 🔑 A ruling
  that reverses a mechanic owes its test an edit, exactly as it owes `SkillText` a line.
- **The twelfth was a THIRD stale assertion, and a sharper one:** *"…and when the fear expires the body
  stops dead"* had been failing because the fear **was still running**. `BL-131` gave `/buff` a duration
  word and a **one-hour default**, and this line calls it without one — so what the harness put on the
  victim was a sixty-minute Terrifying Roar, not the skill's five seconds. Now `buff terrifying roar 5s`.
  🔑 **A command that gains a DEFAULT changes every caller that omits the argument**, and a test is a
  caller like any other.
- 🔴 **…and chasing that turned up a real bug, now fixed: control did not clear its destination when it
  ended.** `TickControlledMovement` writes a random hop up to 200 units away and is the only thing that
  clears it (on arrival), so a fear that expired mid-hop left the destination behind and the ordinary
  stepper walked the victim the rest of the way — a body still "running" after the panic was over. It
  now clears on the EDGE where control ends (`Entity.WasControlDriven`, runtime only, never persisted).
  It bit charm identically, and it is the tank's own **Intimidate** that casts most of the fears in the
  game.

`tools/SmokeTest` is green apart from the documented `You can't leave while in combat` tail, which is
correct behaviour being observed.
### Five corrections to `tank 4th.csv`, and one refusal — ALL YOURS TO REVERSE

Corrected in the file **and** in the code, per the standing "the CSVs and the game move together" rule:

1. 🔴 **Eight `Silencing Shock` rows carried SKILL_ID `tank_shield_stun`** — Shield Shock's id — while
   being a different name, a different TYPE, a different range, cast, reuse, duration, and the only
   rows in that block with a RACE. Two skills cannot share one id: the engine keys cooldowns, saved
   bars and buff families on it. Set to `tank_silence_magical`.
2. **Tauting Wall's AOE cell read 0 and its RANGE cell 800.** Your own Mass Taunt row is rng 0 / aoe
   400, so the radius belongs in AOE — an `enemy/aoe` with a radius of zero taunts nothing at all.
   AOE set to 800.
3. **`robe` in the WEIGHT column on all 15 Heavy Armor Mastery rows** (every DESCR cell says *"with
   heavy"* and the skill is called Heavy Armor Mastery) → `heavy`; and on all 15 Tank Anti-Magic rows,
   where not one DESCR cell mentions armour at all → blank.
4. **Perfect Whisp's SP read `150кк` with a CYRILLIC к**, so every reader scored it 0 → `150kk`.
5. **Heavy Armor Mastery's `mpReg` read `x3.4` on all fifteen rows**, against **x5.1** at your last
   3rd-tier rung — and x3.4 is exactly the number your level-36 row carries. A single value pasted
   fifteen times, and a real regression at the class change (+410% MP regen down to +240%). Set to
   **x5.1** on both sides. One edit reverses it.

**Refused in the code and LEFT in the file** (a ladder you wrote, not a paste, so the cells stand):

6. 🔵 **SHIELD SMASH - POWER's crit-damage ladder restarts** — `15%` at 76 against `35%` at 74, then
   re-treads 19/24/28/33 back to 35. Its twin, Shield Smash - Rate, is FLAT at its own ceiling
   (50%/25%) for all eight 4th-tier rungs, so the symmetric reading is that this one is flat at
   35%/15% too — **and that is what is built**. Say the word and your ladder goes in verbatim.

**Flagged, not touched:** three whisp calls (Binding, Healing, Weapon Breaking) ladder **77, 79 … 91**
— eight rungs from an odd start overshoot the 90 the world is built to. `ExpCurve.MaxLevel` is 100, so
the rung is reachable rather than dead, and `Check.Specs` carries the band 76-**91** for this file so
it stays compared. Compressing it would invent a shape you did not author.

## 2026-09-04 — 0.111.0: `BL-158`…`BL-162`, the NPC buffer LEVELS UP with you

His idea, and its purpose in his own words: *"help single players that dont want to spend time in party
and or lvl up a buffer"*. All five entries written and built the same day.

⚠ **NO NEW APK.** `ProtocolVersion` stays **33** and `BufferBuff`/`BufferInfo` are unchanged; the client
holds no local knowledge of the shelf, so an existing build renders all thirty rows with the right
names, prices and lock levels. Only the build label moved (0.110.3 → 0.111.0).

**`BL-158` — the shelf hands out the rung a same-level buffer would have.** Every `npc_*` blessing was
one def welded to its TOP rung, so a level-40 character wore the level-74 buff. Each is now a LADDER:
one wrapper whose SkillLevel *n* names the family rung for tier *n*, driven by a single table
(`SkillCatalog.NpcBuffTiers`) read straight off his `buffs.csv`. The same table answers all four
questions — unlock level, rung, price, greyed-out button — so they cannot drift apart.
- 🔑 **The wrappers were KEPT, not deleted.** The player still receives `npc_might`, so `SourceSkillId`
  is unchanged and [Save], both role presets and every saved row keep working with **no migration**.
- 🔑 `cast_atk_phys` is not an engine id; the real ladder is `buff_<family>_<rank>`, and the wrapper
  already delivered the real rung. The engine needed nothing new — `ApplyBuff` already took level,
  duration and source overrides, and `SkillDef.ChildBuffsAt(level)` already gave a wrapper per-rung
  children. **All 30 of his authored values already existed**, so the CSV described the shipped ladders.
- The price is the RUNG's price now (5k/10k/15k as it climbs), so the paid tier is *cheaper* below 52
  than the old flat 15,000. `BuffCostPerLevel` and `BufferBuffNominalLevel` are deleted.

**`BL-159` — the level-75 ceiling is gone**, reversing half of `BL-150` on his ruling (*"NPC buffer no
top cap"*). What limits the NPC now is the shelf itself, not a wall. The Blessing Box stays (*"leave
them be"*), its role changed to the weaker option that saves the walk back to town.

**`BL-160` — eight new single harmonies, 50k each.** Each lifts ONE effect out of a Warchanter harmony
at the exact level she gains it (verified 8/8 against `buffer 3rd.csv`). Own BuffKeys, so all eight
stack; the four class harmonies now name them in `Replaces`, which is his rule — *"his acts as a group
one so replaces them"*. Single-target, unlike the class harmony they come from.

**`BL-161` — the three Marks at 78 / 300,000.** The Lightbringer's own 4th-class skills at rung 1 (she
learns rung 2 at 83, which the NPC never sells). **No skill stones** — the reagent gate lives in
`HandleCastSkill` and the NPC grants through `ApplyBuff`, so it is free of them by construction. They
share one BuffKey and so do not stack, which is why one Mark, not three, is what an endgame set costs.
⚠ The buffer path now forces the 1-hour duration: a Mark is a 5-minute class skill, and granted as-is
the NPC would have sold five minutes for 300,000.

**`BL-162` — Swift joined the Mage preset**, making both role presets five and the free eight split
3 fighter / 3 mage / 2 shared. The `MageBuffSet` invariant comment moved with it.

**Guards and measurements added, because a shelf this size rots silently:**
- Startup now asserts every shelf id has a tier row, that tiers never exceed the def's levels, and that
  a ladder is monotonic. ⚠ **It caught a real case on its first run** — written as `!=` it rejected the
  Marks, which legitimately sell a one-tier PREFIX of a two-level def. Now `>`.
- `tools/BalanceMatrix --npcshelf` prints the shelf at every level that changes it, **read off the live
  catalog rather than his CSV** — a dump that restates an authored number can never contradict you.
- Three places in BalanceMatrix that assumed the old shape were corrected: `ApplyNpcBuffs` hardcoded
  level 1 (which would have measured every character, including a level 80, wearing the WEAKEST rung of
  everything under a "buffed" heading); `NpcRank` read `def.ChildBuffs`, now the *lowest* rung; and the
  admin-coverage census now excludes the harmonies and Marks, which are designed to be covered by
  nothing and would have printed as eleven false "holes in the CLASS kit".

`dotnet build` clean, server boots, `SkillCsvSeed --check` reports no discrepancies.

⚠ **Still owed and not done here:** `buffs.csv` is in no `Check.Specs`, so none of this is CSV-verified.
Its shape (a catalogue with no `LEARN @ LVL`) does not fit the existing checker.

## 2026-09-04 — the NPC-buffer design pass, and the Frenzy ladder cut to two rungs

**No version bump: the wire protocol did not move and nothing a player can obtain changed**, so no new
APK is owed for this entry.

### The Frenzy ladder is two rungs (his ruling)

*"frenzy have only 2 rungs .. If u want a ladder for scribe make 35 frenzy L1 and 52 frenzy L6 and do
ladder between ... But I see no point in scribe having buffs that don't exist ... He make scrolls at lvl
that buffers have or sooner ... But the crafting is not yet finished so just remove the weird frenzy
ladder and fix it. The box gives max so.."*

Found while verifying his `buffs.csv` rung values for `BL-158`. `FamFrenzy` carried **seven** rungs, of
which only the first two were ever his (`cleric 2nd.csv` @35 = rung 1, `healer 3rd.csv` @52 = rung 2).
Rungs 3-7 were ours and were incoherent: **rung 3 gave +6% offence for −22% Max HP/MP — strictly worse
than rung 2's +8% for −10% — yet outranked it**, so `ApplyBuff`'s replace-on-rank-≥-rank would have let
the weaker buff evict the stronger. **Rung 6 was byte-for-byte identical to rung 2.** The three Frenzy
scrolls sat on rungs 2/4/6, which made the *Grand* scroll worse than the *Superior* one and the
*Supreme* equal to it.

🔑 **The cut was safe because none of it was reachable.** No class table grants `holy_frenzy` above
level 2; only ONE Frenzy scroll ITEM exists (`scroll_frenzy_m`); War Frenzy already handed out rung 2.

- `Skills.BuffLadders.cs` — rungs 3-7 deleted. Two rungs remain, both his, verbatim.
- `Skills.Healer.cs` — `holy_frenzy` levels 3-6 deleted (they pointed at the dead rungs and existed
  only to be quoted wrongly by the skill card).
- `Skills.Buffer.cs` — the NPC's Frenzy now names rung **2** instead of rung 6. **Identical numbers.**
- The scrolls are **two, not three**, mirroring the buffer's two rungs on his rule that a scroll may
  only carry a buff that exists. `ScrFrenzyM` keeps its id and its item ("Scroll of Frenzy") and still
  hands out the family's top rung, at the numbers it always gave. `ScrFrenzyE` becomes the Lesser
  scroll at rung 1; `ScrFrenzyL` is retired, its const kept because ids are append-only.
  ⚠ Both scroll DESCRIPTIONS were already stale — "Superior" claimed −26%/+6% while pointing at rung 2.
- `docs/data/BuffConsumables.md` — the Scroll of Frenzy's rung column corrected 6 → 2.

`dotnet build` clean; `SkillCsvSeed --check` reports **no discrepancies**.

### `BL-158`…`BL-162` — the NPC buffer levels up with you (DESIGNED, not built)

His idea, to *"help single players that dont want to spend time in party and or lvl up a buffer"*, is
written up in [Backlog.md](Backlog.md) against his new `docs/data/classes_skills_csv/buffs.csv`: the
shelf hands out the rung a same-level buffer would have (`BL-158`), the level-75 ceiling goes
(`BL-159`, reversing half of `BL-150`), eight new NPC single harmonies at 50k (`BL-160`), the three
Marks at 78/300k (`BL-161`), and Swift joins the Mage preset (`BL-162`). All five are ready to build.

Three findings that shrank the work, recorded in `BL-158` because they will be needed again:
`npc_might` is a **one-child wrapper** and the real family rung already lands (`cast_atk_phys` is not an
engine id at all); `ApplyBuff` already takes level + duration + source overrides and
`def.ChildBuffsAt(level)` lets one wrapper carry a different child per rung; and **all 30 of his
authored rung values already exist** in `Skills.BuffLadders.cs`, so the CSV describes the shipped
ladders rather than proposing new ones.

## 2026-09-04 — 0.110.3: his tank Shield Mastery / MP delta, and the bow-resist ladder

His ask: *"fix the tank 2nd and 3rd mp and sheield mastery rungs .. and create a bow resistance"*.
The sheets had been committed the same morning (`dc0020e`) as a restore point with the code
deliberately left behind; `--check` was reporting **9 discrepancies against `tank 3rd.csv`**, every
one of them this delta. It reports **none** now.

**Bow resistance did not need creating — it needed AUTHORS.** The whole path already existed and has
since 2026-08-21: `PassiveEffect.BowResist` → `Entity.BowResist` (summed from passives, buffs and
mastery profiles, clamped 0-90%) → `ResolvePhysicalCritAndBlock`/`ResolvePhysicalDouble`, which trim
`baseDamage` by it whenever the attacker's `WeaponType` is `Bow` — hit, crit and block alike, before
any of the other resolution. `SkillText` prints it, the target window prints it, and
`tools/SkillCsvSeed/Descr.cs` has read *"bow resistance"* / *"bow resist"* / *"arrow defence"* out of
the free-text DESCR column all along. What was missing was that only **two** rungs in the game carried
any, and his three files now author **seven**. So this entry adds no field and no formula; every
number below is data catching up with him.

**Shield Mastery is SEVEN rungs, was four.** The 3rd-class band grew 40/52 → 40/43/46/49/52, and the
shape of that growth is the thing to notice: **the three new rungs differ from the level-40 one in
NOTHING BUT BOW RESISTANCE.**

| rung | level | shield P.Def | block rate | P.Def | bow resist | SP |
|---|---|---|---|---|---|---|
| 1 | 20 (tank) / 40 (Warchanter) | 150% | +50% | — | — | 3.2k / 36k |
| 2 | 28 / 60 | 200% | +70% | — | **16%** ← new | 3.2k / 120k |
| 3 | 40 / 70 | 250% | +85% | +10% | 16% | 28k / 390k |
| 4 | 43 | 250% | +85% | +10% | **24%** | 35k |
| 5 | 46 | 250% | +85% | +10% | **32%** | 40k |
| 6 | 49 | 250% | +85% | +10% | **40%** | 50k |
| 7 | 52 | 300% | +100% | +10% | 40% (was 24%) | 74k |

(The shield-P.Def column is his IG percentage ×5, the standing 2026-08-12 pairing with the ×5 cut to
every shield's flat defence in `Items.cs`. `--check` still prints those five as ⚪ RULED.)

So rungs 4-6 are a **bow-resistance ladder wearing a Shield Mastery name** — nine SP-thousand a step
to climb 16% → 40% against archers, and the shield's own numbers do not move again until 52. That
flatness is his and it is deliberate; do not "fill in" the middle columns to make the ladder look
normal. It also means a Bulwark reaches **40% bow reduction at 49**, three levels earlier than the top
rung, which is the first time a tank has had a real answer to a ranged attacker mid-band.

**And bow resistance moved DOWN a rung.** `tank 2nd.csv`'s level-28 row and `buffer 3rd.csv`'s
level-60 row both gained *"bow resistance 16%"* — the same rung 2 in both files, so it is one change,
not two. The 2026-08-21 note in `Skills.Fighter.cs` that it "starts at rung 3" is now history. A tank
gets it twelve levels earlier than before, and the **Human Warchanter** — the only buffer who learns
this skill, and only in heavy armour — gets it at 60 instead of 70.

**The MP ladder his three shield actives share is a ladder again.** `BulwarkSmashMp` opened
`62, 76, 76, 76, 83, …` — three identical rungs, the one place in his whole tank file where a column
stood still for three levels. He filled the step in himself: **65 at 43 and 71 at 46**. One array, so
**Shield Shock**, **Shield Smash — Rate** and **Shield Smash — Power** all moved together (that is
what `--check` was reporting as six separate defects). Nothing else on those three skills changed.

**One renumbering worth knowing about.** The retired Vanguard's legacy level-52 line — kept since
`BL-97` so a character who bought that discipline still holds the rung he paid for — asked for
`SkillLevel: 4`. Rung 4 is now the level-43 payload, so the line says `SkillLevel: 7`. The level and
the price did not change; only the index into a longer ladder did.

⚠ **NEW APK.** The client builds its Learn tab **locally** from the compiled `ClassSkills`, so an old
client shows a tank the two old rungs and none of 43/46/49. `GameVersion` is bumped to **0.110.3** and
the login handshake refuses a mismatch, which is exactly the enforcement wanted here — but it means
server and APK deploy **together**. `ProtocolVersion` is untouched: no DTO, hub method or push name
changed.

⚠ **A SAVED TANK AT 52+ IS AFFECTED, and the fix is the `game.db` delete you already owe.** Learned
skills persist as `id:level` and nothing reconciles them against the class tables at login, so a
character holding `tank_shield_mastery:4` from before today now resolves to the **level-43** payload —
shield P.Def 250% instead of 300%, block +85% instead of +100%. It cannot be migrated safely in code:
after today a level-4 rung is a legitimate thing for a level-43 tank to hold, and there is no schema
version to tell the two apart. The delete banner at the top of `docs/testing/Open-Checklist.md` now
names this too. Your admin account is a Warchanter, whose rungs 1-3 did not move — only a **tank at
52 or above** is exposed.

## 2026-09-04 — the taunt card still promised the top of the table

Him: *"look at taunt description and it say it puts me on the top ... but it shouldnt put me to the
top it just add aggro value .. the csvs no longer say put me to the top .. and we should have removed
that"*.

He is right, and the engine has been right since `BL-123` (2026-09-02, 0.107.x) — `ApplyTaunt`,
`FireWhispOffensive` and the charm path all do a **plain add** to the caster's row and nothing else.
What never moved was the sentence the skill card prints. `SkillText.Mechanics` had one line for
threat, written back at `BL-71`:

> `Threat 4,500, on top of jumping you to the top of the table`

So for two days every Taunt, Lure, Mass Taunt, Charm and Taunting Whisp described a mechanic the
code no longer has. That is the worst shape a stale string can take: it does not merely omit, it
teaches the player the wrong model of his own class — a tank reading that card would reasonably
conclude he need not spam, which is the exact opposite of the ruling (*"the idea is tank to spam
taunt/charm for mob to keep it agrro on him"*).

**Now two lines, and the second is conditional.** The card reads `Adds 4,500 threat`, and — only for
a real `SkillEffect.Taunt` — `Locks it onto you for 1.5s`, which is the taunt lock the card had never
mentioned at all despite it being the skill's actual guarantee. A **charm does not print the lock**:
it pays points through `AddThreat` and deliberately does not force a target change (*"dont change
target like taunt"*), so claiming a lock there would be the same class of lie in the other direction.
The whisp path's doc comment, which still called the taunting whisp's purpose "put the MASTER at the
top of the table", was corrected with it.

⚠ **This needs a NEW APK.** `SkillText` lives in `Game.Shared`, and the client builds the card
**locally** from its bundled copy — the server never sends this text. Nothing else changed: no
`SkillDef`, no `ClassSkill`, no CSV row (his files say "leaves you N aggro ahead", which is the plain
add, correctly authored all along), and `--check` is clean. No protocol change, so no version bump.

## 2026-09-04 — 0.110.2: the drag was announcing itself as a teleport ten times a second

Him, on Grapple: *"when successful it drags the monster but it's like lagging, not like a continuous
clean drag — it's not really a problem because the mob is near me after it ends, it seems real time ..
but if you find the problem without breaking the working one — I remember even IG had not a perfect
drag animation"*.

Found, and it is one line. **`EntityDto.Warp` is not a "position changed" flag — it is an instruction
to the client to `SnapTo` and RETURN, skipping interpolation entirely** (`EntityView.SetTarget`, first
branch). `TickPull` moves the body through `PlaceEntity`, and `PlaceEntity` bumps that counter on
every call, by design: it is the one seam every non-walk reposition passes through, so blink,
knockback, the gatekeeper, respawn and the admin jump all declare themselves there for free
(`BL-102` — a 200-unit Phase Shift is smaller than both distance thresholds the client used to infer a
teleport from, so it moved the player on the server and nowhere on screen).

A pull calls that seam **every tick**. So the counter moved every tick and the client hard-snapped the
mob **ten times a second with no interpolation between any two of them** — a 10 Hz staircase that
nevertheless lands in exactly the right place, which is why he read it as real-time but lagging.

- `GameLoopService.PlaceEntity` — gained `announce` (default **true**, so every existing caller is
  byte-for-byte unchanged). `TickPull` passes `false`.
- 🔑 **THE LINE IS CONTINUITY, NOT "DID SOMETHING ELSE MOVE IT".** A blink, a knockback, a gatekeeper
  and a respawn are DISCONTINUOUS — there is no path between the two points and interpolating across
  it is a lie. A drag is a body crossing the ground at a defined speed over a known number of ticks: it
  is movement, and the snapshot interpolator is the thing that draws it correctly.
- ⚠ The step stays far inside the client's own 5-unit teleport guard: **0.43 Unity units** per tick for
  Grapple's 600 range, 0.68 even for a 900-range pull. It interpolates like a walking mob.
- 🟢 **The animation comes along for free.** `EntityView.DriveModel` reads facing and animator speed
  from the position it ALREADY DREW — so under the snaps it saw one huge frame delta and then zeros,
  and now it sees continuous motion. The dragged body will turn to face the pull and play its run.

⚠ **SERVER-SIDE ONLY — this one needs no new APK.** (The camera fix and Grapple's card from 0.110.1
still do.) No wire change; protocol stays 33.

🟡 **One residual — NOT BUILT AND NOT TESTED, and that is his instruction:** *"mark the one clamp /
EntityView.Update as untested and I'll see it in game first then decide"*. The interpolator's segment
duration is the measured gap between the last two updates, and the server sends only what changed —
so a mob that stood still for ten seconds and is then grappled has a **ten-second first segment**, and
the drag's opening ~100ms is drawn almost frozen before the second sample corrects it. It self-corrects
after one sample, so it is a hitch at the START of a drag, not a stutter through it. One clamp on that
span (~0.2s) would fix it, and every mob's first step out of an idle with it — but it touches
`EntityView.Update`, which has been rewritten three times to kill the rubber-band, and it is not what
he reported. **It waits on his eyes.** The test: grapple something that has been standing STILL. Full
diagnosis and what to look for is in `docs/testing/Open-Checklist.md`.

## 2026-09-04 — 0.110.1: the whisp that never stopped resummoning, Grapple off the taunt rung, and the ortho camera losing the world

Three of his finds against 0.110.0, and two of them are the same shape: **a payload that lives in a
FIELD reaching a test that only knows about the old container.**

---

### 1. A whisp was re-summoned every 30 seconds instead of every 20 minutes

Him: *"whisps are still auto used every cooldown (30s); they should be used every 20 min or when they
are not present ... if I have 1 slot and I put 2 whisps on auto they will be used on cd yes, because
one will remove the other ... but when I have space for both they will not be used until I make space
(worn off, or I die and respawn with them gone)"*.

🔑 **A summon is authored `Category.Buff` on purpose** — it is cast at yourself and leaves something
that expires, and the category is what keeps it out of the offensive target checks. So it reaches the
**Buff arm** of the auto chain, which asks `AutoBuffUpToDate`, which walks `Entity.Buffs`. **A whisp
rides in `Entity.Whisps`.** The walk found nothing, every summon read as MISSING, and the chain
re-called it the instant its 30s reuse was up: six re-summons a minute at **4 skill stones each**,
against a whisp that lasts twenty minutes and was already floating beside him.

- `GameLoopService.AutoBuffUpToDate` — a `SummonsWhisp` skill now asks the **whisp stack**: present
  under this summon's id at this rung or better = up to date. The rung test is `BL-112`'s, kept, so
  learning a stronger rung re-calls it once and then leaves it alone.
- ⚠ **No renewal window, unlike a blessing.** `BL-130` already settled what a whisp is — *"a summon you
  place, not a blessing that ticks down"* — so this asks the flat question he asked for: is it there?
  A summon therefore fires **on expiry (his 20 minutes) or on absence** (death and respawn, a class
  change, evicted by another whisp) and at no other time. The ~1s gap while the replacement casts is
  the honest cost of that rule.
- 🔑 **His one-slot case needed no code — it is this rule working, and he said so first.** Two summons
  against one slot each evict the other, so each is genuinely absent when its turn comes and both fire
  on cooldown. Whisp Mastery's second slot is what stops it.
- `SendAutoHuntStatus` — a whisp is now **priced on its duration, not its reuse**. It was quoting
  ~1.6 MP/s for something that costs 50 MP every twenty minutes: a HUD that contradicts the rule it is
  reporting on. 🔵 The same gap exists for every long **buff** on the auto bar (renewed on expiry,
  priced on cooldown) and is deliberately left alone — that number is his to move.

### 2. Grapple was a threat skill, so it could never be auto-cast

Him: *"does grapple work in auto or is it a taunt skill .. if it's a taunt skill I want it to not be,
and be a normal dmg skill with 3k power (my standard dmg skill is 4k so later it will grow as well
when authoring)"*.

It shipped yesterday as `TauntPower: 3000` with **no damage at all** — the honest reading of his
*"lower power than the actual taunt skill but still higher than most dmg ones"* at the time. He was
describing where the number sits; he has now said which **column** it belongs in. And the consequence
he found before I did: `BL-83` sends every threat skill to the never-auto bucket (*"get a tank, leave
it auto, he taunts — almost impossible to kill"*), and `TauntPower > 0` is the **first** test in
`ClassifyAuto`. A tank's new signature move could not appear in a rotation at all.

- `Skills.Bulwark4th.cs` — **the 3000 MOVED, it did not double.** `SkillEffect.PhysicalDamage | Stun`,
  `Power: 3000`, `TauntPower` gone. It builds threat by the only route a damage skill ever does — the
  damage it deals through `AddThreat` — and lands in the **Attack** rung of the chain. The drag, the
  1s stun tail and the single CON contest are untouched, as is its `DebuffSchool.Physical` (which is
  what already paces it on attack speed, per `BL-132`).
- `tank 4th.csv` — the Grapple row moved with it, same commit. Still a placeholder ladder of one rung.

### 3. 🔴 The ortho camera lost the entire world above zoom 9

Him: *"ortho zoom-out is still broken over 9 zoom"* — the third grey rectangle, and the first one that
is **arithmetic rather than geometry**. His threshold is the whole diagnosis.

🔑 **`Mathf.Tan(90f * Mathf.Deg2Rad)` is NEGATIVE.** Unity's `Deg2Rad` is built from the float `PI`,
which rounds UP, so `90 * Deg2Rad` lands a hair past π/2 and the tangent comes back around **−2·10⁷**
instead of +∞. The near-clip correction added in 0.102.x divides by `Mathf.Max(0.01f, tan)` — which
with a negative tangent picks **0.01**, not the huge number — so the rig was pushed back by
`OrthoSize × 100`. At the default camera height 38 that is **938 units at ortho zoom 9 and 1038 at
zoom 10, past the camera's 1000 far clip plane**. The entire world falls out of the ortho slab and the
screen becomes nothing but the clear colour — which since 0.102.11 is the ground's own grey. His
"over 9" is exactly where the arithmetic says it starts.

🔑 **Pitch 90 is the only angle that trips it, and it is the shipped default.** At 89° the tangent is a
healthy 57.3 and the correction is under one unit — which is why it never showed while he was judging
the 2.5D models at 45-55°, and why it read as "the same bug back again".

- `CameraRig.RigDistance` — the correction is now written as `cos/sin` (cot θ, a clean −4·10⁻⁸ at 90°
  instead of a sign flip) and floored at **zero**, not at a divisor, plus a hard ceiling on the rig
  distance. Nothing may push the camera far enough to lose the world, because losing the world looks
  exactly like a rendering bug and reads as one.
- ⚠ **The world-edge fix from 0.102.11 stays.** It was a real second bug (the view is genuinely wider
  than the 240-unit map at the top of the slider); this third one was hiding underneath it.

---

⚠ **NEW APK.** The camera fix and Grapple's skill card are both client-side. No wire change, so
**protocol stays 33**.

## 2026-09-03 — the boot console says what went wrong, not what went right

Owner: *"remove the server console info flood in the start with every city/zone creation ...it's a
system that works now and only flood the console"*. Startup printed a line **per region** plus one per
**gate** — ~89 log lines (twice that on screen, the default formatter puts the category on its own
line) describing a world that has been correct for versions.

- `GameLoopService.SpawnNpcs` — the per-region/per-gate loop is now **one summary line**
  (`World: 28 field(s), 9 town(s), 82 spawner(s), 52 gate(s)`). 🔑 The loop was not decoration: its
  comment says a mis-authored polygon contains **no spawners** and fails *silently*. So that check is
  kept and inverted — a `LogWarning` **naming** any field region with zero spawners, silent when the
  world is fine. Printing 37 healthy regions to catch one broken one is the wrong way round.
- `Program.cs` — dropped `URLS:{urls}` and `App Build!` (raw `Console.WriteLine` debug leftovers), and
  the LAN-address printout: it walked every NIC that was **up**, so it advertised two virtual-adapter
  addresses a phone can never reach next to the real one. `Start!` is kept, at his instruction.

Boot is now **16 lines**, of which ours are three: the version, the NPC count, the world summary.
No behaviour changed; nothing on the wire changed, so **no new APK and no protocol bump**.

## 2026-09-03 — 0.110.0: the pull, the two silences, and CON/SPT shortening what they failed to stop

Three systems he specced in one message and ruled over the two that followed (`BL-154`, `BL-155`,
`BL-156`). Two of them are engine work with placeholder rows on a file he has not written yet — his own
instruction, *"so when I author it to remember to fix ranges/duration etc"* — and one is a global rule
that needed no CSV at all.

### 1. `BL-156` — CON and SPT now SHORTEN a debuff as well as resisting it

*"if we can make con and spt to decrease duration of coresponding debuffs -> it saves with a % and if
it lands on a high stat it stays less (investing have benifits)"*, then the numbers: *"only 20~30%
decrease no more. Like a 50 con/spt is 30% decrease and 30(the base what was) x1 so 30~50 == x1~0.7"*,
and finally *"it cuts only 1~0.7 not 1.3~0.7 so never increases duration .. Only decrease"*.

```
factor = clamp( 1 - 0.3 * (defenderStat - 30) / 20 ,  0.70 , 1.00 )
```

CON for a physical debuff, SPT for a magical one — the same stat that just lost the landing contest.

🔑 **It reads the RAW STAT, not the land chance.** Scaling by the chance would fold in `CcResist`, the
per-school blessings and the skill's own `DebuffLandMod` — three channels that already paid for
themselves on the roll — and turn one defence into three dips.

🔑 **It lives in `ApplyBuff`, not at the call sites**, because every road to a landed debuff comes
through there: the contested branch, the fizzle branch (Armor Break, Mana Strain), a reflected debuff,
a whisp's cast, a boss's own skills. Putting it on the contested branch alone would have left the
fizzle-path curses full length for no stated reason. The gate is `DebuffSchool != None` — the field
that already names which stat defends the skill, so "corresponding debuffs" needed no second
classification. It composes with **[Double]** by multiplication (2 × 0.7).

🔑 **His 30 and 50 land almost exactly on the real spread — checked, not assumed.** Base CON runs 25-47
and base SPT 25-41, armour sets move CON by ±3, and **nothing in the game buffs CON or SPT**, so a
character lives inside the window for life:

| | CON → physical | SPT → magical |
|---|---|---|
| Demon fighter | 47 → **×0.75** | 27 → ×1.00 |
| Human fighter | 43 → ×0.81 | 26 → ×1.00 |
| Elf fighter | 39 → ×0.87 | 25 → ×1.00 |
| Demon mage | 31 → ×0.99 | 41 → **×0.84** |
| Human mage | 29 → ×1.00 | 37 → ×0.90 |
| Elf mage | 25 → ×1.00 | 36 → ×0.91 |

⚠ **AND IT CUTS MOBS TOO** — *"If con/spt does anything for mobs it's not just a decorative stat ok
let's shorten it as well"*. Mob CON/SPT are flat by role, and they sit high: melee CON 45 → ×0.78, a
tank `MobMod` 50 → ×0.70, and a **mage mob's SPT 58 is already past the floor → ×0.70**. **Player CC
now runs 12-30% short of its authored duration against everything**, on top of the land roll it already
loses. That is a farming change, made with eyes open; if holds stop being worth casting the lever is
`MobCcSpt`, not this curve.

⚠ A DoT's internal stack COUNTER now runs for the duration the damage buff actually got, not the
authored one — otherwise a shortened bleed would leave a stale stack alive behind it.

### 2. `BL-154` — PULL: a body dragged across the ground, not teleported

*"tanks will have pull -> target or aoe around.. con saves and if succeed pulls the target to the
caster, hope its not instant but 300 range per second .. to look like a pull not phase shift"*.

The contest already existed — a pull is an ordinary contested physical debuff (ATK vs CON) whose
payload happens to be movement, and his expected *"0.4~0.5"* is what `DebuffLandChance` gives at
parity. What did **not** exist was gradual forced movement: the only thing in the game that moved a
body against its will was `DoKnockback`, a single `PlaceEntity` — an instant teleport, precisely the
phase shift he did not want.

🔑 **THE DRAG IS TIMED, NOT PACED, and his two numbers are two different rules.** *"I like the whole
pull to be a 1s~1.5s pull"* wins over the 300/s, because a fixed SPEED makes the lockdown scale with
whatever range the skill authors (a 900 pull would take three seconds) while a fixed DURATION does not.
`SkillDef.PullSeconds` is the whole journey from any distance; the speed is derived, floored at
`GameConstants.PullMinSpeed` = his 300/s so a short pull arrives early instead of crawling. **Range now
buys reach and never buys lockdown.**

🔑 **ONE CONTEST, TWO PAYLOADS, IN SEQUENCE.** His simplification — *"also one con contest for pull
+stun"* — means a landed pull always stuns, so the chain fires at the full ~45-50%. The stun is held on
the victim and applied by `FinishPull` **on arrival**, because applying it on landing would overlap the
two windows and the chain would be `max(drag, stun)` instead of his *"1s~1.5s pull. And 1~2s stun"*.

🔑 **THE INTERRUPT HE WANTED IS FREE.** *"The pull idea is shorten the distance + enemy interrupt rather
than control"* — a landed stun sets `IsActionLocked` and `UpdateAction` cancels the cast with
`startCooldown: false`, the existing enemy-interrupt contract (the victim loses the 20% initial MP and
may retry at once). ✅ **And the DRAG interrupts too** — it follows from *"Yes like charmed while
dragging - no act"* (being dragged is an action lock, and charm and fear have always cancelled a cast),
it corrected what `BL-154` claimed when it was written, and he **ruled it correct** the same day:
*"I like the actual pull interrupt - it's the logical way ... U don't see a mage being dragged and
still casts."* So an AoE pull, which carries no stun, still interrupts what it drags.

Also ruled and built: the drag stops at **melee range**; **bosses are immune** (a boss dragged around
the field is the same perma-kite the knockback rule already refuses); **players are not**
(*"is a tanks chance to close the gap with everyone else"*); threat sits **above a damage skill and
below the real taunt** (3000, against the Taunt ladder's 4,500 → 12,000).

⚠ **The area sweep learned a CAP.** `EnemiesInRadius` was uncapped; `SkillDef.MaxTargets`, when a skill
authors one, now takes the **nearest** N. His rule for the AoE pull is *"2~5 enemies"*, and an uncapped
one is either a wipe opener or the best farm skill in the game. Every skill that shipped before this
authors 0, so nothing already in the game narrows.

🔵 **The two AoE pull shapes are NOT authored** — he named one pull, not three. The engine serves both
(`AreaAtTarget` picks the centre, `MaxTargets` is the cap, `DeliverSimpleHit` grew the pull arm), so
they need rows and nothing else.

### 3. `BL-155` — the DISARM is declined; SILENCE replaces it

*"If we leave the weapon bonuses it's not a disarm. Let's don't do a disarm .. But I like your silence
idea"*. He was right, and the old entry is why: the one question it hung on — does a disarmed character
also fail the skills that REQUIRE a weapon — had only two answers, and both were bad.

What ships instead: **physical silence** (physical skills fail, the **basic attack still works** —
his *"(only basic attack)"*), **magical silence**, and **both at once = a full silence**. Two
independent debuffs, so the "full" version needs no third skill; a single skill may set both, and the
boss's does.

🔑 **THE PHYSICAL/MAGICAL AXIS WAS ALREADY BUILT AND WAS NOT RE-INVENTED.** `SkillMath
.PacedByAttackSpeed` — `Category.Physical` **or** `DebuffSchool.Physical` **or** `PhysicalCast` — is
the three-marker test from the `BL-133` cast-speed pass, and its own note said nothing later may grow a
second version of it. Silence is that "anything later", so the method was **renamed `IsPhysical`** (the
name of the question it actually answers) with `PacedByAttackSpeed` kept as a one-line alias at the
speed call sites. A skill can never be physical for cast speed and magical for silence.

⚠ `SkillEffect` has had **zero bits left since `1L << 62`**, so both halves ride as FIELDS — and the
`BL-110` charm lesson applies in full: *when a payload is a field, every flag test on its path has to
learn about it.* `IsContestedDebuff`, `BossShrugsOff`, the harmful/beneficial test in `HandleUseSkill`,
`BuffInstance.IsDebuff` and `SkillText` were all taught. Bosses are immune to silence, on the same
reasoning as the pull: a boss that can be silenced has no mechanics left.

### 4. Three rows on a file he has not written

His instruction: *"put pull for the three tanks 4th, one m.silence skill for elf tank 4th and one
p.silence for human/demon tank 4th in the csv so when I author it to remember to fix ranges/duration
etc"*. So `tank 4th.csv` gains three rows and `Skills.Bulwark4th.cs` the three defs — **Grapple**
(all three races), **Numbing Strike** (Human + Demon) and **Silencing Ward** (Elf), one rung each at 76.

⚠ **Every number behind them is a placeholder except the ones he ruled**, and both sides say so. The
race split continues the one `tank 3rd.csv` already draws (`BL-133`): the Human and Demon tank hold
with physical tools, the Elf with magical ones.

### 5. The dungeon bosses get a full silence

*"U can add dungeon bosses a full silence aoe skill for 15s duration and 45s cd (mp cost u deside)"* —
150 ticks and 450 ticks exactly. **Word of Unmaking**, 500 radius (wider than the slam's 250, so it
reaches the healer standing behind and not only the melee ring), SPT-defended, **MP 0** like every other
boss skill because a rotation must never stall on mana. It goes to the three bosses at the end of a
dungeon corridor: `grave_lich` (44), `dread_knight` (65), `disciple_of_the_dawn` (90).

⚠ Each profile lists the **slam as well**. A template with no profile falls back to the generic
Devastating Slam; the moment it has one, the profile is the whole rotation — leaving the slam out would
have traded each boss's only attack for a silence.

🔵 **Watch it in play.** 15s on a 45s reuse is 33% uptime with no heals, which is brutal by design and
the first number to move if a boss becomes unkillable.

### Verification

`dotnet build` green · server **boots** clean with no startup-guard complaint ·
`SkillCsvSeed --check` reports **no discrepancies** across all twelve walked files.

⚠ **NEW APK.** The class-skill TABLE changed (three learn rows on the Bulwark's 4th tier) and the
client builds its Learn tab locally from the compiled `ClassSkills`.


## 2026-09-03 — 0.109.4: whole-number set bonuses, STR is ATK, a sigil admits its cooldown, and the potion faucets are banded

His report, on the Epic copy of the heavy A set: *"Heavy A grade epic test have Str +1.4 stat .. when
lowering stats of rarity make them integers ... 1.4 is 1 .... The other % based stat can be left as is
.. Now just STR should become ATK as ours stat and 2 should not become 1.4 but 1"*.

Two faults on one line of one item card, a third he found on the sigil card in the same breath, and a
fourth that is not a fault at all — two drop-table rulings he gave when he asked whether an earlier one
had ever been written down. The first three all sit in generated or derived text that nobody authors by
hand, which is why none had ever been read — they describe the game wrongly rather than behaving
wrongly. **Only §1 and §4 change what the game does.**

### 1. `StatMods.Scaled` never rounded, so a primary stat came back in tenths

An armour set is authored ONCE, at Mythic, and the Epic/Legendary copies are that bundle × 0.70 /
× 0.85 (`ArmorSetCatalog.QualityVariants`). Ironforge A authors `MaxHp: 455, Str: 2, Con: 2, Agi: -2`,
so its Epic copy was **STR +1.4, CON +1.4, AGI −1.4, Max HP +318.5** — and a primary stat is a COUNT of
points. There is no such thing as four tenths of one.

Every countable flat is now rounded inside `Scaled`, at the one place a derived quality is built:
pools, defences, attack, accuracy/evasion, move speed, the six primary stats, ATK and flat crit damage.
The item PIECES already did this (`ScaledDropItems.S()` has always returned an int) and
`ClassFlatBonus.Scaled` has always rounded its own fields — it was only the `StatMods` half of a set
bonus that leaked fractions, so the two halves of one set disagreed.

🔑 **What is deliberately NOT rounded is the bigger half of the record.** Every `*Pct`, the
multiplier-shaped `CritRate`/`CritDamage`/`MagicCritRate`, the resist fractions, vamp, reflect,
`MagicResist`, `MagicCritDamage`, `MpCostPct` and `CritRateFlat` all carry their percentage **as a
fraction** (0.02 = 2%). Rounding one of those would not shave it — it would **delete** it. Regen is a
per-tick rate and legitimately fractional, so it stays too. This is his own rule: *"The other % based
stat can be left as is"*.

Measured, Ironforge A: Epic now reads `Max HP +318 · CC resist +28% · ATK +1 · AGI −1 · CON +1`,
Legendary `Max HP +387 · ATK +2 · AGI −2 · CON +2`, Mythic `Max HP +455 · ATK +2`. ⚠ On a 2-point
stat, 85% rounds back to 2 — Legendary and Mythic tie. That is inherent in whole numbers at this
scale, and it is what he asked for.

### 2. The card printed "STR" and "INT", two stats this game does not have

The five primary stats are **CON / ATK / WIT / AGI / SPT** (`StatCalculator.BaseStats`). STR and INT
are not among them: they are the two names the gear CSVs write **ATK** with, fighter and mage, and
`StatMods.Str`, `.Int` and `.Atk` all fold into the same slot in `RecomputeDerived`'s primary-stat
pre-pass. `SkillText.Mods` printed them under their CSV names, inventing two stats the character sheet
cannot show — so a set's "STR +2" could not be found on the character it was raising. They are summed
into one **ATK** line now (a set never authors more than one of them).

That also closed the file's own standing warning: `Atk`, `MagicCritDamage` and `MpCostPct` were
appended to `StatMods` for the 2026-08-19 rework and never given a formatter line, so any set carrying
one described itself with the number silently missing — the same omission as the four S-grade channels
in 0.59.1. All three have a line now (`MpCostPct` prints as a reduction, which is how it is authored).

### 3. Sigils read "instant cast, instant reuse" — and yes, it was only visual

His question: *"Also sigils should have cooldown ...now says instant cast and instant reuse ...is it
only visual?"*. It was. Nothing about a sigil was broken underneath; the card was describing a
mechanism sigils do not have and hiding the one they do.

**A sigil is never cast**, so its `CastTicks`/`CooldownTicks` are 0 and always were. The card's guard
for "is this a passive, hide the casting rows" tested `def.Passive == null` — but the `passive` bool it
had computed two lines earlier tests **both** marks, a `PassiveEffect` payload OR `Category.Passive`.
**Twelve** skills in the game carry no `PassiveEffect` at all because their entire payload is a PROC —
the six proc sigils (Fury, Frenzy, Focus, Aegis, Immortality, Holy Support / Arcane Support), plus
`arcane_protection`, `magic_proficiency`, `physical_proficiency`, `tank_aggravated_state` and
`wc_combo_mastery`. All twelve fell through the guard and printed `Cast instant / Cooldown instant /
Target Self`. The guard is `!passive` now.

**The cooldown they DO have was printed nowhere.** A proc's `ProcCooldownTicks` is a real lockout,
rolled and ticked server-side (`GameLoopService.TryProcs` → `Entity.ProcCooldowns`): Fury/Frenzy/Focus/
Aegis/Immortality **20s**, Holy Support and Arcane Support **5s**, Combo Mastery 60s, the three
proficiencies 25-30s. Nothing formatted it, so the only thing a player could read was whatever the
author had written into the prose — and the lockout was in none of it. A 3%-on-hit sigil looked like it
could fire every swing.

`SkillText.Mechanics` now opens with the proc block, so both clients get it:

```
Fury Sigil
  Fires by itself — a 3% chance each time you land a hit
  Once it fires it cannot fire again for 20s
  Grants you Fury for 15s

Combo Mastery
  Fires by itself — a 3% (two-handed: 3.45%) chance each time you land a hit
  Once it fires it cannot fire again for 60s
  Grants you Combo Rush for 30s
  Grants your party Combo Rush for 30s
```

It reads the trigger from the flags (`on hit` / `on damage taken` / `on finishing a spell`, narrowed by
`ProcMagicOnly`), states `BL-120`'s two-handed chance when the def carries one, and names the payload
rung **at the level being shown**, resolved exactly the way the proc handler resolves it.

🔑 **The same authoring rule as `Mods` and the four S-grade channels, for the third time now:** the
proc fields went into `SkillDef` on 2026-08-21 and grew two more triggers since, and no formatter line
ever followed them. A field with no line in `SkillText` is a mechanic the game will not admit to.

### 4. Both potion faucets are LEVEL-BANDED now, and one of them closes at 61

Two rulings in one message, after he asked whether the drop rule had ever been written down.

**It had — this morning.** *"Did I said the only dash potions dropped from monsters are common and
uncommon"*: yes, as `BL-152` in 0.109.0 (*"dash pots to drop to uncommon ... all else from crafters"*),
and it was built. Greater, Superior and Grand left every drop table then; Supreme was already
craft-only. What was **never** said is where each surviving rung lives, and that is what this is —
`BL-152` ruled which RARITIES drop, not at which LEVELS.

His two bands, verbatim, and they are different ladders:

> *"Common drop from mobs to 52 after 52~60 start to mix with uncommon and after 60 is only uncommon"* (dash)
> *"Buff potions below 40 drop common at 40~52 start to mix at 52~60 drom uncommon and 61+ stop"*

| mob level | Dash Lesser | Dash Plain | Swift / Alacrity / Fury Common | …Uncommon |
|---|---|---|---|---|
| 1–39   | ✅ | — | ✅ | — |
| 40–51  | ✅ | — | ✅ | ✅ |
| 52–60  | ✅ | ✅ | — | ✅ |
| 61+    | — | ✅ | — | — |

Every boundary moved, in both directions:

- **Dash Plain** opened at level **20** and now opens at 52 — the whole 20-51 stretch was paying out
  the +30 rung he places at a level-52 farm.
- **Dash Lesser** dropped forever with no ceiling; a level-85 kill could still hand out a +15 bottle.
  It stops at 60.
- **The three speed potions' Uncommon** rung also opened at **20**, and now opens at 40.
- **Their Common rung** dropped forever too, and now stops at 51.

Dash is banded on its own because his sentence is about the dash line — and because it is the one
potion ladder that collides with a class skill (see the `FamDash` ordering, where the rogue's Sprint
interleaves with these exact rungs).

🔑 **`61+ stop` is the part with no precedent, and it is only safe because these three are not
drop-only.** From 61 a normal kill pays no buff potion at all, so the scrolls group above 60 is the
enchant rungs alone. That strands nobody: the Apothecary sells the **Common** rung of all nine families
for gold at any level, and a player Potion Master crafts Common at L2 and **Uncommon at L4**. So the
band moves an endgame consumable from loot to the player economy — the same trade playtest 28 made when
the six stat potions left the tables. ⚠ Check that a faucet has a second source before closing it.

**On the magnitudes, he ruled: leave them.** His *"legend pots with 55 speed increase ...and rogues +60
sprint is useless ... rogues sprint is class identity"* was the reasoning for the drop rule, not a
request to retune the ladder — asked directly, the answer was *"Drops alone for dash potions"*. So Dash
stays 15/30/45/50/55/60 and Sprint stays +40 / +60. Worth recording why that holds: Dash and Sprint
share ONE family, so they never stack and the higher rank always wins — **Sprint L2 outranks even
Supreme at the same +60**, and Sprint runs 15s on a 30s reuse against the potion's 15s on 60s, so it
keeps double the uptime. From Greater up, a potion is now a crafted purchase rather than something a
mob hands you.

⚠ **NEW APK, and most of this is client-side.** All three cards — the set bonus, the sigil, every proc
passive — are rendered CLIENT-side from the compiled `Game.Shared`, so none of that text appears until
the APK is rebuilt. The two things that are NOT client-side are the stat ROUNDING and the potion drop
bands: both are server-authoritative and live on the next server start. `ProtocolVersion` stays **33**;
nothing on the wire moved.

## 2026-09-03 — 0.109.3: the full buff was handing out singles

His report: *"Why buff and full buf from admin menu give me now singles ?.. I several time write that
full admin buff and /buff gives -> group buffs + harmonies + harmony mark + great might"*.

Three faults, all in the same command. Together they turned the one route that shows a
**fully-buffed character** — the state every balance number is signed off at — into the weakest set in
the game.

### 1. `force` was applied INSIDE the set, so the set ate itself (regression, 0.107.0)

`BL-131` gave both admin routes `force: true` to fix *"now im full buff and cannot put war bulwark
because of 'something stronger'"*. On the single-buff route that was right. On the **set** route it was
catastrophic, and silently so.

The admin set is **ordered** — groups first, then the class singles, then the 19 NPC blessings — and
that order works only because everything arriving after a group is **refused** by the family/rank rule.
Forcing turns each of those **45 refusals into an eviction**:

- every NPC single tore out the group covering its family (Might evicted `Feral Bloodlust`, Body
  evicted `Body Reinforcement`, …), and
- the extra bodies then blew past `MaxBuffSlots` (20), whose FIFO drops the **oldest** — which is
  exactly the groups, the harmonies and Great Might, applied first.

**The rule now:** force against what the player was **already wearing**, never against what the same
call just laid down. A family claimed by a buff that landed in this call falls back to the normal rank
contest, which is what makes *"groups come first and they simply win"* true again. `BL-131`'s own case
is untouched — `/buff <name>` is still forced.

🔑 **A duration/priority override that is correct for one buff is not automatically correct for a set
of them.** The set's whole design was an ordering argument, and `force` deleted the ordering.

### 2. The set stopped at the 3rd class, so Harmony Mark was never in it

`BuildAdminBuffSet` called `ClassSkills.Cumulative(...)` and let `fourth` default to **false** — the
flag means *"has paid the 100kk Rite"* for a real character. So a set documented as *"everything a
max-level buffer can give"* contained **no 4th-tier buff at all**: Harmony Mark, Harmony of the Soul
and Harmony of Madness were missing from the game's only way to see them. Now `fourth: true`; the
sigils and shared passives it also pulls in are dropped by the existing "is this a timed buff" test.

### 3. The two lists are now SEPARATE — that was the coupling

His ruling on reading the fix: *"The npc buffer that is the spirit helper gives the singles we decided
… the admin-full and /buff gives the real full buffs — groups + harmonies + great might + harmony mark.
**Both are separate. Altering the one should not break the other.**"*

They were not separate. `BuildAdminBuffSet` ended `.Concat(NewbieBuffSet)` — the admin set physically
**contained** the Spirit Helper's shelf, "for anything uncovered". So `BL-150` growing his shelf from
16 singles to 19 put three more singles into the admin bar the same day, and once fault 1 made them
force their way in, they were three more groups evicted. That line is gone: the admin set is now the
buffer CLASS's kit and nothing else.

It cost nothing to remove — **all 19 NPC singles were refused as already-covered, in every version it
ever shipped in**. The safety it pretended to provide is now measured rather than assumed: `--buffs`
prints any NPC family the buffer's own kit fails to cover (today: none, 19 checked). If that line is
ever non-empty the answer is a missing buff in the class kit, not a re-import of the NPC's shelf.

🔑 **Two lists that must move independently may not be built out of one another** — sooner or later
one grows and silently rewrites the other.

### Measured, not asserted

`dotnet run --project tools/BalanceMatrix -- --buffs` models the same rank rule the engine applies:

| | before | after |
|---|---|---|
| squares on the bar | 15 | **18** (2 free of 20) |
| harmonies | 4 | **7** — incl. Harmony Mark |
| groups | 9 | 9 |
| class singles | Frenzy, Great Might | Frenzy, Great Might |

…and that is the intended picture in both versions — the census never modelled `force`, which is why
it went on printing a healthy bar for two versions while the game handed out singles. The census now
also **prints every refusal by name** (45 before the split, 26 after), a list its own header had promised since it was written: with
only a count, a buff that was *never in the set* and one that was *out-ranked* look identical, which is
how fault 2 hid behind fault 1.

⚠ **NO NEW APK, and `ProtocolVersion` stays 33.** All three fixes are server-side (`GrantFullBuffSet`, and
`AdminBuffSet`, which nothing on the client reads). The **server zip** carries this.

## 2026-09-03 — 0.109.2: a debuff that borrowed a buff flag was cast on the caster

His report, hunting as a healer: *"when im with healer and doing armor break why do i get the debuff ?"*
and then the line that located it — *"on auto-on i get the debuff not the mob"*.

**Root cause, one sentence:** Armor Break's M.Def half is authored as a **negative magnitude on
`BuffMagicDef`**, because there is no `DebuffMagicDef` flag and the `SkillEffect` enum is full — and
`BuffMagicDef` is in the `AnyBuff` mask, so two different places in the engine read the skill as
*beneficial*.

### Where it went wrong — two bugs, one cause

**1. Auto-hunt cast it on the player (`ClassifyAuto`).** The classifier asked *"is this a buff?"*
before *"is this a debuff?"*, so Armor Break was bucketed `AutoSkillKind.Buff`, and the Buff arm of
the auto chain targets `p.Id` — the caster. He was farming with **−30% P.Def and −15% M.Def on
himself** and the monster untouched. The two questions are now asked in the other order.

🔑 **A skill is not beneficial because it carries a buff flag — it is beneficial because it carries no
harmful one.** Measured over the real catalog rather than argued: **2 of 581 skills change bucket**
(`lb_ork_armor_break`, `whisp_armor_break`, both Buff → Debuff) and **no** beneficial skill is caught
by the harmful test. The enum being full guarantees more debuffs will borrow a buff flag; asked in
this order they are all handled in advance.

**2. The same fall-through made four debuffs UNRESISTABLE.** The debuff arms of the cast resolution
had already applied the whole def — one `BuffInstance`, both magnitudes — and then the Buff arm
applied it a **second time with no roll at all**. So a *resisted* Armor Break landed anyway: the
contest, the target's `CcResist` and the per-school blessing were decoration. Worse, cursing a monster
paid **support threat** — the same aggro a heal or a blessing pays — because that arm ends in
`AddSupportThreat` over everything it "blessed".

The Buff arm now refuses a harmful payload outright. The four skills this corrects, and what each
gets back:

| Skill | Was | Now |
|---|---|---|
| `lb_ork_armor_break` Armor Break | always landed | its authored `DebuffLandMod` **×1.5** applies |
| `witches_curse` Witches Curse | always landed | its CSV's *"(success chance x0.7)"* finally means something |
| `frost_burst` Frost Burst | M.Def cut landed even when the root was resisted | the cut rides **with** the root — its own note asks for exactly this: *"one buff, so a cure that lifts the hold lifts both"* |
| `whisp_armor_break` Whisp Armor Break | always landed | contested like every other whisp debuff |

⚠ **This makes those four weaker, on purpose** — they are landing on the numbers you authored instead
of ignoring them. Watch Armor Break's uptime in the next pass; ×1.5 is 75% at parity, so it should
still stick most casts.

⚠ **NO NEW APK, and `ProtocolVersion` stays 33.** All three fixes are server-side; `ClassifyAuto` has three
call sites and all three are in `GameLoopService`. The **server zip** is what carries this.

📌 Noticed while verifying, NOT changed: `SureHit` is honoured on the fizzle path and on the
*uncontested* debuff branch, but not on the **contested** one — so Frost Burst's root has always been
resistable despite the flag. Left alone as a separate question rather than folded into a bug fix.

## 2026-09-03 — 0.109.1: every rune is Mythic

`BL-153` widened, and the version constant it was owed. The ruling and its reasoning are written up
under `BL-153` in the 0.109.0 section below — this entry exists because the label shipped without one:
`27d9501` carried the commit message `fix(0.109.1)` but never moved `GameConstants.GameVersion`, so
the next build would have stamped 0.109.0 a second time. Corrected here.

⚠ **NEW APK.** The client colours item names from its own compiled `ItemCatalog`, not from the server,
so the 55 laddered reward runes only turn Mythic on the phone with a fresh install. `ProtocolVersion`
stays **33** — no wire change.

## 2026-09-03 — 0.109.0: `BL-149`…`BL-153`, the buffer economy

Five rulings from the chat pass that followed `BL-147`'s generated page. He started from *"limit the
buffer free to <60, 60~75 paid and 75 no buff only box"* and landed somewhere better: **the free/paid
line is the BUFF, not the player's level.**

⚠ **NEW APK, and `ProtocolVersion` moves 32 → 33.** `BufferBuff` gained `MinLevel`, but the reason for
the bump is behavioural: [Full buff] no longer exists server-side, the Mage and Fighter presets are
different lists, and eleven blessings are refused below 40. An old client would draw a button that
does nothing and offer buffs the server will not cast.

### `BL-149` — Vampirism and Resolve become scrolls; the box goes 17 → 19

*"vamp and resolve can be made as scrolls as well and add to boxes. Buffers/healers have resists,
shield, great might/bulwark buffs"*.

They were **the only two NPC blessings with no consumable anywhere** — the gap `BL-147`'s page was
built to expose, and it mattered because `BL-150` stops the buffer at 75: without a scroll, both would
have vanished above 75 for anyone without a Warchanter. His reason for being comfortable is now in the
code: the buffer class keeps Clarity, Fortitude, Shield Blessing, Shield Hardening and the Great
Might/Bulwark tier, and after this change **those are the only four families left with no consumable
at all** — which the regenerated page proves in its own section 2 rather than asserting.

⚠ One scroll each, not a trio, at rungs **5 and 7, not 6**: a scroll takes its family's top rung and
those two ladders are not six deep. Craftable at Scribe L5; the box stays **pick 10**, so it got wider,
not more generous.

### `BL-150` — the NPC buffer: two tiers, nineteen blessings, no [Full buff], and it ends at 75

| tier | from | each | what |
|---|---|---|---|
| **free eight** | **6** | **free** | Fury, Alacrity, Force, Might, Bulwark, Swift, Vampirism, Resolve |
| **paid eleven** | **40** | **15,000** | Body, Soul, Vigor, Serenity, Agility, Aim, Ward, Frenzy, Focus, Ferocity, Insight |
| *above 75* | — | — | the buffer refuses and names the replacement |

🔑 **The free/paid line is the buff, not the player.** The old rule was "everyone free below 75,
everyone pays above"; a level-74 character now still pays nothing for Might and 15,000 for Aim, and
neither answer depends on who is asking. `BufferFreeUnderLvl` was deleted rather than retuned.

🔑 **The two presets are the free eight, partitioned.** Fighter (might, bulwark, vamp, fury, swift) ∪
Mage (alacrity, force, bulwark, resolve) is exactly the eight, Bulwark being the buff both roles want.
That is what makes his instruction work — *"if you want 'full buff' you buff fighter+mage sets and buy
all 40+ then save your own"*: two free presses fill a levelling bar, and no single press takes all
nineteen any more.

🔑 **The level gate is applied when a preset is EXPANDED**, which is why his saved-preset rule needed no
new state: *"if some1 buff me with body or soul and i save it and im <40lvl they will not activate ..
they will activate after 40+"*. The id stays in the preset and starts landing on its own at 40.

⚠ The price doubled 7,500 → 15,000 (the constant moved, not the formula, so the "scales when buffs
become multi-level" TODO survives). But **a full set is 165,000, not the 120,000 he calculated** — that
sum was eight paid blessings, and Focus, Ferocity and Insight joined the paid tier in the same message.
⚠ And **nineteen against a buff cap of twenty** is the state playtest 28 trimmed 19 → 11 to escape.
Deliberate here: a real buffer's groups evict 18 of the 19 into 5 squares, so the squeeze is only felt
buffing solo. If it bites, the cap moves, not the list.
⚠ The **restore price is ours, not his** — he priced the buffs only, and the old "free at or below 75"
would have become free forever under the new ceiling. Aligned to the paid tier: free below 40.

### `BL-151` — the Blessing Box is 300k

*"Buff box price 250-> 300k twice as the cost per buff from npc but it gives you outside town buffs"*.
300,000 ÷ 10 picks = **30,000 a blessing-hour, exactly twice** the NPC's 15,000. The price is derived
from those two numbers in the comment rather than picked, so changing either is visibly changing both.
What the double buys is the thing the NPC cannot do: a scroll re-buffs you in the field.

### `BL-152` — Dash potions drop only to Uncommon

*"dash pots to drop to uncommon ... all else from crafters"*. Greater, Superior and Grand left the drop
tables; Supreme was already craft-only, and all six rungs stay craftable. This finishes a rule two
earlier passes started — playtest-17 `E3` pulled the scrolls, playtest 28 cut the stat potions to three
speed families, and **both times Dash was written down as the deliberate exception**. "The top of a
ladder is bought, not found" is now true without a footnote. ⚠ Unlike those passes this one *narrows*
the faucet instead of concentrating it: the three removed ids were the whole of rungs 3-5.

### `BL-153` — every rune is Mythic

*"make war/spell runes mythic grade (all others as well if they have no Levels but still SP rune 10 is
different from SP rune 100)"*, and then, the same day, closing the open question this entry had left:
*"all runes if they can be same rarity at mythic and SP/EXP/etc runes just be same rarity at mythic"*.

🔑 **The first pass read the ruling as a test and the test was wrong.** It took "SP rune 10 is
different from SP rune 100" to mean rarity is what tells the rungs apart, so it swept only the
level-less runes — War, Spell (Rare → Mythic) and the two punishments Sinister and Sinners (Epic →
Mythic) — and left the 55 laddered reward runes on Epic. His answer is that the rung and the NAME carry
that difference, not the colour of the line in the bag. So the rule is now flat and has no test in it:
**`EquipSlot.Rune` ⇒ `ItemRarity.Mythic`, no exception**, all 59. `RewardRune` no longer takes a rarity
parameter, and `ItemCatalog.ValidateRunes` refuses to boot on a rune that is not Mythic — the rule is
one word, and the next rune will be authored by copying a neighbour, so the guard is what makes "all
runes" survive that copy.

⚠ **Display and sort order only, verified rather than assumed.** Rarity feeds three real systems —
`Recipes.FinishedItemRecipes`, `Crafting.Disassemble` and the `ShopCatalog` ladder — and all three gate
on `ItemLevel > 0` **and** a gear slot before they ever look at it, while a rune has ItemLevel 0. Rune
pricing is pinned by `BuyPriceOverride: -1` / `SellPriceOverride: 0` / `Value: 0`, so `RarityPriceMul`
never runs on one. Nothing in the economy moved.

⚠ The **Rune of Tincture** is not swept: it is `EquipSlot.Consumable` with a real `Value: 40000`, so
raising it would move its vendor price. It carries the word "Rune" but is not one. Flagged on the
Backlog as a one-line change if he meant it too.


## 2026-09-03 — 0.108.0: `BL-145`…`BL-148`, the buff bar and the zone HP ladder

The second half of the same playtest pass. Four items: two on the buff bar, one generated reference
page, and his revision of the HP multiplier that `BL-137` had spent a whole entry misattributing to IG.

⚠ **NEW APK.** `ProtocolVersion` stays **32** — nothing on the wire moved. Both buff-bar items are
pure client layout, reading fields the server has been sending since `BL-111`.

### `BL-145` — an hour-long scroll was hiding in the rune row

*"scroll/potion buffs and swift should count towards the buff limit.. now i have 2 scrolls 16npc buffs
+ focus ferocity scrolls and the 2 scrolls are in the warrune bar"*.

🔑 **Half of this was already true and the entry said otherwise — my error, recorded so it is not
re-derived.** Scroll and potion buffs, Swift among them, have ALWAYS counted against the twenty: a
`ConsumableBuff` wrapper hands out the family's rung, the rung carries `CountsTowardBuffLimit: true`,
and the wrapper's row is `BuffRow.Consumable`, which `CountsAgainstBuffCap` counts. The generated page
from `BL-147` below now proves it in a column — Swift, Focus and Ferocity all read **Slot: yes**. The
Backlog entry's claim that the War Rune bar is `BuffRow.Item` was simply wrong; every rune buff in the
game is authored `Consumable` (and exempted by its own flag), which is why a scroll landed beside one.

🔴 **What WAS broken is the ROW, and it contradicted the code's own doc comment.** That comment has
read *"COUNTS AGAINST THE CAP IS THE FIRST TEST … that is what makes the top bar mean something"*
since `BL-111`, and the code underneath it tested `Item` and `Consumable` **first**. So the top bar was
not the counted set: an hour-long Scroll of Focus, which spends one of his twenty, drew in the
consumable row beside a War Rune, which spends nothing.

The grouping is now the server's predicate exactly — everything costing a slot is in the top bar, and
`Item`/`Consumable` hold only the free riders (the runes, healing and mana potions, Dash, the toggles).
A potion of healing still has its own bar, which is what he asked for in playtest 27; a blessing that
came out of a scroll no longer hides in it.

### `BL-146` — the count moves onto the hide button, and every bar gets its own

*"the x/20 text is invisible make the hid button show count (if possible over 15 yellow over 18 red)
also i want each buff bar to have its own hide button"*.

`BL-111` drew the counter as a bare 12pt label on the world layer with nothing behind it. The button
beside it is a filled box big enough to read on a phone, so the number moved onto it. His thresholds,
verbatim: **>15 yellow, >18 red** (red also covers the cap itself, where "20/20" and "19/20" are one
glyph apart).

Four buttons now, one per collapsible row (buffs / consumables / items / others), each with its own
three-stage collapse — shown, one row, hidden. Debuffs still have none and are never hidden. A hidden
group keeps the one line its button sits on: four buttons stacked on the same y would be unclickable,
and each has to stay in front of the bar it belongs to.

### `BL-147` — the consumable-buff inventory, GENERATED

*"can u show me what buffs we have as scrolls and what on potions (which are bought which are crafted
and which are same as npc buffer) and which we dont have that are single buffs"*.

**[`docs/data/BuffConsumables.md`](data/BuffConsumables.md)**, written by
`dotnet run --project tools/BalanceMatrix -- --buff-consumables`. 20 families with a consumable, 52
without, 48 items.

🔑 **It is generated because the interesting half is an ABSENCE.** "Which buffs have no potion" is
defined by what is *missing*, so a typed table is wrong the day someone adds the missing bottle and
nobody remembers the page exists. Every column is a query: `UseSkillId` for what a bottle does,
`ConsumableBuffForm` for potion vs scroll, `ShopCatalog` / `RecipeCatalog` / `BoxCatalog` / the mob
drop tables for where it comes from, `NewbieBuffSet` for what the buffer NPC gives, and the server's
own `CountsAgainstBuffCap` for the slot column.

**The answer to the last question — ladder families with no potion and no scroll:** Clarity,
Fortitude, Resolve, Shield Blessing, Shield Hardening and Vampirism. Buffer-or-nothing.

⚠ Two classification traps the first draft fell into, both fixed and both worth knowing: **a
consumable buff has TWO shapes** (a Might Potion is a one-child wrapper; a healing potion IS the buff,
no children), and filtering on the first alone listed `potion_heal` under "has no consumable" — the
exact opposite of the truth. And **a toggle is not a ladder rung** even though it has no duration, no
MP and no cast time, which filed Holy Soul as an unbuyable family nothing grants.

### `BL-148` — the zone HP ladder, re-ruled, and the plate now says so

**His ruling:** *"Zone laddre x1<40, x1.5<76, x2<83, x3 84+, elits still have their x4 everywhere so
x4<40, x6<76, x8<83, x12 84+ (futer tests will alter it probably..)"*

| level | zone | elite (zone × rank ×4) | a field mob's TTK, was → is |
|---|---|---|---|
| < 40 | ×1 | ×4 | unchanged |
| 40-75 | **×1.5** | ×6 | 61: 39s → **19s** · 72: 66s → **33s** |
| 76-83 | ×2 | ×8 | 80: 46s → **31s** (unchanged in value, the rung moved under it) |
| 84+ | ×3 | ×12 | 55s, unchanged |

His second list is the **composed** number, not a second knob: ×1.5 × 4 = ×6, ×2 × 4 = ×8, ×3 × 4 = ×12.
`MobRankScale.Hp(Elite)` stays ×4 flat and was not touched. The 84+ elite therefore **keeps its
68,208** — that is deliberate, and it is the number he opened the entry complaining about.

⚠ **Level 83 is mine, not his.** His bands read `x2<83` and `x3 84+`, which leaves 83 unnamed; it is
filed under ×2 so that "x3 84+" is literally true. One line to move if he meant otherwise.

Measured, not derived: **`dotnet run --project tools/BalanceMatrix -- --zonehp`**, new today. It prints
the base curve, every rung's pool, and the time-to-kill each produces for the buffed farm roster — the
same five sheets the band table uses, so no single class decides the answer.

🔴 **And the plate now prints the multipliers, which is the half of his complaint that was plainly a
bug**: *"in its info panel there is nowhere x3 and no passive in skills tab"*. Correct — the two
biggest terms in a creature's pool are entity FIELDS (`MobZoneHpScale`, `MobHpScale`), not `MobMod`
passives, so `MobMod.Describe` could never see them and nothing else printed them. That is why
searching the plate for the ×3 found nothing and why searching the code for a `MobMod.Hp` found four
templates, none of them his. They are drawn as **two lines, never pre-multiplied** — "×12" tells you
nothing about which knob to turn, and these are exactly the two knobs a retune moves. A boss is exempt
from the zone ladder, so its zone line is not drawn: printing a multiplier that is not being applied is
the same bug in reverse.


## 2026-09-03 — 0.107.0: `BL-130`…`BL-144`, the cast-speed pass

Fifteen items across one long chat pass while he playtested 0.106.0. The heart of it is one wrong test: **which
speed stat paces a cast** was asking `Category == SkillCategory.Physical`, and `Category` is a ROLE
tag, not the physical/magical axis.

⚠ **NEW APK.** `ProtocolVersion` stays **32** — nothing on the wire moved — but the client builds its
Learn tab locally from the compiled `ClassSkills`, and `BL-134` adds a row to the fighter's shelf.

### `BL-132` — a physical skill's cast time reads ATTACK speed. Only *damage* skills did.

*"physical buffs/debuffs/spells should speed up by attack speed not cast … now i cast shield shock for
~2s .. when its default cast is 1s and my as is 580 (x1.74) and my cast is 182 (x0.55) … physical
skills seem to work but the buffs dont"*.

His measurement was exact: Shield Shock's authored 1s × the cast multiplier 333/182 = **1.83s**.

🔑 **`SkillCategory` is a five-way ROLE tag** — Physical / Magic / Buff / Debuff / Heal — so a
physical stun is authored `Debuff` and a physical self-buff is authored `Buff`. Asking
`Category == Physical` really asked *"is this a physical DAMAGE skill"*, and every other physical
skill in the game was paced by a fighter's (poor) WIT-driven cast speed. That is precisely the split
he described.

🔑 **The axis already existed twice over and neither half was being read here**: `SkillDef.DebuffSchool`
carries physical/magical for contested debuffs, and his CSVs' `TYPE` column has always carried the
word in prose (`Physical/Active`, `physical debuff`, `pfysical buff`, `Magic/*`). New:
`SkillMath.PacedByAttackSpeed(def)` = `Category.Physical` **or** `DebuffSchool.Physical` **or** the
new `SkillDef.PhysicalCast` flag, which is what the physical BUFFS carry. One helper, used by the
cast path, the auto-hunt cycle estimate and `BalanceMatrix` alike.

Tagged `PhysicalCast`: `sprint`, `evasion_boost`, `bow_expertise`, `wc_bow_expertise`,
`defensive_wall`, `battle_regeneration`, `battle_presence`, `battle_defence`, and — found by the new
check, not by reading — `provoke` (Taunt) and `mass_provoke`.

🔴 **`SkillCsvSeed --check` NOW COMPARES THE `TYPE` COLUMN**, which it never has. That is exactly how
`Charm` sat in the code as `DebuffSchool.Physical` for a whole version while his own `tank 3rd.csv`
said `Magical Debuff`. Only the physical/magical *word* is checked — cells with no such word
(`Passive`, `Toggle`, `Whisp`, blanks) are skipped and every spelling is accepted, because the column
has never had a grammar and demanding one would report thirty rows he has no reason to touch. ⚠ The
check was proved by planting the Charm break back and watching it report 19 rungs before reverting it.

### `BL-133` — fighter base cast speed 150 → 300, and Charm becomes magical

*"why fighters have so low cast speed ? shouldnt it all have about the 300~400 cast in the begining …
now my elf figter have 130 base and 182 buffed .. and i think he must have 260 (or whatever base x wit
mod) and ~365 buffed"*. **Both of his numbers were the code's exactly**: an elf fighter's WIT is 17, so
150 × `CastWitModifier(17)` = 150 × 0.864 = **130**, and ×1.4 from a cast buff = **182**.

| class | WIT | ×witMod | was | now (base 300) | buffed ×1.4 |
|---|---|---|---|---|---|
| Demon Fighter | 10 | 0.613 | 92 | **184** | 258 |
| Human Fighter | 14 | 0.746 | 112 | **224** | 313 |
| Elf Fighter | 17 | 0.864 | 130 | **259** | 363 |
| Demon / Human / Elf Mage | 19/20/23 | 0.952/1.000/1.158 | 286/333/386 | unchanged | 400/466/540 |

🔑 **One correction to his model, and it makes his case stronger**: the elf mage's 386 is not
Spellcaster Mastery — it is 333 × the WIT modifier of a 23-WIT elf. The masteries carry the *wrong*
-armour and *wrong*-weapon PENALTIES (`CastSpeedPct −0.5`), which is his *"386 → 193 without robe → 96
without wand"*. So `base × witMod` already was the model he described; the only number disagreeing
with it was the fighter's 150.

⚠ What this actually moves is small, because `BL-132` took every physical skill off cast speed in the
same pass — a fighter's cast bar now holds only his magical debuffs. The class that gains broadly is
the **Warchanter**, a `BaseClass.Fighter` whose whole kit is songs.

**Charm is `DebuffSchool.Magical`**, on his ruling: *"charm is a magic taunt not phisical -> charm is
saved by SPT, Freeze as well, Stay and Shield Shock are the only physical debuffs atm and are saved by
CON -> the tank 3rd is fixed (2nd charm is still physical active)"*. `tank 2nd.csv`'s four Charm rows
moved from `physical active` to `Magical Debuff` with it. ❓ Intimidate (the Demon's fear) is left
PHYSICAL and flagged in `BL-133` — he did not name it, and a Demon roar is not a spell.

### `BL-131` — `/buff` gets a duration, and an admin buff can replace what the full buff gave you

`/buff [target] <name> [duration] [lvl]`, where a duration is `90s` / `30m` / `1h`, scanned from any
position (his grammar puts it in the middle). **Everything defaults to one hour** — the typed command,
the six buttons, the full set — which was his fallback ask and makes the whole admin buff layer one
number. A bare trailing integer is still the LEVEL; only digits with a unit letter are a duration.

🔴 **The "something stronger" refusal was a consequence of `BL-126`.** War Might and War Bulwark share
a family at the SAME rank, and `ApplyBuff`'s equal-rank rule is *keep whichever runs longer* — so the
moment the full buff started handing its half out at an hour, the 20-minute one from a button could
never win, and `BL-127`'s six swap buttons stopped swapping the day the hour landed. 🔑 **A duration
override changes who wins a stacking contest**: two rules that were independent stopped being
independent when one side's clock was extended. Fixed with `ApplyBuff(force: true)` on the admin path
only — it skips the two refusals and nothing else, so the eviction still happens properly. The NPC
buffer does **not** force: it pre-filters with `BuffWouldLand` so nobody is charged for a blessing the
contest would discard, and forcing there would overwrite a player's own stronger buff with a bought one.

### `BL-135` — cancelling a cast by pressing Attack was free

*"click on attack that is on the skill bar it cancels the cast of the skill and dont enter it in
cooldown .. while if i cast and cancel it trough same button X … it start to cooldown"*. `HandleAttack`
called `CancelCast(attacker)` and `startCooldown` defaults to **false** — an exploit shape, not only an
inconsistency: any long cast could be aborted at no cost by tapping Attack. **The rule, now stated in
the code: a cancel the PLAYER chose starts the cooldown; only an enemy interrupt or a forced stop
(stun, death, petrify) leaves the skill ready to retry.**

### `BL-130` — whisps re-summon on their reuse, not in the last five seconds

*"charming whisp (and i guess all whisps) resummon on cd not when whisps disapear"*. `BL-109` gave
whisps `BL-112`'s five-second renewal window, and the consequence he hit is that the only moment you
may re-call one is the moment it is about to leave anyway — a ban with a five-second hole in it. The
30s reuse is the limiter now. ⚠ It still costs 4 Skill Stones a call, so spamming it is expensive
rather than impossible; `SummonWhisp` still refreshes in place without reordering the stack.

### `BL-134` — the fighter gets `+WIT −SPT` on the Mindwriter's shelf

*"please add the +wit-spt in the skill swap for fighters as well"*. A fighter could not buy a single
point of WIT at any price, which made *"the elf is a magic knight"* a direction with no lever behind
it. One way, like his `+SPT −ATK`; forgetting a rung is free.

### `BL-138` — the Learn tab: the row IS the learn button

*"clicking on the row … not to open the details but the learn details … u can remove the learn button
and the actual row click is the learn click and inside the learn details to be a confirm button that
is grayed out when unable to learn"*. Two targets on one row is what made a mis-tap possible — the row
body opened the skill card and a 104px button opened the purchase, so most of the row was the wrong
action. Now one target, one destination; the purchase page already contains everything the card
showed, and it opens for a skill you cannot afford with the reason spelled out and a **dim** Confirm.
Greyed, not hidden: a missing button reads as a broken window.

### `BL-136` — chat and combat leave the back-button stack

*"can chat and combat window not to count as opened windows for the back button"*. Both went through
`ToggleWindow` → `OpenWindow`, which is what registers a panel, so leaving the chat log open meant
every back press spent itself closing it. They are persistent HUD, not modal windows.

### `BL-137` — his level-72 mob validated our HP curve to the unit. No code changed.

*"a lvl 72 redhorn footman have 12561"*. `MobBaseStats.Hp(72) = 40 + 0.8·72² = 4,187`, and
**4,187 × 3 = 12,561** — his number exactly. That is an IG `HP Increase (x3)` tag, which
`balance/MobCurveVsIG.md` measured across 2,831 creatures (77% ×1, 23% ×2-×5) and is exactly why
`BL-78`'s HP half was ruled *"stays as is"*: **our base equals their base, and the big numbers are
bought by the `MobMod.Hp` layer**, which exists, works, and is unauthored on the field roster. The
first outside data point confirming the curve.

### The second half of the same pass — `BL-139`…`BL-144`

Found while he playtested **0.106.0**, so none of them are caused by the work above.

**`BL-139` — Shield Reinforcement was never declared a toggle.** *"it not act as a toggle at all .. it
casts something but doesnt do nothing ... for a split second i see my pdef rises"*. His diagnosis is
the bug: `Category: Buff` + `MpPerSecond: 15` and **no `Toggle: true`**, so `ApplyBuff` read its
`DurationTicks` of 0 and the stance landed and expired on the same tick. The +300 P.Def he glimpsed
was real, for one tick. His CSV row has said `Toggle` since the file was written — **the second
disagreement between that column and the code in one day**, after `Charm`. So `--check` grew the
other half of the `BL-132` test: **a CSV `Toggle` now demands the flag.** One-directional on purpose —
a missing flag is a dead skill, a missing word is a spelling. Both new checks were proved by planting
the break back before reverting it.

**`BL-140` — enchant and attribute run from the item, not only from the scroll.** *"i open details of
a weapon and click Enchant it ask me which scroll if i have any … now the reverse is a bit harsh ->
find scroll click _. click use -> find weapon from 250 equipments"*. He is right about which end is
long: you hold a few scrolls and a couple of hundred items. Both directions stay — scroll-first is
right when you have just looted one — and the new buttons appear only when a scroll that would
actually be accepted is in the bag. The eligibility test is `ScrollCanTarget` read backwards, so the
two flows cannot disagree, and both land in the same confirmation and the same command.

**`BL-141` — the item window never redrew.** *"attri scrlls dont update the weapon details after added
-> the only way to see what have been added is to open the attribute weapon selection again"*. Two
causes: it closed itself after a scroll, and nothing re-rendered it on a bag push anyway — it drew
once from the DTO it was opened with and kept that copy. It now tracks the INSTANCE it is showing and
redraws from every inventory push, closing itself only when that item has actually left the bag (a
spent last scroll, a piece a Common scroll shattered). ⚠ **Its stamp includes the ATTRIBUTES**, which
is the whole trick: a re-roll changes nothing else about an item, so a stamp built the way the bag's
is would have been identical and still would not have redrawn.

**`BL-142` — `[ORDER]` persists.** It shipped unpersisted in `BL-117` because there is no settings
message to carry it, and there does not need to be one: it is a client preference, so it goes in
`PlayerPrefs` beside the camera distance. Written on the press, not at shutdown — a phone app is
killed, not closed. ⚠ Also fixed: the five buttons share one setting but each painted its own caption
at build time, so changing the order in the bag left the vendor's button reading the old word.

**`BL-143` — Backlash moves to the 4th class (76).** *"its not authored and not seeing it in the csv
but in the game is a class mismatch"*. 🔑 It was never in ANY file: it is auto-granted, and **the
level was mine, not his** — the code comment has said so since `BL-08` (*"⚠ THE LEVEL IS MINE, NOT
HIS ... granted at the 3rd class change (40) to sit beside Deflection, which he DID date"*). He has
now dated it, so the invention retires: 76, with a row in `tank 4th.csv` at SP 0. ⚠ **The grant now
also un-grants below its gate** — it is a plain assignment into `LearnedSkills` and nothing here has
ever taken one back, so every 40-75 tank on an existing save would have kept a skill the new rule does
not give. The warrior's Deflection is untouched; he dated that one himself.

**`BL-144` — Skill Stones stack to 9,999.** *"skill stones to stack to 9999 while the element type
stones to stay at 99 … skill stones are used for fast reuse casts like heals etc"*. The line he drew
is SPEND RATE: a Skill Stone is the reagent of ordinary repeated casts (a heal, a whisp at four a
call), the elemental/holy/physical stones are set-piece reagents spent in ones. It is the **first user
of `ItemDef.MaxStackOverride`**, written for exactly this in 0.93.0 and unused until today; the number
still lives in `StackLimits` so a retune stays one edit.

**`BL-137` follow-up — the ×3 he could not find.** *"i dont see in skills x3 hp on mobs passive ...
its somewhere invisible"*. The display works (`MobMod.Describe` prints `Max HP ×3` on the inspect
plate); **the roster is empty** — exactly four templates in the game carry an HP multiplier, and not
one is an ordinary field creature. Nothing is hidden; there is nothing there. That is the authoring
`BL-137` says is owed.

## 2026-09-03 — 0.106.0: `BL-126` free self-buffing, `BL-127` the admin menu rework

Two asks of his that had **never been written into any file** — raised in chat beside `BL-118`, never
given a `BL-nn`, and so never built. He was right that playtest 29 was not closed. Both are built
here, with the two UI bugs he found in the same message.

⚠ **NEW APK**, and `ProtocolVersion` 31 → 32: `DebugConfigDto` gained a field and the panel sends
that record positionally, so a stale admin client would send one float short and silently switch the
new setting off.

### `BL-126` — anyone may `/buff` themselves, for an hour

*"I want a setting in the menu same as the class without quest one ... Or easier with this settings on
everyone can use /buff command (just self not others)"*. He offered both roads and named this one
easier; it is, and it lands the same thing — **a non-admin character fully buffed without being
promoted to admin and demoted again**, which is the workaround `BL-118` deleted for class change.

`RateConfig.FreeBuffs`, a 0/1 on the Tune tab beside Free class change. While it is on:

- **`/buff` works for every player, and only on himself.** The target word is not parsed at all under
  `selfOnly` — no name, no `@t` — so the half of the command that acts *on* another person stays
  staff. Enforced on the server, not by the client hiding a field.
- **`/buff` now travels from every client**, because whether a player may cast it is a server setting
  this client is never told (the tuning DTO is admin-only). With the flag off the server answers
  *"Self-buffing is switched off on this server."* — 🔑 the old path returned in silence, which is the
  reply that costs an hour to diagnose.
- Staff fall through untouched and keep the full command, targets included.

### The admin buff set: an hour long, and two buffs lighter

- 🔑 **All admin buffs now last 1 hour** (*"make all the admin buffs 1h"*). A class buff's authored
  duration is 20 minutes or less — right for a buffer playing the class, wrong for a test bar you want
  intact after a farm hour — while the NPC blessings beside them already ran an hour, so **half the bar
  used to expire while the other half stayed**. One `durationOverride` on the set the admin button and
  `/buff` hand out; nothing a player casts is touched.
- **Shrouding Hymn and Bow Expertise are out of the full buff** (*"don't want a Shrouding hymn in the
  full buff. And bow expertise."*). Both were really in it, and both spoil what a full buff is for:
  Shrouding Hymn is party stealth, so a buffed test character cannot be attacked unless he starts the
  fight; Bow Expertise does nothing without a bow. Neither is lost — both are one `/buff <name>` away.

### `BL-127` — the Functions and Class tabs

- **The four level buttons leave Functions and open the Class tab**, first, above everything they
  unblock: a discipline needs 40, a subclass its own floor, a 4th class 76.
- **Six new buff buttons under Full Buffs** — Holy / Life / Blood / Harmony Mark, Great Might and
  Great Bulwark. 🔑 These are exactly the buffs a full buff can only give you ONE of: the four Marks
  share a buff key and the two greats share theirs, so the set picks one and there was no way to see
  the others. A button is the swap — *"now as mage I get might - I want to be able to swap it"*. They
  send the skill ID, which `/buff` matches exactly, so a button can never trip the ambiguity rule a
  name lookup lives with ("Might" is three different buffs).
- **Reset is one button and a selection**, the same shape as "+ Add a class" beside it. Six permanent
  rows that wipe your character sat under six that merely switch between them.

### The three UI bugs from the same pass

- 🔴 **The buff detail popup was smaller than its text** (*"when bow expertise is inactive and showing
  details from the buff bar the containing window is smaller than the text"*). It was a fixed 360×150
  with a 104-tall body — fine for the two lines an ordinary blessing prints, and **exactly wrong for a
  suppressed buff, which has to say what is holding it down as well as what it does**. It now measures
  its own text with TMP's `GetPreferredValues` at the real wrap width and grows, clamped at both ends.
- 🔴 **The bag's `[ORDER]` button hung outside the window while the bag was collapsed.** The
  arithmetic is the diagnosis: five tabs at 82 from x=16 end at 426, the button is 86 wide, so its
  right edge sat at **512 against a collapsed width of 460** — it only fitted once `[Equip]` widened
  the panel to 792, which is why it looked right whenever the paper-doll was open. It moves to the
  control row beside `[Equip]` and `[Del]`, where it belongs anyway: sorting is a view control, not a
  category.
- 🔴 **The Equip tab's filter chips were 100px tall** (*"lower the height of equip buttons ...now they
  are like a 100 .. Make them as height same as text ... Just the actual filter buttons
  [weapon][armor][F20] etc"*). They were written at 24 — twice, on his own earlier instruction — and
  rendered at 100 anyway. 🔑 **The 24 was on a `LayoutElement` that nothing in that parent reads.**
  The scroll content's `VerticalLayoutGroup` runs with `childControlHeight = false`, so it never sets a
  row's height: a row is laid out at its OWN rect height, and a strip built as a bare
  `new GameObject(typeof(RectTransform))` starts at Unity's default **100**. The buttons beneath it
  looked right only because they come from `UiKit.TextButton` and carry their own rect. Fixed by
  putting the height where the parent actually looks — the rect — and keeping the `LayoutElement` for
  parents that do read it; `childForceExpandHeight` also goes off, so a chip can never inherit an
  oversized strip again. Three strips × the tier row's two lines: **~150px of gear list bought back**.
  ⚠ The item rows below the filters are untouched, per *"The filtered equip can stay as is"*.

## 2026-09-03 — 0.105.1: a whisp costs four Skill Stones, and Shield Smash retires Strike

Two rulings of his, one line each, both built on both sides — the code and the CSV row.

⚠ **NEW APK.** Both are visible on the client: the Learn tab is built from the compiled class
tables (a `Replaces` hides the retired skill), and the reagent line is read off the same `SkillDef`.

### A whisp summon burns 4 Skill Stones

*"I want whisps to take 4 skillstone each summon"*. Every one of the six summon ladders now carries
`ConsumableId: skill_stone, ConsumableAmount: 4` — one number on the shared `WhispSummon` builder, so
the six cannot drift apart. It rides the reagent machinery that was already there:

- **Gated up front** (`GameLoopService.cs:1949`) — without four stones the cast never starts and the
  message names the price, so a whisp is never half-summoned.
- **Charged on landing** (`GameLoopService.cs:10743`) — an interrupted summon costs the 20% initial MP
  and no stones.
- **Paid again on every re-call**, including the 5-seconds-remaining renewal window of `BL-112`. That
  is the actual price: a whisp lasts 20 minutes, so keeping one up is **3 summons = 12 stones = 4,800g
  an hour** at the vendor's 400g, and **9,600g** once Whisp Mastery opens the second slot. Against the
  ~549k/hour a level-34 farm was measured at, that is ~1.7% of income — a real cost, not a wall.

### Shield Smash replaces Strike

*"shield smash to replace strike"*. Both smashes — `tank_smash_rate` (Human;Elf) and
`tank_smash_power` (Demon) — declare `Replaces: [strike]`, so the level-40 rung retires the level-5
sword/blunt bash off `fighter 1st.csv` exactly the way Holy Ray retires Holy Bolt. It is a
**within-chain** replace (fighter → tank), which is what his cross-chain id rule allows —
`--chain-audit` still reports **0 cross-chain Replaces**.

### Both sides moved, and the checker proves it

`tank 3rd.csv`: 48 whisp rows gained *"Consumes 4 skill stones"*, 30 smash rows gained `[strike]` in
the REPLACES column. `SkillCsvSeed --check` is green on all thirteen walked files.

🔑 **The reagent count is genuinely compared, not eyeballed** — a deliberate 5-vs-4 mismatch was
planted in one row first and the tool caught it twice over (`🟡 reagent: CSV 5 vs code 4`, plus a
`🔵 LADDER DIP` on the way down to rung 2). A guard that has not been run against the broken case is
not yet a guard.

## 2026-09-02 — 0.105.0: THE BULWARK, 40-74 — the fourth finished 3rd class

*"U can finish the tank 3rd"*. `tank 3rd.csv` lost its `NOT DONE` banner, so the tank is built —
and the pass turned out to span **all three tank files**: he retuned `tank 2nd.csv` in the same
breath, and the 3rd's ladders continue it.

⚠ **NEW APK.** The class table changed, and the client builds its Learn tab from the compiled tables.

### 🔑 Race decides four of the tank's tools, and this is the first class where race decides anything

His RACE column, which is the whole discipline:

| | Human | Elf | Demon |
|---|---|---|---|
| aggro | **Taunt** | **Charm** (replaces Taunt) | **Taunt** |
| control | **Mass Taunt** | **Freeze** (30s, to −50% speed) | **Intimidate** (10s fear) |
| smash | Shield Smash — **Rate** | Shield Smash — **Rate** | Shield Smash — **Power** |
| whisps | taunt + bind | charm + heal | armor + weapon break |

So a Human holds a pack, an Elf controls one thing at a time and drags it to him, and a Demon breaks
what he is fighting. Everything else is shared: the four masteries, Final Defense, Aggravated State,
Stay, Shield Shock, Defensive Wall, Shield Reinforcement, Whisp Mastery.

### The 2nd class moved too, and three of those changes are structural

- **Taunt: 3s → 1.5s, reach 600 → 400, and its MP cost is now ZERO at every rung.** A taunt that
  costs mana is a taunt a tank stops spamming, which is the opposite of the threat economy `BL-123`
  settled — the design is that he has to keep *earning* the top of the table.
- **Charm arrives at 24, for the Elf, and REPLACES Taunt.** Race splitting a kit at level 24 is
  earlier than anything else in the game does it.
- **Shield Stun -> Shield Shock**: a ladder from 24 instead of one rung at 28, reuse 10s -> **3s**,
  and it lands at x0.7 — a 9-second stun every 3 seconds would be a perma-lock at x1.
- **Stay! left the 2nd class entirely** and is the 3rd's whole ladder from 40, at 10s not 15.
- **Defensive Wall lost its two `x2` percent terms** — flat P.Def and M.Def now, on both rungs.

### `BL-123` is finished: the taunt's two halves go different distances

His ruling: *"the aggression ladder is mob only. The actual target change is pvp (+ mob if mobs have
targets though) and charm/fear work on both."* So `ApplyTaunt` splits them — the **lock** reaches
players (their target is pinned to the taunter for its 1.5s, refused in `HandleAttack`), the **aggro
ladder** is paid only into a monster's threat table, because a person has no threat table for it to
mean anything against.

🔑 **That was the fourth `Kind == Player`-shaped gate of its family, and the first that was HALF
right** — deleting the test outright would have paid threat into a table nobody reads. The lesson
generalises: a kind test is usually guarding *one* of the things inside it, and the fix is to ask
which. ⚠ Its sibling: `TauntLockTicks` only ever counted down in `MobAi`, so the moment the lock
reached PvP it would have been **permanent**. A counter only one kind of entity decrements is a trap
the second the other kind can set it.

### Engine work the file needed

- **AoE taunt.** The area path resolved damage and contested CC and knew nothing about a taunt, so
  Mass Taunt would have swept its radius and done nothing to anything in it.
- **Three crit-debuff channels** for the two Shield Smashes (`CritRatePenalty`, `CritDamagePenalty`,
  `MagicCritRatePenalty`; the magic crit-damage quarter already existed for the buffer's blessings).
  🔑 They are **holder-side penalties on the creature**, not resistances on the tank: he cannot
  out-tank a crit aimed at his healer, but he can make the monster worse at critting anyone.
- **Final Defense reads live HP** rather than applying a buff. HP moves every tick and nothing
  recomputes derived stats when it does, so a buff would have needed watchers on the damage, heal,
  regen and potion paths — and whichever was forgotten is where the tank silently keeps 30% at full
  health. A getter cannot be forgotten.
- **Aggravated State needed nothing at all** — `ProcOnDamaged` with self/party rung arrays already
  existed for the Sigils, and his row maps onto them exactly.
- **`SkillDef.SharesLadderKey`** replaces the two-id allowlist added yesterday. `BL-85`'s guard
  refuses two laddered buffs on one family key; that is right for a pair meant to be mutually
  exclusive and wrong for two versions of the same control from different classes (a tank's Stay and
  a healer's Bind must not stack, and the deeper rung should win). The declaration now sits on the
  skill that made the choice, and the guard still fires for anyone who lands on an occupied key
  without saying so.

### Six things in his files that `--check` and the monotonic rule caught

All six are corrected in the CSV as well, so file and game still agree:

- 🔴 **A STRAY QUOTE MADE 19 ROWS INVISIBLE.** Every Shield Shock row across both files ended
  `…x0.7);",15,6400` — a closing quote with no opening one, so the parser swallowed the MP and SP
  columns into the description, the row fell under the column-count test, and `--check` reported the
  skill as "the class learns 4 rungs with nothing authored". A quoting slip does not corrupt a row;
  it *deletes* it.
- 🔴 **Heavy Armor Mastery's 2nd-class P.Def was 20 points high on every rung** — the code carried
  40/47/54/61/70 where his file has said 20/27/34/41/60 for as long as `--check` has walked it. A
  tank was wearing twenty points of defence nobody authored.
- 🟠 **`robe` in the WEIGHT column of Heavy Armor Mastery and Tank Anti-Magic**, on all thirty rows —
  pasted from the healer template. Built HEAVY and (for anti-magic) ungated; every DESCR cell says
  *"with heavy"* and the skill is called Heavy Armor Mastery. This is not the `BL-105` "the column
  wins" case: that is for a column disagreeing with prose about a real choice, not a pasted cell
  contradicting the skill's own name.
- 🔵 **Weapon Mastery dipped at 52** — `+26` between `+31` and `+41`. Straightened to **36**, the
  midpoint his own neighbours describe. A rung you pay 74,000 SP for cannot make you weaker.
- 🟡 **Charm's 2nd-class duration read 1.5s** where its own DESCR and its 3rd-tier rows say 3s —
  Taunt's cell pasted into the Charm block.
- 🟢 **His SP column declares its own units.** `tank 3rd.csv`'s header says `SP COST (x1000)`, because
  writing the `k` each time annoyed him. The checker now reads that from the HEADER — never a
  per-file table in the tool, which would be a second place the truth lives and would silently
  multiply by a thousand the day he adds the `k`s back.

## 2026-09-02 — 0.104.0: `BL-110` fear and charm, and `BL-109` the whisps

His order after the four CSV files: *"now do 110 then 109 then UI"*. Both are built here, in that
order, because the second needs the first — a charming whisp cannot exist until charm does.

⚠ **NEW APK.** The whisps ride their own protocol message (`Whisps`), and a client that does not
listen for it will never draw one. The class table changed too, which needs a new build on its own.

### `BL-110` — FEAR AND CHARM: the two states where the server drives your body

His ruling, from `BL-123`: *"both dont change target like taunt — just act uncontrolably"*. Fear
**runs** you to random points 100-200 away; charm **walks** you toward the caster. Neither re-points
your target — they move the body and lock the hands.

- **Fear kept its bit (41) and changed shape.** It used to mean *"cannot cast or attack, can still
  move"*, which is not a fear, it is a silence — the victim kept full control of his feet. It now
  drives him. Two skills carry it, Terrifying Roar and Witches Scarecrow, and both descriptions were
  corrected; his `nuker 3rd.csv` row says only *"Fear the enemy"*, so no CSV cell was made untrue.
- **Charm is a FIELD, `SkillDef.Charms`** — the `SkillEffect` flag enum has been full since
  `1L << 62`, the same reason CC resistance and MP-cost reduction ride fields. Everything that tests
  a flag on the way to a debuff had to learn about it: `IsDebuff` (or a charm would render in the
  BUFF row and be strippable by a *cancel*), `IsContestedDebuff` (or a charm with no school would
  land on the ~99% fizzle roll instead of the contest), `BossShrugsOff` (or a raid boss would be
  walkable), and the `offensive` test at the cast gate.
- **`Entity.IsControlDriven`** is the new seam. While it holds, the server writes the destination and
  nothing else may: move taps are refused, follow and auto-hunt stand down, and a mob's AI does not
  run at all. That last one matters most — the engaged branch of `MobAi` does the LEASH RESET, which
  would have dragged a feared creature home mid-panic and ended the fear's movement outright.
- **Charm's aggro is unconditional, its control is not** — *"charm can fail the actual debuff (the
  un-charm-movement) but still adds the points"*. The threat is paid on cast, before the contest;
  only the walk rolls. It goes through `AddThreat`, so a charm never FORCES a target change the way
  a taunt does — it puts points on the table and lets them speak.
- **`/buff` can reach a control skill now.** It matched `Category.Buff` only, so there was no way, on
  any character, to put a stun, a fear or a charm on somebody and watch what it does. A third search
  pool (timed debuffs, the CC flags, the charm field) was the only route to testing this at all;
  `/buff @target charm` names the admin as the caster, which is what a charm walks toward.

**Verified by SmokeTest §14, and the guard was run against the broken code first**: with the drive
reverted, "FEAR DRIVES A BODY THAT WAS GIVEN NO ORDERS" reports 0 units moved and fails, which is
what makes it a guard rather than a decoration.

### `BL-109` — WHISPS: a support spirit that rides you and acts on its own

His design, and `whisps_skills.csv` was already authored — nine skills, built row for row.

- **NOT an entity**, on his instruction (*"it can be part of the character game object"*): the same
  reasoning that kept the totem out of `EntityKind`. A whisp is a row in `Entity.Whisps`, so it can
  never be aggroed, hit or looted by a call site somebody forgot to audit.
- **The push-down stack is his**, and its two cases are deliberately not symmetrical: a whisp you do
  not have is pushed on the FRONT and the tail falls off; a whisp you DO have is refreshed WHERE IT
  STANDS. So keeping a whisp never costs you the others, and adding one always costs you your oldest.
- **One slot, raised to two by Whisp Mastery at 60** (`PassiveEffect.WhispSlots`, summed).
- **Twenty minutes, re-summonable only in the last five seconds** — the buff renewal window from
  `BL-112`, refused at the CAST GATE so a mis-tap costs neither MP nor the 30-second reuse.
- **Uninfluenced by master gear**, his rule taken literally: the debuffs contest on a flat
  `WhispCcAtk` at the MASTER'S LEVEL, and the heals are flat powers that skip his heal-power
  passives. A fully-geared tank and a naked one have the same whisp. ⚠ `WhispCcAtk = 40` is the one
  invented number in the build — `whisps_skills.csv` has no attack column — and it is a plain melee
  creature's. It is the figure to move if whisps land too often or too rarely.
- **Their debuffs share the player family's `BuffKey`** — *"whisp debuffs do not stack with the
  player version"* — so the ordinary rank rules resolve them and a whisp can never overwrite a
  healer's work.
- **Conditions, not IG's 8-13s clock** (*"just like normal skills with some conditions and
  cooldown"*): master in combat, the whisp's own 400 range measured from the WHISP, the master's own
  PvP gate, and for the support pair his HP bands — Whisp Heal covers 50-99% on a 20s reuse, Quick
  Heal covers below 50% on a 10s reuse. That is why one whisp carries two skill ids.
- **A whisp never picks its own target.** It helps with the fight its master is already in; pulling
  is a decision that belongs to the player.
- **Lost on death, with the buffs and by the same test** — his own *"if its easier we can make them
  to be saved by angelsProtection"*, taken literally: one rule about death, not two.
- **The six summons and Whisp Mastery** are the `Whisps` block of `tank 3rd.csv`, split by race —
  Human taunt + bind, Elf charm + heal, Demon the two breaks. With one slot until 60 that is the
  largest thing race has ever decided about a class here.
- **He laddered that block from one rung to EIGHT while this was being built**, and the build follows
  it: MP 50 → 100, the taunt/charm aggro 6500 → 12000 (the two whisps share a ladder cell for cell),
  the heal 250 → 740, armor break 10/5% → 30/15%, weapon break 5% → 15%, and two level sets
  (40/46/52/58/62/66/70/74 and 43/49/55/60/64/68/72/74). `--check` is clean on all of it.
  🔴 **Three cells in his file need a word from him and are flagged on `BL-109`**: the race column now
  reads Human on all four of taunt/bind/armor/weapon (built as **Demon** for the breaks, his original
  split, under the typo rule); Charming Whisp's last four comment cells say `uses whisp_provoke`; and
  that file's SP column has no `k`, so `--check` shows 28 yellow SP lines until he adds them —
  deliberately left showing rather than taught away in the tool.
- ⚠ **A STARTUP GUARD MADE ONE DECISION EXPLICIT.** The whisp breaks ladder on the HEALER's buff keys,
  which is `BL-85`'s "two childless multi-level buffs on one key" trap — except here the competition
  IS the feature (*"upgrade-or-fail against a Healer's Armor Break of lower/equal/higher level"*), and
  both of the guard's escapes break his rule: a separate key lets them stack, `FlatRank` pins the
  whisp at rank 1 forever. The guard now carries a two-entry allowance with the cost written into it.
  ⚠ **Nothing else from that file.** It is still open (his `NOT DONE` banner at line 228), and the
  taunt / mass-taunt / intimidate / freeze / stay / charm ladders, the masteries and Defensive Wall
  all wait for the single tank pass.
- **The client draws them as coloured orbs** (`GroundDecals.SetWhisps`), one colour per summon,
  chasing the server's position. A deliberate placeholder: the position is honest and two whisps are
  told apart at a glance, and the art belongs with `BL-93`/`BL-103`.

**Verified by SmokeTest §15** — the push reaches the wire at all (a whisp is invisible without it),
the leash, the re-summon refusal *by its own gate* (the first cut of that check was in fact reading
the 30s cooldown), the one-slot eviction, the mastery's second slot, and the half that actually
matters: **a whisp acts on its own** once its master is fighting, at his target.

### Then the UI, the third thing he asked for — `BL-111`, `BL-117`, `BL-118`

- **`BL-111` — FOUR BUFF BARS, and a number.** *"I cannot see if I have 20 or less buffs to not over
  buff me"*. The split is **duration-shaped, not source-shaped**, and his two worked examples are the
  whole spec: Bow Expertise is a 20-minute self-buff and belongs in NORMAL; the tank's ultimate and
  the warrior's Battle Defence/Presence are 30-120s, do not count against the limit, and go in
  OTHERS. So the first test is "does this occupy one of the 20 slots", and the answer comes from the
  SERVER — off the same predicate that evicts, never re-derived on the client, because whether a
  skill counts is authored per skill and a nearly-right counter is worse than none. Debuffs keep
  their own bar above all four, unhidable, as before. The count is drawn as **`n/20` beside the
  collapse button** and stays visible at every collapse stage, since "is there room for one more
  blessing" is asked precisely when the bar is folded away.

  🔴 **AND ASKING FOR THE NUMBER FOUND A REAL BUG.** A debuff def carries the default
  `BuffRow.Buff` — the Debuff row is a DISPLAY override on the instance — so the cap predicate
  answered TRUE for one. **A poison, a stun or a fear landing on a character at 20 buffs evicted one
  of his blessings** ("Might faded — you can only hold 20 buffs at once", mid-fight) and then took
  the slot itself. Invisible for exactly as long as nobody could see the count. Fixed, and guarded in
  SmokeTest §14: a blessing reports `Counts=true`, a fear reports `Counts=false`.

- **`BL-117` — the `[ORDER]` button**, cycling his five: A-Z → Z-A → rarity (Mythic first) → rarity
  ascending → type (weapons → armor → jewels → consumables, rarity then name inside each). 🔑 **ONE
  setting for every list in the game** — the bag, the vendor's sell list, his buy list, the buyback
  shelf and the warehouse all read it, so the order you pick in your bag is still the order you see
  at the shop. Not persisted: it is a view preference, and it resets to A-Z, which is what every list
  did before the button existed.

- **`BL-118` — class change without the quest**, an admin setting beside the exp rate
  (`RateConfig.FreeClassChange`, a 0/1 field on the tuning panel). *"at x100exp doing quest at 20 is
  kinda annoying"* — a ×1-paced quest chain standing in front of a character who reached 20 in
  minutes, worked around with three admin round-trips per character. 🔑 **It applies to everyone on
  the server, not to admins**, which is the whole point: the character who needs it is an ordinary
  player. ⚠ It waives the ITEMS and the quest, never the level, the race/class fit, or the
  never-the-same-discipline rule — those are what a class change means; the quest is only its gate.
  The NPC window reads the same flag, so the option is offered rather than greyed out.

## 2026-09-02 — 0.103.0: `BL-108`, the four authored 40+ files

His word on the 22 finds of playtest 29 was **bugs first, then the CSVs** — the bugs closed at
0.102.11, so this is the CSVs. Four files, and the biggest of them is a whole class kit:
`buffer 4th.csv` is the **second finished 4th-class discipline in the game**, after the healer's.

### The WARCHANTER, 76-90 — his `buffer 4th.csv`

Sixteen families continue past 74 and four are new. `Skills.Warchanter4th.cs` holds the rung
builders; `ClassSkillTables.Fourth.RegisterWarchanterFourth` holds who learns what and when.

- **Continued**: Anti-Magic (21-35), Armor Mastery (15-29 — and it gains a PERCENT M.Def and an
  MP-cost reduction at this tier), Spell Mastery (19-33), Harmony of Restoration (15-29), both
  toggles (14-21), the three Sound skills (14-28), the three per-race weapon masteries (9-16), and
  the two groups Soul Reinforcement and Arcane and Feral Protection (2-9).
- **Harmony of Protection** gains a sixth rung (bow resistance); **Harmony of the Wizard** gains
  three (MP regen, then magic crit rate, then magic crit damage) — its 3rd-class "and it STOPS" was
  always a statement about the 3rd tier.
- **New**: `buffer_shield_mastery` (robe **and** shield — only the Human buffer can satisfy it),
  `wc_harmony_soul`, `wc_harmony_madness` — which is finally the home the retired Madness never had —
  and `wc_harmony_mark`, the party-wide Mark, on the healer's own buff key so an ally wears one Mark
  and never two.

### The other three files

- **`buffer 3rd.csv`** — `doctor_blunt_mastery`, a new eight-rung Human ladder at 40-74. The third
  buffer race finally has a weapon line: Elf bow, Demon two-handed blunt, Human **one-handed** blunt,
  which is what leaves his shield hand free. ⚠ His DESCR cell says "2h Blunt" on all eight rows and
  his WEAPON column says `blunt/1`; the column wins, and it is flagged back to him.
- **`healer 4th.csv`** — the three Marks gain a **second rung at 83**, MP falls 300 → **150** on all
  of them, and the resist numbers move (SPT +10→+15, CON +5→+10, melee vamp +3→+5%).
- **`shared 4th.csv`** — Magic Proficiency gains a **10% proc**.

### Three engine additions the rows needed

- **A THIRD PROC TRIGGER.** Magic Proficiency is *"With 10% chance when using Magic(spells/buffs/
  debuffs/heals)"*, and every proc in the game until now rolled on damage DEALT or damage TAKEN. A
  party heal and a buff touch neither, so a healer would have owned a 10% proc that could not fire.
  `SkillDef.ProcOnMagicCast`, rolled once per committed cast past every gate.
- **MAGIC CRIT RATE RECEIVED.** The Marks' *"M.Crit.Rate.Received -10%"* had no home: the physical
  half is a SkillEffect bit, the magic half did not exist. It rides as a field, the enum being full.
- **PER-CHANNEL SKILL REUSE.** Harmony of the Soul is *"−10% Magical Reuse, −20% Physical Reuse"* on
  ONE buff, which the single `BuffCooldown` number cannot say. The twin of the MP-cost pair.

### 🔴 A group buff was dropping every payload that is not a SkillEffect bit

Found on the way past, and it is not small: `ApplyBuff` folded only the children's `Effect` and
`Magnitudes` into a group. Half the buff payloads in the game are FIELDS instead — the enum has been
full since `1L << 62` — so **Arcane and Feral Protection granted nothing at all** (both its children
are pure CC-resist fields) and **Soul Reinforcement silently lost its whole −20%/−10% MP-cost third**.
Nothing on screen said so: the buff landed, its icon appeared, and the numbers were zero.

### Four authoring slips, corrected and flagged

Per the standing monotonic rule; the CSVs were edited to match, so the file and the game still agree.

| what | his rows | built as |
| --- | --- | --- |
| Harmony of Restoration | 110 / **100** / 120 / **100** / 130 … — every odd rung an untouched copy of the level-74 row | 110 → 180 in fives |
| Harmony of the Wizard | two rows at level **78** | the second is 79 (its price cells are the 79 band) |
| Sound Burst | a second, identical level-90 row inside the Sound Smash block | removed |
| the two blunt masteries | WEAPON column left blank at this tier | `blunt/1` / `blunt/2` carried forward from the 3rd |

Two cells were merely EMPTY and were filled from the row's own siblings: Magic Proficiency's
reuse/duration pair (0/0 against its two neighbours' 30/10, on a row that is nothing but a proc), and
the AoE radius of the three new harmonies (blank, where every other harmony says 800).

And two he ruled on in the same session, both now in the files and the build:

- ✅ **Spell Mastery 76-90 is priced on the tier's ladder** (6.5kk / 11kk / 16kk / 80kk, then gold
  only, 5kk → 100kk). Its fifteen rows had been pasted out of `buffer 3rd.csv` carrying that file's
  36k … 880k SP and its `[]` in the gold cell — which priced a level-90 rung of the buffer's core
  caster passive at a 3rd-class **880k SP and no gold at all**.
- ✅ **`doctor_blunt_mastery`'s eight DESCR cells read "Blunt:"** where they said "2h Blunt:", matching
  his own 76-90 rows. The hands stay in the WEAPON column (`blunt/1`), which is the `BL-105` rule
  working: a requirement written in prose cannot be compared to the one the engine enforces, and the
  two had been saying opposite things.

`--check` clean on all four files (and `buffer 4th` earns its `Check.Specs` line), zero ladder dips,
`--learn-audit` clean on all 69 class/tier combinations, server boots 0.103.0, Unity type-check clean,
protocol unchanged. 🔑 **A class-skill-table change needs a NEW APK.**

## 2026-09-02 — 0.102.11: the last five finds of playtest 29

`BL-114` plus the four remaining pure bugs. Three of the five had been broken since the day they
shipped and could not have been caught by playing more carefully — each is a rule that was *stated*
somewhere and then asked in the wrong place.

### `BL-114` — the sell divisor is PER RARITY

*"the sell prices are to much for the current drop rates … common sels for 0.225 of the original
price so selling 4.(4) items Is like I sold a real item"*.

His ladder, and it divides the **MYTHIC rung** of the price table rather than the item's own buy
price — which is the units his own arithmetic is in, and the only reading that reproduces all six of
his numbers (Mythic 1/10 is what it already was, which is why he did not ask for it to move):

| rarity | divisor | sell ÷ Mythic price | was | ÷ its OWN buy price |
| --- | --- | --- | --- | --- |
| Mythic | /10 | 0.100 | 0.1000 — unchanged | /10 |
| Legendary | /25 | 0.040 | 0.0425 | /10.6 |
| Epic | /33 | 0.030 | 0.0350 | /11.6 |
| Rare | /50 | 0.020 | 0.0350 | /17.5 |
| Uncommon | /100 | 0.010 | 0.0275 | /27.5 |
| Common | /200 | 0.005 | 0.0225 | /45 |

One place (`ItemCatalog.SellPrice`), off a new `TieredGearBasePrice` — the row cell before
`RarityPriceMul`. It also **separates Epic from Rare**, which sold identically before (they share a
buy multiplier of 0.35 on purpose). Use-consumables keep the flat /10: a buff potion has no Mythic
rung to be a fraction of.

📊 **Measured, not derived.** The Common gauntlet goes buy 112,500 / sell 11,250 → **2,500**: 45
sales to buy its own replacement, up from 10. On the playtest-18 level-34 farm the effective divisor
is **/35.5** against /10 and that farm's total income falls **~1.04M → ~549k**. 🔴 His playtest-18
target for it was ~1kk, so this lands at **half the number he accepted a month ago** — the cut he
asked for, but bigger than "4.4 → 20" sounds, because the ladder bites the Common/Uncommon end and
that is nearly everything that drops. The other knob (the gear DROP rate) is untouched and is one
number if he wants the total back.

⚠ `BalanceMatrix`'s sweep knob is a single number and the ladder is six, so its live baseline is now
a drop-WEIGHTED effective divisor, measured on the same drops `pk.Gear` is measured on. Its rows are
divisors on the item's OWN price — reading his 200 straight into that column would have overstated
the cut 4.4× — and the `0.075 / 10` row reads 1.04M again, matching the number that section was
calibrated against.

### The NPC buffer's `[Save]` was never once alive

*"npc buffer the save button is inactive …I have buffs and it's still inactive"*.

🔑 **Every blessing is a ONE-CHILD WRAPPER**, so `ApplyBuff` recurses into the child and stores
`SkillId` = the FAMILY RUNG (`BuffPAtk3`), keeping the wrapper only in `SourceSkillId`.
`SavableBuffIds` filtered `NewbieBuffSet.Contains(b.SkillId)` against a set of `npc_*` ids, so the
intersection was **always empty** and `SavableNow` was always 0 — dead since `BL-95` shipped in
0.99.0. The original comment even stated the mechanism correctly (*"resolves to its FAMILY rung,
never npc_might"*); it just did not notice that this is true of the NPC's own blessings too, not only
of potions. It reads `SourceSkillId` now, and both of his worked examples still fall out for free.

🔑 **The general lesson: when a wrapper and its child both carry an id, name which question you are
asking.** "What is on me" is the child; "what did the player press" is the source, and every preset
question is the second one.

⤷ **Found on the way past and fixed with it: you could pay for a blessing that never landed.** A
single or a preset charged first and applied afterwards, ignoring the result — so a buffer standing
at the NPC with his own stronger Might paid full price for nothing. Both paths ask `BuffWouldLand`
first now (the same resolver the cast path uses), a preset is charged **for what lands** and says how
many it skipped, and a single that cannot land refuses instead of taking the gold.

### Phase Shift moved you on the server and nowhere on screen

*"phase shift don't visually update my position. The mob attacks where I must be server wise but
client sees the nit updated position."*

🔑 **A JUMP IS A DIFFERENT EVENT FROM A WALK AND THE CLIENT CANNOT INFER WHICH IT GOT.** It guessed by
DISTANCE: over 5 Unity units (500 server) is a teleport, and a self-prediction in flight tolerates
2.5 units of server disagreement before it snaps. A rung-1 Phase Shift is **200 server units — two
Unity units — so it fits under BOTH thresholds**. The server moved him 200, every mob followed, and
the client went on drawing the walk it was predicting.

`EntityDto`/`EntityLean` carry a one-byte **`Warp` counter** now, bumped by `PlaceEntity` — the
single seam every non-walk reposition goes through. The client snaps whenever the number changes.
Four teleports that had hand-rolled `PlaceEntity` inline (respawn, the gatekeeper, jail release, the
admin jump) were routed through it, so blink, knockback, respawn, teleport and anything added later
all get it from one line. Protocol **30 → 31**; optional with a default, so an old client is
unaffected — but a new APK is needed to SEE it.

### The buff bar kept a stale snapshot after a reconnect

*"after long break when reccinect my buff bar stays with the last snapshot.. But the buffs aren't
there"* — and the damaging half, *"if I'm buffed with group buffs and they are 'fake' it looks like I
disaseble the group buff and can overbuff it with singles"*.

`PushBuffs` sends an EMPTY bar only once, when the last row goes away, and `_hadBuffs` records what
the **server** last sent — not what any particular client HOLDS. Those come apart the moment someone
is link-dead or offline-farming: the buffs expire while he is away, the one empty update goes into a
dead socket, the id leaves the set, and on reconnect the rule says "already told them" and never
speaks again.

The bar is pushed **unconditionally on arrival** now — the same rule, and the same one line, as the
empty party roster sitting ten lines below it: *state the client needs on arrival must be pushed on
arrival*. 🟢 **SmokeTest §13 guards it, and it was run against the broken code first**: with the push
removed the section fails with "no Buffs push arrived", which is the only reason to trust it.

### The grey rectangle at full ortho zoom-out is the EDGE OF THE WORLD

*"now in ortho if I zoom out to much it become the gray rectangle clip … now it just covers the whole
screen if I don't zoomin to much"*.

Not a clip plane this time, and not the same bug as the growing bottom band. The world is 24,000
server units = **240 Unity units** across. An ortho camera at `MaxOrthoSize` 60 shows `2 · 60 ·
aspect` horizontally — on a 19.5:9 phone in landscape that is **260**. At the end of the zoom slider
**the view is wider than the map**, the ground plane genuinely runs out, and every pixel it does not
cover is the camera's clear colour: Unity's default blue-grey, alpha 0, no skybox assigned.
Vertically it is `2 · 60 / sin(Pitch)` = 170 of the 240, so standing near an edge puts most of the
frame outside the world.

🔑 **The previous grey band WAS the near clip plane and that fix still holds** — at the sliders' own
limits (Pitch 45-90, OrthoSize 6-60, Distance 10-90) the near edge of the ground sits at Distance ≥
10 against a 0.3 plane and the far edge at ≤ 210 against 1000. Neither plane is reachable any more.
Two different bugs wearing the same grey; the second only became visible once the first stopped
hiding it.

The fix is to stop rendering a HOLE: the camera clears to the ground's own colour, so "beyond the
map" reads as more ground. That also covers standing at the world edge at ANY zoom, which was already
true before he touched the slider. The grid still stops at the real boundary, so the edge is still
legible. **The zoom range is deliberately not reduced** — he asked for that zoom.

⚠ **This one is reasoned, not observed.** It is the only find of the five I could not drive myself:
it needs the device. If a grey rectangle survives the change, it is something else, and the
arithmetic above is what to rule out next.

## 2026-09-02 — 0.102.10: a taunt does not put you on top for free


`BL-123`. *"taunt (and charm also adds aggro points) but they donnt mve you on top for free .. the
idea is tank to spam taunt/charm for mob to keep it agrro on him .. if some1 is doing alot of
dmg/heals the tank will ahve hard time to keep it up so the one must slow down so tank can take 1st
place"*.

The taunt did `Math.Max(mine, top) + power` — top of the threat table for free, then the power on top
of that. It is a **plain add** now. The target LOCK is untouched and is the taunt's only guarantee.

The old shape made aggro something the tank *owned* rather than something he keeps earning, and it
made the whole threat economy — damage at 1:1, `ThreatHealFactor`, `ThreatBuffPerLevel` — decorative
for as long as a tank had a taunt off cooldown.

**Measured before the jump came out, so his ladder is not re-tuned by guess.** Provoke is 4,500-6,000
on a 6s reuse = **750-1,000 threat/s**. Against it: a level-28-36 attacker does **~250-300 dps**
(BalanceMatrix E4 — 2.6-3.5s TTK against a 667-1,077 HP mob, and threat is damage 1:1), and a cleric
spamming Quick Heal generates **~750/s** (301 power / 2s cast × `ThreatHealFactor` 10). So the tank
leads on his taunt alone, is level with a flat-out healer, and is genuinely pulled off by a healer
plus a committed nuker. That is his *"the one must slow down"*, and his 4,500 start needs no change.

🔑 **His charm rule is the asymmetry that makes this hold together:** *"charm can fail the actual
debuff (the un-charm-movement) but still adds the points"*. The threat add lands on cast; only the
walk-toward-caster rolls against the land rate. That is the reverse of every other debuff here, and
it is also why the taunt's lock can afford to be short — the guaranteed half is the lock.

⚠ **Not chased, deliberately: his tank CSVs are open on his desk** (*"do not touch tanks csvs untill
im done with them"*). `--check` already reads **duration 1.5 and range 400** out of his in-flight
`tank 2nd.csv` against the code's 3s / 600 — his *"ill lower it to 1.5s"*, plus a range cut he has not
mentioned — and `tank 3rd.csv` carries 15 unregistered rungs of Taunt, Mass Taunt, Intimidate and Tank
Anti-Magic. The whole tank delta goes in as one pass when he says he is done, descriptions included
(the four Taunt rungs still read "for 3s").

## 2026-09-02 — 0.102.9: his six base move speeds, and no DEX term

`BL-122`. `SpeedTable.BaseRunSpeed` was 130-165, invented. It is now his, verbatim:

| | fighter | mage |
|---|---|---|
| **Elf** | 143 | 114 |
| **Human** | 115 | 109 |
| **Demon** | 112 | 113 |

🔑 **I had read his *"speed should be around 180 for slow and 210 for faster classes"* as the base
table and proposed 180-210 — wrong by ~65 points.** It describes where a party-buffed player *lands*.
The buff stack is **+61** (Swift / Wind Grace +33, Harmony of Speed +20, Frenzy +8), so his table
gives Human fighter 115+61 = **176** ≈ "180 for slow", Elf fighter 143+61 = **204** ≈ "210 for faster
classes". A rogue's own +60 from sprint and passives then clears the 250 cap — his *"they usually max
it out"*. The lesson is the general one: **a target figure he quotes is the figure at the table, not
the number in the table.**

**No DEX term, and there never was one.** *"IG is base class+race speed x dex mod but i dont want dex
to affect speed (rogues have enough passives so their ms to rise even more)"*. Nothing in the codebase
has ever multiplied move speed by DEX, so this is a rule to keep rather than a change to make — it is
now written into `SpeedTable` so nobody restores the IG modifier while porting a formula from a
reference table that carries it.

Two shapes in his table are deliberate and are commented as such, so a later pass does not "fix" them:
the **Demon mage (113) is a point faster than the Demon fighter (112)**, and the **Elf fighter's 143
is a 28-point outlier** rather than the top of a smooth ladder. (The monotonic-ladder rule is about a
ladder of rungs, not about race rows.)

⚠ **CONSEQUENCE, FLAGGED NOT FIXED: unbuffed, the player is now slower than the average creature.**
Mob run speeds are 90-155 with the mode at **132** and a long tail at 140-155. Against the old
130-165 table most classes could walk away from most things; against this one only the Elf fighter
(143) beats a median mob, and nothing beats the fastest. Fully buffed (170-204) everyone clears the
field again. So kiting — which he ruled is deliberately how a low-defence mage or archer farms — is
now **buff-dependent** rather than free. That interaction is his call, not a bug; `BL-122` records it.

Server-only, no new APK: move speed reaches the client as `EffectiveSpeed` on the wire, not as a
compiled-in constant.

## 2026-09-02 — 0.102.8: the creature that gave up now really leaves

`BL-116`, the bow-kite exploit: *"I stand just outside the radius and shoot it with a bow .. The mob
agros me back then stops and it moves towards/away from me and if I do more than it's 5% regen I can
kill it"*.

### The number in that sentence was wrong by a factor of fifty

`AddThreat` set `mob.Engaged = true` unconditionally, on every hit. The regen tick reads that **same
flag** to choose its rate — `MobHpRegenPerSecond(maxHp, engaged)`, **0.1%/s engaged vs 5%/s idle**. So
the arrow that re-aggroed a leashed creature also pinned it to the combat rate, and the 5% ramp he
believed he was out-damaging never applied to a kited mob at all.

The same line explains the shape he described. A mob past the 1500 leash calls `ResetMob`, turns for
home — and the next arrow re-engages it, so it walks back out over the boundary, `ResetMob` fires
again, and it oscillates on the rim. It is *permanently* inside bow range and never once arrives. That
is the *"moves towards/away from me"* verbatim, and it is why nothing about the leash distance or the
return speed could have fixed this on its own.

### His ruling, and which half of it does the work

He rejected all three options offered, correctly and for one reason: a full heal on leash and damage
immunity while returning **both make the 5%/s regen ramp dead code**, and extending the chase range
has no natural stopping point. His own fourth: *"when a mob reaches the end of leash it start to
sprint back to start .. not just walk .. like +100ms then when reached start it reset the ability to
chace again"*.

`Entity.ReturningHome` — set by `ResetMob`, cleared within `MobHomeArrivalRange` (60) of home. While
it is set the creature:

- runs at `RunSpeed + GameConstants.MobLeashSprintBonus` (+100), so 210-230 against 110-130 — faster
  than an unbuffed player and slower than a speed-buffed one. Not clamped to `MoveSpeedCap`: 250 is a
  ceiling on what a *player* may reach.
- does not wander (the wander roll would otherwise steer it elsewhere within 3-12s) and does not scan
  for aggro, so a player standing on the path is walked straight past;
- 🔑 **takes no threat.** `AddThreat` returns early. Damage still lands and still kills it; nothing
  re-targets or re-engages it. **This is the fix — the sprint is the trim.** Leave the early return
  out and the sprint is decoration that dies on its first tick.

Being un-Engaged also puts it on the **5%/s idle ramp for the whole return**, which is his HP ruling
with no code of its own: *"their hp keep the 5% untill when reached home and regen until some1
reengages ... when full and no1 reengaged then they reset aggro and etc"*. It climbs on arrival too,
so a wounded creature can still be re-pulled at its post; `MobRecoveryCheck` closes the pull at the
top of the bar exactly as before.

⚠ **Kiting is untouched, deliberately.** *"kiting is a way mage/archer with low defence to be able to
farm actively ... monster speed is irelevant.. its only how many seconds u have before it cut the 900
range .. so mages get 1~3 free spells"*. What was broken was not that a kite works, but that a
creature which had already quit could be farmed forever from outside its reach.

Server-only — no new APK. His player base-speed raise (180/210) is `BL-122`, pending six numbers.

## 2026-09-02 — 0.102.7: a grant that never landed, the watch's dead gate, two proc chances

Three more of his playtest-29 findings, all server-side.

### 1. A NEWLY CREATED CHARACTER HAD NONE OF HIS AUTO-GRANTED PASSIVES

His find: *"newly created mage (lvl 1) have his first spell cast without penalty of
weapon_proficiency .. No cast reduction no nothing ..almost oneshoted a pig being naked ... Then i
become lvl 5 (x100 exp) and I did 1dmg with ~11s cast so the passive recalculate the stats."*

`AutoLearnCoreSkills` writes `LearnedSkills` — Spellcaster Mastery, the class floor passive, the
reflect passive, Novice's Grace, Angel's Protection, the robe-rung clamp — and **never recomputed**.
On the login path the only `RecomputeDerived` (in `PersistenceService`, right after the bag loads)
runs **before** it. So a character whose saved row did not already carry those ids — every brand-new
one — walked into the world with the ids in his skill window and not one of their effects on his
numbers: no untrained-weapon cast penalty, no ×25 fizzle multiplier, no floor passive. The first
level-up or equip fixed it, which is why it read as the passive arriving late rather than as the
grant never having happened.

The recompute is now part of the grant, at the end of `AutoLearnCoreSkills` rather than at its ten
call sites — every one of them has the same obligation. Login re-fills HP/MP afterwards, because the
pools `PersistenceService` topped up were measured against the *pre-passive* maximum.

⚠ It was never only the mage. Every class lost its floor passive on a fresh character; the mage was
merely the one whose loss is visible in a single cast.

### 2. THE WATCH'S PVP GATE WAS WRITTEN AND UNREACHABLE — `BL-115`

His find: *"field guards/watchmen are targetable and hittable even without a pvp-on ...and i can hit
them auto in auto-farm ...they shouldn't act as mob"*.

`BL-79` put the rule in `CanPvpHit` — *"a player attacking them (pvp-on must be on)"* — and both
callers asked it as `target.Kind == EntityKind.Player && !CanPvpHit(...)`. A guard is a **mob**, so
no caller ever arrived with a target the clause could answer. The rule was stated and never enforced.
Both single-target paths (basic attack, offensive skill) now delegate unconditionally; an ordinary
mob still answers `true`, so PvE is untouched.

**And the auto-farm half is a separate answer, not the same one.** A guard is now excluded from
target *acquisition* outright, not merely gated: the PvP toggle alone would still let a PK's
autopilot walk into a guard tower. Attacking the watch is a deliberate act, and a deliberate act is
what an autopilot cannot make. Defence is untouched — if the watch is already on you, the autopilot
may answer it, behind the same toggle.

**NPCs get his two properties.** `NpcDef.CanDie` and `NpcDef.Retaliate`, both **false** by default,
and false on every NPC the world places today:
- every NPC is attackable **only with PvP on** — this REPLACES the flat *"you can't attack that"*
  refusal of 2026-07-21, which was the right shape for the bug it fixed (a client killing a vendor)
  and the wrong shape for the dummy he wants to practise on;
- `canDie: false` — *"hp can't go below 1"*, the pool untouched, the bar honest, the floor at one;
- `retaliate: false` — *"don't strike back just sit and take it"*.

⚠ **The guards are NOT `NpcDef`s and were not rebuilt as any.** `BL-79`'s watch are mobs with the mob
AI, a real class kit, real gear and a respawn timer, and they are already the true/true pair by
construction — they die and they hit back. What they were missing is the gate above.

### 3. COMBO MASTERY: TWO CHANCES, ONE PROC — `BL-120`

*"3% chance with `blunt/1` and 3.45% chance with `bow|blunt/2`"*, his reason being the whole design:
*"2h weapons are slower by ~12/18%, so increasing the chance balances the slower attack speed"*. A
proc is rolled per **landed hit**, so a slower weapon fires it less often for the same authored
number. `SkillDef.ProcChanceTwoHanded` (0 = no split, which is every other proc) is what buys it
back; 3.45/3.00 = ×1.15, the middle of his own 12-18%. Bow and Dual are inherently two-handed, so a
bow takes the higher branch — which is what he asked for by name. The gate is unchanged: blunt or
bow, any hands. Both `buffer 3rd.csv` rows moved with it.

## 2026-09-02 — 0.102.6: the rung you can actually buy

His playtest-29 find: *"for some reason 'harmonist' don't learn serenity, vigor, vamp? Why are they in
the csv... Are there other like that? — force, insight"*.

**There were. Twelve skills, across fifteen classes, and the cause was one `+ 1`.**

### The bug

Both halves of the game computed your next purchase the same way — `GameUi.Skills.BuildLearnTab` on
the client, `GameLoopService.HandleLearnSkill` on the server — as **the rung you own plus one**, and
then asked the class table for exactly that rung. A class table is a **shelf**, not a staircase, and it
is authored two ways that `+ 1` cannot read. Both are deliberate:

- **A ladder may START above rung 1.** The single-buff ladders are shared with the CONSUMABLES: rung 1
  of Force / Ward / Aim is the **potion**, so the cleric's first authored row is rung 2. `owned + 1`
  asked for a rung no class stocks, missed, and reported *"your class cannot learn this"* — about a
  skill sitting on his CSV, on the class table, at a level he had passed.
- **A ladder may SKIP rungs as it climbs.** The Warchanter takes Serenity **2 → 4 → 6** and Insight
  **3 → 6**; the rungs between belong to other classes. The old rule stalled at the first hole and
  called the rest *"cannot be raised further by your class"*.

The twelve: **Serenity, Vigor, Vampirism, Force, Insight** (his five), plus **Ward, Focus, Ferocity,
Fury, Agility, Swift** and **Resolve**. The healer line lost rungs to it as well, not only the buffer.

🔑 **Nothing in the data was wrong** — no CSV moved. The engine now reads the shelf:
`ClassSkills.NextLearnableLevel` returns the LOWEST class-table rung strictly above what you own, and
both the tab and the handler ask it. Everything else about a purchase is unchanged: the level gate, the
SP and gold price, the exclusive groups, the supersede check.

### Why no playtest ever caught it

**`DebugLearnAll` assigns the highest rung directly** and never walks the ladder — so every admin
character in every previous run had all five buffs. Only a character *buying* them one at a time could
see it, which is exactly the shape of bug the smoke test exists for.

### The two regressions that now guard it

- **`dotnet run --project tools/SkillCsvSeed -- --learn-audit`** — walks all **69 real class/tier
  combinations** (base, 2nd, each discipline that race actually opens into, and its 4th tier) and
  asserts every authored rung is reachable. `--learn-audit --old` replays the broken rule and prints
  the twelve. ⚠ It unions the tiers a character CLIMBED THROUGH: `ClassSkills.Cumulative` starts at the
  2nd-class list, so auditing a discipline against it alone reports every continued ladder as broken —
  24 classes' worth of Anti-Magic that is in fact perfectly fine.
- **SmokeTest §11** — a real Elf Harmonist buys Serenity over the wire and lands on **2, then 4, then
  6**, and the other four land on their shelf's first rung. Every check in the suite passed.

⚠ **This needs a NEW APK.** The Learn tab is built on the client, so the server half alone will not
make the rows appear. `ProtocolVersion` is unchanged (30) — nothing on the wire moved.

## 2026-09-02 — 0.102.5: the rebuff window is 5s, a stronger rung rebuffs, and stances stop flickering

Three findings, one loop — the auto-buff chain.

### 1. The renewal window: 60s → 5s (`BL-112`)

*"buffs should rebuff when they are wearing off ... They should buff when the time is 5s"*, and
*"conceal rebuff with 15s remaining and thats twice the mana/s consumption .. For a 30s buff"*.

**This replaces his own earlier "below 60s", and the arithmetic is why.** The window threw away 60s of
every blessing — 5% of a 20-minute buff, but it was capped at half the duration, which made it
**15s of a 30-second Conceal**. He was paying for two Conceals to get one, exactly as he measured.

⚠ **The cast has to fit inside the window** or the renewal lands after the gap it was meant to close.
His sentence assumes casts are 1-5s, so at exactly 5s a 5s cast is a coin flip: `RefreshWindow` takes
the longer of his five seconds and the skill's own cast plus a second of slack. For every buff he was
describing the answer is simply 5s.

### 2. A stronger rung is a rebuff trigger (`BL-112`, second half)

*"if I have a buff L1 and I buff myself with it ...then after lvling up I learn L2 ..it should rebuf me
..because it's stronger"*.

**`Rank` could not see this.** It is ONE number per `SkillDef`, shared by every rung of a ladder — so
rung 1 and rung 6 of Might compare EQUAL, and the chain called the weaker one "covered" for its whole
duration. The rung lives on `BuffInstance.Level`, which was already being kept so a buff can be rebuilt
on login, and was simply never consulted. ⚠ The level test is restricted to the **same skill id**:
levels are only comparable inside one ladder, and a group's level is not on the singles' scale at all.

### 3. Stances stop flickering (his toggle find)

*"toggles with auto on toggle on and off indefenetely fast ... Once my mp depleates they try to be
toggled and etc...."*

**The loop is exact.** The stance drops the tick MP falls below **one second** of upkeep; regen puts a
few points back inside that same second; the chain re-lights it; it starves again. ~1 Hz, and it spends
a cast every time. Two halves, because either alone only slows it down:

- a starved stance is **locked out of the autopilot for 10s**, stamped onto `AutoReadyTick` — the
  chain's own per-skill gate, so no new state and no future auto path can forget it;
- the chain will not arm a stance it cannot **sustain** — it now needs **10 seconds** of upkeep in the
  bar, not the one second the generic MP gate asks for, which is precisely the level the stance dies at.

**Pressing a toggle by hand is untouched.** This is the autopilot's clock, and his complaint is about
the autopilot.

### 4. Harmony of Restoration is a healing buff, not a heal (`BL-113`)

*"the logic is for all skills that leave a buff .. the skill cannot be reexecuted even after the
cooldown is done while the same skill is already active"* — and his model for it: *"it's like a hp pot
— I cannot use another pot while the last is active"*.

Nothing was wrong with the threshold. What was missing is that **being below the threshold stays true
for the whole time the regeneration is working**, so a short reuse re-bought the same buff every cycle —
400+ MP for +5/s, on cooldown, until the bar was empty. `OwnBuffStillRunningOn` now takes a heal or
MP-heal target out of the running while that skill's own buff is on them.

🔑 **The low cooldown is NOT the thing to fix**, and he said why: *"the low cd is made so once we have a
debuff that increase the cooldown of skills x2 still hor to be permanent .. while a healing totem with
cd of 25 x2 becomes 50 and duration 30"*. It is deliberate armour against a future cooldown debuff.

⚠ **Heal and MP-heal only, and the other kinds are excluded because they already have a better test** —
a `Buff` must be allowed to re-fire inside the new 5-second renewal window, and a `Debuff` already runs
this exact rule with a zero window. ⚠ **A manual press is still allowed.** His *"for all skills"* may
well mean the tap too, but refusing a hand-cast rebuff before a boss pull is a big behaviour change to
infer — say the word and it becomes a hard gate.

## 2026-09-02 — 0.102.4: a buff whose weapon gate is shut now pays nothing

His find: *"bow expertise should work with only a bow (now if I activate it with a bow and then change
to other weapon the 12% stay active)"*.

**`RequiredWeapon` had only ever been a CAST-TIME gate.** `HandleSkill` refuses the cast if you are not
holding the right weapon, and nothing ever re-asked the question afterwards — so the buff outlived the
bow and Bow Expertise's own description, *"+12% attack speed while wielding a bow"*, was simply untrue.
It is not one skill's bug: every `RequiredWeapon` buff in the game had it, and the shared-4th
proficiencies he just authored would have inherited it.

### Suppressed, not removed

Bow Expertise runs **twenty minutes**. Deleting it because you drew a dagger for one pull would cost
the whole duration and teach the player never to swap weapons. So a gated-off buff keeps its clock and
its bar slot and simply stops contributing — put the bow back and it works again, free.

⚠ It still occupies one of the 20 buff slots and still burns its timer. Holding a blessing you have
switched off is a choice, not a free slot.

### One seam, not a dozen loops

`BuffInstance.Suppressed` gates the three accessors — `Has`, `Percent`, `Flat` — that **every** reader
of a buff's numbers already goes through: a dozen aggregation loops in `RecomputeDerived` plus the
`IsStunned`/`IsRooted`/`IsFeared` family. Gating them covers all of it and, more importantly, cannot be
forgotten by the next buff that needs a gate.

The single writer is `Entity.RefreshBuffSuppression`, called from `RecomputeDerived` immediately after
the equip loop has resolved `WeaponType` and before the first line in that method that reads a buff
magnitude. It asks the same `Satisfies(RequiredWeapon, RequiredHands)` the cast gate asks, so the two
axes can never drift apart. Mobs and player-built creatures go through it too — deliberately: `BL-79`
was three bugs caused by asking "is this a player?" where the question was "does it run player stats?".

### The bar has to show it

A lit icon granting nothing looks exactly like the bug it fixes, so `BuffDto` gained an optional
`Suppressed` and the square draws **dimmed** (label at 40% alpha, the dim tint beating even the
under-a-minute expiry blink — a buff paying nothing has no expiry worth warning about). The detail card
says it in words. A collapsed GROUP dims only when **every** part of it is gated off.

`ProtocolVersion` 29 → **30**. The field is optional with a default, so an old client deserializes fine
and simply never dims — which is the state it is in today. **The server-side fix works either way; the
new APK is what lets you SEE it.**

## 2026-09-02 — 0.102.3: the buffer's armour ladder gets its own id, and the double cancel dies

`BL-119`, his find: *"I managed to make x4 cast speed with light amror ...I'm 40lvl harmonist with
35lvl armor_mastery and wc_harmonist_light_mastery both remove the light penalty"*.

**He is right, and the mechanism is worth writing down because nothing about it is visible from either
skill.** The cleric's Armor Mastery rung 4 (bought at 35) carries a LIGHT row authored to *cancel*
Spellcaster Mastery's ×0.5 caster penalty — `CastSpeedPct 0.90` is ×1.90 against ×0.50, landing on his
"−5% from a robe". At 40 the Elf Warchanter learns Harmonist Light Mastery, which cancels **the same
penalty again** (×1.80). Armour masteries stack MULTIPLICATIVELY (`Entity.ApplyArmorMastery` — that is
deliberate and load-bearing for the cancel arithmetic), so both cancels applied.

**Why it was invisible:** the 40-74 rungs of `armor_mastery` carry no speed clause at all, precisely so
this cannot happen — the comment on them has said *"leaving a copy here would apply it TWICE"* since
August. But they were rungs **5-18 of the same skill**, and a level-40 who had not yet SPENT the SP on
rung 5 still held rung 4. The guard was authored on the rung, and the character was on the rung below.

### The fix is his own, and it is structural

- **`buffer_armor_mastery` is a new skill id** carrying the fourteen 40-74 rungs. `armor_mastery` stops
  at four — the cleric's, 20-35.
- **It `Replaces` `armor_mastery`** (and `mastery_robe`, which `armor_mastery` had replaced and which
  must not resurface underneath it).
- **Both race masteries `Replaces` `armor_mastery` too** — `wc_chanter_heavy_mastery` and
  `wc_harmonist_light_mastery`. This is the half that closes the window: the moment a buffer takes his
  race's mastery, the cleric's speed clause is superseded whether or not he has bought the 40 rung.
  They do **not** replace `buffer_armor_mastery`, which carries no speed clause and is additive by
  design.
- 🔑 **A SPLIT NEEDS A SAVE MIGRATION where a DELETE does not** (`BL-106`). A retired id dies on load
  because `SkillCatalog.Get` returns null; a *shortened* one does not — a Warchanter holding
  `armor_mastery:9` would have pointed at a rung that no longer exists and silently lost the whole
  40-74 ladder, with the skill window still showing him an Armor Mastery. `ParseLearnedSkills` now
  carries the level across (rung 5 → 1, rung 18 → 14) at the one seam where stored text becomes runtime
  state, and the next autosave writes the new pair back.
- 🔑 **The name is the one `BL-106`'s cross-chain rule already predicted**: the mage side reads
  `mage_* → spellcaster_* → buffer_*`.

**CSVs move with it** — the fourteen Armor Mastery rows in `buffer 3rd.csv` and the fifteen in
`buffer 4th.csv` now carry `buffer_armor_mastery` in `SKILL_ID`; `cleric 2nd.csv` keeps `armor_mastery`
for its four. `--check` reads `buffer 3rd.csv` clean apart from his newly-authored Doctor Weapon
Mastery, which is `BL-108`.

## 2026-08-29 — 0.102.2: the player-animation path, ready for the file that is missing

`BL-102` has been "I need one file from you" since 0.100.2. This builds everything on **this** side of
that file, so the file is now the only step left: drop clips into a folder, run one command, and the
character animates.

**The finding it answers, restated because it is not obvious:** all 21 FBXs under `Models/Characters/`
have mesh, skeleton, bind pose and 65 bones — and `AnimationStack` count **0**. The monster pack ships
five takes per creature (which is why the rats and spiders already walk, swing and die); the character
pack ships none. `humanoid.prefab` is a body with nothing to play.

### What changed

- **`ModelSetup` grew its character half.** `BuildAll` now builds creatures *and* characters; the new
  `Bodies` table is one row per player body (today just `humanoid` ← `Man/Adventurer.fbx`) and the new
  `BuildCharacters` menu item / `-executeMethod` target does the character half alone.
- **`Assets/Resources/Models/Characters/Animations/`** — the drop folder, created and empty. Everything
  importable in it is read: one multi-take FBX, or one file per action, or both.
- **Clips retarget, they are not bound to a body.** The sources import as `Human` +
  `CreateFromThisModel`, so the same six files animate every one of the 21 bodies — and the elf and
  demon bodies added later — with no per-model work. That is the payoff of the Humanoid rule from
  `BL-93`, arriving early.
- **A single-take file is renamed to its own file name.** Every Mixamo download calls its take
  `mixamo.com`; six downloads would be six clips with one name, and the last one loaded would silently
  win every lookup. Multi-take files keep their authored names, which are real.
- **Loop, and lock root travel, on the looping clips only** (`loopTime`, `loopPose`,
  `lockRootPositionXZ`). An unlooped idle freezes the body in its final frame; an unlocked walk slides
  the mesh out of the position the server put it at, because root motion is off by design.
- **`Casting` joined the generated controller** as a bool (never a trigger — the cast has a duration the
  server owns, and it ends on interrupt as well as on landing). The monsters have no cast clip, so their
  controllers are unchanged: a parameter with no clip behind it is not declared and the client does not
  ask for it.
- **The clip matcher is now three passes** — exact name, then the monster packs' `Rat_idle` suffix, then
  a plain substring — shared by both halves. The substring pass is what lets a raw download work
  unrenamed: `Walking`, `Run_Fwd` and `Standing Melee Attack Downward` all land.
- **An empty folder is a SKIP, not a failure.** The existing hand-made `humanoid.prefab` is the best
  thing that exists until art lands and must not be overwritten with a clipless regeneration — and a
  headless build must not go red over an art file nobody has downloaded yet.

### For him

`docs/guides/UnityClient.md` gained **"Adding move / idle / attack animations to the PLAYER"** — the
Mixamo route step by step (format, skin, in-place), the six file names, the one command, and a
symptom → cause table for when it does not move. Only `idle.fbx` is required; `walk` falls back to
`idle` and `run` to `walk`, so two files are enough to see it working.

## 2026-08-29 — 0.102.1: Shield Mastery is `heavy/shield` on every rung

He asked for `/shield` on rungs 1-3 and `heavy/shield` on rung 4, then changed his own mind reading the
result back — and the reason is class balance, not flavour:

*"If we allow the human buffer on a robe+shield … when he become 4th class he have additional bonus on
the shield when the other 2 buffers wearing any+shield will get only one. So the human buffer chooses
heavy+shield for becoming semitank and robe+shield works as the other 2 buffers (no shield bonuses
except the 4th class one) … The demon buffer wearing 2h blunt or staff will get him a bit more
patk+acc than the other buffers but mage weapon is not so much made for hitting. While giving more
pDef and shield rate+Def on a robe pushes one class in front a lot."*

`tank_shield_mastery` is learned by the TANK and by the **Human Warchanter** (rungs 1-3 at 40/60/70,
his own SP). Gated `/shield` alone it was a free edge for one of the three buffers: the elf and demon
never had the skill, so a Human in robe + buckler collected shield P.Def, block rate and (at rung 3)
+10% P.Def that his brothers could not match at any price.

**The gate turns that into a choice.** Heavy + shield → the Human Warchanter is a semi-tank and earns
the ladder. Robe + shield → he is the buffer the other two are, and the shield still does its ordinary
job. Nothing is refused; the skill simply pays nothing outside plate.

### What changed

- All four rungs of `tank_shield_mastery` carry `RequiresShield: true` + `RequiredArmor: Heavy`. The
  WEIGHT cell is `heavy/shield` in `tank 2nd` (×3), `tank 3rd` (×1) and `buffer 3rd` (×3).
- **Back to ONE `PassiveEffect` per rung.** 0.102.0 built `SkillLevel.ExtraPassives` for exactly this
  skill — one rung, two different gates — and this ruling collapses the two gates into one, so the
  "+10% P.Def" layer folded back into the rung as plain `DefencePct`. ⚠ The mechanism stays; it simply
  has no author today, and it is the tool if a rung ever needs two gates again.
- The card now reads *"In HEAVY armor with a shield: … In any other armor it does nothing."*

⚠ **The 4th-class shield passive he is counting on is the HEALER's, and it is unchanged** —
`healer_shield_mastery` @76 (Lightbringer, +10% heal power and +10% MP regen, plain `/shield`, any
armour). The **buffer's** 4th tier has no shield row built yet: `buffer 4th.csv` lines 136-138 are the
three 3rd-tier Shield Mastery rungs at 40/60/70 copied into a 76-90 file with a blank `SKILL_ID`, which
reads as a paste leftover. His file, still in progress — not touched.

`--check` clean, `git diff --numstat docs/data/` 3/3/1 on the three files, server boots 0.102.1, Unity
type-check clean, protocol unchanged (29).

## 2026-08-29 — 0.102.0: the `WEIGHT` column, and a passive that can finally say "in heavy"

*"I like it to do it same as 'weapon' column. `heavy/shield` == heavy and shield required …
`heavy|light` == heavy or light required."*

`BL-107`, the armour twin of yesterday's `WEAPON` column. Same `[set]/[axis]` grammar, one axis over:
`|` is OR among the weights, `/` is AND with the shield slot. The reason it had to be two axes is the
same as the hands token's — **a shield is not an armour weight**, it is a different slot that coexists
with every weight, so `heavy|shield` under an OR reading would pay a robed character with a buckler the
+10% P.Def he asked to confine to heavy.

### The gap it closed

A weight gate existed only inside `ArmorMasteryProfile`, so **only an armour mastery could be
weight-gated**. Any other passive, and every active, could not say "heavy only" at all — which is why
`PassiveEffect.DefencePctWithShield` had been invented as a bespoke field for one skill in 2026-08-21,
when his own note read *"IG is shield+heavy but I'm not sure if we can"*.

### What landed

- **`ArmorWeights`** (the [Flags] requirement MASK; an item still carries one `ArmorWeight`) +
  **`ShieldGate`** (`Any`/`Required`/`Forbidden`) + **`ArmorGate`** — `Satisfies`, `Describe`, `Format`,
  `TryParseRequirement`. The exact twin of `WeaponTypes`, and the ONE place the rule lives.
- **`SkillDef.RequiredArmor` / `RequiredShield`** — the ACTIVE cast gate, enforced in `HandleUseSkill`
  and in the auto-hunt chain (a gate the tap refuses must be SKIPPED there, not attempted).
- **`PassiveEffect.RequiredArmor`** — the general passive gate, all-or-nothing like `RequiresShield`.
- **`SkillLevel.ExtraPassives`** — 🔑 extra passive LAYERS per rung, each with its own gate. An
  all-or-nothing gate cannot describe a rung whose halves differ, and Shield Mastery is exactly that:
  block rate needs a shield, "+10% P.Def" needs a shield **and** heavy. Widening the rung's gate to the
  strictest field would have switched the block rate off for the robed Human Warchanter who learns the
  same skill. **`DefencePctWithShield` is deleted**; the rung carries plain `DefencePct` on layer two.
- **`ArmorMasteryProfile.RequiredShield`** — the shield axis on a mastery. Nothing authors one yet.
- **`SkillText`** states the gate for the first time: `Requires heavy armour and a shield` on the skill,
  `— only with a shield` under a gated passive's numbers. Both clients read the same formatter.
- **`--weight-column`** wrote the column into all 24 files — 1,420 rows, 103 with a real requirement —
  and **`--check`** verifies every cell. A `heavy:` clause in DESCR now resolves against the layer a
  heavy-armoured character actually collects, so a per-weight split is checkable on a NON-mastery row.

### Two behaviour changes, both his

- **A warrior or rogue in a ROBE, or naked, now gets nothing from Armor Mastery.** *"Yes turn off robe
  and naked from warrior mastery no point in them wearing a robe. And we have nothing that strips you
  from armor/weapon."* That second sentence is the argument, and it is the right one. It deliberately
  reverses the 2026-07-01 "with all means every weight" fix. Applied to the rogue as well — the question
  named both and the reasoning is identical.
- **Shield Mastery's bow resistance is now really shield-gated.** Its card has always said *"Every part
  of it needs a shield equipped"*; `ShieldDefPct` and `BlockChancePct` were inert without one by
  accident, but bow resist is an ordinary stat and rungs 3-4 were paying it to a tank with a greatsword.

### The DESCR keys — his call, kept as they are

He offered to rewrite every cell into snake_case keys (`P.Def` → `p_def`). Declined, and it is the
easier side for both: the reader already understands 141 spellings across 46 keys, so a rename buys the
parser nothing and costs 24 files a full rewrite. What he asked for instead is
**`classes_skills_csv/DESCR-KEYS.md`** — every key, every spelling, every scope label — **generated**
from the parser's own alias table by `--descr-keys`, so it cannot drift from what `--check` reads.
(There is no `AllDef`/`alldef%`: P.Def and M.Def are separate stats everywhere in the game.)

`--check` clean on all twelve specs, `git diff --numstat docs/data/` N-for-N on all 24 files, server and
Unity type-check clean, protocol unchanged (29).

## 2026-08-29 — 0.101.5: Body Mastery was HP Boost twice, so it is gone

*"In warrior 2nd file the body_mastery should be removed (it's hp_boost) … warrior_armor_master should
replace the fighter_armor_mastery and include the hp-regen part of the removed body_mastery to all
weights."*

He is right, and the duplication was ours: `HP Boost` landed in `warrior 2nd.csv` on 2026-08-27 at
20/28/36 (+120/+200/+300 max HP) while `Body Mastery` had been paying the warrior +60→+150 max HP on the
same rungs since the file was written. Two skills, one stat, no reason for the second.

### What landed

- **`body_mastery` is deleted** — the id, the `SkillDef`, and all five warrior learn rows (20/24/28/32/36).
  It was warrior-only, so nothing else loses anything. **No save migration:** a retired id dies on load
  (`PersistenceService.ParseLearnedSkills`), the filter that exists for exactly this.
- **Its HP-regen half moved into `warrior_armor_mastery`, on every weight** — the ladder copied rung for
  rung, `0 / 1.1 / 1.1 / 1.6 / 1.6` HP/s, sitting alongside the existing `mpReg x1.1` and flat P.Def in
  the "with all" clause. Flat HP/s, per `BL-92`; never `HpRegenPct`.
- **Two `REPLACES` cells filled in** that the code already carried and the sheet did not:
  `warrior_armor_mastery` → `[fighter_armor_mastery]` (all five rungs), and `tank_weapon_mastery`'s
  **level-20** row → `[fighter_weapon_mastery]`, which was the only rung of its five left empty.

**Net effect on a level-36 warrior:** −150 flat max HP (the duplicate), HP regen unchanged, everything
else unchanged. `HP Boost`'s +300 is what the HP now comes from.

### Proved, not assumed

`--check` reads the new number. Injecting `hpReg +9.9` into rung 2 produced both a
`🟡 VALUE hpreg: CSV 9.9 vs code 1.1` and a `🔵 LADDER DIP` — so the clause is genuinely parsed per
weight, not skipped. Restored, and the file is clean. Server boots 0.101.5, Unity type-check clean,
protocol unchanged (29).

### Still open — his armor-WEIGHT column

The same message proposed a `WEIGHT` column, the armour twin of `BL-105`'s `WEAPON`. Written up as
**`BL-107`** with a recommendation on all three of his questions; not built pending his ruling.

## 2026-08-29 — 0.101.4: the sheet carries the SKILL ID, and the name becomes a label

*"We can add a column skill_ID and the name to be just the 'display name' … and replace the replaces
column to be a list of id's not names. U can fill them for now and I'll look at them … Now having only
names start to take its toll. We can have 10 skills with the same display name but to be actual
different skills."*

🔑 **He is right, and the evidence is this tool's own last two days.** Twice running, a name-keyed
lookup produced a wrong answer nobody could see: `--weapon-column` handed `rogue 2nd.csv` the **tank's**
weapon requirement because three different skills are displayed "Weapon Mastery", and `Descr.cs` keys
its exception table on the name, so the 0.101.2 rename silently unhooked it. A display name is a label.

### What landed

`SKILL_ID` at index 2, right after NAME. **1,425 rows, 1,354 resolved.** The three Weapon Masteries are
three skills now — `fighter_`, `tank_`, `rogue_weapon_mastery` — and `--check` **pairs the two sides on
the id** wherever a row has one, falling back to the name only where it does not. Proved by pointing one
row at the wrong id: it immediately split into a NOT REGISTERED row plus a rung-count mismatch, where
before it would have matched happily on the name.

`REPLACES` became ids in the same pass — 328 cells. It had to move with it: the cells were
space-separated lists of names whose names *also* contain spaces, so `[Shield Harden Shield Bless]` is
two skills and `[Might Fury Vampirism]` is three, and nothing but a guess could separate them.

### 🔴 The first run had this row replacing itself

Resolving the REPLACES *names* gave `rogue 2nd`'s Weapon Mastery `[rogue_weapon_mastery]` — itself. The
row means the **fighter's**, and a rogue's kit is cumulative, so it contains both under one display
name and matched its own entry first.

🔑 **The fix is that the code already knows.** `SkillDef.Replaces` is a list of ids and is unambiguous,
so that is what gets written; name resolution survives only as a fallback for rows the code does not
carry, and it excludes self. Third time in three days that a name-keyed lookup has been wrong — which
is the argument for this whole change, arriving on schedule.

### What was deliberately NOT filled in

⚠ **Nineteen `REPLACES` cells were left exactly as they were, and nine `SKILL_ID`s are empty.** Putting
invented ids into an authoritative file is the one thing this pipeline must never do:

- **18 sigil cells** read `[Warrior/Mage/Tank/Buffer/Rogue Defence]` — a family shorthand, not names.
- **`[Shield Harden Shield Bless]`** — neither half is a name the buffer's class knows.
- **9 rows** in the `buffer 4th` draft (*Harmony of the Soul*, *Harmony of Madness*, *Harmony Mark*):
  not built, so there is no id yet.

His to fill in; every one is printed by the tool and listed in `classes_skills_csv/README.md`.

🔵 **`--check` does not compare REPLACES yet** — deliberately. He said he will review and re-author
these; comparing now would report his in-progress edits as defects. It becomes a checked column once
he says the list is right.

`--check` clean · protocol still 29, no db reset.

## 2026-08-29 — 0.101.3: mana vamp for all three buffers, and his id rule made checkable

### Mana Vampirism is all three races now

It was Human + Demon, which is why the elf half of its blunt-or-bow gate looked pointless yesterday.
His reason is the class's whole economy, not a bonus: *"it's their way of rebuffing every 5 mins with
500mp buffs (mp pots now help but not in pvp)"*. A full re-buff costs more than the pool holds, the
potions sit on a cooldown the pull does not wait for, and in PvP they are not an option at all — so
the mana comes back through the weapon or the buffer stops buffing. The elf was the one race that
could not do that.

⚠ **His CSV row has always had a blank RACE column**, i.e. all three. The code was the odd one out, and
that is the second time in two days this file has been right where the code was wrong. It moves to the
shared `kit2` rather than being added to a third race list, so it cannot drift again.

### `--chain-audit` — his id rule, measured

His rule: *"a chain of classes (fighter/mage) should replace their weaker skills with newer or
continuing the line .. but cross chain should have different id's"* — `mage_weap_mastery →
spellcaster_weap_mastery → buffer_weapon_mastery` against `fight_weap_mastery → war_weapon_mastery →
swordmaster_weap_mastery`.

✅ **The masteries already obey it.** Fighter: `fighter_weapon_mastery` → `tank_`/`warrior_`/
`rogue_weapon_mastery`, same for armor. Mage chain separate throughout. No mastery id is shared, and
**no `Replaces` crosses a chain at all**.

🔵 What does not obey it is in `BL-106` with the cost of each fix: two mage ids that do not name their
chain (`weapon_mastery`, `armor_mastery`), and **six ids learned by both chains — every one of them the
Warchanter**, which is exactly the class that borrows from the fighter (`tank_shield_mastery`,
`hp_boost`, four stat swaps). 🔴 A rename here is **not** free like the class renames were: a skill id
is persisted in a character's learned set, so it needs a new id plus a load-time migration. His call,
not mine.

### 🔴 The audit's first run reported 44 violations. All 44 were invented

It walked `BaseClass × Archetype × Discipline` as a cross product. `ClassSkills.Cumulative` resolves an
archetype's base class internally and **ignores the one you pass**, so asking for "Mage/Tank" hands
back the Fighter's tank kit — and the tool then concluded that a Mage learns `tank_weapon_mastery`.

🔑 **That is the same mistake as the display-name lookup in `--weapon-column`, one day apart:**
enumerate what EXISTS, never a cross product. It now walks `Disciplines.Of` and only real classes.
It also separates the 26 deliberately-shared ids (`shared 4th` + the Sigils) by a **derived** test —
"taught to every 4th-tier class in both chains" — rather than a hand-written allowlist that would rot
and let a new sharer hide inside it.

`--check` clean · server boots v0.101.3 · protocol still 29, no db reset.

## 2026-08-29 — 0.101.2: two names the Ork→Demon rename had left behind

Four small things from his read of `buffer 3rd`, and the first one is the same bug for the third time.

### 🔴 Mana Vampirism was blunt-only. It is blunt **or bow**

His correction: *"the mana vamp works on basic attack with required weapon blunt or bow … not only
blunt"*. His CSV row has always said `Require: Bow/Blunt`; only the code said blunt.

🔑 **That is the THIRD requirement in two days found disagreeing with its own authored row** — Combo
Mastery, the tank's "any weapon", and now this. All three were invisible for exactly the same reason:
the requirement lived in free-text DESCR where nothing could compare it. This one was caught by
`--check` the moment the code changed and the `WEAPON` cell did not — which is the column earning its
keep on its first day.

### Two renames the class rename had missed

- **`Bloodhanter Blunt Mastery` → `Warlock Weapon Mastery`.** He points out it was a typo for
  *Bloodchanter* — and rather than fix the typo he retired the word: *"as we changed the orks to
  demons and changed the classes names -> so rename it to 'warlock weapon mastery' the 4th classes
  name"*. Warlock is the Demon buffer's 4th class. His own IP test passes — word + SAME RACE + SAME
  ROLE: ours is a buffer, IG's is a summoner. "Blunt" left the name too, because the requirement lives
  in the `WEAPON` column now (`blunt/2`), not in prose.
- **`Chanter Heavy Mastery` → `Heavy Armor Mastery`**: *"human and demon are no longer 'chanter' as
  class name"*. They are a Doctor and a Dreadcaller since `BL-100`/`BL-101`.

🔑 **The skill IDs did NOT move** — `wc_bloodhanter_blunt_mastery` and `wc_chanter_heavy_mastery` are
still the ids, and now deliberately do not match their names. Ids are append-only because characters
persist their learned ids; moving one orphans every save that holds it. The C# const identifiers were
renamed to match (compile-checked, free), and the strings stayed. Read them as serial numbers.

⚠ **The TANK already has a "Heavy Armor Mastery"** and that is fine: one is Fighter, one is Mage, no
character can hold both, and `Abbreviations` de-duplicates names before assigning bar labels — the
startup validator confirms it.

🔴 **A rename broke something silent, and this is the reusable part.** `Descr.cs` carries an exception
table **keyed on the skill NAME** (`("chanter heavy mastery", "cast")` — the row where 90%(×1.8) is
result-then-input). A name-keyed table does not error when a name changes; the exception simply stops
matching and a correct row starts reporting as a defect. **A rename has to grep the tool, not just the
catalog.** Also swept: the stale `ORK` labels around these two skills, `README.md`, `Open-Checklist.md`
and the `buffer 4th` seed rows.

### `buffer_auto 3rd.md` deleted

*"remove the buffer_auto file as we are done with it"* — the rejected auto-draft of the buffer's 3rd
kit. Its two surviving rulings (harmonies do NOT evict singles; race is a combat tint only) are in the
built kit and in the CHANGELOG.

`--check` clean · server boots v0.101.2 · Unity type-check clean · BalanceMatrix byte-identical ·
protocol still 29, no db reset.

## 2026-08-29 — 0.101.1: the `WEAPON` column, and hands stop meaning what I thought

`BL-105`, approved the day it was proposed. The weapon requirement is a real, enforced gate that lived
**only in free-text DESCR** — so `--check` could not compare it, and that is exactly how the elf's
Combo Mastery bug survived. It is a column now, in his own grammar.

### His grammar, verbatim

```
"weapon" -> weaponType1[|weaponType2|weaponType3][/hands]
  sword|blunt|bow     == any sword or any blunt or bow
  sword|blunt|bow/1   == 1 handed sword or 1 handed blunt or bow
  duals               == only duals;  duals/1 also parses as duals — typo-WARNING
  blunt               == any blunt (mace/maul/staff/wand)
  blunt/2             == 2h blunt (staff/maul)
  no /1 or /2         == any hands
  anything else (/ , /3, /a) == ERROR, and the hands become invalid
```

### 🔴 This corrected a semantic shipped hours earlier

`sword|blunt|bow/1` includes **a bow**. So the hands token narrows **the TYPES, not the equipped
weapon** — and 0.101.0 had it the other way round, checking hands literally against what you hold,
where a bow would have *failed* `/1`. That is true of a bow and useless to an author.

The rule is now one function, `WeaponTypes.Resolve`: expand each named type by the hands token —
sword → `Sword` or `TwoHandedSword`, blunt likewise — and pass **Bow and Dual through untouched**,
since neither has a one-handed variant to narrow to. `Satisfies` is a bitwise-AND against that.

🔑 **It made the code simpler, not more complex.** The conditional fold from playtest 28 (*"a bare type
means any hands of it"*) is gone as a special case — it now falls out of the expansion, because `blunt`
+ `Any` simply resolves to `Blunt|TwoHandedBlunt`. One rule replaces two.

⚠ **A requirement mask must name BASE types only** now (`Sword`/`Blunt`/`Bow`/`Dual`, or the
`AnySword`/`AnyBlunt` pairs). Spelling hands into the mask no longer means anything — it is folded and
re-expanded, so `TwoHandedBlunt` + `Any` would *widen* to any blunt. Nothing in the catalog does that,
and the comment at `WeaponMasteryProfile.RequiredWeapon` says so.

His follow-up, recorded because it is the authoring rule: *"passives won't ever be a (bow/duals or one
handed weapon) … but if authored they should work that way"*. So no combination is special-cased or
refused — the grammar is general and the odd ones simply work.

### The column

`--weapon-column` inserts `WEAPON` at index 3, after `TYPE` and ahead of the three targeting columns
(RANGE / AOE / TARGET) — what a skill *demands* sits with what it *is*, not with where it lands.
**1,425 rows across 24 files; 187 carry a real requirement.** Every file diffs N/N, so the splice
touched nothing else.

🔑 **Two different code fields feed one column, deliberately.** An active gates through
`SkillDef.RequiredWeapon`; a mastery passive carries its requirement on the per-rung
`WeaponMasteryProfile` instead. To a player they are the same sentence — *"this does nothing unless
you are holding X"* — so they are one column. ⚠ And a profile with no explicit mask still gates
through its filled **slots**: `BufferMastery` sets Blunt and Bow and leaves the rest inert, which *is*
"blunt or bow". Reading the mask alone would have written an empty cell for every one of those.

### 🔴 The generator's first run was wrong, and the output is what caught it

`--aoe-column` keys its lookup on the DISPLAY NAME across every class. **Three different skills are all
called "Weapon Mastery"** — the fighter's, the tank's, the rogue's — so `rogue 2nd.csv` was handed the
*tank's* cell, `sword|blunt/1`, for a passive whose own DESCR says bow and dual.

🔑 `--aoe-column` has the same collision **and it has never shown**, because all three carry a radius of
0: the wrong answer equalled the right one. The lookup is per-file now, scoped by filename the way
`Check.Specs` is, and a display name carrying two different cells at one learn level is **reported and
left empty rather than guessed** (5 such, all in `shared 4th.csv`, which genuinely spans every class).

### `--check` reads it, and I proved it fails

Compared as **parsed-and-reformatted requirements, never as raw strings** — `blunt|sword/1`,
`sword | blunt/1` and `SWORD|BLUNT/1` are one requirement, and a checker that called them three would
train him to ignore it. Verified by deliberately corrupting five cells:

| authored | result |
| --- | --- |
| `sword\|blunt/3` | 🔴 WEAPON ERROR, hands dropped, then the resulting mismatch |
| `duals/1` | ⚠ WEAPON TYPO — parsed as `duals` |
| `blunt\|sword/1` | **silent** — normalisation works |
| `blunt/1` | 🟡 mismatch |
| `sword\|blunt/` | 🔴 WEAPON ERROR |

Restored; `--check` is clean. **BalanceMatrix output is byte-identical** across the semantic change,
and the server boots.

⚠ **CSV DESCR text updated in the same commit**: `tank 2nd` *"with any weapon"* → *"with 1h
sword/blunt"*; `buffer 3rd` *"Blunt:"* → *"2h Blunt:"* and `Box` → `Bow`.

## 2026-08-29 — 0.101.0: a weapon gate that can finally say "one-handed"

His question: *"can we make a hands gate to skills/passives … a skill/passive gates a type of weapon
(sword/blunt/bow/dual) and gates hands (1h/2h/any)"*. Half of it was already built. The missing half
turned out to be the important one, and it was hiding a bug.

### The type gate existed; the ONE-HANDED gate was impossible

`WeaponType` is a `[Flags]` enum carrying hands and type together (`Sword=1 … TwoHandedBlunt=32`), and
`SkillDef.RequiredWeapon` has gated casts on it for months. But since playtest 28 a **bare type means
any hands of it** — the fix for *"cannot use acoustic shock and sound smash with maul, only work with
1h"*, where the Warchanter's own maul locked him out of his own damage skills. `Sword|Blunt` **is**
that bare pair, so:

```
Satisfies(equipped: TwoHandedBlunt, required: Sword|Blunt)  →  TRUE
```

A maul passed a mask meant to read *one-handed sword or blunt*. There is no spare bit for a
`OneHandedSword` and renumbering the enum would move every item's type, so hands became **their own
axis**: `WeaponHands { Any, One, Two }`, as a `RequiredHands` field on both `SkillDef` and
`WeaponMasteryProfile`. The type mask stays hands-agnostic; hands are checked literally against the
equipped weapon, first, with no special case.

🔑 **Bow and Dual need no clause.** His own note — *"a bow shot requires a bow + any hands (always 2
so hands are unnecessary)"* — is what falls out for free: a bow skill is authored `Bow` + `Any` and
never mentions hands, and because the check is literal a bow satisfies `Two` and fails `One`, which is
simply true of a bow. An **empty hand** satisfies neither.

The three authored rows that spelled hands into the mask (`TwoHandedSword|TwoHandedBlunt`) moved to
`AnySword|AnyBlunt` + `Hands.Two`, so there is exactly one way to say it. The old spelling still works
and still means what it says — it is kept as a backstop, not as a second style.

### His three rulings, live

| class | gate | was |
| --- | --- | --- |
| **Knight** (tank) | 1H sword or blunt | 🔴 **"any weapon"** — a knight could hold a greatsword and keep the whole passive |
| **Demon buffer** (Bloodhanter Blunt Mastery) | 2H blunt — maul/staff | bare `Blunt`, i.e. any hands |
| **Human buffer** | 1H blunt, via his own **Shield Mastery** | nothing needed — a shield *is* the 1H gate |
| **Elf buffer** | bow | unchanged |

⚠ **The SHIELD is deliberately not part of the tank gate** — *"the shield is not a requirement, the
shield has its own passive"*. A tank who drops the shield for a second one-hander keeps the mastery.

⚠ **The shared Spell Mastery stays hands-agnostic blunt-or-bow**, per *"they share one so we gate only
the type, and their additional passives are hands gated"*. Do not push hands up into `BufferMastery`.

🔑 **Wrong weapon costs you the bonus and NOTHING else** — his ruling, and already how every weapon
mastery in the game reads. No penalty was added anywhere.

Tank Weapon Mastery moved from `Levels[].Passive` to `WeaponMasteryLevels` to get there — the same
five numbers through the same `RecomputeDerived` path, but `Levels[].Passive` is unconditional by
construction and has nowhere to hang a gate. **`BalanceMatrix` output is byte-identical before and
after**, because its reference tank already carries 1H + shield: the change bites only a tank who
chooses a two-hander, which is the entire point.

### 🔴 The elf Warchanter could never proc a passive he had paid up to 880k SP for

`Combo Mastery` is in the **shared** buffer kit, taught to all three races, and it was gated
`RequiredWeapon: Blunt`. The elf's identity is the bow. So the elf bought a three-rung passive at
74k/190k/880k SP and it could not fire once. His own CSV row has always said **`Require: Box/Blunt`**
— Bow/Blunt, with the typo — so the file was right and the code was wrong.

🔑 **Nothing in the game could have shown this.** A proc that never fires is indistinguishable from a
3% roll that keeps missing. It survived because the weapon requirement lives *only* in the free-text
`DESCR` column, where `--check` cannot compare it — which is why `BL-105` proposes a structural
`WEAPON` column, the same move already made for `AOE` and `TARGET`.

### Two smaller things

- **`BalanceMatrix` was applying its own weapon rule** — a raw `(required & equipped) != 0` that
  predated both the playtest-28 fold and this change, so it refused a maul a skill the server allows
  and would have allowed a 2H a one-handed passive. It calls `WeaponTypes.Satisfies` now. A
  measurement that applies its own rule measures nothing.
- **The requirement text is generated, once, for both readers.** `WeaponTypes.Describe` gives the
  cast-refused message and the tooltip the same words — "requires a **two-handed sword or blunt**
  weapon". The raw `Enum.ToString()` it replaces printed the convenience masks by their internal
  names ("anysword or anyblunt") and could not mention hands at all.

**CSVs updated in the same commit** (`tank 2nd`: *"with any weapon"* → *"with 1h sword/blunt"*;
`buffer 3rd`: *"Blunt:"* → *"2h Blunt:"* on the eight Bloodhanter rows, and `Box` → `Bow` on the six
that carried the typo). `--check` passes with no discrepancies.

⚠ **The warrior sword-vs-blunt split is RULED but not built** — `BL-104`. There is no warrior
3rd-class kit to gate; the mechanism is ready and the rule is recorded for the day his
`warrior 3rd.csv` / `war_aoe 3rd.csv` land. The mace+shield half of his worry was already covered:
Two-Hand Mastery has been two-handed-only since it was written.

⚠ **No protocol change (still 29)** — an existing APK connects and the gate works, since it is
enforced server-side. A rebuild is only wanted for the new tooltip wording.

## 2026-08-28 — 0.100.2: two creature families, and the camera stops lying about its centre

The first models are on screen, so this is the pass that answers what looking at them found. Three
things, all from his remote-control notes.

### The mobs got bodies of their own — and they MOVE

`mob_animal` (Rat) and `mob_insect` (Spider) are built and reachable by the loader, covering **20 of
the 79 roster templates** — including the first creature a new character ever meets (Ridgeback Pup,
Lv 1), Fox at 4, Ashen Wolf at 10 and Hook Spider at 14. Wildlife stops wearing a human body inside
the first hour of play, which is the question the proof of concept was asking.

🔑 **They are ANIMATED, and that half cost nothing.** The monster FBXs he committed ship with
Idle/Walk/Run/Attack/Death takes, which is exactly the parameter set `EntityView` was already
driving since protocol 29 — `Speed` off the drawn-position delta, `Attack` off `CombatEvent`,
`Dead` off the snapshot. So the animation pipeline is now proven end to end with **no new message,
no new field and no new code in the client**. Locomotion is a 1D blend tree on `Speed` (thresholds in
Unity units/sec — `WorldMapper.Scale` is 0.01, so a mob running at 132 server units arrives as 1.32);
Death transitions from **Any State**, because a creature can die mid-swing, and back out again
because `Dead` is a bool and the same view is reused on respawn.

⤷ ⏸ **This reverses the "prefab automation is deferred" ruling of 2026-08-28**, on his own
instruction the same day: *"can u add 1~2 mobs? U said u can do it alone as I don't have access to
the pc."* The deferral assumed he would hand-make an animal or two first; he cannot, so the tool got
built. `Assets/Editor/ModelSetup.cs` + `-executeMethod`, run with the Editor closed. **Adding a
family is one line in `ModelSetup.Families`.**

🔴 **Two things the FBXs did not advertise, both found by running it:**

- **The monster packs import with `avatarSetup: NoAvatar`.** A Generic rig with no Avatar has nothing
  to play clips *on*, so Unity does not put an `Animator` on the model at all — the file arrives full
  of perfectly good animation that cannot run, and nothing in the inspector says why. `PrepareImporter`
  now forces `CreateFromThisModel` for every creature FBX, so the next one dropped in is not a second
  afternoon. Imported takes also do not loop by default: an unlooped idle plays once and freezes the
  creature in its last frame, which reads as a broken model rather than a missing checkbox.
- **Scale is authored, not inherited.** The packs disagree with each other and with the character
  pack; the Rat imports ~2.9 units tall. Each family states a height and `ModelSetup` normalises to
  it, so the "Entity size" slider is never asked to compensate for an art decision.

⚠ **`GetComponent<T>() ?? AddComponent<T>()` is a trap and it cost the first run.** `??` is a plain
C# null check, and `UnityEngine.Object` overloads `==` to report an absent component as null *without
being null to the runtime* — so the coalesce keeps the empty handle and the next line throws.

### The camera: two real bugs, both only reachable since the view was tilted

**The rotation slider did not rotate around the character.** His words: *"it doesn't rotate around
the center but it rotates like a smaller circle in the middle."* Correct, and the cause was that the
rig **smoothed its own world position** toward `target + orbit` while applying rotation *exactly*.
Fine while only the target moves; wrong the instant yaw moves, because the camera then aims with the
new yaw from an old place — the character stops being the point the world turns about and swings
around a smaller, phase-lagged circle instead. Not subtle either: at `Follow` 12 the time constant is
83 ms, so dragging the slider at ~360°/s throws him roughly half a screen off centre.

🔑 **The smoothing belongs to the FOLLOW, not to the orbit.** It now damps the *follow point* — still
the player's position, so chase-the-player feel is unchanged — and hangs the orbit off it as rigid
geometry the filter cannot distort. Dead centre at every yaw.

**The growing unrendered band along the bottom in ortho** is the **near clip plane** eating the
ground. An ortho camera's clip planes are a *slab*, not a frustum: anything nearer than 0.3 (the
scene default) is cut regardless of how wide the view is. Tilt the camera and the bottom of the
screen is the *near* end of the ground — at pitch θ it sits at depth `Distance − OrthoSize·cot θ`.
Zooming out raises `OrthoSize` while `Distance` stays put, so that depth marches at the camera,
crosses zero, and the ground is clipped from the bottom up in a band that grows with every further
zoom. ⚠ **At Pitch 90 — the shipped default — `cot θ` is 0 and this cannot happen.** It was
unreachable until the camera-angle slider was pulled to 45° to judge the models.

The fix is free because **under ortho, distance does not zoom**: the rig is backed off by exactly the
term that was eating the margin, pinning the bottom-edge ground depth at `Distance` (≥10) at every
angle and every zoom. Perspective is untouched.

### A subclass no longer changes your body

His question: *"in IG I can be a human fighter (model) and take a sub of a demon mage — my model
still looks like the human fighter one but the stats and skill kits are the subclass one. Can we do
this?"* We could not: `Entity.Race`/`BaseClass` proxy into the **active** subclass, so a swap to a
demon mage swapped the model with it.

🔑 **Appearance is SLOT 0** — the class the character was created as, and the one slot that can never
be removed. Nothing needed inventing, and both behaviours he named fall out of the one rule:
`SwitchSubclass` never touches slot 0, so a swap cannot change the body; `HandleDebugReset` rebuilds
slot 0 from the chosen race/class, so **the admin re-roll does** — *"(and admins reset resets the
model - changing all)"*.

⚠ **No protocol change and no new fields.** Nothing in the client reads `EntityDto.Race` or
`.BaseClass` except `ModelLibrary.Keys` — they exist to choose a mesh; the character sheet and
class-select screens read their own DTOs, which still carry the active class and are untouched. The
day a target frame wants a stranger's active class, *that* is when two more fields land.

### 🔴 The player still cannot run — and it is not a code problem

**Every one of the 21 character FBXs contains zero animation clips.** Measured, not assumed: mesh,
skeleton, bind pose and 65 bones, `AnimationStack` count **0**. The monster pack ships animated; the
character pack he committed does not. So `humanoid.prefab` is a body that slides. No amount of
controller wiring fixes that — there is nothing to play. See **`BL-102`**: it needs a clip source
(the pack's own animation file, or Mixamo, which retargets onto the Humanoid rig he correctly set).
The mob work above is the proof that the moment clips exist, this is a one-line addition.


## 2026-08-28 — 0.100.1: `BL-93` gets a body — the first prefab lands

The Editor half of `BL-93` is done: `Assets/Resources/Models/humanoid.prefab` exists, so the client
draws a real model instead of a sphere for **every player, every NPC and every humanoid mob at once**
— the universal last resort at the bottom of `EntityManager.Keys`. No code changed; the loader has
been waiting for the file since protocol 29.

The FBX source packs are committed alongside it (`Models/Characters/`, `Models/Monsters/` — 31 files,
16 MB). Owner: *"push all ill later remove/update them to prefabs - if PoC works"*. They are raw
sources, not yet wired to anything; only `humanoid.prefab` is reachable by the loader today.

⚠ **50 of the 83 mob templates are not humanoid** (11 animal, 9 undead, 9 insect, 5 dragon,
5 demon, 3 angel, 2 plant, 2 magiccreature) and currently fall through to the human body. Each is
peeled off by adding `mob_<category>.prefab` — same Editor procedure, different file name, still no
code.

### Three fixes to `docs/guides/UnityClient.md`, all found by walking the guide with him

- **Step 10 told you to save the scene.** It contradicted the guide's own preamble (*"you never touch
  a scene that gets saved"*) and, in an unsaved scene, Ctrl+S opens **Save Scene As** pointing at
  `Assets/Scenes` — which reads as "Unity will not save my prefab" when the prefab was already written
  to disk the moment it was dragged into `Models`. Now: do not save, press Cancel.
- **Step 11 said `pwsh tools/publish.ps1`, without `-Apk`.** That builds the server zip and **no APK
  at all** — you would reinstall the old build and conclude the model had failed.
- Step 11 never said to **close the Editor first**; the headless build refuses to run while
  `Unity.exe` holds the project lock.
- The naming table still said `player_ork_mage`, stale since the `BL-101` race rename → `player_demon_mage`.

## 2026-08-28 — 0.100.0: the base stat table is rebalanced, and 153 becomes a rule

He filled in the empty Stats table in `docs/data/classes_skills_csv/README.md`, then asked two
questions about his own numbers: *"is the sum right"* and *"i want to fix the 47 demons atk.. is way
higher than others (even the fighters)"*.

### The 47

The demon **mage** carried ATK 47. That is higher than every FIGHTER in the game (40/36/41), which is
what he caught — a mystic's power stat sitting above the men who swing swords for a living.

It was deliberate, for one week. On 2026-08-21 it was raised from 41 to 47 — `41 × (25/22)`, the human
mage's ATK scaled by IG's own mystic STR ratio — to fix his measured complaint that *"2h blunt ork have
almost the same as 1h mace human (with 1000pdef on top)"*. The reasoning was sound; the side effect was
not looked at hard enough. It is **42** now, his number.

⚠ **His old complaint does not come back.** Measured, not asserted (`--warchanter 90`):

| demon ATK              | Warchanter P.Atk vs human's mace+shield | Nuker M.Atk vs human |
| ---------------------- | --------------------------------------- | -------------------- |
| 47 (the week it stood) | +45.6%                                  | +9.8%                |
| **42 (now)**           | **+32.9%**                              | **+1.6%**            |

+32.9% is still a clean two-hander-versus-shield trade. What the demon MYSTIC pays is the nuker's
damage edge, which is now ~nil while he still carries the slowest cast and the lowest magic crit — he
buys pool and body instead (CON 31, SPT 41). That is the deliberate half of this change.

### The sums — and why nothing had noticed

His fighter row was right (153/153/153). His mage row was not: **151 / 151 / 155**. And the LIVE table
in `StatCalculator.GetBaseStats` was worse than either of them:

|                 | Human   | Elf     | Demon   |
| --------------- | ------- | ------- | ------- |
| Fighter, before | 153     | 153     | **150** |
| Mage, before    | **148** | **141** | **162** |

The **elf mage was 21 points behind the demon mage** — not as a design, but as drift. Six columns had
been edited a cell at a time over months, each edit locally reasonable, and **nothing had ever added
them up**.

### 🔑 153 is a rule now, and the server enforces it

His ruling: a race is a **REDISTRIBUTION of the same 153 points, never a bigger pile**. `Program.cs`
now runs `StatCalculator.BaseStatsNotSummingTo153()` at startup beside the duplicate-class-name check,
and **refuses to boot** on a column that is off — naming the race, the class and the delta. A one-cell
edit here is the easiest slip there is and completely invisible in a playtest, which is exactly the
profile that earns a boot failure rather than a comment.

### The table he authored

| FIGHTERS | Human   | Elf     | Demon   |     | MAGES   | Human   | Elf     | Demon   |
| -------- | ------- | ------- | ------- | --- | ------- | ------- | ------- | ------- |
| ATK      | 40      | 36      | 41      |     | ATK     | 41      | 37      | 42      |
| CON      | 43      | 39      | 47      |     | CON     | 29      | 25      | 31      |
| AGI      | 30      | **36**  | 28      |     | AGI     | **26**  | **32**  | 20      |
| SPT      | **26**  | **25**  | 27      |     | SPT     | **37**  | 36      | **41**  |
| WIT      | **14**  | 17      | 10      |     | WIT     | 20      | 23      | 19      |
| **Sum**  | **153** | **153** | **153** |     | **Sum** | **153** | **153** | **153** |

It also completes a sentence of his from July — *"Elf have wit/agi - demon have con/spt/int human is in
between"*. With SPT at 37 the human mage is now literally the middle value of all five of his stats.

### ✅ And the biggest step in the table was raised as a worry, then ruled fine

The elf mage's AGI went **24 → 32**, which puts the **elf MAGE above the human FIGHTER (30)** — +8
accuracy and +8 evasion on the level-90 elf Warchanter sheet. Flagged, and closed by him the same day:

> *"my idea is the elf is fast and buffer is semi archer .. the human warrior have acc and rogue have
> evasion+speed so matching in agility stat alone is not OP"*

🔑 That is this project's founding rule aimed at AGI — **a stat is not an identity, the KIT is**. The
human warrior's identity is *accuracy* and the rogue's is *evasion + speed*, both authored in their
kits, so a mage who merely ties them on one raw number has matched the seasoning and none of the meal.
The elf buffer is also a **semi-archer** (bow + light armour), so the race whose theme is *fast* is
holding the weapon that wants AGI: the 32 is describing the design, not leaking past it.

⚠ The corollary is a rule for later, and it cuts the other way: AGI has no archetype split, so any
future *"the rogue should be more agile"* must be paid in the ROGUE'S KIT — never by re-splitting this
table by archetype, which would reinstate the deleted `ClassFlatBonus` idea by the back door. Written
into `docs/design/CritBlowAndDouble.md` beside its (still unbuilt) `dexMod`.

### ⚠ The stat columns in `game.db` are no longer trusted on load

Base stats are stamped from the table at character CREATION and stored per subclass. Nothing in the
game invests or mutates them — so a stored value is only ever a stale copy, and every character already
in a database would have kept the OLD numbers for life, invisibly, with a character sheet that looked
perfectly normal. `PersistenceService.ToSubclass` now calls `RollBaseStats()` on the way in instead of
reading the five columns. They are still written; they are simply not believed. **No schema change, so
no `game.db` delete is needed** — an existing character picks the new table up on its next login.

🔑 If a stat ever becomes investable (dyes, player-spent ±5 swaps), that re-roll must become
re-roll-then-re-apply, or the investment is what gets thrown away. The note is on the method.

### Also

- The stale sketch at the top of `StatCalculator.cs` — *"Demon Fighter 40/30/10/20"*, wrong for long
  enough that it had already been annotated as wrong — is deleted rather than re-annotated. The table
  is the table.
- The README table now carries the 153 rule and the "this table IS the code" note, the same mirror
  contract the skill CSVs run on.
- `docs/design/CritBlowAndDouble.md`'s AGI table was quoting the old values; refreshed and re-sorted.


