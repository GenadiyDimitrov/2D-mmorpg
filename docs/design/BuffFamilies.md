# Buff families — one effect line, many wrappers (`BL-263`)

**Status: 🔵 YOUR DESIGN, RECORDED. Nothing built.** This is the shape you described on 2026-09-18,
checked line by line against the code, with the gaps named and a staged way in. It does not replace
[BuffLadders.md](BuffLadders.md) — that document describes the ladders as they are *today*; this one
describes where they are going and which parts of today's shape will fight it.

---

## 1. Your model, in your words

> everything is a wrapper for icon/duration/name/descr/animation/cost/cooldown/casttime/etc… but it
> provides an effect of a family, and the same effect of one family doesn't stack.

Concretely, using Body:

- One **family**, `hp_max`, with six levels: 10 / 15 / 20 / 25 / 30 / 35 % Max HP.
- Many **wrappers**, each with its own id, name, icon, animation, duration, cast, cooldown, cost:
  `human_body`, `demon_body`, `elf_body`, `npc_body`. All four **provide `(hp_max, N)`**.
- **Conflict is `(family, level)` and nothing else.** `demon_body` L5 replaces `npc_body` L≤5, and is
  replaced by `human_body` L≥5. **Duration is never consulted** — *"if a lvl of the family is the
  same or less, the skill is replaced"*.
- A **group** provides several families at once, each one rung above the ceiling:
  `[(hp_max, 7), (hp_regen, 7), (…, max+1)]` — so no single can ever take one part back.
- `/buff demon_body 6 1h` → *"Demon Body"* L6 for an hour, **with the demon_body icon, name and
  description**, giving +35% Max HP.
- A **different family name is a different number line and they stack**: `har_hp_max` and `hp_max`
  are two families, so a harmony and a Body sit side by side.

### 1.1 The harmony case, which is where the idea came from

> @48 `harmony of focus` L1 (family `har_focus/1`) == `harmony of wizard` L1 (family `har_focus/1`)
> and can be swapped, but then @52 `harmony of wizard` L2 (`har_focus/2` + `har_alacrity/2`) — nor
> `harmony of alacrity` L1 nor `harmony of focus` L1 can replace it.

This is the real prize. Today the group-vs-single relationship is expressed with a **single number**
(`GroupRank = 100 + level`) that outranks everything by brute force. Your version expresses it as
**per-family levels**, so a group that is strong in one family and weak in another can say so, and
the swap at equal level works in both directions for free.

### 1.2 A different family name can also be a LADDER of wrappers

> `harmony_of_protection/7` and `harmony_of_body/6` → both give `harmony_hp_max`, just har_protection
> is from lvl 7 up while har_body is up to lvl 6 and is overridden by the protection one.

One family, two wrappers covering different stretches of it. Falls straight out of the model.

### 1.3 The passive extension (your "not necessary, just an idea")

> both provide casting speed ×2 … if I forgot to make one replace the other they stack and get me
> cast ×4 — while if we have `passive_cast_armor/20` (L1 == 0.1 … L20 == 2), even if I have 10
> passives they provide the effect only once (the strongest).

**This risk is real today and it is not theoretical.** `Entity.RecomputeDerived` → `ApplyPassive`
(`Entity.cs:3658`) folds *every* learned skill's `PassiveEffect` in, and cast speed **multiplies**:

```csharp
if (pe.CastSpeedPct != 0f)
    CastSpeedMultiplier = Math.Clamp(CastSpeedMultiplier * (1f - pe.CastSpeedPct), 0.4f, 2.5f);
```

Two mis-authored passives really do compound. The only thing standing between us and your ×4 is the
`0.4f … 2.5f` clamp — which is a *ceiling*, not arbitration: it hides the bug instead of refusing it.
There is **no family concept on passives at all**.

---

## 2. What the code already does that matches

More than you would think. The vocabulary is all there:

| Your term | In the code | Where |
|---|---|---|
| family | `SkillDef.BuffKey` / `BuffInstance.Key` | `Entity.cs:18` |
| level on that family | `BuffInstance.Rank` | `Entity.cs:19` |
| a group's extra families | `BuffInstance.CoveredKeys` | `Entity.cs:25` |
| every family a buff occupies | `BuffInstance.Families` | `Entity.cs:28` |
| "level IS the rank" | `rank = def.Rank + level - 1` | `GameLoopService.cs:13155` |
| a group outranks every single | `GroupRank(level)` = 100 + level | `GameLoopService.cs:13141` |
| wrapper owns duration, child owns numbers | `SingleBuff(… DurationTicks not passed …)` | `Skills.Common.cs:228` |

And **one whole shape of your design is already shipped**: `cast_hp_max`, the buffer class's own
castable Body (`CastSingle`, `Skills.BuffLadders.cs:252`). One id, six levels, each level naming its
rung. That is your `(wrapper, level)` model, built, working, and in the game since 0.40.0.

So the question is not "can the engine do this". It is "which places can't".

---

## 3. THE FIVE GAPS — in the order they will bite

### GAP 1 🔴 A buff occupies several families but carries only ONE level

`BuffInstance.CoveredKeys` is `string[]`. A covered family has **no level of its own** — it is
covered *absolutely*, and the group wins everywhere by its one `GroupRank`.

Your model wants `[(hp_max, 7), (hp_regen, 7)]` — a level **per family**.

**Why this is the one that bites.** Today's substitute is an authoring rule enforced by a human
reading `CLAUDE.md`:

> ⚠ Authoring rule: a group must be ≥ the best single in EVERY family it covers.

Nothing checks it. The day a group covers a family where it is weaker than a single, the group wins
anyway and silently downgrades the player — and that is invisible, because the buff lands, the icon
appears, and only the number is wrong. **That exact failure mode has already happened twice**:
0.36-0.41's group-as-a-bag-of-children, and `BL-108`'s group that folded magnitudes but dropped every
field (*"the buff landed, its icon appeared, and the numbers were zero"*).

Per-family levels turn a rule you must remember into **data a boot check can verify**.

### GAP 2 🔴 Equal level keeps the LONGER DURATION — you want duration ignored

`ApplyBuff`: same family, equal rank → whichever has more time left survives. You want: *equal or
lower level is replaced, full stop.*

**We already paid for this once.** `SkillCatalog.HarmonyRank = NpcBuffRank + 1` exists for exactly
one reason, and the comment says so: both shelves sat at the same rank, the NPC's single harmony runs
an **hour** and the Warchanter's class harmony **five minutes**, so at equal rank the bought single
refused the real buffer's own. The fix was a +1 constant. Under your rule that hack is unnecessary
and would be deleted.

⚠ **The one thing playtesters will notice:** an NPC 1-hour Body 6 gets wiped by a party buffer's
20-minute Body 6. You said that explicitly (*"we don't care for duration"*) and it is the right call —
but it should be a line in the patch notes, not a surprise.

### GAP 3 🟡 A wrapper's NAME AND ICON do not survive onto the buff

This is what stops `demon_body` today. `ApplyBuff`'s one-child branch (`GameLoopService.cs:13238`)
recurses into the child **without forwarding `displayName`**:

```csharp
if (kids is { Length: 1 } && SkillCatalog.Get(kids[0]) is SkillDef onlyChild)
    return ApplyBuff(target, onlyChild, 1, refresh: refresh, toggle: toggle, …);
//                                      ↑ displayName is dropped
```

So a `demon_body` wrapper would land on the bar named **"Body"**, with Body's icon. Your
`/buff demon_body 6 1h` → *"Demon Body"* requires this one argument to be passed.

**Same root cause as the cast-bar mismatch you noticed.** The cast bar shows the WRAPPER's name
(`ClassSkills.DisplayName(def.Id, …)`, `GameLoopService.cs:11982`); the buff bar shows the CHILD's.
Note also that `ClassSkills.DisplayName` **takes no level** — so the day a class buff gets per-rung
names, the cast bar shows the wrong rung and nothing will complain.

### GAP 4 🟡 The rung is addressed by ID, not by `(family, level)`

`ChildBuffs` is `string[]` — an id with no level (`Skills.cs:81`). Every reference to a rung is a
bare string: group child lists, `ItemDef.UseSkillId`, the shelf CSV. Your model wants
`Provides: (string Family, int Level)[]`.

**And a correction to a comment that is currently WRONG** — `Skills.BuffLadders.cs` says:

> Nothing persists a rung id (buffs die with the session), so no character carries a stale one.

**Buffs do persist.** `BuffSnapshot.CaptureAll` (`PersistenceService.cs:1129`) writes `b.SkillId` —
which for a wrapper-applied buff is the **child**, i.e. `buff_hp_max_6` — into `BuffsJson`. So rung
ids are in the database. That makes the 2026-08-20 renumbering riskier than its comment claimed, and
it means moving to `(family, level)` is a **save-format change**. Pre-release that is a `game.db`
delete, which costs nothing — but it has to be said out loud rather than discovered.

### GAP 5 🟢 Passives have no family arbitration — see §1.3

No mechanism, and no check. The cheapest useful thing here is **not** the mechanism.

---

## 4. What I would actually do, in order

Each step stands alone and is worth shipping on its own. Nothing here is a rewrite.

**Step 1 — give `CoveredKeys` a level. `(string Family, int Rank)[]`.**
No behaviour change: author every existing group at its current effective rank and the game plays
identically. Then add a **startup assertion**: for every group, its declared rank in a family ≥ every
single def in that family. This is the step that stops today's system biting, and it is the smallest.

**Step 2 — forward the wrapper's name and icon to the child instance.** One argument in one recursive
call. Buys: racial Body variants with *zero* new machinery, the `/buff demon_body` behaviour you
described, and it closes the cast-bar mismatch at the same time. Also worth giving
`ClassSkills.DisplayName` a level parameter while we are in there.

**Step 3 — drop duration from the conflict rule.** Equal or lower level is replaced. Delete
`HarmonyRank`'s +1 hack, which exists only to work around it.

**Step 4 (larger, optional) — `Provides: (Family, Level)[]` replacing `ChildBuffs: string[]`**, with a
per-family value table. This is the full version of your model. Do it only if steps 1-3 have not
already bought what you want, because it touches every author site, `ItemDef.UseSkillId`, the shelf
file and the save format.

**Step 5 (separate track) — passives. Start with a CHECK, not a mechanism.** A boot assertion that
flags any two *learnable-together* passives feeding the same channel without a `Replaces` between
them. That catches the mis-authoring you are actually afraid of, today, for a fraction of the cost of
a passive-family layer. If the check turns out to fire often, build `passive_cast_armor/20` then.

---

## 5. Two constraints any version must respect

1. **`SkillEffect` HAS ZERO BITS LEFT** (full since `1L << 62`). Anything new rides `SkillDef` /
   `SkillLevel` **fields**, the way CC resistance, MP-cost reduction, magic accuracy, magic crit
   damage and heal-received already do. A "family value table" row is therefore a **payload**
   (`EffectMagnitude[]` *plus* fields), never one number.
2. **Not every rung is one stat.** Frenzy's rung is a whole multi-effect buff with a penalty and four
   gains; the CC-resist rungs carry *only* fields and no `Effect` at all. Any family table must hold
   a full payload or those two families fall out of the model.

---

## 6. Where this came from

Your messages of 2026-09-18, in the conversation that started *"Why are its ids `buff_hp_max_1`~
`buff_hp_max_6`? And what is `npc_body`?"*. The question was answered in chat; this file is the
design that came out of it. See also [BuffLadders.md](BuffLadders.md) for the system as it stands.
