# Skills that exist in code but are NOT in your CSVs (playtest-17 B3)

**You asked for this list so you can say which to delete. Nothing has been deleted.**

Your CSVs live in `docs/data/classes_skills_csv/` — 7 files (`fighter 01-15`, `mage 01-15`,
`warrior/tank/rogue/nuker/healer 20-35`). They identify a skill by **NAME only**, so this is a
name diff. There are **no CSVs for level 40+**, so every 3rd-class skill is "outside the CSVs"
by definition and is listed separately at the bottom.

---

## ⚠ Read this before deleting anything

A skill NAME on a class table is often an **override** of a shared definition. Twelve 3rd-class
skills are really five underlying skills wearing different names:

- `power_shot` = **Heavy Draw** (rogue 24) **and** Piercing Shot / Snare Shot / Rending Shot (40)
- `twin_slash` = **Twin Slash** **and** Ambush / Venom Strike / Silencing Cut (40/43)

So **deleting the definition hollows out three disciplines**. The safe operation for the two you
named is to remove the **class-table grant at level 24**, keeping the definition for the 40+ kit.

## 1. The two you named

| you said | actually | where it comes from |
|---|---|---|
| **Heavy Draw** | `power_shot` — confirmed absent from every CSV | granted to **Rogue @24** |
| **Twin Blade** | there is no "Twin Blade" — you mean **Twin Slash** (`twin_slash`) | **already removed** from the level-24 archer table on 2026-07-01; at HEAD nothing below 40 grants it. You were playing an older build. |

**So: one grant to remove (Heavy Draw @24). Twin Slash is already gone below 40.**

## 2. Other learnable skills not in the CSVs

**Base mage** — `cast_def_phys` **Bulwark** (+8% P.Def) @7.

**Healer @20-35** — these are the individual buffs you moved off the Warchanter on 2026-07-31.
Your CSV still lists the OLD group names (Might/Force/Focus/Speed/Body), so they read as
"missing" but they are the intended replacement. **Probably keep all of these:**
Swift, Alacrity, Resolve, Bulwark, Vampirism, Agility, Aim, Haste, Vigor, Ward,
Combat Stance, Antidote, Resurrection, Restore Mana.

**Tank / warrior / nuker / base fighter** — nothing extra. Everything they grant is in a CSV.

## 3. In the catalog but granted to NOBODY (dead weight — safe to delete)

- `evade_mastery`, `reflexes`, `precision`, `anti_magic` — the four "identity floor" passives.
- `class_balance_*` (8) — Class Balance passives.
- `archer_armor_mastery`, `archer_weapon_mastery` — orphaned by the archer→rogue merge.
- `dispel_magic`.
- Lightbringer (8, `lb_*`) and Warchanter per-race (12, `wc_*`) — **see the answer below.**
- `hp_boost`, `greater_heal` — god-only, and the god table is never registered.

### ❓ You asked what `lb_*` and `wc_*` are (playtest-18 G2)

**They are the level-40 HEALER disciplines — a written, finished 3rd-class kit that nobody can learn
yet.** The healer's two branches: **Lightbringer** = the pure healer, **Warchanter** = the buffer.
The definitions are alive and registered in the catalog; what is commented out is one line in
`ClassSkillTables.Third.cs` — `// RegisterLightbringer(); RegisterWarchanter();` — the *learn
assignments*, dropped **pending your level-40 CSVs**. So they are not dead like the four passives are
dead; they are **parked waiting on you.**

| | Human | Elf | Ork | shared |
|---|---|---|---|---|
| **Lightbringer** (8) | Mend @40 (fast strong single heal), Purify @44 (cleanse) | Dawn @40 (AoE heal + cleanse), Warden @44 (root + self de-taunt) | Font @40 (AoE heal), Sap @44 (anti-heal debuff) | Blessing of Light @48 (party +15 % HP/def), Devotion @52 (passive) |
| **Warchanter** (12) | Bolt @40, Chant @44, Renew @48, Passive @52 | same four | same four | — (the mega party chant is per-race, same magnitudes, different names) |

⚠ **The Warchanter's BUFF layer is a separate thing and it IS live** — `RegisterWarchanterBuffs()` runs,
which is where every group buff and Harmony you use today comes from. Deleting `wc_*` does **not** touch
those.

**My recommendation: keep both, delete neither.** They cost nothing (they are unreachable), and they are
most of a 3rd-class healer kit already written — re-authoring them later is more work than uncommenting
one line once your 40+ CSVs exist. If you want them gone anyway, say so and they go with the rest.

## 4. Level 40+ (no CSV exists yet)

- **12 stat-swap passives** (`swap_*`, gold-priced, @40).
- **24 discipline placeholders** @40/43 — the renamed shared skills listed in the warning above.
- The authored 3rd-class kit: `elemental_burst`, `frost_bind`, `entangling_roots`, `glacial_spike`,
  `creeping_frost`, `phase_shift`, `mana_barrier`, `cleaving_strike`, `hamstring`, `war_focus`,
  `terrifying_roar`, `shield_bash`, `provoke`, `aegis`, `last_stand`, `indomitable`, `rupture`,
  `detonate_wounds`, `toxic_sting`, `toxic_burst`, `envenom`, `venom_burst`, `shadowstep`,
  `vanish`, `repelling_shot`, `snare_trap`.
- The **Warchanter** buff table @40-74 + the five improved group buffs.

## 5. Name drift — these ARE in your CSVs, just spelled differently

Defencive Wall→Defensive Wall · Bow Expretise→Bow Expertise · Two Handed Mastery→Two-Hand Mastery ·
Anti magic→Anti-Magic · Taunt→Provoke · Rogue Armor/Weapon Mastery→Armor/Weapon Mastery ·
warrior "Strike"→Smash · rogue "Stab"/"Shot"→Piercing Stab/Precise Shot · healer "Holy Bolt"→
`holy_strike` (per-race name: Holy / Moonlight / Spirit Bolt).

## 6. In your CSV but NOT in the code (the reverse gap)

- healer **"Speed"** (all 4 levels) — `holy_speed` is Warchanter-only now.
- healer **"Body" @35** — she gets Vigor and Ward instead.
