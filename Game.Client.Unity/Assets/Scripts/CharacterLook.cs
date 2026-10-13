using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// `BL-354` — dresses a character model in its owner's look: the hairstyle, the hair colour, the skin tone and the
    /// race's build. The numbers come off <see cref="EntityDto"/> (Game.Shared.Appearance's indices); WHAT each one draws
    /// lives here, because it is presentation only.
    ///
    /// <para>🔑 Every hairstyle a body can wear is already in its prefab, hidden (ModelSetup bakes them in, rebound to
    /// that body's head bone), so a style is just which children are on. Colours go through a
    /// <see cref="MaterialPropertyBlock"/>, never a material copy: one shared material per pack piece keeps every
    /// character in the same draw batch, and the stealth fade's material swap (EntityView.ApplyModelFade) keeps the
    /// block, which lives on the renderer.</para>
    ///
    /// <para>The race builds are the owner's (2026-10-13): an elf 10% taller and slimmer, lighter skin, no different
    /// ears yet; a demon MAN 5% taller and 10% broader, a demon woman built like a human woman; demon skin "like a light
    /// brown or even reddish", not black. ⚠ The build is a scale on the model ROOT, so a raised arm shears a few percent
    /// — invisible at 10%, and the reason not to push these numbers much further without per-bone scaling.</para>
    /// </summary>
    public static class CharacterLook
    {
        /// <summary>Per gender, per style: the hair meshes (prefab child names) that are on. Same order as
        /// <see cref="Appearance.HairStyleNamesMale"/> / <see cref="Appearance.HairStyleNamesFemale"/>.</summary>
        private static readonly string[][] MaleHair =
        {
            new[] { "Hair_SimpleParted" }, new[] { "Hair_Long" }, new[] { "Hair_Buzzed" },
            new[] { "Hair_SimpleParted", "Hair_Beard" }, new[] { "Hair_Buzzed", "Hair_Beard" },
        };
        private static readonly string[][] FemaleHair =
        {
            new[] { "Hair_Long" }, new[] { "Hair_Buns" }, new[] { "Hair_SimpleParted" }, new[] { "Hair_BuzzedFemale" },
            new[] { "Hair_Long", "Hair_Buns" },
        };

        /// <summary>Multiplies the pack's grey hair mask. Same order as <see cref="Appearance.HairColorNames"/>.</summary>
        private static readonly Color[] HairColors =
        {
            new Color(0.10f, 0.09f, 0.08f), new Color(0.24f, 0.15f, 0.09f), new Color(0.42f, 0.27f, 0.16f),
            new Color(0.58f, 0.24f, 0.12f), new Color(1.05f, 0.85f, 0.52f), new Color(0.78f, 0.75f, 0.68f),
            new Color(0.92f, 0.92f, 0.92f), new Color(0.75f, 0.22f, 0.08f),
        };

        /// <summary>Multiplies the pack's (tan) skin map; above 1 lightens it. Pale → Deep, as Appearance names them.</summary>
        private static readonly Color[] HumanSkin =
        {
            new Color(1.22f, 1.14f, 1.08f), new Color(1.10f, 1.04f, 0.98f), Color.white,
            new Color(0.86f, 0.78f, 0.72f), new Color(0.64f, 0.56f, 0.50f),
        };

        /// <summary>An elf is a step lighter across the whole range.</summary>
        private static readonly Color ElfShift = new Color(1.12f, 1.10f, 1.08f);
        /// <summary>A demon's own five, light brown to red-brown (owner: "not like a black person just molad like a light
        /// brown or even redish"). Its own table, not the human one tinted: the human Deep tinted red came out near-black.</summary>
        private static readonly Color[] DemonSkin =
        {
            new Color(1.08f, 0.80f, 0.68f), new Color(1.02f, 0.72f, 0.60f), new Color(0.96f, 0.64f, 0.52f),
            new Color(0.90f, 0.57f, 0.46f), new Color(0.82f, 0.50f, 0.40f),
        };

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>Applies the look to a freshly instantiated model, BEFORE it is parented (the race build is its
        /// local scale). A model with no hair children and no skin materials (a rat) is left untouched.</summary>
        public static void Apply(GameObject model, EntityDto e)
        {
            if (model == null || e == null || e.Kind == EntityKind.Mob) return;
            Apply(model, e.Race, e.Gender, e.Hair, e.HairColor, e.Skin);
        }

        /// <summary>The same, from the bare picks (the editor's look preview has no DTO).</summary>
        public static void Apply(GameObject model, Race race, Gender gender, int hair, int hairColorIndex, int skinTone)
        {
            var styles = gender == Gender.Female ? FemaleHair : MaleHair;
            var on = styles[Mathf.Clamp(hair, 0, styles.Length - 1)];
            var hairColor = HairColors[Mathf.Clamp(hairColorIndex, 0, HairColors.Length - 1)];
            var skin = SkinTone(race, skinTone);

            var block = new MaterialPropertyBlock();
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name.StartsWith("Hair_"))
                    r.gameObject.SetActive(System.Array.IndexOf(on, r.name) >= 0);

                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string n = mats[i].name;
                    Color? tint = n.StartsWith("MI_Hair") ? hairColor                       // hair AND brows
                                : n.Contains("Regular_") || n.Contains("Superhero_") ? skin  // head + bare arms
                                : (Color?)null;
                    if (tint == null) continue;
                    r.GetPropertyBlock(block, i);
                    block.SetColor(BaseColorId, tint.Value);
                    block.SetColor(ColorId, tint.Value);
                    r.SetPropertyBlock(block, i);
                }
            }

            model.transform.localScale = Vector3.Scale(model.transform.localScale, Build(race, gender));
        }

        /// <summary>(width, height, depth) multipliers for a race's body.</summary>
        public static Vector3 Build(Race race, Gender gender)
        {
            switch (race)
            {
                case Race.Elf: return new Vector3(0.9f, 1.1f, 0.9f);
                case Race.Demon: return gender == Gender.Male ? new Vector3(1.1f, 1.05f, 1.1f) : Vector3.one;
                default: return Vector3.one;
            }
        }

        public static Color SkinTone(Race race, int tone)
        {
            int i = Mathf.Clamp(tone, 0, HumanSkin.Length - 1);
            var c = race == Race.Demon ? DemonSkin[i] : HumanSkin[i];
            if (race == Race.Elf) c *= ElfShift;
            c.a = 1f;
            return c;
        }
    }
}
