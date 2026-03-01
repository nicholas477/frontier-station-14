using Content.Server._EGG.BountyContracts.Antag;
using Content.Server._NF.BountyContracts;
using Content.Server._NF.Radio.Components;
using Content.Shared._NF.BountyContracts;
using Content.Shared.BarSign;
using Content.Shared.CartridgeLoader;
using Content.Shared.Mind;
using JetBrains.FormatRipper.Elf;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server._EGG.BountyContracts;

public sealed partial class EGGBountyContractSystem : EntitySystem
{
    private readonly ProtoId<BountyContractCollectionPrototype> _antagCollection = "Antag";
    //private readonly ProtoId<Entity> _objectivesCollection = "Antag";

    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BountyContractsCartridgeComponent, GetBountyContractsEvent>(OnGetBountyContracts);
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

    private void OnGetBountyContracts(EntityUid uid, BountyContractsCartridgeComponent component, GetBountyContractsEvent ev)
    {
        if (!GetMindFromCartridge(uid, out var mindId, out var mind))
        {
            //Log.Debug("Player mind: {0}", mind!.ToString());
            return;
        }

        // Iterate over all prototypes of a specific type
        foreach (var prototype in _protoMan.EnumeratePrototypes<AntagBountyPrototype>())
        {
            // Do something with prototype
            //Log.Info($"Prototype: {prototype.ID}");

            if (prototype.Collection == ev.Collection)
            {
                ev.Bounties.Add(new BountyContract(1000, BountyContractCategory.Other, prototype.Name, prototype.Reward, GetNetEntity(uid), null, null, prototype.Description, null));
            }
        }




        //// PDA
        //var pdaParent = Transform(uid).ParentUid;
        //var playerParent = Transform(pdaParent).ParentUid;

        //if (ev.Collection == _antagCollection)
        //{
        //    ev.Bounties.Add(new BountyContract(1000, BountyContractCategory.Other, "test", 1000, GetNetEntity(uid), null, null, null, null));
        //}
    }
}
