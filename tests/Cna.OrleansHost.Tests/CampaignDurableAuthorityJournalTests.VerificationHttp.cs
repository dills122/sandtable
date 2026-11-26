using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Tests.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Orleans;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public async Task VerificationHttpIsGatedReadOnlyAndDetectsChangedHistory()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = Request();
        var session = journal.CreateCampaign(request);
        var seat = journal.RedeemSeatGrant(journal.IssueSeatGrant(request.CampaignId,
            CampaignActionAudience.Axis, OperatorSecret))!;
        var store = CampaignDurableAuthorityJournalStore.ForSingleCampaign(journal);
        var silo = await StartOutwardApiSiloAsync(store);
        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.Environment.EnvironmentName = "Development";
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
            builder.Services.AddSingleton(store);
            builder.Services.AddSingleton(silo.Services.GetRequiredService<IGrainFactory>());
            await using var app = builder.Build();
            // Simulate a remote peer before the existing group middleware checks the connection.
            app.Use(async (context, next) =>
            {
                if (context.Request.Headers.ContainsKey("X-Test-Remote"))
                    context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.1");
                await next(context);
            });
            app.MapDurableCampaignApi();
            await app.StartAsync(TestContext.Current.CancellationToken);
            using var client = new HttpClient
            {
                BaseAddress = new Uri(app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single())
            };
            var path = $"/api/v2/campaigns/{request.CampaignId}";
            Assert.Equal(HttpStatusCode.Forbidden, (await Verify(OperatorSecret, sameOrigin: false)).Status);
            Assert.Equal(HttpStatusCode.Forbidden, (await Verify(OperatorSecret, remote: true)).Status);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Verify(null)).Status);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Verify("wrong")).Status);

            var events = await fixture.ReadCanonicalCheckpointEvents(request.CampaignId);
            var discovery = await Discover();
            var first = await Verify(OperatorSecret);
            Assert.Equal(HttpStatusCode.OK, first.Status);
            Assert.Equal(first, await Verify(OperatorSecret));
            Assert.Equal(events, await fixture.ReadCanonicalCheckpointEvents(request.CampaignId));
            Assert.Equal(discovery, await Discover());
            using var initial = JsonDocument.Parse(first.Body);
            Assert.True(initial.RootElement.GetProperty("isVerified").GetBoolean());
            Assert.Equal(events.Length, initial.RootElement.GetProperty("eventCount").GetInt32());
            Assert.Equal(events.Sum(e => e.Length), initial.RootElement.GetProperty("canonicalByteCount").GetInt64());
            Assert.Equal(CampaignExercises.Reconstruct(session).ReconstructedSnapshotHash,
                initial.RootElement.GetProperty("reconstructionHash").GetString());
            Assert.Equal(CampaignExercises.QueryCheckpoint(session).PositionId, initial.RootElement.GetProperty("positionId").GetString());
            Assert.Equal(1, initial.RootElement.GetProperty("gameTurn").GetInt32());

            var actions = CampaignExercises.Query(session, CampaignActionAudience.System).ActionSet!;
            Assert.True(journal.Append(request.CampaignId, new CampaignActionSubmission(1, request.CampaignId,
                actions.StateVersion, actions.PositionId, CampaignActionAudience.System, actions.Candidates.Single().ActionId)).IsAccepted);
            var after = await Verify(OperatorSecret);
            Assert.Equal(HttpStatusCode.OK, after.Status);
            using var changed = JsonDocument.Parse(after.Body);
            Assert.True(changed.RootElement.GetProperty("isVerified").GetBoolean());
            Assert.NotEqual(initial.RootElement.GetProperty("chainHeadHash").GetString(), changed.RootElement.GetProperty("chainHeadHash").GetString());
            Assert.NotEqual(initial.RootElement.GetProperty("reconstructionHash").GetString(), changed.RootElement.GetProperty("reconstructionHash").GetString());
            Assert.True(changed.RootElement.GetProperty("eventCount").GetInt32() > events.Length);

            // Use B6's one-byte canonical evidence alteration on the retained stream,
            // rather than altering an in-memory copy after verified load.
            if (!JournalFixture.UsesMarten)
            {
                var altered = (await fixture.ReadCanonicalCheckpointEvents(request.CampaignId))[^1].ToArray();
                var marker = "\"worldHash\":\"sha256:"u8.ToArray();
                var index = altered.AsSpan().IndexOf(marker);
                if (index < 0)
                {
                    marker = "\"eventType\":\""u8.ToArray();
                    index = altered.AsSpan().IndexOf(marker);
                }
                Assert.True(index >= 0);
                index += marker.Length;
                altered[index] = altered[index] == (byte)'0' ? (byte)'1' : (byte)'0';
                using var connection = new SqliteConnection($"Data Source={fixture.Path}");
                connection.Open();
                using var update = connection.CreateCommand();
                update.CommandText = "UPDATE accepted_steps SET canonical_event=$event";
                update.Parameters.AddWithValue("$event", altered);
                Assert.Equal(1, update.ExecuteNonQuery());
                var corrupt = await Verify(OperatorSecret);
                Assert.Equal(HttpStatusCode.OK, corrupt.Status);
                using var invalid = JsonDocument.Parse(corrupt.Body);
                Assert.False(invalid.RootElement.GetProperty("isVerified").GetBoolean());
            }

            async Task<(HttpStatusCode Status, string Body)> Verify(string? secret, bool sameOrigin = true, bool remote = false)
            {
                using var message = new HttpRequestMessage(HttpMethod.Get, path + "/verification");
                if (secret is not null) message.Headers.Add("X-Sandtable-Operator", secret);
                if (sameOrigin) message.Headers.Add("Sec-Fetch-Site", "same-origin");
                if (remote) message.Headers.Add("X-Test-Remote", "test");
                using var response = await client.SendAsync(message, TestContext.Current.CancellationToken);
                return (response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            }

            async Task<string> Discover()
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, path + "/openings/open")
                {
                    Content = JsonContent.Create(new
                    {
                        contractVersion = 2,
                        credential = new
                        {
                            campaignId = seat.CampaignId,
                            audience = "axis",
                            epoch = seat.Epoch,
                            secret = seat.Secret,
                            expiresUnixMs = seat.ExpiresUnixMs,
                        }
                    }),
                };
                message.Headers.Add("Sec-Fetch-Site", "same-origin");
                using var response = await client.SendAsync(message, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            }
        }
        finally { await StopOutwardApiSiloAsync(silo); }
    }

    [Fact]
    public async Task VerificationReadMartenRejectsMalformedSequence()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await SharedPostgres.CreateDatabaseAsync();
        using var documentStore = SharedPostgres.CreateCampaignStore(connectionString);
        var backend = await CampaignJournalMartenBackend.CreateAsync(documentStore, OperatorSecret,
            cancellationToken: cancellationToken);
        var store = new CampaignDurableAuthorityJournalStore(backend);
        var request = Request();
        await store.CreateCampaignAsync(request);
        var before = await store.VerifyReadOnlyAsync(request.CampaignId, cancellationToken);
        Assert.NotNull(before);
        Assert.True(before.IsVerified);
        Assert.Equal(before, await store.VerifyReadOnlyAsync(request.CampaignId, cancellationToken));
        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            // Corrupt only the test stream's explicit sequence header. This fails
            // during Marten materialization, before the journal checks event bytes.
            command.CommandText = """
                UPDATE public.mt_events SET headers=jsonb_set(headers,'{sequence}','"invalid"'::jsonb)
                WHERE stream_id=@campaignId
                """;
            command.Parameters.AddWithValue("campaignId", request.CampaignId);
            Assert.Equal(1, await command.ExecuteNonQueryAsync(cancellationToken));
        }
        var corrupt = await store.VerifyReadOnlyAsync(request.CampaignId, cancellationToken);
        Assert.NotNull(corrupt);
        Assert.False(corrupt.IsVerified);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public async Task VerificationHttpIsNotMappedWhenOutwardApiDisabled(string environment)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = environment;
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        await using var app = builder.Build();
        if (environment == "Production")
            Assert.Throws<InvalidOperationException>(() => app.MapDurableCampaignApi());
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = new HttpClient
        {
            BaseAddress = new Uri(app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single())
        };
        using var response = await client.GetAsync("/api/v2/campaigns/test/verification", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
