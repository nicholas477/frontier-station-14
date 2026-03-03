using Content.Shared._NF.BountyContracts;
using Content.Shared.CartridgeLoader;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._EGG.BountyContracts;

public enum AntagBountyContractCommand : byte
{
    AcceptBounty = 0,
    RejectBounty = 1
}

[NetSerializable, Serializable]
public sealed class AntagBountyContractCommandMessageEvent(AntagBountyContractCommand command, uint contractId) : CartridgeMessageEvent
{
    public readonly AntagBountyContractCommand Command = command;
    public readonly uint ContractId = contractId;
}

public abstract partial class SharedEGGBountyContractSystem : EntitySystem
{
    protected readonly ProtoId<BountyContractCollectionPrototype> AntagCollection = "Antag";
}
