using Content.Server._EGG.BountyContracts.Objectives.Components;
using Content.Server._NF.BountyContracts;
using Content.Server.Access.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Objectives.Components;
using Content.Shared._EGG.BountyContracts;
using Content.Shared._EGG.BountyContracts.Antag;
using Content.Shared._NF.BountyContracts;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Movement.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Server.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Toolshed.TypeParsers;
using System.Linq;

namespace Content.Server._EGG.BountyContracts.Objectives.Systems;

public sealed partial class StealCargoObjectiveSystem : EntitySystem
{
    [Dependency] private readonly EGGBountyContractSystem _eggBountyContractSystem = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Used for deciding which player cargo should be stolen from
        SubscribeLocalEvent<DecideAntagBountiesEvent>(OnDecideAntagBounties);

        SubscribeLocalEvent<StealCargoObjectiveComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<StealCargoObjectiveComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<StealCargoObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
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

        if (playerSession is null)
        {
            return;
        }

        var validSessions = _playerManager.Sessions
            .Where(session => session.AttachedEntity is { Valid: true } && session != playerSession)
            .ToList();

        if (validSessions.Count == 0)
        {
            Log.Debug("No valid players found to give \"steal cargo\" objective");
            return;
        }

        var randomSession = _random.Pick(validSessions);

        if (!_eggBountyContractSystem.TryGetAntagCartridgeFromSession(randomSession, out EntityUid? cartridgeUid, out AntagBountyContractsCartridgeComponent? comp))
        {
            return;
        }


        // AntagBountyStealCargo
        var prototype = _protoMan.Index<AntagBountyPrototype>("AntagBountyStealCargo");
        var nextContractId = comp.GetNextContractId();
        var newBounty = new AntagBountyContract(prototype,
            new BountyContract(nextContractId, BountyContractCategory.Other, prototype.Name, prototype.Reward, GetNetEntity(cartridgeUid.Value), null, null, prototype.Description, null, "antag"));
        comp.Contracts.Add(nextContractId, newBounty);
        _eggBountyContractSystem.SendBountyNotification(cartridgeUid.Value, null, "New antag bounty!");
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

        return totalValue;
    }

    private bool AppraisalPredicate(EntityUid uid)
    {
        //return !TryComp<ShipyardSellConditionComponent>(uid, out var comp) || comp.PreserveOnSale == false;
        return true;
    }

    /// start checks of target acceptability, and generation of start values.
    private void OnAssigned(Entity<StealCargoObjectiveComponent> condition, ref ObjectiveAssignedEvent args)
    {
        //List<StealTargetComponent?> targetList = new();

        //var query = AllEntityQuery<StealTargetComponent>();
        //while (query.MoveNext(out var target))
        //{
        //    if (condition.Comp.StealGroup != target.StealGroup)
        //        continue;

        //    targetList.Add(target);
        //}

        //// cancel if the required items do not exist
        //if (targetList.Count == 0 && condition.Comp.VerifyMapExistence)
        //{
        //    args.Cancelled = true;
        //    return;
        //}

        ////setup condition settings
        //var maxSize = condition.Comp.VerifyMapExistence
        //    ? Math.Min(targetList.Count, condition.Comp.MaxCollectionSize)
        //    : condition.Comp.MaxCollectionSize;
        //var minSize = condition.Comp.VerifyMapExistence
        //    ? Math.Min(targetList.Count, condition.Comp.MinCollectionSize)
        //    : condition.Comp.MinCollectionSize;

        //condition.Comp.CollectionSize = _random.Next(minSize, maxSize);
    }

    //Set the visual, name, icon for the objective.
    private void OnAfterAssign(Entity<StealCargoObjectiveComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        //var group = _proto.Index(condition.Comp.StealGroup);
        //string localizedName = Loc.GetString(group.Name);

        //var title = condition.Comp.OwnerText == null
        //    ? Loc.GetString(condition.Comp.ObjectiveNoOwnerText, ("itemName", localizedName))
        //    : Loc.GetString(condition.Comp.ObjectiveText, ("owner", Loc.GetString(condition.Comp.OwnerText)), ("itemName", localizedName));

        //var description = condition.Comp.CollectionSize > 1
        //    ? Loc.GetString(condition.Comp.DescriptionMultiplyText, ("itemName", localizedName), ("count", condition.Comp.CollectionSize))
        //    : Loc.GetString(condition.Comp.DescriptionText, ("itemName", localizedName));

        _metaData.SetEntityName(condition.Owner, "Steal Cargo", args.Meta);
        _metaData.SetEntityDescription(condition.Owner, "Steal some cargo idiot", args.Meta);
        //_objectives.SetIcon(condition.Owner, group.Sprite, args.Objective);
    }

    private void OnGetProgress(Entity<StealCargoObjectiveComponent> condition, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 0.0f;
    }
}
