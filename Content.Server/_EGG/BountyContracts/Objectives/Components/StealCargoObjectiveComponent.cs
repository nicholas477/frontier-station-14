using Content.Server._EGG.BountyContracts.Objectives.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Server._EGG.BountyContracts.Objectives.Components;

/// <summary>
/// Requires that you steal a certain dollar amount of cargo from a player's ship.
/// Tracks items by detecting when they leave the target player's grid.
/// </summary>
[RegisterComponent, Access(typeof(StealCargoObjectiveSystem))]
public sealed partial class StealCargoObjectiveComponent : Component
{
    /// <summary>
    /// The player whose cargo should be stolen.
    /// </summary>
    [DataField]
    public ICommonSession? PlayerToStealFrom;

    public EntityUid? GetPlayerEntity()
    {
        return PlayerToStealFrom?.AttachedEntity;
    }

    /// <summary>
    /// The target dollar amount of cargo to steal.
    /// </summary>
    [DataField]
    public double TargetStolenValue = 10000.0;

    /// <summary>
    /// The current dollar amount of cargo stolen so far.
    /// </summary>
    [DataField]
    public double CurrentStolenValue = 0.0;

    /// <summary>
    /// Set of entity UIDs that have already been counted as stolen to prevent double-counting.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> StolenItems = new();
}
