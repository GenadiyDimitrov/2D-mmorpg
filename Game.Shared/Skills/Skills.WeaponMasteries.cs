namespace Game.Shared;

/// <summary>DATA-DRIVEN weapon-mastery skills for the FIGHTER 2nd-class archetypes
/// (Tank/Warrior/Rogue/Archer). Each carries a per-equipped-weapon
/// <see cref="WeaponMasteryProfile"/>: holding the class's intended weapon grants a bonus
/// (its identity); any other weapon simply grants nothing (no penalty — unlike armor
/// weight). The effect reuses <see cref="PassiveEffect"/> and flows through the SAME
/// passive-application path in Entity.RecomputeDerived, just gated on the equipped
/// WeaponType. The weapon sibling of the armor masteries (Skills.Masteries.cs);
/// Increment 2 of [[weapon-armor-mastery-design]].
///
/// MAGES (Nuker/Healer) intentionally have NO weapon-type mastery: their identity comes
/// from armor mastery (robe, +light for healers / +heavy for buffers) plus the flat
/// pAtk/mAtk passive (Weapon/Spell Mastery). Weapon TYPE doesn't matter for casters.
///
/// NUMBERS ARE PLACEHOLDERS — tune during testing.</summary>
public static partial class SkillCatalog
{
    public const string TankWeaponMastery    = "tank_weapon_mastery";
    public const string WarriorWeaponMastery = "warrior_weapon_mastery";
    public const string RogueWeaponMastery   = "rogue_weapon_mastery";
    // (`archer_weapon_mastery` — deleted 2026-08-07, playtest-19 `0a`/G1. Orphaned by the
    //  archer→rogue merge: Rogue Weapon Mastery already carries the BOW rungs, so this was a
    //  second bow passive nobody could be granted. Don't re-add it.)

    /// <summary>A caster weapon-mastery level: the given <paramref name="bonus"/> (M/P.Atk,
    /// reuse, cast/regen) applies ONLY with the wizard's weapon — a SWORD or BLUNT (1H/2H).
    /// ANYTHING ELSE — bow, dual, or an EMPTY HAND — gets NO bonus and halves cast speed
    /// (CastSpeedPct -1 ⇒ ×2 cast time). Stacked with the robe mastery's non-robe cast ×0.5,
    /// a bare-handed unarmoured mage casts at ×0.25. "Not using your optimal gear = penalty."</summary>
    /// ⚠ 2026-08-07: the wrong-weapon PENALTY is gone from here (it was `CastSpeedPct: -1.0f` on
    /// dual/bow/other). Spellcaster Mastery owns every weapon penalty now — owner: *"no other weapon
    /// penalties, they come from spellcaster"* — and stacking a −100% cast on top of Spellcaster's
    /// ×0.5 was double-charging the same rule. A caster mastery is now purely "sword or blunt earns
    /// this bonus; anything else earns nothing", which is how every OTHER weapon mastery already reads.
    internal static WeaponMasteryProfile CasterMastery(PassiveEffect bonus) =>
        new(Sword: bonus, Blunt: bonus);

    /// <summary>The WARCHANTER's version of the same thing: BLUNT or BOW, never sword. Every one of his
    /// `buffer 3rd.csv` Spell Mastery rows reads *"With blunt/bow weapon"*, because his buffer's three
    /// races hold a blunt (Human, Demon) or a bow (Elf) and nothing else. It is a separate helper rather
    /// than a parameter so the difference is visible at every call site — a caster mastery that pays on
    /// a BOW is unusual, and it only works at all because Harmonist Bow Proficiency cancels the
    /// untrained-weapon penalty that would otherwise be eating half the same character's magic.</summary>
    internal static WeaponMasteryProfile BufferMastery(PassiveEffect bonus) =>
        new(Blunt: bonus, Bow: bonus);

    /// <summary>A two-handed sword/blunt profile carrying the same PassiveEffect for both
    /// (the warrior 2H mastery doesn't distinguish sword vs blunt), gated to TwoHand.
    /// ⚠ The sword-vs-blunt split between the two warrior DISCIPLINES (melee = 2H sword,
    /// AoE = 2H blunt) is a 3rd-class rule and does NOT belong here — at 2nd class the warrior is
    /// one class and takes either. See `BL-104`.</summary>
    private static WeaponMasteryProfile TwoHand(PassiveEffect pe) =>
        new(Sword: pe, Blunt: pe,
            RequiredWeapon: WeaponType.AnySword | WeaponType.AnyBlunt,
            RequiredHands: WeaponHands.Two);

    /// <summary>A ONE-handed sword/blunt profile — the tank's weapon (owner, 2026-08-29: *"a tank
    /// for now is mace/blade (1h sword/blunt), the shield is not a requirement, the shield has its
    /// own passive"*). The mirror image of <see cref="TwoHand"/>, and the first gate in the game that
    /// could not be written before <see cref="WeaponHands"/> existed.</summary>
    private static WeaponMasteryProfile OneHand(PassiveEffect pe) =>
        new(Sword: pe, Blunt: pe,
            RequiredWeapon: WeaponType.AnySword | WeaponType.AnyBlunt,
            RequiredHands: WeaponHands.One);

    /// <summary>A rogue Weapon Mastery level: shared crit/acc/atk-speed on both dual and bow,
    /// plus each weapon's own flat P.Atk, and +200 range for the bow. <paramref name="critRate"/>
    /// is a MULTIPLIER on the weapon's crit base (0.20 = ×1.20) — his one rogue crit passive.</summary>
    private static WeaponMasteryProfile RogueWM(float critDmg, int acc, float critRate, float atkSpd, int dualAtk, int bowAtk) =>
        new(Dual: new PassiveEffect(PhysAtkPct: 0.085f, CritDamageFlat: critDmg, Accuracy: acc, CritRate: critRate, AtkSpeedPct: atkSpd, PhysAtk: dualAtk),
            Bow:  new PassiveEffect(PhysAtkPct: 0.085f, CritDamageFlat: critDmg, Accuracy: acc, CritRate: critRate, AtkSpeedPct: atkSpd, PhysAtk: bowAtk, BowRange: 200f));

    private static SkillDef WeaponMasteryPassive(string id, string name, BaseClass cls,
        string desc, WeaponMasteryProfile profile) =>
        new(id, name, cls, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. " + desc,
            Levels: new[] { new SkillLevel(SpCost: 500) },
            WeaponMasteryLevels: new[] { profile });

    private static SkillDef[] WeaponMasterySkills() => new SkillDef[]
    {
        // Warrior — Two-Handed Mastery (CSV warrior 2nd): big P.Atk + crit damage with a
        // 2H sword/blunt, at the cost of some defence (p.def ×0.8) and evasion. 5 levels.
        new(WarriorWeaponMastery, "Two-Hand Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, Replaces: new[] { FighterWeaponMastery },
            Description: "Passive. Mastery of TWO-HANDED swords and blunts: much greater attack "
                       + "power and critical damage, but lower defence and evasion. No effect one-handed.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 3400),
                new SkillLevel(SpCost: 6400),
                new SkillLevel(SpCost: 12000),
                new SkillLevel(SpCost: 22000),
                new SkillLevel(SpCost: 40000),
            },
            WeaponMasteryLevels: new[]
            {
                // crit dmg is the CSV's FLAT +35/+48/+64/+84/+106 (attack added inside the crit),
                // not a multiplier — it used to be read as ×2.35 … ×3.06. See CritBlowAndDouble.md §3.
                //
                // ⚠ DefencePct is -0.10, NOT -0.20 (owner, playtest-19 M10): "I want a warrior in a
                // heavy not to have lower defence than a mage...it's not logical". At -20% a 2H
                // Champion in heavy armour sat UNDER a robed mage, and the trade only got better with
                // level anyway — attack climbs 0.30 → 0.50 while the penalty stayed flat.
                TwoHand(new PassiveEffect(PhysAtkPct: 0.30f, PhysAtk: 13, CritDamageFlat: 35f,  Accuracy: 3, Evasion: -3, DefencePct: -0.10f)),
                TwoHand(new PassiveEffect(PhysAtkPct: 0.50f, PhysAtk: 15, CritDamageFlat: 48f,  Accuracy: 6, Evasion: -3, DefencePct: -0.10f)),
                TwoHand(new PassiveEffect(PhysAtkPct: 0.50f, PhysAtk: 17, CritDamageFlat: 64f,  Accuracy: 6, Evasion: -3, DefencePct: -0.10f)),
                TwoHand(new PassiveEffect(PhysAtkPct: 0.50f, PhysAtk: 20, CritDamageFlat: 84f,  Accuracy: 6, Evasion: -3, DefencePct: -0.10f)),
                TwoHand(new PassiveEffect(PhysAtkPct: 0.50f, PhysAtk: 20, CritDamageFlat: 106f, Accuracy: 6, Evasion: -3, DefencePct: -0.10f)),
            }),

        // Rogue — Weapon Mastery (CSV rogue 2nd): DUAL and BOW both gain +8.5% P.Atk plus
        // shared crit-damage / accuracy / crit-rate / attack-speed; each also gets its own flat
        // P.Atk, and BOW gains +200 range. 5 levels. Replaces the base fighter weapon mastery.
        new(RogueWeaponMastery, "Weapon Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, Replaces: new[] { FighterWeaponMastery },
            Description: "Passive. Sharpens dual-wield and bow attacks: more attack power, "
                       + "critical damage, accuracy and (with a bow) greater range.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 3400),
                new SkillLevel(SpCost: 6400),
                new SkillLevel(SpCost: 12000),
                new SkillLevel(SpCost: 22000),
                new SkillLevel(SpCost: 40000),
            },
            WeaponMasteryLevels: new[]
            {
                // crit dmg = the CSV's FLAT +35/+64/+80/+140/+165. The @24 and @28 rungs used to be
                // SWAPPED (80 before 64) as well as read as a multiplier. See CritBlowAndDouble.md §3.
                // crit rate is ×1.20 on EVERY rung, i.e. from level 20 (playtest-19 M9): it used to
                // arrive as +10/+10 points at 32/36, which is exactly the "each blow lands with the
                // 64+% chance" spike he wanted gone AND the 9.2%-until-32 blow gate of §50h. One
                // multiplier, early, matching his ladder's single ×1.2 rogue passive — bows included.
                RogueWM(critDmg: 35f,  acc: 0, critRate: 0.20f, atkSpd: 0f,    dualAtk: 8,  bowAtk: 30),
                RogueWM(critDmg: 64f,  acc: 3, critRate: 0.20f, atkSpd: 0f,    dualAtk: 11, bowAtk: 42),
                RogueWM(critDmg: 80f,  acc: 3, critRate: 0.20f, atkSpd: 0f,    dualAtk: 14, bowAtk: 56),
                RogueWM(critDmg: 140f, acc: 3, critRate: 0.20f, atkSpd: 0f,    dualAtk: 17, bowAtk: 74),
                RogueWM(critDmg: 165f, acc: 3, critRate: 0.20f, atkSpd: 0.05f, dualAtk: 21, bowAtk: 96),
            }),

        // (Archer "Bow Mastery" DELETED 2026-08-07 with its id — the rogue mastery above carries
        //  the bow profile since the merge.)

        // Tank — Weapon Mastery (CSV tank 2nd): flat + 8.5% attack power with a ONE-HANDED sword or
        // blunt. 5 levels (@20/24/28/32/36). Replaces the base fighter weapon mastery.
        //
        // 🔑 IT WAS "ANY WEAPON" UNTIL 2026-08-29 — his ruling: *"a tank for now is mace/blade
        //    (1h sword/blunt)"*. "Any weapon" let a knight hold a greatsword and keep the whole
        //    passive, which is the warrior's identity taken for free; and it is the reason the hands
        //    gate exists at all, since `Sword|Blunt` alone could never exclude a maul.
        // ⚠ The SHIELD is deliberately NOT part of this gate — *"the shield is not a requirement,
        //    the shield has its own passive"* (Shield Mastery). A tank who drops the shield for a
        //    second one-hander keeps this; a tank who picks up a 2H loses it.
        // ⚠ Moved from Levels[].Passive to WeaponMasteryLevels — the SAME numbers, applied through
        //    the same RecomputeDerived path, but Levels[].Passive is unconditional by construction
        //    and so has nowhere to hang a weapon gate. Wrong weapon = no bonus, never a penalty.
        new(TankWeaponMastery, "Weapon Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, Replaces: new[] { FighterWeaponMastery },
            Description: "Passive. Increases physical attack power while wielding a ONE-HANDED "
                       + "sword or blunt. No effect with a two-handed weapon, a bow or daggers.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 3400),
                new SkillLevel(SpCost: 6400),
                new SkillLevel(SpCost: 12000),
                new SkillLevel(SpCost: 22000),
                new SkillLevel(SpCost: 40000),
            },
            WeaponMasteryLevels: new[]
            {
                OneHand(new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: 6)),
                OneHand(new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: 8)),
                OneHand(new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: 10)),
                OneHand(new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: 12)),
                OneHand(new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: 15)),
            }),
    };
}
