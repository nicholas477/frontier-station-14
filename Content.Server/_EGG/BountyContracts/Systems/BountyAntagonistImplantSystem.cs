using Content.Server._EGG.BountyContracts.Objectives.Components;
using Content.Server.Implants;
using Content.Shared._EGG.BountyContracts.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;

namespace Content.Server._EGG.BountyContracts.Systems;

/// <summary>
/// Handles the delivery and management of bounty antagonist implants.
/// When implanted, gives players access to the bounty contract system.
/// </summary>
public sealed class BountyAntagonistImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BountyAntagonistImplantComponent, ComponentInit>(OnImplantInit);
        SubscribeLocalEvent<BountyAntagonistImplantComponent, ComponentRemove>(OnImplantRemove);
    }

    private void OnImplantInit(EntityUid uid, BountyAntagonistImplantComponent component, ComponentInit args)
    {
        // This implant is permanent and cannot be removed by normal means
        if (TryComp<SubdermalImplantComponent>(uid, out var subdermal))
        {
            subdermal.Permanent = true;
        }
    }

    private void OnImplantRemove(EntityUid uid, BountyAntagonistImplantComponent component, ComponentRemove args)
    {
        // Additional cleanup if needed
    }
}
