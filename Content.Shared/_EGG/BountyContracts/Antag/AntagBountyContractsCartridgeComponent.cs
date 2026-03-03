using Content.Shared._NF.BountyContracts;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared._EGG.BountyContracts.Antag;

public sealed partial class AntagBountyContract
{
    public enum BountyState
    {
        Offered,
        Accepted,
        Rejected
    }

    public AntagBountyContract(AntagBountyPrototype prototype, BountyContract bounty)
    {
        Prototype = prototype;
        Bounty = bounty;
    }
    public AntagBountyPrototype Prototype;
    public BountyContract Bounty;
    public BountyState State = BountyState.Offered;
}

[RegisterComponent]
[Access(typeof(SharedEGGBountyContractSystem))]
public sealed partial class AntagBountyContractsCartridgeComponent : Component
{
    public Dictionary<uint, AntagBountyContract> Contracts = new Dictionary<uint, AntagBountyContract>();

    public uint GetNextContractId()
    {
        if (Contracts.Count == 0)
        {
            return 0;
        }
        else
        {
            return Contracts.Last().Key + 1;
        }
    }

    public AntagBountyContract? GetContract(uint id)
    {
        if (Contracts.TryGetValue(id, out var contract))
        {
            return contract;
        }
        else
        {
            return null;
        }
    }
}
