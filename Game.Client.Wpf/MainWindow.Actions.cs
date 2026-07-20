using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Game.Shared;

namespace Game.Client.Wpf;

/// <summary>
/// The built-in ACTIONS (see <see cref="ActionCatalog"/>): the things a player does constantly that
/// aren't skills — basic attack, target closest, sit/stand, run/walk, trade, party invite, follow,
/// assist. They sit on the skill bar as "action:&lt;id&gt;" tokens.
///
/// Every one of these already existed as a button or a keypress somewhere; putting them on the bar just
/// gives them a home the player chooses. Nothing here grants anything the server wouldn't allow anyway —
/// each dispatches the same command its button does.
/// </summary>
public partial class MainWindow
{
    /// <summary>Targets already stepped through by "target closest" this cycle. Cleared when the cycle
    /// is broken (see <see cref="TargetClosest"/>), which is what makes repeated presses flip between
    /// the nearest two rather than sticking on the nearest one.</summary>
    private readonly HashSet<Guid> _targetCycle = new();

    private DateTime _lastTargetCycleAt = DateTime.MinValue;

    private void RunAction(ActionDef action)
    {
        switch (action.Id)
        {
            case GameConstants.ActionBasicAttack:
                if (_targetId is Guid attackId) _ = _net.AttackAsync(attackId);
                else Notify("No target.");
                break;

            case GameConstants.ActionTargetClosest:
                TargetClosest();
                break;

            case GameConstants.ActionSitStand:
                _ = _net.SetMoveStateAsync(
                    _moveState == MoveState.Sitting ? MoveState.Running : MoveState.Sitting);
                break;

            case GameConstants.ActionRunWalk:
                _ = _net.SetMoveStateAsync(
                    _moveState == MoveState.Walking ? MoveState.Running : MoveState.Walking);
                break;

            case GameConstants.ActionTradeTarget:
                if (TargetedPlayer() is Guid tradeId) _ = _net.TradeRequestAsync(tradeId);
                else Notify("Target a player to trade.");
                break;

            case GameConstants.ActionPartyInvite:
                if (TargetedPlayer() is Guid inviteId) _ = _net.PartyInviteAsync(inviteId);
                else Notify("Target a player to invite.");
                break;

            case GameConstants.ActionFollowTarget:
                if (TargetedPlayer() is Guid followId) _ = _net.FollowAsync(followId);
                else Notify("Target a player to follow.");
                break;

            case GameConstants.ActionAssistTarget:
                if (TargetedPlayer() is Guid assistId) _ = _net.AssistAsync(assistId);
                else Notify("Target a player to assist.");
                break;
        }
    }

    private void Notify(string text) =>
        AppendChat(new ChatMessage("SYSTEM", text, ChatChannel.System));

    /// <summary>The current target if it's another LIVING player, else null.</summary>
    private Guid? TargetedPlayer() =>
        _targetId is Guid id && _visuals.TryGetValue(id, out var v)
        && v.Latest is { Kind: EntityKind.Player, Dead: false } && id != _myId
            ? id
            : null;

    /// <summary>Select the nearest attackable enemy within the configured radius; press again to step to
    /// the next-nearest.
    ///
    /// The cycle is a SET of everything already visited, not an index: entities come and go every
    /// snapshot, so an index into a live-sorted list would jump around as things spawn, die and move.
    /// Remembering who you've already been offered is stable under all of that.
    ///
    /// The cycle resets when it runs out of candidates — so with two mobs in front of you, presses go
    /// closest → 2nd → closest → 2nd, which is what the owner actually wants it for. It also resets
    /// after a few seconds of not pressing, so coming back to it later starts from the nearest again
    /// rather than silently continuing an old cycle.</summary>
    private void TargetClosest()
    {
        if (_myDto is null) return;

        if ((DateTime.UtcNow - _lastTargetCycleAt).TotalSeconds > 5)
            _targetCycle.Clear();
        _lastTargetCycleAt = DateTime.UtcNow;

        float range = (float)_settings.TargetSearchRange;
        float rangeSq = range * range;

        var candidates = _visuals
            .Where(kv => kv.Key != _myId)
            .Select(kv => (Id: kv.Key, Dto: kv.Value.Latest))
            .Where(e => e.Dto is not null)
            .Select(e => (e.Id, Dto: e.Dto!))
            // NPCs are never combat targets, and a corpse isn't one either.
            .Where(e => e.Dto.Kind != EntityKind.Npc && !e.Dto.Dead)
            .Select(e => (e.Id, e.Dto, DistSq: DistSq(_myDto.X, _myDto.Y, e.Dto.X, e.Dto.Y)))
            .Where(e => e.DistSq <= rangeSq)
            .OrderBy(e => e.DistSq)
            .ToList();

        if (candidates.Count == 0)
        {
            _targetCycle.Clear();
            Notify($"Nothing within {range:0} units.");
            return;
        }

        // Keep the current target in the cycle, so the first press after clicking something moves ON
        // rather than re-selecting what you already have.
        if (_targetId is Guid current) _targetCycle.Add(current);

        var next = candidates.FirstOrDefault(c => !_targetCycle.Contains(c.Id));
        if (next.Dto is null)
        {
            // Everything in range has been offered — start the cycle again from the nearest.
            _targetCycle.Clear();
            next = candidates[0];
        }

        _targetCycle.Add(next.Id);
        _targetId = next.Id;
        UpdateTargetFrame();
    }

    private static float DistSq(float ax, float ay, float bx, float by)
    {
        float dx = ax - bx, dy = ay - by;
        return dx * dx + dy * dy;
    }

    /// <summary>Point the settings slider at the saved value. Called once when the window loads —
    /// setting Value fires ValueChanged, so this must not run before _settings exists.</summary>
    private void InitTargetRangeSlider()
    {
        TargetRangeSlider.Value = _settings.TargetSearchRange;
        TargetRangeValue.Text = $"{_settings.TargetSearchRange:0}";
    }

    private void TargetRangeSlider_ValueChanged(
        object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_settings is null) return;   // fires during XAML load, before settings are read
        _settings.TargetSearchRange = e.NewValue;
        if (TargetRangeValue is not null) TargetRangeValue.Text = $"{e.NewValue:0}";
        _settings.Save();
    }
}
