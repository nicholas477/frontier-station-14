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
            var list = ev.List;
            var contract = ev.Contract.ContractId;

            var control = new AntagBountyContractUiFragmentListEntry(ev.Contract, ev.CanRemove);
            control.OnRemoveButtonPressed += _ =>
            {
                var command = new AntagBountyContractCommandMessageEvent(AntagBountyContractCommand.RejectBounty, contract);
                list.SendContractCommand(command);
            };

            control.OnAcceptButtonPressed += _ =>
            {
                var command = new AntagBountyContractCommandMessageEvent(AntagBountyContractCommand.AcceptBounty, contract);
                list.SendContractCommand(command);
            };
            ev.Control = control;
        }
    }
}
