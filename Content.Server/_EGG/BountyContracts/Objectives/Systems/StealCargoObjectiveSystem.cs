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
using Content.Shared.Mind;
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
using static Robust.Shared.Physics.DynamicTree;

namespace Content.Server._EGG.BountyContracts.Objectives.Systems;

public sealed partial class AntagBountyContractStealCargo : AntagBountyContract
{
    public AntagBountyContractStealCargo(ICommonSession? inPlayerToStealFrom, BountyContract bounty)
        : base(bounty)
    {
        PlayerToStealFrom = inPlayerToStealFrom;
    }

    public readonly ICommonSession? PlayerToStealFrom;
}

public sealed partial class StealCargoObjectiveSystem : EntitySystem
{
    [Dependency] private readonly EGGBountyContractSystem _eggBountyContractSystem = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Used for deciding which player cargo should be stolen from
        SubscribeLocalEvent<DecideAntagBountiesEvent>(OnDecideAntagBounties);

        SubscribeLocalEvent<OnAntagBountyAcceptedEvent>(OnBountyAccepted);

        SubscribeLocalEvent<StealCargoObjectiveComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<StealCargoObjectiveComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<StealCargoObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        // Track when entities change parent (leave the target grid)
        SubscribeLocalEvent<TransformComponent, EntParentChangedMessage>(OnEntityParentChanged);
    }

    private void OnDecideAntagBounties(ref DecideAntagBountiesEvent ev)
    {
        //Log.Debug("Deciding antag bounties");

        var playerWithMostCargo = FindPlayerWithHighestCargoValue();
        if (playerWithMostCargo == null)
        {
            //Log.Debug("No player found with cargo on their ship");
            //return;
        }

        var (playerToStealFrom, cargoValue) = playerWithMostCargo.HasValue ? playerWithMostCargo.Value : (null, 0.0);
        if (playerToStealFrom is not null)
        {
            //Log.Debug($"Player {playerToStealFrom.Name} has the highest cargo value: {cargoValue}");
        }

        playerToStealFrom ??= _random.Pick(_playerManager.Sessions);

        if (playerToStealFrom is null)
        {
            return;
        }

        var validSessions = _playerManager.Sessions
            .Where(session => session.AttachedEntity is { Valid: true } && session != playerToStealFrom)
            .ToList();

        if (validSessions.Count == 0)
        {
            //Log.Debug("No valid players found to give \"steal cargo\" objective");
            return;
        }

        var randomSession = _random.Pick(validSessions);

        if (!_eggBountyContractSystem.TryGetAntagCartridgeFromSession(randomSession, out EntityUid? cartridgeUid, out AntagBountyContractsCartridgeComponent? comp))
        {
            return;
        }

        // Don't offer more than one contract
        if (comp.Contracts.Any(item => item.Value is AntagBountyContractStealCargo bounty && bounty.State == AntagBountyContract.BountyState.Offered))
        {
            //Log.Debug("Not offering a new bounty");
            return;
        }

        var id = "AntagBountyStealCargo";
        var prototype = _protoMan.Index<AntagBountyPrototype>(id);

        var nextContractId = comp.GetNextContractId();
        var bounty = AntagBountyContract.MakeBountyFromPrototype(nextContractId, GetNetEntity(cartridgeUid.Value), prototype);
        var newBounty = new AntagBountyContractStealCargo(playerToStealFrom, bounty);

        comp.Contracts.Add(nextContractId, newBounty);
        _eggBountyContractSystem.SendBountyNotification(cartridgeUid.Value, null, "New antag bounty!");
    }


    private void OnBountyAccepted(ref OnAntagBountyAcceptedEvent ev)
    {
        if (ev.Contract is not AntagBountyContractStealCargo contract)
        {
            return;
        }

        if (contract.PlayerToStealFrom is null)
        {
            Log.Error("Contract has null player to steal from, cannot accept.");
            return;
        }

        // Spawn a new objective component, set up its variables
        var proto = "EGGAntagStealCargoObjective";
        var uid = Spawn(proto);
        if (!TryComp<ObjectiveComponent>(uid, out var comp))
        {
            Del(uid);
            Log.Error($"Invalid objective prototype {proto}, missing ObjectiveComponent");
            return;
        }

        if (!TryComp<StealCargoObjectiveComponent>(uid, out var stealComp))
        {
            Del(uid);
            Log.Error($"Invalid objective prototype {proto}, missing ObjectiveComponent");
            return;
        }

        // Configure the objective with the target player
        stealComp.PlayerToStealFrom = contract.PlayerToStealFrom;

        var objectiveEv = new ObjectiveAssignedEvent(ev.Mind.Owner, ev.Mind.Comp);
        RaiseLocalEvent(uid, ref objectiveEv);
        if (objectiveEv.Cancelled)
        {
            Del(uid);
            Log.Warning($"Could not assign objective {proto}, deleted it");
            return;
        }

        // let the title description and icon be set by systems
        var afterEv = new ObjectiveAfterAssignEvent(ev.Mind.Owner, ev.Mind.Comp, comp, MetaData(uid));
        RaiseLocalEvent(uid, ref afterEv);

        _mind.AddObjective(ev.Mind.Owner, ev.Mind.Comp, uid);

        Log.Debug("Butthole sniffers!");
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

            //Log.Debug($"Player {session.Name} has cargo value: {cargoValue}");

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

        var targetValueStr = condition.Comp.TargetStolenValue.ToString("F0");

        // Get the character's name
        var playerName = "Unknown";
        var shipName = "Unknown";
        if (condition.Comp.PlayerToStealFrom?.AttachedEntity is { Valid: true } playerEntity)
        {
            playerName = MetaData(playerEntity).EntityName;

            // Get the ship name
            var shipUid = GetPlayerShip(playerEntity);
            if (shipUid is { Valid: true })
            {
                shipName = MetaData(shipUid.Value).EntityName;
            }
        }

        _metaData.SetEntityName(condition.Owner, "Steal Cargo", args.Meta);
        _metaData.SetEntityDescription(condition.Owner, $"Steal {targetValueStr} credits worth of cargo from {playerName}'s ship, the {shipName}.", args.Meta);
    }

    private void OnGetProgress(Entity<StealCargoObjectiveComponent> condition, ref ObjectiveGetProgressEvent args)
    {
        if (condition.Comp.TargetStolenValue <= 0)
        {
            args.Progress = 0;
            return;
        }

        // Get the mind's entity (the thief)
        if (!TryComp<MindComponent>(args.MindId, out var mind) || mind.OwnedEntity is not { Valid: true } thiefEntity)
        {
            args.Progress = 0;
            return;
        }

        // Get the thief's ship
        var thiefShip = GetPlayerShip(thiefEntity);
        if (thiefShip == null)
        {
            args.Progress = 0;
            return;
        }

        // Sum the value of all stolen cargo on the thief's ship
        double totalStolenValue = 0.0;
        var stolenCargoQuery = EntityQueryEnumerator<StolenCargoComponent, TransformComponent>();
        while (stolenCargoQuery.MoveNext(out var cargoUid, out var stolenComp, out var xform))
        {
            // Check if this item was stolen from the target player
            if (stolenComp.LastOwner != condition.Comp.PlayerToStealFrom)
                continue;

            // Check if the item is on the thief's ship
            if (xform.GridUid != thiefShip)
                continue;

            // Add the item's value
            var itemValue = _pricing.GetPrice(cargoUid);
            totalStolenValue += itemValue;
        }

        // Calculate progress as a ratio of stolen value to target value
        var progress = (float)(totalStolenValue / condition.Comp.TargetStolenValue);
        args.Progress = Math.Clamp(progress, 0.0f, 1.0f);
    }

    /// <summary>
    /// Tracks when entities leave the target player's ship grid.
    /// If they were stolen from the target ship, adds their value to the objective.
    /// </summary>
    private void OnEntityParentChanged(Entity<TransformComponent> ent, ref EntParentChangedMessage args)
    {
        //Log.Debug($"Entity parent changed! Ent: {ent}");

        if (!TryComp(ent.Owner, out MetaDataComponent? ownerMeta))
        {
            return;
        }

        if (ownerMeta.EntityLifeStage >= EntityLifeStage.Terminating)
        {
            return;
        }

        // Find all active steal cargo objectives
        // TONS of items change parent, make this code less ass
        var query = AllEntityQuery<StealCargoObjectiveComponent>();
        while (query.MoveNext(out var objectiveUid, out var objective))
        {
            // Skip if not configured yet
            if (objective.PlayerToStealFrom == null)
                continue;

            if (objective.GetPlayerEntity() is not { Valid: true } playerUid)
                continue;

            var targetShipGrid = GetPlayerShip(playerUid);
            if (targetShipGrid is null)
                continue;

            // Skip if the entity was already counted
            if (objective.StolenItems.Contains(ent.Owner))
                continue;

            // Check if the item is leaving the target ship
            if (args.OldParent == targetShipGrid && ent.Comp.GridUid != targetShipGrid)
            {
                // Skip certain entities that shouldn't be counted
                if (HasComp<ActorComponent>(ent))
                    continue;

                var stolenCargoComp = EnsureComp<StolenCargoComponent>(ent);
                stolenCargoComp.LastOwner = objective.PlayerToStealFrom;

                //// Get the value of this item
                //var itemValue = _pricing.GetPrice(ent);
                //if (itemValue <= 0)
                //    continue;

                //// Add to stolen value
                //objective.CurrentStolenValue += itemValue;
                //objective.StolenItems.Add(ent.Owner);
                //Dirty(objectiveUid, objective);

                //Log.Debug($"Item {ToPrettyString(ent)} left target ship with value {itemValue}. Total stolen: {objective.CurrentStolenValue}");
            }
        }
    }
}
