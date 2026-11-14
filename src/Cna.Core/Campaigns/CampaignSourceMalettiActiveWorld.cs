using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Promotes owner-detached Maletti infantry into retained World8.
/// Source location terrain, topology, mobility and full force coverage remain unresolved.</summary>
internal sealed class CampaignSourceMalettiActiveWorld
{
    internal const string PolicyId = "graziani.maletti-source-active-world.v1";
    private CampaignSourceMalettiActiveWorld(ContentSourceMalettiPackArtifact content, CampaignWorldSnapshotV8 world,
        IEnumerable<string> added, string axisPriorityHash)
    {
        Content = content;
        World = world;
        NewlyActivatedIds = Array.AsReadOnly(added.Order(StringComparer.Ordinal).ToArray());
        AxisPriorityHash = axisPriorityHash;
    }

    internal ContentSourceMalettiPackArtifact Content { get; }
    internal CampaignWorldSnapshotV8 World { get; }
    internal IReadOnlyList<string> NewlyActivatedIds { get; }
    internal IReadOnlyList<string> ActivatedIds => Content.Units;
    internal string AxisPriorityHash { get; }

    internal static CampaignSourceMalettiActiveWorld Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var cairo = state.CairoWorld ?? throw new InvalidOperationException("Cairo source world is missing.");
        var priority = state.AxisPriorityMotorization ?? throw new InvalidOperationException("Axis priority setup is missing.");
        var supplement = state.AxisSupplement ?? throw new InvalidOperationException("Axis source supplement is missing.");
        if (supplement.Hash() != CampaignSourceAxisSupplementalForces.ContentHash)
            throw new InvalidOperationException("Axis source supplement differs from pinned source.");
        var prior = state.World;
        var priorMaletti = state.MalettiWorld;
        var previousIds = priorMaletti?.ActivatedIds ?? [];
        if (prior.ContentIdentity != (priorMaletti?.Content.Identity ?? cairo.Content.Identity)
            || previousIds.Any(id => !priority.DetachedTargets.Contains(id, StringComparer.Ordinal)))
            throw new InvalidOperationException("Retained Maletti world disagrees with source setup.");
        var additions = priority.DetachedTargets.Except(previousIds, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (additions.Length == 0) throw new InvalidOperationException("No detached Maletti units await promotion.");
        var activeIds = previousIds.Concat(additions).Order(StringComparer.Ordinal).ToArray();
        var existing = prior.World.Elements.Select(element => element.ElementId).ToHashSet(StringComparer.Ordinal);
        if (additions.Any(id => existing.Contains(id) || prior.PendingArrivals.Any(group => group.ElementIds.Contains(id, StringComparer.Ordinal))))
            throw new InvalidOperationException("Maletti source unit overlaps existing or deferred unit.");
        foreach (var id in additions)
        {
            var force = supplement.Forces.Single(value => value.ElementId == id);
            if (force.LocationId != "c3617" || force.InitialToes is null || force.InitialToes.Count != 1
                || force.InitialToes[0].ComponentId != id + ".infantry" || force.InitialToes[0].Toe != 5)
                throw new InvalidOperationException("Maletti force differs from pinned source.");
        }
        var content = ContentSourceMalettiPackArtifact.Create(cairo.Content, activeIds);
        if (ContentSourceMalettiPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("Maletti source content failed validation.");
        var init = prior.Initialization;
        if (init.CapabilityPointsExpended > new CapabilityPointAmount(10, 1))
            throw new InvalidOperationException("Retained initial expenditure exceeds Maletti allowance.");
        var elements = additions.Select(id => new CampaignElementStateV5(id, "c3617", init.ReserveStatus,
            new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
            [new CampaignComponentToeState(id + ".infantry", 5, ContentSourceMalettiPackArtifact.Origin)])).ToArray();
        var representations = additions.Select(id => new CampaignMapRepresentationState("maletti-setup." + id, "c3617",
            CampaignMapRepresentationBindingKind.IndependentElement, [id])).ToArray();
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Concat(elements), prior.World.Representations.Concat(representations), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(content, world, additions, priority.Hash());
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("axisPriorityHash", AxisPriorityHash);
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
