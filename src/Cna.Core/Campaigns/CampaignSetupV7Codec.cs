using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSetupSnapshotV7
{
    private CampaignSetupSnapshotV7(string setupId, int initialGameTurn,
        InitiativePolicy initialInitiative, CampaignOpeningPreamblePolicy openingPreamble,
        CampaignWeatherPolicy weather, CampaignStageEntryPolicy stageEntry,
        CampaignCombatInitializationPolicy combatInitialization, ContentPackV7Artifact artifact,
        ContentCombatScenario scenario, IEnumerable<RuleReference> sources)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(initialInitiative);
        ArgumentNullException.ThrowIfNull(openingPreamble);
        ArgumentNullException.ThrowIfNull(weather);
        ArgumentNullException.ThrowIfNull(stageEntry);
        ArgumentNullException.ThrowIfNull(combatInitialization);
        SetupId = ContentContractGuards.RequireStableId(setupId, nameof(setupId));
        if (initialGameTurn is < 1 or > 111 || initialInitiative is not PredeterminedInitiative ||
            artifact.Definition.CapabilityProfileId != ContentPackV7Definition.SupportedCapabilityProfileId ||
            artifact.Identity.RulesetId != Cna1979Ruleset.RulesetId ||
            !artifact.Definition.Scenarios.Contains(scenario) || scenario.Start.GameTurn != initialGameTurn ||
            scenario.Start.OperationStage != 1 || scenario.End != scenario.Start ||
            stageEntry.GameTurn != initialGameTurn || stageEntry.OperationStage != 1 ||
            stageEntry.Organization != StageEntryObligationKind.ExplicitNone ||
            stageEntry.NavalConvoyArrival != StageEntryObligationKind.ExplicitNone ||
            stageEntry.FleetAssignment != StageEntryObligationKind.ExplicitNone ||
            stageEntry.FleetRepair != StageEntryObligationKind.ExplicitNone ||
            combatInitialization.GameTurn != initialGameTurn || combatInitialization.OperationStage != 1 ||
            combatInitialization.CapabilityPointsExpended != CapabilityPointAmount.Zero ||
            combatInitialization.CohesionLevel != 0 ||
            combatInitialization.ReserveStatus != CampaignElementReserveStatus.None)
            throw new ArgumentException("Unsupported Combat Setup7 scope or initialization.");
        InitialGameTurn = initialGameTurn;
        InitialInitiative = initialInitiative;
        OpeningPreamble = openingPreamble;
        Weather = weather;
        StageEntry = stageEntry;
        CombatInitialization = combatInitialization;
        Artifact = artifact;
        Scenario = scenario;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
        if (!Sources.SequenceEqual([new RuleReference("sandtable-rules-lab",
                "combat.close-assault-positive.v1:setup")]) ||
            !OpeningPreamble.Sources.SequenceEqual([new RuleReference("sandtable-rules-lab",
                "opening-preamble.no-naval-convoy-obligations.v1")]) ||
            !Weather.Sources.SequenceEqual([new RuleReference("sandtable-rules-lab",
                "weather.no-immediate-effect-subjects.v1")]) ||
            CombatInitialization.Origin.Kind != ContentOriginKind.Synthetic ||
            !CombatInitialization.Origin.References.SequenceEqual([new RuleReference("sandtable-rules-lab",
                "combat.close-assault-positive.v1:initial-ledger")]))
            throw new ArgumentException("Unsupported Combat Setup7 provenance.");
        SetupHash = CampaignSetupV7Codec.CalculateHash(this);
    }

    public string SetupId { get; }
    public string SetupHash { get; }
    public int InitialGameTurn { get; }
    public InitiativePolicy InitialInitiative { get; }
    public CampaignOpeningPreamblePolicy OpeningPreamble { get; }
    public CampaignWeatherPolicy Weather { get; }
    public CampaignStageEntryPolicy StageEntry { get; }
    public CampaignCombatInitializationPolicy CombatInitialization { get; }
    public ContentPackV7Artifact Artifact { get; }
    public ContentCombatScenario Scenario { get; }
    public IReadOnlyList<RuleReference> Sources { get; }

    public static CampaignSetupSnapshotV7 Create(string setupId, int initialGameTurn,
        InitiativePolicy initialInitiative, CampaignOpeningPreamblePolicy openingPreamble,
        CampaignWeatherPolicy weather, CampaignStageEntryPolicy stageEntry,
        CampaignCombatInitializationPolicy combatInitialization, ContentPackV7Artifact artifact,
        ContentCombatScenario scenario, IEnumerable<RuleReference> sources) => new(setupId,
            initialGameTurn, initialInitiative, openingPreamble, weather, stageEntry,
            combatInitialization, artifact, scenario, sources);

    public bool Equals(CampaignSetupSnapshotV7? other) => ReferenceEquals(this, other) ||
        (other is not null && SetupId == other.SetupId && SetupHash == other.SetupHash &&
            InitialGameTurn == other.InitialGameTurn && InitialInitiative == other.InitialInitiative &&
            OpeningPreamble == other.OpeningPreamble && Weather == other.Weather &&
            StageEntry == other.StageEntry && CombatInitialization == other.CombatInitialization &&
            Artifact.Identity == other.Artifact.Identity && Scenario == other.Scenario &&
            Sources.SequenceEqual(other.Sources));

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(SetupHash);
}

internal static class CampaignSetupV7Codec
{
    public static string CalculateHash(CampaignSetupSnapshotV7 setup) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(Serialize(setup, false))).ToLowerInvariant()}";

    public static byte[] Serialize(CampaignSetupSnapshotV7 setup) => Serialize(setup, true);

    private static byte[] Serialize(CampaignSetupSnapshotV7 setup, bool includeHash)
    {
        ArgumentNullException.ThrowIfNull(setup);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 7);
            writer.WriteString("setupId", setup.SetupId);
            if (includeHash) writer.WriteString("setupHash", setup.SetupHash);
            writer.WriteBoolean("isSynthetic", true);
            writer.WriteString("capabilityProfileId", ContentPackV7Definition.SupportedCapabilityProfileId);
            writer.WriteNumber("initialGameTurn", setup.InitialGameTurn);
            writer.WriteStartObject("initialInitiative");
            CampaignSnapshotSerializer.WriteInitiative(writer, setup.InitialInitiative);
            writer.WriteEndObject();
            CampaignSnapshotSerializer.WriteOpeningPreamble(writer, setup.OpeningPreamble);
            CampaignSnapshotSerializer.WriteWeatherPolicy(writer, setup.Weather);
            CampaignStageEntryPolicyCodec.Write(writer, "stageEntry", setup.StageEntry);
            var initialization = setup.CombatInitialization;
            writer.WriteStartObject("combatInitialization");
            writer.WriteNumber("contractVersion", initialization.ContractVersion);
            writer.WriteNumber("gameTurn", initialization.GameTurn);
            writer.WriteNumber("operationStage", initialization.OperationStage);
            writer.WritePropertyName("capabilityPointsExpended");
            CapabilityPointAmountCodec.WriteCanonical(writer, initialization.CapabilityPointsExpended);
            writer.WriteNumber("cohesionLevel", initialization.CohesionLevel);
            writer.WriteString("reserveStatus", CampaignSnapshotSerializer.FormatReserveStatus(
                initialization.ReserveStatus));
            writer.WriteStartObject("origin");
            writer.WriteString("kind", "synthetic");
            writer.WriteStartArray("references");
            foreach (var reference in initialization.Origin.References)
            {
                writer.WriteStartObject();
                writer.WriteString("sourceId", reference.SourceId);
                writer.WriteString("locator", reference.Locator);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteStartObject("content");
            var identity = setup.Artifact.Identity;
            writer.WriteNumber("schemaVersion", identity.SchemaVersion);
            writer.WriteString("formatId", identity.FormatId);
            writer.WriteString("packId", identity.PackId);
            writer.WriteString("rulesetId", identity.RulesetId);
            writer.WriteString("hash", identity.Hash);
            writer.WriteString("scenarioId", setup.Scenario.ScenarioId);
            writer.WriteEndObject();
            CampaignSnapshotSerializer.WriteSources(writer, setup.Sources);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CampaignSetupSnapshotV7 Deserialize(ReadOnlySpan<byte> bytes,
        ContentPackV7Artifact artifact, ContentCombatScenario scenario)
    {
        if (bytes.Length > 65_536) throw new JsonException("Setup7 exceeds byte limit.");
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "schemaVersion", "setupId", "setupHash",
                "isSynthetic", "capabilityProfileId", "initialGameTurn", "initialInitiative",
                "openingPreamble", "weather", "stageEntry", "combatInitialization", "content", "sources");
            if (root.GetProperty("schemaVersion").GetInt32() != 7 ||
                !root.GetProperty("isSynthetic").GetBoolean() ||
                root.GetProperty("capabilityProfileId").GetString() !=
                    ContentPackV7Definition.SupportedCapabilityProfileId)
                throw new JsonException("Unsupported Setup7 identity.");
            var content = root.GetProperty("content");
            CampaignSnapshotSerializer.RequireProperties(content, "schemaVersion", "formatId", "packId",
                "rulesetId", "hash", "scenarioId");
            var identity = artifact.Identity;
            if (content.GetProperty("schemaVersion").GetInt32() != identity.SchemaVersion ||
                content.GetProperty("formatId").GetString() != identity.FormatId ||
                content.GetProperty("packId").GetString() != identity.PackId ||
                content.GetProperty("rulesetId").GetString() != identity.RulesetId ||
                content.GetProperty("hash").GetString() != identity.Hash ||
                content.GetProperty("scenarioId").GetString() != scenario.ScenarioId)
                throw new JsonException("Setup7 content selection differs from trusted Content7.");
            var init = root.GetProperty("combatInitialization");
            CampaignSnapshotSerializer.RequireProperties(init, "contractVersion", "gameTurn",
                "operationStage", "capabilityPointsExpended", "cohesionLevel", "reserveStatus", "origin");
            var cp = init.GetProperty("capabilityPointsExpended");
            CampaignSnapshotSerializer.RequireProperties(cp, "numerator", "denominator");
            var origin = init.GetProperty("origin");
            CampaignSnapshotSerializer.RequireProperties(origin, "kind", "references");
            var policy = new CampaignCombatInitializationPolicy(init.GetProperty("contractVersion").GetInt32(),
                init.GetProperty("gameTurn").GetInt32(), init.GetProperty("operationStage").GetInt32(),
                new CapabilityPointAmount(cp.GetProperty("numerator").GetInt64(),
                    cp.GetProperty("denominator").GetInt32()),
                init.GetProperty("cohesionLevel").GetInt32(),
                init.GetProperty("reserveStatus").GetString() == "none"
                    ? CampaignElementReserveStatus.None
                    : throw new JsonException("Unsupported initial reserve status."),
                new ContentOrigin(origin.GetProperty("kind").GetString() == "synthetic"
                        ? ContentOriginKind.Synthetic
                        : throw new JsonException("Unsupported initial origin kind."),
                    CampaignSnapshotSerializer.ParseSources(origin.GetProperty("references"))));
            var setup = CampaignSetupSnapshotV7.Create(root.GetProperty("setupId").GetString()!,
                root.GetProperty("initialGameTurn").GetInt32(),
                CampaignSnapshotSerializer.ParseInitiative(root.GetProperty("initialInitiative")),
                CampaignSnapshotSerializer.ParseOpeningPreamble(root.GetProperty("openingPreamble")),
                CampaignSnapshotSerializer.ParseWeatherPolicy(root.GetProperty("weather")),
                CampaignStageEntryPolicyCodec.Parse(root.GetProperty("stageEntry")), policy, artifact,
                scenario, CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
            if (root.GetProperty("setupHash").GetString() != setup.SetupHash ||
                !bytes.SequenceEqual(Serialize(setup)))
                throw new JsonException("Setup7 hash or canonical bytes differ.");
            return setup;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or OverflowException or FormatException)
        {
            throw new JsonException("Invalid Setup7.", exception);
        }
    }
}
