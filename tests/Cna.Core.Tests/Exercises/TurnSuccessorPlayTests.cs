using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Tests.Exercises.Sequencing;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Exercises;

public sealed class TurnSuccessorPlayTests
{
    private static string Hash(string value) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static CampaignActionAudience Audience(LandSide side) =>
        side == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;

    private static CampaignActionSubmission Reserve(ExerciseSession session, CampaignActionAudience audience, long? version = null,
        string? position = null)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new CampaignActionSubmission(1, checkpoint.CampaignId, version ?? checkpoint.StateVersion,
            position ?? checkpoint.PositionId, audience, new CompleteReserveDesignationAction().ActionId);
    }

    private static void AssertCold(ExerciseSession session)
    {
        var result = CampaignExercises.Reconstruct(session);
        Assert.True(result.IsVerified, result.FailureReason.ToString());
        // Independent cut derivation: head event, contact authority and checkpoint all name one version.
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        Assert.Equal(session.CurrentContacts.AuthorityVersion, checkpoint.StateVersion);
        Assert.IsType<TurnSuccessorPending>(session.Pending);
    }

    [Theory]
    [InlineData(1, 2UL, false)]
    [InlineData(1, 3UL, true)]
    [InlineData(12, 12345UL, true)]
    public void FirstSideReserveMovementAndCompletionReplayAtEveryStep(int initialTurn, ulong seed, bool expectMove)
    {
        var opened = SequencingScenarios.DriveTurnBoundary(initialTurn, seed).Samples[^1].Session;
        var declaration = opened.CurrentHistory.OfType<CampaignNativeInitiativeDeclarationV49>().Last();
        var initiative = opened.CurrentHistory.OfType<CampaignNativeTurnInitiativeV56>().Single();
        var audience = Audience(declaration.FirstSide);
        var cursorBefore = ExerciseSession.LatestCursor(opened.CurrentSnapshot.RandomState, opened.CurrentHistory);
        Assert.NotEqual(initiative.Holder, declaration.FirstSide); // role inversion: the driver declares act-last

        var reserved = CampaignExercises.SubmitWithCombat(opened, Reserve(opened, audience));
        Assert.True(reserved.IsAccepted, reserved.RejectionReason.ToString());
        var session = reserved.SuccessorSession!;
        AssertCold(session);
        Assert.Equal(initialTurn + 1, CampaignGameTurn.CurrentGameTurn(session));
        Assert.Equal(opened.CurrentContacts.AuthorityVersion + 1, session.CurrentContacts.AuthorityVersion);

        var side = declaration.FirstSide;
        var moved = false;
        foreach (var element in session.CurrentSnapshot.World.Elements.Select(v => v.ElementId).Order(StringComparer.Ordinal))
        {
            CampaignContactOwnerMovementCut cut;
            try { cut = CampaignExercises.QueryTurnSuccessorMovementCut(session, side, element); }
            catch (Exception error) when (error is InvalidOperationException or ArgumentException) { continue; }
            foreach (var offer in cut.MovementOffers.OrderBy(v => v.OriginLocationId, StringComparer.Ordinal)
                .ThenBy(v => v.DestinationLocationId, StringComparer.Ordinal))
            {
                var attempt = CampaignExercises.SubmitTurnSuccessorMovement(session, audience,
                    CampaignNativeSecondMoveV44.ActionIdFor(offer), element);
                if (!attempt.IsAccepted) continue;
                session = attempt.SuccessorSession!;
                Assert.IsType<CampaignNativeSecondMoveV44>(session.CurrentHistory[^1]);
                AssertCold(session);
                moved = true;
                break;
            }
            if (moved) break;
        }
        // Seed 2 has no non-Reaction legal move for its first side; the completion-only path is then what replays.
        Assert.Equal(expectMove, moved);

        var completed = CampaignExercises.CompleteTurnSuccessorMovement(session, audience, Hash("t2-movement-completion"));
        Assert.True(completed.IsAccepted, completed.RejectionReason.ToString());
        session = completed.SuccessorSession!;
        Assert.IsType<CampaignNativeSecondMovementCompletedV45>(session.CurrentHistory[^1]);
        AssertCold(session);
        Assert.Equal(cursorBefore, ExerciseSession.LatestCursor(session.CurrentSnapshot.RandomState, session.CurrentHistory));
        Assert.Equal(initialTurn + 1, CampaignGameTurn.CurrentGameTurn(session));
        Assert.Equal(LandActorRole.FirstActingSide, Cna1979LandSequenceV4.CreateTurn(initialTurn + 1)
            .Single(p => p.PositionId == CampaignExercises.QueryCheckpoint(session).PositionId).ActorRole);
    }

    [Theory]
    [InlineData(1, 2UL)]
    [InlineData(1, 3UL)]
    public void BothInitiativeHoldersReachTheirOwnFirstSideCut(int initialTurn, ulong seed)
    {
        var opened = SequencingScenarios.DriveTurnBoundary(initialTurn, seed).Samples[^1].Session;
        var declaration = opened.CurrentHistory.OfType<CampaignNativeInitiativeDeclarationV49>().Last();
        var wrong = CampaignExercises.SubmitWithCombat(opened, Reserve(opened, Audience(declaration.SecondSide)));
        Assert.False(wrong.IsAccepted);
        Assert.True(CampaignExercises.SubmitWithCombat(opened, Reserve(opened, Audience(declaration.FirstSide))).IsAccepted);
    }

    [Fact]
    public void WrongStampsAndWrongOwnersAreRejectedAndTamperedHeadsDoNotReplay()
    {
        var opened = SequencingScenarios.DriveTurnBoundary(1, 2UL).Samples[^1].Session;
        var declaration = opened.CurrentHistory.OfType<CampaignNativeInitiativeDeclarationV49>().Last();
        var audience = Audience(declaration.FirstSide);
        var version = opened.CurrentContacts.AuthorityVersion;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            CampaignExercises.SubmitWithCombat(opened, Reserve(opened, audience, version - 1)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.UnexpectedPosition,
            CampaignExercises.SubmitWithCombat(opened, Reserve(opened, audience, position: "land.position.end-of-turn")).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitWithCombat(opened, Reserve(opened, Audience(declaration.SecondSide))).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.CampaignMismatch, CampaignExercises.SubmitWithCombat(opened,
            Reserve(opened, audience) with { CampaignId = "other-campaign" }).RejectionReason);

        var head = Assert.IsType<CampaignNativeSecondReserveV43>(CampaignExercises
            .SubmitWithCombat(opened, Reserve(opened, audience)).SuccessorSession!.CurrentHistory[^1]);
        var accepted = CampaignExercises.SubmitWithCombat(opened, Reserve(opened, audience)).SuccessorSession!;
        foreach (var wrong in new[] { head with { Side = declaration.SecondSide }, head with { ElementId = "not-an-element" } })
        {
            var tampered = SessionTamper.Clone(accepted);
            SessionTamper.Set(tampered, nameof(ExerciseSession.CurrentHistory),
                accepted.CurrentHistory.SetItem(accepted.CurrentHistory.Count - 1, wrong));
            Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, CampaignExercises.Reconstruct(tampered).FailureReason);
        }
        // Completion owned by the wrong side is rejected before any event is built.
        Assert.False(CampaignExercises.CompleteTurnSuccessorMovement(accepted, Audience(declaration.SecondSide), Hash("x")).IsAccepted);
        Assert.False(CampaignExercises.CompleteTurnSuccessorMovement(opened, audience, Hash("x")).IsAccepted);
    }
}
