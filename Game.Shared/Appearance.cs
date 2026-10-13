using System;
using System.Collections.Generic;

namespace Game.Shared;

/// <summary>
/// `BL-354` (0.254.0) — how a character LOOKS: gender, hairstyle, hair colour and skin tone, picked at creation and
/// stored as four small numbers. Nothing here touches a stat. The client owns what each number draws
/// (<c>CharacterLook</c> in the Unity client); this file only fixes how many choices there are, their names for the
/// creation screen, and the rule that gives an NPC its look.
/// </summary>
public static class Appearance
{
    /// <summary>Owner, 2026-10-13: "5 hair stiles". Per gender — the meshes differ, the count does not.</summary>
    public const int HairStyles = 5;
    public static readonly string[] HairStyleNamesMale = { "Parted", "Long", "Buzzed", "Parted + beard", "Buzzed + beard" };
    public static readonly string[] HairStyleNamesFemale = { "Long", "Buns", "Parted", "Short", "Long + buns" };

    public static readonly string[] HairColorNames = { "Black", "Dark brown", "Brown", "Auburn", "Blonde", "Ash", "Grey", "Red" };
    public static int HairColors => HairColorNames.Length;

    /// <summary>Five tones per race, lightest first. The race shifts the whole range (an elf's lightest is lighter
    /// than a human's, a demon's run reddish-brown) — the client's table, by the same index.</summary>
    public static readonly string[] SkinToneNames = { "Pale", "Light", "Medium", "Tan", "Deep" };
    public static int SkinTones => SkinToneNames.Length;

    public static string[] HairStyleNames(Gender g) => g == Gender.Female ? HairStyleNamesFemale : HairStyleNamesMale;

    /// <summary>A creation request's numbers, forced into range — the client is not trusted with them.</summary>
    public static (Gender Gender, byte Hair, byte HairColor, byte Skin) Clamp(Gender gender, int hair, int hairColor, int skin) =>
        (gender == Gender.Female ? Gender.Female : Gender.Male,
         (byte)Math.Clamp(hair, 0, HairStyles - 1),
         (byte)Math.Clamp(hairColor, 0, HairColors - 1),
         (byte)Math.Clamp(skin, 0, SkinTones - 1));

    /// <summary>The town NPCs' given names that are women's (owner, 2026-10-13: "can atlast the npcs have a female").
    /// One explicit list rather than a guess off the spelling: "Wyn" and "Nim" read either way, and a name moved to
    /// another NPC keeps its body.</summary>
    private static readonly HashSet<string> FemaleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Brida", "Bryn", "Cera", "Edda", "Ilse", "Ilva", "Ingra", "Maela", "Maeve", "Miren", "Mora", "Nerys", "Nim",
        "Nyra", "Orla", "Rhosa", "Rilla", "Sela", "Sigrid", "Thessa", "Vess", "Ylva",
    };

    /// <summary>An NPC's gender, from the given name ("Elder Marius" → Marius).</summary>
    public static Gender NpcGender(string personalName) =>
        FemaleNames.Contains(personalName ?? "") ? Gender.Female : Gender.Male;

    /// <summary>An NPC's hair and skin: a stable pick off its name, so the same NPC looks the same on every phone and
    /// after every restart, and a row of NPCs does not look cloned. The hash is our own (string.GetHashCode is
    /// randomised per process on .NET Core).</summary>
    public static (byte Hair, byte HairColor, byte Skin) NpcLook(string personalName)
    {
        uint h = 2166136261;
        foreach (char c in personalName ?? "") h = (h ^ c) * 16777619;
        return ((byte)(h % HairStyles), (byte)((h / 7) % (uint)HairColors), (byte)((h / 61) % (uint)SkinTones));
    }
}
