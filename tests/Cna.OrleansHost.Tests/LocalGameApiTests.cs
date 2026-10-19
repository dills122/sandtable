using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.OrleansHost;
using Cna.OrleansHost.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;

namespace Cna.OrleansHost.Tests;

public sealed class LocalGameApiTests
{
    [Fact]
    public async Task CreationCapacityIsReservedBeforeGrainCommit()
    {
        var clock = new ManualClock();
        var sessions = new LocalSessionStore(clock);
        var owner = sessions.Bootstrap(null)!;
        for (var index = 0; index < LocalSessionStore.MaxGrants / 2; index++)
            sessions.IssueGrants(owner.OperatorId, "host-rsh-001");
        var creator = new CountingCreator();
        using var creations = new LocalCampaignCreationStore();
        var requestId = Guid.NewGuid().ToString("D");
        Assert.Equal(LocalCreationStatus.Unavailable, (await creations.CreateAsync(owner,
            "host-rsh-001", requestId, sessions, creator,
            TestContext.Current.CancellationToken)).Status);
        Assert.Equal(0, creator.Calls);
        clock.Advance(TimeSpan.FromMinutes(11));
        Assert.Equal(LocalCreationStatus.Created, (await creations.CreateAsync(owner,
            "host-rsh-001", requestId, sessions, creator,
            TestContext.Current.CancellationToken)).Status);
        Assert.Equal(1, creator.Calls);
    }

    [Fact]
    public async Task ExpiredReservationRetainsOriginalGrantsWithoutRenewal()
    {
        var clock = new ManualClock();
        var sessions = new LocalSessionStore(clock);
        var owner = sessions.Bootstrap(null)!;
        var creator = new HeldCreator();
        using var creations = new LocalCampaignCreationStore();
        var requestId = Guid.NewGuid().ToString("D");
        var first = creations.CreateAsync(owner, "host-rsh-001", requestId,
            sessions, creator, TestContext.Current.CancellationToken);
        await creator.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromMinutes(11));
        creator.Release.SetResult(new TemporaryCampaignReply
        { Status = TemporaryCampaignStatus.Created });
        var committed = await first;
        Assert.Equal(LocalCreationStatus.Created, committed.Status);
        var recovered = await creations.CreateAsync(owner, "host-rsh-001", requestId,
            sessions, creator, TestContext.Current.CancellationToken);
        Assert.Equal(LocalCreationStatus.Created, recovered.Status);
        Assert.Equal(1, creator.Calls);
        Assert.Equal(JsonSerializer.Serialize(committed.Response),
            JsonSerializer.Serialize(recovered.Response));
        var grants = JsonDocument.Parse(JsonSerializer.Serialize(recovered.Response))
            .RootElement.GetProperty("seatGrants");
        Assert.Null(await sessions.RedeemAsync(owner, "host-rsh-001",
            grants.GetProperty("axis").GetString()!));
    }

    [Fact]
    public async Task CancelledCreationWaitKeepsSameRequestRecoverable()
    {
        var sessions = new LocalSessionStore(TimeProvider.System);
        var owner = sessions.Bootstrap(null)!;
        var creator = new HeldCreator();
        using var creations = new LocalCampaignCreationStore();
        var requestId = Guid.NewGuid().ToString("D");
        using var cancellation = new CancellationTokenSource();
        var first = creations.CreateAsync(owner, "host-rsh-001", requestId,
            sessions, creator, cancellation.Token);
        await creator.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        using var queuedCancellation = new CancellationTokenSource();
        var queued = creations.CreateAsync(owner, "host-rsh-001", requestId,
            sessions, creator, queuedCancellation.Token);
        queuedCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
        Assert.Equal(1, creator.Calls);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        var recovered = await creations.CreateAsync(owner, "host-rsh-001", requestId,
            sessions, creator, TestContext.Current.CancellationToken);
        Assert.Equal(LocalCreationStatus.Created, recovered.Status);
        Assert.Equal(2, creator.Calls);
        creator.Release.SetResult(new TemporaryCampaignReply
        { Status = TemporaryCampaignStatus.Created });
    }

    [Fact]
    public async Task TimedOutCreationReconcilesOnExactRetry()
    {
        var sessions = new LocalSessionStore(TimeProvider.System);
        var owner = sessions.Bootstrap(null)!;
        var creator = new HeldCreator();
        using var creations = new LocalCampaignCreationStore(TimeSpan.FromMilliseconds(10));
        var requestId = Guid.NewGuid().ToString("D");
        var first = await creations.CreateAsync(owner, "host-rsh-001", requestId,
            sessions, creator, TestContext.Current.CancellationToken);
        Assert.Equal(LocalCreationStatus.Unavailable, first.Status);
        Assert.Equal(LocalCreationStatus.Conflict, (await creations.CreateAsync(owner,
            "host-rsh-001", Guid.NewGuid().ToString("D"), sessions, creator,
            TestContext.Current.CancellationToken)).Status);
        var recovered = await creations.CreateAsync(owner, "host-rsh-001", requestId,
            sessions, creator, TestContext.Current.CancellationToken);
        Assert.Equal(LocalCreationStatus.Created, recovered.Status);
        Assert.Equal(2, creator.Calls);
        creator.Release.SetResult(new TemporaryCampaignReply
        { Status = TemporaryCampaignStatus.Created });
    }

    [Fact]
    public async Task ViewReadInFlightIsDeniedAfterSeatRotation()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Development";
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<LocalSessionStore>();
        builder.Services.AddSingleton<TemporaryPlayerViewProjection>();
        var reader = new HeldPrivateCutReader();
        builder.Services.AddSingleton<ITemporaryPrivateCutReader>(reader);
        await using var app = builder.Build();
        app.MapLocalGameApi();
        await app.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient(new HttpClientHandler { UseCookies = false })
            { BaseAddress = new Uri(address) };
            var store = app.Services.GetRequiredService<LocalSessionStore>();
            var operatorSession = store.Bootstrap(null)!;
            var grants = store.IssueGrants(operatorSession.OperatorId, "host-rsh-001");
            var axis = (await store.RedeemAsync(operatorSession,
                "host-rsh-001", grants.Axis))!;
            var pending = Send(client, Get("/api/v1/campaigns/host-rsh-001/view",
                $"sandtable_session={axis.Token}"));
            await reader.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(await store.RedeemAsync(axis,
                "host-rsh-001", grants.Commonwealth));
            reader.Release.SetResult(new TemporaryPrivateCutReply
            { Status = TemporaryCampaignStatus.Current });
            Assert.Equal(HttpStatusCode.Forbidden, (await pending).StatusCode);
        }
        finally { await app.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task LocalCreationIssuesOneGrantPairAndRetainsExactRetryResponse()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Development";
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<LocalSessionStore>();
        builder.Services.AddSingleton<LocalCampaignCreationStore>();
        builder.Services.AddSingleton<ITemporaryCampaignCreator, TemporaryCampaignCreator>();
        builder.Services.AddSingleton<ITemporaryPrivateCutReader, TemporaryPrivateCutReader>();
        builder.Services.AddSingleton<TemporaryPlayerViewProjection>();
        builder.Host.UseOrleans(silo => silo.UseTemporaryCampaignDevelopmentSilo(FreePort()));
        await using var app = builder.Build();
        app.MapLocalGameApi();
        await app.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient(new HttpClientHandler { UseCookies = false })
            { BaseAddress = new Uri(address) };
            var fixtures = await Send(client, new HttpRequestMessage(HttpMethod.Get, "/api/v1/fixtures"));
            Assert.Equal(HttpStatusCode.OK, fixtures.StatusCode);
            var fixtureJson = await fixtures.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain("configurationHash", fixtureJson);
            Assert.DoesNotContain("12345", fixtureJson);
            var bootstrap = await Send(client, Bootstrap(client));
            var cookie = bootstrap.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
            var csrf = JsonDocument.Parse(await bootstrap.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken)).RootElement.GetProperty("csrfToken")
                .GetString()!;
            var id = Guid.NewGuid().ToString("D");
            HttpRequestMessage Create(string fixture = "host-rsh-001", string? token = null,
                string? requestId = null)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/campaigns")
                {
                    Content = JsonContent.Create(new
                    {
                        contractVersion = 1,
                        fixtureId = fixture,
                        requestId = requestId ?? id,
                    }),
                };
                request.Headers.TryAddWithoutValidation("Cookie", token ?? cookie);
                request.Headers.TryAddWithoutValidation("X-Sandtable-CSRF", csrf);
                return request;
            }
            Assert.Equal(HttpStatusCode.BadRequest,
                (await Send(client, Create("unknown"))).StatusCode);
            var badCsrf = Create();
            badCsrf.Headers.Remove("X-Sandtable-CSRF");
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, badCsrf)).StatusCode);
            var badOrigin = Create();
            badOrigin.Headers.TryAddWithoutValidation("Origin", "https://attacker.invalid");
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, badOrigin)).StatusCode);
            var first = await Send(client, Create());
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            var json = await first.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain("configurationHash", json);
            Assert.DoesNotContain("stateVersion", json);
            Assert.DoesNotContain("12345", json);
            using var document = JsonDocument.Parse(json);
            var grants = document.RootElement.GetProperty("seatGrants");
            var axis = grants.GetProperty("axis").GetString()!;
            var commonwealth = grants.GetProperty("commonwealth").GetString()!;
            Assert.NotEqual(axis, commonwealth);
            Assert.Equal(json, await (await Send(client, Create())).Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken));
            Assert.Equal(HttpStatusCode.Conflict,
                (await Send(client, Create(requestId: Guid.NewGuid().ToString("D")))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await Send(client, Seat("other", cookie, csrf, axis))).StatusCode);
            var entered = await Send(client, Seat("host-rsh-001", cookie, csrf, axis));
            Assert.Equal(HttpStatusCode.OK, entered.StatusCode);
            var seatCookie = entered.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
            Assert.Equal(HttpStatusCode.Forbidden,
                (await Send(client, Seat("host-rsh-001", seatCookie, csrf, axis))).StatusCode);
            Assert.Equal(HttpStatusCode.OK,
                (await Send(client, Get("/api/v1/campaigns/host-rsh-001/view", seatCookie)))
                .StatusCode);
            var seatedBootstrap = await Send(client, Bootstrap(client, seatCookie));
            var seatCsrf = JsonDocument.Parse(await seatedBootstrap.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken)).RootElement.GetProperty("csrfToken")
                .GetString()!;
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await Send(client, Create(token: seatCookie))).StatusCode);
            var switched = await Send(client,
                Seat("host-rsh-001", seatCookie, seatCsrf, commonwealth));
            Assert.Equal(HttpStatusCode.OK, switched.StatusCode);
            var commonwealthCookie = switched.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
            Assert.Equal(HttpStatusCode.Forbidden,
                (await Send(client, Get("/api/v1/campaigns/host-rsh-001/view", seatCookie)))
                .StatusCode);
            Assert.Equal(HttpStatusCode.OK,
                (await Send(client, Get("/api/v1/campaigns/host-rsh-001/view", commonwealthCookie)))
                .StatusCode);
        }
        finally { await app.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task TrustedCreatedLabServesPinnedInitialAndEarlySuccessorViewsForEachSeat()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Development";
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<LocalSessionStore>();
        builder.Services.AddSingleton<TemporaryPlayerViewProjection>();
        builder.Services.AddSingleton<ITemporaryPrivateCutReader, TemporaryPrivateCutReader>();
        builder.Host.UseOrleans(silo => silo.UseTemporaryCampaignDevelopmentSilo(FreePort()));
        await using var app = builder.Build();
        app.MapLocalGameApi();
        await app.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var access = app.Services.GetRequiredService<TemporaryCampaignAccess>();
            var grain = app.Services.GetRequiredService<IGrainFactory>()
                .GetGrain<ITemporaryCampaignGrain>("host-rsh-001");
            Assert.Equal(TemporaryCampaignStatus.Created,
                (await grain.CreateAsync(access.Capability, "operator-test", "request-test")).Status);
            var address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient(new HttpClientHandler { UseCookies = false })
            { BaseAddress = new Uri(address) };
            var store = app.Services.GetRequiredService<LocalSessionStore>();
            string? axisCookie = null;
            string? commonwealthCookie = null;
            string[]? axisContacts = null;
            foreach (var side in new[] { "axis", "commonwealth" })
            {
                var bootstrap = await Send(client, Bootstrap(client));
                var cookie = bootstrap.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
                var csrf = JsonDocument.Parse(await bootstrap.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken)).RootElement
                    .GetProperty("csrfToken").GetString()!;
                var operatorId = store.Find(cookie.Split('=')[1])!.OperatorId;
                var grants = store.IssueGrants(operatorId, "host-rsh-001");
                var grant = side == "axis" ? grants.Axis : grants.Commonwealth;
                var entered = await Send(client, Seat("host-rsh-001", cookie, csrf, grant));
                Assert.Equal(HttpStatusCode.OK, entered.StatusCode);
                var seatCookie = entered.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
                if (side == "axis") axisCookie = seatCookie;
                else commonwealthCookie = seatCookie;
                var response = await Send(client,
                    Get("/api/v1/campaigns/host-rsh-001/view", seatCookie));
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                using var view = JsonDocument.Parse(json);
                Assert.Equal(side, view.RootElement.GetProperty("seat").GetString());
                Assert.Equal(15, view.RootElement.EnumerateObject().Count());
                Assert.Equal(9, view.RootElement.GetProperty("locations").GetArrayLength());
                var own = view.RootElement.GetProperty("ownFormations").EnumerateArray().ToArray();
                Assert.Equal(2, own.Length);
                Assert.All(own, formation =>
                {
                    var label = formation.GetProperty("label").GetString()!;
                    Assert.StartsWith(side == "axis" ? "Axis " : "Commonwealth ", label);
                    Assert.True(Guid.TryParse(formation.GetProperty("formationId").GetString(), out _));
                });
                var contacts = view.RootElement.GetProperty("apparentContacts").EnumerateArray()
                    .ToArray();
                Assert.Equal(2, contacts.Length);
                Assert.All(contacts, contact =>
                {
                    Assert.Equal("Unidentified presence", contact.GetProperty("label").GetString());
                    Assert.True(Guid.TryParse(contact.GetProperty("contactId").GetString(), out _));
                });
                var contactIds = contacts.Select(contact =>
                    contact.GetProperty("contactId").GetString()!).ToArray();
                if (side == "axis") axisContacts = contactIds;
                else Assert.Empty(contactIds.Intersect(axisContacts!, StringComparer.Ordinal));
                // Creation precedes trusted System progression. No human candidate is yet legal.
                Assert.Empty(view.RootElement.GetProperty("legalActions").EnumerateArray());
                Assert.Equal("unsupported-phase", view.RootElement.GetProperty("position")
                    .GetProperty("decision").GetString());
                Assert.DoesNotContain("stateVersion", json);
                Assert.DoesNotContain("RepresentationId", json);
                Assert.DoesNotContain("CoreActionId", json);
                Assert.DoesNotContain("axis-infantry.formation", json);
                Assert.DoesNotContain("commonwealth-infantry.formation", json);
                Assert.Equal(HttpStatusCode.Forbidden,
                    (await Send(client, Get("/api/v1/campaigns/other/view", seatCookie)))
                    .StatusCode);
            }
            Assert.Equal(TemporaryCampaignStatus.StepLimit,
                (await grain.AdvanceSystemAsync(access.Capability, 1)).Status);
            foreach (var cookie in new[] { axisCookie!, commonwealthCookie! })
            {
                var response = await Send(client,
                    Get("/api/v1/campaigns/host-rsh-001/view", cookie));
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var json = await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken);
                using var view = JsonDocument.Parse(json);
                Assert.Equal("0", view.RootElement.GetProperty("nextDiaryCursor").GetString());
                Assert.Empty(view.RootElement.GetProperty("legalActions").EnumerateArray());
                Assert.Empty(view.RootElement.GetProperty("warDiary").EnumerateArray());
                Assert.DoesNotContain("sourcePin", json);
            }
            Assert.Equal(TemporaryCampaignStatus.NeedsHuman,
                (await grain.AdvanceSystemAsync(access.Capability, 8)).Status);
            foreach (var (cookie, expectedActions) in new[]
            {
                (axisCookie!, 2), (commonwealthCookie!, 0),
            })
            {
                var response = await Send(client,
                    Get("/api/v1/campaigns/host-rsh-001/view", cookie));
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                using var view = JsonDocument.Parse(await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken));
                Assert.Equal(expectedActions,
                    view.RootElement.GetProperty("legalActions").GetArrayLength());
                Assert.Equal("0", view.RootElement.GetProperty("nextDiaryCursor").GetString());
                Assert.Empty(view.RootElement.GetProperty("warDiary").EnumerateArray());
            }
        }
        finally { await app.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task LoopbackSessionsEnforceOriginCsrfCampaignAndSeatRotation()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Development";
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<LocalSessionStore>();
        await using var app = builder.Build();
        app.MapLocalGameApi();
        await app.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient(new HttpClientHandler { UseCookies = false })
            { BaseAddress = new Uri(address) };
            var hostile = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bootstrap");
            hostile.Headers.TryAddWithoutValidation("Origin", "https://attacker.invalid");
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, hostile)).StatusCode);
            var crossSiteNavigation = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bootstrap");
            crossSiteNavigation.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "cross-site");
            Assert.Equal(HttpStatusCode.Forbidden,
                (await Send(client, crossSiteNavigation)).StatusCode);
            var directNavigation = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bootstrap");
            directNavigation.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "none");
            Assert.Equal(HttpStatusCode.Forbidden,
                (await Send(client, directNavigation)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await client.GetAsync("/api/v1/bootstrap", TestContext.Current.CancellationToken))
                .StatusCode);
            var browserFetch = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bootstrap");
            browserFetch.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
            Assert.Equal(HttpStatusCode.OK, (await Send(client, browserFetch)).StatusCode);
            var rebound = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bootstrap");
            rebound.Headers.Host = "attacker.invalid";
            rebound.Headers.TryAddWithoutValidation("Origin", client.BaseAddress!.GetLeftPart(UriPartial.Authority));
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, rebound)).StatusCode);

            var bootstrap = await Send(client, Bootstrap(client));
            Assert.Equal(HttpStatusCode.OK, bootstrap.StatusCode);
            var cookie = bootstrap.Headers.GetValues("Set-Cookie").Single();
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
            var token = cookie.Split(';')[0];
            var csrf = (await JsonDocument.ParseAsync(await bootstrap.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken))
                .RootElement.GetProperty("csrfToken").GetString()!;
            var session = app.Services.GetRequiredService<LocalSessionStore>().Find(token.Split('=')[1])!;
            var grants = app.Services.GetRequiredService<LocalSessionStore>()
                .IssueGrants(session.OperatorId, "host-rsh-001");
            Assert.NotEqual(grants.Axis, grants.Commonwealth);

            var badCampaign = Seat("other", token, csrf, grants.Axis);
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, badCampaign)).StatusCode);
            var forgedGrant = Seat("host-rsh-001", token, csrf, Guid.NewGuid().ToString());
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, forgedGrant)).StatusCode);
            var forgedActor = Seat("host-rsh-001", token, csrf, grants.Axis);
            forgedActor.Content = JsonContent.Create(new
            {
                contractVersion = 1,
                grantId = grants.Axis,
                actorId = "host-system",
            });
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, forgedActor)).StatusCode);
            var badCsrf = Seat("host-rsh-001", token, "wrong", grants.Axis);
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, badCsrf)).StatusCode);
            var crossOrigin = Seat("host-rsh-001", token, csrf, grants.Axis);
            crossOrigin.Headers.TryAddWithoutValidation("Origin", "http://localhost:4200");
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, crossOrigin)).StatusCode);

            var entered = await Send(client, Seat("host-rsh-001", token, csrf, grants.Axis));
            Assert.Equal(HttpStatusCode.OK, entered.StatusCode);
            var seatCookie = entered.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
            var body = await entered.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("\"seat\":\"axis\"", body);
            Assert.DoesNotContain(grants.Axis, body);
            Assert.DoesNotContain(grants.Commonwealth, body);
            var generation = JsonDocument.Parse(body).RootElement.GetProperty("sessionGeneration").GetString();
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await Send(client, Get("/api/v1/campaigns/host-rsh-001/session", token))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await Send(client, Get("/api/v1/campaigns/other/session", seatCookie))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await Send(client, Get("/api/v1/campaigns/other/view", seatCookie))).StatusCode);
            var unavailable = await Send(client, Get("/api/v1/campaigns/host-rsh-001/view", seatCookie));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
            var unavailableBody = await unavailable.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("unavailable", unavailableBody);
            Assert.DoesNotContain("stateVersion", unavailableBody);
            Assert.DoesNotContain("LegalActions", unavailableBody);

            var refresh = await Send(client, Bootstrap(client, seatCookie));
            var seatCsrf = (await JsonDocument.ParseAsync(await refresh.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken))
                .RootElement.GetProperty("csrfToken").GetString()!;
            Assert.Equal(HttpStatusCode.Forbidden,
                (await Send(client, Seat("host-rsh-001", seatCookie, seatCsrf, grants.Axis))).StatusCode);
            var switched = await Send(client, Seat("host-rsh-001", seatCookie, seatCsrf,
                grants.Commonwealth));
            Assert.Equal(HttpStatusCode.OK, switched.StatusCode);
            var secondBody = await switched.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("\"seat\":\"commonwealth\"", secondBody);
            Assert.NotEqual(generation, JsonDocument.Parse(secondBody).RootElement
                .GetProperty("sessionGeneration").GetString());
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await Send(client, Get("/api/v1/campaigns/host-rsh-001/session", seatCookie))).StatusCode);
            var secondCookie = switched.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
            var active = await Send(client, Get("/api/v1/campaigns/host-rsh-001/session", secondCookie));
            Assert.Equal(HttpStatusCode.OK, active.StatusCode);
            Assert.Contains("\"seat\":\"commonwealth\"",
                await active.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var secondBootstrap = await Send(client, Bootstrap(client, secondCookie));
            var secondCsrf = (await JsonDocument.ParseAsync(
                await secondBootstrap.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                cancellationToken: TestContext.Current.CancellationToken)).RootElement
                .GetProperty("csrfToken").GetString()!;
            var leave = new HttpRequestMessage(HttpMethod.Delete,
                "/api/v1/campaigns/host-rsh-001/seat");
            leave.Headers.TryAddWithoutValidation("Cookie", secondCookie);
            leave.Headers.TryAddWithoutValidation("X-Sandtable-CSRF", secondCsrf);
            Assert.Equal(HttpStatusCode.NoContent, (await Send(client, leave)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await Send(client, Get("/api/v1/campaigns/host-rsh-001/session", secondCookie)))
                .StatusCode);

            var store = app.Services.GetRequiredService<LocalSessionStore>();
            for (var i = 1; i < LocalSessionStore.MaxSessions; i++)
                Assert.NotNull(store.Bootstrap(null));
            Assert.Equal(HttpStatusCode.ServiceUnavailable,
                (await Send(client, Bootstrap(client))).StatusCode);
        }
        finally { await app.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public async Task GrantsAreBoundToOperatorAndExpire()
    {
        var clock = new ManualClock();
        var store = new LocalSessionStore(clock);
        var owner = store.Bootstrap(null)!;
        var other = store.Bootstrap(null)!;
        var grants = store.IssueGrants(owner.OperatorId, "host-rsh-001");
        Assert.Null(await store.RedeemAsync(other, "host-rsh-001", grants.Axis));
        clock.Advance(TimeSpan.FromMinutes(11));
        Assert.Null(await store.RedeemAsync(owner, "host-rsh-001", grants.Axis));
    }

    [Fact]
    public void AnonymousSessionsHaveBoundedCapacityAndExpire()
    {
        var clock = new ManualClock();
        var store = new LocalSessionStore(clock);
        var first = store.Bootstrap(null)!;
        for (var index = 1; index < LocalSessionStore.MaxSessions; index++)
            Assert.NotNull(store.Bootstrap(null));
        Assert.Null(store.Bootstrap(null));
        Assert.Equal(first, store.Bootstrap(first.Token));
        clock.Advance(TimeSpan.FromMinutes(16));
        Assert.Null(store.Find(first.Token));
        Assert.NotNull(store.Bootstrap(null));
    }

    [Fact]
    public async Task SeatSessionsExpireAndExpiredGrantsAreSwept()
    {
        var clock = new ManualClock();
        var store = new LocalSessionStore(clock);
        var owner = store.Bootstrap(null)!;
        var first = store.IssueGrants(owner.OperatorId, "host-rsh-001");
        var seat = (await store.RedeemAsync(owner, "host-rsh-001", first.Axis))!;
        Assert.Equal("axis", seat.Seat);
        for (var index = 1; index < LocalSessionStore.MaxGrants / 2; index++)
            store.IssueGrants(owner.OperatorId, "host-rsh-001");
        Assert.Throws<InvalidOperationException>(() =>
            store.IssueGrants(owner.OperatorId, "host-rsh-001"));
        clock.Advance(TimeSpan.FromMinutes(11));
        var replacement = store.IssueGrants(owner.OperatorId, "host-rsh-001");
        Assert.NotEqual(replacement.Axis, replacement.Commonwealth);
        clock.Advance(TimeSpan.FromHours(2));
        Assert.Null(store.Find(seat.Token));
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, HttpRequestMessage request) =>
        client.SendAsync(request, TestContext.Current.CancellationToken);

    private static HttpRequestMessage Bootstrap(HttpClient client, string? cookie = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bootstrap");
        request.Headers.TryAddWithoutValidation("Origin", client.BaseAddress!.GetLeftPart(UriPartial.Authority));
        if (cookie is not null) request.Headers.TryAddWithoutValidation("Cookie", cookie);
        return request;
    }

    private static HttpRequestMessage Seat(string campaignId, string cookie, string csrf, string grant)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/campaigns/{campaignId}/seat")
        { Content = JsonContent.Create(new { contractVersion = 1, grantId = grant }) };
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        request.Headers.TryAddWithoutValidation("X-Sandtable-CSRF", csrf);
        return request;
    }

    private static HttpRequestMessage Get(string path, string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        return request;
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => now;
        internal void Advance(TimeSpan by) => now += by;
    }

    private sealed class HeldPrivateCutReader : ITemporaryPrivateCutReader
    {
        internal TaskCompletionSource Started { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource<TemporaryPrivateCutReply> Release { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<TemporaryPrivateCutReply> ReadAsync(string campaignId,
            CampaignActionAudience audience, string sessionGeneration)
        {
            Started.SetResult();
            return Release.Task;
        }
    }

    private sealed class CountingCreator : ITemporaryCampaignCreator
    {
        internal int Calls { get; private set; }

        public Task<TemporaryCampaignReply> CreateAsync(string campaignId,
            string operatorId, string requestId)
        {
            Calls++;
            return Task.FromResult(new TemporaryCampaignReply
            { Status = TemporaryCampaignStatus.Created });
        }
    }

    private sealed class HeldCreator : ITemporaryCampaignCreator
    {
        internal TaskCompletionSource Started { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource<TemporaryCampaignReply> Release { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Calls { get; private set; }

        public Task<TemporaryCampaignReply> CreateAsync(string campaignId,
            string operatorId, string requestId)
        {
            Calls++;
            if (Calls > 1)
                return Task.FromResult(new TemporaryCampaignReply
                { Status = TemporaryCampaignStatus.Created });
            Started.SetResult();
            return Release.Task;
        }
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
