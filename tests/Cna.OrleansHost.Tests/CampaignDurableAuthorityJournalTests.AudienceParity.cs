using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Tests;

// Task 020D parity, option (b) (owner ruling 2026-11-20): before any Host-calls-Core cutover, prove the
// Host's existing owner openings serve exactly the bytes and offer count of the trusted
// CampaignHostAdapter drafts, and that a Host-accepted action records the same canonical event as the
// direct adapter path. Cna.Core.Tests separately pins each draft to the internal Core audience decision
// (CombatCycleDisclosureHostDraftParityTests); together the two hops give Host opening == Core decision.
// Core friend access stays limited to Cna.Core.Tests (AuthorityBoundaryTests), so this file only uses
// the public adapter.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public void HostOpeningsMatchAdapterDraftsFromSelectionThroughCycleControl()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        // Seed 5: the positive infantry assault captures, so Custody is reached (as in
        // CaptorCustodyUsesSideSafeOpeningAndColdReceipt).
        var request = PositiveCombatRequest(5);
        AdvanceToContactCombat(journal, request, Timing(request), preserveAdjacentInfantry: true);
        ExerciseSession Session() => journal.ReadVerified(request.CampaignId);
        var attacker = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var defender = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;

        // Selection
        var selection = journal.QueryCombatOwnerOpening(attacker, "axis-infantry");
        var selectionDraft = CampaignHostAdapter.QueryContactCombatOwnerDraft(Session(),
            CampaignActionAudience.Axis, "axis-infantry");
        AssertOpeningParity(selection, selectionDraft.VisibleBytes, selectionDraft.PrivateCombatActionIds.Count);
        using (var visible = System.Text.Json.JsonDocument.Parse(selection.VisibleBytes))
        {
            var offers = visible.RootElement.GetProperty("assaultOffers");
            var infantry = Enumerable.Range(0, offers.GetArrayLength()).Single(index =>
                offers[index].GetProperty("apparentLocationId").GetString() == "north-east");
            var preSelection = Session();
            var direct = CampaignHostAdapter.SubmitContactOwnerCombatSelection(preSelection,
                ParityCommand(preSelection, CampaignActionAudience.Axis,
                    selectionDraft.PrivateCombatActionIds[infantry]), "axis-infantry");
            Assert.True(journal.SubmitOwnerCombatSelection(attacker, "axis-infantry",
                selection.Revision, selection.PublicActionRefs[infantry]).IsAccepted);
            AssertSameAcceptedEvent(fixture.Path, CampaignHostAcceptedStep.ContactCombatSelectionKind, direct);
        }

        // Decline-only RBA (changed Host route)
        var defenderId = CampaignHostAdapter.QueryContactRbaDefenderId(Session(), CampaignActionAudience.Commonwealth);
        var preRba = Session();
        var rba = journal.QueryRbaOwnerOpening(defender, defenderId);
        var rbaDraft = CampaignHostAdapter.QueryContactRbaDeclineOwnerDraft(preRba,
            CampaignActionAudience.Commonwealth, defenderId);
        AssertOpeningParity(rba, rbaDraft.VisibleBytes, 1);
        Assert.Single(rbaDraft.PrivateActionIds);
        var directDecline = CampaignHostAdapter.SubmitContactDefenderRbaDecline(preRba,
            ParityCommand(preRba, CampaignActionAudience.Commonwealth, rbaDraft.PrivateActionIds[0]), defenderId);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, defenderId, rba.Revision,
            rba.PublicActionRefs[0]).IsAccepted);
        AssertSameAcceptedEvent(fixture.Path, CampaignHostAcceptedStep.ContactRbaDeclineKind, directDecline);

        // Force Assignment, both owners
        var attackerForce = journal.QueryForceOwnerOpening(attacker, "axis-infantry");
        var defenderForce = journal.QueryForceOwnerOpening(defender, defenderId);
        AssertOpeningParity(attackerForce, CampaignHostAdapter.QueryContactForceOwnerDraft(Session(),
            CampaignActionAudience.Axis, "axis-infantry").VisibleBytes, 1);
        AssertOpeningParity(defenderForce, CampaignHostAdapter.QueryContactForceOwnerDraft(Session(),
            CampaignActionAudience.Commonwealth, defenderId).VisibleBytes, 1);
        clock.Advance(TimeSpan.FromSeconds(1));
        var preSeal = Session();
        var directSeal = CampaignHostAdapter.SubmitContactOwnerForceSeal(preSeal, ParityCommand(preSeal,
            CampaignActionAudience.Axis, CampaignHostAdapter.QueryContactForceOwnerDraft(preSeal,
                CampaignActionAudience.Axis, "axis-infantry").PrivateActionIds[0]), "axis-infantry");
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        AssertSameAcceptedEvent(fixture.Path, CampaignHostAcceptedStep.ContactForceSealKind, directSeal);
        // The first seal does not change the other owner's opening.
        var defenderAfter = journal.QueryForceOwnerOpening(defender, defenderId);
        Assert.Equal(defenderForce.VisibleBytes, defenderAfter.VisibleBytes);
        Assert.Equal(defenderForce.PublicActionRefs, defenderAfter.PublicActionRefs);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        CompleteRetreatIfPending(journal, request.CampaignId, defender, defenderId);

        // Custody
        Assert.True(CampaignHostAdapter.IsNativeCombatCustodyPending(Session()));
        var (captor, captorAudience, captorId) = ResolveCaptor(journal, attacker, defender, defenderId);
        var custody = journal.QueryCustodyOwnerOpening(captor, captorId);
        var preCustody = Session();
        var custodyDraft = CampaignHostAdapter.QueryContactCustodyOwnerDraft(preCustody, captorAudience, captorId);
        AssertOpeningParity(custody, custodyDraft.VisibleBytes, custodyDraft.PrivateActionIds.Count);
        var directCustody = CampaignHostAdapter.SubmitNativeCombatCustody(preCustody,
            ParityCommand(preCustody, captorAudience, custodyDraft.PrivateActionIds[0]), captorId);
        Assert.True(journal.SubmitOwnerCustody(captor, captorId, custody.Revision,
            custody.PublicActionRefs[0]).IsAccepted);
        AssertSameAcceptedEvent(fixture.Path, CampaignHostAcceptedStep.ContactCombatCustodyKind, directCustody);

        // Cycle Control (no reserve member, so Reserve Release completes on its own)
        var control = journal.QueryCycleControlOwnerOpening(attacker);
        var preControl = Session();
        var controlDraft = CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(preControl, CampaignActionAudience.Axis);
        AssertOpeningParity(control, controlDraft.VisibleBytes, controlDraft.PrivateActionIds.Count);
        var directControl = CampaignHostAdapter.SubmitNativeCycleControl(preControl,
            ParityCommand(preControl, CampaignActionAudience.Axis, controlDraft.PrivateActionIds[^1]));
        Assert.True(journal.SubmitOwnerCycleControl(attacker, control.Revision,
            control.PublicActionRefs[^1]).IsAccepted);
        AssertSameAcceptedEvent(fixture.Path, CampaignHostAcceptedStep.ContactCycleControlKind, directControl);
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void HostReserveReleaseOpeningMatchesAdapterDraft()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: 50,
                reserveElementId: "axis-infantry-second", doubleContact: true);
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        CompleteRetreatIfPending(journal, request.CampaignId, defender, defenderId);
        Assert.False(CampaignHostAdapter.IsNativeCombatCustodyPending(journal.ReadVerified(request.CampaignId)));
        var release = journal.QueryReserveReleaseOwnerOpening(attacker, "axis-infantry-second");
        var preRelease = journal.ReadVerified(request.CampaignId);
        var draft = CampaignHostAdapter.QueryNativeReserveReleaseOwnerDraft(preRelease,
            CampaignActionAudience.Axis, "axis-infantry-second");
        AssertOpeningParity(release, draft.VisibleBytes, draft.PrivateActionIds.Count);
        var direct = CampaignHostAdapter.SubmitNativeReserveReleaseChoice(preRelease,
            ParityCommand(preRelease, CampaignActionAudience.Axis, draft.PrivateActionIds[0]), "axis-infantry-second");
        Assert.True(journal.SubmitOwnerReserveRelease(attacker, "axis-infantry-second",
            release.Revision, release.PublicActionRefs[0]).IsAccepted);
        AssertSameAcceptedEvent(fixture.Path, CampaignHostAcceptedStep.ContactReserveReleaseChoiceKind, direct);
    }

    private static void AssertOpeningParity(CampaignOwnerOpening opening, byte[] draftVisible, int draftOffers)
    {
        Assert.Equal(draftVisible, opening.VisibleBytes);
        Assert.Equal(draftOffers, opening.PublicActionRefs.Count);
        // Host refs are seat-bound HMAC refs, never the Core action IDs or Core audience refs.
        Assert.All(opening.PublicActionRefs, value => Assert.StartsWith("act.", value, StringComparison.Ordinal));
    }

    private static void AssertSameAcceptedEvent(string path, string kind, ExerciseStepResult direct)
    {
        Assert.True(direct.IsAccepted, direct.RejectionReason.ToString());
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT canonical_event FROM accepted_steps WHERE action_kind=$kind ORDER BY sequence DESC LIMIT 1";
        command.Parameters.AddWithValue("$kind", kind);
        Assert.Equal(direct.Evidence!.EventRecords.Single(), (byte[])command.ExecuteScalar()!);
    }

    private static CampaignActionSubmission ParityCommand(ExerciseSession session,
        CampaignActionAudience audience, string actionId)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(1, checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, actionId);
    }

    private static (CampaignSeatCredential Seat, CampaignActionAudience Audience, string ElementId)
        ResolveCaptor(CampaignDurableAuthorityJournal journal, CampaignSeatCredential attacker,
            CampaignSeatCredential defender, string defenderId)
    {
        try
        {
            _ = journal.QueryCustodyOwnerOpening(attacker, "axis-infantry");
            return (attacker, CampaignActionAudience.Axis, "axis-infantry");
        }
        catch (InvalidOperationException)
        {
            return (defender, CampaignActionAudience.Commonwealth, defenderId);
        }
    }
}
