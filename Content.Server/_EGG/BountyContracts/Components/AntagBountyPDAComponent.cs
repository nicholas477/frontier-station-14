using Content.Server._EGG.BountyContracts.Objectives.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._EGG.BountyContracts.Components;

/// <summary>
///     Just handles access tags for when the player is a bounty antag
/// </summary>
[RegisterComponent, Access(typeof(EGGBountyContractSystem))]
public sealed partial class AntagBountyPDAComponent : Component
{
}
