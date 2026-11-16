using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignSourceMatruhGroupedDefinition
{
    internal const string GroupId = "matruh-garrison";
    internal const string LocationId = "d3714";
    internal const string PoolId = "cw-matruh-attached";

    internal static CampaignGroupedFormation Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var setup = state.MatruhFormation ?? throw new InvalidOperationException("Matruh source setup is unavailable.");
        var motor = state.CommonwealthMotorization.Pools.SingleOrDefault(value => value.Id == PoolId);
        if (!setup.Confirmed || motor is null || motor.Points != 20 || !motor.Attached || motor.SourceLocation != LocationId
            || state.CommonwealthMotorization.Remaining(PoolId) != 20
            || state.AttachedInitialLoads is null || state.AttachedInitialLoads.Reserved(PoolId) != 0)
            throw new InvalidOperationException("Matruh source disposition or motor pool is incomplete.");
        setup.Validate(state);
        var init = state.World.Initialization;
        if (init.CapabilityPointsExpended > new CapabilityPointAmount(10, 1))
            throw new InvalidOperationException("Initial expenditure exceeds Matruh member allowance.");
        var members = setup.AttachedIds.Select(id => new CampaignGroupedMember(id, CampaignSourceMatruhFormation.HeadquartersId,
            6, 0, false, "infantry", 6, 10, init.CapabilityPointsExpended, init.CohesionLevel))
            .Append(new CampaignGroupedMember(CampaignSourceMatruhFormation.HeadquartersId, null, null,
                0, false, null, null, 30, init.CapabilityPointsExpended, init.CohesionLevel));
        return new CampaignGroupedFormation(GroupId, "commonwealth", CampaignSourceMatruhFormation.HeadquartersId,
            LocationId, 0, 20, members);
    }

    internal static CampaignDetachedGroupedMember? CreateDetached(CampaignSourceAuthorityState state)
    {
        var setup = state.MatruhFormation ?? throw new InvalidOperationException("Matruh source setup missing.");
        if (setup.DetachedId is not { } id) return null;
        var active = state.World.World.Elements.Count(value => value.ElementId == id && value.CurrentLocationId == LocationId);
        var representations = state.World.World.Representations.Count(value => value.CurrentLocationId == LocationId
            && value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement && value.BoundElementIds.SequenceEqual([id]));
        if (active == 1 && representations == 1 && state.MatruhDetachedWorld is not null) return null;
        if (active != 0 || representations != 0 || state.MatruhDetachedWorld is not null)
            throw new InvalidOperationException("Matruh detached state conflicts with the retained world.");
        var init = state.World.Initialization;
        return new CampaignDetachedGroupedMember(GroupId, "commonwealth", LocationId, 1,
            new CampaignGroupedMember(id, null, 6, 0, false, "infantry", 6, 10,
                init.CapabilityPointsExpended, init.CohesionLevel));
    }

    internal static void Validate(CampaignSourceAuthorityState state, CampaignGroupedWorld world)
    {
        ArgumentNullException.ThrowIfNull(state); ArgumentNullException.ThrowIfNull(world);
        var setup = state.MatruhFormation ?? throw new InvalidOperationException("Matruh source setup missing.");
        var group = world.Groups.SingleOrDefault(value => value.GroupId == GroupId);
        if (!setup.Confirmed || group is null || group.SideId != "commonwealth" || group.RootId != CampaignSourceMatruhFormation.HeadquartersId
            || group.LocationId != LocationId || group.StackValue != 0 || group.MotorPool != 20 || !group.DispositionConfirmed
            || state.CommonwealthMotorization.Remaining(PoolId) != 20 || state.AttachedInitialLoads is null
            || state.AttachedInitialLoads.Reserved(PoolId) != 0
            || !group.Members.Select(value => value.ElementId).Order(StringComparer.Ordinal)
                .SequenceEqual(setup.AttachedIds.Append(CampaignSourceMatruhFormation.HeadquartersId).Order(StringComparer.Ordinal))
            || group.Members.Any(member => member.AssignedMotorPoints != 0 || member.HistoricalMotorPriority
                || member.AdditionalComponents is { Count: > 0 }
                || member.CapabilityPointsExpended != world.Prior.Initialization.CapabilityPointsExpended
                || member.CohesionLevel != world.Prior.Initialization.CohesionLevel
                || (member.ElementId == CampaignSourceMatruhFormation.HeadquartersId
                    ? member.ParentId is not null || member.ComponentType is not null || member.ComponentToe is not null
                        || member.InfantryToe is not null || member.CapabilityPointAllowance != 30
                    : member.ParentId != CampaignSourceMatruhFormation.HeadquartersId || member.ComponentType != "infantry"
                        || member.ComponentToe != 6 || member.InfantryToe != 6 || member.CapabilityPointAllowance != 10))
            || world.Detached.Where(value => value.GroupId == GroupId).Any(value =>
                value.Member.ElementId != setup.DetachedId || value.SideId != "commonwealth" || value.LocationId != LocationId
                || value.StackValue != 1 || value.Member.ParentId is not null || value.Member.ComponentType != "infantry"
                || value.Member.ComponentToe != 6 || value.Member.InfantryToe != 6 || value.Member.AssignedMotorPoints != 0
                || value.Member.HistoricalMotorPriority || value.Member.AdditionalComponents is { Count: > 0 } || value.Member.CapabilityPointAllowance != 10
                || value.Member.CapabilityPointsExpended != world.Prior.Initialization.CapabilityPointsExpended
                || value.Member.CohesionLevel != world.Prior.Initialization.CohesionLevel)
            || world.ContentHash != (world.Groups.Any(value => value.GroupId == CampaignSourceCcnnBatch.FirstId) ? Cna.Core.Content.ContentSourceCcnnBatchPackArtifact.HashFor(world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId), world.Groups.Any(value => value.GroupId == CampaignSourceArescaGroupedDefinition.GroupId)) : world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId) ? Cna.Core.Content.ContentSourceSixthAustralianGroupedPackArtifact.ContentHash : world.Groups.Any(value => value.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId) ? ContentSourceCatanzaroGroupedPackArtifact.ContentHash : world.Groups.Any(value => value.GroupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId) ? ContentSourceSixteenthBrigadeGroupedPackArtifact.ContentHash : world.Groups.Any(value => value.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId) ? ContentSourceSeventhArmoredGroupedPackArtifact.ContentHash
                : world.Groups.Any(value => value.GroupId == CampaignSourceCireneMarmaricaGroupedDefinition.CireneId) ? ContentSourceCireneMarmaricaGroupedPackArtifact.ContentHash
                : world.Groups.Any(value => value.GroupId == "1-libyan-division") ? ContentSourceLibyanGroupedPackArtifact.ContentHash
                : world.Groups.Any(value => value.GroupId == CampaignSourceMalettiGroupedDefinition.GroupId)
                    ? ContentSourceMalettiGroupedPackArtifact.ContentHash : ContentSourceMatruhGroupedPackArtifact.ContentHash))
            throw new InvalidOperationException("Matruh grouped world differs from its retained source choices.");
    }
}
