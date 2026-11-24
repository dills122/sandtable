using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.RecommendationConnector;
using OllamaSharp;

namespace Cna.OrleansHost.Tests;

// Local, manual verification of SecondReserveAdvisor against a real campaign, same discipline
// as the other advisor verifications: real Kestrel + real Orleans silo, real HTTP round trip.
// Second Reserve is the second acting side's own reserve designation, reached only after the
// first side's full chain (Initiative Order, Reserve Designation, Movement, Movement Segment
// Completion, Combat Selection, Cycle Control, Truck Convoy Pass, four Post-Combat passes) --
// this test drives every one of those steps for real over the outward HTTP API, reusing the
// exact sequence TwoRealHttpPlayersConductAnEntireOperationStageOne
// (EndToEndHttpRelease.cs) already proved reachable, rather than re-deriving it. Requires a
// local Ollama instance with qwen2.5:1.5b pulled; skipped otherwise so this never blocks CI.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact(Skip = "Manual local verification only -- requires a running local Ollama instance with qwen2.5:1.5b pulled. Confirmed working locally; not part of the automated suite since CI has neither.")]
    public async Task RecommendationConnectorGivesGroundedSecondReserveAdviceAgainstARealCampaign()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = CombatInfantryRequest();
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);

        await outward.CreateCampaign(request, Timing(request), OperatorSecret);

        var axis = await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret);
        var commonwealth = await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret);

        async Task<(long Revision, string[] Refs, JsonElement Visible)> Query(
            Cna.OrleansHost.Api.CampaignSeatCredential credential, string actor, string stage)
        {
            var (status, body) = await outward.OpeningResponse(credential, actor, stage);
            Assert.True(status == HttpStatusCode.OK, $"{stage} query for {actor} failed: {status} {body}");
            using var json = JsonDocument.Parse(body);
            var revision = json.RootElement.GetProperty("revision").GetInt64();
            var refs = json.RootElement.GetProperty("publicActionRefs").EnumerateArray()
                .Select(value => value.GetString()!).ToArray();
            using var visibleDoc = JsonDocument.Parse(Convert.FromBase64String(
                json.RootElement.GetProperty("visibleCutBase64").GetString()!));
            return (revision, refs, visibleDoc.RootElement.Clone());
        }

        async Task Submit(Cna.OrleansHost.Api.CampaignSeatCredential credential, string actor,
            string stage, long revision, string reference)
        {
            var (status, body) = await outward.SubmitResponse(credential, actor, stage, revision, reference);
            Assert.True(status == HttpStatusCode.OK, $"{stage} submit for {actor} failed: {status} {body}");
        }

        async Task<bool> Available(Cna.OrleansHost.Api.CampaignSeatCredential credential, string actor, string stage)
        {
            var (status, _) = await outward.OpeningResponse(credential, actor, stage);
            return status == HttpStatusCode.OK;
        }

        // 1. Initiative Order: find the winner, submit act-first.
        var winner = await Available(axis, "land.initiative-order", "initiative-order") ? axis : commonwealth;
        var (initRevision, initRefs, initVisible) = await Query(winner, "land.initiative-order", "initiative-order");
        var initChoices = initVisible.GetProperty("choices").EnumerateArray().Select(value => value.GetString()).ToArray();
        var actFirstIndex = Array.IndexOf(initChoices, "act-first");
        Assert.True(actFirstIndex >= 0);
        await Submit(winner, "land.initiative-order", "initiative-order", initRevision, initRefs[actFirstIndex]);

        // 2. Reserve Designation: finish immediately with nobody held back.
        var acting = await Available(axis, "land.reserve-designation", "reserve-designation") ? axis : commonwealth;
        var other = acting == axis ? commonwealth : axis;
        var (reserveRevision, reserveRefs, reserveVisible) = await Query(acting, "land.reserve-designation", "reserve-designation");
        var reserveKinds = reserveVisible.GetProperty("choices").EnumerateArray()
            .Select(value => value.GetProperty("kind").GetString()).ToArray();
        var completeReserveIndex = Array.IndexOf(reserveKinds, "complete");
        Assert.True(completeReserveIndex >= 0);
        await Submit(acting, "land.reserve-designation", "reserve-designation", reserveRevision, reserveRefs[completeReserveIndex]);

        var actingInfantry = acting == axis ? "axis-infantry" : "commonwealth-infantry";

        // 3. Movement: drain any real per-element moves, if offered.
        for (var moveIndex = 0; moveIndex < 15; moveIndex++)
        {
            var (moveStatus, moveBody) = await outward.OpeningResponse(acting, actingInfantry, "movement");
            if (moveStatus != HttpStatusCode.OK) break;
            using var moveJson = JsonDocument.Parse(moveBody);
            var moveRevisionLoop = moveJson.RootElement.GetProperty("revision").GetInt64();
            var moveRefsLoop = moveJson.RootElement.GetProperty("publicActionRefs").EnumerateArray()
                .Select(value => value.GetString()!).ToArray();
            if (moveRefsLoop.Length == 0) break;
            await Submit(acting, actingInfantry, "movement", moveRevisionLoop, moveRefsLoop[0]);
        }

        // 4. Movement Segment Completion.
        var (completeMoveRevision, completeMoveRefs, _) = await Query(acting, "land.movement-segment-completion", "movement-segment-completion");
        await Submit(acting, "land.movement-segment-completion", "movement-segment-completion",
            completeMoveRevision, completeMoveRefs[0]);

        // 5. Breakdown auto-resolves transparently (System audience, UX-001 auto-drain).

        // 6. First-side Combat Selection: finish without attack.
        var (combatRevision, combatRefs, _) = await Query(acting, actingInfantry, "combat-selection");
        await Submit(acting, actingInfantry, "combat-selection", combatRevision, combatRefs[^1]);

        // 7. Cycle Control: Finish.
        var (cycleRevision, cycleRefs, _) = await Query(acting, "land.cycle-control", "cycle-control");
        await Submit(acting, "land.cycle-control", "cycle-control", cycleRevision, cycleRefs[^1]);

        // 8. Truck Convoy Pass.
        var (convoyRevision, convoyRefs, _) = await Query(acting, "land.truck-convoy-pass", "truck-convoy-pass");
        await Submit(acting, "land.truck-convoy-pass", "truck-convoy-pass", convoyRevision, convoyRefs[0]);

        // 9. Four Post-Combat passes -- this is what finally opens the second side's Second
        // Reserve stage.
        for (var index = 0; index < 4; index++)
        {
            var (phaseRevision, phaseRefs, _) = await Query(acting, "land.post-combat-pass", "post-combat-pass");
            await Submit(acting, "land.post-combat-pass", "post-combat-pass", phaseRevision, phaseRefs[0]);
        }

        // 10. Second side: the real Second Reserve opening is now live. Ask the connector for
        // advice against it -- this advisor never calls /actions itself, only /openings.
        var audience = other == axis ? "axis" : "commonwealth";
        var connectorCredential = new CampaignSeatCredential(
            request.CampaignId, audience, other.Epoch, other.Secret, other.ExpiresUnixMs);

        using var ollama = new OllamaApiClient(new Uri("http://localhost:11434"), "qwen2.5:1.5b");
        using var outwardApiClient = new HttpClient { BaseAddress = outward.Address };
        outwardApiClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        var advisor = new SecondReserveAdvisor(outwardApiClient, ollama);

        var advice = await advisor.Advise(
            connectorCredential, "land.second-reserve", "second-reserve",
            TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(advice.RecommendedActionId));
        Assert.False(string.IsNullOrWhiteSpace(advice.Rationale));
    }
}
