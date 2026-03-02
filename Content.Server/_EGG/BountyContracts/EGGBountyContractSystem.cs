using Content.Server._NF.BountyContracts;
using Content.Shared._EGG.BountyContracts;
using Content.Shared._EGG.BountyContracts.Antag;
using Content.Shared._NF.BountyContracts;
using Content.Shared.CartridgeLoader;
using Content.Shared.Mind;
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
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly TimeSpan _nextAntagDecisionTimerLength = TimeSpan.FromSeconds(10);
    private TimeSpan _nextAntagDecisionTime = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagBountyContractsCartridgeComponent, GetBountyContractsEvent>(OnGetBountyContracts);
        SubscribeLocalEvent<AntagBountyContractsCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        if (curTime > _nextAntagDecisionTime)
        {
            DecideAntagBounties();
            _nextAntagDecisionTime += _nextAntagDecisionTimerLength;
        }
    }

    private void DecideAntagBounties()
    {
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
                var newBounty = new BountyContract(nextContractId, BountyContractCategory.Other, prototype.Name, prototype.Reward, GetNetEntity(uid), null, null, prototype.Description, null, "antag");
                comp.Contracts.Add(nextContractId, newBounty);
            }
        }
    }

    private void OnUiMessage(Entity<AntagBountyContractsCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        Log.Debug("Hello!");
        if (args is AntagBountyContractCommandMessageEvent command)
        {
            //command.
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
            //ev.Bounties.Add(new BountyContract(1000, BountyContractCategory.Other, prototype.Name, prototype.Reward, GetNetEntity(uid), null, null, prototype.Description, null, "antag"));
            ev.Bounties.Add(bounty.Value);
        }
    }
}
