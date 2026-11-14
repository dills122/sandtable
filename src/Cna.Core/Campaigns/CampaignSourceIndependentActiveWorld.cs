using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Promotes source-defined independent counters, preserving predecessor World8.</summary>
internal sealed class CampaignSourceIndependentActiveWorld
{
    internal const string PolicyId = "graziani.source-independent-active-world.v1";
    private CampaignSourceIndependentActiveWorld(ContentSourceIndependentPackArtifact content, CampaignWorldSnapshotV8 world,
        IEnumerable<string> added, string predecessorHash)
    {
        Content = content;
        World = world;
        NewlyActivatedIds = Array.AsReadOnly(added.Order(StringComparer.Ordinal).ToArray());
        PredecessorHash = predecessorHash;
    }
    internal ContentSourceIndependentPackArtifact Content { get; }
    internal CampaignWorldSnapshotV8 World { get; }
    internal IReadOnlyList<string> NewlyActivatedIds { get; }
    internal string PredecessorHash { get; }

    internal static CampaignSourceIndependentActiveWorld Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var cairo = state.CairoWorld ?? throw new InvalidOperationException("Cairo source world is missing.");
        var cw = state.CommonwealthSupplement ?? throw new InvalidOperationException("Commonwealth source supplement is missing.");
        var axis = state.AxisSupplement ?? throw new InvalidOperationException("Axis source supplement is missing.");
        var priority = state.AxisPriorityMotorization ?? throw new InvalidOperationException("Axis priority setup is missing.");
        if (cw.Hash() != CampaignSourceCommonwealthSupplementalForces.ContentHash
            || axis.Hash() != CampaignSourceAxisSupplementalForces.ContentHash)
            throw new InvalidOperationException("Independent force source differs from pinned content.");
        var content = ContentSourceIndependentPackArtifact.Create(cairo.Content);
        var previous = state.World;
        var expectedIdentity = state.LocalMapWorld?.Content.Identity ?? state.IndependentWorld?.Content.Identity
            ?? state.MalettiWorld?.Content.Identity ?? cairo.Content.Identity;
        if (previous.ContentIdentity != expectedIdentity)
            throw new InvalidOperationException("Prior active source content identity differs.");
        var existing = previous.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var previouslyPromoted = state.IndependentWorld is null
            ? state.MalettiWorld?.ActivatedIds ?? []
            : ContentSourceIndependentPackArtifact.Units.Where(unit => existing.Contains(unit.ElementId))
                .Select(unit => unit.ElementId).ToArray();
        if (ContentSourceIndependentPackArtifact.Units.Any(unit => existing.Contains(unit.ElementId)
            && !previouslyPromoted.Contains(unit.ElementId, StringComparer.Ordinal)))
            throw new InvalidOperationException("Source-independent candidate overlaps the predecessor world.");
        var candidates = ContentSourceIndependentPackArtifact.Units.Where(unit => unit.SideId == "commonwealth"
            || priority.DetachedTargets.Contains(unit.ElementId, StringComparer.Ordinal)).ToArray();
        var additions = candidates.Where(unit => !existing.Contains(unit.ElementId)).ToArray();
        if (additions.Length == 0) throw new InvalidOperationException("No source-independent units await promotion.");
        foreach (var unit in additions)
        {
            if (previous.PendingArrivals.Any(group => group.ElementIds.Contains(unit.ElementId, StringComparer.Ordinal)))
                throw new InvalidOperationException("Source-independent unit overlaps a deferred arrival.");
            var force = (unit.SideId == "axis" ? axis.Forces : cw.Forces).Single(value => value.ElementId == unit.ElementId);
            if (force.LocationId != unit.LocationId || force.InitialToes is null || force.InitialToes.Count != 1
                || force.InitialToes[0].ComponentId != unit.ElementId + ".infantry" || force.InitialToes[0].Toe != unit.Toe)
                throw new InvalidOperationException("Independent source force differs from pinned catalog.");
        }
        var init = previous.Initialization;
        if (additions.Any(unit => init.CapabilityPointsExpended > new CapabilityPointAmount(unit.CapabilityPoints, 1)))
            throw new InvalidOperationException("Retained initial expenditure exceeds a source unit's allowance.");
        var elements = additions.Select(unit => new CampaignElementStateV5(unit.ElementId, unit.LocationId, init.ReserveStatus,
            new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
            [new CampaignComponentToeState(unit.ElementId + ".infantry", unit.Toe, unit.Origin)])).ToArray();
        var representations = additions.Select(unit => new CampaignMapRepresentationState("independent-setup." + unit.ElementId,
            unit.LocationId, CampaignMapRepresentationBindingKind.IndependentElement, [unit.ElementId]));
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            previous.World.Elements.Concat(elements), previous.World.Representations.Concat(representations), previous.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, previous.ScenarioId, init, world6, previous.PendingArrivals);
        var predecessorHash = state.LocalMapWorld?.Hash() ?? state.IndependentWorld?.Hash() ?? state.MalettiWorld?.Hash() ?? cairo.Hash();
        return new(content, world, additions.Select(unit => unit.ElementId), predecessorHash);
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
