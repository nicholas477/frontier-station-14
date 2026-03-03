using Content.Server._EGG.BountyContracts.Objectives.Systems;


namespace Content.Server._EGG.BountyContracts.Objectives.Components;

/// <summary>
/// Requires that you steal a certain item (or several) on a ship
/// </summary>
[RegisterComponent, Access(typeof(StealCargoObjectiveSystem))]
public sealed partial class StealCargoObjectiveComponent : Component
{

}
