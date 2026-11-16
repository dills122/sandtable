using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

/// <summary>Owner-confirmable source setup for the deployed 16th Infantry Brigade.</summary>
internal static class CampaignSourceSixteenthBrigadeGroupedDefinition
{
    internal const string GroupId = "16-infantry-brigade";
    internal const string RootId = "16-infantry-brigade-hq";
    internal const string LocationId = "e1829";
    internal const string PoolId = "cw-16-brigade-attached";

    internal static CampaignGroupedFormation Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var source = Cna1979GrazianiSourceFacts.SixteenthInfantryBrigadeInitialCandidate;
        var ids = new[] { RootId, "2-queens-royal-regiment", "2-leicesters", "1-argyll-sutherland" };
        if (source.LocationId != LocationId || !source.ElementIds.Order(StringComparer.Ordinal).SequenceEqual(ids.Order(StringComparer.Ordinal))
            || state.CommonwealthMotorization.Remaining(PoolId) != 20)
            throw new InvalidOperationException("Sixteenth Brigade source roster or motor pool differs.");
        var allowances = Cna1979GrazianiCounterFacts.CairoBrigadeCandidates.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var world = state.World.World;
        var scenario = state.Pregame.Source.Session.Content.Definition.LegacyDefinition.Scenarios
            .Single(value => value.ScenarioId == state.World.ScenarioId);
        var toes = Cna1979GrazianiSourceFacts.CreateSixteenthInfantryBrigadeToeCandidates(scenario.ScenarioId)
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var members = ids.Select(id =>
        {
            var element = world.Elements.Single(value => value.ElementId == id);
            var component = element.Components.Single();
            var sourceToe = toes[id].InitialComponentToes.Single();
            var type = component.ComponentId[(id.Length + 1)..];
            if (element.CurrentLocationId != LocationId || component.ComponentId != id + "." + type
                || type != (id == RootId ? "3-7-howitzer" : "infantry")
                || component.CurrentToe != (id == RootId ? 3 : 6)
                || component.ComponentId != sourceToe.ComponentId || component.CurrentToe != sourceToe.CurrentToe
                || component.InitialToeOrigin != sourceToe.Origin
                || element.OperationalState.LedgerGameTurn != scenario.Start.GameTurn
                || element.OperationalState.LedgerOperationStage != scenario.Start.OperationStage)
                throw new InvalidOperationException("Sixteenth Brigade retained source differs.");
            return new CampaignGroupedMember(id, id == RootId ? null : RootId,
                type == "infantry" ? component.CurrentToe : null, 0, false, type, component.CurrentToe,
                allowances[id].CapabilityPointAllowance, element.OperationalState.CapabilityPointsExpended,
                element.OperationalState.CohesionLevel);
        });
        return new CampaignGroupedFormation(GroupId, "commonwealth", RootId, LocationId, null, 20,
            members, dispositionConfirmed: false, usesRetainedMembers: true);
    }

    internal static void Validate(CampaignSourceAuthorityState state, CampaignGroupedWorld world)
    {
        var group = world.Groups.Single(value => value.GroupId == GroupId);
        var expected = Create(state);
        if (world.ContentHash != (world.Groups.Any(value => value.GroupId == CampaignSourceCcnnBatch.FirstId) ? Cna.Core.Content.ContentSourceCcnnBatchPackArtifact.HashFor(world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId), world.Groups.Any(value => value.GroupId == CampaignSourceArescaGroupedDefinition.GroupId)) : world.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId) ? Cna.Core.Content.ContentSourceSixthAustralianGroupedPackArtifact.ContentHash : world.Groups.Any(value => value.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId)
                ? ContentSourceCatanzaroGroupedPackArtifact.ContentHash : ContentSourceSixteenthBrigadeGroupedPackArtifact.ContentHash)
            || group.SideId != expected.SideId || group.RootId != expected.RootId || group.LocationId != expected.LocationId
            || group.MotorPool != expected.MotorPool || group.StackValue != (group.DispositionConfirmed ? 2 : null)
            || !group.UsesRetainedMembers || !group.Members.SequenceEqual(expected.Members))
            throw new InvalidOperationException("Sixteenth Brigade differs from its source setup or stacking ruling.");
    }

    internal static byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", "graziani.sixteenth-brigade-grouped.v1");
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("commonwealthChartsHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("landRulesHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("airRulesHash", "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4");
            writer.WriteString("sources", "scenario:60.41;commonwealth:oa-70-infantry-p9,19.31;land:9.26-9.27;air:counter-section-7-p43");
            writer.WriteString("disposition", "provisional-owner-confirm-source-hierarchy");
            writer.WriteString("stackStatus", "three-of-three-battalions-non-shell-printed-two-when-confirmed");
            writer.WriteString("groupId", GroupId); writer.WriteString("locationId", LocationId);
            writer.WriteString("poolId", PoolId); writer.WriteNumber("motorPoints", 20);
            writer.WriteStartArray("members");
            foreach (var id in Cna1979GrazianiSourceFacts.SixteenthInfantryBrigadeInitialCandidate.ElementIds.Order(StringComparer.Ordinal))
            {
                var root = id == RootId;
                writer.WriteStartObject(); writer.WriteString("elementId", id);
                if (root) writer.WriteNull("parentId"); else writer.WriteString("parentId", RootId);
                writer.WriteString("component", root ? "3-7-howitzer" : "infantry");
                writer.WriteNumber("toe", root ? 3 : 6); writer.WriteNumber("capabilityPoints", root ? 30 : 10);
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
