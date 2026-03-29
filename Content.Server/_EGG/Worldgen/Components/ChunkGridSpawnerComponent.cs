using Content.Server._EGG.Worldgen.Systems;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._EGG.Worldgen.Components;

[RegisterComponent, Access(typeof(ChunkGridSpawnerSystem))]
public sealed partial class ChunkGridSpawnerComponent : Component
{
    /// <summary>
    ///     Entries that will have their spawn behavior overriden
    /// </summary>
    [DataField("entries", required: true)]
    public List<EntProtoId> Entries = default!;
}
