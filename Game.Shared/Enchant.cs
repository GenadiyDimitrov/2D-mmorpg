namespace Game.Shared;

public enum EnchantResult { Success = 0, Broke = 1, Reset = 2, Downgraded = 3, Failed = 4 }

/// <summary>
/// Enchanting rules (design doc). Success chance falls in bands as the
/// enchant level climbs; the three scroll kinds differ only in what happens
/// on failure. Lives in Shared so the client can show odds before committing.
/// </summary>
public static class EnchantRules
{
    public const int MaxEnchant = 16;

    /// <summary>Chance the NEXT enchant (current -> current+1) succeeds.
    /// +1..+3 = 100%, +4..+6 = 66%, +7..+9 = 40%, +10..+16 = 20%.</summary>
    public static float SuccessChance(int currentLevel) => currentLevel switch
    {
        < 3 => 1.00f,       // going to +1, +2, +3
        < 6 => 0.66f,       // +4, +5, +6
        < 9 => 0.40f,       // +7, +8, +9
        < MaxEnchant => 0.20f,
        _ => 0f             // already maxed
    };

    /// <summary>Each enchant level adds this fraction of the item's base
    /// bonus, so a +10 weapon is meaningfully stronger. Flat per-level on top
    /// keeps low-bonus items relevant.</summary>
    public static int BonusAt(int baseBonus, int enchantLevel)
    {
        if (enchantLevel <= 0 || baseBonus <= 0)
            return baseBonus;
        // +20% of base per level, plus 1 flat per level.
        return baseBonus + (int)(baseBonus * 0.20f * enchantLevel) + enchantLevel;
    }

    /// <summary>Resolve one enchant attempt. Returns the outcome and the new
    /// enchant level. Caller consumes the scroll regardless of outcome.</summary>
    public static (EnchantResult Result, int NewLevel) Attempt(
        int currentLevel, ScrollKind kind, Random rng)
    {
        if (currentLevel >= MaxEnchant)
            return (EnchantResult.Failed, currentLevel);

        if (rng.NextDouble() < SuccessChance(currentLevel))
            return (EnchantResult.Success, currentLevel + 1);

        // Failure path depends on the scroll kind.
        return kind switch
        {
            ScrollKind.Common => (EnchantResult.Broke, currentLevel),       // item destroyed
            ScrollKind.Uncommon => (EnchantResult.Reset, 0),                // back to +0
            ScrollKind.Rare => (EnchantResult.Downgraded, Math.Max(0, currentLevel - 1)),
            _ => (EnchantResult.Failed, currentLevel)
        };
    }
}
