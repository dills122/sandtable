using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cna.OrleansHost.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Cna.OrleansHost.Tests;

public sealed class LocalGameApiTests
{
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
            var rebound = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bootstrap");
            rebound.Headers.Host = "attacker.invalid";
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, rebound)).StatusCode);

            var bootstrap = await client.GetAsync("/api/v1/bootstrap", TestContext.Current.CancellationToken);
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

            var refresh = await Send(client, Get("/api/v1/bootstrap", seatCookie));
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
        }
        finally { await app.StopAsync(TestContext.Current.CancellationToken); }
    }

    [Fact]
    public void GrantsAreBoundToOperatorAndExpire()
    {
        var clock = new ManualClock();
        var store = new LocalSessionStore(clock);
        var owner = store.Bootstrap(null);
        var other = store.Bootstrap(null);
        var grants = store.IssueGrants(owner.OperatorId, "host-rsh-001");
        Assert.Null(store.Redeem(other, "host-rsh-001", grants.Axis));
        clock.Advance(TimeSpan.FromMinutes(11));
        Assert.Null(store.Redeem(owner, "host-rsh-001", grants.Axis));
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, HttpRequestMessage request) =>
        client.SendAsync(request, TestContext.Current.CancellationToken);

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
}
