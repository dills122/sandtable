using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSourceItalianDivisionMember(string Id, string? ParentId, string SourceCode,
    int CapabilityPoints, IReadOnlyList<CampaignGroupedComponent>? Components);
internal sealed record CampaignSourceItalianDivisionGroup(string Id, string RootId, string LocationId,
    string PoolId, int MotorPoints, IReadOnlyList<CampaignSourceItalianDivisionMember> Members);

/// <summary>Owner-confirmable source hierarchies for the two deployed Italian divisions.</summary>
internal static class CampaignSourceCireneMarmaricaGroupedDefinition
{
    internal const string CireneId = "cirene-division";
    internal const string MarmaricaId = "marmarica-division";
    private static readonly IReadOnlyList<CampaignSourceItalianDivisionGroup> Data = Array.AsReadOnly(new[]
    {
        Build(CireneId, "63-cirene-hq", "c4120", "axis-c4120-attached", 50,
            Cna1979GrazianiSourceFacts.CireneDivisionChartCandidate),
        Build(MarmaricaId, "62-marmarica-hq", "c3918", "axis-c3918-attached", 45,
            Cna1979GrazianiSourceFacts.MarmaricaDivisionChartCandidate),
    });
    internal static IReadOnlyList<CampaignSourceItalianDivisionGroup> Groups => Data;

    private static CampaignSourceItalianDivisionGroup Build(string id, string root, string location,
        string pool, int motorPoints, ContentInitialGroup source)
    {
        var seeds = Cna1979GrazianiSourceFacts.CreateCireneMarmaricaInfantryToeSeeds("graziani")
            .Concat(Cna1979GrazianiSourceFacts.CreateCireneMarmaricaAntiTankToeSeeds("graziani"))
            .Concat(Cna1979GrazianiSourceFacts.CreateCireneMarmaricaArtilleryToeSeeds("graziani"))
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        string? Parent(string element) => element == root ? null : element switch
        {
            "1-157-infantry" or "2-157-infantry" or "3-157-infantry" => "157-infantry-hq",
            "1-158-infantry" or "2-158-infantry" or "3-158-infantry" => "158-infantry-hq",
            "1-115-infantry" or "2-115-infantry" or "3-115-infantry" => "115-infantry-hq",
            "1-116-infantry" or "2-116-infantry" or "3-116-infantry" => "116-infantry-hq",
            _ => root,
        };
        var members = source.ElementIds.Select(element =>
        {
            var code = element == root ? "b" : element.EndsWith("-infantry-hq", StringComparison.Ordinal) ? "h"
                : element.EndsWith("-infantry", StringComparison.Ordinal) ? "u"
                : element.EndsWith("-machine-gun", StringComparison.Ordinal) ? "bb"
                : element.EndsWith("-antitank", StringComparison.Ordinal) ? "rr"
                : element.EndsWith("-artillery", StringComparison.Ordinal) ? "kk"
                : element.EndsWith("-engineer", StringComparison.Ordinal) ? "bbb"
                : element == "63-bersaglieri" ? "aaa" : element is "lxiii-light-tank" or "lxii-light-tank" ? "k"
                : throw new InvalidOperationException("Unsupported source division member.");
            var cpa = code switch
            {
                "b" or "k" => 30,
                "h" => 20,
                "u" => 10,
                "bb" => 8,
                "rr" or "kk" => 15,
                "bbb" or "aaa" => 25,
                _ => throw new InvalidOperationException()
            };
            IReadOnlyList<CampaignGroupedComponent>? components = null;
            if (seeds.TryGetValue(element, out var seed))
                components = Array.AsReadOnly(seed.InitialComponentToes.Select(value =>
                    new CampaignGroupedComponent(value.ComponentId[(element.Length + 1)..], value.CurrentToe))
                    .OrderBy(value => value.ComponentType, StringComparer.Ordinal).ToArray());
            else if (code == "h") components = Array.AsReadOnly(new[] { new CampaignGroupedComponent("65-17-gun", 1) });
            else if (code == "bb") components = Array.AsReadOnly(new[] { new CampaignGroupedComponent("infantry", 2) });
            else if (code == "aaa") components = Array.AsReadOnly(new[] { new CampaignGroupedComponent("reconnaissance", 1) });
            else if (code == "k") components = Array.AsReadOnly(new[] { new CampaignGroupedComponent("cv33", 9) });
            return new CampaignSourceItalianDivisionMember(element, Parent(element), code, cpa, components);
        }).OrderBy(value => value.Id, StringComparer.Ordinal).ToArray();
        if (source.LocationId != location || members.Length != (id == CireneId ? 15 : 14))
            throw new InvalidOperationException("Italian source formation differs from pinned scenario roster.");
        return new(id, root, location, pool, motorPoints, Array.AsReadOnly(members));
    }

    internal static IReadOnlyList<CampaignGroupedFormation> Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var supplement = state.AxisSupplement ?? throw new InvalidOperationException("Axis source supplement missing.");
        if (supplement.Hash() != CampaignSourceAxisSupplementalForces.ContentHash)
            throw new InvalidOperationException("Italian formations require the pinned source supplement.");
        var initialization = state.World.Initialization;
        return Array.AsReadOnly(Data.Select(group =>
        {
            if (state.AxisMotorization.Remaining(group.PoolId) != group.MotorPoints)
                throw new InvalidOperationException("Italian formation motor pool differs from source.");
            var members = group.Members.Select(member =>
            {
                var force = supplement.Forces.SingleOrDefault(value => value.ElementId == member.Id);
                if (force is null || force.LocationId != group.LocationId || !MatchesRetainedToe(force, member)
                    || initialization.CapabilityPointsExpended > new CapabilityPointAmount(member.CapabilityPoints, 1))
                    throw new InvalidOperationException("Italian source member differs from retained setup.");
                var components = member.Components;
                var primary = components is { Count: > 0 } ? components[0] : null;
                return new CampaignGroupedMember(member.Id, member.ParentId,
                    member.SourceCode is "u" or "bb" ? primary!.Toe : null, 0, false,
                    primary?.ComponentType, primary?.Toe, member.CapabilityPoints,
                    initialization.CapabilityPointsExpended, initialization.CohesionLevel,
                    components is { Count: > 1 } ? Array.AsReadOnly(components.Skip(1).ToArray()) : null);
            });
            return new CampaignGroupedFormation(group.Id, "axis", group.RootId, group.LocationId,
                null, group.MotorPoints, members, false);
        }).ToArray());
    }

    private static bool MatchesRetainedToe(CampaignSourceInitialForce force, CampaignSourceItalianDivisionMember member) =>
        force.InitialToes is null || member.Components is not null && force.InitialToes.All(value =>
            member.Components.Any(component => value.ComponentId == member.Id + "." + component.ComponentType && value.Toe == component.Toe));

    internal static void Validate(CampaignSourceAuthorityState state, CampaignGroupedWorld world)
    {
        ArgumentNullException.ThrowIfNull(state); ArgumentNullException.ThrowIfNull(world);
        if (world.ContentHash != ContentSourceCireneMarmaricaGroupedPackArtifact.ContentHash
            || state.AxisSupplement?.Hash() != CampaignSourceAxisSupplementalForces.ContentHash)
            throw new InvalidOperationException("Italian grouped content differs from pinned source.");
        foreach (var source in Data)
        {
            var group = world.Groups.SingleOrDefault(value => value.GroupId == source.Id)
                ?? throw new InvalidOperationException("Italian source group is missing.");
            if (group.SideId != "axis" || group.RootId != source.RootId || group.LocationId != source.LocationId
                || group.MotorPool != source.MotorPoints || state.AxisMotorization.Remaining(source.PoolId) != source.MotorPoints
                || group.StackValue != (group.DispositionConfirmed ? 5 : null) || group.Members.Count != source.Members.Count
                || source.Members.Any(member => !group.Members.Any(actual => MatchesMember(actual, member, world.Prior.Initialization))))
                throw new InvalidOperationException("Italian group differs from retained source roster or disposition.");
        }
    }

    private static bool MatchesMember(CampaignGroupedMember actual, CampaignSourceItalianDivisionMember source, CampaignWorldV8Initialization init)
    {
        var components = source.Components;
        var primary = components is { Count: > 0 } ? components[0] : null;
        var extras = components is { Count: > 1 } ? components.Skip(1) : [];
        return actual.ElementId == source.Id && actual.ParentId == source.ParentId
            && actual.ComponentType == primary?.ComponentType && actual.ComponentToe == primary?.Toe
            && (actual.AdditionalComponents ?? []).SequenceEqual(extras)
            && actual.InfantryToe == (source.SourceCode is "u" or "bb" ? primary!.Toe : null)
            && !actual.HistoricalMotorPriority && actual.AssignedMotorPoints == 0
            && actual.CapabilityPointAllowance == source.CapabilityPoints
            && actual.CapabilityPointsExpended == init.CapabilityPointsExpended && actual.CohesionLevel == init.CohesionLevel;
    }

    internal static byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", "graziani.cirene-marmarica-grouped.v1");
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("axisChartsHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("errataHash", "sha256:5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb");
            writer.WriteString("landRulesHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("airRulesHash", "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4");
            writer.WriteString("sources", "scenario:60.31;errata:oa-cirene-p4,oa-marmarica-p4;axis:oa-libyan-tank-command-p6,4.46b;land:4.45,9.26-9.27,19.5,19.32,32.54,32.83;air:counter-section-1-p40");
            writer.WriteString("hierarchyPolicy", "provisional-owner-select-source-assigned-regiments-and-scenario-attached-tank");
            writer.WriteString("tankAssignment", "aresca-regiment-libyan-tank-command"); writer.WriteString("tankTraining", "scenario-trained");
            writer.WriteString("engineerToe", "unresolved"); writer.WriteString("lightAaPolicy", "kk-light-aa-retained-land-32.83");
            writer.WriteString("stackPolicy", "unconfirmed-unknown-confirmed-full-division-five-9.26-9.27-19.32");
            writer.WriteBoolean("globalPriorityComplete", false); writer.WriteStartArray("groups");
            foreach (var group in Data)
            {
                writer.WriteStartObject(); writer.WriteString("groupId", group.Id); writer.WriteString("rootId", group.RootId);
                writer.WriteString("locationId", group.LocationId); writer.WriteString("poolId", group.PoolId);
                writer.WriteNumber("poolPoints", group.MotorPoints); writer.WriteNumber("sourceMediumTrucks", group.Id == CireneId ? 45 : 40);
                writer.WriteNumber("sourceHeavyTrucks", 5); writer.WriteStartArray("members");
                foreach (var member in group.Members)
                {
                    writer.WriteStartObject(); writer.WriteString("elementId", member.Id);
                    if (member.ParentId is null) writer.WriteNull("parentId"); else writer.WriteString("parentId", member.ParentId);
                    writer.WriteString("sourceCode", member.SourceCode); writer.WriteNumber("cpa", member.CapabilityPoints);
                    if (member.Components is null) writer.WriteNull("components");
                    else
                    {
                        writer.WriteStartArray("components"); foreach (var component in member.Components)
                        { writer.WriteStartObject(); writer.WriteString("type", component.ComponentType); writer.WriteNumber("toe", component.Toe); writer.WriteEndObject(); }
                        writer.WriteEndArray();
                    }
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
