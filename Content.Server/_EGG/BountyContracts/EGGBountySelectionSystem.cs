namespace Content.Server._EGG.BountyContracts;

/// <summary>
/// Currently handles antag bounties
/// </summary>
public sealed partial class EGGBountySelectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<DecideAntagBountiesEvent>(OnDecideAntagBounties);
    }

    //private void OnDecideAntagBounties(DecideAntagBountiesEvent ev)
    //{
    //    Log.Debug("Deciding antag bounties");

    //    var playerWithMostCargo = FindPlayerWithHighestCargoValue();
    //    if (playerWithMostCargo == null)
    //    {
    //        Log.Debug("No player found with cargo on their ship");
    //        return;
    //    }

    //    var (playerSession, cargoValue) = playerWithMostCargo.Value;
    //    Log.Debug($"Player {playerSession.Name} has the highest cargo value: {cargoValue}");
    //}
}
