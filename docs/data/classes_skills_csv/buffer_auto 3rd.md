# `buffer_auto 3rd` — a GENERATED proposal, not an authored file

Built from l2elo (current chronicle) + `buffer 3rd.csv` (what the game registers today) +
`healer 3rd.csv` / `cleric 2nd.csv` / `warrior 2nd.csv` (your format, SP ladder, MP convention, power
scale). Not authoritative. Rename it to `.csv` only if you want it to *become* the file.

---

## The structure

**The buff kit is the CLASS and every race gets all of it** — singles, harmonies, grouped ladder.
**The RACE column is a combat TINT only**: an armour passive, a weapon passive, one flavour passive,
one or two actives, all far below the real class.

| race | class | tint | from |
| ---- | ----- | ---- | ---- |
| **Elf** | Harmonist | archer | Bow Mastery · Longsight · Twin Arrow |
| **Ork** | Bloodchanter | 2H warrior | Two-Hand Grip · Thick Blood · Bloodrage · Ruinous Blow |
| **Human** | Warchanter | off-tank | Shield Mastery · Mace Mastery · Armor Mastery (heavy) · Shield Bash |

| | the real class at 40 | this buffer at 40 | at 74 |
| --- | --- | --- | --- |
| bow P.Atk | Hawkeye Bow Mastery **+105** | **+14** | +58 vs their +795 |
| 2H | your `warrior 2nd` (at 36) ×1.5, +20 P.Atk, crit dmg +106 | **×1.15, +8, +40** | ×1.25, +20, +90 |
| shield | Shillien Knight **+50% shield def, +85% rate** | **+15% / +20%** | +30% / +45% |

---

## Your four rulings, and what each one did to the file

### 1. Procs are an ENGINE, not a skill

> *"The tank have a passive that proc increasing his skill dmg and defence on dmg taken … the buffer
> for atk/cast and rogue — venom on hit stacks … Also archers have a proc on basic attack"*

That is **four consumers of one system**, and none of it exists — nothing in `SkillEffect` fires a
skill from an event. What the four need between them is **three trigger points**:

| trigger | who wants it |
| ------- | ------------ |
| **on damage DEALT** (skill) | buffer — `Quickening`, party Atk/Cast Spd |
| **on damage TAKEN** | tank — +skill damage and +defence |
| **on BASIC ATTACK** | archer — their proc; rogue — venom stacks on hit |

Each needs: a **chance**, an **internal reuse** (your 30s), the **skill it fires**, and a **target**
(self / party / the enemy hit). The rogue's is the interesting one — it applies an *existing* effect
(`Venom`, which already stacks) rather than a new buff, so the system has to be able to fire a
debuff at the victim, not only a buff at yourself.

🔴 **This wants a `BL-nn` of its own.** It is a shared mechanic that four disciplines are now
specified against, and it is much bigger than the one row in this file. Say the word and I will write
the backlog entry.

⚠ `SkillEffect` has **one free bit left** (62 = `BuffReflect`, 63 = sign), so the trigger data goes in
as `StatMods`/fields, per the note in `Enums.cs`.

### 2. Three INDEPENDENT layers that multiply — harmony · single (or its group) · everything else

> *"a formula is base critical x buff1 x buff2 x buff3 … a harmony is 100% (x2), a focus is x1.3 …
> a focus with id focus, the harmony with id harmony_warrior. They will stack and not override."*

Correct, and the code does exactly that. `Entity.cs:2580` —
`CritRateMult *= 1f + buff.Percent(BuffCritRate)` — every buff multiplies in on its own.

`ApplyBuff` derives "family" from **`ChildBuffs`**: a buff with **no children** lands on its own
`BuffKey` and conflicts only with the same key. So a harmony authored as an ordinary buff
(`BuffKey = harmony_warrior`, no children) is invisible to `Focus`, to `Focus and Ferocity`, to
everything. Only `harmony_warrior` L2 replaces `harmony_warrior` L1.

An earlier draft of this file had the harmonies authored as **group** buffs, which is what made them
cover families and evict singles. That was my authoring mistake, not a constraint — the harmonies are
plain buffs here and **your five groups are back exactly as the game has them today**:

| tier | key | overridden by |
| ---- | --- | ------------- |
| single | `focus` | a higher `focus` rung, or `Focus and Ferocity` (which covers it) |
| harmony | `harmony_warrior` | a higher `harmony_warrior` rung, nothing else |
| group | `focus_ferocity` | nothing |

**Harmony × single × group all multiply.** `Harmony of the Warrior` +75% crit rate ×
`Focus and Ferocity` +30% = **×2.275** on the crit-rate stat.

⚠ Worth measuring, not an objection: `StatCaps.PhysicalCritRate` is **0.50** and
`StatCaps.MagicCritRate` is **0.20**. Three multiplying layers reach those ceilings quickly, so run
`BalanceMatrix` before and after rather than reading the percentages off the page.

🔑 **Every harmony ladder ENDS on the exact value the game has today**, at the exact level — Warrior
at 62, Protection at 60, Wizard at 64, all byte-identical to `buffer 3rd.csv`. The ladder is pure
addition *below* those rows; nothing you already authored moves. Each rung folds in one more real
song or dance, per your Song of Hunter example.

⚠ One thing genuinely wrong in today's file, unrelated to any of the above: **`Swift and Sure L6`
grants `Move +33`, which is level 30's Swift**, and **`Might and Bulwark L6` grants `+15% P.Atk`,
which is level 40's Might** — level-66 group buffs worth nothing over their own singles, and a group
locks its families out, so that is a downgrade the player cannot refuse. Swift is raised to L4 +40 /
L5 +45 / L6 +50 and `Swift and Sure` to Move +50 / Cast +35% here.

### 3. `Ruinous Blow` — your rung levels, taken exactly

**43 · 46 · 49 · 52 · 55 · 58 → L1-L6, all below Crush of Doom L1, then L7 @60 matches it.** That is
IG's own 3-level Destroyer cadence, not the 4-level cadence of the rest of this file, so those rows
carry their true learn level while sitting in the nearest section — the same thing `healer 3rd` does.

🔴 **The one number with no authored ground under it: what Crush of Doom L1 *is*, in our scale.**
`warrior 3rd.csv` is empty, so I extended your authored `Smash` ladder (105/143/191/251/326 at
20/24/28/32/36 = ×1.3 per 4 levels) to **1573 at 60** and set the match point at **1600**. Ruinous
Blow climbs 320→1380 across your six rungs, hits 1600 at 60, then grows at half speed so the real
class pulls away again. **Re-anchor the day you author `warrior 3rd`.**

### 4. The wand healing penalty — it already exists, and your fix is one level

The mechanism is `Divine Focus` (`Skills.Common.cs:652`), applied in `Entity.RecomputeDerived:2519`:
with **no magic weapon** equipped, `HealOutputMult` = **Lv1 ×0.5** (clerics/healers) / **Lv2 ×0.75**
(already granted to Warchanters at 40, `GameLoopService.cs:1353`).

✅ So your ruling is **`Divine Focus Lv3` = ×1.0**, and `want` becomes `3` for the buffer discipline.
One new `SkillLevel` and one changed constant. The row is in the file at 40, SP 0, auto-granted like
the other two.

🆕 **Which exposed a gap: the buffer's heal ladder stops at the cleric's level-35 `Heal`, power 301,
forever.** Ruling on the buffer's heal penalty only means something if the buffer has heals worth the
penalty. I added **`Mending Chant`** at ~60% of the healer's `Great Heal` — **invented, not derived**,
and the most likely thing in this file to be wrong.

## 🔑 The bow: your earlier ruling, recorded

You asked on 2026-08-19 for *"a passive % that will negate the bow cast penalty"*; I answered ×2
(`CastSpeedPct: 0.50`) and flagged that the gate fires **three** effects. "×2 cast and something to
negate the fizzle" buys back two:

| bow penalty | bought back |
| --- | --- |
| cast speed ×0.5 | ✅ yes |
| spell FAIL ×25 (`StatCaps.UntrainedWeaponMagicFailMod`) | ✅ yes |
| **M.Atk ×0.5** (`MagicWeaponPenaltyMult`) | 🔴 **not named** — an elf buffer's heals and nukes still run at half power |

⚠ The passive must be **weapon-conditional**. A flat `CastSpeedPct` would double a *staff* buffer's
casting too and make the buffer the fastest caster in the game. `CastSpeedPenaltyMult` is set in one
place, so "clear it for a bow" is a two-line rule.

## Smaller things I decided

- **Physical actives pay all MP up front** (`INIT MP` full, `FINIT MP` 0) — your `warrior 2nd`
  convention, not the healer's two-stage.
- **SP** is `healer 3rd`'s exact ladder (36k/43k/64k/74k/81k/88k/120k/170k/190k/280k/320k/390k/650k/880k
  for actives and passives, half for buffs) so both files sit in one economy.
- **No weapon is class-restricted anywhere in the code** — there is no `AllowedWeapons` of any kind.
  These three passives are the only thing that makes a buffer's weapon choice mean anything.
- **Bloodrage is ungated** where your `warrior 2nd` `Battle Presence` needs HP ≤ 60%. Weaker (×1.2 vs
  ×1.35) but always available — a support class cannot plan around being nearly dead.
- **`--check` never compares POWER.** Every DESCR number below is in the half nobody verifies.

---

```csv
LEARN @ LVL, NAME,TYPE,RANGE,TARGET, CAST s,CD s,DURRATION s, DESCR, INIT MP,FINIT MP ,SP COST,REPLACES,RACE,COMMENT
,,,,,,,,,,,,,,--------------------------------40--------------------------------
40,Divine Focus L3,Passive,0,self,0,0,0,The non-magic-weapon healing penalty is gone entirely (x1.0). Buffers heal at full power in fighter gear.,0,0,0,[],,🔑 YOUR ruling — today the buffer sits on Lv2 (x0.75)
40,Anti magic,Passive,0,self,0,0,0,"magic def +40, mRes +12%",0,0,36k,[],,a shade under the healer's
40,Chant Mastery,Passive,0,self,0,0,0,"with sword/blunt decreases the reuse delay with 10%, mAtk +18, pAtk +14, mpReg x1.3, cast speed x1.05",0,0,36k,[Spell Mastery],
40,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +36, maxMP +60; Light: mpReg x1.2, pDef +36, evasion +3, −10% critical damage taken",0,0,36k,[],Elf
40,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +36, maxMP +60; Heavy: pDef +42, maxHP +4%, hpReg x1.2",0,0,36k,[],Ork
40,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +36, maxMP +60; Heavy: pDef +46, −10% critical damage taken, shield def +10%",0,0,36k,[],Human
40,Bow Mastery,Passive,0,self,0,0,0,"with a bow: pAtk +14, accuracy +1, cast speed x2 and spells no longer suffer the untrained-weapon failure penalty. M.Atk is still halved.",0,0,36k,[],Elf,🔑 buys back 2 of the bow's 3 caster penalties
40,Longsight,Passive,0,self,0,0,0,with a bow: basic-attack range +150.,0,0,36k,[],Elf,IG Long Shot (+400) at a third of it
40,Two-Hand Grip,Passive,0,self,0,0,0,"with 2h sword/blunt: pAtk x1.15, pAtk +8, accuracy +2, crit damage +40, evasion −2",0,0,36k,[],Ork
40,Thick Blood,Passive,0,self,0,0,0,"maxHP +150, hpReg x1.2",0,0,36k,[],Ork
40,Shield Mastery,Passive,0,self,0,0,0,"shield defence +15%, block chance +20%. With heavy armor additionally pDef +5%.",0,0,36k,[],Human
40,Mace Mastery,Passive,0,self,0,0,0,"with a ONE-HANDED blunt: pAtk +11, accuracy +1",0,0,36k,[],Human,1h blunt only — your call
40,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 200 power total - requires a bow,30,0,36k,[],Elf
40,Bloodrage,physical buff,0,self,0.5,300,90,"pAtk x1.2, accuracy +2, evasion −3, pDef x0.85 - requires 2h blunt/sword",25,0,36k,[],Ork
40,Shield Bash,physical active,40,enemy,1,6,0,dmg with 150 power and provokes the target - requires a shield,25,0,36k,[],Human
40,Resonant Bolt,Magic/Dmg,750,enemy,3,1,0,m.Atk +36,8,28,36k,[Holy Bolt],,the buffer has NO attack line above 40 today
40,Mending Chant,Magic/Heal,600,self/target,5,2,0,heal with power 240,10,40,36k,[Heal],,🆕 INVENTED — ~60% of the healer's Great Heal; the buffer's heal is frozen at lvl-35 Heal today
40,Might L3,Magic/Buff,600,self/target,1,1,1200,+15% P.Atk.,12,48,19k,[],
40,Bulwark L3,Magic/Buff,600,self/target,1,1,1200,+15% P.Def.,12,48,19k,[],
40,Haste L2,Magic/Buff,600,self/target,1,1,1200,+23% Attack Speed.,12,48,19k,[],
40,Agility L3,Magic/Buff,600,self/target,1,1,1200,+4 Evasion.,12,48,19k,[],
40,Alacrity L3,Magic/Buff,600,self/target,1,1,1200,+30% Cast Speed.,12,48,19k,[],
40,Aim L2,Magic/Buff,600,self/target,1,1,1200,+3 Accuracy.,12,48,19k,[],
40,Force L3,Magic/Buff,600,self/target,1,1,1200,+32% M.Atk.,12,48,19k,[],
40,Ward L2,Magic/Buff,600,self/target,1,1,1200,+20% M.Def.,12,48,19k,[],
40,Vampirism L3,Magic/Buff,600,self/target,1,1,1200,+9% melee vampirism.,12,48,19k,[],
40,Clarity L3,Magic/Buff,600,self/target,1,1,1200,40% resist to SPT debuffs,12,48,19k,[],
40,Shield Bless L2,Magic/Buff,600,self/target,1,1,1200,Increases Shield Rate with 8%,12,48,19k,[],
40,Harmony of the Warrior L1,Magic/Buff,600,self/party,1.5,1,1200,+25% critical rate.,40,160,36k,[],,IG Song of Hunter — your example. Own key: stacks with Focus
44,Harmony of the Warrior L2,Magic/Buff,600,self/party,1.5,1,1200,"+40% critical rate, +15% critical damage.",40,160,43k,[],,IG Dance of Fire folded in
43,Ruinous Blow L1,physical active,40,enemy,1.5,7,0,dmg with 320 power - ignores shield defence - requires 2h blunt/sword,32,0,36k,[],Ork,YOUR rung cadence 43/46/49/52/55/58
,,,,,,,,,,,,,,--------------------------------44--------------------------------
44,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 270 power total - requires a bow,33,0,43k,[],Elf
44,Shield Bash,physical active,40,enemy,1,6,0,dmg with 200 power and provokes the target - requires a shield,28,0,43k,[],Human
44,Resonant Bolt,Magic/Dmg,750,enemy,3,1,0,m.Atk +40,9,30,43k,[Holy Bolt],
44,Haste L3,Magic/Buff,600,self/target,1,1,1200,+33% Attack Speed.,13,52,22k,[],
44,Aim L3,Magic/Buff,600,self/target,1,1,1200,+4 Accuracy.,13,52,22k,[],
44,Ward L3,Magic/Buff,600,self/target,1,1,1200,+30% M.Def.,13,52,22k,[],
44,Resolve L3,Magic/Buff,600,self/target,1,1,1200,+40 interrupt resistance.,13,52,22k,[],
44,Swift L4,Magic/Buff,600,self/target,1,1,1200,+40 Move Speed.,13,52,22k,[],,raised from L3 +33
44,Fortitude L3,Magic/Buff,600,self/target,1,1,1200,25% resist to CON debuffs,13,52,22k,[],
44,Harmony of Protection L1,Magic/Buff,600,self/party,1.5,1,1200,+15% M.Def.,40,160,43k,[],,IG Song of Warding
46,Ruinous Blow L2,physical active,40,enemy,1.5,7,0,dmg with 450 power - ignores shield defence - requires 2h blunt/sword,35,0,43k,[],Ork
,,,,,,,,,,,,,,--------------------------------48--------------------------------
48,Anti magic,Passive,0,self,0,0,0,"magic def +52, mRes +15%",0,0,64k,[],
48,Chant Mastery,Passive,0,self,0,0,0,"with sword/blunt decreases the reuse delay with 15%, mAtk +34, pAtk +28, mpReg x1.7, cast speed x1.07",0,0,64k,[Spell Mastery],
48,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +48, maxMP +90; Light: mpReg x1.2, pDef +48, evasion +3, −12% critical damage taken",0,0,64k,[],Elf
48,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +48, maxMP +90; Heavy: pDef +55, maxHP +5%, hpReg x1.2",0,0,64k,[],Ork
48,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +48, maxMP +90; Heavy: pDef +60, −12% critical damage taken, shield def +14%",0,0,64k,[],Human
48,Bow Mastery,Passive,0,self,0,0,0,"with a bow: pAtk +22, accuracy +1, cast speed x2 and spells no longer suffer the untrained-weapon failure penalty. M.Atk is still halved.",0,0,64k,[],Elf
48,Two-Hand Grip,Passive,0,self,0,0,0,"with 2h sword/blunt: pAtk x1.18, pAtk +11, accuracy +2, crit damage +52, evasion −2",0,0,64k,[],Ork
48,Thick Blood,Passive,0,self,0,0,0,"maxHP +220, hpReg x1.3",0,0,64k,[],Ork
48,Shield Mastery,Passive,0,self,0,0,0,"shield defence +19%, block chance +25%. With heavy armor additionally pDef +6%.",0,0,64k,[],Human
48,Mace Mastery,Passive,0,self,0,0,0,"with a ONE-HANDED blunt: pAtk +16, accuracy +1",0,0,64k,[],Human
48,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 360 power total - requires a bow,36,0,64k,[],Elf
48,Shield Bash,physical active,40,enemy,1,6,0,dmg with 265 power and provokes the target - requires a shield,31,0,64k,[],Human
48,Resonant Bolt,Magic/Dmg,750,enemy,3,1,0,m.Atk +44,10,32,64k,[Holy Bolt],
48,Mending Chant,Magic/Heal,600,self/target,5,2,0,heal with power 310,12,46,64k,[Heal],
48,Focus L5,Magic/Buff,600,self/target,1,1,1200,+25% critical rate.,14,56,32k,[],
48,Ferocity L3,Magic/Buff,600,self/target,1,1,1200,+20% critical damage.,14,56,32k,[],
48,Body L3,Magic/Buff,600,self/target,1,1,1200,+20% Max HP.,14,56,32k,[],
48,Soul L3,Magic/Buff,600,self/target,1,1,1200,+20% Max MP.,14,56,32k,[],
48,Harmony of the Warrior L3,Magic/Buff,600,self/party,1.5,1,1200,"+52% critical rate, +22% critical damage, +2 Accuracy.",40,160,64k,[],,IG Dance of Inspiration folded in
48,Harmony of Protection L2,Magic/Buff,600,self/party,1.5,1,1200,"+18% M.Def, +10% HP regeneration.",40,160,64k,[],,IG Song of Life folded in
49,Ruinous Blow L3,physical active,40,enemy,1.5,7,0,dmg with 620 power - ignores shield defence - requires 2h blunt/sword,39,0,64k,[],Ork
,,,,,,,,,,,,,,--------------------------------52--------------------------------
52,Ruinous Blow L4,physical active,40,enemy,1.5,7,0,dmg with 830 power - ignores shield defence - requires 2h blunt/sword,43,0,74k,[],Ork
52,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 480 power total - requires a bow,39,0,74k,[],Elf
52,Shield Bash,physical active,40,enemy,1,6,0,dmg with 350 power and provokes the target - requires a shield,34,0,74k,[],Human
52,Bloodrage,physical buff,0,self,0.5,300,90,"pAtk x1.22, accuracy +3, evasion −3, pDef x0.85 - requires 2h blunt/sword",30,0,74k,[],Ork
52,Resonant Bolt,Magic/Dmg,750,enemy,3,1,0,m.Atk +48,11,34,74k,[Holy Bolt],
52,Insight L2,Magic/Buff,600,self/target,1,1,1200,+35% magic critical rate.,15,60,38k,[],
52,Resolve L4,Magic/Buff,600,self/target,1,1,1200,+60 interrupt resistance.,15,60,38k,[],
52,Vigor L4,Magic/Buff,600,self/target,1,1,1200,+15% HP regeneration.,15,60,38k,[],
52,Serenity L4,Magic/Buff,600,self/target,1,1,1200,+15% MP regeneration.,15,60,38k,[],
52,Frenzy L3,Magic/Buff,600,self/target,1,1,1200,"−22% Max HP/MP, +6% offence and speed, +6 move, −8 evasion.",29,116,38k,[],,no group covers Frenzy — it conflicts with everything by design
52,Harmony of Protection L3,Magic/Buff,600,self/party,1.5,1,1200,"+20% M.Def, +14% HP regeneration, +15% P.Def.",40,160,74k,[],,IG Song of Earth folded in
52,Harmony of the Wizard L1,Magic/Buff,600,self/party,1.5,1,1200,+12% Cast Speed.,40,160,74k,[],,IG Dance of Concentration. Own key: stacks with Alacrity
55,Ruinous Blow L5,physical active,40,enemy,1.5,7,0,dmg with 1080 power - ignores shield defence - requires 2h blunt/sword,45,0,74k,[],Ork
,,,,,,,,,,,,,,--------------------------------56--------------------------------
56,Anti magic,Passive,0,self,0,0,0,"magic def +66, mRes +18%",0,0,81k,[],
56,Chant Mastery,Passive,0,self,0,0,0,"with sword/blunt decreases the reuse delay with 20%, mAtk +56, pAtk +46, mpReg x2.2, cast speed x1.09",0,0,81k,[Spell Mastery],
56,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +60, maxMP +120; Light: mpReg x1.2, pDef +60, evasion +4, −14% critical damage taken",0,0,81k,[],Elf
56,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +60, maxMP +120; Heavy: pDef +68, maxHP +6%, hpReg x1.3",0,0,81k,[],Ork
56,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +60, maxMP +120; Heavy: pDef +74, −14% critical damage taken, shield def +18%",0,0,81k,[],Human
56,Bow Mastery,Passive,0,self,0,0,0,"with a bow: pAtk +32, accuracy +2, cast speed x2 and spells no longer suffer the untrained-weapon failure penalty. M.Atk is still halved.",0,0,81k,[],Elf
56,Two-Hand Grip,Passive,0,self,0,0,0,"with 2h sword/blunt: pAtk x1.20, pAtk +14, accuracy +2, crit damage +64, evasion −2",0,0,81k,[],Ork
56,Thick Blood,Passive,0,self,0,0,0,"maxHP +300, hpReg x1.4",0,0,81k,[],Ork
56,Shield Mastery,Passive,0,self,0,0,0,"shield defence +23%, block chance +31%. With heavy armor additionally pDef +7%.",0,0,81k,[],Human
56,Mace Mastery,Passive,0,self,0,0,0,"with a ONE-HANDED blunt: pAtk +22, accuracy +2",0,0,81k,[],Human
56,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 630 power total - requires a bow,42,0,81k,[],Elf
56,Shield Bash,physical active,40,enemy,1,6,0,dmg with 460 power and provokes the target - requires a shield,37,0,81k,[],Human
56,Resonant Bolt,Magic/Dmg,750,enemy,3,1,0,m.Atk +52,12,36,81k,[Holy Bolt],
56,Mending Chant,Magic/Heal,600,self/target,5,2,0,heal with power 450,15,55,81k,[Heal],
56,Ferocity L6,Magic/Buff,600,self/target,1,1,1200,+35% critical damage.,16,64,42k,[],
56,Focus L6,Magic/Buff,600,self/target,1,1,1200,+30% critical rate.,16,64,42k,[],
56,Insight L4,Magic/Buff,600,self/target,1,1,1200,+65% magic critical rate.,16,64,42k,[],
56,Alacrity L4,Magic/Buff,600,self/target,1,1,1200,+35% Cast Speed.,16,64,42k,[],
56,Harmony of the Warrior L4,Magic/Buff,600,self/party,1.5,1,1200,"+62% critical rate, +28% critical damage, +3 Accuracy, +8% P.Atk.",40,160,81k,[],,IG Dance of the Warrior folded in
56,Harmony of Protection L4,Magic/Buff,600,self/party,1.5,1,1200,"+22% M.Def, +17% HP regeneration, +20% P.Def, +2 Evasion.",40,160,81k,[],,IG Song of Water folded in
56,Harmony of the Wizard L2,Magic/Buff,600,self/party,1.5,1,1200,"+18% Cast Speed, +4% M.Atk.",40,160,81k,[],,IG Dance of the Mystic folded in
,,,,,,,,,,,,,,--------------------------------58--------------------------------
58,Ruinous Blow L6,physical active,40,enemy,1.5,7,0,dmg with 1380 power - ignores shield defence - requires 2h blunt/sword,49,0,88k,[],Ork,last rung below Crush of Doom L1
58,Great Might,Magic/Buff,600,self/target,1,1,1200,+7% P.Atk. Does not stack with other Great Might|Bulwark effects.,17,68,45k,[],,its own family — stacks on top of Might
58,Great Bulwark,Magic/Buff,600,self/target,1,1,1200,+10% P.Def. Does not stack with other Great Might|Bulwark effects.,17,68,45k,[],
58,Great Group Might,Magic/Buff,600,self/party,1,1,1200,+5% P.Atk. Does not stack with other Great Might|Bulwark effects.,34,136,88k,[],
58,Great Group Bulwark,Magic/Buff,600,self/party,1,1,1200,+5% P.Def. Does not stack with other Great Might|Bulwark effects.,34,136,88k,[],
58,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 720 power total - requires a bow,44,0,88k,[],Elf
58,Shield Bash,physical active,40,enemy,1,6,0,dmg with 525 power and provokes the target - requires a shield,39,0,88k,[],Human
58,Harmony of the Warrior L5,Magic/Buff,600,self/party,1.5,1,1200,"+70% critical rate, +32% critical damage, +4 Accuracy, +10% P.Atk, +12% Attack Speed.",40,160,88k,[],,IG Dance of Fury folded in — your example
58,Harmony of Protection L5,Magic/Buff,600,self/party,1.5,1,1200,"+24% M.Def, +19% HP regeneration, +23% P.Def, +3 Evasion, +20% Max HP.",40,160,88k,[],,IG Song of Vitality folded in
58,Harmony of the Wizard L3,Magic/Buff,600,self/party,1.5,1,1200,"+22% Cast Speed, +6% M.Atk, +10% MP regeneration.",40,160,88k,[],
,,,,,,,,,,,,,,--------------------------------60--------------------------------
60,Quickening,Passive,0,self,0,0,30,"When you deal damage, 3% chance to give the whole party +15% Attack Speed and +15% Cast Speed for 30s. 30s internal reuse.",0,0,120k,[],,"🔴 YOUR proc — needs the trigger ENGINE, see the header"
60,Ruinous Blow L7,physical active,40,enemy,1.5,7,0,dmg with 1600 power - ignores shield defence - requires 2h blunt/sword,52,0,120k,[],Ork,🔑 the match point — equals Crush of Doom L1; it falls behind again from here
60,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 820 power total - requires a bow,47,0,120k,[],Elf
60,Shield Bash,physical active,40,enemy,1,6,0,dmg with 600 power and provokes the target - requires a shield,42,0,120k,[],Human
60,Resonant Bolt,Magic/Dmg,750,enemy,3,1,0,m.Atk +58,13,40,120k,[Holy Bolt],
60,Insight L6,Magic/Buff,600,self/target,1,1,1200,+100% magic critical rate.,18,72,61k,[],
60,Body L5,Magic/Buff,600,self/target,1,1,1200,+30% Max HP.,18,72,61k,[],
60,Soul L5,Magic/Buff,600,self/target,1,1,1200,+30% Max MP.,18,72,61k,[],
60,Harmony of Protection L6,Magic/Buff,600,self/party,1.5,1,1200,"+25% P.Def & M.Def, +30% Max HP, +20% HP regen, +3 evasion, reflects 20% of melee damage (20 minutes).",40,160,120k,[],,"🔑 UNCHANGED — this is today's authored row, at today's level. The ladder above it is pure addition."
,,,,,,,,,,,,,,--------------------------------62--------------------------------
62,Bloodrage,physical buff,0,self,0.5,300,90,"pAtk x1.25, accuracy +3, evasion −3, pDef x0.85 - requires 2h blunt/sword",34,0,170k,[],Ork
62,Ruinous Blow L8,physical active,40,enemy,1.5,7,0,dmg with 1680 power - ignores shield defence - requires 2h blunt/sword,54,0,170k,[],Ork
62,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 920 power total - requires a bow,49,0,170k,[],Elf
62,Shield Bash,physical active,40,enemy,1,6,0,dmg with 680 power and provokes the target - requires a shield,44,0,170k,[],Human
62,Vigor L6,Magic/Buff,600,self/target,1,1,1200,+20% HP regeneration.,19,76,86k,[],
62,Serenity L6,Magic/Buff,600,self/target,1,1,1200,+20% MP regeneration.,19,76,86k,[],
62,Frenzy L6,Magic/Buff,600,self/target,1,1,1200,"−10% Max HP/MP, +8% offence and speed, +8 move, −8 evasion.",35,140,86k,[],
62,Harmony of the Warrior L6,Magic/Buff,600,self/party,1.5,1,1200,"+12% P.Atk, +15% atk speed, +35% crit damage, +75% crit rate, +4 acc, 8% vamp, −20% physical-skill MP cost (20 minutes).",40,160,170k,[],,🔑 UNCHANGED — today's authored row at today's level
62,Harmony of the Wizard L4,Magic/Buff,600,self/party,1.5,1,1200,"+26% Cast Speed, +8% M.Atk, +15% MP regeneration.",40,160,170k,[],
,,,,,,,,,,,,,,--------------------------------64--------------------------------
64,Anti magic,Passive,0,self,0,0,0,"magic def +80, mRes +20%",0,0,190k,[],
64,Chant Mastery,Passive,0,self,0,0,0,"with sword/blunt decreases the reuse delay with 20%, mAtk +72, pAtk +58, mpReg x2.6, cast speed x1.10",0,0,190k,[Spell Mastery],
64,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +72, maxMP +150; Light: mpReg x1.2, pDef +72, evasion +4, −16% critical damage taken",0,0,190k,[],Elf
64,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +72, maxMP +150; Heavy: pDef +82, maxHP +7%, hpReg x1.4",0,0,190k,[],Ork
64,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +72, maxMP +150; Heavy: pDef +88, −16% critical damage taken, shield def +22%",0,0,190k,[],Human
64,Bow Mastery,Passive,0,self,0,0,0,"with a bow: pAtk +43, accuracy +2, cast speed x2 and spells no longer suffer the untrained-weapon failure penalty. M.Atk is still halved.",0,0,190k,[],Elf
64,Two-Hand Grip,Passive,0,self,0,0,0,"with 2h sword/blunt: pAtk x1.22, pAtk +17, accuracy +3, crit damage +76, evasion −2",0,0,190k,[],Ork
64,Thick Blood,Passive,0,self,0,0,0,"maxHP +380, hpReg x1.5",0,0,190k,[],Ork
64,Shield Mastery,Passive,0,self,0,0,0,"shield defence +26%, block chance +37%. With heavy armor additionally pDef +8%.",0,0,190k,[],Human
64,Mace Mastery,Passive,0,self,0,0,0,"with a ONE-HANDED blunt: pAtk +28, accuracy +2",0,0,190k,[],Human
64,Ruinous Blow L9,physical active,40,enemy,1.5,7,0,dmg with 1760 power - ignores shield defence - requires 2h blunt/sword,57,0,190k,[],Ork
64,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 1030 power total - requires a bow,52,0,190k,[],Elf
64,Shield Bash,physical active,40,enemy,1,6,0,dmg with 760 power and provokes the target - requires a shield,46,0,190k,[],Human
64,Resonant Bolt,Magic/Dmg,750,enemy,3,1,0,m.Atk +64,14,44,190k,[Holy Bolt],
64,Mending Chant,Magic/Heal,600,self/target,5,2,0,heal with power 580,18,64,190k,[Heal],
64,Body L6,Magic/Buff,600,self/target,1,1,1200,+35% Max HP.,20,80,100k,[],
64,Soul L6,Magic/Buff,600,self/target,1,1,1200,+35% Max MP.,20,80,100k,[],
64,Swift L5,Magic/Buff,600,self/target,1,1,1200,+45 Move Speed.,20,80,100k,[],
64,Harmony of the Wizard L5,Magic/Buff,600,self/party,1.5,1,1200,"+30% cast speed, +10% M.Atk, +20% MP regen, −30% magic-skill MP cost (20 minutes).",40,160,190k,[],,🔑 UNCHANGED — today's authored row at today's level
,,,,,,,,,,,,,,--------------------------------66--------------------------------
66,Swift L6,Magic/Buff,600,self/target,1,1,1200,+50 Move Speed.,21,84,145k,[],
66,Alacrity L6,Magic/Buff,600,self/target,1,1,1200,+35% Cast Speed.,21,84,145k,[],
66,Shield Bless and Harden,Magic/Buff,600,self/party,1,1,1200,"Increases Shield PDef with 50%, and shield chance with 30%",40,160,280k,[Shield Bless],,GROUPED — today's row
66,Swift and Sure L6,Magic/Buff,600,self/party,1,1,1200,"Move +50, Cast +35%, Evasion +4, Attack Speed +33%.",40,160,280k,[Swift Alacrity Agility Haste],,"GROUPED — Move raised 33→50 and Cast 30→35%, so it beats its own singles"
66,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 1130 power total - requires a bow,54,0,280k,[],Elf
66,Ruinous Blow L10,physical active,40,enemy,1.5,7,0,dmg with 1840 power - ignores shield defence - requires 2h blunt/sword,59,0,280k,[],Ork
66,Shield Bash,physical active,40,enemy,1,6,0,dmg with 840 power and provokes the target - requires a shield,48,0,280k,[],Human
,,,,,,,,,,,,,,--------------------------------68--------------------------------
68,Might and Bulwark L6,Magic/Buff,600,self/party,1,1,1200,"+15% P.Atk, +15% P.Def, 9% melee vampirism, +4 Accuracy.",40,160,320k,[Might Bulwark Vampirism Aim],,GROUPED — today's row
68,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 1270 power total - requires a bow,57,0,320k,[],Elf
68,Ruinous Blow L11,physical active,40,enemy,1.5,7,0,dmg with 1920 power - ignores shield defence - requires 2h blunt/sword,62,0,320k,[],Ork
68,Shield Bash,physical active,40,enemy,1,6,0,dmg with 940 power and provokes the target - requires a shield,50,0,320k,[],Human
,,,,,,,,,,,,,,--------------------------------70--------------------------------
70,Force and Ward L6,Magic/Buff,600,self/party,1,1,1200,"+60 interrupt resistance, +32% M.Atk, +30% M.Def.",40,160,390k,[Force Ward Resolve],,GROUPED — today's row
70,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 1400 power total - requires a bow,59,0,390k,[],Elf
70,Ruinous Blow L12,physical active,40,enemy,1.5,7,0,dmg with 2000 power - ignores shield defence - requires 2h blunt/sword,64,0,390k,[],Ork
70,Shield Bash,physical active,40,enemy,1,6,0,dmg with 1040 power and provokes the target - requires a shield,52,0,390k,[],Human
,,,,,,,,,,,,,,--------------------------------72--------------------------------
72,Anti magic,Passive,0,self,0,0,0,"magic def +96, mRes +22%",0,0,650k,[],
72,Chant Mastery,Passive,0,self,0,0,0,"with sword/blunt decreases the reuse delay with 20%, mAtk +92, pAtk +74, mpReg x3.0, cast speed x1.12",0,0,650k,[Spell Mastery],
72,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +86, maxMP +180; Light: mpReg x1.2, pDef +86, evasion +5, −18% critical damage taken",0,0,650k,[],Elf
72,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +86, maxMP +180; Heavy: pDef +98, maxHP +8%, hpReg x1.5",0,0,650k,[],Ork
72,Armor Mastery,Passive,0,self,0,0,0,"Robe: mpReg x1.2, pDef +86, maxMP +180; Heavy: pDef +105, −18% critical damage taken, shield def +26%",0,0,650k,[],Human
72,Bow Mastery,Passive,0,self,0,0,0,"with a bow: pAtk +58, accuracy +3, cast speed x2 and spells no longer suffer the untrained-weapon failure penalty. M.Atk is still halved.",0,0,650k,[],Elf
72,Longsight,Passive,0,self,0,0,0,with a bow: basic-attack range +250.,0,0,650k,[],Elf
72,Two-Hand Grip,Passive,0,self,0,0,0,"with 2h sword/blunt: pAtk x1.25, pAtk +20, accuracy +3, crit damage +90, evasion −2",0,0,650k,[],Ork
72,Thick Blood,Passive,0,self,0,0,0,"maxHP +460, hpReg x1.6",0,0,650k,[],Ork
72,Shield Mastery,Passive,0,self,0,0,0,"shield defence +30%, block chance +45%. With heavy armor additionally pDef +10%.",0,0,650k,[],Human
72,Mace Mastery,Passive,0,self,0,0,0,"with a ONE-HANDED blunt: pAtk +35, accuracy +3",0,0,650k,[],Human
72,Quickening,Passive,0,self,0,0,30,"When you deal damage, 4% chance to give the whole party +18% Attack Speed and +18% Cast Speed for 30s. 30s internal reuse.",0,0,650k,[],
72,Bloodrage,physical buff,0,self,0.5,300,90,"pAtk x1.28, accuracy +4, evasion −3, pDef x0.85 - requires 2h blunt/sword",38,0,650k,[],Ork
72,Mending Chant,Magic/Heal,600,self/target,5,2,0,heal with power 700,21,72,650k,[Heal],
72,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 1530 power total - requires a bow,62,0,650k,[],Elf
72,Ruinous Blow L13,physical active,40,enemy,1.5,7,0,dmg with 2080 power - ignores shield defence - requires 2h blunt/sword,67,0,650k,[],Ork
72,Shield Bash,physical active,40,enemy,1,6,0,dmg with 1140 power and provokes the target - requires a shield,55,0,650k,[],Human
72,Focus and Ferocity L6,Magic/Buff,600,self/party,1,1,1200,"+30% critical rate, +35% critical damage, double magic critical rate.",40,160,650k,[Focus Ferocity Insight],,GROUPED — today's row
,,,,,,,,,,,,,,--------------------------------74--------------------------------
74,Body and Soul L6,Magic/Buff,600,self/party,1,1,1200,"+20% HP and MP regeneration, +35% Max HP and Max MP.",40,160,880k,[Body Soul Vigor Serenity],,GROUPED — today's row
74,Twin Arrow,physical active,bow range,enemy,2,3,0,fires two arrows for 1670 power total - requires a bow,64,0,880k,[],Elf
74,Ruinous Blow L14,physical active,40,enemy,1.5,7,0,dmg with 2120 power - ignores shield defence - requires 2h blunt/sword,69,0,880k,[],Ork
74,Shield Bash,physical active,40,enemy,1,6,0,dmg with 1240 power and provokes the target - requires a shield,57,0,880k,[],Human
```

---

## The names

Every tint skill is renamed off its IG source per the standing rule; the IG name sits in the COMMENT
column where it helps. `Bow Mastery` · `Shield Mastery` · `Armor Mastery` · `Mace Mastery` are kept
because they are generic compounds and already your convention (`Weapon Mastery`, `Anti magic`).

| mine | IG source |
| ---- | --------- |
| Longsight | Long Shot |
| Twin Arrow | Double Shot |
| Two-Hand Grip | Two-handed Weapon Mastery |
| Thick Blood | Boost HP |
| Bloodrage | Rage |
| Ruinous Blow | Crush of Doom |
| Shield Bash | Shield Strike |
| Quickening · Mending Chant · Resonant Bolt · Chant Mastery | ours |
