using System.Text.Json;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignEventSerializer
{
    public static byte[] Serialize(CampaignEvent campaignEvent)
    {
        ArgumentNullException.ThrowIfNull(campaignEvent);
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            switch (campaignEvent)
            {
                case CampaignCreated created:
                    ValidateCreated(created);
                    WriteCreated(writer, created);
                    break;
                case InitiativeDetermined determined:
                    ValidateDetermined(determined);
                    WriteDetermined(writer, determined);
                    break;
                case NoObligationNavalConvoyScheduleResolved resolved:
                    ValidateAdvance(resolved, LandPhaseIds.TacticalShipping);
                    WriteAdvance(writer, "no-obligation-naval-convoy-schedule-resolved", resolved);
                    break;
                case NoObligationTacticalShippingResolved resolved:
                    ValidateAdvance(resolved, LandPhaseIds.InitiativeDeclaration);
                    WriteAdvance(writer, "no-obligation-tactical-shipping-resolved", resolved);
                    break;
                case InitiativeOrderDeclared declared:
                    ValidateDeclaration(declared);
                    WriteDeclaration(writer, declared);
                    break;
                default:
                    throw new JsonException("The campaign event type is not serializable.");
            }

            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignEvent Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            var eventType = root.GetProperty("eventType").GetString();

            return eventType switch
            {
                "campaign-created" => ParseCreated(root),
                "initiative-determined" => ParseDetermined(root),
                "no-obligation-naval-convoy-schedule-resolved" => ParseSchedule(root),
                "no-obligation-tactical-shipping-resolved" => ParseTactical(root),
                "initiative-order-declared" => ParseDeclaration(root),
                _ => throw new JsonException($"Unknown campaign event type '{eventType}'."),
            };
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
            throw new JsonException("The campaign event JSON is invalid.", exception);
        }
    }

    private static void WriteCreated(Utf8JsonWriter writer, CampaignCreated created)
    {
        writer.WriteNumber("contractVersion", created.ContractVersion);
        writer.WriteString("eventType", "campaign-created");
        writer.WriteString("campaignId", created.CampaignId);
        writer.WriteNumber("stateVersion", created.StateVersion);
        writer.WriteString("rulesetHash", created.RulesetHash);
        CampaignSnapshotSerializer.WriteSetup(writer, created.Setup);
        CampaignSnapshotSerializer.WriteWorld(writer, "initialWorld", created.InitialWorld);
        CampaignSnapshotSerializer.WriteRandomState(writer, created.RandomState);
        CampaignSnapshotSerializer.WritePosition(writer, created.SequencePosition);
    }

    private static CampaignCreated ParseCreated(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "rulesetHash",
            "setup",
            "initialWorld",
            "randomState",
            "sequencePosition");
        var created = new CampaignCreated(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("rulesetHash").GetString()!,
            CampaignSnapshotSerializer.ParseSetup(root.GetProperty("setup")),
            CampaignSnapshotSerializer.ParseWorld(root.GetProperty("initialWorld")),
            CampaignSnapshotSerializer.ParseRandomState(root.GetProperty("randomState")),
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")));

        if (root.GetProperty("contractVersion").GetInt32() != created.ContractVersion)
        {
            throw new JsonException("The campaign creation contract version is invalid.");
        }

        ValidateCreated(created);
        return created;
    }

    private static void WriteDetermined(Utf8JsonWriter writer, InitiativeDetermined determined)
    {
        writer.WriteNumber("contractVersion", determined.ContractVersion);
        writer.WriteString("eventType", "initiative-determined");
        writer.WriteString("campaignId", determined.CampaignId);
        writer.WriteNumber("stateVersion", determined.StateVersion);
        writer.WriteString("fromPositionId", determined.FromPositionId);
        writer.WriteStartObject("outcome");
        WriteOutcome(writer, determined.Outcome);
        writer.WriteEndObject();
        writer.WriteString("randomAlgorithmId", determined.RandomAlgorithmId);
        writer.WriteNumber("randomCursorBefore", determined.RandomCursorBefore);
        writer.WriteNumber("randomCursorAfter", determined.RandomCursorAfter);
        CampaignSnapshotSerializer.WritePosition(writer, determined.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, determined.Sources);
    }

    private static void WriteAdvance(Utf8JsonWriter writer, string eventType,
        OpeningPreambleAdvanced resolved)
    {
        writer.WriteNumber("contractVersion", resolved.ContractVersion);
        writer.WriteString("eventType", eventType);
        writer.WriteString("campaignId", resolved.CampaignId);
        writer.WriteNumber("stateVersion", resolved.StateVersion);
        writer.WriteString("fromPositionId", resolved.FromPositionId);
        CampaignSnapshotSerializer.WritePosition(writer, resolved.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, resolved.Sources);
    }

    private static NoObligationNavalConvoyScheduleResolved ParseSchedule(JsonElement root)
    {
        RequireAdvanceProperties(root);
        var value = new NoObligationNavalConvoyScheduleResolved(
            root.GetProperty("campaignId").GetString()!, root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
        if (root.GetProperty("contractVersion").GetInt32() != value.ContractVersion)
            throw new JsonException("The schedule event contract version is invalid.");
        ValidateAdvance(value, LandPhaseIds.TacticalShipping);
        return value;
    }

    private static NoObligationTacticalShippingResolved ParseTactical(JsonElement root)
    {
        RequireAdvanceProperties(root);
        var value = new NoObligationTacticalShippingResolved(
            root.GetProperty("campaignId").GetString()!, root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
        if (root.GetProperty("contractVersion").GetInt32() != value.ContractVersion)
            throw new JsonException("The tactical event contract version is invalid.");
        ValidateAdvance(value, LandPhaseIds.InitiativeDeclaration);
        return value;
    }

    private static void RequireAdvanceProperties(JsonElement root) => CampaignSnapshotSerializer.RequireProperties(
        root, "contractVersion", "eventType", "campaignId", "stateVersion", "fromPositionId",
        "sequencePosition", "sources");

    private static void WriteDeclaration(Utf8JsonWriter writer, InitiativeOrderDeclared declared)
    {
        writer.WriteNumber("contractVersion", declared.ContractVersion);
        writer.WriteString("eventType", "initiative-order-declared");
        writer.WriteString("campaignId", declared.CampaignId);
        writer.WriteNumber("stateVersion", declared.StateVersion);
        writer.WriteString("fromPositionId", declared.FromPositionId);
        writer.WriteNumber("operationStage", declared.OperationStage);
        writer.WriteString("declaringHolder", CampaignSnapshotSerializer.FormatSide(declared.DeclaringHolder));
        writer.WriteString("firstSide", CampaignSnapshotSerializer.FormatSide(declared.FirstSide));
        writer.WriteString("secondSide", CampaignSnapshotSerializer.FormatSide(declared.SecondSide));
        CampaignSnapshotSerializer.WritePosition(writer, declared.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, declared.Sources);
    }

    private static InitiativeOrderDeclared ParseDeclaration(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId",
            "stateVersion", "fromPositionId", "operationStage", "declaringHolder", "firstSide",
            "secondSide", "sequencePosition", "sources");
        var value = new InitiativeOrderDeclared(root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(), root.GetProperty("fromPositionId").GetString()!,
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")),
            root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParseSide(root.GetProperty("declaringHolder").GetString()),
            CampaignSnapshotSerializer.ParseSide(root.GetProperty("firstSide").GetString()),
            CampaignSnapshotSerializer.ParseSide(root.GetProperty("secondSide").GetString()),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
        if (root.GetProperty("contractVersion").GetInt32() != value.ContractVersion)
            throw new JsonException("The declaration event contract version is invalid.");
        ValidateDeclaration(value);
        return value;
    }

    private static InitiativeDetermined ParseDetermined(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "fromPositionId",
            "outcome",
            "randomAlgorithmId",
            "randomCursorBefore",
            "randomCursorAfter",
            "sequencePosition",
            "sources");
        var determined = new InitiativeDetermined(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            ParseOutcome(root.GetProperty("outcome")),
            root.GetProperty("randomAlgorithmId").GetString()!,
            root.GetProperty("randomCursorBefore").GetUInt64(),
            root.GetProperty("randomCursorAfter").GetUInt64(),
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));

        if (root.GetProperty("contractVersion").GetInt32() != determined.ContractVersion)
        {
            throw new JsonException("The Initiative event contract version is invalid.");
        }

        ValidateDetermined(determined);
        return determined;
    }

    private static void WriteOutcome(Utf8JsonWriter writer, InitiativeOutcome outcome)
    {
        switch (outcome)
        {
            case PredeterminedInitiativeOutcome predetermined:
                writer.WriteString("kind", "predetermined");
                writer.WriteString(
                    "holder",
                    CampaignSnapshotSerializer.FormatSide(predetermined.Holder));
                break;
            case ContestedInitiativeOutcome contested:
                writer.WriteString("kind", "contested");
                writer.WriteStartObject("axisFacts");
                writer.WriteString(
                    "rommelLocation",
                    CampaignSnapshotSerializer.FormatLocation(
                        contested.AxisFacts.RommelLocation));
                writer.WriteStartArray("germanLandCombatUnitLocations");

                foreach (var location in contested.AxisFacts.GermanLandCombatUnitLocations)
                {
                    writer.WriteStringValue(
                        CampaignSnapshotSerializer.FormatLocation(location));
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteString("axisPresence", FormatPresence(contested.AxisPresence));
                writer.WriteStartArray("rounds");

                foreach (var round in contested.Rounds)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("round", round.Round);
                    writer.WriteNumber("axisDie", round.AxisDie);
                    writer.WriteNumber("axisRating", round.AxisRating);
                    writer.WriteNumber("axisTotal", round.AxisTotal);
                    writer.WriteNumber("commonwealthDie", round.CommonwealthDie);
                    writer.WriteNumber("commonwealthRating", round.CommonwealthRating);
                    writer.WriteNumber("commonwealthTotal", round.CommonwealthTotal);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteString(
                    "holder",
                    CampaignSnapshotSerializer.FormatSide(contested.Holder));
                break;
            default:
                throw new JsonException("The Initiative outcome type is not serializable.");
        }
    }

    private static InitiativeOutcome ParseOutcome(JsonElement outcome)
    {
        var kind = outcome.GetProperty("kind").GetString();

        return kind switch
        {
            "predetermined" => ParsePredeterminedOutcome(outcome),
            "contested" => ParseContestedOutcome(outcome),
            _ => throw new JsonException($"Unknown Initiative outcome '{kind}'."),
        };
    }

    private static PredeterminedInitiativeOutcome ParsePredeterminedOutcome(JsonElement outcome)
    {
        CampaignSnapshotSerializer.RequireProperties(outcome, "kind", "holder");
        return new PredeterminedInitiativeOutcome(
            CampaignSnapshotSerializer.ParseSide(outcome.GetProperty("holder").GetString()));
    }

    private static ContestedInitiativeOutcome ParseContestedOutcome(JsonElement outcome)
    {
        CampaignSnapshotSerializer.RequireProperties(
            outcome,
            "kind",
            "axisFacts",
            "axisPresence",
            "rounds",
            "holder");
        var facts = outcome.GetProperty("axisFacts");
        CampaignSnapshotSerializer.RequireProperties(
            facts,
            "rommelLocation",
            "germanLandCombatUnitLocations");
        var axisFacts = new AxisInitiativeSourceFacts(
            CampaignSnapshotSerializer.ParseLocation(
                facts.GetProperty("rommelLocation").GetString()),
            facts.GetProperty("germanLandCombatUnitLocations")
                .EnumerateArray()
                .Select(location => CampaignSnapshotSerializer.ParseLocation(
                    location.GetString()))
                .ToArray());
        var rounds = outcome.GetProperty("rounds")
            .EnumerateArray()
            .Select(ParseRound)
            .ToArray();

        return new ContestedInitiativeOutcome(
            axisFacts,
            ParsePresence(outcome.GetProperty("axisPresence").GetString()),
            rounds,
            CampaignSnapshotSerializer.ParseSide(
                outcome.GetProperty("holder").GetString()));
    }

    private static InitiativeRollRound ParseRound(JsonElement round)
    {
        CampaignSnapshotSerializer.RequireProperties(
            round,
            "round",
            "axisDie",
            "axisRating",
            "axisTotal",
            "commonwealthDie",
            "commonwealthRating",
            "commonwealthTotal");

        return new InitiativeRollRound(
            round.GetProperty("round").GetInt32(),
            round.GetProperty("axisDie").GetInt32(),
            round.GetProperty("axisRating").GetInt32(),
            round.GetProperty("axisTotal").GetInt32(),
            round.GetProperty("commonwealthDie").GetInt32(),
            round.GetProperty("commonwealthRating").GetInt32(),
            round.GetProperty("commonwealthTotal").GetInt32());
    }

    private static string FormatPresence(AxisInitiativePresence presence) => presence switch
    {
        AxisInitiativePresence.RommelOnQualifyingGameMap =>
            "rommel-on-qualifying-game-map",
        AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap =>
            "german-land-combat-unit-on-qualifying-game-map",
        AxisInitiativePresence.NeitherOnQualifyingGameMap =>
            "neither-on-qualifying-game-map",
        _ => throw new ArgumentOutOfRangeException(nameof(presence)),
    };

    private static AxisInitiativePresence ParsePresence(string? presence) => presence switch
    {
        "rommel-on-qualifying-game-map" =>
            AxisInitiativePresence.RommelOnQualifyingGameMap,
        "german-land-combat-unit-on-qualifying-game-map" =>
            AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap,
        "neither-on-qualifying-game-map" =>
            AxisInitiativePresence.NeitherOnQualifyingGameMap,
        _ => throw new JsonException($"Unknown Axis Initiative presence '{presence}'."),
    };

    private static void ValidateCreated(CampaignCreated created)
    {
        if (created.Setup is null
            || created.InitialWorld is null
            || created.RandomState is null
            || created.SequencePosition is null)
        {
            throw new JsonException("The campaign creation event is invalid.");
        }

        var localSnapshot = new CampaignSnapshot(
            4,
            created.CampaignId,
            created.StateVersion,
            created.RulesetHash,
            created.Setup,
            created.InitialWorld,
            null,
            [],
            created.RandomState,
            created.SequencePosition);

        if (created.ContractVersion != 4
            || created.StateVersion != 1
            || created.RandomState.NextByteCursor != 0
            || !CampaignSnapshotValidator.IsLocallyValid(localSnapshot))
        {
            throw new JsonException("The campaign creation event is invalid.");
        }
    }

    private static void ValidateDetermined(InitiativeDetermined determined)
    {
        if (determined.ContractVersion != 2
            || determined.StateVersion < 2
            || !string.Equals(
                determined.RandomAlgorithmId,
                SandtableRandom.AlgorithmId,
                StringComparison.Ordinal)
            || determined.SequencePosition.ContractVersion != Cna1979LandSequence.ContractVersion
            || determined.SequencePosition.StageId != LandStageIds.NavalConvoy
            || determined.SequencePosition.PhaseId != LandPhaseIds.NavalConvoySchedule
            || determined.SequencePosition.ActorRole != LandActorRole.None
            || determined.SequencePosition.ActiveSide is not null)
        {
            throw new JsonException("The Initiative event contract is invalid.");
        }
    }

    private static void ValidateAdvance(OpeningPreambleAdvanced resolved, string expectedPhase)
    {
        if (resolved.ContractVersion != 1 || resolved.StateVersion is < 3 or > 4
            || resolved.SequencePosition.PhaseId != expectedPhase)
            throw new JsonException("The preamble event contract is invalid.");
    }

    private static void ValidateDeclaration(InitiativeOrderDeclared declared)
    {
        if (declared.ContractVersion != 1 || declared.StateVersion != 5 || declared.OperationStage != 1
            || declared.SequencePosition.PhaseId != LandPhaseIds.WeatherDetermination
            || declared.FirstSide == declared.SecondSide
            || (declared.FirstSide != declared.DeclaringHolder
                && declared.SecondSide != declared.DeclaringHolder))
            throw new JsonException("The declaration event contract is invalid.");
    }
}
