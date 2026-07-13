namespace Game.Shared;

/// <summary>
/// The level-40 STAT-SWAP passives — the only thing in the game that moves your main stats.
///
/// You are born with your CON/ATK/WIT/DEX and nothing raises them for free any more (the old
/// LevelStatBonus and the class-change stat grants are both gone). At 40 you may buy ONE trade-off
/// per group: each level gives <b>+1 to one stat and −1 to another</b>, up to +5/−5 at level 5.
/// They cost GOLD, not SP — 1kk / 2kk / 3kk / 4kk / 5kk (15kk to max a single skill).
///
/// Skills are grouped by the stat they RAISE, and the group is mutually exclusive: learn +CON−DEX
/// and you can never also learn +CON−ATK. You commit to a direction.
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

    /// <summary>What ONE point of the retired MEN stat is worth, now that it IS its modifiers:
    /// 2% Max MP, 2% M.Def and 2% MP regen. So a maxed ±5 swap moves each by ±10%.</summary>
    private const float MenPointPct = 0.02f;

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

    /// <summary>±MEN as its modifiers (MEN is not a stat any more).</summary>
    private static PassiveEffect MenSwap(int menDelta, int con = 0, int dex = 0, int atk = 0, int wit = 0) =>
        new(MaxMpPct: menDelta * MenPointPct,
            MagicDefencePct: menDelta * MenPointPct,
            MpRegenPct: menDelta * MenPointPct,
            Con: con, Dex: dex, Atk: atk, Wit: wit);

    private static string Swap2(int n, string up, string down) =>
        $"Passive. +{n} {up}, −{n} {down}. (Level {n} of 5.)";

    private static string SwapMenUp(int n, string down) =>
        $"Passive. +{n * 2}% Max MP, M.Def and MP regen; −{n} {down}. (Level {n} of 5.)";

    private static string SwapMenDown(int n, string up) =>
        $"Passive. +{n} {up}; −{n * 2}% Max MP, M.Def and MP regen. (Level {n} of 5.)";

    private static SkillDef[] StatSwapSkillDefs() => new[]
    {
        // ---- Group CON ----
        StatSwap(SwapConAtk, "Fortitude (Power)",   GroupSwapCon,
            l => new PassiveEffect(Con:  l, Atk: -l), l => Swap2(l, "CON", "ATK")),
        StatSwap(SwapConDex, "Fortitude (Agility)", GroupSwapCon,
            l => new PassiveEffect(Con:  l, Dex: -l), l => Swap2(l, "CON", "DEX")),

        // ---- Group DEX ----
        StatSwap(SwapDexAtk, "Agility (Power)",     GroupSwapDex,
            l => new PassiveEffect(Dex:  l, Atk: -l), l => Swap2(l, "DEX", "ATK")),
        StatSwap(SwapDexCon, "Agility (Vigour)",    GroupSwapDex,
            l => new PassiveEffect(Dex:  l, Con: -l), l => Swap2(l, "DEX", "CON")),

        // ---- Group ATK (class-gated; buffers may take all four) ----
        StatSwap(SwapAtkDex, "Power (Agility)",     GroupSwapAtk,
            l => new PassiveEffect(Atk:  l, Dex: -l), l => Swap2(l, "ATK", "DEX")),
        StatSwap(SwapAtkCon, "Power (Vigour)",      GroupSwapAtk,
            l => new PassiveEffect(Atk:  l, Con: -l), l => Swap2(l, "ATK", "CON")),
        StatSwap(SwapAtkWit, "Power (Insight)",     GroupSwapAtk,
            l => new PassiveEffect(Atk:  l, Wit: -l), l => Swap2(l, "ATK", "WIT")),
        StatSwap(SwapAtkMen, "Power (Spirit)",      GroupSwapAtk,
            l => MenSwap(-l, atk: l),                 l => SwapMenDown(l, "ATK")),

        // ---- Group WIT ----
        StatSwap(SwapWitAtk, "Insight (Power)",     GroupSwapWit,
            l => new PassiveEffect(Wit:  l, Atk: -l), l => Swap2(l, "WIT", "ATK")),
        StatSwap(SwapWitMen, "Insight (Spirit)",    GroupSwapWit,
            l => MenSwap(-l, wit: l),                 l => SwapMenDown(l, "WIT")),

        // ---- Group MEN (raises the MEN modifiers) ----
        StatSwap(SwapMenAtk, "Spirit (Power)",      GroupSwapMen,
            l => MenSwap(l, atk: -l),                 l => SwapMenUp(l, "ATK")),
        StatSwap(SwapMenWit, "Spirit (Insight)",    GroupSwapMen,
            l => MenSwap(l, wit: -l),                 l => SwapMenUp(l, "WIT")),
    };

    /// <summary>The stat swaps a class may buy. ATK is our single power stat — it feeds P.Atk for a
    /// fighter and M.Atk for a caster (the WEAPON decides which) — so only the ATK group is gated:
    /// fighters pay in CON/DEX, mages pay in WIT/MEN. The BUFFER (Warchanter) gets all four on
    /// purpose: he can pay in a stat he barely uses. If that proves too strong, move him to a
    /// dual-cost (+ATK −a −b) form instead. Every other group is open to everyone.
    /// (The buffer is a 3rd-class DISCIPLINE, not an archetype — and 3rd class arrives at 40, the
    /// same level these unlock, so the gate is always decidable.)</summary>
    public static IEnumerable<string> StatSwapsFor(BaseClass baseClass, Discipline? discipline)
    {
        yield return SwapConAtk; yield return SwapConDex;
        yield return SwapDexAtk; yield return SwapDexCon;
        yield return SwapWitAtk; yield return SwapWitMen;
        yield return SwapMenAtk; yield return SwapMenWit;

        bool buffer = discipline == Discipline.Warchanter;
        if (buffer || baseClass == BaseClass.Fighter) { yield return SwapAtkDex; yield return SwapAtkCon; }
        if (buffer || baseClass == BaseClass.Mage)    { yield return SwapAtkWit; yield return SwapAtkMen; }
    }
}
