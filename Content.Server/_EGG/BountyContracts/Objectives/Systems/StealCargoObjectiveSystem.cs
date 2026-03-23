using Content.Server._EGG.BountyContracts.Components;
using Content.Server._EGG.BountyContracts.Objectives.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Objectives.Components;
using Content.Server.Thief.Systems;
using Content.Shared._EGG.BountyContracts.Antag;
using Content.Shared._EGG.BountyContracts.Components;
using Content.Shared._NF.BountyContracts;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Players;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

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
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ThiefBeaconSystem _thiefBeacon = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

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

        // Allow players with steal cargo objectives to use beacons
        SubscribeLocalEvent<CanSetBeaconCoordinateEvent>(OnCanSetBeaconCoordinate);
    }

    private void OnDecideAntagBounties(ref DecideAntagBountiesEvent ev)
    {
        //Log.Debug("Deciding antag bounties");

        var playerWithMostCargo = FindPlayerWithHighestCargoValue();
        if (playerWithMostCargo == null)
        {
            //Log.Debug("No player found with cargo on their ship");
            return;
        }

        var (playerToStealFrom, cargoValue) = playerWithMostCargo.HasValue ? playerWithMostCargo.Value : (null, 0.0);
        if (playerToStealFrom is null)
        {
            return;
        }

        var validSessions = _playerManager.Sessions
            .Where(session => 
            {
                if (session.AttachedEntity is not { Valid: true } entity || session == playerToStealFrom)
                    return false;

                var mindId = session.GetMind();
                if (mindId is null)
                {
                    return false;
                }

                return _roles.MindHasRole<BountyAntagonistRoleComponent>(mindId.Value);
            })
            .ToList();

        if (validSessions.Count == 0)
        {
            //Log.Debug("No valid players found with bounty antagonist implant");
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

        if (ev.Mind.Comp.OwnedEntity is not { Valid : true } traitor)
        {
            Log.Error("Somehow a player with no body accepted a bounty on a PDA");
            return;
        }

        // Give the player a thief uplink with 20 telecrystals
        //var uplinkBalance = FixedPoint2.New(20);
        //bool uplinked = _uplink.AddUplink(traitor, uplinkBalance, giveDiscounts: true);

        //string briefing = "";
        //Note[]? code = null;

        //var pda = _uplink.FindUplinkTarget(traitor);
        //if (pda is not null && uplinked)
        //{
        //    //Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink is PDA");
        //    // Codes are only generated if the uplink is a PDA
        //    var generateCodeEv = new GenerateUplinkCodeEvent();
        //    RaiseLocalEvent(pda.Value, ref generateCodeEv);

        //    if (generateCodeEv.Code is { } generatedCode)
        //    {
        //        code = generatedCode;

        //        // If giveUplink is false the uplink code part is omitted
        //        briefing = string.Format("{0}\n{1}",
        //            briefing,
        //            Loc.GetString("traitor-role-uplink-code-short", ("code", string.Join("-", code).Replace("sharp", "#"))));
        //        //return (code, briefing);
        //    }
        //}

        //_antag.SendBriefing(traitor, briefing, null, null);

        // Give the player the thief beacon and satchel
        var thiefBeacon = Spawn("ThiefBeacon", Transform(traitor).Coordinates);
        var satchelThief = Spawn("SatchelThief", Transform(traitor).Coordinates);

        _hands.TryPickupAnyHand(traitor, thiefBeacon);
        _hands.TryPickupAnyHand(traitor, satchelThief);
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

    private void OnAssigned(Entity<StealCargoObjectiveComponent> condition, ref ObjectiveAssignedEvent args)
    {
    }

    //Set the visual, name, icon for the objective.
    private void OnAfterAssign(Entity<StealCargoObjectiveComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        UpdateStealCargoMetadata(condition, args.Meta);
    }

    private void UpdateStealCargoMetadata(Entity<StealCargoObjectiveComponent> condition, MetaDataComponent? meta)
    {
        meta ??= TryComp(condition.Owner, out MetaDataComponent? metaComp) ? metaComp : null;

        _metaData.SetEntityName(condition.Owner, "Steal Cargo", meta);
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

        _metaData.SetEntityDescription(condition.Owner, $"Steal {targetValueStr} credits worth of cargo from {playerName}'s ship, the {shipName}.", meta);
    }

    private void OnGetProgress(Entity<StealCargoObjectiveComponent> condition, ref ObjectiveGetProgressEvent args)
    {
        // Update the metadata for the description (in case the target player changes ships or name, etc)
        UpdateStealCargoMetadata(condition, null);

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

        // Track entities that have been appraised to prevent double counting
        var appraisedEntities = new HashSet<EntityUid>();

        // Sum the value of all stolen cargo on the thief's ship
        double totalStolenValue = 0.0;

        // Get the thief's ship
        var thiefShip = GetPlayerShip(thiefEntity);
        if (thiefShip != null)
        {
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
                appraisedEntities.Add(cargoUid);
            }
        }

        var beaconQuery = EntityQueryEnumerator<StealAreaComponent, TransformComponent>();
        while (beaconQuery.MoveNext(out var beaconUid, out var areaComp, out var beaconXform))
        {
            // Check if the beacon is owned by the player
            var owners = _thiefBeacon.GetStealAreaOwners(beaconUid);
            if (owners is null)
            {
                continue;
            }

            if (!owners.Contains(args.MindId))
            {
                continue;
            }

            // Add the value of all items currently in the beacon's area
            totalStolenValue += GetBeaconAreaValue(beaconUid, condition.Comp.PlayerToStealFrom, appraisedEntities);
        }

        // Calculate progress as a ratio of stolen value to target value
        var progress = (float)(totalStolenValue / condition.Comp.TargetStolenValue);
        args.Progress = Math.Clamp(progress, 0.0f, 1.0f);
    }

    private double GetBeaconAreaValue(EntityUid beacon, ICommonSession? targetPlayer, HashSet<EntityUid> appraisedEntities)
    {
        if (!TryComp<StealAreaComponent>(beacon, out var area))
            return 0;

        if (targetPlayer == null)
            return 0;

        double totalValue = 0.0;
        var nearestEnts = new HashSet<Entity<TransformComponent>>();
        var xform = Transform(beacon);

        // Get all entities within the area's range
        _lookup.GetEntitiesInRange<TransformComponent>(xform.Coordinates, area.Range, nearestEnts);

        foreach (var ent in nearestEnts)
        {
            // Skip if already appraised (on thief's ship)
            if (appraisedEntities.Contains(ent.Owner))
                continue;

            // Check if the entity has StolenCargoComponent
            if (!TryComp<StolenCargoComponent>(ent, out var stolenComp))
                continue;

            // Check if the item was stolen from the target player
            if (stolenComp.LastOwner != targetPlayer)
                continue;

            // Optional: Check if unobstructed
            if (!_interaction.InRangeUnobstructed((beacon, xform), (ent, ent.Comp), range: area.Range))
                continue;

            // Get the value of this item
            var itemValue = _pricing.GetPrice(ent.Owner);
            totalValue += itemValue;
            appraisedEntities.Add(ent.Owner);
        }

        return totalValue;
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
            }
        }
    }

    /// <summary>
    /// Allows players with active steal cargo objectives to set beacon coordinates.
    /// Also allows players with the bounty antagonist implant to use beacons.
    /// </summary>
    private void OnCanSetBeaconCoordinate(CanSetBeaconCoordinateEvent ev)
    {
        // First, check if the user has the bounty antagonist implant
        if (HasComp<BountyAntagonistImplantComponent>(ev.User))
        {
            ev.CanSet = true;
            return;
        }

        // Check if the user has an active steal cargo objective
        var query = AllEntityQuery<StealCargoObjectiveComponent>();
        while (query.MoveNext(out var objectiveUid, out var objective))
        {
            // Skip if not configured yet
            if (objective.PlayerToStealFrom == null)
                continue;

            // Get the player entity from the objective
            if (objective.GetPlayerEntity() is not { Valid: true } playerEntity)
                continue;

            // Check if this objective's player matches the user requesting beacon access
            // The user should be the thief, so we need to find the mind associated with them
            if (!_mind.TryGetMind(ev.User, out var mindId, out _))
                continue;

            // Get the thief's mind entity
            if (!TryComp<MindComponent>(mindId, out var thiefMind))
                continue;

            // The player in the objective should be the victim, not the thief
            // The thief is the one accessing the beacon, so if they have a steal cargo objective, grant access
            if (thiefMind.OwnedEntity == ev.User)
            {
                ev.CanSet = true;
                return;
            }
        }
    }
}
