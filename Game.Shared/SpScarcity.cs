namespace Game.Shared;

/// <summary>
/// `BL-314` — THE PASSIVE SCARCITY MULTIPLIER, one number per archetype. His why (2026-09-28): *"after lvl 20 or so u
/// have SP to spare ... the sum of all the passives is 3-4 times more that the current single one - until 75 .. after
/// the sum should be x1"*. The passives were split into single-stat pieces (`docs/design/PassiveSplit.md`) so a player
/// has to CHOOSE among them, and this is what makes the choice bite: every PASSIVE rung a class learns at level
/// 20-75 costs its authored price ×k. Actives stay ×1, and so does everything from 76 on.
///
/// <para>🔑 <b>k IS SOLVED, NOT PICKED.</b> Each number lands its archetype's paths on his affordability target
/// x = SP earned in 20-75 / the kit's 20-75 cost (2026-09-29: daggers .65, bows .55, warriors .65, tanks .70,
/// Magus .60, Lightbringer .55, Warchanter .50). `BalanceMatrix --sp-budget` prints the x each path lands on;
/// re-solve there after any kit change rather than nudging a number by feel.</para>
///
/// <para>🔑 <b>IT IS APPLIED ONCE, WHEN THE CLASS TABLES LOAD</b> (<see cref="ClassSkills"/>), onto the per-class
/// <see cref="ClassSkill.SpCost"/> of every passive row in the band. So every reader — the learn handler, the
/// client's Learn tab, `--check`, the SP budget — sees the same price, and retuning a class's scarcity is one
/// number here. The class CSVs show the RESULT (his rule: the CSVs represent what is in the game), and
/// `SkillCsvSeed --write-sp` rewrites those cells from this; never edit both by hand.</para>
///
/// <para>⚠ A 2nd-tier character has not chosen a discipline yet, so the two 2nd classes that feed two different
/// groups pay the MEAN of their children: the rogue (daggers 7.15, bows 9.09) and the cleric (Lightbringer 3.52,
/// Warchanter 3.73). Their 2nd-tier passives are a few percent of the 20-75 bill, so the blend moves x by
/// hundredths.</para>
/// </summary>
public static class SpScarcity
{
    /// <summary>The learn levels the multiplier covers, inclusive. Below 20 there is SP to spare; from 76 on the
    /// 4th tier is priced ×1 by his ruling.</summary>
    public const int FromLevel = 20, ToLevel = 75;

    public const double Daggers = 7.15, Bows = 9.09, Warriors = 4.63, Tanks = 3.58,
                        Magus = 4.60, Lightbringer = 3.52, Warchanter = 3.73;

    /// <summary>k for one class table. Base classes (no archetype) are ×1: they end at 19.</summary>
    public static double For(Archetype? archetype, Discipline? discipline) => discipline switch
    {
        Discipline.Nullblade or Discipline.Phantom or Discipline.Venomweaver => Daggers,
        Discipline.Sharpshooter or Discipline.Trapper or Discipline.Hunter => Bows,
        Discipline.Ravager or Discipline.Warlord => Warriors,
        Discipline.Bulwark => Tanks,
        Discipline.Magus => Magus,
        Discipline.Lightbringer => Lightbringer,
        Discipline.Warchanter => Warchanter,
        null => archetype switch
        {
            Archetype.Rogue => (Daggers + Bows) / 2,
            Archetype.Archer => Bows,
            Archetype.Warrior => Warriors,
            Archetype.Tank => Tanks,
            Archetype.Nuker => Magus,
            Archetype.Healer => (Lightbringer + Warchanter) / 2,
            _ => 1.0,
        },
        _ => 1.0,
    };

    /// <summary>Does the multiplier apply to a rung learned at <paramref name="learnLevel"/>?</summary>
    public static bool Covers(SkillDef def, int learnLevel) =>
        def.Category == SkillCategory.Passive && learnLevel >= FromLevel && learnLevel <= ToLevel;

    /// <summary>The price a player pays: ×k, rounded to three significant figures so the Learn tab and his CSV
    /// read 66,500 rather than 66,495.</summary>
    public static int Scale(int basePrice, double k) => basePrice <= 0 || k == 1.0 ? basePrice : Nice(basePrice * k);

    /// <summary>Round to three significant figures (below 1,000: to the unit).</summary>
    public static int Nice(double v)
    {
        if (v < 1000) return (int)Math.Round(v);
        double step = Math.Pow(10, Math.Floor(Math.Log10(v)) - 2);
        return (int)(Math.Round(v / step) * step);
    }

    /// <summary>The inverse, for the generator reading a CSV that already shows scaled prices: the base price
    /// whose <see cref="Scale"/> is exactly <paramref name="scaled"/>, or null when no integer base lands on it
    /// (a hand-typed price that is not three significant figures).</summary>
    public static int? Unscale(int scaled, double k)
    {
        if (scaled <= 0 || k == 1.0) return scaled;
        // Several bases land on one rounded price; take the ROUNDEST (his prices are 1700, not 1701), then the
        // nearest — so a base read back from the CSV is the one that was scaled into it.
        double exact = scaled / k;
        int b = (int)Math.Round(exact);
        int? best = null;
        for (int c = Math.Max(1, b - 60); c <= b + 60; c++)
            if (Scale(c, k) == scaled
                && (best is not int bb || Zeros(c) > Zeros(bb)
                    || (Zeros(c) == Zeros(bb) && Math.Abs(c - exact) < Math.Abs(bb - exact))))
                best = c;
        return best;

        static int Zeros(int v) { int z = 0; while (v > 0 && v % 10 == 0) { v /= 10; z++; } return z; }
    }
}
