using Content.Shared._NF.BountyContracts;
using Content.Shared.Objectives.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._EGG.BountyContracts.Antag;


/// <summary>
///     Collection of objectives
/// </summary>
[Prototype]
public sealed partial class AntagBountyPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public ProtoId<BountyContractCollectionPrototype> Collection;

    [DataField]
    public string Name = "";

    [DataField]
    public string Description = "";

    [DataField]
    public int Reward = 0;


    /// <summary>
    /// List of static objectives to give.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId<ObjectiveComponent>> Objectives = new();
}
