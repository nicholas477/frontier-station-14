using Content.Server._EGG.BountyContracts.Components;
using Content.Server._NF.BountyContracts;
using Content.Server.CartridgeLoader;
using Content.Shared._EGG.BountyContracts;
using Content.Shared._EGG.BountyContracts.Antag;
using Content.Shared._EGG.BountyContracts.Components;
using Content.Shared._NF.Bank;
using Content.Shared._NF.BountyContracts;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server._EGG.BountyContracts;

/// <summary>
/// Currently handles antag bounties
/// </summary>
public sealed partial class EGGBountyContractSystem : SharedEGGBountyContractSystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly BountyContractSystem _bounty = default!;

    private TimeSpan _lastAntagDecisionTime = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagBountyContractsCartridgeComponent, GetBountyContractsEvent>(OnGetBountyContracts);
        SubscribeLocalEvent<AntagBountyContractsCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<AntagBountyPDAComponent, GetAccessTagsEvent>(OnGetAccessTags);

        InitializeCVars();
    }

    private void OnGetAccessTags(Entity<AntagBountyPDAComponent> ent, ref GetAccessTagsEvent args)
    {
        var playerEnt = Transform(ent.Owner).ParentUid;
        if (playerEnt is not { Valid: true })
        {
            return;
        }

        // Check if player has the bounty antagonist implant
        if (TryComp<BountyAntagonistImplantComponent>(playerEnt, out _))
        {
            args.Tags.Add("BountyAntag");
        }

        if (_mind.TryGetMind(playerEnt, out var mindId, out var mindComp))
        {
            if (_roles.MindHasRole<BountyAntagonistRoleComponent>(mindId, out _))
            {
                args.Tags.Add("BountyAntag");
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        if (curTime > (_lastAntagDecisionTime + NextAntagDecisionTimerLength))
        {
            DecideAntagBounties();
            _lastAntagDecisionTime = curTime;
        }
    }

    private void DecideAntagBounties()
    {
        // Let other systems place bounties too
        var ev = new DecideAntagBountiesEvent();
        RaiseLocalEvent(ref ev);

        var query = EntityQueryEnumerator<AntagBountyContractsCartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            RefreshBountyUI((uid, comp));
        }
    }

    public void SendBountyNotification(EntityUid cartridgeUid, string? sender, string msg)
    {
        sender ??= Loc.GetString("bounty-contracts-announcement-pda-name");

        TryComp<CartridgeComponent>(cartridgeUid, out CartridgeComponent? cartComp);
        if (cartComp is null || cartComp.LoaderUid is null)
        {
            return;
        }

        TryComp<CartridgeLoaderComponent>(cartComp.LoaderUid, out CartridgeLoaderComponent? cartLoaderComp);
        if (cartLoaderComp is null)
        {
            return;
        }

        if (_cartridgeLoader.TryGetProgram<BountyContractsCartridgeComponent>(cartComp.LoaderUid.Value, out _, out var bountyCartComp, true, cartLoaderComp)
            && bountyCartComp.NotificationsEnabled)
        {
            _cartridgeLoader.SendNotification(cartComp.LoaderUid.Value, sender, msg, cartLoaderComp);
        }
    }

    private void OnUiMessage(Entity<AntagBountyContractsCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is AntagBountyContractCommandMessageEvent command)
        {
            var contract = ent.Comp.GetContract(command.ContractId);
            if (contract is null)
            {
                Log.Error($"Failed to find contract with id {command.ContractId} for command {command.Command}");
                return;
            }

            // Don't accept messages from entities with no mind.
            // Maybe change this later? Idk
            if (!GetMindFromCartridge(ent.Owner, out var mindId, out var mindComp))
            {
                Log.Warning("OnUiMessage: Received a cartridge command message from an entity with no mind. Ignoring message.");
                return;
            }

            switch (command.Command)
            {
                case AntagBountyContractCommand.AcceptBounty:
                    {
                        if (contract.State != AntagBountyContract.BountyState.Offered)
                        {
                            Log.Error($"Contract {contract.Bounty} is not in an offered state, cannot accept.");
                            return;
                        }

                        var ev = new OnAntagBountyAcceptedEvent(
                            contract,
                            (mindId, mindComp),
                            ent
                        );

                        RaiseLocalEvent(ref ev);

                        contract.State = AntagBountyContract.BountyState.Accepted;
                    }
                    break;
                case AntagBountyContractCommand.RejectBounty:
                    {
                        if (contract.State != AntagBountyContract.BountyState.Offered)
                        {
                            Log.Error($"Contract {contract.Bounty} is not in an offered state, cannot reject.");
                            return;
                        }

                        var ev = new OnAntagBountyRejectedEvent(
                            contract,
                            (mindId, mindComp),
                            ent
                        );

                        RaiseLocalEvent(ref ev);

                        contract.State = AntagBountyContract.BountyState.Rejected;
                        break;
                    }
            }

            RefreshBountyUI(ent);
        }
    }

    public bool GetMindFromCartridge(EntityUid uid, out EntityUid mindId, [NotNullWhen(true)] out MindComponent? mind)
    {
        TryComp<CartridgeComponent>(uid, out CartridgeComponent? comp);

        var loaderEnt = comp?.LoaderUid;

        // The entity holding this pda.
        EntityUid? pdaEnt = loaderEnt.HasValue ? Transform(loaderEnt.Value).ParentUid : null;
        if (pdaEnt is null)
        {
            mindId = EntityUid.Invalid;
            mind = null;
            return false;
        }

        return _mind.TryGetMind(pdaEnt.Value, out mindId, out mind);
    }

    /// <summary>
    /// Tries to get an AntagBountyContractsCartridgeComponent from a player's session.
    /// </summary>
    /// <param name="session">The player session to check</param>
    /// <param name="cartridgeUid">The entity UID of the cartridge, if found</param>
    /// <param name="component">The AntagBountyContractsCartridgeComponent, if found</param>
    /// <returns>True if the player has a PDA with the antag bounty cartridge installed</returns>
    public bool TryGetAntagCartridgeFromSession(ICommonSession session, [NotNullWhen(true)] out EntityUid? cartridgeUid, [NotNullWhen(true)] out AntagBountyContractsCartridgeComponent? component)
    {
        cartridgeUid = null;
        component = null;

        // Get the player's attached entity
        if (session.AttachedEntity is not { Valid: true } playerUid)
        {
            return false;
        }

        // Try to find a PDA/cartridge loader on the player
        // Check inventory slots for a device with CartridgeLoaderComponent
        var query = EntityQueryEnumerator<CartridgeLoaderComponent, TransformComponent>();
        while (query.MoveNext(out var loaderUid, out var loader, out var xform))
        {
            // Check if this loader is being held by or is a child of the player
            if (xform.ParentUid != playerUid)
            {
                continue;
            }

            // Try to get the antag bounty cartridge from this loader
            if (_cartridgeLoader.TryGetProgram<AntagBountyContractsCartridgeComponent>(loaderUid, out cartridgeUid, out component, false, loader))
            {
                return true;
            }
        }

        return false;
    }

    private void OnGetBountyContracts(EntityUid uid, AntagBountyContractsCartridgeComponent component, GetBountyContractsEvent ev)
    {
        if (ev.Collection != AntagCollection)
        {
            return;
        }

        foreach (var bounty in component.Contracts)
        {
            // Dont show the bounty if its been accepted/rejected
            if (bounty.Value.State != AntagBountyContract.BountyState.Offered)
            {
                continue;
            }

            ev.Bounties.Add(bounty.Value.Bounty);
        }
    }

    private void RefreshBountyUI(Entity<AntagBountyContractsCartridgeComponent> ent)
    {
        TryComp<BountyContractsCartridgeComponent>(ent, out var bountyComp);
        if (bountyComp is null)
        {
            return;
        }

        TryComp<CartridgeComponent>(ent, out CartridgeComponent? cartridgeComp);
        if (cartridgeComp is null || cartridgeComp.LoaderUid is null)
        {
            return;
        }

        _bounty.CartridgeRefreshListUi(new Entity<BountyContractsCartridgeComponent>(ent.Owner, bountyComp), cartridgeComp.LoaderUid.Value);
    }
}
