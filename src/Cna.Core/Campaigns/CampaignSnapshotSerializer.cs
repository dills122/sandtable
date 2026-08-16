using System.Text.Json;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

public static class CampaignSnapshotSerializer
{
    public static byte[] Serialize(CampaignSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Validate(snapshot);
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", snapshot.ContractVersion);
            writer.WriteString("campaignId", snapshot.CampaignId);
            writer.WriteNumber("stateVersion", snapshot.StateVersion);
            writer.WriteString("rulesetHash", snapshot.RulesetHash);
            WriteSetup(writer, snapshot.Setup);

            if (snapshot.InitiativeHolder is null)
            {
                writer.WriteNull("initiativeHolder");
            }
            else
            {
                writer.WriteString(
                    "initiativeHolder",
                    FormatSide(snapshot.InitiativeHolder.Value));
            }

            WriteRandomState(writer, snapshot.RandomState);
            WritePosition(writer, snapshot.SequencePosition);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignSnapshot Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            RequireProperties(
                root,
                "contractVersion",
                "campaignId",
                "stateVersion",
                "rulesetHash",
                "setup",
                "initiativeHolder",
                "randomState",
                "sequencePosition");

            var holderElement = root.GetProperty("initiativeHolder");
            var snapshot = new CampaignSnapshot(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("stateVersion").GetInt64(),
                root.GetProperty("rulesetHash").GetString()!,
                ParseSetup(root.GetProperty("setup")),
                holderElement.ValueKind == JsonValueKind.Null
                    ? null
                    : ParseSide(holderElement.GetString()),
                ParseRandomState(root.GetProperty("randomState")),
                ParsePosition(root.GetProperty("sequencePosition")));

            Validate(snapshot);
            return snapshot;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or KeyNotFoundException
            or OverflowException)
        {
            throw new JsonException("The campaign snapshot JSON is invalid.", exception);
        }
    }

    internal static void WriteSetup(Utf8JsonWriter writer, CampaignSetupSnapshot setup)
    {
        writer.WriteStartObject("setup");
        writer.WriteNumber("schemaVersion", setup.SchemaVersion);
        writer.WriteString("setupId", setup.SetupId);
        writer.WriteString("setupHash", setup.SetupHash);
        writer.WriteBoolean("isSynthetic", setup.IsSynthetic);
        writer.WriteNumber("initialGameTurn", setup.InitialGameTurn);
        writer.WriteStartObject("initialInitiative");
        WriteInitiative(writer, setup.InitialInitiative);
        writer.WriteEndObject();
        WriteSources(writer, setup.Sources);
        writer.WriteEndObject();
    }

    internal static CampaignSetupSnapshot ParseSetup(JsonElement setup)
    {
        RequireProperties(
            setup,
            "schemaVersion",
            "setupId",
            "setupHash",
            "isSynthetic",
            "initialGameTurn",
            "initialInitiative",
            "sources");

        return new CampaignSetupSnapshot(
            setup.GetProperty("schemaVersion").GetInt32(),
            setup.GetProperty("setupId").GetString()!,
            setup.GetProperty("setupHash").GetString()!,
            setup.GetProperty("isSynthetic").GetBoolean(),
            setup.GetProperty("initialGameTurn").GetInt32(),
            ParseInitiative(setup.GetProperty("initialInitiative")),
            ParseSources(setup.GetProperty("sources")));
    }

    private static void WriteInitiative(Utf8JsonWriter writer, InitiativePolicy policy)
    {
        switch (policy)
        {
            case PredeterminedInitiative predetermined:
                writer.WriteString("kind", "predetermined");
                writer.WriteString("holder", FormatSide(predetermined.Holder));
                break;
            case ContestedInitiative contested:
                writer.WriteString("kind", "contested");
                writer.WriteStartObject("axisFacts");
                writer.WriteString(
                    "rommelLocation",
                    FormatLocation(contested.AxisFacts.RommelLocation));
                writer.WriteStartArray("germanLandCombatUnitLocations");

                foreach (var location in contested.AxisFacts.GermanLandCombatUnitLocations)
                {
                    writer.WriteStringValue(FormatLocation(location));
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
                break;
            default:
                throw new JsonException("Unknown initiative policy.");
        }
    }

    private static InitiativePolicy ParseInitiative(JsonElement initiative)
    {
        var kind = initiative.GetProperty("kind").GetString();

        return kind switch
        {
            "predetermined" => ParsePredetermined(initiative),
            "contested" => ParseContested(initiative),
            _ => throw new JsonException($"Unknown initiative policy '{kind}'."),
        };
    }

    private static PredeterminedInitiative ParsePredetermined(JsonElement initiative)
    {
        RequireProperties(initiative, "kind", "holder");
        return new PredeterminedInitiative(ParseSide(
            initiative.GetProperty("holder").GetString()));
    }

    private static ContestedInitiative ParseContested(JsonElement initiative)
    {
        RequireProperties(initiative, "kind", "axisFacts");
        var facts = initiative.GetProperty("axisFacts");
        RequireProperties(
            facts,
            "rommelLocation",
            "germanLandCombatUnitLocations");
        var locations = facts
            .GetProperty("germanLandCombatUnitLocations")
            .EnumerateArray()
            .Select(location => ParseLocation(location.GetString()))
            .ToArray();

        return new ContestedInitiative(new AxisInitiativeSourceFacts(
            ParseLocation(facts.GetProperty("rommelLocation").GetString()),
            locations));
    }

    internal static void WriteRandomState(Utf8JsonWriter writer, RandomStreamState state)
    {
        writer.WriteStartObject("randomState");
        writer.WriteNumber("contractVersion", state.ContractVersion);
        writer.WriteString("algorithmId", state.AlgorithmId);
        writer.WriteNumber("seed", state.Seed);
        writer.WriteNumber("nextByteCursor", state.NextByteCursor);
        writer.WriteEndObject();
    }

    internal static RandomStreamState ParseRandomState(JsonElement randomState)
    {
        RequireProperties(
            randomState,
            "contractVersion",
            "algorithmId",
            "seed",
            "nextByteCursor");
        return new RandomStreamState(
            randomState.GetProperty("contractVersion").GetInt32(),
            randomState.GetProperty("algorithmId").GetString()!,
            randomState.GetProperty("seed").GetUInt64(),
            randomState.GetProperty("nextByteCursor").GetUInt64());
    }

    internal static void WritePosition(Utf8JsonWriter writer, LandSequencePosition position)
    {
        writer.WriteStartObject("sequencePosition");
        writer.WriteNumber("contractVersion", position.ContractVersion);
        writer.WriteString("positionId", position.PositionId);
        writer.WriteNumber("gameTurn", position.GameTurn);
        writer.WriteNumber("operationStage", position.OperationStage);
        writer.WriteString("stageId", position.StageId);
        writer.WriteString("phaseId", position.PhaseId);
        WriteNullableString(writer, "segmentId", position.SegmentId);
        WriteNullableString(writer, "stepId", position.StepId);
        writer.WriteString("actorRole", FormatActorRole(position.ActorRole));

        if (position.ActiveSide is null)
        {
            writer.WriteNull("activeSide");
        }
        else
        {
            writer.WriteString("activeSide", FormatSide(position.ActiveSide.Value));
        }

        WriteSources(writer, position.Sources);
        writer.WriteEndObject();
    }

    internal static LandSequencePosition ParsePosition(JsonElement position)
    {
        RequireProperties(
            position,
            "contractVersion",
            "positionId",
            "gameTurn",
            "operationStage",
            "stageId",
            "phaseId",
            "segmentId",
            "stepId",
            "actorRole",
            "activeSide",
            "sources");
        var activeSide = position.GetProperty("activeSide");

        return new LandSequencePosition(
            position.GetProperty("contractVersion").GetInt32(),
            position.GetProperty("positionId").GetString()!,
            position.GetProperty("gameTurn").GetInt32(),
            position.GetProperty("operationStage").GetInt32(),
            position.GetProperty("stageId").GetString()!,
            position.GetProperty("phaseId").GetString()!,
            ParseNullableString(position.GetProperty("segmentId")),
            ParseNullableString(position.GetProperty("stepId")),
            ParseActorRole(position.GetProperty("actorRole").GetString()),
            activeSide.ValueKind == JsonValueKind.Null
                ? null
                : ParseSide(activeSide.GetString()),
            ParseSources(position.GetProperty("sources")));
    }

    internal static void WriteSources(
        Utf8JsonWriter writer,
        IEnumerable<RuleReference> sources)
    {
        writer.WriteStartArray("sources");

        foreach (var source in sources)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", source.SourceId);
            writer.WriteString("locator", source.Locator);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    internal static RuleReference[] ParseSources(JsonElement sources) => sources
        .EnumerateArray()
        .Select(source =>
        {
            RequireProperties(source, "sourceId", "locator");
            return new RuleReference(
                source.GetProperty("sourceId").GetString()!,
                source.GetProperty("locator").GetString()!);
        })
        .ToArray();

    internal static void RequireProperties(JsonElement element, params string[] expected)
    {
        var actual = element
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new JsonException("The campaign snapshot property contract is invalid.");
        }
    }

    private static string? ParseNullableString(JsonElement element) =>
        element.ValueKind == JsonValueKind.Null ? null : element.GetString();

    private static void WriteNullableString(
        Utf8JsonWriter writer,
        string propertyName,
        string? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
        }
        else
        {
            writer.WriteString(propertyName, value);
        }
    }

    internal static string FormatSide(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    internal static LandSide ParseSide(string? side) => side switch
    {
        "axis" => LandSide.Axis,
        "commonwealth" => LandSide.Commonwealth,
        _ => throw new JsonException($"Unknown Land side '{side}'."),
    };

    private static string FormatActorRole(LandActorRole role) => role switch
    {
        LandActorRole.None => "none",
        LandActorRole.Commonwealth => "commonwealth",
        LandActorRole.InitiativeHolder => "initiative-holder",
        LandActorRole.FirstActingSide => "first-acting-side",
        LandActorRole.SecondActingSide => "second-acting-side",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    private static LandActorRole ParseActorRole(string? role) => role switch
    {
        "none" => LandActorRole.None,
        "commonwealth" => LandActorRole.Commonwealth,
        "initiative-holder" => LandActorRole.InitiativeHolder,
        "first-acting-side" => LandActorRole.FirstActingSide,
        "second-acting-side" => LandActorRole.SecondActingSide,
        _ => throw new JsonException($"Unknown Land actor role '{role}'."),
    };

    internal static string FormatLocation(AxisInitiativeLocation location) => location switch
    {
        AxisInitiativeLocation.QualifyingGameMap => "qualifying-game-map",
        AxisInitiativeLocation.TripoliTunisiaHoldingBox =>
            "tripoli-tunisia-holding-box",
        AxisInitiativeLocation.OffMapOrUnavailable => "off-map-or-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(location)),
    };

    internal static AxisInitiativeLocation ParseLocation(string? location) => location switch
    {
        "qualifying-game-map" => AxisInitiativeLocation.QualifyingGameMap,
        "tripoli-tunisia-holding-box" => AxisInitiativeLocation.TripoliTunisiaHoldingBox,
        "off-map-or-unavailable" => AxisInitiativeLocation.OffMapOrUnavailable,
        _ => throw new JsonException($"Unknown Axis initiative location '{location}'."),
    };

    private static void Validate(CampaignSnapshot snapshot)
    {
        if (!CampaignSnapshotValidator.IsValid(snapshot))
        {
            throw new JsonException("The campaign snapshot contract is invalid.");
        }
    }
}
