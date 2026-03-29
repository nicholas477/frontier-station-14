using Content.Shared._NF.BountyContracts;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.BountyContracts;

public readonly record struct GetBountyContractsEvent(List<BountyContract> Bounties, ProtoId<BountyContractCollectionPrototype>? Collection);
