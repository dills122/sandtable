using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Timeouts;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;

// Ephemeral, local-only operator secret: 32 random bytes as hex, generated fresh each run --
// never persisted or reused, just the credential this one throwaway journal file requires.
var operatorSecret = Convert.ToHexStringLower(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

static int FreePort()
{
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    return ((IPEndPoint)listener.LocalEndpoint).Port;
}

var journalPath = Path.Combine(Path.GetTempPath(), "sandtable-local-simulator-" + Guid.NewGuid().ToString("N") + ".sqlite");
var journal = CampaignDurableAuthorityJournal.CreateNew(
    journalPath, operatorSecret, contentResolver: Cna1979SyntheticContentResolver.Instance);
var store = CampaignDurableAuthorityJournalStore.ForSingleCampaign(journal);

var siloBuilder = Host.CreateApplicationBuilder();
siloBuilder.Services.AddSingleton(store);
siloBuilder.Services.AddSingleton<ICampaignRoundDeadlineSink, MartenCampaignRoundDeadlineSink>();
siloBuilder.Logging.SetMinimumLevel(LogLevel.Warning);
siloBuilder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering(siloPort: FreePort(), gatewayPort: 0);
    silo.AddReminders();
    silo.UseInMemoryReminderService();
});
var silo = siloBuilder.Build();
await silo.StartAsync();

var builder = WebApplication.CreateBuilder();
builder.Environment.EnvironmentName = "Development";
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
builder.Services.AddSingleton(store);
builder.Services.AddSingleton(silo.Services.GetRequiredService<IGrainFactory>());
var app = builder.Build();
app.MapDurableCampaignApi();
await app.StartAsync();
var address = app.Services.GetRequiredService<IServer>().Features
    .Get<IServerAddressesFeature>()!.Addresses.Single();

using var client = new HttpClient { BaseAddress = new Uri(address) };
client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");

// Same real outward-API shape OwnerHttpHarness (the xunit manual-verification harness) uses --
// the fixed combat-infantry rules-lab scenario that, on Stage 1, opens Initiative Order then
// Reserve Designation for whichever side's dice made it the opening's real holder.
var request = new CampaignCreationRequest(1,
    "campaign-local-simulator", Cna1979Ruleset.Manifest.Hash, 12345,
    "rules-lab.breakdown.reaction.combat-infantry.v1",
    "sha256:18734d3693747a35d855205f869917dc53dfdbdb9333ee32cb7764cbddaff3ac",
    "rules-lab.content.breakdown-reaction.combat-infantry.v1",
    "sha256:f6c8e7cbed61be1e99c877323471857b2d6b43a5bcfbe9475a59df9599e3498e",
    "breakdown-reaction-combat-infantry");
var timing = new CampaignDecisionTimingConfigurationV1("rules-lab.campaign-timing.1",
    request.RulesetHash, request.ContentHash,
    [new("custody", 30_000, "leave-unguarded"),
        new("cycle-control", 30_000, "finish-phase"),
        new("force-assignment", 30_000, "cancel-incomplete-voluntary-round"),
        new("rba", 30_000, "cancel-without-decline"),
        new("reserve-release", 30_000, "convert-unresolved-i-retain-ii"),
        new("retreat", 30_000, "refuse-retreat"),
        new("selection", 30_000, "no-selection")]);

using var timingJson = JsonDocument.Parse(timing.SerializeCanonical());
var createBody = JsonSerializer.Serialize(new
{
    contractVersion = 2,
    campaignId = request.CampaignId,
    rulesetHash = request.RulesetHash,
    seed = request.Seed,
    setupId = request.SetupId,
    setupHash = request.SetupHash,
    contentPackId = request.ContentPackId,
    contentHash = request.ContentHash,
    scenarioId = request.ScenarioId,
    timing = timingJson.RootElement,
});
using var createMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v2/campaigns/")
{ Content = new StringContent(createBody, Encoding.UTF8, "application/json") };
createMessage.Headers.Add("X-Sandtable-Operator", operatorSecret);
using var createResponse = await client.SendAsync(createMessage);
createResponse.EnsureSuccessStatusCode();

async Task<JsonElement> IssueAndRedeemSeat(string side)
{
    using var grantMessage = new HttpRequestMessage(HttpMethod.Post,
        $"/api/v2/campaigns/{request.CampaignId}/grants/{side}")
    { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
    grantMessage.Headers.Add("X-Sandtable-Operator", operatorSecret);
    using var grantResponse = await client.SendAsync(grantMessage);
    grantResponse.EnsureSuccessStatusCode();
    using var grant = JsonDocument.Parse(await grantResponse.Content.ReadAsStringAsync());
    var seatBody = "{\"contractVersion\":2,\"grant\":" + grant.RootElement.GetProperty("grant").GetRawText() + "}";
    using var seatResponse = await client.PostAsync($"/api/v2/campaigns/{request.CampaignId}/seats",
        new StringContent(seatBody, Encoding.UTF8, "application/json"));
    seatResponse.EnsureSuccessStatusCode();
    using var credentialJson = JsonDocument.Parse(await seatResponse.Content.ReadAsStringAsync());
    return credentialJson.RootElement.GetProperty("credential").Clone();
}

var axisCredential = await IssueAndRedeemSeat("axis");
var commonwealthCredential = await IssueAndRedeemSeat("commonwealth");

Console.WriteLine("Local campaign simulator is live.");
Console.WriteLine($"Outward API base address: {address}");
Console.WriteLine($"Campaign id: {request.CampaignId}");
Console.WriteLine("Axis seat credential (connector AdviceRequest shape -- audience/epoch/secret/expiresUnixMs):");
Console.WriteLine(axisCredential.GetRawText());
Console.WriteLine("Commonwealth seat credential:");
Console.WriteLine(commonwealthCredential.GetRawText());
Console.WriteLine("Point Cna.RecommendationConnector's OutwardApi:BaseUrl at the address above, then");
Console.WriteLine("POST /advise/initiative-order and /advise/reserve-designation at it for a live run.");
Console.WriteLine("Press Ctrl+C to stop this simulator.");

await app.WaitForShutdownAsync();
await silo.StopAsync();
