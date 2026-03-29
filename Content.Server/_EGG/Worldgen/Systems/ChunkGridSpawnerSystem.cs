using Content.Server._EGG.Components;
using Content.Server._EGG.Worldgen.Components;
using Content.Server.Worldgen.Systems.Debris;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._EGG.Worldgen.Systems;

public sealed class ChunkGridSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChunkGridSpawnerComponent, TrySpawnPlaceableDebrisFeatureEvent>(OnSpawnDebris);
    }

    private void OnSpawnDebris(EntityUid uid, ChunkGridSpawnerComponent component, ref TrySpawnPlaceableDebrisFeatureEvent args)
    {
        if (args.DebrisProto is null)
            return;

        if (!component.Entries.Contains(args.DebrisProto))
            return;

        //var xform = Transform(uid);
        //if (xform.GridUid == null)
        //{
        //    Log.Error($"Entity {ToPrettyString(uid)} with ChunkGridSpawnerComponent has no grid!");
        //    return;
        //}

        var ent = Spawn(args.DebrisProto);
        if (!TryComp(ent, out GridLoadComponent? gridLoadComponent))
        {
            Log.Error($"Spawned debris {args.DebrisProto} does not have a GridLoadComponent!");
            Del(ent);
            return;
        }

        if (_loader.TryLoadGrid(_transformSystem.ToMapCoordinates(args.Coords).MapId, gridLoadComponent.GridPath, out var loadedGrid, null, args.Coords.Position, _random.NextAngle()))
        {
            if (loadedGrid is null)
            {
                Log.Error($"Failed to load grid for debris {args.DebrisProto} at {args.Coords}!");
                Del(ent);
                return;
            }

            args.SpawnedEnt = loadedGrid;
        }

        Del(ent);
    }
}
