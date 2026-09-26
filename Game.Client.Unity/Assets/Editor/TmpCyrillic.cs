using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Game.Client.Editor
{
    /// <summary>
    /// `BL-313` half 2 — bakes the Cyrillic font atlas WITHOUT opening the Editor (owner, 2026-09-26: *"if u can do
    /// it alone so do it"*). Run once in batchmode; its output is committed, so the normal build never runs it:
    ///
    ///     Unity.exe -quit -batchmode -nographics -projectPath Game.Client.Unity
    ///         -executeMethod Game.Client.Editor.TmpCyrillic.Bake -logFile &lt;log&gt;
    ///
    /// Why a baked STATIC atlas: the main `LiberationSans SDF` is static (about 250 glyphs), and the dynamic
    /// `LiberationSans SDF - Fallback` behind it, which should draw Cyrillic from the .ttf on demand, does not on the
    /// phone. A static atlas needs nothing at runtime. Settings match the main asset (86 pt, padding 9, SDFAA) so the
    /// letters come out the same size and weight. The .ttf already holds every character asked for here.
    ///
    /// Idempotent: a rerun replaces the asset and keeps exactly one entry for it, FIRST in the main font's fallback list.
    /// </summary>
    public static class TmpCyrillic
    {
        private const string Dir = "Assets/TextMesh Pro/Resources/Fonts & Materials/";
        private const string MainPath = Dir + "LiberationSans SDF.asset";
        private const string OutPath = Dir + "LiberationSans SDF - Cyrillic.asset";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";

        /// <summary>U+0400-045F (the whole basic Cyrillic block), the arrows ← ↑ → ↓ ↔ ↕, and ■ ○ ●.</summary>
        private static string Characters()
        {
            var sb = new System.Text.StringBuilder();
            for (int c = 0x0400; c <= 0x045F; c++) sb.Append((char)c);
            for (int c = 0x2190; c <= 0x2195; c++) sb.Append((char)c);
            sb.Append('■').Append('○').Append('●');
            return sb.ToString();
        }

        public static void Bake()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var main = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainPath);
            if (font == null || main == null) Fail("font or main asset missing: " + FontPath + " / " + MainPath);

            // A rerun starts clean: drop the old asset and its fallback entry.
            var old = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutPath);
            if (old != null)
            {
                main.fallbackFontAssetTable.RemoveAll(f => f == null || f == old);
                AssetDatabase.DeleteAsset(OutPath);
            }

            string wanted = Characters();
            TMP_FontAsset fa = null;
            string missing = wanted;
            foreach (int height in new[] { 1024, 2048 })
            {
                fa = TMP_FontAsset.CreateFontAsset(font, 86, 9, GlyphRenderMode.SDFAA, 1024, height,
                                                   AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);
                if (fa == null) Fail("CreateFontAsset returned null");
                fa.TryAddCharacters(wanted, out missing);
                if (string.IsNullOrEmpty(missing)) break;
                Debug.Log("[cyrillic] " + missing.Length + " did not fit at 1024x" + height + ", retrying larger");
            }
            if (!string.IsNullOrEmpty(missing))
                Fail("characters missing after the bake: " + string.Join(" ", missing.Select(ch => ((int)ch).ToString("X4"))));

            fa.name = "LiberationSans SDF - Cyrillic";
            fa.atlasPopulationMode = AtlasPopulationMode.Static;   // clears the runtime font reference, like the main asset
            fa.atlasTexture.name = fa.name + " Atlas";
            fa.material.name = fa.name + " Material";

            AssetDatabase.CreateAsset(fa, OutPath);
            AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
            AssetDatabase.AddObjectToAsset(fa.material, fa);

            main.fallbackFontAssetTable.Insert(0, fa);
            EditorUtility.SetDirty(fa);
            EditorUtility.SetDirty(main);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[cyrillic] baked " + fa.characterTable.Count + " characters into " + OutPath + " ("
                      + fa.atlasTexture.width + "x" + fa.atlasTexture.height + "), fallbacks of the main font: "
                      + string.Join(", ", main.fallbackFontAssetTable.Select(f => f == null ? "null" : f.name)));
        }

        private static void Fail(string why)
        {
            Debug.LogError("[cyrillic] FAILED: " + why);
            EditorApplication.Exit(1);
        }
    }
}
