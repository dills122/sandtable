using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Cna.Tests.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cna.OrleansHost.Tests;

public sealed class GrazianiSourceJournalTests
{
    private const string Secret = "bb2ba979667134402906109cd92a6cf68427d84b58c30db5bcd0fae8bef692b1";

    [Fact]
    public async Task SourceExcerptSurvivesNormalJournalAndFreshProcessWithoutOpeningPlay()
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-source-journal-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        try
        {
            var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
            var content = GrazianiSourceExcerpt.Build();
            var init = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
            var request = new CampaignCreationRequest(CampaignSourceCreation.RequestVersion, "graziani-excerpt-journal",
                Cna1979Ruleset.Manifest.Hash, 1940, CampaignSourceCreation.SetupId,
                CampaignSourceCreation.CalculateSetupHash(content.Identity, GrazianiSourceExcerpt.ScenarioId, init),
                content.Identity.PackId, content.Identity.Hash, GrazianiSourceExcerpt.ScenarioId);
            var concurrent = Enumerable.Range(0, 4)
                .Select(_ => Task.Run(() => journal.CreateCampaign(request, content, init)))
                .ToArray();
            var created = await Task.WhenAll(concurrent);
            Assert.All(created, session => Assert.Equal(request.ContentHash, session.Request.ContentHash));
            journal.CreateCampaign(request, content, init);
            var reopened = CampaignDurableAuthorityJournal.OpenExisting(path).ReadSourceVerified(request.CampaignId);
            Assert.Equal(content.Identity.Hash, reopened.Request.ContentHash);
            Assert.Equal(new[] { Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId,
                    Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId }.Order(StringComparer.Ordinal),
                reopened.PendingArrivalGroupIds);
            Assert.Throws<InvalidOperationException>(() => journal.ReadVerified(request.CampaignId));
            Assert.Throws<InvalidOperationException>(() => journal.IssueSeatGrant(request.CampaignId, CampaignActionAudience.Axis, Secret));
            var changed = new CampaignCreationRequest(request.ContractVersion, request.CampaignId, request.RulesetHash,
                request.Seed + 1, request.SetupId, request.SetupHash, request.ContentPackId, request.ContentHash, request.ScenarioId);
            Assert.Throws<InvalidOperationException>(() => journal.CreateCampaign(changed, content, init));
            using (var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT (SELECT count(*) FROM campaigns), (SELECT count(*) FROM accepted_steps)";
                using var rows = command.ExecuteReader();
                Assert.True(rows.Read());
                Assert.Equal(1, rows.GetInt32(0));
                Assert.Equal(0, rows.GetInt32(1));
            }
            var child = await ReadInFreshProcess(path, request.CampaignId);
            Assert.NotEqual(Environment.ProcessId, child.ProcessId);
            Assert.Equal(request.ContentHash, child.ContentHash);
            Assert.Equal(reopened.PendingArrivalGroupIds, child.PendingGroupIds);
            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                var grantSecret = new string('a', 64);
                var digestMethod = typeof(CampaignDurableAuthorityJournal).GetMethod("CredentialDigest", BindingFlags.NonPublic | BindingFlags.Static)!;
                var digest = (byte[])digestMethod.Invoke(null, ["seat-grant", grantSecret])!;
                command.CommandText = "INSERT INTO seats VALUES ($id,'axis',1,$digest,$expires,NULL,NULL)";
                command.Parameters.AddWithValue("$id", request.CampaignId);
                command.Parameters.AddWithValue("$digest", digest);
                command.Parameters.AddWithValue("$expires", long.MaxValue);
                Assert.Equal(1, command.ExecuteNonQuery());
                Assert.Throws<InvalidOperationException>(() => journal.ReadSourceVerified(request.CampaignId));
                Assert.Null(journal.RedeemSeatGrant(new CampaignSeatGrant(request.CampaignId,
                    CampaignActionAudience.Axis, 1, grantSecret, long.MaxValue)));
                command.CommandText = "DELETE FROM seats WHERE campaign_id=$id";
                Assert.Equal(1, command.ExecuteNonQuery());
                command.CommandText = """
                    INSERT INTO force_rounds VALUES ($id,'decision','decline','configuration',0,1,0,'collecting','hash')
                    """;
                Assert.Equal(1, command.ExecuteNonQuery());
                Assert.Throws<InvalidOperationException>(() => journal.ReadSourceVerified(request.CampaignId));
                command.CommandText = "DELETE FROM force_rounds WHERE campaign_id=$id";
                Assert.Equal(1, command.ExecuteNonQuery());
            }
            Assert.Equal(2, journal.ReadSourceVerified(request.CampaignId).PendingArrivalGroupIds.Count);
            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var query = connection.CreateCommand();
                query.CommandText = "SELECT creation_event,initial_contacts,timing_configuration FROM campaigns WHERE campaign_id=$id";
                query.Parameters.AddWithValue("$id", request.CampaignId);
                byte[] creation, contacts, timing;
                using (var row = query.ExecuteReader())
                {
                    Assert.True(row.Read());
                    creation = (byte[])row.GetValue(0);
                    contacts = (byte[])row.GetValue(1);
                    timing = (byte[])row.GetValue(2);
                }
                var forged = new byte[] { 123, 125 };
                var hashMethod = typeof(CampaignDurableAuthorityJournal).GetMethod("InitialHash", BindingFlags.NonPublic | BindingFlags.Static)!;
                var forgedHash = (string)hashMethod.Invoke(null, [request, creation, forged, contacts, timing, false])!;
                using var command = connection.CreateCommand();
                command.CommandText = "UPDATE campaigns SET initial_snapshot=$snapshot,head_hash=$hash WHERE campaign_id=$id";
                command.Parameters.AddWithValue("$snapshot", forged);
                command.Parameters.AddWithValue("$hash", forgedHash);
                command.Parameters.AddWithValue("$id", request.CampaignId);
                Assert.Equal(1, command.ExecuteNonQuery());
            }
            Assert.Throws<InvalidOperationException>(() =>
                CampaignDurableAuthorityJournal.OpenExisting(path).ReadSourceVerified(request.CampaignId));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task OperatorCanInitializeSourceExcerptButCannotGrantPlayerAccess()
    {
        var directory = Path.Combine(Path.GetTempPath(), "graziani-source-api-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.sqlite");
        var journal = CampaignDurableAuthorityJournal.CreateNew(path, Secret);
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Development";
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(journal);
        await using var app = builder.Build();
        app.MapDurableCampaignApi();
        await app.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            // Not a same-origin-gate test (besides the explicit crossOrigin case below);
            // simulate a genuine same-origin fetch so the v2 middleware's stricter
            // header-presence check (#319) doesn't block the other requests here.
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            var content = GrazianiSourceExcerpt.Build();
            var body = new
            {
                contractVersion = 2,
                campaignId = "graziani-source-api",
                rulesetHash = Cna1979Ruleset.Manifest.Hash,
                seed = 1940,
                scenarioId = GrazianiSourceExcerpt.ScenarioId,
                expectedContentHash = content.Identity.Hash,
                contentBase64 = Convert.ToBase64String(content.CanonicalBytes),
                capabilityNumerator = 0,
                capabilityDenominator = 1,
                cohesion = 0
            };
            var denied = await client.PostAsJsonAsync("/api/v2/campaigns/source", body, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            client.DefaultRequestHeaders.Add("X-Sandtable-Operator", Secret);
            using (var crossOrigin = new HttpRequestMessage(HttpMethod.Post, "/api/v2/campaigns/source")
            { Content = JsonContent.Create(body) })
            {
                crossOrigin.Headers.Add("Origin", "https://untrusted.example");
                Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(crossOrigin,
                    TestContext.Current.CancellationToken)).StatusCode);
            }
            var badHash = new
            {
                body.contractVersion,
                campaignId = "wrong-content-hash",
                body.rulesetHash,
                body.seed,
                body.scenarioId,
                expectedContentHash = "sha256:" + new string('0', 64),
                body.contentBase64,
                body.capabilityNumerator,
                body.capabilityDenominator,
                body.cohesion
            };
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v2/campaigns/source", badHash,
                TestContext.Current.CancellationToken)).StatusCode);
            Assert.Throws<InvalidOperationException>(() => journal.ReadSourceVerified("wrong-content-hash"));
            var created = await client.PostAsJsonAsync("/api/v2/campaigns/source", body, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var ack = JsonDocument.Parse(await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal("initialization-only", ack.RootElement.GetProperty("status").GetString());
            var state = await client.GetAsync("/api/v2/campaigns/graziani-source-api/source-initialization", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, state.StatusCode);
            var text = await state.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain(Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId, text);
            Assert.DoesNotContain(Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId, text);
            using var status = JsonDocument.Parse(text);
            Assert.Equal(2, status.RootElement.GetProperty("pendingGroupCount").GetInt32());
            var grant = await client.PostAsJsonAsync("/api/v2/campaigns/graziani-source-api/grants/axis", new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Conflict, grant.StatusCode);
            client.DefaultRequestHeaders.Remove("X-Sandtable-Operator");
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v2/campaigns/graziani-source-api/source-initialization", TestContext.Current.CancellationToken)).StatusCode);
        }
        finally
        {
            await app.StopAsync(TestContext.Current.CancellationToken);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static async Task<ChildResult> ReadInFreshProcess(string path, string campaignId)
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Name;
        var child = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "Cna.OrleansHost.FoundationChild", configuration, "Cna.OrleansHost.FoundationChild.dll"));
        Assert.True(File.Exists(child), $"Built child missing: {child}");
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(child);
        start.ArgumentList.Add("--source-campaign-read");
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start replay child.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new { Path = path, CampaignId = campaignId }));
            process.StandardInput.Close();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            await process.WaitForExitAsync(timeout.Token);
            var output = await stdout;
            Assert.True(process.ExitCode == 0, $"Replay child failed: {await stderr}");
            var result = JsonSerializer.Deserialize<ChildResult>(output)!;
            Assert.Equal(process.Id, result.ProcessId);
            return result;
        }
        finally { if (!process.HasExited) process.Kill(true); }
    }

    private sealed record ChildResult(int ProcessId, string CampaignId, string ContentHash, string[] PendingGroupIds);
}
