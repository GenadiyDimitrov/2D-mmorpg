using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.Client.Editor
{
    /// <summary>
    /// Imports the TextMesh Pro "Essential Resources" (TMP Settings + the default LiberationSans SDF
    /// font) WITHOUT opening the Editor.
    ///
    /// Normally this is a menu click — Window ▸ TextMeshPro ▸ Import TMP Essential Resources — and
    /// nothing that uses TMP_Text renders a single glyph until it has been done: TMP resolves its
    /// default font through TMP_Settings, which lives in those resources. Since the owner never opens
    /// Unity (the whole client is built and deployed headlessly), a required manual step would be a
    /// permanent tax on every fresh checkout, so this drives the same importer from batchmode.
    ///
    /// It tries the public menu item first and falls back to the internal importer type by reflection,
    /// because that type has moved between package versions. If BOTH fail it says so loudly and
    /// non-zero, rather than letting a build ship a UI with no font in it.
    /// </summary>
    public static class TmpSetup
    {
        private const string EssentialsMarker = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        public static void ImportEssentials()
        {
            if (File.Exists(EssentialsMarker))
            {
                Debug.Log("[tmp] essentials already present — nothing to do.");
                return;
            }

            // The importer TYPE first, not the menu item: the menu item only OPENS the importer
            // window and waits for a button click, so in batchmode it reports success while
            // importing nothing (observed 2026-07-21 — it returned true and the folder never
            // appeared). Internal, and it has lived in two different assemblies across versions, so
            // search rather than hard-code one.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("TMPro.TMP_PackageResourceImporter");
                if (type == null) continue;

                foreach (var method in type.GetMethods(
                             BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (method.Name != "ImportResources") continue;

                    var parameters = method.GetParameters();
                    var args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                        args[i] = parameters[i].ParameterType == typeof(bool)
                            ? (object)(i == 0)          // essentials yes, examples/extras no
                            : null;

                    Debug.Log("[tmp] invoking " + type.FullName + "." + method.Name
                              + " with " + parameters.Length + " arg(s).");
                    method.Invoke(null, args);
                    Finish("reflection on " + type.FullName);
                    return;
                }
            }

            Debug.LogError("[tmp] FAILED to import TMP essential resources — no menu item and no "
                         + "importer type. The UI will have no font. Import them by hand: "
                         + "Window > TextMeshPro > Import TMP Essential Resources.");
            EditorApplication.Exit(1);
        }

        private static void Finish(string how)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            bool ok = File.Exists(EssentialsMarker);
            Debug.Log("[tmp] import via " + how + " → " + (ok ? "OK" : "ran but marker missing"));
            if (!ok) EditorApplication.Exit(1);
        }
    }
}
