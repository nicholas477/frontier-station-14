using Content.Shared._NF.BountyContracts;
using Robust.Shared.GameStates;
using System.Linq;

namespace Content.Shared._EGG.BountyContracts.Antag;

[RegisterComponent]
[Access(typeof(SharedEGGBountyContractSystem))]
public sealed partial class AntagBountyContractsCartridgeComponent : Component
{
    public Dictionary<uint, BountyContract> Contracts = new Dictionary<uint, BountyContract>();

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
}
