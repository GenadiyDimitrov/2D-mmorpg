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
    float Y);

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
    int MaxHp);

/// <summary>Client -> Server: "move me toward this point" (click-to-move).</summary>
public record MoveCommand(float TargetX, float TargetY);

/// <summary>Server -> Client, every tick: everything you can currently see
/// (including yourself). Anything not listed has left your view range.</summary>
public record WorldSnapshot(EntityDto[] Entities);

/// <summary>Server -> Client: a chat line.</summary>
public record ChatMessage(string From, string Text, ChatChannel Channel);
