using Content.Server._EGG.Worldgen.Systems;
using Robust.Shared.Utility;

namespace Content.Server._EGG.Components;

/// <summary>
///     Tells the ChunkGridSpawnerComponent to load a grid from a .yml file
///     instead of generating one for worldgen
/// </summary>
[RegisterComponent, Access(typeof(ChunkGridSpawnerSystem))]
public sealed partial class GridLoadComponent : Component
{
    /// <summary>
    /// Path to the .yml grid file to load.
    /// </summary>
    [DataField(required: true)]
    public ResPath GridPath = default!;
}
