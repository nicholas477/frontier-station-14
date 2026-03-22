using Content.Server._EGG.Components;
using Content.Server.Database;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using JetBrains.FormatRipper.Elf;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;

namespace Content.Server._EGG.Systems;

/// <summary>
/// System that handles loading grids from .yml files via GridLoadComponent.
/// </summary>
public sealed class GridLoadSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GridLoadComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(EntityUid uid, GridLoadComponent component, ComponentStartup args)
    {
        var xform = Transform(uid);

        if (xform.GridUid == null)
        {
            Log.Error($"Entity {ToPrettyString(uid)} with GridLoadComponent has no grid!");
            return;
        }

        if (xform.MapUid == null)
        {
            Log.Error($"Entity {ToPrettyString(uid)} with GridLoadComponent has no map!");
            return;
        }

        var mapId = xform.MapID;

        if (_loader.TryLoadGrid(mapId, component.GridPath, out var loadedGrid, null, _transformSystem.GetWorldPosition(xform)))
        {
            if (loadedGrid is null)
            {
                return;
            }

            var enumerator = Transform(loadedGrid.Value.Owner).ChildEnumerator;

            // Reparent the grid
            //var children = new List<EntityUid>();
            //while (enumerator.MoveNext(out var child))
            //{
            //    children.Add(child);
            //}

            //foreach (var child in children)
            //{
            //    _transformSystem.SetParent(child, xform.GridUid.Value);
            //}
            //_transformSystem.SetParent(loadedGrid.Value.Owner, xform.GridUid.Value);

            // Set the grid name if requested
            //if (component.NameGrid)
            //{
            //    var name = component.GridPath.FilenameWithoutExtension;
            //    _metadata.SetEntityName(loadedGrid.Value.Owner, name);
            //}

            // Add to station if requested
            //if (component.AddToStation)
            //{
            //    _station.AddGridToStation(uid, loadedGrid.Value.Owner);
            //}
        }
        else
        {
            Log.Error($"Failed to load grid from {component.GridPath} for {ToPrettyString(uid)}");
        }
    }
}
