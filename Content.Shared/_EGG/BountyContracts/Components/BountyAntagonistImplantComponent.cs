using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._EGG.BountyContracts.Components;

/// <summary>
/// Marks an entity as being eligible to receive antagonist bounty contracts.
/// Implanted via a medical device.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BountyAntagonistImplantComponent : Component
{
    /// <summary>
    ///     Sound that plays when the implant is implanted, on the player
    /// </summary>
    [DataField]
    public SoundSpecifier ImplantSound = new SoundPathSpecifier("/Audio/Misc/thief_greeting.ogg");
}
