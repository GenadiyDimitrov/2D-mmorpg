# Discipline Skill Sets (3rd class)

Design source for the 12 disciplines (× 3 races = 36 third classes). I (Claude)
turn this into real `SkillDef`s + `RegisterThird` kits, one archetype at a time.

**All names here are ORIGINAL PLACEHOLDERS** — generic fantasy, no trademarked
names from IG or any other game. Owner will rename later; change anything freely.

## Format per race kit (4 skills)
- **Main 1 / Main 2** — active, each tagged `[DMG]`, `[DEF]`, `[HEAL]`, or `[CC]`.
- **Buff** — active, `[self]` or `[party]` (party = "allies in radius" until real groups exist).
- **Passive** — always-on stat line (applies when learned). Concrete numbers given; tune freely.

## Engine-support legend (so we know what's drop-in vs needs a small addition)
- ✅ ready: direct damage, heal, defence/atk buffs & debuffs, move/cast/atk-speed buffs,
  root (CC), max-HP/MP buffs, crit/eva/acc/def stat passives, interrupt.
- 🔧 needs a new effect type (we'll approximate with ✅ for v1, build later):
  damage-over-time (poison/bleed), stealth, damage-absorb shields, reflect/thorns,
  mana-burn, traps as placed objects. Where I tag 🔧 I'll note the v1 stand-in.

---

# TANK

## Bulwark  (idea: defensive wall — near-immortal, minimal damage)

### Human — flavor: stalwart sworn guardian
- Main 1 [DEF]: **Aegis Wall** — raise guard: big temp defence + block chance, taunt nearby (12s).
- Main 2 [DEF]: **Stoneskin** — 🔧 damage-absorb shield (v1: large flat defence buff 8s).
- Buff [self]: **Unyielding** — +20% max HP & +def, 60s.
- Passive: **Ironbound** — +12% max HP, +25 defence.

### Elf — flavor: graceful warden
- Main 1 [DEF]: **Wardstance** — defensive stance: +evasion, 🔧 reflect melee (v1: +def + thorns-as-flat later).
- Main 2 [HEAL/DEF]: **Glimmer Ward** — small self heal + short def buff.
- Buff [party]: **Sheltering Grace** — party +def, 30s.
- Passive: **Evasive Guard** — +10% max HP, +18 evasion.

### Ork — flavor: blood bulwark
- Main 1 [DEF]: **Bonewall** — taunt + heavy flat defence, 12s.
- Main 2 [DEF]: **Thickhide** — damage-reduction buff (% def), 10s.
- Buff [self]: **Bloodbarrier** — +max HP + bonus regen while in combat, 40s.
- Passive: **Granite Skin** — +15% max HP, +8 magic defence.

## Vanguard  (idea: offensive tank — high defence + real damage)

### Human — flavor: martial knight
- Main 1 [DMG]: **Shield Punish** — shield bash, damage + interrupt.
- Main 2 [DEF]: **Iron Guard** — defensive stance with counter-bonus.
- Buff [self]: **Battle Resolve** — +atk & +def, 30s.
- Passive: **Retribution** — +10% pAtk, +18 defence.

### Elf — flavor: light-lancer
- Main 1 [DMG]: **Lightlance** — piercing thrust damage.
- Main 2 [DEF]: **Mirror Guard** — block + 🔧 reflect (v1: block + small def).
- Buff [party]: **Valor** — party +accuracy & +atk, 30s.
- Passive: **Disciplined Edge** — +8% pAtk, +12 evasion.

### Ork — flavor: brutal warden
- Main 1 [DMG]: **Skullcrush** — heavy blunt strike.
- Main 2 [DEF]: **Warden's Wrath** — taunt + def + 🔧 thorns (v1: taunt + def).
- Buff [self]: **Frenzied Guard** — +atk & +max HP, 30s.
- Passive: **Bloodforged** — +10% pAtk, +10% max HP.

---

# WARRIOR

## Ravager  (idea: pure single-target burst, low survivability)

### Human — flavor: disciplined slayer
- Main 1 [DMG]: **Sunder** — big single hit.
- Main 2 [DMG]: **Finisher** — bonus damage vs low-HP targets.
- Buff [self]: **Bloodlust** — +crit rate & +atk speed, 20s.
- Passive: **Killer Instinct** — +12% crit rate.

### Elf — flavor: tempo duelist
- Main 1 [DMG]: **Tempo Strike** — fast hard hit.
- Main 2 [DMG]: **Lunge** — gap-close burst.
- Buff [self]: **Keen Edge** — +crit damage, 20s.
- Passive: **Precision** — +10% pAtk, +10 accuracy.

### Ork — flavor: berserker
- Main 1 [DMG]: **Maul** — brutal heavy blow.
- Main 2 [DMG]: **Rampage** — escalating-damage strike.
- Buff [self]: **Berserk** — +atk, −def, 20s.
- Passive: **Savagery** — +15% pAtk.

## Warlord  (idea: balanced bruiser with AoE)

### Human — flavor: battle commander
- Main 1 [DMG]: **Cleave** — frontal AoE.
- Main 2 [DMG]: **Warstrike** — single hit + small AoE.
- Buff [party]: **Rally** — party +atk, 30s.
- Passive: **Commander** — +8% pAtk, +8% max HP.

### Elf — flavor: whirling blade
- Main 1 [DMG]: **Bladestorm** — spin AoE.
- Main 2 [DMG]: **Sweeping Arc** — cone damage.
- Buff [party]: **War Hymn** — party +atk speed, 30s.
- Passive: **Battle Flow** — +10% attack speed.

### Ork — flavor: earthbreaker
- Main 1 [DMG]: **Earthbreaker** — ground-slam AoE.
- Main 2 [DMG]: **Crushing Blow** — heavy single.
- Buff [self]: **Warcry** — +atk & +max HP, 30s.
- Passive: **Brutish Might** — +10% pAtk, +8% max HP.

---

# ROGUE

## Phantom  (idea: stealth, high evasion, ambush burst)

### Human — flavor: cutthroat
- Main 1 [DMG]: **Backstrike** — high-damage ambush hit.
- Main 2 [DMG]: **Flurry** — fast multi-hit.
- Buff [self]: **Vanish** — 🔧 stealth (v1: big evasion spike + move speed), 8s.
- Passive: **Slippery** — +18 evasion, +5% crit.

### Elf — flavor: moonshadow
- Main 1 [DMG]: **Moonblade** — silent strike.
- Main 2 [DMG]: **Fade Cut** — hit + evasion up.
- Buff [self]: **Nightveil** — +evasion & +crit, 15s.
- Passive: **Ghostwalk** — +22 evasion.

### Ork — flavor: feral ambusher
- Main 1 [DMG]: **Gutrip** — vicious ambush.
- Main 2 [DMG]: **Throatcut** — crit-focused strike.
- Buff [self]: **Predator** — +crit damage & +move speed, 15s.
- Passive: **Ambusher** — +10% crit, +10% crit damage.

## Venomweaver  (idea: DoT stacks then burst)

### Human — flavor: poisoner
- Main 1 [DMG]: **Toxic Lash** — 🔧 poison DoT (v1: hit + def-down debuff).
- Main 2 [DMG]: **Rupture** — burst that 🔧 consumes DoT (v1: bonus single hit).
- Buff [self]: **Coated Blades** — +atk; (later: attacks apply poison), 20s.
- Passive: **Virulence** — +10% pAtk, +10 accuracy.

### Elf — flavor: blightcaller
- Main 1 [DMG]: **Sporecut** — 🔧 nature DoT (v1: hit + slow).
- Main 2 [DMG]: **Wither** — damage + slow (CC).
- Buff [self]: **Toxin Bloom** — +crit, 20s.
- Passive: **Creeping Toxin** — +10% crit.

### Ork — flavor: plaguebearer
- Main 1 [DMG]: **Blightstrike** — 🔧 heavy poison (v1: heavy hit + def-down).
- Main 2 [DMG]: **Plaguebite** — damage + 🔧 spread (v1: small AoE).
- Buff [self]: **Feral Venom** — +atk speed, 20s.
- Passive: **Pestilent** — +12% pAtk.

---

# ARCHER

## Sharpshooter  (idea: long range, high single-target)

### Human — flavor: marksman
- Main 1 [DMG]: **Piercing Shot** — high single-target, armor-pierce.
- Main 2 [DMG]: **Aimed Shot** — slow cast, huge damage.
- Buff [self]: **Steady Aim** — +crit & +range, 20s.
- Passive: **Marksmanship** — +12% crit, +range.

### Elf — flavor: star-archer
- Main 1 [DMG]: **Starshot** — precise shot.
- Main 2 [DMG]: **Longpierce** — bonus damage at distance.
- Buff [self]: **Hawk Eye** — +accuracy & +crit damage, 20s.
- Passive: **Keen Sight** — +10% crit damage, +10 accuracy.

### Ork — flavor: bone-archer
- Main 1 [DMG]: **Bonebreaker Shot** — heavy-impact shot.
- Main 2 [DMG]: **Skewer** — high damage.
- Buff [self]: **Hunter's Focus** — +atk, 20s.
- Passive: **Brutal Aim** — +12% pAtk.

## Trapper  (idea: utility / traps / crowd control)

### Human — flavor: ranger-engineer
- Main 1 [CC]: **Snare** — root the target (Root), 6s.
- Main 2 [DMG]: **Net Shot** — damage + bind (slow).
- Buff [self]: **Field Kit** — +evasion & +regen, 30s.
- Passive: **Trapcraft** — +12 evasion, +8 accuracy.

### Elf — flavor: warden of paths
- Main 1 [CC]: **Tanglevine** — nature root (Root), 6s.
- Main 2 [DMG]: **Thornshot** — damage + slow.
- Buff [party]: **Pathfinder** — party +move speed & +evasion, 30s.
- Passive: **Woodcraft** — +15 evasion.

### Ork — flavor: beast-trapper
- Main 1 [CC]: **Bear Trap** — heavy root + damage (Root + hit).
- Main 2 [DMG]: **Bola Shot** — damage + slow.
- Buff [self]: **Beast Ward** — +def & +atk, 30s.
- Passive: **Survivalist** — +8% max HP, +10 evasion.

---

# HEALER

## Lightbringer  ✅ DONE (Phase 24.1) — reference for the pattern
Real per-race skills already live (on top of the cumulative 2nd-class healer kit):
- Human: **Mend** (strong fast single heal), **Purify** (cleanse an ally).
- Elf: **Dawn** (AoE heal + cleanse), **Warden** (root enemy + self de-taunt).
- Ork: **Font** (AoE heal, totem stand-in), **Sap** (anti-heal debuff).
(Note: Lightbringer currently has 2 authored skills/race; when we standardize to the
4-skill template we can add a buff + passive to match the others.)

## Warchanter  (idea: buffer — stat buffs + heal-over-time, farm-oriented)

### Human — flavor: battle-priest
- Main 1 [HEAL]: **Renewal** — 🔧 heal-over-time on ally (v1: medium instant heal, short cd).
- Main 2 [DEF]: **Warding Hymn** — party damage-reduction (party +def), 20s.
- Buff [party]: **Battle Hymn** — party +atk, 30s.
- Passive: **Resonance** — +max MP, +MP regen.

### Elf — flavor: songweaver
- Main 1 [HEAL]: **Dawnsong** — HoT 🔧 (v1: heal + small over-time stand-in).
- Main 2 [BUFF]: **Grace Anthem** — party +cast & +atk speed, 30s.
- Buff [party]: **Windsong** — party +move speed, 30s.
- Passive: **Harmony** — +max MP, +magic defence.

### Ork — flavor: spirit-chanter
- Main 1 [HEAL]: **Spirit Chant** — HoT 🔧 (v1: heal).
- Main 2 [BUFF]: **War Drums** — party +atk & +max HP, 30s.
- Buff [party]: **Bloodbeat** — party regen, 30s.
- Passive: **Totemic** — +max HP, +MP regen.

---

# NUKER

## Magus  (idea: single-target glass cannon)

### Human — flavor: arcanist
- Main 1 [DMG]: **Annihilate** — big single-target nuke.
- Main 2 [DMG]: **Mana Sear** — damage + 🔧 mana-burn (v1: damage + MP-cost-down debuff later; v1 just damage).
- Buff [self]: **Arcane Focus** — +magic crit & +cast speed, 20s.
- Passive: **Overload** — +12% magic crit.

### Elf — flavor: starcaller
- Main 1 [DMG]: **Starlance** — precise burst.
- Main 2 [DMG]: **Voidbolt** — reduced-resist nuke (treat target mdef lower).
- Buff [self]: **Astral Clarity** — +cast speed & +mAtk, 20s.
- Passive: **Attunement** — +10% atk (feeds mAtk), +magic defence.

### Ork — flavor: spirit-burner
- Main 1 [DMG]: **Spirit Burn** — heavy single hit.
- Main 2 [DMG]: **Soul Rend** — high-power nuke.
- Buff [self]: **Feral Surge** — +mAtk, −magic def, 20s.
- Passive: **Wild Magic** — +15% magic crit damage.

## Tempest  (idea: AoE damage + control)

### Human — flavor: storm-mage
- Main 1 [DMG]: **Arc Cascade** — AoE chained bolt.
- Main 2 [CC]: **Rimeburst** — AoE damage + slow.
- Buff [self]: **Storm Focus** — +cast speed, 20s.
- Passive: **Conductor** — +magic crit, +atk (mAtk).

### Elf — flavor: skyweaver
- Main 1 [DMG]: **Starfall** — AoE rain of light.
- Main 2 [CC]: **Gale** — AoE knock/slow.
- Buff [party]: **Tailwind** — party +cast & +move speed, 30s.
- Passive: **Tempest Heart** — +max MP, +magic crit.

### Ork — flavor: stormborn
- Main 1 [DMG]: **Eruption** — AoE ground burst.
- Main 2 [DMG]: **Vortex** — sustained AoE.
- Buff [self]: **Spirit Storm** — +mAtk, 20s.
- Passive: **Stormborn** — +12% atk (mAtk).
