using Content.Server._EGG.Systems;
using Robust.Shared.Utility;

namespace Content.Server._EGG.Components;

/// <summary>
/// Simple component that loads a grid from a .yml file
/// </summary>
[RegisterComponent, Access(typeof(GridLoadSystem))]
public sealed partial class GridLoadComponent : Component
{
    /// <summary>
    /// Path to the .yml grid file to load.
    /// </summary>
    [DataField(required: true)]
    public ResPath GridPath = default!;
}
