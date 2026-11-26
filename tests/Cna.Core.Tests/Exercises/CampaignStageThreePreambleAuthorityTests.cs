using System.Security.Cryptography;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Exercises;

public sealed class CampaignStageThreePreambleAuthorityTests
{
    [Fact]
    public void DeclarationSourceRequiresExactCheckpointAndReturnsLatestPass()
    {
        var declared = Fixture<ThirdStageInitiativeDeclarationPending>();
        var terminal = declared.Pending!.Predecessor!;
        var source = CampaignStageThreePreambleAuthority.QueryStageThreeInitiativeDeclarationSource(terminal);
        Assert.Same(terminal.CurrentHistory[^1], source);
        Assert.Equal(terminal.CurrentContacts.AuthorityVersion, source.StateVersion);
        var early = terminal;
        for (var index = 0; index < 3; index++)
        {
            early = early.Pending!.Predecessor!;
            Assert.Throws<InvalidOperationException>(() =>
                CampaignStageThreePreambleAuthority.QueryStageThreeInitiativeDeclarationSource(early));
        }
        Assert.Throws<InvalidOperationException>(() =>
            CampaignStageThreePreambleAuthority.QueryStageThreeInitiativeDeclarationSource(declared));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignStageThreePreambleAuthority.QueryStageThreeInitiativeDeclarationSource(
                Fixture<InitiativeDeclarationPending>().Pending!.Predecessor!));
    }

    [Fact]
    public void ForwardPreambleMatchesHandBuiltEventsAndReachesFirstOwnerDecision()
    {
        // The independent driver constructs the Stage 3 preamble using native Create/With methods.
        var declared = Fixture<ThirdStageInitiativeDeclarationPending>();
        var session = declared.Pending!.Predecessor!;
        var holder = session.CurrentSnapshot.InitiativeHolder!.Value;
        var audience = Audience(holder);
        var declarationId = CampaignStageEntryCursorContractTests.Hash("stage-three-declaration");
        var steps = new (Func<ExerciseSession, ExerciseStepResult> Complete, byte[] Expected,
            CampaignActionAudience Audience, string? ActionId)[]
        {
            (current => CampaignStageThreePreambleAuthority.SubmitStageThreeInitiativeDeclaration(
                current, audience, declarationId, actFirst: false),
                declared.CurrentThirdStageInitiativeDeclaration!.SerializeCanonical(), audience, declarationId),
            (CampaignExercises.CompleteStageEntryWeather,
                Fixture<ThirdStageWeatherPending>().CurrentThirdStageWeather!.SerializeCanonical(), CampaignActionAudience.System, null),
            (CampaignStageThreePreambleAuthority.CompleteStageThreeOrganization,
                Fixture<ThirdStageNoObligationOrganizationPending>().CurrentThirdStageNoObligationOrganization!.SerializeCanonical(), CampaignActionAudience.System, null),
            (CampaignStageThreePreambleAuthority.CompleteStageThreeNavalConvoyArrival,
                Fixture<ThirdStageNoObligationNavalConvoyArrivalPending>().CurrentThirdStageNoObligationNavalConvoyArrival!.SerializeCanonical(), CampaignActionAudience.System, null),
            (CampaignStageThreePreambleAuthority.CompleteStageThreeFleetAssignment,
                Fixture<ThirdStageNoObligationFleetAssignmentPending>().CurrentThirdStageNoObligationFleetAssignment!.SerializeCanonical(), CampaignActionAudience.System, null),
            (CampaignStageThreePreambleAuthority.CompleteStageThreeFleetRepair,
                Fixture<ThirdStageNoObligationFleetRepairPending>().CurrentThirdStageNoObligationFleetRepair!.SerializeCanonical(), CampaignActionAudience.System, null),
        };

        Assert.Equal("land.position.operation-3.initiative-declaration", CampaignExercises.QueryCheckpoint(session).PositionId);
        for (var index = 0; index < steps.Length; index++)
        {
            var (complete, expected, expectedAudience, actionId) = steps[index];
            for (var other = 0; other < steps.Length; other++)
                if (other != index)
                    AssertRejected(steps[other].Complete(session));

            var before = CampaignExercises.QueryCheckpoint(session);
            var accepted = complete(session);
            Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
            var evidence = accepted.Evidence!;
            Assert.Equal(expected, Assert.Single(evidence.EventRecords));
            Assert.Equal(expected, evidence.SnapshotCheckpoint);
            var predecessor = session;
            session = accepted.SuccessorSession!;
            Assert.Same(predecessor, session.Pending!.Predecessor);
            Assert.Equal(predecessor.CurrentHistory.Count + 1, session.CurrentHistory.Count);
            Assert.Equal(expected, CampaignCurrentEventSerializer.Serialize(session.CurrentHistory[^1]));
            var after = CampaignExercises.QueryCheckpoint(session);
            Assert.Equal(before.CampaignId, evidence.Receipt.CampaignId);
            Assert.Equal(before.StateVersion, evidence.Receipt.PriorStateVersion);
            Assert.Equal(before.StateVersion + 1, after.StateVersion);
            Assert.Equal(after.StateVersion, evidence.Receipt.CommittedStateVersion);
            Assert.Equal(after.PositionId, evidence.Receipt.ResultingPositionId);
            Assert.Equal(expectedAudience, evidence.Receipt.Audience);
            Assert.Equal(actionId ?? "sha256:" + Convert.ToHexStringLower(SHA256.HashData(expected)),
                evidence.Receipt.ActionId);
            var reconstruction = CampaignExercises.Reconstruct(session);
            Assert.True(reconstruction.IsVerified, reconstruction.FailureReason.ToString());
            Assert.Equal(expected, reconstruction.ReconstructedSnapshotBytes);
            AssertRejected(complete(session));
        }

        Assert.Equal("land.position.operation-3.first-player.reserve-designation",
            CampaignExercises.QueryCheckpoint(session).PositionId);
        var firstSide = session.CurrentThirdStageNoObligationFleetRepair!.ToOrder().FirstSide;
        Assert.NotEqual(holder, firstSide);
        var reserve = CampaignExercises.SubmitThirdStageReserve(session, Audience(firstSide),
            CampaignStageEntryCursorContractTests.Hash("stage-three-reserve"), elementId: null);
        Assert.True(reserve.IsAccepted, reserve.RejectionReason.ToString());
        Assert.IsType<ThirdStageReservePending>(reserve.SuccessorSession!.Pending);
        Assert.True(CampaignExercises.Reconstruct(reserve.SuccessorSession).IsVerified);
    }

    [Fact]
    public void DeclarationRejectsEarlierPassCutsAndWrongAudienceAndAcceptsActFirst()
    {
        var terminal = Fixture<ThirdStageInitiativeDeclarationPending>().Pending!.Predecessor!;
        var holder = terminal.CurrentSnapshot.InitiativeHolder!.Value;
        var audience = Audience(holder);
        var other = audience == CampaignActionAudience.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var actionId = CampaignStageEntryCursorContractTests.Hash("stage-three-act-first");

        // Each premature cut already has a pass as its latest event: T10 must
        // reject it based on exact position, not merely event type.
        var early = terminal;
        for (var count = 0; count < 3; count++)
        {
            early = early.Pending!.Predecessor!;
            Assert.IsType<CampaignNativePostCombatPassV42>(early.CurrentHistory[^1]);
            Assert.NotEqual(CampaignExercises.QueryCheckpoint(terminal).PositionId,
                CampaignExercises.QueryCheckpoint(early).PositionId);
            AssertRejected(CampaignStageThreePreambleAuthority.SubmitStageThreeInitiativeDeclaration(
                early, audience, actionId, actFirst: true));
        }

        AssertRejected(CampaignStageThreePreambleAuthority.SubmitStageThreeInitiativeDeclaration(
            Fixture<InitiativeDeclarationPending>().Pending!.Predecessor!, audience, actionId, actFirst: true));
        AssertRejected(CampaignStageThreePreambleAuthority.SubmitStageThreeInitiativeDeclaration(
            terminal, other, actionId, actFirst: true));
        AssertRejected(CampaignStageThreePreambleAuthority.SubmitStageThreeInitiativeDeclaration(
            terminal, CampaignActionAudience.System, actionId, actFirst: true));

        var expected = CampaignNativeInitiativeDeclarationV49.Create(
            terminal.CurrentStageEntrySecondAssault!.PostCombatPasses[^1], holder, actFirst: true,
            terminal.CurrentSnapshot.Setup.InitialGameTurn, terminal.CurrentContacts.AuthorityVersion);
        var result = CampaignStageThreePreambleAuthority.SubmitStageThreeInitiativeDeclaration(
            terminal, audience, actionId, actFirst: true);
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        Assert.Equal(expected.SerializeCanonical(), Assert.Single(result.Evidence!.EventRecords));
        Assert.Equal(holder, result.SuccessorSession!.CurrentThirdStageInitiativeDeclaration!.FirstSide);
        Assert.True(CampaignExercises.Reconstruct(result.SuccessorSession).IsVerified);
    }

    [Fact]
    public void SystemCompletionsRejectCorrespondingStageTwoCuts()
    {
        AssertRejected(CampaignStageThreePreambleAuthority.CompleteStageThreeOrganization(Fixture<StageEntryWeatherPending>()));
        AssertRejected(CampaignStageThreePreambleAuthority.CompleteStageThreeNavalConvoyArrival(Fixture<NoObligationOrganizationPending>()));
        AssertRejected(CampaignStageThreePreambleAuthority.CompleteStageThreeFleetAssignment(Fixture<NoObligationNavalConvoyArrivalPending>()));
        AssertRejected(CampaignStageThreePreambleAuthority.CompleteStageThreeFleetRepair(Fixture<NoObligationFleetAssignmentPending>()));
    }

    private static CampaignActionAudience Audience(LandSide side) =>
        side == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;

    private static ExerciseSession Fixture<T>() where T : PendingDecision => PendingFixtures.All[typeof(T)]();

    private static void AssertRejected(ExerciseStepResult result)
    {
        Assert.False(result.IsAccepted);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, result.RejectionReason);
        Assert.Null(result.SuccessorSession);
        Assert.Null(result.Evidence);
    }
}
