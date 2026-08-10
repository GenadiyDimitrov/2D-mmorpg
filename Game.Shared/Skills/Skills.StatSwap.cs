namespace Game.Shared;

/// <summary>
/// The level-40 STAT-SWAP passives — the only thing in the game that moves your main stats.
///
/// You are born with your CON/ATK/WIT/AGI/SPT and nothing raises them for free (the old
/// LevelStatBonus and the class-change stat grants are both gone). From 40 you buy RUNGS: each rung
/// is <b>+1 to one stat and −1 to another</b>, and you may buy nine of them.
///
/// <b>REBUILT 2026-08-10 (owner, playtest-20 find #4).</b> It used to sell coarse bundles under a
/// "direction rule" that forbade ever pushing a stat back the other way. He replaced both with plain
/// arithmetic — *"The 9 are stats… each +1-1 have its cost and can do this 9 times so +9-9… the
/// maximum of 1 stat is +5"*:
/// <list type="bullet">
///   <item><b>A rung is +1/−1.</b> A skill is one PAIR, and its level is how many rungs you have put
///         into that pair.</item>
///   <item><b>At most +5 on any single stat</b> (<see cref="StatSwapMaxPerStat"/>) — which is why a
///         pair caps at level 5, and why <c>+9 AGI −9 CON</c> is impossible.</item>
///   <item><b>At most 9 rungs in total</b> (<see cref="StatSwapMaxTotal"/>), so the raises sum to 9
///         and so do the sacrifices. His examples: <c>+5 AGI −5 CON</c> then <c>+4 ATK −4 CON</c>
///         (= +5/+4/−9), or <c>+5 ATK −5 SPT</c>, <c>+2 WIT −2 SPT</c>, <c>+2 CON −2 AGI</c>.</item>
///   <item><b>No cap on how far a stat is SOLD</b> beyond the 9 total — CON reaching −9 above is the
///         point of the example.</item>
/// </list>
///
/// <b>THE DIRECTION RULE IS GONE</b>, at his instruction — the old <c>StatSwapConflict</c> forbade
/// cancelling yourself, so <c>+5 AGI −5 CON</c> followed by <c>+4 CON −4 AGI</c> was illegal. It is
/// legal now, and it nets <c>+1 AGI −1 CON</c>: buying a fine-grained trade out of two coarse ones is
/// a legitimate way to spend rungs, not an exploit. The ring it used to prevent (+A−B, +B−C, +C−A
/// netting zero) is now simply a bad use of nine rungs, and the price schedule already punishes it.
///
/// <b>PRICE IS PER RUNG AND GLOBAL</b> — <see cref="StatSwapGold"/>, 1/2/3/4/5/5/5/5/5 kk, indexed by
/// how many rungs you ALREADY own, so all nine cost 35kk however you spread them. It is charged at
/// purchase (<c>StatSwapRungPrice</c>) rather than baked into the SkillLevel, because the price of a
/// rung depends on the character, not on which pair it belongs to.
///
/// <b>WHO MAY BUY WHAT</b> (his lists): a class only trades among the stats it uses.
///   • FIGHTER: ATK↔AGI, ATK↔CON, AGI↔CON, and +SPT −ATK.
///   • MAGE and CLERIC: ATK↔WIT, ATK↔SPT, WIT↔SPT, AGI↔CON.
///   • The BUFFER (Warchanter) keeps everything, as before.
///
/// Still passives, still bought from the Mindwriter, still resettable there — removing is free, the
/// gold is not refunded.
/// </summary>
public static partial class SkillCatalog
{
    // ---- Group ids (mutually exclusive within a group) ----
    public const string GroupSwapCon = "swap_con";
    public const string GroupSwapAgi = "swap_agi";
    public const string GroupSwapAtk = "swap_atk";
    public const string GroupSwapWit = "swap_wit";
    public const string GroupSwapMen = "swap_men";

    // ---- Skill ids: swap_<raised>_<sacrificed> ----
    // ⚠ The four AGI ids still SPELL "dex", and must keep spelling it. A skill id is a persisted key
    // (LearnedSkills stores the string) and the project rule is that ids are append-only — renaming
    // these would silently delete a 15kk purchase from every character that owns one. The stat was
    // renamed DEX -> AGI everywhere a player can see it; this is the one place the old spelling is
    // load-bearing rather than cosmetic. Same reasoning that keeps `swap_men*` spelling MEN after
    // Spirit replaced it.
    public const string SwapConAtk = "swap_con_atk";   // +CON −ATK
    public const string SwapConAgi = "swap_con_dex";   // +CON −AGI
    public const string SwapAgiAtk = "swap_dex_atk";   // +AGI −ATK
    public const string SwapAgiCon = "swap_dex_con";   // +AGI −CON
    public const string SwapAtkAgi = "swap_atk_dex";   // +ATK −AGI   (fighter)
    public const string SwapAtkCon = "swap_atk_con";   // +ATK −CON   (fighter)
    public const string SwapAtkWit = "swap_atk_wit";   // +ATK −WIT   (mage)
    public const string SwapAtkMen = "swap_atk_men";   // +ATK −MEN   (mage)
    public const string SwapWitAtk = "swap_wit_atk";   // +WIT −ATK
    public const string SwapWitMen = "swap_wit_men";   // +WIT −MEN
    public const string SwapMenAtk = "swap_men_atk";   // +MEN −ATK
    public const string SwapMenWit = "swap_men_wit";   // +MEN −WIT

    /// <summary>All levels unlock at 40 — the gate is GOLD, not level.</summary>
    public const int StatSwapLearnLevel = 40;

    /// <summary>Most a single stat may be RAISED, and so the level cap of any one pair.</summary>
    public const int StatSwapMaxPerStat = 5;

    /// <summary>Most rungs a character may own across every swap. Raises total this, and so do the
    /// sacrifices — a rung is +1/−1.</summary>
    public const int StatSwapMaxTotal = 9;

    /// <summary>Gold for the Nth rung you buy, indexed by how many you already own: 1, 2, 3, 4, then
    /// 5kk for the rest. All nine = 35kk however you spread them across pairs.</summary>
    private static readonly int[] StatSwapGold =
    {
        1_000_000, 2_000_000, 3_000_000, 4_000_000, 5_000_000,
        5_000_000, 5_000_000, 5_000_000, 5_000_000,
    };

    /// <summary>What the NEXT rung costs a character who already owns <paramref name="rungsOwned"/>.</summary>
    public static int StatSwapRungPrice(int rungsOwned) =>
        StatSwapGold[Math.Clamp(rungsOwned, 0, StatSwapGold.Length - 1)];

    /// <summary>Total gold for rungs <paramref name="from"/>..<paramref name="to"/> (0-based, exclusive
    /// end). Used both to price a multi-rung purchase and to tell the reset NPC what a skill's rungs
    /// are worth — since price depends on POSITION, forgetting a pair frees the topmost positions.</summary>
    public static long StatSwapPriceRange(int from, int to)
    {
        long sum = 0;
        for (int i = Math.Max(0, from); i < to; i++) sum += StatSwapRungPrice(i);
        return sum;
    }

    /// <summary>How many swap rungs this character owns — the sum of every swap skill's level.</summary>
    public static int StatSwapRungsOwned(IEnumerable<KeyValuePair<string, int>> learned)
    {
        int n = 0;
        foreach (var (id, level) in learned)
            if (StatSwapOf(id) is not null) n += level;
        return n;
    }

    /// <summary>How far a stat is currently RAISED across every swap owned.</summary>
    public static int StatSwapRaiseOf(SwapStat stat, IEnumerable<KeyValuePair<string, int>> learned)
    {
        int n = 0;
        foreach (var (id, level) in learned)
            if (StatSwapOf(id) is { } pair && pair.Up == stat) n += level;
        return n;
    }

    // SPT is a REAL STAT now (owner, 2026-07-20), so a Spirit swap is just ±1 SPT per level like
    // every other swap — no more bundling percentages. The percentages this used to emit are gone;
    // the stat itself carries Max MP, M.Def and MP regen through StatCalculator.SptModifier.

    /// <summary>Build a stat-swap passive: <see cref="StatSwapMaxPerStat"/> levels, each +1/−1 further.
    /// <paramref name="at"/> maps a level to that level's cumulative effect.
    ///
    /// <para>⚠ <c>GoldCost</c> is deliberately 0 on every level. The price of a rung depends on how
    /// many rungs the CHARACTER already owns, not on which pair it belongs to, so it is charged at
    /// purchase from <see cref="StatSwapRungPrice"/>. A static per-level cost here would be a second,
    /// wrong answer sitting next to the right one.</para></summary>
    private static SkillDef StatSwap(string id, string name, string group,
        System.Func<int, PassiveEffect> at, System.Func<int, string> describe) => new(
        id, name, BaseClass.Fighter, SkillEffect.None,
        MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
        Category: SkillCategory.Passive, SpCost: 0, ExclusiveGroup: group,
        Description: describe(StatSwapMaxPerStat),
        Levels: System.Linq.Enumerable.Range(1, StatSwapMaxPerStat)
            .Select(l => new SkillLevel(SpCost: 0, GoldCost: 0,
                Passive: at(l), Description: describe(l)))
            .ToArray());

    /// <summary>±Spirit — now simply ±SPT, the stat.</summary>
    private static PassiveEffect MenSwap(int sptDelta, int con = 0, int agi = 0, int atk = 0, int wit = 0) =>
        new(Con: con, Agi: agi, Atk: atk, Wit: wit, Spt: sptDelta);

    private static string Swap2(int n, string up, string down) =>
        $"Passive. +{n} {up}, −{n} {down}. ({n} of your {StatSwapMaxTotal} stat rungs.)";

    private static string SwapMenUp(int n, string down) =>
        $"Passive. +{n} SPT (Max MP, M.Def, MP regen); −{n} {down}. ({n} of your {StatSwapMaxTotal} stat rungs.)";

    private static string SwapMenDown(int n, string up) =>
        $"Passive. +{n} {up}; −{n} SPT (Max MP, M.Def, MP regen). ({n} of your {StatSwapMaxTotal} stat rungs.)";

    /// <summary>A stat a swap can move. MEN is not a real stat any more (it IS its modifiers — see
    /// <see cref="MenSwap"/>), but for the DIRECTION rule it commits exactly like the others.</summary>
    public enum SwapStat { Con, Agi, Atk, Wit, Men }

    /// <summary>THE source of truth for the swaps: id, display name, the stat it RAISES and the stat
    /// it LOWERS. Everything else — the exclusive group, the PassiveEffect, the description and the
    /// direction rule — is derived from this, so a new swap cannot fall out of sync with the rule
    /// that polices it. Skill ids follow <c>swap_&lt;raised&gt;_&lt;sacrificed&gt;</c>.</summary>
    private static readonly (string Id, string Name, SwapStat Up, SwapStat Down)[] SwapTable =
    {
        (SwapConAtk, "Fortitude (Power)",   SwapStat.Con, SwapStat.Atk),
        (SwapConAgi, "Fortitude (Agility)", SwapStat.Con, SwapStat.Agi),

        (SwapAgiAtk, "Agility (Power)",     SwapStat.Agi, SwapStat.Atk),
        (SwapAgiCon, "Agility (Vigour)",    SwapStat.Agi, SwapStat.Con),

        // ATK group is class-gated (buffers may take all four) — see StatSwapsFor.
        (SwapAtkAgi, "Power (Agility)",     SwapStat.Atk, SwapStat.Agi),
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
        int con = 0, agi = 0, atk = 0, wit = 0, men = 0;
        void Move(SwapStat s, int d)
        {
            switch (s)
            {
                case SwapStat.Con: con += d; break;
                case SwapStat.Agi: agi += d; break;
                case SwapStat.Atk: atk += d; break;
                case SwapStat.Wit: wit += d; break;
                case SwapStat.Men: men += d; break;
            }
        }
        Move(up, l);
        Move(down, -l);
        return men == 0
            ? new PassiveEffect(Con: con, Agi: agi, Atk: atk, Wit: wit)
            : MenSwap(men, con: con, agi: agi, atk: atk, wit: wit);
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

    /// <summary>Why <paramref name="skillId"/> may not be raised to <paramref name="targetLevel"/>
    /// given what is already known, or null if it may. Returns the message to show the player.
    ///
    /// <para>Two numeric limits, and nothing else. The DIRECTION rule this replaced (2026-08-10) also
    /// banned reversing a stat, so <c>+5 AGI −5 CON</c> then <c>+4 CON −4 AGI</c> was illegal; the
    /// owner struck that out — it is a legitimate way to buy the fine-grained <c>+1 AGI −1 CON</c> that
    /// no single pair sells. So a stat may now be raised by several pairs at once, and the exclusive
    /// group no longer blocks a swap purchase (it is kept only so the reset NPC can still find them).</para></summary>
    public static string? StatSwapConflict(string skillId, int targetLevel,
                                           IEnumerable<KeyValuePair<string, int>> learned)
    {
        if (StatSwapOf(skillId) is not { } candidate) return null;   // not a swap → nothing to police
        var (up, _) = candidate;

        int current = 0;
        foreach (var (id, level) in learned)
            if (id == skillId) { current = level; break; }
        int added = targetLevel - current;
        if (added <= 0) return null;

        // 1. The per-stat ceiling. Counted across EVERY pair that raises it, not just this one.
        int raised = StatSwapRaiseOf(up, learned) + added;
        if (raised > StatSwapMaxPerStat)
            return $"{Label(up)} may only be raised by {StatSwapMaxPerStat}. You are already at "
                 + $"+{StatSwapRaiseOf(up, learned)}.";

        // 2. The character-wide rung budget.
        int owned = StatSwapRungsOwned(learned);
        if (owned + added > StatSwapMaxTotal)
            return $"You may move {StatSwapMaxTotal} stat points in total, and you have spent {owned}.";

        return null;
    }

    // NOTE: there is deliberately NO "auto-pick a legal subset" helper. It is tempting (debug
    // "learn all skills" wants one) but any subset is an arbitrary BUILD decision, and the obvious
    // greedy pick — take each in turn, skip what it bans — lands on four swaps that all sacrifice
    // ATK, our single power stat, for −20 ATK. Debug learn-all therefore grants NO swaps at all.

    /// <summary>The stat swaps a class may buy — his playtest-20 lists verbatim. A class only ever
    /// trades among the stats it actually uses, and "X↔Y" means both directions are on the shelf.
    ///   • FIGHTER: ATK↔AGI, ATK↔CON, AGI↔CON, plus <b>+SPT −ATK</b> — the one-way door that lets a
    ///     fighter buy MP/M.Def with power. There is no +ATK −SPT for him: a fighter selling Spirit
    ///     for power is the mage's trade, and he named it only for the mage.
    ///   • MAGE: ATK↔WIT, ATK↔SPT, WIT↔SPT, AGI↔CON.
    ///   • CLERIC = MAGE, at his instruction — a passive already balances their M.Atk with a melee
    ///     weapon, so they are casters for this purpose whatever they are holding.
    /// The BUFFER (Warchanter) keeps ALL of them, as before — he can pay in a stat he barely uses.</summary>
    public static IEnumerable<string> StatSwapsFor(BaseClass baseClass, Discipline? discipline)
    {
        bool buffer = discipline == Discipline.Warchanter;
        // A LIST with a contains-check, not two yield blocks: +SPT −ATK is on both his lists, and a
        // buffer takes both branches — it would otherwise appear twice on the Mindwriter's shelf.
        var list = new List<string>();
        void Offer(string id) { if (!list.Contains(id)) list.Add(id); }

        // AGI↔CON is on both lists — every class cares about both.
        Offer(SwapConAgi); Offer(SwapAgiCon);

        if (buffer || baseClass == BaseClass.Fighter)
        {
            Offer(SwapAtkCon); Offer(SwapConAtk);
            Offer(SwapAtkAgi); Offer(SwapAgiAtk);
            Offer(SwapMenAtk);                       // +SPT −ATK, one way
        }

        if (buffer || baseClass == BaseClass.Mage)
        {
            Offer(SwapAtkWit); Offer(SwapWitAtk);
            Offer(SwapAtkMen); Offer(SwapMenAtk);
            Offer(SwapWitMen); Offer(SwapMenWit);
        }
        return list;
    }
}
