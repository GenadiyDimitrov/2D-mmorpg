namespace Game.Shared;

/// <summary>
/// Tunable values shared by server and all clients.
/// Server is authoritative — clients use these only for prediction/UI.
/// </summary>
public static class GameConstants
{
    /// <summary>Display name of the in-game currency. Generic on purpose (no IP);
    /// change here to rebrand everywhere it's shown.</summary>
    public const string CurrencyName = "Gold";

    /// <summary>Simulation ticks per second on the server.</summary>
    public const int TickRate = 10;

    /// <summary>Seconds per tick (0.1s at 10 t/s).</summary>
    public const float TickSeconds = 1f / TickRate;

    /// <summary>How far (world units) an entity can see other entities.</summary>
    public const float ViewRange = 3000f;

    /// <summary>Interest-management cell size. Equal to ViewRange so a 3x3
    /// cell neighborhood always covers the full view circle.</summary>
    public const float CellSize = 3000f;

    /// <summary>Demo zone size. Design target is 75000x75000; we use a
    /// smaller zone while there is only one of them.</summary>
    public const float ZoneWidth = 15000f;
    public const float ZoneHeight = 15000f;

    /// <summary>Base/maximum player movement speed in units per second.</summary>
    public const float BasePlayerSpeed = 250f;

    public const int MaxCharacterNameLength = 16;

    // ----- Safe zone (town) ---------------------------------------------------

    /// <summary>No mobs spawn or enter; aggro clears on players inside;
    /// natural regen is multiplied while inside.</summary>
    public const float SafeZoneRadius = 1200f;

    public const int SafeZoneRegenMultiplier = 5;

    /// <summary>True inside ANY placed safe zone (see WorldMap.SafeZones).</summary>
    public static bool InSafeZone(float x, float y) => WorldMap.InAnySafeZone(x, y);

    // ----- Combat ----------------------------------------------------------------

    /// <summary>Base melee range. Design doc: base attack range is 40;
    /// we use 80 for a forgiving feel until weapons define ranges.</summary>
    public const float MeleeRange = 80f;

    /// <summary>Ticks between player basic attacks (1.5s). Attack-speed
    /// stats/buffs will modify this in a later phase.</summary>
    public const int PlayerAttackIntervalTicks = 15;

    /// <summary>Ticks between mob basic attacks (2.0s).</summary>
    public const int MobAttackIntervalTicks = 20;

    /// <summary>Aggressive mobs attack players that come this close.</summary>
    public const float MobAggroRange = 400f;

    /// <summary>A mob chased this far from home resets: returns and heals.
    /// Kept tight to match the short aggro range.</summary>
    public const float MobLeashRange = 1500f;

    /// <summary>Ticks until a dead mob respawns at its home position (10s).</summary>
    public const int MobRespawnTicks = 100;

    /// <summary>Out-of-combat regen is applied once per this many ticks (1s).</summary>
    public const int RegenIntervalTicks = 10;

    // ----- Chat ---------------------------------------------------------------------

    /// <summary>Client keeps at most this many lines per chat tab.</summary>
    public const int ChatHistoryLimit = 150;

    // ----- Items / progression / trade (Phase 4) -------------------------------

    public const int InventorySize = 30;

    public const int ClassChangeLevel = 20;

    /// <summary>Archer second classes: +500 basic-attack range with a ranged
    /// weapon, capped at 1100 (design doc).</summary>
    public const float ArcherRangeBonus = 500f;
    public const float MaxBasicAttackRange = 1100f;

    public const int TradeMaxOfferSlots = 10;

    /// <summary>Both characters must be this close to start a trade.</summary>
    public const float TradeRange = 300f;

    // ----- Admin / jail (Phase 5) ----------------------------------------------

    /// <summary>Jail is a corner of the map; jailed players are pinned here.</summary>
    public const float JailX = 500f;
    public const float JailY = 500f;

    /// <summary>Periodic character auto-save interval (ticks). 600 = 60s.</summary>
    public const int AutoSaveIntervalTicks = 600;

    /// <summary>Skill points earned per exp point (≈ 1/4 of exp).</summary>
    public const float SkillPointRatio = 0.25f;

    /// <summary>How close you must be to an NPC to talk.</summary>
    public const float TalkRange = 250f;

    // ----- Vendors (Phase 21) -------------------------------------------------

    /// <summary>Fraction of an item's Value a vendor pays when you SELL to it.</summary>
    public const float VendorSellFraction = 0.30f;

    /// <summary>Extra fraction added to an item's Value when you BUY from a vendor.
    /// Reserved for the future castle system: a vendor in a castle-owned village
    /// charges this surcharge and the surcharge flows to the castle vault. 0 until
    /// castles exist (no current vendor is castle-owned).</summary>
    public const float VendorBuyTaxRate = 0.0f;

    // ----- Teleport-for-fee (Phase 22) ----------------------------------------

    /// <summary>Gold charged per world unit of teleport distance.</summary>
    public const float TeleportGoldPerUnit = 0.04f;

    /// <summary>Minimum teleport fee regardless of distance.</summary>
    public const int TeleportMinFee = 50;

    /// <summary>Gold fee to warp between two safe zones (distance-based).</summary>
    public static int TeleportFee(SafeZone from, SafeZone to)
    {
        float dx = to.X - from.X, dy = to.Y - from.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        return Math.Max(TeleportMinFee, (int)(dist * TeleportGoldPerUnit));
    }
}
