# Where the sound could come from (`BL-45`, note of 2026-10-12)

Your question, asked during `BL-344` step 4: *"also offer sound ideas ... suno ? or orher ai tools ? or free from
real ppl ? - note"*. A note only; nothing is built. Sound stays `BL-45`, after the visual pass. The game currently has
**no audio at all** (no clips, no AudioSource anywhere in the client).

The game needs two different things, and they come from different places:
- **Sound effects** (hits, swings, spell casts, footsteps, UI clicks, level-up, death, potions): many short clips,
  about 60-120 of them. This is where most of the "feel" comes from.
- **Music** (town theme, field ambience per biome, boss fight, login screen): a handful of long loops.

## A. Free, made by real people (my recommendation for the effects)

| Source | Licence | Notes |
|---|---|---|
| **Kenney** audio packs (kenney.nl: *RPG Audio*, *Impact Sounds*, *Interface Sounds*, *Music Jingles*) | CC0, no credit needed | Same spirit as the Quaternius art. Covers UI, hits, footsteps, coins, jingles. The first place to look. |
| **Sonniss GDC Game Audio Bundle** (sonniss.com/gameaudiogdc) | Royalty-free, commercial use, no credit | Gigabytes of professional effects released free every year. Big download; you pick a few dozen clips. |
| **OpenGameArt.org** | Per file: CC0, CC-BY or GPL | Lots of RPG effects and fantasy music loops. Check each file's licence; CC-BY means a credits line. |
| **Freesound.org** | Per file: CC0, CC-BY or CC-BY-NC | The largest library. ⚠ Avoid **NC** (non-commercial) files if the game might ever earn money. |
| **Pixabay** sound effects and music | Pixabay licence, no credit | Good for ambience (wind, rain, birds, fire). |
| **Kevin MacLeod / incompetech.com** | CC-BY (credit line) | Classic fantasy and battle music; widely heard, so less unique. |

## B. AI tools

| Tool | What it makes | Licence, as checked 2026-10-12 |
|---|---|---|
| **Suno** | Full music tracks from a text prompt | ⚠ **Free plan = non-commercial only**, and a later subscription does **not** clear earlier free songs. Paid plans (Pro/Premier) grant commercial use for songs made **while subscribed**. New terms from 2026-09-03 also cap downloads (reportedly 7 lifetime on Free). AI music likely can't be copyrighted by you, so anyone could reuse it. |
| **Udio** | Music, like Suno | Similar paid-for-commercial model; check its terms on the day. |
| **ElevenLabs Sound Effects** | Short effects from a text prompt ("sword hitting a wooden shield") | Free plan allows commercial use **with credit to elevenlabs.io**; paid plans drop the credit. Can't be resold as an asset pack. Strong for exactly the oddly specific effects a library lacks (a skill's own sound). |

## My recommendation

- **Effects: free libraries first** (Kenney + Sonniss cover most of it at no cost and no risk), and **ElevenLabs** only
  for the specific ones nothing else has, such as a unique sound per skill archetype.
- **Music: one month of Suno Pro** when you are ready, generating all the loops in that month (town, each biome,
  boss, login), *if* you want unique music. Otherwise free CC0/CC-BY loops from OpenGameArt are fine for friends.
  ⚠ Don't make music on Suno's free plan that you mean to keep.
- **Keep a `CREDITS.md`** from the first clip on: source, author, licence, link. CC-BY needs it, and it is what lets
  you prove later that a clip was allowed.
- **APK size:** effects as short mono OGG (~10-30 KB each), music as streamed OGG (~1-2 MB per loop). 100 effects +
  8 loops ≈ 12-15 MB, within the 120 MB budget.

When `BL-45` starts, the engine side is small: an audio manager keyed by event (hit, cast start, cast land, level-up,
UI click), one field on `SkillDef` for the skill's sound archetype (the same idea as the FX archetypes of
`BL-344` step 6), and a volume slider in Settings.

## Sources (checked 2026-10-12)
- [Suno ownership terms after the Warner partnership](https://www.musicinafrica.net/magazine/suno-adjusts-ai-music-ownership-terms-after-warner-music-partnership)
- [Suno commercial use by tier](https://conductatlas.com/platform/suno/suno-terms-of-service/commercial-use-restrictions-by-subscription-tier/)
- [ElevenLabs: commercial sound effects](https://elevenlabs.io/sound-effects/commercial)
