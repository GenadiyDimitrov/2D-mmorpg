namespace Game.Shared;

/// <summary>
/// Tunable values shared by server and all clients.
/// Server is authoritative — clients use these only for prediction/UI.
/// </summary>
public static class GameConstants
{
    /// <summary>The game version — ONE source of truth shared by server and client (both compile it in).
    /// Shown on the login screen + GET /version, logged by the server at startup, and checked at login: a
    /// client whose version differs from the server's is rejected ("please update"), because an
    /// out-of-date client speaks an out-of-date protocol (delta snapshots, DTO changes).
    ///
    /// Scheme (owner): MAJOR.MINOR.BUILD. A **bigger change** (a new system/feature) bumps the **MINOR**
    /// and resets BUILD to 0; each **in-between commit** bumps the **BUILD**. Pre-release, so MAJOR stays
    /// 0. 0.27 ≈ the ~27 major systems built so far (combat, stats, skills, 2nd/3rd classes, subclasses,
    /// combat primitives, mobs, bosses, stealth/traps, gear, attributes, crafting, vendors, party, loot,
    /// PvP/karma, auto-hunt, disconnect/Return, death/res, grade penalty, damage/heal rework, buff bar +
    /// icons, multi-row bar + items, delta snapshots, moderation, social, versioning).
    ///
    /// **Bump it on every commit that changes shipped code** — BUILD for an ordinary commit, MINOR for
    /// a new system. Because server and client both compile this in and the login handshake compares
    /// them, a bump means the APK and the server must be redeployed TOGETHER: an old client is refused
    /// rather than left to speak an old protocol. (A docs- or memory-only commit changes nothing that
    /// ships, so it does not need a bump — that would only force a pointless redeploy.)
    ///
    /// 0.28 = the client UI rebuilt on uGUI + TextMeshPro, and the WPF→Unity parity work that follows
    /// it. That whole port is ONE system, so each panel brought over bumps the BUILD — otherwise ~20
    /// windows would walk the MINOR from 0.28 to 0.48 and say nothing useful about the game.</summary>
    public const string GameVersion = "0.28.54";

    /// <summary>
    /// The WIRE contract's version, and the ONLY thing compatibility is decided on.
    ///
    /// Bump it when DTOs, hub methods or push names change in a way an older client cannot handle.
    /// Do NOT bump it for anything else: a UI-only client release and a server-side balance fix both
    /// leave the wire untouched, so both sides keep talking with no coordination at all.
    ///
    /// Why this and not the build label: <see cref="GameVersion"/> moves on every commit, which made
    /// the handshake a LOCKSTEP — a server rebuilt for a server-side fix refused a client that was
    /// byte-identical on the wire. The workaround was a hand-written list of blessed build labels, and
    /// that list could only ever say "this old client is fine". It had no way to express the case that
    /// actually happens most: **client-only work, where the CLIENT is ahead of the server.** A version
    /// number that describes the contract instead of the build makes that case a non-event.
    /// </summary>
    public const int ProtocolVersion = 2;

    /// <summary>
    /// The oldest protocol this server still speaks. Equal to <see cref="ProtocolVersion"/> means
    /// "current only"; setting it lower is a deliberate promise to keep handling the older shape, so
    /// it should only move when someone has actually checked that the code still does.
    /// </summary>
    public const int MinAcceptedProtocol = 2;

    /// <summary>
    /// Build labels accepted from clients too old to send a protocol number. LEGACY — frozen.
    ///
    /// Every client from 0.28.25 on sends <see cref="ProtocolVersion"/>, and is judged on that. This
    /// list exists only so the APKs built before that change keep working; nothing should be added to
    /// it. Delete it once no old build is installed anywhere.
    /// </summary>
    public static readonly string[] LegacyCompatibleClientVersions =
    {
        "0.28.13", "0.28.14", "0.28.15", "0.28.16", "0.28.17", "0.28.18",
        "0.28.19", "0.28.20", "0.28.21", "0.28.22", "0.28.23", "0.28.24",
    };

    /// <summary>
    /// Can this client talk to this server? Returns null when yes, or the reason to show when no.
    ///
    /// <paramref name="clientProtocol"/> 0 means the client never sent one — a pre-0.28.25 build, or a
    /// dev tool — and falls back to the legacy build-label list, which is what let this ship without a
    /// flag day.
    ///
    /// ⚠ The protocol is carried INSIDE <see cref="AuthRequest"/>, not as an extra hub parameter.
    /// SignalR does NOT bind by arity: the dispatcher requires the argument count to match, and a
    /// default value on a hub parameter does not make an omitted argument legal. Trying it that way
    /// broke every reconnect from the previous build. A DTO field degrades gracefully (missing → 0);
    /// a hub signature does not.
    /// </summary>
    public static string? ClientRejectionReason(string? clientVersion, int clientProtocol)
    {
        if (clientProtocol > 0)
            return clientProtocol >= MinAcceptedProtocol && clientProtocol <= ProtocolVersion
                ? null
                : clientProtocol > ProtocolVersion
                    ? $"This client (protocol {clientProtocol}) is NEWER than the server "
                      + $"(protocol {ProtocolVersion}). Update the server."
                    : $"Client too old (protocol {clientProtocol}; this server needs "
                      + $"{MinAcceptedProtocol}). Please update to v{GameVersion}.";

        // No protocol number: an empty version is local tooling and always allowed.
        if (string.IsNullOrEmpty(clientVersion)) return null;
        if (clientVersion == GameVersion) return null;
        foreach (var accepted in LegacyCompatibleClientVersions)
            if (accepted == clientVersion) return null;

        return $"Client out of date (v{clientVersion}). Please update to v{GameVersion}.";
    }

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

    /// <summary>World size. Design target is 75000x75000; enlarged to 48000 so the
    /// level 1-80 zone ring (see WorldMap) is spread out (towns far apart, zones not
    /// clustered) with room for future dungeons/bosses. The starter town sits at the
    /// centre (24000,24000). All WorldMap coordinates were scaled ×2 to match.</summary>
    public const float ZoneWidth = 48000f;
    public const float ZoneHeight = 48000f;

    /// <summary>Base/maximum player movement speed in units per second.</summary>
    public const float BasePlayerSpeed = 250f;

    public const int MaxCharacterNameLength = 16;

    /// <summary>Character slots per account — enough for every race/class/discipline
    /// combination so a player needn't make extra accounts.</summary>
    public const int MaxCharactersPerAccount = 36;

    /// <summary>How long a character deletion is held (a cancellable "pending delete")
    /// before it becomes permanent. Higher-level characters get a longer grace period.
    /// Below the class-change level it's instant.
    ///
    /// DEBUG builds collapse the whole ladder to <see cref="DebugCharacterDeleteSeconds"/>: while testing,
    /// BOTH ends of the live rule get in the way — under level 20 a delete is INSTANT, so a misclick is
    /// unrecoverable; at 20+ the character (and its NAME) is locked away for 24h-30d, so you cannot re-make
    /// the character you just deleted. A few seconds gives an undo window AND frees the name straight after
    /// (owner, 2026-07-17). The live ladder is unchanged for real builds.</summary>
    public static TimeSpan CharacterDeleteDelay(int level) =>
#if DEBUG
        TimeSpan.FromSeconds(DebugCharacterDeleteSeconds);
#else
        level >= 76 ? TimeSpan.FromDays(30)
        : level >= 40 ? TimeSpan.FromDays(7)
        : level >= 20 ? TimeSpan.FromHours(24)
        : TimeSpan.Zero;
#endif

    /// <summary>DEBUG-only pending-delete window (see <see cref="CharacterDeleteDelay"/>). Long enough to
    /// undo a misclick, short enough that the name is reusable moments later. The purge itself runs when
    /// character-select next lists the account (PersistenceService.ListCharactersAsync), so the character
    /// is really gone — and its name free — on the next refresh after this elapses.</summary>
    public const int DebugCharacterDeleteSeconds = 10;

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

    /// <summary>The once-per-SECOND housekeeping cadence: damage-over-time, heal-over-time, the buff
    /// push and the party-roster refresh. These are authored "per second" and must stay at 1s no matter
    /// how the regen cadence is tuned — they used to share the regen flag, so retuning regen would
    /// silently have nerfed every DoT by the same factor.</summary>
    public const int SecondIntervalTicks = TickRate;

    /// <summary>Out-of-combat natural regen cadence, in ticks. Default 30 = **3 seconds**, matching
    /// L2's `HP_REGENERATE_PERIOD = 3000`. NOT a const: it's live-editable from the admin Debug Tuning
    /// panel so the cadence can be compared in-game. Changing it does NOT change how fast you heal —
    /// <see cref="RegenIntervalSeconds"/> scales the amount — only how chunky the healing is.</summary>
    public static int RegenIntervalTicks = 30;

    /// <summary>The regen period in seconds, i.e. how much "per second" regen each tick pays out.</summary>
    public static float RegenIntervalSeconds => RegenIntervalTicks * TickSeconds;

    /// <summary>Maximum stacks a damage-over-time effect can build (bleed/poison/venom).</summary>
    public const int MaxDotStacks = 10;

    /// <summary>Death exp penalty (5% of the level) applies only AT OR ABOVE this level — low-level
    /// "newbie protection", so a low-level death costs nothing (and a res scroll has nothing to restore).
    /// (Later: a noblesse-style passive also waives the loss on boss/instance deaths.)</summary>
    public const int DeathExpPenaltyMinLevel = 40;

    // ----- Chat ---------------------------------------------------------------------

    /// <summary>Client keeps at most this many lines per chat tab.</summary>
    public const int ChatHistoryLimit = 150;

    // ----- Items / progression / trade (Phase 4) -------------------------------

    // Slot cap counts UNEQUIPPED items only — worn gear doesn't occupy a bag slot (owner). 250 for now
    // (30 was far too low once every gear piece and material stacked up).
    public const int InventorySize = 250;

    /// <summary>Skill-bar slots — 5 rows of 12. The bar is ONE FLAT collection of ids; "rows" are purely a
    /// client visualization (it slices this list into chunks of <see cref="SkillBarColumns"/>). Shared,
    /// because the SERVER owns the bar — see SyncSkillBar. Old saved bars are shorter and just pad with
    /// empties on load.
    ///
    /// The server no longer AUTO-PLACES newly-learned skills (owner, 2026-07-20): it only validates.
    /// See SyncSkillBar for why.</summary>
    public const int SkillBarSlots = 60;

    /// <summary>Slots per visual row (the client draws up to 5 rows of this).</summary>
    public const int SkillBarColumns = 12;

    /// <summary>A bar slot may hold an INVENTORY ITEM instead of a skill: the entry is
    /// "item:&lt;defId&gt;". Clicking it USES the item (like a potion), and the slot greys out when you
    /// have none — exactly like a skill on cooldown. SyncSkillBar must NOT treat these as unknown skills
    /// and wipe them.</summary>
    public const string SkillBarItemPrefix = "item:";
    public static bool IsItemSlot(string? id) => id is not null && id.StartsWith(SkillBarItemPrefix, StringComparison.Ordinal);
    public static string ItemSlotToken(string defId) => SkillBarItemPrefix + defId;

    /// <summary>Equipment-preset bar token: "preset:0/1/2" (A/B/C). Tapping it applies that saved
    /// loadout. Like item:/action: tokens, it just rides the existing skill bar.</summary>
    public const string SkillBarPresetPrefix = "preset:";
    public static bool IsPresetSlot(string? id) => id is not null && id.StartsWith(SkillBarPresetPrefix, StringComparison.Ordinal);
    public static string PresetSlotToken(int slot) => SkillBarPresetPrefix + slot;
    public static string ItemSlotDefId(string token) => token.Substring(SkillBarItemPrefix.Length);

    /// <summary>A bar slot may also hold a built-in ACTION: "action:&lt;id&gt;". These are not skills —
    /// they cost nothing, have no cooldown and are never learned — but they belong on the bar because
    /// they are the two things you press constantly. They are the ONLY entries a new character starts
    /// with (owner, 2026-07-20). Like item slots, SyncSkillBar must not treat them as unknown skills.</summary>
    public const string SkillBarActionPrefix = "action:";
    public static bool IsActionSlot(string? id) => id is not null && id.StartsWith(SkillBarActionPrefix, StringComparison.Ordinal);
    public static string ActionSlotToken(string actionId) => SkillBarActionPrefix + actionId;
    public static string ActionSlotId(string token) => token.Substring(SkillBarActionPrefix.Length);

    // Action ids. The catalog that describes them (name, icon, what they need) is ActionCatalog.
    public const string ActionBasicAttack   = "basic_attack";
    public const string ActionTargetClosest = "target_closest";
    public const string ActionSitStand      = "sit_stand";
    public const string ActionRunWalk       = "run_walk";
    public const string ActionTradeTarget   = "trade_target";
    public const string ActionPartyInvite   = "party_invite";
    public const string ActionFollowTarget  = "follow_target";
    public const string ActionAssistTarget  = "assist_target";

    // A new character starts with a COMPLETELY EMPTY bar (owner, 2026-07-20). Nothing is placed for
    // you — not skills, not even the actions. The player builds their own bar from the skills window's
    // Skills and Actions tabs.

    // ----- Target-closest search radius (client preference) -----------------------

    /// <summary>Default radius for the "target closest" action.</summary>
    public const float TargetSearchRangeDefault = 1000f;
    public const float TargetSearchRangeMin = 400f;
    public const float TargetSearchRangeMax = 1500f;

    public const int ClassChangeLevel = 20;

    /// <summary>Max classes ONE character may own (L2-style: the main class + up to 3 subclasses).
    /// Stops a character stacking pointless duplicate base classes when only a few can reach a unique
    /// 3rd-class discipline. The player-facing swap rules (safe-zone-only, 5-min delay) are separate.</summary>
    public const int MaxSubclasses = 4;

    /// <summary>Party EXP share band: a member more than this many levels from the KILLER earns nothing
    /// from that kill (owner, 2026-07-17). Anti-powerlevelling — the level-weighting alone still let a
    /// level-1 ride a level-80's kills for a trickle. Outside the band you get a flat zero, and the
    /// in-band members split the whole reward between them. Kill-QUEST credit ignores this (range only).</summary>
    public const int PartyExpMaxLevelGap = 9;

    /// <summary>Level ceiling for a normal character. ADMINS ARE EXEMPT — an admin can push past it,
    /// which is the point (testing the 85+ mob band, the top gear tier, etc. without capping the game
    /// for everyone else). Everything is authored to 85 today (mob curve, gear tiers, the nuke
    /// ladder), so 90 is deliberate headroom rather than a content boundary.</summary>
    public const int MaxPlayerLevel = 90;

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

    /// <summary>How far a jailed player may wander from the jail centre. Serving a sentence should feel
    /// like a CELL, not paralysis — they can walk around inside it; everything else (chat, skills, items,
    /// escape) stays blocked.</summary>
    public const float JailRadius = 260f;

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
