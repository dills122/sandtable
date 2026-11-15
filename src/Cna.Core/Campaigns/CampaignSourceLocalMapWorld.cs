using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

/// <summary>Preserves active World8 while binding the limited source map successor.</summary>
internal sealed class CampaignSourceLocalMapWorld
{
    internal const string PolicyId = "graziani.source-local-map-world.v1";
    private CampaignSourceLocalMapWorld(ContentSourceLocalMapPackArtifact content, CampaignWorldSnapshotV8 world, string predecessorHash)
    { Content = content; World = world; PredecessorHash = predecessorHash; }
    internal ContentSourceLocalMapPackArtifact Content { get; }
    internal CampaignWorldSnapshotV8 World { get; }
    internal string PredecessorHash { get; }

    internal static CampaignSourceLocalMapWorld Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var independent = state.IndependentWorld ?? throw new InvalidOperationException("Independent source world is missing.");
        if (state.World.ContentIdentity != independent.Content.Identity)
            throw new InvalidOperationException("Local map predecessor identity differs from active world.");
        var content = ContentSourceLocalMapPackArtifact.Create(independent.Content);
        if (ContentSourceLocalMapPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity)
            throw new InvalidOperationException("Local source map content failed validation.");
        var prior = state.World;
        var world = new CampaignWorldSnapshotV8(content.Identity, prior.ScenarioId, prior.Initialization,
            prior.World, prior.PendingArrivals);
        return new(content, world, independent.Hash());
    }

    internal static CampaignSourceLocalMapWorld Rebind(CampaignSourceLocalMapWorld previous,
        CampaignSourceIndependentActiveWorld successor)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(successor);
        if (successor.PredecessorHash != previous.Hash()
            || successor.Content.Identity != previous.Content.Basis.Identity
            || successor.World.ContentIdentity != successor.Content.Identity)
            throw new InvalidOperationException("Local map rebind requires its actual independent-world successor.");
        var prior = successor.World;
        var world = new CampaignWorldSnapshotV8(previous.Content.Identity, prior.ScenarioId, prior.Initialization,
            prior.World, prior.PendingArrivals);
        return new(previous.Content, world, successor.Hash());
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
