# Adding Cyrillic to the client font (`BL-313`, half 2)

**Why:** Bulgarian text (chat, names, whispers) shows as boxes on the phone. Once, in the Unity Editor, you bake a
second font atlas that holds the Cyrillic letters and hang it behind the main font. After that it ships in every APK
and nobody has to open the Editor again. It takes about 10 minutes.

## What is already true (checked 2026-09-26, so you don't have to)

- The main font, `LiberationSans SDF`, is a **static** atlas of about 250 baked characters. Nothing outside that set can
  draw from it.
- The source font file, `Assets/TextMesh Pro/Fonts/LiberationSans.ttf`, **does contain Cyrillic** (А-я, Ё, Й, …) and
  the arrows. You need no new font file.
- A dynamic fallback, `LiberationSans SDF - Fallback`, is already hung behind the main font, and it should draw
  those characters on demand. **On the phone it does not**, so a Cyrillic letter still falls through to the box.
  I could not find the cause without the phone's own log. The fix below works around it: it bakes the letters ahead of
  time, so nothing has to be generated on the phone.

## The steps

1. **Open the project** `Game.Client.Unity` in Unity 6 (the same version the build uses). Let it finish importing.

2. **Open the Font Asset Creator:** menu **Window → TextMeshPro → Font Asset Creator**.

3. **Fill it in exactly like this** (these match the main font, so the letters come out the same size and weight):

   | Field | Value |
   |---|---|
   | Source Font File | `LiberationSans` (drag in `Assets/TextMesh Pro/Fonts/LiberationSans.ttf`) |
   | Sampling Point Size | **Custom Size**, `86` |
   | Padding | `9` |
   | Packing Method | `Optimum` |
   | Atlas Resolution | `2048` × `1024` |
   | Character Set | **Unicode Range (Decimal)** |
   | Character Sequence (Decimal) | `1024-1119, 8592-8597, 9632, 9675, 9679` |
   | Render Mode | `SDFAA` |

   The sequence is: `1024-1119` = the whole basic Cyrillic block (Bulgarian, Russian, Serbian, Ukrainian …),
   `8592-8597` = the arrows `← ↑ → ↓ ↔ ↕`, and `■ ○ ●`, which have boxed before.

4. Press **Generate Font Atlas**. When it finishes, the panel on the right reports the characters it included.
   **"Missing" must read 0.** If it doesn't, change Atlas Resolution to `2048 × 2048` and generate again.

5. Press **Save as…** and save it **in the same folder as the main font**:
   `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Cyrillic.asset`
   (the name matters only so I can find it later).

6. **Hang it behind the main font:** in the Project window, click `LiberationSans SDF` (the main one, in that same
   folder). In the Inspector open **Fallback Font Assets**, press **+**, and drag `LiberationSans SDF - Cyrillic` into
   the new slot. **Put it FIRST**, above the existing `LiberationSans SDF - Fallback`, so the baked atlas is tried before
   the dynamic one. Leave the dynamic one in place; it costs nothing.

7. **File → Save Project**, then close Unity.

8. **Tell me it's done.** I then:
   - check the new asset has the glyphs (a grep for `m_Unicode: 1041`, the Б) and that the main font lists it;
   - commit the two changed assets (the new `.asset` + `.meta`, and `LiberationSans SDF.asset`);
   - build and publish a new APK the usual way.

## How you'll know it worked

Whisper yourself or say something in Bulgarian in chat: it shows letters, not boxes, and the System tab stays quiet.
A name with Cyrillic letters in it shows normally over the character's head.

## If something goes wrong

- **The Font Asset Creator menu item is missing:** the TextMeshPro essentials are not imported. The headless build does
  that itself (`Assets/Editor/TmpSetup.cs`); in the Editor use **Window → TextMeshPro → Import TMP Essential
  Resources**, then start again from step 2.
- **The letters look thinner or bolder than the English ones:** a field in step 3 differs from the table (usually the
  point size or the render mode). Generate again with the table's values and overwrite the same asset.
- **You'd rather not open Unity at all:** say so. The same bake can be done by a small editor script that runs
  headlessly, like `TmpSetup.cs` does for the essentials. I haven't done that yet because I can't check the result on
  the phone before you do, and you asked for the Editor route.
