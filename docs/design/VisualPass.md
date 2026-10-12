# The visual pass (`BL-344`): survey and proposed order

Step 1 of `BL-344` (2026-10-12). This is what the Unity client draws today, read off the code at 0.242.0, and the
order I propose for the work. **No asset work starts until you pick from §3.** The direction set in `BL-93`
(2026-08-26) still holds: low-poly stylised CC0 art, Humanoid rigs, the art budget spent per *family* and not per
mob, terrain that is cosmetic only (the server stays 2D at y=0), and an APK that stays at or under 120 MB.

## 0. Decided (2026-10-12) — the LIVE order and where each step stands

Your picks: **Q1 = A**, **Q2 = B with C as its own step**, **Q3 = A** (*"Explain steps - for dummies"*), **Q4 = A**
(*"for now .. later i would like to add horizontal rotation and limited vertical"*), **Q5**: *"I would like to have
fantasy ones so we can remove them from later commits (they will still hold in history)"*, which is B for the modern
bodies (deleted, git keeps them) and the fantasy ones kept.

| # | Step | Status |
|---|---|---|
| 1 | Clean-up + the free families | ✅ **0.243.0.** Undead → Skeleton, Dragon → Dragon. Source FBXs moved to `Assets/Art/Models/`; 15 modern bodies deleted, 6 fantasy kept (Man Adventurer / Farmer / King, Woman Adventurer / Medieval / Witch). Slime was NOT given to MagicCreature: that family is golems, a lich, blades and an eye, and a slime fits none of them. |
| 2 | Character animation (`BL-102`) | ✅ **0.244.0.** Your `UAL1_Standard.fbx` (Quaternius Universal Animation Library, Standard, CC0). Picks: idle = `Idle_Loop`, walk = `Walk_Loop`, run = `Jog_Fwd_Loop`, attack = `Sword_Attack`, death = `Death01`, cast = `Spell_Simple_Idle_Loop`. One body and one clip set for everyone; bows and staves still swing the sword take until step 7. APK 47.3 → 47.4 MB: only the six used clips ship. |
| 3 | Ground, vertex-coloured (your Q2 = B) | ✅ **0.245.0.** Biome per city territory, blended by distance (`Game.Shared/Biomes.cs`), streamed 2000-unit chunks (`BiomeGround.cs`). Your picks: level band on the field RIMS, grid DELETED, FLAT, biomes as proposed **+ lava** (Redhorn Highlands). Old plane + grid deleted (they covered only a quarter of the world). |
| 4 | Props: trees, rocks, grass clumps (your Q2's C, own step) | ✅ **0.246.0** scatter (`PropScatter.cs`, §0.3) + **0.247.0** real models from the nature pack (§0.2): 19 picks over 9 kinds, twisted trees in the marsh, flower patches in the meadow. APK 47.5 → 49.7 MB. ❓ judge in a playtest: density, tree size, trees hiding you in a wood (§0.3 last point), frame rate on High. |
| 5 | Light and time of day | ✅ **0.248.0.** Lit ground (`LitVertexColor.shader`), characters cast shadows, the directional light turned by `GameClock` (`DayNight.cs`, §0.4). Your pick: night 0.30 (*"0.12 is to dark, 0.2 as well"*), plus **Settings → Night brightness** 0.10-0.60 for your *"a bar or gamma as other games"*. Settings → Shadows: characters (default) / + trees / off. ❓ judge on the phone: frame rate with trees casting, how night reads outdoors. |
| 6 | Skill FX archetypes | ✅ **0.249.0.** `FxPlayer.cs`, `SkillDef.Fx` + `SkillFxRules` (§0.5). Your picks: exceptions in `docs/data/skill_fx.csv` (one row: Mana Ray = Beam), basic attacks show only crit sparks. ❓ judge on the phone: size and brightness at your zoom, the frame rate in a crowd fight. |
| 7 | Weapons in hand (`BL-103`, protocol bump) | |
| 8 | Camera: horizontal rotation + limited vertical tilt (your Q4 "later") | top-down stays the default until then |

### 0.1 Step 2: the one file I needed from you (done 2026-10-12, kept for the next download)

1. Open **https://quaternius.itch.io/universal-animation-library** in your browser.
2. Click **Download Now**. A page asks for money: click **"No thanks, just take me to the downloads"** (it is CC0 and
   free; tipping is optional).
3. Download **`Universal Animation Library[Standard].zip`** (about 15 MB). Not Pro, not Source.
4. Unzip it anywhere (right-click → *Extract All*).
5. Inside, look for the **Unity** export as an **`.fbx`** file. There are two of each: one whose name ends in **`_RM`**
   (root motion) and one without. **Take the one WITHOUT `_RM`**: the server moves the characters, and root motion
   would fight it. If there is no Unity-specific file, take the plain `.fbx` without `_RM`.
6. Copy that ONE `.fbx` into
   **`G:\Work\Repository\L2Clone\Game.Client.Unity\Assets\Art\Models\Characters\Animations\`**
   (the folder already exists, with a `.gitkeep` in it). Do not rename it.
7. Tell me "animations are in". I run the builder headlessly, check which takes it found (idle, walk, run, attack,
   death, cast), and ship the APK. If the Standard set has no attack or cast take, I'll say which one is missing
   rather than borrow a wrong-looking one.

You do not need to open Unity or Mixamo. The file stays outside `Resources/`, so only the clips the controller uses
reach the APK.

### 0.2 Step 4: the nature pack (done 2026-10-12, kept for the next download)

**Built 0.247.0.** The pack's `FBX(Unity)` export sits in `Assets/Art/Models/Nature/FBX/` with `Textures/` beside it
(the FBXs find their maps by relative path; keep that layout). `ModelSetup.BuildProps` (`Assets/Editor/ModelSetup.Props.cs`)
builds `Resources/Props/<Kind>/` headlessly; `ModelSetup.PreviewProps` renders every pick to a PNG (batchmode WITHOUT
`-nographics`, `-previewOut <path>`) so the textures can be checked without the Editor or a phone.

| Kind | Models | Why |
|---|---|---|
| Tree | CommonTree_3, CommonTree_5 | the two lightest broad trees (~3.2-3.5k tris) |
| TwistedTree (**new kind**, Marsh) | TwistedTree_1, TwistedTree_2 | gnarled = swamp. ⚠ the heaviest picks (~9.3k tris); they replace the broad tree in the marsh only |
| Pine | Pine_4, Pine_5 | lightest (1.6k / 3.4k tris) |
| DeadTree | DeadTree_4, DeadTree_5 | lightest of five near-equal ones |
| Bush | Bush_Common, Bush_Common_Flowers | the only two |
| Rock | Rock_Medium_1, _2, _3 | all three: 250-520 tris each |
| Grass | Grass_Common_Short, _Tall | green |
| DryGrass | Grass_Wispy_Short, _Tall | the pack has no dry grass; the wispy ones already sit on the orange strip of the grass map, muted a little |
| Flower (**new kind**, Meadow) | Flower_3_Group, Flower_4_Group | groups, not singles: one prop reads as a patch |

**Materials are ours** (`Art/Models/Nature/Materials/`, URP Lit, instancing ON as shipped assets so the build keeps
the instanced variant): leaves, grass and flowers alpha CLIP + two-sided, never blended; mip coverage preserved so
leaves don't thin out at distance. The pack's twisted-tree leaf map is autumn RED (the plain bush wears it too), so
those use the white mask tinted moss green. Normal maps unused, textures capped at 512 (1024 for the plant atlas,
128 for the grass gradient): what reaches the APK is those, not the pack's 2048² maps.

The original download how-to:

The pack is **Quaternius Stylized Nature MegaKit**: CC0 (free for any use, no credit needed), the free Standard tier
has about 70-80 textured models (trees incl. pine, birch and dead/dry trees, rocks, bushes, grass, flowers, mushrooms).

1. Open **https://quaternius.itch.io/stylized-nature-megakit** in your browser.
2. Click **Download Now**, then **"No thanks, just take me to the downloads"** (free; tipping optional).
3. Download **`Stylized Nature MegaKit[Standard].zip`** (about 99 MB). Not Pro, not Source.
   (Mirror if itch is down: https://opengameart.org/content/stylized-nature-megakit, file
   `stylized_nature_megakitstandard.zip`; it may be an older copy.)
4. Unzip it anywhere (right-click → *Extract All*).
5. Inside are the same models in three formats (FBX, OBJ, glTF). **Take only the `FBX` folder.** If the textures
   (`.png` files) sit in their own folder BESIDE it rather than inside it, take that folder too: the FBX files find
   their textures by relative path.
6. Copy them into **`G:\Work\Repository\L2Clone\Game.Client.Unity\Assets\Art\Models\Nature\`** (the folder exists,
   with a `.gitkeep`). Do not rename anything. Skip OBJ and glTF; they would only grow the repo.
7. Tell me "nature is in". I pick about two models per kind (broad tree, pine, dead tree, bush, rock, grass, dry
   grass), build them into `Resources/Props/<Kind>/` headlessly, and ship the APK. Only the picked models reach the
   APK; the rest stay in `Art/` (in git, not shipped), like the character bodies.

### 0.3 Step 4: how the scatter works (built 0.246.0)

- **Per-biome sets** (props per 100×100 cell before clumping): Meadow 0.22 (broad trees, bushes, a few rocks, grass, flower patches),
  Moor 0.16 (some pines, bushes, rocks, grass), Ash 0.10 (dead trees, rocks, dry grass), Marsh 0.22 (twisted and dead
  trees, bushes, grass), Highland 0.18 (pines, rocks, dry grass), Snow 0.14 (pines, rocks), Lava 0.08 (dead trees,
  rocks). The biome is drawn from the same territory weights that colour the ground, so props mix across a border
  exactly where the colours do.
- **Woods, not an even sprinkle:** trees and bushes follow a 1500-unit noise (dense woods, near-bare clearings);
  rocks gather into outcrops on a finer one; grass is even.
- **Kept clear:** towns plus a 250 rim, 380 either side of the road's centre line, the middle 55% of every mob camp
  (where the fighting is), 250 around any NPC outside a town, and the dungeon void. Grass may come 25% closer.
- **Deterministic:** every number is a hash of the cell, so the same spot grows the same tree on every phone and every
  visit. It streams with the ground chunks (rolled when one is built, dropped when it is pooled).
- **Mobile cost:** GPU instancing, gathered across all chunks in range, so the draw calls are one per prop mesh and
  material (about a dozen), however many chunks are on screen. No collision, no shadows yet (step 5).
- **Settings → Scenery: High / Medium / Low / Off.** Density ×1 / ×0.65 / ×0.35 and draw range 9000 / 6500 / 4000
  units from the screen centre. Off is the bare ground.
- ❓ **One thing to judge in a playtest:** from straight overhead a tree hides whatever walks under it. Camps, roads
  and towns are kept clear for that reason, but walking through a wood you will lose sight of yourself for a moment.
  If that bothers you, the fix is to fade the trees near your character; say so and I'll add it.

### 0.4 Step 5: how the light works (built 0.248.0)

- **One directional light, keyed over 24 game hours** (`DayNight.Keys`): night (moon, blue) to 04:30, purple twilight
  at 05:30, warm sun at 06:30, white day 08:30-15:30, warm again at 17:30, twilight 18:30, night from 19:30. Blended
  linearly between keys. The sun rises in the east, stands in the south at noon (shadows point up the screen), sets in
  the west, never lower than 25°; the moon crosses at 50°. The swap between them happens in the dim twilight hours.
- **Brightness:** midday ≈ the old unlit ground (sun 0.8 + ambient 0.33). Night = `NightLevel` of that, half moon and
  half blue ambient. ⚠ The keys are LINEAR light; Unity reads a light's colour and the flat ambient as sRGB, so they
  go in as `.gamma` (the first preview had every dim scene at a third of the intended brightness for that reason).
- **Shadows:** hard, one cascade (the phone's URP asset). The range follows the camera: the distance to the farthest
  ground corner on screen, so a zoom-out never cuts shadows off mid-screen. Characters cast; props cast only on
  "characters + trees"; the ground and placeholders never cast.
- **Not lit, on purpose:** name plates, UI, zone colours, decals, the move marker and the fallback spheres.
- **Preview:** `ModelSetup.PreviewLight` (batchmode WITHOUT `-nographics`, `-previewOut <png>`): real biome ground beside
  Brackenford, two players, a rat, props; dawn / noon / dusk / twilight, then night at the slider's min / default / max
  and before dawn. ⚠ It renders once to warm up: the first render draws before the textures upload.

### 0.5 Step 6: how the skill effects work (built 0.249.0)

- **Two halves.** The DELIVERY, how a skill travels, is the one per-skill choice: `SkillDef.Fx` (`Auto` / `None` /
  `Instant` / `Slash` / `Projectile` / `Arrow` / `Beam`). The LANDING, what plays on the target, comes from the
  outcome the server already sends: hit = sparks, crit = a bigger orange burst, magic fail = a dim one, heal = green
  glow, MP = blue glow, buff = gold ring, debuff = purple motes falling, miss = nothing. One short enum covers every
  skill that way.
- **`Auto`** (`SkillFxRules.AutoDelivery`): passives draw nothing; caster-centred areas and self-only skills are
  `Instant` (the area keeps its ground flash and every target gets its landing); damage with a bow is `Arrow`;
  physical damage at melee reach (range ≤ 100, melee is 40) is `Slash`; ranged damage is `Projectile`; ranged heals
  and buffs are `Beam` (no beam when you cast on yourself); ranged debuffs fly as a `Projectile`. At 0.249.0 that gives
  29 slashes, 15 arrows, 79 projectiles, 54 beams, 508 instant.
- **Exceptions:** `docs/data/skill_fx.csv` → `SkillFx.g.cs` (`SkillCsvSeed --gen-fx`), applied in `BuildCatalog`.
  Never write `Fx =` in a SkillDef; `--check` catches it. An unknown id in the file is skipped, not thrown (a look
  must never stop the server booting).
- **The wire:** `CombatEvent` gained `SkillId` at the end, because `Skill` is the caster's display name (a race face,
  sometimes with " [Reflect]") and cannot be looked up. The server sets it in ONE place, `ExecuteSkill`, for every
  event the resolution sends; `TryProcs` clears it. Ticks, traps, procs and the whisp send `""` and draw nothing.
- **The pillar** needs no message: `EntityManager.Upsert` sees a PLAYER go from dead to alive without the teleport
  counter (`Warp`) moving. A respawn in town moves it, so it gets no pillar.
- **Cost:** three `ParticleSystem`s for the world (soft dots, sparks with drag, stretched streaks), one draw call
  each, fed by `Emit` with one reused parameter struct. Projectiles in flight sit in a fixed 64-slot array and land
  where the target IS on arrival. Shader `Resources/Shaders/FxAdditive.shader`: additive, a soft round falloff
  computed from the UV, no texture. Additive means a debuff can't be dark, so it is a dusky purple.
- **From above** (the default camera) a vertical rise is a dot, so every landing also spreads outward on the ground.
- **Preview:** `ModelSetup.PreviewFx` (batchmode WITHOUT `-nographics`, `-previewOut <png>`): 12 frames, one effect
  each, the last one straight down. It logs the live particle count per frame.

## 1. What the client draws today

### Ground
> ⚠ This is the state at 0.242.0. Since 0.245.0 the Base, Grid and Field-fill rows are replaced by `BiomeGround.cs` (see §0, step 3).

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
