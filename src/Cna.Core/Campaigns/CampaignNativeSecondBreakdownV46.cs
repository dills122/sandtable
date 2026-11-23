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

    internal static CampaignNativeSecondBreakdownV46 Create(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var completed = session.CurrentSecondMovementCompletion
            ?? throw new InvalidOperationException("Second Movement has not completed.");
        if (session.CurrentSecondBreakdown is not null)
            throw new InvalidOperationException("Second Breakdown has already completed.");
        var source = session.SecondMovementCompletionPredecessor
            ?? throw new InvalidOperationException("Second Movement history is missing.");
        var (_, world) = CampaignExercises.QuerySecondMovementSource(source);
        var context = session.Context;
        var initial = session.CurrentSnapshot;
        var contactVersion = session.CurrentContacts.AuthorityVersion;
        if (context.ArtifactV6 is not { } artifact || completed.CampaignId != initial.CampaignId
            || completed.StateVersion != contactVersion || completed.WorldHash != CampaignNativeCombatPaidWorldV22.Hash(world)
            || !CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario)
            || initial.ReactionWindow is not null || initial.BreakdownFlow is not CampaignBreakdownFlow.Idle)
            throw new InvalidOperationException("Second Breakdown requires settled movement and World authority.");
        var (movementSource, _) = CampaignExercises.QuerySecondMovementSource(source);
        var expectedCompletion = CampaignNativeSecondMovementCompletedV45.Create(movementSource, world, context,
            initial.OperationStageOrders, completed.Side, initial.Setup.InitialGameTurn, source.CurrentContacts.AuthorityVersion);
        if (!completed.SerializeCanonical().AsSpan().SequenceEqual(expectedCompletion.SerializeCanonical()))
            throw new InvalidOperationException("Movement completion differs from its predecessor.");
        // Every admitted second-side move is retained as V44. Walk the chain
        // back to its completed reserve predecessor: missing history, vehicle
        // movement or a reaction prevents this no-breakdown successor.
        var cursor = source;
        var seen = new HashSet<ExerciseSession>(ReferenceEqualityComparer.Instance);
        while (cursor.CurrentHistory.Count != 0 && cursor.CurrentHistory[^1] is CampaignNativeSecondMoveV44 move)
        {
            if (!seen.Add(cursor)) throw new InvalidOperationException("Second Movement history contains a cycle.");
            var definition = artifact.Definition.LegacyDefinition.Elements.Single(value => value.ElementId == move.ElementId);
            if (definition.BreakdownVehicleCohort is not null || move.ReactionWindow is not null)
                throw new InvalidOperationException("Movement requires breakdown or reaction handling.");
            var predecessor = cursor.SecondMovePredecessor
                ?? throw new InvalidOperationException("Second Movement predecessor is missing.");
            var (priorEvent, priorWorld) = CampaignExercises.QuerySecondMovementSource(predecessor);
            var (replayed, replayedWorld) = CampaignNativeSecondMoveV44.Create(priorEvent, priorWorld, context,
                initial.OperationStageOrders, predecessor.CurrentContacts, move.Side, initial.Setup.InitialGameTurn,
                move.ElementId, move.ActionId, initial.RulesetHash);
            if (!move.SerializeCanonical().AsSpan().SequenceEqual(replayed.SerializeCanonical())
                || cursor.CurrentSecondMoveWorld != replayedWorld)
                throw new InvalidOperationException("Second Movement differs from retained authority.");
            cursor = predecessor;
        }
        if (cursor.CurrentHistory.Count == 0 || cursor.CurrentHistory[^1] is not CampaignNativeSecondReserveV43 { Completed: true })
            throw new InvalidOperationException("Second Movement lacks its completed reserve predecessor.");
        var catalog = Cna1979LandSequenceV4.CreateTurn(initial.Setup.InitialGameTurn);
        var movement = catalog.Single(value => value.PositionId == completed.PositionId);
        var breakdown = catalog.Single(value => value.PositionId == completed.NextPositionId);
        var order = initial.OperationStageOrders.Single(value => value.GameTurn == movement.GameTurn
            && value.OperationStage == movement.OperationStage);
        // MTS-A (2026-11-22): generalized, same reasoning as
        // CampaignNativeSecondReserveV43.Create -- `actingRole` is reused below
        // for the Breakdown -> Combat successor check too, since a side-block's
        // acting role is constant across the whole block.
        var actingRole = order.ActingRole(completed.Side);
        if (actingRole is null || movement.ActorRole != actingRole
            || movement.SegmentId != LandSegmentIds.Movement || Cna1979LandSequenceV4.GetNext(movement) != breakdown
            || breakdown.SegmentId != LandSegmentIds.BreakdownDetermination)
            throw new InvalidOperationException("Second Breakdown does not follow its retained movement slot.");
        var definitions = artifact.Definition.LegacyDefinition.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        if (world.Elements.Any(value => definitions[value.ElementId].BreakdownVehicleCohort is not null
            && value.OperationalState.MovementEnded?.PositionId == movement.PositionId))
            throw new InvalidOperationException("Vehicle movement requires breakdown accounting.");
        var next = Cna1979LandSequenceV4.GetNext(breakdown);
        if (next.ActorRole != actingRole || next.SegmentId != LandSegmentIds.Combat
            || next.StepId != LandStepIds.PositionDetermination)
            throw new InvalidOperationException("Second Breakdown requires its combat successor.");
        return new(CurrentContractVersion, completed.CampaignId, completed.StateVersion,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(completed.SerializeCanonical())),
            completed.Side, breakdown.PositionId, next.PositionId, completed.WorldHash);
    }

    // MTS-TASK-005/006 (2026-11-23): duplicate of Create, entered from this
    // stage's own completed Movement Completion (CurrentStageEntryMovementCompletion)
    // instead of the first side's. Create(ExerciseSession) above reads four
    // named session fields directly (CurrentSecondMovementCompletion,
    // SecondMovementCompletionPredecessor, SecondMovePredecessor,
    // CurrentSecondMoveWorld) rather than taking its predecessor by type the
    // way CampaignNativeSecondMoveV44/V45 do, so it cannot be reused as-is
    // for this stage's own slots. This mirrors the CampaignExercises layer's
    // existing per-generation duplication (QuerySecondMovementSource/
    // QueryStageEntryMovementSource) rather than introducing new
    // delegate-based parameterization for a single call site. Every other
    // check, invariant, and the resulting event's contract are byte-for-byte
    // identical to Create -- only which ExerciseSession slots are read
    // differs.
    internal static CampaignNativeSecondBreakdownV46 CreateForStageEntry(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var completed = session.CurrentStageEntryMovementCompletion
            ?? throw new InvalidOperationException("Stage Entry Movement has not completed.");
        if (session.CurrentStageEntryBreakdown is not null)
            throw new InvalidOperationException("Stage Entry Breakdown has already completed.");
        var source = session.StageEntryMovementCompletionPredecessor
            ?? throw new InvalidOperationException("Stage Entry Movement history is missing.");
        var (_, world) = CampaignExercises.QueryStageEntryMovementSource(source);
        var context = session.Context;
        var initial = session.CurrentSnapshot;
        var orders = CampaignExercises.StageEntryOrders(session);
        var contactVersion = session.CurrentContacts.AuthorityVersion;
        if (context.ArtifactV6 is not { } artifact || completed.CampaignId != initial.CampaignId
            || completed.StateVersion != contactVersion || completed.WorldHash != CampaignNativeCombatPaidWorldV22.Hash(world)
            || !CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario)
            || initial.ReactionWindow is not null || initial.BreakdownFlow is not CampaignBreakdownFlow.Idle)
            throw new InvalidOperationException("Stage Entry Breakdown requires settled movement and World authority.");
        var (movementSource, _) = CampaignExercises.QueryStageEntryMovementSource(source);
        var expectedCompletion = CampaignNativeSecondMovementCompletedV45.Create(movementSource, world, context,
            orders, completed.Side, initial.Setup.InitialGameTurn, source.CurrentContacts.AuthorityVersion);
        if (!completed.SerializeCanonical().AsSpan().SequenceEqual(expectedCompletion.SerializeCanonical()))
            throw new InvalidOperationException("Movement completion differs from its predecessor.");
        var cursor = source;
        var seen = new HashSet<ExerciseSession>(ReferenceEqualityComparer.Instance);
        while (cursor.CurrentHistory.Count != 0 && cursor.CurrentHistory[^1] is CampaignNativeSecondMoveV44 move)
        {
            if (!seen.Add(cursor)) throw new InvalidOperationException("Stage Entry Movement history contains a cycle.");
            var definition = artifact.Definition.LegacyDefinition.Elements.Single(value => value.ElementId == move.ElementId);
            if (definition.BreakdownVehicleCohort is not null || move.ReactionWindow is not null)
                throw new InvalidOperationException("Movement requires breakdown or reaction handling.");
            var predecessor = cursor.StageEntryMovePredecessor
                ?? throw new InvalidOperationException("Stage Entry Movement predecessor is missing.");
            var (priorEvent, priorWorld) = CampaignExercises.QueryStageEntryMovementSource(predecessor);
            var (replayed, replayedWorld) = CampaignNativeSecondMoveV44.Create(priorEvent, priorWorld, context,
                orders, predecessor.CurrentContacts, move.Side, initial.Setup.InitialGameTurn,
                move.ElementId, move.ActionId, initial.RulesetHash);
            if (!move.SerializeCanonical().AsSpan().SequenceEqual(replayed.SerializeCanonical())
                || cursor.CurrentStageEntryMoveWorld != replayedWorld)
                throw new InvalidOperationException("Stage Entry Movement differs from retained authority.");
            cursor = predecessor;
        }
        if (cursor.CurrentHistory.Count == 0 || cursor.CurrentHistory[^1] is not CampaignNativeSecondReserveV43 { Completed: true })
            throw new InvalidOperationException("Stage Entry Movement lacks its completed reserve predecessor.");
        var catalog = Cna1979LandSequenceV4.CreateTurn(initial.Setup.InitialGameTurn);
        var movement = catalog.Single(value => value.PositionId == completed.PositionId);
        var breakdown = catalog.Single(value => value.PositionId == completed.NextPositionId);
        var order = orders.Single(value => value.GameTurn == movement.GameTurn
            && value.OperationStage == movement.OperationStage);
        var actingRole = order.ActingRole(completed.Side);
        if (actingRole is null || movement.ActorRole != actingRole
            || movement.SegmentId != LandSegmentIds.Movement || Cna1979LandSequenceV4.GetNext(movement) != breakdown
            || breakdown.SegmentId != LandSegmentIds.BreakdownDetermination)
            throw new InvalidOperationException("Stage Entry Breakdown does not follow its retained movement slot.");
        var definitions = artifact.Definition.LegacyDefinition.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        if (world.Elements.Any(value => definitions[value.ElementId].BreakdownVehicleCohort is not null
            && value.OperationalState.MovementEnded?.PositionId == movement.PositionId))
            throw new InvalidOperationException("Vehicle movement requires breakdown accounting.");
        var next = Cna1979LandSequenceV4.GetNext(breakdown);
        if (next.ActorRole != actingRole || next.SegmentId != LandSegmentIds.Combat
            || next.StepId != LandStepIds.PositionDetermination)
            throw new InvalidOperationException("Stage Entry Breakdown requires its combat successor.");
        return new(CurrentContractVersion, completed.CampaignId, completed.StateVersion,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(completed.SerializeCanonical())),
            completed.Side, breakdown.PositionId, next.PositionId, completed.WorldHash);
    }

    // MTS-TASK-006 (2026-11-23): a third duplicate of Create, entered from
    // this stage's own completed SECOND acting side's Movement Completion
    // (CurrentStageEntrySecondMovementCompletion) instead of either the
    // first side's or this stage's own first acting side's slots.
    // CreateForStageEntry above already established the convention this
    // follows: each occurrence reads its own four named session fields
    // directly (confirmed by reading CreateForStageEntry before writing
    // this), so a third occurrence needs its own entry point, not a change
    // to either existing one. Every check, invariant, and the resulting
    // event's contract are byte-for-byte identical to Create/CreateForStageEntry
    // -- only which ExerciseSession slots are read differs.
    internal static CampaignNativeSecondBreakdownV46 CreateForStageEntrySecondSide(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var completed = session.CurrentStageEntrySecondMovementCompletion
            ?? throw new InvalidOperationException("Stage Entry second-side Movement has not completed.");
        if (session.CurrentStageEntrySecondBreakdown is not null)
            throw new InvalidOperationException("Stage Entry second-side Breakdown has already completed.");
        var source = session.StageEntrySecondMovementCompletionPredecessor
            ?? throw new InvalidOperationException("Stage Entry second-side Movement history is missing.");
        var (_, world) = CampaignExercises.QueryStageEntrySecondMovementSource(source);
        var context = session.Context;
        var initial = session.CurrentSnapshot;
        var orders = CampaignExercises.StageEntryOrders(session);
        var contactVersion = session.CurrentContacts.AuthorityVersion;
        if (context.ArtifactV6 is not { } artifact || completed.CampaignId != initial.CampaignId
            || completed.StateVersion != contactVersion || completed.WorldHash != CampaignNativeCombatPaidWorldV22.Hash(world)
            || !CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario)
            || initial.ReactionWindow is not null || initial.BreakdownFlow is not CampaignBreakdownFlow.Idle)
            throw new InvalidOperationException("Stage Entry second-side Breakdown requires settled movement and World authority.");
        var (movementSource, _) = CampaignExercises.QueryStageEntrySecondMovementSource(source);
        var expectedCompletion = CampaignNativeSecondMovementCompletedV45.Create(movementSource, world, context,
            orders, completed.Side, initial.Setup.InitialGameTurn, source.CurrentContacts.AuthorityVersion);
        if (!completed.SerializeCanonical().AsSpan().SequenceEqual(expectedCompletion.SerializeCanonical()))
            throw new InvalidOperationException("Movement completion differs from its predecessor.");
        var cursor = source;
        var seen = new HashSet<ExerciseSession>(ReferenceEqualityComparer.Instance);
        while (cursor.CurrentHistory.Count != 0 && cursor.CurrentHistory[^1] is CampaignNativeSecondMoveV44 move)
        {
            if (!seen.Add(cursor)) throw new InvalidOperationException("Stage Entry second-side Movement history contains a cycle.");
            var definition = artifact.Definition.LegacyDefinition.Elements.Single(value => value.ElementId == move.ElementId);
            if (definition.BreakdownVehicleCohort is not null || move.ReactionWindow is not null)
                throw new InvalidOperationException("Movement requires breakdown or reaction handling.");
            var predecessor = cursor.StageEntrySecondMovePredecessor
                ?? throw new InvalidOperationException("Stage Entry second-side Movement predecessor is missing.");
            var (priorEvent, priorWorld) = CampaignExercises.QueryStageEntrySecondMovementSource(predecessor);
            var (replayed, replayedWorld) = CampaignNativeSecondMoveV44.Create(priorEvent, priorWorld, context,
                orders, predecessor.CurrentContacts, move.Side, initial.Setup.InitialGameTurn,
                move.ElementId, move.ActionId, initial.RulesetHash);
            if (!move.SerializeCanonical().AsSpan().SequenceEqual(replayed.SerializeCanonical())
                || cursor.CurrentStageEntrySecondMoveWorld != replayedWorld)
                throw new InvalidOperationException("Stage Entry second-side Movement differs from retained authority.");
            cursor = predecessor;
        }
        if (cursor.CurrentHistory.Count == 0 || cursor.CurrentHistory[^1] is not CampaignNativeSecondReserveV43 { Completed: true })
            throw new InvalidOperationException("Stage Entry second-side Movement lacks its completed reserve predecessor.");
        var catalog = Cna1979LandSequenceV4.CreateTurn(initial.Setup.InitialGameTurn);
        var movement = catalog.Single(value => value.PositionId == completed.PositionId);
        var breakdown = catalog.Single(value => value.PositionId == completed.NextPositionId);
        var order = orders.Single(value => value.GameTurn == movement.GameTurn
            && value.OperationStage == movement.OperationStage);
        var actingRole = order.ActingRole(completed.Side);
        if (actingRole is null || movement.ActorRole != actingRole
            || movement.SegmentId != LandSegmentIds.Movement || Cna1979LandSequenceV4.GetNext(movement) != breakdown
            || breakdown.SegmentId != LandSegmentIds.BreakdownDetermination)
            throw new InvalidOperationException("Stage Entry second-side Breakdown does not follow its retained movement slot.");
        var definitions = artifact.Definition.LegacyDefinition.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        if (world.Elements.Any(value => definitions[value.ElementId].BreakdownVehicleCohort is not null
            && value.OperationalState.MovementEnded?.PositionId == movement.PositionId))
            throw new InvalidOperationException("Vehicle movement requires breakdown accounting.");
        var next = Cna1979LandSequenceV4.GetNext(breakdown);
        if (next.ActorRole != actingRole || next.SegmentId != LandSegmentIds.Combat
            || next.StepId != LandStepIds.PositionDetermination)
            throw new InvalidOperationException("Stage Entry second-side Breakdown requires its combat successor.");
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
