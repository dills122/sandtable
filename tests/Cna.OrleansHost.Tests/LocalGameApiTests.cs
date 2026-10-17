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
    public async Task TrustedCreatedLabServesPinnedInitialViewsForEachAuthorizedSeat()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Development";
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<LocalSessionStore>();
        builder.Services.AddSingleton<TemporaryPlayerViewProjection>();
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
                var response = await Send(client,
                    Get("/api/v1/campaigns/host-rsh-001/view", seatCookie));
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                using var view = JsonDocument.Parse(json);
                Assert.Equal(side, view.RootElement.GetProperty("seat").GetString());
                Assert.Equal(9, view.RootElement.GetProperty("locations").GetArrayLength());
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
            Assert.Equal(HttpStatusCode.ServiceUnavailable,
                (await Send(client, Get("/api/v1/campaigns/host-rsh-001/view", axisCookie!)))
                .StatusCode);
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
    public void GrantsAreBoundToOperatorAndExpire()
    {
        var clock = new ManualClock();
        var store = new LocalSessionStore(clock);
        var owner = store.Bootstrap(null)!;
        var other = store.Bootstrap(null)!;
        var grants = store.IssueGrants(owner.OperatorId, "host-rsh-001");
        Assert.Null(store.Redeem(other, "host-rsh-001", grants.Axis));
        clock.Advance(TimeSpan.FromMinutes(11));
        Assert.Null(store.Redeem(owner, "host-rsh-001", grants.Axis));
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
    public void SeatSessionsExpireAndExpiredGrantsAreSwept()
    {
        var clock = new ManualClock();
        var store = new LocalSessionStore(clock);
        var owner = store.Bootstrap(null)!;
        var first = store.IssueGrants(owner.OperatorId, "host-rsh-001");
        var seat = store.Redeem(owner, "host-rsh-001", first.Axis)!;
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

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
