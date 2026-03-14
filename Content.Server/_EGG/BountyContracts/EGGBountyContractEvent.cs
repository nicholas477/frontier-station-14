using Content.Shared._EGG.BountyContracts.Antag;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.Server._EGG.BountyContracts;

[ByRefEvent]
public readonly record struct DecideAntagBountiesEvent();

[ByRefEvent]
public readonly record struct OnAntagBountyAcceptedEvent(AntagBountyContract InContract, Entity<MindComponent> InMind, Entity<AntagBountyContractsCartridgeComponent> InTarget)
{
    public readonly AntagBountyContract Contract = InContract;

    public readonly Entity<MindComponent> Mind = InMind;

    public readonly Entity<AntagBountyContractsCartridgeComponent> Target = InTarget;
}

[ByRefEvent]
public readonly record struct OnAntagBountyRejectedEvent(AntagBountyContract InContract, Entity<MindComponent> InMind, Entity<AntagBountyContractsCartridgeComponent> InTarget)
{
    public readonly AntagBountyContract Contract = InContract;
    public readonly Entity<MindComponent> Mind = InMind;
    public readonly Entity<AntagBountyContractsCartridgeComponent> Target = InTarget;
}
