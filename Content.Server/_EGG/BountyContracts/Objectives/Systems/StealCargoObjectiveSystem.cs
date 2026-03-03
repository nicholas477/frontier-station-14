using Content.Server._EGG.BountyContracts.Objectives.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;

namespace Content.Server._EGG.BountyContracts.Objectives.Systems;

public sealed partial class StealCargoObjectiveSystem : EntitySystem
{
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StealCargoObjectiveComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<StealCargoObjectiveComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<StealCargoObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
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

        //_metaData.SetEntityName(condition.Owner, title, args.Meta);
        _metaData.SetEntityDescription(condition.Owner, "test", args.Meta);
        //_objectives.SetIcon(condition.Owner, group.Sprite, args.Objective);
    }

    private void OnGetProgress(Entity<StealCargoObjectiveComponent> condition, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 0.0f;
    }
}
