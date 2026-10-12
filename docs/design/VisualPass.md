# The visual pass (`BL-344`): survey and proposed order

Step 1 of `BL-344` (2026-10-12). This is what the Unity client draws today, read off the code at 0.242.0, and the
order I propose for the work. **No asset work starts until you pick from §3.** The direction set in `BL-93`
(2026-08-26) still holds: low-poly stylised CC0 art, Humanoid rigs, the art budget spent per *family* and not per
mob, terrain that is cosmetic only (the server stays 2D at y=0), and an APK that stays at or under 120 MB.

## 1. What the client draws today

### Ground
| Layer | What it is | File |
|---|---|---|
| Base | One Unity `Plane` scaled to the whole 240-unit world, flat dark green `(0.13,0.16,0.14)`, **unlit**, no texture. The camera clears to the same colour, so zooming out past the map edge shows more ground instead of a hole. | `WorldGround.cs` |
| Grid | A wireframe every 1000 server units. It exists so that walking reads as movement on a featureless floor. | `GroundGrid.cs` |
| Field fills | Each authored field region is a flat polygon, coloured green → yellow → red by its level band. Towns are slate "islands" on top. All unlit. | `GameUi.Regions.cs` |
| Zone discs | Spawn zones outside any field (bosses, the dungeon) are flat discs coloured the same way. | `ZoneOverlay.cs` |
| Decals | Totem footprints, trap rings and the AoE flash. These are the only transparent things in the world. | `GroundDecals.cs` |

No height, no texture, no props (trees, rocks, buildings), no lighting on the ground. The `RegionMap` polygons
already know where every field, town and level band is, so a generated look costs no new data.

### Entities
Every entity is a sphere unless `ModelLibrary` (`EntityManager.cs`) finds a prefab for it. The lookup walks from
the most specific key to the most general and stops at the first hit; the last resort is `Models/humanoid`.

| Who | Draws as | Animates? |
|---|---|---|
| Every player, every NPC | `humanoid.prefab` = `Characters/Man/Adventurer.fbx` (one body for all races, both sexes, all classes) | **No.** It slides in its bind pose. The character pack has zero animation clips (`BL-102`). |
| Animal mobs (Ridgeback Pup, Fox, Ashen Wolf, …) | `mob_animal` = Rat | Yes: idle, walk, run, attack, death |
| Insect mobs | `mob_insect` = Spider | Yes |
| Humanoid, Undead, Demon, Dragon, Plant, MagicCreature, Angel mobs | **The same human body as the players** | No |

The code is ready for animation. `EntityView` turns the model to face its movement and drives four optional
Animator parameters from messages the server already sends: `Speed`, `Attack` (trigger, on `CombatEvent`),
`Casting` and `Dead`. Character clips only need to exist.

The repo also holds models that are committed but not used: Skeleton, Dragon, Bat, Frog, Slime, Snake,
Snake_angry and Wasp, plus 20 more character bodies. Several of those bodies are modern (Spacesuit, Swat,
SciFi, Punk, Suit, Casual, Beach) and do not fit a fantasy world.

### Everything else
- **Camera:** top-down by default (`Pitch` 90), with the Settings sliders for pitch, height, rotation and
  ortho/perspective. ⚠ From straight overhead you see the tops of heads, so a walk cycle barely reads. That
  matters for how much animation is worth.
- **Lighting:** one directional light in the scene. Spheres, the ground and every overlay use unlit materials.
  The imported models keep their own materials.
- **FX and sound:** none. There are no particle systems and no audio anywhere in the client. The only "skill
  effect" is the ground flash.
- **APK:** 54 MB (`builds/L2Clone.apk`, 0.242.0). It was 43 MB before any art.

### The asset pipeline
- `Assets/Editor/ModelSetup.cs` builds every prefab and Animator controller **headlessly** (Unity 6000.3.19f1 is
  installed on this PC, so I can run it here with the Editor closed). Adding a creature family is one line in
  `Families`. Character clips go in `Models/Characters/Animations/` and retarget through the Humanoid avatar.
- Models load by key through one function (`ModelLibrary.Load`), so a later switch to downloadable Addressables
  is one function body.
- 🔴 **Everything in a `Resources/` folder ships in the APK, used or not.** About 13 MB of the 16 MB of source
  FBX in `Resources/Models` is files nothing references. Moving unused art out of `Resources/` costs nothing and
  is the first thing to do before adding more.

## 2. Proposed order (my recommendation)

Each step is one version and one APK.

1. **Clean-up and the free families (no download needed).** Move the unused bodies and spare monsters out of
   `Resources/` (to `Assets/Art/`, still in git, no longer shipped). Add the two families whose models are
   already committed: `mob_undead` → Skeleton (~10 templates), `mob_dragon` → Dragon (~7). Optionally
   `mob_magiccreature` → Slime (5: wisps, golems, the Watcher Eye). That takes 17-22 more templates off the
   human body. Demon, Angel and Plant still have no fitting model.
2. **Character animation (`BL-102`).** Get idle, walk, run, attack, cast and death onto the humanoid body. Every
   player, NPC and humanoid mob then moves. Source: see Q3 below.
3. **The camera question.** Settle the default angle **before** terrain, because how the ground and props should
   look depends on whether you see them from above or at an angle. The slider stays either way.
4. **Ground.** Replace the flat plane with generated terrain that follows the regions. Style: see Q2 below.
5. **Light and time of day.** Move the ground and models to lit materials, add model shadows as a quality
   setting, and turn the directional light by `GameClock` (already synced from the server), giving day/night
   for one rotation.
6. **Skill FX archetypes.** About 8 particle effects (projectile, beam, self-aura, heal glow, melee slash,
   debuff mote, resurrect pillar, plus the AoE flash already built), chosen by one field on `SkillDef`. Every
   skill gets a look with no protocol change.
7. **Weapons in hand (`BL-103`).** Eight weapon meshes on the right-hand bone. This one needs a protocol bump,
   because `EntityDto` does not carry equipment.

Sound stays with `BL-45`, which you kept as its own conversation. It can slot in after step 5.

**Why this order:** steps 1 and 2 fix the most visible problems (human bodies on monsters, characters that
slide) for the least work, and step 1 needs nothing from you. Terrain is the biggest single change, but it
waits on the camera decision.

## 3. Your picks

**Q1 — Order.**
- **A (recommended):** as in §2: clean-up/families → animation → camera → ground → light → FX → weapons.
- **B, ground first:** camera → ground → light, then models and animation.
- **C, models first:** clean-up/families → animation → weapons, then the world.

**Q2 — Ground style** (step 4).
- **A (recommended): textured and blended.** Tiling CC0 ground textures (grass, dirt, rock, snow, sand) blended
  per region, roads drawn as a darker strip, and a small cosmetic height wobble that never touches the y=0 tap
  plane. Lit. About 2-5 MB of textures.
- **B, vertex-coloured.** No textures: a chunked mesh coloured per level band, darker in camps, with the same
  height wobble. Almost free in APK size, but reads as "stylised map" rather than "world".
- **C, A plus props.** As A, plus scattered CC0 trees, rocks and grass clumps per region, with a density setting
  for slow phones. The biggest change, and the most draw calls. Could follow A as its own step.

**Q3 — Where the character animation comes from** (step 2). Each option needs a download done in your browser.
- **A (recommended): Quaternius Universal Animation Library, Standard.** Free, CC0, 45 clips, built on a
  universal humanoid rig for Unity retargeting. It covers idle, walk, run, death and more.
- **B: the same library, Pro** (pay what you want, $14.99 suggested), 120+ clips. Library 2 (also CC0, 130+
  clips) adds melee and armed combos, which would cover attack and cast better.
- **C: Mixamo** (free, needs an Adobe login): upload one body, pick the clips, download "without skin".
  The best choice of individual clips, but more clicking.

**Q4 — Camera default** (step 3). You deferred this in `BL-93` until the model proof of concept existed. It exists now.
- **A: keep top-down (90°)** as the default.
- **B: change the default to a 3/4 view (~55°).** Animation, faces and props read far better, and taps still
  resolve on the ground plane. The slider keeps 90° for anyone who wants it.

**Q5 — The modern character bodies** (Spacesuit, Swat, SciFi, Punk, Suit, Casual, Beach).
- **A (recommended): move them out of the build and keep them in git.**
- **B: delete them from the repo.**

## Sources
- [Quaternius — Universal Animation Library](https://quaternius.com/packs/universalanimationlibrary.html)
- [itch.io — Universal Animation Library](https://quaternius.itch.io/universal-animation-library)
- [OpenGameArt — Universal Animation Library 2](https://opengameart.org/content/universal-animation-library-2)
