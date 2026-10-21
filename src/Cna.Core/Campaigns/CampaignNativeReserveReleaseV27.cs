using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignNativeReserveReleaseEventV27(
    int ContractVersion, string CampaignId, long PriorStateVersion,
    string PriorEventHash, string Kind, string? ElementId,
    CampaignReserveReleaseChoice? Choice, string ResultWorldHash,
    string NextPositionId)
{
    internal const int CurrentContractVersion = 27;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion < 1
            || !IsHash(PriorEventHash) || !IsHash(ResultWorldHash)
            || (Kind == "member-choice" && (ElementId is null || Choice is null))
            || (Kind == "completed" && (ElementId is not null || Choice is not null))
            || (Choice is not null && !Enum.IsDefined(Choice.Value))
            || Kind is not ("member-choice" or "completed"))
            throw new JsonException("Invalid Reserve Release event.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "reserve-release-" + Kind);
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(
                CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteString("elementId", ElementId is null ? null
                : ContentContractGuards.RequireStableId(ElementId, nameof(ElementId)));
            writer.WriteString("choice", Choice?.ToString());
            writer.WriteString("resultWorldHash", ResultWorldHash);
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(
                NextPositionId, nameof(NextPositionId)));
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeReserveReleaseEventV27 Deserialize(
        ReadOnlyMemory<byte> canonicalEvent)
    {
        try
        {
            if (canonicalEvent.Length is < 1 or > 8192)
                throw new JsonException("Reserve Release event exceeds its byte bound.");
            using var document = JsonDocument.Parse(canonicalEvent);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion",
                "eventType", "campaignId", "priorStateVersion", "priorEventHash",
                "elementId", "choice", "resultWorldHash", "nextPositionId");
            var eventType = root.GetProperty("eventType").GetString();
            var kind = eventType switch
            {
                "reserve-release-member-choice" => "member-choice",
                "reserve-release-completed" => "completed",
                _ => throw new JsonException("Unknown Reserve Release event."),
            };
            var choiceText = root.GetProperty("choice").GetString();
            var choice = choiceText is null ? (CampaignReserveReleaseChoice?)null
                : Enum.Parse<CampaignReserveReleaseChoice>(choiceText);
            var parsed = new CampaignNativeReserveReleaseEventV27(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!, kind,
                root.GetProperty("elementId").GetString(), choice,
                root.GetProperty("resultWorldHash").GetString()!,
                root.GetProperty("nextPositionId").GetString()!);
            if (!canonicalEvent.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical Reserve Release event.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or KeyNotFoundException or FormatException
            or NullReferenceException)
        {
            throw new JsonException("Invalid Reserve Release event.", exception);
        }
    }

    internal static string Hash(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9')
            or (>= 'a' and <= 'f'));
}

internal sealed record CampaignNativeReserveReleaseStateV27(
    string CampaignId, long StateVersion, string HeadEventHash,
    LandSide ActingSide, int CycleOrdinal,
    LandSequencePosition Position, CampaignWorldSnapshotV6 World,
    IReadOnlyList<CampaignReserveReleaseMovementAllowance> NextMovement,
    IReadOnlyList<string> ResolvedElementIds, bool Completed)
{
    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignNativeReserveReleaseEventV27.CurrentContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("headEventHash", HeadEventHash);
            writer.WriteString("actingSide", ActingSide.ToString());
            writer.WriteNumber("cycleOrdinal", CycleOrdinal);
            writer.WriteString("positionId", Position.PositionId);
            writer.WriteString("worldHash", CampaignNativeCombatPaidWorldV22.Hash(World));
            writer.WriteStartArray("nextMovement");
            foreach (var allowance in NextMovement)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", allowance.ElementId);
                writer.WriteNumber("nextCycleOrdinal", allowance.NextCycleOrdinal);
                writer.WriteNumber("capabilityPointCeiling", allowance.CapabilityPointCeiling);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("resolvedElementIds");
            foreach (var elementId in ResolvedElementIds)
                writer.WriteStringValue(elementId);
            writer.WriteEndArray();
            writer.WriteBoolean("completed", Completed);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeReserveReleaseStateV27 Initialize(
        CampaignNativeCombatSuccessorV26 predecessor,
        CampaignNativeCombatSuccessorV26.Projection projection,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(context);
        if (predecessor.CampaignId != projection.PriorSnapshot.CampaignId
            || predecessor.StateVersion != projection.StateVersion
            || predecessor.NextPositionId != projection.NextPosition.PositionId
            || predecessor.StateHash != CampaignNativeReserveReleaseEventV27.Hash(
                projection.SerializeCanonical())
            || predecessor.CustodyStateHash != CampaignNativeReserveReleaseEventV27.Hash(
                CampaignNativeCombatCustodyProjectionV25.SerializeStateCanonical(
                    projection.Custody))
            || projection.NextPosition.SegmentId != LandSegmentIds.ReserveRelease
            || projection.NextPosition.ActorRole != LandActorRole.FirstActingSide)
            throw new InvalidOperationException(
                "Reserve Release must follow the accepted normal Combat successor.");
        var firstSide = projection.PriorSnapshot.OperationStageOrders.Single().FirstSide;
        _ = CampaignReserveReleaseRules.Pending(projection.World, context,
            firstSide, 1);
        return new(predecessor.CampaignId, predecessor.StateVersion,
            CampaignNativeReserveReleaseEventV27.Hash(predecessor.SerializeCanonical()),
            firstSide, 1, projection.NextPosition, projection.World, [], [], false);
    }

    internal static CampaignNativeReserveReleaseStateV27 InitializeAfterRepeatNoAttack(
        CampaignSnapshotV11 initial,
        CampaignNativeRepeatCombatDecisionV31 selection,
        CampaignNativeRepeatCombatNoAttackV32 completion,
        CampaignWorldSnapshotV6 world,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(completion);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(context);
        var artifact = context.ArtifactV6
            ?? throw new InvalidOperationException(
                "Later-cycle Reserve Release needs registered Content 6.");
        if (initial.CampaignId != selection.CampaignId
            || selection.Choice != "finish-combat-without-attack"
            || completion.CampaignId != selection.CampaignId
            || completion.PriorStateVersion != selection.StateVersion
            || completion.PriorSelectionHash
                != CampaignNativeReserveReleaseEventV27.Hash(
                    selection.SerializeCanonical())
            || completion.CycleOrdinal != selection.CycleOrdinal
            || completion.WorldHash != CampaignNativeCombatPaidWorldV22.Hash(world)
            || !CampaignWorldV6Validator.IsValid(world, artifact,
                context.Scenario))
            throw new InvalidOperationException(
                "Later-cycle Reserve Release lacks accepted Combat authority.");
        var position = Cna1979LandSequenceV4.CreateTurn(
            initial.CurrentPosition.SequenceContext.GameTurn).Single(value =>
                value.PositionId == completion.NextPositionId);
        if (position.SegmentId != LandSegmentIds.ReserveRelease
            || position.ActorRole != LandActorRole.FirstActingSide
            || position.OperationStage
                != initial.CurrentPosition.SequenceContext.OperationStage)
            throw new InvalidOperationException(
                "Later-cycle finish must reach same-slot Reserve Release.");
        _ = CampaignReserveReleaseRules.Pending(world, context,
            selection.Side, selection.CycleOrdinal);
        return new(completion.CampaignId, completion.StateVersion,
            CampaignNativeReserveReleaseEventV27.Hash(
                completion.SerializeCanonical()), selection.Side,
            selection.CycleOrdinal, position, world, [], [], false);
    }

    internal static CampaignNativeReserveReleaseStateV27 InitializeAfterRepeatAssault(
        CampaignNativeRepeatCombatDecisionV31 selection,
        CampaignNativeCombatSuccessorV26 successor,
        CampaignNativeCombatSuccessorV26.Projection projection,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(successor);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(context);
        if (selection.Choice != "select-contact-assault"
            || successor.CampaignId != selection.CampaignId
            || successor.StateVersion != projection.StateVersion
            || successor.NextPositionId != projection.NextPosition.PositionId
            || successor.StateHash != CampaignNativeReserveReleaseEventV27.Hash(
                projection.SerializeCanonical())
            || successor.CustodyStateHash != CampaignNativeReserveReleaseEventV27.Hash(
                CampaignNativeCombatCustodyProjectionV25.SerializeStateCanonical(
                    projection.Custody))
            || projection.NextPosition.SegmentId != LandSegmentIds.ReserveRelease
            || projection.NextPosition.ActorRole != LandActorRole.FirstActingSide)
            throw new InvalidOperationException(
                "Repeated assault successor lacks accepted Reserve Release authority.");
        _ = CampaignReserveReleaseRules.Pending(projection.World, context,
            selection.Side, selection.CycleOrdinal);
        return new(successor.CampaignId, successor.StateVersion,
            CampaignNativeReserveReleaseEventV27.Hash(successor.SerializeCanonical()),
            selection.Side, selection.CycleOrdinal, projection.NextPosition,
            projection.World, [], [], false);
    }

    internal IReadOnlyList<CampaignReserveReleaseMember> Pending(
        CampaignContentContext context) => Completed
            ? [] : CampaignReserveReleaseRules.Pending(World, context,
                ActingSide, CycleOrdinal).Where(member =>
                    !ResolvedElementIds.Contains(member.ElementId, StringComparer.Ordinal))
                .ToArray();

    internal (CampaignNativeReserveReleaseStateV27 State,
        CampaignNativeReserveReleaseEventV27 Event) Choose(
        CampaignContentContext context, string elementId,
        CampaignReserveReleaseChoice choice)
    {
        if (Completed) throw new InvalidOperationException("Reserve Release is complete.");
        var disposition = CampaignReserveReleaseRules.Apply(World, context,
            ActingSide, CycleOrdinal, elementId, choice,
            ResolvedElementIds.ToHashSet(StringComparer.Ordinal));
        var movement = disposition.NextMovement is null
            ? NextMovement : [.. NextMovement, disposition.NextMovement];
        var emitted = new CampaignNativeReserveReleaseEventV27(
            CampaignNativeReserveReleaseEventV27.CurrentContractVersion,
            CampaignId, StateVersion, HeadEventHash, "member-choice",
            elementId, choice,
            CampaignNativeCombatPaidWorldV22.Hash(disposition.World),
            Position.PositionId);
        var next = this with
        {
            StateVersion = emitted.StateVersion,
            HeadEventHash = CampaignNativeReserveReleaseEventV27.Hash(
                emitted.SerializeCanonical()),
            World = disposition.World,
            NextMovement = movement,
            ResolvedElementIds = [.. ResolvedElementIds, elementId],
        };
        return (next, emitted);
    }

    internal (CampaignNativeReserveReleaseStateV27 State,
        CampaignNativeReserveReleaseEventV27 Event) Complete(
        CampaignContentContext context)
    {
        if (Completed || Pending(context).Count != 0)
            throw new InvalidOperationException(
                "Reserve Release cannot finish with unresolved members.");
        // Segment completion leaves cycle control at Reserve Release. The
        // owner must choose the source-backed repeat or same-slot finish edge.
        var emitted = new CampaignNativeReserveReleaseEventV27(
            CampaignNativeReserveReleaseEventV27.CurrentContractVersion,
            CampaignId, StateVersion, HeadEventHash, "completed", null, null,
            CampaignNativeCombatPaidWorldV22.Hash(World),
            Position.PositionId);
        var next = this with
        {
            StateVersion = emitted.StateVersion,
            HeadEventHash = CampaignNativeReserveReleaseEventV27.Hash(
                emitted.SerializeCanonical()),
            Completed = true,
        };
        return (next, emitted);
    }

    internal static CampaignNativeReserveReleaseStateV27 Replay(
        CampaignNativeReserveReleaseStateV27 initial,
        CampaignContentContext context,
        IEnumerable<ReadOnlyMemory<byte>> events)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(events);
        var state = initial;
        var count = 0;
        foreach (var bytes in events)
        {
            if (++count > initial.World.Elements.Count + 1)
                throw new InvalidCampaignHistoryException(
                    "Reserve Release exceeds its member and completion bound.");
            var parsed = CampaignNativeReserveReleaseEventV27.Deserialize(bytes);
            if (parsed.CampaignId != state.CampaignId
                || parsed.PriorStateVersion != state.StateVersion
                || parsed.PriorEventHash != state.HeadEventHash)
                throw new InvalidCampaignHistoryException(
                    "Reserve Release event does not follow prior authority.");
            (CampaignNativeReserveReleaseStateV27 State,
                CampaignNativeReserveReleaseEventV27 Event) next;
            try
            {
                next = parsed.Kind == "member-choice"
                    ? state.Choose(context, parsed.ElementId!, parsed.Choice!.Value)
                    : state.Complete(context);
            }
            catch (Exception exception) when (exception is ArgumentException
                or InvalidOperationException or JsonException)
            {
                throw new InvalidCampaignHistoryException(
                    "Reserve Release event is not legal at this authority cut.");
            }
            if (!bytes.Span.SequenceEqual(next.Event.SerializeCanonical()))
                throw new InvalidCampaignHistoryException(
                    "Reserve Release event differs from Core replay.");
            state = next.State;
        }
        return state;
    }
}
