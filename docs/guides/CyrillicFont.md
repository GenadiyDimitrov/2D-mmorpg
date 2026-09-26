# Cyrillic in the client font (`BL-313`, half 2)

✅ **Done without the Editor in 0.214.23** (the owner, 2026-09-26: *"if u can do it alone so do it"*). This page records
what was done and how to redo it. **Nobody needs to open Unity for it.**

## What was wrong

- The main font, `LiberationSans SDF`, is a **static** atlas of 250 baked characters. Nothing outside that set can
  draw from it.
- The source font file, `Assets/TextMesh Pro/Fonts/LiberationSans.ttf`, **does contain Cyrillic** (А-я, Ё, Й, …) and
  the arrows.
- A dynamic fallback, `LiberationSans SDF - Fallback`, sits behind the main font and should draw those characters on
  demand. **On the phone it did not**, so Cyrillic fell through to boxes. The cause is unknown (it would need the
  phone's own log), so the fix avoids runtime generation altogether.

## What 0.214.23 did

`Assets/Editor/TmpCyrillic.cs` baked a **static** font asset, `LiberationSans SDF - Cyrillic`, in the same folder as the
main font and with the main font's settings (86 pt, padding 9, SDFAA, 1024×1024). It holds 105 characters:

- U+0400-045F, the whole basic Cyrillic block (Bulgarian, Russian, Serbian, Ukrainian …);
- the arrows `← ↑ → ↓ ↔ ↕`;
- `■ ○ ●`.

It is **first** in the main font's fallback list, ahead of the old dynamic one. Everything is committed, so a normal
APK build ships it; the script does not run during builds.

## Re-running it (only if the character set changes)

Close Unity, then from the repo root:

```
"C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" -quit -batchmode -nographics ^
  -projectPath Game.Client.Unity -executeMethod Game.Client.Editor.TmpCyrillic.Bake -logFile cyrillic.log
```

The log's `[cyrillic] baked N characters …` line lists the fallback order. A rerun replaces the asset and keeps one
fallback entry for it. To add characters, extend `TmpCyrillic.Characters()`.
