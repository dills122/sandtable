using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSourceLibyanMember(string Id, string? ParentId, string SourceCode,
    int CapabilityPoints, IReadOnlyList<CampaignGroupedComponent>? Components);
internal sealed record CampaignSourceLibyanGroup(string Id, string RootId, string LocationId,
    string PoolId, IReadOnlyList<CampaignSourceLibyanMember> Members);

/// <summary>Source-bound provisional initial hierarchies; owner confirmation is separate.</summary>
internal static class CampaignSourceLibyanGroupedDefinition
{
    internal const string FirstId = "1-libyan-division";
    internal const string SecondId = "2-libyan-division";
    private static readonly IReadOnlyList<CampaignSourceLibyanGroup> Data = Array.AsReadOnly(new[]
    {
        Build(FirstId, "1-libyan-division-hq", "c4020", "axis-c4020-attached",
            Cna1979GrazianiSourceFacts.FirstLibyanDivisionChartCandidate),
        Build(SecondId, "2-libyan-division-hq", "c3920", "axis-c3920-attached",
            Cna1979GrazianiSourceFacts.SecondLibyanDivisionChartCandidate),
    });
    internal static IReadOnlyList<CampaignSourceLibyanGroup> Groups => Data;

    private static CampaignSourceLibyanGroup Build(string id, string root, string location, string pool, ContentInitialGroup source)
    {
        var seeds = Cna1979GrazianiSourceFacts.CreateLibyanDivisionInfantryToeSeeds("graziani")
            .Concat(Cna1979GrazianiSourceFacts.CreateLibyanDivisionSupportToeSeeds("graziani"))
            .Append(Cna1979GrazianiSourceFacts.CreateAttachedNinthLibyanTankToeSeed("graziani"))
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        string? Parent(string element) => element == root ? null : element switch
        {
            "8-1-libyan-infantry" or "9-1-libyan-infantry" or "10-1-libyan-infantry" => "1-libyan-regiment-hq",
            "11-2-libyan-infantry" or "12-2-libyan-infantry" or "13-2-libyan-infantry" => "2-libyan-regiment-hq",
            "2-3-libyan-infantry" or "6-3-libyan-infantry" or "7-3-libyan-infantry" => "3-libyan-regiment-hq",
            "14-4-libyan-infantry" or "15-4-libyan-infantry" or "16-4-libyan-infantry" => "4-libyan-regiment-hq",
            _ => root,
        };
        var members = source.ElementIds.Select(element =>
        {
            var code = element == root ? "b" : element.EndsWith("-regiment-hq", StringComparison.Ordinal) ? "h"
                : element.EndsWith("-infantry", StringComparison.Ordinal) ? "x"
                : element.EndsWith("-artillery", StringComparison.Ordinal) ? "jj"
                : element == "1-libyan-antitank" ? "rr" : element == "ix-libyan-light-tank" ? "k"
                : throw new InvalidOperationException("Unsupported Libyan source member.");
            var cpa = code switch { "b" or "k" => 30, "h" => 20, "x" => 10, "jj" or "rr" => 15, _ => throw new InvalidOperationException() };
            IReadOnlyList<CampaignGroupedComponent>? components = null;
            if (seeds.TryGetValue(element, out var seed))
                components = Array.AsReadOnly(seed.InitialComponentToes.Select(value =>
                    new CampaignGroupedComponent(value.ComponentId[(element.Length + 1)..], value.CurrentToe))
                    .OrderBy(value => value.ComponentType == "75-27-gun" ? 0 : 1)
                    .ThenBy(value => value.ComponentType, StringComparer.Ordinal).ToArray());
            return new CampaignSourceLibyanMember(element, Parent(element), code, cpa, components);
        }).OrderBy(value => value.Id, StringComparer.Ordinal).ToArray();
        if (source.LocationId != location || source.ElementIds.Count != 11 || members.Length != 11)
            throw new InvalidOperationException("Libyan source group differs from pinned scenario roster.");
        return new(id, root, location, pool, Array.AsReadOnly(members));
    }

    internal static IReadOnlyList<CampaignGroupedFormation> Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var supplement = state.AxisSupplement ?? throw new InvalidOperationException("Axis source supplement missing.");
        if (supplement.Hash() != CampaignSourceAxisSupplementalForces.ContentHash)
            throw new InvalidOperationException("Libyan setup requires the pinned source supplement.");
        var init = state.World.Initialization;
        return Array.AsReadOnly(Data.Select(group =>
        {
            if (state.AxisMotorization.Remaining(group.PoolId) != 15)
                throw new InvalidOperationException("Libyan attached motor pool differs from source.");
            var members = group.Members.Select(member =>
            {
                var force = supplement.Forces.SingleOrDefault(value => value.ElementId == member.Id);
                if (force is null || force.LocationId != group.LocationId || !MatchesToe(force, member)
                    || init.CapabilityPointsExpended > new CapabilityPointAmount(member.CapabilityPoints, 1))
                    throw new InvalidOperationException("Libyan source member differs from retained setup.");
                var components = member.Components;
                var primary = components is { Count: > 0 } ? components[0] : null;
                return new CampaignGroupedMember(member.Id, member.ParentId, member.SourceCode == "x" ? components![0].Toe : null,
                    0, false, primary?.ComponentType, primary?.Toe,
                    member.CapabilityPoints, init.CapabilityPointsExpended, init.CohesionLevel,
                    components is { Count: > 1 } ? Array.AsReadOnly(components.Skip(1).ToArray()) : null);
            });
            return new CampaignGroupedFormation(group.Id, "axis", group.RootId, group.LocationId, null, 15, members, false);
        }).ToArray());
    }

    private static bool MatchesToe(CampaignSourceInitialForce force, CampaignSourceLibyanMember member)
    {
        if (member.Components is null) return force.InitialToes is null;
        return force.InitialToes is not null && force.InitialToes.Count == member.Components.Count
            && member.Components.All(component => force.InitialToes.Any(value => value.ComponentId == member.Id + "." + component.ComponentType
                && value.Toe == component.Toe));
    }

    internal static void Validate(CampaignSourceAuthorityState state, CampaignGroupedWorld world)
    {
        ArgumentNullException.ThrowIfNull(state); ArgumentNullException.ThrowIfNull(world);
        if (world.ContentHash != (world.Groups.Any(value => value.GroupId == CampaignSourceCireneMarmaricaGroupedDefinition.CireneId)
                ? ContentSourceCireneMarmaricaGroupedPackArtifact.ContentHash : ContentSourceLibyanGroupedPackArtifact.ContentHash)
            || state.AxisSupplement?.Hash() != CampaignSourceAxisSupplementalForces.ContentHash)
            throw new InvalidOperationException("Libyan grouped world content identity differs from pinned source.");
        foreach (var source in Data)
        {
            var group = world.Groups.SingleOrDefault(value => value.GroupId == source.Id)
                ?? throw new InvalidOperationException("Libyan source group is missing.");
            if (group.SideId != "axis" || group.RootId != source.RootId || group.LocationId != source.LocationId
                || group.MotorPool != 15 || state.AxisMotorization.Remaining(source.PoolId) != 15
                || group.StackValue != (group.DispositionConfirmed ? 5 : null) || group.Members.Count != source.Members.Count
                || source.Members.Any(member => !group.Members.Any(actual => MatchesMember(actual, member, world.Prior.Initialization))))
                throw new InvalidOperationException("Libyan group differs from retained source roster or unverified stack.");
        }
    }

    private static bool MatchesMember(CampaignGroupedMember actual, CampaignSourceLibyanMember source, CampaignWorldV8Initialization init)
    {
        var components = source.Components;
        var primary = components is { Count: > 0 } ? components[0] : null;
        var extras = components is { Count: > 1 } ? components.Skip(1) : [];
        return actual.ElementId == source.Id && actual.ParentId == source.ParentId
            && actual.ComponentType == primary?.ComponentType && actual.ComponentToe == primary?.Toe
            && (actual.AdditionalComponents ?? []).SequenceEqual(extras)
            && actual.InfantryToe == (source.SourceCode == "x" ? components![0].Toe : null)
            && !actual.HistoricalMotorPriority && actual.AssignedMotorPoints == 0
            && actual.CapabilityPointAllowance == source.CapabilityPoints
            && actual.CapabilityPointsExpended == init.CapabilityPointsExpended && actual.CohesionLevel == init.CohesionLevel;
    }

    internal static byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", "graziani.libyan-divisions-grouped.v1");
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("axisChartsHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("landRulesHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("airRulesHash", "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4");
            writer.WriteString("sources", "scenario:60.31;axis:oa-libyan-p5,oa-libyan-tank-command-p6,4.46b;land:4.45,9.26-9.27,19.5,19.32,32.54;air:counter-section-2-p40");
            writer.WriteString("hierarchyPolicy", "provisional-owner-select-source-assigned-regiments-and-scenario-ix-l-attached");
            writer.WriteString("ixLAssignment", "trivoli-regiment-libyan-tank-command");
            writer.WriteString("ixLTraining", "scenario-trained");
            writer.WriteString("motorPoolPolicy", "medium-only-light-excluded-land-only-59.63b");
            writer.WriteString("stackPolicy", "unconfirmed-unknown-confirmed-full-division-five-9.26-9.27-19.32"); writer.WriteBoolean("globalPriorityComplete", false);
            writer.WriteStartArray("groups");
            foreach (var group in Data)
            {
                writer.WriteStartObject(); writer.WriteString("groupId", group.Id); writer.WriteString("rootId", group.RootId);
                writer.WriteString("locationId", group.LocationId); writer.WriteString("poolId", group.PoolId); writer.WriteNumber("poolPoints", 15);
                writer.WriteNumber("sourceLightTrucks", group.Id == FirstId ? 10 : 15); writer.WriteNumber("sourceMediumTrucks", 15);
                writer.WriteStartArray("members");
                foreach (var member in group.Members)
                {
                    writer.WriteStartObject(); writer.WriteString("elementId", member.Id);
                    if (member.ParentId is null) writer.WriteNull("parentId"); else writer.WriteString("parentId", member.ParentId);
                    writer.WriteString("sourceCode", member.SourceCode); writer.WriteNumber("cpa", member.CapabilityPoints);
                    if (member.Components is null) writer.WriteNull("components");
                    else { writer.WriteStartArray("components"); foreach (var component in member.Components)
                        { writer.WriteStartObject(); writer.WriteString("type", component.ComponentType); writer.WriteNumber("toe", component.Toe); writer.WriteEndObject(); } writer.WriteEndArray(); }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
