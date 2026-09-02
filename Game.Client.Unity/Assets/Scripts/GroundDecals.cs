using System.Collections.Generic;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// The circles on the ground: a totem's footprint, which STAYS while the totem does, and an area
    /// skill's footprint, which FLASHES once as the skill lands.
    ///
    /// <para>Both exist for one reason the owner gave plainly: *"I want to see where it stands and the
    /// AOE so I can stand inside"*. A totem was invisible for its whole life — it is a
    /// <c>TotemInstance</c> on the server, never an entity, so nothing was ever drawn and nothing was
    /// ever sent. Standing in it was guesswork.</para>
    ///
    /// <para>🔑 <b>A decal is drawn as its RADIUS, not as a marker.</b> The point is not "a totem is
    /// here", it is "this is the ground the totem pays" — so the disc is the real AoE, straight off the
    /// server's own radius through <see cref="WorldMapper"/>, and standing inside the colour is
    /// literally standing inside the effect.</para>
    ///
    /// <para>Colours are his: <b>green = HP, blue = mana</b>, and a totem that does both is drawn in
    /// both. The flashes reuse the same two so the meaning carries across.</para>
    ///
    /// <para>Built from squashed cylinders like <see cref="MoveMarker"/> and <see cref="ZoneOverlay"/>,
    /// but with <see cref="UnlitMaterials.CreateTransparent"/> — these are the one thing in the world
    /// you look THROUGH. Each keeps its own material instance because each animates its own alpha.</para>
    /// </summary>
    public class GroundDecals : MonoBehaviour
    {
        // His colours. Kept a touch desaturated: at full saturation a 900-unit disc under a top-down
        // camera reads as terrain rather than as an effect.
        public Color HealColour = new Color(0.30f, 0.85f, 0.35f);
        public Color ManaColour = new Color(0.30f, 0.55f, 1.00f);
        public Color BuffColour = new Color(0.95f, 0.85f, 0.35f);
        public Color HarmColour = new Color(0.90f, 0.30f, 0.25f);
        public Color ResurrectColour = new Color(0.95f, 0.95f, 0.75f);

        [Tooltip("Resting alpha of a totem's disc.")]
        public float TotemAlpha = 0.16f;
        [Tooltip("How far the totem's alpha swings either side of resting, per pulse.")]
        public float TotemPulseAlpha = 0.09f;
        [Tooltip("Seconds per totem pulse — cosmetic; the server's own pulse is its business.")]
        public float TotemPulseSeconds = 2f;

        [Tooltip("Alpha an area flash starts at before fading to nothing.")]
        public float FlashAlpha = 0.42f;
        [Tooltip("How long an area flash lasts. 'A brief moment', in his words.")]
        public float FlashSeconds = 0.55f;
        [Tooltip("A flash starts slightly inside its true radius and snaps out to it.")]
        public float FlashGrowFrom = 0.75f;

        // Height above the grid. The totem sits UNDER a flash so a heal landing on a totem still reads.
        private const float TotemHeight = 0.02f;
        private const float FlashHeight = 0.04f;

        private sealed class Decal
        {
            public Transform T;
            public Material M;
            public Color Base;
            public float Radius;      // Unity units
            public float BornAt;
        }

        private readonly Dictionary<System.Guid, List<Decal>> _totems = new();
        private readonly List<Decal> _flashes = new();

        // ---------------------------------------------------------------------------------------
        //  TOTEMS — the server sends the whole visible set whenever it changes.
        // ---------------------------------------------------------------------------------------

        /// <summary>Replace the drawn totems with exactly what the server says is visible.
        ///
        /// <para>Whole-list, not a diff, because that is what arrives — and it is what makes this
        /// self-healing: anything the server stopped listing (expired, moved, walked out of range) is
        /// destroyed here, so a missed message can never leave a circle burned into the ground.</para></summary>
        public void SetTotems(TotemList list)
        {
            var all = list?.Totems ?? System.Array.Empty<TotemDto>();
            var seen = new HashSet<System.Guid>();

            // 🔑 TOTEMS STACK ON ONE SPOT, and that is the normal case, not an edge one: every totem
            // is planted WHERE YOU STAND, so an demon who drops his Healing and Mana totems together has
            // two circles of identical radius at identical coordinates. Drawn naively the second one
            // covers the first completely and he sees one colour — the exact opposite of the point.
            // So co-located totems are NESTED, each ring a little inside the last, and the whole
            // group is still centred on the same spot he is standing on.
            var nest = new Dictionary<(int, int), int>();
            int NestIndex(TotemDto t)
            {
                // ~1 unit of tolerance. Two totems planted on "the same" spot are never bit-identical.
                var cell = (Mathf.RoundToInt(t.X), Mathf.RoundToInt(t.Y));
                nest.TryGetValue(cell, out int n);
                nest[cell] = n + 1;
                return n;
            }

            foreach (var t in all)
            {
                int depth = NestIndex(t);
                seen.Add(t.Id);
                if (_totems.ContainsKey(t.Id)) continue;   // already drawn; a totem never moves in place

                // Each nesting level takes 14% off, and never below half — the disc still has to read
                // as the area it pays, not as a decoration.
                float nestScale = Mathf.Max(0.5f, Mathf.Pow(0.86f, depth));
                float lift = depth * 0.004f;

                var discs = new List<Decal>(2);
                // A totem that fills BOTH pools gets both colours, the mana ring inside the heal ring
                // for the same reason — two rings read, one blended average does not.
                if (t.Heals)
                    discs.Add(MakeDisc(t.X, t.Y, t.Radius * nestScale, HealColour,
                                       TotemHeight + lift, "TotemHp"));
                if (t.Restores)
                    discs.Add(MakeDisc(t.X, t.Y, t.Radius * nestScale * (t.Heals ? 0.82f : 1f),
                                       ManaColour, TotemHeight + lift + 0.002f, "TotemMp"));
                // A totem with neither flag should not exist, but drawing nothing would hide the bug.
                if (discs.Count == 0)
                    discs.Add(MakeDisc(t.X, t.Y, t.Radius * nestScale, BuffColour,
                                       TotemHeight + lift, "Totem"));

                _totems[t.Id] = discs;
            }

            foreach (var id in new List<System.Guid>(_totems.Keys))
            {
                if (seen.Contains(id)) continue;
                foreach (var d in _totems[id]) if (d.T != null) Destroy(d.T.gameObject);
                _totems.Remove(id);
            }
        }

        // ---------------------------------------------------------------------------------------
        //  WHISPS (`BL-109`) — the server sends the whole visible set EVERY TICK, because unlike a
        //  totem a whisp is flying and the position is the message.
        // ---------------------------------------------------------------------------------------

        [Tooltip("Radius of a whisp orb, in SERVER units (scaled like everything else).")]
        public float WhispRadius = 22f;
        [Tooltip("How high a whisp floats above the ground, in Unity units.")]
        public float WhispHeight = 0.75f;
        [Tooltip("How fast a whisp orb chases the position the server last reported (per second).")]
        public float WhispChase = 12f;

        private sealed class Orb
        {
            public Transform T;
            public Vector3 Target;
        }

        private readonly Dictionary<string, Orb> _whisps = new();

        /// <summary>Draw the whisps the server says are in view.
        ///
        /// <para>🔑 KEYED BY (owner, summon skill), not by an id — because that pair IS a whisp's
        /// identity on the server: one whisp per summon skill per master, and re-summoning refreshes
        /// the one you have rather than making a second. Keying on anything else would make a
        /// refreshed whisp look like a new orb and leave the old one behind for a frame.</para>
        ///
        /// <para>⚠ The orb CHASES rather than teleports. The push is 10/s and the frame rate is not,
        /// so snapping to each new position is visibly steppy on something this small and this fast —
        /// the same reason entity views interpolate.</para></summary>
        public void SetWhisps(WhispList list)
        {
            var all = list?.Whisps ?? System.Array.Empty<WhispDto>();
            var seen = new HashSet<string>();

            foreach (var w in all)
            {
                string key = w.OwnerId.ToString() + "/" + w.SummonSkillId;
                seen.Add(key);

                var centre = WorldMapper.ToUnity(w.X, w.Y);
                var target = new Vector3(centre.x, WhispHeight, centre.z);

                if (!_whisps.TryGetValue(key, out var orb))
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.name = "Whisp";
                    go.transform.SetParent(transform, false);
                    var collider = go.GetComponent<Collider>();
                    if (collider != null) Destroy(collider);   // never eat a tap meant for the ground

                    float r = WhispRadius * WorldMapper.Scale;
                    go.transform.localScale = new Vector3(r * 2f, r * 2f, r * 2f);
                    go.transform.position = target;   // a NEW orb appears where it is, it does not fly in

                    var renderer = go.GetComponent<Renderer>();
                    var material = UnlitMaterials.CreateTransparent(WhispColour(w.SummonSkillId));
                    if (material != null) renderer.material = material;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;

                    _whisps[key] = orb = new Orb { T = go.transform };
                }
                orb.Target = target;
            }

            foreach (var key in new List<string>(_whisps.Keys))
            {
                if (seen.Contains(key)) continue;
                if (_whisps[key].T != null) Destroy(_whisps[key].T.gameObject);
                _whisps.Remove(key);
            }
        }

        /// <summary>A whisp's colour, from its summon skill. Deterministic and local — the server does
        /// not send one, and it should not have to: what a spirit looks like is presentation.
        ///
        /// <para>⚠ A PLACEHOLDER, and deliberately a legible one. These are six unlit orbs; the real
        /// article is art, and it belongs with `BL-93`/`BL-103` rather than here. What this owes the
        /// player today is only that his two whisps are told apart at a glance and that their POSITION
        /// is honest, because the position is the thing the server is simulating.</para></summary>
        private static Color WhispColour(string summonSkillId)
        {
            switch (summonSkillId)
            {
                case "tank_whisp_taunt":        return new Color(0.95f, 0.45f, 0.25f);   // angry orange
                case "tank_whisp_charm":        return new Color(0.90f, 0.45f, 0.85f);   // lure violet
                case "tank_whisp_bind":         return new Color(0.45f, 0.75f, 0.95f);   // holding ice
                case "tank_whisp_heal":         return new Color(0.35f, 0.90f, 0.45f);   // the heal green
                case "tank_whisp_armor_break":  return new Color(0.85f, 0.80f, 0.40f);   // dulled gold
                case "tank_whisp_weapon_break": return new Color(0.75f, 0.35f, 0.35f);   // blunted red
                default:                        return new Color(0.85f, 0.85f, 0.95f);   // an unknown spirit
            }
        }

        /// <summary>Drop every whisp orb — the twin of <see cref="ClearTotems"/>, called from the same
        /// places for the same reason.</summary>
        public void ClearWhisps()
        {
            foreach (var orb in _whisps.Values)
                if (orb.T != null) Destroy(orb.T.gameObject);
            _whisps.Clear();
        }

        /// <summary>Drop every totem circle — on logout, or on any resync where the old set is no
        /// longer known to be true.</summary>
        public void ClearTotems()
        {
            foreach (var discs in _totems.Values)
                foreach (var d in discs) if (d.T != null) Destroy(d.T.gameObject);
            _totems.Clear();
        }

        // ---------------------------------------------------------------------------------------
        //  AREA FLASHES — one shot, self-destructing.
        // ---------------------------------------------------------------------------------------

        /// <summary>Flash an area skill's footprint where it landed.</summary>
        public void Flash(AreaEffectEvent e)
        {
            if (e == null || e.Radius <= 0f) return;
            var d = MakeDisc(e.X, e.Y, e.Radius, ColourFor(e.Kind), FlashHeight, "AreaFlash");
            _flashes.Add(d);
        }

        private Color ColourFor(AreaEffectKind kind) => kind switch
        {
            AreaEffectKind.Heal => HealColour,
            AreaEffectKind.Mana => ManaColour,
            AreaEffectKind.Harm => HarmColour,
            AreaEffectKind.Resurrect => ResurrectColour,
            _ => BuffColour,
        };

        // ---------------------------------------------------------------------------------------

        private Decal MakeDisc(float serverX, float serverY, float serverRadius, Color colour,
                               float height, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(transform, false);

            // Same reason as the move marker: a decal must never eat a tap meant for the ground, or
            // standing in your own totem would stop you being able to walk out of it.
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            float radius = serverRadius * WorldMapper.Scale;
            var centre = WorldMapper.ToUnity(serverX, serverY);
            go.transform.position = new Vector3(centre.x, height, centre.z);
            // A Unity cylinder is 2 units across and 2 tall, so X/Z take the DIAMETER and Y stays flat.
            go.transform.localScale = new Vector3(radius * 2f, 0.004f, radius * 2f);

            var renderer = go.GetComponent<Renderer>();
            var material = UnlitMaterials.CreateTransparent(colour);
            if (material != null) renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return new Decal
            {
                T = go.transform,
                M = renderer.material,   // the INSTANCE, so animating alpha touches only this disc
                Base = colour,
                Radius = radius,
                BornAt = Time.time,
            };
        }

        private void Update()
        {
            // `BL-109` — whisp orbs chase the last position the server reported. Exponential, not
            // linear, so a whisp that has just been dragged across a teleport catches up fast and one
            // that is only trailing its master glides.
            if (_whisps.Count > 0)
            {
                float t = 1f - Mathf.Exp(-WhispChase * Time.deltaTime);
                foreach (var orb in _whisps.Values)
                    if (orb.T != null)
                        orb.T.position = Vector3.Lerp(orb.T.position, orb.Target, t);
            }

            // Totems breathe. Purely cosmetic and on the client's own clock — the server's pulse
            // interval is not on the wire, and tying a visual to it would put a timer on every totem
            // message for no gain. His words: *"if it can pulse semy transperant good"*.
            if (_totems.Count > 0 && TotemPulseSeconds > 0f)
            {
                float phase = Mathf.Sin(Time.time * (2f * Mathf.PI / TotemPulseSeconds));
                float alpha = Mathf.Max(0.02f, TotemAlpha + phase * TotemPulseAlpha);
                foreach (var discs in _totems.Values)
                    foreach (var d in discs)
                    {
                        if (d.M == null) continue;
                        var c = d.Base; c.a = alpha;
                        UnlitMaterials.SetColor(d.M, c);
                    }
            }

            // Flashes fade out and grow to their true radius, then delete themselves.
            for (int i = _flashes.Count - 1; i >= 0; i--)
            {
                var d = _flashes[i];
                if (d.T == null) { _flashes.RemoveAt(i); continue; }

                float t = FlashSeconds <= 0f ? 1f : Mathf.Clamp01((Time.time - d.BornAt) / FlashSeconds);
                if (t >= 1f)
                {
                    Destroy(d.T.gameObject);
                    _flashes.RemoveAt(i);
                    continue;
                }

                // Out fast, fade slower: the size is the message, the alpha is the goodbye.
                float grow = Mathf.Lerp(FlashGrowFrom, 1f, Mathf.Sqrt(t));
                float width = d.Radius * 2f * grow;
                d.T.localScale = new Vector3(width, 0.004f, width);

                if (d.M != null)
                {
                    var c = d.Base;
                    c.a = FlashAlpha * (1f - t) * (1f - t);
                    UnlitMaterials.SetColor(d.M, c);
                }
            }
        }
    }
}
