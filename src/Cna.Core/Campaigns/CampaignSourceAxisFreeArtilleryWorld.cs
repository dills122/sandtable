using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Activates placed source 1AR counters in retained World8.</summary>
internal sealed class CampaignSourceAxisFreeArtilleryWorld
{
    internal const string PolicyId = "graziani.axis-free-1ar-active-world.v1";
    private CampaignSourceAxisFreeArtilleryWorld(ContentSourceAxisFreeArtilleryPackArtifact content,
        CampaignWorldSnapshotV8 world, IEnumerable<string> added, string predecessorHash, string setupHash)
    {
        Content = content; World = world; PredecessorHash = predecessorHash; SetupHash = setupHash;
        NewlyActivatedIds = Array.AsReadOnly(added.Order(StringComparer.Ordinal).ToArray());
    }
    internal ContentSourceAxisFreeArtilleryPackArtifact Content { get; }
    internal CampaignWorldSnapshotV8 World { get; }
    internal IReadOnlyList<string> NewlyActivatedIds { get; }
    internal string PredecessorHash { get; }
    internal string SetupHash { get; }

    internal static CampaignSourceAxisFreeArtilleryWorld Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var setup = state.AxisFreeArtillery ?? throw new InvalidOperationException("Source 1AR setup is missing.");
        var battlefield = state.Battlefield ?? throw new InvalidOperationException("Source battlefield is missing.");
        var basis = state.ExpandedIndependentWorld ?? throw new InvalidOperationException("Expanded source world is missing.");
        var previous = state.AxisFreeArtilleryWorld;
        var prior = state.World;
        if (prior.ContentIdentity != (previous?.Content.Identity ?? basis.Content.Identity))
            throw new InvalidOperationException("Source 1AR predecessor identity differs.");
        var content = ContentSourceAxisFreeArtilleryPackArtifact.Create(basis.Content);
        if (ContentSourceAxisFreeArtilleryPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("Source 1AR content failed validation.");
        var existing = prior.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var candidates = CampaignSourceAxisFreeArtillery.Units.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var previouslyActivated = previous is null ? new HashSet<string>(StringComparer.Ordinal)
            : existing.Where(candidates.Contains).ToHashSet(StringComparer.Ordinal);
        if (previous is null && existing.Overlaps(candidates))
            throw new InvalidOperationException("Source 1AR candidate overlaps prior world.");
        var placements = setup.Placements.Where(value => !previouslyActivated.Contains(value.ElementId))
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        if (placements.Length == 0) throw new InvalidOperationException("No placed 1AR unit awaits activation.");
        foreach (var placement in setup.Placements)
        {
            var entry = battlefield.Entries.SingleOrDefault(value => value.ElementId == placement.ElementId);
            if (entry is null || entry.SideId != "axis" || entry.LocationId != placement.LocationId
                || entry.AuthorityStatus is not ("source-1ar-placed" or "source-1ar-active"))
                throw new InvalidOperationException("Source 1AR placement disagrees with retained battlefield.");
        }
        if (placements.Any(value => prior.PendingArrivals.Any(group => group.ElementIds.Contains(value.ElementId, StringComparer.Ordinal))))
            throw new InvalidOperationException("Source 1AR unit overlaps deferred arrival.");
        var init = prior.Initialization;
        if (init.CapabilityPointsExpended > new CapabilityPointAmount(15, 1))
            throw new InvalidOperationException("Retained initial expenditure exceeds source 1AR allowance.");
        var elements = placements.Select(placement =>
        {
            var unit = CampaignSourceAxisFreeArtillery.Units.Single(value => value.ElementId == placement.ElementId);
            return new CampaignElementStateV5(unit.ElementId, placement.LocationId, init.ReserveStatus,
                new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
                [new CampaignComponentToeState(unit.ElementId + "." + unit.ComponentSuffix, unit.Toe,
                    ContentSourceAxisFreeArtilleryPackArtifact.Origin)]);
        });
        var reps = placements.Select(placement => new CampaignMapRepresentationState("1ar-setup." + placement.ElementId,
            placement.LocationId, CampaignMapRepresentationBindingKind.IndependentElement, [placement.ElementId]));
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Concat(elements), prior.World.Representations.Concat(reps), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(content, world, placements.Select(value => value.ElementId), previous?.Hash() ?? basis.Hash(), setup.Hash());
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("predecessorHash", PredecessorHash);
            writer.WriteString("setupHash", SetupHash);
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
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
