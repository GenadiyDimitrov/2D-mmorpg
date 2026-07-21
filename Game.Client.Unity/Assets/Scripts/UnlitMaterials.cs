using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Makes the flat, unlit, single-colour material every billboard and the ground grid use.
    ///
    /// This exists because <c>Shader.Find("Unlit/Color")</c> — the obvious call — returns NULL in a
    /// PLAYER build of a URP project: built-in-pipeline shaders are not shipped unless something in
    /// the build references them, and URP does not. In the Editor it resolves fine, so the bug is
    /// invisible until you are on the phone: every entity and the grid rendered as Unity's MAGENTA
    /// "missing shader" material (2026-07-21 playtest). Worse, the old code did
    /// <c>if (shader != null)</c> and silently kept the default material, so nothing said why.
    ///
    /// So: try the URP shader FIRST, fall back down a chain, and SHOUT if the chain runs out.
    /// The URP shader is also pinned in Always Included Shaders (ProjectSettings/GraphicsSettings.asset)
    /// so the build never strips it — <c>Shader.Find</c> can only find what shipped.
    /// </summary>
    public static class UnlitMaterials
    {
        private static readonly string[] Candidates =
        {
            "Universal Render Pipeline/Unlit",  // the one this project actually renders with
            "Unlit/Color",                      // built-in pipeline
            "Sprites/Default",                  // always shipped (sprite default material)
            "Hidden/Internal-Colored",          // last resort; always present
        };

        private static Shader _shader;
        private static bool _searched;

        /// <summary>The best available unlit shader, or null if this build shipped none of them.</summary>
        public static Shader Shader
        {
            get
            {
                if (_searched) return _shader;
                _searched = true;

                foreach (var name in Candidates)
                {
                    _shader = UnityEngine.Shader.Find(name);
                    if (_shader != null)
                    {
                        Debug.Log($"[UnlitMaterials] using shader '{name}'.");
                        return _shader;
                    }
                }

                Debug.LogError(
                    "[UnlitMaterials] NO unlit shader found in this build — everything will render " +
                    "MAGENTA. Add 'Universal Render Pipeline/Unlit' to Always Included Shaders.");
                return null;
            }
        }

        /// <summary>A new material of the given colour, or null if no shader shipped.</summary>
        public static Material Create(Color color)
        {
            var shader = Shader;
            if (shader == null) return null;
            var material = new Material(shader);
            SetColor(material, color);
            return material;
        }

        /// <summary>
        /// Sets the colour on whichever property the shader that actually loaded uses: URP's Unlit
        /// exposes <c>_BaseColor</c>, the built-in ones <c>_Color</c>. URP tags <c>_BaseColor</c> as
        /// [MainColor] so <c>material.color</c> usually routes there anyway, but "usually" is how the
        /// magenta got shipped — set both explicitly.
        /// </summary>
        public static void SetColor(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            material.color = color;
        }
    }
}
