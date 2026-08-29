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
    public const string GameVersion = "0.101.5";

    // ----- SP BOTTLE (owner, 2026-08-26) -------------------------------------------------------
    // *"u can make an npc to take your 1kkk SP + 100kk gold and give you a tradable/sellabel
    //  (100kk shop-buy price) SP bottle"*. Three numbers, one place.
    //
    // ✅ BOTH 2026-08-26 FLAGS ARE CLOSED (same day, by him).
    //
    // 1. The CSV header that read "1kkk SP + 100k Gold" WAS the typo — *"csv is a typeo"*. It says
    //    100kk now, and spells out the either/or below.
    // 2. 🔑 `Entity.SkillPoints` STAYS A 32-BIT INT, and his level-85 five-bottle row is fine, because
    //    bottles are SPENT AS A CURRENCY, not drunk toward the total: *"Skills cost X bottles as
    //    consumed-ID (u cannot have 5kkk SP int limit to 2.147kkk)"*. That is `SkillDef
    //    .LearnConsumableId`, charged in HandleLearnSkill. **Do not widen SkillPoints to a long** —
    //    the ceiling is the design, and the item price is what routes around it.
    //
    // 🔑 IT SELLS FOR WHAT IT COST, 100kk — not the /25 consumable rule's 4kk. The broker takes 1kkk
    // SP **and** 100kk gold; the bottle gives back exactly ONE of the two, and which one is the
    // player's choice: *"drinking return 1kkk sp and selling return 100kk gold .. u cannot do both"*.
    // At a 96% sell loss that choice did not exist. Not a faucet — no vendor stocks the bottle.
    public const int SpBottleSpCost = 1_000_000_000;    // what the broker takes: 1kkk SP
    public const int SpBottleGoldCost = 100_000_000;    // ...and 100kk gold
    public const int SpBottleShopPrice = 100_000_000;   // its BUY price — and its SELL price, see above
    public const int SpBottleSpGranted = 1_000_000_000; // what drinking one gives back (no gold)

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
    /// 17 → 18 (2026-08-12, `BL-37`): `DebugConfigDto` lost its `TestHealPower` field with the test heal
    /// itself. It is a POSITIONAL record, so every field after it shifted — an old client sending the
    /// 20-field order would write its heal power into `TestSkillPower`. Admin-only, and
    /// `MinAcceptedProtocol` is 8 so such a client is still let in, which is precisely why the number has
    /// to move: the handshake is the only place that difference is written down.
    /// 18 → 19 (2026-08-13, `BL-05`): `CraftingUpdate` gained `Level`/`Exp`/`BandCap`/`AtMaster`, and
    /// two new hub methods (`JoinProfession`, `QuitProfession`) replaced the self-pick `ChooseProfession`.
    /// The DTO fields are pure ADDITIONS with defaults, so an old client just draws no crafting level —
    /// but it would also still be calling `ChooseProfession`, which now refuses, so the number moves.
    /// 19 → 20 (2026-08-14, `BL-22`): one new hub method, `DisassembleItem`. Nothing else on the wire
    /// changed — no DTO gained or lost a field — so an old client is functionally fine and simply has
    /// no Break-down button. The number still moves, because the handshake is the only place a client
    /// that CALLS a method the server does not have gets caught, and this is the direction that breaks:
    /// a NEW client against an OLD server would throw on the send, not degrade.
    /// 20 → 21 (2026-08-15, playtest 23): three DTO additions, all with defaults. `EntityDto` and
    /// `TargetDetails` gained the mob BEHAVIOUR fields (`SocialClan`, and `Aggressive` on the details
    /// sheet), and `ResurrectOffer` gained `SelfRes` so the preservation prompt can word itself. All are
    /// trailing optional parameters, so an old client deserialises them as defaults and merely shows
    /// less — but the number moves anyway, because the whole point of a contract version is that "it
    /// happens to still work" is not something either side should have to work out at runtime.
    /// 21 → 22 (2026-08-22, playtest 26): the ground decals gave the world a second channel. Two new
    /// server→client pushes — `"Totems"` (the WHOLE `TotemList` a viewer can see, resent when the set
    /// changes) and `"AreaEffect"` (a one-shot `AreaEffectEvent` flash) — plus the `TotemDto` they
    /// carry. An old client subscribes to neither and simply draws no circles. The direction that
    /// actually matters is the reverse one, the same case as 16: a NEW client on an OLD server would
    /// draw a scene whose totems are never sent, showing empty ground where the healing really is, and
    /// the handshake is the only place that pair gets caught.
    /// 22 → 23 (2026-08-22, playtest 26): <see cref="AccountRole"/> gained two ranks and its numbers
    /// MOVED — ChatModerator was inserted at 1, so Moderator went 1 → 2 and Admin 2 → 3, with Owner at 4.
    /// It rides on `AuthResponse` and `AdminStateDto` (renamed `SelfStateDto` at 25) as a plain int, so
    /// this is the rare bump that is not an addition but a REDEFINITION: an old client on this server
    /// reads a Moderator as a Chat Moderator
    /// and an Admin as a Moderator, and hides the admin toolbox from a real admin. Nothing crashes, which
    /// is exactly why it has to be caught by the handshake rather than noticed later.
    /// 23 → 24 (2026-08-23, playtest 27): <see cref="BuffDto"/> gained a `Level`, so the effects bar can
    /// finally say WHICH rung it is showing (*"The title just says Aim no lvl"*). A pure addition — an old
    /// client would simply not read it — but the same version carries three CLIENT-side rules that a
    /// server cannot enforce alone and that would look like bugs if the halves disagreed: `_ . -` are
    /// legal in names now, `~` and `%target` stopped being target tokens (`~` is the relative-coordinate
    /// prefix for `/tp`) while ``/`` started being one, and a non-admin may send a bare `/where`.
    /// Pairing an old client with this server would silently mean the wrong name rule and a dead ``.
    /// 24 → 25 (2026-08-23, `BL-82`): `AdminStateDto` became <see cref="SelfStateDto"/> — renamed, three
    /// fields richer (`Invisible`, `Hidden`, `Stealthed`) and pushed on the hub message `"SelfState"`
    /// instead of `"AdminState"`. A RENAME, so this is not a bump an old client survives by reading
    /// less: it subscribes to a message name the server no longer sends, and the god-mode badge and
    /// the stealth fade are simply absent — which is the same "you cannot see that you are in god
    /// mode" hole this version exists to close. It is also the direction that matters least in the
    /// reverse: a new client on an old server subscribes to a message that never arrives and shows
    /// nothing, silently. The handshake is the only place either pairing is caught.
    /// 25 → 26 (2026-08-24): the three DUNGEONS were re-shaped into a corridor with side rooms
    /// (<see cref="DungeonLayout"/>), and this is the first bump where NOT ONE BYTE OF THE WIRE MOVED.
    /// No DTO, no hub method, no push name. It moves because the dungeon's WALL is shared code, not a
    /// message: <see cref="WorldDomain"/> lives in Game.Shared precisely so the client can stop you at
    /// the surface while the server keeps its clamp as the anti-cheat backstop — *"two halves enforcing
    /// the same rule is only safe if they cannot disagree"*. An old APK holds the OLD polygon, so inside
    /// a dungeon the two halves clamp to different shapes: the client would refuse to walk into rooms
    /// that now exist, rubber-band along walls that no longer do, and draw the old outline on the map.
    /// Nothing crashes — which is exactly why the handshake has to catch it. Same reasoning as 23 → 24,
    /// where the client-side rules, not the DTO, were what forced the number.
    /// 26 → 27 (2026-08-26): the FOURTH-CLASS KIT. Second bump running where not a byte of the wire
    /// moved, and for the same reason as 25 → 26: the thing that changed is SHARED CODE both halves
    /// compile. <see cref="ClassSkills.ClassKey"/> grew a TIER and `Cumulative` a `fourth` flag, and the
    /// client builds its Learn tab LOCALLY from the compiled tables rather than from a server push —
    /// so an old APK against this server shows an ascended Lightbringer an EMPTY 76-90 ladder, no
    /// Sigils tab at all, and eighteen skills it has never heard of arriving in its Learned map. It
    /// would not crash; it would simply be blind to the whole feature while the server happily sold it.
    /// That is precisely the case the handshake exists to catch. ⚠ A NEW APK IS REQUIRED.
    /// 27 → 28 (2026-08-26): the MP-REGEN MODEL (`BL-92`). Third bump running with no wire change, and
    /// this one for BOTH of the reasons above at once. (1) A new class-table entry — Calm Spirit's six
    /// rungs on the nuker — and the Learn tab is built locally, so an old APK shows a Magus a ladder
    /// with a hole in it while the server sells the rungs. (2) Every regen NUMBER moved: the stance
    /// multipliers, the SPT curve, and the mastery ladder going from ×1.5…×3.4 to +1.5…+3.4. Those live
    /// in shared code the client compiles for its own tooltips and stat window, so an old client would
    /// quote the OLD regen at a player the server is paying the new one to. ⚠ A NEW APK IS REQUIRED.
    /// 28 → 29 (2026-08-27): `BL-93` — two fields added to the spawn `EntityDto` (`Category`, `Role`),
    /// so the client can tell a wolf from a warrior and choose a MODEL for it. A real wire change this
    /// time, not a table-only bump: an old client deserializing the new spawn shape simply ignores
    /// them, but the new client cannot work without them, so the two move together.
    /// ⚠ A NEW APK IS REQUIRED.
    public const int ProtocolVersion = 29;

    /// <summary>
    /// The oldest protocol this server still speaks. Equal to <see cref="ProtocolVersion"/> means
    /// "current only"; setting it lower is a deliberate promise to keep handling the older shape, so
    /// it should only move when someone has actually checked that the code still does.
    ///
    /// Held at 8 through the 9 (0.43.0, a field added to QuestLog), 10 (0.44.0, a Title field added
    /// to EntityDto plus a new SetTitle hub method) and 11 bumps. The first two are pure ADDITIONS an
    /// older client does not read and never calls. 11 (0.45.0) DOES change a hub signature —
    /// RerollAttributes dropped its lockedIndices argument — but no shipped client has ever called it:
    /// attribute scrolls had no phone UI at all until this build, which is exactly why the system was
    /// rebuilt. So an installed 0.42.x-0.44.x APK still plays against this server; it simply shows no
    /// titles, no chat tabs and no scroll windows.
    ///
    /// 13 (0.55.0) adds EntityDto.TitleColor and two hub methods (SetCustomTitle / SetTitleColor) for
    /// player-written titles. Pure additions again: an older client ignores the colour and draws every
    /// title in its own default gold, and never calls what it does not know about. NPCs will read as a
    /// bare "Marius" on it, since the role now travels on the title field it does not know to draw.
    ///
    /// 14 (0.56.0) adds ChatChannel.Combat — loot and the per-kill reward line moved off System onto
    /// their own channel (D5). An older client has no case for it and falls through to its Local
    /// default, which prints the line uncoloured on the Local tab: noisier than before, never lost.
    ///
    /// 16 (0.59.0) adds the "Crafting" push (<see cref="CraftingUpdate"/>) that carries the character's
    /// profession and unlocked blueprints. It is what the crafting WINDOW is filled from, and the
    /// direction of the risk is the reverse of the usual one: an old client simply has no window, but a
    /// NEW client on an OLD server would open a crafting window that is never told the profession and
    /// would therefore offer nothing while the server happily crafts. Bumped so that pair refuses
    /// instead — the "client is NEWER than the server" branch above is exactly this case.
    /// </summary>
    public const int MinAcceptedProtocol = 8;

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

    /// <summary>How many COUNTED buffs one entity may carry. **20** since playtest 27, down from 24:
    /// *"Now I have 24 buffs as healer ... So if we make it 20 then the buffer becomes a must"*, and he
    /// was right for a reason worth writing down. A Warchanter-buffed character spends **13** of these
    /// (nine improved groups + four harmonies) and has seven to spare; the same character buffing
    /// himself off the NPC buffer spends **19**, for a strictly weaker set — because a GROUP packs
    /// three or four families into one slot and a single never can. So the cap does not limit the
    /// buffer, it limits the ALTERNATIVE to the buffer. To make it bite harder, lower this number;
    /// do not touch the flag. Measure it with `dotnet run --project tools/BalanceMatrix -- --buffs`.
    ///
    /// WHAT COUNTS is per-buff and authored: `SkillDef.CountsTowardBuffLimit`, default true, false on
    /// the temporary ones. Toggles, debuffs and the gear/rune row are excluded by the engine — see
    /// GameLoopService.CountsAgainstBuffCap for why each.
    ///
    /// Over the cap the OLDEST buff is dropped and the new one lands, FIFO, *"if the 1st buff still
    /// have 2h time remaining I still can overbuff and remove it"*. It is never the other way round: a
    /// refusal arrives mid-fight and sends you hunting through the bar for something to cancel, which
    /// is the exact moment you cannot afford to be reading icons.</summary>
    public const int MaxBuffSlots = 20;

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

    /// <summary>The world's NEGATIVE-coordinate floor. The overworld lives in [0, Zone*]; the negative
    /// quadrant is reserved for DUNGEONS and JAIL (owner) — special areas you reach by teleport, kept out
    /// of the positive overworld. Position clamps use [WorldMin*, Zone*], and the cell grid (a sparse
    /// dictionary) buckets negative coordinates into their own negative cells. It's all data — widen this
    /// as more instanced content is added.</summary>
    public const float WorldMinX = -48000f;
    public const float WorldMinY = -48000f;

    /// <summary>Base/maximum player movement speed in units per second.</summary>
    public const float BasePlayerSpeed = 250f;

    public const int MaxCharacterNameLength = 16;

    /// <summary>Shortest character name. THREE, not one — his playtest-26 find was that a name made of
    /// nothing at all got through, and a 1-character name is the same problem one step along: it cannot
    /// be typed at, reported, or told apart on a plate.</summary>
    public const int MinCharacterNameLength = 3;

    /// <summary>Is this a legal character name? ONE rule, shared by the server's create path and (once
    /// the client is rebuilt) the create screen, so the two can never disagree about what is legal.
    ///
    /// 🔑 The rule is deliberately narrow, and it answers all three of his playtest-26 questions at once
    /// (*"should we disable the cirulyc … Same question for space in the name or allowed symbol"*):
    ///   • **ASCII letters and digits only.** No Cyrillic, no accents, no emoji. A name has to be
    ///     TYPEABLE by every other player in the world — `/whisper`, `/ptinv`, `/jail` and the friend
    ///     list are all name-addressed, and a name nobody else's keyboard can produce is a name nobody
    ///     can whisper, invite, report or moderate.
    ///   • **No spaces.** Every name-taking command in the game parses `name` as the first token or
    ///     splits on the last space (`/role <name> <role>`, `/jail <name> [min]`), so a space in a name
    ///     breaks the parser, not just the eye.
    ///   • **Three symbols ARE allowed: `_` `.` `-`** (owner, playtest 27: *"symbols like _ . - I see no
    ///     reason why cannot be included. Players should be able to separate `Name_.-Family`"*). None of
    ///     the three is a token separator, none needs a keyboard layout nobody has, and none can be
    ///     confused with nothing at all — which is what the rule is actually for. Consecutive ones are
    ///     legal on purpose: his own example is `Name_.-Family`.
    ///   • **Must START with a letter**, and cannot be all digits — an all-digit name collides with
    ///     every command that takes a number in the same slot.
    ///
    /// ⚠ What it does NOT try to fix is `IlIlllIIllI` — visually confusable names are a real problem and
    /// no charset rule solves them (that is what the `@target` token is for). This rule is about names
    /// that cannot be ADDRESSED at all.
    ///
    /// ⚠ It also closes the exact hole he found: a name of one or more invisible characters. `Trim()`
    /// alone did not, because U+200B ZERO WIDTH SPACE is not whitespace to .NET — two such names are
    /// both non-empty, both distinct, and both render as nothing.</summary>
    public static bool IsValidCharacterName(string? name, out string error)
    {
        string n = (name ?? "").Trim();
        if (n.Length < MinCharacterNameLength || n.Length > MaxCharacterNameLength)
        {
            error = $"Name must be {MinCharacterNameLength}-{MaxCharacterNameLength} characters.";
            return false;
        }
        foreach (char c in n)
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                  || c == '_' || c == '.' || c == '-'))
            {
                error = "Name may use English letters, digits and _ . - only — no spaces or other symbols.";
                return false;
            }
        if (!char.IsLetter(n[0]))
        {
            error = "Name must start with a letter.";
            return false;
        }
        error = "";
        return true;
    }

    /// <summary>Character slots per account — enough for every race/class/discipline
    /// combination so a player needn't make extra accounts.</summary>
    public const int MaxCharactersPerAccount = 36;

    /// <summary>How long a character deletion is held (a cancellable "pending delete")
    /// before it becomes permanent. Higher-level characters get a longer grace period.
    /// Below the class-change level it's instant.
    ///
    /// ADMIN characters — and DEBUG builds — collapse the whole ladder to
    /// <see cref="DebugCharacterDeleteSeconds"/>: while testing, BOTH ends of the live rule get in the way —
    /// under level 20 a delete is INSTANT, so a misclick is unrecoverable; at 20+ the character (and its
    /// NAME) is locked away for 24h-30d, so you cannot re-make the character you just deleted. A few
    /// seconds gives an undo window AND frees the name straight after (owner, 2026-07-17).
    ///
    /// The <paramref name="admin"/> door exists for the same reason the debug menu became an admin menu
    /// (0.33.1): a `#if DEBUG` convenience is no convenience at all on the RELEASE server running on the
    /// owner's phone, which is where the testing actually happens. Ordinary players keep the live
    /// ladder.</summary>
    public static TimeSpan CharacterDeleteDelay(int level, bool admin = false) =>
#if DEBUG
        TimeSpan.FromSeconds(DebugCharacterDeleteSeconds);
#else
        admin ? TimeSpan.FromSeconds(DebugCharacterDeleteSeconds)
        : level >= 76 ? TimeSpan.FromDays(30)
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

    /// <summary>Regen multiplier while resting in a CITY. Was 5 until playtest 27, when a sitting
    /// healer measured 220 MP/s and the sit stack was doing the damage: town x5 * sitting x1.8 = x9
    /// on top of every regen buff, which made downtime free and a mana pool decorative. Owner:
    /// *"Hp/mp regen in cities should be decreased to x2 and only in the big cities"*. At x2 the same
    /// sitting healer runs x3.6 — still the obvious place to rest, no longer a second mana bar.
    ///
    /// Paid ONLY where <see cref="SafeZone.RegenBoost"/> is set: the five cities, never the training
    /// outpost or a dungeon entrance. Read it through <see cref="SafeZoneRegen"/>, never by testing
    /// <see cref="InSafeZone"/> yourself — safety and rest are two different questions now.</summary>
    public const int SafeZoneRegenMultiplier = 2;

    /// <summary>How full a resurrected player stands up, as a fraction of max HP and MP, when the
    /// skill that raised them does not say otherwise. Every res before the 4th tier used this number
    /// and the scroll still does; his `healer 4th.csv` is the first to override it (35% at 76, 40% at
    /// 80) — see <c>SkillDef.ResHpPct</c>.</summary>
    public const float DefaultResurrectHpPct = 0.30f;

    /// <summary>The regen multiplier a point is worth: the city bonus inside a resting safe zone,
    /// 1 everywhere else (open world, outposts, dungeon entrances).</summary>
    public static float SafeZoneRegen(float x, float y) =>
        WorldMap.SafeZoneAt(x, y) is { RegenBoost: true } ? SafeZoneRegenMultiplier : 1f;

    // NOTE: there is deliberately NO combat regen multiplier (owner, 2026-07-29). Regen is modified by
    // STANCE only — MovementTuning.RegenMultiplier: sitting ×1.8, walking ×1.2, running ×1.0 — plus the
    // safe zone, SPT/CON and buffs. See Regenerate() for why the old Engaged/casting suppression went.

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

    // ----- Threat / aggro (BL-71) -------------------------------------------------------
    //
    // Threat IS damage: 1 point per 1 damage dealt (GameLoopService.ApplyDamage). Everything
    // below is expressed against that one scale, so every number here reads as "worth this
    // much damage" — which is the only way a taunt, a heal and a pull can be compared at all.

    /// <summary>Per-second multiplier applied to every entry in an engaged mob's threat table.
    /// Proportional, so it never re-orders the table on its own — what it does is shrink the
    /// ABSOLUTE gaps, which is what makes a taunt's flat cushion something you have to renew
    /// rather than something you buy once at the pull. 0.99 halves a lead in ~69s.</summary>
    public const float ThreatDecayPerSecond = 0.99f;

    /// <summary>Threat entries below this are pruned — decay's floor, so a table doesn't grow
    /// a long tail of entities that once threw a debuff and left.</summary>
    public const float ThreatFloor = 1f;

    /// <summary>What a mob that walked to YOU owes you, as a fraction of its own max HP.
    ///
    /// Without this a proximity pull seeded NO threat at all, so the mob arrived with an empty
    /// table and the first point of damage from anyone — including someone who never pulled it —
    /// instantly owned it. Expressed as a fraction of the mob's HP rather than a flat number
    /// because threat is damage: 5% of its bar means "another player must out-damage the puller
    /// by 5% of this creature to take it", which reads the same at level 20 and at level 85.</summary>
    public const float ThreatAggroPullFraction = 0.05f;

    /// <summary>Support threat conversion (owner, playtest-22): a heal generates
    /// <c>healPower / castSeconds × 10</c> threat, <b>per person healed</b> (the × people is his
    /// 2026-08-14 correction — see <see cref="ThreatBuffPerLevel"/>, which a heal now mirrors).
    /// His worked examples: 300 power over 2s = 1500 on one ally; a 1500-power party heal on a 10s
    /// cast is 150/s, so 13,500 across a full party of 9. The division is what stops a big slow heal
    /// out-threatening a spammed small one.</summary>
    public const float ThreatHealFactor = 10f;

    /// <summary>Buff threat per LEVEL per person affected (owner, 2026-08-14). A buff has no power to
    /// read, so its worth is its LEVEL and its REACH: <c>grantLevel × 20 × peopleAffected</c>.
    ///
    /// His worked example, and it lands exactly on shipped data: a group buff learned at 70 is
    /// 70 × 20 = 1400 a head, and across a full party of 7 that is <b>9,800</b> — against a quick heal
    /// of 1500 power over 2s, which is 7,500. So a self-buff or a single-target buff is worth well
    /// under one heal, and blanketing a party is worth rather more than one. That asymmetry IS the
    /// rule: *"if it affect only the caster or a single target won't be as much as a value but a
    /// whole party ..."*
    ///
    /// 🔑 It is the level the buff was LEARNED at, not the caster's — *"If I learn a buff at 50 and
    /// another at 70 the 50 one should have less aggro value."* The caster's own level is only the
    /// fallback for a buff no class list owns (a scroll).</summary>
    public const float ThreatBuffPerLevel = 20f;

    /// <summary>Floor on the cast time in that formula. An instant heal would otherwise divide by
    /// zero (or, at one tick, by 0.1 — a ×100 blow-up). One second is the shortest cast the
    /// formula is allowed to see.</summary>
    public const float ThreatMinCastSeconds = 1f;

    /// <summary>How far a wounded mob's cry for help carries to its social clan (BL-70). His figure
    /// was "400-500"; 450 is the middle of it.
    ///
    /// Note this is LARGER than <see cref="MobAggroRange"/> (400) on purpose: a camp that answers only
    /// as far as it can already see you is not a camp, it is four independent mobs.</summary>
    public const float MobClanCallRadius = 450f;

    /// <summary>How long you play before the client shows the "take a break" banner.
    ///
    /// ✅ BACK TO 3 HOURS on 2026-08-24. It sat at 10 MINUTES from playtest 24 to playtest 28 — his own
    /// request (*"change it to 10mins. (tag it to return to default 3h after test)"*) because checklist
    /// row `13a` had gone untested for six passes for the obvious reason. He has now seen it:
    /// *"Working - Can return it to 3h"*, so the tag is discharged and the default is restored.</summary>
    public const long BreakReminderSeconds = 3 * 3600;

    /// <summary>The ordinary field respawn cadence, and the yardstick a mob's SCARCITY is measured
    /// against for EXP (`BL-49`, playtest 23). 22s is not invented — it is the number every ordinary
    /// spawn zone in <see cref="WorldMap"/> is authored with, which is what makes normal trash come out
    /// at exactly ×1.00 and leaves plain levelling untouched by the whole mechanism.</summary>
    public const float BaselineRespawnSeconds = 22f;

    /// <summary>What SHARE of a long respawn a rare creature is paid for. See
    /// <c>GameLoopService.RespawnScarcity</c> — 1.0 would pay the wait in full, which assumes you stand
    /// at the corpse doing nothing; 0.25 is tuned to land a level-90 field boss on his stated *"at least
    /// 20kk"*. ⚠ This is the one invented number in the boss-EXP rule and the only knob to turn.</summary>
    public const float RespawnScarcityExponent = 0.25f;

    /// <summary>🔴 THE MOB SOCIAL-CLAN MASTER SWITCH, and it is OFF — his instruction, playtest 23
    /// (2026-08-15): *"Now we can remove all mobs social clan (leave the system ..we will use it just
    /// not now) only mobs not to be social for now ... Make a note to turn it on once the world map is
    /// in place."* The note is `BL-73`.
    ///
    /// <para>🔑 What he saw is NOT this feature misbehaving — it is spawn DENSITY meeting it: *"all mobs
    /// are spawning almost next to each other and hitting one wolf getting ganked by 10 other … For a
    /// mage lvl 9 hitting a warefolf means dead."* Every camp is currently generated on nearly one point,
    /// so a 450 radius reaches the whole camp at once. The shape he wants is *"it will call ONE, and
    /// while you fight, if others wander in the social range they will aggro"* — which is what this same
    /// radius already does once a camp occupies real ground. So the retune that eventually follows is the
    /// SPACING, not this number.</para>
    ///
    /// <para>⚠ Deliberately a switch and not a data deletion: the twelve clans stay authored on the mobs
    /// in <see cref="MobCatalog"/> and every line of <c>CryForHelp</c> stays live, so turning this back
    /// to <c>true</c> is the whole of re-enabling it. Do not "clean up" the clan column while this is
    /// off — re-authoring it later from memory is exactly the work this avoids.</para></summary>
    /// <remarks><c>static readonly</c> and not <c>const</c> on purpose: a const <c>false</c> makes the
    /// whole of <c>CryForHelp</c> compile-time unreachable and the build starts warning about the code
    /// we are deliberately keeping alive.</remarks>
    public static readonly bool MobClansEnabled = false;

    /// <summary>Default hard-commit window after a taunt lands, when the skill doesn't author its
    /// own (DurationTicks). During it the mob will not retarget away from the taunter even if
    /// someone out-threats them — the taunt's guarantee, distinct from its POWER, which is what
    /// decides whether it still holds afterwards.</summary>
    public const int TauntLockTicksDefault = 30;   // ~3s

    /// <summary>The once-per-SECOND housekeeping cadence: damage-over-time, heal-over-time, the buff
    /// push and the party-roster refresh. These are authored "per second" and must stay at 1s no matter
    /// how the regen cadence is tuned — they used to share the regen flag, so retuning regen would
    /// silently have nerfed every DoT by the same factor.</summary>
    public const int SecondIntervalTicks = TickRate;

    /// <summary>Out-of-combat natural regen cadence, in ticks. Default 30 = **3 seconds**, matching
    /// IG's `HP_REGENERATE_PERIOD = 3000`. NOT a const: it's live-editable from the admin Debug Tuning
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

    /// <summary>
    /// Minimum level to speak in WORLD chat. Local chat and whispers are never gated — the point is
    /// the channel that reaches everyone at once, which is the one worth farming accounts for.
    ///
    /// A mutable static rather than a `const` on purpose: it is a policy dial, not a fact about the
    /// game, and the value that is right depends on how much bot spam there actually is. Set it to 1
    /// to open world chat to everyone; wire it to the debug tuning panel if it ever needs changing
    /// mid-playtest. Staff (admin/moderator) are exempt — they have to be able to announce.
    /// </summary>
    public static int WorldChatMinLevel = 10;

    /// <summary>The anti-phishing line, shown on entering the world and again on your first whisper in
    /// any rolling hour. Owner, 2026-08-26, in the same breath as approving the chat-log whisper split.
    ///
    /// <para>🔑 WHY IT RIDES ON THE WHISPER and not on a timer: a whisper is where this scam happens.
    /// World chat is public and self-policing, local chat is a crowd — a private message from someone
    /// claiming to be staff is the shape of the attack, so the warning arrives in the same window the
    /// attempt does. Both sides of the conversation get it, each on their own hourly clock, so it does
    /// not matter which of you opened the exchange.</para>
    ///
    /// <para>⚠ It says NO STAFF MEMBER WILL EVER ASK, which is a promise the game then has to keep:
    /// nothing the server sends may ever ask a player for a password. If a real feature ever needs to,
    /// this line is what it has to be argued against.</para></summary>
    public const string ScamWarning =
        "No staff member will ask for login details or personal information! "
        + "Please be aware of scammers!";

    /// <summary>How long between repeats of <see cref="ScamWarning"/> in the whisper channel. His
    /// *"each first whisper in every hour"* — a rolling hour per player, not a wall-clock one, so a
    /// player who whispers twice a day sees it twice rather than never.</summary>
    public const int ScamWarningIntervalMinutes = 60;

    // ----- Items / progression / trade (Phase 4) -------------------------------

    // Slot cap counts UNEQUIPPED items only — worn gear doesn't occupy a bag slot (owner). 250 for now
    // (30 was far too low once every gear piece and material stacked up).
    public const int InventorySize = 250;

    /// <summary>Private-warehouse slot cap (per character). Like the bag, a slot is one unequipped item;
    /// the warehouse never holds equipped gear. A timed rune stored here still expires but does NOT
    /// apply its buff — that's how you switch a rune off.
    ///
    /// ⚠ Raised 50 → 200 for playtest-19 (owner, 46o): the cap was in the way and the expansion system
    /// that is supposed to sell the space does not exist yet. **When the expandable warehouse lands,
    /// pull this back to the BASE (his figure: ~150 private / ~100 account) and let tickets buy the
    /// rest** — otherwise there is nothing left to sell.</summary>
    public const int WarehouseSize = 200;

    /// <summary>ACCOUNT-warehouse slot cap — shared by every character on the account, which is what
    /// makes it the way to move gear between your own characters.
    /// ⚠ Same story as <see cref="WarehouseSize"/>: raised for 46o, lower it to base when the
    /// expandable system lands.</summary>
    public const int AccountWarehouseSize = 200;

    /// <summary>Gold charged for each SLOT the account bank has to open (owner: 10k). Merging into a
    /// stack that is already in there is free — the fee buys the slot, not the deposit. The private
    /// warehouse stays free; this one crosses characters, so it has to cost something or it is simply
    /// a bigger bag with no downside.</summary>
    public const long AccountWarehouseSlotFee = 10_000;

    /// <summary>How many recently-sold items the buy-back list keeps (per character, in-memory). Selling
    /// past this drops the oldest entry. 24 was long enough to scroll (owner, playtest-19 M14) — the
    /// list exists to undo the sale you just regretted, not to be a second inventory.</summary>
    public const int BuyBackSlots = 12;

    /// <summary>How many recently BINNED items can be undone (per character, in-memory). His own number
    /// (playtest-17 C18): the sold list and the deleted list are separate, "shops last 10-20 items and
    /// restore last 5". Restoring a binned item is FREE — you were never paid for it.
    /// ⚠ Deliberately NOT behind a vendor: you bin things in the field, which is where the accident
    /// happens, so the undo has to be reachable there too.</summary>
    public const int RestoreSlots = 5;

    /// <summary>How many quests may be pinned to the on-screen tracker at once. The tracker earns its
    /// place by being readable at a glance while you fight; a dozen pinned quests is just the log
    /// again, in the way of the game (owner asked for 3-5). Shared, because the SERVER owns the pins
    /// now (playtest-18 Q1) and a client with a different idea of the cap would draw a lie.</summary>
    public const int MaxTrackedQuests = 5;

    // ----- Charisma (reputation) -----
    /// <summary>Likes a player may GIVE per day (a budget, freely distributed; resets at UTC midnight).</summary>
    public const int DailyLikeBudget = 20;
    /// <summary>Charisma POOL cap. Every <see cref="CharismaPerBonusPercent"/> of pool = +1% exp/sp, so the
    /// cap is +50%. The pool is drained by kills (and, later, moderation); the lifetime value (uncapped)
    /// is what the ranking board uses.</summary>
    public const int CharismaPoolCap = 1000;
    /// <summary>Pool points per +1% exp/sp. 20 → cap 1000 gives +50%.</summary>
    public const int CharismaPerBonusPercent = 20;
    /// <summary>A kill drains this × the karma gained from it, off BOTH charisma values (200 karma → −2,
    /// 15 000 → −150). Bad behaviour costs reputation.</summary>
    public const double CharismaKillPenaltyPerKarma = 0.01;

    /// <summary>Exp/sp multiplier from a character's charisma pool (1.0 … 1.5).</summary>
    public static float CharismaExpMultiplier(int pool) =>
        1f + Math.Clamp(pool, 0, CharismaPoolCap) / (float)(CharismaPerBonusPercent * 100);

    // Moderation charisma penalties (per STARTED hour-band): a chatban costs 20, a jail 100, a kick 250,
    // scaling by the duration tier (&lt;1h ×1, &lt;2h ×2, …). A ban zeroes both values. All drain BOTH the
    // pool and the lifetime — a griefer can't top the ranking board and just eat the punishments.
    public const int CharismaChatBanPenaltyPerHour = 20;
    public const int CharismaJailPenaltyPerHour = 100;
    public const int CharismaKickPenaltyPerHour = 250;

    /// <summary>Charisma lost for a moderation action of <paramref name="minutes"/> minutes: the per-hour
    /// base × the duration tier (minutes/60 + 1, so &lt;1h=×1, [1h,2h)=×2, …).</summary>
    public static int CharismaModerationPenalty(int basePerHour, int minutes) =>
        basePerHour * (Math.Max(0, minutes) / 60 + 1);

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
    // Every remaining command whose only argument is a NAME — so the TARGET supplies it and nothing has
    // to be typed (owner, 2026-07-24). Commands that need a real VALUE (a whisper's message, a trade
    // quantity) stay typed, because a button cannot supply one.
    public const string ActionFriendAdd     = "friend_add";
    public const string ActionFriendRemove  = "friend_remove";
    public const string ActionFriendList    = "friend_list";
    public const string ActionPartyLeave    = "party_leave";
    public const string ActionPartyKick     = "party_kick";
    public const string ActionPartyLeader   = "party_leader";
    /// <summary>The one action for a command that needs TYPED text. It cannot send the whisper — it
    /// prepares it: the target's name goes into the command box after "/w " and the caret waits.</summary>
    public const string ActionWhisperTarget = "whisper_target";
    public const string ActionLike          = "like_target";
    public const string ActionBlock         = "block_target";
    public const string ActionUnblock       = "unblock_target";

    // A new character starts with a COMPLETELY EMPTY bar (owner, 2026-07-20). Nothing is placed for
    // you — not skills, not even the actions. The player builds their own bar from the skills window's
    // Skills and Actions tabs.

    // ----- Target-closest search radius (client preference) -----------------------

    /// <summary>Default radius for the "target closest" action.</summary>
    public const float TargetSearchRangeDefault = 1000f;
    public const float TargetSearchRangeMin = 400f;
    public const float TargetSearchRangeMax = 1500f;

    public const int ClassChangeLevel = 20;

    /// <summary>Max classes ONE character may own (IG-style: the main class + up to 3 subclasses).
    /// Stops a character stacking pointless duplicate base classes when only a few can reach a unique
    /// 3rd-class discipline. The player-facing swap rules are <see cref="SubclassSwapDelaySeconds"/>.</summary>
    public const int MaxSubclasses = 4;

    /// <summary>How long a class change takes when it is started OUTSIDE a town or peace zone
    /// (`BL-36`, his 2026-08-14 ruling). Inside one it is instant and this never applies.
    ///
    /// <para>His shape, in full: *"Out of a town: a 5-minute wait. In a peace zone/town: INSTANT, no
    /// cd."* Both cases require being out of combat, and 🔑 *"When changed out if town and 5min start
    /// to count and enter in town the countdown stays … w8 the 5mins then change (city don't trigger
    /// the cd) both waits it."* — so walking into a city neither cancels nor shortcuts a timer that is
    /// already running. The city only means a timer never STARTS.</para></summary>
    public const int SubclassSwapDelaySeconds = 300;

    /// <summary>OBSOLETE (2026-07-24) — no longer read by anything; kept only so the history is legible.
    /// This was the old party EXP band: a member more than this many levels from the KILLER earned
    /// nothing. It has been replaced by <see cref="ExpCurve.LevelGapMultiplier"/>, which is measured
    /// against the MOB rather than the killer, tapers (0.85 per level past 5) instead of cliff-edging,
    /// zeroes at 13, and applies PERSONALLY to each member's share rather than gating the whole party.
    /// Delete once nobody needs the breadcrumb.</summary>
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

    /// <summary>Longest name an admin may write onto ONE item instance with `/give` (owner, `58d`:
    /// *"newItemName: quoted, max 20 chars, spaces allowed"*). Short on purpose — it shares the plate
    /// and the inventory row with the enchant and the tag, and a long one pushes those off the line.</summary>
    public const int CustomItemNameMax = 20;

    public const int TradeMaxOfferSlots = 10;

    /// <summary>Both characters must be this close to start a trade.</summary>
    public const float TradeRange = 300f;

    /// <summary>How close you must stand to a dummy that HITS BACK for it to reach you (owner,
    /// playtest-20 `56c`: *"lvl 80, 50 range, 1 magic dmg every 0.1s"*).
    ///
    /// <para>🔴 It WAS his literal 50, and that is why both dummies "act as the old" (`63h`): a melee
    /// attacker is walked to <see cref="MeleeRange"/> = 80 and stops there, so the closest anyone ever
    /// stood by playing normally was 30 units OUTSIDE the strike radius, and a caster stands at 600.
    /// The dummy struck nobody, ever. 150 is the smallest value that a melee stop-distance fits inside
    /// with room to spare while still excluding anyone casting from range — you must walk up to it, and
    /// a few steps back still ends the test, which was the whole intent of a short radius.</para></summary>
    public const float DummyStrikeRange = 150f;

    // ----- Admin / jail (Phase 5) ----------------------------------------------

    /// <summary>How many days of chat the moderation log keeps. Swept every six hours by
    /// <c>GameLoopService.FlushChatLog</c>. <b>0 would mean keep everything forever</b> — it does not
    /// any more.
    ///
    /// **90, ruled by the owner 2026-08-26** (`BL-89`): *"90 days retention no point in keeping more ..
    /// if some1 gets reported .. must take no more than a week to deem him banable or not"*.
    ///
    /// 🔑 **The reasoning is the useful part, because it is what a future change has to argue against.**
    /// The window is not sized to how long the evidence stays *interesting* — it is sized to how long a
    /// CASE can stay open, and a case is a week. 90 days is therefore ~12× the longest decision he will
    /// tolerate: enough slack for a report that arrives late, a moderator on holiday, or a pattern that
    /// only shows up when someone finally looks, while still being far short of a permanent record of
    /// everything every player ever whispered. Do not raise it "to be safe" — that is the exact instinct
    /// he ruled against, and an indefinite chat archive is a liability, not a safety margin.</summary>
    public const int ChatLogRetentionDays = 90;

    /// <summary>Jail sits in the NEGATIVE quadrant (owner: dungeons + jail live at minus coordinates,
    /// away from the overworld). It is the CENTRE of the jail yard, not where inmates stand.</summary>
    public const float JailX = -4000f;
    public const float JailY = -4000f;

    /// <summary>The jail YARD — one shared room, <see cref="JailWidth"/> × <see cref="JailHeight"/>
    /// (owner, playtest-20 `61d`: *"the jail cell is 1px × 1px … one shared jail, not a cell per
    /// player"*). It was already a 260-unit circle you could pace, but every sentence and every relog
    /// placed the inmate on the exact centre COORDINATE, so any number of inmates stood in one spot and
    /// the room read as a point. The shape is his 300 × 500 and arrivals are spread across it
    /// (<see cref="JailArrival"/>). Serving a sentence should feel like a room, not paralysis — walking
    /// is all you may do; chat, skills, items and escape stay blocked.</summary>
    public const float JailWidth = 300f;
    public const float JailHeight = 500f;

    /// <summary>How far from the wall an arriving inmate is placed, so nobody spawns standing in it.</summary>
    public const float JailArrivalMargin = 30f;

    /// <summary>Somewhere to stand in the yard. Spread, so two inmates are two people in a room rather
    /// than one avatar drawn twice.</summary>
    public static (float X, float Y) JailArrival(System.Random rng)
    {
        float hw = JailWidth / 2f - JailArrivalMargin, hh = JailHeight / 2f - JailArrivalMargin;
        return (JailX + (float)(rng.NextDouble() * 2 - 1) * hw,
                JailY + (float)(rng.NextDouble() * 2 - 1) * hh);
    }

    /// <summary>Broadcast "X entered/left the world." to EVERY player. **Off** (owner): presence is
    /// private — you should not learn that someone logged in, nor should they learn that you did,
    /// unless you are MUTUAL friends (that notice is `NotifyFriendsPresence`, which is a different and
    /// correctly-gated message). Flip to true only as a debugging aid; the server log records every
    /// entry regardless, so debugging rarely needs it.
    /// (`static readonly`, not `const` — a const false makes the call sites dead code and the compiler
    /// warns on every one of them.)</summary>
    public static readonly bool AnnounceWorldEntryExit = false;

    /// <summary>Periodic character auto-save interval (ticks). 600 = 60s.</summary>
    public const int AutoSaveIntervalTicks = 600;

    /// <summary>Skill points earned per exp point (≈ 1/4 of exp).</summary>
    public const float SkillPointRatio = 0.25f;

    /// <summary>How close you must be to an NPC to talk.</summary>
    public const float TalkRange = 250f;

    // ----- Vendors (Phase 21) -------------------------------------------------

    /// <summary>Fraction of an item's Value a vendor pays when you SELL to it. Applies to the
    /// GENERIC price formula only — mats, potions, scrolls, legacy gear. Tiered gear has its own
    /// rule (<see cref="GearSellDivisor"/>), because gear is what floods the economy.</summary>
    public const float VendorSellFraction = 0.30f;

    /// <summary>Tiered gear (and use-consumables) sell for their BUY price divided by this. The owner's
    /// original acceptance test was "selling ~25 Robes should buy one Leathers" — same slot, same
    /// grade+rarity, so they share a buy price and the divisor IS that ratio. That was 25.
    ///
    /// It is 10 as of playtest-18 (owner, 2026-08-05), and the direction is deliberate: sold gear was
    /// ~10x the mob's own gold drop and the faucet had to come down ~4x, but doing that with the PRICE
    /// would have left the player wading through the same flood of near-worthless drops. So the cut went
    /// on the drop RATE instead (<see cref="RateConfig.DropGroupRates"/>, the four gear groups, 13x
    /// rarer) and this moved the OTHER way — fewer drops, each one worth 2.5x more. Ten Robes buy one
    /// Leathers now. Measured in tools/BalanceMatrix; change it there and re-run, don't re-derive.</summary>
    public const int GearSellDivisor = 10;

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

    /// <summary>Below this level every gatekeeper ride is FREE (owner, playtest-15 §32u). Levelling
    /// characters change hunting ground constantly and have no income yet; the fee only starts to be a
    /// meaningful sink once you are farming for gold rather than for levels.</summary>
    public const int FreeTeleportUnderLevel = 40;

    /// <summary>Gold fee to warp between two safe zones (distance-based).</summary>
    public static int TeleportFee(SafeZone from, SafeZone to) =>
        TeleportFee(from.X, from.Y, to.X, to.Y);

    /// <summary>The fee THIS character pays — the distance fee, or nothing while under
    /// <see cref="FreeTeleportUnderLevel"/>. Both the gatekeeper's price list and the charge itself go
    /// through here, so what you are quoted is what you are billed.</summary>
    public static int TeleportFee(int level, float fromX, float fromY, float toX, float toY) =>
        level < FreeTeleportUnderLevel ? 0 : TeleportFee(fromX, fromY, toX, toY);

    public static int TeleportFee(int level, SafeZone from, SafeZone to) =>
        TeleportFee(level, from.X, from.Y, to.X, to.Y);

    /// <summary>Gold fee to warp between two POINTS. Field gates are points, not safe zones, and a short
    /// hop to a camp on your own city's doorstep should cost accordingly — so the fee is the same
    /// distance rule rather than a second pricing scheme.</summary>
    public static int TeleportFee(float fromX, float fromY, float toX, float toY)
    {
        float dx = toX - fromX, dy = toY - fromY;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        return Math.Max(TeleportMinFee, (int)(dist * TeleportGoldPerUnit));
    }
}
