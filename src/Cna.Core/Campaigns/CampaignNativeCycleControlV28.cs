using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

internal enum CampaignNativeCycleControlChoiceV28
{
    Repeat,
    Finish,
}

// Reserve Release closes its own segment. This separate event chooses one of
// the two source-backed cycle edges without treating a linear catalog edge as
// an implicit player decision.
internal sealed record CampaignNativeCycleControlV28(
    int ContractVersion, string CampaignId, long PriorStateVersion,
    string PriorEventHash, CampaignNativeCycleControlChoiceV28 Choice,
    int PriorCycleOrdinal, string ResultWorldHash,
    string NextPositionId, string? MovementWitnessActionId)
{
    internal const int CurrentContractVersion = 28;
    internal long StateVersion => checked(PriorStateVersion + 1);
    internal int NextCycleOrdinal => Choice == CampaignNativeCycleControlChoiceV28.Repeat
        ? checked(PriorCycleOrdinal + 1) : PriorCycleOrdinal;

    internal static CampaignNativeCycleControlV28 Create(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        CampaignNativeCycleControlChoiceV28 choice)
    {
        ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        if (!Enum.IsDefined(choice) || !release.Completed
            || release.Position.SegmentId != LandSegmentIds.ReserveRelease
            || release.Position.ActorRole != LandActorRole.FirstActingSide
            || release.Pending(context).Count != 0
            || contacts.CampaignId != release.CampaignId
            || contacts.AuthorityVersion != release.StateVersion)
            throw new InvalidOperationException("Cycle control requires completed same-cut Reserve Release.");

        LandSequencePosition next;
        string? witnessActionId = null;
        if (choice == CampaignNativeCycleControlChoiceV28.Repeat)
        {
            var witness = CampaignCycleContinuationMovementWitnessV28.Find(
                release, context, contacts)
                ?? throw new InvalidOperationException(
                    "Repeat requires a supported source-legal movement witness.");
            witnessActionId = witness.ActionId;
            var movement = Cna1979LandSequenceV4.CreateTurn(
                release.Position.GameTurn).Single(value =>
                    value.OperationStage == release.Position.OperationStage
                    && value.ActorRole == release.Position.ActorRole
                    && value.SegmentId == LandSegmentIds.Movement);
            next = new LandSequencePosition(movement.ContractVersion,
                movement.PositionId, movement.GameTurn,
                movement.OperationStage, movement.StageId,
                movement.PhaseId, movement.SegmentId, movement.StepId,
                movement.ActorRole, release.ActingSide, movement.Sources);
            Cna1979LandSequenceV4.RequireMaterializedMovement(next);
        }
        else
        {
            next = Cna1979LandSequenceV4.GetNext(release.Position);
            if (next.PhaseId != LandPhaseIds.TruckConvoyMovement
                || next.GameTurn != release.Position.GameTurn
                || next.OperationStage != release.Position.OperationStage
                || next.ActorRole != release.Position.ActorRole)
                throw new InvalidOperationException(
                    "Finish must enter same-slot Truck Convoy Movement.");
        }
        return new(CurrentContractVersion, release.CampaignId,
            release.StateVersion, release.HeadEventHash, choice,
            release.CycleOrdinal,
            CampaignNativeCombatPaidWorldV22.Hash(release.World),
            next.PositionId, witnessActionId);
    }

    internal static CampaignNativeCycleControlV28 Replay(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = Create(release, context, contacts, parsed.Choice);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException(
                "Cycle control differs from completed Reserve Release authority.");
        return expected;
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || PriorCycleOrdinal < 1 || !Enum.IsDefined(Choice)
            || !IsHash(PriorEventHash) || !IsHash(ResultWorldHash)
            || (Choice == CampaignNativeCycleControlChoiceV28.Repeat)
                != (MovementWitnessActionId is not null)
            || MovementWitnessActionId is not null && !IsHash(MovementWitnessActionId))
            throw new JsonException("Invalid cycle control event.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "movement-combat-cycle-control");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteString("choice", Choice.ToString());
            writer.WriteNumber("priorCycleOrdinal", PriorCycleOrdinal);
            writer.WriteString("resultWorldHash", ResultWorldHash);
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(
                NextPositionId, nameof(NextPositionId)));
            writer.WriteString("movementWitnessActionId", MovementWitnessActionId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeCycleControlV28 Deserialize(
        ReadOnlyMemory<byte> canonicalEvent)
    {
        try
        {
            if (canonicalEvent.Length is < 1 or > 8192)
                throw new JsonException("Cycle control event exceeds its byte bound.");
            using var document = JsonDocument.Parse(canonicalEvent);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion",
                "eventType", "campaignId", "priorStateVersion", "priorEventHash",
                "choice", "priorCycleOrdinal", "resultWorldHash", "nextPositionId",
                "movementWitnessActionId");
            if (root.GetProperty("eventType").GetString()
                != "movement-combat-cycle-control")
                throw new JsonException("Unknown cycle control event.");
            var parsed = new CampaignNativeCycleControlV28(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!,
                Enum.Parse<CampaignNativeCycleControlChoiceV28>(
                    root.GetProperty("choice").GetString()!),
                root.GetProperty("priorCycleOrdinal").GetInt32(),
                root.GetProperty("resultWorldHash").GetString()!,
                root.GetProperty("nextPositionId").GetString()!,
                root.GetProperty("movementWitnessActionId").GetString());
            if (!canonicalEvent.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical cycle control event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or KeyNotFoundException or FormatException
            or NullReferenceException)
        {
            throw new JsonException("Invalid cycle control event.", exception);
        }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9')
            or (>= 'a' and <= 'f'));
}
