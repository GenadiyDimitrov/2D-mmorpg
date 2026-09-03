namespace Game.Shared;

// ---------------------------------------------------------------------------
// Network contracts. These records are serialized by SignalR (System.Text.Json)
// in both directions. Keep them flat and small — they go over the wire 10x/sec.
// ---------------------------------------------------------------------------

/// <summary>Client -> Server: enter the world with a character.</summary>
public record LoginRequest(string CharacterName, Race Race, BaseClass BaseClass);

/// <summary>Server -> Client: result of a login attempt. <paramref name="Role"/> is the staff role of
/// the CHARACTER you just entered with (roles are per-character, not per-account) — the client uses it
/// only to decide which commands are worth sending; the server authorizes every one of them anyway.</summary>
public record LoginResult(
    bool Success,
    string? Error,
    Guid EntityId,
    float X,
    float Y,
    DateTime ServerEpochUtc = default,
    AccountRole Role = AccountRole.Player);

/// <summary>One visible entity's state inside a snapshot.</summary>
public record EntityDto(
    Guid Id,
    string Name,
    EntityKind Kind,
    Race Race,
    BaseClass BaseClass,
    float X,
    float Y,
    float Speed,
    int Level,
    int Hp,
    int MaxHp,
    int Mp,
    int MaxMp,
    int SecondClass,
    int ThirdClass,
    bool Dead,
    // A link-dead player in the reconnect grace window: clients draw a "Disconnected" title
    // above the head. Offline-FARMING players are NOT flagged (they look like normal players).
    bool Disconnected = false,
    // PvP name colour: Innocent = white, Flagged = purple, Pk = red.
    PvpFlag Flag = PvpFlag.Innocent,
    // Mobs only: this one attacks on sight. Clients mark it with a "*" after the name so you can see
    // what to tiptoe around BEFORE it decides for you. Cached on the entity at spawn, so this costs a
    // bool per snapshot and no catalog lookups.
    bool Aggressive = false,
    // THE TITLE LINE over this entity's head: the words, and the colour to draw them in (RRGGBB, no
    // '#'; "" = TitleCatalog.DefaultHex). TEXT, not an id — a title may be granted by a board, by a
    // staff role, or written by its owner with `/title`, and the plate must not have to know which.
    // For an NPC this is the ROLE half of its name ("Elder" over "Marius").
    // Empty for everyone not wearing one, so it costs an empty string per snapshot.
    string Title = "",
    string TitleColor = "",
    // Mobs only: the SOCIAL CLAN this creature answers a cry from, or "" for a loner. Playtest 23:
    // *"add info like -> agro:true/false, social: true/false, social clan: clanName, info that will be
    // helpful to a player."* It is the raw clan NAME rather than a bool because "social" alone does not
    // tell you the thing that matters — which OTHER creatures come — and the target frame prints the
    // name for exactly that reason.
    // ⚠ Sent as "" while GameConstants.MobClansEnabled is off (`BL-73`), so the frame never advertises
    // a behaviour the simulation is not currently running. It comes back on with the switch.
    string SocialClan = "",
    // `BL-93` — WHAT this creature IS, so the client can pick a MODEL for it.
    //
    // 🔑 Deliberately the AUTHORED taxonomy (`MobCatalog`'s own Category/Role) and NOT a new "model
    // id" invented for the client. Three reasons, and they are the whole design:
    //   1. The server says what a thing IS; the client decides how it LOOKS. A mesh name on the wire
    //      would put an art decision in the simulation, where it can never be changed without a
    //      protocol bump.
    //   2. Every template ALREADY declares a Category (it maps the CSV "Type" column), so a new mob
    //      inherits a model for free — nobody has to remember a second table. A parallel taxonomy is
    //      a thing that drifts.
    //   3. The art budget is per FAMILY, not per mob: nine categories × three roles is the whole
    //      model set, and tint + scale separate members inside one.
    //
    // MOBS ONLY. For a player or NPC these stay at their defaults and the client uses Race +
    // BaseClass + SecondClass/ThirdClass, which it already has and which says strictly more than a
    // model needs. Both are enums with a cheap default, so a client that never reads them is
    // unaffected — this costs two bytes on a SPAWN dto and nothing per tick (see EntityLean).
    MobCategory Category = MobCategory.Humanoid,
    MobRole Role = MobRole.Melee,
    // ---- THE TELEPORT COUNTER (playtest 29: *"phase shift don't visually update my position"*) ----
    //
    // 🔑 A JUMP IS A DIFFERENT EVENT FROM A WALK, AND THE CLIENT CANNOT INFER WHICH IT GOT. It used to
    // guess by DISTANCE — under 5 Unity units (500 server units) is movement, over it is a teleport —
    // and a 200-unit blink is 2. Worse, a self-prediction in flight tolerates 2.5 units of server
    // disagreement before it snaps, so a rung-1 Phase Shift fits inside BOTH thresholds: the server
    // moved him 200 units, the mob followed, and the client went on drawing the walk it was
    // predicting. A threshold cannot separate "he blinked 200" from "he walked 200"; only the server
    // knows, so the server says.
    //
    // It is a COUNTER and not a bool so it survives a dropped frame and a re-spawn of the entity: the
    // client stores the last value it saw and snaps whenever the number CHANGES, which is correct from
    // any starting value. Wrapping at 255 is harmless — two warps 256 apart with no frame between them
    // is not a thing that happens at 10 ticks/sec.
    //
    // Costs one byte on a spawn and one on a lean. An old client ignores it and behaves exactly as it
    // does today; a new client against an old server reads 0 forever and never snaps, which is also
    // exactly as today.
    byte Warp = 0);

/// <summary>What to draw over an NPC's head about quests. Sent per player, because availability is
/// personal — level, race, class and what you have already done all decide it.</summary>
public enum QuestMarkState { None = 0, Available = 1, InProgress = 2, ReadyToHandIn = 3 }

/// <summary>One NPC's quest marker.</summary>
public record QuestMark(Guid NpcEntityId, QuestMarkState State);

/// <summary>Server -> owning client: which NPCs currently have something quest-shaped for YOU.
/// Rides alongside every QuestLog push, so it can never drift out of step with the log. The NPC
/// roster is small (a couple of dozen), so this sends every marked NPC rather than only the visible
/// ones — cheaper than tracking view state, and the marker is already right when one comes on screen.</summary>
public record QuestMarks(QuestMark[] Marks);

/// <summary>Client -> Server: "move me toward this point" (click-to-move).
/// Moving cancels engagement, queued skills, and casting (classic MMO).</summary>
public record MoveCommand(float TargetX, float TargetY);

/// <summary>Server -> Client, every tick: everything you can currently see
/// (including yourself). Anything not listed has left your view range.
/// SUPERSEDED for the live path by <see cref="SnapshotDelta"/> (kept for reference/compat).</summary>
public record WorldSnapshot(EntityDto[] Entities);

/// <summary>The fields of an entity that change tick-to-tick — the LEAN per-tick update. The STATIC
/// fields (name, class, level, max HP/MP, aggressive, …) are sent ONCE as a full <see cref="EntityDto"/>
/// spawn and never repeated, so this is all the wire needs while an entity is just moving/fighting.</summary>
public record EntityLean(
    Guid Id, float X, float Y, float Speed,
    int Hp, int Mp, bool Dead, bool Disconnected, PvpFlag Flag,
    // A TELEPORT COUNTER, bumped by every server-side reposition that is NOT the walk simulation:
    // a blink, a knockback, a gatekeeper ride, a respawn, a leash reset, an admin jump. See
    // EntityDto.Warp — the two carry the same number and the client reads whichever arrives.
    byte Warp = 0);

/// <summary>Server -> Client, every tick: a DELTA of the viewer's world.
///   Spawns   = entities that just ENTERED view (or whose static data changed) — full DTOs.
///   Updates  = entities still in view whose dynamic fields changed — lean.
///   Despawns = entities that LEFT view (or were removed).
/// An entity absent from all three is UNCHANGED — the client keeps what it has (unlike WorldSnapshot,
/// where absence meant "removed"). This stops re-sending ~11 static fields per entity 10×/second.</summary>
public record SnapshotDelta(EntityDto[] Spawns, EntityLean[] Updates, Guid[] Despawns);

/// <summary>Server -&gt; Client: one planted totem, as the ground needs to draw it.
///
/// <para>🔑 A totem is NOT an entity — it is a <c>TotemInstance</c> in a plain list on the world, so it
/// never travelled in a snapshot and the client had no idea one existed. The owner planted totems for
/// weeks and saw nothing: *"Totem work (invisible but work)"*. This is the whole of what a viewer
/// needs to stand in the right place, and nothing else.</para>
///
/// <para><paramref name="Radius"/> is the SERVER radius (the same units as X/Y) — the client scales it
/// through WorldMapper like every other distance. <paramref name="Heals"/> / <paramref name="Restores"/>
/// come from the totem's snapshotted Effect, and they are not exclusive: a totem carrying both pulses
/// both, and the client blends the two colours rather than picking one.</para></summary>
public record TotemDto(Guid Id, float X, float Y, float Radius, bool Heals, bool Restores);

/// <summary>Server -&gt; Client: every totem this viewer can see, whole. Small and rare enough that a
/// diff would cost more than it saves — the loop sends it only when the visible SET changes, so a
/// world with no totems in it is silent.</summary>
public record TotemList(TotemDto[] Totems);

/// <summary>`BL-109` — one WHISP, for drawing. A whisp is not an entity (see <c>WhispInstance</c>), so
/// it never appears in the world delta and this is the only way a client learns one exists.
///
/// <para><paramref name="OwnerId"/> is the master's entity id — the whisp belongs to a character, and
/// a client that cannot see that character has no business drawing his spirits.
/// <paramref name="SecondsLeft"/> is what lets the UI show a whisp expiring the way a buff does.</para></summary>
public record WhispDto(string SummonSkillId, Guid OwnerId, string Name, float X, float Y, int SecondsLeft);

/// <summary>Server -&gt; Client: every whisp this viewer can see. Sent whole for the same reason
/// <see cref="TotemList"/> is — there are at most three per character and the list is silent while
/// nobody in view has one.</summary>
public record WhispList(WhispDto[] Whisps);

/// <summary>What an area effect DID, so the client can colour the flash without knowing any skill
/// ids. Heal and Mana match the totem colours on purpose — the same green and blue mean the same
/// thing whether they linger or flash.</summary>
public enum AreaEffectKind { Buff = 0, Heal = 1, Mana = 2, Harm = 3, Resurrect = 4 }

/// <summary>Server -&gt; Clients nearby: an area skill just LANDED, centred here. One shot, no id and
/// nothing to clean up — the client flashes the circle and forgets it.
///
/// <para>The owner's ask: *"When done casting for a brief moment shows the range — resurrection field
/// .. The party heal .. They just flash one time when cast ends as if the effect is applied"*. Sent at
/// the point the cast is committed, so it fires exactly when the effect does — never on a cast that
/// was interrupted or refused for MP.</para></summary>
public record AreaEffectEvent(float X, float Y, float Radius, AreaEffectKind Kind);

/// <summary>Server -> Client: a chat line. To is set for whispers.</summary>
public record ChatMessage(string From, string Text, ChatChannel Channel, string? To = null);

/// <summary>Server -> Clients near the fight: one resolved combat action.
/// Damage doubles as the heal amount for Heal; Skill is set for skill-based
/// outcomes (and carries the buff/debuff name for Buff).</summary>
public record CombatEvent(
    Guid AttackerId,
    string AttackerName,
    Guid TargetId,
    string TargetName,
    int Damage,
    CombatOutcome Outcome,
    string? Skill = null);

/// <summary>Server -> the owning client: exp/level progress after a kill.
///
/// SkillPoints rides along because SP is earned on the SAME event as exp, and this is the only push
/// that fires on every kill. It used to travel solely in StatsUpdate, which the kill path never sent,
/// so the SP figure sat at its login value for a whole session and only corrected on relog. Sending
/// the full ~45-field StatsUpdate per kill would fix it far more expensively.</summary>
public record ProgressUpdate(
    int Level,
    long Exp,
    long ExpToNext,
    bool LeveledUp,
    int SkillPoints = 0);

/// <summary>Server -> the casting client: show/update the cast bar.
/// Seconds &lt;= 0 means the cast was cancelled — hide the bar.</summary>
public record CastInfo(string SkillName, float Seconds);

/// <summary>Server -> a fallen player: an ally (or a scroll) offers to resurrect you. The client shows a
/// confirm prompt; the player accepts/declines (see ResurrectResponse) so they don't revive on top of the
/// mob that killed them. ExpPct is the fraction of lost exp restored; ExpRestored is the resulting amount.</summary>
/// <summary><paramref name="SelfRes"/> marks the preservation skills' own prompt (Undying Will / Rite of
/// Preservation): there is no rescuer, the caster IS the corpse, and the offer never expires. The client
/// needs it to word the prompt — "&lt;your own name&gt; offers to resurrect you" reads like a bug — and it
/// travels as a flag rather than a name comparison because a name match would also fire on a same-named
/// character raising you.</summary>
public record ResurrectOffer(string FromName, float ExpPct, long ExpRestored, bool SelfRes = false);

/// <summary>Server -> nearby clients: a MOB started casting (drives a cast bar over the mob's head,
/// so a boss's telegraphed slam is visible/dodgeable). Seconds 0 = the cast ended/was cancelled.</summary>
public record MobCastInfo(Guid CasterId, string SkillName, float Seconds);


/// <summary>One item instance in a player's inventory.</summary>
/// <summary>One item instance on the wire. The last three are the PER-INSTANCE overrides of `58d`
/// (owner, playtest-20): a real item carrying tags, never a cloned `_bound` def. Each is null when the
/// instance has no opinion and the catalog's own value applies. See <see cref="ItemTag"/>.</summary>
public record InventoryItemDto(Guid InstanceId, string DefId, bool Equipped, int Enchant, int Quantity,
    ItemAttribute[] Attributes, DateTime? ExpiresAtUtc = null,
    long? SellPriceOverride = null, bool? TradableOverride = null, string? CustomName = null,
    bool? CanStorePrivate = null, bool? CanStoreAccount = null);

/// <summary>The tag an item instance DISPLAYS, derived from its three properties rather than stored —
/// so there is exactly one truth and a tag can never disagree with the behaviour (owner, `58d`).
///
/// <para>His table: sellable+tradable reads as nothing at all; neither reads as <b>bound</b>; sellable
/// but not tradable reads as <b>private</b> (his word, and he said the name is open). A TIMER composes
/// on top rather than replacing it, so a bound timed Soulcrystal is `Soulcrystal (temporary, bound)`.
/// The fourth combination — tradable but worthless — is left untagged: it describes half the drop
/// table, and a tag that appears on everything tells you nothing.</para></summary>
public static class ItemTag
{
    // ⚠ The three predicates live HERE, taking primitives, so the server's InventoryItem and the
    // client's item card read ONE implementation. Duplicating them was how `67i` happened — a display
    // that quietly disagreed with the rule it was describing, with nothing to fail loudly.

    /// <summary>What a vendor pays for an instance: its own price if it has one, else the def's.</summary>
    public static long SellPrice(ItemDef def, long? sellOverride) =>
        sellOverride ?? ItemCatalog.SellPrice(def);

    /// <summary>May this instance leave the character?</summary>
    public static bool Tradable(ItemDef def, bool? tradeOverride) => tradeOverride ?? def.Tradable;

    /// <summary>May a vendor buy it? Same three conditions as <see cref="ItemCatalog.IsSellable"/>.</summary>
    public static bool Sellable(ItemDef def, long? sellOverride, bool? tradeOverride) =>
        Tradable(def, tradeOverride) && def.Slot != EquipSlot.QuestItem
        && SellPrice(def, sellOverride) > 0;

    /// <summary>May this instance go into the character's PRIVATE keeper? The instance's own opinion
    /// wins; otherwise the def's — normally yes (that bank is just a bigger bag), but never for a
    /// SoulBound def like the Rune of Sinners.</summary>
    public static bool StorablePrivate(ItemDef def, bool? storeOverride) =>
        storeOverride ?? !def.SoulBound;

    /// <summary>May this instance go into the ACCOUNT keeper? The instance's own opinion wins;
    /// otherwise the standing rule — TRADABLE only, since that bank is a door between characters —
    /// and never for a SoulBound def.</summary>
    public static bool StorableAccount(ItemDef def, bool? storeOverride, bool? tradeOverride) =>
        storeOverride ?? (!def.SoulBound && Tradable(def, tradeOverride));

    /// <summary>The name THIS copy goes by — the def's unless one was written for the instance.</summary>
    public static string Name(ItemDef def, InventoryItemDto i) =>
        string.IsNullOrEmpty(i.CustomName) ? def.Name : i.CustomName!;

    /// <summary>The tag for one instance on the wire.</summary>
    public static string For(ItemDef def, InventoryItemDto i) =>
        Of(Sellable(def, i.SellPriceOverride, i.TradableOverride),
           Tradable(def, i.TradableOverride),
           i.ExpiresAtUtc.HasValue);

    public static string Of(bool sellable, bool tradable, bool timed)
    {
        string bond = !tradable ? (sellable ? "private" : "bound") : "";
        string[] parts = timed && bond.Length > 0 ? new[] { "temporary", bond }
                       : timed                    ? new[] { "temporary" }
                       : bond.Length > 0          ? new[] { bond }
                       : System.Array.Empty<string>();
        return parts.Length == 0 ? "" : "(" + string.Join(", ", parts) + ")";
    }
}

/// <summary>Server -> owning client: full inventory sync (sent on change).</summary>
public record InventoryUpdate(InventoryItemDto[] Items);

/// <summary>The character's private warehouse contents (same shape as the bag). Sent when the warehouse
/// window is opened and after every deposit/withdraw.</summary>
public record WarehouseUpdate(InventoryItemDto[] Items);

/// <summary>The ACCOUNT-wide warehouse, shared by every character on the account. Same shape as the
/// private one; the size cap and the per-slot deposit fee are constants both sides already know
/// (<see cref="GameConstants.AccountWarehouseSize"/>, <see cref="GameConstants.AccountWarehouseSlotFee"/>).</summary>
public record AccountWarehouseUpdate(InventoryItemDto[] Items);

/// <summary>Server -> client: someone wants to trade with you.</summary>
public record TradeRequestNotice(Guid FromId, string FromName);

/// <summary>Client -> server: ONE line of a trade offer — an item instance and HOW MANY of it.
/// Quantity is meaningful only for stackables (the server clamps it to 1..stack, and to 1 for gear),
/// which is what lets you put 20 of your 50 potions on the table instead of the whole stack.</summary>
public record TradeOfferEntry(Guid InstanceId, int Quantity);

/// <summary>Server -> both traders: full trade state (sent on every change).
/// Active=false closes the trade window.</summary>
public record TradeStateUpdate(
    bool Active,
    string PartnerName,
    InventoryItemDto[] MyOffer,
    InventoryItemDto[] TheirOffer,
    bool MyReady,
    bool TheirReady,
    long MyGold = 0,
    long TheirGold = 0);


/// <summary>Server -> owning client: full derived stats for the Stats window.
/// Sent whenever stats change (level, equip, class change).</summary>
public record StatsUpdate(
    int Con, int Atk, int Wit, int Agi, int Spt,
    int MaxHp, int MaxMp, int AttackPower, int Defence,
    int Accuracy, int Evasion, float CritChance, float BasicAttackRange,
    int SecondClass, float MoveSpeed, float CastModifier,
    float CastSpeedMult, float AttackSpeedMult, int SkillPoints, MoveState MoveState,
    int MagicAttack, float MagicCritChance,
    bool HasShield, float BlockChance, float BlockReduction, int ShieldDefense,
    int MagicDefence, string ActiveSet, string ArmorMastery,
    // Extended debug stats (regens per second + the buff/effect layer).
    float HpRegen = 0f, float MpRegen = 0f, float CritDamage = 0f,
    float MeleeVamp = 0f, float SpellVamp = 0f, float CooldownReduction = 0f,
    // Magic landing (2026-08-10): resistance is DAMAGE reduction, the modifier is the tank's
    // ×N on the enemy's fizzle roll. Neither is a caster-side accuracy stat — there isn't one.
    float MagicResist = 0f, float MagicFailMod = 1f,
    float CritRateResist = 0f, float CritDmgResist = 0f, float BowResist = 0f,
    // Interrupt resistance as a whole PERCENT (SPT curve x resist buffs, folded). Was flat "points"
    // against a wit-based pool until IG's interrupt formula landed on 2026-08-26.
    int InterruptResist = 0,
    // DEBUG / IG-reference: the OLD-style internal M.Atk (base·levelMod²·buffs²) the shrunk display hides.
    int MagicAttackInternal = 0,
    // Heal stats (no M.Atk): output = (HealPowerFlat + skillPower)·HealPowerMod; received = (HealReceivedFlat
    // + output)·HealReceivedMod. Default 0/×1.
    int HealPowerFlat = 0, float HealPowerMod = 1f,
    int HealReceivedFlat = 0, float HealReceivedMod = 1f,
    // FLAT crit damage (the CSVs' "crit dmg +80"): attack added INSIDE the ratio on a crit only,
    // before the multiplier. Separate line from CritDamage, which is the multiplier bonus.
    float CritDamageFlat = 0f,
    // The FINISHED magic crit multiplier (2 = base, 2.6 = one blessing, 3.38 = both). Its own line
    // because CritDamage above is the PHYSICAL channel and the two share nothing.
    float MagicCritDamage = 2f);

/// <summary>Server -> owning client: a potion cooldown started (seconds),
/// or an active potion effect changed. Cooldown 0 = ready.</summary>
public record PotionStatus(float CooldownSeconds, string ActiveEffect);

/// <summary>One reuse timer for the action bar. <paramref name="Id"/> is the bar TOKEN it belongs
/// to — a skill id for a skill slot, an "item:defId" token for a consumable — so the client can
/// look it up with the token it already holds and needs no second mapping.
///
/// There is deliberately no "total" field: the push happens the tick the timer STARTS, so the first
/// Seconds the client sees for an id IS the full reuse. The client keeps that as the denominator and
/// only replaces it when Seconds jumps back UP (a restart) — which costs the server no extra state.</summary>
public record CooldownEntry(string Id, float Seconds);

/// <summary>Server -> owning client: every reuse timer currently running, pushed whenever one
/// STARTS (and once on entering the world). The client counts them down locally — expiry needs no
/// message. A full snapshot each time, not a delta: it is a handful of entries and it self-corrects
/// after any dropped push.</summary>
public record CooldownUpdate(CooldownEntry[] Entries);


/// <summary>One active buff/debuff on the player, for the buff bar + tooltip. Stacks &gt; 1
/// for a stacking effect (shown as "Name xN"). Icon = an emoji/glyph for the square (server-resolved,
/// per-class); "" falls back to the name's initials on the client.</summary>
/// <para>SourceSkillId/SourceName are set ONLY for a child of an improved (GROUP) buff with more
/// than one child, and name the parent: they are what lets the buff bar collapse a whole blessing
/// into one square instead of the four independent buffs it really is (docs/design/BuffLadders.md).
/// Deliberately not set for a potion or a scroll — those are one-child groups, and labelling their
/// square with the bottle's name instead of the effect's would be noise, not grouping.</para>
public record BuffDto(string Name, string Description, float SecondsLeft, bool IsDebuff,
    string Key = "", int Stacks = 1, BuffRow Row = BuffRow.Buff, string Icon = "",
    string SourceSkillId = "", string SourceName = "",
    /// <summary>The skill LEVEL this buff landed at, and 0 for anything that has no ladder (a synthetic
    /// row, a single-level buff). Owner, playtest 27: *"I can't see a buff's rank once I have it as
    /// effect - I see it in 'known' as `Aim Lv.1` but once is in the effects bar and click on it to
    /// open details. The title just says Aim no lvl"*. The bar knew the buff's LEVEL server-side all
    /// along (BuffInstance.Level, kept so a buff can be rebuilt on login) and simply never sent it, so
    /// the one screen where you go to ask "which rung am I actually carrying" could not answer.
    /// MaxLevel is deliberately NOT sent: the client has the whole catalog and can look it up.</summary>
    int Level = 0,
    /// <summary>The buff is HELD but PAYING NOTHING because its skill's weapon gate is shut right now
    /// (2026-09-02 — Bow Expertise with a dagger in hand). It keeps its clock and its slot, so the bar
    /// must still draw it; it draws DIMMED, with the reason on the detail card. Without this the fix is
    /// invisible in the worst way: a lit icon granting nothing looks exactly like the bug it fixed.
    ///
    /// <para>Optional with a default, so an older client simply ignores it and behaves as before.</para></summary>
    bool Suppressed = false,
    /// <summary>`BL-111` — DOES THIS BUFF OCCUPY ONE OF THE <see cref="GameConstants.MaxBuffSlots"/>?
    /// His whole complaint is a number he cannot see: *"I cannot see if I have 20 or less buffs to not
    /// over buff me"*.
    ///
    /// <para>🔑 IT IS COMPUTED BY THE SERVER, FROM THE SAME PREDICATE THAT EVICTS
    /// (<c>CountsAgainstBuffCap</c>) — not re-derived on the client from row and toggle. The client
    /// cannot see <c>CountsTowardBuffLimit</c>, which is authored per skill, so any client-side guess
    /// would be a second copy of the rule that drifts the first time one skill opts out. A counter
    /// that is nearly right is worse than none: he would stop trusting it and be back where he
    /// started.</para>
    ///
    /// <para>⚠ Which BAR a buff is drawn in and whether it COUNTS are two different questions, and
    /// they do not always agree — a potion's effect counts but has its own bar. The header counts
    /// this flag across every bar, so the number stays true whatever the grouping does.</para></summary>
    bool Counts = false);

/// <summary>Server -> client: the character's learned skills (id + current level) + SP.</summary>
public record LearnedSkills(SkillRef[] Skills, int SkillPoints);

/// <summary>A learned skill reference: its id and the level the character has it at.</summary>
public record SkillRef(string Id, int Level);

/// <summary>Server -> owning client: the player's current buffs (sent each
/// second while any are active, and once when the last one drops).</summary>
public record BuffUpdate(BuffDto[] Buffs);

/// <summary>Server -> owning client: a SELECTION box was opened — show a chooser. The
/// player picks PickCount of Options, then calls SelectBoxItems with the chosen ids.</summary>
public record SelectionOffer(Guid BoxInstanceId, string BoxName, SelectionOption[] Options, int PickCount);
public record SelectionOption(string ItemId, string Name);

/// <summary>Server -> owning client: a Rune of Tincture was used — show the palette (owner,
/// playtest-20 `59r`). The client answers with the ordinary SetTitleColor command, which is where the
/// rune is actually spent; opening the list costs nothing. Names only, because the server is the
/// authority on which colours exist and what hex each one is.</summary>
public record TitleColorOffer(string[] Colors);

/// <summary>Server -> owning client: the expanded target window (IG-style inspect) —
/// the target's detailed stats and, for a mob, its passive modifier lines.</summary>
public record TargetDetails(
    Guid Id, string Name, int Level, bool IsMob,
    int Hp, int MaxHp, int Mp, int MaxMp,
    int PAtk, int MAtk, int PDef, int MDef,
    int Accuracy, int Evasion, float CritChance,
    float BowResist, float CritResist,
    string[] Passives,
    // Active temporary effects on the target (incl. DoT stack counts), e.g. "Bleed x5",
    // "Slow" — so a Venomweaver/Tempest can read stacks on the enemy.
    string[] Effects,
    // For a MOB only: its level-appropriate drop list, "ItemName (chance%)" (effective chance, after the
    // global drop-rate). Empty for players. Shown behind the [Details] button in the mob target window.
    string[]? Drops = null,
    // Extended detail (appended, so older clients ignore it). Gives the inspect window the SAME depth as
    // the character sheet — base attributes, speeds, and the whole combat layer — because "better to
    // have the info and not need it than not have it" (owner). Rank = "Normal"/"Elite"/"Boss", "" for
    // a player.
    int Con = 0, int Atk = 0, int Wit = 0, int Agi = 0, int Spt = 0,
    float MoveSpeed = 0f, float AttackSpeedMult = 1f, float CastSpeedMult = 1f, float AttackRange = 0f,
    float MagicCritChance = 0f, float CritDamage = 0f,
    float MeleeVamp = 0f, float SpellVamp = 0f, float CooldownReduction = 0f,
    float HpRegen = 0f, float MpRegen = 0f,
    int InterruptResist = 0, float CritDmgResist = 0f, float MagicResist = 0f,   // InterruptResist: whole PERCENT
    string Rank = "",
    // For a MOB only: its ACTIVE kit, pre-formatted for the client's Skills tab — one title line per
    // skill followed by indented detail lines, the same shape Drops uses. Empty for a plain melee
    // creature (which is the useful answer, not a missing section) and for players: a mob's kit is
    // bestiary knowledge, another player's is not.
    string[]? Skills = null,
    // 🔴 For a MOB only: how it BEHAVES, which is what he asked the info window to start with (playtest
    // 23): *"add info like -> agro:true/false, social: true/false, social clan: clanName, info that will
    // be helpful to a player."* Aggression is per-SPAWN (a zone can turn a passive template hostile), so
    // it is read off the entity and not the template. SocialClan is "" for a loner and for every mob
    // while `BL-73`'s switch is down.
    bool Aggressive = false,
    string SocialClan = "");

/// <summary>Server -> owning client: the result of an enchant attempt.</summary>
public record EnchantResultDto(string ItemName, int NewEnchant, string Outcome, bool Destroyed);

/// <summary>Server -> owning client: an attribute reroll finished (inventory update
/// carries the new attributes; this drives the reroll popup refresh + a message).</summary>
public record RerollResultDto(string ItemName, string Outcome);

/// <summary>Server -> owning client: the player's gold wallet balance (sent on entry
/// and whenever it changes — kills, quest rewards, vendor buy/sell, teleport fees).</summary>
public record GoldUpdate(long Gold);

/// <summary>Server -> owning client: an incoming party invite from Inviter (accept/decline). Carries
/// the loot rule the invitee would be joining under so they can decide before accepting.</summary>
public record PartyInviteDto(Guid InviterId, string InviterName,
    LootMode LootMode = LootMode.Random);

/// <summary>Server -> a party member: the leader proposes a loot-rule change and needs everyone to
/// agree. Open=true shows the accept/decline prompt; Open=false dismisses it (vote resolved).</summary>
public record PartyLootVoteDto(LootMode Mode, string RequestedBy, bool Open = true);


// ----- Auto-hunt / idle farming (docs/design/AutoHunt.md) -------------------------

/// <summary>One auto-use skill: the skill id, whether it's on, and an ADDITIONAL post-cast delay
/// (ticks, ≥0) on top of the skill's own reuse (so auto-reuse is never below the default).</summary>
public record AutoSkillDto(string SkillId, bool Enabled, int ExtraDelayTicks);

/// <summary>One class a character owns (an IG-style subclass). Server → client, so the UI can list
/// them and let you swap. <paramref name="Active"/> = the one being played right now.</summary>
public record SubclassDto(
    int Slot, Race Race, BaseClass BaseClass, int SecondClass, int ThirdClass, int Level, bool Active,
    // 0 = none; a FourthClassCatalog id (201-236). Defaulted so an older client still deserialises.
    int FourthClass = 0);

/// <summary>Every class this character owns. Pushed on login and after any add/swap.</summary>
public record SubclassListDto(SubclassDto[] Classes);

/// <summary>The character's skill-bar layout: one entry per slot, "" = empty. Travels BOTH ways —
/// server → client on login (restore), client → server on every rearrangement (persist).
///
/// The bar is CHARACTER data, not a client preference. It used to live in the WPF client's
/// client-settings.json, which meant it did not follow the account to another machine, and its load
/// raced the first Learned push on login — which is what silently reshuffled the bar. The server now
/// owns it. (It does not USE it: casting is by skill id, not slot. It just stores it.)</summary>
public record SkillBarDto(string[] Slots);

/// <summary>Client -> server: the character's full auto-hunt configuration. The use CONDITION for
/// each skill is inferred server-side (buff→if missing, debuff→if target lacks, attack→on cd). The
/// new roaming fields default to sensible values until a settings window exposes them.</summary>
public record AutoHuntConfigDto(
    bool Enabled,
    int HpPotionPct,
    int MpPotionPct,
    bool AutoBuffPotions,
    AutoSkillDto[] Skills,
    string[] BuffPotionIds,
    int FarmRange = 1000,          // radius the auto-hunt searches (clamped [200, 2000])
    bool StaticSpot = false,       // false = roam (scan follows the char); true = fixed circle at the start
    bool AttackNormal = true,      // engage normal-rank mobs
    bool AttackElite = false,      // engage elites
    bool AttackBoss = false,       // engage bosses
    // The auto-potions POTIONS tab: per-potion on/off + HP% threshold. The auto-hunt drinks the
    // highest-threshold ENABLED heal potion that's ready (so common@80 / uncommon@70 / rare@50 act as
    // fallbacks). Empty/null = fall back to the single HpPotionPct + best-potion behaviour.
    AutoPotionDto[]? HealPotions = null,
    // ----- skill CHAINS (playtest-15 design #1) -----
    // How the next skill is chosen inside a priority group. false = "first available": the scan always
    // restarts at the top of the bar (1-2-1-3-1-4…). true = "cyclic": it carries on from the last one
    // used and only wraps once the rest of the group has had its turn (1-2-3-4-1…).
    bool CyclicOrder = false,
    // HP% below which the auto-HEAL chain takes over from buffs/debuffs/attacks. 0 = never auto-heal,
    // 100 = a dedicated healer that heals on cooldown. Distinct from the auto-POTION thresholds.
    int HealThresholdPct = 70,
    // Only ever attack what the party leader is attacking; with no leader target, wait rather than
    // pick your own. (Ignored when you are not in a party, or you ARE the leader.)
    bool AssistPartyLeader = false,
    // ----- the auto-buff BUFFS tab (BL-04) -----
    // One line per buff FAMILY. Null/empty = this character has never opened the tab, and the server
    // falls back to the old AutoBuffPotions + BuffPotionIds behaviour so an existing save keeps
    // working. A non-empty array REPLACES both of them.
    AutoBuffDto[]? Buffs = null,
    // ----- the MP threshold (BL-67) -----
    // MP% below which the auto-MPHEAL chain (Restore / Restore Spirit) takes over. 0 = never.
    // His worked case is "50% MP_treshold + 30% HP_treshold": Restore Spirit at MP<=50, and when that
    // has spent enough HP to cross 30, Vampiric Bolt heals it back.
    // ⚠ Defaults to 60, NOT 0 — 60 is the constant this replaced, so a save written before the field
    // existed keeps the exact behaviour it had instead of silently losing its mana chain.
    int MpThresholdPct = 60);

/// <summary>One line in the auto-potions Potions tab: which potion item, whether it's armed, and the
/// HP (or MP) percent below which to drink it.</summary>
public record AutoPotionDto(string ItemId, bool Enabled, int ThresholdPct);

/// <summary>One line in the auto-potions BUFFS tab: a buff FAMILY, which shapes of it may be spent,
/// and the most expensive rarity the autopilot is allowed to open.
///
/// <para>A family, not an item, is the unit because a family is what can be UP — the whole ladder
/// applies one buff under one key, so "keep Bulwark up" is a single question with a list of possible
/// answers. Listing items instead (the old <c>BuffPotionIds</c>) could not express "use the cheap one
/// unless I say otherwise", which is the entire point of the cap.</para></summary>
public record AutoBuffDto(string Family, bool Potion, bool Scroll, ItemRarity MaxRarity);

/// <summary>Client -> server: one line of a stat-swap basket — a pair and how many further rungs to
/// buy into it. Sent as a whole basket so the purchase is all-or-nothing (see BuyStatSwapsCmd): the
/// Stats tab prices nine rungs at once, and a half-charged basket would leave a build the player never
/// chose and cannot undo without a trip to the Mindwriter.</summary>
public record StatSwapPurchaseDto(string SkillId, int Rungs);

/// <summary>Server -> owning client: you just crossed into a named region. Shown as transient
/// centre-screen text that fades. MinLevel/MaxLevel are the derived band (0/0 = a peaceful area or a
/// town — no band shown). Replaces the always-on zone label (owner: the HUD carries no permanent
/// place text).</summary>
public record RegionNotice(string Name, int MinLevel, int MaxLevel);

/// <summary>One row of a leaderboard: rank position, character, the ranked metric value, and the reward
/// title the #1 in that category wears, as display text (empty for everyone else). Its colour is not
/// sent — the row knows its own category, so the client reads it from
/// <see cref="TitleCatalog.ColorHex"/>. Value's meaning depends on the category (gold, kills, seconds
/// online, or level).</summary>
public record LeaderboardEntry(int Rank, string Name, int Level, long Value, string Title);

/// <summary>Server -> client (request/response): a ranked board for one <see cref="Leaderboards"/>
/// category — the top N characters by that metric.</summary>
public record LeaderboardDto(string Category, IReadOnlyList<LeaderboardEntry> Entries);

/// <summary>The leaderboard categories + their labels and the honorary title the #1 in each earns.
/// Category ids are append-only strings, like skill ids.</summary>
public static class Leaderboards
{
    public static readonly string[] Categories = { "level", "gold", "pvp", "pk", "online", "charisma" };

    public static string Label(string cat) => cat switch
    {
        "level"  => "Level",
        "gold"   => "Wealth",
        "pvp"    => "PvP Kills",
        "pk"     => "Player Kills",
        "online" => "Time Played",
        "charisma" => "Charisma",
        _        => cat,
    };

    public static bool IsCategory(string? cat) =>
        cat is not null && Array.IndexOf(Categories, cat) >= 0;

    /// <summary>The honorary title the rank-1 character in this category earns.
    ///
    /// ⚠ No leading "the" (owner, C16 — "not <i>the Devouted</i>"). A title is drawn on its own line
    /// above the name, not read as a sentence after it, so the article was doing nothing but making the
    /// short ones ("the Feared") sit off-centre and read as a caption.</summary>
    public static string TopTitle(string cat) => cat switch
    {
        "level"  => "Ascended",
        "gold"   => "Wealthy",
        "pvp"    => "Warlord",
        "pk"     => "Feared",
        "online" => "Devoted",
        "charisma" => "Beloved",
        _        => "",
    };
}

/// <summary>
/// Titles: the words, the colours, and the rules about who may write their own.
///
/// ⚠ **A title on the wire is TEXT plus a COLOUR, never an id** (owner, 2026-08-07). The ids below
/// exist only for the GRANTS a character holds — "you top the Wealth board", "you are staff" — and are
/// resolved to text+colour the moment one is worn. That split is the whole point: a granted title and a
/// player-written one (`/title asdf`) arrive at the nameplate as exactly the same pair of fields, so
/// the drawing code does not know or care which is which, and free titles needed no new plumbing.
///
/// What the ids are still for:
///   • the picker lists what you HOLD, and holding is a live fact (rank 1 of a board, or a staff role);
///   • losing the board has to take the title back off, which needs to know where it came from.
///
/// The words granted by the ranking and staff systems are RESERVED — <see cref="IsReserved"/> — so a
/// player with writing rights cannot simply type "Warlord" and wear a rank he never earned. That is the
/// one rule that makes an earned title mean anything once free text exists.
/// </summary>
public static class TitleCatalog
{
    // ─── THE FOUR STAFF TITLES ───────────────────────────────────────────────────────────────
    // The RANKS are plain words (Owner/Admin/Moderator/Chat Moderator — see AccountRole), and the
    // TITLES are the fantasy ones. That split is his ruling from playtest 26: a moderation message has
    // to be unambiguous about who has authority ("jailed by Moderator Kaido"), while the words floating
    // over a head are part of the world. Both layers already existed; only the words are new.
    /// <summary>Held by <see cref="AccountRole.Owner"/>. Append-only string, like a skill id.</summary>
    public const string Owner = "staff_owner";
    /// <summary>Held by <see cref="AccountRole.Admin"/>. Append-only string, like a skill id.</summary>
    public const string Admin = "staff_admin";
    /// <summary>Held by <see cref="AccountRole.Moderator"/>.</summary>
    public const string Moderator = "staff_mod";
    /// <summary>Held by <see cref="AccountRole.ChatModerator"/>.</summary>
    public const string ChatModerator = "staff_chatmod";

    /// <summary>The pseudo-id a CUSTOM title is worn under. Never appears in a picker — it is what
    /// <see cref="TitlesDto.Worn"/> reads when the worn title is one the player wrote.</summary>
    public const string Custom = "custom";

    /// <summary>The title ids this account role holds unconditionally (empty for a player). A rank holds
    /// ONLY its own title, not the ones below it — the Owner wears Supreme Being, not a choice of four.</summary>
    public static string[] ForRole(AccountRole role) => role switch
    {
        AccountRole.Owner         => new[] { Owner },
        AccountRole.Admin         => new[] { Admin },
        AccountRole.Moderator     => new[] { Moderator },
        AccountRole.ChatModerator => new[] { ChatModerator },
        _                         => Array.Empty<string>(),
    };

    public static bool IsStaffTitle(string id) =>
        id == Owner || id == Admin || id == Moderator || id == ChatModerator;

    /// <summary>True if this id names a GRANTED title — a board category or a staff title. The server
    /// validates a wear request against this, so an unknown id can never reach a plate.</summary>
    public static bool IsTitle(string? id) => Leaderboards.IsCategory(id) || (id is not null && IsStaffTitle(id));

    /// <summary>Display text for a granted title id.</summary>
    public static string Text(string id) => id switch
    {
        // The fantasy half of his split ruling: these four strings are the ONLY place a rank wears a
        // fantasy word — the RANK is still called Admin and Moderator in every system message, log
        // line and `/role` argument.
        //
        // ⚠ REWRITTEN 2026-08-28 as one descending ladder (owner): *"titles to read: supreme
        // being(owner) -> god(admin) -> demi god(mod) -> warden(chat mod) -> player"*. Two changed:
        //   • Moderator was "Sentinel" → **Demi God**, because `BL-100` gave the elf archer 3rd class
        //     the name Sentinel and a plate whose whole job is "this person is staff" cannot also be
        //     a class a hundred players wear. He kept the class and moved the title.
        //   • ChatModerator was "Silencer" → **Warden**, so the four read as one descending order
        //     rather than four unrelated words.
        // 🔑 `Demi God` is safe to use even though `Demigod` was a deleted CLASS (id 98, gone
        // 2026-08-07 with the God layer): nothing resolves a title to a class, and the id stays dead.
        Owner         => "Supreme Being",
        Admin         => "God",
        Moderator     => "Demi God",
        ChatModerator => "Warden",
        _             => Leaderboards.TopTitle(id ?? ""),
    };

    /// <summary>Where the title came from, in words — the picker's "— top of Wealth" line, and the
    /// only place a staff title has to explain that no board is involved.</summary>
    public static string Source(string id) => id switch
    {
        Owner     => "staff",
        Admin     => "staff",
        Moderator => "staff",
        ChatModerator => "staff",
        _         => "top of " + Leaderboards.Label(id ?? ""),
    };

    /// <summary>
    /// The colour a GRANTED title is worn in, as an RRGGBB hex with no '#'.
    ///
    /// The owner's palette: gold board golden, time-played green, PvP purplish, PK dark red. The PvP
    /// purple is deliberately DEEPER than the PvP-flag name colour (#CC80FF) — a flagged player's name
    /// is already purple, and matching it would turn a two-line plate into one purple blob. Level and
    /// Charisma were not named: sky (the ladder everyone climbs) and rose (the social board).
    /// </summary>
    public static string ColorHex(string id) => id switch
    {
        "gold"     => "FFCC33",   // golden
        "online"   => "5FD65F",   // green
        "pvp"      => "8A4DD6",   // purplish, NOT the flag's #CC80FF
        "pk"       => "8C1F26",   // dark red
        "level"    => "5FC8FF",   // sky
        "charisma" => "FF8FC4",   // rose
        Owner         => "FFD24A",   // the one gold above the gold board's — there is exactly one
        Admin         => "FF5555",   // staff, loud on purpose
        Moderator     => "4FC3F7",
        ChatModerator => "6FD3B0",   // cooler and quieter than a Sentinel's blue: less authority, on purpose
        _          => DefaultHex,
    };

    /// <summary>What a title with no colour of its own is drawn in — the BOARD titles' fallback.</summary>
    public const string DefaultHex = "F2D473";

    /// <summary>What a title you WROTE starts as. White, not gold (owner, playtest-20 `59r`: *"/title
    /// must default to white"*) — gold is the fallback the earned board titles wear, so a title anyone
    /// can type must not arrive already dressed in it. Colour is the thing the rune buys.</summary>
    public const string CustomDefaultHex = "FFFFFF";

    /// <summary>An NPC's ROLE line ("Elder" over Marius). Deliberately a cool grey-blue and NOT one of
    /// the player-title colours: an NPC's role is furniture you read once, not an achievement, and it
    /// must not compete with the six board colours standing next to it in a busy town square.</summary>
    public const string NpcHex = "9FB6C9";

    /// <summary>The RANK line over a creature (owner, 2026-08-12): elites and bosses wear what they are
    /// on the title line instead of carrying it inside the name — *"put on the valley treant `Field
    /// Boss` [Aqua] and on the dungeon one `Dungeon Boss` [Orange] and on elite mobs `Elite` [red] and
    /// remove the Elite part of their names"*.
    ///
    /// <para>These are the one place a title's colour means DANGER rather than identity, so they are
    /// loud where <see cref="NpcHex"/> is deliberately quiet — you should read "Dungeon Boss" from
    /// across a room. They are kept out of the custom <see cref="Palette"/> for the same reason the
    /// board colours are: a player must not be able to type themselves a name that reads as a boss.</para></summary>
    public const string EliteHex       = "FF6B6B";   // red
    public const string FieldBossHex   = "4FE0E8";   // aqua
    public const string DungeonBossHex = "FFA94D";   // orange

    // ----- custom titles -------------------------------------------------------------------------

    /// <summary>Colours a player may choose for a title they wrote. A NAMED palette rather than free
    /// hex: it keeps garbage off the wire, and it stops a custom title from being typed in the exact
    /// dark red the PK board uses — the reserved WORDS would be protected while the look was not.</summary>
    public static readonly (string Name, string Hex)[] Palette =
    {
        ("white",  "FFFFFF"), ("gold",   DefaultHex), ("amber",  "FFA94D"),
        ("green",  "7BD97B"), ("teal",   "4FD1C5"),   ("sky",    "6FC9FF"),
        ("blue",   "7C9CFF"), ("violet", "B98CFF"),   ("rose",   "FF9BC4"),
        ("crimson","FF6B6B"), ("silver", "C9D1D9"),
    };

    /// <summary>Resolve a palette NAME to its hex. Returns false for anything not in the palette —
    /// including raw hex, deliberately.</summary>
    public static bool TryPaletteColor(string? name, out string hex)
    {
        hex = DefaultHex;
        if (string.IsNullOrWhiteSpace(name)) return false;
        foreach (var (n, h) in Palette)
            if (string.Equals(n, name.Trim(), StringComparison.OrdinalIgnoreCase)) { hex = h; return true; }
        return false;
    }

    public static string PaletteNames() => string.Join(", ", Array.ConvertAll(Palette, p => p.Name));

    /// <summary>The longest a player-written title may be. A nameplate line is ~200px on a phone; past
    /// this the title starts painting over the neighbouring plates it is supposed to sit above.</summary>
    public const int MaxCustomLength = 20;

    /// <summary>Words the ranking and staff systems own. A custom title may not BE one of these, or the
    /// boards stop meaning anything — the entire value of "Warlord" is that it cannot be typed.</summary>
    public static bool IsReserved(string text)
    {
        string t = (text ?? "").Trim();
        if (t.Length == 0) return false;
        foreach (var cat in Leaderboards.Categories)
            if (string.Equals(Leaderboards.TopTitle(cat), t, StringComparison.OrdinalIgnoreCase)) return true;
        // Every staff word is reserved, so nobody with writing rights can type themselves a rank.
        foreach (var staff in new[] { Owner, Admin, Moderator, ChatModerator })
            if (string.Equals(Text(staff), t, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>Validate a player-written title. <paramref name="reason"/> is a sentence to show the
    /// player when it fails. The server is the authority; the client calls this only to fail fast.</summary>
    public static bool IsValidCustom(string? text, out string reason)
    {
        string t = (text ?? "").Trim();
        if (t.Length == 0)         { reason = "A title needs some text."; return false; }
        if (t.Length > MaxCustomLength)
        { reason = $"Too long — {MaxCustomLength} characters at most."; return false; }
        if (IsReserved(t))
        { reason = $"\"{t}\" is a rank title — it is earned on a board, not written."; return false; }

        foreach (char c in t)
        {
            // Letters, digits, space, apostrophe and hyphen. Everything else is refused: rich-text
            // markup would let a title recolour or resize itself past every rule here (TMP reads
            // "<color=...>" out of any label), and control characters break the plate's one line.
            if (char.IsLetterOrDigit(c) || c == ' ' || c == '\'' || c == '-') continue;
            reason = "Letters, numbers, spaces, ' and - only.";
            return false;
        }
        reason = "";
        return true;
    }

    // ----- NPC role lines ------------------------------------------------------------------------

    /// <summary>
    /// Split an NPC's authored name into its ROLE and its personal name: "Elder Marius" → ("Elder",
    /// "Marius"), so the plate can read `Elder` over `Marius` instead of one long run-on (owner).
    ///
    /// Splits at the LAST space, not the first: the roles in this world are not all one word ("High
    /// Priest Oren", "Spirit Helper Nyra", "Class Master Vael"), while the personal names all are. A
    /// name with no space has no role and is returned whole.
    ///
    /// ⚠ NPCs only. A MOB's name is not "&lt;role&gt; &lt;name&gt;" — splitting "Ridgeback Pup" would
    /// invent a creature called Pup.
    /// </summary>
    public static (string Role, string Name) SplitNpcName(string fullName)
    {
        string full = (fullName ?? "").Trim();
        int space = full.LastIndexOf(' ');
        return space <= 0 ? ("", full) : (full.Substring(0, space), full.Substring(space + 1));
    }
}

/// <summary>
/// Server -> owning client: the titles this character may WEAR, and which one is worn.
///
/// A title is HELD, not owned: you hold it for as long as you are rank 1 of that board, and the server
/// re-reads the boards every few minutes. That is deliberately different from an achievement — "the
/// Wealthy" that stays on a player who has since been out-earned says the opposite of what the board
/// says, and the whole point of the thing is to advertise the board.
///
/// <paramref name="Held"/> and <paramref name="Worn"/> are GRANT ids (append-only, like skill ids) —
/// board categories, plus the STAFF titles a role grants. They name what you may wear, not what is
/// drawn: the words and the colour that actually reach a plate travel on <see cref="EntityDto"/> as
/// text. Worn = "" means none, and <see cref="TitleCatalog.Custom"/> means the worn one is written.
///
/// <paramref name="MayWrite"/> is the per-character right to set a title of your own (`/title`), off
/// by default and granted by staff — the hook the owner asked for, so that "do something, earn the
/// right to name yourself" has somewhere to land. <paramref name="CustomText"/> /
/// <paramref name="CustomColor"/> carry what they last wrote, so the picker can offer it back after
/// they try a board title and want their own name again.
/// </summary>
public record TitlesDto(string[] Held, string Worn,
                        bool MayWrite = false, string CustomText = "", string CustomColor = "");

/// <summary>The pseudo skill-id for "basic attack" as an opt-in auto action: put it in
/// <see cref="AutoHuntConfigDto.Skills"/> (enabled) and the auto-hunt will melee when no real skill
/// is ready; leave it out/disabled and the character only casts skills (mage style).</summary>
public static class AutoHuntIds
{
    public const string BasicAttack = "basic_attack";
}

/// <summary>Server -> client HUD: an enabled auto-skill's effective reuse and its MP/s draw.</summary>
public record AutoSkillReuse(string SkillId, string Name, float ReuseSeconds, float MpPerSec);

/// <summary>Server -> client HUD: total MP/s of all enabled auto-skills (after cost/CD-reduction
/// buffs) + the per-skill breakdown, refreshed as buffs change.</summary>
/// <summary>Server -> client auto-hunt state. FarmCenterX/Y is where the STATIC farm circle is
/// anchored — the server owns that anchor, and without it on the wire the client drew the range ring
/// around the CHARACTER, so "keep position" showed a circle that walked off with you instead of
/// marking the spot you were held to (playtest-13). Server-to-client only, so it never round-trips
/// back as part of the config the client saves.</summary>
/// <para><paramref name="IdleSecondsLeft"/> / <paramref name="OfflineSecondsLeft"/> are the two
/// runtime budgets left on the clock (online idle 8h, offline 2h by default), so the client can
/// count the Auto button down instead of the session simply stopping one day with no warning.
/// <c>-1</c> = uncapped (the owner sets a cap of 0 to leave a character farming overnight).
/// New fields with defaults: an older client just ignores them — see GameConstants.ProtocolVersion,
/// where DTO fields are explicitly NOT a protocol break, unlike a hub signature.</para>
public record AutoHuntStatus(bool Enabled, float MpPerSec, AutoSkillReuse[] Skills,
    float FarmCenterX = 0f, float FarmCenterY = 0f,
    int IdleSecondsLeft = -1, int OfflineSecondsLeft = -1);

/// <summary>Server -> client: what the AUTOPILOT is currently on. null = it has nothing.
///
/// The autopilot has always picked a target server-side (CombatTargetId) and never told the client,
/// so while auto-hunting the target window sat empty or stale and you could not see what it had
/// chosen — which also made the targeting RULE impossible to judge (playtest-15). Sent only when the
/// choice CHANGES, like the Cooldowns push: a few messages per fight, not one per tick.</summary>
public record AutoTargetUpdate(Guid? TargetId);

/// <summary>Server -> client: the result of an exit/logout request. Ok=false when blocked (e.g.
/// in combat); the client keeps playing and shows Reason. Ok=true → the client may close.</summary>
public record LogoutResult(bool Ok, string Reason);

/// <summary>Server -> client: the player's PvP toggles + reputation (karma / kill counts) for the HUD.</summary>
public record PvpState(bool Pvp, bool CounterAttack, int Karma = 0, int PkCount = 0, int PvpCount = 0);

/// <summary>Admin-only live-tuning knobs (Debug settings panel). Runtime only — the final values get
/// moved back into the code defaults. Round-trips both ways (server sends current, client applies).</summary>
public record DebugConfigDto(
    // ⚠ `DropAmountRate` was REMOVED from this record (2026-08-18, positional — the client's send order
    // moved with it). Two drop boxes read as two rate knobs that both had to be raised, and raising both
    // squares the multiplier on everything stackable. There is one drop rate now; stack size is not a
    // rate and lives at `/droprate amount`.
    float ExpRate, float SpRate, float DropChanceRate, float GoldRate,
    int KarmaBase, float KarmaConsecGrowth, float KarmaLevelGrowth, int KarmaLossPerDeath, int KarmaLossPerMob,
    int IdleCapSeconds, int OfflineCapSeconds, int GraceSeconds,
    // Test skills: the two debug damage skills read Flat=TestSkillPower, Mod=TestSkillMod. Lets the owner
    // read the {Flat, Mod} damage curve live before authoring real skills. (`TestHealPower` was dropped
    // with the test heal, 2026-08-12, `BL-37` — a positional record, so the client's send order moved too.)
    int TestSkillPower = 0, float TestSkillMod = 1f,
    // Regen: the CADENCE (seconds between natural-regen ticks; 3 = IG's period) and how steeply the
    // stat weights it (per-point multiplier — 1.03 is IG's CON curve, 1.0 = stat does nothing).
    // Changing the cadence does NOT change healing speed, only its chunkiness.
    float RegenIntervalSeconds = 3f, float ConRegenBase = 1.03f,
    // Mob regen is a FRACTION OF THE MOB'S OWN POOL per second, not the CON curve (see
    // StatCalculator.MobHpRegenPerSecond). No level term, so neither number ever needs revisiting
    // when the level range grows. IN COMBAT reads as a maximum kill time (0.001 = you must finish
    // inside ~16 minutes); IDLE reads as time-to-full (0.05 = 20 seconds).
    float MobHpRegenPctCombat = 0.001f, float MobRegenPctIdle = 0.05f,
    // `BL-118` — CLASS CHANGE WITHOUT THE QUEST. 0 = off, anything else = on. A 0/1 float rather than
    // a bool because the panel's Tune tab is a grid of numeric fields with one round-trip, and a
    // single odd-shaped control there would be its own little machine to keep in step. Appended LAST:
    // the record is POSITIONAL and the client sends it in order.
    float FreeClassChange = 0f,
    // `BL-126` — ANYONE MAY `/buff` THEMSELVES. Same 0/1 shape and the same reason as the field above;
    // appended LAST because the record is positional and the client sends it in order.
    float FreeBuffs = 0f);

/// <summary>One member row in the party window. Debuffs = the names of the debuffs currently on this
/// member, so a healer sees at a glance who to cleanse without selecting each one.</summary>
public record PartyMemberDto(Guid Id, string Name, int Level, string ClassName,
    int Hp, int MaxHp, int Mp, int MaxMp, bool IsLeader,
    PartyMemberStatus Status = PartyMemberStatus.Online,
    string[]? Debuffs = null,
    // Positive buff NAMES (appended) — so the party window can show who has what up, behind a
    // buffs/debuffs view toggle. Internal counters (DoT stacks) are excluded like Debuffs.
    string[]? Buffs = null);

/// <summary>Server -> party members: the current roster (empty array = you left/were the last
/// member, so the client hides the party window). Sent on membership change and refreshed
/// periodically for live HP/MP bars.</summary>
public record PartyUpdate(PartyMemberDto[] Members, LootMode LootMode = LootMode.Random);


// ----- Accounts & character selection (Phase 5) ----------------------------

/// <summary>
/// Client -> Server: register or login.
///
/// <paramref name="Protocol"/> is the wire contract the client speaks
/// (<see cref="GameConstants.ProtocolVersion"/>), and it lives HERE rather than as an extra hub
/// parameter for one hard-won reason: **SignalR does NOT bind by arity.** A hub method's default
/// parameter value does not make an omitted argument legal — the dispatcher requires the argument
/// count to match, and an older client calling the shorter overload gets "Failed to invoke 'Login'
/// due to an error on the server" on every attempt. (That is exactly what happened: a client one
/// build old could reconnect its socket but never re-authenticate, so it sat connected and frozen.)
///
/// A DTO field has none of that problem. An old client simply omits it from the JSON, the
/// deserializer leaves it 0, and 0 is the documented "too old to say" value that falls back to the
/// legacy build-label list. Extending a DTO is the backwards-compatible move; extending a hub
/// signature is not.
/// </summary>
public record AuthRequest(string Username, string Password, int Protocol = 0);

/// <summary>Server -> Client: auth outcome. Token is the account id used for
/// subsequent character calls within this connection.</summary>
/// <summary>Server -> admin client: another player's bag (for /bag), or the admin's own bag when it is
/// the /give picker. <paramref name="OwnerName"/> is always the character the action TARGETS.</summary>
public record AdminBagDto(string OwnerName, long Gold, InventoryItemDto[] Items);

/// <summary>Server -> owning client: everything about YOUR OWN state that the world does not draw
/// by itself (`BL-82`). Two families live here for one reason — both are states you are IN, and both
/// were previously announced by a single chat line that had scrolled away by the time you wondered.
///
/// <para><b>Staff states</b> (<paramref name="Role"/>, <paramref name="GodMode"/>, the three forced
/// speeds): the owner's *"Add a flag for admin to see that he is in god/invis ... but now i cannot
/// see nothing."* God mode and a forced speed change nothing you can look at, so the only way to
/// recall whether god was on was to type <c>/god</c> again and read which way it toggled.</para>
///
/// <para><b>Visibility</b> (<paramref name="Invisible"/>, <paramref name="Hidden"/>,
/// <paramref name="Stealthed"/>): the three kinds of `BL-69`, split out rather than merged into one
/// "how see-through am I" number, because they do not mean the same thing and the client draws them
/// differently — his rule is *"the players in shtealt will see themselves with opacity to 0.7 and in
/// invis 0.4"*. <c>Stealthed</c> is the buff-carried kind (Prowl / Conceal / Shrouding Hymn) and is
/// sent as the server's own cached answer rather than a list of skill ids, so a stealth buff added
/// later fades you with no client change.</para>
///
/// <para>🔑 <b>This describes the RECIPIENT and nobody else, and that is load-bearing.</b> His
/// sentence ends *"(for them selves only - for others stealth does nothing, invis vanishes them)"* —
/// an observer must learn nothing from another player's stealth. There is no wire shape here that
/// could leak it: this push goes to one connection and speaks only about that connection's own
/// character. The observer half needs no field at all, because it is already true server-side — a
/// hidden entity is an OMISSION from the snapshot (see <c>GameLoopService.CanSee</c>), never a flag
/// the client is trusted to honour.</para>
///
/// <para>Pushed on CHANGE, from the tick loop, rather than from each command that could cause one.
/// Hide ends by expiry, by damage, by acting, by a flare, by death and by <c>/invis</c> being typed
/// again — enumerating those call sites is how one gets missed and the fade sticks on a visible
/// character.</para></summary>
public record SelfStateDto(
    AccountRole Role, bool GodMode, float? CastSpeed, float? AttackSpeed, float? MoveSpeed,
    bool Invisible = false, bool Hidden = false, bool Stealthed = false);

/// <summary>Account login/register result. Carries no staff role: authorization now belongs to the
/// CHARACTER (see <see cref="LoginResult.Role"/>), so logging in proves identity only.</summary>
public record AuthResponse(bool Success, string? Error, AccountRole Role = AccountRole.Player);

/// <summary>One character on the account, for the selection screen. PendingDeleteAt
/// (UTC) is set when the character is scheduled for deletion; null = active.
///
/// <para><paramref name="OfflineSecondsLeft"/>: null = not offline-farming (the normal case), -1 =
/// farming with no time limit, >= 0 = seconds of offline budget left. The character screen is the
/// ONLY place this can be seen — an offline farmer has no connection and no UI to push it to.</para></summary>
public record CharacterSlot(int Id, string Name, Race Race, BaseClass BaseClass, int SecondClass,
    int Level, DateTime? PendingDeleteAt = null, int ThirdClass = 0, int? OfflineSecondsLeft = null,
    int FourthClass = 0);

/// <summary>Server -> Client: the account's characters.</summary>
public record CharacterList(CharacterSlot[] Characters);

/// <summary>Client -> Server: create a new character on the account.</summary>
public record CreateCharacterRequest(string Name, Race Race, BaseClass BaseClass);

/// <summary>Client -> Server: enter the world with one of the account's characters.</summary>
public record EnterWorldRequest(int CharacterId);


// ----- Quests (Phase 7) ----------------------------------------------------

/// <summary>Client -> server: talk to an NPC (open dialog).</summary>
public record TalkToNpcRequest(Guid NpcEntityId);

/// <summary>Client -> server: accept / complete / change-class actions.</summary>
public record QuestActionRequest(string Action, string Id, Guid NpcEntityId);

/// <summary>One quest line in an NPC dialog or the quest log. <see cref="Location"/>
/// is a short "who/where" hint for the current step (e.g. "Elder Marius — Brackenford"
/// or "Grey Wolf — near Brackenford (Lv 1-10)"); "" when there's nothing useful to say.</summary>
public record QuestSummary(string Id, string Name, string Description, string CurrentStepText,
    int StepIndex, int StepCount, int Counter, int CounterNeeded, bool Completed, bool CanComplete,
    string Location = "", bool Tracked = false);

/// <summary>A class-change option shown by a class-change NPC. Description is a
/// "what this class does" blurb so the player can choose before committing.</summary>
public record ClassChangeOption(int SecondClassId, string ClassName, bool Meets,
    string[] RequiredItemNames, bool[] HasItem, string Description = "");

/// <summary>One buyable line in a vendor shop.</summary>
public record ShopItemDto(string DefId, string Name, int BuyPrice);

/// <summary>A vendor's wares, attached to the dialog when talking to a vendor.</summary>
public record ShopInfo(string Title, ShopItemDto[] Items);

/// <summary>One entry in the buy-back list: an item you recently SOLD, re-buyable for what you got for it.
/// Index is the entry's position in the list (the client passes it back to re-buy).</summary>
public record BuyBackEntryDto(int Index, string DefId, string Name, int Quantity, int Enchant, long UnitPrice);

/// <summary>The character's current buy-back list (recently-sold items). Sent when a shop opens and after
/// every sell / buy-back. In-memory only — it does not survive logout.</summary>
public record BuyBackUpdate(BuyBackEntryDto[] Items);

/// <summary>Server -> one player: the recently BINNED items, restorable for free (C18). Same row shape
/// as the buy-back list — <c>UnitPrice</c> is always 0 — but its own message, because it is its own
/// list with its own cap and it is reachable in the FIELD rather than at a vendor.</summary>
public record RestoreUpdate(BuyBackEntryDto[] Items);

/// <summary>One teleport destination offered by a gatekeeper.
///
/// <paramref name="DestId"/> is EITHER a city's safe-zone id OR a named field gate's id
/// (<see cref="TeleportPoint"/>) — a gatekeeper now sends you to a specific camp doorstep, not just to
/// another town (owner: *"a city gatekeeper should list all the owned fields and their teleporting
/// points, removing the random teleporting factor"*). It was called ZoneId while towns were the only
/// possible destination.
///
/// MinLevel/MaxLevel are the level band you are travelling TO (0/0 = unknown), and
/// <paramref name="Group"/> is the field a gate belongs to (empty for a city), so the client can list
/// gates under their field instead of as a flat wall of names.</summary>
public record TeleportDest(string DestId, string Name, int Fee, int MinLevel = 0, int MaxLevel = 0,
                           string Description = "", string Group = "");

/// <summary>A gatekeeper's destinations, attached to the dialog.</summary>
public record TeleportInfo(TeleportDest[] Destinations);

/// <summary>Server -> client: the dialog when talking to an NPC.</summary>
public record NpcDialog(
    string NpcName,
    string NpcRole,
    QuestSummary[] Offered,      // quests this NPC can give now
    QuestSummary[] Turnable,     // active quests ready to complete here
    QuestSummary[] InProgress,   // active quests not yet complete
    ClassChangeOption[] ClassChanges,
    ShopInfo? Shop = null,       // vendor wares (null for non-vendors)
    TeleportInfo? Teleport = null, // gatekeeper destinations (null for non-gatekeepers)
    SkillResetInfo? SkillReset = null, // un-learnable skills (null for non-reset NPCs)
    BufferInfo? Buffer = null, // buffer options (null for non-buffers)
    bool Warehouse = false, // true for a Warehouse Keeper — the client shows an "Open Warehouse" button
    CraftMasterInfo? CraftMaster = null, // crafting master options (null for everyone else)
    SpExchangeInfo? SpExchange = null); // SP broker (null for everyone else) — APPENDED, old clients ignore it

/// <summary>Server -> client: the SP BROKER's one trade (owner, 2026-08-26) — *"an npc to take your
/// 1kkk SP + 100kk gold and give you a tradable/sellabel SP bottle"*.
///
/// <para>Everything the button needs is here, including what the player currently HAS, so the client
/// can grey it out and say why without a second round trip. The server re-checks on the command; this
/// is display, not authority.</para></summary>
public record SpExchangeInfo(
    int SpCost,
    long GoldCost,
    int SpGranted,
    int YourSp,
    long YourGold,
    bool CanAfford);

/// <summary>Server -> client: what a CRAFTING MASTER offers this character right now (`BL-05`).
///
/// <para>Four mutually-exclusive states, and the DTO is shaped so the client never has to work out
/// which: <see cref="CanOpenWorkshop"/> (he is your master — craft here), <see cref="CanRejoin"/> (you
/// did his quest before, he will take you back at level 1), <see cref="CanQuit"/> (he is your master and
/// will release you), or none of the three, in which case his joining quest is in the normal Offered
/// list like any other quest.</para>
///
/// <para><see cref="CurrentLevel"/> is the level a quit would DESTROY — sent so the confirmation can
/// spell the loss out in numbers, the way the Mindwriter and the stat basket do, rather than saying
/// "are you sure".</para></summary>
public record CraftMasterInfo(
    int Profession,
    bool CanOpenWorkshop,
    bool CanRejoin,
    bool CanQuit,
    int CurrentLevel);

/// <summary>Server -> client: the skills a reset NPC can un-learn — the permanent, mutually-
/// exclusive picks (the level-40 stat swaps). Removing is FREE, but the gold you spent is NOT
/// refunded; it only frees the group so you can commit again.</summary>
public record SkillResetInfo(ResettableSkill[] Skills);
public record ResettableSkill(string SkillId, string Name, int Level, int GoldSpent);

/// <summary>Server -> client: what the NPC BUFFER offers — a preset, a single blessing from the list,
/// or an HP/MP restore. `BL-150`: the window is 6-75 and the free/paid line is the BUFF, not the
/// player's level (eight free from 6, eleven at 15,000 from 40).</summary>
public record BufferInfo(
    bool CanBuff,           // level within the buffer's 6-75 window
    string Message,         // shown when CanBuff is false (too low / too high)
    // ⚠ DEAD SINCE `BL-150` — always 0. The [Full buff] button was removed on his ruling; the field
    // stays only because this record is positional and the two presets below carry their own costs.
    long FullBuffCost,
    long RestoreCost,       // cost to restore HP+MP right now (0 = free / already full)
    BufferBuff[] Buffs,     // single buffs, each with its own cost
    // ----- `BL-95` PRESETS, appended (an old client ignores them and still sees Full + singles) -----
    // Three buttons that cast a LIST in one press. Full/Mage/Fighter are shipped and identical for
    // everyone; Custom is this CLASS's own saved list (see Subclass.BuffPreset).
    //
    // 🔑 The COUNT travels with each one. The player's question at this window is "how many squares is
    // this about to cost me" — the cap is 20 and the full set is 16 — and the client must never work
    // that out by counting a list it built itself, because only the server knows which ids survived
    // the re-filter on load.
    BufferPreset[]? Presets = null,
    // How many NPC blessings the player is wearing RIGHT NOW, i.e. what pressing [Save] would store.
    // 0 = the Save button is dead, and the client says why rather than saving an empty preset.
    int SavableNow = 0);
/// <summary>One blessing on the buffer's list. <see cref="MinLevel"/> is `BL-150`: 6 for the free
/// eight, 40 for the paid eleven. The client greys out what the player cannot buy yet rather than
/// hiding it — seeing what waits at 40 is the point of showing the whole list.</summary>
public record BufferBuff(string SkillId, string Name, long Cost, int MinLevel = 0);

/// <summary>Server -> client: one preset button at the NPC buffer (`BL-95`). <see cref="Key"/> is the
/// action string the client sends back ("full" / "mage" / "fighter" / "custom") — the client never
/// composes a buff list, it names a preset and the server expands it, so the two can never disagree
/// about what "Mage" means.</summary>
public record BufferPreset(string Key, string Name, int Count, long Cost);

// ----- The quest WINDOW's view (0.43.0) ------------------------------------
//
// QuestSummary above is the DIALOG's view: one line about the step you are on, already formatted by
// the server. The window needs the other thing — every quest this character can ever see, whether it
// is takeable, and what each of its steps was — so the three tabs (active / available / completed) and
// the per-quest detail window can be drawn without the client knowing any quest rules.

/// <summary>Where a quest stands for THIS character, which is also the tab it lands in.
/// <see cref="Available"/> and <see cref="Locked"/> share one tab: a list of what you cannot do yet,
/// with no way to see what you CAN take, is only half an answer.</summary>
public enum QuestAvailability { Available = 0, Active = 1, Completed = 2, Locked = 3 }

/// <summary>One objective line of a quest, structured. Until 0.43.0 a step reached the client only as
/// a pre-formatted sentence — enough for one line in the log, useless for a detail window that shows
/// every step with its own progress and tick.</summary>
public record QuestStepDto(string Text, string Location, int Counter, int Needed,
                           bool Done, bool Current);

/// <summary>One gathering line of a contract: what drops, off what, how many you carry, and what a
/// token is worth (a fraction of that creature's own kill exp+gold — see <c>QuestGather</c>).
/// 0.42.9 folded this into the step TEXT to avoid a protocol bump; this is it structured, as promised.</summary>
public record QuestGatherDto(string ItemName, string MobName, int Held,
                             float DropChance, float RewardModifier);

/// <summary>One quest as the quest WINDOW sees it. <paramref name="Status"/> is the one line that
/// explains the state — "Requires level 20", "Ready to hand in", "Repeatable", "Again tomorrow" —
/// so a locked row never just sits there greyed out without saying why.</summary>
public record QuestEntry(
    string Id, string Name, string Description,
    QuestAvailability State, string Status,
    string GiverName, string GiverLocation,
    int MinLevel, int MaxLevel,
    bool Repeatable, bool Daily, bool CanComplete,
    int StepIndex,
    QuestStepDto[] Steps,
    QuestGatherDto[] Gathers,
    string RewardText,
    bool Tracked = false);

/// <summary>Server -> client: the full quest log. <paramref name="Active"/> and
/// <paramref name="Completed"/> stay as they were (the on-screen tracker reads them);
/// <paramref name="Entries"/> is every quest this character can see, in every state.</summary>
public record QuestLog(QuestSummary[] Active, string[] Completed, QuestEntry[] Entries);

/// <summary>Server -&gt; client: this character's SOCIAL options (playtest-19 M2), as a
/// <see cref="SocialOptions"/> flag set. Pushed on login and after every change, so the Options
/// window draws the server's answer rather than a guess it made when you tapped.</summary>
public record SocialOptionsUpdate(int Options);

/// <summary>Server -&gt; owning client: this character's CRAFTING state. Deliberately TINY — the
/// recipes themselves live in <see cref="RecipeCatalog"/>, which is compiled into the client, so the
/// only things that have to travel are the two the SERVER owns: the one permanent
/// <see cref="Profession"/> (0 = not chosen yet) and the <c>DropOnly</c> recipes this character has
/// unlocked from a blueprint. Everything else the crafting window shows — inputs, costs, level gates,
/// success chance — it computes locally from the same catalog the server crafts from, so the two can
/// never disagree about what a recipe costs.
///
/// Pushed on login and after every change (join a master, craft, learn a recipe, quit, the admin
/// override).
///
/// <para><see cref="Exp"/> is the RAW internal exp (12 points per same-level craft — see
/// <see cref="Crafting.CraftExpPerCraft"/>), not the owner's 0/5/15/30/50/100 display scale, and the
/// client divides. <see cref="BandCap"/> is the highest rung this character's PROGRESSION allows right
/// now (20 → 2, 40+3rd → 4, 76 → 6): sending it rather than recomputing it client-side is what lets the
/// window say *"frozen at L2 until level 40"* without the client having to know the third-class rule.
/// <see cref="Level"/> is already clamped by it, so `Level == BandCap` **is** the frozen state.</para>
///
/// <para>⚠ At a master, <see cref="AtMaster"/> is true and the craft buttons go live. Away from one the
/// same window opens in BROWSE mode — every recipe, every have/need count — with the buttons dead
/// (owner: *"better at NPC — and craft happens with their respected masters"*, softened by the
/// have/need colouring being useful precisely where you decide what to farm).</para></summary>
public record CraftingUpdate(
    int Profession, string[] KnownRecipes,
    int Level = 0, int Exp = 0, int BandCap = 0, bool AtMaster = false);
