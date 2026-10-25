using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static (CampaignCreationRequest Request, CampaignSeatCredential Owner)
        PrepareSelectionRound(CampaignDurableAuthorityJournal journal, Action? beforeEntry = null,
            bool withTiming = true)
    {
        var request = PositiveCombatRequest(6);
        AdvanceToContactCombat(journal, request, withTiming ? Timing(request) : null,
            preserveAdjacentInfantry: true, beforeSelectionEntry: beforeEntry);
        var owner = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        return (request, owner);
    }

    private static JsonDocument SelectionTimeoutEvent(string path, string campaignId)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT canonical_event FROM accepted_steps WHERE campaign_id=$id AND action_kind='native-combat-selection-timeout' ORDER BY sequence DESC LIMIT 1";
        query.Parameters.AddWithValue("$id", campaignId);
        return JsonDocument.Parse((byte[])query.ExecuteScalar()!);
    }

    [Fact]
    public void SelectionTimeoutStartsAtEntryAndExpiresWithoutQueryAfterRestart()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _) = PrepareSelectionRound(journal);
        Assert.Contains(request.CampaignId, journal.CollectingSelectionCampaigns("", 16));
        clock.Advance(TimeSpan.FromMilliseconds(29_999));
        Assert.False(journal.TickSelectionRound(request.CampaignId));
        clock.Advance(TimeSpan.FromMilliseconds(1));
        var cold = fixture.Open(clock);
        new CampaignCombatTimeoutService(cold, clock, NullLogger<CampaignCombatTimeoutService>.Instance)
            .SweepOnce(CancellationToken.None);
        var session = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(session).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        Assert.False(cold.TickSelectionRound(request.CampaignId));
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "contact-combat-selection"));
        using var evidence = SelectionTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal("no-selection", evidence.RootElement.GetProperty("fallback").GetString());
        Assert.Equal("deadline", evidence.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public void SelectionTimeoutPreservesAcceptedOwnerReceiptAndDoesNotRunAgain()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, owner) = PrepareSelectionRound(journal);
        var opening = journal.QueryCombatOwnerOpening(owner, "axis-infantry");
        var accepted = journal.SubmitOwnerCombatSelection(owner, "axis-infantry", opening.Revision, opening.PublicActionRefs[^1]);
        Assert.True(accepted.IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(30));
        var cold = fixture.Open(clock);
        Assert.False(cold.TickSelectionRound(request.CampaignId));
        var retry = cold.SubmitOwnerCombatSelection(owner, "axis-infantry", opening.Revision, opening.PublicActionRefs[^1]);
        Assert.True(retry.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-selection-timeout"));
    }

    [Fact]
    public void SelectionTimeoutInvalidLateProposalCannotConsumeRound()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, owner) = PrepareSelectionRound(journal);
        var opening = journal.QueryCombatOwnerOpening(owner, "axis-infantry");
        var before = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion;
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.False(journal.SubmitOwnerCombatSelection(owner, "axis-infantry", opening.Revision, "invalid").IsAccepted);
        Assert.Equal(before, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion);
        Assert.False(journal.SubmitOwnerCombatSelection(owner, "axis-infantry", opening.Revision, opening.PublicActionRefs[^1]).IsAccepted);
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-selection-timeout"));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectionTimeoutClockFailureOrRegressionRetainsExplicitEvidence(bool regress)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _) = PrepareSelectionRound(journal);
        clock.Advance(TimeSpan.FromSeconds(5));
        Assert.False(journal.TickSelectionRound(request.CampaignId));
        if (regress) clock.Advance(TimeSpan.FromSeconds(-1));
        else clock.IsAvailable = false;
        Assert.True(journal.TickSelectionRound(request.CampaignId));
        using var evidence = SelectionTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal(regress ? "clock-regression" : "clock-unavailable", evidence.RootElement.GetProperty("reason").GetString());
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectionTimeoutMissingOrAlteredRoundFailsClosed(bool missing)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _) = PrepareSelectionRound(journal);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = missing ? "DELETE FROM selection_rounds" : "UPDATE selection_rounds SET deadline_ms=deadline_ms+1";
        command.ExecuteNonQuery();
        Assert.Throws<InvalidOperationException>(() => fixture.Open(clock).ReadVerified(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => journal.TickSelectionRound(request.CampaignId));
    }

    [Fact]
    public void SelectionTimeoutLegacyThirteenRemainsReadOnly()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, owner) = PrepareSelectionRound(journal);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM selection_rounds; PRAGMA user_version=13";
        command.ExecuteNonQuery();
        var legacy = fixture.Open(clock);
        Assert.NotNull(legacy.ReadVerified(request.CampaignId));
        Assert.False(legacy.TickSelectionRound(request.CampaignId));
        Assert.Throws<InvalidOperationException>(() => legacy.QueryCombatOwnerOpening(owner, "axis-infantry"));
    }

    [Fact]
    public void SelectionTimeoutRoundInsertFailureRollsBackEntry()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create(new ManualClock());
        long before = -1;
        string? campaign = null;
        Assert.Throws<SqliteException>(() => PrepareSelectionRound(journal, () =>
        {
            using var connection = new SqliteConnection($"Data Source={fixture.Path}");
            connection.Open();
            using var query = connection.CreateCommand();
            query.CommandText = "SELECT campaign_id FROM campaigns";
            campaign = (string)query.ExecuteScalar()!;
            before = CampaignExercises.QueryCheckpoint(journal.ReadVerified(campaign)).StateVersion;
            query.CommandText = "CREATE TRIGGER fail_selection BEFORE INSERT ON selection_rounds BEGIN SELECT RAISE(ABORT,'injected selection failure'); END";
            query.ExecuteNonQuery();
        }));
        Assert.Equal(before, CampaignExercises.QueryCheckpoint(fixture.Open().ReadVerified(campaign!)).StateVersion);
    }

    [Fact]
    public void SelectionTimeoutNoAttackFailureRollsBackDispositionAndRoundClosure()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _) = PrepareSelectionRound(journal);
        var before = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion;
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TRIGGER fail_selection_successor BEFORE INSERT ON accepted_steps WHEN NEW.action_kind='native-combat-no-attack' BEGIN SELECT RAISE(ABORT,'injected successor failure'); END";
        command.ExecuteNonQuery();
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.Throws<SqliteException>(() => journal.TickSelectionRound(request.CampaignId));
        Assert.Equal(before, CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).StateVersion);
        Assert.Contains(request.CampaignId, journal.CollectingSelectionCampaigns("", 16));
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-selection-timeout"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectionTimeoutUnavailableAtEntryClosesWithoutInventedRound(bool missingClock)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var request = PositiveCombatRequest(6);
        AdvanceToContactCombat(journal, request, missingClock ? Timing(request) : null,
            preserveAdjacentInfantry: true,
            beforeSelectionEntry: missingClock ? () => clock.IsAvailable = false : null);
        Assert.Empty(journal.CollectingSelectionCampaigns("", 16));
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-selection-timeout"));
        using var evidence = SelectionTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal("selection-unavailable", evidence.RootElement.GetProperty("reason").GetString());
        Assert.Equal(-1, evidence.RootElement.GetProperty("openedAtUnixMilliseconds").GetInt64());
        // Append's accepted receipt describes its input transition; durable authority
        // also includes atomic System fallbacks and is read separately.
        Assert.Equal(34, CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(journal.ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void SelectionTimeoutGenericAppendCannotBypassDeadline()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, _) = PrepareSelectionRound(journal);
        var session = journal.ReadVerified(request.CampaignId);
        var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(session,
            CampaignActionAudience.Axis, "axis-infantry");
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var proposal = new CampaignActionSubmission(1, checkpoint.CampaignId, checkpoint.StateVersion,
            checkpoint.PositionId, CampaignActionAudience.Axis, draft.PrivateCombatActionIds[^1]);
        var before = checkpoint.StateVersion;
        clock.Advance(TimeSpan.FromSeconds(30));
        // Native owner choices have no generic legal-action route. Even a real
        // owner action ID submitted through generic Append cannot bypass that fence.
        Assert.False(journal.Append(request.CampaignId, proposal).IsAccepted);
        Assert.Equal(before, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).StateVersion);
        Assert.Equal(0, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-selection-timeout"));
        Assert.True(journal.TickSelectionRound(request.CampaignId));
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-selection-timeout"));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void SelectionTimeoutStartsFreshRoundAtGenuineRepeatBreakdown()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: 6,
                reserveElementId: "axis-infantry-second", doubleContact: true);
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        Assert.Equal(22, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).ContractVersion);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(journal.TickRetreatRound(request.CampaignId));
        var release = journal.QueryReserveReleaseOwnerOpening(attacker, "axis-infantry-second");
        Assert.True(journal.SubmitOwnerReserveRelease(attacker, "axis-infantry-second",
            release.Revision, release.PublicActionRefs[0]).IsAccepted);
        var control = journal.QueryCycleControlOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerCycleControl(attacker, control.Revision, control.PublicActionRefs[0]).IsAccepted);
        var movement = journal.QueryRepeatMovementOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerRepeatMovement(attacker, movement.Revision, movement.PublicActionRefs[1]).IsAccepted);
        Assert.Contains(request.CampaignId, journal.CollectingSelectionCampaigns("", 16));
        clock.Advance(TimeSpan.FromMilliseconds(29_999));
        Assert.False(fixture.Open(clock).TickSelectionRound(request.CampaignId));
        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.True(fixture.Open(clock).TickSelectionRound(request.CampaignId));
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-selection-timeout"));
        Assert.Equal(1, CountAcceptedStep(fixture.Path, request.CampaignId, "native-repeat-combat-no-attack"));
        using var evidence = SelectionTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal(2, evidence.RootElement.GetProperty("cycleOrdinal").GetInt32());
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

    [Fact]
    public void SelectionTimeoutInitialEntryAfterOwnerMovementUsesOriginalClock()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var request = ContactRequest();
        AdvanceToContactMovement(journal, request, Timing(request));
        var owner = journal.RedeemSeatGrant(journal.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Axis, OperatorSecret))!;
        var opening = journal.QueryOwnerOpening(owner, "axis-infantry");
        Assert.True(journal.SubmitOwnerMovement(owner, "axis-infantry", opening.Revision,
            opening.PublicActionRefs[0]).IsAccepted);
        for (var i = 0; i < 80 && !journal.CollectingSelectionCampaigns("", 16).Contains(request.CampaignId); i++)
        {
            var session = journal.ReadVerified(request.CampaignId);
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var action = set.Candidates.FirstOrDefault(value => value is not MoveElementAction) ?? set.Candidates[0];
            Assert.True(journal.Append(request.CampaignId, new CampaignActionSubmission(1,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, action.ActionId)).IsAccepted);
        }
        Assert.Contains(request.CampaignId, journal.CollectingSelectionCampaigns("", 16));
        clock.Advance(TimeSpan.FromSeconds(20));
        _ = journal.QueryCombatOwnerOpening(owner, "axis-infantry");
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.True(fixture.Open(clock).TickSelectionRound(request.CampaignId));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
    }

}
