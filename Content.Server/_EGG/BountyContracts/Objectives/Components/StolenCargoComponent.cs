using Content.Server._EGG.BountyContracts.Objectives.Systems;
using Robust.Shared.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._EGG.BountyContracts.Objectives.Components;

/// <summary>
/// Component added on to cargo that leaves a ship to mark it as stolen for the steal cargo objective.
/// </summary>
[RegisterComponent, Access(typeof(StealCargoObjectiveSystem))]
public sealed partial class StolenCargoComponent : Component
{
    /// <summary>
    /// The player whose cargo should be stolen.
    /// </summary>
    [DataField]
    public ICommonSession? LastOwner;
}
