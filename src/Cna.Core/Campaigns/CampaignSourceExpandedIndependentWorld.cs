using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Promotes independently deployed source counters into the retained active world.</summary>
internal sealed class CampaignSourceExpandedIndependentWorld
{
    internal const string PolicyId = "graziani.source-independent-expanded-world.v1";
    private CampaignSourceExpandedIndependentWorld(ContentSourceExpandedIndependentPackArtifact content,
        CampaignWorldSnapshotV8 world, IEnumerable<string> added, string predecessorHash)
    {
        Content = content;
        World = world;
        NewlyActivatedIds = Array.AsReadOnly(added.Order(StringComparer.Ordinal).ToArray());
        PredecessorHash = predecessorHash;
    }
    internal ContentSourceExpandedIndependentPackArtifact Content { get; }
    internal CampaignWorldSnapshotV8 World { get; }
    internal IReadOnlyList<string> NewlyActivatedIds { get; }
    internal string PredecessorHash { get; }

    internal static CampaignSourceExpandedIndependentWorld Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var map = state.LocalMapWorld ?? throw new InvalidOperationException("Source local map world is missing.");
        var cw = state.CommonwealthSupplement ?? throw new InvalidOperationException("Commonwealth source supplement is missing.");
        var axis = state.AxisSupplement ?? throw new InvalidOperationException("Axis source supplement is missing.");
        var priority = state.AxisPriorityMotorization ?? throw new InvalidOperationException("Axis priority setup is missing.");
        if (cw.Hash() != CampaignSourceCommonwealthSupplementalForces.ContentHash
            || axis.Hash() != CampaignSourceAxisSupplementalForces.ContentHash)
            throw new InvalidOperationException("Expanded independent forces differ from pinned source.");
        var content = ContentSourceExpandedIndependentPackArtifact.Create(map.Content);
        var prior = state.World;
        var expected = state.ExpandedIndependentWorld?.Content.Identity ?? map.Content.Identity;
        if (prior.ContentIdentity != expected)
            throw new InvalidOperationException("Expanded source predecessor identity differs.");
        var existing = prior.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var units = ContentSourceIndependentPackArtifact.Units.Concat(ContentSourceExpandedIndependentPackArtifact.AddedUnits).ToArray();
        var previousAdded = state.ExpandedIndependentWorld is null ? Array.Empty<string>()
            : ContentSourceExpandedIndependentPackArtifact.AddedUnits.Where(unit => existing.Contains(unit.ElementId))
                .Select(unit => unit.ElementId).ToArray();
        if (ContentSourceExpandedIndependentPackArtifact.AddedUnits.Any(unit => existing.Contains(unit.ElementId)
            && !previousAdded.Contains(unit.ElementId, StringComparer.Ordinal)))
            throw new InvalidOperationException("Expanded source unit overlaps prior world.");
        var additions = units.Where(unit => !existing.Contains(unit.ElementId)
            && (unit.SideId == "commonwealth" || priority.DetachedTargets.Contains(unit.ElementId, StringComparer.Ordinal))).ToArray();
        if (additions.Length == 0) throw new InvalidOperationException("No independent source units await promotion.");
        foreach (var unit in additions)
        {
            if (prior.PendingArrivals.Any(group => group.ElementIds.Contains(unit.ElementId, StringComparer.Ordinal)))
                throw new InvalidOperationException("Independent source unit overlaps deferred arrival.");
            var force = (unit.SideId == "axis" ? axis.Forces : cw.Forces).Single(value => value.ElementId == unit.ElementId);
            if (force.LocationId != unit.LocationId || force.InitialToes is null || force.InitialToes.Count != 1
                || force.InitialToes[0].ComponentId != unit.ElementId + ".infantry" || force.InitialToes[0].Toe != unit.Toe)
                throw new InvalidOperationException("Independent source force differs from pinned catalog.");
        }
        var init = prior.Initialization;
        if (additions.Any(unit => init.CapabilityPointsExpended > new CapabilityPointAmount(unit.CapabilityPoints, 1)))
            throw new InvalidOperationException("Retained initial expenditure exceeds a source unit's allowance.");
        var elements = additions.Select(unit => new CampaignElementStateV5(unit.ElementId, unit.LocationId, init.ReserveStatus,
            new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
            [new CampaignComponentToeState(unit.ElementId + ".infantry", unit.Toe, unit.Origin)]));
        var representations = additions.Select(unit => new CampaignMapRepresentationState("independent-setup." + unit.ElementId,
            unit.LocationId, CampaignMapRepresentationBindingKind.IndependentElement, [unit.ElementId]));
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Concat(elements), prior.World.Representations.Concat(representations), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(content, world, additions.Select(unit => unit.ElementId), state.ExpandedIndependentWorld?.Hash() ?? map.Hash());
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("predecessorHash", PredecessorHash);
            writer.WriteString("contentHash", Content.Hash());
            writer.WriteBase64String("content", Content.CanonicalBytes);
            writer.WriteString("scenarioId", World.ScenarioId);
            writer.WriteBoolean("worldCoverageComplete", false);
            writer.WriteString("mobilityStatus", "unresolved-no-native-movement");
            writer.WriteStartArray("newlyActivatedIds");
            foreach (var id in NewlyActivatedIds) writer.WriteStringValue(id);
            writer.WriteEndArray();
            CampaignV11CanonicalCodec.WriteWorld(writer, "world", World.World);
            writer.WriteStartArray("pendingArrivals");
            foreach (var arrival in World.PendingArrivals)
            {
                writer.WriteStartObject();
                writer.WriteString("groupId", arrival.GroupId);
                writer.WriteString("sideId", arrival.SideId);
                writer.WriteNumber("gameTurn", arrival.Arrival.GameTurn);
                writer.WriteNumber("operationStage", arrival.Arrival.OperationStage);
                writer.WriteString("entryAreaId", arrival.EntryAreaId);
                writer.WriteStartArray("elementIds");
                foreach (var id in arrival.ElementIds) writer.WriteStringValue(id);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
