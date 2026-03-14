using Content.Shared._EGG.BountyContracts.Antag;
using Robust.Shared.Prototypes;

namespace Content.Server._EGG.BountyContracts;

[ByRefEvent]
public readonly record struct DecideAntagBountiesEvent();

[ByRefEvent]
public readonly record struct OnAntagBountyAcceptedEvent(AntagBountyContract InContract, Entity<AntagBountyContractsCartridgeComponent> InTarget)
{
    public readonly AntagBountyContract Contract = InContract;
    public readonly Entity<AntagBountyContractsCartridgeComponent> Target = InTarget;
}

[ByRefEvent]
public readonly record struct OnAntagBountyRejectedEvent(AntagBountyContract InContract, Entity<AntagBountyContractsCartridgeComponent> InTarget)
{
    public readonly AntagBountyContract Contract = InContract;
    public readonly Entity<AntagBountyContractsCartridgeComponent> Target = InTarget;
}
