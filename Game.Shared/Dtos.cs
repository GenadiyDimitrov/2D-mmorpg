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
    int MagicDefence);

/// <summary>Server -> owning client: a potion cooldown started (seconds),
/// or an active potion effect changed. Cooldown 0 = ready.</summary>
public record PotionStatus(float CooldownSeconds, string ActiveEffect);


/// <summary>One active buff/debuff on the player, for the buff bar + tooltip.</summary>
public record BuffDto(string Name, string Description, float SecondsLeft, bool IsDebuff);

/// <summary>Server -> client: the character's learned skill ids + SP balance.</summary>
public record LearnedSkills(string[] SkillIds, int SkillPoints);

/// <summary>Server -> owning client: the player's current buffs (sent each
/// second while any are active, and once when the last one drops).</summary>
public record BuffUpdate(BuffDto[] Buffs);

/// <summary>Server -> owning client: the result of an enchant attempt.</summary>
public record EnchantResultDto(string ItemName, int NewEnchant, string Outcome, bool Destroyed);


// ----- Accounts & character selection (Phase 5) ----------------------------

/// <summary>Client -> Server: register or login.</summary>
public record AuthRequest(string Username, string Password);

/// <summary>Server -> Client: auth outcome. Token is the account id used for
/// subsequent character calls within this connection.</summary>
public record AuthResponse(bool Success, string? Error, bool IsAdmin);

/// <summary>One character on the account, for the selection screen.</summary>
public record CharacterSlot(int Id, string Name, Race Race, BaseClass BaseClass, int SecondClass, int Level);

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

/// <summary>One quest line in an NPC dialog or the quest log.</summary>
public record QuestSummary(string Id, string Name, string Description, string CurrentStepText,
    int StepIndex, int StepCount, int Counter, int CounterNeeded, bool Completed, bool CanComplete);

/// <summary>A class-change option shown by a class-change NPC.</summary>
public record ClassChangeOption(int SecondClassId, string ClassName, bool Meets,
    string[] RequiredItemNames, bool[] HasItem);

/// <summary>Server -> client: the dialog when talking to an NPC.</summary>
public record NpcDialog(
    string NpcName,
    string NpcRole,
    QuestSummary[] Offered,      // quests this NPC can give now
    QuestSummary[] Turnable,     // active quests ready to complete here
    QuestSummary[] InProgress,   // active quests not yet complete
    ClassChangeOption[] ClassChanges);

/// <summary>Server -> client: the full quest log.</summary>
public record QuestLog(QuestSummary[] Active, string[] Completed);
