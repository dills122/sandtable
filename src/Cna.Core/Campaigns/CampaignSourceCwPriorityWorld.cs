using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Normalized source world with real French motor company; optional source units remain independent choices.</summary>
internal sealed class CampaignSourceCwPriorityWorld
{
    internal const string PolicyId = "graziani.cw-priority-active-world.v1";
    internal const string FrenchId = "1-french-motor-marines";
    private CampaignSourceCwPriorityWorld(ContentSourceCwPriorityPackArtifact content, CampaignWorldSnapshotV8 world, string predecessorHash)
    { Content = content; World = world; PredecessorHash = predecessorHash; }
    internal ContentSourceCwPriorityPackArtifact Content { get; }
    internal CampaignWorldSnapshotV8 World { get; }
    internal string PredecessorHash { get; }

    internal static CampaignSourceCwPriorityWorld Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var catalog = state.CwPriorityCatalog ?? throw new InvalidOperationException("CW priority catalog missing.");
        catalog.Validate(state);
        if (state.CwPriorityWorld is not null) throw new InvalidOperationException("CW priority world already active.");
        var corps = state.AxisCorpsArtilleryWorld ?? throw new InvalidOperationException("Source corps world missing.");
        var prior = state.World;
        var expectedIdentity = state.MatruhDetachedWorld?.Content.Identity ?? state.CertifiedMalettiWorld?.Content.Identity ?? corps.Content.Identity;
        var predecessorHash = state.MatruhDetachedWorld?.Hash() ?? state.CertifiedMalettiWorld?.Hash() ?? corps.Hash();
        if (prior.ContentIdentity != expectedIdentity || prior.World.Elements.Any(value => value.ElementId == FrenchId)
            || prior.PendingArrivals.Any(group => group.ElementIds.Contains(FrenchId, StringComparer.Ordinal)))
            throw new InvalidOperationException("CW priority source predecessor is inconsistent.");
        var battlefield = state.Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        var french = battlefield.Entries.SingleOrDefault(value => value.ElementId == FrenchId);
        if (french is null || french.SideId != "commonwealth" || french.LocationId != "c3926"
            || french.AuthorityStatus != "source-evidence"
            || battlefield.Entries.Any(value => value.LocationId == "c3926" && value.ElementId != FrenchId))
            throw new InvalidOperationException("French source unit independence is not established.");
        var content = ContentSourceCwPriorityPackArtifact.Create(corps.Content);
        if (ContentSourceCwPriorityPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("CW priority source content failed validation.");
        var init = prior.Initialization;
        if (init.CapabilityPointsExpended > new CapabilityPointAmount(10, 1))
            throw new InvalidOperationException("Retained expenditure exceeds French company allowance.");
        var element = new CampaignElementStateV5(FrenchId, "c3926", init.ReserveStatus,
            new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
            [new CampaignComponentToeState(FrenchId + ".infantry", 1, ContentSourceCwPriorityPackArtifact.FrenchOrigin)]);
        var representation = new CampaignMapRepresentationState("cw-priority." + FrenchId, "c3926",
            CampaignMapRepresentationBindingKind.IndependentElement, [FrenchId]);
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Append(element), prior.World.Representations.Append(representation), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(content, world, predecessorHash);
    }

    internal static CampaignSourceCwPriorityWorld Rebind(CampaignSourceCwPriorityWorld previous,
        CampaignSourceCertifiedMalettiWorld successor) => Rebind(previous, successor.World, successor.PredecessorHash, successor.Hash());
    internal static CampaignSourceCwPriorityWorld Rebind(CampaignSourceCwPriorityWorld previous,
        CampaignSourceMatruhDetachedWorld successor) => Rebind(previous, successor.World, successor.PredecessorHash, successor.Hash());
    private static CampaignSourceCwPriorityWorld Rebind(CampaignSourceCwPriorityWorld previous,
        CampaignWorldSnapshotV8 successor, string predecessorHash, string successorHash)
    {
        ArgumentNullException.ThrowIfNull(previous); ArgumentNullException.ThrowIfNull(successor);
        if (predecessorHash != previous.Hash()
            || !previous.World.World.Elements.All(value => successor.World.Elements.Contains(value))
            || !previous.World.World.Representations.All(value => successor.World.Representations.Contains(value))
            || !previous.World.World.BrokenVehicleLots.SequenceEqual(successor.World.BrokenVehicleLots)
            || !previous.World.PendingArrivals.SequenceEqual(successor.PendingArrivals))
            throw new InvalidOperationException("Normalized source rebind must preserve its actual predecessor.");
        var world = new CampaignWorldSnapshotV8(previous.Content.Identity, successor.ScenarioId, successor.Initialization,
            successor.World, successor.PendingArrivals);
        return new(previous.Content, world, successorHash);
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", PolicyId); writer.WriteString("predecessorHash", PredecessorHash);
            writer.WriteString("contentHash", Content.Hash()); writer.WriteBase64String("content", Content.CanonicalBytes);
            writer.WriteString("scenarioId", World.ScenarioId); writer.WriteBoolean("worldCoverageComplete", false);
            writer.WriteString("mobilityStatus", "unresolved-no-native-movement");
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
