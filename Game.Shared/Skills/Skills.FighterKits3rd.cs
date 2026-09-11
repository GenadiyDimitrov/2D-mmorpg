using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>THE WARRIOR'S 3rd-CLASS DAMAGE KIT (`BL-185`, built 2026-09-06).
///
/// <para>🔴 <b>THE ARCHER HALF IS GONE, 2026-09-09 — HIS FILE LANDED.</b> This file used to carry
/// both derived kits; `archer 3rd.csv` is now authored end to end, so Archer Bow Mastery, Split
/// Volley, the cloned Bow Expertise and Killing Focus are deleted and Skills.Archer3rd.cs holds his
/// rows instead. The rogue's derived light-armour rungs went with them (Skills.Dual3rd.cs authors
/// those now). That is exactly what the paragraph below promised would happen: *"when
/// `warrior 3rd.csv` and the archer's file land, they win"*. THE WARRIOR IS STILL DERIVED and still
/// waiting for its file — everything left here is provisional.</para>
///
/// <para>⚠ Their four skill ids are gone from the catalog, which is normally forbidden (a learned id
/// persists in a save). It is safe HERE and only here: they were three days old, registered on the
/// three ranged disciplines alone, and nobody has played a build carrying them — the 0.116.0 APK went
/// out but no character on it was ever saved with one, since the levelling to reach 40 as a
/// Sharpshooter takes longer than the three days they existed. Do not read this as a precedent.</para>
///
/// <para>⚠ READ THIS BEFORE ADDING ANYTHING HERE. The 40+ purge in `ClassSkillTables.Third.cs` still
/// stands — no invented 3rd-class skill without his word. These exist because he gave that word
/// explicitly, and he gave it as a RECIPE rather than as a file: *"For archer take the elf harmonist
/// skills and bow passives ... Increase them with ~20% and you'll get the dmg part of the archer
/// kit"* / *"For fighter kit take demon harmonist skills and 2h wepon passives increase them by ~20%
/// and u get the dmg part of the fighter kit"*, 2026-09-06, alongside *"I'll try next week to finish
/// the csvs"*. So every number below is a SOURCE LADDER × a factor, never an invention — and when
/// `warrior 3rd.csv` and the archer's file land, they win and this becomes the thing that gets
/// corrected. Nothing here is authored, it is DERIVED, and each derivation names its source.</para>
///
/// <para>🔑 WHY THESE TWO CLASSES AT ALL. `--dmgmatrix` (see docs/balance/DamageVsIG.md) found the
/// archer hitting a buffed mage for 235 where the ELF HARMONIST — a buffer — hit the same target for
/// 495. The ranged rogue disciplines had exactly one 3rd-class skill between them (Signal Flare at
/// 60) and the warrior had only HP Boost: both DAMAGE kits were simply never authored, which is why
/// a buffer out-damaged a dedicated DD by two to one.</para>
///
/// <para>🔑 THE FACTOR IS ×1.25 — the midpoint of his "20~30%", applied once, to POWER and to FLAT
/// P.Atk only. It is <see cref="KitFactor"/> so a re-tune is one number.</para>
///
/// <para>⚠ HIS HP-BOOST ITEM WAS ALREADY BUILT and nothing here touches it. *"have the same hp boos
/// as harmonist just to +1000hp 1 or 2 more lvls of it"* — <c>RegisterHpBoost</c> has given the
/// warrior rungs 4-10 (400 → 1000 max HP) at levels 43/49/55/62/66/70/74 since it was written, where
/// the buffer stops at rung 7 (+700). The ask was already satisfied.</para>
/// </summary>
public static partial class SkillCatalog
{
    /// <summary>His "20~30%", taken at the midpoint. Multiplies POWER and FLAT P.Atk; it does NOT
    /// touch MP, SP, cast, reuse, range or hit count, which are copied from the source verbatim.</summary>
    private const float KitFactor = 1.25f;

    private static int Up(int v) => (int)MathF.Round(v * KitFactor);

    // ---- WARRIOR (Ravager / Warlord) ----
    // 🔴 `war_sword_mastery` AND THE ARMOUR RUNGS LEFT THIS FILE ON 2026-09-11 — his two warrior files
    //    landed, which is exactly the event the paragraph at the top of this file has been promising:
    //    *"when `warrior 3rd.csv` and the archer's file land, they win and this becomes the thing that
    //    gets corrected."* `warrior_sword_mastery` in Skills.Warrior3rd.cs is the derived sword
    //    mastery's SUCCESSOR (a rename plus his own fifteen-rung ladder, not a second skill), and
    //    WarriorArmorMasteryThirdRungs/Profiles are his numbers now — he authors a light branch that
    //    keeps growing and a heavy branch that is nothing like the tank's copy that stood here.
    //    ⚠ Deleting the id is safe HERE for the same narrow reason the archer's four were: it was five
    //      days old (0.116.0), registered on these two disciplines alone, and reaching 40 as a Ravager
    //      takes longer than it existed. Not a precedent.
    /// <summary>Warrior melee damage skill — Sound Smash's ladder at ×1.25, gated to a TWO-HANDED
    /// SWORD (the weapon his 2H mastery line commits the class to). His *"the same smash/shock skill
    /// as the demon harmonist increased in dmg with 20%~30%"*.
    /// 🔴 THE LAST SURVIVOR OF THE DERIVED KIT, and it survives for a stated reason: his 2026-09-11
    /// files carry the warrior's passives and buffs but *"are missing only teir dmg and control
    /// (active dmg) skills"*. This stands in until those rows land, then it goes the way of the
    /// sword mastery above. Do not author a second damage skill beside it in the meantime.</summary>
    public const string WarSunderingBlow = "war_sundering_blow";

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  THE SKILLS
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    private static SkillDef[] FighterKits3rdSkills()
    {
        var list = new List<SkillDef>();

        // ---- Warrior Sundering Blow — Sound Smash's thirteen rungs at ×1.25 power. Same MP, same SP,
        //      same 40 range, same 1s cast, same 3s reuse; the weapon is a 2H sword instead of a blunt.
        //      ⚠ It does NOT carry Sound Smash's `Replaces: [HolyStrike]` — that clause exists to
        //      retire the BUFFER's inherited cleric bolt and means nothing on a fighter. ----
        list.Add(new SkillDef(WarSunderingBlow, "Sundering Blow", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: SoundMp[0], CastTicks: 10, CooldownTicks: 30, Range: 40, Power: Up(SoundPower[0]),
            Category: SkillCategory.Physical,
            RequiredWeapon: WeaponType.AnySword, RequiredHands: WeaponHands.Two,
            Description: "A two-handed blow that splits armour and the man inside it.",
            Levels: Enumerable.Range(0, SoundPower.Length).Select(i => new SkillLevel(
                Power: Up(SoundPower[i]), MpCost: SoundMp[i], SpCost: BandSp13[i],
                Description: $"Strikes for power {Up(SoundPower[i])}.")).ToArray()));

        return list.ToArray();
    }
}
