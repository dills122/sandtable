using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// This successor is limited to a fully settled, no-vehicle second movement.
// It does not implement vehicle breakdown or waive a pending reaction.
internal sealed record CampaignNativeSecondBreakdownV46(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    LandSide Side, string PositionId, string NextPositionId, string WorldHash)
{
    internal const int CurrentContractVersion = 46;
    internal long StateVersion => checked(PriorStateVersion + 1);

    // Slot: "which ExerciseSession fields does this entry point read, and
    // what is its error-message prefix." Create/CreateForStageEntry/
    // CreateForStageEntrySecondSide were byte-for-byte identical in control
    // flow, differing only in which four named ExerciseSession fields they
    // read, whether orders comes from initial.OperationStageOrders directly
    // or CampaignExercises.StageEntryOrders(session), and an error-message
    // prefix. This collapses all three into one CreateCore body (MTS
    // maintainability Slice 1, 2026-11-24) -- a pure, mechanical,
    // behavior-preserving extraction: every check, invariant and the
    // resulting event's SerializeCanonical() output are byte-identical to
    // before. Only the internal control flow is parameterized.
    private readonly record struct Slot(
        string Context,
        Func<ExerciseSession, CampaignNativeSecondMovementCompletedV45?> Completed,
        Func<ExerciseSession, bool> AlreadyCompleted,
        Func<ExerciseSession, ExerciseSession?> Source,
        Func<ExerciseSession, IReadOnlyList<CampaignOperationStageOrder>> Orders,
        Func<ExerciseSession, (object Predecessor, CampaignWorldSnapshotV6 World)> QuerySource,
        Func<ExerciseSession, ExerciseSession?> MovePredecessor,
        Func<ExerciseSession, CampaignWorldSnapshotV6?> MoveWorld);

    private static readonly Slot SecondSlot = new(
        "Second ",
        session => session.CurrentSecondMovementCompletion,
        session => session.CurrentSecondBreakdown is not null,
        session => session.SecondMovementCompletionPredecessor,
        session => session.CurrentSnapshot.OperationStageOrders,
        CampaignExercises.QuerySecondMovementSource,
        cursor => cursor.SecondMovePredecessor,
        cursor => cursor.CurrentSecondMoveWorld);

    private static readonly Slot StageEntrySlot = new(
        "Stage Entry ",
        session => session.CurrentStageEntryMovementCompletion,
        session => session.CurrentStageEntryBreakdown is not null,
        session => session.StageEntryMovementCompletionPredecessor,
        CampaignExercises.StageEntryOrders,
        CampaignExercises.QueryStageEntryMovementSource,
        cursor => cursor.StageEntryMovePredecessor,
        cursor => cursor.CurrentStageEntryMoveWorld);

    private static readonly Slot StageEntrySecondSlot = new(
        "Stage Entry second-side ",
        session => session.CurrentStageEntrySecondMovementCompletion,
        session => session.CurrentStageEntrySecondBreakdown is not null,
        session => session.StageEntrySecondMovementCompletionPredecessor,
        CampaignExercises.StageEntryOrders,
        CampaignExercises.QueryStageEntrySecondMovementSource,
        cursor => cursor.StageEntrySecondMovePredecessor,
        cursor => cursor.CurrentStageEntrySecondMoveWorld);

    private static readonly Slot ThirdStageSlot = new(
        "Stage 3 ",
        session => session.CurrentThirdStageMovementCompletion,
        session => session.CurrentThirdStageBreakdown is not null,
        session => session.ThirdStageMovementCompletionPredecessor,
        CampaignExercises.ThirdStageOrders,
        CampaignExercises.QueryThirdStageMovementSource,
        cursor => cursor.ThirdStageMovePredecessor,
        cursor => cursor.CurrentThirdStageMoveWorld);

    private static readonly Slot ThirdStageSecondSlot = new(
        "Stage 3 second-side ",
        session => session.CurrentThirdStageSecondMovementCompletion,
        session => session.CurrentThirdStageSecondBreakdown is not null,
        session => session.ThirdStageSecondMovementCompletionPredecessor,
        CampaignExercises.ThirdStageOrders,
        CampaignExercises.QueryThirdStageSecondMovementSource,
        cursor => cursor.ThirdStageSecondMovePredecessor,
        cursor => cursor.CurrentThirdStageSecondMoveWorld);

    internal static CampaignNativeSecondBreakdownV46 Create(ExerciseSession session) => CreateCore(session, SecondSlot);

    // MTS-TASK-005/006 (2026-11-23): entered from this stage's own completed
    // Movement Completion (CurrentStageEntryMovementCompletion) instead of
    // the first side's.
    internal static CampaignNativeSecondBreakdownV46 CreateForStageEntry(ExerciseSession session) => CreateCore(session, StageEntrySlot);

    // S3-3 (2026-11-24): entered from Operation Stage 3's own first acting
    // side's completed Movement Completion (CurrentThirdStageMovementCompletion).
    internal static CampaignNativeSecondBreakdownV46 CreateForThirdStage(ExerciseSession session) => CreateCore(session, ThirdStageSlot);

    // S3-8 (2026-11-25): entered from Operation Stage 3's own second acting
    // side's completed Movement Completion (CurrentThirdStageSecondMovementCompletion).
    internal static CampaignNativeSecondBreakdownV46 CreateForThirdStageSecondSide(ExerciseSession session) => CreateCore(session, ThirdStageSecondSlot);

    // MTS-TASK-006 (2026-11-23): entered from this stage's own completed
    // SECOND acting side's Movement Completion
    // (CurrentStageEntrySecondMovementCompletion) instead of either the
    // first side's or this stage's own first acting side's slots.
    internal static CampaignNativeSecondBreakdownV46 CreateForStageEntrySecondSide(ExerciseSession session) => CreateCore(session, StageEntrySecondSlot);

    private static CampaignNativeSecondBreakdownV46 CreateCore(ExerciseSession session, Slot slot)
    {
        ArgumentNullException.ThrowIfNull(session);
        var completed = slot.Completed(session)
            ?? throw new InvalidOperationException($"{slot.Context}Movement has not completed.");
        if (slot.AlreadyCompleted(session))
            throw new InvalidOperationException($"{slot.Context}Breakdown has already completed.");
        var source = slot.Source(session)
            ?? throw new InvalidOperationException($"{slot.Context}Movement history is missing.");
        var (_, world) = slot.QuerySource(source);
        var context = session.Context;
        var initial = session.CurrentSnapshot;
        var orders = slot.Orders(session);
        var contactVersion = session.CurrentContacts.AuthorityVersion;
        if (context.ArtifactV6 is not { } artifact || completed.CampaignId != initial.CampaignId
            || completed.StateVersion != contactVersion || completed.WorldHash != CampaignNativeCombatPaidWorldV22.Hash(world)
            || !CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario)
            || initial.ReactionWindow is not null || initial.BreakdownFlow is not CampaignBreakdownFlow.Idle)
            throw new InvalidOperationException($"{slot.Context}Breakdown requires settled movement and World authority.");
        var (movementSource, _) = slot.QuerySource(source);
        var expectedCompletion = CampaignNativeSecondMovementCompletedV45.Create(movementSource, world, context,
            orders, completed.Side, CampaignGameTurn.CurrentGameTurn(source), source.CurrentContacts.AuthorityVersion);
        if (!completed.SerializeCanonical().AsSpan().SequenceEqual(expectedCompletion.SerializeCanonical()))
            throw new InvalidOperationException("Movement completion differs from its predecessor.");
        // Every admitted second-side move is retained as V44. Walk the chain
        // back to its completed reserve predecessor: missing history, vehicle
        // movement or a reaction prevents this no-breakdown successor.
        var cursor = source;
        var seen = new HashSet<ExerciseSession>(ReferenceEqualityComparer.Instance);
        while (cursor.CurrentHistory.Count != 0 && cursor.CurrentHistory[^1] is CampaignNativeSecondMoveV44 move)
        {
            if (!seen.Add(cursor)) throw new InvalidOperationException($"{slot.Context}Movement history contains a cycle.");
            var definition = artifact.Definition.LegacyDefinition.Elements.Single(value => value.ElementId == move.ElementId);
            if (definition.BreakdownVehicleCohort is not null || move.ReactionWindow is not null)
                throw new InvalidOperationException("Movement requires breakdown or reaction handling.");
            var predecessor = slot.MovePredecessor(cursor)
                ?? throw new InvalidOperationException($"{slot.Context}Movement predecessor is missing.");
            var (priorEvent, priorWorld) = slot.QuerySource(predecessor);
            var (replayed, replayedWorld) = CampaignNativeSecondMoveV44.Create(priorEvent, priorWorld, context,
                orders, predecessor.CurrentContacts, move.Side, CampaignGameTurn.CurrentGameTurn(predecessor),
                move.ElementId, move.ActionId, initial.RulesetHash);
            if (!move.SerializeCanonical().AsSpan().SequenceEqual(replayed.SerializeCanonical())
                || slot.MoveWorld(cursor) != replayedWorld)
                throw new InvalidOperationException($"{slot.Context}Movement differs from retained authority.");
            cursor = predecessor;
        }
        if (cursor.CurrentHistory.Count == 0 || cursor.CurrentHistory[^1] is not CampaignNativeSecondReserveV43 { Completed: true })
            throw new InvalidOperationException($"{slot.Context}Movement lacks its completed reserve predecessor.");
        var catalog = Cna1979LandSequenceV4.CreateTurn(CampaignGameTurn.CurrentGameTurn(session));
        var movement = catalog.Single(value => value.PositionId == completed.PositionId);
        var breakdown = catalog.Single(value => value.PositionId == completed.NextPositionId);
        var order = orders.Single(value => value.GameTurn == movement.GameTurn
            && value.OperationStage == movement.OperationStage);
        // MTS-A (2026-11-22): generalized, same reasoning as
        // CampaignNativeSecondReserveV43.Create -- `actingRole` is reused below
        // for the Breakdown -> Combat successor check too, since a side-block's
        // acting role is constant across the whole block.
        var actingRole = order.ActingRole(completed.Side);
        if (actingRole is null || movement.ActorRole != actingRole
            || movement.SegmentId != LandSegmentIds.Movement || Cna1979LandSequenceV4.GetNext(movement) != breakdown
            || breakdown.SegmentId != LandSegmentIds.BreakdownDetermination)
            throw new InvalidOperationException($"{slot.Context}Breakdown does not follow its retained movement slot.");
        var definitions = artifact.Definition.LegacyDefinition.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        if (world.Elements.Any(value => definitions[value.ElementId].BreakdownVehicleCohort is not null
            && value.OperationalState.MovementEnded?.PositionId == movement.PositionId))
            throw new InvalidOperationException("Vehicle movement requires breakdown accounting.");
        var next = Cna1979LandSequenceV4.GetNext(breakdown);
        if (next.ActorRole != actingRole || next.SegmentId != LandSegmentIds.Combat
            || next.StepId != LandStepIds.PositionDetermination)
            throw new InvalidOperationException($"{slot.Context}Breakdown requires its combat successor.");
        return new(CurrentContractVersion, completed.CampaignId, completed.StateVersion,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(completed.SerializeCanonical())),
            completed.Side, breakdown.PositionId, next.PositionId, completed.WorldHash);
    }

    internal byte[] SerializeCanonical()
    {
        if (ContractVersion != CurrentContractVersion || PriorStateVersion is < 1 or long.MaxValue
            || !Enum.IsDefined(Side) || !IsHash(PriorEventHash) || !IsHash(WorldHash))
            throw new JsonException("Invalid second Breakdown completion.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "second-breakdown-completed");
            writer.WriteString("campaignId", ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId)));
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("priorEventHash", PriorEventHash);
            writer.WriteString("side", CampaignSnapshotSerializer.FormatSide(Side));
            writer.WriteString("positionId", ContentContractGuards.RequireStableId(PositionId, nameof(PositionId)));
            writer.WriteString("nextPositionId", ContentContractGuards.RequireStableId(NextPositionId, nameof(NextPositionId)));
            writer.WriteString("worldHash", WorldHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeSecondBreakdownV46 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 8192) throw new JsonException("Invalid event size.");
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId",
                "priorStateVersion", "priorEventHash", "side", "positionId", "nextPositionId", "worldHash");
            if (root.GetProperty("eventType").GetString() != "second-breakdown-completed") throw new JsonException("Invalid event type.");
            var side = root.GetProperty("side").GetString() switch
            {
                "axis" => LandSide.Axis,
                "commonwealth" => LandSide.Commonwealth,
                _ => throw new JsonException("Invalid side."),
            };
            var parsed = new CampaignNativeSecondBreakdownV46(root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!, root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("priorEventHash").GetString()!, side, root.GetProperty("positionId").GetString()!,
                root.GetProperty("nextPositionId").GetString()!, root.GetProperty("worldHash").GetString()!);
            if (!bytes.Span.SequenceEqual(parsed.SerializeCanonical())) throw new JsonException("Noncanonical event.");
            return parsed;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or NullReferenceException)
        { throw new JsonException("Invalid second Breakdown event.", error); }
    }

    private static bool IsHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToString().All(digit => digit is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
