# Unity Editor — the manual steps

Everything about this client is built, installed and driven **headlessly** (see
`docs/unity/README.md` and the build command in the playtest notes). This file is the short list of
things that genuinely cannot be done that way, so you only ever open Unity for what is on this page.

**Rules:**
- Do the steps **in order**, and only the ones under a heading marked **TODO**.
- When a step is done, change its heading to **DONE (date)** and commit this file — that is how we
  both know the project state without asking.
- **Close Unity when you are finished.** The project lock stops every headless build while the
  Editor is open.

---

## 1. Import TMP Essential Resources — **TODO**

**Why:** the UI is being rebuilt on uGUI + TextMeshPro (the UI that stays — it is what movable
windows, resizable panels and a portrait layout will need). TextMeshPro resolves its default font
through `TMP Settings`, which ships in a package that has to be imported into `Assets/` once per
project. Until it is, **no text renders at all** — not "it looks wrong", nothing appears.

**Why you and not me:** the importer runs `AssetDatabase.ImportPackage`, which finishes on a later
editor tick. In batchmode `-quit` ends the process first, so the import starts and is thrown away.
I tried the menu item (it only opens the importer window and waits for a click, so batchmode reports
success and imports nothing) and the internal importer type by reflection (runs, imports nothing for
the tick reason). This is the one-click step it is not worth fighting.

**Steps:**

1. Open Unity Hub → open the project `G:\Work\Repository\L2Clone\Game.Client.Unity`.
   The first open after a headless build re-imports assets — give it a minute.
2. Menu: **Window ▸ TextMeshPro ▸ Import TMP Essential Resources**.
3. A window titled *TMP Importer* appears. Click **Import TMP Essentials**.
   (Do **not** import "Examples & Extras" — they are samples we do not ship.)
4. Wait for the progress bar to finish. A folder **`Assets/TextMesh Pro/`** now exists in the
   Project window, containing `Resources/TMP Settings.asset` among others.
5. **File ▸ Save Project.**
6. **Close Unity** (fully — check no `Unity.exe` is left running, or my next build will fail on the
   project lock).
7. Tell me it is done. I verify with the marker file below and carry on.

**How I verify it worked** (you do not need to run this — I do):

```
Test-Path "G:\Work\Repository\L2Clone\Game.Client.Unity\Assets\TextMesh Pro\Resources\TMP Settings.asset"
```

**Commit it:** the imported folder belongs in git — otherwise a fresh checkout is back to a UI with
no font. It is a few MB.

---

## Done

*(nothing yet)*
