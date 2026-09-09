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
    /// <summary>Warrior 2H SWORD mastery — the Demon Harmonist's Warlock Weapon Mastery, sword instead
    /// of blunt, P.Atk ×1.25. His line: *"the same 2h mastery as demon harmonist only for sword and
    /// power increased with 20~30%"*.</summary>
    public const string WarSwordMastery = "war_sword_mastery";
    /// <summary>Warrior melee damage skill — Sound Smash's ladder at ×1.25, gated to a TWO-HANDED
    /// SWORD (the weapon his 2H mastery line commits the class to). His *"the same smash/shock skill
    /// as the demon harmonist increased in dmg with 20%~30%"*.</summary>
    public const string WarSunderingBlow = "war_sundering_blow";


    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  ARMOUR — both are RUNGS APPENDED to an existing 2nd-class ladder, never a new skill.
    //
    //  🔑 WHY APPEND RATHER THAN ADD A SKILL. An armour mastery's payload rides ArmorMasteryLevels,
    //     and the profile at the LEARNED rung supplies every weight at once. A second, separate
    //     mastery would either stack silently with the 2nd-class one (the archer would carry 15%
    //     crit-rate resistance twice) or, with `Replaces`, would delete the weights it does not
    //     re-state (a light-armour warrior would lose everything). Appending is the idiom the TANK
    //     already uses — one skill, 2nd-class rungs then 3rd-class rungs — and it is regression-proof
    //     by construction: each new profile RE-STATES the 2nd class's other weights.
    //  ⚠ APPEND ONLY. A rung inserted mid-ladder silently re-points every saved SkillLevel above it.
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>WARRIOR HEAVY MASTERY, rungs 6-20 — *"have the same heavy passive as tanks minus the
    /// crit dmg reduction"*. Verbatim <see cref="TankArmorPDef"/> / <see cref="TankArmorPDefPct"/> /
    /// <see cref="TankArmorMpReg"/> and the tank's −2 evasion, with <c>CritDmgResist</c> left at zero:
    /// that one column is the tank's alone, and it is the whole difference between the two kits.</summary>
    internal static SkillLevel[] WarriorArmorMasteryThirdRungs() =>
        BulwarkRungs(i => new SkillLevel(SpCost: BulwarkSp[i],
            Description: $"With heavy armor: +{TankArmorPDef[i]} P.Def, "
                       + $"×{1f + TankArmorPDefPct[i]:0.00} P.Def, +{TankArmorMpReg[i]:0.0} MP regen/s, "
                       + $"−2 evasion. (Light armor keeps its level-36 bonus.)"));

    /// <summary>The profiles for those rungs. ⚠ LIGHT is FROZEN at the 2nd class's top rung
    /// (<c>WarriorArmor(32, 9, hpRegen: 1.6f)</c>) rather than left blank — blank would mean a
    /// warrior who learns rung 6 in light armour loses the defence he had at rung 5. His ask was
    /// "the same HEAVY passive"; the light branch is carried forward untouched, not extended.</summary>
    internal static ArmorMasteryProfile[] WarriorArmorMasteryThirdProfiles() =>
        Enumerable.Range(0, BulwarkLevels.Length).Select(i => new ArmorMasteryProfile(
            Robe: default, None: default,
            Light: new StatMods(PDef: 32, MpRegenPct: 0.1f, HpRegen: 1.6f, Evasion: 9),
            Heavy: new StatMods(
                MpRegen: TankArmorMpReg[i],
                PDef: TankArmorPDef[i], PDefPct: TankArmorPDefPct[i],
                Evasion: -2))).ToArray();

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  THE SKILLS
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    private static SkillDef[] FighterKits3rdSkills()
    {
        var list = new List<SkillDef>();

        // ---- Warrior 2H Sword Mastery — Warlock Weapon Mastery's { 30 … 100 } at ×1.25, and its
        //      constant +3 accuracy. TWO-HANDED SWORD: a bare `AnySword` would also pass a one-hander,
        //      which is the tank's weapon, so the hands axis is not optional here. ----
        int[] warSwordAtk = new[] { 30, 40, 50, 60, 70, 80, 90, 100 }.Select(Up).ToArray();
        int[] kitMastSp = { 36_000, 64_000, 81_000, 120_000, 190_000, 320_000, 390_000, 880_000 };
        list.Add(new SkillDef(WarSwordMastery, "Two-Handed Sword Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. A TWO-HANDED sword strikes harder and truer in your hands. "
                       + "No effect one-handed, and none with any other weapon.",
            Levels: warSwordAtk.Select((a, i) => new SkillLevel(SpCost: kitMastSp[i],
                Description: $"Two-handed sword: +{a} P.Atk, +3 accuracy.")).ToArray(),
            WeaponMasteryLevels: warSwordAtk.Select(a => new WeaponMasteryProfile(
                Sword: new PassiveEffect(PhysAtk: a, Accuracy: 3),
                RequiredWeapon: WeaponType.AnySword,
                RequiredHands: WeaponHands.Two)).ToArray()));

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
