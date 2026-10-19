using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Hosting;

public sealed class CampaignAudienceOutcomeSourceTests
{
    [Fact]
    public void TwelveCurrentStepsProveBothSidesWithoutMutatingPredecessor()
    {
        var session = CampaignHostAdapter.Create(Request()).Session!;
        var kinds = new[]
        {
            "initiative-determined", "no-obligation-naval-convoy-schedule-resolved",
            "no-obligation-tactical-shipping-resolved", "initiative-order-declared",
            "weather-determined", "no-obligation-organization-resolved",
            "no-obligation-naval-convoy-arrival-resolved", "no-obligation-fleet-assignment-resolved",
            "no-obligation-fleet-repair-resolved", "reserve-designation-completed",
            "movement-segment-completed", "breakdown-segment-completed",
        };
        string?[] expectedKeys =
        [
            null, null, null, "diary.initiativeOrder", null, null,
            null, null, null, "diary.reserveCompleted", "diary.movementCompleted", null,
        ];
        string? priorCommittedHead = null;
        string? priorSource = null;
        for (var index = 0; index < kinds.Length; index++)
        {
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            var candidate = set.Candidates.FirstOrDefault(value => value is ActFirstAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteMovementSegmentAction)
                ?? set.Candidates[0];
            var command = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion,
                set.PositionId, set.Audience, candidate.ActionId);
            var before = CampaignCurrentSnapshotSerializer.Serialize(session.CurrentSnapshot);
            var historyCount = session.CurrentHistory.Count;
            var accepted = CampaignHostAdapter.Submit(session, command);
            Assert.True(accepted.IsAccepted);
            var source = CampaignHostAdapter.DecideAcceptedOutcomes(session,
                Step(command, accepted.Evidence!));
            Assert.True(source.HasTypedProof);
            AssertAcceptedStepBinding(source, Step(command, accepted.Evidence!),
                command, accepted.Evidence!.Receipt);
            Assert.Equal(kinds[index], source.Axis!.EventKind);
            var eventBytes = Assert.Single(accepted.Evidence!.EventRecords);
            var classification = CampaignAudienceOutcomeSource.ClassifyCanonicalAcceptedEvent(
                eventBytes, candidate.Kind, set.Audience);
            Assert.NotNull(classification);
            Assert.Equal(expectedKeys[index], classification.Value.CopyKey);
            Assert.Null(classification.Value.ElementId);
            Assert.Null(CampaignAudienceOutcomeSource.ClassifyCanonicalAcceptedEvent(
                eventBytes, "unrecognized-action-kind", set.Audience));
            var wrongActor = set.Audience == CampaignActionAudience.Axis
                ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
            Assert.Null(CampaignAudienceOutcomeSource.ClassifyCanonicalAcceptedEvent(
                eventBytes, candidate.Kind, wrongActor));
            Assert.Equal(set.Audience == CampaignActionAudience.Axis
                    ? expectedKeys[index] : null, source.Axis.ProposedCopyKey);
            Assert.Equal(set.Audience == CampaignActionAudience.Commonwealth
                    ? expectedKeys[index] : null, source.Commonwealth!.ProposedCopyKey);
            Assert.Null(source.Axis.OwnElementIndex);
            Assert.Null(source.Commonwealth.OwnElementIndex);
            Assert.Equal(source.Axis.PriorHeadHash, source.Commonwealth!.PriorHeadHash);
            Assert.Equal(source.Axis.SuccessorHeadHash, source.Commonwealth.SuccessorHeadHash);
            Assert.Equal(source.Axis.EventHash, source.Commonwealth.EventHash);
            if (priorCommittedHead is not null)
                Assert.Equal(priorCommittedHead, source.Axis.PriorHeadHash);
            if (priorSource is not null)
                Assert.Equal(priorSource, source.Axis.SourceBindingHash);
            priorCommittedHead = source.Axis.SuccessorHeadHash;
            priorSource = source.Axis.SourceBindingHash;
            Assert.Equal(session.CurrentSnapshot.StateVersion, source.Axis.PriorStateVersion);
            Assert.Equal(accepted.SuccessorSession!.CurrentSnapshot.StateVersion,
                source.Axis.SuccessorStateVersion);
            Assert.Equal(before, CampaignCurrentSnapshotSerializer.Serialize(session.CurrentSnapshot));
            Assert.Equal(historyCount, session.CurrentHistory.Count);
            var text = JsonSerializer.Serialize(source);
            Assert.DoesNotContain(command.ActionId, text, StringComparison.Ordinal);
            Assert.DoesNotContain(command.CampaignId, text, StringComparison.Ordinal);
            if (set.Audience == CampaignActionAudience.System)
            {
                Assert.Equal(CampaignAudienceOutcomeDisposition.Silent, source.Axis.Disposition);
                Assert.Equal(CampaignAudienceOutcomeDisposition.Silent, source.Commonwealth.Disposition);
            }
            else
            {
                var own = set.Audience == CampaignActionAudience.Axis ? source.Axis : source.Commonwealth;
                var other = set.Audience == CampaignActionAudience.Axis ? source.Commonwealth : source.Axis;
                Assert.Equal(CampaignAudienceOutcomeDisposition.Unavailable, own.Disposition);
                Assert.Equal("copy-catalog-unapproved", own.ReasonCode);
                Assert.Equal(CampaignAudienceOutcomeDisposition.Silent, other.Disposition);
            }
            session = accepted.SuccessorSession;
        }
    }

    [Fact]
    public void ChangedCommandReceiptEventOrHeadCannotProduceTypedProof()
    {
        var session = CampaignHostAdapter.Create(Request()).Session!;
        var set = CampaignHostAdapter.QuerySystem(session).ActionSet!;
        var command = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion,
            set.PositionId, set.Audience, Assert.Single(set.Candidates).ActionId);
        var accepted = CampaignHostAdapter.Submit(session, command);
        var step = Step(command, accepted.Evidence!);
        Assert.True(CampaignAudienceOutcomeSource.Decide(session, step).HasTypedProof);
        Assert.False(CampaignAudienceOutcomeSource.Decide(session,
            new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(
                command with { Audience = CampaignActionAudience.Axis }), step.EventRecords,
                step.CanonicalReceipt, step.SnapshotCheckpoint)).HasTypedProof);
        foreach (var changed in new[]
        {
            command with { CampaignId = "other-campaign" },
            command with { ExpectedStateVersion = command.ExpectedStateVersion + 1 },
            command with { ExpectedPositionId = "other-position" },
            command with { ActionId = $"sha256:{new string('0', 64)}" },
        })
            Assert.False(CampaignAudienceOutcomeSource.Decide(session,
                new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(changed),
                    step.EventRecords, step.CanonicalReceipt, step.SnapshotCheckpoint)).HasTypedProof);
        Assert.False(CampaignAudienceOutcomeSource.Decide(session,
            new CampaignHostAcceptedStep(step.CanonicalCommand, step.EventRecords,
                [0], step.SnapshotCheckpoint)).HasTypedProof);
        Assert.False(CampaignAudienceOutcomeSource.Decide(session,
            new CampaignHostAcceptedStep(step.CanonicalCommand, [[0]],
                step.CanonicalReceipt, step.SnapshotCheckpoint)).HasTypedProof);
        Assert.False(CampaignAudienceOutcomeSource.Decide(session,
            new CampaignHostAcceptedStep(step.CanonicalCommand, step.EventRecords,
                step.CanonicalReceipt, [0])).HasTypedProof);
        Assert.Equal("unavailable-source", CampaignAudienceOutcomeSource.Decide(session,
            new CampaignHostAcceptedStep(step.CanonicalCommand, step.EventRecords,
                step.CanonicalReceipt, [0])).UnavailableReason);
        var creation = CampaignHostAdapter.Create(Request()).CreationEventBytes!;
        Assert.False(CampaignAudienceOutcomeSource.Decide(session,
            new CampaignHostAcceptedStep(step.CanonicalCommand, [creation],
                step.CanonicalReceipt, step.SnapshotCheckpoint)).HasTypedProof);
        var wrongEvent = Encoding.UTF8.GetString(step.EventRecords.Single())
            .Replace("\"contractVersion\":2", "\"contractVersion\":1", StringComparison.Ordinal);
        Assert.False(CampaignAudienceOutcomeSource.Decide(session,
            new CampaignHostAcceptedStep(step.CanonicalCommand,
                [Encoding.UTF8.GetBytes(wrongEvent)], step.CanonicalReceipt,
                step.SnapshotCheckpoint)).HasTypedProof);
    }

    [Fact]
    public void OwnEntryCandidateRemainsUnavailableAndNeverCarriesRawElement()
    {
        var session = CampaignHostAdapter.Create(Request()).Session!;
        for (var step = 0; step < 20; step++)
        {
            var available = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            if (available.Candidates.OfType<DesignateReserveAction>().FirstOrDefault() is { } designation)
            {
                var command = new CampaignActionSubmission(1, available.CampaignId,
                    available.StateVersion, available.PositionId, available.Audience,
                    designation.ActionId);
                var accepted = CampaignHostAdapter.Submit(session, command);
                Assert.True(accepted.IsAccepted);
                var pair = CampaignAudienceOutcomeSource.Decide(session, Step(command, accepted.Evidence!));
                Assert.True(pair.HasTypedProof);
                var classified = CampaignAudienceOutcomeSource.ClassifyCanonicalAcceptedEvent(
                    Assert.Single(accepted.Evidence!.EventRecords), designation.Kind,
                    available.Audience);
                Assert.NotNull(classified);
                Assert.Equal("diary.reserveDesignated", classified.Value.CopyKey);
                Assert.Equal(designation.ElementId, classified.Value.ElementId);
                Assert.Null(CampaignAudienceOutcomeSource.ClassifyCanonicalAcceptedEvent(
                    Assert.Single(accepted.Evidence.EventRecords), "complete-reserve-designation",
                    available.Audience));
                var own = available.Audience == CampaignActionAudience.Axis ? pair.Axis! : pair.Commonwealth!;
                Assert.Equal("diary.reserveDesignated", own.ProposedCopyKey);
                Assert.Equal("copy-catalog-unapproved", own.ReasonCode);
                Assert.NotNull(own.OwnElementIndex);
                Assert.DoesNotContain(designation.ElementId, JsonSerializer.Serialize(pair), StringComparison.Ordinal);
                return;
            }
            var next = available.Candidates.FirstOrDefault(value => value is ActFirstAction)
                ?? available.Candidates[0];
            var submission = new CampaignActionSubmission(1, available.CampaignId,
                available.StateVersion, available.PositionId, available.Audience, next.ActionId);
            session = CampaignHostAdapter.Submit(session, submission).SuccessorSession!;
        }
        throw new InvalidOperationException("Expected reserve designation candidate.");
    }

    [Fact]
    public void CurrentBreakdownMoveStopAndSystemResolutionKeepBothCutsBound()
    {
        var session = CampaignHostAdapter.Create(Request()).Session!;
        for (var index = 0; index < 20; index++)
        {
            var candidateSet = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            if (candidateSet.Candidates.OfType<MoveElementAction>().Any()) break;
            var next = candidateSet.Candidates.FirstOrDefault(value => value is ActFirstAction)
                ?? candidateSet.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? candidateSet.Candidates[0];
            session = CampaignHostAdapter.Submit(session, new CampaignActionSubmission(1,
                candidateSet.CampaignId, candidateSet.StateVersion, candidateSet.PositionId,
                candidateSet.Audience, next.ActionId)).SuccessorSession!;
        }
        var movement = Enum.GetValues<CampaignActionAudience>()
            .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
            .First(value => value.Candidates.OfType<MoveElementAction>().Any());
        var move = movement.Candidates.OfType<MoveElementAction>()
            .First(value => value.ElementId.EndsWith("truck", StringComparison.Ordinal));
        var moveCommand = new CampaignActionSubmission(1, movement.CampaignId,
            movement.StateVersion, movement.PositionId, movement.Audience, move.ActionId);
        var moveAccepted = CampaignHostAdapter.Submit(session, moveCommand);
        Assert.True(moveAccepted.IsAccepted);
        var moveStep = Step(moveCommand, moveAccepted.Evidence!);
        var moved = CampaignAudienceOutcomeSource.Decide(session, moveStep);
        Assert.True(moved.HasTypedProof);
        var moveClassification = CampaignAudienceOutcomeSource.ClassifyCanonicalAcceptedEvent(
            Assert.Single(moveStep.EventRecords), move.Kind, movement.Audience);
        Assert.NotNull(moveClassification);
        Assert.Equal("diary.elementMoved", moveClassification.Value.CopyKey);
        Assert.Equal(move.ElementId, moveClassification.Value.ElementId);
        Assert.Null(CampaignAudienceOutcomeSource.ClassifyCanonicalAcceptedEvent(
            Assert.Single(moveStep.EventRecords), "stop-element-movement", movement.Audience));
        var moveJson = Encoding.UTF8.GetString(moveStep.EventRecords.Single());
        var historical = moveJson.Replace("\"contractVersion\":3", "\"contractVersion\":2",
            StringComparison.Ordinal);
        Assert.NotEqual(moveJson, historical);
        Assert.False(CampaignAudienceOutcomeSource.Decide(session,
            new CampaignHostAcceptedStep(moveStep.CanonicalCommand,
                [Encoding.UTF8.GetBytes(historical)], moveStep.CanonicalReceipt,
                moveStep.SnapshotCheckpoint)).HasTypedProof);
        var forgedElement = moveJson.Replace(move.ElementId, "forged-private-element",
            StringComparison.Ordinal);
        Assert.NotEqual(moveJson, forgedElement);
        Assert.False(CampaignAudienceOutcomeSource.Decide(session,
            new CampaignHostAcceptedStep(moveStep.CanonicalCommand,
                [Encoding.UTF8.GetBytes(forgedElement)], moveStep.CanonicalReceipt,
                moveStep.SnapshotCheckpoint)).HasTypedProof);
        var wrongSource = moveJson.Replace(session.CurrentSnapshot.RulesetHash,
            new string('0', session.CurrentSnapshot.RulesetHash.Length), StringComparison.Ordinal);
        Assert.NotEqual(moveJson, wrongSource);
        Assert.False(CampaignAudienceOutcomeSource.Decide(session,
            new CampaignHostAcceptedStep(moveStep.CanonicalCommand,
                [Encoding.UTF8.GetBytes(wrongSource)], moveStep.CanonicalReceipt,
                moveStep.SnapshotCheckpoint)).HasTypedProof);
        session = moveAccepted.SuccessorSession!;
        Assert.Equal("element-moved", moved.Axis!.EventKind);
        Assert.Equal(3, moved.Axis.EventContractVersion);
        var owner = movement.Audience == CampaignActionAudience.Axis ? moved.Axis : moved.Commonwealth!;
        Assert.Equal("diary.elementMoved", owner.ProposedCopyKey);
        Assert.NotNull(owner.OwnElementIndex);
        Assert.DoesNotContain(move.ElementId, JsonSerializer.Serialize(moved), StringComparison.Ordinal);

        var route = CampaignExercises.Query(session, movement.Audience).ActionSet!;
        var stop = Assert.Single(route.Candidates.OfType<StopElementMovementAction>());
        var stopped = DecideAndAdvance(ref session, route, stop, "diary.movementStopped");
        Assert.Equal("element-movement-stopped", stopped.Axis!.EventKind);
        Assert.Equal(1, stopped.Axis.EventContractVersion);
        var system = CampaignHostAdapter.QuerySystem(session).ActionSet!;
        var resolved = DecideAndAdvance(ref session, system,
            Assert.Single(system.Candidates.OfType<ResolveBreakdownStopAction>()), null);
        Assert.Equal("breakdown-stop-resolved", resolved.Axis!.EventKind);
        Assert.Equal(CampaignAudienceOutcomeDisposition.Silent, resolved.Axis.Disposition);
        Assert.Equal(CampaignAudienceOutcomeDisposition.Silent, resolved.Commonwealth!.Disposition);
    }

    [Fact]
    public void ReactionAcceptedEventsRequireExactActorAndReasonPairing()
    {
        var session = CampaignHostAdapter.Create(Request(Cna1979BreakdownSetupCatalog.ReactionSetupId)).Session!;
        for (var index = 0; index < 20; index++)
        {
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            if (set.Candidates.OfType<MoveElementAction>().Any()) break;
            var next = set.Candidates.FirstOrDefault(value => value is ActFirstAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates[0];
            session = CampaignHostAdapter.Submit(session, new CampaignActionSubmission(1,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, next.ActionId))
                .SuccessorSession!;
        }
        var ownerSet = Enum.GetValues<CampaignActionAudience>()
            .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
            .First(value => value.Candidates.OfType<MoveElementAction>().Any());
        var trigger = ownerSet.Candidates.OfType<MoveElementAction>()
            .Select(candidate => (candidate, result: CampaignHostAdapter.Submit(session,
                new CampaignActionSubmission(1, ownerSet.CampaignId, ownerSet.StateVersion,
                    ownerSet.PositionId, ownerSet.Audience, candidate.ActionId))))
            .FirstOrDefault(value => value.result.IsAccepted
                && value.result.SuccessorSession!.CurrentSnapshot.ReactionWindow is not null);
        Assert.NotNull(trigger.candidate);
        var opening = new CampaignActionSubmission(1, ownerSet.CampaignId,
            ownerSet.StateVersion, ownerSet.PositionId, ownerSet.Audience, trigger.candidate.ActionId);
        var opened = CampaignAudienceOutcomeSource.Decide(session, Step(opening, trigger.result.Evidence!));
        Assert.True(opened.HasTypedProof);
        AssertAcceptedStepBinding(opened, Step(opening, trigger.result.Evidence!),
            opening, trigger.result.Evidence!.Receipt);
        session = trigger.result.SuccessorSession!;
        var reactor = ownerSet.Audience == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var reacting = CampaignExercises.Query(session, reactor).ActionSet!;
        Assert.Contains(reacting.Candidates, value => value is DeclineReactionWindowAction);
        var initialDecline = Assert.Single(reacting.Candidates.OfType<DeclineReactionWindowAction>());
        AssertDeclineAndMutationRejection(session, reacting, initialDecline);
        var systemFallback = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
        Assert.Contains(systemFallback.Candidates, value => value is CloseReactionWindowUnavailableAction);
        Assert.Contains(systemFallback.Candidates, value => value is CloseReactionWindowTimeoutAction);
        foreach (var fallback in systemFallback.Candidates.Where(candidate =>
                     candidate is CloseReactionWindowUnavailableAction or CloseReactionWindowTimeoutAction))
        {
            var command = new CampaignActionSubmission(1, systemFallback.CampaignId,
                systemFallback.StateVersion, systemFallback.PositionId,
                CampaignActionAudience.System, fallback.ActionId);
            var accepted = CampaignHostAdapter.Submit(session, command);
            Assert.True(accepted.IsAccepted);
            var proof = CampaignAudienceOutcomeSource.Decide(session, Step(command, accepted.Evidence!));
            Assert.True(proof.HasTypedProof);
            AssertAcceptedStepBinding(proof, Step(command, accepted.Evidence!),
                command, accepted.Evidence!.Receipt);
            var eventBytes = Assert.Single(accepted.Evidence!.EventRecords);
            var fallbackClassification = CampaignAudienceOutcomeSource.ClassifyCanonicalAcceptedEvent(
                eventBytes, fallback.Kind, CampaignActionAudience.System);
            Assert.NotNull(fallbackClassification);
            Assert.Null(fallbackClassification.Value.CopyKey);
            Assert.Null(CampaignAudienceOutcomeSource.ClassifyCanonicalAcceptedEvent(
                eventBytes, fallback is CloseReactionWindowUnavailableAction
                    ? "close-reaction-window-timeout"
                    : "close-reaction-window-scripted-unavailable",
                CampaignActionAudience.System));
            Assert.Equal("reaction-window-closed", proof.Axis!.EventKind);
            Assert.Equal(CampaignAudienceOutcomeDisposition.Silent, proof.Axis.Disposition);
            Assert.Equal(CampaignAudienceOutcomeDisposition.Silent, proof.Commonwealth!.Disposition);
        }
        var move = reacting.Candidates.OfType<MoveReactingElementAction>().FirstOrDefault();
        Assert.NotNull(move);
        var moved = DecideAndAdvance(ref session, reacting, move, "diary.reactionMoved");
        Assert.Equal("reacting-element-moved", moved.Axis!.EventKind);
        Assert.Equal(2, moved.Axis.EventContractVersion);
        reacting = CampaignExercises.Query(session, reactor).ActionSet!;
        var complete = reacting.Candidates.OfType<CompleteReactionParticipantAction>().FirstOrDefault();
        Assert.NotNull(complete);
        var completed = DecideAndAdvance(ref session, reacting, complete,
            "diary.reactionCompleted");
        Assert.Equal("reaction-participant-completed", completed.Axis!.EventKind);
        var resolution = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
        var resolved = DecideAndAdvance(ref session, resolution,
            Assert.Single(resolution.Candidates.OfType<ResolveBreakdownStopAction>()), null);
        Assert.Equal("breakdown-stop-resolved", resolved.Axis!.EventKind);
        Assert.Equal(CampaignAudienceOutcomeDisposition.Silent, resolved.Axis.Disposition);
        Assert.Equal(CampaignAudienceOutcomeDisposition.Silent, resolved.Commonwealth!.Disposition);
        reacting = CampaignExercises.Query(session, reactor).ActionSet!;
        var noEligible = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
        Assert.Empty(reacting.Candidates);
        var close = Assert.IsType<CloseReactionWindowNoEligibleAction>(Assert.Single(noEligible.Candidates));
        var closedProof = DecideAndAdvance(ref session, noEligible, close, null);
        Assert.Equal("reaction-window-closed", closedProof.Axis!.EventKind);
        Assert.Equal(CampaignAudienceOutcomeDisposition.Silent, closedProof.Axis.Disposition);
        Assert.Equal(CampaignAudienceOutcomeDisposition.Silent, closedProof.Commonwealth!.Disposition);
        Assert.Null(closedProof.Axis.ProposedCopyKey);
        Assert.Null(closedProof.Commonwealth.ProposedCopyKey);
    }

    private static void AssertDeclineAndMutationRejection(ExerciseSession session,
        CampaignLegalActionSet reacting, DeclineReactionWindowAction decline)
    {
        var command = new CampaignActionSubmission(1, reacting.CampaignId,
            reacting.StateVersion, reacting.PositionId, reacting.Audience, decline.ActionId);
        var accepted = CampaignHostAdapter.Submit(session, command);
        Assert.True(accepted.IsAccepted);
        var step = Step(command, accepted.Evidence!);
        var closed = CampaignAudienceOutcomeSource.Decide(session, step);
        Assert.True(closed.HasTypedProof);
        AssertAcceptedStepBinding(closed, step, command, accepted.Evidence!.Receipt);
        var declineClassification = CampaignAudienceOutcomeSource.ClassifyCanonicalAcceptedEvent(
            Assert.Single(step.EventRecords), decline.Kind, reacting.Audience);
        Assert.NotNull(declineClassification);
        Assert.Equal("diary.reactionDeclined", declineClassification.Value.CopyKey);
        Assert.Null(declineClassification.Value.ElementId);
        Assert.Equal(reacting.Audience == CampaignActionAudience.Axis
                ? "diary.reactionDeclined" : null, closed.Axis!.ProposedCopyKey);
        Assert.Equal(reacting.Audience == CampaignActionAudience.Commonwealth
                ? "diary.reactionDeclined" : null, closed.Commonwealth!.ProposedCopyKey);
        Assert.Equal("reaction-window-closed", closed.Axis!.EventKind);
        Assert.Equal(CampaignAudienceOutcomeDisposition.Unavailable,
            (reacting.Audience == CampaignActionAudience.Axis
                ? closed.Axis : closed.Commonwealth!).Disposition);
        Assert.Equal(CampaignAudienceOutcomeDisposition.Silent,
            (reacting.Audience == CampaignActionAudience.Axis
                ? closed.Commonwealth! : closed.Axis).Disposition);
        var reasonChanged = Encoding.UTF8.GetString(step.EventRecords.Single())
            .Replace("player-decline", "timeout", StringComparison.Ordinal);
        Assert.NotEqual(Encoding.UTF8.GetString(step.EventRecords.Single()), reasonChanged);
        Assert.False(CampaignAudienceOutcomeSource.Decide(session,
            new CampaignHostAcceptedStep(step.CanonicalCommand,
                [Encoding.UTF8.GetBytes(reasonChanged)], step.CanonicalReceipt,
                step.SnapshotCheckpoint)).HasTypedProof);
        Assert.False(CampaignAudienceOutcomeSource.Decide(session,
            new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(
                command with { Audience = CampaignActionAudience.System }), step.EventRecords,
                step.CanonicalReceipt, step.SnapshotCheckpoint)).HasTypedProof);
    }

    private static CampaignAudienceOutcomePair DecideAndAdvance(ref ExerciseSession session,
        CampaignLegalActionSet set, CampaignActionCandidate candidate, string? expectedCopyKey)
    {
        var command = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion,
            set.PositionId, set.Audience, candidate.ActionId);
        var accepted = CampaignHostAdapter.Submit(session, command);
        Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
        var proof = CampaignAudienceOutcomeSource.Decide(session, Step(command, accepted.Evidence!));
        Assert.True(proof.HasTypedProof);
        AssertAcceptedStepBinding(proof, Step(command, accepted.Evidence!),
            command, accepted.Evidence!.Receipt);
        var classification = CampaignAudienceOutcomeSource.ClassifyCanonicalAcceptedEvent(
            Assert.Single(accepted.Evidence!.EventRecords), candidate.Kind, set.Audience);
        Assert.NotNull(classification);
        Assert.Equal(expectedCopyKey, classification.Value.CopyKey);
        if (candidate is CloseReactionWindowNoEligibleAction)
        {
            var close = Assert.IsType<ReactionWindowClosedV2>(
                CampaignCurrentEventSerializer.Deserialize(
                    Assert.Single(accepted.Evidence.EventRecords)));
            Assert.Equal(CampaignReactionWindowCloseReason.NoEligibleReactor, close.Reason);
            Assert.Null(close.ActingSide);
            Assert.Equal(CampaignActionAudience.System, set.Audience);
            Assert.Null(CampaignAudienceOutcomeSource.ClassifyCanonicalAcceptedEvent(
                Assert.Single(accepted.Evidence.EventRecords), candidate.Kind,
                CampaignActionAudience.Axis));
        }
        var acting = set.Audience == CampaignActionAudience.Axis
            ? proof.Axis! : proof.Commonwealth!;
        var opposing = set.Audience == CampaignActionAudience.Axis
            ? proof.Commonwealth! : proof.Axis!;
        Assert.Equal(CampaignAudienceOutcomeDisposition.Silent, opposing.Disposition);
        Assert.Null(opposing.ProposedCopyKey);
        Assert.Equal(expectedCopyKey is null
                ? CampaignAudienceOutcomeDisposition.Silent
                : CampaignAudienceOutcomeDisposition.Unavailable,
            acting.Disposition);
        if (candidate.Kind == "move-reacting-element")
        {
            var before = CampaignHostAdapter.QueryPrivateSideCut(session, set.Audience);
            var after = CampaignHostAdapter.QueryPrivateSideCut(accepted.SuccessorSession!,
                set.Audience);
            Assert.Equal("reacting", before!.DecisionStateKind);
            Assert.Equal("reacting", after!.DecisionStateKind);
            Assert.Empty(before.OwnElements);
            Assert.Empty(after.OwnElements);
            var suppressed = set.Audience == CampaignActionAudience.Axis
                ? proof.Axis! : proof.Commonwealth!;
            Assert.Equal("diary.reactionMoved", suppressed.ProposedCopyKey);
            Assert.Null(suppressed.OwnElementIndex);
            Assert.Equal("own-element-suppressed", suppressed.ReasonCode);
            Assert.Equal(CampaignAudienceOutcomeDisposition.Unavailable,
                suppressed.Disposition);
            session = accepted.SuccessorSession!;
            return proof;
        }
        Assert.Equal(set.Audience == CampaignActionAudience.Axis ? expectedCopyKey : null,
            proof.Axis!.ProposedCopyKey);
        Assert.Equal(set.Audience == CampaignActionAudience.Commonwealth ? expectedCopyKey : null,
            proof.Commonwealth!.ProposedCopyKey);
        session = accepted.SuccessorSession!;
        return proof;
    }

    private static CampaignHostAcceptedStep Step(CampaignActionSubmission command,
        ExerciseStepEvidence evidence) => new(
        CampaignActionSubmissionSerializer.SerializeCanonical(command), evidence.EventRecords,
        CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt),
        evidence.SnapshotCheckpoint);

    private static void AssertAcceptedStepBinding(CampaignAudienceOutcomePair proof,
        CampaignHostAcceptedStep step, CampaignActionSubmission command,
        CampaignActionAcceptanceReceipt committedReceipt)
    {
        var receipt = CampaignActionAcceptanceReceiptSerializer.DeserializeCanonical(
            step.CanonicalReceipt);
        Assert.Equal(committedReceipt, receipt);
        Assert.Equal(command.CampaignId, receipt.CampaignId);
        Assert.Equal(command.Audience, receipt.Audience);
        Assert.Equal(command.ActionId, receipt.ActionId);
        Assert.Equal(command.ExpectedStateVersion, receipt.PriorStateVersion);
        Assert.Equal(receipt.PriorStateVersion + 1, receipt.CommittedStateVersion);
        foreach (var decision in new[] { proof.Axis!, proof.Commonwealth! })
        {
            Assert.Equal(receipt.PriorStateVersion, decision.PriorStateVersion);
            Assert.Equal(receipt.CommittedStateVersion, decision.SuccessorStateVersion);
            Assert.Equal($"sha256:{Convert.ToHexStringLower(SHA256.HashData(step.SnapshotCheckpoint))}",
                decision.SuccessorHeadHash);
        }
    }

    private static CampaignCreationRequest Request(string? setupId = null)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
                value.SetupId == (setupId ?? Cna1979BreakdownSetupCatalog.TruckSetupId)));
        return new CampaignCreationRequest(1, "outcome-source-test",
            Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.SetupHash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
    }
}
