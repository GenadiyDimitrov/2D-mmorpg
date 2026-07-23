using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// Paints the world's zones on the ground: one flat disc per spawn zone, coloured by its LEVEL
    /// band, plus a distinct colour for the safe zones (towns).
    ///
    /// The point is orientation. A uniform grey plane tells you nothing about where you are or what
    /// lives there — you find out by walking into something forty levels above you. Zone colour makes
    /// "what is where" readable at a glance, which is the whole reason for a top-down camera.
    ///
    /// Built once from <see cref="WorldMap"/>, which is shared data, so this can never disagree with
    /// where the server actually spawns things.
    /// </summary>
    public class ZoneOverlay : MonoBehaviour
    {
        [Tooltip("Level that counts as 'max' when picking a colour — the top of the gradient.")]
        public float TopLevel = 80f;

        public Color TownColour = new Color(0.30f, 0.55f, 0.95f);

        private void Start()
        {
            foreach (var zone in WorldMap.SpawnZones)
            {
                // A spawn zone that falls inside an authored FIELD region is drawn by that field's
                // coloured FILL instead (owner: colour the fields, not the circles). The rest of the map
                // has no field polygon yet, so its circles stay until those bands are authored.
                if (RegionMap.At(zone.X, zone.Y) is { Kind: RegionKind.Field }) continue;
                Disc(zone.X, zone.Y, zone.Radius, ColourForLevel(zone.MaxLevel), 0.01f,
                     "Zone " + zone.MinLevel + "-" + zone.MaxLevel);
            }

            // Towns last so they draw ON TOP of any spawn zone that overlaps them — a town you cannot
            // pick out is worse than no overlay at all.
            foreach (var town in WorldMap.SafeZones)
                Disc(town.X, town.Y, town.Radius, TownColour, 0.015f, "Town " + town.Name);
        }

        /// <summary>Green (low) → yellow → orange → red (high). Deliberately the same reading as the
        /// nameplate level colours, so a red patch of ground and a red name mean the same thing.</summary>
        private Color ColourForLevel(int level)
        {
            float t = Mathf.Clamp01(level / Mathf.Max(1f, TopLevel));
            return t < 0.5f
                ? Color.Lerp(new Color(0.25f, 0.55f, 0.25f), new Color(0.70f, 0.68f, 0.20f), t * 2f)
                : Color.Lerp(new Color(0.70f, 0.68f, 0.20f), new Color(0.65f, 0.20f, 0.20f), (t - 0.5f) * 2f);
        }

        private void Disc(float serverX, float serverY, float serverRadius, Color colour,
                          float height, string name)
        {
            // A squashed cylinder, like the move marker: a solid circle with no texture, no alpha and
            // nothing to sort against the entity spheres.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(transform, false);

            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);   // the ground under it must stay tappable

            float radius = serverRadius * WorldMapper.Scale;
            var centre = WorldMapper.ToUnity(serverX, serverY);
            go.transform.position = new Vector3(centre.x, height, centre.z);
            go.transform.localScale = new Vector3(radius * 2f, 0.005f, radius * 2f);

            var renderer = go.GetComponent<Renderer>();
            var material = UnlitMaterials.Create(colour);
            if (material != null) renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
