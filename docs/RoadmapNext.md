# Roadmap — compact view (what's left, what depends on what)

A one-screen digest of [Roadmap.md](Roadmap.md). Updated 2026-07-23 (0.28.55). For the full history
see [CHANGELOG.md](CHANGELOG.md).

## Just built (this session, 0.28.42 → 0.28.55)
Playtest-10 fixes · flat HoT potions (+ instant) · auto-potions Potions tab · potions on the bar ·
equipment presets A/B/C · Hollow Crypt dungeon · regions stage 2 (towns as polygons, entry banners,
non-overlapping) · sit/stand · **leaderboards** · **3h break reminder** · **equipment folded into the
bag** · **all target commands as buttons**.

## Independent — buildable any time, no blockers
- **MP potions** — a parallel set of flat mana-over-time tiers, same shape as the HP potions. Small.
  (Owner flagged as next-after-3rd-class, but it depends on nothing, so it can go now.)
- **Soul/spirit-shots** — consumable that grants a per-hit (or timed) damage boost. Design notes in
  Roadmap.md. Independent.
- **Wearable titles** — show the leaderboard title over the head / by the name. Extends the
  leaderboards that just shipped; the reward layer the owner hinted at ("titles").
- **Combat depth**: perfect/excellent block, magic-resist passives, position bonuses. Each independent.

## Dependent chains (blocked or gated)
- **3rd / 4th class kits** — 🔴 BLOCKED on the owner's skill CSVs (Lightbringer / Warchanter …).
  Everything class-progression waits here. Biggest single content unlock once the CSVs arrive.
- **Instances** — design done ([design/Instances.md]); owner is HOLDING. One open decision: daily
  attempts GLOBAL vs PER-INSTANCE. No technical blocker; needs that call.
- **Castles + vault** — needs siege design; consumes the reserved `VendorBuyTaxRate` hook.
- **Boss-point reward shop** — rare potions / premium consumables bought with boss/event points.
  Depends on bosses + instances producing the points first.
- **Crafting economy** — already BUILT; remaining polish (Epic recipes, mat sinks) is incremental.

## Deferred (explicit owner hold)
- **Bot-prevention CAPTCHA** — petrification after a random 200–500 back-to-back manual kills (over
  ~1h of nonstop farming + town refills), mob-immune while frozen, tap-to-answer challenge. See
  `reminder-bot-prevention-idea`. Note: a CAPTCHA only stops low-effort scripts; **behavioural
  detection** is the real net against an AI that plays. Do the petrification + tap-CAPTCHA first
  (small, self-contained); analytics later.
- **3rd-class CSVs, Instances** — owner said "Hold" (2026-07-22).

## My view of what's next
1. **MP potions** — cheap, completes the potion set, unblocks mana-heavy classes. Independent, do now.
2. **Wearable titles** — small; gives the just-shipped leaderboards real teeth as a reward.
3. **3rd-class kits** — the moment the CSVs land, this is the highest-value unlock.
4. **Instances** — after the owner picks global-vs-per-instance daily attempts.
5. **Bot-prevention** — when the owner wants it; petrification + tap-CAPTCHA is a contained first slice.
