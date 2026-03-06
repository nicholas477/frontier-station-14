using Content.Client._NF.BountyContracts.UI;
using Content.Shared._NF.BountyContracts;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Diagnostics.Contracts;

namespace Content.Client._NF.BountyContracts;

[ByRefEvent]
public record struct GetBountyContractUIEvent(BountyContractUiFragmentList List, BountyContract Contract, bool CanRemove, NetEntity AuthorUid, Control? Control);

public sealed class BountyContractSystem : SharedBountyContractSystem
{
    public Control GetControlForBountyEntry(BountyContractUiFragmentList list, BountyContract contract, bool canRemove, NetEntity authorUid)
    {
        var ev = new GetBountyContractUIEvent(list, contract, canRemove, authorUid, null);
        RaiseLocalEvent(ref ev);

        if (ev.Control is not null)
        {
            return ev.Control;
        }
        else
        {
            var control = new BountyContractUiFragmentListEntry(contract, canRemove || contract.AuthorUid == authorUid);
            control.OnRemoveButtonPressed += c =>
            {
                list.InvokeOnRemoveButtonPressed(c);
            };
            return control;
        }
    }
}
