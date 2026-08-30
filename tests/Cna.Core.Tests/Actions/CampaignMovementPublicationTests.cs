using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;
using Cna.Core.Tests.Observations;

namespace Cna.Core.Tests.Actions;

public sealed class CampaignMovementPublicationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ActingSideReceivesObservationDerivedMovesAndExactlyOneCompletion(
        int reserveCount)
    {
        var evidence = CampaignMovementTestData.ReachMovement(reserveCount);
        var handle = new CampaignAuthorityHandle(evidence.Snapshot, evidence.Context);
        var audience = CampaignReserveActionTestData.ToAudience(evidence.ActingSide);
        var opponent = audience == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth
            : CampaignActionAudience.Axis;
        var projection = CampaignObservationProjector.Project(
            evidence.Snapshot,
            evidence.Context,
            evidence.ActingSide);
        var observation = Assert.IsType<CampaignObservation>(projection.Observation);
        var expected = CampaignMovementActionDerivation.Derive(observation);

        var acting = Query(handle, audience);

        Assert.Equal(expected, acting.Candidates);
        Assert.Single(acting.Candidates.OfType<CompleteMovementSegmentAction>());
        Assert.Equal(2 - reserveCount,
            acting.Candidates.OfType<MoveElementAction>()
                .Select(candidate => candidate.ElementId)
                .Distinct(StringComparer.Ordinal)
                .Count());
        Assert.Empty(Query(handle, opponent).Candidates);
        Assert.Empty(Query(handle, CampaignActionAudience.System).Candidates);
    }

    [Fact]
    public void MoveAndCompletionCandidatesMapToCurrentClosedCommands()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var handle = new CampaignAuthorityHandle(evidence.Snapshot, evidence.Context);
        var audience = CampaignReserveActionTestData.ToAudience(evidence.ActingSide);
        var set = Query(handle, audience);

        foreach (var candidate in set.Candidates.OfType<MoveElementAction>())
        {
            var command = Assert.IsType<MoveElement>(CampaignActionExecution.ToCommand(
                evidence.Snapshot,
                audience,
                candidate));
            Assert.Equal(evidence.Snapshot.StateVersion, command.ExpectedStateVersion);
            Assert.Equal(evidence.Snapshot.SequencePosition.PositionId,
                command.ExpectedPositionId);
            Assert.Equal(evidence.ActingSide, command.ActingSide);
            Assert.Equal(candidate.ActionId, command.CandidateId);
            Assert.Equal(candidate.ElementId, command.ElementId);
            Assert.Equal(candidate.OriginLocationId, command.OriginLocationId);
            Assert.Equal(candidate.DestinationLocationId, command.DestinationLocationId);
        }

        var completion = Assert.IsType<CompleteMovementSegment>(
            CampaignActionExecution.ToCommand(
                evidence.Snapshot,
                audience,
                Assert.Single(set.Candidates.OfType<CompleteMovementSegmentAction>())));
        Assert.Equal(evidence.Snapshot.StateVersion, completion.ExpectedStateVersion);
        Assert.Equal(evidence.Snapshot.SequencePosition.PositionId,
            completion.ExpectedPositionId);
        Assert.Equal(evidence.ActingSide, completion.ActingSide);
    }

    [Fact]
    public void OrdinarySubmissionAcceptsMoveThenCompletionAndRejectsRetainedIds()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var handle = new CampaignAuthorityHandle(evidence.Snapshot, evidence.Context);
        var audience = CampaignReserveActionTestData.ToAudience(evidence.ActingSide);
        var initial = Query(handle, audience);
        var move = initial.Candidates.OfType<MoveElementAction>()
            .OrderBy(candidate => candidate.ActionId, StringComparer.Ordinal)
            .First();
        var retainedCompletion = Assert.Single(
            initial.Candidates.OfType<CompleteMovementSegmentAction>());
        var staleMove = Bind(initial, move);
        var staleCompletion = Bind(initial, retainedCompletion);

        var moved = CampaignLegalActions.Submit(handle, staleMove);

        Assert.True(moved.IsAccepted);
        Assert.Equal(LandSegmentIds.Movement, moved.SuccessorHandle!.Snapshot.SegmentId);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            CampaignLegalActions.Submit(moved.SuccessorHandle, staleMove).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            CampaignLegalActions.Submit(moved.SuccessorHandle, staleCompletion).RejectionReason);
        var reboundMove = staleMove with
        {
            ExpectedStateVersion = moved.SuccessorHandle.Snapshot.StateVersion,
            ExpectedPositionId = moved.SuccessorHandle.Snapshot.SequencePosition.PositionId,
        };
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignLegalActions.Submit(moved.SuccessorHandle, reboundMove).RejectionReason);

        var current = Query(moved.SuccessorHandle, audience);
        var completion = Assert.Single(
            current.Candidates.OfType<CompleteMovementSegmentAction>());
        var completed = CampaignLegalActions.Submit(
            moved.SuccessorHandle,
            Bind(current, completion));

        Assert.True(completed.IsAccepted);
        Assert.Equal(LandSegmentIds.BreakdownDetermination,
            completed.SuccessorHandle!.Snapshot.SegmentId);
        Assert.Equal(completion.ActionId, completed.Receipt!.ActionId);
        Assert.Empty(Query(completed.SuccessorHandle, audience).Candidates);
        var repeatedCompletion = Bind(current, completion) with
        {
            ExpectedStateVersion = completed.SuccessorHandle.Snapshot.StateVersion,
            ExpectedPositionId = completed.SuccessorHandle.Snapshot.SequencePosition.PositionId,
        };
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignLegalActions.Submit(
                completed.SuccessorHandle,
                repeatedCompletion).RejectionReason);
    }

    [Fact]
    public void MalformedForgedAndWrongAudienceMovementSubmissionsFailClosed()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var handle = new CampaignAuthorityHandle(evidence.Snapshot, evidence.Context);
        var audience = CampaignReserveActionTestData.ToAudience(evidence.ActingSide);
        var opponent = audience == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth
            : CampaignActionAudience.Axis;
        var set = Query(handle, audience);
        var candidate = set.Candidates.OfType<MoveElementAction>().First();
        var submission = Bind(set, candidate);
        var before = CampaignSnapshotSerializer.Serialize(evidence.Snapshot);
        CampaignActionSubmission[] invalid =
        [
            submission with { ActionId = "not-a-hash" },
            submission with { ActionId = $"sha256:{new string('0', 64)}" },
            submission with { Audience = opponent },
        ];

        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidSubmission,
            CampaignLegalActions.Submit(handle, invalid[0]).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignLegalActions.Submit(handle, invalid[1]).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignLegalActions.Submit(handle, invalid[2]).RejectionReason);
        Assert.All(invalid, value => Assert.False(
            CampaignLegalActions.Submit(handle, value).IsAccepted));
        Assert.Equal(before, CampaignSnapshotSerializer.Serialize(evidence.Snapshot));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RetainedMoveAndCompletionReadjudicateToExactEventReceiptAndSuccessor(
        bool complete)
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var handle = new CampaignAuthorityHandle(evidence.Snapshot, evidence.Context);
        var audience = CampaignReserveActionTestData.ToAudience(evidence.ActingSide);
        var set = Query(handle, audience);
        var candidate = complete
            ? (CampaignActionCandidate)Assert.Single(
                set.Candidates.OfType<CompleteMovementSegmentAction>())
            : set.Candidates.OfType<MoveElementAction>()
                .OrderBy(value => value.ActionId, StringComparer.Ordinal)
                .First();
        var submission = Bind(set, candidate);

        var first = CampaignActionExecution.Execute(
            evidence.Snapshot,
            evidence.Context,
            submission);
        var second = CampaignActionExecution.Execute(
            evidence.Snapshot,
            evidence.Context,
            submission);

        Assert.True(first.IsAccepted);
        Assert.True(second.IsAccepted);
        Assert.Equal(
            CampaignEventSerializer.Serialize(first.AcceptedEvent!),
            CampaignEventSerializer.Serialize(second.AcceptedEvent!));
        Assert.Equal(
            CampaignActionAcceptanceReceiptSerializer.Serialize(first.Receipt!),
            CampaignActionAcceptanceReceiptSerializer.Serialize(second.Receipt!));
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(first.SuccessorSnapshot!),
            CampaignSnapshotSerializer.Serialize(second.SuccessorSnapshot!));
    }

    [Theory]
    [InlineData(LandSide.Axis)]
    [InlineData(LandSide.Commonwealth)]
    public void ApparentEquivalentMovementAuthoritiesPublishByteIdenticalActionSets(
        LandSide observer)
    {
        var pair = CampaignObservationTestData.CreateApparentEquivalentPair(observer);
        var baseline = CampaignObservationTestData.AdvanceThroughMovement(
            pair.BaselineSnapshot,
            pair.BaselineContext)[^1];
        var changed = CampaignObservationTestData.AdvanceThroughMovement(
            pair.ChangedSnapshot,
            pair.ChangedContext)[^1];
        var audience = CampaignReserveActionTestData.ToAudience(observer);

        var baselineSet = Query(
            new CampaignAuthorityHandle(baseline, pair.BaselineContext),
            audience);
        var changedSet = Query(
            new CampaignAuthorityHandle(changed, pair.ChangedContext),
            audience);

        Assert.Equal(
            CampaignLegalActionSerializer.Serialize(baselineSet),
            CampaignLegalActionSerializer.Serialize(changedSet));
    }

    private static CampaignLegalActionSet Query(
        CampaignAuthorityHandle handle,
        CampaignActionAudience audience)
    {
        var query = CampaignLegalActions.Query(handle, audience);
        Assert.True(query.IsSuccessful);
        return query.ActionSet!;
    }

    private static CampaignActionSubmission Bind(
        CampaignLegalActionSet set,
        CampaignActionCandidate candidate) => new(
        CampaignActionSubmission.CurrentContractVersion,
        set.CampaignId,
        set.StateVersion,
        set.PositionId,
        set.Audience,
        candidate.ActionId);
}
