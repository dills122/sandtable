using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// MTS-TASK-002 slice 1 (2026-11-22, docs/work/plans/2026-11-22-stage-entry-bridge-design-options.md):
// the second native-chain preamble event, following
// CampaignNativeInitiativeDeclarationV49. Reuses Cna1979Weather.Resolve and
// WeatherEventFactory.GetSources directly -- the weather table/dice logic
// itself is already correct and stage-generic (it only ever needed
// `gameTurn` and a `RandomStreamState`, never a CampaignSnapshotV11); this
// type only adds the native-chain plumbing around it (predecessor
// validation against the real catalog position, World/random-cursor
// threading), the same division of labor CampaignNativeSecondMoveV44 uses
// around CampaignElementMovedV3Factory's already-correct movement-cost
// logic.
//
// Unlike every mechanic in this chain so far, Weather is the only one of
// the six preamble steps that draws randomness (confirmed:
// CampaignV11Preamble.Create's ActFirstAction/ActLastAction and the four
// no-obligation cases never touch `prior.RandomState`; only
// ResolveWeatherAction does). Its predecessor,
// CampaignNativeInitiativeDeclarationV49, carries no RandomStreamState
// field at all (Initiative Declaration doesn't consume one), so this type
// takes its starting cursor as an explicit parameter. ExerciseSession.LatestCursor
// derives that value from the accepted predecessor history, as defined in
// docs/work/plans/2026-11-24-stage-entry-weather-cursor-contract.md. The factory
// remains independent of session history and its canonical byte layout is unchanged.
internal sealed record CampaignNativeWeatherDeterminationV50(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    string PositionId, string NextPositionId, string WorldHash, int GameTurn, int OperationStage,
    LandSide FirstSide, LandSide SecondSide, LandSide DeterminingSide, WeatherSeason Season,
    int FirstDie, int SecondDie, WeatherKind Kind, WeatherScope Scope, int? LocationDie,
    IReadOnlyList<WeatherArea> AffectedAreas, ulong PriorRandomCursor, ulong ResultRandomCursor)
{
    internal const int CurrentContractVersion = 50;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeWeatherDeterminationV50 Create(
        CampaignNativeInitiativeDeclarationV49 predecessor, RandomStreamState priorRandomState,
        int gameTurn, long contactVersion)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(priorRandomState);
        // The cursor's *value* is not re-derivable from the predecessor (it
        // carries none, per this file's own header comment) -- this only
        // guards that the caller passed a cursor shaped like a real
        // RandomStreamState for this algorithm, the same verification depth
        // CampaignNativeSecondMoveV44.Source applies to its own
        // caller-supplied inputs. The exercise submit path and cold replay
        // derive the value independently from the verified predecessor history.
        if (priorRandomState.ContractVersion != SandtableRandom.ContractVersion
            || priorRandomState.AlgorithmId != SandtableRandom.AlgorithmId)
            throw new InvalidOperationException("Weather Determination requires a valid random cursor.");
        if (contactVersion != predecessor.StateVersion || predecessor.GameTurn != gameTurn)
            throw new InvalidOperationException("Weather Determination belongs to the current cut.");
        var position = Cna1979LandSequenceV4.CreateTurn(gameTurn)
            .Single(value => value.PositionId == predecessor.NextPositionId);
        if (position.ActorRole != LandActorRole.InitiativeHolder
            || position.PhaseId != LandPhaseIds.WeatherDetermination)
            throw new InvalidOperationException("This phase does not admit Weather Determination.");
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (next.PhaseId != LandPhaseIds.Organization
            || next.GameTurn != position.GameTurn || next.OperationStage != position.OperationStage)
            throw new InvalidOperationException("Weather Determination must advance to Organization.");
        var resolution = Cna1979Weather.Resolve(gameTurn, priorRandomState);
        var priorBytes = predecessor.SerializeCanonical();
        return new(CurrentContractVersion, predecessor.CampaignId, predecessor.StateVersion,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(priorBytes)),
            position.PositionId, next.PositionId, predecessor.WorldHash, position.GameTurn, position.OperationStage,
            predecessor.FirstSide, predecessor.SecondSide,
            predecessor.DeclaringHolder, resolution.Season, resolution.FirstDie, resolution.SecondDie,
            resolution.Kind, resolution.Scope, resolution.LocationDie, resolution.AffectedAreas,
            priorRandomState.NextByteCursor, resolution.RandomState.NextByteCursor);
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1 || !IsHash(PriorEventHash)
            || !IsHash(WorldHash) || GameTurn < 1 || OperationStage is < 1 or > 3
            || !Enum.IsDefined(FirstSide) || !Enum.IsDefined(SecondSide) || FirstSide == SecondSide
            || !Enum.IsDefined(DeterminingSide) || !Enum.IsDefined(Season) || !Enum.IsDefined(Kind)
            || !Enum.IsDefined(Scope) || FirstDie is < 1 or > 6 || SecondDie is < 1 or > 6
            || (LocationDie is { } die && die is < 1 or > 6))
            throw new JsonException("Invalid Weather Determination.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "native-weather-determined");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("worldHash", WorldHash);
            writer.WriteNumber("gameTurn", GameTurn);
            writer.WriteNumber("operationStage", OperationStage);
            writer.WriteString("firstSide", CampaignSnapshotSerializer.FormatSide(FirstSide));
            writer.WriteString("secondSide", CampaignSnapshotSerializer.FormatSide(SecondSide));
            writer.WriteString("determiningSide", CampaignSnapshotSerializer.FormatSide(DeterminingSide));
            writer.WriteString("season", Season.ToString());
            writer.WriteNumber("firstDie", FirstDie);
            writer.WriteNumber("secondDie", SecondDie);
            writer.WriteString("kind", Kind.ToString());
            writer.WriteString("scope", Scope.ToString());
            if (LocationDie is { } locationDie) writer.WriteNumber("locationDie", locationDie);
            else writer.WriteNull("locationDie");
            writer.WriteStartArray("affectedAreas");
            foreach (var area in AffectedAreas) writer.WriteStringValue(area.ToString());
            writer.WriteEndArray();
            writer.WriteNumber("priorRandomCursor", PriorRandomCursor);
            writer.WriteNumber("resultRandomCursor", ResultRandomCursor);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeWeatherDeterminationV50 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Invalid event size.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "priorEventHash", "positionId", "nextPositionId", "worldHash", "gameTurn",
                "operationStage", "firstSide", "secondSide", "determiningSide", "season", "firstDie", "secondDie",
                "kind", "scope", "locationDie", "affectedAreas", "priorRandomCursor", "resultRandomCursor");
            if (root.GetProperty("eventType").GetString() != "native-weather-determined")
                throw new JsonException("Invalid event type.");
            LandSide ParseSide(string property) => root.GetProperty(property).GetString() switch
            {
                "axis" => LandSide.Axis,
                "commonwealth" => LandSide.Commonwealth,
                _ => throw new JsonException("Invalid side."),
            };
            var locationProperty = root.GetProperty("locationDie");
            var areas = root.GetProperty("affectedAreas").EnumerateArray()
                .Select(value => Enum.Parse<WeatherArea>(value.GetString()!)).ToArray();
            var parsed = new CampaignNativeWeatherDeterminationV50(root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!, root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!, root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!, root.GetProperty("worldHash").GetString()!,
                root.GetProperty("gameTurn").GetInt32(), root.GetProperty("operationStage").GetInt32(),
                ParseSide("firstSide"), ParseSide("secondSide"), ParseSide("determiningSide"),
                Enum.Parse<WeatherSeason>(root.GetProperty("season").GetString()!),
                root.GetProperty("firstDie").GetInt32(), root.GetProperty("secondDie").GetInt32(),
                Enum.Parse<WeatherKind>(root.GetProperty("kind").GetString()!),
                Enum.Parse<WeatherScope>(root.GetProperty("scope").GetString()!),
                locationProperty.ValueKind == JsonValueKind.Null ? null : locationProperty.GetInt32(),
                areas, root.GetProperty("priorRandomCursor").GetUInt64(),
                root.GetProperty("resultRandomCursor").GetUInt64());
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical())) throw new JsonException("Noncanonical event.");
            return parsed;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        {
            throw new JsonException("Invalid Weather Determination.", error);
        }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
