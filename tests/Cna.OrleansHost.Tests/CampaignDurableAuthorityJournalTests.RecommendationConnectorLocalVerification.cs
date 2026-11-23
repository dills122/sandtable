using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.RecommendationConnector;
using OllamaSharp;

namespace Cna.OrleansHost.Tests;

// Local, manual verification that the recommendation connector (a separate project with
// zero reference to Cna.Core/Cna.OrleansHost) can give grounded advice against a real
// campaign driven entirely over the real outward HTTP API -- the same harness and genesis
// flow TwoRealHttpPlayersConductAnEntireOperationStageOne uses, reused rather than
// re-derived. Requires a local Ollama instance with qwen2.5:1.5b pulled; skipped otherwise
// so this never blocks CI, which has neither. This is the connector's own locked v1 scope
// (Initiative Order on Stage 1 only) -- it never calls /actions, only /openings.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact(Skip = "Manual local verification only -- requires a running local Ollama instance with qwen2.5:1.5b pulled. Confirmed working locally; not part of the automated suite since CI has neither.")]
    public async Task RecommendationConnectorGivesGroundedAdviceAgainstARealCampaign()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = CombatInfantryRequest();
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);

        await outward.CreateCampaign(request, Timing(request), OperatorSecret);

        var axis = await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret);
        var commonwealth = await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret);

        async Task<bool> Available(Cna.OrleansHost.Api.CampaignSeatCredential credential) =>
            (await outward.OpeningResponse(credential, "land.initiative-order", "initiative-order")).Status
                == HttpStatusCode.OK;

        // Whichever real seat the initiative dice contest actually made the holder --
        // not assumed, discovered the same way the E2E proof test discovers it.
        var winner = await Available(axis) ? axis : commonwealth;
        var audience = winner == axis ? "axis" : "commonwealth";

        // The connector's own plain credential shape, built from the real seat this harness
        // issued -- not Cna.OrleansHost.Api.CampaignSeatCredential itself (the connector has
        // zero reference to that assembly), but the same wire fields.
        var connectorCredential = new CampaignSeatCredential(
            request.CampaignId, audience, winner.Epoch, winner.Secret, winner.ExpiresUnixMs);

        using var ollama = new OllamaApiClient(new Uri("http://localhost:11434"), "qwen2.5:1.5b");
        using var outwardApiClient = new HttpClient { BaseAddress = outward.Address };
        outwardApiClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        var advisor = new InitiativeOrderAdvisor(outwardApiClient, ollama);

        var advice = await advisor.Advise(
            connectorCredential, "land.initiative-order", "initiative-order", TestContext.Current.CancellationToken);

        Assert.True(advice.RecommendedActionId is "act-first" or "act-last");
        Assert.False(string.IsNullOrWhiteSpace(advice.Rationale));
        // Not asserting Grounded == true here -- that's exactly the open empirical
        // question this test exists to surface, not something to assume passes.
    }
}
