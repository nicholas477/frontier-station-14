using Content.Shared._EGG.BountyContracts.Antag;
using Content.Shared._NF.BountyContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._EGG.BountyContracts.Antag;

[Virtual]
public sealed partial class AntagBountyContract : SharedAntagBountyContract
{
    public AntagBountyContract(BountyContract bounty)
        : base(bounty)
    {

    }
}
