using Content.Client._EGG.BountyContracts.UI;
using Content.Client._NF.BountyContracts;
using Content.Client.UserInterface.ControlExtensions;
using Content.Shared._EGG.BountyContracts;
using System.Xml.Linq;

namespace Content.Client._EGG.BountyContracts;

public sealed partial class EGGBountyContractSystem : SharedEGGBountyContractSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetBountyContractUIEvent>(OnGetBountyContractUI);
    }

    private void OnGetBountyContractUI(ref GetBountyContractUIEvent ev)
    {
        if (ev.Contract.EntryUIId == "antag")
        {
            var control = new AntagBountyContractUiFragmentListEntry(ev.Contract, ev.CanRemove);
            control.OnRemoveButtonPressed += ev.List.InvokeOnRemoveButtonPressed;

            var contract = ev.Contract.ContractId;
            var list = ev.List;
            control.OnAcceptButtonPressed += _ =>
            {
                var command = new AntagBountyContractCommandMessageEvent(AntagBountyContractCommand.AcceptBounty, contract);
                list.SendContractCommand(command);
            };
            ev.Control = control;
        }
    }
}
