using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal static class CampaignSnapshotSerializer
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
            WriteWorld(writer, "world", snapshot.World);

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

            CampaignOperationStageOrderCodec.Write(writer, snapshot.OperationStageOrders);
            CampaignOperationStageWeatherCodec.Write(writer, snapshot.OperationStageWeather);
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
                "world",
                "initiativeHolder",
                "operationStageOrders",
                "operationStageWeather",
                "randomState",
                "sequencePosition");

            var holderElement = root.GetProperty("initiativeHolder");
            var snapshot = new CampaignSnapshot(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("stateVersion").GetInt64(),
                root.GetProperty("rulesetHash").GetString()!,
                ParseSetup(root.GetProperty("setup")),
                ParseWorld(root.GetProperty("world")),
                holderElement.ValueKind == JsonValueKind.Null
                    ? null
                    : ParseSide(holderElement.GetString()),
                CampaignOperationStageOrderCodec.Parse(root.GetProperty("operationStageOrders")),
                CampaignOperationStageWeatherCodec.Parse(root.GetProperty("operationStageWeather")),
                ParseRandomState(root.GetProperty("randomState")),
                ParsePosition(root.GetProperty("sequencePosition")));

            Validate(snapshot);
            if (!canonicalJson.Span.SequenceEqual(Serialize(snapshot)))
            {
                throw new JsonException("The campaign snapshot is not canonical JSON.");
            }

            return snapshot;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or FormatException
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
        WriteOpeningPreamble(writer, setup.OpeningPreamble);
        WriteWeatherPolicy(writer, setup.Weather);
        CampaignStageEntryPolicyCodec.Write(writer, "stageEntry", setup.StageEntry);
        WriteContent(writer, setup.Content);
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
            "openingPreamble",
            "weather",
            "stageEntry",
            "content",
            "sources");

        return new CampaignSetupSnapshot(
            setup.GetProperty("schemaVersion").GetInt32(),
            setup.GetProperty("setupId").GetString()!,
            setup.GetProperty("setupHash").GetString()!,
            setup.GetProperty("isSynthetic").GetBoolean(),
            setup.GetProperty("initialGameTurn").GetInt32(),
            ParseInitiative(setup.GetProperty("initialInitiative")),
            ParseOpeningPreamble(setup.GetProperty("openingPreamble")),
            ParseWeatherPolicy(setup.GetProperty("weather")),
            CampaignStageEntryPolicyCodec.Parse(setup.GetProperty("stageEntry")),
            ParseContent(setup.GetProperty("content")),
            ParseSources(setup.GetProperty("sources")));
    }

    internal static void WriteWorld(
        Utf8JsonWriter writer,
        string propertyName,
        CampaignWorldSnapshot world)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteNumber("contractVersion", world.ContractVersion);
        writer.WriteStartArray("elements");

        foreach (var element in world.Elements)
        {
            writer.WriteStartObject();
            writer.WriteString("elementId", element.ElementId);
            writer.WriteString("currentLocationId", element.CurrentLocationId);
            writer.WriteString("reserveStatus", FormatReserveStatus(element.ReserveStatus));
            writer.WriteStartObject("operationalState");
            writer.WriteNumber(
                "ledgerGameTurn",
                element.OperationalState.LedgerGameTurn);
            writer.WriteNumber(
                "ledgerOperationStage",
                element.OperationalState.LedgerOperationStage);
            writer.WritePropertyName("capabilityPointsExpended");
            CapabilityPointAmountCodec.WriteCanonical(
                writer,
                element.OperationalState.CapabilityPointsExpended);
            writer.WriteNumber("cohesionLevel", element.OperationalState.CohesionLevel);
            WriteVehicleBreakdownState(
                writer,
                element.OperationalState.VehicleBreakdownState);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("representations");

        foreach (var representation in world.Representations)
        {
            writer.WriteStartObject();
            writer.WriteString("representationId", representation.RepresentationId);
            writer.WriteString("currentLocationId", representation.CurrentLocationId);
            writer.WriteString(
                "bindingKind",
                FormatRepresentationBindingKind(representation.BindingKind));
            writer.WriteStartArray("boundElementIds");
            foreach (var elementId in representation.BoundElementIds)
            {
                writer.WriteStringValue(elementId);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    internal static CampaignWorldSnapshot ParseWorld(JsonElement world)
    {
        RequireProperties(world, "contractVersion", "elements", "representations");
        return new CampaignWorldSnapshot(
            world.GetProperty("contractVersion").GetInt32(),
            world.GetProperty("elements")
                .EnumerateArray()
                .Select(element =>
                {
                    RequireProperties(
                        element,
                        "elementId",
                        "currentLocationId",
                        "reserveStatus",
                        "operationalState");
                    var operational = element.GetProperty("operationalState");
                    RequireProperties(
                        operational,
                        "ledgerGameTurn",
                        "ledgerOperationStage",
                        "capabilityPointsExpended",
                        "cohesionLevel",
                        "vehicleBreakdownState");
                    return new CampaignElementState(
                        element.GetProperty("elementId").GetString()!,
                        element.GetProperty("currentLocationId").GetString()!,
                        ParseReserveStatus(element.GetProperty("reserveStatus").GetString()),
                        new CampaignElementOperationalState(
                            operational.GetProperty("ledgerGameTurn").GetInt32(),
                            operational.GetProperty("ledgerOperationStage").GetInt32(),
                            CapabilityPointAmountCodec.Deserialize(
                                System.Text.Encoding.UTF8.GetBytes(
                                    operational
                                        .GetProperty("capabilityPointsExpended")
                                        .GetRawText())),
                            operational.GetProperty("cohesionLevel").GetInt32(),
                            ParseVehicleBreakdownState(
                                operational.GetProperty("vehicleBreakdownState"))));
                })
                .ToArray(),
            world.GetProperty("representations")
                .EnumerateArray()
                .Select(representation =>
                {
                    RequireProperties(
                        representation,
                        "representationId",
                        "currentLocationId",
                        "bindingKind",
                        "boundElementIds");
                    return new CampaignMapRepresentationState(
                        representation.GetProperty("representationId").GetString()!,
                        representation.GetProperty("currentLocationId").GetString()!,
                        ParseRepresentationBindingKind(
                            representation.GetProperty("bindingKind").GetString()),
                        representation.GetProperty("boundElementIds")
                            .EnumerateArray()
                            .Select(elementId => elementId.GetString()!)
                            .ToArray());
                })
                .ToArray());
    }

    private static void WriteVehicleBreakdownState(
        Utf8JsonWriter writer,
        CampaignVehicleBreakdownState? state)
    {
        if (state is null)
        {
            writer.WriteNull("vehicleBreakdownState");
            return;
        }

        writer.WriteStartObject("vehicleBreakdownState");
        writer.WriteString("cohortId", state.CohortId);
        writer.WritePropertyName("cumulativeBreakdownPoints");
        BreakdownPointAmountCodec.WriteCanonical(writer, state.CumulativeBreakdownPoints);
        writer.WritePropertyName("sandstormAttributedBreakdownPoints");
        BreakdownPointAmountCodec.WriteCanonical(
            writer,
            state.SandstormAttributedBreakdownPoints);
        WriteNullableString(
            writer,
            "highestEffectiveCheckedBandId",
            state.HighestEffectiveCheckedBandId);
        writer.WriteNumber("workingPointCount", state.WorkingPointCount);
        writer.WriteNumber("brokenPointCount", state.BrokenPointCount);
        writer.WriteEndObject();
    }

    private static CampaignVehicleBreakdownState? ParseVehicleBreakdownState(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        try
        {
            RequireProperties(
                element,
                "cohortId",
                "cumulativeBreakdownPoints",
                "sandstormAttributedBreakdownPoints",
                "highestEffectiveCheckedBandId",
                "workingPointCount",
                "brokenPointCount");
            return new CampaignVehicleBreakdownState(
                element.GetProperty("cohortId").GetString()!,
                BreakdownPointAmountCodec.Deserialize(
                    System.Text.Encoding.UTF8.GetBytes(
                        element.GetProperty("cumulativeBreakdownPoints").GetRawText())),
                BreakdownPointAmountCodec.Deserialize(
                    System.Text.Encoding.UTF8.GetBytes(
                        element
                            .GetProperty("sandstormAttributedBreakdownPoints")
                            .GetRawText())),
                ParseNullableString(element.GetProperty("highestEffectiveCheckedBandId")),
                element.GetProperty("workingPointCount").GetInt32(),
                element.GetProperty("brokenPointCount").GetInt32());
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or FormatException
            or InvalidOperationException
            or KeyNotFoundException)
        {
            throw new JsonException("The campaign vehicle Breakdown state is invalid.", exception);
        }
    }

    private static string FormatRepresentationBindingKind(
        CampaignMapRepresentationBindingKind bindingKind) => bindingKind switch
        {
            CampaignMapRepresentationBindingKind.IndependentElement => "independent-element",
            _ => throw new ArgumentOutOfRangeException(nameof(bindingKind)),
        };

    private static CampaignMapRepresentationBindingKind ParseRepresentationBindingKind(
        string? bindingKind) => bindingKind switch
        {
            "independent-element" => CampaignMapRepresentationBindingKind.IndependentElement,
            _ => throw new JsonException(
                $"Unknown map representation binding kind '{bindingKind}'."),
        };

    private static string FormatReserveStatus(CampaignElementReserveStatus status) => status switch
    {
        CampaignElementReserveStatus.None => "none",
        CampaignElementReserveStatus.ReserveI => "reserve-i",
        CampaignElementReserveStatus.ReserveII => "reserve-ii",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    private static CampaignElementReserveStatus ParseReserveStatus(string? status) => status switch
    {
        "none" => CampaignElementReserveStatus.None,
        "reserve-i" => CampaignElementReserveStatus.ReserveI,
        "reserve-ii" => CampaignElementReserveStatus.ReserveII,
        _ => throw new JsonException("The campaign element Reserve status is invalid."),
    };

    private static void WriteContent(Utf8JsonWriter writer, CampaignContentSelection content)
    {
        writer.WriteStartObject("content");
        writer.WriteNumber("schemaVersion", content.Pack.SchemaVersion);
        writer.WriteString("formatId", content.Pack.FormatId);
        writer.WriteString("packId", content.Pack.PackId);
        writer.WriteString("rulesetId", content.Pack.RulesetId);
        writer.WriteString("hash", content.Pack.Hash);
        writer.WriteString("scenarioId", content.ScenarioId);
        writer.WriteEndObject();
    }

    private static void WriteOpeningPreamble(
        Utf8JsonWriter writer,
        CampaignOpeningPreamblePolicy policy)
    {
        writer.WriteStartObject("openingPreamble");
        writer.WriteNumber("contractVersion", policy.ContractVersion);
        writer.WriteString(
            "kind",
            policy.Kind switch
            {
                CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations =>
                    "no-opening-naval-convoy-obligations",
                _ => throw new JsonException("Unknown opening preamble policy."),
            });
        WriteSources(writer, policy.Sources);
        writer.WriteEndObject();
    }

    private static CampaignOpeningPreamblePolicy ParseOpeningPreamble(JsonElement policy)
    {
        RequireProperties(policy, "contractVersion", "kind", "sources");
        var kind = policy.GetProperty("kind").GetString() switch
        {
            "no-opening-naval-convoy-obligations" =>
                CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations,
            var value => throw new JsonException($"Unknown opening preamble policy '{value}'."),
        };

        return new CampaignOpeningPreamblePolicy(
            policy.GetProperty("contractVersion").GetInt32(),
            kind,
            ParseSources(policy.GetProperty("sources")));
    }

    private static void WriteWeatherPolicy(
        Utf8JsonWriter writer,
        CampaignWeatherPolicy policy)
    {
        writer.WriteStartObject("weather");
        writer.WriteNumber("contractVersion", policy.ContractVersion);
        writer.WriteString(
            "kind",
            policy.Kind switch
            {
                CampaignWeatherPolicyKind.NoImmediateWeatherEffectSubjects =>
                    "no-immediate-weather-effect-subjects",
                _ => throw new JsonException("Unknown Weather policy."),
            });
        WriteSources(writer, policy.Sources);
        writer.WriteEndObject();
    }

    private static CampaignWeatherPolicy ParseWeatherPolicy(JsonElement policy)
    {
        RequireProperties(policy, "contractVersion", "kind", "sources");
        var kind = policy.GetProperty("kind").GetString() switch
        {
            "no-immediate-weather-effect-subjects" =>
                CampaignWeatherPolicyKind.NoImmediateWeatherEffectSubjects,
            var value => throw new JsonException($"Unknown Weather policy '{value}'."),
        };

        return new CampaignWeatherPolicy(
            policy.GetProperty("contractVersion").GetInt32(),
            kind,
            ParseSources(policy.GetProperty("sources")));
    }

    private static CampaignContentSelection ParseContent(JsonElement content)
    {
        RequireProperties(
            content,
            "schemaVersion",
            "formatId",
            "packId",
            "rulesetId",
            "hash",
            "scenarioId");
        return new CampaignContentSelection(
            new ContentPackIdentity(
                content.GetProperty("schemaVersion").GetInt32(),
                content.GetProperty("formatId").GetString()!,
                content.GetProperty("packId").GetString()!,
                content.GetProperty("rulesetId").GetString()!,
                content.GetProperty("hash").GetString()!),
            content.GetProperty("scenarioId").GetString()!);
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
        if (!CampaignSnapshotValidator.IsLocallyValid(snapshot))
        {
            throw new JsonException("The campaign snapshot contract is invalid.");
        }
    }
}
