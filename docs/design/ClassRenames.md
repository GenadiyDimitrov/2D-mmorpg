# The class names — `BL-100` + `BL-101`, BUILT 0.98.2

> 📋 **His own table — with weapon, armor and path per class — is `docs/data/classes_skills_csv/README.md`
> under `## Classes and Races`.** It is checked against the code and all 24 pairs match. This file is
> the reasoning; that one is the quick lookup.

> ✅ **BUILT 2026-08-28.** This is the live roster: every name below is what the game shows today.
> Nothing generates it — `ClassCatalog.All` holds the 2nd-class names and `ClassNames.Table` the
> 3rd/4th — so if you change a name here, change it there in the same pass.

## What changed, in two rulings

**`BL-100` — the names.** *"now just sound over complicated ... but I want it simpler ... All races
are the same until lvl 40 so we can call it elf-A human-A"*

1. **The 2nd class (20-39) is race + role** — *Human Rogue*, *Elf Apprentice*, *Demon Knight*.
   Nothing differs before 40 — same kit, same formulas — so a flavour name there promised an identity
   the game does not deliver, and it was spending the six best words we had (`Assassin`, `Sentinel`,
   `Templar`, `Shadowblade`, `Stalker`, `Champion`) on the one tier with none. **All six moved down
   onto 3rd classes that earn them.**
2. **The 3rd/4th say what the class DOES**, in words a player already owns. The coined compounds are
   gone: `Bladesworn`, `Galeherald`, `Bramblewarden`, `Gracebinder`, `Skullbreaker`, `Celestine`.

**`BL-101` — the third race is DEMON.** Your own idea, and it earned itself: the world already spawns
*Orc Archer* from level 12, so the player race was sharing its name with common trash. It also killed
the last exception — the support line had to hide behind `Shaman` because *"ork priest just dont have
the ork sounding"*, and **Demon Priest → Dreadcaller → Warlock** is a line with a voice.

🔑 **`Race.Demon` is still value 2.** A character persists the number, so every save is the same race
under a new name. Nothing broke, no `game.db` reset.

---

## THE ROSTER

### Human

| path            | 1st           | 2nd                  | 3rd          | 4th              |
| --------------- | ------------- | -------------------- | ------------ | ---------------- |
| tank            | Human Fighter | **Human Knight**     | Iron Guard   | Knight Commander |
| warrior, single | Human Fighter | **Human Warrior**    | Champion     | Sword Master     |
| warrior, AoE    | Human Fighter | **Human Warrior**    | Vanguard     | War Master       |
| rogue, dagger   | Human Fighter | **Human Rogue**      | Assassin     | Nullblade        |
| rogue, bow      | Human Fighter | **Human Rogue**      | Sharpshooter | Deadeye          |
| healer          | Human Mage    | **Human Priest**     | Holy Priest  | Holy Messenger   |
| buffer          | Human Mage    | **Human Priest**     | Doctor       | War Doctor       |
| nuker           | Human Mage    | **Human Apprentice** | Mana Adept   | Arcane Master    |

### Elf

| path            | 1st         | 2nd                | 3rd              | 4th           |
| --------------- | ----------- | ------------------ | ---------------- | ------------- |
| tank            | Elf Fighter | **Elf Knight**     | Templar          | Paladin       |
| warrior, single | Elf Fighter | **Elf Warrior**    | Swiftblade       | Sword Saint   |
| warrior, AoE    | Elf Fighter | **Elf Warrior**    | Skirmisher       | War Storm      |
| rogue, dagger   | Elf Fighter | **Elf Rogue**      | Phantom          | Shadowblade   |
| rogue, bow      | Elf Fighter | **Elf Rogue**      | Sentinel         | Trapper       |
| healer          | Elf Mage    | **Elf Priest**     | Forest Whisperer | Forest Elder  |
| buffer          | Elf Mage    | **Elf Priest**     | Harmonist        | War Harmonist |
| nuker           | Elf Mage    | **Elf Apprentice** | Water Adept      | Ice Master    |

### Demon

| path            | 1st           | 2nd                  | 3rd          | 4th            |
| --------------- | ------------- | -------------------- | ------------ | -------------- |
| tank            | Demon Fighter | **Demon Knight**     | Dread Knight | Abyssal Knight |
| warrior, single | Demon Fighter | **Demon Warrior**    | Ravager      | Berserker      |
| warrior, AoE    | Demon Fighter | **Demon Warrior**    | Warborn      | Warbringer     |
| rogue, dagger   | Demon Fighter | **Demon Rogue**      | Stalker      | Venomblade     |
| rogue, bow      | Demon Fighter | **Demon Rogue**      | Soultracker  | Soulhunter     |
| healer          | Demon Mage    | **Demon Priest**     | Dark Healer  | Occultist      |
| buffer          | Demon Mage    | **Demon Priest**     | Dreadcaller  | Warlock        |
| nuker           | Demon Mage    | **Demon Apprentice** | Fire Adept   | Inferno Master |

All fifteen 2nd classes are race + role now — **no exceptions left**, which is what the Demon rename
bought.

---

## The nuker ladder is the ELEMENT growing up

> *"about the nukers .. apprentices -> water/fire/?(something smaller than arcane) adept -> ice or
> blizzard master/inferno master/arcane master"*

| race  | 3rd — the lesser form | 4th — mastered     |
| ----- | --------------------- | ------------------ |
| Elf   | **Water** Adept       | **Ice** Master     |
| Demon | **Fire** Adept        | **Inferno** Master |
| Human | **Mana** Adept        | **Arcane** Master  |

Water hardens into ice; fire swells into an inferno. **The human is the odd one, and you spotted why**
— *arcane* names a SCHOOL, not a magnitude, so there is no smaller word for it the way water is
smaller than ice. **`Mana`** is the raw stuff the art is made of, and a word the player already owns
off the blue bar, so *Mana Adept → Arcane Master* still reads as raw → mastered. (`Ether Adept` and
`Spell Adept` were the other two candidates.)

⚠ I took **Ice Master** over your `Blizzard Master`: *Blizzard* is a very well-known game company's
name, and ice loses nothing.

## 🔑 EVERY AoE / SUPPORT 4th CLASS IS A "WAR" WORD

> *"my general idea is anything aoe is War named -> war master, war doctor, war harmonist,
> warbringer, war chanter, warlock (its not war per say but it contains it as a letter :))"*

It holds across **all six**, and it is now written into `ClassNames` so the next name gets checked
against it:

| path         | Human          | Elf               | Demon          |
| ------------ | -------------- | ----------------- | -------------- |
| warrior, AoE | **War** Master | **War**storm      | **War**bringer |
| buffer       | **War** Doctor | **War** Harmonist | **War**lock    |

⚠ **And the 3rd is that 4th's LESSER FORM — never a word that merely rhymes with it.** That is what
fixed the elf: `Sword Dancer → War Dancer` was the one row on the whole roster chosen backwards, for
the rhyme. **`Skirmisher → War Storm`** replaces it, and it puts the war_aoe 3rd tier in one voice —
**Vanguard / Skirmisher / Warborn**, three martial POSITION words, the way the nukers are three
elements. A skirmish is what one fighter does; a warstorm is the whole battle.

*(`Windblade` was the other candidate and reads well on its own — it just sits too far from Vanguard
and Warborn to belong to the set. `Tempest → War Storm` is semantically perfect and was rejected on
sight: `Discipline.Tempest` is the enum value retired in `BL-97`, and a live class named after a dead
discipline is a trap that costs somebody a build later.)*

⚠ **`Sword Dancer` also sat one letter from a wood-elf unit in another well-known fantasy game** —
the naming rule covers that as much as it covers IG. Gone with the rest.

## The titles are one descending ladder now

> *"sentinel to stay as class and titles to read: supreme being(owner) -> god(admin) -> demi god(mod)
> -> warden(chat mod) -> player"*

| rank           | plate                       |
| -------------- | --------------------------- |
| Owner          | Supreme Being               |
| Admin          | God                         |
| Moderator      | **Demi God** — was Sentinel |
| Chat moderator | **Warden** — was Silencer   |
| everyone else  | —                           |

That closes the clash the build turned up: `Sentinel` is the elf archer 3rd class and could not also
be the plate that means *"this person is staff"*. You kept the class and moved the title — and the
four now read as one order instead of four unrelated words.

🔑 `Demi God` is safe even though *Demigod* was a deleted CLASS (id 98, gone 2026-08-07 with the God
layer): nothing resolves a title to a class, and the id stays dead forever.

---

## The four places the built table differs from your written list

1. **Elf warrior is `Swiftblade → Sword Saint`**, not `Sword Master → Sword Saint`. `Sword Master` is
   the human's 4th, and since `BL-97` the duplicate-name guard has **no exemptions left** — so it was
   a **hard startup failure**, not just the smell you spotted.
2. **Demon bow is `Soultracker → Soulhunter`** — your demon row's own order. Your ork row had said
   `hunter → tracker`; the demon row said the reverse, and *hunter* is the stronger endpoint.
3. **`Adept`, not `Apprentice`, at the 3rd tier** — an apprentice at level 40 reads junior, and
   `Demon Apprentice → Fire Apprentice` repeated the word.
4. **`Ice Master`, not `Blizzard Master`** — see above.

## On the IP flags — where they landed

You were right on the two you pushed back on, and right again on `Warlock`:

- **Templar** — theirs is *Temple Knight* (3rd) and *Eva's Templar* (4th); ours is a bare *Templar*
  at the 3rd. Compound, different tier. **Dropped.**
- **Sentinel** — theirs is *Moonlight Sentinel* at the 4th; ours is bare, at the 3rd. **Dropped.**
- **Warlock** — ✅ **you applied my own rule better than I did.** The test is word + same race + same
  ROLE. Theirs is a summoner; ours is a **buffer**. Two of three fail. Kept.
- **Hell Knight** — you swapped it for **Dread Knight** yourself, which was the safer of your two
  anyway. `Hell Guard` stays on record as the other fallback.
- **Paladin** — the one bare exact match left: theirs is a human tank 3rd, ours the elf tank 4th, so
  race and tier differ. You ruled it *"to generic"* to matter. Built as asked, recorded here so the
  decision stays visible rather than forgotten.
- **`Juggernaut`** went with the ork — *"sounds orkish"* — so the demon tank ends on **Abyssal
  Knight**, and Juggernaut is now unused.

🔑 **The rule that came out of all of it: the test is word + SAME RACE + SAME ROLE, not the word.**
A common noun used descriptively (`Assassin`, `Paladin`, `Berserker`, `Phantom`, `Stalker`) is about
the weakest claim there is — judgment, not legal advice.

## Nothing is open

Every flag raised over this pass is closed: the `Sentinel` title clash, the `Lifebringer` /
`Light Bringer` confusion, the bow order, the three IG names in the demon column, and `Sword Dancer`
— the last one settled 2026-08-28 as **`Skirmisher → War Storm`**.
