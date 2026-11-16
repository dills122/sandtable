using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Owner-confirmable historical 7th Armored setup, after scenario detachments.</summary>
internal static class CampaignSourceSeventhArmoredGroupedDefinition
{
    internal const string GroupId = "7-armored-division";
    internal const string RootId = "7-armored-division-hq";
    internal const string LocationId = "d3612";
    internal const string PoolId = "cw-7-armored-attached";
    private static readonly IReadOnlyDictionary<string, string?> Parents = new Dictionary<string, string?>(StringComparer.Ordinal)
    {
        [RootId] = null,
        ["4-armored-brigade-hq"] = RootId,
        ["6-rtr"] = "4-armored-brigade-hq",
        ["7-queens-own-hussars"] = "4-armored-brigade-hq",
        ["7-armored-brigade-hq"] = RootId,
        ["8-hussars"] = "7-armored-brigade-hq",
        ["7-support-group-hq"] = RootId,
    };

    internal static CampaignGroupedFormation Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var source = Cna1979GrazianiSourceFacts.SeventhArmoredDivisionInitialCandidate;
        if (source.LocationId != LocationId || !source.ElementIds.Order(StringComparer.Ordinal).SequenceEqual(Parents.Keys.Order(StringComparer.Ordinal))
            || state.CommonwealthMotorization.Remaining(PoolId) != 10)
            throw new InvalidOperationException("Seventh Armored source roster or motor pool differs.");
        var allowances = Cna1979GrazianiCounterFacts.SeventhArmoredInitialCandidates.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var init = state.World.Initialization;
        var members = source.ElementIds.Select(id =>
        {
            var retained = state.CommonwealthInitialForces.Forces.Single(value => value.ElementId == id);
            var allowance = allowances[id];
            if (retained.LocationId != LocationId || init.CapabilityPointsExpended > new CapabilityPointAmount(allowance.CapabilityPointAllowance, 1))
                throw new InvalidOperationException("Seventh Armored retained source differs.");
            var component = retained.InitialToes?.SingleOrDefault();
            return new CampaignGroupedMember(id, Parents[id], null, 0, false,
                component?.ComponentId[(id.Length + 1)..], component?.Toe, allowance.CapabilityPointAllowance,
                init.CapabilityPointsExpended, init.CohesionLevel);
        });
        // The proposed disposition is not certified until owner confirmation.
        return new CampaignGroupedFormation(GroupId, "commonwealth", RootId, LocationId, null, 10, members, false);
    }

    internal static void Validate(CampaignSourceAuthorityState state, CampaignGroupedWorld world)
    {
        var group = world.Groups.Single(value => value.GroupId == GroupId);
        var expected = Create(state);
        if (world.ContentHash != (world.Groups.Any(value => value.GroupId == CampaignSourceCcnnBatch.FirstId) ? Cna.Core.Content.ContentSourceCcnnBatchPackArtifact.HashFor(world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId), world.Groups.Any(value => value.GroupId == CampaignSourceArescaGroupedDefinition.GroupId)) : world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId) ? Cna.Core.Content.ContentSourceSixthAustralianGroupedPackArtifact.ContentHash : world.Groups.Any(value => value.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId) ? ContentSourceCatanzaroGroupedPackArtifact.ContentHash : world.Groups.Any(value => value.GroupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId)
                ? ContentSourceSixteenthBrigadeGroupedPackArtifact.ContentHash : ContentSourceSeventhArmoredGroupedPackArtifact.ContentHash)
            || group.SideId != expected.SideId || group.RootId != expected.RootId || group.LocationId != expected.LocationId
            || group.MotorPool != expected.MotorPool || group.StackValue != (group.DispositionConfirmed ? (state.IsSeventhShellCorrected ? 3 : 5) : null)
            || !group.Members.SequenceEqual(expected.Members))
            throw new InvalidOperationException("Seventh Armored differs from its source setup or shell stacking ruling.");
    }

    internal static byte[] SerializeCanonical()
    {
        var allowances = Cna1979GrazianiCounterFacts.SeventhArmoredInitialCandidates.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var seeds = Cna1979GrazianiSourceFacts.CreateSeventhArmoredDivisionToeCandidates("graziani")
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("policyId", "graziani.seventh-armored-grouped.v1");
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("commonwealthChartsHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("landRulesHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("airRulesHash", "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4");
            writer.WriteString("sources", "scenario:60.41;commonwealth:oa-7-armored-p6,19.31;land:4.45,9.11-9.12,9.26-9.28,32.51;air:counter-section-7-p43");
            writer.WriteString("disposition", "provisional-owner-confirm-source-hierarchy-with-scenario-detachments");
            writer.WriteString("shellStatus", "division-shell-one-nonshell-brigade-of-three");
            writer.WriteString("stackStatus", "provisional-shell-stacks-printed-five-when-confirmed");
            writer.WriteString("poolId", PoolId); writer.WriteNumber("motorPoints", 10);
            writer.WriteStartArray("members");
            foreach (var id in Parents.Keys.Order(StringComparer.Ordinal))
            {
                writer.WriteStartObject(); writer.WriteString("elementId", id);
                if (Parents[id] is null) writer.WriteNull("parentId"); else writer.WriteString("parentId", Parents[id]);
                writer.WriteNumber("capabilityPointAllowance", allowances[id].CapabilityPointAllowance);
                if (!seeds.TryGetValue(id, out var seed) || seed.InitialComponentToes.Count == 0) writer.WriteNull("components");
                else
                {
                    writer.WriteStartArray("components");
                    foreach (var component in seed.InitialComponentToes.OrderBy(value => value.ComponentId, StringComparer.Ordinal))
                    { writer.WriteStartObject(); writer.WriteString("componentId", component.ComponentId); writer.WriteNumber("toe", component.CurrentToe); writer.WriteEndObject(); }
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
