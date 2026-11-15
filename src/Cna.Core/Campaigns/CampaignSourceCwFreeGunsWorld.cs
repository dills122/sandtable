using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Activates owner-placed CW source gun counters in the retained World8.</summary>
internal sealed class CampaignSourceCwFreeGunsWorld
{
    internal const string PolicyId = "graziani.cw-free-guns-active-world.v1";
    private CampaignSourceCwFreeGunsWorld(ContentSourceCwFreeGunsPackArtifact content, CampaignWorldSnapshotV8 world,
        IEnumerable<string> added, string predecessorHash, string setupHash)
    {
        Content = content; World = world; PredecessorHash = predecessorHash; SetupHash = setupHash;
        NewlyActivatedIds = Array.AsReadOnly(added.Order(StringComparer.Ordinal).ToArray());
    }
    internal ContentSourceCwFreeGunsPackArtifact Content { get; }
    internal CampaignWorldSnapshotV8 World { get; }
    internal IReadOnlyList<string> NewlyActivatedIds { get; }
    internal string PredecessorHash { get; }
    internal string SetupHash { get; }

    internal static CampaignSourceCwFreeGunsWorld Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var setup = state.CwFreeGuns ?? throw new InvalidOperationException("CW free-gun setup missing.");
        var battlefield = state.Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        var basis = state.CwPriorityWorld ?? throw new InvalidOperationException("Normalized priority source world missing.");
        var previous = state.CwFreeGunsWorld;
        var prior = state.World;
        if (prior.ContentIdentity != (previous?.Content.Identity ?? basis.Content.Identity))
            throw new InvalidOperationException("CW free-gun predecessor identity differs.");
        var content = ContentSourceCwFreeGunsPackArtifact.Create(basis.Content);
        if (ContentSourceCwFreeGunsPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("CW free-gun source content failed validation.");
        var existing = prior.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var candidateIds = CampaignSourceCwFreeGuns.Units.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        var previouslyActive = previous is null ? new HashSet<string>(StringComparer.Ordinal)
            : existing.Where(candidateIds.Contains).ToHashSet(StringComparer.Ordinal);
        if (previous is null && existing.Overlaps(candidateIds))
            throw new InvalidOperationException("CW free-gun source overlaps prior world.");
        var placements = setup.Placements.Where(value => !previouslyActive.Contains(value.ElementId))
            .OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray();
        if (placements.Length == 0) throw new InvalidOperationException("No placed CW gun awaits activation.");
        foreach (var placement in setup.Placements)
        {
            var entry = battlefield.Entries.SingleOrDefault(value => value.ElementId == placement.ElementId);
            if (entry is null || entry.SideId != "commonwealth" || entry.LocationId != placement.LocationId
                || entry.AuthorityStatus is not ("source-cw-free-gun-placed" or "source-cw-free-gun-active"))
                throw new InvalidOperationException("CW free-gun placement disagrees with retained battlefield.");
        }
        if (placements.Any(value => prior.PendingArrivals.Any(group => group.ElementIds.Contains(value.ElementId, StringComparer.Ordinal))))
            throw new InvalidOperationException("CW free gun overlaps deferred arrival.");
        var init = prior.Initialization;
        var elements = placements.Select(placement =>
        {
            var unit = CampaignSourceCwFreeGuns.Units.Single(value => value.ElementId == placement.ElementId);
            if (init.CapabilityPointsExpended > new CapabilityPointAmount(unit.CapabilityPoints, 1))
                throw new InvalidOperationException("Retained expenditure exceeds source gun allowance.");
            return new CampaignElementStateV5(unit.ElementId, placement.LocationId, init.ReserveStatus,
                new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
                [new CampaignComponentToeState(unit.ElementId + "." + unit.ComponentSuffix, unit.Toe,
                    ContentSourceCwFreeGunsPackArtifact.Origin(unit.ElementId))]);
        }).ToArray();
        var reps = placements.Select(placement => new CampaignMapRepresentationState("cw-free-gun." + placement.ElementId,
            placement.LocationId, CampaignMapRepresentationBindingKind.IndependentElement, [placement.ElementId]));
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Concat(elements), prior.World.Representations.Concat(reps), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(content, world, placements.Select(value => value.ElementId), previous?.Hash() ?? basis.Hash(), setup.Hash());
    }

    internal static CampaignSourceCwFreeGunsWorld Rebind(CampaignSourceCwFreeGunsWorld previous,
        CampaignSourceCertifiedMalettiWorld successor) => Rebind(previous, successor.World, successor.PredecessorHash, successor.Hash());
    internal static CampaignSourceCwFreeGunsWorld Rebind(CampaignSourceCwFreeGunsWorld previous,
        CampaignSourceMatruhDetachedWorld successor) => Rebind(previous, successor.World, successor.PredecessorHash, successor.Hash());
    private static CampaignSourceCwFreeGunsWorld Rebind(CampaignSourceCwFreeGunsWorld previous,
        CampaignWorldSnapshotV8 successor, string predecessorHash, string successorHash)
    {
        ArgumentNullException.ThrowIfNull(previous); ArgumentNullException.ThrowIfNull(successor);
        if (predecessorHash != previous.Hash()
            || !previous.World.World.Elements.All(value => successor.World.Elements.Contains(value))
            || !previous.World.World.Representations.All(value => successor.World.Representations.Contains(value))
            || !previous.World.World.BrokenVehicleLots.SequenceEqual(successor.World.BrokenVehicleLots)
            || !previous.World.PendingArrivals.SequenceEqual(successor.PendingArrivals))
            throw new InvalidOperationException("CW free-gun rebind must preserve its actual predecessor.");
        var world = new CampaignWorldSnapshotV8(previous.Content.Identity, successor.ScenarioId, successor.Initialization,
            successor.World, successor.PendingArrivals);
        return new(previous.Content, world, previous.NewlyActivatedIds, successorHash, previous.SetupHash);
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", PolicyId); writer.WriteString("predecessorHash", PredecessorHash);
            writer.WriteString("setupHash", SetupHash); writer.WriteString("contentHash", Content.Hash());
            writer.WriteBase64String("content", Content.CanonicalBytes); writer.WriteString("scenarioId", World.ScenarioId);
            writer.WriteBoolean("worldCoverageComplete", false); writer.WriteString("mobilityStatus", "unresolved-no-native-movement");
            writer.WriteStartArray("newlyActivatedIds"); foreach (var id in NewlyActivatedIds) writer.WriteStringValue(id); writer.WriteEndArray();
            CampaignV11CanonicalCodec.WriteWorld(writer, "world", World.World); writer.WriteStartArray("pendingArrivals");
            foreach (var arrival in World.PendingArrivals)
            {
                writer.WriteStartObject(); writer.WriteString("groupId", arrival.GroupId); writer.WriteString("sideId", arrival.SideId);
                writer.WriteNumber("gameTurn", arrival.Arrival.GameTurn); writer.WriteNumber("operationStage", arrival.Arrival.OperationStage);
                writer.WriteString("entryAreaId", arrival.EntryAreaId); writer.WriteStartArray("elementIds");
                foreach (var id in arrival.ElementIds) writer.WriteStringValue(id);
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
