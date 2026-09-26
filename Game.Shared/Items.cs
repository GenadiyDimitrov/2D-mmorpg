namespace Game.Shared;

public enum ItemGrade { F = 0, E = 1, B = 2, A = 3, S = 4 }

/// <summary>IG-style GRADE PENALTY (owner 2026-07-16, redesigned 2026-07-17).
///
/// The penalty is driven by the GAP between YOUR grade and the ITEM's grade — not by the item's grade
/// alone. Both sides sit on the same ladder of seven steps (<see cref="GradeLevels"/>): F=1, E=20, D=40,
/// C=52, B=61, A=76, S=80 — the very tiers <see cref="ItemCatalog.TierLetter"/> already names. So a level-1
/// character (step 0 = F) in E gear (step 1) is ONE step over and keeps x0.5; the same character in A gear
/// (step 5) is FIVE steps over and keeps x0.1. Level up and the gap closes on its own.
///
/// ⚠ The <see cref="ItemGrade"/> enum is NOT the ladder — it has no C/D and exists for pricing/sorting
/// only. <see cref="ItemDef.ItemLevel"/> is the real tier; hand-authored items that predate tiers have no
/// ItemLevel, so they fall back to <see cref="LegacyGradeLevel"/> and behave exactly as they did.
///
/// In normal play this NEVER fires: a level-40 character wears level-40 gear (gap 0 → x1). It exists to
/// stop a level-1 twink swinging A-grade.</summary>
public static class GradePenalty
{
    /// <summary>Character level at which each grade STEP becomes "yours" (F, E, D, C, B, A, S).
    ///
    /// <para>⚠ The S step (80) was missing until 0.57.0, which meant S-grade gear was the ONE tier with
    /// no grade gate: a level-76 character in a Soulcrystal set took no penalty at all, while its item
    /// details said "Requires level 80". <see cref="EnchantRules.GradeOf(int)"/> already cited this array
    /// as the source of "E 20, D 40, C 52, B 61, A 76, S 80" — it just wasn't true here yet.</para></summary>
    public static readonly int[] GradeLevels = { 1, 20, 40, 52, 61, 76, 80 };

    /// <summary>Display letter per step, so UI never re-derives the ladder. Parallel to
    /// <see cref="GradeLevels"/> — the two are indexed by the same step and must stay the same length.</summary>
    public static readonly string[] GradeNames = { "F", "E", "D", "C", "B", "A", "S" };

    /// <summary>What each GAP (steps over your grade) leaves of the affected stats. Index = gap, so
    /// index 0 (at or above your grade) is the no-op x1. Deliberately SHORTER than the ladder: with the
    /// S step the largest possible gap is 6 (a level-1 character in S gear), which clamps to the same
    /// x0.1 floor as gap 5 — five steps under is already inert, and a sixth rung would be theatre.</summary>
    private static readonly float[] GapFactors = { 1f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };

    /// <summary>The grade STEP (0..5) a character level sits at.</summary>
    public static int StepForLevel(int level)
    {
        int step = 0;
        for (int i = 0; i < GradeLevels.Length; i++)
            if (level >= GradeLevels[i]) step = i;
        return step;
    }

    /// <summary>Fallback grade level for the hand-authored items that carry no ItemLevel.</summary>
    private static int LegacyGradeLevel(ItemGrade g) => g switch
    {
        ItemGrade.E => 20,
        ItemGrade.B => 40,
        ItemGrade.A => 52,
        ItemGrade.S => 61,
        _ => 1,
    };

    /// <summary>The level an ITEM's grade sits at (its tier, or its legacy grade's level).</summary>
    public static int ItemGradeLevel(ItemDef def) =>
        def.ItemLevel > 0 ? def.ItemLevel : LegacyGradeLevel(def.Grade);

    /// <summary>How many grade steps this item is ABOVE the wearer (0 = no penalty).
    /// <paramref name="gradeLevelBonus"/> is the future "equip N levels early" perk: it lifts the
    /// character's effective level for grade purposes only, so the thresholds slide down for them.</summary>
    public static int Gap(ItemDef def, int level, int gradeLevelBonus = 0)
    {
        int itemStep = StepForLevel(ItemGradeLevel(def));
        int charStep = StepForLevel(level + gradeLevelBonus);
        return Math.Max(0, itemStep - charStep);
    }

    /// <summary>Multiplier this item's gap imposes (1 = none).</summary>
    public static float Factor(ItemDef def, int level, int gradeLevelBonus = 0) =>
        GapFactors[Math.Min(Gap(def, level, gradeLevelBonus), GapFactors.Length - 1)];

    /// <summary>The multiplier for an already-computed gap (1 = none).</summary>
    public static float FactorForGap(int gap) =>
        GapFactors[Math.Clamp(gap, 0, GapFactors.Length - 1)];

    /// <summary>Grade letter of an item, for UI ("A", "D", …).</summary>
    public static string GradeNameOf(ItemDef def) => GradeNames[StepForLevel(ItemGradeLevel(def))];
}

/// <summary>Item quality. The ladder is one item at six qualities (owner, 2026-07-29):
/// Common 45% / Uncommon 55% / Rare 70% / Epic 70% / Legendary 85% / Mythic 100% of the piece's full
/// stats. THE SPLIT IS AT 70%: Rare and Epic carry identical raw numbers, and Epic is where set
/// bonuses and rolled attributes switch on. Below Epic you are buying numbers; from Epic up you are
/// buying identity — which is what makes the ladder readable.
///
/// Mythic is APPENDED (5), never inserted: these values are persisted on every saved item.
/// (God = 99, the untouchable debug tier, was DELETED 2026-08-07 with the rest of the God layer —
/// playtest-19 `0b`, *"nothing that can't be acquired in game"*. 99 stays retired.)</summary>
public enum ItemRarity { Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, Mythic = 5 }

// Jewel = the magic-defence slot. Five DESIGNATED slots: 2 rings, 2 earrings, 1 necklace
// (see MaxOfJewelType). Equipping into a full pair displaces one rather than refusing —
// the choice is JewelStrength + "ties go to slot 1".
public enum EquipSlot { Weapon = 0, Armor = 1, Consumable = 2, Scroll = 3, QuestItem = 4, Shield = 5, Jewel = 6, Box = 7, Material = 8, Rune = 9 }

/// <summary>Jewel sub-type — limits how many can be worn: 2 Rings, 2 Earrings, 1 Necklace.</summary>
public enum JewelType { None = 0, Ring = 1, Earring = 2, Necklace = 3 }

/// <summary>Unified TOP-LEVEL item category, derived from EquipSlot (see ItemDef.Type).
/// One clean axis for grouping/filtering. A 2H weapon is MainHand AND occupies the
/// OffHand (no separate type — see ItemDef.OccupiesOffHand).</summary>
public enum ItemType { Other = 0, MainHand, OffHand, Armor, Jewel, Consumable, Scroll, Box, Quest, Material, Rune }

/// <summary>Unified SUB-TYPE across all items (see ItemDef.Subtype), derived from the
/// per-domain enums (WeaponType / ArmorSlot / JewelType / ScrollKind …). Lets you ask
/// "all Boots" or "all Sword main-hands" uniformly.</summary>
public enum ItemSubtype
{
    None = 0,
    // MainHand
    Sword, Blunt, Bow, Dual,
    // OffHand
    Shield,
    // Armor
    Helmet, Body, Gloves, Boots,
    // Jewel
    Ring, Earring, Necklace,
    // Consumable
    Potion, BuffPotion,
    // Scroll
    EnchantScroll, AttributeScroll,
    // misc
    Box, QuestToken, Material,
}

public enum ArmorWeight { None = 0, Heavy = 1, Light = 2, Robe = 3 }

/// <summary>Body-part slot for armor. A full set is one of each (Head/Body/Gloves/
/// Boots). Only BODY carries an ArmorWeight (Heavy/Light/Robe) and the bulk of the
/// defence + 2 rolled attributes; Head/Gloves/Boots are WEIGHTLESS accessories shared
/// across builds, each carrying a single slot-specific attribute (Head HP/MP regen,
/// Gloves atk/cast speed, Boots move speed/eva). None = not a body-armor piece.</summary>
public enum ArmorSlot { None = 0, Head = 1, Body = 2, Gloves = 3, Boots = 4 }

/// <summary>The REQUIREMENT side of <see cref="ArmorWeight"/> — a [Flags] MASK of the body weights a
/// skill or passive accepts, where an ITEM carries exactly one <see cref="ArmorWeight"/> value. The
/// same shape as <see cref="WeaponType"/>'s item-value-vs-requirement-mask split, and for the same
/// reason: `light|heavy` is one cell in his `WEIGHT` column and must be one field here.
///
/// 🔑 <see cref="Bare"/> IS ITS OWN MEMBER, not the absence of one. <c>ArmorWeight.None</c> means "no
/// body armour equipped", and that is a state a gate must be able to name — his rule for the warrior
/// and rogue masteries is *"turn off robe and naked"* (2026-08-29), so `light|heavy` has to exclude a
/// naked torso as deliberately as it excludes a robe. <see cref="None"/> = no requirement at all.</summary>
[Flags]
public enum ArmorWeights
{
    None = 0,
    Bare = 1, Robe = 2, Light = 4, Heavy = 8,
    Any = Bare | Robe | Light | Heavy,
}

/// <summary>The SHIELD half of an armour gate — the second, ORTHOGONAL axis, exactly as
/// <see cref="WeaponHands"/> is to <see cref="WeaponType"/> (owner, 2026-08-29: *"heavy/shield ==
/// heavy and shield required"*).
///
/// 🔑 IT COULD NOT BE ANOTHER WEIGHT. A shield is a different equip SLOT and coexists with every
/// weight, so folding it into <see cref="ArmorWeights"/> would make `heavy|shield` read as an OR —
/// paying a robed character with a buckler the very bonus he asked to confine to heavy. `|` cannot
/// say AND; a second axis after the `/` can. Same lesson, same shape, one day after the hands gate.
///
/// ⚠ <see cref="Forbidden"/> (`/noshield`) is the symmetric third value and the engine honours it,
/// but NOTHING in the catalog authors one yet — it exists so the axis has no hole, not because a
/// skill wanted it.</summary>
public enum ShieldGate { Any = 0, Required = 1, Forbidden = 2 }

/// <summary>The armour twin of <see cref="WeaponTypes"/>: one gate, one grammar, one place.
/// <see cref="Satisfies"/> is the whole rule; <see cref="Format"/> and
/// <see cref="TryParseRequirement"/> are inverses of each other and live here TOGETHER so the CSV
/// round trip cannot drift.
///
/// His grammar (`BL-107`, 2026-08-29), the `[set]/[axis]` shape `WEAPON` already uses:
/// <code>
///   WEIGHT = weight[|weight…][/shield]
///     (empty)        no requirement — anything, naked included
///     heavy          heavy body armour; shield irrelevant
///     light|heavy    light OR heavy; robe and a bare torso get nothing
///     /shield        a shield equipped, any armour
///     heavy/shield   heavy AND a shield
/// </code></summary>
public static class ArmorGate
{
    /// <summary>The one bit an equipped <see cref="ArmorWeight"/> occupies in a requirement mask.</summary>
    public static ArmorWeights Bit(this ArmorWeight worn) => worn switch
    {
        ArmorWeight.Robe  => ArmorWeights.Robe,
        ArmorWeight.Light => ArmorWeights.Light,
        ArmorWeight.Heavy => ArmorWeights.Heavy,
        _ => ArmorWeights.Bare,          // ArmorWeight.None = no body armour worn
    };

    /// <summary>Does this gear state satisfy a gate? Both axes must pass — the weights are an OR
    /// among themselves, the shield is an AND against them, which is precisely what his `/` means.
    /// An empty gate (<see cref="ArmorWeights.None"/> + <see cref="ShieldGate.Any"/>) passes always.</summary>
    public static bool Satisfies(ArmorWeight worn, bool hasShield,
                                 ArmorWeights required, ShieldGate shield = ShieldGate.Any)
    {
        if (required != ArmorWeights.None && (required & worn.Bit()) == 0) return false;
        return shield switch
        {
            ShieldGate.Required  => hasShield,
            ShieldGate.Forbidden => !hasShield,
            _ => true,
        };
    }

    /// <summary>Human-readable gate — "heavy armour and a shield" — for the cast-refused system
    /// message and the skill tooltip, which MUST agree (the same contract
    /// <see cref="WeaponTypes.Describe"/> carries on the weapon axis).</summary>
    public static string Describe(ArmorWeights required, ShieldGate shield = ShieldGate.Any)
    {
        var parts = new List<string>();
        if ((required & ArmorWeights.Robe) != 0) parts.Add("robe");
        if ((required & ArmorWeights.Light) != 0) parts.Add("light");
        if ((required & ArmorWeights.Heavy) != 0) parts.Add("heavy");
        bool bare = (required & ArmorWeights.Bare) != 0;

        string armour = parts.Count == 0
            ? (bare ? "no body armour" : "")
            : string.Join(" or ", parts) + " armour" + (bare ? ", or none" : "");
        string sh = shield switch
        {
            ShieldGate.Required  => "a shield",
            ShieldGate.Forbidden => "no shield",
            _ => "",
        };
        if (armour.Length == 0 && sh.Length == 0) return "any";
        if (armour.Length == 0) return sh;
        return sh.Length == 0 ? armour : armour + " and " + sh;
    }

    // -------------------------------------------------------------------------------------------
    //  THE `WEIGHT` CSV COLUMN — his grammar, 2026-08-29 (`BL-107`), the `WEAPON` shape reused.
    // -------------------------------------------------------------------------------------------

    /// <summary>The canonical CSV cell for a gate — the exact string
    /// <see cref="TryParseRequirement"/> reads back. Empty for "no requirement".</summary>
    public static string Format(ArmorWeights required, ShieldGate shield = ShieldGate.Any)
    {
        var parts = new List<string>();
        // ⚠ FIXED ORDER, heaviest first, so the generated column and a re-parsed hand-typed cell
        // compare as STRINGS. `--check` does an ordinal comparison; a set that formatted in author
        // order would report every reordered cell as a mismatch.
        if ((required & ArmorWeights.Heavy) != 0) parts.Add("heavy");
        if ((required & ArmorWeights.Light) != 0) parts.Add("light");
        if ((required & ArmorWeights.Robe) != 0) parts.Add("robe");
        if ((required & ArmorWeights.Bare) != 0) parts.Add("bare");
        string s = string.Join("|", parts);
        if (shield == ShieldGate.Required) s += "/shield";
        else if (shield == ShieldGate.Forbidden) s += "/noshield";
        return s;
    }

    /// <summary>Parse a `WEIGHT` cell. Returns false only when the cell names no weight AND no shield
    /// token — otherwise the gate is usable and any problem is reported through
    /// <paramref name="error"/>, the same contract <see cref="WeaponTypes.TryParseRequirement"/>
    /// keeps: a bad axis token is an error and the axis is DROPPED, the row keeps its weights.</summary>
    /// <param name="warning">His typo-warning case — a cell that names every weight, which is the
    /// same as naming none and is almost always a misunderstanding of the empty cell.</param>
    public static bool TryParseRequirement(string cell, out ArmorWeights required, out ShieldGate shield,
                                           out string? error, out string? warning)
    {
        required = ArmorWeights.None;
        shield = ShieldGate.Any;
        error = null;
        warning = null;

        string s = (cell ?? "").Trim().ToLowerInvariant();
        if (s.Length == 0) return true;                 // empty cell = no armour requirement

        string weights = s;
        int slash = s.IndexOf('/');
        if (slash >= 0)
        {
            weights = s[..slash];
            string a = s[(slash + 1)..].Trim();
            if (a == "shield") shield = ShieldGate.Required;
            else if (a == "noshield") shield = ShieldGate.Forbidden;
            else error = $"invalid axis '/{a}' — only /shield or /noshield; the axis is ignored";
        }

        foreach (string raw in weights.Split('|'))
        {
            string t = raw.Trim();
            if (t.Length == 0) continue;                // `/shield` alone, or a stray separator
            switch (t)
            {
                case "heavy": required |= ArmorWeights.Heavy; break;
                case "light": required |= ArmorWeights.Light; break;
                case "robe":  required |= ArmorWeights.Robe; break;
                // "naked" is his own word for it (*"turn off robe and naked"*); the CSV is the
                // authority, so the reader learns his spelling rather than the file learning ours.
                case "bare":
                case "none":
                case "naked": required |= ArmorWeights.Bare; break;
                default:      error ??= $"unknown armour weight '{t}'"; break;
            }
        }

        if (required == ArmorWeights.None && shield == ShieldGate.Any)
        {
            error ??= $"no armour weight in '{cell}'";
            return false;
        }
        if (required == ArmorWeights.Any)
            warning = $"'{s}' names every weight, which is the same as an EMPTY cell; likely a typo";
        return true;
    }
}

/// <summary>Broad weapon category. Drives which skills work and the base
/// attack range. All classes CAN equip any weapon; skills gate usefulness.</summary>
// Daggers ARE the Dual type (treated as dual-wield): lower per-hit, very fast,
// high crit, no shield. There is deliberately no separate Dagger value.
// A "Staff" is a TwoHandedBlunt; the "Staff" name is just an item noun. Blunt = higher
// accuracy, lower crit than bladed. HANDS + TYPE are encoded in this ONE [Flags] enum: an
// ITEM has exactly one value, while a skill's weapon REQUIREMENT is a mask (e.g. Strike =
// AnySword | AnyBlunt, Battle Presence = TwoHandedSword | TwoHandedBlunt), tested with a
// single bitwise-AND. Bow and Dual are inherently two-handed (no 1H variants).
[Flags]
public enum WeaponType
{
    None = 0,
    Sword = 1, Blunt = 2, Dual = 4, Bow = 8,
    TwoHandedSword = 16, TwoHandedBlunt = 32,
    // Convenience masks (for skill requirements + hands tests):
    AnySword  = Sword | TwoHandedSword,
    AnyBlunt  = Blunt | TwoHandedBlunt,
    OneHanded = Sword | Blunt,
    TwoHanded = TwoHandedSword | TwoHandedBlunt | Bow | Dual,
}

/// <summary>How many HANDS a skill/passive demands of the equipped weapon — the second, ORTHOGONAL
/// half of a weapon gate (owner, 2026-08-29: *"a skill/passive gates a type of weapon
/// (sword/blunt/bow/dual) and gates hands (1h/2h/any)"*).
///
/// 🔑 IT HAD TO BE A SEPARATE FIELD. Hands live inside <see cref="WeaponType"/> for an ITEM, but a
/// REQUIREMENT could only ever say "two-handed" — never "one-handed". A bare type means *any hands of
/// it* (playtest 28, see <see cref="WeaponTypes.Satisfies"/>), and <c>Sword|Blunt</c> IS that bare
/// pair, so a maul passed a mask meant to read "1H sword or blunt". There is no spare bit for a
/// <c>OneHandedSword</c> and renumbering the enum would move every item's type, so hands became their
/// own axis: the TYPE mask stays hands-agnostic and this says how many hands hold it.
///
/// ⚠ Bow and Dual are inherently two-handed, so a bow skill is authored <c>Bow</c> + <c>Any</c> and
/// never mentions hands — his own note, *"a bow shot requires a bow + any hands (always 2 so hands
/// are unnecessary)"*. Hands are checked LITERALLY against the equipped weapon, with no special case:
/// a bow therefore satisfies <c>Two</c> and fails <c>One</c>, which is simply true of a bow.</summary>
public enum WeaponHands { Any = 0, One = 1, Two = 2 }

/// <summary>Helpers over the merged WeaponType (hands + type in one enum).</summary>
public static class WeaponTypes
{
    /// <summary>True for a two-handed weapon (occupies the offhand → no shield).</summary>
    public static bool IsTwoHanded(this WeaponType w) => (w & WeaponType.TwoHanded) != 0;

    /// <summary>Fold a hands-specific type down to its BASE type (TwoHandedSword→Sword,
    /// TwoHandedBlunt→Blunt) so hands-agnostic stat tables (variance/speed/crit) still match.</summary>
    public static WeaponType Base(this WeaponType w) => w switch
    {
        WeaponType.TwoHandedSword => WeaponType.Sword,
        WeaponType.TwoHandedBlunt => WeaponType.Blunt,
        _ => w
    };

    /// <summary>Does <paramref name="equipped"/> satisfy a skill's <c>RequiredWeapon</c> mask?
    ///
    /// 🔑 A BARE TYPE MEANS "ANY HANDS OF IT" (owner, playtest 28: *"cannot use acoustic shock and
    /// sound smash with maul (2h blunt), only work with 1h .. Should work with the 4 weapons
    /// (maul, mace, wand, staff — all blunts), same goes for all other"*). The gate used to be a raw
    /// <c>(required &amp; equipped) != 0</c>, and <c>Blunt</c> and <c>TwoHandedBlunt</c> are two
    /// different BITS — so a skill authored "blunt" silently meant "one-handed blunt", and the
    /// Warchanter's own maul locked him out of his own damage skills.
    ///
    /// ⚠ THE FOLD IS CONDITIONAL, and that is the whole subtlety. Folding the equipped weapon down to
    /// its base unconditionally would also let a maul pass a genuinely two-hands-only requirement
    /// (Whirlwind, Crushing Blow, the 2H mastery) — <c>TwoHandedBlunt.Base()</c> is <c>Blunt</c>, which
    /// sits inside those masks' opposite. So: if the requirement NAMES a two-handed bit, it is asking
    /// about hands and is matched exactly; if it names only base types, hands are not its business and
    /// the equipped weapon is folded. Both authored shapes keep working with no row edited.</summary>
    ///
    /// <paramref name="hands"/> is the ORTHOGONAL second gate (2026-08-29) and is checked FIRST,
    /// literally, against the equipped weapon — see <see cref="WeaponHands"/> for why it could not be
    /// another bit in the mask. An EMPTY HAND satisfies neither One nor Two.
    public static bool Satisfies(this WeaponType equipped, WeaponType required,
                                 WeaponHands hands = WeaponHands.Any) =>
        required == WeaponType.None || (Resolve(required, hands) & equipped) != 0;

    /// <summary>Expand a requirement (base TYPES + a HANDS token) into the exact set of equippable
    /// weapon values that satisfies it. This is the whole gate in one function; <see cref="Satisfies"/>
    /// is a bitwise-AND against it.
    ///
    /// 🔑 HANDS APPLY PER TYPE, NOT TO THE EQUIPPED WEAPON (owner, 2026-08-29). His worked example is
    /// <c>sword|blunt|bow/1</c> = *"1 handed sword or 1 handed blunt **or bow**"*, and
    /// <c>duals/1</c> = *"also parse as duals as it don't care for hands"*. So the hands token narrows
    /// only the types that HAVE two hand counts — sword and blunt — and Bow and Dual, which are
    /// inherently two-handed and have no 1H variant, pass through it untouched.
    ///
    /// ⚠ THIS REPLACED A LITERAL CHECK AGAINST THE EQUIPPED WEAPON (built 2026-08-29, corrected the
    /// same day by his spec above). Under the old rule a bow FAILED <c>/1</c> — true of a bow, but not
    /// what an author writing <c>sword|blunt|bow/1</c> means. The rule now lives entirely in the
    /// requirement, and the equipped weapon is only ever tested for membership.
    ///
    /// ⚠ THE REQUIRED MASK MUST NAME BASE TYPES ONLY (Sword/Blunt/Bow/Dual, or the AnySword/AnyBlunt
    /// convenience pairs). Spelling hands INTO it — <c>TwoHandedBlunt</c>, or the <c>TwoHanded</c>
    /// mask — no longer means anything: it is folded to its base type and then re-expanded by
    /// <paramref name="hands"/>, so <c>TwoHandedBlunt</c> + <c>Any</c> would WIDEN to any blunt. Say
    /// hands in <see cref="WeaponHands"/>, always. Nothing in the catalog does otherwise.</summary>
    public static WeaponType Resolve(WeaponType required, WeaponHands hands = WeaponHands.Any)
    {
        WeaponType r = WeaponType.None;
        if ((required & WeaponType.AnySword) != 0)
            r |= hands switch
            {
                WeaponHands.One => WeaponType.Sword,
                WeaponHands.Two => WeaponType.TwoHandedSword,
                _ => WeaponType.AnySword
            };
        if ((required & WeaponType.AnyBlunt) != 0)
            r |= hands switch
            {
                WeaponHands.One => WeaponType.Blunt,
                WeaponHands.Two => WeaponType.TwoHandedBlunt,
                _ => WeaponType.AnyBlunt
            };
        // Inherently two-handed, no 1H variant to narrow to — the hands token does not touch them.
        if ((required & WeaponType.Bow) != 0) r |= WeaponType.Bow;
        if ((required & WeaponType.Dual) != 0) r |= WeaponType.Dual;
        return r;
    }

    /// <summary>True when <paramref name="required"/> contains at least one type the hands token can
    /// actually narrow (sword or blunt). A <c>/1</c> or <c>/2</c> written against bow/dual alone is
    /// harmless but meaningless — his *"just mark it as typo-warning"*.</summary>
    public static bool HandsAreMeaningful(WeaponType required) =>
        (required & (WeaponType.AnySword | WeaponType.AnyBlunt)) != 0;

    /// <summary>Human-readable weapon requirement — "one-handed sword or blunt, or bow" — for the
    /// cast-refused system message and the skill tooltip, which MUST agree. Replaces a raw
    /// <c>Enum.ToString()</c> that printed the convenience masks by their internal names
    /// ("anysword or anyblunt") and had no way to mention hands at all.
    /// ⚠ Bow and dual are listed AFTER a comma, never under the hands word, because the hands word
    /// does not apply to them — "one-handed sword or blunt or bow" would be a lie.</summary>
    public static string Describe(WeaponType required, WeaponHands hands = WeaponHands.Any)
    {
        var handed = new List<string>();
        if ((required & WeaponType.AnySword) != 0) handed.Add("sword");
        if ((required & WeaponType.AnyBlunt) != 0) handed.Add("blunt");
        var free = new List<string>();
        if ((required & WeaponType.Dual) != 0) free.Add("dual");
        if ((required & WeaponType.Bow) != 0) free.Add("bow");

        if (handed.Count == 0 && free.Count == 0) return "any";
        string prefix = hands switch
        {
            WeaponHands.One => "one-handed ",
            WeaponHands.Two => "two-handed ",
            _ => ""
        };
        if (handed.Count == 0) return string.Join(" or ", free);
        string s = prefix + string.Join(" or ", handed);
        return free.Count == 0 ? s : s + ", or " + string.Join(" or ", free);
    }

    // -------------------------------------------------------------------------------------------
    //  THE `WEAPON` CSV COLUMN — his grammar, 2026-08-29 (`BL-105`):
    //      weaponType1[|weaponType2|weaponType3][/hands]
    //  `sword|blunt|bow` · `sword|blunt|bow/1` · `duals` · `blunt/2` · empty = any weapon.
    //  Format and TryParse are inverses and live TOGETHER so the round trip cannot drift.
    // -------------------------------------------------------------------------------------------

    /// <summary>The canonical CSV cell for a requirement — the exact string
    /// <see cref="TryParseRequirement"/> reads back. Empty for "no requirement".
    /// ⚠ The hands suffix is omitted when it would not narrow anything (bow/dual only), so the
    /// generated column never writes the very typo the checker warns about.</summary>
    public static string Format(WeaponType required, WeaponHands hands = WeaponHands.Any)
    {
        if (required == WeaponType.None) return "";
        var parts = new List<string>();
        if ((required & WeaponType.AnySword) != 0) parts.Add("sword");
        if ((required & WeaponType.AnyBlunt) != 0) parts.Add("blunt");
        if ((required & WeaponType.Bow) != 0) parts.Add("bow");
        if ((required & WeaponType.Dual) != 0) parts.Add("duals");
        if (parts.Count == 0) return "";
        string s = string.Join("|", parts);
        if (hands != WeaponHands.Any && HandsAreMeaningful(required))
            s += hands == WeaponHands.One ? "/1" : "/2";
        return s;
    }

    /// <summary>Parse a `WEAPON` cell. Returns false only when the cell cannot be understood at all;
    /// an INVALID HANDS token is reported through <paramref name="error"/> and the hands are dropped
    /// to <see cref="WeaponHands.Any"/> — his rule: *"they are marked as errors and make the hands
    /// invalid"*, i.e. the row still names its types, it just loses the narrowing.</summary>
    /// <param name="error">Set when the cell is malformed: an unknown type word, or a hands token that
    /// is anything but `1` or `2` (`/`, `/3`, `/a`).</param>
    /// <param name="warning">Set for his TYPO-WARNING case — a hands token on a requirement no hand
    /// count can narrow (`duals/1`). Parsed as if the token were not there.</param>
    public static bool TryParseRequirement(string cell, out WeaponType required, out WeaponHands hands,
                                           out string? error, out string? warning)
    {
        required = WeaponType.None;
        hands = WeaponHands.Any;
        error = null;
        warning = null;

        string s = (cell ?? "").Trim().ToLowerInvariant();
        if (s.Length == 0) return true;                 // empty cell = no weapon requirement

        string types = s;
        int slash = s.IndexOf('/');
        if (slash >= 0)
        {
            types = s[..slash];
            string h = s[(slash + 1)..].Trim();
            if (h == "1") hands = WeaponHands.One;
            else if (h == "2") hands = WeaponHands.Two;
            else error = $"invalid hands '/{h}' — only /1 or /2 are hands; the hands are ignored";
        }

        foreach (string raw in types.Split('|'))
        {
            string t = raw.Trim();
            if (t.Length == 0) { error ??= "empty weapon type between '|' separators"; continue; }
            switch (t)
            {
                case "sword":  required |= WeaponType.AnySword; break;
                case "blunt":  required |= WeaponType.AnyBlunt; break;
                case "bow":    required |= WeaponType.Bow; break;
                // His spelling is the plural; the singular is accepted so a hand-typed row still reads.
                case "duals":
                case "dual":   required |= WeaponType.Dual; break;
                default:       error ??= $"unknown weapon type '{t}'"; break;
            }
        }

        if (required == WeaponType.None) { error ??= $"no weapon type in '{cell}'"; return false; }
        if (hands != WeaponHands.Any && !HandsAreMeaningful(required))
        {
            warning = $"'{s}' — {Format(required)} has no one/two-handed variants, so the hands token "
                    + "does nothing; likely a typo";
            hands = WeaponHands.Any;
        }
        return true;
    }
}

/// <summary>Enchant scroll TYPE — what a FAILURE costs (owner, playtest-17 D1). This is now only
/// HALF of a scroll's identity: the other half is <see cref="ItemDef.ScrollGrade"/>, the grade of
/// gear it may be spent on, which the scroll's RARITY signals. See <see cref="EnchantRules"/>.
///
/// Until 0.49.0 these three values WERE the whole ladder (named Common/Uncommon/Rare after the
/// rarities they shipped at) and every scroll worked on every item. The numbers are unchanged, so
/// the three original scroll ids keep their meaning where it matters — the type that breaks the item
/// is still 1 — but the names now say what they do.</summary>
public enum ScrollKind { None = 0, Normal = 1, Greater = 2, Safe = 3 }

/// <summary>Attribute scroll tier (0.45.0). Each kind serves ONE grade band and does ONE
/// thing — see <see cref="AttributeSystem.ActionOf"/> / <see cref="AttributeSystem.Accepts"/>:
///   D-C-B: Common = roll a type, Uncommon = re-roll the value, Rare = re-roll in the top half.
///   A:     Epic = roll a type, Legendary = re-roll in the top half.
///   S:     Mythic = roll a type at its maximum.
/// There is no attribute LOCK any more, and outside S no scroll guarantees the top value.
/// (Epic/Mythic are appended out of ladder order because the values are persisted.)</summary>
public enum AttrScrollKind { None = 0, Common = 1, Uncommon = 2, Rare = 3, Legendary = 4, Epic = 5, Mythic = 6 }

/// <summary>
/// An item template. The Id is a STABLE STRING KEY (e.g. "sword_e_rare") — it
/// is the item's permanent identity, stored in saves and referenced by loot
/// tables, the debug menu, etc. IDs are never renumbered; new items get new
/// keys. WeaponRange &gt; 0 marks ranged weapons (bows/staves).
/// </summary>
public record ItemDef(
    string Id,
    string Name,
    EquipSlot Slot,
    ItemGrade Grade,
    ItemRarity Rarity,
    ArmorWeight Weight = ArmorWeight.None,
    ArmorSlot ArmorSlot = ArmorSlot.None,
    WeaponType WeaponType = WeaponType.None,
    // A weapon carries ONE power number (AtkBonus) plus two CHANNEL FACTORS. The factors
    // multiply the FINISHED channel (base stat + level + gear), which is the whole point:
    // P.Atk and M.Atk are both built on the same shared base (AtkStat + level*2), so only a
    // MULTIPLIER can stop that base leaking into the channel a weapon isn't meant to serve.
    // A second authored number could never do it — a 2H sword's flat M.Atk is small, but the
    // shared base handed a sword-wielding buffer ~85% of a staff's magic damage for free.
    // Fighter weapon: power = its P.Atk, PAtkFactor 1.0, MAtkFactor ~0.6.
    // Mage weapon:    power = its M.Atk, MAtkFactor 1.0, PAtkFactor ~0.6 (P.Atk nerfed).
    int AtkBonus = 0,
    float PAtkFactor = 1f,
    float MAtkFactor = 1f,
    int MAtkBonus = 0,
    int DefBonus = 0,
    int HpBonus = 0,
    int MpBonus = 0,
    int EvaBonus = 0,
    float WeaponRange = 0,
    // ----- Shield stats (only when Slot == Shield) -----
    float BlockChance = 0f,        // flat chance to block (0..1); buffs/passives add
    float BlockReduction = 0f,     // fraction of damage removed on a block (0..1)
    float ShieldCritDefense = 0f,  // reduces attacker crit CHANCE (0..1)
    int ShieldEvasionPenalty = 0,  // lowers your evasion (the IG tradeoff)
    // ----- Consumables -----
    // A consumable does NOT implement an effect. It names a SKILL, and the skill does the work
    // (heal, HoT, buff, teleport) — "everything is a skill; only what GRANTS it differs".
    // The skill's CastTicks decides the feel: 0 = drink it (instant), > 0 = a channelled scroll.
    // The old bespoke heal fields (HealPercentPerSecond / InstantHealPercent /
    // PotionDurationTicks) are gone: a HoT potion is now just a buff, so it shows on the buff
    // bar and gets "stronger cancels weaker" from the skill's BuffKey + Rank.
    string UseSkillId = "",
    // The shared "one healing potion per N ticks" rule. This stays an ITEM property because it's
    // a rule about DRINKING, not about the effect — and it's what separates a heal potion (has
    // one) from a buff potion (doesn't). 0 = no shared cooldown.
    int PotionCooldownTicks = 0,
    // ----- PVE ONLY (owner, 2026-08-27, for the mana potions): *"I want they to be active only
    //       outside pve. (having mp pot On and then entering pvp it works until stop but the next
    //       one is forbidden)"*. It gates the DRINK, never the effect — a potion already running
    //       when a fight starts plays out its full window, and only the NEXT one is refused. So it
    //       is a rule about pressing the button, exactly like PotionCooldownTicks above, and it
    //       lives on the ITEM for the same reason: the skill it names may be perfectly castable by
    //       other means. The gate is FlagOf(player) != Innocent, i.e. the purple flag or PK karma.
    bool PveOnly = false,
    ScrollKind ScrollKind = ScrollKind.None,
    // ----- Enchant scroll GRADE BAND (None = not an enchant scroll). The second axis: ScrollKind
    //       says what a failure costs, this says WHICH grade of gear the scroll may be spent on.
    //       Deliberately its own field rather than a re-reading of Grade/ItemLevel — ItemLevel
    //       drives pricing and the grade PENALTY, and a scroll has no business in either. -----
    EnchantGrade ScrollGrade = EnchantGrade.None,
    // ----- Fixed (non-rolled) attributes, e.g. for the legendary one-off -----
    ItemAttribute[]? FixedAttributes = null,
    // ----- Jewel stat: magic defence. Jewels are the ONLY source of magic
    // defence beyond the level-based base (see StatCalculator.MagicDefence). -----
    int MDefBonus = 0,
    // ----- Attribute (re-roll) scroll tier (None = not an attribute scroll). -----
    AttrScrollKind AttrScroll = AttrScrollKind.None,
    // ----- Armor SET id ("" = not a set piece). Wearing all 4 slots of one set
    // grants its set bonus (see ArmorSetCatalog). -----
    string SetId = "",
    // ----- Gold value (vendor pricing). 0 = filled from DefaultValue at build time;
    //       quest items and god-tier one-offs stay 0 = not buyable/sellable. Pass an
    //       explicit Value to override the formula for a specific item. -----
    int Value = 0,
    // ----- Trade/price control (every item carries these three) -----
    //  Tradable=false: can't be sold to a vendor or traded to players (only DELETED).
    //  BuyPriceOverride:  null = use the Value formula; -1 = cannot be purchased; 0 = free.
    //  SellPriceOverride: null = use the Value formula; 0 = sells for nothing.
    bool Tradable = true,
    int? BuyPriceOverride = null,
    int? SellPriceOverride = null,
    // ----- PLATINUM (`BL-257`, owner 2026-09-16) -----------------------------------------------
    //  *"items Def on their buy price also must have a platinum value (Default 0) … any item that have
    //   a platinum or/and gold must be bought with the value."*
    //
    //  🔑 A SECOND PRICE, NOT A SECOND CURRENCY FIELD ON THE SAME PRICE. 0 = this item costs no
    //     platinum, which is every item in the game today. An item may be priced in gold ALONE (the
    //     normal shelf), in platinum ALONE (set `BuyPriceOverride: -1` beside it, the shape the premium
    //     rune boxes already have), or in BOTH — and when both are set, BOTH are charged.
    //  ⚠ It is NOT scaled by rarity, by the vendor tax or by the equipment floor. Those all exist to
    //    keep a DERIVED gold price sane; a platinum price is always authored by hand, one number, and
    //    a formula quietly moving it is the last thing a premium price wants.
    //  ⚠ There is no platinum SELL price and there will not be one by accident: selling returns gold,
    //    so a premium item that must not be laundered into gold carries `SellPriceOverride: 0`, which
    //    is what every premium item already does.
    int PlatinumPrice = 0,
    // NoAttributes=true: never rolls a random attribute and can't be given one
    // (newbie/starter gear). Enforced in AttributeSystem.Roll.
    bool NoAttributes = false,
    /// <summary>ASK BEFORE USING IT (owner, 2026-08-26). A consumable with this set makes the client
    /// put an "are you sure?" between the Use button and the drink — from the details window AND from a
    /// skill-bar slot, because a one-tap bar is exactly where an expensive mistake happens.
    /// <para>Default FALSE: a healing potion must stay one tap. True today only for the SP Bottle,
    /// which is worth 100kk and 1kkk SP and cannot be un-drunk.</para>
    /// <para>⚠ CLIENT-SIDE ONLY, and deliberately so — it is a courtesy, not a rule. The server has
    /// nothing to enforce here: drinking is always legal, the confirmation only slows your hand.</para></summary>
    bool ConfirmOnUse = false,
    // SoulBound=true: this DEF may not be stored ANYWHERE — not the private keeper, not the account
    // one — on top of whatever Tradable says. Authored for the Rune of Sinners: *"Keeper cannot accept
    // this item ... as its bound to your soul for the time it has left."* Untradable alone was not
    // enough, because the PRIVATE keeper takes anything by default (it is just a bigger bag), which
    // would have let the punished player park the rune until it expired. Runes are already
    // delete-protected, so with this the rune has nowhere to go but with you.
    bool SoulBound = false,
    // Jewel sub-type (only when Slot == Jewel) — gates how many can be worn.
    JewelType JewelType = JewelType.None,
    // Gear TIER by character level (0 = legacy grade/rarity gear). The level tiers
    // (20/40/52/61/76 ≈ E/D/C/B/A) drive the number + max of rolled weapon attributes.
    int ItemLevel = 0,
    // Caster weapon flag: a wand/staff is a Blunt/TwoHandedBlunt type but rolls the CASTER
    // attribute pool (cast/M.Atk/magic-crit) instead of the fighter blunt pool. Owner's pick —
    // a simple flag that doesn't touch the WeaponType logic.
    bool IsMagicWeapon = false,
    // Per-item basic-attack speed base (333 = normal; higher = faster). 0 = use the weapon
    // type default (StatCalculator.WeaponAttackBaseSpeed). Lets two bows differ (slow vs very slow).
    int AttackSpeedBase = 0,
    // Recipe BOOK: the recipe id this item teaches when "opened" ("" = not a book). A book is an
    // EquipSlot.Box so the client's open flow reuses; opening adds the id to the char's KnownRecipes.
    string TeachesRecipeId = "",
    // ----- RECIPE % (`BL-273` part 2): the success % a gear recipe item carries (20/40/60/100), 0 = not
    // a gear recipe. Learning it fills a slot at this %; crafting spends one and rolls at this %. -----
    int RecipePercent = 0,
    // ----- RUNE (soul/spell rune). A held, non-equipped item that grants a timed buff while it's in the
    // MAIN inventory and not expired (wall-clock ExpiresAtUtc lives on the item INSTANCE, not here). The
    // buff is the named skill. Delete-protected (see the bin handler). Not equipped, not consumed. -----
    bool IsRune = false,
    string RuneBuffSkillId = "",
    // Which LEVEL of RuneBuffSkillId this rune grants — the rung of a reward-rune ladder (a
    // Rune of Experience (20%) is level 3 of `rune_exp`). One skill, many items: that is what lets
    // a stronger rung evict a weaker one instead of stacking with it. 1 for every other rune.
    int RuneBuffLevel = 1,
    // ----- BOX that grants a RUNE: seconds the granted rune lasts, stamped as ExpiresAtUtc at OPEN time
    // (so buying the sealed box doesn't start the clock). 0 = not a rune box. -----
    int GrantsRuneSeconds = 0,
    // ----- TIMED item: seconds this item survives from the moment it is ACQUIRED (0 = it never
    // expires). Stamped as ExpiresAtUtc on the INSTANCE by AddItem, so the clock is wall-clock and
    // runs while offline, and the expiry sweep deletes it wherever it sits (bag, warehouse, account
    // bank) — even while it is worn. This is the generic version of GrantsRuneSeconds, which stays
    // because a rune BOX overrides its rune's default duration at open time; a timed item has no
    // such second source. Used by the bound Newbie loaner kit (30 days). -----
    int LifetimeSeconds = 0,
    // ----- WORN-TIME item (`BL-272` part 2, 0.202.0): seconds this item lasts while EQUIPPED (0 = none).
    // The temporary 2-hour Common gear from the vendor boxes: *"the 2 hours tick only while WORN. Two
    // hours of active farming uses it up; unequipping or logging off pauses it."* Copied onto the
    // INSTANCE as WornSecondsLeft by AddItem; the per-second tick spends it only while the piece is worn,
    // and at 0 the piece is deleted. NOT a wall clock, so unlike LifetimeSeconds it never runs in a bag,
    // a warehouse or while offline. -----
    int WornLifetimeSeconds = 0,
    // ----- MAX STACK OVERRIDE (0 = derive it from the CATEGORY, which is what nearly everything does).
    // Only set this when one item has to disagree with its whole category. See ItemDef.MaxStack and
    // StackLimits: the numbers live in ONE place so a retune is one edit, never a sweep of authored
    // rows. -----
    int MaxStackOverride = 0,
    // ----- Optional blunt flavour/warning text shown in item details ("" = none; consumables fall back to
    // their use-skill's description). Used to spell out e.g. "War Runes boost PHYSICAL damage only". -----
    string Description = "")
{
    /// <summary>Hash on the ID alone. Same reason as SkillDef.GetHashCode — a positional record's
    /// generated hash is one deeply-nested expression per field, and IL2CPP turns that into C++ that
    /// can exceed clang's 256-bracket limit and break the Android build. Item DefIds are unique, so
    /// the id is the identity. Keep this override.</summary>
    public override int GetHashCode() => Id?.GetHashCode() ?? 0;

    /// <summary>`BL-301` — the name the SKILL/BUFF BAR labels this item by, when it is not
    /// <see cref="Name"/> ("" = the name). His rule: the qualifier words came OUT of item names
    /// (*"by the color of the item and the quality/rarity u can understand what is it"*), and *"Only the
    /// abbreviation for the skill/buff bar can differ"*. So a Lesser and a plain Swift Potion now share a
    /// name but keep two labels, and a buff potion's label still matches the buff it lands (its wrapper
    /// skill kept the old name). Read it through <see cref="Abbreviations.ForItem"/>, never directly.</summary>
    public string BarName { get; init; } = "";

    /// <summary>Does this item merge into a single inventory ROW with a quantity, rather than
    /// occupying one row per unit? Consumables, scrolls and crafting materials stack.
    ///
    /// This lives HERE, on the shared def, because it is asked on BOTH sides and the two copies
    /// drifted: the server's AddItem stacked Material while the client's vendor did not, so a
    /// stack of 11 gems showed as "x11" but sold one at a time with no quantity numpad. Anything
    /// that needs the answer asks this property — never re-list the slots.
    ///
    /// <para>Quest items stack too, and must: a GATHERING quest hands out one token per kill, so an
    /// hour of farming is 200+ of them. One row per token would fill the bag in twenty minutes and
    /// make the feature unusable. The class-change proofs are quest items as well and are unaffected —
    /// a chain grants exactly one of each, so a stack of one is the row it always was.</para></summary>
    /// <para>BLUEPRINTS stack, and they are the one <see cref="EquipSlot.Box"/> that does (owner,
    /// 2026-08-13: *"The blueprints need to be stackable not like a box"*). A loot box is a thing you
    /// open once and it is gone; a blueprint is CURRENCY — one to learn the recipe and one consumed by
    /// every craft, so an A-grade smith carries a pile of identical ones and at the top rungs a
    /// successful item takes several. One row each would bury the bag in the same way a gathering
    /// quest's tokens would. Both consumers already decrement a quantity rather than dropping the row
    /// (<c>HandleLearnRecipe</c>, <c>ConsumeItem</c>), so nothing else has to change.</para></summary>
    /// <para>BOXES stack too since 0.93.0 (owner: *"buff and other boxes 99"*). A box carries no
    /// per-instance state — the rune box stamps its clock on the RUNE at OPEN time, not on itself —
    /// and <c>HandleOpenBox</c> already decrements a quantity rather than dropping the row, which is
    /// what makes this safe. Blueprints were the one box that stacked; now the rule is uniform and
    /// the special case is gone.</para>
    ///
    /// <para>⚠ THE ONE THING THAT CAN NEVER STACK IS PER-INSTANCE STATE. Two items only merge when
    /// they are genuinely interchangeable, so anything carrying its own clock (a rune, a timed
    /// loaner) is excluded here no matter what slot it sits in — otherwise a merge would hand two
    /// acquisitions one expiry and silently extend or destroy one of them.</para></summary>
    public bool IsStackable => (Slot is EquipSlot.Consumable or EquipSlot.Scroll or EquipSlot.Material
                                     or EquipSlot.QuestItem or EquipSlot.Box
                                || TeachesRecipeId.Length > 0)
                               && !IsRune && LifetimeSeconds == 0 && WornLifetimeSeconds == 0;

    /// <summary>How many of this item fit in ONE inventory row. The (cap+1)-th opens a new row; it is
    /// never destroyed and never refused for being over a cap (owner, 0.93.0: *"the 10,100,1000 etc
    /// item to make new stack"*).
    ///
    /// <para>🔑 THE NUMBER IS DERIVED FROM THE CATEGORY, NOT AUTHORED PER ITEM. Every cap in the game
    /// is one of the constants in <see cref="StackLimits"/>, so changing what a buff scroll stacks to
    /// is ONE edit — his standing requirement for this feature: *"make those so we have the system and
    /// not need to retink it if we change a number"*. <see cref="MaxStackOverride"/> exists for the
    /// item that has to disagree with its category, and nothing uses it yet.</para>
    ///
    /// <para>⚠ It applies EVERYWHERE — bag, private warehouse, account warehouse, trade, drops, craft
    /// output, quest rewards, buy-back and the death-restore list — because a cap that one container
    /// ignores is a laundering route around it, not a cap.</para></summary>
    public int MaxStack => !IsStackable ? 1
                         : MaxStackOverride > 0 ? MaxStackOverride
                         : StackLimits.For(this);

    /// <summary>Unified top-level category (derived from EquipSlot). Weapons are MainHand,
    /// shields OffHand; everything else maps 1:1.</summary>
    public ItemType Type => Slot switch
    {
        EquipSlot.Weapon => ItemType.MainHand,
        EquipSlot.Shield => ItemType.OffHand,
        EquipSlot.Armor => ItemType.Armor,
        EquipSlot.Jewel => ItemType.Jewel,
        EquipSlot.Consumable => ItemType.Consumable,
        EquipSlot.Scroll => ItemType.Scroll,
        EquipSlot.Box => ItemType.Box,
        EquipSlot.QuestItem => ItemType.Quest,
        EquipSlot.Material => ItemType.Material,
        EquipSlot.Rune => ItemType.Rune,
        _ => ItemType.Other
    };

    /// <summary>Unified sub-type (derived from the per-domain enums).</summary>
    public ItemSubtype Subtype => Slot switch
    {
        EquipSlot.Weapon => WeaponType.Base() switch
        {
            WeaponType.Sword => ItemSubtype.Sword,
            WeaponType.Blunt => ItemSubtype.Blunt,
            WeaponType.Bow => ItemSubtype.Bow,
            WeaponType.Dual => ItemSubtype.Dual,
            _ => ItemSubtype.None
        },
        EquipSlot.Shield => ItemSubtype.Shield,
        EquipSlot.Armor => ArmorSlot switch
        {
            ArmorSlot.Head => ItemSubtype.Helmet,
            ArmorSlot.Body => ItemSubtype.Body,
            ArmorSlot.Gloves => ItemSubtype.Gloves,
            ArmorSlot.Boots => ItemSubtype.Boots,
            _ => ItemSubtype.None
        },
        EquipSlot.Jewel => JewelType switch
        {
            JewelType.Ring => ItemSubtype.Ring,
            JewelType.Earring => ItemSubtype.Earring,
            JewelType.Necklace => ItemSubtype.Necklace,
            _ => ItemSubtype.None
        },
        // A heal potion is the one with the shared drink cooldown; anything else consumable
        // that grants an effect is a buff potion (scrolls carry a ScrollKind and fall through).
        EquipSlot.Consumable => PotionCooldownTicks > 0 ? ItemSubtype.Potion : ItemSubtype.BuffPotion,
        EquipSlot.Scroll => AttrScroll != AttrScrollKind.None ? ItemSubtype.AttributeScroll : ItemSubtype.EnchantScroll,
        EquipSlot.Box => ItemSubtype.Box,
        EquipSlot.QuestItem => ItemSubtype.QuestToken,
        EquipSlot.Material => ItemSubtype.Material,
        _ => ItemSubtype.None
    };

    /// <summary>True if this is a two-handed MAIN-HAND weapon — it also claims the
    /// OffHand slot (so a shield can't be worn with it; enforced in HandleEquip).</summary>
    public bool OccupiesOffHand => Slot == EquipSlot.Weapon && WeaponType.IsTwoHanded();
}

/// <summary>EVERY stack cap in the game, in one table (owner, 0.93.0). <see cref="ItemDef.MaxStack"/>
/// classifies an item into one of these and nothing else decides a cap, so a retune is a single edit
/// here — his requirement when he ordered the feature: *"make those so we have the system and not need
/// to retink it if we change a number"*.
///
/// <para>🔑 WHY THE NUMBERS ARE WHAT THEY ARE. A stack cap prices a consumable in BAG ROWS, and a row
/// is only a real cost for something you carry a long time without spending. That is why they are not
/// one number:</para>
/// <list type="bullet">
///   <item><b>Buff scrolls (9)</b> — the only cap that is a real mechanic. 17 blessings, one hour each,
///   so a fully-buffed player burns 17 scrolls an hour and their row count stays FLAT while they farm.
///   At 9 a stack, an hour of full buffs is ~2 rows and a long session is a visible pile; at 99 it
///   would be 17 rows for four days and would never be felt. He worked this out himself: *"having 99
///   of each is indefenetily buffed ... while having 10 is 10h of buffs"*.</item>
///   <item><b>Buff potions, enchant/attribute scrolls, boxes (99)</b> — the middle: carried for a
///   while, spent in ones. 99 is deep enough never to annoy and shallow enough that a hoard shows.</item>
///   <item><b>HP/MP potions (999)</b> — deliberately NOT a lever. They drain at up to 120 drinks an
///   hour while loot fills the bag behind them, so the peak row count is at hour zero and a cap that
///   bit would have to be tiny. What actually prices them is GOLD (an Uncommon mana potion is 60k an
///   hour). The cap is here for bounded arithmetic, not balance. ⚠ Its companion rule is his: the
///   shop sells at most ONE STACK per purchase (<c>HandleBuy</c>).</item>
///   <item><b>Materials (9,999)</b> — a crafter holds piles by design; taxing that bag is pure friction.</item>
///   <item><b>Quest items (uncapped)</b> — a gathering contract hands out a token per kill. A cap here
///   would be a bug wearing a mechanic's clothes.</item>
/// </list></summary>
public static class StackLimits
{
    /// <summary>Hour-long blessings. The one cap meant to be felt.</summary>
    public const int BuffScroll = 9;
    /// <summary>Buff potions, enchant scrolls, attribute scrolls, boxes, blueprints — and the default.</summary>
    public const int Standard = 99;
    /// <summary>HP and MP potions: a sanity bound, not a balance lever.</summary>
    public const int VitalPotion = 999;
    /// <summary>Crafting materials.</summary>
    public const int Material = 9999;
    /// <summary>`BL-144` (owner, 2026-09-03) — THE SKILL STONE, and only it: *"skill stones to stack to
    /// 9999 while the element type stones to stay at 99 -&gt; skill stones are used for fast reuse casts
    /// like heals etc .. and 99 are not near enough to have"*.
    /// <para>🔑 THE DISTINCTION IS SPEND RATE, not what the item is. A Skill Stone is the reagent of
    /// ordinary, repeated casts — Ultimate Heal takes one or two per cast and a whisp four per call, so
    /// a raid burns hundreds in an evening and 99 is under an hour of play. The elemental / holy /
    /// physical stones are the reagent of a handful of SET-PIECE casts, spent in ones, and they keep the
    /// <see cref="Standard"/> 99 he named.</para>
    /// <para>⚠ Delivered through <see cref="ItemDef.MaxStackOverride"/> — the FIRST user of that field,
    /// which exists precisely for "one item disagrees with its category". Everything sharing this item's
    /// category (a plain Consumable that is not a potion and not a blessing) is still 99, which is what
    /// keeps the elemental stones where he wants them without a second table.</para></summary>
    public const int Reagent = 9999;
    /// <summary>Quest items. Not <c>int.MaxValue</c>: the cap is multiplied by a row budget when a
    /// container works out what it can accept, and MaxValue overflows that arithmetic. This is
    /// "effectively no cap" while staying a real number — a gathering quest would need a million
    /// kills to reach it.</summary>
    public const int Uncapped = 1_000_000;

    /// <summary>The cap for a def, by CATEGORY. Order matters: quest items and materials are decided
    /// by slot, the two scroll kinds have to be separated before the slot answers, and a potion's
    /// drink cooldown is what tells an HP/MP potion from a blessing.</summary>
    public static int For(ItemDef def) => def.Slot switch
    {
        EquipSlot.QuestItem  => Uncapped,
        EquipSlot.Material   => Material,
        EquipSlot.Box        => Standard,
        // ⚠ EquipSlot.Scroll is the ENCHANT/ATTRIBUTE BENCH, not the blessings — those are Consumables.
        EquipSlot.Scroll     => Standard,
        // Three things share EquipSlot.Consumable and they do NOT share a cap. A drink cooldown marks
        // the HP/MP line (ItemCatalog.IsHealPotion); of what is left, an hour-long wrapper is a
        // blessing SCROLL and everything else — buff potions, the Scroll of Return — is ordinary.
        EquipSlot.Consumable => def.PotionCooldownTicks > 0 ? VitalPotion
                              : ItemCatalog.IsBuffScroll(def) ? BuffScroll
                              : Standard,
        _                    => Standard,
    };
}

/// <summary>The bag/vendor/keeper tabs (`BL-240` moved them here from the Unity client — see the note
/// on <see cref="ItemCatalog.CategoryOf"/>). The numbering is the tab ORDER and is on the wire
/// (`InstantSellCmd`), so it is append-only like every other id in this game.</summary>
public enum ItemCategory { All = 0, Gear = 1, Use = 2, Mats = 3, Quest = 4 }

public static class ItemCatalog
{
    // -----------------------------------------------------------------------
    // Stable string keys for hand-referenced items (potions, scrolls, legendary).
    // Weapon/armor keys are generated as "<type>_<grade>_<rarity>" — see below.
    // -----------------------------------------------------------------------
    public const string MinorPotion = "potion_minor";      // Common HoT
    public const string HealingPotion = "potion_healing";  // Uncommon HoT
    public const string GreaterPotion = "potion_greater";  // Rare HoT
    public const string InstantPotion = "potion_instant";  // Instant %-heal panic potion
    // ----- MANA potions (2026-08-27). The same shape as the healing ladder — a per-second restore
    //       over a fixed window, three rarities, each with its OWN drink cooldown — plus one rule
    //       the healing potions do not have: PVE ONLY. His SOURCES, 2026-08-27: *"only shop
    //       common/uncommon - rare apothecary crafter"* — so unlike the healing ladder these do
    //       NOT drop at ALL. Common and Uncommon are the Apothecary shelf; the Rare is the Potion
    //       Master's L5 recipe (Recipes.cs).
    //
    //       🔑 THE RATE COLUMN **IS** THE HEALING LADDER'S (owner, 2026-08-27: *"so healing are
    //       20/70/150 and we match that just 15/30 cycle"*). Read them off PotHealMinor / PotHeal /
    //       PotHealGreater in Skills.Common.cs — if the healing ladder is ever retuned, this one
    //       moves with it. ⚠ This SUPERSEDES the shipped-and-replaced 20/50/100 of 0.92.0.
    //
    //       ⚠ THE DURATION IS DELIBERATELY UNDER THE COOLDOWN — the ONE thing that differs from the
    //       healing ladder besides price. The Rare HEALING potion runs 30s on a 20s reuse, i.e.
    //       permanent uptime, so its "150 HP/s" really is 150 HP/s forever. 15s up on a 30s reuse is
    //       a 50% duty cycle, so these deliver 10 / 35 / 75 MP/s SUSTAINED — which is what the
    //       measurement (`BalanceMatrix --mpdrain` / `--mpnpc`) sizes them against, not the sticker.
    public const string MinorManaPotion   = "potion_mana_minor";     // Common,    20 MP/s
    public const string ManaPotion        = "potion_mana";           // Uncommon,  70 MP/s
    public const string GreaterManaPotion = "potion_mana_greater";   // Rare,     150 MP/s
    // ---- Buff potions and scrolls. ⚠ REWORKED, playtest-17 E3 (owner, 2026-08-03; built 2026-08-05).
    //      The shape is now: **the potion is what you FIND, the scroll is what you BUY**, and they no
    //      longer mirror each other rung for rung.
    //        · POTIONS keep two rungs — Common and Uncommon (r1/r2), 20 minutes, from drops and the
    //          Apothecary. The Rare potion is GONE: it was the same rung as the scroll, so the top of
    //          every ladder fell out of the sky and the paid layer had nothing left to sell.
    //        · SCROLLS are ONE per buff, at the family's MAX rung, Rare quality, one hour — *"no need
    //          for 6 scrolls for 1 buff"*. They drop NOWHERE (not even bosses) and are untradable;
    //          the only source is the Apothecary's Blessing Box, 250k for a pick of 10.
    //      Deleted with the rework: the 9 Rare potions, the 18 Common/Uncommon buff scrolls, and the
    //      Epic/Legendary rungs of the eight scroll-only families. A DefId that leaves the catalog is
    //      dropped on load (PersistenceService), so an old save simply loses them.
    //      Ids spell the STAT, not the display name, which has already changed twice.
    //      See docs/design/BuffLadders.md. ----
    public const string SpeedPotionC = "potion_speed_c";   // Swift    (move speed)
    public const string SpeedPotionU = "potion_speed_u";
    public const string CastPotionC = "potion_cast_c";     // Alacrity (cast speed)
    public const string CastPotionU = "potion_cast_u";
    public const string AtkPotionC = "potion_atk_c";       // Fury     (attack speed)
    public const string AtkPotionU = "potion_atk_u";
    public const string EvaPotionC = "potion_eva_c";       // Agility  (evasion)
    public const string EvaPotionU = "potion_eva_u";
    public const string MightPotionC = "potion_patk_c";
    public const string MightPotionU = "potion_patk_u";
    public const string BulwarkPotionC = "potion_pdef_c";
    public const string BulwarkPotionU = "potion_pdef_u";
    public const string ForcePotionC = "potion_matk_c";
    public const string ForcePotionU = "potion_matk_u";
    public const string WardPotionC = "potion_mdef_c";
    public const string WardPotionU = "potion_mdef_u";
    public const string AimPotionC = "potion_acc_c";
    public const string AimPotionU = "potion_acc_u";
    // The 17 SCROLLS — one per buff, top rung, all Rare, all box-only. The nine families that also
    // have a potion top out at rung 3 (`_r`); the eight scroll-only families at rung 6 (`_m`), which
    // is also what finally gives the Mythic rung a source at all.
    public const string SpeedScrollR = "scroll_speed_r";
    public const string CastScrollR = "scroll_cast_r";
    public const string AtkScrollR = "scroll_atk_r";
    public const string EvaScrollR = "scroll_eva_r";
    public const string MightScrollR = "scroll_patk_r";
    public const string BulwarkScrollR = "scroll_pdef_r";
    public const string ForceScrollR = "scroll_matk_r";
    public const string WardScrollR = "scroll_mdef_r";
    public const string AimScrollR = "scroll_acc_r";
    public const string BodyScrollM = "scroll_hp_m";
    public const string SoulScrollM = "scroll_mp_m";
    public const string VigorScrollM = "scroll_hpreg_m";
    public const string SerenityScrollM = "scroll_mpreg_m";
    public const string FocusScrollM = "scroll_crit_m";
    public const string FerocityScrollM = "scroll_critdmg_m";
    public const string InsightScrollM = "scroll_mcrit_m";
    public const string FrenzyScrollM = "scroll_frenzy_m";
    // `BL-149` — Vampirism and Resolve, the two NPC blessings that had no potion and no scroll.
    public const string VampScrollM = "scroll_vamp_m";
    public const string ResolveScrollM = "scroll_interrupt_m";
    // Dash — the short sprint burst, six rarities, no scroll.
    public const string DashPotionC = "potion_dash_c";
    public const string DashPotionU = "potion_dash_u";
    public const string DashPotionR = "potion_dash_r";
    public const string DashPotionE = "potion_dash_e";
    public const string DashPotionL = "potion_dash_l";
    public const string DashPotionM = "potion_dash_m";
    // ----- ENCHANT SCROLLS: three TYPES x six GRADES = 18 (owner, playtest-17 D1).
    //       ⚠ The E/D/C Normals KEEP the three original ids. Those three scrolls shipped at
    //       Common/Uncommon/Rare rarity, and the new rule (rarity picks the grade) maps exactly
    //       onto that — so re-pointing them costs nothing and every saved bag, box table and
    //       crafting recipe that already names them stays valid. Only the FAILURE behaviour of
    //       the D and C ones changes (they used to reset to +0 / drop 1; all Normals now break).
    public const string ScrollNormalE = "scroll_common";
    public const string ScrollNormalD = "scroll_uncommon";
    public const string ScrollNormalC = "scroll_rare";
    public const string ScrollNormalB = "scroll_enchant_b";
    public const string ScrollNormalA = "scroll_enchant_a";
    public const string ScrollNormalS = "scroll_enchant_s";
    public const string ScrollGreaterE = "scroll_greater_e";
    public const string ScrollGreaterD = "scroll_greater_d";
    public const string ScrollGreaterC = "scroll_greater_c";
    public const string ScrollGreaterB = "scroll_greater_b";
    public const string ScrollGreaterA = "scroll_greater_a";
    public const string ScrollGreaterS = "scroll_greater_s";
    public const string ScrollSafeE = "scroll_safe_e";
    public const string ScrollSafeD = "scroll_safe_d";
    public const string ScrollSafeC = "scroll_safe_c";
    public const string ScrollSafeB = "scroll_safe_b";
    public const string ScrollSafeA = "scroll_safe_a";
    public const string ScrollSafeS = "scroll_safe_s";
    public const string ScrollReturn = "scroll_return";
    public const string ScrollReturnUltimate = "scroll_return_ultimate";
    public const string ScrollResurrect = "scroll_resurrect";
    public const string ScrollResurrectUltimate = "scroll_resurrect_ultimate";
    public const string AttrScrollCommon = "attrscroll_common";
    public const string AttrScrollUncommon = "attrscroll_uncommon";
    public const string AttrScrollRare = "attrscroll_rare";
    public const string AttrScrollLegendary = "attrscroll_legendary";
    public const string AttrScrollEpic = "attrscroll_epic";
    public const string AttrScrollMythic = "attrscroll_mythic";
    // ----- THE CRAFTER QUEST (`BL-273` part 2, 0.203.0): the gathered tokens, the hammer head, and the
    //       Blacksmith's Hammer the quest recipe makes. The quest recipe item itself is
    //       RecipeBookId(Crafting.HammerRecipeId, Crafting.HammerRecipePercent).
    public const string CrafterQuestWood = "quest_crafter_wood";
    public const string CrafterQuestIron = "quest_crafter_iron";
    public const string CrafterQuestGem = "quest_crafter_gem";
    public const string CrafterHammerHead = "quest_crafter_hammer_head";
    public const string CrafterHammer = "quest_crafter_hammer";
    public static string CrafterQuestRecipe => RecipeBookId(Crafting.HammerRecipeId, Crafting.HammerRecipePercent);

    // ----- VOLCANIC (`BL-273` part 3 / `BL-274`): ash + stone drop on some 76/80/85 normals (step 11), and
    //       20 + 20 refine into one Volcanic Bar (step 10). Defined in 0.204.0 because step 9b's rare potions
    //       and 2h rune boxes already name them; until those steps land nothing gives one.
    public const string VolcanicAsh = "mat_volcanic_ash";
    public const string VolcanicStone = "mat_volcanic_stone";
    public const string VolcanicBar = "mat_volcanic_bar";

    public const string MarkOfFaith = "quest_mark_of_faith";
    public const string ClericsProof = "quest_clerics_proof";

    // ----- Repeatable-hunt gathering tokens (Quests.Repeatable.cs authors which creature drops which).
    //       Named after the creature, not the quest: the Huntmaster's list can be re-cut without the
    //       token in the player's bag suddenly meaning something else.
    public const string TokenFoxPelt = "quest_token_fox_pelt";
    public const string TokenWerewolfFang = "quest_token_werewolf_fang";
    public const string TokenSpiderHook = "quest_token_spider_hook";
    public const string TokenCrackedRib = "quest_token_cracked_rib";
    public const string TokenBearPelt = "quest_token_bear_pelt";
    public const string TokenMantisClaw = "quest_token_mantis_claw";
    public const string TokenHarpyFeather = "quest_token_harpy_feather";
    public const string TokenBasiliskScale = "quest_token_basilisk_scale";
    public const string TokenAshOrcInsignia = "quest_token_ash_orc_insignia";
    public const string TokenRustedShard = "quest_token_rusted_shard";
    public const string TokenDreadSigil = "quest_token_dread_sigil";
    public const string TokenRedhornBadge = "quest_token_redhorn_badge";
    public const string TokenEmberScale = "quest_token_ember_scale";
    public const string TokenRadiantPlume = "quest_token_radiant_plume";
    public const string TokenSplinterChitin = "quest_token_splinter_chitin";

    /// <summary>Every gathering token and its display name, in one list so the defs are built from the
    /// same place the ids are declared.</summary>
    private static readonly (string Id, string Name)[] GatherTokens =
    {
        (TokenFoxPelt,        "Fox Pelt"),
        (TokenWerewolfFang,   "Werewolf Fang"),
        (TokenSpiderHook,     "Barbed Hook"),
        (TokenCrackedRib,     "Cracked Rib"),
        (TokenBearPelt,       "Bear Pelt"),
        (TokenMantisClaw,     "Mantis Claw"),
        (TokenHarpyFeather,   "Harpy Feather"),
        (TokenBasiliskScale,  "Amber Scale"),
        (TokenAshOrcInsignia, "Ash Orc Insignia"),
        (TokenRustedShard,    "Rusted Shard"),
        (TokenDreadSigil,     "Dread Sigil"),
        (TokenRedhornBadge,   "Redhorn Badge"),
        (TokenEmberScale,     "Emberwyrm Scale"),
        (TokenRadiantPlume,   "Radiant Plume"),
        (TokenSplinterChitin, "Splinter Chitin"),
    };
    // (`god_judgment` / `god_robes` — deleted 2026-08-07 with the God layer, playtest-19 `0b`.)
    public const string WoodenShield = "shield_wooden";
    // (`shield_iron` — deleted 2026-08-12, him: *"Iron sheld can go .. wooden is a training gear"*. It
    //  was an E-grade hand-authored one-off that nothing sold, dropped or boxed, sitting between the
    //  Wooden Shield and the generated tier ladder for no reason. The training tier is ONE shield.)
    // (`jewel_brass_amulet`, `jewel_silver_talisman`, `blunt_1h_iron_mace`, `blunt_1h_ash_wand` —
    //  deleted 2026-08-13, playtest-22, with the whole legacy gear grid. Same reason as `shield_iron`
    //  above: hand-authored one-offs from before the ladder, unbalanced against it, and the Amulet was
    //  still on a shop shelf. 🔑 The rule is now written down: gear is LADDER (ItemLevel > 0) or
    //  TRAINING, and nothing else.)
    public const string ElementalStone = "elemental_stone";     // reagent for Elemental Burst
    public const string SkillStone = "skill_stone";             // reagent for skill costs (e.g. Angel's Protection)
    // ⚠ THREE ELEMENT-FLAVOURED REAGENTS, one shape (owner, 2026-08-26: *"need holy and physical(for
    // fighters) stones (same as elemental)"*). Same grade, same rarity, same 20k vendor price — a
    // skill picks the one its school burns. Elemental = the nuker's, Holy = the healer/buffer's,
    // Physical = the fighters'. Keep them identical: the moment one is cheaper it becomes the
    // one every skill is authored against.
    public const string HolyStone = "holy_stone";               // reagent — the holy/divine twin of ElementalStone
    public const string PhysicalStone = "physical_stone";       // reagent — the fighter twin of ElementalStone
    public const string SpBottle = "sp_bottle";                 // 1kkk SP, bought from an NPC for SP + gold, tradable
    /// <summary>How much of your ATK power a weapon lets through to the channel it does NOT
    /// exist to serve: a sword's magic, a staff's melee. THE tuning knob for weapon identity.
    /// 0.6 reproduces the gear CSV's second column (a sword's 92 P.Atk × 0.6 ≈ its authored 54
    /// M.Atk). NOTE: 0.6 does NOT yet close the buffer's staff→2H-sword swap — because magic
    /// damage goes as √mAtk, he loses only ~15% of it while doubling P.Atk. Dropping this to
    /// ~0.2-0.3 makes that a real trade. Any single weapon can override via its own
    /// PAtkFactor/MAtkFactor.</summary>
    public const float OffChannelFactor = 0.6f;

    // ---- TRAINING tier: levels 1-10, the WEAKEST gear in the game (owner, 2026-07-24) ----
    // A new character starts with these instead of the Newbie set. The Newbie gear did not go away —
    // it became the reward for the level-10 starter QUEST, so the first ten levels are played in kit
    // that cannot one-shot a mob. Bought at any weapon/armor vendor for 400g so a death or a bad pick
    // is recoverable. Untradeable and attribute-less like all starter gear; unlike the Newbie tier
    // these DO have a buy price, because being able to replace them is the point.
    public const string TrainingSword   = "training_sword";
    // (`training_bow` — deleted 2026-08-11, owner: no training bow/staff/2h. See TieredWeapons' note.)
    // (`training_club` + `training_knives` — deleted 2026-08-12, him: *"Any fighter cen get trough with
    //  a sword. Other training Club and training knives can be deleted."* The training tier is now
    //  exactly two items, one per BASE CLASS, because that is all the boxes hand out — see Boxes.cs.
    //  A save holding one just loses it: PersistenceService drops bag rows whose def no longer resolves.)
    public const string TrainingWand    = "training_wand";
    public const string TrainingLeather = "training_leather_armor";
    public const string TrainingRobe    = "training_robe";
    /// <summary>Price of every training weapon and armor at a vendor (owner: 400g each).</summary>
    public const int TrainingGearPrice = 400;

    // ---- BROKEN jewels: the level 1-5 drop line, and the only jewels a new character can get ----
    // The Newbie jewels box is gone from character creation (owner: "no shot/jewels"). These drop from
    // level 1-5 mobs and are sold in the shop, so the first accessories are EARNED. Tradable, unlike
    // the bound starter kit — an early player having something worth selling is the point.
    public const string BrokenEarring  = "broken_earring";
    public const string BrokenRing     = "broken_ring";
    public const string BrokenNecklace = "broken_necklace";

    /// <summary>Training WEAPONS selection box — pick ONE of the five. Given at character creation.</summary>
    public const string BoxTrainingWeapons = "box_training_weapons";
    /// <summary>Training ARMOR choice — pick leather (fighter) or robe (mage). Given at creation.</summary>
    public const string BoxTrainingArmorChoice = "box_training_armor_choice";

    // ===== THE NEWBIE KIT *IS* THE F-GRADE TOP (owner, 2026-07-30) ============================
    // "Make the newbie gear the Ferrite Mythic — it's the top for its grade."
    //
    // These were a parallel item line sitting beside the real ladder at the same levels. They are now
    // ALIASES onto the F tier's MYTHIC rung — the authored F piece, themed "Ferrite" by GradeTheme — so
    // the level-10 quest hands out the best F-grade gear there is instead of a separate set that has to
    // be kept in step with it by hand. The F tier's Mythic numbers were authored FROM these, so nothing
    // got stronger or weaker in the swap; there is simply one item where there were two.
    //
    // The names change with it (Newbie Sword → Ferrite Blade), which is the point: it is a real rung on
    // the ladder, not a tutorial prop.
    public const string NewbieSword1H = "sword1h_t1";
    public const string NewbieDaggers = "duals_t1";
    public const string NewbieSword2H = "sword2h_t1";
    public const string NewbieBow     = "bow_t1";
    public const string NewbieStaff   = "staff_t1";
    public const string NewbieLightBody   = "light_t1";
    public const string NewbieRobeBody    = "robe_t1";
    // SHARED accessories — used by BOTH the light and robe newbie sets.
    public const string NewbieHelm        = "helm_t1";
    public const string NewbieGloves      = "gloves_t1";
    public const string NewbieBoots       = "boots_t1";
    public const string NewbieEarring     = "earring_t1";
    public const string NewbieRing        = "ring_t1";
    public const string NewbieNecklace    = "necklace_t1";

    // ===== THE NEWBIE KIT IS A LOANER: BOUND + 30-DAY TIMED (owner, playtest-19 M6) ==============
    // "I want the newbie equipment to be unsellable and untradable and timelimited for 30d (can be
    //  destroied) - from the dolans quest"
    //
    // The pieces above are ALIASES onto the F tier's Mythic rung, which is real ladder gear: it
    // drops, it is crafted, it is bought. Stamping them untradable and timed would time out every
    // Ferrite Mythic in the game. So the tutorial chain hands out BOUND COPIES instead — same def,
    // same numbers, same SetId (so a loaner body still completes its set, with loaner or with real
    // accessories), differing only in id, name, tradability and the 30-day clock.
    //
    // Nothing is authored here: BoundCopies clones the generated piece, so the gear CSV stays the
    // single source of the numbers.
    public const int NewbieKitLifetimeSeconds = 30 * 24 * 3600;

    /// <summary>The id of an item's bound copy (see <see cref="BoundCopies"/>).</summary>
    public static string BoundId(string baseId) => baseId + "_bound";

    // ===== THE TEMPORARY 2-HOUR COMMON GEAR (`BL-272` part 2, 0.202.0) ==============================
    // His rulings (2026-09-23, design doc §2.1/§2.4; price `BL-287` item 8, 2026-09-24; the gaps 2026-09-24):
    //   * Vendors sell T40/T52 temporary Common gear in selection boxes: a WEAPON box (pick one) and an
    //     ARMOUR box (pick heavy / light / robe), each armour set WITH A SHIELD. No temporary jewellery.
    //   * Priced at the COMMON price: the weapon box = one Common two-hander (T40 214k), the armour box =
    //     the Common set's sum (body + helm + gloves + boots + shield; the three bodies cost the same).
    //   * The 2 hours tick ONLY WHILE WORN (<see cref="ItemDef.WornLifetimeSeconds"/>), and at 0 it is gone.
    //   * Untradeable, so unsellable (<see cref="IsSellable"/> needs tradable), and UNBREAKABLE (<see
    //     cref="Crafting.BreakYield"/>): a bought box that broke into essence would be a vendor selling
    //     essence, which he ruled out. It may still be destroyed, and kept in the private warehouse.
    // Like the loaner kit, every piece is a CLONE of the generated Common piece; nothing is authored here.

    /// <summary>How long a temporary piece lasts while worn: two hours.</summary>
    public const int TempGearWornSeconds = 2 * 3600;

    /// <summary>The tiers that sell temporary gear.</summary>
    public static readonly int[] TempGearTiers = { 40, 52 };

    /// <summary>The weapon lines in a temporary weapon box (the id stem before "_t{tier}").</summary>
    public static readonly string[] TempWeaponStems =
        { "sword1h", "sword2h", "blunt1h", "blunt2h", "duals", "bow", "wand", "staff" };

    /// <summary>The three armour weights; each set box holds its body + <see cref="TempArmorShared"/>.</summary>
    public static readonly string[] TempArmorWeights = { "heavy", "light", "robe" };

    /// <summary>The pieces every temporary armour set shares, shield included (*"all three … include a shield"*).</summary>
    public static readonly string[] TempArmorShared = { "helm", "gloves", "boots", "shield" };

    /// <summary>The temporary copy of a Mythic base-tier piece: "sword2h_t40" → "sword2h_t40_temp".</summary>
    public static string TempId(string mythicId) => mythicId + "_temp";
    public static string TempWeaponBoxId(int tier) => $"box_temp_weapon_t{tier}";
    public static string TempArmorBoxId(int tier) => $"box_temp_armor_t{tier}";
    public static string TempSetBoxId(string weight, int tier) => $"box_temp_{weight}_t{tier}";

    /// <summary>Every temporary piece's id at a tier, in box order.</summary>
    public static IEnumerable<string> TempPieceIds(int tier) =>
        TempWeaponStems.Concat(TempArmorWeights).Concat(TempArmorShared).Select(s => TempId($"{s}_t{tier}"));

    public const string NewbieSword1HBound  = "sword1h_t1_bound";
    public const string NewbieDaggersBound  = "duals_t1_bound";
    public const string NewbieSword2HBound  = "sword2h_t1_bound";
    public const string NewbieBowBound      = "bow_t1_bound";
    public const string NewbieStaffBound    = "staff_t1_bound";
    public const string NewbieWandBound     = "wand_t1_bound";
    public const string NewbieLightBodyBound = "light_t1_bound";
    public const string NewbieRobeBodyBound  = "robe_t1_bound";
    public const string NewbieHelmBound      = "helm_t1_bound";
    public const string NewbieGlovesBound    = "gloves_t1_bound";
    public const string NewbieBootsBound     = "boots_t1_bound";
    public const string NewbieEarringBound   = "earring_t1_bound";
    public const string NewbieRingBound      = "ring_t1_bound";
    public const string NewbieNecklaceBound  = "necklace_t1_bound";

    // The tutorial chain's COMPLETION kit (owner, M5): "x1 Ultimate Scroll of Escape, x1 Ultimate
    // Scroll of Resurrection, x5 Mythic Dash Potion, x5 Instant Health Potion — every one
    // untradable/unsellable". Bound copies again, but with NO timer: the reward for finishing is
    // yours to spend whenever, it just cannot be sold or handed to an alt.
    // ("Scroll of Escape" is this game's Scroll of Return — teleport out to town.)
    public const string ScrollReturnUltimateBound   = "scroll_return_ultimate_bound";
    public const string ScrollResurrectUltimateBound = "scroll_resurrect_ultimate_bound";
    public const string DashPotionMBound            = "potion_dash_m_bound";
    public const string InstantPotionBound          = "potion_instant_bound";
    // RUNES + their sealed boxes (open → rune, wall-clock expiry set on open).
    public const string WarRune      = "rune_war";
    public const string SpellRune    = "rune_spell";
    public const string BoxWarRune1h     = "box_war_rune_1h";
    public const string BoxWarRune2h     = "box_war_rune_2h";
    public const string BoxWarRune24h    = "box_war_rune_24h";
    public const string BoxWarRune30d    = "box_war_rune_30d";
    public const string BoxSpellRune1h   = "box_spell_rune_1h";
    public const string BoxSpellRune2h   = "box_spell_rune_2h";
    public const string BoxSpellRune24h  = "box_spell_rune_24h";
    public const string BoxSpellRune30d  = "box_spell_rune_30d";
    // `BL-187`, 2026-09-10 — the combined rune. ONE item, ONE rung (24h), admin-granted only:
    // no vendor price, no 1h/2h/30d ladder. See SkillCatalog.GrandRuneBuff.
    public const string GrandRune        = "rune_grand";
    public const string BoxGrandRune24h  = "box_grand_rune_24h";
    // `BL-277` part 3 — the Wayfarer's items: four runes (two buffs × 1 h / 2 h), the restore potion,
    // and the box a subclass slot pays the first time it is ever filled.
    public const string FavorKeepRune1h      = "rune_favor_keep_1h";
    public const string FavorKeepRune2h      = "rune_favor_keep_2h";
    public const string BlessingBoostRune1h  = "rune_blessing_boost_1h";
    public const string BlessingBoostRune2h  = "rune_blessing_boost_2h";
    public const string FavorRestorePotion   = "potion_favor_restore";
    public const string BoxWayfarerSubclass  = "box_wayfarer_subclass";
    // Newbie CHOICE selection-boxes (pick one of two sub-boxes).
    public const string BoxNewbieArmorChoice = "box_newbie_armor_choice";
    public const string BoxNewbieRuneChoice  = "box_newbie_rune_choice";
    /// <summary>The Apothecary's daily reward — pick soul or spirit, 1 hour, untradable.</summary>
    public const string BoxDailyRuneChoice   = "box_daily_rune_choice";
    // The Apothecary's Blessing Box (playtest-17 E3) — 250k for a PICK OF 10 of the 17 buff scrolls,
    // and the only source of a buff scroll in the game. At 76+ the full set is two boxes = 500k, which
    // is deliberately about an hour of farming: a live buffer still has to be the better deal.
    public const string BoxBuffScrolls    = "box_buff_scrolls";
    /// <summary>The Rune of Tincture — what makes a title's COLOUR something you spend rather than
    /// something you type (owner, playtest-20 `59r`: *"/titlecolor should be gated behind a rune item
    /// that opens a colour list when clicked, not a free command"*). Using one opens the palette; the
    /// rune is consumed when a colour is actually chosen, never merely by opening the list.</summary>
    public const string TitleColorRune    = "rune_title_colour";
    public const string TitleRuneName     = "Rune of Tincture";

    /// <summary>THE SUBCLASS TICKET (`BL-250` §5) — *"those values give you a subclassTicket and u can
    /// unlock them using(consumable) ticket"*. Earned three times over your progress and bought four
    /// more; USING one opens the next subclass slot. See <see cref="SubclassSlots"/> for the ladder.
    /// <para>⚠ Untradable and unsellable by design. It is an ENTITLEMENT that four of its seven copies
    /// were paid for in gold or platinum, so a market in them would be a way to launder premium
    /// currency into gold, and a way to sell a slot you were given for reaching 76.</para></summary>
    public const string SubclassTicket    = "subclass_ticket";

    // ----- PREMIUM REWARD RUNES (2026-08-12). One ITEM per channel per rung, every one pointing at
    //       the ONE ladder skill for its channel. The ids and the wording live in RewardRunes.cs;
    //       these two are aliased here because they are named directly (by `/give`, by the tests). -----
    /// <summary>Rune of Sinister — the grinder's rune: no exp, no SP, gold and drops untouched.</summary>
    public const string SinisterRune = RewardRunes.SinisterId;
    /// <summary>Rune of Sinners — all four rewards zeroed, and bound to the soul: unsellable,
    /// untradable, refused by BOTH keepers and undeletable for as long as it has time left.</summary>
    public const string SinnersRune  = RewardRunes.SinnersId;

    // Boxes/chests — opened from the inventory; roll their BoxCatalog loot table.
    public const string BoxNewbie         = "box_newbie";
    public const string BoxTreasure       = "box_treasure";
    public const string BoxNewbieArmorLight = "box_newbie_armor_light";
    public const string BoxNewbieArmorRobe  = "box_newbie_armor_robe";
    public const string BoxNewbieJewels     = "box_newbie_jewels";
    public const string BoxNewbieWeapons    = "box_newbie_weapons";   // SELECTION box
    // 🔴 THE SIX "Dark Dominion" IDS ARE GONE (playtest 23, his ruling: *"Delete the `dark dominion` it
    // falls in the category for deletion."*). They were a hand-authored E-grade set from before the
    // grade ladder existed — six real pieces with a real set bonus that NOTHING dropped, sold or boxed,
    // so no character could ever have assembled it. That is the same category as `79e`'s 64 off-ladder
    // items; the only reason they outlived that sweep is that a designed SET is a decision to delete,
    // not a cleanup, so it was put to him instead. Now ruled.
    //
    // ⚠ THE RULE THIS LEAVES IS UNCHANGED (and now has no exception at all): gear is **LADDER**
    // (ItemLevel > 0, generated from gear_sets.csv) or **TRAINING** (its own CSV block). There is no
    // third category, and no hand-authored named set outside the generator.

    // ===================================================================================
    //  THE ENCHANT SCROLL TABLE — the single source of truth for the 18 scrolls (D1).
    //  The catalog entries, the drop tables and the admin menu all read it, so a scroll can
    //  never exist in one and be missing from another.
    // ===================================================================================

    /// <summary>One grade rung: which RARITY signals it, the ItemGrade the pricing enum should use,
    /// the character level the band opens at, and the NORMAL scroll's buy price at that grade.
    /// The pricing enum has no C/D, so E/D/C all sit at F exactly as they did before this rework —
    /// which is what keeps the three original scrolls at the prices the economy pass just set.</summary>
    public static readonly (EnchantGrade Grade, ItemRarity Rarity, ItemGrade Priced, int Level, int Value)[]
        EnchantScrollBands =
    {
        (EnchantGrade.E, ItemRarity.Common,    ItemGrade.F, 20,     30),
        (EnchantGrade.D, ItemRarity.Uncommon,  ItemGrade.F, 40,     60),
        (EnchantGrade.C, ItemRarity.Rare,      ItemGrade.F, 52,    120),
        (EnchantGrade.B, ItemRarity.Epic,      ItemGrade.B, 61,    500),
        (EnchantGrade.A, ItemRarity.Legendary, ItemGrade.A, 76,   2000),
        (EnchantGrade.S, ItemRarity.Mythic,    ItemGrade.S, 80,  10000),
    };

    /// <summary>One TYPE: its name prefix and what it multiplies the grade's price by. A Greater is
    /// worth several Normals and a Safe several Greaters, which is the only place the three types
    /// differ in value — rarity can't say it, because rarity is spent on signalling the GRADE.</summary>
    public static readonly (ScrollKind Kind, string Prefix, int PriceMul)[] EnchantScrollTypes =
    {
        (ScrollKind.Normal,  "Scroll of Enchant",         1),
        (ScrollKind.Greater, "Greater Scroll of Enchant", 5),
        (ScrollKind.Safe,    "Safe Scroll of Enchant",   20),
    };

    /// <summary>The id of an enchant scroll by TYPE and GRADE. The E/D/C Normals return their
    /// original ids (see the constants above); everything else is `scroll_{type}_{grade}`.</summary>
    public static string EnchantScrollKey(ScrollKind kind, EnchantGrade grade)
    {
        if (kind == ScrollKind.Normal)
            switch (grade)
            {
                case EnchantGrade.E: return ScrollNormalE;
                case EnchantGrade.D: return ScrollNormalD;
                case EnchantGrade.C: return ScrollNormalC;
            }
        string type = kind switch
        {
            ScrollKind.Greater => "greater",
            ScrollKind.Safe => "safe",
            _ => "enchant",
        };
        return $"scroll_{type}_{EnchantRules.GradeName(grade).ToLowerInvariant()}";
    }

    // WeaponKey/ArmorKey went with the legacy grid they addressed (playtest-22). Gear ids are the
    // ladder's own `<slot>_t<level>[_<rarity>]` now, and there is exactly one way to build one.

    private static readonly Dictionary<string, ItemDef> All = BuildCatalog();

    private static Dictionary<string, ItemDef> BuildCatalog()
    {
        var list = new List<ItemDef>();

        // ===================================================================
        //  🔴 THE LEGACY GEAR GRID IS GONE (playtest-22).
        //
        //  Sixty items used to be generated here — 4 weapon types and 3 armour weights
        //  plus 3 accessory slots, each x F/E grade x Common/Uncommon/Rare — under names
        //  like "Masterwork Steel Sword" and "Fine Worn Plate Armor". They PREDATE the
        //  grade ladder (TieredWeapons/TieredArmor below, driven by gear_sets.csv) and
        //  were never re-cut with it, so every one of them sat off the ladder with
        //  ItemLevel 0, unbalanced against everything around it. His find:
        //  *"Brass amulet also need to be gone. Look for other items that are not from
        //  the grade items or training ... treasure chest just gave me masterwork iron
        //  sword."* (He was holding `sword_e_rare`; the noun is "Steel", not "Iron".)
        //
        //  🔑 The only live way in was ONE Treasure Chest line (Boxes.cs), which is why
        //  they survived four gear passes: nothing else referenced them, so nothing else
        //  ever showed them to anyone. That chest line now rolls a ladder item.
        //
        //  ⚠ THE RULE THIS LEAVES BEHIND: every piece of gear is either GENERATED from
        //  the ladder (ItemLevel > 0) or is on the hand-authored TRAINING tier that has
        //  its own block in gear_sets.csv (72b). There is no third category. A bag row
        //  naming one of the deleted ids is dropped on load, which is the intended and
        //  already-handled path for a retired def.
        // ===================================================================

        // ===================================================================
        //  NAMED ARMOR SETS — none. The six Dark Dominion pieces stood here until playtest 23 and are
        //  deleted on his ruling; see the id block above for why. Every set in the game is now emitted
        //  by the tiered generator, which is what guarantees a set's pieces can actually be obtained.
        // ===================================================================

        // ===================================================================
        //  POTIONS
        // ===================================================================
        // Healing potions: priced so heals are a real gold sink (~500 for the staple).
        // Healing potions: the potion NAMES a skill and the skill heals. PotionCooldownTicks is
        // what marks it a HEAL potion (the shared "one per 30s" drink rule).
        // Flat heal-over-time tiers (owner, 2026-07-23). PotionCooldownTicks is the PER-POTION drink
        // cooldown (each tier independent): Common/Uncommon 10s, Rare 20s, Instant 60s. It also marks the
        // item as a heal potion (IsHealPotion). Rarer potions restore more per second and last longer.
        list.Add(new ItemDef(MinorPotion, "Common Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common,
            UseSkillId: SkillCatalog.PotHealMinor, PotionCooldownTicks: 100, Value: 60));
        list.Add(new ItemDef(HealingPotion, "Uncommon Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon,
            UseSkillId: SkillCatalog.PotHeal, PotionCooldownTicks: 100, Value: 250));
        list.Add(new ItemDef(GreaterPotion, "Rare Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            UseSkillId: SkillCatalog.PotHealGreater, PotionCooldownTicks: 200, Value: 5000));
        // The instant panic potion: +30% max HP, 60s cooldown, rare quality. (Meant for the future
        // boss/challenge-point shop; a gold value stands in until that exists.)
        list.Add(new ItemDef(InstantPotion, "Instant Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            UseSkillId: SkillCatalog.PotHealInstant, PotionCooldownTicks: 600, Value: 5000));

        // ----- MANA potions. Three rarities, 20/70/150 MP per second for 15s, each on its own 30s
        //       drink cooldown (owner, 2026-08-27). PveOnly: the drink is refused while flagged or
        //       PK; an already-running one is never stripped.
        //
        //       🔑 THE LADDER MIRRORS HEALING, THE PRICE IS DOUBLE IT (owner, 2026-08-27: *"so
        //       healing are 20/70/150 and we match that just 15/30 cycle … and price is double of
        //       the healing"*). Healing is 60 / 250 / 1500, so mana is 120 / 500 / 3000. (0.204.0: the Rares are 5,000 / 10,000, his step-9b base price.) ⚠ This
        //       SUPERSEDES his earlier *"500 common, 1500 uncommon"* — newest ruling wins, and the
        //       Rare is no longer my invention either: it is 2x the Rare healing potion like the
        //       other two. His own check on the middle rung: *"60k/hour for uncommon is ok"*
        //       (500 x 2 drinks/min x 60).
        //
        //       🔑 WHY MANA IS DEARER THAN HP AT ALL, given the identical rate: *"common/uncommon
        //       healing potions are dropped so u dont spend there … u need to buy mp pots"*. Mana
        //       potions do NOT drop anywhere in the game — the price pays for the missing faucet,
        //       not for potency. Rare stays off the shelf for the same reason the Rare healing
        //       potion does — it is the Apothecary's L7/L10 recipe (0.204.0), and the ONLY way to get one;
        //       *"its raiding support item that is economy player trade only"*.
        list.Add(new ItemDef(MinorManaPotion, "Common Mana Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common,
            UseSkillId: SkillCatalog.PotManaMinor, PotionCooldownTicks: 300, PveOnly: true, Value: 120,
            Description: "Restores 20 MP per second for 15s. Cannot be drunk while flagged for PvP."));
        list.Add(new ItemDef(ManaPotion, "Uncommon Mana Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon,
            UseSkillId: SkillCatalog.PotMana, PotionCooldownTicks: 300, PveOnly: true, Value: 500,
            Description: "Restores 70 MP per second for 15s. Cannot be drunk while flagged for PvP."));
        list.Add(new ItemDef(GreaterManaPotion, "Rare Mana Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            UseSkillId: SkillCatalog.PotManaGreater, PotionCooldownTicks: 300, PveOnly: true, Value: 10000,
            Description: "Restores 150 MP per second for 15s. Cannot be drunk while flagged for PvP."));

        // ----- RUNES + their boxes. The rune is HELD (not equipped/consumed): while it's in the main
        // inventory and unexpired the reconciliation loop keeps its buff up. Tradable:false (can't sell/
        // trade) AND delete-protected in the bin handler (IsRune). The box is sealed — OPENING it stamps
        // ExpiresAtUtc = now + GrantsRuneSeconds, so the clock starts on open, not on purchase. -----
        // GrantsRuneSeconds on the RUNE is its DEFAULT lifespan, stamped at ACQUIRE time (AddItem) for any
        // source — a drop, a direct give, a quest — not just a box. A box that grants the rune OVERRIDES
        // this with its own duration. So a rune always has a wall-clock expiry the moment you get it.
        // ⚠ EVERY RUNE IS MYTHIC — `BL-153`, WIDENED THE SAME DAY. The first ruling read as a test
        // (*"make war/spell runes mythic grade (all others as well if they have no Levels but still SP
        // rune 10 is different from SP rune 100)"*) and this file implemented it as one: Mythic for a
        // rune with no ladder, the rung's own grade for a rune that ladders. He then said plainly
        // *"all runes if they can be same rarity at mythic and SP/EXP/etc runes just be same rarity at
        // mythic"* — so the ladder does NOT split the rarity after all. **`EquipSlot.Rune` ⇒
        // `ItemRarity.Mythic`, with no exception**, and `RewardRune` no longer takes a rarity.
        // What tells a +10% SP rune from a +100% one is its NAME and its rung, which is what he meant
        // by "different" — not the colour of the line in the bag.
        // ⚠ Safe to sweep: rarity feeds crafting recipes, breaking and the shop ladder, but all three
        // gate on `ItemLevel > 0` and a GEAR slot first (`Recipes.FinishedItemRecipes`,
        // `Crafting.BreakYield`, `ShopCatalog`), and a rune has ItemLevel 0. Pricing is pinned by
        // `BuyPriceOverride: -1` / `SellPriceOverride: 0` / `Value: 0`, so `RarityPriceMul` never runs
        // on one. The change is display and sort order only. ⚠ The **Rune of Tincture** is NOT swept:
        // it is `EquipSlot.Consumable` with a real 40 000 `Value`, so raising it would move its vendor
        // price — it carries the word "Rune" but is not one.
        list.Add(new ItemDef(WarRune, "War Rune", EquipSlot.Rune, ItemGrade.F, ItemRarity.Mythic,
            IsRune: true, RuneBuffSkillId: SkillCatalog.WarRuneBuff, GrantsRuneSeconds: 3600,
            Tradable: false, Value: 0,
            Description: "Held rune: increases your final PHYSICAL damage ×2 while in your bag. Boosts PHYSICAL damage only (melee/bow) — useless for spells. Move it to the warehouse to switch it off; it can't be deleted."));
        list.Add(new ItemDef(SpellRune, "Spell Rune", EquipSlot.Rune, ItemGrade.F, ItemRarity.Mythic,
            IsRune: true, RuneBuffSkillId: SkillCatalog.SpellRuneBuff, GrantsRuneSeconds: 3600,
            Tradable: false, Value: 0,
            Description: "Held rune: increases your final MAGICAL damage ×2, and cast speed, while in your bag. Boosts MAGIC (spells) only — useless for melee/bow. Move it to the warehouse to switch it off; it can't be deleted."));
        // `BL-187` — the combined rune. Same held-rune machinery as the two above; the only
        // differences are that it carries BOTH channels and that nothing sells it.
        list.Add(new ItemDef(GrandRune, "Grand Rune", EquipSlot.Rune, ItemGrade.F, ItemRarity.Mythic,
            IsRune: true, RuneBuffSkillId: SkillCatalog.GrandRuneBuff, GrantsRuneSeconds: 24 * 3600,
            Tradable: false, Value: 0,
            Description: "Held rune: increases your final PHYSICAL and MAGICAL damage ×2, shortens your casts by 30%, and raises cast speed, while in your bag. Supersedes a War or Spell Rune held at the same time. Move it to the warehouse to switch it off; it can't be deleted."));

        // Sealed rune boxes. 1h/2h are vendor-stocked (Apothecary, real gold price) and TRADABLE (giftable
        // sealed — the RUNE inside is still bound). 24h/30d are premium/pass items: not buyable (BuyPrice
        // -1) and NOT tradable. Prices/tradability are a stand-in for the future premium economy.
        const int H = 3600, D = 24 * 3600;
        void RuneBox(string id, string name, int seconds, int buyPrice, bool tradable, string desc) =>
            list.Add(new ItemDef(id, name, EquipSlot.Box, ItemGrade.F, ItemRarity.Rare,
                GrantsRuneSeconds: seconds, Tradable: tradable, BuyPriceOverride: buyPrice, SellPriceOverride: 0,
                Description: desc));
        RuneBox(BoxWarRune1h,  "War Rune Box (1h)",  1 * H, 150000, true,  "Opens to a War Rune lasting 1 hour. War Runes multiply your final PHYSICAL damage ×2 (melee/bow) — useless for spells.");
        RuneBox(BoxWarRune2h,  "War Rune Box (2h)",  2 * H, 280000, true,  "Opens to a War Rune lasting 2 hours. War Runes multiply your final PHYSICAL damage ×2 (melee/bow) — useless for spells.");
        RuneBox(BoxWarRune24h, "War Rune Box (1d)",  1 * D, -1,  false,  "Opens to a War Rune lasting 24 hours. War Runes multiply your final PHYSICAL damage ×2 (melee/bow) — useless for spells.");
        RuneBox(BoxWarRune30d, "War Rune Box (30d)", 30 * D, -1, false,  "Opens to a War Rune lasting 30 days. War Runes multiply your final PHYSICAL damage ×2 (melee/bow) — useless for spells.");
        RuneBox(BoxSpellRune1h,  "Spell Rune Box (1h)",  1 * H, 150000, true,  "Opens to a Spell Rune lasting 1 hour. Spell Runes multiply your final MAGICAL damage ×2 (spells) — useless for melee/bow.");
        RuneBox(BoxSpellRune2h,  "Spell Rune Box (2h)",  2 * H, 280000, true,  "Opens to a Spell Rune lasting 2 hours. Spell Runes multiply your final MAGICAL damage ×2 (spells) — useless for melee/bow.");
        RuneBox(BoxSpellRune24h, "Spell Rune Box (1d)",  1 * D, -1,  false,  "Opens to a Spell Rune lasting 24 hours. Spell Runes multiply your final MAGICAL damage ×2 (spells) — useless for melee/bow.");
        RuneBox(BoxSpellRune30d, "Spell Rune Box (30d)", 30 * D, -1, false,  "Opens to a Spell Rune lasting 30 days. Spell Runes multiply your final MAGICAL damage ×2 (spells) — useless for melee/bow.");
        // ⚠ PREMIUM ONLY — `BuyPriceOverride: -1` and not tradable, exactly like the 24h/30d singles.
        // Its one route into a bag today is the Admin panel. `BL-187` closes here.
        // 🔑 THE CURRENCY EXISTS NOW (`BL-257`, 2026-09-16) — this and the 24h/30d singles are the
        //    items it was waiting for. Giving one a price is a single `PlatinumPrice: N` beside the
        //    `-1`, which is precisely the platinum-ONLY shape; it is left at 0 because he has not
        //    priced them, and an invented premium price is not ours to author.
        RuneBox(BoxGrandRune24h, "Grand Rune Box (1d)", 1 * D, -1, false, "Opens to a Grand Rune lasting 24 hours. Grand Runes multiply your final PHYSICAL and MAGICAL damage x2, shorten your casts by 30% and raise cast speed - both channels of the War and Spell Runes in one item, each at full strength.");

        // ----- `BL-277` part 3 — THE WAYFARER'S ITEMS. Four held runes on the same machinery as every
        // rune above (the item's wall clock drives the buff; the kill and the fill rate ask whether the
        // buff is up), the restore potion and the subclass box. ⚠ NOTHING SELLS ANY OF THEM: his note's
        // source is event currency and events do not exist (the recurring premium grant is `BL-284`), so
        // today they come from the subclass box and the admin `/give`. Untradable and unsellable like the
        // premium reward runes, so the box a subclass pays out cannot be farmed onto the market.
        void WayfarerRune(string id, string name, string skillId, int seconds, string desc) =>
            list.Add(new ItemDef(id, name, EquipSlot.Rune, ItemGrade.F, ItemRarity.Mythic,
                IsRune: true, RuneBuffSkillId: skillId, GrantsRuneSeconds: seconds,
                Tradable: false, BuyPriceOverride: -1, SellPriceOverride: 0, Value: 0,
                NoAttributes: true, Description: desc));
        const string KeepLine = "Held rune: your kills do not drain the Wayfarer's Favor while it is in your bag. Move it to the warehouse to switch it off; it can't be deleted.";
        const string BoostLine = "Held rune: the Wayfarer's Blessing gauge fills twice as fast (kills, combat time, Favor stages, level-ups) while it is in your bag. Move it to the warehouse to switch it off; it can't be deleted.";
        WayfarerRune(FavorKeepRune1h, "Favor Keep-Rune (1h)", SkillCatalog.FavorKeepRuneBuff, 1 * H, KeepLine);
        WayfarerRune(FavorKeepRune2h, "Favor Keep-Rune (2h)", SkillCatalog.FavorKeepRuneBuff, 2 * H, KeepLine);
        WayfarerRune(BlessingBoostRune1h, "Blessing Booster Rune (1h)", SkillCatalog.BlessingBoostRuneBuff, 1 * H, BoostLine);
        WayfarerRune(BlessingBoostRune2h, "Blessing Booster Rune (2h)", SkillCatalog.BlessingBoostRuneBuff, 2 * H, BoostLine);
        // No use-skill: what it does is a number on the character, answered by name in HandleUsePotion
        // (the Subclass Ticket's shape). Its hour of reuse is WALL-CLOCK on the character, not a
        // PotionCooldownTicks — those live in memory and a relog would reset them.
        list.Add(new ItemDef(FavorRestorePotion, "Favor Restore Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Mythic,
            Tradable: false, BuyPriceOverride: -1, SellPriceOverride: 0, Value: 0, NoAttributes: true,
            Description: $"Restores {WayfarerFavor.PotionPoints:N0} Wayfarer's Favor. Once per hour."));
        list.Add(new ItemDef(BoxWayfarerSubclass, "Wayfarer's Subclass Box", EquipSlot.Box,
            ItemGrade.F, ItemRarity.Mythic,
            Tradable: false, BuyPriceOverride: -1, SellPriceOverride: 0, Value: 0,
            Description: "Given the first time each subclass slot is filled. Holds a Favor Keep-Rune (1h), "
                       + "a Blessing Booster Rune (1h) and 4 Favor Restore Potions. The runes' hours start "
                       + "when you open it."));

        // ----- PREMIUM REWARD RUNES: one item per channel per rung (5 × 11), plus Sinister and
        // Sinners. Same held-rune machinery as the War/Spell runes above — the difference is entirely
        // in the buff, which carries a RewardRates package instead of a combat stat.
        //
        // Not buyable and not tradable: these are the premium/shop currency of the future store, and
        // until that exists `/give` is how they reach a player. Unsellable too (SellPriceOverride 0),
        // so a rune can never be laundered into gold.
        // ⚠ MYTHIC LIKE EVERY OTHER RUNE (`BL-153` as widened — see the note above the War Rune). The
        // rarity used to be a parameter, Epic for the 55 laddered runes and Mythic for the two
        // punishments; he then ruled the ladder does not split the rarity, so the parameter is gone
        // rather than defaulted — a rune's rung and name carry the difference, not its colour.
        void RewardRune(string id, string name, string skillId, int buffLevel, string desc,
                        bool soulBound = false) =>
            list.Add(new ItemDef(id, name, EquipSlot.Rune, ItemGrade.F, ItemRarity.Mythic,
                IsRune: true, RuneBuffSkillId: skillId, RuneBuffLevel: buffLevel,
                GrantsRuneSeconds: RewardRunes.DefaultSeconds,
                Tradable: false, BuyPriceOverride: -1, SellPriceOverride: 0, Value: 0,
                NoAttributes: true, SoulBound: soulBound, Description: desc));

        foreach (var ch in RewardRunes.All)
            for (int rung = 0; rung < RewardRunes.Ladder.Length; rung++)
            {
                int pct = RewardRunes.Percent(rung);
                RewardRune(ch.ItemId(pct), ch.NameAt(pct), ch.SkillId, rung + 1, ch.Line(pct));
            }
        RewardRune(RewardRunes.SinisterId, RewardRunes.SinisterName,
            RewardRunes.SinisterId, 1, RewardRunes.SinisterLine);
        // Sinners is BOUND on the DEF as well as by whatever `/give` writes on the instance: an
        // authored punishment must not depend on the admin remembering the right flags. The per-
        // instance overrides (`58d`) are what let him hand out a HARSHER one — a shorter clock, a
        // custom name — not what makes this one bound.
        RewardRune(RewardRunes.SinnersId, RewardRunes.SinnersName,
            RewardRunes.SinnersId, 1, RewardRunes.SinnersLine, soulBound: true);

        // Newbie CHOICE selection-boxes (untradable): armor set (fighter vs mage) and a 1-day rune
        // (soul vs spirit). Each is a PickCount:1 box whose OPTIONS are other boxes — pick one, open it.
        list.Add(new ItemDef(BoxNewbieArmorChoice, "Newbie Armor Set", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, Description: "Choose ONE: a Fighter (light) or Mage (robe) armor set."));
        list.Add(new ItemDef(BoxNewbieRuneChoice, "Newbie Rune", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, Description: "Choose ONE: a 1-day War Rune (physical) or Spell Rune (magic) rune box."));
        // The DAILY quest reward. Untradable and worth nothing at a vendor, unlike the 1h boxes the
        // Apothecary SELLS: a free daily that could be farmed across characters and sold on would be a
        // gold faucet rather than a leg-up (owner: quest rune boxes untradable, bought ones not).
        list.Add(new ItemDef(BoxDailyRuneChoice, "Rune Box (1h) — Daily", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, BuyPriceOverride: -1, SellPriceOverride: 0,
            Description: "Choose ONE: a 1-hour War Rune (physical) or Spell Rune (magic) rune box. "
                       + "Untradable — the Apothecary gives these out, she does not sell them."));

        // The Rune of Tincture — the RIGHT to colour a title you wrote, made into a thing you own
        // (`59r`). Not consumed: he asked to click the rune to open the colour list, which a one-shot
        // item could not do twice, so holding it IS the right and the click is the menu. Tradable —
        // a cosmetic you can hand to a friend is a better cosmetic.
        list.Add(new ItemDef(TitleColorRune, TitleRuneName, EquipSlot.Consumable, ItemGrade.F, ItemRarity.Uncommon,
            Value: 40000, NoAttributes: true,
            Description: "Keep it to earn the right to colour the title you wrote, and use it to pick the colour."));

        // The SUBCLASS TICKET (`BL-250` §5). A Consumable with no use-skill, answered by name in
        // HandleUsePotion exactly like the Rune of Tincture above — the one thing it does is not a
        // skill, so there is nothing for a UseSkillId to point at.
        // ⚠ Untradable, unsellable, unbuyable off any SHELF (`BuyPriceOverride: -1`): four of the
        //   seven are sold, but by the CLASS MASTER at a price that depends on which slot you are
        //   opening, which no fixed shelf row could express. ConfirmOnUse because it is permanent and
        //   it lands on a bar you can tap by accident.
        list.Add(new ItemDef(SubclassTicket, "Subclass Ticket", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Mythic,
            Tradable: false, BuyPriceOverride: -1, SellPriceOverride: 0, Value: 0,
            NoAttributes: true, ConfirmOnUse: true,
            Description: "Use it to open one more subclass slot. Three are earned — your main's 4th "
                       + "class, then your first and second subclasses reaching level "
                       + $"{ThirdClassCatalog.SubclassLevel} — and the rest are bought from a class master."));

        // Return scrolls: same mechanism, but their skill has a CAST time, so double-clicking one
        // channels it. The skills are NOT learned — the ITEM is what grants them.
        // Sellable as of 2026-07-31 (playtest-15): it is tradable and drops constantly, so refusing it
        // at the vendor read as a bug. It sells through the use-consumable /25 rule (500 -> 20), not the
        // generic 30 %, so making it sellable does not re-open the faucet playtest-14 just closed.
        list.Add(new ItemDef(ScrollReturn, "Scroll of Return", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common,
            UseSkillId: SkillCatalog.ScrollReturnSkill, Value: 500));
        list.Add(new ItemDef(ScrollReturnUltimate, "Ultimate Scroll of Return", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            UseSkillId: SkillCatalog.ScrollReturnUltSkill,
            Tradable: false, BuyPriceOverride: -1, SellPriceOverride: 0));

        // Resurrection scrolls: used WHILE DEAD to self-revive (their skill channels a cast). Basic
        // restores no exp (1500g vendor); the ultimate restores all lost exp (not shop-stocked).
        // Sellable too, same rule and same reason as the Scroll of Return (1500 -> 60).
        list.Add(new ItemDef(ScrollResurrect, "Scroll of Resurrection", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon,
            UseSkillId: SkillCatalog.ScrollResurrectSkill, Value: 1500));
        // ===== THE ULTIMATE IS TRADABLE (`BL-59`, the third of his three parts) =====================
        // *"Ultimate Resurrection scrolls should be tradable — atleast the one that drop and from the
        // admin menu."* This is that one: the un-suffixed def is what drops and what `/give` hands out.
        //
        // 🔑 It costs the tutorial nothing, which is why it can simply be flipped. The completion kit
        // he wanted untradable is handed out as `ScrollResurrectUltimateBound`, a separate BoundCopies
        // clone — the same split the Newbie gear uses, where the plain id stays an ordinary item and
        // the `_bound` twin carries the restriction.
        //
        // ⚠ MINE, not his: the 15,000 Value. Tradable-but-refused-at-the-counter is the exact
        // complaint recorded on the Scroll of Return above, so leaving SellPriceOverride: 0 here would
        // recreate it one line down. 15,000 is 10x the basic scroll, i.e. 7,500 gold over the counter
        // at the half-price sell rule (`BL-287`; it was 600 under the old /25). BuyPriceOverride stays -1: no vendor STOCKS it, so this
        // opens no new faucet — it only lets players move the ones they find.
        list.Add(new ItemDef(ScrollResurrectUltimate, "Ultimate Scroll of Resurrection", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            UseSkillId: SkillCatalog.ScrollResurrectUltSkill,
            BuyPriceOverride: -1, Value: 15000));

        // Elemental Stone — a crafting/reagent material (not drinkable). Stacks; consumed
        // by skills that list it as a ConsumableId (nuker's Elemental Burst = 1/cast). Vendor 20k.
        list.Add(new ItemDef(ElementalStone, "Elemental Stone", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, Value: 100, BuyPriceOverride: 20000));

        // Holy Stone / Physical Stone — the divine and the fighter twins of the Elemental Stone
        // (owner, 2026-08-26: *"same as elemental"*). Identical grade, rarity, value and vendor price
        // on purpose; only the school that burns them differs.
        list.Add(new ItemDef(HolyStone, "Holy Stone", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, Value: 100, BuyPriceOverride: 20000));
        list.Add(new ItemDef(PhysicalStone, "Physical Stone", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, Value: 100, BuyPriceOverride: 20000));

        // Skill Stone — cheap reagent consumed by skills that cost stones (e.g. Angel's Protection = 5/cast).
        // Not free, not expensive: 400g at the vendor. Stacks; not drinkable.
        // ⚠ 9,999 A ROW, ALONE AMONG THE STONES (`BL-144`) — see StackLimits.Reagent for why: it is the
        // reagent of ORDINARY repeated casts (a heal, a whisp at four a call), while the elemental/holy/
        // physical stones are spent on set-pieces and stay at 99.
        list.Add(new ItemDef(SkillStone, "Skill Stone", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, Value: 400,
            MaxStackOverride: StackLimits.Reagent));

        // SP Bottle — a TRADABLE, SELLABLE store of skill points (owner, 2026-08-26). Bought from the
        // SP Broker for 1kkk SP + 100kk gold and worth 100kk at a shop, so it is the way a high-level
        // character banks surplus SP and hands it to someone who needs it. Drinking it grants its SP.
        // ⚠ It is NOT sold on any vendor shelf — the exchange NPC is the only source (BuyPriceOverride
        // is the shop's PRICE, which is what makes the sell value real; it is never on a shelf to buy).
        // 🔑 IT SELLS FOR WHAT IT COSTS — 100kk, not the /25 the consumable rule would give (4kk).
        //    His ruling, 2026-08-26: *"drinking potions gives 1kkk SP (0 gold) … crafting potion takes
        //    1kkk sp and 100kk gold, but drinking return 1kkk sp and selling return 100kk gold .. u
        //    cannot do both"*. That symmetry IS the item: the broker takes 1kkk SP **and** 100kk gold,
        //    and the bottle gives back exactly one of the two, your choice. At the /25 sell price the
        //    choice did not exist — selling was a 96% loss and nobody would ever do it.
        //    ⚠ NOT a gold faucet: no vendor STOCKS it (the broker is the only source), so the only way
        //    to sell one for 100kk is to have paid 100kk plus a billion SP to make it.
        list.Add(new ItemDef(SpBottle, "SP Bottle", EquipSlot.Consumable,
            ItemGrade.S, ItemRarity.Epic, Value: GameConstants.SpBottleShopPrice,
            BuyPriceOverride: GameConstants.SpBottleShopPrice,
            SellPriceOverride: GameConstants.SpBottleShopPrice,
            UseSkillId: SkillCatalog.SpBottleUse, ConfirmOnUse: true));

        // ----- Buff potions and scrolls: consume to gain one SINGLE buff off a family's ladder
        //       (docs/design/BuffLadders.md). Rarity IS the rung, so a plain potion supersedes a
        //       Lesser one — and equally supersedes the same rung of a cleric's blessing, because
        //       they are literally the same buff. No heal cooldown; 1s reuse.
        //       ⚠ Since playtest-17 E3 the two ladders are no longer parallel: the POTION is the
        //       found layer (rungs 1-2, 20 min) and the SCROLL is the bought layer (rung 3 or 6, one
        //       hour, Blessing Box only). A family therefore reads Lesser → plain → *scroll*, and the
        //       thing you pay for is always the thing at the top. -----
        list.Add(new ItemDef(SpeedPotionC, "Swift Potion (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.PotSwiftC, SellPriceOverride: 0, Value: 1500));
        list.Add(new ItemDef(SpeedPotionU, "Swift Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.PotSwiftU, SellPriceOverride: 0, Value: 5000));
        list.Add(new ItemDef(CastPotionC, "Alacrity Potion (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.PotAlacrityC, SellPriceOverride: 0, Value: 1500));
        list.Add(new ItemDef(CastPotionU, "Alacrity Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.PotAlacrityU, SellPriceOverride: 0, Value: 5000));
        list.Add(new ItemDef(AtkPotionC, "Fury Potion (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.PotHasteC, SellPriceOverride: 0, Value: 1500));
        list.Add(new ItemDef(AtkPotionU, "Fury Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.PotHasteU, SellPriceOverride: 0, Value: 5000));
        list.Add(new ItemDef(EvaPotionC, "Agility Potion (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.PotAgilityC, SellPriceOverride: 0, Value: 1500));
        list.Add(new ItemDef(EvaPotionU, "Agility Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.PotAgilityU, SellPriceOverride: 0, Value: 5000));

        // ----- The other buff ladders (BuffLadders.md step 6). Same shape as the four speed
        //       families above, so the prices are the same ladder: potion 1.5k/5k, scroll 36k.
        //       The SCROLL-ONLY families have no potion at all — for them the ONE scroll is the
        //       whole consumable ladder, which is what "no potion analogue" means now. -----
        // ⚠ SellPriceOverride: 0 on BOTH ladders (owner, playtest-18 V2b, 2026-08-05): *"buff pots are 0
        // sell (ppl still can sell them to others if they want)"*. He believed this was already done in
        // the playtest-17 vendor rework; it was not, and the ÷10 divisor had just made them 2.5x richer.
        // They drop on ~33 % of kills at level 33, which measured at ~188k of gold over a 14-15 h farm —
        // the entire remaining consumable faucet once gear was cut. Value stays: it is still the BUY
        // price, and player-to-player trade is untouched. The point is that a buff potion is something
        // you DRINK or trade, never a coin the vendor mints for you.
        void BuffPotion(string id, string name, ItemRarity rarity, string skill) =>
            list.Add(new ItemDef(id, name, EquipSlot.Consumable, ItemGrade.F, rarity,
                UseSkillId: skill, SellPriceOverride: 0, Value: rarity switch
                {
                    ItemRarity.Common => 1500, ItemRarity.Uncommon => 5000, _ => 12000,
                }));

        // ⚠ A buff scroll is BOX-ONLY and BOUND (playtest-17 E3): `Tradable: false` because the box it
        // came out of was the tradable thing, and there is no other source in the game — no drop, no
        // boss, no vendor shelf. Value stays as the notional worth (250k / 10 picks = 25k, rounded up
        // the ladder to the old Rare price) so the details panel can still print a number; nothing
        // buys one at that price. All 17 are Rare: one scroll per buff means rarity has no rungs left
        // to spell, and a wall of six colours for one effect was the thing he asked to end.
        void BuffScroll(string id, string name, string skill) =>
            list.Add(new ItemDef(id, name, EquipSlot.Consumable, ItemGrade.F, ItemRarity.Rare,
                UseSkillId: skill, SellPriceOverride: 0, Value: 36000, Tradable: false));

        BuffPotion(MightPotionC, "Might Potion (Lesser)",  ItemRarity.Common,   SkillCatalog.PotMightC);
        BuffPotion(MightPotionU, "Might Potion",           ItemRarity.Uncommon, SkillCatalog.PotMightU);

        BuffPotion(BulwarkPotionC, "Bulwark Potion (Lesser)",  ItemRarity.Common,   SkillCatalog.PotBulwarkC);
        BuffPotion(BulwarkPotionU, "Bulwark Potion",           ItemRarity.Uncommon, SkillCatalog.PotBulwarkU);

        BuffPotion(ForcePotionC, "Force Potion (Lesser)",  ItemRarity.Common,   SkillCatalog.PotForceC);
        BuffPotion(ForcePotionU, "Force Potion",           ItemRarity.Uncommon, SkillCatalog.PotForceU);

        BuffPotion(WardPotionC, "Ward Potion (Lesser)",  ItemRarity.Common,   SkillCatalog.PotWardC);
        BuffPotion(WardPotionU, "Ward Potion",           ItemRarity.Uncommon, SkillCatalog.PotWardU);

        // Aim — accuracy, the mirror of the Agility (evasion) line and priced identically.
        BuffPotion(AimPotionC, "Aim Potion (Lesser)",  ItemRarity.Common,   SkillCatalog.PotAimC);
        BuffPotion(AimPotionU, "Aim Potion",           ItemRarity.Uncommon, SkillCatalog.PotAimU);

        // ----- THE 17 SCROLLS. One per buff, top rung, no suffix in the name because there is no
        //       other rung to tell it apart from. The nine with a potion line take their family's
        //       rung 3; the eight scroll-only families take rung 6 — which is the NPC buffer's own
        //       value, so a boxed set is exactly a buffer's blessing for an hour. -----
        BuffScroll(SpeedScrollR,   "Scroll of Swift",    SkillCatalog.ScrSwiftR);
        BuffScroll(CastScrollR,    "Scroll of Alacrity", SkillCatalog.ScrAlacrityR);
        BuffScroll(AtkScrollR,     "Scroll of Fury",    SkillCatalog.ScrHasteR);
        BuffScroll(EvaScrollR,     "Scroll of Agility",  SkillCatalog.ScrAgilityR);
        BuffScroll(MightScrollR,   "Scroll of Might",    SkillCatalog.ScrMightR);
        BuffScroll(BulwarkScrollR, "Scroll of Bulwark",  SkillCatalog.ScrBulwarkR);
        BuffScroll(ForceScrollR,   "Scroll of Force",    SkillCatalog.ScrForceR);
        BuffScroll(WardScrollR,    "Scroll of Ward",     SkillCatalog.ScrWardR);
        BuffScroll(AimScrollR,     "Scroll of Aim",      SkillCatalog.ScrAimR);
        BuffScroll(BodyScrollM,     "Scroll of Body",     SkillCatalog.ScrBodyM);
        BuffScroll(SoulScrollM,     "Scroll of Soul",     SkillCatalog.ScrSoulM);
        BuffScroll(VigorScrollM,    "Scroll of Vigor",    SkillCatalog.ScrVigorM);
        BuffScroll(SerenityScrollM, "Scroll of Serenity", SkillCatalog.ScrSerenityM);
        BuffScroll(FocusScrollM,    "Scroll of Focus",    SkillCatalog.ScrFocusM);
        BuffScroll(FerocityScrollM, "Scroll of Ferocity", SkillCatalog.ScrFerocityM);
        BuffScroll(InsightScrollM,  "Scroll of Insight",  SkillCatalog.ScrInsightM);
        BuffScroll(FrenzyScrollM,   "Scroll of Frenzy",   SkillCatalog.ScrFrenzyM);
        // `BL-149` — NINETEEN now. Vampirism and Resolve were the only two blessings the NPC buffer
        // gave that nothing else in the game could, which `BL-150`'s cut-off at 75 would have turned
        // into "gone above 75 unless you know a Warchanter". Both take their family's TOP rung like
        // every other scroll here — rung 5 and rung 7, because those ladders are not six deep.
        BuffScroll(VampScrollM,     "Scroll of Vampirism", SkillCatalog.ScrVampM);
        BuffScroll(ResolveScrollM,  "Scroll of Resolve",   SkillCatalog.ScrResolveM);

        // Dash — 15 seconds of sprint on a 90-second reuse (`BL-249`), six rarities, no scroll. Priced at half
        // a buff potion of the same rarity: it is a burst, not a blessing.
        list.Add(new ItemDef(DashPotionC, "Dash Potion (Lesser)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common, UseSkillId: SkillCatalog.PotDashC, SellPriceOverride: 0, Value: 750));
        list.Add(new ItemDef(DashPotionU, "Dash Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon, UseSkillId: SkillCatalog.PotDashU, SellPriceOverride: 0, Value: 2500));
        list.Add(new ItemDef(DashPotionR, "Dash Potion (Greater)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare, UseSkillId: SkillCatalog.PotDashR, SellPriceOverride: 0, Value: 6000));
        list.Add(new ItemDef(DashPotionE, "Dash Potion (Superior)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Epic, UseSkillId: SkillCatalog.PotDashE, SellPriceOverride: 0, Value: 12500));
        list.Add(new ItemDef(DashPotionL, "Dash Potion (Grand)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Legendary, UseSkillId: SkillCatalog.PotDashL, SellPriceOverride: 0, Value: 25000));
        list.Add(new ItemDef(DashPotionM, "Dash Potion (Supreme)", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Mythic, UseSkillId: SkillCatalog.PotDashM, SellPriceOverride: 0, Value: 50000));

        // ===================================================================
        //  SHIELDS — equippable by any class (with a one-hand weapon), but only
        //  tanks make them matter via Shield Mastery passives. Base values are
        //  modest; passives/buffs scale them. Block = flat % damage reduction.
        // ===================================================================
        // The Wooden Shield is TRAINING kit — the same tier as the training leather and the broken
        // jewels, and authored in `docs/data/gear/gear_sets.csv` alongside them (him, 2026-08-12). It
        // sits BELOW the F ladder on every column, which is the rule the 2026-07-31 pass restored: you
        // start in gear that is worse than what drops. At its old 40 defence it equalled the F-tier
        // Ferrite Aegis and the first shield you could loot was never an upgrade.
        // ⚠ It was HAND-authored and the 0.59.1 block re-cut missed it — that is his 67m, "the wood
        // shield still caries 30% dmg reduction should be 10%". It is on the F column of the tier
        // profile now (shBlock/shReduce/shCrit/shEvaPen further down), so a hand-written starter can
        // never again out-mitigate the ladder, and its defence took the same 5x cut (35 -> 7).
        // ⚠ The Iron Shield is DELETED with the same ruling ("Iron sheld can go"): an E-grade one-off
        // that nothing sold, dropped or boxed. Between the training shield and the ladder there is
        // nothing to author.
        list.Add(new ItemDef(WoodenShield, "Wooden Shield", EquipSlot.Shield,
            ItemGrade.F, ItemRarity.Common,
            BlockChance: 0.10f, BlockReduction: 0.10f,
            ShieldCritDefense: 0.03f, ShieldEvasionPenalty: 3,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: TrainingGearPrice,
            NoAttributes: true,
            Description: "A strapped plank. It stops about as much as you would expect."));

        // 🔴 Iron Mace and Ash Wand DELETED (playtest-22) — two hand-authored F-grade 1H blunts from
        // before the ladder, off it and unreferenced by any shop, drop or box since the training
        // weapons replaced them. The ladder's own `mace_t*` / `wand_t*` are the 1H blunt line.

        // ===================================================================
        //  NEWBIE STARTER WEAPONS — handed out on character creation. They are
        //  UNTRADEABLE, sell for 0, and cannot be purchased (buy -1). A fighter gets
        //  all four melee/ranged options; a mage gets the staff. P.Atk / M.Atk per owner.
        // ===================================================================
        // ONE power number + channel factors (see ItemDef). Fighter weapons: power = P.Atk,
        // P×1.0 / M×0.6. The staff: power = its M.Atk, M×1.0 / P×0.6 (its P.Atk is nerfed —
        // a mage should not swing like a fighter now that the archetype multiplier is gone).

        // ===================================================================
        //  TRAINING tier — levels 1-10, the weakest gear in the game (owner). Roughly a QUARTER of the
        //  Newbie weapons' power, so a starting character cannot one-shot its way through the first zones.
        //  Buyable at 400g; untradeable and attribute-less like the rest of the starter kit.
        //
        //  The owner authored these as "P.Atk / M.Atk" pairs: sword 6/5, club 6/5, knives 5/5,
        //  wand 5/7 — and as of 2026-07-31 (playtest-15) BOTH numbers are authored, exactly
        //  as the level-tier ladder below already does (P -> AtkBonus, M -> MAtkBonus, factors 1.0).
        //
        //  This tier was simply left behind by the 2026-07-24 migration: it still carried one power
        //  number and reconstructed the M column with OffChannelFactor, which meant no training weapon
        //  ever showed an M.Atk line on its card and a mage's starter numbers were whatever the factor
        //  happened to produce. The old objection here — "daggers with MAtkFactor 1.0 cast as well as a
        //  staff" — does not apply to authored numbers: knives are 5/5 because that is what was written,
        //  not because a factor let the shared base leak through. Weapon identity now lives in the
        //  class passive and IsMagicWeapon, per the note on the ladder below.
        list.Add(new ItemDef(TrainingSword, "Training Sword", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Sword,
            AtkBonus: 6, MAtkBonus: 5,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: TrainingGearPrice, NoAttributes: true,
            Description: "Blunt-edged practice sword. The weakest blade there is — replace it as soon as you can."));
        // ⚠ THE TRAINING CLUB AND KNIVES ARE GONE (him, 2026-08-12, 63j/67t) — the same reasoning that
        // deleted the bow, applied to the rest: "Any fighter cen get trough with a sword. Other training
        // Club and training knives can be deleted." The training tier is the cheap kit that carries
        // anyone to level 10, not one weapon per playstyle; the real choice is the Newbie box at the
        // level-10 quest, which stocks all six. Two items now, one per base class.
        // ⚠ THE TRAINING BOW IS GONE (owner, 2026-08-11): "no staff, no 2h, no bow … the 'no' training
        // items can be removed. You don't need them to start playing." The training tier is deliberately
        // NOT one weapon per playstyle — it is the cheap kit that carries anyone to level 10, where the
        // real choice (the Newbie/Ferrite box) is made. An archer starts on knives and picks his bow up
        // at the level-10 quest. `training_bow` also carried the tier's one outlier number (11/5, twice
        // the melee P.Atk, at range), so it was the training weapon a new player was punished for not
        // taking. Removing the DEF is safe: PersistenceService drops bag rows whose def no longer
        // resolves, so a save holding one just loses it.
        //
        // The wand's +6 MaxMP is GONE (owner, 2026-07-31): the training tier is meant to be the weakest
        // gear in the game, and a stat no other training weapon carried made it the default pick.
        // Its P.Atk is 5, not 6 (owner, 2026-08-11 — and docs/Roadmap.md said 5/7 all along; the 6 was
        // never his number).
        list.Add(new ItemDef(TrainingWand, "Training Wand", EquipSlot.Weapon,
            ItemGrade.F, ItemRarity.Common, WeaponType: WeaponType.Blunt,
            AtkBonus: 5, MAtkBonus: 7,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: TrainingGearPrice, NoAttributes: true,
            Description: "An apprentice's wand. Poor in the hand, but it carries a spell."));

        // Training ARMOR — light (fighter) and robe (mage). No set bonus: the set line starts at Newbie.
        //
        // ⚠ These were RE-CUT (owner, 2026-07-30): light 53 → 35, robe 27 → 20, +MP unchanged. The old
        // numbers were the sum of an IG upper + lower body, taken from the TOP of the no-grade range,
        // while this ladder's F Common rung is 45 % of a MID no-grade set — so the starter armor sat
        // ABOVE the first armor you could loot and every early armor drop was a DOWNGRADE. The weapons
        // never had this problem: they were cut from IG's top no-grade weapon, which lines up with the
        // ladder, which is why only the armor moved.
        //
        // The rule this restores: you start in gear that is WORSE than what drops, and gear UP as you
        // play. Fixed on the STARTER side rather than by lifting the F rung, so the ladder keeps its one
        // rule (every quality is a fixed fraction of the authored Mythic piece). Defence is a small share
        // of survival at this level anyway — the owner has levelled a melee fighter wearing none.
        list.Add(new ItemDef(TrainingLeather, "Training Leather Armor", EquipSlot.Armor,
            ItemGrade.F, ItemRarity.Common, Weight: ArmorWeight.Light, ArmorSlot: ArmorSlot.Body,
            DefBonus: 35,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: TrainingGearPrice, NoAttributes: true,
            Description: "Scuffed practice leathers."));
        list.Add(new ItemDef(TrainingRobe, "Training Robe", EquipSlot.Armor,
            ItemGrade.F, ItemRarity.Common, Weight: ArmorWeight.Robe, ArmorSlot: ArmorSlot.Body,
            DefBonus: 20, MpBonus: 29,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: TrainingGearPrice, NoAttributes: true,
            Description: "A rough apprentice's robe."));

        // BROKEN jewels — the TRAINING rung of the jewel line, authored in `docs/data/gear/gear_sets.csv`
        // with the Wooden Shield (him, 2026-08-12: *"wooden is a training gear same as broken jewels
        // (put them inside the csv)"*). TRADABLE on purpose: these are the first thing a new player owns
        // that is worth anything, and selling them is the first bit of economy they touch. The owner's
        // "40g / 30g / 60g" is the SHOP price, so it goes on BuyPriceOverride; the sell-back value falls
        // out of the normal Value formula, as it does for every other tradable item.
        //
        // 🔴 M.Def CUT to his numbers: necklace 15 -> 9, earring 11 -> 5, ring 7 -> 3. They were sitting
        // ABOVE the F ladder they are supposed to sit below — F common/uncommon/rare runs necklace
        // 11/13/17, earring 7/8/11, ring 5/6/8, so broken beat F COMMON in every slot and beat F
        // UNCOMMON in two: *"Broken jewels (they are like 'train' gear) now have more than uncommon F
        // gear."* Same bug the training armor and the Wooden Shield had, in the one slot line that had
        // never been checked for it.
        list.Add(new ItemDef(BrokenEarring, "Broken Earring", EquipSlot.Jewel, ItemGrade.F, ItemRarity.Common,
            MDefBonus: 5, JewelType: JewelType.Earring, Value: 40, BuyPriceOverride: 40, NoAttributes: true,
            Description: "Cracked, but it still turns a little magic."));
        list.Add(new ItemDef(BrokenRing, "Broken Ring", EquipSlot.Jewel, ItemGrade.F, ItemRarity.Common,
            MDefBonus: 3, JewelType: JewelType.Ring, Value: 30, BuyPriceOverride: 30, NoAttributes: true,
            Description: "Bent out of shape, and worth a few coins."));
        list.Add(new ItemDef(BrokenNecklace, "Broken Necklace", EquipSlot.Jewel, ItemGrade.F, ItemRarity.Common,
            MDefBonus: 9, JewelType: JewelType.Necklace, Value: 60, BuyPriceOverride: 60, NoAttributes: true,
            Description: "The chain is mended with wire."));

        // ===================================================================
        //  BOXES / CHESTS — opened from inventory; contents roll the BoxCatalog table.
        // ===================================================================
        list.Add(new ItemDef(BoxNewbie, "Newbie Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(BoxTreasure, "Treasure Chest", EquipSlot.Box, ItemGrade.F, ItemRarity.Uncommon));
        // The Blessing Box — the ONLY source of a buff scroll (playtest-17 E3). 250k at the Apothecary
        // for a pick of 10 of the 17; two boxes cover every buff in the game. Deliberately NOT cheap:
        // an hour of a real buffer has to stay the better deal, and this is the offline substitute.
        // ⚠ The BOX is tradable and sells at Value ÷ 25 (his number, not the gear divisor); the scrolls
        // that come out of it are bound. So the market that exists is in boxes, not in blessings.
        // `BL-151` — 250k → 300k, and the price is DERIVED, not picked (owner, 2026-09-03: *"Buff box
        // price 250-> 300k twice as the cost per buff from npc but it gives you outside town buffs"*).
        // 300,000 ÷ 10 picks = 30,000 a blessing-hour, exactly twice the NPC buffer's 15,000
        // (`BL-150`). What the double buys is the thing the NPC cannot do: a scroll works in the field,
        // so you re-buff where you are standing instead of walking back to a town.
        // ⚠ The divisor is PickCount, not the number of options. `BL-149` widened the box to 19
        // scrolls and left the picks at 10, so this arithmetic is untouched by that.
        list.Add(new ItemDef(BoxBuffScrolls, "Blessing Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Rare,
            Value: 300000, SellPriceOverride: 10000, NoAttributes: true,
            Description: "Choose any 10 of the 19 buff scrolls. Scrolls taken from the box are bound to you."));
        list.Add(new ItemDef(BoxNewbieArmorLight, "Newbie Light Armor Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(BoxNewbieArmorRobe, "Newbie Robe Armor Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(BoxNewbieJewels, "Newbie Jewels Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(BoxNewbieWeapons, "Newbie Weapons Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true));
        list.Add(new ItemDef(BoxTrainingWeapons, "Training Weapons Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true,
            Description: "Pick one training weapon."));
        list.Add(new ItemDef(BoxTrainingArmorChoice, "Training Armor Box", EquipSlot.Box, ItemGrade.F, ItemRarity.Common,
            Tradable: false, SellPriceOverride: 0, BuyPriceOverride: -1, NoAttributes: true,
            Description: "Pick leather or robe."));

        // ===================================================================
        //  NEWBIE ARMOR (two sets) + JEWELS — no attributes, untradeable, sell 0, buy -1.
        //  Light set (fighter): +42 HP, +2% P.Def. Robe set (mage): +15% cast speed.
        //  Full set = body + helm + gloves + boots (set bonus from ArmorSetCatalog).
        // ===================================================================
        // Bodies — each grants its set's bonus (light/robe). They SHARE the accessories below.

        // ===================================================================
        //  JEWELS — the ONLY source of magic defence (beyond the level base).
        //  One jewel equips for now; the slot is built to expand to 5 later.
        // ===================================================================
        // 🔴 Brass Amulet and Silver Talisman DELETED — *"Brass amulet also need to be gone"*
        // (playtest-22). Both were pre-ladder hand-authored jewels: the Amulet was still on the
        // Outfitter's shelf at a value the F ladder already covers, and the Talisman was referenced by
        // nothing at all. The ladder supplies necklace/earring/ring at every grade, and the Broken
        // jewels (72a, 9/5/3) are the rung below F.

        // ===================================================================
        //  ENCHANT SCROLLS
        // ===================================================================
        // ----- The 18 scrolls (0.49.0, owner's D1 spec). Two axes, generated from the one table so
        //       a rung can never be authored into the drop table and forgotten here:
        //         TYPE  = what a FAILURE costs (Normal breaks / Greater −1 / Safe keeps it),
        //         GRADE = which gear it works on, and the RARITY is how that grade is signalled.
        //       There is deliberately no F scroll — F is the training tier you leave by 20. -----
        foreach (var (grade, rarity, priced, level, value) in EnchantScrollBands)
        {
            string letter = EnchantRules.GradeName(grade);
            foreach (var (kind, prefix, priceMul) in EnchantScrollTypes)
                list.Add(new ItemDef(EnchantScrollKey(kind, grade), $"{prefix} ({letter})",
                    EquipSlot.Scroll, priced, rarity,
                    ScrollKind: kind, ScrollGrade: grade, Value: value * priceMul,
                    Description: $"{letter} grade only (item level {level}+). Raises the enchant by "
                               + $"one on success. On failure: {EnchantRules.FailureText(kind)}."));
        }

        // ----- Attribute scrolls (0.45.0). A weapon or jewel drops BARE; a scroll is the only
        //       way it ever gains an attribute. Three scrolls cover the D-C-B stretch, a pair
        //       covers A, and one covers S. Each is locked to its grade band so a cheap scroll
        //       can never touch endgame gear. -----
        list.Add(new ItemDef(AttrScrollCommon, "Attribute Scroll (Common)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Common, AttrScroll: AttrScrollKind.Common,
            Description: "D, C or B grade. Gives the item a random attribute for its type, "
                       + "at a random value. Replaces whatever it had."));
        list.Add(new ItemDef(AttrScrollUncommon, "Attribute Scroll (Uncommon)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Uncommon, AttrScroll: AttrScrollKind.Uncommon,
            Description: "D, C or B grade. Keeps the attribute the item already has and "
                       + "re-rolls its value. The item must already have one."));
        list.Add(new ItemDef(AttrScrollRare, "Attribute Scroll (Rare)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Rare, AttrScroll: AttrScrollKind.Rare,
            Description: "D, C or B grade. Keeps the attribute and re-rolls its value in the "
                       + "TOP HALF of the range. The item must already have one."));
        list.Add(new ItemDef(AttrScrollEpic, "Attribute Scroll (Epic)", EquipSlot.Scroll,
            ItemGrade.A, ItemRarity.Epic, AttrScroll: AttrScrollKind.Epic,
            Description: "A grade only. Gives the item a random attribute for its type, at a "
                       + "random value. Replaces whatever it had."));
        list.Add(new ItemDef(AttrScrollLegendary, "Attribute Scroll (Legendary)", EquipSlot.Scroll,
            ItemGrade.A, ItemRarity.Legendary, AttrScroll: AttrScrollKind.Legendary,
            Description: "A grade only. Keeps the attribute and re-rolls its value in the TOP "
                       + "HALF of the range. The item must already have one."));
        list.Add(new ItemDef(AttrScrollMythic, "Attribute Scroll (Mythic)", EquipSlot.Scroll,
            ItemGrade.S, ItemRarity.Mythic, AttrScroll: AttrScrollKind.Mythic,
            Description: "S grade only. Gives the item a random attribute for its type, "
                       + "always at its MAXIMUM value."));

        // ===================================================================
        //  QUEST ITEMS — non-droppable, non-tradeable proofs for class changes.
        // ===================================================================
        list.Add(new ItemDef(MarkOfFaith, "Mark of Faith", EquipSlot.QuestItem,
            ItemGrade.F, ItemRarity.Rare));
        list.Add(new ItemDef(ClericsProof, "Cleric's Proof", EquipSlot.QuestItem,
            ItemGrade.F, ItemRarity.Epic));

        // ----- THE CRAFTER QUEST (`BL-273` part 2): four gathered materials, the hammer they make, and the
        //       40% quest recipe. The recipe is a Box (the bag's Use button learns it, like any recipe) but
        //       untradable and worthless: it teaches only the quest's own recipe.
        list.Add(new ItemDef(CrafterQuestWood, "Seasoned Hardwood", EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Common,
            Description: "Quest wood for the Master's trial."));
        list.Add(new ItemDef(CrafterQuestIron, "Raw Iron", EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Common,
            Description: "Quest iron for the Master's trial."));
        list.Add(new ItemDef(CrafterQuestGem, "Rough Gem", EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Common,
            Description: "A quest gem for the Master's trial."));
        list.Add(new ItemDef(CrafterHammerHead, "Hammer Head", EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Rare,
            Description: "The head of the hammer you must make for the Master."));
        list.Add(new ItemDef(CrafterHammer, "Blacksmith's Hammer", EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Rare,
            Description: "Your first craft. Give it to the Master to become a crafter."));
        list.Add(new ItemDef(CrafterQuestRecipe, "Recipe: Blacksmith's Hammer (40%)", EquipSlot.Box,
            ItemGrade.F, ItemRarity.Common, TeachesRecipeId: Crafting.HammerRecipeId,
            RecipePercent: Crafting.HammerRecipePercent, Tradable: false, SellPriceOverride: 0,
            Description: "The Master's trial recipe. Learn one and spend the other on the craft."));

        // ----- GATHERING TOKENS: the trophies the repeatable hunt quests collect. One token per
        //       creature, so the Huntmaster can pay a different QuestItemRewardModifier for each
        //       (see Quests.Repeatable.cs). Common rarity — they are proof of work, not treasure, and
        //       they carry no value: the whole reward is the exp+gold paid at turn-in.
        foreach (var (id, name) in GatherTokens)
            list.Add(new ItemDef(id, name, EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Common,
                Description: "A hunting trophy. Worth nothing to a merchant — bring it to the "
                           + "Huntmaster who asked for it."));

        // ===================================================================
        //  (The two GOD-TIER debug one-offs — God's Judgment / God's Robes — were DELETED
        //   2026-08-07 along with ItemRarity.God, playtest-19 `0b`. The owner's rule: *"nothing
        //   that can't be acquired in game"*. Their job — cosmic stats for testing — is done by
        //   `/enchant <value>` plus `/spd <m|a|c> <v>`, so those two commands are load-bearing now.)
        // ===================================================================
        //  CLASS-CHANGE PROOFS — two non-tradeable quest items per playable second
        //  class, awarded by its quest chain and consumed at the class change.
        // ===================================================================
        foreach (var cls in ClassCatalog.Playable)
        {
            list.Add(new ItemDef(ClassTokenId(cls.Id), $"{cls.Name} Trial Token",
                EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Rare));
            list.Add(new ItemDef(ClassProofId(cls.Id), $"{cls.Name}'s Proof",
                EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Epic));
        }
        // 3rd-class (discipline) proofs — same helpers, 3rd-class ids (101-136).
        foreach (var cls in ThirdClassCatalog.Playable)
        {
            list.Add(new ItemDef(ClassTokenId(cls.Id), $"{cls.Name} Ordeal Mark",
                EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Epic));
            list.Add(new ItemDef(ClassProofId(cls.Id), $"Seal of the {cls.Name}",
                EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Legendary));
        }
        // The 4th-class key. ONE item, not one per class: the 4th class is not a choice (a discipline
        // has exactly one ascension), so there is nothing for a per-class token to distinguish. Sold
        // at the Apothecary for 100kk and consumed at the 4th-class master; untradeable so it can't
        // be bought by a rich friend for a level-76 who has not earned the gold. SellPriceOverride 0
        // because a QuestItem is unsellable anyway (IsSellable) — stated so nobody reads the 100kk
        // BuyPrice and assumes a 40kk vendor refund.
        list.Add(new ItemDef(FourthClassKey, "Rite of Ascension",
            EquipSlot.QuestItem, ItemGrade.F, ItemRarity.Legendary,
            BuyPriceOverride: 100_000_000, SellPriceOverride: 0, Tradable: false));

        // ----- Level-tier gear (docs/data/gear/gear_sets.csv): weapons + base armor/shield/accessory/
        //       jewel pieces. SET BONUSES (and the dmg/support VARIANTS) come later; these carry only
        //       their own base stats via the existing equip rails, so no new mechanic to test. -----
        // The tiered gear pieces ARE the Mythic ("plain") items. From each T40-T61 base piece we also
        // generate its COMMON copy (`BL-272`): the same stats, but unmodifiable (no set, no attribute,
        // no enchant), so mobs can drop usable-now gear while the real piece stays the one you invest in.
        var tieredGear = TieredWeapons().Concat(TieredArmor()).ToList();
        list.AddRange(tieredGear);
        list.AddRange(CommonCopies(tieredGear));
        list.AddRange(Materials());
        list.AddRange(Parts(tieredGear));
        list.AddRange(Essences());
        list.AddRange(VolcanicMaterials());
        list.AddRange(RecipeBooks(tieredGear));
        // The tutorial chain's BOUND copies — the 30-day Newbie loaner kit and the completion
        // consumables. Generated last, off the finished list, so a clone always mirrors the real
        // item (see BoundCopies).
        list.AddRange(BoundCopies(list));
        // The temporary 2-hour Common gear and its vendor boxes (`BL-272` part 2), cloned off the Commons.
        list.AddRange(TempGear(list));
        // `BL-301` — LAST, so the bound copies' "(Bound)" goes too.
        for (int i = 0; i < list.Count; i++) list[i] = WithoutQualifier(list[i]);

        // ----- Duplicate-key guard + value fill: any item left at Value 0 gets the
        //       formula price (quest items / god one-offs stay 0 = not for trade). -----
        var dict = new Dictionary<string, ItemDef>();
        foreach (var raw in list)
        {
            var item = raw.Value > 0 ? raw : raw with { Value = DefaultValue(raw) };
            if (!dict.TryAdd(item.Id, item))
                throw new InvalidOperationException(
                    $"Duplicate item id '{item.Id}' ({item.Name} collides with {dict[item.Id].Name}).");
        }
        return dict;
    }

    /// <summary>The quality words `BL-301` takes out of item names when they trail it in brackets.
    /// A CLOSED list on purpose: "(Assault)" on an armour-set variant is identity, not quality.
    /// ⚠ A method, not a static set: the catalog is built from a static initializer ABOVE this point in
    /// the file, and a static field declared down here would still be null while it runs.</summary>
    private static bool IsQualifierWord(string w) => w is "Lesser" or "Greater" or "Superior" or "Grand"
        or "Supreme" or "Bound" or "Common" or "Uncommon" or "Rare" or "Epic" or "Legendary" or "Mythic";

    /// <summary>`BL-301` part 2 — *"Instant Healing Potion (Bound)", "Alacrity Potion (Lesser)", "(Supreme)"
    /// and any others like them"* come out of the name: *"by the color of the item and the quality/rarity u
    /// can understand what is it"*. Strips every trailing "(Word)" from <see cref="IsQualifierWord"/> and
    /// keeps the old full name as the <see cref="ItemDef.BarName"/>, the one place *"can differ"*. Whether an
    /// item is bound is now the row's (T/B/U) tag (<see cref="ItemTag.Letters"/>), not its name.</summary>
    private static ItemDef WithoutQualifier(ItemDef d)
    {
        string name = d.Name;
        while (name.EndsWith(")", StringComparison.Ordinal))
        {
            int open = name.LastIndexOf(" (", StringComparison.Ordinal);
            if (open < 0 || !IsQualifierWord(name.Substring(open + 2, name.Length - open - 3))) break;
            name = name.Substring(0, open);
        }
        // The one LEADING form: "Common / Uncommon / Rare Healing|Mana Potion".
        int space = name.IndexOf(' ');
        if (space > 0 && name.EndsWith(" Potion", StringComparison.Ordinal)
            && IsQualifierWord(name.Substring(0, space)))
            name = name.Substring(space + 1);
        if (name == d.Name) return d;
        return d with { Name = name, BarName = string.IsNullOrEmpty(d.BarName) ? d.Name : d.BarName };
    }

    /// <summary>Crafting MATERIALS (`BL-273` part 3, 0.205.0). The five BASE mats are one rung each (the old
    /// Uncommon…Mythic ladder is gone) and sell for 2.5 apiece. Above them sit the two refinable families,
    /// Nightsilver and Nightsilk, five rungs each at 10:1, and Alloy (20 gems + 20 iron). ⚠ The refinable
    /// Values are placeholders, ×10 a rung so that a refine neither makes nor loses coin; step 11 measures
    /// them against the drop.</summary>
    private static IEnumerable<ItemDef> Materials()
    {
        foreach (var type in Crafting.MaterialTypes)
            yield return new ItemDef(Crafting.MaterialId(type), Crafting.MaterialName(type),
                EquipSlot.Material, ItemGrade.F, ItemRarity.Common, Value: 5, NoAttributes: true);

        var rungRarity = new[] { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare, ItemRarity.Epic, ItemRarity.Legendary };
        int value = 20;
        for (int rung = 0; rung < Crafting.RefineRungs; rung++, value *= Crafting.RefineRatio)
        {
            string p = Crafting.RefineRungPrefix[rung];
            string tier = $"{TierLetter(Crafting.GearTiers[rung])}-grade";   // `BL-304`: grades, not tiers
            yield return new ItemDef(Crafting.NightsilverId(rung), $"{p}Nightsilver", EquipSlot.Material, ItemGrade.F,
                rungRarity[rung], Value: value, NoAttributes: true,
                Description: $"The smith's metal of the {tier} weapons, earrings and rings. 10 refine into 1 of the next rung.");
            yield return new ItemDef(Crafting.NightsilkId(rung), $"{p}Nightsilk", EquipSlot.Material, ItemGrade.F,
                rungRarity[rung], Value: value, NoAttributes: true,
                Description: $"The cloth of the {tier} armour, shields and necklaces. 10 refine into 1 of the next rung.");
        }
        yield return new ItemDef(Crafting.AlloyId, "Alloy", EquipSlot.Material, ItemGrade.F, ItemRarity.Common,
            Value: 200, NoAttributes: true, Description: "Refined from 20 gems and 20 iron. Every gear recipe takes some.");
    }

    /// <summary>The PARTS (the note's "heads", step 10): one per gear KIND per crafted tier, 18 a tier, e.g.
    /// "Darksteel Maul Head". A part's Value is <see cref="Crafting.PartPriceFraction"/> (1%) of its full item's
    /// price since `BL-274` part 1. It was the Common's price (the note: *"the head costs same as the common
    /// item"*), which assumed a 0.1% drop; at the ruled 1% it made 20 heads outsell the finished item.</summary>
    private static IEnumerable<ItemDef> Parts(IEnumerable<ItemDef> tiered)
    {
        foreach (var d in tiered)
        {
            if (d.Rarity != ItemRarity.Mythic || !IsBaseTier(d.Id) || !Crafting.IsGearSlot(d.Slot)) continue;
            if (Array.IndexOf(Crafting.GearTiers, d.ItemLevel) < 0) continue;
            string kind = Crafting.GearKind(d.Id);
            if (!Crafting.PartNames.TryGetValue(kind, out var noun)) continue;
            yield return new ItemDef(Crafting.PartId(kind, d.ItemLevel), $"{GradeTheme(d.ItemLevel)} {noun}",
                EquipSlot.Material, d.Grade, ItemRarity.Rare,
                Value: Math.Max(1, (int)Math.Round(DefaultValue(d) * Crafting.PartPriceFraction)), NoAttributes: true,
                Description: $"The main part of a {d.Name}. Every {GradeTheme(d.ItemLevel)} recipe of its kind takes some.");
        }
    }

    /// <summary>The five grade ESSENCES (`BL-273` part 1): what breaking gear gives
    /// (<see cref="Crafting.BreakYield"/>). Stackable and tradable; <b>no vendor sells one</b> (*"no vendor
    /// sells essence"*, so it is unbuyable). `BL-287`: its Value is TWICE <see cref="Crafting.EssenceSellPrice"/>,
    /// so the ordinary half-price sell rule pays exactly his 1500 … 25000 with no override.
    /// ⚠ Rarity is Common for all five: it is a grade material, not a rarity ladder. When essence starts to
    /// DROP (`BL-274` step 12), a pickup filter set above Common would skip it; decide that there.</summary>
    /// <summary>The three volcanic materials (see <see cref="VolcanicAsh"/>). ⚠ Values are placeholders until
    /// step 11 measures the drop: ash and stone 5,000, the bar 200,000 (its 20 + 20 inputs at cost).</summary>
    private static IEnumerable<ItemDef> VolcanicMaterials()
    {
        yield return new ItemDef(VolcanicAsh, "Volcanic Ash", EquipSlot.Material, ItemGrade.F, ItemRarity.Rare,
            Value: 5_000, NoAttributes: true, Description: "Found on creatures of the fire lands (76+).");
        yield return new ItemDef(VolcanicStone, "Volcanic Stone", EquipSlot.Material, ItemGrade.F, ItemRarity.Rare,
            Value: 5_000, NoAttributes: true, Description: "Found on creatures of the fire lands (76+).");
        yield return new ItemDef(VolcanicBar, "Volcanic Bar", EquipSlot.Material, ItemGrade.F, ItemRarity.Epic,
            Value: 200_000, NoAttributes: true, Description: "Refined from volcanic ash and stone. A- and S-grade crafting.");
    }

    private static IEnumerable<ItemDef> Essences()
    {
        for (int g = 0; g < Crafting.EssenceIds.Length; g++)
        {
            yield return new ItemDef(Crafting.EssenceIds[g],
                $"{GradeTheme(Crafting.EssenceItemLevels[g])} Essence",
                EquipSlot.Material, ItemGrade.F, ItemRarity.Common,
                Value: 2 * Crafting.EssenceSellPrice[g], BuyPriceOverride: -1,
                NoAttributes: true,
                Description: $"Broken down from {TierLetter(Crafting.EssenceItemLevels[g])}-grade gear. "
                           + "The grade's crafting essence; no merchant sells it.");
        }
    }

    // ===================================================================
    //  EQUIPMENT RARITY = COMMON + MYTHIC (`BL-272`, 2026-09-23)
    // ===================================================================
    // The six-rung quality ladder for equipment (45/55/70/70/85/100 % power, set + attributes from Epic
    // up) is GONE. His ruling: *"equipment will have common equip items and normal (current mythic)
    // items -> no longer in between"*. What is left:
    //   * MYTHIC: the authored piece, bare id. Shown to the player as a PLAIN item (no rarity word).
    //   * COMMON: `{id}_common`, T40-T61 only, the SAME stats as the Mythic piece, and UNMODIFIABLE:
    //     *"u cannot add attribute to weapons nor have set bonus nor can enchant -> its just flat
    //     defense"*. A fresh Mythic is ahead from day one through its set and its attribute slot.
    // Consumables and materials keep all six rarities; this is about equipment only.

    /// <summary>The item-level window a COMMON copy exists in: T40, T52 and T61 (*"common equip are only
    /// available T40 ~ T61"*). Above it, T76+ essence drops directly instead (`BL-273`).</summary>
    public const int CommonMinLevel = 40, CommonMaxLevel = 61;

    /// <summary>Does a Common copy exist at this item level?</summary>
    public static bool HasCommonTier(int itemLevel) =>
        itemLevel >= CommonMinLevel && itemLevel <= CommonMaxLevel;

    /// <summary>Is this a COMMON piece of equipment, the unmodifiable kind? The one test the enchant gate
    /// (server and the client's picker) asks. Attributes and sets need no gate: a Common is generated
    /// with <c>NoAttributes</c> and an empty <c>SetId</c>.</summary>
    public static bool IsCommonGear(ItemDef def) =>
        def.Rarity == ItemRarity.Common && Crafting.IsGearSlot(def.Slot);

    /// <summary>The rarity WORD a player is shown. Equipment speaks the collapsed vocabulary: a Mythic
    /// piece is a plain item and says nothing (*"equipment 'Mythic' becomes plain items; the reduced ones
    /// are 'Common' items"*, `BL-274` answer 3); everything else keeps its six-rung word.
    /// "" = show no word.</summary>
    public static string RarityLabel(ItemDef def) =>
        Crafting.IsGearSlot(def.Slot)
            ? (def.Rarity == ItemRarity.Common ? "Common" : "")
            : def.Rarity.ToString();

    // (`SGradeOverA` = 1.60 — DELETED 2026-08-11. S was derived from A by this one number; he has now
    //  authored the whole level-80 column by hand for weapons, armor, shields, accessories and jewels,
    //  and the authored numbers are a CUT against the derivation everywhere. A constant claiming S is
    //  A × 1.6 would now be a lie, and a lie a future retune would act on.)

    /// <summary>The level S gear is built for: 80+, sitting above A's 76-80 window.</summary>
    public const int SGradeLevel = 80;

    /// <summary>The id of a gear piece at another QUALITY, given the AUTHORED (Mythic) piece's id.
    /// Mythic is the authored item and carries no suffix; the only other rung equipment has since
    /// `BL-272` is Common, "{id}_common", and only at T40-T61. Returns the input unchanged if the
    /// resulting id is not a real item, so a caller can never hand out a phantom.</summary>
    public static string QualityId(string mythicId, ItemRarity rarity)
    {
        if (rarity == ItemRarity.Mythic) return mythicId;
        string id = $"{mythicId}_{rarity.ToString().ToLowerInvariant()}";
        return Get(id) is null ? mythicId : id;
    }

    /// <summary>True for a plain base-tier id like "heavy_t52" (the part after the last "_t" is all
    /// digits) — excludes alternate variants like "heavy_t52_dmg".</summary>
    public static bool IsBaseTier(string id)
    {
        int i = id.LastIndexOf("_t", StringComparison.Ordinal);
        if (i < 0) return false;
        string tail = id.Substring(i + 2);
        // The LOW sets of a grade share the grade's equip level with the top set, so their id carries a
        // "lo" suffix after the level (e.g. sword1h_t20lo = Low E, alongside sword1h_t20 = Top E). Strip it
        // so they still count as base tiers and get their own rarity drop copies.
        if (tail.EndsWith("lo", StringComparison.Ordinal)) tail = tail[..^2];
        return tail.Length > 0 && tail.All(char.IsDigit);
    }

    /// <summary>Gear RECIPE items (`BL-273` part 2, 0.203.0): one per craftable gear piece (T40-T80 Mythic)
    /// and per recipe % that something gives out at its tier (<see cref="Crafting.RecipePercentsFor"/>).
    /// Using one from the bag LEARNS it into a slot (anywhere); crafting at a Master SPENDS one. Derived
    /// from the tiered gear here, NOT from RecipeCatalog, to avoid a circular static-init with the recipe
    /// catalog, which itself reads ItemCatalog.AllItems. Tradable: recipes are what crafters buy and sell.
    ///
    /// <para>Priced by <see cref="Crafting.RecipePrice"/>: 10% of the piece's own buy price × its % (owner,
    /// 2026-09-24: *"a 20% recipe will cost 2%"*); only the Master's T40/T52 100% shelf actually sells them.</para></summary>
    private static IEnumerable<ItemDef> RecipeBooks(IEnumerable<ItemDef> tiered)
    {
        foreach (var d in tiered)
        {
            if (d.ItemLevel < Crafting.MinCraftedGearLevel || d.Rarity != ItemRarity.Mythic) continue;
            if (!Crafting.IsGearSlot(d.Slot)) continue;
            string recipeId = $"craft_{d.Id}";
            // ⚠ d.Value is still 0 here: tiered gear is priced by the Value pass AFTER the list is built, so
            // ask DefaultValue directly (BoundCopies does the same, for the same reason).
            int piece = d.Value > 0 ? d.Value : DefaultValue(d);
            foreach (int pct in Crafting.RecipePercentsFor(d.ItemLevel))
                yield return new ItemDef(RecipeBookId(recipeId, pct), $"Recipe: {d.Name} ({pct}%)",
                    EquipSlot.Box, d.Grade, ItemRarity.Common,
                    Value: Crafting.RecipePrice(piece, pct), TeachesRecipeId: recipeId, RecipePercent: pct, ItemLevel: 0,
                    Description: $"Use it to learn the {d.Name} recipe at {pct}% (it takes a recipe slot). "
                               + "Each craft at a Master spends one recipe of this % or lower.");
        }
    }

    /// <summary>The BOUND copies (playtest-19 M6 + M5's completion kit): the 30-day Newbie loaner
    /// gear and the untradable finishing consumables.
    ///
    /// <para>Every one is a CLONE of the real item — <c>d with { … }</c> — so not one number is
    /// authored here and the gear CSV stays the only source of the kit's stats. What changes is the
    /// id, the name, tradability, the prices, and (gear only) the 30-day <see
    /// cref="ItemDef.LifetimeSeconds"/>. The <c>SetId</c> is deliberately KEPT: a loaner body must
    /// still complete its armour set, and mixing a loaner piece with a real Ferrite one should work
    /// too — this is a starter kit, not a separate item line.</para>
    ///
    /// <para>Returns a materialized list: the caller feeds it the very list it is appending to.</para>
    /// </summary>
    private static List<ItemDef> BoundCopies(List<ItemDef> all)
    {
        var by = all.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var made = new List<ItemDef>();

        // The loaner GEAR: bound, worthless, unbuyable, and it evaporates after 30 days.
        string[] kit =
        {
            NewbieSword1H, NewbieDaggers, NewbieSword2H, NewbieBow, NewbieStaff, "wand_t1",
            NewbieLightBody, NewbieRobeBody, NewbieHelm, NewbieGloves, NewbieBoots,
            NewbieEarring, NewbieRing, NewbieNecklace,
        };
        foreach (var baseId in kit)
        {
            if (!by.TryGetValue(baseId, out var d)) continue;
            made.Add(d with
            {
                Id = BoundId(baseId),
                BarName = $"Newbie {d.Name}",   // `BL-301`: the (T/B) tag says it now, the name does not
                Tradable = false,
                BuyPriceOverride = -1,
                SellPriceOverride = 0,
                LifetimeSeconds = NewbieKitLifetimeSeconds,
            });
        }

        // The COMPLETION kit: bound, but no clock — finishing the chain earns them outright.
        string[] finishers = { ScrollReturnUltimate, ScrollResurrectUltimate, DashPotionM, InstantPotion };
        foreach (var baseId in finishers)
        {
            if (!by.TryGetValue(baseId, out var d)) continue;
            made.Add(d with
            {
                Id = BoundId(baseId),
                // `BL-301`: the SAME name as the item it copies; the row's (B) tag tells the two stacks
                // apart, and the bar keeps its own label through BarName.
                BarName = $"{(string.IsNullOrEmpty(d.BarName) ? d.Name : d.BarName)} (Bound)",
                Tradable = false,
                BuyPriceOverride = -1,
                SellPriceOverride = 0,
            });
        }

        return made;
    }

    /// <summary>The temporary 2-hour Common gear and the four kinds of box that sell it (`BL-272` part 2;
    /// the rules are on <see cref="TempGearWornSeconds"/>). Every piece is its Common copy with a new id,
    /// no trade, no shelf price and the worn clock. The two SHELF boxes per tier are priced off the Commons
    /// they hold, at build time, so a Common retune moves the box with it; the three set boxes are what the
    /// armour box's pick hands you and have no price of their own. Throws if a stem names no Common piece,
    /// so a renamed line fails at boot instead of shipping an empty box.</summary>
    private static List<ItemDef> TempGear(List<ItemDef> all)
    {
        var by = all.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var made = new List<ItemDef>();

        ItemDef Common(string mythicId) =>
            by.TryGetValue(mythicId + "_common", out var c) ? c
            : throw new InvalidOperationException($"Temporary gear: no Common copy of '{mythicId}'.");
        // The Common's shelf price. Its Value is still 0 here (DefaultValue fills it after the build),
        // so ask the same formula the dictionary will.
        int CommonPrice(string mythicId)
        {
            var c = Common(mythicId);
            return BuyPrice(c.Value > 0 ? c : c with { Value = DefaultValue(c) });
        }

        foreach (int tier in TempGearTiers)
        {
            string grade = GradeTheme(tier);
            foreach (var stem in TempWeaponStems.Concat(TempArmorWeights).Concat(TempArmorShared))
            {
                var c = Common($"{stem}_t{tier}");
                made.Add(c with
                {
                    Id = TempId($"{stem}_t{tier}"),
                    Tradable = false,
                    BuyPriceOverride = -1,
                    SellPriceOverride = 0,
                    WornLifetimeSeconds = TempGearWornSeconds,
                });
            }

            // The weapon box costs one Common TWO-HANDER (his example: *"a T40 temporary 2H = 214k"*),
            // whichever weapon is picked.
            made.Add(new ItemDef(TempWeaponBoxId(tier), "Temporary Common Weapon Selection Box", EquipSlot.Box,
                ItemGrade.B, ItemRarity.Common, Tradable: false, BuyPriceOverride: CommonPrice($"sword2h_t{tier}"),
                SellPriceOverride: 0, NoAttributes: true,
                Description: $"Choose ONE {grade} weapon. It is a Common piece that lasts 2 hours of WEARING: "
                           + "the clock stops while it is unequipped or you are offline. Cannot be traded, "
                           + "sold or broken."));

            // The armour box costs the whole Common set: body + helm + gloves + boots + shield. All three
            // bodies are one price, so the pick does not change it; heavy is read as the representative.
            int setPrice = CommonPrice($"heavy_t{tier}")
                         + TempArmorShared.Sum(s => CommonPrice($"{s}_t{tier}"));
            made.Add(new ItemDef(TempArmorBoxId(tier), "Temporary Common Armor Selection Box", EquipSlot.Box,
                ItemGrade.B, ItemRarity.Common, Tradable: false, BuyPriceOverride: setPrice,
                SellPriceOverride: 0, NoAttributes: true,
                Description: $"Choose ONE {grade} set: heavy, light or robe. Each is body, helmet, gloves, "
                           + "boots and a shield, Common pieces that last 2 hours of WEARING. Cannot be "
                           + "traded, sold or broken."));

            foreach (var weight in TempArmorWeights)
            {
                string body = Common($"{weight}_t{tier}").Name;
                made.Add(new ItemDef(TempSetBoxId(weight, tier), $"Temporary {body} Set", EquipSlot.Box,
                    ItemGrade.B, ItemRarity.Common, Tradable: false, BuyPriceOverride: -1,
                    SellPriceOverride: 0, NoAttributes: true,
                    Description: $"Opens into a temporary {body}, helmet, gloves, boots and shield "
                               + "(2 hours of wearing each)."));
            }
        }
        return made;
    }

    /// <summary>The COMMON copy of every T40-T61 base-tier piece (`BL-272`). Same stats as the authored
    /// Mythic piece; the difference is what you can DO to it: no set id, no attribute, no enchant (the
    /// enchant gate reads <see cref="IsCommonGear"/>). Only plain base-tier ids get one; the alternate
    /// body VARIANTS (e.g. "heavy_t52_dmg") stay Mythic-only. Id: "{baseid}_common".</summary>
    private static IEnumerable<ItemDef> CommonCopies(IEnumerable<ItemDef> tiered)
    {
        foreach (var d in tiered)
        {
            if (!Crafting.IsGearSlot(d.Slot)) continue;
            if (!IsBaseTier(d.Id) || !HasCommonTier(d.ItemLevel)) continue;
            // The quality is NOT in the name (owner). "Common Electrum Longbow" became a different item's
            // name in the player's head; the piece is an Electrum Longbow and its quality is a property,
            // shown by the name's COLOUR and a Rarity: row in the description.
            yield return d with
            {
                Id = d.Id + "_common",
                Rarity = ItemRarity.Common,
                SetId = "",
                NoAttributes = true,
                Value = 0,             // filled from DefaultValue (the Common price multiplier)
            };
        }
    }

    /// <summary>Display letter for a gear LEVEL tier (20/40/52/61/76/80 → E/D/C/B/A/S). Cosmetic —
    /// the item's <see cref="ItemDef.ItemLevel"/> drives the mechanics, not the letter.
    ///
    /// <para>⚠ The S rung is not decoration. Without it a level-80 Soulcrystal piece printed "Grade: A"
    /// in item details while every banded SYSTEM — <see cref="AttributeSystem.TierOf"/>,
    /// <see cref="EnchantRules.GradeOf(int)"/>, <see cref="GradePenalty"/> — already called it S, so the
    /// details window and the scroll that fits it named two different grades (playtest-17 `B8`).
    /// It deliberately stops at "S": there is no S*/S** here, because no banded system has those rungs
    /// and inventing a letter no scroll answers to is the very bug this fixes.
    /// <see cref="GradeTheme"/>'s Starstone/Seraphite are NAME themes, not grades.</para></summary>
    public static string TierLetter(int level) =>
        level >= 80 ? "S" : level >= 76 ? "A" : level >= 61 ? "B" : level >= 52 ? "C" : level >= 40 ? "D"
        : level >= 20 ? "E" : "F";

    /// <summary>THE grade a player is shown for an item (`BL-289`) — every UI line and chat message that
    /// prints a grade reads this, never <see cref="ItemDef.Grade"/> (that enum has no C/D and is a
    /// pricing/sorting input only; every non-gear def carries its default, which is how a temporary box
    /// read "B-grade"). Gear = its grade letter; a box = the grade of the GEAR inside it, derived from
    /// the contents ("D", or "C/D" if it mixes); an essence = its grade (below); everything else — potions,
    /// scrolls, other mats, runes, quest items, boxes of non-gear — is <see cref="NoGrade"/>. His words:
    /// *"most won't have any"*.
    ///
    /// <para>`BL-309` (owner, 2026-09-26): an ESSENCE carries the grade it is the essence OF — *"Darksteel
    /// essence is D grade ... They represent essence for each grade"* — read off
    /// <see cref="Crafting.EssenceItemLevels"/>, the same level its break-down source is graded by.</para></summary>
    public static string GradeLabel(ItemDef def)
    {
        if (Crafting.IsGearSlot(def.Slot)) return GradePenalty.GradeNameOf(def);
        int essence = Array.IndexOf(Crafting.EssenceIds, def.Id);
        if (essence >= 0)
            return GradePenalty.GradeNames[GradePenalty.StepForLevel(Crafting.EssenceItemLevels[essence])];
        if (def.Slot != EquipSlot.Box) return NoGrade;
        var steps = new SortedSet<int>();
        CollectBoxGrades(def.Id, steps, depth: 0);
        return steps.Count == 0 ? NoGrade
            : string.Join("/", steps.Reverse().Select(s => GradePenalty.GradeNames[s]));
    }

    /// <summary>What <see cref="GradeLabel"/> prints for an item with no grade.</summary>
    public const string NoGrade = "-";

    /// <summary>True when <see cref="GradeLabel"/> has a letter to show.</summary>
    public static bool HasGrade(ItemDef def) => GradeLabel(def) != NoGrade;

    private static void CollectBoxGrades(string boxId, SortedSet<int> steps, int depth)
    {
        // Boxes nest (the temporary armour box holds three set boxes); 4 levels is far past any real one.
        if (depth > 4 || BoxCatalog.Get(boxId) is not { } box) return;
        foreach (var e in box.Entries)
        {
            if (Get(e.ItemId) is not { } d) continue;
            if (Crafting.IsGearSlot(d.Slot)) steps.Add(GradePenalty.StepForLevel(GradePenalty.ItemGradeLevel(d)));
            else if (d.Slot == EquipSlot.Box) CollectBoxGrades(d.Id, steps, depth + 1);
        }
    }

    /// <summary>The grade's MATERIAL name — the display prefix that signals grade at a glance (owner
    /// 2026-07-25). Each starts with the grade's LETTER as a mnemonic (D→Darksteel, A→Adamantine…). The
    /// item name is "{GradeTheme} {noun}", e.g. "Darksteel Warplate". S/S*/S** are ready for the endgame
    /// CSV. Retune any word here — it changes every item of that grade at once.</summary>
    public static string GradeTheme(int itemLevel) => itemLevel switch
    {
        >= 85 => "Seraphite",       // S**
        >= 83 => "Starstone",       // S*
        >= 80 => "Soulcrystal",     // S
        >= 76 => "Adamantine",      // A
        >= 61 => "Bloodsteel",      // B
        >= 52 => "Cobalt",          // C
        >= 40 => "Darksteel",       // D
        >= 20 => "Electrum",        // E
        _     => "Ferrite",         // F
    };

    // Enum grade for pricing/sorting only (the enum has no C/D). ItemLevel is the real tier.
    // ⚠ The S rung is NOT optional cosmetics: without it the Soulcrystal/Starstone/Seraphite
    // items (levels 80/83/85) came out labelled A grade, and every grade-banded system that
    // reads the enum — pricing, the attribute scrolls — put them in the wrong band.
    private static ItemGrade TierGrade(int level) =>
        level >= 80 ? ItemGrade.S : level >= 61 ? ItemGrade.A : level >= 40 ? ItemGrade.B
        : level >= 20 ? ItemGrade.E : ItemGrade.F;

    /// <summary>The F tier's level. F-grade is now part of the ONE ladder rather than a separate
    /// "(Lesser)" line, and its MYTHIC rung is deliberately the old Newbie gear's power — the newbie
    /// kit IS the top of F grade (owner, 2026-07-30), not a parallel item set beside it.</summary>
    public const int FGradeLevel = 1;

    /// <summary>The level-tier weapons from docs/data/gear/gear_sets.csv — id "<key>_t<level>", base
    /// P.Atk/M.Atk straight from the CSV (the two numbers), bow attack-speed variants, and the
    /// IsMagicWeapon flag on wands/staves (their attributes roll the caster pool). Attribute COUNT
    /// + MAX come from the level (AttributeSystem tiered methods), not grade/rarity.</summary>
    private static IEnumerable<ItemDef> TieredWeapons()
    {
        var weapons = new (string Key, string Noun, WeaponType Type, bool Magic, float Range,
            (int L, int P, int M, int As)[] Rows)[]
        {
            // The FIRST row of each is the F tier (level 1), whose Mythic rung is the old Newbie gear's
            // power — so the newbie kit is the top of F grade instead of a parallel item line.
            //
            // ⚠ THE WHOLE F ROW WAS RE-AUTHORED BY HIM on 2026-08-11 (playtest-20). He gave the six
            // pairs outright: "staff 23/24, wand 22/23, 2h 29/17, 1h 24/17, bow 49/17, dagg 21/17".
            // Two things changed shape, not just value: every FIGHTER weapon now shares M.Atk 17 at F
            // (it was a flat 14), which is the same "one M.Atk column for the whole grade" the E-A rows
            // already have; and the two CASTER weapons crossed over — a wand/staff's M.Atk is now
            // ABOVE its P.Atk (22/23 and 23/24), where before the F rung had them below it.
            //
            // ⚠ THE WHOLE S ROW IS NOW AUTHORED TOO (him, 2026-08-11, gear_sets.csv). Every table below
            // carries its own level-80 row and nothing is derived any more — see the loop's note.
            ("sword1h", "Blade",      WeaponType.Sword,          false, 0,
                new[] { (1,24,17,0),(20,92,54,0),(40,156,83,0),(52,194,99,0),(61,232,114,0),(76,281,132,0),(80,437,192,0) }),
            // ⚠ THE ×1.166 2H P.ATK RAISE IS REVERTED (his ratification, 2026-08-11). I raised these on
            // 2026-08-10 to hold the 2H's pre-speed-ruling DPS, and wrote the result into HIS gear CSV —
            // which is why it was owed a yes/no. The answer is no: he re-gave the line by grade,
            // "38/21, 112/54, 190/83, 236/99, 282/114, 342/132, 532/192", and every P.Atk in it is the
            // ORIGINAL number. So the speed ruling keeps its cost: a 2H really is ~14% less DPS than it
            // was, and the Maul really does sit only ~4% above a one-hander. That is now a stated
            // outcome rather than an accident — do NOT "restore" it again without asking.
            //
            // (The 38/21 F row he opened that table with is superseded by the per-weapon F list above:
            // 2h = 29/17. The two messages are the same pass, the second is the specific one.)
            //
            // The S row here is AUTHORED, not derived — see the loop below.
            ("sword2h", "Greatsword", WeaponType.TwoHandedSword, false, 0,
                new[] { (1,29,17,0),(20,112,54,0),(40,190,83,0),(52,236,99,0),(61,282,114,0),(76,342,132,0),(80,532,192,0) }),
            ("blunt1h", "Mace",       WeaponType.Blunt,          false, 0,
                new[] { (1,24,17,0),(20,92,54,0),(40,156,83,0),(52,194,99,0),(61,232,114,0),(76,281,132,0),(80,437,192,0) }),
            ("blunt2h", "Maul",       WeaponType.TwoHandedBlunt, false, 0,
                new[] { (1,29,17,0),(20,112,54,0),(40,190,83,0),(52,236,99,0),(61,282,114,0),(76,342,132,0),(80,532,192,0) }),
            // ⚠ The A rung dropped 271 -> 246 (him, 2026-08-11). Duals were the ONE fighter line whose A
            // P.Atk did not sit on the shared 1H/2H shape; 246 puts it back below the sword's 281 by the
            // same margin the lower rungs use.
            ("duals",   "Fangs",      WeaponType.Dual,           false, 0,
                new[] { (1,21,17,0),(20,80,54,0),(40,136,83,0),(52,170,99,0),(61,203,114,0),(76,246,132,0),(80,382,192,0) }),
            // The level-40 bow is ONE item again: he deleted the 316 row and told the 323 row to ship at
            // as:293 (him, 2026-08-11 — "Remove (this was the very slow bow)" / "Build this => as:293").
            // ⚠ His parenthetical has the two the wrong way round: 316 was the SLOW (293) rung that
            // shipped and 323 was the very-slow (227) one that never did. The instruction is unambiguous
            // either way, and its outcome is the same in both readings: 323/84 at as:293.
            ("bow",     "Longbow",    WeaponType.Bow,            false, 400,
                new[] { (1,49,17,293),(20,191,55,293),(40,323,84,293),(52,400,99,293),(61,528,114,227),(76,581,132,293),(80,794,192,293) }),
            ("wand",    "Wand",       WeaponType.Blunt,          true,  0,
                new[] { (1,19,22,0),(20,74,72,0),(40,111,101,0),(52,155,132,0),(61,186,152,0),(76,225,175,0),(80,360,256,0) }),
            ("staff",   "Battlestaff",WeaponType.TwoHandedBlunt, true,  0,
                new[] { (1,23,24,0),(20,90,79,0),(40,135,111,0),(52,189,145,0),(61,226,167,0),(76,274,193,0),(80,426,281,0) }),
        };
        // BOTH CSV numbers are authored now (owner, 2026-07-24): P -> AtkBonus, M -> MAtkBonus. Until
        // this, only ONE of the pair survived — a fighter weapon kept P and threw M away, a magic weapon
        // kept M and threw P away — and the discarded channel was reconstructed by multiplying the WHOLE
        // finished channel by OffChannelFactor. That hid the second number from the item card entirely
        // (no weapon in the game set MAtkBonus, so no weapon ever showed an M.Atk line) and made the
        // split an invisible property of code rather than of data.
        //
        // The factors are gone (1.0 both ways). What they were really enforcing — "a caster swinging a
        // mace should not cast like a caster holding a wand" — is not a property of the WEAPON at all;
        // it is a property of the CLASS's training, so it belongs in a passive that says so out loud.
        // Entity's caster check now keys on IsMagicWeapon instead of the weapon TYPE, which is what
        // actually distinguishes a wand from a mace (both are Blunt).
        foreach (var w in weapons)
        {
            // ===== S IS AUTHORED, NOT DERIVED (him, 2026-08-11) =========================================
            // The S row used to be A × SGradeOverA (1.60), so the whole grade was one number to retune
            // ("not so much authoring"). He has now given the level-80 column by hand for every weapon,
            // and it is a CUT against the derivation on all but one cell:
            //     1H  450/211 -> 437/192     duals 434/211 -> 382/192     bow 930/211 -> 794/192
            //     wand 360/280 -> 360/256    staff 438/309 -> 426/281     2H stays at his 532/192
            // Two shapes fall out of it. Every FIGHTER weapon now shares S M.Atk 192, which CLOSES the
            // "1H M.Atk ≠ 2H M.Atk" disagreement the derived 211 created — the two lines agree again.
            // And the bow lost the most (-15%): derivation compounded the bow's already-outsized A P.Atk,
            // which is exactly the kind of artefact authoring the column exists to stop.
            //
            // Nothing derives now, so `Scale`/`SGradeOverA` are gone. A table missing its 80 row simply
            // has no S item — that is a visible hole, not a silently invented number.
            foreach (var (L, P, M, As) in w.Rows)
                yield return new ItemDef($"{w.Key}_t{L}", $"{GradeTheme(L)} {w.Noun}",
                    EquipSlot.Weapon, TierGrade(L), ItemRarity.Mythic,
                    WeaponType: w.Type,
                    AtkBonus: P,
                    MAtkBonus: M,
                    WeaponRange: w.Range,
                    ItemLevel: L, IsMagicWeapon: w.Magic, AttackSpeedBase: As);
        }
    }

    /// <summary>The level-tier ARMOR from docs/data/gear/gear_sets.csv — base bodies (Heavy/Light/Robe),
    /// shields, weightless accessories (Gloves/Boots/Helm) and jewels (Necklace/Ring/Earring). Each
    /// carries only its own base stat (P.Def / M.Def / +MP), via the existing equip path — SET BONUSES
    /// and the dmg/support VARIANTS are deferred (they need the StatMods main-stat pass + a playtest).
    /// Armors roll NO attributes for now (owner). Ids: "<key>_t<level>".</summary>
    private static IEnumerable<ItemDef> TieredArmor()
    {
        int[] lv = { FGradeLevel, 20, 40, 52, 61, 76, SGradeLevel };

        // (F, [E..A], S) → the full seven-rung column. BOTH ends are authored now: the S rung used to be
        // A × 1.60 and he re-gave the whole level-80 column by hand on 2026-08-11 (see TieredWeapons).
        // ⚠ ARMOR was cut HARDER than weapons: bodies/accessories land on ~×1.33 over A where the
        // weapons kept ~×1.55, so offence outruns defence at S by roughly 17%. That is a real TTK change
        // at endgame, not a rounding pass — measure it with tools/BalanceMatrix before tuning anything
        // else on top of it.
        static int[] Column(int fVal, int[] mid, int sVal) =>
            mid.Prepend(fVal).Append(sVal).ToArray();

        // ---- Bodies: (key, noun, weight, pDef[7], mp[7]) — robe carries inherent +MaxMP. ----
        var bodies = new (string Key, string Noun, ArmorWeight W, int[] Def, int[] Mp)[]
        {
            ("heavy", "Bulwark",       ArmorWeight.Heavy, Column(115, new[]{167,240,270,293,332}, 442), Column(0, new[]{0,0,0,0,0}, 0)),
            ("light", "Leathers",      ArmorWeight.Light, Column(86, new[]{125,179,202,220,249}, 332), Column(0, new[]{0,0,0,0,0}, 0)),
            ("robe",  "Robe",          ArmorWeight.Robe,  Column(49, new[]{84,110,135,147,166}, 221),  Column(109, new[]{274,508,613,718,866}, 1100)),
        };
        foreach (var b in bodies)
            for (int i = 0; i < lv.Length; i++)
                yield return new ItemDef($"{b.Key}_t{lv[i]}", $"{GradeTheme(lv[i])} {b.Noun}",
                    EquipSlot.Armor, TierGrade(lv[i]), ItemRarity.Mythic,
                    Weight: b.W, ArmorSlot: ArmorSlot.Body, DefBonus: b.Def[i], MpBonus: b.Mp[i],
                    ItemLevel: lv[i], NoAttributes: true,
                    SetId: $"set_{b.Key}_t{lv[i]}");

        // ---- Body VARIANTS: same base P.Def as the tier's body, alternate SET bonus (dmg/support/
        //      nuke lines from the CSV). They share the tier's accessory line. (Bonuses in ArmorSets.) ----
        var variants = new (string Key, ArmorWeight W, string Noun, int L, int Def, int Mp, string Role)[]
        {
            // Noun carries the ROLE (dmg=Warplate/Warhide, tank/def=Guardhide, etc.); "{GradeTheme} {Noun}".
            ("heavy_t52_dmg",  ArmorWeight.Heavy, "Warplate",  52, 270, 0,   "Assault"),
            ("heavy_t61_dmg",  ArmorWeight.Heavy, "Warplate",  61, 293, 0,   "Assault"),
            ("light_t40_pdef", ArmorWeight.Light, "Guardhide", 40, 179, 0,   "Bulwark"),
            ("light_t40_mdef", ArmorWeight.Light, "Wardhide",  40, 179, 0,   "Warded"),
            ("light_t40_str",  ArmorWeight.Light, "Brawlhide", 40, 179, 0,   "Brawler"),
            ("light_t52_sup",  ArmorWeight.Light, "Sagehide",  52, 202, 0,   "Sage"),
            ("light_t61_dmg",  ArmorWeight.Light, "Warhide",   61, 220, 0,   "Assault"),
            ("robe_t40_sup",   ArmorWeight.Robe,  "Raiment",   40, 110, 508, "Warden"),
            ("robe_t40_nuke",  ArmorWeight.Robe,  "Vestments", 40, 110, 508, "Destroyer"),
            // `Robe 611` (BL-27) — P.Def 147 and MaxMP 718 are the tier's own body column, straight off
            // the CSV row; only the SET bonus differs from the base 61 robe. Same "Raiment" noun as the
            // 40 Warden, so the support line reads as one line across grades.
            ("robe_t61_sup",   ArmorWeight.Robe,  "Raiment",   61, 147, 718, "Warden"),
        };
        foreach (var v in variants)
            yield return new ItemDef(v.Key, $"{GradeTheme(v.L)} {v.Noun}",
                EquipSlot.Armor, TierGrade(v.L), ItemRarity.Mythic,
                Weight: v.W, ArmorSlot: ArmorSlot.Body, DefBonus: v.Def, MpBonus: v.Mp,
                ItemLevel: v.L, NoAttributes: true, SetId: $"set_{v.Key}");

        // ---- Weightless accessories (shared across weights). ----
        var acc = new (string Key, string Noun, ArmorSlot Slot, int[] Def)[]
        {
            ("gloves", "Gauntlets", ArmorSlot.Gloves, Column(15, new[]{29,39,44,49,55}, 74)),
            ("boots",  "Greaves",   ArmorSlot.Boots,  Column(15, new[]{29,39,44,49,55}, 74)),
            ("helm",   "Helm",      ArmorSlot.Head,   Column(21, new[]{41,58,66,73,83}, 110)),
        };
        foreach (var a in acc)
            for (int i = 0; i < lv.Length; i++)
                yield return new ItemDef($"{a.Key}_t{lv[i]}", $"{GradeTheme(lv[i])} {a.Noun}",
                    EquipSlot.Armor, TierGrade(lv[i]), ItemRarity.Mythic,
                    ArmorSlot: a.Slot, DefBonus: a.Def[i], ItemLevel: lv[i], NoAttributes: true,
                    SetId: $"set_acc_t{lv[i]}");   // shared accessory line per tier (all weights)

        // ---- Shields (ShieldDefense from the CSV P.Def; block stats extrapolate Wooden→Iron, tunable). ----
        // ===== THE P.Def COLUMN WAS CUT 5x — HIS OPTION 3 (2026-08-12) ==============================
        // "I just noticed that the sheild Pdef is added to the whole Pdef (armor + helmet + gloves +
        //  boots + shield) ... thats why the tank/cleric felt immortal." He is right and it is the same
        // double-dip as the block profile below, one layer down: a 61 tank read 683 set P.Def + 358
        // shield = +50% defence on EVERY hit, before a block was ever rolled.
        //
        // He chose to keep the flat defence permanent (so an empty off-hand is a real trade) and scale
        // it to what a ~20%-block item is actually worth: "the inspiration uses .1 ... so the defence is
        // about 5 times more than if it was permenent — 256 Bloodsteel Aegis -> 51 Pdef". The tank gets
        // the difference back through his PASSIVE, not through the item (Skills.Fighter Shield Mastery
        // passive went x5, 0.40 -> 2.00), which is exactly the split he asked for: at 61 a shielded mage
        // now carries 51 (~+7% P.Def) and a mastery tank 229 (~+33%).
        // 🔑 A SHIELD HAS NO DEFENCE NUMBER ANY MORE (owner, 2026-09-09). The `shDef` ladder that
        // stood here — 18/29/41/46/51/60/83 — was added into P.Def PERMANENTLY, so it paid on every
        // hit and was then multiplied by every P.Def passive, buff and set on top: 300 points on a
        // level-76 tank off an 83-point item. His ruling: *"lets remove defence as additional armor …
        // the shield only will provide dmg reduction based on actual block"*. A block is now its
        // reduction %, and that is the shield's entire contribution.
        // ===== HE RE-GAVE THE WHOLE BLOCK PROFILE (2026-08-11) ======================================
        // "To much dmg reduction on top of the additional pdef when sucsessifull blocked. Mage should
        //  not be immortal even with a shield — it helps a bit but not 47% dmg reduction with 33%
        //  chance, that's average 15%."
        //
        // He is right about the double-dip, and it is not obvious from this table: a shield's
        // ShieldDefense is added into EffectivePhysicalDefence *permanently* (Entity, `pdef`), so it is
        // already paying out on every single hit — and then a block used to remove another 34-47% on top.
        // The new profile keeps the flat defence untouched and cuts only the block half:
        //     chance  .15 .22 .24 .26 .28 .30 .32  ->  .10 .15 .15 .20 .20 .25 .25
        //     reduce  .34 .37 .39 .41 .43 .45 .47  ->  .10 .10 .15 .15 .20 .20 .25
        // Average mitigation from blocking alone therefore goes 5.1% -> 1.0% at F and 15.0% -> 6.3% at S.
        //
        // ⚠ Two knock-ons worth knowing before this is retuned again:
        //  * Shield MASTERY is untouched (BlockChancePct up to ×1.7, and ShieldDefPct ×0.4 adds +0.08
        //    BlockReduction), so it is now a much larger share of a tank's blocking than the shield is —
        //    which reads as intended, since the complaint was specifically about the SHIELD alone
        //    carrying a mage. A mastery tank at S lands ~.425 × .33 ≈ 14% average, close to the old
        //    shield-only number.
        //  * The crit-defence column moved with it (.08-.16 -> .03-.10). Block resolution runs crit
        //    FIRST — the shield lowers crit CHANCE, and a crit that still lands ignores the block — so
        //    this is the other half of the same nerf, not a separate one.
        float[] shBlock = { 0.10f, 0.15f, 0.15f, 0.20f, 0.20f, 0.25f, 0.25f };
        float[] shReduce = { 0.10f, 0.10f, 0.15f, 0.15f, 0.20f, 0.20f, 0.25f };
        float[] shCrit = { 0.03f, 0.05f, 0.05f, 0.07f, 0.07f, 0.10f, 0.10f };
        // Evasion penalty is the one column that got HARSHER at the top: 5..9 -> 3..10, so a low-grade
        // shield costs a light-armor class less and an S shield costs it slightly more.
        int[] shEvaPen = { 3, 5, 5, 7, 7, 10, 10 };
        // The shield belongs to its tier's HEAVY set (the CSV puts shields in the same GroupId).
        // It is NOT required to complete the set — wearing it just adds the set's ShieldBonus.
        for (int i = 0; i < lv.Length; i++)
            yield return new ItemDef($"shield_t{lv[i]}", $"{GradeTheme(lv[i])} Aegis",
                EquipSlot.Shield, TierGrade(lv[i]), ItemRarity.Mythic,
                BlockChance: shBlock[i], BlockReduction: shReduce[i],
                ShieldCritDefense: shCrit[i], ShieldEvasionPenalty: shEvaPen[i],
                SetId: $"set_heavy_t{lv[i]}",
                ItemLevel: lv[i], NoAttributes: true);

        // ---- Jewels (M.Def + inherent +MP at 61/76). IG layout = 1 necklace / 2 rings / 2 earrings. ----
        var jewels = new (string Key, string Noun, JewelType T, int[] MDef, int[] Mp)[]
        {
            // BOTH ends moved on 2026-08-11: the F rung was raised (18/9/13 -> 25/12/16) and the S rung
            // authored below the old ×1.60 derivation (152/77/114 -> 138/69/104, +MP 67/34/50 -> 52/27/39).
            // Jewels are the ONLY source of M.Def, so the F raise is what stops a level-1 caster from
            // having effectively none.
            ("necklace", "Pendant",  JewelType.Necklace, Column(25, new[]{45,64,72,85,95}, 138), Column(0, new[]{0,0,0,33,42}, 52)),
            ("ring",     "Band",     JewelType.Ring,     Column(12, new[]{22,32,36,42,48}, 69), Column(0, new[]{0,0,0,17,21}, 27)),
            ("earring",  "Stud",     JewelType.Earring,  Column(16, new[]{34,45,54,63,71}, 104), Column(0, new[]{0,0,0,25,31}, 39)),
        };
        foreach (var j in jewels)
            for (int i = 0; i < lv.Length; i++)
                yield return new ItemDef($"{j.Key}_t{lv[i]}", $"{GradeTheme(lv[i])} {j.Noun}",
                    EquipSlot.Jewel, TierGrade(lv[i]), ItemRarity.Mythic,
                    MDefBonus: j.MDef[i], MpBonus: j.Mp[i], JewelType: j.T,
                    ItemLevel: lv[i], NoAttributes: true);

        // ---- Accessory BOX per tier (debug convenience): opens into the 3 accessories, so you
        //      grab a full accessory line at once instead of three items (see BoxCatalog). ----
        foreach (int L in lv)
            yield return new ItemDef($"box_acc_t{L}", $"{GradeTheme(L)} Accessory Box",
                EquipSlot.Box, TierGrade(L), ItemRarity.Rare);
    }


    /// <summary>Formula gold value by slot/grade/rarity, used when an item def does
    /// not set an explicit Value. Quest items and god-tier one-offs return 0 so they
    /// can be neither bought nor sold.</summary>
    /// <summary>Vendor price of TIERED GEAR at all seven grades, or null if this item is not tiered
    /// gear and should fall through to the generic formula.
    ///
    /// The table below is the **MYTHIC** rung, because Mythic is the 100 % base the rarity scale is a
    /// fraction of. But the F/E/D half is NOT authored here — it is written as <c>Shop(x)</c>, where
    /// x is the owner's shop price. **The shop sells RARE only, at F-D**, and those shop numbers are
    /// the fixed points of this whole table (owner, playtest-14: *"the F, E, D prices that are in the
    /// shop are for rare"*). <see cref="Shop"/> divides by the Rare multiplier to find the Mythic rung
    /// above them, so the Rare price always comes back out at exactly the authored number — the source
    /// keeps showing the shop's numbers and the round-trip is exact by construction.
    ///
    ///                    F        E         D          C           B           A            S
    ///   gloves/boots     6 000    175 000     600 000    5 400 000   12 000 000   24 000 000  120 000 000
    ///   helm/shield     10 000    250 000   1 000 000    9 000 000   20 000 000   40 000 000  200 000 000
    ///   body armor      18 000    400 000   1 800 000   16 200 000   36 000 000   72 000 000  360 000 000
    ///   1H weapon       27 000    670 000   2 700 000   24 300 000   54 000 000  108 000 000  540 000 000
    ///   2H weapon       30 000    750 000   3 000 000   27 000 000   60 000 000  120 000 000  600 000 000
    ///   ring             3 000     70 000     250 000    2 250 000    5 000 000   10 000 000   50 000 000
    ///   earring          6 000    140 000     500 000    4 500 000   10 000 000   20 000 000  100 000 000
    ///   necklace        12 000    280 000   1 500 000   13 500 000   30 000 000   60 000 000  300 000 000
    ///           (F/E/D columns are RARE prices; C/B/A/S columns are MYTHIC prices)
    ///
    /// The 2H weapon's C..S column is the owner's own: C 27kk, B 60kk ("30 was to cheap"), A 120kk,
    /// S 600kk. Every other C..S cell is DERIVED by holding that column's slot fractions, which is not
    /// a guess — they are the fractions the authored F/E/D numbers already satisfy:
    ///   * a 2H weapon = 75 % of a full 4-piece armor set (body+helm+gloves+boots);
    ///   * the set splits 45 / 25 / 15 / 15 between those pieces;
    ///   * 1H = 90 % of 2H — it hits softer AND needs a shield bought beside it, about a third of the
    ///     shield's price being the saving;
    ///   * jewels: ring 1/12, earring 1/6, necklace 1/2 of the 2H price (the D column's split).
    /// So retuning a grade is ONE number — the 2H cell — not eight.
    ///
    /// RARITY then scales it at HALF the power ratio (owner): the rarity ladder's power runs
    /// 45/55/70/70/85/100 %, so gold moves 22.5/27.5/35/35/42.5 % — rarity is worth less in gold than
    /// it is in stats, deliberately. Mythic is a 2.35x jump over Legendary, which is intended: Mythic
    /// is craft-only and meant to be traded between players for absurd sums. Epic and above are NOT
    /// vendor stock; their multipliers exist only so selling one pays sensibly.
    ///
    /// ⚠ Since `BL-272` (0.199.0) equipment is only Common (×0.05 since `BL-287`) and Mythic (×1), and
    /// since `BL-287` (0.201.0) each tier's cells are scaled by <see cref="TierPriceFactor"/>: the middle rows of
    /// <see cref="RarityPriceMul"/> no longer price any gear, and the merchants sell the MYTHIC piece at
    /// this table's full cell (F/E/D cells are lifted from the old Rare shop price by <see cref="Shop"/>).</summary>
    private static int? TieredGearPrice(ItemDef def) =>
        TieredGearBasePrice(def) is int mythic
            ? Math.Max(1, (int)Math.Round(mythic * (double)RarityPriceMul(def.Rarity)))
            : null;

    /// <summary>The MYTHIC rung of the table above — the row cell BEFORE <see cref="RarityPriceMul"/>
    /// is applied, with the tier's <see cref="TierPriceFactor"/> already in. Null for untiered/legacy gear.
    /// (It was split out for the per-rarity sell divisors, which divided THIS number; `BL-287` deleted
    /// them, and selling now reads the item's own price.)</summary>
    public static int? TieredGearBasePrice(ItemDef def)
    {
        int tier = def.ItemLevel switch
        {
            >= SGradeLevel => 6,   // S (and S*/S** when they are authored)
            >= 76 => 5,            // A
            >= 61 => 4,            // B
            >= 52 => 3,            // C
            >= 40 => 2,            // D
            >= 20 => 1,            // E
            >= 1  => 0,            // F
            _ => -1,
        };
        if (tier < 0) return null;   // untiered/legacy gear keeps the old formula

        int[]? row = def.Slot switch
        {
            EquipSlot.Shield => new[] { Shop(10_000), Shop(250_000), Shop(1_000_000), 9_000_000, 20_000_000, 40_000_000, 200_000_000 },
            EquipSlot.Armor => def.ArmorSlot switch
            {
                ArmorSlot.Body => new[] { Shop(18_000), Shop(400_000), Shop(1_800_000), 16_200_000, 36_000_000, 72_000_000, 360_000_000 },
                ArmorSlot.Head => new[] { Shop(10_000), Shop(250_000), Shop(1_000_000), 9_000_000, 20_000_000, 40_000_000, 200_000_000 },
                _              => new[] { Shop(6_000), Shop(175_000), Shop(600_000), 5_400_000, 12_000_000, 24_000_000, 120_000_000 },   // gloves / boots
            },
            EquipSlot.Weapon => def.WeaponType.IsTwoHanded()
                ? new[] { Shop(30_000), Shop(750_000), Shop(3_000_000), 27_000_000, 60_000_000, 120_000_000, 600_000_000 }
                : new[] { Shop(27_000), Shop(670_000), Shop(2_700_000), 24_300_000, 54_000_000, 108_000_000, 540_000_000 },
            EquipSlot.Jewel => def.JewelType switch
            {
                JewelType.Necklace => new[] { Shop(12_000), Shop(280_000), Shop(1_500_000), 13_500_000, 30_000_000, 60_000_000, 300_000_000 },
                JewelType.Earring  => new[] { Shop(6_000), Shop(140_000), Shop(500_000), 4_500_000, 10_000_000, 20_000_000, 100_000_000 },
                _                  => new[] { Shop(3_000), Shop(70_000), Shop(250_000), 2_250_000, 5_000_000, 10_000_000, 50_000_000 },   // ring
            },
            _ => null,
        };
        if (row is null) return null;

        return (int)Math.Round(row[tier] * TierPriceFactor(tier));
    }

    /// <summary>`BL-287` (owner, 2026-09-24, design doc §2.5): the IG-shaped move of the MYTHIC prices,
    /// one factor per tier and the same for every slot of it. T1 ×2.2 (2H ~190k), T20 ×0.65 (2H 1.4M),
    /// T40 and T52 ×0.5 (2H 4.29M / 13.5M), T61 / T76 / T80 unchanged (60M / 120M / 600M). The rows above
    /// keep their authored cells, so the slot fractions they encode stay intact and this is the only
    /// place a tier's level moves. Index = the tier switch in <see cref="TieredGearBasePrice"/>.
    /// ⚠ A switch, not a static array: the catalog is built by a static initializer declared ABOVE any
    /// field here, so an array would still be null when the first price is computed.</summary>
    private static double TierPriceFactor(int tier) => tier switch
    {
        0 => 2.2,    // F  (T1)
        1 => 0.65,   // E  (T20)
        2 => 0.5,    // D  (T40)
        3 => 0.5,    // C  (T52)
        _ => 1.0,    // B / A / S (T61 / T76 / T80)
    };

    /// <summary>The owner's F/E/D shop price is the price of a **RARE** item. This lifts it to the
    /// MYTHIC rung the price table is expressed in, so that multiplying back down by the Rare
    /// multiplier returns the shop's number exactly.</summary>
    private static int Shop(int rareShopPrice) =>
        (int)Math.Round(rareShopPrice / (double)RarityPriceMul(ItemRarity.Rare));

    /// <summary>Rarity's effect on GOLD. Since `BL-287` (0.201.0) only two rungs price real gear: Mythic 1.0
    /// and **Common 0.05** (owner, 2026-09-24: *"a common why its is at 1/4 of the normal?"*; it was 0.225,
    /// half its old 45% power). The middle rungs price nothing any more; Rare stays 0.35 only because
    /// <see cref="Shop"/> lifts the F/E/D shop cells through it, and moving it would move those cells.</summary>
    public static float RarityPriceMul(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common    => 0.05f,
        ItemRarity.Uncommon  => 0.275f,
        ItemRarity.Rare      => 0.350f,
        ItemRarity.Epic      => 0.350f,
        ItemRarity.Legendary => 0.425f,
        ItemRarity.Mythic    => 1.000f,
        _ => 0.350f,
    };

    public static int DefaultValue(ItemDef def)
    {
        if (def.Slot == EquipSlot.QuestItem)
            return 0;

        if (TieredGearPrice(def) is int tiered) return tiered;

        int gradeBase = def.Grade switch
        {
            ItemGrade.F => 10,
            ItemGrade.E => 35,
            ItemGrade.B => 120,
            ItemGrade.A => 400,
            ItemGrade.S => 1200,
            _ => 10,
        };
        float rarityMul = def.Rarity switch
        {
            ItemRarity.Common => 1f,
            ItemRarity.Uncommon => 2f,
            ItemRarity.Rare => 4f,
            ItemRarity.Epic => 8f,
            ItemRarity.Legendary => 16f,
            _ => 1f,
        };
        float slotMul = def.Slot switch
        {
            EquipSlot.Weapon => 2.0f,
            EquipSlot.Armor => def.ArmorSlot == ArmorSlot.Body ? 1.6f : 0.8f,
            EquipSlot.Shield => 1.2f,
            EquipSlot.Jewel => 1.4f,
            EquipSlot.Consumable => 0.8f,
            EquipSlot.Scroll => 3.0f,
            _ => 1f,
        };
        return Math.Max(1, (int)(gradeBase * rarityMul * slotMul));
    }

    /// <summary>Gold paid to a player who SELLS this item: **half its buy price, for everything**
    /// (`BL-287`, owner, 2026-09-24, IG-shaped). <c>SellPriceOverride</c> still wins, and 0 there means
    /// "sells for nothing" (premium items, runes, buff potions), which the ruling keeps at 0.
    ///
    /// The buy price is the item's own: <c>BuyPriceOverride</c> when it names a positive price (a vendor
    /// stone is 20,000 against a Value of 100), else its <c>Value</c>, which for tiered gear is the rarity-
    /// scaled cell (<see cref="TieredGearPrice"/>). So a Common sells for 2.5% of its Mythic, not a
    /// fraction of the Mythic rung.
    ///
    /// ⚠ This replaced FOUR rules at once, all deleted: gear Mythic ÷10 and a per-rarity ladder down to
    /// Common ÷200 of the Mythic rung (`BL-114`), buff potions and cast-on-use scrolls ÷10 of their own
    /// value, and 30% for everything else. The flood those divisors fought is now held by the DROP side
    /// (per-slot Common chances, no healing potions above 40) and by the Common price itself.</summary>
    public static int SellPrice(ItemDef def)
    {
        if (def.SellPriceOverride is int s) return Math.Max(0, s);
        int basis = def.BuyPriceOverride is int b && b > 0 ? b : def.Value;
        return basis <= 0 ? 0 : Math.Max(1, (int)(basis * (double)GameConstants.VendorSellFraction));
    }

    /// <summary>Gold charged when BUYING this item from a vendor (incl. the future
    /// castle surcharge). BuyPriceOverride wins (-1 = unbuyable, 0 = free); otherwise
    /// the Value formula (-1 = not buyable).</summary>
    /// <summary>Vendor equipment can't cost less than this (owner: "equipments must start at 200g at
    /// least" — they were "very very cheap"). Applies to weapons/armor/shields only; JEWELS are exempt
    /// (the broken-jewel line is deliberately 40-60g) and so are consumables/boxes.</summary>
    public const int EquipmentMinBuyPrice = 200;

    public static int BuyPrice(ItemDef def)
    {
        int price = def.BuyPriceOverride is int b ? b
                  : def.Value <= 0 ? -1 : Math.Max(1, (int)(def.Value * (1f + GameConstants.VendorBuyTaxRate)));
        if (price > 0 && def.Slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield)
            price = Math.Max(EquipmentMinBuyPrice, price);
        return price;
    }

    /// <summary>The PLATINUM half of an item's shelf price (`BL-257`). 0 for everything that is not
    /// premium. Authored verbatim — no rarity multiplier, no vendor tax, no equipment floor; see the
    /// field's note in the record for why a derived platinum price would be a mistake.</summary>
    public static int PlatinumPrice(ItemDef def) => Math.Max(0, def.PlatinumPrice);

    /// <summary>Is this item on sale at all? TRUE when it carries a gold price, a platinum price, or
    /// both — his *"any item that have a platinum or/and gold must be bought with the value"*. The
    /// single place that question is asked, so the shelf, the client and <c>HandleBuy</c> cannot
    /// disagree about whether a platinum-only item exists.</summary>
    public static bool IsPurchasable(ItemDef def) => BuyPrice(def) > 0 || PlatinumPrice(def) > 0;

    /// <summary>An item the player can sell to a vendor: TRADABLE, not a quest item,
    /// and worth something. Untradeable items can only be deleted.</summary>
    public static bool IsSellable(ItemDef def) =>
        def.Tradable && def.Slot != EquipSlot.QuestItem && SellPrice(def) > 0;

    /// <summary>An openable box/chest (rolls its BoxCatalog loot table).</summary>
    public static bool IsBox(ItemDef def) => def.Slot == EquipSlot.Box;

    /// <summary>A recipe BOOK — opening it teaches its recipe (see TeachesRecipeId).</summary>
    public static bool IsRecipeBook(ItemDef def) => def.TeachesRecipeId.Length > 0;

    /// <summary>The item id of the recipe item that teaches a recipe at a given % (`BL-273` part 2).</summary>
    public static string RecipeBookId(string recipeId, int percent) => $"recipe_{recipeId}_{percent}";

    /// <summary>How many jewels of a given sub-type can be worn at once.</summary>
    public static int MaxOfJewelType(JewelType t) => t switch
    {
        JewelType.Ring => 2,
        JewelType.Earring => 2,
        JewelType.Necklace => 1,
        _ => 1   // untyped jewel: single
    };

    /// <summary>How strong a worn jewel is, for deciding which one a new one displaces and which
    /// end of a PAIR it sits in.
    ///
    /// It is the MAGIC DEFENCE THE JEWEL ACTUALLY DELIVERS — enchant included, exactly as
    /// <c>RecomputeDerived</c> adds it — with MP and HP breaking a tie. This replaces ranking by
    /// RARITY alone (playtest-17 C10), which ignored grade and so answered the wrong question: a
    /// **Mythic F** band gives 9 M.Def, an **Uncommon E** band gives 12, yet the old key called the
    /// Mythic F stronger purely because Mythic &gt; Uncommon, and equipping a third ring threw away
    /// the better piece. Rarity is only ever a fraction of a GRADE's ceiling, so it cannot order two
    /// grades against each other; the delivered number can, and it is the owner's own fallback
    /// suggestion ("or simply the defence value").
    ///
    /// Enchant is inside the number rather than a tie-break for the same reason — a +6 band really
    /// is worth more M.Def than a +0 one of the same piece.
    ///
    /// Deliberately NOT persisted: which physical slot a jewel occupies is a pure function of what
    /// you are wearing (strongest first — see JewelSlotOrder), so it survives a relog with no extra
    /// column and can never drift out of sync with the items themselves.</summary>
    public static long JewelStrength(ItemDef def, int enchant)
        => (long)(def.MDefBonus + EnchantRules.MDefDelta(def, enchant)) * 1_000_000L
         + (long)(def.MpBonus + EnchantRules.MpDelta(def, enchant)) * 1_000L
         + def.HpBonus;   // jewels carry no enchant-scaled HP (his table: M.Def + MP only)

    /// <summary>Sort key placing worn jewels of one sub-type into their designated slots: the
    /// STRONGER of a pair takes slot 1. DefId breaks a full tie so the order is stable across
    /// relogs (a live InstanceId is regenerated on load and would not be).</summary>
    public static (long NegStrength, string DefId) JewelSlotOrder(ItemDef def, int enchant)
        => (-JewelStrength(def, enchant), def.Id);

    public static ItemDef? Get(string id) => id is null ? null : All.GetValueOrDefault(id);

    /// <summary>Fail STARTUP if any rune names a buff that does not exist, or a LEVEL its buff does not
    /// have. Both mistakes produce a rune that looks perfect in the bag and does nothing at all — the
    /// reconciliation loop simply skips a skill it cannot resolve, silently — and a reward rune ladder is
    /// 55 items generated from a table, which is exactly the shape that goes wrong once and everywhere.
    ///
    /// <para>Also checks the two catalogs agree on the SPAN of a ladder: a rung added to the items but
    /// not to the skill is the same invisible dud.</para></summary>
    public static void ValidateRunes()
    {
        var bad = new List<string>();
        foreach (var def in All.Values.Where(d => d.IsRune))
        {
            if (string.IsNullOrEmpty(def.RuneBuffSkillId))
            {
                bad.Add($"{def.Id}: a rune with no RuneBuffSkillId grants nothing.");
                continue;
            }
            if (SkillCatalog.Get(def.RuneBuffSkillId) is not SkillDef skill)
            {
                bad.Add($"{def.Id}: names unknown buff skill '{def.RuneBuffSkillId}'.");
                continue;
            }
            if (def.RuneBuffLevel < 1 || def.RuneBuffLevel > skill.MaxLevel)
                bad.Add($"{def.Id}: RuneBuffLevel {def.RuneBuffLevel} is outside "
                      + $"'{skill.Id}' (levels 1..{skill.MaxLevel}).");
            // `BL-153` as widened: EVERY rune is Mythic, laddered or not. Guarded here because the
            // rule is one word and the next rune added will be authored by copying a neighbour — this
            // is what makes "all runes" survive that copy.
            if (def.Rarity != ItemRarity.Mythic)
                bad.Add($"{def.Id}: rarity {def.Rarity} — every rune is Mythic (`BL-153`).");
        }

        // Every rung of every reward channel must exist as an item AND as a level of its skill.
        foreach (var ch in RewardRunes.All)
        {
            if (SkillCatalog.Get(ch.SkillId) is not SkillDef skill)
            {
                bad.Add($"reward channel '{ch.Key}': no skill '{ch.SkillId}'.");
                continue;
            }
            if (skill.MaxLevel != RewardRunes.Ladder.Length)
                bad.Add($"'{ch.SkillId}' has {skill.MaxLevel} rungs, the ladder has "
                      + $"{RewardRunes.Ladder.Length} — the two must match.");
            for (int rung = 0; rung < RewardRunes.Ladder.Length; rung++)
            {
                string itemId = ch.ItemId(RewardRunes.Percent(rung));
                if (!All.ContainsKey(itemId)) bad.Add($"reward rune item '{itemId}' is missing.");
            }
        }

        if (bad.Count > 0)
            throw new InvalidOperationException("Rune catalog is broken:\n  " + string.Join("\n  ", bad));
    }

    /// <summary>All catalog items of a top-level category (e.g. every OffHand).</summary>
    public static IEnumerable<ItemDef> OfType(ItemType type) => All.Values.Where(d => d.Type == type);

    /// <summary>All catalog items of a sub-type (e.g. every Boots, or every Sword).</summary>
    public static IEnumerable<ItemDef> OfSubtype(ItemSubtype subtype) => All.Values.Where(d => d.Subtype == subtype);

    // Per-class quest-item ids (the two proofs a class-change chain awards). Generated
    // in BuildCatalog from ClassCatalog; the quest chains reference them by these ids.
    public static string ClassTokenId(int classId) => $"qi_{classId}_token";
    public static string ClassProofId(int classId) => $"qi_{classId}_proof";

    /// <summary>The 4th-class key — ONE item for all 36 fourth classes, bought at any Apothecary and
    /// consumed by the 4th-class master (owner, 2026-08-17: *"now can be without quest but go in the
    /// apothecary and buy a 100kk 4th_class_item and go to class master with it … then we add
    /// additional long quest"*). The long chain replaces the PURCHASE, not the item — when it lands,
    /// this comes off the Apothecary shelf and becomes the chain's reward, and nothing else in the
    /// class-change path has to change.</summary>
    public const string FourthClassKey = "qi_ascension_rite";

    public static IEnumerable<ItemDef> AllItems => All.Values;

    /// <summary>A BUFF scroll — the hour-long blessing you read.
    ///
    /// <para>⚠ IT IS NOT <see cref="EquipSlot.Scroll"/>. That slot is the ENCHANT/ATTRIBUTE bench; a
    /// blessing scroll is authored as a <see cref="EquipSlot.Consumable"/> exactly like a buff potion,
    /// and the only thing separating the two is the DURATION its wrapper skill carries — one hour for a
    /// scroll, twenty minutes for a potion. <see cref="SkillCatalog.ConsumableBuffForm"/> already owns
    /// that distinction and drives the auto-buff tab with it, so this asks that rather than inventing a
    /// second test which could disagree with the first.</para></summary>
    public static bool IsBuffScroll(ItemDef def) =>
        def.Slot == EquipSlot.Consumable && def.PotionCooldownTicks == 0
        && SkillCatalog.Get(def.UseSkillId) is { } s
        && SkillCatalog.ConsumableBuffForm(s) == BuffForm.Scroll;

    public static bool IsPotion(ItemDef def) => def.Slot == EquipSlot.Consumable;
    /// <summary>A HEALING potion — the one bound by the shared drink cooldown.</summary>
    public static bool IsHealPotion(ItemDef def) =>
        def.Slot == EquipSlot.Consumable && def.PotionCooldownTicks > 0 && !string.IsNullOrEmpty(def.UseSkillId);

    /// <summary>A MANA potion: a drink-cooldown consumable whose skill restores MP over time. It is
    /// <see cref="IsHealPotion"/>'s twin and its EXCLUSION — both are "a consumable with its own drink
    /// timer", so without this the auto-hunt's HP line would happily drink your mana potions to top up
    /// a health bar they cannot touch.</summary>
    public static bool IsManaPotion(ItemDef def) =>
        IsHealPotion(def) && SkillCatalog.Get(def.UseSkillId) is { } s
        && (s.Effect & SkillEffect.RestoreMp) != 0;

    /// <summary>A BUFF potion: grants a lasting effect instantly, free of the heal cooldown.
    /// Excludes inert reagents and the cast-on-use scrolls.
    ///
    /// It must actually GRANT A BUFF — "instant + no heal cooldown" alone is not enough. The Ultimate
    /// Scroll of Return is a 0-cast Consumable too, so the moment it became truly instant it started
    /// matching this test, and auto-hunt's keep-your-buff-potions-up loop would have happily drunk it and
    /// teleported the farmer to town on repeat.</summary>
    public static bool IsBuffPotion(ItemDef def) =>
        def.Slot == EquipSlot.Consumable && def.PotionCooldownTicks == 0
        && !string.IsNullOrEmpty(def.UseSkillId)
        && SkillCatalog.Get(def.UseSkillId) is { CastTicks: 0 } s
        && (s.Effect & SkillEffect.AnyBuff) != 0;
    public static bool IsScroll(ItemDef def) => def.Slot == EquipSlot.Scroll;
    public static bool IsEnchantScroll(ItemDef def) => def.Slot == EquipSlot.Scroll && def.ScrollKind != ScrollKind.None;
    public static bool IsAttributeScroll(ItemDef def) => def.AttrScroll != AttrScrollKind.None;
    public static bool IsQuestItem(ItemDef def) => def.Slot == EquipSlot.QuestItem;
    public static bool IsEquippable(ItemDef def) => def.Slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Jewel;

    // ===== THE BAG TABS ===========================================================================
    //
    // 🔑 THESE LIVE HERE, IN SHARED, SINCE `BL-240`. They were private to the Unity `GameUi` until the
    // instant-sell button gave the SERVER a reason to ask "is this item on the Gear tab?" — and a
    // second copy of the rule on the server is exactly the drift `ItemTag` exists to prevent: the
    // player would pick a tab, and the sweep would sell by a slightly different definition of it.
    //
    // Gear = anything you can WEAR (runes included: you hold them). Use = anything you consume,
    // scrolls and boxes with it — a box is a tap-to-spend, not a material. Mats = the rest, which is
    // what "everything else" honestly is. Quest is a category so the bag can keep its own tab for it;
    // the vendor and the keeper never show it at all (B4).

    public static ItemCategory CategoryOf(ItemDef def) => def == null ? ItemCategory.Mats : def.Slot switch
    {
        EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield
            or EquipSlot.Jewel or EquipSlot.Rune          => ItemCategory.Gear,
        EquipSlot.Consumable or EquipSlot.Scroll
            or EquipSlot.Box                              => ItemCategory.Use,
        EquipSlot.QuestItem                               => ItemCategory.Quest,
        _                                                 => ItemCategory.Mats,
    };

    /// <summary>Does this item belong under the given tab? <see cref="ItemCategory.All"/> means
    /// everything EXCEPT quest tokens, which are only ever reachable through their own tab.</summary>
    public static bool InCategory(ItemCategory tab, ItemDef def)
    {
        var c = CategoryOf(def);
        return tab == ItemCategory.All ? c != ItemCategory.Quest : c == tab;
    }

    // ===== `BL-241`: THE PICKUP FILTER'S THREE CATEGORIES ==========================================
    //
    // Owner, 2026-09-16: *"we need in bag rarity filter for any type gear/mats/use to be able to select
    // min rarity for pickup"*. His three words are exactly three of the tabs the bag already has, which
    // is why the filter is keyed on `ItemCategory` rather than on a parallel enum of its own — you set
    // it on the tab you are looking at, and it means that tab.
    //
    // 🔑 `All` and `Quest` are deliberately NOT filterable. `All` is not a kind of item, and a quest
    // token you refused would be a quest you cannot finish — the one drop that must always land.

    /// <summary>The three categories a pickup filter can be set on, in the order they are sent on the
    /// wire (<c>InventoryUpdate.PickupMinRarity</c>) and drawn in the bag. Never reorder this: the
    /// array is positional on both sides and persisted by INDEX nowhere — but the wire is by index.</summary>
    public static readonly ItemCategory[] PickupCategories =
        { ItemCategory.Gear, ItemCategory.Use, ItemCategory.Mats };

    /// <summary>Index of <paramref name="c"/> in <see cref="PickupCategories"/>, or -1 for a category
    /// that cannot be filtered (All, Quest).</summary>
    public static int PickupCategoryIndex(ItemCategory c) => Array.IndexOf(PickupCategories, c);

    /// <summary>The level at which an ITEM reaches FULL power (below it you may still equip it, but the
    /// GRADE PENALTY scales your stats down by the grade GAP). Not a hard equip gate. Takes the DEF, not
    /// the grade: the real tier is <see cref="ItemDef.ItemLevel"/> — the ItemGrade enum has no C/D and is
    /// for pricing only. F-level returns 0 so the UI shows no note for starter gear.</summary>
    public static int RequiredLevel(ItemDef def)
    {
        int lvl = GradePenalty.ItemGradeLevel(def);
        return lvl <= 1 ? 0 : lvl;
    }
}


// 🔴 LootEntry / LootTables DELETED (playtest-22). A per-mob-NAME gear table addressing the legacy
// grid above — dead since drops moved to MobCatalog.GearDrops + DropEntry: `LootTables.Roll` had no
// caller anywhere in the solution. It was the last thing keeping the deleted ids compiling, and a
// dead table that still looks authoritative is how a wrong number gets quoted years later.
