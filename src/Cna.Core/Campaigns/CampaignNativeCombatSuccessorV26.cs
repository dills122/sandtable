using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Completes an accepted Combat tail into the ordinary campaign sequence. The
// custody companion remains authoritative alongside World until its assets
// have a versioned home in the normal campaign snapshot.
internal sealed record CampaignNativeCombatSuccessorV26(
    int ContractVersion, string CampaignId, long PriorStateVersion,
    string CustodyEventHash, string NextPositionId, string StateHash,
    string CustodyStateHash)
{
    internal const int CurrentContractVersion = 26;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal sealed record Projection(CampaignSnapshotV11 PriorSnapshot,
        long StateVersion, CampaignWorldSnapshotV6 World,
        RandomStreamState RandomState, LandSequencePosition NextPosition,
        CampaignNativeCombatCustodyStateV25? Custody)
    {
        internal byte[] SerializeCanonical()
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteNumber("contractVersion", CurrentContractVersion);
                writer.WriteString("campaignId", PriorSnapshot.CampaignId);
                writer.WriteNumber("stateVersion", StateVersion);
                writer.WriteString("priorSnapshotHash", Hash(
                    CampaignCurrentSnapshotSerializer.Serialize(PriorSnapshot)));
                writer.WriteString("positionId", NextPosition.PositionId);
                writer.WriteString("worldHash", CampaignNativeCombatPaidWorldV22.Hash(World));
                writer.WriteStartObject("randomState");
                writer.WriteNumber("contractVersion", RandomState.ContractVersion);
                writer.WriteString("algorithmId", RandomState.AlgorithmId);
                writer.WriteNumber("seed", RandomState.Seed);
                writer.WriteNumber("nextByteCursor", RandomState.NextByteCursor);
                writer.WriteEndObject();
                writer.WriteString("custodyStateHash", Hash(
                    CampaignNativeCombatCustodyProjectionV25.SerializeStateCanonical(Custody)));
                writer.WriteEndObject();
            }
            return stream.ToArray();
        }
    }

    internal static Projection Project(CampaignCombatSelectionFrame selected,
        CampaignNativeCombatAssaultResultV22 result,
        CampaignNativeCombatSettlementV24 settlement,
        CampaignNativeCombatCustodyDispositionV25 disposition,
        CampaignWorldSnapshotV6 custodyWorld,
        CampaignNativeCombatCustodyStateV25? custody,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(settlement);
        ArgumentNullException.ThrowIfNull(disposition);
        ArgumentNullException.ThrowIfNull(custodyWorld);
        ArgumentNullException.ThrowIfNull(context);
        var prior = selected.PriorSnapshot;
        if (prior.CampaignId != result.CampaignId
            || prior.CampaignId != settlement.CampaignId
            || prior.CampaignId != disposition.CampaignId
            || result.DecisionActionId != selected.Decision.ActionId
            || settlement.DecisionActionId != result.DecisionActionId
            || settlement.PriorStateVersion <= result.StateVersion
            || disposition.PriorStateVersion != settlement.StateVersion
            || disposition.SettlementHash != Hash(settlement.SerializeCanonical())
            || disposition.PositionId != result.PositionId
            || disposition.WorldHash != CampaignNativeCombatPaidWorldV22.Hash(custodyWorld)
            || disposition.CustodyStateHash != Hash(
                CampaignNativeCombatCustodyProjectionV25.SerializeStateCanonical(custody))
            || result.BeforeRandomState != selected.RandomState)
            throw new InvalidOperationException("Combat successor lacks accepted result and custody authority.");

        var position = Cna1979LandSequenceV4.CreateTurn(
            prior.CurrentPosition.SequenceContext.GameTurn).Single(value =>
            value.PositionId == disposition.PositionId);
        if (position.SegmentId != LandSegmentIds.Combat
            || position.StepId != LandStepIds.CloseAssault)
            throw new InvalidOperationException("Combat successor requires completed Close Assault.");
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (next.SegmentId != LandSegmentIds.ReserveRelease
            || next.GameTurn != position.GameTurn
            || next.OperationStage != position.OperationStage)
            throw new InvalidOperationException("Combat successor must enter same-slot Reserve Release.");

        var artifact = context.ArtifactV6
            ?? throw new InvalidOperationException("Combat successor needs normal Content 6.");
        if (!CampaignWorldV6Validator.IsValid(custodyWorld, artifact, context.Scenario))
            throw new InvalidOperationException("Combat successor World is invalid.");
        return new(prior, checked(disposition.StateVersion + 1), custodyWorld,
            result.AfterRandomState, next, custody);
    }

    internal static CampaignNativeCombatSuccessorV26 Create(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatAssaultResultV22 result,
        CampaignNativeCombatSettlementV24 settlement,
        CampaignNativeCombatCustodyDispositionV25 disposition,
        CampaignWorldSnapshotV6 custodyWorld,
        CampaignNativeCombatCustodyStateV25? custody,
        CampaignContentContext context)
    {
        var projected = Project(selected, result, settlement, disposition, custodyWorld, custody,
            context);
        return new(CurrentContractVersion, disposition.CampaignId,
            disposition.StateVersion, Hash(disposition.SerializeCanonical()),
            projected.NextPosition.PositionId,
            Hash(projected.SerializeCanonical()),
            Hash(CampaignNativeCombatCustodyProjectionV25.SerializeStateCanonical(
                projected.Custody)));
    }

    internal static CampaignNativeCombatSuccessorV26 Replay(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatAssaultResultV22 result,
        CampaignNativeCombatSettlementV24 settlement,
        CampaignNativeCombatCustodyDispositionV25 disposition,
        CampaignWorldSnapshotV6 custodyWorld,
        CampaignNativeCombatCustodyStateV25? custody,
        CampaignContentContext context, ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = Create(selected, result, settlement, disposition, custodyWorld,
            custody, context);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException(
                "Combat successor differs from accepted custody authority.");
        return expected;
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !IsHash(CustodyEventHash) || !IsHash(StateHash)
            || !IsHash(CustodyStateHash))
            throw new JsonException("Invalid native Combat successor event.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "combat-sequence-resumed");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("custodyEventHash", CustodyEventHash);
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(
                NextPositionId, nameof(NextPositionId)));
            writer.WriteString("stateHash", StateHash);
            writer.WriteString("custodyStateHash", CustodyStateHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeCombatSuccessorV26 Deserialize(
        ReadOnlyMemory<byte> canonicalEvent)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalEvent);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion",
                "eventType", "campaignId", "priorStateVersion", "custodyEventHash",
                "nextPositionId", "stateHash", "custodyStateHash");
            if (root.GetProperty("eventType").GetString() != "combat-sequence-resumed")
                throw new JsonException("Unknown native Combat successor event.");
            var parsed = new CampaignNativeCombatSuccessorV26(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("custodyEventHash").GetString()!,
                root.GetProperty("nextPositionId").GetString()!,
                root.GetProperty("stateHash").GetString()!,
                root.GetProperty("custodyStateHash").GetString()!);
            if (!canonicalEvent.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical native Combat successor event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or KeyNotFoundException or FormatException
            or NullReferenceException)
        {
            throw new JsonException("Invalid native Combat successor event.", exception);
        }
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9')
            or (>= 'a' and <= 'f'));
}
