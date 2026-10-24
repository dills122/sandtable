using System.Net;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.OrleansHost;
using Cna.OrleansHost.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Cna.OrleansHost.Tests;

public sealed class CampaignDurableOutwardApiTests
{
    private const string OperatorSecret =
        "bb2ba979667134402906109cd92a6cf68427d84b58c30db5bcd0fae8bef692b1";

    [Fact]
    public async Task NormalCampaignRouteCreatesRedeemsAndOpensOnlyForCurrentSeat()
    {
        var directory = Path.Combine(Path.GetTempPath(), "sandtable-outward-" + Guid.NewGuid().ToString("N"));
        var journal = CampaignDurableAuthorityJournal.CreateNew(
            Path.Combine(directory, "campaign.sqlite"), OperatorSecret);
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Development";
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(journal);
        await using var app = builder.Build();
        app.MapDurableCampaignApi();
        await app.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            var request = new CampaignCreationRequest(1, "campaign-durable-route",
                Cna1979Ruleset.Manifest.Hash, 12345,
                "rules-lab.breakdown.reaction.v1",
                "sha256:ed4e33358ede11f3c25661798add9088f11d8853053dbf7c8992ba5f92d9a36e",
                "rules-lab.content.breakdown-reaction.v1",
                "sha256:1e10648bb65f7ec6d4ad3f33bbf612f51c11a927ebc2e8b110aa5544af3918dd",
                "breakdown-reaction-lab");
            var timing = new CampaignDecisionTimingConfigurationV1(
                "rules-lab.campaign-timing.1", request.RulesetHash, request.ContentHash,
                [new("custody", 30_000, "leave-unguarded"),
                    new("cycle-control", 30_000, "finish-phase"),
                    new("force-assignment", 30_000, "cancel-incomplete-voluntary-round"),
                    new("rba", 30_000, "cancel-without-decline"),
                    new("reserve-release", 30_000, "convert-unresolved-i-retain-ii"),
                    new("retreat", 30_000, "refuse-retreat"),
                    new("selection", 30_000, "no-selection")]);
            using var timingJson = JsonDocument.Parse(timing.SerializeCanonical());
            var creation = JsonSerializer.Serialize(new
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
            var denied = await client.PostAsync("/api/v2/campaigns/",
                Body(creation), TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            using (var crossOrigin = new HttpRequestMessage(HttpMethod.Post,
                "/api/v2/campaigns/")
            { Content = Body(creation) })
            {
                crossOrigin.Headers.Add("Origin", "https://untrusted.example");
                crossOrigin.Headers.Add("X-Sandtable-Operator", OperatorSecret);
                Assert.Equal(HttpStatusCode.Forbidden,
                    (await client.SendAsync(crossOrigin,
                        TestContext.Current.CancellationToken)).StatusCode);
            }
            var created = await Send(client, "/api/v2/campaigns/", creation, OperatorSecret);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var creationAck = await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var beforeRetry = RetainedAuthority(Path.Combine(directory, "campaign.sqlite"));
            var createdAgain = await Send(client, "/api/v2/campaigns/", creation, OperatorSecret);
            Assert.Equal(HttpStatusCode.Created, createdAgain.StatusCode);
            Assert.Equal(creationAck, await createdAgain.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
            var creationConflict = await Send(client, "/api/v2/campaigns/",
                creation.Replace("12345", "12346", StringComparison.Ordinal), OperatorSecret);
            Assert.Equal(HttpStatusCode.Conflict, creationConflict.StatusCode);
            var unauthorizedRetry = await client.PostAsync("/api/v2/campaigns/", Body(creation),
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedRetry.StatusCode);
            Assert.Equal(beforeRetry, RetainedAuthority(Path.Combine(directory, "campaign.sqlite")));
            var session = journal.ReadVerified(request.CampaignId);
            for (var index = 0; index < 20; index++)
            {
                var set = Enum.GetValues<CampaignActionAudience>()
                    .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                    .First(value => value.Candidates.Count > 0);
                if (set.Candidates.OfType<MoveElementAction>().Any()) break;
                var next = set.Candidates.FirstOrDefault(value => value is ActFirstAction)
                    ?? set.Candidates.FirstOrDefault(value =>
                        value is CompleteReserveDesignationAction)
                    ?? set.Candidates[0];
                var command = new CampaignActionSubmission(1, set.CampaignId,
                    set.StateVersion, set.PositionId, set.Audience, next.ActionId);
                var progressed = journal.Append(request.CampaignId, command);
                Assert.True(progressed.IsAccepted);
                session = progressed.SuccessorSession!;
            }
            var advancedAuthority = RetainedAuthority(Path.Combine(directory, "campaign.sqlite"));
            var advancedRetry = await Send(client, "/api/v2/campaigns/", creation, OperatorSecret);
            Assert.Equal(HttpStatusCode.Created, advancedRetry.StatusCode);
            Assert.Equal(creationAck, await advancedRetry.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
            Assert.Equal(advancedAuthority, RetainedAuthority(Path.Combine(directory, "campaign.sqlite")));
            var element = CampaignHostAdapter.QueryPrivateSideCut(session,
                CampaignActionAudience.Axis)!.OwnElements[0].ElementId;

            var grantResponse = await Send(client,
                $"/api/v2/campaigns/{request.CampaignId}/grants/axis", "{}", OperatorSecret);
            Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode);
            using var grant = JsonDocument.Parse(await grantResponse.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
            var redeem = await Send(client, $"/api/v2/campaigns/{request.CampaignId}/seats",
                "{\"contractVersion\":2,\"grant\":"
                + grant.RootElement.GetProperty("grant").GetRawText() + "}");
            Assert.Equal(HttpStatusCode.OK, redeem.StatusCode);
            using var credential = JsonDocument.Parse(await redeem.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
            var openingBody = "{\"contractVersion\":2,\"credential\":"
                + credential.RootElement.GetProperty("credential").GetRawText()
                + ",\"stage\":\"movement\",\"actingElementId\":"
                + JsonSerializer.Serialize(element) + "}";
            var opening = await Send(client,
                $"/api/v2/campaigns/{request.CampaignId}/openings", openingBody);
            Assert.Equal(HttpStatusCode.OK, opening.StatusCode);
            using var opened = JsonDocument.Parse(await opening.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
            Assert.NotEmpty(Convert.FromBase64String(opened.RootElement
                .GetProperty("visibleCutBase64").GetString()!));
            Assert.True(opened.RootElement.GetProperty("revision").GetInt64() > 0);
            var references = opened.RootElement.GetProperty("publicActionRefs");
            Assert.NotEmpty(references.EnumerateArray());
            var invalidAction = JsonSerializer.Serialize(new
            {
                contractVersion = 2,
                credential = credential.RootElement.GetProperty("credential"),
                stage = "movement",
                actingElementId = element,
                openingRevision = opened.RootElement.GetProperty("revision").GetInt64(),
                publicActionRef = "forged",
            });
            var rejected = await Send(client,
                $"/api/v2/campaigns/{request.CampaignId}/actions", invalidAction);
            Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
            Assert.DoesNotContain("authority", await rejected.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken), StringComparison.OrdinalIgnoreCase);
            var acceptedAction = JsonSerializer.Serialize(new
            {
                contractVersion = 2,
                credential = credential.RootElement.GetProperty("credential"),
                stage = "movement",
                actingElementId = element,
                openingRevision = opened.RootElement.GetProperty("revision").GetInt64(),
                publicActionRef = references[0].GetString(),
            });
            var accepted = await Send(client,
                $"/api/v2/campaigns/{request.CampaignId}/actions", acceptedAction);
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            var visibleReceipt = await accepted.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);
            Assert.DoesNotContain("actionId", visibleReceipt, StringComparison.OrdinalIgnoreCase);
            var retry = await Send(client,
                $"/api/v2/campaigns/{request.CampaignId}/actions", acceptedAction);
            Assert.Equal(visibleReceipt, await retry.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));

            var replaced = await Send(client,
                $"/api/v2/campaigns/{request.CampaignId}/grants/axis", "{}", OperatorSecret);
            Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);
            var stale = await Send(client,
                $"/api/v2/campaigns/{request.CampaignId}/openings", openingBody);
            Assert.Equal(HttpStatusCode.Unauthorized, stale.StatusCode);
        }
        finally
        {
            await app.StopAsync(TestContext.Current.CancellationToken);
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string[] RetainedAuthority(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT head_sequence,head_hash,hex(creation_event),hex(initial_snapshot),"
            + "hex(initial_contacts),hex(timing_configuration),"
            + "(SELECT COUNT(*) FROM accepted_steps) FROM campaigns";
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        return Enumerable.Range(0, reader.FieldCount).Select(index =>
            Convert.ToString(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture)!).ToArray();
    }

    private static StringContent Body(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<HttpResponseMessage> Send(HttpClient client, string path,
        string json, string? operatorSecret = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = Body(json) };
        if (operatorSecret is not null)
            request.Headers.Add("X-Sandtable-Operator", operatorSecret);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
