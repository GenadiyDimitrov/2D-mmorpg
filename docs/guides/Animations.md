# Animations — where to get them and how they get into the game

(2026-10-13, from the owner's question: *"can i make animations? or some sort of free software where i give clips and it
takes the body movement of the person on the clip and converts it to a animation?"*)

## Why almost anything works

Every character body (players, NPCs, humanoid mobs) uses Unity's **Humanoid** rig — the Quaternius outfit kit on the
Universal Animation Library skeleton (0.253.0). A Humanoid animation is stored as *muscle* motion, not as bone names, so
an animation made for **any** humanoid skeleton plays on ours. You do not need a file made for our exact model.

## 1. Mixamo — ready animations (start here)

- **mixamo.com**, free with an Adobe account.
- Hundreds of animations: idles, walks/runs, sword combos, two-handed swings, bow shots, spell casts, hits, deaths,
  emotes, sitting.
- Licence: free to use in a game, commercial included, no royalties.
- Download as **FBX for Unity**, **"Without Skin"** (just the motion). For a loop (idle, run), tick **In Place** so the
  character does not walk away from its own position — the server moves the body, not the animation.
- Most of what we are missing is here, so look here before making anything.

## 2. Turning a video into an animation (motion capture from a phone)

You film a person doing the move, upload it, and get an animation file back.

| Tool | Cost | Notes |
|---|---|---|
| **Rokoko Vision** | free tier | one camera, short clips; exports FBX |
| **DeepMotion Animate 3D** | free monthly credits | ~short clips per month on the free plan; exports FBX |
| **FreeMoCap** | free, open source | best with 2+ cameras; runs on your PC |

What to expect:
- **Good for:** idles, emotes, casting gestures, sitting, waving, slow motions.
- **Bad for:** fast weapon swings — the arm blurs on video and the capture loses it.
- The result is usually **jittery** (feet sliding, hands trembling) and needs a cleanup pass in Blender: smooth the
  curves, pin the feet.

Filming tips: the whole body in frame head to feet, a fixed camera (on a stand, not handheld), plain background, tight
clothes, good light, start and end standing still.

## 3. Making them by hand in Blender

Possible (Blender is free) but slow — hours per good animation. Worth it only to **fix** an animation from 1 or 2
(a hand that clips the body, a loop that does not join), not to make the whole library from scratch.

## Getting a new animation into the game

1. Put the downloaded **FBX** in `G:\Downloads` and say what it is for ("idle for mages", "two-handed swing", …).
2. Claude imports it into `Game.Client.Unity/Assets/Art/Characters/Animations/` and wires it into the animator in
   `Assets/Editor/ModelSetup.cs` (it finds clips by name — `FindClip`), then rebuilds the prefabs and the APK.
3. Which moves exist today: idle, walk, run, a sword swing, a bow shot (`Pistol_Shoot`), a spell thrust, casting,
   death — all from the Universal Animation Library (CC0).

Good first additions: a different idle per weapon (two-handed, bow, staff), a proper bow draw (the current shot is a
pistol take, which is why the bow sits in the right hand), more sword swings so attacks are not all the same.
