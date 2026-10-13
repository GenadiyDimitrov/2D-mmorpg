using UnityEditor;
using UnityEngine;

namespace Game.Client.Editor
{
    /// <summary>
    /// `BL-45` — how a dropped-in sound is imported, so a new clip needs no clicking in the Editor.
    /// <list type="bullet">
    /// <item><c>Resources/Music/</c>: STREAMED from the APK (a 4-minute track decompressed into memory is ~40 MB), Vorbis at
    /// a quality that keeps the APK small. Stereo is kept.</item>
    /// <item><c>Resources/Sounds/</c>: short effects, MONO (they are panned by hand in <c>Sfx</c>), decompressed on load
    /// so a hit plays with no decode delay.</item>
    /// </list>
    /// Runs on import only: a clip already imported keeps its settings until it is reimported.
    /// </summary>
    public class AudioImport : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            var path = assetPath.Replace('\\', '/');
            var importer = (AudioImporter)assetImporter;
            var s = importer.defaultSampleSettings;
            if (path.Contains("/Resources/Music/"))
            {
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.35f;
                importer.forceToMono = false;
            }
            else if (path.Contains("/Resources/Sounds/"))
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
                importer.forceToMono = true;
            }
            else return;
            importer.defaultSampleSettings = s;
        }
    }
}
