namespace Game.Shared;

// ---------------------------------------------------------------------------
// Network contracts. These records are serialized by SignalR (System.Text.Json)
// in both directions. Keep them flat and small — they go over the wire 10x/sec.
// ---------------------------------------------------------------------------

/// <summary>Client -> Server: enter the world with a character.</summary>
public record LoginRequest(string CharacterName, Race Race, BaseClass BaseClass);

/// <summary>Server -> Client: result of a login attempt.</summary>
public record LoginResult(
    bool Success,
    string? Error,
    Guid EntityId,
    float X,
    float Y,
    DateTime ServerEpochUtc = default);

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
    bool Dead);

/// <summary>Client -> Server: "move me toward this point" (click-to-move).
/// Moving cancels engagement, queued skills, and casting (classic MMO).</summary>
public record MoveCommand(float TargetX, float TargetY);

/// <summary>Server -> Client, every tick: everything you can currently see
/// (including yourself). Anything not listed has left your view range.</summary>
public record WorldSnapshot(EntityDto[] Entities);

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

/// <summary>Server -> the owning client: exp/level progress after a kill.</summary>
public record ProgressUpdate(
    int Level,
    long Exp,
    long ExpToNext,
    bool LeveledUp);

/// <summary>Server -> the casting client: show/update the cast bar.
/// Seconds &lt;= 0 means the cast was cancelled — hide the bar.</summary>
public record CastInfo(string SkillName, float Seconds);


/// <summary>One item instance in a player's inventory.</summary>
public record InventoryItemDto(Guid InstanceId, string DefId, bool Equipped, int Enchant, int Quantity, ItemAttribute[] Attributes);

/// <summary>Server -> owning client: full inventory sync (sent on change).</summary>
public record InventoryUpdate(InventoryItemDto[] Items);

/// <summary>Server -> client: someone wants to trade with you.</summary>
public record TradeRequestNotice(Guid FromId, string FromName);

/// <summary>Server -> both traders: full trade state (sent on every change).
/// Active=false closes the trade window.</summary>
public record TradeStateUpdate(
    bool Active,
    string PartnerName,
    InventoryItemDto[] MyOffer,
    InventoryItemDto[] TheirOffer,
    bool MyReady,
    bool TheirReady);


/// <summary>Server -> owning client: full derived stats for the Stats window.
/// Sent whenever stats change (level, equip, class change).</summary>
public record StatsUpdate(
    int Con, int Atk, int Wit, int Dex,
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
    float MagicFailResist = 0f, float MagicFailFloor = 0f,
    float CritRateResist = 0f, float CritDmgResist = 0f, float BowResist = 0f,
    int InterruptResist = 0);

/// <summary>Server -> owning client: a potion cooldown started (seconds),
/// or an active potion effect changed. Cooldown 0 = ready.</summary>
public record PotionStatus(float CooldownSeconds, string ActiveEffect);


/// <summary>One active buff/debuff on the player, for the buff bar + tooltip.</summary>
public record BuffDto(string Name, string Description, float SecondsLeft, bool IsDebuff, string Key = "");

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

/// <summary>Server -> owning client: the expanded target window (L2-style inspect) —
/// the target's detailed stats and, for a mob, its passive modifier lines.</summary>
public record TargetDetails(
    Guid Id, string Name, int Level, bool IsMob,
    int Hp, int MaxHp, int Mp, int MaxMp,
    int PAtk, int MAtk, int PDef, int MDef,
    int Accuracy, int Evasion, float CritChance,
    float BowResist, float CritResist,
    string[] Passives);

/// <summary>Server -> owning client: the result of an enchant attempt.</summary>
public record EnchantResultDto(string ItemName, int NewEnchant, string Outcome, bool Destroyed);

/// <summary>Server -> owning client: an attribute reroll finished (inventory update
/// carries the new attributes; this drives the reroll popup refresh + a message).</summary>
public record RerollResultDto(string ItemName, string Outcome);

/// <summary>Server -> owning client: the player's gold wallet balance (sent on entry
/// and whenever it changes — kills, quest rewards, vendor buy/sell, teleport fees).</summary>
public record GoldUpdate(long Gold);


// ----- Accounts & character selection (Phase 5) ----------------------------

/// <summary>Client -> Server: register or login.</summary>
public record AuthRequest(string Username, string Password);

/// <summary>Server -> Client: auth outcome. Token is the account id used for
/// subsequent character calls within this connection.</summary>
public record AuthResponse(bool Success, string? Error, bool IsAdmin);

/// <summary>One character on the account, for the selection screen. PendingDeleteAt
/// (UTC) is set when the character is scheduled for deletion; null = active.</summary>
public record CharacterSlot(int Id, string Name, Race Race, BaseClass BaseClass, int SecondClass,
    int Level, DateTime? PendingDeleteAt = null);

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
    string Location = "");

/// <summary>A class-change option shown by a class-change NPC.</summary>
public record ClassChangeOption(int SecondClassId, string ClassName, bool Meets,
    string[] RequiredItemNames, bool[] HasItem);

/// <summary>One buyable line in a vendor shop.</summary>
public record ShopItemDto(string DefId, string Name, int BuyPrice);

/// <summary>A vendor's wares, attached to the dialog when talking to a vendor.</summary>
public record ShopInfo(string Title, ShopItemDto[] Items);

/// <summary>One teleport destination offered by a gatekeeper. MinLevel/MaxLevel are
/// the level band of the hunting grounds around that town (0/0 = unknown), shown so
/// players know where they're going.</summary>
public record TeleportDest(string ZoneId, string Name, int Fee, int MinLevel = 0, int MaxLevel = 0);

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
    TeleportInfo? Teleport = null); // gatekeeper destinations (null for non-gatekeepers)

/// <summary>Server -> client: the full quest log.</summary>
public record QuestLog(QuestSummary[] Active, string[] Completed);
