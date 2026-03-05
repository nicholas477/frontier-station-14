using Content.Server._NF.BountyContracts;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Shared._EGG.BountyContracts;
using Content.Shared._EGG.BountyContracts.Antag;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Movement.Components;
using Robust.Server.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server._EGG.BountyContracts;

/// <summary>
/// Currently handles antag bounties
/// </summary>
public sealed partial class EGGBountySelectionSystem : EntitySystem
{
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private EntityQuery<CargoPalletComponent> _cargoPalletQuery;
    private HashSet<EntityUid> _itemsOnPallet = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DecideAntagBountiesEvent>(OnDecideAntagBounties);
        _cargoPalletQuery = GetEntityQuery<CargoPalletComponent>();
    }

    private void OnDecideAntagBounties(DecideAntagBountiesEvent ev)
    {
        Log.Debug("Deciding antag bounties");

        var playerWithMostCargo = FindPlayerWithHighestCargoValue();
        if (playerWithMostCargo == null)
        {
            Log.Debug("No player found with cargo on their ship");
            return;
        }

        var (playerSession, cargoValue) = playerWithMostCargo.Value;
        Log.Debug($"Player {playerSession.Name} has the highest cargo value: {cargoValue}");
    }

    /// <summary>
    /// Finds the player with the highest total cargo value on their ship.
    /// </summary>
    /// <returns>A tuple of (ICommonSession, int) where int is the total cargo value, or null if no player has cargo.</returns>
    private (ICommonSession Session, double CargoValue)? FindPlayerWithHighestCargoValue()
    {
        (ICommonSession, double)? result = null;
        double highestValue = 0.0;

        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is not { Valid: true } playerUid)
            {
                continue;
            }

            var cargoValue = GetPlayerShipCargoValue(playerUid);

            Log.Debug($"Player {session.Name} has cargo value: {cargoValue}");

            if (cargoValue > highestValue)
            {
                highestValue = cargoValue;
                result = (session, cargoValue);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the total cargo value on a player's ship.
    /// </summary>
    private double GetPlayerShipCargoValue(EntityUid playerUid)
    {
        var shipUid = GetPlayerShip(playerUid);
        if (shipUid == null || !Exists(shipUid))
            return 0;

        return GetShipCargoValue(shipUid.Value);
    }

    /// <summary>
    /// Gets the ship owned by a player from their ID card deed.
    /// </summary>
    private EntityUid? GetPlayerShip(EntityUid playerUid)
    {
        if (!_idCardSystem.TryFindIdCard(playerUid, out var idCard))
            return null;

        if (!TryComp<ShuttleDeedComponent>(idCard, out var deed) || deed.ShuttleUid == null)
            return null;

        return deed.ShuttleUid;
    }

    /// <summary>
    /// Gets the total cargo value on all pallets on a ship/grid.
    /// </summary>
    private double GetShipCargoValue(EntityUid gridUid)
    {
        double totalValue = 0.0;

        totalValue = _pricing.AppraiseGrid(gridUid, AppraisalPredicate);

        //var query = EntityQueryEnumerator<CargoPalletComponent, TransformComponent>();
        //while (query.MoveNext(out var palletUid, out _, out var palletXform))
        //{
        //    // Check if this pallet is on the target grid
        //    if (palletXform.GridUid != gridUid)
        //        continue;

        //    // Get all items on this pallet
        //    _itemsOnPallet.Clear();
        //    _lookup.GetEntitiesIntersecting(
        //        palletUid,
        //        _itemsOnPallet,
        //        LookupFlags.Dynamic | LookupFlags.Sundries);

        //    foreach (var item in _itemsOnPallet)
        //    {
        //        // Skip anchored items (like fixtures)
        //        if (TryComp<TransformComponent>(item, out var itemXform) && itemXform.Anchored)
        //            continue;

        //        totalValue += _pricing.GetPrice(item);
        //    }
        //}

        return totalValue;
    }

    private bool AppraisalPredicate(EntityUid uid)
    {
        //return !TryComp<ShipyardSellConditionComponent>(uid, out var comp) || comp.PreserveOnSale == false;
        return true;
    }
}
