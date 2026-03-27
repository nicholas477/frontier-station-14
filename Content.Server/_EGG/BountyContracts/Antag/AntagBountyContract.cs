using Content.Shared._EGG.BountyContracts.Antag;
using Content.Shared._NF.BountyContracts;

namespace Content.Server._EGG.BountyContracts.Antag;

[Virtual]
public partial class AntagBountyContract : SharedAntagBountyContract
{
    public AntagBountyContract(BountyContract bounty)
        : base(bounty)
    {

    }
}
