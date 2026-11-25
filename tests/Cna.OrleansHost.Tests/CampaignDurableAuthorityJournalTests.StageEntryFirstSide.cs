using System.Net;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public async Task StageEntryFirstSidePreambleAndOwnerOpeningsAreDiscoverableAndReplayable()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, axis, commonwealth) = AdvanceToSecondCombatSelectionWithAdjacentEnemy(journal);
        var selection = journal.QueryCombatOwnerOpening(commonwealth, "commonwealth-infantry");
        Assert.True(journal.SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
            selection.Revision, selection.PublicActionRefs[^1]).IsAccepted);
        var control = journal.QueryCycleControlOwnerOpening(commonwealth);
        Assert.True(journal.SubmitOwnerCycleControl(commonwealth, control.Revision, control.PublicActionRefs[^1]).IsAccepted);
        var convoy = journal.QueryTruckConvoyPassOwnerOpening(commonwealth);
        Assert.True(journal.SubmitOwnerTruckConvoyPass(commonwealth, convoy.Revision, convoy.PublicActionRefs[0]).IsAccepted);
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);

        async Task<List<(string OpeningStage, string ActionStage, string[] Elements, bool RuleNotImplemented)>> Open(
            CampaignSeatCredential seat)
        {
            var (status, body) = await outward.OpenForSeatResponse(seat);
            Assert.True(status == HttpStatusCode.OK, body);
            return ParseOpen(body);
        }

        const string declaration = "stage-entry-initiative-declaration";
        const string declarationKey = "land.stage-entry-initiative-declaration";
        for (var index = 0; index < 4; index++)
        {
            Assert.Contains(await Open(commonwealth), entry => entry.OpeningStage == "post-combat-pass");
            foreach (var seat in new[] { axis, commonwealth })
            {
                Assert.DoesNotContain(await Open(seat), entry => entry.OpeningStage == declaration);
                Assert.Equal(HttpStatusCode.Conflict, (await outward.OpeningResponse(seat, declarationKey, declaration)).Status);
            }
            var pass = journal.QueryPostCombatPassOwnerOpening(commonwealth);
            Assert.True(journal.SubmitOwnerPostCombatPass(commonwealth, pass.Revision, Assert.Single(pass.PublicActionRefs)).IsAccepted);
        }

        var holder = (await Open(axis)).Any(entry => entry.OpeningStage == declaration) ? axis : commonwealth;
        var other = holder == axis ? commonwealth : axis;
        Assert.Contains(await Open(holder), entry => entry.OpeningStage == declaration && entry.Elements.SequenceEqual([declarationKey]));
        Assert.DoesNotContain(await Open(other), entry => entry.OpeningStage == declaration);
        var initiative = await QueryOpeningForOpenSeatTest(outward, holder, declarationKey, declaration);
        Assert.Equal(HttpStatusCode.OK, (await outward.SubmitResponse(holder, declarationKey, declaration,
            initiative.Revision, initiative.Refs[0])).Status);

        const string reserve = "stage-entry-reserve";
        const string reserveKey = "land.stage-entry-reserve";
        var before = CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId));
        var beforeRows = JournalFixture.UsesMarten ? null
            : OpenForSeatTables.Select(table => IntegratedCampaignRows(fixture.Path, table)).ToArray();
        Assert.Contains(await Open(holder), entry => entry.OpeningStage == reserve && entry.Elements.SequenceEqual([reserveKey]));
        Assert.DoesNotContain(await Open(other), entry => entry.OpeningStage == reserve);
        if (beforeRows is not null)
            for (var index = 0; index < OpenForSeatTables.Length; index++)
                Assert.Equal(beforeRows[index], IntegratedCampaignRows(fixture.Path, OpenForSeatTables[index]));
        var afterPreview = CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId));
        Assert.Equal(before.StateVersion, afterPreview.StateVersion);
        var reserveCut = await QueryOpeningForOpenSeatTest(outward, holder, reserveKey, reserve);
        var afterPreamble = CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId));
        Assert.Equal(before.StateVersion + 5, afterPreamble.StateVersion);
        Assert.Equal("land.position.operation-2.first-player.reserve-designation", afterPreamble.PositionId);
        Assert.Equal(HttpStatusCode.Conflict, (await outward.OpeningResponse(other, reserveKey, reserve)).Status);
        var complete = reserveCut.Refs[^1];
        Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(other, reserveKey, reserve, reserveCut.Revision, complete)).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(holder, reserveKey, reserve, reserveCut.Revision + 1, complete)).Status);
        var forged = complete[..^1] + (complete[^1] == '0' ? '1' : '0');
        Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(holder, reserveKey, reserve, reserveCut.Revision, forged)).Status);
        var accepted = await outward.SubmitResponse(holder, reserveKey, reserve, reserveCut.Revision, complete);
        Assert.Equal(HttpStatusCode.OK, accepted.Status);
        Assert.Equal(accepted, await outward.SubmitResponse(holder, reserveKey, reserve, reserveCut.Revision, complete));
        Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(other, reserveKey, reserve, reserveCut.Revision, complete)).Status);
        Assert.DoesNotContain(await Open(holder), entry => entry.OpeningStage == reserve);

        const string movement = "stage-entry-movement";
        var actor = holder == axis ? "axis-infantry" : "commonwealth-infantry";
        var move = await QueryOpeningForOpenSeatTest(outward, holder, actor, movement);
        Assert.Equal("land.position.operation-2.first-player.movement-and-combat.movement",
            move.Visible.GetProperty("positionId").GetString());
        // Known Core gap: the retained world has not acquired a Stage 2 movement ledger yet.
        // No legal action can be invented by the Host; direct query works, discovery excludes it.
        Assert.Equal(1, move.Visible.GetProperty("ownElements")[0].GetProperty("ledgerOperationStage").GetInt32());
        Assert.Empty(move.Refs);
        Assert.DoesNotContain(await Open(holder), entry => entry.OpeningStage == movement);
        Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(holder, actor, movement, move.Revision, forged)).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await outward.OpeningResponse(other, actor, movement)).Status);
        const string finish = "stage-entry-movement-completion";
        const string finishKey = "land.stage-entry-movement-completion";
        Assert.Contains(await Open(holder), entry => entry.OpeningStage == finish && entry.Elements.SequenceEqual([finishKey]));
        Assert.DoesNotContain(await Open(other), entry => entry.OpeningStage == finish);
        var completion = await QueryOpeningForOpenSeatTest(outward, holder, finishKey, finish);
        var finishRef = Assert.Single(completion.Refs);
        Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(other, finishKey, finish, completion.Revision, finishRef)).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(holder, finishKey, finish, completion.Revision + 1, finishRef)).Status);
        var finishForged = finishRef[..^1] + (finishRef[^1] == '0' ? '1' : '0');
        Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(holder, finishKey, finish, completion.Revision, finishForged)).Status);
        var finished = await outward.SubmitResponse(holder, finishKey, finish, completion.Revision, finishRef);
        Assert.Equal(HttpStatusCode.OK, finished.Status);
        Assert.Equal(finished, await outward.SubmitResponse(holder, finishKey, finish, completion.Revision, finishRef));
        Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(other, finishKey, finish, completion.Revision, finishRef)).Status);
        Assert.DoesNotContain(await Open(holder), entry => entry.OpeningStage == finish);
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal(46, CampaignExercises.QueryCheckpoint(cold).ContractVersion);
        // Stage 2 selection is not yet wired. There must be no collecting round to
        // schedule indefinitely while its owner and timeout continuations are unavailable.
        Assert.Null(fixture.Open(clock).GetSelectionRoundDeadlineMs(request.CampaignId));
    }
}
