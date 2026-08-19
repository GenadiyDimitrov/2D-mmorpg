namespace Game.Shared;

/// <summary>
/// ONE bundle of reward multipliers, so the five knobs travel together instead of as five loose
/// floats per scope (owner, 2026-08-18: *"we will need a class that have {exp,gold,sp,dropChance,
/// dropAmount} so the game will have WorldMod = new RatesMod(1,1,1,1,1) and QuestMod = new (1,1,1,1,1)
/// -> so no 20 floating stats"*).
///
/// <para>There are three scopes and they COMPOSE with <c>*</c>, never by arithmetic at a call site:
/// <see cref="RateConfig.World"/> (the server's own rates) × <see cref="RateConfig.Quest"/> (quest
/// rewards only) × the player's own <c>Entity.Runes</c> (the premium reward runes). One multiplication
/// order, one place to read, and a new scope is a new field rather than four more statics.</para>
///
/// <para>⚠ A <c>readonly record struct</c> on purpose: these are copied and multiplied constantly on
/// the kill path, and value semantics mean a scope can never be mutated through a shared reference by
/// something that only meant to scale it.</para>
/// </summary>
/// <param name="Exp">Experience gained.</param>
/// <param name="Sp">Skill points gained. SP still derives from exp at
/// <see cref="GameConstants.SkillPointRatio"/>; this scales the result, so holding it EQUAL to
/// <paramref name="Exp"/> keeps the SP economy exactly where x1 put it — you reach a level having
/// earned the same SP, because you killed proportionally fewer creatures to get there.</param>
/// <param name="Gold">Coin from a kill (and, via <see cref="RateConfig.Quest"/>, from a quest).</param>
/// <param name="DropChance">Every drop's CHANCE. Above 100% the excess becomes COPIES rather than
/// clamping (<see cref="MobCatalog.DropCopies"/>), so this alone delivers "as if you had killed N"
/// to every group — gear, mats, scrolls and the independent rolls alike.</param>
/// <param name="DropAmount">Stack SIZE of a stackable drop, and nothing else — a piece of gear is one
/// row per copy however high this goes. ⚠ It is NOT a second rate knob: the multiplier lives in
/// <paramref name="DropChance"/>, and setting both to N gives stackables N² (owner asked for one
/// number, and this is the one that isn't it).</param>
public readonly record struct RateSet(
    float Exp, float Sp, float Gold, float DropChance, float DropAmount)
{
    /// <summary>The neutral element — every channel untouched.</summary>
    public static readonly RateSet One = new(1f, 1f, 1f, 1f, 1f);

    /// <summary>Every channel EXCEPT <see cref="DropAmount"/> at <paramref name="n"/>. That omission is
    /// the point: exp, sp, gold and drop chance at x30 make the game thirty times faster with the
    /// economy unchanged, and stack size must stay at 1 or stackables would take the multiplier twice.
    /// This is the one-liner for "make it N times faster".</summary>
    public static RateSet Uniform(float n) => new(n, n, n, n, 1f);

    /// <summary>Compose two scopes. The world's rate times the quest's times the player's rune.</summary>
    public static RateSet operator *(RateSet a, RateSet b) => new(
        a.Exp * b.Exp, a.Sp * b.Sp, a.Gold * b.Gold,
        a.DropChance * b.DropChance, a.DropAmount * b.DropAmount);

    /// <summary>The same set with every channel floored at zero — what an admin edit is run through, so
    /// a negative typed into the tuning panel can never invert a reward. Zero is legal and meaningful:
    /// it is exactly what the Rune of Sinister (no exp/sp) and the Rune of Sinners (nothing) do.</summary>
    public RateSet Clamped() => new(
        Math.Max(0f, Exp), Math.Max(0f, Sp), Math.Max(0f, Gold),
        Math.Max(0f, DropChance), Math.Max(0f, DropAmount));
}
