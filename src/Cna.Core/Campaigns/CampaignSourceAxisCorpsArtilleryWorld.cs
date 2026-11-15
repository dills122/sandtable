using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Activates placed source corps artillery counters in retained World8.</summary>
internal sealed class CampaignSourceAxisCorpsArtilleryWorld
{
    internal const string PolicyId = "graziani.axis-corps-artillery-active-world.v1";
    private CampaignSourceAxisCorpsArtilleryWorld(ContentSourceAxisCorpsArtilleryPackArtifact content,
        CampaignWorldSnapshotV8 world, IEnumerable<string> added, string predecessorHash, string setupHash)
    {
        Content = content; World = world; PredecessorHash = predecessorHash; SetupHash = setupHash;
        NewlyActivatedIds = Array.AsReadOnly(added.Order(StringComparer.Ordinal).ToArray());
    }
    internal ContentSourceAxisCorpsArtilleryPackArtifact Content { get; }
    internal CampaignWorldSnapshotV8 World { get; }
    internal IReadOnlyList<string> NewlyActivatedIds { get; }
    internal string PredecessorHash { get; }
    internal string SetupHash { get; }

    internal static CampaignSourceAxisCorpsArtilleryWorld Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var setup = state.AxisCorpsArtillery ?? throw new InvalidOperationException("Source corps artillery setup is missing.");
        var battlefield = state.Battlefield ?? throw new InvalidOperationException("Source battlefield is missing.");
        var basis = state.AxisFreeArtilleryWorld ?? throw new InvalidOperationException("Source free artillery world is missing.");
        var previous = state.AxisCorpsArtilleryWorld;
        var prior = state.World;
        if (prior.ContentIdentity != (previous?.Content.Identity ?? basis.Content.Identity))
            throw new InvalidOperationException("Source corps artillery predecessor identity differs.");
        var content = ContentSourceAxisCorpsArtilleryPackArtifact.Create(basis.Content);
        if (ContentSourceAxisCorpsArtilleryPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("Source corps artillery content failed validation.");
        var existing = prior.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var candidates = CampaignSourceAxisCorpsArtillery.Units.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var previouslyActivated = previous is null ? new HashSet<string>(StringComparer.Ordinal)
            : existing.Where(candidates.Contains).ToHashSet(StringComparer.Ordinal);
        if (previous is null && existing.Overlaps(candidates))
            throw new InvalidOperationException("Source corps artillery candidate overlaps prior world.");
        var placements = setup.Placements.Where(value => !previouslyActivated.Contains(value.ElementId))
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        if (placements.Length == 0) throw new InvalidOperationException("No placed corps artillery unit awaits activation.");
        foreach (var placement in setup.Placements)
        {
            var entry = battlefield.Entries.SingleOrDefault(value => value.ElementId == placement.ElementId);
            if (entry is null || entry.SideId != "axis" || entry.LocationId != placement.LocationId
                || entry.AuthorityStatus is not ("source-corps-artillery-placed" or "source-corps-artillery-active"))
                throw new InvalidOperationException("Source corps artillery placement disagrees with retained battlefield.");
        }
        if (placements.Any(value => prior.PendingArrivals.Any(group => group.ElementIds.Contains(value.ElementId, StringComparer.Ordinal))))
            throw new InvalidOperationException("Source corps artillery unit overlaps deferred arrival.");
        var init = prior.Initialization;
        if (init.CapabilityPointsExpended > new CapabilityPointAmount(15, 1))
            throw new InvalidOperationException("Retained initial expenditure exceeds source corps artillery allowance.");
        var elements = placements.Select(placement =>
        {
            var unit = CampaignSourceAxisCorpsArtillery.Units.Single(value => value.ElementId == placement.ElementId);
            return new CampaignElementStateV5(unit.ElementId, placement.LocationId, init.ReserveStatus,
                new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
                unit.Components.Select(component => new CampaignComponentToeState(unit.ElementId + "." + component.Suffix,
                    component.Toe, ContentSourceAxisCorpsArtilleryPackArtifact.Origin)));
        });
        var reps = placements.Select(placement => new CampaignMapRepresentationState("corps-artillery-setup." + placement.ElementId,
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
