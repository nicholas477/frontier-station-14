using Content.Shared._EGG.BountyContracts.Antag;
using Content.Shared._NF.BountyContracts;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared._EGG.BountyContracts.Objectives;

public interface IStealCargoBountyUI
{

}

[Virtual, Serializable, NetSerializable]
public partial class SharedStealCargoBounty : SharedAntagBountyContract
{
    public bool CanTurnIn = false;

    public SharedStealCargoBounty(ICommonSession? inPlayerToStealFrom, BountyContract bounty)
    : base(bounty)
    {
        PlayerToStealFrom = inPlayerToStealFrom;
    }

    [NonSerializedAttribute]
    public readonly ICommonSession? PlayerToStealFrom;

    [NonSerializedAttribute]
    public IStealCargoBountyUI? UI;
}
