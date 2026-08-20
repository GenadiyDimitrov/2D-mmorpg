# Disciplines — the 40+ kits, race identity, and the CSV format

The single file for 3rd-class content. It merges what used to be three docs: your kit prose
(`Disciplines.md`), the live-tree grid (`DisciplineIdentity.md`) and my old placeholder draft
(`Descipline.md`, deleted — its name list survives as Appendix A).

> 🔴 **Nothing here is built yet.** `BL-02` stands: no level-40+ skill exists until the CSVs below
> land. The only exceptions you granted by name are the two level-83 skills (§8). This is the plan.

---

## 1. How you author it (your rule, 2026-08-14)

> *"I'll make them by discipline (bulwark, vanguard, ravager, warlord, meleeRogue, archer … etc) and
> add another column at the end if it's only for the specific race — if no race specified it's a skill
> for all. All will have a lot in common, just a few buffs, 1-2 skills to underline the race identity."*

So the unit of authoring is the **discipline**, not the class, and race is a **filter on a row**, not
a separate table. That collapses the 30 third classes into **10 CSVs**:

| CSV | Covers | Discipline per race (Human / Elf / Ork) |
|---|---|---|
| `bulwark 40+.csv` | tank, defensive | Bulwark / Bulwark / Bulwark |
| `vanguard 40+.csv` | tank, offensive | Vanguard / Vanguard / Vanguard |
| `ravager 40+.csv` | warrior, single-target | Ravager / Ravager / Ravager |
| `warlord 40+.csv` | warrior, AoE | Warlord / Warlord / Warlord |
| `melee rogue 40+.csv` | dagger | **Nullblade / Phantom / Venomweaver** |
| `archer 40+.csv` | bow | **Sharpshooter / Trapper / Hunter** |
| `magus 40+.csv` | nuker, single-target | Magus / Magus / Magus |
| `tempest 40+.csv` | nuker, AoE | Tempest / Tempest / Tempest |
| `lightbringer 40+.csv` | healer | Lightbringer / Lightbringer / Lightbringer |
| `warchanter 40+.csv` | buffer | Warchanter / Warchanter / Warchanter |

⚠ **The rogue line is the one place where the class NAME changes per race** — a blank race column in
`melee rogue 40+.csv` still means "all three", it just registers under a different discipline id for
each. Eight of the ten CSVs are one discipline; those two are three each. Nothing in the code needs
changing for this: 3rd-class skills already register per `(race, discipline)`.

### 1.1 The CSV format

Same header as the 20-35 files, plus **`RACE`** at the end. Blank = every race.

```
LEARN @ LVL, NAME,TYPE,RANGE,TARGET, CAST s,CD s,DURRATION s, DESCR,MP,SP COST,REPLACES,RACE
40,Shield Strike,physical active,40,enemy,0,6,0,power 210 + taunt - requires shield,32,12000,[],
44,Ground Stomp,physical active,0,enemy,0,15,10,aoe radius 250 - taunt + atk -20%,60,24000,[],ork
44,Rallying Shout,physical active,0,enemy,0,15,0,aoe radius 250 - taunt + shield 5% maxHP per enemy hit - cap 30%,60,24000,[],human
```

- **`RACE`** — `human` / `elf` / `ork`, or empty. One race per row; a skill for two races gets two rows
  (or leave it blank if it is really for all three).
- Everything else behaves exactly as in the 20-35 files, including `REPLACES` for upgrade ladders.
- **Learn cadence** at 40+ is currently 40, 44, 48, … (step 4) for the nuker kits that exist. Yours to
  change per CSV.

### 1.2 The shape of a kit

Your ratio — *"a lot in common, just a few buffs, 1-2 skills to underline the race identity"* — reads
as roughly:

- **Shared (blank race):** the main damage/taunt/heal line, the ladder passives, the utility.
- **Race-specific (1-2 skills + a buff or two):** the CC school it applies, the self-buff, and the
  rider on the passive.

Every §5 kit below is written in exactly that split, so you can read off which rows carry a race tag.

---

## 2. The race axis — a constant across every discipline

Race never changes *what* a discipline is; it changes *how*. Your line:

> *"human 'evades' magic, the elf evades phys. the ork should outlive the target"*

| Race | Flavour | Stat lean | Its CC school |
|---|---|---|---|
| **Human** | anti-magic + precision | +enemy **magic fail**, crit rate, crit damage, accuracy | **slow** |
| **Elf** | anti-physical + speed | **evasion**, AS/MS/cast, utility | **root** |
| **Ork** | outlives and outdamages | **skill damage**, max HP, defence, attack | **fear** |

The CC column is not decoration — it holds in the warrior, nuker and archer kits already, and it is
what makes a race recognisable at a glance in a fight.

---

## 3. The live tree — 30 third classes

15 second classes × 2 disciplines. Archer was merged into Rogue (2026-07-29): ids 4/10/16 are gone
and bow-vs-dagger is a **level-40** choice.

| Race | 2nd class | → A | → B |
|---|---|---|---|
| Ork | Beast (tank) | Bulwark | Vanguard |
| Ork | Warrior | Ravager | Warlord |
| Ork | Stalker (rogue) | **Venomweaver** (melee) | **Hunter** (bow) |
| Ork | Shaman | Lightbringer | Warchanter |
| Ork | Witch | Magus | Tempest |
| Elf | Templar (tank) | Bulwark | Vanguard |
| Elf | Sentinel (warrior) | Ravager | Warlord |
| Elf | Shadowblade (rogue) | **Phantom** (melee) | **Trapper** (bow) |
| Elf | Priest | Lightbringer | Warchanter |
| Elf | Inquisitor | Magus | Tempest |
| Human | Knight (tank) | Bulwark | Vanguard |
| Human | Champion (warrior) | Ravager | Warlord |
| Human | Assassin (rogue) | **Nullblade** (melee) | **Sharpshooter** (bow) |
| Human | Cleric | Lightbringer | Warchanter |
| Human | Sorcerer | Magus | Tempest |

There is no "Ork Phantom" or "Human Trapper" — each rogue discipline exists in exactly one race.

---

## 4. The grid at a glance

| | Human | Elf | Ork |
|---|---|---|---|
| **Bulwark** | shield + raw defence | shield + **self-healing** defence | **immortality** — immunity window, lethal save |
| **Vanguard** | charge + stun, most damage | ranged lightning stun, speed | **pull** + stun; lower HP → more damage, less taken |
| **Ravager** | 2H sword **crit master** | 2H utility, **root** | 2H blunt **berserker**, fear |
| **Warlord** | **AoE crit**, crit-vamp | AoE + root, on-hit self-heal | AoE + fear, HP/regen/DR |
| **Melee rogue** | *Nullblade* — anti-magic, bleed, crit | *Phantom* — evasion, evade→burst | *Venomweaver* — venom stacks → burst |
| **Archer** | *Sharpshooter* — crit, single-target | *Trapper* — traps, root, kite | *Hunter* — raw damage, party attack buff |
| **Magus** | crit + **mana shield**, slow-lock | **cast speed**, root-lock | fear-lock, HP shield that raises attack |
| **Tempest** | AoE **slow** → +50% vs slowed | AoE **root** → +50% vs rooted | AoE **fear** |
| **Lightbringer** | fast single-target + cleanse | AoE + holds + aggro drop | **totem** AoE + **anti-heal** debuff |
| **Warchanter** | robe → magic crit; light → crit | robe → cast; light → AS/MS | robe → attack; light → attack (more) |

---

## 5. The kits

Your prose, reorganised into **shared** vs **race**. `<name>` = still unnamed (see Appendix A for a
name bank). Slots: `Main 1` · `Main 2` · `Buff` · `Passive`.

### 5.1 Bulwark — defensive wall: near-immortal, minimal damage

**Shared**
- Main 1 `[DMG/TAUNT]` — single-target damage with shield, taunt *(shield required)*
- Main 2 `[AOE/TAUNT]` — multi-target shout/provoke that taunts everything nearby
- Passive — +defence, +max HP *(may be split in two)*

**Race**
| | Main 2 rider | Buff `[self]` | Passive rider |
|---|---|---|---|
| **Human** | gains a **shield** per enemy hit, stacking to 30% max HP | self shield 8% max HP, 15s, cd 15s, instant | +mob **aggro** on hit |
| **Elf** | **heals self** per enemy hit, up to 30% max HP | self-heal 30% max HP, cd 1 min, instant | +**healing received** |
| **Ork** | ground stomp: also **lowers enemy attack** | **immune to damage 10s**, cd 5 min, recovers 25% over it | on lethal damage **recover 50% HP** (5 min cd) |

> Taunt always works: on mobs it raises aggro, on **players it forces their target onto the tank**.

### 5.2 Vanguard — offensive tank: high defence + real damage

**Shared**
- Main 1 `[DMG/TAUNT]` — single-target hit with shield, taunt *(shield required)*
- Main 2 `[DMG]` — a **stun** delivered differently per race (below)
- Passive — +attack, +max HP

**Race**
| | Main 2 | Buff `[self]` | Passive rider |
|---|---|---|---|
| **Human** | **charge forward**, damage + stun *(at melee range: just damage + stun)* | +damage, +defence, +max HP | +mob aggro on hit |
| **Elf** | **ranged lightning strike**, damage + stun | +attack speed, +move speed, +defence | **vamp on hit** |
| **Ork** | **pull** the target to you, damage + stun *(if the pull resists the stun can still land)* | +attack, +crit rate, +crit damage | **lower HP → more damage**, and damage reduction up to **50% at 25% HP** |

> ATK raises stun/pull chance, CON resists, bosses immune, 10-90% clamp — the normal debuff contest.

### 5.3 Ravager — pure single-target burst, low survivability

**Shared**
- Main 1 `[DMG]` — big single-target slash `[Double]` ⚠ *(your text omits `[Double]` on the Ork — §9)*
- Main 2 `[DMG/CC]` — big **frontal horizontal strike**, can hit several enemies upfront; the CC differs
- Passive — +attack, +max HP, plus a weapon-conditional rider

**Race**
| | Main 2 CC | Buff `[self]` | Passive rider |
|---|---|---|---|
| **Human** | **slow 90% / 10s** | 15-30s: +attack, +crit rate, +skill crit rate, +crit damage (cd 1-2 min) | **2H sword**: +crit rate, +crit damage |
| **Elf** | **root 10s** | +attack, +attack speed *(standard 20 min buff)* | **any 2H**: +MS, +AS |
| **Ork** | **fear 5s** | 15-30s: +attack, **each attack can stun** (cd 1-2 min) | **2H blunt**: attack rises as HP falls |

> Warriors apply **physical** root/slow/fear.

### 5.4 Warlord — balanced bruiser with AoE

**Shared**
- Main 1 `[DMG/AOE]` — big AoE slash `[Double]` ⚠ *(same Ork omission)*
- Main 2 `[AOE/CC]` — AoE damage plus the race's CC
- Passive — +attack, +max HP, plus a weapon-conditional rider

**Race**
| | Main 2 CC | Buff `[self]` | Passive rider |
|---|---|---|---|
| **Human** | **slow 50% / 10s** | +attack, +crit rate, +crit damage | **2H sword**: crits can **vamp** |
| **Elf** | **root 5s** | +attack, +AS/MS | **any 2H**: basic attacks can heal % max HP |
| **Ork** | **fear 3s** | +attack, +max HP, +HP regen | **2H blunt**: +damage reduction |

### 5.5 Melee rogue — Nullblade (H) / Phantom (E) / Venomweaver (O)

🟡 **This is the kit your new format changes most, and it settles the open Nullblade question.**
It used to be two disciplines (Phantom, Venomweaver) × 3 races; the merge left the Human cell holding
two half-kits. Under "shared core + 1-2 race skills" it resolves cleanly: **the stealth kit is the
shared core, and the DoT line becomes the Ork's race skill.** Say if you'd rather split it differently.

**Shared**
- Main 1 `[DMG]` — single-target strike, **increased damage if cast while hidden** `[Double]`
- Main 2 `[DMG]` — **blink behind** the target + damage, increased if hidden `[Double]`
- Buff `[self]` — **hide 30s**; acting cancels it; for 30s afterwards a race rider applies (below)
- Passive — with **dual** and with **light** armour, a race rider (below)

**Race**
| | Post-hide rider | Passive | Race skill (the 1-2) |
|---|---|---|---|
| **Human** *(Nullblade)* | +enemy **magic fail** | if a spell **fails** against you, the next skill gets the **full** hide bonus; dual → +crit rate/damage; light → +magic fail | **bleed** apply + a burst that consumes the stacks `[Double]` |
| **Elf** *(Phantom)* | +**evasion** | if a physical attack is **evaded**, the next skill gets **half** the hide bonus *(evasion is likelier than a spell failing, so it is halved)*; dual → +AS/+MS; light → +evasion | — *(the evade→burst payoff IS its identity)* |
| **Ork** *(Venomweaver)* | +skill damage, +max HP, +defence | chance to apply **venom** on hit; dual → +skill damage; light → +max HP/+defence | **venom** apply + a burst that consumes the stacks `[Double]` |

> "Increased damage if stealth was active" = enter hide → cast **while hidden**. The after-effect
> window does *not* grant it.
>
> **DoT rules:** debuff 30s, skill cd 3s, **max 10 stacks**, re-hitting refreshes; burst = damage ×
> stacks (×10 at full); the applier must be able to **see the stacks** on the target.
> bleed = AGI vs CON, slows MS · poison = ATK vs WIT, slows AS/cast · venom = AGI vs CON, lowers atk/def.

### 5.6 Archer — Sharpshooter (H) / Trapper (E) / Hunter (O)

**Shared**
- Main 1 `[DMG]` — single-target damage `[Double]`
- Main 2 `[DMG]` — focused shot + **knockback**: 2s cast, +20% double chance `[Double]`
- Buff `[self]` — **+range**, plus a race rider
- Passive — with **bow**, basic attacks **proc** something for self + party; with **light**: +MS, +evasion

**Race**
| | Buff rider | Passive proc | Race skill |
|---|---|---|---|
| **Human** *(Sharpshooter)* | +crit rate, +crit damage | +crit rate / +magic crit rate to **self + party** | — |
| **Elf** *(Trapper)* | +MS, +AS | chance to **root 1s** | **place a trap**: on trigger, stun 5s + damage. Main 1 also carries −AS/cast |
| **Ork** *(Hunter)* | +attack, +skill damage | +attack / +skill damage to **self + party** | — |

> ✅ Your Main 2 note *"never misses"* is now redundant — since `BL-06` a physical **skill cannot be
> evaded at all**, so it is true of every skill in the game. Keep the 2s cast and the +20%; drop that
> phrase, or tell me it should mean something else (ignores the boss CC immunity? pierces block?).

### 5.7 Magus — single-target glass cannon

**Shared**
- Main 1 `[DMG]` — big single-target nuke
- Main 2 `[DMG]` — **armour-ignoring** nuke, cd 1 min, applying the race's CC
- Passive — with **robe**: +MP regen, +max MP, plus a rider

**Race**
| | Main 2 CC | Buff `[self]` | Passive rider |
|---|---|---|---|
| **Human** | **slow 75% / 15s** | **mana shield**: 70% of damage → 0.5 MP per 1 damage, 30s / cd 30s | +10% magic crit rate |
| **Elf** | **root 10s** | +max MP, +cast, +MP regen | +10% cast |
| **Ork** | **fear 10s** | HP shield 15s / cd 30s; **while active +attack** | +15% attack |

### 5.8 Tempest — AoE damage + control

**Shared**
- Main 1 `[DMG/AOE]` — AoE damage applying the race's CC
- Main 2 `[DMG/AOE]` — AoE damage, **+50% against targets under that CC**, and lowers its resist
- Buff `[self]` — **blink back 400**, 5s duration, cd 30s, plus a race rider
- Passive — with **robe**: +MP regen, +max MP, plus a rider

**Race**
| | CC | Buff rider | Passive rider |
|---|---|---|---|
| **Human** | **slow 50% / 10s** | +magic crit chance, +slow chance | +magic crit chance |
| **Elf** | **root 7s** | +cast, +root chance | +cast |
| **Ork** | **fear 5s** | +attack, +fear chance | +magic attack |

⚠ Your Ork Main 2 says "+50% vs **rooted**" in a fear kit — read here as "vs feared" (§9).

### 5.9 Lightbringer — pure healer: AoE heals + shields

🟡 Your doc marks this `[DONE]` with no per-race text. Below is reconstructed from the kit that was
actually built (Phase 24.1) plus your worked example of 2026-06-22 — **confirm or overwrite**. The
**Buff and Passive slots have never been written for any race.**

**Shared**
- Main 1 `[HEAL]` — the main heal line
- Main 2 `[HEAL/UTIL]` — the second line, differing per race
- Buff / Passive — ❌ **not written**

**Race**
| | Main 1 | Main 2 | Trade-off |
|---|---|---|---|
| **Human** | strong, **fast single-target** heal *(built: `lb_human_mend`)* | **cleanse** debuffs from an ally *(`lb_human_purify`)* | weaker AoE |
| **Elf** | **AoE** heal, removes bleeds *(`lb_elf_dawn`)* | **holds** the target (root) + **drops own aggro** *(`lb_elf_warden`)* | weaker single-target |
| **Ork** | **totem** AoE heal — placed, allies stand in it *(built as a plain AoE heal, `lb_ork_font`; the totem waits on the summon system)* | **anti-heal** debuff on an enemy *(`lb_ork_sap`)* | positioning-dependent |

Plus **Rite of Preservation @83** — auto-resurrect cast on an ally, `BL-35`. Already in code.

### 5.10 Warchanter — buffer: stat buffs + heal-over-time (farm)

The model case for your format: **both party buffs are shared by all three races**; only the passive
carries race.

**Shared**
- Main 1 `[DMG]` — single-target main damage skill
- Buff `[self + party]` — the big one: **+30%** magic def / cast / attack speed, **+15%** phys def /
  attack, **+45** move speed, **+35%** max HP/MP, **+20%** HP/MP regen
- Buff `[self + party]` — heal **10% max HP** then a **10s HoT at 2%/s**; cd 30s, 2s cast
- Passive — with **robe**: +max MP, +MP regen · with **light**: +attack, +defence, +max HP

**Race** — the passive rider only
| | robe rider | light rider |
|---|---|---|
| **Human** | +magic crit rate | +crit rate, +crit damage |
| **Elf** | +cast | +AS, +MS |
| **Ork** | +attack | +attack (more) |

Already in code: the whole **buff ladder** (singles 40-64, improved/harmony above) and **Madness @76**
— the latter a knowingly temporary home (`BL-34`), to be moved when this CSV lands.

---

## 6. Global combat rules

- **Physical skills can `[Double]`** — same shape as magic crit, driven by the higher of **AGI/ATK**,
  **max 30%**, ×2 damage. Only skills tagged `[Double]`.
- **Magic skills crit** — max **20%**, ×3.
- **Physical debuff = ATK vs CON**; **magical debuff = ATK vs WIT**.
- **stun / pull** — always physical. **root / slow / fear** — either, authored per skill.
- **bleed / venom** — always physical. **poison** — always magical.
- Every contest clamps **10%-90%**, 50% at equal stats. **Bosses are immune.**

---

## 7. Engine readiness

✅ **Callable today, no new code:**
`[Double]` · magic crit · the ATK-vs-CON/WIT contest · **slow / stun / fear / root** · conditional
damage (+% vs slowed/rooted/stunned/feared) · **DoT with stacks + burst consume** and their secondary
debuffs · absorb **shields** · **mana shield** · **lethal save** · **taunt + real threat** · **blink**
(behind / away) · **knockback** · cure / **cancel** + cancel-resist · **hide** · **stealth** ·
**reveal** · **traps** · HoT · vamp · **weapon-conditional passives** ("with dual…") ·
**armour-conditional passives** ("with light…") · skill-damage % by PvE/PvP × phys/magic/basic ·
evade & hit floors · magic-fail modifier · mRes.

🔧 **Needs new code — say so when a CSV row wants it:**

| Wanted by | Missing |
|---|---|
| Vanguard-Ork | **Pull** — the mirror of Knockback, not built |
| ~10 passives (rogue, warrior, archer) | **On-hit procs** — "chance to apply bleed on hit", "basic attacks can slow/root/fear", "crits can vamp", "chance to heal % max HP". `PassiveEffect` is flat stats only; there is no proc channel at all. **The biggest gap by far.** |
| Melee rogue Main 1 & 2 | **"more damage if cast while hidden"** — conditional damage keys on the *target's* state, not the caster's |
| Bulwark-Ork buff | **Damage immunity** for a duration |
| Bulwark Main 2 | **Shield/heal accumulating per enemy hit**, capped at 30% max HP |
| Vanguard-Ork, Ravager-Ork | **HP-scaled** damage and damage reduction |
| every archer buff | **+range as a buff** — `BowRange` exists only as a passive field |
| Sharpshooter, Hunter | **Party-wide proc buff** |
| every tank Main 1 | a **"shield required"** gate — `RequiredWeapon` covers weapon types, and a shield is not one |

⚠ **`SkillEffect` has no bits left** (62 is the last, 63 is the sign). Anything new rides as an
explicit **field** on `SkillDef` — how hide, stealth and traps were added — or goes through `StatMods`.

---

## 8. What exists in code today

| Discipline | Granted at 40+? | What |
|---|---|---|
| **Magus** | ✅ | Annihilate, Mana Burn, Elemental Burst (ult, 10 levels), Frost Bind, Entangling Roots, Glacial Spike, Mana Barrier |
| **Tempest** | ✅ | Chain Lightning, Maelstrom, Elemental Burst, Frost Bind, Entangling Roots, Glacial Spike, Creeping Frost, Phase Shift |
| **Warchanter** | ✅ buffs only | the buff ladder + Madness @76 (`BL-34`, temporary) |
| **Lightbringer** | ⚠ one skill | Rite of Preservation @83 (`BL-35`); the 24.1 heal kit is written but **not granted** |
| **Bulwark** | ⚠ one skill | Undying Will @83 (`BL-35`) |
| everything else | ❌ | purged 2026-08-10 |

The **skill definitions** from the purge all survive in the catalog and are ready-made raw material:
`Shadowstep`, `Vanish`, `Rupture`/`DetonateWounds`, `ToxicSting`/`ToxicBurst`, `Envenom`/`VenomBurst`,
`RepellingShot`, `SnareTrap`, `ShieldBash`, `Provoke`, `Aegis`, `LastStand`, `Indomitable`,
`CleavingStrike`, `Hamstring`, `WarFocus`, `TerrifyingRoar`. Only the learn assignments were deleted.

---

## 9. Open questions

1. **Melee rogue** (§5.5) — confirm the split: stealth kit shared by all three, DoT as the Ork's race
   skill (and optionally the Human's bleed). Or keep bleed out of the Human entirely?
2. **Lightbringer** (§5.9) — confirm the per-race read, and its **Buff + Passive slots are blank for
   all three races**.
3. **Tempest-Ork Main 2** — "+50% vs rooted" in a fear kit. Should be "vs feared"?
4. **Ravager-Ork / Warlord-Ork Main 1** have no `[Double]` where the Human and Elf do. Deliberate?
5. **Orphaned by the merge** — Phantom-Ork, Venomweaver-Elf (**poison**; already coded as
   `ToxicSting`/`ToxicBurst`), Sharpshooter-Elf, Trapper-Human, Trapper-Ork. Nothing reaches them.
   Under §5 the poison line has no home at all — park it, or give the Elf poison as a race skill?
6. **4th class** — a 1:1 capstone of each 3rd (stronger + 1-2 ultimates, mostly shared across races),
   ~10kk gold + a boss kill. Nothing written. The two level-83 skills already live in that space.

---

## Appendix A — name bank

Original, IP-safe placeholders from the deleted draft. Free to take, rename or ignore.

**Tank** — Aegis Wall · Bonewall · Ironbound · Stoneskin · Unyielding · Wardstance · Glimmer Ward ·
Sheltering Grace · Granite Skin · Thickhide · Bloodbarrier · Evasive Guard · Iron Guard · Mirror Guard ·
Shield Punish · Lightlance · Skullcrush · Warden's Wrath · Frenzied Guard · Battle Resolve · Valor ·
Retribution · Disciplined Edge · Bloodforged

**Warrior** — Sunder · Finisher · Bloodlust · Killer Instinct · Tempo Strike · Lunge · Keen Edge ·
Precision · Maul · Rampage · Berserk · Savagery · Cleave · Warstrike · Rally · Commander · Bladestorm ·
Sweeping Arc · Earthbreaker · Crushing Blow · Bonebreaker · Feral Surge · Brutish Might · Warcry ·
War Drums · Battle Flow

**Rogue** — Backstrike · Throatcut · Gutrip · Fade Cut · Ghostwalk · Nightveil · Vanish · Ambusher ·
Moonblade · Slippery · Coated Blades · Toxic Lash · Creeping Toxin · Plaguebite · Pestilent · Sporecut ·
Virulence · Wither · Feral Venom · Toxin Bloom · Blightstrike · Predator

**Archer** — Aimed Shot · Piercing Shot · Longpierce · Skewer · Starshot · Thornshot · Net Shot ·
Bola Shot · Bear Trap · Snare · Tanglevine · Steady Aim · Hawk Eye · Keen Sight · Hunter's Focus ·
Marksmanship · Brutal Aim · Pathfinder · Trapcraft · Woodcraft · Survivalist · Field Kit · Beast Ward

**Nuker** — Voidbolt · Starfall · Starlance · Eruption · Rimeburst · Arc Cascade · Vortex · Spirit Burn ·
Mana Sear · Soul Rend · Spirit Storm · Overload · Wild Magic · Arcane Focus · Astral Clarity ·
Storm Focus · Stormborn · Tempest Heart · Attunement · Gale · Tailwind

**Healer / buffer** — Mend · Purify · Dawn · Dawnsong · Font · Sap · Renewal · Warden · Totemic ·
Battle Hymn · War Hymn · Warding Hymn · Grace Anthem · Spirit Chant · Windsong · Resonance · Harmony ·
Bloodbeat · Conductor
