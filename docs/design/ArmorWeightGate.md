# The `WEIGHT` column — an armour gate any skill can carry

**Status: ✅ BUILT 2026-08-29 (0.102.0). You said yes to all three (§5).** `BL-107`.
Your message, 2026-08-29:

> *"Can we do the same as we did for weapon? Add a column a required weight : heavy|light|heavy|shield
> and the description is the one that splits it … if column is empty (and the weapon one as well) means
> it works regardless … That way I can make the tank_shield_mastery L4 to work only on heavy|shield and
> not give the % defence on any armor except the heavy."*

Yes to the column. This document says what it should mean, where I disagree with the spelling, and why
your third idea (a Description column / `{0}` templates) is **already built** and should not be added.

---

## 1. The gap is real, and it is narrower than it looks

A weight gate exists in the engine **today** — but only inside one record, `ArmorMasteryProfile`, which
has a `StatMods` slot per weight (`Robe` / `Light` / `Heavy` / `None`). That is why
`warrior_armor_mastery` can already say "light gets evasion, everyone gets P.Def".

Everything else cannot. A plain passive is one `PassiveEffect` on the rung, applied unconditionally, and
there is **nowhere to hang a weight**. So today:

| Want to say | Can you? |
|---|---|
| "in LIGHT, +6 evasion" on an armor mastery | ✅ built (`ArmorMasteryProfile`) |
| "in HEAVY, +10% P.Def" on any other passive | ❌ **impossible** |
| "requires a shield" on any passive | ✅ built (`PassiveEffect.RequiresShield`) |
| "requires HEAVY **and** a shield" | ❌ **impossible** — and this is your Shield Mastery L3/L4 |
| "this ACTIVE needs heavy armour" | ❌ impossible (the weapon twin, `RequiredWeapon`, exists) |

The shield row is the one that is already half-built: `PassiveEffect.RequiresShield` was added on
2026-08-20 for the healer's shield mastery. The weight axis is the missing half.

**Evidence that the gap has already cost something.** `PassiveEffect.DefencePctWithShield` exists as its
own bespoke field for one reason: Shield Mastery's "+10% P.Def" needed a gate and there was no general
one. Your note at the time (2026-08-21) was *"IG is shield+heavy but I'm not sure if we can"* — you asked
for shield-only because heavy was not expressible. It is the same request you are making now.

---

## 2. The grammar — one disagreement, and it is the same one as last time

Your spelling: `heavy|light|shield`.

**A shield is not an armour weight.** It is a different equip slot, and it coexists with every weight —
so `heavy|shield` under an OR-reading means *"heavy armour, **or** a shield with any armour"*, which pays
a robe-wearer with a buckler the +10% P.Def you just said should never leave heavy. You need AND, and `|`
cannot say it.

This is exactly the `WEAPON` lesson from yesterday. A bare type meant *any hands*, so "one-handed" was
unsayable until hands became **their own axis** after the `/`. Shields are the same shape:

```
WEIGHT  =  weight[|weight…][/shield]
```

| Cell | Means |
|---|---|
| *(empty)* | no requirement — works in anything, naked included |
| `heavy` | heavy body armour; shield irrelevant |
| `light\|heavy` | light **or** heavy; robe and bare torso get nothing |
| `robe\|light\|heavy` | any armour, but **not** bare — this is the buffer's `Light/Heavy/Robe:` rows |
| `/shield` | shield equipped, any armour — Shield Mastery rungs 1-2 |
| `heavy/shield` | heavy **and** a shield — Shield Mastery's "+10% P.Def" |
| `/noshield` | *(only if something ever wants it — say the word and it is one token)* |

Weights: `robe` · `light` · `heavy` · `bare` (no body armour). Order and case irrelevant, same as
`WEAPON`. Anything after `/` that is not `shield`/`noshield` is a 🔴 error, same as `/3` is on `WEAPON`.

One grammar for both columns — `[set]/[axis]` — instead of two.

---

## 3. The DESCR split — keep your prose, drop the brackets

Your idea: `["light: does this","heavy: does this"]`.

**The split itself is right and I want it. The JSON array is the part I would not do**, for three reasons:

1. **You already write it.** Your 24 files carry per-weight clauses in three spellings today —
   `with light: … ; with all …` (fighter/warrior/rogue), `Light/Heavy/Robe: …` (buffer/nuker),
   `Robe: … ; Light: …` (cleric). Normalising those keys is an edit. Re-quoting every cell into an array
   is a rewrite.
2. **Quotes inside a CSV cell are the known corruption vector.** A cell containing `["a: x","b: y"]` is
   a quoted field holding quotes. Excel re-quotes cells when it saves, and an editor writing a stale
   cached copy of these files has already reverted two shipped commits once
   (`csv-files-corrupted-by-editors`). Adding a second quoting layer on top of CSV's own invites it back.
3. **The reader already segments on those labels.** `Descr.cs` splits DESCR on `;` and on scope labels
   (`Robe:`, `with all`, `with sword/blunt`) — so a fixed key vocabulary makes the checker verify
   **per weight** with a small change, where an array needs a new parser first.

**So: same idea, existing syntax.** A fixed vocabulary of clause keys, `;`-separated:

```
DESCR:  heavy: p.def +40 and p.def x1.07;  any: mpReg x1.1
```

`robe:` · `light:` · `heavy:` · `bare:` · `shield:` · `any:` — where **`any:` means "every gear state the
WEIGHT column lists"**, which is precisely your rule (*"the 'any' part will work only on heavy or
light"*). `with light:` and `Light/Heavy/Robe:` stay legal spellings of the same keys; I would normalise
them on the way past, not force them.

### What this buys the checker

Today `--check` reads the numbers out of DESCR but **has no idea which weight they belong to** — it
compares against whatever the profile happens to carry. With the column and the keys it can assert three
separate things: that the cell's weight set matches the code's gate, that each clause's numbers match
*that weight's* slot, and that a clause naming a weight the column does not list is an error. That is the
same jump `WEAPON` made yesterday, on the axis where the CSV is currently least verifiable.

---

## 4. Your third idea is already built — do not add a Description column

> *"If it's possible in the code the description of some level of passive/skill should say
> 'Heavy: +{0}hp, +{1}P.Def' so it's dynamic based on the lvl of the skill … If not we will need an
> actual Description column."*

**It is possible and it already happens.** `Game.Shared/SkillText.cs` generates the numbers from the
data, and the client prints them per level in the skill window — `SkillText.ArmorMastery` returns exactly
`"Heavy:  P.Def +40, Max MP +30"`, grouped by weight, built from the rung you are hovering. Nothing is
templated and nothing is authored twice; the authored `Description` is only the prose sentence above it.

So a `DESCRIPTION` column would restate data the game already renders itself, and would go stale the
first time a number changes — the one failure mode the CSV-mirror rule exists to prevent.

**What is genuinely missing is one line, and the column supplies it:** the generated block never states
the *gate*. Once `WEIGHT` exists, `SkillText` should print `Requires: heavy armour + shield` above the
numbers, from the same mask the engine checks. That is the whole gap, and it is a few lines, not a column.

---

## 5. What I asked — and your answers, 2026-08-29

1. **`heavy/shield` instead of `heavy|shield`?** (§2 — `|` cannot say AND, and your Shield Mastery
   example needs AND.)
2. **Does `light|heavy` really turn robe off for the warrior and the rogue?** It is what you wrote
   (*"equipping robe or naked will not work"*) and I will build it, but say it once explicitly: today
   both masteries pay their "with all" half in **robe** too, and that was itself a deliberate fix — a
   rogue in robe used to get no MP regen, no HP regen and no P.Def at all (2026-07-01). Turning robe off
   is a small nerf to a fighter who is wearing the wrong armour on purpose. Fine by me; your call.
3. **DESCR keys, not a JSON array?** (§3.)

**All three: yes.**

1. ✅ *"I like it to do it same as 'weapon' column. `heavy/shield` == heavy and shield required …
   `heavy|light` == heavy or light required"* — built exactly as §2.
2. ✅ *"Yes turn off robe and naked from warrior mastery no point in them wearing a robe. And we have
   nothing that strips you from armor/weapon."* The second sentence is the right argument and it is why
   this is safe: nothing in the game can disarm you, so a fighter in a robe chose to be there.
   ⚠ **Applied to the ROGUE's mastery as well** — the question named both and the reasoning is
   identical. One line each in `Skills.Masteries.cs` if you want the rogue's back.
3. ✅ *"Descr keys as they are now.. It's easy for me to write them."* Agreed, and it is easier here
   too — the reader already knows six spellings of each stat, so a rename would cost 24 files a full
   rewrite and buy the parser nothing. What you asked for instead is
   **[`DESCR-KEYS.md`](../data/classes_skills_csv/DESCR-KEYS.md)**: every key, every spelling it
   accepts, and every scope label — **generated** from the parser's own table
   (`--descr-keys`), so it cannot drift from what `--check` actually reads.
   ⚠ There is no `AllDef`/`alldef%` in your example: P.Def and M.Def are separate stats everywhere in
   the game and no skill authors "all defence". Nothing to map.

## 6. What was built

One increment, 0.102.0:

- **`ArmorWeights`** (a [Flags] weight MASK, the requirement side of `ArmorWeight`) + **`ShieldGate`**
  (`Any` / `Required` / `Forbidden`) + **`ArmorGate`** — `Satisfies` / `Describe` / `Format` /
  `TryParseRequirement`, the exact twin of `WeaponTypes`, in `Items.cs`.
- **`SkillDef.RequiredArmor` / `RequiredShield`** — the ACTIVE cast gate, checked in `HandleUseSkill`
  *and* in the auto-hunt chain (a gate the tap refuses must be SKIPPED by the autopilot, not attempted).
- **`PassiveEffect.RequiredArmor`** — the general passive gate, all-or-nothing like `RequiresShield`.
- **`SkillLevel.ExtraPassives`** — extra passive LAYERS per rung, each with its own gate, because an
  all-or-nothing gate cannot describe a rung whose halves differ. Shield Mastery is the reason.
- **`ArmorMasteryProfile.RequiredShield`** — the shield axis on a mastery (nothing authors one yet).
- **`DefencePctWithShield` DELETED.** Shield Mastery's "+10% P.Def" is now plain `DefencePct` under a
  `RequiresShield + Heavy` gate — IG's own rule, and yours.
- **`SkillText`** prints the gate: `Requires heavy armour and a shield` on the skill, and
  `— only with …` under a gated passive's numbers. Both clients read it.
- **`--weight-column`** wrote the column into all 24 files (1,420 rows, 103 real requirements),
  **`--check`** verifies it, and a `heavy:` clause in DESCR now resolves against the matching layer.

---

## 6a. Your correction the same day — Shield Mastery is `heavy/shield` on EVERY rung (0.102.1)

You asked for `/shield` on rungs 1-3 and `heavy/shield` on rung 4, then changed it reading the result:

> *"If we allow the human buffer on a robe+shield … when he become 4th class he have additional bonus
> on the shield when the other 2 buffers wearing any+shield will get only one. So the human buffer
> chooses heavy+shield for becoming semitank and robe+shield works as the other 2 buffers (no shield
> bonuses except the 4th class one) … While giving more pDef and shield rate+Def on a robe pushes one
> class in front a lot."*

Right, and it is the stronger reading. `tank_shield_mastery` is learned by the tank **and by the Human
Warchanter** (rungs 1-3 at 40/60/70) — so a `/shield` gate handed one of the three buffers a ladder the
elf and demon cannot buy at any price. Gated `heavy/shield` it becomes a **choice**: heavy + shield =
semi-tank, robe + shield = the buffer the other two are. The demon's two-handed-blunt lean (a little
P.Atk and accuracy on a weapon that is not for hitting) is a far smaller edge than that was.

⚠ **This collapsed the only user of `SkillLevel.ExtraPassives`.** With one gate for the whole rung the
"+10% P.Def" layer folded back in as plain `DefencePct`. The mechanism stays — a rung whose halves
genuinely differ still needs it — but nothing authors one today. Worth knowing before reaching for it.

⚠ **The 4th-class shield bonus you are counting on exists for the HEALER, not the buffer.**
`healer_shield_mastery` @76 (+10% heal power, +10% MP regen, plain `/shield`, any armour) is on the
Lightbringer's table. The Warchanter's 4th tier has no shield row built; `buffer 4th.csv` 136-138 are
the three 3rd-tier rungs at 40/60/70 pasted into a 76-90 file with a blank `SKILL_ID`. Your file, in
progress — untouched, but say the word if that is meant to be a real 4th-tier passive.

---

## 7. Related

- `BL-105` / [the `WEAPON` column](../data/classes_skills_csv/README.md) — the grammar this mirrors.
- [StatMods.md](StatMods.md) — the two payload records (`StatMods` for per-weight profiles,
  `PassiveEffect` for everything else) and why both exist.
- `BL-92` — hpReg is FLAT HP/s; any weight-gated regen authored here is flat.
