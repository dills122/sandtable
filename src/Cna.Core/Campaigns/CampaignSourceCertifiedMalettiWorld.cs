using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Activates the single stack-certified detached source Maletti battalion.</summary>
internal sealed class CampaignSourceCertifiedMalettiWorld
{
    internal const string PolicyId = "graziani.certified-maletti-active-world.v1";
    private CampaignSourceCertifiedMalettiWorld(ContentSourceCertifiedMalettiPackArtifact content,
        CampaignWorldSnapshotV8 world, string elementId, string predecessorHash, string hierarchyHash)
    { Content = content; World = world; ElementId = elementId; PredecessorHash = predecessorHash; HierarchyHash = hierarchyHash; }
    internal ContentSourceCertifiedMalettiPackArtifact Content { get; }
    internal CampaignWorldSnapshotV8 World { get; }
    internal string ElementId { get; }
    internal string PredecessorHash { get; }
    internal string HierarchyHash { get; }

    internal static CampaignSourceCertifiedMalettiWorld Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var hierarchy = state.MalettiHierarchy ?? throw new InvalidOperationException("Source Maletti hierarchy is missing.");
        hierarchy.Validate(state);
        var id = hierarchy.DetachedId ?? throw new InvalidOperationException("No certified Maletti detachment is selected.");
        if (state.CertifiedMalettiWorld is not null) throw new InvalidOperationException("Source Maletti unit is already active.");
        var battlefield = state.Battlefield ?? throw new InvalidOperationException("Source battlefield is missing.");
        var entry = battlefield.Entries.SingleOrDefault(value => value.ElementId == id);
        if (entry is null || entry.SideId != "axis" || entry.LocationId != "c3617" || entry.AuthorityStatus != "source-evidence")
            throw new InvalidOperationException("Certified Maletti source placement is inconsistent.");
        var basis = state.AxisCorpsArtilleryWorld ?? throw new InvalidOperationException("Source corps world is missing.");
        var prior = state.World;
        if (prior.ContentIdentity != (state.CwFreeGunsWorld?.CwFrontlineContent?.Identity ?? state.CwFreeGunsWorld?.MotorMachineGunContent?.Identity ?? state.CwFreeGunsWorld?.GarrisonRemainderContent?.Identity ?? state.CwFreeGunsWorld?.MinorGarrisonContent?.Identity ?? state.CwFreeGunsWorld?.Content.Identity ?? state.CwPriorityWorld?.Content.Identity
                ?? state.MatruhDetachedWorld?.Content.Identity ?? basis.Content.Identity)
            || prior.World.Elements.Any(value => value.ElementId == id)
            || prior.PendingArrivals.Any(group => group.ElementIds.Contains(id, StringComparer.Ordinal)))
            throw new InvalidOperationException("Certified Maletti unit overlaps or changes predecessor identity.");
        var content = ContentSourceCertifiedMalettiPackArtifact.Create(basis.Content);
        if (ContentSourceCertifiedMalettiPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("Certified Maletti content failed validation.");
        var init = prior.Initialization;
        if (init.CapabilityPointsExpended > new CapabilityPointAmount(10, 1))
            throw new InvalidOperationException("Retained expenditure exceeds Maletti allowance.");
        var element = new CampaignElementStateV5(id, "c3617", init.ReserveStatus,
            new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
            [new CampaignComponentToeState(id + ".infantry", 5, ContentSourceCertifiedMalettiPackArtifact.Origin)]);
        var representation = new CampaignMapRepresentationState("certified-maletti-setup." + id, "c3617",
            CampaignMapRepresentationBindingKind.IndependentElement, [id]);
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Append(element), prior.World.Representations.Append(representation), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(content, world, id, state.CwFreeGunsWorld?.Hash() ?? state.CwPriorityWorld?.Hash()
            ?? state.MatruhDetachedWorld?.Hash() ?? basis.Hash(), hierarchy.Hash());
    }
    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", PolicyId); writer.WriteString("predecessorHash", PredecessorHash);
            writer.WriteString("hierarchyHash", HierarchyHash); writer.WriteString("contentHash", Content.Hash());
            writer.WriteBase64String("content", Content.CanonicalBytes); writer.WriteString("elementId", ElementId);
            writer.WriteString("scenarioId", World.ScenarioId); writer.WriteBoolean("worldCoverageComplete", false);
            writer.WriteString("mobilityStatus", "unresolved-no-native-movement");
            CampaignV11CanonicalCodec.WriteWorld(writer, "world", World.World);
            writer.WriteStartArray("pendingArrivals");
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
