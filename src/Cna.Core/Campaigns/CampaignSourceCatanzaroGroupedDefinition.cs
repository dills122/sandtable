using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSourceCatanzaroMember(string Id, string? ParentId, int CapabilityPoints,
    bool Infantry, IReadOnlyList<CampaignGroupedComponent>? Components);

/// <summary>Source hierarchy for the deployed 64th Catanzaro division.</summary>
internal static class CampaignSourceCatanzaroGroupedDefinition
{
    internal const string GroupId = "catanzaro-division";
    internal const string RootId = "64-catanzaro-hq";
    internal const string LocationId = "c4707";
    internal const string PoolId = "axis-c4707-attached";
    private static readonly IReadOnlyList<CampaignSourceCatanzaroMember> Data = Build();
    internal static IReadOnlyList<CampaignSourceCatanzaroMember> Members => Data;

    private static ReadOnlyCollection<CampaignSourceCatanzaroMember> Build()
    {
        var source = Cna1979GrazianiSourceFacts.CatanzaroInitialChartCandidate;
        var seeds = Cna1979GrazianiSourceFacts.CreateCatanzaroToeCandidates("graziani")
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        string? Parent(string id) => id == RootId ? null : id switch
        {
            "1-141-infantry" or "2-141-infantry" or "3-141-infantry" => "141-infantry-hq",
            "1-142-infantry" or "2-142-infantry" or "3-142-infantry" => "142-infantry-hq",
            _ => RootId,
        };
        var values = source.ElementIds.Select(id =>
        {
            var cpa = id == RootId ? 30 : id.EndsWith("-infantry-hq", StringComparison.Ordinal) ? 20
                : id.EndsWith("-infantry", StringComparison.Ordinal) ? 10
                : id == "64-machine-gun" ? 8 : id is "64-antitank" or "203-artillery" ? 15
                : id == "64-engineer" ? 25 : throw new InvalidOperationException("Unknown Catanzaro source member.");
            IReadOnlyList<CampaignGroupedComponent>? components = null;
            if (seeds.TryGetValue(id, out var seed) && seed.InitialComponentToes.Count > 0)
                components = Array.AsReadOnly(seed.InitialComponentToes.Select(value =>
                    new CampaignGroupedComponent(value.ComponentId[(id.Length + 1)..], value.CurrentToe))
                    .OrderBy(value => value.ComponentType, StringComparer.Ordinal).ToArray());
            return new CampaignSourceCatanzaroMember(id, Parent(id), cpa,
                id.EndsWith("-infantry", StringComparison.Ordinal) || id == "64-machine-gun", components);
        }).OrderBy(value => value.Id, StringComparer.Ordinal).ToArray();
        if (source.LocationId != LocationId || values.Length != 13) throw new InvalidOperationException("Catanzaro source roster differs.");
        return Array.AsReadOnly(values);
    }

    internal static CampaignGroupedFormation Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var supplement = state.AxisSupplement ?? throw new InvalidOperationException("Axis source supplement missing.");
        if (supplement.Hash() != CampaignSourceAxisSupplementalForces.ContentHash || state.AxisMotorization.Remaining(PoolId) != 35)
            throw new InvalidOperationException("Catanzaro source supplement or attached motor pool differs.");
        var init = state.World.Initialization;
        var members = Data.Select(source =>
        {
            var force = supplement.Forces.Single(value => value.ElementId == source.Id);
            if (force.LocationId != LocationId || init.CapabilityPointsExpended > new CapabilityPointAmount(source.CapabilityPoints, 1)
                || (force.InitialToes is null) != (source.Components is null)
                || (force.InitialToes is not null && (force.InitialToes.Count != source.Components!.Count
                    || force.InitialToes.Any(value => !source.Components!.Any(component => value.ComponentId == source.Id + "." + component.ComponentType && value.Toe == component.Toe)))))
                throw new InvalidOperationException("Catanzaro retained source member differs.");
            var primary = source.Components is { Count: > 0 } ? source.Components[0] : null;
            return new CampaignGroupedMember(source.Id, source.ParentId, source.Infantry ? primary!.Toe : null,
                0, false, primary?.ComponentType, primary?.Toe, source.CapabilityPoints,
                init.CapabilityPointsExpended, init.CohesionLevel,
                source.Components is { Count: > 1 } ? Array.AsReadOnly(source.Components.Skip(1).ToArray()) : null);
        });
        return new CampaignGroupedFormation(GroupId, "axis", RootId, LocationId, null, 35, members, false);
    }

    internal static void Validate(CampaignSourceAuthorityState state, CampaignGroupedWorld world)
    {
        var group = world.Groups.Single(value => value.GroupId == GroupId);
        var expected = Create(state);
        if (world.ContentHash != (world.Groups.Any(value => value.GroupId == CampaignSourceCcnnBatch.FirstId) ? Cna.Core.Content.ContentSourceCcnnBatchPackArtifact.HashFor(world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId), world.Groups.Any(value => value.GroupId == CampaignSourceArescaGroupedDefinition.GroupId)) : world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId) ? ContentSourceSixthAustralianGroupedPackArtifact.ContentHash : ContentSourceCatanzaroGroupedPackArtifact.ContentHash)
            || group.SideId != expected.SideId || group.RootId != expected.RootId || group.LocationId != expected.LocationId
            || group.MotorPool != expected.MotorPool || group.StackValue != (group.DispositionConfirmed ? 5 : null)
            || group.Members.Count != expected.Members.Count
            || group.Members.Zip(expected.Members).Any(pair => pair.First with { AdditionalComponents = null } != pair.Second with { AdditionalComponents = null }
                || !(pair.First.AdditionalComponents ?? []).SequenceEqual(pair.Second.AdditionalComponents ?? [])))
            throw new InvalidOperationException("Catanzaro group differs from retained source setup.");
    }

    internal static byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", "graziani.catanzaro-grouped.v1");
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("axisChartsHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("landRulesHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("airRulesHash", "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4");
            writer.WriteString("sources", "scenario:60.31;axis:oa-catanzaro-p10,4.46b,19.32;land:4.45,9.26-9.27,32.83;air:counter-section-1-p40");
            writer.WriteString("disposition", "provisional-owner-confirm-source-assigned-regiments");
            writer.WriteString("stackPolicy", "unconfirmed-unknown-confirmed-nonshell-printed-five");
            writer.WriteString("engineerToe", "unresolved"); writer.WriteString("lightAaPolicy", "kk-light-aa-retained-land-32.83");
            writer.WriteString("poolId", PoolId); writer.WriteNumber("motorPoints", 35);
            writer.WriteStartArray("members");
            foreach (var member in Data)
            {
                writer.WriteStartObject(); writer.WriteString("elementId", member.Id);
                if (member.ParentId is null) writer.WriteNull("parentId"); else writer.WriteString("parentId", member.ParentId);
                writer.WriteNumber("capabilityPointAllowance", member.CapabilityPoints); writer.WriteBoolean("infantry", member.Infantry);
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
        return stream.ToArray();
    }
    internal static string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
