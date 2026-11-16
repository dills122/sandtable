using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSourceMalettiMember(string Id, string? ParentId, string SourceCode,
    string? Component, int? Toe, int CapabilityPoints, bool HistoricalMotorPriority);

internal static class CampaignSourceMalettiGroupedDefinition
{
    internal const string GroupId = "maletti-division";
    internal const string RootId = "maletti-hq";
    internal const string LocationId = "c3617";
    internal const string PoolId = CampaignSourceAxisPriorityMotorization.PoolId;
    private static readonly IReadOnlyList<CampaignSourceMalettiMember> Data = Array.AsReadOnly(
        CampaignSourceMalettiHierarchy.SourceAttachments.Select(value =>
        {
            var code = value.Id switch
            {
                "1-libyan-motor-regiment-hq" or "5-libyan-regiment-hq" or "sahariano-regiment-hq" => "f",
                "1-1-libyan-motor-infantry" or "3-1-libyan-motor-infantry" or "5-1-libyan-motor-infantry" => "w",
                "17-5-libyan-infantry" or "19-5-libyan-infantry" => "x",
                "1-sahariano-camel" or "2-sahariano-camel" or "3-sahariano-camel" => "zz",
                "1-maletti-artillery" or "2-maletti-artillery" => "mm",
                "ii-medium-tank" => "l",
                _ => throw new InvalidOperationException("Unrecognized source Maletti member."),
            };
            var cpa = code switch { "f" => 20, "w" or "x" or "zz" => 10, "mm" => 15, "l" => 30, _ => throw new InvalidOperationException() };
            return new CampaignSourceMalettiMember(value.Id, value.Parent, code, value.Component, value.Toe, cpa, code == "w");
        }).Append(new CampaignSourceMalettiMember(RootId, null, "b", null, null, 30, false))
        .OrderBy(value => value.Id, StringComparer.Ordinal).ToArray());
    internal static IReadOnlyList<CampaignSourceMalettiMember> Members => Data;

    internal static CampaignGroupedFormation Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var hierarchy = state.MalettiHierarchy ?? throw new InvalidOperationException("Maletti hierarchy missing.");
        var priority = state.AxisPriorityMotorization ?? throw new InvalidOperationException("Maletti priority state missing.");
        if (!hierarchy.Confirmed || state.AxisMotorization.Remaining(PoolId) != 35)
            throw new InvalidOperationException("Maletti confirmed hierarchy and retained pool are required.");
        hierarchy.Validate(state); _ = priority.Remaining(state.AxisMotorization);
        var initialization = state.World.Initialization;
        if (Data.Any(value => initialization.CapabilityPointsExpended > new CapabilityPointAmount(value.CapabilityPoints, 1)))
            throw new InvalidOperationException("Initial expenditure exceeds a Maletti source allowance.");
        var members = Data.Where(value => value.Id != hierarchy.DetachedId).Select(value => new CampaignGroupedMember(
            value.Id, value.ParentId, value.Component == "infantry" ? value.Toe : null,
            0, value.HistoricalMotorPriority, value.Component, value.Toe, value.CapabilityPoints,
            initialization.CapabilityPointsExpended, initialization.CohesionLevel));
        return new CampaignGroupedFormation(GroupId, "axis", RootId, LocationId, 5, 35, members);
    }

    internal static (CampaignDetachedGroupedMember? Detached, CampaignGroupedExternalReservation? External) CreateDetached(CampaignSourceAuthorityState state)
    {
        var hierarchy = state.MalettiHierarchy ?? throw new InvalidOperationException("Maletti hierarchy missing.");
        var priority = state.AxisPriorityMotorization ?? throw new InvalidOperationException("Maletti priority state missing.");
        if (hierarchy.DetachedId is not { } id) return (null, null);
        var source = Data.Single(value => value.Id == id && value.HistoricalMotorPriority && value.Toe == 5);
        var points = priority.AssignedPoints(id);
        var active = state.World.World.Elements.Count(value => value.ElementId == id && value.CurrentLocationId == LocationId);
        var reps = state.World.World.Representations.Count(value => value.CurrentLocationId == LocationId
            && value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement && value.BoundElementIds.SequenceEqual([id]));
        if (active == 1 && reps == 1 && state.CertifiedMalettiWorld is not null)
            return (null, new CampaignGroupedExternalReservation(GroupId, id, 5, points));
        if (active != 0 || reps != 0 || state.CertifiedMalettiWorld is not null)
            throw new InvalidOperationException("Maletti detachment conflicts with active world.");
        var init = state.World.Initialization;
        return (new CampaignDetachedGroupedMember(GroupId, "axis", LocationId, 1,
            new CampaignGroupedMember(id, null, 5, points, true, "infantry", 5,
                source.CapabilityPoints, init.CapabilityPointsExpended, init.CohesionLevel)), null);
    }

    internal static void Validate(CampaignSourceAuthorityState state, CampaignGroupedWorld world)
    {
        ArgumentNullException.ThrowIfNull(state); ArgumentNullException.ThrowIfNull(world);
        var hierarchy = state.MalettiHierarchy ?? throw new InvalidOperationException("Maletti hierarchy missing.");
        var priority = state.AxisPriorityMotorization ?? throw new InvalidOperationException("Maletti priority missing.");
        var group = world.Groups.SingleOrDefault(value => value.GroupId == GroupId);
        if (!hierarchy.Confirmed || group is null || group.SideId != "axis" || group.RootId != RootId || group.LocationId != LocationId
            || group.StackValue != 5 || group.MotorPool != 35 || !group.DispositionConfirmed
            || state.AxisMotorization.Remaining(PoolId) != 35 || world.RemainingMotorPoints(GroupId) != priority.Remaining(state.AxisMotorization)
            || !group.Members.Select(value => value.ElementId).Order(StringComparer.Ordinal)
                .SequenceEqual(Data.Where(value => value.Id != hierarchy.DetachedId).Select(value => value.Id).Order(StringComparer.Ordinal))
            || Data.Where(value => value.Id != hierarchy.DetachedId).Any(source => !group.Members.Any(member =>
                member.ElementId == source.Id && member.ParentId == source.ParentId && member.ComponentType == source.Component
                && member.ComponentToe == source.Toe && member.AdditionalComponents is not { Count: > 0 } && member.InfantryToe == (source.Component == "infantry" ? source.Toe : null)
                && member.HistoricalMotorPriority == source.HistoricalMotorPriority && member.AssignedMotorPoints == 0
                && member.CapabilityPointAllowance == source.CapabilityPoints
                && member.CapabilityPointsExpended == world.Prior.Initialization.CapabilityPointsExpended
                && member.CohesionLevel == world.Prior.Initialization.CohesionLevel))
            || world.Detached.Where(value => value.GroupId == GroupId).Any(value =>
                value.Member.ElementId != hierarchy.DetachedId || value.SideId != "axis" || value.LocationId != LocationId
                || value.StackValue != 1 || value.Member.ParentId is not null || value.Member.ComponentType != "infantry"
                || value.Member.ComponentToe != 5 || value.Member.InfantryToe != 5 || value.Member.AdditionalComponents is { Count: > 0 } || !value.Member.HistoricalMotorPriority
                || value.Member.AssignedMotorPoints != priority.AssignedPoints(value.Member.ElementId)
                || value.Member.CapabilityPointAllowance != 10
                || value.Member.CapabilityPointsExpended != world.Prior.Initialization.CapabilityPointsExpended
                || value.Member.CohesionLevel != world.Prior.Initialization.CohesionLevel)
            || world.ExternalReservations.Where(value => value.GroupId == GroupId).Any(value =>
                value.ElementId != hierarchy.DetachedId || value.InfantryToe != 5 || value.AssignedMotorPoints != priority.AssignedPoints(value.ElementId))
            || world.ContentHash != (world.Groups.Any(value => value.GroupId == CampaignSourceCcnnBatch.FirstId) ? Cna.Core.Content.ContentSourceCcnnBatchPackArtifact.HashFor(world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId), world.Groups.Any(value => value.GroupId == CampaignSourceArescaGroupedDefinition.GroupId)) : world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId) ? Cna.Core.Content.ContentSourceSixthAustralianGroupedPackArtifact.ContentHash : world.Groups.Any(value => value.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId) ? ContentSourceCatanzaroGroupedPackArtifact.ContentHash : world.Groups.Any(value => value.GroupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId) ? ContentSourceSixteenthBrigadeGroupedPackArtifact.ContentHash : world.Groups.Any(value => value.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId)
                ? ContentSourceSeventhArmoredGroupedPackArtifact.ContentHash : world.Groups.Any(value => value.GroupId == CampaignSourceCireneMarmaricaGroupedDefinition.CireneId)
                ? ContentSourceCireneMarmaricaGroupedPackArtifact.ContentHash : world.Groups.Any(value => value.GroupId == "1-libyan-division")
                ? ContentSourceLibyanGroupedPackArtifact.ContentHash : ContentSourceMalettiGroupedPackArtifact.ContentHash))
            throw new InvalidOperationException("Maletti grouped world differs from retained source choices.");
    }

    internal static byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", "graziani.maletti-grouped-world.v1");
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("axisChartsHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("landRulesHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("airRulesHash", "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4");
            writer.WriteString("sources", "scenario:60.31;axis:oa-maletti-p7,oa-libyan-tank-command-p6,4.46b;land:8.95-8.96,19.32,19.5,32.54;air:counter-section-2-p40");
            writer.WriteString("scenarioOverOaLxPolicy", "provisional-ii-m-only-maletti-lx-whole-bardia");
            writer.WriteString("repeated4ArPolicy", "provisional-one-unique-counter"); writer.WriteString("xxviIdentity", "provisional-unique-artillery-counter");
            writer.WriteString("saharianoNote", "camel-regiment-in-name-only-mobility-unresolved");
            writer.WriteString("groupId", GroupId); writer.WriteString("locationId", LocationId); writer.WriteString("poolId", PoolId);
            writer.WriteNumber("poolPoints", 35); writer.WriteBoolean("globalPriorityComplete", false); writer.WriteStartArray("members");
            foreach (var value in Data)
            {
                writer.WriteStartObject(); writer.WriteString("elementId", value.Id);
                if (value.ParentId is null) writer.WriteNull("parentId"); else writer.WriteString("parentId", value.ParentId);
                writer.WriteString("sourceCode", value.SourceCode);
                if (value.Component is null) writer.WriteNull("component"); else writer.WriteString("component", value.Component);
                if (value.Toe is int toe) writer.WriteNumber("toe", toe); else writer.WriteNull("toe");
                writer.WriteNumber("cpa", value.CapabilityPoints); writer.WriteBoolean("historicalMotorPriority", value.HistoricalMotorPriority);
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
