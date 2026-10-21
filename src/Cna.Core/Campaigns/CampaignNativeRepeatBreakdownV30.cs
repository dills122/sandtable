using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// A cohortless repeated move has no vehicle breakdown roll. Record that
// determination before entering Combat so replay cannot skip a sequence step.
internal sealed record CampaignNativeRepeatBreakdownV30(
    int ContractVersion, string CampaignId, long PriorStateVersion,
    string PriorEventHash, int CycleOrdinal, string PositionId,
    string NextPositionId, string WorldHash)
{
    internal const int CurrentContractVersion = 30;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeRepeatBreakdownV30 Create(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignNativeCycleControlV28 control,
        CampaignNativeRepeatMovementV29 movement,
        CampaignWorldSnapshotV6 movedWorld,
        CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(movedWorld);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        if (control.Choice != CampaignNativeCycleControlChoiceV28.Repeat
            || movement.CampaignId != release.CampaignId
            || movement.PriorStateVersion != control.StateVersion
            || movement.CycleOrdinal != control.NextCycleOrdinal
            || movement.PositionId != control.NextPositionId
            || movement.PriorEventHash != Hash(control.SerializeCanonical())
            || movement.PriorWorldHash
                != CampaignNativeCombatPaidWorldV22.Hash(release.World)
            || contacts.AuthorityVersion != movement.StateVersion
            || movement.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(movedWorld)
            || movedWorld.Elements.Single(value => value.ElementId == movement.ElementId)
                .CurrentLocationId != movement.DestinationLocationId)
            throw new InvalidOperationException(
                "Breakdown determination needs an accepted repeated move.");
        var content = context.ArtifactV6?.Definition.LegacyDefinition.Elements
            .Single(value => value.ElementId == movement.ElementId)
            ?? throw new InvalidOperationException("Breakdown needs normal Content 6.");
        if (content.SideId != CampaignSnapshotSerializer.FormatSide(release.ActingSide)
            || content.BreakdownVehicleCohort is not null)
            throw new InvalidOperationException(
                "A vehicle move needs its breakdown accounting.");
        var movementPosition = Cna1979LandSequenceV4.CreateTurn(
            release.Position.GameTurn).Single(value =>
                value.PositionId == movement.PositionId);
        var breakdown = Cna1979LandSequenceV4.GetNext(movementPosition);
        var combat = Cna1979LandSequenceV4.GetNext(breakdown);
        if (movementPosition.SegmentId != LandSegmentIds.Movement
            || movementPosition.OperationStage != release.Position.OperationStage
            || movementPosition.ActorRole != release.Position.ActorRole
            || breakdown.SegmentId != LandSegmentIds.BreakdownDetermination
            || combat.SegmentId != LandSegmentIds.Combat
            || combat.StepId != LandStepIds.PositionDetermination)
            throw new InvalidOperationException(
                "Repeated Movement does not enter same-slot Combat.");
        return new(CurrentContractVersion, release.CampaignId,
            movement.StateVersion, Hash(movement.SerializeCanonical()),
            movement.CycleOrdinal, breakdown.PositionId, combat.PositionId,
            movement.ResultWorldHash);
    }

    internal static CampaignNativeRepeatBreakdownV30 Replay(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignNativeCycleControlV28 control,
        CampaignNativeRepeatMovementV29 movement,
        CampaignWorldSnapshotV6 movedWorld,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var parsed = Deserialize(canonicalEvent);
        var expected = Create(release, control, movement, movedWorld,
            context, contacts);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException(
                "Repeated Breakdown differs from accepted Movement.");
        return expected;
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || CycleOrdinal < 2 || !IsHash(PriorEventHash)
            || !IsHash(WorldHash))
            throw new JsonException("Invalid repeated Breakdown event.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "cycle-repeat-breakdown-none");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteNumber("cycleOrdinal", CycleOrdinal);
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(
                PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(
                NextPositionId, nameof(NextPositionId)));
            writer.WriteString("worldHash", WorldHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeRepeatBreakdownV30 Deserialize(
        ReadOnlyMemory<byte> canonicalEvent)
    {
        try
        {
            if (canonicalEvent.Length is < 1 or > 4096)
                throw new JsonException("Repeated Breakdown exceeds its byte bound.");
            using var document = JsonDocument.Parse(canonicalEvent);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion",
                "eventType", "campaignId", "priorStateVersion",
                "priorEventHash", "cycleOrdinal", "positionId",
                "nextPositionId", "worldHash");
            if (root.GetProperty("eventType").GetString()
                != "cycle-repeat-breakdown-none")
                throw new JsonException("Unknown repeated Breakdown event.");
            var parsed = new CampaignNativeRepeatBreakdownV30(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!,
                root.GetProperty("cycleOrdinal").GetInt32(),
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!,
                root.GetProperty("worldHash").GetString()!);
            if (!canonicalEvent.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical repeated Breakdown event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or KeyNotFoundException
            or FormatException)
        {
            throw new JsonException("Invalid repeated Breakdown event.", exception);
        }
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9')
            or (>= 'a' and <= 'f'));
}
