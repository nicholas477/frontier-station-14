using Content.Client._NF.BountyContracts.UI;
using Content.Shared._NF.BountyContracts;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Diagnostics.Contracts;

namespace Content.Client._NF.BountyContracts;

[ByRefEvent]
public record struct GetBountyContractUIEvent(BountyContractListUiState State, BountyContractUiFragmentList List, BountyContract Contract, bool CanRemove, NetEntity AuthorUid, Control? Control);

public sealed class BountyContractSystem : SharedBountyContractSystem
{
    public Control? GetControlForBountyEntry(BountyContractListUiState state, BountyContractUiFragmentList list, BountyContract contract, bool canRemove, NetEntity authorUid)
    {
        var ev = new GetBountyContractUIEvent(state, list, contract, canRemove, authorUid, null);
        RaiseLocalEvent(ref ev);

        return ev.Control;
    }

    public Control CreateDefaultBountyEntryControl(BountyContract contract, BountyContractUiFragmentList list, bool canRemove, NetEntity authorUid)
    {
        var control = new BountyContractUiFragmentListEntry(contract, canRemove || contract.AuthorUid == authorUid);
        control.OnRemoveButtonPressed += c =>
        {
            list.InvokeOnRemoveButtonPressed(c);
        };
        return control;
    }
}
