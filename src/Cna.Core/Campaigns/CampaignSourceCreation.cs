using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Explicit initial operational policy. Reserve designation is not yet supported.</summary>
public sealed record CampaignSourceInitialization(CapabilityPointAmount CapabilityPointsExpended, int CohesionLevel);

/// <summary>Source-backed initial state only; no gameplay capability is implied.</summary>
public sealed class CampaignSourceSession
{
    internal CampaignSourceSession(CampaignCreationRequest request, ContentSourceScheduledCombatPackArtifact content,
        CampaignWorldSnapshotV8 initialWorld)
    { Request = request; Content = content; InitialWorld = initialWorld; }
    public CampaignCreationRequest Request { get; }
    public ContentSourceScheduledCombatPackArtifact Content { get; }
    internal CampaignWorldSnapshotV8 InitialWorld { get; }
    /// <summary>Trusted host inspection; never expose this list to player audiences.</summary>
    public IReadOnlyList<string> PendingArrivalGroupIds =>
        Array.AsReadOnly(InitialWorld.PendingArrivals.Select(value => value.GroupId).ToArray());
}

public sealed record CampaignSourceCreationResult(CampaignSourceSession Session,
    byte[] CreationEventBytes, byte[] InitialSnapshotBytes);

/// <summary>Versioned creation and replay for source-backed initial state.</summary>
public static class CampaignSourceCreation
{
    public const int RequestVersion = 2;
    public const string SetupId = "cna.1979.source-initialization.v1";
    private const string EventType = "source-campaign-initialized";

    public static string CalculateSetupHash(ContentScheduledPackIdentity identity, string scenarioId,
        CampaignSourceInitialization initialization)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(initialization);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("setupId", SetupId);
            writer.WriteString("formatId", identity.FormatId);
            writer.WriteString("packId", identity.PackId);
            writer.WriteString("rulesetId", identity.RulesetId);
            writer.WriteString("contentHash", identity.Hash);
            writer.WriteString("scenarioId", ContentContractGuards.RequireStableId(scenarioId, nameof(scenarioId)));
            WriteInitialization(writer, initialization);
            writer.WriteEndObject();
        }
        return Hash(stream.ToArray());
    }

    public static CampaignSourceCreationResult Create(CampaignCreationRequest request,
        ContentSourceScheduledCombatPackArtifact content, CampaignSourceInitialization initialization)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(initialization);
        var id = content.Identity;
        var scenario = content.Definition.LegacyDefinition.Scenarios.SingleOrDefault(value => value.ScenarioId == request.ScenarioId)
            ?? throw new ArgumentException("The source scenario is absent from its content.", nameof(request));
        if (request.ContractVersion != RequestVersion || request.SetupId != SetupId
            || !Cna1979Ruleset.IsCanonicalHash(request.RulesetHash)
            || request.ContentPackId != id.PackId || request.ContentHash != id.Hash
            || id.RulesetId != Cna1979Ruleset.RulesetId
            || request.SetupHash != CalculateSetupHash(id, scenario.ScenarioId, initialization)
            || scenario.Origin.Kind != ContentOriginKind.SourceDerived)
            throw new ArgumentException("Source creation identity or scenario is invalid.", nameof(request));
        ContentContractGuards.RequireStableId(request.CampaignId, nameof(request.CampaignId));
        var world = CampaignWorldV8Factory.CreateInitial(content, scenario.ScenarioId,
            new(initialization.CapabilityPointsExpended, initialization.CohesionLevel, CampaignElementReserveStatus.None));
        var session = new CampaignSourceSession(request, content, world);
        return new CampaignSourceCreationResult(session, SerializeEvent(session), SerializeSnapshot(session));
    }

    public static CampaignSourceCreationResult Replay(CampaignCreationRequest request,
        byte[] creationEventBytes, byte[] initialSnapshotBytes)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(creationEventBytes);
        ArgumentNullException.ThrowIfNull(initialSnapshotBytes);
        using var document = JsonDocument.Parse(creationEventBytes);
        var root = document.RootElement;
        CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId",
            "rulesetHash", "seed", "setupId", "setupHash", "formatId", "packId", "rulesetId",
            "contentHash", "scenarioId", "initialization", "contentBase64");
        if (root.GetProperty("contractVersion").GetInt32() != RequestVersion
            || root.GetProperty("eventType").GetString() != EventType
            || root.GetProperty("campaignId").GetString() != request.CampaignId)
            throw new JsonException("Invalid source creation event.");
        var identity = new ContentScheduledPackIdentity(root.GetProperty("formatId").GetString()!,
            root.GetProperty("packId").GetString()!, root.GetProperty("rulesetId").GetString()!,
            root.GetProperty("contentHash").GetString()!);
        var content = ContentSourceScheduledCombatPackArtifact.Read(root.GetProperty("contentBase64").GetBytesFromBase64(), identity);
        var initialization = ParseInitialization(root.GetProperty("initialization"));
        var reconstructed = Create(request, content, initialization);
        if (!creationEventBytes.AsSpan().SequenceEqual(reconstructed.CreationEventBytes)
            || !initialSnapshotBytes.AsSpan().SequenceEqual(reconstructed.InitialSnapshotBytes))
            throw new InvalidOperationException("Retained source creation differs from replay.");
        return reconstructed;
    }

    private static byte[] SerializeEvent(CampaignSourceSession session)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", RequestVersion);
            writer.WriteString("eventType", EventType);
            writer.WriteString("campaignId", session.Request.CampaignId);
            writer.WriteString("rulesetHash", session.Request.RulesetHash);
            writer.WriteNumber("seed", session.Request.Seed);
            writer.WriteString("setupId", session.Request.SetupId);
            writer.WriteString("setupHash", session.Request.SetupHash);
            writer.WriteString("formatId", session.Content.Identity.FormatId);
            writer.WriteString("packId", session.Content.Identity.PackId);
            writer.WriteString("rulesetId", session.Content.Identity.RulesetId);
            writer.WriteString("contentHash", session.Content.Identity.Hash);
            writer.WriteString("scenarioId", session.InitialWorld.ScenarioId);
            WriteInitialization(writer, new(session.InitialWorld.Initialization.CapabilityPointsExpended,
                session.InitialWorld.Initialization.CohesionLevel));
            writer.WriteBase64String("contentBase64", session.Content.CanonicalBytes);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static byte[] SerializeSnapshot(CampaignSourceSession session)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignWorldSnapshotV8.CurrentContractVersion);
            writer.WriteString("campaignId", session.Request.CampaignId);
            writer.WriteString("contentHash", session.Content.Identity.Hash);
            writer.WriteString("scenarioId", session.InitialWorld.ScenarioId);
            CampaignV11CanonicalCodec.WriteWorld(writer, "world", session.InitialWorld.World);
            writer.WriteStartArray("pendingArrivalGroupIds");
            foreach (var arrival in session.InitialWorld.PendingArrivals) writer.WriteStringValue(arrival.GroupId);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static void WriteInitialization(Utf8JsonWriter writer, CampaignSourceInitialization initialization)
    {
        writer.WriteStartObject("initialization");
        writer.WriteNumber("capabilityNumerator", initialization.CapabilityPointsExpended.Numerator);
        writer.WriteNumber("capabilityDenominator", initialization.CapabilityPointsExpended.Denominator);
        writer.WriteNumber("cohesion", initialization.CohesionLevel);
        writer.WriteString("reserve", "none");
        writer.WriteEndObject();
    }

    private static CampaignSourceInitialization ParseInitialization(JsonElement value)
    {
        CampaignSnapshotSerializer.RequireProperties(value, "capabilityNumerator", "capabilityDenominator", "cohesion", "reserve");
        if (value.GetProperty("reserve").GetString() != "none")
            throw new JsonException("Unsupported source initialization reserve status.");
        return new(new CapabilityPointAmount(value.GetProperty("capabilityNumerator").GetInt64(),
            value.GetProperty("capabilityDenominator").GetInt32()), value.GetProperty("cohesion").GetInt32());
    }

    private static string Hash(byte[] bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
}
