using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Activates one owner-detached source Matruh battalion in retained World8.</summary>
internal sealed class CampaignSourceMatruhDetachedWorld
{
    internal const string PolicyId = "graziani.matruh-detached-active-world.v1";
    private CampaignSourceMatruhDetachedWorld(ContentSourceMatruhDetachedPackArtifact content, CampaignWorldSnapshotV8 world,
        string elementId, string predecessorHash, string formationHash)
    { Content = content; World = world; ElementId = elementId; PredecessorHash = predecessorHash; FormationHash = formationHash; }
    internal ContentSourceMatruhDetachedPackArtifact Content { get; }
    internal CampaignWorldSnapshotV8 World { get; }
    internal string ElementId { get; }
    internal string PredecessorHash { get; }
    internal string FormationHash { get; }

    internal static CampaignSourceMatruhDetachedWorld Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var formation = state.MatruhFormation ?? throw new InvalidOperationException("Matruh source formation is missing.");
        formation.Validate(state);
        var id = formation.DetachedId ?? throw new InvalidOperationException("No Matruh detachment selected.");
        if (state.MatruhDetachedWorld is not null) throw new InvalidOperationException("Matruh unit already active.");
        var battlefield = state.Battlefield ?? throw new InvalidOperationException("Source battlefield is missing.");
        var entry = battlefield.Entries.SingleOrDefault(value => value.ElementId == id);
        if (entry is null || entry.SideId != "commonwealth" || entry.LocationId != "d3714"
            || entry.AuthorityStatus != "source-matruh-detached")
            throw new InvalidOperationException("Matruh detached source placement is inconsistent.");
        var corps = state.AxisCorpsArtilleryWorld ?? throw new InvalidOperationException("Source corps world missing.");
        var maletti = state.CertifiedMalettiWorld;
        var prior = state.World;
        if (prior.ContentIdentity != (state.CwFreeGunsWorld?.Content.Identity ?? state.CwPriorityWorld?.Content.Identity
            ?? maletti?.Content.Identity ?? corps.Content.Identity)
            || prior.World.Elements.Any(value => value.ElementId == id)
            || prior.PendingArrivals.Any(group => group.ElementIds.Contains(id, StringComparer.Ordinal)))
            throw new InvalidOperationException("Matruh unit overlaps or changes predecessor identity.");
        var content = ContentSourceMatruhDetachedPackArtifact.Create(corps.Content, maletti?.Content);
        if (ContentSourceMatruhDetachedPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("Matruh source content failed validation.");
        var init = prior.Initialization;
        if (init.CapabilityPointsExpended > new CapabilityPointAmount(10, 1))
            throw new InvalidOperationException("Retained expenditure exceeds Matruh unit allowance.");
        var element = new CampaignElementStateV5(id, "d3714", init.ReserveStatus,
            new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
            [new CampaignComponentToeState(id + ".infantry", 6, ContentSourceMatruhDetachedPackArtifact.Origin)]);
        var representation = new CampaignMapRepresentationState("matruh-detached." + id, "d3714",
            CampaignMapRepresentationBindingKind.IndependentElement, [id]);
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            prior.World.Elements.Append(element), prior.World.Representations.Append(representation), prior.World.BrokenVehicleLots);
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, init, world6, prior.PendingArrivals);
        return new(content, world, id, state.CwFreeGunsWorld?.Hash() ?? state.CwPriorityWorld?.Hash()
            ?? maletti?.Hash() ?? corps.Hash(), formation.Hash());
    }

    internal static CampaignSourceMatruhDetachedWorld Rebind(CampaignSourceMatruhDetachedWorld previous,
        CampaignSourceCertifiedMalettiWorld successor)
    {
        ArgumentNullException.ThrowIfNull(previous); ArgumentNullException.ThrowIfNull(successor);
        if (successor.PredecessorHash != previous.Hash()
            || previous.Content.BasisIdentity != successor.Content.Basis.Identity
            || successor.World.ContentIdentity != successor.Content.Identity
            || !successor.World.World.Elements.Any(value => value.ElementId == previous.ElementId))
            throw new InvalidOperationException("Matruh rebind requires its actual certified Maletti successor.");
        var content = ContentSourceMatruhDetachedPackArtifact.Create(successor.Content.Basis, successor.Content);
        var prior = successor.World;
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, prior.Initialization, prior.World, prior.PendingArrivals);
        return new(content, world, previous.ElementId, successor.Hash(), previous.FormationHash);
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", PolicyId); writer.WriteString("predecessorHash", PredecessorHash);
            writer.WriteString("formationHash", FormationHash); writer.WriteString("contentHash", Content.Hash());
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
