using Content.Shared._NF.BountyContracts;
using Content.Shared._NF.Clothing.EntitySystems;
using Content.Shared._NF.Pirate;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Shared._EGG.BountyContracts.Antag;

[Virtual, Serializable, NetSerializable]
public abstract partial class SharedAntagBountyContract
{
    public enum BountyState
    {
        Offered,
        Accepted,
        Rejected
    }

    public SharedAntagBountyContract(BountyContract bounty)
    {
        Bounty = bounty;
    }
    public BountyContract Bounty;
    public BountyState State = BountyState.Offered;

    public static BountyContract MakeBountyFromPrototype(uint id, NetEntity entity, AntagBountyPrototype prototype)
    {
        return new BountyContract(
            id,
            BountyContractCategory.Other,
            prototype.Name,
            prototype.Reward,
            entity,
            null,
            null,
            prototype.Description,
            null,
            prototype.EntryUIId
        );
    }
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AntagBountyContractsCartridgeComponent : Component
{
    [AutoNetworkedField, DataField]
    public Dictionary<uint, SharedAntagBountyContract> Contracts = new Dictionary<uint, SharedAntagBountyContract>();

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

    public SharedAntagBountyContract? GetContract(uint id)
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

