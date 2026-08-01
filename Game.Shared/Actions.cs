using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>What an action needs in order to do anything, so the client can grey it out instead of
/// firing a command the server will only reject.</summary>
public enum ActionNeeds
{
    /// <summary>Always available (toggles that act on yourself).</summary>
    Nothing = 0,
    /// <summary>Any target selected.</summary>
    Target = 1,
    /// <summary>A living ENEMY target (mob, or a player in PvP).</summary>
    EnemyTarget = 2,
    /// <summary>Another living PLAYER targeted.</summary>
    PlayerTarget = 3,
}

/// <summary>One built-in action: something you DO that isn't a skill.
///
/// Actions cost nothing, have no cooldown, and are never learned — which is exactly why they can't be
/// SkillDefs. They are placed on the skill bar as "action:&lt;id&gt;" tokens.</summary>
public record ActionDef(
    string Id,
    string Name,
    string Icon,
    string Description,
    ActionNeeds Needs = ActionNeeds.Nothing);

/// <summary>
/// The ACTIONS catalog — the non-skill things a player does constantly and wants one click away:
/// basic attack, sit/stand, run/walk, target closest, trade, party invite, follow, assist.
///
/// They live here rather than as skills because they share none of a skill's machinery (no MP, no cast,
/// no cooldown, no learning, no levels) and would need every one of those concepts special-cased to
/// nothing. They live in Game.Shared rather than the WPF client because the bar they sit on is SERVER
/// state, and the future 2D/Unity client needs the same list.
///
/// Adding one: append an entry here, then handle its id in the client's action dispatcher. The server
/// only has to keep the token — it never interprets it (see SyncSkillBar).
/// </summary>
public static class ActionCatalog
{
    public static readonly ActionDef[] All =
    {
        new(GameConstants.ActionBasicAttack, "Attack", "⚔",
            "Basic-attack your current target.", ActionNeeds.EnemyTarget),

        new(GameConstants.ActionTargetClosest, "Target Closest", "🎯",
            "Select the nearest enemy. Press again to step to the next-nearest, so you can flick "
            + "between the two in front of you. Search radius is set in Settings."),

        new(GameConstants.ActionSitStand, "Sit / Stand", "🪑",
            "Toggle sitting. Sitting boosts regen but you can't move, and standing up after a hit "
            + "takes a moment."),

        new(GameConstants.ActionRunWalk, "Run / Walk", "🏃",
            "Toggle walking. Walking is slower but regenerates faster."),

        new(GameConstants.ActionTradeTarget, "Trade", "🤝",
            "Ask the targeted player to trade.", ActionNeeds.PlayerTarget),

        new(GameConstants.ActionPartyInvite, "Invite to Party", "👥",
            "Invite the targeted player to your party.", ActionNeeds.PlayerTarget),

        new(GameConstants.ActionFollowTarget, "Follow", "👣",
            "Walk after the targeted player until you move or they leave.", ActionNeeds.PlayerTarget),

        new(GameConstants.ActionAssistTarget, "Assist", "🫱",
            "Attack whatever the targeted player is attacking.", ActionNeeds.PlayerTarget),

        // ---- Everything else whose only argument is a NAME (owner, 2026-07-24): the TARGET supplies it,
        //      so nothing has to be typed. Commands that need a real VALUE — a whisper's message, a trade
        //      quantity — stay typed, because a button has no way to provide one. ----

        new(GameConstants.ActionFriendAdd, "Add Friend", "➕",
            "Send the targeted player a friend request. Friendship is MUTUAL: until they add you back "
            + "you are only [pending] and neither of you sees the other's comings and goings.",
            ActionNeeds.PlayerTarget),

        new(GameConstants.ActionFriendRemove, "Remove Friend", "➖",
            "Remove the targeted player from your friend list.", ActionNeeds.PlayerTarget),

        new(GameConstants.ActionFriendList, "Friend List", "📖",
            "Show your friends and which of them are online."),

        new(GameConstants.ActionPartyLeave, "Leave Party", "🚪",
            "Leave your current party."),

        new(GameConstants.ActionPartyKick, "Kick from Party", "👢",
            "Remove the targeted player from the party. Leader only.", ActionNeeds.PlayerTarget),

        new(GameConstants.ActionPartyLeader, "Pass Leadership", "👑",
            "Make the targeted party member the leader. Leader only, and it cannot be undone by you.",
            ActionNeeds.PlayerTarget),

        // The one exception to "a value must be typed": a whisper needs a MESSAGE, so this action
        // cannot send one. What it can do is everything up to the message — the half that is fiddly on
        // a phone — by putting "/w <name> " in the command box with the caret after it.
        new(GameConstants.ActionWhisperTarget, "Whisper", "✉",
            "Start a whisper to the targeted player: the command box is filled in with their name and "
            + "you type the message.", ActionNeeds.PlayerTarget),

        new(GameConstants.ActionLike, "Like", "👍",
            "Give the targeted player +1 charisma from your daily budget (20/day). Their reputation "
            + "grants an exp/sp bonus and ranks on the charisma board.", ActionNeeds.PlayerTarget),

        new(GameConstants.ActionBlock, "Block", "🚫",
            "Ignore the targeted player — you stop seeing their chat (whisper / world / local). "
            + "They are not told.", ActionNeeds.PlayerTarget),

        new(GameConstants.ActionUnblock, "Unblock", "🔊",
            "Stop ignoring the targeted player.", ActionNeeds.PlayerTarget),
    };

    private static readonly Dictionary<string, ActionDef> ById =
        All.ToDictionary(a => a.Id, StringComparer.Ordinal);

    public static ActionDef? Get(string? id) =>
        id is not null && ById.TryGetValue(id, out var a) ? a : null;

    /// <summary>Resolve a bar token ("action:&lt;id&gt;") to its definition, or null if it isn't an
    /// action token or names an action that no longer exists.</summary>
    public static ActionDef? FromToken(string? token) =>
        GameConstants.IsActionSlot(token) ? Get(GameConstants.ActionSlotId(token!)) : null;
}
