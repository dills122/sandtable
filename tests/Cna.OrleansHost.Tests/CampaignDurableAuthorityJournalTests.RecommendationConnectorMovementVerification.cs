using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.RecommendationConnector;
using OllamaSharp;

namespace Cna.OrleansHost.Tests;

// Local, manual verification of MovementAdvisor against a real campaign, same discipline as
// the Initiative Order and Reserve Designation verifications: real Kestrel + real Orleans
// silo, real HTTP round trip, advanced past Initiative Order and Reserve Designation (both
// submitted for real, over the real outward API) to reach the real Movement opening. Requires
// a local Ollama instance with qwen2.5:1.5b pulled; skipped otherwise so this never blocks CI.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact(Skip = "Manual local verification only -- requires a running local Ollama instance with qwen2.5:1.5b pulled. Confirmed working locally; not part of the automated suite since CI has neither.")]
    public async Task RecommendationConnectorGivesGroundedMovementAdviceAgainstARealCampaign()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = CombatInfantryRequest();
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);

        await outward.CreateCampaign(request, Timing(request), OperatorSecret);

        var axis = await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret);
        var commonwealth = await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret);

        async Task<bool> OpeningAvailable(Cna.OrleansHost.Api.CampaignSeatCredential credential, string actor, string stage) =>
            (await outward.OpeningResponse(credential, actor, stage)).Status == HttpStatusCode.OK;

        var winner = await OpeningAvailable(axis, "land.initiative-order", "initiative-order") ? axis : commonwealth;

        var (initStatus, initBody) = await outward.OpeningResponse(winner, "land.initiative-order", "initiative-order");
        Assert.Equal(HttpStatusCode.OK, initStatus);
        using var initJson = JsonDocument.Parse(initBody);
        using var initVisible = JsonDocument.Parse(Convert.FromBase64String(
            initJson.RootElement.GetProperty("visibleCutBase64").GetString()!));
        var initChoices = initVisible.RootElement.GetProperty("choices").EnumerateArray()
            .Select(value => value.GetString()).ToArray();
        var initRefs = initJson.RootElement.GetProperty("publicActionRefs").EnumerateArray()
            .Select(value => value.GetString()!).ToArray();
        var actFirstIndex = Array.IndexOf(initChoices, "act-first");
        Assert.True(actFirstIndex >= 0);
        await outward.SubmitResponse(winner, "land.initiative-order", "initiative-order",
            initJson.RootElement.GetProperty("revision").GetInt64(), initRefs[actFirstIndex]);

        var acting = await OpeningAvailable(axis, "land.reserve-designation", "reserve-designation")
            ? axis : commonwealth;

        var (reserveStatus, reserveBody) = await outward.OpeningResponse(
            acting, "land.reserve-designation", "reserve-designation");
        Assert.Equal(HttpStatusCode.OK, reserveStatus);
        using var reserveJson = JsonDocument.Parse(reserveBody);
        var reserveRefs = reserveJson.RootElement.GetProperty("publicActionRefs").EnumerateArray()
            .Select(value => value.GetString()!).ToArray();
        await outward.SubmitResponse(acting, "land.reserve-designation", "reserve-designation",
            reserveJson.RootElement.GetProperty("revision").GetInt64(), reserveRefs[0]);

        var actingInfantry = acting == axis ? "axis-infantry" : "commonwealth-infantry";
        var audience = acting == axis ? "axis" : "commonwealth";
        var connectorCredential = new CampaignSeatCredential(
            request.CampaignId, audience, acting.Epoch, acting.Secret, acting.ExpiresUnixMs);

        using var ollama = new OllamaApiClient(new Uri("http://localhost:11434"), "qwen2.5:1.5b");
        using var outwardApiClient = new HttpClient { BaseAddress = outward.Address };
        outwardApiClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        var advisor = new MovementAdvisor(outwardApiClient, ollama);

        var advice = await advisor.Advise(
            connectorCredential, actingInfantry, "movement", TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(advice.RecommendedActionId));
        Assert.False(string.IsNullOrWhiteSpace(advice.Rationale));
    }
}
