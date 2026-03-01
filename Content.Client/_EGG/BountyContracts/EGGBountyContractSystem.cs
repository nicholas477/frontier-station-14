using Content.Client._NF.BountyContracts;
using Content.Shared._EGG.BountyContracts;

namespace Content.Client._EGG.BountyContracts;

public sealed partial class EGGBountyContractSystem : SharedEGGBountyContractSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetBountyContractUIEvent>(OnGetBountyContractUI);
    }

    private void OnGetBountyContractUI(GetBountyContractUIEvent ev)
    {
        //if ev.Contract.EntryUIId == 
    }
}
