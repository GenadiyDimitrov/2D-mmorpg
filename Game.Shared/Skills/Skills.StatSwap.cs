namespace Game.Shared;

/// <summary>
/// The level-40 STAT-SWAP passives — the only thing in the game that moves your main stats.
///
/// You are born with your CON/ATK/WIT/DEX and nothing raises them for free any more (the old
/// LevelStatBonus and the class-change stat grants are both gone). At 40 you may buy ONE trade-off
/// per group: each level gives <b>+1 to one stat and −1 to another</b>, up to +5/−5 at level 5.
/// They cost GOLD, not SP — 1kk / 2kk / 3kk / 4kk / 5kk (15kk to max a single skill).
///
/// <b>THE DIRECTION RULE.</b> Every stat you touch commits to ONE DIRECTION, for good. Taking
/// <c>+X −Y</c> means X is now an "up" stat and Y is now a "down" stat, so from then on:
/// <list type="bullet">
///   <item>nothing else may RAISE X (that's the old exclusive-group rule — one +X skill only),</item>
///   <item>nothing may LOWER X (you cannot give back what you bought), and</item>
///   <item>nothing may RAISE Y (you cannot buy back what you sold).</item>
/// </list>
/// A second skill that ALSO lowers Y is still allowed — those stack. Without this rule you could
/// buy a circular loop (+A−B, +B−C, +C−A) that nets to +0 for 45kk, which is a pure gold sink with
/// no decision in it. With it, <c>StatSwapConflict</c> makes such a loop unreachable: the second
/// skill in the ring always tries to raise a stat the first one already sold.
///
/// Worked example (fighter): take <c>+ATK −MEN</c>, then <c>+WIT −MEN</c> (MEN stacks to −10). The
/// only pair still open is <c>+CON −DEX</c> / <c>+DEX −CON</c> — pick one and you land on
/// +5 ATK, +5 WIT, +5 CON, −5 DEX, −10 MEN. Every other swap is banned by one of the three clauses.
///
/// The ATK group is gated by class, because ATK is our single power stat (it feeds P.Atk for a
/// fighter and M.Atk for a caster — the WEAPON decides which):
///   • fighters pay in CON or DEX     • mages pay in WIT or MEN
///   • BUFFERS may take all four — deliberately strong, since they can pay in a stat they don't
///     use. If that proves too good, switch them to a dual-cost form (+ATK −a −b).
///
/// MEN is NOT a stat any more. A "±MEN" swap IS its modifiers: ±% Max MP, ±% M.Def, ±% MP regen.
///
/// LATER: a "reset skills" NPC that un-learns these so a bad pick can be re-chosen. Removing is
/// free but does NOT refund the gold.
/// </summary>
public static partial class SkillCatalog
{
    // ---- Group ids (mutually exclusive within a group) ----
    public const string GroupSwapCon = "swap_con";
    public const string GroupSwapDex = "swap_dex";
    public const string GroupSwapAtk = "swap_atk";
    public const string GroupSwapWit = "swap_wit";
    public const string GroupSwapMen = "swap_men";

    // ---- Skill ids: swap_<raised>_<sacrificed> ----
    public const string SwapConAtk = "swap_con_atk";   // +CON −ATK
    public const string SwapConDex = "swap_con_dex";   // +CON −DEX
    public const string SwapDexAtk = "swap_dex_atk";   // +DEX −ATK
    public const string SwapDexCon = "swap_dex_con";   // +DEX −CON
    public const string SwapAtkDex = "swap_atk_dex";   // +ATK −DEX   (fighter)
    public const string SwapAtkCon = "swap_atk_con";   // +ATK −CON   (fighter)
    public const string SwapAtkWit = "swap_atk_wit";   // +ATK −WIT   (mage)
    public const string SwapAtkMen = "swap_atk_men";   // +ATK −MEN   (mage)
    public const string SwapWitAtk = "swap_wit_atk";   // +WIT −ATK
    public const string SwapWitMen = "swap_wit_men";   // +WIT −MEN
    public const string SwapMenAtk = "swap_men_atk";   // +MEN −ATK
    public const string SwapMenWit = "swap_men_wit";   // +MEN −WIT

    /// <summary>All 5 levels unlock at 40 — the gate is GOLD, not level.</summary>
    public const int StatSwapLearnLevel = 40;

    /// <summary>Gold per level: 1kk, 2kk, 3kk, 4kk, 5kk (15kk to max one skill).</summary>
    private static readonly int[] StatSwapGold =
        { 1_000_000, 2_000_000, 3_000_000, 4_000_000, 5_000_000 };

    // SPT is a REAL STAT now (owner, 2026-07-20), so a Spirit swap is just ±1 SPT per level like
    // every other swap — no more bundling percentages. The percentages this used to emit are gone;
    // the stat itself carries Max MP, M.Def and MP regen through StatCalculator.SptModifier.

    /// <summary>Build a stat-swap passive: 5 levels, each +1/−1 further, each priced in gold.
    /// <paramref name="at"/> maps a level (1-5) to that level's cumulative effect.</summary>
    private static SkillDef StatSwap(string id, string name, string group,
        System.Func<int, PassiveEffect> at, System.Func<int, string> describe) => new(
        id, name, BaseClass.Fighter, SkillEffect.None,
        MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
        Category: SkillCategory.Passive, SpCost: 0, ExclusiveGroup: group,
        Description: describe(5),
        Levels: System.Linq.Enumerable.Range(1, 5)
            .Select(l => new SkillLevel(SpCost: 0, GoldCost: StatSwapGold[l - 1],
                Passive: at(l), Description: describe(l)))
            .ToArray());

    /// <summary>±Spirit — now simply ±SPT, the stat.</summary>
    private static PassiveEffect MenSwap(int sptDelta, int con = 0, int dex = 0, int atk = 0, int wit = 0) =>
        new(Con: con, Dex: dex, Atk: atk, Wit: wit, Spt: sptDelta);

    private static string Swap2(int n, string up, string down) =>
        $"Passive. +{n} {up}, −{n} {down}. (Level {n} of 5.)";

    private static string SwapMenUp(int n, string down) =>
        $"Passive. +{n} SPT (Max MP, M.Def, MP regen); −{n} {down}. (Level {n} of 5.)";

    private static string SwapMenDown(int n, string up) =>
        $"Passive. +{n} {up}; −{n} SPT (Max MP, M.Def, MP regen). (Level {n} of 5.)";

    /// <summary>A stat a swap can move. MEN is not a real stat any more (it IS its modifiers — see
    /// <see cref="MenSwap"/>), but for the DIRECTION rule it commits exactly like the others.</summary>
    public enum SwapStat { Con, Dex, Atk, Wit, Men }

    /// <summary>THE source of truth for the swaps: id, display name, the stat it RAISES and the stat
    /// it LOWERS. Everything else — the exclusive group, the PassiveEffect, the description and the
    /// direction rule — is derived from this, so a new swap cannot fall out of sync with the rule
    /// that polices it. Skill ids follow <c>swap_&lt;raised&gt;_&lt;sacrificed&gt;</c>.</summary>
    private static readonly (string Id, string Name, SwapStat Up, SwapStat Down)[] SwapTable =
    {
        (SwapConAtk, "Fortitude (Power)",   SwapStat.Con, SwapStat.Atk),
        (SwapConDex, "Fortitude (Agility)", SwapStat.Con, SwapStat.Dex),

        (SwapDexAtk, "Agility (Power)",     SwapStat.Dex, SwapStat.Atk),
        (SwapDexCon, "Agility (Vigour)",    SwapStat.Dex, SwapStat.Con),

        // ATK group is class-gated (buffers may take all four) — see StatSwapsFor.
        (SwapAtkDex, "Power (Agility)",     SwapStat.Atk, SwapStat.Dex),
        (SwapAtkCon, "Power (Vigour)",      SwapStat.Atk, SwapStat.Con),
        (SwapAtkWit, "Power (Insight)",     SwapStat.Atk, SwapStat.Wit),
        (SwapAtkMen, "Power (Spirit)",      SwapStat.Atk, SwapStat.Men),

        (SwapWitAtk, "Insight (Power)",     SwapStat.Wit, SwapStat.Atk),
        (SwapWitMen, "Insight (Spirit)",    SwapStat.Wit, SwapStat.Men),

        (SwapMenAtk, "Spirit (Power)",      SwapStat.Men, SwapStat.Atk),
        (SwapMenWit, "Spirit (Insight)",    SwapStat.Men, SwapStat.Wit),
    };

    /// <summary>The exclusive group of a swap = the stat it RAISES ("swap_con", …). Kept because the
    /// skill-reset NPC un-learns anything carrying an ExclusiveGroup, and because it is still the
    /// clause that stops you holding two different +X skills.</summary>
    private static string GroupOf(SwapStat up) => "swap_" + up.ToString().ToLowerInvariant();

    /// <summary>The cumulative effect of a swap at level <paramref name="l"/>: +l to the raised stat,
    /// −l to the sacrificed one. MEN is expressed as its modifiers rather than as a stat.</summary>
    private static PassiveEffect SwapEffect(SwapStat up, SwapStat down, int l)
    {
        int con = 0, dex = 0, atk = 0, wit = 0, men = 0;
        void Move(SwapStat s, int d)
        {
            switch (s)
            {
                case SwapStat.Con: con += d; break;
                case SwapStat.Dex: dex += d; break;
                case SwapStat.Atk: atk += d; break;
                case SwapStat.Wit: wit += d; break;
                case SwapStat.Men: men += d; break;
            }
        }
        Move(up, l);
        Move(down, -l);
        return men == 0
            ? new PassiveEffect(Con: con, Dex: dex, Atk: atk, Wit: wit)
            : MenSwap(men, con: con, dex: dex, atk: atk, wit: wit);
    }

    private static string SwapDescription(SwapStat up, SwapStat down, int l) =>
        down == SwapStat.Men ? SwapMenDown(l, Label(up))
        : up == SwapStat.Men ? SwapMenUp(l, Label(down))
        : Swap2(l, Label(up), Label(down));

    private static string Label(SwapStat s) => s.ToString().ToUpperInvariant();

    private static SkillDef[] StatSwapSkillDefs() => SwapTable
        .Select(r => StatSwap(r.Id, r.Name, GroupOf(r.Up),
            l => SwapEffect(r.Up, r.Down, l),
            l => SwapDescription(r.Up, r.Down, l)))
        .ToArray();

    // ---- THE DIRECTION RULE ------------------------------------------------------------------

    /// <summary>The (raised, lowered) pair of a stat-swap skill, or null if the id isn't one.</summary>
    public static (SwapStat Up, SwapStat Down)? StatSwapOf(string skillId)
    {
        foreach (var r in SwapTable)
            if (r.Id == skillId) return (r.Up, r.Down);
        return null;
    }

    /// <summary>Why <paramref name="skillId"/> may NOT be learned given what is already known, or
    /// null if it may. Enforces the direction rule: once a swap sets a stat's direction, no later
    /// swap may push that stat the other way.
    ///
    /// Returns the message to show the player, naming the skill that blocks the pick.
    ///
    /// Note the ONE thing this deliberately allows: another skill that also LOWERS the same stat.
    /// Two swaps can both sell MEN, and they stack (−5 and −5 = −10). It's only reversals that are
    /// banned, which is exactly what makes the net-zero ring (+A−B, +B−C, +C−A) unreachable.</summary>
    public static string? StatSwapConflict(string skillId, IEnumerable<string> learnedSkillIds)
    {
        if (StatSwapOf(skillId) is not { } candidate) return null;   // not a swap → nothing to police
        var (up, down) = candidate;

        foreach (var learnedId in learnedSkillIds)
        {
            if (learnedId == skillId) continue;
            if (StatSwapOf(learnedId) is not { } holdPair) continue;
            var (heldUp, heldDown) = holdPair;
            string held = Get(learnedId)?.Name ?? learnedId;

            if (heldUp == up)                                  // two skills raising the same stat
                return $"You have already committed to {held}. Only one skill may raise {Label(up)}.";
            if (heldDown == up)                                // we sold this stat; can't buy it back
                return $"{held} already sacrifices {Label(up)}. You cannot raise a stat you have given up.";
            if (heldUp == down)                                // we bought this stat; can't sell it
                return $"{held} already raises {Label(down)}. You cannot sacrifice a stat you have bought.";
        }
        return null;
    }

    // NOTE: there is deliberately NO "auto-pick a legal subset" helper. It is tempting (debug
    // "learn all skills" wants one) but any subset is an arbitrary BUILD decision, and the obvious
    // greedy pick — take each in turn, skip what it bans — lands on four swaps that all sacrifice
    // ATK, our single power stat, for −20 ATK. Debug learn-all therefore grants NO swaps at all.

    /// <summary>The stat swaps a class may buy. EVERY group is class-gated (owner, 2026-07-15), not
    /// just the ATK group — a class only ever trades among the stats it actually uses:
    ///   • FIGHTER: only CON / DEX / ATK (the physical stats) — CON↔DEX, ATK↔CON, ATK↔DEX.
    ///   • MAGE:    CON↔DEX, ATK↔WIT, ATK↔MEN, WIT↔MEN (never the DEX-for-ATK physical trades).
    /// "X↔Y" = both directions (+X−Y and +Y−X); the DIRECTION rule then stops you owning both.
    /// The BUFFER (Warchanter) keeps ALL of them on purpose — he can pay in a stat he barely uses;
    /// if that proves too strong, move him to a dual-cost (+ATK −a −b) form instead.</summary>
    public static IEnumerable<string> StatSwapsFor(BaseClass baseClass, Discipline? discipline)
    {
        bool buffer = discipline == Discipline.Warchanter;

        // CON↔DEX is shared by both classes (both care about CON and DEX).
        yield return SwapConDex; yield return SwapDexCon;

        if (buffer || baseClass == BaseClass.Fighter)
        {
            // ATK↔CON, ATK↔DEX — the fighter juggles only physical stats.
            yield return SwapAtkCon; yield return SwapConAtk;
            yield return SwapAtkDex; yield return SwapDexAtk;
        }

        if (buffer || baseClass == BaseClass.Mage)
        {
            // ATK↔WIT, ATK↔MEN, WIT↔MEN — the caster juggles power, cast/crit (WIT) and MP/M.Def (MEN).
            yield return SwapAtkWit; yield return SwapWitAtk;
            yield return SwapAtkMen; yield return SwapMenAtk;
            yield return SwapWitMen; yield return SwapMenWit;
        }
    }
}
