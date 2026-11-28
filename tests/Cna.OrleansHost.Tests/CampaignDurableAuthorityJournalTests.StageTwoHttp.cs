using System.Net;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public Task TwoHttpSeatsCompleteStageTwoWithDiscoveryAndColdReplay() => RunStagesHttp(includeStageThree: false);

    private static async Task RunStagesHttp(bool includeStageThree, int? boundaryTurn = null,
        Func<OwnerHttpHarness, CampaignSeatCredential, ExerciseCheckpoint, int, Task>? observationCheck = null)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var request = boundaryTurn is { } turn ? TurnBoundaryRequest(turn) : CombatInfantryRequest();
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.CreateCampaign(request, Timing(request), OperatorSecret);
        var axis = await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret);
        var commonwealth = await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret);
        // Boundary-specific tests only need the real prior-turn checkpoint, not the W5/W6
        // assertions repeated for every owner action while constructing that checkpoint.
        var checkingBoundary = false;

        CampaignSeatCredential Other(CampaignSeatCredential seat) => seat == axis ? commonwealth : axis;
        string Element(CampaignSeatCredential seat) => seat == axis ? "axis-infantry" : "commonwealth-infantry";
        async Task<List<(string OpeningStage, string ActionStage, string[] Elements, bool RuleNotImplemented)>> Discover(CampaignSeatCredential seat)
        {
            var (status, body) = await outward.OpenForSeatResponse(seat);
            Assert.True(status == HttpStatusCode.OK, body);
            return ParseOpen(body);
        }
        void Replay()
        {
            var live = journal.ReadVerified(request.CampaignId);
            var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
            var liveCheckpoint = CampaignExercises.QueryCheckpoint(live);
            var coldCheckpoint = CampaignExercises.QueryCheckpoint(cold);
            Assert.Equal((liveCheckpoint.PositionId, liveCheckpoint.StateVersion, liveCheckpoint.ContractVersion),
                (coldCheckpoint.PositionId, coldCheckpoint.StateVersion, coldCheckpoint.ContractVersion));
            var reconstruction = CampaignExercises.Reconstruct(cold);
            Assert.True(reconstruction.IsVerified, $"{coldCheckpoint.PositionId}: {reconstruction.FailureReason}");
            Assert.Equal(reconstruction.ExpectedSnapshotHash, reconstruction.ReconstructedSnapshotHash);
            Assert.Equal(CampaignExercises.Reconstruct(live).EventStreamHash, reconstruction.EventStreamHash);
        }
        async Task Play(CampaignSeatCredential seat, string stage, string key, bool last = true, bool verify = false)
        {
            var buildingCheckpoint = boundaryTurn is not null && !checkingBoundary;
            verify &= !buildingCheckpoint;
            if (!buildingCheckpoint)
            {
                var own = await Discover(seat);
                var other = await Discover(Other(seat));
                Assert.Contains(own, entry => entry.OpeningStage == stage && entry.Elements.Contains(key));
                Assert.DoesNotContain(other, entry => entry.OpeningStage == stage);
                if (verify)
                {
                    var required = stage == "stage-entry-movement-completion"
                        ? own.Where(entry => entry.OpeningStage != "stage-entry-movement") : own;
                    Assert.Equal(stage, Assert.Single(required).OpeningStage);
                    Assert.Empty(other);
                }
            }
            var opening = await QueryOpeningForOpenSeatTest(outward, seat, key, stage);
            var before = verify ? CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)) : null;
            if (verify) Replay();
            var reference = last ? opening.Refs[^1] : opening.Refs[0];
            if (stage is "reserve-designation" or "stage-entry-reserve")
            {
                var choices = opening.Visible.GetProperty("choices").EnumerateArray()
                    .Select(value => value.GetProperty("kind").GetString()).ToArray();
                reference = opening.Refs[Array.IndexOf(choices, "complete")];
            }
            if (verify)
            {
                Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(Other(seat), key, stage, opening.Revision, reference)).Status);
                Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(seat, key, stage, opening.Revision + 1, reference)).Status);
            }
            var accepted = await outward.SubmitResponse(seat, key, stage, opening.Revision, reference);
            Assert.True(accepted.Status == HttpStatusCode.OK, $"{stage}: {accepted}");
            if (observationCheck is not null)
                await observationCheck(outward, seat, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)),
                    (boundaryTurn ?? 1) + (checkingBoundary ? 1 : 0));
            if (!buildingCheckpoint)
                Assert.Equal(accepted, await outward.SubmitResponse(seat, key, stage, opening.Revision, reference));
            if (verify)
            {
                var after = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId));
                var committed = stage switch
                {
                    "stage-entry-movement-completion" => 2, // owner + Breakdown
                    "combat-selection" => 3, // owner + no-attack + empty Reserve Release
                    _ => 1,
                };
                Assert.Equal(before!.StateVersion + committed, after.StateVersion);
                Assert.NotEqual(before!.PositionId, after.PositionId);
                if (stage == "stage-entry-movement-completion")
                    Assert.NotNull(journal.GetSelectionRoundDeadlineMs(request.CampaignId));
                if (stage == "combat-selection")
                    Assert.Null(journal.GetSelectionRoundDeadlineMs(request.CampaignId));
                Replay();
            }
        }
        async Task FinishSide(CampaignSeatCredential seat, string reserve, string completion, bool verify)
        {
            verify &= boundaryTurn is null || checkingBoundary;
            if (verify)
            {
                Assert.Equal(HttpStatusCode.Conflict, (await outward.OpeningResponse(seat, "land.second-reserve", "second-reserve")).Status);
                Assert.Equal(HttpStatusCode.Conflict, (await outward.OpeningResponse(seat, "land.reserve-designation", "reserve-designation")).Status);
            }
            await Play(seat, reserve, $"land.{reserve}", verify: verify);
            if (verify)
            {
                var movement = await QueryOpeningForOpenSeatTest(outward, seat, Element(seat), "stage-entry-movement");
                Assert.NotEmpty(movement.Refs);
                var checkpoint = CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId));
                var stage = checkpoint.PositionId.Contains("operation-3", StringComparison.Ordinal) ? 3 : 2;
                Assert.Equal(stage, movement.Visible.GetProperty("ownElements")[0].GetProperty("ledgerOperationStage").GetInt32());
                Assert.Equal(HttpStatusCode.Conflict, (await outward.OpeningResponse(Other(seat), Element(seat), "stage-entry-movement")).Status);
                Assert.Contains(await Discover(seat), entry => entry.OpeningStage == "stage-entry-movement");
            }
            await Play(seat, completion, $"land.{completion}", verify: verify);
            await Play(seat, "combat-selection", Element(seat), verify: verify);
            await Play(seat, "cycle-control", "land.cycle-control", verify: verify);
            await Play(seat, "truck-convoy-pass", "land.truck-convoy-pass", verify: verify);
            for (var i = 0; i < 4; i++)
                await Play(seat, "post-combat-pass", "land.post-combat-pass", verify: verify);
        }

        var holder = (await Discover(axis)).Any(entry => entry.OpeningStage == "initiative-order") ? axis : commonwealth;
        await Play(holder, "initiative-order", "land.initiative-order", last: false);
        await FinishSide(holder, "reserve-designation", "movement-segment-completion", false);
        await FinishSide(Other(holder), "second-reserve", "second-movement-completion", false);
        Replay();
        holder = (await Discover(axis)).Any(entry => entry.OpeningStage == "stage-entry-initiative-declaration") ? axis : commonwealth;
        await Play(holder, "stage-entry-initiative-declaration", "land.stage-entry-initiative-declaration", last: false, verify: true);
        await FinishSide(holder, "stage-entry-reserve", "stage-entry-movement-completion", true);
        Assert.Equal("land.position.operation-2.second-player.reserve-designation",
            CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).PositionId);
        await FinishSide(Other(holder), "stage-entry-reserve", "stage-entry-movement-completion", true);
        Assert.Equal("land.position.operation-3.initiative-declaration",
            CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).PositionId);
        holder = (await Discover(axis)).Any(entry => entry.OpeningStage == "stage-entry-initiative-declaration") ? axis : commonwealth;
        Assert.Equal("stage-entry-initiative-declaration", Assert.Single(await Discover(holder)).OpeningStage);
        Assert.Empty(await Discover(Other(holder)));
        foreach (var seat in new[] { axis, commonwealth })
            Assert.Equal(HttpStatusCode.Conflict, (await outward.OpeningResponse(seat, "land.stage-entry-reserve", "stage-entry-reserve")).Status);
        if (!includeStageThree) return;

        // Switch the first role at the next occurrence, so retained side/role aliases cannot pass.
        await Play(holder, "stage-entry-initiative-declaration", "land.stage-entry-initiative-declaration", verify: true);
        Assert.Equal("land.position.operation-3.weather-determination",
            CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).PositionId);
        var declarationHead = fixture.Open(clock).ReadVerified(request.CampaignId);
        var preamble = ReplayStageThreePreamblePrefixes(declarationHead);
        Assert.Equal("stage-entry-reserve", Assert.Single(await Discover(Other(holder))).OpeningStage);
        Assert.Empty(await Discover(holder));
        Assert.Equal(CampaignExercises.QueryCheckpoint(declarationHead).StateVersion,
            CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).StateVersion);
        await QueryOpeningForOpenSeatTest(outward, Other(holder), "land.stage-entry-reserve", "stage-entry-reserve");
        var afterPreamble = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(CampaignExercises.QueryCheckpoint(declarationHead).StateVersion + 5,
            CampaignExercises.QueryCheckpoint(afterPreamble).StateVersion);
        Assert.Equal(CampaignExercises.Reconstruct(preamble).EventStreamHash,
            CampaignExercises.Reconstruct(afterPreamble).EventStreamHash);
        Replay();
        await FinishSide(Other(holder), "stage-entry-reserve", "stage-entry-movement-completion", true);
        Assert.Equal("land.position.operation-3.second-player.reserve-designation",
            CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).PositionId);
        await FinishSide(holder, "stage-entry-reserve", "stage-entry-movement-completion", true);
        Assert.Equal("land.position.end-of-turn",
            CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).PositionId);
        Replay();
        if (boundaryTurn is null)
        {
            var beforeRefusal = CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId));
            foreach (var seat in new[] { axis, commonwealth })
            {
                var refused = await outward.OpenForSeatResponse(seat);
                Assert.Equal(HttpStatusCode.Conflict, refused.Status);
                Assert.Contains("rule-not-implemented", refused.Body);
            }
            Assert.Equal(beforeRefusal.StateVersion,
                CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).StateVersion);
            return;
        }

        checkingBoundary = true;
        var boundaryHead = fixture.Open(clock).ReadVerified(request.CampaignId);
        var declaration = ReplayTurnBoundaryPrefixes(boundaryHead, boundaryTurn.Value, beforeDeclaration: true);
        holder = (await Discover(axis)).Any(entry => entry.OpeningStage == "stage-entry-initiative-declaration") ? axis : commonwealth;
        Assert.Equal("stage-entry-initiative-declaration", Assert.Single(await Discover(holder)).OpeningStage);
        Assert.Empty(await Discover(Other(holder)));
        // Discovery previews; querying an opening persists the preceding System steps.
        await QueryOpeningForOpenSeatTest(outward, holder, "land.stage-entry-initiative-declaration", "stage-entry-initiative-declaration");
        var actualDeclaration = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(CampaignExercises.Reconstruct(declaration).EventStreamHash,
            CampaignExercises.Reconstruct(actualDeclaration).EventStreamHash);
        Replay();
        await Play(holder, "stage-entry-initiative-declaration", "land.stage-entry-initiative-declaration", verify: true);
        var declarationAccepted = fixture.Open(clock).ReadVerified(request.CampaignId);
        var reserve = ReplayTurnBoundaryPrefixes(declarationAccepted, boundaryTurn.Value, beforeDeclaration: false);
        Assert.Equal("stage-entry-reserve", Assert.Single(await Discover(Other(holder))).OpeningStage);
        Assert.Empty(await Discover(holder));
        var opening = await QueryOpeningForOpenSeatTest(outward, Other(holder), "land.stage-entry-reserve", "stage-entry-reserve");
        Assert.NotEmpty(opening.Refs);
        var actualReserve = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.Equal(CampaignExercises.Reconstruct(reserve).EventStreamHash,
            CampaignExercises.Reconstruct(actualReserve).EventStreamHash);
        Assert.Equal("land.position.operation-1.first-player.reserve-designation", CampaignExercises.QueryCheckpoint(actualReserve).PositionId);
        if (observationCheck is not null)
            await observationCheck(outward, Other(holder), CampaignExercises.QueryCheckpoint(actualReserve), boundaryTurn.Value + 1);
        Assert.Equal(HttpStatusCode.Conflict, (await outward.SubmitResponse(Other(holder), "land.stage-entry-reserve", "stage-entry-reserve", opening.Revision, opening.Refs[^1])).Status);
        Assert.Equal(CampaignExercises.QueryCheckpoint(actualReserve).StateVersion,
            CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(request.CampaignId)).StateVersion);
        Replay();
    }
}
