using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// MTS-TASK-002 slice 2 (2026-11-22, docs/work/plans/2026-11-22-stage-entry-bridge-design-options.md):
// the third native-chain preamble event. Unlike Initiative Declaration and
// Weather, Organization carries no decision/random data of its own in
// either the original V11 preamble (NoObligationOrganizationResolved,
// StageEntryEvents.cs:96-121) or here -- it is a pure no-obligation
// advancement. FirstSide/SecondSide/GameTurn/OperationStage are carried
// forward unchanged from the predecessor (the same WorldHash-style passthrough
// every native event in this chain already uses), since the terminal event
// of this six-event chain is what finally makes them self-contained per the
// owner's ruling, and every event between Initiative Declaration and the
// terminal one has to carry them that far somehow.
internal sealed record CampaignNativeNoObligationOrganizationV51(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    string PositionId, string NextPositionId, string WorldHash, int GameTurn, int OperationStage,
    LandSide FirstSide, LandSide SecondSide)
{
    internal const int CurrentContractVersion = 51;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeNoObligationOrganizationV51 Create(
        CampaignNativeWeatherDeterminationV50 predecessor, long contactVersion)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (contactVersion != predecessor.StateVersion)
            throw new InvalidOperationException("Organization belongs to the current cut.");
        var position = Cna1979LandSequenceV4.CreateTurn(predecessor.GameTurn)
            .Single(value => value.PositionId == predecessor.NextPositionId);
        if (position.PhaseId != LandPhaseIds.Organization || position.SegmentId is not null)
            throw new InvalidOperationException("This phase does not admit Organization.");
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (next.PhaseId != LandPhaseIds.NavalConvoyArrival
            || next.GameTurn != position.GameTurn || next.OperationStage != position.OperationStage)
            throw new InvalidOperationException("Organization must advance to Naval Convoy Arrival.");
        var priorBytes = predecessor.SerializeCanonical();
        return new(CurrentContractVersion, predecessor.CampaignId, predecessor.StateVersion,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(priorBytes)),
            position.PositionId, next.PositionId, predecessor.WorldHash, position.GameTurn, position.OperationStage,
            predecessor.FirstSide, predecessor.SecondSide);
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1 || !IsHash(PriorEventHash)
            || !IsHash(WorldHash) || GameTurn < 1 || OperationStage is < 1 or > 3
            || !Enum.IsDefined(FirstSide) || !Enum.IsDefined(SecondSide) || FirstSide == SecondSide)
            throw new JsonException("Invalid Organization resolution.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "native-no-obligation-organization-resolved");
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
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeNoObligationOrganizationV51 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Invalid event size.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "priorEventHash", "positionId", "nextPositionId", "worldHash", "gameTurn",
                "operationStage", "firstSide", "secondSide");
            if (root.GetProperty("eventType").GetString() != "native-no-obligation-organization-resolved")
                throw new JsonException("Invalid event type.");
            LandSide ParseSide(string property) => root.GetProperty(property).GetString() switch
            {
                "axis" => LandSide.Axis,
                "commonwealth" => LandSide.Commonwealth,
                _ => throw new JsonException("Invalid side."),
            };
            var parsed = new CampaignNativeNoObligationOrganizationV51(root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!, root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!, root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!, root.GetProperty("worldHash").GetString()!,
                root.GetProperty("gameTurn").GetInt32(), root.GetProperty("operationStage").GetInt32(),
                ParseSide("firstSide"), ParseSide("secondSide"));
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical())) throw new JsonException("Noncanonical event.");
            return parsed;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        {
            throw new JsonException("Invalid Organization resolution.", error);
        }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
