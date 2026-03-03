using Content.Server._NF.BountyContracts;
using Content.Shared._EGG.BountyContracts;
using Content.Shared._EGG.BountyContracts.Antag;
using Content.Shared._NF.BountyContracts;
using Content.Shared.CartridgeLoader;
using Content.Shared.Mind;
using JetBrains.FormatRipper.Elf;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Content.Server._EGG.BountyContracts;

/// <summary>
/// Currently handles antag bounties
/// </summary>
public sealed partial class EGGBountyContractSystem : SharedEGGBountyContractSystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BountyContractSystem _bounty = default!;

    private TimeSpan _lastAntagDecisionTime = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagBountyContractsCartridgeComponent, GetBountyContractsEvent>(OnGetBountyContracts);
        SubscribeLocalEvent<AntagBountyContractsCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);

        InitializeCVars();
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
        // Leave it to other systems to do
        RaiseLocalEvent(new DecideAntagBountiesEvent());

        var query = EntityQueryEnumerator<AntagBountyContractsCartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            //If whatever is holding the cartridge doesn't have a mind then they wont get new antag bounties
            if (!GetMindFromCartridge(uid, out var mindId, out var mind))
            {
                continue;
            }

            foreach (var prototype in _protoMan.EnumeratePrototypes<AntagBountyPrototype>())
            {
                var nextContractId = comp.GetNextContractId();
                var newBounty = new AntagBountyContract(prototype,
                    new BountyContract(nextContractId, BountyContractCategory.Other, prototype.Name, prototype.Reward, GetNetEntity(uid), null, null, prototype.Description, null, "antag"));
                comp.Contracts.Add(nextContractId, newBounty);
            }

            TryComp<BountyContractsCartridgeComponent>(uid, out var bountyComp);
            if (bountyComp is null)
            {
                continue;
            }

            RefreshBountyUI(new Entity<AntagBountyContractsCartridgeComponent>(uid, comp));
        }
    }

    private void OnUiMessage(Entity<AntagBountyContractsCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        Log.Debug("Hello!");
        if (args is AntagBountyContractCommandMessageEvent command)
        {
            var contract = ent.Comp.GetContract(command.ContractId);
            if (contract is null)
            {
                return;
            }

            if (!GetMindFromCartridge(ent.Owner, out var mindId, out var mindComp))
            {
                return;
            }

            switch (command.Command)
            {
                case AntagBountyContractCommand.AcceptBounty:
                    {
                        foreach (var item in contract.Prototype.Objectives)
                        {
                            _mind.TryAddObjective(mindId, mindComp, item);
                        }

                        contract.State = AntagBountyContract.BountyState.Accepted;
                    }
                    break;
                case AntagBountyContractCommand.RejectBounty:
                    contract.State = AntagBountyContract.BountyState.Rejected;
                    break;
            }

            RefreshBountyUI(ent);
        }
    }

    private bool GetMindFromCartridge(EntityUid uid, out EntityUid mindId, [NotNullWhen(true)] out MindComponent? mind)
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
