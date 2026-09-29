# Changelog, 0.58.0 to 0.99.x, 2026-08-10 to 2026-08-28 (archived volume)

Newest first, verbatim. The live file is [../CHANGELOG.md](../CHANGELOG.md), which lists every volume.

## 2026-08-28 — 0.99.0: `BL-95` buff presets, and [Char] gets its own button

Two asks, one build.

### `[Char]` leaves the bag

> *"Take the [Char] button out of the bag and put it between [bag] and [skills] -> [bag][char][skills]
> -> now its very anoying each time to open -bag open stats"*

The action bar's second row is **[Bag][Char][Skills]** now, three wide like the first. The sheet has
had three homes in a fortnight — the bar, the vitals panel, and the bag (where it went once tapping
your own vitals became *target myself*) — and each move bought a tap somewhere else. It is a
top-level window, so it gets a top-level button, and it sits in the MIDDLE so neither of the two you
already know the position of moves out from under your thumb. The bag's [Del: off] toggle slides left
into the gap.

### `BL-95` — buff presets, and the NPC set grows to SIXTEEN

> *"npc to have (bulwark, might, force, alactiry, fury, swift, ward, body, vigor, resolve, frenzy,
> vamp + serenity, soul, aim, agility) — players to not be so overwelmed by mobs (serenity, soul —
> longer mage sessions, agility+aim — fighter les misses dagger less hits taken)"*

Four blessings come BACK to the newbie buffer: **Serenity, Soul, Aim, Agility**. That is the playtest-
28 trim partly reversed, and knowingly — the eight cut then were called "the optimiser's row", but two
of those axes turn out to decide how long a SESSION is rather than how good a parse is. A mage out of
MP and a dagger eating every swing both stop playing. Focus, Ferocity and Insight stay out; they are
still the optimiser's row.

**Four preset buttons**, each casting a list in one press:

| Button        | Count | Contents                                                                     |
| ------------- | ----- | ---------------------------------------------------------------------------- |
| **Full buff** | 16    | everything                                                                   |
| **Mage**      | 10    | Bulwark, Force, Alacrity, Swift, Ward, Body, Soul, Serenity, Resolve, Frenzy |
| **Fighter**   | 10    | Bulwark, Might, Fury, Swift, Ward, Body, Vigor, Vamp, Frenzy, Aim            |
| **Custom**    | —     | your own, once saved — hidden until then                                     |

🔑 **The client never composes a buff list.** It sends a preset KEY and the server expands it, so the
price on the button and the buffs that land are one decision made in one place — and a client too old
to know the word "mage" simply doesn't draw the button instead of casting a wrong set. There is no
preset discount and no surcharge: same shopping, one press. Every row carries the COUNT as well as the
price, because with a 20-slot bar and a 16-buff set the count is what you are actually choosing.

🔑 **Save reads what you are WEARING** — his workflow, verbatim: *"the idea is to buff fully from npc
then remove what u dont need as that class and save it"*. Both of his worked examples fell out of ONE
filter with no special case, because `BuffInstance.SkillId` is the def that literally created the
buff: a potion or scroll resolves to its FAMILY rung (`buff_might_r`, never `npc_might`), and an
improved GROUP like Feral Bloodlust is **one** buff under the group's own id, with no children to be
found. So *"if i have harmony + might it will save only might"* and *"if i have group 'feral
bloodlust' it will NOT save the might, fury, vamp"* are both just `NewbieBuffSet.Contains(SkillId)`.

The button shape is the second of the two he offered: **[Save] alone until you have a preset, then
[Custom] [Save] [Delete]**, with Save asking before it overwrites and Delete asking before it deletes.
Saving with no blessings on you is **refused, not stored** — an empty preset would put a [Custom]
button on the window that casts nothing, and then he would have to delete it to get rid of it.

🔑 **PER SUBCLASS**, which is the question the Backlog entry flagged and the 0.94.3 auto-marks bug
already answered: a buffer's preset (no Force — he is his own Force) is meaningless on his warrior
subclass, and one shared list would hand it over on every swap. `Subclass.BuffPreset`, new
`SubclassRecord.BuffPresetJson`. It is **re-filtered against the current `NewbieBuffSet` on load**, not
trusted — that set has been 19, 11, 12 and now 16 inside one month, and a preset saved between two of
those would otherwise keep asking for a buff the NPC no longer sells.

Ids only, no rank, as he ruled: *"npc buffer have the highest grade so only id's not rank"*.

Fixed on the way past: the NPC's accuracy single displayed as **"Accuracy"** while the ladder buff, all
three potions and all three scrolls call the family **"Aim"** — the one place a blessing wore the
stat's name instead of its own. It is `Aim` now; the id is untouched (append-only).

🔴 **New column → delete `Game.Server/game.db` (and `-shm`/`-wal`).**

🔵 One number worth watching in play: 16 against the cap of 20 leaves **four** free slots, not the
eight the trim to twelve bought. The presets are the answer — ten leaves ten, so the NPC set and a real
buffer's groups still fit on one bar — but taking the full sixteen is now a deliberate choice to fill
it.

---

## 2026-08-28 — 0.98.2: his class/race table lands in the CSV README

He wrote a **`## Classes and Races`** table into `docs/data/classes_skills_csv/README.md` — all 24
third classes with their 1st/2nd tiers, race, **weapon**, **armor** and **path**. Checked row by row
against `ClassCatalog` + `ClassNames`: **all 24 pairs match the code.** Four typos fixed (`Raveger`,
`Knighs Commander`, `Holy Messanger`, `Occulist`) plus two double spaces.

🔑 **HIS SPELLING WINS ON `War Storm`.** He wrote it as two words and the code said `Warstorm`; his
form is the consistent one by his own pattern — *War Master*, *War Doctor*, *War Harmonist* take the
space, and only the real English compounds (*Warbringer*, *Warlock*) close up. The code moved.

🔑 **The table carries information the CODE DOES NOT HOLD** — weapon, armor and path per class — which
makes it the most useful thing in that file. Two findings from checking it:

- ✅ **The three buffer rows are already true.** Human takes Heavy (`Chanter Heavy Mastery`), Elf takes
  Light + Bow (`Harmonist Bow/Light`), Demon takes 2-handed blunt (`Bloodchanter Two-Hand Mastery`).
- 🔵 **The warrior SWORD-vs-BLUNT split is new and nothing enforces it** — `Ravager` 2h sword against
  `Warlord` 2h blunt. There is no warrior 3rd-class kit at all (the 2026-08-10 purge took it, and its
  CSVs have not landed), so it reads as a **ruling for when `warrior 3rd.csv` / `war_aoe 3rd.csv` are
  written**, not a description of today. Flagged in the README for him to confirm.

Also repaired the prose around his table, which the rename had left stale: the `Warlord`-is-retired
note still named *Banneret / Galeherald / Skullbreaker* and still said `Sorcerer` was an unfixed slip,
and the per-race register table still worked its example through *Knight → Bulwark → Ironcrown*. The
Demon register is **dread, blood, the abyss** now, not the ork's *bone, blood, endurance* — which is
exactly why the mage lines never worked before `BL-101`.

---

## 2026-08-28 — 0.98.1: the elf AoE warrior joins the set — `Skirmisher → War Storm`

> *"my general idea is anything aoe is War named ... so war dancer was a call that required sword
> dancer before that .. and if we change it need to change it to something war related and its lesser
> part"* → then, on the options: *"Skirmisher → War Storm ... windblade also sounds good but its far
> from vanguard/warborn so skirmisher wins"*

🔑 **HIS "WAR" PATTERN IS REAL AND IT HOLDS ACROSS ALL SIX** AoE/support 4th classes — War Master ·
War Storm · Warbringer · War Doctor · War Harmonist · Warlock. It is written into `ClassNames` now,
with the rule that matters beside it: **the 3rd is that 4th's LESSER FORM, never a word that merely
rhymes with it.**

`Sword Dancer → War Dancer` was the one row on the whole roster chosen backwards — the 3rd picked to
rhyme with the 4th. **`Skirmisher → War Storm`** puts the war_aoe 3rd tier in one voice:
**Vanguard / Skirmisher / Warborn**, three martial POSITION words, the way the nukers are three
elements. A skirmish is what one fighter does; a warstorm is the whole battle. His own reason for
picking it over `Windblade`: *"its far from vanguard/warborn"*.

⚠ Two rejected on sight, both worth recording: `Sword Dancer` sat one letter from a wood-elf unit in
another well-known fantasy game (the naming rule covers that as much as IG), and `Tempest → War Storm`
is semantically perfect but `Discipline.Tempest` is the enum value retired in `BL-97` — **a live class
named after a dead discipline is a trap that costs somebody a build later.**

---

## 2026-08-28 — 0.98.0: `BL-101` THE THIRD RACE IS `DEMON` — and the titles become a ladder

> *"ok lets do the ork-> demon transfer (use the demon names)"* · *"sentinel to stay as class and
> titles to read: supreme being(owner) -> god(admin) -> demi god(mod) -> warden(chat mod) -> player"*

🔑 **THE RACE RENAME EARNED ITSELF TWICE, AND NEITHER REASON WAS SOUNDING.**

1. **`Orc Archer` is already a level-12 MOB.** The player race was sharing its name with common
   trash, from the second hunting ground on. No amount of class renaming fixes that; only this does.
2. **It killed the last naming exception.** The support line had to hide behind `Shaman` because, in
   his words, *"ork priest just dont have the ork sounding"*. `Demon Priest → Dreadcaller → Warlock`
   is a line with a voice, so all fifteen 2nd classes are race+role now with no special case.

🔑 **`Race.Demon` IS STILL VALUE 2.** A character persists the number, not the name, so every save is
the same race under a new label — no migration, no `game.db` reset. Only the identifier and the
display strings moved.

### What the sweep covered, and what it deliberately did not

- **The enum and all 17 code files** — `Race.Ork` → `Race.Demon`, compiler-verified, zero left.
- **162 RACE-column cells across five CSVs.** Safe because `Ork` was only ever a standalone token
  there (checked every occurrence's context first); `git diff --numstat` came back a clean N-for-N
  per file, no reflow. ⚠ The tools never *parsed* that column — `--check` collapses across races — so
  this was about the CSVs mirroring the game, which is the standing rule.
- **The prose in the live docs** (Backlog, design/, Formulas, testing/).
- 🔑 **NOT the owner's own words.** 35 lines carrying a `*"…"*` quote still read "ork", deliberately,
  and so do CHANGELOG, Roadmap and Playtest-Archive — they are records of what was said and when.
- **NOT the mobs.** `Orc Archer` and the orc clan are spelled with a *c* and are monsters; they stay.

### The demon names

Tank **Dread Knight → Abyssal Knight** · warrior **Ravager → Berserker** and **Warborn → Warbringer**
· rogue **Stalker → Venomblade** and **Soultracker → Soulhunter** · healer **Dark Healer → Occultist**
· buffer **Dreadcaller → Warlock** · nuker **Fire Adept → Inferno Master**.

⚠ Of the three IG names I had flagged in his demon column: **`Warlock` stays, and he was right about
why** — the test is word + same race + same ROLE, and theirs is a *summoner* against our **buffer**.
**`Hell Knight` → `Dread Knight`** by his own swap (the safer of his two anyway). `Dreadnought` was
only ever an alternative. **`Juggernaut` went back to the ork** — *"sounds orkish"* — and is now
unused.

### The nuker ladder is the ELEMENT growing up

> *"apprentices -> water/fire/?(something smaller than arcane) adept -> ice or blizzard master /
> inferno master / arcane master"*

**Water → Ice**, **Fire → Inferno**, **Mana → Arcane**. Water hardens, fire swells. 🔑 The human was
the odd one *and he saw it himself*: `arcane` names a SCHOOL, not a magnitude, so there is no smaller
word for it the way water is smaller than ice. **`Mana`** is the raw stuff the art is made of and a
word the player already owns off the blue bar. Took **Ice Master** over his `Blizzard Master` — a very
well-known game company's name, and ice loses nothing.

### The staff titles are one descending ladder

Supreme Being → God → **Demi God** *(was Sentinel)* → **Warden** *(was Silencer)* → player. This is
what closed the clash `BL-100` uncovered: `Sentinel` became the elf archer 3rd class, and a plate
whose whole job is *"this person is staff"* cannot also be a class a hundred players wear. He kept the
class and moved the title. 🔑 `Demi God` is safe next to the deleted `Demigod` CLASS (id 98) —
nothing resolves a title to a class, and the id stays dead.

Also: **human healer 3rd `Light Bringer` → `Holy Priest`**, which ends the `Lifebringer` confusion and
makes the human line a ladder you can hear — Human Priest → Holy Priest → Holy Messenger.

---

## 2026-08-28 — 0.97.0: `BL-100` EVERY CLASS RENAMED — the 2nd tier stops pretending

> *"Also I want to rename the classes (only cosmetics) — now just sound over complicated ... you
> wrote that everything has a meaning ..but I want it simpler ... All races are the same until lvl 40
> so we can call it elf-A human-A .. u can say if you think of something better"*

The full roster is **[docs/design/ClassRenames.md](design/ClassRenames.md)**, which is now the live
reference. Two changes, both his:

1. 🔑 **THE 2ND CLASS (20-39) IS RACE + ROLE NOW** — *Human Rogue*, *Elf Apprentice*, *Ork Knight*.
   He is right that it was a lie: nothing differs before 40 — same kit, same formulas — so a flavour
   name there promised an identity the game does not deliver, and it was spending the six best words
   we owned (`Assassin`, `Sentinel`, `Templar`, `Shadowblade`, `Stalker`, `Champion`) on the one tier
   with none. **All six moved down onto 3rd classes that earn them.** The ork support line keeps
   `Shaman` — his one deliberate exception.
2. **The 3rd/4th say what the class DOES**, in words a player already owns: `Iron Guard`,
   `Sword Master`, `Fire Adept`, `War Doctor`. The coined compounds are gone — `Bladesworn`,
   `Galeherald`, `Bramblewarden`, `Gracebinder`, `Skullbreaker`, `Thornblade`, `Celestine`.

🔑 **COST WAS ZERO.** Nothing persists a name — a character stores the numeric class id — so no save
broke and no `game.db` reset was needed. Quest-item *names* changed (`"{cls.Name} Ordeal Mark"`);
their ids did not. `docs/guides/ItemIds.md` regenerated.

### Four built lines differ from his written list

- 🔴 **Elf warrior is `Swiftblade → Sword Saint`, not `Sword Master → Sword Saint`.** `Sword Master`
  is the human's 4th, and since `BL-97` `ClassNames.DuplicateNames()` has **no exemptions left** — so
  this was a **hard startup failure**, not just the smell he spotted (*"it sounds like the elf warrior
  3rd is stronger than human warrior 4th"*). His Sword-Saint endpoint survives.
- **Ork bow is `Tracker → Hunter`.** His own two rows disagreed — the ork row said `hunter →
  tracker`, the demon row `tracker → hunter`. Took the demon order; *Hunter* is the stronger endpoint.
- **Ork nuker is `Fire Adept → Fire Master`**, the elemental half of his `fire adept/witch` either/or
  — it keeps the arcane/ice/fire pattern and stops "witch" being spent twice in one race.
- **`Adept`, not `Apprentice`, at the 3rd tier** — an apprentice at 40 reads junior, and
  `Ork Apprentice → Fire Apprentice` repeated the word.

### 🔴 A CLASH THE BUILD FOUND: `Sentinel` is already the MODERATOR's title

`Dtos.Text` gives a Moderator the worn plate **"Sentinel"** — his own split ruling, where the RANK
stays "Moderator" everywhere and only the title is fantasy. The elf bow 3rd class is now *Sentinel*
too. A title whose whole job is *"this person is staff"* cannot also be a class a hundred players
wear. **Not resolved — his call.** My read: move the title (`Arbiter` / `Warden`), keep the class.

### On the IP flags — he was right, twice

He corrected two of my four: theirs is *Temple Knight* / *Eva's Templar* against our bare *Templar*
at a different tier, and *Moonlight Sentinel* at their 4th against our bare *Sentinel* at the 3rd.
Compounds at different tiers are not the same class. **Both flags dropped.** `Spirit Elder` became
*Forest Elder* anyway. **Only `Paladin` stands** as a bare exact match (their human tank 3rd, ours
the elf tank 4th) — built as he asked, recorded so the decision stays visible.
🔑 **The rule that came out of it: the test is word + SAME RACE + SAME ROLE, not the word.**

---

## 2026-08-28 — 0.96.0: `BL-97` THE ROSTER COLLAPSES — the Tempest and the Vanguard are retired

> *"Tempests must go .. And elf nuker 3rd is starweaver, ork is cinderwitch and human stays magus"*
>
> *"Remove the vacant tank as well — the 3 tanks must have their name and the other is the same for
> the 3 races ... So is the one that must go"*

The NUKER and the TANK each opened into two disciplines and now open into **one**. Their three
identities are the three RACES — nuker: Human **Magus**, Elf **Starweaver**, Ork **Cinderwitch**;
tank: Human **Bulwark**, Elf **Aegis**, Ork **Ironhide** — which is exactly the shape his 2026-08-17
map asked for: *"same logic as the tank, 1 discipline ... 3 identities"*. **Ten choosable paths per
race became eight, and 30 third classes became 24** — the roster the map drew.

⚠ **And while counting them I found a number that had been wrong in the code comments for months:**
`ThirdClassCatalog` described itself as "the 36 third classes", from 18 second classes × 2. Only
**15** second-class ids are playable (4/10/16 are the retired archers), so it was 30 before this pass
and is 24 after. Corrected everywhere. 🔑 **Count classes by asking the catalog, never by
multiplying** — a derived number agrees with itself forever.

🔑 **HIS TEST FOR WHICH OF A PAIR DIES IS THE KEEPER.** *"The 3 tanks must have their name and the
other is the same for the 3 races."* Both retired disciplines wore **one name across all three
races** — Vanguard/Doomward and Tempest/Skybreaker — precisely because neither was ever really three
classes. The naming table had been recording the answer for eleven days.

⚠ **A retired discipline's NAME is free to reuse** — `Vanguard` is a name, not an id, and nothing
persists a name. Only the enum VALUES must never move.

🔑 **THE THREE NAMES WERE ALREADY EXACTLY THOSE.** `ClassNames` has read Magus / Starweaver /
Cinderwitch since the per-race naming pass of 2026-08-17, so the naming half of his ruling cost
nothing and the whole pass was the retirement itself.

🔑 **AND IT DELETED NO AUTHORED ROW — the `BL-97` entry that warned it would was wrong.** `nuker
3rd.csv` carries no discipline column, so `RegisterNuker3rd` looped over Magus AND Tempest handing
both the *identical* 208-row array. Retiring one removed a duplicate REGISTRATION, not content.
`dotnet run --project tools/SkillCsvSeed -- --check` is still clean, and it now covers the whole nuker
outright instead of checking one of a pair and trusting the other by hand.

### What actually changed

- **`Disciplines.Of` returns a NULLABLE second branch** (`(Discipline A, Discipline? B)`), and both
  the Nuker's and the Tank's B are `null`. An archetype offering one discipline is now a representable
  thing rather than a special case — which is why the Vanguard, ruled hours after the Tempest, cost
  one line.
- **Ids 102 / 114 / 126 (tank) and 112 / 124 / 136 (nuker) are permanently vacant**, with their
  ascensions 202/214/226 and 212/224/236. A third-class id is computed from its PARENT's id
  (`100 + (secondId-1)*2`), never from a running counter, so retiring a discipline left holes and
  moved nothing else. **24 third classes.** 🔴 Those twelve numbers are dead forever.
- **`Discipline.Tempest = 11` and `Vanguard = 1` KEEP THEIR VALUES.** Characters persisted them, so
  the numbers can never be reused; what changed is that nothing mints, names, offers or teaches
  either. Their `ClassNames` rows are gone, their Grandmaster blurbs are gone, and `RegisterNuker3rd`
  registers Magus alone.
- **The Vanguard cost even less than the Tempest.** The 2026-08-10 40+ purge had already taken every
  Vanguard learn line, so it was an empty class that could still be chosen — a level-40 Knight could
  pick a discipline that taught nothing.
- ✅ **`ClassNames.DuplicateNames()` HAS NO EXEMPTIONS LEFT.** Its two were exactly these two
  disciplines. Every remaining row is a real class and every name must now be unique — which is what
  makes that startup guard worth having the next time the names are reshuffled.
- **A saved Tempest is migrated, not orphaned.** `ThirdClassCatalog.Surviving` (and its 4th-tier
  sibling) maps a retired B slot onto its surviving A sibling, applied on both load paths **and** on
  the character-SELECT list. Without it such a character would have shown no class name, learned
  nothing above 40 and never ascended at 76 — bricked, not cosmetic. The rule is positional, not a
  table of literals, and the next autosave writes the corrected id back.
- **Twelve quest items went with the six classes** (1080 → 1068): the `_token`/`_proof` class-change
  proofs are generated FROM `ThirdClassCatalog.Playable`. `docs/guides/ItemIds.md` regenerated.
- **`tools/SkillCsvSeed` seeds `nuker` from Magus alone**, so the two duplicate names that folding
  two kits into one file produced — FlameBolt as both *Annihilate* and *Chain Lightning*,
  GreaterWeakness as both *Mana Burn* and *Maelstrom* — are no longer his to reconcile.
- **`BalanceMatrix`'s uniqueness demo was rebuilt around what is left to prove.** It used to show a
  Tempest still OK beside a barred Magus; there is no such escape now, so it walks all three nuker
  2nd classes and shows the CROSS-RACE bar (a human Magus bars the elf's Starweaver and the ork's
  Cinderwitch — different ids, same path) with the healer line open beside it for contrast.

⚠ **"Two nukers" is no longer a legal subclass pair.** The one-per-discipline rule is unchanged, but
with a single nuker discipline a second Sorcerer/Inquisitor/Witch would have to walk the Magus twice,
and `CanAddDiscipline` bars it. The mage's four slots are Magus + Lightbringer + Warchanter and then
a non-mage. That is the ruling working, not a regression.

🔴 **A NEW APK IS NEEDED.** The client builds its class-change and Learn lists LOCALLY from the
compiled `ClassSkills`/`ThirdClassCatalog`, so an old APK would still offer the Tempest.

---

## 2026-08-28 — 0.95.0: `BL-98` THE BOSS'S JUDGMENT (a six-rung ladder nothing removes) + `BL-99` raid-lock

> *"Rename the petrify as 'bosses judgment' make it 6 lvls. L1 is 3min petrify state, after 3mins end
> u get 1h L2..if you hit a boss who's lvl is 9 lvl or more different u get L3 a petrified state for
> 30min … u cycle l5<>L6 until u stop for 24h and the start form l1 … This curse is unremovable..no
> cleanse no healers whatever nothing .. It's a punishment."*

```
rung  what it is        lasts    runs out into    offend while holding it
L1    PETRIFIED          3 min   → L2             (cannot act)
L2    remembered         1 h     → clean          → L3
L3    PETRIFIED         30 min   → L4             (cannot act)
L4    remembered         1 h     → clean          → L5
L5    PETRIFIED          2 h     → L6             (cannot act)
L6    remembered        24 h     → clean          → L5   ← the cycle
```

🔑 **THE RUNGS ALTERNATE: ODD = PETRIFIED, EVEN = REMEMBERED — and that is what makes the ladder
work.** You cannot act while frozen, so an odd rung needs no "offend again" rule at all; the even
rungs carry **no effect whatsoever** and exist purely as the window in which a repeat costs more. The
only way off the top is to let a full 24 hours pass un-offended, which drops you to clean and starts
the next offence back at L1. The gap that triggers it is unchanged at **±9**.

🔴🔑 **"UNREMOVABLE" IS ENFORCED BY THE ARCHITECTURE, NOT BY A FLAG — and it had to be.** Four
separate paths in this codebase wipe a buff list: **death**, a **subclass swap**, and two more. Every
one of them would have been a way out if the buff were the state. It is not: `Entity.BossJudgmentRung`
is the truth, `IsStunned` and `HpFrozen` read **the rung directly**, and the buff is only the icon and
the countdown — re-asserted once a second by `TickBossJudgment`. So a cleanse, a death, a swap or a
bug can all remove the buff and achieve exactly nothing.

🔑 **PETRIFIED STILL NEEDED NO NEW MACHINERY — IT IS `Stun` + `FreezesHp`.** "Cannot act" is exactly
what a Stun buff already means (gates the cast, breaks one in flight, drops the queued and chained
skill, zeroes move speed); "doesn't take any dmg" is exactly the Immortality Sigil's HP freeze,
already checked inside `ApplyDamage` and `HealOne`. That also settled the enum problem before it was
one: `SkillEffect` has **no free bits left** (`1L << 62` is the last and it is taken), so a `Petrify`
flag was never on the table.

**The ladder keeps running while you are offline, and the walk is EXACT.** An L1 that ended at T means
L2 ran T→T+1h, so logging in 90 minutes later leaves you **clean**, not starting a fresh hour of being
remembered. `WalkBossJudgmentClock` advances as many rungs as the elapsed time bought. Waiting it out
logged off has to cost precisely what waiting it out logged in costs, or logging off *is* the answer
to it. ⚠ No transition ever *tightens* on expiry — a petrifaction only ever falls to its memory rung —
which is why a once-a-second clock is accurate enough.

**Where it fires — two seams, not twenty**

- **Hostile:** `AddThreat`. Every hostile act a player can aim at a creature already arrives there —
  a landed hit through `ApplyDamage`, and anything that lands *no* damage (a taunt, a cancel, a
  resisted debuff) through `Retaliate`, which `AfterOffensiveSkill` always calls. ⚠ One caller had to
  be exempted: the **aggro pull** puts a player in the table for something the *mob* did, so it passes
  `byPlayerAct: false` — it still marks him as in the fight, it never judges him for being noticed.
- **Support:** the new `OnSupport`, which replaced all seven `FlagForSupporting` call sites (heal, MP
  restore, buff ×2, resurrect ×3). 🔑 One seam rather than two calls at each site, **because a
  one-of-two miss is the exact shape of the hole BL-98 was raised about** — `RaidLevelGapMult` had
  priced *damage* to a boss by the level gap since the ±10 rule and nobody ever mirrored it onto the
  help.
- "Fighting a raid boss" = the helped player is in that boss's **threat table**, a claim valid for 30s.
  The marker lives on the *player*, so the support path is a field read, not a scan of every boss.
- ⚠ **An AoE that reaches two participants costs ONE rung, not two.** Already-petrified is a no-op and
  `BossJudgment.OnOffence` holds the same line as a second guard — otherwise one area heal would have
  jumped someone from clean to two hours.

🔑 **AND IT LEAVES EVERY THREAT TABLE.** Without that the punishment hands the raid a *better* exploit
than the one it closes: an over-levelled friend gets himself petrified on purpose and the boss spends
two hours swinging at a target that cannot be damaged. Frozen means out of the fight in both directions.

⚠ **New columns (`BossJudgmentRung`, `BossJudgmentUntilUtc`) → delete `Game.Server/game.db`.**

### `BL-99` in the same version — a raid participant is unhelpable by outsiders

> *"If you are boss engaged nothing can heal you outside your party… Game don't punish you with area
> heal the target is unhelable by outsiders. If heal/partyheal/areaheal comes for a party member u
> take the benifit .. And for the healer ...it never reach the pipe so never punish him. And if he
> tries to heal with a single/target heal he is deliberately trying an exploit and it's punishable."*

🔑 **TWO RULES THAT DIVIDE THE WORLD CLEANLY BETWEEN THEM.** `BL-99` gates on **party membership**, at
any level; `BL-98` gates on the **±9 level band**, inside the party. An outsider cannot help a raid
participant *at all*; a party member can, and is judged if he is far outside the boss's band. Which
means the "aided the raid" cause can now only ever fire on a party member — the original exploit in
its pure form, someone who joined the party to carry a raid he towers over.

🔑 **THE SPLASH IS SKIPPED, THE AIM IS PUNISHED** — his distinction, and it is the one that makes the
whole thing safe to be near. An area heal filters the locked target out before it lands (*"it never
reach the pipe so never punish him"*), so standing near someone else's raid can never cost you
anything. Aiming a single-target heal, cleanse, buff or resurrection at a locked raider is refused
**and** judged, at cast start, before a tenth of a second has run — **the flag is on the reach, not on
the heal**, exactly the shape `BL-77` gave hostile acts and the resurrect flag already had.

⚠ **A refusal, not the usual fall-through to self-cast.** Every other unreachable support target in
that method quietly becomes a self-cast, which is right there ("act as u r not nearby") and wrong
here: silently healing yourself *and* taking a rung for it reads as a broken skill rather than a rule.

🔑 **MEASURED BEFORE BUILT: only THREE skills in the game could splash onto a stranger at all** —
Urgent Great Heal and the two totems (`FriendlyInRadius` + `PlacesTotem`). Every party heal, area buff
and resurrection field is `AlliesInRadius`, which never leaves the party. So *"we make it a party
one"* was already true of everything except the one skill he deliberately ruled twice to keep open,
and it stays open — it just cannot reach someone else's raid.

⚠ **His *"heal per next -3%"* was for a party-scoped chain heal and is moot now.** The falloff is
already built at **2%** (`TargetFalloff: 0.02f`, 11 targets, 30%→10%); −3% cannot fit 11 slots.

⚠ **Caught while building:** the aimed-cast gate had to mirror the ally branch's own `TargetMode !=
SelfOnly && Range > 0` conditions. Without them a **self-buff** pressed while a raider happened to be
selected would have been refused and judged, for an act aimed at nobody.

🔵 **When an alliance / raid group exists, `RaidLocked` reads "your raid group", not "your party".**
Today a party caps at 9 and a boss is tuned for a 5-man, so one party is the whole raid; the day two
parties are meant to fight one boss together, that line is what would stop them healing each other.

## 2026-08-27 — 0.94.3: the party heals are cast on yourself, and retuned end to end

> *"The party heals (party heal, great party heal, ultimate party heal) should be cast able without a
> target.. So 0/x party/AOE. Let's redo the party heals I haven't saw what ig's was so I just estimated
> ..party heals are 0/1000 1k range around caster"*

🔑 **"CASTABLE WITHOUT A TARGET" NEEDED NO NEW CODE — `Range: 0` ALREADY MEANS THAT.** The support
branch of the cast resolver only accepts an ally target when `def.Range > 0`; at 0 it falls through to
the self-cast branch, which is also how a support skill already behaves when you have an ENEMY selected.
So the mechanic he asked for was one number per skill, and the same number is what the new AOE column
made legible in the first place.

| skill                 | range / aoe  | target        | cast | reuse  |
| --------------------- | ------------ | ------------- | ---- | ------ |
| Party Heal            | **0 / 1000** | party/aoe     | 7s   | **6s** |
| Party Great Heal      | **0 / 1000** | party/aoe     | 7s   | **6s** |
| Ultimate Party Heal   | **0 / 1000** | party/aoe     | 7s   | **3s** |
| Healer Party Blessing | **0 / 1000** | party/aoe     | 3s   | **9s** |
| Urgent Great Heal     | **0 / 1000** | target/aoe    | 3s   | 5s     |
| Heal / Great Heal     | 600 / 0      | target/single | 5s   | **3s** |
| Ultimate Heal         | 600 / 0      | target/single | 5s   | **1s** |
| Healer Blessing       | 600 / 0      | target/single | 3s   | **3s** |

⚠ **HE OVERRODE HIS OWN AUTHORED CSV ROWS AND SAID SO** — *"Let's redo the party heals … I just
estimated"*. `healer 4th.csv` had Ultimate Party Heal at 5s / 2s for rungs 76-90, carried in code as a
per-rung `CastTicks/CooldownTicks` override. That override had to move with the base or the top eight
rungs would have kept the old numbers while 40-75 took the new ones — a split that `--check` would have
reported as eight CSV defects rather than as one missed edit.

⚠ **`BL-96`'s open question is closed by this.** 0.94.2 flagged that a party heal read `600,600` rather
than the `0,600` he sketched, because the range gate really did apply to the targeted ally. His answer
is that the GATE should go, not the column — so the range is 0 and the column is honest either way.

✅ **URGENT GREAT HEAL IS `target/aoe`, CONFIRMED** — the `self/AOE` in his table was a slip and he
settled it in the other direction, with the principle: *"Urgent great heal is 11 targets so it's never
a party one while healer PARTY blessing implies only party :) urgent heal is a safe anyone anywhere"*.
🔑 **The ELEVEN is the argument**: a party caps at 9, so a skill that reaches 11 cannot be describing a
party — the two slots past it are the point. That is what `FriendlyInRadius` was split out of
`AlliesInRadius` for in 0.93.2. The code was already right and is now commented so it stays that way.

🔵 **`BL-98` opened from the same message**: *"The only thing need to prevent is outside help of
high-level healers to a low lvl boss fights .. Mark it so we can deside what to do"*. 🔑 The
anti-cheese curve for this ALREADY EXISTS on the other side — `RaidLevelGapMult` scales a player's
DAMAGE to a boss by the level gap and was never mirrored onto SUPPORT, so an over-levelled character
was stopped from killing a low boss and never from healing the people who do. Four options on the
entry; the open question is what counts as "in the fight".

## 2026-08-27 — 0.94.2: the AOE column, and a "generated" file that was half hand-maintained

> *"Elemental wave - 200 AOE around caster 0 cast range - maybe enemy/AOE if we have the two range
> columns then they will be 0,200 and that will work"* · *"remove the rune porting mob it's to op for
> a normal zone +120% pDef to op"*

### `BL-96` — RANGE and AOE are two columns now

His proposal, and his go-ahead once he saw what it fixes. `LEARN, NAME, TYPE, RANGE, **AOE**, TARGET, …`
across all 24 files, 1,425 rows, by a new `SkillCsvSeed --aoe-column`.

🔑 **RANGE used to mean two different things** — "how far can I throw this" for a nuke, "how wide does
this go off" for a party heal — so it could not be compared against any single code field, and the
**radius was never a checked number**, only prose in DESCR. It is checked now, against
`SkillDef.AreaRadiusAt`, on every rung.

It also dissolves a contradiction between two of his own rulings: 2026-08-27 said the TARGET column
deliberately does NOT encode where the circle sits; 2026-08-28 called Elemental Wave `self/aoe`, which
encodes precisely that. With two columns neither has to carry the other's meaning. Elemental Wave is
**`0,200,enemy/aoe`** — his own worked example — and Arcane Wave **`900,400,enemy/aoe`**.

⚠ **A party heal reads `600,600`, not his proposed `0,600`.** The range gate really does apply to the
ally you target (`SkillMath.EffectiveRange`, checked at cast start for any non-self target), so 600 is
what the game does and the column now says so. Zeroing it is a behaviour change, not a column change —
left for him.

⚠ **THE MIGRATION FAILED ONCE, INSTRUCTIVELY.** It first copied `--retarget`'s habit of stopping at his
`NOT DONE` banners. But `--retarget` rewrites the MEANING of a cell, where this inserts a COLUMN — and
`--check` reads by index, so leaving half a file un-shifted gave it **two schemas** and produced 1,259
invented discrepancies, every field compared against its neighbour. 🔑 **A SEMANTIC pass may skip rows;
a STRUCTURAL pass may never. A file has exactly one shape or it has none.** Draft rows past the banner
now get the column with an empty cell.

Two more traps it walked into and now documents: the lookup must key **name → every rung** (an exact
name+learn-level key missed 46 rows, because a 4th-tier row re-teaching a 3rd-tier skill does not share
its learn level), and `ClassSkills.Cumulative` **needs its `fourth` flag** or the entire 4th tier is
invisible — the same trap `Check.Specs` already documents.

### The Rift Portling is gone — and so is a lie in the mob CSV

*"remove the rune porting mob it's to op for a normal zone +120% pDef to op"* — `rift_portling`, whose
`MobMod.PDef: 2.2` **is** that +120%. Deleted; nothing referenced it.

🔑 **How a "CHAMPION outlier" ended up in a starter field is the part worth keeping.** Rosters are
DERIVED from the level band (`MobCatalog.InBand`), so a champion template with a natural level of 40
is rostered into every generated 40-44 camp automatically. Nothing marked it special, so nothing kept
it out. **An outlier needs `HandPlaced: true` or it is not an outlier, it is just a very hard normal mob.**

⚠ **And `--dump-mob-csv` could not have told us.** It walked the EXISTING file and refreshed columns
4-9 in place, so the row LIST was hand-maintained while only the stats were generated: the deleted
Portling kept its row and the four `BL-79` guards, added the day before, never got one. A reference
that is "regenerated from the code" but silently keeps whatever roster it already had is 0.93.1's
hand-copied interrupt table again — **it can never contradict you, because the half that would have
disagreed is the half nobody regenerates.** It rebuilds from `MobCatalog` now, preserving ids by name.

## 2026-08-27 — 0.94.1: AoE actually hits, and auto-on stops leaking between subclasses

> *"AOE don't work ..it shows the red circle pulse but tont hit the mobs ...also the arcane wave
> should AOE around the mob not the player like elemental wave"* · *"I'm buffer and have in skill belt
> the atack as auto on .. Then I change to/add new subclass ..and I put atack on belt it's auto-on
> from the getgo … if I have never put a skill on bar and haven't never make it auto-on it should
> never be auto on"* · *"relog in etc never clears the chat ...and it should"*

Server-side except where noted. ⚠ **`game.db` MUST BE DELETED** — new `Subclasses.AutoSkillsJson`
column, and `EnsureCreated` never adds columns to an existing file.

### AoE WAS NEVER HITTING ANYTHING — two bugs, stacked

🔑 **No player offensive AoE has ever swept.** The sweep runs only for `TargetMode.EnemiesInRadius`
and **only mob spells ever set it**. Elemental Wave and Arcane Wave — the only two player attack AoEs
in the game — authored a radius but no mode, so they drew their circle and hit the single target.
That is the whole of his report, and the circle is the reason it looked cosmetic: the display half was
the only half that worked.

⚠ **And a second one underneath it that would have re-broken it later**: the sweep read
`def.AreaRadius` (the SkillDef field) while the circle read `def.AreaRadiusAt(lvl)` (the per-rung
value). A skill authoring its radius PER RUNG therefore got a **zero-radius sweep under a full-size
circle** — the identical symptom, lying in wait for the next such skill. Both read `AreaRadiusAt` now.

🔑 **Fizzle and magic crit had to be added to the shared hit path for players.** `DeliverSimpleHit`
was written for mob spells and traps, where neither exists; routing a mage's AoE through it unchanged
would have quietly deleted both halves of the magic channel from every area spell he owns — an area
nuke strictly more reliable than the single-target one beside it, and a WIT build that stops paying
the moment he presses AoE. **Gated on a player attacker on purpose**: giving creatures fizzle and crit
here would retune every boss as a side effect of a bug fix that never meant to touch bosses.

### The two waves are different SHAPES again

New `SkillDef.AreaAtTarget` says where the circle sits; `EnemiesInRadius` takes an origin; and the
circle broadcast moved below the target resolve so the drawing and the damage come from ONE decision —
it could only ever centre on the caster before, because it ran before `target` existed.

- **Arcane Wave** is `AreaAtTarget: true` — range 900, radius 400, centred on the mob. He asked
  whether 400/900 were swapped: **they were not**; only the centre was wrong.
- **Elemental Wave** range **200 → 0** (*"self/aoe with 0 range"*), radius 200, centred on the caster.
  Its 14 CSV rows moved in the same commit.

🔵 **One conflict left to him, filed as `BL-96`**: he called Elemental Wave `self/aoe`, but on
2026-08-27 he ruled the TARGET column deliberately does NOT encode where the circle sits. Both cannot
be true of one column — which is the argument for the `AOE RANGE` column he proposed in the same
message. The CSV keeps `enemy/aoe` until he rules; the game behaves as he described either way.

### Auto-on marks are PER SUBCLASS now

The skill BAR was class-level and the AUTO marks that arm it were character-level, keyed by skill id
with no slot and no class — so any subclass whose bar happened to hold that id painted it armed.
⚠ **Filtering the shared list on swap could not have fixed it**: it is one list, so pruning it for the
incoming class destroys the outgoing class's marks for good. The marks moved to `Subclass`, with a new
`SubclassRecord.AutoSkillsJson` column, and `ActivateSubclass` now pushes `SendAutoHuntConfig` —
without that push the storage fix is invisible, because the client paints from its own flat mirror.

⚠ **Same bug as playtest-17 B1, one level down.** That one leaked marks between CHARACTERS and was
fixed by clearing them on character change; a subclass swap never leaves the world, so it never went
through that fix. The split was even documented at the top of `Subclass.cs` — "skill-bar layout"
class-level, "auto-hunt settings" character-level — one feature straddling the line. That sentence
was the bug; it now says which half is which.

The rest of the auto-hunt config (enabled, potion thresholds, buff potion ids) stays per character on
purpose: those are preferences about how you play, not about this class's kit.

### Filed, not built

- **`BL-95`** buff presets — two built-in (fighter / mage) plus custom sets saved from what you are
  currently wearing; NPC-buffer skills only, ids without rank, per character.
- **`BL-96`** the `AOE RANGE` column (above).
- **`BL-97`** one nuker per race — Tempest vs Magus. The factual answer to his question: there is no
  stat difference at all (a class grants no stats, 2026-08-10); they are two authored KITS of one
  archetype, both already written across three races in `nuker 3rd.csv`. So "one must go" deletes
  authored rows and touches the 4th-tier ascension — his call which survives.
- 🔵 **CHAT ON RELOG IS NOT CHANGED, DELIBERATELY.** He asked for it to clear; the current behaviour is
  a feature he explicitly asked for in playtest 28 (*"chat again is saved between logins. Don't
  reset"*), reversing his earlier `C1`. `ClientLog` files the log per character and nothing ages it
  out, which is exactly why "3h later" still shows it. Three options put to him on the checklist
  rather than silently reversing a ruling.

## 2026-08-27 — 0.94.0: the watch is posted, and three Kind-gates that hid a whole class of bug

> *"bl-79 -> try make town guards and one archer(overenchanded) in several zones … killing a guard
> dont give karma nor flags … to match a 80 lvl player S grade equip (no nenchanted) / pieasfull zone
> guards have everithing s grade +16 and are 90lvl -> town 80lvl S grade +0"* · *"if we treat guards
> as mobs give them mob passives … If we treat them like a player give them classes so they atleast
> have the player stats"* · *"Field guard they are like a guard tower … Faster stronger almost 1 shot
> a pk"* · *"pk killing guards only enters town so he can be safe from other players the npcs still
> refuse trade"* · *"they should ware a robe not light … don't give them +8 evasion"* · *"respawn time
> for guards should be 60/90s .. Field guard is 1-2s (if ever killed)"* · *"fix the caster mobs .. the
> 15k mobs are zone placed with x2/x3 hp .. some zones can have x1"*

Server-side only — no protocol change, so an installed 0.93.x client plays all of it.

### 🔑 THREE `Kind == EntityKind.Player` GATES THAT SHOULD HAVE BEEN `PlayerBuilt` (`Entity.cs`)

The headline, and it is not the guards. `BL-47` added `PlayerBuilt` meaning "take the player side of
the stat formulas", and the FORMULAS were duly switched to `playerStats`. But the code that **feeds**
those formulas kept asking `Kind == Player`, because each piece predates the flag and none of it
looks like a formula. A player-built creature was running player MATHS on mob INPUTS:

- **`:2008` the armour SET bonus** — a creature in a full matched set got none of it, and since the
  2026-08-19 ruling folds a set's Str/Int into `BonusAtk`, that is the POWER stat.
- **`:2029` a passive's STAT bonuses** (`BonusAtk`/`BonusCon`/…) — and this one runs BEFORE the attack
  and pool formulas, so exclusion here excluded the creature from everything they produce.
- **`:2558` the armour-weight MASTERY block** — a creature could LEARN an armour mastery and this
  would then decline to read it, so a fully kitted tank stood in untrained numbers.

Every symptom looked like bad tuning rather than a missing branch. **Grep `Kind == EntityKind.Player`
in `Entity.cs` before trusting any player-built number.**

### `BL-79` — the guards, and they carry NO invented multiplier

His fork — *"treat them as mobs … or treat them like a player"* — resolved to the player route,
because his calibration target is a player. `MobBuild.LearnsKit` teaches the PASSIVE half of the class
kit (weapon, armour and shield masteries; the actives are excluded so a guard never casts, per *"they
dont use skills"*). The town pair's power is now **entirely class kit + gear**:

| level 80, S+0 Epic          | HP     | P.Atk     | P.Def     |
| --------------------------- | ------ | --------- | --------- |
| the reference player (tank) | 10,737 | 1,214     | 1,101     |
| `guard_town_tank`           | 9,969  | **1,158** | **1,101** |

A near-exact mirror, which is what *"match a 80 lvl player S grade equip"* asks for. Time-to-kill runs
**105s / 124s** against an S+0 warrior — his "hands full". The FIELD pair is the *"guard tower …
almost 1 shot a pk"*: a `GuardTower` passive off the elite rank's own rungs, leaning on attack rather
than health, killing that warrior in **16-30s** while taking 304-590s to die.

⚠ **THE FIRST ATTEMPT AT THIS SHIPPED A NUMBER TUNED TO A BROKEN MEASUREMENT, AND THAT IS THE LESSON.**
It gave the guards a hand-picked `WatchTraining` block (P.Atk ×4.2, P.Def ×3.0, …) tuned until the
table looked right. The table was wrong: `--guards` built its reference player with `BuildPlayer`,
which **already equips a full best-for-tier set**, then equipped an S+0 Epic set on top — and nothing
in the tool enforces one item per slot, so the "player" wore 22 equipped pieces at roughly double a
real one's stats. The guards were always closer to his target than the tool claimed. A multiplier
fitted to a bad measurement is worse than no multiplier: it is wrong AND it looks deliberate.
`WatchTraining` is deleted; the reference player's inventory is cleared before dressing.

Also: **respawn is his** — town **75±15s** (his 60/90), field **1.5±0.5s**, *"if ever killed"*. Eight
posts (five city gates, three quiet fields), karma-keyed aggro (a PK and nobody else), PvP-on as the
GATE to attack one, and no exp/drop/quest-credit/karma-shed for the kill.

### `BL-79`'s other half — an outlaw gets no SERVICE

*"pk killing guards only enters town so he can be safe from other players the npcs still refuse
trade"* — a better rule than "a pk cant use npcs" read literally, and it is why killing the watch is
still worth something to a PK: the safe ground is real, the town is not. One chokepoint
(`NpcRefusesService`) on **ten** handlers — vendor buy-back, teleport, buffer, SP broker, warehouse in
and out, class change, profession, mindwriter, stat re-roll. Only buying was gated before.
**Selling stays exempt** (his earlier ruling): being red is meant to be expensive, not to strand you
with a full pack. A merely FLAGGED player keeps every service.

### `BL-78` item 1 — the HP multiplier, and it is the ZONE's

⚠ **HIS RULING OVERRODE THE FILED PLAN** (per-template `MobMod.Hp` authoring). The ZONE carries it, so
the same creature reads ×1 in one field and ×3 in another with no template edited. One derived ladder
(`WorldPlan.HpScaleFor`): **×1 below 40, ×2 from 40, ×3 from 61**, overridable per field.
`MobBaseStats.Hp(80)` = 5,160, so ×3 = **15,480** — his *"15k not 5"* exactly. ⚠ A **boss ignores it**
(0.89.0 measured every boss into the 12-25 min band off a curve bosses derive from); an **elite does
not**. It multiplies HP and nothing else.

### `BL-78` item 2 — a caster wears a ROBE

🔑 **The backlog was wrong about the cause**: it said a caster "pays twice (low P.Def AND low HP)";
`MobRole.Mage` never touched HP. The double-dip was DEFENCE — the role's ×0.7 compounding with the
template's own `MobMod.PDef`, worst at `watcher_eye` (0.5 × 0.7 = **×0.35**, nearly three times less,
not "a bit"). Role is now **×0.85**, and `watcher_eye`'s own P.Def moved 0.5 → 0.8.

⚠ **AND THE FIRST FIX OVERREACHED — he corrected it.** It also gave the Mage role Archer's **+8
evasion**, reasoning from IG's `Light Armor Type` tag. His ruling: *"they should ware a robe not light
… don't give them +8 evasion so they be missed and hit for 300"*. A robe is not light armour, and a
caster that also DODGES turns a soft target into a coin-flip. Evasion is the archer's trade. Reverted.

### Also

- `MobBuild` can carry a **shield** (one-handed builds only); `MobType` gains **`Guard`** and a
  per-template **`AggroRange`** (his 400 melee / 600 archer).
- Guard posts are **exempt from the rogue-spawner boot guard** — a town post stands just outside a
  city's safe radius, outside every field polygon by construction.
- `BalanceMatrix --guards` builds both sides through the real paths, including the **held rune buff**
  the spawn path applies (omitting it handed the player a silent +100% P.Atk).

## 2026-08-27 — 0.93.2: the TARGET column becomes `[scope]/[breadth]`, and a real Unity guide

> *"the logic is [self-onlyMe/target-anyFriendly/party-anyPartyMemeber/enemy]/[single-affectOne/
> aoe-affectsMany]"* … *"do for all files"* … *"--check should read target after i check them and if
> i made a change"*

### THE COLUMN

Every skill CSV's `TARGET` column is rewritten into his two-part scheme. **1,268 rows across 18 files**,
and the totals now read:

| value           | rows | value        | rows |
| --------------- | ---- | ------------ | ---- |
| `self/single`   | 433  | `party/aoe`  | 153  |
| `party/single`  | 326  | `target/aoe` | 42   |
| `enemy/single`  | 226  | `enemy/aoe`  | 28   |
| `target/single` | 155  |              |      |

Done by a new **`dotnet run --project tools/SkillCsvSeed -- --retarget`**, not by hand and not by
find-and-replace, because the old column **cannot answer the question**: `self/target` collapsed both
`party/single` (a buff) and `target/single` (a heal), and `enemy` collapsed single and AoE. The tool
asks the **catalog** — `TargetMode`, `AreaRadius`, `PlacesTotem`, the effect flags — so a row can only
be wrong here if the code is wrong too. It rewrites **field 5 as a character span** on the raw line,
never re-serialising: line counts and CRLF counts are byte-identical to before on all 18 files.

⚠ It stops at his **`NOT DONE`** banners exactly as `--check` does, so the unfinished halves of
`buffer 4th` and `nuker 4th` were not touched.

### `--check` NOW COMPARES IT

His instruction, and it is what makes his review checkable: after he edits a row, `--check` says
whether the engine agrees. It found three real defects **in my own derivation** within a minute of
being pointed at the files:

- **67 passive rungs** came out `party/single` — a passive sets no `TargetMode`, so it inherited the
  record default and fell through the friendly branch.
- **Healing Totem** came out `self/single`: `SelfOnly` is about who you CAST on, not who you REACH, and
  it was tested before the radius.
- **Sprint** — a genuine **name collision**. Three skills are called "Sprint" (the rogue's skill and
  the two buff squares it grants) and the tool took the wrong one. Class-table skills now win the name,
  which is how `--check` resolves it, so the two tools cannot disagree by construction.

### 🔑 `TargetMode.FriendlyInRadius` — THE SPLIT HIS RULES FORCED

A party heal and a totem were **the same enum value**, so the engine could not express the line he
draws between them: *"harmonies are party/aoe … (party heals are party heals)"* against *"urgent great
heal and totems are target/aoe -> anyone friendli in a radius"*. Two skills that reach different people
cannot share one mode.

- **`AlliesInRadius`** = `party/aoe` — the harmonies, Party Heal, Party Great Heal, Resurrection Field.
- **`FriendlyInRadius`** = `target/aoe` — Urgent Great Heal and the totems: **anyone friendly, party or
  not**.

`PlayersInRadius` and `AlliesAroundPoint` take the scope as a flag; `AreaSupport(def)` and
`FriendlyScope(def)` are the two helpers the five area-support branches read, so a sixth branch cannot
invent a different rule.

### THE PVP RULES, from IG's Ctrl key

> *"in ig if i try to help flagged playe I hold ctrl -> thats allow pvp (our pvp-on) so if i want to
> heal faged party member i click pvp-on and resurect/heal while i get flagged as well"* ·
> *"everithing lfagged party works only in party"*

`CanAreaSupport` — a **clean** player is reached party or not; a **flagged or PK** one only from inside
their own party **and only with PvP ON**. The flag is still paid afterwards (`FlagForSupporting`,
`BL-59`): **the toggle is the gate, the flag is the price**, and he asked for both.

⚠ Deliberately NOT the same predicate as `CanSupport`. That governs a cast you AIMED, and lets a clean
caster help a flagged party-mate at the cost of a flag. This governs a splash you did NOT aim, where
the same permission would flag you for standing in the wrong place.

⚠ **Two of his four rules were already built** and needed nothing: negative AoE is `BL-77`
(`EnemiesInRadius`), and single-target positive is `BL-59` (`CanSupport`).

### THE UNITY GUIDE

*"i need step by step guide (for an idiot with unity -> click here -> click here -> click there) ..
your current explanation is to generic"* — fair. The old §"Dropping in a model" said things like *"drag
the model into the scene, add an Animator with a controller, drag it back"*, which assumes you already
know Unity.

Replaced with **twelve numbered steps** (0-11) that name the window, the tab, the dropdown and the
button for every action — from downloading a `.fbx` off Quaternius to seeing it on the phone — plus a
**"When it doesn't work"** table mapping each symptom to its step. Every claim was checked against the
client rather than carried over: the fallback chain against `EntityManager.cs:348-382`, the four
Animator parameters against `EntityView.cs:568-650` (all four really are driven), and the feet-at-y=0
rule against `RefreshModelOffset`.

## 2026-08-27 — 0.93.1: the fourteen-ruling backlog pass, and the last healer 4th row

> *"thats everithing in the baklog i can answer for now"*

He answered **fourteen backlog entries in one message**. Nine closed, two were rewritten, and three
turned out to be **stale rather than open** — the code had shipped days earlier and the file had not
caught up. Two things were actually built out of it, plus the row he pointed at mid-pass.

### URGENT GREAT HEAL @83 — the last unbuilt row in `healer 4th.csv`

> *"urgent great heal in healer 4th is placed/authored u can look at it as well"* … *"so if we make it
> the 10 moost injured around the caster and the caster is 11th that make it 30~10% heal"*

`SkillCsvSeed --check` had exactly one 🔴 in the whole repo and this was it. Now built, and the file is
green end to end.

**It is a TRIAGE heal**, which is a shape the engine did not have. Every area heal before it paid the
same amount to everyone it reached; this one picks **who** and pays each successive target **less**:

- **`SkillDef.MaxTargets`** (0 = everyone, as before) caps an `AlliesInRadius` effect at **11** here —
  the ten most injured allies plus the caster, which is where his *"→ 10%"* tail comes from. At ten
  slots the ladder stopped at 12% and the last rung had no source.
- **`SkillDef.TargetFalloff`** is the percentage the heal loses per rank, `0.02f` against a `0.30`
  magnitude: **30 / 28 / 26 / … / 12 / 10**.
- **The ordering is the skill.** Targets sort by the FRACTION of HP each is missing, worst first —
  not by raw HP lost. A 15k tank at 60% is in more danger than a 3k mage down the same 6,000 points,
  and raw-HP ordering would hand every slot to whoever has the biggest bar. The caster is in that
  ordering like anyone else (*"caster is always healed - just placed based on injury"*), not pinned
  to a slot.
- Power 0: it is a pure **% of the target's own pool**, like Urgent Heal, which it `Replaces`. That is
  why a level-83 button is still the right size for a 15k tank and needs no rung after it. 500 MP,
  3s cast, 5s reuse, 900 radius, **5 Skill Stones** a cast. Learned by all three races.

⚠ **The eleventh slot cannot be reached today.** `PlayersInRadius` is party-only and a full party is
**nine**, so the span in actual play is 30% → **14%**. The cap is authored at his number anyway so the
ladder is already correct the day "friendly" widens past the party.

### `BL-23` — the coin curve, MEASURED instead of asserted: `--goldflow`

> *"i want potion/rune per hour consumation and golddrop/h .. to compare for fewe lvl rangees - for
> now at lvl 43 i have 5kk + gold so it dont seem like a problem"*

`dotnet run --project tools/BalanceMatrix -- --goldflow`. Real drop tables × real vendor prices × real
damage formulas; the only invented number is **5s of pull/travel between kills**, and it is named at
the top of the function rather than buried.

| what it measures              | what it found                                                                                                                                                 |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **his own data point**        | at 43 a farmer nets **740k–1,010k gold/h** — his 5kk is 5-7 hours. The model and his save agree with nothing tuned.                                           |
| **potion burn**               | **0-3% of income** at every band 20→76; 10% in the single worst case (level-85 nuker). There is no potion economy to fix.                                     |
| **rune upkeep**               | a 1h War Rune is 150k flat: **2.4-2.9 hours of rune per hour farmed at 20-30**, 25 at 61, 37 at 85. A **newbie tax that evaporates**.                         |
| **the drift `BL-23` claimed** | real, but **5.4×** across 20→76 (1.68 → 0.31 chest pieces per hour), not the **51×** the entry asserted for a fortnight.                                      |
| **🔴 the finding nobody had**  | a **cliff at 80**. S grade is top-half only — no Common rung exists — so the cheapest level-80 body is **126,000,000**: **26 hours a piece** against 3 at 76. |

🔑 A potion tier has a **ceiling, not just a price**, and the report checks it before pricing anything:
healing Common/Uncommon are 15s on a 10s cooldown (always up: 20 and 70 HP/s), Rare is 30s on 20s
(150 HP/s), and **every mana potion is 15s on a 30s cooldown — half uptime**, so the mana ladder
sustains **10/35/75** against its 20/70/150 label. Pricing a deficit at a tier that cannot physically
deliver it is the mistake the table is built to avoid.

### `BL-91` — the BalanceMatrix interrupt table now READS the catalog

Its four rows were a hand-copied literal, typed in while `nuker 3rd` was unbuilt. They now come off the
real `SkillDef`s — power, cast, reuse and `InterruptMult` — so a retuned rung moves the table with it.

🔑 **That literal is why the entry could go stale while reading as current.** `InterruptMult = 2f` had
been in the code since 0.87.0; the report kept printing its own copy of the number and agreeing with
itself, and the backlog kept saying "not in the code". **A measurement that repeats an authored number
instead of reading it will never contradict you.**

### The rulings that closed without code

- **`BL-94` (fizzle floor) — his own wording was wrong, not the code.** *"failing a spell is 1/3 dmg -
  IG is like that not 0 my wording was wrong."* `damage / 3` stays. Second time a verbatim quote turned
  out to be a phrasing slip; not building it was correct.
- **`BL-10` (bow-caster floor) — deliberately no floor.** *"casting down with a bow is a choice."*
  ⚠ `BL-09`, the **wrong-weapon** floor, is a different entry and is still open.
- **`BL-12` (enchant scaling) — already his answer.** The bonus is flat **per enchant LEVEL**, so +16 is
  worth sixteen rungs and +3 three — his +16 healer is 5.3× the +3 warrior on the same slot. "The same
  offset for every CLASS" had been misread as "the same total for every enchant level".
- **`BL-16` (heal powers) — the 40+ rungs carry it**, the second of the two exits the entry offered.
  Ultimate Heal 1400→2000, Healer's Power +1000→+2000, and Urgent Heal's 15% % channel are all in his
  own files. The 20-35 numbers stay. A level-35 cleric is not meant to out-heal a group buff.
- **`BL-17` (BuffMagAtk) — *"authored . working system"*.** Code and CSVs agree: Force +25% @25,
  +28% @44, +32% @52.
- **`BL-24` (enchant scrolls) — *"it build ? why blue ?"*** It is built; the 🔵 was holding a place for a
  **conversation**, not for work. 🔑 An entry waiting on a chat looks identical to one waiting on code —
  it should say so in its first line.
- **`BL-54` / `BL-55` (newbie gear) — both already true.** The tutorial hands the boxes out on its
  level-10 and level-15 steps, and the newbie light/robe sets **are** the real starter sets.
- **`BL-86` (shutdown countdown) — the toast is accepted.** *"noticable enoght."* Red text optional.
- **`BL-15` (precision / anti_magic) — re-specced to LEARNABLE passives**, not auto-granted floors, and
  now gated on the warrior/rogue CSVs. 🔔 The reminder he asked for lives on the entry.
- **`BL-90` / `BL-91` / `BL-92`'s ork buffer — stale, not open.** The bursts' `DebuffLandMod`, the ×2
  and the Warchanter's HP Boost (L1-7 at 40-70, all three races) were all already built. His own
  message is what caught them.

⚠ 🔑 **The pass's real lesson:** three of fourteen entries described code that already existed, two of
them since 0.87.0. **When a build closes a dependency, sweep every entry that named it in the same
commit.** A stale 🔴 costs more than a missing one — it invites work that is already done.


## 2026-08-27 — 0.93.0: STACK CAPS — a row has a bottom now

> *"i was wondering if hp/mp pots stack to 99 or 999 and over 100/1000th item creates new row … so u
> cannot have infinity hp pots for the cost of nothing"* … *"make those so we have the system and not
> need to retink it if we change a number"*

Every stackable item now has a **maximum per row**. The (cap+1)-th item opens a **new row** — nothing
is ever destroyed, nothing is refused for being over a cap, and a container still refuses only when it
runs out of **rows**, with the same message it always had.

| category                                         | cap            | items |
| ------------------------------------------------ | -------------- | ----- |
| **Buff scrolls**                                 | **9**          | 17    |
| Buff potions, Scroll of Return, misc consumables | **99**         | 37    |
| Enchant + attribute scrolls                      | **99**         | 24    |
| Boxes + blueprints                               | **99**         | 63    |
| **HP / MP potions**                              | **999**        | 8     |
| Materials                                        | **9,999**      | 30    |
| Quest items                                      | **uncapped**   | 108   |
| Gear and anything with per-instance state        | does not stack | 793   |

Read that table off `dotnet run --project tools/BalanceMatrix -- --stacks`, which prints it from the
catalog along with what a farm trip costs in rows. It is not a hand-kept list: **the cap is DERIVED
from the item's category and authored nowhere**, so retuning one is a single edit to `StackLimits` —
his condition when he ordered the feature. `ItemDef.MaxStackOverride` exists for the item that has to
disagree with its whole category, and nothing uses it yet.

**Why the caps are not one number.** A stack cap prices a consumable in bag rows, and a row is only a
real cost for something you carry a long time without spending. Potions drain at up to 120 drinks an
hour while loot fills the bag behind them, so their row count peaks at hour zero and falls — a whole
day of drinking is 3 rows out of 250. What actually prices potions is gold (0.92.1). A fully-buffed
player burns **17 scrolls an hour and their row count stays flat**, so at 9 a stack the pile is visible
within a session and keeps growing. That is the one cap here a player will ever feel, and it is the one
meant to be felt — his own reasoning: *"having 99 of each is indefenetily buffed … while having 10 is
10h of buffs"*.

**Boxes stack now** (they never did, apart from blueprints). Safe because a box carries no state and
`HandleOpenBox` already decremented a quantity rather than dropping the row.

**One rule, one implementation.** All of it lives in `Game.Server/Simulation/Stacking.cs`, and every
container asks it: bag, private warehouse, account warehouse, trade, drops, craft output, quest
rewards, vendor buy, buy-back and the death-restore list. A cap one container computed its own way
would not be a cap, it would be the laundering route around one. Three consequences worth knowing:

- **Merging is by IDENTITY, not by DefId.** Two rows merge only when swapping them would be
  undetectable — same enchant, no expiry, same picks left, same bound/renamed/price overrides. The old
  test compared DefId alone, which was already wrong for a bound or renamed instance and would have
  let a fresh Blessing Box absorb one with 4 picks left. Runes and timed items no longer count as
  stackable at all: one row per acquisition is the only way two clocks stay two clocks.
- **The trade room-check is now a simulation**, not a count. It used to track "does a stack of this def
  survive here" as a yes/no, which was exactly right while a def meant at most one row. It builds the
  bag as it will be and runs the real placement over it, so the check and the move cannot disagree.
- **The account-warehouse fee follows rows.** 10k buys a slot, and a deposit that needs three slots now
  costs 30k and says so. Nothing changed about the rule; it simply never had to open more than one.

**Shops sell at most one stack per purchase** (*"max shop buy = 1 stack"*) — the old clamp was a
hard-coded 999 for everything, and is now the item's own cap, so mana potions still buy 999 at a time
and buff scrolls buy 9. It also removes the partial-order question entirely: a single stack either fits
or it does not, so a purchase never half-completes and takes your gold with it.

**Existing characters migrate on login.** Any row saved over its new cap is split into legal ones the
moment it loads — bag, warehouse and account bank. It only ever splits, so nothing is lost, and a stack
that has nowhere to spill stays as one oversized row rather than being deleted. No `game.db` reset.

Items tooltips now read "Stacks to 999 per slot" (quest items stay silent).

## 2026-08-27 — 0.92.1: the mana ladder mirrors the healing ladder

> *"so healing are 20/70/150 and we match that just 15/30 cycle … and price is double of the healing"*

The three mana potions shipped hours earlier at 20/50/100 for 500/1500/4500. He retuned both columns
to one rule: **the rate IS the healing ladder's, and the price is exactly twice the healing ladder's.**

| item                 | restores     | window | drink reuse | **sustained** | buy                       | was         |
| -------------------- | ------------ | ------ | ----------- | ------------- | ------------------------- | ----------- |
| Common Mana Potion   | 20 MP/s      | 15s    | 30s         | **10 MP/s**   | **120** (heal 60 ×2)      | 20 @ 500    |
| Uncommon Mana Potion | **70 MP/s**  | 15s    | 30s         | **35 MP/s**   | **500** (heal 250 ×2)     | 50 @ 1,500  |
| Rare Mana Potion     | **150 MP/s** | 15s    | 30s         | **75 MP/s**   | **3,000** (heal 1,500 ×2) | 100 @ 4,500 |

**Only two things still differ from the healing ladder**: the cycle (15s up on a 30s reuse = a 50%
duty cycle, where the Rare healing potion's 30s-on-20s is permanent uptime) and the price. His own
arithmetic on the middle rung, which the build matches: *"uncommon its 1050/15s mp"* and *"60k/hour
for uncommon is ok"* (500 × 2 drinks/min × 60).

**Why mana costs double when the rate is identical** — it is not potency, it is the missing faucet:
*"common/uncommon healing potions are dropped so u dont spend there … u need to buy mp pots"*. Mana
potions do not drop anywhere. **Sources are unchanged**: Common + Uncommon on the Apothecary shelf,
Rare = the Potion Master's craft L5, nothing on any drop table. The Rare is deliberately a raid item:
*"its raiding support item that is economy player trade only"*.

**Boss fights stay allowed** and that is a ruling, not an oversight: the PvE gate is the PvP flag, and
a boss is PvE. *"in a party as alt char helping main char to heal ocasionally its still consider farm
so its ok"*. The only lockout remains flagged/PK — *"in pvp they need to conserve/strategize"*.

Coverage against `--mpnpc`'s measured deficits at 74 with a full NPC buff pack (healer −25.6 MP/s
farming, nuker −14.8, buffer toggles −32.0): **Uncommon's 35 sustained covers the healer AND the
buffer**, Common's 10 covers the nuker and the whole low-level game. `--mpnpc`'s cost table no longer
hard-codes the ladder — it reads the rate off the potion's skill and the price off the item, so it
cannot go stale behind a retune again.

Touched: `Skills.Common.cs` (the two `RestoreMp` magnitudes + descriptions), `Items.cs` (three
`Value`s + descriptions + the price rationale), `tools/BalanceMatrix` (the coverage table).

## 2026-08-27 — 0.92.0: MP potions, PvE only

Three tiers, his numbers verbatim: *"20/50/100 15s-up/30s-cd"*, and his sources: *"Common in shop /
uncommon shop / rare drop"*.

| item                 | restores | window | drink reuse | **sustained** | source                     |
| -------------------- | -------- | ------ | ----------- | ------------- | -------------------------- |
| Common Mana Potion   | 20 MP/s  | 15s    | 30s         | **10 MP/s**   | Apothecary, **500 gold**   |
| Uncommon Mana Potion | 50 MP/s  | 15s    | 30s         | **25 MP/s**   | Apothecary, **1,500 gold** |
| Rare Mana Potion     | 100 MP/s | 15s    | 30s         | **50 MP/s**   | Potion Master, craft L5    |

⚠ **The window is deliberately SHORTER than the reuse, which the healing ladder is not.** The Rare
Healing Potion runs 30s on a 20s reuse — permanent uptime, so its "150 HP/s" really is 150 HP/s
forever. 15s on a 30s reuse is a 50% duty cycle, so the sustained column above is the real number and
it is what `--mpdrain`'s measured deficits (0-15 MP/s for a caster, 16-64 for a buffer) were sized
against. Do not lengthen the window without re-reading that table.


**Dearer than the healing ladder at every rung** — his ruling: *"buy prices 500 common , 1500 uncommon
in shop .. higher then hp pots"*. Against the healing shelf's 60 / 250 that is 8.3× and 6×, and it is
not arbitrary: an Uncommon mana potion returns 750 MP, which at level 43 is **half a caster's entire
bar**, where an Uncommon healing potion returns 1,050 HP against a much larger pool. The Rare's 4,500
is inferred rather than ruled — it keeps his ×3 stride, and being craft-only that number is never a
buy price, only a sell one (180 gold).
### PvE only — the gate is on the DRINK, never on the effect

Owner: *"having mp pot On and then entering pvp it works until stop but the next one is forbidden"*.
So a potion already running is never stripped and never shortened; it ticks out its full 15 seconds,
and only the next bottle is refused. `ItemDef.PveOnly` + `FlagOf(player) != PvpFlag.Innocent` in
`UsePotion` — the same purple flag the rest of the game already runs on (60s after a PvP action, or
while PK karma stands).

🔑 **An innocent VICTIM can still drink.** The flag follows what you did, not what was done to you —
that is the existing rule for who may be freely attacked, and the potions inherit it rather than
inventing a second definition of "in PvP". Say the word if being *hit* by a player should also close
the gate.

### No new engine, no new APK

The effect is a lasting `RestoreMp` buff, whose Flat magnitude on a *buff* already means "give this
much MP each second" — the meaning `TickHealOverTime` implements for Harmony of Restoration's mana
half. So no new `SkillEffect` bit (there are none left) and no new tick loop.

⚠ `RestoreMp` is **not** in the `AnyBuff` mask and must not be added to it: on a CAST that bit means
"give MP now" (Restore Mana, the Mana Totem), and widening the mask would push those through
`ApplyBuff` too. `UsePotion` admits the potions by DURATION instead — a `RestoreMp` that lasts is the
MP twin of a heal-over-time potion.

`IsManaPotion` splits them from `IsHealPotion` (both are "a consumable with its own drink timer"), so
the auto-hunt's HP line can no longer drink your mana potions to top up a bar they cannot touch. And
`BestManaPotion`, a `=> null` stub carrying a *"reserved for when they are"* note, is filled in — the
client's MP slider in the auto-hunt Potions tab has been sending `MpPotionPct` all along and starts
working with no client change.

**Mana potions do NOT drop at all**, unlike the healing ladder. Owner, correcting his first answer
mid-build: *"can u make mp pots not drop sory"*, then *"only shop common/uncommon - rare apothecary
crafter"*. So there are exactly two sources and no faucet: the **Apothecary shelf** for Common and
Uncommon, and the **Potion Master's L5 recipe** for the Rare — filed beside the Rare healing potion,
same rarity, same rung, same input rarity, so the mana line's top is the HP line's top rather than a
new stride. That also keeps the "always" drop group at the size playtest-17 deliberately cut it to.



### `--mpnpc` — the fully NPC-buffed mage at 74, all three roles

His ask: *"show me npc buffed mage (elf - the worst of the 3) at lvl 74 (healer/nuker/buffer-toggles)
-> what is the drain (reuse passives/cast speed reduction) vs mp regen and mp pool"*.

The whole `npc_` shelf is applied — Soul +35% Max MP, Serenity +20% MP regen, Alacrity +30% cast, and
the rest — plus every reuse-reduction passive the class owns. **ELF at 74, buffed, standing still:**

| role                  | pool  | cast  | reuse− | spell                 | cycle | drain/s | regen | net       | empties |
| --------------------- | ----- | ----- | ------ | --------------------- | ----- | ------- | ----- | --------- | ------- |
| healer, farming       | 4,605 | ×2.29 | 20%    | Holy Ray              | 1.80s | 38.3    | 12.8  | **−25.6** | 180s    |
| healer, healing       | 4,605 | ×2.29 | 20%    | Great Heal            | 3.70s | 32.4    | 12.8  | −19.7     | 234s    |
| nuker                 | 3,877 | ×2.29 | 20%    | Elemental Blast       | 2.50s | 27.6    | 12.8  | −14.8     | 263s    |
| buffer, toggles only  | 3,740 |       |        | Reinf r13 + Sharp r13 | —     | 45.0    | 13.0  | −32.0     | 117s    |
| buffer, + Sound Burst | 3,740 |       |        |                       | 5.60s | 66.4    | 13.0  | **−53.5** | **70s** |

🔑 **A BUFF PACK IS AN ACCELERATOR, NOT SUSTAIN.** It raises the pool 35% and the regen 20%, and
raises the drain by *more* than either — because Alacrity and the reuse passives both SHORTEN the
cycle, and the same spell therefore costs more per second the better buffed you are. The elf's cycle
is 1.80s where the ork's is 2.20s; ork nets −15.7 against elf's −25.6 on the identical spell.

🔑 **The ladder as authored already lands where it was aimed.** Over one 30s cycle a buffed elf healer
takes 750 MP from an Uncommon, 384 from regen, and spends 1,150 — **net −0.5 MP/s against a 4,605
bar**, i.e. he farms for over two hours on one potion line. Uncommon is the healer's tier, Rare (50
sustained against −53.5) is the buffer's, Common is the nuker's and the low levels'.

⚠ **What that costs is the other half.** A 30s reuse means two drinks a minute, forever: Common
**1,000 gold/min**, Uncommon **3,000/min**, Rare **9,000/min** (60k / 180k / 540k an hour). That, not
the PvP rule, is the argument for a lower price.
### Calm Spirit: his CSV edit, and the run penalty is gone

He fixed it in `nuker 3rd.csv` the same day it was reported, and the code follows the file. The run
column is **deleted outright**; what the passive buys is the walk column, ×1.06 → ×1.16, with a ×1.01
on standing from rung 4.

🔴 **Why it had to change.** The first build gave running ×0.30 climbing to ×0.70 — but that
MULTIPLIED the 0.70 run *stance*, so learning the passive took a running mage from 7.7 MP/s to **3.3**
at rung 1 and never caught up: an unremovable passive that made running strictly worse than not having
it, at every rung including the last. Running is now simply the 0.70 stance, untouched.

His aim is unchanged — *"both walk/still is the same mp regen in the end … keep farming while kiting
(slowly)"* — but it is bought rather than paid for: walk/stand goes 91.5% → **98.1%** across the six
rungs instead of a running mage being punished into it. ⚠ **Both columns are AUTHORED now**; the stand
column used to be DERIVED as `0.85 × walk` and no longer is.

### `--mpcase` pinned to his live client — and the model was wrong

He measured his own character against the report: *"x1.3 cast speed and holy ray l1 (30mp 2.5 cast
time /1s cd) -> 2.5/1.3 = 1.92cast + 1cd(/1.15 …) = 2.79s for 30mp = 10.75/s cost … thats all only
holy ray no heals no buffs nothing in between casts"*.

He is right and the table was wrong. A first attempt to reproduce his ×1.30 inflated WIT by 6, which
he corrected on the spot: *"Ork have 19 wit but u dont take into the acount alacruty/frenzy/passives"*.
The stat was never the gap — the **buff stack** was. With a real Alacrity (Rare) buff applied and WIT
left at the racial value, the model lands on his client:

|                          | cast  | cycle     | drain               | his               |
| ------------------------ | ----- | --------- | ------------------- | ----------------- |
| ork healer 43            | ×1.26 | **2.79s** | **10.77 MP/s**      | 2.79s, 10.75 MP/s |
| regen still / walk / run |       |           | **8.7 / 7.6 / 6.5** | 8.4 / 7.6 / 6.5   |

⚠ **A cast-speed error MULTIPLIES the drain**, because it divides the cycle — which is exactly how
0.91.2's *"below ~45 nobody has an MP problem"* came to be written. It is withdrawn: **standing still,
unbuffed, on the cheapest spell he owns, with nothing between casts, a level-43 healer does not pay
for himself.** And his prediction about the elf holds — casting fastest on the lowest SPT, the elf is
**−6.2 MP/s** where the ork is −2.1:

| race at 43 | WIT | SPT | cast  | drain | regen (still) | net      |
| ---------- | --- | --- | ----- | ----- | ------------- | -------- |
| Human      | 21  | 39  | ×1.47 | 11.98 | 7.9           | **−4.1** |
| Elf        | 24  | 32  | ×1.70 | 13.21 | 7.0           | **−6.2** |
| Ork        | 20  | 45  | ×1.26 | 10.77 | 8.7           | **−2.1** |

The Common mana potion's 10 MP/s sustained covers all three several times over, which is the right
shape: the tier that trivialises the problem at 43 should still be the cheap one at 80.
### `BalanceMatrix --mpcase` — the level-43 ork healer, and what it rules out

He read 0.91.2's *"below ~45 nobody has an MP problem"* against what he actually plays: *"Ork healer
fight for 1min and mp is depleted ... 43lvl ... E robe + wand/shield"*. So `--mpcase` measures that
character — E-grade (t20) gear, wand + shield instead of a staff, UNBUFFED — instead of the
best-geared abstraction, and prints time-to-empty against his own minute.

It ruled out all three suspects. **The heal is not guilty**: Great Heal is 62 MP over a 6.8s cycle
against Holy Ray's 30 over 3.3s — 9.1 MP/s either way, so the rotation mix barely moves the number.
**Jewellery is not guilty**: five accessory slots add ZERO Max MP, the pool is pure level + SPT.
**Regen is not blocked while casting** — there is no cast guard before `Regenerate` in the tick loop.

Which leaves a gap the model cannot close: 60s of Holy Ray costs 545 MP out of a 1462 bar, and regen
very nearly pays for it. His Max MP on screen and how many Holy Rays a full bar really buys are the
two numbers that would settle it.

⚠ 0.91.2's `--mpcase` had two bugs of its own, both fixed here: `"UNBUFFED".Contains("BUFFED")` is
true, so the unbuffed build was measured with the ×1.44 stack; and its footer claimed a heal doubles
the drain, which the corrected run disproves.
## 2026-08-27 — 0.91.2: the MP economy measured per race, and a toggle finally charges its rung

No new content — this is the measurement the MP-potion decision rests on, plus the one bug it found.

### `BalanceMatrix --mpdrain` — drain vs regen, flat MP/s, all three races, 20→80

Owner, opening the MP-potion question: *"Just need to measure what is the mp consumption at
20,30,40,50,60,70,80 (healer-holy bolt, Mage-elemental bolt, buffer with reinforcement and sharpening
active) vs current mp regen ... but take into an account spt/wit cast speed elf casts faster"*.

`--mpregen` (`BL-92`) already answered a **different** question — "is a mage's regen too big" — for one
HUMAN nuker on his most MP-EFFICIENT spell. A potion has to be sized off the named spells, every race,
and the whole ladder, so `--mpdrain` measures that instead. Both sides mirror the engine line for line:
drain is `EffectiveMpCost / AutoCycleTicks`, regen is `Regenerate`'s MP branch with the flats outside.

Race is the point, and it cuts **twice in the same direction**: WIT drives cast speed (exponential,
×1.63 per +10) so a faster caster empties the bar sooner, and SPT drives `SptRegenModifier` so he also
refills it slower. Elf casts ×1.48 on 0.84 regen; ork casts ×1.10 on 1.10. At level 80 an elf healer is
**14.7 MP/s under water where the ork is 5.6** — the same spell, the same rung.

What it found, in flat numbers: **a caster's deficit is 0-15 MP/s at every level**, and below ~40
there is no deficit at all (regen outruns the bolt). **The buffer is the real customer** — two stances
plus his sound skill run 33 MP/s at 40 and **81 at 80**, against 17.6 of regen: a full bar every ~55
seconds, at every level from 40 up.

### 🔴 A TOGGLE WAS CHARGED RUNG 1's UPKEEP AT EVERY RUNG

Found by the above. `TickToggleUpkeep` read `SkillDef.MpPerSecond` — one field, one number per def —
so the Warchanter's Reinforcement, authored as a 13-rung ladder from **12 MP/s up to 30**, really took
**12** at rung 13. Both stances together: **15 MP/s charged against 45 authored, a 3× discount** on the
one class whose mana is supposed to be a decision.

Fixed the way every other per-rung number in this codebase is fixed — `SkillLevel.MpPerSecond` (0 =
inherit the def's) and `SkillDef.MpPerSecondAt(level)`, read by the tick loop and by the skill card.

⚠ **Not `MpCostAt`.** Prowl carries a 20 MP one-off cast cost *and* a 1 MP/s burn, so a toggle's two
prices are genuinely two fields on one skill; folding them would have made every Prowl tick cost 20.

No authored number moved — `SkillCsvSeed --check` is clean — and no wire change, so **no new APK**.
## 2026-08-27 — 0.91.1: HP Boost, Swift back on the NPC buffer, the buffer's ×1.2 moves

**⚠ NEEDS A NEW APK.** The client builds its Learn tab locally from the compiled `ClassSkills`, so a
class-skill-TABLE change is invisible to an old build.

### HP Boost — one skill, ten rungs, two classes, different ladders

Rebuilt from `warrior 2nd/3rd.csv` and `buffer 3rd.csv`. Flat Max HP, **+120 climbing to +1000**.
The warrior takes L1-L3 at **20 / 28 / 36** and L4-L10 at **43 / 49 / 55 / 62 / 66 / 70 / 74**; the
buffer starts at rung 1 twenty levels later and stops at rung 7 — **40 / 44 / 48 / 52 / 56 / 62 / 70**,
ending +700 where the warrior ends +1000.

🔴 **The authored numbers are ALREADY DOUBLED — never scale them again.** Owner: *"i doubled the hp
passive read as is"*. Our flats stack **outside** the buff multiplier (the global "flats after
percentages" rule) where IG applies them inside, so he pre-doubled while writing the CSV. Unbuffed
reads a little above IG, buffed lands on it — the trade he chose over moving the stacking order.

⚠ The buffer's SP prices are his own 3rd-class ladder, so every buffer rung carries an explicit
`ClassSkill.SpCost`. Rung 1 is 3,400 on the warrior's ladder against the buffer's 36,000; without the
override he would buy it at a tenth of price.

⚠ `hp_boost` existed once and was deleted 2026-08-07 with the God layer. This shares nothing with it
but the id. And this is the **only** 3rd-class row a warrior has — Ravager/Warlord stays unauthored
until he writes it, on the same terms as the tank's lone Shield Mastery rung.

### The buffer's ×1.2 MP regen moves to the race masteries

*"robe/light/heavy: pdef +X, max mp +Y; the mp regen is moved to the represented masteries per race
(human/ork heavy, elf light)"*. So **Armor Mastery** now grants flat P.Def and Max MP in all three
weights and **nothing else**, and the `MpRegenPct 0.2` rides on **Chanter Heavy Mastery** (Human;Ork)
and **Harmonist Light Mastery** (Elf) instead.

Same outcome for a buffer wearing his race's armour — and it turns the grant into a **reward for
wearing it**: a Human Warchanter in light armour now gets no ×1.2 at all, where Armor Mastery used to
hand it to him regardless. The `BL-92` rule still holds exactly: **one ×1.2 per mage**, robe from the
born Spellcaster Mastery, light/heavy from the race mastery.

### Swift is back on the newbie buffer

*"add swift in the NPC buffer - i missed it apparently"* — reversing the playtest-28 cut, which had
dropped it on the reasoning that Dash and a mount cover move speed. **Twelve** buffs against the cap of
twenty, so a real buffer's groups still fit beside the full NPC set.

`SkillCsvSeed --check`: no discrepancies across all ten checked files.

## 2026-08-27 — 0.91.0: player HP rebuilt on IG's curve — `BL-78` item 3

**The pool rose to meet the attack.** `0.73.0` refitted creature attack up ~×1.65 against IG's current
chronicle and never re-ran the player side; `BL-78` item 3 has carried that debt since. His playtest
words then were *"a healer with 1500 hp getting hit for 300 is abit harsh"*, and on 2026-08-27 he put it
plainly again: *"the hp of players seems twice if not trice as low from IG"*. It was — and the fix is on
the player side, so this changes **no mob number at all**.

### Max HP is now a growth rate that STEPS AT CLASS CHANGE

```
base(L) = level1Base(race, baseClass) + SUM over tiers of  g(tier) * ( Q(hi) - Q(lo) )
MaxHp   = base(L) * conHpModifier(effectiveCon)          Q(L) = (L*L + 3L)/2
tiers   = 1-19 | 20-39 | 40-75 | 76-85     (our class-change levels)
```

Each level grants `g × (L+1)` HP and **`g` steps up at every class change** — that step *is* the
class-growth bonus IG's per-class tables carry, and it answers his question directly: a knight's rate
jumps **+53%** at 2nd class, a mystic's only **+13%**. The old single quadratic (`classMod·(L²+3L)/2`)
could not express it, which is why it was 2-3× short at the top and 2× fat at level 1.

Fitted to IG's own tables: **0% error at 1 / 40 / 80, −3% at 10, +7% at 20** (worst), +5% at 50-60. The
three anchors he set read **2414 / 1184 / 9969** against his 2380 / 1180 / 9840.

### The HP track is keyed by DISCIPLINE, and it is a pure function

Keyed by discipline where the character has one, by archetype before 40. That is the **only** way
Warchanter (buffer) and Lightbringer (healer) can differ, since they share `Archetype.Healer` — his
ruling: *"buffers are mele and need boost ... in between the nukers and rogues"*.

Nothing is accumulated, so taking a discipline at 40 recomputes the whole curve on the new track and a
Warchanter **visibly gains +20% HP the moment he class-changes**. That is deliberate: it reproduces IG's
per-class table jump without a discontinuity in L.

The six tracks, and the ordering he specified (nuker = healer < buffer < rogue < warrior < tank), base
HP at 80: **3233 / 3233 / 3926 / 4097 / 4454 / 4902**.

### The CON curve is IG's, and it is normalised at 20

`CON 20 → ×1.00, 30 → ×1.25, 40 → ×1.80, 50 → ×2.58` (continued to ×3.72 at 60 so stat swaps stay
smooth). The table it replaced read ×1.83 at CON 50 and was normalised at 30. ⚠ **Both halves move
together** — the base table above is quoted *against this curve*, so changing one alone rescales every
pool in the game.

### What it does

|         | @20   | @40   | @60   | @80   |
| ------- | ----- | ----- | ----- | ----- |
| tank    | ×1.03 | ×1.63 | ×1.83 | ×1.92 |
| warrior | ×1.08 | ×1.77 | ×2.02 | ×2.12 |
| rogue   | ×1.15 | ×1.97 | ×2.29 | ×2.42 |
| buffer  | ×1.51 | ×2.54 | ×3.28 | ×3.61 |
| healer  | ×1.51 | ×2.25 | ×2.78 | ×3.00 |
| nuker   | ×1.38 | ×1.97 | ×2.39 | ×2.56 |

Measured survival against a same-level creature (`BalanceMatrix`, E2), standing still: a **robe at 52
goes 9s → 21s**, a tank 73s → 132s, a rogue 27s → 58s, a champion 36s → 69s. The early game barely
moves; the correction lands where he felt it. Levels 1-10 do get *smaller* (`level1Base` 126 → 44, so a
level-1 tank reads 89 HP not 186) — that is IG's own level-1 row, and creature P.Atk at level 1 is 7.

### Deliberately NOT changed, on his rulings the same day

- **Interrupt** (`damageTaken / casterMaxHp`) keeps its formula. A bigger pool lowers interrupt chance,
  and that is the point: *"a mage with 500hp getting hit by 100 .. is 20% base interrupt chance"*.
- **Heals, HoTs and HP potions** keep their numbers. They are throughput, not survivability: *"now u
  just need more pots to heal to max, they do not touch the survavability factor"*.
- **HP regen** keeps its formula; the fighter sitting-regen boost arrives as an authored CSV passive.
- **Flats stay OUTSIDE** the buff multiplier, per the global rule (playtest 28). IG puts them inside;
  his ruling is that we keep our order and he **doubles the authored `+HP`** when writing the rung.
- **No mob number moved.** `docs/balance/MobCurveVsIG.md` still stands as measured.

`BalanceMatrix --hpcurve` prints the tracks against IG's tables, the three anchors and the
class-change step, so this stays checkable.

## 2026-08-27 — 0.90.0: `BL-93` step 1 — the client can draw a body

**⚠ Half a feature on purpose, and it ships in that state.** The engine side of the model pass is
complete and the world still renders exactly as it did — flat coloured spheres — because there is no
art in the repo yet. The other half is one Unity Editor session (below), and until it happens this
change is invisible, which is the point.

**Protocol 28 → 29.** `EntityDto` gained two fields, `Category` and `Role`.

### The wire tells the client WHAT a creature is, never what it looks like

The client could not draw a wolf as a wolf because it did not know it was a wolf: for a mob,
`EntityDto` carried a name and a level and nothing else about its identity. It now carries the
**authored** taxonomy — `MobCatalog`'s own `MobCategory` × `MobRole`, which every template already
declares because it maps the CSV "Type" column.

Deliberately *not* a model id on the wire. The server says what a thing **is**; the client decides how
it **looks**. A mesh name in a DTO would put an art decision inside the simulation, where changing it
costs a protocol bump — and a taxonomy invented for the client is a second table to keep in step with
the first, which is a thing that drifts. Players and NPCs send nothing new at all: race, class and
subclass were already on the wire and say strictly more than a model needs.

### The art budget is per FAMILY, not per mob

Nine categories × three roles is the whole model set for 100+ templates, with tint and scale
separating members inside one family. A new creature inherits a model for free.

`ModelLibrary` (in `EntityManager.cs`) resolves an entity down a **fallback chain**, most specific
first — `mob_animal_archer` → `mob_animal` → `mob` → `humanoid` — and stops at the first hit. So a
**single** `Resources/Models/humanoid.prefab` gives every player, NPC and humanoid mob in the game a
body, and each more specific prefab added later peels one group off the general case with no code
change. Art can arrive in any order over any number of months and the client is correct throughout.
Missing lookups are cached as misses: until art lands *every* lookup fails, and an unremembered miss
is a filesystem probe per spawn on a phone.

### Animation cost no new messages

Everything the server already sends turned out to be enough. `EntityLean.Speed` and the drawn
position delta give walk speed **and facing** (no wire field for either — the interpolator has both);
`CombatEvent` gives the swing, for every visible fight rather than only your own, so the hook sits
*above* `OnCombat`'s self-only filter; `MobCastInfo` and `CastInfo` give the casting pose; `Dead`
gives the fall. Every animator parameter is optional, so a controller with only `Speed` gets walking
and no console noise — which is what matters while clips arrive one at a time.

One thing that had to be kept apart: **a sphere billboards toward the camera, a body faces where it
walks.** Doing both would spin every character to stare at you whenever the camera moved, so
`EntityView.LateUpdate` picks one path or the other and they never mix.

### "3D models: off" is a quality preset, not a debug switch

In the Settings window with the other look options, persisted, and the OFF position is the exact
client that shipped before this change — one draw call per entity, no skinning. A phone that cannot
carry rigged meshes still gets a game rather than a slideshow. Animators are also culled off-screen
by default (`CullUpdateTransforms`), because the server sends everything inside `ViewRange = 3000`,
which is a good deal more than the camera is looking at.

### ⚠ What is still owed — the Editor half

Nothing here can be seen until someone opens Unity once: import a rigged model, set its **Rig to
Humanoid** (never Generic — see `docs/guides/UnityClient.md`, "Dropping in a model"), and save it as
`Assets/Resources/Models/humanoid.prefab`. **A version bump and a new APK go with it**, since the
protocol moved.

## 2026-08-26 — 0.89.0: a boss becomes a boss — `BL-13` + `BL-81` + `BL-83` + `BL-88`

Four entries, one build, and the first APK since **0.81.1** — seven versions of server work had never
reached the phone.

### `BL-13` — 10 to 30 minutes, and a boss that is felt

His playtest-25 ruling was three sentences and every one of them was a separate defect.

**1. *"A 3 min boss is not a boss its a stronger elite mob."*** A boss's HP was a **flat ×100**
multiple of the level curve. That curve is quadratic (`40 + 0.8·L²`) while a party's DPS is roughly
**flat** across the game, because gear tracks level — so time-to-kill grew with the SQUARE of the
level, and it measured exactly that way: **96s at level 20** against **1514s at 76**. The bottom of the
game was 6× too fast, and no single number could fix that without pushing the top out of his own
600–1800s band.

So the multiplier is now a **curve**, `43000 / L^1.49`, living in the new
`Game.Shared/MobRankScale.cs`. Measured against his own party — tank + healer + 3 DDs, best-for-tier
gear, runes up:

| Lvl |                                       | HP × | boss HP | party dps | TTK       |        |
| --- | ------------------------------------- | ---- | ------- | --------- | --------- | ------ |
| 20  | *(no boss spawns here — shape check)* | ×495 | 178,334 | 253       | **704s**  | 12 min |
| 44  | Grave Lich, Hollow Crypt              | ×153 | 242,982 | 225       | **1079s** | 18 min |
| 60  | the world boss                        | ×96  | 281,453 | 205       | **1376s** | 23 min |
| 65  | Dread Knight, Sunless Warrens         | ×86  | 292,586 | 209       | **1398s** | 23 min |
| 76  |                                       | ×68  | 315,821 | 208       | **1515s** | 25 min |
| 85  |                                       | ×57  | 333,854 | 292       | **1144s** | 19 min |
| 90  | Disciple of the Dawn                  | ×53  | 343,474 | 270       | **1274s** | 21 min |

Every level inside the band, and rising — his *"the target rises"*. (85 dips because the party's own
damage jumps there: S grade lands at 80. That is player gear, not the boss curve.)

⚠ **The tables run at the levels a boss ACTUALLY SPAWNS AT now** — 44, 60, 65, 90. They used to say
20/40/60/76/85, and the two lowest rows described bosses that do not exist, which is how a measurement
quietly stops describing the game.

**2. *"Bosses should have stronger defences."*** They had **none**. A rank scaled HP and attack and not
one point of defence, so a "boss" wore exactly the paper armour of the trash around it with a hundred
times the health bar — which is the mechanical reason a boss read as a **sponge** rather than as
something armoured. A rank now carries a defence multiplier: elite ×1.33, boss ×2.0, deliberately the
same ladder the control contest already uses, because a creature twice as hard to hold should be twice
as hard to cut.

⚠ It also paid for itself: boss EXP derives from `HP × defence`, so **doubling a boss's defence doubled
its exp with nobody editing an exp number**. That term was written a month ago against exactly this day.

**3. *"Not one shooting but a tank can feel it"* + *"a healer, tank and dds in a party are a must"*.**
🔴 **Boss P.Atk came DOWN, ×10 → ×4, and that is a number of yours I am moving.** Two reasons, and the
second is the one that settles it:

- The ground moved under it. The ×10 is playtest-20's *"P.Atk from x5 -> x20"*, taken as your ratio off
  the base of the day — and **0.73.0 refitted the creature attack curve ~×1.65 upward**, so ×10 today
  is ~×16.5 in the units you ruled in. Your own ratio, in today's units, is about ×6.
- **Your party clause makes ×10 unpayable.** Measured at 76, a boss put **752 dps through a shielded
  Knight while a Lightbringer's best heal sustains 391**. The party you prescribe loses its tank in
  **thirteen seconds** — a 10-to-30-minute fight was not merely hard, it was arithmetically impossible.
  And a boss's ordinary swing killed a **robe twice over** at every level from 40 up, which is *"one
  shooting"* in the plainest sense of your words.

At ×4 the boss sits between both walls at every level: an unhealed tank dies in **19–33s**, the healer
covers the incoming at **48–83%** of his ceiling, and a robe survives one basic hit (39–80% of its
pool).

**BalanceMatrix measures all of this now** — a real five-man party, the tank's **shield** inside the
incoming number, and the healer wearing his actual **Lightbringer** kit instead of a 2nd-class heal
ladder that stopped at level 35. It also stopped keeping a hand-typed copy of the four rank
multipliers: it reads `MobRankScale`, the same code `BuildMob` spawns with, so the tool and the game
can no longer disagree without either being edited.

⚠ **What it does to boss EXP, checked rather than assumed.** Boss exp derives from kill time, so a
boss that takes ~3× longer pays ~3× more: the level-44 Grave Lich is now **49% of a level per head in
a nine-man**, for a 20-minute fight. The `MobKillTimeRatio` sanity rail (clamped at 400) does **not**
bind on it — it would only bite below level ~37, and nothing spawns there. If a boss reads as too rich
in play, that clamp is the knob; the curve under it is `BL-49`, which you ruled *"leave it"*.

### `BL-81` — god mode is absolute, a boss is not

Two rules, one gate (`ResistsDebuff`), so no resolution path can pick up two of them and miss the third.

- **God mode now resists every debuff.** It only blocked *damage* before, so an admin in god mode could
  still be stunned, slowed, cursed and dispelled. 🔑 Built as a **resist, not a refusal**, which is your
  clause read literally (*"can be used on him but resisted"*): the cast resolves, the MP is paid, the
  cooldown starts, and the combat line says Resisted — so the skill you turned god mode on to debug is
  still testable. Cancel/dispel and knockback are refused the same way.
- **A boss is immune to CONTROL only.** That half shipped on 2026-08-19 and is folded into the same
  gate. Attrition still lands, exactly as you wrote it: stat-downs on the four attack/defence stats,
  DoTs, regen suppression. Knockback joined the control side — a boss shoved around the field is the
  same perma-lock the immunity exists to prevent.

### `BL-83` — a taunt can never be automated again

*"I think remove the taunt as being able to be auto. I feel it like an exploit. Get a tank leave it
auto he taunts almost impossible to kill you farm with ur other hero. Taunt should be active play
only."* This **reverses** the rung 0.68.0 gave taunts three days earlier — but as a **removal**, not a
revert. 0.68.0's diagnosis was right: a pure taunt carries neither `ContestCc` nor a `DebuffSchool`, so
every branch missed it and it fell into the never-cast bucket **by accident**. It goes back to that
bucket **deliberately**, and the good half of 0.68.0 survives — an armed row the chain cannot cast is
still reported the moment you save, so an armed Provoke now says *it is for you to press yourself*
instead of looking like the same silent bug you reported.

⚠ Asked **before** the damage test, unlike the old rung, so a taunt that also hits cannot smuggle its
threat into the attack rotation. It covers `Provoke`, `Lure` and any 40+ taunt your kits add — you
named no exception, and a tank's kit is where an exception would live.

### `BL-88` — the three UI changes from playtest 25, three passes late

- **The target frame's title bar is the NAME and nothing else** — *"no lvl no target.title, now the
  [title + name + lvl] overflows"*. The level moved down in playtest 23; the worn **title** now joins it
  in the detail row, keeping its colour: `Mob: 44, Field Boss, Aggressive`. A phone frame is a fixed
  width, and one variable-length thing is the only count that cannot be made to overflow it.
- **The chat tab row fits at the window's minimum width.** It was six 96px buttons on a 100px step —
  608px of row inside a window you are allowed to shrink to 520, so the `[Combat]` button hung outside
  the frame with nothing behind it. 76 on an 80 step is 488.
- **The gear picker, second pass** — chips 28 → **24** high (*"even smaller. Like the tab buttons in
  height"*) and the **splitter** under the `[S 80]` row that was missing, so the filter chips stop
  reading as the first rows of the list.

### Also
- ⚠ **NEW APK — and `ProtocolVersion` is UNCHANGED at 28**, so an old client still connects and looks
  fine while missing every client-side change from 0.82.0 onward. Install both halves.
- No schema change in this build; the `game.db` delete owed from the earlier ones still stands.
- `BL-47` and `BL-49` closed on your rulings of 2026-08-26 — field creatures stay on the
  `MobBaseStats` curve, player-built ones are a hand-placed content tool, and the levelling curve is
  left alone. `BL-93` opened for the visual pass you want to talk about.

## 2026-08-26 — 0.88.2: the HP half of `BL-92`, and the mage stops out-regenerating the tank

The half he held two versions ago. He supplied IG's own HP-regen reference — base **1.5–3.0 by race
and class**, a CON modifier anchored at **CON 30 → 1.00 / CON 43 → 1.32**, and
`LvlMod = Level/100 + 0.89` — and it turned out our formula factors **exactly** into that shape:

```
(3 + 0.1·L) × 1.03^(CON−40)   ≡   3.00 × (1 + L/30) × 1.03^(CON−40)
                                  base    LvlMod       ConMod
```

So the comparison was like-for-like, and it found two different problems.

**What was measured** (`BalanceMatrix --hpregen`, new). At level 1 we sit inside IG's band for all
three fighters and 6–13% above it for mages. Across the game we drift to **~2× IG for fighters and
~2.7× for mages**, and, worse, the class order was **upside down**: buffed at 74 a nuker regenerated
**27.5 HP/s** against a tank's **16.4** — the class IG deliberately gives the *lowest* base regen held
the game's highest, because `hpReg x2.7` multiplied a level term the tank's `x1.00` did not.

**His ruling**: *"I want to make the passives + not x as the mp .. and buffs to carry the multiplier ..
and the flat is to added at the end"*.

```
HpRegen/s = [ (3 + 0.1·L) × 1.03^(CON−40) × stance × safeZone × (1+buff%) ] + flats
```

- **Every `hpReg` mastery rung is now a FLAT HP/s**, read off the CSV row whole exactly as `mpReg` is:
  `hpReg +2.7` is +2.7 HP/s, not ×2.7. Body Mastery, Spell Mastery, Spellcaster Weapon Mastery (3rd
  and 4th tier) and the rogue's Armor Mastery all converted; the CSVs moved in the same commit.
- **Buffs keep the multiplier**, and **flats land last** — the same global rule MP took in 0.88.0.
- `Entity.HpRegenMult` now carries only what is genuinely a percent (the armour-**set** bonus and the
  `HpRegenPercent` gear attribute). `StatMods.HpRegen` is read at last, so a flat authored on a set or
  an armour mastery actually pays.

**The order is right again.** Buffed and standing at 74: warrior **18.0** > rogue **17.6** > tank
**16.4** > nuker **12.9**, which is IG's own intent — a mage's base regen is half a fighter's.

**⚠ We knowingly sit at ~1.6–2.0× IG, and that is deferred, not settled.** *"Leave out lvl mod just
leave the flat outside … So we will have x2 more than IG but not as much as we have now … Playtest
will decide if it stays"*. The entire residual is our **level term**: `1 + L/30` climbs ×3.71 across
1–85 where IG's `L/100 + 0.89` climbs ×1.93 — and IG's is character-for-character the `(level+89)/100`
our **damage** formula already runs on. The swap was measured and offered; `--hpregen` prints it as
its own column so a playtest has the number ready. **Don't take it without a new ruling.**

**⚠ Two things he flagged, neither built.** The fighter 3rd/4th kits are unauthored, and when they land
*"fighters … have higher regen flat bonuses than mage"* must become true — today the nuker carries
+2.7, the warrior +1.6 (frozen at level 32), the rogue +1.2 and the **tank none at all** (archer and
dual have no `hpReg` row either). And *"buffer ork should have more but yet not desided"* — no number
invented.

**⚠ The ladder stopped being progression**, knowingly. A nuker's six rungs from +1.1 to +2.7 used to
buy +19 HP/s and now buy **+1.6 across 34 levels** — the same trade the `mpReg` ladder took. If those
rungs should be felt, the flat numbers get re-authored bigger in the CSVs; it is not an engine change.

**And every primary stat is now read EFFECTIVE.** *"Need effective con to count on hp max/regen and
whatever con have mod on … con armor set now will buy u nothing and atk-con won't hinder you"*. CON
and ATK were the last two read **base**: HP regen used `entity.Con`, and the character sheet and the
target panel sent `p.Con` / `p.AtkStat` while sending `EffectiveWit/Agi/Spt` beside them. So an armour
set's `Con: -2, Str: +3` moved your HP pool, your regen and your damage while **the stat window showed
no change at all**. Max HP, HP regen (tick loop *and* the stats-window preview), the contested-debuff
save and both panels now read `Effective*` uniformly. Mob paths keep raw CON — a mob has no bonus.

**Found on the way:** fifteen rows of `healer 4th.csv` had lost their `mpReg` label (`, x3.4,hpReg …`),
so `--check` could not read the value and had been skipping it silently. Label restored; the value
already matched the code. And `StandingRegen` — the helper that tells the stats window what your regen
is — still had the **old flats-inside** HP shape after the change above, so the number on screen would
have disagreed with the number the tick pays. Both halves now mirror `Regenerate` exactly.

Also new: **`BalanceMatrix --hpregen`**, which prints IG's numbers beside ours in HP/s, the flat ladder
per class, regen against pool / mob DPS / potion throughput, and the level-term swap priced.
`docs/Formulas.md` moved with it, in this commit.

⚠ **Protocol stays 28** (no wire change). **A new APK is still required**: skill tooltips are built
*locally* by the client from the compiled `SkillCatalog`, and every mastery rung's regen line changed
from `HP regen x2.7` to `HP regen +2.7/s`. No schema change, so **no `game.db` delete**.

## 2026-08-26 — 0.88.1: one ×1.2 per mage, not two

The last thing 0.88.0 measured rather than changed. **The 20% MP-regen bonus was authored twice** —
once on the born Spellcaster Mastery's Robe profile and again on every class Armor Mastery's Robe
profile — so a robed mage from level 40 quietly ran **×1.44**, and every regen number in the game had
been sized against one of them. His words: *"I forgot the second x1.2 … My calculations were only
with the 1st spellcasters u are born with … The second is not needed"*.

He considered and rejected cutting both to ×1.1 (*"the starting mp suffers while the higher the 10%
is not of a difference"*) — a compounding pair is worst exactly where the pool is smallest.

**The rule is now one ×1.2 per mage, from the armour actually worn.** The `Robe` slot of every class
Armor Mastery lost its `mpReg` — cleric, nuker, Lightbringer 3rd and 4th, and the Warchanter's
one-line all-weights profile, which had to split. **`Light` (cleric) and `Heavy` (buffer) keep theirs**,
because Spellcaster Mastery pays those weights nothing: it penalises them. Five code sites and every
affected CSV row moved together; the only four files that still author `mpReg x1.2` are `mage 1st`
(the born one), `cleric 2nd` (Light) and `buffer 3rd`/`4th` (Light/Heavy). `--check` green.

**Measured effect** — a buffed human nuker's regen as a % of his own main-nuke spam drain, standing:

| L   | 0.88.0 | **0.88.1** |
| --- | ------ | ---------- |
| 40  | 146%   | 125%       |
| 60  | 117%   | 101%       |
| 68  | 101%   | **87%**    |
| 74  | 102%   | **88%**    |
| 85  | 111%   | 95%        |

Which is the shape he asked for at the start: a high-level mage who spams **cannot** pay for himself
and needs a restorer or a rotation, while a levelling one is comfortable. Calm Spirit is unaffected —
still exactly **100.0%** walk/stand at its top rung.

## 2026-08-26 — 0.88.0: mana stops being free, and standing still becomes a stance

`BL-92`, opened by the owner (*"our MP regen is 10 times faster than IG's"*) and ruled by him in full
the same day. **Measured first**: a new `BalanceMatrix --mpregen` report priced a real nuker's
spell-spam drain against his own regen and found a buffed level-74 mage regenerating **288% of what
spamming his main nuke costs** — 196% at 40, climbing to 320% by 85. The base `2 + 0.08·L` was never
the problem. **The mastery ladder was**: `mpReg x1.5 … x3.4` compounding to ×4.84 by 74.

**Four rulings, all his:**

- **The mastery `mpReg` column is FLAT, not a multiplier** — *"except armor masteries the 20%
  increase .. the other increases are flat increases so the 1.9~3.4 is + not x"*. Spell Mastery and
  Spellcaster Weapon Mastery now grant **+1.1 … +3.4 MP/s**; the armour masteries' `x1.2` stays a
  percent. 63 CSV cells re-notated from `xN` to `+N` in the same commit.
- **The flats sit OUTSIDE** — *"flat is outside as everything flat in our formulas"*, the same global
  rule playtest 28 set for `ModifiedStat`. IG puts them inside; he compared both at level 74 (23.5 vs
  ~20) and took ours.
- **SPT gets its own regen curve**: `clamp(1 + (SPT − 40)×0.02, 0.70, 1.30)`, off the Max-MP curve it
  used to borrow. Wider on purpose — every fighter sits at the 0.70 floor, the ork mage reaches 1.10.
  ⚠ He wrote the step as ×0.05; his own three anchors make it **0.02**, and he took the correction.
- **A STANDING STANCE EXISTS.** The ladder is now **running 0.70 · walking 0.85 · standing still 1.00
  · sitting 1.50** — IG's own shape, which could not be copied before because a player who stopped
  moving was still `Running`. Movement is a cost instead of standing being a bonus. Standing is
  DERIVED (no move target); `MoveState` keeps its three persisted values.

**CALM SPIRIT is built** (nuker, six rungs at 40-74), held since 0.87.0 on *"w8 on calm spirit"*
because the stance model it needed did not exist. It multiplies the *stance*: ×0.30→×0.70 running,
×1.03→×1.20 walking, ×1.00→×1.02 standing. 🔑 **It is a nerf to running and that is the point** — his
design is *"in pvp need to click run (but regen slower)"*. The standing column is derived, not
authored: he specified an outcome, not a number (*"both walk/still is the same mp regen in the end …
keep farming while kiting (slowly)"*), so stand = `max(1, 0.85 × walk)`, which reaches parity at the
top rung and nowhere before it — measured at exactly **100.0%** walk/stand at rung 6. ⚠ They are
multipliers, not flats, because a flat pair balances walk against stand at only ONE level.
Its MP column (35→69 on a passive) was a copy-paste typo he confirmed; zeroed.

**Measured result**: a buffed mage now sustains **146% → 101%** of his main-nuke drain standing, and
**44% → 58%** running. The rotation tightens with level, and the heavier spells (the `ceil` column,
~2× the main nuke) are no longer free at any level.

**🔵 The HP half is deliberately HELD** — *"let finish with the MP first then check IG formulas on HP
regen"*. HP keeps its flats inside and its `hpReg x1.1…x3.4` multipliers; only the shared stance
ladder lands on it. His reason it can wait: HP comes from potions, so HP regen only shaves 10-20% off
potion spend.

Also fixed on the way past: `SkillText` displayed every flat regen grant **×10** (it multiplied a
per-second field by `TickRate`) — harmless while three Warchanter self-buffs were the only users, not
harmless now the whole mastery ladder lives there. And `--check` learned Calm Spirit's three stance
multipliers, so its numbers are verified rather than printing as `UNREAD`.

⚠ **PROTOCOL 27 → 28, NEW APK REQUIRED** — a new class-table entry (the Learn tab is built locally)
and every regen number moving in shared code the client compiles for its own tooltips. No schema
change, so no `game.db` delete.

🔴 **Two left for him**: the rogue's Armor Mastery `mpReg x1.8` and the tank's Heavy Armor Mastery
`mpReg x3.4` are ARMOR masteries carrying weapon-mastery-sized numbers, so his literal ruling leaves
them as percents — flagged, not changed. And `BalanceMatrix.BuildPlayer` never sets a 3rd class, so
every OTHER mage table in the tool is still measuring the level-35 kit; `--mpregen` uses its own
builder and the rest were left alone rather than silently moving every number in the file.

## 2026-08-26 — 0.87.1: the SP bottle's real price, a scam warning, and one page of formulas

Three small things he asked for in one message.

### The SP Bottle sells for what it cost

*"bottles cost 1kkk SP + 100kk gold (csv is a typeo) … i think to make the bottle buy/sell(override)
price 100kk → drinking potions gives 1kkk SP (0 gold) … drinking return 1kkk sp and selling return
100kk gold .. u cannot do both"*.

`SellPriceOverride` = **100kk**, where the /25 consumable rule was giving 4kk. **That symmetry is the
item**: the broker takes 1kkk SP *and* 100kk gold, and the bottle hands back exactly one of the two —
your choice. At a 96% sell loss the choice did not exist. ⚠ Not a gold faucet: no vendor stocks it, so
the only way to sell one for 100kk is to have paid 100kk plus a billion SP to make it.

The CSV header that read `1 SP bottle = 1kkk SP + 100k Gold` was the typo he named; it now says 100kk
and spells out the either/or.

### A skill can be priced in ITEMS

`SkillDef.LearnConsumableId` + a per-level `LearnConsumableAmount`, charged in `HandleLearnSkill`
alongside SP and gold. His 4th-tier files carry an `SP Bottles` column and the learn path had nowhere
to put it — it could charge SP and gold and nothing else.

🔑 **This is what keeps `Entity.SkillPoints` an `int`.** His level-85 row costs FIVE SP Bottles; a
bottle is 1kkk SP, so five is 5kkk against a 2.147kkk ceiling. Bottles **spent** never enter that
number, so nothing has to widen to a `long` and no protocol field changes. Owner: *"Skills cost X
bottles as consumed-ID (u cannot have 5kkk SP int limit to 2.147kkk)"*. **Do not "fix" this later by
widening SkillPoints — the ceiling is the design.**

⚠ Order of operations: **the item is consumed FIRST** of the three charges. It is the only one that can
still fail after its own gate passed (a stack split, an item traded away in the same tick), and taking
SP and gold for a learn that then refuses is the one outcome with no way back.

Nothing authors a bottle price yet — this is the plumbing his `healer 4th` / `buffer 4th` kits need.

### The anti-phishing line

*"when game start/enter and each first whisper in every hour writes a message"*.

`GameConstants.ScamWarning`, shown on entering the world and again on your **first whisper in any
rolling hour** — to **both** sides of the conversation, each on its own clock, so it does not matter
who opened it. Entering the world arms the clock, so the session's first whisper does not repeat it.

🔑 **It rides on the WHISPER and not on a timer** because that is where the scam happens: world chat is
public and self-policing, local chat is a crowd, and a private message claiming to be staff is the
shape of the attack. The warning arrives in the same window the attempt does. It is posted AFTER the
whisper is delivered — a line that arrived first would read as a verdict on the message.

⚠ It promises **no staff member will ever ask**, which the game then has to keep: nothing the server
sends may ever ask a player for a password. The timestamp is runtime-only and not persisted — entering
the world shows the line anyway, so a relog costs one extra reminder and saves a column.

### `docs/Formulas.md`

*"do one md file in the docs folder for fast read (update on change) not to look at 1000 comments to
know which is which .. need it simle Mdmg = baseMagic x whatever / mDef … simpler not over explained"*.

One page: damage, attack/defence inputs, hit/evade/crit/block, fizzle, debuff landing, interrupt, pools
and regen, speed, MP cost, mob curves, drop rates — each with the source file under it. **Every number
in it was read off the code in the same pass**, not remembered; the reasoning stays in the code
comments and here, which is what keeps the page short enough to actually read.

⚠ **It is now a rule in `CLAUDE.md`: a formula change updates that file in the same commit**, the same
rule the skill CSVs run on and for the same reason — a reference that trails the build is worse than
no reference.

**Build green · server boots at 0.87.1 · SmokeTest ALL CHECKS PASSED.** Protocol stays 27. No
`game.db` delete. The APK owed since 0.87.0 still covers all of this.

## 2026-08-26 — 0.87.0: the NUKER's 3rd class, off a file that had been finished for days

The third fully-authored 3rd class. `docs/data/classes_skills_csv/nuker 3rd.csv` — **208 rows, 21
families, no `NOT DONE` banner** — has been complete since before `healer 3rd.csv` was, and nothing in
the game read it: the two nuker disciplines still taught the placeholder kit that survived the
2026-08-10 purge under his point 1, *"leave the mage"*. They don't any more.

**Magus and Tempest, all three races, 40 → 74.** Registered to **both** disciplines, because his file
carries no discipline column — the same treatment `tank 3rd` gets, and deliberately the safe direction:
nobody is locked out of a spell he wrote, and splitting them later is one line plus a column.

### The kit

Eleven families shared, and the race splits the rest — **four ways for the Human, three for the other
two**, which is his authoring and not a slip (Arcane Void is utility, not damage):

|        |                                                                                                                                                                                               |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| shared | Anti-Magic · Spellcaster Weapon Mastery · Mage Armor Mastery · Restore Spirit · Phase Shift · **Elemental Blast** · **Quick Blast** · **Elemental Wave** · Elemental Burst · **Thunderstorm** |
| Human  | **Arcane Wave** · Vampiric Bolt · **Arcane Void** · **Arcane Burst**                                                                                                                          |
| Elf    | **Frost Spikes** · **Frost Pierce** · **Frost Burst**                                                                                                                                         |
| Ork    | **Witches Curse** · **Witches Scarecrow** · **Pyro Burst**                                                                                                                                    |

🔑 **THREE OF THE FOUR PASSIVES WERE ALREADY BUILT AND ARE SHARED, NOT COPIED.** Anti-Magic's rungs
7-20 are the ladder he asked to be shared across all three files. **Spellcaster Weapon Mastery IS the
healer's skill** — its fourteen rungs matched this file's rows to the last digit, exactly as the note
on that def predicted ("the nuker file will want the same row"). Only Mage Armor Mastery stays the
nuker's own, because it alone carries `mpWhenRestored` and its @48 P.Def is 50 where the healer's is 47.

### What the 40+ purge took out with it

The invented half of the nuker's table is gone: the Annihilate / Chain-Lightning renames, the ten-rung
Elemental Burst, Frost Bind, Entangling Roots, Glacial Spike, Creeping Frost, Mana Barrier, and the
**2nd-class ladder's 40-80 tail** (Elemental Bolt, Quick Bolt, Vampiric Bolt, Restore Spirit and Mage
Armor Mastery all continued past 35 by *us*, in the band that had no file). Defs stay in the catalog,
learn lines are gone — the standing rule.

🔑 **That also kills the two dead-end rungs the notes have been carrying**: Flamebolt @40 and Glacial
Spike @44 were single 40+ placeholders whose fizzle curve made them unusable by 58 and 62. BalanceMatrix's
SPELL LADDERS table now shows every nuker spell topping out at 74 and viable past 89 — **no `!` on a
single one of them**.

### Four skills re-authored to his rows

**Elemental Burst** was ten invented rungs (150 → 250 power, 4k SP, 1 stone, 3s reuse); his is **three
rungs at 58/66/74**, power 120/133/154, a **1-second cast on a 5-minute reuse**, eating **2** stones.
**Phase Shift** was one 400-unit blink for 20 MP; his is a **three-rung ladder whose ladder is the
distance** (200/400/600 at 52/62/72, 96-138 MP). **Restore Spirit**'s rungs 2-5 are his four rows and
the ladder now **stops at 66** — deliberately stingier than ours, his own note: *"Intentionally
decreased as mp regen with x3.4/x1.2/x1.2 = ~x5; still alot more mp and alot less hp than IG"*.
**Vampiric Bolt** becomes the Human nuker's 3rd-class spell, rungs 6-19, and gains his **900 range**
(the 2nd class keeps 750 — per-level `Range`).

### Engine work: two new primitives, and three that turned out to already exist

⚠ **Grep before declaring a primitive missing.** `SureHit` already zeroed the fizzle in all three
landing arms, so **"Never Fizzle"** on the three level-74 bursts needed nothing. `Cancellable: false`
already existed, so **BURN** — his *"true dmg per second no cure and can kill"* — needed nothing
either: every DoT here already calls `ApplyDamage` with a **raw** number (true damage) and calls `Kill`
if the bar empties, and non-cancellable is the existing switch `DispelFrom` honours. And *"decrease SPT
resistance by 40%"* is simply a **negative `CcResistMagical`**, which `RecomputeDerived` already sums
and clamps to [0, 0.8].

Genuinely new:

- **`SkillDef/SkillLevel.DotPower`** — the first skill where the impact and the burn are different
  numbers. Pyro Burst hits for 150 and then burns for his 100/s; every DoT before it *was* its Power,
  so `DotPowerAt` falls back to Power and nothing else in the catalog moved.
- **`MpReceivedPct`** — the mana twin of the `DebuffHealRecv` anti-heal, multiplying the same
  `Entity.RestoreMpMod` the robe mastery raises. His row asks for both halves; a burn that stopped
  heals while the victim refilled with Restore Spirit would be half a skill.
- **`SkillLevel.DispelCount`** and **`SkillLevel.BlinkRange`** — Arcane Void's whole ladder is the
  count (2/3/4) and Phase Shift's is the distance. Both were def-only fields, so every rung would
  silently have used rung 1's number.
- `CcResistMagicalAt` tests `!= 0` instead of `> 0`, or Arcane Burst's negative would have been
  discarded. No existing skill authors a negative there.

Also finally used for the first time: **`PvpDamageMult`** (Quick Blast's *"Power in PVP x0.5"*) and
**`InterruptMult`** (the elf's ×2 — and note his rule that this is UNRELATED to `DebuffLandMod`:
*"does dmg but have a lower success rate for the slow - interrupt unaffected"*).

### Five corrections to his file, all flagged

- **Thunderstorm's SP read `4000` at @62 and @70** against a band ladder that is 170k and 390k there,
  and 880k on the skill's own last rung. A 4,000-SP spell at 62 is free. → the band prices.
- **Quick Blast and Witches Curse @52 both read power 52, the same as @48** — the identical defect
  Holy Ray had at the identical rung of the identical ladder. → **57**, his own 2026-08-20 rule.
  ⚠ Not a debuff-magnitude plateau at the top of a ladder, which he restored on purpose two days ago.
- **Arcane Void's row said `0,self`** — copy-pasted from Phase Shift above it, identical MP column —
  while its DESCR says "of the target". → `900,enemy`.
- **Arcane Burst's DURRATION read 0** where its two siblings read 15. → 15.
- Three section headers named the wrong skill (cosmetic, his file only).

🔴 **CALM SPIRIT IS NOT BUILT — his hold, not an omission.** Its six rows multiply MP regen ×0.3 → ×0.7
while **running** and ×1.03 → ×1.2 while **walking**, on top of the engine's stance multipliers (run
×1.0, walk ×1.2), so at 74 a walking mage sits at ×1.44 and a running one at ×0.7. That is exactly what
he intends — *"a farming mage will click walking, and in pvp need to click run but regen slower"* — but
it lands in the middle of the open MP-regen question (`BL-92`), so: *"w8 on calm spirit"*. `--check`
reports the family as NOT REGISTERED until he says go, which is the flag working.

`--check` also gained a `\d+ radius` rule for its DESCR reader — his AoE rows read "in 200 radius with
m.Atk +30" and it was calling the M.Atk 200.

**Build green on both solutions · server boots at 0.87.0 (69 NPCs) · `SkillCsvSeed --check` clean on
all twelve files but the held family · SmokeTest ALL CHECKS PASSED · BalanceMatrix's spell ladders
confirm no nuker spell now dies inside the level range.** ⚠ **Protocol stays 27, but a NEW APK IS
REQUIRED** — the client builds its Learn tab locally from the compiled `ClassSkills`, so a phone on
0.81.1 will not show one row of this.

## 2026-08-26 — 0.86.1: retention is 90 days

One number, and it switches the purge on. Owner: *"90 days retention no point in keeping more .. if
some1 gets reported .. must take no more than a week to deem him banable or not"*.

`GameConstants.ChatLogRetentionDays` goes **0 → 90**, and the six-hourly sweep wired in 0.86.0 starts
deleting. `_nextChatLogPurgeUtc` is `MinValue`, so the first autosave after a boot sweeps — a server
restarted more often than every six hours would otherwise never purge at all, which is this machine's
habit exactly.

🔑 **The reasoning is worth more than the number, because it is what a future change has to argue
against.** The window is not sized to how long the evidence stays *interesting* — it is sized to how
long a CASE can stay open, and a case is a week. 90 days is ~12× the longest decision he will tolerate:
slack for a report that arrives late, a moderator on holiday, or a pattern nobody notices until someone
finally looks — and still far short of a permanent record of everything every player ever whispered.
**Do not raise it "to be safe"**; that is the instinct he ruled against, and an indefinite chat archive
is a liability rather than a safety margin.

`PurgeChatLogAsync` still returns 0 and deletes nothing for `days <= 0`. That guard now matters more,
not less: it is the difference between *retention is switched off* and *purge everything*, on a method
whose rows are the evidence a ban rests on.

That closes `BL-89` completely — nothing about the chat log is owed any more except a playtest.
**Protocol stays 27.** No APK, no `game.db` delete.

## 2026-08-26 — 0.86.0: `BL-89` — the chat log gets a READER, and it does not wait for the autosave

The write half shipped in 0.81.0 and nothing ever opened it. Every delivered line has been going into
`ChatLogRecord` for days; the only way to *see* one was to open `game.db` on the machine, which is not
available to a moderator holding a phone — and the moderator holding the phone is the entire point, since
the case that opened this was *"an admin/mod should ban based on som1 is trying to sell u for $ on private
chat"*.

### `/chatlog` — four shapes, freely combined

```
/chatlog                        the last 25 lines anyone said
/chatlog <name>                 what that character said — or was whispered
/chatlog <name> -w              whispers only: the channel this feature exists for
/chatlog around 15m             a window ±10 min around that instant, for a fresh report
… -p 2                          the page BEFORE, for any of the above
```

`around` takes the RELATIVE forms first (`15m`, `2h`, `1d`) because that is what a real report produces —
a player says *"about ten minutes ago"*, and nobody should do clock arithmetic on a phone to act on it.
`11:02` means that time **today in UTC**, and a full `yyyy-MM-dd HH:mm` says which day. Everything prints
UTC because that is what the rows hold; a server-local zone here would print times a moderator could not
match back to anything.

Pages read **oldest-first**, which is how a conversation reads, and paging walks *backwards* through the
same ordering — page 2 is the block before page 1, not a re-shuffle. A name matches **case-insensitively**,
the same lesson `/jail` learned: SQLite compares TEXT with `=` case-sensitively, and `/chatlog test1`
missing every row for "Test1" would read as *this player never said anything* — the most misleading answer
this command can give. For the same reason an empty page distinguishes **"nobody said anything"** from
**"the log does not reach back that far"**, and prints the total and the oldest timestamp either way.

A name query matches the **receiver** too, because a whisper has two ends and a report names whichever one
the moderator was told about — usually the victim's.

### 🔑 It flushes before it reads

Accepted lines sit in `_chatLogPending` until the 60-second autosave. Reading straight from the table would
therefore have been **blind to the last minute** — and the one case this feature exists for is a LIVE report:
*he is whispering me right now*. A moderator who types `/chatlog` the moment they are told, sees nothing, and
concludes the player is innocent is worse than having no command at all. So the pending batch is detached on
the tick thread that owns it and written by the worker **before** the query runs.

### The open question, answered provisionally

The backlog flagged that this is the first table holding something a player would call **private**, and that
it was worth deciding *once* whether staff below admin may read whispers. The split shipped:

- **Moderator and above may read whispers.** They hold the jail and the kick, the feature exists for the
  private-channel RMT case, and a punishment handed out without the evidence it rests on is the thing this
  was meant to stop.
- **A Chat Moderator reads the PUBLIC channels only.** That rank is deliberately handed to someone you do
  *not* fully trust (playtest 26), and a mute needs no private mail to justify it — everything a chat mod
  polices was said out loud. `-w` is refused for them, and the whisper rows are filtered out of every page
  they see regardless of the query.

🔴 **Both halves are one line to reverse** if the owner rules otherwise.

### Retention: wired, and deliberately inert

`GameConstants.ChatLogRetentionDays` is **0 = keep everything, forever**, and that is the shipped value.
`FlushChatLog` calls the purge every six hours and 0 returns immediately, so the machinery is finished and
nothing is destroyed by a default nobody chose — the rows a purge would delete are the evidence a ban rests
on. 🔴 **The number is the owner's to name** (*"30 or 90 days — but the sensible window depends on how long
after the fact a report arrives"*); the day he says one, it is a number, not a wiring job.

### Covered by the smoke test

Six new checks, and they exist for the usual reason: a query SQLite refuses to translate, a name match that
misses on case, a page that comes back newest-first — every one of those renders as a tidy, plausible, WRONG
page in the System tab. The flush is checked by reading back a line said **half a second earlier**, so if that
ever regresses the test fails immediately instead of passing on the autosave's luck.

**Protocol stays 27** — `/chatlog` prints into the System channel, which every client already draws. **No new
APK needed**, and no `game.db` delete: the `ChatLog` table has shipped since 0.81.0 and its schema is untouched.

## 2026-08-26 — 0.85.2: a buff rung carries its LEVEL in its rank, and two 0.85.0 flags stop being flags

Three small things that were each written down as known-broken and left. `BL-85` had been deliberately
deferred (*"it wants its own increment"*); the other two were flagged in 0.85.0's own changelog entry.

### `BL-85` — a Lv1 Harmony no longer evicts a Lv5

Every rung of a Harmony is one `SkillDef` with `Levels[]` and **one** `Rank`, and `BuffPlan` read that
flat number for a buff with no children. Rung 1 and rung 5 therefore competed as EQUALS, and equal rank
keeps whichever has the longer time left — so a level-44 Warchanter's Harmony of Protection Lv1
replaced a level-74's Lv5 the moment the Lv5 dropped under five minutes. `/buff harmony of protection 3`
on a fully-buffed character did the same, which is how it surfaced.

`BuffPlan` now carries the level for a **childless multi-level** buff — `Rank + level - 1`, the same
shape `GroupRank` already used for groups. The single ladders (Might, Focus, …) were never affected:
they are one-child WRAPPERS and resolve to a child def that carries its own Rank.

🔑 **The backlog called this a one-line fix and it is not**, because of a ruling written in the same
file it touches. **Great Might and Great Bulwark deliberately share `great_blessing` at Rank 1 at every
level** so that casting either evicts the other and the choice stays re-makeable mid-fight. Carrying
the level there would let a Lv3 Might lock out a Lv1 Bulwark — the opposite of the ruling. So the rank
ladder is opt-OUT: **new `SkillDef.FlatRank`**, set on exactly those two.

A **startup guard** now refuses any second def that ladders on a key another def already ladders on,
naming both and pointing at `FlatRank`. It fired immediately on the tank's Anti-Magic vs the mage's —
a false positive, since PASSIVES never reach `ApplyBuff`, so the guard skips `SkillCategory.Passive`.

Verified with `BalanceMatrix --buffs`: Harmony of the Warrior Lv6 → rank 105, Protection Lv5 → 104,
Speed/Wizard Lv2 → 101, Great Might/Bulwark → 1. Mana Blessing gains something too — its hidden Lv3
rung (`buff_mana_blessing_3`, Rank 3) used to outrank the healer's own Lv3 and every 4th-class rung
above it.

### Holy Soul's 50 HP/s is real

0.85.0 shipped it inert and said so: *"HP UPKEEP has no field … as written the toggle is a straight
MP-cost win, which is NOT what his row says."* **New `SkillDef.HpPerSecond`**, the twin of
`MpPerSecond`, charged by the same `TickToggleUpkeep`. Both halves are tested before either is charged,
so a stance you can only half-afford takes nothing instead of draining one bar and then dropping.

🔑 **A stance never kills you** — the HP test is strictly greater-than, so the toggle drops itself while
HP still remains. The skill card names both bars now ("costing 50 HP a second").

### Healer's Shield Mastery is actually shield-gated

Also flagged in 0.85.0: the two numbers rode a plain passive with the shield test left to the player's
honour, so a healer who swapped to a two-handed staff kept +10% healing and +10% MP regen. **New
`PassiveEffect.RequiresShield`**, checked at the top of `ApplyPassive` so no field can leak through.
All-or-nothing, unlike the existing per-field `BlockChancePct`/`ShieldDefPct` gate — those scale the
shield's own numbers and are inert bare-handed anyway, while heal power is not.

No CSV changed: in all three cases the code was behind his file, not the other way round.
`SkillCsvSeed --check` green on all eleven. No protocol change.
## 2026-08-26 — 0.85.1: a sigil only excludes its own SLOT now, so all three may come from one class

He relaxed the rule the same afternoon it shipped: *"1-attack, 1-Defence, 1-support from any
race/descipline"*, and asked first whether any same-flavour trio was overpowered.

**None is, and the reason is structural rather than lucky.** The eighteen were authored
one-per-slot-per-flavour with no intra-flavour synergy — a flavour's three act on three different
channels, so nothing in a trio multiplies another member of it. The only trio worth arithmetic was
the TANK's, the one that is all mitigation: +10% max HP, Aegis's +25% to both defences (a DIVISOR,
so −20% damage taken) at roughly 40% uptime, and −10 points of crit chance with −10% crit damage.
That is about **+26% effective HP** — and the OLD rule's best defensive pairing, **Aegis
(Tank-Defence) + Immortality (Buffer-Support)**, was worth the same and left the Attack slot free.
The trio is not stronger than what was already buildable, only purer.

🔴 Flagged on the way past, and not a combination: **Immortality's uptime scales with the NUMBER of
hits taken, not their size** — near its 20% ceiling against many small hits, ~10% against a slow
boss. It is most reliable when you are safest and least reliable against the burst it exists to
survive. Aegis and Holy Support have the same shape.

The guard is gone from three places (the catalog's REPLACES generator, the learn path and the Sigils
tab), each with a note saying what it was and how to restore it. No protocol change.


## 2026-08-26 — 0.85.0: the FOURTH CLASS finally teaches something — the Lightbringer's 76-90 kit, an all-classes shared kit, and eighteen SIGILS on a tab of their own

The 4th class has existed since 0.70.0 and granted **nothing but a name**. The reason was written
into `Classes.Fourth.cs` at the time: `ClassKey` had no tier, so a 4th kit registered against
`Discipline.Lightbringer` would have leaked to every level-40 Lightbringer. That tier exists now, and
with it his three finished 4th-tier files are built.

### The tier

`ClassSkills.ClassKey` gained a `Fourth` flag and `Cumulative` a `fourth` parameter, threaded through
the learn gate, the SP price, the learn list, the rung-level lookup (which the fizzle and contested
rolls read) and the client's Learn tab. **Level 76 is not the gate** — the 100kk Rite of Ascension is.
A level-76 Lightbringer who has not paid is offered exactly what they were offered at 75.

### `healer 4th.csv` — the Lightbringer, 76 to 90

Twenty ladders continue past 74 and eleven families are new.

- **Continued**: Anti-Magic (M.Def 113→149, mRes 30→35%), Spellcaster Weapon Mastery, Healer Armor
  Mastery (which now also carries an M.Def **percent** and an MP-cost cut), Holy Ray, Great Heal,
  Party Great Heal, Quick Great Heal, Healer Blessing, Healing Totem, Ultimate Heal and its party
  twin, Mana Ray, Mana Strain, Weapon Break, Gravity / Bind / Armor Break, Mana Blessing, Fortitude,
  Resurrection, Resurrection Field, Antidote.
- **New**: Healer's Shield Mastery, Arcane Resistance, Holy Blessing (the only cure in the game with
  no rank ceiling), Holy Soul (a toggle), Healer's Power, Healer Party Blessing (Elf), the three
  **Restorations** (one per race, one-hour reuse) and the three **Marks** (one per race, one shared
  buff key — an ally wears one Mark, never two).
- **Rite of Preservation moved tiers.** It arrived in 0.70.0 as one of two skills he named
  individually, before there was a 4th file to hold it. His CSV now carries it, so the CSV wins: 500
  MP, a 1s cast, 500kk SP + 100kk gold and **five Holy Stones**, and its learn line moved off the
  3rd-class table. 🔴 The Bulwark's twin (Undying Will) did NOT move — `tank 4th.csv` is still a
  placeholder, and re-tiering a skill with no file behind it would put it out of reach with nothing
  to say when.

### `shared 4th.csv` — every class, same rows

Five passives, registered once rather than fanned across 36 (race, discipline) keys: **Strong Mind**
and **Strong Body** at 76 (the two halves of debuff resistance, split by the stat that defends them —
Mind for Spirit, Body for Constitution), and at 83 **Arcane Protection** (+15% M.Def and a magic-only
defensive proc), **Magic Proficiency** and **Physical Proficiency** (weapon-conditional, with a proc of its own).

### Named on the way past

His `buffer 4th.csv` has a party-wide Mark at 79 that had no name yet; it is **Harmony Mark**. It
keeps the family's `<Word> Mark` shape rather than flipping to "Mark of Harmony", and HARMONY is the
buffer's own signature the way Holy / Life / Blood are the three races'. 🔴 Not built — that file is
still in progress — but when it lands it MUST share the Marks' buff key, or a healer's Mark and a
buffer's would stack.

### The SIGILS — eighteen passives, three slots, one tab

His file called them *runes*; **RUNE was already a held item** in this game (War Rune / Spell Rune)
and **MARK** is now the Prophecy-shaped blessings, so he chose **Sigil**. Six class flavours × Attack
/ Defence / Support, 20kk SP + 10kk gold each, and **any class may take any of them** — his
"Fighter ideal: …" lines are advice, and he relabelled them *ideal* the moment it was asked.

The exclusion rule is **two** rules, both read straight off his REPLACES column: one per **slot**
(carried by `ExclusiveGroup`, which the learn path already enforces) and one per **flavour** (its own
check). So the three you wear always come from three different classes.

- A **SIGILS tab** in the Skills window, laid out as three slots rather than eighteen skills, shaped
  after the Stat-Swap tab as he asked. It refuses to open before the ascension, and a row you cannot
  take says *which* of the two rules is stopping you — the server refuses both and a tab that drew
  them identically would look broken.
- **Reset at the Mindwright** for 10kk gold **per sigil**, no SP and no gold refunded. Everything
  else there is still free to forget; that was the deal those were sold under.

### Engine work these needed

- **A defensive PROC trigger.** Half the sigils are "chance on damage *received*", so the on-hit proc
  machinery grew `ProcOnDamaged` (and `ProcMagicOnly`, for Arcane Protection). A proc payload can now pay
  out in HP or MP as well as in a buff — the handler dispatches on the rung's own effect flags.
- **Immortality.** `FreezesHp`: while it is up the holder's HP does not move — damage takes none off
  and healing puts none back, exactly as he described it. It is **not** `Immune`: the blow lands, it
  still threatens, still flags PvP, still contests your cast. The freeze cuts both ways, which is the
  balancing half of it.
- **Five new per-level fields** — `DurationTicks`, `CooldownTicks`, `ConsumableAmount`, `ResHpPct` and
  the heal-power pair. Every one is a ladder his 4th tier authors and the 3rd did not: Bind's hold
  grows 31→40s, Ultimate Heal's reagent goes 1→2, Resurrection stands you up at 35% then 40%.
- `PassiveEffect` gained the per-school CC resists, an MP-cost cut, magic evasion and PvP damage
  taken; `StatMods` gained `MpCostPct`.
- **M.ACCURACY, the mirror of M.Evasion.** He asked what his Marks' `M.Acc` meant and answered it in
  the asking — *"the mAcc is magic fizzle chance? what does Magic evasion do? so the oposite"*. Yes:
  `MagicFailBonus` is flat percentage POINTS the DEFENDER adds to your spell's fail roll, and
  `MagicAccuracy` is now the same points taken back off it by the CASTER. Holy Mark grants +4, Blood
  Mark +3. ⚠ This does NOT reopen the caster-side accuracy STAT the 2026-08-10 rework deleted — that
  was something you carried and levelled; this is a flat grant a named skill hands out, on exactly the
  footing M.Evasion has had since 2026-08-11. Nothing derives it and no gear rolls it.

### The tooling

`SkillCsvSeed --check` now walks `healer 4th.csv` **and** `shared 4th.csv`, and three of its own bugs
came out in the process: `kk` parsed as thousands (so every 4th-tier price read 6500 instead of
6,500,000), a passive's CD/DURATION columns were compared against the skill's own zeroes rather than
against its **proc's**, and anything with an `ExclusiveGroup` was skipped — which would have hidden
all eighteen sigils. ✅ Green on all eleven files.

### 🔴 Two CSV typos corrected, both flagged

Per the standing monotonic rule, a value that goes backwards is a typo. **Party Great Heal @82** read
760, below the 770 at 81; it is **775**. **Mana Blessing @90** read an MP cost of **20** against 190
at 88; it is **200**. Both were changed in the CSV so `--check` stays honest — if either was meant,
the CSV is the authority and they come back.

⚠ **Protocol 26 → 27, and a new APK is required.** Not a byte of the wire moved — the same shape as
25 → 26. The client builds its Learn tab from the compiled class tables, so an old APK shows an
ascended Lightbringer an empty 76-90 ladder and no Sigils tab at all. No `game.db` delete is owed.


## 2026-08-26 — 0.84.0: interrupt is IG's own formula — damage vs your HP pool, Resolve becomes a percent, his debuff ladders plateau, the nuker's two Frost skills get x2 interrupt, and an SP Broker sells SP Bottles you must confirm before drinking
## 2026-08-26 — 0.84.0: interrupt is IG's own formula — damage vs your HP pool, and Resolve becomes a percent

He brought IG's interrupt formula, which is what 0.83.0's model was explicitly parked waiting for.
It replaces that model whole; almost nothing of it survives.

```
BaseChance  = (DmgTaken / MaxHP) x random(100..120)
FinalChance = BaseChance x MEN-mod x (1 - Buffs/EquipMod)
```

His worked example: 1000 damage on a 2000 HP pool is a base of 50; ×1 × (1 − 0.54 Resolve) = **23%**.
`StatCalculator.InterruptChance` reproduces it exactly.

**1. 🔑 THE YARDSTICK IS THE CASTER'S HP POOL, NOT THE SPELL.** Every input 0.83.0 needed — the spell's
damage, its cast time, its **reuse**, the attacker's DPS, both sides' WIT and level — is gone. One hit is
measured against the one quantity that is always defined: how much of the caster it just took off.
`Entity.CastingInterruptReference` and `GameLoopService.CastInterruptReference` are deleted.

That kills the 🔴 **Thunderstorm finding** outright. 0.83.0 priced a cast against its own DPS, so a 300s
reuse made the game's biggest nuke the *easiest* cast in the game to break. Reuse is not an input any
more, and a 5s Thunderstorm is now simply a cast that eats more hits than a 1.5s one — which is the
behaviour he wanted and the reason he called ours provisional.

**Cast time is still in there, but as an emission rather than a term**: a longer cast takes more hits, so
it breaks more often, at exactly the rate the hits arrive. His *"they cast fast enough for a small window
to interrupt"* is the whole default defence a mage gets.

**2. RESOLVE IS A PERCENT.** *"Apperanlty resolve is % not flat number 54%"*. **The numbers did not move** —
the ladder is still 18 / 25 / 36 / 40 / 42 / 48 / 54 and the CSV rows still say those figures; each one now
reads as a percentage of the incoming roll. `ModifierMode.Flat` → `Percent`, and the four CSVs that author
Resolve (`cleric 2nd`, `healer 3rd`, `buffer 3rd`, plus `Arcane Serenity`) grew a `%`.

This is what fixes the decay `BL-91` reported: as flat points against a `wit·2 + level` pool, +54 cut
interrupts by 47% at level 20 and only 30% at 80. As a percent it is ×0.46 at every level, forever.

**3. THE MEN CURVE, FLATTENED TO HIS NUMBERS.** IG's is geometric at ~4.8%/point (20 MEN = ×1.00,
50 = ×0.23). On our SPT bases that prices a level-39 human mage at ×0.395 and a 50%-HP hit at 9% —
*"a bit low"*. `StatCalculator.SpiritInterruptMod` uses the curve he named instead: **20 SPT = ×1.00,
50 SPT = ×0.67**, same geometric shape, one third the slope. On our own bases:

|      | human ftr 25 | elf ftr 26 | ork ftr 27 | elf mage 32 | human mage 39 | ork mage 45 |
| ---- | ------------ | ---------- | ---------- | ----------- | ------------- | ----------- |
| ours | ×0.94        | ×0.92      | ×0.91      | ×0.85       | ×0.78         | ×0.72       |
| IG's | ×0.78        | ×0.75      | ×0.71      | ×0.56       | ×0.39         | ×0.29       |

**4. 🔴 NO ROBE-SET INTERRUPT RESIST, deliberately.** IG's robe set carries 50% on top of Resolve's 54% and
the product makes a mage effectively uninterruptible — *"and i dont want that"*. `StatCaps.InterruptResistMax`
(0.80) exists so any future source stacks into a clamp rather than multiplying past it.

**5. Unit changes that go with it.** `Entity.InterruptResist` (int points) → `InterruptResistPct` (fraction)
+ `InterruptSpiritMod`, folded by `InterruptMitigation`; `SkillDef.InterruptDefense` int → float fraction;
`PassiveEffect.InterruptResist` and `StatMods.InterruptResist` are fractions; `InterruptPower` and
`BuffInterruptPower` are percentage POINTS added to the final roll, which is why **Disrupt's 99999 still
guarantees a cancel**. `MagicInterruptPower`, `InterruptResist(wit, level)`, `InterruptPoints` and
`InterruptPerCast` are deleted — WIT is not in the interrupt on either side any more.

⚠ The `StatsUpdate`/`TargetInfo` field `InterruptResist` keeps its type but changed meaning: it is now the
folded resistance as a whole **percent** (a level-74 human mage under Resolve reads 64, not 118). The wire
did not change, so the protocol stays at **26** — an old client just prints a differently-scaled number
without its `%`. The Unity target panel appends the sign.

**6. 🔴 BALANCEMATRIX WAS BUILDING EVERY PLAYER AT SPT 0.** Found while measuring this: `BuildPlayer`,
`BuildStarter` and the buffed farm roster seeded Con/Atk/Wit/Agi and **not Spt**, so `SptModifier` clamped
to its floor of 1.16 instead of the 1.44-1.57 a real character gets. Every player Max MP and M.Def the tool
has ever printed was understated by roughly a fifth. Server code was always correct; this is the tool only.
Three lines.

**7. The two new tables.** `=== INTERRUPT: IG's formula ===` (the SPT curve on our bases against IG's, his
worked example, a hit-size grid, and real fighter-vs-mage measurements at 20/40/60/80) replaces both
0.83.0 sections. `=== INTERRUPT: the elf nuker's spells vs a same-level mage ===` answers his last question
directly — see `BL-91`.

**8. HIS LIVE CSV EDITS, SYNCED — and one of them reverses a ruling on purpose.**

*"I fixed healer 3rd skills u had debuffs percent going up and I wanted it to stop (the lvling is the
spell not to fail)"*. 🔑 **A DEBUFF'S MAGNITUDE PLATEAUS; WHAT A HIGHER RUNG BUYS PAST THE CEILING IS THE
LANDING CHANCE.** The engine already works that way — the contested and fizzle rolls read the RUNG's own
learn level, so the top rungs land more often at the same number.

⚠ This is a **deliberate reversal of his 2026-08-20 ruling**, which called a repeated magnitude a defect
(*"if the 40 lvl description is the same as 44 one then the description is wrong"*) and made both ladders
continue their +2 stride. That objection was about duplicated rungs at the BOTTOM of a ladder; a designed
ceiling at the top is not the same thing. Both comment blocks now say so, so the next pass does not
"fix" it back.

- **Gravity** plateaus at **23%** from level 64 (was climbing to 33% at 74).
- **Armor Break** plateaus at **30% P.Def / 15% M.Def** from level 66 (was climbing to 38/19). The
  M.Def-is-half-P.Def identity is preserved.
- **Anti-Magic** magic resistance **25% → 30%** at levels 70/72/74 (all three files that carry it).
- **Harmony of Restoration** rung 13 (@72) MP **464 → 458**, so the top of the ladder reads 452/458/464.
- **Mana Totem** now **replaces Restore Mana** — his `[Restore Mana]` cell. The ork healer's totem is the
  upgrade of the single-target MP restore, not a second skill beside it. ⚠ `--check` does not compare the
  REPLACES column, which is why this one had to be read out of the diff by eye.
- Cosmetic in his file only: two `buffer 3rd` section headers said "Barrier" over rows that already read
  Reinforcement and Sharpening.

`SkillCsvSeed --check` is green again: **no discrepancies.**

⚠ **NOT synced, and not small:** `healer 4th.csv` (255 rows, he says finished) and `buffer 4th.csv`
(150 rows, in progress) are full 4th-class kits in a NEW column layout (`Gold`, `SP Bottles`, `Comment`).
They need engine primitives that do not exist — **skill stones** (a consumable a cast charges: 2 per
Ultimate Heal, 4 per Mark, 15 per Undying Will), **SP bottles** and a **gold-per-level cost schedule** for
76-90, and **runes as mutually-exclusive passives** rather than held items. That is its own build, not a
CSV sync. `tank 4th`'s Undying Will grew the same "Consume 15 skill stones" clause.

**9. RULED: the two nuker interrupt skills are ×2.** *"add the nuker the two high interrupt skills a x2
chance. They are fast cast and x2 interrupt chance is good enough"*. They are exactly the two rows in
`nuker 3rd.csv` that say *"Higher chance to interrupt enemy casts"* — **Frost Spikes** and **Frost
Pierce**, both `m.Atk +64` on a 2.5s cast with a 1s reuse. All 28 rows now also carry
**`(interrupt chance x2)`**, and `Descr.cs` learned that token (twin of `(success chance xN)`), so the
number is verified from the day the kit exists.

At 74 against a same-level human mage they read **21.6% per hit** (9.9% through Resolve) against 10.8% at
×1 — and firing every ~2.5s they compound into roughly a third of a 4s cast. His original ×10 guess
assumed these were small hits; a mage has the smallest HP pool in the game, and ×10 makes either one a
guaranteed cancel, which is Disrupt rather than a nuke.

🔴 **×2 is not in the CODE and cannot be** — the `nuker 3rd` kit is unbuilt, so neither skill has a
`SkillDef`. It lives in the CSV and in `BL-91`, which carries the three-line instruction for the day the
kit lands.

⚠ The BalanceMatrix table's numbers for these two were **wrong before this** — an earlier pass invented
42/1.5s/3s and 55/2.0s/5s where his file says 64/2.5s/1s for both, understating each. Fixed to read his
rows literally.

**10. ⚠ CORRECTING §8: two of the three "missing primitives" were already built.** His answer:
*"We have skill stones and elemental stones … we have a gold cost for learning skills … the swap
skills passives were that kind"*. Both are true and were in the tree the whole time —
`ItemCatalog.SkillStone` (Angel's Protection burns 5/cast, the Lightbringer's rows 1 and 4),
`ItemCatalog.ElementalStone` (Elemental Burst, 1/cast), and `SkillLevel.GoldCost` /
`SkillDef.GoldCostAt`, which the level-40 stat swaps have always used. **Only the runes were really
missing, and he has deferred those** (*"the rune skills are not ready I'll say so"*). What §8 should
have said is that the 4th-class kits need TWO new things, not four.

**11. HOLY STONE and PHYSICAL STONE** — *"need holy and physical(for fighters) stones (same as
elemental)"*. Deliberately identical to the Elemental Stone in grade, rarity, value and 20k shelf
price; only the school that burns them differs. Both join it on the Apothecary shelf. 🔑 Keep them
identical: the moment one is cheaper, every skill gets authored against that one.

**12. THE SP BOTTLE AND THE SP BROKER** — *"u can make an npc to take your 1kkk SP + 100kk gold and give
you a tradable/sellabel(100kk shop-buy price) SP bottle"*. Built end to end:

- **`ItemCatalog.SpBottle`** — S-grade, Epic, tradable, 100kk shop price (so it sells for 4kk under the
  buy÷25 rule). **Not on any shelf**: the broker is its only source, because its price is SP as well as
  gold and a shelf cannot charge that.
- **`NpcRole.SpExchange`** (appended, 8) and **Ledgerkeep Mora** in **Frostmere** — the same town as the
  4th-class master, for the same reason: it is the only town whose neighbours reach the level-76
  ascension. ⚠ The boot assert `ValidateNpcLabels` rejected three placements before one stuck; the west
  column there is seven NPCs deep. Trust the assert, not the arithmetic.
- **`BuySpBottleCmd`** → `HandleBuySpBottle`. 🔑 **The bottle is added to the inventory FIRST and only
  then is the player charged**, so a full bag costs nothing — the other order would eat a billion SP and
  hand back an error. The dialog's `CanAfford` is a button label; the handler re-checks both sides.
- **`SkillDef.GrantsSp`** — a FIELD, not a `SkillEffect` flag, because the flag enum has no bits left.
  Drinking a bottle runs `sp_bottle_use`, a 0-cast skill with the bottle as its `ConsumableId`, exactly
  like every other consumable in this game.
- Client: one section in the NPC dialog stating **both** costs, because SP is invisible on the HUD and a
  player who can pay the gold would not otherwise know why the button is dim.

🔴 **TWO THINGS FOR HIM, both about the same 32-bit number:**
1. **`Entity.SkillPoints` is an `int`, max 2.147kkk.** One bottle fits, two fit, three do not. The drink
   REFUSES rather than wraps, so nothing is ever lost — but his own level-85 row costs **five bottles**.
   That works if bottles are SPENT as a currency (which is how his `SP Bottles` column reads) and breaks
   if they are meant to be drunk toward it. Making SP a `long` is Entity + the persisted record + the
   `StatsUpdate` wire field + the client, i.e. a protocol bump. **Not decided, not done.**
2. **His CSV header says `1 SP bottle = 1kkk SP + 100k Gold`** — one hundred THOUSAND — where his message
   says 100kk. The code follows the message (newest ruling wins). The CSV line is worth a second look.

⚠ `NpcRole` gained a value and `NpcDialog` gained an appended nullable field, so **the protocol stays at
26**: an old client simply draws no section for the broker. A new APK is wanted to use it.

**13. `ItemDef.ConfirmOnUse` — a per-item "are you sure?"** *"the confirmation message should be a bool
flag in any consumable (later if we have others) default at false (true for SP bottles for now)"*. One
bool on `ItemDef`, default **false**, and the client puts an `Ask` between the button and the drink when
it is set. True today only on the SP Bottle.

🔑 **DEFAULT FALSE IS THE WHOLE POINT — a healing potion must stay ONE TAP.** That is why this is a
per-item flag rather than a rule about expensive consumables.

🔑 **IT GATES BOTH USE PATHS, not just the details window.** Item details → Use goes through
`ConfirmUse`, and so does a **skill-bar slot tap** — the bar is one tap with no window in front of it,
which is precisely where a 100kk bottle gets drunk by accident. One helper, so the two cannot disagree
about which items ask. The prompt names what you GET rather than saying "are you sure", the same rule the
Mindwriter and the disassemble confirmation follow.

⚠ **Client-side only, deliberately.** The server has nothing to enforce — drinking is always legal; the
flag only slows your hand. Nothing changes for any other item.

**14. Drinking a bottle returns SP and NOTHING ELSE** — *"drinking them gives 1kkk SP and 0 gold"*. The
gold half of the broker's price is a fee. Now said out loud in the skill's own description ("No gold is
returned"), so nobody expects 100kk back out of a bottle they paid 100kk into.

**15. Over the int ceiling, the drink is REFUSED** — *"Over 2.147kk is lost (or unable to use the
potion.. whatever is cheaper)"*. Refusing is both cheaper and safer than clamping: the bottle is not
consumed, nothing is lost, and the player keeps a tradable 100kk item instead of burning it for a
fraction. `UsePotion` returns false with "You already hold too much SP to drink that." — the ceiling
question in §12 is unchanged and still his to rule on.


## 2026-08-24 — 0.83.0: a debuff has its own SUCCESS RATE, interrupt becomes a DPS contest, and four skills stop lying about being contested

Three asks, then three rulings on the first pass's answers. What follows is the ruled version.

**1. `SkillDef.DebuffLandMod` — ONE FLOAT, DEFAULT 1, PER SKILL AND PER RUNG.** *"DebuffLandMod should be
floating one value - default 1"*. A first pass gave it four named tiers in `StatCaps`; he replaced them
with a plain float and authored the values themselves into the CSVs' DESCR column as
`(success chance x1.5)`. That is the right home — the number belongs to the skill, in his file, like
every other authored magnitude. `SkillLevel.DebuffLandMod` (0 = inherit) carries the per-rung half.

It multiplies **the probability the debuff sticks**, on whichever roll decided it, and never touches
damage or interrupt — his *"interrupt unaffected"*.

**2. 🔑 THE ROUTING BUG HIS ARITHMETIC EXPOSED.** *"armor/weapon break + gravity + Arcane/Fros/Pyro
blasts(nuker 3rd) should be 75% at parity (x1.5) and the other should be 25% at parity (x0.5)."*

×1.5 only reaches 75% off a **50%** base. 50% is the CONTESTED curve; the fizzle path is ~99%. So his own
numbers said those skills must be contested — and they were not. **Armor Break, Weapon Break, Gravity and
Mana Strain each set `DebuffSchool.Magical`, each printed *"Contested ATK vs SPT"* on the skill card, and
each took the fizzle roll anyway**, because the branch tested the effect-FLAG mask (`ContestCc`) and never
read the school the author had declared. `DebuffSchool`'s own documentation has said *"None = not a
contested debuff"* the whole time; nothing read it.

`GameLoopService.IsContestedDebuff` now reads both, and the fizzle branch became an `else if` so nothing
can resolve twice. Exactly four skills move, and they are precisely the four whose descriptions already
claimed to be contested: **~99% → 75% / 75% / 50% / 25%** at parity.

⚠ This is the change most likely to be felt in play. Armor Break used to land nine times in ten.

**3. THE VALUES, FROM HIS CSVs.** Armor Break ×1.5 · Weapon Break ×1.5 · Gravity ×1 · Bind ×0.7 · Mana
Strain ×0.5. From his general rule, MAGICAL only: Entangling Roots, Warding Step ×0.5. ⚠ **His message and
his CSV disagree about Gravity** — the message groups it with the ×1.5 set, all fourteen authored rows say
`x1`. The CSV won.

🔑 **PHYSICAL DEBUFFS STAY ×1** — his narrowing ruling in the same pass: *"physical debuffs should be x1
for now .. Con saves .. we deside later"*. The physical school already contests **CON**, which a fighter
really carries, where the magical one contests SPT, which a mage has given up; taxing both would
double-charge the physical side. Shield Stun, Shield Bash, Stay! and Terrifying Roar went back to ×1.

**4. `--check` READS THE NEW COLUMN — 119 authored rows.** It needed a carve-out that is worth naming:
`Descr.cs` strips every parenthetical as commentary (a rule earned from four false alarms), and his
multiplier lives *inside* brackets. Without lifting it out first, the one number this whole feature is
about would have been the only authored value in the files that nothing verified. Proven by breaking
Armor Break to ×1.4 and watching it report.

**5. 🔴 INTERRUPT WAS DEAD, AND IS NOW A DPS CONTEST.** He asked for the measurement first; the
measurement said the baseline was zero. The old rule was `0.25 + (power − resist)/100` with
`resist = wit·2 + LEVEL` and **no level term on the attacker's side** — measured mage vs mage at parity,
**5% / 0% / 0% / 0%** at levels 20/40/60/80. One authored `InterruptPower` in the whole catalog (Disrupt,
99999), zero authored `InterruptDefense`. Nothing had ever interrupted anything.

His replacement: *"chance per spell to interrupt 33% … high atack speed low dmg will interrupt on average
same amount as high dmg low … the average should be 33%"*.

🔑 **Requiring the per-cast total to be independent of cadence FORCES the per-hit chance to be that hit's
share of the damage — and then the attacker's DPS cancels out of the formula entirely:**

```
p(hit) = 0.33 × hitDamage / (spellDps × castSeconds) × attackerPoints/defenderPoints × InterruptMult
```

So the chance one hit breaks a cast is literally *how big that hit is next to everything the spell itself
would produce in the time it takes to cast*. `spellDps = base_dmg / (base_cast + base_reuse)` is his
definition verbatim, priced against the **caster's own defences** so the incoming hit and the spell sit on
one yardstick, and computed once at cast start rather than per incoming swing. `points = wit·2 + level` on
**both** sides, so parity is exactly ×1 — the same rule `CcLevelBase` is built on. Disrupt is unchanged.

Verified: three attackers with identical DPS and cadences of 2s / 1s / 0.25s all come out at **28.9%** over
one cast. The invariant holds exactly.

⚠ `Entity.MagicInterruptBonus` no longer seeds itself with `wit·2`. Both sides now read WIT through
`InterruptPoints`, so the old seed would have counted a caster's WIT twice and broken the parity property.
The field now means what its name says: the buff/passive bonus on top.

**6. TWO QUESTIONS THE MEASUREMENT ASKS BACK** — both on `BL-91`, both printed by the tool:
**real builds land at 58-94% per cast** (his parity is *DPS* parity, and a geared fighter out-DPSes a
mage's nuke), and **Resolve's +54 decays** from a 47% cut at level 20 to 30% at 80, because a flat buff on
a ratio always shrinks.

**7. `BalanceMatrix` gained `=== DEBUFF SUCCESS ===`, `=== INTERRUPT ===` and `=== BALANCING RESOLVE ===`,
the last on his ask — Elemental Blast and Thunderstorm against a tank, a warrior and duals, basic attack
and best skill, with and without Resolve.

🔴 **AND IT FOUND SOMETHING.** Thunderstorm is **100% interrupted by everything**, because his rule prices
a cast against its OWN DPS and a 300-second reuse makes that DPS almost nothing. The big rare nuke is the
easiest thing in the game to break. If a long-cooldown spell should instead be HARDER to break, the
reference has to be the spell's DAMAGE, not its DPS — his call, and it is a one-line change.

**8. EIGHT CSVs REGROUPED PER SKILL.** *"please redo the Healer 3rd and all 1st/2nds to be per skill not
per level organized (to be like buffer/nuker)"*. `healer 3rd` and all seven 1st/2nd files: level banners
out, a `----Name----` banner per skill with its rungs ascending. **Row text byte-identical** — verified by
comparing the sorted multiset of non-banner rows before and after, then `--check` green.

⚠ **A new APK is owed** for the corrected skill descriptions, which the client reads from its own copy of
`Game.Shared.dll`. No wire change — protocol stays 26.

## 2026-08-24 — 0.82.0: a dungeon is a CORRIDOR now, the gate lands you at its mouth, and a Scroll of Return goes to a town

Three finds in the Hollow Crypt, and the third is a shape.

**1. THE GATEKEEPER WAS DROPPING HIM IN THE MIDDLE.** *"Entering trough GK teleports me in the middle …
not the start."* Literally true and easy to miss in the source: the crypt's field gate was authored at
`(-9600, -11000)`, which is the centre of the SECOND of its four spawn rings — the middle of the dungeon
by construction. The gate is generated at the corridor mouth now, just inside the entrance safe zone.

**2. THE ESCAPE BUTTON WAS PUTTING HIM BACK IN THE CRYPT.** *"using scroll of return -> returns me to the
starting chamber of the crypt .. not a main town … the return scrolls should teleprt you back in town not
in the start of the dungeon - its valid even for a instance (u reenter)."* `ReturnToTown` asked for the
NEAREST SAFE ZONE — and a dungeon entrance is a safe zone, so inside a dungeon it is always the nearest
one there is. The escape button was finding the door of the place he was escaping from.

Two things changed, not one. `SafeZone` gained a `DungeonEntrance` flag and `WorldMap.NearestTown` skips
those — a separate flag from `RegenBoost`, which happens to be false for the same three zones today but
means something different (resting, not home; the training outpost is the case that splits them). And the
scroll asks `RegionMap.ManagingCity` FIRST, exactly as a town respawn already does: nearest-town alone
answers **Frostmere** from inside the Hollow Crypt, which is geometrically true and useless, because
Greymarsh is the only gatekeeper that lists the crypt. His *"(u reenter)"* is the whole point — you
leave to a town you can leave FROM.

**3. AND THE SHAPE.** *"can we make the dungeons(valid for all) with one main cooridor few side rooms for
mobs — if 3 mob groups -> 2 rooms and the last one is protecting the boss as of now … number of mobs
groups -1 is the rooms on the sides .. and in the end of cooridor is the boss with the last group upfont
(far enought so u can go trough and atack the boss without newly spawned elites to aggro u - same as
now)."*

🔑 **That is a rule about COUNTS, not a drawing, so it is GENERATED** — `Game.Shared/DungeonLayout.cs`.
A dungeon is now one plan: a door, a direction, a list of mob groups and a boss. From that the file
derives the outline, the arrival gate, every spawner and the wall. **N groups ⇒ N−1 side rooms, the Nth
group in the corridor in front of the boss.** Add a fourth group to a roster and the third room, its
walls and its spawner all appear together.

What it replaced is why: twelve literal spawn circles in `WorldMap.cs` and three hand-drawn twelve-vertex
polygons in `Regions.cs` — the second and third being the first translated 10k and 22k SW — which had to
be kept agreeing with each other by eye. A corridor with rooms is not a shape anyone should draw twice.

The measurements, since none of this is visible from the source:

|                               |                                                                                                      |
| ----------------------------- | ---------------------------------------------------------------------------------------------------- |
| corridor                      | 600 wide, mouth inside the entrance circle, ~4950 long to the boss chamber                           |
| side rooms                    | 900 × 750, alternating sides, 1400 pitch — you cannot reach the boss without walking past every door |
| boss chamber                  | 1400 × 1400 at the end                                                                               |
| **guard → boss clear ground** | **850 units, against a 400 aggro range**                                                             |

That last row IS his rule, and `DungeonLayout.Validate()` asserts it at boot rather than trusting the
arithmetic to survive the next edit — the symptom of getting it wrong is a boss fight that occasionally
goes wrong, not an error. A second boot guard checks the corridor mouth still overlaps its entrance
circle, because the failure there is a wall across the only door.

🔴 **THE PRICE, MEASURED AND ACCEPTED: corner-cutting got much worse, and it had to be paid for.** This
game has no pathfinding — a move order clamps its DESTINATION once and then draws a straight line — so a
concave world lets a walk clip a wall corner. The old diagonal band cut on 0.76% of point pairs by at
most 129 units. A corridor with rooms is concave by construction: over 400,000 pairs it reads **40% and
683 units**. Client and server draw the same line from the same geometry, so it never rubber-bands, and
the route the dungeon is actually walked (entrance → rooms → guard → boss) peaks at **102**. The cutting
happens when you tap straight from one side room into the one opposite.

⚠ **One real bug fell out of that and is fixed here:** the dungeon WARD — the anti-cheat net that
teleports anyone found too far outside a dungeon back to its door — was set at 500 units, which a
legitimate 683-unit corner cut clears. It would have started yanking players to the entrance on roughly
**one long cross-room walk in 125**. Its tolerance is `DungeonLayout.MaxCornerCut` now (1050, a generous
bound on room depth plus corridor width). An anti-cheat net that fires on ordinary movement is worse than
no net, and it is the hardest kind of bug to diagnose from a report.

⚠ **The map fill also had to be fixed, in the CLIENT.** `BuildRegionFill` triangulated every region as a
TRIANGLE FAN from vertex 0, on the stated grounds that "the outlines are convex". They were not, and a
corridor with rooms emphatically is not — a fan across it fills in every gap between two rooms, so the
map would have drawn a solid blob with only the rim hinting at the real shape. It is ear clipping now:
correct for any simple polygon, run once per region at startup, verified against every region in the
world by area (14 triangles for 16 vertices, exact).

**Nothing about the dungeons themselves changed** — same three, same rosters, same bands, same elite
ranks and respawn timers, same managing cities. The gate DESCRIPTION is derived rather than authored now
("Lv 39-42 · 2 side rooms off the corridor, all aggressive · Lv 44 boss at the end"), so a roster edit
cannot leave the menu advertising a dungeon that no longer exists. ⚠ The bosses moved, and a boss's
persisted respawn timer is keyed on its coordinates (`SpawnZone.Id`), so all three reset once.

🔴 **PROTOCOL 25 → 26, AND THIS IS THE FIRST BUMP WHERE NOT ONE BYTE OF THE WIRE MOVED.** No DTO, no hub
method, no push name. It moves because a dungeon's wall is shared CODE, not a message: `WorldDomain`
lives in `Game.Shared` precisely so the client can stop you at the surface while the server keeps its
clamp as the backstop — *"two halves enforcing the same rule is only safe if they cannot disagree"*. An
old APK holds the OLD polygon, so inside a dungeon it would refuse to walk into rooms that now exist,
rubber-band along walls that no longer do, and draw the old outline. Nothing crashes, which is exactly
why the handshake has to catch it. **A new APK is required.**

**Also: `13a` closed.** The "take a break" banner is back to 3 hours (`GameConstants.BreakReminderSeconds`)
after five passes at 10 minutes — *"Working - Can return it to 3h"*.

**Noted, not built — `BL-90`, your own COMMENT column.** Four rows in `nuker 3rd.csv` ask for a lower
debuff success rate on a spell that also deals damage (Frost Spikes' slow, Frost Pierce's bleed, Witches
Curse's curse), plus a bare *"lower success rate"* on Arcane Void and Witches Scarecrow. The engine has
one landing roll, `StatCalculator.DebuffLandChance`, and it is a pure stat contest with **no per-skill
term at all** — so today that rider would land exactly as often as a dedicated Slow. Your *"interrupt
unaffected"* is already free (the interrupt is a separate contest and never touches this roll). The fix
is one `SkillDef` field multiplied into two call sites; what is missing is the NUMBER. It belongs with
the nuker 3rd kit, which your CSV has just unblocked.

## 2026-08-24 — 0.81.2: a spell fizzles on the level it was LEARNED at, not the level you are

*"A dmg spell should fail if it's to low lvl … same as debuffs … we made dbufs to fail (hit floors) on
lvl difference, why we haven't done the same for dmg spells … if I hit you with lvl 1 vamp that I have
learned at lvl 14 it should stop be effective at some point."* And the mechanism, in his own words:
**"the fizzle effect is based of spell.learned-lvl not caster.lvl vs enemy.lvl … if I learn 35 lvl spell
at lvl 50 it should use the 35."**

The magic-fail roll took the CASTER'S level, so a level-80 mage's first bolt fizzled exactly as little
as his last one — the ladder was worth power and nothing else, and an obsolete rung stayed a perfectly
accurate spell forever. Contested CC stopped working that way on 2026-08-19, when he ruled the attacker
level is the RUNG'S learn level; that ruling now reaches the other two arms of `ExecuteSkill` that roll
`MagicFailChance` — **magic damage, and the uncontested debuffs**. `GameLoopService.RungLevel` is the
one helper all three landing rolls read (the third is `BL-71`'s buff threat, which prices a buff on the
level its class learns it at for the same reason).

**The formula did not move a single point** — `StatCalculator.MagicFailChance` is untouched, and its
table in `docs/balance/BalanceMatrix.md` is valid exactly as printed. What changed is which level you
read it at: 1% at parity with the rung, 5% at +6, 18% at +11, 67% at +16, pinned to the 95% ceiling from
+18 — all now measured from the rung. Casting DOWN is still free (0% from −3), so an old rung is never
*worse* than useless, it simply stops keeping up. A skill no class list owns — a mob spell, a scroll,
the practice dummy — has no rung to read and still falls back to the caster's own level.

⚠ **A fizzle is still not a miss**: it lands `damage / 3` and still rolls the interrupt. So a spell
pinned at the 95% ceiling is doing about **37%** of its damage, not zero. His *"I should not be able to
hit (atleast on floor)"* would be a second, separate ruling — the fizzle payload has never been zero and
was deliberately not changed here.

🔑 **A HOLE THAT WOULD HAVE DEFEATED HIS OWN EXAMPLE.** `ClassSkills.Cumulative` returns the CURRENT
tier only — the 2nd-class list plus the 3rd-class one — so every skill bought on the base path before
level 20 is invisible to it the moment you have an archetype, and the rung lookup returned 0 for exactly
those skills, falling back to the caster's level. Vampiric Bolt @14 is a BASE MAGE line: his *"lvl 1
vamp that I have learned at lvl 14"* would have gone on fizzling as an 80. `RungLevel` now asks the base
tier second, when the current tier does not own the id — and `BalanceMatrix` walks the same two lists in
the same order, so the table cannot hide it again. ⚠ It applies to all three rolls sharing the helper,
so a base-class buff's threat is now priced at its own learn level too.

🔴 **What actually stops working, measured per CLASS** (the merged-by-skill first cut was worthless —
Vampiric Bolt has 14 rungs to @80 on the nuker ladder and exactly ONE, at 14, on the Warchanter's):

| spell                                    | rungs | top @ | 95% by | classes                   |
| ---------------------------------------- | ----: | ----: | -----: | ------------------------- |
| Magic Bolt                               |     2 |    14 | **32** | all four mage disciplines |
| Vampiric Bolt                            |     1 |    14 | **32** | Lightbringer, Warchanter  |
| Holy Bolt                                |     4 |    35 | **53** | Lightbringer, Warchanter  |
| Flamebolt (Annihilate / Chain Lightning) |     1 |    40 | **58** | Magus, Tempest            |
| Glacial Spike                            |     1 |    44 | **62** | Magus, Tempest            |

The first two rows are the ruling doing exactly what he asked — his vamp-bolt case is the second one.
The last two are the ones to look at: single-rung **40+ placeholders**, the same shape as the five
single-rung CC skills, where he chose *"literal now, ladders later"*. **The fix is a CSV ladder, not
code**, and it arrives with the nuker CSVs. Everything with a real ladder is untouched: the nuker bolts
run to @80 (14% fail on a level-90 target), Elemental Burst to @75, the Lightbringer's Holy Ray /
Gravity / Mana Ray / Armor Break to @74.

`tools/BalanceMatrix` grew a **SPELL LADDERS** table next to the CC one — every fizzling spell, its
rungs, and the target level at which its best rung reaches 5% / 50% / 95% — because "does this class
have a rung near the cap" is now a balance question rather than a curiosity. `--fizzle`'s first argument
is a SPELL'S learn level now, not a caster level; its output is unchanged.

**Also in this build — the progression triad `/lvl`, `/sp`, `/exp`** (owner). Staff-only, and written to
read exactly like `/givegold`: `[name] <value>`, where the **name is optional** (omit it and the subject
is you), the value takes `1_000_000`, `100k/m/b/t` and a negative sign, and `max` is a word rather than a
number to remember. `@t` / `@self` already substitute into a name on the client, so `/sp @t max` works
with nothing added there — and because the client passes any unknown `/verb` straight through for staff,
**no new APK is needed**.

- `/lvl [name] <level|max>` **SETS** the level outright — not a delta like the debug +1/+10 buttons —
  clamped to the subject's own ceiling (`LevelCapFor`: 90 for a player, the deliberate staff exemption
  for an admin), itself held to the end of the *authored* exp curve so a typo cannot park someone on an
  extrapolated level. Delevelling keeps the learned skills, his standing rule; only the auto-granted,
  level-derived passives re-sync (`AutoLearnCoreSkills`).
- `/sp [name] <amount|max>` **ADDS** skill points; `max` is `int.MaxValue`, the saturation ceiling SP
  already has in `AwardExp` rather than a new rule. A negative amount takes SP away, floored at zero.
- `/exp [name] <amount|max>` **ADDS RAW exp — deliberately NOT through `AwardExp`**, which would scale it
  by the server's ×10 exp rate and by the subject's runes, so "give exactly this many" would quietly be a
  lie. It runs the same level-up loop and the same at-the-ceiling parking rule. `max` leaves them one
  point short of the next level: *"a single drop of 1 exp can lvl the target to the next"*.

⚠ `/sp` and `/spd` are distinct verbs — the dispatcher splits on the first space, so the speed-override
rig is untouched.

Server-only: no DTO, no hub method, no push name, so **protocol stays 25** and the 0.81.1 APK talks to
this server unchanged.

**And three CSV edits of his, walked back into the code** — the other half of "the CSVs and the game
move together". `--check` was red on six rows before this and is green after:

- **`nuker 2nd.csv` — mpWhenRestored is 10 / 15 / 20 / 25%**, down from 19/23/26/30. Those were never
  his numbers: they were the 2026-08-19 flat→percent conversion (old flat × 0.75) that *we* wrote into
  his file. He has replaced it with a round 5-point ladder, and a much smaller one, in exactly the band
  where Restore Spirit's own number is smallest — an early nuker's mana now comes from the SKILL, not
  from the robe. ⚠ Rungs 5-8 (@40-70, ours, no CSV yet) keep their 38-60%: that endpoint is his separate
  *"+200 MP for −200 HP at 80"* ruling. The hand-off at 40 is 25→38 now instead of 30→38 — monotonic,
  just steeper, and the 40+ nuker CSV is what resolves it.
- **`nuker 2nd.csv` — Restore Spirit rung 1 is −66 HP for +22 MP**, was −65/+20. `--check` does not read
  those two numbers out of DESCR, so this one was found by reading the diff, not by the tool.
- **`healer 3rd.csv` — "Healer Weapon Mastery" is now "Spellcaster Weapon Mastery"**, on all fourteen
  rungs. The skill was never healer-flavoured — it is what a 3rd-class caster's blunt does, and the
  nuker file will want the same row. 🔑 **The id stays `healer_weapon_mastery`**: skill ids are
  append-only, and renaming one strands the learned rows of every saved character. Display names move
  freely, ids do not. ⚠ This is the one line here with a client tell — the Learn tab builds its labels
  from the compiled `SkillCatalog`, so the phone keeps saying "Healer Weapon Mastery" until the next APK.
- **`nuker 3rd.csv`** — his in-progress authoring, committed as-authored. It is a placeholder file, not
  in `Check.Specs`, and nothing in the code reads it yet; the nuker 3rd kit is still unbuilt.

**Also: the "take a break" banner is back to 3 hours** (`GameConstants.BreakReminderSeconds`). It ran at
10 minutes from playtest 24 to 28 at his own request, tagged in the source to be put back, and checklist
row `13a` finally came back read — *"Working - Can return it to 3h"*. Five passes to get one banner seen.

## 2026-08-23 — 0.81.1: playtest 28, twelve finds — an exploit that was a level window, a buff cap that never reached a rune, and a blunt skill that refused a maul

Twelve free-form finds in one pass, eleven built. Three of them are the same shape and worth naming
first, because in each case the rule we thought was in the game **was written down and never reached
the thing it was about**.

**The mana-restore exploit was not a hole, it was a level window.** His words: *"a healer/buffer that
haven't learned mana restore can be restored — should check the actual kit (future/present/etc); a 20lvl
cleric should not be able to be restored even when he should learn it at lvl 30."* The rule that stops
two restorers printing mana off each other tested `HasSkill(restore_mana)` — a LEARNED check. So a cleric
was a legal restore target from level 1 to 29 and stopped being one at 30, which is not an exception, it
is a thirty-level window with the door shut at the end of it. The loop is a property of the CLASS, so
`ClassSkills.CanClassLearn` answers it now, at every level, at both call sites (the manual cast and the
autopilot's target pick).

**Runes counted against the buff cap because the exemption named the wrong row.** `CountsAgainstBuffCap`
exempts `BuffRow.Item` and its own comment claimed the War/Spell Rune among them — while every rune buff
in the game, those two included, is authored `BuffRow.Consumable`. The written exemption reached nothing.
They carry `CountsTowardBuffLimit: false` now, which is where the answer belongs: it is a property of the
buff, not of which row it draws in. Evicting one was pointless as well as unfair — the rune
reconciliation re-applies it on the next tick, so the cap spent a slot, dropped a real blessing to free
it, and ended the second with the rune still on the bar.

**`Blunt` and `TwoHandedBlunt` are two different bits.** *"Cannot use acoustic shock and sound smash with
maul (2h blunt), only work with 1h .. Should work with the 4 weapons (maul, mace, wand, staff — all
blunts), same goes for all other."* The gate was `(required & equipped) != 0`, so a skill authored
"blunt" silently meant "one-handed blunt" and the Warchanter's own maul locked him out of his own damage
skills. One helper now — `WeaponTypes.Satisfies` — at all four places that ask: the cast gate, the
auto-farm's skill pick, the on-hit proc check and the weapon-mastery passive. ⚠ **The fold is
conditional**, and that is the whole subtlety: folding a two-handed weapon to its base type
unconditionally would also let a maul pass a genuinely two-hands-only requirement, because
`TwoHandedBlunt.Base()` is `Blunt`. So a requirement that NAMES a two-handed bit is matched exactly; one
that names only base types has hands folded out of the question. Both authored shapes keep working with
no skill row edited. (The proc check had been folding unconditionally — the same bug pointing the other
way.)

**Flat buffs apply after percentages.** *"Sharpening and reinforcement toggles should apply after
everything as a flat bonus not before buffs. Armor x buffs + reinforcement."* It was
`(base + Σflat) × (1 + Σ%)`, which put every flat bonus inside the percentage stack — Reinforcement's
+600 P.Def was worth 600 to an unbuffed character and ~900 to a fully-buffed one, so the toggle you flip
to survive a bad pull was worth least exactly when you needed it. It is `base × (1 + Σ%) + Σflat` now.
⚠ It applies to **every** flat magnitude, not just the two stances, deliberately: two orders of
composition side by side is how a formula stops being predictable. `BalanceMatrix` is byte-for-byte
identical before and after — every buff set it models is pure percentage — so the change is arithmetic
and small: about −6% total P.Def while Reinforcement is up at a 30% buff stack.

**The NPC buffer: nineteen singles down to his eleven, and the window moved to 6-90 free-to-75.** He
listed the survivors by hand — Body, Vigor, Resolve, Alacrity, Might, Bulwark, Vampirism, Ward, Force,
Fury, Frenzy — and his own parenthesis is the shape: one buff per axis a levelling character actually
feels. The eight that went are all one thing, **the optimiser's row**: the whole accuracy/crit block, the
MP pair (the HP pair stayed, because dying is what a new character does), move speed and evasion. This is
the other half of `BL-87`: nineteen singles against a cap of twenty left ONE free slot, so taking the
full NPC set and grouping with a real buffer were mutually exclusive. Eleven leaves nine.

The price window is a deliberate reversal of what the price was for. It charged from 40 — exactly when a
real buffer class becomes available — so the NPC competed with a *player* on price. Now it is free for
the whole levelling game and charges only 76-90, where gold is plentiful and the buffer you want is a
person. What squeezes the NPC below 75 is the cap and the set's ceiling, not a bill.

**Buff potions: three families drop, and the sixth Uncommon rung moves to the player economy.** Swift,
Alacrity, Fury and Dash stay in the loot tables; Agility, Might, Bulwark, Force, Ward and Aim leave them.
The rung weights are untouched, exactly as when the scrolls came out: ten ids became four, so a buff
potion drops as often as before and is 2.5× more likely to be one of the three you can only get that way.

⚠ **His "apothecary masters" meant the CRAFTER, not the shop NPC** — corrected the same day: *"the shop
can supply common only and the crafter can supply the rest … and if they are not tradeble we should make
them so a crafter can sell them to others."* So the Apothecary's shelf keeps the **Common** rung of all
nine families and nothing above it, and **nothing had to be built for the crafter**: the Potion Master's
ladder is his own (*"l2 - common buff pots … l4 - uncommon buff pots"*) and has carried all nine Common
recipes at craft L2 and all nine Uncommon at L4 since the crafting build. The six that left the loot
tables therefore land where he wanted them — **a player Potion Master at L4 is now the only source of an
Uncommon Agility/Might/Bulwark/Force/Ward/Aim potion in the game.** That is the real content of the
change: a whole rung of a consumable moves from a vendor to the player economy.

**Potions were already tradable** — every potion in the catalog, by his own playtest-18 `V2b` ruling:
*"buff pots are 0 sell (ppl still can sell them to others if they want)"*. `SellPriceOverride: 0` stops a
**vendor** buying one back; `Tradable` was never touched, so a **player** always could. The one bound
consumable is the buff **SCROLL** (playtest-17 `E3`), box-only because the Blessing Box was the tradable
thing.

**A buff's details say which bottle it came from.** *"I'm getting buffed 'Mig' and don't know if it's my
or the potions."* And he could not tell: a potion's wrapper owns the duration and the bar row, but the
buff that LANDS is the family rung — literally the same buff a buffer casts, same key, same rank. That
identity is the design (it is what makes the two compete instead of stack), so the only place the
difference can live is the text. A `From: Might Potion (Lesser).` line now leads the details popup. It is
in the description rather than the name on purpose: the square's abbreviation is built from the name, and
"Might Potion (Lesser)" does not abbreviate to `Mig`.

**Chat is filed per character instead of wiped.** *"Chat again is saved between logins. Don't reset"* —
which reads like a reversal of playtest-17's `C1` (*"chat must reset on exit"*) and is not. `C1` was
reported because a freshly created character opened onto a **deleted** character's conversation. Both
rulings say the chat belongs to the CHARACTER: the first complaint was it leaking across characters, this
one is it being thrown away within one. So leaving the world stores the chat under whoever was talking
and entering restores that character's own. It goes to **disk**, and flushes on `OnApplicationPause` too
— "between logins" on a phone mostly means the OS killed us, and an in-memory stash would have failed the
likeliest case. The System tab is still never stored: it is the crash trail, it is not per-character, and
it is the one thing you want fresh for the relog you are doing right now.

**A chat log, for moderation.** His question — *"how do the big games work it out? with tickets with a
screenshot, or they have their chat log?"* — has an answer: they have the log. A screenshot is evidence
the reporter supplies and the accused can dispute; the log is what the moderator reads, and it is the
only thing that answers "what else has this account been saying". Tickets are how a case opens; the log
is how it is decided. `ChatLogRecord` carries his four columns plus the channel — time, sender id AND
name (a name can be freed by a delete and re-taken, and a six-month-old row that only says "Aldric" is
evidence against the wrong person), channel and receiver in two columns rather than one overloaded field
(that is what makes "every whisper this account sent" a query), and the text. It logs what was
DELIVERED: anything refused above the send — a chat ban, a jail, the world-chat level floor, a whisper to
someone who blocked you — was not said to anybody. A `/block` on Local or World only filters who hears
it, so that IS logged. Written off the tick: the loop buffers a minute and the autosave flushes one
batch. 🔴 **No reader command yet** — today it is a table you open with a SQLite browser.

**Client: you can target yourself, and a live toggle looks live.** Tapping your own vitals panel now
targets you; the character sheet moved to a `[Char]` button in the bag beside `[Equip]`. He was literally
right that self-targeting was impossible: a world tap on your own body is refused on purpose (so your own
collider cannot steal a tap meant for the ground under your feet) and the party window only exists in a
party, so a solo healer had no way to select himself at all. A toggle that is on now draws a muted aqua
ring **outside** the green auto ring, so a slot can carry both marks — no protocol change and no server
work, because a live toggle was already a buff on the bar with no timer and nothing was reading it from
the slot's side.

**The sound skills retire Holy Bolt**, the way Holy Ray already does for the healer. All three carry the
clause — an ork can buy Smash and Shock at 40 in either order and the retirement must not depend on the
shopping order. ⚠ The trade is real: Holy Bolt is a spell with no weapon requirement and all three of
these are weapon-gated, so a Warchanter with the wrong weapon in hand now has no attack skill rather than
a weak one.

**Answered, not built:** *"shouldn't lvl 14 vamp bolt fail all the time fighting 37/39 mobs?"* No — the
fizzle roll reads the CASTER's level, never the skill's, and casting DOWN is 0%. The interesting half is
the 300 damage: with `K·(mAtk·lvlMod + power)/def` and a rung-1 power of 21, the rung is 3-5% of that hit
and the gear is the rest. That is why a level-14 skill still works at 40 — and a Warchanter never gets
another rung of it, because rungs 2-5 are on the nuker ladder. The honest fix for that is the power
ladder, not a landing penalty.

⚠ **Schema change** (the chat log table) — delete `Game.Server/game.db` or let the stale-schema check
recreate it. Protocol stays **25**; nothing on the wire changed shape.

## 2026-08-23 — 0.80.0: you can see that you are in god mode, and stealth looks like stealth (`BL-82`)

His question was *"What happen to the /god /invis + prowl/conceal/hymn visual?"* — and the honest answer
was that the Unity client never had one. The god-mode badge lived in the **WPF harness** and died with it
in 0.42.8 (`00f17fb`). The server has been pushing the state ever since — `SendAdminState` on every `/god`
and `/spd` — into a message name **no Unity handler was ever registered for**, so it went into the void;
`/invis` did not even push, and `AdminStateDto` had no field for it. The rest (his opacity rule) had been
parked as "later, once models exist", which was never a real gate: a capsule takes an alpha exactly as
well as a model will.

**The badge.** Top strip, left of the version. Rank, plus `GOD`, `INVIS` and any forced speed, on a
background that carries the alarm on its own — red for god mode, indigo for invisible, neutral for staff
with nothing on, hidden entirely for an ordinary player. It is painted from the SERVER's view of the
character, never from local memory of what was typed: `/role` dropping someone below admin also clears
their god mode, and a client tracking its own toggles would go on showing GOD to someone who no longer
has it (the old PvP-button bug, which guessed and was wrong every time the server refused a toggle).

**The opacity**, exactly as he ruled it in playtest 25 — *"the players in shtealt will see themselves
with opacity to 0.7 and in invis 0.4 (for them selves only - for others stealth does nothing, invis
vanishes them)"*. Stealth (Prowl / Conceal / Shrouding Hymn) fades your own marker to **0.7**; both
invisibilities — `/invis` and the rogue's Vanish — to **0.4**; a god admin gets a **golden ring**, a
translucent shell around the marker rather than a repaint of it, so the dot keeps its own colour. The
marker's material is swapped for a transparent one **on the first fade only**: every marker in this
project is deliberately opaque (transparency in URP costs a second material, a render queue and
back-to-front sorting), so a character who is not hiding renders exactly as before.

🔑 **"For themselves only" is enforced by there being nothing to leak.** The push describes ONE
connection's own character and says nothing about anybody else's, and the observer half was already true
server-side: `BL-69` makes a hidden entity an **omission from the snapshot** (`CanSee`), never a flag the
client is trusted to honour. Stealth, correctly, changes nothing for an observer — the stealthed player
is still sent, still drawn, still clickable; it is the unaggroed MOBS that decline to start on him.

**Pushed on change, from the tick loop** — not from each command that could cause one. That is the whole
design of `PushSelfState`: hide starts on a cast and ends on expiry, on damage taken, on any action, on a
Signal Flare, on death; stealth ends on toggle-off, dispel, expiry, an unaffordable upkeep tick and death.
Enumerating those call sites is how one gets missed, and a missed one leaves a character faded while he
stands in plain sight — a bug that looks exactly like the feature working until someone walks up and kills
him. Comparing the finished record (records compare by value) costs one allocation per player per tick and
cannot be missed. The dead-check `continue` sits below it deliberately, so a corpse is never left faded.

⚠ **`ProtocolVersion` 24 → 25, and it is a RENAME, not an addition.** `AdminStateDto` is now
`SelfStateDto` — three fields richer (`Invisible`, `Hidden`, `Stealthed`) and pushed as `"SelfState"`
instead of `"AdminState"`. An old client subscribes to a message the server no longer sends and shows
none of this, which is the very hole this version closes. **Install both halves.**

Verified: `dotnet build Game.sln` clean, the Unity type-check clean against the refreshed
`Assets/Plugins/Game.Shared.dll`, and the server boots reporting `L2Clone server v0.80.0 starting.`

## 2026-08-23 — 0.79.0 shipped: the playtest-27 build, and a wire bump that is not optional

Playtest 27 landed across two commits (`8619a6b`, `a00385f`) and **bumped nothing** — the same slip as
the Warchanter kit before it. Bumped to **0.79.0** and republished both halves:
`builds/L2Clone-0.79.0.apk` (41.1 MB, log line `[build] version 0.79.0 (code 7900)`) and
`builds/Game.Server-0.79.0.zip` (14.9 MB).

⚠ **`ProtocolVersion` is 24 here, and unlike 0.76.0 the two halves must move together.** The buff-level
field in the effects popup is a pure addition an old client would simply not read — but the same build
carries three CLIENT-side rules a server cannot enforce alone: `_ . -` are legal in character names,
`~` and `%target` stopped being target tokens (`~` is now the relative-coordinate prefix for `/tp`)
while ``/`` started being one, and a non-admin may send a bare `/where`. An old APK against this
server means the wrong name rule and a dead ``. **Install both halves.**

Most of this build is client-side anyway — the stat swaps leaving the Learn tab, the buff-level label,
the blue mana HoT and the cast-chaining feel all live in the APK, not on the wire.

**No schema change of its own.** The `game.db` delete owed from 0.71.0 and re-owed by 0.78.0's
`AccountRole` renumber is **still owed on the phone**, for the same two reasons; the zip ships without
a database on purpose, so his characters are never overwritten by this workstation's.

Verified in the order that matters: `dotnet build Game.sln` (0 errors) refreshed
`Assets/Plugins/Game.Shared.dll` **before** Unity ran — the DLL carries `0.79.0` and its timestamp
(07:36) is older than the APK's (07:42), the check that catches an APK stamped with the previous
version — the client type-check was clean, and the published server boots reporting
`L2Clone server v0.79.0 starting.`

## 2026-08-23 — playtest 27 in full: town regen, the buff cap, cast chaining, and every checklist comment

A **fast pass** — five finds plus nine comments written into the checklist rows, all of them answered
here. *"Made a fast/simple playtest ...look at the file"*. ⚠ **The comments were missed on the first
sweep** and three rows were built in isolation; everything else landed in the same day.

⚠ **Mana Vampirism 3/7/10% → 1/1.5/2%** (`buffer 3rd.csv` moved with it): *"Should lower the buffers
mana vamp - to op - same levels just 1,1.5,2% or 10% on 10/15/20% chance"*. 🔑 His two options are the
SAME expected value — 10% on a 10/15/20% chance averages 1/1.5/2% — so it was a feel question, and the
flat one won: a sustain line is the wrong place for variance.

⚠ **`ProtocolVersion` 23 → 24 and most of this is client-side — the 0.79.0 APK is owed.** No DB change
of its own; the `game.db` delete owed from 0.71.0 / 0.78.0 is unaffected, still owed, still for the same
two reasons.

---

### 1. A sitting healer in town regenerated 220 MP/s

*"Hp/mp regen in cities should be decreased to x2 and only in the big cities ..not in a starting point
of elit dungeon ...I can sit with the healer with 220mp/s regen and heal like crazy"*.

Two separate things were wrong, and he named both.

**The multiplier is 5 → 2** (`GameConstants.SafeZoneRegenMultiplier`). The number that actually hurt was
never 5 on its own — it was the **stack**: town ×5 × sitting ×1.8 = **×9**, applied on top of every regen
buff *and* on top of Meditation's flat +MP/s, which sits inside the same multiplier on purpose ("sitting
to meditate should pay"). At ×2 the sitting healer runs **×3.6** — still obviously the place to rest, no
longer a second mana bar.

**And it is a CITY bonus now, not a safe-zone bonus.** `SafeZone` gained a `RegenBoost` flag, defaulting
true and set **false** on the four safe zones that are not cities: the **Training Outpost** and the three
dungeon entrances — **Hollow Crypt**, **Sunless Warrens**, **Ashen Sepulchre**. They keep everything else
a safe zone does — no mobs, no aggro, no PvP — they simply are not rest stops, so an elite dungeon can no
longer be farmed from a chair one step outside its door.

🔑 **Read it through `GameConstants.SafeZoneRegen(x, y)`, never by testing `InSafeZone` yourself.** Being
safe and being able to rest are two different questions from today, and both regen call sites
(`Regenerate` and the stats-window `StandingRegen`, which must never drift from it) go through the one
helper.

⚠ **The Training Outpost is my call, not his words.** He said "big cities"; the outpost is a 400-radius
hut beside the dummies. Flagged on the checklist row as trivially reversible.

### 2. The stat swaps were listed twice, and one of the two places couldn't price them properly

*"The stat swap,passive should not be in the 'to learn' tab. They have their own. Only show in the
passives already learned."*

The Learn tab now filters them out (`SkillCatalog.StatSwapOf(cs.SkillId) is null`). They were always
buyable in two places, which is the "a bit chaotic" complaint from an earlier pass showing up again: the
**Stats** tab is built for them — a basket you stage for free, a running "Added:" line, one total — and
the Learn tab could only ever show twelve pair-shaped rows priced one at a time. Bought rungs still read
back on **Known**, greyed, as the passives they are.

The per-rung gold computation that had to be duplicated in the Learn tab (swaps are priced by **rungs
owned**, not per level) went with the rows, so exactly one place in the client prices a swap now.

⚠ **CLIENT-SIDE — needs a new APK.**

### 3. The buff cap is 20, and what counts is now a per-buff flag

*"we need make max buffs limit. Now I have 24 buffs as healer … So if we make it 20 then the buffer
becomes a must."* Three rulings followed, and all three are built.

🔑 **He had been living inside this feature without knowing it.** A `MaxBuffSlots` cap with FIFO eviction
has existed since the buff-ladder work — **at 24**. That is why he counted exactly 24: he was *at the
cap*, and buffs had been quietly falling off the back of his bar all session. **24 → 20.**

**What counts is authored, per buff.** New `SkillDef.CountsTowardBuffLimit`, **default true** — his rule
verbatim: *"the flag is not self or not, the flag is per buff .. default is true (counts towards max) -
toggle don't and heals etc"*, and *"a self buff that is 20min still counts … For example the bow
expertise is a buff that counts toward the limit"*. So it is deliberately **not** derived from
`TargetMode`, and **not** derived from duration either — that every `false` in the catalog also happens
to be ≤90 seconds is a consequence of what short buffs *are*, not the rule. A future 30-second blessing
that should cost a slot only has to say nothing.

⚠ **The flag is read off the buff that LANDS, never the wrapper.** A one-child wrapper resolves to its
child in `ApplyBuff`, so Dash is flagged on `buff_dash_*` and a **Might potion is not flagged at all** —
it *is* a single of the might family, out of a bottle, and it pays its slot like one.

Authored `false`: the six Combo Rush rungs · War Cry / Greater War Cry · Battle Fury · Fortify ·
Shrouding Hymn · the three racial Renew verses · Harmony of Restoration (the party HoT) · Aegis · Battle
Presence / Battle Defence · Conceal · Defensive Wall · Evasion Boost · Indomitable · Last Stand · Mana
Barrier · Meditation · the eight Dash/Sprint rungs · the three healing potions. Toggles, debuffs and the
gear/rune row were already excluded by the engine, for reasons that are not authoring questions, and
still are.

**FIFO stands** — *"1st buff buffed gets removed…if the 1st buff still have 2h time remaining I still can
overbuff and remove it"*. That was already the behaviour; only the count and the cap moved. **And the cap
is on counted buffs only**, so his *"20+14"* is right: uncounted buffs stack on top without limit.

**The measured result: a fully-buffed character sits at 16 / 20, four free.** Buffing yourself off the
NPC buffer instead costs **19 of 20**, for a strictly weaker set, because a group packs three or four
families into one slot and a single never can. **The cap does not squeeze the buffer; it squeezes the
alternative to him.**

### The census that proves it: `BalanceMatrix --buffs`

New mode — `dotnet run --project tools/BalanceMatrix -- --buffs`. It replays what `/fullbuff` hands out
through the **real** `GameLoopService.BuffPlan` and the real family-conflict rules, prints what survives
with a `SLOT` / `-` column computed by the engine's own rule (resolving wrappers the way `ApplyBuff`
does), then censuses the whole catalog by kind. `BuffPlan` was made `public` for it: one access modifier,
so the census can never drift from the resolver every buff already goes through.

It earned its keep immediately — it caught a bulk edit that had wrongly exempted the three **Swift**
rungs, which are real singles and must cost a slot.

What it says today: **210 timed buffs** — 5 harmonies (Protection, Warrior, Wizard, Speed, plus
Restoration, which is the party HoT and does not count) · 20 group skills over 9 Warchanter lanes, 6
Lightbringer `holy_*` and 5 NPC-buffer versions of the same lanes · 49 class singles over **36 families**
· 19 NPC-buffer singles · 18 self buffs over 14 families · 3 toggles · 96 potion/scroll/rune rows over 28
families.

⚠ No CSV column changed — `SkillCsvSeed --check` is green on all ten files.

---

### 4. Cast chaining, and cancelling that only cancels what you meant

*"Cancel casting should be done only from clicking the same skill on the bar (it's X) or the cast bar ..
Now I click one skill and clicking the second cancels the first and start the seconds cast ...I have no
way of chaining skills … Now if I do it fast I can skip buffs"*.

Starting a skill ran `CancelCast(caster)` and began the new one, so tapping down a buff bar cast the LAST
one and silently threw away everything before it. Now, while a cast is in flight **or** a queued skill is
walking into range:

- the **same** skill → cancels it, exactly as the cast bar's X does, cooldown and all;
- **any other** skill → becomes the chained one, replacing whatever was chained before.

**One chain slot**, per his *"(only 2)"*: what is running, and what runs next.

🔑 **The chained cast is re-gated when it FIRES, not when you click it.** `HandleSkill` was split into a
re-enterable `BeginSkill`, and the chain re-enters it: MP, cooldown, range and target validity are all
re-checked at that moment. Nothing is reserved, nothing is pre-paid, and a chain that has become
impossible fails exactly as if the button had been pressed then. `TryStartChainedSkill` clears the slot
*before* re-entering, so a failure cannot retry forever one tick at a time.

**A cancel ends the whole plan**, chain included — an enemy interrupt too. An armed chain surviving a
cancel would hand the player a surprise cast at some unrelated later moment, which is the class of "why
did I just cast that" this was built to remove. **Toggles are never chained** (instant, no cast time), and
the cast bar's X now also drops a queued skill that has not started casting — there was no way to call
one of those off before.

### 5. The rest of his §90/§91 comments

Five follow-ups he had written into the checklist rows, all answered in the same increment.

**`_ . -` are legal in names** (`91a`) — *"I see no reason why cannot be included. Players should be able
to separate `Name_.-Family`"*. He is right: none of the three is a token separator, none needs a keyboard
layout nobody has, and none can be confused with *nothing at all*, which is what the rule was built for.
Consecutive ones are legal on purpose — his own example has three in a row. Everything else stands: no
spaces, no Cyrillic, must start with a letter.

**`owner.txt` writes itself when missing** (`91d`) — ⚠ **`ServerControl.EnsureOwnerFileForDev`, and the
name says the whole story.** `SeedOwnerFile` only fires on a fresh DATABASE; his loop is the other one —
he deletes the deployed FOLDER on every build, which takes `owner.txt` with it while the database he keeps
survives, leaving an install with no Owner and no way to appoint one (the rank is deliberately unreachable
from any command). The generated file contains `Owner` plus two comments telling you to put your own name
there. **On a public server this is exactly wrong** — a file that writes itself is a file an attacker can
predict — so the method, its comment block and its call site all say DELETE BEFORE PUBLIC.

**`/buff` finally takes a target** (`91f`) — *"the name was a player's name … Now target don't work cannot
buff no1 else except me"*. It applied to the caster and nothing else. The client substitutes `@t`/`@target`
and `@s`/`@self` into a NAME before sending, so the server only decides whether the first word is a
person: **names someone online → that is the target; otherwise the whole argument is the buff and the
target is you.** `/buff @t`, `/buff Ivan`, `/buff @s aim 1` and the old `/buff aim 1` all read as written.

And the token rules he fixed with it: **only `@t`/`@target` and `@s`/`@self`** — `%target` and bare `~` are
gone as target tokens — because **`~` is the relative-coordinate prefix now**. `/tp` gained coordinates:
`/tp 100 123` exact, `/tp ~100 ~-50` relative, a bare `~` meaning "unchanged" so `/tp ~ 5000` walks north
on your own x. One character cannot mean both "my target" and "offset from here" on one command line.

**`/where` split in two** — bare `/where` works for **anyone** and reports your own coordinates and town
(*"to tell friends where to find them"*); `/where <name>` stays staff. It is handled before the staff gate
in `HandleAdmin`, and the client — which used to refuse every `/` command from a non-admin before it left
the phone — now lets that one through.

**The buff level reaches the effects popup** (`91g`) — *"I see it in 'known' as `Aim Lv.1` but once is in
the effects bar and click on it to open details. The title just says Aim no lvl"*. The server knew it all
along (`BuffInstance.Level`, kept so a buff can be rebuilt on login) and never sent it, so the one screen
you go to in order to ask which rung you are carrying could not answer. `BuffDto` gained a `Level` and the
title reads `Aim   Lv.1`. Sent as 0 for a buff with no ladder, so "Frenzy Lv.1" never appears.

**Harmony of Restoration's mana tick is blue** (`90l`) — *"the mana part is no different of the healing -
show same green 10 as +100 while the mana totem is a blue 20"*. One word: the tick was broadcast as
`CombatOutcome.Heal` with the skill named "Mana". The distinct `ManaHeal` outcome already existed — it is
what makes the totem blue — and this path was written before it and never moved.

**Quick Heal (`90h`) is ANSWERED, not built.** *"what will be good replacement for it … harmony of
protection? or any of the 3 passives?"* — **neither, and nothing needs to.** Both candidates would
re-create this row's own bug: `Replaces` between unrelated skills is what stripped Quick Heal in the first
place (a passive called "Harmony" replacing a heal). The ladder is already whole — Human Lightbringer's
Quick Great Heal and Elf's Healer Blessing both replace it; the Ork keeps it because his answer is a
Healing Totem, a different tool. The Warchanter keeps it too and should: his Renew verse is a party heal in
a 600 radius **centred on himself** and cannot reach a hurt ally across the field, so it does not supersede
a 600-range targeted heal. The double-SP worry answers itself on the `game.db` delete already owed.

### `ProtocolVersion` 23 → 24

`BuffDto` gained a field, which is a pure addition — but the same version carries three CLIENT-side rules a
server cannot enforce alone, and mismatched halves would look like bugs rather than a version skew: the new
name charset, the changed target tokens, and a non-admin being allowed to send `/where`.


---


## 2026-08-22 — 0.78.0: his six playtest-26 finds, five built

*"Read open checklist and my finds (other I haven't done) - when u are done u can reset them and move
them to the backlog or mark them with green circle emote as done"*.

Six free-form finds in `Open-Checklist.md`'s **My Finds** section, none of the `90*` rows played yet.
**Five are built here**; the sixth's outstanding half (a big red on-screen countdown) is `BL-86`, and a
defect found on the way past is `BL-85`.

### `ProtocolVersion` 22 → 23, and this one is a REDEFINITION

Almost every previous bump added something an old client could ignore. This one **renumbers an existing
enum**: `AccountRole` gained two ranks, and `ChatModerator` was inserted at 1, so `Moderator` moved 1 → 2
and `Admin` 2 → 3, with `Owner` at 4. It travels on `AuthResponse` and `AdminStateDto` as a plain int, so
an old client against this server would read a Moderator as a Chat Moderator, an Admin as a Moderator,
and would hide the admin toolbox from a real admin. **Nothing throws** — which is exactly why the
handshake has to catch it. `MinAcceptedProtocol` stays 8.

⚠ **It also means the DB.** `CharacterRecord.Role` is persisted as that int, so rows written before
today read one rank too low. Survivable only because the `game.db` delete is already owed from the
0.71.0 schema change; **do it in the same sitting.**

---

### 1. Two characters could be called nothing at all — the name rule

*"I can register two chars named - " " & " " - naming should be cace incentive so admin can write names
with all low. -should we disable the cirulyc ot it works everywhere . Same question for space in the name
or allowed symbol"*.

`Trim()` was the whole rule, and it does not catch what he hit: **U+200B ZERO WIDTH SPACE is not
whitespace to .NET**, so two names made of one and of two of them are both non-empty, both distinct, and
both render as nothing.

One validator now, `GameConstants.IsValidCharacterName`, in `Game.Shared` so the server's create path and
the client's create screen cannot disagree — the server re-runs it regardless, because the client is not
trusted with what a legal name is. **His three questions, answered as one narrow rule:**

|                                               |                                                                                                                                                                                                          |
| --------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **ASCII letters and digits only**             | so **no Cyrillic** — his own suggestion, and the reason is that every name-addressed command (`/whisper`, `/ptinv`, `/jail`, the friend list) needs a name **every other player's keyboard can produce** |
| **No spaces, no symbols**                     | every name-taking command parses the name as the first token or splits on the last space (`/role <name> <role>`), so a space breaks the parser, not just the eye                                         |
| **Must start with a letter**, 3-16 characters | an all-digit name collides with every command that takes a number in the same slot; the **3** is mine, not his — a 1-character name is the same problem one step along                                   |

⚠ **Case-insensitivity was already there** and was not the bug: `CreateCharacterAsync` has compared
lower-cased names since it was written, and every slash-command lookup is `OrdinalIgnoreCase`.

⚠ What this deliberately does **not** fix is `IlIlllIIllI`. No charset rule solves visually confusable
names — that is what the next item is for.

### 2. `@target` — every name command now takes the target instead

*"we should make @target or %target or ~ so admin/players commands to work on the target (take the name
from the target window) because a player named "IlIlllIIllI" for a human is impossible to read."*

Substituted **once, on the client, before any command is parsed** — so it works on every command that
takes a name, including ones written later, with **no server change at all**: `/jail @target`,
`/w ~ hello`, `/ptinv @target`, `/give @target sword1h_t10`. Targeting is client-side in this game (the
server is only ever told a target id when you act on something), so the client is the only place that
knows the answer.

All four spellings work — `@target`, `%target`, `@t` and a bare `~` — and only as **whole tokens**, so a
message containing one of those characters is untouched. With nothing targeted the command is **not sent
at all** rather than reaching the server as the literal word "@target".

### 3. `/server shutdown|reboot|on` — the countdown, on his ladder

*"Can we do something like 'The server is Shutting down' - countdown"*, and then a full specification.

Built as written, in `Game.Server/Simulation/ServerControl.cs`:

```
/server shutdown|stop  [minutes] [adminOnly]
/server reboot|restart [minutes] [adminOnly]
/server on|online      [minutes]
```

- **minutes** — `-`, blank or `0` is instant; anything **unparseable is 30 minutes**. His rule, and it is
  the safe way round: a typo delays the server rather than killing it under a full population.
- **adminOnly** — writes a flag file beside the exe, and the server that comes back up **admits staff
  only** until an admin types `/server on`. Checked per CHARACTER at `EnterWorld`, so an admin's ordinary
  character is refused too — the role is per character by design.
- **on** — cancels any procedure **instantly**, and lifts the lock now (no time) or after the minutes
  given. Each command replaces the one before it, which is why there is no separate cancel verb.
- **reboot** relaunches a detached copy of our own executable and exits **66**, so it needs no supervisor
  (he runs the server under termux-ubuntu on a phone) but a supervisor can take the job over later.

**The announcement ladder is his, verbatim** — whole hours above an hour, then every 10 minutes, then
every minute under 10, then **every second for the last 60**. `/server shutdown 117` says:

```
1:57h (opening), 1:00h, 50 min, 40 min, 30 min, 20 min, 10 min, 9…1 min, 59…1 sec
```

🔑 The countdown is a **tick job, not a timer**, because when it fires it runs `AutoSaveAll()` first —
the same single-writer thread that owns every entity, so the snapshot cannot be torn. Killing the process
from anywhere else would cost every player whatever they had done since the last 60-second autosave.

⚠ Every line goes out on the existing `Notice` toast plus System chat, which needed **no protocol
change** — so this works on a client built before it. It is **not** yet his *"red big"* permanent
overlay; that is `BL-86` and is his call.

### 4. Five staff ranks, plain names and fantasy titles

*"Can we have a (1)Suprime Being > (2)Gods > (3)Sentinels > (4)Silencers > (5)Player … the gods/snetinel
have the fantasy game feeling, but isnt it to childish ?"*

His ruling on being asked: **split them.** The enum, every `/role` argument and every system message use
the plain words; the wearable staff TITLE keeps the flavour. Both layers already existed, so only the
words are new.

| Rank (messages, `/role`) | Title (over the head) | Can                                                                 |
| ------------------------ | --------------------- | ------------------------------------------------------------------- |
| **Owner**                | Supreme Being         | everything, **and is the only rank that can grant or revoke Admin** |
| **Admin**                | God                   | unchanged — every command, and may rank people **below** himself    |
| **Moderator**            | Sentinel              | jail · kick · chatban · where                                       |
| **Chat Moderator**       | Silencer              | **(un)chatban and nothing else**                                    |
| Player                   | —                     | —                                                                   |

🔑 **The Chat Moderator's omissions are the design.** No kick or jail, because *"the jail and kick will
allow them to farm undisturbed - go to zone .. kick players/jail then start to farm"*; no `/where`,
because it *"will allow them to know anywhone on the map where he is so he can take revange or bully
kill"*. This is the rank you hand to someone you do not fully trust.

🔑 **The Owner is a file, not a row.** `owner.txt` beside the exe, one character name on the first
non-blank non-`#` line, **read once at startup**. Deliberately not in the database and not reachable from
any command — his own *"a file in the directory that can be altered only by hand .. read only at start ..
no db no nothing"* — so the top of the hierarchy cannot be granted, stolen or lost to a careless `/role`.
"Cannot have two" is enforced by reading rather than validating. A fresh DB seeds it with `Admin`, so the
whole hierarchy is testable the moment the database is deleted.

⚠ **A real bug fell out of this.** `/role` guarded with `newRole > admin.Role`, which allows **equal** —
so any admin could mint another admin, in flat contradiction of the comment sitting on that very line.
It is now `>=`: you may only grant a rank strictly below your own, which is what makes Admin the Owner's
gift and nobody else's.

### 5. The admin full buff was stale AND stuck at level 1

*"asmin fullbuff should give the new buffs and should fallow buffers buf changes.. Meaning if new
buff/harmony should be added as well in the fullbuff and max effect - now harmonies are L1"*.

Two faults, both fixed at the root:

- **It was a hand-written array**, naming the nine lane groups and four harmonies that existed the day it
  was typed. It is now **DERIVED** from the Warchanter's own class tables — every buff every race of
  Warchanter can learn, filtered to `Category == Buff && DurationTicks > 0` (which drops his attack
  skills, heals, totems and passives in one test), groups first so they cover and evict the singles, then
  the NPC hour-long singles for anything uncovered. **34 hand-listed entries became 62 derived ones.** Add
  a rung, a harmony or a whole new group to `buffer 3rd.csv` and it appears with no second edit.
- **Every buff was applied at level 1.** `GrantFullBuffSet` now applies each at `def.MaxLevel`, so
  Harmony of the Warrior lands at rung 6 and Harmony of Protection at rung 5 instead of rung 1. The NPC
  buffer is untouched — those defs are single-level.

### 6. `/buff [name] [level]`

*"we can add admin command `/buff name [name of buff] [effect level]`"*.

```
/buff                       the whole admin set, every buff at its own top rung
/buff harmony of wizard     that one buff, at ITS top rung
/buff hw 2                  the same buff by acronym, at rung 2
```

The name matcher is forgiving in tiers, first hit wins: **exact name or skill id · acronym · every word
in order · prefix · substring**. It has to be, because his own example does not match literally — he
types "harmony of wizard" for a buff actually called "Harmony of **the** Wizard", so joining words are
skipped on both sides. Acronyms are generated three ways (`hotw`, `how`, `hw`) since there is no one
convention a person uses.

Two details that matter more than they look:

- **The admin set is searched first**, the rest of the catalog only if that finds nothing. Without it,
  "mana blessing" is ambiguous against the hidden per-rung defs that exist only as a group's children —
  names a buffer never casts and an admin never means.
- **One display name means one buff.** "Might" is both the buffer's 3-rung ladder and the NPC's hour-long
  single; reporting that would print *"ambiguous: Might, Might"*. The strongest wins, since the pool is
  ordered groups → class buffs → NPC singles.

An out-of-range rung is **not clamped** — it says what the range is, which is the only way to learn a
ladder's depth without opening the CSV: *"if effect lvl is out of range just system msg with the effect
lvl"*.

### Found on the way past, NOT built — `BL-85`

`/buff harmony of protection 3` on a fully-buffed character **downgrades it from rung 5**, and that is
not the command's fault. Every rung of a Harmony is one `SkillDef` with `Levels[]` and **one** `Rank`, so
`BuffPlan` treats rung 1 and rung 5 as equal — and equal rank keeps whichever has the longer time left.
In a party that means **a level-44 Warchanter's Harmony Lv1 replaces a level-74's Lv5** the moment the
Lv5 has under five minutes on it. The single ladders are fine (they are one-child wrappers, so each rung
resolves to a child carrying its own rank); it is the childless multi-level buffs — the four harmonies,
Great Might, Great Bulwark, Mana Blessing — that fall through to a flat number.

The fix is one line, and it is **deliberately not in this batch**: `BuffPlan` is the resolver every buff
in the game goes through, and moving it alongside two 3rd-class kits would make the playtest unreadable.
See `BL-85`. His Combo Rush ruling is the precedent — *"even if some other buffer procs lvl 3 buff u
still get your effect over"*.

### Checks

`dotnet build` on both halves · **server boots at v0.78.0** (the `net10.0` exe, not the stale `net8.0`
one) · `SmokeTest` **all checks passed** against a freshly-deleted DB · `SkillCsvSeed --check` green on
all ten files · `BalanceMatrix` unchanged · and every new command driven live against the running server
through a headless client — `/help`, `/buff` (full, by name, by acronym, out of range, no match),
`/server` (usage, shutdown, restart, on) and `/role` (grant, promote to admin as Owner, refuse to
re-rank the Owner).


## 2026-08-22 — 0.77.0 shipped: the playtest-26 fixes and the circles reach the phone

*"Build apk/server"*.

`1ead74d` fixed three Warchanter bugs, gave every group buff its numbers back and drew the ground
circles — and, for the **third feature commit running**, bumped nothing. So 0.76.0 on his phone is
blind to all of it, and in the way that is hardest to spot: `Replaces` and the `SkillText` fall-through
live in `Game.Shared.dll`, which the client **compiles in**, and the whole totem/AoE renderer is client
code. Nothing about it could arrive over the wire. Bumped to **0.77.0** and republished both halves.

### `ProtocolVersion` moves this time: 21 → 22

The previous two releases left it alone because nothing on the wire had changed. This one adds a
channel: two server→client pushes, `"Totems"` (the whole visible `TotemList`, resent when the set
changes) and `"AreaEffect"` (a one-shot flash), plus the `TotemDto` they carry. An old client
subscribes to neither and merely draws no circles — but the direction worth catching is the reverse,
the same case as protocol 16: a **new client on an old server** would render empty ground exactly where
the healing is, and the handshake is the only place that pair is refused. `MinAcceptedProtocol` stays
8, so every previously-shipped APK still logs in.



*"Build a totem visual in the client .. I want to see where it stands and the AOE so I can stand
inside … Same goes for all AOE skills … They just flash one time when cast ends as if the effect is
applied … while the totem just stays on the ground … (blue mana, green hp)"*.

### Why nothing was drawn: a totem was never on the wire at all

🔑 **A totem is not an entity.** It is a `TotemInstance` in a plain `List` on the world, deliberately
so — that is what made "a totem is a heal skill with `PlacesTotem`, not a creature" cheap. But every
pixel the client draws comes out of the entity snapshot, so a totem could never appear in it, and
nothing else ever mentioned one. It was not a rendering gap; there was no data.

So it needed a channel of its own, and it got the smallest one that works.

|                                                        |                                           |
| ------------------------------------------------------ | ----------------------------------------- |
| `TotemDto(Id, X, Y, Radius, Heals, Restores)`          | one totem, as the ground needs it         |
| `TotemList` → `"Totems"`                               | every totem the viewer can see, **whole** |
| `AreaEffectEvent(X, Y, Radius, Kind)` → `"AreaEffect"` | one shot, fire and forget                 |

**Whole list, not a diff.** There are single-figure totems in a world and the bookkeeping would cost
more than the payload — and a whole list is *self-healing*: a client that misses a message is
corrected by the next one instead of holding a phantom circle for ever. The loop sends it only when
the visible SET changes, so a world with no totems is silent.

⚠ **Time is deliberately NOT on the wire.** `NextPulseIn` and `LifeTicks` change every tick, so
including either would turn "send when it changes" into ten sends a second per viewer per totem. The
client draws what the server lists and drops what it stops listing — same information, no traffic.

⚠ It is sent **above** the snapshot loop's heartbeat `continue`, or a totem planted beside a player
who is standing still would wait for someone to move.

### The flash is ONE call, not seven

`ExecuteSkill` gets a single line, and every area skill in the game inherits it — party heal, the
resurrection field, all nine Warchanter groups, enemy AoE. That works because **every player area
skill is centred on the CASTER**: `PlayersInRadius`, `EnemiesInRadius` and `DeadPartyInRadius` all
take him as the origin. One site cannot drift out of step with a skill added later; seven would.

It sits past every gate and past `BreakHide`, so it marks the moment the skill *lands* — an
interrupted cast, or one refused for MP, has already returned above and draws nothing. `AreaKindOf`
reads the colour off what the skill DOES, checking resurrection first because a res field also
carries `Heal`, and "you can be raised here" is what the player standing in it needs to read.

### The circles

New `GroundDecals`, built from squashed cylinders like `MoveMarker` and `ZoneOverlay` — but for the
first time **transparent**. `MoveMarker`'s note has always said transparency in URP means a second
material, a render queue and sorting; that is still true, and this is the one place worth paying it,
because a circle you have to STAND IN has to let you see yourself standing in it.

`UnlitMaterials.CreateTransparent` does the paying: URP's Unlit is opaque until told otherwise, and
telling it takes `_Surface`, `_Blend`, `_SrcBlend`, `_DstBlend`, `_ZWrite`, the
`_SURFACE_TYPE_TRANSPARENT` keyword and the Transparent queue. Set `_BaseColor`'s alpha alone and you
get a solid disc. On the built-in fallback shaders there is no alpha at all and it degrades to an
opaque circle — which is his own stated fallback: *"if not I'll work with static semy transperant
circle"*.

- **Totem** — drawn at the server's real radius, resting alpha 0.16, breathing ±0.09 on a 2s cycle.
  **Green HP, blue mana**, his colours; a totem that fills both pools gets **both rings**, the mana one
  drawn slightly smaller and a hair higher so it reads as two rings and not one muddled average.
  The pulse is on the client's own clock — the server's pulse interval is not on the wire, and putting
  it there to drive an animation would have cost a message a second.
- **Flash** — 0.55s, starts at 75% of its radius and snaps out to the true one while the alpha fades
  quadratically. Size is the message, alpha is the goodbye.
- Colliders are stripped from every disc, for the same reason the move marker strips its own: a decal
  must never eat a tap meant for the ground, or standing in your own totem would stop you walking out.
- Circles are cleared at all five places the client tears down the entity list, so a logout never
  leaves one burned into the map.

⚠ **Every AoE BUFF flashes too** — that is "same goes for all AOE skills" taken literally, and a full
group-buff rotation is a lot of yellow in a row. If it reads as noise, muting `AreaEffectKind.Buff`
(or shortening `FlashSeconds`) is a one-line change.

⚠ **A MOB's AoE does not flash.** The one call sits in `ExecuteSkill`, which is the PLAYER cast path;
a boss resolves its own area attacks down a separate branch. Deliberate for now — telegraphing a
boss's ground slam is a real feature and a real balance decision, not a side effect of this one — but
it is the obvious next thing if he wants it.

### Checked headlessly, because this is exactly the bug class the smoke test exists for

A totem worked perfectly for weeks and was invisible; a human playtest cannot tell "the server never
sent it" from "the client never drew it", and it was reported as the latter. So `tools/SmokeTest`
grew a section that reads the pushes directly, on an ork Lightbringer (both totems are his):

```
--- totem + area-effect pushes ---
  planting a totem PUSHES it to the client · carrying the HP flag · and the server's own radius
  the MANA totem is accepted and pushed too (the mana-restorer gate no longer eats it)
  ...and the two totems COEXIST, one per skill rather than one per owner
  an AoE skill flashes its footprint when it LANDS · at the real radius · coloured by what it DOES
  walking out of range clears the circles (the push is a WHOLE list, so it self-heals)
```

🔑 **Writing it found nothing wrong with the feature and three things wrong with the test**, and the
third is worth keeping:

1. `ConnectAsync` **logs in, it does not register**, so a made-up account throws rather than failing a
   check.
2. **Every `Debug*` command is an `IAdminCommand`** — a fresh character is refused with "That is an
   admin-only command." and the section then measures a level-1 nobody. `PromoteToAdminAsync` already
   existed for exactly this.
3. ⚠ **THE WAITS WERE TOO SHORT, AND THE FEATURE LOOKED BROKEN.** Eight checks failed against code
   that was working perfectly. The thing that said so was the **cast bar**: `Healing Totem 4.6s` for a
   skill authored at 1s. This character is NAKED and an ork — WIT 19, the worst in the game — so
   `EffectiveCastSpeedMultiplier` is ~4.6×, and Party Great Heal's 7s cast becomes **32.6s**. The
   harness's default 4s timeout never stood a chance. `WaitFor` returns the instant the condition
   holds, so the fix costs nothing: raise the ceiling, don't shorten the work.

⚠ **And the check order is load-bearing.** A totem lives 30s; the party heal takes ~33s to cast. Run
the range check *after* the flash and the totems have already expired, so "walking out of range clears
the circles" passes against an empty list — a false pass proving nothing. It now runs first, with an
explicit assertion that both totems are still standing when it does.

⚠ The AoE skill is **read off the learned set, shortest cast first**, never hard-coded: the
Lightbringer's Ultimate Party Heal `Replaces` the plain one *and* then demands 4× Skill Stone, so a
literal `party_heal` fails twice over for reasons that have nothing to do with flashes.

🔴 **Needs an APK** — all of the client half ships inside it. No `ProtocolVersion` change: the two new
pushes are additive, an old client simply never subscribes and an old server simply never sends.

## 2026-08-21 — the ork mage stops paying IG's INT bill for a melee he never gets

His three-buffer stat comparison: *"Human got ~3200 Def and 1700 patk the ork have 1800patk and 2200
Def … the only problem is that ork have 31 atk … And 2h blunt ork have almost the same as 1h mace
human (with 1000pdef on top) — check IG for ork mage INT and if it's 31 for our game we should
increase it over the human"*.

### It is 31, verbatim — and that is the bug, not the evidence against it

IG's mystic bases, read off the same source as the mob-curve research:

| mystic | INT    | WIT | MEN | **STR** | CON | DEX |
| ------ | ------ | --- | --- | ------- | --- | --- |
| ork    | **31** | 21  | 42  | **25**  | 31  | 20  |
| human  | 41     | 20  | 39  | 22      | 27  | 21  |
| elf    | 37     | 23  | 40  | 21      | 25  | 24  |

Our mage ATK column was 31 / 41 / 37 — IG's INT, copied straight across.

🔑 **IG has TWO power stats and we have ONE.** STR drives melee there, INT drives magic; our single
`Atk` feeds both. Seeding it from INT alone took the half of the spread the ork mystic LOSES and
threw away the half he WINS — **his STR is the highest of any mystic**, 25 against the human's 22.
So the ork inherited the magic deficit with none of the melee edge, which is exactly why his
two-hander lands where a one-hander-plus-shield does.

### Measured, not asserted — `BalanceMatrix --warchanter`

New mode. It builds all three Warchanters with their real 3rd class, the whole kit they can learn,
and the weapon and armour **their own masteries train** — human heavy/mace/shield, ork heavy/maul,
elf light/bow. `BuildPlayer` could not be used: it stops at the 2nd class and puts a wand or a staff
in every mage's hands, which is the wrong weapon for all three.

It reproduced his reading before anything was changed. At level 90, ork ATK 31:

```
race     CON  ATK  WIT  AGI  SPT |   P.Atk   M.Atk   P.Def   M.Def
human     27   41   20   21   39 |     946     918    1458    1919
ork       31   31   19   20   45 |     994     792     936    1947
elf       25   37   23   24   32 |    2139     854     826    1720
```

**+5.1% P.Atk for the two-hander, against a shield worth +56% P.Def** — his "almost the same … with
1000pdef on top", to the point. And the sweep shows why: at ATK 41, level with the human, the maul
alone is worth **+30.3%**. The ork's ten missing ATK were eating his entire weapon class.

### 47

`41 × (25/22)` — the human mage's ATK scaled by IG's own mystic STR ratio, so the number is derived
from IG rather than invented. Outcome: **+45.6% P.Atk over the human**, which is a clean
two-hander-versus-shield trade instead of a strictly dominated build.

🔑 **It also completes his sentence** — *"Elf have wit/agi - ork have con/spt/int human is in
between"*. At 47 the human mage is the **middle value of all five stats**, and the other two own
exactly the pairs he named:

|       | CON    | ATK    | WIT    | AGI    | SPT    |
| ----- | ------ | ------ | ------ | ------ | ------ |
| ork   | **31** | **47** | 19     | 20     | **45** |
| human | 27     | 41     | 20     | 21     | 39     |
| elf   | 25     | 37     | **23** | **24** | 32     |

⚠ **ATK is one stat per race+base class, so this lands on every ork MAGE** — the Shaman and the Witch
too. Measured rather than reasoned about: the ork **nuker** gains **+11.7% M.Atk** over the human's,
and pays for it with the game's slowest cast (×0.87 against ×0.75) and its lowest magic crit (4.4% vs
4.8%). A slow, heavy-hitting nuker — a coherent identity, not a broken one. If it ever reads as too
much, **44** is the same trade at +37.9% P.Atk and +5.8% M.Atk; the mode takes the sweep as arguments.

⚠ **Base stats are written at character CREATION and persisted**, so this reaches new characters only.
His three test buffers keep their 31 until the `game.db` delete he already owes.


## 2026-08-21 — playtest 26, first three finds: the party HoT he could not cast, and Frenzy that would not die

Three reports off the 0.76.0 APK, all on the Warchanter, and each one turned out to be a different
kind of miss.

### 1. Harmony of Restoration was refused outright — the gate read a FLAG, not a skill

His words: *"cannot use harmony of restoration ... (system: cannot be used on mana restorer) ... only
Restore is forbidden other means of mp regen should work"*.

The rule exists for one skill. **Restore Mana** converts the caster's HP into the target's MP at a
fixed rate, so two restorers topping each other up print mana for free — hence "not on yourself or
another mana-restorer". But the gate was written as `(def.Effect & SkillEffect.RestoreMp) != 0`,
which is **every source of MP in the game**, and Harmony of Restoration carries `RestoreMp` because
its @64 MP/s half rides that flag on a heal-over-time. A party HoT resolves on the caster too, and a
Warchanter *is* a mana-restorer — so his own party heal refused itself.

Now `IsRestoreManaCast(def)` — `def.Id == SkillCatalog.RestoreMana`, all thirteen rungs. What it
frees, exactly:

| skill                  | carries `RestoreMp`   | blocked before | blocked now             |
| ---------------------- | --------------------- | -------------- | ----------------------- |
| Restore Mana           | ✔                     | ✔              | ✔ — the rule is its own |
| Harmony of Restoration | ✔ (the @64 MP/s half) | ✔              | —                       |
| Mana Totem             | ✔                     | ✔              | —                       |
| Restore Spirit         | ✔                     | ✔              | —                       |

The autopilot's `AutoManaTarget` carried a copy of the same test and was narrowed with it, so auto
and manual still refuse exactly the same casts.

### 2. War Frenzy did not remove Frenzy — it named an id nobody learns

`Replaces` read `CastId(FamFrenzy)` = **`cast_frenzy`**, the generic ladder caster. No class is
granted that id: both the Warchanter (52) and the Lightbringer (52) learn **`holy_frenzy`**, whose
display name is plainly "Frenzy". So his CSV column said `[Frenzy]`, the code looked like it agreed,
and the skill it actually named was on nobody's learn list. Now `[holy_frenzy, cast_frenzy]`.

**🔑 And fixing the list was only half of it.** `Replaces` was enforced at LEARN time only, which
assumes the list a skill carried the day you bought it is the list it carries forever. It is not —
it just changed twice in one commit. Without more, the correction would reach nobody who had already
spent the SP, and the only cure would be deleting the character. So superseded ids now **die on
load**, in `ParseLearnedSkills`, next to the retired-id line that has always done the same job. It is
the rule the LEARN LIST has applied all along (`IsSuperseded` hides what you can no longer buy
because you own its replacement) — a skill hidden from the shop and still sitting on the bar was
never a coherent state.

⚠ **His Quick Heal does not come back by itself.** Harmony used to replace it (see below), so it was
stripped when he bought Harmony. Nothing replaces it now, so it returns to the Learn list at the
cleric's 20/25/30/35 rungs — at SP he has already paid once.

### 3. Harmony of Restoration replaces PARTY HEAL, not Quick Heal — his own correction

*"harmony of restoration (my bad that I have forgot) but need to replace party heal"*. It is the
right way round: Great Heal already takes Heal, Harmony is the party heal so it supersedes the party
heal, and Quick Heal survives as the fast single-target cast the Warchanter still wants. All
fourteen `buffer 3rd.csv` rows moved with the code.

### And what "War Frenzy has no description" actually was

The prose was there — the card printed it. What was missing was every NUMBER underneath it.
`SkillText.Buff` read `MagnitudesAt` alone, and **a group buff has no magnitudes of its own**: it
exists to apply `ChildBuffs`, and the numbers live one hop down in the rungs it names. So the card
showed a sentence and then said nothing whatsoever about what the skill does — on War Frenzy, War
Might/Bulwark, Frenzy itself, and **all ten Warchanter groups**. `SkillText.EffectiveMagnitudes`
falls through to the children, so War Frenzy now reads `Max HP −10% | Max MP −10% | P.Atk +8% |
M.Atk +8% | Cast speed +8% | Atk speed +8% | Move speed +8 | Evasion −8`.

⚠ **`ChildBuffsAt(level)`, not `ChildBuffs`** — a laddered group names different rungs at each level.
Read flat (the first cut of this), Frenzy Lv2 described rung 1's −7%/+5% while applying rung 2's
−10%/+8%.

**It immediately caught a stale line of its own.** With the numbers printed under the prose, Frenzy
Lv1 read *"−30% Max HP/MP"* over a rung that gives **−7%**, and *"−8 evasion"* over **−5**. The rung
was re-authored when the IG-√ conversion landed and the sentence never followed; his
`cleric 2nd.csv` row agrees with the rung, so the prose was the wrong half and was corrected to it.

`--check` stays green on all ten files.

## 2026-08-21 — 0.76.0 shipped: the APK and the server the Warchanter actually needs

The 0.75.0 APK was built at 15:22; the whole Warchanter non-buff half landed at 19:34 and **bumped
nothing**, the same slip as the buff layer before it. A class-skill TABLE change is exactly the case
the wire cannot carry — the client builds its Learn tab locally from the compiled `ClassSkills` — so
0.75.0 on the phone is blind to sixteen new families. Bumped to **0.76.0** and republished both
halves: `builds/L2Clone-0.76.0.apk` (41.0 MB, log line `[build] version 0.76.0 (code 7600)`) and
`builds/Game.Server-0.76.0.zip` (14.9 MB). No wire change, so `ProtocolVersion` stays 21.

Verified in the order that matters: `dotnet build Game.sln` (0 errors) refreshed
`Assets/Plugins/Game.Shared.dll` **before** Unity ran, the client type-check was clean, and the
published server boots reporting `L2Clone server v0.76.0 starting.` The DLL's timestamp (20:42) is
older than the APK's (20:46) — the check that catches an APK stamped with the previous version.

## 2026-08-21 — THE WHOLE WARCHANTER: the buffer's non-buff half, 40-74, and the ork gets a second fist

He finished authoring `buffer 3rd.csv` — *"Ok i finished the buffer"* — removed its `NOT DONE`
banner, and said build it. This is the other half of the class: sixteen skill families below the old
banner, plus one skill that did not exist until he asked for it in the same message. **`--check` is
green on all ten files, for the first time since `buffer 3rd` earned its line.**

### What the file needed that the engine did not have

Five primitives, because five of his rows described things nothing in the game did yet. All are
FIELDS, never new `SkillEffect` flags — there are none left (bit 62 is the last, and it is taken).

| his row                                                                               | the primitive                                                                                                                                                                                                                                                  |
| ------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Sound Burst: *"…With Power +1000 **Twice**"*                                          | **`SkillDef.HitCount`** — N independent resolutions of one cast, each rolling its own miss/crit/block. Not a ×2 on power: a 2×1000 volley is worth less than one 2000 against an evasive target and more against a shield.                                     |
| Mana Vampirism: *"+3% mana vampirism (physical basic atack only)"*                    | **`PassiveEffect.ManaVamp` → `Entity.ManaVamp`**, drained on a landed BASIC attack. Its own field, not a reuse of `MeleeVamp`: that heals HP, this refills the bar the buffer actually runs out of.                                                            |
| Harmony of Restoration @64+: *"+90 HP/s **and +5 MP/s**"*                             | **MP-over-time**, riding `RestoreMp`'s Flat magnitude on a lasting buff and ticked in `TickHealOverTime`. ⚠ Deliberately NOT `Regenerate`: natural regen is combat-gated, and a party HoT is the one thing that must keep paying while the party is being hit. |
| Combo Mastery: *"**Doing Damage** Increases Attack/Cast Speed … With 3% Chance"*      | **THE FIRST ON-HIT PROC IN THE GAME** — `ProcChance` / `ProcCooldownTicks` / `ProcSelfRungs` / `ProcPartyRungs` on `SkillDef`, `Entity.ProcCooldowns`, and `GameLoopService.TryOnHitProcs` called from both damage paths.                                      |
| Harmonist Bow Proficiency: *"Bow: Removed Penalty [cast(x2), mAtk(x2), mAcc(x0.04)]"* | **`PassiveEffect.CastPenaltyMult` / `MagicPenaltyMult`**, on the same "0 = not in the chain, otherwise a PRODUCT" convention `MagicFailSelfMult` already used.                                                                                                 |

🔑 **That last one is the most interesting thing in the build.** It is the first skill that *undoes*
the untrained-caster-weapon rule instead of working around it. Spellcaster Mastery charges a bow
×0.5 cast, ×0.5 M.Atk and ×25 into the fizzle chain; his three numbers are the exact inverses, so an
Elf Warchanter with a bow is a **full** caster. That is the whole reason his elf can be an archer and
a buffer at once, and it only works because every one of those penalties was already built as a
multiplier a passive could divide back out.

### The race split IS the class

His design, verbatim: *"human is tank - 1dmg skill and higher Def, elf is archer - range/evasion 1dmg
skill, ork is mele fighter so need more than 1dmg skill"*. One discipline, three combat kits:

|           | armour                          | weapon             | damage skills                       | its own line                                     |
| --------- | ------------------------------- | ------------------ | ----------------------------------- | ------------------------------------------------ |
| **Human** | heavy (Chanter Heavy Mastery)   | blunt + **shield** | Sound Smash                         | Shield Mastery 40/60/70                          |
| **Elf**   | light (Harmonist Light Mastery) | **bow**            | Sound Burst (hits twice, 900 range) | Bow Proficiency · Bow Mastery ×8 · Bow Expertise |
| **Ork**   | heavy                           | blunt              | Sound Smash **+ Acoustic Shock**    | Bloodhanter Blunt Mastery ×8                     |

Shared by all three: Armor Mastery and Spell Mastery (rungs 5-18), Great Heal (11 rungs, 40-68),
Harmony of Restoration, the Reinforcement and Sharpening stances, Combo Mastery, Mana Vampirism
(Human/Ork).

### Acoustic Shock — the ork's second fist

*"Add another skill to the ork buffer same as sound smash (name it Acoustic Shock) just with a stun
effect"*. Same thirteen rungs as Sound Smash — same power 1000→4000, same MP, same SP, same 40 range
and 1s cast — with a **5s stun** on top, and ORK ONLY. Its thirteen rows were written into
`buffer 3rd.csv` in the same pass, because a skill ruled in chat owes its CSV row.
⚠ The stun is **contested** (ATK vs CON, `DebuffSchool.Physical`) like every other CC in the game, so
it is a chance, not a lock, and bosses are immune.

### Two things in his file that were wrong, and what was done about them

🔑 **`mAtk +23, mAtk +15` — the second one is P.Atk.** Every Spell Mastery row in the 40-74 band
carries two M.Atk tokens, which cannot both be M.Atk; his own `cleric 2nd.csv` writes the identical
skill as *"mAtk +6, pAtk +4"*. **Fixed in the CSV** (14 rows) and built as `MagAtk` / `PhysAtk`.
⚠ Note the knock-on: rung 5 had carried `PhysAtk: 18` since it was written, and his row says **15**.
The CSV is the authority, so the built rung moved.

⚠ **Armor Mastery's Light row lost its speed clauses from rung 5 up, and that is a fix.** His 40+
rows read *"Robe/Light/Heavy: mpReg x1.2, pDef +N, maxMP +M"* — one line, three weights, **no speed
clause**. From 40 the penalty-cancelling belongs to the RACE masteries; the copy that rung 5 was
carrying stacked with Harmonist Light Mastery's own ×1.8/×2 and drove an Elf Warchanter straight into
the cast-speed clamp. All three weights are now identical, exactly as his line reads.

### Also in this pass

**Spell Mastery's weapon pair changes at rung 5.** Rungs 1-4 are the cleric's *"with sword/blunt"*;
every 40+ row says *"With blunt/bow weapon"*, so rungs 5-18 use a new `BufferMastery` helper
(Blunt + Bow, never sword). A caster mastery that pays on a **bow** is unusual, and it only works
because Bow Proficiency has already cancelled the penalty that would be eating the same character's
magic.

**The verifier learned four things**: `mana vampirism` as a metric distinct from plain vampirism (one
word apart, two different stats), his *"critical damage resist"* spelling, a fix for the
HoT-vs-MP-over-time collision on `power` (both halves of *"+90 HP/s and +5 MP/s"* now have a
candidate), and two ruled divergences for the armour masteries, whose rows write the RESULT
(*"90%(x1.8)"*) while the code must hold the factor that produces it.

⚠ **One thing `--check` still cannot read, on purpose:** Combo Mastery's compound *"+10/5% and
+5/3%"* notation. No alias table decomposes `X/Y%`. It prints as UNREAD rather than being silently
skipped, which is the tool working correctly — the numbers are verified by eye against the three
rungs in `Skills.Warchanter3rd.Kit.cs`.

### Combo Rush, corrected — one six-rung family, and the only ladder in the game that dips

His compound `+10/5% and +5/3%` notation was the one thing `--check` could not read, so it got asked
about, and the answer reshaped the skill: *"Cast speed goes 5->10->15%, atack speed goes 10->15->20%
and half of both goes to the party as buff (u get the 20% and party 10%) -> so something like 6 levels
of that passives proc-buff and u get 4,5,6 while party gets 1,2,3"*.

| rung | atk speed | cast | who gets it                                    |
| ---- | --------- | ---- | ---------------------------------------------- |
| 1    | 5%        | 2.5% | your PARTY, from a Combo Mastery **L1** buffer |
| 2    | 7.5%      | 5%   | your PARTY, from an **L2** buffer              |
| 3    | 10%       | 7.5% | your PARTY, from an **L3** buffer              |
| 4    | 10%       | 5%   | **YOU**, at Combo Mastery L1                   |
| 5    | 15%       | 10%  | **YOU**, at L2                                 |
| 6    | 20%       | 15%  | **YOU**, at L3                                 |

🔑 **ONE FAMILY IS THE WHOLE MECHANISM.** All six rungs share the BuffKey `wc_combo` and carry their
index as `Rank`, so the ordinary `ApplyBuff` rule — same family, higher rank wins, weaker is ignored
entirely — does everything. Your own rung 4-6 simply outranks any rung 1-3 a party-mate's proc throws
at you. Nothing is special-cased, and two buffers in one party never fight over a bar square. (The
first build had them as two *separate* hidden buffs with different keys, which would have let both
land at once and stack.)

⚠⚠ **RUNG 3 → RUNG 4 GOES BACKWARDS ON CAST SPEED (7.5% → 5%), AND IT IS MEANT TO.** A ladder that
moves backwards is normally a typo and the tool says so. Here it falls out of ranking *half of a
strong buffer's* above *all of a weak buffer's* — and he called the consequence in the same breath as
the design: *"even if some other buffer procs lvl 3 buff u still get your effect over (loosing only 2%
cast in the process)"*. That is precisely this row: an L1 buffer standing next to an L3 buffer keeps
his own rung 4 and forgoes the 2.5% extra cast speed rung 3 would have handed him. **Do not straighten
it into a rising line.**

`SkillDef` therefore carries `ProcSelfRungs` / `ProcPartyRungs` — arrays indexed by the passive's
level — rather than one id per side. The 4/5/6-vs-1/2/3 offset is the thing he is authoring, so it
lives in the data where it can be read, not in an arithmetic rule.

**And the row is now fully verified.** `--check` follows a proc to the buff it fires (the numbers are
never on the passive itself), and learned `chance` as a metric, so *"attack speed +10% and cast speed
+5% … With 3% Chance"* is checked end to end. The party half stays in **parentheses** on purpose: the
segmenter strips those as commentary, and two `as` tokens in one row would collide and let the later
one silently win — which is exactly the `mAtk +23, mAtk +15` trap this same pass had to fix.

### Verified

`dotnet build` clean (server + shared + the Unity client's own csproj); the server boots at v0.75.0
with no catalog-collision failure; **`tools/SmokeTest` ALL CHECKS PASSED** against a live server on a
fresh `game.db` — worth running here because this pass adds ~150 learn rows and the skill bar's
auto-placement is server-side; `--check` reports **no discrepancies** across all ten files.

🔴 **Needs a new APK** — the client builds its Learn tab locally from the compiled `ClassSkills`.

## 2026-08-21 — Shield Mastery re-authored: one skill, two classes, and the ×5 kept where it belongs

He re-authored Shield Mastery across three files in one pass — `tank 2nd.csv`, the previously empty
`tank 3rd.csv`, and `buffer 3rd.csv` — and told us what to do with the numbers in it: *"there is
shield mastery for human buffer and for tanks but human buffer leans it 40+ while tank have the 3 lvls
before the 40 .. and 4th at 52 while humab buffer dont learn lvl 4 ... and the % of the shield mastery
are the IG one so fix them in the process"*.

### The ladder — his percentages, ×5 on one column only

| rung | his DESCR (IG units)                                          | built                                                                                     | tank learns @    | Human Warchanter learns @ |
| ---- | ------------------------------------------------------------- | ----------------------------------------------------------------------------------------- | ---------------- | ------------------------- |
| 1    | Shield P.Def **+30%**, Shield Rate +50%                       | `ShieldDefPct 1.50`, `BlockChancePct 0.50`                                                | **20** (3200 SP) | **40** (36k SP)           |
| 2    | Shield P.Def **+40%**, Shield Rate +70%                       | `ShieldDefPct 2.00`, `BlockChancePct 0.70`                                                | **28** (3200 SP) | **60** (120k SP)          |
| 3    | Shield P.Def **+50%**, Rate +85%, +10% P.Def, bow resist 16%  | `ShieldDefPct 2.50`, `BlockChancePct 0.85`, `DefencePctWithShield 0.10`, `BowResist 0.16` | **36** (40k SP)  | **70** (390k SP)          |
| 4    | Shield P.Def **+60%**, Rate +100%, +10% P.Def, bow resist 24% | `ShieldDefPct 3.00`, `BlockChancePct 1.00`, `DefencePctWithShield 0.10`, `BowResist 0.24` | **52** (74k SP)  | *never*                   |

🔑 **The ×5 is on the shield-P.Def column and nowhere else**, which is not a new decision — it is the
2026-08-12 ruling still standing: *"sheild_mastery.Shield_PDef will be the only part that will increase
5 times — the sheild chance, arrow defence and other passives, sets and buffs that increase the
shieldPdef/chance etc are kept as is."* Every shield's flat `ShieldDefense` is a fifth of what it was
(`90 143 203 …` → `18 29 41 …`, Items.cs), so the mastery multiplier is the half that gives it back to
the class that paid SP for it. **Shield Rate (block chance) and the new +10% P.Def are copied verbatim.**

🔑 **THE CSV STAYS IN IG UNITS ON PURPOSE.** `--check` reads the DESCR column now, so a 30-vs-150
mismatch would print on every run; the `("shield mastery","shielddef")` entry in
`tools/SkillCsvSeed/Descr.cs` was rewritten to cover the whole four-rung ladder and both files, and it
prints as **⚪ RULED** with both numbers side by side rather than as a defect. His file reads in the
units he authors in; the game runs the compensated number.

### The +10% P.Def is SHIELD-GATED, and bow resist came back

Both were queried and both were ruled the same day.

**The +10% P.Def** is the wearer's WHOLE physical defence — armour, jewels, everything, not just the
shield's share — and it pays only while a shield is equipped: *"The 10% pDef (overall pDef not only
shieldPDef) is only when shield is equipped (IG is shield+heavy but I'm not sure if we can)"*. It has
its own field, **`PassiveEffect.DefencePctWithShield`**, because plain `DefencePct` is unconditional
and is used by masteries that must keep paying with a two-hander. ⚠ We *can* test the armour weight
too — `ArmorWeight` is right there in the same block — so IG's shield+heavy version is one `&&` away
if he wants it; he asked for shield-only and that is what is built.

**Bow resistance** had vanished from every row when he re-authored them: *"My mistake in the hurry ..
Make lvl 3 +16% and lvl 4 +24% bow resist"*. Restored, and note it moved UP the ladder — it used to
start at rung 2 (16/16/24) and now starts at rung 3 (—/—/16/24). Worth flagging because this passive
is the **only** carrier of `BowResist` in the entire player kit: with those two lines gone, a built
stat had no source at all.

### One skill, two prices — `ClassSkill.SpCost`

The tank pays 3200 for rung 1 at level 20; the Human Warchanter pays 36,000 for the same rung at 40.
SP in this game is priced by **the level you buy at**, not by the ability, so `ClassSkill` gained an
optional `SpCost` override and a `SpCostFor(def)` accessor; `ClassSkills.SpCostOf(...)` is the server's
lookup. All four readers go through it — the learn charge in `GameLoopService`, the client's Learn tab,
`--check`, and the CSV seeder. The alternative was a second `SkillDef` named "Shield Mastery", which
would have duplicated a ladder he authored identically in both files and invited it to drift.

### What actually moved, measured

`tools/BalanceMatrix`, tank in 1H+shield, before → after:

| level | P.Def         | survives        |
| ----- | ------------- | --------------- |
| 20    | 498 → **498** | 104s → **104s** |
| 28    | 533 → **533** | 80s → **80s**   |
| 36    | 556 → **617** | 64s → **71s**   |
| 44    | 717 → **796** | 74s → **80s**   |
| 52    | 801 → **892** | 65s → **73s**   |

**Nothing regresses.** The old ladder was `1.50 1.50 2.00 2.00` at 20/24/28/32 and the new one is
`1.50 2.00 2.50 3.00` at 20/28/36/52, and those happen to agree exactly up to 35 — a tank holds 1.50
from 20 and 2.00 from 28 either way. From 36 he is ~11% better off and keeps gaining at 52, where the
old ladder had been flat for twenty levels. The gaps at 24 and 32 are his, like Provoke's missing
level-20 row.

### Also: `tank 3rd` joins `--check`, and the buffer file's banner is gone

`tank 3rd.csv` was a `# start here` placeholder until this pass; it has one real row now, so it earned
its line in `Check.Specs` (as Bulwark — the row is registered to Vanguard too, and one spec names one
discipline). And he removed the `NOT DONE` banner from `buffer 3rd.csv` — *"Ok i finished the buffer"* —
so `ReadCsv` no longer stops at line 186 and the whole file is live. That immediately reported sixteen
unbuilt skill families as 🔴 NOT REGISTERED — which is the pressure working exactly as designed, and
which the entry above this one then went and built.

🔴 **Needs a new APK**: the client builds its Learn tab locally from the compiled `ClassSkills`, and
this pass moves four learn rows and adds three.


## 2026-08-21 — THE WARCHANTER'S BUFF LAYER, 40-74: nine groups split by lane, four Harmony ladders

The second discipline authored end to end, and the first built from a HALF-finished file. Owner:
*"buffer 3rd is done to the 186 row ---NOT DONE--- everything below is not done.. I managed to do all
the buffs all the harmonies and all the group buffs to lvl 74 .. leter ill do his passive/atack skills"*.

### Why it happened: the groups mixed the two channels

He opened the session with the complaint: *"i would like to change the grouped buffs .. not to mix magic
with fighters buff .. to be like the haromonies several for fighter several for mage and several
combined(defences)"*. He was right, and the five old groups show it plainly:

| old group          | children                                        | the problem                             |
| ------------------ | ----------------------------------------------- | --------------------------------------- |
| Swift and Sure     | move, **cast speed**, evasion, **attack speed** | fighter and mage in one cast            |
| Focus and Ferocity | crit rate, crit dmg, **magic crit**             | ditto                                   |
| Might and Bulwark  | P.Atk, **P.Def**, vamp, accuracy                | fighter offence + a shared defence      |
| Force and Ward     | M.Atk, **M.Def**, interrupt                     | mage offence + a shared defence         |
| Body and Soul      | Max HP/MP, both regens                          | genuinely combined — the only clean one |

He then authored the replacement himself, and the NAMES carry the lane: **Feral\*** = fighter,
**Arcane\*** = mage, **Arcane and Feral \*** = both.

| group                       | lane     | children                           | @   |
| --------------------------- | -------- | ---------------------------------- | --- |
| Feral Precision             | fighter  | crit rate · crit damage · accuracy | 58  |
| Feral Bloodlust             | fighter  | P.Atk · attack speed · vampirism   | 74  |
| Arcane Insight              | mage     | M.Atk · magic crit                 | 72  |
| Arcane Serenity             | mage     | cast speed · interrupt · MP regen  | 70  |
| Soul Reinforcement          | mage     | Max MP · M.Def · MP cost           | 74  |
| Body Reinforcement          | combined | Max HP · P.Def · HP regen          | 72  |
| Shield Reinforcement        | tank     | shield P.Def · block chance        | 74  |
| Arcane and Feral Protection | combined | both CC resists                    | 74  |
| Wind Grace                  | combined | move speed · evasion               | 56  |

Plus three PARTY ECHOES — `War Frenzy` @56, `War Might` / `War Bulwark` @74 — each handing the whole
party what its single-target version gives one ally.

🔑 **EVERY GROUP ARRIVES ONE LEARN TIER AFTER ITS LAST CHILD TOPS OUT.** His rule: *"The group buff
shoul be learned 1 learn tire after the last buff is maxed out"*. Each line in the class table carries
the child levels it derives from, so the rule stays checkable instead of being trusted.

### 🔑 THE MP COLUMN IS SELF-CHECKING — and it caught four errors proof-reading missed

Row 2 of his file states the rule: *"Each max lvl buff of the group MP + the group learned lvl MP
cost"*. So `groupMp = Σ(each child single's TOP-rung MP) + the band MP at the group's own learn level`.
All eleven groups and echoes satisfy it to the MP — and because they do, the sum **identifies the
children**, which is how four copy-paste slips were found in rows that read perfectly:

- **`Body Reinforcement` @72** — 402 only decomposes as Body 120 + **Bulwark 72** + Vigor 85 + 125. Its
  `REPLACES` said `[Ward Soul Mana Blessing]` (copied from Soul Reinforcement) and its `DESCR` said
  "+35% Max HP **and Max MP**" while omitting Bulwark's +15% P.Def. Both wrong; the arithmetic wasn't.
- **`Wind Grace` @56** — 198 only closes with the CLERIC's level-30 Swift (33) + Agility (80) + 85,
  which is what identified its two children in a file that authors no Swift row of its own.
- **`Arcane and Feral Protection` @74** — children right, but SP was 42k: the level-**56** band.
- **`Resurrection` @56** — SP 81k against the healer's 42k, and 81k → 45k ran backwards.

### 🔑 HARMONIES ARE A DIFFERENT SHAPE NOW: 5 minutes, 2-minute reuse

Not 20 minutes. His reasoning, verbatim: *"its not a buffs they are additional support … The idea is
the buffer is a must .. not enter party buffs get kicked for 20 mins ... need to stay and rebuff thats
his job"*. IG's own is 2 minutes; 5 is the compromise he settled on.

They are still **not groups** — own `BuffKey`, cover no family, evict nothing, and MULTIPLY on top of
the basic layer. That is the whole reason the tier exists.

**MP is `60 × 1.1^i` per buff inside**, summed: 60 / 66 / 73 / 80 / 88 / 97, so the ladders total
60 / 126 / 199 / 279 / 367 / 464.

| harmony                | rungs                 | ends                                                                         |
| ---------------------- | --------------------- | ---------------------------------------------------------------------------- |
| Harmony of the Warrior | 6 @ 40/44/48/56/58/74 | double crit rate, +35% crit dmg, +4 acc, +12% P.Atk, +15% atk speed, 8% vamp |
| Harmony of Protection  | 5 @ 44/52/56/66/74    | +30% M.Def, +20% HP regen, +25% P.Def, +30% Max HP, 20% melee reflect        |
| Harmony of Speed       | 2 @ 48/58             | +20 move, +3 evasion — **and it stops**                                      |
| Harmony of the Wizard  | 2 @ 48/52             | +10% M.Atk, +30% cast — **continues in `buffer 4th.csv`**                    |

⚠ **Speed stopping at 58 and the Wizard at 52 is a RULING, not an unfinished ladder** (*"The speed one
stops - no more buffs for it; Wizard continue in Buffer 4th"*). The Wizard's old +20% MP regen and
−30% magic MP cost are deliberately gone from the 3rd tier: the MP-cost half is Mana Blessing's job
now, the regen half is on the 4th-class ladder. Do not restore them.

### 🔑 HARMONY OF THE WARRIOR IS +100% CRIT RATE, NOT +75%

He asked why it was 75, and the answer was that **nothing had decided it**. The 0.75f was authored
2026-07-03 in `6211942`, a month before the crit model landed, and was never reconciled — his own
worked ladder in `docs/design/CritBlowAndDouble.md` §5 uses **Harmony ×2** on every line (dagger
`132 ×1.3 ×1.5 ×2 = 514 → capped 500`; bow `205 ×2 = 410`; sword `88 ×1.3 ×2 = 228`). Every *other*
multiplier in that chain already matched the code — Focus ×1.30, the rogue passives ×1.20/×1.50, the
3:2:1 weapon factors — so Harmony was the lone survivor of the old numbers.

It matters beyond tidiness: at ×2 a maxed melee rogue reaches 514 and is clamped to the **50% cap**,
which is what the cap is *for*. At ×1.75 he stopped at 45% and **nothing in the game ever touched the
ceiling**. He authored ×2 into every rung of the ladder.

⚠ `TestChecklist.Unity.md` §52d still asserts the ×1.75 figure ("Add Harmony of the Warrior on top →
**36%**"). It is **41%** now.

### 🔑 THE CHECKER NOW STOPS AT HIS `NOT DONE` BANNER — so a half-finished file is checkable

`Check.ReadCsv` breaks on the banner, and **`buffer 3rd` earned its line in `Check.Specs`**. The
authored half is compared; the stubs below are invisible. As he moves the banner down, the
newly-authored rows start being checked automatically.

That paid for itself immediately — with the spec added, `--check` found four real code defects that
the build could not:

- `Great Might` / `Great Bulwark` / `Mana Blessing` rung 3 still sat at level 74 / 130 MP / 450k SP.
  He moved all three to **72 / 125 MP / 330k SP** in BOTH files this session; the class table had been
  updated for the buffer and not for the healer, and the `SkillDef` levels not at all.
- `Shrouding Hymn` SP was 12000 against his 880000.

And the catalog's own startup guard caught a fifth before that: `Rung(FamAs, 3)` produced
`buff_spd_as_3`, which does not exist — **the four speed families use hand-written ids**
(`buff_haste_r`, `buff_alacrity_r`, `buff_swift_r`, `buff_agility_r`), not the `buff_{family}_{n}`
shape every other ladder uses. Worth remembering: `Rung()` is not universal.

### What else moved

- **His singles are the healer's ladder, rung for rung** — all twenty-five families are learned at the
  same character level and the same rung index the Lightbringer gets, verified family by family. So
  the class table authors no values at all: it is WHICH rung and WHEN. Retuning a ladder moves both
  disciplines together, automatically.
- **`Resurrection` stops at 66** for the buffer (80% of lost exp) where the healer's runs to 74 and
  100% — the clearest line between the two disciplines.
- **`Shrouding Hymn` moved 30 → 74.** It is the PARTY stealth and belongs at the top (*"IG learns it at
  ~80"*); the level-40 SELF version is `Conceal`.
- 🔑 **`Madness` IS `War Frenzy` — renamed, not replaced** (*"madness is now named War Frenzy and its
  the 56lvl group frenzy buff"*). Its level 76 was always an explicitly temporary home (*"and when the
  kits land we will move it"*); this is that kit landing. **The id stays `madness`** — append-only, and
  characters carry it — while the const is now `WarFrenzy` and the skill is called War Frenzy. It drops
  76 → **56**, rung 7 → **rung 2** (his row is Frenzy @52's payload verbatim: the reward is the PARTY,
  not a bigger number), MP 220 → **165** and SP 100k → **45k** — and 165 satisfies the group-MP rule as
  Frenzy's top-rung 80 + the 85 his band charges at 56. ⚠ A first pass of this build invented a
  SECOND skill (`wc_war_frenzy`) for it, which would have left the original orphaned; that was backed
  out. ⚠ **`FamFrenzy` rung 7 is now reached by nothing** — allowed (Resolve carries a dead rung too),
  but it is not live content, and rungs 3-6 are still weaker than his authored rung 2.
- **The five old groups are no longer taught**, nor is `HolyShield`. Their defs stay in the catalog —
  deleting one orphans every character who bought it — but no class grants them.
- **`AdminBuffSet` rebuilt** around the nine lane groups, the two echoes and the four harmonies.
- 🔴 **`RegisterWarchanter()` is now genuinely dead code.** It was already switched off; its per-race
  Bolt/Chant/Renew/Pass were the invented pre-CSV kit, and the attack half of his file replaces them.

### The MP budget he sized it against

> *"buffer needs 3268 /20 min to keep buffs active ~2.72mp/s and 1126/5min to keep harmonies ~3.75/s ..
> so ~6.5 mp regen keeps all up and runing ... and thats is without the mp consumption buff so i say 5k
> mp one time to fully buff a whole party is a good way to start farming"*

`dotnet build` clean · `--check` green on `healer 3rd` **and** `buffer 3rd` · Unity `Assembly-CSharp`
clean. ⚠ Needs the **APK that is already pending** — the client builds its Learn tab locally from the
compiled `ClassSkills`. No version bump: that is his call at deploy time.


## 2026-08-21 — three renames from his CSVs, and Resolve caps at +54

Four owner edits made straight into his CSVs, mid-session, while he was authoring `buffer 3rd.csv`.

### 1. `Haste` → `Fury` (the attack-speed family)

He renamed the rows himself in **both** `buffer 3rd.csv` and `healer 3rd.csv`. Because `healer 3rd.csv`
is one of the eight files `--check` walks, that immediately went red — and red in the way the name
mismatch always goes red, which is worth recording because it hides everything else:

```
🔴 NOT REGISTERED  Fury — 2 authored rung(s) at 44/52, the class learns none.
🟠 NOT IN THE CSV  Haste — the class learns 2 rung(s) at 44/52 with nothing authored.
```

Four numeric columns on those two rows went unchecked for as long as the names disagreed. Same shape
as the `Taunt`/`Provoke` mismatch in the fighter files.

**DISPLAY NAMES ONLY — every id is untouched**, because ids are append-only: the family is still
`spd_as`, the rungs are still `buff_as_1..3`, the potion skills are still `pot_haste_*`, the scroll
skills `scr_haste_*`, and the items `potion_atk_c/u` and `scroll_atk_r`. Read the const, not its name.

- `Skills.Common.cs` — the three ladder rungs, the three potions (`Fury Potion` / `(Lesser)` /
  `(Greater)`), the three scrolls (`Scroll of Fury …`).
- `Skills.BuffLadders.cs` — the castable family name. `Skills.Buffer.cs` — the NPC buffer's single.
- `Items.cs` — the two potion `ItemDef`s and `Scroll of Fury`.
- `nuker 3rd.csv` — its two rows still said `Haste` (the same values, copied from the healer's).
  Brought in line; it is not a `--check` file yet, so nothing would have caught it.
- `GameUi.Debug.cs` — the debug-give button's label.
- `docs/design/BuffLadders.md`, `docs/guides/ItemIds.md`. CHANGELOG and `Playtest-Archive.md` keep the
  old name: they are the record of what was said, not of what the game currently calls things.

⚠ **The consumables followed the family**, the way Swift/Alacrity/Agility already do — a Fury Potion
grants Fury. ⚠ `Battle Fury` (`battle_fury`) still exists in the catalog: retired, granted by nobody,
def kept so it does not orphan anyone. No collision, but the two names now sit together in a search.

### 2. Resolve +54 at 68 — and 54 becomes the ceiling: the 60 rung is parked

Owner: *"for healer again forgot … 1 more lvl"*, then
`68,Resolve,Magic/Buff,600,self/target,1,1,1200,+54 interrupt resistance.,115,165k,[]` — followed a few
minutes later by the ruling that decides the rest of it: *"54 is max resolve for now .. comment out the
60lvl .. no1 is leatrning itatm"*, and *"the healer buffs are source of truth now ... buffer later"*.

+54 sat **between** rung 6 (48) and the old top of 60. A family's rung index *is* its rank, so holding a
middle value normally means inserting and renumbering everything above it — the treatment six families
got on 2026-08-20. Here the top was **capped instead**: `Resolve` is `18, 25, 36, 40, 42, 48, 54`, seven
rungs, and **54 is the ceiling**.

🔑 **60 IS PARKED, NOT DELETED.** The value left `Ladder(FamInterrupt, …)` and `BuffIntr8` is commented
out beside the live consts. Re-adding 60 to that one array is the whole restoration — the id is reserved
and nothing else has to move. ⚠ Ids are append-only, so `buff_interrupt_8` must never be reused for
anything else.

⚠ **He was almost right that nobody learns it.** The Warchanter did — `Third.cs:233`, +60 at 52 — but
that is the *invented* pre-CSV buffer kit, and his second message settles it: the healer's authored
numbers are the source of truth and the buffer is rebuilt from `buffer 3rd.csv` later. So everything
that handed out 60 now hands out his 54:

- `Skills.Buffer.cs` — `NpcResolve` (the NPC newbie buffer's hour-long single) and `Force and Ward`'s
  child list, both descriptions with them.
- `Skills.Healer.cs` — `HolyForce` levels 5 and 6.
- `ClassSkillTables.Third.cs` — the Warchanter's row at 52 is now `SkillLevel: 7`.

The healer's own line gains the rung: `At(CastId(FamInterrupt), (44, 3), (52, 5), (60, 6), (68, 7))`.
Its price comes from a new band constant `H68 = R(115, 165000)`, which lands cleanly between
`H66 = R(110, 145000)` and `H70 = R(120, 200000)` — monotonic in MP and in SP.

⚠ `docs/design/BuffLadders.md`'s ladder table was **two edits stale** and is now current: `interrupt`
read `18 / 25 / 40 / 60` (it never got the 36/42/48 inserts) and `vamp` read `3 / 6 / 9` (it never got
7/8%). Both corrected.

### 3. The shield pair renamed: `Shield Bless` → **Shield Blessing**, `Shield Harden` → **Shield Hardening**

Display names only, both times — the families are still `shield_block` / `shield_def` and the rungs are
still `buff_shield_block_1..6` / `buff_shield_def_1..3`.

He renamed Blessing in `healer 3rd.csv` himself; Hardening he renamed in `buffer 3rd.csv` and ruled in
chat, so the healer's three rows (@58/66/72) were brought over to match — *"the healer buffs are source
of truth now"*. `nuker 3rd.csv` carried stale copies of both and was brought in line too (it is not a
`--check` file, so nothing would have caught it).

🔑 **THE TWO RENAMES FAILED DIFFERENTLY, AND THAT IS THE USEFUL PART.** `Shield Bless` → `Shield
Blessing` reported as
`🔵 NAME DRIFT  CSV "Shield Blessing" = code "Shield Bless" (matched on spelling; rungs 6 vs 6)` — a
fuzzy match, so **all six rungs were still compared**. `Haste` → `Fury` shares no spelling, so it
reported as two unrelated skills (`🔴 NOT REGISTERED` + `🟠 NOT IN THE CSV`) and **compared nothing**.
The checker degrades gracefully on a near-miss and goes blind on a true rename — so a rename to an
unrelated word is the one that has to be re-checked deliberately.

- `Skills.BuffLadders.cs` — both ladder names and both castable names, plus the family comments.
- `docs/design/BuffLadders.md`. His own quoted words (*"it should rapladse Shield Harder and Shield
  Bless"*) are left verbatim — they record what was said, not what things are called.

🔴 **THE GROUPS WERE DELIBERATELY NOT TOUCHED.** `HolyShield` still reads `"Shield Bless and Harden"`
even though both of its singles have been renamed, because he is redoing the improved groups with new
names of his own — `buffer 3rd.csv` already calls this one **Shield Reinforcement** @74. An earlier pass
in this session had renamed the group to match its singles; that was **reverted**, so this commit
contains no group-name decision at all.

⚠ That doc paragraph was also stale: it still claimed **"each family has one rung today"**, which stopped
being true on 2026-08-20 when both shield ladders were authored in full. Corrected to the real ladders —
Shield Blessing 5/10/15/20/25/**30**% (@40/48/56/62/66/70), Shield Hardening 30/40/**50**% (@58/66/72).

`dotnet build` clean, `--check` green with no `UNREAD`, Unity `Assembly-CSharp` clean.
⚠ All of it is class-skill-table/catalog change and needs the **APK that is already pending**.


## 2026-08-21 — the healer's missing cast-speed buff: Alacrity rung 3 at 48

Owner, mid-session: *"have forgoten on healer the cast speed buff"*, then the row itself —
`48,Alacrity,Magic/Buff,600,self/target,1,1,1200,+30% Cast Speed.,75,32k,[]` (now `healer 3rd.csv:74`).

**Nothing new was authored.** `Alacrity` already existed as family `spd_cast` with three rungs
(+15% / +23% / +30%), and his +30% is `BuffAlacrityR` verbatim — same magnitude, same 600 range, same
1s cast, same 1200s duration. What was missing was the *learn row*: the cleric teaches rungs 1-2 (at
20 and 35) and the Lightbringer's buff block simply never finished the family, which is exactly why
the gap was invisible — the buff worked, nobody could reach its top rung.

🔑 **THE CODE HAD ALREADY PREDICTED THE ROW.** `ClassSkillTables.Common.cs` said of the cleric's L2:
*"Alacrity STOPS at L2 (+23%): cast speed past that is a 3rd-class reward"* — and `H48 = R(75, 32000)`
was already sitting in the healer's per-level price table. His row is that reward, at that price.

- `ClassSkillTables.Third.cs` — `At(CastId(FamCast), (48, 3))` added to the shared Lightbringer block.
- `Skills.BuffLadders.cs` — **rung 3 was UNPRICED**: the cost array held two entries for three
  children, so +30% fell through to the generic 30→50 formula. Added `H48`, so it now costs the
  level's own price like every other healer buff row.
- `ClassSkillTables.Common.cs` — the "3rd-class reward" comment now names where the reward landed.

✅ `dotnet run --project tools/SkillCsvSeed -- --check` — **no discrepancies**, all eight files.
🔴 **NEEDS THE PENDING APK** — a class-skill TABLE change; the Learn tab is built from the compiled
`ClassSkills`, not pushed by the server.

⚠ **The WARCHANTER still has no `FamCast` row either** — it reaches +30% cast only through *Swift and
Sure* at 70, never learning the single. Left alone deliberately: `buffer 3rd.csv` is a placeholder the
owner is editing right now, and nothing is built from it until he says it is done.

## 2026-08-21 — Mana Ray vs the IG drain formula: MEASURED, NOT CHANGED (+ the fizzle curve, documented)

He brought IG's own mana-drain formula — `mpDmg = √mAtk · power · (enemyMaxMp / 97) / enemyMDef` —
and asked how ours compares. Read it carefully and **it is our model D with the magic pipeline
multiplied back in**: the `(enemyMaxMp / 97)` term *is* the pool-proportionality that made D fair,
and `√mAtk / mDef` is our own magic ratio on top. IG never chose between "a share of the pool" and
"a damage number"; it multiplied them.

**Measured**, not argued — two new columns in `tools/BalanceMatrix -- --mana-ray`:

| target   | pool | D pool share     | E' IG shape, renormalised |
| -------- | ---: | ---------------- | ------------------------- |
| tank     |  696 | 100 · 14% · 7.0× | 96 · 14% · 7.2×           |
| champion |  696 | 100 · 14% · 7.0× | 107 · 15% · 6.5×          |
| nuker    | 2662 | 385 · 14% · 6.9× | 378 · 14% · 7.0×          |
| healer   | 3158 | 457 · 14% · 6.9× | 449 · 14% · 7.0×          |

The fairness invariant survives the IG shape (±8%: "7 casts to zero anyone" becomes 6.5–7.2) because
M.Def is nearly flat across classes at 74, and it would have bought back a gear axis (×2 M.Atk →
×1.41 drain). A single constant was enough for the whole level range — the ratio drifts only 19%
from 40 to 85, so no level-indexed reference curve was needed.

🔴 **RULING: `"leave it as is"`.** The engine keeps model D. Nothing in `Game.Shared` or
`Game.Server` changed. The `E`/`E'` columns stay in BalanceMatrix, labelled as measurement only, so
the comparison never has to be re-derived.

🆕 **`--fizzle [casterLevel] [from] [to]`** — a new BalanceMatrix mode printing the magic-fail curve
out of the shipped `StatCalculator.MagicFailChance`, with the tank ×2, the +4 magic-evasion and the
bow ×25 columns beside the plain one. Its level-74 output is now written into
`docs/balance/BalanceMatrix.md` as a **CURRENT** section, above that file's stale banner's reach.

🔑 **THE SKILL'S OWN LEVEL IS NOT AN INPUT TO THE FIZZLE.** `GameLoopService` passes `caster.Level`
and `target.Level` — a level-80 healer casting his level-74 rung fizzles as an 80. There is no
per-rung fizzle, and the question "what is the fizzle of a 74 skill" has no answer except through
the caster.

🔑 **Casting DOWN is free** (`1.3^Δ` rounds to zero from Δ−3, so 71 and below is a flat 0%);
**casting up** is 5% at +6, 18% at +11, 67% at +16, ceiling from +18 — matched to
`StatCaps.CcLevelFloorGap`, so spells and control stop landing at the same level. And a fizzle is
**not a miss**: it still lands `damage / 3` and still rolls the interrupt.


## 2026-08-20 — THE HEALER IS BUILT: `healer 3rd.csv`, levels 40-74, end to end (0.74.0)

**The Lightbringer is the first fully-authored 3rd class in the game.** He finished the file and said
go — *"OK get the healer 3rd file and build away"* — so the 40+ purge no longer has a healer-shaped
hole in it: every rung of the discipline, 40 to 74, comes off one of his rows. Roughly **340 authored
rows** became **~430 learn entries**, 12 new skills and 6 re-laddered buff families.

🔑 **`--check` NOW COVERS IT.** `tools/SkillCsvSeed --check` walked seven files before today; it walks
eight, and `healer 3rd.csv` reports **zero discrepancies** with **every number either verified or
explained by a ruling** (`--check -v`). That is what makes "the CSVs and the game move together"
checkable for a 3rd-tier file rather than a promise — and it is the only reason a build this size can
be called correct instead of merely finished.

🔴 **NEEDS A NEW APK.** The Learn tab is built from the compiled `ClassSkills`, so none of this is
reachable on his current phone build. 🔴 **And a `game.db` delete** — no schema changed, but the buff
rung ids underneath six families moved (below).

### What the discipline is

Three races, one job, and the split happens **exactly twice** — once on the fast heal and once on the
control debuff. Everything else in the 40-74 kit is shared.

|            | Human                                                                  | Elf                                                                                     | Ork                                                        |
| ---------- | ---------------------------------------------------------------------- | --------------------------------------------------------------------------------------- | ---------------------------------------------------------- |
| its heal   | **Quick Great Heal** — Great Heal's power on a 2s cast for 1.5x the MP | **Healer Blessing** — heals less, and cures bleed/poison up to a rank that climbs 3 → 9 | **Healing Totem** — planted ground, +64 → 150 HP/s for 30s |
| its debuff | **Gravity** — −7 → −23% attack AND cast speed                          | **Bind** — a 30s hold at every rung                                                     | **Armor Break** — −10 → −30% P.Def, −5 → −15% M.Def        |
| extra      | —                                                                      | —                                                                                       | **Mana Totem** from 52 — +10 → 20 MP/s                     |

**Twelve new skills**: Urgent Heal (a % of the target's OWN max HP, four rungs and then it stops for
good — that is why it stays relevant at 74), Ultimate Heal and Ultimate Party Heal (Skill Stones, 1
and 4), **Resurrection Field**, Mana Totem, **Mana Ray**, **Mana Strain**, Meditation, Weapon Break,
Mana Blessing, and the **Great Might / Great Bulwark** pair.

🔑 **GREAT MIGHT AND GREAT BULWARK SHARE A BUFF KEY.** That single line IS his *"Does not stack with
Other Great Might|Bulwark effects"* — a shared family key is non-stacking in this engine, so the rule
needed no new mechanism. Both sit at Rank 1 at every level so the choice stays re-makeable mid-fight;
a rank ladder would have let a level-74 Might lock out a level-74 Bulwark. ⚠ They are NOT rungs of the
ordinary Might/Bulwark families and must never be folded into them — they stack **on top**.

### Three engine gaps his file opened

1. **`SkillLevel.CastTicks`** — Resurrection's cast SHORTENS with its rung, 10s at 40 down to 5s from
   62. It is the only ladder in the game that does that, and a res you can land inside a fight is now
   something you **buy** rather than something cast speed alone gives you.
2. **`SkillLevel.AreaRadius`** + **the area resurrect**. Resurrection Field is a res aimed at the
   GROUND, and nothing else in the game targets the DEAD in an area (`PlayersInRadius` skips them by
   construction). It needed its own scan (`DeadPartyInRadius`) and its own arm through all three places
   a res is gated — hence `AreaResurrect(def)`, so the three cannot drift apart. The offer is still
   **per person**: a res is a prompt the corpse answers, so a field is N offers, not one group decision.
   ⚠ Its PvP flag is paid at EXECUTION, not at cast start like the single-target res — forced, because
   at cast start it has no target to be flagged for. Worth a ruling if it matters.
3. **`SkillLevel.PhysMpCostPct` / `MagicMpCostPct`.** Mana Blessing climbs 10 → 20% and Mana Strain
   100 → 200%, and both fields lived on the SkillDef, which has one of each — so **every rung would
   have silently applied rung 1's number**. Found by the DESCR reader the moment it was taught the
   metric, which is precisely what that reader is for.

### 🐛 CLARITY AND FORTITUDE WERE INERT — a bug found on the way past

Their entire payload is the `CcResistMagical/Physical` **fields** (the SkillEffect enum is full), so
their `Effect` is `None` — and the buff-apply arm in `ExecuteSkill` gated on `AnyBuff`. The cast landed,
charged the MP, announced itself and **applied nothing**. It never showed because nothing could learn
them until now. Fixed by adding `Category == Buff` to that gate, which is the same lesson
`IsAllyTargetable` had already learned; every future field-only buff gets it free. Mana Strain needed
the debuff twin of the same fix (`Category == Debuff` in the offensive-target test and the debuff arm).

### ⚠ SIX BUFF LADDERS GREW A MIDDLE RUNG

His file authored values that sit BETWEEN existing ones — M.Atk 28%, M.Def 23%, +3 accuracy, +3
evasion, 7 and 8% vampirism, 36/42/48 interrupt. A family's rung index IS its rank, so the only
monotonic way to hold a new middle value is to renumber everything above it.

🔑 **No value changed anywhere.** The Greater potions, the NPC buffer, the improved groups and the
Harmonies all still hand out exactly what they did — they just name a higher index. `BuffMAtk3` is
28% now and `BuffMAtk4` is the 32% top; **read a const's comment, never its number.** Nothing persists
a rung id (buffs die with the session), so no character carries a stale one.

⚠ `interrupt` kept a rung nobody authors (40) between his 36 and 42, because `Force and Ward` levels
3-4 hand it out and dropping it would have quietly retuned a group he has not re-authored. A ladder may
carry a rung no CSV names; it may not carry one that goes backwards.

**Shield Bless and Shield Harden are real ladders at last** — 5→30% over six rungs and 30→50% over
three — so the buffer's `Shield Bless and Harden` group finally names the TOP of each family instead
of a lone placeholder.

### What is NOT here

Five invented skills the discipline used to teach — **Blessing of Light, Devotion, Purify, Warding
Step and Soul Sap** — are on none of his rows and are no longer granted by anybody. Their DEFS survive
in the catalog (deleting one orphans every character who bought it). 🔴 That also settles the known
overlap: `Warding Step` was an invented 8-second root sitting beside his 30-second `Bind`.

`RegisterHealerMasteries()` is gone — it existed for one day, teaching the two masteries and Frenzy L2
while `RegisterLightbringer()` was still commented out. Both would have registered every rung twice.

### ✅ EVERY LADDER IN THE FILE NOW GOES UP — his four rulings, applied to BOTH sides

The first pass mirrored a dozen flat rungs and dipping prices verbatim and reported them. **He ruled on
all of them the same day**, so `healer 3rd.csv` and the code were corrected together and the file now
has **zero MP dips, zero SP dips, and zero repeated descriptions** (machine-checked, not eyeballed).

**His four rules, which are the durable part:**

1. **Resurrection SP = the BUFF ladder** — *"Resurrection sp should match the buffs of the same lvl"*.
   A res is priced like a blessing learned at the same character level (19k at 40 … 450k at 74), NOT
   like the far dearer combat band ladder an attack or a heal runs on. His draft was already on it at
   every rung but one: **56 read 81k where the buff price is 42k.** Restore Mana turns out to be on the
   same ladder (its @72 read 200k, repeating @70 — now 330k), and so is Resurrection Field.
2. **Antidote SP = the COMBAT band ladder** — *"as much as any other passive/active (except buffs and
   resurrect)"* — and **its cure rank runs exactly one band behind the Elf's Healer Blessing**:
   *"Elf learns tire 3 at 40 antidote tire 3 at 44... Elf tire 4 at 48 antidote t4 at 52"*.
   The Elf first reaches rank 3/4/5/6/7/8/9 at 40/48/56/60/64/68/72, so Antidote reaches each one rung
   later, at **44/52/58/62/66/70/74**. 🔑 That is the design in one line: the race that specialises in
   curing always gets there first, and the general-purpose cure catches up.
   ⚠ **That is SEVEN rungs, not the eight his draft had.** The level-64 row gave rank 7 at the *same*
   level the Elf gets it and duplicated the 66 row, so no value could satisfy the rule — **the row was
   removed** rather than given an invented number. Say so if that was not the intent.
3. **A buff's MP is the standard price at the level it is learned.** Serenity's *"New insert"* @40 read
   80 MP / 32k SP against every other 40-level buff's 60 / 19k.
4. 🔑 **THE DIAGNOSTIC, and it is worth keeping**: *"if the 40 lvl description is the same as 44 one then
   the description is wrong ... if the descr goes up then the mp is wrong — mp cost should match the
   descr and should go up."* So a repeated description means the DESCRIPTION is the defect; a rising
   description over a flat MP means the MP is.

**What rule 4 caught.** A rung counts as repeated only when **nothing** in it improved — a mastery rung
whose M.Atk is flat but whose regen climbs is fine, which is why Healer Weapon Mastery was left alone
and Healer Armor Mastery (whose 48/52 pair improved in *nothing*) was not.

|                                                    | was                             | now                                                                                           |
| -------------------------------------------------- | ------------------------------- | --------------------------------------------------------------------------------------------- |
| Gravity 66-74                                      | flat 23%                        | the +2 stride continues → **25/27/29/31/33%**                                                 |
| Armor Break 68-74                                  | flat 30% / 15%                  | **32/34/36/38%** P.Def, half that M.Def                                                       |
| Armor Break @56                                    | 18% / **10%** (duplicating @58) | **9%** — M.Def is exactly half P.Def at every other rung                                      |
| Holy Ray @52                                       | 52 (duplicating @48)            | **57**, continuing +5 and smoothing the +11 jump to 63                                        |
| Quick Great Heal @72                               | power 820 (duplicating @70)     | **840**                                                                                       |
| Mana Totem 64-72                                   | 14,15,16,17,18 with a flat pair | **15,16,17,18,19** — a clean +1 line, 10→20                                                   |
| Healer Armor Mastery @48                           | pDef 50 (duplicating @52)       | **47**, giving 39/44/47/50/53                                                                 |
| Antidote @74                                       | MP 42                           | **64**                                                                                        |
| Mana Ray @68                                       | SP 280k (repeating @66)         | **320k**, the band ladder                                                                     |
| Restore Mana @44                                   | DESCR 77 vs MP column 79        | **77** — the column moved to the text, per rule 4. The one lossy transfer in the game is gone |
| Meditation @68                                     | MP 52 (repeating @64)           | **57**                                                                                        |
| Great Heal / Healer Blessing @74                   | MP 117                          | **120**                                                                                       |
| Party Great Heal 72/74 · Ultimate Party Heal 72/74 | MP 228                          | **234 / 240**                                                                                 |
| Quick Great Heal @74 · Ultimate Heal @74           | MP 175 / 114                    | **180 / 120**                                                                                 |
| Ultimate Heal @72                                  | MP 114                          | **117**                                                                                       |
| Healing Totem · Mana Totem @74                     | MP 464                          | **476**                                                                                       |

🔑 **THREE IDENTITIES IN HIS OWN FILE MADE MOST OF THIS DETERMINATE RATHER THAN GUESSWORK**, and they
are worth knowing before anyone retunes these: **Party Great Heal MP = 2 × Great Heal MP** · **Ultimate
Heal MP = Great Heal MP, and Quick Great Heal POWER = Great Heal power** · **Armor Break M.Def =
P.Def ÷ 2**. Each holds at every rung and each broke in exactly the places above.

⚠ **Bind is the one skill whose description legitimately repeats** — a hold is 30 seconds at rung 1 and
at rung 14. What its ladder buys is the LEVEL CONTEST (`DebuffLandChance` reads the attacker's level)
and the price, which is how every CC ladder in the game works.

### The checker learned five things

Each was a real blind spot, and four of them produced confident wrong output first:
- **`36k` is 36000.** The 3rd-tier files write SP with a `k` suffix; the parser read every one as **0**
  and reported ~300 SP mismatches against perfectly correct code. A parser that reads what it does not
  understand as zero is worse than one that refuses.
- **A section banner is not a rung.** `,,,,,,,,,,,,,----40----` has the full column count.
- **A totem's "duration" is its LIFE** (`TotemLifeTicks`), not `DurationTicks`.
- **A reduction is authored positive and stored negative** — M.Def curses and Meditation's −90% P.Def.
- **The stat-swap passives are not CSV content**; they are bought with gold and his purge spared them.

Plus four new metrics it can now read: `mpcost`, `reagent` (a Skill Stone count is checkable data, not
noise), `shield pdef` above plain `pdef`, and `DebuffAtk` as **both** channels.


## 2026-08-20 — Mana Ray drains a SHARE OF THE TARGET'S POOL, not a magic-damage number

⚠ **ENGINE ONLY — the skill itself is still NOT authored.** There is no `mana_ray` `SkillDef` and no
class-table row; his `healer 3rd.csv` rungs (56-70) are still being written and were not touched here.
*"u can fix the code and w8 to finish the file"*. **No version bump, no APK** — nothing reachable
changed, because nothing carries `DamageToMp` yet.

### The model, and why the obvious one was wrong

`DamageToMp` shipped reusing the magic pipeline verbatim, which was his own earlier ruling (*"Same
formula; mRes; can fizzle; etc"*). Measured at level 74 against real geared Entities, that model
emptied a **fighter in 1.2 casts** while taking 5-6 on a caster. The cause is not defence: M.Def is
nearly identical across classes (697-782). It is that **MP pools differ 4.5×** — 696 on a fighter
against 2662/3158 on a nuker/healer — so any drain whose size ignores the pool is lopsided by
construction. Owner: *"a healer vs tank/fighter making the fighter and a tank in 2 cast to 0 mp .. and
they are sitting targets to your 80% mp"*.

Four models were measured (`tools/BalanceMatrix -- --mana-ray`, added here). Casts-to-zero at power 165:

| model                                | tank     | champion | nuker     | healer    |
| ------------------------------------ | -------- | -------- | --------- | --------- |
| A the magic pipeline (what shipped)  | **1.4×** | **1.3×** | 5.8×      | 6.9×      |
| B flat ÷ mRes                        | 5.1×     | 5.1×     | **21.1×** | **25.1×** |
| C flat × mRes ("mana punishment")    | **3.5×** | **3.5×** | 12.4×     | 14.8×     |
| **D share of the target's max MP** ✅ | 6.1×     | 6.1×     | 6.1×      | 6.1×      |

🔑 **Magic resistance cannot fix this, and that is measured, not argued.** He proposed hypothetical
20%/30% resistances to rescue model A; they moved the tank from 1.2 to **1.4** casts. Pushed to
`MagicResist`'s hard ±0.9 clamp (`Entity.cs`) — coefficient 1.90, the most the engine can express — a
fighter still falls in **2.0-2.3**. Model A would need ~4.2, which is impossible *and* would cut every
nuke landing on that fighter by 76%, because mRes is the same coefficient that divides real magic
damage. 🔑 His **C reversal bites the wrong way**: multiplying by resistance raises the fighter's
number too, and his is the small pool — 28% of a fighter's bar per cast against 8% of a mage's.

### What was built

- 🆕 **`StatCalculator.ManaDrain(targetMaxMp, power)`** — the drain is `power` PER MILLE of the
  target's own max MP, so his authored **145 = 14.5%** (*"ill make it to a max of 145 power to be less
  OP"*). Reading the power as per mille is what lets the CSV keep an ordinary `+145 Power` cell and
  keeps `SkillCsvSeed --check` and `Descr.cs` reading the same number the engine does.
- The magic block in `GameLoopService` **branches once**: a `DamageToMp` skill takes the pool share and
  skips `MagicDamageFM` and weapon variance entirely (it reads no weapon, and a predictable share is
  the point). Everything below the branch is deliberately still shared with an ordinary nuke — the
  fizzle ÷3, the magic crit, the interrupt contest, threat, the hide break — because his ruling was
  about the SIZE of the number, never about what a hit does. `FinalizeDamage` still runs, so his
  *"half effect on monsters"* (`PveDamageMult 0.5`) and the PvP matrix are unchanged and un-duplicated.
  ⚠ That also means magic-damage BONUSES still scale a drain; flagged to him as a consequence of
  keeping one path rather than a rule anyone chose.
- `SkillText` now states the SHARE (`Drains 14.5% of the target's MAXIMUM MP`) plus a line saying it
  ignores magic defence and resistance. A card showing the raw `145` next to a 3158-MP pool would read
  as 145 MP.
- `tools/BalanceMatrix` gained `--mana-ray <power> <level> [fighterRes] [casterRes]`, a `healer:` mode
  on `BuildPlayer` (Cleric = class 17, **wand** not staff) and a shared `Targets()` list. Its D column
  calls the shipped `ManaDrain`, not a copy of it.

**At 14.5% the result is 7.0 casts to zero every target** (100/cast off a fighter's 696, 457 off a
healer's 3158), ≈13.8 casts to strip a boss after the ×0.5.

### Still owed when his file lands

The `SkillDef` + class rows, and the **MP cost ×3** he ruled (*"i should tripple the mp cost"*): at his
authored 90 a full drain costs the healer 4% of his own bar, at 270 it costs a flat **59-60% against
every target** — the "strategy move, not a farming tool" the `NeverAuto` flag already encodes.
⚠ His ladder tops out at 145 **already at level 68** (56→120 … 68→145, 70→153, 72→157), so a literal
cap flattens 70/72/74 onto the 68 rung for 390k-650k SP. Raised with him; respacing is his call, and
`--check` cannot see a flat top — it only catches a ladder going down.

## 2026-08-20 — a skill has ONE MP price: the CSVs collapse to one column, the engine splits 20/80

⚠ **NO VERSION BUMP, and NO APK NEEDED.** No skill's total cost moved by a single point — the two CSV
columns always summed to the total the code already carried, and `--check` runs clean on all seven
compared files after the collapse, which is the proof. The client already showed the total and still
does; everything new here is server-side.

His ask: *"i sould like to be a one number .. players will see that its split at start and at finish ..
but for the whole skill theyll need the full mp"*, then *"i can sum the IG values and we just split it
in the code as 20/80 .. (looking at IG numbers they seem like a 20/80 split)"*.

### 1. `INIT MP` + `FINIT MP` → one `MP` column

Every `.csv` in `docs/data/classes_skills_csv/` (23 files) now carries the SUM in a single column. **Only those two cells were rewritten** — his instruction was explicit that a skill whose CSV
row disagrees with the code stays as authored: *"do not fix skills in the code from the csv if they
differ .. just colapce the two column into one"*. Nothing else in any file was touched, `buffer_auto
3rd.md` included (it is the rejected generated draft and is not authoritative).

🔑 **Why the ratio was never real.** His sheets split 20/80 on a heal and put the whole cost up front on
a physical strike, and the code books physical skills the other way round (`Strike` authored 20/0,
registered 0/20). Two spellings of one number, disagreeing for no design reason — which is exactly why
`--check` had to sum them before comparing anything.

### 2. The split moves into the engine, once, for everything

`SkillMath.InitialMpFraction = 0.20f` — 20% charged when the cast starts, the remaining 80% when it
lands. `SkillDef.InitialMpCost` and `SkillLevel.InitialMpCost` are **deleted** along with `InitialMpAt`
/ `FinishMpAt`; ~180 authored per-skill values went with them across six skill files, and `RungCost` in
`Skills.BuffLadders.cs` lost its `Init` field. Don't reinstate a per-skill split.

What the split still buys, and the only reason it exists: **an interrupted cast keeps the 20%** (the
price of trying) and is never charged the 80%.

### 3. The cast gate asks for the WHOLE price — and for the price THIS caster pays

Two bugs in one line. `HandleCastSkill` read `def.MpCostAt(...)` raw: the total (right), but the
**authored** total, ignoring the caster's own MP-cost modifiers (wrong). Both charge points then applied
`MpCostFactor` — so a debuffed caster could start a cast the gate had approved and be refused halfway.

Everything now goes through one helper, `GameLoopService.EffectiveMpCost` = authored total ×
`MpCostFactor`, and `MpCostFactor` runs **0.2× to 3×** (the reduction is clamped to `[-2, +0.8]`), not
0.2×-1×. His worked example, which is now literally what happens:

| caster       | 100-MP skill costs | can cast at |
| ------------ | ------------------ | ----------- |
| plain        | 100                | 100 MP      |
| −20% MP cost | 80                 | 80 MP       |
| ×3 MP debuff | 300                | **300 MP**  |

The 20% up front is sliced off that effective number, and the finish charge is the **remainder** of it
(`effective − CastInitialMpPaid`) rather than a second independently-rounded 80% — so the two halves
always sum to exactly what the player was quoted, and a buff expiring mid-cast re-prices the balance
instead of double-charging. The toggle path (`HandleToggle`) and auto-hunt's MP budget read the same
helper.

### 4. Tools

`SkillCsvSeed` writes the one-column header and `def.MpCostAt`; `--check` reads column 9 as the total
(it summed 9+10 before); `BalanceMatrix` reads `MpCostAt` instead of adding the two halves.

---

## 2026-08-20 — the caster weapon rule collapses to ONE penalty, and the healer takes his own masteries

⚠ **NO VERSION BUMP.** 🔴 **The buffer's two new learn lines NEED A NEW APK** — the Learn tab is built
from the compiled `ClassSkills` the client ships with, not pushed by the server. Everything else here is
server-side and lands on the running server immediately.

### 1. A sword or mace costs a caster NOTHING now

His ruling: *"magic weapon only is removed and it become a sword/blunt x1/x1 (no penalty) (bow/duals
stay)"*. `Spellcaster Mastery` used to have three weapon cases; it has two.

| held                | before                            | now                   |
| ------------------- | --------------------------------- | --------------------- |
| wand / staff        | cast ×1, M.Atk ×1                 | unchanged             |
| **sword / mace**    | cast ×1, **M.Atk ×0.6**           | **cast ×1, M.Atk ×1** |
| bow / dagger / bare | cast ×0.5, M.Atk ×0.5, ×25 fizzle | unchanged             |

🔑 **Why it was wrong twice over:** the item catalogue ALREADY prices this choice — a sword carries a low
authored M.Atk and rolls no cast-speed attribute — so the class rule was charging a second time for a
trade the player had already paid. `Entity.NonMagicWeaponMagicMult` is deleted; don't reinstate it as a
per-item `MAtkFactor` either, which is the shape it had before 2026-08-07.

**Effect: +67% magic damage** for any caster holding a non-magic sword/blunt (`×0.6 → ×1`).

### 2. Divine Focus is deleted

*"Remove the Divine Focus of cleric/buffers/healers -> if a healer wants to use sword so be it, swords
have lower mAtk and no cast speed atri."* The heal-output penalty for holding no wand (×0.5, ×0.75 for a
Warchanter) is gone with its skill, its id and `Entity.HealOutputMult`. A heal is now power →
`HealPowerFlat/Mod` → the target's `HealReceived*`, with **no weapon gate anywhere in the chain**.

⚠ It was AUTO-GRANTED, so every healer alive carries the id — `AutoLearnCoreSkills` strips it on login.
That is the only place the `divine_focus` literal survives.

### 3. The healer's masteries split off from the cleric's (his `healer 3rd.csv`)

At 40 the two shared cleric masteries fork. **The healer replaces them; the buffer continues them.**

|        | healer (Lightbringer)                                                                      | buffer (Warchanter)                        |
| ------ | ------------------------------------------------------------------------------------------ | ------------------------------------------ |
| weapon | **Healer Weapon Mastery** — *"Removed the P.Atk bonus and made it only for magic weapons"* | Spell Mastery rung 5 (keeps the +18 P.Atk) |
| armor  | **Healer Armor Mastery** — *"Removed the Light Armor bonus"*, robe only                    | Armor Mastery rung 5 (keeps the light row) |

Both new skills carry his full 9-rung ladder (@40/44/48/52/56/58/60/62/64) and his SP prices
(36k…190k). A pure healer's kit is now a **wand and a robe**, and that is expressed as a bonus he
forgoes — not a penalty he carries, which is what Divine Focus was.

🔑 **THE GATE IS THE TYPE `Blunt`, not a magic-weapon flag.** A `MagicWeaponOnly` flag (keyed on
`ItemDef.IsMagicWeapon`) was built first and removed the same day on his ruling: *"the healers weapon
mastery can say blunt .. as both wand/staff are blunts .. that way a sword wont work on a healer and
cariing a normal blunt is lower matk and no attri .. so its a choice"*. Blunt leaves a plain mace
**working** — it just carries less M.Atk and rolls no caster attributes, so the healer trades power for
whatever the mace gives him. The flag was the stricter rule, and strictness is a wall, not a choice.

⚠ **Built from a 3rd-tier CSV, which is a file he has not finished.** His note: *"you shouldn't have
built anything from the 3rd csvs as they are not finished … later we will recheck them and fix them if
any changes occur in the 64- lvls."* Left in place, to be re-checked against the finished file.

⚠ `Spell Mastery` rung 5 also had its reuse corrected **10% → 15%**: both his level-40 rows say 15% and
the code had carried 10% since the rung was written. Power drift like this is invisible to
`SkillCsvSeed --check`, which does not read the `DESCR` column.

### 4. The fizzle is a CHAIN now, so the bow penalty can be bought back

His question: *"make the opposite of a bow/dagger fizzle … fizzleFormula x 25(penalty) x 1/(25 x bonus
x bonus)"*. **Yes — that works, and it now exists.** `StatCalculator.MagicWeaponFailMod(bool)` (which
could only ever say "penalised or not") is replaced by **`Entity.MagicFailSelfMult`**, a running product:

```
fail% = round( 1.3^(defLvl−atkLvl) × defenderMod × MagicFailSelfMult ) + flatMagicEvasion
```

The untrained weapon multiplies **×25** in; a passive carrying `PassiveEffect.MagicFailSelfMult: 0.04f`
divides it back out **exactly**, because both meet inside the same `round()` — parity stays at 1% and a
+10 level gap stays at 14%, identical to a wand. Multiple entries compose, both directions.

⚠ **`0` means "not in the chain", never ×0.** `default(PassiveEffect)` is the inert value every unset
mastery slot hands out; a field that meant ×0 when blank would silence every spell in the game.

⚠ **The rounding floor is real**: at level parity the base is 1 point, so a bonus milder than the full
negation (say ×0.9) rounds back to 1% and does nothing. Sub-1 multipliers only bite across a level gap.
Nothing authors this field yet — it is an engine, like the proc triggers.

### 5. `--check` reads the DESCR column — the one place the VALUES live

*"make it so a DESCR is read .. as it contains the values for the skills."* Every other column is a
number in its own cell and has been compared for days; **power, +M.Atk, mpReg x1.2, -15% reuse live in
free text and nothing verified them.** That is how `Spell Mastery` rung 5 sat on 10% reuse while both
authored rows said 15%, and how the bolt powers drifted 25-30% over his sheet.

`tools/SkillCsvSeed/Descr.cs` now segments each DESCR cell, binds every number to a stat, resolves it
against what the game actually carries (PassiveEffect · StatMods · weapon/armor mastery rows · the
skill's power · buff magnitudes, **following the buff ladder into the child rung**) and compares.

🔑 **It reports what it could NOT read.** Every number must be consumed by a token or matched by an
ignore rule (`20 min`, `rank 3`, `2h sword`); anything left prints as `UNREAD` under `-v`. A parser that
quietly skips what it doesn't understand is worse than none — it reports "no discrepancies" over an
unchecked file. **Coverage today: every value in all seven files is verified or explained.**

**It found a real defect on the day it was written** — `Frenzy` rung 1 — which he then re-authored
outright (see below). It also caught his `healer 3rd` @44 reuse reading 10% between 15% and 20%, and its
own inability to read a **Unicode minus**: he writes "−5%" with U+2212, so the sign silently vanished
and a penalty compared as a bonus.

🔑 **A LADDER ONLY GOES UP.** His rule, given with that typo: *"the stats should go up not down - if
they got down i made a mistake or swaped two levels"*. `--check` now walks each skill's rungs and prints
a **`LADDER DIP`** when an authored stat falls as the levels rise. ⚠ Reported and counted **separately**:
everything else the tool prints is a code defect measured against an authoritative CSV, while this one
says the CSV itself looks wrong — conflating them would break the rule the file header states.

### 6. Frenzy, re-authored: the IG √ conversion nobody applied

His numbers, both rungs verbatim:

| rung   | learned by          | Max HP/MP | P.Atk / M.Atk | atk & cast speed | move | evasion |
| ------ | ------------------- | --------- | ------------- | ---------------- | ---- | ------- |
| **L1** | cleric @35          | **−7%**   | **+5%**       | +5%              | +5   | −5      |
| **L2** | healer + buffer @52 | **−10%**  | **+8%**       | +8%              | +8   | −8      |

🔑 **WHY 8% AND NOT IG'S 16%:** IG's Frenzy reads +16% M.Atk / +8% P.Atk, but IG applies magic buffs
under a **√** — √1.16 = ×1.077 — so its real effect is ~7.7%. Ours stores the **honest** percent
(`Entity` squares `BuffMagAtk` precisely so the stored number is what lands), which is why 8% here
equals 16% there. His words: *"we fixed the Force (IG-75% == Our-32%) and forgot the frenzy"*.
⚠ **Copying an IG percentage straight into a magic buff doubles it.** Rung 1 had been sitting at
−30% Max HP/MP for a +5% return — a trade-off nobody would take.

⚠ **Rungs 3-7 are OURS and are now out of line**: rung 3 gives +6% for −22% Max HP/MP, strictly worse
than rung 2's +8% for −10%. A buffer learns rung 3 at 62, and `ApplyBuff` replaces on rank ≥ rank, so
**the weaker buff would evict the stronger one he had at 52**. Left un-invented (they are also the three
Frenzy SCROLLS, ranks 2/4/6) — his call whether to retune them or drop the 62/64 grants until L3+ is
authored.

⚠ Two divergences are **`RULED`**, not defects — a later ruling in chat beats the CSV, and the tool
prints the reason: Shield Mastery's `ShieldDefPct` ×5 (2026-08-12, paired with the item cut) and Evasion
Boost's magic evasion as flat points (2026-08-11, `62e`). ⚠ Add an entry only for a decision he actually
made; the value of this pass is that an *unexplained* difference is loud.

## 2026-08-19 — Provoke is called Taunt, and Lure moves to the dual 3rd at 40

⚠ **NO VERSION BUMP**, but 🔴 **BOTH CHANGES NEED A NEW APK.** A display name and a class-skill TABLE
are read from the compiled `Game.Shared` the client ships with, not pushed by the server — so on the
current APK the skill still reads "Provoke" and a level-20 rogue still sees Lure in his Learn tab.
Nothing to do now (the APK is deliberately parked until the healer is out); this is the note that says
why the phone disagrees with the server until then.

### `Provoke` → **`Taunt`** (display name only)

His call: *"Rename to taunt."* — closing the last mismatch from this morning's CSV alignment, in the
direction that keeps HIS name.

🔑 **The skill ID stays `provoke`.** Ids are append-only and are what a saved skill bar, a hotkey and
every persisted character hold; renaming the id would silently empty bars on the next login. So the
code and its comments still say Provoke, while the screen and `tank 2nd.csv` say Taunt. One line in
`Skills.Fighter.cs`; the four CSV rows follow it back to his spelling.

### `Lure` moves from the 2nd-class rogue to the **melee/dual 3rd, at 40**

His call: *"move lure from rogue to dual 3rd (@40) I'll author it to the corresponding lvls as I'm
making the file. No lure for lvl 29 and below .. It's a skill that need the prawl effect."*

The reasoning is the pairing: a pull is only survivable if you can leave the camp afterwards, and
**Prowl is a 40+ melee-rogue skill**. Lure at 20 handed out one half of a two-part tactic twenty levels
before the other — and handed it to future ARCHERS too, since the 2nd-class rogue block covers both
weapons up to 40.

- **Removed** from `ClassSkillTables.Common.cs` (the rogue's 20/28/36 rungs) and from `rogue 2nd.csv`.
- **Registered** in `ClassSkillTables.Third.RegisterHideKit()` next to Prowl — Nullblade, Venomweaver,
  Phantom, all three races, **level 1 only**.
- ⚠ **Levels 2-3 (reach 400 and 600) are now UNREACHABLE**, and that is the intended state: the ladder
  is his to place as he writes `dual 3rd.csv`. **No row was added to that file** — it is his to author
  (*"I'll author it to the corresponding lvls"*), which is why the CSV mirror rule is not being broken
  here. This is the one thing to re-check when `dual 3rd.csv` lands.

`--check` still reports **"No discrepancies"** on all seven 1st/2nd files; `Game.sln` and the Unity
`Assembly-CSharp.csproj` both build clean.

## 2026-08-19 — the 1st/2nd-tier CSVs are made to match the game, and `--check` runs clean

⚠ **NO CODE CHANGE AT ALL.** Documentation only: three CSV files corrected. No skill was retuned, no
class table touched, nothing rebuilt.

🔑 **HIS RULING, AND IT REVERSES THE USUAL DIRECTION FOR THESE FILES ONLY**: *"1st and 2nd are
currently in the game based out of csv files … should not have much differences … Mage + cleric +
nuker 2nd should be 1:1 … while fighter files should be made to match what's in game."* So for the
**fighter side of tiers 1-2** the GAME is the reference and the CSV is corrected to it — the opposite
of the standing rule (`docs/data/classes_skills_csv/` is authoritative). The reason is forward-looking:
he is about to author the 3rd/4th kits, and a reference sheet that disagrees with the running game is
worse than no sheet. **⚠ This is NOT a general licence to edit CSVs to match code.** It applies to
these three files, on his word, on this date. Everywhere else the CSV still wins.

⚠ **3rd/4th were deliberately NOT touched** (*"3rd and 4th u should skip and don't touch for now"*).

### `tank 2nd.csv`

- `Defencive Wall` → **`Defensive Wall`** (spelling).
- 🔑 **`Taunt` → `Provoke`** — the same skill under two names, which is why `--check` reported it as
  *"NOT REGISTERED"* + *"NOT IN THE CSV"* rather than as a field mismatch, and therefore **never
  compared its numbers**. With the name aligned, four columns turned out to be wrong: range **400 →
  600**, cast **1 → 0**, cooldown **10 → 6**, duration **0 → 3** (the hard-commit lock), and MP
  **10 flat → 15/18/22/26**. His taunt POWER ladder (4500/5000/5500/6000) and SP were already right —
  the code took those from this file in the first place.
- `Shield Stun` and `Stay!` had **duration 0** in the column while their own DESCR said 9s and 15s.
  Now 9 and 15, matching the code.

### `warrior 2nd.csv`

- `Two Handed Mastery` → **`Two-Hand Mastery`**.
- 🔑 **`Strike` → `Smash`** — same story as Taunt/Provoke. The five rows already carried Smash's exact
  powers (105/143/191/251/326), MP and SP, so this was purely a name, but it too was slipping past the
  numeric comparison. Two things the rows did not say and now do: `REPLACES` is **`[Strike Stab Shot]`**
  (a warrior loses the dagger and bow lines), and the weapon gate is **2H** sword/blunt, not any
  sword/blunt.

### `rogue 2nd.csv`

- Five names aligned: `Rogue Armor Mastery` → **`Armor Mastery`**, `Rogue Weapon Mastery` →
  **`Weapon Mastery`** (the game shows the plain names; the `[Armor Mastery]`/`[Weapon Mastery]` in
  REPLACES still points at the BASE fighter skill, which is a different id), `Stab` → **`Piercing
  Stab`** `[Stab Strike]`, `Shot` → **`Precise Shot`** `[Shot Strike]`, `Bow Expretise` →
  **`Bow Expertise`**.
- `Sprint` cast **0.1 → 0.2**.
- 🆕 **`Lure` added at 20/28/36** — it was in the game (BL-70) and in no CSV. Its ladder is REACH:
  200/400/600, MP 12/16/20, SP 3400/12000/40000, agro power 500 flat, monsters only.

### The one number left alone

⚠ `Precise Shot` rung 4 (@32) costs **34 MP** where rung 3 costs 53 and rung 5 costs 67. The code and
the CSV agree, so it is not drift — but it reads like a transposed typo in the original authoring
(43?). Left exactly as authored; his call.

**`dotnet run --project tools/SkillCsvSeed -- --check` now reports "No discrepancies"** across all
seven 1st/2nd files. The only remaining lines are ⚪ AUTO-GRANTED notes (Precision, Evasion Mastery,
Anti-Magic, Spellcaster Mastery, Magic Bolt L1), which are the SP-0 rows and not defects.

## 2026-08-19 — mpWhenRestored becomes a PERCENT, and mana-over-time joins one pipe

⚠ **NO VERSION BUMP.** Server-side numbers only.

His ruling: *"I should reauthor bonus restore to be a % rather than flat bonus … so it works like the
heal hot … a 100 heal/s with 30% increase will heal 130/s … 120+80 = 200 … 125x1.6 = 200 … And the
mana over time will go trough the same pipe as other mana restores."*

### The stat

`StatMods.RestoreMpBonus` (flat `+N` MP per restore) → **`StatMods.RestoreMpPct`** (a fraction), and
`Entity.RestoreMpBonus` (int) → **`Entity.RestoreMpMod`** (float, starts at ×1) — the exact twin of
`HealReceivedMod`. `RestoreMpOne` multiplies instead of adding.

🔑 **The `payRestoreBonus` special case from this morning is DELETED.** It only existed because a flat
per-event bonus had to be suppressed on ticks; a percent scales with whatever landed, so a cast and a
totem pulse go through **one pipe**, which is what he asked for. A robed nuker in a mana totem now gets
his ×1.6 on every pulse — his own worked example (10/s → 16/s, ~30/s → 48/s at the top).

### The ladder — the old flat × 0.75

His anchor is the top rung: `120 + 80 = 200` and `125 × 1.6 = 200`, so **+80 flat → +60%**, and 60/80
= 0.75 applied to the whole ladder keeps its shape ("keep the linear feel"):

| rung @ char   | 20   | 25   | 30   | 35   | 40   | 50   | 60   | 70       |
| ------------- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | -------- |
| was (flat MP) | +25  | +30  | +35  | +40  | +50  | +60  | +70  | +80      |
| now (percent) | +19% | +23% | +26% | +30% | +38% | +45% | +53% | **+60%** |

### 🔴 MEASURED — the conversion is anchored at 80 and costs a LOT below it

`BalanceMatrix` E3, delivered MP per Restore Spirit cast:

| lvl | base | ×mast | now     | flat before | Δ                                                        |
| --- | ---- | ----- | ------- | ----------- | -------------------------------------------------------- |
| 25  | 20   | 1.23  | **25**  | 50          | **−50%**                                                 |
| 36  | 20   | 1.30  | **26**  | 60          | **−57%**                                                 |
| 44  | 45   | 1.38  | **62**  | 95          | **−35%**                                                 |
| 52  | 65   | 1.45  | **94**  | 125         | **−25%**                                                 |
| 60  | 85   | 1.53  | **130** | 155         | −16%                                                     |
| 70  | 105  | 1.60  | **168** | 185         | −9%                                                      |
| 80  | 120  | 1.60  | **192** | 200         | **−4%** ← his anchor, and *"nothing is lost"* holds here |

That is inherent to the change, not a tuning slip: a flat bonus is worth relatively MORE the smaller
the base, so +25 on a 20 MP restore was more than doubling it and no sane percent can match that.
`nukes/cast` (how many nukes one cast pays for, designed to clear 1.0) now reads **0.95 at level 44**
where it used to clear comfortably, and `MP/HP` fell from an authored 1.18 → 1.00 curve to 0.38 → 0.96.

🔑 **He already called this** — *"so i need to increase the restore spirit and do a % based robe
mastery"*. The base is his to raise (*"I'll reautor it later as I do the mage"*); to hold the OLD
delivered numbers a level-25 Restore Spirit would need ~37 MP instead of 20, and that row is his
authored CSV, so it is not ours to move.

### CSVs updated as reference (his request)

- **`nuker 2nd.csv`** — the four authored rungs now read `mpWhenRestored +19%/+23%/+26%/+30%`, and
  Restore Spirit's line says *multiplied by* rather than *plus*.
- **`nuker 3rd.csv`** — had **no** robe-mastery or Restore Spirit rows at all. Added what the code
  actually runs above 35: mastery rungs 5-8 @40/50/60/70 and Restore Spirit levels 2-10 @40 then every
  5 to 80. ⚠ **These rows are OURS**, not his authoring — the 40+ band has never had a CSV. They are
  there so he can see the current numbers while re-authoring the mage.

## 2026-08-19 — the totem grows a second channel, for his Mana Totem

⚠ **NO VERSION BUMP** and **no skill authored** — he is still writing `healer 3rd.csv` (*"when the
file is finished we will add the skills"*). This is the **ground** only: the engine now supports the
Mana Totem he wrote, so adding it later is a `SkillDef` row and nothing else.

### What was already there

The totem engine has existed since 2026-08-17 (the Ork healer's level-40 **Healing Totem**): placement
at the caster's feet, lifetime, pulse timer, `AlliesAroundPoint` (party-only, never the dead, never a
hidden member, measured from **the totem** so walking away from it costs you the pulse), owner-gone
cleanup, and the `PlacesTotem` arm of the targeting path that lets the cast succeed with nothing
selected. ⚠ A totem is **deliberately not an `Entity`** — see the comment on `TotemInstance`; it is
felt, not seen, until a client visual exists.

### What it could not do

**It pulsed HP and only HP** — `TickTotems` called `HealOne` unconditionally. His `Mana Totem` rows
(52/56/58/60/62, +10…+14 MP/s, 60s reuse, 30s life, 300 range) had no channel to run on.

- `TotemInstance.Effect` now snapshots the placing skill's own `SkillEffect`, and `TickTotems` pulses
  `Heal` → HP, `RestoreMp` → MP, both flags → both. 🔑 **A totem flavour is therefore an ordinary
  heal/restore skill with `PlacesTotem` set** — no second totem type, no new flag (the enum is full
  anyway), and `SkillText` reads the same flags so the skill card describes itself correctly.

### 🔴 Two defects fixed on the way in

**`RestoreMpBonus` would have been paid on every pulse.** The nuker robe mastery grants "MP restored
+25…+80", priced **per cast** — that is what makes someone else's Recharge worth taking. A totem
pulses 30 times, so a robed nuker standing in a Mana Totem authored to give **300 MP** would have
collected `30 × (12 + 80)` = **2760**, with the *bonus* running eight times the skill it is a bonus to.
`RestoreMpOne` gained a `payRestoreBonus` parameter; periodic restores pay the authored number.

**Totems stacked without limit.** `PlacesTotem` only ever appended. His Healing Totem authors a **25s
reuse on a 30s totem**, so every healer would have run a permanent overlapping stack and multiplied the
pulse by however many he could squeeze up. A recast now **moves** a totem: one per owner *per skill*,
so a Healing Totem and a Mana Totem still coexist.

### Still open — his calls, not mine

- **A totem heal generates no threat.** `HealOne` is called directly, so `AddSupportThreat` (BL-71,
  *"a heal is aggro, scaled by heads reached"*) never runs for a totem. Left alone deliberately: making
  a 30-pulse object generate aggro 30 times is a balance ruling, not a bug fix.
- **The built Healing Totem is off his current CSV** — code has Power 30 and 19/93 MP where rows 11/35/…
  now read +64/s and 42/196. Not touched: the file is unfinished, and `SkillCsvSeed --check` does not
  read the 3rd-tier CSVs yet, so nothing was silently validating it either.

## 2026-08-19 — magic crit gets headroom under the cap, and a damage stat of its own

⚠ **NO VERSION BUMP.** The wire is unchanged in the only way that matters: `StatsUpdate` gained one
trailing field (`MagicCritDamage`, default 2) which an older client simply ignores, and every number
here is rolled on the server. The new **"Magic crit dmg"** row in the client's stat window will not
appear until the next APK — the APK is knowingly stale.

His ruling: *"Magic crit rate should be lowered a bit … (still max 20% but one day if we want to
increase it no mage to be short on crit) … Magic crit dmg is default x2 … base critDmg x multiPliers
x (1 - debuffs) … we need to add mcritdmg as a editable stat and formula"*.

### The rate: the cap stops being the ceiling a mage already lives on

`StatCalculator.MagicCharacterCritBase` **50 → 40** on IG's 0-1000 scale (5% → 4%). One constant; the
witMod curve, the flat/mult chain and the 20% cap are all untouched.

The point is not the nerf, it is the **headroom**. At 50, a fully-kitted elf (WIT 30, ×2.00) computed
exactly 20% off Insight alone — he was pinned to the cap, so the 4th-class crit-rate buff being
authored would have bought him **nothing**, and raising the cap later would have bought him nothing
either. At 40 (measured, `BalanceMatrix`, and matching all four of his targets):

| WIT | who                         | bare     | ×2 Insight | ×4 (Insight + the 4th-class buff) |
| --- | --------------------------- | -------- | ---------- | --------------------------------- |
| 30  | elf mage + set +2 + swap +5 | **8.0%** | **16.0%**  | **32.0%** → capped at 20%         |
| 27  | human, same kit             | 6.8%     | 13.6%      | 27.2% → capped                    |
| 26  | ork, same kit               | 6.4%     | 12.8%      | 25.6% → capped                    |

His asks were "7-8% bare", "15-16% with Insight only", "31% at ×4", and "humans/orks over 20% at ×4".
All four hold (the ×4 elf figure is 32, not 31 — the rounder base 40 was preferred to a 38.75 that
would hit 31 exactly).

### The damage: a flat ×3 becomes a base ×2 with a knob

`StatCaps.MagicCritDamage` (flat ×3) is replaced by **`MagicCritDamageBase` = ×2** plus a
**`MagicCritDamageCap` = ×5** of headroom, and `StatCalculator.MagicCritMult` now takes the chain:

```
magic crit damage = 2.0 × ∏(1 + multipliers) × (1 − Σ debuffs),   clamped to [1, 5]
```

which is his formula verbatim. The rule that made it flat in the first place **still holds**: this
reads its own channel and never `CritDamageBonus`, so Ferocity and the crit-damage item attribute —
both authored for fighters — still pay a mage nothing.

Plumbed everywhere a stat is authored, so the 4th-class blessings need no engine work when the CSVs
land — only rows:

- **`SkillDef.MagicCritDamage` / `.MagicCritDamageDebuff`** (+ per-`SkillLevel` overrides and the
  `…At(level)` accessors), riding as **fields, not `SkillEffect` flags** — the flag enum has had zero
  bits left since `1L << 62`.
- **`PassiveEffect.MagicCritDamage`** and **`StatMods.MagicCritDamage`** (appended at the END of
  `StatMods`, per that file's own warning) for a kit passive, an armour set or a mastery.
- **`Entity.MagicCritDamageMult` / `.MagicCritDamageResist`**, folded in `RecomputeDerived` from all
  three sources — multipliers **compound**, debuffs **sum** — and read through the one getter
  `Entity.EffectiveMagicCritDamage`. His arithmetic falls out of it: the buffer's or healer's +30%
  gives ×2.6, and both together give **×2 × 1.3 × 1.3 = ×3.38**.
- **`/stat mcdmg <x>`** sets the FINISHED multiplier (`3.38` reproduces a fully-blessed 4th-class
  caster today, with no 4th class in the game), alongside the existing `mcrate`.
- The grade-penalty block shrinks **only the excess** (`1 + (mult−1)×penalty`), never the ×2 base.

### ⚠ What this costs the nuker right now

Both halves land before any of the buffs that give them back exist. An elf with Insight goes from
`0.80 + 0.20×3 = ×1.40` average damage to `0.84 + 0.16×2 = ×1.16` — **about −17% magic damage**, on
top of the bolt-power correction and 0.73.0's tougher creatures. The 4th-class kits are what restore
it (×4 rate to the cap, ×3.38 damage → `0.80 + 0.20×3.38 = ×1.48`, better than today ever was), and
they are blocked on his CSVs.

## 2026-08-19 — the debuff contest gets a level, and mobs get a CON worth having

⚠ **NO VERSION BUMP**, for the same reason as the entry below: nothing here touches the wire (the
target-inspect DTO already carried CON and SPT), and the APK is knowingly stale. No client rebuild is
needed — every number here is rolled on the server.

His question: *"mobs con with 175 .. wont get stunned at all? .. if con does nothing on the mobs, the
fighter mobs should have 40, tanks 45 and mages 35 .. and if we add a lvl curve like the magic fail one"*.

### 🔴 Two bugs found on the way in

**Mob SPT was ZERO, on every creature in the game.** `BuildMob` assigned CON, ATK, WIT and AGI and
simply never assigned SPT, so `DebuffLandChance(atk, 0)` came out at 1.0 and clamped to the **90%
ceiling**. Every root, hold and fear has been landing nine times in ten on everything since the contest
was written — **raid bosses included**, which have no `CcResist` either (that field only ever
accumulates from player gear). A boss has been perma-rootable this whole time.

**Every armour set's `Str` / `Int` line was inert.** They landed in `BonusStr` / `BonusInt`, and nothing
in the engine reads either — this game's one power stat is `Atk`. So `Ironforge +3 STR` did nothing at
all. **Fixed on his ruling** (*"input the armor stats to the effective stats"*) — see below.

### The level term rides the DEFENDER'S STAT, not the chance

```
land% = 0.5 + 0.5·(atk − def·L) / (atk + def·L),   clamped [10%, 90%]
L     = 1.1298 ^ (targetLevel − casterLevel)
```

`DebuffLandChance` is symmetric in attacker/defender, so **one** geometric factor puts the floor and the
ceiling at exactly ±20 levels and leaves parity at exactly ×1 — *"same level should be x1 (and pure stat
vs stat)"*. Scaling the CHANCE instead would flatten every build to the same number at a gap; scaling
the STAT keeps a debuff-built caster ahead of a nuker at every gap, which is the point.

`StatCaps.CcLevelBase` is **derived**, not authored: at the floor the defender's stat must be
`(1−CcLandMin)/CcLandMin` = 9× the attacker's, so the base is `9^(1/CcLevelFloorGap)`. Retune the reach
by moving `CcLevelFloorGap` alone; move the clamp and the curve follows it.

**The gap is 18** — *"match the fizzel 18 it is"*. `1.3^18` = 112 fail points, which is exactly where
`MagicFailMax` clamps, so **the level at which your spells stop landing and the level at which your
control stops landing are now the same level.**

| Δlvl        | −18 | −10   | −5    | **0**   | +5    | +10   | +13   | **+18** |
| ----------- | --- | ----- | ----- | ------- | ----- | ----- | ----- | ------- |
| equal stats | 90% | 77.2% | 64.8% | **50%** | 35.2% | 22.8% | 17.0% | **10%** |

### All THREE contest stats are now flat and authored by ROLE

CON was `15 + 2·level`, SPT was 30, **and ATK was `8 + 2·level`** — 168 at level 80 against a player CON
of ~43, which is a 4:1 ratio, which is the 90% ceiling, which is his *"ill get perma stunned"*. The
level growth on all three was silently doing the job of the level term the contest never had. It has a
level term now, so these three say only what the creature IS.

🔑 **They cost nothing else.** A mob's HP is `MobBaseStats.Hp(level)`, its MP is `MobBaseStats.Mp(level)`,
its P.Atk / M.Atk are `MobBaseStats.PAtk/MAtk(level)` and its regen is a fraction of its own pool — no
mob number reads a core stat. `EffectiveAtk` only reaches attack power on the *player* branch of
`RecomputeDerived`. So **flattening ATK does not weaken a creature's damage**; the one real side effect
is `PhysicalDoubleChance`, which mobs were pinning at its 25% cap and now sit at 10-13% on, and which
only fires on a `CanDouble` skill — no mob skill is one today.

"Normal ranges" is also the domain the rest of the math assumes: `PAtkStatReference` is 40 and
`PhysicalDoubleChance` caps at 60, so a mob at 168 was outside the reach of its own formulas.

| role          | ATK    | its stun on a fighter / a mage          |
| ------------- | ------ | --------------------------------------- |
| Melee, Archer | 40     | 48.2% / 59.7%                           |
| Mage          | 45     | 51.1% / 62.5% (slow: 64.3% / 53.6%)     |
| **BOSS** (×2) | **80** | **65.0% / 74.8%** (slow: 76.2% / 67.2%) |

A boss's Devastating Slam lands at 65% on a fighter — down from the ~80% the runaway curve gave it, and
restored to a real threat by the rank multiplier rather than by the level term.

| role            | CON    | SPT    | ATK    | stun/bleed *on* it | root/hold *on* it |
| --------------- | ------ | ------ | ------ | ------------------ | ----------------- |
| Melee (fighter) | 45     | 38     | 40     | 47.1%              | 51.3%             |
| Archer          | 43     | 40     | 40     | 48.2%              | 50.0%             |
| Mage            | 40     | **58** | **45** | 50.0%              | 40.8%             |
| tank            | **50** | 40     | 40     | 44.4%              | 50.0%             |

(vs a level-matched ATK 40 attacker.) The lean is his own defensive rule turned around — a fighter
shrugs off stuns and eats holds, a caster is the reverse — and the mage leans highest on ATK because a
caster is the creature that debuffs.

Was `15 + 2·level` / 30 / `8 + 2·level`.

**A TANK is not a `MobRole`.** Role says how a creature *fights* and a tank fights melee; it is authored
per template with the new `MobMod.Con` / `MobMod.Spt` (0 = take the role default), in the same place its
P.Def passive already lives. The inspect window prints them as *"Stun/bleed resistance high"*, not as a
raw number, and only when a template overrides its role.

### Ranks: one multiplier, both directions

**`StatCaps.CcRankMult` scales all THREE stats — elite ×1.33, boss ×2** (*"elites can get x1.33
atk/con/spt stats increase so it will give them more resists/chance .. bosses can get x2"*).

🔑 **It multiplies the OFFENSIVE stat as well as the two defences**, which is what makes a rank read as a
*bigger* creature rather than merely a tougher one: it is harder to hold **and** lands its own control
harder, off one number.

| at parity, ATK 40 vs a melee | normal | elite ×1.33 | boss ×2   |
| ---------------------------- | ------ | ----------- | --------- |
| your bleed lands             | 47.1%  | 40.0%       | **30.8%** |
| its stun lands on a fighter  | 48.2%  | 55.2%       | **65.0%** |

- **Boss** takes the ×2 **and** a hard **zero** for control — stun, root, fear and slow never land, at
  any stat, at any level. The two are not alternatives. Everything else it is merely very resistant to,
  which is his point exactly: *"even at 10% u still can debuff a boss but strategicly to not waste mp
  that can be used to taunt/heal"*. A bleed on a boss is 30.8% at parity and hits the 10% floor by
  Δ+13 — never a lockout, just a bad trade, which is a decision rather than a wall.
  New mask `SkillEffect.ControlCc`.
- The affliction half is untouched — bleeds, poisons and venoms land, tick, and carry their stat
  debuffs, which is his allow-list exactly (*"only dot/bleeds dmg/def mp debuffs"*).
- ⚠ **SLOW counts as control.** He named stun/root/fear/confuse and allow-listed dot/dmg/def/mp; slow is
  on neither list, so it fell on the blocked side. One identifier to reverse. (Confusion has no flag
  yet; when it gets one it belongs in that mask.)
- ⚠ A DoT carrying a slow as a **rider** (`Rupture = Bleed | Slow`) still lands whole on a boss — the
  test is "purely control", because a magnitude-level slow is not the perma-lock the rule guards
  against and splitting one buff's magnitudes apart is not worth the machinery.

### 🔴 The attacker's level is the RUNG'S, not the caster's

*"it should be difference enemy lvl and skill learned lvl .. not casters."* His case: a hold whose every
rung is identical — same 30s, nothing else on the sheet — so with the caster's level driving the
contest, *"casting it lvl 1 (@40) or lvl 10 (@74) when character is lvl 75 wont do nothing of a
difference"*. Reading the RUNG makes the ladder the whole point: at 75 the @74 rung lands ~48% and the
@40 rung sits on the floor. And an old rung decays on its own — *"if im lvl 80 and cast lvl 74 debuff it
should be weaker than a 80 lvl debuff"*.

🔑 **Same rule and same fallback as `BL-71`'s buff threat**, which already prices a buff on the level its
class learns it at for exactly this reason. Both now share one helper, `GameLoopService.RungLevel`. A
skill no class list owns — a mob spell, a scroll — has no rung level, and only then does the caster's
own stand in (so a boss's slam still fires at its own level).

⚠ **This expires every CC skill in the game today, and he took that knowingly.** All five learnable ones
are single-rung; the floor is Δ+18, so:

| skill            | rungs | learn | its top rung floors at |
| ---------------- | ----- | ----- | ---------------------- |
| Shield Stun      | 1     | 28    | target lvl **46+**     |
| Stay!            | 1     | 36    | target lvl **54+**     |
| Frost Bind       | 1     | 40    | target lvl **58+**     |
| Entangling Roots | 1     | 40    | target lvl **58+**     |
| Creeping Frost   | 1     | 44    | target lvl **62+**     |

He chose this over an auto-switch, because the ladders are the next authoring job. **The fix is a CSV,
not code**: give a skill more rungs and it stops expiring, with nothing to change here.

🔑 And nine more CC skills — Bind, Warding Step, Envenom, Hamstring, Rupture, Shield Bash, Snare Trap,
Terrifying Roar, Toxic Sting — **are in the catalog but learnable by nobody**. `RegisterLightbringer()`
and `RegisterWarchanter()` are still commented out from the 40+ purge, so his healer's own hold does not
exist as a learnable skill yet: the rung rule has nothing to break there. BalanceMatrix prints both
lists, generated from the class tables, so they re-measure themselves as the CSVs land.

### Gear and swaps now count — and armour power finally lands at all

The contest read `AtkStat` and `Con` — **raw** base stats, so a level-20 and a level-85 character landed
debuffs with the same number and neither gear nor the level-40 stat swaps did anything. It now reads
`EffectiveAtk` / `EffectiveCon` / `EffectiveSpt` (new `EffectiveCon`, mirroring what Max HP already did
inline). Swaps are ±5, sets ±3, so the attacker side moves ~34-48 rather than exploding.

🔴 **And armour `Str` / `Int` now fold into ATK** (*"input the armor stats to the effective stats"*).
This engine has ONE power stat — ATK, which *is* STR for a fighter and INT for a mage
(`GetBaseStats`) — so the gear CSVs' two names are the same stat, and both landed in fields nothing
read. All three (`Str`, `Int`, the new `StatMods.Atk`) now sum into `BonusAtk`. A set never carries more
than one, so summing needs no per-class branch.

⚠ **This is a real damage change, not bookkeeping** — `EffectiveAtk` multiplies the weapon in
`PhysicalAttackPower` and `MagicAttackStatScaled`. Measured on BalanceMatrix, best-gear, before → after:

|                  | before | after     |         |
| ---------------- | ------ | --------- | ------- |
| tank TTK @52     | 10.4s  | **9.7s**  | −7%     |
| champion TTK @52 | 8.0s   | **7.5s**  | −6%     |
| nuker M.Atk @85  | 1690   | **1814**  | +7.3%   |
| nuker TTK @85    | 27.7s  | **26.0s** | −6%     |
| rogue TTK @52    | 5.8s   | **5.9s**  | **+2%** |

🔑 **The rogue getting *worse* is the sheets working.** `Nightleaf 52` authors `Str: -1` — the light set
trades a point of power for its AGI — and now that STR lands, the trade is real in both directions. Every
negative in the gear CSVs started counting on the same line as every positive.

`tools/BalanceMatrix` prints the whole thing as a new **CONTESTED DEBUFFS** section, next to MAGIC
LANDING.

## 2026-08-19 (later) — the bolts read his sheet, and four healer mechanics get their engine

⚠ **NO VERSION BUMP, deliberately.** `GameConstants.GameVersion` gates the client/server handshake, and
the APK is knowingly stale (*"dont build apk untill the healer atleast is out"*) — bumping it would lock
the phone out of a server it can otherwise talk to. Nothing here changes the wire.
⚠ **The client's skill CARDS will read the OLD bolt powers** until an APK is built: `SkillCatalog` is
compiled into `Game.Shared.dll` and shipped inside the app. The SERVER deals the new numbers regardless,
so the cards are wrong, not the damage.

### The nuker's bolt power now matches `nuker 2nd.csv` and `mage 1st.csv` (his call: *"fix the nuker bolt power"*)

The four authored rungs were **~25-30% above his own sheet** and nothing caught it: `SkillCsvSeed --check`
compares learn level, range, cast, cooldown, duration, MP and SP — **not power**, which lives in the
free-text `DESCR` column. This is the drift that gap was always going to produce.

| skill                   | his 20/25/30/35 | code carried | continued to (80) | was |
| ----------------------- | --------------- | ------------ | ----------------- | --- |
| Elemental Bolt          | 26/32/38/44     | 37/44/50/57  | **98**            | 116 |
| Vampiric Bolt (+21 @14) | 26/32/38/44     | 37/44/50/57  | **98**            | 116 |
| Quick Bolt              | 21/26/30/36     | 30/35/40/46  | **81**            | 93  |
| Magic Bolt @7 / @14     | 15 / 21         | 17 / 24      | —                 | —   |

🔑 **His four points are exactly linear**, so rungs 5-13 are not invented — they are his own line continued:
Elemental/Vampiric `26 + 1.2 per character level`, Quick Bolt `21 + 1.0`, which holds Quick Bolt at ~81% of
Elemental at every rung. The ladder had been anchored at "power 108 @ level 74", an IG top-nuke reading that
was **ours**; where his band and our anchor disagreed, the band wins. The MP column had already been given
this exact treatment on 2026-08-19 morning — power was simply missed.

**Measured, `BalanceMatrix`** (nuke damage / casts-to-kill vs a same-level normal): 20 → 602/0.6 becomes
**423/0.9**; 52 → 710/3.1 becomes **579/3.8**; 85 → 886/6.6 becomes **749/7.8**. The nuker's MP economy pays
for it too — kills before out-of-mana at level 20 fall **22 → 16**, at 52 **24 → 20**. Full-character farm
hours at S move 603h → **618h**.
🔴 **Worth his eye: this is a 16-30% nuker cut landing directly on top of 0.73.0's ~3× tougher creatures.**
Applied as authored, on his instruction; flagged, not softened.

### Engine prep for four skills on his in-progress healer file — *no healer content built*

He is still writing `healer 3rd.csv` (*"dont build/retune healer .. until im finished there can be more
changes"*), so this is the machinery only. **Two of the four needed nothing at all:**

- **Mana Blessing** (−10% physical / −5% magic MP cost) — `SkillDef.PhysMpCostPct` / `MagicMpCostPct`
  already run end to end, buff → `Entity.PhysMpCostReduction` → charged at cast. Authoring only.
- **The four-way Great Might / Great Bulwark override** — *"3 lvls of the same key ... they will override
  eachother and cannot be buffed lvl-down"* — is what `ApplyBuff` already does for one shared `BuffKey`
  with ranks 1/2/3: a lower rank is refused outright, an equal rank keeps whichever runs longer (so a
  fresh cast always swaps), and single and group collide because they are literally the same key.

**Two needed building:**

- 🆕 **`SkillDef.DamageToMp` — mana damage (Mana Ray).** The magic pipeline's own number is subtracted
  from MP instead of HP: same formula, same M.Def divisor, same magic-resist coefficient, same fizzle,
  same magic crit, exactly his *"same formula; mRes; can fizzle; etc"*. His *"half effect on monsters"* is
  the **existing** `PveDamageMult: 0.5f`, not a second knob — two knobs for one number drift apart. It
  runs through the ordinary `ApplyDamage` (one `toMp` flag, not a parallel method) so threat, the combat
  timer, PvP flagging, the hide break and the interrupt contest cannot fall out of step; absorb shields,
  the mana shield (which would divert mana damage into mana) and the lethal save are skipped, spell-vamp
  is blocked off it, and MP floors at 0 so it can never kill.
- 🆕 **`SkillDef.EndsOnDamageTaken` — Meditation.** Checked in `ApplyDamage`, the one choke point every
  source of damage passes, so a DoT tick, an AoE, a reflect and a swing all end it with no per-source
  code. ⚠ Not a hide: it does **not** break on the owner's own actions — his words are "canceled on dmg
  taken", and whether a meditating healer may cast is his to rule.
- 🔧 **A FLAT per-second regen buff now works at all.** Meditation's *"+30/s"* was silently reading as
  **zero**: `Regenerate` accumulated only the `Percent` half of `BuffHpRegen`/`BuffMpRegen`, so the `Flat`
  mode that already existed on the magnitude went nowhere. Added beside the gear/passive flat bonus (so
  the stance multiplier still applies — sitting to meditate pays), and mirrored into `StandingRegen` so
  the stats window cannot disagree with what is actually paid.

### 🆕 `SkillDef.NeverAuto` — a skill can declare itself manual (his ruling, same day)

*"Mana Ray can be removed from auto - no point in 'farming' with it .. its a strategy move - depleate
boss/enemy mp not a farming tool ... same goes for 'Mana Strain'"*. Checked **first** in `ClassifyAuto`,
because Mana Ray carries `MagicDamage` and the attack test would claim it two lines later — and no rule
about effect flags could ever tell it from a nuke. What makes it manual is what it is *for*.
🔑 It routes to `Other`, the never-cast bucket, which means `WarnUncastableAutoSkills` (0.68.0) already
**tells the player the moment they arm the row** that it is theirs to press — so this is an exclusion that
explains itself rather than one that looks like the silent-skip bug playtest 23 found.
🔵 **`BL-83`'s taunts are one flag away** and were deliberately NOT flipped here — he ruled on Mana Ray,
not on that, in this message.

### Mana Strain's half: MP cost can now be RAISED, not only lowered

`PhysMpCostReduction`/`MagicMpCostReduction` were clamped to `[0, 0.8]`, which made a cost-**increasing**
effect impossible — and that is exactly *"a debuff that increases mana consumption of the enemy"*. The
clamp is now `[-2, +0.8]`: from three times the price to a 80% discount, one number read from both ends,
because a discount and a surcharge are the same multiplier and two fields would only get a chance to
disagree.

## 2026-08-19 — 0.73.0: the creatures stop being paper (`BL-78`, defence and attack)

🔴 **Server-side balance only — no protocol change, no client change.** The APK debt from 0.72.0 is
unchanged and still deliberately unpaid (*"dont build apk untill the healer atleast is out"*).
⚠ **Delete `Game.Server/game.db`** before the next run — still owed from 0.71.0's schema change.

### The measurement first: `MobBaseStats` was fitted to a chronicle of IG that no longer exists

The old curve was not sloppy — it was faithful to an **older chronicle**. The public IG databases
disagree by ~3× because they are different versions of the game: creature 22225 at level 80 reads
**3,290 HP / 1,600 P.Atk / 341 P.Def** in the databases the 2026-07-14 fit used, and **13,763 / 4,514 /
1,053** in the one he is playing against. Its six reference creatures (Keltir, Grizzly, Ghoul, Grandis,
Invader Shaman, Tracker Howl) are all old-chronicle. **The old reasoning is rewritten into
`MobBaseStats.cs`, not deleted** — whoever refits this next needs to know "measured against IG" is not
one number.

**New sample: 2,831 creatures** — every monster `l2elo.com` lists at levels 1-83, each read with its
**NPC skill list**. That list is why this is a re-derivation and not another guess: IG authors a creature
exactly the way `MobMod`/`MobMasteries` does, and says the grade out loud (`HP Increase (3x)`,
`Strong P. Atk. Lv15`, `Average P. Def. Lv11`). So the base curve is the median over the creatures **IG
itself tags as `Average`** — the ×1 rung by construction, not a median over a mixed roster.

🔑 **Our passive layer is already IG's, measured.** Pooled over levels 20-90, the tier words are worth
**Weak ×0.82 / Average ×1.00 / Strong ×1.21 / Very Strong ×1.61** — which is `MobMasteries.DefTable`'s
own ladder (0.83 / 1.00 / 1.21 / 1.61). Nothing above the curve needed touching.

### One smooth function per stat — his binding constraint, checked before accuracy

*"everithing above lvl 20 should walk normal curve because there are bosses/instances that will derive
from it (with passives)"*. A boss is base × a passive, so a kink in the base is inherited and multiplied
by every derived creature. All four columns are now the same family, `a·(level + shift)^k`:

```
P.Def(L) = 0.00113 · (L + 44)^2.743      P.Atk(L) = 1.12e-6 · (L + 31)^4.539
M.Def(L) = 0.0027  · (L + 38)^2.542      M.Atk(L) = 1.14e-7 · (L + 32)^4.904
```

Strictly increasing and infinitely differentiable at every level. **Both old discontinuities went with
them**: the `Math.Max(44, …)` P.Def floor (a corner at ~level 10) and the 57-node interpolated P.Atk /
M.Atk table (a slope change at every node). Verified off the compiled code across levels 1-95: **zero
decreasing steps**, and the only second-difference wobble is ±1 from integer truncation.

| lvl | P.Def         | M.Def         | P.Atk             | M.Atk             |
| --- | ------------- | ------------- | ----------------- | ----------------- |
| 1   | 44 → **38**   | 30 → **29**   | 4 → **7**         | 2 → **3**         |
| 20  | 84 → **101**  | 63 → **82**   | 48 → **63**       | 32 → **29**       |
| 40  | 168 → **214** | 126 → **174** | 171 → **283**     | 118 → **146**     |
| 60  | 251 → **385** | 189 → **311** | 529 → **873**     | 370 → **486**     |
| 80  | 336 → **624** | 252 → **498** | 1,321 → **2,152** | 929 → **1,277**   |
| 85  | 356 → **695** | 268 → **554** | 1,627 → **2,629** | 1,145 → **1,582** |

- ✅ **HP is deliberately untouched** — his ruling. Our base HP shape measures 0.87 → 1.08 of IG's from
  40 up. His *"the 80 mobs should have 15k not 5"* is real but it is **not this curve**: 77% of IG
  creatures are `HP Increase (1x)` and 23% carry ×2-×5, so 4,298 at 76 × 3 = 12,894 and × 5 = 21,490.
  The bulk is bought by `MobMod.Hp`, which exists and works. **Authoring it is what is still owed.**
- ✅ **Below 20 came free, so it was taken** (he ruled it a nice-to-have that must not cost the smooth
  curve above 20 — the shift term means it costs nothing). It fixes a real bug on the way past: a
  level-10 mage **no longer one-shots a same-level creature** (nuke 149 → 92 against 120 HP).
- ✅ **M.Atk barely moves** (×0.93 at 20, ×1.37 at 80) because it was the one attack column already close
  to IG. P.Atk was the deficit: ~×1.65 across the whole midgame and endgame.

### What it did to the game — `BalanceMatrix`, before and after

|                                                       | before                 | after                      |
| ----------------------------------------------------- | ---------------------- | -------------------------- |
| Champion TTK on a same-level creature, L60 / L80      | 20.5s / 17.9s          | **31.4s / 33.2s**          |
| Creature DPS onto that champion, L60 / L80            | 47 / 71                | **77 / 116**               |
| Tank survives standing still, L20 / L52               | 133s / 109s            | **104s / 65s**             |
| Kills before the HP bar empties, L52 champion / nuker | 26 / 6                 | **9 / 2**                  |
| Field boss TTK, 3 DD, L60 / L76 / L85                 | 11.4 / 14.8 / 11.6 min | **17.4 / 26.4 / 22.6 min** |
| Kills per hour, S band                                | 75                     | **65**                     |
| Full S-grade character, farm hours (`M12c`)           | 347h                   | **603h**                   |

- ✅ **`BL-13` lands without touching a boss.** His playtest-25 ruling was *10-30 minutes*; field bosses
  now sit inside it at every level, because a boss's defence *is* the base curve (rank multiplies HP and
  P.Atk only), so it inherited the rise.
- ✅ **EXP and SP per kill are unchanged.** `MobKillTimeRatio` reads HP and P.Def as *ratios to the base
  curve*, so a normal creature still scores exactly 1.0 and the elite/boss premiums are untouched. What
  moved is EXP per *hour*, through TTK.
- 🔴 **The farm economy is the bill, and it needs your call.** A full S-grade character went 347h →
  **603h**, and an elite camp fell from 115% of a normal farm to **76%**. `BL-22`'s budget was already
  unreachable at S and is now further out by ~1.7×; that solve has to be re-run against these numbers.
- 🔴 **An unattended farm no longer sustains itself** at level and grade parity (52: 26 kills → 9).

### Tooling

- **`dotnet run --project tools/BalanceMatrix -- --dump-mob-csv`** regenerates
  `docs/data/mobs/mob_base_stats.csv` FROM `MobBaseStats`, so the documented dump can never drift again.
  It had already drifted: the file still carried the pre-2026-07-25 M.Def coefficient.
- Full measurement, method (including the `l2elo.com` JSON API and its two traps — the 400-row band cap
  and the raid-boss levels ending in 4 and 9) and the per-level table:
  **[balance/MobCurveVsIG.md](balance/MobCurveVsIG.md)**.

## 2026-08-19 — 0.72.0: one rate knob, and the buffs cost what the sheet says

🔴 **Needs a new APK, and it is deliberately NOT built yet** — his instruction: *"dont build apk untill
the healer atleast is out"*. Two things in here require one, so the next build carries both: `DebugConfigDto`
lost a field, and the class skill tables changed (the Learn tab is built client-side from the compiled
tables, not from a server push). `Game.sln` builds, the Unity client type-checks, the server boots green.
⚠ **Delete `Game.Server/game.db`** before the next run — still owed from 0.71.0's schema change.

### 🔑 A NEW STANDING RULE: the CSVs and the game move together, both ways

Owner: *"I want the csv-s to represent what is inside the game at all time … I author the skills through
the file so you update the game; if I author a skill through you, you update the file."* Written into
`CLAUDE.md`. `docs/data/classes_skills_csv/` is not documentation that trails the build — it is the skill
data, mirrored, and a commit that changes a `SkillDef` or a `ClassSkill` without touching a CSV should
make you check why. `SkillCsvSeed --check` is what makes "at all times" verifiable; this release took it
from **25 findings to 8**, and **not one MP or SP number disagrees with a CSV any more**. The 8 that
remain are all naming or never-authored rows (`Taunt`/`Provoke`, `Strike`/`Smash`, `Lure`, two spellings,
two duration-column conventions).

### Rates: one knob, and nothing lost to a clamp

*"if i make the dropMod = 30f … killing a mob to yield stuff as much as i killed 30"*. **`/droprate global
30` now means ×30 of everything.** Above 100% a drop pays **COPIES** — whole part guaranteed, fraction
rolled flat, so the expected value is exactly the multiplier (`MobCatalog.DropCopies`; 250% = two copies
plus a 50% roll for a third). Floor was rejected: it would make the knob dead between integers.

- **The guaranteed-group exemption came off.** It only ever existed because of the 100% clamp, which
  pinned a mats group and threw away the weights inside it. With copies, a 100% mats group at ×30 fires
  30 weighted picks in exact table proportion, so his authored numbers are honoured by being *untouched*
  rather than by the knob skipping them.
- 🔑 **Every rate should be the same N.** You kill 1/N as many mobs per level, so ×N on exp, SP, gold and
  drop chance leaves rewards-per-level identical to x1. **`DropAmount` is not a rate** — setting it too
  squares the multiplier — so it left the Debug panel for `/droprate amount <x>`.
- 🔴 **Bug fixed in passing: quest gold and quest SP were paid RAW.** On a x30 server every quest paid x1.
  Both now route through `RateConfig.Quest` × World × runes.
- The five rate floats became one **`RateSet`** (`Game.Shared/RateSet.cs`), composed with `*` across three
  scopes: World × Quest × the character's runes.
- Verified with `tools/BalanceMatrix`: **at x1 the output is byte-identical**, so his normal build is
  untouched. At ×30, mats/kill went 1.76 → 52.8 — that group used to be exempt and moved not at all.

### Buffs: a single's price is authored per rung now, not derived

His re-priced `cleric 2nd.csv` prices a buff by **the level it is learned at** (20 → 20 MP, 25 → 26,
30 → 33, 35 → 40); `buffer 3rd.csv` still prices by rung, 30-50. The two cannot both be true — Focus L4 is
42 by the old `30 + 20·i/(n−1)` formula and 26 on his sheet — so `RungCost[]` in `Skills.BuffLadders.cs`
now carries each authored `INIT MP` / `FINIT MP` / `SP` verbatim, and the formula survives only as the
default for rungs no CSV has reached. ⚠ **Ladders that look irregular are irregular on purpose**: Ward L1
and L2 both cost 40 MP and the SP goes *down* between them; Frenzy L1 fell 125 → 40 while rungs 3 and 6
keep the buffer's 145/175.

- **`mage 1st.csv`**: he split the level-7 buff row, so **Might stays at 7 and Bulwark moves to 14**. A
  level-7 mage buys offence and waits a tier for defence.
- **`cleric 2nd.csv`**: Swift L3 moved 25 → 30, Alacrity L2 moved 25 → 35, **Haste is gone** from the
  cleric entirely (attack speed is a buffer reward from 40), and **Shrouding Hymn left the cleric for the
  buffer** — which settles the one row in that file whose values were never his.
- **Clarity is new at 25, at his 20%.** The family had one rung (the healer's 30% at 40), so 20% became
  rung 1 and the Lightbringer's grant moved to rung 2 — the number he authored is unchanged, only its index.
- 🔑 **A mastery's SP is the level's SP**: 3200 / 6400 / 12800 / 25000 at 20/25/30/35, for every Armor and
  Spell Mastery. *"i dont know why they were so overinflated"* — the cleric's Armor Mastery and the nuker's
  separate `Mage Armor Mastery` both opened at 9600. He corrected `nuker 2nd.csv` himself in the same pass,
  so both sheets and the code now say 3200 / 6400.
- **Bolt MP came down to his sheets.** Magic Bolt (12/17 → 10/15), Vampiric Bolt (40/54/64/72/82 →
  28/40/46/52/62), Elemental and Quick Bolt (27/32/36/41 → 20/23/26/31). The unauthored rungs above 35
  carry the same ratio his band implies, so there is no cliff at 40 — Vampiric used to jump 62 → 90.

### Shield Bless and Harden — the sixth improved group (Warchanter 66)

His `buffer 3rd.csv` row. Its `REPLACES` column read `[Swift Alacrity Agility Haste]`, copied from the row
below it; this was reported as a paste error and **he corrected it** — *"it should rapladse Shield Harder
and Shield Bless"*. The copy was the contents, not the intent. So two families were created,
`shield_def` (*Shield Harden*, % shield P.Def) and `shield_block` (*Shield Bless*, % block **chance** —
reduction is never raised), and `HolyShield` names both in `ChildBuffs` (which is what makes the engine
treat it as a group, covering their families at group rank) and in `Replaces` (which collapses them off the
learn list). Party, 40 + 160 MP, 100k SP, 1s cast, 20 minutes; in `AdminBuffSet`, not the newbie NPC's set.
**Self-gating** — both numbers are a percent of what the shield carries, and 0 × 1.5 is still 0.
⏰ **Each family has ONE rung, at the group's own +50% / +30%**, because that is all that exists. He is
authoring the singles now (`healer 3rd.csv` drafts Shield Harden at +5% @40, +10% @48); when they land the
rungs become a ladder and the group re-points at the top index. Nothing between 5% and 50% was invented.

## 2026-08-17 — 0.71.0: the cleric says what it does, and the healer reaches 40

⚠ **Needs a new APK** — the Learn tab is built client-side from the compiled class tables, so none of the
new skills appear on the phone until it is rebuilt. `Game.sln` builds, the Unity client type-checks, the
server boots green, and `cleric 2nd.csv` now reports **zero** findings from `SkillCsvSeed --check` (the
repo total went 53 → 25).

**`cleric 2nd.csv` was the last document still describing the PRE-SPLIT buffs.** It wrote Might as P.Atk
*and* P.Def, Speed as cast+move+evasion, Force as M.Atk plus a magic-cancel line — bundles the code
stopped granting on 2026-07-31, when he asked for *"the cleric to learn the individual buffs"*. The 11
bundled rows are now 17 single rows naming the buffs the game actually casts. **Nothing was retuned**: his
numbers survived the split intact, and the "decrease magic dmg by 18/25" on the old Force rows turns out
to be the Resolve interrupt ladder. He then edited it back — Antidote's cure ranks, Aim down to L1, and
Alacrity capped at L2 — and the code follows him.

**Taunt is his ladder now.** He authored it into `tank 2nd.csv`: **four rungs at 24/28/32/36, power
4500 / 5000 / 5500 / 6000**, replacing the five-rung 1500→5100 curve `BL-71` had derived from his
playtest-22 endpoints. His is higher at the bottom and far flatter — and it **starts at 24**, because he
deleted the level-20 row by hand. Confirmed deliberate when queried: a tank has no taunt for his first
four levels.

**Combat Stance is gone from the cleric** — *"the only one created after the csv ... which we will remove
and add it to the buffer later in different form"*. It was the one cleric skill this project invented
rather than took from him, which is why it is the one taken back out; the `SkillDef` stays so it can
return on the buffer.

**Magical debuffs are resisted by SPT, not WIT.** His rule, and it makes both halves of the contest a real
build cost: *"the actual stat u give up to increase wit and atk as a mage (so u get easily debuffed by
magic debuffs) — con is for physical ... same logic you give up con to increase atk and agi"*. The
glassier your offence, the easier you are to lock down.

**Totems exist.** A totem is the mirror image of a trap — a trap waits once for an ENEMY and dies, a totem
pulses at ALLIES on a timer — so it is built the same way, as a placed server-side object rather than an
Entity. That is deliberate: a new `EntityKind` would need auditing through ~137 call sites, 54 of which
ask "is this a mob", and a totem would quietly become a valid aggro or loot target in whichever one was
missed. **Pets** are the case that will justify paying that cost. A totem outlives its owner's death, on
purpose: it is planted ground, not a channel.

**His `healer 3rd.csv` landed, and its whole level-40 block is built** — the first authored 3rd-class
discipline (`BL-02`). Race splits the kit twice, once on the heal and once on the debuff: Human gets Quick
Great Heal + Gravity, Elf gets Healer Blessing + Bind, Ork gets the Healing Totem + Armor Break. Where a
row landed on an existing skill's exact slot the **id was reused rather than retired** — which is right for
the data and wrong for reading code, and is now `BL-84`. Two new buffs needed a new engine piece,
**per-school control resistance**: **Clarity** (30% vs SPT-defended debuffs) and **Fortitude** (15% vs
CON-defended ones), riding as fields because the SkillEffect flag enum is full. Only the rungs he authored
exist. ⚠ Everything at level **44 and above** in that file is marked "not done" and was left alone.

## 2026-08-17 — the fourth class exists, and every class has a name of its own

⚠ **New DB column** (`FourthClass`, on both the subclass rows and the character mirror) — `EnsureCreated()`
does not ALTER, so **delete `Game.Server/game.db` (+ `-shm`/`-wal`)**. ⚠ **The client changed too**
(class labels, stats sheet, debug panel), so this needs a **new APK**. `Game.sln` builds, the Unity
client type-checks, the server boots green and **SmokeTest passes**.

**3rd- and 4th-class names are PER RACE now.** His call, picked from three options: *"lets decide on
class names for 3rd and 4th"*. `ThirdClassDef.Name` was `Discipline.ToString()`, so every race's tank
read "Bulwark" — which sat badly against his own *"the varity will come from race diference"*: the KIT
could differ per race (the trailing `RACE` column in the 40+ CSVs), but the LABEL could not, so the
variety was invisible. The rogue line was the lone exception, because the 2026-07-29 archer merge had
split it into real per-race disciplines. Now an Elf tank is an **Aegis**, an Ork tank an **Ironhide**,
and at 76 a **Dawnshield** and a **Stonemaw**. All 48 strings live in one file,
`Game.Shared/Classes.Names.cs`, keyed `(discipline, race)`.

🔑 **Names are free to change; ids are not.** Nothing persists a class name — a character stores the
number (101-136 for 3rd, 201-236 for 4th). Retune any string in that table and no save breaks. A
**boot-time guard rejects duplicates**, because the class-change NPC lists what you may become *by
name* and a repeat would make two different changes indistinguishable.

⚠ **`Warlord` was retired as a name** — it is a class name in IG, the same rule that took the old town
names and the old currency term. war_aoe is **Banneret / Galeherald / Skullbreaker** now. (`Sorcerer`,
the Human nuker's 2nd class, is the identical slip and was deliberately **left alone** — renaming a 2nd
class was not what he asked for.)

**The 4th class is available.** His instruction: *"now can be without quest but go in the apothecary and
buy a 100kk 4th_class_item and go to class amster with it … then we add additional long quest"*. So:
level **76**, the **Rite of Ascension** at **100,000,000 gold** on every Apothecary shelf (untradeable),
consumed by **Archmaster Sevrin** (`master_class4`) on the west side of **Frostmere** — the last town on
the level path, and therefore the only one whose fields reach 76. When the long chain lands it replaces
the **purchase**, not the item: the Rite comes off the shelf and becomes the chain's reward, and the
class-change requirement itself does not change.

🔑 **A 4th class does not branch** — it is the same discipline awakened, one ascension per 3rd class. So
there is no fourth enum and no choice to make: `FourthClassDef` carries the `Discipline`, its id is the
parent's + 100, and the debug affordance is a **toggle** (which steps back down too, because the real
change is one-way and the not-yet-ascended state has to be reachable twice in one session).

⚠ **It grants NO skills, on purpose.** `ClassKey` has no tier component, so a 4th kit registered against
the discipline would leak to every level-40 — and the standing 40+ rule is *"anything that's not inside
the csv should not exist"*. The kit lands with the `*.4th.csv` files (`BL-02`). Today the ascension buys
**the name** and **the L5/L6 crafting band**, and nothing else.

🔴 **`Crafting.RequireFourthClassForL5` flipped to `true`.** His gate is *"L5,6 needs 76 (4th class)"*,
and it sat at `false` only because no 4th class existed to gate on. **This is a live change for anyone
already at 76: the top two crafting rungs now cost the Rite.** One `const` reverts it. `Entity.CraftBandCap`
stopped passing a hard-coded `false` the same day.

⚠ **Vanguard and Tempest are still SELECTABLE.** The CSV README had claimed nothing reaches them — true
of SKILLS, false of the class list: `Disciplines.Of` returns two disciplines per archetype, so a level-40
Knight is still offered Vanguard at the Grandmaster. Collapsing that touches persisted ids, so it was left
alone; both carry one name for all three races (Doomward / Skybreaker) rather than six invented names for
two classes on their way out.

The class tree in `docs/data/classes_skills_csv/README.md` was rewritten to match — it now names all 24
third and 24 fourth classes, and the 🔴 it had been holding open ("only the ROGUE line has a name per
race") is answered and gone.

## 2026-08-17 — the skill CSVs become class tiers, and a recheck that actually runs

Docs and one tool. No game code changed; `Game.sln` builds clean and the protocol is untouched.

**The filenames are class TIERS now.** His call: *"well for fighters 20-35 is not right .. they have
skills at 36 .. so 2nd class is more suited and understandable"* — a level band in a filename is a claim
about the content, and that one was already false. `fighter/mage 01-15` → `1st`, the five `… 20-35` →
`2nd`, `… 40-74` → `3rd`, `… 76-85` → `4th`, and `melee rogue` → **`dual`**. All fifteen moved with
`git mv`; **not one row was edited**. References were swept through the code comments, `Backlog.md` and
`Open-Checklist.md` `85n`; `Playtest-Archive.md` and older changelog entries were left alone, being a
verbatim record.

**His new discipline map — eight, not twelve.** *"class 2nd => desc1/desc2 3rd => desc1/desc2 4th"*:
cleric → **buffer**/**healer**, rogue → **archer**/**dual**, warrior → **warrior**/**war_aoe**, and then
tank → **tank** and nuker → **nuker** alone, because *"the varity will come from race diference"*. So
`Vanguard` is dropped (*"we have a warrior for that"*) and Magus+Tempest merge. The **RACE column is the
third identity**, which is what keeps this at 16 files instead of 48. ⚠ The `Discipline` enum is NOT
collapsed — its values persist on characters, so that happens when the authored kits land.

`tools/SkillCsvSeed` grew the new map and wrote the eight files that had none. The one that matters is
**`nuker 3rd.csv`, 20 rows** — the Magus/Tempest kit (Elemental Burst L1-L10, Frost Bind, Entangling
Roots, Glacial Spike, Creeping Frost, Phase Shift, Mana Barrier) is the only substantial 40+ content
outside the buffer's ladder and **no seeded file had ever covered it**. Folding two kits into one leaves
two skills under two names each (`FlameBolt` as *Annihilate* and *Chain Lightning*, `GreaterWeakness` as
*Mana Burn* and *Maelstrom*), which is his to reconcile. Nine of the sixteen are empty; that is the
honest picture, not an oversight.

⚠ The old suffixes left **level 75 in neither band**, silently dropping Elemental Burst's 10th rung.
`3rd` now closes at 75 and that rung is the last row of the file.

**`dotnet run --project tools/SkillCsvSeed -- --check`** — the recheck he asked for, as a tool rather
than a read-through, because a rung is implied by ORDER (`Heal` at 20/25/30/35 *is* levels 1-4) and
eyeballing 180 rows is how a wrong number gets ratified. It reports **53 discrepancies**, and three
lessons are baked into it after each produced a page of fake findings first: **MP must be compared as a
TOTAL** (his sheet puts the whole cost in `INIT MP`, the engine splits it two-stage); **the code side
must be band-limited to the tier** (`Elemental Bolt` is registered 20→80 against a file authoring
20-35); and **an auto-granted rung has no class-table row**, so leaving it in shifts the whole ladder by
one. Name drift is paired up and reported once, not twice.

It found one real defect, and it was in the CSV rather than the code: **`tank 2nd.csv` had two
transposed learn levels** — Tank Anti-Magic authored at 20/**34**/**38**/32/36 where the code registers
20/24/28/32/36. In file order the magnitudes (25/30/35/40/45) and SP (1700/3200/6000/11000/20000) match
the code exactly, so `34` was a typo for 24 and `38` for 28. **He fixed both himself the same day**, and
the row is gone from the report — which is the tool doing the only job it was built for.

Two things it flags that are sheet CONVENTION, not defects, and are staying: `Shield Stun` and `Stay!`
leave the DURRATION column at 0 while their own descriptions say "for 9s" / "for 15s" (the code uses 9
and 15); and his SP-0 rows are auto-granted skills, which by definition have no class-table entry.

**`.gitignore` — the Google Drive block grew up.** The repo sits inside a Drive-synced tree and only the
two `.tmp.drive{upload,download}/` folders were covered. Added: the `.tmp.drive*/` wildcard for the
variants those two miss, `desktop.ini` (hidden, so it slips past a glance at `git status` and rides in
on `git add .`), `.shortcut-targets-by-id/`, and the Google-native document stubs — `*.gdoc`, `*.gsheet`,
`*.gslides` and the rest. Those last are not documents: each is a tiny JSON stub holding a doc id, so
committing one commits a dead link. Nothing matching was tracked, so nothing had to be un-tracked.

## 0.70.0 — 2026-08-16 — `BL-47` step 2: five creatures built like players

The demo he asked for in playtest 24 — *"and later we can do 2~5 mobs so I can test"* — after ruling
**migrate** on `86b`. **Server-only: no DTO, no hub method, no push name changed, so the protocol stays
at 21 and the 0.69.0 APK he already has talks to this server unchanged.** No DB reset — mobs are
runtime-only and nothing persisted moved.

**Where they are.** A new gatekeeper destination, **Proving Grounds**, on the row south of the training
dummies (the Training Grounds field was extended south to hold it). Five columns; in each one the
player-built creature stands north and its **curve twin** — an ordinary `MobBaseStats` creature of the
same level, no passives, holding the same weapon — stands directly south. Nothing is aggressive and
nothing drops loot: you pick the fight and the only thing that changes hands is exp.

| #   | Creature                  | Lv  | Race lean (±5)           | Loadout                                     | Passive                                             |
| --- | ------------------------- | --- | ------------------------ | ------------------------------------------- | --------------------------------------------------- |
| 1   | Goblin Raider             | 40  | +5 CON / +5 ATK / −5 AGI | t40 Rare 2H sword over t1 Uncommon heavy    | **none**                                            |
| 2   | Goblin Elder Raider       | 45  | same                     | **identical to #1**                         | **none**                                            |
| 3   | Cairn Lich                | 60  | −5 CON / +5 WIT          | t52 Common staff +30 over t1 Epic robe      | HP ×3.73, P.Def ×1.02, M.Def ×0.78, M.Atk ×0.97     |
| 4   | Fallen Seraph             | 80  | +5 AGI / −5 CON          | t80 Epic 2H sword +16 over t52 Common heavy | HP ×1.46, P.Def ×1.05, M.Def ×0.61, **P.Atk ×2.07** |
| 5   | Fallen Seraph, Runebearer | 80  | same                     | **identical to #4** + a held War Rune       | same, **no attack passive**                         |

**What it already answered** (new `BalanceMatrix` section **`G3.8`**, each creature divided by its twin):

- 🔑 **His ±5 band works on everything except attack.** The same authored loadout five levels apart
  holds defence and HP (P.Def x1.04 → x0.95, HP x1.10 → x1.06) and **loses a quarter of its P.Atk**
  (x0.87 → x0.64) — the mob attack curve is the steep one. So *"prefixed 100+ mobs and give them +-5
  lvl ranges"* costs **one number per band, and it is the attack number.** Both goblins are left
  deliberately bare so the drift can be felt rather than read.
- 🔑 **A held War Rune replaces an authored attack passive.** Bare, the level-80 build reads **x0.48**
  of its curve's P.Atk. The authored per-band passive gets it to x1.00; **the rune gets it to x0.97.**
  One item against a table that drifts with level — his B3, measured.
- ⚠ **The level-80 attack passive needed ×2.07, not `G3.7`'s ×1.55, and nobody was wrong.** `G3.7`
  measures against the bare `MobBaseStats` curve; the creature that actually spawns beside it also
  carries **BL-14's weapon power factor** (a slow 2H weapon buys per-hit damage). `G3.8` measures
  against the game, and it is the one to trust. Every `MobMod` number in the demo is fitted from it —
  **re-run `G3.8` after touching a demo template.**

**How it is built, and how little it moves.** `MobType.Build` (a new `MobBuild`) carries the class, the
±5 lean, the split loadout — weapon and armour on **separate** tiers, which is the whole finding of
`G3.7` — and any held item. `Entity.ApplyMobBuild` gives the creature its identity and puts the gear ON
it before the recompute (the equip loop is what turns worn gear into stats, so a piece added afterwards
does nothing until something else re-runs). `Entity.PlayerBuilt` then swings the **six stat bases**
plus the weapon P.Atk/M.Atk fold and the two magic level terms onto the player side of
`RecomputeDerived`.

- **That is all it moves.** It is still a Mob to aggro, drops, targeting, the client's plate, PvP and
  party. It deliberately does NOT take the player-only branches around it: no armor sets, no
  learned-passive main-stat loop, no armor-weight masteries, no race+class speed override (so it can
  still be kited) and no grade penalty (it wears under-grade gear by design, so the gap is 0 anyway).
- **Rank and `MobMod` still land on top** in `ApplyMobScale` — the design in one line: gear gets you
  most of the way, the passive carries the remainder. Two things are switched OFF there for a
  player-built creature because they would pay twice: BL-14's weapon power factor (it holds the real
  weapon) and the ROLE's stat lean (a robe, a staff and a mage class curve already make it a caster).
- **The bag is HELD, never looted.** A mob's loot is its drop table and nothing in the death path looks
  at its inventory — his *"not a dropped one..but just to hold stuff"* needed no work at all, which is
  what admits the War Rune. Its buff is applied once at spawn and never expires: the player-side rune
  reconciliation is player-only and clock-driven, and a creature has neither a clock nor a login.
- **Two boot guards.** `MobCatalog.ValidateBuilds` fails startup if a build names gear that does not
  exist — a missing id is silent *and flattering*, since the creature just spawns without that slot and
  a half-naked entity reads as "the player pipeline under-delivers". And `MobType.HandPlaced` fences
  these nine templates out of `MobCatalog.InBand`, without which a level-40 Goblin Raider would have
  been rostered into every generated 40-44 camp in the game the moment it was authored.

**One line on the target window.** A player-built creature says so when you inspect it, listing its
weapon, its armour and anything it holds. It rides the existing passive-lines array, which is why the
whole feature needs no client build.

## 0.69.0 — 2026-08-16 — the playtest-24 batch: the flag follows intent

Everything playtest 24 produced except the mob-as-player demo (`BL-47` step 2, which is content, not a
fix). Protocol unchanged at 21 — nothing here touches the wire — and **no DB reset**.

**The two bugs he found.**

- 🔴 **REFLECT NO LONGER FLAGS THE DEFENDER** — his anti-PK exploit (`87a`). *"Reflect should not flag
  me — that's a big anti pk exploit ... som1 comes to me and wants to kill me but I don't want to ..so
  he hits me see I become pvp flag and he just kills me."* Reflect damage runs through `ApplyDamage`
  with the roles swapped — the defender arrives as the `attacker` argument — so the flag block turned
  the *defender* purple for a blow he never struck, and an aggressor could manufacture a legal victim
  by walking into a reflect. `ApplyDamage` takes a `reflected` flag that skips both halves of the PvP
  block (the flag AND the `LastPvpAttackerId` record, so the aggressor cannot claim the defender as
  "the man who attacked me" either). **All three reflect paths covered**: the armor sets'
  `MeleeReflect`, `BL-07` Deflection, and `BL-08` Backlash — the third needed no change, but only by
  accident (a bounced debuff carries no `SourceId`, so its ticks credit nobody), and that accident is
  now written down at the call site so a future kill-credit "fix" cannot re-open the door.
- 🔴 **THE SYSTEM/ALL CHAT TABS NO LONGER LAG THE GAME** (`87b`). *"System chat lagging the game ...
  Other tabs don't just system(respectedly and 'all')"* · *"after a game restart it works."* The
  per-line append path was already fast; the per-BATCH one was not. A batch is everything not yet
  drawn, and three routes make that the whole 1000-line buffer — switching tab, reopening the window
  (the refresh early-outs while it is closed, so the backlog waits for it) and a Clear generation.
  Each built up to 1000 labels with ContentSizeFitters in ONE frame and then destroyed ~880 of them
  immediately. That is exactly the report: System and All are the only tabs the buffer fills, and a
  restart empties it. The draw batch is now capped at the display cap, so a row is never created only
  to be trimmed. Two smaller cuts alongside: console rows are DETACHED before `Destroy` (deferred
  destruction was leaving corpses in `childCount`, which every piece of the trim arithmetic reads),
  and stack-trace capture is off for `Log`/`Warning` — every System line goes through `Debug.Log` and
  chat lines deliberately do not, which is the other reason that tab cost more than the rest.

**The rule he gave.**

- 🔑 **`BL-77` — THE PVP FLAG IS THE AREA FILTER** (`87c`), from `85a` and generalised by him on the
  spot: *"pvp-off = using AOE skills hit only nearby monsters"* · *"pvp-on = hit nearby players as
  well"* · *"flare with pvp on reveals nearby players and Act as hit so flags"* · *"any skill that does
  no dmg and can be casted on a player if the PvP is off is (monster only) but if pvp is on it cast on
  a player and flags."* Built where every area skill inherits it at once (`EnemiesInRadius`), so the
  AOE warrior class picks it up when `BL-02` lands. The player arm delegates to `CanPvpHit`, so an area
  cast obeys every rule a single swing does — never your own party, never in or into a safe zone. Two
  no-damage holes closed with it: the flare now flags (and, with PvP off, sweeps creatures only and
  says so instead of silently doing nothing), and a single-target hostile skill that lands no damage —
  a taunt, a cancel, a resisted debuff — flags too, which it never did.
  🔑 **Read with `87a`: the flag follows INTENT.** What you deliberately do with PvP on costs you the
  purple name even when it deals nothing; what your gear does back to an attacker on its own costs you
  nothing. ⚠ Three shape questions on that entry were answered as the shape every other system here
  already has, and they are marked as mine in the source: party excluded, support skills not routed
  through it, and only the ACTOR flagged (never the person revealed).

**The four `[~]` changes.**

- **The target frame's first row is no longer covered** (`87d`). *"the text 'mob:..' is hidden ... the
  1st text is half hidden."* He read it as the title row; the arithmetic says the bottom. The detail
  line ran 94→114 from the panel top while the first button row, being bottom-anchored, ran 76→104 in
  a 148-tall panel — the other half of playtest 23's 28px shrink, where the top rows moved up with the
  deleted name row and the bottom-anchored buttons moved up with the panel floor, into them. Fixed
  twice over: ONE button row instead of two (five of the seven buttons have been permanently hidden
  since playtest 23) and 12px more height, so the gap is real rather than a tie.
- ⚠ **…and he called it the GENERIC window bug, so all 23 windows with a title bar were swept.** He was
  right twice: the **trade window's** partner-name line bit 6px out of both column headers, because
  their row constant was a hand-picked 96 instead of being measured off the title bar — it is derived
  from `chrome` now, so it cannot drift again. The other 21 are clean. (The skills window's right-hand
  readout does sit inside the title-bar band, but it is drawn over an empty part of the bar and hides
  nothing — left alone deliberately.)
- **The chat and combat windows — all four asks** (`87e`).
  **(a)** The combat window can reach the left edge. `DragMove.Clamp` assumed every window was
  centre-anchored and none of the movable ones are; for a bottom-RIGHT-pinned window the same numbers
  allowed a long drag off the right edge and stopped it dead just past the left one. It works in the
  parent's own coordinates now, so anchor and pivot drop out and both directions get the same 60 units
  of guaranteed handle.
  **(b)** Resize is no longer inverted. *"The drag button should move not the top/left."* He was
  describing a PIVOT: the size grew correctly but a uGUI rect grows away from its pivot, and both
  windows are pinned by a bottom corner — so height was added upwards and the grip never followed the
  finger. The position is compensated by the pivot, pinning the TOP-LEFT corner instead.
  **(c)** The Clear/Reply row is gone and the feed runs to the bottom of the window. Both moved into
  the title bar beside the padlock, as a **bin** and a **speech bubble**.
  **(d)** The grip no longer appears and disappears — it stays and DIMS when locked, so the corner it
  owns is the same corner at all times, and the lock is a **padlock**.
  🔑 **The three icons are drawn from rectangles, not typed as characters.** The bundled TMP atlas is
  static and has no 🗑/💬/🔒; TMP draws a missing glyph as the hollow box that has turned up twice
  before, and adding glyphs needs the Editor, which is not part of this workflow. `UiKit.Icon` composes
  each one from a handful of Images in normalised coordinates — no font, no sprite asset, scales with
  the button.
- **The gear picker** (`87f`): selection chips are 28px instead of 34 (three strips of them sat above
  the list, and the tier row already wraps to two), and the filtered list gets its own header. Only the
  armor-with-sets branch had one, so on weapons, jewels and sub-Epic armor the first give-button butted
  onto the tier chips and read as a fourth row of them.

## 0.68.0, part two — 2026-08-16 — the admin gear picker, and `G3` documented

⚠ **Same version, same APK as the section below.** 0.68.0 was authored on 2026-08-15 and then sat
unpublished — `builds/` still held 0.67.2 — so this work went in before the build ran rather than after
it, and rides into the one APK he installs. There is no 0.68.1. Two backlog items he cleared to build
while playtest 24 waited on that APK, plus one new entry recorded.

### `BL-56` — the admin gear picker is a selection box

The Equip tab was a two-level drill-down (category → level → piece) that **could only ever hand out
MYTHIC gear**. That is not a filter someone chose: the catalog's authored piece *is* the Mythic one and
every lesser quality is a generated copy at a suffixed id (`{id}_rare`), so the list's
`Rarity == Mythic` filter — correct, and load-bearing, since it is what stops the picker handing out the
Epic copy at 70% of the real stats — also made five sixths of the gear ladder unreachable from the
window an admin uses to set up a test. Kitting a character in what a player at that level would actually
be wearing was the one thing this window could not do.

It is now a single page with three selection boxes — **type / quality / tier** — and the list under
them. Every axis visible at once, no Back button, and the whole ladder reachable.

- 🔑 **Chips, not a `TMP_Dropdown`.** His entry offered either shape and said *"Pick wichever is easier
  to implemment."* A dropdown built in code needs a template hierarchy (caption, item template, its own
  scroll rect) that cannot be verified without opening the Editor, and chips win on the thing that
  prompted the entry anyway: every option is visible rather than behind a tap. The strip wraps at 6, so
  the tier row cannot squeeze its labels to nothing as tiers are authored.
- ⚠ **`ItemCatalog.QualityId` falls back to the Mythic id instead of returning null**, so the chosen
  rarity is re-checked before a row is drawn (`QualityDef`). Without that a rung the catalog does not
  generate would silently hand out the Mythic piece — the same class of bug as the stale-filter one
  above, in the same window.
- **S grade is top-half only**, so below Epic at level 80+ the quality chips dim and the list says why
  rather than showing an empty page.
- **The full-set button now respects quality** and is withheld below Epic, where a generated copy
  carries no `SetId` and no attributes — offering it there would promise a set bonus that cannot exist.
- The quality choice is deliberately **not** reset by a tab switch, unlike type and tier: a drill-down
  position is somewhere you navigated to, a quality is a filter you picked.

### `BL-47` / `G3` — step 1 delivered: the document and the tables

His order was *"I want it documented and balance matrix tables. So I can make comparisons. And later we
can do 2~5 mobs so I can test."* → **[docs/design/MobsAsPlayers.md](design/MobsAsPlayers.md)**. No game
code was touched; the `G3` measurement sections have existed since 2026-08-05.

Three findings that were not known when the entry was written:

- 🔑 **The inflated ATK/CON he objected to are already inert, and the only thing left of them is a
  DISPLAY.** `MobStats` still sets `Con 15+2·level` / `Atk 8+2·level` (level 80 → 175 / 168), but
  `RecomputeDerived` sends a mob to `MobBaseStats` for HP, MP, P.Atk, M.Atk, P.Def and M.Def — neither
  stat is read by any of them. Only AGI 30 and WIT 5 do anything. `GameUi.Target.cs` printed both
  numbers on the target sheet, which is the whole of what he saw — **so that line is now gone**, and with
  it SPT, whose own comment in `MobStats` says mobs never read it. A mob's Attributes block is AGI and
  WIT, the two that are live. Nothing in the simulation moved; the inflated numbers were only ever text.
- 🔑 **Four of his five passive families already ship**, as `MobMasteries` / `MobMod` plus 0.65.0's mob
  weapon types — armor weight (3 rungs, no robe arm), weapon weight (17 rungs, ~4 of his 7 axes), M.Def
  and HP tracks. **Speed is the only family with no track at all.** The design is ~70% built under a
  different name, which is the argument against migrating rather than finishing it.
- ⚠ **The `G3` verdict block was lying.** It restated its own tables as hardcoded prose, and three of
  its claims had drifted: TTK "4-16s vs 2-16s" (really **1.9-35.7s vs 2.4-20.5s**), level-80 dps "13-33
  vs 46" (really **13-37 vs 71**), and a swing-clock side effect that has since been **fixed to ×1.00**.
  The verdict now computes from the same values the table prints, and says so when the clocks agree.

### Recorded, not built

- **`BL-76` — boss skill gems**, his new design: a boss drops a gem for its own level in three rarities
  (Epic 50% / Legendary 5% / Mythic 0.5%) granting a damage skill at 1/5, 1/2 or 1/1 of a class skill,
  with a PvP/PvE atk/def passive from Legendary and a random +1 stat at Mythic. His numbers are
  explicitly pre-authorised to move. It would be the **first real consumer of the PvP/PvE damage
  multiplier hooks**, which have been hardcoded 1.0 and reserved under the held `BL-19`.

## 0.68.0 — 2026-08-15 — the playtest-23 batch

His first device pass in five versions came back with almost everything green and **sixteen finds**, and
this is all of them that were mine to act on. The pass itself is archived verbatim at
[testing/Playtest-Archive.md#playtest-23](testing/Playtest-Archive.md#playtest-23).

**🔴 NEEDS A NEW APK** — protocol **20 → 21**, and half of this is client work. **No DB reset**: nothing
persisted changed shape.

### Two things were built and could never fire

- 🔴 **Signal Flare could not catch anybody, ever.** *"Flare does nothing ...cannot find flagged player
  next to me. Doesn't cancel his vanish skill."* `RevealHidden` walked `PlayersInRadius`, which is the
  **party-support** enumeration: it returns the caster plus party members, and it deliberately skips
  `e.Hidden` because a party heal must not silently find someone nobody can see. Both halves are exactly
  wrong for a flare, whose entire subject is a hidden NON-party enemy — so the two rules cancelled and the
  skill was a no-op with a success message. It walks the grid itself now.
- 🔴 **No taunt had ever fired from the auto-hunt chain.** *"Provoke is not auto used in any form."*
  `ClassifyAuto` sorted it into `Other`, the never-cast bucket, because the debuff test asks for
  `ContestCc` or a `DebuffSchool` and a taunt is neither — it is not contested and has no school to
  resist. Taunts now have their own rung, **above Attack** (a tank's attack chain is never idle, so a rung
  below it would fire on no tick at all). Also answers *"check the cyclic logic ...I feel there is a
  problem"*: the cursor walk is correct, and what looked broken was armed rows the chain silently
  ignored — **it now says which rows it cannot cast** instead of skipping them in silence.
- **The resurrection scroll refused every valid corpse.** *"cannot use scroll of resurrection (cleric
  skill works) but scroll says 'need a fallen ally as its target'."* The server has had a targeted
  item-use path since the scroll shipped; **this client only ever called the untargeted one**, so the
  scroll validated a target that was never sent. The cleric's skill worked because a cast has always
  carried its target.

### His rulings on things already built

- 🔴 **Mob social clans are OFF, and the system is untouched.** *"way to harsh with our mobs position …
  hitting one wolf getting ganked by 10 other … For a mage lvl 9 hitting a warefolf means dead … remove
  all mobs social clan (leave the system ..we will use it just not now) … Make a note to turn it on once
  the world map is in place."* 🔑 The defect is **spawn DENSITY, not the 450 radius** — every camp
  generates on nearly one point, so a cry reaches all of it. His target shape (*"it will call ONE, and
  while you fight, if others wander in the social range they will aggro"*) is what the same radius already
  does once a camp occupies real ground. One switch, `GameConstants.MobClansEnabled`; the twelve clans
  stay authored and every line of `CryForHelp` stays live. Note filed as **`BL-73`**.
- 🔴 **The three hide skills are re-homed to the classes he named.** **`Prowl` → every melee rogue at 40**
  (was rogue 20, which handed the dagger's stance to every future archer too), **`Signal Flare` → every
  archer at 60**, **`Vanish` → every melee rogue at 60** with a **2 min cooldown / 30s duration**, his
  numbers. 🔑 The counter now sits **level with** what it counters instead of twelve levels below it, and
  the 2-minute reuse is what makes the flare's 30s no-hide stamp mean something — at the old 30s cooldown
  the stamp expired at the same moment the skill came back. "Melee rogue" and "archer" are three
  disciplines each (the archer merge splits the rogue by race at 40). Supersedes 0.67.2's Phantom-only
  stopgap; still a named exception to `BL-02`, not a repeal of the 40+ purge.
- 🔴 **The preservation skills are a DEATH PROMPT now, not a refusal to die.** *"now is literally undying
  will ... U just don't die u heal +30% when your hp reaches 0"* → *"the tanks and healers are like you die
  (mobs stop attacking etc ..the hole pipe) and get a resurrection promp if you click yes u resurrect on
  the spot, else back to town"* → *"I want phebyx blood - u die -> u stay dead until you click the
  resurrection prompt."* Undying Will and Rite of Preservation now run the **whole death pipeline** and
  then offer a self-resurrection **that never expires**. One call changed, from `ResurrectTarget` to
  `OfferResurrect` — everything he listed as "the whole pipe" was already running before that line.
  ⚠ The heal-at-0 shape he liked for a warrior is **not deleted**: it is `LastStand` (`LethalSave`, 50%),
  already in the catalog and waiting on a class and a level like every other 40+ skill — **`BL-75`**.
- 🔴 **The PvP flag is paid at the START of a resurrection.** *"In a mass pvp if my friend is dead
  (flagged/pk) and I start to resurrect I become pvp while I resurrect him not after he stands.. So other
  ppl can kill me or attempt to stop the resurrection."* It was charged in `ResurrectTarget` — after a 10s
  channel **and** a prompt the corpse might never answer — so the entire window in which the res could be
  contested was a window in which the resurrector was untouchable. Charged on both the cast and the scroll
  path, and not refunded on an interrupt: you were visibly holding a channel over an outlaw's body.
- 🔴 **Boss EXP: the party split is out, the respawn wait is in.** *"a 90 elite gives ~200k exp while boss
  gives 6kk … 30times more and feel like a waste ... make it give atleast 20kk ... we should take the
  respawn time and the time it takes a 1 dd to kill the boss not 5."* Two changes, and the first is his
  argument rather than his arithmetic: the 1.5 boss efficiency was justified *by* the five-way split he has
  now struck out, so priced for one damage dealer it becomes **2.0**, the top of his own "x1.2~2". The
  second is a factor the formula did not have at all — **what you spend waiting for the thing to come
  back**, measured against the world's own authored 22s trash cadence (`BaselineRespawnSeconds`), so
  ordinary trash comes out at ×1.00 and normal levelling does not move. A level-90 field boss goes
  **~6kk → ~24kk**, inside his "at least 20kk"; an elite gains ~29%. ⚠ The **exponent (0.25) is the one
  invented number** and the only knob: 1.0 would pay the wait in full, which assumes you stand at the
  corpse for thirty minutes. `M`-table `BL-49` in BalanceMatrix prints all of it, now with 89 and 90 rows.
- 🔴 **Dark Dominion is deleted.** *"it falls in the category for deletion."* Six E-grade pieces and a real
  set bonus that **nothing dropped, sold or boxed** — the last hand-authored set outside the generator, and
  the same category as `79e`'s 64 off-ladder items. The rule it leaves has no exception at all now: gear is
  **LADDER** or **TRAINING**, there is no third kind.

### What the screen tells you

- 🔴 **The drop tab applies the LEVEL-GAP PENALTY it always hid.** *"the drop value with double drop rune
  shows double chances ..the problem is there should be the same penalty as exp/sp when mob and player have
  a difference and that penalty is not displayed."* The kill roll has always multiplied by
  `LevelGapMultiplier`; the inspect list never did. 🔑 The rune was the tell — both are per-player scalars
  on the same roll, and showing one while hiding the other is worse than showing neither, because the
  visible one certifies the number as personal and it is then wrong by up to 100%. A header states the
  penalty when there is one, and says outright when a creature drops nothing for you.
- 🔴 **The target frame: the title bar IS the name.** *"put the name in place of `Target`. The title of the
  window to be the targets name"* · *"the current name text can be removed so the title window be smaller
  in size."* The duplicate name row is gone (the frame is 28px shorter) and the half-clipped type line is
  full width and rewritten to his format: **`Mob: 44, Aggressive, Social (wolf)`** / **`Player: Vagabond`**.
  ⚠ "Vagabond" is not waiting on a lookup — there are no player clans in the game yet, so every player
  genuinely is clanless.
- **The mob info sheet leads with BEHAVIOUR and drops what it never used.** *"Remove mobs unused info
  statuses; add info like -> agro:true/false, social: true/false, social clan: clanName."* Aggression, the
  social clan and the rank go **first**, because they are the only part of that sheet you need *before* you
  pull. A mob's mana and an all-zero Utility block are gone; players keep both, because another player's MP
  is exactly what tells a healer whether they can still cast.
- **The chat and combat windows move, resize and LOCK, remembered on the device.** *"a small button with a
  lock so it's locked in position and in size - persistent for the apk not the server"* · *"I want to be
  able to move the window side to side or on top of the other without they obscure my view."* A corner grip
  and an L/U button in the title bar; position, size and lock state live in `PlayerPrefs`. 🔑 Device, not
  server, and that is the right home: where a window sits is a property of the screen it is read on.
- **The chat input clears the camera cutout.** *"move the chat text box with 10-20 pixels more higher. Now
  it's the middle of the screen and it's under my front camera circle."* 🔑 It only reads as mid-screen
  **while typing** — the row lives at the bottom edge and the soft-keyboard lift is what puts it level with
  a landscape punch-hole. So the 20px clearance is added to the lift, not to the resting position.

### For him to author

- **The eight level-40+ CSV files exist now, seeded.** *"u can add files next to other skills 20-35.Csv the
  mele rogues one, one for archers, one for buffers and one for healers ..with what u have after 40 so I
  start with them later on."* `melee rogue` / `archer` / `healer` / `buffer`, each `40-74` and `76-85`, in
  the 40+ format (the 20-35 header plus a trailing `RACE` column), holding **exactly what the game already
  registers above 40** — nothing invented. Written by `tools/SkillCsvSeed`, which **refuses to overwrite**,
  so they are his from the moment he opens one. `docs/data/classes_skills_csv/README.md` explains the
  discipline mapping. ⚠ Four of the eight are nearly empty, and that is the honest picture.
- ⚠ **The break banner is at 10 MINUTES, temporarily, at his request** — *"change it to 10mins. (tag it to
  return to default 3h after test)"*, because `13a` has gone untested for six passes for the obvious
  reason. `GameConstants.BreakReminderSeconds`. **Put it back to 3h once he has seen it.**
- **`BL-74`** filed for the Game Launcher: everything a manifest can claim is already claimed, so the
  remaining variable is outside it (install source / Play category), and it needs his device to settle.

## 0.67.2 — 2026-08-14 — the Phantom gets Vanish back

His instruction: *"add vanish to the phantom - its not there cannot test the invis logic with non admin
invis"*.

**🔴 NEEDS A NEW APK** (protocol still **20**, no DB reset). The client builds its Learn tab from the
`ClassSkills` catalog it *compiled against* (`GameUi.Skills.cs`), not from a server push, so a learn line
added server-side is invisible to an older client. Server zip and APK both published at 0.67.2.

### `Vanish` was in the catalog, and nothing on earth could learn it

The 2026-08-10 **40+ purge** deleted every invented 3rd-class learn assignment — deliberately, and that
rule still stands. It took `Vanish` (with Shadowstep, Repelling Shot and Snare Trap) out of the Phantom's
kit while leaving the `SkillDef` in place, as the purge did everywhere.

The consequence was not noticed at the time: **`Vanish` is the only skill in the game with
`GrantsHide`**. So `BL-69`'s kind-1 hide — the one enforced by *omission from the snapshot*, the headline
of that whole feature — had no player-reachable trigger. Its **counter shipped without it**: Signal Flare
(`RevealsHidden`, `NoHideTicks`) is learnable on the rogue's own level-28 rung, answering something
nobody could cast. The only route into the code was the admin toggle, which is a different path and
proves nothing about the skill's own cast / break / reveal rules.

- One learn line: **`Vanish` at level 40** on the Phantom, the discipline's own floor and its identity
  beat (*"Vanishes, then opens with a devastating ambush"*). Registered for all three races to match the
  file's idiom; only the Elf can hold this discipline today, so the other two keys are inert.
- 🔑 Standing exactly as `BL-35`'s two level-83 skills do — **a skill he named individually, not a repeal
  of the purge.** Not licence to rebuild the rogue kit around it.
- ⚠ **Its SP price is left unset (the `SkillDef` default of 1).** That never mattered while the skill was
  unlearnable, so Vanish is currently the cheapest 40+ skill in the game by four orders of magnitude —
  Prowl @20 costs 3400, Signal Flare @28 costs 12000. Pricing it is 40+ balance and belongs in his CSVs,
  so it is flagged rather than invented.

Nothing else changed: the hide logic itself (`GameLoopService`, cast-completion break, aggro shed,
`NoHideTicks` gate) was already built and untouched.

## 0.67.1 — 2026-08-14 — a collect step never counted anything

Playtest-23, his first finding: *"the smiths quests (bring common ingots) dont count the ingots as
items… i kill mobs they drop or i added 200 with admin command - didnt increase the quest count 0/20
still"*.

**Server-only. Protocol stays 20; no DB reset. The APK does not need rebuilding** — nothing on the
wire changed, and the label check has not gated login since 0.28.25, so a 0.67.0 client talks to a
0.67.1 server fine.

### `QuestStepType.CollectItem` was declared, rendered, persisted — and never advanced

The enum member existed. The quest window drew its `0/20`. `CharacterQuestState.Counter` saved and
loaded it. But **no code anywhere in the server ever incremented it**: there was no item-side hook at
all, so the counter could only ever be the zero it was created with. A quest that reached such a step
stalled forever.

This is the *identical* hole `QuestStepType.ReachLevel` sat in until a quest finally used it — a step
type is not implemented by being in the enum, and the thing that hides it is that no quest exercises
it until one does. Here five quests did: **all five crafting professions are gated behind a collect
step**, so `BL-05`'s entire feature — every profession, every recipe, the whole grade ladder — has
been unreachable by normal play since it shipped in 0.63.0. Only the debug join path worked.

- **`AdvanceCollectQuests` is hung off `SendInventory`**, the one funnel every item gain and loss
  already pushes through. Same shape as `SupplyStepItems` on `SendQuestLog`: a future source of items
  (a drop, a craft, a trade, a warehouse withdrawal, `/give`) cannot forget to credit a collect step,
  because it cannot forget to show the player their own bag. It is also re-checked on **accept** and
  after a **talk** step, for the case where the master finishes his pitch and you are already carrying
  the twenty.
- 🔑 **The counter reads the BAG, not a tally.** Mats leave a bag — sold, salvaged, spent on a craft —
  as easily as they arrive, so a counter incremented on pickup would drift the first time one is spent
  and then lie about a step the player can no longer satisfy. `Summarize` and `BuildQuestEntry` both
  read the live count, so the two windows cannot disagree.
- 🔑 **Nothing is consumed in the field.** A collect step is a *hold* requirement; the master takes the
  mats when he hires you, in `CompleteQuestAtNpc`. Items must not evaporate out in a field the instant
  the 20th one drops. The turn-in re-checks the hold first and puts you back on the collect step if
  the pile is gone, so selling the ingots on the way in does not buy a free profession.

⚠ Existing characters need no repair: a stalled quest is stored as `StepIndex` on the collect step, and
the first inventory push after login — login sends one — credits it against what the bag already holds.

## 0.67.0 — 2026-08-14 — three of the four he ruled on

Crafting is **parked on his instruction** until he can test it properly (*"leave the salvage/mats etc
craft until I'm able to test it fully… that's a single playtest only for this"*), so this pass is the
polish he named instead. Protocol stays **20**; **no DB reset**.

### `BL-43` — NextTarget: retaliation outranks distance

His original note, deferred at the time: *"Need NextTarget (targeting closest/retaliate 5 and cycling
through them)."*

Half of it was already there — `TargetClosest` cycled the nearest living mob within 2500 and stepped
outward on each press, on the `ActionTargetClosest` hotbar action. What was missing is the half the
note leads with. Now:

- **Anything that has hit you in the last 10s sorts ahead of everything that has not**, and only then
  does distance decide.
- The ring is capped at **5**, his number — a cycle over every mob in a 2500 radius is not something
  you can tap around without looking.

🔑 This is the *manual* twin of the autopilot fix from the same playtest (*"a mob hitting you is higher
priority than nearest… I'm getting ganked by orc archers and still kill the nearest"*). That one taught
the autopilot to retaliate; the tap-to-cycle selector had the identical hole and kept handing you the
nearest idle mob while something else chewed on you.

🔑 **Client-only, no protocol change.** The combat feed already carries every blow landed on you with
its attacker's id, so the client can keep its own short retaliation memory. It is deliberately NOT
shared with the server's `RetaliationTarget`: that one picks what the autopilot will *fight*, this one
picks what the player is *looking at*.

### `BL-46` — the app is a game now, and the second icon is gone

*"Since my phone updated it didn't appear. Now I'm using Secure Folder so I can have 2 clients side by
side. Need only to be able to make it as a game — treat it as a game — to enter the game launcher on
its own and to be able to use the game boost features."*

The dead `UnityPlayerActivity` block is **deleted**. It was kept from 2026-08-02 as the duo-testing
rig, but that reading was half wrong and the device has now settled it: our Application Entry is
GameActivity, so that activity merged in disabled while still keeping its LAUNCHER filter — it drew a
second icon and nothing else. The two independent clients were always **Secure Folder** (a Samsung
profile clone with its own UID and data dir), exactly as the old comment suspected. Deleting it costs
no test capability.

🔑 **That deletion is what makes the game-mode hint work**, which is why the two halves shipped
together: a game launcher classifies an *app*, not an icon, and a package declaring two MAIN/LAUNCHER
entries is ambiguous to it. `android:appCategory="game"` was already present and inert; it is now joined
by the older `android:isGame="true"` for the One UI builds that still read it, with exactly one launcher
activity behind them.

### `BL-49` (part) — a boss pays for the time it costs

*"Bosses should give exp based on how long it takes to kill a normal mob vs boss (x1.2~2) — killing a
boss gives you twice (or 1.5) the exp for the same time of normal fighting. Something like that, not a
real formula to calculate it, just a curve to have."*

EXP and SP are now `base × killTimeRatio × rankEfficiency`, where the efficiency is **1.2 elite / 1.5
boss** — his range, and the one number the whole design reads off: *an hour spent on bosses is worth
1.5 hours spent on trash.*

🔑 **A time RATIO needs no simulation and no per-boss authoring.** Time-to-kill is `EHP / yourDPS`, and
this compares two mobs at the same level against the same player — so your DPS cancels completely and
nothing about the killer enters the number. It is `HP × P.Def` off the spawned entity, so rank
multipliers, MobMod HP passives and buffs a mob is standing in are all already counted. Only the HP half
moves today (rank scales HP and P.Atk, not defence); the defence term is there so that the moment a boss
is given real defence its EXP follows on its own.

🔑 **This fixes a silent five-fold underpayment.** The old rule was HP-only *and clamped at 20×*, while a
boss carries **100×** HP — so every field boss in the game paid a fifth of what it owed, and paid exactly
the same as a mob merely 20× bulky. That clamp is what "the elite/boss EXP multiplier wants a look" was
about. Boss EXP goes **20× → 150×**; elite **4× → 4.8×**.

🔑 **1.5 and not 2.0 for a boss, because a boss is fought by a PARTY.** Five people kill it five times
faster and split the pot five ways, so the efficiency each of them sees is exactly this constant — the
party size cancels, and 2.0 would make boss-camping strictly dominant over every other way to level.

⚠ The world boss still has no rank of its own. It lands here as a `Boss` at 1.5 and is paid for its real
bulk by the ratio, which is the correct behaviour — but it remains the open half of `BL-13`.

**Measured, not derived** (`tools/BalanceMatrix`, new `BL-49` table): `exp/sec ×` reads 1.20 / 1.50 on
every row at levels 20/40/60/76/85, which is the assertion that the time ratio and the payout have not
come apart. ⚠ The same table surfaces something for him to look at: one **level-20** field boss is now
**125% of a level** solo, while a level-85 one is **0.1%**. Both are the same 150 trash kills — that
spread is the levelling curve, not the boss rule, and it is the part of `BL-49` still open.

## 0.66.0 — 2026-08-14 — his eight rulings, built

On 2026-08-14 he ruled on **all eight remaining "ready to build" backlog items in one message**.
This is those eight. Protocol **19 → 20** (one new hub method); **the DB schema changed**, so
`Game.Server/game.db` (and `-shm`/`-wal`) must be deleted and recreated.

### `BL-20` — a partial Blessing Box pick keeps the box for the rest

*"I'll want to be able to pick 5 and I get my 5 scrolls + the box for the other 5."* — **"is OK"**.

Taking fewer than the full ten used to be **refused outright**. That refusal was itself a fix: before
it, a partial pick consumed the whole box and silently forfeited the rest (playtest-19 `48g` — 7 of 10
from a 250k box). It was the right answer only while there was nowhere to put the leftovers.

Now there is. The picks live on the **item instance** (`InventoryItem.PicksRemaining`), so the box
simply stays in the bag with a smaller number on it and is consumed when its last pick is spent. No
`box_scrolls_5` family of defs, no second item handed back, and no free inventory slot needed at the
moment of the split — and the InstanceId never changes. Re-opening the box offers what it still owes,
and the client's counter reads `0 / 5`.

🔑 The remainder is decremented by what was actually **granted**, not by what was asked for, so picks
lost to a full inventory stay in the box rather than evaporating — the same failure `48g` was about,
one layer in.

### `BL-22` — trash disassembles into crafting materials

*"rarity for mats rarity, grade for mats ammount"* and 🔑 *"u give up gold to get mats"*. Any unworn
piece of tiered gear now has a **Break down** button: the item's **rarity** is the material's rarity,
its **grade** decides the amount (`Crafting.SalvageQtyByRung`), and its own maker's material decides
the type — a blade into Ingots, plate into Leather, a robe into Thread, a ring into Gems.

It is an **alternative to selling, never a bonus on top of one**: nothing here pays gold, and the
gates are deliberately the *selling* gates, so an unsellable bound item cannot be laundered into
tradable materials.

**🔴 The finding, and it needs his ruling.** His approval came with a budget — *"now as 347h for fully
geared if we add the disassembly this should not go to 20h .. 10~20% decrease in time should be ok"* —
and **the S row cannot be moved by this feature at all**, at any tuning:

| rung | before   | after    | change   |
| ---- | -------- | -------- | -------- |
| E    | 5.7h     | 5.5h     | −3%      |
| D    | 26.3h    | 23.8h    | **−10%** |
| C    | 20.1h    | 16.5h    | **−18%** |
| B    | 47.7h    | 47.5h    | −0%      |
| A    | 65.3h    | 65.3h    | −0%      |
| S    | **347h** | **347h** | **−0%**  |

Because *"rarity for mats rarity"* means salvage can only pay the rarity of the gear that **drops**,
and gear rarity is capped by **rank, not band**: a normal mob stops at Epic and an **elite stops at
Epic too**; only a BOSS drops Legendary or Mythic gear, at 0.09 kills/h. The A and S recipes bind on
**Legendary Ingot**, which salvage therefore never produces. Measured, not argued: at a uniform
quantity of **20** the early rungs collapse to −24/−39/−72% while **A and S still move 0.00%**. The
quantity knob is not the binding constraint; the rarity mapping is. His three options are printed by
the new `M13` section; option 1 — accept it as a mid-game feature — is what ships, because the other
two change things he did not ask to change.

⚠ **A real bug in `tools/BalanceMatrix` was found and fixed while measuring this.** `RecipeHoursDetail`
only tried the refine path when the direct rate was *exactly zero*, contradicting its own header
(*"the cheaper of the two wins per ingredient"*) and making the model **non-monotonic** — adding a
trickle of a material the band never dropped replaced a cheap refine path with an expensive direct
one, so a strictly larger faucet came out as a *longer* farm. The first run printed D at **+286%**.
Both paths are costed now and the minimum wins. This also moved C's baseline to 20.1h (was 25.0h).

### `BL-27` — `Robe 611` finally has an item

🔑 *"build as u wish - I haven't gotten to the part that I need or drop so it's not of a difference
yet."* The last authored gear row with nothing behind it. Taken literally off the CSV — WIT +2,
INT −2, SPT +2, Speed +7 — as `set_robe_t61_sup` / `robe_t61_sup` ("Bloodsteel Raiment").

⚠ The one clause needing a reading, *"Stun/Fear Resist x1.7"*, is **not** a guess: it is the same fold
he already accepted on the other two `611` rows (heavy and light), both of which carried that exact
wording and both of which ship as `CcResist 0.4`. Identity: the base 61 robe is the caster line
(Cast ×1.15, SPT −1); INT −2 with SPT +2 inverts that trade, so this is the tier's **support** robe —
the 40 Warden / 52 Sage line continued at B, the one rung it was missing.

### `BL-34` — "Madness", the party Frenzy, at 76 on the buffer

🔑 *"put it at 76 on the buffer"*, explicitly so **an admin can party-buff with it** now — *"and when
the kits land we will move it"*. A deliberate temporary home at the top of the Warchanter's existing
40-74 ladder, which is the only 76 slot the game has; the debug admin is a level-90 Warchanter, so it
is castable the moment the server boots.

A thin party wrapper handing out a **new rung 7** of the Frenzy family, so it outranks and evicts any
weaker Frenzy the party is wearing and nothing can override it after. ⚠ One number is invented and
flagged in the source: his penalty stride is perfectly regular (−0.04 a rung → 0.06), but the **gain**
steps only on even rungs, which would leave the top rung differing from the one below by the penalty
alone. It takes the step: **−6% Max HP/MP, +9% offence and speed, +9 move, −8 evasion**.

### `BL-35` — the two level-83 preservation skills

Both carry Angel's whole effect (buffs survive death) **plus the auto-resurrect that nothing in the
game used until now**:

- **Rite of Preservation** (Lightbringer) — cast on an ally; **they** rise where they fell, 100% exp
  returned, 1h duration, 1h reuse.
- **Undying Will** (Bulwark) — the **self** version.

🔑 The RANKS are not invented: the Angel's Protection comment has said since 2026-07-17 that the
healer's target auto-res is **Rank 2** and the tank's self auto-res **Rank 3**, both above Angel's
Rank 1, all on the shared `buff_preservation` key. The exp return rides on the **buff**
(`ActiveBuff.AutoResExpPct`), because by the time you die the caster may be across the map or a
different class; the death penalty still applies first, so a 100% skill nets to zero — his rule is
*"you die, you have the penalty"*.

⚠ **An explicit, named exception to `BL-02`.** He authorised these two skills and only these two; the
rest of both kits stay unregistered. ⚠ His own note on the 1h/1h numbers is *"(not fixed)"*.

### `BL-36` — the subclass swap rules

Out of a town: a **5-minute wait**. In a town or peace zone: **instant, no cd**. Both require being
out of combat. The machinery already swapped fine — the comment above it has said since it was written
that the rules would gate the *command*, not the mechanism, and this is that gate.

🔑 The clause that shapes the whole method: *"When changed out if town and 5min start to count and
enter in town the countdown stays … w8 the 5mins then change (city don't trigger the cd) both waits
it."* So the pending-swap check sits **above** the safe-zone fast path — reversed, walking into the
nearest town would skip the wait. The town rule decides whether a timer *starts*, never whether one
finishes. ⚠ Mine, not his, and each is one line: re-asking reports the time left, asking for a
different class mid-count is refused, and a death cancels the change.

### `BL-42` — skills and passives describe themselves with real numbers

*"all skills and passive should show the desctiption with numbers."* The gap was structural, not
cosmetic: the `SkillEffect` flag enum has been **full** for years (`1L << 62` was the last bit), so
every mechanic since has been added as a plain **field** — `Resurrect`, `KeepsBuffsOnDeath`,
`Lifesteal`, `GrantsHide`, `PlacesTrap`, `Rewards`, `TauntPower`, `BlockAccuracy`, the fixed-timing
flags. The card reads flags and magnitudes, so **none of it ever appeared**. Angel's Protection is the
clearest case: its entire payload is one bool, so its card could say nothing at all about what it did.

`SkillText.Mechanics(def, level)` states all of it, per level, and it is wired into both the detail
card **and** the Learn preview — where several of them are exactly what an upgrade buys (Resurrection
walks 25 → 50 → 75 → 100%, a taunt 1500 → 5100). The **conditional** lines he asked about now carry
their condition: "Block chance (with a shield)", "Bow range (with a bow)".

⚠ Same authoring rule as `SkillText.Mods`: a field added to `SkillDef` needs a line here in the same
commit. Nothing fails loudly when it is missing — the skill just quietly stops describing itself.

### `BL-59` — resurrect / party / PvP-flag rules, re-specced

🔴 He **re-specced this entirely**; the old entry was self-based (*"you cannot res a party member while
YOU are flagged"*) and is superseded. The new rule is **target-based**:

| situation                                       | rule                                     |
| ----------------------------------------------- | ---------------------------------------- |
| single-target support of a **non-party** player | allowed **if they are not pvp/pk**       |
| target **is** pvp/pk                            | allowed **only** from inside their party |
| supporting a still-flagged player               | 🔑 **flags you**                          |
| party invite to a pvp/pk player                 | allowed                                  |
| trade                                           | allowed with **pvp**, **never** with pk  |
| res in the same party                           | allowed for **both**                     |

⚠ This **opens** something that used to be shut: support was party-only, and anything else fell
through to a self-cast. Helping a passing stranger is legal now, and the flag is what prices it —
which is the whole point of moving the test from the caster to the target. The supporter-flagging that
already existed for heals and MP now also covers **buffs and resurrects**.

Trade used to refuse **both** purple and red, which made a 60-second flag a trading ban a player
earned by defending themselves. Karma is the sentence, so karma is what blocks a trade.

The third part of the old entry is built too: the **Ultimate Scroll of Resurrection is tradable** —
*"atleast the one that drop and from the admin menu"*. It costs the tutorial nothing, because the
completion kit hands out the separate `_bound` clone. ⚠ Its 15,000 Value is mine: leaving it
unsellable would recreate the exact "tradable yet refused at the counter" complaint recorded one line
above it in the catalog. No vendor stocks it, so no faucet opens.

## 0.65.0 — 2026-08-14 — the buildable-backlog batch

### `BL-11` — the anti-magic / anti-physical pair, and the mRes channel it needed

*"We had a anti magic mobs (lower pdef more mdef) and anty physical (less m def more pdef) — this
should feed your mres passive."* Two things were missing, not one.

**The channel.** A mob template could only raise **M.Def** — a flat divisor a levelling mage simply
out-scales. `mRes`, the *percentage* channel the mob ladder is written in and the one every player
anti-magic passive already reads, had no mob-side route at all. `MobMod.MagicResist` is it, plus a
**Magic Resistance** track in the mastery layer (the same twelve rungs and the same neutral as the
three weapon resists — it is literally the CSV's *"???? Resistance … the same logic for all other
resistances we will have"* row, filled in). A **negative** value is a magic WEAKNESS, which is what
makes the anti-physical half mean something rather than just "no resist".

**The mobs.** The pair existed as a comment on `MobMod` and as exactly **one** template (Watcher Eye),
and no mob in the game was anti-physical. Two shared presets now carry it, so it reads as a pattern
instead of eight hand-tuned numbers:

|                              | P.Def | M.Def | mRes     | who                                                   |
| ---------------------------- | ----- | ----- | -------- | ----------------------------------------------------- |
| **Warded** (anti-magic)      | ×0.8  | ×1.5  | +20%     | Grave Lich 44 · Aether Wisp 58 · Spiteful Ghost 66    |
| **Ironhide** (anti-physical) | ×1.5  | ×0.8  | **−20%** | Shield Skeleton 20 · Fomor Brute 45 · Dread Knight 65 |

Watcher Eye (26) keeps its own steeper 2.0/0.5 — it is the archetype's namesake — and gains the mRes
half. Obsidian Knight (63) takes a Magic Resistance **L5** on its existing Stoneplate mastery, so the
golem that already resists arrows and blades is the one a mage answers.

They are spread 20 → 66 on purpose: Shield Skeleton is early, where "bring the mage" is teachable,
and two of the three Ironhides are dungeon bosses.

### `BL-14` — the weapon a mob holds now decides its per-hit damage too

*"didn't we gave monsters weapon types? Archer is slower but does more dmg, the fast attacking have
more crit rate and more atck speed but less dmg."* Two of his three clauses were already true — a
mob's attack SPEED and CRIT RATE have come off `InnateWeaponType` since 2026-08-10. The third was
not, and its absence was a real defect.

A **player** gets the trade free from the weapon ITEM: a 2H sword carries more P.Atk than duals, so a
slow weapon buys per-hit damage. A mob has no item — its P.Atk is one level curve — so handing out
weapons changed only the attack RATE. A club mob became **12% worse than a claw mob at nothing**, and
the fast attacker was strictly better instead of trading. `StatCalculator.MobWeaponPowerFactor` is the
missing half: `433 / weaponBaseSpeed`, referenced on the DUAL's 433 because that is the speed every
mob in the game was pinned to *before* the weapon change — so this is DPS-neutral against the pin,
nothing is nerfed, and the mobs that were silently slowed get their lost damage back as hit size.

Measured at level 40 against a same-level geared champion (`BalanceMatrix`, new section):

| weapon       | atk base | pwr × | P.Atk   | crit      | dps  |
| ------------ | -------- | ----- | ------- | --------- | ---- |
| Dual (claws) | 433      | ×1.00 | 171     | **13.2%** | 16.5 |
| Sword        | 379      | ×1.14 | 195     | 8.8%      | 16.4 |
| Blunt (club) | 379      | ×1.14 | 195     | 4.4%      | 15.8 |
| 2H Sword     | 325      | ×1.33 | **227** | 8.8%      | 16.0 |
| Bow          | 293      | ×1.00 | 171     | 13.2%     | 11.2 |
| none         | 300      | ×1.44 | 246     | 11.0%     | 16.3 |

The DPS column is **flat** — that is what makes it a trade — while hit size and crit rate diverge
exactly the way he described.

⚠ **BOW is ×1.00 on purpose.** An archer mob already pays this precise trade explicitly in its ROLE
(`MobRole.Archer`: P.Atk ×2, 450 range, −15% P.Def), so charging his one sentence twice would put an
archer at ~3× per arrow. If the role's ×2 is ever removed, `MobWeaponPowerFactor` is where it goes.

### `BL-13` — the boss check, answered with numbers (and nothing changed)

*"boss had 260? He should have 520? Check."* **Checked: the multiplier is landing.** The boss rank's
HP ×100 survives every recompute (that was the playtest-20 #7 `ApplyMobScale` rework), and a level-20
field boss spawns with exactly 36,000 HP = `MobBaseStats.Hp(20)` 360 × 100. Nothing is being eaten.

But measuring it against your **six-minute, 3-DD** target found a different problem, and it is bigger
than a multiplier. New `BalanceMatrix` section — three geared champions, no downtime (so these are
*ceilings*; a real fight is slower):

| Lvl | boss HP | 3-DD dps | TTK      | vs your 360s   |
| --- | ------- | -------- | -------- | -------------- |
| 20  | 36,000  | 448      | **80s**  | 4.5× too fast  |
| 40  | 132,000 | 446      | 296s     | about right    |
| 60  | 292,000 | 427      | **684s** | ~2× too slow   |
| 76  | 466,000 | 525      | **888s** | ~2.5× too slow |
| 85  | 582,000 | 840      | 693s     | ~2× too slow   |

**A single flat ×100 cannot hit 360s at every level**, because mob HP grows as `0.8·L²` while a geared
party's DPS is nearly flat across the game (448 → 525). The boss rank's difficulty therefore swings
**11× between level 20 and level 76**. Hitting your target needs a level-shaped multiplier — the table
prints the one each level would want (×448 / ×122 / ×53 / ×41 / ×52).

🔵 **Nothing was changed.** Which curve a boss should follow is a ruling, not a fix, and the numbers
above are what you need to make it. Two things to decide: whether a level-20 field boss really should
take a level-20 party six minutes, and whether the late-game bosses come DOWN to 360s or the target
itself rises with level.

🔵 **The world boss has no rank to live in.** `MobRank` is Normal / Elite / Boss; the only thing
separating your 21-hour spawn from a 30-minute one is the respawn timer. *"An hour for ~10 parties
(~50 DDs)"* is ~16.7× the party over 10× the time = **~167× a field boss's HP** — a new rank with its
own drops, phases and lockout, not a bigger number. Not invented.

### `BL-06` / `BL-07` / `BL-08` — the three skill-defence channels

His `69e` answer plus the "New formulas" block under it, built as one thing because they are one
thing: what happens to a SKILL aimed at you, as opposed to a basic attack or a spell.

**`BL-06` — a physical skill is no longer evaded at all, by default.** *"normaly no1 can evade a
physical skill … now on then i miss a skill which is anoying — stab fails… then stab should land but
misses … no1 evades only rogues gets a floor while in an ulitmate 25%."* The
accuracy-vs-evasion roll is **gone from the physical-skill branch entirely** — with it went the
caster's accuracy, the warrior's `Precision` hit floor and the rogue's `EvadeFloor`, none of which
have any say over a skill any more. All three still govern basic attacks, untouched.

What replaces it is a single defender-side grant, `Entity.SkillEvadeChance`, and **the rogue's
Evasion Boost is the only thing in the game that sets it: 25%, for its 30s**. That also resolves the
CSV's long-unbuilt *"skill evasion x1.25"* — it was never a multiplier on anything, it was the 25%.

- 🔵 **The 40% rung is not built**, deliberately. It is the second half of his *"25%,40%"* and there
  is no rung to put it on: `rogue 20-35.csv` authors Evasion Boost as a single level, and adding one
  would re-spec his data. Same for *"76lvl the physical phantom gets a 90% for 15s"* — a 4th-class
  Phantom skill. Both are `BL-02` (the 40+ kits).
- `SkillMath.PhysicalSkillAccuracyBonus` (+10 accuracy on the skill miss roll) is **deleted**: it
  softened a roll that no longer happens.

**`BL-07` — physical skill reflect: `Deflection` (warrior).** *"default warrior @40 → 0.15 chance ×1
reflected; @76 → 0.3 chance ×1 reflected."* His numbers verbatim, and his own pick between the two
shapes he offered (*"a 100% chance to reflect 15% p skill dmg, or 15% chance to reflect 100%"*) — so
the fraction stays ×1.0 at both rungs and only the chance moves. A landed physical skill rolls the
victim's chance; on a hit the full damage goes back at the caster, who can die to it.

Kept **separate from `MeleeReflect`** (the armor sets' 5% counter-to-vampirism), which fires on basic
attacks only — no blow is ever taxed by both. A reflected skill is applied directly, never through
the skill pipeline, so two Deflection warriors terminate after one bounce.

**`BL-08` — debuff reflect: `Backlash` (tank), 30%.** *"tanks get 30% chance to reflect a debuff → u
cast on tank he reflects u get the debuff."* Rolled **before** the land contest on both debuff paths
(contested CC and the fizzle-model debuffs), because a bounce is not a resist: a tank who throws your
stun back was never tested against it. The caster gets the effect with no resist roll of their own and
no second bounce.

⚠ **One number here is mine, not his: WHEN a tank gets Backlash.** He gave the 30% and no level. It
is granted at the **3rd class change (40)**, to sit beside Deflection, which he did date. Moving it to
the 2nd class change is one line in `SkillCatalog.ReflectPassiveFor`.

Both are auto-granted like the three identity floor passives, but on **their own ladder**
(`ReflectPassiveFor`, 40 then 76) rather than the floors' 20/40/76 — the two do not line up, and
folding them together would have handed a level-20 warrior a reflect he was never promised. All three
new channels fold by **MAX**, like every other guarantee, so two sources never compound.

## 0.64.0 — 2026-08-13 — the playtest-22 batch

Protocol stays **19** — nothing here changes the wire. No DB reset.

### `BL-68` — every 16-40 band now exists four times

*"Add several new zones to duplicate the 16-20, 20-24, 24-28, 28-32, 32-36, 36-40 (all the Stonewatch
zones) ... `north` and `south` zones to have 4 of each."* Nine new fields, eighteen new camps: the
bands are identical, only the ground is new. The point is somewhere else to farm at your level, not a
longer ladder.

**They go east**, which is his instruction (*"the bot side fields can be extended ... increased ~4
times in width (to the right)"*) and also the only direction with room — Brackenford sits 14000 due
south of Stonewatch, Frostmere 13000 west, and north is the Training Outpost and its dummy row. East
is 22000 units of empty map.

They sit on a **3 × 3 grid** at x ≈ 31000 / 36000 / 41000, each north-south lane keeping the original
field's shape (low band nearest the city, high band furthest out):

|           | x ≈ 31000           | x ≈ 36000             | x ≈ 41000                |
| --------- | ------------------- | --------------------- | ------------------------ |
| y ≈ 6500  | Sunward Moor 16-24  | Highstone Ridge 24-32 | Emberdust Barrens 32-40  |
| y ≈ 12000 | Thornfen Moor 16-24 | Ravencrag Ridge 24-32 | Palewind Barrens 32-40   |
| y ≈ 17500 | Mistlow Moor 16-24  | Bleakspur Ridge 24-32 | Cinderflat Barrens 32-40 |

**The city was not moved.** He offered to (*"The whole City can move to the right"*) and it turned out
not to be needed: the generator places a field by bearing and distance, so more ground is a matter of
more distance. Not moving it avoids relocating a town every player already knows and stranding every
character saved standing inside it.

The geometry is **not hand-derived**. `ValidateLayout` fails the boot on any camp that touches a town
wall, another camp or another field, and prints the shortfall in units — the first attempt at this put
two of the outer fields into Brackenford's wall and its east field, the validator said so at boot with
the exact numbers, and the layout was re-aimed onto the eastern grid.

⚠ Stonewatch's gatekeeper now lists **12 fields**. That is a long menu on a phone, and `BL-41`'s
question about a grade filter on the craft page is the same question in a different window.

### `BL-65` — the dungeons get level bands, and the old one had a real cause

*"Now a 32 lvl mobs almost next to a 65 lvl which protect the 44 lvl boss ... The mob lvls are all
over the place."* Those were the literal numbers, and the cause was one line in `SpawnMobFor`: **a
mob with a NATURAL level brings its own**, and a spawner's `MinLevel`/`MaxLevel` is then only a
label. The crypt's roster was `hollow_one` (58), `grave_robber_fighter` (32) and `dread_knight` (65)
— three unrelated levels wearing a "44-48" sign.

So the fix is the **roster**, not the sign. Every room is now stocked with creatures whose natural
level sits in its band, and the band written on the gate agrees with what actually spawns:

| Dungeon                            | Rooms | Boss                        | Entrance gated to |
| ---------------------------------- | ----- | --------------------------- | ----------------- |
| **Hollow Crypt** (unchanged place) | 39-42 | Grave Lich **44**           | Greymarsh         |
| **Sunless Warrens** (new)          | 58-64 | Dread Knight **65**         | Ironreach Keep    |
| **Ashen Sepulchre** (new)          | 80-85 | Disciple of the Dawn **90** | Frostmere         |

His layout exactly. The level-90 boss is the one spawner in the game's three dungeons that forces its
level — nothing is authored above 85 — which is the same deliberate reuse the 85-90 field already
runs on, not a fallback.

Both new dungeons are the crypt's **outline translated** (10k and 22k south-west). That is on
purpose: it is a known-good narrow diagonal band that the wall clamp, the entrance annex and the
straight-line move order have all been measured against (`WorldDomain.OfDungeon`). Three invented
cave shapes would have bought nothing and re-opened all three of those questions.

Each entrance is gated to the city whose band contains the dungeon's, for the reason the crypt
already is: a safe zone is otherwise a destination on **every** gatekeeper's list, and a level-1
should not be offered the level-85 vaults beside his first hunting field.

⚠ Side effect worth knowing: the Sepulchre's rooms are **elites at 80-85**, so they feed the
`EliteMatDrops` faucet that 0.63.0 added for Epic/Legendary/Mythic crafting materials. That is a
second high-level elite field, and it makes the top of the crafting ladder measurably less scarce
than the numbers in `docs/balance/CraftingMats.md` assume.

### `BL-69` — invisibility, in his three separate kinds

His spec is explicit that these share a word and nothing else, so they are three pieces of state,
not one flag with modes.

**1 · Hide** (`Vanish`, the Phantom's) is now actually hidden. It used to mean "invisible to mob AI
targeting" and nothing more — every other player still saw and could click you. A hidden character
is now **withheld from the world snapshot itself**, which is the whole of *"a buff nobody renders,
targets or checks as nearby"*: the client never receives them, so it cannot draw them, click them or
hold them in its nearby list, and the existing despawn diff means everyone who could see you is told
you left the instant you hide. The server checks visibility again on both target paths, because a
target id a client is still holding from a moment ago would otherwise sail straight through.

**Anything but movement ends it** — a hit, any skill, a potion, damage taken. That last one is what
makes his *"any AoE damage also reveals"* true with no special case in any AoE: an area hit is
positional, so it finds a hidden character, and the hit itself drags them out.

🔑 **The reveal is at EXECUTION, not at the click**, which is his rule and the reason a gap-closer
works: *"i want to click the skill and im not in range to start to move towards the target but still
invisible once the skill is executed then i appear."* So the break sits in `ExecuteSkill`, past the
point of no return, and nothing on the cast-**start** path is allowed to touch a hide.

The **counter** is `Signal Flare` (rogue/bow, 28): a non-damaging area cast that reveals every hidden
character within 300 **and bars them from hiding for 30s**. The second half is the part that makes it
a counter — stripping a hide the rogue re-casts a heartbeat later is an inconvenience, not an answer.
It deals no damage on purpose, so sweeping an area for a rogue does not also wake a mob clan.

**2 · Stealth** is a different thing and is now built as one: it hides you from **unaggroed monsters
only**. Players still see and can target you, anything already chasing keeps chasing and hitting, and
it does **not** break when you act — only when you stop it. Two deliveries, one mechanic:
- `Prowl` — a rogue **toggle** at 20, **1 MP/s**, no cast. His purpose for it, verbatim: *"toggle-on
  makes the rogues farm in peacefull zones."* Toggles now support a per-second MP upkeep and drop
  themselves when the caster runs dry.
- `Shrouding Hymn` — the buffer's party version at 30: **1 minute, 30s reuse, 300 MP**, his numbers.
  The price is the point — 300 MP at that level is most of the bar, so it is a journey, not a rotation.

It rides on the **buff** rather than on the entity, so every way a buff can leave — toggled off,
double-clicked, dispelled, expired, lost on death — ends the stealth, with no second bookkeeping path
to fall out of step.

**3 · `/invis`** (admin) is absolute: nothing in the simulation ends it, not acting, not an AoE, not
the flare, and it hides him from other staff too. It goes off when the command is typed again. He is
still *hittable* by area damage — `/god` is the separate switch, which is his own distinction.

**Hidden means hidden from EVERYONE** — party and staff included (his ruling, 2026-08-14: *"yes a hide
hides you from all ... Also it hides you from the staff as well"*). This shipped the narrow way first,
exempting the hider's party and staff on the reasoning that a party member no healer can reach is a
bug report; he overruled it, and his answer disposes of the objection: **you cannot die hidden**,
because taking or dealing damage reveals you before it lands. Death clears a hide too, so a corpse
stays findable and resurrectable.

🔑 **You are not removed from the party or from anything else** — you *"act as u r not nearby"*. The
roster still lists you; what goes is being renderable, clickable and heal-targetable. So a hidden
member is skipped by `PlayersInRadius` (party heals and party buffs), by both auto-heal/auto-mana
target pickers, and by the manual ranged-ally cast, which falls through to a self-cast exactly as an
out-of-range member already does. The party window shows them as **`Hidden`** — the roster already
dims any non-Online row and prints the status, so this is one enum value and **no protocol change**:
the wire shape is untouched, and a client built before this prints the number instead of the word.

**Staff lose sight, not control.** `/tp`, `/tpme`, `/jail` and `/where` resolve a character by NAME
and never consult visibility, which is deliberate — *"they still can teleport them self on you or you
on them or can jail you ... for the 30 sec you are hidden they will live with it."*

### `BL-70` — mobs have a social circle, and the rogue has a way around it

A creature can now belong to a named **clan** (`MobType.Clan`) — `orc`, `mantis`, `redhorn`,
`wildhorn`, `radiant`, `drake`, `skeleton`, `dread`, `mirror`, `lizardman`, `marauder`, `wolf`.
Damage one of them and every clanmate within **450** joins the fight, seeded with the same threat a
pull is worth so the person who started it owns the whole camp instead of whoever hits each mob
first. The radius is deliberately **wider than a mob's own 400 aggro range**: a camp that answers
only as far as it can already see you is not a camp, it is four independent mobs.

Clans are authored on the families that already read as a warband or a nest — the ones sharing a name
root, the same grouping his "ork settlement" picture describes (`BL-21`). Everything else stays
clanless on purpose: a bear, a treant and a lone medusa have nobody to call.

🔑 **The trigger is DAMAGE and nothing else**, which is his ruling and is the whole design rather
than an implementation detail: *"social circle only works if a mob is hit, not when
taunted/debuffed/aggroed/etc."* Two further limits keep a camp from becoming a zone-wide riot — a
clanmate already fighting somebody is left alone, and the mobs that answer a cry do not cry in turn.

**The rogue gets `Lure`** (20/28/36), which is what the damage-only rule exists to permit: a taunt
that does no damage, so a camp never learns it happened. Power **500** — far below the tank's Provoke,
because a lure is how you *start* a fight, not how you keep a mob off the party. Its ladder is pure
**reach: 200 / 400 / 600**, his numbers, and level 3 out-ranges a mob's own aggro so a level-36 rogue
can pull without stepping into the camp's notice. It is **mob-only** and refuses a person out loud —
the taunt handler would ignore a player target anyway, and a skill that silently does nothing is a
bug report.

(New plumbing: `SkillLevel.Range` for the one ladder that is reach, and `SkillDef.MobTargetOnly`.)

### `BL-71` — the threat model gets numbers, and a pull is finally worth something

The answer that opened this was that **most of it already existed**: `Entity.Threat` is a real
per-attacker table, `RetargetByThreat` picks the maximum on every damage tick, aggro is damage 1:1,
and `provoke` genuinely taunts. What was missing was everything that makes those facts *authorable*.

**Taunt POWER is now a number on the skill** (`SkillDef.TauntPower`, per-level via `SkillLevel`).
A taunt does two separate things and they are no longer the same thing: it puts you at the **top** of
the table and locks the mob there for `DurationTicks` (Provoke: 3s), and *then* adds its power as the
**cushion** that decides whether you still hold the mob when that window closes. Because threat is
damage, the number reads literally — a 5,100 taunt means another player must out-damage the tank by
5,100 to take the mob back.

The old rule was `top × 1.2 + 100`, identical at every level and for every taunt ever written. That is
not something anyone can author against, and 20% of the top is a rounding error once a DD lands 7-8k a
skill — which is the complaint this came from.

**Provoke became a ladder** on the tank's existing 20/24/28/32/36 cadence: **1500 · 2000 · 2800 ·
3800 · 5100**, anchored on his two endpoints (*"1000-2000 at L1"* → *"20-30k"*). It is a ×1.36 step,
which continues 7000/9500/12900/17500/23900 and lands inside 20-30k at skill level 10 — those five
rungs belong to the 3rd/4th-class kits and wait on his CSVs (`BL-02`), like every other 40+ number
here. Level 1 keeps the SP price it has always shipped at; the four new rungs are priced on **Smash's**
ladder, the tank's neighbour on the same cadence, rather than a scale invented for one skill.

**A healer is no longer invisible to every mob in the game.** A heal generates
`power / castSeconds × 10 × peopleHealed` threat, given to every engaged mob currently fighting
somebody the cast helped — so a heal in another zone costs nothing, and one cast counts **once** per
mob however many of that fight's allies it topped up. Computed from the **authored** power and cast
time, never from the HP that landed: a full-HP target, an anti-heal debuff or the healer's weapon must
not change who a monster hits.

The rate is his playtest-22 rule (300 power over 2s = 1500; 500 over 5s = 1000); the **× people** is
his 2026-08-14 correction, and it puts a heal on exactly the same footing as a buff — a per-head value
times the heads it reached. His example: a 1500-power party heal on a 10s cast is 150/s, so **13,500**
across a full party of 9, against 7,500 for the same power thrown at one ally in 2s. Blanketing the
group is what takes the room's attention.

**Threat decays**, 1%/s on an engaged mob. It is proportional, so it can never re-order the table on
the tick it runs — what it shrinks is the absolute *gaps*, which is exactly what makes a taunt
something you renew instead of something you buy once at the pull.

**And one real defect, found while answering rather than while playing: a proximity pull added no
threat at all.** A mob that walked to you arrived with an empty table, so the first point of damage
from anyone — including someone who wandered past afterwards — became the top of it and owned the
kill. A pull is now seeded at **5% of the mob's own max HP**: a fraction rather than a flat number
because threat is damage, and "out-damage the puller by 5% of this creature" reads the same at level
20 and at level 85.

**Buff threat closed the same day** (his ruling, 2026-08-14). A buff has no power to read, which is
why it is a different formula rather than the heal one with a substitution — it is priced on the two
things it *does* have: `grantLevel × 20 × peopleAffected`.

🔑 **It is the level the buff is LEARNED at, not the caster's** — *"If I learn a buff at 50 and
another at 70 the 50 one should have less aggro value."* `ClassSkills.LearnLevelOf` already knew this
per race/class/discipline. A skill no class list owns (a buff scroll) falls back to the caster's level.

His worked example lands exactly on shipped data: `HolyForce` is learned at **70**, so 70 × 20 = 1400
a head. That asymmetry is the rule — a self-buff or single-target buff is worth well under one heal,
blanketing a party is worth rather more than one: *"if it affect only the caster or a single target
won't be as much as a value but a whole party ..."*

⚠ **A buff cast before the pull is worth nothing**, and that is not an oversight — support threat only
reaches mobs already fighting somebody the cast helped. A buffer draws aggro for re-buffing
**mid-fight**, which is exactly when he should, and his own note that buffs run "20 or so minutes" is
what makes the big number safe.

A full party is **9**, not the 7 in his example, so a level-70 group buff tops out at **12,600**. That
is the intent, not an overshoot — *"Full buffing a full party should take the agro from mobs for
awhile."*

⚠ **The one number that is off is the heal, not the buff.** His comparison assumed a quick heal of
~1500 power at level 70, but the cleric's heal ladder stops at skill level **4** — learned at 35,
power **301** — because everything above it is blocked on `BL-02`. So today a group buff out-threatens
a heal by ~8× rather than the ~1.3× he sized it against. The buff formula is right; **`BL-16`** (heal
powers "sit at ~151-301 against a scale that has moved to ~1000") is the half that has not caught up.

## 0.63.0 — 2026-08-13 — `BL-05`: crafting is a PROFESSION with six levels, five masters and a mat economy

Protocol **18 → 19**. 🔴 **DB RESET REQUIRED** — `CharacterRecord.CraftExp` is a new column and
`EnsureCreated()` does not ALTER an existing table. Delete `Game.Server/game.db` (+ `-shm`/`-wal`).

### The ladder is GRADE-based for gear, and F is not craftable

His organising sentence was *"just the idea is grade based not as much as rarity based"*. Rarity still
governs materials and consumables; **grade** governs gear, and dropping F makes the ladder exact —
seven grades minus F is six, against six crafting rungs, with nothing shared and nothing invented:
**L1 E · L2 D · L3 C · L4 B · L5 A · L6 S**.

This was not cosmetic. Every craftable gear recipe outputs the authored **Mythic** piece, so filing the
rung by rarity put all 135 of them at L6 — measured, a fresh smith could reach **2 recipes out of 67**.

**Only Legendary and Mythic gear is craftable**, so a gear craft has two successes and a failure, rolled
per attempt from his table (E 50/40/10 → S 5/20/75). A failure consumes the materials and produces
nothing: the first real sink the crafting economy has. The blueprint is spent on a **success only** — his
fail rule names the *materials*, and a blueprint is the recipe, not a material.

### The mat costs are SOLVED, not chosen — and the top of the ladder needed a new faucet

`tools/BalanceMatrix` grew **`M12`**, which prices the recipes that actually shipped against his target
curve (*"2-3h of farming for E grade per weapon craft … 1d of farming to mean the full 12h"*). All six
rungs land inside his range: **E 2.3h · D 4.1h · C 8.2h · B 17.3h · A 23.7h · S 126h** per finished
weapon, and a fully S-geared character is **347 farm hours**.

They only land there because of a second change the measurement forced. **Legendary and Mythic materials
dropped from nothing in the game** — the only source was refining at 7-in-1-out on top of an Epic mat
that itself only dropped at 76+ at 0.015/kill, which priced his own authored S recipe at **3 to 6 years**.
`MobCatalog.EliteMatDrops` gives Epic/Legendary/Mythic a home on **elites**, banded like the enchant
scrolls `D1` moved the same way for the same reason. Elites, not bosses: `M11` measured an elite camp at
**110 kills/h** against a boss's **0.09**.

🔑 Two of his numbers moved, and both are worth knowing. **His target curve won over his mat ranges**
where they disagreed — the ranges came with *"depending on drop rates/amount"* attached and the curve is
a considered ruling in wall-clock hours. And the elite faucet pays **all five material types**, the one
place mat flavor is dropped: above 61 the mob categories present do not span the five, so a flavored top
faucet left whole recipes with an ingredient that dropped from nothing anywhere in the band.

🔑 **The shield is priced** (owner: *"It's armor so make it as a helmet price"*) — WH/3.33. It sits
outside both of his sums, so a shield user's kit is 1.30 weapons of armor rather than 1.00.

### Five masters, a joining quest each, and quitting

*"U go to the 'Master apothecary Roger' or watever → U accept a quest → he explains what a apothecery can
craft and other means of aquirering the items … u compleate the quest and u can take his proffesion."*

Five `NpcRole.CraftMaster` NPCs stand in **every town** (the same reason the hunting contracts became
any-town: *"i have no way to go back to the 1st town just to take it"*), each with a three-beat joining
quest gated at character level 20. **The quest is remembered forever and the LEVELS are lost every time**
— his *"Skip the quest if it's once done, but still lose levels if switching"* — so a returning apprentice
is re-hired on the spot at crafting level 1.

The old self-pick `ChooseProfession` is gone. **Crafting happens at the master**: the window opens
anywhere in **browse** mode, with every have/need count readable in the field and the buttons dead, and
goes live when you are standing at your own master.

### Crafting exp, and the freeze

His marks (0/5/15/30/50/100) stored as integers — one same-level craft is 12 internal points, so ⅓ and
1¼ are exact and his craft counts come out whole (150 same-level, 450 below, 120 above). Exp is paid on
**every attempt, success or failure**: the materials are spent either way, and the levels are practice.

The freeze is the load-bearing half — *"my exp freezes until i reach the next class … then the l2@100%
becomes l3@0%"*. Exp is CAPPED at the band's mark, never banked. ⚠ The band is read from the **best
subclass**, not the active one: `Level` and `ThirdClass` both proxy to the active subclass, so a level-76
main swapping to a fresh level-20 subclass would otherwise have had an L6 smith clamped to L2 on his next
craft — permanently.

### The two consumable ladders, and blueprints stack now

The Scroll Scribe's and the Potion Master's rungs are the **only** authored crafting levels in the game;
everything else derives its rung from what it makes. Both are deliberately offset from rarity — his
Scribe's L1 is *"nothing gear related"* (return + resurrection scrolls), which pushes his gear service to
D on L2 and lets five grades fill five rungs exactly; his Potion Master alternates an HP line and a buff
line on a two-rung stride while *dash* climbs every rung to Mythic. The Scribe gains the **A and S**
enchant scrolls, which `M11` argues for: the normal-mob faucet closes at 80, so the S band drops **zero**
enchant scrolls an hour and crafting is the intended supply.

**Blueprints stack** (owner: *"The blueprints need to be stackable not like a box"*) — they are currency,
one to learn and one per craft, not a box you open once.

### Also

- `GameHub.Craft` had **no session check**, alone among the crafting methods. Closed.
- `/DebugSetCraftLevel` — jump to a rung without the grind. The band still clamps it, which is most of
  what it is for.
- ⚠ **The SmokeTest's rune-relog pair was a coin flip and had simply been winning.** `Settle()` is a flat
  500 ms; rune buffs are re-applied by a once-a-second reconcile. An unrelated section getting longer
  shifted the phase and it started failing. `Session.WaitFor` polls instead — three consecutive clean
  runs, including on a dirty `game.db`, which also retires the "not idempotent" note on that section.

## Unreleased — 2026-08-13 — `BL-67`: MpHeal is its own rung, and the MP threshold is a knob

Protocol still **18** (unreleased, so the new config field rides along rather than bumping again).
**No schema change** — the auto-hunt config is JSON in one column, so the new field needs no DB reset.

### The bug was a hardcoded 60 that no screen could show you

*"`Restore Spirit` → now it doesnt work anyway as a heal type - nor as cyclic nor as 100% hp
treshold (self heal fires while restore_spirit no)."* Two constants sat behind the mana chain: it
fired below **60% MP**, and only while above **60% HP**. Neither was on any screen, so from the
outside the skill simply did nothing at settings where a heal plainly worked. The HP floor is the
one that broke his own worked case — *"50% MP_treshold + 30% HP_treshold ... `Restore Spirit` to be
used (MP <= 50%) and if it lowers me (HP <= 30%) to use the `Vampiric Bolt` to heal me"* — because
spending HP down to 30 is the entire plan and a floor of 60 stopped it at 60.

The floor is now **his heal threshold**, so the two chains hand off at exactly the line he set,
clamped `[15, 60]` for the two settings a threshold cannot express: 0 ("never heal") must not mean
"spend all your HP", and 100 ("heal on cooldown") must not mean "never restore mana below full".

### MpHeal is a priority group, not a special case inside Heal

*"below the `Heal` as priority but above all other (need mp to cast/buff)."* The chain is now
**heal → MP → buffs → debuffs → attacks**. It used to share the Heal group and be told apart inside
it by `IsManaRestore`, which is why one threshold armed two different resources. Each rung is now
armed by its own bar. 🔑 `AutoChainCursor` is sized to that enum — a new group means widening it.

**`Restore` and `Restore Spirit` needed no new flag**: `SkillEffect.RestoreMp` already marked exactly
those two, and the enum has had **zero bits left** since `1L << 62`. `MpHeal` is the *group*, not a
new effect.

### Vampiric Bolt is a HEAL and nothing else

*"any skill that restores HP as a `Heal` skill (only vamp bolt is left)."* It is the only skill in the
game carrying `Lifesteal`, and the heal group now casts it at the **enemy**, since it heals by dealing
damage.

It briefly had two homes — heal group when hurt, attack chain otherwise, so a nuker would not lose it
from his rotation. **He ruled that out before it shipped**: *"I want it only with a treshold .. if I
want it permanent ill do cycle or 100% treshold."* The threshold IS the control, and a skill that
fires from two different gates cannot be reasoned about from the settings screen — which is the whole
complaint behind `BL-67`. So it is off the attack chain entirely: set the HP threshold to 100 (or run
a cyclic chain) to have it fire on cooldown.

🔑 The marker is `Lifesteal`, deliberately **not** the `SkillEffect.Heal` flag — that flag routes
through the heal pipeline, which lands on the skill's *target*, so a lifesteal nuke would have healed
the mob it was shooting.

### The threshold slider, and the field that four sites have to learn

`AutoHuntConfigDto` gains `MpThresholdPct`, defaulting to **60, not 0** — 60 is the constant it
replaces, so a save written before the field existed keeps the behaviour it had instead of silently
losing its mana chain. All four sites were updated together, including the echo that fails silently
(the `78f` lesson from the batch below, applied the same day it was written).

⚠ The Auto Farm panel is a fixed 600 tall against a 720 reference, so **Normal and Elite now share a
row** to pay for the new slider. Nothing was dropped.

⚠ **Known flaky, NOT a regression**: `tools/SmokeTest`'s two rune-buff-after-relog checks fail on some
runs and pass on others — reproduced identically at `f49e192`, *before* today's work. The rune tests
run on the seeded **admin** account rather than a fresh character, so state parked in the private
keeper by one run changes the next. That is the project's own *"a test that is not idempotent lies to
you"* rule catching the one place SmokeTest breaks it. Worth fixing in the harness, not in the game.

## Unreleased — 2026-08-13 — the playtest-22 fix batch

Protocol unchanged (**18**), no schema change. Everything here answers something he wrote in his
0.62.0 pass; the eight FEATURE asks from the same pass went to `Backlog.md` as `BL-65`…`BL-72`.
The pass itself went well — §71, §72, §73, §74 and §77 passed outright, and two long-running
questions closed on his own data (`74e`'s enchant cut: *"to 28 I got 2"*; `55f`, the mage MP ladder,
after three passes).

### 🔴 The auto-buff tab was DESTROYING its own setting, not failing to draw it

*"it doesn't survive a relog ... it says that it's saved but relog says otherwise."*
`SendAutoHuntConfig` — the echo the server sends at login — omitted the `Buffs` array added with
`BL-04` the day before. The client treats that echo as its whole idea of the config and pushes it
back as `AutoConfig with { …the edited bit… }`, so `Buffs` returned as `null` and
`HandleSetAutoHuntConfig` cleared it. The first press of the **Auto** button after any login wiped
the tab on the server for real.

Fixed at both ends: the echo carries `p.AutoBuffs`, and a `null` array is read as *"no opinion"*
rather than *"clear it"* — turning every row off is 17 rows of `false`, an array, not a null.
🔑 **Appending a field to `AutoHuntConfigDto` means touching four sites and only the echo fails
silently.** Same shape as `67i`/`74b`.

### 🔴 Block reduction may never be raised — his ruling

*"what increase the reduction? The shield says 10 but I see 18% ..the shield says 20 I see 28% ...
Shields dmg reduction is never increased by any means ...only chance."* He was reading the Shield
Mastery passive: `BlockReduction += ShieldDefPct * 0.04f`, which at the maxed 2.00 is exactly the
+8 points he saw. A second, smaller coupling sat on the Mastery buff (`Percent * 0.2f`). **Both
removed.** `BlockReduction` is the shield's own printed number; the ladder scales block CHANCE and
shield DEFENCE instead. ⚠ Costs a maxed tank ~2.6 percentage points of total mitigation on top of
`70a`, which he passed while the +18% was still live — flagged for re-test.

### 🔴 The legacy gear grid is deleted — 64 items, not one

*"Brass amulet also need to be gone. Look for other items that are not from the grade items or
training ... treasure chest just gave me masterwork iron sword."* The chest handed him
`sword_e_rare`, one of a **sixty-item grid** (4 weapon types + 3 armour weights + 3 accessory slots
× F/E × Common/Uncommon/Rare) generated by a block predating the grade ladder by a whole generation
and never re-cut with it. 🔑 *They survived four gear passes because exactly one line referenced
them* — that treasure chest — *so nothing else ever showed them to anyone.*

Deleted: the grid, `WeaponKey`/`ArmorKey`, the dead `LootTables`/`LootEntry` (no caller anywhere),
plus `jewel_brass_amulet` (still on the Outfitter's shelf), `jewel_silver_talisman`,
`blunt_1h_iron_mace` and `blunt_1h_ash_wand`. The chest's 1% slot rolls `sword1h_t20_rare` now.
**The rule this leaves: gear is LADDER (`ItemLevel > 0`) or TRAINING. There is no third category.**
⚠ Dark Dominion (6 pieces, a real set bonus, obtainable by nobody) was found and deliberately NOT
deleted — removing a designed set is his call.

### The item-id reference, and the id on the card

*"Need a grouped list (in a file - like the commands one) with each equip/item ID, and in each items
details in game only for admin to see: a row like the enchant info one with the ID."* New
`docs/guides/ItemIds.md` — 1,078 ids grouped by slot, gear sorted by tier — **generated** by the new
`tools/ItemIds` (outside `Game.sln`, like BalanceMatrix), because a hand-kept id list is wrong the
first time anyone adds an item. Plus an `id <defId>` line on the item card, staff only.

This is what unblocks his own §75/§76 testing, which he could not reach for want of ids.

**`/give` gained a final `[amount]`**, default 1, capped at 10,000 — *"if i want to get 1000 mats not
to have to write command 1000 times."* It splits the way the bag does: a **stackable** is ONE row
carrying the quantity (1000 mats = one slot), while **gear** cannot stack, so an amount there is that
many separate rows, bounded by free slots and reported if it does not all fit. One inventory push for
the whole grant either way — the lesson of `66n`, where a push per unit stalled the server.

### Three smaller ones

- **Quest tokens stop flooding SYSTEM chat.** *"Remove the `SYSTEM: Stonewatch Contract: Bear Pelt
  93` .... its a drop item .. u can say in combat `You looted: Bear Pelt [Q]`."* Built as written, in
  the combat log; the running count goes with it (the quest window already refreshes on the kill).
- **The gatekeeper closes behind you** after a teleport — the rows left on screen belong to an NPC in
  another city.
- **The stats window shows the regen you are actually paid.** *"MP regen is unchanged when
  walking/runnin - or atleast vissually - it seems like its only visually."* He was right and right
  about the cause: the server pays walking ×1.2 / sitting ×1.8 / safe zone ×5, but `StandingRegen`
  reported the RUNNING baseline, so the one place the bonus was checkable denied it existed.

⚠ **`/give <player>` with no item id opens no picker, and that is not a regression** — the server has
always sent `AdminGivePicker`, but the Unity client has no handler for it, nor for `/bag`. Both are
WPF-harness survivors. Queued on `BL-56`.

## Unreleased — 2026-08-12 — the housekeeping batch, and six entries that were already done

Protocol **17 → 18**. No schema change. `GameVersion` is deliberately still `0.62.0`: the published
build on the phone is 0.62.0 and **playtest 22 is still owed against it**, so this work sits on the
branch until the crafting rework lands beside it and they bump together.

### `BL-37` — the test heal is gone

The power-1000 `test_heal`, auto-granted to every character at 76, existed to read two numbers —
`HealK` (15) and `OffChannelFactor` (0.6). Both were decided long ago, so it had been a debug skill on
every live character's bar for nothing. Deleted: the const, the `SkillDef`, the auto-grant, the
`_testHealPower` knob and its Debug-panel row. The two `{Flat, Mod}` damage test skills **stay** — they
still read live from the panel and nothing has replaced them.

Removing the knob shrank `DebugConfigDto`, which is a positional record — hence the protocol bump.

🔑 **It also surfaced a real bug, which is the part worth keeping.** `PersistenceService.ParseLearnedSkills`
read a saved `id:level` row back with **no catalog check**, and `SendLearned` pushes the map's keys
verbatim — so a *retired* skill id survived forever in `LearnedSkillsCsv` and reached the client as a
`SkillRef` it could not resolve, then got written straight back on the next save. This was not
theoretical: `hp_boost` went with the God layer and the two archer masteries went with the archer→rogue
merge, so any character alive across those builds is still carrying them. The parser now **drops ids the
catalog no longer knows**, at the one seam where stored text becomes runtime state. Deleting a skill is
a one-file job from now on.

### `BL-58` / `58i` — the inspiration game's name is out of the codebase

*"We need to rename everything that says l2 … every comment to refer from l2 to the (inspiration game)
or `IG`."* Done: **113 lines across 20 C# files** and **~150 lines across 17 docs**. The tag is `IG`,
defined at the top of `CLAUDE.md`.

Three deliberate survivors, each for a reason:

- **`L1`/`L2` meaning *skill level*** — he called this out himself (*"as the game, not the level"*).
  Seven lines keep it: `Sprint L2`, `Might L2 of 3`, `Precision L2`, the Speed-ladder table rows.
- **`docs/testing/Playtest-Archive.md`** — a verbatim record of his own messages. Rewriting what he
  wrote would forge the record, including the request `58i` itself.
- **`L2Clone`, the directory and APK name** — that is a product-identity decision, not a comment. It
  changes the Android package name and every published filename, so it is **his call, not a sweep**.

⚠ **Left for him, found on the way:** several comments cite the inspiration game's own *mob and skill
names* as research provenance (`MobBaseStats.cs`, `Skills.Mage.cs`). Those are proper nouns of the same
kind the naming rule forbids, but they are citations of a source table rather than content we ship, so
they were not touched unasked.

### Six backlog entries that were already built and never written down

Checked against the code, not the list. Each was fixed in a pass whose commit carried no CHANGELOG
entry — the exact failure the 0.61.0 note warned about — so they sat in `Backlog.md` as open work:

| Was                                               | Reported        | Actually fixed in                                                                   |
| ------------------------------------------------- | --------------- | ----------------------------------------------------------------------------------- |
| `BL-31` a skill card must print the HP price      | `55b`           | `GameUi.SkillDetail.cs` — prints an `HP` row                                        |
| `BL-32` an HP-cost skill refused at low HP        | `55c`           | `GameLoopService` — gated at cast start **and** at finish                           |
| `BL-33` Robe Armor Mastery in two learn groups    | `57b`           | `ClassSkills.cs` — the level-1 yield removed                                        |
| `BL-53` Elder Marius shows a "!" with no quest    | playtest-20 #10 | `OfferedQuests` — one method feeds dialog *and* marker                              |
| `BL-63` Frost Bind strips a dummy's HP multiplier | playtest-20 #7  | `Entity.ApplyMobScale` — factors kept on the entity, so the recompute is idempotent |
| `BL-64` target lost for a physical skill cast     | playtest-20 #8  | the manual-play branch of the auto-target push                                      |

All six are deleted from `Backlog.md`. **`BL-63` and `BL-64` were never re-tested by him** — they are
the two that fell off with no fix and no reply, so they go onto the checklist rather than being called
closed on my reading of the code alone.

## 0.62.0 — 2026-08-12 — two tabs: a place to execute the systems we already had

**`BL-03` and `BL-04`.** Both features existed and neither had anywhere to be used from: stat swaps
were twelve pair-shaped rows scattered through the Learn tab, and the auto-buff switch had no UI at
all — it was a config field nothing could set. Protocol **16 → 17** (one new hub method, one new DTO
field). ⚠ **No schema change** — the auto-hunt config already rides in a JSON column, so `game.db`
survives this one.

### `BL-03` — the Stats tab: see the build before you pay for it

*"its a bit chaotic .. need a new place -- may be a new tab where u see what stats u selected and
before u confirm a selection to show what u are changing."*

A fourth tab in the Skills window, built round the two numbers the Learn tab never showed:

- **`[-] n [+]` per pair**, and the count reads `2 (+1)` — what you own kept apart from what you are
  about to add, because one merged number hides which half is already paid for.
- **`Added:  WIT +5  |  ATK +3  |  SPT -8`** — the running NET per stat, his own line verbatim. A build
  is not "four pairs", it is where the stats land.
- **`Next price`** and a **`Rungs n / 9 committed`** row, then a Confirm that states the basket total.

🔑 **Staging is free; the bill is at the end.** `[+]` respects only the two rung caps (+5 per stat, 9
total) — gold is asked once, at Confirm. That is why `[-]` can never take back a rung you have already
*paid* for: un-committing is the Mindwriter's job, it is free there, and it drops a **whole pair** at a
time, which is not something a `[-]` button should imply. The tab says so in a footer line.

🔑 **The purchase is ATOMIC** — a new `BuyStatSwaps` hub method, not nine `LearnSkill` calls. Buying
line by line until the gold ran out would commit a partial build the player never chose. It re-validates
through the *same* `StatSwapConflict` the single-rung path uses (`StatSwapBasketConflict` walks the
basket one rung at a time against a running total), so there is no second copy of the caps to drift.

🔑 **The price is a LADDER, not a multiple.** A rung costs 1/2/3/4/5/5/5/5/5 kk *by how many you already
own*, so a basket of nine is 35kk however it is spread — and is **not** nine times the "next rung"
price. The tab and the server both read it from `StatSwapPriceRange`; the smoke test asserts the exact
charge, because a tab that summed it the obvious way would show a plausible number and bill a different
one.

### `BL-04` — the Buffs tab: one row per buff family

*"One row per buff family: `Bulwark [potion ☒][scroll ☐][max rarity: rare]`."* Now a second tab in the
Auto Potions window (which absorbs `C4` — the auto-on he deferred *into* this tab). Works with auto-farm
**off**: `AutoPotions` has always run every tick regardless.

🔑 **A FAMILY is the unit, not an item.** Every rung of Bulwark — potion, scroll, a cleric's blessing —
is the same buff under one key, so "keep Bulwark up" is one question with a list of possible answers.
The old `BuffPotionIds` was a list of items and could not express *"use the cheap one unless I say
otherwise"*, which is the entire point of the cap.

**The pick order is his**: rarity first, then **scroll > potion**, capped at the row's max rarity. Rank
is only the tiebreak, never the lead — it agrees with rarity everywhere in today's ladder, but the cap
is spelled in *rarity*, so a cap of "uncommon" must never be undercut by a rung that sorts higher.

🔑 **The walk STOPS at the first candidate already up** rather than falling through. Without that, a
character under a Rare scroll's blessing would drop to the Uncommon potion, which `ApplyBuff` refuses —
but only after the bottle is gone.

**Nothing is authored twice.** `BuffConsumables` reads the whole table back out of the catalogs: an
item's `UseSkillId` is a wrapper whose one child is the family rung, and the child carries the key, the
rank and the name. Adding a potion in `Items.cs` is enough for it to appear in the tab. The **bursts
fall out for free** — Dash's wrapper is 150 ticks, neither the potion (12000) nor the scroll (36000)
duration, so `SkillCatalog.ConsumableBuffForm` calls it `None` and the autopilot can never empty a stack
of sprints for 15 seconds each. Measured: **17 families, 35 items**; the nine paired families offer
Common/Uncommon/Rare, the eight scroll-only ones a single Rare, and their potion toggle is a *dead*
button rather than a missing one so the column still lines up.

**Back-compat:** an empty `Buffs` array means "never opened", and the old `AutoBuffPotions` behaviour
stands. A save writes **all 17 rows**, armed or not — otherwise "I turned them all off" would be
indistinguishable from "I never opened it" and the old behaviour would quietly come back.

### Smoke test

New `4f`: the illegal basket is refused **entirely and free of charge**, the legal one applies every
line and charges exactly 35,000,000, a 10th rung is refused free, and all nine rungs come back at their
bought **levels** after a relog. Three of those failures are invisible on the screen that causes them —
a wrong total, a half-applied basket, and gold taken for a refusal.

## 0.61.0 — 2026-08-12 — the playtest-21 batch: shields stop carrying the mage, the tutorial stops dead-ending, an item carries its own tags, and a rune can pay you

**One release, four commits, and the whole of playtest 21 answered.** The pass is closed and
transcribed into [testing/Playtest-Archive.md](testing/Playtest-Archive.md#playtest-21); everything he
asked to be *built* rather than fixed now has a permanent id in [Backlog.md](Backlog.md).
⚠ **Schema change — `game.db` must be moved.** Protocol stays **16**.

### Shields stop double-dipping — his option 3

He found it himself and it was real: a shield's `ShieldDefense` was folded into physical defence
**permanently**, so it already paid on every hit, and a block then removed another 34-47% on top.
*"Thats why the tank/cleric felt immortal ... Mage should not be immortal even with a shield."*

Of his three ways out he picked the third: **cut the flat defence 5× and give the tank it back through
his kit.** Shield defence goes `90 143 203 230 256 299 413` → **`18 29 41 46 51 60 83`** (his own worked
number, 51, lands on the 61 rung), and **Shield Mastery's passive is raised 5×** (`ShieldDefPct`
0.30/0.30/0.40/0.40 → **1.50/1.50/2.00/2.00**). The nerf therefore lands on the wearer with no mastery
— the mage — which is exactly what he asked for.

🔑 **He ruled that the mastery passive is the ONLY thing that scales**: *"arrow defence and other
passives, sets and buffs that increase the shieldPdef/chance etc are kept as is."* So the Mastery buff,
`BlockChancePct`, `BowResist` and the sets' `shield.p.def x1.25` keep their numbers and buy a fifth of
the absolute defence they used to, on purpose. That ruling is written into all three places it lives so
it cannot drift back.

**Block behaviour is bit-identical to 0.59.1** — the coupling was rescaled to match
(`BlockReduction += ShieldDefPct * 0.2f` → `* 0.04f`, so 0.04 × 2.00 is the same +0.08 as 0.2 × 0.40).
Only flat defence moved: a 1H+shield tank's P.Def goes L20 625 → **498**, L52 1031 → **801**, about
−19% throughout. The `Shield def:` row is gone from the stat sheet, because the number is simply part of
P.Def now. A shield's enchant drops **+9 → +3** per level, the same as armour — *"i was considering that
it worked only in block state."*

**And `67m` was a genuine miss, not a tuning complaint.** The Wooden and Iron Shields are hand-authored,
so 0.59.1's block re-cut — which walked the generated rungs — never touched either of them. That is why
the wood shield still carried 30% reduction after a build that supposedly halved it. Both are on the
tier profile now (defence 35 → 7 and 90 → 18), and `shield_iron` is deleted per *"Iron sheld can go."*

### The start quest, re-specced end to end — and it could dead-end two more ways

0.60.1 fixed the dead-end he hit; playing past it found two more, and both were worse.

**A fighter had nothing to cast.** The "use a skill" beat was credited only from a *completed cast*, and
a level-1 fighter has no spell — *"I had to use TestSkill to continue with quest."* A **basic attack**
credits it now.

**The Auto button had stopped calling the handler that credits the auto-farm beat.** When it was changed
to push the whole config, `ToggleAutoHunt` lost its last caller — so no button on the screen could
credit the step, and *"nothing works to allow me to continue"* was literally true. The config handler
credits it now, which also explains `63a`: the reward rendered and the quest never finished.

The rest is his order, built as written. **Creation grants no boxes** (that was the source of his three
weapons and three armours); the two training boxes are **plain, not selections**, and **class-conditional**
via a new `BoxEntry.ForClass` — fighter gets sword + leather, mage gets wand + robe — with the filter
applied in the random path, the selection *offer* and the selection *confirm*, the confirm being the
authoritative one. Part 1's beats are now: talk → open a box → equip ×2 → **travel with Pell's list** →
**put something on the bar** → target and use it → kill 5 pups → level 3 → back to Cera. The last two of
those are new step types (`Teleport`, `AssignBar`); `AssignBar` is credited only from a **player** edit
of the bar, so the skill-bar rule holds. `training_club` and `training_knives` are deleted —
*"the others are useless."*

### The training tier is written down

Broken jewels beat **F Common in all three slots and F Uncommon in two**, so the starter reward
outranked the first thing you buy. They are his 9 / 5 / 3 now (were 15 / 11 / 7).

🔑 **But the numbers are not the fix.** That rung has now drifted above the ladder **four times** —
training armor, the Wooden Shield, its block profile, and now the jewels — because it is the one tier
that is hand-authored instead of generated from a column. So `gear_sets.csv` grew a **TRAINING TIER
block**, every starter piece on its own row **directly above its F row**: *when you raise an F rung, the
thing that used to silently outrank it is now the line above your cursor.*

### The training dummies were inert for three reasons, not one

*"Both dummies act as the old - they dont do nothing different."*

🔑 **Nobody was ever inside the strike radius.** `DummyStrikeRange` was his literal **50**, but a melee
attacker is walked to `MeleeRange` = **80** and stops there, and a caster stands at 600. Now **150**.
*A reach authored from a design note must be checked against the stop-distance the movement code
actually produces* — the number was right in the note and wrong in the world.

Second, the spawner hard-coded `"Training Dummy (Lv N)"` for **every** dummy, so all three wore the same
plate. Third, they had no titles: `Normal` / `Physical` / `Magic`, via a new `MobType.Title`. No client
change was needed — the nameplate already draws a title for any entity. **`69d` magic evasion is finally
testable**, because the magic dummy feeds the fail-chance channel it was waiting on.

**Rank titles** came with it (his ask mid-session): elites wear **`Elite`** in red and lost the `Elite `
prefix from their *name*; the valley treant is **`Field Boss`**, the grave lich **`Dungeon Boss`**.
🔑 Field vs dungeon is read from the **coordinates**, reusing the existing "dungeons are the negative
quadrant" rule — a second flag would be a second thing to drift.

### `65d`, and why a client fix would not have worked

*"I select a target and select my next target manually before the first dies -> then the 1st dies and
closes my second."* The root cause was **server-side**: in manual play the autopilot pushed a live-target
message every tick, **and a null push is a revocation of a selection the server never made.** Kill A
while B is manually selected → null → B is wiped. Manual play may now only ever *hand a target over*,
never take one away. The client drops its own target on the **alive→dead transition** — his *"'DIES' not
'DEAD'"* — so tapping a corpse still sticks.

### `67i` was bigger than the line he saw

He reported that the leather armour description omitted its +200 crit damage. In fact **five** channels
had no formatter line at all — `CritRateFlat`, `CritDamageFlat`, `MagicResist`, `PvpDamageTakenPct` and
`AccuracyPct` — so **every S set in the game described itself with numbers missing** since 0.59.1.
🔑 *Appending a field to `StatMods` requires a text line in the same commit; nothing fails loudly when
the formatter is missing one.*

### The rest of the batch

**`66n` the x500 mats freeze, root cause**: the admin give enqueued **one command per unit** — 25
materials × 500 = **12,500 commands**, each granting one item and re-serialising the whole inventory.
Quantity rides on the command now (a stackable is a single add, only real gear loops), one inventory
push at the end, clamped to 10,000. **Auto-farm ignored a skill's weapon requirement** — it checked
cooldown, MP and HP but never `RequiredWeapon`, so it cast Stab off a mace while manual play refused;
both gates are checked now, as a *skip*, so the cursor moves to a skill that can fire. **`68h`** F-grade
gear prints `Unenchantable` instead of a per-enchant line. **`63i`** the Rune of Tincture leaves the
Apothecary shelf for admin/event only.

**`62j` the enchant-scroll drop rate, ratified and cut 30×.** His data pinned it exactly: the E scroll
was `0.40 × EnchantShare(0.15)` = **6% of every kill**, and the only rung live below level 40 — which is
how he had 80 scrolls by level 28. `EnchantShare` 0.15 → **0.005**: E **0.20%** · D 0.15% · C 0.10% ·
B **0.075%**. Rung shape, boxes, elites and bosses untouched. ⚠ B may be too thin; flagged to him.

### `58d` — an item carries its own tags, and `/give` can write them

His playtest-20 design: *"it is a REAL item with tags — never a new server-side def."* An instance now
carries five properties of its own, each `null` meaning *no opinion, use the catalog*: **sell price**
(−1 = unsellable), **tradable**, **custom name**, **can store private**, **can store account**. The last
two are new — the private keeper had no instance gate at all.

**The displayed tag is DERIVED, never stored**, so it cannot disagree with the behaviour: sellable +
tradable reads as nothing, neither reads as **bound**, sellable but untradable reads as **private**, and
a timer composes on top → **"(temporary, bound)"**. The three predicates live in `Game.Shared` and are
called by the server *and* the item card — because a display that quietly disagrees with the rule it
describes is exactly how `67i` happened.

```
/give <player> <itemId> [sellPrice] [tradable] [timed] ["name"] [enchant]
                        [canStorePrivate] [canStoreAccount]
```

Everything after the item id is optional and positional, `-` means no opinion, and **`1m` is one
MINUTE** (his rule). A tagged instance is **always a fresh bag row**, never merged into a stack — the
tag belongs to that copy. `/give <player>` alone still opens the admin's own bag as a picker.
Enforcement reads the **instance**, not the def, at the vendor, the trade offer and both keepers.

**The five fields persist**, which is the schema change. *A bound item that comes back ordinary looks
perfectly right until the moment it can be sold* — so SmokeTest §8 asserts the round-trip through SQLite
rather than just the grant.

### Premium reward runes (`BL-01`)

**Five ladders and two punishments, all on the machinery that already existed.** A rune has been an
item that grants a buff while it sits in your bag since the War/Spell runes; the only new thing here is
the *payload*. `RewardRates` — exp, SP, gold, drop chance, plus two "stop" flags — rides as a field on
`SkillDef` and on `BuffInstance`, and `Entity.RecomputeDerived` folds it into four multipliers.

- **Rune of Experience / Skillpoints / Exp-SP / Gold / Drop**, each ONE skill whose **levels are the
  rungs**: his +5%, then tenths to +100% (`RewardRunes.Ladder`). One item per rung (55 of them),
  generated from the same table, with the percentage in the id (`rune_exp_20`) — an id that states its
  own number cannot come to mean another one.
- **Rune of Sinister** — no exp, no SP, gold and drops untouched. *"So a grinder can grind and no lvl
  up."* **Rune of Sinners** — all four zeroed, bound to the soul.
- **Rungs never stack, the best one wins.** They share a family key, so the reconciliation applies the
  strongest rung you hold and evicts a weaker one that is already running; when the +100% expires the
  +20% in your bag takes over by itself on the next pass. The rates are folded by **MAX**, not summed,
  so an Exp rune next to an Exp/SP rune is +50%, never +70%. A **stop** is a hard override applied
  after the max: no pile of bonus runes can dilute a punishment.

**Two things the plan for this had wrong, both found while building it:**

- `SkillEffect` did not have "3 bits left" — `1L << 62` was already the last one (63 is the sign). So
  there is no `BuffRewardRate` flag; the package is a field, like `PhysMpCostPct` and the crit fields
  before it. Nothing about a reward channel wanted a bit anyway.
- **Untradable was not enough to bar the keeper.** The private warehouse takes anything that is not a
  quest item — it is deliberately just a bigger bag — so a Rune of Sinners could have been parked there
  until it expired, which is the one thing it must not allow. New def-level **`SoulBound`**: refused by
  *both* keepers regardless of instance flags, so the punishment does not depend on whoever handed it
  out remembering the right `/give` arguments. Runes were already delete-protected, so it now has
  nowhere to go but with you.

**The drop multiplier is a PARAMETER of `MobCatalog.EffectiveRate`, not arithmetic at a call site.**
That is the rates rule in CLAUDE.md doing its job: the kill roll and the target-inspect list ask the
same function with the same player, so a player wearing a Drop rune is *shown* the chance they roll.
Doing it anywhere else makes the inspect screen lie, silently, to exactly the players who paid.

Two display fixes fell out of it: a leveled buff's bar popup now reads **its own level's** text
(`DescriptionAt`) instead of the skill's generic blurb — every rung used to describe itself as +5% —
and a rune's square is named after the **item**, so it reads "Rune of Experience (50%)".

Also: **`ItemCatalog.ValidateRunes()`** fails startup if any rune names a missing buff skill or a rung
its ladder does not have. A rune pointing at nothing sits in the bag looking perfect and pays nothing.

**Measured, not derived** — new BalanceMatrix **§R**: a +100% Exp rune halves the climb to 60 (18,737
→ 9,368 kills) and *lowers* lifetime trash gold to 0.50× with it; a +100% Drop rune is ×1.90 on total
sold value while a +100% Gold rune is only ×1.10, because coin is a small share of what a kill is worth.
**SmokeTest §9** covers the three failures a playtest cannot see: two rungs both applying, the inspect
list showing the server's rate while the kill rolls the player's, and a rune buff coming back twice
after a relog. The runes themselves needed no protocol change and no new column — **the db reset this
release asks for is `58d`'s**, above.

## 0.60.1 — 2026-08-11 — a quest step supplies its own props; magic evasion is a fail chance

**The tutorial could dead-end, and he walked straight into it.** `63j` gave part 1 three "prove you did
it" beats, the first being *open a box*. He opened both creation boxes **before** Huntmaster Cera handed
him the quest, so the step had nothing left to open and the chain could not continue — a DoAction step
is a gate, and a gate whose prop is already consumed is a wall. The file even predicted the shape of
this (it asks for ONE box, not two, for exactly this reason); asking for less was the wrong mitigation
of the right worry.

His fix, and it is the general one: **a step that requires an object hands that object over.**
`QuestStep.SupplyItemIds` lists a step's props; while that step is current, anything the bag does not
hold is granted. Part 1 carries the two **training** boxes on its first step (so they arrive when Cera
gives the quest) and again on the box beat. Granting is idempotent by construction — "you hold none of
it" is the only condition — so re-entering the step, relogging or talking to Cera again can never hand
over a second one. That also means the props must be worthless, and these are: untradable, sell price
0, the weakest tier in the game.

It runs from `SendQuestLog`, the one call every quest-state change already funnels through (accept, all
four advance paths, login). Two consequences worth stating: a future step type cannot forget to supply
itself, and **a character already stranded is repaired on his next login** rather than needing a manual
grant.

**Magic evasion is built, and it is not an evasion roll** (`62e`). His rogue CSV asked for "magic
evasion x1.1", which the game had no channel for; asked what he meant, he ruled *"the magic evasion
should be magic fail chance like 3-4"*. So **Evasion Boost now adds +4 percentage points to the fail
chance of spells cast at you** for its 30s — a caster at parity drops from 99% success to 95%, and
against one punching up it stacks on a fail chance that is already climbing. Flat and additive on
purpose: multiplying would make it worth nothing at parity and enormous at a level gap, the opposite of
a defensive burst. `SkillEffect.BuffMagicEvasion` (bit 32, reusing the one freed when
`BuffMagicFailResist` was deleted) → `Entity.MagicFailBonus` → the new `defenderFlatPoints` argument of
`StatCalculator.MagicFailChance`. Its sibling channel, "skill evasion x1.25", is **still not built** —
dodging a physical skill separately from a basic attack is a new resolution mechanic and he has not
ruled on it.

**Piercing Stab level 4 costs 28 MP, not 58** — he ruled the CSV row a typo (*"should be 28.. a
typeo"*) and edited `rogue 20-35.csv` himself. It was the one spike in a line that runs 18 / 21 / 24 /
**28** / 30.

Protocol stays **16** and `game.db` survives — no wire or schema change. The client needs rebuilding
only for the version label; nothing above is client code.

## 0.60.0 — 2026-08-11 — enchanting stops being a percentage

**The enchant payout is now a flat, authored number per level — his table.** Until today every
enchanted stat ran through one formula, `base + 0.20·base·level + level`, applied to *every* bonus on
*every* slot. That is **×4.2 at +16**, against a ladder whose best weapon is 437 P.Atk: a +16 S blade
hit for 1,851, and a +16 S armour set quartered incoming damage. Enchanting was worth about two and a
half grades in both directions at once, which made PvP a count of scrolls rather than of gear. The
formula was written when the whole game was F-tier and items carried 5–30 points; it never got re-cut
when the ladder grew.

What replaces it, per enchant level:

| slot                                  | per enchant                                                        | at +16                     |
| ------------------------------------- | ------------------------------------------------------------------ | -------------------------- |
| Armour                                | +3 P.Def, +Max HP by grade (E 0 · D 0 · C 15 · B 20 · A 25 · S 30) | +48 P.Def, +240…480 HP     |
| Shield                                | **+9** defence (triple), same HP row as armour                     | +144 defence, +480 HP at S |
| Jewel                                 | +3 M.Def, +Max MP by grade (E 0 · D 0 · C 1 · B 2 · A 3 · S 5)     | +48 M.Def, +16…80 MP       |
| Weapon 1H (sword, blunt, wand, duals) | +6 P.Atk                                                           | +96 P.Atk                  |
| Weapon 2H (greatsword, maul, staff)   | +8 P.Atk                                                           | +128 P.Atk                 |
| Bow                                   | +P.Atk by grade (E 10 · D 12 · C 14 · B 16 · A 18 · S 20)          | +160…320 P.Atk             |
| Any weapon                            | +6 M.Atk                                                           | +96 M.Atk                  |

Three rulings inside that, so they don't get "fixed" later. **The offset is the same for every class** —
a full +16 armour set is +1,920 Max HP at S whether a tank or a healer wears it, which is +37% for the
tank and +130% for the caster: *"a healer spends gold/farm to enchant gear to +16, he gets the full
bonus — he will be stronger than just a warrior."* **It is by grade, not rarity**, so enchanting a cheap
piece is relatively better value. And **the shield's defence is tripled** because its damage reduction
only pays on a successful block (25% of hits at S since the block re-cut), so its enchant has to pay in
the flat defence that applies to every hit. Everything he did not name **stopped scaling entirely**:
Evasion, a robe's inherent +MP, a weapon's +MP, an armour piece's M.Def.

**Measured, not derived** — `BalanceMatrix` grew a **§E** section that dresses each playstyle in a full
tier loadout at +0 and again at +16 and runs the real resolvers. At S: tank +21% dps, warrior +21%,
dagger +16%, mage +15%, **archer +34%** — the bow's grade-scaled row is worth 2.5× a greatsword's, and
the measurement shows it *closing* the archer's gap to the dagger (324 vs 333 dps at +16) rather than
opening a new one, which is what he asked for it to do. The defensive half moves further than the
offensive one everywhere, most of all for the mage (ttk 12.9s → 38.1s).

The item card now shows the enchanted total on stats a piece does not natively carry (every tiered
armour has HpBonus 0, so a +16 S body owed +480 HP that the old "print only if the base is non-zero"
test hid completely), and adds a **"Per enchant"** line saying what the next scroll buys. No DB reset —
the bonus is recomputed from the stored enchant level, so existing saves simply get the new numbers.
Protocol unchanged at 16; the client needs a rebuild because the math lives in `Game.Shared`.

## 0.59.1 — 2026-08-11 — the S grade is AUTHORED, and it finally has set bonuses

**The top grade stops being a formula.** `SGradeOverA` (a flat ×1.60 over the A row) is deleted:
every weapon, body, shield, accessory and jewel now carries its own authored level-80 row. Almost
everywhere that is a *cut* against the old derivation, and it is deliberately uneven — armour came down
harder (bodies and accessories ≈ ×1.33 over A) than weapons (≈ ×1.55), so offence outruns defence at S
by about 17%. The bow lost the most (930 → 794), which was a derivation artefact compounding an already
large A-grade P.Atk; and every fighter weapon now shares an S M.Atk of 192, which closes the one rung
where "a 2H's M.Atk equals a 1H's" had broken.

**S has set bonuses for the first time** — Ironforge, Nightleaf and Arcanum at 80. Until now an S body
with S accessories completed *nothing*: the best gear in the game was the only tier with no set identity,
and the orphan report in BalanceMatrix listed 21 pieces belonging to sets that did not exist. It is now
0. Heavy S is the tank line at its endgame shape (the first set to carry crit rate, magic damage
reduction, melee vamp and a PvP damage-taken cut, and its shield clause repeats the PvP cut so shield-up
compounds to ×0.9025); light S adds move speed, flat crit rate and **+200 flat crit damage**; robe S is
the M.Atk set. Measured at level 85: a fighter gains +16% MaxHP and loses a little attack, a mage gains
+6% M.Atk *despite* his staff being cut, because the new robe set more than pays for it.

**Shields no longer double-dip.** A shield's `ShieldDefense` is folded into physical defence
permanently — it is already paid on every single hit — and a successful block then removed another
34–47% on top. Block chance, block reduction and shield crit-defence are all cut hard: average
mitigation from *blocking alone* falls from 5.1% to 1.0% at F, and from 15.0% to 6.3% at S. **Shield
Mastery is untouched**, so a mastery tank at S still reaches ~14%; the nerf lands exactly where it was
aimed, on the shield-only wearer — the mage who was becoming unkillable by holding one.

**PvP damage *received* now exists.** All three PvP modifiers were attacker-side; the S sets needed the
other half, so `PvpDamageTaken` is read in the damage pipeline. Its counterpart is the weapon rule, and
that one has to be **earned**: an A- or S-grade weapon adds +5% to all three PvP damage channels only
when it is enchanted to **+4 or more** — the price is the risk of breaking it. The armour half (−5%
damage taken) is set-only; the weapon half pays on every hit. Both are PvP-only, so no PvE number moves.

Nothing on the wire changed: **protocol stays 16 and `game.db` survives.**

## 0.59.0 — 2026-08-11 — CRAFTING becomes reachable, and the admin gear list stops lying

**Crafting was never missing — it was unreachable.** Professions, refinement, finished-item and
consumable recipes, blueprints and a mats-primary drop table all shipped on 2026-07-06. Nothing since
could touch any of it, because the phone had no window: the only thing in the client that ever named a
profession was a row of debug buttons. That is why "crafting" has sat at the top of the content-blocker
list through four playtests while being, in code, already built. This build is the window.

**The client computes the recipes; the server owns two facts.** The new `Crafting` push
(`CraftingUpdate`) carries exactly the profession and the unlocked blueprints — the things only the
server knows. Everything else the window draws (inputs, quantities, level gates, success chances) it
reads out of `RecipeCatalog`, which is compiled into the client from `Game.Shared`, so it is the same
data the server crafts from and the two cannot drift. No recipe list travels on the wire.

The window is four pages: **Refine** (5 same-type + 2 cross, the trade engine), **Gear**, **Goods**
(potions and scrolls) and **Mats** — every material with what you hold, because "how many Rare Ingots
do I have" is a question asked away from any one recipe. A row is *what it makes* over *what it costs
and what you have*, ingredient by ingredient, green when you have enough and **red on the one that is
stopping you**. A locked recipe is dimmed rather than hidden — knowing what is three levels ahead is
most of what a crafting list is for. A risky craft names its odds before it spends anything.

**Choosing a profession is in-game at last**, behind a confirm that says PERMANENT, and it now saves
immediately instead of waiting on the 60-second autosave — there is no way to pick again.

🔴 **The admin Equip tab has been handing out 70% gear.** It filtered on `ItemRarity.Epic`, which was
right until the rarity ladder was re-anchored and the authored tier tables became the **Mythic** rung
with every lesser quality a derived copy. After that, "Level 76 → Adamantine Blade" gave the *Epic
copy*: **P.Atk 196 where the real item is 281**. It never looked broken, because the Epic list carries
the same levels 1/20/40/52/61/76/80 as the Mythic one. Every balance number taken off admin-issued gear
since the re-anchor was ~30% light. The same stale filter had already produced *zero* craftable recipes
in `RecipeCatalog.FinishedItemRecipes` and was fixed there; this was the same bug in the place it is
hardest to see.

**Admin gains the materials and the blueprints** he asked for: a Crafting page under Items with all 25
materials (generated from the shared tables, not listed) at x200 each plus a **give-everything x500**
button, and all 36 blueprints at x5 plus a give-all — sized for what a craft actually costs, since one
E-grade body is 100 commons and 50 uncommons.

**Protocol 15 → 16.** The direction of risk is the unusual one: an old client simply has no window, but
a *new* client on an *old* server would open a crafting window that is never told its profession and
would sit there offering nothing while the server crafted happily. The bump makes that pair refuse.

Verified against the real `Game.Shared`: 187 recipes build, every output and input resolves, all 36
DropOnly recipes have their blueprint item, and `craft_heavy_t20` still reproduces his E-body anchor
exactly (100 common 50/20/20/10 + 50 uncommon 25/10/10/5).

## 0.58.3 — 2026-08-11 — his weapon numbers, and the mob-info window becomes a bestiary page

**The ×1.166 two-handed P.Atk raise is REVERTED.** On 2026-08-10 the 2H line was raised by exactly
379/325 — the swing rate a two-hander lost when the speed table stopped folding it down to Sword — and
the new numbers were written into `docs/data/gear/gear_sets.csv`, which is his file. That edit was owed
a yes or no. The answer is **no**: he re-gave the whole line by grade and every P.Atk in it is the
original number. So the speed ruling keeps its price — a 2H is ~14% less DPS than it was, and the Maul
sits only ~4% above a one-hander while giving up the shield. That is now a stated outcome rather than
an accident. BalanceMatrix C1 is back to rogue/warrior 1.05× at 20 and 1.37× at 36.

| grade            | F     | E      | D      | C      | B       | A       | S           |
| ---------------- | ----- | ------ | ------ | ------ | ------- | ------- | ----------- |
| 2H P.Atk / M.Atk | 29/17 | 112/54 | 190/83 | 236/99 | 282/114 | 342/132 | **532/192** |

The **S row is authored**, not derived: A × `SGradeOverA` would give 547/211. The generator now skips
its own S derivation whenever a table already ends on the S level.

**The whole F rung was re-authored** — staff 23/24 · wand 22/23 · 2H 29/17 · 1H 24/17 · bow 49/17 ·
daggers 21/17. Two shape changes, not just values: every fighter weapon now shares **M.Atk 17** at F
(it was a flat 14), matching the one-M.Atk-column-per-grade shape the E–A rows already had; and the two
caster weapons **crossed over**, so a wand or staff now carries more M.Atk than P.Atk at F, as it does
at every grade above it.

**The training bow is gone**, with no training staff or 2H to keep it company: *"you don't need them to
start playing."* The creation box and the vendor now offer four training weapons; an archer starts on
knives and picks up a bow from the level-10 quest box. The training wand's P.Atk is **5**, not 6 —
`docs/Roadmap.md` has said 5/7 since the tier was authored.

**The target window holds what you opened.** It rendered the inspect response only while it still
matched the *current* target, so the moment auto-farm switched mobs the drop table he was reading
blanked to "Select a target and tap Info." It is now pinned to the entity Info was tapped on, and says
`[pinned]` in the title once that stops being what you are fighting.

**A Skills tab**, mob-only, beside Stats and Drops: every skill the creature can cast — category,
range, cast and reuse time, power and MP, at the level it actually has them — and its passives, which
moved here from the Stats sheet. A plain melee creature reads "None — this creature only attacks",
because that is a real answer about a mob and a blank section is not.

**Rulings recorded, no code needed.** Accuracy cannot eat the evade floor and evasion cannot eat the
hit floor — already true: `ResolveAvoidChance` applies the floor window last, after the level gap. The
tank's magic defence is fine as shipped (25% damage reduction against the mage line's 30%), and
Anti-Magic Lv2/Lv3 stay at levels 43/76 until the 40+ class kits land.

## 0.58.2 — 2026-08-11 — magic gets its own landing formula, and "mRes" becomes what it always said

Playtest-20 `57d`, clarified: *"I don't see my magic failing with a bow more than with a wand"* — level
60 vs a level-60 dummy, several skills cast. **He was right, and the code agreed with him: the fail
chance was bit-for-bit identical.**

**The bug.** Spellcaster Mastery promises a bow caster three penalties — cast speed ×0.5, M.Atk ×0.5,
and "magic accuracy ×0.5". The first two worked. The third was `MagicFailResist *= 0.5f`, and
`MagicFailResist` is **0** unless a spell-focus buff is running (no skill in the game grants one). Half
of zero is zero. It was also pointing the wrong way: at the roll the stat could only ever *subtract*
from fail, so no term anywhere could raise a fizzle because of the weapon. Against a mob the base was
1% and both stat terms were hard-coded `0` — wand and bow alike, ~1 fizzle per 100 casts.

**The replacement (owner's model, in percentage POINTS):**

```
fail% = round( 1.3^(defenderLvl − attackerLvl) × defenderMod × weaponMod )   clamp [0, 95]
```

Parity with every modifier at 1 is `round(1) = 1` — **same level is 1% fail, 99% success**, his number.
`defenderMod` is 1 for everyone and **2** with the tank's Anti-Magic passive. `weaponMod` is 1 with a
trained caster weapon and **25** with a bow / dual / bare hands. Magic no longer touches
`ResolveAvoidChance` at all; the physical channel keeps it unchanged.

| Δ lvl (def − atk)   | 0       | +3      | +5     | +10 | +14 | +18 |
| ------------------- | ------- | ------- | ------ | --- | --- | --- |
| wand                | 99%     | 98%     | 96%    | 86% | 61% | 5%  |
| wand vs a tank (×2) | 98%     | 96%     | 93%    | 72% | 21% | 5%  |
| **bow** (×25)       | **75%** | **45%** | **7%** | 5%  | 5%  | 5%  |

Fail clamps at 95%, not 100% — the playtest-19 `M1` ruling ("nothing is unhittable any more") holds in
this channel too, and a gap that big already pays zero exp and zero drops.

**"mRes" is a damage reduction, and always was.** The owner: *"the anti-magic passives where it says
1.25 magic resist — it's actually endMagicDmg × 0.75, so it's actually a dmg reduction rather than fail
chance… the problem was we didn't have a mdmg reduction, that's why we converted them to a floor."*
Exactly right — `healer/nuker 20-35.csv` says **"magic def +20, mRes +5%"** and it was built as a
fizzle floor because no magic damage-reduction stat existed. There is one now: `Entity.MagicResist`
sums the CSVs' percentages and lands as a divisor **inside M.Def**, the same shape as
`PierceDefCoef`/`BluntDefCoef`/`BowDefCoef`. One correction he accepted: the mob ladder's values
(`1.11 / 1.25 / 1.43 / 1.67 / 2`) are exact reciprocals of `0.9 … 0.5`, so **1.25 → ×0.8**, not ×0.75 —
a divisor is what keeps his own ladder symmetric.

**Deleted, don't reinstate:**
- `MagicFailResist` — the caster-side accuracy stat. His model has none: level, tank ×2, weapon,
  nothing else. It was zero on every character and it is what made `57d` invisible.
- `MagicFailFloor` and the whole fizzle-floor concept, including `SkillEffect.BuffMagicFailFloor`.
  The tank's Anti-Magic is the ×2 modifier now; the mage/healer/nuker Anti-Magic line is resistance.
  (`1L << 32` is a free `SkillEffect` bit again — there were only two left.)

⚠ The tank's Anti-Magic reads *smaller* at parity (2% vs the old 10% floor) but it multiplies the
level term, so it is worth more where it matters: vs a caster 10 levels up it turns 14% fail into 28%.
⚠ Anti-Magic Lv2/Lv3 (×2.5 / ×3) are **extrapolated, not authored** — the 40+ CSVs overwrite them.
⚠ The bow penalty is multiplicative, so it fades when punching down: at Δ−10 a bow caster is back to
~98% success. Inherent to the formula, flagged rather than patched.

`tools/BalanceMatrix` grew a **MAGIC LANDING** section printing both tables off the real code.

### 🔴 And the server did not boot — found by trying to run the smoke test

`0.58.1` shipped a **server that refuses to start**. The two striking training dummies added for `56c`
sit at x=26500 and x=27500; the Training Grounds field's east edge was x=26500, so both were outside
every field and `RegionMap.ValidateSpawnersInFields` threw at startup — by design, that guard is
meant to catch exactly this. Nothing had run since. The field is extended 1400u east (nothing else is
within 5000u; the quadrant east of the outpost is empty).

Two **stale smoke-test assertions** were failing on shipped features, not on bugs:
- *"a level-1 character has no quest markers"* — untrue since **0.54.0**, when the tutorial chain
  landed and deliberately opens at level 1. It now asserts exactly one marker, the tutorial.
- *"the Brackenford gatekeeper is visible"* — it matched the catalog name `"Gatekeeper Pell"`, but
  since **0.55.0** ("NPCs wear their role") the server splits that into `Name="Pell"` +
  `Title="Gatekeeper"`. The mismatch then threw at a `First(...)` and took **the whole rest of the
  run** with it, so the gate-travel section had never executed. It matches the personal name now.

`tools/SmokeTest` reports **ALL CHECKS PASSED** end to end.

## 0.58.0 — 2026-08-10 — a class grants no stats, and the invented level-40+ kits are gone

*(The third slice of 0.58.0; the evasion root cause and the mob weapon table shipped in the two
commits before it and are not yet written up here.)*

**Identity is the kit, not the stats.** The owner's ruling: *"There is no identity. The identity is
just skills/passives kit … the magus and the tempest have same stats, just one has more dmg skills
while the other more debuffs … no more u change your class and get bonus."* Every class runs the same
stat formulas; what separates two disciplines of one archetype is what their skills *do*.

So the whole class-bonus layer is deleted — `ThirdClassCatalog.FlatFor` (the twelve per-discipline
leans: Bulwark +220 HP/+45 Def, Ravager +45 Atk, …), the Cleric's stray `+60 MP/+30 HP/+10 Def` (the
only one of eighteen 2nd classes that had one), the `Bonus` field on both class-def records, and the
two apply-blocks in `Entity.RecomputeDerived`. `ClassFlatBonus` the *record* survives as an
**armor-set** type; gear is not class.

The proposal to re-home the same numbers as discipline passives was rejected too — *"Remove them,
don't add them as passives. W8 on the 40+ csvs."* This table was where `Discipline.Phantom` hid an
`Evasion: 32` against a whole-game evasion budget of ~18 points, unnoticed until the 0.58.0 hunt: a
bonus nobody can see is a bonus nobody can tune.

**The 40+ purge.** *"Anything that's not inside the csv should not exist except the class balance."*
Every level-40+ skill grant was invented ahead of the CSVs, so `ClassSkillTables.Third.cs` loses the
placeholder rename kit for all ten fighter disciplines, the warrior demos (Cleaving Strike, Hamstring,
War Focus), the tank kit (Shield Bash, Provoke, Aegis, Last Stand, Indomitable), Terrifying Roar, the
Venomweaver DoT trio, and the rogue primitives (Shadowstep, Vanish, Repelling Shot, Snare Trap).
Kept by his exception list: everything nuker — Elemental Burst, Frost Bind, Entangling Roots, Glacial
Spike, Creeping Frost, Phase Shift, Mana Barrier, the Magus/Tempest kit — and the Warchanter's buff
ladder, which has a CSV behind it.

⚠ Only the **learn assignments** are gone; every `SkillDef` stays in the catalog. Anything already
learned keeps working (`LearnedSkills` persists ids, not table entries), and those defs are the raw
material for the level-40+ CSVs. Don't re-grant them, and don't invent replacements.

**The three floor passives move into the CSVs** — Evasion Mastery (rogue), Precision (warrior) and
Anti-Magic (tank) each get a level-20 row in `docs/data/classes_skills_csv/`, SP 0 because they are
auto-granted rather than bought. They still work at every tier via `FloorPassiveFor`; the CSV is now
the authority on their numbers. ⚠ The tank CSV separately contains a *different* skill called "Tank
Anti-Magic" (m.def +25/+45) — a stat, not the fizzle floor.

**Server + shared only** — no DTO, protocol unchanged, no schema change, `game.db` untouched. The
in-game effect is at 40+, where every discipline loses its lean (Bulwark's −220 HP/−45 Def is the
largest), and on the Human Cleric from level 20. Not visible in BalanceMatrix, which assigns a 3rd
class in only one section — **unmeasured and unplayed.**

