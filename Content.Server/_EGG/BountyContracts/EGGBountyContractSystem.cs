using Content.Server._NF.BountyContracts;
using Content.Server._NF.Radio.Components;
using Content.Shared._NF.BountyContracts;
using Robust.Shared.Prototypes;

namespace Content.Server._EGG.BountyContracts;

public sealed partial class EGGBountyContractSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BountyContractsCartridgeComponent, GetBountyContractsEvent>(OnGetBountyContracts);
    }

    private void OnGetBountyContracts(EntityUid uid, BountyContractsCartridgeComponent component, GetBountyContractsEvent ev)
    {
        Log.Debug("OnGetBountyContracts");

        ProtoId<BountyContractCollectionPrototype> antagCollection = "Antag";

        if (ev.Collection == antagCollection)
        {
            ev.Bounties.Add(new BountyContract(1000, BountyContractCategory.Other, "test", 1000, GetNetEntity(uid), null, null, null, null));
        }
    }
}
