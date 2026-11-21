using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// #394: second-side Combat continuation. Drives the exact acceptance path the
// issue names -- exercise owner opening, legal finish/selection, atomic
// persistence, cold replay and retry through the normal durable Host -- for
// the second-acting side's first Combat decision after their own Breakdown
// (V46) completes. Before gaps 1-4 and extras 1-2 landed, opening Combat
// Selection for the second side threw "Combat selection has already advanced
// this authority cut." (PR #400's ATTEMPT1/ATTEMPT2) even though no
// second-side decision had ever been made.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static (CampaignCreationRequest Request, CampaignSeatCredential Axis,
        CampaignSeatCredential Commonwealth) AdvanceToSecondCombatSelection(
            CampaignDurableAuthorityJournal journal)
    {
        var request = Request();
        AdvanceToContactCombat(journal, request, Timing(request));
        var axis = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var opening = journal.QueryCombatOwnerOpening(axis, "axis-infantry");
        var finishRef = opening.PublicActionRefs[^1];
        var finished = journal.SubmitOwnerCombatSelection(axis, "axis-infantry",
            opening.Revision, finishRef);
        if (!finished.IsAccepted)
            throw new InvalidOperationException("First-side voluntary finish was not accepted.");
        var control = journal.QueryCycleControlOwnerOpening(axis);
        if (!journal.SubmitOwnerCycleControl(axis, control.Revision,
                control.PublicActionRefs[^1]).IsAccepted)
            throw new InvalidOperationException("Cycle control was not accepted.");
        var convoy = journal.QueryTruckConvoyPassOwnerOpening(axis);
        if (!journal.SubmitOwnerTruckConvoyPass(axis, convoy.Revision,
                convoy.PublicActionRefs[0]).IsAccepted)
            throw new InvalidOperationException("Truck convoy pass was not accepted.");
        for (var index = 0; index < 4; index++)
        {
            var phase = journal.QueryPostCombatPassOwnerOpening(axis);
            if (!journal.SubmitOwnerPostCombatPass(axis, phase.Revision,
                    Assert.Single(phase.PublicActionRefs)).IsAccepted)
                throw new InvalidOperationException("Post-combat pass was not accepted.");
        }
        var commonwealth = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;
        // Finish Second Reserve designation immediately with nobody
        // designated -- the simplest closed path to a completed Reserve
        // stage, with no outstanding Movement to resolve before Breakdown.
        var secondReserve = journal.QuerySecondReserveOwnerOpening(commonwealth);
        if (!journal.SubmitOwnerSecondReserve(commonwealth, secondReserve.Revision,
                secondReserve.PublicActionRefs[^1]).IsAccepted)
            throw new InvalidOperationException("Second Reserve completion was not accepted.");
        var movementCompletion = journal.QuerySecondMovementCompletionOwnerOpening(commonwealth);
        var completed = journal.SubmitOwnerSecondMovementCompletion(commonwealth,
            movementCompletion.Revision, Assert.Single(movementCompletion.PublicActionRefs));
        if (!completed.IsAccepted)
            throw new InvalidOperationException("Second Movement completion was not accepted.");
        return (request, axis, commonwealth);
    }

    // CampaignNativeCombatDecisionStateV12 is internal to Cna.Core and not
    // visible from this test project (unlike Cna.Core.Tests) -- read its
    // Decision.Choice purely through reflection, the same way this file
    // already reaches ExerciseSession's internal properties.
    private static string? DecisionChoice(ExerciseSession session, string decisionPropertyName)
    {
        const System.Reflection.BindingFlags hidden =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public;
        var decision = typeof(ExerciseSession).GetProperty(decisionPropertyName, hidden)!.GetValue(session);
        if (decision is null) return null;
        var choice = decision.GetType().GetProperty("Decision", hidden)!.GetValue(decision);
        return (string?)choice!.GetType().GetProperty("Choice", hidden)!.GetValue(choice);
    }

    // Same reflection path as DecisionChoice above, generalized to any field
    // of the CampaignCombatSelectionFacts record (ContactHandle, ApparentLocationId,
    // BoundRepresentationId, ...) -- needed to prove an assault pick actually
    // bound a real target, not just that some legal choice was recorded.
    private static string? DecisionFact(ExerciseSession session, string decisionPropertyName, string factName)
    {
        const System.Reflection.BindingFlags hidden =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public;
        var decision = typeof(ExerciseSession).GetProperty(decisionPropertyName, hidden)!.GetValue(session);
        if (decision is null) return null;
        var choice = decision.GetType().GetProperty("Decision", hidden)!.GetValue(decision);
        return (string?)choice!.GetType().GetProperty(factName, hidden)!.GetValue(choice);
    }

    // Real adjacent-enemy analog of AdvanceToSecondCombatSelection above. Uses
    // CombatInfantryRequest's scenario (axis-infantry at "north",
    // commonwealth-infantry at "north-east" -- an adjacent pair by
    // construction, per CampaignContactCombatSelectionCutTests/
    // PrepareInfantryForceRound's own precedent for the first side) together
    // with AdvanceToContactCombat's preserveAdjacentInfantry knob, which keeps
    // both infantry elements from moving away during the Movement phase. The
    // first side voluntarily finishes its own Combat without attacking (still
    // legal today -- Land 10.3's mandatory-attack-in-ZOC rule is not yet
    // enforced, see CampaignContactCombatSelectionCut.Project's own remarks)
    // so neither infantry element is displaced or eliminated before the
    // second side ever reaches its own Combat-selection cut, where
    // commonwealth-infantry is then genuinely adjacent to a visible
    // axis-infantry contact.
    private static (CampaignCreationRequest Request, CampaignSeatCredential Axis,
        CampaignSeatCredential Commonwealth) AdvanceToSecondCombatSelectionWithAdjacentEnemy(
            CampaignDurableAuthorityJournal journal, CampaignCreationRequest? payable = null,
            string? secondReserveElementId = null)
    {
        var request = payable ?? CombatInfantryRequest();
        AdvanceToContactCombat(journal, request, Timing(request), preserveAdjacentInfantry: true);
        var axis = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var opening = journal.QueryCombatOwnerOpening(axis, "axis-infantry");
        var finishRef = opening.PublicActionRefs[^1];
        var finished = journal.SubmitOwnerCombatSelection(axis, "axis-infantry",
            opening.Revision, finishRef);
        if (!finished.IsAccepted)
            throw new InvalidOperationException("First-side voluntary finish was not accepted.");
        var control = journal.QueryCycleControlOwnerOpening(axis);
        if (!journal.SubmitOwnerCycleControl(axis, control.Revision,
                control.PublicActionRefs[^1]).IsAccepted)
            throw new InvalidOperationException("Cycle control was not accepted.");
        var convoy = journal.QueryTruckConvoyPassOwnerOpening(axis);
        if (!journal.SubmitOwnerTruckConvoyPass(axis, convoy.Revision,
                convoy.PublicActionRefs[0]).IsAccepted)
            throw new InvalidOperationException("Truck convoy pass was not accepted.");
        for (var index = 0; index < 4; index++)
        {
            var phase = journal.QueryPostCombatPassOwnerOpening(axis);
            if (!journal.SubmitOwnerPostCombatPass(axis, phase.Revision,
                    Assert.Single(phase.PublicActionRefs)).IsAccepted)
                throw new InvalidOperationException("Post-combat pass was not accepted.");
        }
        var commonwealth = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;
        // Finish Second Reserve designation immediately with nobody
        // designated -- same closed path AdvanceToSecondCombatSelection uses,
        // so commonwealth-infantry is never pulled into reserve and stays on
        // the map, still adjacent to axis-infantry.
        if (secondReserveElementId is not null)
        {
            var designation = journal.QuerySecondReserveOwnerOpening(commonwealth);
            using var choices = JsonDocument.Parse(designation.VisibleBytes);
            var index = Enumerable.Range(0, choices.RootElement.GetProperty("choices").GetArrayLength())
                .Single(value => choices.RootElement.GetProperty("choices")[value]
                    .GetProperty("elementId").GetString() == secondReserveElementId);
            if (!journal.SubmitOwnerSecondReserve(commonwealth, designation.Revision,
                    designation.PublicActionRefs[index]).IsAccepted)
                throw new InvalidOperationException("Second Reserve designation was not accepted.");
        }
        var secondReserve = journal.QuerySecondReserveOwnerOpening(commonwealth);
        if (!journal.SubmitOwnerSecondReserve(commonwealth, secondReserve.Revision,
                secondReserve.PublicActionRefs[^1]).IsAccepted)
            throw new InvalidOperationException("Second Reserve completion was not accepted.");
        var movementCompletion = journal.QuerySecondMovementCompletionOwnerOpening(commonwealth);
        var completed = journal.SubmitOwnerSecondMovementCompletion(commonwealth,
            movementCompletion.Revision, Assert.Single(movementCompletion.PublicActionRefs));
        if (!completed.IsAccepted)
            throw new InvalidOperationException("Second Movement completion was not accepted.");
        return (request, axis, commonwealth);
    }

    [Fact]
    public void SecondSideVoluntaryFinishIsLegalPersistedColdReplayableAndRetried()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, axis, commonwealth) = AdvanceToSecondCombatSelection(journal);
        var head = journal.ReadVerified(request.CampaignId);
        var checkpoint = CampaignExercises.QueryCheckpoint(head);
        Assert.Equal(46, checkpoint.ContractVersion); // CampaignNativeSecondBreakdownV46.CurrentContractVersion
        Assert.True(CampaignExercises.Reconstruct(head).IsVerified);

        // #394 gap 2: before the fix, this threw "Combat selection has
        // already advanced this authority cut." because the gate checked
        // only the (stale, first-side) CurrentCombatDecision.
        Assert.Throws<InvalidOperationException>(() =>
            journal.QueryCombatOwnerOpening(axis, "commonwealth-infantry"));
        var opening = journal.QueryCombatOwnerOpening(commonwealth, "commonwealth-infantry");
        Assert.NotEmpty(opening.PublicActionRefs);
        Assert.Contains("finish-combat-without-attack",
            System.Text.Encoding.UTF8.GetString(opening.VisibleBytes), StringComparison.Ordinal);
        var finishRef = opening.PublicActionRefs[^1];
        // The first side's own (unrelated) voluntary finish earlier in this
        // same test already produced one "native-reserve-release-completion"
        // step -- capture that baseline so the later assertion counts only
        // the second side's own completion.
        var reserveReleaseCountBeforeSecondSide = CountAcceptedStep(fixture.Path, request.CampaignId,
            "native-reserve-release-completion");

        // Legal finish/selection, atomic persistence.
        var accepted = journal.SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
            opening.Revision, finishRef);
        Assert.True(accepted.IsAccepted);

        // Retry through the normal durable Host: resubmitting the same
        // accepted action must be idempotent, not throw, and return the
        // same canonical receipt.
        var reopened = fixture.Open();
        var retry = reopened.SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
            opening.Revision, finishRef);
        Assert.True(retry.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);

        // Cold replay: the whole chain (first-side combat through the
        // second side's own decision and its auto-completed no-attack step)
        // must reconstruct and verify from durable storage alone.
        var cold = fixture.Open();
        var verified = cold.ReadVerified(request.CampaignId);
        var reconstruction = CampaignExercises.Reconstruct(verified);
        Assert.True(reconstruction.IsVerified);

        // #394 gap 1/extra-2: the decision is recorded on the new
        // side-scoped field, not on the stale first-side one, and the
        // second-side V41-V46 history (CurrentSecondBreakdown and its
        // predecessors) survives rather than being dropped.
        const System.Reflection.BindingFlags hidden =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var secondDecisionProperty = typeof(ExerciseSession).GetProperty(
            "CurrentSecondCombatDecision", hidden)!;
        var secondBreakdownProperty = typeof(ExerciseSession).GetProperty(
            "CurrentSecondBreakdown", hidden)!;
        var secondNoAttackProperty = typeof(ExerciseSession).GetProperty(
            "CurrentSecondNoAttackCompletion", hidden)!;
        Assert.NotNull(secondDecisionProperty.GetValue(verified));
        Assert.NotNull(secondBreakdownProperty.GetValue(verified));
        Assert.Equal("finish-combat-without-attack",
            DecisionChoice(verified, "CurrentSecondCombatDecision"));
        Assert.Equal("finish-combat-without-attack",
            DecisionChoice(verified, "CurrentCombatDecision"));
        Assert.NotSame(secondDecisionProperty.GetValue(verified),
            typeof(ExerciseSession).GetProperty("CurrentCombatDecision", hidden)!.GetValue(verified));

        // #394 extra-3: the Host auto-completes the second side's own
        // voluntary finish the same way it does for the first side, using
        // the side-correct check/command pair -- it must land on the new
        // CurrentSecondNoAttackCompletion field, not silently read or
        // mutate the first side's.
        Assert.NotNull(secondNoAttackProperty.GetValue(verified));
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId,
            "native-second-combat-no-attack"));
        // The second side's no-attack now opens its own Reserve Release
        // (InitializeAfterSecondNoAttack), which completes empty and leaves
        // the second side's Cycle Control pending.
        Assert.Equal(reserveReleaseCountBeforeSecondSide + 1, CountAcceptedStep(fixture.Path, request.CampaignId,
            "native-reserve-release-completion"));
        var released = CampaignExercises.QueryCheckpoint(verified);
        Assert.Equal(27, released.ContractVersion);
        Assert.Equal("land.position.operation-1.second-player.movement-and-combat.reserve-release",
            released.PositionId);
    }

    // Note: this fixture's second side never ends up adjacent to a visible
    // enemy contact (confirmed by inspecting the owner cut's VisibleBytes:
    // "contacts":[] / "currentTargets":[] / "assaultOffers":[]), so an actual
    // "select-contact-assault" pick isn't exercisable with this request
    // without a bespoke scenario. This test instead proves gap 2/extra-2's
    // gate correctly tells a legal action apart from an illegal one for the
    // second side specifically -- not just that it stopped throwing
    // "already advanced" (SecondSideVoluntaryFinish... above covers the
    // legal path end to end).
    [Fact]
    public void SecondSideCombatSelectionRejectsAnActionNotOfferedAtTheCut()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, _, commonwealth) = AdvanceToSecondCombatSelection(journal);
        var opening = journal.QueryCombatOwnerOpening(commonwealth, "commonwealth-infantry");
        var legalRef = Assert.Single(opening.PublicActionRefs);

        // Well-formed (68 chars, "act." + 64 hex digits, per IsPublicReference)
        // but not among the action IDs this opening actually mapped.
        var bogusRef = "act." + new string(legalRef[4] == '0' ? '1' : '0', 64);
        Assert.NotEqual(legalRef, bogusRef);
        var rejected = journal.SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
            opening.Revision, bogusRef);
        Assert.False(rejected.IsAccepted);

        // The bogus attempt must not have advanced or corrupted anything --
        // the legal action is still open and still accepted afterward.
        var stillOpen = journal.QueryCombatOwnerOpening(commonwealth, "commonwealth-infantry");
        Assert.Equal(legalRef, Assert.Single(stillOpen.PublicActionRefs));
        var accepted = journal.SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
            stillOpen.Revision, legalRef);
        Assert.True(accepted.IsAccepted);
        var verified = fixture.Open().ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(verified).IsVerified);
    }

    // Real adjacent-enemy counterpart to SecondSideCombatSelectionRejectsAnActionNotOfferedAtTheCut's
    // gate-only check and to SecondSideVoluntaryFinishIsLegalPersistedColdReplayableAndRetried's
    // voluntary-finish path: exercises CampaignNativeSecondCombatDecisionV47's
    // actual "select-contact-assault" pick, with a genuine adjacent enemy
    // contact present at the cut (see AdvanceToSecondCombatSelectionWithAdjacentEnemy's
    // remarks). Same acceptance bar as the voluntary-finish test -- legal,
    // atomically persisted, idempotently retried, cold-replayable -- for the
    // one choice that test's own fixture could never offer.
    [Fact]
    public void SecondSideRealAssaultSelectionIsLegalPersistedColdReplayableAndRetried()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, _, commonwealth) = AdvanceToSecondCombatSelectionWithAdjacentEnemy(journal);
        var head = journal.ReadVerified(request.CampaignId);
        var checkpoint = CampaignExercises.QueryCheckpoint(head);
        Assert.Equal(46, checkpoint.ContractVersion); // CampaignNativeSecondBreakdownV46.CurrentContractVersion
        Assert.True(CampaignExercises.Reconstruct(head).IsVerified);

        var opening = journal.QueryCombatOwnerOpening(commonwealth, "commonwealth-infantry");
        using var visible = JsonDocument.Parse(opening.VisibleBytes);
        var assaultOffers = visible.RootElement.GetProperty("assaultOffers");
        // A genuine adjacent-enemy cut, unlike SecondSideCombatSelectionRejectsAnActionNotOfferedAtTheCut's
        // fixture: a real assault offer against axis-infantry at "north" sits
        // alongside the voluntary finish offer, so PublicActionRefs holds more
        // than just the single finish ref that test exercises.
        Assert.True(assaultOffers.GetArrayLength() > 0,
            System.Text.Encoding.UTF8.GetString(opening.VisibleBytes));
        var assaultIndex = Enumerable.Range(0, assaultOffers.GetArrayLength())
            .Single(index => assaultOffers[index].GetProperty("apparentLocationId").GetString() == "north");
        Assert.True(opening.PublicActionRefs.Count > 1);
        var assaultRef = opening.PublicActionRefs[assaultIndex];
        Assert.NotEqual(opening.PublicActionRefs[^1], assaultRef); // the finish ref is last, per QueryContactCombatOwnerDraft.

        var noAttackCountBeforeAssault = CountAcceptedStep(fixture.Path, request.CampaignId,
            "native-second-combat-no-attack");

        // Legal real-target selection, atomic persistence.
        var accepted = journal.SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
            opening.Revision, assaultRef);
        Assert.True(accepted.IsAccepted);

        // Retry through the normal durable Host: resubmitting the same
        // accepted action must be idempotent, not throw, and return the
        // same canonical receipt.
        var reopened = fixture.Open();
        var retry = reopened.SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
            opening.Revision, assaultRef);
        Assert.True(retry.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);

        // Cold replay: the whole chain -- first-side combat through the
        // second side's own real assault pick -- must reconstruct and verify
        // from durable storage alone.
        var cold = fixture.Open();
        var verified = cold.ReadVerified(request.CampaignId);
        var reconstruction = CampaignExercises.Reconstruct(verified);
        Assert.True(reconstruction.IsVerified);

        // The decision is recorded on the side-scoped field, with a real
        // bound target, not a voluntary finish -- and the stale first-side
        // field is untouched.
        const System.Reflection.BindingFlags hidden =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var secondDecisionProperty = typeof(ExerciseSession).GetProperty(
            "CurrentSecondCombatDecision", hidden)!;
        var secondBreakdownProperty = typeof(ExerciseSession).GetProperty(
            "CurrentSecondBreakdown", hidden)!;
        var secondNoAttackProperty = typeof(ExerciseSession).GetProperty(
            "CurrentSecondNoAttackCompletion", hidden)!;
        Assert.NotNull(secondDecisionProperty.GetValue(verified));
        Assert.NotNull(secondBreakdownProperty.GetValue(verified));
        Assert.Equal("select-contact-assault",
            DecisionChoice(verified, "CurrentSecondCombatDecision"));
        Assert.Equal("finish-combat-without-attack",
            DecisionChoice(verified, "CurrentCombatDecision")); // the first side's own unrelated voluntary finish.
        Assert.NotSame(secondDecisionProperty.GetValue(verified),
            typeof(ExerciseSession).GetProperty("CurrentCombatDecision", hidden)!.GetValue(verified));

        // The real target binding -- the whole point of this test over the
        // legal/illegal gate check above it.
        var contactHandle = DecisionFact(verified, "CurrentSecondCombatDecision", "ContactHandle");
        Assert.NotNull(contactHandle);
        Assert.Equal("north",
            DecisionFact(verified, "CurrentSecondCombatDecision", "ApparentLocationId"));
        Assert.NotNull(DecisionFact(verified, "CurrentSecondCombatDecision", "BoundRepresentationId"));

        // A real assault pick must NOT trigger the second side's own
        // voluntary-no-attack auto-completion -- that only fires for
        // "finish-combat-without-attack" (see
        // SecondSideVoluntaryFinishIsLegalPersistedColdReplayableAndRetried).
        Assert.Null(secondNoAttackProperty.GetValue(verified));
        Assert.Equal(noAttackCountBeforeAssault, CountAcceptedStep(fixture.Path, request.CampaignId,
            "native-second-combat-no-attack"));
    }
}
