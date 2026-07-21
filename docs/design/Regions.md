# Regions — polygonal fields and towns

**Status: DESIGNED, NOT BUILT.** Owner's spec, 2026-07-22.

## The problem

Today the world is a bag of **circles**. `SpawnZone` is `(X, Y, Radius, …)` and `SafeZone` is
`(X, Y, Radius)`. Everything downstream inherits that shape: the client draws discs, teleports land on
a centre point, and a town is a perfect circle with hunting grounds in rings around it. It reads as
generated, because it is.

## The model

One wrapper — a **Region** — with a real outline:

```
Region
  Id            "field_massacre"
  Name          "Field of Massacre"
  Kind          Town | Field
  Outline       Vec2[]        polygon, world coordinates
  ArrivalPoints Vec2[]        teleport lands on ONE AT RANDOM (a single point = always there)
  SpawnZoneIds  string[]      the circular spawners INSIDE it — unchanged, they still spawn mobs
```

- **The polygon is the shape.** It is what the client fills, what "you are in the Field of Massacre"
  tests against, and what makes the map stop looking like scattered coins.
- **Spawners keep their circles.** A field does not spawn anything; it contains spawners that do. No
  mob data changes, which is what keeps this additive rather than a rewrite.
- **A region may have NO spawners** — a peaceful field is just a named area with an outline.
- **Level band is DERIVED**, never authored: `Min = min(spawner mins)`, `Max = max(spawner maxes)`.
  Two spawners at 5-15 and 13-17 give a field of 5-17. Authoring it separately guarantees it drifts.
- **Towns are the same wrapper**, `Kind = Town`. A town stops being a circle, gets its own arrival
  point(s), and can be surrounded by four different fields on four sides.

## What this changes downstream

| Area | Change |
|---|---|
| Teleport | Gatekeeper destinations gain FIELDS beside towns; arrival is a random `ArrivalPoint` |
| Client map | Fill the region polygon; the level-band colour moves off the spawner discs onto the field |
| "Where am I" | Point-in-polygon instead of distance-to-centre |
| Safe zones | `InAnySafeZone` becomes "inside a Region with Kind == Town" |

## 🔴 The trap: safe-zone semantics

`InAnySafeZone` / `SafeZoneAt` are not cosmetic. They gate **PvP**, jail release, respawn, vendor
access and karma rules. Swapping a distance check for a polygon test changes *where those rules apply*
— and a polygon wound the wrong way, or a boundary a metre off, changes gameplay silently, with no
error and no crash. A town that is 5% smaller than it looks is a town where someone gets killed
standing at what appears to be the fountain.

So this ships in two stages, and the second one is the dangerous one:

**Stage 1 — additive, low risk.** Add `Region`, point-in-polygon, derived level bands, field teleport
destinations, and the client drawing polygons. `SafeZone` is untouched; towns keep working exactly as
they do. Nothing that currently gates a rule changes behaviour.

**Stage 2 — migrate towns.** Convert `SafeZone` call sites to `Region`, with the town polygons
authored to *contain* the old circles rather than approximate them, so no location that is safe today
becomes unsafe. Needs a SmokeTest case per rule that depends on safe zones (PvP refusal in town, jail
release, respawn point) — these are exactly the bugs a human playtest does not catch, because nobody
stands on the boundary on purpose.

## Implementation notes

- **Point-in-polygon**: standard ray-crossing test. Precompute an axis-aligned **bounding box** per
  region and test that first — regions are few (dozens) but the check runs per player per tick, and a
  bbox rejects almost every candidate in one comparison.
- **Winding**: pick one convention (counter-clockwise) and assert it at startup, next to the existing
  skill-id collision guard. A reversed polygon still *tests* correctly for containment but will render
  inside-out.
- **Rendering**: the polygon needs triangulating for a mesh (ear clipping is enough for simple
  polygons). Both clients change — the WPF reddish overlay moves from spawners to fields too.
- **Authoring**: outlines are data, so they belong beside the existing zone tables in `WorldMap`, and
  should be readable/editable as plain coordinates rather than generated.

## Open question for the owner

Do towns become `Kind = Town` on the SAME type (one list of regions, filtered), or a separate list
that happens to share the shape? One list is simpler to reason about and makes "which region am I in"
a single query; two lists keep the safe-zone code visibly distinct from decorative fields, which has
some value given what those rules gate.
