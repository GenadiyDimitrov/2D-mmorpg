namespace Game.Shared;

/// <summary>
/// THE premium reward-rune table — the ladder, the five channels and the words each rung is
/// described with. Owner's spec, 2026-08-12:
///
/// <list type="bullet">
/// <item>Rune of Experience (X%) · Rune of Skillpoints (X%) · Rune of Exp/SP (X%)</item>
/// <item>Rune of Gold (X%) — the gold a kill drops · Rune of Drop (X%) — the CHANCE its table rolls</item>
/// <item>Rune of Sinister — *"stops the exp gain (so a grinder can grind and no lvl up)"*; gold and
///       drops untouched.</item>
/// <item>Rune of Sinners — *"A timed rune given by the Gods to punish those who sinned. Exp/Sp/Gold/
///       Drop are 0."*</item>
/// </list>
///
/// <para>It is a class of its OWN, not a corner of SkillCatalog or ItemCatalog, and that is
/// load-bearing: both of those catalogs build a rune from this table, and a static table living on
/// either one would make the other's build touch its static state at initialization time. The two
/// catalogs only ever share compile-time <c>const</c> ids today, which is why that has never bitten.</para>
///
/// <para>One SKILL per channel whose LEVELS are the rungs, and one ITEM per rung pointing at it
/// (<c>ItemDef.RuneBuffLevel</c>). That is what makes a stronger rung EVICT a weaker one — same
/// family key, one buff — rather than the two stacking into +160%.</para>
/// </summary>
public static class RewardRunes
{
    /// <summary>The rungs, as fractions added to a neutral 1. His +5%, then tenths to +100%:
    /// *"u can make all the levels from 0,1~2 over 0.1 -&gt; 1.1,1.2,1.3,1.4 ... x2"*. The INDEX is
    /// the skill level − 1. x0 is not a rung here — it is the two named zeroing runes instead.</summary>
    public static readonly float[] Ladder =
    {
        0.05f, 0.10f, 0.20f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f, 1.00f,
    };

    /// <summary>What a rung READS as: 5, 10, 20 … 100. The item id, the item name, the skill level's
    /// text and the buff bar all derive from this one number, so none of them can drift.</summary>
    public static int Percent(int rung) => (int)MathF.Round(Ladder[rung] * 100f);

    /// <summary>The rung index for a percentage, or -1 if it isn't on the ladder.</summary>
    public static int RungOf(int percent)
    {
        for (int i = 0; i < Ladder.Length; i++)
            if (Percent(i) == percent) return i;
        return -1;
    }

    /// <summary>One reward channel: which skill carries its ladder, what it is called, and what a
    /// rung of it DOES (<see cref="RatesAt"/>).</summary>
    public readonly record struct Channel(string Key, string SkillId, string Name, string Abbrev)
    {
        /// <summary>This channel's reward package at a given fraction. The channel key IS the
        /// switch, so adding a channel is one row in <see cref="All"/> and nothing else.</summary>
        public RewardRates RatesAt(float p) => Key switch
        {
            KeyExp   => new RewardRates(Exp: p),
            KeySp    => new RewardRates(Sp: p),
            KeyExpSp => new RewardRates(Exp: p, Sp: p),
            KeyGold  => new RewardRates(Gold: p),
            KeyDrop  => new RewardRates(Drop: p),
            _ => default,
        };

        /// <summary>The ITEM id of one rung of this channel — <c>rune_exp_20</c>. The percentage is
        /// in the id on purpose: an id that states its own number cannot come to mean another.</summary>
        public string ItemId(int percent) => $"rune_{Key}_{percent}";

        /// <summary>The item/buff NAME of one rung — "Rune of Experience (20%)".</summary>
        public string NameAt(int percent) => $"{Name} ({percent}%)";

        /// <summary>The one sentence a rung is described with, on the item card AND on the buff bar.</summary>
        public string Line(int percent) => Key switch
        {
            KeyExp   => $"Held rune: +{percent}% EXPERIENCE from monsters while it is in your bag.",
            KeySp    => $"Held rune: +{percent}% SP from monsters while it is in your bag.",
            KeyExpSp => $"Held rune: +{percent}% experience AND +{percent}% SP from monsters while it is in your bag.",
            KeyGold  => $"Held rune: +{percent}% GOLD from monsters while it is in your bag.",
            KeyDrop  => $"Held rune: +{percent}% DROP CHANCE on every monster drop while it is in your bag.",
            _ => "",
        };
    }

    public const string KeyExp   = "exp";
    public const string KeySp    = "sp";
    public const string KeyExpSp = "expsp";
    public const string KeyGold  = "gold";
    public const string KeyDrop  = "drop";

    /// <summary>The five ladder channels. Skill ids are the <c>SkillCatalog</c> consts, spelled out
    /// here as literals for the same reason the table lives in its own class — a const is inlined at
    /// compile time, so naming them costs nothing and triggers no catalog.</summary>
    public static readonly Channel[] All =
    {
        new(KeyExp,   "rune_exp",   "Rune of Experience",  "EXP"),
        new(KeySp,    "rune_sp",    "Rune of Skillpoints", "SP"),
        new(KeyExpSp, "rune_expsp", "Rune of Exp/SP",      "XSP"),
        new(KeyGold,  "rune_gold",  "Rune of Gold",        "GLD"),
        new(KeyDrop,  "rune_drop",  "Rune of Drop",        "DRP"),
    };

    // ---- The two ZEROING runes. Single-rung, and they do NOT compete with the ladders: they hold
    //      their own buff families and win by hard override when the rates are folded, so no pile of
    //      +100% runes can dilute either of them. ----
    public const string SinisterId = "rune_sinister";
    public const string SinnersId  = "rune_sinners";
    public const string SinisterName = "Rune of Sinister";
    public const string SinnersName  = "Rune of Sinners";

    public const string SinisterLine =
        "Held rune: you gain NO experience and NO SP from monsters while it is in your bag. "
        + "Gold and drops are untouched — grind for items without levelling.";

    /// <summary>His words, kept: *"A timed rune given by the Gods to punish those who sinned. "
    /// Exp/Sp/Gold/Drop are 0. Keeper cannot accept this item, or it cannot be sold or discarded as
    /// its bound to your soul for the time it has left."*</summary>
    public const string SinnersLine =
        "A timed rune given by the Gods to punish those who sinned: experience, SP, gold and drops "
        + "are ALL zero while it is in your bag. Bound to your soul for the time it has left — no "
        + "keeper will accept it, and it cannot be sold, traded or destroyed. It leaves when it expires.";

    /// <summary>Default lifespan of a reward rune when nothing overrides it: 24 hours. A rune box or
    /// <c>/give &lt;player&gt; &lt;id&gt; - - 7d</c> stamps its own clock instead.</summary>
    public const int DefaultSeconds = 24 * 3600;
}
