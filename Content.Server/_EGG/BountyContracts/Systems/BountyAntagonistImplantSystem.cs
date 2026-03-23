using Content.Server._EGG.BountyContracts.Objectives.Components;
using Content.Server.Implants;
using Content.Shared._EGG.BountyContracts.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using NetCord;

namespace Content.Server._EGG.BountyContracts.Systems;

/// <summary>
/// Handles the delivery and management of bounty antagonist implants.
/// When implanted, gives players access to the bounty contract system.
/// </summary>
public sealed class BountyAntagonistImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BountyAntagonistImplantComponent, ImplantImplantedEvent>(OnImplantEvent);
    }

    private void OnImplantEvent(Entity<BountyAntagonistImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (args.Implanted is null)
        {
            return;
        }

        // Get the mind from the implanted entity and add the bounty thief role
        if (_mind.TryGetMind(args.Implanted.Value, out var mindId, out _))
        {
            _roles.MindAddRole(mindId, "MindRoleBountyAntag");
        }
    }
}
