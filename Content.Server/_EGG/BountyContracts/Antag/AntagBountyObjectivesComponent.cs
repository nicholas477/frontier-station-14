using Content.Shared.Objectives.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._EGG.BountyContracts.Antag;


/// <summary>
/// Gives antags selected by this rule a fixed list of objectives.
/// </summary>
[RegisterComponent, Access(typeof(EGGBountyContractSystem))]
public sealed partial class AntagBountyObjectivesComponent : Component
{
    /// <summary>
    /// List of static objectives to give.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId<ObjectiveComponent>> Objectives = new();
}
