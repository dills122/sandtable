using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static ExerciseSession ReachActLastCombat(CampaignDurableAuthorityJournal journal,
        CampaignCreationRequest request)
    {
        var session = journal.CreateCampaign(request, Timing(request));
        var choseActLast = false;
        for (var index = 0; index < 80; index++)
        {
            try
            {
                _ = CampaignHostAdapter.QueryContactCombatOwnerDraft(session,
                    CampaignActionAudience.Commonwealth, "commonwealth-infantry");
                Assert.True(choseActLast);
                return session;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                // Advance ordinary campaign authority to Combat.
            }
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .FirstOrDefault(value => value?.Candidates.Count > 0);
            Assert.NotNull(set);
            var action = set.Candidates.FirstOrDefault(value => value is ActLastAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteMovementSegmentAction)
                ?? set.Candidates[0];
            var result = journal.Append(request.CampaignId, new CampaignActionSubmission(1,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, action.ActionId));
            Assert.True(result.IsAccepted, result.RejectionReason.ToString());
            if (action is ActLastAction) choseActLast = true;
            session = result.SuccessorSession!;
        }
        throw new InvalidOperationException("Act-last Combat was not reached.");
    }

    [Fact]
    public void ActLastOwnerDecisionCommitsAndColdReplays()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = PositiveCombatRequest();
        _ = ReachActLastCombat(journal, request);
        var owner = journal.RedeemSeatGrant(journal.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Commonwealth, OperatorSecret))!;
        var other = journal.RedeemSeatGrant(journal.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Axis, OperatorSecret))!;
        Assert.Throws<InvalidOperationException>(() => journal.QueryCombatOwnerOpening(other, "axis-infantry"));
        var opening = journal.QueryCombatOwnerOpening(owner, "commonwealth-infantry");
        Assert.NotEmpty(opening.PublicActionRefs);
        var action = opening.PublicActionRefs[^1];
        var accepted = journal.SubmitOwnerCombatSelection(owner, "commonwealth-infantry", opening.Revision, action);
        Assert.True(accepted.IsAccepted);
        var reopened = fixture.Open();
        var retry = reopened.SubmitOwnerCombatSelection(owner, "commonwealth-infantry", opening.Revision, action);
        Assert.True(retry.IsAccepted);
        Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
        Assert.True(CampaignExercises.Reconstruct(reopened.ReadVerified(request.CampaignId)).IsVerified);
        using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT canonical_event FROM accepted_steps WHERE campaign_id=$id AND action_kind='contact-combat-selection'";
        query.Parameters.AddWithValue("$id", request.CampaignId);
        using var recorded = JsonDocument.Parse((byte[])query.ExecuteScalar()!);
        Assert.Equal(39, recorded.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.Equal("commonwealth", recorded.RootElement.GetProperty("side").GetString());
    }

    [Fact]
    public void ActLastTimeoutUsesActingSideAndColdReplays()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var request = PositiveCombatRequest();
        _ = ReachActLastCombat(journal, request);
        clock.Advance(TimeSpan.FromMilliseconds(30_000));
        var reopened = fixture.Open(clock);
        Assert.True(reopened.TickSelectionRound(request.CampaignId));
        Assert.True(CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
        using var recorded = SelectionTimeoutEvent(fixture.Path, request.CampaignId);
        Assert.Equal(40, recorded.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.Equal("commonwealth", recorded.RootElement.GetProperty("side").GetString());
    }
}
