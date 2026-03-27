using Content.Client._EGG.BountyContracts.UI;
using Content.Client._NF.BountyContracts;
using Content.Client.UserInterface.ControlExtensions;
using Content.Shared._EGG.BountyContracts;
using Content.Shared._EGG.BountyContracts.Antag;
using Content.Shared._EGG.BountyContracts.Objectives;
using Content.Shared.Objectives.Components;
using System.Xml.Linq;
using Robust.Client.UserInterface;

namespace Content.Client._EGG.BountyContracts;

public sealed partial class EGGBountyContractSystem : SharedEGGBountyContractSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetBountyContractUIEvent>(OnGetBountyContractUI);
    }

    private void OnGetBountyContractUI(ref GetBountyContractUIEvent ev)
    {
        if (ev.Contract.EntryUIId == "antag")
        {
            var uid = _entityManager.GetEntity(ev.State.Loader);

            if (!TryComp<AntagBountyContractsCartridgeComponent>(uid, out var cartridge))
            {
                return;
            }

            var list = ev.List;
            var contract = ev.Contract.ContractId;

            if (cartridge.GetContract(contract) is not SharedStealCargoBounty stealBounty)
            {
                return;
            }

            var control = new AntagBountyContractUI(ev.Contract, ev.CanRemove);
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
            stealBounty.UI = control;
        }
    }
}
