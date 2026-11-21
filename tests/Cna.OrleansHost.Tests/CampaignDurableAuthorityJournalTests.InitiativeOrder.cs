using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// Owner ruling 2026-11-21 (release-blocking): the preamble's "who acts first" choice
// (ActFirstAction/ActLastAction, land.phase.initiative-declaration) had no owner-opening
// or HTTP stage at all -- found during the Movement/ZOC/Reaction/Breakdown adversarial
// audit. Every existing Host test reached this step only through the raw, non-HTTP
// CampaignDurableAuthorityJournal.Append/legal-action-set path (test/Exercise-Harness-only),
// never through a real player's seat credential. This file adds the missing
// InitiativeOrder owner opening/HTTP stage ("initiative-order"), built the same way every
// other owner opening works: a CampaignOwnerOpeningKind entry, Journal query/submit methods,
// and the matching CampaignGrain/outward-API stage -- no new Core logic (CampaignV11Preamble
// and CampaignLegalActions already compute and replay-verify this decision; this is a
// redacted Host wrapper over the same generic CampaignExercises.Query/Submit path).
public sealed partial class CampaignDurableAuthorityJournalTests
{
    // Drains the three system-audience preamble steps (initiative roll, the two
    // no-obligation naval/shipping resolves) that always precede ActFirst/ActLast, using
    // the campaign's own generic legal-action set -- these are audience=System automatic
    // resolutions, not an owner gap, so draining them this way (rather than through an
    // owner opening) is the correct, existing pattern, not a workaround.
    private static (CampaignCreationRequest Request, CampaignSeatCredential Winner,
        CampaignSeatCredential Loser) AdvanceToInitiativeDeclaration(CampaignDurableAuthorityJournal journal)
    {
        var request = CombatInfantryRequest();
        var session = journal.CreateCampaign(request);
        for (var index = 0; index < 10; index++)
        {
            var system = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet;
            if (system is null || system.Candidates.Count == 0) break;
            var pick = system.Candidates[0];
            var command = new CampaignActionSubmission(1, system.CampaignId, system.StateVersion,
                system.PositionId, system.Audience, pick.ActionId);
            var accepted = journal.Append(request.CampaignId, command);
            if (!accepted.IsAccepted)
                throw new InvalidOperationException("System preamble step was not accepted.");
            session = accepted.SuccessorSession!;
        }
        var axis = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var commonwealth = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;
        CampaignSeatCredential winner, loser;
        try
        {
            journal.QueryInitiativeOrderOwnerOpening(axis);
            winner = axis;
            loser = commonwealth;
        }
        catch (InvalidOperationException)
        {
            winner = commonwealth;
            loser = axis;
        }
        return (request, winner, loser);
    }

    [Fact]
    public async Task InitiativeOrderIsOfferedOnlyToTheWinningSideAndAcceptedThroughHttp()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, winner, loser) = AdvanceToInitiativeDeclaration(journal);

        // Side-safe: the losing side has no opening here at all, same invariant every
        // other asymmetric owner opening (Combat Selection, Custody, ...) already holds.
        Assert.Throws<InvalidOperationException>(() => journal.QueryInitiativeOrderOwnerOpening(loser));

        var opening = journal.QueryInitiativeOrderOwnerOpening(winner);
        Assert.Equal(2, opening.PublicActionRefs.Count);
        using (var visible = JsonDocument.Parse(opening.VisibleBytes))
        {
            Assert.False(visible.RootElement.TryGetProperty("authorityVersion", out _));
            var choices = visible.RootElement.GetProperty("choices").EnumerateArray()
                .Select(value => value.GetString()).ToArray();
            Assert.Equal(["act-first", "act-last"], choices);
        }

        // The real outward HTTP API, not CampaignDurableAuthorityJournal.Append: the whole
        // point of this fix. "act-first" is PublicActionRefs[0] (choices/refs share the
        // act-first,act-last order CampaignLegalActionSet's own Kind-then-ActionId sort gives).
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.Submit(winner, opening.ActingElementId, "initiative-order",
            "initiative-order", opening, opening.PublicActionRefs[0]);

        var cold = fixture.Open();
        var afterHttp = cold.ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(afterHttp).IsVerified);
        var checkpoint = CampaignExercises.QueryCheckpoint(afterHttp);
        Assert.Equal(5, checkpoint.StateVersion);

        // Retry through the normal durable Host: resubmitting the same accepted reference
        // is idempotent, not a throw, and returns the same canonical receipt.
        var retried = cold.SubmitOwnerInitiativeOrder(winner, opening.Revision, opening.PublicActionRefs[0]);
        Assert.True(retried.IsAccepted);

        // The decision is gone: a second query for this (now-resolved) preamble step throws,
        // same "no owner opening is available at this cut" pattern every other one-shot
        // decision uses once its own successor position is reached.
        Assert.Throws<InvalidOperationException>(() => cold.QueryInitiativeOrderOwnerOpening(winner));
    }

    [Fact]
    public void InitiativeOrderRejectsStaleForgedAndWrongAudienceSubmissions()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, winner, loser) = AdvanceToInitiativeDeclaration(journal);
        var opening = journal.QueryInitiativeOrderOwnerOpening(winner);
        var before = StoredAuthority(fixture.Path, request.CampaignId);

        // Wrong audience: the losing side cannot submit even a well-formed reference.
        Assert.False(journal.SubmitOwnerInitiativeOrder(loser, opening.Revision,
            opening.PublicActionRefs[0]).IsAccepted);
        // Stale revision.
        Assert.False(journal.SubmitOwnerInitiativeOrder(winner, opening.Revision + 1,
            opening.PublicActionRefs[0]).IsAccepted);
        // Forged reference: well-formed ("act." + 64 hex) but never issued.
        var forged = "act." + new string(opening.PublicActionRefs[0][4] == '0' ? '1' : '0', 64);
        Assert.NotEqual(opening.PublicActionRefs[0], forged);
        Assert.False(journal.SubmitOwnerInitiativeOrder(winner, opening.Revision, forged).IsAccepted);
        // Oversized reference.
        Assert.False(journal.SubmitOwnerInitiativeOrder(winner, opening.Revision,
            "act." + new string('0', 1_000_000)).IsAccepted);

        Assert.Equal(before, StoredAuthority(fixture.Path, request.CampaignId));
        Assert.True(CampaignExercises.Reconstruct(journal.ReadVerified(request.CampaignId)).IsVerified);

        // The genuine offer still works after every rejected attempt above.
        var accepted = journal.SubmitOwnerInitiativeOrder(winner, opening.Revision,
            opening.PublicActionRefs[^1]);
        Assert.True(accepted.IsAccepted);
        var cold = fixture.Open();
        Assert.True(CampaignExercises.Reconstruct(cold.ReadVerified(request.CampaignId)).IsVerified);
    }

    private static string StoredAuthority(string path, string campaignId)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}");
        connection.Open();
        var tables = new List<List<string[]>>();
        foreach (var table in new[] { "campaigns", "accepted_steps", "owner_openings", "owner_actions" })
        {
            using var query = connection.CreateCommand();
            query.CommandText = $"SELECT * FROM {table} WHERE campaign_id=$id ORDER BY rowid";
            query.Parameters.AddWithValue("$id", campaignId);
            using var reader = query.ExecuteReader();
            var rows = new List<string[]>();
            while (reader.Read())
                rows.Add(Enumerable.Range(0, reader.FieldCount).Select(index =>
                    reader.GetValue(index) is byte[] bytes ? Convert.ToHexString(bytes)
                        : reader.GetValue(index)?.ToString() ?? "null").ToArray());
            tables.Add(rows);
        }
        return string.Join("|", tables.Select(rows => string.Join(";", rows.Select(row => string.Join(",", row)))));
    }

    [Fact]
    public void InitiativeOrderAcceptanceSurvivesColdRestartAndDrivesThePreambleForward()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var (request, winner, _) = AdvanceToInitiativeDeclaration(journal);
        var opening = journal.QueryInitiativeOrderOwnerOpening(winner);
        Assert.True(journal.SubmitOwnerInitiativeOrder(winner, opening.Revision,
            opening.PublicActionRefs[0]).IsAccepted);

        // Cold replay from durable storage alone, fresh journal handle.
        var cold = fixture.Open();
        var verified = cold.ReadVerified(request.CampaignId);
        var reconstruction = CampaignExercises.Reconstruct(verified);
        Assert.True(reconstruction.IsVerified);
        Assert.Equal(5, CampaignExercises.QueryCheckpoint(verified).StateVersion);

        // The accepted choice actually moved the preamble forward: the next candidate is
        // the (system-audience) weather resolve, not the same act-first/act-last choice.
        var next = CampaignExercises.Query(verified, CampaignActionAudience.System).ActionSet;
        Assert.NotNull(next);
        Assert.Contains(next!.Candidates, candidate => candidate is ResolveWeatherAction);
    }
}
